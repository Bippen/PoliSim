using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One economic sector's tracked state: its Output (% of GDP), Employment (% of total workforce),
    /// and one sector-specific metric whose real-world meaning varies by Type (Manufacturing ->
    /// Capacity Utilization %, Technology -> a stylized Innovation Index 0-100, Agriculture -> Export
    /// Share % of sector output, Finance -> a stylized annual Credit Growth Rate %) - see WorldFactory
    /// for what's real-data-informed vs. stylized for each. All three are DESCRIPTIVE tracked stats in
    /// this first pass - they do NOT feed back into the core national accounts identity/Okun's
    /// Law/Phillips Curve (see "Economic Sectors" in CLAUDE.md for why this was a deliberate,
    /// escalated design choice, not an oversight). Each mean-reverts toward its own seeded
    /// BaselineX anchor (the same "avoid a turn-1 shock" idiom BaselinePovertyRate/BaselineCrimeIndex
    /// already use), adjusted by this sector's own SubsidyLevel/RegulationLevel - see
    /// MacroSystem.ApplySectorEffects.
    /// </summary>
    [Serializable]
    public class Sector
    {
        public SectorType Type;

        public float OutputShareOfGdp;
        public float EmploymentShare;
        public float SectorMetric;

        /// <summary>Structural per-country anchor - seeded equal to the country's own starting OutputShareOfGdp so a fresh game opens at zero gap (no effect) rather than a turn-1 shock.</summary>
        public float BaselineOutputShareOfGdp;
        public float BaselineEmploymentShare;
        public float BaselineSectorMetric;

        /// <summary>
        /// This sector's subsidy level, 0-100 (50 = neutral - a uniform placeholder for every sector/
        /// country, the same reasoning Country.PoliceFundingLevel already uses). Persistent,
        /// player-adjustable via PolicyDecision.SectorSubsidyOverrides (an absolute target, the same
        /// "SET, not delta" idiom as TaxLine.Rate) - see SimulationManager.ApplySectorPolicyChanges.
        /// Higher subsidy nudges Output/Employment/SectorMetric up; deliberately NOT wired to the
        /// budget in this pass (see "Economic Sectors" in CLAUDE.md).
        /// </summary>
        public float SubsidyLevel = 50f;

        /// <summary>
        /// This sector's regulation level, 0-100 (0 = light-touch, 100 = heavily regulated; 50 =
        /// neutral). Higher regulation nudges Output/Employment/SectorMetric down (compliance-cost
        /// tradeoff) - see MacroSystem.ApplySectorEffects.
        /// </summary>
        public float RegulationLevel = 50f;

        /// <summary>
        /// Playtest 3's seed-spread ruling (2026-08-27): this sector's regulation level AS SEEDED -
        /// the country's own anchor, the same "zero gap at seed" idiom BaselineOutputShareOfGdp uses.
        /// MacroSystem.ApplySectorEffects measures the regulation adjustment from THIS value, not
        /// from the uniform 50, because the sourced output shares already embody the country's real
        /// regulatory stringency: a heavily regulated seed is not a suppressed sector, it is the
        /// sector as measured. Seeded equal to RegulationLevel by WorldFactory.SeedSectorRegulation;
        /// a save that lacks the field loads at 50, the pre-ruling uniform anchor.
        /// </summary>
        public float BaselineRegulationLevel = 50f;

        /// <summary>Round 3 item 2: this sector's tax credit generosity, 0-100 (50 = neutral). Higher nudges Output/Employment/SectorMetric up, the same broad "boosts everything" shape as SubsidyLevel - a tax credit and a direct subsidy have a similar practical effect in this stylized model, just a different fiscal mechanism (this pass doesn't distinguish their budget treatment - see MacroSystem.ApplySectorEffects). Player-adjustable via PolicyDecision.SectorTaxCreditOverrides.</summary>
        public float TaxCreditLevel = 50f;

        /// <summary>Round 3 item 2: this sector's R&D/research funding level, 0-100 (50 = neutral). Higher nudges Output/SectorMetric up at full sensitivity but Employment up only at half sensitivity - grants fund research and output, not broad hiring, a deliberately smaller Employment effect than SubsidyLevel's (see MacroSystem.ApplySectorEffects). Player-adjustable via PolicyDecision.SectorResearchGrantsOverrides.</summary>
        public float ResearchGrantsLevel = 50f;

        /// <summary>
        /// Round 3 item 2's "Deregulation/Nationalization as a single axis" lever, 0-100 (0 = fully
        /// nationalized/state-controlled, 100 = fully deregulated/privatized, 50 = current/neutral
        /// status quo) - a genuinely DIFFERENT real-world question from RegulationLevel above
        /// (ownership structure, not regulatory stringency; a state-owned firm and a private one can
        /// each be lightly or heavily regulated in principle). The one deliberate divergence from this
        /// mechanic's otherwise-uniform-across-stats shape: higher (more deregulated/private) nudges
        /// Output/SectorMetric UP but Employment DOWN, and lower (more nationalized) does the reverse -
        /// the real, well-documented state-owned-enterprise tradeoff (privatization/deregulation
        /// typically gains efficiency by shedding excess labor; nationalization typically preserves
        /// jobs at an efficiency cost) - see MacroSystem.ApplySectorEffects. Player-adjustable via
        /// PolicyDecision.SectorDeregulationNationalizationOverrides.
        /// </summary>
        public float DeregulationNationalizationLevel = 50f;

        public Sector() { }

        public Sector(SectorType type, float outputShareOfGdp, float employmentShare, float sectorMetric)
        {
            Type = type;
            OutputShareOfGdp = outputShareOfGdp;
            EmploymentShare = employmentShare;
            SectorMetric = sectorMetric;
            BaselineOutputShareOfGdp = outputShareOfGdp;
            BaselineEmploymentShare = employmentShare;
            BaselineSectorMetric = sectorMetric;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - SubsidyLevel/RegulationLevel/TaxCreditLevel/ResearchGrantsLevel/DeregulationNationalizationLevel/the three tracked stats are all mutated during a turn, so the preview needs its own copies, not shared references.</summary>
        public Sector Clone()
        {
            return new Sector(Type, OutputShareOfGdp, EmploymentShare, SectorMetric)
            {
                BaselineOutputShareOfGdp = BaselineOutputShareOfGdp,
                BaselineEmploymentShare = BaselineEmploymentShare,
                BaselineSectorMetric = BaselineSectorMetric,
                SubsidyLevel = SubsidyLevel,
                RegulationLevel = RegulationLevel,
                BaselineRegulationLevel = BaselineRegulationLevel,
                TaxCreditLevel = TaxCreditLevel,
                ResearchGrantsLevel = ResearchGrantsLevel,
                DeregulationNationalizationLevel = DeregulationNationalizationLevel
            };
        }
    }
}
