namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5a): each country's real
    /// government fiscal-year start date (month/day only - this recurs annually, so no year field),
    /// sourced from real government fiscal-year conventions - the USA federal fiscal year starts
    /// October 1; Germany, France, Italy, Poland, and Sweden all budget on the calendar year, starting
    /// January 1. See SimulationManager.IsFiscalYearStart for how this is checked against CurrentDate,
    /// and the revised step 5 design in POLISIM_MASTER_ROADMAP.md's Part B for what fires on this date
    /// once 5c wires the actual Annual Budget bill.
    /// </summary>
    public static class FiscalYearData
    {
        public static (int Month, int Day) GetFiscalYearStart(CountryId countryId)
        {
            switch (countryId)
            {
                case CountryId.USA: return (10, 1);
                default: return (1, 1);
            }
        }
    }
}
