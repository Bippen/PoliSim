using System;
using System.Globalization;
using UnityEngine;

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
        /// <para>⚠ <b>THE TWO PLAYER LEVERS REACH THE MODEL THROUGH THIS METHOD AND NOWHERE ELSE.</b>
        /// The cohort spec-let's §4.4 predicted that a substrate landing without them re-pointed would
        /// leave both demographic levers as no-ops — *"three dead levers would be a pattern, not an
        /// accident"*, after S-18's interest rate and C-C11's tax dials. `fertilityMultiplier` scales the
        /// general fertility rate (the family lever); `netMigrationMillions` adds or removes people
        /// across the bands by the country's own sourced immigration age profile (the immigration lever).
        /// **Both default to no-ops**, so a caller that forgets them gets the unforced trajectory rather
        /// than a silent zero.</para>
        ///
        /// <para>⚠ <b>The migration term is ADDITIVE and cannot be anything else.</b> D-6 made `Survival`
        /// deaths and net migration together, so there is nothing inside it to scale. That is the decision's
        /// stated cost, paid here.</para>
        /// </summary>
        /// <param name="rates">The country's own derived step rates.</param>
        /// <param name="fertilityMultiplier">1 = the observed rate. The family policy lever.</param>
        /// <param name="netMigrationMillions">Millions added (or removed, if negative) across the bands
        /// by <paramref name="immigrationProfile"/>. The immigration policy lever.</param>
        /// <param name="immigrationProfile">Band shares summing to 1. Required only when
        /// <paramref name="netMigrationMillions"/> is non-zero.</param>
        public void StepOneYear(CohortStepRates rates, float fertilityMultiplier = 1f,
            float netMigrationMillions = 0f, float[] immigrationProfile = null)
        {
            if (rates == null) { return; }

            Counts = Generate(Counts, rates, fertilityMultiplier, netMigrationMillions, immigrationProfile);
        }

        /// <summary>
        /// **P-I2 stage 3 — the same step, ANCHORED to a published projection (D-15 (c), D-19 (b)).**
        ///
        /// <para><b>Why this exists.</b> Stage 3 was built once without an anchor and reverted on its own
        /// measurement: the step applies one observed year's rates forever, so over the horizon the model
        /// actually runs, two countries reached the population ceiling and three reached the floor. The
        /// retired scalars had an anchor — they mean-reverted toward a steady-state growth rate — and the
        /// spec-let's §4.2 said in advance that a substrate without an equivalent one would compound.</para>
        ///
        /// <para>⚠ <b>AND THE ANCHOR HAS NO CONVERGENCE SPEED, WHICH IS THE POINT.</b> §141 named the
        /// danger precisely: *"that convergence has a speed, and a speed nothing sources is an authored
        /// figure in the most load-bearing place in the model."* So there is no speed here and no blend
        /// weight to tune. The pyramid is placed **on** the published trajectory each year and displaced
        /// from it only by what the levers actually did:</para>
        ///
        /// <code>
        /// stepped   = Generate(Counts,          rates, the levers as set)
        /// neutral   = Generate(targetThisYear,  rates, no levers at all)
        /// Counts[b] = targetNextYear[b] * (stepped[b] / neutral[b])
        /// </code>
        ///
        /// <para><b>Read the ratio as "what the levers did to this band".</b> With the levers neutral and
        /// the pyramid already on the target, <c>stepped</c> and <c>neutral</c> are the same array, the
        /// ratio is exactly 1, and the model follows the publisher exactly — **asserted by the hindcast,
        /// not asserted here**. Move a lever and the band is displaced by the size of that lever's effect,
        /// and it stays displaced while the lever is held. ⚠ **It cannot run away**, because next year's
        /// base is read from the publisher again rather than from this year's result — which is precisely
        /// the anchor semantics `NaturalBirthRate` has always had, carried across to the substrate as
        /// §4.2 demanded.</para>
        ///
        /// <para>⚠ <b>THE PRICE, AND IT IS STATED HERE BECAUSE THIS IS WHERE IT IS PAID.</b> The
        /// population is no longer purely generated. It is generated **and then placed on a published
        /// projection**, which is a different claim about what the model knows: between gates the model is
        /// not forecasting the population, it is tracking a forecast and modelling the deviation from it.
        /// D-15 recorded that this sentence is the cost of option (c), and that it must be written where
        /// the code is rather than in the register.</para>
        ///
        /// <para>⚠ <b>Both targets must be REBASED</b> — see <see cref="RebasedTarget"/>. Passing a raw
        /// projection would step the pyramid onto the publisher's base year instead of its own seeded one
        /// and put a discontinuity in year zero (D-19).</para>
        /// </summary>
        /// <param name="targetThisYear">The rebased target pyramid for the year being stepped FROM.</param>
        /// <param name="targetNextYear">The rebased target pyramid for the year being stepped TO.</param>
        public void StepOneYearAnchored(CohortStepRates rates, float[] targetThisYear, float[] targetNextYear,
            float fertilityMultiplier = 1f, float netMigrationMillions = 0f, float[] immigrationProfile = null)
        {
            if (rates == null) { return; }

            // ⚠ No anchor available is not a reason to silently do something else: fall back to the
            // unanchored step, which is stage 2's measured behaviour, rather than inventing a target.
            if (targetThisYear == null || targetNextYear == null)
            {
                StepOneYear(rates, fertilityMultiplier, netMigrationMillions, immigrationProfile);
                return;
            }

            Counts = AnchoredNext(rates, targetThisYear, targetNextYear, fertilityMultiplier, netMigrationMillions, immigrationProfile);
        }

        /// <summary>
        /// The pyramid <see cref="StepOneYearAnchored"/> would step THIS one to, returned rather than
        /// assigned - F2 step 4's game path reads a year's step every day and commits it once, so the
        /// arithmetic lives here and the assignment there. Null targets fall back to the unanchored
        /// generative step, as the assigning form does.
        /// </summary>
        public float[] AnchoredNext(CohortStepRates rates, float[] targetThisYear, float[] targetNextYear,
            float fertilityMultiplier = 1f, float netMigrationMillions = 0f, float[] immigrationProfile = null)
        {
            if (rates == null) { return (float[])Counts.Clone(); }
            if (targetThisYear == null || targetNextYear == null)
            {
                return Generate(Counts, rates, fertilityMultiplier, netMigrationMillions, immigrationProfile);
            }

            float[] stepped = Generate(Counts, rates, fertilityMultiplier, netMigrationMillions, immigrationProfile);
            float[] neutral = Generate(targetThisYear, rates, 1f, 0f, null);

            var next = new float[CohortCount];
            for (int k = 0; k < CohortCount; k++)
            {
                // ⚠ A neutral band of zero would make the ratio undefined. No band of any of the six is
                // zero at any published year - but a division that produces a silent infinity in the open
                // band is exactly the kind of thing that is discovered a thousand turns later, so the
                // guard is written rather than argued away. No anchor for this band means no displacement.
                float ratio = neutral[k] > 0f ? stepped[k] / neutral[k] : 1f;
                next[k] = Mathf.Max(0f, targetNextYear[k] * ratio);
            }
            return next;
        }

        /// <summary>
        /// F2 step 4: the births a year's step implies, read off its result rather than recomputed -
        /// the 0–4 band's inflow. Only births and the immigration lever's migrants enter that band
        /// (the survivors who stay are <c>before[0] × Survival[0] × (1 − Crossing[0])</c>), so the
        /// inflow net of <paramref name="migrantsIntoBand0"/> is the step's births. Exact for the
        /// generative step; for the anchored step it is the births the anchor's own trajectory
        /// implies, which is the quantity a crude birth rate read from the substrate should report.
        /// </summary>
        public static float ImpliedBirths(float[] before, float[] after, CohortStepRates rates, float migrantsIntoBand0)
        {
            if (before == null || after == null || rates == null) { return 0f; }
            float stayers = before[0] * rates.Survival[0] * (1f - rates.Crossing[0]);
            return Mathf.Max(0f, after[0] - stayers - migrantsIntoBand0);
        }

        /// <summary>
        /// **D-19 (b): the projection's TRAJECTORY, rebased onto the seeded pyramid's own base year.**
        ///
        /// <para>⚠ <b>The two publishers disagree about the present, and neither is wrong.</b> The
        /// substrate is seeded from an OBSERVED stock; a projection's base year is PROJECTED, computed
        /// before that observation existed. For Sweden 2024 the gap is 0.84 % on the total and larger in
        /// the young bands. Converging on the projection's LEVELS would therefore jerk the model off its
        /// own sourced seed at turn zero and quietly invalidate the reconciliation
        /// `CohortSubstrateDiagnostic` exists to assert.</para>
        ///
        /// <para><b>So only the ratio is taken.</b> <c>seeded[b] * projectionYear[b] / projectionBase[b]</c>
        /// — the target equals the seeded pyramid exactly in the base year, by construction, and thereafter
        /// carries the publisher's own band-by-band change. **A projection's forecast is its trajectory;
        /// its base year is the one year it is worst at.**</para>
        /// </summary>
        public static float[] RebasedTarget(float[] seeded, float[] projectionBase, float[] projectionYear)
        {
            if (seeded == null || projectionBase == null || projectionYear == null) { return null; }

            var target = new float[CohortCount];
            for (int k = 0; k < CohortCount; k++)
            {
                // A zero base band would make the rebasing undefined; hold the seeded value rather than
                // emitting an infinity. Same reasoning as the anchored step's own guard.
                target[k] = projectionBase[k] > 0f
                    ? seeded[k] * projectionYear[k] / projectionBase[k]
                    : seeded[k];
            }

            return target;
        }

        /// <summary>The generative step, on any pyramid rather than only on this one. ⚠ Extracted at stage
        /// 3 because the anchor needs to run the SAME arithmetic on the target to find out what the levers
        /// did — running a second, similar implementation there would have been two things to keep true,
        /// and the first disagreement between them would have been resolved by whichever was edited last.</summary>
        private static float[] Generate(float[] counts, CohortStepRates rates, float fertilityMultiplier,
            float netMigrationMillions, float[] immigrationProfile)
        {
            // Births first, from the population as it stands BEFORE the survivors move - a child born
            // this year is born to the women who were here at its start, not to the survivors of the
            // step. Computed now, added last.
            float childbearing = InAgeRange(counts, 15, 49) * rates.FemaleShareOfChildbearingAge;
            float births = childbearing * rates.GeneralFertilityRate * fertilityMultiplier;

            var next = new float[CohortCount];
            for (int k = 0; k < CohortCount; k++)
            {
                float survivors = counts[k] * rates.Survival[k];
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

            if (netMigrationMillions != 0f && immigrationProfile != null)
            {
                for (int k = 0; k < CohortCount; k++)
                {
                    // A band can be emptied by an outflow but never driven negative: a negative cohort is
                    // not a small population, it is a broken model, and the lever must not be able to
                    // produce one at any setting.
                    next[k] = Mathf.Max(0f, next[k] + netMigrationMillions * immigrationProfile[k]);
                }
            }

            return next;
        }

        /// <summary>The instance <see cref="InAgeRange(int,int)"/>'s arithmetic, over any pyramid.</summary>
        private static float InAgeRange(float[] counts, int fromAge, int toAge)
        {
            int first = Math.Max(0, fromAge / CohortWidth);
            int last = toAge >= OpenBandIndex * CohortWidth ? OpenBandIndex : Math.Min(OpenBandIndex, toAge / CohortWidth);
            float sum = 0f;
            for (int i = first; i <= last; i++) { sum += counts[i]; }
            return sum;
        }

        /// <summary>The band's human label, so a screen or a log never prints a bare index.</summary>
        public static string Label(int index) =>
            index == OpenBandIndex
                ? "100+"
                : string.Format(CultureInfo.InvariantCulture, "{0}-{1}", index * CohortWidth, index * CohortWidth + CohortWidth - 1);
    }
}
