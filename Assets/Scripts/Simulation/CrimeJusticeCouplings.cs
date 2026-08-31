using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>The six Crime &amp; Justice dials, in LawDefinition.DialDeltas' own order.</summary>
    public enum CrimeJusticeDial
    {
        PoliceFunding,
        SentencingSeverity,
        BailReform,
        DrugPolicy,
        JudicialFunding,
        BorderEnforcement
    }

    /// <summary>The downstream surface the six dials feed: five tracked stats plus the budget-cost
    /// FLOW (the couplings pass, 2026-08-26 - enforcement costs money, line-resident; its unit is
    /// % of GDP per year, not stat points, see <see cref="CrimeJusticeCouplings.Unit"/>).</summary>
    public enum CrimeJusticeEffectStat
    {
        CrimeIndex,
        OrganizedCrimeIndex,
        CorruptionIndex,
        PrisonPopulationRate,
        ApprovalRating,
        BudgetCost
    }

    /// <summary>One dial-to-stat edge: the signed sensitivity AS APPLIED in the target formula
    /// (stat-target points per dial point above the shared neutral 50; positive raises the stat).</summary>
    public readonly struct CrimeJusticeCoupling
    {
        public readonly CrimeJusticeDial Dial;
        public readonly CrimeJusticeEffectStat Stat;
        public readonly float SignedSensitivity;
        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. True where the underlying research is honestly disputed (today: bail reform's
        /// crime effect - see the constant's own doc comment) - surfaced as "(contested)" wherever
        /// the edge is rendered, the same flag PolicyWebRenderer's dial lines already carry.</summary>
        public readonly bool Contested;

        public CrimeJusticeCoupling(CrimeJusticeDial dial, CrimeJusticeEffectStat stat, float signedSensitivity, bool contested = false)
        {
            Dial = dial;
            Stat = stat;
            SignedSensitivity = signedSensitivity;
            Contested = contested;
        }
    }

    /// <summary>
    /// THE DECLARED COUPLING TABLE (playtest-2 item 6, ruled 2026-08-25): the eleven dial-to-stat
    /// edges of the Crime &amp; Justice system, extracted from MacroSystem's scattered constants into
    /// ONE declared home that the Apply* formulas THEMSELVES read - so any text derived from this
    /// table (the law detail pane's "Expected effects", PolicyWebRenderer's dial lines) cannot
    /// drift from what the simulation actually computes. The twin-drift lesson applied to prose:
    /// a description that recomputes what the model does is a second book; a description that reads
    /// the model's own constants is the same book, quoted.
    ///
    /// ⚠ The constants moved here VERBATIM (names and values unchanged, doc comments carried) from
    /// MacroSystem - the extraction is a refactor under the trajectory byte-identity bar; a moved
    /// field means an edge changed value in transit. MacroSystem's formulas reference these
    /// qualified names directly; the <see cref="All"/> rows below cite the same consts, so table
    /// and simulation are one source by construction.
    ///
    /// THE COUPLINGS PASS (build-order item 2, terminal rulings 2026-08-26) CONSUMED the gap list
    /// this table logged as its input. Closed: SentencingSeverity now feeds PrisonPopulationRate
    /// (<see cref="SentencingPrisonPopulationSensitivity"/>, NRC-2014-anchored); the enforcement
    /// dials and the prison stock now touch the budget LINE-RESIDENT per the ruling (the
    /// BudgetCost rows below + SimulationManager.ApplyEnforcementCostPressure - costs land on the
    /// real Justice/HomelandSecurity/Migration/PublicServices spending lines and therefore feed
    /// the national-accounts G term). Declined, with the reasons recorded: BorderEnforcement's
    /// second SIM edge - the migration channel belongs to ImmigrationPolicyLevel (0.1 per point,
    /// the recorded anti-double-counting design), the single-channel scoping was itself a ruling,
    /// and the documented deterrence elasticity is modest (Angelucci 2012: -0.4..-0.8, inelastic);
    /// the dial keeps its one direct edge plus the transitive crime chain and its new budget edge -
    /// single-edge ruled HONEST, not incomplete.
    /// </summary>
    public static class CrimeJusticeCouplings
    {
        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. CrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - a real, well-documented deterrence/response-capacity effect. The larger of the two policy sensitivities - see SentencingSensitivity.</summary>
        public const float PoliceFundingSensitivity = 0.16f;

        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. CrimeIndex points reduced per point Country.SentencingSeverity sits above its neutral 50 - deliberately HALF of PoliceFundingSensitivity, reflecting the well-established criminology finding (Nagin and others) that the CERTAINTY of enforcement deters crime more reliably than the SEVERITY of punishment, which has a smaller, more debated effect.</summary>
        public const float SentencingSensitivity = 0.08f;

        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. CrimeIndex points added per point Country.BailReformLevel sits above its neutral 50 (Round 2's "Deeper Crime &amp; Justice") - small and HONESTLY CONTESTED, the same "flag the real debate, don't pretend it's settled" treatment OvertimeRegulationLevel's own Unemployment effect already got: bail reform's real effect on crime is genuinely disputed in criminology research, not a settled empirical fact.</summary>
        public const float BailReformCrimeIndexSensitivity = 0.02f;

        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. OrganizedCrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - policing already fights organized crime in reality, reusing this existing lever rather than requiring a brand-new one for this specific link. Smaller than its own primary levers - a secondary contributor.</summary>
        public const float PoliceFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION. OrganizedCrimeIndex points reduced per point Country.BorderEnforcementLevel sits above its neutral 50 (and increased per point below) - stricter border enforcement disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core activity. The primary lever for this stat.</summary>
        public const float BorderEnforcementOrganizedCrimeSensitivity = 0.12f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - better-funded prosecution capacity disrupts organized-crime networks, a real secondary contributor alongside BorderEnforcementLevel's more direct effect.</summary>
        public const float JudicialFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>CorruptionIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - an independent, well-funded judiciary is a canonical real-world anti-corruption mechanism. The sole lever for this stat in this pass.</summary>
        public const float JudicialFundingCorruptionSensitivity = 0.14f;

        /// <summary>PrisonPopulationRate points reduced per point Country.BailReformLevel sits above its neutral 50 (and added per point below) - bail reform's primary real-world goal is reducing pretrial detention, a direct and substantial real effect (pretrial detainees are a significant share of incarcerated populations, especially in the US).</summary>
        public const float BailReformPrisonPopulationSensitivity = 2.0f;

        /// <summary>PrisonPopulationRate points added per point Country.DrugPolicyLevel sits above its neutral 50 (and reduced per point below) - the well-documented real link between strict drug enforcement and mass incarceration (the US "war on drugs" being the clearest real-world example).</summary>
        public const float DrugPolicyPrisonPopulationSensitivity = 1.6f;

        /// <summary>Round 3 item 3: PrisonPopulationRate points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and added per point below) - a real, well-documented indirect effect: well-funded courts process cases faster, reducing the pretrial-detention backlog that swells incarceration in underfunded systems. Deliberately smaller than BailReformPrisonPopulationSensitivity's direct mechanical effect, since this is a secondary, capacity-driven channel, not bail policy's own primary lever.</summary>
        public const float JudicialFundingPrisonPopulationSensitivity = 0.8f;

        /// <summary>Approval points gained per point Country.DrugPolicyLevel sits above its neutral 50 (and lost per point below) - a small "tough on crime" political effect, gap versus the shared neutral 50 rather than a country-specific anchor (DrugPolicyLevel has no real per-country seed the way PaidFamilyLeaveWeeks does).</summary>
        public const float DrugPolicyApprovalSensitivity = 0.02f;

        /// <summary>The shared neutral every sensitivity above measures its gap against - the same
        /// 50 MacroSystem's own private NeutralPolicyDialLevel and PolicyWebRenderer's local carry
        /// (three statements of one fact predate this pass; unifying them is a refactor this pass
        /// deliberately does not do - this public const exists so NEW consumers stop adding a
        /// fourth).</summary>
        public const float NeutralDialLevel = 50f;

        /// <summary>
        /// THE COUPLINGS PASS (terminal ruling 2026-08-26): PrisonPopulationRate target points
        /// added per point Country.SentencingSeverity sits above its neutral 50 (and removed per
        /// point below) - the edge the pane's gap list made visible first (a Truth in Sentencing
        /// law deriving its prison effect through its bail delta alone). RULED AT PARITY with
        /// <see cref="DrugPolicyPrisonPopulationSensitivity"/> (band 1.0-2.0 recorded): the NRC's
        /// 2014 assessment of the US 1980-2010 prison-rate tripling attributes the entire growth
        /// to policy, through more admissions AND longer time served "in almost equal measure" -
        /// truth-in-sentencing statutes named by name - so the time-served channel (this dial) is
        /// the same order as the admissions channel (drug policy). The stock identity behind it
        /// (rate ~ admissions x time served) is what makes the LAGGED target shape honest: the
        /// dial moves the target, the 0.15/turn reversion fills the prisons over ~4-year
        /// half-lives, never instantly.
        /// </summary>
        public const float SentencingPrisonPopulationSensitivity = 1.6f;

        /// <summary>
        /// THE BUDGET EDGES (terminal rulings 2026-08-26, "line-resident, feeds G"): percent of
        /// GDP of recurring enforcement cost per dial point above neutral 50 - NEGATIVE below it
        /// (an under-funded regime spends less). Landed on REAL spending lines by
        /// SimulationManager.ApplyEnforcementCostPressure (USA: Justice + HomelandSecurity;
        /// Sweden: Justice/UO4 + Migration/UO8; the four generics: PublicServices), so the cost
        /// flows into the national-accounts G term through the existing discretionary-line sum -
        /// no money invented outside the line structure the recalibration just made honest.
        /// NEUTRAL-ANCHORED: at dial 50 the cost is zero, because the baseline enforcement
        /// apparatus is already inside the recalibrated seed totals; the coupling prices the
        /// DELTA from the status quo. Sizes are JUDGMENT WITH ANCHORS, banded +-50% (ruled): full
        /// +50-point swing costs Police 0.30% / Judicial 0.15% / Border 0.15% of GDP, sized
        /// between the USA's federal justice perimeter (~0.3% of GDP: DOJ ~0.14%, CBP+ICE ~0.10%,
        /// judiciary ~0.03%) and Sweden's general-government justice line (~1.6% of GDP, UO4).
        /// </summary>
        public const float PoliceFundingBudgetCostPercentOfGdpPerPoint = 0.006f;
        /// <summary>See <see cref="PoliceFundingBudgetCostPercentOfGdpPerPoint"/> - the judicial share (0.15% of GDP at full +50 swing).</summary>
        public const float JudicialFundingBudgetCostPercentOfGdpPerPoint = 0.003f;
        /// <summary>See <see cref="PoliceFundingBudgetCostPercentOfGdpPerPoint"/> - the border share (0.15% of GDP at full +50 swing).</summary>
        public const float BorderEnforcementBudgetCostPercentOfGdpPerPoint = 0.003f;

        /// <summary>
        /// The incarceration VARIABLE cost (terminal ruling 2026-08-26): GDP-per-capita units per
        /// inmate-year - one inmate above the country's own baseline rate costs one GDP-per-capita
        /// per year at 1.0. Anchors, stated as the judgment band's ends (ruled band 0.5-2.2): the
        /// US federal COIF FY2024 is $47,162/inmate-year ~ 0.5x US GDP per capita (Federal
        /// Register 2024-12-06); Sweden's Kriminalvarden runs ~EUR 117k/inmate-year ~ 2.2x Swedish
        /// GDP per capita (EuroPris 2024) - the cross-country spread is real, and 1.0 is the
        /// stated midpoint judgment, not a sourced constant. Applied to the gap
        /// (PrisonPopulationRate - BaselinePrisonPopulationRate) only - the baseline prison estate
        /// is already inside the recalibrated seed totals - which completes the honest chain:
        /// sentencing -&gt; prison stock (lagged) -&gt; budget.
        /// </summary>
        public const float IncarcerationCostGdpPerCapitaPerInmate = 1.0f;

        /// <summary>The fifteen edges, signed as the target formulas apply them. Rows cite the consts
        /// above - the same values the simulation reads - never restated literals. (Eleven at the
        /// item-6 extraction; the couplings pass added the sentencing-prison edge and the three
        /// direct budget edges, 2026-08-26.)</summary>
        public static readonly CrimeJusticeCoupling[] All =
        {
            new CrimeJusticeCoupling(CrimeJusticeDial.PoliceFunding, CrimeJusticeEffectStat.CrimeIndex, -PoliceFundingSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.SentencingSeverity, CrimeJusticeEffectStat.CrimeIndex, -SentencingSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.BailReform, CrimeJusticeEffectStat.CrimeIndex, BailReformCrimeIndexSensitivity, contested: true),
            new CrimeJusticeCoupling(CrimeJusticeDial.PoliceFunding, CrimeJusticeEffectStat.OrganizedCrimeIndex, -PoliceFundingOrganizedCrimeSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.BorderEnforcement, CrimeJusticeEffectStat.OrganizedCrimeIndex, -BorderEnforcementOrganizedCrimeSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.JudicialFunding, CrimeJusticeEffectStat.OrganizedCrimeIndex, -JudicialFundingOrganizedCrimeSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.JudicialFunding, CrimeJusticeEffectStat.CorruptionIndex, -JudicialFundingCorruptionSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.BailReform, CrimeJusticeEffectStat.PrisonPopulationRate, -BailReformPrisonPopulationSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.DrugPolicy, CrimeJusticeEffectStat.PrisonPopulationRate, DrugPolicyPrisonPopulationSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.JudicialFunding, CrimeJusticeEffectStat.PrisonPopulationRate, -JudicialFundingPrisonPopulationSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.SentencingSeverity, CrimeJusticeEffectStat.PrisonPopulationRate, SentencingPrisonPopulationSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.DrugPolicy, CrimeJusticeEffectStat.ApprovalRating, DrugPolicyApprovalSensitivity),
            new CrimeJusticeCoupling(CrimeJusticeDial.PoliceFunding, CrimeJusticeEffectStat.BudgetCost, PoliceFundingBudgetCostPercentOfGdpPerPoint),
            new CrimeJusticeCoupling(CrimeJusticeDial.JudicialFunding, CrimeJusticeEffectStat.BudgetCost, JudicialFundingBudgetCostPercentOfGdpPerPoint),
            new CrimeJusticeCoupling(CrimeJusticeDial.BorderEnforcement, CrimeJusticeEffectStat.BudgetCost, BorderEnforcementBudgetCostPercentOfGdpPerPoint),
        };

        /// <summary>Display vocabulary, shared with PolicyWebRenderer's dial lines so the two
        /// derived surfaces speak identically about the same edges.</summary>
        public static string DisplayName(CrimeJusticeEffectStat stat)
        {
            switch (stat)
            {
                case CrimeJusticeEffectStat.CrimeIndex: return "Crime Index";
                case CrimeJusticeEffectStat.OrganizedCrimeIndex: return "Organized Crime";
                case CrimeJusticeEffectStat.CorruptionIndex: return "Corruption";
                case CrimeJusticeEffectStat.PrisonPopulationRate: return "Incarceration Rate";
                case CrimeJusticeEffectStat.ApprovalRating: return "Approval";
                case CrimeJusticeEffectStat.BudgetCost: return "Budget cost";
                default: return stat.ToString();
            }
        }

        public static string Unit(CrimeJusticeEffectStat stat)
        {
            switch (stat)
            {
                case CrimeJusticeEffectStat.PrisonPopulationRate: return "per 100k";
                case CrimeJusticeEffectStat.BudgetCost: return "% of GDP/yr";
                default: return "pts";
            }
        }

        /// <summary>A law's delta on one dial - the six LawDefinition fields, addressed by enum.</summary>
        public static float DialDelta(LawDefinition law, CrimeJusticeDial dial)
        {
            switch (dial)
            {
                case CrimeJusticeDial.PoliceFunding: return law.PoliceFundingDelta;
                case CrimeJusticeDial.SentencingSeverity: return law.SentencingSeverityDelta;
                case CrimeJusticeDial.BailReform: return law.BailReformDelta;
                case CrimeJusticeDial.DrugPolicy: return law.DrugPolicyDelta;
                case CrimeJusticeDial.JudicialFunding: return law.JudicialFundingDelta;
                case CrimeJusticeDial.BorderEnforcement: return law.BorderEnforcementDelta;
                default: return 0f;
            }
        }

        /// <summary>One rendered line of a law's derived effects.</summary>
        public readonly struct LawEffectLine
        {
            public readonly CrimeJusticeEffectStat Stat;
            public readonly float Amount;
            public readonly bool Contested;

            public LawEffectLine(CrimeJusticeEffectStat stat, float amount, bool contested)
            {
                Stat = stat;
                Amount = amount;
                Contested = contested;
            }
        }

        /// <summary>
        /// The neutral derived "Expected effects" of one law (item 6's ruling): per downstream
        /// stat, the long-run TARGET shift = Σ(dial delta × signed sensitivity) over the table's
        /// edges - direction and size from the model's own constants, no authored valence. Stats
        /// the law doesn't reach are omitted (zero rows are noise, not honesty - the ledger's own
        /// rule); a stat is flagged contested when any contributing edge is. Face deltas, before
        /// the dials' own [0,100] clamp - the same basis as the delta rows above it.
        /// </summary>
        public static List<LawEffectLine> AggregateLawEffects(LawDefinition law)
        {
            var lines = new List<LawEffectLine>(5);
            foreach (CrimeJusticeEffectStat stat in System.Enum.GetValues(typeof(CrimeJusticeEffectStat)))
            {
                float total = 0f;
                bool contested = false;
                foreach (CrimeJusticeCoupling edge in All)
                {
                    if (edge.Stat != stat)
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

                // The BudgetCost line also carries the INCARCERATION TAIL (the couplings pass):
                // the law's long-run prison-target shift, priced at the ruled cost-per-inmate.
                // Recomputed here from the same prison rows the pane's own Incarceration line
                // uses - one source, quoted twice - and converted per-100k -> % of GDP at the
                // 1-GDP-per-capita coefficient (shift/100,000 people x GDPpc = shift x 0.001% of
                // GDP at 1.0). This is the honest chain on the row a player reads: a sentencing
                // law costs money THROUGH the prisons it fills, on the prison stock's own lag.
                if (stat == CrimeJusticeEffectStat.BudgetCost)
                {
                    float prisonShift = 0f;
                    foreach (CrimeJusticeCoupling edge in All)
                    {
                        if (edge.Stat != CrimeJusticeEffectStat.PrisonPopulationRate)
                        {
                            continue;
                        }

                        prisonShift += DialDelta(law, edge.Dial) * edge.SignedSensitivity;
                    }

                    total += prisonShift * IncarcerationCostGdpPerCapitaPerInmate * 0.001f;
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
