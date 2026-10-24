using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-23 (`docs/specs/USA_STAGE_PLAN.md` US-23; R-US7 (a) as ruled, F8: "where the party out of the White House holds both chambers, its majority - the
    /// AI's when it is not the player's - passes its own budget reconciliation once a fiscal year and sends it to the president"): THE MAJORITY'S RECONCILIATION'S
    /// PURE PART - who sends one, when, and what it carries. The simulation's path (the slot, the vote, the president's desk) is `SimulationManager`'s.
    /// <list type="bullet">
    /// <item><b>Who</b> (<see cref="TryOutPartyMajority"/>): the party holding the House (`Congress.HouseMajority`) and the Senate (`Congress.SenateMajority`, the
    /// Vice President's party on an equal division) on the day, where it is not the president of the day's party (`Congress.PresidentOn`). Where that majority is
    /// the player's own party nothing is sent for it (DECLARED: its own reconciliation is the player's to draft, and no surface drafts one - asked).</item>
    /// <item><b>When</b> (DECLARED, <see cref="Due"/>): once in the fiscal year before each 1 October (31 U.S.C. 1102), tabled on the first day from the
    /// president's request day (`FiscalYearData.BudgetRequestDay`, 1105(a)'s first Monday in February - the budget season the majority answers) on which the
    /// out-party holds both chambers, and no later than a bill's course (`ParliamentSystem.BillDurationDays`) before 1 October, so it is voted and lands on the
    /// president's desk before that day. The year is marked when it is tabled (`Country.MajorityReconciliationYear`), whatever then becomes of it.</item>
    /// <item><b>What</b> (DECLARED - the plan: "its content DECLARED as the AI finance ministry's book for that party (how the ministry takes a party's stance is
    /// read at the item)"). Read: the ministry (`AiFinanceMinistry`) writes no party's stance - its rules are the treaty's and the US caps - and the one path by
    /// which a party's stance reaches the ministry's instrument is the Finance partner's (`FinancePartner`, §755): one step a year of at most
    /// `FinancePartner.StepPointsPerYear` points of GDP, through the ministry's own household instrument, the income tax and VAT moved by the same points
    /// (`AiFinanceMinistry`'s RaiseHouseholdRates form). The majority's book is that step on what reconciliation carries (R-US9 (a): the rates; the spending
    /// lines are the appropriation's, at cloture): each household rate moved toward the rate the party's own position stands for on the line's dial - the
    /// spatial scale q16's vote reads (`SpatialBudgetVote.RateAt`) - and never past it, so a majority whose rates already stand at its position sends nothing.
    /// The benefit levels stay as they stand (the ministry's instrument carries none). The book is built again on the vote day against the law of that day,
    /// and what becomes law is its rates alone, each moved by its vote-day move from the law of the day it applies (<see cref="Applied"/>; the review's M1).</item>
    /// </list>
    /// Pure, deterministic: no stream is drawn.
    /// </summary>
    public static class UsMajorityReconciliation
    {
        /// <summary>CONVENTION - a rate move below this many points is not written (the ministry's own noise floor, `AiFinanceMinistry`).</summary>
        private const float MinPoints = 0.01f;

        /// <summary>The party out of the White House holding both chambers on <paramref name="day"/> - <paramref name="majority"/> - with the president's party; false
        /// where no Congress votes (`Congress.Applies`), no president is found, the chambers are split or held by no one, or their majority is the president's party.</summary>
        public static bool TryOutPartyMajority(Country country, DateTime day, out string majority, out string presidentParty)
        {
            majority = null; presidentParty = null;
            if (country == null || !Congress.Applies(country) || !Congress.PresidentOn(country, day, out _, out presidentParty, out _) || string.IsNullOrEmpty(presidentParty)) { return false; }
            string house = Congress.HouseMajority(country), senate = Congress.SenateMajority(country, day);
            if (string.IsNullOrEmpty(house) || house != senate || house == presidentParty) { return false; }
            majority = house;
            return true;
        }

        /// <summary>The 1 October a reconciliation tabled on <paramref name="day"/> must reach the president before - the start of the next fiscal year (31 U.S.C. 1102,
        /// DERIVED from `UsBudgetProcedure.FiscalYearEnd`).</summary>
        public static DateTime Deadline(DateTime day) => UsBudgetProcedure.FiscalYearEnd(day).AddDays(1);

        /// <summary>The year a reconciliation tabled on <paramref name="day"/> is marked under - the calendar year of its 1 October.</summary>
        public static int YearOf(DateTime day) => Deadline(day).Year;

        /// <summary>The first day one may be tabled before <paramref name="day"/>'s 1 October: the president's request day of that year (1105(a)).</summary>
        public static DateTime WindowOpens(DateTime day) => FiscalYearData.BudgetRequestDay(CountryId.USA, YearOf(day));

        /// <summary>The last day one may be tabled and still be voted before 1 October: a bill's course (`ParliamentSystem.BillDurationDays`) and a day before it.</summary>
        public static DateTime WindowCloses(DateTime day) => Deadline(day).AddDays(-ParliamentSystem.BillDurationDays - 1);

        /// <summary>Whether <paramref name="day"/> is inside the tabling window of its fiscal year.</summary>
        public static bool InWindow(DateTime day) => day.Date >= WindowOpens(day) && day.Date <= WindowCloses(day);

        /// <summary>
        /// Whether a majority's reconciliation is DUE on <paramref name="day"/>: inside the window, none tabled for this 1 October yet, and an out-party majority in
        /// both chambers that is not the player's (<paramref name="majority"/> is set where one holds them, the player's included, so a caller can say why none is due).
        /// </summary>
        public static bool Due(Country country, DateTime day, out string majority)
        {
            majority = null;
            if (country == null || !InWindow(day) || country.MajorityReconciliationYear == YearOf(day)) { return false; }
            if (!TryOutPartyMajority(country, day, out majority, out _)) { return false; }
            return majority != country.PlayerPartyAbbrev;
        }

        /// <summary>The rate <paramref name="party"/>'s position stands for on <paramref name="line"/>'s dial (`SpatialBudgetVote.RateAt` at the party's `redistribution`,
        /// `lrecon` where unpublished); false where the party has no position.</summary>
        public static bool TryPartyRate(Country country, string party, TaxLine line, out float rate)
        {
            rate = float.NaN;
            if (country == null || line == null || string.IsNullOrEmpty(party) || !SpatialBudgetVote.Applies(country)) { return false; }   // the review's L-4: Congress.Applies - a US Congress with a sworn Senate; the player's by this method's callers, all on the player's day tick
            foreach (PoliticalParty p in PartySystems.For(country.Id))
            {
                if (p.Abbrev != party) { continue; }
                float at = StanceModel.Position(p, StanceAxis.Redistribution, false);
                if (float.IsNaN(at)) { return false; }
                rate = SpatialBudgetVote.RateAt(country, line, at);   // K72: the sourced scale where it holds
                return true;
            }
            return false;
        }

        /// <summary>
        /// THE MAJORITY'S BOOK for <paramref name="party"/> on <paramref name="country"/> as it stands: each implemented household rate (`FinancePartner.IsHouseholdRate`)
        /// moved toward the party's rate on its dial (<see cref="TryPartyRate"/>) by the same points - the Finance partner's yearly step
        /// (`FinancePartner.StepPointsPerYear`, points of GDP) over the household bases that move (`TaxBases.Base`) - and never past it; as the ministry's bill
        /// (`AiFinanceMinistry.AsBudgetBill`: the fund's terms as they stand, no spending line), tabled by the party and not the government's. Null, with
        /// <paramref name="why"/>, where nothing would move.
        /// </summary>
        public static BudgetBill Book(Country country, string party, out string why)
        {
            why = null;
            float gdp = country?.State?.NominalGdp ?? 0f;
            var moving = new List<(TaxLine Line, float Toward)>();
            float baseTotal = 0f;
            if (country?.TaxLines != null)
            {
                foreach (TaxLine line in country.TaxLines)
                {
                    if (!line.IsImplemented || !FinancePartner.IsHouseholdRate(line.Type) || !TryPartyRate(country, party, line, out float toward) || Mathf.Abs(toward - line.Rate) < MinPoints) { continue; }
                    moving.Add((line, toward));
                    baseTotal += TaxBases.Base(country, line.Type);
                }
            }
            if (moving.Count == 0 || baseTotal <= 0f || gdp <= 0f) { why = "the household rates already stand at the party's position"; return null; }
            float points = FinancePartner.StepPointsPerYear / 100f * gdp / baseTotal * 100f;
            var decision = new PolicyDecision();
            foreach ((TaxLine line, float toward) in moving)
            {
                float to = toward > line.Rate ? Mathf.Min(toward, line.Rate + points) : Mathf.Max(toward, line.Rate - points);
                to = Mathf.Clamp(to, line.MinRate, line.MaxRate);
                if (Mathf.Abs(to - line.Rate) < MinPoints) { continue; }
                decision.TaxRateOverrides[line.Type] = to;
            }
            if (decision.TaxRateOverrides.Count == 0) { why = "the step is below the noise floor"; return null; }
            BudgetBill bill = AiFinanceMinistry.AsBudgetBill(country, decision);
            bill.GovernmentBill = false;
            bill.TabledBy = party;
            bill.RateMoves = MovesOf(country, bill);
            return bill;
        }

        /// <summary>The review's M1: the book's moves - per rate it names, its figure less the law standing on <paramref name="country"/> today, in points. Built with the
        /// book (<see cref="Book"/>), which the day step builds again on the vote day, so the moves are the step from the law of the day Congress votes it.</summary>
        public static Dictionary<TaxType, float> MovesOf(Country country, BudgetBill book)
        {
            var moves = new Dictionary<TaxType, float>();
            if (country?.TaxLines == null || book == null) { return moves; }
            foreach (KeyValuePair<TaxType, float> kv in book.TaxLines)
            {
                foreach (TaxLine line in country.TaxLines) { if (line.Type == kv.Key && line.IsImplemented) { moves[kv.Key] = kv.Value - line.Rate; break; } }
            }
            return moves;
        }

        /// <summary>
        /// The review's M1: WHAT APPLIES the day the majority's reconciliation becomes law - its RATES ALONE (by reading 1 it carries nothing else: no spending line,
        /// no benefit level, no fund term), each moved by its vote-day move (`BudgetBill.RateMoves`) FROM THE LAW OF THAT DAY, so a rate law passed while it waited
        /// is kept and the move is never more than the step; never past the party's own rate (<see cref="TryPartyRate"/> - a line already there, or beyond it,
        /// does not move) and within the dial. A bill carrying no moves (none is tabled so) is read from its figures against today's law.
        /// </summary>
        public static BudgetBill Applied(Country country, BudgetBill bill)
        {
            var applied = new BudgetBill { GovernmentBill = false, TabledBy = bill?.TabledBy, NamesNoFund = true };
            if (country?.TaxLines == null || bill == null) { return applied; }
            Dictionary<TaxType, float> moves = bill.RateMoves ?? MovesOf(country, bill);
            foreach (KeyValuePair<TaxType, float> kv in moves)
            {
                TaxLine line = null;
                foreach (TaxLine l in country.TaxLines) { if (l.Type == kv.Key) { line = l; break; } }
                if (line == null || !line.IsImplemented || Mathf.Abs(kv.Value) < MinPoints) { continue; }
                float to = line.Rate + kv.Value;
                if (TryPartyRate(country, bill.TabledBy, line, out float partyRate))
                {
                    if (kv.Value < 0f) { to = line.Rate > partyRate ? Mathf.Max(partyRate, to) : line.Rate; }
                    else { to = line.Rate < partyRate ? Mathf.Min(partyRate, to) : line.Rate; }
                }
                to = Mathf.Clamp(to, line.MinRate, line.MaxRate);
                if (Mathf.Abs(to - line.Rate) < MinPoints) { continue; }
                applied.TaxLines[line.Type] = to;
            }
            return applied;
        }

        /// <summary>The concern both chambers vote the majority's reconciliation on - the budget's reconciliation act's (`UsBudgetProcedure.ConcernOf`), its author the
        /// majority that tabled it (it votes for its own bill, §654) and no government's bill. The vote, the pages' projection and the gate read this one.</summary>
        public static BillConcern ConcernOf(Country country, BudgetBill bill)
        {
            BillConcern concern = UsBudgetProcedure.ConcernOf(country, bill, UsBudgetProcedure.Act.Reconciliation);
            concern.Author = bill.TabledBy;
            concern.GovernmentAuthored = false;
            return concern;
        }

        /// <summary>The reconciliation's division title: the act's own (`UsBudgetProcedure.ActWords`) naming the majority that sends it, then its moves - a title
        /// `SimulationManager.IsStatuteActTitle` reads as a statute act, never as the standing budget (the review's L2).</summary>
        public static string TitleOf(Country country, string party, string actTitle)
        {
            string head = UsBudgetProcedure.ActWords(UsBudgetProcedure.Act.Reconciliation);
            string rest = actTitle != null && actTitle.StartsWith(head, StringComparison.Ordinal) ? actTitle.Substring(head.Length) : ": " + actTitle;
            return head + " (the " + PartySystems.ShortName(country.Id, party) + " majority)" + rest;
        }

        /// <summary>The majority's reconciliation in a status line's words - before Congress, the day it is voted; on the President's desk, the day it is law
        /// unanswered (<paramref name="lawOn"/>, the caller's from the docket); null where none is pending (the review's L1).</summary>
        public static string StatusWords(Country country, BudgetBill bill, DateTime today, DateTime lawOn)
        {
            if (country == null || bill == null) { return null; }
            string who = "The " + PartySystems.ShortName(country.Id, bill.TabledBy) + " majority's reconciliation";
            return bill.Docket != null
                ? who + " passed Congress and is on the President's desk - law on " + lawOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture) + " unless answered."
                : who + " is before Congress - voted " + today.AddDays(Math.Max(0, bill.DaysRemaining)).ToString("d MMM yyyy", CultureInfo.InvariantCulture) + " by a majority in both chambers, then to the President.";
        }

        /// <summary>The majority's reconciliation before Congress, in a page's words - who sends it and the day it is voted; null where none is before it.</summary>
        public static string PendingWords(Country country, BudgetBill bill, DateTime today)
        {
            if (country == null || bill == null || bill.Docket != null) { return null; }
            return "THE " + PartySystems.ShortName(country.Id, bill.TabledBy).ToUpperInvariant() + " MAJORITY'S RECONCILIATION IS BEFORE CONGRESS · VOTED "
                + today.AddDays(Math.Max(0, bill.DaysRemaining)).ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · BY A MAJORITY IN BOTH CHAMBERS, THEN TO THE PRESIDENT";
        }
    }
}
