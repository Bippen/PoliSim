using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// The authored scenario pool - the `EventSystem.EventPool` / `ForeignPolicySystem.MeetingPool`
    /// precedent, and hardcoded for the same reason: authored content as code costs nothing to add,
    /// gets compile-time checking, and needs no asset pipeline or serializer.
    ///
    /// <para><b>v1 shipped ONE scenario</b> (R-S3f, "Inherit the Fund" - the minimum playable
    /// slice). **Two of the ruled slate's other four then DROPPED on measurement** - Wage Boom
    /// Management and The Disinflation both foreclosed by `UnemploymentReversionSpeed`, one finding
    /// from two directions (see their own reports). **"Italy Debt Crisis" is the second scenario to
    /// ship**, the first content pass to survive measurement on the first try - its difficulty
    /// source (the debt identity) is untouched by either drop. **Poland convergence and The Unequal
    /// Recovery were MEASURED and DROPPED on 2026-08-28 (R-K2 of the omnibus;
    /// `ScenarioCandidateMeasurementDiagnostic`, seed 777, 30 turns each).** Poland fell to the
    /// §22 root cause a third time: real wages track PotentialGrowthRate one-for-one, so the only
    /// thing a player can add is the tightness term, and `UnemploymentReversionSpeed` (0.7/turn)
    /// closes it - wage growth ≥ trend + 0.5 pp held for 0 turns on the no-policy and stimulus lines
    /// and 2 turns under an overheat line (+40% discretionary two years running, the policy rate to
    /// 0), while inflation never left [1, 4] on any line (max 2.77), so the fail condition is
    /// unreachable and "≥ 2%" is met 28 of 30 turns with no policy at all. The Unequal Recovery
    /// fell to a DIFFERENT root cause, in the political model: the transfer programs are the only
    /// levers strong enough (3–4 Gini points each at full generosity, against 0.08/pt for income
    /// tax and 0.02/pt for the wage floor), every one of them is an expansionary bill, and under
    /// `PartyArchetypeData` the Progressive and Conservative seat targets are identical at every
    /// approval level (equal base shares, equal sensitivities), so the expected expansionary
    /// alignment is −0.0015 × Nationalist seats - negative everywhere (−0.036 at seed, −0.006 at
    /// approval 100, −0.09 at approval 30) - and an expansionary bill passes only by ±1-seat jitter
    /// (once, at t14, on a line whose approval had already saturated at 100). Failable (no-policy
    /// Gini 45.47 against 39.5), mechanically winnable only on the harness's un-voted path (38.65,
    /// with debt at 233% of GDP), impossible in play. Return trigger: item 10's party re-seeding,
    /// which is what makes an expansionary bill passable. The format's claim continues to hold:
    /// each shipped scenario has been a SUBSET of the shape below, no new <see cref="ObjectiveKind"/>
    /// needed yet. If one ever does, that is a finding about the grammar, to be recorded rather
    /// than special-cased.</para>
    /// </summary>
    public static class ScenarioLibrary
    {
        public static readonly IReadOnlyList<ScenarioDefinition> All = new List<ScenarioDefinition>
        {
            InheritTheFund(),
            ItalyDebtCrisis()
        };

        public static ScenarioDefinition ById(string id)
        {
            foreach (ScenarioDefinition definition in All)
            {
                if (definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>
        /// "INHERIT THE FUND" - and it is also **R3's creditor branch getting its first live
        /// exercise**, which is why this one is the slice.
        ///
        /// <para><b>The coverage gap, stated:</b> the erosion term is SYMMETRIC by ruling R3 - a net
        /// creditor's real claim erodes at π exactly as a debtor's real burden does, "no free money in
        /// either direction". That branch has been code-verified since 2026-08-17 and NEVER RUN: the
        /// recorded finding is that no scenario at HEAD creates a net creditor, so a 500-turn
        /// swfstress run produced zero negative ratios. This scenario starts Sweden at −50% of GDP,
        /// so the branch executes from day one and the scenario doubles as the gap's closure.</para>
        ///
        /// <para><b>Why the model makes it hard - MEASURED, and the measurement corrected the
        /// premise.</b> The first headless run (2026-08-18, seed 777, no-policy) decomposed the debt
        /// stock's move per period into its two arms and found the ORDER OF THE ANTAGONISTS IS THE
        /// REVERSE of what this scenario was drafted assuming:
        /// 1. <b>The structural deficit dominates</b> - about +42 per turn against Sweden's ~620 GDP,
        ///    which exhausts a −310 inheritance by turn 8 with no policy at all.
        /// 2. <b>Erosion is real but second-order</b> - roughly +6 per turn on the opening stock
        ///    (π ≈ 2% against the negative position), about a seventh of the move. It is a genuine
        ///    drag that never stops, not the thing that kills the fund.
        /// 3. <b>A net creditor earns NOTHING</b> on its position (the 2026-08-02 ruling, deliberately
        ///    the conservative half), so the claim cannot pay for itself while the deficit runs.
        /// 4. Sweden opens with <b>unemployment 8.0 against a 6.5 NAIRU</b>, pushing the poverty
        ///    baseline up - so the poverty objective needs real spending, and spending is exactly what
        ///    stops you staying a creditor.
        /// **The real dilemma is therefore fiscal, not monetary**: the player must close a
        /// ~6.6%-of-GDP structural gap to keep the inheritance, while spending enough of it to move
        /// poverty, with approval as the constraint that forbids doing either to an extreme. The
        /// no-policy run LOSES (1 of 3 objectives), which is the shape a challenge should have -
        /// whether a skilled line wins is a playtest question, not a claim made here.</para>
        /// </summary>
        private static ScenarioDefinition InheritTheFund()
        {
            return new ScenarioDefinition
            {
                Id = "inherit_the_fund",
                Name = "Inherit the Fund",
                Premise =
                    "Sweden hands you a sovereign fund and a treasury that owes nothing - the state is a net " +
                    "creditor, holding claims worth half a year of national output. It earns you nothing while " +
                    "it sits, inflation quietly shaves it every year, and the budget it is propping up does not " +
                    "balance: left alone, the inheritance is gone in eight years. Unemployment is above its " +
                    "structural rate and poverty is drifting up with it. Spend it well enough to matter, " +
                    "without spending it away.",
                Country = CountryId.Sweden,
                EndTurn = 12,

                // R-S3e: 1.0 - this scenario takes the standing pacing. The three-rate playtest varies
                // THIS field, not the global constant.
                ForeignPolicyCadenceMultiplier = 1f,

                ApplyDeltas = (world, country) =>
                {
                    EconomyState state = country.State;

                    // The inheritance: a net creditor position at −50% of GDP. Written directly on the
                    // stock because that IS the starting state - the erosion term then runs its
                    // symmetric arm against a negative stock every day of play.
                    state.GovernmentDebt = -0.5f * state.GDP;

                    // The fund itself, at 60% of GDP, in a conservative real-world split. Contribution
                    // rate 0: the player decides whether to keep feeding it, which is half the dilemma.
                    country.SovereignWealthFund = new SovereignWealthFund
                    {
                        TotalAssets = 0.6f * state.GDP,
                        ContributionRatePercent = 0f,
                        EquitiesWeight = 40f,
                        BondsWeight = 40f,
                        InfrastructureWeight = 10f,
                        RealEstateWeight = 10f
                    };
                },

                Objectives = new List<ScenarioObjective>
                {
                    new ScenarioObjective
                    {
                        Id = "still_creditor",
                        Description = "Finish still a net creditor (government debt at or below 0)",
                        Kind = ObjectiveKind.Terminal,
                        Comparison = ObjectiveComparison.AtMost,
                        Target = 0f,
                        Unit = "B",
                        Read = c => c.State.GovernmentDebt
                    },
                    new ScenarioObjective
                    {
                        Id = "poverty_down",
                        Description = "Bring poverty to 8.0% or below",
                        Kind = ObjectiveKind.Terminal,
                        Comparison = ObjectiveComparison.AtMost,
                        Target = 8f,
                        Unit = "%",
                        Read = c => c.State.PovertyRate
                    },
                    new ScenarioObjective
                    {
                        Id = "hold_the_room",
                        Description = "Never let approval fall below 30",
                        Kind = ObjectiveKind.NeverBreach,
                        Comparison = ObjectiveComparison.AtLeast,
                        Target = 30f,
                        Unit = "",
                        Read = c => c.State.ApprovalRating
                    }
                }
            };
        }

        /// <summary>
        /// "ITALY DEBT CRISIS" - the third content pass, and the first to survive measurement on
        /// the first try (Wage Boom Management and The Disinflation both DROPPED, for the identical
        /// reason from opposite directions - see their own reports - the finding named there being
        /// that `UnemploymentReversionSpeed` forecloses any premise built on moving the unemployment
        /// gap away from NAIRU and holding it there). This scenario's difficulty source is a
        /// different mechanism entirely: the debt identity, post-erosion/maturity - the
        /// best-validated part of the model, not touched by either drop.
        ///
        /// <para><b>Measured, not assumed</b> (the Italy debt measurement report, consumed to
        /// COMPLETED.md §22, 2026-08-26). Starting
        /// 27 points above `ComfortableDebtToGdpPercent` (138), the Fiscal Reaction Function's own
        /// aggressive first-turn response (a real, calibrated stabilizer - "confirmed flat from turn
        /// 500 through turn 2000" per its own doc comment) pulls the ratio down sharply in turn 1
        /// REGARDLESS of policy, then the no-policy path settles into the 105-110% band and drifts
        /// slowly back up (109.6% at turn 30). **The lever bites, dramatically and monotonically**:
        /// at turn 30, a −10% discretionary cut barely beats doing nothing (108.0%), a −20% cut
        /// reaches 89.8%, a −30% cut reaches 52.6% - three clearly separated outcomes from one
        /// lever at three magnitudes, the opposite of both drops' "every lever gives the identical
        /// result". A VAT hike bites too, more modestly and asymmetrically (+3pp → 108.3%,
        /// +6pp → 104.2%) - taxation buys less relief per point than spending, and costs more
        /// approval per point doing it (the standing `TaxHikeApprovalSensitivity` term, undampened
        /// by debt level, unlike a spending INCREASE's approval bonus, which the deficit-awareness
        /// factor floors to zero above ~160% debt - a spending CUT's penalty is equally undampened,
        /// so consolidation costs what it costs on the approval side either way it is done).</para>
        ///
        /// <para><b>The approval-survival question, killed independently for Disinflation, answered
        /// here by the same direct measurement</b>: debt is NOT in `ApplyApprovalRating`'s misery
        /// term (confirmed by reading the formula before running anything - only
        /// unemployment/inflation/crime/corruption gaps are). Across every tested configuration,
        /// including the harshest (a 6-point VAT hike), turn-30 approval never dropped below 39.9 -
        /// comfortably clear of `ElectionSystem.LosingThreshold` (35). **A player has real time to
        /// govern here**, unlike the inflation-driven collapse that killed Disinflation on its own
        /// terms before any lever could even be judged.</para>
        ///
        /// <para><b>Fiscal-only by construction, and that is a stated FEATURE, not a limitation -
        /// the Eurozone question, ruled by measurement in the prior pass.</b> Italy shares a
        /// currency zone; `EurozoneRateSystem`'s auto-following blend already made a large,
        /// automatic monetary move for Germany in the Disinflation measurement (2.25%→8.6% with
        /// ZERO player input) and it still could not do the job needed there. A finance minister
        /// with no central bank of their own is a real constraint several actual Eurozone
        /// treasuries govern under - this scenario names it in the premise rather than working
        /// around it: every lever here is fiscal, and none of them needs to be, because the fiscal
        /// levers alone already measure as decisive.</para>
        ///
        /// <para><b>RE-PREMISED 2026-08-26 (the seed recalibration, build-order item 1 - terminal
        /// ruling: "stabilize ≤145 by t30").</b> The honest calibration dissolved the old
        /// consolidation-triumph premise: with real revenue targets and the mandatory transfer
        /// block seeded, the FRF and erosion bring 165% down to ~146-148 ON THEIR OWN, and the
        /// measured lever spread narrowed from 57 points to ~5 (a -20% discretionary cut buys
        /// 2.5 points where it bought 17, because discretionary G is now a small slice of an
        /// honest spending base). EndTurn 20→30, target 95→145, and the scenario's claim is now
        /// stabilization against a 5.4%-interest stock - the thing Italy actually does. The
        /// per-objective comments carry the re-measurement; the pre-recalibration figures in the
        /// paragraphs above are the ORIGINAL era's record, kept as history.</para>
        /// </summary>
        private static ScenarioDefinition ItalyDebtCrisis()
        {
            return new ScenarioDefinition
            {
                Id = "italy_debt_crisis",
                Name = "Italy Debt Crisis",
                Premise =
                    "The debt-to-GDP ratio has climbed to 165% - deep past what markets consider comfortable, " +
                    "and every euro of new borrowing prices in the strain. Italy has no central bank of its own " +
                    "to lean on: the Eurozone sets one rate for three economies, and Rome's voice in that room " +
                    "is a modest, capped push, not a lever. Every tool you actually have is fiscal - spending " +
                    "and taxation - and every one of them costs something with voters. Bring the ratio down " +
                    "without losing the room.",
                Country = CountryId.Italy,
                EndTurn = 30,

                ForeignPolicyCadenceMultiplier = 1f,

                ApplyDeltas = (world, country) =>
                {
                    // The crisis: 165% of GDP, 27 points past ComfortableDebtToGdpPercent (138) -
                    // written directly on the stock, the same idiom Inherit the Fund uses for its
                    // own starting position. Nothing else is touched: NAIRU, inflation, the tax
                    // portfolio and the spending lines all stay at Italy's real seeded values, so
                    // the crisis is the debt level alone, not a bundle of unrelated bad luck.
                    country.State.GovernmentDebt = 1.65f * country.State.GDP;
                },

                Objectives = new List<ScenarioObjective>
                {
                    // RE-MEASURED AND RE-PREMISED under the honest calibration (build-order item 1,
                    // terminal ruling 2026-08-26). The original ≤95-by-t20 target was authored
                    // against the suppressed-revenue era, where a -20% discretionary cut bought 17
                    // points; under the recalibrated seeds the FRF and the erosion term do the
                    // heavy lifting themselves (no-policy: 165% -> 148.3% by t30) and the player's
                    // levers buy the LAST points, not fifty - so the scenario becomes what real
                    // Italy is: STABILIZATION, not a consolidation triumph. The 2026-08-26
                    // re-measurement (recal_italymeasure.log, seed 777, t30): no-policy 148.3
                    // FAILS by 3.3; cut20+VAT25 143.1 WINS with margin 1.9; VAT28 143.2 wins the
                    // debt objective but dips approval to 39.9 and breaks keep_the_room's streak
                    // (the instrument tension, sharpened); cut30 alone 144.4 wins thin (0.6).
                    new ScenarioObjective
                    {
                        Id = "debt_down",
                        Description = "Stabilize debt-to-GDP at 145% or below",
                        Kind = ObjectiveKind.Terminal,
                        Comparison = ObjectiveComparison.AtMost,
                        Target = 145f,
                        Unit = "%",
                        Read = c => c.State.DebtToGdpRatio
                    },
                    // The Sustained form's first REAL exercise (Step 2/3's `SustainedObjectiveDiagnostic`
                    // proved the FORM works on a synthetic condition; this is content actually using
                    // it). MEASURED: every tested configuration held at least the mid-40s for most of
                    // the run, but the harshest tax-led package dipped to the low 40s early - a real,
                    // measured tension between hitting the debt target fast and keeping the streak
                    // alive, not a number chosen to feel hard.
                    new ScenarioObjective
                    {
                        Id = "keep_the_room",
                        Description = "Hold approval at 40 or above for 10 consecutive turns",
                        Kind = ObjectiveKind.Sustained,
                        Comparison = ObjectiveComparison.AtLeast,
                        Target = 40f,
                        RequiredTurns = 10,
                        Unit = "",
                        Read = c => c.State.ApprovalRating
                    },
                    // The hard floor - MEASURED to never bind in any tested configuration (worst
                    // observed at the 2026-08-26 re-measurement: 39.9 at t30, the VAT28 package -
                    // which breaks keep_the_room's streak yet stays 9.9 points clear of this
                    // floor), so it is genuinely a SAFETY NET against a bad line, not a second
                    // copy of keep_the_room's own threshold.
                    new ScenarioObjective
                    {
                        Id = "no_collapse",
                        Description = "Never let approval fall below 30",
                        Kind = ObjectiveKind.NeverBreach,
                        Comparison = ObjectiveComparison.AtLeast,
                        Target = 30f,
                        Unit = "",
                        Read = c => c.State.ApprovalRating
                    }
                }
            };
        }
    }
}
