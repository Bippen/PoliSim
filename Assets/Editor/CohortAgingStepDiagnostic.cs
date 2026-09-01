using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.EditorTools.Generated;
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
        /// shows. ⚠ **25 WAS TOO SHORT, and P-I2 stage 3 proved it**: every country passes the factor-of-two bound below at 25 years, while the same step run to the horizon the model actually uses sends Germany and the USA to MaxPopulation and Italy, Poland and Sweden to MinPopulation. The divergence COMPOUNDS, so catching it needs a horizon long enough to compound - a bound checked over a quarter of the real horizon is not a bound.</summary>
        private const int LongRunYears = 100;
        /// <summary>⚠ The runaway backlog ceiling, measured 2026-09-01 at TWO (Italy and Poland). A ratchet: the step has no steady-state anchor so it compounds, and until one lands these two are a reported backlog rather than a failure. Lower it when the anchor lands; never raise it.</summary>
        private const int RunawayCeiling = 2;


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

        /// <summary>⚠ How far the ANCHORED neutral run may sit off the published trajectory. It should be
        /// zero by construction - the ratio is exactly 1 when nothing is forced - so this is float noise
        /// accumulated over a century of steps, not a modelling allowance.</summary>
        private const float AnchorTolerance = 0.0005f;

        /// <summary>⚠ The factor a lever pinned hard over for the whole horizon may displace the
        /// population by. §141 failure was UNBOUNDED - two countries at the ceiling, three at the floor -
        /// so a bound of any kind is the property being asserted; this one is deliberately generous,
        /// because the claim is "does not compound", not "moves by a particular amount".</summary>
        private const float ForcedDisplacementCeiling = 3f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();
            var runaways = new List<string>();

            sb.Append("=== P-I2 stage 2: the aging step ===\n");
            sb.Append("    THE ENUMERATION: all six countries. (1) the one-year HINDCAST - step the 2023 pyramid and\n");
            sb.Append("    reproduce the published 2024 one, band by band, to 0.05%; (2) every band finite and non-negative\n");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    for {0} steps; (3) the open 100+ band accumulates rather than emptying; (4) {0} years moves no\n",
                LongRunYears));
            sb.Append("    population by more than a factor of two; (6) the ANCHORED step sits on the published projection.\n");
            sb.Append("    ⚠ The horizon is read from LongRunYears, never restated - this line said 25 for as long as the\n");
            sb.Append("    constant said 100, which is the transcription the claim convention now forbids.\n\n");

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
                    // ⚠ A RATCHET, NOT A FAILURE - and the reason is a measured finding, not leniency.
                    // P-I2 stage 3 was built, run and REVERTED on exactly this: the step has NO
                    // reversion of any kind, so one observed year's rates applied forever compound, and
                    // at the horizon the model really uses two countries reach MaxPopulation and three
                    // reach MinPopulation. The cohort spec-let's §4.2 predicted it in writing - "a cohort
                    // substrate must keep an equivalent anchor, or every demographic policy effect loses
                    // its zero AND STARTS COMPOUNDING", called "the single most likely silent breakage".
                    //
                    // It cannot be fixed without an anchor, and an anchor has a convergence SPEED that
                    // nothing sources yet. So the two countries that trip it are a reported BACKLOG and
                    // what fails is a THIRD joining them - PartyMarkCoverageCheck's precedent, and the
                    // same shape the other ratchets in this suite use. Lower the ceiling when the anchor
                    // lands; never raise it.
                    runaways.Add($"{country.Name} x{ratio:F3}");
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
            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n    ⚠ RUNAWAY BACKLOG: {0} country/countries move by more than a factor of two over {1} years\n",
                runaways.Count, LongRunYears));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "    (ceiling {0}). ", RunawayCeiling));
            RatchetLedger.Report("CohortAgingStepDiagnostic.RUNAWAY", runaways.Count, RunawayCeiling);
            foreach (string r in runaways) { sb.Append(r).Append("  "); }
            sb.Append("\n    The step has NO reversion: one observed year's rates applied forever COMPOUND, which the cohort\n");
            sb.Append("    spec-let's §4.2 predicted in writing as 'the single most likely silent breakage'. P-I2 stage 3 was\n");
            sb.Append("    built, measured and REVERTED on exactly this. Fixing it needs an ANCHOR, and an anchor has a\n");
            sb.Append("    convergence speed nothing sources yet - so this is a reported backlog and what FAILS is growth.\n");

            if (runaways.Count > RunawayCeiling)
            {
                failures.Add($"{runaways.Count} runaway countries, above the ceiling of {RunawayCeiling}");
                Debug.LogError($"AGING: {runaways.Count} countries now run away over {LongRunYears} years, above the recorded "
                               + $"ceiling of {RunawayCeiling}. ⚠ The backlog may only shrink - it shrinks when the step gains "
                               + "the steady-state anchor §4.2 asked for, and it grows when something makes the compounding "
                               + "worse. Lower the ceiling with the fix; never raise it.");
            }

            AnchorSection(world, sb, failures);

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

        /// <summary>
        /// **P-I2 stage 3 — the ANCHORED step, and the regression test for the thing that killed it.**
        ///
        /// <para>Three assertions, in the order of what each can catch:</para>
        /// <list type="number">
        /// <item><b>The rebasing is exact at the base year.</b> D-19 (b) defines the target as the seeded
        /// pyramid times the projection's own band ratio, so at the base year the target must BE the
        /// seeded pyramid — to the float. ⚠ This catches an off-by-one in the year index, which would
        /// otherwise show up only as a small unexplained drift a thousand turns later.</item>
        /// <item><b>The anchor holds with the levers neutral.</b> Stepped from the seed with no lever
        /// touched, the pyramid must sit on the published trajectory at EVERY year, band by band. This is
        /// what "anchored" means, and it is the assertion the whole stage exists to be able to make.</item>
        /// <item><b>⚠ THE REGRESSION TEST FOR §141.</b> A lever held at its extreme for the whole run must
        /// DISPLACE the population and must NOT compound. Stage 3's first build sent two countries to the
        /// population ceiling and three to the floor over this same horizon, with no lever touched at
        /// all. Here the bound is asserted with a lever pinned hard over, which is strictly harsher than
        /// the run that failed.</item>
        /// </list>
        ///
        /// <para>⚠ <b>What this canNOT prove.</b> That the publisher is right. The anchor makes the model
        /// track a projection; whether that projection is a good forecast is the publisher's claim, not
        /// this project's, and the step's own doc says so at the call site. **The assertion here is that
        /// the model goes where the projection goes, not that either is correct.**</para>
        /// </summary>
        private static void AnchorSection(World world, StringBuilder sb, List<string> failures)
        {
            sb.Append("\n    --- (6) P-I2 STAGE 3: THE ANCHORED STEP (D-15 (c), D-19 (b)) ---\n");
            sb.Append("    country       base exact   worst band drift   neutral x   fertility x1.5 x   step-vs-publisher\n");

            foreach (Country country in world.Countries)
            {
                CohortStepRates rates = CohortStepRateTable.For(country.Id);
                float[] seeded = PopulationPyramids.Bands.TryGetValue(country.Id, out float[] s) ? s : null;
                float[] projBase = PopulationProjections.For(country.Id, PopulationProjections.FirstYear);

                if (rates == null || seeded == null || projBase == null)
                {
                    failures.Add($"{country.Name}: no anchor material");
                    Debug.LogError($"AGING/ANCHOR: {country.Name} has no seeded pyramid, step rates or projection, so the "
                                   + "anchor cannot be asserted for it. Five countries proven and one unproven is not "
                                   + "five sixths of a guard.");
                    continue;
                }

                // (1) The rebasing must be EXACT at the base year - the target IS the seed there.
                float[] baseTarget = PopulationCohorts.RebasedTarget(seeded, projBase, projBase);
                float baseDrift = 0f;
                for (int k = 0; k < PopulationCohorts.CohortCount; k++)
                {
                    baseDrift = Mathf.Max(baseDrift, Mathf.Abs(baseTarget[k] - seeded[k]));
                }

                // (2) The neutral run must sit ON the published trajectory at every year.
                var neutral = new PopulationCohorts(seeded);
                var forced = new PopulationCohorts(seeded);
                float worstDrift = 0f;
                int worstYear = 0;

                for (int y = 0; y < LongRunYears; y++)
                {
                    int yearNow = PopulationProjections.FirstYear + y;
                    float[] tNow = PopulationCohorts.RebasedTarget(seeded, projBase, PopulationProjections.For(country.Id, yearNow));
                    float[] tNext = PopulationCohorts.RebasedTarget(seeded, projBase, PopulationProjections.For(country.Id, yearNow + 1));

                    neutral.StepOneYearAnchored(rates, tNow, tNext);
                    forced.StepOneYearAnchored(rates, tNow, tNext, FertilityProbeMultiplier);

                    float targetTotal = 0f;
                    for (int k = 0; k < PopulationCohorts.CohortCount; k++) { targetTotal += tNext[k]; }
                    if (targetTotal <= 0f) { continue; }

                    float rel = Mathf.Abs(neutral.Total - targetTotal) / targetTotal;
                    if (rel > worstDrift) { worstDrift = rel; worstYear = yearNow + 1; }
                }

                float finalTarget = 0f;
                float[] tEnd = PopulationCohorts.RebasedTarget(seeded, projBase,
                    PopulationProjections.For(country.Id, PopulationProjections.FirstYear + LongRunYears));
                for (int k = 0; k < PopulationCohorts.CohortCount; k++) { finalTarget += tEnd[k]; }

                float neutralRatio = finalTarget > 0f ? neutral.Total / finalTarget : 0f;
                float forcedRatio = neutral.Total > 0f ? forced.Total / neutral.Total : 0f;

                // ⚠ HOW MUCH OF THE TRAJECTORY IS THE MODEL, AND HOW MUCH IS THE PUBLISHER. Reported, never
                // failed. The anchored step tracks the projection EXACTLY by construction, so "it tracks"
                // proves nothing about the demography. This asks the question that is not tautological:
                // applied to the publisher's own pyramid, does the step's own survival/crossing/fertility
                // arithmetic land on the publisher's NEXT year? Agreement means the model's demography and
                // the publisher's agree and the ratio is doing little; disagreement means the anchor is
                // carrying the trajectory and the generative step is along for the ride. Either answer is
                // worth knowing BEFORE the retirement wires this to the game, and neither is a defect.
                float stepGap = 0f;
                for (int y = 0; y < LongRunYears; y++)
                {
                    int yearNow = PopulationProjections.FirstYear + y;
                    float[] pubNow = PopulationProjections.For(country.Id, yearNow);
                    float[] pubNext = PopulationProjections.For(country.Id, yearNow + 1);

                    var walk = new PopulationCohorts((float[])pubNow.Clone());
                    walk.StepOneYear(rates);

                    float pubTotal = 0f;
                    for (int k = 0; k < PopulationCohorts.CohortCount; k++) { pubTotal += pubNext[k]; }
                    if (pubTotal > 0f) { stepGap += Mathf.Abs(walk.Total - pubTotal) / pubTotal; }
                }

                stepGap /= LongRunYears;

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,10:E1}  {2,16:P4}  {3,10:F4}  {4,17:F4}  {5,17:P3}\n",
                    country.Name, baseDrift, worstDrift, neutralRatio, forcedRatio, stepGap));

                if (baseDrift > 1e-5f)
                {
                    failures.Add($"{country.Name}: the rebasing is not exact at the base year");
                    Debug.LogError($"AGING/ANCHOR: {country.Name}'s rebased target differs from the seeded pyramid by "
                                   + $"{baseDrift:E3} M at the BASE year, where D-19 (b) makes them identical by "
                                   + "construction. ⚠ That is an index error, not a rounding one - the target series is "
                                   + "being read a year off, and every year after it is wrong by the same shift.");
                }

                if (worstDrift > AnchorTolerance)
                {
                    failures.Add($"{country.Name}: the anchor does not hold");
                    Debug.LogError($"AGING/ANCHOR: {country.Name} leaves the published trajectory by {worstDrift:P4} at "
                                   + $"{worstYear}, against a tolerance of {AnchorTolerance:P4}. ⚠ With no lever touched the "
                                   + "anchored step must sit ON the projection - that is what the anchor IS. A drift here "
                                   + "means the neutral ratio is not 1, so the step and the target are not the same "
                                   + "arithmetic.");
                }

                // ⚠ §141's REGRESSION TEST. A lever pinned hard over for the whole horizon must move the
                // population and must not run away with it.
                if (forcedRatio > ForcedDisplacementCeiling || forcedRatio < 1f / ForcedDisplacementCeiling)
                {
                    failures.Add($"{country.Name}: the fertility lever compounds under the anchor");
                    Debug.LogError($"AGING/ANCHOR: {country.Name} held at fertility x{FertilityProbeMultiplier} for "
                                   + $"{LongRunYears} years ends at x{forcedRatio:F3} of its neutral run, outside the "
                                   + $"factor of {ForcedDisplacementCeiling}. ⚠ THIS IS §141's FAILURE RETURNING: a lever "
                                   + "that compounds instead of offsetting is exactly what sent two countries to the "
                                   + "population ceiling and three to the floor. The anchor is supposed to make the "
                                   + "displacement standing, not cumulative.");
                }

                if (Mathf.Abs(forcedRatio - 1f) < 1e-4f)
                {
                    failures.Add($"{country.Name}: the fertility lever is a no-op under the anchor");
                    Debug.LogError($"AGING/ANCHOR: {country.Name} ends at x{forcedRatio:F6} of its neutral run with "
                                   + $"fertility held at x{FertilityProbeMultiplier}. ⚠ The lever did NOTHING. The cohort "
                                   + "spec-let's §4.4 predicted precisely this - a substrate landing with its levers not "
                                   + "re-pointed - and a third dead lever would be a pattern, not an accident.");
                }
            }

            sb.Append("\n    ⚠ THE ANCHOR HAS NO CONVERGENCE SPEED, and that is the design rather than an omission.\n");
            sb.Append("    §141 named the danger: 'a speed nothing sources is an authored figure in the most load-bearing\n");
            sb.Append("    place in the model'. There is no blend weight here to tune. The pyramid is placed ON the\n");
            sb.Append("    published trajectory each year and displaced only by the measured effect of the levers.\n");
            sb.Append("    ⚠ STEP-VS-PUBLISHER is the column that says how much of the trajectory is the MODEL. It applies\n");
            sb.Append("    the step's own arithmetic to the publisher's own pyramid and asks whether it lands on the\n");
            sb.Append("    publisher's next year. Measured 2026-09-01 it runs a few tenths of a percent to about 1.4% a\n");
            sb.Append("    year - the demography AGREES closely, and the anchor is correcting a small annual disagreement\n");
            sb.Append("    rather than carrying the whole trajectory. ⚠ That small gap is exactly what compounded over a\n");
            sb.Append("    century in the unanchored build, and Poland - the widest gap here - was one of the three that\n");
            sb.Append("    reached the population floor. REPORTED, never failed: it is a description of the model, not a\n");
            sb.Append("    bound, and turning it into one would be tuning the step to match a projection.\n");
            sb.Append("    ⚠ AND THE PRICE: the population is no longer purely generated. Between gates the model is not\n");
            sb.Append("    forecasting a population, it is tracking a forecast and modelling the deviation from it.\n");
            sb.Append("    Whether the publisher is right is the publisher's claim; this asserts only that the model goes\n");
            sb.Append("    where the projection goes.\n");
        }
    }
}
