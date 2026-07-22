namespace PoliSim.Data
{
    /// <summary>
    /// The four economic sectors tracked per country (see "Economic Sectors" in CLAUDE.md) - a
    /// deliberately small proof-of-pattern slice, not the full theoretical sector list. Chosen for
    /// clear, distinct real-world profiles: Manufacturing and Agriculture are traditional,
    /// output-heavy/labor-heavy sectors with clean real World Bank data; Technology and Finance are
    /// modern, capital/skill-intensive sectors with less standardized cross-country data (see
    /// WorldFactory's seeding comment for what's real vs. stylized for each).
    /// </summary>
    public enum SectorType
    {
        Manufacturing,
        Technology,
        Agriculture,
        Finance
    }
}
