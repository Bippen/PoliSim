using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                            continue;
                        }
                        GovernmentFormation.View view = p.View;
                        Check(view != null && (view.HasGovernment ? view.ProjectionStep != null && view.Cabinet.Count == p.Cabinet.Count : view.Reason != null) && view.ReadsPlayersAnswer == p.ReadsPlayersAnswer,
                            F("{0}: the board's view is the projection's - {1}{2}", id, view == null ? "NONE" : view.HasGovernment ? "EXPECTED " + view.ProjectionStep : "NONE EXPECTED: " + view.Reason,
                                p.ReadsPlayersAnswer ? " (it reads the player's party's answer as the AI's rule gives it)" : string.Empty));

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
