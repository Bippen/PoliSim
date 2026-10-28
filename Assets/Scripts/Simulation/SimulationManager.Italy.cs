using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-7 (Elias's rulings K155, K204, K206): ITALY'S GOVERNMENT AFTER A COUNT - by GATE-18 q20's one formation code. The night installs nothing: it PROJECTS
    /// what the procedure is expected to form (<see cref="ProjectFormation"/>, the very mandates <see cref="ItalianFormation"/> runs, nothing stored), and the
    /// outgoing government carries on. The mandates run on the new chambers' first sitting (13 Oct 2022 for the count of 25 Sep 2022, DPR 97/2022; art. 61's
    /// twentieth day for a count the game holds - `WorldClock.ItalyFirstSittingAfter`), each government to the confidence of both chambers (art. 94); the first
    /// both grant is installed through the manager's install (its arrival budget reset, a window the player no longer governs closed, §698). After three failed
    /// mandates the government in office stays as a caretaker and the chambers are dissolved (art. 88), the new ones elected on art. 61's day
    /// (<see cref="ConfidenceProcedure.ItalianElectionDay"/>) through the extra-election order the Riksdag's procedure uses - unless the President is in the last six
    /// months of his term (art. 88 c. 2, K206), when the dissolution is dated the day the bar lifts.
    /// <para><b>The procedure's state is the world's own</b> (no save step): it is due on and after the first sitting of the latest count while the government
    /// was formed before that count, is no caretaker discharged on or after that sitting, and no extra election was ordered since it. DECLARED: every mandate is
    /// tried on the first sitting's day - the record's own intervals (the 2022 government appointed nine days on, its confidence votes three and four days after)
    /// are not dated.</para>
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>Whether Italy's mandates decide who governs after this country's election - the player's Italy (art. 94's rule).</summary>
        public bool ItalianMandatesApply(CountryId countryId) => PlayerCountryId.HasValue && PlayerCountryId.Value == countryId
            && ConfidenceProcedure.RulesOf(countryId) == ConfidenceProcedure.Rules.Parlamento;

        /// <summary>K206: the day of this world's latest Italian chamber count (a counted record), false before the game has counted one.</summary>
        private bool LastItalianCount(out System.DateTime day)
        {
            day = System.DateTime.MinValue;
            Country country = _world?.GetCountry(CountryId.Italy);
            if (country?.ElectionHistory == null) { return false; }
            foreach (ElectionRecord e in country.ElectionHistory) { if (e.Method == ElectionMethod.ItalyRosatellum && e.Date > day) { day = e.Date; } }
            return day != System.DateTime.MinValue;
        }

        /// <summary>K204: the mandates of the count of <paramref name="pollingDay"/> ran and none was granted - the government a caretaker discharged on or after that
        /// count's first sitting (what a check reads off the procedure's end, as off a concluded round).</summary>
        public bool ItalianMandatesEndedWithout(Country country, System.DateTime pollingDay)
        {
            if (country == null || !ItalianMandatesApply(country.Id) || country.Government == null) { return false; }
            System.DateTime firstSitting = WorldClock.ItalyFirstSittingAfter(pollingDay);
            return CurrentDate >= firstSitting && country.Government.Caretaker && country.Government.CaretakerSince >= firstSitting;
        }

        /// <summary>K204: the day loop's step - on and after the latest count's first sitting, the mandates run once (see the class's state rule); true on the day
        /// they ran.</summary>
        private bool AdvanceItalianMandates(Country country, GovernmentRecord g)
        {
            if (!ItalianMandatesApply(country.Id) || !LastItalianCount(out System.DateTime counted)) { return false; }
            System.DateTime firstSitting = WorldClock.ItalyFirstSittingAfter(counted);
            if (CurrentDate < firstSitting) { return false; }
            bool done = g.FormedOn > counted || (g.Caretaker && g.CaretakerSince >= firstSitting) || (_extraElectionOrderedOn != System.DateTime.MinValue && _extraElectionOrderedOn >= firstSitting);
            if (done) { return false; }
            InstallByMandates(country, DeclarationReading.OfElection(country.Id, counted));
            return true;
        }

        /// <summary>The mandates tried on the chambers as seated, today; true where a government was installed.</summary>
        private bool InstallByMandates(Country country, DeclarationReading reading)
        {
            List<ItalianFormation.Mandate> mandates = ItalianFormation.Run(country, reading, out ItalianFormation.Mandate granted);
            var lines = new List<string>();
            foreach (ItalianFormation.Mandate m in mandates) { lines.Add($"{CurrentDate:yyyy-MM-dd}: " + ItalianFormation.Line(m)); }
            if (!IsCounterfactual) { foreach (string line in lines) { Debug.Log($"MANDATE: {country.Id} - {line}"); } }
            if (granted != null)
            {
                HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
                GovernmentRecord formed = GovernmentRecord.FromView(country, granted.View, CurrentDate, world: _world);
                formed.Breaks.AddRange(lines);
                country.Government = formed;
                CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the government seats the chairs it hands the player
                ResetArrivalBudgetWindow(country.Id);
                CloseBudgetWindowIfNotGoverning(country);   // §698
                return true;
            }
            GovernmentRecord sitting = country.Government;
            if (sitting != null)
            {
                sitting.Discharge(CurrentDate);   // the record's version moves (ChamberVerdicts), CaretakerSince set
                sitting.Breaks.AddRange(lines);
            }
            bool barred = Dissolution(CurrentDate, out System.DateTime dissolvedOn, out System.DateTime _);
            ScheduleExtraElection(country, barred
                ? $"{ConfidenceProcedure.ItalianMandates} mandates failed; the President may not dissolve the chambers in the last six months of his term (art. 88 c. 2) - the caretaker governs on, and the chambers are dissolved on {dissolvedOn:yyyy-MM-dd}, the new ones elected within seventy days (art. 61)"
                : $"{ConfidenceProcedure.ItalianMandates} mandates failed - the President dissolves the chambers (Costituzione art. 88), the new ones elected within seventy days (art. 61)",
                dissolvedOn);
            return false;
        }

        /// <summary>K155, K206: the dissolution after three failed mandates on <paramref name="day"/> - dated that day, or the day the semestre bianco lifts (true) - and
        /// the polling day art. 61 gives. Reads only.</summary>
        private bool Dissolution(System.DateTime day, out System.DateTime dissolvedOn, out System.DateTime polling)
        {
            System.DateTime termFirstSitting = LastItalianCount(out System.DateTime lastCount) ? WorldClock.ItalyFirstSittingAfter(lastCount) : WorldClock.ItalyFirstSitting2022;
            bool barred = WorldClock.ItalyDissolutionBarred(day, termFirstSitting, out System.DateTime opens);
            dissolvedOn = barred ? opens : day;
            polling = ConfidenceProcedure.ItalianElectionDay(dissolvedOn);
            return barred;
        }
    }
}
