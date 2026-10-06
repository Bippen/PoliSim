using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-11 (`COMPLETED.md` §800): THE HOUSE COUNT PROOF - R-US18's instrument. It runs R-US18's rule, the fenced block in
    /// `docs/specs/USA_STAGE_PLAN.md` (preregistered §791, amended §797 by Elias's ruling G1), on the House's record of 2016-2024: (a) district uniform
    /// swing with its fallbacks F-old and F-prop, (b) the anchored state swing and its companions (b0), (b U-blind) and the strawman (b'), (c) the
    /// anchored bilogit, and no change - each scored against the record's own district plurality (W, G, H, N, K, control, the contingent call), the
    /// seven steps, the declared readings (S4; reading (vi) printed only, G1), the outcome and the verdict line. Every input is the generated catalog
    /// (<see cref="UsPresidentialReturns"/>: the districts, the maps with their lines in force, the House's own-line votes by state, [HH-DIV]).
    ///
    /// <para>Pinned in the cheap bar and the documents bar: the card's third GENERATED block (`docs/reference/US_ELECTIONS.md`) is what
    /// <see cref="WriteReadings"/> writes; a stale block fails. The instrument holds the block's digest list (the preregistered one, then one per
    /// amendment) and the ruling record; the line after the block in the plan must mirror the record's state, section and ruling. An ask is
    /// registered, not failed: under ASKED the bars pass while the decisive digest and the outcome equal the registered ones.</para>
    /// </summary>
    public static class UsHouseCountCheck
    {
        // ------------------------------------------------------------------ the block's digests and the ruling record (R-US18's THE ASK AND THE BARS)
        /// <summary>The block's SHA-256 digests, preregistered first, then one per amendment naming Elias's ruling; the block must match the last.</summary>
        private static readonly (string Digest, string Why)[] RuleDigests =
        {
            ("b827a7f234c2a34933c0e92d506beaa190f16e7655e6b044682f7fe73280152c", "preregistered (§791)"),
            ("5c89b0cf400e50c0e64e687f3f05b8cb74f13f76384bca184773f3724673307a", "AMENDED (G1, §797): choice 4 answered - reading (vi) printed only"),
        };

        /// <summary>The ruling record: NONE, ASKED or RULED. The first run registers the ask (its section, the outcome of each run asked, the
        /// decisive digest).</summary>
        private const string RulingState = "ASKED";
        private const string RulingSection = "§800";
        private const string RulingOutcomeOverD = "(a) + S2, S5, S4 [(ii)]";
        private const string RulingDecisiveDigest = "2df09e4c3986bda5b6030d0927929cedc273b8ba8cb3771fd405c7aa9af5d513";
        /// <summary>RULED only: the ruling's words and the option it names - one the block's THE ASK AND THE BARS says can be built.</summary>
        private const string RulingRuled = "", RulingOption = "";
        private static readonly string[] BuildableOptions =
        {
            "(a) with F-old", "(a) with F-prop", "(b)", "(b) as (b')'s curve", "(c) reconciled to (a) with F-old", "(c) reconciled to (a) with F-prop", "(c) reconciled to (b)",
        };

        // ------------------------------------------------------------------ the cycles
        private static readonly int[] Years = { 2016, 2018, 2020, 2022, 2024 };
        private sealed class Cycle { public string Name; public int B; public int T; public string Role; }   // Role: swing, level, printed
        private static readonly Cycle[] Cycles =
        {
            new Cycle { Name = "C1", B = 2016, T = 2018, Role = "swing" },
            new Cycle { Name = "C2", B = 2018, T = 2020, Role = "swing" },
            new Cycle { Name = "C3", B = 2020, T = 2022, Role = "printed" },
            new Cycle { Name = "C4", B = 2022, T = 2024, Role = "level" },
        };

        // ------------------------------------------------------------------ the rows a cycle measures
        private static readonly string[] RowNames = { "(a) F-old", "(a) F-prop", "(b)", "(b0)", "(b U-blind)", "(b')", "(c)", "no change" };
        private const int A_FOLD = 0, A_FPROP = 1, B_ANCH = 2, B0 = 3, B_UBLIND = 4, B_PRIME = 5, C_BILOGIT = 6, NO_CHANGE = 7;

        /// <summary>A run's configuration: the declared readings move these, one at a time.</summary>
        private sealed class Config
        {
            public string Name = "the run over D";
            public string[] D = { "C1", "C2", "C4" };
            public bool Truth2020Published;        // (i)
            public bool MinnesotaHeld2018;         // (iii), (v)
            public bool ColoradoHeld2018;          // (iv), (v)
            public bool DecidingCountSwing;        // (vi), printed only (G1)
            public bool LevelOutOfSums;            // (vii)
            public bool PrintedOnly;
        }

        private sealed class District { public string State; public int No; public long FinalR, FinalD; public string Winner, Flags; }

        /// <summary>One row's count for one cycle: REP seats per state (null for (c), which has no states), the national REP seats.</summary>
        private sealed class RowCount { public Dictionary<string, int> Rep; public int National; public bool Fitted = true; public string Note = ""; public List<string> FoldStates = new List<string>(); }

        /// <summary>A cycle's scores for one row.</summary>
        private sealed class Score { public int Rep, N, AbsN, G, H, K; public bool Control; public string Contingent; public bool HasStates; public bool Fitted = true; public Dictionary<string, int> W = new Dictionary<string, int>(); }

        private sealed class Truth { public Dictionary<string, int> R = new Dictionary<string, int>(), D = new Dictionary<string, int>(), Z = new Dictionary<string, int>(), O = new Dictionary<string, int>(); public int Rn, Dn, Zn, On; }

        // ------------------------------------------------------------------ the catalog, read once
        private static Dictionary<int, Dictionary<string, List<District>>> _districts;
        private static Dictionary<(int, string), (int Seats, bool InForce, string Instrument)> _maps;
        private static Dictionary<int, double> _vOwn, _vDc;
        private static string[] _states;

        private static void Load()
        {
            if (_districts != null) { return; }
            _districts = new Dictionary<int, Dictionary<string, List<District>>>();
            foreach (var r in UsPresidentialReturns.HouseDistricts)
            {
                if (!_districts.TryGetValue(r.Year, out var byState)) { _districts[r.Year] = byState = new Dictionary<string, List<District>>(StringComparer.Ordinal); }
                if (!byState.TryGetValue(r.State, out var list)) { byState[r.State] = list = new List<District>(); }
                list.Add(new District { State = r.State, No = r.District, FinalR = r.FinalR, FinalD = r.FinalD, Winner = r.Winner, Flags = r.Flags });
            }
            foreach (var byState in _districts.Values) { foreach (var list in byState.Values) { list.Sort((x, y) => x.No.CompareTo(y.No)); } }
            _maps = new Dictionary<(int, string), (int, bool, string)>();
            foreach (var m in UsPresidentialReturns.HouseMaps) { _maps[(m.Year, m.State)] = (m.Seats, m.InForce, m.Instrument); }
            _states = UsPresidentialReturns.HouseMaps.Where(m => m.Year == 2016).Select(m => m.State).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            _vOwn = new Dictionary<int, double>();
            _vDc = new Dictionary<int, double>();
            foreach (int y in Years)
            {
                long r = 0, d = 0;
                foreach (var h in UsPresidentialReturns.House) { if (h.Year == y) { r += h.VotesR; d += h.VotesD; } }
                _vOwn[y] = (double)r / (r + d);
                long fr = 0, fd = 0;
                foreach (var list in _districts[y].Values) { foreach (District x in list) { fr += x.FinalR; fd += x.FinalD; } }
                _vDc[y] = (double)fr / (fr + fd);
            }
        }

        private static double V(int year, Config cfg) => cfg.DecidingCountSwing ? _vDc[year] : _vOwn[year];

        // ------------------------------------------------------------------ the terms
        private static bool Held(Cycle c, string st, Config cfg)
        {
            if (c.B == 2016 && c.T == 2018 && ((st == "MN" && cfg.MinnesotaHeld2018) || (st == "CO" && cfg.ColoradoHeld2018))) { return true; }
            if (_maps[(c.B, st)].Seats != _maps[(c.T, st)].Seats) { return false; }
            foreach (int y in Years) { if (y > c.B && y <= c.T && _maps[(y, st)].InForce) { return false; } }
            return true;
        }

        /// <summary>A base district's share d: none (N, no winner and no votes); 1 or 0 by its winner if unopposed with no votes (U); 1 or 0 where
        /// exactly one of the deciding count's figures is zero; else final_r / (final_r + final_d).</summary>
        private static double? Share(District x)
        {
            if (x.Flags.Contains("U")) { return x.Winner == "R" ? 1.0 : 0.0; }
            if (x.FinalR == 0 && x.FinalD == 0) { return null; }
            if (x.FinalD == 0) { return 1.0; }
            if (x.FinalR == 0) { return 0.0; }
            return (double)x.FinalR / (x.FinalR + x.FinalD);
        }

        private static (int R, int D) DecidedSeats(IEnumerable<District> list) { int r = 0, d = 0; foreach (District x in list) { if (x.Winner == "R") { r++; } else if (x.Winner == "D") { d++; } } return (r, d); }

        /// <summary>The half rule: a half goes to the party that held more of the state's base seats, then to the national base majority.</summary>
        private static bool HalfToRep(int baseYear, string st)
        {
            (int r, int d) = DecidedSeats(_districts[baseYear][st]);
            if (r != d) { return r > d; }
            int nr = 0, nd = 0;
            foreach (var list in _districts[baseYear].Values) { (int a, int b) = DecidedSeats(list); nr += a; nd += b; }
            return nr > nd;
        }

        /// <summary>P(c, x): the nearest integer to c times x clamped to [0, 1], a half going by the half rule.</summary>
        private static int P(int c, double x, bool halfToRep)
        {
            double v = c * Math.Min(1.0, Math.Max(0.0, x));
            double f = Math.Floor(v);
            double frac = v - f;
            if (frac > 0.5) { return (int)f + 1; }
            if (frac < 0.5) { return (int)f; }
            return halfToRep ? (int)f + 1 : (int)f;
        }

        private static int RoundHalf(double v, bool halfUp) { double f = Math.Floor(v), frac = v - f; return frac > 0.5 ? (int)f + 1 : frac < 0.5 ? (int)f : (halfUp ? (int)f + 1 : (int)f); }

        // ------------------------------------------------------------------ (a): district uniform swing, F-old, F-prop
        private static int FOld(List<(double X, string Winner)> swung, int n, bool halfToRep)
        {
            var xs = swung.OrderBy(t => t.X).ToList();
            int m = xs.Count;
            if (m == 0) { throw new InvalidOperationException("F-old over no shares (M = 0)"); }
            int rep = 0;
            if (m == n)
            {
                foreach (var t in xs) { if (t.X > 0.5 || (t.X == 0.5 && t.Winner == "R")) { rep++; } }
                return rep;
            }
            for (int k = 1; k <= n; k++)
            {
                double p = (k - 0.5) / n, q;
                double p1 = 0.5 / m, pm = (m - 0.5) / m;
                if (p <= p1) { q = xs[0].X; }
                else if (p >= pm) { q = xs[m - 1].X; }
                else
                {
                    double pos = p * m - 0.5;   // between points j and j+1 (0-based)
                    int j = (int)Math.Floor(pos);
                    double w = pos - j;
                    q = xs[j].X + w * (xs[j + 1].X - xs[j].X);
                }
                if (q > 0.5 || (q == 0.5 && halfToRep)) { rep++; }
            }
            return rep;
        }

        private static RowCount CountA(Cycle c, Config cfg, bool fprop)
        {
            double delta = V(c.T, cfg) - V(c.B, cfg);
            var rc = new RowCount { Rep = new Dictionary<string, int>(StringComparer.Ordinal) };
            foreach (string st in _states)
            {
                List<District> baseList = _districts[c.B][st];
                int nT = _maps[(c.T, st)].Seats;
                bool held = Held(c, st, cfg);
                var shares = baseList.Select(x => (S: Share(x), x.Winner)).ToList();
                if (held && shares.All(s => s.S.HasValue))
                {
                    int rep = 0;
                    foreach (var s in shares) { double dh = s.S.Value + delta; if (dh > 0.5 || (dh == 0.5 && s.Winner == "R")) { rep++; } }
                    rc.Rep[st] = rep;
                }
                else if (!held && fprop) { rc.Rep[st] = CountBState(c, st, cfg, "b0", 0, 0); rc.FoldStates.Add(st + " (F-prop)"); }
                else
                {
                    var swung = shares.Where(s => s.S.HasValue).Select(s => (s.S.Value + delta, s.Winner)).ToList();
                    rc.Rep[st] = FOld(swung, nT, HalfToRep(c.B, st));
                    if (swung.Count != nT || !held) { rc.FoldStates.Add(st + (swung.Count != nT ? F(" (F-old, M {0}, n {1})", swung.Count, nT) : " (F-old)")); }
                }
            }
            rc.National = rc.Rep.Values.Sum();
            return rc;
        }

        // ------------------------------------------------------------------ (b): the anchored state swing and its companions
        private sealed class StateBase { public double U; public int UR, UD, MB, RB, CB, CT, NT; public string Winner1; public bool HasVotes; }

        private static StateBase BaseOf(Cycle c, string st, bool uBlind)
        {
            List<District> list = _districts[c.B][st];
            var sb = new StateBase { NT = _maps[(c.T, st)].Seats };
            long fr = 0, fd = 0;
            foreach (District x in list)
            {
                bool u = x.Flags.Contains("U");
                if (u) { if (x.Winner == "R") { sb.UR++; } else if (x.Winner == "D") { sb.UD++; } }
                if (x.Winner == "R" || x.Winner == "D") { sb.MB++; }
                if (x.Winner == "R") { sb.RB++; }
                if (!u && x.FinalR + x.FinalD > 0) { fr += x.FinalR; fd += x.FinalD; sb.HasVotes = true; }
            }
            sb.CB = uBlind ? sb.MB : sb.MB - sb.UR - sb.UD;
            sb.CT = uBlind ? sb.NT : sb.NT - sb.UR - sb.UD;
            sb.U = (sb.CB == 0 || !sb.HasVotes) ? (double)sb.RB / sb.MB : (double)fr / (fr + fd);
            sb.Winner1 = list.Count == 1 ? list[0].Winner : (sb.RB * 2 > sb.MB ? "R" : "D");
            return sb;
        }

        private static (double Alpha, double K, bool Fitted) _bPrimeFit;

        /// <summary>One state's REP seats by (b), (b0), (b U-blind) or (b'). <paramref name="alpha"/> and <paramref name="k"/> are (b')'s curve.</summary>
        private static int CountBState(Cycle c, string st, Config cfg, string kind, double alpha, double k)
        {
            StateBase s = BaseOf(c, st, kind == "bU");
            double delta = V(c.T, cfg) - V(c.B, cfg);
            double uhat = s.U + delta;
            bool half = HalfToRep(c.B, st);
            if (s.NT == 1) { return uhat > 0.5 ? 1 : uhat < 0.5 ? 0 : (s.Winner1 == "R" ? 1 : 0); }
            Func<int, double, int> p = (cc, x) => P(cc, x, half);
            if (kind == "bp")
            {
                p = (cc, x) =>
                {
                    double xc = Math.Min(1 - 1e-9, Math.Max(1e-9, x));
                    double prob = 1.0 / (1.0 + Math.Exp(-(alpha + k * Math.Log(xc / (1 - xc)))));
                    return RoundHalf(cc * prob, half);
                };
            }
            if (kind == "b0") { return s.UR + p(s.CT, uhat); }
            return Math.Min(s.NT - s.UD, Math.Max(s.UR, s.RB + p(s.CT, uhat) - p(s.CB, s.U)));
        }

        private static RowCount CountB(Cycle c, Config cfg, string kind)
        {
            var rc = new RowCount { Rep = new Dictionary<string, int>(StringComparer.Ordinal) };
            double alpha = 0, k = 1;
            if (kind == "bp")
            {
                (alpha, k, rc.Fitted) = FitBPrime(c, cfg);
                rc.Note = rc.Fitted ? F("alpha {0:0.0000}, k {1:0.0000}", alpha, k) : "not fitted";
            }
            foreach (string st in _states) { rc.Rep[st] = CountBState(c, st, cfg, kind, alpha, k); }
            rc.National = rc.Rep.Values.Sum();
            return rc;
        }

        /// <summary>(b')'s two parameters, fitted on the base alone by binomial maximum likelihood over its states with n_B >= 2, c_B >= 1 and
        /// 0 &lt; u &lt; 1 (R_B - U_R successes in c_B trials, logit u the predictor); Newton from (0, 1), converged when the step's largest component
        /// is below 1e-12, at most 100 iterations; not converged, or k &lt;= 0: not fitted.</summary>
        private static (double, double, bool) FitBPrime(Cycle c, Config cfg)
        {
            var pts = new List<(double X, int Y, int N)>();
            foreach (string st in _states)
            {
                int nB = _districts[c.B][st].Count;
                StateBase s = BaseOf(c, st, false);
                if (nB >= 2 && s.CB >= 1 && s.U > 0 && s.U < 1) { pts.Add((Math.Log(s.U / (1 - s.U)), s.RB - s.UR, s.CB)); }
            }
            double a = 0, k = 1;
            for (int it = 0; it < 100; it++)
            {
                double g0 = 0, g1 = 0, h00 = 0, h01 = 0, h11 = 0;
                foreach (var t in pts)
                {
                    double pr = 1.0 / (1.0 + Math.Exp(-(a + k * t.X)));
                    double r = t.Y - t.N * pr, w = t.N * pr * (1 - pr);
                    g0 += r; g1 += r * t.X; h00 += w; h01 += w * t.X; h11 += w * t.X * t.X;
                }
                double det = h00 * h11 - h01 * h01;
                if (det <= 0 || double.IsNaN(det)) { return (a, k, false); }
                double da = (h11 * g0 - h01 * g1) / det, dk = (-h01 * g0 + h00 * g1) / det;
                a += da; k += dk;
                if (double.IsNaN(a) || double.IsNaN(k) || double.IsInfinity(a) || double.IsInfinity(k)) { return (a, k, false); }
                if (Math.Max(Math.Abs(da), Math.Abs(dk)) < 1e-12) { return (a, k, k > 0); }
            }
            return (a, k, false);
        }

        // ------------------------------------------------------------------ (c): the anchored bilogit; no change
        private static double Logit(double x) { x = Math.Min(1 - 1e-9, Math.Max(1e-9, x)); return Math.Log(x / (1 - x)); }

        private static double S(int year, Config cfg)
        {
            if (year == 2020 && cfg.Truth2020Published) { return 212.0 / 434.0; }
            int r = 0, d = 0;
            foreach (var list in _districts[year].Values) { (int a, int b) = DecidedSeats(list); r += a; d += b; }
            return (double)r / (r + d);
        }

        /// <summary>The transitions (t-1, t) of the record whose later election precedes T, less those in which more than half of the 435 seats were
        /// redrawn or re-apportioned.</summary>
        private static List<(int From, int To)> Window(int t)
        {
            var w = new List<(int, int)>();
            for (int i = 1; i < Years.Length; i++)
            {
                int a = Years[i - 1], b = Years[i];
                if (b >= t) { continue; }
                int moved = 0;
                foreach (string st in _states) { if (_maps[(b, st)].InForce || _maps[(b, st)].Seats != _maps[(a, st)].Seats) { moved += _maps[(b, st)].Seats; } }
                if (moved * 2 <= 435) { w.Add((a, b)); }
            }
            return w;
        }

        private static RowCount CountC(Cycle c, Config cfg)
        {
            var win = Window(c.T);
            var rc = new RowCount { Rep = null };
            if (win.Count == 0) { rc.Fitted = false; rc.Note = "no window - no (c)"; return rc; }
            double sxy = 0, sxx = 0;
            foreach ((int a, int b) in win) { double x = Logit(V(b, cfg)) - Logit(V(a, cfg)), y = Logit(S(b, cfg)) - Logit(S(a, cfg)); sxy += x * y; sxx += x * x; }
            if (sxx == 0) { throw new InvalidOperationException(c.Name + ": sum x^2 = 0 in (c)'s window"); }
            double rho = sxy / sxx;
            double sh = 1.0 / (1.0 + Math.Exp(-(Logit(S(c.B, cfg)) + rho * (Logit(V(c.T, cfg)) - Logit(V(c.B, cfg))))));
            (int rB, int dB) = (0, 0);
            foreach (var list in _districts[c.B].Values) { (int a2, int b2) = DecidedSeats(list); rB += a2; dB += b2; }
            rc.National = RoundHalf(435 * sh, rB > dB);
            rc.Note = F("rho {0:0.0000} over {1}{2}", rho, string.Join(", ", win.Select(w => w.From + "->" + w.To)), rho <= 0 ? " - UNPLAYABLE (rho <= 0)" : "");
            return rc;
        }

        private static RowCount CountNoChange(Cycle c)
        {
            var rc = new RowCount { Rep = new Dictionary<string, int>(StringComparer.Ordinal) };
            foreach (string st in _states)
            {
                (int r, int d) = DecidedSeats(_districts[c.B][st]);
                int nT = _maps[(c.T, st)].Seats;
                rc.Rep[st] = nT == r + d ? r : P(nT, (double)r / (r + d), HalfToRep(c.B, st));
            }
            rc.National = rc.Rep.Values.Sum();
            return rc;
        }

        // ------------------------------------------------------------------ the truth and the scores
        private static Truth TruthOf(int year, Config cfg)
        {
            var t = new Truth();
            foreach (string st in _states)
            {
                int r = 0, d = 0, z = 0, o = 0;
                foreach (District x in _districts[year][st])
                {
                    bool undecided2020 = year == 2020 && cfg.Truth2020Published && st == "NY" && x.No == 22;
                    if (undecided2020 || x.Winner == "-") { z++; } else if (x.Winner == "R") { r++; } else if (x.Winner == "D") { d++; } else { o++; }
                }
                t.R[st] = r; t.D[st] = d; t.Z[st] = z; t.O[st] = o; t.Rn += r; t.Dn += d; t.Zn += z; t.On += o;
            }
            return t;
        }

        private static int Dist(int x, int lo, int hi) => x < lo ? lo - x : x > hi ? x - hi : 0;
        private static int SignedDist(int x, int lo, int hi) => x < lo ? x - lo : x > hi ? x - hi : 0;

        /// <summary>The classes (REP majority, DEM majority, tied) a state's record can take under some resolution of its undecided seats.</summary>
        private static HashSet<string> RecordClasses(Truth t, string st) { var s = new HashSet<string>(); for (int z = 0; z <= t.Z[st]; z++) { s.Add(Class(t.R[st] + z, t.D[st] + t.Z[st] - z)); } return s; }
        private static string Class(int r, int d) => r > d ? "R" : d > r ? "D" : "T";

        private static string ContingentOf(IEnumerable<string> classes) { var l = classes.ToList(); int r = l.Count(x => x == "R"), d = l.Count(x => x == "D"); return r >= 26 ? "REP" : d >= 26 ? "DEM" : "no state majority"; }

        private static Score ScoreOf(Cycle c, RowCount rc, Truth t, Config cfg)
        {
            var s = new Score { Rep = rc.National, HasStates = rc.Rep != null, Fitted = rc.Fitted };
            s.N = SignedDist(rc.National, t.Rn, t.Rn + t.Zn + t.On);
            s.AbsN = Math.Abs(s.N);
            s.Control = rc.National >= 218;
            if (rc.Rep == null) { return s; }
            var classes = new List<string>();
            foreach (string st in _states)
            {
                int n = _maps[(c.T, st)].Seats, rep = rc.Rep[st];
                int w = Dist(rep, t.R[st], t.R[st] + t.Z[st]);
                s.W[st] = w; s.G += w;
                if (Held(c, st, cfg)) { s.H += w; }
                string cl = Class(rep, n - rep);
                classes.Add(cl);
                if (RecordClasses(t, st).Contains(cl)) { s.K++; }
            }
            s.Contingent = ContingentOf(classes);
            return s;
        }

        // ------------------------------------------------------------------ a run: the counts, the scores, the seven steps, the outcome
        private sealed class RunResult
        {
            public Config Cfg; public Dictionary<string, Score[]> Scores = new Dictionary<string, Score[]>(); public Dictionary<string, RowCount[]> Counts = new Dictionary<string, RowCount[]>();
            public string Step2 = "", DigestText = ""; public bool S2, S3, S5, S6; public int SwingCycles;
            public List<string> Lines = new List<string>(), UnderS1 = new List<string>();
            public Dictionary<string, string> Figures = new Dictionary<string, string>(StringComparer.Ordinal);
            /// <summary>The stops that fire besides the step-2 verdict, in the block's order; S2, S3 and S6 marked printed where this D has one swing cycle.</summary>
            public List<string> StopList()
            {
                var l = new List<string>(); string p = SwingCycles >= 2 ? "" : " printed";
                if (S2) { l.Add("S2" + p); } if (S3) { l.Add("S3" + p); } if (S5) { l.Add("S5"); } if (S6) { l.Add("S6" + p); }
                return l;
            }
            public string Outcome() { List<string> l = StopList(); return Step2 + (l.Count > 0 ? " + " + string.Join(", ", l) : ""); }
            /// <summary>The outcome as step 6 compares it (reading (ii)): the step-2 verdict and S5, and S2, S3 and S6 only where the reading's D has two swing cycles.</summary>
            public string Compared(bool two) => Step2 + "|S5 " + S5 + (two ? "|S2 " + S2 + "|S3 " + S3 + "|S6 " + S6 : "");
        }
        private static string Short(int row) => row == A_FOLD ? "(a)" : row == B_ANCH ? "(b)" : RowNames[row];

        private static RunResult Evaluate(Config cfg)
        {
            var run = new RunResult { Cfg = cfg };
            foreach (Cycle c in Cycles)
            {
                var counts = new RowCount[RowNames.Length];
                counts[A_FOLD] = CountA(c, cfg, false);
                counts[A_FPROP] = CountA(c, cfg, true);
                counts[B_ANCH] = CountB(c, cfg, "b");
                counts[B0] = CountB(c, cfg, "b0");
                counts[B_UBLIND] = CountB(c, cfg, "bU");
                counts[B_PRIME] = CountB(c, cfg, "bp");
                counts[C_BILOGIT] = CountC(c, cfg);
                counts[NO_CHANGE] = CountNoChange(c);
                Truth t = TruthOf(c.T, cfg);
                run.Counts[c.Name] = counts;
                run.Scores[c.Name] = counts.Select(rc => ScoreOf(c, rc, t, cfg)).ToArray();
            }
            Cycle[] d = Cycles.Where(c => cfg.D.Contains(c.Name)).ToArray();
            Cycle[] swing = d.Where(c => c.Role == "swing").ToArray();
            Cycle[] level = d.Where(c => c.Role == "level").ToArray();
            Cycle[] summed = cfg.LevelOutOfSums ? swing : d;
            int Sum(int row, Func<Score, int> f, Cycle[] over) => over.Sum(c => f(run.Scores[c.Name][row]));

            // STEP 1 - (a)'s fallback: over D's redrawn state-cycles, W under F-old and under F-prop
            int wOld = 0, wProp = 0;
            var digest = new StringBuilder();
            foreach (Cycle c in d) { foreach (string st in _states) { if (!Held(c, st, cfg)) { int wo = run.Scores[c.Name][A_FOLD].W[st], wp = run.Scores[c.Name][A_FPROP].W[st]; wOld += wo; wProp += wp; digest.Append(F("W {0} {1} {2} {3};", c.Name, st, wo, wp)); } } }
            bool s5 = wProp < wOld;
            run.Lines.Add(F("step 1 - (a)'s fallback over D's redrawn state-cycles: W F-old {0}, F-prop {1} - {2}", wOld, wProp, s5 ? "F-prop strictly lower" : "F-old kept"));

            // STEP 2 - (a) against (b) on held-state seats misplaced
            int hA = Sum(A_FOLD, s => s.H, summed), hB = Sum(B_ANCH, s => s.H, summed);
            var failed = new List<string>();
            if (!swing.All(c => run.Scores[c.Name][B_ANCH].H < run.Scores[c.Name][A_FOLD].H)) { failed.Add("H(b) < H(a) in every swing cycle"); }
            if (!level.All(c => run.Scores[c.Name][B_ANCH].H <= run.Scores[c.Name][A_FOLD].H)) { failed.Add("H(b) <= H(a) in the level cycle"); }
            if (!(Sum(B_ANCH, s => s.G, summed) < Sum(A_FOLD, s => s.G, summed))) { failed.Add("sum G(b) < sum G(a)"); }
            if (!d.All(c => run.Scores[c.Name][B_ANCH].K >= run.Scores[c.Name][A_FOLD].K)) { failed.Add("K(b) >= K(a) in every cycle"); }
            string step2 = hA <= hB ? "(a)" : failed.Count == 0 ? "(b)" : "S1";
            run.Step2 = step2;
            run.Lines.Add(F("step 2 - sum H over D: (a) {0}, (b) {1}; (b)'s switch conditions failed: {2} - {3}", hA, hB, failed.Count == 0 ? "none" : string.Join("; ", failed), step2 == "S1" ? "S1" : "code recommends " + step2));
            run.Figures["S1"] = F("{0}; sum H over D (a) {1}, (b) {2}", failed.Count == 0 ? "no condition failed" : string.Join("; ", failed), hA, hB);
            run.Figures["S5"] = F("W over D's redrawn state-cycles F-old {0}, F-prop {1}", wOld, wProp);

            // STEP 3 - control; after S1 both directions are listed under S1, not as a stop of their own
            bool twoSwing = swing.Length >= 2;
            run.SwingCycles = swing.Length;
            int picked = step2 == "(a)" ? A_FOLD : step2 == "(b)" ? B_ANCH : -1;
            bool Worse(int row, int other) => swing.All(c => run.Scores[c.Name][row].AbsN > run.Scores[c.Name][other].AbsN);
            string nFig = string.Join("; ", swing.Select(c => F("{0} |N| (a) {1}, (b) {2}", c.Name, run.Scores[c.Name][A_FOLD].AbsN, run.Scores[c.Name][B_ANCH].AbsN)));
            if (picked >= 0)
            {
                int other = picked == A_FOLD ? B_ANCH : A_FOLD;
                run.S2 = Worse(picked, other);
                run.Figures["S2"] = F("{0}'s |N| above {1}'s in every swing cycle - {2}", Short(picked), Short(other), nFig);
                run.Lines.Add(F("step 3 - {0}{1}", nFig, run.S2 ? (twoSwing ? " - S2 fires" : " - S2 PRINTED ONLY (one swing cycle in D): it would fire") : " - S2 does not fire"));
            }
            else
            {
                string under = F("control both ways - (a)'s |N| above (b)'s in every swing cycle {0}, (b)'s above (a)'s {1}: {2}", Worse(A_FOLD, B_ANCH), Worse(B_ANCH, A_FOLD), nFig);
                run.UnderS1.Add(under); run.Lines.Add("step 3 - after S1, " + under);
            }

            // STEP 4 - (c): S3 only with at least two forward swing cycles; after S1, (c) below both (a) and (b)
            Cycle[] forward = swing.Where(c => run.Counts[c.Name][C_BILOGIT].Fitted).ToArray();
            int[] against = picked >= 0 ? new[] { picked } : new[] { A_FOLD, B_ANCH };
            string CFig(Cycle c) => F("{0} |N| (c) {1}, {2}, no change {3}", c.Name, run.Scores[c.Name][C_BILOGIT].AbsN,
                string.Join(", ", against.Select(r => Short(r) + " " + run.Scores[c.Name][r].AbsN)), run.Scores[c.Name][NO_CHANGE].AbsN);
            if (forward.Length >= 2)
            {
                run.S3 = forward.All(c => against.All(r => run.Scores[c.Name][C_BILOGIT].AbsN < run.Scores[c.Name][r].AbsN)
                    && run.Scores[c.Name][C_BILOGIT].AbsN < run.Scores[c.Name][NO_CHANGE].AbsN);
            }
            run.Figures["S3"] = forward.Length == 0 ? "no forward swing cycle in D" : string.Join("; ", forward.Select(CFig));
            run.Figures["C"] = F("{0} forward swing cycle(s) in D{1}", forward.Length, forward.Length > 0 ? " - " + run.Figures["S3"] : "");
            run.Lines.Add(F("step 4 - (c): {0}{1}", run.Figures["C"], forward.Length >= 2 ? (run.S3 ? " - S3 fires" : " - S3 does not fire") : " - S3 cannot fire"));

            // STEP 5 - the strawman (b'), by step 2's own switch conditions; after S1, against both (a) and (b), listed under S1
            bool bpFitted = d.All(c => run.Counts[c.Name][B_PRIME].Fitted);
            if (bpFitted)
            {
                bool Beats(int vs) =>
                    swing.All(c => run.Scores[c.Name][B_PRIME].H < run.Scores[c.Name][vs].H)
                    && level.All(c => run.Scores[c.Name][B_PRIME].H <= run.Scores[c.Name][vs].H)
                    && Sum(B_PRIME, s => s.G, summed) < Sum(vs, s => s.G, summed)
                    && d.All(c => run.Scores[c.Name][B_PRIME].K >= run.Scores[c.Name][vs].K);
                string BFig(int vs) => F("(b') against {0}: {1}; sum G {2} against {3}", Short(vs),
                    string.Join("; ", d.Select(c => F("{0} H {1} against {2}, K {3} against {4}", c.Name, run.Scores[c.Name][B_PRIME].H, run.Scores[c.Name][vs].H, run.Scores[c.Name][B_PRIME].K, run.Scores[c.Name][vs].K))),
                    Sum(B_PRIME, s => s.G, summed), Sum(vs, s => s.G, summed));
                if (picked >= 0)
                {
                    run.S6 = Beats(picked);
                    run.Figures["S6"] = BFig(picked);
                    run.Lines.Add(F("step 5 - {0}{1}", BFig(picked), run.S6 ? (twoSwing ? " - S6 fires" : " - S6 PRINTED ONLY (one swing cycle in D): it would fire") : " - S6 does not fire"));
                }
                else
                {
                    string under = F("the strawman both ways - (b') beats (a) {0}, beats (b) {1}: {2}; {3}", Beats(A_FOLD), Beats(B_ANCH), BFig(A_FOLD), BFig(B_ANCH));
                    run.UnderS1.Add(under); run.Lines.Add("step 5 - after S1, " + under);
                }
            }
            else { run.Lines.Add("step 5 - (b') not fitted in every cycle of D - S6 cannot fire"); }

            // S5, with step 1's exception: (b) picked and S2 not firing - a printed S2 (one swing cycle) counting as not firing
            run.S5 = s5 && !(step2 == "(b)" && !(run.S2 && twoSwing));
            if (s5 && !run.S5) { run.Lines.Add("step 1 - (b) picked and S2 not firing: (b) has no fallback, the comparison printed"); }
            run.Lines.Add(Cycles.Any(c => c.T > 2024) ? "step 7 - a measured cycle ends after 2024: S7 NOT BUILT" : "step 7 - every measured cycle's later election is 2024 or before: S7 cannot fire");

            // the decisive digest's figures for this run, each whatever an earlier condition of its step gives
            foreach (Cycle c in d)
            {
                foreach (int row in new[] { A_FOLD, B_ANCH, B_PRIME })
                {
                    Score s = run.Scores[c.Name][row];
                    digest.Append(F("{0} {1} H {2} G {3} N {4} K {5};", c.Name, RowNames[row], s.H, s.G, s.N, s.K));
                }
                digest.Append(F("{0} (b') fitted {1};", c.Name, run.Counts[c.Name][B_PRIME].Fitted));
            }
            if (forward.Length >= 2) { foreach (Cycle c in forward) { digest.Append(F("{0} (c) N {1} no change N {2};", c.Name, run.Scores[c.Name][C_BILOGIT].N, run.Scores[c.Name][NO_CHANGE].N)); } }
            run.DigestText = digest.ToString();
            return run;
        }

        // ------------------------------------------------------------------ the measurement: every run, the readings, the verdict, the card
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            Load();
            void Both(string line) { sb.Append("    ").Append(line).Append('\n'); card.Add(line); }

            // the block: its digest the list's last
            string plan = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs/specs/USA_STAGE_PLAN.md"), Encoding.UTF8).Replace("\r\n", "\n");
            int b0 = plan.IndexOf("<!-- R-US18 RULE BEGIN -->\n", StringComparison.Ordinal), b1 = plan.IndexOf("<!-- R-US18 RULE END -->", StringComparison.Ordinal);
            Check(b0 >= 0 && b1 > b0, "R-US18's block found in the plan");
            string blockDigest = b0 >= 0 && b1 > b0 ? Digest(plan.Substring(b0 + "<!-- R-US18 RULE BEGIN -->\n".Length, b1 - b0 - "<!-- R-US18 RULE BEGIN -->\n".Length)) : "-";
            Check(blockDigest == RuleDigests[RuleDigests.Length - 1].Digest, F("the block's digest {0} is the list's last ({1})", blockDigest.Substring(0, Math.Min(8, blockDigest.Length)), RuleDigests[RuleDigests.Length - 1].Why));

            // C5 (2024->2026) is BILLED: the catalog holds no House returns after 2024. Once it does, the rule is owed a run over D plus C5 and
            // step 7's S7 - neither built - so this fails rather than passing on the 2016-2024 cycles alone
            int lastYear = UsPresidentialReturns.HouseDistricts.Max(r => r.Year);
            Check(lastYear <= 2024 && Cycles.All(c => c.T <= 2024), F("C5 BILLED: the catalog's House returns end {0} - a later year needs the run over D plus C5 and S7, not built", lastYear));

            // the truth gate: the record's district plurality gives [HH-DIV] every year but its footnoted vacancies
            foreach (var y in UsPresidentialReturns.HouseRecord)
            {
                if (y.Year > 2024) { continue; }
                Truth t = TruthOf(y.Year, new Config());
                int vacantToR = 0, vacantToD = 0;
                if (y.Vacant != "-") { string[] p = y.Vacant.Split('-'); District v = _districts[y.Year][p[0]].First(x => x.No == int.Parse(p[1], CultureInfo.InvariantCulture)); if (v.Winner == "R") { vacantToR = 1; } else if (v.Winner == "D") { vacantToD = 1; } }
                Check(t.Rn == y.SeatsR + vacantToR && t.Dn == y.SeatsD + vacantToD && t.On == 0, F("the truth {0}: REP {1} DEM {2} undecided {3} - [HH-DIV] {4}/{5}, its vacancy {6}", y.Year, t.Rn, t.Dn, t.Zn, y.SeatsR, y.SeatsD, y.Vacant));
                var classes = _states.Select(st => RecordClasses(t, st)).ToList();
                Check(classes.All(c => c.Count == 1), F("the record's {0} delegations each of one class", y.Year));
                var cls = classes.Select(c => c.First()).ToList();
                Both(F("the record {0}: delegations REP {1} DEM {2} tied {3}; the contingent call {4}", y.Year, cls.Count(x => x == "R"), cls.Count(x => x == "D"), cls.Count(x => x == "T"), ContingentOf(cls)));
            }

            // the declared readings
            var baseCfg = new Config();
            var readings = new List<(string Key, Config Cfg)>
            {
                ("(i)", new Config { Name = "(i) 2020's truth as published", Truth2020Published = true }),
                ("(ii)", new Config { Name = "(ii) D less C1", D = new[] { "C2", "C4" } }),
                ("(iii)", new Config { Name = "(iii) Minnesota 2018 held", MinnesotaHeld2018 = true }),
                ("(iv)", new Config { Name = "(iv) Colorado 2018 held", ColoradoHeld2018 = true }),
                ("(v)", new Config { Name = "(v) both held", MinnesotaHeld2018 = true, ColoradoHeld2018 = true }),
                ("(vi)", new Config { Name = "(vi) the swing on the deciding count - PRINTED ONLY (G1)", DecidingCountSwing = true, PrintedOnly = true }),
                ("(vii)", new Config { Name = "(vii) the level cycle out of the sums", LevelOutOfSums = true }),
            };
            RunResult main = Evaluate(baseCfg);

            // the measures, per cycle and row
            foreach (Cycle c in Cycles)
            {
                Truth t = TruthOf(c.T, baseCfg);
                Both(F("{0} {1}->{2} ({3}): V {4:0.0000} -> {5:0.0000}, swing {6:+0.0000;-0.0000}; the record REP {7} DEM {8} undecided {9}; redrawn: {10}", c.Name, c.B, c.T, c.Role,
                    _vOwn[c.B], _vOwn[c.T], _vOwn[c.T] - _vOwn[c.B], t.Rn, t.Dn, t.Zn, string.Join(" ", _states.Where(st => !Held(c, st, baseCfg)).Select(st => st + "(" + _maps[(c.T, st)].Seats + ")"))));
                for (int row = 0; row < RowNames.Length; row++)
                {
                    Score s = main.Scores[c.Name][row];
                    RowCount rc = main.Counts[c.Name][row];
                    if (!rc.Fitted && row == C_BILOGIT) { Both(F("  {0,-12} {1}", RowNames[row], rc.Note)); continue; }
                    Both(s.HasStates
                        ? F("  {0,-12} REP {1,3}  N {2,3:+0;-0;0}  control {3,-3}  G {4,3}  H {5,3}  K {6,2}/50  contingent {7}{8}", RowNames[row], s.Rep, s.N, s.Control ? "REP" : "DEM", s.G, s.H, s.K, s.Contingent, rc.Note.Length > 0 ? "  - " + rc.Note : "")
                        : F("  {0,-12} REP {1,3}  N {2,3:+0;-0;0}  control {3,-3}  - {4}", RowNames[row], s.Rep, s.N, s.Control ? "REP" : "DEM", rc.Note));
                }
                Both(F("  F-old counted: {0}", main.Counts[c.Name][A_FOLD].FoldStates.Count == 0 ? "none" : string.Join(", ", main.Counts[c.Name][A_FOLD].FoldStates)));
            }

            // the steps of the run over D
            foreach (string line in main.Lines) { Both(line); }

            // the readings, each run alone; S4 where one not printed only changes the outcome as step 6 compares it
            var changed = new List<string>();
            var changedFigs = new List<string>();
            var digestText = new StringBuilder("base:" + main.DigestText);
            foreach (var r in readings)
            {
                RunResult rr = Evaluate(r.Cfg);
                bool two = rr.SwingCycles >= 2;
                bool moves = main.Compared(two) != rr.Compared(two);
                Both(F("reading {0}: {1}{2}", r.Cfg.Name, rr.Outcome(), r.Cfg.PrintedOnly ? " (printed only - compared with nothing)"
                    : (two ? "" : " (one swing cycle: the step-2 verdict and S5 compared)") + (moves ? " - CHANGES THE OUTCOME" : " - the same")));
                if (!r.Cfg.PrintedOnly)
                {
                    digestText.Append(r.Key + ":" + rr.DigestText);
                    if (moves) { changed.Add(r.Key); changedFigs.Add(F("{0} gives {1} - {2}", r.Key, rr.Outcome(), Moved(main, rr, two))); }
                }
            }
            var stops = main.StopList();
            if (changed.Count > 0) { stops.Add("S4 [" + string.Join(", ", changed) + "]"); }
            string outcome = main.Step2 + (stops.Count > 0 ? " + " + string.Join(", ", stops) : "");
            string decisive = Digest(digestText.ToString());
            bool decided = main.Step2 != "S1" && stops.Count == 0;
            var stopFigs = new List<string>();
            if (main.Step2 == "S1") { stopFigs.Add("S1 (" + main.Figures["S1"] + (main.UnderS1.Count > 0 ? "; listed under S1: " + string.Join("; ", main.UnderS1) : "") + ")"); }
            foreach (string s in main.StopList()) { stopFigs.Add(s + " (" + main.Figures[s.Substring(0, 2)] + ")"); }
            if (changed.Count > 0) { stopFigs.Add(F("S4 ({0}; the run over D gives {1})", string.Join("; ", changedFigs), main.Outcome())); }
            int pickedRow = main.Step2 == "(a)" ? A_FOLD : B_ANCH;
            Cycle[] dMain = Cycles.Where(c => baseCfg.D.Contains(c.Name)).ToArray();
            string verdict = decided
                ? F("By R-US18's rule, code recommends {0}: {1}{2}; summed over D H {3}, G {4}, K {5}, N {6:+0;-0;0}; (c)'s comparison: {7}; the same under every declared reading.", main.Step2, main.Step2 == "(a)" ? "with F-old; " : "",
                    string.Join("; ", dMain.Select(c => { Score s = main.Scores[c.Name][pickedRow]; return F("{0} H {1} G {2} K {3} N {4:+0;-0;0}", c.Name, s.H, s.G, s.K, s.N); })),
                    dMain.Sum(c => main.Scores[c.Name][pickedRow].H), dMain.Sum(c => main.Scores[c.Name][pickedRow].G), dMain.Sum(c => main.Scores[c.Name][pickedRow].K),
                    dMain.Sum(c => main.Scores[c.Name][pickedRow].N), main.Figures["C"])
                : F("R-US18's rule does not decide: {0}; (c), printed: {1} - asked.", string.Join("; ", stopFigs), main.Figures["C"]);
            Both("THE OUTCOME (the run over D): " + outcome);
            Both("THE VERDICT: " + verdict);
            Both(F("the decisive digest: {0}", decisive));

            // the ruling record, held by this instrument and mirrored in the plan's line after the block
            Check(RulingState == "NONE" || RulingState == "ASKED" || RulingState == "RULED", "the ruling record's state is one of NONE, ASKED, RULED");
            if (RulingState == "NONE") { Check(false, "the ruling record is NONE - the first run registers the ask (R-US18's block: NONE fails every run until its ask is registered)"); }
            if (RulingState == "ASKED")
            {
                Check(RulingOutcomeOverD == outcome, F("ASKED: the outcome {0} is the registered one ({1})", outcome, RulingOutcomeOverD));
                Check(RulingDecisiveDigest == decisive, F("ASKED: the decisive digest {0} is the registered one ({1}) - a moved digest is registered again and Elias told", decisive.Substring(0, 8), RulingDecisiveDigest.Length >= 8 ? RulingDecisiveDigest.Substring(0, 8) : RulingDecisiveDigest));
            }
            if (RulingState == "RULED")
            {
                Check(Array.IndexOf(BuildableOptions, RulingOption) >= 0, F("RULED: the ruling names an option that can be built ('{0}')", RulingOption));
                Check(RulingOutcomeOverD == outcome, F("RULED: the outcome {0} is the one the ruling was made on ({1}) - a changed outcome asks R-US18 again, never re-pinned", outcome, RulingOutcomeOverD));
                Check(RulingDecisiveDigest == decisive, F("RULED: the decisive digest {0} is the one the ruling was made on ({1}) - with the outcome unchanged, a commit re-pins it naming the old, the new and \"outcome unchanged\"", decisive.Substring(0, 8), RulingDecisiveDigest.Length >= 8 ? RulingDecisiveDigest.Substring(0, 8) : RulingDecisiveDigest));
            }
            string mirror = RulingState == "RULED" ? F("**Ruling record:** RULED ({0}): {1}.", RulingSection, RulingRuled) : F("**Ruling record:** {0} ({1}): {2}.", RulingState, RulingSection, RulingOutcomeOverD);
            int at = b1 >= 0 ? plan.IndexOf("**Ruling record:**", b1, StringComparison.Ordinal) : -1;
            string planLine = at >= 0 ? plan.Substring(at, plan.IndexOf('\n', at) - at) : "-";
            Check(planLine == mirror, F("the plan's line after the block mirrors the record: '{0}'", planLine));
            return Digest(UsPresidentialReturns.HouseDistrictSourceDigest + UsPresidentialReturns.HouseMapSourceDigest + UsPresidentialReturns.HouseYearSourceDigest + UsPresidentialReturns.HouseSourceDigest + blockDigest + decisive + RulingState);
        }

        /// <summary>What moved a reading's outcome as step 6 compares it, with the reading's own figures.</summary>
        private static string Moved(RunResult main, RunResult rr, bool two)
        {
            var l = new List<string>();
            if (main.Step2 != rr.Step2) { l.Add("step 2: " + rr.Figures["S1"]); }
            if (main.S5 != rr.S5) { l.Add("S5: " + rr.Figures["S5"]); }
            if (two && main.S2 != rr.S2) { l.Add("S2: " + (rr.Figures.TryGetValue("S2", out string f2) ? f2 : "-")); }
            if (two && main.S3 != rr.S3) { l.Add("S3: " + rr.Figures["S3"]); }
            if (two && main.S6 != rr.S6) { l.Add("S6: " + (rr.Figures.TryGetValue("S6", out string f6) ? f6 : "-")); }
            return string.Join("; ", l);
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsHouseCountCheck (PS-6 US-11, §800): R-US18's rule on the House of record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(CardPath(), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no House block", CardRelative)
                    : onDisk == expected ? F("the model card's House block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's House block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsHouseCountCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== UsHouseCountCheck: the reading holds ===" : F("=== UsHouseCountCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's House block from this reading - refused while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsHouseCountCheck.WriteReadings (§800): the US model card's House block ===\n");
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
                else if (old == null) { Check(false, F("the model card ({0}) carries no House block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's House block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }
            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ the card
        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsHouseCountCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
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
