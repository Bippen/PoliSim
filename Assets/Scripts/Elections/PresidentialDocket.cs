using System;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-21b (Elias's ruling I12 (i), 2026-10-18: "A bill unanswered after the Constitution's ten days (Sundays excepted) becomes law (Art. I §7).
    /// Poland's player-president is the same at Art. 122's 21 days, DECLARED. The pocket veto stays out of scope."): THE DOCKET'S PERIOD, one rule keyed by
    /// the country - how long a PLAYER-president has to answer a bill presented to him before it becomes law without his signature. The docket itself (the
    /// held bill, the answer, the expiry) is `SimulationManager.UsVetoGate`'s; this is the arithmetic of the day it runs out, and nothing else.
    /// <list type="bullet">
    /// <item>THE USA - Art. I §7 cl. 2 (the National Archives' transcript, `ElectionsData/usa/raw/records/archives_constitution_transcript.html`): "If any
    /// Bill shall not be returned by the President within ten Days (Sundays excepted) after it shall have been presented to him, the Same shall be a Law,
    /// in like Manner as if he had signed it". Counted as the clause counts: from the day AFTER presentment, ten days, no Sunday counted; the tenth is the
    /// last day to answer, and the bill is law the day after it.</item>
    /// <item>POLAND - Konstytucja Art. 122 ust. 2 (`ElectionsData/poland/raw/records/trybunal_konstytucja.html`): "Prezydent Rzeczypospolitej podpisuje
    /// ustawę w ciągu 21 dni od dnia przedstawienia" - 21 calendar days from the day after presentment (DECLARED: the day of the event is not counted, the
    /// Polish rule for a term in days). That an unsigned statute becomes law when they run out is the ruling's, DECLARED - the article sets the term and
    /// says nothing of its lapse. The budget act's 7 days (Art. 224 ust. 1) and the 7 days after an override (Art. 122 ust. 5) are not read here. NO POLISH
    /// PATH REACHES THIS YET: Poland has no player-president until SP-7b, and Poland's president answers by his own gate on the day
    /// (`SimulationManager.PresidentialVetoGate`); the period is proved by `UsDocketDiagnostic`.</item>
    /// </list>
    /// THE POCKET VETO IS NOT MODELLED (out of scope by the ruling): Art. I §7's "unless the Congress by their Adjournment prevent its Return, in which Case
    /// it shall not be a Law" needs an adjournment, and the game models none - so an unanswered bill is always law at the period's end. Every other country
    /// has no period (no player-president's docket).
    /// </summary>
    public static class PresidentialDocket
    {
        /// <summary>The country's period: the days counted and whether a Sunday is excepted from the count. False for a country with no docket.</summary>
        public static bool TryPeriodOf(CountryId country, out int days, out bool sundaysExcepted)
        {
            switch (country)
            {
                case CountryId.USA: days = 10; sundaysExcepted = true; return true;      // Art. I §7 cl. 2: "within ten Days (Sundays excepted)"
                case CountryId.Poland: days = 21; sundaysExcepted = false; return true;  // Art. 122 ust. 2: "w ciągu 21 dni od dnia przedstawienia"
                default: days = 0; sundaysExcepted = false; return false;
            }
        }

        /// <summary>The last day to answer a bill presented on <paramref name="presentedOn"/>: the period's last counted day, counting from the day after
        /// (a Sunday not counted where excepted). MinValue for a country with no docket.</summary>
        public static DateTime LastDay(CountryId country, DateTime presentedOn)
        {
            if (!TryPeriodOf(country, out int days, out bool sundaysExcepted)) { return DateTime.MinValue; }
            DateTime day = presentedOn.Date;
            int counted = 0;
            while (counted < days)
            {
                day = day.AddDays(1);
                if (sundaysExcepted && day.DayOfWeek == DayOfWeek.Sunday) { continue; }
                counted++;
            }
            return day;
        }

        /// <summary>Whether the period has run by <paramref name="today"/>: the bill is law without signature from the day after its last day. False for a country
        /// with no docket.</summary>
        public static bool Expired(CountryId country, DateTime presentedOn, DateTime today)
        {
            DateTime last = LastDay(country, presentedOn);
            return last != DateTime.MinValue && today.Date > last;
        }

        /// <summary>The day a bill presented on <paramref name="presentedOn"/> becomes law unanswered - the day after <see cref="LastDay"/>. MinValue for a country
        /// with no docket. PS-6 US-21c (Elias's ruling J6 (ii)): the USA's is SOURCED - P.L. 119-101, presented Monday 29 June 2026, "has become law without
        /// his signature on July 11, 2026" (`ElectionsData/usa/raw/unsigned/`), the day after Friday 10 July, the tenth day counted.</summary>
        public static DateTime LawDay(CountryId country, DateTime presentedOn)
        {
            DateTime last = LastDay(country, presentedOn);
            return last == DateTime.MinValue ? DateTime.MinValue : last.AddDays(1);
        }

        /// <summary>The period in a log's or a page's words.</summary>
        public static string PeriodWords(CountryId country)
        {
            if (!TryPeriodOf(country, out int days, out bool sundaysExcepted)) { return "no period"; }
            return country == CountryId.USA ? "ten days, Sundays excepted" : days.ToString(System.Globalization.CultureInfo.InvariantCulture) + " days" + (sundaysExcepted ? ", Sundays excepted" : string.Empty);
        }
    }
}
