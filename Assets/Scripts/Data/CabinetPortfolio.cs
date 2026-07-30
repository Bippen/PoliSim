namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part A (Cabinet), Master Sequence step 1. The confirmed FULL scope
    /// is six generic portfolios (Finance/Treasury, Foreign Affairs, Defense, Interior/Justice,
    /// Health &amp; Social Affairs, Economy/Trade &amp; Industry - see POLISIM_MASTER_ROADMAP.md Part
    /// A), but per that document's own content-authoring warning ("6-8 roles x 2-3 candidates x
    /// multiple decisions each is a real content burden - build 2-3 portfolios with real,
    /// fully-realized content first, mirroring the Sectors 4-&gt;8 pattern, prove the pattern feels
    /// right, then expand"), only the three below are actually implemented this pass - chosen because
    /// each lands on a well-understood, already-audited existing channel (Fiscal Reaction Function/
    /// CollectionEfficiency, Crime &amp; Justice's gap-based target formulas, Welfare/PovertyRate's
    /// reduction terms). Foreign Affairs, Defense, and Economy/Trade &amp; Industry are deliberately
    /// NOT defined here yet - SectorType's own history (4 members at launch, 4 more added only when
    /// Round 3 item 4 actually built them, not defined upfront as unused placeholders) is the
    /// established convention this follows.
    /// </summary>
    public enum CabinetPortfolio
    {
        FinanceTreasury,
        InteriorJustice,
        HealthSocialAffairs
    }
}
