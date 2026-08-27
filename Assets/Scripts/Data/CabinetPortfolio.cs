namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part A (Cabinet), Master Sequence step 1. The confirmed FULL scope
    /// is six generic portfolios (Finance/Treasury, Foreign Affairs, Defense, Interior/Justice,
    /// Health &amp; Social Affairs, Economy/Trade &amp; Industry - see COMPLETED.md §2, Part A's
    /// record), but per the original plan's content-authoring warning ("6-8 roles x 2-3 candidates x
    /// multiple decisions each is a real content burden - build 2-3 portfolios with real,
    /// fully-realized content first, mirroring the Sectors 4-&gt;8 pattern, prove the pattern feels
    /// right, then expand"), Part A implemented only the first three - chosen because each lands on a
    /// well-understood, already-audited existing channel (Fiscal Reaction Function/
    /// CollectionEfficiency, Crime &amp; Justice's gap-based target formulas, Welfare/PovertyRate's
    /// reduction terms).
    ///
    /// ROUND 4 BATCH R4-4 (2026-08-17) added Defense, ForeignAffairs and Education - the pattern
    /// proven, the expansion authored per the signed pre-report (POLISIM_R4_4_PREREPORT.md) and its
    /// seven recorded rulings. Economy/Trade &amp; Industry remains deliberately NOT defined -
    /// SectorType's own history (members added only when actually built, never as unused
    /// placeholders) is still the convention. ⚠ Passive competence is intentionally ASYMMETRIC
    /// across the six (ruling R3): Education lands on the youth-unemployment reversion target
    /// (MacroSystem.ApplyYouthUnemployment); Defense and ForeignAffairs are DECISIONS-ONLY this
    /// pass - Defense because no audited channel exists to land on (no military state, and the
    /// G-term is fiscal-engine contact the F1 sequencing keeps closed), ForeignAffairs because its
    /// candidates (TradeSystem dampening, CurrencyStrength) would need rule-11 ceiling audits the
    /// batch doesn't otherwise require. Their ministers' CompetenceBias values are authored but
    /// inert - see the CandidatePool comments in CabinetSystem.
    /// </summary>
    public enum CabinetPortfolio
    {
        FinanceTreasury,
        InteriorJustice,
        HealthSocialAffairs,

        // ROUND 4 BATCH R4-4: appended after the shipped three - append-only growth keeps every
        // serialized appointment/pending-decision in a pre-R4-4 save loading unchanged.
        Defense,
        ForeignAffairs,
        Education
    }
}
