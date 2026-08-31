using System;
using System.Globalization;

namespace PoliSim.Data
{
    /// <summary>
    /// P-I2 stage 1 — **the cohort substrate**, ruled at D-4 (a) on the cohort spec-let
    /// (`POLISIM_COHORT_SPECLET.md` §1: five-year cohorts, recommended because the sourcing FITS them,
    /// not merely because it permits them).
    ///
    /// <para><b>21 cohorts</b>: 0–4, 5–9, … 95–99, and an open 100+. Counts are in **MILLIONS of
    /// persons**, the same unit `EconomyState.Population` already carries, so that nothing in this build
    /// introduces a second population unit to reconcile.</para>
    ///
    /// <para>⚠ <b>What this stage deliberately does NOT do.</b> Nothing here ages, and nothing in
    /// `EconomyState` is derived from it yet. The spec-let's §4 collision map lists five ways the
    /// substrate goes wrong, and every one of them is in the step that RETIRES the eight demographic
    /// scalars — double-stepping, the lost natural-trajectory anchor, the BASELINE family, the two
    /// player levers that go dead, and the re-seeded dependency ratio. **This stage lands the numbers
    /// and the arithmetic and proves the trajectory did not move**; the retirement is its own stage with
    /// its own explained family. Two BASELINE families landing together cannot be explained apart.</para>
    ///
    /// <para>⚠ <b>The uniform-within-cohort assumption is not made here and must not be made silently
    /// later.</b> When the aging step lands, shifting 1/5 of a five-year cohort per year assumes people
    /// are spread evenly inside it. That is the standard approximation and it is standardly wrong at the
    /// two ends of the pyramid. The spec-let requires it to be stated in the step's own doc comment
    /// rather than discovered; this comment is the reminder that owes it.</para>
    /// </summary>
    [Serializable]
    public class PopulationCohorts
    {
        /// <summary>CONVENTION: 0–4 … 95–99 is twenty bands, plus one open band for 100+.</summary>
        public const int CohortCount = 21;

        /// <summary>CONVENTION: the band width in years. The last band is open-ended and is NOT five
        /// years wide, which is why every loop over cohorts must treat index 20 separately.</summary>
        public const int CohortWidth = 5;

        /// <summary>CONVENTION: the index of the open 100+ band.</summary>
        public const int OpenBandIndex = CohortCount - 1;

        /// <summary>Millions of persons per band, index 0 = ages 0–4.</summary>
        public float[] Counts = new float[CohortCount];

        public PopulationCohorts() { }

        public PopulationCohorts(float[] counts)
        {
            if (counts == null || counts.Length != CohortCount)
            {
                throw new ArgumentException($"A cohort pyramid must carry exactly {CohortCount} bands.", nameof(counts));
            }

            Counts = (float[])counts.Clone();
        }

        public PopulationCohorts Clone() => new PopulationCohorts(Counts);

        /// <summary>DERIVED: the population as the cohorts themselves state it, in millions.</summary>
        public float Total
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < CohortCount; i++) { sum += Counts[i]; }
                return sum;
            }
        }

        /// <summary>DERIVED: millions in the closed age interval [fromAge, toAge]. ⚠ The interval is
        /// snapped OUTWARD to whole bands, because a five-year substrate cannot answer a question
        /// narrower than five years and pretending otherwise is the resolution lie the spec-let's §1
        /// chose five-year cohorts to avoid. `toAge` of 200 or more reaches the open band.</summary>
        public float InAgeRange(int fromAge, int toAge)
        {
            int first = Math.Max(0, fromAge / CohortWidth);
            int last = toAge >= OpenBandIndex * CohortWidth ? OpenBandIndex : Math.Min(OpenBandIndex, toAge / CohortWidth);
            float sum = 0f;
            for (int i = first; i <= last; i++) { sum += Counts[i]; }
            return sum;
        }

        /// <summary>DERIVED: 65+ / 15-64, as a percentage — the OLD-AGE dependency ratio.
        /// <para>⚠ <b>THIS, and not the total dependency ratio, is what `EconomyState.DependencyRatio` has
        /// always held — and the cohort spec-let said the opposite.</b> Its §3 specified the replacement as
        /// *"(0–14 + 65+) / 15–64, the standard definition"*, which is A standard definition but not this
        /// model's. Measured against the seeds at P-I2 stage 1: Sweden 33.08 computed against 33.0 seeded,
        /// Germany 35.23 against 35.0, the USA 27.91 against 28.0 — while the TOTAL ratio gives 60.52,
        /// 57.14 and 55.14 for the same three. **Building the derivation on the spec-let's own words would
        /// have roughly DOUBLED every country's dependency ratio silently**, which is exactly the class of
        /// quiet breakage §4's collision map exists to catch. The spec-let is corrected where it is wrong
        /// rather than the code being written to match it.</para></summary>
        public float OldAgeDependencyRatio
        {
            get
            {
                float working = InAgeRange(15, 64);
                return working <= 0f ? 0f : 100f * InAgeRange(65, 999) / working;
            }
        }

        /// <summary>DERIVED: (0-14 plus 65+) / 15-64, as a percentage — the TOTAL dependency ratio. Kept
        /// and named because it is a real measure the substrate can now answer, and because naming both
        /// explicitly is the only way the confusion above cannot recur.</summary>
        public float TotalDependencyRatio
        {
            get
            {
                float working = InAgeRange(15, 64);
                return working <= 0f ? 0f : 100f * (InAgeRange(0, 14) + InAgeRange(65, 999)) / working;
            }
        }

        /// <summary>DERIVED: the share of the population aged 65 and over, as a percentage — the pension
        /// cost weight's own base (spec-let §3).</summary>
        public float ElderlyShare
        {
            get { float total = Total; return total <= 0f ? 0f : 100f * InAgeRange(65, 999) / total; }
        }

        /// <summary>DERIVED: the share aged 0–19, the education cost weight's base (spec-let §3).</summary>
        public float SchoolAgeShare
        {
            get { float total = Total; return total <= 0f ? 0f : 100f * InAgeRange(0, 19) / total; }
        }


        /// <summary>
        /// P-I2 stage 2 — **one year of aging, in place.** Spec-let §2's order, with §2's own uniform-1/5
        /// approximation replaced by the observed crossing fraction.
        ///
        /// <para>The step, per band, in this order and no other: (1) apply the band's survival ratio,
        /// which is deaths and net migration together as the data itself reports them; (2) split each
        /// band's survivors into those who stay and those who cross into the band above, by the observed
        /// crossing fraction; (3) the 100+ band accumulates rather than emptying, because there is no
        /// band above it; (4) births enter band 0–4 from the general fertility rate applied to the female
        /// share of the 15–49 bands.</para>
        ///
        /// <para>⚠ <b>WHY THE FEMALE SHARE IS A PARAMETER AND NOT A CONSTANT.</b> The substrate is
        /// sex-blind — 21 bands, both sexes — so the fertility denominator cannot be read out of it. The
        /// caller passes the country's own observed female share of 15–49, from the same publisher and
        /// the same year as everything else. **A hard-coded 0.5 would be an invented figure**, and it
        /// would be wrong in the direction that matters: the share is not 0.5 in any of the six.</para>
        ///
        /// <para>⚠ <b>What this step does NOT do, deliberately.</b> It does not touch `EconomyState`, and
        /// nothing in the turn loop calls it yet. Wiring it is the retirement stage's job, where the
        /// eight demographic scalars stop being stepped by their own rules — running both would advance
        /// population twice by different arithmetic, which is the spec-let's collision §4.1 and the one
        /// that would look like a plausible number while being wrong.</para>
        /// </summary>
        public void StepOneYear(CohortStepRates rates)
        {
            if (rates == null) { return; }

            // Births first, from the population as it stands BEFORE the survivors move - a child born
            // this year is born to the women who were here at its start, not to the survivors of the
            // step. Computed now, added last.
            float childbearing = InAgeRange(15, 49) * rates.FemaleShareOfChildbearingAge;
            float births = childbearing * rates.GeneralFertilityRate;

            var next = new float[CohortCount];
            for (int k = 0; k < CohortCount; k++)
            {
                float survivors = Counts[k] * rates.Survival[k];
                if (k == OpenBandIndex)
                {
                    // Nothing above 100+, so its survivors stay where they are and are joined by the
                    // band below. It ACCUMULATES; it does not empty.
                    next[k] += survivors;
                    continue;
                }

                float crossing = survivors * rates.Crossing[k];
                next[k] += survivors - crossing;
                next[k + 1] += crossing;
            }

            next[0] += births;
            Counts = next;
        }

        /// <summary>The band's human label, so a screen or a log never prints a bare index.</summary>
        public static string Label(int index) =>
            index == OpenBandIndex
                ? "100+"
                : string.Format(CultureInfo.InvariantCulture, "{0}-{1}", index * CohortWidth, index * CohortWidth + CohortWidth - 1);
    }
}
