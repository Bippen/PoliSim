using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K-1 part (2), 2026-09-23, K-1f, 2026-09-24, and K-1h / K-1g, 2026-09-25: THE FORMATION ON THE 2026 CHAMBER, MEASURED - what
    /// `CoalitionFormation` forms from the seated Riksdag (Valmyndigheten's decision of 2026-09-19) under the declarations as of the 2026
    /// election, beside the counterfactuals that show what each piece does: without each of K-1g's three rules, SD's rule read as V's shape
    /// (the reading not wired), the candidacy pair at cabinet-blocking strength (the reading K-1h (ii) did not rule), K-1f's set alone, §603's
    /// wiring, 2022's declarations, and the derived lines alone; then the pinned film's year-32 count and 2022's chamber, where MP's 2022 rule
    /// is measured and not wired (K-1g: the model cannot hold it). Every row holds out as K-1h (i) ruled - only for a cabinet that could pass.
    ///
    /// <para>Prints only. Every variant runs the same compatibility and the same negative rule. Most run the seated chamber; the rows
    /// that run another chamber (the pinned film's year-32 count, 2022's) print it, so a difference between two rows on ONE chamber is the
    /// lines' and rules' doing.</para>
    /// </summary>
    public static class Formation2026Diagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Sweden);
            int n = parties.Count;
            var seats = new int[n];
            var lrGen = new double[n];
            var galtan = new double[n];
            for (int p = 0; p < n; p++) { seats[p] = parties[p].SeedSeats; lrGen[p] = parties[p].LrGen; galtan[p] = parties[p].Galtan; }
            double[,] compatibility = GovernmentFormation.Compatibility(parties);
            int c = Index(parties, "C"), v = Index(parties, "V"), s = Index(parties, "S"), m = Index(parties, "M");
            int mp = Index(parties, "MP"), sd = Index(parties, "SD"), kd = Index(parties, "KD"), l = Index(parties, "L");

            List<RedLine> wired = DeclaredRedLines.For(CountryId.Sweden, parties);
            bool KdToS(RedLine line) => line.Kind == RedLineKind.Declared && line.OneWay && line.A == kd && line.B == s;
            List<RedLine> noKd = wired.FindAll(line => !KdToS(line));
            List<RedLine> noCandidacy = wired.FindAll(line => !DeclaredRedLines.IsCandidacy(line));
            List<RedLine> k1f = noKd;   // K-1f's lines: 2026's set before K-1g added KD's
            List<RedLine> k603 = noKd.FindAll(line => !DeclaredRedLines.IsCandidacy(line));
            // The candidacy pair at the model's weaker strength: S and M never sit together, and either may tolerate the other's cabinet.
            var candidacyCabinetOnly = new List<RedLine>(noCandidacy)
            {
                new RedLine(s, m, RedLineKind.Declared, blocksSupport: false, basis: "[PROBE: the candidacy pair at cabinet-blocking strength]"),
            };

            // K-1f's V rule; K-1g's MP (votes against) and SD (refuses the support role only).
            List<InOrAgainst> rules = DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, parties);
            List<InOrAgainst> Without(string abbrev) => rules.FindAll(r => parties[r.Party].Abbrev != abbrev);
            List<InOrAgainst> vOnly = rules.FindAll(r => parties[r.Party].Abbrev == "V");
            var sdAsV = new List<InOrAgainst>(Without("SD")) { new InOrAgainst(sd, "[PROBE: SD's rule as V's shape - it would also vote against]") };
            var none = new List<InOrAgainst>();
            var mp2022 = new List<InOrAgainst> { new InOrAgainst(mp, "[PROBE: MP 2022 [MP-I1], read right, NOT WIRED - the model cannot hold it]") };

            List<RedLine> lines2022 = DeclaredRedLines.For(CountryId.Sweden, parties, ElectionVintage.Sweden2022);
            List<RedLine> lines2022NoCandidacy = lines2022.FindAll(line => !DeclaredRedLines.IsCandidacy(line));
            bool NoSdMinisters(RedLine line) => line.Kind == RedLineKind.Declared && !line.BlocksSupport
                && (line.Covers(m, sd) || line.Covers(kd, sd) || line.Covers(l, sd));
            List<RedLine> lines2022NoTido = lines2022.FindAll(line => !NoSdMinisters(line));

            int[] yearThirtyTwo = Chamber(parties, ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20));
            int[] chamber2022 = Chamber(parties, ("S", 107), ("SD", 73), ("M", 68), ("V", 24), ("C", 24), ("KD", 19), ("MP", 18), ("L", 16));
            var variants = new (string Name, List<RedLine> Lines, List<InOrAgainst> Rules, int[] Seats)[]
            {
                ("AS WIRED (K-1h + K-1g): 2026 declarations, the S-M candidacy pair, KD>S, V's and MP's in-or-against, SD's no support role (the day-one government)", wired, rules, seats),
                ("the same without KD>S", noKd, rules, seats),
                ("the same without MP's rule", wired, Without("MP"), seats),
                ("the same without SD's rule", wired, Without("SD"), seats),
                ("the same, SD's rule read as V's shape (it would vote against every cabinet it is not in) - the other reading, open for Elias (K-1i)", wired, sdAsV, seats),
                ("K-1f's set alone (V's rule, no K-1g) - K-1h (i)'s own measurement", k1f, vOnly, seats),
                ("AS WIRED without the candidacies", noCandidacy, rules, seats),
                ("AS WIRED, the candidacy pair at CABINET-BLOCKING strength - the reading K-1h (ii) did not rule", candidacyCabinetOnly, rules, seats),
                ("§603's wiring (C-SD, C->V one way, SD lines lifted; no candidacies, no party rules)", k603, none, seats),
                ("2022 declarations (with 2022's S-M candidacy pair) on the 2026 chamber", lines2022, none, seats),
                ("derived lines alone", DerivedRedLines.From(lrGen, galtan), none, seats),
                ("AS WIRED on the pinned film's own year-32 count", wired, rules, yearThirtyTwo),
                ("2022 declarations with 2022's candidacy pair on the 2022 chamber (the backtest's record: M+KD+L carried by SD)", lines2022, none, chamber2022),
                ("the same without the candidacy pair", lines2022NoCandidacy, none, chamber2022),
                ("2022 on the 2022 chamber without the M, KD, L no-SD-ministers lines (CoalitionHarness 4c's counterfactual)", lines2022NoTido, none, chamber2022),
                ("2022 on the 2022 chamber + MP's 2022 in-or-against rule (read right, NOT WIRED - K-1g: the model cannot hold it)", lines2022, mp2022, chamber2022),
            };

            var sb = new StringBuilder("=== Formation2026Diagnostic: the formation on the seated 2026 Riksdag ===\n");
            sb.Append("    the seated chamber:").Append(ChamberText(parties, seats)).Append("; negative parliamentarism (RF 6:4)\n");
            foreach ((string name, List<RedLine> lines, List<InOrAgainst> partyRules, int[] chamber) in variants)
            {
                CoalitionResult r = CoalitionFormation.Form(chamber, compatibility, lines, negativeRule: true, inOrAgainst: partyRules);
                sb.Append("\n  --- ").Append(name).Append(" ---\n");
                if (chamber != seats) { sb.Append("    chamber:").Append(ChamberText(parties, chamber)).Append('\n'); }
                foreach (InOrAgainst rule in partyRules) { sb.Append("    in-or-against: ").Append(parties[rule.Party].Abbrev).Append(rule.VotesAgainst ? "" : " (no support role; its vote is the lines')").Append('\n'); }
                int declared = 0;
                foreach (RedLine line in lines) { if (line.Kind == RedLineKind.Declared) { declared++; sb.Append("    declared: ").Append(parties[line.A].Abbrev).Append('-').Append(parties[line.B].Abbrev).Append(line.OneWay ? " one way" : line.BlocksSupport ? " support-blocking" : " cabinet-blocking").Append('\n'); } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} declared, {1} derived line(s)\n", declared, lines.Count - declared));
                if (r.Outcome == CoalitionOutcomeKind.NewElection || r.Outcome == CoalitionOutcomeKind.Collapse)
                {
                    sb.Append("    OUTCOME: ").Append(r.Outcome).Append(" - no viable government\n");
                }
                else
                {
                    sb.Append("    OUTCOME: ").Append(r.Outcome).Append(" - ").Append(Describe(r.Government, parties, chamber)).Append('\n');
                }
                int shown = 0;
                foreach (GovernmentOption g in r.Viable)
                {
                    if (shown++ >= 6) { break; }
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "      viable #{0}: {1}  score {2:F2}\n", shown, Describe(g, parties, chamber), g.Score));
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "      {0} viable in all; {1} majority cabinet(s) refused by a red line\n", r.Viable.Count, r.BlockedByRedLine.Count));
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string Describe(GovernmentOption g, IReadOnlyList<PoliticalParty> parties, int[] seats)
        {
            var cabinet = new List<string>();
            var support = new List<string>();
            for (int p = 0; p < parties.Count; p++)
            {
                if ((g.Cabinet & (1 << p)) != 0) { cabinet.Add(parties[p].Abbrev); }
                else if ((g.Support & (1 << p)) != 0) { support.Add(parties[p].Abbrev); }
            }
            return string.Format(CultureInfo.InvariantCulture, "{0} cabinet {1} ({2}), support {3} (to {4}), opposed {5}",
                g.Kind, string.Join("+", cabinet), g.CabinetSeats, support.Count == 0 ? "none" : string.Join("+", support), g.SupportedSeats, g.OpposedSeats);
        }

        private static int Index(IReadOnlyList<PoliticalParty> parties, string abbrev)
        {
            for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == abbrev) { return p; } }
            return -1;
        }

        private static int Sum(int[] a) { int s = 0; foreach (int x in a) { s += x; } return s; }

        private static string ChamberText(IReadOnlyList<PoliticalParty> parties, int[] chamber)
        {
            var sb = new StringBuilder();
            for (int p = 0; p < parties.Count; p++) { sb.Append(' ').Append(parties[p].Abbrev).Append(' ').Append(chamber[p]); }
            return sb.Append(string.Format(CultureInfo.InvariantCulture, " = {0}; majority {1}", Sum(chamber), CoalitionMath.Majority(chamber))).ToString();
        }

        /// <summary>A chamber given by party key, in the table's order - a party the list does not name holds no seats.</summary>
        private static int[] Chamber(IReadOnlyList<PoliticalParty> parties, params (string Abbrev, int Seats)[] seats)
        {
            var chamber = new int[parties.Count];
            foreach ((string abbrev, int count) in seats) { int p = Index(parties, abbrev); if (p >= 0) { chamber[p] = count; } }
            return chamber;
        }
    }
}
