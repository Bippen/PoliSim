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
    /// says nothing of its lapse. The 7 days after an override (Art. 122 ust. 5) are not read here.</item>
    /// <item>SP-7b (Elias's ruling J6 (iii), 2026-10-19: "7 days for the budget act (Art. 224(1)) and for an urgent bill (Art. 123(3)), 21 otherwise (Art.
    /// 122(2))"): POLAND'S TERMS BY THE BILL (<see cref="Bill"/>, the overloads that take one), each quoted from the Tribunal's text of the Konstytucja
    /// [TK-KONST] (`trybunal_konstytucja.html`): Art. 224 ust. 1, "Prezydent Rzeczypospolitej podpisuje w ciągu 7 dni ustawę budżetową albo ustawę o
    /// prowizorium budżetowym przedstawioną przez Marszałka Sejmu"; Art. 123 ust. 3, "termin podpisania ustawy przez Prezydenta Rzeczypospolitej wynosi 7
    /// dni" for a bill the Council of Ministers declared urgent; Art. 122 ust. 2's 21 days otherwise - and Art. 235 ust. 7's for a constitutional amendment,
    /// "Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia", the same words. The Polish docket is
    /// `SimulationManager.PolishDocketGate`'s (the file `SimulationManager.PlayerPresident.cs`); the game models no urgent procedure (Art. 123 ust. 1), so no bill is ever <see cref="Bill.Urgent"/> - its term stands here, read by nothing that
    /// plays (DECLARED).</item>
    /// </list>
    /// THE POCKET VETO IS NOT MODELLED (out of scope by the ruling): Art. I §7's "unless the Congress by their Adjournment prevent its Return, in which Case
    /// it shall not be a Law" needs an adjournment, and the game models none - so an unanswered bill is always law at the period's end. Every other country
    /// has no period (no player-president's docket).
    /// </summary>
    public static class PresidentialDocket
    {
        /// <summary>SP-7b (J6 (iii)): the kind of bill a Polish term is read by - stored on the entry as its number (`DocketEntry.Bill`; 0, ordinary, on every US
        /// bill and every entry a save from before carries).</summary>
        public enum Bill { Ordinary = 0, BudgetAct = 1, Urgent = 2, ConstitutionalAmendment = 3 }

        /// <summary>SP-7b (J6 (iii)): the country's period for a bill of <paramref name="bill"/>'s kind. Poland: 7 days for the budget act (Art. 224 ust. 1) and
        /// an urgent bill (Art. 123 ust. 3), 21 for every other (Art. 122 ust. 2; an amendment's, Art. 235 ust. 7); every other country as
        /// <see cref="TryPeriodOf(CountryId, out int, out bool)"/>, whatever the kind.</summary>
        public static bool TryPeriodOf(CountryId country, Bill bill, out int days, out bool sundaysExcepted)
        {
            if (!TryPeriodOf(country, out days, out sundaysExcepted)) { return false; }
            if (country == CountryId.Poland && (bill == Bill.BudgetAct || bill == Bill.Urgent)) { days = PolishShortTermDays; }
            return true;
        }

        /// <summary>SOURCED - Art. 224 ust. 1 ("podpisuje w ciągu 7 dni ustawę budżetową") and Art. 123 ust. 3 ("termin podpisania ustawy przez Prezydenta
        /// Rzeczypospolitej wynosi 7 dni"), [TK-KONST]: the Polish budget act's and urgent bill's term.</summary>
        public const int PolishShortTermDays = 7;

        /// <summary>SP-7b: the last day to answer a bill of <paramref name="bill"/>'s kind presented on <paramref name="presentedOn"/> (see
        /// <see cref="LastDay(CountryId, DateTime)"/> for the count). MinValue for a country with no docket.</summary>
        public static DateTime LastDay(CountryId country, DateTime presentedOn, Bill bill)
        {
            if (!TryPeriodOf(country, bill, out int days, out bool sundaysExcepted)) { return DateTime.MinValue; }
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

        /// <summary>SP-7b: the day a bill of <paramref name="bill"/>'s kind becomes law unanswered - the day after its last day. MinValue with no docket.</summary>
        public static DateTime LawDay(CountryId country, DateTime presentedOn, Bill bill)
        {
            DateTime last = LastDay(country, presentedOn, bill);
            return last == DateTime.MinValue ? DateTime.MinValue : last.AddDays(1);
        }

        /// <summary>SP-7b: a Polish term in a page's words - "21 days", "7 days (the budget act)".</summary>
        public static string PeriodWords(CountryId country, Bill bill)
        {
            if (country != CountryId.Poland || !TryPeriodOf(country, bill, out int days, out _)) { return PeriodWords(country); }
            string n = days.ToString(System.Globalization.CultureInfo.InvariantCulture) + " days";
            return bill == Bill.BudgetAct ? n + " (the budget act)" : bill == Bill.Urgent ? n + " (an urgent bill)" : n;
        }

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

        /// <summary>The day a bill presented on <paramref name="presentedOn"/> becomes law unanswered - the day after <see cref="LastDay(CountryId, DateTime)"/>. MinValue for a country
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
