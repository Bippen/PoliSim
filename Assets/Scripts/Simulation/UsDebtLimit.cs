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

        /// <summary>[AUTHORED-DRAFT] - DECLARED, the size R-US21 (vi) leaves open (the review's L1), RULED by Elias's K21 (2026-10-23, "+$5.0 tn, but never less
        /// than the debt's excess over the limit"), RE-RULED by K77 (2026-10-23, "the raise is the excess PLUS $5.0 tn"): the size an AI-governed USA's raise and the
        /// government's own raise add to the excess - the record's last increase, P.L. 119-21 s72001's, nominal. Every raise is the day's excess plus its size, on
        /// the day it becomes law (`RaiseOf`), so it clears the excess and leaves its size as headroom. Every episode of record
        /// ended in a raise or a suspension (CRS: "Since 1960, Congress has acted 78 separate times"); a suspension is a player-president's to propose (K23).</summary>
        public const double RaiseUsd = Pl119IncreaseUsd;

        /// <summary>SOURCED - P.L. 117-73's increase, "$2,500,000,000,000" (the limit above less the one before it): the second of the two sizes of record a
        /// player-president's raise is offered at (`ProposalSizesUsd`).</summary>
        public const double Pl117IncreaseUsd = 2500000000000.0;

        /// <summary>The sizes a player-president's raise is offered at (K23; the board's stepper, 30c, read as the record's own increases - a stepper's step is
        /// not on record, DECLARED): P.L. 117-73's and P.L. 119-21's - each added to the excess on the day it is law (K77).</summary>
        public static readonly double[] ProposalSizesUsd = { Pl117IncreaseUsd, Pl119IncreaseUsd };

        /// <summary>The words a refusal of the debt-limit verbs gives, the screen's.</summary>
        public const string NoLimitWords = "THIS COUNTRY VOTES NO DEBT-LIMIT RAISE";

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

        /// <summary>PS-6 K21: the debt subject to limit's excess over the limit in force, USD - 0 below it and while suspended.</summary>
        public static double ExcessUsd(Country country)
            => country == null || country.DebtLimitSuspendedThrough != DateTime.MinValue ? 0d : Math.Max(0d, SubjectToLimitUsd(country) - country.DebtLimitUsd);

        /// <summary>PS-6 K77 (Elias's ruling of 2026-10-23, replacing K21's floor: "the raise is the excess PLUS $5.0 tn, so a raise clears the excess and leaves the
        /// record's $5 tn headroom; the 2022/2023 second episode ends"): the raise a bill of <paramref name="proposedUsd"/> makes today - the day's excess
        /// (`ExcessUsd`) plus the size (`RaiseUsd` where 0), so the limit lands that size above the day's debt subject to limit.</summary>
        public static double RaiseOf(Country country, double proposedUsd) => ExcessUsd(country) + (proposedUsd > 0d ? proposedUsd : RaiseUsd);

        /// <summary>PS-6 K43 (Elias's ruling: "after a fallen or withheld raise the player may propose again, voted the day it is tabled"): whether a raise tabled
        /// on <paramref name="today"/> is voted that day - once the measures have run out (DECLARED: a first proposal after the deadline too; before it, K25's
        /// vote at the deadline).</summary>
        public static bool VotedTheDayTabled(Country country, DateTime today) => Binding(country, today);

        /// <summary>PS-6 K25/K43 (the review's M2): the day a raise or suspension proposed on <paramref name="today"/> is voted - that day once the measures have
        /// run out, else the day they run out. A suspension's last day must come after it.</summary>
        public static DateTime VoteDayOf(Country country, DateTime today) => VotedTheDayTabled(country, today) ? today.Date : BindsOn(country).Date;

        /// <summary>PS-6 K23 (Elias's ruling: "a player-president gets a verb: propose the raise, its amount or a suspension"): whether
        /// <paramref name="presidentsParty"/>'s player may propose one today - the player's party holds the presidency (<paramref name="playerPresident"/>), the
        /// game's Congress votes, the limit is reached and not suspended, and no raise is before Congress or on the desk (<paramref name="pending"/>); a raise of
        /// a positive size, or a suspension to a day after today. False with the screen's words where refused.</summary>
        public static bool MayPropose(Country country, bool playerPresident, bool congressVotes, BudgetBill pending, DateTime today, double raiseUsd, DateTime suspendThrough, out string why)
        {
            why = null;
            if (!IsRead(country) || !congressVotes) { why = NoLimitWords; return false; }
            if (!playerPresident) { why = "ONLY THE PRESIDENT PROPOSES THE RAISE"; return false; }
            if (Suspended(country, today)) { why = "THE LIMIT IS SUSPENDED"; return false; }
            if (country.DebtLimitReachedOn == DateTime.MinValue) { why = "THE DEBT IS BELOW THE LIMIT"; return false; }
            if (pending != null) { why = pending.Docket != null ? "THE RAISE WAITS ON YOUR DESK" : "A RAISE IS BEFORE CONGRESS ALREADY"; return false; }
            if (suspendThrough != DateTime.MinValue ? suspendThrough.Date <= VoteDayOf(country, today) : !(raiseUsd > 0d)) { why = "A RAISE NEEDS A SIZE, A SUSPENSION A DAY AFTER ITS VOTE"; return false; }   // the review's M2: never a last day the vote has passed
            return true;
        }

        /// <summary>PS-6 K22 (Elias's ruling: "a hostile chamber can withhold the raise, as with the CR"): whether the player's party may withhold the raise today -
        /// a party seated, not the presidency's (the raise is its own government's otherwise), holding a chamber (the House's majority or the Senate's,
        /// `Congress.HoldsHouse` / `HoldsSenate`), while the limit is reached and not suspended. False with the screen's words where refused.</summary>
        public static bool MayWithhold(Country country, bool congressVotes, DateTime today, out string why)
        {
            why = null;
            if (!IsRead(country) || !congressVotes) { why = NoLimitWords; return false; }
            string party = country.PlayerPartyAbbrev;
            if (string.IsNullOrEmpty(party)) { why = "NO PARTY OF YOURS IS SEATED"; return false; }
            if (country.Government != null && country.Government.RoleOf(party) == Elections.PlayerRole.PrimeMinister) { why = "THE RAISE IS YOUR OWN GOVERNMENT'S - PROPOSE IT OR NOT"; return false; }
            if (!Congress.HoldsHouse(country, party) && !Congress.HoldsSenate(country, party, today)) { why = "YOUR PARTY HOLDS NEITHER CHAMBER"; return false; }
            if (Suspended(country, today) || country.DebtLimitReachedOn == DateTime.MinValue) { why = "NO RAISE IS DUE - THE DEBT IS BELOW THE LIMIT"; return false; }
            return true;
        }

        /// <summary>PS-6 K85 (Elias's ruling of 2026-10-23, on the review's M1: "a binding debt limit with nothing proposed holds the clock for a player-president,
        /// like the other waits"): whether the limit waits on the President - the player's party holds the presidency (<paramref name="playerPresident"/>), the
        /// game's Congress votes, the limit binds, it is not suspended, and nothing is before Congress or on the desk (<paramref name="pending"/>).</summary>
        public static bool AwaitsPresident(Country country, bool playerPresident, bool congressVotes, BudgetBill pending, DateTime today)
            => playerPresident && congressVotes && IsRead(country) && pending == null && !Suspended(country, today) && Binding(country, today) && !country.DebtLimitLetBind;   // K90: not where he chose to let it bind

        /// <summary>PS-6 K22 (the review's L2): whether a standing withhold no longer bites - its party holds neither chamber.</summary>
        public static bool WithholdOutlived(Country country, DateTime today)
            => country?.DebtLimitWithheldBy != null && !Congress.HoldsHouse(country, country.DebtLimitWithheldBy) && !Congress.HoldsSenate(country, country.DebtLimitWithheldBy, today);

        /// <summary>PS-6 K22: whether a standing withhold lapses - its party now holds the presidency, so the raise is its own government's.</summary>
        public static bool WithholdLapses(Country country)
            => country?.DebtLimitWithheldBy != null && country.Government != null && country.Government.RoleOf(country.DebtLimitWithheldBy) == Elections.PlayerRole.PrimeMinister;

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 K78: the AI's hostile majority withholds (`ElectionsData/usa/debtlimit_episodes.md`)
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>One hostile-majority episode of record: its name, the hostile chamber's majority party and the president's, the day the limit was reached
        /// (extraordinary measures begun), the day the hostile chamber first refused or conditioned a clean raise (MinValue where BILLED), the X-date (Treasury's
        /// last projected day of the measures) and the day the act that ended it was approved.</summary>
        public readonly struct HostileEpisode
        {
            public readonly string Name, Majority, President;
            public readonly DateTime Reached, FirstRefused, XDate, Law;
            public HostileEpisode(string name, string majority, string president, DateTime reached, DateTime firstRefused, DateTime xDate, DateTime law)
            {
                Name = name; Majority = majority; President = president; Reached = reached; FirstRefused = firstRefused; XDate = xDate; Law = law;
            }
            /// <summary>The X-date less the law's day, in days - how long before Treasury's last projected day the hostile majority let the act pass.</summary>
            public int LeadDays => (XDate - Law).Days;
        }

        /// <summary>SOURCED (each date's file in `ElectionsData/usa/raw/debtlimit_episodes/`, read in `ElectionsData/usa/debtlimit_episodes.md`; R43389's dates
        /// [SECONDARY], its primary BILLED; `UsDebtLimitLiveDiagnostic` holds each to its file): K78's named episodes, 2011, 2013 and 2023, each a REP House against a
        /// DEM president. 2021 does not belong (no hostile majority: a Senate minority's filibuster). 2013's first refusal is BILLED.</summary>
        public static readonly HostileEpisode[] HostileEpisodes =
        {
            new HostileEpisode("2011", "REP", "DEM", new DateTime(2011, 5, 16), new DateTime(2011, 5, 31), new DateTime(2011, 8, 2), new DateTime(2011, 8, 2)),
            new HostileEpisode("2013", "REP", "DEM", new DateTime(2013, 5, 20), DateTime.MinValue, new DateTime(2013, 10, 17), new DateTime(2013, 10, 17)),
            new HostileEpisode("2023", "REP", "DEM", new DateTime(2023, 1, 19), new DateTime(2023, 4, 26), new DateTime(2023, 6, 5), new DateTime(2023, 6, 3)),
        };

        /// <summary>[AUTHORED-DRAFT] - K78's rule as amended by K89 (Elias's ruling of 2026-10-24: "the AI's withhold BITES: it holds through the deadline and
        /// releases only at the record's LATEST lead ... read the max of the episodes' leads, not the median"), n = 3 (too few to fit; the episodes are its
        /// reading): the days PAST the day the measures run out that the AI's hostile majority still withholds - DERIVED as the largest of the episodes' leads
        /// (X-date less law: 0, 0 and 2 days). DECLARED, the reading: the record's largest lead is read as the length of the stand-off past the model's deadline,
        /// so a raise voted on the deadline or the day after fails and the cut binds those days; the literal "2 days before the X-date" would release before the
        /// model's only vote day, where the withhold would bite nothing (the second review's L4) - asked.</summary>
        public static readonly int AiHoldDaysPastDeadline = LargestLead();

        private static int LargestLead()
        {
            int most = 0;
            foreach (HostileEpisode e in HostileEpisodes) { most = Math.Max(most, e.LeadDays); }
            return most;
        }

        /// <summary>PS-6 K89: the day the AI's hostile majority releases the raise - the measures' end plus `AiHoldDaysPastDeadline`, before that day's vote.</summary>
        public static DateTime AiReleaseDay(Country country) => BindsOn(country).AddDays(AiHoldDaysPastDeadline);

        /// <summary>PS-6 K78: the AI party that would withhold the raise today - the majority party of a chamber (the House's, else the Senate's) that is not the
        /// presidency's and not the player's (a player's party withholds only by its verb, K22); null where none is, or no presidency governs.</summary>
        public static string AiWithholder(Country country, DateTime today)
        {
            if (country?.Government == null || country.Government.Kind != Elections.WorldClock.ExecutiveKind.Presidency) { return null; }
            string president = country.Government.PmParty, player = country.PlayerPartyAbbrev;
            foreach (string held in new[] { Congress.HouseMajority(country), Congress.SenateMajority(country, today) })
            {
                if (!string.IsNullOrEmpty(held) && held != president && held != player) { return held; }
            }
            return null;
        }

        /// <summary>PS-6 K78 with K89 (the rule, [AUTHORED-DRAFT]): whether the AI's hostile majority withholds today - the limit reached and not suspended, and
        /// before the release (`AiReleaseDay`, past the deadline). DECLARED (Elias, 2026-10-23, not chosen to change): the withhold starts on the day the
        /// limit is reached, not at the record's refusal lag (15 days in 2011, 97 in 2023); no further episode is sourced now.</summary>
        public static bool AiWithholdsOn(Country country, DateTime today)
            => country != null && country.DebtLimitReachedOn != DateTime.MinValue && !Suspended(country, today) && today.Date < AiReleaseDay(country);

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
                country.DebtLimitLetBind = false;   // K90: the episode closed
                return;
            }
            if (!congressVotes && Binding(country, today)) { Raise(country, today, "the AI-governed USA raises it by act - no Congress of the game votes here"); }
        }

        /// <summary>A raise becomes law on <paramref name="today"/>: the limit up by `RaiseOf` - <paramref name="proposedUsd"/> (the standard
        /// `RaiseUsd` where 0) added to the day's excess (K77) - the episode closed (the measures and the cut end; a withhold ends with it; the next
        /// day's bookkeeping reaches again where the debt still stands at or over the new limit).</summary>
        public static void Raise(Country country, DateTime today, string how, double proposedUsd = 0d)
        {
            double was = country.DebtLimitUsd;
            double by = RaiseOf(country, proposedUsd);
            country.DebtLimitUsd = was + by;
            country.DebtLimitSince = today.Date;
            country.DebtLimitReachedOn = DateTime.MinValue;
            country.DebtLimitCutFlowDay = DateTime.MinValue;   // disarmed here: a docket answer between the day steps must not leave the next day's flows cut
            country.DebtLimitWithheldBy = null;   // K22: the episode closed - the next raise is withheld afresh, as the continuing resolution's
            country.DebtLimitLetBind = false;   // K90: the episode closed
            country.DebtLimitRaises++;
            string withExcess = by > (proposedUsd > 0d ? proposedUsd : RaiseUsd) ? " - the excess " + Usd(by - (proposedUsd > 0d ? proposedUsd : RaiseUsd)) + " and the size (K77)" : string.Empty;
            Debug.Log($"LIMIT: {country.Id} - raised on {today:yyyy-MM-dd} by {Usd(by)}{withExcess}, {Usd(was)} to {Usd(country.DebtLimitUsd)} ({how}; the game's raise no. {country.DebtLimitRaises}); the cut since the measures ran out: {country.DebtLimitCutBillions:0.###} bn");
            country.DebtLimitCutBillions = 0f;
        }

        /// <summary>PS-6 K22 (Elias's ruling: "a raise may suspend the limit to a date"): a suspension becomes law on <paramref name="today"/> - no limit applies
        /// through <paramref name="through"/>, and the day after it the limit returns at that day's debt subject to limit (`Track`, P.L. 118-5
        /// s401(b)'s form); the episode closed as a raise closes it.</summary>
        public static void Suspend(Country country, DateTime today, DateTime through, string how)
        {
            if (through.Date <= today.Date)
            {
                // the second review's L1: a suspension signed after its own last day (late on the desk) suspends nothing - the limit binds on, and the President is asked again
                Debug.Log($"LIMIT: {country.Id} - the suspension through {through:yyyy-MM-dd} becomes law on {today:yyyy-MM-dd}, after its last day: nothing is suspended ({how})");
                country.DebtLimitLetBind = false;
                return;
            }
            double was = country.DebtLimitUsd;
            country.DebtLimitUsd = 0d;
            country.DebtLimitSince = today.Date;
            country.DebtLimitSuspendedThrough = through.Date;
            country.DebtLimitReachedOn = DateTime.MinValue;
            country.DebtLimitCutFlowDay = DateTime.MinValue;
            country.DebtLimitWithheldBy = null;
            country.DebtLimitLetBind = false;
            country.DebtLimitRaises++;
            Debug.Log($"LIMIT: {country.Id} - suspended on {today:yyyy-MM-dd} through {through:yyyy-MM-dd} (the limit was {Usd(was)}; {how}; the game's act no. {country.DebtLimitRaises}); the cut since the measures ran out: {country.DebtLimitCutBillions:0.###} bn");
            country.DebtLimitCutBillions = 0f;
        }

        /// <summary>The act <paramref name="bill"/> carries becomes law: its suspension, or its raise (K77: the day's excess and its size).</summary>
        public static void Enact(Country country, DateTime today, BudgetBill bill, string how)
        {
            if (bill != null && bill.DebtLimitSuspendThrough != DateTime.MinValue) { Suspend(country, today, bill.DebtLimitSuspendThrough, how); return; }
            Raise(country, today, how, bill?.DebtLimitRaiseUsd ?? 0d);
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
        public static string BillTitle() => "Reconciliation (the debt limit): the limit raised by the excess and " + RaiseWords;

        /// <summary>PS-6 K23: the title of <paramref name="bill"/> - the government's own raise `BillTitle()`, a proposed raise by its size as tabled, a
        /// suspension by its last day; every figure in the title's one fixed form, so it stays world-independent for FORK-2 (the size is the played world's,
        /// handed to every replay as given).</summary>
        public static string BillTitle(BudgetBill bill)
        {
            if (bill == null || (bill.DebtLimitRaiseUsd <= 0d && bill.DebtLimitSuspendThrough == DateTime.MinValue)) { return BillTitle(); }
            return bill.DebtLimitSuspendThrough != DateTime.MinValue
                ? "Reconciliation (the debt limit): the limit suspended through " + bill.DebtLimitSuspendThrough.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
                : "Reconciliation (the debt limit): the limit raised by the excess and " + TrillionsWords(bill.DebtLimitRaiseUsd);
        }

        /// <summary>The raise in the title's fixed form - trillions to two places, the book's unit ("US$5.00T"), the invariant culture.</summary>
        public static string RaiseWords => TrillionsWords(RaiseUsd);

        /// <summary>A size in the title's fixed form.</summary>
        public static string TrillionsWords(double usd) => "US$" + (usd / 1e12).ToString("0.00", CultureInfo.InvariantCulture) + "T";

        /// <summary>The concern both chambers vote the raise on - the budget's own concern builder over the bill (which moves no rate, line, benefit or fund, so it
        /// asks the blocs nothing), by reconciliation (2 U.S.C. 641(a)(3), DECLARED). The vote and the Laws page's card read this one. PS-6 K22: a withheld raise
        /// carries its withholder, so the blocs take the continuing resolution's fixed sides (`StanceModel`: the withholder against in both chambers, every other
        /// party for) and a chamber the withholder holds stops it.</summary>
        public static BillConcern ConcernOf(Country country, BudgetBill bill)
        {
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, bill);
            concern.Reconciliation = true;
            concern.WithheldBy = country?.DebtLimitWithheldBy;
            return concern;
        }

        /// <summary>The law in force, for the log.</summary>
        public static string LawWords(Country country)
            => country.DebtLimitSuspendedThrough != DateTime.MinValue ? "suspended through " + country.DebtLimitSuspendedThrough.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : Usd(country.DebtLimitUsd) + " since " + country.DebtLimitSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string Usd(double usd) => "$" + usd.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
