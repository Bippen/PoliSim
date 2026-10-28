using System;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// MF q33 (ruled 2026-10-18 q33: "At the loss of market access a debt-crisis event fires, its costs sourced (Borensztein &amp; Panizza 2009), and the
    /// clamp no longer absorbs deficits"; K141-K144, 2026-10-27): THE DEBT CRISIS.
    /// <list type="bullet">
    /// <item><b>The trigger (K141, K143, K212)</b>: market access is lost the day a scheduled review leaves the rating at CCC (`CreditRatingSystem.ReviewIfDue`), for
    /// every country alike - the USA included, no exemption for its own currency: its trigger reads the rating without the reserve discount (K212, `SovereignCrisis.TriggerRatingOf`).</item>
    /// <item><b>The USA defaults (K142)</b>: the stock takes the sourced haircut (a dated event on the debt ledger), the economy the sourced output cost, and
    /// the country is excluded from the market for the sourced period. DECLARED: excluded, the deficit cannot be financed, so non-interest outlays are cut
    /// to revenue less interest - the form the binding US debt limit already takes (`UsDebtLimit.CutToday`); interest on what remains is paid.</item>
    /// <item><b>The EU countries take a programme (K142, K144)</b>: the euro countries an ESM-style programme, Sweden and Poland the EU's balance-of-payments
    /// facility with the IMF (Regulation (EC) No 332/2002, stored). The programme lends at the zone's rate without the market's premium for its three years;
    /// each year carries a deficit target ending at the Treaty's 3 % in the third year (the Portugal MoU of 2011 sources the three years and the 3 % end -
    /// its 5.9, 4.5 and 3.0 %); DECLARED: the path is linear, equal steps from the entry deficit, which the MoU's own steps were not; a year whose target is missed withholds the next year's disbursement, and in a withheld year the deficit is not financed - the
    /// default's cut.</item>
    /// </list>
    /// Sources under `ElectionsData/macro/raw/q32_debt/` with their SHA256SUMS and fetch log.
    /// </summary>
    public static class SovereignCrisis
    {
        /// <summary>SOURCED: the mean haircut of sovereign restructurings, Cruces and Trebesch's SZ-haircut of 37 % ("Cruces and Trebesch (2012) estimate a 40%
        /// market haircut and a 37% SZ-haircut", Tomz and Wright 2013, NBER WP 18855, stored as `nber_w18855_tomz_wright.pdf`; the AEJ: Macro article and its
        /// CESifo working paper are BILLED in the fetch log). The SZ measure (Sturzenegger and Zettelmeyer) is the loss in present value the debtor's relief
        /// is closest to, so it is the one written off the face value here.</summary>
        public const float HaircutFraction = 0.37f;

        /// <summary>SOURCED: the years a defaulter takes to re-access the market - Gelos, Sahay and Sandleris (2011) as Tomz and Wright (2013) report them: "the
        /// average country taking 4.7 years to re-access markets following a default, declining to 2.9 years more recently". DECLARED: the recent figure, the
        /// period nearest the game's world.</summary>
        public const float ExclusionYears = 2.9f;

        /// <summary>SOURCED: the default's output cost, per cent of GDP, landed over <see cref="OutputCostDays"/> - Borensztein and Panizza (2009, IMF WP 08/238,
        /// stored as `wayback_20111016200912_imf_wp08238.pdf`, the Internet Archive's capture of imf.org's file), section III, column 2: "We estimate a large
        /// effect in the first year of the default episode (with a drop in growth of 2.6 percentage points), and we find no statistically significant effect
        /// of the lagged default variables." DECLARED: the drop lands on output, not on potential, so the gap's own persistence governs what is recovered
        /// after the year - an event shock's treatment, landed at its placement (`SovereignCrisis.ApplyOutputCostDaily` beside `MacroSystem.ApplyEventShockDaily`).</summary>
        public const float OutputCostPercent = 2.6f;

        /// <summary>CONVENTION: the output cost lands over one year of days, the event shock's own span idiom (`EventSystem.ShockDurationDays` spreads a year's
        /// print the same way).</summary>
        public const int OutputCostDays = 365;

        /// <summary>SOURCED: a programme runs three years - "the three-year duration of the programme" (the Portugal Memorandum of Understanding of 17 May 2011,
        /// European Commission, stored as `ec_mou_portugal_2011-05-18.pdf`).</summary>
        public const int ProgrammeYears = 3;

        /// <summary>CONVENTION: one day as a year's share, for the exclusion's length in days.</summary>
        private const double DaysPerYear = 365.25;

        /// <summary>A crisis stands - the market is closed to the country.</summary>
        public static bool InCrisis(Country country) => country?.Crisis != null && country.Crisis.Phase != CrisisPhase.None;

        /// <summary>The programme lends: new issuance prices at the zone's rate without the market's premium (`SimulationManager.GetDebtIssuanceRate`).</summary>
        public static bool OfficialLending(Country country) => country?.Crisis != null && country.Crisis.Phase == CrisisPhase.Programme;

        /// <summary>K142, K144: the branch a country takes at the loss of market access - the USA defaults, every EU country (the euro countries, Sweden and
        /// Poland) takes the programme.</summary>
        public static CrisisPhase BranchOf(CountryId id) => id == CountryId.USA ? CrisisPhase.Default : CrisisPhase.Programme;

        /// <summary>The programme year's deficit target, per cent of GDP: the entry deficit to the Treaty's reference in <see cref="ProgrammeYears"/> steps. DECLARED (the q32
        /// review's L1): the steps are equal, a linear path - the MoU sources the three years and the 3 % end, not equal steps. An entry at or under the reference holds the reference.</summary>
        public static float TargetFor(float entryDeficitPercent, int programmeYear)
        {
            float reference = AiFinanceMinistry.EuDeficitReferencePercent;
            if (entryDeficitPercent <= reference) { return reference; }
            int year = Mathf.Clamp(programmeYear, 1, ProgrammeYears);
            return entryDeficitPercent + (reference - entryDeficitPercent) * year / ProgrammeYears;
        }

        /// <summary>
        /// The day's crisis step, run after the rating's review (`SimulationManager.AdvanceDay`): the trigger, the programme's annual review, the end of an
        /// exclusion or a programme (the output cost lands in the macro step, ApplyOutputCostDaily). Nothing for a country with no crisis and a rating above CCC.
        /// </summary>
        public static void AdvanceDay(Country country, DateTime today)
        {
            if (country == null) { return; }
            if (country.Crisis == null) { country.Crisis = new SovereignCrisisState(); }
            SovereignCrisisState crisis = country.Crisis;

            if (crisis.Phase == CrisisPhase.Programme && today.Date >= crisis.NextReviewOn.Date) { ReviewProgrammeYear(country, today); }
            if (crisis.Phase != CrisisPhase.None && today.Date > crisis.EndsOn.Date)
            {
                Debug.Log($"CRISIS: {country.Id} - {(crisis.Phase == CrisisPhase.Default ? "the exclusion from the market ends" : "the programme ends")} on {today:yyyy-MM-dd}; the rating stands at {CreditRatingSystem.Format(country.Rating.Rating)}");
                crisis.Phase = CrisisPhase.None;
                crisis.WithheldThrough = DateTime.MinValue;
            }

            // K212: the trigger's rating is re-read on each day the scheduled review set the standing one
            if (country.Rating != null && country.Rating.HasBeenReviewed && country.Rating.LastReviewDate.Date == today.Date)
            {
                crisis.TriggerRating = TriggerRatingOf(country, today);
                crisis.TriggerReviewedOn = today.Date;
            }

            // the q32 review's M1: only on a review day - an exclusion or a programme that ends between reviews leaves the market open until the next review
            // reads the country again, never re-entering on a rating up to a year old
            if (crisis.Phase == CrisisPhase.None && crisis.TriggerReviewedOn == today.Date && crisis.TriggerRating == CreditRating.CCC)
            {
                Enter(country, today);
            }
        }

        /// <summary>The default's output cost, one day's slice (the q32 review's M2): landed where FT-9 lands an event's shock - inside the macro step, after the
        /// day's GDP base is taken and before the identity, so Okun's step and the approval step read it in the day's growth (beside
        /// `MacroSystem.ApplyEventShockDaily` in `SimulationManager.AdvanceDay`). Nothing when no cost is left to land.</summary>
        public static void ApplyOutputCostDaily(Country country)
        {
            SovereignCrisisState crisis = country?.Crisis;
            if (crisis == null || crisis.OutputCostDaysLeft <= 0) { return; }
            country.State.GDP = Mathf.Max(MacroSystem.MinGdp, country.State.GDP - crisis.OutputCostPerDay);
            crisis.OutputCostDaysLeft--;
        }

        /// <summary>K212 (2026-10-28, on K143): the risk-premium sensitivity the trigger's rating reads - 1 for the USA, whose rating otherwise discounts its
        /// debt above the reference for its reserve currency (that discount stays in the standing rating, the premium and the UI); every other country's own.</summary>
        public static float TriggerSensitivity(Country country) => country.Id == CountryId.USA ? 1f : country.RiskPremiumSensitivity;

        /// <summary>K212: the rating the trigger reads today - `CreditRatingSystem.EvaluateFrom` on the review's own settled position, at <see cref="TriggerSensitivity"/>.</summary>
        public static CreditRating TriggerRatingOf(Country country, DateTime today)
        {
            CreditRatingSystem.GetSettledPosition(country, today, out float debtToGdp, out float? deficit, out float? growth);
            return CreditRatingSystem.EvaluateFrom(debtToGdp, TriggerSensitivity(country), deficit, growth).Rating;
        }

        /// <summary>The loss of market access (K141): the branch opens. The haircut is a dated event on the debt ledger - the one write to the stock off the
        /// fiscal day, recorded as the cabinet's and the fund's are.</summary>
        private static void Enter(Country country, DateTime today)
        {
            SovereignCrisisState crisis = country.Crisis;
            crisis.Phase = BranchOf(country.Id);
            crisis.StartedOn = today.Date;
            crisis.Crises++;
            if (crisis.Phase == CrisisPhase.Default)
            {
                EconomyState state = country.State;
                float before = state.GovernmentDebt;
                float writtenOff = Mathf.Max(0f, before) * HaircutFraction;
                state.GovernmentDebt = before - writtenOff;
                crisis.HaircutBillions += writtenOff;
                DebtLedgerRecorder.RecordEvent(country, today, "Sovereign default: the haircut", before, state.GovernmentDebt, "Cruces and Trebesch's mean SZ-haircut", stockWriteOff: true);   // H1: in the audit, out of the year's balance
                crisis.Haircuts.Add(new DatedAmount { Date = today.Date, Billions = writtenOff });   // H1: the rating's settled deficit adds it back
                crisis.OutputCostPerDay = state.GDP * (OutputCostPercent / 100f) / OutputCostDays;
                crisis.OutputCostDaysLeft = OutputCostDays;
                crisis.EndsOn = today.Date.AddDays(Math.Round(ExclusionYears * DaysPerYear) - 1);
                Debug.Log($"CRISIS: {country.Id} - market access lost on {today:yyyy-MM-dd} (rated CCC): DEFAULT - {writtenOff.ToString("0.###", CultureInfo.InvariantCulture)} bn written off ({HaircutFraction * 100f:0} %), excluded from the market through {crisis.EndsOn:yyyy-MM-dd}");
                return;
            }

            CreditRatingSystem.GetSettledPosition(country, today, out _, out float? deficit, out _);
            crisis.EntryDeficitPercent = deficit ?? country.Rating.ReviewedDeficitPercent ?? AiFinanceMinistry.EuDeficitReferencePercent;   // DECLARED (the q32 review's L6): with no trailing year closed yet (a programme opened at the first review) the entry deficit is unread and the target is the Treaty's 3 % from year one
            crisis.ProgrammeYear = 1;
            crisis.NextReviewOn = today.Date.AddYears(1);
            crisis.WithheldThrough = DateTime.MinValue;
            crisis.EndsOn = today.Date.AddYears(ProgrammeYears).AddDays(-1);
            Debug.Log($"CRISIS: {country.Id} - market access lost on {today:yyyy-MM-dd} (rated CCC): PROGRAMME through {crisis.EndsOn:yyyy-MM-dd} ({FacilityOf(country.Id)}), entry deficit {crisis.EntryDeficitPercent.ToString("0.0", CultureInfo.InvariantCulture)} % of GDP, the first year's target {TargetFor(crisis.EntryDeficitPercent, 1).ToString("0.0", CultureInfo.InvariantCulture)} %");
        }

        /// <summary>A programme year closes: the trailing year's deficit (the rating's settled reading) against the year's target. A miss withholds the next
        /// year's disbursement (the MoU: disbursements "subject to the compliance with the conditions of this Memorandum").</summary>
        private static void ReviewProgrammeYear(Country country, DateTime today)
        {
            SovereignCrisisState crisis = country.Crisis;
            CreditRatingSystem.GetSettledPosition(country, today, out _, out float? deficit, out _);
            float target = TargetFor(crisis.EntryDeficitPercent, crisis.ProgrammeYear);
            bool missed = deficit.HasValue && deficit.Value > target;
            if (missed)
            {
                crisis.TargetsMissed++;
                crisis.WithheldThrough = today.Date.AddYears(1).AddDays(-1);
            }
            else { crisis.WithheldThrough = DateTime.MinValue; }
            Debug.Log($"CRISIS: {country.Id} - programme year {crisis.ProgrammeYear} reviewed on {today:yyyy-MM-dd}: deficit {(deficit.HasValue ? deficit.Value.ToString("0.0", CultureInfo.InvariantCulture) : "unread")} % against a target of {target.ToString("0.0", CultureInfo.InvariantCulture)} % - {(missed ? "MISSED, the next disbursement withheld through " + crisis.WithheldThrough.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "met")}");
            crisis.ProgrammeYear++;
            crisis.NextReviewOn = today.Date.AddYears(1);
        }

        /// <summary>
        /// The cut a closed market forces (`SimulationManager.ApplyRevenueAndSpending`): in default's exclusion, and in a programme year whose disbursement
        /// was withheld, the deficit cannot be financed, so the slice's non-interest outlays are cut to its revenue less interest - `UsDebtLimit.CutToday`'s
        /// form; interest is always paid. 0 on every other day, and summed into the spending in one expression.
        /// </summary>
        public static float CutToday(Country country, DateTime today, float revenue, float interest, float nonInterest)
        {
            if (!Unfinanced(country, today)) { return 0f; }
            float cut = Mathf.Clamp(nonInterest - Mathf.Max(0f, revenue - interest), 0f, Mathf.Max(0f, nonInterest));
            country.Crisis.CutBillions += cut;
            return cut;
        }

        /// <summary>No borrowing finances the deficit today: excluded after a default, or in a programme year whose disbursement was withheld.</summary>
        public static bool Unfinanced(Country country, DateTime today)
        {
            SovereignCrisisState crisis = country?.Crisis;
            if (crisis == null) { return false; }
            if (crisis.Phase == CrisisPhase.Default) { return today.Date <= crisis.EndsOn.Date; }
            return crisis.Phase == CrisisPhase.Programme && crisis.WithheldThrough != DateTime.MinValue && today.Date <= crisis.WithheldThrough.Date;
        }

        /// <summary>K142, K144: the lender a programme is with - the euro countries the euro area's (the ESM's), Sweden and Poland the EU's balance-of-payments
        /// facility with the IMF.</summary>
        public static string FacilityOf(CountryId id) =>
            id == CountryId.Sweden || id == CountryId.Poland ? "the EU's balance-of-payments facility with the IMF" : "the euro area's stability mechanism";
    }
}
