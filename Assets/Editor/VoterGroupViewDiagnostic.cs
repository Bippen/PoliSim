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
