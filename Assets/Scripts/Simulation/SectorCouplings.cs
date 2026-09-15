using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P4-B3 (Playtest 4, 2026-09-04): WHAT A SECTOR'S SUPPORT COSTS THE BUDGET. Until this item no sector dial touched a
    /// spending line (`MacroSystem`'s own constants say "deliberately not wired to the budget"), so the sheet's cost line
    /// would have read zero on every slider. The coupling is built the way the crime six's was
    /// (`CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint`): a percentage of GDP per dial point above the
    /// neutral 50, per sector, summed over the country's sectors into ONE target that
    /// `SimulationManager.ApplySectorSupportCostPressure` writes onto the support line (<see cref="SupportLine"/> - each statute
    /// budget's Business-and-industry line, the USA's Commerce; SC-1, ruled 2026-09-15) at each boundary through
    /// <see cref="Country.AppliedSectorSupportCost"/> - the stateless target composing with the stateful line writer, so each
    /// boundary applies only the difference, outside the line's seed band (SC-1). At neutral dials the target is zero and the
    /// seed's trajectory does not move. Regulation and deregulation cost nothing here: a rulebook is not a cheque.
    /// </summary>
    public static class SectorCouplings
    {
        /// <summary>Percent of GDP a sector's Subsidy costs per point above neutral - the direct cheque, the largest of the three.</summary>
        /// <remarks>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION - sized beside the police-funding cost (0.006 per point for a whole force): a single sector's subsidy at the dial's ceiling costs 0.15 % of GDP, eight sectors at the ceiling 1.2 %; a game figure no cited study fixes.</remarks>
        public const float SubsidyBudgetCostPercentOfGdpPerPoint = 0.003f;

        /// <summary>Percent of GDP a sector's Tax Credits cost per point above neutral - revenue forgone, booked here as spending so one line carries the sector's support.</summary>
        /// <remarks>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION - two thirds of the subsidy's, since a credit is capped by the tax the sector pays; a game figure.</remarks>
        public const float TaxCreditBudgetCostPercentOfGdpPerPoint = 0.002f;

        /// <summary>Percent of GDP a sector's Research Grants cost per point above neutral - the smallest cheque of the three.</summary>
        /// <remarks>[AUTHORED-DRAFT] MAGNITUDE, documented DIRECTION - a third of the subsidy's, since grants fund projects rather than payrolls (the same reasoning that halves SectorResearchGrantsEmploymentSensitivity); a game figure.</remarks>
        public const float ResearchGrantsBudgetCostPercentOfGdpPerPoint = 0.001f;

        /// <summary>The neutral level every cost is measured from - the sector dials' own 50.</summary>
        public const float NeutralDialLevel = CrimeJusticeCouplings.NeutralDialLevel;   // CONVENTION: the dial midpoint, stated once in CrimeJusticeCouplings (S-26); a reference, not a fifth statement

        /// <summary>One sector's support cost at the given dial levels, in money (GDP × percent / 100). Negative below neutral: a sector starved below custom gives the line back.</summary>
        public static float SupportCost(float gdp, float subsidy, float taxCredits, float researchGrants)
        {
            return gdp / 100f * (
                SubsidyBudgetCostPercentOfGdpPerPoint * (subsidy - NeutralDialLevel)
              + TaxCreditBudgetCostPercentOfGdpPerPoint * (taxCredits - NeutralDialLevel)
              + ResearchGrantsBudgetCostPercentOfGdpPerPoint * (researchGrants - NeutralDialLevel));
        }

        /// <summary>The country's support target on the sector-support line (<see cref="SupportLine"/>): every sector's cost at its standing dials, summed - less the Energy
        /// sector's SUBSIDY where the book carries an energy line, which is retail intervention's money side and lands there (EN-7a,
        /// <see cref="EnergySupportCostTarget"/>).</summary>
        public static float SupportCostTarget(Country country)
        {
            float total = 0f;
            bool energyLine = HasEnergyLine(country);
            foreach (Sector sector in country.Sectors)
            {
                if (energyLine && sector.Type == SectorType.Energy)
                {
                    total += SupportCost(country.State.NominalGdp, NeutralDialLevel, sector.TaxCreditLevel, sector.ResearchGrantsLevel);   // EN-7a: the subsidy term is the energy line's
                    continue;
                }
                total += SupportCost(country.State.NominalGdp, sector.SubsidyLevel, sector.TaxCreditLevel, sector.ResearchGrantsLevel);   // §497: the line is nominal (P5-B6)
            }
            return total;
        }

        /// <summary>
        /// EN-7a (2026-09-14; S9: *"Subsidy = retail intervention's money side"*): the Energy sector's subsidy cost, on the book's energy line -
        /// the same percentage of GDP per point as every sector's subsidy, zero at the neutral 50, and zero where the book carries no energy line
        /// (Germany - the climate fund sits off the budget, so the subsidy's cost stays in <see cref="SupportCostTarget"/> with the other sectors').
        /// </summary>
        public static float EnergySupportCostTarget(Country country)
        {
            if (!HasEnergyLine(country)) { return 0f; }
            foreach (Sector sector in country.Sectors)
            {
                if (sector.Type == SectorType.Energy) { return SupportCost(country.State.NominalGdp, sector.SubsidyLevel, NeutralDialLevel, NeutralDialLevel); }
            }
            return 0f;
        }

        /// <summary>Whether the book carries an energy spending line (every covered country but Germany).</summary>
        public static bool HasEnergyLine(Country country) => EnergyLine(country) != null;

        /// <summary>EN-7a: the book's energy spending line, the one the Energy sector's subsidy lands on - or null (Germany).</summary>
        public static SpendingLine EnergyLine(Country country) => LineOf(country, SpendingCategory.Energy);

        /// <summary>The line the sector dials' support cost lands on - the one lookup the boundary's pressure, the index and the Sectors page share. SC-1
        /// (measured 2026-09-14, §498: only the USA's book carried Commerce, so in the other five the cost was booked on no line; RULED 2026-09-15, §503):
        /// each statute budget's Business-and-industry line - Sweden's UO24, Germany's Epl 09, Italy's, Poland's and France's business lines - else the
        /// USA's Commerce, else PublicServices (a generic seed's, none left).</summary>
        public static SpendingLine SupportLine(Country country) => LineOf(country, SpendingCategory.BusinessAndIndustry) ?? LineOf(country, SpendingCategory.Commerce) ?? LineOf(country, SpendingCategory.PublicServices);

        private static SpendingLine LineOf(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines) { if (line.Category == category) { return line; } }
            return null;
        }
    }
}
