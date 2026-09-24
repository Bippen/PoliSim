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
    /// K-1 part (2), 2026-09-23, and K-1f, 2026-09-24: THE FORMATION ON THE 2026 CHAMBER, MEASURED - what `CoalitionFormation` forms from
    /// the seated Riksdag (Valmyndigheten's decision of 2026-09-19) under the declarations as of the 2026 election, beside the
    /// counterfactuals that show what each piece does: without V's in-or-against rule, without the candidacy pair, the candidacy pair at
    /// cabinet-blocking strength, the hold-out read only over cabinets that pass on the lines (MEASURED, NOT WIRED - Elias's ruling
    /// question, §607), MP's and SD's in-or-against rules (on record, NOT WIRED - K-1g), §603's wiring, the C-V line symmetric, 2022's
    /// declarations, and the derived lines alone. Part (4)'s provisional government is this formation's result, so the record is here.
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
            List<RedLine> noCandidacy = wired.FindAll(line => !DeclaredRedLines.IsCandidacy(line));
            var supportBlockingCv = new List<RedLine>();
            foreach (RedLine line in noCandidacy)
            {
                bool cv = line.Kind == RedLineKind.Declared && line.Covers(c, v);
                supportBlockingCv.Add(cv ? new RedLine(line.A, line.B, line.Kind, blocksSupport: true, basis: line.Basis + " [PROBE: symmetric support-blocking]") : line);
            }
            // The candidacy pair at the model's weaker strength: S and M never sit together, and either may tolerate the other's cabinet.
            var candidacyCabinetOnly = new List<RedLine>(noCandidacy)
            {
                new RedLine(s, m, RedLineKind.Declared, blocksSupport: false, basis: "[PROBE: the candidacy pair at cabinet-blocking strength]"),
            };

            // K-1f (ruled 2026-09-24): V's in-or-against rule rides beside the lines. MP's and SD's are on record and NOT wired (K-1g).
            List<InOrAgainst> vRule = DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, parties);
            var none = new List<InOrAgainst>();
            var vMpSd = new List<InOrAgainst>(vRule) { new InOrAgainst(mp, "[PROBE: MP 2026, secondary, not wired]"), new InOrAgainst(sd, "[PROBE: SD 2026 [SD-P2], not wired]") };
            var mp2022 = new List<InOrAgainst> { new InOrAgainst(mp, "[PROBE: MP 2022 [MP-I1], secondary, not wired]") };

            List<RedLine> lines2022 = DeclaredRedLines.For(CountryId.Sweden, parties, ElectionVintage.Sweden2022);
            List<RedLine> lines2022NoCandidacy = lines2022.FindAll(line => !DeclaredRedLines.IsCandidacy(line));
            bool NoSdMinisters(RedLine line) => line.Kind == RedLineKind.Declared && !line.BlocksSupport
                && (line.Covers(m, sd) || line.Covers(kd, sd) || line.Covers(l, sd));
            List<RedLine> lines2022NoTido = lines2022.FindAll(line => !NoSdMinisters(line));
            List<RedLine> lines2022NoTidoNoCandidacy = lines2022NoTido.FindAll(line => !DeclaredRedLines.IsCandidacy(line));

            int[] yearThirtyTwo = Chamber(parties, ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20));
            int[] chamber2022 = Chamber(parties, ("S", 107), ("SD", 73), ("M", 68), ("V", 24), ("C", 24), ("KD", 19), ("MP", 18), ("L", 16));
            var variants = new (string Name, List<RedLine> Lines, List<InOrAgainst> Rules, int[] Seats, bool HoldOutPassable)[]
            {
                ("K-1f AS WIRED: 2026 declarations + the S-M candidacy pair + V's in-or-against rule (the day-one government)", wired, vRule, seats, false),
                ("the same without V's rule", wired, none, seats, false),
                ("the same without the candidacies", noCandidacy, vRule, seats, false),
                ("the same, the candidacy pair at CABINET-BLOCKING strength (they never sit together, either may tolerate the other) - the reading not wired", candidacyCabinetOnly, vRule, seats, false),
                ("K-1f AS WIRED, a party holding out ONLY for a cabinet that passes on the lines and rules alone - MEASURED, NOT WIRED (Elias's ruling question)", wired, vRule, seats, true),
                ("K-1f AS WIRED + MP's and SD's in-or-against rules (on record, NOT WIRED - K-1g)", wired, vMpSd, seats, false),
                ("§603's wiring (neither: C-SD, C->V one way, SD lines lifted)", noCandidacy, none, seats, false),
                ("§603's wiring, C-V as a symmetric support-blocking line", supportBlockingCv, none, seats, false),
                ("2022 declarations (with 2022's S-M candidacy pair) on the 2026 chamber", lines2022, none, seats, false),
                ("derived lines alone", DerivedRedLines.From(lrGen, galtan), none, seats, false),
                ("K-1f AS WIRED on the pinned film's own year-32 count", wired, vRule, yearThirtyTwo, false),
                ("§603's wiring on the same year-32 count", noCandidacy, none, yearThirtyTwo, false),
                ("2022 declarations with 2022's candidacy pair on the 2022 chamber (the backtest's record: M+KD+L carried by SD)", lines2022, none, chamber2022, false),
                ("the same without the candidacy pair (the pair changes nothing here)", lines2022NoCandidacy, none, chamber2022, false),
                ("2022 on the 2022 chamber without the M, KD, L no-SD-ministers lines (CoalitionHarness 4c's counterfactual), with the candidacy pair", lines2022NoTido, none, chamber2022, false),
                ("the same without the candidacy pair (the table's row before K-1f)", lines2022NoTidoNoCandidacy, none, chamber2022, false),
                ("2022 on the 2022 chamber + MP's 2022 in-or-against rule (on record, secondary, NOT WIRED)", lines2022, mp2022, chamber2022, false),
            };

            var sb = new StringBuilder("=== Formation2026Diagnostic: the formation on the seated 2026 Riksdag ===\n");
            sb.Append("    the seated chamber:").Append(ChamberText(parties, seats)).Append("; negative parliamentarism (RF 6:4)\n");
            foreach ((string name, List<RedLine> lines, List<InOrAgainst> rules, int[] chamber, bool holdOutPassable) in variants)
            {
                CoalitionResult r = CoalitionFormation.Form(chamber, compatibility, lines, negativeRule: true, inOrAgainst: rules, holdOutOnlyForPassable: holdOutPassable);
                sb.Append("\n  --- ").Append(name).Append(" ---\n");
                if (chamber != seats) { sb.Append("    chamber:").Append(ChamberText(parties, chamber)).Append('\n'); }
                foreach (InOrAgainst rule in rules) { sb.Append("    in-or-against: ").Append(parties[rule.Party].Abbrev).Append('\n'); }
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
