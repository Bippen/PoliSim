using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>Every policy lever/tax type/spending category/welfare program this widget can show as a source node.</summary>
    public enum PolicyNodeId
    {
        MinimumWage, PaidFamilyLeave, OvertimeRegulation, RetrainingProgram, FamilyPolicy, ImmigrationPolicy,
        PoliceFunding, SentencingSeverity, BailReform, DrugPolicy, JudicialFunding, BorderEnforcement,
        IncomeTax, CorporateTax, VAT, PayrollTax, CapitalGainsTax, SalesTax, ExciseTax, PropertyTax, EstateTax, WealthTax, CarbonTax, Tariffs, StampDuty,
        SocialSecurity, Medicare, Medicaid, IncomeSecurity, VeteransBenefitsMandatory, FederalRetirement,
        Defense, Transportation, HHSDiscretionary, Education, Justice, HomelandSecurity, Energy, Housing, OtherDiscretionarySpending,
        UBI, NegativeIncomeTax, MeansTestedWelfare, UniversalHealthcare, HousingAssistance, ChildcareSubsidies,
        SectorSubsidy, SectorRegulation, SectorTaxCredit, SectorResearchGrants, SectorDeregulationNationalization,
        SwfContributionRate, SwfAssetAllocation,
        TariffPolicy,
        InterestRateDecision
    }

    /// <summary>Every stat this widget can show as a target node - a subset of EconomyState/Country fields, not every tracked value (see PolicyWebRenderer's own class comment for scoping).</summary>
    public enum StatNodeId
    {
        Gdp, Unemployment, Inflation, Approval, DebtToGdp, Poverty, InterestRate, TradeBalance,
        Lfpr, Crime, PrisonPopulation, OrganizedCrime, Corruption,
        PotentialGrowth, PopulationGrowthRate, DependencyRatio, ConsumerConfidence, BusinessConfidence,
        // §E4, promoted by R-CL4 at C-F1 (2026-08-31): R4-1's two Society rows join the web. APPENDED,
        // never inserted - the enum is persisted nowhere but it is read positionally by films and
        // censuses, and this project's stream enum carries the same rule for the same reason. ⚠ Neither
        // has a delivered icon, and that is the POINT: they are the Design ask's own evidence, and
        // `StatIconCoverageCheck` now reports a missing icon as a GAP rather than failing on it.
        YouthUnemployment, LifeExpectancy
    }

    /// <summary>One real effect relationship: <see cref="Source"/> policy node to <see cref="Target"/> stat node. <see cref="Increases"/> is the SIGN of the real formula (does raising the policy dial/rate raise or lower the target) - combined with the target's own HigherIsBetter framing (see StatNodeInfo) to pick green/red, the same judgment GraphRenderer's own title-row delta already makes elsewhere in this UI. <see cref="RelativeStrength"/> is 0-1 and ONLY meaningfully comparable against other edges from the SAME small documented group (e.g. the six welfare programs' own poverty-reduction sensitivities) - 1f (uniform) everywhere a real cross-comparable ratio isn't available, per this task's own "uniform otherwise" instruction.</summary>
    /// <summary>
    /// The honesty class of an edge (omnibus 2026-08-28, R-K1 — roadmap item 1, the causal graph
    /// on the web itself). DERIVED: the edge's effect is a recorded ledger term — the approval
    /// ledger's, the debt ledger's or the confidence book's — so the line on screen is the same
    /// arithmetic the boundary audit proves every period; it cannot drift from the model. DECLARED:
    /// the edge is asserted from a MacroSystem/SimulationManager formula (a coupling table, a
    /// sensitivity) with no ledger term behind it yet — real, sourced, but not audited on screen.
    /// The two draw differently (solid vs dashed at reduced ink) and are tagged in the detail panels.
    /// </summary>
    public enum EdgeProvenance { Declared, Derived }

    public readonly struct PolicyWebEdge
    {
        public readonly PolicyNodeId Source;
        public readonly StatNodeId Target;
        public readonly bool Increases;
        public readonly float RelativeStrength;
        public readonly EdgeProvenance Provenance;
        /// <summary>The ledger term this edge derives from (its field name on ApprovalAttribution /
        /// DebtAttribution, or the confidence book's row), null for a declared edge.</summary>
        public readonly string LedgerTerm;

        public PolicyWebEdge(PolicyNodeId source, StatNodeId target, bool increases, float relativeStrength = 1f)
            : this(source, target, increases, relativeStrength, EdgeProvenance.Declared, null)
        {
        }

        public PolicyWebEdge(PolicyNodeId source, StatNodeId target, bool increases, float relativeStrength, EdgeProvenance provenance, string ledgerTerm)
        {
            Source = source;
            Target = target;
            Increases = increases;
            RelativeStrength = relativeStrength;
            Provenance = provenance;
            LedgerTerm = ledgerTerm;
        }
    }

    /// <summary>
    /// A stat → stat edge — the causal graph's own kind. Every one is DERIVED from a ledger
    /// term whose input is another stat: the approval formula's growth and misery terms, the debt
    /// identity's erosion and interest terms. Drawn as chords inside the Stats wedge when either end
    /// is the active node; listed on the stat panel as "moved by" and "feeds".
    /// </summary>
    public readonly struct StatWebEdge
    {
        public readonly StatNodeId Source;
        public readonly StatNodeId Target;
        public readonly bool Increases;
        public readonly float RelativeStrength;
        public readonly string LedgerTerm;

        public StatWebEdge(StatNodeId source, StatNodeId target, bool increases, float relativeStrength, string ledgerTerm)
        {
            Source = source;
            Target = target;
            Increases = increases;
            RelativeStrength = relativeStrength;
            LedgerTerm = ledgerTerm;
        }
    }

    /// <summary>
    /// Policy Web tab: a node/connecting-line diagram of which policy levers affect which stats,
    /// reusing MapRenderer's own node+line rendering TECHNIQUE (hand-drawn Texture2D circles/lines,
    /// click-to-pin detail panel) rather than its fixed six-country layout, which doesn't generalize
    /// to ~55 policy nodes. Laid out as a circular chord diagram, sized to fill the large majority of
    /// its tab's own panel space (see GetLabelMargin/Draw - a Screen-fraction-driven rect from the
    /// caller, not a fixed pixel canvas) - every node sits on the circumference, grouped into wedges
    /// (one per policy SystemArea plus one for all stat nodes) sized proportional to how many nodes
    /// they hold but floored at a minimum angular width so a 1-2-node area isn't squeezed unreadably
    /// thin, with a visible divider between adjacent wedges so the grouping reads at a glance. Node
    /// size scales with DEGREE (edge count) between a clamped min/max so low-connection levers stay
    /// visible without letting hub nodes dominate. At rest, only the ~9 wedge headers are always
    /// visible - individual node labels (all ~73 of them) are not, since no circle size keeps that
    /// many simultaneous labels legible; hovering or pinning a node reveals its own label AND draws
    /// only its own connections as straight chords across the circle's interior, both anchored to the
    /// node itself - the same declutter model as before, just redrawn on a ring instead of two columns,
    /// and now extended to node labels as well as edges. Every edge below is sourced from a real MacroSystem/SimulationManager
    /// formula (see each edge's own inline comment) - none invented for this view, per the task's own
    /// explicit instruction. Two honest simplifications, both noted where they occur: (1) each of the
    /// four Sector policy dials is ONE node applying uniformly across all eight Sectors (not 32
    /// separate per-sector nodes) - the same "one conceptual dial" framing already used for
    /// Country.PoliceFundingLevel etc.; (2) the ~15 SpendingCategory values with no differentiated
    /// effect beyond the generic Budget/DebtToGdp channel are consolidated into one
    /// OtherDiscretionarySpending node rather than drawn 15 times with an identical single edge.
    /// </summary>
    public class PolicyWebRenderer
    {
        private const float MinNodeDiameter = 7f;
        private const float MaxNodeDiameter = 22f;
        private const float WedgeGapDegrees = 3f;
        private const float LabelPad = 5f;

        /// <summary>Angular floor per wedge, independent of node count - without it a 1-node wedge (Political, Trade) or 2-node wedge (Sovereign Wealth) gets squeezed to a sliver a few degrees wide under pure proportional sizing, crowding its divider lines and header label against its neighbors'. Any degrees this floor adds are taken from the proportional split of the remaining (non-floored) wedges - see Draw's own allocation loop - so the circle still sums to exactly 360 degrees.</summary>
        private const float MinWedgeSpanDegrees = 16f;

        private const float MinLineThickness = 1.1f;
        private const float MaxLineThickness = 3.4f;

        /// <summary>
        /// The plate a procedural chart is drawn ON - paper, not the dark-dashboard near-black this was
        /// until 2026-08-10.
        ///
        /// ⚠ **Three renderers carried this identical value and all three were missed**, because a chart
        /// with no data yet draws no plate: at turn 0 the graphs say "No data yet" and the map is empty,
        /// so every v2.0 capture to date showed paper where real play would show black. PolicyWeb was
        /// only found first because its ring renders immediately.
        ///
        /// Rule 10 draws the line exactly here: the plate and frame AROUND a procedural chart are the
        /// v2.0 pack's business, the marks inside are not. Node inks, edge good/bad and area accents all
        /// stay exactly as they were - they are already on the aged palette.
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        /// <summary>A stat node keys no area, so it is deliberately neutral - but light grey was neutral against BLACK, and on paper it is invisible. Neutral ink is the same intent re-expressed for the ground it now sits on.</summary>
        private static readonly Color StatNodeColor = PoliSimTheme.Accent(UiPalette.SystemArea.Neutral);
        // Furniture, not data - a wedge divider is a rule, and the palette has a rule colour.
        private static readonly Color WedgeDividerColor = PoliSimTheme.Hairline;

        private Texture2D _backgroundTexture;
        private Texture2D _circleTexture;
        private Texture2D _lineTexture;

        private struct PolicyNodeInfo
        {
            public string Name;
            public UiPalette.SystemArea Area;
            public string Description;
        }

        private struct StatNodeInfo
        {
            public string Name;
            public bool? HigherIsBetter;

            /// <summary>
            /// The currency unit this stat is stored in, or null if it is not money at all (a rate, a
            /// percentage, an index). Lives here rather than at the display sites because the P2 unit
            /// bug survived two correct site-specific fixes: a stat that carries its own unit lets a
            /// graph axis ASK what it is drawing instead of assuming, and makes the next display site
            /// hard to get wrong rather than merely discouraged from it.
            /// </summary>
            public MoneyUnit? Unit;
        }

        private static readonly Dictionary<PolicyNodeId, PolicyNodeInfo> PolicyInfo = new Dictionary<PolicyNodeId, PolicyNodeInfo>
        {
            { PolicyNodeId.MinimumWage, new PolicyNodeInfo { Name = "Minimum Wage", Area = UiPalette.SystemArea.Labor, Description = "Statutory minimum as % of median wage (USA only - Sweden/Italy have no statutory minimum)." } },
            { PolicyNodeId.PaidFamilyLeave, new PolicyNodeInfo { Name = "Paid Family Leave", Area = UiPalette.SystemArea.Labor, Description = "Weeks of guaranteed paid family leave." } },
            { PolicyNodeId.OvertimeRegulation, new PolicyNodeInfo { Name = "Overtime Regulation", Area = UiPalette.SystemArea.Labor, Description = "Working-hour/overtime cap strictness, 0 (unregulated) to 100 (strict caps)." } },
            { PolicyNodeId.RetrainingProgram, new PolicyNodeInfo { Name = "Workforce Retraining", Area = UiPalette.SystemArea.Labor, Description = "Government-funded retraining program intensity, 0-100." } },
            { PolicyNodeId.FamilyPolicy, new PolicyNodeInfo { Name = "Family Policy", Area = UiPalette.SystemArea.Labor, Description = "Family/childcare policy support intensity, 0 (minimal) to 100 (maximal pro-natalist support)." } },
            { PolicyNodeId.ImmigrationPolicy, new PolicyNodeInfo { Name = "Immigration Policy", Area = UiPalette.SystemArea.Labor, Description = "Immigration policy openness, 0 (restrictive) to 100 (open)." } },
            { PolicyNodeId.PoliceFunding, new PolicyNodeInfo { Name = "Police Funding", Area = UiPalette.SystemArea.CrimeJustice, Description = "Police funding level, 0-100." } },
            { PolicyNodeId.SentencingSeverity, new PolicyNodeInfo { Name = "Sentencing Severity", Area = UiPalette.SystemArea.CrimeJustice, Description = "Sentencing severity, 0 (lenient) to 100 (harsh)." } },
            { PolicyNodeId.BailReform, new PolicyNodeInfo { Name = "Bail Reform", Area = UiPalette.SystemArea.CrimeJustice, Description = "Bail reform level, 0 (traditional cash bail) to 100 (full reform)." } },
            { PolicyNodeId.DrugPolicy, new PolicyNodeInfo { Name = "Drug Policy", Area = UiPalette.SystemArea.CrimeJustice, Description = "Drug policy, 0 (decriminalized) to 100 (strict criminalization)." } },
            { PolicyNodeId.JudicialFunding, new PolicyNodeInfo { Name = "Judicial Funding", Area = UiPalette.SystemArea.CrimeJustice, Description = "Courts/prosecution funding level, 0-100." } },
            { PolicyNodeId.BorderEnforcement, new PolicyNodeInfo { Name = "Border Enforcement", Area = UiPalette.SystemArea.CrimeJustice, Description = "Border enforcement strictness, 0 (open) to 100 (strict)." } },
            { PolicyNodeId.IncomeTax, new PolicyNodeInfo { Name = "Income Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Income tax rate." } },
            { PolicyNodeId.CorporateTax, new PolicyNodeInfo { Name = "Corporate Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Corporate tax rate." } },
            { PolicyNodeId.VAT, new PolicyNodeInfo { Name = "VAT", Area = UiPalette.SystemArea.Fiscal, Description = "Value-added tax rate." } },
            { PolicyNodeId.PayrollTax, new PolicyNodeInfo { Name = "Payroll Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Payroll tax rate." } },
            { PolicyNodeId.CapitalGainsTax, new PolicyNodeInfo { Name = "Capital Gains Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Capital gains tax rate." } },
            { PolicyNodeId.SalesTax, new PolicyNodeInfo { Name = "Sales Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Sales tax rate." } },
            { PolicyNodeId.ExciseTax, new PolicyNodeInfo { Name = "Excise Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Excise tax rate." } },
            { PolicyNodeId.PropertyTax, new PolicyNodeInfo { Name = "Property Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Property tax rate." } },
            { PolicyNodeId.EstateTax, new PolicyNodeInfo { Name = "Estate Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Estate tax rate." } },
            { PolicyNodeId.WealthTax, new PolicyNodeInfo { Name = "Wealth Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Wealth tax rate." } },
            { PolicyNodeId.CarbonTax, new PolicyNodeInfo { Name = "Carbon Tax", Area = UiPalette.SystemArea.Fiscal, Description = "Carbon tax rate." } },
            { PolicyNodeId.Tariffs, new PolicyNodeInfo { Name = "Tariffs (Tax Line)", Area = UiPalette.SystemArea.Fiscal, Description = "An enum member only: TaxType.Tariffs never gets a TaxLine (GetTotalTaxRevenue skips it, its base share is 0), so no dial reaches this node and it draws no edge (the edge sweep of 2026-08-27 removed two phantom ones). The real tariff lever is the Tariff Policy node." } },
            { PolicyNodeId.StampDuty, new PolicyNodeInfo { Name = "Stamp Duty", Area = UiPalette.SystemArea.Fiscal, Description = "Stamp duty rate." } },
            { PolicyNodeId.SocialSecurity, new PolicyNodeInfo { Name = "Social Security", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.Medicare, new PolicyNodeInfo { Name = "Medicare", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.Medicaid, new PolicyNodeInfo { Name = "Medicaid", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.IncomeSecurity, new PolicyNodeInfo { Name = "Income Security", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.VeteransBenefitsMandatory, new PolicyNodeInfo { Name = "Veterans Benefits (Mand.)", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.FederalRetirement, new PolicyNodeInfo { Name = "Federal Retirement", Area = UiPalette.SystemArea.Fiscal, Description = "Mandatory spending line." } },
            { PolicyNodeId.Defense, new PolicyNodeInfo { Name = "Defense", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line." } },
            { PolicyNodeId.Transportation, new PolicyNodeInfo { Name = "Transportation (Infra.)", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line - this project's Infrastructure spending category. A non-USA portfolio's Infrastructure & Development line folds onto this node: same effect, the country's own line." } },
            { PolicyNodeId.HHSDiscretionary, new PolicyNodeInfo { Name = "HHS (Healthcare)", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line - this project's Healthcare spending category. A non-USA portfolio's Healthcare & Social Care line folds onto this node: same effect, the country's own line." } },
            { PolicyNodeId.Education, new PolicyNodeInfo { Name = "Education", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line." } },
            { PolicyNodeId.Justice, new PolicyNodeInfo { Name = "Justice (Spending)", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line - distinct from the Crime & Justice policy dials." } },
            { PolicyNodeId.HomelandSecurity, new PolicyNodeInfo { Name = "Homeland Security", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line." } },
            { PolicyNodeId.Energy, new PolicyNodeInfo { Name = "Energy (Spending)", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line." } },
            { PolicyNodeId.Housing, new PolicyNodeInfo { Name = "Housing (Spending)", Area = UiPalette.SystemArea.Fiscal, Description = "Discretionary spending line - distinct from the HousingAssistance welfare program." } },
            { PolicyNodeId.OtherDiscretionarySpending, new PolicyNodeInfo { Name = "Other Discretionary", Area = UiPalette.SystemArea.Fiscal, Description = "Consolidated: Veterans Affairs (Disc.), State/Foreign Affairs, Agriculture, Interior, NASA, Commerce, Labor, Treasury Ops, NSF, EPA, SBA, and the generic non-USA categories Social Programs, Public Services and Administration - each real, but none has a differentiated effect beyond the generic Budget/DebtToGdp channel every spending line shares. (Infrastructure & Development and Healthcare & Social Care left this group 2026-08-28: they fold onto the Transportation and HHS nodes, whose effects they carry.)" } },
            { PolicyNodeId.UBI, new PolicyNodeInfo { Name = "UBI", Area = UiPalette.SystemArea.Welfare, Description = "Universal Basic Income generosity level, 0-100%." } },
            { PolicyNodeId.NegativeIncomeTax, new PolicyNodeInfo { Name = "Negative Income Tax", Area = UiPalette.SystemArea.Welfare, Description = "Negative Income Tax generosity level, 0-100%." } },
            { PolicyNodeId.MeansTestedWelfare, new PolicyNodeInfo { Name = "Means-Tested Welfare", Area = UiPalette.SystemArea.Welfare, Description = "Means-tested welfare generosity level, 0-100%." } },
            { PolicyNodeId.UniversalHealthcare, new PolicyNodeInfo { Name = "Universal Healthcare", Area = UiPalette.SystemArea.Welfare, Description = "Universal Healthcare generosity level, 0-100%." } },
            { PolicyNodeId.HousingAssistance, new PolicyNodeInfo { Name = "Housing Assistance", Area = UiPalette.SystemArea.Welfare, Description = "Housing Assistance generosity level, 0-100%." } },
            { PolicyNodeId.ChildcareSubsidies, new PolicyNodeInfo { Name = "Childcare Subsidies", Area = UiPalette.SystemArea.Welfare, Description = "Childcare Subsidies generosity level, 0-100%." } },
            { PolicyNodeId.SectorSubsidy, new PolicyNodeInfo { Name = "Sector Subsidies", Area = UiPalette.SystemArea.Sectors, Description = "Applies uniformly across all eight Sectors (one conceptual dial, not a separate node per sector)." } },
            { PolicyNodeId.SectorRegulation, new PolicyNodeInfo { Name = "Sector Regulation", Area = UiPalette.SystemArea.Sectors, Description = "Applies uniformly across all eight Sectors." } },
            { PolicyNodeId.SectorTaxCredit, new PolicyNodeInfo { Name = "Sector Tax Credits", Area = UiPalette.SystemArea.Sectors, Description = "Applies uniformly across all eight Sectors." } },
            { PolicyNodeId.SectorResearchGrants, new PolicyNodeInfo { Name = "Sector Research Grants", Area = UiPalette.SystemArea.Sectors, Description = "Applies uniformly across all eight Sectors." } },
            { PolicyNodeId.SectorDeregulationNationalization, new PolicyNodeInfo { Name = "Deregulation / Nationalization", Area = UiPalette.SystemArea.Sectors, Description = "0 (nationalized) to 100 (deregulated) - the one dial where Output and Employment move in OPPOSITE directions (privatization gains efficiency by shedding labor)." } },
            { PolicyNodeId.SwfContributionRate, new PolicyNodeInfo { Name = "SWF Contribution Rate", Area = UiPalette.SystemArea.SovereignWealth, Description = "% of GDP contributed to (or, if negative, withdrawn from) the Sovereign Wealth Fund each year." } },
            { PolicyNodeId.SwfAssetAllocation, new PolicyNodeInfo { Name = "SWF Asset Allocation", Area = UiPalette.SystemArea.SovereignWealth, Description = "Equities/Bonds/Infrastructure/Real Estate weighting - higher equities weight raises expected AND variance of returns." } },
            { PolicyNodeId.TariffPolicy, new PolicyNodeInfo { Name = "Tariff Policy", Area = UiPalette.SystemArea.Trade, Description = "This country's own base tariff rate plus any per-partner override. Partners mirror an override's excess onto your exports; a change in the take passes through to prices for a year." } },
            { PolicyNodeId.InterestRateDecision, new PolicyNodeInfo { Name = "Interest Rate", Area = UiPalette.SystemArea.Political, Description = "Central bank rate - Taylor-Rule-determined for a country with an independent Fed chair, player-set via PolicyDecision.InterestRateChange otherwise." } },
        };

        private static readonly Dictionary<StatNodeId, StatNodeInfo> StatInfo = new Dictionary<StatNodeId, StatNodeInfo>
        {
            // The two money stats. Every currency figure in the model is stored in billions - verified
            // against the seeds, see MoneyUnit's own doc comment - and these are the only two entries in
            // this table that are amounts rather than rates, percentages or indices.
            { StatNodeId.Gdp, new StatNodeInfo { Name = "GDP", HigherIsBetter = true, Unit = MoneyUnit.Billions } },
            { StatNodeId.Unemployment, new StatNodeInfo { Name = "Unemployment", HigherIsBetter = false } },
            { StatNodeId.Inflation, new StatNodeInfo { Name = "Inflation", HigherIsBetter = null } },
            { StatNodeId.Approval, new StatNodeInfo { Name = "Approval Rating", HigherIsBetter = true } },
            { StatNodeId.DebtToGdp, new StatNodeInfo { Name = "Debt-to-GDP", HigherIsBetter = false } },
            { StatNodeId.Poverty, new StatNodeInfo { Name = "Poverty Rate", HigherIsBetter = false } },
            { StatNodeId.InterestRate, new StatNodeInfo { Name = "Interest Rate", HigherIsBetter = null } },
            { StatNodeId.TradeBalance, new StatNodeInfo { Name = "Trade Balance", HigherIsBetter = true, Unit = MoneyUnit.Billions } },
            { StatNodeId.Lfpr, new StatNodeInfo { Name = "Labor Force Participation", HigherIsBetter = true } },
            { StatNodeId.Crime, new StatNodeInfo { Name = "Crime Index", HigherIsBetter = false } },
            { StatNodeId.PrisonPopulation, new StatNodeInfo { Name = "Incarceration Rate", HigherIsBetter = null } },
            { StatNodeId.OrganizedCrime, new StatNodeInfo { Name = "Organized Crime Index", HigherIsBetter = false } },
            { StatNodeId.Corruption, new StatNodeInfo { Name = "Corruption Index", HigherIsBetter = false } },
            { StatNodeId.PotentialGrowth, new StatNodeInfo { Name = "Potential Growth Rate", HigherIsBetter = true } },
            { StatNodeId.PopulationGrowthRate, new StatNodeInfo { Name = "Population Growth Rate", HigherIsBetter = null } },
            { StatNodeId.DependencyRatio, new StatNodeInfo { Name = "Dependency Ratio", HigherIsBetter = false } },
            { StatNodeId.ConsumerConfidence, new StatNodeInfo { Name = "Consumer Confidence", HigherIsBetter = true } },
            { StatNodeId.BusinessConfidence, new StatNodeInfo { Name = "Business Confidence", HigherIsBetter = true } },
        };

        /// <summary>
        /// Every edge, grouped by source for readability. Direction (Increases) and RelativeStrength
        /// are both sourced from the real MacroSystem/SimulationManager formulas researched for this
        /// task - see the inline comment on each group for the specific method/constant.
        ///
        /// Omnibus 2026-08-28: the list is no longer purely AUTHORED. Wherever a ledger term
        /// records an edge's effect (ApprovalAttribution: TaxHikePenalty, SpendingEffect, WelfareEffect,
        /// PaidLeaveEffect, DrugPolicyEffect; DebtAttribution: PrimaryBalanceEffect for every money
        /// lever, InterestAtIssuance for the policy rate; the confidence book's policy base) the edge
        /// is marked DERIVED with its term named - the line is the audited arithmetic. Every other
        /// edge stays DECLARED, its sign and weight from the formula it cites, and draws in the
        /// declared idiom (dashed, reduced ink). The authored array survives as the declared set: the
        /// derived list is additive plumbing, and restoring the old drawing is one provenance swap.
        /// </summary>
        private static readonly List<PolicyWebEdge> Edges = BuildEdges();

        // The ledger term names, once: the field names on ApprovalAttribution / DebtAttribution and
        // the confidence book's row, so the panel's tag and the ledger cannot spell the same term
        // two ways.
        private const string TermTaxHike = "TaxHikePenalty";
        private const string TermSpending = "SpendingEffect";
        private const string TermWelfare = "WelfareEffect";
        private const string TermPaidLeave = "PaidLeaveEffect";
        private const string TermDrugPolicy = "DrugPolicyEffect";
        private const string TermGrowth = "GrowthEffect";
        private const string TermMiseryUnemployment = "MiseryUnemployment";
        private const string TermMiseryInflation = "MiseryInflation";
        private const string TermMiseryCrime = "MiseryCrime";
        private const string TermMiseryCorruption = "MiseryCorruption";
        private const string TermPrimaryBalance = "PrimaryBalanceEffect";
        private const string TermInterestAtIssuance = "InterestAtIssuance";
        private const string TermErosion = "Erosion";
        private const string TermConfidenceBase = "Policy base (confidence book)";

        private static PolicyWebEdge Derived(PolicyNodeId source, StatNodeId target, bool increases, float relativeStrength, string term)
            => new PolicyWebEdge(source, target, increases, relativeStrength, EdgeProvenance.Derived, term);

        private static List<PolicyWebEdge> BuildEdges()
        {
            var e = new List<PolicyWebEdge>();

            // Labor - the sensitivities live in LaborCouplings (pass 3's declared labor coupling
            // table, 2026-08-26; formerly MacroSystem's scattered constants, moved verbatim the
            // same way the C&J constants moved to CrimeJusticeCouplings): the Get*Adjustment
            // helpers stay on MacroSystem, the constants are the table's.
            e.Add(new PolicyWebEdge(PolicyNodeId.MinimumWage, StatNodeId.Unemployment, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.MinimumWage, StatNodeId.Poverty, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.PaidFamilyLeave, StatNodeId.Lfpr, true));
            // Derived: the approval ledger's PaidLeaveEffect IS this edge, every period.
            e.Add(Derived(PolicyNodeId.PaidFamilyLeave, StatNodeId.Approval, true, 1f, TermPaidLeave));
            e.Add(new PolicyWebEdge(PolicyNodeId.OvertimeRegulation, StatNodeId.Unemployment, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.RetrainingProgram, StatNodeId.Unemployment, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.RetrainingProgram, StatNodeId.Lfpr, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.FamilyPolicy, StatNodeId.PopulationGrowthRate, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.ImmigrationPolicy, StatNodeId.PopulationGrowthRate, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.ImmigrationPolicy, StatNodeId.Lfpr, true));

            // Crime & Justice (MacroSystem.ApplyCrimeIndex/ApplyPrisonPopulationRate/
            // ApplyOrganizedCrimeIndex/ApplyCorruptionIndex/ApplyApprovalRating's DrugPolicyApprovalSensitivity).
            // Weights fold to CrimeJusticeCouplings const refs (the couplings pass, 2026-08-26) -
            // the extraction left these as restated literal ratios, the one place a re-tuned
            // sensitivity could silently drift from the declared table.
            e.Add(new PolicyWebEdge(PolicyNodeId.PoliceFunding, StatNodeId.Crime, false, CrimeJusticeCouplings.PoliceFundingSensitivity / CrimeJusticeCouplings.PoliceFundingSensitivity));
            e.Add(new PolicyWebEdge(PolicyNodeId.PoliceFunding, StatNodeId.OrganizedCrime, false, CrimeJusticeCouplings.PoliceFundingOrganizedCrimeSensitivity / CrimeJusticeCouplings.BorderEnforcementOrganizedCrimeSensitivity));
            e.Add(new PolicyWebEdge(PolicyNodeId.SentencingSeverity, StatNodeId.Crime, false, CrimeJusticeCouplings.SentencingSensitivity / CrimeJusticeCouplings.PoliceFundingSensitivity));
            // Edge sweep (2026-08-27, Elias's pass-6 follow-up): the Crime group denominates on
            // PoliceFundingSensitivity and the PrisonPopulation group on BailReformPrisonPopulationSensitivity
            // (line 261 declared that denominator in the couplings pass); three edges had been left at
            // the 1f default inside those groups - a 0.02 bail-crime sensitivity drawing as thick as the
            // 0.16 police one. Every weight below is the declared table's own ratio.
            e.Add(new PolicyWebEdge(PolicyNodeId.BailReform, StatNodeId.Crime, true, CrimeJusticeCouplings.BailReformCrimeIndexSensitivity / CrimeJusticeCouplings.PoliceFundingSensitivity));
            e.Add(new PolicyWebEdge(PolicyNodeId.BailReform, StatNodeId.PrisonPopulation, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.DrugPolicy, StatNodeId.PrisonPopulation, true, CrimeJusticeCouplings.DrugPolicyPrisonPopulationSensitivity / CrimeJusticeCouplings.BailReformPrisonPopulationSensitivity));
            // Derived: the approval ledger's DrugPolicyEffect.
            e.Add(Derived(PolicyNodeId.DrugPolicy, StatNodeId.Approval, true, 1f, TermDrugPolicy));
            e.Add(new PolicyWebEdge(PolicyNodeId.JudicialFunding, StatNodeId.PrisonPopulation, false, CrimeJusticeCouplings.JudicialFundingPrisonPopulationSensitivity / CrimeJusticeCouplings.BailReformPrisonPopulationSensitivity));
            e.Add(new PolicyWebEdge(PolicyNodeId.JudicialFunding, StatNodeId.OrganizedCrime, false, CrimeJusticeCouplings.JudicialFundingOrganizedCrimeSensitivity / CrimeJusticeCouplings.BorderEnforcementOrganizedCrimeSensitivity));
            e.Add(new PolicyWebEdge(PolicyNodeId.JudicialFunding, StatNodeId.Corruption, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.BorderEnforcement, StatNodeId.OrganizedCrime, false, 1f));
            // The couplings pass's new edges (terminal rulings 2026-08-26): the sentencing-prison
            // edge (weight relative to bail's own primary prison lever, table refs), and the three
            // line-resident budget edges - enforcement cost lands on real spending lines and
            // reaches the debt path through the fiscal engine, so DebtToGdp is the honest target
            // node. Edge sweep (2026-08-27): all four were born with the WRONG sign under Increases'
            // own definition - harsher sentencing RAISES the prison population (the NRC time-served
            // channel, MacroSystem.ApplyPrisonPopulationRate), and a dial that spends money RAISES
            // the debt ratio (they had taken the tax group's sign; AddSpending's own DebtToGdp edges
            // are `true`). The three budget edges are one documented group, weighted by their
            // declared per-point costs.
            e.Add(new PolicyWebEdge(PolicyNodeId.SentencingSeverity, StatNodeId.PrisonPopulation, true, CrimeJusticeCouplings.SentencingPrisonPopulationSensitivity / CrimeJusticeCouplings.BailReformPrisonPopulationSensitivity));
            // Derived: line-resident spending reaches the debt path through the fiscal engine, and
            // the debt ledger's PrimaryBalanceEffect records it every day.
            e.Add(Derived(PolicyNodeId.PoliceFunding, StatNodeId.DebtToGdp, true, CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint / CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint, TermPrimaryBalance));
            e.Add(Derived(PolicyNodeId.JudicialFunding, StatNodeId.DebtToGdp, true, CrimeJusticeCouplings.JudicialFundingBudgetCostPercentOfGdpPerPoint / CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint, TermPrimaryBalance));
            e.Add(Derived(PolicyNodeId.BorderEnforcement, StatNodeId.DebtToGdp, true, CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint / CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint, TermPrimaryBalance));

            // Taxes: every TaxType shares the SAME two real channels (MacroSystem.ApplyApprovalRating's
            // TaxHikeApprovalSensitivity on a this-turn hike; SimulationManager.GetTotalTaxRevenue feeding
            // Budget/GovernmentDebt, i.e. DebtToGdp) - not a fabricated per-tax-type distinction.
            // PolicyNodeId.Tariffs is NOT in this list (edge sweep, 2026-08-27): TaxType.Tariffs never
            // gets a TaxLine, so neither channel exists for it - its two edges were phantoms. The node
            // stays as a signpost with zero edges; the real lever is TariffPolicy below.
            foreach (PolicyNodeId tax in new[] {
                PolicyNodeId.IncomeTax, PolicyNodeId.CorporateTax, PolicyNodeId.VAT, PolicyNodeId.PayrollTax,
                PolicyNodeId.CapitalGainsTax, PolicyNodeId.SalesTax, PolicyNodeId.ExciseTax, PolicyNodeId.PropertyTax,
                PolicyNodeId.EstateTax, PolicyNodeId.WealthTax, PolicyNodeId.CarbonTax, PolicyNodeId.StampDuty })
            {
                // Both derived: the approval ledger's TaxHikePenalty and the debt ledger's
                // PrimaryBalanceEffect record exactly these two channels.
                e.Add(Derived(tax, StatNodeId.Approval, false, 1f, TermTaxHike));
                e.Add(Derived(tax, StatNodeId.DebtToGdp, false, 1f, TermPrimaryBalance));
            }

            // Spending (MacroSystem.ApplyApprovalRating's per-category multipliers, ApplyCategorySpendingEffects,
            // every line's generic Budget/DebtToGdp contribution). RelativeStrength within the Approval
            // group uses the real multipliers (Mandatory 3.0 is the strongest, Defense 0.5 the weakest).
            AddSpending(e, PolicyNodeId.SocialSecurity, 3.0f);
            AddSpending(e, PolicyNodeId.Medicare, 3.0f);
            AddSpending(e, PolicyNodeId.Medicaid, 3.0f);
            AddSpending(e, PolicyNodeId.IncomeSecurity, 3.0f);
            AddSpending(e, PolicyNodeId.VeteransBenefitsMandatory, 3.0f);
            AddSpending(e, PolicyNodeId.FederalRetirement, 3.0f);
            AddSpending(e, PolicyNodeId.Defense, 0.5f);
            AddSpending(e, PolicyNodeId.HomelandSecurity, 0.7f);
            AddSpending(e, PolicyNodeId.Transportation, 1.0f);
            e.Add(new PolicyWebEdge(PolicyNodeId.Transportation, StatNodeId.PotentialGrowth, true));
            AddSpending(e, PolicyNodeId.HHSDiscretionary, 1.5f);
            e.Add(new PolicyWebEdge(PolicyNodeId.HHSDiscretionary, StatNodeId.ConsumerConfidence, true));
            AddSpending(e, PolicyNodeId.Education, 1.5f);
            e.Add(new PolicyWebEdge(PolicyNodeId.Education, StatNodeId.BusinessConfidence, true));
            AddSpending(e, PolicyNodeId.Justice, 1.0f);
            e.Add(new PolicyWebEdge(PolicyNodeId.Justice, StatNodeId.Crime, false));
            AddSpending(e, PolicyNodeId.Energy, 1.0f);
            e.Add(new PolicyWebEdge(PolicyNodeId.Energy, StatNodeId.BusinessConfidence, true));
            AddSpending(e, PolicyNodeId.Housing, 1.3f);
            e.Add(new PolicyWebEdge(PolicyNodeId.Housing, StatNodeId.Poverty, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.OtherDiscretionarySpending, StatNodeId.DebtToGdp, true));

            // Welfare (MacroSystem.GetPovertyReductionSensitivity/GetWelfareApprovalSensitivity per type -
            // RelativeStrength reads the real per-type constants for both groups THROUGH THE ACCESSORS,
            // since the edge sweep of 2026-08-27 found three poverty weights restated as literals that
            // had never matched the constants (UniversalHealthcare 2 vs 4, HousingAssistance 2 vs 3,
            // ChildcareSubsidies 1.5 vs 3). Every program's cost is a real spending flow
            // (SimulationManager.GetTotalWelfareCost -> totalSpending), so each program also feeds
            // DebtToGdp, the same edge every spending-category node draws - the couplings pass's rule
            // that money reaching the debt path targets DebtToGdp, applied to the one area that lacked it.
            AddWelfare(e, PolicyNodeId.UBI, WelfareProgramType.UBI);
            // Derived: the confidence book's "Policy base" row is the healthcare/UBI accumulation.
            e.Add(Derived(PolicyNodeId.UBI, StatNodeId.ConsumerConfidence, true, 1f, TermConfidenceBase));
            AddWelfare(e, PolicyNodeId.NegativeIncomeTax, WelfareProgramType.NegativeIncomeTax);
            AddWelfare(e, PolicyNodeId.MeansTestedWelfare, WelfareProgramType.MeansTestedWelfare);
            AddWelfare(e, PolicyNodeId.UniversalHealthcare, WelfareProgramType.UniversalHealthcare);
            e.Add(Derived(PolicyNodeId.UniversalHealthcare, StatNodeId.BusinessConfidence, true, 1f, TermConfidenceBase));
            AddWelfare(e, PolicyNodeId.HousingAssistance, WelfareProgramType.HousingAssistance);
            AddWelfare(e, PolicyNodeId.ChildcareSubsidies, WelfareProgramType.ChildcareSubsidies);

            // Sectors (MacroSystem.ApplySectorEffects/GetSectorGrowthAdjustment/GetSectorUnemploymentAdjustment
            // - each dial applies uniformly across all eight Sectors, feeding PotentialGrowth/Unemployment
            // under Sector Integration's own combined ceiling).
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorSubsidy, StatNodeId.PotentialGrowth, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorSubsidy, StatNodeId.Unemployment, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorRegulation, StatNodeId.PotentialGrowth, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorRegulation, StatNodeId.Unemployment, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorTaxCredit, StatNodeId.PotentialGrowth, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorTaxCredit, StatNodeId.Unemployment, false));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorResearchGrants, StatNodeId.PotentialGrowth, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorResearchGrants, StatNodeId.Unemployment, false, 0.5f));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorDeregulationNationalization, StatNodeId.PotentialGrowth, true));
            e.Add(new PolicyWebEdge(PolicyNodeId.SectorDeregulationNationalization, StatNodeId.Unemployment, true));

            // Sovereign Wealth Fund - genuinely complex, multi-turn, risk/return-mediated (contribution
            // builds an asset base that only pays off in DebtToGdp terms over many turns via investment
            // returns). Edge sweep (2026-08-27): there is no "neutral" encoding - Increases is drawn
            // as a signed colour - so each edge carries the sign of its own expected effect: the
            // contribution is spending today (raises the ratio), a higher equities weight raises the
            // expected return the structural draw books as revenue (lowers it). The pair's 0.5 stays
            // undeclared-relative (no cross-comparable single-turn constant exists for either).
            e.Add(new PolicyWebEdge(PolicyNodeId.SwfContributionRate, StatNodeId.DebtToGdp, true, 0.5f));
            e.Add(new PolicyWebEdge(PolicyNodeId.SwfAssetAllocation, StatNodeId.DebtToGdp, false, 0.5f));

            // Trade (TradeSystem.ApplyTradeEffects; pass 6, 2026-08-27). Our own tariff take is real
            // revenue since pass 5 - it LOWERS the debt ratio (the `true` this edge carried from the
            // accumulator era was a sign slip against Increases' own definition, fixed here). Since pass
            // 6 a per-partner override also moves our OWN TradeBalance - partners mirror its excess onto
            // our exports (TradeSystem.GetRetaliatoryTariffRate) - and the change in the take passes
            // through to Inflation for a year (TradeCosts.ImportPricePassThrough).
            // Derived: the take is revenue on the debt ledger (PrimaryBalanceEffect). The trade-balance
            // and pass-through edges stay declared - the pass-through is a FiscalPeriod term on the
            // report, not a ledger term, and the mirrored balance has no ledger at all.
            e.Add(Derived(PolicyNodeId.TariffPolicy, StatNodeId.DebtToGdp, false, 0.5f, TermPrimaryBalance));
            e.Add(new PolicyWebEdge(PolicyNodeId.TariffPolicy, StatNodeId.TradeBalance, false, 0.5f));
            e.Add(new PolicyWebEdge(PolicyNodeId.TariffPolicy, StatNodeId.Inflation, true, 0.5f));

            // Interest rate (MacroSystem.ApplyNationalAccounts - rate above TaylorRule.NeutralRealRate
            // dampens Consumption/Investment directly; Unemployment/Inflation react only INDIRECTLY,
            // through Okun's Law/the Phillips Curve afterward - not drawn as a second direct edge).
            e.Add(new PolicyWebEdge(PolicyNodeId.InterestRateDecision, StatNodeId.Gdp, false));
            // Derived, PER COUNTRY: the policy rate prices new issuance, and the debt ledger's
            // InterestAtIssuance records it - for the five whose issuance rate follows the policy
            // rate. The USA's does not (Country.BaseDebtInterestRateOverride, WorldFactory: 3.3), so
            // IsLiveFor drops this edge for a country with the override set - the web reads the
            // country's own books, not a six-country average.
            e.Add(Derived(PolicyNodeId.InterestRateDecision, StatNodeId.DebtToGdp, true, 1f, TermInterestAtIssuance));

            return e;
        }

        private static void AddSpending(List<PolicyWebEdge> e, PolicyNodeId node, float approvalMultiplier)
        {
            // Both derived: the approval ledger's SpendingEffect and the debt ledger's PrimaryBalanceEffect.
            e.Add(Derived(node, StatNodeId.Approval, true, approvalMultiplier / 3.0f, TermSpending));
            e.Add(Derived(node, StatNodeId.DebtToGdp, true, 1f, TermPrimaryBalance));
        }

        /// <summary>The three edges every welfare program draws: poverty reduction (declared - the poverty reduction is a target pull with no ledger) and approval (derived: the approval ledger's WelfareEffect), each weighted by the program's own declared constant relative to UBI's (the largest of both groups), and the debt path its cost reaches (derived: PrimaryBalanceEffect; uniform, like every spending node's).</summary>
        private static void AddWelfare(List<PolicyWebEdge> e, PolicyNodeId node, WelfareProgramType type)
        {
            e.Add(new PolicyWebEdge(node, StatNodeId.Poverty, false,
                MacroSystem.GetPovertyReductionSensitivity(type) / MacroSystem.GetPovertyReductionSensitivity(WelfareProgramType.UBI)));
            e.Add(Derived(node, StatNodeId.Approval, true,
                MacroSystem.GetWelfareApprovalSensitivity(type) / MacroSystem.GetWelfareApprovalSensitivity(WelfareProgramType.UBI), TermWelfare));
            e.Add(Derived(node, StatNodeId.DebtToGdp, true, 1f, TermPrimaryBalance));
        }

        /// <summary>
        /// The causal graph's stat → stat edges, every one a ledger term whose INPUT is another
        /// stat - derived by construction, listed here from the ledgers' own field names. The approval
        /// formula: growth against potential (GrowthEffect - the GDP node is the growth carrier this
        /// web has), and the four misery gaps (unemployment above NAIRU, inflation off target, crime
        /// and corruption above baseline). The debt identity: inflation erodes the stock (−π·b,
        /// Erosion) and the policy rate prices issuance (InterestAtIssuance). Weights are the
        /// formula's own sensitivities relative to the largest in each group. NOT drawn, stated: the
        /// GiniEffect (no Gini node on this web) and the confidence book's wage-sentiment factor (no
        /// real-wage node) - two ledger terms whose input stat this web does not carry.
        /// </summary>
        private static readonly List<StatWebEdge> StatEdges = BuildStatEdges();

        private static List<StatWebEdge> BuildStatEdges()
        {
            var e = new List<StatWebEdge>
            {
                new StatWebEdge(StatNodeId.Gdp, StatNodeId.Approval, true, 1f, TermGrowth),
                new StatWebEdge(StatNodeId.Unemployment, StatNodeId.Approval, false, 1f, TermMiseryUnemployment),
                new StatWebEdge(StatNodeId.Inflation, StatNodeId.Approval, false, MacroSystem.InflationApprovalSensitivity / MacroSystem.UnemploymentApprovalSensitivity, TermMiseryInflation),
                new StatWebEdge(StatNodeId.Crime, StatNodeId.Approval, false, MacroSystem.CrimeApprovalSensitivity / MacroSystem.UnemploymentApprovalSensitivity, TermMiseryCrime),
                new StatWebEdge(StatNodeId.Corruption, StatNodeId.Approval, false, MacroSystem.CorruptionApprovalSensitivity / MacroSystem.UnemploymentApprovalSensitivity, TermMiseryCorruption),
                new StatWebEdge(StatNodeId.Inflation, StatNodeId.DebtToGdp, false, 1f, TermErosion),
                new StatWebEdge(StatNodeId.InterestRate, StatNodeId.DebtToGdp, true, 1f, TermInterestAtIssuance),
            };
            return e;
        }

        /// <summary>
        /// R-K1's per-country edge set, as a predicate over the one shared list rather than six copies:
        /// the only edge that differs today is the policy rate's derived edge to the debt ratio, which
        /// does not exist for a country whose issuance rate is pinned by BaseDebtInterestRateOverride
        /// (the USA). Null country = the full set (the chips' area-level enumeration).
        /// </summary>
        public static bool IsLiveFor(PolicyWebEdge edge, Country country)
        {
            if (country == null)
            {
                return true;
            }

            if (edge.Source == PolicyNodeId.InterestRateDecision && edge.Target == StatNodeId.DebtToGdp)
            {
                return country.BaseDebtInterestRateOverride < 0f;
            }

            return true;
        }

        /// <summary>The stat → stat edges touching a stat, either end, for the panel's "moved by" / "feeds" lists.</summary>
        /// <summary>C-C3: every edge, for a census that has to check a property of ALL of them (R-W2's
        /// "every encoded weight traces to the coupling table"). A copy, so a caller cannot edit the
        /// model's own table through it.</summary>
        public static List<PolicyWebEdge> GetAllEdges() => new List<PolicyWebEdge>(Edges);

        /// <summary>C-C3: the stat→stat half, on the same terms.</summary>
        public static List<StatWebEdge> GetAllStatEdges() => new List<StatWebEdge>(StatEdges);

        public static List<StatWebEdge> GetStatEdgesFor(StatNodeId id)
        {
            var result = new List<StatWebEdge>();
            foreach (StatWebEdge edge in StatEdges)
            {
                if (edge.Source == id || edge.Target == id) result.Add(edge);
            }
            return result;
        }

        private Dictionary<PolicyNodeId, Vector2> _policyPixels;
        private Dictionary<StatNodeId, Vector2> _statPixels;

        /// <summary>One wedge ("pie slice") of the circle - either a policy SystemArea, or (Area == null) the single Stats wedge. Order here is the order wedges appear around the circle, starting at the top and sweeping clockwise.</summary>
        private readonly struct WedgeDef
        {
            public readonly string Label;
            public readonly UiPalette.SystemArea? Area;
            public WedgeDef(string label, UiPalette.SystemArea? area) { Label = label; Area = area; }
        }

        private static readonly WedgeDef[] Wedges =
        {
            new WedgeDef("Labor", UiPalette.SystemArea.Labor),
            new WedgeDef("Crime & Justice", UiPalette.SystemArea.CrimeJustice),
            new WedgeDef("Fiscal", UiPalette.SystemArea.Fiscal),
            new WedgeDef("Welfare", UiPalette.SystemArea.Welfare),
            new WedgeDef("Sectors", UiPalette.SystemArea.Sectors),
            new WedgeDef("Sovereign Wealth", UiPalette.SystemArea.SovereignWealth),
            new WedgeDef("Trade", UiPalette.SystemArea.Trade),
            new WedgeDef("Political", UiPalette.SystemArea.Political),
            new WedgeDef("Stats", null),
        };

        /// <summary>Each node's DEGREE (count of real edges touching it, in either direction) - "importance" for sizing purposes, computed once from the same audited Edges list every line/color decision already uses, not a separate invented metric.</summary>
        private static readonly Dictionary<PolicyNodeId, int> PolicyDegree = BuildPolicyDegree();
        private static readonly Dictionary<StatNodeId, int> StatDegree = BuildStatDegree();
        private static readonly int MinObservedDegree = ComputeMinDegree();
        private static readonly int MaxObservedDegree = ComputeMaxDegree();

        private static Dictionary<PolicyNodeId, int> BuildPolicyDegree()
        {
            var result = new Dictionary<PolicyNodeId, int>();
            foreach (PolicyNodeId id in System.Enum.GetValues(typeof(PolicyNodeId))) result[id] = 0;
            foreach (PolicyWebEdge edge in Edges) result[edge.Source]++;
            return result;
        }

        private static Dictionary<StatNodeId, int> BuildStatDegree()
        {
            var result = new Dictionary<StatNodeId, int>();
            foreach (StatNodeId id in System.Enum.GetValues(typeof(StatNodeId))) result[id] = 0;
            foreach (PolicyWebEdge edge in Edges) result[edge.Target]++;
            // R-K1: the causal graph's stat → stat edges count at both ends - Approval and DebtToGdp are
            // the hubs the ledgers say they are.
            foreach (StatWebEdge edge in StatEdges) { result[edge.Source]++; result[edge.Target]++; }
            return result;
        }

        private static int ComputeMinDegree()
        {
            int min = int.MaxValue;
            foreach (int d in PolicyDegree.Values) min = Mathf.Min(min, d);
            foreach (int d in StatDegree.Values) min = Mathf.Min(min, d);
            return min;
        }

        private static int ComputeMaxDegree()
        {
            int max = 0;
            foreach (int d in PolicyDegree.Values) max = Mathf.Max(max, d);
            foreach (int d in StatDegree.Values) max = Mathf.Max(max, d);
            return max;
        }

        /// <summary>Node diameter scales with degree, clamped to [MinNodeDiameter, MaxNodeDiameter] - a 0-edge node (e.g. DependencyRatio, which no policy in this pass directly targets) still renders at the visible minimum; the single most-connected node (Approval Rating) is capped at the maximum rather than dominating the whole diagram.</summary>
        private static float GetNodeDiameter(int degree)
        {
            float t = MaxObservedDegree > MinObservedDegree
                ? Mathf.InverseLerp(MinObservedDegree, MaxObservedDegree, degree)
                : 0f;
            return Mathf.Lerp(MinNodeDiameter, MaxNodeDiameter, t);
        }

        private static int CountInArea(UiPalette.SystemArea area)
        {
            int count = 0;
            foreach (KeyValuePair<PolicyNodeId, PolicyNodeInfo> kv in PolicyInfo)
            {
                if (kv.Value.Area == area) count++;
            }
            return count;
        }

        /// <summary>
        /// Widest wedge header label as rendered in headerStyle - the margin permanently reserved
        /// around the circle for the ~9 always-visible headers. Deliberately does NOT budget for node
        /// (policy/stat) labels, even though those can be considerably longer ("Veterans Benefits
        /// (Mand.)", "Labor Force Participation") - only one node label is ever showing at a time
        /// (hover/pin), so reserving permanent radius for the single worst-case name across all ~73
        /// nodes would shrink the circle for every frame to guard against a label that's active maybe
        /// a few seconds at a time. Draw instead clamps that one active label's rect to the diagram's
        /// own bounds at draw time (see the ClampLabelRect call in DrawRadialLabel) - cheap per-frame
        /// safety net instead of a permanent space tax. Recomputed every call, not cached, for the same
        /// reason every other label-width measurement in this UI now is (see GetColumnWidth's own
        /// history in MapRenderer/GameController).
        /// </summary>
        private static float GetLabelMargin(GUIStyle headerStyle)
        {
            float widest = 0f;
            foreach (WedgeDef wedge in Wedges)
            {
                widest = Mathf.Max(widest, headerStyle.CalcSize(new GUIContent(wedge.Label)).x);
            }
            return widest + LabelPad * 2f;
        }

        /// <summary>Same headerStyle construction Draw uses - kept in one place so any external size calculations can never drift out of sync with what Draw actually renders.</summary>
        private static GUIStyle GetHeaderStyle(GUIStyle labelStyle) => new GUIStyle(labelStyle) { fontSize = Mathf.Max(9, labelStyle.fontSize - 1) };

        /// <summary>Each wedge's angular span in degrees, proportional to its node count but floored at MinWedgeSpanDegrees - any degrees the floor adds are taken from the remaining (non-floored) wedges' own proportional split of what's left over, so the whole set (plus gaps) still sums to exactly 360 degrees. Order matches Wedges.</summary>
        private static float[] ComputeWedgeSpans()
        {
            int totalNodes = PolicyInfo.Count + StatInfo.Count;
            float availableDegrees = 360f - Wedges.Length * WedgeGapDegrees;

            var nodeCounts = new int[Wedges.Length];
            var spans = new float[Wedges.Length];
            var floored = new bool[Wedges.Length];
            float flooredTotal = 0f;
            int unflooredNodeSum = 0;

            for (int w = 0; w < Wedges.Length; w++)
            {
                int n = Wedges[w].Area.HasValue ? CountInArea(Wedges[w].Area.Value) : StatInfo.Count;
                nodeCounts[w] = n;
                float raw = (float)n / totalNodes * availableDegrees;
                if (raw < MinWedgeSpanDegrees)
                {
                    spans[w] = MinWedgeSpanDegrees;
                    floored[w] = true;
                    flooredTotal += MinWedgeSpanDegrees;
                }
                else
                {
                    unflooredNodeSum += n;
                }
            }

            float remainingDegrees = availableDegrees - flooredTotal;
            for (int w = 0; w < Wedges.Length; w++)
            {
                if (!floored[w])
                {
                    spans[w] = (float)nodeCounts[w] / unflooredNodeSum * remainingDegrees;
                }
            }

            return spans;
        }

        /// <summary>
        /// Draws the whole diagram into <paramref name="rect"/> - the caller sizes that rect (large
        /// majority of its own available panel space, per the Screen-fraction pattern used throughout
        /// this UI), not this method. Returns whichever node was clicked this event, if any.
        /// <paramref name="pinnedPolicy"/>/<paramref name="pinnedStat"/> is whichever node the caller
        /// currently has pinned (its own detail panel showing) - at most one of the two is ever
        /// non-null, mirroring GameController's own single-selection state.
        ///
        /// Circular chord-diagram layout: every node sits on the circumference of one circle, grouped
        /// into wedges (one per policy SystemArea, sized proportional to how many nodes it holds but
        /// floored at MinWedgeSpanDegrees so a 1-2-node area isn't squeezed unreadably thin - see
        /// ComputeWedgeSpans - plus one wedge for the 18 stat nodes), separated by a visible radial
        /// divider at each wedge boundary. Node SIZE scales with DEGREE (see GetNodeDiameter) - how
        /// many real edges touch it, from the same audited Edges data every line already uses, not a
        /// separate metric.
        ///
        /// At rest, only the ~9 wedge header labels are always visible - individual node labels (all
        /// ~73 of them) are NOT, since no circle size keeps that many simultaneous labels legible at
        /// once, especially in the 18-node Stats wedge. Edge visibility is interaction-based too, not
        /// permanent: at rest NO edges draw at all, just the positioned/sized/colored dots - with ~55
        /// policy nodes x 18 stat nodes, permanently drawing every real edge at once was legible as "a
        /// dense web exists" but not as "which specific things connect to which." Hovering a node makes
        /// it the active node, drawing ONLY its own edges (both directions) AND its own label - both
        /// revealed together, the label anchored to the node itself the same way the edges are, not a
        /// separate mouse-following tooltip - replacing whatever's pinned while the mouse stays there.
        /// The pinned node (if any, and nothing is currently hovered) stays active after the mouse
        /// moves away, matching how the detail panel itself already stays pinned - this is
        /// INTENTIONALLY the same node the panel is showing, not a separate pin state.
        /// </summary>
        public void Draw(Rect rect, GUIStyle labelStyle, PolicyNodeId? pinnedPolicy, StatNodeId? pinnedStat, out PolicyNodeId? clickedPolicy, out StatNodeId? clickedStat)
        {
            Draw(rect, labelStyle, null, pinnedPolicy, pinnedStat, out clickedPolicy, out clickedStat);
        }

        /// <summary>The per-country form: <paramref name="country"/> selects the live edge set
        /// (see IsLiveFor) - the five draw the policy rate's issuance edge, the USA does not.</summary>
        public void Draw(Rect rect, GUIStyle labelStyle, Country country, PolicyNodeId? pinnedPolicy, StatNodeId? pinnedStat, out PolicyNodeId? clickedPolicy, out StatNodeId? clickedStat)
        {
            Draw(rect, labelStyle, country, pinnedPolicy, pinnedStat, out clickedPolicy, out clickedStat, out _);
        }

        /// <summary>C-C3 (P-F1): the form that also reports a click on EMPTY SPACE inside the web, which
        /// is how focus is released without hunting for the focused node again. A caller that ignores it
        /// keeps the old behaviour exactly.</summary>
        public void Draw(Rect rect, GUIStyle labelStyle, Country country, PolicyNodeId? pinnedPolicy, StatNodeId? pinnedStat, out PolicyNodeId? clickedPolicy, out StatNodeId? clickedStat, out bool clickedEmptySpace)
        {
            EnsureTexturesInitialized();
            clickedPolicy = null;
            clickedStat = null;
            clickedEmptySpace = false;

            GUI.DrawTexture(rect, _backgroundTexture, ScaleMode.StretchToFill);

            GUIStyle headerStyle = GetHeaderStyle(labelStyle);
            float labelMargin = GetLabelMargin(headerStyle);
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - labelMargin - MaxNodeDiameter * 0.5f;

            _policyPixels = new Dictionary<PolicyNodeId, Vector2>();
            _statPixels = new Dictionary<StatNodeId, Vector2>();

            float[] wedgeSpans = ComputeWedgeSpans();

            float currentAngle = -90f;
            for (int w = 0; w < Wedges.Length; w++)
            {
                WedgeDef wedge = Wedges[w];
                int nodeCount = wedge.Area.HasValue ? CountInArea(wedge.Area.Value) : StatInfo.Count;
                float wedgeSpan = wedgeSpans[w];

                DrawWedgeDivider(center, radius, currentAngle);
                float midAngle = currentAngle + wedgeSpan * 0.5f;
                float wedgeLabelOffset = radius + MaxNodeDiameter * 0.5f + LabelPad;
                DrawRadialLabel(center, wedgeLabelOffset, midAngle, wedge.Label, headerStyle, rect);

                if (wedge.Area.HasValue)
                {
                    int i = 0;
                    foreach (KeyValuePair<PolicyNodeId, PolicyNodeInfo> kv in PolicyInfo)
                    {
                        if (kv.Value.Area != wedge.Area.Value) continue;
                        float nodeAngle = currentAngle + (i + 0.5f) * (wedgeSpan / nodeCount);
                        _policyPixels[kv.Key] = PointOnCircle(center, radius, nodeAngle);
                        i++;
                    }
                }
                else
                {
                    int i = 0;
                    foreach (StatNodeId stat in StatInfo.Keys)
                    {
                        float nodeAngle = currentAngle + (i + 0.5f) * (wedgeSpan / nodeCount);
                        _statPixels[stat] = PointOnCircle(center, radius, nodeAngle);
                        i++;
                    }
                }

                currentAngle += wedgeSpan + WedgeGapDegrees;
            }

            // Hit-testing happens BEFORE edge/label drawing - which edges and which node's own label
            // to draw this frame both depend on hover state, so hover must be known first.
            Vector2 mousePosition = Event.current.mousePosition;
            bool isClick = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            PolicyNodeId? hoveredPolicy = null;
            StatNodeId? hoveredStat = null;

            foreach (KeyValuePair<PolicyNodeId, Vector2> kv in _policyPixels)
            {
                float diameter = GetNodeDiameter(PolicyDegree[kv.Key]);
                var nodeRect = new Rect(kv.Value.x - diameter * 0.5f, kv.Value.y - diameter * 0.5f, diameter, diameter);
                if (nodeRect.Contains(mousePosition))
                {
                    hoveredPolicy = kv.Key;
                    if (isClick) clickedPolicy = kv.Key;
                }
            }
            foreach (KeyValuePair<StatNodeId, Vector2> kv in _statPixels)
            {
                float diameter = GetNodeDiameter(StatDegree[kv.Key]);
                var nodeRect = new Rect(kv.Value.x - diameter * 0.5f, kv.Value.y - diameter * 0.5f, diameter, diameter);
                if (nodeRect.Contains(mousePosition))
                {
                    hoveredStat = kv.Key;
                    if (isClick) clickedStat = kv.Key;
                }
            }

            // C-C3 (P-F1): a click that landed on NO node, inside the web, is the restore gesture.
            // Decided here, after every node has had its hit test, so it cannot fire on a click that
            // actually hit something.
            if (isClick && rect.Contains(mousePosition) && !clickedPolicy.HasValue && !clickedStat.HasValue)
            {
                clickedEmptySpace = true;
            }

            // Hover always wins over the pin while it's active; falls back to whichever node is
            // currently pinned (if any) once the mouse moves off every node. This single active node
            // drives both which edges draw AND which node's own label shows - intentionally the same
            // state, not tracked separately (see the class doc comment's "revealed together").
            PolicyNodeId? activePolicy;
            StatNodeId? activeStat;
            if (hoveredPolicy.HasValue) { activePolicy = hoveredPolicy; activeStat = null; }
            else if (hoveredStat.HasValue) { activeStat = hoveredStat; activePolicy = null; }
            else { activePolicy = pinnedPolicy; activeStat = pinnedStat; }

            if (activePolicy.HasValue || activeStat.HasValue)
            {
                foreach (PolicyWebEdge edge in Edges)
                {
                    bool matchesActiveNode = (activePolicy.HasValue && edge.Source == activePolicy.Value)
                        || (activeStat.HasValue && edge.Target == activeStat.Value);
                    if (!matchesActiveNode || !IsLiveFor(edge, country))
                    {
                        continue;
                    }
                    if (!_policyPixels.TryGetValue(edge.Source, out Vector2 from) || !_statPixels.TryGetValue(edge.Target, out Vector2 to))
                    {
                        continue;
                    }

                    StatNodeInfo statInfo = StatInfo[edge.Target];
                    Color lineColor = statInfo.HigherIsBetter.HasValue
                        ? UiPalette.GetDeltaColor(edge.Increases ? 1f : -1f, statInfo.HigherIsBetter.Value)
                        : UiPalette.NeutralChangeColor;

                    float t = Mathf.Clamp01(edge.RelativeStrength);
                    float thickness = Mathf.Lerp(MinLineThickness, MaxLineThickness, t);
                    // R-K1's honesty idiom: a DERIVED edge (a ledger term) is a solid line at full ink;
                    // a DECLARED one (a formula with no ledger term yet) is dashed at reduced ink - the
                    // Class A / Class D distinction the trace panels already make, on the web.
                    if (edge.Provenance == EdgeProvenance.Derived)
                    {
                        DrawLineSegment(from, to, thickness, lineColor);
                        DrawArrowHead(from, to, thickness, lineColor, GetNodeDiameter(StatDegree[edge.Target]) * 0.5f);
                    }
                    else
                    {
                        // C-C3: the head takes the DECLARED edge's own reduced ink, so PROVENANCE
                        // SURVIVES THE ARROW - a dashed line finished with a full-ink head would read
                        // as more certain than the line it sits on.
                        Color declaredInk = PoliSimTheme.Tint(lineColor, DeclaredEdgeAlpha);
                        DrawDashedLineSegment(from, to, thickness, declaredInk);
                        DrawArrowHead(from, to, thickness, declaredInk, GetNodeDiameter(StatDegree[edge.Target]) * 0.5f);
                    }
                }

                // The causal graph's own chords: stat → stat, inside the Stats wedge, drawn when
                // either end is the active stat. Always derived, so always solid.
                if (activeStat.HasValue)
                {
                    foreach (StatWebEdge edge in StatEdges)
                    {
                        if (edge.Source != activeStat.Value && edge.Target != activeStat.Value)
                        {
                            continue;
                        }
                        if (!_statPixels.TryGetValue(edge.Source, out Vector2 from) || !_statPixels.TryGetValue(edge.Target, out Vector2 to))
                        {
                            continue;
                        }

                        StatNodeInfo statInfo = StatInfo[edge.Target];
                        Color lineColor = statInfo.HigherIsBetter.HasValue
                            ? UiPalette.GetDeltaColor(edge.Increases ? 1f : -1f, statInfo.HigherIsBetter.Value)
                            : UiPalette.NeutralChangeColor;
                        float thickness = Mathf.Lerp(MinLineThickness, MaxLineThickness, Mathf.Clamp01(edge.RelativeStrength));
                        DrawLineSegment(from, to, thickness, lineColor);

                        // C-C3: a stat→stat chord is DIRECTED - "unemployment moves poverty" is not the
                        // same statement as its reverse - and these are always DERIVED, so full ink.
                        DrawArrowHead(from, to, thickness, lineColor, GetNodeDiameter(StatDegree[edge.Target]) * 0.5f);
                    }
                }
            }

            // C-C3 (P-F1): FOCUS MODE. With a node active, everything not connected to it DIMS rather
            // than vanishing - the ring's shape is what makes a wedge readable, so removing the
            // unconnected nodes would destroy the structure the focus exists to explain. The connected
            // set is read from the SAME edge lists the lines are drawn from, so the dimming and the
            // drawing cannot disagree about what "connected" means.
            //
            // ⚠ R-W2: this adds no edge, no hue and no grouping. Dimming is the EXISTING ink at a lower
            // alpha - PoliSimTheme.Tint, the same call the DECLARED edge already uses to sit back from
            // a DERIVED one - so no new colour enters the sheet and the good/bad convention is untouched.
            bool focusing = activePolicy.HasValue || activeStat.HasValue;
            HashSet<PolicyNodeId> litPolicy = null;
            HashSet<StatNodeId> litStat = null;
            if (focusing) { BuildConnectedSet(activePolicy, activeStat, country, out litPolicy, out litStat); }

            foreach (KeyValuePair<PolicyNodeId, Vector2> kv in _policyPixels)
            {
                float diameter = GetNodeDiameter(PolicyDegree[kv.Key]);
                var nodeRect = new Rect(kv.Value.x - diameter * 0.5f, kv.Value.y - diameter * 0.5f, diameter, diameter);
                Color policyInk = UiPalette.GetAreaColor(PolicyInfo[kv.Key].Area);
                if (focusing && !litPolicy.Contains(kv.Key)) { policyInk = PoliSimTheme.Tint(policyInk, UnfocusedNodeAlpha); }
                DrawCircle(nodeRect, policyInk);
            }

            foreach (KeyValuePair<StatNodeId, Vector2> kv in _statPixels)
            {
                float diameter = GetNodeDiameter(StatDegree[kv.Key]);
                var nodeRect = new Rect(kv.Value.x - diameter * 0.5f, kv.Value.y - diameter * 0.5f, diameter, diameter);
                Color statInk = focusing && !litStat.Contains(kv.Key)
                    ? PoliSimTheme.Tint(StatNodeColor, UnfocusedNodeAlpha)
                    : StatNodeColor;
                DrawCircle(nodeRect, statInk);
            }

            // The active node's own label - exactly one node's worth of text at any moment (hover
            // always resolves to at most one node; a pin with nothing hovered is also just one node),
            // so there's no stagger/collision concern the way the old always-on stat labels had. Drawn
            // last, after every dot, so it's never covered by a neighboring node.
            if (activePolicy.HasValue && _policyPixels.TryGetValue(activePolicy.Value, out Vector2 activePolicyPos))
            {
                float diameter = GetNodeDiameter(PolicyDegree[activePolicy.Value]);
                float angle = Mathf.Atan2(activePolicyPos.y - center.y, activePolicyPos.x - center.x) * Mathf.Rad2Deg;
                DrawRadialLabel(activePolicyPos, diameter * 0.5f + LabelPad, angle, PolicyInfo[activePolicy.Value].Name, labelStyle, rect);
            }
            else if (activeStat.HasValue && _statPixels.TryGetValue(activeStat.Value, out Vector2 activeStatPos))
            {
                float diameter = GetNodeDiameter(StatDegree[activeStat.Value]);
                float angle = Mathf.Atan2(activeStatPos.y - center.y, activeStatPos.x - center.x) * Mathf.Rad2Deg;
                DrawRadialLabel(activeStatPos, diameter * 0.5f + LabelPad, angle, StatInfo[activeStat.Value].Name, labelStyle, rect);
            }
        }

        /// <summary>
        /// C-C3: every node the active node actually touches, read from the SAME edge lists the focused
        /// lines are drawn from — so a node can never be lit while its edge is not drawn, or dimmed
        /// while its edge is.
        ///
        /// ⚠ **Liveness is respected exactly as the drawing respects it** (`IsLiveFor`): an edge whose
        /// policy is not implemented in this country is not drawn, so the node at its far end is not
        /// lit either. Lighting a node whose edge is absent would assert a relationship the sheet is
        /// deliberately not showing.
        /// </summary>
        private static void BuildConnectedSet(PolicyNodeId? activePolicy, StatNodeId? activeStat, Country country,
            out HashSet<PolicyNodeId> litPolicy, out HashSet<StatNodeId> litStat)
        {
            litPolicy = new HashSet<PolicyNodeId>();
            litStat = new HashSet<StatNodeId>();

            if (activePolicy.HasValue) { litPolicy.Add(activePolicy.Value); }
            if (activeStat.HasValue) { litStat.Add(activeStat.Value); }

            foreach (PolicyWebEdge edge in Edges)
            {
                bool matches = (activePolicy.HasValue && edge.Source == activePolicy.Value)
                    || (activeStat.HasValue && edge.Target == activeStat.Value);
                if (!matches || !IsLiveFor(edge, country)) { continue; }

                litPolicy.Add(edge.Source);
                litStat.Add(edge.Target);
            }

            if (!activeStat.HasValue) { return; }

            // The stat→stat half of the graph, on the same terms.
            foreach (StatWebEdge edge in StatEdges)
            {
                if (edge.Source != activeStat.Value && edge.Target != activeStat.Value) { continue; }
                litStat.Add(edge.Source);
                litStat.Add(edge.Target);
            }
        }

        /// <summary>
        /// C-C3 (P-F1): the direction arrowhead — a filled triangle at the TARGET end, so an edge reads
        /// as "this moves that" rather than as an undirected association.
        ///
        /// ⚠ **It carries the line's own colour and thickness and nothing else.** The head's size is
        /// derived from the line's thickness rather than set independently, so a weak edge gets a small
        /// head and a strong one a large head — the arrow cannot contradict the weight the line is
        /// already encoding from the coupling table.
        /// </summary>
        private void DrawArrowHead(Vector2 from, Vector2 to, float thickness, Color color, float targetRadius)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.01f) { return; }

            direction.Normalize();
            Vector2 normal = new Vector2(-direction.y, direction.x);

            // ⚠ THE TIP SITS ON THE TARGET NODE'S RIM, NOT ITS CENTRE. Edges are drawn before nodes, so
            // a head placed at the centre is painted over by the node circle and is invisible - which
            // is exactly what the first cut did, and what the 1280 film showed.
            to -= direction * targetRadius;

            float length = thickness * ArrowHeadLengthPerThickness;
            float halfWidth = thickness * ArrowHeadHalfWidthPerThickness;

            // IMGUI has no filled-triangle primitive, so the head is drawn as a short stack of
            // segments narrowing to the tip - the same "build it from the one primitive we have"
            // technique DrawCircle and DrawDashedLineSegment already use in this file.
            const int Steps = 5;
            for (int i = 0; i < Steps; i++)
            {
                float t = (i + 0.5f) / Steps;
                Vector2 mid = to - direction * (length * t);
                float halfSpan = halfWidth * t;
                DrawLineSegment(mid - normal * halfSpan, mid + normal * halfSpan, length / Steps + 0.6f, color);
            }
        }

        private static Vector2 PointOnCircle(Vector2 center, float radius, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        /// <summary>Thin radial line at a wedge boundary, from near the center out past the node ring - the "visible gap or divider between adjacent wedges" the grouping needs to read at a glance.</summary>
        private void DrawWedgeDivider(Vector2 center, float outerRadius, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 from = center + dir * (outerRadius * 0.12f);
            Vector2 to = center + dir * (outerRadius + MaxNodeDiameter * 0.5f + LabelPad);
            DrawLineSegment(from, to, 1.2f, WedgeDividerColor);
        }

        /// <summary>
        /// Places a label just outside <paramref name="origin"/> along the direction implied by
        /// <paramref name="angleDegrees"/> - used for both wedge labels (origin = circle center,
        /// offset = radius) and node labels (origin = the node's own position, offset = its own
        /// radius). Full continuous-angle text rotation isn't practical in IMGUI without extra texture
        /// work, so this uses the same "which half of the circle" binary alignment MapRenderer's own
        /// event-marker labels already use: right half (cos >= 0) left-aligns starting at the offset
        /// point, left half right-aligns ending at it, vertically centered on the point either way.
        ///
        /// <paramref name="clampBounds"/> is the diagram's own outer rect - the final drawn position is
        /// clamped inside it so a label is never clipped at the panel edge, without needing to
        /// pre-reserve permanent circle radius for it (see GetLabelMargin's own remarks on why that
        /// matters specifically for the hover/pin-only node labels, which can be considerably longer
        /// than any wedge header but only ever show one at a time).
        /// </summary>
        private static void DrawRadialLabel(Vector2 origin, float offset, float angleDegrees, string text, GUIStyle style, Rect clampBounds)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            Vector2 point = origin + new Vector2(cos, Mathf.Sin(rad)) * offset;
            Vector2 size = style.CalcSize(new GUIContent(text));

            float rectX = cos >= 0f ? point.x : point.x - size.x;
            float rectY = point.y - size.y * 0.5f;
            rectX = Mathf.Clamp(rectX, clampBounds.xMin, Mathf.Max(clampBounds.xMin, clampBounds.xMax - size.x));
            rectY = Mathf.Clamp(rectY, clampBounds.yMin, Mathf.Max(clampBounds.yMin, clampBounds.yMax - size.y));
            GUI.Label(new Rect(rectX, rectY, size.x, size.y), text, style);
        }

        public static string GetPolicyName(PolicyNodeId id) => PolicyInfo[id].Name;

        /// <summary>
        /// The curated name if this node has one, without throwing when it does not.
        ///
        /// Exposed for <see cref="DisplayName"/>, which reaches in here rather than restating these
        /// strings. They are hand-written and better than any formatter would produce:
        /// "Means-Tested Welfare" keeps its hyphen, "Veterans Benefits (Mand.)" is abbreviated to fit a
        /// column, and "UBI" survives as an acronym instead of being split into initials.
        /// </summary>
        public static bool TryGetPolicyName(PolicyNodeId id, out string name)
        {
            if (PolicyInfo.TryGetValue(id, out PolicyNodeInfo info) && !string.IsNullOrEmpty(info.Name))
            {
                name = info.Name;
                return true;
            }

            name = null;
            return false;
        }

        public static string GetPolicyDescription(PolicyNodeId id) => PolicyInfo[id].Description;
        public static string GetStatName(StatNodeId id) => StatInfo[id].Name;
        public static UiPalette.SystemArea GetPolicyArea(PolicyNodeId id) => PolicyInfo[id].Area;

        /// <summary>This stat's good/bad/neither framing. Exposed for PolicyScreenStats so each policy screen's contextual stat row colours a delta by the SAME judgment this widget uses, rather than forming a second opinion that could disagree about the same number.</summary>
        public static bool? GetStatHigherIsBetter(StatNodeId id) => StatInfo[id].HigherIsBetter;

        /// <summary>
        /// The currency unit this stat is stored in, or null if it is not money. The single source of
        /// truth for that question: a display site asks here rather than deciding locally, which is what
        /// the P2 unit bug's two failed fixes both did.
        /// </summary>
        public static MoneyUnit? GetStatUnit(StatNodeId id) => StatInfo[id].Unit;

        /// <summary>Every stat this policy node has a real edge to, for the detail popup's own effect list. <paramref name="country"/> selects the live set; null keeps the full one.</summary>
        public static List<PolicyWebEdge> GetEdgesFor(PolicyNodeId id, Country country = null)
        {
            var result = new List<PolicyWebEdge>();
            foreach (PolicyWebEdge edge in Edges)
            {
                if (edge.Source == id && IsLiveFor(edge, country)) result.Add(edge);
            }
            return result;
        }

        /// <summary>Every real edge that targets this stat, for the stat detail popup's own "affected by" list. <paramref name="country"/> selects the live set; null keeps the full one.</summary>
        public static List<PolicyWebEdge> GetEdgesForTarget(StatNodeId id, Country country = null)
        {
            var result = new List<PolicyWebEdge>();
            foreach (PolicyWebEdge edge in Edges)
            {
                if (edge.Target == id && IsLiveFor(edge, country)) result.Add(edge);
            }
            return result;
        }

        /// <summary>The panel's provenance tag for an edge - "ledger: TaxHikePenalty" for a derived edge, "declared" for a formula-asserted one.</summary>
        public static string ProvenanceTag(PolicyWebEdge edge)
            => edge.Provenance == EdgeProvenance.Derived ? $"ledger: {edge.LedgerTerm}" : "declared";

        /// <summary>This stat's StatHistory buffer, if this stat is one of the 13 already tracked there - null otherwise (5 of the 18 stat nodes here have no history, e.g. PotentialGrowthRate/ConsumerConfidence/BusinessConfidence, matching the task's own "if one exists" instruction). Continuous Time Migration Phase 0: reads .Quarterly, matching every other graph call site's resolution choice.</summary>
        public static IReadOnlyList<float> GetHistory(StatNodeId id, StatHistory history)
        {
            switch (id)
            {
                case StatNodeId.Gdp: return history.Gdp.Quarterly;
                case StatNodeId.Unemployment: return history.Unemployment.Quarterly;
                case StatNodeId.Inflation: return history.Inflation.Quarterly;
                case StatNodeId.Approval: return history.ApprovalRating.Quarterly;
                case StatNodeId.DebtToGdp: return history.DebtToGdpRatio.Quarterly;
                case StatNodeId.Poverty: return history.PovertyRate.Quarterly;
                case StatNodeId.InterestRate: return history.InterestRate.Quarterly;
                case StatNodeId.TradeBalance: return history.TradeBalance.Quarterly;
                case StatNodeId.Lfpr: return history.LaborForceParticipationRate.Quarterly;
                case StatNodeId.Crime: return history.CrimeIndex.Quarterly;
                case StatNodeId.PrisonPopulation: return history.PrisonPopulationRate.Quarterly;
                case StatNodeId.OrganizedCrime: return history.OrganizedCrimeIndex.Quarterly;
                case StatNodeId.Corruption: return history.CorruptionIndex.Quarterly;
                default: return null;
            }
        }

        /// <summary>
        /// Real, currently-computed effect numbers for the detail popup (not just direction) - each
        /// line uses the actual exposed MacroSystem sensitivity constant/method and the player
        /// country's actual current dial/rate values, not a duplicated or invented figure.
        /// </summary>
        public static List<string> GetCurrentEffectSummary(PolicyNodeId id, Country country)
        {
            var lines = new List<string>();
            // S-26 (2026-09-01): was a fourth statement of 50f; now the one statement, referenced.
            const float neutral = CrimeJusticeCouplings.NeutralDialLevel;

            switch (id)
            {
                case PolicyNodeId.MinimumWage:
                    if (!country.MinimumWageImplemented)
                    {
                        lines.Add("No statutory minimum wage in this country.");
                        break;
                    }
                    lines.Add($"Current level: {country.MinimumWagePercentOfMedian:F0}% of median wage");
                    lines.Add($"Unemployment this year: {MacroSystem.GetMinimumWageUnemploymentAdjustment(country):+0.000;-0.000} pts");
                    lines.Add($"Poverty Rate pull: {-LaborCouplings.MinimumWagePovertyReductionSensitivity * (country.MinimumWagePercentOfMedian - country.BaselineMinimumWagePercentOfMedian) / 100f:+0.00;-0.00} pts");
                    break;
                case PolicyNodeId.PaidFamilyLeave:
                    lines.Add($"Current level: {country.PaidFamilyLeaveWeeks:F0} weeks (baseline {country.BaselinePaidFamilyLeaveWeeks:F0})");
                    lines.Add($"LFPR pull (before combined ceiling): {LaborCouplings.PaidFamilyLeaveParticipationSensitivity * (country.PaidFamilyLeaveWeeks - country.BaselinePaidFamilyLeaveWeeks):+0.00;-0.00} pts");
                    lines.Add($"Approval pull: {LaborCouplings.PaidFamilyLeaveApprovalSensitivity * (country.PaidFamilyLeaveWeeks - country.BaselinePaidFamilyLeaveWeeks):+0.00;-0.00} pts");
                    break;
                case PolicyNodeId.OvertimeRegulation:
                    lines.Add($"Current level: {country.OvertimeRegulationLevel:F0}/100");
                    lines.Add($"Unemployment this year: {MacroSystem.GetOvertimeUnemploymentAdjustment(country):+0.000;-0.000} pts (contested)");
                    break;
                case PolicyNodeId.RetrainingProgram:
                    lines.Add($"Current level: {country.RetrainingProgramLevel:F0}/100");
                    lines.Add($"Unemployment this year: {MacroSystem.GetRetrainingUnemploymentAdjustment(country):+0.000;-0.000} pts");
                    lines.Add($"LFPR pull (before combined ceiling): {LaborCouplings.RetrainingParticipationSensitivity * (country.RetrainingProgramLevel - neutral):+0.00;-0.00} pts");
                    break;
                case PolicyNodeId.FamilyPolicy:
                    lines.Add($"Current level: {country.FamilyPolicyLevel:F0}/100");
                    lines.Add($"BirthRate offset: {LaborCouplings.FamilyPolicyBirthRateSensitivity * (country.FamilyPolicyLevel - neutral):+0.00;-0.00} per 1,000/yr");
                    break;
                case PolicyNodeId.ImmigrationPolicy:
                    lines.Add($"Current level: {country.ImmigrationPolicyLevel:F0}/100");
                    lines.Add($"NetMigrationRate offset: {LaborCouplings.ImmigrationPolicyNetMigrationSensitivity * (country.ImmigrationPolicyLevel - neutral):+0.00;-0.00} per 1,000/yr");
                    lines.Add($"LFPR pull (before combined ceiling): {MacroSystem.NetMigrationParticipationSensitivity * (country.State.NetMigrationRate - country.BaselineNetMigrationRate):+0.00;-0.00} pts");
                    break;
                // Item 6 (2026-08-25): the eleven crime-and-justice sensitivities below now read
                // from CrimeJusticeCouplings - the declared table the Apply* formulas themselves
                // read - a pure qualifier change, identical strings and arithmetic. Collapsing
                // these hand-listed cases into a loop over the table's edges is possible and
                // deliberately NOT done here (this pass is the extraction, not a rewrite of this
                // renderer's line grammar).
                case PolicyNodeId.PoliceFunding:
                    lines.Add($"Current level: {country.PoliceFundingLevel:F0}/100");
                    lines.Add($"Crime Index target pull: {-CrimeJusticeCouplings.PoliceFundingSensitivity * (country.PoliceFundingLevel - neutral):+0.00;-0.00} pts");
                    lines.Add($"Organized Crime target pull: {-CrimeJusticeCouplings.PoliceFundingOrganizedCrimeSensitivity * (country.PoliceFundingLevel - neutral):+0.00;-0.00} pts");
                    lines.Add($"Budget cost: {CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint * (country.PoliceFundingLevel - neutral):+0.000;-0.000} % of GDP/yr (line-resident)");
                    break;
                case PolicyNodeId.SentencingSeverity:
                    lines.Add($"Current level: {country.SentencingSeverity:F0}/100");
                    lines.Add($"Crime Index target pull: {-CrimeJusticeCouplings.SentencingSensitivity * (country.SentencingSeverity - neutral):+0.00;-0.00} pts");
                    lines.Add($"Incarceration Rate target pull: {CrimeJusticeCouplings.SentencingPrisonPopulationSensitivity * (country.SentencingSeverity - neutral):+0.00;-0.00} per 100k");
                    break;
                case PolicyNodeId.BailReform:
                    lines.Add($"Current level: {country.BailReformLevel:F0}/100");
                    lines.Add($"Crime Index target pull: {CrimeJusticeCouplings.BailReformCrimeIndexSensitivity * (country.BailReformLevel - neutral):+0.00;-0.00} pts (contested)");
                    lines.Add($"Incarceration Rate target pull: {-CrimeJusticeCouplings.BailReformPrisonPopulationSensitivity * (country.BailReformLevel - neutral):+0.00;-0.00} per 100k");
                    break;
                case PolicyNodeId.DrugPolicy:
                    lines.Add($"Current level: {country.DrugPolicyLevel:F0}/100");
                    lines.Add($"Incarceration Rate target pull: {CrimeJusticeCouplings.DrugPolicyPrisonPopulationSensitivity * (country.DrugPolicyLevel - neutral):+0.00;-0.00} per 100k");
                    lines.Add($"Approval pull: {CrimeJusticeCouplings.DrugPolicyApprovalSensitivity * (country.DrugPolicyLevel - neutral):+0.00;-0.00} pts");
                    break;
                case PolicyNodeId.JudicialFunding:
                    lines.Add($"Current level: {country.JudicialFundingLevel:F0}/100");
                    lines.Add($"Incarceration Rate target pull: {-CrimeJusticeCouplings.JudicialFundingPrisonPopulationSensitivity * (country.JudicialFundingLevel - neutral):+0.00;-0.00} per 100k");
                    lines.Add($"Organized Crime target pull: {-CrimeJusticeCouplings.JudicialFundingOrganizedCrimeSensitivity * (country.JudicialFundingLevel - neutral):+0.00;-0.00} pts");
                    lines.Add($"Corruption target pull: {-CrimeJusticeCouplings.JudicialFundingCorruptionSensitivity * (country.JudicialFundingLevel - neutral):+0.00;-0.00} pts");
                    lines.Add($"Budget cost: {CrimeJusticeCouplings.JudicialFundingBudgetCostPercentOfGdpPerPoint * (country.JudicialFundingLevel - neutral):+0.000;-0.000} % of GDP/yr (line-resident)");
                    break;
                case PolicyNodeId.BorderEnforcement:
                    lines.Add($"Current level: {country.BorderEnforcementLevel:F0}/100");
                    lines.Add($"Organized Crime target pull: {-CrimeJusticeCouplings.BorderEnforcementOrganizedCrimeSensitivity * (country.BorderEnforcementLevel - neutral):+0.00;-0.00} pts");
                    lines.Add($"Budget cost: {CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint * (country.BorderEnforcementLevel - neutral):+0.000;-0.000} % of GDP/yr (line-resident)");
                    break;
                case PolicyNodeId.SwfContributionRate:
                    lines.Add(country.SovereignWealthFund != null
                        ? $"Current contribution: {country.SovereignWealthFund.ContributionRatePercent:F1}% of GDP/year (fund: {UiFormat.Money(country.SovereignWealthFund.TotalAssets, MoneyUnit.Billions)})"
                        : "No Sovereign Wealth Fund currently exists for this country.");
                    lines.Add("DebtToGdp effect is multi-year (contribution reduces this year's Budget, then compounds via investment returns) - no single-year number to show.");
                    break;
                case PolicyNodeId.SwfAssetAllocation:
                    lines.Add(country.SovereignWealthFund != null
                        ? $"Equities {country.SovereignWealthFund.EquitiesWeight:F0}% / Bonds {country.SovereignWealthFund.BondsWeight:F0}% / Infrastructure {country.SovereignWealthFund.InfrastructureWeight:F0}% / Real Estate {country.SovereignWealthFund.RealEstateWeight:F0}%"
                        : "No Sovereign Wealth Fund currently exists for this country.");
                    lines.Add("Higher equities weight raises expected AND variance of returns - no single-year number to show.");
                    break;
                case PolicyNodeId.TariffPolicy:
                    lines.Add($"Base tariff rate: {country.BaseTariffRate:F1}%");
                    lines.Add("Tariff revenue lowers the debt ratio; a per-partner override is mirrored by that partner onto our exports (moving our own TradeBalance), and a change in the take passes through to prices for a year (pass 6).");
                    break;
                case PolicyNodeId.InterestRateDecision:
                    lines.Add($"Current rate: {country.CurrencyZone.InterestRate:F2}%");
                    lines.Add($"Points above Taylor Rule neutral real rate: {country.CurrencyZone.InterestRate - TaylorRule.NeutralRealRate:+0.00;-0.00}");
                    lines.Add("Higher-than-neutral rate directly dampens this year's Consumption/Investment (see ApplyNationalAccounts) - Unemployment/Inflation react only indirectly, afterward.");
                    break;
                case PolicyNodeId.OtherDiscretionarySpending:
                    lines.Add("Consolidated group (15 categories) - each contributes its own dollar amount to Budget/DebtToGdp, no differentiated secondary effect.");
                    break;
                default:
                    if (TryGetSectorSummary(id, country, lines)) break;
                    if (TryGetTaxSummary(id, country, lines)) break;
                    if (TryGetSpendingSummary(id, country, lines)) break;
                    if (TryGetWelfareSummary(id, country, lines)) break;
                    lines.Add("No current-effect data available for this node.");
                    break;
            }

            return lines;
        }

        private static readonly Dictionary<PolicyNodeId, TaxType> TaxNodeMap = new Dictionary<PolicyNodeId, TaxType>
        {
            { PolicyNodeId.IncomeTax, TaxType.IncomeTax }, { PolicyNodeId.CorporateTax, TaxType.CorporateTax }, { PolicyNodeId.VAT, TaxType.VAT },
            { PolicyNodeId.PayrollTax, TaxType.PayrollTax }, { PolicyNodeId.CapitalGainsTax, TaxType.CapitalGainsTax }, { PolicyNodeId.SalesTax, TaxType.SalesTax },
            { PolicyNodeId.ExciseTax, TaxType.ExciseTax }, { PolicyNodeId.PropertyTax, TaxType.PropertyTax }, { PolicyNodeId.EstateTax, TaxType.EstateTax },
            { PolicyNodeId.WealthTax, TaxType.WealthTax }, { PolicyNodeId.CarbonTax, TaxType.CarbonTax }, { PolicyNodeId.Tariffs, TaxType.Tariffs },
            { PolicyNodeId.StampDuty, TaxType.StampDuty },
        };

        private static bool TryGetTaxSummary(PolicyNodeId id, Country country, List<string> lines)
        {
            if (!TaxNodeMap.TryGetValue(id, out TaxType type)) return false;

            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type != type) continue;
                lines.Add(line.IsImplemented ? $"Current rate: {line.Rate:F1}%" : "Not currently implemented.");
                lines.Add($"Approval sensitivity to a hike: {MacroSystem.TaxHikeApprovalSensitivity:F2} pts lost per point raised this year");
                lines.Add($"Revenue base: ~{line.BaseShareOfGdp * 100f:F0}% of GDP x rate (feeds Budget/DebtToGdp)");
                return true;
            }
            lines.Add("No TaxLine of this type exists for this country.");
            return true;
        }

        private static readonly Dictionary<PolicyNodeId, SpendingCategory> SpendingNodeMap = new Dictionary<PolicyNodeId, SpendingCategory>
        {
            { PolicyNodeId.SocialSecurity, SpendingCategory.SocialSecurity }, { PolicyNodeId.Medicare, SpendingCategory.Medicare },
            { PolicyNodeId.Medicaid, SpendingCategory.Medicaid }, { PolicyNodeId.IncomeSecurity, SpendingCategory.IncomeSecurity },
            { PolicyNodeId.VeteransBenefitsMandatory, SpendingCategory.VeteransBenefitsMandatory }, { PolicyNodeId.FederalRetirement, SpendingCategory.FederalRetirement },
            { PolicyNodeId.Defense, SpendingCategory.Defense }, { PolicyNodeId.Transportation, SpendingCategory.Transportation },
            { PolicyNodeId.HHSDiscretionary, SpendingCategory.HHSDiscretionary }, { PolicyNodeId.Education, SpendingCategory.Education },
            { PolicyNodeId.Justice, SpendingCategory.Justice }, { PolicyNodeId.HomelandSecurity, SpendingCategory.HomelandSecurity },
            { PolicyNodeId.Energy, SpendingCategory.Energy }, { PolicyNodeId.Housing, SpendingCategory.Housing },
        };

        /// <summary>
        /// R-K1's two generic-line folds: a non-USA portfolio carries InfrastructureAndDevelopment
        /// where the USA carries Transportation, and HealthcareAndSocialCare where the USA carries
        /// HHSDiscretionary - never both (SimulationManager.ResolveSpendingForTurn folds the pairs
        /// into one Infrastructure/Healthcare effect the same way). The web's node is the effect,
        /// so the panel reads whichever line the country actually has.
        /// </summary>
        private static SpendingCategory? FoldTwin(SpendingCategory category)
        {
            switch (category)
            {
                case SpendingCategory.Transportation: return SpendingCategory.InfrastructureAndDevelopment;
                case SpendingCategory.HHSDiscretionary: return SpendingCategory.HealthcareAndSocialCare;
                default: return null;
            }
        }

        private static bool TryGetSpendingSummary(PolicyNodeId id, Country country, List<string> lines)
        {
            if (!SpendingNodeMap.TryGetValue(id, out SpendingCategory category)) return false;

            SpendingCategory? twin = FoldTwin(category);
            foreach (SpendingLine line in country.SpendingLines)
            {
                bool folded = twin.HasValue && line.Category == twin.Value;
                if (line.Category != category && !folded) continue;
                lines.Add($"Current amount: {UiFormat.Money(line.Amount, MoneyUnit.Billions)} ({(line.IsMandatory ? "Mandatory" : "Discretionary")})"
                          + (folded ? $" - this country's {DisplayName.Spaced(line.Category.ToString())} line, folded onto this node" : ""));
                lines.Add($"Approval multiplier: {GetApprovalMultiplier(category):F1}x (Mandatory baseline is {MacroSystem.MandatorySpendingApprovalMultiplier:F1}x, the strongest)");
                return true;
            }
            lines.Add("No SpendingLine of this category exists for this country.");
            return true;
        }

        private static float GetApprovalMultiplier(SpendingCategory category)
        {
            switch (category)
            {
                case SpendingCategory.SocialSecurity:
                case SpendingCategory.Medicare:
                case SpendingCategory.Medicaid:
                case SpendingCategory.IncomeSecurity:
                case SpendingCategory.VeteransBenefitsMandatory:
                case SpendingCategory.FederalRetirement:
                    return MacroSystem.MandatorySpendingApprovalMultiplier;
                case SpendingCategory.Defense: return MacroSystem.DefenseApprovalMultiplier;
                case SpendingCategory.Transportation: return MacroSystem.InfrastructureApprovalMultiplier;
                case SpendingCategory.HHSDiscretionary: return MacroSystem.HealthcareApprovalMultiplier;
                case SpendingCategory.Education: return MacroSystem.EducationApprovalMultiplier;
                case SpendingCategory.Justice: return MacroSystem.JusticeApprovalMultiplier;
                case SpendingCategory.HomelandSecurity: return MacroSystem.HomelandSecurityApprovalMultiplier;
                case SpendingCategory.Energy: return MacroSystem.EnergyApprovalMultiplier;
                case SpendingCategory.Housing: return MacroSystem.HousingApprovalMultiplier;
                default: return 0f;
            }
        }

        private static readonly Dictionary<PolicyNodeId, WelfareProgramType> WelfareNodeMap = new Dictionary<PolicyNodeId, WelfareProgramType>
        {
            { PolicyNodeId.UBI, WelfareProgramType.UBI }, { PolicyNodeId.NegativeIncomeTax, WelfareProgramType.NegativeIncomeTax },
            { PolicyNodeId.MeansTestedWelfare, WelfareProgramType.MeansTestedWelfare }, { PolicyNodeId.UniversalHealthcare, WelfareProgramType.UniversalHealthcare },
            { PolicyNodeId.HousingAssistance, WelfareProgramType.HousingAssistance }, { PolicyNodeId.ChildcareSubsidies, WelfareProgramType.ChildcareSubsidies },
        };

        private static bool TryGetWelfareSummary(PolicyNodeId id, Country country, List<string> lines)
        {
            if (!WelfareNodeMap.TryGetValue(id, out WelfareProgramType type)) return false;

            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (program.Type != type) continue;
                lines.Add(program.IsImplemented ? $"Current generosity: {program.GenerosityLevel:F0}%" : "Not currently implemented.");
                float poverty = MacroSystem.GetPovertyReductionSensitivity(type) * (program.IsImplemented ? program.GenerosityLevel / 100f : 0f);
                float approval = MacroSystem.GetWelfareApprovalSensitivity(type) * (program.IsImplemented ? program.GenerosityLevel / 100f : 0f);
                lines.Add($"Poverty Rate pull: {-poverty:+0.00;-0.00} pts");
                lines.Add($"Approval pull: {approval:+0.00;-0.00} pts");
                return true;
            }
            lines.Add("No WelfareProgram of this type exists for this country.");
            return true;
        }

        private static readonly Dictionary<PolicyNodeId, string> SectorDialNames = new Dictionary<PolicyNodeId, string>
        {
            { PolicyNodeId.SectorSubsidy, "Subsidy" }, { PolicyNodeId.SectorRegulation, "Regulation" }, { PolicyNodeId.SectorTaxCredit, "Tax Credit" },
            { PolicyNodeId.SectorResearchGrants, "Research Grants" }, { PolicyNodeId.SectorDeregulationNationalization, "Deregulation/Nationalization" },
        };

        private static bool TryGetSectorSummary(PolicyNodeId id, Country country, List<string> lines)
        {
            if (!SectorDialNames.TryGetValue(id, out string dialName)) return false;

            lines.Add($"{dialName} level, per sector (average across all {country.Sectors.Count}):");
            float sum = 0f;
            foreach (Sector sector in country.Sectors)
            {
                sum += id switch
                {
                    PolicyNodeId.SectorSubsidy => sector.SubsidyLevel,
                    PolicyNodeId.SectorRegulation => sector.RegulationLevel,
                    PolicyNodeId.SectorTaxCredit => sector.TaxCreditLevel,
                    PolicyNodeId.SectorResearchGrants => sector.ResearchGrantsLevel,
                    PolicyNodeId.SectorDeregulationNationalization => sector.DeregulationNationalizationLevel,
                    _ => 50f,
                };
            }
            float average = country.Sectors.Count > 0 ? sum / country.Sectors.Count : 50f;
            lines.Add($"Average: {average:F0}/100");
            return true;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _circleTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        /// <summary>The declared idiom's ink: a formula-asserted edge at 55% - visibly the lighter class beside a solid ledger-backed one, still readable on paper.</summary>
        private const float DeclaredEdgeAlpha = 0.55f;

        /// <summary>C-C3: how far back an UNCONNECTED node sits when a node is focused. Dim, never gone -
        /// the ring shape is what makes a wedge readable. Below DeclaredEdgeAlpha so a dimmed node cannot
        /// be mistaken for a DECLARED edge sitting back from a DERIVED one.</summary>
        private const float UnfocusedNodeAlpha = 0.22f;

        /// <summary>C-C3: the arrowhead is sized FROM the line thickness, so it cannot contradict the
        /// weight the line already encodes from the coupling table.</summary>
        private const float ArrowHeadLengthPerThickness = 3.2f;
        private const float ArrowHeadHalfWidthPerThickness = 1.6f;
        private const float DeclaredDashOn = 7f;
        private const float DeclaredDashOff = 5f;

        /// <summary>A dashed chord: the same rotated rect as <see cref="DrawLineSegment"/>, laid down in DeclaredDashOn / DeclaredDashOff runs along the line - the declared edge's own idiom.</summary>
        private void DrawDashedLineSegment(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) return;

            Vector2 direction = delta / length;
            float period = DeclaredDashOn + DeclaredDashOff;
            for (float start = 0f; start < length; start += period)
            {
                float run = Mathf.Min(DeclaredDashOn, length - start);
                DrawLineSegment(from + direction * start, from + direction * (start + run), thickness, color);
            }
        }

        /// <summary>Same rotated-stretched-rect technique as MapRenderer.DrawLineSegment.</summary>
        private void DrawLineSegment(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) return;

            float angleDegrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;

            GUIUtility.RotateAroundPivot(angleDegrees, from);
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, length, thickness), _lineTexture, ScaleMode.StretchToFill);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void EnsureTexturesInitialized()
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _backgroundTexture.SetPixels(new[] { BackgroundColor, BackgroundColor, BackgroundColor, BackgroundColor });
                _backgroundTexture.Apply(false);
            }
            if (_circleTexture == null)
            {
                _circleTexture = BuildCircleTexture(16);
            }
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var white = Color.white;
                _lineTexture.SetPixels(new[] { white, white, white, white });
                _lineTexture.Apply(false);
            }
        }

        private static Texture2D BuildCircleTexture(int diameter)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            var pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    pixels[y * diameter + x] = Mathf.Sqrt(dx * dx + dy * dy) <= radius ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }
    }
}
