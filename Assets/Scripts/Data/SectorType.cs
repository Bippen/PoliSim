namespace PoliSim.Data
{
    /// <summary>
    /// The economic sectors tracked per country (see "Economic Sectors" in CLAUDE.md) - a
    /// deliberately small proof-of-pattern slice, not the full theoretical sector list. The original
    /// four were chosen for clear, distinct real-world profiles: Manufacturing and Agriculture are
    /// traditional, output-heavy/labor-heavy sectors with clean real World Bank data; Technology and
    /// Finance are modern, capital/skill-intensive sectors with less standardized cross-country data
    /// (see WorldFactory's seeding comment for what's real vs. stylized for each). Round 3 item 4
    /// added four more, the same "clear, distinct profile" reasoning: Energy and Construction are
    /// traditional, real-value-added categories with some genuinely country-differentiated real data
    /// (Poland's coal-heavy energy sector, EU-funded construction boom); Retail and Telecommunications
    /// round out the mix with a labor-intensive consumer-facing sector and a capital-intensive,
    /// low-employment one (mirroring Manufacturing/Agriculture vs. Technology/Finance's own contrast).
    /// </summary>
    public enum SectorType
    {
        Manufacturing,
        Technology,
        Agriculture,
        Finance,
        Energy,
        Construction,
        Retail,
        Telecommunications
    }
}
