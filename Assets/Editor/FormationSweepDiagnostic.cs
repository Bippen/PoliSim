using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §646 (the formateur's evaluator): THE FORMATION'S WHOLE ANSWER OVER A SWEEP OF CHAMBERS, as text, with its digest - so a refactor of
    /// <see cref="CoalitionFormation.Form"/> is proved to reproduce it exactly by the diff of two runs, not by reading the code (the standing rule
    /// on inertness). The sweep: Sweden's seed, 2022 and year-32 chambers under every vintage's declarations and under the dated timeline on
    /// every day a declaration starts or ends, each with its party rules and without; every other country's chamber on its seed seats; both investiture
    /// rules. The result is printed - the outcome, the government, the negotiating power, every viable option with its doubles round-tripped, and each blocked cabinet with its line's two parties; the evaluator's API is asserted on the formation's own government in every case. The text is written to `PoliSim-captures/logs/formation_sweep.txt`.
    /// </summary>
    public static class FormationSweepDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var text = new StringBuilder();
            int cases = 0;
            var mismatches = new List<string>();
            void Sweep(string name, CountryId country, IReadOnlyList<PoliticalParty> parties, int[] seats, List<RedLine> lines, List<InOrAgainst> rules)
            {
                double[,] compatibility = GovernmentFormation.Compatibility(parties);
                foreach (bool negative in new[] { true, false })
                {
                    CoalitionResult r = CoalitionFormation.Form(seats, compatibility, lines, negativeRule: negative, inOrAgainst: rules);
                    cases++;
                    // The evaluator's own API on the formation's own government (asserted, not printed - the text is the pinned formation):
                    // Evaluate gives back the formation's option, and every supporter passes the support tests and stays.
                    if (r.Outcome != CoalitionOutcomeKind.NewElection)
                    {
                        CoalitionFormation.Chamber prepared = CoalitionFormation.Prepare(seats, compatibility, lines, negative, rules);
                        CoalitionFormation.CabinetEvaluation e = CoalitionFormation.Evaluate(prepared, r.Government.Cabinet, r.Government.Support);
                        GovernmentOption back = e.AsOption();
                        bool same = e.Wins && back.Cabinet == r.Government.Cabinet && back.Support == r.Government.Support && back.Kind == r.Government.Kind
                            && back.SupportedSeats == r.Government.SupportedSeats && back.OpposedSeats == r.Government.OpposedSeats && back.Score.Equals(r.Government.Score) && back.Cohesion.Equals(r.Government.Cohesion);
                        bool supporters = true;
                        for (int p = 0; p < parties.Count; p++) { if ((r.Government.Support & (1 << p)) != 0 && CoalitionFormation.SupportRefusal(prepared, p, r.Government.Cabinet) != null) { supporters = false; } }
                        supporters &= CoalitionFormation.SharedSupport(prepared, r.Government.Support) == r.Government.Support;
                        if (!same || !supporters) { mismatches.Add($"{country} | {name} | negative {negative}: {(same ? "" : "Evaluate does not give back the formation's government; ")}{(supporters ? "" : "a formation supporter fails the support tests")}"); }
                    }
                    text.Append("== ").Append(country).Append(" | ").Append(name).Append(" | negative ").Append(negative).Append('\n');
                    text.Append("   outcome ").Append(r.Outcome).Append(", majority ").Append(r.Majority).Append(", government ").Append(Option(r.Government)).Append('\n');
                    text.Append("   power ").Append(string.Join(",", Array.ConvertAll(r.NegotiatingPower, x => x.ToString("R", CultureInfo.InvariantCulture)))).Append('\n');
                    foreach (GovernmentOption g in r.Viable) { text.Append("   viable ").Append(Option(g)).Append('\n'); }
                    foreach ((int cabinet, RedLine line) in r.BlockedByRedLine) { text.Append("   blocked ").Append(cabinet).Append(" by ").Append(line.A).Append('-').Append(line.B).Append('\n'); }
                }
            }

            using IDisposable epoch = SimulationManager.EpochScope();
            IReadOnlyList<PoliticalParty> sweden = PartySystems.For(CountryId.Sweden);
            int[] Chamber(params (string Abbrev, int Seats)[] held)
            {
                var s = new int[sweden.Count];
                foreach ((string abbrev, int count) in held) { for (int p = 0; p < sweden.Count; p++) { if (sweden[p].Abbrev == abbrev) { s[p] = count; } } }
                return s;
            }
            var seed = new int[sweden.Count];
            for (int p = 0; p < sweden.Count; p++) { seed[p] = sweden[p].SeedSeats; }
            var chambers = new (string Name, int[] Seats)[]
            {
                ("seed", seed),
                ("2022", Chamber(("S", 107), ("SD", 73), ("M", 68), ("V", 24), ("C", 24), ("KD", 19), ("MP", 18), ("L", 16))),
                ("year-32", Chamber(("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20))),
            };
            var edges = new SortedSet<DateTime> { new DateTime(2022, 9, 11), new DateTime(2026, 1, 18), new DateTime(2026, 9, 13) };
            foreach (DeclaredRedLines.DatedFact fact in DeclaredRedLines.SwedenTimeline)
            {
                if (fact.From.Year >= 2022 && fact.From.Year <= 2027) { edges.Add(fact.From.Date); }
                if (fact.Until != DateTime.MaxValue && fact.Until.Year >= 2022 && fact.Until.Year <= 2027) { edges.Add(fact.Until.Date); }
            }
            foreach ((string chamberName, int[] seats) in chambers)
            {
                foreach (ElectionVintage vintage in new[] { ElectionVintage.Seated, ElectionVintage.Sweden2018, ElectionVintage.Sweden2022, ElectionVintage.Sweden2026 })
                {
                    List<RedLine> lines = DeclaredRedLines.For(CountryId.Sweden, sweden, vintage);
                    Sweep(chamberName + " " + vintage + " with rules", CountryId.Sweden, sweden, seats, lines, DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, sweden, vintage));
                    Sweep(chamberName + " " + vintage + " without rules", CountryId.Sweden, sweden, seats, lines, new List<InOrAgainst>());
                }
                foreach (DateTime d in edges)
                {
                    List<RedLine> lines = DeclaredRedLines.ForDate(CountryId.Sweden, sweden, d);
                    Sweep(chamberName + " dated " + d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " with rules", CountryId.Sweden, sweden, seats, lines, DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, sweden, d));
                    Sweep(chamberName + " dated " + d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " without rules", CountryId.Sweden, sweden, seats, lines, new List<InOrAgainst>());
                }
            }
            foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                if (id == CountryId.Sweden) { continue; }
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(id);
                if (parties == null || parties.Count == 0 || parties.Count > 16) { continue; }
                var seats = new int[parties.Count];
                for (int p = 0; p < parties.Count; p++) { seats[p] = parties[p].SeedSeats; }
                Sweep("seated", id, parties, seats, DeclaredRedLines.For(id, parties), DeclaredRedLines.InOrAgainstFor(id, parties));
            }

            string body = text.ToString();
            string digest;
            using (SHA256 sha = SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant(); }
            string dir = Path.Combine("..", "PoliSim-captures", "logs");
            Directory.CreateDirectory(dir);
            // The text is written where it matches the pin; a mismatch is written beside it, so the pinned text is never overwritten by the run it is compared with.
            string path = Path.Combine(dir, digest == PinnedDigest ? "formation_sweep.txt" : "formation_sweep_mismatch.txt");
            File.WriteAllText(path, body);
            foreach (string m in mismatches) { Debug.LogError("FORMATION SWEEP: " + m); }
            if (mismatches.Count > 0) { CheckExit.Finish(1); return; }
            Debug.Log($"=== FormationSweepDiagnostic (§646): {cases} formation(s) over the sweep, digest {digest}, written to {path} ===");
            // THE PIN: the formation's whole answer over the sweep is a baseline, as the trajectory's are - a change that moves it is a change to what the
            // formation computes, and sets the pin in the same commit with the diff of the text (keep the old text beside the new before re-running).
            if (digest != PinnedDigest)
            {
                Debug.LogError($"FORMATION SWEEP: the digest {digest} is not the pinned {PinnedDigest} - the formation's answer moved. Diff {path} against the text of the pinned run; a deliberate change sets the pin in its own commit.");
                CheckExit.Finish(1);
                return;
            }
            CheckExit.Finish(cases > 0 ? 0 : 1);
        }

        /// <summary>The digest of the sweep's text as the formation computed it when the evaluator was built (§646) - the formation reproduced exactly.</summary>
        private const string PinnedDigest = "a66608018b6d3be4bd90d466a7c4cb774e5a16079e304274b839ebedac823ba0";

        private static string Option(GovernmentOption g) => string.Format(CultureInfo.InvariantCulture, "cab {0} sup {1} {2} seats {3} supported {4} opposed {5} cohesion {6:R} score {7:R}",
            g.Cabinet, g.Support, g.Kind, g.CabinetSeats, g.SupportedSeats, g.OpposedSeats, g.Cohesion, g.Score);
    }
}
