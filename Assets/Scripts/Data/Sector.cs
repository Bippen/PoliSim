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

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - SubsidyLevel/RegulationLevel/the three tracked stats are all mutated during a turn, so the preview needs its own copies, not shared references.</summary>
        public Sector Clone()
        {
            return new Sector(Type, OutputShareOfGdp, EmploymentShare, SectorMetric)
            {
                BaselineOutputShareOfGdp = BaselineOutputShareOfGdp,
                BaselineEmploymentShare = BaselineEmploymentShare,
                BaselineSectorMetric = BaselineSectorMetric,
                SubsidyLevel = SubsidyLevel,
                RegulationLevel = RegulationLevel
            };
        }
    }
}
