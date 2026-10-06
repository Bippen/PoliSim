using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Elections.Generated;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-12 (`COMPLETED.md` §801): THE SENATE COUNT PROOF - R-US19's instrument. It runs R-US19's rule, the fenced block in
    /// `docs/specs/USA_STAGE_PLAN.md` (preregistered §794, amended §797 by Elias's rulings G1-G3), on the Senate's record: (a) the state's REP share
    /// by R-US14 (a)'s derivation and (b) the seat's base share rolled by the House's own-line swing, each called seat by seat against the winner of
    /// the target race's deciding count over K1 (2018 -> 2024, Class I) and K3 (2016 -> 2022, Class III) - M and N per cycle, step 1 cycle by cycle,
    /// control (S2), (c) printed per parent, the eleven declared readings (S4; (vii), (viii), (ix) and (xi) printed only by G1-G3), two sides (S7),
    /// the identity (S8) and the saved pages (S9), the outcome and the verdict line, and the block's printed rows. Every input is the generated
    /// catalog (<see cref="UsPresidentialReturns"/>: the Senate's races, seats and days, the House's own-line votes by state, the presidential rows).
    ///
    /// <para>Pinned in the cheap bar and the documents bar: the card's fourth GENERATED block (`docs/reference/US_ELECTIONS.md`) is what
    /// <see cref="WriteReadings"/> writes; a stale block fails. The instrument holds the block's digest list and the ruling record; the line after
    /// the block in the plan mirrors the record's state, section and ruling. Under ASKED the bars pass while the decisive digest and the outcome
    /// equal the registered ones.</para>
    /// </summary>
    public static class UsSenateRaceCheck
    {
        // ------------------------------------------------------------------ the block's digests and the ruling record (R-US19's THE ASK AND THE BARS)
        private static readonly (string Digest, string Why)[] RuleDigests =
        {
            ("03382d0c0dd22975dea191650f74962bd852477a86ad177944bd93455f425f9c", "preregistered (§794)"),
            ("1caec656193a6d2d2c9c235af0cefe4d591134a24a3f7d338be0dac0108e3957", "AMENDED (G1-G3, §797): B7 and B2 answered, R-US14 ruled (a), Sinema's reading ruled"),
        };

        private const string RulingState = "ASKED";
        private const string RulingSection = "§801";
        private const string RulingOutcomeOverD = "S1 + S4 [(i)]";
        private const string RulingDecisiveDigest = "1ede3f625179cddfd3e1d4ea500588c774e5c046033032131db4c55cccc466ed";
        /// <summary>RULED only: the ruling's words and the option it names - one the block's THE ASK AND THE BARS says can be built.</summary>
        private const string RulingRuled = "", RulingOption = "";
        private static readonly string[] BuildableOptions =
        {
            "(a)", "(b)", "(a) on presidential days and (b) at midterms", "(b) on presidential days and (a) at midterms",
            "(c) over (a)", "(c) over (b)", "(c) over (a) on presidential days and (b) at midterms", "(c) over (b) on presidential days and (a) at midterms",
        };

        // ------------------------------------------------------------------ the cycles
        private sealed class Cycle { public string Name; public int B; public int T; public int Class; public bool Presidential; public int Count; }
        private static readonly Cycle[] Cycles =
        {
            new Cycle { Name = "K1", B = 2018, T = 2024, Class = 1, Presidential = true, Count = 33 },
            new Cycle { Name = "K3", B = 2016, T = 2022, Class = 3, Presidential = false, Count = 34 },
        };
        private const int A = 0, B = 1;
        private static readonly string[] MethodNames = { "(a)", "(b)" };

        /// <summary>A run's configuration: the declared readings move these, one at a time.</summary>
        private sealed class Config
        {
            public string Name = "the run over D";
            public string[] D = { "K1", "K3" };
            public bool SpecialsIn;            // (ii)
            public bool GenOneSidedOut;        // (iii)
            public bool NoTwoSidedAtWinner;    // (iv)
            public bool NoTwoSidedOut;         // (v)
            public bool TopOnly;               // (vi)
            public bool DcSwing;               // (vii), printed only (G1: B7 answered)
            public int AMethod;                // (viii) 1 proportional, (ix) 2 plain additive - printed only (G2: R-US14 ruled (a))
            public bool MidtermRederived;      // (x)
            public bool SinemaNoSide;          // (xi), printed only (G3)
            public bool PrintedOnly;
        }

        private sealed class Race
        {
            public int Year, Class; public string State, Term, Winner, WinnerClass, Flags, BaseCount;
            public long GenR, GenDc, FinalR, FinalDc, FinalRTop, FinalDcTop, FinalNoneTop;
            public string Key => F("{0} {1} cl{2}{3}", Year, State, Class, Term == "full" ? "" : " (" + Term + ")");
            public bool TwoSided => FinalR > 0 && FinalDc > 0;
        }

        private sealed class Seat { public Race Target, Base; public string Kind; }   // Kind: regular, special (another class, reading (ii))

        // ------------------------------------------------------------------ the catalog, read once
        private static List<Race> _races;
        private static Dictionary<int, double> _vOwn, _vDc;


        private static void Load()
        {
            if (_races != null) { return; }
            _races = UsPresidentialReturns.SenateRaces.Select(r => new Race
            {
                Year = r.Year, State = r.State, Class = r.Class, Term = r.Term, Winner = r.Winner, WinnerClass = r.WinnerClass, Flags = r.Flags, BaseCount = r.BaseCount,
                GenR = r.GenR, GenDc = r.GenDc, FinalR = r.FinalR, FinalDc = r.FinalDc, FinalRTop = r.FinalRTop, FinalDcTop = r.FinalDcTop, FinalNoneTop = r.FinalNoneTop,
            }).ToList();
            _vOwn = new Dictionary<int, double>();
            _vDc = new Dictionary<int, double>();
            foreach (int y in new[] { 2016, 2018, 2020, 2022, 2024 })
            {
                long r = 0, d = 0, fr = 0, fd = 0;
                foreach (var h in UsPresidentialReturns.House) { if (h.Year == y) { r += h.VotesR; d += h.VotesD; } }
                foreach (var x in UsPresidentialReturns.HouseDistricts) { if (x.Year == y) { fr += x.FinalR; fd += x.FinalD; } }
                _vOwn[y] = (double)r / (r + d);
                _vDc[y] = (double)fr / (fr + fd);
            }
        }

        /// <summary>The polling day: the Tuesday next after the first Monday in November (2 U.S.C. 7, quoted in records_by_date.md section 4).</summary>
        private static string PollingDay(int year)
        {
            var d = new DateTime(year, 11, 1);
            while (d.DayOfWeek != DayOfWeek.Monday) { d = d.AddDays(1); }
            return d.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static double V(int year, Config cfg) => cfg.DcSwing ? _vDc[year] : _vOwn[year];

        // ------------------------------------------------------------------ sides
        private static string SurnameOf(string name)
        {
            var parts = name.Replace(",", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            while (parts.Count > 1 && new[] { "Jr.", "Jr", "Sr.", "Sr", "II", "III", "IV" }.Contains(parts[parts.Count - 1])) { parts.RemoveAt(parts.Count - 1); }
            return parts.Count == 0 ? "" : parts[parts.Count - 1];
        }

        private static bool IsSinema(string senator) => SurnameOf(senator) == "Sinema";

        /// <summary>A caucus's side: D gives DEM, R gives REP, N (a sourced neither) or none gives no side.</summary>
        private static string CaucusSide(string caucus) => caucus == "D" ? "D" : caucus == "R" ? "R" : "";

        /// <summary>A race winner's side: R and D by the Clerk's label; I by the caucus of his seat row (state, class, surname) - none where he holds
        /// no row in the catalog's window or caucuses with neither; any other party none.</summary>
        private static string WinnerSide(Race r, Config cfg)
        {
            if (r.WinnerClass == "R") { return "R"; }
            if (r.WinnerClass == "D") { return "D"; }
            if (r.WinnerClass != "I") { return ""; }
            var row = UsPresidentialReturns.SenateSeats.FirstOrDefault(s => s.State == r.State && s.Class == r.Class && SurnameOf(s.Senator) == SurnameOf(r.Winner));
            if (row.Senator == null) { return ""; }
            if (cfg.SinemaNoSide && IsSinema(row.Senator)) { return ""; }
            return CaucusSide(row.Caucus);
        }

        /// <summary>The holder of a seat at the end of a day, and his side: REP for R, DEM for D, the caucus's for I or D/I.</summary>
        private static (string Senator, string Side) HolderAt(string state, int cls, string day, Config cfg)
        {
            foreach (var s in UsPresidentialReturns.SenateSeats)
            {
                if (s.State != state || s.Class != cls) { continue; }
                if (string.CompareOrdinal(s.Took, day) > 0) { continue; }
                if (s.Left != "-" && string.CompareOrdinal(s.Left, day) <= 0) { continue; }
                string side = s.Party == "R" ? "R" : s.Party == "D" ? "D" : (cfg.SinemaNoSide && IsSinema(s.Senator)) ? "" : CaucusSide(s.Caucus);
                return (s.Senator, side);
            }
            return (null, "");
        }

        // ------------------------------------------------------------------ the shares
        /// <summary>x(base): final_r / (final_r + final_dc) where both are positive (each side's strongest alone under (vi)); otherwise F-H - the
        /// House's own-line share in the base's state and year, else the state's two-party presidential share of record at the latest presidential
        /// year at or before the base. Under (iv), a base with no two-sided count stands at 1 or 0 by its winner's side.</summary>
        private static double X(Race b, Config cfg, out string sub)
        {
            sub = "";
            long fr = cfg.TopOnly ? b.FinalRTop : b.FinalR, fd = cfg.TopOnly ? b.FinalDcTop : b.FinalDc;
            if (fr > 0 && fd > 0) { return (double)fr / (fr + fd); }
            if (cfg.NoTwoSidedAtWinner)
            {
                string w = WinnerSide(b, cfg);
                if (w != "") { sub = "(iv) at its winner's side"; return w == "R" ? 1.0 : 0.0; }
            }
            var h = UsPresidentialReturns.House.FirstOrDefault(r => r.Year == b.Year && r.State == b.State);
            if (h.VotesR > 0 && h.VotesD > 0) { sub = F("F-H: the House {0} {1}", b.State, b.Year); return (double)h.VotesR / (h.VotesR + h.VotesD); }
            int py = UsPresidentialReturns.Years.Select(y => y.Year).Where(y => y <= b.Year).Max();
            var p = UsPresidentialReturns.States.First(s => s.Year == py && s.State == b.State);
            sub = F("F-H: the presidency {0} {1} (the House row lacks a side)", b.State, py);
            return (double)p.VotesR / (p.VotesR + p.VotesD);
        }

        private static readonly Dictionary<string, Dictionary<string, double[]>> _derived = new Dictionary<string, Dictionary<string, double[]>>(StringComparer.Ordinal);

        /// <summary>R-US14's derivation of one presidential election from the one before, at the later one's national shares (or <paramref name="nat"/>):
        /// each state's (D, R, others), weights the earlier election's votes_total; method 0 (a) normalised uniform, 1 proportional, 2 plain additive.</summary>
        private static Dictionary<string, double[]> Derive(int prev, int next, int method, double[] nat = null, Dictionary<string, double[]> priorOverride = null, Action<bool, string> Check = null)
        {
            string key = F("{0}>{1}:{2}", prev, next, method);
            if (nat == null && priorOverride == null && _derived.TryGetValue(key, out var cached)) { return cached; }
            var p = UsPresidentialReturns.States.Where(s => s.Year == prev).ToArray();
            double[] natP = National(p), natN = nat ?? National(UsPresidentialReturns.States.Where(s => s.Year == next).ToArray());
            double[][] prior = p.Select(s => priorOverride != null ? priorOverride[s.State] : new[] { (double)s.VotesD / s.VotesTotal, (double)s.VotesR / s.VotesTotal, (double)s.VotesOther / s.VotesTotal }).ToArray();
            double[][] shares;
            if (method == 0)
            {
                var regions = p.Select(s => new RegionalVoteModel.RegionInput(s.State, s.VotesTotal, null)).ToArray();
                shares = RegionalVoteModel.RegionalSharesByUniformSwing(natN, regions, prior, out double residual);
                if (residual >= 1e-9) { throw new InvalidOperationException(F("(a) {0} -> {1}: the residual {2:E1} is not below 1e-9", prev, next, residual)); }
            }
            else
            {
                shares = prior.Select(s => method == 1 ? Proportional(s, natP, natN) : Additive(s, natP, natN)).ToArray();
            }
            var d = new Dictionary<string, double[]>(StringComparer.Ordinal);
            for (int i = 0; i < p.Length; i++) { d[p[i].State] = shares[i]; }
            if (nat == null && priorOverride == null) { _derived[key] = d; }
            return d;
        }

        private static double TwoParty(double[] s) => s[1] / (s[0] + s[1]);

        private static double[] National((int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)[] rows)
        {
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in rows) { d += s.VotesD; r += s.VotesR; o += s.VotesOther; t += s.VotesTotal; }
            return new[] { d / t, r / t, o / t };
        }

        private static double[] Proportional(double[] prior, double[] natP, double[] natN)
        {
            var s = new double[prior.Length]; double sum = 0;
            for (int i = 0; i < s.Length; i++) { s[i] = prior[i] * natN[i] / natP[i]; sum += s[i]; }
            for (int i = 0; i < s.Length; i++) { s[i] /= sum; }
            return s;
        }

        private static double[] Additive(double[] prior, double[] natP, double[] natN)
        {
            var s = new double[prior.Length];
            for (int i = 0; i < s.Length; i++) { s[i] = Math.Max(0.0, prior[i] + natN[i] - natP[i]); }
            return s;
        }

        /// <summary>(a)'s share for a state at T: at a presidential T, T-4's derivation at T's true national shares; at a midterm, a_s(P) + V(T) - V(P),
        /// P = T - 2; under (x), P's derived shares re-derived at N(T) built from pi* = pi(P) + V(T) - V(P).</summary>
        private static double AShare(string st, int t, Config cfg)
        {
            bool presidential = UsPresidentialReturns.Years.Any(y => y.Year == t);
            if (presidential) { return TwoParty(Derive(t - 4, t, cfg.AMethod)[st]); }
            int p = t - 2;
            if (cfg.MidtermRederived) { return TwoParty(Rederived(p, V(t, cfg) - V(p, cfg), cfg.AMethod, false)[st]); }
            return TwoParty(Derive(p - 4, p, cfg.AMethod)[st]) + V(t, cfg) - V(p, cfg);
        }

        /// <summary>(x): pi(P) the true national REP share of REP+DEM at P, o its others' share, pi* = pi(P) + dV, N = ((1-o)(1-pi*), (1-o) pi*, o);
        /// the states by R-US14 (a)'s method from (a)'s own derived shares at P (or, for the printed variant, P's record rows), weighted as derived.</summary>
        private static Dictionary<string, double[]> Rederived(int p, double dV, int method, bool recordRows)
        {
            double[] natP = National(UsPresidentialReturns.States.Where(s => s.Year == p).ToArray());
            double pi = natP[1] / (natP[0] + natP[1]), o = natP[2], piStar = pi + dV;
            if (!(piStar > 0 && piStar < 1)) { throw new InvalidOperationException(F("(x): pi* {0} is not in (0, 1)", piStar)); }
            double[] n = { (1 - o) * (1 - piStar), (1 - o) * piStar, o };
            if (recordRows)
            {
                var rows = UsPresidentialReturns.States.Where(s => s.Year == p).ToArray();
                var rec = rows.ToDictionary(s => s.State, s => new[] { (double)s.VotesD / s.VotesTotal, (double)s.VotesR / s.VotesTotal, (double)s.VotesOther / s.VotesTotal }, StringComparer.Ordinal);
                var regions = rows.Select(s => new RegionalVoteModel.RegionInput(s.State, s.VotesTotal, null)).ToArray();
                double[][] sh = RegionalVoteModel.RegionalSharesByUniformSwing(n, regions, rows.Select(s => rec[s.State]).ToArray(), out double res);
                if (res >= 1e-9) { throw new InvalidOperationException("(x)'s variant: the residual is not below 1e-9"); }
                var d = new Dictionary<string, double[]>(StringComparer.Ordinal);
                for (int i = 0; i < rows.Length; i++) { d[rows[i].State] = sh[i]; }
                return d;
            }
            return Derive(p - 4, p, 0, n, Derive(p - 4, p, method));
        }

        /// <summary>A call: REP above 0.5, DEM below; at exactly 0.5 the cascade - the holder's side at the end of the polling day, else the side
        /// holding the Senate majority that day by the caucuses (the Vice President's vote is not in the catalog: a tie there fails, a defect).</summary>
        private static string Call(double share, Seat s, Config cfg, List<string> cascades, Action<bool, string> Check)
        {
            if (share > 0.5) { return "R"; }
            if (share < 0.5) { return "D"; }
            string day = PollingDay(s.Target.Year);
            var h = HolderAt(s.Target.State, s.Target.Class, day, cfg);
            if (h.Side != "") { cascades.Add(F("{0}: exactly 0.5 - the holder {1}'s side {2}", s.Target.Key, h.Senator, h.Side)); return h.Side; }
            var on = UsPresidentialReturns.SenateOn.Last(x => string.CompareOrdinal(x.Day, day) <= 0);
            if (on.DemCaucus != on.RepCaucus) { string side = on.DemCaucus > on.RepCaucus ? "D" : "R"; cascades.Add(F("{0}: exactly 0.5 - the majority {1}", s.Target.Key, side)); return side; }
            Check?.Invoke(false, F("{0}: exactly 0.5, no holder's side and the caucuses tied - the Vice President's vote is not in the catalog (a defect)", s.Target.Key));
            return "";
        }

        // ------------------------------------------------------------------ the seats of a cycle
        private static Race BaseOf(string st, int cls, int t)
        {
            var before = _races.Where(r => r.State == st && r.Class == cls && r.Year < t).ToList();
            if (before.Count == 0) { return null; }
            int y = before.Max(r => r.Year);
            var that = before.Where(r => r.Year == y).ToList();
            return that.Count == 1 ? that[0] : that.FirstOrDefault(r => r.Term == "full") ?? that[0];
        }

        private static List<Seat> SeatsOf(Cycle c, Config cfg)
        {
            var seats = new List<Seat>();
            foreach (Race r in _races.Where(r => r.Year == c.T && r.Class == c.Class && r.Term == "full").OrderBy(r => r.State, StringComparer.Ordinal))
            {
                seats.Add(new Seat { Target = r, Base = BaseOf(r.State, r.Class, c.T), Kind = "regular" });
            }
            if (cfg.SpecialsIn)
            {
                foreach (Race r in _races.Where(r => r.Year == c.T && r.Class != c.Class && r.Term != "full").OrderBy(r => r.State, StringComparer.Ordinal))
                {
                    seats.Add(new Seat { Target = r, Base = BaseOf(r.State, r.Class, c.T), Kind = "special" });
                }
            }
            if (cfg.GenOneSidedOut) { seats.RemoveAll(s => s.Target.GenR == 0 || s.Target.GenDc == 0); }
            if (cfg.NoTwoSidedOut) { seats.RemoveAll(s => s.Base != null && !s.Base.TwoSided); }
            return seats;
        }

        // ------------------------------------------------------------------ a run
        private sealed class CycleRun
        {
            public Cycle C; public List<Seat> Seats; public int R, Z, O;
            public double[][] Share = new double[2][]; public string[][] Calls = new string[2][]; public string[] Truth; public string[] Sub;
            public int[] M = new int[2], N = new int[2], CalledRep = new int[2];
            public List<string>[] Misses = { new List<string>(), new List<string>() };
            public List<string> Cascades = new List<string>();
        }

        private sealed class RunResult
        {
            public Config Cfg; public Dictionary<string, CycleRun> Cycles = new Dictionary<string, CycleRun>(StringComparer.Ordinal);
            public string Step1 = "", By = "", DigestText = ""; public bool S2, S7; public List<string> S8 = new List<string>(), S9 = new List<string>();
            public List<string> Lines = new List<string>(), Listed = new List<string>(); public Dictionary<string, string> Figures = new Dictionary<string, string>(StringComparer.Ordinal);
            public int DCount;
            public List<string> StopList(bool withS4Excluded)
            {
                var l = new List<string>();
                if (S2) { l.Add(DCount >= 2 ? "S2" : "S2 printed"); }
                if (!withS4Excluded)
                {
                    if (S7) { l.Add("S7"); }
                    if (S8.Count > 0) { l.Add("S8 [" + string.Join(", ", S8) + "]"); }
                    if (S9.Count > 0) { l.Add("S9 [" + string.Join(", ", S9) + "]"); }
                }
                return l;
            }
            public string Outcome() { var l = StopList(false); return Step1 + (l.Count > 0 ? " + " + string.Join(", ", l) : ""); }
            /// <summary>S4 compares outcomes less S4, S7, S8 and S9, and, where a reading's D has one cycle, less S2.</summary>
            public string Compared(bool two) => Step1 + (two ? "|S2 " + S2 : "");
        }

        private static CycleRun RunCycle(Cycle c, Config cfg, Action<bool, string> Check)
        {
            var cr = new CycleRun { C = c, Seats = SeatsOf(c, cfg) };
            int n = cr.Seats.Count;
            cr.Truth = new string[n]; cr.Sub = new string[n];
            for (int m = 0; m < 2; m++) { cr.Share[m] = new double[n]; cr.Calls[m] = new string[n]; }
            for (int i = 0; i < n; i++)
            {
                Seat s = cr.Seats[i];
                cr.Truth[i] = WinnerSide(s.Target, cfg);
                if (s.Base == null) { Check?.Invoke(false, F("{0}: its base race is not in the catalog - BILLED, never served by an older row", s.Target.Key)); continue; }
                double x = X(s.Base, cfg, out string sub);
                cr.Sub[i] = sub;
                cr.Share[A][i] = AShare(s.Target.State, c.T, cfg);
                cr.Share[B][i] = x + V(c.T, cfg) - V(s.Base.Year, cfg);
                for (int m = 0; m < 2; m++)
                {
                    cr.Calls[m][i] = Call(cr.Share[m][i], s, cfg, cr.Cascades, Check);
                    bool miss = cr.Truth[i] == "" || cr.Calls[m][i] != cr.Truth[i];
                    if (miss) { cr.M[m]++; cr.Misses[m].Add(s.Target.State + " cl" + s.Target.Class); }
                    if (cr.Calls[m][i] == "R") { cr.CalledRep[m]++; }
                }
            }
            cr.R = cr.Truth.Count(t => t == "R"); cr.Z = 0; cr.O = cr.Truth.Count(t => t == "");
            for (int m = 0; m < 2; m++) { cr.N[m] = SignedDist(cr.CalledRep[m], cr.R, cr.R + cr.Z + cr.O); }
            return cr;
        }

        private static int SignedDist(int x, int lo, int hi) => x < lo ? x - lo : x > hi ? x - hi : 0;

        private static RunResult Evaluate(Config cfg, Action<bool, string> Check)
        {
            var run = new RunResult { Cfg = cfg };
            foreach (Cycle c in Cycles) { run.Cycles[c.Name] = RunCycle(c, cfg, Check); }

            // STEP 6 - S9 first: a target or base race of D whose deciding count rests on no saved page; steps 1-5 run over the cycles fully read
            var unread = new HashSet<string>(StringComparer.Ordinal);
            foreach (Cycle c in Cycles.Where(c => cfg.D.Contains(c.Name)))
            {
                foreach (Seat s in run.Cycles[c.Name].Seats)
                {
                    foreach (Race r in new[] { s.Target, s.Base })
                    {
                        if (r == null || !SavedCount(r)) { string k = r == null ? s.Target.Key + "'s base" : r.Key; if (!run.S9.Contains(k)) { run.S9.Add(k); } unread.Add(c.Name); }
                    }
                }
            }
            run.Lines.Add(run.S9.Count == 0 ? "step 6 - S9: every target and base race of D rests on a saved page" : "step 6 - S9: " + string.Join(", ", run.S9) + " - steps 1-5 run over the cycles fully read");
            Cycle[] d = Cycles.Where(c => cfg.D.Contains(c.Name) && !unread.Contains(c.Name)).ToArray();
            run.DCount = d.Length;
            int Mx(Cycle c, int m) => run.Cycles[c.Name].M[m];
            int An(Cycle c, int m) => Math.Abs(run.Cycles[c.Name].N[m]);
            string mFig = string.Join("; ", d.Select(c => F("{0} M (a) {1}, (b) {2}", c.Name, Mx(c, A), Mx(c, B))));
            string nFig = string.Join("; ", d.Select(c => F("{0} |N| (a) {1}, (b) {2}", c.Name, An(c, A), An(c, B))));
            run.Figures["M"] = mFig; run.Figures["N"] = nFig;
            run.Figures["Nsigned"] = string.Join("; ", d.Select(c => F("{0} N (a) {1:+0;-0;0}, (b) {2:+0;-0;0}", c.Name, run.Cycles[c.Name].N[A], run.Cycles[c.Name].N[B])));

            // STEP 1 - (a) against (b), cycle by cycle
            bool Better(Func<Cycle, int, int> f, int x, int y) => d.All(c => f(c, x) <= f(c, y)) && d.Any(c => f(c, x) < f(c, y));
            int picked = -1;
            if (Better(Mx, A, B)) { picked = A; run.By = "by M"; }
            else if (Better(Mx, B, A)) { picked = B; run.By = "by M"; }
            else if (d.Any(c => Mx(c, A) < Mx(c, B)) && d.Any(c => Mx(c, B) < Mx(c, A))) { run.Step1 = "S1"; }
            else if (Better(An, A, B)) { picked = A; run.By = "by |N|"; }
            else if (Better(An, B, A)) { picked = B; run.By = "by |N|"; }
            else { run.Step1 = "TIE"; }
            if (picked >= 0) { run.Step1 = MethodNames[picked]; }
            run.Lines.Add(F("step 1 - {0}; {1} - {2}", mFig, nFig, picked >= 0 ? "code recommends " + run.Step1 + " " + run.By : run.Step1 == "S1" ? "S1 (the cycles part)" : "TIE (M equal in every cycle, |N| does not part them) - put to Elias with the figures (B2, G1)"));
            if (picked >= 0 && d.Any(c => c.Name == "K1")) { Cycle k1 = d.First(c => c.Name == "K1"); Check?.Invoke(Mx(k1, picked) <= Mx(k1, 1 - picked), F("{0}: the rule never recommends a method with more misses in K1 than the other", cfg.Name)); }

            // STEP 2 - control: where D has two cycles and step 1 picked by M
            bool Above(int x, int y) => d.All(c => An(c, x) > An(c, y));
            if (picked >= 0 && run.By == "by M")
            {
                bool fires = Above(picked, 1 - picked);
                run.Figures["S2"] = F("{0}'s |N| above {1}'s in every cycle - {2}", MethodNames[picked], MethodNames[1 - picked], nFig);
                if (d.Length >= 2) { run.S2 = fires; }
                run.Lines.Add(F("step 2 - {0}{1}", nFig, d.Length >= 2 ? (fires ? " - S2 fires" : " - S2 does not fire") : (fires ? " - S2 PRINTED (one cycle in D): it would fire; counts as not firing" : " - S2 printed (one cycle in D): it would not fire")));
            }
            else if (picked < 0)
            {
                string l = F("control both ways - (a)'s |N| above (b)'s in every cycle {0}, (b)'s above (a)'s {1}: {2}", Above(A, B), Above(B, A), nFig);
                run.Listed.Add(l); run.Lines.Add("step 2 - after " + run.Step1 + ", " + l);
            }
            else { run.Lines.Add("step 2 - step 1 picked by |N|: S2 does not apply"); }

            // STEP 5 - two sides
            var oCycles = d.Where(c => run.Cycles[c.Name].O > 0).ToList();
            run.S7 = oCycles.Count > 0;
            run.Figures["S7"] = run.S7 ? string.Join("; ", oCycles.Select(c => { var cr = run.Cycles[c.Name]; return F("{0}: O {1}; a miss for every method M (a) {2} (b) {3}, |N|+O (a) {4} (b) {5}; O moved into Z M (a) {6} (b) {7}", c.Name, cr.O, cr.M[A], cr.M[B], Math.Abs(cr.N[A]) + cr.O, Math.Abs(cr.N[B]) + cr.O, cr.M[A] - cr.O, cr.M[B] - cr.O); })) : "";
            run.Lines.Add(run.S7 ? "step 5 - S7: " + run.Figures["S7"] : "step 5 - every winner of D's counts has a side: O = 0, S7 does not fire");


            // the decisive digest's figures for this run
            var dg = new StringBuilder();
            foreach (Cycle c in d) { var cr = run.Cycles[c.Name]; dg.Append(F("{0} M {1} {2} N {3} {4};", c.Name, cr.M[A], cr.M[B], cr.N[A], cr.N[B])); }
            run.DigestText = dg.ToString();
            return run;
        }

        /// <summary>A race's deciding count rests on a saved page: the catalog reads every general and every runoff's marks from the Clerk's listing
        /// and a ranked count's rounds (flag R) from the FEC's sheet, each saved and digested; base_count names the page of its count - "(the
        /// Clerk's)" or "(the FEC's)" - and anything else (a count on no saved page) fires S9.</summary>
        private static bool SavedCount(Race r) =>
            System.Text.RegularExpressions.Regex.IsMatch(r.BaseCount, @"^(general|runoff|round \d+) \(the (Clerk|FEC)'s\)")
            && (!r.Flags.Contains("R") || r.BaseCount.Contains("(the FEC's)"));

        /// <summary>The exact two-sided sign test on d_ab:d_ba.</summary>
        private static double SignTest(int ab, int ba)
        {
            int n = ab + ba, k = Math.Min(ab, ba);
            if (n == 0) { return 1.0; }
            double sum = 0, c = 1;
            for (int i = 0; i <= k; i++) { if (i > 0) { c = c * (n - i + 1) / i; } sum += c; }
            return Math.Min(1.0, 2 * sum / Math.Pow(2, n));
        }

        // ------------------------------------------------------------------ the measurement
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            Load();
            _derived.Clear();
            void Both(string line) { sb.Append("    ").Append(line).Append('\n'); card.Add(line); }

            // the block: its digest the list's last
            string plan = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs/specs/USA_STAGE_PLAN.md"), Encoding.UTF8).Replace("\r\n", "\n");
            const string Begin = "<!-- R-US19 RULE BEGIN -->\n";
            int b0 = plan.IndexOf(Begin, StringComparison.Ordinal), b1 = plan.IndexOf("<!-- R-US19 RULE END -->", StringComparison.Ordinal);
            Check(b0 >= 0 && b1 > b0, "R-US19's block found in the plan");
            string blockDigest = b0 >= 0 && b1 > b0 ? Digest(plan.Substring(b0 + Begin.Length, b1 - b0 - Begin.Length)) : "-";
            Check(blockDigest == RuleDigests[RuleDigests.Length - 1].Digest, F("the block's digest {0} is the list's last ({1})", blockDigest.Substring(0, Math.Min(8, blockDigest.Length)), RuleDigests[RuleDigests.Length - 1].Why));

            // K2 (2020 -> 2026, Class II) is BILLED: no 2026 race in the catalog; once there is, the run over D plus K2 is owed (not built)
            int lastRace = _races.Max(r => r.Year);
            Check(lastRace <= 2024, F("K2 BILLED: the catalog's Senate races end {0} - a later year needs the run over D plus K2, not built", lastRace));

            var baseCfg = new Config();
            foreach (Cycle c in Cycles)
            {
                var seats = SeatsOf(c, baseCfg);
                Check(seats.Count == c.Count, F("{0}: {1} regular full-term races of Class {2} in {3} (the block: {4})", c.Name, seats.Count, c.Class, c.T, c.Count));
                Check(seats.Select(s => s.Target.State).Distinct().Count() == seats.Count, F("{0}: one race per state of the class", c.Name));
            }

            // (a)'s residuals, and (x)'s identity at V(T) = V(P)
            foreach (int y in new[] { 2016, 2020, 2024 }) { try { Derive(y - 4, y, 0); Check(true, F("(a) {0} -> {1}: R-US14 (a)'s derivation closes on {1}'s national shares (residual below 1e-9)", y - 4, y)); } catch (Exception ex) { Check(false, ex.Message); } }
            {
                var at = Rederived(2020, 0.0, 0, false); var aP = Derive(2016, 2020, 0);
                double worst = aP.Keys.Max(st => Math.Abs(TwoParty(at[st]) - TwoParty(aP[st])));
                Check(worst < 1e-9, F("(x) returns a_s(2020) at V(2022) = V(2020): the worst difference {0:E1}", worst));
            }

            // the run over D, and the same-seat, same-day special: each method's call on it equals its call on the full term
            var cascadesSink = new List<string>();
            RunResult main = Evaluate(baseCfg, Check);
            foreach (Cycle c in Cycles)
            {
                foreach (Race sp in _races.Where(r => r.Year == c.T && r.Class == c.Class && r.Term != "full"))
                {
                    var cr = main.Cycles[c.Name];
                    int i = cr.Seats.FindIndex(s => s.Target.State == sp.State);
                    var spSeat = new Seat { Target = sp, Base = BaseOf(sp.State, sp.Class, c.T), Kind = "same seat" };
                    double xs = X(spSeat.Base, baseCfg, out _);
                    string ca = Call(AShare(sp.State, c.T, baseCfg), spSeat, baseCfg, cascadesSink, Check), cb = Call(xs + V(c.T, baseCfg) - V(spSeat.Base.Year, baseCfg), spSeat, baseCfg, cascadesSink, Check);
                    Check(i >= 0 && ca == cr.Calls[A][i] && cb == cr.Calls[B][i], F("{0}: never counted; (a) calls it {1} and (b) {2}, as each calls the full term", sp.Key, ca, cb));
                }
            }

            // the per-seat table, per cycle
            foreach (Cycle c in Cycles)
            {
                var cr = main.Cycles[c.Name];
                int aOnly = 0, bOnly = 0, both = 0;
                Both(F("{0} {1}->{2} Class {3} ({4}): V {5:0.0000} -> {6:0.0000}, swing {7:+0.0000;-0.0000}; {8} seats; the truth REP {9} DEM {10} O {11} Z {12}",
                    c.Name, c.B, c.T, c.Class, c.Presidential ? "a presidential year" : "a midterm", _vOwn[c.B], _vOwn[c.T], _vOwn[c.T] - _vOwn[c.B], cr.Seats.Count, cr.R, cr.Truth.Count(t => t == "D"), cr.O, cr.Z));
                for (int i = 0; i < cr.Seats.Count; i++)
                {
                    Seat s = cr.Seats[i];
                    double x = X(s.Base, baseCfg, out string sub);
                    // the DEM side's make-up in the base count: one candidate or several summed, and the winner where he is an independent
                    string dem = (s.Base.WinnerClass == "I" ? F("the winner an I {0}; ", WinnerSide(s.Base, baseCfg) == "D" ? "with the Democrats" : "of no side") : "")
                        + (s.Base.FinalDc == 0 ? "DEM side none" : s.Base.FinalDc == s.Base.FinalDcTop ? "DEM side one candidate" : F("DEM side summed, its strongest {0}", s.Base.FinalDcTop));
                    bool missA = cr.Truth[i] == "" || cr.Calls[A][i] != cr.Truth[i], missB = cr.Truth[i] == "" || cr.Calls[B][i] != cr.Truth[i];
                    if (missA && missB) { both++; } else if (missA) { aOnly++; } else if (missB) { bOnly++; }
                    Both(F("  {0} base {1} [{2}; R {3} DC {4}; {5}] x {6:0.0000}{7}, the change {8:+0.0000;-0.0000} | (a) {9:0.0000} {10} | (b) {11:0.0000} {12} | truth {13}{14}",
                        s.Target.State, s.Base.Key, s.Base.BaseCount, s.Base.FinalR, s.Base.FinalDc, dem, x,
                        sub.Length > 0 ? " <" + sub + ", side " + ((x > 0.5 ? "R" : "D") == WinnerSide(s.Base, baseCfg) ? "agrees" : "DISAGREES") + " with the base winner's>" : "",
                        V(c.T, baseCfg) - V(s.Base.Year, baseCfg), Math.Max(0, Math.Min(1, cr.Share[A][i])), cr.Calls[A][i], Math.Max(0, Math.Min(1, cr.Share[B][i])), cr.Calls[B][i], cr.Truth[i] == "" ? "none (O)" : cr.Truth[i],
                        missA && missB ? "  MISS both" : missA ? "  MISS a only" : missB ? "  MISS b only" : ""));
                }
                Both(F("  {0}: misses a only {1}, b only {2}, both {3}", c.Name, aOnly, bOnly, both));
                foreach (string k in cr.Cascades) { Both("  cascade: " + k); }
                Both(F("  {0}: M (a) {1}, (b) {2}; N (a) {3:+0;-0;0}, (b) {4:+0;-0;0}; misses (a) {5}; (b) {6}", c.Name, cr.M[A], cr.M[B], cr.N[A], cr.N[B],
                    cr.Misses[A].Count == 0 ? "none" : string.Join(", ", cr.Misses[A]), cr.Misses[B].Count == 0 ? "none" : string.Join(", ", cr.Misses[B])));
            }

            // the steps of the run over D
            foreach (string l in main.Lines) { Both(l); }

            // STEP 3 - (c), printed per parent
            foreach (string line in CLines(baseCfg, main)) { Both(line); }

            // STEP 6 - S8: the identity over every base of D and every play base the catalog step reads with them
            var s8 = new List<string>();
            var playBases = new List<(Race R, string Why)>();
            foreach (Cycle c in Cycles) { foreach (Seat s in main.Cycles[c.Name].Seats) { playBases.Add((s.Base, "a base of " + c.Name)); } }
            foreach (Race r in _races.Where(r => (r.Year == 2020 && r.Class == 2) || (r.Year == 2022 && r.Class == 3))) { playBases.Add((r, "a play base")); }
            var notRead = new List<string>();
            foreach (Race sp in _races.Where(r => r.Term != "full"))
            {
                Race prev = BaseOf(sp.State, sp.Class, sp.Year);
                if (prev == null) { notRead.Add(sp.Key); } else { playBases.Add((prev, "the previous race of " + sp.Key)); }
            }
            var playSubs = new List<string>();
            foreach (var g in playBases.GroupBy(p => p.R.Key).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                Race r = g.First().R;
                string side = WinnerSide(r, baseCfg);
                double x = X(r, baseCfg, out string sub);
                if (sub.Length > 0 && g.Any(p => p.Why == "a play base" || p.Why.StartsWith("the previous", StringComparison.Ordinal)))
                {
                    playSubs.Add(F("{0} x {1:0.0000} by {2}, side {3} with the base winner's", r.Key, x, sub, side == "" ? "- (the winner has none)" : (x > 0.5 ? "R" : "D") == side ? "agrees" : "DISAGREES"));
                }
                if (side == "") { continue; }
                bool ok = side == "R" ? x > 0.5 : x < 0.5;
                if (!ok) { s8.Add(r.Key); Both(F("step 6 - S8: {0} ({1}): its winner's side {2}, x {3:0.0000}{4}", r.Key, string.Join("; ", g.Select(p => p.Why).Distinct()), side, x, sub.Length > 0 ? " by " + sub : "")); }
            }
            main.S8.AddRange(s8);
            Both("the play bases' substitutions: " + (playSubs.Count == 0 ? "none" : string.Join("; ", playSubs)));
            Both(F("step 6 - S8: the identity over {0} bases (D's and the play bases read with them){1}; {2}", playBases.Select(p => p.R.Key).Distinct().Count(), s8.Count == 0 ? " holds" : " fails at " + string.Join(", ", s8),
                notRead.Count == 0 ? "every special's previous race read" : "specials whose previous race is before the catalog's window, not read: " + string.Join(", ", notRead)));

            // STEP 4 - the declared readings
            var readings = new List<(string Key, Config Cfg)>
            {
                ("(i)", new Config { Name = "(i) D = {K1}", D = new[] { "K1" } }),
                ("(ii)", new Config { Name = "(ii) the specials in the count", SpecialsIn = true }),
                ("(iii)", new Config { Name = "(iii) targets with a side absent from the general's ballot out", GenOneSidedOut = true }),
                ("(iv)", new Config { Name = "(iv) a base with no two-sided count at 1 or 0 by its winner", NoTwoSidedAtWinner = true }),
                ("(v)", new Config { Name = "(v) seats whose base has no two-sided count out", NoTwoSidedOut = true }),
                ("(vi)", new Config { Name = "(vi) each side's strongest candidate alone in every base count", TopOnly = true }),
                ("(vii)", new Config { Name = "(vii) V_dc, the swing on the deciding count - PRINTED ONLY (B7 answered, G1)", DcSwing = true, PrintedOnly = true }),
                ("(viii)", new Config { Name = "(viii) (a) by R-US14 (b), proportional - PRINTED ONLY (G2)", AMethod = 1, PrintedOnly = true }),
                ("(ix)", new Config { Name = "(ix) (a) by R-US14 (c), plain additive - PRINTED ONLY (G2)", AMethod = 2, PrintedOnly = true }),
                ("(x)", new Config { Name = "(x) (a) at a midterm re-derived", MidtermRederived = true }),
                ("(xi)", new Config { Name = "(xi) Sinema an independent of no side - PRINTED ONLY (G3)", SinemaNoSide = true, PrintedOnly = true }),
            };
            var changed = new List<string>();
            var changedFigs = new List<string>();
            var digestText = new StringBuilder("base:" + main.DigestText);
            foreach (var r in readings)
            {
                RunResult rr = Evaluate(r.Cfg, Check);
                bool two = rr.DCount >= 2;
                bool moves = main.Compared(two) != rr.Compared(two);
                string counts = string.Join(", ", rr.Cycles.Where(kv => r.Cfg.D.Contains(kv.Key)).Select(kv => F("{0} {1} seats M (a) {2} (b) {3} N (a) {4:+0;-0;0} (b) {5:+0;-0;0}", kv.Key, kv.Value.Seats.Count, kv.Value.M[A], kv.Value.M[B], kv.Value.N[A], kv.Value.N[B])));
                Both(F("reading {0}: {1} - {2}{3}", r.Cfg.Name, rr.Outcome(), counts, r.Cfg.PrintedOnly ? " (printed only - compared with nothing)" : (two ? "" : " (one cycle: compared less S2)") + (moves ? " - CHANGES THE OUTCOME" : " - the same")));
                if (!r.Cfg.PrintedOnly)
                {
                    digestText.Append(r.Key + ":" + rr.DigestText);
                    if (moves) { changed.Add(r.Key); changedFigs.Add(F("{0} gives {1} - {2}", r.Key, rr.Outcome(), rr.Figures["M"] + "; " + rr.Figures["N"])); }
                }
            }

            // the outcome, the verdict, the decisive digest
            var stops = main.StopList(true);
            if (changed.Count > 0) { stops.Add("S4 [" + string.Join(", ", changed) + "]"); }
            if (main.S7) { stops.Add("S7"); }
            if (main.S8.Count > 0) { stops.Add("S8 [" + string.Join(", ", main.S8) + "]"); }
            if (main.S9.Count > 0) { stops.Add("S9 [" + string.Join(", ", main.S9) + "]"); }
            string outcome = main.Step1 + (stops.Count > 0 ? " + " + string.Join(", ", stops) : "");
            Cycle[] dMain = Cycles.Where(c => baseCfg.D.Contains(c.Name)).ToArray();
            foreach (Cycle c in dMain)
            {
                var cr = main.Cycles[c.Name];
                digestText.Append(F("{0} misses (a) {1}; (b) {2}; O {3}; Z none;", c.Name, string.Join(",", cr.Misses[A]), string.Join(",", cr.Misses[B]),
                    string.Join(",", cr.Seats.Where((s, i) => cr.Truth[i] == "").Select(s => s.Target.State + " cl" + s.Target.Class))));
            }
            digestText.Append("S8:" + string.Join(",", main.S8) + ";S9:" + string.Join(",", main.S9) + ";K3 read:" + dMain.Any(c => c.Name == "K3"));
            string decisive = Digest(digestText.ToString());
            bool decided = (main.Step1 == "(a)" || main.Step1 == "(b)") && stops.Count == 0;
            string verdict;
            if (decided)
            {
                var discord = dMain.Select(c => { var cr = main.Cycles[c.Name]; int ab = cr.Misses[A].Except(cr.Misses[B]).Count(), ba = cr.Misses[B].Except(cr.Misses[A]).Count(); return (c.Name, ab, ba); }).ToList();
                int pab = discord.Sum(x => x.ab), pba = discord.Sum(x => x.ba);
                string P(int ab, int ba) { double p = SignTest(ab, ba); return F("{0}:{1} p {2:0.000}{3}", ab, ba, p, p > 0.05 ? " (within the record's noise)" : ""); }
                verdict = F("By R-US19's rule, code recommends {0}: {1}; {2}; summed M (a) {3}, (b) {4}; discordant d_ab:d_ba {5}; pooled {6}; the same under every declared reading; (c) is in-sample by construction.",
                    main.Step1, main.Figures["M"], main.Figures["Nsigned"] + F("; summed N (a) {0:+0;-0;0}, (b) {1:+0;-0;0}", dMain.Sum(c => main.Cycles[c.Name].N[A]), dMain.Sum(c => main.Cycles[c.Name].N[B])),
                    dMain.Sum(c => main.Cycles[c.Name].M[A]), dMain.Sum(c => main.Cycles[c.Name].M[B]),
                    string.Join("; ", discord.Select(x => x.Name + " " + P(x.ab, x.ba))), P(pab, pba));
            }
            else
            {
                var figs = new List<string>();
                if (main.Step1 == "S1") { figs.Add("S1 (" + main.Figures["M"] + (main.Listed.Count > 0 ? "; listed under S1: " + string.Join("; ", main.Listed) : "") + ")"); }
                if (main.Step1 == "TIE") { figs.Add("TIE (" + main.Figures["M"] + "; " + main.Figures["N"] + (main.Listed.Count > 0 ? "; listed: " + string.Join("; ", main.Listed) : "") + ") - put to Elias with the figures (B2, G1)"); }
                if (main.S2) { figs.Add("S2 (" + main.Figures["S2"] + ")"); }
                if (changed.Count > 0) { figs.Add(F("S4 ({0}; the run over D gives {1})", string.Join("; ", changedFigs), main.Step1 + (main.S2 ? " + S2" : ""))); }
                if (main.S7) { figs.Add("S7 (" + main.Figures["S7"] + ")"); }
                if (main.S8.Count > 0) { figs.Add("S8 (" + string.Join(", ", main.S8) + ")"); }
                if (main.S9.Count > 0) { figs.Add("S9 (" + string.Join(", ", main.S9) + ")"); }
                verdict = F("R-US19's rule does not decide: {0} - asked.", string.Join("; ", figs));
            }
            Both("THE OUTCOME (the run over D): " + outcome);
            Both("THE VERDICT: " + verdict);
            Both(F("the decisive digest: {0}", decisive));

            // PRINTED, never deciding
            foreach (string line in Printed(baseCfg, main)) { Both(line); }

            // the ruling record, held by this instrument and mirrored in the plan's line after the block
            if (RulingState == "NONE") { Check(false, "the ruling record is NONE - the first run registers the ask (R-US19's block: NONE fails every run until its ask is registered)"); }
            if (RulingState == "ASKED")
            {
                Check(RulingOutcomeOverD == outcome, F("ASKED: the outcome {0} is the registered one ({1})", outcome, RulingOutcomeOverD));
                Check(RulingDecisiveDigest == decisive, F("ASKED: the decisive digest {0} is the registered one ({1}) - a moved digest is registered again and Elias told", decisive.Substring(0, 8), RulingDecisiveDigest.Length >= 8 ? RulingDecisiveDigest.Substring(0, 8) : RulingDecisiveDigest));
            }
            if (RulingState == "RULED")
            {
                Check(Array.IndexOf(BuildableOptions, RulingOption) >= 0, F("RULED: the ruling names an option that can be built ('{0}')", RulingOption));
                Check(RulingOutcomeOverD == outcome, F("RULED: the outcome {0} is the ruling's ({1}) - a changed outcome asks R-US19 again", outcome, RulingOutcomeOverD));
                Check(RulingDecisiveDigest == decisive, F("RULED: the decisive digest {0} is the one the ruling was made on ({1}) - with the outcome unchanged, a commit re-pins it naming the old, the new and \"outcome unchanged\"", decisive.Substring(0, 8), RulingDecisiveDigest.Length >= 8 ? RulingDecisiveDigest.Substring(0, 8) : RulingDecisiveDigest));
            }
            string mirror = RulingState == "RULED" ? F("**Ruling record:** RULED ({0}): {1}.", RulingSection, RulingRuled) : F("**Ruling record:** {0} ({1}): {2}.", RulingState, RulingSection, RulingOutcomeOverD);
            int atLine = b1 >= 0 ? plan.IndexOf("**Ruling record:**", b1, StringComparison.Ordinal) : -1;
            string planLine = atLine >= 0 ? plan.Substring(atLine, plan.IndexOf('\n', atLine) - atLine) : "-";
            Check(planLine == mirror, F("the plan's line after the block mirrors the record: '{0}'", planLine));
            return Digest(UsPresidentialReturns.SenateRaceSourceDigest + UsPresidentialReturns.SenateSeatSourceDigest + UsPresidentialReturns.SenateOnSourceDigest + UsPresidentialReturns.HouseSourceDigest + UsPresidentialReturns.HouseDistrictSourceDigest
                + UsPresidentialReturns.StateSourceDigest + UsPresidentialReturns.YearSourceDigest + blockDigest + decisive + RulingState);
        }

        // ------------------------------------------------------------------ (c), printed per parent (STEP 3)
        private static IEnumerable<string> CLines(Config cfg, RunResult main)
        {
            const double Lo = 1e-9, Hi = 1 - 1e-9;
            double Clamp(double v) => Math.Max(Lo, Math.Min(Hi, v));
            double Logit(double v) => Math.Log(v / (1 - v));
            var fit = _races.Where(r => r.Year == 2024 && ((r.Class == 1) || (r.State == "NE" && r.Class == 2))).OrderBy(r => r.Class).ThenBy(r => r.State, StringComparer.Ordinal).ThenBy(r => r.Term, StringComparer.Ordinal).ToList();
            var cr = main.Cycles["K1"];
            for (int m = 0; m < 2; m++)
            {
                var cs = new List<(string Key, double C)>();
                var notFitted = new List<string>();
                var reversed = new List<string>();
                int cRep = 0, cMiss = 0;
                foreach (Race r in fit)
                {
                    var seat = new Seat { Target = r, Base = BaseOf(r.State, r.Class, 2024) };
                    double p = m == A ? AShare(r.State, 2024, cfg) : X(seat.Base, cfg, out _) + V(2024, cfg) - V(seat.Base.Year, cfg);
                    long fr = r.FinalR, fd = r.FinalDc;
                    string declared = "";
                    if (fr == 0 && r.FinalNoneTop > 0) { fr = r.FinalNoneTop; declared = " (final_none_top stands for REP)"; }
                    else if (fd == 0 && r.FinalNoneTop > 0) { fd = r.FinalNoneTop; declared = " (final_none_top stands for DEM)"; }
                    bool regular = r.Class == 1 && r.Term == "full";
                    double sOut = p;
                    if (fr == 0 || fd == 0) { notFitted.Add(r.Key); }
                    else
                    {
                        double s = (double)fr / (fr + fd);
                        double c = Logit(Clamp(s)) - Logit(Clamp(p));
                        cs.Add((r.Key + declared, c));
                        sOut = 1.0 / (1.0 + Math.Exp(-(Logit(Clamp(p)) + c)));
                        if ((p > 0.5) != (sOut > 0.5)) { reversed.Add(r.Key); }
                    }
                    if (regular)
                    {
                        int i = cr.Seats.FindIndex(x => x.Target == r);
                        string call = sOut > 0.5 ? "R" : sOut < 0.5 ? "D" : cr.Calls[m][i];
                        if (call == "R") { cRep++; }
                        if (cr.Truth[i] == "" || call != cr.Truth[i]) { cMiss++; }
                    }
                }
                int nC = SignedDist(cRep, cr.R, cr.R + cr.Z + cr.O);
                var abs = cs.Select(x => Math.Abs(x.C)).ToList();
                yield return F("step 3 - (c) over {0} on K1, in-sample; zero by construction except O seats, races not fitted, and races whose base-count share and deciding-count winner disagree: M {1}, N {2:+0;-0;0}; |c_j| over {3} fitted races mean {4:0.000}, largest {5:0.000}, RMS {6:0.000}; not fitted: {7}; the races whose call c_j reverses: {8}",
                    MethodNames[m], cMiss, nC, cs.Count, abs.Count > 0 ? abs.Average() : 0, abs.Count > 0 ? abs.Max() : 0, abs.Count > 0 ? Math.Sqrt(abs.Average(v => v * v)) : 0,
                    notFitted.Count == 0 ? "none" : string.Join(", ", notFitted), reversed.Count == 0 ? "none" : string.Join(", ", reversed));
                yield return "  c_j: " + string.Join("; ", cs.Select(x => F("{0} {1:+0.000;-0.000}", x.Key, x.C)));
            }
        }

        // ------------------------------------------------------------------ PRINTED, never deciding
        private static IEnumerable<string> Printed(Config cfg, RunResult main)
        {
            // control per method: the class's calls with the holdovers on the next 3 January
            foreach (Cycle c in Cycles)
            {
                var cr = main.Cycles[c.Name];
                string jan = F("{0}-01-03", c.T + 1);
                int holdR = 0, holdD = 0, holdNone = 0; var vacant = new List<string>();
                foreach (var g in UsPresidentialReturns.SenateSeats.Select(s => (s.State, s.Class)).Distinct().Where(k => k.Class != c.Class).OrderBy(k => k.State, StringComparer.Ordinal).ThenBy(k => k.Class))
                {
                    var h = HolderAt(g.State, g.Class, jan, cfg);
                    if (h.Senator == null) { vacant.Add(g.State + " cl" + g.Class); } else if (h.Side == "R") { holdR++; } else if (h.Side == "D") { holdD++; } else { holdNone++; }
                }
                var on = UsPresidentialReturns.SenateOn.Last(x => string.CompareOrdinal(x.Day, jan) <= 0);
                int congress = (c.T + 1 - 1789) / 2 + 1;
                var div = UsPresidentialReturns.SenateDivision.FirstOrDefault(x => x.Congress == congress);
                string senDiv = div.HoldsFrom == null ? F("[SEN-DIV] has no line for the {0}th", congress) : F("[SEN-DIV] the {0}th D {1} R {2} I {3} (from {4})", congress, div.D, div.R, div.I, div.HoldsFrom);
                for (int m = 0; m < 2; m++)
                {
                    int rep = cr.CalledRep[m] + holdR, dem = cr.Seats.Count - cr.CalledRep[m] + holdD;
                    yield return F("control {0} {1} on {2}: REP caucus {3}, DEM caucus {4}{5}{6} - {7}; cloture's 60 {8}; against {9}; the roster that day REP {10} DEM {11} ({12})",
                        c.Name, MethodNames[m], jan, rep, dem, holdNone > 0 ? F(", no side {0}", holdNone) : "", vacant.Count > 0 ? ", vacant " + string.Join(" ", vacant) : "",
                        rep >= 51 ? "REP holds 51" : dem >= 51 ? "DEM holds 51" : "50-50: the Vice President's vote (the Vice President's side is not in a catalog)", rep >= 60 ? "REP" : dem >= 60 ? "DEM" : "neither", senDiv, on.RepCaucus, on.DemCaucus, on.What);
                }
            }

            // the mean absolute two-party miss, labelled not neutral; no change
            foreach (Cycle c in Cycles)
            {
                var cr = main.Cycles[c.Name];
                var two = Enumerable.Range(0, cr.Seats.Count).Where(i => cr.Seats[i].Target.TwoSided).ToList();
                double Mam(int m) => two.Average(i => Math.Abs(cr.Share[m][i] - (double)cr.Seats[i].Target.FinalR / (cr.Seats[i].Target.FinalR + cr.Seats[i].Target.FinalDc)));
                int ncMiss = 0, ncRep = 0;
                for (int i = 0; i < cr.Seats.Count; i++) { string w = WinnerSide(cr.Seats[i].Base, cfg); if (w == "R") { ncRep++; } if (cr.Truth[i] == "" || w != cr.Truth[i]) { ncMiss++; } }
                yield return F("{0}: the mean absolute two-party miss over {1} two-sided targets (not neutral - each method's own scale) (a) {2:0.00} pp, (b) {3:0.00} pp; no change M {4}, N {5:+0;-0;0}",
                    c.Name, two.Count, 100 * Mam(A), 100 * Mam(B), ncMiss, SignedDist(ncRep, cr.R, cr.R + cr.Z + cr.O));
            }

            // the variants
            yield return Variant("K1", "(a-record), the state's own presidential share of record at 2024", main, (s, i) => { var p = UsPresidentialReturns.States.First(r => r.Year == 2024 && r.State == s.Target.State); return (double)p.VotesR / (p.VotesR + p.VotesD); }, cfg);
            yield return "K3: (a-record) - no presidential election at 2022";
            {
                double[] nat24 = National(UsPresidentialReturns.States.Where(s => s.Year == 2024).ToArray());
                double o = nat24[2], v = V(2024, cfg);
                var atV = Derive(2020, 2024, 0, new[] { (1 - o) * (1 - v), (1 - o) * v, o });
                yield return Variant("K1", "(a) at the House V (PROVISIONAL: 2024's national REP two-party share set to V(2024), its others as recorded)", main, (s, i) => TwoParty(atV[s.Target.State]), cfg);
            }
            {
                var a16 = Derive(2012, 2016, 0);
                var seats18 = _races.Where(r => r.Year == 2018 && r.Class == 1 && r.Term == "full").OrderBy(r => r.State, StringComparer.Ordinal).ToList();
                int miss = 0, rep = 0, rr = 0, o = 0;
                foreach (Race r in seats18)
                {
                    double sh = TwoParty(a16[r.State]) + V(2018, cfg) - V(2016, cfg);
                    string call = sh > 0.5 ? "R" : "D", t = WinnerSide(r, cfg);
                    if (call == "R") { rep++; } if (t == "R") { rr++; } if (t == "") { o++; } if (t == "" || call != t) { miss++; }
                }
                yield return F("(a) at a midterm on 2018's Class I ({0} races) from 2016's derivation: M {1}, N {2:+0;-0;0}", seats18.Count, miss, SignedDist(rep, rr, rr + o));
            }
            {
                var var20 = Rederived(2020, V(2022, cfg) - V(2020, cfg), 0, true);
                yield return Variant("K3", "(x)'s variant from 2020's record rows", main, (s, i) => TwoParty(var20[s.Target.State]), cfg);
            }
            yield return Variant("K1", "(b) proportional (= logit)", main, (s, i) => Logistic(Logit(X(s.Base, cfg, out _)) + Logit(V(2024, cfg)) - Logit(V(s.Base.Year, cfg))), cfg);
            yield return Variant("K3", "(b) proportional (= logit)", main, (s, i) => Logistic(Logit(X(s.Base, cfg, out _)) + Logit(V(2022, cfg)) - Logit(V(s.Base.Year, cfg))), cfg);
            yield return Variant("K3", "the hybrid-midterm row - an illustration; not measured as play runs it (each seat's (a) share at its base year plus V(2022) - V(base year))", main,
                (s, i) => TwoParty(Derive(s.Base.Year - 4, s.Base.Year, 0)[s.Target.State]) + V(2022, cfg) - V(s.Base.Year, cfg), cfg);

            // the holder on 3 Jan 2025 beside K1's truth
            {
                var cr = main.Cycles["K1"]; var diff = new List<string>();
                for (int i = 0; i < cr.Seats.Count; i++)
                {
                    var h = HolderAt(cr.Seats[i].Target.State, 1, "2025-01-03", cfg);
                    if (h.Side != cr.Truth[i]) { diff.Add(F("{0}: the truth {1}, the holder on 3 Jan 2025 {2}", cr.Seats[i].Target.State, cr.Truth[i], h.Senator == null ? "none (vacant)" : h.Senator + " " + h.Side)); }
                }
                yield return "K1: the holder on 3 Jan 2025 beside the truth, not a reading - differs at " + (diff.Count == 0 ? "no seat" : string.Join("; ", diff));
            }

            // the game's range per method: the REP-caucus Senate at V from 40 to 60 percent
            foreach (string line in Range(cfg)) { yield return line; }

            // the exceptions named
            var ex = new List<string>();
            foreach (Cycle c in Cycles)
            {
                foreach (Seat s in main.Cycles[c.Name].Seats)
                {
                    foreach (Race r in new[] { s.Target, s.Base })
                    {
                        string f = r.Flags == "-" ? "" : r.Flags;
                        var why = new List<string>();
                        if (f.Contains("F")) { why.Add("fusion"); } if (f.Contains("M")) { why.Add("a runoff"); } if (f.Contains("R")) { why.Add("a ranked count"); }
                        if (f.Contains("S")) { why.Add("two of one party"); } if (f.Contains("U")) { why.Add("an unnamed entry"); } if (f.Contains("N")) { why.Add("no Democrat"); }
                        if (r.WinnerClass == "I") { why.Add("an independent winner"); }
                        if (!r.TwoSided) { why.Add(r == s.Base ? "no two-sided count - a fallback" : "no two-sided count"); }
                        if (why.Count > 0) { string e = r.Key + ": " + string.Join(", ", why); if (!ex.Contains(e)) { ex.Add(e); } }
                    }
                }
            }
            // the specials of D's years, printed beside them (Mississippi's marks among them)
            foreach (Race r in _races.Where(r => r.Term != "full" && (r.Year == 2016 || r.Year == 2018 || r.Year == 2022 || r.Year == 2024)))
            {
                string f = r.Flags == "-" ? "" : r.Flags;
                var why = new List<string> { "a special" };
                if (f.Contains("F")) { why.Add("fusion"); } if (f.Contains("M")) { why.Add("a runoff, its finalists' marks read off"); } if (f.Contains("R")) { why.Add("a ranked count"); }
                if (f.Contains("S")) { why.Add("two of one party"); } if (f.Contains("N")) { why.Add("no Democrat"); }
                string e = r.Key + ": " + string.Join(", ", why);
                if (!ex.Contains(e)) { ex.Add(e); }
            }
            yield return "the exceptions named: " + string.Join("; ", ex.OrderBy(e => e, StringComparer.Ordinal)) + "; California's Class I contest of 2024 never counted (one seat, two contests); West Virginia's Class I oath after 3 Jan 2025 (the holder line); Ohio 2018's FEC difference (senate_record.md)";
        }

        private static string Variant(string cycle, string name, RunResult main, Func<Seat, int, double> share, Config cfg)
        {
            var cr = main.Cycles[cycle];
            int miss = 0, rep = 0;
            for (int i = 0; i < cr.Seats.Count; i++)
            {
                double sh = share(cr.Seats[i], i);
                string call = sh > 0.5 ? "R" : sh < 0.5 ? "D" : cr.Calls[A][i];
                if (call == "R") { rep++; }
                if (cr.Truth[i] == "" || call != cr.Truth[i]) { miss++; }
            }
            return F("{0}: {1}: M {2}, N {3:+0;-0;0}", cycle, name, miss, SignedDist(rep, cr.R, cr.R + cr.Z + cr.O));
        }

        /// <summary>The game's range (PROVISIONAL construct, G5): the REP-caucus Senate after 5 Nov 2024 (Class I called, NE's Class II special with it)
        /// and 3 Nov 2026 (Class II called on its bases, and each other seat held by an appointee sworn after the 2024 election called as a 2026
        /// special on its latest race - Ohio's and Florida's Class III) at V from 40 to 60 percent; (a) at 2024
        /// from the national REP two-party share set to V; the holdovers by the latest roster row of each other seat.</summary>
        private static IEnumerable<string> Range(Config cfg)
        {
            double[] nat24 = National(UsPresidentialReturns.States.Where(s => s.Year == 2024).ToArray());
            var fields = new[] { (Day: "2024-11-05", Year: 2024, Called: (Func<Race, bool>)(r => r.Year == 2024 && (r.Class == 1 && r.Term == "full" || (r.Class == 2 && r.Term != "full")))),
                                 (Day: "2026-11-03", Year: 2026, Called: (Func<Race, bool>)(r => false)) };
            foreach (var f in fields)
            {
                List<(string State, int Class, Race Base)> called = f.Year == 2024
                    ? _races.Where(f.Called).Select(r => (r.State, r.Class, BaseOf(r.State, r.Class, 2024))).ToList()
                    : UsPresidentialReturns.SenateSeats.Where(s => s.Class == 2).Select(s => s.State).Distinct().OrderBy(s => s, StringComparer.Ordinal).Select(st => (st, 2, BaseOf(st, 2, 2026)))
                        .Concat(UsPresidentialReturns.SenateSeats.Where(s => s.Class != 2 && s.HowIn == "appointed" && s.Left == "-" && string.CompareOrdinal(s.Took, "2024-11-05") > 0
                                && !_races.Any(r => r.State == s.State && r.Class == s.Class && r.Year == 2024)).OrderBy(s => s.State, StringComparer.Ordinal).Select(s => (s.State, s.Class, BaseOf(s.State, s.Class, 2026)))).ToList();
                var calledKeys = new HashSet<(string, int)>(called.Select(x => (x.State, x.Class)));
                int holdR = 0, holdD = 0;
                foreach (var k in UsPresidentialReturns.SenateSeats.Select(s => (s.State, s.Class)).Distinct())
                {
                    if (calledKeys.Contains(k)) { continue; }
                    var rows = UsPresidentialReturns.SenateSeats.Where(s => s.State == k.State && s.Class == k.Class && string.CompareOrdinal(s.Took, f.Day) <= 0).ToList();
                    if (rows.Count == 0) { continue; }
                    var last = rows.OrderBy(s => s.Took, StringComparer.Ordinal).Last();
                    string side = last.Party == "R" ? "R" : last.Party == "D" ? "D" : CaucusSide(last.Caucus);
                    if (side == "R") { holdR++; } else if (side == "D") { holdD++; }
                }
                for (int m = 0; m < 2; m++)
                {
                    var sb = new StringBuilder();
                    for (int pct = 40; pct <= 60; pct += 2)
                    {
                        double v = pct / 100.0;
                        Dictionary<string, double[]> atV = null;
                        if (m == A && f.Year == 2024) { double o = nat24[2]; atV = Derive(2020, 2024, 0, new[] { (1 - o) * (1 - v), (1 - o) * v, o }); }
                        int rep = holdR;
                        foreach (var s in called)
                        {
                            double sh = m == A
                                ? (f.Year == 2024 ? TwoParty(atV[s.State]) : TwoParty(Derive(2020, 2024, 0)[s.State]) + v - V(2024, cfg))
                                : X(s.Base, cfg, out _) + v - V(s.Base.Year, cfg);
                            if (sh > 0.5) { rep++; }
                        }
                        sb.Append(F("{0}% {1}{2}", pct, rep, pct < 60 ? ", " : ""));
                    }
                    yield return F("the game's range {0} {1} ({2} seats called{3}, holdovers REP {4} DEM {5}; PROVISIONAL): the REP caucus at V = {6}", f.Day, MethodNames[m], called.Count,
                        called.Any(x => f.Year == 2026 && x.Class != 2) ? " - with the specials " + string.Join(", ", called.Where(x => x.Class != 2).Select(x => x.State + " cl" + x.Class)) : "", holdR, holdD, sb);
                }
            }
        }

        private static double Logit(double v) => Math.Log(v / (1 - v));
        private static double Logistic(double v) => 1.0 / (1.0 + Math.Exp(-v));

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsSenateRaceCheck (PS-6 US-12, §801): R-US19's rule on the Senate of record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(CardPath(), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no Senate block", CardRelative)
                    : onDisk == expected ? F("the model card's Senate block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's Senate block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsSenateRaceCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== UsSenateRaceCheck: the reading holds ===" : F("=== UsSenateRaceCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's Senate block from this reading - refused while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsSenateRaceCheck.WriteReadings (§801): the US model card's Senate block ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = CardPath();
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no Senate block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's Senate block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }
            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ the card
        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsSenateRaceCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";
        private static string CardPath() => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), CardRelative);
        private static string Digest(string text) { using (var sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); } }
        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n```text\n");
            foreach (string l in lines) { block.Append(l).Append('\n'); }
            return block.Append("```\n").Append(CardEnd).ToString();
        }
        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
