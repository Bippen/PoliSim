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
    /// K-1 part (2), 2026-09-23: THE FORMATION ON THE 2026 CHAMBER, MEASURED BEFORE THE DECLARATIONS ARE READ AS LANDED - what
    /// `CoalitionFormation` forms from the seated Riksdag (Valmyndigheten's decision of 2026-09-19) under the declarations as of
    /// the 2026 election, beside the counterfactuals that show what each line does: 2022's declarations on the same chamber, the
    /// C-V line at each of the model's two symmetric strengths in place of the one-way shape it is wired as, no C-V line, and the
    /// derived lines alone. Part (4)'s provisional government is this formation's result, so the record of it is here.
    ///
    /// <para>Prints only. Every variant runs the same chamber, the same compatibility and the same negative rule; only the
    /// lines differ, so a difference between two rows is the lines' doing.</para>
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
            int c = Index(parties, "C"), v = Index(parties, "V");

            List<RedLine> wired = DeclaredRedLines.For(CountryId.Sweden, parties);
            var supportBlocking = new List<RedLine>();
            var cabinetOnly = new List<RedLine>();
            var noCv = new List<RedLine>();
            foreach (RedLine line in wired)
            {
                bool cv = line.Kind == RedLineKind.Declared && line.Covers(c, v);
                if (!cv) { supportBlocking.Add(line); cabinetOnly.Add(line); noCv.Add(line); continue; }
                supportBlocking.Add(new RedLine(line.A, line.B, line.Kind, blocksSupport: true, basis: line.Basis + " [PROBE: symmetric support-blocking]"));
                cabinetOnly.Add(new RedLine(line.A, line.B, line.Kind, blocksSupport: false, basis: line.Basis + " [PROBE: at cabinet-blocking strength]"));
            }

            var variants = new (string Name, List<RedLine> Lines)[]
            {
                ("2026 declarations as wired (C-SD support-blocking, C-V one way; M, KD, L - SD lifted)", wired),
                ("the same, C-V as a symmetric support-blocking line", supportBlocking),
                ("the same, C-V at cabinet-blocking strength", cabinetOnly),
                ("the same, no C-V line", noCv),
                ("2022 declarations on the 2026 chamber (what part (1) alone seats)", DeclaredRedLines.For(CountryId.Sweden, parties, ElectionVintage.Sweden2022)),
                ("derived lines alone", DerivedRedLines.From(lrGen, galtan)),
            };

            var sb = new StringBuilder("=== Formation2026Diagnostic: the formation on the seated 2026 Riksdag ===\n");
            sb.Append("    chamber:");
            for (int p = 0; p < n; p++) { sb.Append(' ').Append(parties[p].Abbrev).Append(' ').Append(seats[p]); }
            sb.Append(string.Format(CultureInfo.InvariantCulture, " = {0}; majority {1}; negative parliamentarism (RF 6:4)\n", Sum(seats), CoalitionMath.Majority(seats)));
            foreach ((string name, List<RedLine> lines) in variants)
            {
                CoalitionResult r = CoalitionFormation.Form(seats, compatibility, lines, negativeRule: true);
                sb.Append("\n  --- ").Append(name).Append(" ---\n");
                int declared = 0;
                foreach (RedLine line in lines) { if (line.Kind == RedLineKind.Declared) { declared++; sb.Append("    declared: ").Append(parties[line.A].Abbrev).Append('-').Append(parties[line.B].Abbrev).Append(line.OneWay ? " one way" : line.BlocksSupport ? " support-blocking" : " cabinet-blocking").Append('\n'); } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} declared, {1} derived line(s)\n", declared, lines.Count - declared));
                if (r.Outcome == CoalitionOutcomeKind.NewElection || r.Outcome == CoalitionOutcomeKind.Collapse)
                {
                    sb.Append("    OUTCOME: ").Append(r.Outcome).Append(" - no viable government\n");
                }
                else
                {
                    sb.Append("    OUTCOME: ").Append(r.Outcome).Append(" - ").Append(Describe(r.Government, parties, seats)).Append('\n');
                }
                int shown = 0;
                foreach (GovernmentOption g in r.Viable)
                {
                    if (shown++ >= 6) { break; }
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "      viable #{0}: {1}  score {2:F2}\n", shown, Describe(g, parties, seats), g.Score));
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
    }
}
