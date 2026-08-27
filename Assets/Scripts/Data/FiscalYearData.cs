namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5a): each country's real
    /// government fiscal-year start date (month/day only - this recurs annually, so no year field),
    /// sourced from real government fiscal-year conventions - the USA federal fiscal year starts
    /// October 1; Germany, France, Italy, Poland, and Sweden all budget on the calendar year, starting
    /// January 1. See SimulationManager.IsFiscalYearStart for how this is checked against CurrentDate,
    /// and Part B's step 5 record in COMPLETED.md §5 for what fires on this date - 5c wired the
    /// Annual Budget bill (the plan left the roadmap 2026-08-27).
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
