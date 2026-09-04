using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P4-B3 (Playtest 4, 2026-09-04): WHAT A SECTOR'S SUPPORT COSTS THE BUDGET. Until this item no sector dial touched a
    /// spending line (`MacroSystem`'s own constants say "deliberately not wired to the budget"), so the sheet's cost line
    /// would have read zero on every slider. The coupling is built the way the crime six's was
    /// (`CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint`): a percentage of GDP per dial point above the
    /// neutral 50, per sector, summed over the country's sectors into ONE target that
    /// `SimulationManager.ApplySectorSupportCostPressure` writes onto the Commerce line (else PublicServices) at each
    /// boundary through <see cref="Country.AppliedSectorSupportCost"/> - the stateless target composing with the stateful
    /// line writer, so each boundary applies only the difference. At neutral dials the target is zero and the seed's
    /// trajectory does not move. Regulation and deregulation cost nothing here: a rulebook is not a cheque.
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

        /// <summary>The country's whole support target: every sector's cost at its standing dials, summed.</summary>
        public static float SupportCostTarget(Country country)
        {
            float total = 0f;
            foreach (Sector sector in country.Sectors)
            {
                total += SupportCost(country.State.GDP, sector.SubsidyLevel, sector.TaxCreditLevel, sector.ResearchGrantsLevel);
            }
            return total;
        }
    }
}
