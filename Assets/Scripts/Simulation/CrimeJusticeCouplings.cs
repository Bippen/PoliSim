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

    /// <summary>The five tracked stats the six dials feed - the complete downstream surface.</summary>
    public enum CrimeJusticeEffectStat
    {
        CrimeIndex,
        OrganizedCrimeIndex,
        CorruptionIndex,
        PrisonPopulationRate,
        ApprovalRating
    }

    /// <summary>One dial-to-stat edge: the signed sensitivity AS APPLIED in the target formula
    /// (stat-target points per dial point above the shared neutral 50; positive raises the stat).</summary>
    public readonly struct CrimeJusticeCoupling
    {
        public readonly CrimeJusticeDial Dial;
        public readonly CrimeJusticeEffectStat Stat;
        public readonly float SignedSensitivity;
        /// <summary>True where the underlying research is honestly disputed (today: bail reform's
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
    /// The gap list this table makes visible, logged as the couplings-pass input per the ruling:
    /// SentencingSeverity feeds ONLY CrimeIndex (no prison edge); BorderEnforcement feeds ONLY
    /// OrganizedCrimeIndex; no dial touches the budget.
    /// </summary>
    public static class CrimeJusticeCouplings
    {
        /// <summary>CrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - a real, well-documented deterrence/response-capacity effect. The larger of the two policy sensitivities - see SentencingSensitivity.</summary>
        public const float PoliceFundingSensitivity = 0.16f;

        /// <summary>CrimeIndex points reduced per point Country.SentencingSeverity sits above its neutral 50 - deliberately HALF of PoliceFundingSensitivity, reflecting the well-established criminology finding (Nagin and others) that the CERTAINTY of enforcement deters crime more reliably than the SEVERITY of punishment, which has a smaller, more debated effect.</summary>
        public const float SentencingSensitivity = 0.08f;

        /// <summary>CrimeIndex points added per point Country.BailReformLevel sits above its neutral 50 (Round 2's "Deeper Crime &amp; Justice") - small and HONESTLY CONTESTED, the same "flag the real debate, don't pretend it's settled" treatment OvertimeRegulationLevel's own Unemployment effect already got: bail reform's real effect on crime is genuinely disputed in criminology research, not a settled empirical fact.</summary>
        public const float BailReformCrimeIndexSensitivity = 0.02f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - policing already fights organized crime in reality, reusing this existing lever rather than requiring a brand-new one for this specific link. Smaller than its own primary levers - a secondary contributor.</summary>
        public const float PoliceFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.BorderEnforcementLevel sits above its neutral 50 (and increased per point below) - stricter border enforcement disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core activity. The primary lever for this stat.</summary>
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

        /// <summary>The eleven edges, signed as the target formulas apply them. Rows cite the consts
        /// above - the same values the simulation reads - never restated literals.</summary>
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
            new CrimeJusticeCoupling(CrimeJusticeDial.DrugPolicy, CrimeJusticeEffectStat.ApprovalRating, DrugPolicyApprovalSensitivity),
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
                default: return stat.ToString();
            }
        }

        public static string Unit(CrimeJusticeEffectStat stat)
        {
            return stat == CrimeJusticeEffectStat.PrisonPopulationRate ? "per 100k" : "pts";
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

                if (total != 0f)
                {
                    lines.Add(new LawEffectLine(stat, total, contested));
                }
            }

            return lines;
        }
    }
}
