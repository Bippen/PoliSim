using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// COAL-1 (RULINGS_2026-10-18 q1, q21; Elias's rulings K130-K133 of 2026-10-27; the audit's GA-4-1): A PARTNER'S EXIT OPENS THE COUNTRY'S CONFIDENCE
    /// PROCEDURE. Each case says which clause it proves; the seats are planted where the clause needs a chamber, and the projected vote is planted
    /// (<see cref="CoalitionExit.PlantProjection"/>) where K130's test is walked - the budget vote's hook is the manager's own
    /// (<see cref="SimulationManager.CountBudgetVoteOnAgreements"/>, which every budget vote's site calls).
    /// <para>(1) GA-4-1 REPRODUCED AND ANSWERED: Sweden on 2026-11-14 (the audit's day 300), the chamber the audit's seed 802 counted, SD+M+KD+L led by M;
    /// the player as L leaves. Ruled: the procedure opens - the declared opposition (the parties red-lined from SD+M+KD) does not hold 175 of 349, L votes by
    /// its own lines (it abstains), so no motion is moved, SD+M+KD governs as a minority on 172 and every vote reads its new support.</para>
    /// <para>(2) K131 bites: after C leaves SD+M+KD+C, the declared opposition V+MP+C holds a majority - V moves and the motion carries, C voting for it by its
    /// own line (K132).</para>
    /// <para>(3) K131's second clause - THE S641 GATE IS LIFTED: on a planted chamber where the gated daily rule (`SimulationManager.TryAiMotion` with no exit
    /// named, invoked by reflection) moves nothing, the same chamber after the player's partner C leaves moves the declared opposition's motion. Fails if the
    /// gate were put back on the exit path, and fails if no planted chamber holds a gate that refuses.</para>
    /// <para>(4) K130 bites: one point short of sigma_v stays, one point past leaves; past it, inside 90 days before or after a polling day stays; two partners
    /// past it - the larger margin leaves alone, and on equal margins the key first in ordinal order.</para>
    /// <para>(5) Germany (Art. 67, K131's declared movers): with the Linke in the cabinet the CDU is declared against it (the Union's resolution) and its
    /// constructive vote elects its candidate after the FDP leaves; with SPD+Grüne alone the CDU is not declared, and no constructive vote is moved.</para>
    /// <para>(6) Poland (K133): the Sejm's rule (Art. 158). With the player as President and TD his party, TD - the AI's party (SP-7b) - leaves KO+TD+NL and
    /// PiS's constructive vote chooses its candidate, 231 of the statutory 460 needed. The player's own motion: 46 movers, the statutory majority whatever is
    /// seated.</para>
    /// </summary>
    public static class CoalitionExitDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CoalitionExitDiagnostic (COAL-1, K130-K133): a partner's exit opens the confidence procedure ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();   // the review's L6: the streams restored for the suite's next check
            var hosts = new List<GameObject>();
            (SimulationManager, Country) Open(string name, CountryId id, DateTime date, string player)
            {
                var go = new GameObject(name); hosts.Add(go);
                SimulationManager.SetEpoch(date);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = id;
                Country c = world.GetCountry(id);
                c.PlayerPartyAbbrev = player;
                return (sim, c);
            }
            void Seat(Country c, Dictionary<string, int> seats)
            {
                foreach (string k in new List<string>(c.ParliamentSeats.Keys)) { c.ParliamentSeats[k] = 0; }
                foreach (KeyValuePair<string, int> kv in seats) { c.ParliamentSeats[kv.Key] = kv.Value; }
            }
            GovernmentRecord Govern(Country c, string pm, DateTime on, params string[] cabinet)
            {
                var g = new GovernmentRecord { PmParty = pm, Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString(), FormedOn = on, Kind = c.Government != null ? c.Government.Kind : default, Basis = "planted (CoalitionExitDiagnostic)" };
                g.Cabinet.AddRange(cabinet);
                g.AllocatePortfolios(c);
                c.Government = g;
                return g;
            }
            string Sides(ConfidenceProcedure.MotionVote v) { var s = new List<string>(); foreach (DivisionSide d in v.Sides) { s.Add(d.Abbrev + (d.Side > 0 ? " for" : d.Side < 0 ? " against" : " abstains")); } return string.Join(", ", s); }
            DivisionSide SideOf(IEnumerable<DivisionSide> sides, string key) { foreach (DivisionSide d in sides) { if (d.Abbrev == key) { return d; } } return null; }
            int SeatsIn(Country c, IEnumerable<string> keys) { int n = 0; foreach (string k in keys) { n += c.ParliamentSeats.TryGetValue(k, out int s) ? s : 0; } return n; }
            int Motions(Country c) { int n = 0; foreach (DivisionRecord d in c.Divisions.Entries) { if (d.Motion) { n++; } } return n; }
            string Last(List<string> breaks) => breaks.Count > 0 ? breaks[breaks.Count - 1] : "NONE";
            var ga41Seats = new Dictionary<string, int> { { "S", 95 }, { "M", 74 }, { "SD", 71 }, { "KD", 27 }, { "V", 23 }, { "C", 22 }, { "MP", 21 }, { "L", 16 } };
            DateTime clear = new DateTime(2027, 3, 1);   // clear of every polling day by more than 90 days (2026-09-13 and 2030-09-08)
            double sigma = CoalitionExit.Threshold;
            try
            {
                Ga41();
                OppositionMajority();
                GateLifted();
                Threshold();
                Germany();
                Poland();
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); SimulationRandom.Reinstate(live); }
            if (failures > 0) { Debug.LogError($"COALITION EXIT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);

            // (1) GA-4-1, the player's exit
            void Ga41()
            {
                DateTime day = new DateTime(2026, 11, 14);
                (SimulationManager sim, Country se) = Open("CoalitionExitDiagnostic.ga41", CountryId.Sweden, day, "L");
                Seat(se, ga41Seats);
                GovernmentRecord g = Govern(se, "M", new DateTime(2026, 10, 20), "SD", "M", "KD", "L");
                int motions = Motions(se);
                bool left = sim.LeaveGovernment(CountryId.Sweden, out string refused);
                HashSet<string> declared = GovernmentFormation.RedLinedFrom(se, g.Cabinet, sim.CurrentDate);
                ConfidenceProcedure.MotionVote probe = ConfidenceProcedure.Vote(se, "S", sim.CurrentDate);
                DivisionSide l = SideOf(probe.Sides, "L");
                GovernmentFormation.TryGovernment(se, out IReadOnlyList<string> votesCabinet, out IReadOnlyList<string> votesSupport);
                sb.Append(F("    measured  (1) the declared opposition after L left: {0} on {1} of 349; S's motion would count {2} of {3} needed: {4}\n",
                    string.Join("+", declared), SeatsIn(se, declared), probe.For, probe.Needed, Sides(probe)));
                Check(left && refused == null && ReferenceEquals(se.Government, g) && string.Join("+", g.Cabinet) == "SD+M+KD" && SeatsIn(se, g.Cabinet) == 172,
                    F("(1) GA-4-1: the player as L leaves SD+M+KD+L on {0:yyyy-MM-dd} ({1}) - SD+M+KD on {2} of 349", day, refused ?? "accepted", SeatsIn(se, g.Cabinet)));
                Check(SeatsIn(se, declared) < 175 && !probe.Carried && l != null && l.Side == 0,
                    F("(1) K131/K132: the declared opposition holds {0} of 175 needed, and L votes by its own lines - {1} ({2})", SeatsIn(se, declared), l == null ? "NOT SEATED" : l.Side > 0 ? "for" : l.Side < 0 ? "against" : "abstains", l?.Reason ?? "-"));
                Check(Motions(se) == motions && g.NoConfidenceOn == DateTime.MinValue && !g.Caretaker && g.Outcome == CoalitionOutcomeKind.MinorityGovernment.ToString()
                      && Last(g.Breaks).Contains("of the 175 seats a motion needs;") && Last(g.Breaks).Contains("governs as MinorityGovernment on 172 of 349 seats") && Last(g.Breaks).Contains("K131"),
                    F("(1) GA-4-1 answered: no motion is moved, and the record says why - \"{0}\"", Last(g.Breaks)));
                Check(string.Join("+", votesCabinet) == "SD+M+KD" && votesSupport.Count == 0,
                    F("(1) its next budget is voted on its new support: every division reads the cabinet {0}{1} (GovernmentFormation.TryGovernment, the stored record)",
                        string.Join("+", votesCabinet), votesSupport.Count > 0 ? " with " + string.Join("+", votesSupport) : string.Empty));
            }

            // (2) K131: the declared opposition holds a majority after the exit
            void OppositionMajority()
            {
                var seats = new Dictionary<string, int> { { "V", 62 }, { "MP", 55 }, { "C", 60 }, { "SD", 70 }, { "M", 60 }, { "KD", 20 }, { "S", 8 }, { "L", 14 } };
                (SimulationManager sim, Country se) = Open("CoalitionExitDiagnostic.k131", CountryId.Sweden, clear, "S");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", new DateTime(2026, 10, 20), "SD", "M", "KD", "C");
                int motions = Motions(se);
                using (CoalitionExit.PlantProjection(p => p == "C" ? (0.05, 0.12) : (double.NaN, double.NaN)))
                {
                    sim.CountBudgetVoteOnAgreements(se);
                }
                DivisionRecord motion = Motions(se) > motions ? se.Divisions.Entries[se.Divisions.Entries.Count - 1] : null;
                DivisionSide c = motion != null ? SideOf(motion.Sides, "C") : null;
                Check(!g.Cabinet.Contains("C") && motion != null && motion.Motion && g.NoConfidenceOn == clear && g.NoConfidenceMover == "V",
                    F("(2) K131: C leaves at the budget vote (planted 5.0 % inside, 12.0 % outside) and the declared opposition moves - by {0}: {1}", g.NoConfidenceMover ?? "NOBODY", motion?.Title ?? "NO MOTION"));
                Check(c != null && c.Side > 0 && c.Reason == "a red line against a cabinet party",
                    F("(2) K132: C, the departed partner, votes by its own declared line, never as the mover - {0} ({1})", c == null ? "NOT RECORDED" : c.Side > 0 ? "for" : c.Side < 0 ? "against" : "abstains", c?.Reason ?? "-"));
            }

            // (3) K131's second clause: the s641 gate does not apply after an exit
            void GateLifted()
            {
                MethodInfo gated = typeof(SimulationManager).GetMethod("TryAiMotion", BindingFlags.NonPublic | BindingFlags.Instance);
                Check(gated != null, "(3) the gated daily rule is reachable by reflection (SimulationManager.TryAiMotion)");
                if (gated == null) { return; }
                // the player is C in both worlds, so C never moves (the player's party moves by the player's verb) and votes by its line in both
                var variants = new List<Dictionary<string, int>>
                {
                    new Dictionary<string, int> { { "V", 36 }, { "MP", 36 }, { "C", 110 }, { "SD", 70 }, { "M", 60 }, { "KD", 18 }, { "S", 4 }, { "L", 15 } },
                    new Dictionary<string, int> { { "V", 40 }, { "MP", 40 }, { "C", 100 }, { "SD", 80 }, { "M", 50 }, { "KD", 20 }, { "S", 4 }, { "L", 15 } },
                    new Dictionary<string, int> { { "V", 70 }, { "MP", 70 }, { "C", 40 }, { "SD", 80 }, { "M", 50 }, { "KD", 20 }, { "S", 4 }, { "L", 15 } },
                    new Dictionary<string, int> { { "V", 62 }, { "MP", 55 }, { "C", 60 }, { "SD", 70 }, { "M", 60 }, { "KD", 20 }, { "S", 8 }, { "L", 14 } },
                };
                for (int i = 0; i < variants.Count; i++)
                {
                    (SimulationManager sim, Country se) = Open("CoalitionExitDiagnostic.gate" + i, CountryId.Sweden, clear, "C");
                    Seat(se, variants[i]);
                    GovernmentRecord quiet = Govern(se, "M", new DateTime(2026, 10, 20), "SD", "M", "KD");
                    int declaredSeats = SeatsIn(se, GovernmentFormation.RedLinedFrom(se, quiet.Cabinet, clear));
                    GovernmentFormation.View round = GovernmentFormation.ViewOfSitting(se, null, clear);
                    string roundWords = round.HasGovernment ? string.Join("+", round.Cabinet.ConvertAll(x => x.Abbrev)) : "none";
                    gated.Invoke(sim, new object[] { se, null });
                    bool gatedMoved = !ReferenceEquals(se.Government, quiet) || quiet.NoConfidenceOn != DateTime.MinValue;
                    sb.Append(F("    measured  (3) chamber {0}: the declared opposition on {1} of 349; the round would form {2}; the gated rule without an exit {3}\n",
                        i, declaredSeats, roundWords, gatedMoved ? "moves (by " + quiet.NoConfidenceMover + ") - not this chamber" : "moves nothing"));
                    if (gatedMoved || declaredSeats < 175) { continue; }
                    (SimulationManager sim2, Country se2) = Open("CoalitionExitDiagnostic.gate" + i + ".exit", CountryId.Sweden, clear, "C");
                    Seat(se2, variants[i]);
                    GovernmentRecord g = Govern(se2, "M", new DateTime(2026, 10, 20), "SD", "M", "KD", "C");
                    bool left = sim2.LeaveGovernment(CountryId.Sweden, out string refused);
                    Check(left && g.NoConfidenceOn == clear && (g.NoConfidenceMover == "V" || g.NoConfidenceMover == "MP"),
                        F("(3) K131: on chamber {0} the gated rule moves nothing (the round forms {1}), and after the player's C leaves the declared opposition moves all the same - by {2} ({3})",
                            i, roundWords, g.NoConfidenceMover ?? "NOBODY", refused ?? Last(g.Breaks)));
                    return;
                }
                Check(false, "(3) K131: no planted chamber holds a gate that refuses - the gate's lift is unproven; re-plant");
            }

            // (4) K130: one sigma_v, the quiet days around a polling day, one exit a vote
            void Threshold()
            {
                (GovernmentRecord G, string Window) Case(string name, DateTime day, Func<string, (double Inside, double Outside)> projection)
                {
                    (SimulationManager sim, Country se) = Open("CoalitionExitDiagnostic." + name, CountryId.Sweden, day, "S");
                    Seat(se, ga41Seats);
                    GovernmentRecord g = Govern(se, "M", day.AddDays(-120), "SD", "M", "KD", "L");
                    DateTime next = sim.TryPlayerPollingDay(out DateTime n) ? n : DateTime.MinValue;
                    DateTime last = WorldClock.ChamberAt(CountryId.Sweden, day).ElectionDay;
                    if (last > day) { last = DateTime.MinValue; }
                    bool quiet = CoalitionExit.InQuietWindow(day, next, last);
                    using (CoalitionExit.PlantProjection(projection)) { sim.CountBudgetVoteOnAgreements(se); }
                    return (g, F("{0:yyyy-MM-dd}: the next polling day {1:yyyy-MM-dd}, the last {2:yyyy-MM-dd} - {3}", day, next, last, quiet ? "within 90 days of one" : "clear of both by more than 90 days"));
                }
                Func<string, (double, double)> L(double gain) => p => p == "L" ? (0.05, 0.05 + gain) : (double.NaN, double.NaN);
                (GovernmentRecord below, string w1) = Case("below", clear, L(sigma - 0.01));
                (GovernmentRecord above, string w2) = Case("above", clear, L(sigma + 0.01));
                (GovernmentRecord before, string w3) = Case("before", new DateTime(2026, 7, 1), L(sigma + 0.05));
                (GovernmentRecord after, string w4) = Case("after", new DateTime(2026, 11, 14), L(sigma + 0.05));
                Check(below.Cabinet.Contains("L") && !above.Cabinet.Contains("L"),
                    F("(4) K130: sigma_v is {0:0.00} points - L one point short of it stays ({1}), one point past it leaves ({2}); {3}", sigma * 100.0, below.Cabinet.Contains("L") ? "stays" : "LEFT", above.Cabinet.Contains("L") ? "STAYED" : "leaves", w1));
                Check(before.Cabinet.Contains("L") && after.Cabinet.Contains("L") && w3.Contains("within 90 days") && w4.Contains("within 90 days") && w2.Contains("clear of both"),
                    F("(4) K130: past sigma_v by five points, L stays within 90 days of a polling day - before it ({0}: {1}) and after it ({2}: {3})",
                        before.Cabinet.Contains("L") ? "stays" : "LEFT", w3, after.Cabinet.Contains("L") ? "stays" : "LEFT", w4));
                // one exit a vote: the larger margin alone; on equal margins (exact in binary) the key first in ordinal order
                (GovernmentRecord larger, string _) = Case("larger", clear, p => p == "L" ? (0.125, 0.375) : p == "KD" ? (0.125, 0.25) : (double.NaN, double.NaN));
                (GovernmentRecord tie, string _) = Case("tie", clear, p => p == "SD" ? (0.25, 0.375) : p == "KD" ? (0.125, 0.25) : (double.NaN, double.NaN));
                Check(!larger.Cabinet.Contains("L") && larger.Cabinet.Contains("KD") && !tie.Cabinet.Contains("KD") && tie.Cabinet.Contains("SD"),
                    F("(4) one partner leaves a vote: L (25 points) over KD (12.5) - left {0}; SD and KD on equal margins - left {1} (the key first in ordinal order)",
                        larger.Cabinet.Contains("L") ? "NOT L" : "L, KD stays", tie.Cabinet.Contains("KD") ? "NOT KD" : "KD, SD stays"));
            }

            // (5) Germany
            void Germany()
            {
                DateTime day = WorldClock.StartDate(CountryId.Germany);
                var seats = new Dictionary<string, int> { { "CDU", 330 }, { "CSU", 60 }, { "SPD", 150 }, { "Grune", 80 }, { "FDP", 40 }, { "AfD", 50 }, { "Linke", 25 }, { "SSW", 1 }, { "BSW", 0 } };
                Func<string, (double, double)> fdpLeaves = p => p == "FDP" ? (0.04, 0.10) : (double.NaN, double.NaN);
                // (a) the CDU declared: the Linke sits in the cabinet, and the Union's resolution against it is a support-blocking declared line
                (SimulationManager sim, Country de) = Open("CoalitionExitDiagnostic.de", CountryId.Germany, day, "SSW");
                Seat(de, seats);
                GovernmentRecord g = Govern(de, "SPD", new DateTime(2021, 12, 8), "SPD", "Grune", "Linke", "FDP");
                bool cduDeclared = GovernmentFormation.RedLinedFrom(de, new List<string> { "SPD", "Grune", "Linke" }, day).Contains("CDU");
                int motions = Motions(de);
                using (CoalitionExit.PlantProjection(fdpLeaves)) { sim.CountBudgetVoteOnAgreements(de); }
                DivisionRecord motion = Motions(de) > motions ? de.Divisions.Entries[de.Divisions.Entries.Count - 1] : null;
                Check(cduDeclared && !ReferenceEquals(de.Government, g) && de.Government.PmParty == "CDU" && motion != null && motion.Title.Contains("Art. 67") && de.Government.Basis.Contains("Art. 67"),
                    F("(5) Germany: the CDU is declared against SPD+Grüne+Linke ({0}); the FDP leaves at the budget vote and the CDU's constructive vote elects its candidate - {1}; governing {2} led by {3}",
                        cduDeclared ? "the Union's resolution" : "NOT DECLARED", motion?.Title ?? "NO MOTION", string.Join("+", de.Government.Cabinet), de.Government.PmParty));
                // (b) the CDU not declared: SPD+Grüne alone - no declared party moves (the CSU moves only by its group's larger member), so the cabinet stays
                (SimulationManager sim2, Country de2) = Open("CoalitionExitDiagnostic.de.undeclared", CountryId.Germany, day, "SSW");
                Seat(de2, seats);
                GovernmentRecord g2 = Govern(de2, "SPD", new DateTime(2021, 12, 8), "SPD", "Grune", "FDP");
                bool cduUndeclared = !GovernmentFormation.RedLinedFrom(de2, new List<string> { "SPD", "Grune" }, day).Contains("CDU");
                int motions2 = Motions(de2);
                using (CoalitionExit.PlantProjection(fdpLeaves)) { sim2.CountBudgetVoteOnAgreements(de2); }
                Check(cduUndeclared && ReferenceEquals(de2.Government, g2) && !g2.Cabinet.Contains("FDP") && Motions(de2) == motions2 && Last(g2.Breaks).Contains("puts up no candidate the chamber elects"),
                    F("(5) K131's declared movers: with SPD+Grüne alone the CDU is not declared ({0}), and after the FDP leaves no constructive vote is moved - \"{1}\"",
                        cduUndeclared ? "no line" : "DECLARED", Last(g2.Breaks)));
            }

            // (6) Poland (K133)
            void Poland()
            {
                Check(ConfidenceProcedure.RulesOf(CountryId.Poland) == ConfidenceProcedure.Rules.Sejm && ConfidenceProcedure.IsConstructive(CountryId.Poland) && ConfidenceProcedure.SejmMovers == 46
                      && !ConfidenceProcedure.TakesMotions(CountryId.Poland),
                    "(6) K133: Poland's rule is the Sejm's constructive vote (Art. 158 Konstytucja, ElectionsData/poland/confidence_rules.md), moved by 46 deputies; no declaration waits there (K197 draws its verbs in the constructive row)");
                DateTime day = new DateTime(2024, 6, 1);
                (SimulationManager sim, Country pl) = Open("CoalitionExitDiagnostic.pl", CountryId.Poland, day, "TD");
                var system = new List<string>(); foreach (PoliticalParty p in PartySystems.For(CountryId.Poland)) { system.Add(p.Abbrev); }
                bool listed = system.Contains("PiS") && system.Contains("KO") && system.Contains("TD") && system.Contains("NL") && system.Contains("Konf");
                Check(listed, F("(6) the Sejm seated on {0:yyyy-MM-dd} lists PiS, KO, TD, NL and Konf: {1}", day, string.Join(", ", system)));
                if (!listed) { return; }
                // M2 (SP-7b): the player is the President, TD his party - the AI's party, so K130 weighs its exit as any partner's
                pl.PlayerHoldsPresidency = true;
                Seat(pl, new Dictionary<string, int> { { "PiS", 240 }, { "KO", 120 }, { "TD", 60 }, { "NL", 25 }, { "Konf", 15 } });
                GovernmentRecord g = Govern(pl, "KO", new DateTime(2023, 12, 13), "KO", "TD", "NL");
                bool pisDeclared = GovernmentFormation.RedLinedFrom(pl, new List<string> { "KO", "NL" }, day).Contains("PiS");
                int motions = Motions(pl);
                using (CoalitionExit.PlantProjection(p => p == "TD" ? (0.10, 0.16) : (double.NaN, double.NaN))) { sim.CountBudgetVoteOnAgreements(pl); }
                DivisionRecord motion = Motions(pl) > motions ? pl.Divisions.Entries[pl.Divisions.Entries.Count - 1] : null;
                sb.Append(F("    measured  (6) PiS declared against KO+NL: {0}\n", pisDeclared ? "yes" : "no"));
                Check(pisDeclared && !ReferenceEquals(pl.Government, g) && pl.Government.PmParty == "PiS" && motion != null && motion.Title.Contains("Art. 158 Konstytucja") && motion.Title.Contains("231 needed")
                      && pl.Government.Basis.Contains("Art. 158"),
                    F("(6) Poland: TD, the player-president's party and so the AI's, leaves KO+TD+NL at the budget vote, and PiS's constructive vote chooses its candidate - {0}; governing {1} led by {2}",
                        motion?.Title ?? "NO MOTION", string.Join("+", pl.Government.Cabinet), pl.Government.PmParty));
                // the player's own motion, deterministic: NL alone against a PiS cabinet - every other seat is PiS's, which votes against its own prime minister's successor
                (SimulationManager sim2, Country pl2) = Open("CoalitionExitDiagnostic.pl.short", CountryId.Poland, day, "NL");
                Seat(pl2, new Dictionary<string, int> { { "PiS", 435 }, { "NL", 25 } });
                Govern(pl2, "PiS", new DateTime(2023, 12, 13), "PiS");
                bool movedShort = sim2.MoveNoConfidence(CountryId.Poland, out string refusedShort, out _);
                Check(!movedShort && refusedShort == "A CONSTRUCTIVE VOTE NEEDS 46 DEPUTIES TO MOVE IT - ITS CANDIDATE HAS 25",
                    F("(6) Art. 158 ust. 1: NL's 25 cannot move it - \"{0}\"", refusedShort ?? "MOVED"));
                (SimulationManager sim3, Country pl3) = Open("CoalitionExitDiagnostic.pl.statutory", CountryId.Poland, day, "NL");
                Seat(pl3, new Dictionary<string, int> { { "PiS", 400 }, { "NL", 50 } });   // 450 seated: the statutory 460 still sets the majority
                GovernmentRecord g3 = Govern(pl3, "PiS", new DateTime(2023, 12, 13), "PiS");
                bool moved = sim3.MoveNoConfidence(CountryId.Poland, out string refused, out ConfidenceProcedure.MotionVote vote);
                Check(moved && vote != null && !vote.Carried && vote.For == 50 && vote.Members == 450 && vote.Needed == 231 && ReferenceEquals(pl3.Government, g3)
                      && vote.Title() == "Constructive vote of no confidence in the Council of Ministers (PiS): NL's candidate not elected, 50 of 450 deputies for, 231 needed (Art. 158 Konstytucja)",
                    F("(6) Art. 158 ust. 1 and L3: NL's 50 move it, and a majority of the statutory 460 is needed whatever is seated - {0}", vote?.Title() ?? refused ?? "NOTHING"));
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
