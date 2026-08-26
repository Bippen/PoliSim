using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>The six Labor Market dials, in LawDefinition.DialDeltas' own labor-half order
    /// (indices 6-11 of that array - the C&amp;J six occupy 0-5).</summary>
    public enum LaborDial
    {
        MinimumWage,
        PaidFamilyLeave,
        OvertimeRegulation,
        RetrainingProgram,
        FamilyPolicy,
        ImmigrationPolicy
    }

    /// <summary>The downstream surface the six labor dials feed: seven tracked stats. No BudgetCost
    /// member - none of the labor dials touches a spending line at HEAD (a real coupling gap,
    /// deliberately visible in the pane rather than papered over; logged as a future couplings-pass
    /// input the same way the C&amp;J pane's gaps were before build-order item 2 consumed them).</summary>
    public enum LaborEffectStat
    {
        UnemploymentRate,
        PovertyRate,
        GiniCoefficient,
        LaborForceParticipation,
        ApprovalRating,
        BirthRate,
        NetMigrationRate
    }

    /// <summary>One dial-to-stat edge: the signed sensitivity AS APPLIED in the target formula
    /// (stat-target points per unit of dial gap - the dial's own unit: Kaitz points for MinimumWage,
    /// weeks for PaidFamilyLeave, 0-100 dial points for the rest; positive raises the stat).</summary>
    public readonly struct LaborCoupling
    {
        public readonly LaborDial Dial;
        public readonly LaborEffectStat Stat;
        public readonly float SignedSensitivity;
        /// <summary>True where the underlying research is honestly disputed (the minimum wage's
        /// employment effect - the Card/Krueger vs. neoclassical debate the constant's own CBO
        /// anchor calls "modest, debated"; overtime regulation's work-sharing claim - one side of
        /// the contested 35-hour-week literature, per its constant's own doc) - surfaced as
        /// "(contested)" wherever the edge is rendered, the same flag CrimeJusticeCouplings and
        /// PolicyWebRenderer's dial lines already carry.</summary>
        public readonly bool Contested;

        public LaborCoupling(LaborDial dial, LaborEffectStat stat, float signedSensitivity, bool contested = false)
        {
            Dial = dial;
            Stat = stat;
            SignedSensitivity = signedSensitivity;
            Contested = contested;
        }
    }

    /// <summary>
    /// THE SECOND DECLARED COUPLING TABLE (pass 3, the Labor Market law category, 2026-08-26):
    /// the ten dial-to-stat edges of the labor system, extracted from MacroSystem's scattered
    /// constants into ONE declared home that the Apply* formulas THEMSELVES read - the same
    /// architecture CrimeJusticeCouplings established (playtest-2 item 6), applied to the second
    /// category rather than reinvented. Any text derived from this table (the law detail pane's
    /// "Expected effects", PolicyWebRenderer's dial lines) cannot drift from what the simulation
    /// actually computes, because both quote these constants.
    ///
    /// ⚠ The constants moved here VERBATIM (names and values unchanged, doc comments carried) from
    /// MacroSystem - the extraction is a refactor under the trajectory byte-identity bar; a moved
    /// field means an edge changed value in transit. MacroSystem's formulas reference these
    /// qualified names directly; the <see cref="All"/> rows below cite the same consts, so table
    /// and simulation are one source by construction.
    ///
    /// THE GENERALITY FINDING (pass 3's §3 bar, reported not absorbed): CrimeJusticeCouplings'
    /// AggregateLawEffects could NOT render this category's edges - its stat enum, dial switch and
    /// edge table are all C&amp;J-typed, so a labor law fed to it yields an EMPTY list (silently -
    /// nothing breaks, nothing renders). The declared-table architecture is per-category, not
    /// category-generic: a new category brings its own table + its own DialDelta switch + its own
    /// AggregateLawEffects, and the pane dispatches on law.Category. That is one new table per
    /// category, not renderer work - the pane's rendering loop is shape-identical either way.
    ///
    /// UNIT DISCIPLINE: unlike the C&amp;J table (six uniform 0-100 dials, one shared neutral 50),
    /// the labor dials carry three different gap anchors, all preserved exactly as the formulas
    /// apply them: MinimumWage and PaidFamilyLeave measure gaps against the COUNTRY'S OWN seeded
    /// baseline (BaselineMinimumWagePercentOfMedian / BaselinePaidFamilyLeaveWeeks - the
    /// "gap versus a country-specific anchor" idiom), the other four against the shared neutral 50.
    /// A law's delta IS a gap change in the dial's own unit, so sensitivity-per-unit times delta is
    /// the long-run target shift either way - the same face-delta basis the C&amp;J pane uses.
    ///
    /// GATES AND CEILINGS the aggregate honors or names: the three MinimumWage edges are GATED on
    /// Country.MinimumWageImplemented (zero for Sweden/Italy - AggregateLawEffects takes the flag
    /// and omits those edges, so the pane never promises an effect the sim will not deliver
    /// there); the two LaborForceParticipation edges ride INSIDE ApplyLaborForceParticipationRate's
    /// combined ±1.0 ceiling (MaxLaborForceParticipationAdjustment) - the pane's shared "long-run
    /// target shifts ... before dial clamps" caveat covers this family of bounds.
    /// </summary>
    public static class LaborCouplings
    {
        /// <summary>
        /// Unemployment points added per point MinimumWagePercentOfMedian sits above the country's own
        /// BaselineMinimumWagePercentOfMedian (its real seeded starting level, not a universal
        /// constant - the same "gap versus a country-specific anchor" idiom ComfortableDebtToGdpPercent/
        /// BaselinePovertyRate already use, chosen so a fresh game opens at zero gap rather than an
        /// artificial turn-1 shock, and so this doesn't double-count against NAIRU, which already
        /// reflects each country's real structural conditions including its actual minimum wage).
        /// Small and directionally grounded (not precisely fitted) by the CBO's 2019 estimate that a
        /// federal $15/hr minimum wage (raising the effective Kaitz index roughly 20-30 points) would
        /// cost a median-estimate ~1.3 million jobs against a ~160 million labor force (~0.8%) - a
        /// modest, debated, real-world-scale effect, not the dominant driver of Unemployment the
        /// growth gap is. NOTE THE /100: the formula applies this per 100 Kaitz points of gap, so the
        /// per-point edge in <see cref="All"/> divides by 100.
        /// </summary>
        public const float MinimumWageEmploymentSensitivity = 1.5f;

        /// <summary>
        /// Poverty-baseline points reduced per point MinimumWagePercentOfMedian sits above the
        /// country's own BaselineMinimumWagePercentOfMedian (see MinimumWageEmploymentSensitivity
        /// for why the gap is versus a country-specific anchor, not a universal constant). Smaller
        /// than the welfare programs' own sensitivities - directionally grounded by the CBO's
        /// 2019 finding that a federal $15/hr minimum wage would lift roughly as many people out of
        /// poverty as it cost in jobs (~1.3 million each), a modest effect since a minimum wage only
        /// reaches low-wage workers, not the whole poor population the way a direct transfer does.
        /// Applied per 100 Kaitz points of gap, like the employment effect.
        /// </summary>
        public const float MinimumWagePovertyReductionSensitivity = 5f;

        /// <summary>Gini points removed per 100 points of MinimumWagePercentOfMedian above the
        /// country's own anchor - a wage floor compresses the bottom of the distribution; smaller
        /// than the transfer programs for the same only-reaches-low-wage-workers reason
        /// MinimumWagePovertyReductionSensitivity documents. (Was MacroSystem-private before the
        /// extraction; public here like every other table constant.)</summary>
        public const float GiniMinimumWageSensitivity = 2f;

        /// <summary>
        /// Unemployment points removed per point Country.OvertimeRegulationLevel sits above its
        /// neutral 50 (added per point below) - the "work-sharing" argument behind France's 35-hour
        /// week (stricter hour caps spread the same total work across more workers). A GENUINELY
        /// CONTESTED real economic claim, not a settled fact - some empirical studies find the
        /// 35-hour week didn't meaningfully reduce French unemployment as intended - so this is
        /// deliberately small, representing one side of that debate, not a confident modeling choice.
        /// </summary>
        public const float OvertimeUnemploymentSensitivity = 0.008f;

        /// <summary>Unemployment points removed per point Country.RetrainingProgramLevel sits above
        /// its neutral 50 (added per point below) - the well-established real economic rationale that
        /// retraining eases job transitions, smaller than the overtime effect since it's a more
        /// indirect mechanism.</summary>
        public const float RetrainingUnemploymentSensitivity = 0.006f;

        /// <summary>LaborForceParticipationRate points added per week Country.PaidFamilyLeaveWeeks
        /// sits above the country's own seeded BaselinePaidFamilyLeaveWeeks - a real, documented
        /// participation effect (parental leave keeps parents attached to the labor force). Rides
        /// INSIDE ApplyLaborForceParticipationRate's combined ±1.0 ceiling.</summary>
        public const float PaidFamilyLeaveParticipationSensitivity = 0.02f;

        /// <summary>LaborForceParticipationRate points added per point Country.RetrainingProgramLevel
        /// sits above its neutral 50 - retraining's SECOND channel, independent of its own
        /// Unemployment term (a real second-order reinforcement of one lever through two channels,
        /// audited and kept deliberately - see ApplyLaborForceParticipationRate's ceiling doc).
        /// Rides inside the same combined ±1.0 ceiling.</summary>
        public const float RetrainingParticipationSensitivity = 0.01f;

        /// <summary>Approval points gained per week Country.PaidFamilyLeaveWeeks sits above its own
        /// seeded BaselinePaidFamilyLeaveWeeks (and lost per week below) - a small, real political
        /// effect (paid-leave policy tends to be popular).</summary>
        public const float PaidFamilyLeaveApprovalSensitivity = 0.05f;

        /// <summary>
        /// Round 3 item 5, Part B: points BirthRate moves per point Country.FamilyPolicyLevel sits
        /// away from its neutral 50 - at the slider's full extremes (0 or 100) this is +/-1.5 points,
        /// deliberately SMALL: real-world evidence on pro-natalist policy's effect on fertility is
        /// itself small and contested (already flagged honestly in EconomyState.BirthRate's own doc
        /// comment when Part A was written). Feeds directly into the same BirthRate the secular-decline
        /// drift already updates - no separate channel, so this lever automatically flows through the
        /// same (YearsPerTurn-scaled, capped/reverting) ApplyPopulationGrowth pipeline every other
        /// BirthRate driver already uses.
        /// </summary>
        public const float FamilyPolicyBirthRateSensitivity = 0.03f;

        /// <summary>
        /// Round 3 item 5, Part B: points NetMigrationRate moves per point Country.ImmigrationPolicyLevel
        /// sits away from its neutral 50 - at the slider's full extremes (0 or 100) this is +/-5 points,
        /// deliberately WIDER than FamilyPolicyBirthRateSensitivity's swing: immigration policy is a
        /// genuinely more responsive real-world lever than fertility (visa/asylum/quota changes can move
        /// actual migration flows within a single term, unlike birth rates) - see EconomyState.
        /// NetMigrationRate's own doc comment from Part A, which anticipated exactly this. Feeds
        /// directly into the same NetMigrationRate the aging-drift term already updates - no
        /// separate channel, so this lever automatically flows through the same (YearsPerTurn-scaled)
        /// ApplyPopulationGrowth pipeline AND the existing NetMigrationRate-gap term in
        /// ApplyLaborForceParticipationRate's combined ceiling, rather than adding a second, parallel
        /// immigration-to-labor-force channel - avoiding the double-counting risk this item's own
        /// roadmap brief flagged structurally, not just by convention.
        /// </summary>
        public const float ImmigrationPolicyNetMigrationSensitivity = 0.1f;

        /// <summary>The ten edges, signed as the target formulas apply them, per unit of the dial's
        /// OWN unit (Kaitz points / weeks / dial points). Rows cite the consts above - the same
        /// values the simulation reads - never restated literals; the two /100f divisors are the
        /// formulas' own per-100-Kaitz-points application, quoted, not new numbers.</summary>
        public static readonly LaborCoupling[] All =
        {
            new LaborCoupling(LaborDial.MinimumWage, LaborEffectStat.UnemploymentRate, MinimumWageEmploymentSensitivity / 100f, contested: true),
            new LaborCoupling(LaborDial.MinimumWage, LaborEffectStat.PovertyRate, -MinimumWagePovertyReductionSensitivity / 100f),
            new LaborCoupling(LaborDial.MinimumWage, LaborEffectStat.GiniCoefficient, -GiniMinimumWageSensitivity / 100f),
            new LaborCoupling(LaborDial.PaidFamilyLeave, LaborEffectStat.LaborForceParticipation, PaidFamilyLeaveParticipationSensitivity),
            new LaborCoupling(LaborDial.PaidFamilyLeave, LaborEffectStat.ApprovalRating, PaidFamilyLeaveApprovalSensitivity),
            new LaborCoupling(LaborDial.OvertimeRegulation, LaborEffectStat.UnemploymentRate, -OvertimeUnemploymentSensitivity, contested: true),
            new LaborCoupling(LaborDial.RetrainingProgram, LaborEffectStat.UnemploymentRate, -RetrainingUnemploymentSensitivity),
            new LaborCoupling(LaborDial.RetrainingProgram, LaborEffectStat.LaborForceParticipation, RetrainingParticipationSensitivity),
            new LaborCoupling(LaborDial.FamilyPolicy, LaborEffectStat.BirthRate, FamilyPolicyBirthRateSensitivity),
            new LaborCoupling(LaborDial.ImmigrationPolicy, LaborEffectStat.NetMigrationRate, ImmigrationPolicyNetMigrationSensitivity),
        };

        /// <summary>Display vocabulary, shared with PolicyWebRenderer's dial lines so the two
        /// derived surfaces speak identically about the same edges.</summary>
        public static string DisplayName(LaborEffectStat stat)
        {
            switch (stat)
            {
                case LaborEffectStat.UnemploymentRate: return "Unemployment";
                case LaborEffectStat.PovertyRate: return "Poverty Rate";
                case LaborEffectStat.GiniCoefficient: return "Gini (inequality)";
                case LaborEffectStat.LaborForceParticipation: return "Labor Force Participation";
                case LaborEffectStat.ApprovalRating: return "Approval";
                case LaborEffectStat.BirthRate: return "Birth Rate";
                case LaborEffectStat.NetMigrationRate: return "Net Migration";
                default: return stat.ToString();
            }
        }

        public static string Unit(LaborEffectStat stat)
        {
            switch (stat)
            {
                case LaborEffectStat.BirthRate: return "per 1,000/yr";
                case LaborEffectStat.NetMigrationRate: return "per 1,000/yr";
                default: return "pts";
            }
        }

        /// <summary>A law's delta on one labor dial - the six labor LawDefinition fields, addressed
        /// by enum (the same switch shape as CrimeJusticeCouplings.DialDelta).</summary>
        public static float DialDelta(LawDefinition law, LaborDial dial)
        {
            switch (dial)
            {
                case LaborDial.MinimumWage: return law.MinimumWageDelta;
                case LaborDial.PaidFamilyLeave: return law.PaidFamilyLeaveWeeksDelta;
                case LaborDial.OvertimeRegulation: return law.OvertimeRegulationDelta;
                case LaborDial.RetrainingProgram: return law.RetrainingProgramDelta;
                case LaborDial.FamilyPolicy: return law.FamilyPolicyDelta;
                case LaborDial.ImmigrationPolicy: return law.ImmigrationPolicyDelta;
                default: return 0f;
            }
        }

        /// <summary>One rendered line of a law's derived effects (the labor-typed sibling of
        /// CrimeJusticeCouplings.LawEffectLine - separate struct because the stat enum is).</summary>
        public readonly struct LawEffectLine
        {
            public readonly LaborEffectStat Stat;
            public readonly float Amount;
            public readonly bool Contested;

            public LawEffectLine(LaborEffectStat stat, float amount, bool contested)
            {
                Stat = stat;
                Amount = amount;
                Contested = contested;
            }
        }

        /// <summary>
        /// The neutral derived "Expected effects" of one labor law (item 6's ruling, applied to the
        /// second category): per downstream stat, the long-run TARGET shift = Σ(dial delta × signed
        /// sensitivity) over the table's edges - direction and size from the model's own constants,
        /// no authored valence. Stats the law doesn't reach are omitted (zero rows are noise, not
        /// honesty - the ledger's own rule); a stat is flagged contested when any contributing edge
        /// is. Face deltas, before the dials' own clamps and the LFPR combined ceiling - the same
        /// basis as the delta rows above it.
        ///
        /// <paramref name="minimumWageImplemented"/> gates the three MinimumWage edges exactly as
        /// the simulation gates them (GetMinimumWageUnemploymentAdjustment and both distribution
        /// effects early-out for a country with no statutory minimum) - so for Sweden/Italy the pane
        /// never promises a wage-floor effect the sim will not deliver there.
        /// </summary>
        public static List<LawEffectLine> AggregateLawEffects(LawDefinition law, bool minimumWageImplemented)
        {
            var lines = new List<LawEffectLine>(5);
            foreach (LaborEffectStat stat in System.Enum.GetValues(typeof(LaborEffectStat)))
            {
                float total = 0f;
                bool contested = false;
                foreach (LaborCoupling edge in All)
                {
                    if (edge.Stat != stat)
                    {
                        continue;
                    }

                    if (edge.Dial == LaborDial.MinimumWage && !minimumWageImplemented)
                    {
                        continue;
                    }

                    float delta = DialDelta(law, edge.Dial);
                    if (delta == 0f)
                    {
                        continue;
                    }

                    total += delta * edge.SignedSensitivity;
                    contested |= edge.Contested;
                }

                if (total != 0f)
                {
                    lines.Add(new LawEffectLine(stat, total, contested));
                }
            }

            return lines;
        }
    }
}
