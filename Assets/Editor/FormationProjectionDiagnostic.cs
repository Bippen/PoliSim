using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// GATE-18 piece three, q20 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: "one formation code. The night's line names a projection
    /// ('EXPECTED TO FORM'), and office changes when the procedure completes"; the audit's GA-3-5): <b>THE NIGHT'S FORMATION IS THE PROCEDURE'S OWN,
    /// PROJECTED, AND THE NIGHT INSTALLS NOTHING.</b> The cheap half - no day is stepped (`Gate18Diagnostic` steps the idle games through Sweden 2026,
    /// Germany 2025 and Poland 2023 and holds each projection against what the procedure formed). FAILS while any is false:
    /// <list type="number">
    /// <item><description><b>Every country whose chamber the game counts has a procedure</b> that forms its government (the Riksdag's round, the Bundestag's
    /// chancellor election, the Sejm's steps): no counted chamber reaches the seating's fallback, which installs a formation on the night.</description></item>
    /// <item><description><b>The night installs nothing</b> (Sweden at its start, the player's party SD - GA-3-5's): the chamber as counted, the night's
    /// seating (`SimulationManager.SeatPlayerElection`) leaves the government in place and opens no round; the night projects by the round's code
    /// (`SimulationManager.ProjectFormation`), and its board's view names what is EXPECTED with the step it comes from.</description></item>
    /// <item><description><b>One code</b>: the round opened as the day after opens it asks in the projection's order, its first party first, and that
    /// party's draft is the projection's first draft; the government stays the outgoing one, serving as a caretaker.</description></item>
    /// <item><description><b>A projection moves no state</b> (the review's M2): the divisions, the government record and its breaks, the round and the Sejm's
    /// steps, the support agreements, the seats and every random stream are as they were after `ProjectFormation` - and a planted break and draw are seen,
    /// so the clause is shown to bite.</description></item>
    /// <item><description><b>Germany and Poland</b> project too (the Bundestag's first ballot; the Sejm's steps), on their starts' chambers.</description></item>
    /// <item><description>D-NIGHT (Design's 31a-31c, "For Code" 1-2, 8): <b>the board's view carries the vote party by party and the round's cells</b> - every
    /// seated party on one side, the sides summing to the for / abstaining / against counts and those to the chamber (the Riksdag's against counting to the
    /// refusal line at a majority of the members, the Bundestag's for to a majority of the members; Italy's once per chamber), and the cells the procedure's
    /// limit long, the refused before the current and the steps not reached after it. A planted vote whose sides do not sum is refused, so the clause bites.</description></item>
    /// </list>
    /// Prints `=== FormationProjectionDiagnostic: q20 holds ===` and exits 0. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.FormationProjectionDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class FormationProjectionDiagnostic
    {
        /// <summary>The play key the films and the fork checks seed with.</summary>
        private const int Seed = 777;
        private static readonly MethodInfo SeatMethod = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        /// <summary>D-NIGHT: why a night's vote does not hold together, or null where it does - every party on one side, the sides summing to the counts
        /// (<paramref name="sidesSumToMembers"/>: and the counts to the chamber), the line the rule counts.</summary>
        private static string VoteFault(GovernmentFormation.NightVote v, bool countsAgainst, int needed, bool sidesSumToMembers)
        {
            if (v == null) { return "no vote"; }
            int f = 0, a = 0, x = 0;
            foreach (GovernmentFormation.VoteSide s in v.Sides)
            {
                if (s.Seats <= 0) { return "a seatless party on a side (" + s.Abbrev + ")"; }
                if (s.Side == CoalitionFormation.InvestitureSide.InCabinet || s.Side == CoalitionFormation.InvestitureSide.Supports) { f += s.Seats; }
                else if (s.Side == CoalitionFormation.InvestitureSide.Against) { x += s.Seats; }
                else { a += s.Seats; }
            }
            if (v.Sides.Count == 0) { return "no party on any side"; }
            if (sidesSumToMembers && (f != v.For || a != v.Abstain || x != v.Against)) { return F("the sides sum to {0} for, {1} abstaining, {2} against - the counts say {3}, {4}, {5}", f, a, x, v.For, v.Abstain, v.Against); }
            if (sidesSumToMembers && v.For + v.Abstain + v.Against != v.Members) { return F("the counts sum to {0}, not the chamber's {1}", v.For + v.Abstain + v.Against, v.Members); }
            if (v.CountsAgainst != countsAgainst || v.Needed != needed) { return F("the line is {0} {1}, the rule's {2} {3}", v.Needed, v.CountsAgainst ? "against" : "for", needed, countsAgainst ? "against" : "for"); }
            return null;
        }

        /// <summary>D-NIGHT: why a night's round does not hold together, or null - its limit long, the refused before the one current (where a government is
        /// expected) and the steps not reached after it.</summary>
        private static string RoundFault(GovernmentFormation.View view, int limit, bool electedWithoutGovernment = false)
        {
            if (view.RoundLimit != limit || view.Round.Count != limit) { return F("{0} cells of a limit {1}, the procedure's {2}", view.Round.Count, view.RoundLimit, limit); }
            int current = view.Round.FindIndex(c => c.State == GovernmentFormation.RoundCellState.Current);
            if ((view.HasGovernment || electedWithoutGovernment) != (current >= 0)) { return current < 0 ? (view.HasGovernment ? "a government is expected and no cell is current" : "the candidate is elected and no cell is current") : "no government is expected and a cell is current"; }
            if (current >= 0 && view.Round.FindIndex(current + 1, c => c.State != GovernmentFormation.RoundCellState.NotReached) >= 0) { return "a step after the current one is reached"; }
            bool reached = true;
            for (int i = 0; i < view.Round.Count; i++)
            {
                GovernmentFormation.RoundCell c = view.Round[i];
                if (c.State == GovernmentFormation.RoundCellState.NotReached) { reached = false; continue; }
                if (!reached) { return F("cell {0} is {1} after a step not reached", i + 1, c.State); }
                if (c.Asked == null && c.State == GovernmentFormation.RoundCellState.Current) { return F("cell {0} is current and names no party", i + 1); }   // a refused step may name none (Italy's mandate that forms no government)
                if (current >= 0 && i < current && c.State != GovernmentFormation.RoundCellState.Refused) { return F("cell {0} before the current is {1}", i + 1, c.State); }
            }
            return null;
        }

        /// <summary>D-NIGHT: why one chamber's night vote does not hold beside its view, or null - the vote's own sums and line, the Riksdag's against being the
        /// investiture's, and carried where a government is expected (the review's L1: the Bundestag may elect a candidate on whose party no government is drawn).</summary>
        private static string NightFault(GovernmentFormation.View view, GovernmentFormation.NightVote nv, bool bundestag)
        {
            string fault = VoteFault(nv, view.NegativeRule && !bundestag, view.Majority, true);
            if (fault != null) { return fault; }
            if (view.HasGovernment && !bundestag && nv.Against != view.OpposedSeats) { return F("{0} against on the bar, {1} in the investiture", nv.Against, view.OpposedSeats); }
            if (nv.ElectedWithoutGovernment && !(bundestag && nv.Carried && !view.HasGovernment)) { return "the elected-without-government mark outside its case (a carried Bundestag ballot with no government)"; }   // review 3's N12: refused, not ignored
            bool electedWithoutGovernment = bundestag && nv.ElectedWithoutGovernment && nv.Carried && !view.HasGovernment;   // the review-2 N8: marked by the projection, never inferred
            if (view.HasGovernment != nv.Carried && !electedWithoutGovernment) { return F("carried {0} where a government is {1}expected", nv.Carried, view.HasGovernment ? string.Empty : "not "); }
            return null;
        }

        /// <summary>D-NIGHT: why Italy's night votes do not hold, or null - two, the Camera's then the Senato's, each summing to its chamber with its line at art. 64's
        /// "more for than against" (against + 1), all carried where a government is expected and not all where none is.</summary>
        private static string ItalyVotesFault(List<GovernmentFormation.NightVote> votes, bool expected)
        {
            if (votes.Count != 2) { return F("{0} votes, not two", votes.Count); }
            if (votes[0].Chamber != "Camera" || votes[1].Chamber != "Senato") { return "the chambers are " + votes[0].Chamber + ", " + votes[1].Chamber + " - the Camera's then the Senato's"; }
            foreach (GovernmentFormation.NightVote v in votes)
            {
                string fault = VoteFault(v, false, v.Against + 1, true);
                if (fault != null) { return v.Chamber + ": " + fault; }
            }
            if (expected ? !votes.TrueForAll(v => v.Carried) : votes.TrueForAll(v => v.Carried)) { return expected ? "a government is expected and a chamber refuses" : "no government is expected and both chambers carry"; }
            return null;
        }

        /// <summary>The review's M2: why a vote's declines do not hold, or null - each decliner off the for side, a reason for the fall, and the vote not carried.</summary>
        private static string DeclineFault(GovernmentFormation.NightVote v)
        {
            if (v.Decliners.Count == 0 && v.Withdrew.Count == 0) { return null; }
            foreach (string d in v.Decliners.Concat(v.Withdrew))
            {
                GovernmentFormation.VoteSide s = v.Sides.Find(x => x.Abbrev == d);
                if (s.Abbrev == null) { return d + " refuses and holds no side"; }
                if (s.Side == CoalitionFormation.InvestitureSide.InCabinet || s.Side == CoalitionFormation.InvestitureSide.Supports) { return d + " refuses and is still counted for"; }
            }
            if (string.IsNullOrEmpty(v.Fell)) { return "a refusal with no reason for the fall"; }
            if (v.Carried) { return "a refusal and the vote carried"; }
            return null;
        }

        private static GovernmentFormation.NightVote Copy(GovernmentFormation.NightVote v, Action<GovernmentFormation.NightVote> change)
        {
            var c = new GovernmentFormation.NightVote { Chamber = v.Chamber, Members = v.Members, Needed = v.Needed, CountsAgainst = v.CountsAgainst, For = v.For, Abstain = v.Abstain, Against = v.Against, Carried = v.Carried, Fell = v.Fell, ElectedWithoutGovernment = v.ElectedWithoutGovernment };
            c.Sides.AddRange(v.Sides);
            c.Decliners.AddRange(v.Decliners);
            c.Withdrew.AddRange(v.Withdrew);
            change?.Invoke(c);
            return c;
        }

        /// <summary>D-NIGHT (the review's H1): the column's tallest cases per country, summed block by block through `NightColumnPlan.PlanColumn` - each must
        /// come to 516 px or under after the shrink order; and without the shrink order Italy's tallest exceeds it, so the clause is shown to bite.</summary>
        private static void ColumnHeights(Action<bool, string> check)
        {
            var cases = new List<ElectionNightScreen.ColumnCase>();
            foreach (CountryId c in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Italy })
            {
                bool budget = c != CountryId.Germany;
                int lanes = c == CountryId.Italy ? 2 : 1;
                foreach (int effects in budget ? new[] { 6, 7 } : new[] { 0 })
                {
                    cases.Add(new ElectionNightScreen.ColumnCase { Country = c, SupportRow = true, Lanes = lanes, Key = true, K119 = true, Budget = budget, Effects = effects });
                    cases.Add(new ElectionNightScreen.ColumnCase { Country = c, NoneExpected = true, ReasonRows = 2, Lanes = lanes, Key = true, K119 = true, Budget = budget, Effects = effects });
                }
                cases.Add(new ElectionNightScreen.ColumnCase { Country = c, Counting = true, Budget = budget, Effects = budget ? 7 : 0 });
            }
            foreach (ElectionNightScreen.ColumnCase k in cases)
            {
                ElectionNightScreen.ColumnPlan plan = NightColumnPlan.PlanColumn(k);
                check(plan.Final <= ElectionNightScreen.ColumnPx, F("the column fits its {0} px: {1} - {2} natural, {3} drawn{4}", ElectionNightScreen.ColumnPx, k, plan.Natural, plan.Final,
                    plan.Shrunk.Count > 0 ? " (" + string.Join("; ", plan.Shrunk) + ")" : string.Empty));
            }
            var tallest = new ElectionNightScreen.ColumnCase { Country = CountryId.Italy, NoneExpected = true, ReasonRows = 2, Lanes = 2, Key = true, K119 = true, Budget = true, Effects = 6 };
            ElectionNightScreen.ColumnPlan unshrunk = NightColumnPlan.PlanColumn(tallest, shrink: false);
            check(unshrunk.Final > ElectionNightScreen.ColumnPx, F("the height clause bites - without the shrink order Italy's tallest is {0} px, over {1}", unshrunk.Final, ElectionNightScreen.ColumnPx));
        }

        private static string Sides(CountryId id, GovernmentFormation.NightVote v) =>
            v == null ? "none" : string.Join(" · ", v.Sides.Select(s => PartySystems.ShortName(id, s.Abbrev) + " " + s.Seats.ToString(CultureInfo.InvariantCulture) + " " + s.Side + " (" + s.Reason + ")"));

        /// <summary>The review's M2: what <paramref name="act"/> moved - the government record (the same object, its cabinet, version and formation day, its
        /// breaks, its support agreements' items), the round and the Sejm's steps (the same objects, their stages, logs and the Sejm's cabinet), the divisions,
        /// the seats, the master seed and every random stream's draw count. The streams are put back as they were found: the live objects where nothing drew,
        /// rebuilt at the counts before where something did. True where anything moved, named in <paramref name="moves"/>.</summary>
        private static bool Moved(SimulationManager s, Country country, Action act, out string moves)
        {
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            int master = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> c0 = SimulationRandom.CaptureDrawCounts();
            GovernmentRecord g0 = country.Government;
            SpeakerRound r0 = s.RoundOf(country.Id);
            SejmFormation f0 = g0?.Sejm;
            Dictionary<string, string> before = Signature(s, country);
            act();
            Dictionary<string, string> after = Signature(s, country);
            Dictionary<SimulationRandom.Stream, int> c1 = SimulationRandom.CaptureDrawCounts();
            var moved = new List<string>();
            if (!ReferenceEquals(country.Government, g0)) { moved.Add("the government record"); }
            if (!ReferenceEquals(s.RoundOf(country.Id), r0)) { moved.Add("the round"); }
            if (!ReferenceEquals(country.Government?.Sejm, f0)) { moved.Add("the Sejm's steps"); }
            foreach (KeyValuePair<string, string> kv in before) { if (after[kv.Key] != kv.Value) { moved.Add(kv.Key + " (" + kv.Value + " -> " + after[kv.Key] + ")"); } }
            // the delta review's L-b: the streams are a COUNTS check (each stream's draws, before and after) plus the master seed - a callee that reseeds the
            // generator with its counts unchanged is reported, where putting the live streams back would silently repair it
            var streams = new List<string>();
            int masterAfter = SimulationRandom.MasterSeed;
            if (masterAfter != master) { streams.Add(F("the master seed {0} -> {1}", master, masterAfter)); }
            foreach (SimulationRandom.Stream st in (SimulationRandom.Stream[])Enum.GetValues(typeof(SimulationRandom.Stream)))
            {
                int n0 = c0.TryGetValue(st, out int x0) ? x0 : 0, n1 = c1.TryGetValue(st, out int x1) ? x1 : 0;
                if (n0 != n1) { streams.Add(F("{0} {1} -> {2} draws", st, n0, n1)); }
            }
            if (streams.Count > 0) { moved.Add("streams: " + string.Join(", ", streams)); SimulationRandom.RestoreState(master, c0); }
            else { SimulationRandom.Reinstate(live); }
            moves = moved.Count > 0 ? string.Join("; ", moved) : "nothing";
            return moved.Count > 0;
        }

        private static Dictionary<string, string> Signature(SimulationManager s, Country country)
        {
            GovernmentRecord g = country.Government;
            SpeakerRound r = s.RoundOf(country.Id);
            SejmFormation f = g?.Sejm;
            List<DivisionRecord> d = country.Divisions.Entries;
            var agreements = new List<string>();
            if (g != null) { foreach (SupportAgreement a in g.Agreements) { agreements.Add(a.Supporter + ":" + string.Join(",", a.Items.Select(i => i.Kind + "/" + i.State))); } }
            return new Dictionary<string, string>
            {
                ["divisions"] = d.Count + "|" + (d.Count > 0 ? d[d.Count - 1].Title : "-"),
                ["the cabinet"] = g == null ? "none" : string.Join("+", g.Cabinet) + "/" + string.Join("+", g.Support) + "/" + g.PmParty + "/" + g.Caretaker + "/v" + g.Version + "/" + g.FormedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["breaks"] = g == null ? "none" : g.Breaks.Count + "|" + (g.Breaks.Count > 0 ? g.Breaks[g.Breaks.Count - 1] : "-"),
                ["agreements"] = string.Join(";", agreements),
                ["the round's stage"] = r == null ? "none" : r.Stage + "|" + r.Asked + "|" + r.Rejections + "|" + r.Log.Count + "|" + r.Declines.Count + "|" + (r.Proposal?.Formateur ?? "-"),
                ["the Sejm's stage"] = f == null ? "none" : f.Stage + "|" + f.NextOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + f.Log.Count + "|" + f.PmParty + "|" + string.Join("+", f.Cabinet) + "/" + string.Join("+", f.Support),
                ["seats"] = string.Join(",", country.ParliamentSeats.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "=" + kv.Value)),
            };
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FormationProjectionDiagnostic (GATE-18 q20): the night's formation is the procedure's own, projected ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            try
            {
                // (1) every counted chamber has its procedure
                var counted = new List<string>();
                var orphans = new List<string>();
                foreach (CountryId c in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    using (SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(c);
                        if (!NationalElection.TryPredictShares(c, out Dictionary<string, double> shares)) { continue; }
                        ElectionRecord record = NationalElection.Run(c, 0, shares);
                        if (!NationalElection.CountsParliament(record.Method)) { continue; }
                        var probe = new GameObject("FormationProjectionDiagnostic.probe"); hosts.Add(probe);
                        SimulationManager m = probe.AddComponent<SimulationManager>();
                        m.PlayerCountryId = c;
                        string procedure = m.RoundsApply(c) ? (ConfidenceProcedure.RulesOf(c) == ConfidenceProcedure.Rules.Bundestag ? "the chancellor's election" : "the Speaker's round")
                            : m.SejmStepsApply(c) ? "the Sejm's steps" : m.ItalianMandatesApply(c) ? "the President's mandates" : null;   // PS-7 (K204)
                        counted.Add(F("{0} ({1}): {2}", c, record.Method, procedure ?? "NONE"));
                        if (procedure == null) { orphans.Add(c.ToString()); }
                    }
                }
                Check(counted.Count > 0 && orphans.Count == 0, "every chamber the game counts has a procedure forming its government, so no night installs one: " + string.Join("; ", counted)
                    + (orphans.Count > 0 ? " - NONE FOR " + string.Join(", ", orphans) : string.Empty));

                // D-NIGHT (the review's H1): the column's tallest cases fit its rect, by the board's own arithmetic
                ColumnHeights(Check);

                // (2)-(3) Sweden at its start, the player's party SD
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy })   // PS-7 (K204): Italy's mandates
                {
                    using (SimulationManager.EpochScope())
                    {
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        WorldClock.ApplyStart(id);
                        World w = WorldFactory.CreateDefault();
                        var host = new GameObject("FormationProjectionDiagnostic." + id); hosts.Add(host);
                        SimulationManager s = host.AddComponent<SimulationManager>();
                        s.SetWorld(w);
                        s.PlayerCountryId = id;
                        Country country = w.GetCountry(id);
                        country.PlayerPartyAbbrev = id == CountryId.Sweden ? "SD" : country.Government?.PmParty;
                        SeatMethod?.Invoke(s, null);
                        GovernmentRecord before = country.Government;
                        DateTime day = s.CurrentDate;
                        var record = new ElectionRecord
                        {
                            Turn = s.CurrentTurn, Date = day, CountryId = id.ToString(),
                            Method = id == CountryId.Sweden ? ElectionMethod.SwedenTwoTier : id == CountryId.Germany ? ElectionMethod.GermanyNationalProportional : id == CountryId.Italy ? ElectionMethod.ItalyRosatellum : ElectionMethod.PolandDistricts,
                        };
                        foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { record.Seats[kv.Key] = kv.Value; }
                        country.ElectionHistory.Add(record);
                        if (id == CountryId.Italy) { country.SecondChamberSeats = new Dictionary<string, int>(country.ParliamentSeats); }   // PS-7: a Senato planted as the seated Camera, so the mandates have both chambers to stand before
                        s.SeatPlayerElection(record, out DeclarationReading _, out GovernmentFormation.Formed _, out SejmFormation sejm);
                        bool untouched = before != null && ReferenceEquals(country.Government, before) && (country.Government.Round == null || !country.Government.Round.Open);

                        // the review's M2: a projection moves no state - the world's every record the procedure writes, and the random streams, before and after
                        // (the streams captured live and put back, so the probe draws leave the run as they found it); planted first, so the clause is shown to bite
                        Moved(s, country, () =>
                        {
                            country.Government.Breaks.Add("planted by FormationProjectionDiagnostic");
                            SimulationRandom.For(SimulationRandom.Stream.Parliament).Next();
                        }, out string plantedMoves);
                        country.Government.Breaks.RemoveAt(country.Government.Breaks.Count - 1);
                        Check(plantedMoves.Contains("breaks") && plantedMoves.Contains("streams"),
                            F("{0}: the no-mutation clause bites - a planted break and a planted draw are seen ({1})", id, plantedMoves));
                        FormationProjection p = null;
                        bool still = !Moved(s, country, () => { p = s.ProjectFormation(country, day); }, out string moves);
                        Check(still, F("{0}: the projection moves no state - the divisions, the government, its breaks, the round and the Sejm's steps, the support agreements, the seats and every random stream as they were{1}",
                            id, still ? string.Empty : " - MOVED: " + moves));
                        Check(untouched && p != null, F("{0}, the player's party {1}: the night installs nothing and opens no round - {2} led by {3} governs on{4}; the night projects {5}",
                            id, country.PlayerPartyAbbrev ?? "none", before != null ? string.Join("+", before.Cabinet) : "none", before?.PmParty ?? "-", id == CountryId.Poland ? F(" (the Sejm's steps open: {0})", sejm != null && sejm.Open) : string.Empty,
                            p?.Describe() ?? "NOTHING"));
                        if (p == null) { continue; }
                        foreach (string step in p.Steps) { sb.Append("              ").Append(step).Append('\n'); }
                        if (id == CountryId.Poland)
                        {
                            Check(p.View == null && (p.Expected ? p.Cabinet.Count > 0 && p.PmParty != null && p.Step != null : p.Reason != null),
                                F("Poland: the steps' projection names {0}", p.Expected ? F("a cabinet, its prime minister's party and its step ({0})", p.Step) : "why none is expected"));
                            continue;
                        }
                        if (id == CountryId.Italy)
                        {
                            // PS-7 (K204): the mandates' own projection - what ItalianFormation.Run forms on the same chambers under the same reading, and the board's
                            // view of it (the step line, the Camera's seats, Italy's foot and words)
                            List<ItalianFormation.Mandate> mandates = ItalianFormation.Run(country, DeclarationReading.OfElection(id, day), out ItalianFormation.Mandate granted);
                            bool same = granted != null
                                ? p.Expected && p.PmParty == granted.Camera.PmParty && new HashSet<string>(p.Cabinet).SetEquals(granted.View.Cabinet.Select(x => x.Abbrev)) && new HashSet<string>(p.Support).SetEquals(granted.View.Support.Select(x => x.Abbrev))
                                  && p.Step != null && p.View != null && p.View.HasGovernment && p.View.ProjectionStep == p.Step && p.View.Cabinet.Count == p.Cabinet.Count && p.View.ProjectionVote != null
                                : !p.Expected && p.Reason != null && p.View != null && !p.View.HasGovernment && p.View.NoneWords != null && p.View.ProjectionVote != null;
                            Check(same && p.Steps.Count >= mandates.Count,
                                F("Italy: the projection is what the mandates form - {0}; the board's view carries {1}", p.Expected ? F("{0} led by {1} ({2})", string.Join("+", p.Cabinet), p.PmParty, p.Step) : "none: " + p.Reason,
                                    p.View == null ? "NOTHING" : p.View.HasGovernment ? "the step line and the foot's vote (" + p.View.ProjectionVote + ")" : "Italy's words (" + p.View.NoneWords + ")"));
                            Check(ReferenceEquals(country.Government, before), "Italy: office has not changed - the outgoing government serves on until the mandates run on the first sitting");
                            if (p.View != null)
                            {
                                // D-NIGHT ("For Code" 8): art. 94 drawn once per chamber - the Camera's and the Senato's divisions of the mandate voted last, and the
                                // mandates as the round's cells (the division's own counts: those not voting are not a side, so the sides need not fill the chamber)
                                string itf = p.View.Votes.Count == 0 ? (mandates.TrueForAll(m => m.Camera == null) ? null : "no vote where a mandate was voted on") : ItalyVotesFault(p.View.Votes, p.Expected);
                                Check(itf == null, F("Italy: the night's votes, once per chamber - {0}{1}", string.Join(" / ", p.View.Votes.Select(v => F("{0} {1} for, {2} against, {3} needed: {4}", v.Chamber, v.For, v.Against, v.Needed, Sides(id, v)))),
                                    itf == null ? string.Empty : " - " + itf));
                                string rf = RoundFault(p.View, ConfidenceProcedure.ItalianMandates);
                                Check(rf == null, F("Italy: the night's mandates - {0}{1}", string.Join(" · ", p.View.Round.Select(c => (c.Asked ?? "-") + " " + c.State)), rf == null ? string.Empty : " - " + rf));
                                if (p.View.Votes.Count == 2)
                                {
                                    // the review's M4: each Italian clause bites - a count one seat over its sides, the chambers swapped, a line that is not art. 64's
                                    var over = new List<GovernmentFormation.NightVote> { Copy(p.View.Votes[0], c => c.For += 1), p.View.Votes[1] };
                                    var swapped = new List<GovernmentFormation.NightVote> { p.View.Votes[1], p.View.Votes[0] };
                                    var majority = new List<GovernmentFormation.NightVote> { Copy(p.View.Votes[0], c => c.Needed = c.Members / 2 + 1 == c.Against + 1 ? c.Members / 2 + 2 : c.Members / 2 + 1), p.View.Votes[1] };
                                    Check(ItalyVotesFault(over, p.Expected) != null && ItalyVotesFault(swapped, p.Expected) != null && ItalyVotesFault(majority, p.Expected) != null,
                                        "Italy: the votes' clauses bite - a Camera one seat over its sides, the chambers swapped and a majority-of-the-members line are each refused");
                                }
                                ElectionNightScreen.ColumnPlan ip = NightColumnPlan.PlanColumn(NightColumnPlan.WorstCaseOf(id, p.View));
                                Check(ip.Final <= ElectionNightScreen.ColumnPx, F("Italy: the projected night's column at its worst fits {0} px - {1} natural, {2} drawn{3}", ElectionNightScreen.ColumnPx, ip.Natural, ip.Final,
                                    ip.Shrunk.Count > 0 ? " (" + string.Join("; ", ip.Shrunk) + ")" : string.Empty));
                            }
                            continue;
                        }
                        GovernmentFormation.View view = p.View;
                        Check(view != null && (view.HasGovernment ? view.ProjectionStep != null && view.Cabinet.Count == p.Cabinet.Count : view.Reason != null) && view.ReadsPlayersAnswer == p.ReadsPlayersAnswer,
                            F("{0}: the board's view is the projection's - {1}{2}", id, view == null ? "NONE" : view.HasGovernment ? "EXPECTED " + view.ProjectionStep : "NONE EXPECTED: " + view.Reason,
                                p.ReadsPlayersAnswer ? " (it reads the player's party's answer as the AI's rule gives it)" : string.Empty));

                        // D-NIGHT ("For Code" 1-2, 8): the board's view carries the vote party by party and the round's cells
                        if (view != null)
                        {
                            bool bundestag = id == CountryId.Germany;
                            bool voted = view.HasGovernment || view.Round.Exists(c => c.State != GovernmentFormation.RoundCellState.NotReached);
                            GovernmentFormation.NightVote nv = view.Votes.Count > 0 ? view.Votes[0] : null;
                            string vf = !voted ? (view.Votes.Count == 0 ? null : "a vote where no proposal was voted on")
                                : view.Votes.Count != 1 ? F("{0} votes, not one", view.Votes.Count)
                                : NightFault(view, nv, bundestag);
                            Check(vf == null, F("{0}: the night's vote party by party ({1}) - {2}{3}{4}", id, nv == null ? "none" : F("{0} for, {1} abstaining, {2} against; the line {3} {4} of {5}", nv.For, nv.Abstain, nv.Against, nv.Needed, nv.CountsAgainst ? "against" : "for", nv.Members),
                                Sides(id, nv), nv != null && nv.Decliners.Count > 0 ? " - fell on " + string.Join(", ", nv.Decliners) + " declining (" + nv.Fell + ")" : string.Empty, vf == null ? string.Empty : " - " + vf));
                            bool electedWithoutGovernment = bundestag && nv != null && nv.ElectedWithoutGovernment && nv.Carried && !view.HasGovernment;
                            string rf = RoundFault(view, bundestag ? 3 : SpeakerRound.ProposalLimit, electedWithoutGovernment);
                            Check(rf == null, F("{0}: the night's round - {1}{2}", id, string.Join(" · ", view.Round.Select(c => (c.Asked ?? "-") + " " + c.State)), rf == null ? string.Empty : " - " + rf));

                            // the review's M4: every clause bites on a plant made to break it alone
                            int limit = bundestag ? 3 : SpeakerRound.ProposalLimit;
                            var roundPlant = new GovernmentFormation.View { HasGovernment = true, RoundLimit = limit };
                            roundPlant.Round.Add(new GovernmentFormation.RoundCell("X", GovernmentFormation.RoundCellState.Current));
                            roundPlant.Round.Add(new GovernmentFormation.RoundCell("Y", GovernmentFormation.RoundCellState.Refused));
                            while (roundPlant.Round.Count < limit) { roundPlant.Round.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached)); }
                            Check(RoundFault(roundPlant, limit) != null, F("{0}: the round's clause bites - a refused step after the current one is refused", id));
                            if (nv != null)
                            {
                                Check(VoteFault(Copy(nv, c => c.For += 1), nv.CountsAgainst, nv.Needed, true) != null, F("{0}: the vote's clause bites - a planted count one seat over its sides is refused", id));
                                var expectedPlant = new GovernmentFormation.View { HasGovernment = true, NegativeRule = view.NegativeRule, Majority = view.Majority, OpposedSeats = nv.Against };
                                Check(NightFault(expectedPlant, Copy(nv, c => c.Carried = false), bundestag) != null, F("{0}: the carried clause bites - a vote not carried under an expected government is refused", id));
                                if (!bundestag && nv.CountsAgainst)
                                {
                                    var opposedPlant = new GovernmentFormation.View { HasGovernment = true, NegativeRule = true, Majority = view.Majority, OpposedSeats = nv.Against + 1 };
                                    Check(NightFault(opposedPlant, Copy(nv, c => c.Carried = true), false) != null, F("{0}: the against clause bites - a bar whose against is not the investiture's is refused", id));
                                }
                                // the review-2 N8: a carried Bundestag vote with no government is refused unless the projection marked it elected without one
                                if (bundestag)
                                {
                                    var noGovernment = new GovernmentFormation.View { HasGovernment = false, NegativeRule = false, Majority = view.Majority };
                                    Check(NightFault(noGovernment, Copy(nv, c => { c.Carried = true; c.ElectedWithoutGovernment = false; }), true) != null
                                          && NightFault(noGovernment, Copy(nv, c => { c.Carried = true; c.ElectedWithoutGovernment = true; }), true) == null,
                                        "Germany: the elected-without-government case holds only where marked - an unmarked carried ballot with no government is refused");
                                }
                                // the review's M2 (review 2's N1): a proposal that falls on a decline - every decliner off the for side, the reason the verdict's; and a
                                // planted decline shows it (Fell set, the decliner abstaining, for down by its seats, the sides still summing, against untouched)
                                string df = DeclineFault(nv);
                                Check(df == null, F("{0}: the night's declines - {1}{2}", id, nv.Decliners.Count == 0 ? "none" : string.Join(", ", nv.Decliners) + " (" + nv.Fell + ")", df == null ? string.Empty : " - " + df));
                                int cab = nv.Sides.FindIndex(s => s.Side == CoalitionFormation.InvestitureSide.InCabinet);
                                if (!bundestag && cab >= 0)
                                {
                                    GovernmentFormation.VoteSide partner = nv.Sides[cab];
                                    GovernmentFormation.NightVote planted = Copy(nv, c => { c.Decline(partner.Abbrev, "planted by FormationProjectionDiagnostic"); c.Carried = false; c.Fell = "planted: an invited party refuses"; });
                                    GovernmentFormation.VoteSide after = planted.Sides.Find(s => s.Abbrev == partner.Abbrev);
                                    bool shows = planted.Fell != null && planted.Decliners.Contains(partner.Abbrev) && after.Side == CoalitionFormation.InvestitureSide.Abstains
                                                 && planted.For == nv.For - partner.Seats && planted.Against == nv.Against && VoteFault(planted, nv.CountsAgainst, nv.Needed, true) == null && DeclineFault(planted) == null;
                                    // review 3's N10: one plant per clause, each from the valid planted decline and failing alone
                                    bool stillFor = DeclineFault(Copy(nv, c => { c.Decliners.Add(partner.Abbrev); c.Fell = "planted"; c.Carried = false; })) != null;
                                    bool noFell = DeclineFault(Copy(planted, c => c.Fell = null)) != null;
                                    bool carried = DeclineFault(Copy(planted, c => c.Carried = true)) != null;
                                    bool noSide = DeclineFault(Copy(planted, c => c.Decliners.Add("NO-SUCH-PARTY"))) != null;
                                    Check(shows && stillFor && noFell && carried && noSide, F("{0}: the decline clauses bite - a planted decline of {1} shows the fall and leaves {1} out of for; each alone refused: still for {2}, no reason {3}, carried {4}, no side {5}",
                                        id, partner.Abbrev, stillFor, noFell, carried, noSide));
                                    // review 3's N9: a refusing supporter, planted, is drawn as it stands with its reason and the fall
                                    int outside = nv.Sides.FindIndex(s => s.Side != CoalitionFormation.InvestitureSide.InCabinet);
                                    if (outside >= 0)
                                    {
                                        string who = nv.Sides[outside].Abbrev;
                                        GovernmentFormation.NightVote withdrew = Copy(nv, c => { c.Withdraw(who, "planted by FormationProjectionDiagnostic"); c.Carried = false; c.Fell = "planted: an invited supporter refuses"; });
                                        GovernmentFormation.VoteSide ws = withdrew.Sides.Find(s => s.Abbrev == who);
                                        Check(withdrew.Withdrew.Contains(who) && ws.Side != CoalitionFormation.InvestitureSide.InCabinet && ws.Side != CoalitionFormation.InvestitureSide.Supports && ws.Reason.StartsWith("withdraws its support", StringComparison.Ordinal)
                                              && VoteFault(withdrew, nv.CountsAgainst, nv.Needed, true) == null && DeclineFault(withdrew) == null && DeclineFault(Copy(withdrew, c => c.Fell = null)) != null,
                                            F("{0}: a planted withdrawal of {1}'s support is drawn as it stands ({2}) with its reason and the fall; without a reason it is refused", id, who, ws.Side));
                                    }
                                    // review 3's N12: the elected-without-government mark outside its case is refused, not ignored
                                    Check(NightFault(view, Copy(nv, c => c.ElectedWithoutGovernment = true), false) != null, F("{0}: the elected-without-government mark on a Riksdag vote is refused", id));
                                }
                            }
                            ElectionNightScreen.ColumnPlan cp = NightColumnPlan.PlanColumn(NightColumnPlan.WorstCaseOf(id, view));
                            Check(cp.Final <= ElectionNightScreen.ColumnPx, F("{0}: the projected night's column at its worst fits {1} px - {2} natural, {3} drawn{4}", id, ElectionNightScreen.ColumnPx, cp.Natural, cp.Final,
                                cp.Shrunk.Count > 0 ? " (" + string.Join("; ", cp.Shrunk) + ")" : string.Empty));
                        }

                        // (3) one code: the round as the day after opens it
                        s.OpenSpeakerRound(country, WorldClock.VintageOfElection(id, day), F("after the election of {0:yyyy-MM-dd}", day), electionDay: day);
                        SpeakerRound round = country.Government?.Round;
                        FormationProposal draft = round != null && round.Asked != null ? s.DraftProposal(country, round, round.Asked) : null;
                        Check(round != null && round.Order.SequenceEqual(p.Order) && round.Asked == p.FirstAsked && draft != null && new HashSet<string>(draft.CabinetParties).SetEquals(p.FirstDraft),
                            F("{0}: one code - the round asks in the projection's order ({1}), {2} first, and its draft {3} is the projection's first ({4})", id, string.Join(", ", round?.Order ?? new List<string>()), round?.Asked ?? "none",
                                draft != null ? string.Join("+", draft.CabinetParties) : "none", string.Join("+", p.FirstDraft)));
                        Check(ReferenceEquals(country.Government, before),
                            F("{0}: office has not changed - the outgoing government serves on{1} until the procedure completes", id, before.Caretaker ? " as a caretaker" : " (the Bundestag's office ends when it convenes, Art. 69 Abs. 2)"));
                    }
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally
            {
                foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); }
                EnergyMarket.ResetTurnState();
                // the predictions derive the regions as a side effect: derive Sweden's again at the process epoch, so no check after this reads another country's
                NationalElection.TryPredictShares(CountryId.Sweden, out _);
            }

            if (failures > 0) { Debug.LogError($"FORMATION PROJECTION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            sb.Append("=== FormationProjectionDiagnostic: q20 holds ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
