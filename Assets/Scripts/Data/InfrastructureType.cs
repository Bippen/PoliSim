namespace PoliSim.Data
{
    /// <summary>
    /// The four infrastructure categories tracked per country (Round 2's "Infrastructure system") -
    /// deliberately kept to this short list rather than the full real-world inventory (water/sewer,
    /// ports, airports, etc.), per the Round 2 brief's explicit "3-4 types, not the full original
    /// list" scope for this pass (the brief was consolidated into the master roadmap 2026-07-30;
    /// COMPLETED.md §1 is the Rounds 1-3 record).
    /// </summary>
    public enum InfrastructureType
    {
        Roads,
        Rail,
        PowerGrid,
        Broadband
    }
}
