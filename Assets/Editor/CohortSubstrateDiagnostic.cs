using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P-I2 stage 1's guard — **the cohort substrate reconciles against its own publishers, and against
    /// the population the model already carried.**
    ///
    /// <para><b>THE ENUMERATION.</b> All six countries. For each: (1) the 21 bands must sum to the
    /// publisher's OWN total, transcribed separately in `PopulationPyramids.PublishedTotal` — Eurostat's
    /// <c>TOTAL</c> age class, the Census file's <c>AGE=999</c> row; (2) every band must be positive and
    /// finite; (3) the derived dependency ratio is REPORTED against the seeded
    /// `Country.BaselineDependencyRatio`; (4) the pyramid total is REPORTED against the seeded
    /// `EconomyState.Population`.</para>
    ///
    /// <para>⚠ <b>(1) is the only clause that FAILS, and it is the only one that can.</b> The sum is a
    /// transcription check with a real adversary: 126 band figures were folded from single years and
    /// typed into source, and the total they are checked against came from a different field of the
    /// dataset. A slip in any one band breaks it. **The guard was proven in both directions before it was
    /// committed** — a single band perturbed by 0.01 M made it fail, naming the country and the size of
    /// the gap.</para>
    ///
    /// <para>⚠ <b>(3) and (4) are FINDINGS, not failures, and the distinction is the whole design.</b>
    /// Spec-let §4.5 predicted the dependency ratio would become exactly computable and that each
    /// country's seeded value would then be either right or wrong. It is now computable and they
    /// disagree. **Failing on that would be failing on a measurement this stage exists to take** — and
    /// worse, it would push toward re-seeding the pyramids to match the old scalars, which is tuning a
    /// sourced figure to pass a gate. The disagreement is the input to the retirement stage, which is
    /// where a scalar is allowed to move and must be explained per country.</para>
    /// </summary>
    public static class CohortSubstrateDiagnostic
    {
        /// <summary>CONVENTION: the reconciliation tolerance, in millions. The bands are stored as float
        /// millions at six decimals, so 21 of them accumulate rounding well below this; 0.001 M is one
        /// thousand people against national totals of 10 to 340 million, which catches a typo in any
        /// digit that matters and does not chase float noise.</summary>
        private const float ReconcileToleranceMillions = 0.001f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();

            sb.Append("=== P-I2 stage 1: the cohort substrate ===\n");
            sb.Append("    THE ENUMERATION: all six countries; 21 five-year bands each (0-4 ... 95-99, 100+), millions of\n");
            sb.Append("    persons. Eurostat demo_pjan sex=T time=2024 (1 Jan 2024) for the EU five; US Census PEP vintage\n");
            sb.Append("    2024 nc-est2024-agesex-res.csv POPESTIMATE2024 SEX=0 (1 Jul 2024) for the USA.\n\n");
            sb.Append("    country     bands sum   published    gap  |  old-age dep   seeded  |  pyramid   seeded pop\n");
            sb.Append("    ---------------------------------------------------------------------------------------\n");

            foreach (Country country in world.Countries)
            {
                if (country.Cohorts == null)
                {
                    failures.Add($"{country.Name}: no pyramid seeded");
                    Debug.LogError($"COHORTS: {country.Name} has no age pyramid. Every country the world builds must carry one, "
                                   + "or the substrate is present for some countries and absent for others - which is worse than "
                                   + "absent for all, because the code above it cannot tell.");
                    continue;
                }

                // F2 step 4: the reconciliation is of the SEEDED table against its publisher - the live
                // pyramid on the country has been walked to the epoch (CohortDemographics.WalkToEpoch)
                // and is no longer the 2024 stock. The band checks below stay on the live pyramid.
                float sum = PopulationPyramids.For(country.Id).Total;
                float published = PopulationPyramids.PublishedTotal[country.Id];
                float gap = sum - published;

                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    float band = country.Cohorts.Counts[i];
                    if (band > 0f && !float.IsNaN(band) && !float.IsInfinity(band)) { continue; }
                    failures.Add($"{country.Name} band {PopulationCohorts.Label(i)}");
                    Debug.LogError($"COHORTS: {country.Name}'s band {PopulationCohorts.Label(i)} is {band}. Every band of a real "
                                   + "pyramid holds people; a zero or a NaN is a transcription failure, not a small country.");
                }

                if (Mathf.Abs(gap) > ReconcileToleranceMillions)
                {
                    failures.Add($"{country.Name} reconciliation");
                    Debug.LogError($"COHORTS: {country.Name}'s 21 bands sum to {sum:F6} M against the publisher's own total of "
                                   + $"{published:F6} M - a gap of {gap:F6} M. ⚠ The two numbers come from DIFFERENT fields of the "
                                   + "same source and reconciled to the person when they were fetched, so a gap now is a "
                                   + "transcription error in this repository, not a disagreement in the data.");
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-10} {1,10:F4} {2,11:F4} {3,7:F4}  |  {4,8:F2} {5,7:F1}  |  {6,8:F3} {7,8:F1}\n",
                    country.Name, sum, published, gap,
                    country.Cohorts.OldAgeDependencyRatio, country.BaselineDependencyRatio,
                    sum, country.State.Population));
            }

            sb.Append("\n    ⚠ THE SPEC-LET WAS WRONG ABOUT THE DEFINITION, and this stage is what found out.\n");
            sb.Append("    §3 specified the replacement as '(0-14 + 65+) / 15-64, the standard definition'. The field the model\n");
            sb.Append("    actually seeds is the OLD-AGE ratio, 65+ / 15-64 - the column above, which lands within ~0.1 of the\n");
            sb.Append("    seed for Sweden, Germany and the USA. The TOTAL ratio reads 60.52, 57.14 and 55.14 for those same\n");
            sb.Append("    three: roughly DOUBLE. Building the derivation on the spec-let's own words would have doubled every\n");
            sb.Append("    country's dependency ratio SILENTLY - the exact class of quiet breakage §4's collision map exists\n");
            sb.Append("    to catch. The spec-let is corrected; the code is not written to match a wrong spec.\n");
            sb.Append("\n    ⚠ FINDINGS, not failures - and deliberately so.\n");
            sb.Append("    The dependency ratio is now EXACTLY computable (spec-let §4.5), so each country's seeded value is\n");
            sb.Append("    either right or wrong and this stage is what finds out. Same for Population against the pyramid's\n");
            sb.Append("    own sum. Failing here would push toward re-seeding a SOURCED pyramid to match an authored scalar,\n");
            sb.Append("    which is tuning a figure to pass a gate. Both disagreements are the retirement stage's input, and\n");
            sb.Append("    that stage is where a scalar may move - with its family explained per country.\n");
            sb.Append("    ⚠ NOTHING IN EconomyState DERIVES FROM THESE BANDS YET. The trajectory dump is run to PROVE the\n");
            sb.Append("    no-policy family did not move, rather than the reasoning being trusted.\n");

            if (failures.Count == 0)
            {
                sb.Append("\n    CLEAN - all six pyramids reconcile against their own publishers.\n");
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
