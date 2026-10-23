using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-22 (`docs/specs/USA_STAGE_PLAN.md` US-22; R-US9 (a) as ruled, F8): THE US BUDGET PROCEDURE'S PURE PART - what each act carries, how it is
    /// voted, what applies where it falls, the funding arithmetic and the words. The simulation's path is `SimulationManager`'s (the acts' day, the
    /// continuing resolution, the funding day); this holds only functions of the bill, the country and the date, so the gate, the pages and the diagnostic
    /// read one answer.
    /// <list type="bullet">
    /// <item><b>The acts</b>, on E2's parts (`BudgetBill.StatutePart`): THE APPROPRIATION - every spending line, R-US9 (a)'s "the game's spending lines DECLARED
    /// as one appropriation" - at cloture; THE RECONCILIATION - the rates, the benefit levels and the fund (US-18's reading of direct spending; "the rates,
    /// and direct spending and the debt limit where they move, ride reconciliation at simple majorities (2 U.S.C. 641(a), (e))"; the debt limit moves in no
    /// bill until US-24) - by a majority; THE PENSION ACT - "the pension age is its own statute at cloture" (641(g); 644(b)(1)(F)).</item>
    /// <item><b>The funding horizon</b>: an act or resolution enacted on a day funds the fiscal year in progress where it is unfunded (a lapse), else the next -
    /// never further (DECLARED). The fiscal year is 31 U.S.C. 1102's (`FiscalYearData`).</item>
    /// <item><b>Scope</b>: <see cref="Applies"/> - the US Congress's procedure and a Senate seated, so an AI USA in another country's game votes nothing and is
    /// never seeded (the plan's risk 19).</item>
    /// </list>
    /// Deterministic: no random stream is drawn.
    /// </summary>
    public static class UsBudgetProcedure
    {
        /// <summary>Whether the US Congress votes <paramref name="country"/>'s budget as its acts: the procedure (`WorldClock.CongressActs`) and a Senate seated
        /// (`Congress.Applies`) - the player's USA.</summary>
        public static bool Applies(Country country) => country != null && WorldClock.CongressActs(country.Id) && Congress.Applies(country);

        /// <summary>The acts a US budget is voted as.</summary>
        public enum Act { Appropriation, Reconciliation, PensionAct }

        /// <summary>The acts in the order they are voted (the plan's order: the appropriation, the reconciliation, the pension act).</summary>
        public static readonly Act[] Acts = { Act.Appropriation, Act.Reconciliation, Act.PensionAct };

        /// <summary>Whether <paramref name="bill"/> changes what <paramref name="act"/> carries on <paramref name="country"/>: a spending line moved, a rate, a
        /// benefit level or a fund rule set otherwise than in force, the pension age set otherwise than in force.</summary>
        public static bool Changes(BudgetBill bill, Act act, Country country)
        {
            switch (act)
            {
                case Act.Appropriation: return Congress.ChangesSpendingLines(country, bill);
                case Act.Reconciliation: return bill.ChangesTaxRates(country) || bill.ChangesBenefits(country) || bill.ChangesFund(country);
                default: return bill.ChangesPensionAge(country);
            }
        }

        /// <summary>Whether <paramref name="act"/> is voted on <paramref name="bill"/>: the appropriation always - it is the funding, the standing figures where
        /// it moves no line (uncontested) - and the other two only where they change something.</summary>
        public static bool IsVoted(BudgetBill bill, Act act, Country country) => act == Act.Appropriation || Changes(bill, act, country);

        /// <summary>
        /// The act <paramref name="act"/> carries - that part of the bill alone, tabled by whoever tabled the bill, so the chamber votes on it and reads the same
        /// author. ⚠ For the vote's concern and title only, never applied (a part built new carries no fund, and applied it would dissolve one - E2's warning on
        /// `BudgetBill.PartOf`): application withholds fallen parts from the whole bill (<see cref="Without"/>).
        /// </summary>
        public static BudgetBill PartOf(BudgetBill bill, Act act, Country country)
        {
            switch (act)
            {
                case Act.Appropriation: return bill.SpendingOnly(country);
                case Act.PensionAct: return bill.PartOf(BudgetBill.StatutePart.PensionAge);
                default:
                    return new BudgetBill
                    {
                        GovernmentBill = bill.GovernmentBill, TabledBy = bill.TabledBy,
                        TaxLines = new Dictionary<TaxType, float>(bill.TaxLines), BracketRates = new Dictionary<TaxType, float[]>(bill.BracketRates),
                        WelfarePrograms = new Dictionary<WelfareProgramType, float>(bill.WelfarePrograms),
                        SwfShouldExist = bill.SwfShouldExist, SwfContributionRatePercent = bill.SwfContributionRatePercent, SwfDomesticAllocationPercent = bill.SwfDomesticAllocationPercent,
                        SwfEquitiesWeight = bill.SwfEquitiesWeight, SwfBondsWeight = bill.SwfBondsWeight, SwfInfrastructureWeight = bill.SwfInfrastructureWeight, SwfRealEstateWeight = bill.SwfRealEstateWeight,
                    };
            }
        }

        /// <summary>What applies where <paramref name="act"/> falls (or its veto stands): <paramref name="applied"/> with that act's part withheld - the lines as
        /// they stand (`BudgetBill.WithoutSpending`), the rates, benefit levels and fund rules as they stand, or the pension age as it stands.</summary>
        public static BudgetBill Without(BudgetBill applied, Act act, Country country)
        {
            switch (act)
            {
                case Act.Appropriation: return applied.WithoutSpending();
                case Act.PensionAct: return applied.Without(BudgetBill.StatutePart.PensionAge, country);
                default: return applied.Without(BudgetBill.StatutePart.Rates, country).Without(BudgetBill.StatutePart.Benefits, country).Without(BudgetBill.StatutePart.Fund, country);
            }
        }

        /// <summary>The concern the chambers vote <paramref name="act"/> on - the budget's own concern builder over the act's part, its Senate procedure the act's:
        /// the reconciliation by a majority (641(e)), the appropriation and the pension act at cloture (R-US9 (a)) - set here, since the content test alone would
        /// send an appropriation of the standing figures by reconciliation.</summary>
        public static BillConcern ConcernOf(Country country, BudgetBill bill, Act act)
        {
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, PartOf(bill, act, country));
            concern.Reconciliation = act == Act.Reconciliation;
            return concern;
        }

        /// <summary>An act's name, the head of its division's title and ledger event. "Pension act" is Poland's word for the same act (`SimulationManager.ActWords`).</summary>
        public static string ActWords(Act act) => act == Act.Appropriation ? "Appropriation" : act == Act.Reconciliation ? "Reconciliation" : "Pension act";

        /// <summary>A continuing resolution's name, the head of its division's title and ledger event.</summary>
        public const string ContinuingResolutionWords = "Continuing resolution";

        /// <summary>A continuing resolution's division title: the standing figures, and, voted during a lapse, the lapse's day (the resolution whose passage ends
        /// a lapse names its days - the plan's records, J).</summary>
        public static string ContinuingResolutionTitle(Country country, DateTime day)
            => ContinuingResolutionWords + ": the standing figures" + (country.LapseSince == DateTime.MinValue ? string.Empty
                : " - on day " + LapseDays(country.LapseSince, day).ToString(CultureInfo.InvariantCulture) + " of the shutdown");

        /// <summary>Why the US chamber weighs no alternative budget - `SimulationManager.TableShadowBudget`'s refusal and the Budget page's line.</summary>
        public const string NoAlternativeWords = "CONGRESS VOTES THE PRESIDENT'S BUDGET AS ITS ACTS · NO ALTERNATIVE BUDGET IS WEIGHED";

        /// <summary>The refusal where no continuing resolution can be withheld in this country.</summary>
        public const string NoResolutionWords = "THIS COUNTRY VOTES NO CONTINUING RESOLUTION";

        /// <summary>
        /// Ask 2's reading (P), PROVISIONAL (it stands, Elias's ruling I13 (2)): may the player's party withhold the continuing resolution? One before Congress
        /// (<paramref name="pending"/>), or - US-22b (I13 (3)) - one DUE (<see cref="ResolutionDue"/>: the one decided on the day before the horizon is withheld in
        /// advance, as no day passes between its tabling and its vote), by a party seated - PS-6 K75 (Elias's ruling of 2026-10-23: "a player-president's own
        /// party may withhold the continuing resolution"): the president's own party too, its own government's resolution though it is. False with
        /// <paramref name="why"/>, in a page's words, where not.
        /// </summary>
        public static bool MayWithhold(Country country, BudgetBill pending, DateTime today, out string why)
        {
            why = null;
            if ((pending == null || !pending.ContinuingResolution) && !ResolutionDue(country, pending, today)) { why = "NO CONTINUING RESOLUTION IS BEFORE CONGRESS OR DUE"; return false; }
            if (string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { why = "NO PARTY OF YOURS IS SEATED"; return false; }
            return true;
        }

        /// <summary>PS-6 K75: whether a standing withhold of the continuing resolution lapses with the presidency - its party TOOK the presidency after it withheld
        /// (it was out of office when it withheld, or left office since: `Country.ContinuingResolutionWithheldInOffice` false), so the next resolution is its own
        /// new government's. A president's own party's withhold, given in office, stands until released or until funding is restored - a re-election's second
        /// oath included (the review's L1: a new government is formed at every oath, so the day the government formed is not read).</summary>
        public static bool WithholdLapsesWithPresidency(Country country)
            => country?.ContinuingResolutionWithheldBy != null && HoldsPresidency(country, country.ContinuingResolutionWithheldBy) && !country.ContinuingResolutionWithheldInOffice;

        /// <summary>Whether <paramref name="party"/> holds the presidency (the government's head party).</summary>
        public static bool HoldsPresidency(Country country, string party) => country?.Government != null && !string.IsNullOrEmpty(party) && country.Government.RoleOf(party) == PlayerRole.PrimeMinister;

        /// <summary>PS-6 K75 (the review's L1): a withhold given while its party held the presidency stays the president's own party's only while the party
        /// never leaves office - a re-election's second oath forms a new government but keeps it; the day the party is out of the presidency it is marked out,
        /// so a later return lapses the withhold.</summary>
        public static void NoteWithholderInOffice(Country country)
        {
            if (country?.ContinuingResolutionWithheldBy != null && !HoldsPresidency(country, country.ContinuingResolutionWithheldBy)) { country.ContinuingResolutionWithheldInOffice = false; }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------------
        // The funding arithmetic (DECLARED readings, DERIVED dates)
        // ------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The last day of the US fiscal year <paramref name="day"/> falls in - DERIVED from 31 U.S.C. 1102's start (`FiscalYearData.GetFiscalYearStart`):
        /// the day before the next fiscal year begins.</summary>
        public static DateTime FiscalYearEnd(DateTime day)
        {
            (int month, int start) = FiscalYearData.GetFiscalYearStart(CountryId.USA);
            var next = new DateTime(day.Year, month, start);
            if (day.Date >= next) { next = next.AddYears(1); }
            return next.AddDays(-1);
        }

        /// <summary>
        /// THE HORIZON AFTER A FUNDING MEASURE ENACTED ON <paramref name="enacted"/> (DECLARED): where the government is unfunded that day (a lapse, or no horizon
        /// held) it funds the fiscal year in progress; where it is funded, the next fiscal year - and never beyond it, so a horizon already there stands. A
        /// continuing resolution is full-year, as R-US9 (a) words it ("a full-year continuing resolution"). An appropriation applies when enacted, as every
        /// budget does in this game, not on 1 October (DECLARED).
        /// </summary>
        public static DateTime FundedThrough(DateTime horizon, DateTime enacted)
        {
            DateTime inProgress = FiscalYearEnd(enacted);
            DateTime target = enacted.Date > horizon ? inProgress : inProgress.AddYears(1);
            return target > horizon ? target : horizon;
        }

        /// <summary>PS-6 US-22b (I13 (3)): THE DAY BEFORE THE HORIZON - the day a continuing resolution is decided where no appropriation for the coming year is
        /// law (the horizon is the last funded day, `Country.AppropriatedThrough`).</summary>
        public static DateTime HorizonEve(DateTime horizon) => horizon.Date.AddDays(-1);

        /// <summary>PS-6 US-22b: whether a continuing resolution is DUE with none before Congress - nothing in the slot (<paramref name="pending"/> null), and
        /// either funding lapsed (the lapse's cadence) or no appropriation for the coming fiscal year law yet (the horizon the end of the fiscal year in
        /// progress, so one is decided the day before it). DECLARED: "due" is read from the horizon alone, so it holds from the start of a fiscal year until
        /// its appropriation for the next is law.</summary>
        public static bool ResolutionDue(Country country, BudgetBill pending, DateTime today) =>
            pending == null && country != null && country.AppropriatedThrough != DateTime.MinValue
            && (country.LapseSince != DateTime.MinValue || country.AppropriatedThrough == FiscalYearEnd(today));

        /// <summary>A lapse's days on <paramref name="today"/>: the first day counted (the record's count, F8: 1 Oct to 12 Nov 2025 is 43 days).</summary>
        public static int LapseDays(DateTime since, DateTime today) => (today.Date - since.Date).Days + 1;

        /// <summary>K42: the days after the restoration within which the Desk may still first show FUNDING RESTORED (the review of wave/usf, L2: a frame at speed
        /// may step past the day itself). DECLARED - the memo it replaced held the same three.</summary>
        public const int RestoredWindowDays = 3;   // [AUTHORED-DRAFT] a screen's window, no source: the retired memo's three days

        /// <summary>K42 (Elias, 2026-10-23): whether <paramref name="today"/> lies in the restoration's window - from the saved day the last lapse ended
        /// (`Country.LapseRestoredOn`) to `RestoredWindowDays` after it, with no lapse since. The Desk shows the chip on the first day it is drawn in the
        /// window, that day only (`GameController.RestoredChipToday`).</summary>
        public static bool RestoredWithin(Country country, DateTime today) =>
            country != null && country.LapseSince == DateTime.MinValue && country.LapseRestoredOn != DateTime.MinValue
            && today.Date >= country.LapseRestoredOn.Date && (today.Date - country.LapseRestoredOn.Date).Days <= RestoredWindowDays;

        /// <summary>The lapse in a page's words - its day, the day funding lapsed, how it ends, and what it does (US-22b, `UsLapse`); null where funding has not
        /// lapsed.</summary>
        public static string LapseWords(Country country, DateTime today)
        {
            if (country == null || country.LapseSince == DateTime.MinValue) { return null; }
            return "SHUTDOWN · DAY " + LapseDays(country.LapseSince, today).ToString(CultureInfo.InvariantCulture) + " · FUNDING LAPSED ON "
                + country.LapseSince.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()
                + " · IT ENDS THE DAY AN APPROPRIATION OR A CONTINUING RESOLUTION BECOMES LAW · " + UsLapse.StopWords;
        }

        /// <summary>The funding horizon in a page's words, or null where none is held.</summary>
        public static string FundedWords(Country country) => country == null || country.AppropriatedThrough == DateTime.MinValue ? null
            : "FUNDED THROUGH " + country.AppropriatedThrough.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        /// <summary>The procedure in a slip's lines, after each act's own: what follows a fallen appropriation, what is not modelled.</summary>
        public static IEnumerable<string> ProcedureWords()
        {
            yield return "IF NO APPROPRIATION IS LAW BY 29 SEPTEMBER, A CONTINUING RESOLUTION AT CLOTURE CARRIES THE STANDING FIGURES THAT DAY; IF IT FAILS, FUNDING LAPSES ON 1 OCTOBER AND THE GOVERNMENT SHUTS DOWN UNTIL ONE BECOMES LAW";
            yield return "THE ACTS STAND OR FALL APART - A FAILED APPROPRIATION TAKES NO OTHER ACT DOWN";
            yield return "IN A SHUTDOWN " + UsLapse.StopWords + " · THE BYRD RULE IS OTHERWISE NOT MODELLED";
        }

        /// <summary>An act's procedure in a slip's words.</summary>
        public static string ProcedureOf(Act act) => act == Act.Reconciliation ? "BY RECONCILIATION, A MAJORITY" : "AT CLOTURE";
    }
}
