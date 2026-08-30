using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using PoliSim.Testing;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-D3's harness — §29 coalition formation, on Sweden 2022.
    ///
    /// **The done-when's second clause carries the weight: a red line must demonstrably block a
    /// coalition that seat arithmetic alone would permit.** Sweden 2022 supplies the case without
    /// anything being arranged: S 107 + SD 73 = 180 of 349 is an absolute majority, and it is not
    /// a government anyone in Sweden would form. The harness asserts that the formation refuses
    /// it, names the red line that refused it, and prints its basis.
    ///
    /// Every input is SOURCED or DERIVED; none is an authored coalition score:
    /// - **Seats** — Valmyndigheten's final 2022 count (`ElectionsData/sweden/returns_2022.md`).
    /// - **Positions** — CHES 2024 `lrgen` / `lrecon` / `galtan` / `eu_position`
    ///   (`ElectionsData/positions/party_positions.md`), the same file W-A4 reads.
    /// - **Compatibility** — DERIVED from those positions by `CoalitionCompatibility`.
    /// - **Negotiating power** — DERIVED from the seat distribution alone (Banzhaf pivotality).
    /// - **Red lines** — DERIVED from position distance, plus the DECLARED ones, each carrying its
    ///   citation and vintage (`ElectionsData/sweden/coalition_declarations_2022.md`).
    /// </summary>
    public static class CoalitionHarness
    {
        // The driver's party order, as everywhere else in the elections track.
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        private const int S = 0, SD = 1, M = 2, V = 3, C = 4, KD = 5, MP = 6, L = 7;

        /// <summary>SOURCED - Valmyndigheten final count 2022 (RD_S.json, "slutlig"), via ElectionsData/sweden/returns_2022.md. 349 seats.</summary>
        private static readonly int[] Seats2022 = { 107, 73, 68, 24, 24, 19, 18, 16 };

        /// <summary>SOURCED - CHES 2024 (CHES_2024_final_v2), ElectionsData/positions/party_positions.md.</summary>
        private static readonly double[] Lrgen = { 3.74, 8.53, 7.58, 1.58, 5.95, 8.00, 2.74, 6.74 };
        private static readonly double[] Lrecon = { 3.68, 6.32, 7.89, 1.89, 7.84, 7.26, 3.16, 7.32 };
        private static readonly double[] Galtan = { 4.74, 9.00, 6.47, 2.42, 2.95, 7.79, 1.95, 4.47 };
        private static readonly double[] EuPosition = { 5.74, 2.68, 5.74, 3.32, 6.11, 5.35, 5.32, 6.84 };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-D3: coalition formation (section 29) - Sweden 2022, red lines over arithmetic, a new election reachable ===\n");

            double[,] compatibility = BuildCompatibility();
            List<RedLine> lines = BuildRedLines();

            failures += Structural(sb);
            failures += Sweden2022(sb, compatibility, lines);
            failures += RedLineOverArithmetic(sb, compatibility, lines);
            failures += NewElectionReachable(sb);
            failures += DerivedRuleReach(sb);

            sb.Append($"\nCOALITION: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }

        private static string Name(int mask)
        {
            var parts = new List<string>();
            foreach (int p in CoalitionFormation.Members(mask, Parties.Length)) { parts.Add(Parties[p]); }
            return parts.Count == 0 ? "-" : string.Join("+", parts.ToArray());
        }

        /// <summary>The staging is `CoalitionFilm`'s, shared with the screen: the harness and the coalition board must read ONE compatibility matrix and ONE set of red lines, or a green harness would be proving something the screen never shows.</summary>
        private static double[,] BuildCompatibility() => CoalitionFilm.Compatibility();

        private static List<RedLine> BuildRedLines() => CoalitionFilm.AllLines();

        /// <summary>Every red line except the DECLARED ones held by the named parties against SD - for measuring what each declaration is actually doing.</summary>
        private static List<RedLine> WithoutDeclared(int[] holders)
        {
            var kept = new List<RedLine>();
            foreach (RedLine line in BuildRedLines())
            {
                bool drop = false;
                foreach (int h in holders) { if (line.Kind == RedLineKind.Declared && line.Covers(h, SD)) { drop = true; } }
                if (!drop) { kept.Add(line); }
            }

            return kept;
        }

        private static bool Same(CoalitionResult a, CoalitionResult b) =>
            a.Outcome == b.Outcome && a.Government.Cabinet == b.Government.Cabinet && a.Government.Support == b.Government.Support;

        /// <summary>Structural: nothing here can be an authored coalition score, and §29's two unsourceable terms are DEFERRED rather than invented.</summary>
        private static int Structural(StringBuilder sb)
        {
            int failures = 0;

            bool refusedEmptyBasis = false;
            try { new RedLine(0, 1, RedLineKind.Derived, false, ""); }
            catch (ArgumentException) { refusedEmptyBasis = true; }
            failures += Assert(sb, "0a. a red line cannot exist without its basis (a derivation or a citation) - the refusal is what keeps §29 off an authored coalition score",
                refusedEmptyBasis, refusedEmptyBasis ? "an empty basis is refused" : "an empty basis was ACCEPTED");

            // §29 lists Leader Compatibility and Personal Relationships. There is no source for
            // either, and candidate attributes are already [AUTHORED-DRAFT] game fiction, so they
            // are DEFERRED - and the deferral is asserted so it cannot be filled in by accident.
            var forbidden = new[] { "leader", "relationship", "friendship", "trust", "personal" };
            var offenders = new StringBuilder();
            foreach (Type t in new[] { typeof(CoalitionCompatibility), typeof(CoalitionFormation), typeof(CoalitionMath), typeof(DerivedRedLines) })
            {
                foreach (MemberInfo m in t.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    string lower = m.Name.ToLowerInvariant();
                    foreach (string bad in forbidden) { if (lower.Contains(bad)) { offenders.Append(t.Name).Append('.').Append(m.Name).Append(' '); } }
                }
            }

            failures += Assert(sb, "0b. §29's Leader Compatibility and Personal Relationships are DEFERRED, not invented (no member carries them)",
                offenders.Length == 0, offenders.Length == 0 ? "deferred, and the deferral holds" : $"offenders: {offenders}");

            double[,] compat = BuildCompatibility();
            List<RedLine> lines = BuildRedLines();
            CoalitionResult a = CoalitionFormation.Form(Seats2022, compat, lines);
            CoalitionResult b = CoalitionFormation.Form(Seats2022, compat, lines);
            failures += Assert(sb, "0c. the formation is deterministic (same chamber, same government, same ranking)",
                a.Outcome == b.Outcome && a.Government.Cabinet == b.Government.Cabinet
                    && a.Government.Support == b.Government.Support && a.Viable.Count == b.Viable.Count,
                $"{a.Outcome} {Name(a.Government.Cabinet)} twice, {a.Viable.Count} viable options");

            return failures;
        }

        /// <summary>The 2022 seat distribution produces a plausible government set - the done-when's first clause.</summary>
        private static int Sweden2022(StringBuilder sb, double[,] compat, List<RedLine> lines)
        {
            int failures = 0;
            CoalitionResult r = CoalitionFormation.Form(Seats2022, compat, lines);

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  Sweden 2022 (Valmyndigheten final): {0} seats, majority {1}. Negotiating power (Banzhaf pivotality, DERIVED from the seats alone):\n    ",
                CoalitionMath.Seats(Seats2022, (1 << Parties.Length) - 1), r.Majority));
            for (int p = 0; p < Parties.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} seats / {2:P1}   ", Parties[p], Seats2022[p], r.NegotiatingPower[p]));
            }

            sb.Append("\n  The formation's ranking, best first:\n");
            for (int i = 0; i < r.Viable.Count && i < 6; i++)
            {
                GovernmentOption g = r.Viable[i];
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-12} cabinet {1,3} + support {2,-6} = {3,3}; against {4,3}; cohesion {5:F1}; score {6:F1}  [{7}]\n",
                    Name(g.Cabinet), g.CabinetSeats, Name(g.Support), g.SupportedSeats, g.OpposedSeats, g.Cohesion, g.Score, g.Kind));
            }

            int tido = (1 << M) | (1 << KD) | (1 << L);
            GovernmentOption top = r.Government;
            failures += Assert(sb, "1a. the 2022 chamber produces a government at all (not a new election)",
                r.Outcome != CoalitionOutcomeKind.NewElection, $"{r.Outcome}, {r.Viable.Count} viable options");
            failures += Assert(sb, "1b. it is the government that actually formed: an M+KD+L cabinet, in a minority, carried by SD from outside the cabinet (the Tido arrangement, 2022-10-14)",
                top.Cabinet == tido && top.Support == (1 << SD) && top.Kind == CoalitionOutcomeKind.ConfidenceAndSupply,
                string.Format(CultureInfo.InvariantCulture, "{0} cabinet {1} + {2} support = {3} (majority {4}), {5}",
                    Name(top.Cabinet), top.CabinetSeats, Name(top.Support), top.SupportedSeats, r.Majority, top.Kind));
            failures += Assert(sb, "1c. the arithmetic is the real one: M+KD+L is 103 of 349, and only SD's 73 make 176",
                top.CabinetSeats == 103 && top.SupportedSeats == 176,
                $"cabinet {top.CabinetSeats}, supported {top.SupportedSeats}");

            // The strategic fact the model must not lose: adding C to the cabinet COSTS SD's
            // support, because C will not be in a government SD carries - so the larger cabinet is
            // not viable at all. That is the 2022 deadlock in one line.
            int withCentre = tido | (1 << C);
            bool centreCabinetViable = false;
            foreach (GovernmentOption g in r.Viable) { if (g.Cabinet == withCentre) { centreCabinetViable = true; } }
            failures += Assert(sb, "1d. the deadlock is reproduced, not stored: adding C to that cabinet costs SD's support and the larger cabinet cannot govern",
                !centreCabinetViable, centreCabinetViable ? "C+M+KD+L was viable" : "C+M+KD+L is not viable - SD refuses to carry a government containing C");

            return failures;
        }

        /// <summary>
        /// THE DONE-WHEN'S SECOND CLAUSE, and the item's point: a red line demonstrably blocks a
        /// coalition that seat arithmetic alone would permit. S+SD is 180 of 349 - a comfortable
        /// absolute majority, and not a government. Every such refusal is printed with the line
        /// that made it and that line's basis, so the reader can see it is a derivation or a
        /// citation and never an authored score.
        /// </summary>
        private static int RedLineOverArithmetic(StringBuilder sb, double[,] compat, List<RedLine> lines)
        {
            int failures = 0;
            CoalitionResult r = CoalitionFormation.Form(Seats2022, compat, lines);

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  Cabinets an absolute majority of seats would have carried, that a RED LINE refused: {0}. The smallest few:\n", r.BlockedByRedLine.Count));
            int shown = 0;
            foreach ((int cabinet, RedLine line) in r.BlockedByRedLine)
            {
                if (CoalitionMath.Seats(Seats2022, cabinet) > 200 || shown >= 5) { continue; }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-14} {1,3} seats >= {2} - refused by {3}/{4} [{5}]: {6}\n",
                    Name(cabinet), CoalitionMath.Seats(Seats2022, cabinet), r.Majority, Parties[line.A], Parties[line.B], line.Kind, line.Basis));
                shown++;
            }

            int sPlusSd = (1 << S) | (1 << SD);
            bool blocked = false; RedLine which = default;
            foreach ((int cabinet, RedLine line) in r.BlockedByRedLine) { if (cabinet == sPlusSd) { blocked = true; which = line; } }
            failures += Assert(sb, "2a. THE CLAUSE: S+SD is an absolute majority (180 of 349) and the formation refuses it on a red line",
                blocked && CoalitionMath.Seats(Seats2022, sPlusSd) >= r.Majority,
                blocked ? $"180 >= {r.Majority}, refused by {Parties[which.A]}/{Parties[which.B]} [{which.Kind}]: {which.Basis}" : "S+SD was NOT blocked");

            // And the counterfactual, which is what makes it a demonstration rather than an
            // assertion: with the red lines removed and NOTHING else changed, the same chamber
            // forms that very cabinet.
            CoalitionResult free = CoalitionFormation.Form(Seats2022, compat, new List<RedLine>());
            bool formsWithout = false;
            foreach (GovernmentOption g in free.Viable) { if (g.Cabinet == sPlusSd) { formsWithout = true; } }
            failures += Assert(sb, "2b. it is the red line doing the work: remove the red lines, change nothing else, and S+SD becomes a viable majority cabinet in the same chamber",
                formsWithout, formsWithout ? "S+SD is viable once the red lines are gone" : "S+SD did not become viable - something other than the red line refused it");

            // Every refusal must be traceable to a derivation or a citation - that, and not the
            // COUNT of any particular kind, is what keeps this off an authored coalition score.
            bool allSourced = true;
            foreach ((int _, RedLine line) in r.BlockedByRedLine)
            {
                if (string.IsNullOrEmpty(line.Basis) || !(line.Basis.StartsWith("DERIVED") || line.Basis.StartsWith("DECLARED"))) { allSourced = false; }
            }

            failures += Assert(sb, "2c. every refusal carries a basis that is a derivation or a citation, never a score",
                allSourced, $"{r.BlockedByRedLine.Count} refusals, {DeclaredBlocks(r)} of them from a declared line, all with a stated basis");

            return failures;
        }

        private static int DeclaredBlocks(CoalitionResult r)
        {
            int n = 0;
            foreach ((int _, RedLine line) in r.BlockedByRedLine) { if (line.Kind == RedLineKind.Declared) { n++; } }
            return n;
        }

        /// <summary>
        /// A new election must be REACHABLE, not designed out. It is not a branch the formation
        /// takes when it feels like it: it is what is left when every admissible cabinet loses its
        /// investiture. The proof is a counterfactual on one chamber - with the red lines it is a
        /// new election, and with the SAME seats and no red lines a government forms.
        /// </summary>
        private static int NewElectionReachable(StringBuilder sb)
        {
            int failures = 0;
            // Three blocs, none able to govern alone, each refusing both others. Seats sum to 349
            // so the majority is the same 175 the real chamber uses.
            var seats = new[] { 150, 100, 99 };
            var compat = new double[3, 3];
            for (int a = 0; a < 3; a++) { for (int b = 0; b < 3; b++) { compat[a, b] = a == b ? 100.0 : 20.0; } }
            var mutual = new List<RedLine>
            {
                new RedLine(0, 1, RedLineKind.Declared, true, "TEST CHAMBER: a declared mutual refusal"),
                new RedLine(0, 2, RedLineKind.Declared, true, "TEST CHAMBER: a declared mutual refusal"),
                new RedLine(1, 2, RedLineKind.Declared, true, "TEST CHAMBER: a declared mutual refusal"),
            };

            CoalitionResult deadlocked = CoalitionFormation.Form(seats, compat, mutual);
            CoalitionResult freed = CoalitionFormation.Form(seats, compat, new List<RedLine>());

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  A test chamber of 150/100/99 (majority {0}), every pair refusing every other: {1}; the same seats with no red lines: {2}\n",
                deadlocked.Majority, deadlocked.Outcome, freed.Outcome));

            failures += Assert(sb, "3a. a new election is REACHABLE: where every cabinet is refused or voted down, the outcome is a new election",
                deadlocked.Outcome == CoalitionOutcomeKind.NewElection && deadlocked.Viable.Count == 0,
                $"{deadlocked.Outcome}, {deadlocked.Viable.Count} viable options");
            failures += Assert(sb, "3b. and it is not designed out: the same seats without the red lines form a government",
                freed.Outcome != CoalitionOutcomeKind.NewElection && freed.Viable.Count > 0,
                $"{freed.Outcome}, {freed.Viable.Count} viable options");
            failures += Assert(sb, "3c. Sweden 2022 is not one of those chambers - a new election was reachable there and did not happen",
                CoalitionFormation.Form(Seats2022, BuildCompatibility(), BuildRedLines()).Outcome != CoalitionOutcomeKind.NewElection,
                "the real chamber governs");

            return failures;
        }

        /// <summary>
        /// How far the DERIVED rule actually reaches, measured rather than claimed - and the
        /// honesty this item most needs. The shipped thresholds were chosen knowing Sweden 2022's
        /// answer, so "the derived lines reproduce Sweden" would be reporting a fit. What can be
        /// measured without circularity is the SLACK: over what window of single-axis thresholds
        /// does the sourced position data separate exactly the parties that really refused the
        /// Sweden Democrats from exactly those that really did not? A wide window means the data
        /// carries the distinction; a window of nothing means only the fitting did.
        /// </summary>
        private static int DerivedRuleReach(StringBuilder sb)
        {
            int failures = 0;
            // Who really refused SD in 2022 and who really governed with it (Tido, 2022-10-14).
            var refused = new[] { S, V, MP, C };
            var accepted = new[] { M, KD, L };

            sb.Append("\n  CHES lrgen distance from SD, every party, nearest first:\n    ");
            var order = new List<int> { S, M, V, C, KD, MP, L };
            order.Sort((x, y) => Math.Abs(Lrgen[x] - Lrgen[SD]).CompareTo(Math.Abs(Lrgen[y] - Lrgen[SD])));
            foreach (int p in order)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1:F2}{2}   ", Parties[p], Math.Abs(Lrgen[p] - Lrgen[SD]),
                    Array.IndexOf(refused, p) >= 0 ? " (refused)" : " (governed)"));
            }

            // The window: the largest accepted distance is the floor, the smallest refused distance
            // is the ceiling. A threshold strictly between them separates them exactly.
            double floorGap = 0.0, ceilingGap = double.MaxValue;
            foreach (int p in accepted) { floorGap = Math.Max(floorGap, Math.Abs(Lrgen[p] - Lrgen[SD])); }
            foreach (int p in refused) { ceilingGap = Math.Min(ceilingGap, Math.Abs(Lrgen[p] - Lrgen[SD])); }
            bool windowExists = ceilingGap > floorGap;
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  A single-axis lrgen threshold separates exactly the four that refused from the three that governed for any value in [{0:F2}, {1:F2}) - a window {2:F2} wide. {3}\n",
                floorGap, ceilingGap, ceilingGap - floorGap,
                windowExists ? "So the sourced positions DO carry Sweden's cordon on one axis; the shipped thresholds are still a fit, and this is the slack around it."
                             : "So the positions do NOT carry it on this axis and the refusals must be declared."));

            failures += Assert(sb, "4a. the shipped derived rule refuses exactly the parties that really refused SD, and admits exactly those that really governed with it",
                RefusesExactly(refused, accepted), Alignment(refused, accepted));
            failures += Assert(sb, "4b. and the separation is not an artefact of two free thresholds: ONE axis separates them, over a measurable window",
                windowExists, string.Format(CultureInfo.InvariantCulture, "[{0:F2}, {1:F2}), width {2:F2}", floorGap, ceilingGap, ceilingGap - floorGap));

            // Which declarations actually do work? Measured ONE AT A TIME, because dropping them
            // together says only "declarations matter" without saying which - and a declaration
            // the model never consults is decoration.
            double[,] compat = BuildCompatibility();
            CoalitionResult full = CoalitionFormation.Form(Seats2022, compat, BuildRedLines());
            CoalitionResult noCentre = CoalitionFormation.Form(Seats2022, compat, WithoutDeclared(new[] { C }));
            CoalitionResult noMinisters = CoalitionFormation.Form(Seats2022, compat, WithoutDeclared(new[] { M, KD, L }));
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  Each declaration dropped on its own, everything else unchanged:\n"
                + "    all lines                           -> {0,-19} {1} + {2}\n"
                + "    without C/SD (will not depend on)   -> {3,-19} {4} + {5}   [{6}]\n"
                + "    without M,KD,L/SD (no SD ministers) -> {7,-19} {8} + {9}   [{10}]\n",
                full.Outcome, Name(full.Government.Cabinet), Name(full.Government.Support),
                noCentre.Outcome, Name(noCentre.Government.Cabinet), Name(noCentre.Government.Support),
                Same(full, noCentre)
                    ? string.Format(CultureInfo.InvariantCulture, "CORROBORATED, not load-bearing - the derived galtan rule already reaches C ({0:F2} > {1:F2})", Math.Abs(Galtan[C] - Galtan[SD]), DerivedRedLines.SocialGap)
                    : "LOAD-BEARING",
                noMinisters.Outcome, Name(noMinisters.Government.Cabinet), Name(noMinisters.Government.Support),
                Same(full, noMinisters) ? "CORROBORATED, not load-bearing" : "LOAD-BEARING - without it SD sits in the cabinet and the Tido shape is gone"));

            failures += Assert(sb, "4c. the no-SD-ministers declaration is what gives the arrangement its SHAPE: drop it and the government is no longer a cabinet SD stays outside of",
                !Same(full, noMinisters),
                $"with: {Name(full.Government.Cabinet)} + {Name(full.Government.Support)}; without: {Name(noMinisters.Government.Cabinet)} + {Name(noMinisters.Government.Support)}");

            sb.Append("  A declaration that changes nothing HERE still earns its place: it is the only mechanism that can express the Liberals' reversal between 2018 and 2022, which no position distance moved.\n");


            return failures;
        }

        private static bool RefusesExactly(int[] refused, int[] accepted)
        {
            var lines = DerivedRedLines.From(Lrgen, Galtan);
            foreach (int p in refused) { if (!HasLine(lines, p, SD)) { return false; } }
            foreach (int p in accepted) { if (HasLine(lines, p, SD)) { return false; } }
            return true;
        }

        private static string Alignment(int[] refused, int[] accepted)
        {
            var lines = DerivedRedLines.From(Lrgen, Galtan);
            var sb = new StringBuilder();
            foreach (int p in refused) { sb.Append(Parties[p]).Append(HasLine(lines, p, SD) ? " refused ok; " : " NOT refused; "); }
            foreach (int p in accepted) { sb.Append(Parties[p]).Append(HasLine(lines, p, SD) ? " wrongly refused; " : " admitted ok; "); }
            return sb.ToString();
        }

        private static bool HasLine(List<RedLine> lines, int a, int b)
        {
            foreach (RedLine l in lines) { if (l.Covers(a, b)) { return true; } }
            return false;
        }
    }
}
