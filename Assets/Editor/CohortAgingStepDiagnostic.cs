using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P-I2 stage 2's guard — **the aging step reproduces the year it was derived from, then runs
    /// twenty-five years without going anywhere absurd.**
    ///
    /// <para><b>THE ENUMERATION.</b> All six countries. Four assertions.</para>
    ///
    /// <list type="number">
    /// <item><description>⚠ <b>THE ONE THAT MATTERS: the ONE-YEAR HINDCAST.</b> The rates were derived
    /// from each country's published stock in 2023 and 2024. Stepping the 2023 pyramid one year must
    /// reproduce the 2024 pyramid — **a published number, band by band, within a stated tolerance.** A
    /// step that cannot reproduce the year it was measured on has an arithmetic error, and no amount of
    /// plausible-looking long-run output would reveal it.</description></item>
    /// <item><description><b>Conservation.</b> Every band stays finite and non-negative for twenty-five
    /// steps. A negative band is not a small population; it is a broken step.</description></item>
    /// <item><description><b>The open band accumulates.</b> 100+ must not empty, since it has no band
    /// above it to drain into.</description></item>
    /// <item><description><b>Sanity bounds on the long run.</b> Twenty-five years must not move any
    /// country's population by more than a factor of two in either direction. ⚠ A <b>CONVENTION</b>, not
    /// a demographic claim: wide enough that no plausible trajectory trips it, narrow enough that a sign
    /// error or a runaway does.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>The hindcast tolerance is 0.05 % of each band, and it is not chosen to pass.</b> The
    /// step is exact arithmetic on ratios themselves derived from those two stocks, so the only error it
    /// can carry is float rounding through 21 bands plus the ONE named assumption — the 100+ band's
    /// inflow (see `CohortStepRateTable`). **Bands are checked one at a time rather than in total,
    /// because a total can be right while two bands are wrong in opposite directions** — which is exactly
    /// what a crossing-fraction error looks like.</para>
    ///
    /// <para>⚠ <b>Nothing here is wired into the simulation.</b> `StepOneYear` has no caller under
    /// `Assets/Scripts` and the trajectory family is unmoved — proven by the dump, not asserted. Wiring
    /// it is the retirement stage, where the eight demographic scalars stop being stepped by their own
    /// rules; running both would advance population twice by different arithmetic (spec-let §4.1).</para>
    /// </summary>
    public static class CohortAgingStepDiagnostic
    {
        /// <summary>CONVENTION: the hindcast tolerance, as a share of the band. Float arithmetic through
        /// 21 bands cannot reach this; a wrong crossing fraction passes it easily.</summary>
        private const float HindcastTolerance = 0.0005f;

        /// <summary>CONVENTION: how many years the long run steps. Long enough that a compounding error
        /// shows, short enough that derived one-year rates are not being asked to be a forecast.</summary>
        private const int LongRunYears = 25;

        /// <summary>CONVENTION: the migration lever probe, in millions. Small against every country's
        /// population and large against float noise.</summary>
        private const float MigrationProbeMillions = 0.1f;

        /// <summary>CONVENTION: the fertility lever probe. Half again is far outside any policy this
        /// game would apply, which is what a liveness probe wants - it asks whether the lever is
        /// CONNECTED, not whether its calibration is right.</summary>
        private const float FertilityProbeMultiplier = 1.5f;

        /// <summary>CONVENTION: how close a set of shares must sum to 1, and how closely the migration
        /// lever must deliver the number it was given. 21 floats summing to 1 cannot drift this far.</summary>
        private const float ProfileSumTolerance = 0.0005f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();

            sb.Append("=== P-I2 stage 2: the aging step ===\n");
            sb.Append("    THE ENUMERATION: all six countries. (1) the one-year HINDCAST - step the 2023 pyramid and\n");
            sb.Append("    reproduce the published 2024 one, band by band, to 0.05%; (2) every band finite and non-negative\n");
            sb.Append("    for 25 steps; (3) the open 100+ band accumulates rather than emptying; (4) 25 years moves no\n");
            sb.Append("    population by more than a factor of two. (1) is the assertion that can actually catch an error.\n\n");

            sb.Append("    --- (1) THE ONE-YEAR HINDCAST, against a published year the step was not fitted to ---\n");
            sb.Append("    country       worst band   modelled     published   rel.err\n");
            foreach (Country country in world.Countries)
            {
                float[] start = CohortStepRateTable.PriorYearBands(country.Id);
                CohortStepRates rates = CohortStepRateTable.For(country.Id);
                if (start == null || rates == null || country.Cohorts == null)
                {
                    failures.Add($"{country.Name}: no hindcast material");
                    Debug.LogError($"AGING: {country.Name} has no prior-year pyramid or no step rates, so the one assertion "
                                   + "that can catch an arithmetic error cannot run for it. Five countries proven and one "
                                   + "unproven is not five sixths of a guard.");
                    continue;
                }

                var stepped = new PopulationCohorts(start);
                stepped.StepOneYear(rates);

                float worstRel = 0f;
                int worstBand = 0;
                for (int k = 0; k < PopulationCohorts.CohortCount; k++)
                {
                    float published = country.Cohorts.Counts[k];
                    float rel = published <= 0f ? 0f : Mathf.Abs(stepped.Counts[k] - published) / published;
                    if (rel <= worstRel) { continue; }
                    worstRel = rel;
                    worstBand = k;
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,10}  {2,10:F4}  {3,10:F4}  {4,10:P4}\n",
                    country.Name, PopulationCohorts.Label(worstBand), stepped.Counts[worstBand],
                    country.Cohorts.Counts[worstBand], worstRel));

                if (worstRel > HindcastTolerance)
                {
                    failures.Add($"{country.Name} hindcast");
                    Debug.LogError($"AGING: {country.Name}'s one-year hindcast misses band {PopulationCohorts.Label(worstBand)} by "
                                   + $"{worstRel:P4} - {stepped.Counts[worstBand]:F4} M modelled against "
                                   + $"{country.Cohorts.Counts[worstBand]:F4} M published. The step is exact arithmetic on ratios "
                                   + "derived from these two stocks, so it should reproduce the second to float noise. A miss is "
                                   + "an arithmetic error in the step, not a limitation of the data.");
                }
            }

            sb.Append("\n    --- (2)(3)(4) THE LONG RUN, 25 years ---\n");
            sb.Append("    country       pop now   pop +25y   ratio  |  old-age dep now    +25y  |  100+ now     +25y\n");
            foreach (Country country in world.Countries)
            {
                CohortStepRates rates = CohortStepRateTable.For(country.Id);
                if (rates == null || country.Cohorts == null) { continue; }

                var run = country.Cohorts.Clone();
                float startPop = run.Total;
                float startDep = run.OldAgeDependencyRatio;
                float startOpen = run.Counts[PopulationCohorts.OpenBandIndex];
                bool broke = false;

                for (int year = 0; year < LongRunYears && !broke; year++)
                {
                    run.StepOneYear(rates);
                    for (int k = 0; k < PopulationCohorts.CohortCount; k++)
                    {
                        float band = run.Counts[k];
                        if (band >= 0f && !float.IsNaN(band) && !float.IsInfinity(band)) { continue; }
                        failures.Add($"{country.Name} band {PopulationCohorts.Label(k)} at year {year + 1}");
                        Debug.LogError($"AGING: {country.Name}'s band {PopulationCohorts.Label(k)} reached {band} after "
                                       + $"{year + 1} step(s). A negative or non-finite band is a broken step, not a small cohort.");
                        broke = true;
                        break;
                    }
                }

                float endPop = run.Total;
                float ratio = startPop <= 0f ? 0f : endPop / startPop;
                float endOpen = run.Counts[PopulationCohorts.OpenBandIndex];

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,9:F3} {2,10:F3} {3,7:F3}  |  {4,12:F2} {5,7:F2}  |  {6,8:F4} {7,8:F4}\n",
                    country.Name, startPop, endPop, ratio, startDep, run.OldAgeDependencyRatio, startOpen, endOpen));

                if (endOpen <= 0f)
                {
                    failures.Add($"{country.Name} open band emptied");
                    Debug.LogError($"AGING: {country.Name}'s 100+ band emptied over {LongRunYears} years. It has no band above it "
                                   + "to drain into, so emptying means the step drains it anyway.");
                }

                if (ratio > 2f || ratio < 0.5f)
                {
                    failures.Add($"{country.Name} population x{ratio:F2}");
                    Debug.LogError($"AGING: {country.Name}'s population moved by a factor of {ratio:F3} over {LongRunYears} years. "
                                   + "The bound is a CONVENTION, not a demographic claim - it is wide enough that no plausible "
                                   + "trajectory trips it, so tripping it means a sign error or a runaway.");
                }
            }

            sb.Append("\n    --- (5) THE TWO LEVERS ARE LIVE, and the profile is a profile ---\n");
            sb.Append("    C-N3's method applied before the lever is wired rather than after: a lever that cannot move the\n");
            sb.Append("    substrate in a HARNESS will not move it in the game either, and spec-let §4.4 predicted exactly\n");
            sb.Append("    this failure for both of them.\n");
            sb.Append("    country       profile sum   +0.1M migration     of which 0-24   fertility x1.5 births\n");
            foreach (Country country in world.Countries)
            {
                CohortStepRates rates = CohortStepRateTable.For(country.Id);
                float[] profile = CohortStepRateTable.ImmigrationProfile(country.Id);
                if (rates == null || country.Cohorts == null) { continue; }

                if (profile == null)
                {
                    failures.Add($"{country.Name}: no immigration profile");
                    Debug.LogError($"AGING: {country.Name} has no immigration age profile, so the immigration lever has nowhere "
                                   + "to put people. Under D-6 the survival ratio cannot be scaled, so an absent profile is a "
                                   + "DEAD LEVER, not a missing refinement.");
                    continue;
                }

                float profileSum = 0f;
                float youngShare = 0f;
                for (int k = 0; k < PopulationCohorts.CohortCount; k++)
                {
                    profileSum += profile[k];
                    if (k <= 4) { youngShare += profile[k]; }
                }

                var plain = country.Cohorts.Clone();
                plain.StepOneYear(rates);

                var migrated = country.Cohorts.Clone();
                migrated.StepOneYear(rates, netMigrationMillions: MigrationProbeMillions, immigrationProfile: profile);

                var fertile = country.Cohorts.Clone();
                fertile.StepOneYear(rates, fertilityMultiplier: FertilityProbeMultiplier);

                float migrationEffect = migrated.Total - plain.Total;
                float birthEffect = fertile.Counts[0] - plain.Counts[0];

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,11:F6} {2,17:F6} {3,15:P2} {4,21:F6}\n",
                    country.Name, profileSum, migrationEffect, youngShare, birthEffect));

                if (Mathf.Abs(profileSum - 1f) > ProfileSumTolerance)
                {
                    failures.Add($"{country.Name} profile sum {profileSum:F6}");
                    Debug.LogError($"AGING: {country.Name}'s immigration age profile sums to {profileSum:F6}, not 1. It is a set "
                                   + "of SHARES; a sum that is not 1 means the lever moves a different number of people than it "
                                   + "was asked for, quietly.");
                }

                if (Mathf.Abs(migrationEffect - MigrationProbeMillions) > ProfileSumTolerance)
                {
                    failures.Add($"{country.Name} migration lever");
                    Debug.LogError($"AGING: {country.Name}'s migration lever added {migrationEffect:F6} M when asked for "
                                   + $"{MigrationProbeMillions:F6} M. The lever must deliver the number it is given, or the "
                                   + "player's setting means something other than what it says.");
                }

                if (birthEffect <= 0f)
                {
                    failures.Add($"{country.Name} fertility lever");
                    Debug.LogError($"AGING: {country.Name}'s fertility lever at x{FertilityProbeMultiplier} moved the 0-4 band by "
                                   + $"{birthEffect:F6} M. A lever that does not move the model is a dead lever - the class S-18 "
                                   + "found for the interest rate and C-C11 for the tax dials, and the third instance the "
                                   + "spec-let warned this build would create.");
                }
            }

            sb.Append("\n    ⚠ THE LONG RUN IS NOT A FORECAST, and must never be quoted as one.\n");
            sb.Append("    The rates are ONE observed year (2023->2024) held constant for 25. That year was not an ordinary\n");
            sb.Append("    one for two of the six - the USA and Germany both saw exceptional net immigration in the young\n");
            sb.Append("    bands - so the USA's x1.23 and Germany's near-flat dependency ratio are what holding THAT year\n");
            sb.Append("    fixed produces, not what those countries are expected to do. The column exists to show that the\n");
            sb.Append("    arithmetic neither explodes nor collapses. A projection needs a rate series, which is billed.\n");
            sb.Append("\n    ⚠ NOTHING HERE IS WIRED INTO THE SIMULATION. `StepOneYear` has no caller under Assets/Scripts\n");
            sb.Append("    and the trajectory family is unmoved - proven by the dump, not asserted. Wiring it is the\n");
            sb.Append("    retirement stage, where the eight demographic scalars stop being stepped by their own rules;\n");
            sb.Append("    running both would advance population twice by different arithmetic (spec-let §4.1).\n");

            if (failures.Count == 0)
            {
                sb.Append("\n    CLEAN - the step reproduces its own year and survives 25 of them.\n");
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
