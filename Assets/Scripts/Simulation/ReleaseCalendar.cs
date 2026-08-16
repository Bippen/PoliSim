using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence step 9, Step A: when each statistic is published, per country.
    ///
    /// Implemented as RULES rather than a table of dates, per the directive - the schedules come from
    /// `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 1, all marked [VERIFIED]:
    ///
    /// USA        unemployment first Friday monthly; CPI ~12th monthly;
    ///            GDP advance t+30 / second t+60 / third t+90 after quarter end.
    /// EU five    inflation flash on the last working day of the reference month, full ~t+17;
    ///            GDP flash t+30, t+45, then regular estimates ~t+65 and ~t+110.
    /// All        annual stats (poverty, population, crime) publish once yearly.
    ///
    /// Pure date arithmetic: no random draws, no simulation state read or written. That matters for
    /// Step A's acceptance bar - this file cannot move a trajectory even in principle.
    /// </summary>
    public static class ReleaseCalendar
    {
        /// <summary>True if <paramref name="stat"/> is released for <paramref name="country"/> on this exact date. Called once per simulated day.</summary>
        public static bool IsReleaseDay(Country country, PublishedStat stat, System.DateTime date)
        {
            bool isUsa = country.Id == CountryId.USA;

            switch (stat)
            {
                case PublishedStat.Unemployment:
                // ROUND 4 BATCH 1: youth unemployment releases WITH the headline figure - it is the
                // same monthly LFS/Employment-Situation family at both sources, not a separate event.
                case PublishedStat.YouthUnemployment:
                    // USA: BLS Employment Situation, first Friday. The EU five have no separate monthly
                    // unemployment rule in the seed file, so they follow the same monthly cadence rather
                    // than inventing a different one.
                    return IsFirstFridayOfMonth(date);

                case PublishedStat.Inflation:
                // ROUND 4 BATCH 2: real wages release WITH CPI for the USA (BLS Real Earnings, same
                // day - sourced via the seed doc's own recorded release date); the EU five have no
                // wage rule in the seed file and follow the family cadence, the unemployment
                // precedent exactly.
                case PublishedStat.RealWageIndex:
                    // USA CPI lands mid-month; the EU flash lands on the last working day of the month
                    // it describes, which is why European inflation reaches the player sooner.
                    return isUsa ? date.Day == 12 : IsLastWorkingDayOfMonth(date);

                case PublishedStat.Gdp:
                    // Quarterly, with multiple estimates per reference quarter - see GetGdpRevisionStage.
                    return GetGdpRevisionStage(country, date) != GdpStage.None;

                case PublishedStat.PovertyRate:
                case PublishedStat.Population:
                case PublishedStat.CrimeIndex:
                case PublishedStat.LifeExpectancy: // ROUND 4 BATCH 1: annual at both real sources
                case PublishedStat.Gini: // ROUND 4 BATCH 2: the same SILC/Census release as PovertyRate
                    // Annual cadence. Published on the country's own fiscal-year start (USA October 1,
                    // the European five January 1) rather than an invented date - that boundary already
                    // exists in this project via FiscalYearData, and is when annual figures settle.
                    (int fiscalMonth, int fiscalDay) = FiscalYearData.GetFiscalYearStart(country.Id);
                    return date.Month == fiscalMonth && date.Day == fiscalDay;

                default:
                    return false;
            }
        }

        public enum GdpStage
        {
            None,
            Advance,
            Second,
            Third
        }

        /// <summary>
        /// Which GDP estimate, if any, lands today. The USA publishes three estimates for one reference
        /// quarter (BEA advance/second/third at roughly t+30/60/90); the EU five publish a flash at t+30
        /// and a regular estimate at ~t+65, so they get two stages rather than three.
        /// </summary>
        public static GdpStage GetGdpRevisionStage(Country country, System.DateTime date)
        {
            System.DateTime quarterEnd = MostRecentQuarterEnd(date);
            int daysSince = (date - quarterEnd).Days;

            if (country.Id == CountryId.USA)
            {
                if (daysSince == 30) return GdpStage.Advance;
                if (daysSince == 60) return GdpStage.Second;
                if (daysSince == 90) return GdpStage.Third;
                return GdpStage.None;
            }

            if (daysSince == 30) return GdpStage.Advance;
            if (daysSince == 65) return GdpStage.Third;
            return GdpStage.None;
        }

        /// <summary>The reference period a figure published today actually DESCRIBES - never the publication date itself. Separating these two is the entire point of the step; conflating them would silently remove the reporting lag.</summary>
        public static void GetReferencePeriod(PublishedStat stat, System.DateTime publicationDate, out System.DateTime start, out System.DateTime end)
        {
            switch (stat)
            {
                case PublishedStat.Gdp:
                    end = MostRecentQuarterEnd(publicationDate);
                    start = end.AddMonths(-3).AddDays(1);
                    return;

                case PublishedStat.PovertyRate:
                case PublishedStat.Population:
                case PublishedStat.CrimeIndex:
                case PublishedStat.LifeExpectancy:
                case PublishedStat.Gini:
                    end = publicationDate.AddDays(-1);
                    start = end.AddYears(-1).AddDays(1);
                    return;

                default:
                    // Monthly stats describe the month BEFORE the one they are published in - the lag a
                    // player feels most often, since these are the frequently-released headline figures.
                    System.DateTime previousMonth = publicationDate.AddMonths(-1);
                    start = new System.DateTime(previousMonth.Year, previousMonth.Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    return;
            }
        }

        /// <summary>
        /// The period that <paramref name="date"/> falls INSIDE - distinct from GetReferencePeriod, which
        /// gives the period a publication on that date DESCRIBES (an earlier, already-closed one).
        ///
        /// Used to record each period's closing value as it elapses, so a later revision can converge on
        /// the value for its own reference period rather than on whatever the figure happens to be when
        /// the revision is published.
        /// </summary>
        public static System.DateTime GetCurrentPeriodStart(PublishedStat stat, System.DateTime date)
        {
            return GetCurrentPeriodStart(stat.ToClosingStat(), date);
        }

        /// <summary>
        /// Same rule, over the wider <see cref="ClosingStat"/> set. `DebtToGdpRatio` is recorded but never
        /// published, so it needs a period rule without a release rule - it uses the QUARTERLY boundary
        /// GDP uses, deliberately: Step C4's rating review derives the year's deficit from the change in
        /// the debt stock, so the two series must close on the same boundaries or the ratio would compare
        /// a debt reading to a GDP reading from a different date.
        /// </summary>
        public static System.DateTime GetCurrentPeriodStart(ClosingStat stat, System.DateTime date)
        {
            switch (stat)
            {
                case ClosingStat.Gdp:
                case ClosingStat.DebtToGdpRatio:
                    int quarterFirstMonth = ((date.Month - 1) / 3) * 3 + 1;
                    return new System.DateTime(date.Year, quarterFirstMonth, 1);

                case ClosingStat.PovertyRate:
                case ClosingStat.Population:
                case ClosingStat.CrimeIndex:
                case ClosingStat.LifeExpectancy:
                case ClosingStat.Gini:
                    return new System.DateTime(date.Year, 1, 1);

                default:
                    return new System.DateTime(date.Year, date.Month, 1);
            }
        }

        private static System.DateTime MostRecentQuarterEnd(System.DateTime date)
        {
            int lastQuarterEndMonth = ((date.Month - 1) / 3) * 3;
            if (lastQuarterEndMonth == 0)
            {
                return new System.DateTime(date.Year - 1, 12, 31);
            }

            return new System.DateTime(date.Year, lastQuarterEndMonth, System.DateTime.DaysInMonth(date.Year, lastQuarterEndMonth));
        }

        private static bool IsFirstFridayOfMonth(System.DateTime date)
        {
            return date.DayOfWeek == System.DayOfWeek.Friday && date.Day <= 7;
        }

        private static bool IsLastWorkingDayOfMonth(System.DateTime date)
        {
            if (date.DayOfWeek == System.DayOfWeek.Saturday || date.DayOfWeek == System.DayOfWeek.Sunday)
            {
                return false;
            }

            int daysInMonth = System.DateTime.DaysInMonth(date.Year, date.Month);
            for (int day = date.Day + 1; day <= daysInMonth; day++)
            {
                var later = new System.DateTime(date.Year, date.Month, day);
                if (later.DayOfWeek != System.DayOfWeek.Saturday && later.DayOfWeek != System.DayOfWeek.Sunday)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
