namespace PoliSim.Data
{
    /// <summary>
    /// The four infrastructure categories tracked per country (Round 2's "Infrastructure system") -
    /// deliberately kept to this short list rather than the full real-world inventory (water/sewer,
    /// ports, airports, etc.), per ROADMAP_BRIEF.md's explicit "3-4 types, not the full original
    /// list" scope for this pass.
    /// </summary>
    public enum InfrastructureType
    {
        Roads,
        Rail,
        PowerGrid,
        Broadband
    }
}
