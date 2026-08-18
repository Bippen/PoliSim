using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// The authored scenario pool - the `EventSystem.EventPool` / `ForeignPolicySystem.MeetingPool`
    /// precedent, and hardcoded for the same reason: authored content as code costs nothing to add,
    /// gets compile-time checking, and needs no asset pipeline or serializer.
    ///
    /// <para><b>v1 carries ONE scenario</b> (R-S3f, the minimum playable slice). The ruled slate's
    /// other five - Italy debt, Poland convergence, The Disinflation, The Unequal Recovery, and Wage
    /// Boom Management (sequenced behind Step 5 by its own dependency) - are content work behind this
    /// format, and the format's claim is that each is a SUBSET of the shape below. If one needs a new
    /// <see cref="ObjectiveKind"/>, that is a finding about the grammar, to be recorded rather than
    /// special-cased.</para>
    /// </summary>
    public static class ScenarioLibrary
    {
        public static readonly IReadOnlyList<ScenarioDefinition> All = new List<ScenarioDefinition>
        {
            InheritTheFund()
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
    }
}
