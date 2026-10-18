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
    /// <para>PS-6 US-22: the USA's start is SOURCED to 31 U.S.C. 1102 - the fiscal year "begins on October 1 of each year and ends on September 30 of the
    /// following year" (`ElectionsData/usa/raw/budget_statutes/govinfo_USCODE-2024-title31-sec1102.htm`, held by its SHA-256 in
    /// `UsBudgetProcedureDiagnostic`). The day the budget process opens is no longer that start for the USA (<see cref="BudgetRequestDay"/>); the credit
    /// rating's review and the release calendar keep the start.</para>
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

        /// <summary>
        /// PS-6 US-22 (`docs/specs/USA_STAGE_PLAN.md` US-22: "the president's budget request is the government's bill (31 U.S.C. 1105(a), its date read)"):
        /// THE DAY THE BUDGET PROCESS OPENS in <paramref name="year"/> - the player's annual window, and the day the AI government tables its budget.
        /// <list type="bullet">
        /// <item><b>The USA</b>: the first Monday in February - 31 U.S.C. 1105(a), "not later than the first Monday in February of each year, the President shall
        /// submit a budget", and 2 U.S.C. 631's timetable row "First Monday in February - President submits his budget" (both held by their SHA-256 under
        /// `ElectionsData/usa/raw/budget_statutes/`). A READING, DECLARED: the request is laid on the statute's last day, the timetable's own row. The days
        /// are DERIVED from the calendar; `UsBudgetProcedureDiagnostic` prints them.</item>
        /// <item><b>Every other country</b>: its fiscal-year start (<see cref="GetFiscalYearStart"/>) - the day the window opened before, byte for byte.</item>
        /// </list>
        /// </summary>
        public static System.DateTime BudgetRequestDay(CountryId countryId, int year)
        {
            if (countryId == CountryId.USA)
            {
                var first = new System.DateTime(year, 2, 1);
                return first.AddDays(((int)System.DayOfWeek.Monday - (int)first.DayOfWeek + 7) % 7);   // DERIVED: the first Monday on or after 1 February
            }
            (int month, int day) = GetFiscalYearStart(countryId);
            return new System.DateTime(year, month, day);
        }
    }
}
