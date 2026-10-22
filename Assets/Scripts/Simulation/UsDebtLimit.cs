using System;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-24 (`docs/specs/USA_STAGE_PLAN.md` US-24; Elias's B7, `COMPLETED.md` §759; R-US21 (i)-(vi) as ruled, I5, installed §856; J3 for a 2022-2023
    /// start, §870): THE FEDERAL DEBT LIMIT LIVE - the dated statute read through US-14's bridge, the record's extraordinary measures for one DECLARED span, and
    /// after it Congress's raise or the game's cut of spending to revenue. The pure part and the day's bookkeeping; the bill (the raise through both chambers
    /// and the President) and the arming of the cut are `SimulationManager`'s (`SimulationManager.DebtLimit.cs`).
    /// <list type="bullet">
    /// <item><b>(i) The bridge</b>: debt subject to limit is the model's debt times the ratio of Treasury's debt subject to limit to the game's seed at the seed's
    /// vintage (<see cref="BridgeRatio"/>; `docs/generated/US_DEBT_LIMIT.md`, its engine block, which `UsDebtLimitCheck.WriteBridge` writes) - nominal with
    /// nominal (the book is in current prices; the ratio is a pure number).</item>
    /// <item><b>(ii) The law in force at the start</b> (<see cref="ReadLaw"/>): the record's limit for the start's own day (J3: "the debt limit is the law in force
    /// that day"); a start inside P.L. 118-5's suspension is reinstated on its statutory day at the game's own debt subject to limit at the end of the day
    /// before - R-US21 (ii)'s words, "reinstated on 2 Jan at the game's own debt subject to limit" (no headroom added, the review's L2); every later raise is the game's own act - P.L. 119-21's increase is
    /// the record's only where the start falls after it.</item>
    /// <item><b>(iii) The measures</b>: from the day the debt subject to limit reaches the limit, extraordinary measures last <see cref="MeasuresSpanDays"/> - the
    /// record's last episode read to the first day of the month Treasury projected them exhausted (the ruling took Code's recommended reading).</item>
    /// <item><b>(iv) The cut</b> (<see cref="CutToday"/>): where the span has run and no raise is law, the day's non-interest outlays are cut pro rata to what
    /// revenue leaves after interest - armed by the day tick (`Country.DebtLimitCutFlowDay`), so only a world that runs the player's day tick can bind.</item>
    /// <item><b>(v)</b> The AI ministry's ratio-trend trigger and its borrowed sequester are retired for the USA (`AiFinanceMinistry`): this limit is the US rule.</item>
    /// <item><b>(vi) An AI-governed USA raises the limit</b>: where no Congress of the game votes (no Senate seated - the USA of every other country's game) it
    /// raises by act on the day the span runs out (<see cref="Track"/>); where the game's Congress votes, the government of the day tables the raise as a
    /// bill (`SimulationManager.DebtLimit.cs`).</item>
    /// </list>
    /// Deterministic: no random stream is drawn. The bookkeeping (<see cref="Track"/>) writes only this class's fields on the country, so a world that never
    /// arms the cut moves no figure through it.
    /// </summary>
    public static class UsDebtLimit
    {
        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // (i) the bridge
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>SOURCED - Treasury's Monthly Statement of the Public Debt, table 2: the debt subject to limit on 30 June 2026, the seed's vintage, USD
        /// (`docs/generated/US_DEBT_LIMIT.md`, its Data block's `vintage.dsl_usd`; `UsDebtLimitLiveDiagnostic` holds the two equal).</summary>
        public const double VintageSubjectToLimitUsd = 39287682920316.2;

        /// <summary>DERIVED - the game's US debt seed in USD, read from the seed itself (`WorldFactory.UsaSeedDebt`, billions, times a billion) - never
        /// transcribed, so a re-seed moves the bridge with it; the document's engine block (`UsDebtLimitCheck.WriteBridge`) prints the same figure, and
        /// `UsDebtLimitLiveDiagnostic` holds both to a fresh world.</summary>
        public static readonly double SeedDebtUsd = (double)WorldFactory.UsaSeedDebt * 1e9;

        /// <summary>DERIVED - R-US21 (i) as ruled: Treasury's debt subject to limit over the game's seed at the seed's vintage (the engine block's ratio).</summary>
        public static readonly double BridgeRatio = VintageSubjectToLimitUsd / SeedDebtUsd;

        /// <summary>The model's debt subject to limit, USD: its nominal debt (billions) through the bridge.</summary>
        public static double SubjectToLimitUsd(Country country) => country == null ? 0d : (double)country.State.GovernmentDebt * 1e9 * BridgeRatio;

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // (ii) the law of record (`docs/generated/US_DEBT_LIMIT.md`, the limit by date; `UsDebtLimitLiveDiagnostic` holds each to its Data block)
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>SOURCED - P.L. 117-73 (approved 2021-12-16): the limit then in force "is increased by $2,500,000,000,000", USD.</summary>
        public const double Pl117LimitUsd = 31381463000000.0;
        /// <summary>SOURCED - P.L. 117-73's approval day.</summary>
        public static readonly DateTime Pl117From = new DateTime(2021, 12, 16);
        /// <summary>SOURCED - P.L. 118-5 s401(a) (approved 2023-06-03): the limit suspended from its approval.</summary>
        public static readonly DateTime SuspendedFrom = new DateTime(2023, 6, 3);
        /// <summary>SOURCED - P.L. 118-5 s401(a): suspended "through January 1, 2025".</summary>
        public static readonly DateTime SuspendedThrough = new DateTime(2025, 1, 1);
        /// <summary>SOURCED - P.L. 118-5 s401(b): the limit reinstated the day after the suspension ends, in Treasury's figure, USD (the record's - a game
        /// that opens inside the suspension reinstates at its own debt, <see cref="Track"/>).</summary>
        public const double Pl118ReinstatedUsd = 36103995660231.0;
        /// <summary>SOURCED - P.L. 119-21 s72001 (approved 2025-07-04): the limit "is increased by $5,000,000,000,000", USD.</summary>
        public const double Pl119IncreaseUsd = 5000000000000.0;
        /// <summary>SOURCED - P.L. 119-21's approval day.</summary>
        public static readonly DateTime Pl119From = new DateTime(2025, 7, 4);
        /// <summary>DERIVED - the limit since P.L. 119-21: s401(b)'s figure plus s72001's increase (Treasury's 41,103,995,660,231).</summary>
        public const double Pl119LimitUsd = Pl118ReinstatedUsd + Pl119IncreaseUsd;

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // (iii) the measures, (vi) the AI's raise
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>SOURCED - the record's last episode reached the limit on 21 January 2025 (Treasury's CSRDF report of 30 January 2026, the DTS agreeing:
        /// `docs/generated/US_DEBT_LIMIT.md`, `em.reached`).</summary>
        public static readonly DateTime RecordReached = new DateTime(2025, 1, 21);
        /// <summary>SOURCED - the Secretary's letter of 9 May 2025: cash and extraordinary measures "exhausted in August" - read as its first day (`em.exhaustion`).</summary>
        public static readonly DateTime RecordExhaustionFirstDay = new DateTime(2025, 8, 1);
        /// <summary>DERIVED - R-US21 (iii) as ruled (I5, "one DECLARED span from the record's last episode"; Code's recommendation, §838): the days from the
        /// record's reach to the first day of the projected exhaustion (the document's `em.days_to_exhaustion` lower bound). The August projection is of
        /// Treasury's cash and the measures together, and the game holds no cash balance - the span counts both (the document says so).</summary>
        public static readonly int MeasuresSpanDays = (RecordExhaustionFirstDay - RecordReached).Days;

        /// <summary>[AUTHORED-DRAFT] - DECLARED, the size R-US21 (vi) leaves open (the review's L1): the raise an AI-governed USA makes, and the raise the game's
        /// Congress votes - taken as the record's last increase, P.L. 119-21 s72001's, nominal (about two and a half years of the seed's deficit); ASKED. Every episode of record ended in a raise or a suspension (CRS: "Since 1960, Congress has acted 78 separate times");
        /// a suspension, and a raise sized otherwise, are asked.</summary>
        public const double RaiseUsd = Pl119IncreaseUsd;

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // the state, read
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>Whether the limit is modelled for <paramref name="country"/>: the USA's alone.</summary>
        public static bool Applies(Country country) => country != null && country.Id == CountryId.USA;

        /// <summary>Whether the law has been read for <paramref name="country"/> (the day's bookkeeping reads it on its first day).</summary>
        public static bool IsRead(Country country) => Applies(country) && country.DebtLimitSince != DateTime.MinValue;

        /// <summary>Whether the limit is suspended on <paramref name="today"/>.</summary>
        public static bool Suspended(Country country, DateTime today) => country.DebtLimitSuspendedThrough != DateTime.MinValue && today.Date <= country.DebtLimitSuspendedThrough;

        /// <summary>The day extraordinary measures run out for an episode that reached the limit - the reach plus <see cref="MeasuresSpanDays"/>; MinValue where none runs.</summary>
        public static DateTime BindsOn(Country country) => country.DebtLimitReachedOn == DateTime.MinValue ? DateTime.MinValue : country.DebtLimitReachedOn.AddDays(MeasuresSpanDays);

        /// <summary>Whether the limit BINDS on <paramref name="today"/>: reached, and the measures' span run out with no raise law.</summary>
        public static bool Binding(Country country, DateTime today) => country.DebtLimitReachedOn != DateTime.MinValue && today.Date >= BindsOn(country);

        /// <summary>The measures' days left on <paramref name="today"/> (0 once they have run out); -1 where the limit is not reached.</summary>
        public static int MeasuresDaysLeft(Country country, DateTime today) => country.DebtLimitReachedOn == DateTime.MinValue ? -1 : Math.Max(0, (BindsOn(country) - today.Date).Days);

        /// <summary>Whether the raise is due before Congress on <paramref name="today"/> - a bill's course before the measures run out, so it is voted on that
        /// day (DECLARED), or any day after it while the limit binds (the cadence: tabled again the day after one falls).</summary>
        public static bool RaiseDue(Country country, DateTime today)
            => Applies(country) && country.DebtLimitReachedOn != DateTime.MinValue && today.Date >= BindsOn(country).AddDays(-ParliamentSystem.BillDurationDays);

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // the state, written
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// (ii) THE LAW IN FORCE AT THE START, read once: the record's limit on <paramref name="start"/> - P.L. 119-21's from its day, s401(b)'s reinstated figure
        /// from 2 January 2025, P.L. 118-5's suspension (reinstated on its day by <see cref="Track"/>), and P.L. 117-73's before it (a start before P.L. 117-73
        /// reads it too - no playable start falls there).
        /// </summary>
        public static void ReadLaw(Country country, DateTime start)
        {
            DateTime day = start.Date;
            country.DebtLimitSuspendedThrough = DateTime.MinValue;
            if (day >= Pl119From) { country.DebtLimitUsd = Pl119LimitUsd; country.DebtLimitSince = Pl119From; }
            else if (day > SuspendedThrough) { country.DebtLimitUsd = Pl118ReinstatedUsd; country.DebtLimitSince = SuspendedThrough.AddDays(1); }
            else if (day >= SuspendedFrom) { country.DebtLimitUsd = 0d; country.DebtLimitSuspendedThrough = SuspendedThrough; country.DebtLimitSince = SuspendedFrom; }
            else { country.DebtLimitUsd = Pl117LimitUsd; country.DebtLimitSince = Pl117From; }
            Debug.Log($"LIMIT: {country.Id} - the law in force at the start ({day:yyyy-MM-dd}): {LawWords(country)}");
        }

        /// <summary>
        /// THE DAY'S BOOKKEEPING, after the day's flows (`SimulationManager.AdvanceDay`, every world): the law read on the first day; a suspension ending today
        /// reinstated from tomorrow at today's close, the game's own debt (R-US21 (ii); "on 2 January" read as the end of 1 January, US-14's reading); the day the
        /// debt subject to limit reaches the limit (the measures begin) and the day it falls back under it (relieved); and, where no Congress of the game votes
        /// (<paramref name="congressVotes"/> false), the AI-governed USA's raise on the day the measures run out (R-US21 (vi)). Writes only this class's fields.
        /// </summary>
        public static void Track(Country country, DateTime today, DateTime start, bool congressVotes)
        {
            if (!Applies(country)) { return; }
            if (country.DebtLimitSince == DateTime.MinValue) { ReadLaw(country, start); }
            double dsl = SubjectToLimitUsd(country);
            if (country.DebtLimitSuspendedThrough != DateTime.MinValue)
            {
                if (today.Date < country.DebtLimitSuspendedThrough) { return; }
                country.DebtLimitUsd = dsl;
                country.DebtLimitSince = today.Date.AddDays(1);
                country.DebtLimitSuspendedThrough = DateTime.MinValue;
                Debug.Log($"LIMIT: {country.Id} - the suspension ends {today:yyyy-MM-dd}: reinstated from {country.DebtLimitSince:yyyy-MM-dd} at the game's own debt subject to limit, {Usd(dsl)}");
                return;
            }
            bool over = dsl >= country.DebtLimitUsd;
            if (country.DebtLimitReachedOn == DateTime.MinValue)
            {
                if (!over) { return; }
                country.DebtLimitReachedOn = today.Date;
                Debug.Log($"LIMIT: {country.Id} - the debt subject to limit reaches the limit on {today:yyyy-MM-dd} ({Usd(dsl)} against {Usd(country.DebtLimitUsd)}): extraordinary measures for {MeasuresSpanDays} days, to {BindsOn(country):yyyy-MM-dd}");
                return;
            }
            if (!over)
            {
                Debug.Log($"LIMIT: {country.Id} - relieved on {today:yyyy-MM-dd}: the debt subject to limit back under it ({Usd(dsl)} against {Usd(country.DebtLimitUsd)})");
                country.DebtLimitReachedOn = DateTime.MinValue;
                return;
            }
            if (!congressVotes && Binding(country, today)) { Raise(country, today, "the AI-governed USA raises it by act - no Congress of the game votes here"); }
        }

        /// <summary>A raise becomes law on <paramref name="today"/>: the limit up by <see cref="RaiseUsd"/>, the episode closed (the measures and the cut end; the
        /// next day's bookkeeping reaches again where the debt still stands over the new limit).</summary>
        public static void Raise(Country country, DateTime today, string how)
        {
            double was = country.DebtLimitUsd;
            country.DebtLimitUsd = was + RaiseUsd;
            country.DebtLimitSince = today.Date;
            country.DebtLimitReachedOn = DateTime.MinValue;
            country.DebtLimitCutFlowDay = DateTime.MinValue;   // disarmed here: a docket answer between the day steps must not leave the next day's flows cut
            country.DebtLimitRaises++;
            Debug.Log($"LIMIT: {country.Id} - raised on {today:yyyy-MM-dd} by {Usd(RaiseUsd)}, {Usd(was)} to {Usd(country.DebtLimitUsd)} ({how}; the game's raise no. {country.DebtLimitRaises}); the cut since the measures ran out: {country.DebtLimitCutBillions:0.###} bn");
            country.DebtLimitCutBillions = 0f;
        }

        /// <summary>
        /// (iv) THE CUT, on the day's flows (`SimulationManager.ApplyRevenueAndSpending`): on a day the tick armed (`Country.DebtLimitCutFlowDay`), the non-interest
        /// outlays <paramref name="nonInterest"/> are cut by what exceeds the day's revenue less its interest - one share off every one of them (pro rata, R-US21
        /// (iv)), never more than they are; interest is paid whatever the day's revenue (DECLARED: where revenue does not cover it, the rest is borrowed - the game
        /// models no default). Returns the cut (0 on every other day, so the caller's sum is exact) and keeps the episode's total; the caller books it as its own
        /// line of the fiscal period (`SimulationManager.FiscalPeriod.AccruedDebtLimitCut`, the review's M4), so the report's parts less it are its total. A
        /// lapse's back pay owed is not in <paramref name="nonInterest"/>: it stays owed while the cut is armed (`UsLapse.OutlayToday`, the review's M3). The book
        /// only: the planned purchases the macro block's identity reads, benefits' effects and approval are not moved (DECLARED - no sourced output effect of a
        /// binding limit is read; ASKED).
        /// </summary>
        public static float CutToday(Country country, DateTime today, float revenue, float interest, float nonInterest)
        {
            if (country.DebtLimitCutFlowDay != today) { return 0f; }
            float cut = Mathf.Clamp(nonInterest - Mathf.Max(0f, revenue - interest), 0f, Mathf.Max(0f, nonInterest));
            country.DebtLimitCutBillions += cut;
            return cut;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // words for the log and the bill
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The raise bill's division title - "Reconciliation (the debt limit): ..." - a title `SimulationManager.IsStatuteActTitle` reads as no standing budget.
        /// WORLD-INDEPENDENT (the review's M2): it carries the raise alone, never the world's own limit or debt, so FORK-2's identity of a passage (its title,
        /// day, chamber and origin, `SimulationManager.SamePassage`) finds the player-president's answer in every except-world; the limit it leads to is the
        /// slip's figure. Its figure is formatted HERE, in one fixed form (<see cref="RaiseWords"/>; the second review's L3): the title is FORK-2's identity and is
        /// saved in divisions and docket entries, so no change to the UI's money format may move it (`UsDebtLimitLiveDiagnostic` holds it equal to the UI's rendering).</summary>
        public static string BillTitle() => "Reconciliation (the debt limit): the limit raised by " + RaiseWords;

        /// <summary>The raise in the title's fixed form - trillions to two places, the book's unit ("US$5.00T"), the invariant culture.</summary>
        public static string RaiseWords => "US$" + (RaiseUsd / 1e12).ToString("0.00", CultureInfo.InvariantCulture) + "T";

        /// <summary>The concern both chambers vote the raise on - the budget's own concern builder over the bill (which moves no rate, line, benefit or fund, so it
        /// asks the blocs nothing), by reconciliation (2 U.S.C. 641(a)(3), DECLARED). The vote and the Laws page's card read this one.</summary>
        public static BillConcern ConcernOf(Country country, BudgetBill bill)
        {
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, bill);
            concern.Reconciliation = true;
            return concern;
        }

        /// <summary>The law in force, for the log.</summary>
        public static string LawWords(Country country)
            => country.DebtLimitSuspendedThrough != DateTime.MinValue ? "suspended through " + country.DebtLimitSuspendedThrough.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : Usd(country.DebtLimitUsd) + " since " + country.DebtLimitSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string Usd(double usd) => "$" + usd.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
