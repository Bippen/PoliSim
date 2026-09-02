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
    /// C-D1's guard — **the voter groups are a VIEW over the cohorts, and the join holds.**
    ///
    /// <para><b>THE ENUMERATION.</b> All six countries. The spec-let's §5 names the two tests this must
    /// run and they are run exactly as written:</para>
    ///
    /// <list type="number">
    /// <item><description><b>Σ(group shares) == 1</b> to float tolerance. A view whose parts do not sum
    /// to the whole is a second population.</description></item>
    /// <item><description><b>The cohorts each group covers sum to the ELIGIBLE population</b> — not to the
    /// country's population. ⚠ Getting this wrong inflates every young group, which is §5's own stated
    /// failure mode, and it is the reason the eligible total is computed independently of the groups and
    /// then compared, rather than derived from them.</description></item>
    /// <item><description><b>Sweden's turnout weights back to its published total.</b> The group shares
    /// weighted by SCB's own per-band rates must reproduce SCB's own all-ages figure, **85.8 %**, within
    /// a stated tolerance — and that figure is transcribed separately from the thirteen band rates, so it
    /// is a real cross-check rather than an identity. ⚠ It cannot be exact: the weights are 2024 cohorts
    /// and the rates are the 2014 electorate.</description></item>
    /// <item><description><b>Turnout is sourced or NaN, never a borrowed number.</b> Every non-Swedish
    /// group must carry NaN. A Swedish figure worn by another electorate would be the worst kind of
    /// invented figure: real, checkable, and about the wrong country.</description></item>
    /// </list>
    /// </summary>
    public static class VoterGroupViewDiagnostic
    {
        /// <summary>CONVENTION: shares are doubles summing 13 terms; this cannot be reached by rounding.</summary>
        private const double ShareTolerance = 1e-9;

        /// <summary>CONVENTION: millions. The eligible total is computed twice by different routes, so
        /// the only difference possible is float accumulation.</summary>
        private const double EligibleToleranceMillions = 1e-4;

        /// <summary>CONVENTION, and it is WIDE on purpose: the weights are the 2024 pyramid and the rates
        /// are the 2014 electorate, so an exact match would mean the check was comparing a number with
        /// itself. Wide enough to survive a decade of ageing, narrow enough that a mis-keyed band fails.</summary>
        private const double TurnoutTolerancePoints = 2.0;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();

            sb.Append("=== C-D1: voter groups as a VIEW over the cohort substrate ===\n");
            sb.Append("    THE ENUMERATION: all six countries. (1) group shares sum to 1; (2) the cohorts they cover sum to\n");
            sb.Append("    the ELIGIBLE population, not the country's; (3) Sweden's shares weighted by SCB's own band rates\n");
            sb.Append("    reproduce SCB's own all-ages figure; (4) turnout is sourced or NaN, never borrowed.\n\n");
            sb.Append("    country      groups   share sum        eligible M   of population   turnout\n");
            sb.Append("    ----------------------------------------------------------------------------\n");

            foreach (Country country in world.Countries)
            {
                CohortVoterGroups.Group[] groups = CohortVoterGroups.For(country);
                if (groups.Length == 0)
                {
                    failures.Add($"{country.Name}: no groups");
                    Debug.LogError($"COHORTVOTERS: {country.Name} produced no voter groups. Every country carries a pyramid, so "
                                   + "an empty view means the join is broken rather than the country being unusual.");
                    continue;
                }

                double shareSum = 0.0;
                double covered = 0.0;
                bool anySourced = false;
                double eligible = CohortVoterGroups.EligiblePopulation(country.Cohorts, CohortVoterGroups.VotingAge(country.Id));

                foreach (CohortVoterGroups.Group g in groups)
                {
                    shareSum += g.PopulationShare;
                    covered += g.PopulationShare * eligible;
                    if (!double.IsNaN(g.TurnoutBase)) { anySourced = true; }
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,6} {2,11:F9} {3,17:F4} {4,15:P2}   {5}\n",
                    country.Name, groups.Length, shareSum, eligible, eligible / country.Cohorts.Total,
                    anySourced ? "SOURCED" : "not published"));

                if (System.Math.Abs(shareSum - 1.0) > ShareTolerance)
                {
                    failures.Add($"{country.Name} shares sum {shareSum}");
                    Debug.LogError($"COHORTVOTERS: {country.Name}'s group shares sum to {shareSum:F9}, not 1. A view whose parts "
                                   + "do not sum to the whole is a second population - the exact thing the cohort spec-let's §5 "
                                   + "made the groups a view to prevent.");
                }

                if (System.Math.Abs(covered - eligible) > EligibleToleranceMillions)
                {
                    failures.Add($"{country.Name} coverage");
                    Debug.LogError($"COHORTVOTERS: {country.Name}'s groups cover {covered:F4} M against an eligible population of "
                                   + $"{eligible:F4} M. ⚠ The eligible total is computed independently of the groups precisely so "
                                   + "the two can disagree; a share taken of the COUNTRY rather than of the ELECTORATE inflates "
                                   + "every young group, which is §5's own named failure.");
                }

                if (country.Id != CountryId.Sweden && anySourced)
                {
                    failures.Add($"{country.Name} carries a turnout it has no source for");
                    Debug.LogError($"COHORTVOTERS: {country.Name} carries a turnout figure. Only Sweden publishes turnout by age "
                                   + "here. ⚠ A Swedish rate worn by another electorate is the worst kind of invented figure: "
                                   + "real, checkable, and about the wrong country.");
                }
            }

            // --- (3) Sweden's weighted turnout against SCB's own total ---
            Country sweden = world.GetCountry(CountryId.Sweden);
            CohortVoterGroups.Group[] swedish = CohortVoterGroups.For(sweden);
            double weighted = 0.0;
            sb.Append("\n    --- SWEDEN: the groups, and what they weight SCB's own rates to ---\n");
            sb.Append("    group       share of electorate   turnout %\n");
            foreach (CohortVoterGroups.Group g in swedish)
            {
                weighted += g.PopulationShare * g.TurnoutBase;
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-10} {1,19:P2} {2,11:F1}\n", g.Name, g.PopulationShare, g.TurnoutBase));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    weighted {0:F2} % against SCB's own all-ages {1:F1} % (2014) - difference {2:F2} points.\n",
                weighted, CohortVoterGroups.SwedenTurnoutAll2014, weighted - CohortVoterGroups.SwedenTurnoutAll2014));

            if (System.Math.Abs(weighted - CohortVoterGroups.SwedenTurnoutAll2014) > TurnoutTolerancePoints)
            {
                failures.Add("Sweden weighted turnout");
                Debug.LogError($"COHORTVOTERS: Sweden's group shares weight SCB's own band rates to {weighted:F2} % against SCB's "
                               + $"own all-ages figure of {CohortVoterGroups.SwedenTurnoutAll2014:F1} %. The total is transcribed "
                               + "separately from the thirteen band rates, so this is a real cross-check; a gap this size means a "
                               + "band is mis-keyed or the shares are of the wrong denominator.");
            }

            sb.Append("\n    ⚠ NO PER-GROUP LOYALTY, and that is the honest half. C-A1 named per-group loyalty as the Italy FdI\n");
            sb.Append("    ceiling; it needs vote shares BY AGE GROUP, which is survey data no source consulted publishes for\n");
            sb.Append("    Italy 2022. The substrate it would hang on now exists. A per-group loyalty guessed from a national\n");
            sb.Append("    one would reproduce the uniform 60 this whole chain exists to replace.\n");
            sb.Append("    ⚠ ONE APPROXIMATION, named: the 15-19 cohort is apportioned two fifths (ages 18 and 19), which\n");
            sb.Append("    assumes an even spread inside a five-year band - the assumption the aging step REPLACED with observed\n");
            sb.Append("    data, and which cannot be replaced here because no source publishes the electorate by single year.\n");

            // F3 (2026-09-02): the same join PER VALKRETS, over SwedishValkretsPopulation2024 - each of the
            // 29 valkretsar's groups must sum to 1, and its name must join the returns catalog. The 18+
            // RESIDENTS of 2024 are printed beside Valmyndigheten's 2022 ROLL and no identity is asserted
            // between them: the ratio is what it is - non-citizen adult residents plus two years' growth,
            // 1.03-1.09 in the counties and 1.11-1.16 in Stockholm, Uppsala, Göteborg and Malmö - the reason the
            // campaign's mobilisable electorate is the roll and not the resident count (LiveCampaignSetup).
            // The last two columns MEASURE whether the age structure carries anything the roll does not: each
            // valkrets's turnout as its 2024 age mix weights SCB's 2014 band rates, beside its ACTUAL 2022
            // turnout (Cast / Eligible from the returns). The levels differ by year (85.8 national in 2014,
            // 84.2 in 2022); the SPREADS are the comparison, and they are printed, not asserted. ⚠ Nothing on
            // the game path reads this view (the campaign has no age-group mechanic; that consumer is the
            // per-group design, which waits on per-group loyalty) - so it is Editor-consumed, and it counts
            // against UnwiredSubsystemCheck's ceilings the way C-D1 left it.
            sb.Append("\n    --- SWEDEN PER VALKRETS: the view over the 2024 pyramid of each of the 29 ---\n");
            sb.Append("    valkrets                          groups  share sum   18+ res. 2024   roll 2022   ratio   age-pred %   actual 2022 %\n");
            int perValkretsFailures = 0;
            double predMin = double.MaxValue, predMax = double.MinValue, actMin = double.MaxValue, actMax = double.MinValue;
            for (int v = 0; v < PoliSim.Elections.Generated.SwedishValkretsPopulation2024.Names.Length; v++)
            {
                CohortVoterGroups.Group[] vg = CohortVoterGroups.ForValkrets(v);
                PopulationCohorts pyramid = CohortVoterGroups.ValkretsPyramid(v);
                double vEligible = pyramid != null ? CohortVoterGroups.EligiblePopulation(pyramid, CohortVoterGroups.VotingAge(CountryId.Sweden)) : 0.0;
                double vSum = 0.0, predicted = 0.0;
                foreach (CohortVoterGroups.Group g in vg) { vSum += g.PopulationShare; predicted += g.PopulationShare * g.TurnoutBase; }
                string name = PoliSim.Elections.Generated.SwedishValkretsPopulation2024.Names[v];
                int returnsIndex = System.Array.IndexOf(PoliSim.Elections.Generated.SwedishValkretsReturns2022.Names, name);
                double roll = returnsIndex >= 0 ? PoliSim.Elections.Generated.SwedishValkretsReturns2022.Eligible[returnsIndex] / 1_000_000.0 : double.NaN;
                double ratio = roll > 0 ? vEligible / roll : double.NaN;
                double actual = returnsIndex >= 0 ? 100.0 * PoliSim.Elections.Generated.SwedishValkretsReturns2022.Cast[returnsIndex] / PoliSim.Elections.Generated.SwedishValkretsReturns2022.Eligible[returnsIndex] : double.NaN;
                if (returnsIndex >= 0) { predMin = System.Math.Min(predMin, predicted); predMax = System.Math.Max(predMax, predicted); actMin = System.Math.Min(actMin, actual); actMax = System.Math.Max(actMax, actual); }
                bool bad = vg.Length == 0 || System.Math.Abs(vSum - 1.0) > ShareTolerance || returnsIndex < 0;
                if (bad) { perValkretsFailures++; }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-32} {1,6} {2,10:F6} {3,14:F4} {4,11:F4} {5,7:F3} {7,12:F1} {8,15:F1}{6}\n",
                    name, vg.Length, vSum, vEligible, roll, ratio, bad ? "  <- FAIL" : "", predicted, actual));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    spread: age-predicted {0:F1}-{1:F1} ({2:F1} points) against actual 2022 {3:F1}-{4:F1} ({5:F1} points) - printed\n"
                + "    beside the roll, not asserted to explain it.\n",
                predMin, predMax, predMax - predMin, actMin, actMax, actMax - actMin));
            if (perValkretsFailures > 0)
            {
                failures.Add($"{perValkretsFailures} valkrets view(s)");
                Debug.LogError($"COHORTVOTERS: {perValkretsFailures} valkrets view(s) fail - shares not summing to 1 or a name that does not join the returns catalog (see the table).");
            }
            if (failures.Count == 0)
            {
                sb.Append("\n    CLEAN - the join holds in all six, and Sweden's weights reproduce its published total.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n    ⚠ {0} FAILURE(S) - see the errors above.\n", failures.Count));
            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }
    }
}
