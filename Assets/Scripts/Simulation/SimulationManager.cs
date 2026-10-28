using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Snapshot of one country's fiscal computation for a single turn - the revenue/spending
    /// breakdown behind that turn's change to Budget/GovernmentDebt. Exposed for tools/UI (e.g.
    /// GameController's Trade &amp; Spending panel) that want to show where the money went without
    /// duplicating SimulationManager's formulas.
    /// </summary>
    public class FiscalTurnReport
    {
        public float Revenue;
        public float BaselineGovernmentSpending;
        public float DiscretionarySpending;
        public float MandatorySpending;
        public float UnemploymentBenefitCost;
        public float InterestOnDebt;
        public float TariffRevenue;
        /// <summary>EN-7b (2026-09-15): the electricity tax's receipts above the statute's base that accrued over the closed period at STANCE 1 - inside Revenue
        /// like TariffRevenue, so never re-added; a surface showing it says "of which".</summary>
        public float ElectricityTaxRevenue;
        /// <summary>Pass 6 (2026-08-27): the tariff pass-through the previous boundary PLANNED for the period
        /// that just closed, in inflation points (FiscalPeriod.PlannedTariffPassThroughPp) - the change in the
        /// tariff take, as a price-level term for one year. The Trade stats line reads it. EN-5 (2026-09-11):
        /// the planned figure, not the applied one, because the applied print now carries two level terms
        /// that cannot be split after the clamp - see PriceLevelTermsAppliedPp.</summary>
        public float TariffPassThroughPp;
        /// <summary>EN-5 (2026-09-11): the electricity pass-through the previous boundary planned for the period that just closed, inflation points (FiscalPeriod.PlannedEnergyPassThroughPp) - the household price's change relative to the general price level times its index weight.</summary>
        public float EnergyPassThroughPp;
        /// <summary>EN-5: what of the two level terms together ACTUALLY printed on the boundary day (FiscalPeriod.AppliedPriceLevelTermsPp) - net of the [MinInflationPercent, MaxInflationPercent] clamp; the expectations step looks through this figure.</summary>
        public float PriceLevelTermsAppliedPp;
        public float WelfareCost;
        public float SwfContribution;
        public float SwfReturns;

        /// <summary>
        /// This turn's total spending and budget balance EXACTLY as ApplyRevenueAndSpending computed
        /// them, recorded rather than recomputed.
        ///
        /// ⚠ Corrected, pass 5 (2026-08-26): this doc used to claim the components could not be added
        /// back up because BaselineGovernmentSpending + DiscretionarySpending was "a different field"
        /// from the summed G. That was wrong - the two DO reconstruct the summed G exactly (the plan's
        /// baseline plus this period's change is what was spent). What a hand sum actually misses is
        /// SwfContribution (part of TotalSpending) and, since tariffs ride inside Revenue, any line that
        /// adds TariffRevenue on top counts it twice. Read BudgetBalance; never re-add.
        ///
        /// BudgetBalance is signed the same way the simulation signs it: positive is a SURPLUS.
        /// TariffRevenue is the accrued tariff flow at STANCE 1 - what TradeSystem computed and the
        /// period planned - while Revenue carries it times the period's fiscal-reaction multiplier;
        /// a surface showing it as "of which" says so.
        /// </summary>
        public float TotalSpending;
        /// <summary>PS-6 US-24 (the review's M4): the non-interest outlays a binding debt limit cut in the period (`UsDebtLimit.CutToday`), NOMINAL billions -
        /// its own named line, so the parts less it are <see cref="TotalSpending"/>; 0 in every period the limit never bound.</summary>
        public float DebtLimitCut;
        /// <summary>MF q33 (the q32 review's M3): the non-interest outlays a closed market cut in the period (`SovereignCrisis.CutToday` - default's exclusion,
        /// a programme's withheld year), NOMINAL billions - its own named line beside <see cref="DebtLimitCut"/>; 0 in every period with the market open.</summary>
        public float CrisisCut;
        /// <summary>
        /// ⚠ AF-17 (Elias's ruling q17, 2026-10-18): THE YEAR THE PLAYER READS - the period's accrued daily balances PLUS its one-time settlements
        /// (<see cref="OneOffs"/>), so the closed year's deficit IS the debt's own change (the audit protocol's identity; GA-2-9) - less, since MF q33, any
        /// stock write-off the year took (<see cref="WriteOffs"/>, the default's haircut, which is no balance). §220 built it as the
        /// flow alone and named the settlements only in a diagnostic; every surface, the deficit rule's ink, the annual series and the EU ministry's
        /// reading now see one figure. Revenue and TotalSpending stay the flow's; the one-offs are their own line beside them.
        /// </summary>
        public float BudgetBalance;
        /// <summary>MF q33 (the q32 review 2's N1): the stock the year wrote off (the default's haircut, `DebtEventRecord.StockWriteOff`), NOMINAL billions,
        /// positive - outside <see cref="BudgetBalance"/>, so the debt's change is minus the balance less this; 0 in every year without a default.</summary>
        public float WriteOffs;
        /// <summary>
        /// AF-17 (the review's M2): the period's accrued daily balances ALONE - stored exactly as BudgetBalance was before AF-17 (`period.AccruedBudgetBalance`,
        /// one load and one store), never recovered as BudgetBalance − OneOffs, which is not the same float. The figure a FISCAL RULE reads: the AI ministry's
        /// EU rule judges a standing position, and q17 ruled what the player reads, not what the ministry reads. NaN only in a report from a save older than
        /// AF-17, and the load sets it to that report's BudgetBalance (which was the flow then) - so no reader ever sees the NaN.
        /// </summary>
        public float FlowBalance = float.NaN;
        /// <summary>AF-17: the closed year's one-time settlements, net, signed as the balance is (positive is money in) - the sum of <see cref="OneOffLines"/>.
        /// Inside <see cref="BudgetBalance"/>, so never re-added.</summary>
        public float OneOffs;
        /// <summary>AF-17: the one-offs itemised - each one-time settlement of the closed year as it landed on the debt (the debt ledger's dated Class B
        /// events of the period, <see cref="DebtLedgerRecorder.RecordEvent"/>), named by its writer. Empty in a year that had none, and EMPTY in a report
        /// from a save older than AF-17 (the field's initializer stands where the save has no key) - that report's balance is the flow, its one-offs unread.</summary>
        public List<FiscalOneOff> OneOffLines = new List<FiscalOneOff>();
    }

    /// <summary>AF-17 (q17): one one-time settlement of a closed year, itemised - a cabinet or foreign-policy decision, a fund drawdown, any write to the debt
    /// off the daily path. Read off the debt ledger's event as it landed (post-clamp), so the line is what the debt moved, with the sign flipped.</summary>
    public class FiscalOneOff
    {
        public System.DateTime Date;
        public string Label;
        /// <summary>What the one-off settled - the cabinet decision's or the meeting's own name (<see cref="DebtEventRecord.Source"/>); null where the writer has none.</summary>
        public string Source;
        /// <summary>Signed as the balance is: positive is money in (a windfall banked), negative a cost.</summary>
        public float Amount;

        /// <summary>MF q33 (the q32 review 2's N1): the stock a period's ledger wrote off - its write-off events, positive; 0 for none.</summary>
        public static float WriteOffsOf(DebtAttribution ledger)
        {
            float sum = 0f;
            if (ledger?.Events == null) { return sum; }
            foreach (DebtEventRecord e in ledger.Events) { if (e.StockWriteOff) { sum -= e.AppliedDelta; } }
            return sum;
        }

        /// <summary>The closing period's one-offs from its accruing debt ledger, in the order they landed; empty for none.</summary>
        public static List<FiscalOneOff> FromLedger(DebtAttribution ledger, out float sum)
        {
            var lines = new List<FiscalOneOff>();
            sum = 0f;
            if (ledger?.Events == null) { return lines; }
            foreach (DebtEventRecord e in ledger.Events)
            {
                if (e.StockWriteOff) { continue; }   // MF q33 (the q32 review's H1): a write-off of the stock - the default's haircut - is no settlement of the year
                lines.Add(new FiscalOneOff { Date = e.Date, Label = e.Label, Source = e.Source, Amount = -e.AppliedDelta });
                sum += -e.AppliedDelta;
            }
            return lines;
        }
    }

    /// <summary>
    /// Pass 6 (2026-08-27): what a Trade bill DRAFT would do at the next boundary, from
    /// SimulationManager.EstimateTradeBill - see it for how each figure is produced (the real
    /// functions on throwaway clones, never a hand sum). Display-layer only; nothing here is ever
    /// applied.
    /// </summary>
    public class TradeBillEstimate
    {
        /// <summary>The take one period would yield at the bill's rates (ComputeTariffRevenue on the clone the bill was applied to).</summary>
        public float Take;
        /// <summary>Take at the bill's rates minus take at the standing rates.</summary>
        public float TakeDelta;
        /// <summary>TradeBalance at the bill's rates minus at the standing rates - the partners' mirrored tariffs included, the currency factor inside.</summary>
        public float TradeBalanceDelta;
        /// <summary>The price pass-through the boundary would plan, in inflation points for the coming year.</summary>
        public float PassThroughPp;
    }

    /// <summary>
    /// Estimated single-turn effect of a not-yet-committed PolicyDecision, computed by
    /// SimulationManager.PreviewTurn against a throwaway clone - see that method for what it does
    /// and doesn't reproduce faithfully. Purely a display-layer estimate; nothing here is ever
    /// written back into real EconomyState/Country/World.
    /// </summary>
    public class PolicyPreview
    {
        /// <summary>Step 2: the preview clone's approval term ledger - the same named locals the
        /// real boundary records, computed on the clone. Read by the preview-parity diagnostic
        /// (term-by-term against the real boundary's ledger) and available to future preview
        /// explanation UI.</summary>
        public ApprovalAttribution ApprovalTerms;

        public float GdpGrowthPercent;
        public float UnemploymentChange;
        public float InflationChange;
        public float ApprovalChange;
        public float NetBudgetImpact;
        public float PovertyRateChange;
        public float LaborForceParticipationRateChange;
        public float CrimeIndexChange;
        public float SwfContributionEstimate;
        public float SwfReturnsEstimate;

        /// <summary>P4-B3 (2026-09-04): per sector, the clone's output share, employment share and own metric after the previewed turn against before - the sectors page's arrows, the preview's own figures.</summary>
        public Dictionary<SectorType, (float Output, float Employment, float Metric)> SectorDeltas;

        /// <summary>
        /// C-C1: the boundary's own revenue and total spending on the previewed clone, for one full
        /// turn — **and a full turn is a year** (`DaysPerTurn` is 365, the identity W-G1's
        /// trap-closing re-derived; MacroSystem's YearsPerTurn and the Phase 4 years-per-turn
        /// diagnostic that proved it 9 of 9 were REMOVED at F2 step 4, 2026-09-02, with the scalar
        /// demography they scaled). So these ARE the annual figures §P's finding 3 asks for; nothing is scaled
        /// to reach them.
        ///
        /// ⚠ **They are LEVELS, not deltas.** A draft's impact is the difference between two of them —
        /// see <see cref="SimulationManager.EstimateBudgetBill"/>, which runs one clone standing and
        /// one with the bill applied. Reading a single preview's level as an "impact" would report the
        /// whole budget as though the draft had caused it.
        /// </summary>
        public float RevenueEstimate;
        public float SpendingEstimate;
        /// <summary>PS-6 K26 (Elias's ruling of 2026-10-23: "the budget preview shows the cut when the limit binds"): the non-interest outlays the binding debt
        /// limit cuts in the previewed year, NOMINAL billions - inside `SpendingEstimate` already; 0 wherever the day tick has not armed the cut.</summary>
        public float DebtLimitCut;
        /// <summary>MF q33: the cut a closed market makes in the previewed year, NOMINAL billions - inside `SpendingEstimate` already. DECLARED (the q32 review's
        /// L7): the preview reads whether the deficit is financed at TODAY and applies that to its whole year, so a year in which an exclusion or a withheld
        /// year ends is previewed wholly cut, and one in which a crisis would begin wholly financed - a display figure, never booked.</summary>
        public float CrisisCut;
        /// <summary>
        /// P2-3.4 (2026-09-02): the previewed year's end readings the Taylor rule reads, and the rule's reading on
        /// them - so the Riksbank panel's projected path is the rule evaluated on this projection and nothing
        /// else. <see cref="PreviewedInterestRate"/> is the rate the previewed year ran at.
        /// </summary>
        public float PreviewInflation;
        /// <summary>EN-7a (2026-09-14): the electricity pass-through the preview planned, inflation points - read after the clone's spending resolves, so a
        /// standing or drafted Energy subsidy's levy cut is in it as the boundary's plan will be; read by EnergyPassThroughDiagnostic's parity section.</summary>
        public float PreviewEnergyPassThroughPp;
        /// <summary>EN-7b (2026-09-15): the electricity tax's receipts above the statute the preview planned, nominal billions - the clone's book, the flow the turn will book; read by EnergyPassThroughDiagnostic.</summary>
        public float PreviewElectricityTaxRevenue;
        public float PreviewUnemployment;
        public float PreviewNaturalUnemployment;
        public float PreviewOutputGapPercent;
        public float PreviewRuleRate;
        public float PreviewedInterestRate;
        /// <summary>§506 (2026-09-15): the government consumption the preview handed its identity, real - the clone's plan over its price level, as the day deflates it (since T-3 form A, §507, scaled by k);
        /// PreviewParityDiagnostic holds it to the boundary's (SimulationManager.GetIdentityGovernmentConsumption).</summary>
        public float PreviewIdentityGovernment;
    }

    /// <summary>
    /// C-C1: what a Budget bill DRAFT would do to the year's fiscal position — three figures the
    /// Budget and Tax/Welfare surfaces print before enactment (Playtest-1 finding 3).
    ///
    /// Every figure is a DELTA between two runs of the model's own boundary — one clone standing, one
    /// with the draft applied — never a hand sum beside the model. That is pass 5's recorded lesson on
    /// the Budget "Net" line, and `EstimateTradeBill`'s established pattern.
    /// </summary>
    public struct BudgetBillEstimate
    {
        /// <summary>Revenue the year would raise with the draft, minus revenue without it.</summary>
        public float RevenueDelta;

        /// <summary>Total spending with the draft, minus total spending without it.</summary>
        public float SpendingDelta;

        /// <summary>
        /// The year's budget balance with the draft, minus without. ⚠ **NOT computed as
        /// `RevenueDelta − SpendingDelta`** — it is read from the model's own `Budget` movement on each
        /// clone, so any term the balance carries that those two legs do not is inside it rather than
        /// silently dropped. `BudgetDraftEstimateDiagnostic` asserts the two agree, which is what turns
        /// "the legs add up" from an assumption into a measurement.
        /// </summary>
        public float NetDelta;
    }

    /// <summary>
    /// Drives the turn-based simulation loop for every country in the world: currency/trade
    /// effects resolve first (including each country's own tariff-policy change), then each
    /// country's domestic policy - fiscal (tax/spending/debt), the national accounts identity
    /// (GDP), Okun's Law (unemployment), the Phillips Curve (inflation), approval, and a random
    /// event roll - produces next turn's state. See MacroSystem for the macroeconomic theory and
    /// approval formula themselves, ElectionSystem/EventSystem for the rest of the political layer;
    /// this class only orchestrates turn order and the fiscal-accounting rules.
    ///
    /// Each rule is a small, separately named method with named constants rather than one large
    /// update function, so individual pieces of theory can be tuned or replaced independently.
    /// </summary>
    public partial class SimulationManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }

        /// <summary>
        /// 1 turn = 1 in-game year.
        ///
        /// **Was 121 days (~4 months) until 2026-08-10, and that was the source of a live 3.017x fiscal
        /// defect.** Every fiscal quantity in this game is an ANNUAL-rate figure - the spending seeds are
        /// FY2025 federal outlays ($1,530B Social Security, $850B Defense, 25 lines totalling $5,761B),
        /// GDP is annual ($29T for the USA), tax revenue is `GDP x rate x BaseShareOfGdp`, and interest
        /// is an annual rate on the debt stock. None of them was ever divided by a period. So a 121-day
        /// turn charged a full year of spending, revenue and interest every 121 days: 365/121 = 3.017x
        /// too fast, which is what Elias saw as "a full year's deficit every turn".
        ///
        /// **The same defect was found and fixed once before, in demographics.** MacroSystem's
        /// YearsPerTurn (retired with the scalar demography at F2 step 4, 2026-09-02) had a doc
        /// comment describing "3x over-compounding via too many
        /// applications of an annual-scale rate", diagnosed and fixed there by scaling each turn's growth
        /// to the turn's real-time slice. The fiscal path has the identical defect and was never brought
        /// along. `SwfStructuralDrawPerTurnFraction` was the one flow that got the conversion.
        ///
        /// **Why the turn moved to the year rather than the flows moving to the turn** (Elias's ruling,
        /// 2026-08-10): the economy was already annual-per-turn in every respect, so making the calendar
        /// agree with the model is one constant, while making the model agree with the calendar is a
        /// divisor on every flow plus a recalibration of every debt-anchored constant. This direction
        /// also costs nothing in the continuous-time migration, because **every per-day constant in
        /// Phases 1-3 was derived from this value rather than typed** - see `MacroSystem`'s
        /// `PerDayReversion`, `CrimeEffectsDailyScale`, `InfrastructureDecayRatePerDay` and
        /// `FiscalFlowPerDayFraction`, all of which retune themselves. That discipline is what made a
        /// one-line change possible here.
        ///
        /// **`ElectionSystem.ElectionCycle` moved with it**, 12 turns to 4, so a presidential term stays
        /// four years. The two constants are a pair: they are this project's only two statements of how
        /// long a turn is, and they disagreed by 0.5% before this change (121/365 = 0.3315 years against
        /// `YearsPerTurn`'s 4/12 = 0.3333). They now agree exactly at 1.0.
        /// </summary>
        public const int DaysPerTurn = 365;

        /// <summary>The in-game calendar's epoch (Turn 0's date). K-1 part (3) (2026-09-24, §604): **1 October 2026**, after the Riksdag elected on
        /// 2026-09-13 convenes (28 September, fixed by RF 3:10) and its session opens (29 September, scheduled - riksdagen.se as of
        /// 2026-09-23), so the seated chamber is true on day one
        /// (`ElectionsData/sweden/2026/government_2026.md`). It was 1 January 2026, an honestly-arbitrary choice. The move reaches the no-policy
        /// economy through ONE channel, measured: `Country.CalendarYear` (the statute readers) - with the year computed on the old start's day
        /// count the century is byte-identical to the old family. Every boundary now falls in the calendar year 2026 + turn (the old start
        /// lagged a year from turn 3), turn 4's boundary - the first election - is 30 September 2030, 22 days after the statute's second
        /// Sunday of September, and the inherited GDP quarter is exactly July-September 2026.</summary>
        /// <summary>Game start. Public since Step A: PublicationSystem must know it to suppress releases for reference periods that predate the simulation, which have no data behind them.</summary>
        /// <summary>PS-1 (2026-09-25, §618): the epoch a world opens on when no country has been chosen - K-1 part (3)'s 1 October 2026. A chosen country
        /// moves it to its own start (`WorldClock.StartDate`) BEFORE its world is created; a save carries the epoch it was cut on.
        /// ⚠ Declared BEFORE <see cref="EpochDate"/>: static initializers run in textual order, and the first dump of this pass logged
        /// `EPOCH: 0001-01-01 -> 2026-01-18` because the property's initializer read this field before it was set.</summary>
        public static readonly System.DateTime DefaultEpoch = new System.DateTime(2026, 10, 1);

        public static System.DateTime EpochDate { get; private set; } = DefaultEpoch;

        /// <summary>K80 (2026-10-23): the day of the PLAYED world (the last manager that is not a counterfactual to commit its clock), for a reader that has
        /// a country's id and no country - the seat map's order and legend and the inks' fence (`NationalElection.BlocOf`). MinValue after the epoch changes
        /// until a manager commits its clock; the reader then takes the epoch. A counterfactual reads its own countries' day (`Country.CalendarDay`).</summary>
        public static System.DateTime PlayedDay { get; private set; } = System.DateTime.MinValue;

        /// <summary>
        /// PS-1 (§618): sets the world's epoch. ⚠ Called before a world is created and never while one lives - `TurnBoundary`, the day-of-turn
        /// arithmetic and the cohort substrate's year all read it statically, so a manager built on one epoch and advanced under another would
        /// count its turns wrong. The game calls it at the player's choice of country (a fresh world follows) and on a load (the save's own epoch).
        /// </summary>
        public static void SetEpoch(System.DateTime epoch)
        {
            if (epoch == EpochDate) { return; }   // K80 (the review of 2026-10-24, M1): an unchanged epoch keeps the played day - a counterfactual's restore sets the save's own epoch, the played world's
            PlayedDay = System.DateTime.MinValue;   // K80: a new epoch opens a new world; the last played world's day is not this one's (a played world's own SetWorld or restore commits its day at once)
            UnityEngine.Debug.Log($"EPOCH: {EpochDate:yyyy-MM-dd} -> {epoch:yyyy-MM-dd}");
            EpochDate = epoch;
        }

        /// <summary>PS-1 (§618, the review's D3): a scope that puts the epoch back as it found it - for the Editor's tools and checks, which share one
        /// process and each open worlds on a country's start (`WorldClock.ApplyStart`); without it the first tool's start leaked into every check after it.</summary>
        public static System.IDisposable EpochScope() => new EpochRestore(EpochDate);

        private sealed class EpochRestore : System.IDisposable
        {
            private readonly System.DateTime _before;
            public EpochRestore(System.DateTime before) { _before = before; }
            public void Dispose() { SetEpoch(_before); }
        }

        /// <summary>PS-1: a manager built before the epoch moved reads the new one - the clock back to day one of turn 0. Only for a manager that has not advanced.</summary>
        public void ResetClockToEpoch()
        {
            if (CurrentTurn != 0) { throw new System.InvalidOperationException("the clock resets to the epoch only before any turn has run"); }
            CurrentDate = EpochDate;
            CommitCalendarYear();
        }

        /// <summary>Advances one day at a time via AdvanceDay, driven by GameController's own real-time speed-controlled Update loop - never touched by PreviewTurn's own throwaway clone, so a live slider drag can never leak a phantom day into the real calendar.</summary>
        public System.DateTime CurrentDate { get; private set; } = EpochDate;

        /// <summary>
        /// Continuous Time Migration Phase 0: advances CurrentDate by exactly one in-game day.
        /// Returns true on the day a turn boundary is crossed (every DaysPerTurn-th day since
        /// EpochDate) - the caller (GameController's own Update loop) is still the one that actually
        /// resolves the turn via the existing, UNCHANGED AdvanceTurn(decisions), since only the
        /// caller has visibility into the player's current live PolicyDecision draft. This method
        /// only tracks calendar time and signals when a resolution is due - it never calls
        /// AdvanceTurn itself, and never touches any economic state.
        /// </summary>
        /// <summary>
        /// The sim-side calls one simulated day makes for a country BEYOND <see cref="AdvanceDay"/>
        /// itself, in the order GameController.Update has always made them: the foreign-policy roll,
        /// the nine bill countdowns, and the budget-process date check.
        ///
        /// ⚠ EXTRACTED 2026-08-12 (ruled) after the capture driver re-derived this list by COPYING it,
        /// and the copy immediately proved the point: three separate "never captured" findings in one
        /// day were the driver's day differing from the controller's day (the arm calls, then the
        /// countdowns, then CheckElection — each family found by pinning nothing and measuring). One
        /// method, two callers — GameController.Update and UiScreenshotDriver's state searches — so a
        /// call added here reaches both, which is the entire reason it exists.
        ///
        /// <para>Deliberately NOT here: <c>AdvanceDay</c>, whose turn-boundary return belongs to the
        /// caller; and the controller's <c>CheckElection</c>/<c>UpdateFedChairSelectionState</c>, which
        /// build UI-pending state rather than simulation state. The per-call comments that used to sit
        /// on these ten lines in Update — which calls need a gate re-check and why — are preserved at
        /// the call site, where the gates are.</para>
        /// </summary>
        public void AdvanceCountryDayTick(CountryId countryId)
        {
            if (PlayerMayIntroduce(countryId, CabinetPortfolio.ForeignAffairs, out _)) { TryRollForeignPolicyMeeting(countryId); }   // PS-3c (§630): a meeting is the government's - none rolls for a player country the AI governs
            AdvanceBudgetBillDay(countryId);
            AdvanceUsFundingDay(countryId);   // PS-6 US-22: the US funding horizon - a lapse opens the day after it (a no-op wherever the US Congress votes no budget)
            AdvanceMajorityReconciliationDay(countryId);   // PS-6 US-23: the out-party majority's reconciliation - tabled, voted, on the president's desk (a no-op wherever the US Congress votes no budget)
            AdvanceDebtLimitDay(countryId);   // PS-6 US-24: the debt limit's raise before Congress, and the cut armed while the limit binds (a no-op wherever the US Congress votes no budget)
            AdvanceTaxProgramBillsDay(countryId);
            AdvanceWelfareProgramBillsDay(countryId);
            AdvanceLaborBillDay(countryId);
            AdvanceCrimeJusticeBillDay(countryId);
            AdvanceSectorBillDay(countryId);
            AdvanceTradeBillDay(countryId);
            AdvanceSwfDrawdownBillDay(countryId);
            AdvanceLawBillsDay(countryId);
            TryOpenBudgetProcess(countryId, CurrentDate);
            AdvanceConfidenceDay(countryId);   // PS-3i (§636): a carried motion's week, the discharge and the Speaker's round
        }

        public bool AdvanceDay()
        {
            CurrentDate = CurrentDate.AddDays(1);
            CommitCalendarYear();   // PN-1's driver (§520): the year the pension line's driver reads is the clock's
            NotePlayerPresidency();   // SP-7b: the player-president's office on the day, before anything reads it (nothing where the player stood no candidate)
            // F2 step 4: which day of the turn's year this is (1..DaysPerTurn) - the cohort substrate
            // reads its year at this fraction. Derived from the same day count the boundary test uses.
            int dayOfTurn = ((int)(CurrentDate - EpochDate).TotalDays - 1) % DaysPerTurn + 1;

            // Master Sequence step 9, Step A: statistics are published on their real release schedules,
            // which are date-driven ("first Friday", "the 12th", "t+30 after quarter end"), so this is
            // the only correct place for it - once per simulated day, after the date has advanced.
            //
            // PublicationSystem READS country.State and WRITES only country.Published. It returns no
            // value and nothing below consumes its output, so it cannot influence the turn boundary
            // computed here or anything AdvanceTurn later does. Its revision noise draws from
            // SimulationRandom's own PublicationRevision stream, so it cannot perturb any other
            // consumer's draw sequence either.
            if (_world != null)
            {
                foreach (Country country in _world.Countries)
                {
                    // A-LOOP q37 (`docs/specs/A_LOOP_PLAN.md` §2 (2)): first in the day - on the fiscal year's first day its booked net emptied (every line at its opening) before any of the day's moves
                    // (the boundary's decision and the bills run after this loop), then the tax term's fading handed back to approval, recorded
                    TaxTerm.AdvanceDay(country, CurrentDate);
                    EventSalience.AdvanceDay(country);   // A-LOOP q5 (§6 (2)): what events hold of the country's salience decays back toward the seed, first in the day (nothing while nothing stands)
                    PublicationSystem.PublishDueFigures(country, CurrentDate);

                    // Step C4's scheduled rating review, deliberately AFTER the day's closings are
                    // recorded above so a review landing on a period boundary reads that period's own
                    // closing rather than the previous one. It reads State/Published and writes only
                    // Country.Rating, and draws no randomness. Since MF q33 one reader consumes the rating:
                    // the debt crisis below, whose trigger is a review that leaves it at CCC (K141; the USA read without its reserve discount, K212).
                    CreditRatingSystem.ReviewIfDue(country, CurrentDate);
                    SovereignCrisis.AdvanceDay(country, CurrentDate);   // MF q33 (K141-K144): the loss of market access at CCC, the default's or the programme's day
                    Elections.ApprovalVote.NoteGovernment(country, CurrentDate);   // A-LOOP q15 (§3 (2)): net approval marked the first day a government is seen - the approval term's reference; writes nothing else

                    // CONTINUOUS TIME PHASE 1 (2026-08-02): Sectors and Infrastructure now evolve DAILY.
                    // This is the first economic state AdvanceDay has ever touched - Phase 0 deliberately
                    // moved only the calendar - so the ordering guarantee that made publishing safe no
                    // longer holds for these two, and they are placed after it on purpose: publication
                    // reads a day's state, so it must see the state that day actually produced.
                    //
                    // Both are aggregation-equivalent to their turn forms by construction rather than by
                    // tolerance; see the two Daily methods for the per-constant reasoning.
                    MacroSystem.ApplySectorEffectsDaily(country);
                    MacroSystem.ApplyInfrastructureConditionDaily(country);

                    // CONTINUOUS TIME PHASE 4 (2026-08-16): Demographics daily. Rates BEFORE population
                    // (CohortDemographics.Apply reads the same-step freshly-updated Birth/Death/Migration,
                    // the turn form's own documented contract), and BOTH before the Phase 2 block below
                    // (ApplyLaborForceParticipationRateDaily reads DependencyRatio/NetMigrationRate gaps -
                    // it now sees them move daily instead of jumping once per turn, which is the point).
                    // The boundary-day ordering contract survives unchanged: AdvanceDay completes before
                    // AdvanceTurn, so ResolveSpendingForTurn still reads a DependencyRatio that finished
                    // this period's accumulation. One deliberate 1/365 timing shift, stated: a
                    // Family/Immigration policy change committed at a boundary reaches the rates from the
                    // NEXT day rather than the same instant - Phase 3's cash-lands-next-period precedent
                    // in miniature.
                    // F2 step 4 (2026-09-02): the two scalar rules are gone. The cohort substrate's
                    // year is read at today's fraction of the turn (Population and DependencyRatio
                    // interpolated between the year's ends, the rates as the year's flows); the
                    // pyramid itself commits once, in AdvanceTurn. Day DaysPerTurn equals the turn
                    // form exactly, so the equivalence check still holds by construction.
                    CohortDemographics.ApplyDaily(country, CohortDemographics.SubstrateYear(CurrentTurn), dayOfTurn);

                    // CONTINUOUS TIME PHASE 2: Labor Market and Crime & Justice. The ORDER matters and is
                    // preserved exactly from AdvanceTurn - OrganizedCrime and Corruption run BEFORE
                    // CrimeIndex, which reads that day's freshly-updated OrganizedCrimeIndex, and
                    // ApplyCrimeEffects runs after all three because it reads all of their gaps.
                    // Phase 3, part 1: PovertyRate's reversion.
                    MacroSystem.ApplyPovertyRateDaily(country);
                    MacroSystem.ApplyLaborForceParticipationRateDaily(country);
                    MacroSystem.ApplyOrganizedCrimeIndexDaily(country);
                    MacroSystem.ApplyCorruptionIndexDaily(country);
                    MacroSystem.ApplyCrimeIndexDaily(country);
                    MacroSystem.ApplyCrimeEffectsDaily(country);
                    MacroSystem.ApplyPrisonPopulationRateDaily(country);

                    // ROUND 4 BATCH 1 (C3): daily-native from day one, per the foundations gate.
                    // Youth unemployment AFTER the Phase 5 macro block below would read yesterday's
                    // headline... it sits here, BEFORE the macro block, so it reads the same
                    // start-of-day Unemployment its inputs-only contract names - and life expectancy
                    // reads the PovertyRate the poverty daily above just settled. Neither writes
                    // anything any later system reads (inputs-only, the ruled Round 4 posture).
                    MacroSystem.ApplyYouthUnemploymentDaily(country);
                    MacroSystem.ApplyLifeExpectancyDaily(country, MortalitySchedule.YearOf(CurrentDate));   // MF q29: derived from the sourced schedule at today's year

                    // ROUND 4 BATCH 2 (C2): same slot, same reasoning - Gini reads the same
                    // start-of-day Unemployment youth-U reads; the wage index reads yesterday's
                    // settled Inflation/Expectations and the structural trend rate. Neither writes
                    // anything any later system reads (inputs-only, the ruled posture).
                    MacroSystem.ApplyGiniDaily(country);
                    // Q5: wages read trend + productivity's hoarding cycle, and the cycle takes the
                    // PERIOD-OPEN unemployment as its anchor - the same fixed reference Okun already
                    // uses, applied here preemptively because a daily-moving driver inside a power
                    // slice is Q2's measured failure shape.
                    MacroSystem.ApplyRealWageIndexDaily(country, GetOrSeedFiscalPeriod(country).UnemploymentAtPeriodOpen);

                    // ROUND 4 BATCH 3 (C1): same slot; all three read the policy rate against the
                    // zone's epoch anchor (the arc's first monetary coupling - one-way, stated at
                    // MacroSystem's C1 header) and write nothing any later system reads. Overburden
                    // early-outs for the USA per the recorded asymmetry ruling.
                    MacroSystem.ApplyHousingOverburdenDaily(country);
                    MacroSystem.ApplyHomeownershipDaily(country);
                    MacroSystem.ApplyHousePriceIndexDaily(country);

                    // ROUND 4 BATCH R4-5 (C5): same slot, the arc's last stat - pure trend
                    // compounding, reads PotentialGrowthRate only, nothing consumes it (the
                    // coupling is ruled out of Round 4; see MacroSystem's C5 header).
                    MacroSystem.ApplyProductivityDaily(country, GetOrSeedFiscalPeriod(country).UnemploymentAtPeriodOpen);

                    // CONTINUOUS TIME PHASE 3, part 2: the money resolution. Revenue, benefits, welfare,
                    // interest, the SWF's contribution/return/draw and the debt stock itself all move
                    // daily now; only the BUDGET RESOLUTION that decides what to spend stays on the turn
                    // boundary, because a budget passing is an event on a date rather than a flow.
                    //
                    // Placed last in the day so it charges interest against the debt every earlier
                    // system has finished with, and reads the same GDP the rest of the day did.
                    AccrueDailyFiscalFlows(country);
                    Elections.FiscalVote.NoteDay(country, GetOrSeedFiscalPeriod(country).AccruedBudgetBalance, CurrentDate);   // A-LOOP q13 (§4 (2)): the open year's flow to date mirrored for the vote - reads the period, writes only the vote's book
                    UsDebtLimit.Track(country, CurrentDate, EpochDate, UsBudgetProcedure.Applies(country));   // PS-6 US-24: the debt limit's day, after the day's flows - the law read, a suspension's end, the reach, the AI-governed USA's raise where no Congress of the game votes; writes only the limit's fields (nothing where the country is not the USA)

                    // CONTINUOUS TIME PHASE 5 (2026-08-16): the core macro engine, daily - the last
                    // and riskiest conversion, deliberately last. Order preserved from AdvanceTurn's
                    // own sequence: identity (reading the CURRENT period's planned G - the same plan
                    // the fiscal accrual above spends - and the live zone rate), then trend growth,
                    // then Okun from the day's realized growth (annualized for the gap, sliced back
                    // inside), then Phillips (a level map, same form both regimes), then expectations.
                    // The day's growth is measured across the WHOLE day-step so far... deliberately
                    // NOT: it is measured across this block alone (gdp before the identity vs after),
                    // matching the turn form's own "growth produced by the identity" semantics rather
                    // than folding in event shocks, which land at boundaries and decay through the
                    // identity exactly as they always did.
                    FiscalPeriod macroPeriod = GetOrSeedFiscalPeriod(country);
                    EnergyReliability.RestoreOutput(country);   // MF q26: yesterday's unserved-energy overlay off first (it went on after the lapse's), so the two come off exact
                    UsLapse.RestoreOutput(country);   // PS-6 US-22b: yesterday's lapse overlay off before the identity reads GDP (nothing where none is applied)
                    float gdpBeforeMacroStep = country.State.GDP;
                    MacroSystem.ApplyEventShockDaily(country);   // FT-9 (§404): the year's event enters here, inside the increment Okun reads - the boundary no longer moves GDP
                    SovereignCrisis.ApplyOutputCostDaily(country);   // MF q33 (the q32 review's M2): the default's output cost lands where the event's does
                    float anchoredPotential = macroPeriod.PotentialGdpAtPeriodOpen > 0f
                        ? macroPeriod.PotentialGdpAtPeriodOpen
                        : country.State.PotentialGDP;
                    float gdpBeforeIdentity = country.State.GDP;
                    MacroSystem.ApplyNationalAccountsDaily(country, MacroSystem.IdentityGovernmentConsumption(country, macroPeriod.PlannedGovernmentSpending), country.CurrencyZone.InterestRate, anchoredPotential, macroPeriod.WageGrowthGapAtPeriodOpen);
                    if (dayOfTurn == DaysPerTurn) { MacroSystem.CloseDemandReference(country, gdpBeforeIdentity, anchoredPotential); }   // K209: the period's close moves the demand reference   // P5-B6: G is a nominal budget; the identity is real - deflate it by today's price level. T-3 form A (§507): scaled to the sourced share by k
                    MacroSystem.ApplyPotentialGdpGrowthDaily(country);
                    // The day's growth increment is measured against the PERIOD-OPEN GDP, not the
                    // day's own base - the third fixed reference of this phase: daily linear
                    // increments over a fixed denominator sum EXACTLY to the turn form's linear
                    // period growth, where per-day-base percents sum to log growth and leave a
                    // second-order Okun residual that failed the bar on the seeded US output gap
                    // (measured 0.21 unemployment points at a 2% drive). Still causal - the
                    // increment is today's.
                    float openGdp = macroPeriod.GdpAtPeriodOpen > 0f ? macroPeriod.GdpAtPeriodOpen : gdpBeforeMacroStep;
                    float annualizedDailyGrowth = (country.State.GDP - gdpBeforeMacroStep) / Mathf.Max(openGdp, 1f) * 100f * DaysPerTurn;
                    MacroSystem.ApplyOkunsLawDaily(country, annualizedDailyGrowth, macroPeriod.UnemploymentAtPeriodOpen);
                    UsLapse.ApplyOutput(country, CurrentDate);   // PS-6 US-22b: CBO's output effect of a lapse, an overlay on the day's level after Okun has read the identity's growth (nothing where nothing is lapsed or owed)
                    EnergyReliability.ApplyOutput(country);   // MF q26: the year's unserved energy costs output at the value of lost load - an overlay on the day's level, the lapse's form, after Okun has read the identity's growth (nothing where nothing is unserved)
                    // Pass 6: the period's tariff pass-through rides the level map for the whole
                    // period (a price-LEVEL stance planned at the boundary); what actually printed
                    // is kept on the period so the boundary's expectations step can look through it.
                    macroPeriod.AppliedPriceLevelTermsPp = MacroSystem.ApplyPhillipsCurveInflation(country, macroPeriod.PlannedTariffPassThroughPp + macroPeriod.PlannedEnergyPassThroughPp);   // EN-5: the electricity pass-through rides the same level map
                    MacroSystem.ApplyPriceLevelDaily(country);   // P5-B6: the price level compounds at the inflation just printed - the book's prices
                    // Expectations deliberately absent here - a boundary stance; see MacroSystem's
                    // Phase 5 block comment for the measured failure of the daily form.

                    // MF-13 (K191): approval's daily step - its reversion at 0.81 a quarter and every sustained term, read off the state this day
                    // settled (the misery gaps, crime, corruption, Gini, the dials) and the day's realised growth as Okun just read it
                    MacroSystem.ApplyApprovalDay(country, annualizedDailyGrowth, CurrentDate);

                    // PHASE 4: the history point, moved here from AdvanceTurn - see the comment at its
                    // old site for the finding. After every system, so the day's point is the state the
                    // day actually produced (the publication-placement reasoning). Phase 5 note: with
                    // the macro core daily too, the old "turn-stepped stats jump on the next day's
                    // point" caveat has retired itself - every economic quantity now moves on the day
                    // its point records.
                    country.History.Append(CurrentDate, country.State, country.CurrencyZone.InterestRate);
                }
            }

            // PS-6 US-2 (R-US1 (a)): what the game does not elect is the record's, seated on its record's day - before the campaign and the day's votes read it
            SeatTheRecordOnItsDate();
            // C-R4b step 3: the player's campaign steps after every country's day, on the same date.
            AdvanceCampaign();
            // PS-2 / CL-4: the day the player's country votes, on its own calendar - the controller holds the election on it.
            PollingDayToday = TryPlayerPollingDay(out System.DateTime pollingDay) && pollingDay == CurrentDate;
            // §770 (PS-5 item C4): a round of the player's country's presidential election, on its own day - the simulation's alone (no verdict to show)
            HoldPresidentialRound();
            // PS-6 US-8: the US presidential count on its polling day, and the House's choice on the count's day where no one has a majority - the verdict at once
            HoldUsPresidentialCount();
            // PS-8 (K159-K163): every other country's elections on its own calendar, as an AI country - counted, seated and formed in this world (SimulationManager.WorldCalendar.cs)
            HoldWorldCalendar();
            // AF-7 (q7, q10): a central bank's head whose statutory term ended today is replaced - by the AI at once, or by the player's pick (the hold)
            AppointCentralBankHeadsWhoseTermsEnded();

            int daysSinceEpoch = (int)(CurrentDate - EpochDate).TotalDays;
            return daysSinceEpoch > 0 && daysSinceEpoch % DaysPerTurn == 0;
        }

        [SerializeField]
        private World _world;

        public World World => _world;

        // --- Fiscal accounting: automatic stabilizers + sovereign risk premium on debt ---

        /// <summary>
        /// MF q32 (ruled 2026-10-18 q32: "the premium cap is replaced by a sourced convex spread"): THE SOVEREIGN SPREAD IS CONVEX IN DEBT. Pamies, Carnot and
        /// Patarau (2021), "Do fundamentals explain differences between euro area sovereign interest rates?", European Economy Discussion Paper 141 (stored as
        /// `ElectionsData/macro/raw/q32_debt/ec_dp141_pamies_carnot_patarau_2021.pdf`), Table 1, regression (2), "Debt quadratic": the ten-year spread over
        /// the Bund, in percentage points, carries -0.0281 times debt-to-GDP and 0.000300 times its square (euro area, 2000-19, generalised two-stage least
        /// squares). The European Commission's Debt Sustainability Monitor 2024 bases its own financial-stress premium on the same paper.
        /// SOURCED - Table 1, regression (2)'s debt coefficient (the paper's "gdebt_gdp").
        /// DECLARED (the q32 review's L3): the regression is the euro area's, 2000-19, a spread over the Bund; it is applied to all six countries over each
        /// one's own base rate - to the USA (scaled by its RiskPremiumSensitivity), Sweden and Poland over their own policy rates, and to Germany itself, the
        /// paper's benchmark, as a spread over its own rate (a few hundredths of a point at its seed debt). No estimate for the four outside the sample is used.
        /// </summary>
        private const float SpreadDebtCoefficient = -0.0281f;

        /// <summary>SOURCED - Pamies, Carnot and Patarau (2021), Table 1, regression (2)'s squared-debt coefficient (the paper's "debt_sq"), as above.</summary>
        private const float SpreadDebtSquaredCoefficient = 0.000300f;

        /// <summary>DERIVED from the two coefficients: the debt ratio at which the quadratic's slope is zero, -b1 / (2 b2) - the paper's own reading, "the
        /// reaction of spreads to government debt becomes positive only for debt ratios above about 50% of GDP". DECLARED: the premium is the quadratic's rise
        /// above its minimum, so it is zero up to this ratio and convex beyond it; the paper's other regressors (the crisis dummy, the VIX, the ECB's purchases)
        /// are not the model's and are not read.</summary>
        private static readonly float SpreadFloorDebtToGdpPercent = -SpreadDebtCoefficient / (2f * SpreadDebtSquaredCoefficient);

        /// <summary>
        /// MF q33 (ruled 2026-10-18 q33: "the clamp no longer absorbs deficits"): the 300 % ceiling on debt is RETIRED - the debt crisis (`SovereignCrisis`,
        /// K141-K144) does what it did. What stands on the positive side is a RUNAWAY GUARD the size of the net-creditor one, set where nothing should ever
        /// reach it: CONVENTION - a bound on the arithmetic, not on the world. What it absorbs is booked (`SovereignCrisisState.GuardAbsorbedBillions`) and
        /// `MoneyIdentityDiagnostic` fails on any of it - a guard that binds is a bug report.
        /// </summary>
        private const float DebtRunawayGuardPercent = 1000f;

        /// <summary>
        /// The fiscal reaction function's slope: how much the effective-revenue multiplier moves per
        /// point of gap between DebtToGdpRatio and the country's own ComfortableDebtToGdpPercent (see
        /// GetFiscalReactionMultiplier). This is the missing NEGATIVE feedback the debt-to-GDP system
        /// previously lacked - GetDebtRiskPremium is a real, separate mechanism (the market's own cost
        /// of lending more to an already-indebted borrower) and stays completely unchanged; it was
        /// never a substitute for a government's own countercyclical fiscal response, which is what
        /// this represents. See "Fiscal Reaction Function" in CLAUDE.md for the empirical case this
        /// value was calibrated against (every one of 0.05-0.3 failed to escape the pre-existing
        /// bimodal 0%/~294% outcome; 1.0+ genuinely stabilizes all six countries at distinct,
        /// country-appropriate levels, confirmed flat from turn 500 through turn 2000).
        /// </summary>
        // ⚠ FRF SWEEP (2026-08-16): `static` fields rather than `const`, because the ruled real-Unity
        // sweep of this PAIR (the harness-fitted values the record itself says to re-derive) needs to
        // vary them per run and a const is folded at compile time. The DEFAULTS are the standing
        // values, bit-identical in normal play; ONLY FrfSweepDiagnostic may write them, through
        // SetFiscalReactionPairForSweep below, and always restores the defaults. The doc comments
        // above and below carry the original calibration story unchanged - including that it was
        // fitted in the harness whose four-significant-figure stability claim real Unity refuted.
        /// <remarks>[AUTHORED-DRAFT], FITTED - the comment above carries the original calibration story unchanged, including that it was fitted in a harness whose four-significant-figure stability claim real Unity later refuted. Fitted is not sourced, and the mark says which it is.</remarks>
        internal const float DefaultFiscalReactionSensitivity = 1.5f;
        /// <remarks>[AUTHORED-DRAFT], FITTED - the comment above carries the original calibration story unchanged, including that it was fitted in a harness whose four-significant-figure stability claim real Unity later refuted. Fitted is not sourced, and the mark says which it is.</remarks>
        internal const float DefaultMinFiscalReactionMultiplier = 0.5f;
        /// <remarks>[AUTHORED-DRAFT], FITTED - the comment above carries the original calibration story unchanged, including that it was fitted in a harness whose four-significant-figure stability claim real Unity later refuted. Fitted is not sourced, and the mark says which it is.</remarks>
        internal const float DefaultMaxFiscalReactionMultiplier = 1.5f;
        private static float FiscalReactionSensitivity = DefaultFiscalReactionSensitivity;
        private static float MinFiscalReactionMultiplier = DefaultMinFiscalReactionMultiplier;
        private static float MaxFiscalReactionMultiplier = DefaultMaxFiscalReactionMultiplier;

        /// <summary>SWEEP-ONLY hook - see the field block above. PUBLIC because the sweep lives in
        /// the Editor assembly (the ApplyPeriodFiscalStepForValidation precedent); nothing in play
        /// code calls it. The revenue-capacity wall is enforced HERE, not left to the caller's
        /// discipline: an upper bound meaningfully above 1.5 asserts something false about fiscal
        /// capacity (the standing ruling), so this throws rather than accepts one.</summary>
        public static void SetFiscalReactionPairForSweep(float sensitivity, float minMultiplier, float maxMultiplier)
        {
            if (maxMultiplier > 1.5001f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(maxMultiplier),
                    "The revenue-capacity wall: no upper bound meaningfully above 1.5 (ruled).");
            }

            FiscalReactionSensitivity = sensitivity;
            MinFiscalReactionMultiplier = minMultiplier;
            MaxFiscalReactionMultiplier = maxMultiplier;
        }

        public static void ResetFiscalReactionPair()
        {
            FiscalReactionSensitivity = DefaultFiscalReactionSensitivity;
            MinFiscalReactionMultiplier = DefaultMinFiscalReactionMultiplier;
            MaxFiscalReactionMultiplier = DefaultMaxFiscalReactionMultiplier;
        }

        /// <summary>
        /// The missing negative feedback in the debt-to-GDP system: a country's own government
        /// modestly tightens (collects relatively more of its theoretical tax revenue) as debt rises
        /// above its ComfortableDebtToGdpPercent anchor, and loosens (collects relatively less) as debt
        /// falls below it - independent of, and stacked on top of, CollectionEfficiency and
        /// GetDebtRiskPremium (which represents the market's side of the equation, not the
        /// government's). Without this, the system was found to be bimodal - every country's
        /// DebtToGdpRatio eventually settled at either exactly 0% or pinned near the 300% ceiling,
        /// never anything in between, however Discretionary/Mandatory spending growth was tuned (see
        /// "SpendingLine Amount Ceiling - Debt-to-Zero Fix" in CLAUDE.md for that investigation).
        /// </summary>
        private float GetFiscalReactionMultiplier(Country country)
        {
            if (HoldsNoPolicyChange(country)) { return 1f; }   // K218: the no-policy-change book collects its revenue with no reaction to its debt
            float debtGap = country.State.DebtToGdpRatio - country.ComfortableDebtToGdpPercent;
            float multiplier = 1f + FiscalReactionSensitivity * debtGap / 100f;
            return Mathf.Clamp(multiplier, MinFiscalReactionMultiplier, MaxFiscalReactionMultiplier);
        }

        /// <summary>Sane bounds for a country's own tariff policy (see PolicyDecision.TariffRateChange).</summary>
        public const float MinBaseTariffRate = 0f;   // AF-11 (L5): public - the UI's dials and the override's verb read these one pair
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        public const float MaxBaseTariffRate = 50f;

        /// <summary>Bounds for a WelfareProgram's GenerosityLevel - uniform across every WelfareProgramType (unlike TaxLine's per-type MinRate/MaxRate), since the task specifies a single 0-100% range for all six.</summary>
        private const float MinGenerosityLevel = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxGenerosityLevel = 100f;

        /// <summary>Bounds for Country.MinimumWagePercentOfMedian (a Kaitz-index-style percent of median wage) - a gameplay ceiling above any real-world minimum wage's Kaitz index, not a researched maximum.</summary>
        private const float MinMinimumWagePercent = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxMinimumWagePercent = 100f;

        /// <summary>Bounds for Country.PoliceFundingLevel/SentencingSeverity - both share one uniform 0-100 range (unlike TaxLine's per-type ranges), since there's no per-country real-world figure to bound them against.</summary>
        private const float MinPolicyDialLevel = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxPolicyDialLevel = 100f;

        /// <summary>Bounds for Sector.SubsidyLevel/RegulationLevel - reuses the same [0,100] range as the crime/justice dials, since there's likewise no per-sector real-world figure to bound them against.</summary>
        private const float MinSectorDialLevel = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxSectorDialLevel = 100f;

        /// <summary>
        /// Bounds for SovereignWealthFund.ContributionRatePercent - a gameplay ceiling (10% of GDP/
        /// turn is already an aggressive contribution rate for any country), not a researched
        /// maximum. The negative half of this range (Round 3 item 1, the SWF drawdown mechanic) is
        /// the same magnitude as the positive half, deliberately - the player might reasonably want
        /// to unwind an aggressive contribution habit just as quickly as they built it during a real
        /// emergency, and this is a policy LEVER the player chooses to pull, not an automatic
        /// recession-triggered drawdown, so no separate, narrower cap was invented for this pass.
        /// </summary>
        private const float MinSwfContributionRate = -10f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxSwfContributionRate = 10f;

        /// <summary>AF-9: the contribution rate a bill's figure lands at - the clamp <see cref="ApplySwfPolicyChanges"/> applies - so the vote weighs the
        /// rate the bill would set (`ParliamentSystem.FundTermsOf`).</summary>
        public static float LandedSwfContributionRate(float requested) => Mathf.Clamp(requested, MinSwfContributionRate, MaxSwfContributionRate);

        /// <summary>Bounds for SovereignWealthFund's DomesticAllocationPercent and four asset-class weights - shares the same [0,100] range idiom as the other uniform policy dials in this session's work.</summary>
        private const float MinSwfDialLevel = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxSwfDialLevel = 100f;

        /// <summary>Bounds for Country.PaidFamilyLeaveWeeks - a gameplay ceiling (104 weeks = 2 years, comfortably above Sweden's real ~69-week benchmark), not a researched maximum.</summary>
        private const float MinPaidFamilyLeaveWeeks = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxPaidFamilyLeaveWeeks = 104f;

        /// <summary>Bounds for Country.OvertimeRegulationLevel/RetrainingProgramLevel - shares the same [0,100] uniform-dial idiom as PoliceFundingLevel/SentencingSeverity.</summary>
        private const float MinLaborDialLevel = 0f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxLaborDialLevel = 100f;


        /// <summary>
        /// Ceiling on SovereignWealthFund.TotalAssets, as a percentage of GDP - it matched the retired
        /// debt ceiling's number for consistency, a gameplay safety bound not a realistic
        /// target (found necessary during validation: sustained maximum contribution (10% of GDP)
        /// into 100% Equities, held for 500 turns with no rebalancing, compounds far faster than GDP
        /// forever since the fund's average return (9%) structurally exceeds trend GDP growth - left
        /// unclamped, this drove the budget's cumulative total to an astronomically large (still
        /// finite, but unrealistic) figure within a few hundred turns. MF q33 (ruled 2026-10-18 q33:
        /// "The fund ceiling refuses a contribution at booking"): it no longer clamps the stock - a
        /// contribution that would carry the fund past it is refused at booking and stays in the budget
        /// (SwfContributionRefused), and what a return or a fall in GDP leaves above it is paid out into
        /// the budget with the structural draw; both are booked on the country (SovereignCrisisState).
        /// </summary>
        private const float MaxSwfToGdpPercent = 300f;

        /// <summary>
        /// The SWF's STRUCTURAL DRAW into the budget, as a percentage of fund assets per YEAR — Norway's
        /// own fiscal rule (the *handlingsregel*), which permits spending the fund's expected real return
        /// rather than its realised one, whatever the market did that year.
        ///
        /// **This replaces booking the realised return as revenue, and fixes two defects at once
        /// (2026-08-02, Elias's smoothing ruling):**
        ///
        /// 1. **VOLATILITY.** A realised return is market variance arriving in the budget. Sweden's fund
        ///    reaches ~10,900 against a GDP near 1,200, so a single turn's equity swing could exceed 100%
        ///    of GDP — which is the whole of C4's "settled deficit ranges −135.5% to +170.8%" blocker.
        ///    A draw proportional to fund SIZE is smooth by construction.
        /// 2. **DOUBLE-COUNTING.** The realised return was added to `TotalAssets` *and* the same figure
        ///    was then added to revenue — the fund kept it and the government spent it, so the money
        ///    existed twice. The contribution one line above is handled correctly (budget pays, fund
        ///    receives; money MOVES). The draw is now likewise withdrawn from the fund.
        ///
        /// 3% is Norway's own figure. Chosen over a rolling average of realised returns because an
        /// average still transmits the LEVEL of a very large fund into the budget and damps only its
        /// variance — and because this is the rule the real instrument actually uses.
        /// </summary>
        /// <remarks>SOURCED - 3% is Norway's own structural draw figure for the Government Pension Fund Global (handlingsregelen), as the paragraph above records. Chosen because it is the rule the real instrument uses, not because it fits.</remarks>
        private const float SwfStructuralDrawPercentPerYear = 3f;

        /// <summary>The annual structural draw expressed per TURN. A turn is <see cref="DaysPerTurn"/> days, so a year is 365/121 turns - derived rather than hardcoded, because the continuous-time migration will change the turn length and a baked-in constant would silently become a different policy.</summary>
        private static float SwfStructuralDrawPerTurnFraction()
        {
            float turnsPerYear = 365f / DaysPerTurn;
            return SwfStructuralDrawPercentPerYear / 100f / turnsPerYear;
        }

        /// <summary>
        /// How far below zero net government debt may run before it is caught, as a percentage of GDP.
        ///
        /// **This is a RUNAWAY GUARD, not a calibrated bound**, and the distinction is the whole point of
        /// the number being this large. Its job is to stop an unbounded excursion from reaching infinity
        /// during and after the SWF-returns fix; it is NOT meant to shape any live value. Norway - the
        /// real world's most extreme net creditor, and this project's own SWF calibration reference -
        /// sits near -250% of GDP, so nothing plausible comes within a factor of four of this.
        ///
        /// ⚠ **If a country ever reaches it, that is a bug report rather than a clamp working.** The
        /// previous attempt used -300%, which France settled AT (-297.6%) - a bound that binds is a
        /// number the model reads instead of its own state, and C4's rating was reading it.
        /// </summary>
        private const float NetCreditorRunawayGuardPercent = 1000f;

        /// <summary>Bounds for this turn's requested PERCENTAGE change to a Discretionary SpendingLine (see PolicyDecision.SpendingLineChanges).</summary>
        private const float DiscretionaryPercentChangeRange = 30f;

        /// <summary>
        /// Narrower than DiscretionaryPercentChangeRange - reflects the real political difficulty of
        /// entitlement reform (Social Security, Medicare, Medicaid, etc. aren't cut or expanded as
        /// freely, in one turn, as a Discretionary line).
        /// </summary>
        /// <remarks>[AUTHORED-DRAFT] - the paragraph above gives the reasoning (entitlement reform is politically harder than discretionary change, so the range is narrower); how much narrower is a game figure.</remarks>
        private const float MandatoryPercentChangeRange = 15f;
        /// <summary>DERIVED - the same constants under a public name: the AI finance ministry (§388) sizes its cuts against the ranges the player's sliders are clamped to, so one constant rules both.</summary>
        public const float MandatoryPercentChangeRangeForRules = MandatoryPercentChangeRange;
        /// <summary>DERIVED - see MandatoryPercentChangeRangeForRules.</summary>
        public const float DiscretionaryPercentChangeRangeForRules = DiscretionaryPercentChangeRange;

        /// <summary>
        /// AF-9 (Elias's ruling q9, 2026-10-18: "the budget gates move into the model"): THE YEARLY RANGE a bill may SET a spending line's figure in -
        /// this year's allowed change around the line as it stands (±<see cref="MandatoryPercentChangeRange"/> mandatory, ±<see cref="DiscretionaryPercentChangeRange"/>
        /// discretionary), on the line's own path where a dial cost stands outside it (SC-1): the Budget page's track, which until AF-9 was the only place the
        /// range was held, so a figure set off the page reached the book unbounded but for the seed band. A percentage change is clamped to the same range where it
        /// applies (<see cref="ApplySpendingLineChanges"/>); a figure is refused at introduction (<see cref="BudgetBillOutsideYearlyRange"/>).
        /// </summary>
        public static (float Min, float Max) YearlyRangeOf(Country country, SpendingLine line)
        {
            float rangePercent = line.IsMandatory ? MandatoryPercentChangeRange : DiscretionaryPercentChangeRange;
            float standing = line.Amount;
            float min = standing * (1f - rangePercent / 100f), max = standing * (1f + rangePercent / 100f);
            float dialCost = DialCostOf(country, line);
            if (dialCost != 0f)
            {
                float own = standing - dialCost;
                min = LandedTotalOf(country, line, own * (1f - rangePercent / 100f) + dialCost);
                max = LandedTotalOf(country, line, own * (1f + rangePercent / 100f) + dialCost);
            }
            return (min, max);
        }

        /// <summary>CONVENTION (AF-9): the slack a figure at the range's end is allowed, relative to the range - the dial's float, never a policy margin.</summary>
        private const float YearlyRangeSlack = 1e-4f;

        /// <summary>AF-9: the first figure <paramref name="bill"/> sets outside its line's yearly range (<see cref="YearlyRangeOf"/>), as the refusal's words; null
        /// where every figure is inside it.</summary>
        public static string BudgetBillOutsideYearlyRange(Country country, BudgetBill bill)
        {
            if (country == null || bill == null) { return null; }
            foreach (KeyValuePair<SpendingCategory, float> kv in bill.SpendingNominalTargets)
            {
                SpendingLine line = country.SpendingLines.Find(l => l.Category == kv.Key);
                if (line == null) { continue; }
                (float min, float max) = YearlyRangeOf(country, line);
                float slack = YearlyRangeSlack * Mathf.Max(Mathf.Abs(min), Mathf.Abs(max));
                if (kv.Value < min - slack || kv.Value > max + slack)
                {
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} SET AT {1:0.###} · THIS YEAR'S ALLOWED RANGE {2:0.###} - {3:0.###}", kv.Key, kv.Value, min, max);
                }
            }
            return null;
        }

        /// <summary>§388: the AI finance ministry acts for every country the player does not govern. A diagnostic that measures the indexation rule alone
        /// (LineIndexationDiagnostic, SpendingIndexationDiagnostic) or a tax term's inertness (LaborTaxParticipationDiagnostic) sets this false; play never does.</summary>
        public bool AiFinanceMinistryEnabled = true;

        /// <summary>K218 (2026-10-28; Code's recommendation under Elias's standing instruction of 2026-10-27): THE NO-POLICY-CHANGE BOOK - an Editor instrument's
        /// switch, never set by play (default false). CBO's extended baseline projects current law and the EC Debt Sustainability Monitor holds the structural primary
        /// balance unchanged with the cost of ageing on top; neither projects a government correcting its deficit. With it set, the PLAYER'S country (the
        /// instrument's hand, no party seated - the ministry and the Finance partner already keep off it) runs the AI's indexation instead of the player's own
        /// (<see cref="IndexSpendingLines"/>: a driverless line at the economy's real growth, a caseload at the going real wage - the law's reading, §384, where the
        /// player's book holds real provision where the player set it), collects its revenue with no fiscal reaction (<see cref="GetFiscalReactionMultiplier"/>
        /// at 1: the reaction is a government tightening against its own debt, which neither source projects). Every other country is untouched. Read by
        /// `NoPolicyCentury.Unchanged` for `DebtPathHoldDiagnostic`.</summary>
        public bool NoPolicyChangeBook;

        /// <summary>K218: true for the player's country while <see cref="NoPolicyChangeBook"/> is set - the one test its two readers share.</summary>
        private bool HoldsNoPolicyChange(Country country) => NoPolicyChangeBook && country != null && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id;

        /// <summary>
        /// Hard floor/ceiling on every SpendingLine's Amount, expressed as a multiple of that line's
        /// own SpendingLine.SeedAmount - not of its current Amount. Percentage-of-current-value changes
        /// compound geometrically if the same large percentage is held for many turns in a row (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md for the runaway-divergence finding this
        /// closes off); anchoring the clamp to SeedAmount, rather than letting it ride with the current
        /// Amount, is what actually stops the compounding - a clamp relative to the current value would
        /// just get carried along by the same exponential growth it's supposed to bound. Applies to
        /// both Discretionary (ApplySpendingLineChanges) and Mandatory categories' PLAYER-driven
        /// changes, regardless of how many turns of repeated changes are stacked. SC-1 (ruled 2026-09-15, §503): the band
        /// bounds a line's OWN path; an applied dial cost - a figure the player set by law or bill - sits outside it, and
        /// the line is the clamped own path plus the cost (SimulationManager.ComposeLine).
        ///
        /// For a Discretionary line, SeedAmount is NOT frozen at construction - ApplyDiscretionarySpendingGrowth
        /// grows it in lockstep with the automatic GDP-tracking growth applied to Amount (see that
        /// method's doc comment for why this was necessary - an earlier version left SeedAmount fixed
        /// forever, which silently froze this ceiling in absolute dollar terms and broke the "G tracks
        /// GDP" property "Discretionary Spending Growth" depends on; see "SpendingLine Amount Ceiling
        /// - Debt-to-Zero Fix" in CLAUDE.md). A Mandatory line's SeedAmount stays genuinely fixed at
        /// construction, since Mandatory lines have no automatic growth mechanism to track in the
        /// first place.
        /// </summary>
        private const float MinSpendingLineAmountRatio = 0.2f;
        /// <remarks>CONVENTION - a policy-dial bound. It exists so a slider has ends and a feedback bug cannot run past them; the summary above says which range it shares with which other dials, and none of these is a claim about how far a real government could go.</remarks>
        private const float MaxSpendingLineAmountRatio = 3.0f;

        private static float ClampToSeedRange(SpendingLine line, float amount)
        {
            return Mathf.Clamp(amount, line.SeedAmount * MinSpendingLineAmountRatio, line.SeedAmount * MaxSpendingLineAmountRatio);
        }

        private readonly Dictionary<CountryId, FiscalTurnReport> _lastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>();
        private readonly Dictionary<CountryId, EconomicEvent> _lastEventsByCountry = new Dictionary<CountryId, EconomicEvent>();

        /// <summary>
        /// CONTINUOUS TIME PHASE 3: the fraction of a period-shaped flow that lands in one day.
        ///
        /// Translation shape #1, LINEAR. A budget flow is an accumulating quantity with no target to
        /// revert toward, so the multiplicative form MacroSystem.PerDayReversion uses does not apply -
        /// 121 payments of `flow/121` are exactly one period's payment, which is the whole translation.
        /// Derived from DaysPerTurn rather than typed, like every other daily constant in this migration.
        /// </summary>
        private const float FiscalFlowPerDayFraction = 1f / DaysPerTurn;

        /// <summary>
        /// CONTINUOUS TIME PHASE 3: one country's in-flight fiscal period - the spending plan its
        /// current 121 days are executing, plus the running sum of what those days have actually accrued.
        ///
        /// **This is the "NEW PER-COUNTRY STATE" the Phase 3 handoff named as the blocker**, and it
        /// exists because the daily step cannot re-derive everything it needs. Tax revenue, unemployment
        /// benefits, welfare cost, interest and the SWF contribution are all functions of persistent
        /// country state, so AccrueDailyFiscalFlows recomputes each of them from scratch every day.
        /// Government and Mandatory spending are NOT: for the five countries without a detailed
        /// SpendingLines portfolio the discretionary figure comes from
        /// PolicyDecision.TotalDiscretionarySpending, which exists only at a turn boundary and is
        /// persisted nowhere else. ResolveSpendingForTurn's answer is stored here so the days that follow
        /// can spend it.
        ///
        /// **A PLAN IS RESOLVED AT ONE BOUNDARY AND EXECUTED OVER THE NEXT 121 DAYS.** That ordering is
        /// forced rather than chosen - the boundary that resolves a budget is 121 days of play before the
        /// next one - and it is also what a budget is: a government passes it on a date and then spends
        /// it. The consequence worth stating plainly is that a policy change's CASH effect now lands one
        /// period after the boundary that made it, where it used to land instantly. Its effect on the GDP
        /// identity's G term does NOT move - MacroSystem.ApplyNationalAccounts still reads the plan at the
        /// boundary that resolved it - because the core macro engine is Phase 5, not this phase.
        ///
        /// **THE SWF RETURN IS DRAWN ONCE PER PERIOD AND ACCRUED DAILY** (decision recorded 2026-08-02 by
        /// the Phase 3 part 1 commit, flagged for overrule and left standing here). Drawing daily would
        /// consume 121x the RNG and invalidate every recorded baseline in the project, for no modelling
        /// gain - the draw's granularity is not what this migration is about.
        /// </summary>
        // PUBLIC since save/load (item 8, 2026-08-16): SimulationPendingState carries these whole -
        // plan, frozen multiplier and mid-period accruals - because a save taken mid-period that
        // dropped the accruals would close its next boundary against a partial year. Public-NESTED,
        // not promoted to its own file: nothing but this class and the save shape may construct one.
        public class FiscalPeriod
        {
            // THE PLAN - set by the boundary that opened this period, spent across its days.
            public float PlannedGovernmentSpending;
            public float PlannedMandatorySpending;
            public float PlannedBaselineGovernmentSpending;
            public float PlannedDiscretionarySpending;
            public float PlannedSwfReturn;

            /// <summary>AF-35 (Elias's ruling q35, 2026-10-18: "a fund earns on what it holds"): the fund's assets when <see cref="PlannedSwfReturn"/> was
            /// drawn on them - the boundary that opened this period, or the seed. A day's slice of the return is earned on the share of these the fund still
            /// holds as principal, the period's own returns excluded (`SwfReturnNotEarned`), so an emptied fund earns nothing from the day it is empty. Zero where no fund stood; a zero from an older save
            /// degrades to the flat slice for the loaded period's remainder (an empty fund still earns nothing) - the WageGrowthGapAtPeriodOpen posture.</summary>
            public float SwfAssetsAtPeriodOpen;

            /// <summary>Pass 5 (2026-08-26): the period's tariff revenue - TradeSystem's figure at the
            /// boundary that opened this period (the seed period reads the same pure function before
            /// any turn), accrued daily inside ApplyRevenueAndSpending as one more revenue term. A
            /// zero from a pre-pass-5 save degrades to "no tariff flow for the loaded period's
            /// remainder", self-correcting at the next boundary - the WageGrowthGapAtPeriodOpen
            /// posture, no guard needed.</summary>
            public float PlannedTariffRevenue;

            /// <summary>P5-B6: the same take at the seed's prices - what the pass-through measures its CHANGE against, so a
            /// price level the nominal figure carries never reads as a tariff move. Zero from an older save degrades to the
            /// nominal figure deflated by today's level (RealPlannedTariffRevenue).</summary>
            public float PlannedTariffRevenueReal;

            /// <summary>Pass 6 (2026-08-27): the tariff pass-through planned for this period -
            /// TradeCosts.ImportPricePassThrough x 100 x (this boundary's take - the closing period's
            /// planned take) / GDP, in inflation points, read every day by the Phillips level map. Exactly
            /// 0 when no rate changed (the same pure sum on unchanged state). An old-save zero degrades to
            /// "no pass-through for the loaded period's remainder" - the WageGrowthGapAtPeriodOpen posture,
            /// no guard.</summary>
            public float PlannedTariffPassThroughPp;

            /// <summary>EN-5 (2026-09-11): the electricity pass-through planned for this period - electricity's weight in the
            /// consumer price index times the household retail price's change relative to the general price level over the
            /// year the boundary just wrote (EnergyPassThrough.Planned), in inflation points, read every day by the Phillips
            /// level map beside the tariff term. Zero where no ledger covers the country; zero to the noise at no policy.</summary>
            public float PlannedEnergyPassThroughPp;

            /// <summary>EN-7b (2026-09-15): the period's electricity-tax receipts above the statute's base, nominal billions - the book the ledger wrote at the
            /// boundary that opened this period (EnergyLedger.AdvanceYear's return), accrued daily inside ApplyRevenueAndSpending beside the tariff flow: inside the
            /// fiscal-reaction multiplier, outside CollectionEfficiency (the seeded 2023 taxes are inside its coverage bridge; this is only the change). Zero at the seed,
            /// and a zero from an older save degrades to "no flow for the loaded period's remainder" - the tariff flow's posture, no guard.</summary>
            public float PlannedElectricityTaxRevenue;

            /// <summary>Pass 6: what of the planned level terms ACTUALLY printed on the latest day
            /// (ApplyPhillipsCurveInflation's return - the clamped print with the terms minus the clamped
            /// print without them; the tariff and, since EN-5, the electricity pass-through together). The
            /// boundary reads the closing day's value into the FiscalTurnReport and into
            /// ApplyInflationExpectations' look-through, so a cut whose negative wedge floors the print at 0
            /// cannot ratchet expectations. Overwritten daily; nothing reads it between the boundary and the
            /// next day, so ResetAccrual leaves it alone.</summary>
            public float AppliedPriceLevelTermsPp;

            /// <summary>
            /// GetFiscalReactionMultiplier as it stood when this period opened, held FIXED for its whole
            /// 121 days - and this is the one Phase 3 constant that is a modelling call rather than a
            /// mechanical translation.
            ///
            /// **Recomputing it daily was tried first and FAILED the aggregation bar outright** (Sweden
            /// 24.8% drift on budget balance, Germany 22.7%, against a 3% bar). The cause is not a bug:
            /// FiscalReactionSensitivity is 1.5 and a single period moves a country's debt ratio by ten
            /// points or more, so a multiplier that re-reads the ratio every day walks a long way down
            /// its own surplus during the period it is supposed to be governing. The turn form could not
            /// do this - its debt stock moved exactly once - so daily recomputation was not a finer
            /// version of the validated model, it was a different one.
            ///
            /// Freezing it is also the more defensible reading of what the mechanism IS. Its own doc
            /// comment describes "a country's own government modestly tightens... as debt rises above its
            /// ComfortableDebtToGdpPercent anchor" - a fiscal STANCE, and a stance is adopted when the
            /// budget is set, not re-derived every morning. The stance still responds fully to the debt
            /// the country has actually accumulated; it does so at the boundary, which is where every
            /// other budget decision in this game is made.
            /// </summary>
            public float PlannedFiscalReactionMultiplier;

            /// <summary>PHASE 5: GDP as the period opened. With the identity daily, "this turn's
            /// realized growth" (ApprovalRating's input) is the PERIOD's growth, measured from here -
            /// the top-of-AdvanceTurn snapshot the turn form used no longer exists, because by the
            /// boundary the days have already moved GDP. A zero (an old save from before this field)
            /// reads as "no growth measurable yet" via the guard at the read site.</summary>
            public float GdpAtPeriodOpen;

            /// <summary>PHASE 5: unemployment as the period opened - the FIXED REFERENCE for Okun's
            /// distributed reversion (the FRF frozen-stance pattern; see ApplyOkunsLaw's own Phase 5
            /// comment for the measured failure of the self-referencing form). A zero from an old
            /// save degrades to "revert toward NAIRU from zero" for one period's remainder - visible
            /// and self-correcting at the next boundary, preferred over a special case.</summary>
            public float UnemploymentAtPeriodOpen;

            /// <summary>PHASE 5: PotentialGDP as the period opened - the identity's attractor anchor
            /// (the fourth fixed reference; see ApplyNationalAccountsDaily for the measured Okun
            /// amplification that a live attractor causes). Old-save zero degrades through the same
            /// guard the identity call site applies to GdpAtPeriodOpen.</summary>
            public float PotentialGdpAtPeriodOpen;

            /// <summary>Q2: the wage-growth gap (pp/turn) as the period opened - the sentiment
            /// factor's anchor (the FIFTH fixed reference; see the anchored
            /// EffectiveConsumerConfidence overload for the measured @8%shock divergence a live
            /// gap causes). An old-save zero needs NO guard: gap 0 means factor 1 - the identity
            /// simply reads the base for the loaded period's remainder and self-corrects at the
            /// next boundary, the same degradation posture as UnemploymentAtPeriodOpen.</summary>
            public float WageGrowthGapAtPeriodOpen;

            // THE ACCRUAL - summed day by day, closed out into a FiscalTurnReport at the next boundary.
            public float AccruedRevenue;
            public float AccruedMandatorySpending;
            public float AccruedUnemploymentBenefitCost;
            public float AccruedInterestOnDebt;
            public float AccruedWelfareCost;
            public float AccruedSwfContribution;
            public float AccruedSwfReturns;
            public float AccruedTotalSpending;
            public float AccruedBudgetBalance;
            /// <summary>PS-6 US-24 (the review's M4): the period's debt-limit cut, booked as its own line (inside AccruedTotalSpending, net).</summary>
            public float AccruedDebtLimitCut;
            /// <summary>MF q33 (the q32 review's M3): the period's closed-market cut, its own line (inside AccruedTotalSpending, net).</summary>
            public float AccruedCrisisCut;
            /// <summary>MF q33: what left the fund for the budget in the period - the structural draw and what stood above the ceiling - so the fund's own
            /// identity (assets at the close = at the open + contributions booked + returns landed - this) can be read off the period.</summary>
            public float AccruedSwfDraw;
            /// <summary>Pass 5: the tariff portion of AccruedRevenue, kept separately so the
            /// FiscalTurnReport can show "of which tariffs" as a true reading of what accrued.</summary>
            public float AccruedTariffRevenue;
            /// <summary>EN-7b: the electricity tax's portion of AccruedRevenue at stance 1, kept for the report's "of which".</summary>
            public float AccruedElectricityTaxRevenue;

            public void ResetAccrual()
            {
                AccruedRevenue = 0f;
                AccruedMandatorySpending = 0f;
                AccruedUnemploymentBenefitCost = 0f;
                AccruedInterestOnDebt = 0f;
                AccruedWelfareCost = 0f;
                AccruedSwfContribution = 0f;
                AccruedSwfReturns = 0f;
                AccruedTotalSpending = 0f;
                AccruedBudgetBalance = 0f;
                AccruedDebtLimitCut = 0f;
                AccruedCrisisCut = 0f;
                AccruedSwfDraw = 0f;
                AccruedTariffRevenue = 0f;
                AccruedElectricityTaxRevenue = 0f;
            }
        }

        private readonly Dictionary<CountryId, FiscalPeriod> _fiscalPeriods = new Dictionary<CountryId, FiscalPeriod>();

        /// <summary>PS-6 US-24 (the review's M4): the debt-limit cut the last ApplyRevenueAndSpending call took - the daily accrual books it as its own line. Not
        /// saved: written and read inside one day's step.</summary>
        private float _debtLimitCutToday;
        private float _crisisCutToday;   // MF q33 (the q32 review's M3): the day's closed-market cut, read by the daily accrual right after ApplyRevenueAndSpending

        /// <summary>
        /// Political Systems Overhaul Part A: cabinet decisions rolled but not yet resolved by a
        /// player-picked response, per country - unlike EconomicEvent (auto-applied, so only ever
        /// this turn's single result needs remembering), these persist across frames until
        /// ResolveCabinetDecision clears them, since GameController blocks Advance Turn while any
        /// remain (mirrors the existing hasPendingFedChairSelection gate - see OnGUI).
        /// </summary>
        private readonly Dictionary<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> _pendingCabinetDecisionsByCountry =
            new Dictionary<CountryId, List<(CabinetPortfolio, CabinetDecision)>>();

        /// <summary>
        /// Continuous Time Migration Phase 0 short-term gameplay scaffolding: at most ONE pending
        /// foreign policy meeting per country at a time (unlike Cabinet's per-minister list, there's
        /// only one "foreign ministry" rolling here) - see TryRollForeignPolicyMeeting/
        /// ResolveForeignPolicyMeeting and GameController.Update's own gate on it.
        /// </summary>
        private readonly Dictionary<CountryId, ForeignPolicyMeeting> _pendingForeignPolicyMeetingByCountry =
            new Dictionary<CountryId, ForeignPolicyMeeting>();

        /// <summary>
        /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5a): a country's
        /// annual budget process is open (its FiscalYearData start date has arrived) and the player
        /// hasn't yet introduced a BudgetBill - single-slot per country, mirroring
        /// _pendingForeignPolicyMeetingByCountry's own pattern. This is Phase 1 of the annual cycle
        /// (mandatory pause, blocks GameController.Update's day-loop); IntroduceBudgetBill clears this
        /// AND starts Phase 2 (_pendingBudgetBillByCountry's own 21-day countdown, which does NOT
        /// pause). Only ever set for PlayerCountryId (see TryOpenBudgetProcess) - the other five
        /// countries are AI-controlled, never enter this set, and their own annual budget dates are
        /// currently a no-op (Master Sequence step 5c scope note: no AI budget-drafting logic exists
        /// anywhere in this codebase yet, so there's nothing to bundle into a bill for them - resolve
        /// this properly, not by guessing at AI policy-making, if/when it's actually needed).
        /// </summary>
        private readonly HashSet<CountryId> _pendingBudgetProcessByCountry = new HashSet<CountryId>();

        /// <summary>
        /// C-C2: which countries have already had their INCOMING GOVERNMENT'S budget window — the one
        /// that opens on arrival rather than on the calendar (Playtest-1 finding 4). Presence means
        /// "already granted", so the arrival window fires once and the annual fiscal-year cycle governs
        /// everything after it.
        ///
        /// ⚠ **It is a PERMISSION and nothing else.** Every reader of `_pendingBudgetProcessByCountry`
        /// is `GameController` or `UiScreenshotDriver` — no simulation code reads it, and the only
        /// simulation-side writer is `IntroduceBudgetBill` removing it. That is why opening the window
        /// earlier cannot move a trajectory, and `BudgetWindowDiagnostic` asserts it directly rather
        /// than leaving it to a trajectory diff that would never exercise this code at all.
        /// </summary>
        private readonly HashSet<CountryId> _incomingBudgetWindowUsed = new HashSet<CountryId>();

        /// <summary>C-C2: true while the currently-open process IS the arrival window rather than the
        /// annual cycle - so a screen can name which one it is. PS-6 US-7: PERSISTED since format 39
        /// (`SimulationPendingState.IncomingBudgetWindowOpenNow`). It was not, on the reading that it was derivable from the two sets that are - it is
        /// not (an open process with the used flag set is the arrival window or the annual one alike), so a loaded arrival window read as the annual
        /// one; and the oath reads it (<see cref="TakeOffice"/>: a window still open at a same-party oath is the new government's and earns no second),
        /// so a load that dropped it would hand a loaded game a budget window the continuous run never gets.</summary>
        private readonly HashSet<CountryId> _incomingBudgetWindowOpenNow = new HashSet<CountryId>();

        /// <summary>C-C2: true when the open budget process is the INCOMING GOVERNMENT'S arrival window
        /// (Playtest-1 finding 4) rather than the country's annual fiscal-year cycle. False when no
        /// process is open. A screen that says "only on your fiscal-year date" is wrong during this one.</summary>
        public bool IsIncomingGovernmentBudgetWindow(CountryId countryId)
        {
            return _pendingBudgetProcessByCountry.Contains(countryId)
                   && _incomingBudgetWindowOpenNow.Contains(countryId);
        }

        /// <summary>
        /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5c): at most one
        /// pending omnibus BudgetBill per country, mirroring _pendingForeignPolicyMeetingByCountry's
        /// own single-slot pattern - Phase 2 of the annual cycle (see _pendingBudgetProcessByCountry's
        /// own doc comment). Counts down via AdvanceBudgetBillDay (called once per simulated day,
        /// independent of the 121-day turn boundary), the same deterministic-countdown idiom the
        /// Master Sequence step 4 pilot's TaxBill/AdvanceLegislativeDay already established (retired -
        /// see git history - BudgetBill generalizes it to Tax+Spending+Welfare+SWF together).
        /// </summary>
        /// <summary>PS-3e (§632): the ALTERNATIVE budget tabled against the government's pending bill - the Riksdag's budget motion; one per country, voted with the government's on its day.</summary>
        private readonly Dictionary<CountryId, BudgetBill> _pendingBudgetAlternativeByCountry = new Dictionary<CountryId, BudgetBill>();

        private readonly Dictionary<CountryId, BudgetBill> _pendingBudgetBillByCountry =
            new Dictionary<CountryId, BudgetBill>();

        /// <summary>
        /// Master Sequence step 5d, tier 2: standalone program add/remove bills, keyed by CountryId
        /// then by the specific TaxType/WelfareProgramType being added or removed - unlike
        /// _pendingBudgetBillByCountry's single slot, MULTIPLE different program bills can be pending
        /// for the same country at once (e.g. "add UBI" and "remove CarbonTax" simultaneously), just
        /// never two for the SAME program at the same time. Introducible anytime, no mandatory pause -
        /// counts down independently via AdvanceTaxProgramBillsDay/AdvanceWelfareProgramBillsDay, the
        /// same non-blocking daily idiom _pendingBudgetBillByCountry's own Phase 2 already established.
        /// </summary>
        private readonly Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>> _pendingTaxProgramBillsByCountry =
            new Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>();

        /// <summary>WelfareProgramType equivalent of _pendingTaxProgramBillsByCountry - see that field's own doc comment, same pattern.</summary>
        private readonly Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> _pendingWelfareProgramBillsByCountry =
            new Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>>();

        /// <summary>
        /// Law system MVP slice: standalone law bills, keyed by CountryId then by LawId - the SAME
        /// nested-dictionary shape _pendingTaxProgramBillsByCountry/_pendingWelfareProgramBillsByCountry
        /// already use, for the identical reason: multiple different laws (each independently
        /// enacted or repealed) can have their own bill pending for the same country at once, just
        /// never two for the SAME law simultaneously. Introducible anytime, no mandatory pause - the
        /// same non-blocking daily countdown idiom every standalone bill tier already uses. See
        /// LawBill/LawDefinition/LawCatalog.
        /// </summary>
        private readonly Dictionary<CountryId, Dictionary<string, LawBill>> _pendingLawBillsByCountry =
            new Dictionary<CountryId, Dictionary<string, LawBill>>();

        /// <summary>
        /// Master Sequence step 5d, tier 3: at most one pending standalone bill per country per
        /// non-budget policy tab (Labor Market/Crime &amp; Justice/Economic Sectors/Trade), mirroring
        /// _pendingBudgetBillByCountry's own single-slot-per-country pattern - one bill per TAB, not
        /// per dial (see the roadmap's own "one bill per tab" design confirmation). Introducible
        /// anytime, no mandatory pause, same non-blocking daily countdown idiom as the tier-2
        /// dictionaries above.
        /// </summary>
        private readonly Dictionary<CountryId, LaborPolicyBill> _pendingLaborBillByCountry =
            new Dictionary<CountryId, LaborPolicyBill>();

        private readonly Dictionary<CountryId, CrimeJusticePolicyBill> _pendingCrimeJusticeBillByCountry =
            new Dictionary<CountryId, CrimeJusticePolicyBill>();

        private readonly Dictionary<CountryId, SectorPolicyBill> _pendingSectorBillByCountry =
            new Dictionary<CountryId, SectorPolicyBill>();

        private readonly Dictionary<CountryId, TradePolicyBill> _pendingTradeBillByCountry =
            new Dictionary<CountryId, TradePolicyBill>();

        /// <summary>Elias's A2 ruling: the SWF emergency drawdown is a fifth standalone tier-3 bill, so it gets its own pending slot exactly like the other four.</summary>
        private readonly Dictionary<CountryId, SwfDrawdownBill> _pendingSwfDrawdownBillByCountry =
            new Dictionary<CountryId, SwfDrawdownBill>();

        /// <summary>
        /// R-S3e: the foreign-policy pacing multiplier - a challenge scenario's start set it, and a load
        /// re-derived it from the scenario's id, until the scenarios were retired (§825); nothing writes
        /// it now, so it is 1 in every game. **1 leaves the standing cadence exactly as it was.**
        /// </summary>
        public float ForeignPolicyCadenceMultiplier { get; set; } = 1f;

        /// <summary>The most recent turn's fiscal breakdown for a country, or null if no turn has been advanced yet.</summary>
        public FiscalTurnReport GetLastFiscalReport(CountryId countryId)
        {
            return _lastFiscalReports.TryGetValue(countryId, out FiscalTurnReport report) ? report : null;
        }

        /// <summary>
        /// Step 2: the wage-growth gap the daily identity is consuming THIS period - the period-open
        /// stance (the fifth fixed reference), not the live gap. This is the number the trace panel's
        /// single-book confidence line explains, because it is the number the economy actually used.
        /// 0 before the first period exists (factor 1 - the honest nothing).
        /// </summary>
        public float GetWageGrowthGapAtPeriodOpen(CountryId countryId)
        {
            return _fiscalPeriods.TryGetValue(countryId, out FiscalPeriod period) ? period.WageGrowthGapAtPeriodOpen : 0f;
        }

        /// <summary>The event that fired for a country this turn, or null if none did (most turns).</summary>
        public EconomicEvent GetLastEvent(CountryId countryId)
        {
            return _lastEventsByCountry.TryGetValue(countryId, out EconomicEvent economicEvent) ? economicEvent : null;
        }

        /// <summary>Every cabinet decision rolled for this country that the player hasn't responded to yet (usually empty) - see GameController's Cabinet tab and hasPendingCabinetDecisions gate.</summary>
        /// <summary>
        /// P2-4.3 (2026-09-02): a harness hook - stage a pending cabinet decision so the Docket's minister alerts and
        /// an option's cost-and-impact arrows are on film at the seed, where no minister sits and nothing has rolled.
        /// The game never calls it; the decision is whatever the harness hands in, and its own log line says so.
        /// </summary>
        public void StagePendingCabinetDecision(CountryId countryId, CabinetPortfolio portfolio, CabinetDecision decision)
        {
            if (!_pendingCabinetDecisionsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new List<(CabinetPortfolio, CabinetDecision)>();
                _pendingCabinetDecisionsByCountry[countryId] = pending;
            }
            pending.Add((portfolio, decision));
        }

        public List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> GetPendingCabinetDecisions(CountryId countryId)
        {
            return _pendingCabinetDecisionsByCountry.TryGetValue(countryId, out var pending) ? pending : new List<(CabinetPortfolio, CabinetDecision)>();
        }

        /// <summary>Applies the player's chosen response to one pending cabinet decision and clears it from the pending list - called once per response, from GameController.</summary>
        public void ResolveCabinetDecision(CountryId countryId, CabinetPortfolio portfolio, CabinetDecision decision, CabinetDecisionOption chosenOption)
        {
            // FORK-2 (the review's M2): replayed by IDENTITY - the decision by its name and portfolio, the option by its place in that decision; a counterfactual
            // holds its own objects. The ledger is told only where the option is the decision's (the review's L3)
            string name = decision?.Name;
            int pick = decision?.Options?.IndexOf(chosenOption) ?? -1;
            Answered(new PlayerAnswer("a cabinet decision", countryId, sim =>
            {
                (CabinetPortfolio Portfolio, CabinetDecision Decision) match = sim.GetPendingCabinetDecisions(countryId).FindLast(p => p.Portfolio == portfolio && p.Decision?.Name == name);   // the fix review's L4: the newest of that name
                if (match.Decision == null || pick < 0 || pick >= match.Decision.Options.Count) { return false; }
                sim.ResolveCabinetDecision(countryId, portfolio, match.Decision, match.Decision.Options[pick]);
                return true;
            }), () => { ResolveCabinetDecisionNow(countryId, portfolio, decision, chosenOption); return pick >= 0; });
        }

        /// <summary>FORK-2: <see cref="ResolveCabinetDecision"/>'s act.</summary>
        private void ResolveCabinetDecisionNow(CountryId countryId, CabinetPortfolio portfolio, CabinetDecision decision, CabinetDecisionOption chosenOption)
        {
            Country country = _world.GetCountry(countryId);
            float approvalBeforeOption = country.State.ApprovalRating;
            // Step 2's third section: F1's BudgetImpact reaches the debt stock through this option
            // (ApplyOneTimeBudgetImpact) - observed here, beside the approval observation, as a
            // dated Class B event on the debt ledger. Zero impacts are skipped inside RecordEvent.
            float debtBeforeOption = country.State.GovernmentDebt;
            CabinetSystem.ApplyDecisionOption(country, portfolio, chosenOption);   // P2-5.2: POPULARITY scales the approval figure
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"Cabinet: {chosenOption.Label}", country.State.ApprovalRating - approvalBeforeOption);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, $"Cabinet: {chosenOption.Label}", debtBeforeOption, country.State.GovernmentDebt, decision?.Name);   // AF-17: the decision's name rides the line

            if (_pendingCabinetDecisionsByCountry.TryGetValue(countryId, out var pending))
            {
                pending.RemoveAll(p => p.Portfolio == portfolio && p.Decision == decision);
            }
        }

        /// <summary>The pending foreign policy meeting for this country, or null if none is currently awaiting a response (the common case) - see GameController's Foreign Policy tab and the Update() gate on it.</summary>
        public ForeignPolicyMeeting GetPendingForeignPolicyMeeting(CountryId countryId)
        {
            return _pendingForeignPolicyMeetingByCountry.TryGetValue(countryId, out ForeignPolicyMeeting meeting) ? meeting : null;
        }

        /// <summary>
        /// Rolls, once per simulated day, whether a new meeting fires for this country - a no-op if
        /// one is already pending (single-slot, see _pendingForeignPolicyMeetingByCountry's own doc
        /// comment), so GameController.Update can safely call this every day the loop advances without
        /// checking first itself. Called only for the player's country - unlike Cabinet decisions
        /// (rolled for every country every turn as part of AdvanceTurn), NPC countries have no UI to
        /// ever resolve a meeting through, so there'd be nothing to roll for them.
        /// </summary>
        public void TryRollForeignPolicyMeeting(CountryId countryId)
        {
            if (_pendingForeignPolicyMeetingByCountry.ContainsKey(countryId))
            {
                return;
            }

            ForeignPolicyMeeting meeting = ForeignPolicySystem.TryRollMeeting(ForeignPolicyCadenceMultiplier);
            if (meeting != null)
            {
                _pendingForeignPolicyMeetingByCountry[countryId] = meeting;
            }
        }

        /// <summary>Applies the player's chosen response to the pending foreign policy meeting and clears it - called once per response, from GameController.</summary>
        public void ResolveForeignPolicyMeeting(CountryId countryId, ForeignPolicyMeetingOption chosenOption)
        {
            // FORK-2 (the s871 review's M2; the fix review's M2): replayed by IDENTITY - the meeting by its name (the day is the answer's), the option by its place in
            // it; a counterfactual's pending meeting is its own object. No match (that world rolled another meeting, or none), the world answers its waits by
            // the AI's rule (`ShadowBaseline`), so its slot empties. The no-policy world is handed no answer and sets its meetings aside by that rule. The
            // ledger is told only where the option is the meeting's (the review's L3)
            ForeignPolicyMeeting answered = GetPendingForeignPolicyMeeting(countryId);
            string name = answered?.Name;
            int pick = answered?.Options?.IndexOf(chosenOption) ?? -1;
            Answered(new PlayerAnswer("a foreign-policy meeting", countryId, sim =>
            {
                ForeignPolicyMeeting meeting = sim.GetPendingForeignPolicyMeeting(countryId);
                if (meeting == null || meeting.Name != name || pick < 0 || pick >= meeting.Options.Count) { return false; }
                sim.ResolveForeignPolicyMeeting(countryId, meeting.Options[pick]);
                return true;
            }, answersAWait: true),
            () => { ResolveForeignPolicyMeetingNow(countryId, chosenOption); return pick >= 0; });
        }

        /// <summary>FORK-2 (the s871 review's M2): the pending meeting cleared with no option applied - the AI's answer in a counterfactual world, where no
        /// player answers (<see cref="AnswerWaitsAsTheAi"/>: no AI-run country holds a meeting), so the single slot empties and the meetings go on rolling there.
        /// True where a meeting was pending.</summary>
        public bool SetAsideForeignPolicyMeeting(CountryId countryId) => _pendingForeignPolicyMeetingByCountry.Remove(countryId);

        /// <summary>FORK-2: <see cref="ResolveForeignPolicyMeeting"/>'s act.</summary>
        private void ResolveForeignPolicyMeetingNow(CountryId countryId, ForeignPolicyMeetingOption chosenOption)
        {
            Country country = _world.GetCountry(countryId);
            float approvalBeforeOption = country.State.ApprovalRating;
            // Step 2's third section: the meeting option's BudgetImpact is F1's second writer -
            // observed on the debt ledger exactly as the cabinet option is.
            float debtBeforeOption = country.State.GovernmentDebt;
            ForeignPolicySystem.ApplyMeetingOption(country, chosenOption);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"Foreign policy: {chosenOption.Label}", country.State.ApprovalRating - approvalBeforeOption);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, $"Foreign policy: {chosenOption.Label}", debtBeforeOption, country.State.GovernmentDebt,
                _pendingForeignPolicyMeetingByCountry.TryGetValue(countryId, out ForeignPolicyMeeting settled) ? settled?.Name : null);   // AF-17: the meeting's name rides the line
            _pendingForeignPolicyMeetingByCountry.Remove(countryId);
        }

        /// <summary>The pending omnibus BudgetBill for this country, or null if none is currently before Parliament (the common case).</summary>
        /// <summary>
        /// PS-3e (2026-09-25, §632, ruled): THE AI-GOVERNMENT BILL PATH. Where the AI governs the player's country its finance ministry's rule
        /// (AiFinanceMinistry, the EU rule / the US caps) is laid before the chamber as the government's budget bill instead of written into the
        /// book at the turn boundary; the player's party may table an alternative while it stands (<see cref="TableShadowBudget"/>), and on the
        /// bill's day the chamber decides by the country's own procedure (<see cref="Elections.WorldClock.BudgetProcedureOf"/>): Sweden's frame decision sets
        /// the government's frames against the alternative and adopts the one the chamber prefers; a country whose chamber weighs no alternative
        /// (<see cref="Elections.WorldClock.WeighsAlternative"/> - Poland's procedure included, US-20) votes the government's bill alone, and a failed
        /// budget leaves the old one standing, stated in the division's own title.
        /// </summary>
        public void TableGovernmentBudget(Country country)
        {
            if (country == null || _pendingBudgetBillByCountry.ContainsKey(country.Id)) { return; }
            PolicyDecision governmentDecision = AiFinanceMinistry.Decide(country, GetLastFiscalReport(country.Id));
            // §716 (the review's defect 1): the Finance partner's step rides the government's own bill, after the ministry's rule (its first claim kept) -
            // the player's country under an AI head takes no boundary step (PartnerStepsAtBoundary)
            FinancePartner.Written partnerStep = FinancePartnerRuns(country) ? FinancePartner.Apply(country, governmentDecision, CurrentDate) : null;   // once in any year (StepDue)
            BudgetBill bill = AiFinanceMinistry.AsBudgetBill(country, governmentDecision);
            FinancePartner.Table(country, partnerStep, bill, CurrentDate);   // §755 (the review's defect 1): the year's step spent now, counted only if the chamber adopts the bill
            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingBudgetBillByCountry[country.Id] = bill;
            _pendingBudgetProcessByCountry.Remove(country.Id);
            _incomingBudgetWindowOpenNow.Remove(country.Id);
            Debug.Log($"BUDGET: {country.Id} - the government tables its budget ({DescribeBudgetBill(bill)}); the chamber decides in {bill.DaysRemaining} day(s) by {Elections.WorldClock.BudgetProcedureOf(country.Id)}");
        }

        /// <summary>PS-3e (§632): the player's party tables its draft as the ALTERNATIVE to the government's pending budget - the Riksdag's budget motion. False, with the reason, where none can be tabled.</summary>
        public bool TableShadowBudget(CountryId countryId, BudgetBill bill, out string refusedBecause)
        {
            System.Func<BudgetBill> copy = CopiesForLedger(bill);   // FORK-2: the alternative as tabled, a fresh copy for each except-world
            string why = null;
            bool acted = Answered(new PlayerAnswer("an alternative budget", countryId, sim => copy != null && sim.TableShadowBudget(countryId, copy(), out _), answersAWait: false, bill: Congress.Kind.Budget), () => TableShadowBudgetNow(countryId, bill, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="TableShadowBudget"/>'s act.</summary>
        private bool TableShadowBudgetNow(CountryId countryId, BudgetBill bill, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || bill == null) { refusedBecause = "no country"; return false; }
            if (!_pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill pending) || !pending.GovernmentBill) { refusedBecause = "NO GOVERNMENT BUDGET IS BEFORE THE CHAMBER"; return false; }
            if (Elections.WorldClock.CongressActs(countryId)) { refusedBecause = UsBudgetProcedure.NoAlternativeWords; return false; }   // PS-6 US-22: Congress votes the president's budget as its acts and weighs no alternative
            if (!Elections.WorldClock.WeighsAlternative(countryId)) { refusedBecause = "THIS COUNTRY'S BUDGET PROCEDURE IS NOT YET MODELLED · THE GOVERNMENT'S BILL IS VOTED ALONE"; return false; }   // US-20: the chamber weighs no alternative
            if (_pendingBudgetAlternativeByCountry.ContainsKey(countryId)) { refusedBecause = "YOUR ALTERNATIVE IS ALREADY TABLED"; return false; }
            if (country.Government != null && country.Government.RoleOf(country.PlayerPartyAbbrev) == Elections.PlayerRole.JuniorPartner) { refusedBecause = "JUNIOR PARTNER · YOUR BUDGET VOICE IS THE COALITION AGREEMENT"; return false; }   // PS-3f (§633, ruled)
            refusedBecause = BudgetBillOutsideYearlyRange(country, bill);   // AF-9: the yearly range is the model's gate on an alternative as on the player's own bill
            if (refusedBecause != null) { return false; }
            bill.TabledBy = country.PlayerPartyAbbrev;
            bill.GovernmentBill = false;
            bill.DaysRemaining = pending.DaysRemaining;
            _pendingBudgetAlternativeByCountry[countryId] = bill;
            Debug.Log($"BUDGET: {countryId} - {bill.TabledBy} tables an alternative budget ({DescribeBudgetBill(bill)}) against the government's");
            return true;
        }

        /// <summary>PS-3e (§632): the arrival budget window is a GOVERNMENT'S, once - the flag is reset where the government changes (the election's verdict), so an incoming government lays its budget on arrival whether the AI's or the player's laid the last.</summary>
        public void ResetArrivalBudgetWindow(CountryId countryId) => _incomingBudgetWindowUsed.Remove(countryId);

        /// <summary>PS-3e (§632): the alternative tabled against the government's pending budget, or null.</summary>
        public BudgetBill GetPendingBudgetAlternative(CountryId countryId) => _pendingBudgetAlternativeByCountry.TryGetValue(countryId, out BudgetBill bill) ? bill : null;

        /// <summary>The budget's day: the country's procedure decides between the government's bill and the alternative, and what the chamber adopts is applied.</summary>
        private void ResolveGovernmentBudget(Country country, BudgetBill government)
        {
            BillConcern concernG = ParliamentSystem.GetBudgetBillConcern(country, ParliamentSystem.BudgetActOf(country, government));   // §768/§773: Poland's budget act - the spending alone
            _pendingBudgetAlternativeByCountry.TryGetValue(country.Id, out BudgetBill alternative);
            Elections.WorldClock.BudgetProcedure procedure = Elections.WorldClock.BudgetProcedureOf(country.Id);
            float approvalBefore = country.State.ApprovalRating;
            if (procedure == Elections.WorldClock.BudgetProcedure.RiksdagFrameDecision && alternative != null)
            {
                // THE FRAME DECISION (sweden/budget_procedure.md): the revenue estimate and every expenditure area's frame are fixed "genom ett enda beslut" [RO-11-18];
                // with more than two proposals the chamber sets the main proposal against ONE counter-proposal chosen in preparatory votes [RO-11-10], and the one
                // "som stöds av mer än hälften av de röstande" carries [RO-11-10] - abstentions do not count (2021: 154 ja, 143 nej, 51 avstår, the M/SD/KD frames
                // adopted over the government's [FIU1-22]). The model: one alternative (the player's), so the main vote is the whole procedure; each party votes for
                // the proposal it aligns with more, or abstains where it aligns with neither; recorded as sides toward the government's frames (+1, -1, 0 abstaining).
                // STATED DEVIATIONS: a tie keeps the government's frames (the statute decides a preparatory vote's tie by lot [RO-11-11]; a main vote's tie tables the matter, then returns it or draws lots [RO-11-12]); the
                // alternative may be tabled any day the bill stands (21), where the statute gives 15 days from the announcement [RO-9-12].
                BillConcern concernA = ParliamentSystem.GetBudgetBillConcern(country, alternative);
                var alignG = new Dictionary<string, PartyStance>(); foreach (PartyStance st in StanceModel.Stances(country, concernG)) { alignG[st.Party.Abbrev] = st; }
                var alignA = new Dictionary<string, PartyStance>(); foreach (PartyStance st in StanceModel.Stances(country, concernA)) { alignA[st.Party.Abbrev] = st; }
                int forG = 0, forA = 0, abstaining = 0;
                var sides = new List<DivisionSide>();
                foreach (KeyValuePair<string, PartyStance> kv in alignG)
                {
                    PartyStance g = kv.Value;
                    float a = alignA.TryGetValue(kv.Key, out PartyStance sa) ? sa.Alignment : 0f;
                    // The government's own parties - the cabinet and its support - carry its frames (2021: the government's parties voted its budget, the M/SD/KD theirs, C and L abstained [FIU1-22]);
                    // every other party votes for the proposal it aligns with more, or abstains where it aligns with neither.
                    Elections.PlayerRole role = country.Government != null ? country.Government.RoleOf(kv.Key) : Elections.PlayerRole.None;
                    // PS-3f (§633, ruled): a SUPPORT party that tabled the alternative votes for its own frames, not the government's - a break with the government, recorded on it.
                    bool ownAlternative = kv.Key == alternative.TabledBy;   // whoever tabled it votes its own (the reader, s633): the opposition's motion is its own vote; a support party's is also a break
                    bool governmentParty = role == Elections.PlayerRole.PrimeMinister || role == Elections.PlayerRole.JuniorPartner || (role == Elections.PlayerRole.Support && !ownAlternative);
                    int side = governmentParty ? 1 : ownAlternative ? -1 : g.Alignment >= a ? (g.Alignment > 0f ? 1 : 0) : (a > 0f ? -1 : 0);
                    if (side > 0) { forG += g.Seats; } else if (side < 0) { forA += g.Seats; } else { abstaining += g.Seats; }
                    string why = governmentParty ? "the government's own party - carries its frames" : ownAlternative ? (role == Elections.PlayerRole.Support ? "its own alternative over the frames of the government it supports - a break with the government" : "its own alternative") : side > 0 ? $"the government's frames ({g.Alignment:+0.00;-0.00}) over {PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} ({a:+0.00;-0.00})" : side < 0 ? $"{PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} frames ({a:+0.00;-0.00}) over the government's ({g.Alignment:+0.00;-0.00})" : "abstains - aligned with neither";
                    sides.Add(new DivisionSide { Abbrev = g.Party.Abbrev, ShortName = g.Party.ShortName, Seats = g.Seats, Side = side, Alignment = side > 0 ? g.Alignment : side < 0 ? a : 0f, Reason = why });
                }
                bool governmentAdopted = forG >= forA;   // a tie keeps the government's frames - the tie rule of the statute is sourced in the record and applied there when it lands
                BudgetBill adopted = governmentAdopted ? government : alternative;
                country.Divisions.Append(governmentAdopted ? $"Annual budget: the government's frames adopted, {forG} to {forA}, over {PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} alternative" : $"Annual budget: {PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} alternative frames adopted, {forA} to {forG}, over the government's",
                    CurrentDate, ParliamentSystem.GetSeatWeightedAlignment(country, concernG), true, concernG.Direction, (int)BillAxis.Fiscal, sides);   // PASSED either way: the chamber adopted a budget - the title says whose
                string breakBy = alternative.TabledBy != null && country.Government != null && country.Government.RoleOf(alternative.TabledBy) == Elections.PlayerRole.Support ? alternative.TabledBy : null;
                country.Divisions.Entries[country.Divisions.Entries.Count - 1].Contest = new DivisionContest { ProposalFor = "THE GOVERNMENT'S FRAMES", ProposalAgainst = PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, true, true) + " ALTERNATIVE", VotesFor = forG, VotesAgainst = forA, Abstentions = abstaining, AlternativeAdopted = !governmentAdopted, BreakBy = breakBy };   // PS-3f (§633): the two proposals on the record
                if (breakBy != null) { country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {breakBy} voted its own alternative budget against the government's frames"); Debug.Log($"BUDGET: {country.Id} - {breakBy}, a support party, broke with the government on its budget"); }
                if (!governmentAdopted) { country.Government?.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the government lost its budget - {PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} frames adopted; it governs on the adopted frames (sweden/budget_procedure.md [BA-3]); the constitution requires no resignation, and the AI government offers none (a premise, PS-3i §636)"); }
                ParliamentSystem.ApplyBillResult(country, adopted, true, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, governmentAdopted);   // A-LOOP q37 (review 878b's L-5): an alternative adopted over the government's frames is no move of the government's - its rates charge it nothing and enter no net
                if (governmentAdopted) { FinancePartner.CreditAdopted(country, government); }   // §755: the partner's step counted only where its bill was adopted
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, governmentAdopted ? "The government's budget adopted" : $"{PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} alternative budget adopted", country.State.ApprovalRating - approvalBefore);
                Debug.Log($"BUDGET: {country.Id} - the frame decision: the government's {forG} seats, {PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true)} alternative {forA}; {(governmentAdopted ? "the government's" : PoliSim.Wording.EnglishText.Possessive(alternative.TabledBy, false, true))} frames adopted");
                return;
            }
            // The government's bill alone (no alternative tabled, or a chamber that weighs none - WeighsAlternative, US-20): for or against; a failed budget leaves the old one standing, stated.
            // PS-6 US-22: the US Congress's budget never comes here (AdvanceBudgetBillDay routes it to its acts); a US world with no Senate seated still does, through its one chamber
            DivisionRecord budgetDivision = ParliamentSystem.Divide(country, adopted => adopted ? "Annual budget: the government's bill adopted" : (!Elections.WorldClock.WeighsAlternative(country.Id)   // US-20: no alternative procedure sourced
                ? "Annual budget: the government's bill failed - the old budget stands (this country's procedure is not yet sourced)"
                : "Annual budget: the government's bill failed with no alternative tabled - the old budget stands"), concernG, CurrentDate, out bool passed, out string title);
            if (passed && HoldPolishBudget(country, government, budgetDivision)) { return; }   // SP-7b: adopted, Poland's budget act and its statute acts go to the player-president's docket - the slot kept (AdvanceBudgetBillDay)
            List<BudgetBill.StatutePart> fellG = new List<BudgetBill.StatutePart>();
            BudgetBill appliedG = passed ? PolishStatuteActs(country, government, out fellG) : government;   // §768 (D4), §773 (E2): Poland's statute parts travel in their own acts
            approvalBefore = country.State.ApprovalRating;   // §768 (the second review): a fallen act's cost is its own ledger event - the budget's event starts after it
            ParliamentSystem.ApplyBillResult(country, appliedG, passed, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concernG));   // PS-6 US-22 (N1): charged where the government bears it - true in every country without a Congress
            if (passed) { FinancePartner.CreditAdopted(country, government, ratesLanded: !fellG.Contains(BudgetBill.StatutePart.Rates)); }   // §755: counted only where adopted; §768: only what landed
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "The government's budget adopted" : "The government's budget failed", country.State.ApprovalRating - approvalBefore);
            Debug.Log($"BUDGET: {country.Id} - {title}");
        }

        /// <summary>
        /// §768 (Elias's ruling D4) and §773 (E2): POLAND'S BUDGET IS THE BUDGET ACT AND ITS STATUTES. The budget act the Sejm has just adopted stays
        /// veto-proof (Konstytucja Art. 224) and carries the spending alone (E2: "only spending stays in the budget act"); each statute part the bill
        /// changes travels in its own ACT (<see cref="BudgetBill.StatutePart"/>) - the rates a tax act (taxes are set by statute, Art. 217), the pension age
        /// a pension act (E2), the benefit levels and the fund's rules their own acts (RULED, F6: "In Poland benefit levels and the fund's rules are set by
        /// statute, so they are statute parts. 'Spending' means the budget act's appropriations.") - each voted by the Sejm on
        /// that part alone (its own division) and put to the President like every ordinary statute (B1, <see cref="PresidentialVetoGate"/>). Where an act
        /// fails, or its veto stands, what it would have set stays as it is and the rest applies: the bill applies with that part withheld, and the fall
        /// costs what every failed bill costs. RULED (E2) for the tax act - "a rejected budget takes its tax act down with it: accepted"; the other acts ride
        /// the same way (READING, DECLARED: the pension act by E2's "like tax rates", the benefit and fund acts as the statute parts F6 rules them) - each
        /// is voted only once the budget is adopted.
        /// ⚠ CORRECTED, AF-9 (Elias's ruling q9, 2026-10-18: "fund terms go to the vote"): the fund act used to be uncontested by construction. Its money
        /// terms are now weighed (<see cref="ParliamentSystem.FundTermsOf"/>), so it is voted and put to the President like the others; a mix-only act still asks nothing. Every act is a
        /// division of its own; RULED (Elias's ruling F5, "(b): the check reads every bill lost that day"): an act that falls - failed in the Sejm, or vetoed
        /// with the veto standing (<see cref="PresidentialVetoGate"/>) - marks the day lost (<see cref="ParliamentSystem.RecordBillLost"/>), and
        /// <see cref="CabinetSystem.UnderPressure"/> reads that mark, not the day's newest division - so an act passed after a fallen one hides nothing.
        /// Returns the bill to apply and, in <paramref name="fell"/>, the parts withheld. <paramref name="only"/> limits the acts voted to those parts (the
        /// Finance partner's boundary act votes its rates alone - §773's review); null votes every part the bill changes. Every other country, and a bill
        /// that changes no statute part, applies whole.
        /// </summary>
        private BudgetBill PolishStatuteActs(Country country, BudgetBill bill, out List<BudgetBill.StatutePart> fell, IReadOnlyList<BudgetBill.StatutePart> only = null)
        {
            fell = new List<BudgetBill.StatutePart>();
            if (bill == null || !Elections.WorldClock.StatutePartsAreActs(country.Id)) { return bill; }   // US-20: the split keys on the procedure, not the veto
            BudgetBill applied = bill;
            foreach (BudgetBill.StatutePart part in only ?? BudgetBill.StatuteParts)
            {
                if (!bill.Changes(part, country)) { continue; }
                BudgetBill act = bill.PartOf(part);
                BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, act);
                bool passed = ParliamentSystem.WouldBillPass(country, concern);
                string title = StatuteActTitle(country, part, act);
                DivisionRecord vote = ParliamentSystem.RecordDivision(country, title, concern, passed, CurrentDate);
                passed = PresidentialVetoDrawn(country, vote, passed);   // SP-7b: a player-president's budget is held before it comes here (HoldPolishBudget); the boundary's partner act is not his to answer (DECLARED)
                Debug.Log($"BUDGET: {country.Id} - {title}: {(passed ? "stands - it applies" : "falls - what it would set stays as it is, and the rest applies")}");   // "the rest": the budget act's, or the partner's step less its rates (§773's second review)
                if (passed) { continue; }
                fell.Add(part);
                applied = applied.Without(part, country);
                // a statute that falls costs what every failed bill costs (ApplyXResult's FAIL branch) - §768's review, question (2)
                float approvalBefore = country.State.ApprovalRating;
                TaxTerm.Shock(country, -ParliamentSystem.BillFailedApprovalCost);   // A-LOOP q37 (review 879b M-1): the shock through TaxTerm.Shock - the same write, and the run without the tax term takes it through its own clamp
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, ActWords(part) + " failed", country.State.ApprovalRating - approvalBefore);
            }
            return applied;
        }

        /// <summary>§773: a statute act's name as its title and ledger event read it - every act's title is this, ": ", then what it moves.</summary>
        public static string ActWords(BudgetBill.StatutePart part) =>
            part == BudgetBill.StatutePart.Rates ? "Tax act" : part == BudgetBill.StatutePart.PensionAge ? "Pension act" : part == BudgetBill.StatutePart.Benefits ? "Benefits act" : "Fund act";

        /// <summary>§773: whether a division's title is a Polish statute act's - one a budget carried, or the Finance partner's boundary tax act
        /// (<see cref="PartnerTaxAct"/>). The night's standing-budget scan passes over them: the budget act is the standing one.</summary>
        public static bool IsStatuteActTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) { return false; }
            foreach (BudgetBill.StatutePart part in BudgetBill.StatuteParts) { if (title.StartsWith(ActWords(part) + ": ", System.StringComparison.Ordinal)) { return true; } }
            // PS-6 US-22: the US reconciliation and a continuing resolution are no standing budget either (the US pension act's title is Poland's word, above;
            // the appropriation is the standing one) - neither prefix begins any title outside the USA
            return title.StartsWith(UsBudgetProcedure.ActWords(UsBudgetProcedure.Act.Reconciliation) + ": ", System.StringComparison.Ordinal)
                || title.StartsWith(UsBudgetProcedure.ActWords(UsBudgetProcedure.Act.Reconciliation) + " (", System.StringComparison.Ordinal)   // PS-6 US-23 (the review's L2): a congressional majority's reconciliation (`UsMajorityReconciliation.TitleOf`) is no standing budget either
                || title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords + ": ", System.StringComparison.Ordinal);
        }

        /// <summary>§773: a statute act's title as a player reads it - what it moves, from and to.</summary>
        private static string StatuteActTitle(Country country, BudgetBill.StatutePart part, BudgetBill act)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            switch (part)
            {
                case BudgetBill.StatutePart.Rates:
                    return TaxActTitle(country, act);
                case BudgetBill.StatutePart.PensionAge:
                {
                    float inForce = PensionAgeStatute.AgeInForce(country, country.CalendarYear);
                    string to = act.PensionAge < 0f ? "the statute's own (" + PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(country.Id, country.CalendarYear)) + ")"
                        : PensionAgeStatute.Format(Mathf.Clamp(act.PensionAge, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax));
                    return ActWords(part) + ": the pension age " + PensionAgeStatute.Format(inForce) + " to " + to;
                }
                case BudgetBill.StatutePart.Benefits:
                {
                    var moves = new List<string>();
                    foreach (KeyValuePair<WelfareProgramType, float> kv in act.WelfarePrograms)
                    {
                        WelfareProgram standing = country.WelfarePrograms.Find(w => w.Type == kv.Key);
                        float to = Mathf.Clamp(kv.Value, 0f, 100f);
                        if (standing == null || !standing.IsImplemented || System.Math.Abs(to - standing.GenerosityLevel) <= 1e-4f) { continue; }
                        moves.Add(Words(kv.Key.ToString()) + " " + standing.GenerosityLevel.ToString("0.#", inv) + " to " + to.ToString("0.#", inv));
                    }
                    return ActWords(part) + ": " + (moves.Count > 0 ? string.Join(", ", moves) : "the standing levels");
                }
                default:
                {
                    SovereignWealthFund fund = country.SovereignWealthFund;
                    if (fund == null) { return ActWords(part) + ": the sovereign wealth fund created"; }
                    if (!act.SwfShouldExist) { return ActWords(part) + ": the sovereign wealth fund dissolved"; }
                    // §773's review: every rule the act moves, from and to (the test ChangesFund applies). The second review's finding 8: the contribution is
                    // a share of GDP a year and the domestic allocation a share of the fund, but the asset-class weights are RAW - normalised by their live sum
                    // (SovereignWealthFund's own doc) - so they are said as the Fund tab says them, without a unit, and the shares of the fund they come to
                    // are said beside them (the quantity the tab's right-hand figure shows, here to a tenth). The third review's finding 1: a figure is
                    // listed only where its printed from and to differ, so a small step never reads "X % to X %" - §768's rule for the tax act's title
                    var rules = new List<string>();
                    void Moved(string words, string from, string to, string tail) { if (from != to) { rules.Add(words + " " + from + " to " + to + tail); } }
                    void Rule(string name, float from, float to, string of) => Moved(name, from.ToString("0.0#", inv) + " %", to.ToString("0.0#", inv) + " %", of);
                    void Weight(string name, float from, float to) => Moved(name, from.ToString("0.#", inv), to.ToString("0.#", inv), string.Empty);
                    Rule("its contribution", fund.ContributionRatePercent, act.SwfContributionRatePercent, " of GDP a year");
                    Rule("its domestic allocation", fund.DomesticAllocationPercent, act.SwfDomesticAllocationPercent, " of the fund");
                    int weightsFrom = rules.Count;
                    Weight("its equities weight", fund.EquitiesWeight, act.SwfEquitiesWeight);
                    Weight("its bonds weight", fund.BondsWeight, act.SwfBondsWeight);
                    Weight("its infrastructure weight", fund.InfrastructureWeight, act.SwfInfrastructureWeight);
                    Weight("its real-estate weight", fund.RealEstateWeight, act.SwfRealEstateWeight);
                    if (rules.Count > weightsFrom)
                    {
                        var mix = new SovereignWealthFund { EquitiesWeight = act.SwfEquitiesWeight, BondsWeight = act.SwfBondsWeight, InfrastructureWeight = act.SwfInfrastructureWeight, RealEstateWeight = act.SwfRealEstateWeight };
                        var shares = new List<string>();
                        foreach (SovereignWealthAssetClass assetClass in (SovereignWealthAssetClass[])System.Enum.GetValues(typeof(SovereignWealthAssetClass)))
                        {
                            string from = (fund.GetNormalizedWeight(assetClass) * 100f).ToString("0.#", inv), to = (mix.GetNormalizedWeight(assetClass) * 100f).ToString("0.#", inv);
                            if (from != to) { shares.Add(Words(assetClass.ToString()) + " " + from + " % to " + to + " %"); }
                        }
                        if (shares.Count > 0) { rules.Add("its shares of the fund: " + string.Join(", ", shares)); }
                    }
                    return ActWords(part) + ": the sovereign wealth fund's rules" + (rules.Count > 0 ? " - " + string.Join("; ", rules) : string.Empty);
                }
            }
        }

        /// <summary>An identifier in words: "IncomeTax" reads "income tax"; an acronym ("VAT") stays as it is.</summary>
        private static string Words(string id) =>
            id == id.ToUpperInvariant() ? id : System.Text.RegularExpressions.Regex.Replace(id, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();

        /// <summary>§768: the tax act's title as a player reads it - each rate it moves, from and to (invariant numbers), and the schedule named where it moves a sub-row.</summary>
        private static string TaxActTitle(Country country, BudgetBill taxAct)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<TaxType, float> kv in taxAct.TaxLines)
            {
                TaxLine standing = country.TaxLines.Find(t => t.Type == kv.Key);
                if (standing == null || !standing.IsImplemented || System.Math.Abs(kv.Value - standing.Rate) <= 1e-6f) { continue; }
                // the review's defect 2: a per-tonne line in its own unit (the Budget page's, the currency per tonne), and a rate to the hundredth, so a
                // small step never reads "12.4 % to 12.4 %"
                string unit = standing.IsPerTonne ? " " + EnergyLayer.CurrencyCode(country.Id) + "/t" : " %", format = standing.IsPerTonne ? "0.##" : "0.0#";
                parts.Add(TaxWords(kv.Key) + " " + standing.Rate.ToString(format, System.Globalization.CultureInfo.InvariantCulture) + unit + " to "
                    + kv.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture) + unit);
            }
            foreach (KeyValuePair<TaxType, float[]> kv in taxAct.BracketRates)
            {
                // the schedule named only where a sub-row it sets differs from the standing one (the second review: a passed figure stays in the draft)
                if (kv.Value == null) { continue; }
                TaxLine standing = country.TaxLines.Find(t => t.Type == kv.Key);
                bool differs = false;
                for (int i = 0; i < kv.Value.Length && !differs; i++)
                {
                    differs = kv.Value[i] >= 0f && (standing?.BracketRates == null || i >= standing.BracketRates.Length || System.Math.Abs(kv.Value[i] - standing.BracketRates[i]) > 1e-6f);
                }
                if (differs) { parts.Add(PoliSim.Wording.EnglishText.Possessive(TaxWords(kv.Key), false) + " schedule"); }
            }
            return ActWords(BudgetBill.StatutePart.Rates) + ": " + (parts.Count > 0 ? string.Join(", ", parts) : "the standing rates");
        }

        /// <summary>A tax type in words: "IncomeTax" reads "income tax"; an acronym ("VAT") stays as it is.</summary>
        private static string TaxWords(TaxType type)
        {
            string id = type.ToString();
            if (id == id.ToUpperInvariant()) { return id; }
            return System.Text.RegularExpressions.Regex.Replace(id, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
        }

        /// <summary>
        /// §761 (PS-5; Elias's rulings B1 and B2), widened by F3: THE PRESIDENT'S VETO, LIVE. A statute the Sejm has just passed - <paramref name="passage"/>,
        /// the division `RecordDivision` returned for THIS vote (the log is never read back) - goes to the President (<see cref="Elections.PresidentialVeto.Decide"/>):
        /// at risk where the backing party did not vote for it (F3), then vetoed or signed by the seeded draw at the president's rate - drawn here and nowhere
        /// else, keyed on the master seed and the act (<see cref="Elections.PresidentialVeto.Draw"/>), so every world that resolves the act on the same day draws
        /// alike (a PREMISE stated there; since FORK-1 the impact ledger's forks run the played world's day tick and reach the act on its day) and no random stream moves. The Sejm's
        /// re-pass, voted by the same sides, overrides a veto with 3/5 of those voting. The veto and the vote on it are one division of
        /// their own - its line the 3/5 (`DivisionRecord.Required`), passed where overridden, failed where the veto stands - and it carries the bill's
        /// ceremony; the passage keeps none (marked a motion, so no signing is shown for a statute the President returned). Returns whether the statute stands.
        /// The budget act never comes here (Art. 224); a constitutional amendment passes through unvetoed (Art. 235 ust. 7, <paramref name="act"/>); Poland alone.
        /// SP-7b (Elias's ruling I14, ask 2 (2a)): where the president of the day is the PLAYER nothing is drawn here - the passage is returned as voted, and
        /// the docket's gate, which every site calling this reaches next (<see cref="PolishDocketGate"/>), holds it for his answer; the draw stays for every AI
        /// president (<see cref="PresidentialVetoDrawn"/>, the gate's own body, which the budget's statute acts call directly).
        /// </summary>
        private bool PresidentialVetoGate(Country country, DivisionRecord passage, bool passed, Elections.PresidentialVeto.Act act = Elections.PresidentialVeto.Act.OrdinaryStatute)
        {
            if (passed && passage != null && PlayerPresidentDockets(country)) { return passed; }   // SP-7b: the player-president's answer, not the draw - the docket's gate holds it
            return PresidentialVetoDrawn(country, passage, passed, act);
        }

        /// <summary>SP-7b: the gate's draw (§761, F3), whoever the president is - <see cref="PresidentialVetoGate"/>'s body as it stood, called directly where no
        /// docket follows: a budget's statute acts (`PolishStatuteActs`) and a held statute an AI president answers once the player no longer holds the office.</summary>
        private bool PresidentialVetoDrawn(Country country, DivisionRecord passage, bool passed, Elections.PresidentialVeto.Act act = Elections.PresidentialVeto.Act.OrdinaryStatute)
        {
            if (!passed || passage == null || !Elections.PresidentialVeto.Applies(country.Id)) { return passed; }
            Elections.PresidentialVeto.Outcome veto = Elections.PresidentialVeto.Decide(country.Id, country.PresidentialElections, CurrentDate, act, passage.Sides);
            if (veto == null || !veto.AtRisk) { return true; }
            veto.DrawOn(Elections.PresidentialVeto.Draw(SimulationRandom.MasterSeed, country.Id, CurrentDate, passage.Title));
            string risk = (veto.Risk * 100.0).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " %";
            if (!veto.Vetoed) { Debug.Log($"VETO: {country.Id} - signed at a {risk} risk ({veto.RiskBasis}): {passage.Title}"); return true; }
            return RecordPresidentialVeto(country, passage, veto, $"drawn at a {risk} risk, {veto.RiskBasis}");   // SP-7b: the vote on the veto, one writer for the draw and the player's answer
        }

        /// <summary>
        /// PS-6 US-21 (`docs/specs/USA_STAGE_PLAN.md`, "The US veto"; R-US17's rule (c), INSTALLED): THE US VETO GATE, beside Poland's - Art. I §7 cl. 2, "Every
        /// Bill which shall have passed the House of Representatives and the Senate, shall, before it become a Law, be presented to the President". A bill both
        /// chambers sent - <paramref name="passage"/>, the second chamber's division carrying the first's - goes to the president of the day (`Congress.Present`):
        /// an AI president answers by rule (a), deterministically, on every act, budget acts included; a player-president's answer is the player's
        /// (<see cref="PlayerPresidentVetoes"/>). A veto is put to the origin chamber, then the other, at two thirds of those voting on the passage's recorded sides
        /// (`Congress.RecordVeto` writes each vote as its own division and the ceremony moves to the last); a veto that stands is the day's loss (Elias's ruling
        /// F5), written through the one writer a chamber's rejection uses (`Congress.RecordLost`, handed the bill's <paramref name="concern"/> and so its author);
        /// and the gate charges nothing itself - it returns false, and the site's own result method charges a vetoed bill what it charges any bill that did not
        /// pass - so a rule on whose cost or loss a failed bill is applies to a vetoed one, and to an overridden one, unchanged. WHO BEARS (US-19's rule,
        /// `Congress.GovernmentBears`, composed, never repeated here): a congressional majority's bill a president vetoes costs the government nothing and marks no
        /// day lost; the government's own bill a veto kills (a player-president's veto of his own party's bill) is its failure and its lost day; an overridden
        /// bill is law, its enactment cost the government's only where it bears the bill. The president bears nothing for a veto, nor for being overridden
        /// (DECLARED: the plan settles no such cost and no source gives one; asked of Elias). The defectors were drawn at the
        /// chambers' divisions (`Congress.Divide`), not here, and nothing is drawn here. Returns whether the bill becomes law. Poland goes to its own docket's gate
        /// (SP-7b, `PolishDocketGate`: held for a player-president, else passed through as its own gate decided it); every other country, a bill one
        /// chamber voted (a US world with no Senate seated) and a bill not passed pass through untouched; since US-22 every US budget act and continuing
        /// resolution comes here (`UsBudgetActs`, `ResolveContinuingResolution`). A CONSTITUTIONAL AMENDMENT
        /// (<paramref name="amendment"/>) is not presented (DECLARED: `LawDefinition.ConstitutionalAmendment`'s contract, "never vetoed" where a president holds a
        /// veto the game runs, and Art. V, under which Congress proposes an amendment with no presentment; the game models no ratification by the states).
        /// <para>PS-6 US-21b, THE DOCKET (Elias's ruling I12 (i), 2026-10-18; Art. I §7 cl. 2: "If any Bill shall not be returned by the President within ten
        /// Days (Sundays excepted) after it shall have been presented to him, the Same shall be a Law, in like Manner as if he had signed it"): a bill presented
        /// to a PLAYER-president who gives no answer on the day (<see cref="PlayerPresidentVetoes"/> null - no controller sets it) is HELD:
        /// <paramref name="docket"/> is written with the passage and the day it was presented, the bill stays in its pending slot, and this returns null - the
        /// site neither applies nor charges it. Each later day the site comes back here with the held entry (no fresh vote): the player's answer
        /// (<see cref="SignDocketBill"/>, <see cref="VetoDocketBill"/>) is acted on that day - a veto going to the override exactly as an answer on the day
        /// would - and stands whoever holds the presidency by then (`DocketEntry.AnsweredOn`: presented as of the day he answered); unanswered once the period
        /// has run (`PresidentialDocket.Expired`: the day after the tenth day counted, Sundays excepted, from the day after presentment), the bill is law
        /// without his signature; and where an unanswered bill's president of the day is no longer the player, the president in office answers by his own rule
        /// that day. An AI president answers on the day, as before. Held or decided, <paramref name="docket"/> is null again once it returns a
        /// value. THE POCKET VETO IS NOT MODELLED (the clause's "unless the Congress by their Adjournment prevent its Return"): the game models no
        /// adjournment, so an unanswered bill is always law at the period's end (DECLARED, out of scope by the ruling).</para>
        /// <para>PS-6 US-21c (Elias's ruling J6, 2026-10-19): (i) "an answer acts at once; its money moves on the next day step, as every law's does" - the
        /// verbs act on an answer the moment it is given (<see cref="ActOnDocketAnswers"/> brings the answered bill back here the same day), so the law, or the
        /// veto and its override, is of the answer's day and its money flows from the next day step. (ii) The law's date, the day after the tenth day counted,
        /// is SOURCED: P.L. 119-101 (H.R. 6644), "presented to the President of the United States on Monday, June 29, 2026", "has become law without his
        /// signature on July 11, 2026" (the Office of the Federal Register's note on the slip law, `ElectionsData/usa/raw/unsigned/`) - Friday 10 July the
        /// tenth day counted, Sunday 5 July excepted.</para>
        /// <para>PS-6 US-22c (Elias's ruling J7): an entry may carry a deadline of its own (the overload carrying one, `DocketEntry.Deadline` - the eve's
        /// continuing resolution's, the funding horizon): the deadline is the last day to answer, and unanswered at the first day step after it the bill is SIGNED
        /// by the player-president ("Unanswered by then, it is signed") - so the player may still answer on the deadline itself (the review's M2).</para>
        /// </summary>
        private bool? UsVetoGate(Country country, DivisionRecord passage, BillConcern concern, bool passed, ref DocketEntry docket, bool amendment = false)
            => UsVetoGate(country, passage, concern, passed, ref docket, amendment, default);   // the form the film driver and UsVetoDiagnostic resolve by reflection, by its parameter list

        /// <summary>PS-6 US-22c: <see cref="UsVetoGate(Country, DivisionRecord, BillConcern, bool, ref DocketEntry, bool)"/> with a deadline of the entry's own for a
        /// bill it puts on the docket (`DocketEntry.Deadline`; default for the period's rule).</summary>
        private bool? UsVetoGate(Country country, DivisionRecord passage, BillConcern concern, bool passed, ref DocketEntry docket, bool amendment, System.DateTime deadline)
        {
            if (Elections.PresidentialVeto.Applies(country.Id)) { return PolishDocketGate(country, passage, passed, ref docket, amendment ? Elections.PresidentialDocket.Bill.ConstitutionalAmendment : Elections.PresidentialDocket.Bill.Ordinary); }   // SP-7b: Poland's player-president's docket, at the gate every bill site reaches with its slot (an AI president answered at PresidentialVetoGate)
            if (docket != null) { passage = docket.Passage; passed = passage != null; }   // PS-6 US-21b: held from an earlier day - the division it was presented on
            if (!passed || passage == null || passage.OtherChamber == null || !Elections.UsVeto.Applies(country.Id)) { docket = null; return passed; }
            if (amendment) { docket = null; Debug.Log($"VETO: {country.Id} - an amendment is not presented to the President: {passage.Title}"); return passed; }
            System.Func<DivisionRecord, bool> answer = PlayerPresidentVetoes;
            System.DateTime answeredOn = CurrentDate;
            if (docket != null && docket.Answer != 0)
            {
                // the player-president's recorded answer, honoured whoever holds the presidency at this step (the review of 0c669fe6, M1): presented as of the
                // day he gave it, so it is his answer - a signature stays a signature across a change of presidency, a veto a veto
                bool vetoes = docket.Answer < 0;
                answer = _ => vetoes;
                answeredOn = docket.AnsweredOn == System.DateTime.MinValue ? CurrentDate : docket.AnsweredOn;
            }
            else if (answer == null && Congress.PresidentOn(country, CurrentDate, out string onTheDocket, out _, out bool playerPresident) && playerPresident)
            {
                if (docket == null)
                {
                    docket = new DocketEntry { Passage = passage, PresentedOn = CurrentDate.Date, Deadline = deadline.Date };
                    Debug.Log($"DOCKET: {country.Id} - presented to the President (you, {onTheDocket}) on {CurrentDate:yyyy-MM-dd}: {passage.Title} - answered by {DocketLastDay(country.Id, docket):yyyy-MM-dd}, or it becomes law {(docket.Deadline != System.DateTime.MinValue ? "as signed" : "without signature (" + Elections.PresidentialDocket.PeriodWords(country.Id) + ")")}");
                    return null;
                }
                if (docket.Deadline != System.DateTime.MinValue)
                {
                    // PS-6 US-22c (J7): a deadline of its own - unanswered by then, signed: presented to the player-president as signing it, today
                    if (CurrentDate.Date <= docket.Deadline) { return null; }   // the deadline is the last day to answer (the review's M2): signed at the first step after it
                    Debug.Log($"DOCKET: {country.Id} - unanswered by its deadline ({docket.Deadline:yyyy-MM-dd}): {passage.Title} - signed");
                    answer = _ => false;
                }
                else
                {
                    if (!Elections.PresidentialDocket.Expired(country.Id, docket.PresentedOn, CurrentDate)) { return null; }
                    Debug.Log($"DOCKET: {country.Id} - unanswered since {docket.PresentedOn:yyyy-MM-dd} ({Elections.PresidentialDocket.PeriodWords(country.Id)} ran out on {DocketLastDay(country.Id, docket):yyyy-MM-dd}): {passage.Title} - law without the President's signature");
                    docket = null;
                    return true;
                }
            }
            System.DateTime presentedOn = docket != null ? docket.PresentedOn : CurrentDate.Date;   // K41: the day it was presented - the docket's, or today's for an answer on the day
            docket = null;   // answered, or unanswered and the president of the day is not the player: he answers by his own rule, today
            Congress.Veto veto = Congress.Present(country, passage, answeredOn, answer);   // the president of the answer's day; the votes on a veto are held today (RecordVeto)
            if (veto == null)
            {
                Debug.LogError($"VETO: {country.Id} - no president found on {CurrentDate:yyyy-MM-dd} for '{passage.Title}' - signed by no rule, a defect");
                return passed;
            }
            if (!veto.Vetoed) { Debug.Log($"VETO: {country.Id} - signed by the President ({veto.President}): {passage.Title} - {veto.Why}"); return true; }
            DivisionRecord ceremony = Congress.RecordVeto(country, passage, veto, CurrentDate, presentedOn);
            if (!veto.Overridden) { Congress.RecordLost(country, concern, CurrentDate); }   // Elias's ruling F5: a veto that stands is a bill lost that day - the writer a chamber's rejection uses
            Debug.Log($"VETO: {country.Id} - {ceremony.Title} ({veto.Why})");
            return veto.Overridden;
        }

        /// <summary>
        /// PS-6 US-21 (R-US7, F8: "a player-president chooses"): THE PLAYER-PRESIDENT'S ANSWER ON THE DAY a bill is presented - true vetoes it, false signs it. Read
        /// by the US veto gate only where the president of the day is the player (`Congress.PresidentOn`); rule (a) is never read for him. PS-6 US-21b: null - the
        /// default, and play's - puts the bill on his DOCKET (`UsVetoGate`), where he answers through <see cref="SignDocketBill"/> or <see cref="VetoDocketBill"/>
        /// or it becomes law when the period runs out. Not saved: a delegate the checks set to answer on the day; neither a controller nor the film driver sets it.
        /// </summary>
        public System.Func<DivisionRecord, bool> PlayerPresidentVetoes;

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-21b (Elias's ruling I12 (i), 2026-10-18): THE DOCKET - the bills presented to a player-president and not yet answered, each in its own
        // pending slot with its entry (`DocketEntry`: the division it was presented on, the day, the answer given). The verbs record the answer; the bill's
        // own day step acts on it, so the answer, the override and the bill's application run where every bill's do - since US-21c (Elias's ruling J6 (i):
        // "an answer acts at once; its money moves on the next day step, as every law's does") the verbs run that step for the answered bill at once
        // (`ActOnDocketAnswers`), the day's other bills untouched. Its surface (PS-6 US-23) is SP-7b's plate and Docket card, one for both countries
        // (`GameController.PresidentialStart.cs`).
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>PS-6 US-21c: true while <see cref="ActOnDocketAnswers"/> runs the bill sites for the answered bills alone - every bill site skips a bill with
        /// no answer on its docket (<see cref="SkipsForAnswers"/>, <see cref="HasDocketAnswer"/>), so no countdown moves and no bill is voted.</summary>
        private bool _docketAnswersOnly;

        /// <summary>PS-6 US-21c: whether a bill site skips this bill on the answers' run - every bill but one whose docket holds an answer.</summary>
        private bool SkipsForAnswers(DocketEntry docket) => _docketAnswersOnly && (docket == null || docket.Answer == 0);

        /// <summary>PS-6 US-21c: whether the budget slot holds an answer - its continuing resolution's, or one of its held acts'.</summary>
        private static bool HasDocketAnswer(BudgetBill bill)
        {
            if (bill.Docket != null && bill.Docket.Answer != 0) { return true; }
            if (bill.ActsHeld != null) { foreach (DocketEntry entry in bill.ActsHeld) { if (entry.Answer != 0) { return true; } } }
            return false;
        }

        /// <summary>PS-6 US-21c (J6 (i)): the answered bills acted on at once - the country's bill sites run in the day tick's order with every unanswered bill
        /// skipped, so a signature is law today, a veto goes to the override today, and the law's money flows from the next day step (the economy's day runs
        /// before the bills', `AdvanceCountryDayTick`). The funding tick, the budget process and the confidence day are not run. The review's L2: a veto's
        /// override divisions written here reach the signing ceremony only when the caller queues them - the callers in play (the answer plate and the Docket
        /// page, `GameController.PresidentialStart.cs`: SP-7b's for Poland, wired for the USA by PS-6 US-23) queue them right after the answer, on the answer's day.</summary>
        private void ActOnDocketAnswers(CountryId countryId)
        {
            _docketAnswersOnly = true;
            try
            {
                AdvanceBudgetBillDay(countryId);
                AdvanceMajorityReconciliationDay(countryId);   // PS-6 US-23: the out-party majority's reconciliation, in the day tick's order
                AdvanceDebtLimitDay(countryId);   // PS-6 US-24: the debt limit's raise, in the day tick's order
                AdvanceTaxProgramBillsDay(countryId);
                AdvanceWelfareProgramBillsDay(countryId);
                AdvanceLaborBillDay(countryId);
                AdvanceCrimeJusticeBillDay(countryId);
                AdvanceSectorBillDay(countryId);
                AdvanceTradeBillDay(countryId);
                AdvanceSwfDrawdownBillDay(countryId);
                AdvanceLawBillsDay(countryId);
            }
            finally { _docketAnswersOnly = false; }
        }

        /// <summary>PS-6 US-22c: the last day to answer a bill on the docket - its own deadline where it carries one (`DocketEntry.Deadline`), else the country's
        /// period (`PresidentialDocket.LastDay`) - SP-7b: for the bill's kind (`DocketEntry.Bill`, J6 (iii): Poland's budget act 7 days, every other bill 21;
        /// ordinary on every US bill, so the USA's ten days).</summary>
        public static System.DateTime DocketLastDay(CountryId countryId, DocketEntry entry)
            => entry.Deadline != System.DateTime.MinValue ? entry.Deadline : Elections.PresidentialDocket.LastDay(countryId, entry.PresentedOn, (Elections.PresidentialDocket.Bill)entry.Bill);

        /// <summary>PS-6 US-22c: the day a bill on the docket is law unanswered - the day after its last day to answer: after its own deadline where it carries one
        /// (signed at that day's step), else after the period's last day (`PresidentialDocket.LawDay`) - SP-7b: for the bill's kind, as <see cref="DocketLastDay"/>.</summary>
        public static System.DateTime DocketLawDay(CountryId countryId, DocketEntry entry)
            => entry.Deadline != System.DateTime.MinValue ? entry.Deadline.AddDays(1) : Elections.PresidentialDocket.LawDay(countryId, entry.PresentedOn, (Elections.PresidentialDocket.Bill)entry.Bill);

        /// <summary>One bill on the docket, as a page or a check reads it: the division it was presented on, the day, the last day to answer
        /// (<see cref="DocketLastDay"/>) and the answer recorded (0 none, +1 signed, -1 vetoed - acted on at once).</summary>
        public readonly struct DocketItem
        {
            public readonly DocketEntry Entry;
            public readonly System.DateTime LastDay;
            public DocketItem(DocketEntry entry, System.DateTime lastDay) { Entry = entry; LastDay = lastDay; }
            public string Title => Entry.Passage?.Title;
            public System.DateTime PresentedOn => Entry.PresentedOn;
            public int Answer => Entry.Answer;
        }

        /// <summary>PS-6 US-21b: the country's docket - every bill held for the player-president's answer, in the order the slots are read (the budget's acts and
        /// a continuing resolution first, then the programme, policy and law bills).</summary>
        public List<DocketItem> GetDocket(CountryId countryId)
        {
            var items = new List<DocketItem>();
            foreach (DocketEntry entry in DocketEntries(countryId)) { items.Add(new DocketItem(entry, DocketLastDay(countryId, entry))); }
            return items;
        }

        /// <summary>PS-6 US-21b: the player-president signs a bill on his docket - <paramref name="passage"/> the division it was presented on (`DocketItem.Entry.Passage`).
        /// Refused (false, with the reason) where no such bill is held, where it is already answered, or where the president of the day is not the player.
        /// US-21c (J6 (i)): the bill is law at once, on the answer's day; its money moves on the next day step.</summary>
        public bool SignDocketBill(CountryId countryId, DivisionRecord passage, out string refusedBecause) => AnswerDocket(countryId, passage, +1, out refusedBecause);

        /// <summary>PS-6 US-21b: the player-president vetoes a bill on his docket. US-21c (J6 (i)): at once the veto goes to the override - the chamber the bill
        /// began in first, at two thirds on the passage's recorded sides (`Congress.Present`, `Congress.RecordVeto`). Refused as <see cref="SignDocketBill"/> is.</summary>
        public bool VetoDocketBill(CountryId countryId, DivisionRecord passage, out string refusedBecause) => AnswerDocket(countryId, passage, -1, out refusedBecause);

        private bool AnswerDocket(CountryId countryId, DivisionRecord passage, int answer, out string refusedBecause)
        {
            // FORK-2 (§886's L4; the fix reviews' M2 and M1): replayed by IDENTITY - the bill by what its passage is in every world: its title, its day, the
            // chamber it was held in and the chamber it began in. Never the division's number, which counts every division of the country and so parts the
            // moment a world holds one more or one fewer. No match, nothing is answered there. The no-policy world is handed no answer: its docket bills
            // become law at their term, as an unanswered bill does in the played world
            System.DateTime passed = passage?.Date ?? System.DateTime.MinValue;
            string title = passage?.Title;
            int chamber = passage?.Chamber ?? -1, origin = passage?.Origin ?? -1;
            string why = null;
            bool acted = Answered(new PlayerAnswer(answer > 0 ? "a docket bill signed" : "a docket bill vetoed", countryId, sim =>
            {
                DocketItem match = sim.GetDocket(countryId).Find(item => SamePassage(item.Entry.Passage, title, passed, chamber, origin));
                return match.Entry != null && sim.AnswerDocket(countryId, match.Entry.Passage, answer, out _);
            }), () => AnswerDocketNow(countryId, passage, answer, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2 (the fix review's M1): whether <paramref name="passage"/> is the passage of that title, day, chamber and origin - the docket answer's
        /// identity across worlds, which never reads the division's number.</summary>
        public static bool SamePassage(DivisionRecord passage, string title, System.DateTime date, int chamber, int origin) =>
            passage != null && passage.Title == title && passage.Date == date && passage.Chamber == chamber && passage.Origin == origin;

        /// <summary>FORK-2: <see cref="AnswerDocket"/>'s act.</summary>
        private bool AnswerDocketNow(CountryId countryId, DivisionRecord passage, int answer, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || passage == null) { refusedBecause = "no such bill"; return false; }
            if (Elections.PresidentialVeto.Applies(countryId)) { return AnswerPolishDocket(country, passage, answer, out refusedBecause); }   // SP-7b: Poland's player-president
            if (!Congress.PresidentOn(country, CurrentDate, out _, out _, out bool playerPresident) || !playerPresident) { refusedBecause = "you do not hold the presidency today"; return false; }
            DocketEntry answered = null;
            foreach (DocketEntry entry in DocketEntries(countryId))
            {
                if (!ReferenceEquals(entry.Passage, passage)) { continue; }
                if (entry.Answer != 0) { refusedBecause = "already answered"; return false; }
                answered = entry;
                break;
            }
            if (answered == null) { refusedBecause = "that bill is not on your docket"; return false; }
            answered.Answer = answer;
            answered.AnsweredOn = CurrentDate.Date;
            Debug.Log($"DOCKET: {countryId} - the President {(answer > 0 ? "signs" : "vetoes")} on {CurrentDate:yyyy-MM-dd}: {passage.Title} (acted on at once)");
            ActOnDocketAnswers(countryId);   // PS-6 US-21c (J6 (i)): after the walk - acting inside it would change the slots it walks
            return true;
        }

        /// <summary>Every docket entry held in the country's pending slots.</summary>
        private IEnumerable<DocketEntry> DocketEntries(CountryId countryId)
        {
            if (_pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill budget))
            {
                if (budget.ActsHeld != null) { foreach (DocketEntry e in budget.ActsHeld) { yield return e; } }
                if (budget.Docket != null) { yield return budget.Docket; }
            }
            if (_pendingMajorityReconciliationByCountry.TryGetValue(countryId, out BudgetBill majority) && majority.Docket != null) { yield return majority.Docket; }   // PS-6 US-23
            if (_pendingDebtLimitBillByCountry.TryGetValue(countryId, out BudgetBill limit) && limit.Docket != null) { yield return limit.Docket; }   // PS-6 US-24
            if (_pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var taxes)) { foreach (TaxProgramBill b in taxes.Values) { if (b.Docket != null) { yield return b.Docket; } } }
            if (_pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var welfare)) { foreach (WelfareProgramBill b in welfare.Values) { if (b.Docket != null) { yield return b.Docket; } } }
            if (_pendingLaborBillByCountry.TryGetValue(countryId, out LaborPolicyBill labor) && labor.Docket != null) { yield return labor.Docket; }
            if (_pendingCrimeJusticeBillByCountry.TryGetValue(countryId, out CrimeJusticePolicyBill crime) && crime.Docket != null) { yield return crime.Docket; }
            if (_pendingSectorBillByCountry.TryGetValue(countryId, out SectorPolicyBill sector) && sector.Docket != null) { yield return sector.Docket; }
            if (_pendingTradeBillByCountry.TryGetValue(countryId, out TradePolicyBill trade) && trade.Docket != null) { yield return trade.Docket; }
            if (_pendingSwfDrawdownBillByCountry.TryGetValue(countryId, out SwfDrawdownBill swf) && swf.Docket != null) { yield return swf.Docket; }
            if (_pendingLawBillsByCountry.TryGetValue(countryId, out var laws)) { foreach (LawBill b in laws.Values) { if (b.Docket != null) { yield return b.Docket; } } }
        }

        /// <summary>One line naming a budget bill's content for the log and the desk: the lines it moves and the rates it sets.</summary>
        public static string DescribeBudgetBill(BudgetBill bill)
        {
            if (bill == null) { return "none"; }
            var parts = new List<string>();
            int cut = 0, raised = 0; foreach (KeyValuePair<SpendingCategory, float> kv in bill.SpendingPercentChanges) { if (kv.Value < 0f) { cut++; } else if (kv.Value > 0f) { raised++; } }
            if (cut > 0) { parts.Add($"{cut} line(s) cut"); }
            if (raised > 0) { parts.Add($"{raised} line(s) raised"); }
            if (bill.SpendingNominalTargets.Count > 0) { parts.Add($"{bill.SpendingNominalTargets.Count} line(s) re-targeted"); }
            foreach (KeyValuePair<TaxType, float> kv in bill.TaxLines) { parts.Add($"{kv.Key} at {kv.Value:0.0} %"); }
            return parts.Count > 0 ? string.Join(", ", parts) : "the standing figures, unchanged";
        }

        public BudgetBill GetPendingBudgetBill(CountryId countryId)
        {
            return _pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill bill) ? bill : null;
        }

        /// <summary>
        /// Submits a new omnibus BudgetBill for this country - a no-op (returns false) if one is
        /// already pending, since only one bill may be before Parliament at a time (see
        /// _pendingBudgetBillByCountry's own doc comment). Also closes out the mandatory pause this
        /// bill's introduction was blocking (_pendingBudgetProcessByCountry) - introducing IS the
        /// action the pause exists to force, so time resumes the moment this succeeds, and the bill
        /// then resolves quietly in the background over BillDurationDays, never pausing again.
        /// DaysRemaining is set here, not by the caller, so GameController never has to know
        /// ParliamentSystem.BillDurationDays itself.
        /// </summary>
        public bool IntroduceBudgetBill(CountryId countryId, BudgetBill bill)
        {
            System.Func<BudgetBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a budget bill", countryId, sim => copy != null && sim.IntroduceBudgetBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.Budget), () => IntroduceBudgetBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceBudgetBill"/>'s act.</summary>
        private bool IntroduceBudgetBillNow(CountryId countryId, BudgetBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Budget, null, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingBudgetBillByCountry.ContainsKey(countryId))
            {
                return false;
            }
            // AF-9 (Elias's ruling q9, 2026-10-18: "the budget gates move into the model"): the budget window and the yearly range are the manager's
            // refusals, where until AF-9 only the Budget page held them - the button enabled while the process is open, the dial's track the range - and
            // any other caller was accepted on any day at any figure.
            if (!GetPendingBudgetProcess(countryId)) { Debug.Log($"LEVERS: {countryId} - a budget bill was refused: THE BUDGET PROCESS IS NOT OPEN"); return false; }
            string outside = BudgetBillOutsideYearlyRange(_world?.GetCountry(countryId), bill);
            if (outside != null) { Debug.Log($"LEVERS: {countryId} - a budget bill was refused: {outside}"); return false; }
            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingBudgetBillByCountry[countryId] = bill;
            _pendingBudgetProcessByCountry.Remove(countryId);
            _incomingBudgetWindowOpenNow.Remove(countryId);   // C-C2: the arrival window is spent the moment a bill is introduced.
            return true;
        }

        /// <summary>
        /// Counts down the pending BudgetBill (if any) by one day, resolving it PASS/FAIL via
        /// ParliamentSystem once DaysRemaining reaches 0 - called once per simulated day from
        /// GameController.Update's day-processing loop, independent of the 121-day turn boundary
        /// (unlike the mandatory pause that preceded introduction, resolving a bill never pauses time -
        /// it's a deterministic countdown, not something needing a player response, the same idiom the
        /// retired TaxBill/AdvanceLegislativeDay already established).
        /// </summary>
        public void AdvanceBudgetBillDay(CountryId countryId)
        {
            if (!_pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill bill))
            {
                return;
            }

            if (_docketAnswersOnly && !HasDocketAnswer(bill)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null && bill.ActsHeld == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            if (UsBudgetProcedure.Applies(country)) { ResolveUsBudgetDay(country, bill); return; }   // PS-6 US-22: the US Congress votes the budget as its acts, the government's and the player's alike, and a continuing resolution on its own
            if (bill.ActsHeld != null && Elections.WorldClock.StatutePartsAreActs(countryId)) { FinishPolishBudget(country, bill); return; }   // SP-7b: Poland's budget act and statute acts on the player-president's docket - decided as he answers
            if (bill.GovernmentBill) { ResolveGovernmentBudget(country, bill); CountBudgetVoteOnAgreements(country); if (bill.ActsHeld == null) { _pendingBudgetBillByCountry.Remove(countryId); _pendingBudgetAlternativeByCountry.Remove(countryId); } return; }   // PS-3e (§632); SP-7b: held on the player-president's docket, the bill keeps its slot
            float direction = ParliamentSystem.GetBillDirection(country, bill);
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, ParliamentSystem.BudgetActOf(country, bill));   // P3-A2: the chamber votes on what the bill concerns; §768/§773: Poland's budget act - the spending alone
            DivisionRecord budgetVote = ParliamentSystem.Divide(country, "Annual budget bill", concern, CurrentDate, out bool passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
            if (passed && HoldPolishBudget(country, bill, budgetVote)) { CountBudgetVoteOnAgreements(country); return; }   // SP-7b: adopted, Poland's budget act and its statute acts go to the player-president's docket - the slot kept
            DocketEntry unheld = null;   // PS-6 US-21b: never held here - a Congress votes its budget as its acts (ResolveUsBudgetDay), and a budget one chamber voted passes the US gate untouched
            bool? budgetDecided = UsVetoGate(country, budgetVote, concern, passed, ref unheld);   // PS-6 US-21: the president of the day reads every act both chambers send, budget acts included (rule (a) draws nothing on them)
            if (budgetDecided == null)
            {
                Debug.LogError($"DOCKET: {country.Id} - a budget voted outside the US procedure was held on a docket, a defect - it is decided as NOT PASSED (nothing applied, the failed vote charged): {budgetVote?.Title}");
                budgetDecided = false;
            }
            passed = budgetDecided.Value;
            BudgetBill applied = passed ? PolishStatuteActs(country, bill, out _) : bill;   // §768 (D4), §773 (E2): Poland's statute parts travel in their own acts
            float approvalBeforeBill = country.State.ApprovalRating;   // §768 (the second review): after the acts, each fall its own ledger event
            ParliamentSystem.ApplyBillResult(country, applied, passed, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // PS-6 US-22 (N1): charged where the government bears it - true in every country without a Congress
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Budget bill passed (tax changes)" : "Budget bill failed", country.State.ApprovalRating - approvalBeforeBill);
            _pendingBudgetBillByCountry.Remove(countryId);
            CountBudgetVoteOnAgreements(country);   // §654
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-22 (`docs/specs/USA_STAGE_PLAN.md` US-22; R-US9 (a) as ruled, F8): THE US BUDGET PROCEDURE. The president's request (the government's bill,
        // or the player's where the player's party holds the presidency) is voted as Congress's acts on E2's parts - the appropriation at cloture, the
        // reconciliation by a majority, the pension act at cloture (`UsBudgetProcedure`) - each its own division through both chambers (`Congress.Divide`) and
        // then the president (`UsVetoGate`). PS-6 US-22b (Elias's ruling I13 (3), 2026-10-18): where no appropriation for the coming year is law the day
        // before the horizon, a continuing resolution is tabled and decided that day, at cloture, so nothing withheld means no lapse; funding lapses the day
        // after its horizon, and a lapse ends when an act becomes law, on release, at the change of presidency, or through the withholder's own senators drawn
        // at cloture (`UsLapseDraw`). A lapse defers the non-excepted outlays and lowers output by CBO's estimate (`UsLapse`, I13 (1)).
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>PS-6 US-22: the US budget's day - a continuing resolution decided on its own, or the request's acts. US-22b (I13 (3)): a fallen appropriation
        /// tables nothing - the resolution comes the day before the horizon (<see cref="AdvanceUsFundingDay"/>) - except during a lapse, where the cadence goes
        /// on (<see cref="ContinueTheLapse"/>). The slot is emptied first, so a resolution tabled after it stands in it.</summary>
        private void ResolveUsBudgetDay(Country country, BudgetBill bill)
        {
            if (bill.ActsHeld != null) { FinishUsBudgetActs(country, bill); return; }   // PS-6 US-21b: acts on the player-president's docket - decided as he answers
            _pendingBudgetBillByCountry.Remove(country.Id);
            _pendingBudgetAlternativeByCountry.Remove(country.Id);   // never tabled in the US (TableShadowBudget refuses one); cleared as every budget's day clears it
            if (bill.ContinuingResolution) { ResolveContinuingResolution(country, bill); return; }
            bool? appropriated = UsBudgetActs(country, bill);
            CountBudgetVoteOnAgreements(country);   // §654: every budget's day (a presidency holds no support agreement)
            if (appropriated == null) { _pendingBudgetBillByCountry[country.Id] = bill; return; }   // PS-6 US-21b: an act is on the docket - the bill keeps its slot until every act is decided
            AfterUsBudgetActs(country, appropriated.Value);
        }

        /// <summary>PS-6 US-22: the request's acts decided - the appropriation law funds the government; fallen, nothing is tabled before the horizon's eve (US-22b, I13 (3)), and during a lapse the cadence goes on.</summary>
        private void AfterUsBudgetActs(Country country, bool appropriated)
        {
            if (appropriated) { FundedByLaw(country, "the appropriation"); }
            else if (country.LapseSince != System.DateTime.MinValue) { ContinueTheLapse(country); }
        }

        /// <summary>
        /// PS-6 US-22: THE REQUEST'S ACTS, Poland's pattern on the US procedure's parts (`UsBudgetProcedure.Acts`): each act voted where it is (the
        /// appropriation always - the standing figures, uncontested, where it moves no line; the reconciliation and the pension act only where they change
        /// something), through both chambers in the chamber the bill begins in (`Congress.OriginOf`: a revenue bill, so the House), then the president of the
        /// day (`UsVetoGate`: rule (a) for an AI president, the player-president's answer for a player). THE ACTS ARE INDEPENDENT (DECLARED, unlike Poland's
        /// E2): a fallen appropriation does not take the reconciliation down. Application starts from the whole bill and withholds each fallen act's part
        /// (`UsBudgetProcedure.Without` - never a part applied, which would dissolve the fund); each fallen act is charged as a failed budget is, where the
        /// government bears it (`Congress.GovernmentBears`, US-19's rule - the review's second-pass N1, closed), with a ledger event of its own; the remainder
        /// applies once where any act became law. Returns whether the appropriation became law.
        /// <para>PS-6 US-21b: where an act both chambers sent waits on the player-president's DOCKET, nothing is applied that day - the acts decided are kept on
        /// the bill (`BudgetBill.ActsLaw`, `ActsFallen`; a fallen act is charged on its day, as before) with the held ones (`ActsHeld`), and this returns
        /// null; the remainder applies once, the day the last held act is decided (<see cref="FinishUsBudgetActs"/>).</para>
        /// </summary>
        private bool? UsBudgetActs(Country country, BudgetBill bill)
        {
            BudgetBill applied = bill;
            bool appropriated = false, reconciled = true, anyLaw = false;
            List<DocketEntry> held = null;
            var law = new List<int>();
            var fallen = new List<int>();
            foreach (UsBudgetProcedure.Act act in UsBudgetProcedure.Acts)
            {
                if (!UsBudgetProcedure.IsVoted(bill, act, country)) { continue; }
                BudgetBill part = UsBudgetProcedure.PartOf(bill, act, country);
                BillConcern concern = UsBudgetProcedure.ConcernOf(country, bill, act);
                string title = UsActTitle(country, act, part);
                DivisionRecord passage = Congress.Divide(country, title, concern, CurrentDate, out bool passed, BillAxis.Fiscal, Congress.OriginOf(country, concern, CurrentDate));
                DocketEntry docket = null;
                bool? decided = UsVetoGate(country, passage, concern, passed, ref docket);   // PS-6 US-21: every act both chambers send goes to the president (a budget vote draws no defector)
                if (decided == null)
                {
                    docket.Act = (int)act;
                    (held ??= new List<DocketEntry>()).Add(docket);
                    Debug.Log($"BUDGET: {country.Id} - {title}: on the President's docket - it applies the day it is decided");
                    continue;
                }
                passed = decided.Value;
                Debug.Log($"BUDGET: {country.Id} - {title}: {(passed ? "law - it applies" : "falls - what it would set stays as it is, and the rest applies")}");
                if (act == UsBudgetProcedure.Act.Appropriation) { appropriated = passed; }
                else if (act == UsBudgetProcedure.Act.Reconciliation) { reconciled = passed; }
                if (passed) { anyLaw = true; law.Add((int)act); continue; }
                fallen.Add((int)act);
                applied = UsBudgetProcedure.Without(applied, act, country);
                float approvalBefore = country.State.ApprovalRating;
                ParliamentSystem.ApplyBillResult(country, part, false, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // a fallen act costs what a failed budget costs - where the government bears it
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, UsBudgetProcedure.ActWords(act) + " failed", country.State.ApprovalRating - approvalBefore);
            }
            if (held != null) { bill.ActsHeld = held; bill.ActsLaw = law; bill.ActsFallen = fallen; return null; }
            if (!anyLaw) { return false; }
            EnactUsBudgetActs(country, bill, applied, appropriated, reconciled);
            return appropriated;
        }

        /// <summary>PS-6 US-22: the acts that became law, applied once - the whole bill less each fallen act's part.</summary>
        private void EnactUsBudgetActs(Country country, BudgetBill bill, BudgetBill applied, bool appropriated, bool reconciled)
        {
            float approvalBeforeLaw = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, applied, true, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year);
            if (appropriated) { FinancePartner.CreditAdopted(country, bill, ratesLanded: reconciled); }   // §755: counted only where the appropriation stood (the budget act's analogue), its rates only where the reconciliation did
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, "Budget acts enacted", country.State.ApprovalRating - approvalBeforeLaw);
        }

        /// <summary>PS-6 US-21b: a budget day with acts on the player-president's docket - each held act decided where the gate decides it today (his answer, or
        /// law when the period has run unanswered; a veto to the override), a fallen one charged on its day; once none is held the slot is emptied and the
        /// acts complete as <see cref="UsBudgetActs"/> would have on one day: the whole bill less every fallen act's part applied once where any act is law, then
        /// the appropriation funds the government or a continuing resolution is tabled.</summary>
        private void FinishUsBudgetActs(Country country, BudgetBill bill)
        {
            var still = new List<DocketEntry>();
            foreach (DocketEntry entry in bill.ActsHeld)
            {
                var act = (UsBudgetProcedure.Act)entry.Act;
                BillConcern concern = UsBudgetProcedure.ConcernOf(country, bill, act);
                DocketEntry docket = entry;
                bool? decided = UsVetoGate(country, entry.Passage, concern, true, ref docket);
                if (decided == null) { still.Add(entry); continue; }
                Debug.Log($"BUDGET: {country.Id} - {entry.Passage?.Title}: {(decided.Value ? "law - it applies" : "falls - what it would set stays as it is, and the rest applies")}");
                if (decided.Value) { bill.ActsLaw.Add(entry.Act); continue; }
                bill.ActsFallen.Add(entry.Act);
                float approvalBefore = country.State.ApprovalRating;
                ParliamentSystem.ApplyBillResult(country, UsBudgetProcedure.PartOf(bill, act, country), false, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // a fallen act costs what a failed budget costs - where the government bears it
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, UsBudgetProcedure.ActWords(act) + " failed", country.State.ApprovalRating - approvalBefore);
            }
            if (still.Count > 0) { bill.ActsHeld = still; return; }
            bill.ActsHeld = null;
            _pendingBudgetBillByCountry.Remove(country.Id);
            BudgetBill applied = bill;
            foreach (UsBudgetProcedure.Act act in UsBudgetProcedure.Acts) { if (bill.ActsFallen.Contains((int)act)) { applied = UsBudgetProcedure.Without(applied, act, country); } }
            bool appropriated = bill.ActsLaw.Contains((int)UsBudgetProcedure.Act.Appropriation);
            if (bill.ActsLaw.Count > 0) { EnactUsBudgetActs(country, bill, applied, appropriated, !bill.ActsFallen.Contains((int)UsBudgetProcedure.Act.Reconciliation)); }
            AfterUsBudgetActs(country, appropriated);
        }

        /// <summary>PS-6 US-22: a continuing resolution's day - voted at cloture through both chambers (asked nothing, and so uncontested, unless the player's
        /// party withholds it: Ask 2's reading (P), PROVISIONAL), then presented like every act (US-21's rule: an uncontested resolution a player-president
        /// vetoes is overridden on its whole seated base, so it becomes law). Law, it funds the government; fallen - rejected, or its veto sustained - funding is
        /// not advanced, its failed vote charged where the government bears it, and while funding is lapsed the government tables another the same day.
        /// PS-6 US-21b: on the player-president's docket it keeps the slot, and is decided the day he answers or the period runs out (no fresh vote).
        /// US-22c (J7): decided before funding lapses, its deadline on the docket is the funding horizon - signed at the next day's step if unanswered.
        /// US-22b (I13 (2)): voted while funding is lapsed, the withholding party's own senators are drawn at its cloture vote (`UsLapseDraw`, through
        /// `BillConcern.DrawsWithholders` and `Congress.Divide`) - so a lapse can end through its defectors.</summary>
        private void ResolveContinuingResolution(Country country, BudgetBill resolution)
        {
            if (UsBudgetProcedure.WithholdLapsesWithPresidency(country)) { country.ContinuingResolutionWithheldBy = null; country.ContinuingResolutionWithheldOn = System.DateTime.MinValue; }   // the withholding party now holds the presidency: the resolution is its own government's, so the withhold lapses
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, resolution);
            concern.DrawsWithholders = country.LapseSince != System.DateTime.MinValue;   // PS-6 US-22b (I13 (2)): "on each resolution's cloture vote during a lapse"
            DivisionRecord passage = resolution.Docket?.Passage;   // PS-6 US-21b: held, it was voted on an earlier day
            string title = passage?.Title ?? UsBudgetProcedure.ContinuingResolutionTitle(country, CurrentDate);
            bool passed = true;
            if (resolution.Docket == null) { passage = Congress.Divide(country, title, concern, CurrentDate, out passed, BillAxis.Fiscal, Congress.OriginOf(country, concern, CurrentDate)); }
            System.DateTime deadline = country.LapseSince == System.DateTime.MinValue && country.AppropriatedThrough >= CurrentDate.Date ? country.AppropriatedThrough : default;   // PS-6 US-22c (J7): before a lapse, "the horizon as its deadline"
            bool? decided = UsVetoGate(country, passage, concern, passed, ref resolution.Docket, false, deadline);
            if (decided == null) { _pendingBudgetBillByCountry[country.Id] = resolution; return; }   // PS-6 US-21b: on the docket - it keeps the slot
            passed = decided.Value;
            if (passed) { FundedByLaw(country, "the continuing resolution"); return; }
            float approvalBefore = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, resolution, false, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // the failed vote's cost only - a resolution is never applied
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, UsBudgetProcedure.ContinuingResolutionWords + " failed", country.State.ApprovalRating - approvalBefore);
            Debug.Log($"BUDGET: {country.Id} - {title}: falls - funding is not advanced (through {country.AppropriatedThrough:yyyy-MM-dd})");
            if (country.LapseSince != System.DateTime.MinValue) { ContinueTheLapse(country); }   // the cadence while lapsed: again the day each one falls (DECLARED)
        }

        /// <summary>PS-6 US-22: a funding measure became law today - the horizon advanced (`UsBudgetProcedure.FundedThrough`: the fiscal year in progress where
        /// it is unfunded, else the next; never further), a lapse ended with its days, and a withheld resolution released (DECLARED: the next one is withheld
        /// afresh).</summary>
        private void FundedByLaw(Country country, string measure)
        {
            System.DateTime was = country.AppropriatedThrough;
            country.AppropriatedThrough = UsBudgetProcedure.FundedThrough(was, CurrentDate);
            string ended = country.LapseSince == System.DateTime.MinValue ? string.Empty
                : $"; the shutdown that began {country.LapseSince:yyyy-MM-dd} ends after {UsBudgetProcedure.LapseDays(country.LapseSince, CurrentDate)} day(s)";
            UsLapse.NoteReferenceWeek(country, CurrentDate);   // PS-6 US-22c (J7 (iv)): the lapse's last day is one of its days - a reference week it completes is counted
            if (country.LapseSince != System.DateTime.MinValue) { country.LapseRestoredOn = CurrentDate.Date; }   // K42: the day funding was restored, saved - the Desk's chip reads it
            country.LapseSince = System.DateTime.MinValue;
            country.LapseFlowDay = System.DateTime.MinValue;   // PS-6 US-22c (the review's M1): disarmed here, not only at the next funding tick - a docket answer that ends a lapse between the day steps (`ActOnDocketAnswers`) must not leave the next day's economy running one more lapsed day (on the tick's own path the flag is already spent, and the tick would disarm it the same day)
            country.ContinuingResolutionWithheldBy = null;
            country.ContinuingResolutionWithheldOn = System.DateTime.MinValue;   // PS-6 K75
            Debug.Log($"BUDGET: {country.Id} - {measure} becomes law: funded through {country.AppropriatedThrough:yyyy-MM-dd} (was {was:yyyy-MM-dd}){ended}");
        }

        /// <summary>PS-6 US-22: a continuing resolution laid before Congress by the government of the day - the AI's where it governs (the government's bill),
        /// the player's where the player's party holds the presidency (DECLARED: the plan's "the same author" read as the government of the day; they differ
        /// only across a change of presidency). It occupies the budget slot for a bill's whole course (`ParliamentSystem.BillDurationDays`) - US-22b (the
        /// review's M1): while funding is lapsed, the record's spacing between its lapse-period cloture votes (`UsLapseDraw.RecordSpacingDays`, DECLARED from the
        /// dated votes the withholder's draw is fitted on, one draw per vote) - and no window stands beside it (§632's rule for the slot: a window left open by a
        /// governing player who tabled nothing closes, as the government's own tabling closes it, `TableGovernmentBudget`).</summary>
        private void TableContinuingResolution(Country country)
        {
            if (_pendingBudgetBillByCountry.ContainsKey(country.Id)) { return; }
            var resolution = NewContinuingResolution(country, country.LapseSince != System.DateTime.MinValue ? Elections.UsLapseDraw.RecordSpacingDays : ParliamentSystem.BillDurationDays);
            _pendingBudgetBillByCountry[country.Id] = resolution;
            _pendingBudgetProcessByCountry.Remove(country.Id);
            _incomingBudgetWindowOpenNow.Remove(country.Id);
            Debug.Log($"BUDGET: {country.Id} - {(resolution.GovernmentBill ? "the government" : country.PlayerPartyAbbrev)} tables a continuing resolution at the standing figures; Congress decides in {resolution.DaysRemaining} day(s), at cloture");
        }

        /// <summary>A continuing resolution by the government of the day, its course <paramref name="days"/> - and the withhold lapsed first where the withholding
        /// party now holds the presidency (the resolution is its own government's).</summary>
        private BudgetBill NewContinuingResolution(Country country, int days)
        {
            if (UsBudgetProcedure.WithholdLapsesWithPresidency(country)) { country.ContinuingResolutionWithheldBy = null; country.ContinuingResolutionWithheldOn = System.DateTime.MinValue; }   // the withholding party now holds the presidency: the resolution is its own government's, so the withhold lapses
            return new BudgetBill { ContinuingResolution = true, GovernmentBill = !PlayerGoverns(country), DaysRemaining = days };
        }

        /// <summary>PS-6 US-22b (Elias's ruling I13 (3)): THE CADENCE BEFORE THE HORIZON - "when no appropriation for the coming year is law the day before the
        /// horizon, a resolution is tabled that day, so nothing withheld means no lapse". Tabled by the government of the day and DECIDED THAT DAY (no bill's
        /// course: a resolution asked nothing passes on the day it is tabled, and a withheld one falls on it), so it occupies no slot and no window closes for
        /// it - UNCONDITIONALLY, whatever is before Congress (the review's L1). A request still before Congress is decided on its own day after it, and its
        /// appropriation funds by `UsBudgetProcedure.FundedThrough`: law by the horizon, the resolution's year already funded, nothing moves (never further
        /// than the next year); law after it, the request's appropriation funds the NEXT fiscal year (the rule's "else the next", DECLARED). US-21b with US-22b
        /// (the second review's N1): a resolution decided here BEFORE funding lapses was taken as signed on the day by a player-president, never docketed.
        /// PS-6 US-22c (Elias's ruling J7, 2026-10-19, replacing N1's reading: "The eve's resolution goes on the Docket with the horizon as its deadline.
        /// Unanswered by then, it is signed, so a lapse needs the player's veto or a withhold"): a resolution decided here before funding lapses (the eve's, and
        /// the release's on the horizon) meets a player-president's DOCKET with the funding horizon as its deadline (`ResolveContinuingResolution`), and
        /// unanswered by then it is signed - at the step after the horizon, in the budget tick, which runs before that day's funding tick, so the horizon has
        /// moved before the lapse test reads it and nothing withheld still means no lapse; the player may answer on the horizon itself (the review's M2). While funding is lapsed a resolution meets his docket on the period's rule. Either way,
        /// where another bill holds the slot it is taken as signed on the day (no second slot, DECLARED). Also how a RELEASE ends a lapse (the review's M2,
        /// <see cref="AdvanceUsFundingDay"/>).</summary>
        private void DecideContinuingResolutionToday(Country country, string why)
        {
            BudgetBill resolution = NewContinuingResolution(country, 0);
            Debug.Log($"BUDGET: {country.Id} - {why} ({country.AppropriatedThrough:yyyy-MM-dd}): {(resolution.GovernmentBill ? "the government" : country.PlayerPartyAbbrev)} tables a continuing resolution at the standing figures, decided today at cloture");
            System.Func<DivisionRecord, bool> answer = PlayerPresidentVetoes;
            if (answer == null && _pendingBudgetBillByCountry.ContainsKey(country.Id)) { PlayerPresidentVetoes = _ => false; }   // no second slot: the player-president's own government's resolution is signed on the day (US-22c keeps this half of N1)
            try { ResolveContinuingResolution(country, resolution); }
            finally { PlayerPresidentVetoes = answer; }
        }

        /// <summary>PS-6 US-22: THE CADENCE WHILE LAPSED (DECLARED; the record's repeated cloture votes; I13 (4) amends no. 7 only before the horizon): on the
        /// lapse's first day and the day each resolution falls, the government of the day tables another, on the record's spacing (M1) - where no bill is already before
        /// Congress. Where the player's party holds the presidency the resolution is tabled for it (DECLARED: the plan's "a player who governs gets the budget
        /// window during a lapse" is not built - the resolution occupies the slot; the request's day withdraws it, `TryOpenBudgetProcess`).</summary>
        private void ContinueTheLapse(Country country)
        {
            if (_pendingBudgetBillByCountry.ContainsKey(country.Id)) { return; }
            TableContinuingResolution(country);
        }

        /// <summary>PS-6 US-22: THE US FUNDING DAY, in the day tick after the budget's - the day after the horizon (`Country.AppropriatedThrough`) funding lapses
        /// (31 U.S.C. 1341(a)(1)) and the lapse opens, said on screen from then until a funding measure becomes law. US-22b (I13 (3)): on the day before the
        /// horizon, unlapsed, a continuing resolution is decided (<see cref="DecideContinuingResolutionToday"/>); and the tick ARMS the next day's flows while
        /// funding is lapsed (`Country.LapseFlowDay`, read by `UsLapse` - the economy's day runs before this tick). THE RELEASE (the review's M2: "on release"
        /// ends a lapse): where nothing is withheld while funding is lapsed, or on the horizon itself after the eve's resolution fell, an uncontested resolution
        /// is decided on this tick - a resolution waiting out its course in the slot withdrawn for it; one on the player-president's docket is his to answer,
        /// and keeps the lapse open until it is law (US-21b). In this build a lapse has nothing withheld only once its withholder releases (or the withhold
        /// lapsed with the presidency, or a player-president's veto of an uncontested resolution failed to stand). Nothing where the US Congress votes no
        /// budget or where no horizon is held.</summary>
        private void AdvanceUsFundingDay(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (!UsBudgetProcedure.Applies(country) || country.AppropriatedThrough == System.DateTime.MinValue) { return; }
            bool lapsed = country.LapseSince != System.DateTime.MinValue;
            UsBudgetProcedure.NoteWithholderInOffice(country);   // PS-6 K75 (the review's L1): the withholding party out of the presidency today is marked out of it
            if (UsBudgetProcedure.WithholdLapsesWithPresidency(country)) { country.ContinuingResolutionWithheldBy = null; country.ContinuingResolutionWithheldOn = System.DateTime.MinValue; }   // the review's L6: the withholding party now holds the presidency - the withhold lapses here too, so the release below decides a resolution on this tick whatever else is before Congress
            if (!lapsed && CurrentDate.Date == UsBudgetProcedure.HorizonEve(country.AppropriatedThrough))
            {
                DecideContinuingResolutionToday(country, "no appropriation for the coming year is law on the day before the horizon");
            }
            else if (country.ContinuingResolutionWithheldBy == null && (lapsed || CurrentDate.Date == country.AppropriatedThrough))
            {
                _pendingBudgetBillByCountry.TryGetValue(country.Id, out BudgetBill pending);
                if (pending == null || !pending.ContinuingResolution || pending.Docket == null)
                {
                    if (pending != null && pending.ContinuingResolution) { _pendingBudgetBillByCountry.Remove(country.Id); }
                    DecideContinuingResolutionToday(country, lapsed ? "the continuing resolution is released during the lapse" : "the continuing resolution is released after the eve's fell");
                }
            }
            if (country.LapseSince == System.DateTime.MinValue && CurrentDate.Date > country.AppropriatedThrough)
            {
                country.LapseSince = CurrentDate.Date;
                Debug.Log($"BUDGET: {country.Id} - funding lapsed on {CurrentDate:yyyy-MM-dd}: no appropriation or continuing resolution in force after {country.AppropriatedThrough:yyyy-MM-dd} - the government shuts down: its non-excepted outlays stop until funding returns");
                ContinueTheLapse(country);
            }
            System.DateTime flows = country.LapseSince != System.DateTime.MinValue ? CurrentDate.Date.AddDays(1) : System.DateTime.MinValue;
            if (country.LapseFlowDay != flows) { country.LapseFlowDay = flows; }
            UsLapse.NoteReferenceWeek(country, CurrentDate);   // PS-6 US-22c (J7 (iv)): a household survey's reference week the lapse has covered whole - its month's unemployment counts the furloughed (nothing where none is lapsed)
        }

        /// <summary>
        /// PS-6 US-22 (Ask 2's reading (P), PROVISIONAL - Elias's to rule): THE PLAYER'S PARTY WITHHOLDS (or releases) THE CONTINUING RESOLUTION. Withheld, it
        /// stands AGAINST the resolution in both chambers and every other party FOR (`StanceModel`'s fixed sides); it stands until released or until funding
        /// is restored. Only while a resolution is before Congress or due (US-22b: the one decided the day before the horizon is withheld in advance -
        /// `UsBudgetProcedure.MayWithhold`) - PS-6 K75: by the president's own party too (Elias's ruling of 2026-10-23), its withhold lapsing with the presidency only where its party took the presidency after it (`UsBudgetProcedure.WithholdLapsesWithPresidency`) - always releasable. False with the reason where refused. An AI party never withholds (DECLARED, §636's precedent).
        /// </summary>
        public bool WithholdContinuingResolution(CountryId countryId, bool withhold, out string refusedBecause)
        {
            string why = null;   // FORK-2: a withhold or a release, given again in every except-world on its day
            bool acted = Answered(new PlayerAnswer(withhold ? "a resolution withheld" : "a resolution released", countryId, sim => sim.WithholdContinuingResolution(countryId, withhold, out _)),
                () => WithholdContinuingResolutionNow(countryId, withhold, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="WithholdContinuingResolution"/>'s act.</summary>
        private bool WithholdContinuingResolutionNow(CountryId countryId, bool withhold, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || !UsBudgetProcedure.Applies(country)) { refusedBecause = UsBudgetProcedure.NoResolutionWords; return false; }
            if (!withhold)
            {
                country.ContinuingResolutionWithheldBy = null;
                country.ContinuingResolutionWithheldOn = System.DateTime.MinValue;   // PS-6 K75
                Debug.Log($"BUDGET: {countryId} - {country.PlayerPartyAbbrev} releases the continuing resolution");
                return true;
            }
            if (!UsBudgetProcedure.MayWithhold(country, GetPendingBudgetBill(countryId), CurrentDate, out refusedBecause)) { return false; }
            country.ContinuingResolutionWithheldBy = country.PlayerPartyAbbrev;
            country.ContinuingResolutionWithheldOn = CurrentDate.Date;   // PS-6 K75: the day it was given
            country.ContinuingResolutionWithheldInOffice = UsBudgetProcedure.HoldsPresidency(country, country.PlayerPartyAbbrev);   // the review's L1: given in office, it stands while the party never leaves it
            Debug.Log($"BUDGET: {countryId} - {country.PlayerPartyAbbrev} withholds the continuing resolution: it votes against it in both chambers");
            return true;
        }

        /// <summary>PS-6 US-22: an act's title as a player reads it - what it moves: the appropriation's lines (or "the standing figures"), the reconciliation's
        /// rates, benefit levels and fund rules in the statute acts' own words, the pension act's age as Poland's pension act says it.</summary>
        private static string UsActTitle(Country country, UsBudgetProcedure.Act act, BudgetBill part)
        {
            switch (act)
            {
                case UsBudgetProcedure.Act.Appropriation:
                    return UsBudgetProcedure.ActWords(act) + ": " + (Congress.ChangesSpendingLines(country, part) ? DescribeBudgetBill(part) : "the standing figures");
                case UsBudgetProcedure.Act.PensionAct:
                    return StatuteActTitle(country, BudgetBill.StatutePart.PensionAge, part);
                default:
                    var moves = new List<string>();
                    foreach (BudgetBill.StatutePart statute in new[] { BudgetBill.StatutePart.Rates, BudgetBill.StatutePart.Benefits, BudgetBill.StatutePart.Fund })
                    {
                        if (!part.Changes(statute, country)) { continue; }
                        moves.Add(StatuteActTitle(country, statute, part).Substring(ActWords(statute).Length + 2));   // the moves after the statute act's own name and ": "
                    }
                    return UsBudgetProcedure.ActWords(act) + ": " + (moves.Count > 0 ? string.Join("; ", moves) : "the standing rates");
            }
        }

        /// <summary>
        /// The Spending/SWF half of BudgetBill application that ParliamentSystem.ApplyBillResult
        /// delegates back here (passed in as a callback so ParliamentSystem never needs a direct
        /// dependency on SimulationManager's private internals) - reuses ApplySpendingLineChanges/
        /// ApplySwfPolicyChanges via a throwaway PolicyDecision built from the bill's own fields,
        /// rather than duplicating their clamping logic, plus direct SWF create/dissolve handling
        /// (neither existing function owns that - both assume the fund's existence is already settled
        /// going in). Only ever called from ApplyBillResult on a PASS - never on FAIL, never standalone.
        /// </summary>
        private void ApplyBudgetBillSpendingAndSwf(Country country, BudgetBill bill)
        {
            var spendingDecision = new PolicyDecision { SpendingLineChanges = bill.SpendingPercentChanges, SpendingNominalTargets = bill.SpendingNominalTargets, SpendingPinChanges = bill.SpendingPinChanges };   // P5-B2: the bill carries the set figures and the pins too
            ApplySpendingLineChanges(country, spendingDecision);

            if (!bill.SwfShouldExist)
            {
                // AF-9 (Elias's ruling q9, 2026-10-18: "a dissolved fund's assets are credited"; GA-12-2): the assets go to the debt stock, a one-time
                // settlement on F1's path (ApplyOneTimeBudgetImpact: the stock and the Budget accumulator, nominal with nominal) observed on the debt
                // ledger as the drawdown's is. Until AF-9 a dissolution discarded them (GA-12-2).
                SovereignWealthFund dissolved = country.SovereignWealthFund;
                country.SovereignWealthFund = null;
                if (dissolved != null && dissolved.TotalAssets > 0f)
                {
                    float debtBeforeDissolution = country.State.GovernmentDebt;
                    ApplyOneTimeBudgetImpact(country, dissolved.TotalAssets);
                    DebtLedgerRecorder.RecordEvent(country, CurrentDate, "Sovereign fund dissolved", debtBeforeDissolution, country.State.GovernmentDebt);
                }
                // AF-35 (the second review's N3): the period's return figures were the dissolved fund's - zeroed, so a fund created later in the same period
                // earns from the next boundary's draw and inherits nothing. The live country's period only: a preview clone shares this manager's table by id.
                if (dissolved != null && ReferenceEquals(_world?.GetCountry(country.Id), country) && _fiscalPeriods.TryGetValue(country.Id, out FiscalPeriod dissolvedPeriod))
                {
                    dissolvedPeriod.PlannedSwfReturn = 0f;
                    dissolvedPeriod.SwfAssetsAtPeriodOpen = 0f;
                    dissolvedPeriod.AccruedSwfReturns = 0f;
                }
                return;
            }

            if (country.SovereignWealthFund == null)
            {
                country.SovereignWealthFund = new SovereignWealthFund();
            }

            var swfDecision = new PolicyDecision
            {
                SwfContributionRateOverride = bill.SwfContributionRatePercent,
                SwfDomesticAllocationOverride = bill.SwfDomesticAllocationPercent,
                SwfEquitiesWeightOverride = bill.SwfEquitiesWeight,
                SwfBondsWeightOverride = bill.SwfBondsWeight,
                SwfInfrastructureWeightOverride = bill.SwfInfrastructureWeight,
                SwfRealEstateWeightOverride = bill.SwfRealEstateWeight
            };
            ApplySwfPolicyChanges(country, swfDecision);
        }

        /// <summary>Every TaxProgramBill currently pending for this country, or an empty collection if none - see _pendingTaxProgramBillsByCountry's own doc comment.</summary>
        public IEnumerable<TaxProgramBill> GetPendingTaxProgramBills(CountryId countryId)
        {
            return _pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending) ? pending.Values : System.Array.Empty<TaxProgramBill>();
        }

        /// <summary>Submits a new standalone TaxProgramBill - a no-op (returns false) if one is already pending for this SAME TaxType (different TaxTypes may all have their own bill pending at once - see _pendingTaxProgramBillsByCountry's own doc comment). No mandatory pause to close - unlike IntroduceBudgetBill, this tier never blocks time in the first place.</summary>
        public bool IntroduceTaxProgramBill(CountryId countryId, TaxType type, bool isAdd) =>
            Answered(new PlayerAnswer("a tax programme bill", countryId, sim => sim.IntroduceTaxProgramBill(countryId, type, isAdd), answersAWait: false, bill: Congress.Kind.TaxProgram), () => IntroduceTaxProgramBillNow(countryId, type, isAdd));   // FORK-2

        /// <summary>FORK-2: <see cref="IntroduceTaxProgramBill"/>'s act.</summary>
        private bool IntroduceTaxProgramBillNow(CountryId countryId, TaxType type, bool isAdd)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.TaxProgram, CabinetPortfolio.FinanceTreasury, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (!_pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<TaxType, TaxProgramBill>();
                _pendingTaxProgramBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(type))
            {
                return false;
            }

            pending[type] = new TaxProgramBill { Type = type, IsAdd = isAdd, DaysRemaining = ParliamentSystem.BillDurationDays };
            return true;
        }

        /// <summary>Counts down every pending TaxProgramBill for this country by one day, resolving any that reach 0 - called once per simulated day, the same non-blocking idiom AdvanceBudgetBillDay already established.</summary>
        public void AdvanceTaxProgramBillsDay(CountryId countryId)
        {
            if (!_pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<TaxType>();
            foreach (TaxProgramBill bill in pending.Values)
            {
                if (SkipsForAnswers(bill.Docket)) { continue; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
                if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                float direction = ParliamentSystem.GetTaxProgramBillDirection(country, bill);
                BillConcern concern = ParliamentSystem.GetTaxProgramBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
                DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
                bool passed = true;
                if (bill.Docket == null)
                {
                    vetoable = ParliamentSystem.Divide(country, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type}", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                    passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
                }
                bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
                if (decided == null) { continue; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
                passed = decided.Value;
                float approvalBeforeTaxBill = country.State.ApprovalRating;
                bool bears = Congress.GovernmentBears(country, concern);
                TaxLevyMove levyMove = ParliamentSystem.ApplyTaxProgramBillResult(country, bill, passed, bears);   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
                TaxTerm.ChargeLevyMove(country, levyMove, bears);   // A-LOOP q37 (§1 (2), §2 (2)): a levy is a rise from 0 against the year's opening, a removal a cut at the mirror, where the government bears the bill (review 878's L-1) - recorded with the bill on the next line
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} bill {(passed ? "passed" : "failed")}", country.State.ApprovalRating - approvalBeforeTaxBill);
                resolved.Add(bill.Type);
            }

            foreach (TaxType type in resolved)
            {
                pending.Remove(type);
            }
        }

        /// <summary>WelfareProgramType equivalent of GetPendingTaxProgramBills - see that method's own doc comment, same pattern.</summary>
        public IEnumerable<WelfareProgramBill> GetPendingWelfareProgramBills(CountryId countryId)
        {
            return _pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending) ? pending.Values : System.Array.Empty<WelfareProgramBill>();
        }

        /// <summary>WelfareProgramType equivalent of IntroduceTaxProgramBill - see that method's own doc comment, same pattern.</summary>
        public bool IntroduceWelfareProgramBill(CountryId countryId, WelfareProgramType type, bool isAdd) =>
            Answered(new PlayerAnswer("a welfare programme bill", countryId, sim => sim.IntroduceWelfareProgramBill(countryId, type, isAdd), answersAWait: false, bill: Congress.Kind.WelfareProgram), () => IntroduceWelfareProgramBillNow(countryId, type, isAdd));   // FORK-2

        /// <summary>FORK-2: <see cref="IntroduceWelfareProgramBill"/>'s act.</summary>
        private bool IntroduceWelfareProgramBillNow(CountryId countryId, WelfareProgramType type, bool isAdd)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.WelfareProgram, CabinetPortfolio.HealthSocialAffairs, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (!_pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<WelfareProgramType, WelfareProgramBill>();
                _pendingWelfareProgramBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(type))
            {
                return false;
            }

            pending[type] = new WelfareProgramBill { Type = type, IsAdd = isAdd, DaysRemaining = ParliamentSystem.BillDurationDays };
            return true;
        }

        /// <summary>WelfareProgramType equivalent of AdvanceTaxProgramBillsDay - see that method's own doc comment, same pattern.</summary>
        public void AdvanceWelfareProgramBillsDay(CountryId countryId)
        {
            if (!_pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<WelfareProgramType>();
            foreach (WelfareProgramBill bill in pending.Values)
            {
                if (SkipsForAnswers(bill.Docket)) { continue; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
                if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                float direction = ParliamentSystem.GetWelfareProgramBillDirection(country, bill);
                BillConcern concern = ParliamentSystem.GetWelfareProgramBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
                DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
                bool passed = true;
                if (bill.Docket == null)
                {
                    vetoable = ParliamentSystem.Divide(country, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type}", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                    passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
                }
                bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
                if (decided == null) { continue; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
                passed = decided.Value;
                float approvalBeforeWelfareBill = country.State.ApprovalRating;
                ParliamentSystem.ApplyWelfareProgramBillResult(country, bill, passed, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} bill {(passed ? "passed" : "failed")}", country.State.ApprovalRating - approvalBeforeWelfareBill);
                resolved.Add(bill.Type);
            }

            foreach (WelfareProgramType type in resolved)
            {
                pending.Remove(type);
            }
        }

        /// <summary>The pending standalone Labor Market bill for this country, or null if none is currently before Parliament.</summary>
        public LaborPolicyBill GetPendingLaborBill(CountryId countryId)
        {
            return _pendingLaborBillByCountry.TryGetValue(countryId, out LaborPolicyBill bill) ? bill : null;
        }

        /// <summary>Submits a new standalone LaborPolicyBill - a no-op (returns false) if one is already pending, same single-slot pattern as IntroduceBudgetBill. No mandatory pause to close - this tier never blocks time.</summary>
        public bool IntroduceLaborBill(CountryId countryId, LaborPolicyBill bill)
        {
            System.Func<LaborPolicyBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a labour bill", countryId, sim => copy != null && sim.IntroduceLaborBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.Labor), () => IntroduceLaborBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceLaborBill"/>'s act.</summary>
        private bool IntroduceLaborBillNow(CountryId countryId, LaborPolicyBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Labor, null, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingLaborBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingLaborBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>Counts down the pending LaborPolicyBill (if any) by one day, resolving it once DaysRemaining reaches 0 - same non-blocking idiom as AdvanceBudgetBillDay.</summary>
        public void AdvanceLaborBillDay(CountryId countryId)
        {
            if (!_pendingLaborBillByCountry.TryGetValue(countryId, out LaborPolicyBill bill))
            {
                return;
            }

            if (SkipsForAnswers(bill.Docket)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetLaborBillDirection(country, bill);
            BillConcern concern = ParliamentSystem.GetLaborBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
            DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
            bool passed = true;
            if (bill.Docket == null)
            {
                vetoable = ParliamentSystem.Divide(country, "Labor Market bill", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
            }
            bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
            if (decided == null) { return; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
            passed = decided.Value;
            float approvalBeforeLaborBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyLaborBillResult(country, bill, passed, ApplyLaborBillEffects, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Labor Market bill passed" : "Labor Market bill failed", country.State.ApprovalRating - approvalBeforeLaborBill);
            _pendingLaborBillByCountry.Remove(countryId);
        }

        /// <summary>The Labor Market bill's apply delegate - REWRITTEN for the coexistence ruling
        /// (pass 3, 2026-08-26, "keeps sliders"): a passed bill now sets the six STATUTORY BASE
        /// fields (absolute targets, the same clamp bounds the old direct path applied - the
        /// bill's own book), then recomposes the effective dials through
        /// RecomputeLaborDialsFromEnactedLaws so enacted labor laws' delta offsets (the other
        /// book) stack on top instead of being stomped by the bill - and vice versa. With no labor
        /// laws enacted the recompute writes exactly the clamped bill values, identical to the old
        /// direct path. The -1 "no change" sentinel discipline and the no-statutory-minimum-wage
        /// no-op (Sweden/Italy) both carry over from the appliers this used to call directly.</summary>
        private void ApplyLaborBillEffects(Country country, LaborPolicyBill bill)
        {
            if (country.MinimumWageImplemented && bill.MinimumWage >= 0f)
            {
                country.MinimumWagePercentOfMedianBase = Mathf.Clamp(bill.MinimumWage, MinMinimumWagePercent, MaxMinimumWagePercent);
            }

            if (bill.PaidFamilyLeaveWeeks >= 0f)
            {
                country.PaidFamilyLeaveWeeksBase = Mathf.Clamp(bill.PaidFamilyLeaveWeeks, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks);
            }

            if (bill.OvertimeRegulation >= 0f)
            {
                country.OvertimeRegulationBase = Mathf.Clamp(bill.OvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (bill.RetrainingProgram >= 0f)
            {
                country.RetrainingProgramBase = Mathf.Clamp(bill.RetrainingProgram, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (bill.FamilyPolicy >= 0f)
            {
                country.FamilyPolicyBase = Mathf.Clamp(bill.FamilyPolicy, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (bill.ImmigrationPolicy >= 0f)
            {
                country.ImmigrationPolicyBase = Mathf.Clamp(bill.ImmigrationPolicy, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            RecomputeLaborDialsFromEnactedLaws(country);
        }

        /// <summary>The pending standalone Crime &amp; Justice bill for this country, or null if none is currently before Parliament.</summary>
        public CrimeJusticePolicyBill GetPendingCrimeJusticeBill(CountryId countryId)
        {
            return _pendingCrimeJusticeBillByCountry.TryGetValue(countryId, out CrimeJusticePolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceCrimeJusticeBill(CountryId countryId, CrimeJusticePolicyBill bill)
        {
            System.Func<CrimeJusticePolicyBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a crime and justice bill", countryId, sim => copy != null && sim.IntroduceCrimeJusticeBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.CrimeJustice), () => IntroduceCrimeJusticeBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceCrimeJusticeBill"/>'s act.</summary>
        private bool IntroduceCrimeJusticeBillNow(CountryId countryId, CrimeJusticePolicyBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingCrimeJusticeBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingCrimeJusticeBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceCrimeJusticeBillDay(CountryId countryId)
        {
            if (!_pendingCrimeJusticeBillByCountry.TryGetValue(countryId, out CrimeJusticePolicyBill bill))
            {
                return;
            }

            if (SkipsForAnswers(bill.Docket)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetCrimeJusticeBillDirection(country, bill);
            BillConcern concern = ParliamentSystem.GetCrimeJusticeBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
            DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
            bool passed = true;
            if (bill.Docket == null)
            {
                vetoable = ParliamentSystem.Divide(country, "Crime & Justice bill", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
            }
            bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
            if (decided == null) { return; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
            passed = decided.Value;
            float approvalBeforeCrimeBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyCrimeJusticeBillResult(country, bill, passed, ApplyCrimeJusticeBillEffects, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Crime & Justice bill passed" : "Crime & Justice bill failed", country.State.ApprovalRating - approvalBeforeCrimeBill);
            _pendingCrimeJusticeBillByCountry.Remove(countryId);
        }

        /// <summary>Every LawBill currently pending for this country, keyed by LawId, or an empty collection if none - see _pendingLawBillsByCountry's own doc comment.</summary>
        public IReadOnlyDictionary<string, LawBill> GetPendingLawBills(CountryId countryId)
        {
            return _pendingLawBillsByCountry.TryGetValue(countryId, out var pending) ? pending : EmptyLawBills;
        }

        private static readonly Dictionary<string, LawBill> EmptyLawBills = new Dictionary<string, LawBill>();

        /// <summary>The pending bill for one specific law, or null if none is currently before Parliament for it.</summary>
        public LawBill GetPendingLawBill(CountryId countryId, string lawId)
        {
            return _pendingLawBillsByCountry.TryGetValue(countryId, out var pending) && pending.TryGetValue(lawId, out LawBill bill) ? bill : null;
        }

        /// <summary>Submits a new law bill (enact or repeal, per bill.IsRepeal) - a no-op (returns false) if one is already pending for this SAME LawId. Mirrors IntroduceTaxProgramBill's own pattern exactly.</summary>
        public bool IntroduceLawBill(CountryId countryId, LawBill bill)
        {
            System.Func<LawBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a law bill", countryId, sim => copy != null && sim.IntroduceLawBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.Law), () => IntroduceLawBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceLawBill"/>'s act.</summary>
        private bool IntroduceLawBillNow(CountryId countryId, LawBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Law, PortfolioOfLaw(bill), out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            // P4-C3 third category, ruling (a) (2026-09-05): a law outside the parliament's competence (a monetary-regime law in a
            // country that shares its currency zone; EN-7b: an electricity-tax law where no statute is levied - the USA) is refused here as well as not offered - the browser's line and this gate agree.
            if (!LawCatalog.IsWithinCompetence(_world, _world?.GetCountry(countryId), LawCatalog.GetById(bill.LawId)))
            {
                return false;
            }

            if (!_pendingLawBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<string, LawBill>();
                _pendingLawBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(bill.LawId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            pending[bill.LawId] = bill;
            return true;
        }

        /// <summary>Counts down every pending law bill for this country by one day, resolving any that reach 0 - mirrors AdvanceTaxProgramBillsDay's own pattern exactly.</summary>
        public void AdvanceLawBillsDay(CountryId countryId)
        {
            if (!_pendingLawBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<string>();
            foreach (LawBill bill in pending.Values)
            {
                if (SkipsForAnswers(bill.Docket)) { continue; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
                if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                LawDefinition law = LawCatalog.GetById(bill.LawId);
                string lawName = law != null ? law.Name : bill.LawId;
                float direction = ParliamentSystem.GetLawBillDirection(country, bill);
                BillConcern concern = ParliamentSystem.GetLawBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
                DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
                bool passed = true;
                if (bill.Docket == null)
                {
                    vetoable = ParliamentSystem.Divide(country, $"{(bill.IsRepeal ? "Repeal" : "Enact")}: {lawName}", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                    passed = PresidentialVetoGate(country, vetoable, passed, law != null && law.ConstitutionalAmendment ? Elections.PresidentialVeto.Act.ConstitutionalAmendment : Elections.PresidentialVeto.Act.OrdinaryStatute);   // §761 (PS-5): a statute goes to the President - Poland's veto and its 3/5 override; an amendment never vetoed (Art. 235 ust. 7)
                }
                bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket, law != null && law.ConstitutionalAmendment);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override; an amendment is not presented
                if (decided == null) { continue; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
                passed = decided.Value;
                // Found by the fiscal-ledger pass's own bar (2026-08-25), pre-existing since the MVP
                // slice: this was the ONE bill type resolving OUTSIDE the approval ledger's
                // observation sites - a failed vote's BillFailedApprovalCost and a passed law's
                // EnactmentApprovalCost both moved approval with no ledger entry, so the round-trip
                // harness's two coverage law bills (both failing in a no-op world, 2 x 1.5 = the
                // exact -3.0 the audit reported) had been failing the approval self-audit in every
                // scenario since 08-24 - unnoticed, because that harness's own verdict line does not
                // grep ATTRIB. Observed here exactly as the other eight bill types are.
                float approvalBeforeLawResult = country.State.ApprovalRating;
                bool lawBorne = Congress.GovernmentBears(country, concern);   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears - the failed vote's cost, and the enacted law's
                ParliamentSystem.ApplyLawBillResult(country, bill, passed, lawBorne ? ApplyLawBillEffects : (System.Action<Country, LawBill>)ApplyLawBillEffectsUnborne, lawBorne);
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate,
                    $"{(bill.IsRepeal ? "Repeal" : "Enact")}: {lawName} ({(passed ? "passed" : "failed")})",
                    country.State.ApprovalRating - approvalBeforeLawResult);
                resolved.Add(bill.LawId);
            }

            foreach (string lawId in resolved)
            {
                pending.Remove(lawId);
            }
        }

        /// <summary>
        /// Applies an enacted (or repealed) law's dial deltas by building a throwaway
        /// PolicyDecision and reusing the EXISTING ApplyCrimePolicyChanges/
        /// ApplyCrimeJusticeDeeperChanges - the same "reuse the plumbing" pattern
        /// ApplyCrimeJusticeBillEffects/ApplyLaborBillEffects already establish, applied to a law's
        /// DELTA instead of a slider's absolute submitted value. Each target is pre-clamped to
        /// [MinPolicyDialLevel, MaxPolicyDialLevel] HERE, before being written into the
        /// PolicyDecision - not left to ApplyCrimePolicyChanges' own clamp - because a repeal
        /// subtracting a delta from a low current value can compute a raw negative number, and
        /// PolicyDecision's own "-1 sentinel means no change" convention would silently swallow it
        /// as a no-op if it were ever negative before this clamp ran.
        ///
        /// Enact appends an EnactedLaw record (Country.EnactedLaws) and charges the law's own
        /// EnactmentApprovalCost once, on passage. Repeal removes the record and applies the SAME
        /// six deltas with the opposite sign - laws apply as NUDGES (deltas), not absolute
        /// overrides, specifically so this composes: enacting two laws that touch the same dial
        /// stacks both effects, and repealing either one subtracts back out only its own
        /// contribution, decomposable to zero net effect when nothing else has touched the dial in
        /// between (clamp saturation is the one honest exception - the same caveat every clamped
        /// dial in this codebase already carries).
        /// </summary>
        private void ApplyLawBillEffects(Country country, LawBill bill) => EnactOrRepealLaw(country, bill, chargeEnactment: true);

        /// <summary>PS-6 US-19 (the review's M2, DECLARED): a law a congressional majority out of the government carries (<see cref="Congress.GovernmentBears"/>
        /// false) - enacted or repealed exactly as <see cref="ApplyLawBillEffects"/> does it, without charging the government its enactment cost.</summary>
        private void ApplyLawBillEffectsUnborne(Country country, LawBill bill) => EnactOrRepealLaw(country, bill, chargeEnactment: false);

        /// <summary>The body of <see cref="ApplyLawBillEffects"/> (its doc above), the enactment cost charged only where <paramref name="chargeEnactment"/>.</summary>
        private void EnactOrRepealLaw(Country country, LawBill bill, bool chargeEnactment)
        {
            LawDefinition law = LawCatalog.GetById(bill.LawId);
            if (law == null)
            {
                return;
            }

            // Defensive idempotency guard, not reachable through the real UI today (DrawLawCard only
            // ever offers "Enact" while !enacted and "Repeal" while enacted, and IntroduceLawBill
            // already refuses a second bill for the same LawId while one is pending) - but cheap, and
            // it closes a real latent double-application class: without it, enacting an
            // already-enacted law would add a duplicate EnactedLaws entry; repealing a law that isn't
            // enacted would have nothing to remove. Both are now unconditional no-ops instead - the
            // same "idempotent, not merely rejected" spirit IntroduceLawBill already applies one
            // layer up.
            bool alreadyEnacted = country.EnactedLaws.Exists(e => e.LawId == bill.LawId);
            if (bill.IsRepeal == !alreadyEnacted)
            {
                return;
            }

            if (bill.IsRepeal)
            {
                country.EnactedLaws.RemoveAll(e => e.LawId == bill.LawId);
            }
            else
            {
                country.EnactedLaws.Add(new EnactedLaw { LawId = bill.LawId, EnactedOn = CurrentDate });
                if (chargeEnactment) { TaxTerm.Shock(country, -law.EnactmentApprovalCost); }   // PS-6 US-19 (the review's M2): the government pays for its own law
            }
            TrackAgreements(country);   // PS-3h (§635): a law enacted or repealed is a delivery or a break the day it applies

            // Both categories' recomputes run unconditionally (pass 3): each is idempotent and a
            // law's foreign-category deltas are 0f defaults, so the wrong-category recompute is an
            // exact no-op - cheaper to prove than a category dispatch that could silently skip a
            // mixed future law.
            RecomputeCrimeJusticeDialsFromEnactedLaws(country);
            RecomputeLaborDialsFromEnactedLaws(country);
            RecomputeStructuralParametersFromEnactedLaws(country);   // P4-C3
        }

        /// <summary>
        /// P4-C3 (2026-09-04): every structural parameter (StructuralParameters.All) recomputed FRESH from its seeded base and the
        /// enacted structural laws - the same clamp-safe idiom as the two dial recomputes (never incremental, so a
        /// repeal in any order lands on the base exactly; LawCompositionDiagnostic asserts it). A law of any other
        /// category carries a 0 delta, so the loop is an exact no-op for them.
        /// </summary>
        private void RecomputeStructuralParametersFromEnactedLaws(Country country)
        {
            foreach (StructuralParameters.Spec spec in StructuralParameters.All)
            {
                float value = spec.GetBase(country);
                foreach (EnactedLaw enacted in country.EnactedLaws)
                {
                    LawDefinition law = LawCatalog.GetById(enacted.LawId);
                    if (law == null) { continue; }
                    foreach (StructuralDelta d in law.Structural) { if (d.Parameter == spec.Parameter) { value += d.Delta; } }
                }
                spec.Set(country, Mathf.Clamp(value, spec.Min, spec.Max));
            }
        }

        /// <summary>
        /// Content-marathon finding (2026-08-25): the dial each of the six Crime &amp; Justice fields
        /// shows is recomputed FRESH from the current Country.EnactedLaws set every time it changes -
        /// never mutated incrementally by adding one law's signed delta to whatever the dial
        /// currently reads. The difference matters specifically at the clamp boundary.
        ///
        /// The FIRST version of this method did the incremental thing (this file's own earlier
        /// doc comments called clamp saturation "the one honest exception" to decomposability, which
        /// was the right instinct but understated the actual failure): with enough laws pushing
        /// SentencingSeverity past 100, the clamp silently absorbs the overshoot, and each individual
        /// law's OWN delta gets baked against whatever the CLAMPED prior value happened to be rather
        /// than the true unclamped total - repealing the same laws in ANY order then subtracts each
        /// nominal delta from a value that already "spent" some of that delta's effect on the clamp,
        /// landing measurably below the pre-enactment baseline rather than back on it. A real,
        /// realistic-scale composition test (ten laws, several of them touching SentencingSeverity at
        /// MAJOR/SWEEPING magnitude - not a contrived case; five or six of this category's now-twenty
        /// Sentencing-touching laws reach the ceiling on their own) measured this directly: full
        /// repeal of a set that had driven SentencingSeverity to its 100 ceiling landed at 29.0000, not
        /// 50.0000 - the ceiling had silently eaten 21 points nothing gave back.
        ///
        /// The fix treats Country.EnactedLaws as the sole source of truth and every dial as a PURE,
        /// STATELESS function of it: sum every enacted law's delta on a dial (from the seeded 50
        /// baseline - the same "laws are now the sole driver of these six dials" ruling the sliders'
        /// read-only conversion already established, restated as a computation rather than a policy),
        /// clamp exactly ONCE at the end, and set that as the dial. Enact and repeal both just change
        /// which laws are in the set, then call this - correct for ANY history of enactments and
        /// repeals in ANY order, not merely the one sequence a hand-written test happened to check,
        /// because there is no accumulated clamped state left to disagree with a fresh recomputation.
        /// An EnactedLaws entry citing a law no longer in LawCatalog (a hypothetical stale save) is
        /// skipped rather than crashing - the same "missing entry, not a crash" idiom LawCatalog.GetById
        /// itself already documents.
        /// </summary>
        private void RecomputeCrimeJusticeDialsFromEnactedLaws(Country country)
        {
            // CONVENTION - a dial midpoint, and not this file's to state.
            // S-26 (2026-09-01): was the fourth statement of 50f; now the one statement, referenced.
            // ⚠ The <remarks> doc comment that used to sit here was stranded INSIDE the method body,
            // after the opening brace, where it documented nothing at all. Removed with the restatement
            // it explained - a comment whose only subject has gone is the corpse' s alibi (S-36).
            const float baseline = CrimeJusticeCouplings.NeutralDialLevel;
            float police = baseline, sentencing = baseline, bail = baseline;
            float drug = baseline, judicial = baseline, border = baseline;

            foreach (EnactedLaw enacted in country.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null)
                {
                    continue;
                }

                police += law.PoliceFundingDelta;
                sentencing += law.SentencingSeverityDelta;
                bail += law.BailReformDelta;
                drug += law.DrugPolicyDelta;
                judicial += law.JudicialFundingDelta;
                border += law.BorderEnforcementDelta;
            }

            var recomputed = new PolicyDecision
            {
                PoliceFundingOverride = Mathf.Clamp(police, MinPolicyDialLevel, MaxPolicyDialLevel),
                SentencingSeverityOverride = Mathf.Clamp(sentencing, MinPolicyDialLevel, MaxPolicyDialLevel),
                BailReformOverride = Mathf.Clamp(bail, MinPolicyDialLevel, MaxPolicyDialLevel),
                DrugPolicyOverride = Mathf.Clamp(drug, MinPolicyDialLevel, MaxPolicyDialLevel),
                JudicialFundingOverride = Mathf.Clamp(judicial, MinPolicyDialLevel, MaxPolicyDialLevel),
                BorderEnforcementOverride = Mathf.Clamp(border, MinPolicyDialLevel, MaxPolicyDialLevel)
            };
            ApplyCrimePolicyChanges(country, recomputed);
            ApplyCrimeJusticeDeeperChanges(country, recomputed);
        }

        /// <summary>
        /// THE LABOR RECOMPUTE (pass 3, coexistence ruling 2026-08-26): the labor sibling of
        /// RecomputeCrimeJusticeDialsFromEnactedLaws, generalized in exactly two ways (the
        /// pass-3 generalization verdict records both): (1) PER-COUNTRY BASELINES - each
        /// accumulator starts at the country's own bill-owned STATUTORY BASE field
        /// (Country.*Base: Kaitz points, weeks, or dial points), not the uniform 50, because two
        /// labor dials are real-unit dials with per-country seeds and, under coexistence, the
        /// base itself is player-legislated; (2) TWO WRITERS, ONE COMPOSITION - LaborPolicyBill
        /// sets base (ApplyLaborBillEffects), enacted laws contribute a pure delta sum on top,
        /// and this method is the ONLY writer of the effective dials, clamping ONCE at
        /// composition (the 555f4cc lesson restated: no clamped state ever persists into either
        /// component, so any history of bills, enactments and repeals in any order lands exactly
        /// where a fresh recomputation says - and full repeal lands exactly on base). Funnels
        /// through the existing clamp-owning appliers via a throwaway PolicyDecision;
        /// ApplyMinimumWageChange's own !MinimumWageImplemented no-op keeps Sweden/Italy's
        /// minimum-wage law deltas honestly inert. A stale EnactedLaws entry is skipped (the
        /// GetById null contract).
        /// </summary>
        private void RecomputeLaborDialsFromEnactedLaws(Country country)
        {
            float minimumWage = country.MinimumWagePercentOfMedianBase;
            float paidLeave = country.PaidFamilyLeaveWeeksBase;
            float overtime = country.OvertimeRegulationBase;
            float retraining = country.RetrainingProgramBase;
            float family = country.FamilyPolicyBase;
            float immigration = country.ImmigrationPolicyBase;

            foreach (EnactedLaw enacted in country.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null)
                {
                    continue;
                }

                minimumWage += law.MinimumWageDelta;
                paidLeave += law.PaidFamilyLeaveWeeksDelta;
                overtime += law.OvertimeRegulationDelta;
                retraining += law.RetrainingProgramDelta;
                family += law.FamilyPolicyDelta;
                immigration += law.ImmigrationPolicyDelta;
            }

            var recomputed = new PolicyDecision
            {
                MinimumWageOverride = Mathf.Clamp(minimumWage, MinMinimumWagePercent, MaxMinimumWagePercent),
                PaidFamilyLeaveWeeksOverride = Mathf.Clamp(paidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks),
                OvertimeRegulationOverride = Mathf.Clamp(overtime, MinLaborDialLevel, MaxLaborDialLevel),
                RetrainingProgramOverride = Mathf.Clamp(retraining, MinLaborDialLevel, MaxLaborDialLevel),
                FamilyPolicyOverride = Mathf.Clamp(family, MinPolicyDialLevel, MaxPolicyDialLevel),
                ImmigrationPolicyOverride = Mathf.Clamp(immigration, MinPolicyDialLevel, MaxPolicyDialLevel)
            };
            ApplyMinimumWageChange(country, recomputed);
            ApplyLaborPolicyChanges(country, recomputed);
            ApplyDemographicPolicyChanges(country, recomputed);
        }

        /// <summary>See ApplyLaborBillEffects' own doc comment - identical pattern, reuses ApplyCrimePolicyChanges/ApplyCrimeJusticeDeeperChanges.</summary>
        private void ApplyCrimeJusticeBillEffects(Country country, CrimeJusticePolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                PoliceFundingOverride = bill.PoliceFunding,
                SentencingSeverityOverride = bill.SentencingSeverity,
                BailReformOverride = bill.BailReform,
                DrugPolicyOverride = bill.DrugPolicy,
                JudicialFundingOverride = bill.JudicialFunding,
                BorderEnforcementOverride = bill.BorderEnforcement
            };
            ApplyCrimePolicyChanges(country, decision);
            ApplyCrimeJusticeDeeperChanges(country, decision);
        }

        /// <summary>The pending standalone Economic Sectors bill for this country, or null if none is currently before Parliament.</summary>
        public SectorPolicyBill GetPendingSectorBill(CountryId countryId)
        {
            return _pendingSectorBillByCountry.TryGetValue(countryId, out SectorPolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceSectorBill(CountryId countryId, SectorPolicyBill bill)
        {
            System.Func<SectorPolicyBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a sector bill", countryId, sim => copy != null && sim.IntroduceSectorBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.Sector), () => IntroduceSectorBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceSectorBill"/>'s act.</summary>
        private bool IntroduceSectorBillNow(CountryId countryId, SectorPolicyBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Sector, null, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingSectorBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingSectorBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceSectorBillDay(CountryId countryId)
        {
            if (!_pendingSectorBillByCountry.TryGetValue(countryId, out SectorPolicyBill bill))
            {
                return;
            }

            if (SkipsForAnswers(bill.Docket)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetSectorBillDirection(country, bill);
            BillConcern concern = ParliamentSystem.GetSectorBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
            DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
            bool passed = true;
            if (bill.Docket == null)
            {
                vetoable = ParliamentSystem.Divide(country, "Economic Sectors bill", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
            }
            bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
            if (decided == null) { return; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
            passed = decided.Value;
            float approvalBeforeSectorBill = country.State.ApprovalRating;
            ParliamentSystem.ApplySectorBillResult(country, bill, passed, ApplySectorBillEffects, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Economic Sectors bill passed" : "Economic Sectors bill failed", country.State.ApprovalRating - approvalBeforeSectorBill);
            _pendingSectorBillByCountry.Remove(countryId);
        }

        /// <summary>See ApplyLaborBillEffects' own doc comment - identical pattern, reuses ApplySectorPolicyChanges.</summary>
        private void ApplySectorBillEffects(Country country, SectorPolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                SectorSubsidyOverrides = bill.SubsidyLevels,
                SectorRegulationOverrides = bill.RegulationLevels,
                SectorTaxCreditOverrides = bill.TaxCreditLevels,
                SectorResearchGrantsOverrides = bill.ResearchGrantsLevels,
                SectorDeregulationNationalizationOverrides = bill.DeregulationLevels
            };
            ApplySectorPolicyChanges(country, decision);
        }

        /// <summary>The pending SWF emergency drawdown bill for this country, or null if none is before Parliament.</summary>
        public SwfDrawdownBill GetPendingSwfDrawdownBill(CountryId countryId)
        {
            return _pendingSwfDrawdownBillByCountry.TryGetValue(countryId, out SwfDrawdownBill bill) ? bill : null;
        }

        /// <summary>
        /// See IntroduceLaborBill's own doc comment - identical pattern, with one extra guard: a country
        /// with no fund has nothing to draw down, so the bill cannot be introduced rather than passing
        /// and quietly doing nothing. A vote that resolves to no effect is worse than an unavailable
        /// control, because the player spends the wait learning nothing.
        /// </summary>
        public bool IntroduceSwfDrawdownBill(CountryId countryId, SwfDrawdownBill bill)
        {
            System.Func<SwfDrawdownBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a fund drawdown bill", countryId, sim => copy != null && sim.IntroduceSwfDrawdownBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.SwfDrawdown), () => IntroduceSwfDrawdownBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceSwfDrawdownBill"/>'s act.</summary>
        private bool IntroduceSwfDrawdownBillNow(CountryId countryId, SwfDrawdownBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.SwfDrawdown, null, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingSwfDrawdownBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            Country guardCountry = _world.GetCountry(countryId);
            if (guardCountry == null || guardCountry.SovereignWealthFund == null)
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingSwfDrawdownBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceSwfDrawdownBillDay(CountryId countryId)
        {
            if (!_pendingSwfDrawdownBillByCountry.TryGetValue(countryId, out SwfDrawdownBill bill))
            {
                return;
            }

            if (SkipsForAnswers(bill.Docket)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetSwfDrawdownBillDirection(country, bill);
            BillConcern concern = ParliamentSystem.GetSwfDrawdownBillConcern(country, bill);   // P3-A2: the chamber votes on what the bill concerns
            DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
            bool passed = true;
            if (bill.Docket == null)
            {
                vetoable = ParliamentSystem.Divide(country, $"SWF emergency drawdown - {bill.WithdrawalPercentOfGdp:F1}% of GDP", concern, CurrentDate, out passed);   // PS-6 US-18: through both chambers where the country has two (Congress)
                passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
            }
            bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
            if (decided == null) { return; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
            passed = decided.Value;
            float approvalBeforeSwfBill = country.State.ApprovalRating;
            // Pass 5 (2026-08-26): the drawdown is F1's THIRD writer, found by the retirement sweep -
            // it now reaches the stock through ApplyOneTimeBudgetImpact, so the debt ledger observes
            // it here exactly as the cabinet and foreign-policy sites observe theirs.
            float debtBeforeSwfBill = country.State.GovernmentDebt;
            ParliamentSystem.ApplySwfDrawdownBillResult(country, bill, passed, ApplySwfDrawdownBillEffects, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "SWF drawdown bill passed" : "SWF drawdown bill failed", country.State.ApprovalRating - approvalBeforeSwfBill);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, "SWF emergency drawdown", debtBeforeSwfBill, country.State.GovernmentDebt);
            _pendingSwfDrawdownBillByCountry.Remove(countryId);
        }

        /// <summary>
        /// The drawdown's apply delegate: a ONE-OFF transfer out of the fund into the budget, unlike
        /// every other SWF term, which sets a standing rate.
        ///
        /// **Clamped to what the fund actually holds.** A withdrawal larger than the fund cannot be
        /// honoured, and letting `TotalAssets` go negative would invent money and hand the budget a
        /// credit that never existed - the same class of error as the negative-debt interest inversion
        /// caught when the debt floor came off. The bill is not rejected for asking too much; it delivers
        /// what is there, which is what an emergency drawdown of a depleted fund really produces.
        ///
        /// ⚠ CORRECTED, pass 5 (2026-08-26). This doc used to say the proceeds "land on Budget ... so
        /// the drawdown reaches debt the same way a surplus does" - and that was FALSE from the day it
        /// was written: nothing fiscal reads State.Budget (it is the cumulative display accumulator), so
        /// a passed drawdown destroyed fund assets and lowered no debt. The two-books defect F1 closed
        /// for interrupt impacts had a third writer, found by pass 5's retirement sweep. It is a one-time
        /// settlement, so it takes F1's own path: ApplyOneTimeBudgetImpact moves the stock and records
        /// the same entry in the accumulator, and the resolution site observes it on the debt ledger.
        /// </summary>
        private void ApplySwfDrawdownBillEffects(Country country, SwfDrawdownBill bill)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            if (fund == null)
            {
                return;
            }

            float requested = country.State.NominalGdp * bill.WithdrawalPercentOfGdp / 100f;
            float withdrawn = Mathf.Clamp(requested, 0f, fund.TotalAssets);

            float heldBeforeDrawdown = fund.TotalAssets;
            fund.TotalAssets -= withdrawn;
            // AF-35 (the third review's P3-1): a drawdown takes the period's earned returns out in proportion with everything else it takes - the period's
            // accrued returns scaled by the share of the holdings kept, so the principal (TotalAssets - AccruedSwfReturns) keeps its ratio to the holdings.
            // Taken out of the principal whole instead, a loss year's losses stayed inside a principal far above what the fund held, and a small fund's later
            // contributions went into losses at the floor. The live country's period only, as the dissolution's zeroing (N3).
            if (withdrawn > 0f && heldBeforeDrawdown > 0f && ReferenceEquals(_world?.GetCountry(country.Id), country) && _fiscalPeriods.TryGetValue(country.Id, out FiscalPeriod drawnPeriod))
            {
                drawnPeriod.AccruedSwfReturns *= fund.TotalAssets / heldBeforeDrawdown;
            }
            ApplyOneTimeBudgetImpact(country, withdrawn);
        }

        /// <summary>The pending standalone Trade bill for this country, or null if none is currently before Parliament.</summary>
        public TradePolicyBill GetPendingTradeBill(CountryId countryId)
        {
            return _pendingTradeBillByCountry.TryGetValue(countryId, out TradePolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceTradeBill(CountryId countryId, TradePolicyBill bill)
        {
            System.Func<TradePolicyBill> copy = CopiesForLedger(bill);   // FORK-2: the bill as introduced, a fresh copy for each except-world
            return Answered(new PlayerAnswer("a trade bill", countryId, sim => copy != null && sim.IntroduceTradeBill(countryId, copy()), answersAWait: false, bill: Congress.Kind.Trade), () => IntroduceTradeBillNow(countryId, bill));
        }

        /// <summary>FORK-2: <see cref="IntroduceTradeBill"/>'s act.</summary>
        private bool IntroduceTradeBillNow(CountryId countryId, TradePolicyBill bill)
        {
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Trade, CabinetPortfolio.ForeignAffairs, out string lockedBecause)) { Debug.Log($"LEVERS: {countryId} - a bill was refused: {lockedBecause}"); return false; }   // PS-3c (§630): the role gate; PS-6 US-19: by the bill's kind (R-US7 (a))
            if (_pendingTradeBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingTradeBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceTradeBillDay(CountryId countryId)
        {
            if (!_pendingTradeBillByCountry.TryGetValue(countryId, out TradePolicyBill bill))
            {
                return;
            }

            if (SkipsForAnswers(bill.Docket)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only a bill with an answer on its docket moves
            if (bill.Docket == null) { bill.DaysRemaining--; }   // PS-6 US-21b: a bill held on the president's docket was voted already - its days are the docket's
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetTradeBillDirection(country, bill, _world);
            // C-B3 / R-CL2: the tariff bill is scored on the OPENNESS axis (CHES eu_position), not the
            // fiscal one. The USA has no EU position on any seat and falls back to fiscal inside
            // GetSeatWeightedAlignment, with ParliamentSystem.TradeAxisAvailable saying so.
            BillConcern concern = ParliamentSystem.GetTradeBillConcern(country, bill, _world);   // P3-A2: the chamber votes on what the bill concerns
            DivisionRecord vetoable = bill.Docket?.Passage;   // PS-6 US-21b: held on the president's docket, the bill was voted on an earlier day - this division
            bool passed = true;
            if (bill.Docket == null)
            {
                vetoable = ParliamentSystem.Divide(country, "Trade bill", concern, CurrentDate, out passed, BillAxis.Trade);   // PS-6 US-18: through both chambers where the country has two (Congress)
                passed = PresidentialVetoGate(country, vetoable, passed);   // §761 (PS-5): an ordinary statute goes to the President - Poland's veto and its 3/5 override
            }
            bool? decided = UsVetoGate(country, vetoable, concern, passed, ref bill.Docket);   // PS-6 US-21: a US bill both chambers sent goes to the president of the day - rule (a), or a player-president's answer - then the override
            if (decided == null) { return; }   // PS-6 US-21b: on the player-president's docket - decided the day he answers, or law when the period runs out unanswered
            passed = decided.Value;
            float approvalBeforeTradeBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyTradeBillResult(country, bill, passed, ApplyTradeBillEffects, Congress.GovernmentBears(country, concern));   // PS-6 US-19 (the review's M2): the government pays only for a bill it bears
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Trade bill passed" : "Trade bill failed", country.State.ApprovalRating - approvalBeforeTradeBill);
            _pendingTradeBillByCountry.Remove(countryId);
        }

        /// <summary>
        /// The Trade bill's apply delegate. NewBaseTariffRate is an ABSOLUTE target (see
        /// TradePolicyBill's own doc comment), but the existing ApplyTariffRateChange only understands
        /// a DELTA (PolicyDecision.TariffRateChange) - converted here as target-minus-current
        /// immediately before applying, so ApplyTariffRateChange's own clamp
        /// (Clamp(current + delta, min, max) = Clamp(target, min, max)) lands on exactly the bill's
        /// requested target without duplicating its clamp bounds here. PartnerTariffOverrides is
        /// already absolute (matches PolicyDecision.PartnerTariffOverrides' own semantics exactly), so
        /// it's passed through unchanged.
        /// </summary>
        private void ApplyTradeBillEffects(Country country, TradePolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                TariffRateChange = bill.NewBaseTariffRate - country.BaseTariffRate,
                PartnerTariffOverrides = bill.PartnerTariffOverrides
            };
            ApplyTariffRateChange(country, decision);
            ApplyPartnerTariffOverrides(country, decision);
        }

        /// <summary>
        /// True on the one day per year that matches <paramref name="countryId"/>'s own
        /// FiscalYearData start date (month/day only, so this fires exactly once per year, the same
        /// "compare against a specific calendar date" idiom AdvanceDay's own turn-boundary check
        /// uses). Pure query - unlike AdvanceDay, has no side effects and isn't itself responsible for
        /// opening anything; see TryOpenBudgetProcess for the caller that acts on it.
        /// </summary>
        public bool IsFiscalYearStart(CountryId countryId, System.DateTime date)
        {
            (int month, int day) = FiscalYearData.GetFiscalYearStart(countryId);
            return date.Month == month && date.Day == day;
        }

        /// <summary>PS-6 US-22: true on the day the country's annual budget process opens (`FiscalYearData.BudgetRequestDay`) - its fiscal-year start
        /// everywhere but the USA, where the president's request is due on 31 U.S.C. 1105(a)'s first Monday in February. Pure query, as above.</summary>
        public bool IsBudgetRequestDay(CountryId countryId, System.DateTime date) => date.Date == FiscalYearData.BudgetRequestDay(countryId, date.Year);

        /// <summary>True while countryId's annual budget process is open and not yet resolved - see _pendingBudgetProcessByCountry's own doc comment and GameController's pause-gate/banner use of this.</summary>
        public bool GetPendingBudgetProcess(CountryId countryId)
        {
            return _pendingBudgetProcessByCountry.Contains(countryId);
        }

        /// <summary>
        /// Master Sequence step 5a: opens countryId's annual budget process on its own fiscal-year
        /// start date - a no-op if today isn't that date, or if one's already open (single-slot,
        /// mirroring TryRollForeignPolicyMeeting's own "safe to call every day unconditionally"
        /// idiom). Called once per simulated day from GameController.Update's day-processing loop, for
        /// PlayerCountryId only (see _pendingBudgetProcessByCountry's own doc comment on why the other
        /// five countries never enter this set at this phase).
        /// </summary>
        public void TryOpenBudgetProcess(CountryId countryId, System.DateTime date)
        {
            if (_pendingBudgetProcessByCountry.Contains(countryId))
            {
                return;
            }
            if (IsBudgetRequestDay(countryId, date) && _pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill held) && held.ContinuingResolution && UsBudgetProcedure.Applies(_world?.GetCountry(countryId)))
            {
                // PS-6 US-22b (Elias's ruling I13 (3), the review's L2): "the request is tabled on its day whatever is before Congress" - a continuing resolution
                // in the slot is withdrawn so the request opens; a lapse's cadence tables the next after the request's acts (ResolveUsBudgetDay). The review's L6, CLOSED
                // by M2: where the withdrawal leaves a lapse with nothing before Congress and nothing withheld (a player-president's window open, no budget tabled - the
                // withhold lapsed with the presidency, AdvanceUsFundingDay), the next funding tick decides an uncontested resolution and the lapse ends; where the
                // player's party still withholds, the lapse is his. The withdrawal itself charges nothing and shows nothing on screen (DECLARED: a log line only).
                _pendingBudgetBillByCountry.Remove(countryId);
                Debug.Log($"BUDGET: {countryId} - the president's request is due on {date:yyyy-MM-dd}: the continuing resolution before Congress is withdrawn for it");
            }
            if (_pendingBudgetBillByCountry.ContainsKey(countryId)) { return; }   // PS-3e (§632, the reader): no window opens beside a pending bill - a window whose bill cannot be introduced would hold the clock the bill needs to resolve
            // PS-3a (§628): the budget is the government's to lay - a player whose party does not lead it lays none (the AI ministry governs the book).
            // PS-3e (§632, ruled): where the AI governs THE PLAYER'S country, the AI government lays it - as a BILL the chamber votes on, on the same
            // calendar as the player's own (the arrival window once, then every fiscal-year date), with the player's party free to table an alternative
            // while it is before the chamber. PLAYER PATH ONLY: a country that is not the player's never comes here (the ministry writes its book direct).
            Country budgeting = _world?.GetCountry(countryId);
            if (budgeting != null && !PlayerGoverns(budgeting))
            {
                if (!PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || _pendingBudgetBillByCountry.ContainsKey(countryId)) { return; }
                bool arrival = !_incomingBudgetWindowUsed.Contains(countryId);
                if (!arrival && !IsBudgetRequestDay(countryId, date)) { return; }   // PS-6 US-22: the request day (every country's fiscal-year start but the USA's)
                if (arrival) { _incomingBudgetWindowUsed.Add(countryId); }
                TableGovernmentBudget(budgeting);
                return;
            }

            // C-C2 (Playtest-1 finding 4): the INCOMING GOVERNMENT'S window. A government that has
            // just taken office lays a budget on arrival rather than waiting for the calendar - which
            // is real practice rather than a convenience: an incoming Swedish government presents an
            // amending budget (ändringsbudget) instead of governing a year on its predecessor's.
            //
            // ⚠ IT ALSO FIXES AN OFF-BY-ONE THAT COST FIVE COUNTRIES A WHOLE YEAR, measured before
            // this line was written (BudgetWindowDiagnostic): the epoch was then 1 January and five of six
            // countries budget on the calendar year, but the day tick runs AFTER AdvanceDay has moved
            // the date to the 2nd - so 1 January 2026 was never seen by this check and the window did
            // not open until 1 JANUARY 2027, 365 ticks in. The USA, whose year starts 1 October,
            // waited 273. The finding called this "waiting for the calendar's next cycle"; it was
            // worse than that, and the measurement is in the record. K-1 (§604) moved the epoch to 1 October
            // 2026: every arrival window opens on day one (2026-10-02), and the five calendar-year budgets
            // open their fiscal-year window again on 1 January 2027, tick 92 - measured, stated, not changed.
            //
            // ⚠ ONE WINDOW ONLY, and the flag is what makes it once: a government gets its arrival
            // budget, not a permanently open process. It rides the save (see CaptureSaveState), or a
            // load would hand a mid-term government a fresh arrival window every time.
            if (!_incomingBudgetWindowUsed.Contains(countryId))
            {
                _incomingBudgetWindowUsed.Add(countryId);
                _incomingBudgetWindowOpenNow.Add(countryId);
                _pendingBudgetProcessByCountry.Add(countryId);
                return;
            }

            if (IsBudgetRequestDay(countryId, date))   // PS-6 US-22: the request day (every country's fiscal-year start but the USA's)
            {
                _pendingBudgetProcessByCountry.Add(countryId);
            }
        }

        /// <summary>Lets tools/tests (e.g. SimulationTestRunner) inject a specific World instead of the Awake-created default.</summary>
        public void SetWorld(World world)
        {
            _world = world;
            SeedPublishedHistory();
            CommitCalendarYear();   // PN-1's driver (§520): the countries read the clock's year from the first day
        }

        /// <summary>PN-1's driver (2026-09-16, §520): the clock's calendar year committed to every country (Country.CalendarYear), so a reader without a
        /// clock - the pension line's driver, which counts the cohorts at or above the age the statute has in force THIS year - reads the year the turn is
        /// in. Called where the clock is set or moved: SetWorld, RestoreSaveState, AdvanceDay. An int, written here and copied onto the preview clone. ⚠ Since §596 the daily arithmetic
        /// DOES read it (the participation response's age in force, `PensionParticipationResponse`), and K-1's epoch move reaches the economy through it
        /// alone (§604's attribution run). With a 1 October start the year turns on day 92 of turn 0 (1 January 2027), a day later about every four
        /// turns - 93 from turn 2, 116 by turn 100 - the 365-day turn against the calendar.</summary>
        private void CommitCalendarYear()
        {
            if (_world == null) { return; }
            int year = CurrentDate.Year;
            foreach (Country c in _world.Countries) { c.CalendarYear = year; c.CalendarDay = CurrentDate.Date; }   // K80: the day too, for the dated sides
            if (!IsCounterfactual) { PlayedDay = CurrentDate.Date; }   // K80: the played world's day, for a reader that has a country's id and no country (the seat map)
        }

        /// <summary>
        /// SAVE/LOAD (item 8): the explicit capture of every private pending structure - the
        /// reviewable surface the 2026-08-01 scoping asked for, so a future pending-bill type nobody
        /// wired in reads as an obvious omission in this method rather than silently half-persisting.
        /// Containers are copied one level deep (fresh dictionaries/lists holding the same bill
        /// instances) so the returned state object cannot alias this manager's live collections;
        /// the bills themselves are snapshotted by the serializer immediately after.
        /// </summary>
        public Persistence.SimulationPendingState CaptureSaveState()
        {
            var state = new Persistence.SimulationPendingState
            {
                FiscalPeriods = new Dictionary<CountryId, FiscalPeriod>(_fiscalPeriods),
                PendingBudgetProcess = new List<CountryId>(_pendingBudgetProcessByCountry),
                IncomingBudgetWindowUsed = new List<CountryId>(_incomingBudgetWindowUsed),
                IncomingBudgetWindowOpenNow = new List<CountryId>(_incomingBudgetWindowOpenNow),   // PS-6 US-7 (format 39): which open process is the arrival window
                PendingBudgetBills = new Dictionary<CountryId, BudgetBill>(_pendingBudgetBillByCountry),
                PendingBudgetAlternatives = new Dictionary<CountryId, BudgetBill>(_pendingBudgetAlternativeByCountry),   // PS-3e (§632)
                PendingMajorityReconciliations = new Dictionary<CountryId, BudgetBill>(_pendingMajorityReconciliationByCountry),   // PS-6 US-23 (additive)
                PendingDebtLimitBills = new Dictionary<CountryId, BudgetBill>(_pendingDebtLimitBillByCountry),   // PS-6 US-24 (format 53)
                ExtraElectionDate = _extraElectionDate, ExtraElectionOrderedOn = _extraElectionOrderedOn,   // PS-3i (§636)
                PendingLaborBills = new Dictionary<CountryId, LaborPolicyBill>(_pendingLaborBillByCountry),
                PendingCrimeJusticeBills = new Dictionary<CountryId, CrimeJusticePolicyBill>(_pendingCrimeJusticeBillByCountry),
                PendingSectorBills = new Dictionary<CountryId, SectorPolicyBill>(_pendingSectorBillByCountry),
                PendingTradeBills = new Dictionary<CountryId, TradePolicyBill>(_pendingTradeBillByCountry),
                PendingSwfDrawdownBills = new Dictionary<CountryId, SwfDrawdownBill>(_pendingSwfDrawdownBillByCountry),
                PendingForeignPolicyMeetings = new Dictionary<CountryId, ForeignPolicyMeeting>(_pendingForeignPolicyMeetingByCountry),
                LastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>(_lastFiscalReports),
                LastEvents = new Dictionary<CountryId, EconomicEvent>(_lastEventsByCountry)
            };

            foreach (KeyValuePair<CountryId, Dictionary<TaxType, TaxProgramBill>> pair in _pendingTaxProgramBillsByCountry)
            {
                state.PendingTaxProgramBills[pair.Key] = new Dictionary<TaxType, TaxProgramBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> pair in _pendingWelfareProgramBillsByCountry)
            {
                state.PendingWelfareProgramBills[pair.Key] = new Dictionary<WelfareProgramType, WelfareProgramBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, Dictionary<string, LawBill>> pair in _pendingLawBillsByCountry)
            {
                state.PendingLawBills[pair.Key] = new Dictionary<string, LawBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> pair in _pendingCabinetDecisionsByCountry)
            {
                var records = new List<Persistence.PendingCabinetDecisionRecord>(pair.Value.Count);
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in pair.Value)
                {
                    records.Add(new Persistence.PendingCabinetDecisionRecord { Portfolio = portfolio, Decision = decision });
                }

                state.PendingCabinetDecisions[pair.Key] = records;
            }

            return state;
        }

        /// <summary>
        /// SAVE/LOAD (item 8): the restore half. Adopts the restored world (through SetWorld, whose
        /// published-history seeding is idempotent by that method's own contract), sets the clock
        /// through the private setters, then rebuilds every pending structure from the state object.
        /// Every section tolerates null - a save from before a structure existed restores it empty,
        /// the same forward-tolerance MissingMemberHandling gives the serialized layer. Callers do
        /// not call this directly in normal play: SaveGameService.RestoreInto pairs it with the RNG
        /// restore so the two cannot drift apart across call sites.
        /// </summary>
        // ---------------------------------------------------------------------------------------------
        // C-R4b step 3 (2026-09-02): THE PLAYER'S CAMPAIGN, IN THE DAY LOOP.
        //
        // The campaign spec's §3 calendar runs eight weeks before polling day. Until PS-2 (§619) the game's election was
        // the turn boundary the turn cadence named (turns 4, 8, …; its predicate since removed) and the campaign ran the eight weeks before it,
        // Sweden's real September date being the calendar's ask and the boundary the game's - a deviation stated here until
        // election night moved off the boundary. IT HAS (CL-4, §619): the calendar is built on the country's real polling day.
        //
        // The player's party is AI-PLAYED by its cast personality until the HQ screen's queue exists
        // (C-R4b step 4) - `LiveCampaignSetup` scripts nothing - so a campaign runs, is measured, and
        // reaches election night, and the player cannot yet touch it. Stated, so it is not mistaken for
        // a campaign the player ran.
        //
        // Persistence is replay (`PlayerCampaignRecord`): the run is deterministic under its three RNG
        // streams, so a save carries the streams' counts at the campaign's start and the days stepped,
        // and a load rewinds and re-steps - landing on the saved counts, which `RestoreCampaign` asserts.
        // ---------------------------------------------------------------------------------------------

        /// <summary>The country the player runs, told to the manager by the controller at selection and at load. Null until then - and no campaign runs for nobody.</summary>
        public CountryId? PlayerCountryId { get; set; }

        /// <summary>The running campaign's state between <see cref="Elections.CampaignCalendar.CampaignStart"/> and the election boundary; null outside a campaign.</summary>
        public Elections.CampaignRun.State PlayerCampaign { get; private set; }

        /// <summary>The finished campaign's result, from the day the last campaign day was stepped until the next campaign begins - election night reads it.</summary>
        public Elections.CampaignRun.Result PlayerCampaignResult { get; private set; }

        /// <summary>What the save carries of the campaign - see <see cref="Elections.PlayerCampaignRecord"/>. Null when no campaign has begun.</summary>
        public Elections.PlayerCampaignRecord CampaignRecord { get; private set; }

        /// <summary>
        /// CL-1 (2026-09-12): the player's PRE-campaign, stepped for the player's party alone between the calendar's
        /// `PreCampaignStart` and `CampaignStart` (<see cref="Elections.PreCampaignRun"/>); null outside the run-up and once the
        /// campaign has opened on its outcome. The AI parties' run-up is their staging - nothing of theirs moves before day 0.
        /// </summary>
        public Elections.PreCampaignRun.State PlayerPreCampaign { get; private set; }

        /// <summary>CL-1: the run-up's one stream - the polls the player buys before the campaign; never the campaign's three.</summary>
        private const SimulationRandom.Stream PreCampaignStream = SimulationRandom.Stream.PreCampaign;

        /// <summary>The first day of the given turn - the election boundary when that turn is an election turn.</summary>
        public static System.DateTime TurnBoundary(int turn) => EpochDate.AddDays((long)DaysPerTurn * turn);

        /// <summary>The next election turn strictly after the given turn (`ElectionSystem.ElectionCycle` apart).</summary>
        public static int NextElectionTurnAfter(int turn) => (turn / ElectionSystem.ElectionCycle + 1) * ElectionSystem.ElectionCycle;

        private static readonly SimulationRandom.Stream[] CampaignStreams =
        {
            SimulationRandom.Stream.CampaignAi, SimulationRandom.Stream.Debate, SimulationRandom.Stream.Scandal,
        };

        // ---------------------------------------------------------------------------------------------
        // PS-2 / CL-4 (2026-09-25, §619): POLLING DAY ON THE CALENDAR'S DATE. The player's country votes on its
        // own real polling day (`WorldClock.TryNextPollingDay` - Sweden's statute: every fourth year, the second
        // Sunday of September), inside whatever turn that day falls in; the campaign calendar is built on it,
        // and `AdvanceDay` raises `PollingDayToday` on that day for the controller's election call. The AI
        // countries hold no election in a run until their models exist (§618's ruling 4): their chambers hold
        // as of record. The turn cadence no longer times an election, and since AF-7 not the central bank head's term
        // either (`CentralBankTerms` - each bank's own statute); its predicate was removed with that last reader.
        // ---------------------------------------------------------------------------------------------

        /// <summary>True on the day the player's country votes - set by <see cref="AdvanceDay"/> after the date has advanced, read by the controller the same day.</summary>
        public bool PollingDayToday { get; private set; }

        // ---------------------------------------------------------------------------------------------
        // PS-3a (2026-09-25, §628): WHO GOVERNS IS THE PLAYER'S ROLE, NOT THE PLAYER'S COUNTRY. The AI finance ministry, the AI
        // energy ministry, the AI's book indexation and the budget process used to ask "is this the player's country?"; they ask
        // "does the player's party LEAD this country's government?" now (Country.Government, GovernmentRecord.RoleOf) - so a
        // player in opposition or support watches the AI govern their own country, the spec's §5.2. A country with no stored
        // government FAILS LOUDLY (PS-3b, §629): the role is never defaulted - every path that opens a world stores the record first. A junior partner
        // does NOT govern here - the portfolio-gated levers are PS-3's next part, stated.
        // ---------------------------------------------------------------------------------------------

        /// <summary>True where the player's party leads this country's government - the one test that decides whose levers move its book (§628).</summary>
        public bool PlayerGoverns(Country country)
        {
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id) { return false; }
            // PS-3b (§629): a country with NO government stored fails loudly - the role is never defaulted. WorldFactory stores every country's record
            // of the epoch at creation, so a null here is a defect (a Country built outside the factory), not a state.
            if (country.Government == null) { throw new System.InvalidOperationException($"{country.Id} has no government stored: who governs is unknown, and the player's role is never defaulted (PS-3b, §629) - WorldFactory stores GovernmentRecord.AtStart for every country"); }
            // A player country with NO PARTY SEATED is the instrument's hand on it - the meaning every Editor tool that names a player country without a party
            // has always had (the AI ministries keep off it). The game itself always seats a party (SelectPlayerCountry's fallback), so this is the tools' case, stated.
            if (string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return true; }
            if (country.PlayerHoldsPresidency) { return false; }   // SP-7b (I14 (2a); Art. 132, READING): while the player is President his party is the AI's - its government's levers the AI ministries'
            return country.Government.RoleOf(country.PlayerPartyAbbrev) == Elections.PlayerRole.PrimeMinister;
        }

        /// <summary>
        /// PS-3c (§630): THE ROLE GATE ON THE LEVERS. A bill is the government's to introduce - the political-system spec's §5.2: the prime minister's
        /// party has every lever; a junior partner its portfolios' (part 4, not yet built - none here); a support party and the opposition their votes
        /// alone. So a player whose party does not lead the government introduces NO bill in their own country, and every Introduce*Bill refuses with
        /// the reason. Another country's bills are its own AI government's (never refused here; the AI writes its book without a bill).
        /// PS-6 US-7: a president-elect's party governs from the oath, not before - refused like the opposition until then, the reason in the PRESIDENT-ELECT's
        /// words and its day (<see cref="Elections.GovernmentRecord.RoleOn"/>; words only, D-US composes them).
        /// PS-6 US-19 (R-US7 (a)): THIS IS THE EXECUTIVE'S GATE - the government's own acts (the budget, a fleet order, an appointment, a cabinet decision, a
        /// foreign meeting) and every bill in a country with one chamber. Who may refuse is unchanged; in a presidency whose Congress the game seats, a party out of
        /// the White House is refused in its own words - HOUSE MAJORITY, SENATE MAJORITY or MINORITY, and what it may introduce instead - because a BILL there is
        /// asked of <see cref="PlayerMayIntroduce(CountryId, Congress.Kind, CabinetPortfolio?, out string)"/>, which lets a chamber's majority introduce.
        /// </summary>
        public bool PlayerMayIntroduce(CountryId countryId, out string lockedBecause)
        {
            lockedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || PlayerGoverns(country)) { return true; }
            if (country.PlayerHoldsPresidency) { lockedBecause = PresidentOfficeWords; return false; }   // SP-7b: the party's levers are the AI's while the player holds the office
            Elections.PlayerRole role = Elections.GovernmentRecord.RoleOn(country, country.PlayerPartyAbbrev, CurrentDate);
            if (role == Elections.PlayerRole.PresidentElect && Elections.UsPresidentialElection.TryPresidentElect(country, CurrentDate, out Elections.UsPresidentialElection.Contest elect))
            {
                // PS-6 US-19: out of the White House until the oath, the party introduces in the chamber it holds (R-US7 (a)) - said where it holds one
                string introduces = Congress.Applies(country) ? Congress.IntroducesWords(country, country.PlayerPartyAbbrev, CurrentDate) : null;
                lockedBecause = "PRESIDENT-ELECT · THE OUTGOING ADMINISTRATION GOVERNS UNTIL " + OathDayWords(elect) + " · " + (introduces ?? "YOUR PARTY VOTES ON ITS BILLS");
                return false;
            }
            string usRole = Congress.RoleWord(role);
            if (usRole != null)
            {
                // PS-6 US-19: a party out of the White House in a Congress - what the chambers it holds let it do, never IN OPPOSITION with nothing to do
                lockedBecause = usRole + " · THE PRESIDENT'S ADMINISTRATION GOVERNS · " + (Congress.IntroducesWords(country, country.PlayerPartyAbbrev, CurrentDate) ?? Congress.HoldsNothingWords);
                return false;
            }
            string roleWord = role == Elections.PlayerRole.Support ? "IN SUPPORT" : role == Elections.PlayerRole.JuniorPartner ? "JUNIOR PARTNER" : "IN OPPOSITION";
            lockedBecause = role == Elections.PlayerRole.JuniorPartner ? "JUNIOR PARTNER · THE PRIME MINISTER'S LEVER · YOURS: " + country.Government.PortfoliosOf(country.PlayerPartyAbbrev) : roleWord + " · THE GOVERNMENT INTRODUCES BILLS · YOUR PARTY VOTES ON THEM";
            return false;
        }

        /// <summary>The president-elect's oath day in a gate's words - "20 JAN 2025".</summary>
        private static string OathDayWords(Elections.UsPresidentialElection.Contest elect) =>
            elect.TakesOffice.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();

        /// <summary>
        /// PS-6 US-19 (`docs/specs/USA_STAGE_PLAN.md`, "Roles in a presidential system"; R-US7 (a), RULED F8): THE GATE ON A BILL, BY ITS KIND - every
        /// Introduce*Bill asks it. In a presidency whose Congress the game seats (the player's USA, `Congress.Applies`), "the president's party introduces the
        /// administration's bills and proposes the budget (31 U.S.C. 1105(a)); a party out of the White House introduces ordinary bills in any chamber it
        /// controls, and revenue bills only where it holds the House (Art. I §7 cl. 1); a party holding neither ... votes and introduces nothing" - the president's
        /// party passes (<see cref="PlayerGoverns"/>), and any other party is asked of `Congress.MayIntroduce` by the chambers it holds on the day (a
        /// president-elect's included: out of the White House until the oath). The budget is gated to the president's party here; its procedure through
        /// Congress is US-22's. EVERY OTHER COUNTRY, AND AN AI USA, take the gate they always took
        /// (<see cref="PlayerMayIntroduce(CountryId, CabinetPortfolio?, out string)"/>, the role and the portfolio), word for word.
        /// </summary>
        public bool PlayerMayIntroduce(CountryId countryId, Congress.Kind kind, CabinetPortfolio? portfolio, out string lockedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            bool inCongress = country != null && PlayerCountryId.HasValue && PlayerCountryId.Value == countryId && country.Government != null
                              && country.Government.Kind == Elections.WorldClock.ExecutiveKind.Presidency && Congress.Applies(country);
            if (!inCongress || PlayerGoverns(country)) { return PlayerMayIntroduce(countryId, portfolio, out lockedBecause); }
            if (Congress.MayIntroduce(country, country.PlayerPartyAbbrev, kind, CurrentDate, out string why)) { lockedBecause = null; return true; }
            Elections.PlayerRole role = Elections.GovernmentRecord.RoleOn(country, country.PlayerPartyAbbrev, CurrentDate);
            if (role == Elections.PlayerRole.PresidentElect && Elections.UsPresidentialElection.TryPresidentElect(country, CurrentDate, out Elections.UsPresidentialElection.Contest elect))
            {
                // the president-elect's party is out of the White House to the oath - the outgoing administration's budget, and its bills where it holds no chamber
                lockedBecause = kind == Congress.Kind.Budget ? "PRESIDENT-ELECT · THE OUTGOING ADMINISTRATION PROPOSES THE BUDGET UNTIL " + OathDayWords(elect) + " · YOUR PARTY VOTES ON IT"
                    : why == Congress.HoldsNothingWords ? "PRESIDENT-ELECT · THE OUTGOING ADMINISTRATION GOVERNS UNTIL " + OathDayWords(elect) + " · YOUR PARTY VOTES ON ITS BILLS"
                    : "PRESIDENT-ELECT · " + why;
                return false;
            }
            lockedBecause = (Congress.RoleWord(role) ?? "IN OPPOSITION") + " · " + why;
            return false;
        }

        /// <summary>
        /// PS-3g (§634): THE PORTFOLIO LEG OF THE GATE. A JUNIOR PARTNER holds the levers of the portfolios its party holds (the spec's §5.2) - a bill
        /// that belongs to one of them is its to introduce; a lever with no portfolio, or another party's, is the prime minister's. The map of levers to
        /// portfolios (<see cref="PortfolioOfLaw"/> and the callers): a tax-programme bill FINANCE; a welfare-programme bill HEALTH; a crime-and-justice
        /// bill INTERIOR; a trade bill and the foreign-policy meeting FOREIGN; a law by its category (crime INTERIOR; the fiscal, monetary and electricity
        /// categories FINANCE); a cabinet decision its own portfolio. STATED: the budget (the partner's say is the coalition agreement, §5.2), the fund's
        /// drawdown, the labour, sector and energy levers, the pension age and the central bank stay the prime minister's - no portfolio of the six owns
        /// them (AF-6's economy portfolio carries none of them yet), and the spec gives the budget to the agreement.
        /// </summary>
        public bool PlayerMayIntroduce(CountryId countryId, CabinetPortfolio? portfolio, out string lockedBecause)
        {
            // §755 (Elias's ruling A2: "nothing is frozen"): the head of government keeps every lever, Finance's included - §716's refusal under a
            // player head is gone
            if (PlayerMayIntroduce(countryId, out lockedBecause)) { return true; }
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || country.PlayerHoldsPresidency || country.Government.RoleOf(country.PlayerPartyAbbrev) != Elections.PlayerRole.JuniorPartner) { return false; }   // SP-7b: no portfolio is the player-president's
            // §755 ("a partner holding Finance acts through the fiscal stance only ... the same whether the player or the AI holds Finance"): a player
            // partner holding Finance has the stance's dial (the Budget page), not Finance's other levers - as an AI partner has none of them
            if (portfolio == CabinetPortfolio.FinanceTreasury && Simulation.FinancePartner.PlayerHolds(country))
            {
                lockedBecause = "JUNIOR PARTNER · THE FINANCE MINISTER ACTS THROUGH THE FISCAL STANCE · THE BUDGET PAGE'S DIAL";
                return false;
            }
            if (portfolio.HasValue && country.Government.HoldsPortfolio(country.PlayerPartyAbbrev, portfolio.Value)) { lockedBecause = null; return true; }
            string yours = country.Government.PortfoliosOf(country.PlayerPartyAbbrev);
            lockedBecause = portfolio.HasValue
                ? $"JUNIOR PARTNER · THE {Effectiveness.ShortName(portfolio.Value).ToUpperInvariant()} MINISTER'S LEVER · YOURS: {yours}"
                : $"JUNIOR PARTNER · THE PRIME MINISTER'S LEVER · YOURS: {yours}";
            return false;
        }

        /// <summary>§755 (Elias's ruling A2): the player's party, holding Finance as a partner, sets the stance it asks - points of GDP, + = expansion,
        /// within FinancePartner.PlayerTargetLimit - the minister's call, no bill: its step rides the year's budget as an AI partner's does. False where
        /// the player's party does not hold Finance as a partner. AF-11: a lever outside the turn's decision, so it pulls through Pull and the impact ledger
        /// reads it (<see cref="PlayerLeverKind.FinanceStance"/>).</summary>
        public bool SetFinanceStanceTarget(CountryId countryId, float target)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || !FinancePartner.PlayerHolds(country)) { return false; }
            return Pull(new PlayerLever(PlayerLeverKind.FinanceStance, countryId, (world, c, replay) =>
            {
                c.FinanceStancePlayerTarget = Mathf.Clamp(target, -FinancePartner.PlayerTargetLimit, FinancePartner.PlayerTargetLimit);
                return true;
            }), country);
        }

        // ---------------------------------------------------------------------------------------------
        // AF-11 (Elias's ruling q11 of 2026-10-18, `docs/RULINGS_2026-10-18.md`; the audit's GA-0-55): EVERY LEVER THE UI WRITES GETS A MANAGER VERB, AND
        // THE IMPACT LEDGER READS IT. The levers below act on the day they are pulled and ride no PolicyDecision; each was a write into the model from the
        // UI. Each verb asks the gate the UI draws its lock from, then pulls through Pull, which shows the lever to the impact ledger before it acts (a
        // family first touched forks its counterfactual from the state the lever finds) and after (every other family's counterfactual does it too) - so
        // a lever's effect is attributed to its own family, as a dial's is, and no longer to every family forked before it (§544 (b)).
        // `UiLeverVerbCheck` holds the UI to the verbs.
        // ---------------------------------------------------------------------------------------------

        /// <summary>AF-11: the impact ledger reading this game's levers - set by the game that owns the ledger; null in every other world (a shadow's
        /// counterfactual, a check's), where a lever acts and nothing watches.</summary>
        public PolicyImpactLedger ImpactLedger { get; set; }

        /// <summary>AF-11: the days this clock has run of the turn in progress - 0 after a boundary, <see cref="DaysPerTurn"/> on the day
        /// <see cref="AdvanceDay"/> reports the boundary and before it is crossed. A counterfactual world reads it to act on the real game's day.</summary>
        public int DaysIntoTurn => (int)(CurrentDate - EpochDate).TotalDays - CurrentTurn * DaysPerTurn;

        private bool Pull(PlayerLever lever, Country country)
        {
            // FORK-2: a lever pulled inside one of the player's answers is part of that answer - the answer's replay pulls it again in every except-world
            PolicyImpactLedger ledger = _answering > 0 ? null : ImpactLedger;
            ledger?.BeforeLever(this, lever);
            if (!lever.ApplyTo(_world, country, replay: false)) { return false; }
            ledger?.AfterLever(this, lever);
            return true;
        }

        /// <summary>FORK-2: a copier of what the player handed a verb (a bill, a proposal), for the impact ledger's replays - null where no ledger watches this
        /// game or the answer is inside another (nothing is replayed then), and a copier yielding null where the value would not copy (the replay then refuses,
        /// and the log says so; the player's own act is never refused for it).</summary>
        private System.Func<T> CopiesForLedger<T>(T value) where T : class
        {
            if (ImpactLedger == null || _answering > 0) { return null; }
            try { return PlayerAnswer.Copies(value); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"LEDGER: the player's {typeof(T).Name} could not be copied for the impact ledger's worlds ({e.GetType().Name}: {e.Message}) - they do not see it");
                return () => null;
            }
        }

        /// <summary>FORK-2: how many of the player's answers are being given on this manager now - an answer given inside another (a verb calling a verb) is
        /// the outer one's, so only the outermost reaches the impact ledger.</summary>
        private int _answering;

        /// <summary>
        /// FORK-2 (D-45's remainder; the s871 review's M2): the player's NON-LEVER answer <paramref name="answer"/>, given by <paramref name="act"/> (the verb's
        /// own body). Where it acted, the impact ledger watching this game is told after the act (<see cref="PolicyImpactLedger.AfterAnswer"/>), and every
        /// except-world gives the same answer on the same day - so a fork with no policy difference stays the played world. A counterfactual's manager has no
        /// ledger, so a replay is never told again. K51 (Elias's ruling of 2026-10-23: "each kind of bill is its own ledger family"): an answer that lays a bill
        /// (`PlayerAnswer.Bill`) is shown to the ledger BEFORE the act too, as a lever is (`Pull`) - a kind first laid forks its family's world from the state
        /// the bill finds, and a bill the chamber's gate refuses leaves no family behind.
        /// </summary>
        private bool Answered(PlayerAnswer answer, System.Func<bool> act)
        {
            if (ImpactLedger == null || _answering > 0) { return act(); }
            PolicyImpactLedger ledger = ImpactLedger;
            bool forked = ledger.BeforeAnswer(this, answer);   // K51: false for an answer that lays no bill, and for a kind already forked
            _answering++;
            bool acted;
            try { acted = act(); }
            finally { _answering--; }
            if (acted) { ledger.AfterAnswer(this, answer); }
            else if (forked) { ledger.ForgetUnlaid(answer); }   // K51: the bill was refused - its kind was never laid, and a world forked for it would count from a touch that never came
            return acted;
        }

        /// <summary>AF-11: "Set override" - the partner's rate on its goods pinned at today's (the bloc's or the base), from which only the trade bill moves
        /// it. The trade bill's gate (the UI's lock: a trade bill's kind, Foreign's portfolio). False, with the reason, where it is refused.</summary>
        public bool SetPartnerTariffOverride(CountryId countryId, CountryId partnerId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null) { refusedBecause = "NO COUNTRY"; return false; }
            if (!PlayerMayIntroduce(countryId, Congress.Kind.Trade, CabinetPortfolio.ForeignAffairs, out refusedBecause)) { return false; }
            if (country.TradePartners.Find(p => p.PartnerId == partnerId) == null || _world.GetCountry(partnerId) == null) { refusedBecause = "NOT A TRADE PARTNER"; return false; }
            var lever = new PlayerLever(PlayerLeverKind.PartnerTariffOverride, countryId, (world, c, replay) =>
            {
                TradePartner link = c.TradePartners.Find(p => p.PartnerId == partnerId);
                Country partner = world.GetCountry(partnerId);
                if (link == null || partner == null) { return false; }
                float tariffOnOurImports = TradeSystem.GetTariffRate(c, partner, world.TradeBlocs);
                link.PlayerTariffOverride = Mathf.Clamp(tariffOnOurImports, MinBaseTariffRate, MaxBaseTariffRate);
                return true;
            });
            return Pull(lever, country);
        }

        /// <summary>AF-11: the central bank's head appointed from the candidates (PS-3c, §630: the government's to appoint - the executive's gate).
        /// False, with the reason, where it is refused. AF-7: the appointee is seated for its statute's full term from today (<see cref="CentralBankTerms"/>);
        /// the verb does not refuse a pick before the sitting head's term ends (the screens offer candidates only at its end - removal is not modelled).
        /// PS-6 US-29 (R-US12 (a)) with K16 (Elias's ruling of 2026-10-23): the Fed chair is the President's nominee and the Senate's to confirm by majority -
        /// the verb LODGES the nominee before the Senate (`UsFedConsent.Lodge`: the nominating party, the day, the vote day the record's lag later)
        /// under any Senate, hostile or not, and the Senate votes on that day (`AppointCentralBankHeadsWhoseTermsEnded`); the Vice Chair acts
        /// meanwhile (K15). Refused while a nomination is already before the Senate.</summary>
        public bool AppointCentralBankHead(CountryId countryId, FedChair candidate, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null || candidate == null) { refusedBecause = "NO CANDIDATE"; return false; }
            if (!PlayerMayIntroduce(countryId, out refusedBecause)) { return false; }
            System.Func<World, Country, bool, bool> act;   // one lever, one build (UiLeverVerbCheck): the Fed's lodge or AF-7's seat
            if (UsFedConsent.Applies(country))
            {
                if (country.FedNominationPending != null) { refusedBecause = "A NOMINATION IS ALREADY BEFORE THE SENATE"; return false; }
                if (!Congress.PresidentOn(country, CurrentDate, out string president, out string party, out _) || string.IsNullOrEmpty(party)) { refusedBecause = "NO PRESIDENT IS FOUND TO NOMINATE"; return false; }
                System.DateTime on = CurrentDate;
                Debug.Log($"FED: {country.Id} - {president} ({party}) nominated {candidate.Name} on {on:yyyy-MM-dd}; the Senate votes on {on.AddDays(UsFedConsent.NominationToVoteDays):yyyy-MM-dd}");
                act = (world, c, replay) => { UsFedConsent.Lodge(c, replay ? candidate.Copy() : candidate, party, on); return true; };   // GA-1-5: a counterfactual world lodges its own copy
            }
            else
            {
                CentralBankTerms.Seat(candidate, CentralBankTerms.For(countryId), CurrentDate);   // AF-7: before the lever is pulled, so every world's copy carries the term
                act = (world, c, replay) => { c.CurrentFedChair = replay ? candidate.Copy() : candidate; return true; };   // GA-1-5: a counterfactual world seats its own copy
            }
            return Pull(new PlayerLever(PlayerLeverKind.CentralBankHead, countryId, act), country);
        }

        /// <summary>AF-7 (the review's M1): true on a manager that hosts a COUNTERFACTUAL world - set by every `ShadowBaseline` constructor (the impact
        /// ledger's except-worlds, the no-policy world), never inferred. Nobody answers a hold there, so the player's bank's head is appointed by the AI's
        /// keyed rule on its term's day, as an AI-run bank's: "without your policies" is the AI's pick, the same name in every counterfactual. The Fed is the
        /// exception (the K15-K17 review's H-1): a counterfactual waits for the player's pick as the played world does (`FedAwaitsPlayersPick`), the replayed
        /// lever lodging it, and only a world the pick never reaches takes the AI's pick - the morning after, by the AI's rule for an unanswered wait
        /// (`AnswerWaitsAsTheAi`), as the debt limit's (K49).</summary>
        public bool IsCounterfactual { get; set; }

        /// <summary>AF-7 (q7, q10): true where the player's government appoints this country's central bank head - the player's own country with its own bank,
        /// the executive's gate open (<see cref="PlayerMayIntroduce(CountryId, out string)"/>), in the played world (not <see cref="IsCounterfactual"/>).
        /// Every other head is the AI's to appoint, the ECB's always.</summary>
        public bool PlayerAppointsCentralBankHead(Country country) =>
            !IsCounterfactual && country?.CurrentFedChair != null && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id && PlayerMayIntroduce(country.Id, out _);

        /// <summary>AF-7: true while the player's own bank's head serves past the term's end, waiting for the player's pick - the hold `GameController` raises.
        /// K16 (Elias's ruling of 2026-10-23): for the Fed, a player-president may nominate under any Senate, hostile or not, so the hold rises at the term's
        /// end whatever the count - and not while a nomination is before the Senate (`Country.FedNominationPending`); after a refusal it rises again. The Vice
        /// Chair acts meanwhile (K15, `UsFedConsent`'s class note).</summary>
        public bool CentralBankHeadAwaitsPlayer(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            return country != null && PlayerAppointsCentralBankHead(country) && CentralBankTerms.Ended(country.CurrentFedChair, CurrentDate)
                   && (!UsFedConsent.Applies(country) || country.FedNominationPending == null);
        }

        /// <summary>The K15-K17 review's H-1: the Fed's own reading of the player's hold, in EVERY world - the player's USA where the role gate lets the player
        /// appoint (`PlayerMayIntroduce`), a counterfactual's too. The played world holds its clock there until the player lodges a nominee; a counterfactual
        /// waits the same day for the replayed lever, so a fork follows the played world exactly where no policy differs.</summary>
        private bool FedAwaitsPlayersPick(Country country) =>
            UsFedConsent.Applies(country) && country.CurrentFedChair != null && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id && PlayerMayIntroduce(country.Id, out _);

        /// <summary>The review's H-1: a counterfactual's answer to the Fed's unanswered wait, the morning after (`AnswerWaitsAsTheAi`): where the term has ended
        /// unfilled, nothing is before the Senate and the world waits for the player's pick, the AI's keyed nominee goes to the count and is seated - the pick
        /// "without your policies". True where it seated. Never the played world's (the caller's gate).
        /// The second delta review's H-B: never before a turn's close - a term ending on a turn's last day closes that turn with the Vice Chair acting in every
        /// world waiting for the player's pick, as the played world does (`UsFedConsent`'s class note).</summary>
        private bool AnswerFedChairAsTheAi(Country country)
        {
            // the delta review's L-D: the cheap tests first, the role gate last
            if (country == null || country.FedNominationPending != null || !CentralBankTerms.Ended(country.CurrentFedChair, CurrentDate) || !FedAwaitsPlayersPick(country)) { return false; }
            FedChair outgoing = country.CurrentFedChair;
            FedChair nominee = CentralBankTerms.AiNominee(CentralBankTerms.Fed, outgoing, CurrentDate);
            if (!ConsentsToAiNominee(country, nominee, outgoing)) { return false; }
            country.CurrentFedChair = CentralBankTerms.Seat(nominee, CentralBankTerms.Fed, CurrentDate);
            country.FedViceChairActing = UsFedConsent.ViceChairActs(country, CurrentDate);
            return true;
        }

        /// <summary>
        /// K16: the Senate's vote on a pending nomination on or after its vote day, by <paramref name="count"/> (the day's). A null count (no president or no
        /// Senate found) is a DEFECT, never a refusal (the first review's M-2), and its retry is bounded (the delta review's L-A): on the vote day it is logged once
        /// and held again the next day; on any later day it is logged once more and the nomination is RELEASED - cleared, nothing recorded - so the hold rises
        /// again for a player who still appoints, or the AI seats where the player has no gate. True where the Senate voted.
        /// </summary>
        private bool HoldPendingFedVote(Country country, FedNomination pending, UsFedConsent.Count count)
        {
            if (count == null)
            {
                bool release = CurrentDate.Date > pending.VoteOn;
                Debug.LogError($"FED: {country.Id} - no president or no Senate found on {CurrentDate:yyyy-MM-dd} to vote on {pending.Nominee?.Name ?? "the nominee"} (a defect): "
                               + (release ? "the nomination is released, nothing recorded - the choice is open again" : "the vote is held again tomorrow"));
                if (release && ReferenceEquals(country.FedNominationPending, pending)) { country.FedNominationPending = null; }
                return false;
            }
            FedSenateVote vote = UsFedConsent.Resolve(country, pending, count, CurrentDate);
            Debug.Log($"FED: {country.Id} - the Senate's vote on {vote.Nominee ?? "the nominee"} (nominated {pending.NominatedOn:yyyy-MM-dd}) on {CurrentDate:yyyy-MM-dd}: {UsFedConsent.CountWords(count)}"
                      + (vote.Confirmed ? string.Empty : " - refused; the Vice Chair acts on"));
            return true;
        }

        /// <summary>
        /// AF-7 (q7, q10; GA-0-29, GA-0-53): every head whose statutory term has ended is replaced on the day - a country's own bank by its AI government
        /// (<see cref="CentralBankTerms.AiAppointment"/>), unless the player's government appoints it, in which case the sitting head serves on (for the Fed the Vice Chair acts, K15) until the
        /// player's pick (the hold); a shared zone's head (the ECB's President) always by the AI - once per zone, whichever member reaches it first.
        /// The AI's shortlist is keyed on the game's seed, the bank and the day, so every world of one game seats the same head and no counted stream moves.
        /// </summary>
        private void AppointCentralBankHeadsWhoseTermsEnded()
        {
            if (_world == null) { return; }
            List<CurrencyZone> zonesSeen = null;
            foreach (Country country in _world.Countries)
            {
                FedChair own = country.CurrentFedChair;
                if (own != null)
                {
                    bool fed = UsFedConsent.Applies(country);
                    // K16: the Senate votes a pending nomination on its vote day, by the day's count on the nominating party's nominee
                    FedNomination pending = fed ? country.FedNominationPending : null;
                    if (pending != null && CurrentDate.Date >= pending.VoteOn && HoldPendingFedVote(country, pending, UsFedConsent.Vote(country, CurrentDate, pending.NominatingParty)))
                    {
                        own = country.CurrentFedChair;
                    }
                    // the review's H-1: for the Fed the AI does not seat in a world that waits for the player's pick (a counterfactual too, `FedAwaitsPlayersPick`)
                    if (CentralBankTerms.Ended(own, CurrentDate) && !PlayerAppointsCentralBankHead(country) && !(fed && FedAwaitsPlayersPick(country)))
                    {
                        CentralBankTerms.Statute statute = CentralBankTerms.For(country.Id);
                        FedChair nominee = CentralBankTerms.AiNominee(statute, own, CurrentDate);
                        if (!fed || ConsentsToAiNominee(country, nominee, own))   // PS-6 US-29: refused - the Vice Chair acts (K15)
                        {
                            country.CurrentFedChair = CentralBankTerms.Seat(nominee, statute, CurrentDate);
                            // the delta review's H-A: a nomination the player lodged and left the presidency with LAPSES - the AI president's pick is seated at the
                            // oath (the role gate closing), as nominations pending at a change of administration are returned (the integrator's reading)
                            if (fed) { country.FedNominationPending = null; }
                        }
                    }
                    if (fed) { country.FedViceChairActing = UsFedConsent.ViceChairActs(country, CurrentDate); }   // K15: the Vice Chair acts while the term has ended unfilled
                    continue;
                }
                CurrencyZone zone = country.CurrencyZone;
                if (zone?.Head == null || !CentralBankTerms.Ended(zone.Head, CurrentDate)) { continue; }
                if (zonesSeen == null) { zonesSeen = new List<CurrencyZone>(); }
                if (zonesSeen.Contains(zone)) { continue; }
                zonesSeen.Add(zone);
                zone.Head = CentralBankTerms.AiAppointment(CentralBankTerms.For(country.Id), zone.Head, CurrentDate);
            }
        }

        /// <summary>
        /// PS-6 US-29 (R-US12 (a)): the Senate's count on an AI president's Fed nominee (<see cref="UsFedConsent.Vote"/>) - the president of the day, the Senate
        /// the world seats or, where it seats none (an AI USA), the record's by date. A refusal leaves the seat unfilled, the Vice Chair acting (K15), and the same nominee goes again
        /// the next day (the keyed shortlist names the same nominee every day of the holdover); the log says so on the term's own day, on every 3 January (a new Congress -
        /// the review of 2026-10-22, L3: the AI's turn to nominate can begin after the term's day, as when a save is cut inside a holdover) and on every 20 January
        /// (the oath: a player-president who leaves office inside a holdover hands the nomination to the AI that day, and a nomination the player lodged lapses
        /// with the AI's confirmation - the second review's L5, kept by the K15-K17 delta review's H-A), and says every
        /// confirmation. No president or no Senate found is a defect, logged as one on those days, and the seat stays unfilled.
        /// </summary>
        private bool ConsentsToAiNominee(Country country, FedChair nominee, FedChair outgoing)
        {
            UsFedConsent.Count count = UsFedConsent.Vote(country, CurrentDate);
            bool logDay = outgoing.TermEnds == CurrentDate.Date || (CurrentDate.Month == 1 && (CurrentDate.Day == 3 || CurrentDate.Day == 20));
            if (count == null)
            {
                if (logDay) { Debug.LogError($"FED: {country.Id} - no president or no Senate found on {CurrentDate:yyyy-MM-dd} to confirm {nominee?.Name ?? "a nominee"} (a defect): the seat stays unfilled"); }
                return false;
            }
            if (count.Confirms || logDay)
            {
                Debug.Log($"FED: {country.Id} - {count.President} ({count.PresidentsParty}) nominated {nominee?.Name ?? "no one"} on {CurrentDate:yyyy-MM-dd}: {UsFedConsent.CountWords(count)}"
                          + (count.Confirms ? string.Empty : " - " + (UsFedConsent.ActingWords(country, CurrentDate) ?? outgoing.Name + " serves on") + " until a successor is confirmed"));
            }
            return count.Confirms;
        }

        /// <summary>AF-11: a power-plant order in the turn being played (<see cref="EnergyFleet.Place"/>: <paramref name="mw"/> positive builds, negative
        /// retires; <paramref name="year"/> is what the page prints) - the government's (PS-3c, §630: the executive's gate). False, with the reason, where
        /// it is refused or nothing could be placed.</summary>
        public bool PlaceFleetOrder(CountryId countryId, int technology, double mw, int year, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null) { refusedBecause = "NO COUNTRY"; return false; }
            if (!PlayerMayIntroduce(countryId, out refusedBecause)) { return false; }
            refusedBecause = mw > 0 ? EnergyFleet.CannotPlaceWhy(country, technology, mw, year) : EnergyFleet.CannotOrderWhy(countryId, technology) ?? EnergyReliability.RefusalFor(country, technology, mw);   // MF q26: a retirement meets the AI ministry's reliability check (EnWG § 13b where the statute is sourced)
            if (refusedBecause != null) { return false; }
            // the orders EnergyFleet.Place places nothing for, refused here so no lever reaches the ledger: a zero order, a retirement with nothing left to retire
            if (System.Math.Abs(mw) < 1e-9) { refusedBecause = "NOTHING TO PLACE"; return false; }
            if (mw < 0 && EnergyFleet.CapacityMw(country, technology) + EnergyFleet.QueuedMw(country, technology, retirementsOnly: true) <= 0) { refusedBecause = "NOTHING LEFT TO RETIRE"; return false; }
            int turn = CurrentTurn;
            if (Pull(new PlayerLever(PlayerLeverKind.FleetOrder, countryId, (world, c, replay) => EnergyFleet.Place(c, technology, mw, year, turn) != null), country)) { return true; }
            refusedBecause = "NOTHING TO PLACE";
            return false;
        }

        /// <summary>AF-11: a minister appointed to a portfolio from its shortlist - the portfolio's gate (PS-3g, §634). False, with the reason, where it is refused.</summary>
        public bool AppointMinister(CountryId countryId, CabinetPortfolio portfolio, CabinetMinister candidate, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null || candidate == null) { refusedBecause = "NO CANDIDATE"; return false; }
            if (!PlayerMayIntroduce(countryId, portfolio, out refusedBecause)) { return false; }
            return Pull(new PlayerLever(PlayerLeverKind.MinisterAppointment, countryId, (world, c, replay) => { c.CabinetMinisters[portfolio] = replay ? candidate.Copy() : candidate; return true; }), country);   // GA-1-5: a counterfactual world seats its own copy
        }

        /// <summary>AF-11: a minister reshuffled out - the portfolio left vacant and the dismissal's approval cost paid (P2-5.2), recorded on the approval
        /// ledger as <paramref name="approvalEventLabel"/> (the surface's words). The portfolio's gate (PS-3g, §634). False, with the reason, where it is
        /// refused or the portfolio is vacant.</summary>
        public bool ReshuffleMinister(CountryId countryId, CabinetPortfolio portfolio, string approvalEventLabel, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null) { refusedBecause = "NO COUNTRY"; return false; }
            if (!PlayerMayIntroduce(countryId, portfolio, out refusedBecause)) { return false; }
            if (!country.CabinetMinisters.ContainsKey(portfolio)) { refusedBecause = "THE PORTFOLIO IS VACANT"; return false; }
            System.DateTime today = CurrentDate;
            var lever = new PlayerLever(PlayerLeverKind.MinisterReshuffle, countryId, (world, c, replay) =>
            {
                // ⚠ AF-11 moved this act from the UI in its own order: the minister leaves BEFORE the popularity factor reads the portfolio, so the factor
                // reads a vacancy - recorded as a finding for its own ruling, not changed here (a move of the cost is a numeric change of its own)
                if (!c.CabinetMinisters.Remove(portfolio)) { return false; }
                float approvalBeforeReshuffle = c.State.ApprovalRating;
                c.State.ApprovalRating = Mathf.Clamp(c.State.ApprovalRating - CabinetSystem.ReshuffleApprovalCost * CabinetSystem.PopularityFactor(c, portfolio), 0f, 100f); TaxTerm.AfterShock(c, approvalBeforeReshuffle, -(CabinetSystem.ReshuffleApprovalCost * CabinetSystem.PopularityFactor(c, portfolio)));   // P2-5.2: dismissing a popular minister costs the full figure, an unpopular one less
                ApprovalLedgerRecorder.RecordEvent(c, today, approvalEventLabel, c.State.ApprovalRating - approvalBeforeReshuffle);
                return true;
            });
            return Pull(lever, country);
        }

        /// <summary>§716, §755: the AI partner holding Finance in the player's own government, where the player's party leads (it moves the fiscal stance, the
        /// player's every lever its own); null where the player leads none, holds Finance itself, or the country is the instrument's hand (no party seated).</summary>
        public string FinancePartnerOfPlayer(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || string.IsNullOrEmpty(country.PlayerPartyAbbrev) || !PlayerGoverns(country)) { return null; }
            return FinancePartner.AiHolder(country);
        }

        /// <summary>§716: the Finance partner's rule runs where the AI's hand on the book does (the ministry's switch, which every harness measuring one
        /// rule alone turns off) and never on the instrument's hand (a player country with no party seated).</summary>
        private bool FinancePartnerRuns(Country country) =>
            AiFinanceMinistryEnabled && !(PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id && string.IsNullOrEmpty(country.PlayerPartyAbbrev));

        /// <summary>§716: the date of the boundary the current turn closes at - the epoch plus the turns so far and this one, a year each (the day
        /// count AdvanceDay's boundary test reads). The Finance partner's step is judged due as of it, at the boundary and in the preview alike.</summary>
        private System.DateTime ComingBoundaryDate => EpochDate.AddDays((CurrentTurn + 1) * (double)DaysPerTurn);

        /// <summary>§716 (the review's defect 1): the partner steps (since §755 the fiscal stance) AT THE BOUNDARY - where its rule runs, except the player's own
        /// country under an AI head, where the ministry's rule goes to the chamber as the government's bill and the partner's step rides it
        /// (<see cref="TableGovernmentBudget"/>). One rule, read by the boundary and by the preview.</summary>
        private bool PartnerStepsAtBoundary(Country country) =>
            FinancePartnerRuns(country) && !(AiFinanceMinistryEnabled && !PlayerGoverns(country) && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id);

        /// <summary>
        /// §773 (Elias's ruling E2, overruling §768's open question): THE FINANCE PARTNER'S RATES IN POLAND ARE A TAX ACT. "A rate moves only through the
        /// tax act (Sejm vote, then the veto), whoever proposes it. The partner's fiscal stance (A2) acts on the budget's totals." The boundary's step, where
        /// the lines' clamps left it to raise the household rates, puts those rates to the Sejm as a tax act the partner's party tables
        /// (<see cref="PolishStatuteActs"/>) and to the President; where it falls the rates are taken out of the turn's decision, only the lines' part of the
        /// step lands and is counted, and the year's step is spent all the same (a rejected bill spends it, §716).
        /// </summary>
        private void PartnerTaxAct(Country country, PolicyDecision decision, FinancePartner.Written written, System.DateTime boundary)
        {
            var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };
            foreach (TaxType type in written.Taxes) { if (decision.TaxRateOverrides.TryGetValue(type, out float rate)) { act.TaxLines[type] = rate; } }
            // the rates alone (§773's review): a bill built new names no fund, which the fund's act would read as a dissolution never asked
            PolishStatuteActs(country, act, out List<BudgetBill.StatutePart> fell, new[] { BudgetBill.StatutePart.Rates });
            if (!fell.Contains(BudgetBill.StatutePart.Rates)) { return; }
            foreach (TaxType type in written.Taxes) { decision.TaxRateOverrides.Remove(type); }
            written.Taxes.Clear();
            written.StancePoints -= written.RatePoints;
            written.RatePoints = 0f;
            written.RateAmount = 0f;
            written.Moves.Add(written.Lines.Count > 0
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "the rates' part fell - its tax act failed in the Sejm or its veto stood; the lines' part stands: {0:+0.00;-0.00} pp of GDP", written.StancePoints)
                : "the rates' part fell - its tax act failed in the Sejm or its veto stood; nothing of the step lands");
            if (!written.Any) { country.FinancePartnerSteppedOn = boundary; }   // nothing left to record: the year's step is spent all the same
        }

        /// <summary>PS-3g (§634): the portfolio a law bill belongs to by its category - crime INTERIOR; the fiscal, monetary and electricity-tax categories FINANCE; the labour categories none (the prime minister's).</summary>
        public static CabinetPortfolio? PortfolioOfLaw(LawBill bill)
        {
            LawDefinition law = bill != null ? LawCatalog.GetById(bill.LawId) : null;
            if (law == null) { return null; }
            switch (law.Category)
            {
                case LawCategory.CrimeJustice: return CabinetPortfolio.InteriorJustice;
                case LawCategory.FiscalFramework: case LawCategory.MonetaryRegime: case LawCategory.ElectricityTax: return CabinetPortfolio.FinanceTreasury;
                default: return null;
            }
        }

        /// <summary>PS-3i-2b (ruled 2026-09-29, §654): a budget vote was held - every supported agreement counts it, and an item owed through
        /// <see cref="Elections.SupportAgreement.BudgetVotesToBreak"/> of them breaks, recorded as the day's tracker records a break.</summary>
        public void CountBudgetVoteOnAgreements(Country country)
        {
            if (country?.Government == null) { return; }
            foreach (Elections.SupportAgreement agreement in country.Government.Agreements)
            {
                if (!country.Government.Support.Contains(agreement.Supporter)) { continue; }
                foreach (Elections.AgreementItem broken in agreement.CountBudgetVote(CurrentDate))
                {
                    country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: '{broken.Name}', owed to {agreement.Supporter}, is still undelivered after {Elections.SupportAgreement.BudgetVotesToBreak} budget votes - broken");
                    Debug.Log($"AGREEMENT: {country.Id} - '{broken.Name}' owed to {agreement.Supporter} broken, undelivered after {Elections.SupportAgreement.BudgetVotesToBreak} budget votes");
                }
            }
            WeighPartnerExits(country);   // COAL-1 (K130): an AI partner weighs leaving at each budget vote - every budget vote's site calls this method (SimulationManager.CoalitionExit.cs)
        }

        /// <summary>PS-3h (§635): every agreement's items read against the country - a delivery or a break is a fact of the book, so this runs at the turn boundary, the day a law bill applies and, in the player's country, every day (§644); a break is recorded on the government.</summary>
        public void TrackAgreements(Country country)
        {
            if (country?.Government == null) { return; }
            foreach (Elections.SupportAgreement agreement in country.Government.Agreements)
            {
                if (!country.Government.Support.Contains(agreement.Supporter)) { continue; }   // a withdrawn supporter's agreement stands as the record of it, no longer tracked
                foreach (Elections.AgreementItem broken in agreement.Track(country, CurrentDate))
                {
                    country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the government broke '{broken.Name}', owed to {agreement.Supporter} under its support agreement");
                    Debug.Log($"AGREEMENT: {country.Id} - the government broke '{broken.Name}' owed to {agreement.Supporter}");
                }
            }
        }

        /// <summary>
        /// PS-3h (§635): the player's support party WITHDRAWS - the spec's "threaten or withdraw support". Recorded on the government as a break and the
        /// party struck from its support; the government stands on without it, and what follows is the chamber's - the withdrawn party may move no
        /// confidence (§636), and an AI supporter moves where ruling (2) allows (§641).
        /// </summary>
        public bool WithdrawSupport(CountryId countryId, out string refusedBecause)
        {
            string why = null;   // FORK-2 (the review's H1): given again in every except-world on its day
            bool acted = Answered(new PlayerAnswer("support withdrawn", countryId, sim => sim.WithdrawSupport(countryId, out _)), () => WithdrawSupportNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="WithdrawSupport"/>'s act.</summary>
        private bool WithdrawSupportNow(CountryId countryId, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { refusedBecause = "NOT THE PLAYER'S COUNTRY"; return false; }
            if (country.PlayerHoldsPresidency) { refusedBecause = PresidentOfficeWords; return false; }   // SP-7b (I14 (2a)): the party is the AI's while the player is President
            if (country.Government.RoleOf(country.PlayerPartyAbbrev) != Elections.PlayerRole.Support) { refusedBecause = "YOUR PARTY IS NOT A SUPPORT PARTY"; return false; }
            country.Government.WithdrawSupport(country.PlayerPartyAbbrev, country);
            country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} withdrew its support - the government stands on without it; the chamber's confidence votes read it (§636)");
            Debug.Log($"AGREEMENT: {countryId} - {country.PlayerPartyAbbrev} withdrew its support");
            return true;
        }

        // ---------------------------------------------------------------------------------------------
        // PS-3i (2026-09-25, §636): CONFIDENCE AND COLLAPSE (the spec's §5.4; Sweden's rules in ConfidenceProcedure, sourced from Regeringsformen).
        // The motion is the player's verb, and an AI party's where ruling (2) allows it (§641: only a motion that would carry, by a mover that prefers
        // the government the round would form). A carried motion opens the government's week (RF 6 kap. 7 §): the player's government may order an extra election in it; an AI government does not (the 2021
        // precedent: the government resigned rather than dissolve - a premise); at the week's end the Speaker discharges the prime minister and with
        // them the government (6 kap. 9 §), which serves on as a caretaker, and the Speaker's round opens on the sitting chamber (§646, dated: a proposal
        // that wins its investiture installs its government; four rejected break the procedure off to an extra election within three months, 6 kap. 5 §).
        // ---------------------------------------------------------------------------------------------
        private System.DateTime _extraElectionDate = System.DateTime.MinValue;
        private System.DateTime _extraElectionOrderedOn = System.DateTime.MinValue;

        /// <summary>The ordered extra election's polling day, or MinValue.</summary>
        public System.DateTime ExtraElectionDate => _extraElectionDate;

        /// <summary>The player's party moves no confidence in the prime minister. False, with the reason, where no motion is taken up; otherwise the vote, recorded as a division.</summary>
        public bool MoveNoConfidence(CountryId countryId, out string refusedBecause, out Elections.ConfidenceProcedure.MotionVote vote)
        {
            string why = null;   // FORK-2: the motion moved again in every except-world on its day - each chamber votes on it as it stands there
            Elections.ConfidenceProcedure.MotionVote taken = null;
            bool acted = Answered(new PlayerAnswer("a motion of no confidence", countryId, sim => sim.MoveNoConfidence(countryId, out _, out _)), () => MoveNoConfidenceNow(countryId, out why, out taken));
            refusedBecause = why;
            vote = taken;
            return acted;
        }

        /// <summary>FORK-2: <see cref="MoveNoConfidence"/>'s act.</summary>
        private bool MoveNoConfidenceNow(CountryId countryId, out string refusedBecause, out Elections.ConfidenceProcedure.MotionVote vote)
        {
            vote = null;
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { refusedBecause = "NOT THE PLAYER'S COUNTRY"; return false; }
            Elections.GovernmentRecord g = country.Government;
            if (country.PlayerHoldsPresidency) { refusedBecause = PresidentOfficeWords; return false; }   // SP-7b (I14 (2a)): the party is the AI's while the player is President
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Unsourced) { refusedBecause = "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED"; return false; }
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Parlamento) { refusedBecause = "A MOTION OF NO CONFIDENCE UNDER ART. 94 IS NOT YET MODELLED"; return false; }   // PS-7: the investiture by mandates is built, the mid-term motion is not
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Presidential) { refusedBecause = "A PRESIDENCY SERVES A FIXED TERM · NO MOTION OF NO CONFIDENCE IS TAKEN UP"; return false; }   // PS-6 US-7: Art. II §1 - a fixed term, no confidence vote
            switch (g.RoleOf(country.PlayerPartyAbbrev))
            {
                case Elections.PlayerRole.PrimeMinister: refusedBecause = "THE GOVERNMENT MOVES NO MOTION AGAINST ITSELF"; return false;
                case Elections.PlayerRole.JuniorPartner: refusedBecause = "LEAVE THE GOVERNMENT FIRST"; return false;
                case Elections.PlayerRole.Support: refusedBecause = "WITHDRAW YOUR SUPPORT FIRST"; return false;
            }
            if (g.Caretaker) { refusedBecause = "NO MOTION IS TAKEN UP AGAINST A CARETAKER GOVERNMENT"; return false; }
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate) { refusedBecause = "NO MOTION IS TAKEN UP BETWEEN AN EXTRA ELECTION'S DECISION AND THE NEW RIKSDAG"; return false; }
            if (g.NoConfidenceOn != System.DateTime.MinValue) { refusedBecause = "THE CHAMBER HAS ALREADY DECLARED NO CONFIDENCE - THE GOVERNMENT'S WEEK RUNS"; return false; }
            if (Elections.ConfidenceProcedure.IsConstructive(countryId))
            {
                bool sejm = Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Sejm;   // COAL-1 (K133): Art. 158 Konstytucja, the Bundestag's shape
                // §705 (the review's defect 5): no constructive vote while the chancellor's election runs - before the convening the old Bundestag
                // sits, and a successor it elected would never be discharged at the convening (Art. 69 Abs. 2)
                if (!sejm && ((g.Round != null && g.Round.Open) || ElectionAwaitsRound(country, g))) { refusedBecause = "THE CHANCELLOR'S ELECTION IS UNDER WAY - ART. 63, NOT ART. 67"; return false; }
                if (sejm && g.Sejm != null && g.Sejm.Open) { refusedBecause = "THE SEJM'S FORMATION IS UNDER WAY - ART. 154, NOT ART. 158"; return false; }
                // §754 (the review's note): a party with no members moves nothing - the AI's rule, the player's too
                if (SeatsOf(country, country.PlayerPartyAbbrev) <= 0) { refusedBecause = "YOUR PARTY HOLDS NO SEAT - A CONSTRUCTIVE VOTE IS MOVED BY MEMBERS"; return false; }
                // §698 (Art. 67 GG): the motion IS the successor's election - carried, the successor's government takes office today
                refusedBecause = null;
                vote = ConstructiveVoteOf(country, country.PlayerPartyAbbrev, out Elections.FormationProposal successor, out Elections.ProposalVerdict verdict);
                // AF-8 (Elias's ruling q8: a premise backed by a statute becomes a ruling with its citation): WHO MAY MOVE IT is GO-BT § 97 Abs. 1, read in the
                // official print on disk [GOBT-4]: "Der Antrag ist von einem Viertel der Mitglieder des Bundestages oder einer Fraktion, die mindestens ein Viertel
                // der Mitglieder des Bundestages umfasst, zu unterzeichnen ... Anträge, die diesen Voraussetzungen nicht entsprechen, dürfen nicht auf die
                // Tagesordnung gesetzt werden." The signatories are §706's reading of § 4's identical quarter (ruled with it): the members who would elect the
                // candidate. Short of a quarter the motion is not put on the agenda - refused, no division. The AI moves only a motion that carries, a majority
                // of the members, so its quarter always stands.
                // COAL-1 (K133): the Sejm's motion is moved by at least 46 deputies, Art. 158 ust. 1 [TK-KONST-C:158] - read as the quarter is: the deputies who would choose the candidate.
                int quarter = sejm ? Elections.ConfidenceProcedure.SejmMovers : (vote.Members + 3) / 4;
                if (vote.For < quarter) { refusedBecause = sejm ? $"A CONSTRUCTIVE VOTE NEEDS {quarter} DEPUTIES TO MOVE IT - ITS CANDIDATE HAS {vote.For}" : $"A CONSTRUCTIVE VOTE NEEDS A QUARTER OF THE MEMBERS TO SIGN IT - {quarter} - ITS CANDIDATE HAS {vote.For}"; vote = null; return false; }
                country.Divisions.Append(vote.Title(), CurrentDate, vote.Carried ? 1f : -1f, vote.Carried, 0f, (int)BillAxis.Fiscal, vote.Sides);
                country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;
                if (vote.Carried && successor != null) { InstallSuccessor(country, successor, verdict, vote); }
                else if (vote.Carried) { Debug.Log($"CONFIDENCE: {countryId} - {PoliSim.Wording.EnglishText.Possessive(country.PlayerPartyAbbrev, false, true)} candidate is elected and no government can be drawn on its party; none installed"); }   // latent (§754)
                Debug.Log($"CONFIDENCE: {countryId} - {vote.Title()}{(vote.Refusal != null ? " - refused by " + vote.Refusal : string.Empty)}");
                return true;
            }
            if (!Elections.ConfidenceProcedure.CanBeTakenUp(country, country.PlayerPartyAbbrev, out int moverSeats, out int tenth)) { refusedBecause = $"A MOTION NEEDS A TENTH OF THE MEMBERS - {tenth} - YOUR PARTY HOLDS {moverSeats}"; return false; }
            refusedBecause = null;
            vote = Elections.ConfidenceProcedure.Vote(country, country.PlayerPartyAbbrev, CurrentDate);   // §653: the lines standing today
            country.Divisions.Append(vote.Title(), CurrentDate, vote.Carried ? 1f : -1f, vote.Carried, 0f, (int)BillAxis.Fiscal, vote.Sides);
            country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;   // a motion, not a bill (the reader, s636)
            if (vote.Carried)
            {
                g.NoConfidenceOn = CurrentDate;
                g.NoConfidenceMover = country.PlayerPartyAbbrev;
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the Riksdag declared no confidence in the prime minister ({g.PmParty}), {vote.For} of {vote.Members} members, moved by {country.PlayerPartyAbbrev} (RF 13 kap. 4 §)");
            }
            Debug.Log($"CONFIDENCE: {countryId} - {vote.Title()}");
            return true;
        }

        /// <summary>The player's junior partner leaves the government - struck from the cabinet, the portfolios re-apportioned among those who stay; it may then move no confidence as the opposition.</summary>
        public bool LeaveGovernment(CountryId countryId, out string refusedBecause)
        {
            string why = null;   // FORK-2 (the review's H1): given again in every except-world on its day
            bool acted = Answered(new PlayerAnswer("the government left", countryId, sim => sim.LeaveGovernment(countryId, out _)), () => LeaveGovernmentNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="LeaveGovernment"/>'s act.</summary>
        private bool LeaveGovernmentNow(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { refusedBecause = "NOT THE PLAYER'S COUNTRY"; return false; }
            if (country.PlayerHoldsPresidency) { refusedBecause = PresidentOfficeWords; return false; }   // SP-7b (I14 (2a)): the party is the AI's while the player is President
            if (country.Government.RoleOf(country.PlayerPartyAbbrev) != Elections.PlayerRole.JuniorPartner) { refusedBecause = "YOUR PARTY IS NOT A JUNIOR PARTNER"; return false; }
            // §705 (the review's latent case): the smaller member of a parliamentary group does not leave its group's cabinet alone - the CSU sits in the
            // Union's Fraktion, and a CDU cabinet without it is one no chamber could have elected
            if (SeatedGroupPartner(country, country.PlayerPartyAbbrev) is string groupPartner && country.Government.Cabinet.Contains(groupPartner))
            {
                refusedBecause = "YOUR PARTY SITS IN ONE FRAKTION WITH " + groupPartner.ToUpperInvariant() + " - IT DOES NOT LEAVE THE CABINET ALONE";
                return false;
            }
            refusedBecause = null;
            country.Government.LeaveCabinet(country.PlayerPartyAbbrev, country);   // the portfolios re-apportioned among those who stay
            country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} left the government");
            Debug.Log($"CONFIDENCE: {countryId} - {country.PlayerPartyAbbrev} left the government");
            ExitOpensConfidenceProcedure(country, country.PlayerPartyAbbrev);   // COAL-1 (q21, K131; GA-4-1): the exit opens the country's confidence procedure
            return true;
        }

        /// <summary>RF 6 kap. 7 §: the player's government answers a carried motion by ordering an extra election within the week - then no discharge follows.</summary>
        public bool OrderExtraElection(CountryId countryId, out string refusedBecause)
        {
            string why = null;   // FORK-2: the order given again in every except-world on its day
            bool acted = Answered(new PlayerAnswer("an extra election ordered", countryId, sim => sim.OrderExtraElection(countryId, out _)), () => OrderExtraElectionNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="OrderExtraElection"/>'s act.</summary>
        private bool OrderExtraElectionNow(CountryId countryId, out string refusedBecause)
        {
            if (!CanOrderExtraElection(countryId, out refusedBecause)) { return false; }
            Country country = _world.GetCountry(countryId);
            ScheduleExtraElection(country, "the government ordered it within the week of the declaration (RF 6 kap. 7 §)");
            country.Government.NoConfidenceOn = System.DateTime.MinValue;
            return true;
        }

        /// <summary>§641 (the reader): whether <see cref="OrderExtraElection"/> would be accepted today, and why not - so the desk draws the verb disabled with its reason.</summary>
        public bool CanOrderExtraElection(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            Elections.GovernmentRecord g = country?.Government;
            if (g == null || !PlayerGoverns(country)) { refusedBecause = "ONLY THE GOVERNMENT ORDERS AN EXTRA ELECTION"; return false; }
            if (g.Caretaker) { refusedBecause = "A CARETAKER GOVERNMENT ORDERS NO EXTRA ELECTION"; return false; }
            // [RF-R:3:11]: not within three months of a new Riksdag's first sitting - the chamber of record's first sitting, or the game's own last polling day where
            // the game has held one (its first sitting is not modelled; the polling day is the earlier bound, stated).
            System.DateTime sat = Elections.WorldClock.ChamberAt(countryId, CurrentDate).Convened;
            if (country.ElectionHistory != null) { foreach (Elections.ElectionRecord held in country.ElectionHistory) { if (Elections.NationalElection.CountsParliament(held.Method) && held.Date > sat) { sat = held.Date; } } }
            if (CurrentDate < sat.AddMonths(Elections.ConfidenceProcedure.ExtraElectionMonths)) { refusedBecause = "NO EXTRA ELECTION WITHIN THREE MONTHS OF THE NEW RIKSDAG'S FIRST SITTING"; return false; }
            if (g.NoConfidenceOn == System.DateTime.MinValue || CurrentDate >= g.NoConfidenceOn.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { refusedBecause = "NO DECLARATION OF NO CONFIDENCE IS WITHIN ITS WEEK"; return false; }
            refusedBecause = null;
            return true;
        }

        /// <summary>
        /// §641: the player's government answers a declaration by ASKING TO BE DISCHARGED - [RF-R:6:8]: "Ett statsråd ska entledigas om han eller hon begär
        /// det, statsministern av talmannen" - the route Löfven took in 2021 (confidence_rules.md). The discharge and the Speaker's round run at once,
        /// as they would at the week's end. With <see cref="OrderExtraElection"/>, the two answers that end the clock's hold on a declaration.
        /// </summary>
        public bool AskToBeDischarged(CountryId countryId, out string refusedBecause)
        {
            string why = null;   // FORK-2 (the review's H1): given again in every except-world on its day
            bool acted = Answered(new PlayerAnswer("a discharge asked", countryId, sim => sim.AskToBeDischarged(countryId, out _)), () => AskToBeDischargedNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="AskToBeDischarged"/>'s act.</summary>
        private bool AskToBeDischargedNow(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            Elections.GovernmentRecord g = country?.Government;
            if (g == null || !PlayerGoverns(country)) { refusedBecause = "ONLY THE PRIME MINISTER ASKS TO BE DISCHARGED"; return false; }
            if (g.Caretaker) { refusedBecause = "THE GOVERNMENT IS ALREADY DISCHARGED"; return false; }
            if (g.NoConfidenceOn == System.DateTime.MinValue) { refusedBecause = "NO DECLARATION OF NO CONFIDENCE STANDS"; return false; }
            refusedBecause = null;
            g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the prime minister asked the Speaker to be discharged (RF 6 kap. 8 §)");
            DischargeAndRound(country);
            return true;
        }

        private void ScheduleExtraElection(Country country, string why, System.DateTime? dissolvedOn = null)
        {
            // PS-8 (L-N2 of the s938 review): the extra-election date here is the PLAYER'S country's alone - an AI country's comes from its own state (WorldCalendar.TryExtraElection)
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id)
            {
                Debug.LogError($"CONFIDENCE: an extra election was ordered for {country?.Id.ToString() ?? "no country"}, not the player's country - refused: the player's extra-election date is not an AI country's to write ({why})");
                return;
            }
            _extraElectionOrderedOn = CurrentDate;
            _extraElectionDate = Elections.ConfidenceProcedure.RulesOf(country.Id) == Elections.ConfidenceProcedure.Rules.Parlamento
                ? Elections.ConfidenceProcedure.ItalianElectionDay(dissolvedOn ?? CurrentDate)   // PS-7 (K155): art. 61's seventy days from the dissolution, the last Sunday in them (K206: a dissolution the semestre bianco defers is dated the day it lifts)
                : Elections.ConfidenceProcedure.ExtraElectionDay(CurrentDate);
            // §640 (the reader): a run-up begun for another polling day is dropped - the extra election comes first, and that run-up re-begins on its own
            // calendar once the extra election is held; left standing it would be adopted by the wrong campaign. Stated: where the procedure resumes after
            // an election, the day's campaign step has already run by the time the order lands, so that run-up drew its day before it was dropped.
            if (PlayerPreCampaign != null && PlayerPreCampaign.Calendar.ElectionDate != _extraElectionDate) { PlayerPreCampaign = null; }
            if (CampaignRecord != null && CampaignRecord.ElectionDate != _extraElectionDate && (PlayerCampaign == null || PlayerCampaign.Setup.Calendar.ElectionDate != CampaignRecord.ElectionDate)) { CampaignRecord = null; }
            country.Government?.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: an extra election is ordered for {_extraElectionDate:yyyy-MM-dd} - {why}");
            Debug.Log($"CONFIDENCE: {country.Id} - an extra election on {_extraElectionDate:yyyy-MM-dd}: {why}");
        }

        private void AdvanceConfidenceDay(CountryId countryId)
        {
            if (_extraElectionDate != System.DateTime.MinValue && CurrentDate > _extraElectionDate)
            {
                // The extra election is held: the campaign state dated to it ends with it, so the ordinary election's run-up and campaign begin on their own
                // calendar rather than inheriting a record dated to the extra election (the reader, s636).
                if (CampaignRecord != null && CampaignRecord.ElectionDate == _extraElectionDate) { CampaignRecord = null; PlayerCampaign = null; PlayerCampaignResult = null; }
                if (PlayerPreCampaign != null && PlayerPreCampaign.Calendar.ElectionDate == _extraElectionDate) { PlayerPreCampaign = null; }
                _extraElectionDate = System.DateTime.MinValue; _extraElectionOrderedOn = System.DateTime.MinValue;
            }
            Country country = _world?.GetCountry(countryId);
            Elections.GovernmentRecord g = country?.Government;
            if (g == null) { return; }
            if (AdvanceSejmFormation(country, g)) { return; }   // AF-8 (q8): Poland's Art. 162 and 154 steps run their days (SimulationManager.Sejm.cs)
            if (AdvanceItalianMandates(country, g)) { return; }   // PS-7 (K204): Italy's mandates on the new chambers' first sitting (SimulationManager.Italy.cs)
            // §646: a round open runs its day; the day after an election a round opens (R2 - §640's resumption is this); a caretaker waits on either.
            if (OpenRoundAfterElection(country, g)) { return; }   // first: an election held while a round is open ends that round (the reader, §646)
            if (g.Round != null && g.Round.Open) { AdvanceSpeakerRound(country, g); return; }
            if (g.Caretaker) { return; }
            if (g.NoConfidenceOn == System.DateTime.MinValue)
            {
                // PS-3i-2a (§644): the agreements are read every day - a dial moved by a bill breaks its item the day it moves, not at the year's turn - and an
                // AI supporter past its tolerance withdraws before any motion is weighed.
                TrackAgreements(country);
                AiWithdrawals(country, g);
                TryAiMotion(country);   // PS-3i ruling (2), §641
                return;
            }
            if (CurrentDate < g.NoConfidenceOn.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { return; }
            DischargeAndRound(country);
        }

        /// <summary>
        /// PS-3i-2a (ruled 2026-09-26, §644): AN AI SUPPORTER WITHDRAWS ONCE ITS BROKEN AGREEMENT ITEMS PASS A TOLERANCE
        /// (<see cref="Elections.SupportAgreement.BrokenShareTolerated"/>, [AUTHORED-DRAFT]) - recorded on the government as the player's withdrawal is, and
        /// then in opposition: whether it moves no confidence is ruling (2)'s test, weighed apart. The player's own party withdraws by the player's verb.
        /// </summary>
        private void AiWithdrawals(Country country, Elections.GovernmentRecord g)
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id) { return; }
            foreach (string supporter in new List<string>(g.Support))
            {
                if (supporter == country.PlayerPartyAbbrev) { continue; }
                Elections.SupportAgreement agreement = g.AgreementOf(supporter);
                if (agreement == null || !agreement.PastTolerance()) { continue; }
                int broken = agreement.Count(Elections.AgreementState.Broken);
                g.WithdrawSupport(supporter, country);
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {supporter} withdrew its support over {broken} broken item(s) of its agreement's {agreement.Items.Count}, past the share it tolerates");
                Debug.Log($"AGREEMENT: {country.Id} - {supporter} withdrew its support over {broken} broken item(s)");
            }
        }

        /// <summary>
        /// PS-3i RULING (2) (Elias, 2026-09-25): AN AI PARTY MOVES NO CONFIDENCE ONLY WHEN THE MOTION WOULD CARRY AND THE MOVER PREFERS THE GOVERNMENT
        /// THE FORMATION MODEL SAYS WOULD FOLLOW - no doomed motions. A party that withdrew its support over broken agreement items is the natural mover
        /// (PS-3i-2a, §644: the withdrawal is its own step, past the tolerance; a party supporting a government moves nothing - the player's rule, §636):
        /// it is asked first. Then the opposition, largest first.
        /// <para>Each test is the model's own: the vote is <see cref="Elections.ConfidenceProcedure.Vote"/> (the mover for it, the government and its
        /// support against, the red-lined for it); what would follow is the Speaker's round the discharge runs - read as the formation's own government on the sitting chamber, which the round's first party asked proposes (§646; stated: the dated round can end elsewhere where the player is asked or offered a place) (<see cref="Elections.GovernmentFormation.ViewOfSitting"/>,
        /// no refusal line added for an AI mover, as §636's round adds none); the preference is the formation's payoff
        /// (<see cref="Elections.GovernmentFormation.PayoffIn"/>) - a mover is never in the cabinet it would bring down, so it prefers a round that seats
        /// it in the next cabinet. A round that forms nothing - the four proposals rejected, an extra election - is no government a mover can prefer (a
        /// premise, stated). Deterministic: no stream is drawn. The player's country only - the procedure's week, discharge and extra election are
        /// its alone (one pending extra election), and the player's own party moves by the player's verb; in an AI motion the player's party votes by
        /// the model's lines, as every party does.</para>
        /// <para>⚠ <b>Reachability (PS-3i-2a, §644).</b> Built as ruled: a supporter past its tolerance withdraws (<see cref="AiWithdrawals"/>), the
        /// agreements tracked daily, and a withdrawn party is the first mover asked. Measured and put to Elias: on the start's chamber the round reads
        /// the 2022 declarations (§607, §636), where M, KD and L will not sit with SD, so SD never prefers the successor before the 2026 election
        /// (PS-3i-2c, `AiMotionReachDiagnostic` prints the round by date under each reading); and a player-led breach of SD's agreement does not pass
        /// the chamber (PS-3i-2b).</para>
        /// <para>No motion is taken up in the week before the player's next polling day: its week would end on the far side of an election, and the round
        /// it was weighed against would never run (the reader). The player's refusals made by moving a motion stand in the round until the next
        /// election (<see cref="Elections.GovernmentRecord.StandingRefusals"/>), here as in the discharge.</para>
        /// </summary>
        private void TryAiMotion(Country country, string afterExitOf = null)
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id) { return; }
            // COAL-1 (K133; the review's M1): the Sejm's Art. 158 is taken up by the AI only after an exit - the daily weighing (ruling (2), §641) stays the Bundestag's
            // and the Riksdag's; Poland's daily AI constructive vote is unruled and unmeasured, so a player's Poland weighs none on a quiet day
            Elections.ConfidenceProcedure.Rules rules = Elections.ConfidenceProcedure.RulesOf(country.Id);
            if (rules == Elections.ConfidenceProcedure.Rules.Bundestag || (rules == Elections.ConfidenceProcedure.Rules.Sejm && afterExitOf != null)) { TryAiConstructiveVote(country, afterExitOf); return; }
            if (Elections.ConfidenceProcedure.RulesOf(country.Id) != Elections.ConfidenceProcedure.Rules.Riksdag) { return; }
            Elections.GovernmentRecord g = country.Government;
            if (g == null || g.Caretaker || g.NoConfidenceOn != System.DateTime.MinValue || g.Cabinet.Count == 0) { return; }
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate) { return; }
            if (TryPlayerPollingDay(out System.DateTime polling) && polling <= CurrentDate.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { return; }
            Elections.GovernmentFormation.View next = null;
            var nextCabinet = new List<string>();
            // COAL-1 (K131): after a partner's exit the DECLARED opposition moves - a party red-lined from the cabinet by its own lines (the vote's rule), never the
            // partner that left (K132: it votes on the motion by its own lines, ConfidenceProcedure.Vote, and moves nothing - a mover's side is "for" whatever its lines)
            HashSet<string> declared = afterExitOf != null ? Elections.GovernmentFormation.RedLinedFrom(country, g.Cabinet, CurrentDate) : null;
            foreach (string mover in AiMotionCandidates(country, g))
            {
                if (afterExitOf != null && (mover == afterExitOf || !declared.Contains(mover))) { continue; }
                if (!Elections.ConfidenceProcedure.CanBeTakenUp(country, mover, out int _, out int _)) { continue; }
                Elections.ConfidenceProcedure.MotionVote vote = Elections.ConfidenceProcedure.Vote(country, mover, CurrentDate);   // §653: the lines standing today
                if (!vote.Carried) { continue; }
                if (afterExitOf != null)
                {
                    // K131 (Elias, 2026-10-27): the s641 mover-preference gate does not apply after an exit - the declared opposition holds a majority, and it moves
                    country.Divisions.Append(vote.Title(), CurrentDate, 1f, true, 0f, (int)BillAxis.Fiscal, vote.Sides);
                    country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;
                    g.NoConfidenceOn = CurrentDate;
                    g.NoConfidenceMover = mover;
                    g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the Riksdag declared no confidence in the prime minister ({g.PmParty}), {vote.For} of {vote.Members} members, moved by {mover} after {afterExitOf} left the government - the declared opposition holds a majority (RF 13 kap. 4 §; COAL-1, K131)");
                    Debug.Log($"CONFIDENCE: {country.Id} - after {afterExitOf} left, an AI motion by {mover}: {vote.Title()}");
                    return;
                }
                if (next == null)
                {
                    next = Elections.GovernmentFormation.ViewOfSitting(country, Elections.GovernmentFormation.RefusalLines(country.Id, g.StandingRefusals), CurrentDate);   // the round does not depend on the mover; §653: the mid-term reading, as the discharge's round would read
                    if (next.HasGovernment) { foreach ((string abbrev, int _) in next.Cabinet) { nextCabinet.Add(abbrev); } }
                }
                if (!next.HasGovernment) { return; }
                double now = Elections.GovernmentFormation.PayoffIn(country, g.Cabinet, mover);   // zero by construction - a mover is never in the cabinet - kept so the test reads as the ruling's
                double after = Elections.GovernmentFormation.PayoffIn(country, nextCabinet, mover);
                if (after <= now + Elections.CoalitionFormation.DefectionMargin) { continue; }
                country.Divisions.Append(vote.Title(), CurrentDate, vote.Carried ? 1f : -1f, vote.Carried, 0f, (int)BillAxis.Fiscal, vote.Sides);
                country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;
                g.NoConfidenceOn = CurrentDate;
                g.NoConfidenceMover = mover;
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the Riksdag declared no confidence in the prime minister ({g.PmParty}), {vote.For} of {vote.Members} members, moved by {mover}, which the round would seat in {string.Join("+", nextCabinet)} (RF 13 kap. 4 §)");
                Debug.Log($"CONFIDENCE: {country.Id} - an AI motion by {mover}: {vote.Title()}; the round would form {string.Join("+", nextCabinet)}");
                return;
            }
        }

        // ---------------------------------------------------------------------------------------------
        // §698 (PS-4): THE CONSTRUCTIVE VOTE OF NO CONFIDENCE - Art. 67 GG (ConfidenceProcedure.Rules.Bundestag). The motion names its successor and is
        // that successor's election: the best government the formation would form with the mover leading (DraftProposal, on the mid-term reading, the
        // standing refusals kept), every party's answer under Germany's positive rule, and - elected by a majority of the members - the successor's
        // government installed the same day, the outgoing one gone without a caretaker, a week or a round. §754 (Elias's ruling A4): the vote is on the
        // PERSON (ConfidenceProcedure.ConstructiveVote, on the round's chamber); elected and his drafted government not holding, the successor governs with
        // his party and its parliamentary group alone (GroupAlone - Art. 63's rule for an elected candidate).
        // ---------------------------------------------------------------------------------------------

        /// <summary>§698: the successor a constructive vote by <paramref name="mover"/> would elect, and every party's answer to its government.</summary>
        private Elections.FormationProposal DraftSuccessor(Country country, string mover, out Elections.ProposalVerdict verdict, out Elections.SpeakerRound draftRound)
        {
            var round = new Elections.SpeakerRound { MidTerm = true, OpenedOn = CurrentDate, Occasion = "the constructive vote of no confidence" };
            if (country.Government != null)
            {
                round.Refusals.AddRange(country.Government.StandingRefusals);
                // the chancellor's own party neither sits in nor carries the government elected against its chancellor - it votes against (ConstructiveVote)
                if (!string.IsNullOrEmpty(country.Government.PmParty) && country.Government.PmParty != mover) { round.Refusals.Add(country.Government.PmParty + ">" + mover); }
            }
            // The review's defect 1: an AI mover's successor never seats the player's party - nobody asked it (the Speaker's round OFFERS a place to the
            // player instead); a decline keeps it out of the draft's cabinet and support, and it votes on the person - by its lines and its compatibility (§754).
            // COAL-1 (the review's M2; SP-7b, I14 (2a)): while the player is the President his party is the AI's - asked like any party, never declined
            string playersParty = PartyInChamber(country);
            if (!string.IsNullOrEmpty(playersParty) && playersParty != mover) { round.Declines.Add(playersParty + ">" + mover); }
            Elections.FormationProposal proposal = DraftProposal(country, round, mover);
            verdict = Elections.Formateur.Answer(country, proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), playersParty);
            // §705: a partner of the sitting cabinet that the draft seats in the successor's cabinet and that stays where it sits (the review's defect 3)
            // refuses the mover - the mover drafts again without it, as a formateur would: the next government that holds. Before §705 the CDU could
            // stand alone with the CSU outside; one Fraktion, its best draft on §698's planted chamber seats the FDP, which stays - and the motion failed
            // with the CDU and CSU holding a majority of their own. Bounded: each pass refuses at least one more party.
            for (int pass = 0; pass < 4 && country.Government != null; pass++)
            {
                Elections.CoalitionFormation.Chamber trialChamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> trialParties);
                Elections.ConfidenceProcedure.MotionVote trial = Elections.ConfidenceProcedure.ConstructiveVote(country, mover, proposal, verdict, trialChamber, trialParties);   // only its refusers are read here
                var staying = new List<string>();
                foreach (string p in proposal.CabinetParties) { if (p != mover && trial.Refusers.Contains(p) && country.Government.Cabinet.Contains(p) && !round.Refusals.Contains(p + ">" + mover)) { staying.Add(p); } }
                if (staying.Count == 0) { break; }
                foreach (string p in staying) { round.Refusals.Add(p + ">" + mover); }
                proposal = DraftProposal(country, round, mover);
                verdict = Elections.Formateur.Answer(country, proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), playersParty);
            }
            draftRound = round;
            return proposal;
        }

        /// <summary>§698: the constructive vote <paramref name="mover"/> would win or lose today - nothing recorded. §754 (Elias's ruling A4): counted on the
        /// person, on the chamber the successor was drafted on (its reading and lines, the standing refusals kept); <paramref name="successor"/> is the
        /// government the elected successor forms - the drafted one where it holds, else his party and its parliamentary group alone (<see cref="GroupAlone"/>,
        /// the rule Art. 63 applies to an elected candidate whose government does not hold).</summary>
        private Elections.ConfidenceProcedure.MotionVote ConstructiveVoteOf(Country country, string mover, out Elections.FormationProposal successor, out Elections.ProposalVerdict verdict)
        {
            successor = DraftSuccessor(country, mover, out verdict, out Elections.SpeakerRound round);
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
            Elections.ConfidenceProcedure.MotionVote vote = Elections.ConfidenceProcedure.ConstructiveVote(country, mover, successor, verdict, chamber, parties);
            if (!vote.PartnersAccept)
            {
                // drafted, not holding: the government he would form if elected (the review's note - the slip names it whether or not the count reaches it);
                // none drawable (latent - GroupAlone's proposal is always well-formed) and no successor is installed
                Elections.FormationProposal alone = GroupAlone(country, round, mover, out Elections.ProposalVerdict aloneVerdict, out bool seatedByGroup);
                successor = aloneVerdict?.Investiture != null ? alone : null;
                verdict = aloneVerdict;
                vote.SeatedByGroup = successor != null && seatedByGroup;
            }
            if (successor != null) { vote.Governing.AddRange(successor.CabinetParties); }
            return vote;
        }

        /// <summary>§698: the constructive vote the player's party would win or lose today - projected, nothing recorded (the Parliament page's row reads it).</summary>
        public Elections.ConfidenceProcedure.MotionVote ProjectConstructiveVote(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || Elections.ConfidenceProcedure.RulesOf(countryId) != Elections.ConfidenceProcedure.Rules.Bundestag) { return null; }
            return ConstructiveVoteOf(country, country.PlayerPartyAbbrev, out _, out _);
        }

        /// <summary>§698: Art. 67 (1) - "Der Bundespräsident muß dem Ersuchen entsprechen und den Gewählten ernennen": the successor's government takes office at once.</summary>
        private void InstallSuccessor(Country country, Elections.FormationProposal proposal, Elections.ProposalVerdict verdict, Elections.ConfidenceProcedure.MotionVote vote)
        {
            Elections.GovernmentRecord g = country.Government;
            var supporters = new List<string>();
            foreach (Elections.PartyAnswer answer in verdict.Answers) { if (!answer.InCabinet && answer.Accepts && !vote.Refusers.Contains(answer.Party)) { supporters.Add(answer.Party); } }   // a refusing supporter carries nothing
            bool sejm = Elections.ConfidenceProcedure.RulesOf(country.Id) == Elections.ConfidenceProcedure.Rules.Sejm;   // COAL-1 (K133): Art. 158 - the President accepts the cabinet's resignation and appoints the prime minister the Sejm chose
            Elections.GovernmentRecord formed = Elections.GovernmentRecord.FromProposal(country, proposal, supporters, verdict.Investiture.Kind, CurrentDate,
                sejm ? $"the constructive vote of no confidence: the Sejm chose {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} candidate prime minister, {vote.For} of {vote.Members} deputies (Art. 158 Konstytucja)"
                    : $"the constructive vote of no confidence: the Bundestag elected {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} candidate chancellor, {vote.For} of {vote.Members} members (Art. 67 GG)",
                g.Kind, g.Executive, g.StandingRefusals, _world);
            formed.Breaks.Add(sejm ? $"{CurrentDate:yyyy-MM-dd}: the Sejm expressed no confidence in the Council of Ministers and chose {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} candidate prime minister, {vote.For} of {vote.Members} deputies; {PoliSim.Wording.EnglishText.Possessive(g.PmParty, false, true)} cabinet's resignation accepted (Art. 158 Konstytucja)"
                : $"{CurrentDate:yyyy-MM-dd}: the Bundestag elected {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} candidate chancellor by the constructive vote, {vote.For} of {vote.Members} members; {PoliSim.Wording.EnglishText.Possessive(g.PmParty, false, true)} chancellor dismissed (Art. 67 GG)");
            if (vote.SeatedByGroup) { formed.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} sits in {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} cabinet as its parliamentary group's partner - a group votes and governs as one (the game's premise)"); }   // §754
            // COAL-1 (the review's L5): a budget the Sejm adopted and the President holds stays on his docket through an Art. 158 change - it is the Sejm's act, not
            // the cabinet's (Art. 158 discharges the Council of Ministers, not the Sejm's statutes), and no install path voids a held act (the election's, the Sejm's steps)
            if (sejm && _pendingBudgetBillByCountry.TryGetValue(country.Id, out BudgetBill held) && held.ActsHeld != null) { formed.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the budget the Sejm adopted stays before the President - its acts are the Sejm's, and the change of cabinet does not withdraw them"); }
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
            country.Government = formed;
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the chairs the vote hands the player are seated at the average
            ResetArrivalBudgetWindow(country.Id);
            CloseBudgetWindowIfNotGoverning(country);
            Debug.Log($"CONFIDENCE: {country.Id} - the constructive vote carried: {string.Join("+", formed.Cabinet)} led by {formed.PmParty}{(formed.Support.Count > 0 ? " with " + string.Join("+", formed.Support) : string.Empty)} takes office");
        }

        /// <summary>
        /// §698 (the review's defect 2): a government change that leaves the player out of the chancellery closes the player's open budget window - the
        /// window is the governing party's to use; left open, it held the clock for good (the budget-window hold) or, the hold off, barred the new
        /// government's own budget (<see cref="TryOpenBudgetProcess"/> returns while one is open). Called where this manager installs a government; the
        /// controller's election-night install resets the arrival window without it (PF-19).
        /// </summary>
        private void CloseBudgetWindowIfNotGoverning(Country country)
        {
            if (PlayerGoverns(country)) { return; }
            _pendingBudgetProcessByCountry.Remove(country.Id);
            _incomingBudgetWindowOpenNow.Remove(country.Id);
        }

        /// <summary>
        /// §698: ruling (2)'s rule under Art. 67 - an AI party moves a constructive vote only where it would carry and the mover prefers the government it
        /// would lead (the formation's payoff), in <see cref="AiMotionCandidates"/>' order; none in the week before the player's next polling day (the
        /// Riksdag branch's reader, kept). Deterministic: no stream is drawn.
        /// </summary>
        private void TryAiConstructiveVote(Country country, string afterExitOf = null)
        {
            Elections.GovernmentRecord g = country.Government;
            if (g == null || g.Caretaker || g.Cabinet.Count == 0) { return; }
            if (g.Sejm != null && g.Sejm.Open) { return; }   // COAL-1 (K133): the Sejm's formation runs - Art. 154, not Art. 158
            if ((g.Round != null && g.Round.Open) || ElectionAwaitsRound(country, g)) { return; }   // §705 (the review's defect 5, and its second pass A): the chancellor's election runs - Art. 63, not Art. 67
            if (TryPlayerPollingDay(out System.DateTime polling) && polling <= CurrentDate.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { return; }
            // AF-8 (Elias's ruling q8, 2026-10-18: "a premise ... without a statute is sourced or cut"): THE MONDAY CADENCE IS CUT - the AI weighs a constructive
            // vote every day. It was §698's performance premise (Mondays and the day after a formation), backed by no statute and sourced by nothing; its cost
            // is the one the review measured then, a formation per candidate each German day (about 12 ms a day: the German day path's check 1.7 s -> 3.0 s
            // over 111 days). Stateless as before, so a loaded game weighs on the days a continuous one does. The s873 review's M2: every ticking fork weighs too,
            // so a day whose inputs equal the last quiet day's is not weighed again (SimulationManager.AiVoteMemo.cs - the key is everything the weighing reads).
            // COAL-1 (K131): the weighing after an exit is not the quiet day's - its movers and its gate differ - so it neither reads nor writes the memo
            string memoKey = null;
            if (afterExitOf == null && AiVoteMemoHolds(country, g, out memoKey)) { return; }
            // K131 (the review's M3): after an exit the DECLARED opposition moves - a party red-lined from the cabinet by its own lines, as under the Riksdag's
            // rule - never the partner that left (K132: it votes on the successor by its own lines, ConstructiveVote, and moves nothing)
            HashSet<string> declared = afterExitOf != null ? Elections.GovernmentFormation.RedLinedFrom(country, g.Cabinet, CurrentDate) : null;
            foreach (string mover in AiMotionCandidates(country, g))
            {
                if (SeatsOf(country, mover) <= 0) { continue; }   // a party with no members moves nothing
                if (afterExitOf != null && (mover == afterExitOf || !declared.Contains(mover))) { continue; }
                Elections.ConfidenceProcedure.MotionVote vote = ConstructiveVoteOf(country, mover, out Elections.FormationProposal successor, out Elections.ProposalVerdict verdict);
                if (!vote.Carried || successor == null) { continue; }
                double now = Elections.GovernmentFormation.PayoffIn(country, g.Cabinet, mover);
                double after = Elections.GovernmentFormation.PayoffIn(country, successor.CabinetParties, mover);
                if (afterExitOf == null && after <= now + Elections.CoalitionFormation.DefectionMargin) { continue; }   // K131: the s641 gate does not apply after an exit
                country.Divisions.Append(vote.Title(), CurrentDate, 1f, true, 0f, (int)BillAxis.Fiscal, vote.Sides);
                country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;
                Debug.Log($"CONFIDENCE: {country.Id} - {(afterExitOf != null ? "after " + afterExitOf + " left, " : string.Empty)}an AI constructive vote by {mover}: {vote.Title()}");
                InstallSuccessor(country, successor, verdict, vote);
                return;
            }
            if (afterExitOf == null) { AiVoteMemoNothing(g, memoKey); }
        }

        /// <summary>Ruling (2)'s order: the parties that withdrew over broken agreement items first (the natural movers), then the rest of the opposition by
        /// seats; never a cabinet party, never a supporter (a supporter moves nothing - §644: it withdraws first, past its tolerance), never the player's.</summary>
        private static List<string> AiMotionCandidates(Country country, Elections.GovernmentRecord g)
        {
            var aggrieved = new List<string>();
            var opposition = new List<string>();
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                string key = party.Abbrev;
                if (key == PartyInChamber(country) || g.Cabinet.Contains(key) || g.Support.Contains(key)) { continue; }   // COAL-1 (M2): a player-president's party is the AI's (SP-7b)
                Elections.SupportAgreement a = g.AgreementOf(key);
                if (a != null && a.Count(Elections.AgreementState.Broken) > 0) { aggrieved.Add(key); } else { opposition.Add(key); }
            }
            int Seats(string k) => country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(k, out int s) ? s : 0;
            aggrieved.Sort((a, b) => Seats(b).CompareTo(Seats(a)) != 0 ? Seats(b).CompareTo(Seats(a)) : string.CompareOrdinal(a, b));
            opposition.Sort((a, b) => Seats(b).CompareTo(Seats(a)) != 0 ? Seats(b).CompareTo(Seats(a)) : string.CompareOrdinal(a, b));
            aggrieved.AddRange(opposition);
            // §705: a parliamentary group moves once, by its larger member - the CSU moves no motion of its own beside the CDU's (one Fraktion,
            // § 10 Abs. 1 GO-BT); left in, its draft seated the CDU+CSU under a CSU chancellor once the CDU's own motion failed
            foreach ((string a, string b) in Elections.ChamberRules.JointGroups(country.Id))
            {
                if (aggrieved.Contains(a) && aggrieved.Contains(b)) { aggrieved.Remove(Seats(a) >= Seats(b) ? b : a); }
            }
            return aggrieved;
        }

        // =============================================================================================================================================
        // §646 THE SPEAKER'S ROUND (POLITICAL_SYSTEM_SPEC.md §5.3, premises 4-8 and the builder's R1-R8): the formation as a dated procedure in the
        // player's country - who the Speaker asks, the proposal and its investiture, the proposals the chamber rejects to RF 6:5's limit.
        // =============================================================================================================================================

        /// <summary>The round open in a country, or null.</summary>
        public Elections.SpeakerRound RoundOf(CountryId countryId)
        {
            Elections.SpeakerRound round = _world?.GetCountry(countryId)?.Government?.Round;
            return round != null && round.Open ? round : null;
        }

        /// <summary>Whether a Speaker's round runs in this country - the player's, where its confidence rules are sourced (R1).</summary>
        public bool RoundsApply(CountryId countryId) => PlayerCountryId.HasValue && PlayerCountryId.Value == countryId
            && (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Riksdag
                || Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Bundestag);   // §705: the Bundestag's chancellor election (Art. 63 GG)

        /// <summary>§705: whether a country's round is the Bundestag's chancellor election (Art. 63 GG) rather than the Riksdag's Speaker's round.</summary>
        private static bool IsBundestag(CountryId countryId) => Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Bundestag;

        /// <summary>PS-8 (L-N1 of the s938 review): a round's SPEAKER: line - an AI country's round (an AI Bundestag past its first ballot, `ContinueChancellorElection`)
        /// is silent in a counterfactual world, as the AI's other calendar lines are; the player's round logs as it always has.</summary>
        private void SpeakerLog(Country country, string line)
        {
            if (IsCounterfactual && !RoundsApply(country.Id)) { return; }
            Debug.Log(line);
        }

        /// <summary>
        /// Premise 6: THE SPEAKER'S ORDER on a chamber and a vintage's declarations - the formation's prime-minister party (a declared candidacy in its
        /// cabinet, else its largest party), then every other party with a declared candidate, largest first; where neither exists, the largest party.
        /// </summary>
        public List<string> SpeakerOrder(Country country, ElectionVintage vintage, IReadOnlyList<Elections.RedLine> extraLines = null) =>
            SpeakerOrder(country, Elections.DeclarationReading.OfVintage(vintage), extraLines);

        /// <summary>Premise 6 under a named reading (§653): an election's, or the mid-term one.</summary>
        public List<string> SpeakerOrder(Country country, Elections.DeclarationReading reading, IReadOnlyList<Elections.RedLine> extraLines = null)
        {
            var order = new List<string>();
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, reading, extraLines, out IReadOnlyList<PoliticalParty> parties);
            Elections.CoalitionResult formation = Elections.CoalitionFormation.Form(chamber.Seats, chamber.Compatibility, chamber.Lines, chamber.NegativeRule, chamber.Rules, chamber.Joint);
            if (formation.Outcome != Elections.CoalitionOutcomeKind.NewElection) { order.Add(PmOf(country, reading, parties, formation.Government.Cabinet)); }
            var declared = new List<string>();
            foreach ((string abbrev, string _, string _) in reading.Candidacies(country.Id)) { if (!order.Contains(abbrev)) { declared.Add(abbrev); } }
            declared.Sort((a, b) => SeatsOf(country, b).CompareTo(SeatsOf(country, a)) != 0 ? SeatsOf(country, b).CompareTo(SeatsOf(country, a)) : string.CompareOrdinal(a, b));
            order.AddRange(declared);
            if (order.Count == 0)
            {
                string largest = null;
                foreach (PoliticalParty p in parties) { if (largest == null || SeatsOf(country, p.Abbrev) > SeatsOf(country, largest)) { largest = p.Abbrev; } }
                if (largest != null) { order.Add(largest); }
            }
            return order;
        }

        private static int SeatsOf(Country country, string key) => country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(key, out int s) ? s : 0;

        /// <summary>K-1f's premise, as <see cref="Elections.GovernmentRecord.FromView"/> reads it: a declared candidacy standing in the cabinet leads it; else its largest party.</summary>
        private static string PmOf(Country country, Elections.DeclarationReading reading, IReadOnlyList<PoliticalParty> parties, int cabinet)
        {
            // §705: where a cabinet holds more than one declared candidacy - a German coalition may (a Kanzlerkandidatur draws no line, unlike
            // Sweden's) - the candidacy of its largest party leads it: the CDU's Merz in CDU+CSU+SPD, not the SPD's Scholz. Sweden's pairing rule
            // keeps a Swedish cabinet to one, so nothing there changes.
            string led = null;
            foreach ((string abbrev, string _, string _) in reading.Candidacies(country.Id))
            {
                for (int p = 0; p < parties.Count; p++)
                {
                    if ((cabinet & (1 << p)) != 0 && parties[p].Abbrev == abbrev && (led == null || GroupSeats(country, abbrev) > GroupSeats(country, led))) { led = abbrev; }   // §705: the Fraktion's seats - the Union's candidate stands on the CDU and the CSU
                }
            }
            if (led != null) { return led; }
            string largest = null;
            for (int p = 0; p < parties.Count; p++) { if ((cabinet & (1 << p)) != 0 && (largest == null || SeatsOf(country, parties[p].Abbrev) > SeatsOf(country, largest))) { largest = parties[p].Abbrev; } }
            return largest;
        }

        /// <summary>PS-3i-2c (ruled 2026-09-29, §653): what a round reads - after an election, everything dated at its polling day; mid-term, the lines and
        /// candidacies standing today with the platforms held to the sitting chamber's election; a vintage alone where a check opens one on a vintage.</summary>
        public Elections.DeclarationReading RoundReading(Country country, Elections.SpeakerRound round) =>
            round.MidTerm ? Elections.GovernmentFormation.SittingReading(country, CurrentDate)
            : round.ReadsOn != System.DateTime.MinValue ? Elections.DeclarationReading.OfElection(country.Id, round.ReadsOn)
            : Elections.DeclarationReading.OfVintage(round.Vintage);

        /// <summary>The refusal lines that stand in a round: §641's standing refusals and the player's declines (premise 5).</summary>
        private static List<Elections.RedLine> RoundLines(Country country, Elections.SpeakerRound round)
        {
            List<Elections.RedLine> lines = Elections.GovernmentFormation.RefusalLines(country.Id, round.Refusals);
            return lines;
        }

        /// <summary>
        /// R4: THE PROPOSAL A PARTY MAKES WHEN ASKED - the best government the formation would form with that party leading (Gamson's posts, its
        /// supporters' tabled demands accepted); with none, the party alone. Also the formation sheet's first draft when the player is asked.
        /// </summary>
        public Elections.FormationProposal DraftProposal(Country country, Elections.SpeakerRound round, string party)
        {
            Elections.DeclarationReading reading = RoundReading(country, round);
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, reading, RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
            Elections.CoalitionResult formation = Elections.CoalitionFormation.Form(chamber.Seats, chamber.Compatibility, chamber.Lines, chamber.NegativeRule, chamber.Rules, chamber.Joint);
            var proposal = new Elections.FormationProposal { Formateur = party };
            // The governments that would hold (the answers compare against the same set, §646 the reader), best first.
            foreach (Elections.GovernmentOption g in Elections.CoalitionFormation.Holding(formation, chamber.Seats, chamber.Compatibility))
            {
                if (PmOf(country, reading, parties, g.Cabinet) != party) { continue; }
                // Premise 5 (the second reader): a party that declined this formateur's offer stays in OPPOSITION - out of its cabinet and its support, its
                // vote at the investiture the model's, as any opposition party's; never a line that would make it vote against.
                string declined = round.Declines.Contains(country.PlayerPartyAbbrev + ">" + party) ? country.PlayerPartyAbbrev : null;
                bool seatsDecliner = false;
                for (int p = 0; p < parties.Count; p++) { if ((g.Cabinet & (1 << p)) != 0 && parties[p].Abbrev == declined) { seatsDecliner = true; } }
                if (seatsDecliner) { continue; }
                for (int p = 0; p < parties.Count; p++)
                {
                    if ((g.Cabinet & (1 << p)) != 0) { proposal.CabinetParties.Add(parties[p].Abbrev); }
                    else if ((g.Support & (1 << p)) != 0 && parties[p].Abbrev != declined) { proposal.Supporters.Add(parties[p].Abbrev); }
                }
                break;
            }
            if (proposal.CabinetParties.Count == 0)
            {
                proposal.CabinetParties.Add(party);
                // §705: alone, a party still sits with its parliamentary group - the CDU's cabinet holds the CSU (one Fraktion, § 10 Abs. 1 GO-BT);
                // without it the cabinet splits the group and no chamber could elect it
                // (the review's latent case: a partner that is the player's party and declined this formateur stays out - the cabinet then splits the
                // group and cannot be elected, which is the rule: the Union forms together or not at all)
                if (SeatedGroupPartner(country, party) is string partner && !round.Declines.Contains(partner + ">" + party)) { proposal.CabinetParties.Add(partner); }
            }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in Elections.GovernmentRecord.GamsonPosts(country, proposal.CabinetParties, party, CurrentDate)) { proposal.Posts[kv.Key] = new List<CabinetPortfolio>(kv.Value); }
            proposal.FreezeTabled(country, CurrentDate, _world);
            foreach (string supporter in proposal.Supporters)
            {
                var keys = new List<string>();
                foreach (Elections.AgreementItem item in proposal.TabledOf(country, supporter, CurrentDate, _world)) { keys.Add(Elections.SupportAgreement.KeyOf(item)); }
                proposal.AcceptedDemands[supporter] = keys;
            }
            return proposal;
        }

        /// <summary>R2: open a round - the outgoing government serves on as a caretaker (premise 8), and the Speaker asks the first party in the order.</summary>
        public void OpenSpeakerRound(Country country, ElectionVintage vintage, string occasion, IEnumerable<string> refusals = null, System.DateTime? electionDay = null, bool midTerm = false)
        {
            Elections.GovernmentRecord g = country.Government;
            if (g == null) { return; }
            bool bundestag = IsBundestag(country.Id);
            // §705: the Bundestag's outgoing government is not discharged by the election - its office ends when the new Bundestag convenes (Art. 69
            // Abs. 2 GG), and it serves on from then at the Bundespräsident's request (Abs. 3). The Riksdag's serves on as a caretaker at once, and
            // so does a German one whose round opens with no election (the review's latent case: a sitting Bundestag, the office already ended).
            if (!g.Caretaker && !(bundestag && electionDay.HasValue))
            {
                g.Discharge(CurrentDate);
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the government serves on as a caretaker through the Speaker's round {occasion}");
            }
            var round = new Elections.SpeakerRound { OpenedOn = CurrentDate, Occasion = occasion, Vintage = vintage, Stage = Elections.RoundStage.Consulting,
                ReadsOn = electionDay?.Date ?? System.DateTime.MinValue, MidTerm = midTerm && !electionDay.HasValue };
            if (bundestag) { round.Convenes = electionDay.HasValue ? electionDay.Value.Date.AddDays(Elections.SpeakerRound.BundestagConvenesWithinDays) : CurrentDate.Date; round.Phase = 1; }   // a sitting Bundestag has convened
            if (refusals != null) { round.Refusals.AddRange(refusals); }
            round.Order = SpeakerOrder(country, RoundReading(country, round), RoundLines(country, round));
            g.Round = round;
            round.Log.Add(bundestag
                ? $"{CurrentDate:yyyy-MM-dd}: the chancellor's election opens {occasion} (Art. 63 GG) - {(electionDay.HasValue ? "the new Bundestag convenes by " + round.Convenes.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + " (Art. 39 Abs. 2)" : "the Bundestag sits")}; the Bundespräsident's order is {string.Join(", ", round.Order)}"
                : $"{CurrentDate:yyyy-MM-dd}: the Speaker's round opens {occasion}; the order is {string.Join(", ", round.Order)}");
            // §714 (Elias's ruling of 2026-10-01, item 5): after an election the formation takes the country's time on record, polling day to installation;
            // no proposal comes to its vote before it runs out, and the outgoing government governs meanwhile as a caretaker
            if (electionDay.HasValue && Elections.WorldClock.FormationDays(country.Id, out List<string> formations) is int formationDays)
            {
                round.FormationDue = electionDay.Value.Date.AddDays(formationDays);
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the formation takes {formationDays} days, the median of the record's ({string.Join("; ", formations)}) - no vote before {round.FormationDue:yyyy-MM-dd}; the outgoing government governs meanwhile");
            }
            AskNext(country, round);
        }

        /// <summary>
        /// §705 (the review's second pass, A): an election held in a round country that no round has followed yet - the polling day itself, before the
        /// round opens the next day. A constructive vote moved then was taken on the new chamber's seats by the old Bundestag, and the government it
        /// installed, dated the polling day, read as the election's own - so no chancellor's election opened and the convening discharged nothing.
        /// </summary>
        public bool ElectionAwaitsRound(CountryId countryId) { Country country = _world?.GetCountry(countryId); return country?.Government != null && ElectionAwaitsRound(country, country.Government); }

        private bool ElectionAwaitsRound(Country country, Elections.GovernmentRecord g)
        {
            if (!RoundsApply(country.Id) || country.ElectionHistory == null) { return false; }
            System.DateTime held = System.DateTime.MinValue;
            foreach (Elections.ElectionRecord e in country.ElectionHistory) { if (Elections.NationalElection.CountsParliament(e.Method) && e.Date <= CurrentDate && e.Date > held) { held = e.Date; } }
            return held != System.DateTime.MinValue && held > g.FormedOn && held > g.ProcedureResumedAfter;
        }

        /// <summary>§705: whether a round is the Bundestag's chancellor election - it carries the day the new Bundestag convenes.</summary>
        private static bool IsBundestagRound(Elections.SpeakerRound round) => round.Bundestag;

        /// <summary>§705: the formateur's chancellor candidate by name, where its candidacy is declared; else "the formateur's candidate".</summary>
        private string CandidateOf(Country country, Elections.SpeakerRound round, string party)
        {
            foreach ((string abbrev, string candidate, string _) in RoundReading(country, round).Candidacies(country.Id)) { if (abbrev == party) { return candidate + " (" + party + ")"; } }
            return PoliSim.Wording.EnglishText.Possessive(party, false, true) + " candidate";
        }

        private void AskNext(Country country, Elections.SpeakerRound round)
        {
            round.Turn++;
            round.Asked = round.Order.Count > 0 ? round.Order[round.Turn % round.Order.Count] : null;
            if (IsBundestagRound(round) && round.Phase >= 2 && round.Asked != null)
            {
                // §706 (GO-BT § 4 Abs. 2): in the fourteen days a candidate stands only on a nomination signed by a quarter of the members, or by a
                // Fraktion of a quarter - a party whose nomination stands; none standing, the fourteen days run out unballoted
                Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
                List<string> standing = Nominated(country, round, chamber, parties, pluralityBallot: false, out string how);
                // the review of §706 (defect 1): a nomination stands once in the fourteen days - one that has stood, or a party that passed, is not
                // asked again (a pass re-asked the player's party on the same day, and the clock held on it for good)
                standing.RemoveAll(p => round.StoodInPhase2.Contains(p));
                if (standing.Count == 0 && !how.Contains("no nomination")) { how = "every nomination signed by a quarter of the members has stood or passed (GO-BT § 4 Abs. 2)"; }
                // §712 (the review's defect 3): the player's nomination, where it stands, is asked FIRST - it tables or passes before any ballot is called,
                // so every nomination meets in the one ballot (else an AI's ballot left it out and it was balloted alone a week later, on other votes).
                // §715 (item 6): otherwise the first nomination standing - from every party seated, largest Fraktion first; the Bundespräsident's order
                // is phase 1's alone (a party outside it, the Linke's, may nominate here)
                round.Asked = !string.IsNullOrEmpty(country.PlayerPartyAbbrev) && standing.Contains(country.PlayerPartyAbbrev) ? country.PlayerPartyAbbrev
                    : standing.Count > 0 ? standing[0] : null;
                if (!standing.Contains(round.Asked))
                {
                    round.Asked = null;
                    round.Proposal = null;
                    round.Stage = Elections.RoundStage.Consulting;
                    round.AskedOn = CurrentDate;
                    round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {how}; the fourteen days run to {round.SecondPhaseUntil:yyyy-MM-dd} with no candidate before the Bundestag");
                    SpeakerLog(country, $"SPEAKER: {country.Id} - the chancellor's election, phase 2: {how}");
                    return;
                }
            }
            if (round.Asked == null)
            {
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: no party to ask");
                // PS-8 (L-N2 of the s938 review): an AI country's round ends here with the outgoing government serving on - BreakOff orders the PLAYER'S extra election
                if (!RoundsApply(country.Id)) { round.Stage = Elections.RoundStage.Concluded; round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the round ends; the outgoing government serves on"); return; }
                BreakOff(country, country.Government, round);
                return;
            }   // no party to ask: the procedure breaks off (unreachable where the order falls back to the largest party)
            round.AskedOn = CurrentDate;
            round.Proposal = null;
            round.Stage = round.Asked == country.PlayerPartyAbbrev ? Elections.RoundStage.PlayerAsked : Elections.RoundStage.Consulting;
            round.Log.Add(IsBundestagRound(round)
                ? $"{CurrentDate:yyyy-MM-dd}: {(round.Phase <= 1 ? "the Bundespräsident will propose " + CandidateOf(country, round, round.Asked) + " (Art. 63 Abs. 1)" : "the Bundestag weighs " + CandidateOf(country, round, round.Asked) + " (Art. 63 Abs. 3)")}; {round.Asked} forms the government it would lead{(round.Stage == Elections.RoundStage.PlayerAsked ? " - the player's party" : string.Empty)}"
                : $"{CurrentDate:yyyy-MM-dd}: the Speaker asks {round.Asked} to form a government{(round.Stage == Elections.RoundStage.PlayerAsked ? " - the player's party" : string.Empty)}");
            SpeakerLog(country, $"SPEAKER: {country.Id} - {(IsBundestagRound(round) ? "the chancellor's election, phase " + round.Phase + ": " : "the Speaker asks ")}{round.Asked} ({round.Stage})");
        }

        private void Table(Elections.SpeakerRound round, Elections.FormationProposal proposal)
        {
            round.Proposal = proposal;
            round.Stage = Elections.RoundStage.VotePending;
            if (IsBundestagRound(round))
            {
                // §705: the Bundestag elects "ohne Aussprache" (Art. 63 Abs. 1) and sets no delay - the ballot the day the candidate stands, but never
                // before the new Bundestag convenes: the chancellor is the new chamber's to elect
                round.VoteOn = round.VoteDayIfTabled(CurrentDate);   // §714: not before it convenes nor before the formation's day (SpeakerRound.VoteDayIfTabled, the one rule)
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {proposal.Formateur} forms {string.Join("+", proposal.CabinetParties)}{(proposal.Supporters.Count > 0 ? " with " + string.Join("+", proposal.Supporters) : string.Empty)}; the Bundestag elects the chancellor on {round.VoteOn:yyyy-MM-dd} (Art. 63 GG)");
                return;
            }
            round.VoteOn = round.VoteDayIfTabled(CurrentDate);   // §714: the fourth day after the Speaker submits it (RF 6 kap. 4 §), not before the formation's day
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {proposal.Formateur} tables {string.Join("+", proposal.CabinetParties)}{(proposal.Supporters.Count > 0 ? " with " + string.Join("+", proposal.Supporters) : string.Empty)}; {(round.VoteOn > CurrentDate.AddDays(Elections.SpeakerRound.VoteDays) ? "the Speaker submits it on " + round.VoteOn.AddDays(-Elections.SpeakerRound.VoteDays).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + ", once the formation's time on record has run, and " : string.Empty)}the Riksdag votes on {round.VoteOn:yyyy-MM-dd} (RF 6 kap. 4 §)");
        }

        /// <summary>§705 (the review's defect 4): tabled, a proposal comes to its vote - in a convened Bundestag the same day ("ohne Aussprache",
        /// Art. 63 Abs. 1): a vote left to the next day tick fell outside the fourteen days when tabled on their last, and the desk says THE SAME DAY.</summary>
        private void TableAndVote(Country country, Elections.SpeakerRound round, Elections.FormationProposal proposal)
        {
            Table(round, proposal);
            if (IsBundestagRound(round) && CurrentDate >= round.VoteOn && country.Government?.Round == round) { Investiture(country, country.Government, round); }
        }

        private static bool Involves(Elections.FormationProposal proposal, string party) => !string.IsNullOrEmpty(party) && (proposal.CabinetParties.Contains(party) || proposal.Supporters.Contains(party));

        /// <summary>The round's day: the consultation ends in a proposal, a tabled proposal comes to its vote; the player's turns wait on the player.</summary>
        private void AdvanceSpeakerRound(Country country, Elections.GovernmentRecord g)
        {
            Elections.SpeakerRound round = g.Round;
            if (IsBundestagRound(round))
            {
                // §705: the new Bundestag convenes - the outgoing government's office ends (Art. 69 Abs. 2 GG) and it serves on at the Bundespräsident's
                // request until a chancellor is elected (Abs. 3)
                if (!g.Caretaker && CurrentDate >= round.Convenes)
                {
                    g.Discharge(round.Convenes);
                    g.Breaks.Add($"{round.Convenes:yyyy-MM-dd}: the new Bundestag convenes - the government's office ends (Art. 69 Abs. 2 GG); it serves on until a chancellor is elected (Abs. 3)");
                    round.Log.Add($"{round.Convenes:yyyy-MM-dd}: the new Bundestag convenes; the outgoing government serves on (Art. 69 Abs. 2-3 GG)");
                }
                // Art. 63 Abs. 4: the fourteen days pass with no chancellor elected - at once, a ballot the most votes win
                if (round.Phase == 2 && CurrentDate > round.SecondPhaseUntil && round.Stage != Elections.RoundStage.VotePending) { PluralityBallot(country, g, round); return; }
            }
            switch (round.Stage)
            {
                case Elections.RoundStage.Consulting:
                    if (round.Asked == null) { return; }   // §706: no nomination stands (GO-BT § 4 Abs. 2) - the fourteen days run out, then the ballot the most votes win
                    if (CurrentDate < round.AskedOn.AddDays(Elections.SpeakerRound.ConsultationDays)) { return; }
                    Elections.FormationProposal proposal = DraftProposal(country, round, round.Asked);
                    if (Involves(proposal, country.PlayerPartyAbbrev))
                    {
                        round.Proposal = proposal;
                        round.Stage = Elections.RoundStage.OfferToPlayer;
                        round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {round.Asked} offers {country.PlayerPartyAbbrev} {(proposal.CabinetParties.Contains(country.PlayerPartyAbbrev) ? proposal.PostsOf(country.PlayerPartyAbbrev) + " post(s) in " + string.Join("+", proposal.CabinetParties) : "a support agreement with " + string.Join("+", proposal.CabinetParties))}");
                        return;
                    }
                    TableAndVote(country, round, proposal);
                    return;
                case Elections.RoundStage.VotePending:
                    if (CurrentDate < round.VoteOn) { return; }
                    Investiture(country, g, round);
                    return;
                default:
                    return;   // PlayerAsked and OfferToPlayer wait on the player (the clock holds on them, GameController.FormationHolds)
            }
        }

        /// <summary>R7: the investiture - the formation's own vote under the country's rule, recorded as a division; a win installs, a loss counts to the
        /// limit (the Riksdag, RF 6 kap. 5 §). §713: a Bundestag round never comes here - every ballot of Art. 63 is a vote on the person (<see cref="PersonBallot"/>).</summary>
        private void Investiture(Country country, Elections.GovernmentRecord g, Elections.SpeakerRound round)
        {
            if (IsBundestagRound(round)) { PersonBallot(country, g, round); return; }   // §712-§713: the Bundestag votes on persons, in every phase
            Elections.ProposalVerdict verdict = Elections.Formateur.Answer(country, round.Proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), country.PlayerPartyAbbrev);
            string title = $"Investiture: {PoliSim.Wording.EnglishText.Possessive(round.Proposal.Formateur, false, true)} proposal, {string.Join("+", round.Proposal.CabinetParties)} - {(verdict.Passes ? "approved" : "rejected")}";
            RecordInvestiture(country, verdict, title, verdict.Passes);
            if (verdict.Passes) { Install(country, g, round, verdict); return; }
            round.Rejections++;
            var refusals = new List<string>();
            foreach (Elections.PartyAnswer answer in verdict.Answers) { if (!answer.Accepts) { refusals.Add(answer.Party + " " + answer.Reason); } }
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Riksdag rejects {PoliSim.Wording.EnglishText.Possessive(round.Proposal.Formateur, false, true)} proposal ({verdict.Reason}{(refusals.Count > 0 ? ": " + string.Join("; ", refusals) : string.Empty)}); {round.Rejections} of {Elections.SpeakerRound.ProposalLimit} rejected (RF 6 kap. 5 §)");
            SpeakerLog(country, $"SPEAKER: {country.Id} - {title}; {round.Rejections} rejected{(refusals.Count > 0 ? " - " + string.Join("; ", refusals) : string.Empty)}");
            if (round.Rejections >= Elections.SpeakerRound.ProposalLimit) { BreakOff(country, g, round); return; }
            AskNext(country, round);
        }

        /// <summary>The investiture as a division - each seated party's side and its reason, a vote on a head of government rather than a bill.</summary>
        private void RecordInvestiture(Country country, Elections.ProposalVerdict verdict, string title, bool passes)
        {
            var sides = new List<DivisionSide>();
            if (verdict.Investiture != null)
            {
                for (int p = 0; p < verdict.Parties.Count; p++)
                {
                    int seats = SeatsOf(country, verdict.Parties[p].Abbrev);
                    if (seats <= 0) { continue; }
                    Elections.CoalitionFormation.InvestitureSide side = verdict.Investiture.Sides[p];
                    int s = side == Elections.CoalitionFormation.InvestitureSide.Against ? -1 : side == Elections.CoalitionFormation.InvestitureSide.Abstains ? 0 : 1;
                    sides.Add(new DivisionSide { Abbrev = verdict.Parties[p].Abbrev, ShortName = verdict.Parties[p].ShortName, Seats = seats, Side = s, Alignment = s, Reason = verdict.Investiture.Reasons[p] });
                }
            }
            country.Divisions.Append(title, CurrentDate, passes ? 1f : -1f, passes, 0f, (int)BillAxis.Fiscal, sides);
            country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;   // a vote on a prime minister, not a bill: no ceremony, never a budget act
        }

        /// <summary>§705: "die Mehrheit der Mitglieder des Bundestages" (Art. 63 Abs. 2 GG) - more than half the seated members, the chamber's majority.</summary>
        private static int MajorityOf(Country country)
        {
            int total = 0;
            if (country.ParliamentSeats != null) { foreach (int s in country.ParliamentSeats.Values) { total += s; } }
            return total / 2 + 1;
        }

        /// <summary>
        /// §712-§713 (Elias's rulings of 2026-10-01, items 3 and 4): EVERY BALLOT OF ART. 63 IS A VOTE ON THE PERSON. "The 14-day ballot is a vote
        /// on the person: signers vote for their nominee, and members choose among the nominated candidates" (item 3); "In every ballot, parties
        /// vote for the nominee they prefer, never one they've declared against, and abstain if none is acceptable" (item 4).
        /// - **Phase 1** (Art. 63 Abs. 1-2: "Der Bundeskanzler wird auf Vorschlag des Bundespräsidenten vom Bundestage ohne Aussprache gewählt.
        ///   Gewählt ist, wer die Stimmen der Mehrheit der Mitglieder des Bundestages auf sich vereinigt."): the Bundespräsident's one candidate.
        /// - **The fourteen days** (Abs. 3: "... so kann der Bundestag binnen vierzehn Tagen nach dem Wahlgange mit mehr als der Hälfte seiner
        ///   Mitglieder einen Bundeskanzler wählen."): every nomination then standing (GO-BT § 4 Abs. 2, <see cref="FourteenDaysCandidates"/>).
        /// Each party votes by the ballot's tally (<see cref="Tally"/>): a party bound by the tabled government (<see cref="Commitments"/>) for its
        /// candidate; a nomination's own party and its signers for it; every other party for the nominee it prefers - the nearest - among those it
        /// may vote for, abstaining where it may vote for none. A candidate with a majority of the members is elected and its government drawn
        /// (<see cref="ElectedGovernment"/>). Not elected in phase 1, the fourteen days open; in them, every nomination in the ballot has stood
        /// (§706: one ballot) and the days run out to the ballot the most votes win (Abs. 4). Before §713 phase 1 was the formateur's investiture
        /// (its cabinet and supporters carrying it): a nominee the other parties would vote for was not elected for want of a formation.
        /// </summary>
        private void PersonBallot(Country country, Elections.GovernmentRecord g, Elections.SpeakerRound round)
        {
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
            int IndexOf(string key) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }
            string player = country.PlayerPartyAbbrev;
            bool firstPhase = round.Phase <= 1;
            string abs = firstPhase ? "Abs. 2" : "Abs. 3";
            Dictionary<string, string> committed = Commitments(country, round, round.Proposal);
            string how = "the Bundespräsident's proposal (Art. 63 Abs. 1 GG)";
            List<string> candidates = firstPhase ? new List<string> { round.Proposal.Formateur } : FourteenDaysCandidates(country, round, chamber, parties, round.Proposal, committed, out how);
            Dictionary<string, int> votes = Tally(country, round, chamber, parties, candidates, out List<(string Party, string For, string Reason)> cast, committed);
            string leader = candidates[0];
            foreach (string c in candidates) { if (votes[c] > votes[leader]) { leader = c; } }
            int majority = MajorityOf(country);
            bool elected = votes[leader] >= majority;
            var tally = new List<string>();
            foreach (string c in candidates) { tally.Add(CandidateOf(country, round, c) + " " + votes[c]); }

            var sides = new List<DivisionSide>();
            foreach ((string party, string votedFor, string why) in cast)
            {
                int s = elected ? (votedFor == leader ? 1 : votedFor == null ? 0 : -1) : (votedFor == null ? 0 : -1);
                sides.Add(new DivisionSide { Abbrev = party, ShortName = parties[IndexOf(party)].ShortName, Seats = SeatsOf(country, party), Side = s, Alignment = s,
                    Reason = votedFor == null ? why : "votes for " + CandidateOf(country, round, votedFor) + " - " + why });
            }
            string title = $"Chancellor's election (Art. 63 {abs} GG): {string.Join(", ", tally)} - "
                + (elected ? $"{CandidateOf(country, round, leader)} elected, a majority of the members" : $"{(firstPhase ? "not elected" : "no candidate elected")} ({majority} a majority of the members)");
            country.Divisions.Append(title, CurrentDate, elected ? 1f : -1f, elected, 0f, (int)BillAxis.Fiscal, sides);
            country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;
            round.Log.Add(firstPhase
                ? $"{CurrentDate:yyyy-MM-dd}: the Bundestag votes on {CandidateOf(country, round, leader)}, the Bundespräsident's candidate (Art. 63 Abs. 2 GG): {votes[leader]} for - " + (elected ? $"elected, {majority} a majority of the members" : $"not elected, {majority} needed, a majority of the members")
                : $"{CurrentDate:yyyy-MM-dd}: a ballot in the fourteen days (Art. 63 Abs. 3 GG) on the nominations standing ({how}): {string.Join(", ", tally)} - "
                    + (elected ? $"{CandidateOf(country, round, leader)} is elected with a majority of the members, {majority}" : $"none reaches a majority of the members, {majority}"));
            SpeakerLog(country, $"SPEAKER: {country.Id} - {title}");
            if (!firstPhase) { foreach (string c in candidates) { if (!round.StoodInPhase2.Contains(c)) { round.StoodInPhase2.Add(c); } } }   // §706: each stood once in the fourteen days
            if (elected)
            {
                Elections.FormationProposal government = ElectedGovernment(country, round, leader, round.Proposal, out Elections.ProposalVerdict verdict, out bool seatedByGroup);
                if (government != null)
                {
                    round.Proposal = government;
                    Install(country, g, round, verdict, $"the chancellor's election {round.Occasion}: {CandidateOf(country, round, leader)} elected {(firstPhase ? "on the Bundespräsident's proposal" : "in the fourteen days")} with a majority of the members, {votes[leader]} (Art. 63 {abs} GG)");
                    if (seatedByGroup) { country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {player} sits in {PoliSim.Wording.EnglishText.Possessive(CandidateOf(country, round, leader), false)} cabinet as its parliamentary group's partner - a group votes and governs as one (the game's premise)"); }
                    return;
                }
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {CandidateOf(country, round, leader)} is elected, and no government can be drawn on its party; the round goes on");
            }
            round.Rejections++;
            if (firstPhase)
            {
                // §705: the Bundespräsident's candidate not elected opens the fourteen days (Abs. 3)
                round.Phase = 2;
                round.SecondPhaseUntil = CurrentDate.AddDays(Elections.SpeakerRound.BundestagSecondPhaseDays);
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Bundestag has until {round.SecondPhaseUntil:yyyy-MM-dd} to elect a chancellor with a majority of its members (Art. 63 Abs. 3 GG)");
            }
            if (CurrentDate > round.SecondPhaseUntil) { PluralityBallot(country, g, round); return; }
            AskNext(country, round);
        }

        /// <summary>§712 (the review's defect 1): the parties a tabled government binds - where every invited party accepts it, each cabinet partner and
        /// supporter (the player's party included, where it accepted) votes for its formateur's candidate in the ballot it calls. Null where none.</summary>
        private Dictionary<string, string> Commitments(Country country, Elections.SpeakerRound round, Elections.FormationProposal proposal)
        {
            if (proposal == null || string.IsNullOrEmpty(proposal.Formateur)) { return null; }
            Elections.ProposalVerdict verdict = Elections.Formateur.Answer(country, proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), country.PlayerPartyAbbrev);
            if (!verdict.AllAccept) { return null; }
            var bound = new Dictionary<string, string>();
            foreach (string p in proposal.CabinetParties) { if (p != proposal.Formateur) { bound[p] = proposal.Formateur; } }
            foreach (string p in proposal.Supporters) { if (p != proposal.Formateur) { bound[p] = proposal.Formateur; } }
            return bound;
        }

        /// <summary>
        /// §712 (the review's defect 4): what a proposal the player would table faces in the Bundestag's ballot in the fourteen days - the same
        /// nominations, the same tally and the same commitments the ballot itself reads (<see cref="PersonBallot"/>, §713: in phase 1 the Bundespräsident's one candidate): the votes for its
        /// formateur's candidate, for every other candidate, and the majority of the members. §713: in phase 1 the Bundespräsident's one candidate; false outside a Bundestag round.
        /// </summary>
        public bool ProjectPersonBallot(CountryId countryId, Elections.FormationProposal proposal, out int forCandidate, out int forOthers, out int majority)
        {
            forCandidate = forOthers = majority = 0;
            Country country = _world?.GetCountry(countryId);
            Elections.SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null || proposal == null || !IsBundestagRound(round)) { return false; }
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
            Dictionary<string, string> committed = Commitments(country, round, proposal);
            List<string> candidates = round.Phase <= 1 ? new List<string> { proposal.Formateur } : FourteenDaysCandidates(country, round, chamber, parties, proposal, committed, out string _);
            Dictionary<string, int> votes = Tally(country, round, chamber, parties, candidates, out List<(string Party, string For, string Reason)> _, committed);
            foreach (KeyValuePair<string, int> kv in votes) { if (kv.Key == proposal.Formateur) { forCandidate = kv.Value; } else { forOthers += kv.Value; } }
            majority = MajorityOf(country);
            return true;
        }

        /// <summary>§712 (the second pass, defect 5, and its note on drift): a ballot in the fourteen days' candidates, ONE rule for the ballot and the
        /// sheet's projection - the nominations standing with the tabled government's commitments in the signing (a bound party signs its
        /// formateur's nomination, never a rival's), less any that has stood, the player's only where the player tabled; the formateur's tabling is
        /// its nomination.</summary>
        private List<string> FourteenDaysCandidates(Country country, Elections.SpeakerRound round, Elections.CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> parties,
            Elections.FormationProposal proposal, IReadOnlyDictionary<string, string> committed, out string how)
        {
            string player = country.PlayerPartyAbbrev;
            List<string> candidates = Nominated(country, round, chamber, parties, pluralityBallot: false, out how, committed);
            candidates.RemoveAll(c => round.StoodInPhase2.Contains(c) || (c == player && proposal.Formateur != player));
            if (!candidates.Contains(proposal.Formateur)) { candidates.Insert(0, proposal.Formateur); }
            return candidates;
        }

        /// <summary>
        /// §712: the government a candidate elected by the Bundestag forms - shared by the ballot in the fourteen days and the ballot the most votes win
        /// (§705's rule, moved here unchanged): the proposal its own party TABLED in the round where it carries every invited partner's acceptance
        /// and splits no group; else, for a candidate not the player's, its party's draft with every invited partner accepting - the player's party
        /// never drafted in unasked; else its party and its parliamentary group alone (a group governs as one, the game's premise). Null where no
        /// government can be drawn.
        /// </summary>
        private Elections.FormationProposal ElectedGovernment(Country country, Elections.SpeakerRound round, string winner, Elections.FormationProposal tabled,
            out Elections.ProposalVerdict verdict, out bool seatedByGroup)
        {
            string player = country.PlayerPartyAbbrev;
            Elections.FormationProposal government = null;
            verdict = null;
            seatedByGroup = false;
            if (tabled != null && tabled.Formateur == winner)
            {
                government = tabled;
                verdict = Elections.Formateur.Answer(country, government, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), player);
                if (!verdict.AllAccept || verdict.Investiture == null || verdict.Investiture.SplitsJointGroup) { government = null; }
            }
            else if (winner != player)
            {
                string decline = string.IsNullOrEmpty(player) ? null : player + ">" + winner;
                bool added = decline != null && !round.Declines.Contains(decline);
                if (added) { round.Declines.Add(decline); }
                government = DraftProposal(country, round, winner);
                if (added) { round.Declines.Remove(decline); }
                verdict = Elections.Formateur.Answer(country, government, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), player);
                if (Involves(government, player) || !verdict.AllAccept || verdict.Investiture == null || verdict.Investiture.SplitsJointGroup) { government = null; }   // (B) a split group is no government
            }
            if (government == null) { government = GroupAlone(country, round, winner, out verdict, out seatedByGroup); }
            return verdict?.Investiture == null ? null : government;
        }

        /// <summary>§705's last resort for an elected candidate whose government does not hold - its party and its parliamentary group alone (a group
        /// governs as one, the game's premise), Gamson's posts. §754: moved out of <see cref="ElectedGovernment"/> unchanged, so the constructive vote's
        /// elected successor (Art. 67) forms by the same rule as Art. 63's elected candidate.</summary>
        private Elections.FormationProposal GroupAlone(Country country, Elections.SpeakerRound round, string winner, out Elections.ProposalVerdict verdict, out bool seatedByGroup)
        {
            string player = country.PlayerPartyAbbrev;
            seatedByGroup = false;
            var government = new Elections.FormationProposal { Formateur = winner };
            government.CabinetParties.Add(winner);
            if (SeatedGroupPartner(country, winner) is string winnerPartner)
            {
                government.CabinetParties.Add(winnerPartner);
                if (winnerPartner == player) { round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {player}, the player's party, sits in {PoliSim.Wording.EnglishText.Possessive(CandidateOf(country, round, winner), false)} cabinet as its parliamentary group's partner - a group governs as one (the game's premise)"); seatedByGroup = true; }
            }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in Elections.GovernmentRecord.GamsonPosts(country, government.CabinetParties, winner, CurrentDate)) { government.Posts[kv.Key] = new List<CabinetPortfolio>(kv.Value); }
            government.FreezeTabled(country, CurrentDate, _world);
            verdict = Elections.Formateur.Answer(country, government, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), player);
            return government;
        }

        /// <summary>
        /// §705: Art. 63 Abs. 4 GG - "Kommt eine Wahl innerhalb dieser Frist nicht zustande, so findet unverzüglich ein neuer Wahlgang statt, in dem
        /// gewählt ist, wer die meisten Stimmen erhält." A BALLOT, NOT A FORMATION (the review's defect 3: the first cut elected the government the
        /// most seats accepted - in phase 3 none holds, so every draft was a party alone and the largest group won by default):
        /// - every party in the Bundespräsident's order stands its candidate; a parliamentary group stands one, its larger member's;
        /// - [AUTHORED-DRAFT] a candidate's own party and its group vote for it; every other seated party votes for the candidate NEAREST it (the
        ///   formation's compatibility) whose party it does not refuse (a line that blocks its support, declared, derived or standing in the round),
        ///   and abstains where it refuses them all - sincere votes, no party voting tactically; each side's reason on the division says so;
        /// - the most votes elect, a tie to the larger Fraktion (§715: the pool runs largest Fraktion first).
        /// The elected candidate's government is its party's draft with every invited partner accepting - the player's party never drafted in unasked
        /// (the review's defect 2: the player auto-accepts, so an AI draft could seat it, even after a pass) - else its party and group alone; the
        /// player's own candidate stands on its party and group alone, the partners it never tabled not drafted for it.
        /// §713 (Elias's ruling of 2026-10-01, item 4: "The final ballot follows Art. 63(4) as written, with the premise for the President's choice
        /// stated"). Art. 63 Abs. 4 Satz 2-3 GG, as written: "Vereinigt der Gewählte die Stimmen der Mehrheit der Mitglieder des Bundestages auf sich,
        /// so muß der Bundespräsident ihn binnen sieben Tagen nach der Wahl ernennen. Erreicht der Gewählte diese Mehrheit nicht, so hat der
        /// Bundespräsident binnen sieben Tagen entweder ihn zu ernennen oder den Bundestag aufzulösen." With a majority the appointment is the
        /// article's; without one the choice is the Bundespräsident's, and **THE PREMISE FOR HIS CHOICE, STATED [AUTHORED-DRAFT]: he appoints** - since q8 (AF-8) a ruling with its citation, the appointment being one of the two acts Satz 3 allows.
        /// Why: no Bundespräsident has faced the choice, so no record decides it; and a dissolution calls a new election within sixty days (Art. 39
        /// Abs. 1 Satz 4 GG), which is not modelled for Germany. He appoints at once, inside the seven days - as the record's chancellors were appointed
        /// on the day of their election (Merz, 6 May 2025 [BT-KW25]). The premise is on the division's title, the round's log and the government's basis.
        /// In every ballot (item 4) each party votes for the nominee it prefers among those it may vote for, never one it has declared against
        /// (`Tally`) - the reading taken: "acceptable" keeps the model's derived lines too, so a party never votes across a line the model derives.
        /// </summary>
        private void PluralityBallot(Country country, Elections.GovernmentRecord g, Elections.SpeakerRound round)
        {
            round.Phase = 3;
            Elections.CoalitionFormation.Chamber chamber = Elections.Formateur.ChamberOf(country, RoundReading(country, round), RoundLines(country, round), out IReadOnlyList<PoliticalParty> parties);
            int IndexOf(string key) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }
            string player = country.PlayerPartyAbbrev;
            // §706 (Elias's ruling of 2026-10-01): the candidates are the nominations GO-BT § 4 lets stand - a quarter of the members' signatures,
            // or a Fraktion of a quarter (Abs. 2); failing any, a Fraktion or five per cent (Abs. 3); failing even that, every member's right.
            List<string> candidates = Nominated(country, round, chamber, parties, pluralityBallot: true, out string nominatedHow);
            if (candidates.Count == 0)
            {
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: no candidate stands in the ballot the most votes win (Art. 63 Abs. 4 GG); the outgoing government serves on");
                round.Stage = Elections.RoundStage.Concluded;
                return;
            }
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the nominations for the ballot the most votes win: {string.Join(", ", candidates)} - {nominatedHow}");
            Dictionary<string, int> votes = Tally(country, round, chamber, parties, candidates, out List<(string Party, string For, string Reason)> cast);
            string winner = candidates[0];
            foreach (string c in candidates) { if (votes[c] > votes[winner]) { winner = c; } }
            int majority = MajorityOf(country);
            bool absolute = votes[winner] >= majority;
            string candidate = CandidateOf(country, round, winner);

            // The ballot as a division, recorded first (the review's third pass, C: a branch returning before it left no record): for the elected
            // candidate, a vote for another candidate against it, an abstention - each party's reason the premise.
            var sides = new List<DivisionSide>();
            foreach ((string party, string votedFor, string why) in cast)
            {
                int s = votedFor == winner ? 1 : votedFor == null ? 0 : -1;
                PoliticalParty pp = parties[IndexOf(party)];
                sides.Add(new DivisionSide { Abbrev = party, ShortName = pp.ShortName, Seats = SeatsOf(country, party), Side = s, Alignment = s,
                    Reason = votedFor == null ? why : "votes for " + CandidateOf(country, round, votedFor) + " - " + why });
            }
            string title = $"Chancellor's election (Art. 63 Abs. 4 GG): {candidate} - elected with the most votes, {votes[winner]}"
                + (absolute ? ", a majority of the members - the Bundespräsident must appoint within seven days (Satz 2)" : ", short of a majority - appointed within seven days, not dissolved (Satz 3: the choice is the Bundespräsident's; the premise for it stated, the game's)");
            country.Divisions.Append(title, CurrentDate, 1f, true, 0f, (int)BillAxis.Fiscal, sides);
            country.Divisions.Entries[country.Divisions.Entries.Count - 1].Motion = true;

            // The elected candidate's government: its party's draft, the player's party never drafted in unasked, every invited partner accepting;
            // else its party and group alone. The player's own candidate stands on its party and group alone. [AUTHORED-DRAFT] a parliamentary
            // group governs as one: where the elected candidate's group partner is the player's party, it sits in the cabinet by the group's rule
            // (the review's third pass, B and C - leaving it out split the Union, and drawing no government left Germany under a caretaker for good).
            Elections.FormationProposal government = ElectedGovernment(country, round, winner, null, out Elections.ProposalVerdict verdict, out bool seatedByGroup);   // §712: the shared rule, unchanged
            if (government == null)
            {
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {candidate} is elected with the most votes, and no government can be drawn on its party (Art. 63 Abs. 4 GG); the outgoing government serves on");
                round.Stage = Elections.RoundStage.Concluded;
                return;
            }
            round.Proposal = government;
            var tally = new List<string>();
            foreach (string c in candidates) { tally.Add(c + " " + votes[c]); }
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the fourteen days pass; in the ballot the most votes win ({string.Join(", ", tally)}), {candidate} is elected with {votes[winner]} ({majority} a majority of the members)"
                + (absolute ? " - the Bundespräsident must appoint the candidate within seven days (Art. 63 Abs. 4 Satz 2 GG) and does so at once" : " - short of a majority, the Bundespräsident has seven days to appoint the candidate or dissolve the Bundestag (Art. 63 Abs. 4 Satz 3 GG); the premise for his choice, stated: he appoints, at once - no Bundespräsident has faced the choice, and a dissolution's new election (Art. 39 Abs. 1 Satz 4 GG) is not modelled for Germany"));
            SpeakerLog(country, $"SPEAKER: {country.Id} - {title}; the tally {string.Join(", ", tally)}");
            Install(country, g, round, verdict, absolute
                ? $"the chancellor's election {round.Occasion}: {candidate} elected with the most votes, {votes[winner]}, a majority of the members (Art. 63 Abs. 4 Satz 2 GG)"
                : $"the chancellor's election {round.Occasion}: {candidate} elected with the most votes, {votes[winner]}, short of a majority, and appointed within the seven days rather than the Bundestag dissolved - the premise for the Bundespräsident's choice, stated (Art. 63 Abs. 4 Satz 3 GG)");
            // the review's fourth pass (E): the rule that seated the player's party is on the government it sits in, where the desk reads it
            if (seatedByGroup) { country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {player} sits in {PoliSim.Wording.EnglishText.Possessive(candidate, false)} cabinet as its parliamentary group's partner - a group votes and governs as one (the game's premise)"); }
        }

        /// <summary>
        /// §705: the ballot's sincere tally over <paramref name="candidates"/> - a candidate's own party votes for it; a parliamentary group votes as
        /// one, the larger member first and the smaller following it whatever its vote; every other seated party for the candidate nearest it (the
        /// formation's compatibility) whose party it does not refuse; an unplaced party abstains; the player's declines in the round stand in its vote.
        /// Also what signs a nomination (§706): the members who would vote for a candidate sign it.
        /// </summary>
        private Dictionary<string, int> Tally(Country country, Elections.SpeakerRound round, Elections.CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> parties,
            List<string> candidates, out List<(string Party, string For, string Reason)> cast, IReadOnlyDictionary<string, string> committed = null)
        {
            int IndexOf(string key) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }
            string player = country.PlayerPartyAbbrev;
            bool Declined(string formateur) => !string.IsNullOrEmpty(player) && round.Declines.Contains(player + ">" + formateur);
            var votes = new Dictionary<string, int>();
            foreach (string c in candidates) { votes[c] = 0; }
            cast = new List<(string Party, string For, string Reason)>();
            var chose = new Dictionary<string, string>();
            // the review's third pass (D): a parliamentary group votes as one, by its larger member - the larger members first
            var byGroup = new List<int>();
            for (int p = 0; p < parties.Count; p++) { if (!IsSmallerGroupMember(country, parties[p].Abbrev)) { byGroup.Add(p); } }
            for (int p = 0; p < parties.Count; p++) { if (IsSmallerGroupMember(country, parties[p].Abbrev)) { byGroup.Add(p); } }
            foreach (int p in byGroup)
            {
                if (chamber.Seats[p] <= 0) { continue; }
                string key = parties[p].Abbrev;
                string partner = SeatedGroupPartner(country, key);
                bool isPlayer = key == player;
                string choice = candidates.Contains(key) ? key : null;
                string reason;
                if (IsSmallerGroupMember(country, key) && partner != null && chose.ContainsKey(partner))
                {
                    choice = chose[partner];
                    reason = "votes with its parliamentary group, as one (" + PoliSim.Wording.EnglishText.Possessive(partner, false, true) + " side) - the game's premise: a group votes and governs as one";   // the review's fourth pass (E): the premise on the division the player reads
                    chose[key] = choice;
                    if (choice != null) { votes[choice] += chamber.Seats[p]; }
                    cast.Add((key, choice, reason));
                    continue;
                }
                if (committed != null && committed.TryGetValue(key, out string partnerOf) && candidates.Contains(partnerOf))
                {
                    // §712 (the review's defect 1): a party that accepted a tabled government votes for its candidate - the coalition's word; without it a
                    // coalition every partner accepted could fail its own ballot
                    choice = partnerOf;
                    reason = "accepted " + PoliSim.Wording.EnglishText.Possessive(partnerOf, false, true) + " government - votes for its candidate (the coalition's word, the game's premise)";
                }
                else if (choice != null) { reason = "its own candidate"; }
                else if (!parties[p].HasPosition) { reason = "holds no surveyed position to be near to - abstains (the game's premise)"; }   // the SSW: compatibility 0 everywhere would hand its vote to the order's first
                else
                {
                    double nearest = double.NegativeInfinity;
                    foreach (string c in candidates)
                    {
                        int ci = IndexOf(c);
                        if (ci < 0) { continue; }
                        // §715: a nominee whose party holds no surveyed position cannot be NEAR anyone - its compatibility is 0 everywhere, which beats every
                        // negative one, and the first run with every party in the pool had the chamber elect the SSW's candidate 630 to 0. It draws its own
                        // party's vote and its bound and group votes, never a sincere one (the SSW's own vote abstains for the same reason, above)
                        if (!parties[ci].HasPosition) { continue; }
                        int cp = SeatedGroupPartner(country, c) is string cPartner ? IndexOf(cPartner) : -1;
                        bool refuses = false;
                        foreach (Elections.RedLine line in chamber.Lines) { if (line.RefusesSupport(p, ci) || (cp >= 0 && line.RefusesSupport(p, cp))) { refuses = true; break; } }
                        if (isPlayer && Declined(c)) { refuses = true; }   // the player's decline in this round stands in its vote too
                        if (refuses || chamber.Compatibility[p, ci] <= nearest) { continue; }
                        nearest = chamber.Compatibility[p, ci];
                        choice = c;
                    }
                    reason = choice != null ? "the candidate nearest it that it does not refuse (the game's premise: sincere votes)" : "refuses every candidate - abstains";
                }
                chose[key] = choice;
                if (choice != null) { votes[choice] += chamber.Seats[p]; }
                cast.Add((key, choice, reason));
            }
            cast.Sort((a, b) => IndexOf(a.Party).CompareTo(IndexOf(b.Party)));   // the division lists the parties in the chamber's order
            return votes;
        }

        /// <summary>
        /// §706 (Elias's ruling of 2026-10-01: "in the later phases, candidates are nominated as §4 of the Bundestag's Rules of Procedure sets out, read
        /// from its text and applied as written, including what a party too small to nominate alone must do"). GO-BT § 4 (Stand 11. Juli 2026, the
        /// official print, `ElectionsData/germany/raw/records/gobt_2026-07-11.pdf` [GOBT-4]):
        /// (2) "Wahlvorschläge zu Wahlgängen gemäß Artikel 63 Absatz 3 und 4 des Grundgesetzes sind von einem Viertel der Mitglieder des Bundestages
        /// oder einer Fraktion, die mindestens ein Viertel der Mitglieder des Bundestages umfasst, zu unterzeichnen."
        /// (3) "Erreicht zu dem Wahlgang gemäß Artikel 63 Absatz 4 des Grundgesetzes kein Wahlvorschlag die notwendige Anzahl an Unterzeichnungen, steht
        /// jedem Mitglied des Bundestages das Wahlvorschlagsrecht zu, es sei denn, ein Vorschlag ist von einer Fraktion oder von fünf vom Hundert der
        /// Mitglieder des Bundestages unterzeichnet."
        /// A party too small to nominate alone gathers other members' signatures: [AUTHORED-DRAFT] the members who would vote for its candidate sign
        /// it (the ballot's sincere tally, <see cref="Tally"/>, over the nominations standing - iterated, since a nomination that falls moves the
        /// votes it held). A Fraktion is a group of at least five per cent of the members (§ 10 Abs. 1 GO-BT, as §705's record quotes it).
        /// §715 (Elias's ruling of 2026-10-01, item 6: "Nominations follow GO-BT §4's text: any qualifying quarter may nominate. The President's order
        /// applies only to phase 1"): THE POOL IS EVERY PARTY SEATED - a parliamentary group standing one, its larger member's - largest Fraktion
        /// first (a tie in the chamber's order); the Bundespräsident's order is phase 1's alone (<see cref="AskNext"/>). In the ballot the most votes
        /// win the player's party stands only where it stood in the round.
        /// </summary>
        private List<string> Nominated(Country country, Elections.SpeakerRound round, Elections.CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> parties,
            bool pluralityBallot, out string how, IReadOnlyDictionary<string, string> committed = null)
        {
            string player = country.PlayerPartyAbbrev;
            var pool = new List<string>();
            var seated = new List<int>();
            for (int p = 0; p < parties.Count; p++) { if (SeatsOf(country, parties[p].Abbrev) > 0 && !IsSmallerGroupMember(country, parties[p].Abbrev)) { seated.Add(p); } }
            seated.Sort((a, b) => GroupSeats(country, parties[b].Abbrev) != GroupSeats(country, parties[a].Abbrev) ? GroupSeats(country, parties[b].Abbrev).CompareTo(GroupSeats(country, parties[a].Abbrev)) : a.CompareTo(b));
            foreach (int p in seated)
            {
                string party = parties[p].Abbrev;
                if (pluralityBallot && party == player && round.PlayerPassed && !round.PlayerStood) { continue; }   // §705: a pass is not a candidacy; §715: only a pass keeps it off - a player's party never asked stands as an AI party would
                pool.Add(party);
            }
            int members = 0;
            foreach (int s in chamber.Seats) { members += s; }
            int quarter = (members + 3) / 4;              // "ein Viertel der Mitglieder": at least a quarter, 158 of 630
            int fivePercent = (members * 5 + 99) / 100;    // "fünf vom Hundert": 32 of 630
            // the review of §706 (defect 2): ONE AT A TIME - the weakest nomination short of a quarter falls, its members' votes move (the tally over
            // the nominations still standing), and the rest are counted again; dropping every short nomination at once let a small party's votes go
            // only to nominations that already held a quarter, so two small parties could never pool their signatures behind one of them
            var standing = new List<string>(pool);
            for (int pass = 0; pass < pool.Count + 1 && standing.Count > 0; pass++)
            {
                Dictionary<string, int> backing = Tally(country, round, chamber, parties, standing, out _, committed);   // §712 (the second pass, defect 5): a bound party signs its formateur's nomination, never a rival's
                string weakest = null;
                foreach (string c in standing)
                {
                    int signed = System.Math.Max(backing[c], GroupSeats(country, c));
                    if (signed >= quarter) { continue; }
                    if (weakest == null || signed < System.Math.Max(backing[weakest], GroupSeats(country, weakest)) || (signed == System.Math.Max(backing[weakest], GroupSeats(country, weakest)) && standing.IndexOf(c) > standing.IndexOf(weakest))) { weakest = c; }
                }
                if (weakest == null) { break; }
                standing.Remove(weakest);
            }
            if (standing.Count > 0) { how = F($"signed by a quarter of the members, {quarter} of {members}, or a Fraktion of a quarter (GO-BT § 4 Abs. 2)"); return standing; }
            if (!pluralityBallot) { how = F($"no nomination reaches a quarter of the members, {quarter} of {members} (GO-BT § 4 Abs. 2)"); return standing; }
            Dictionary<string, int> fallback = Tally(country, round, chamber, parties, pool, out _, committed);
            List<string> byFraktion = pool.FindAll(c => GroupSeats(country, c) >= fivePercent || fallback[c] >= fivePercent);
            if (byFraktion.Count > 0) { how = F($"none reached a quarter of the members; signed by a Fraktion or five per cent, {fivePercent} of {members} (GO-BT § 4 Abs. 3)"); return byFraktion; }
            how = "none reached a quarter, none a Fraktion or five per cent - every member may nominate (GO-BT § 4 Abs. 3)";
            return pool;
        }

        private static string F(System.FormattableString s) => s.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>§705: a party's seated partner in its parliamentary group (the CDU's CSU, the CSU's CDU), or null.</summary>
        private static string SeatedGroupPartner(Country country, string party)
        {
            foreach ((string a, string b) in Elections.ChamberRules.JointGroups(country.Id))
            {
                string partner = a == party ? b : b == party ? a : null;
                if (partner != null && SeatsOf(country, partner) > 0 && SeatsOf(country, party) > 0) { return partner; }
            }
            return null;
        }

        /// <summary>§705: the smaller member of a seated parliamentary group - it stands no candidate and moves no motion of its own.</summary>
        private static bool IsSmallerGroupMember(Country country, string party) =>
            SeatedGroupPartner(country, party) is string partner && (SeatsOf(country, partner) > SeatsOf(country, party) || (SeatsOf(country, partner) == SeatsOf(country, party) && string.CompareOrdinal(partner, party) < 0));

        /// <summary>§705: a party's seats with its parliamentary group's - what a candidacy stands on in the Bundestag (the Union's candidate is the Fraktion's).</summary>
        private static int GroupSeats(Country country, string party) => SeatsOf(country, party) + (SeatedGroupPartner(country, party) is string partner ? SeatsOf(country, partner) : 0);

        /// <summary>
        /// PS-6 US-2 (Elias's ruling F8, R-US1 (a): "whatever the game does not elect is seated on its record's dates - the 119th Congress at noon on 3 Jan
        /// 2025, and the president of record at noon on 20 Jan 2025 until the game holds the presidential election (US-8) ... A chamber not yet sourced (the
        /// 120th) is not seated: the 119th stands"): in the player's country, where the game elects neither (`WorldClock.RecordSeatsChamber`,
        /// `RecordSeatsExecutive` - the USA's), the House and the president of record are seated on their record's day. Held as STATE, not as a transition:
        /// the seated House and the president are compared with the record's for the day and seated where they differ - each day, and once as a save loads
        /// (`SaveGameService.RestoreInto`) - so a save from a build before US-2, holding the start's House past its day, is put right as it loads and no
        /// save-format step is owed (DECLARED); a budget bill pending in such a save is decided as every install leaves one (§630: a pending bill outlives the
        /// role that introduced it; §632: no window opens while it stands). The record's day is the day the world arrives on it ("noon" is a date - DECLARED). Past the record the last of
        /// record stands: a House elected after the record's date is not on record, so the latest sourced one stays seated (`WorldClock.SeatedVintage`), and a
        /// president whose term of record has ended stays in office until the game holds the presidential election (R-US1 (a); the words are
        /// `WorldClock.RecordStanding` and `ExecutiveStanding`). The record's seating carries each party's capital as the game's own elections do - the House
        /// changed by an election, the record's (DECLARED; else the game's first House election would divide by the start's seats). The president takes office
        /// through <see cref="TakeOffice"/> (the arrival budget window reset - DECLARED). Forks reach it as the game does; a world with no player country (the
        /// seeded shadow, the trajectory dump) never does - so the no-policy shadow a new game seeds keeps the start's House and president, while one forked
        /// from a loaded game seats the record with it (DECLARED; which of the two a shadow should be is not US-2's to decide).
        /// <para>PS-6 US-7 (`docs/specs/USA_STAGE_PLAN.md`): THE OATH, on the same dated transition. A president the game elected takes office on the oath's day -
        /// noon on the 20th of January after the polling day, the Twentieth Amendment §1 (<see cref="Elections.UsPresidentialElection.OathDayAfter"/>) - held as
        /// state like the record: the winner of the latest contest whose oath has come, where it is decided (<see cref="Elections.UsPresidentialElection.TryInOffice"/>) is
        /// seated through <see cref="TakeOffice"/> wherever the government in office is not that oath's (<see cref="Elections.UsPresidentialElection.Governs"/>),
        /// as a presidency whose cabinet is the president's party (<see cref="Elections.UsPresidentialElection.GovernmentOf"/>). From the game's own polling day
        /// the record seats no president (`WorldClock.RecordSeatsExecutive(Country, DateTime)`, R-US1 (a)), so between the polling day and the oath the sitting
        /// president governs and nothing re-seats the record's; and a save cut either side of the oath is put right, or left alone, as it loads. A contest still
        /// undecided on the oath's day - PS-6 US-16, R-US22 RULED (Elias's ruling I6, the Twentieth Amendment's order): never the sitting president past noon on
        /// 20 January; the Vice President the Senate chose acts as President, else the Speaker (<see cref="Elections.UsPresidentialElection.ActingGovernmentOf"/>),
        /// seated through <see cref="TakeOffice"/> as a presidency labelled acting. The player's
        /// country only, as the record (PS-8 owns the rest); the contests are a country's state, so the answer is the same whoever the player is.</para>
        /// <para>PS-6 US-15 (R-US1 (a), R-US10 (a)): THE SENATE OF RECORD on the same transition, between the House and the president - <see cref="SeatTheSecondChamberOfRecord"/>.</para>
        /// </summary>
        internal void SeatTheRecordOnItsDate()
        {
            if (!PlayerCountryId.HasValue) { return; }
            CountryId id = PlayerCountryId.Value;
            Country country = _world?.GetCountry(id);
            if (country == null) { return; }
            if (Elections.UsCongressElection.TryInOffice(country, CurrentDate, out Elections.UsCongressElection.Contest congress))
            {
                // PS-6 US-16: the House the game elected, from noon on 3 January after its polling day - seated once, as the record's is, and the record seats no
                // House in this world from then (WorldClock.RecordSeatsChamber(Country, DateTime)); each party's capital carried as at any election
                Dictionary<string, int> won = congress.HouseSeats(id);
                if (!SameSeats(country.ParliamentSeats, won))
                {
                    ParliamentSystem.SetSeatsFromElection(country, won);
                    Elections.PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);
                    Debug.Log($"CONGRESS: {id} - the House the game elected on {congress.PollingDay:yyyy-MM-dd} seated on {CurrentDate:yyyy-MM-dd}: REP {congress.HouseRep}, DEM {congress.HouseDem}");
                }
            }
            else if (Elections.WorldClock.RecordSeatsChamber(id))
            {
                ElectionVintage seated = Elections.WorldClock.SeatedVintage(id, CurrentDate);
                Dictionary<string, int> ofRecord = PartySystems.InitialSeats(id, seated);
                if (!SameSeats(country.ParliamentSeats, ofRecord))
                {
                    ParliamentSystem.SeatChamberOfRecord(country, seated);
                    Elections.PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);
                    Debug.Log($"RECORD: {id} - the chamber of record seated on {CurrentDate:yyyy-MM-dd}: {Elections.WorldClock.RecordStanding(id, CurrentDate)}");
                }
            }
            SeatTheSecondChamberOfRecord();
            if (Elections.UsPresidentialElection.TryActingOn(country, CurrentDate, out Elections.UsPresidentialElection.Contest undecided))
            {
                // PS-6 US-16 - R-US22 RULED (I6), the Twentieth Amendment's order: a contest still undecided at the oath - never the sitting president past noon on
                // 20 January; the Vice President the Senate chose acts, else the Speaker - seated once, then left alone
                if (!Elections.UsPresidentialElection.ActingGoverns(country.Government, undecided)) { TakeOffice(country, Elections.UsPresidentialElection.ActingGovernmentOf(country, undecided, CurrentDate), oath: undecided); }
            }
            else if (Elections.UsPresidentialElection.TryInOffice(country, CurrentDate, out Elections.UsPresidentialElection.Contest sworn))
            {
                // PS-6 US-7: the game's president, from the oath - seated once, then left alone (the same government object on every later day and load)
                if (!Elections.UsPresidentialElection.Governs(country.Government, sworn)) { TakeOffice(country, Elections.UsPresidentialElection.GovernmentOf(sworn, CurrentDate), oath: sworn); }
            }
            else if (Elections.WorldClock.RecordSeatsExecutive(country, CurrentDate) && Elections.WorldClock.TryGovernmentAt(id, CurrentDate, out Elections.WorldClock.GovernmentOfRecord g)
                && country.Government?.Executive != g.President)
            {
                TakeOffice(country, Elections.GovernmentRecord.AtStart(country, CurrentDate, _world));
            }
        }

        /// <summary>
        /// PS-6 US-15 (`docs/specs/USA_STAGE_PLAN.md`; Elias's ruling F8 - R-US1 (a): "whatever the game does not elect is seated on its record's dates";
        /// R-US10 (a): each independent "counted with the conference it caucuses with"): in the player's country, where the record seats the second chamber
        /// (`WorldClock.RecordSeatsSecondChamber` - the USA's Senate, until US-16 elects its classes), the Senate of record for the day
        /// (`WorldClock.SecondChamberOfRecord`, senator by senator from US-12's table) is compared with the one seated and seated where they differ - a vacancy,
        /// an oath, a new Congress - held as STATE like the House: each day from <see cref="SeatTheRecordOnItsDate"/>, once as a save loads, and once as a new
        /// game seats its player's country (`GameController`'s selection, so the start's own day carries the start's Senate before any day is stepped).
        /// DECLARED premises: the Senate's seating carries no party's capital (the capital's carry-over divides by the House's seats, the chamber a party's
        /// mandate is measured on - US-2's premise is the House's); past the record's reach the last of record stands (R-US1 (a)). Forks reach it as the game
        /// does; a world with no player country - the seeded shadow, the trajectory dump - never does, so its second chamber stays empty: the no-policy shadow a
        /// new game seeds keeps no Senate, while one forked from a loaded game seats the record's with it (US-2's shadow asymmetry, DECLARED again for the Senate).
        /// </summary>
        internal void SeatTheSecondChamberOfRecord()
        {
            if (!PlayerCountryId.HasValue) { return; }
            CountryId id = PlayerCountryId.Value;
            Country country = _world?.GetCountry(id);
            if (country == null || !Elections.WorldClock.RecordSeatsSecondChamber(id)) { return; }
            // PS-6 US-16: the record's seats by date with every seat the game elected over them, from its 3 January (UsCongressElection.SenateOn) - the record's
            // alone until the game's first class is seated
            Dictionary<string, int> ofRecord = Elections.UsCongressElection.SenateOn(country, CurrentDate);
            if (SameSeats(country.SecondChamberSeats, ofRecord)) { SeedTheFundingHorizon(country); return; }   // PS-6 US-22: once, where the Congress votes the budget
            ParliamentSystem.SeatSecondChamberOfRecord(country, ofRecord);
            var seated = new List<string>();
            foreach (KeyValuePair<string, int> kv in ofRecord) { if (kv.Value > 0) { seated.Add(kv.Key + " " + kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)); } }
            Debug.Log(Elections.UsCongressElection.GameSeatsSenate(country, CurrentDate)
                ? $"CONGRESS: {id} - the Senate seated on {CurrentDate:yyyy-MM-dd}: {string.Join(", ", seated)} - the seats the game elected, the rest of record by date"
                : $"RECORD: {id} - the second chamber of record seated on {CurrentDate:yyyy-MM-dd}: {string.Join(", ", seated)} - {Elections.WorldClock.SecondChamberStanding(id, CurrentDate)}");
            SeedTheFundingHorizon(country);   // PS-6 US-22: once the Senate is seated - the selection's call, a load's and the day's
        }

        /// <summary>
        /// PS-6 US-22: THE FUNDING HORIZON SEEDED ONCE - where the US Congress votes the budget (`UsBudgetProcedure.Applies`: the procedure, and a Senate
        /// seated) and no horizon is held, the government is funded through the end of the fiscal year in progress (DECLARED: the record's acts in force at
        /// the start are not read; a game from 2024-03-12 is funded through 2024-09-30). Reached as the Senate is (each day, a load, the selection), so the
        /// start's own day carries it; a save carries its own (format 41) and is never re-seeded. An AI USA, which seats no Senate, is never seeded.
        /// </summary>
        private void SeedTheFundingHorizon(Country country)
        {
            if (country.AppropriatedThrough != System.DateTime.MinValue || !UsBudgetProcedure.Applies(country)) { return; }
            country.AppropriatedThrough = UsBudgetProcedure.FiscalYearEnd(CurrentDate);
            Debug.Log($"BUDGET: {country.Id} - funded through {country.AppropriatedThrough:yyyy-MM-dd}, the end of the fiscal year in progress (seeded: the acts in force at the start are not read)");
        }

        private static bool SameSeats(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
        {
            if (a == null || b == null || a.Count != b.Count) { return false; }
            foreach (KeyValuePair<string, int> kv in a) { if (!b.TryGetValue(kv.Key, out int n) || n != kv.Value) { return false; } }
            return true;
        }

        /// <summary>PS-6 US-2: a government of record takes office, with the arrival budget window reset and a budget window the player no longer governs
        /// closed (<see cref="CloseBudgetWindowIfNotGoverning"/>, §698). PS-6 US-7: the president the game elected takes office through it too, at the oath
        /// (<paramref name="oath"/>, the contest; null for the record's). The window is closed FIRST and the arrival reset only where no arrival window is
        /// still open: a president of the player's own party sworn while the player's arrival window stands (the game's oath can keep the party in office;
        /// the record's never does for a player with a party) keeps that window as the new government's, and is not handed a second one after it - DECLARED.
        /// The one other case that differs from the reset-then-close: a player country with NO PARTY seated (the instrument's hand - `PlayerGoverns` is true
        /// under every government) with its arrival window open at any oath, the record's included, now keeps the used flag where it was reset - Editor tools
        /// only, since the game always seats a party. Every other case ends as before: a window the player no longer governs closed and the arrival reset;
        /// none open, the arrival reset. The open window is saved (format 39), so a loaded game takes the oath exactly as the continuous one.</summary>
        private void TakeOffice(Country country, Elections.GovernmentRecord formed, Elections.UsPresidentialElection.Contest oath = null)
        {
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
            country.Government = formed;
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: a term taken of record or by an oath seats the chairs it hands the player
            CloseBudgetWindowIfNotGoverning(country);
            if (!IsIncomingGovernmentBudgetWindow(country.Id)) { ResetArrivalBudgetWindow(country.Id); }
            Debug.Log(oath == null
                ? $"RECORD: {country.Id} - {formed.Executive} takes office on {CurrentDate:yyyy-MM-dd}, of record"
                : formed.Outcome == Elections.UsPresidentialElection.ActingOutcome
                ? $"OATH: {country.Id} - no president elected by {CurrentDate:yyyy-MM-dd}: {formed.Executive} acts - {oath.ActingAs} (R-US22)"
                : $"OATH: {country.Id} - {formed.Executive} takes office on {CurrentDate:yyyy-MM-dd}, elected in the game on {oath.PollingDay:yyyy-MM-dd}{(oath.Contingent ? " (chosen by the House)" : string.Empty)}");
        }

        private void Install(Country country, Elections.GovernmentRecord g, Elections.SpeakerRound round, Elections.ProposalVerdict verdict, string basis = null)
        {
            Elections.FormationProposal proposal = round.Proposal;
            bool bundestag = IsBundestagRound(round);
            var supporters = new List<string>();
            foreach (Elections.PartyAnswer answer in verdict.Answers) { if (!answer.InCabinet && answer.Accepts) { supporters.Add(answer.Party); } }
            basis ??= bundestag
                ? $"the chancellor's election {round.Occasion}: {CandidateOf(country, round, proposal.Formateur)} elected by a majority of the Bundestag's members (Art. 63 Abs. {(round.Phase <= 1 ? 2 : 3)} GG)"
                : $"the Speaker's round {round.Occasion}: {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} proposal won its investiture";
            Elections.GovernmentRecord formed = Elections.GovernmentRecord.FromProposal(country, proposal, supporters, verdict.Investiture.Kind, CurrentDate,
                basis, g.Kind, g.Executive, round.Refusals, _world);
            round.Stage = Elections.RoundStage.Concluded;
            round.Log.Add(bundestag
                ? $"{CurrentDate:yyyy-MM-dd}: the Bundestag elects {CandidateOf(country, round, proposal.Formateur)}; the Bundespräsident appoints, and {string.Join("+", formed.Cabinet)} takes office"
                : $"{CurrentDate:yyyy-MM-dd}: the Riksdag approves {PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true)} proposal; {string.Join("+", formed.Cabinet)} takes office");
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
            country.Government = formed;
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the round's government seats the chairs it hands the player
            ResetArrivalBudgetWindow(country.Id);
            CloseBudgetWindowIfNotGoverning(country);   // §698 (the review's defect 2, the same gap here)
            SpeakerLog(country, $"SPEAKER: {country.Id} - installed {string.Join("+", formed.Cabinet)} led by {formed.PmParty}{(formed.Support.Count > 0 ? " with " + string.Join("+", formed.Support) : string.Empty)}");
        }

        /// <summary>R7: four proposals rejected - the procedure breaks off until an election; an extra election within three months unless the ordinary one falls in them (RF 6 kap. 5 §, §640).</summary>
        private void BreakOff(Country country, Elections.GovernmentRecord g, Elections.SpeakerRound round)
        {
            round.Stage = Elections.RoundStage.Concluded;
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the procedure breaks off - the Riksdag rejected {Elections.SpeakerRound.ProposalLimit} proposals (RF 6 kap. 5 §)");
            if (Elections.WorldClock.TryNextPollingDay(country.Id, CurrentDate, out System.DateTime ordinary) && ordinary <= CurrentDate.AddMonths(Elections.ConfidenceProcedure.ExtraElectionMonths))
            {
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the Riksdag rejected four proposals; the ordinary election of {ordinary:yyyy-MM-dd} falls within three months and serves (RF 6 kap. 5 §)");
                return;
            }
            ScheduleExtraElection(country, "the Riksdag rejected four of the Speaker's proposals - the procedure breaks off to an extra election within three months (RF 6 kap. 5 §)");
        }

        /// <summary>R2: the day after an election in the player's country, a round opens on the new chamber, on that election's declarations - once per election.</summary>
        private bool OpenRoundAfterElection(Country country, Elections.GovernmentRecord g)
        {
            if (!RoundsApply(country.Id)) { return false; }
            System.DateTime held = System.DateTime.MinValue;
            if (country.ElectionHistory != null)
            {
                foreach (Elections.ElectionRecord e in country.ElectionHistory) { if (Elections.NationalElection.CountsParliament(e.Method) && e.Date < CurrentDate && e.Date > held) { held = e.Date; } }
            }
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate)
            {
                // An extra election ordered and not yet held: the procedure waits for it - unless an election was held after the order (the ordinary one
                // falling first), which elected the new Riksdag; the order lapses with it and that election's round opens (a premise, stated - §646's
                // second reader: waiting on, the government would govern the new chamber undischarged until the extra election).
                if (held == System.DateTime.MinValue || held <= _extraElectionOrderedOn) { return false; }
                g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the election of {held:yyyy-MM-dd} was held before the extra election ordered for {_extraElectionDate:yyyy-MM-dd}; the order lapses with it");
                _extraElectionDate = System.DateTime.MinValue; _extraElectionOrderedOn = System.DateTime.MinValue;
            }
            // §705 (the review's defect 6): in the Bundestag a government formed ON the polling day was formed by that election. Before §705 a German
            // election formed its government at once, dated the polling day; a save made after one would reopen the chancellor's election on load,
            // discharge the sitting government backdated to the convening and re-arm its arrival budget. No Bundestag install falls on a polling day
            // (the convening is thirty days on); the Riksdag's keeps `<` - a round voting on a polling day installs a government the new chamber's
            // round must still follow (SpeakerRoundDiagnostic (4) caught the first cut, which applied `<=` to both).
            if (held == System.DateTime.MinValue || held < g.FormedOn || (held == g.FormedOn && IsBundestag(country.Id)) || held <= g.ProcedureResumedAfter) { return false; }
            g.ProcedureResumedAfter = held;
            if (g.Round != null && g.Round.Open)
            {
                // A round still open when an election is held ends with it: the new chamber's round reads the new election's declarations and order,
                // and a motion's refusals end at the election (§641).
                g.Round.Stage = Elections.RoundStage.Concluded;
                g.Round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the election of {held:yyyy-MM-dd} ends the round - {(IsBundestagRound(g.Round) ? "the new Bundestag's chancellor election opens" : "the new Riksdag's round opens")}");
            }
            OpenSpeakerRound(country, Elections.WorldClock.VintageOfElection(country.Id, held), $"after the election of {held:yyyy-MM-dd}", electionDay: held);   // §641: a motion's refusals end at the election; §653: the election's own day's declarations
            return true;
        }

        /// <summary>Premise 1: the player, asked, tables a proposal - every invited party must accept it (premise 3: revise and re-offer); tabled, the vote comes on the fourth day.</summary>
        public bool SubmitFormation(CountryId countryId, Elections.FormationProposal proposal, out Elections.ProposalVerdict verdict, out string refusedBecause)
        {
            System.Func<Elections.FormationProposal> copy = CopiesForLedger(proposal);   // FORK-2: the proposal as made, a fresh copy for each except-world
            string why = null;
            Elections.ProposalVerdict given = null;
            string asked = RoundOf(countryId)?.Asked;   // FORK-2 (the fix review's M2): the round the answer is to - the party asked
            bool acted = Answered(new PlayerAnswer("a formation proposal", countryId, sim => copy != null && sim.RoundOf(countryId)?.Asked == asked && sim.SubmitFormation(countryId, copy(), out _, out _), answersAWait: true),
                () => SubmitFormationNow(countryId, proposal, out given, out why));
            verdict = given;
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="SubmitFormation"/>'s act.</summary>
        private bool SubmitFormationNow(CountryId countryId, Elections.FormationProposal proposal, out Elections.ProposalVerdict verdict, out string refusedBecause)
        {
            verdict = null;
            Country country = _world?.GetCountry(countryId);
            Elections.SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null || round.Stage != Elections.RoundStage.PlayerAsked) { refusedBecause = IsBundestag(countryId) ? "THE BUNDESPRÄSIDENT HAS NOT ASKED YOUR PARTY" : "THE SPEAKER HAS NOT ASKED YOUR PARTY"; return false; }
            if (proposal == null || proposal.Formateur != country.PlayerPartyAbbrev) { refusedBecause = "THE PROPOSAL MUST BE YOUR PARTY'S"; return false; }
            verdict = PreviewFormation(countryId, proposal);
            if (verdict.Investiture == null) { refusedBecause = verdict.Reason?.ToUpperInvariant() ?? "THE PROPOSAL IS NOT WELL FORMED"; return false; }
            if (!verdict.AllAccept) { refusedBecause = "NOT EVERY INVITED PARTY ACCEPTS - REVISE AND OFFER AGAIN"; return false; }
            refusedBecause = null;
            round.PlayerStood = true;   // §705: the player's candidate has stood - it may stand again in the ballot the most votes win
            TableAndVote(country, round, proposal);
            return true;
        }

        /// <summary>§647: the answer the player's proposal would get if tabled now - the call <see cref="SubmitFormation"/> makes, so the sheet never
        /// shows a verdict the tabling would not reach (the round's refusal lines, the player's party named). Freezes the supporters' tabled demands.</summary>
        public Elections.ProposalVerdict PreviewFormation(CountryId countryId, Elections.FormationProposal proposal)
        {
            Country country = _world?.GetCountry(countryId);
            Elections.SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null || proposal == null) { return null; }
            proposal.FreezeTabled(country, CurrentDate, _world);
            return Elections.Formateur.Answer(country, proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), country.PlayerPartyAbbrev);
        }

        // §706 (Elias's ruling of 2026-10-01): the Treasury lock is lifted - Finance is a portfolio like any other and can go to a partner (the
        // record: the SPD's Klingbeil under Merz in 2025, the FDP's Lindner under Scholz in 2021); §647's HoldsTreasury and its two refusals are gone.

        /// <summary>R5: the player, asked, passes - the Speaker asks the next party; nothing is voted, nothing counts.</summary>
        public bool PassFormation(CountryId countryId, out string refusedBecause)
        {
            string why = null;   // FORK-2 (the review's H1): given again in every except-world on its day
            string asked = RoundOf(countryId)?.Asked;   // FORK-2 (the fix review's M2): the round the answer is to - the party asked
            bool acted = Answered(new PlayerAnswer("a formation passed", countryId, sim => sim.RoundOf(countryId)?.Asked == asked && sim.PassFormation(countryId, out _), answersAWait: true), () => PassFormationNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="PassFormation"/>'s act.</summary>
        private bool PassFormationNow(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            Elections.SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null || round.Stage != Elections.RoundStage.PlayerAsked) { refusedBecause = IsBundestag(countryId) ? "THE BUNDESPRÄSIDENT HAS NOT ASKED YOUR PARTY" : "THE SPEAKER HAS NOT ASKED YOUR PARTY"; return false; }
            refusedBecause = null;
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} does not form a government");
            round.PlayerPassed = true;   // §715 (the review's decision): a pass keeps the player's party off the ballot the most votes win; never asked, it stands like any party
            if (IsBundestagRound(round) && round.Phase >= 2 && !round.StoodInPhase2.Contains(country.PlayerPartyAbbrev)) { round.StoodInPhase2.Add(country.PlayerPartyAbbrev); }   // §706: a pass in the fourteen days nominates no one
            AskNext(country, round);
            return true;
        }

        /// <summary>Premise 5: the player answers an offer - accepted, the proposal is tabled; declined, the party stays in opposition and the asked party
        /// proposes again without it, the Speaker moving on where nothing passes without the player's seats.</summary>
        public bool AnswerOffer(CountryId countryId, bool accept, out string refusedBecause)
        {
            string why = null;   // FORK-2: the answer to the offer given again in every except-world on its day
            string asked = RoundOf(countryId)?.Asked;   // FORK-2 (the fix review's M2): the offer the answer is to - the party offering
            bool acted = Answered(new PlayerAnswer(accept ? "an offer accepted" : "an offer declined", countryId, sim => sim.RoundOf(countryId)?.Asked == asked && sim.AnswerOffer(countryId, accept, out _), answersAWait: true),
                () => AnswerOfferNow(countryId, accept, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="AnswerOffer"/>'s act.</summary>
        private bool AnswerOfferNow(CountryId countryId, bool accept, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            Elections.SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null || round.Stage != Elections.RoundStage.OfferToPlayer) { refusedBecause = "NO OFFER WAITS ON YOUR PARTY"; return false; }
            refusedBecause = null;
            if (accept)
            {
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} accepts {PoliSim.Wording.EnglishText.Possessive(round.Asked, false, true)} offer");
                TableAndVote(country, round, round.Proposal);
                return true;
            }
            round.Declines.Add(country.PlayerPartyAbbrev + ">" + round.Asked);
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {country.PlayerPartyAbbrev} declines {PoliSim.Wording.EnglishText.Possessive(round.Asked, false, true)} offer and stays in opposition");
            Elections.FormationProposal without = DraftProposal(country, round, round.Asked);
            Elections.ProposalVerdict verdict = Involves(without, country.PlayerPartyAbbrev) ? null : Elections.Formateur.Answer(country, without, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), country.PlayerPartyAbbrev);
            // §712 (the review's defect 2): in the fourteen days the ballot decides, not the investiture - the government without the player is tabled
            // where every invited party accepts it and it splits no group, and its candidate meets the nominations standing
            bool personBallot = IsBundestagRound(round);   // §713: every phase
            if (verdict != null && (personBallot ? verdict.AllAccept && verdict.Investiture != null && !verdict.Investiture.SplitsJointGroup : verdict.Passes)) { TableAndVote(country, round, without); return true; }
            round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {round.Asked} cannot form without {PoliSim.Wording.EnglishText.Possessive(country.PlayerPartyAbbrev, false, true)} seats - {(IsBundestagRound(round) ? (round.Phase <= 1 ? "the Bundespräsident proposes the next party's candidate" : "the next nomination standing is asked") : "the Speaker moves on")}");
            if (IsBundestagRound(round) && round.Phase >= 2 && !round.StoodInPhase2.Contains(round.Asked)) { round.StoodInPhase2.Add(round.Asked); }   // §706: its nomination has had its turn
            AskNext(country, round);
            return true;
        }

        /// <summary>The Speaker discharges the prime minister [RF-R:6:7] and every minister with them [RF-R:6:9]; they serve on as a caretaker [RF-R:6:11]; the Speaker's round opens on the sitting chamber [RF-R:6:4] on the week's last day, dated as §646 builds it (2021's ran nine days, 29 June to 7 July).</summary>
        private void DischargeAndRound(Country country)
        {
            Elections.GovernmentRecord fallen = country.Government;
            fallen.Discharge(CurrentDate);
            fallen.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the Speaker discharged the prime minister ({fallen.PmParty}) and the government (RF 6 kap. 7 §, 9 §); the ministers serve on as a caretaker (6 kap. 11 §)");
            // The player's party moved the motion: it will not carry the prime minister it brought down in the round - its own choice, made by moving it.
            // An AI party's refusal is not added (the 2021 precedent: a party that brought the prime minister down tolerated his re-election).
            // §641 (the reader): the refusal stands until the next election - carried from the fallen government to the one the round forms, so a later
            // motion's round cannot hand back the prime minister the player brought down.
            var refusals = new List<string>(fallen.StandingRefusals);
            if (!string.IsNullOrEmpty(fallen.NoConfidenceMover) && !string.IsNullOrEmpty(fallen.PmParty) && fallen.NoConfidenceMover == country.PlayerPartyAbbrev)
            {
                string refusal = fallen.NoConfidenceMover + ">" + fallen.PmParty;
                if (!refusals.Contains(refusal)) { refusals.Add(refusal); }
            }
            // §646 (R2): the Speaker's round, dated, on the sitting chamber's declarations - it installs what wins its investiture, or breaks off at
            // RF 6:5's limit to an extra election (§636's single round was the four proposals' answer at once).
            OpenSpeakerRound(country, Elections.GovernmentFormation.SittingVintage(country), $"after the discharge of {PoliSim.Wording.EnglishText.Possessive(fallen.PmParty, false, true)} government", refusals, midTerm: true);   // §653: the mid-term reading
        }

        /// <summary>PS-3k (§638): a record's shift by party key as one per the setup's parties, or null where none is stored (an older campaign replays as it ran).</summary>
        private static double[] RecordShiftFor(Elections.CampaignRun.Setup setup, System.Collections.Generic.Dictionary<string, double> byParty)
        {
            if (byParty == null || byParty.Count == 0) { return null; }
            // PL-C (DECLARED): a Polish run-off reads no record - the first vote's shares already carried it, and B4's transfer from them is the run-off's prior
            if (setup.Contest == Elections.CampaignContest.PresidentialRunOff) { return null; }
            var shift = new double[setup.Parties.Length];
            for (int i = 0; i < shift.Length; i++) { shift[i] = byParty.TryGetValue(setup.Parties[i].Name, out double by) ? by : double.NaN; }   // NaN: not named, absorbs (a 0 would name it and leave no one to absorb - the §638 film's finding)
            return shift;
        }

        /// <summary>A record's shifts in one line, for the logs.</summary>
        private static string RecordShiftLine(System.Collections.Generic.Dictionary<string, double> byParty)
        {
            if (byParty == null || byParty.Count == 0) { return "no party carries it"; }
            var parts = new System.Collections.Generic.List<string>();
            foreach (System.Collections.Generic.KeyValuePair<string, double> kv in byParty) { parts.Add(kv.Key + " " + (kv.Value * 100.0).ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture) + " pp"); }
            return string.Join(", ", parts);
        }

        /// <summary>A-LOOP q15: the record's components for the logs - the economic vote and the approval term per party, and the approval term's
        /// judgement (net approval then and now, b, its size for the government side); A-LOOP q13: the fiscal term per party and its judgement (the two
        /// balance variables, delta-z, sigma_v, its size for the government side).</summary>
        private static string RecordPartsLine(Elections.EconomicVote.RecordParts parts)
            => " (economy: " + RecordShiftLine(parts.Economy) + "; approval: " + RecordShiftLine(parts.Approval) + " - " + Elections.ApprovalVote.Describe(parts.ApprovalReading)
               + "; fiscal: " + RecordShiftLine(parts.Fiscal) + " - " + Elections.FiscalVote.Describe(parts.FiscalReading) + ")";

        /// <summary>
        /// One day of the player's campaign - and §752 (Elias's ruling A1): the count reads the ELECTION-DAY value, so before the campaign's last day
        /// (the eve; polling day itself is never stepped - `CampaignClock`) the government's record is judged again over its term as of that day (the
        /// figures published before it - <see cref="Elections.PerceivedPerformance.OverTerm"/>) and carried into the day the count is taken from. The
        /// days before it keep the opening's judgement, the electorate's running reading. Live, the count's judgement is STORED on the record
        /// (<see cref="Elections.PlayerCampaignRecord.CountRecordShift"/>) the moment it is made; a load that replays the campaign
        /// (<see cref="RestoreCampaign"/>) replays the stored one and never re-judges - the government and the chamber a save holds may have moved
        /// since (the review's defect 1). A record written before §752 carries none and replays its last day on the opening's, as its build did.
        /// </summary>
        private void StepPlayerCampaignDay(Elections.PlayerCampaignRecord record, bool live)
        {
            if (PlayerCampaign.Day == PlayerCampaign.TotalDays - 1 && PlayerCountryId.HasValue && record != null)
            {
                if (record.CountRecordShift != null)
                {
                    PlayerCampaign.Setup = PlayerCampaign.Setup.WithRecordShift(RecordShiftFor(PlayerCampaign.Setup, record.CountRecordShift));
                }
                else if (live)
                {
                    Country recordCountry = _world.GetCountry(PlayerCountryId.Value);
                    if (recordCountry != null && recordCountry.Government != null)
                    {
                        System.DateTime countDay = PlayerCampaign.Setup.Calendar.CampaignStart.AddDays(PlayerCampaign.Day);
                        Elections.EconomicVote.RecordParts parts = Elections.EconomicVote.RecordPartsOverTerm(recordCountry, countDay, PlayerCampaign.Setup.Calendar.ElectionDate);   // A-LOOP q15: the economy and the approval term, one record
                        record.CountRecordShift = parts.Total;
                        PlayerCampaign.Setup = PlayerCampaign.Setup.WithRecordShift(RecordShiftFor(PlayerCampaign.Setup, record.CountRecordShift));
                        UnityEngine.Debug.Log($"RECORD: the government's record judged for the count on {countDay:yyyy-MM-dd} for {PlayerCountryId.Value} - {Elections.EconomicVote.Describe(parts.Term)}: " + RecordShiftLine(record.CountRecordShift) + RecordPartsLine(parts));
                    }
                }
            }
            Elections.CampaignRun.StepDay(PlayerCampaign);
        }

        /// <summary>§770 (PS-5 item C4): a round of the player's country's presidential election where today is its day - the first vote on the live
        /// prediction as the Sejm's vote reads it (the government's record shifting it), the run-off on the first vote it follows. The elections, as the
        /// Sejm's, are the player's country's alone.</summary>
        private void HoldPresidentialRound()
        {
            if (!PlayerCountryId.HasValue || Elections.TwoRoundElection.RuleOf(PlayerCountryId.Value) == null) { return; }
            Country country = _world.GetCountry(PlayerCountryId.Value);
            if (country == null || !Elections.PresidentialElection.IsRoundDay(country, CurrentDate, out bool runOff)) { return; }
            Dictionary<string, double> predicted = null;
            // PL-C (K168): where the player's campaign ran up to this round (`LiveCampaignSetup.TryPoland`), the first vote reads its final shares as its poll and
            // the run-off its share of the two; with no campaign, the prediction and B4's transfer as before
            bool campaigned = PlayerCampaignResult != null && CampaignRecord != null && CampaignRecord.ElectionDate == CurrentDate && PlayerCampaign != null && PlayerCampaign.Finished;
            Elections.CampaignContest ran = campaigned ? PlayerCampaign.Setup.Contest : Elections.CampaignContest.Chamber;
            double runOffShare = double.NaN;
            if (!runOff && ran == Elections.CampaignContest.PresidentialFirstVote)
            {
                predicted = new Dictionary<string, double>();
                for (int p = 0; p < PlayerCampaign.Setup.Parties.Length; p++) { predicted[PlayerCampaign.Setup.Parties[p].Name] = PlayerCampaignResult.FinalShares[p]; }
                Debug.Log("PRESIDENTIAL: the first vote reads the campaign's final shares - " + string.Join(" ", System.Array.ConvertAll(PlayerCampaignResult.FinalShares, v => v.ToString("P1", System.Globalization.CultureInfo.InvariantCulture))));
            }
            else if (runOff && ran == Elections.CampaignContest.PresidentialRunOff)
            {
                runOffShare = RunOffCampaignShare(country, PlayerCampaign.Setup, PlayerCampaignResult);
            }
            if (!runOff && predicted == null && !Elections.NationalElection.TryPredictShares(country.Id, out predicted, Elections.EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country))   // PS-6 US-26: the chamber's poll on its history rolled forward
            {
                predicted = null;
                Debug.LogWarning("PRESIDENTIAL: no prediction could be made on the first vote's day - nothing is held, and the office stands as it is");
            }
            Elections.PresidentialElection.Contest held = Elections.PresidentialElection.HoldRounds(country, CurrentDate, predicted, runOffShare);
            if (held != null && !runOff && ran == Elections.CampaignContest.PresidentialFirstVote)
            {
                // K167: what each committee spent on the first round, kept with the contest - the run-off's chest is the one limit less it (a save carries it)
                held.FirstRoundSpent = new Dictionary<string, double>();
                for (int p = 0; p < PlayerCampaign.Setup.Parties.Length && p < PlayerCampaignResult.Parties.Length; p++)
                {
                    double limitUnits = PlayerCampaign.Setup.StandingShare(p) > 0.0 ? Elections.LiveCampaignSetup.WarChest : 0.0;   // the committee's limit in the purse's units - a run-up's spending counts against it too
                    held.FirstRoundSpent[PlayerCampaign.Setup.Parties[p].Name] = System.Math.Max(0.0, limitUnits - PlayerCampaignResult.Parties[p].MoneyLeft);
                }
            }
            if (held != null) { Debug.Log("PRESIDENTIAL: " + held.Line); }
            if (held != null && !runOff && held.RunOffPending() && PlayerPartyIndexForCampaign() >= 0)
            {
                // PL-C (the review's L6, L7): the run-off's campaign opens on the first vote's own day, after the count - its day 0 stepped tonight, so the player's
                // queue reaches its day 1 tomorrow; a player whose party backs neither finalist runs no run-off campaign, and is told so
                if (PlayerBacksAFinalist(country, held)) { AdvanceCampaign(); }
                else { Debug.Log($"PRESIDENTIAL: {country.Id} - your party, {country.PlayerPartyAbbrev}, backs neither finalist: the run-off of {held.RunOffOn:yyyy-MM-dd} is {held.RunOffA} against {held.RunOffB}, and its campaign is theirs alone"); }
            }
            NotePlayerVerdict(country, held);   // SP-7b: what the round means for the player, where his candidate stood
        }

        /// <summary>PL-C (K168): the run-off campaign's share of the two for the pending contest's first finalist (%) - its party's final share over the two finalists'
        /// parties'; NaN where the contest or a finalist's party cannot be found in the staging (B4's transfer then counts).</summary>
        private static double RunOffCampaignShare(Country country, Elections.CampaignRun.Setup setup, Elections.CampaignRun.Result result)
        {
            Elections.PresidentialElection.Contest contest = country.PresidentialElections.Find(c => c.RunOffPending() && c.RunOffOn.Date == setup.Calendar.ElectionDate);
            if (contest == null || result == null) { return double.NaN; }
            string a = contest.Field.Find(c => c.Name == contest.RunOffA)?.Party, b = contest.Field.Find(c => c.Name == contest.RunOffB)?.Party;
            int ia = -1, ib = -1;
            for (int p = 0; p < setup.Parties.Length; p++) { if (setup.Parties[p].Name == a) { ia = p; } if (setup.Parties[p].Name == b) { ib = p; } }
            if (ia < 0 || ib < 0 || ia == ib) { return double.NaN; }
            double two = result.FinalShares[ia] + result.FinalShares[ib];
            return two > 0.0 ? 100.0 * result.FinalShares[ia] / two : double.NaN;
        }

        /// <summary>
        /// PS-6 US-8 (`docs/specs/USA_STAGE_PLAN.md`): THE US PRESIDENTIAL COUNT, on the day loop - the player's country's alone, as the record is (R-US5 (a): the
        /// count is a pure function of the world, <see cref="Elections.UsPresidentialElection.Count"/>; PS-8 gives an AI USA its gate). On the count's day of a
        /// contest no one won outright, the House chooses by states first (R-US3 (a), <see cref="Elections.UsPresidentialElection.HoldHouseChoice"/>). On the
        /// polling day (3 U.S.C. §21(1)), not yet counted, with no campaign run the count reads the live prediction as Poland's first vote does - the government's record over its term
        /// shifting it (§752), the same call E1's reference world reads its poll by, so the factors meet the poll they were fitted on - and the contest is added to
        /// the country: the president-elect takes office at the oath (<see cref="SeatTheRecordOnItsDate"/>), and from the polling day the record seats no president.
        /// PS-6 US-30: where the player's campaign ran up to this polling day (`LiveCampaignSetup.TryUsa`, on the jurisdictions), the count reads the run's final
        /// shares (<see cref="TryPlayerCampaignShares"/>) - the vote model the prediction is made of, moved by the campaign, the government's record judged for
        /// the count on its last day (§752) - then the states (R-US14), then the electors; with no campaign, the prediction as before. A day the
        /// count cannot be made (no prediction; E1's factors not fitted) holds nothing and says so: the record's president stays seated by date. PS-6 US-31
        /// (R-US16 (b), I3): where the campaign ran, each state is counted with its ground game and the declared regional noise, and the state effect enters
        /// the electors, the Senate's states and the House swing alike (<see cref="Elections.UsGroundGame"/>). The count draws no number from a counted stream
        /// (the regional noise is keyed by the master seed, the country and the day), so no stream moves. Nothing here is shown as a night: the verdict lands at
        /// once (D-US builds the night).
        /// </summary>
        private void HoldUsPresidentialCount()
        {
            if (!PlayerCountryId.HasValue || !Elections.UsPresidentialElection.ElectsIn(PlayerCountryId.Value)) { return; }
            Country country = _world?.GetCountry(PlayerCountryId.Value);
            if (country == null) { return; }
            Elections.UsPresidentialElection.Contest chosen = Elections.UsPresidentialElection.HoldHouseChoice(country, CurrentDate, out string choice);
            if (chosen != null) { Debug.Log($"PRESIDENTIAL: {country.Id} - the House's choice: {choice}"); }   // its own line only - the count's was logged on its polling day
            if (Elections.UsCongressElection.IsMidterm(CurrentDate)) { HoldUsMidtermCount(country); return; }   // PS-6 US-27: a midterm counts no president
            if (!Elections.UsPresidentialElection.IsPollingDay(country, CurrentDate)) { return; }
            Elections.UsGroundGame.Campaign groundGame = null;   // PS-6 US-31: the campaign's ground game, counted state by state where it ran (R-US16 (b), I3)
            if (TryPlayerCampaignShares(out Dictionary<string, double> poll))
            {
                var counted = new System.Text.StringBuilder();
                foreach (KeyValuePair<string, double> kv in poll) { counted.Append(' ').Append(kv.Key).Append(' ').Append((100.0 * kv.Value).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)).Append(" %"); }
                Debug.Log($"PRESIDENTIAL: {country.Id} - the campaign ran to the polling day, and its final shares are counted:{counted}");
                groundGame = Elections.UsGroundGame.Of(PlayerCampaign.Setup, PlayerCampaignResult);
                if (groundGame == null) { Debug.LogWarning($"PRESIDENTIAL: {country.Id} - the campaign ground game could not be read (no REP or DEM staged, or its mobilization does not match the jurisdictions); the states are counted with the regional noise alone"); }
            }
            else if (!Elections.NationalElection.TryPredictShares(country.Id, out poll, Elections.EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country))   // PS-6 US-26: the House history rolled forward - the game's own counts before the day
            {
                Debug.LogWarning($"PRESIDENTIAL: {country.Id} - no prediction could be made on the polling day {CurrentDate:yyyy-MM-dd}; nothing is counted, and the record's president stays seated by date");
                return;
            }
            // Elias's ruling of 2026-10-23 ("always, all countries"): a polling day no campaign ran up to is counted state by state too - no contact anywhere,
            // the same keyed regional noise (UsGroundGame.NoCampaign); where its jurisdictions cannot be read the states stand as derived and the line says why
            if (groundGame == null && Elections.ParliamentaryCount.SigmaPp > 0.0)   // inside ParliamentaryCount.NoiseAtZero (a measurement at sigma 0 by design) the derived states stand exactly, as before
            {
                groundGame = Elections.UsGroundGame.NoCampaign(CurrentDate, out string noCampaignWhy);
                if (groundGame == null && !IsCounterfactual) { Debug.LogWarning($"PRESIDENTIAL: {country.Id} - the jurisdictions of {CurrentDate:yyyy-MM-dd} could not be read ({noCampaignWhy}); the states are counted as derived"); }
            }
            Elections.UsPresidentialElection.Contest held = Elections.UsPresidentialElection.Hold(country, CurrentDate, poll, groundGame, out string why);
            if (held == null)
            {
                // its own prefix: a reader that picks count lines by "the count of" (the diagnostics' filters over what their listeners collect) never takes a failure for a count
                Debug.LogWarning($"PRESIDENTIAL: {country.Id} - no count on {CurrentDate:yyyy-MM-dd}: {why}; nothing is counted, and the record's president stays seated by date");
                return;
            }
            Debug.Log($"PRESIDENTIAL: {country.Id} - {held.Line}");
            // PS-6 US-16: the House and the Senate class on the same day, on the same counted poll and the night's states - seated on 3 January (SeatTheRecordOnItsDate)
            Elections.UsCongressElection.Contest congress = Elections.UsCongressElection.Hold(country, CurrentDate, Elections.UsPresidentialElection.CountedPoll(country, CurrentDate, poll), held, out string congressWhy);
            if (congress != null) { Debug.Log($"CONGRESS: {country.Id} - {congress.Line}"); }
            else
            {
                // what stays seated: the game's previous Congress where one sits (the record seats no House in this world any more), else the record's by date
                Debug.LogWarning(Elections.UsCongressElection.TryInOffice(country, CurrentDate, out Elections.UsCongressElection.Contest sitting)
                    ? $"CONGRESS: {country.Id} - no Congress count on {CurrentDate:yyyy-MM-dd}: {congressWhy}; the House the game elected on {sitting.PollingDay:yyyy-MM-dd} sits on, and the Senate as seated"
                    : $"CONGRESS: {country.Id} - no Congress count on {CurrentDate:yyyy-MM-dd}: {congressWhy}; the record's House and Senate stay seated by date");
            }
        }

        /// <summary>
        /// PS-6 US-27 (`docs/specs/USA_STAGE_PLAN.md` US-27; 2 U.S.C. §7): THE MIDTERM COUNT on the day loop - on a midterm polling day, not yet counted, the House
        /// and the Senate class whose terms expire, with no presidential count (<see cref="Elections.UsCongressElection.HoldMidterm"/>; R-US20 (b)'s fitted term on
        /// the president's party), seated on 3 January (<see cref="SeatTheRecordOnItsDate"/>). No campaign runs for a midterm (DECLARED, the conservative reading -
        /// <see cref="CurrentCampaignCalendar"/>), so the count reads the live prediction as the presidential count does with no campaign: the government's record
        /// over its term shifting it (§752), on the history rolled forward (US-26), the day's counted poll (<see cref="Elections.UsPresidentialElection.CountedPoll"/>).
        /// The player's country alone, as every US count; the forks reach it through the same day loop. A day it cannot be made holds nothing and says so.
        /// </summary>
        private void HoldUsMidtermCount(Country country, bool quiet = false)   // PS-8 (L7 of the s938 review): an AI USA in a counterfactual world counts its midterm silently
        {
            if (!Elections.UsCongressElection.IsPollingDay(country, CurrentDate)) { return; }
            // the second review's M1 (measurement only; before the count's own prediction, so the regional readout left is the count's): the record's components in V
            // beside the fitted term - the overlap I4's revisit clause governs
            string recordWords = IsCounterfactual ? null : Elections.UsCongressElection.RecordInVWords(country, CurrentDate, Elections.UsCongressElection.MidtermPresidentsParty(country, CurrentDate), Elections.UsCongressElection.MidtermTermPoints);   // PS-6 K13: the party the count's term is applied to
            if (!Elections.NationalElection.TryPredictShares(country.Id, out Dictionary<string, double> poll, Elections.EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country))
            {
                if (!quiet) Debug.LogWarning(Elections.UsCongressElection.TryInOffice(country, CurrentDate, out Elections.UsCongressElection.Contest standing)
                    ? $"CONGRESS: {country.Id} - no prediction could be made on the midterm {CurrentDate:yyyy-MM-dd}; nothing is counted, and the House the game elected on {standing.PollingDay:yyyy-MM-dd} sits on"
                    : $"CONGRESS: {country.Id} - no prediction could be made on the midterm {CurrentDate:yyyy-MM-dd}; nothing is counted, and the record's House and Senate stay seated by date");
                return;
            }
            Elections.UsCongressElection.Contest midterm = Elections.UsCongressElection.HoldMidterm(country, CurrentDate, Elections.UsPresidentialElection.CountedPoll(country, CurrentDate, poll),
                Elections.UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, CurrentDate.Year), out string why);
            if (midterm != null) { if (!quiet) { Debug.Log($"CONGRESS: {country.Id} - {midterm.Line}" + (recordWords != null ? "; " + recordWords : string.Empty)); } }
            else
            {
                if (!quiet) Debug.LogWarning(Elections.UsCongressElection.TryInOffice(country, CurrentDate, out Elections.UsCongressElection.Contest sitting)
                    ? $"CONGRESS: {country.Id} - no midterm count on {CurrentDate:yyyy-MM-dd}: {why}; the House the game elected on {sitting.PollingDay:yyyy-MM-dd} sits on, and the Senate as seated"
                    : $"CONGRESS: {country.Id} - no midterm count on {CurrentDate:yyyy-MM-dd}: {why}; the record's House and Senate stay seated by date");
            }
        }

        /// <summary>PS-6 US-30: the player's finished campaign's final shares by party key, where the campaign ran up to TODAY's polling day (the election
        /// controller's own condition for a parliamentary count: the record dated today, the run still held); false otherwise. The shares are the run's own,
        /// in its setup's party order - the vote model's keys for a staged country.</summary>
        public bool TryPlayerCampaignShares(out Dictionary<string, double> shares)
        {
            shares = null;
            Elections.CampaignRun.Result result = PlayerCampaignResult;
            if (result == null || PlayerCampaign == null || CampaignRecord == null || CampaignRecord.ElectionDate != CurrentDate) { return false; }
            Elections.CampaignRun.PartySetup[] parties = PlayerCampaign.Setup.Parties;
            if (parties == null || result.FinalShares == null || result.FinalShares.Length != parties.Length) { return false; }
            shares = new Dictionary<string, double>();
            for (int i = 0; i < parties.Length; i++) { shares[parties[i].Name] = result.FinalShares[i]; }
            return true;
        }

        /// <summary>The player's country's next polling day on or after today, false where its election calendar is not modelled.</summary>
        public bool TryPlayerPollingDay(out System.DateTime pollingDay)
        {
            pollingDay = System.DateTime.MinValue;
            bool ordinary = PlayerCountryId.HasValue && Elections.WorldClock.TryNextPollingDay(PlayerCountryId.Value, CurrentDate, out pollingDay);
            // PS-7 (K206): Italy's calendar re-anchors on the game's last count - an art. 88 dissolution starts a new five-year term (art. 60)
            if (ordinary && PlayerCountryId.Value == CountryId.Italy && LastItalianCount(out System.DateTime lastCount)) { pollingDay = Elections.WorldClock.ItalyOrdinaryAfter(lastCount, CurrentDate); }
            // PS-3i (§636): an ordered EXTRA election is the next polling day where it falls first; the four-year cycle carries on beside it.
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate && (!ordinary || _extraElectionDate < pollingDay)) { pollingDay = _extraElectionDate; return true; }
            return ordinary;
        }

        /// <summary>The campaign window and calendar for the current day: the run-up and the eight weeks before the player's country's next polling day, or null where none is modelled.
        /// PS-6 US-27 (DECLARED, the conservative reading, asked): none for a US midterm - US-30 staged the presidential campaign on the 51 jurisdictions, and nothing
        /// rules a campaign for the House and a Senate class, so a midterm runs no run-up and no campaign and is counted on the prediction.</summary>
        private Elections.CampaignCalendar? CurrentCampaignCalendar()
        {
            // PL-C (K168): Poland's presidential rounds campaign too - whichever of the Sejm's and the presidency's polling days comes first opens its window
            Elections.CampaignCalendar? chamber = ChamberCampaignCalendar();
            Elections.CampaignCalendar? presidential = PolishPresidentialCalendar();
            if (!presidential.HasValue) { return chamber; }
            return !chamber.HasValue || presidential.Value.ElectionDate < chamber.Value.ElectionDate ? presidential : chamber;
        }

        /// <summary>
        /// PL-C (K168): the player's next Polish presidential round's campaign calendar - a pending run-off's (the two weeks from the first vote, no run-up,
        /// `LiveCampaignSetup.PolandRunOffCalendar`), else the next first vote's (the standard windows from the world's start); null outside Poland, where none
        /// is ahead, or - DECLARED - where the player plays no party in the cast: a world with no player's party (E1's reference world, a no-player dump) counts
        /// its presidential rounds on the prediction as before, so the world the factors of record were fitted on is the world they are read on.
        /// </summary>
        private Elections.CampaignCalendar? PolishPresidentialCalendar()
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != CountryId.Poland || PlayerPartyIndexForCampaign() < 0) { return null; }
            Country country = _world?.GetCountry(CountryId.Poland);
            if (country == null) { return null; }
            Elections.CampaignCalendar? next = null;
            foreach (Elections.PresidentialElection.Contest c in country.PresidentialElections)
            {
                // L6 (the review): a run-off is campaigned only where the player's party backs a finalist - else the run-off is the finalists' alone, said on the first vote's day
                if (c.RunOffPending() && c.RunOffOn.Date >= CurrentDate.Date && PlayerBacksAFinalist(country, c) && (!next.HasValue || c.RunOffOn.Date < next.Value.ElectionDate)) { next = Elections.LiveCampaignSetup.PolandRunOffCalendar(c.RunOffOn); }
            }
            if (Elections.PresidentialElection.TryNextFirstVote(country, CurrentDate.Date, out System.DateTime firstVote, out System.DateTime _, out string _)
                && (!next.HasValue || firstVote < next.Value.ElectionDate))
            {
                next = Elections.CampaignCalendar.FromWorldStart(firstVote, EpochDate);
            }
            return next;
        }

        /// <summary>PL-C (the review's L6): whether the player's party backs one of <paramref name="contest"/>'s two finalists - the run-off's campaign is theirs alone.</summary>
        private static bool PlayerBacksAFinalist(Country country, Elections.PresidentialElection.Contest contest)
        {
            if (country == null || contest == null || string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return false; }
            string a = contest.Field.Find(x => x.Name == contest.RunOffA)?.Party, b = contest.Field.Find(x => x.Name == contest.RunOffB)?.Party;
            return country.PlayerPartyAbbrev == a || country.PlayerPartyAbbrev == b;
        }

        /// <summary>PL-C: the calendar a campaign record of <paramref name="electionDate"/> was stepped on, for its replay - a Polish run-off's two weeks where the
        /// world holds a first vote whose run-off falls that day, else the standard windows from the world's start (§697), as every record before PL-C replayed.</summary>
        private Elections.CampaignCalendar CampaignCalendarOfRecord(System.DateTime electionDate)
        {
            Country country = PlayerCountryId == CountryId.Poland ? _world?.GetCountry(CountryId.Poland) : null;
            if (country != null && country.PresidentialElections.Exists(c => c.RunOffA != null && c.RunOffOn.Date == electionDate.Date)) { return Elections.LiveCampaignSetup.PolandRunOffCalendar(electionDate); }
            return Elections.CampaignCalendar.FromWorldStart(electionDate, EpochDate);
        }

        /// <summary>The chamber's campaign calendar - the window before the player's country's next polling day (see <see cref="CurrentCampaignCalendar"/>).</summary>
        private Elections.CampaignCalendar? ChamberCampaignCalendar()
        {
            if (!TryPlayerPollingDay(out System.DateTime pollingDay)) { return null; }
            if (PlayerCountryId.HasValue && Elections.UsCongressElection.ElectsIn(PlayerCountryId.Value) && Elections.UsCongressElection.IsMidterm(pollingDay)) { return null; }
            // PS-3i (§636): an extra election's campaign runs from its decision to its polling day - no run-up, the campaign proper at most eight weeks (a premise).
            if (pollingDay == _extraElectionDate)
            {
                int weeks = System.Math.Max(0, System.Math.Min(Elections.CampaignCalendar.DefaultCampaignWeeks, (int)((_extraElectionDate - _extraElectionOrderedOn).TotalDays / 7)));
                return new Elections.CampaignCalendar(pollingDay, weeks, 0);
            }
            return Elections.CampaignCalendar.FromWorldStart(pollingDay, EpochDate);   // §697: a run-up never opens before the world began (the snap start's)
        }

        /// <summary>
        /// Called once per day after the date has advanced: begins the player's campaign on its first
        /// day and steps it through today; finishes it on the day after its last. Idle outside the
        /// window and for a country with no staged campaign. CL-1 (2026-09-12): the window opens at the
        /// PRE-campaign's first day - the run-up is stepped by <see cref="AdvancePreCampaign"/> for the
        /// player's party alone, and the campaign opens on its outcome.
        /// </summary>
        private void AdvanceCampaign()
        {
            if (!PlayerCountryId.HasValue) { return; }
            // A finished campaign's result stays readable until the next window opens; the running state is dropped once its election has
            // passed. Strictly AFTER polling day: the election is counted on polling day's own Update (the controller's CheckElection reads
            // PlayerCampaign and the Result then - PS-2, §619). §697 (the review's defect 3): dropped BEFORE the calendar is asked for - a
            // country whose last polling day on record has passed (Germany after its snap) has no next calendar, and the drop sat behind that
            // return, so a German game carried its finished campaign for the rest of the run.
            if (PlayerCampaign != null && CurrentDate > PlayerCampaign.Setup.Calendar.ElectionDate) { PlayerCampaign = null; PlayerPreCampaign = null; }
            Elections.CampaignCalendar? next = CurrentCampaignCalendar();
            if (!next.HasValue) { return; }   // no election calendar for this country yet - no run-up and no campaign, as there is no election
            Elections.CampaignCalendar calendar = next.Value;
            if (CurrentDate < calendar.PreCampaignStart || CurrentDate >= calendar.ElectionDate)
            {
                return;   // outside the window - which opens at the PRE-campaign's first day since CL-1
            }
            if (CurrentDate < calendar.CampaignStart) { AdvancePreCampaign(calendar); return; }
            if (PlayerCampaign == null || PlayerCampaign.Setup.Calendar.ElectionDate != calendar.ElectionDate)
            {
                // CL-1: a run-up that reached this day is finished first (a load can land here with days unstepped),
                // and its outcome is what the player's party brings to day 0. The record is the run-up's own when one
                // ran; otherwise (a load straight into the campaign, a record from before CL-1) a fresh one - and the
                // record first either way: the player's script reads its queue, so the Setup closes over it.
                Elections.PreCampaignRun.Outcome? brought = null;
                // §640 (the reader): the run-up is adopted only with its own election's record - a record dated to another polling day (an extra
                // election held in this election's run-up) is not the queue this run-up stepped, as the run-up's own guard already holds (§636).
                if (PlayerPreCampaign != null && PlayerPreCampaign.Calendar.ElectionDate == calendar.ElectionDate && CampaignRecord != null && CampaignRecord.ElectionDate == calendar.ElectionDate)
                {
                    while (!PlayerPreCampaign.Finished)
                    {
                        Elections.PreCampaignRun.StepDay(PlayerPreCampaign, PreCampaignDecisionsFor(CampaignRecord, PlayerPreCampaign, PlayerPreCampaign.Day));
                        CampaignRecord.PreCampaignDaysStepped = PlayerPreCampaign.Day;
                    }
                    brought = Elections.PreCampaignRun.Finish(PlayerPreCampaign);
                }
                Elections.PlayerCampaignRecord record = brought.HasValue ? CampaignRecord
                    : new Elections.PlayerCampaignRecord { ElectionDate = calendar.ElectionDate, StartDate = calendar.CampaignStart, DaysStepped = 0,
                        SalienceShift = EventSalience.Snapshot(_world.GetCountry(PlayerCountryId.Value)) };   // A-LOOP q5: the salience the window opened on
                if (!Elections.LiveCampaignSetup.TryFor(PlayerCountryId.Value, new (int, int, Elections.Scandal)[0], calendar,
                        out Elections.CampaignRun.Setup setup, out _, onVoteModelCompatibility: true,
                        playerParty: PlayerPartyIndexForCampaign(), playerScript: PlayerScriptOver(record), playerOutcome: brought,
                        playerScandalScript: PlayerScandalScriptOver(record), liveScandalRate: Elections.Scandals.LiveRatePerPartyDay, played: _world.GetCountry(PlayerCountryId.Value)))   // PS-6 US-26: staged on the history rolled forward
                {
                    return;   // no campaign staged for this country - LiveCampaignSetup says why
                }
                EventSalience.ApplyToCampaign(record.SalienceShift, setup.TrueSalience);   // A-LOOP q5 (§6 (2)): the campaign's relevance reads the salience its window opened on
                CampaignRecord = record;
                // PS-3k (§638, ruled): the government's record judged at the campaign's opening, stored on the record so a replay is the same campaign -
                // §752 (A1): over the government's TERM, the change since it took office, never levels; judged again for the count (StepPlayerCampaignDay).
                Country recordCountry = _world.GetCountry(PlayerCountryId.Value);
                if (record.RecordShift == null && recordCountry != null)
                {
                    Elections.EconomicVote.RecordParts parts = Elections.EconomicVote.RecordPartsOverTerm(recordCountry, CurrentDate, calendar.ElectionDate);   // A-LOOP q15: the economy and the approval term, one record
                    record.RecordShift = parts.Total;
                    UnityEngine.Debug.Log($"RECORD: the government's record judged at the campaign's opening for {PlayerCountryId.Value} - {Elections.EconomicVote.Describe(parts.Term)}: " + RecordShiftLine(record.RecordShift) + RecordPartsLine(parts));
                }
                setup = setup.WithRecordShift(RecordShiftFor(setup, record.RecordShift));
                System.Collections.Generic.Dictionary<SimulationRandom.Stream, int> atStart = SimulationRandom.CaptureDrawCounts();   // a stream that has never drawn is absent, and absent means 0
                foreach (SimulationRandom.Stream stream in CampaignStreams) { CampaignRecord.DrawCountsAtStart[stream] = atStart.TryGetValue(stream, out int n) ? n : 0; }
                PlayerCampaignResult = null;
                PlayerCampaign = Elections.CampaignRun.Begin(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi),
                    SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
                if (brought.HasValue)
                {
                    UnityEngine.Debug.Log($"CAMPAIGN: the run-up's outcome opens the campaign for {PlayerCountryId.Value} - chest {brought.Value.Money:F0}, {brought.Value.Offices.Length} office(s), {brought.Value.Staff.Length} staff, {brought.Value.TelevisionBuys} buy(s) planned; digest {brought.Value.Digest.Length} chars");
                }
            }
            if (PlayerCampaign.Day == 0) { UnityEngine.Debug.Log($"CAMPAIGN: began {calendar.CampaignStart:yyyy-MM-dd} for {PlayerCountryId.Value}, polling day {calendar.ElectionDate:yyyy-MM-dd} ({PlayerCampaign.TotalDays} days)"); }
            int todayIndex = (int)(CurrentDate - calendar.CampaignStart).TotalDays;
            while (PlayerCampaign.Day <= todayIndex && !PlayerCampaign.Finished)
            {
                StepPlayerCampaignDay(CampaignRecord, live: true);
                CampaignRecord.DaysStepped = PlayerCampaign.Day;
            }
            if (PlayerCampaign.Finished && PlayerCampaignResult == null)
            {
                PlayerCampaignResult = Elections.CampaignRun.Finish(PlayerCampaign); RecordCampaignLedger();
                if (PlayerCampaignResult.AbsorptionFallback != null) { UnityEngine.Debug.Log("CAMPAIGN: " + PlayerCampaignResult.AbsorptionFallback); }   // review 2's L2: a fallback is named, never silent
                UnityEngine.Debug.Log($"CAMPAIGN: finished {PlayerCampaign.TotalDays} days for {PlayerCountryId.Value}; final shares " + string.Join(" ", System.Array.ConvertAll(PlayerCampaignResult.FinalShares, v => v.ToString("P1", System.Globalization.CultureInfo.InvariantCulture))));
            }
        }

        /// <summary>
        /// CL-1: the run-up's day - the player's pre-campaign begun on the calendar's `PreCampaignStart` from the campaign's
        /// own staging (read, not run: no AI party moves before the campaign), then stepped through today on the record's
        /// queue. A country with no staged campaign has no run-up either, for the same reason it has no campaign.
        /// </summary>
        private void AdvancePreCampaign(Elections.CampaignCalendar calendar)
        {
            if (PlayerPreCampaign == null || PlayerPreCampaign.Calendar.ElectionDate != calendar.ElectionDate || CampaignRecord == null || CampaignRecord.ElectionDate != calendar.ElectionDate)   // PS-3i (§636, the reader): a run-up whose record is not this calendar's re-begins
            {
                int me = PlayerPartyIndexForCampaign();
                if (me < 0) { return; }
                if (!Elections.LiveCampaignSetup.TryFor(PlayerCountryId.Value, new (int, int, Elections.Scandal)[0], calendar,
                        out Elections.CampaignRun.Setup staged, out _, onVoteModelCompatibility: true, playerParty: me, played: _world.GetCountry(PlayerCountryId.Value)))   // PS-6 US-26: staged on the history rolled forward
                {
                    return;
                }
                var record = new Elections.PlayerCampaignRecord
                {
                    ElectionDate = calendar.ElectionDate, StartDate = calendar.PreCampaignStart, DaysStepped = 0,
                    PreCampaignDays = 7 * calendar.PreCampaignWeeks, PreCampaignDaysStepped = 0,
                    SalienceShift = EventSalience.Snapshot(_world.GetCountry(PlayerCountryId.Value)),   // A-LOOP q5: the salience the window opened on
                };
                EventSalience.ApplyToCampaign(record.SalienceShift, staged.TrueSalience);
                System.Collections.Generic.Dictionary<SimulationRandom.Stream, int> atStart = SimulationRandom.CaptureDrawCounts();
                record.DrawCountsAtStart[PreCampaignStream] = atStart.TryGetValue(PreCampaignStream, out int n0) ? n0 : 0;
                CampaignRecord = record;
                PlayerCampaign = null;
                PlayerCampaignResult = null;
                PlayerPreCampaign = Elections.PreCampaignRun.Begin(staged, me, SimulationRandom.For(PreCampaignStream), PlayerCountryId);   // K84: the country's postal tariff prices a letter
                UnityEngine.Debug.Log($"CAMPAIGN: the run-up began {calendar.PreCampaignStart:yyyy-MM-dd} for {PlayerCountryId.Value} - {PlayerPreCampaign.TotalDays} days to the campaign's opening on {calendar.CampaignStart:yyyy-MM-dd}, polling day {calendar.ElectionDate:yyyy-MM-dd}");
            }
            int todayIndex = (int)(CurrentDate - calendar.PreCampaignStart).TotalDays;
            while (PlayerPreCampaign.Day <= todayIndex && !PlayerPreCampaign.Finished)
            {
                Elections.PreCampaignRun.StepDay(PlayerPreCampaign, PreCampaignDecisionsFor(CampaignRecord, PlayerPreCampaign, PlayerPreCampaign.Day));
                CampaignRecord.PreCampaignDaysStepped = PlayerPreCampaign.Day;
            }
        }

        /// <summary>The record's queue for one run-up day, as the run-up steps it - keyed by the NEGATIVE day counted back from the campaign's first.</summary>
        private static System.Collections.Generic.List<Elections.PreCampaignRun.Decision> PreCampaignDecisionsFor(Elections.PlayerCampaignRecord record, Elections.PreCampaignRun.State pre, int day)
        {
            var list = new System.Collections.Generic.List<Elections.PreCampaignRun.Decision>();
            if (record == null || pre == null) { return list; }
            foreach (Elections.QueuedDecisionRecord q in record.QueuedFor(day - pre.TotalDays)) { list.Add(new Elections.PreCampaignRun.Decision(q.Kind, q.RegionIndex, q.Role)); }
            return list;
        }

        // ---- C-R4b step 4b: the player's hand on the campaign - a queue the party plays ----------------

        /// <summary>The player's party's index in the staged campaign's party order, or −1 (no party chosen, or a country with no staging).</summary>
        /// <summary>
        /// P2-4.3 (2026-09-02): the campaign's attribution ledger for the player's party - W-E7's inputs read from
        /// the finished run (each side's persuasion, the attacks received, the coverage, the total per party) and
        /// the setup's compatibility, prior and loyalty - kept here past the run's own clearing so election night
        /// can show it beside the count. Null when no live campaign ran to the election.
        /// </summary>
        public Elections.VoteAttribution.Ledger PlayerCampaignLedger { get; private set; }
        public string PlayerCampaignLedgerParty { get; private set; }

        private void RecordCampaignLedger()
        {
            PlayerCampaignLedger = null;
            PlayerCampaignLedgerParty = null;
            Elections.CampaignRun.Result result = PlayerCampaignResult;
            Elections.CampaignRun.State run = PlayerCampaign;
            int me = PlayerPartyIndexForCampaign();
            if (result == null || run == null || result.Parties == null || me < 0 || me >= result.Parties.Length) { return; }
            var input = new Elections.VoteAttribution.Inputs
            {
                Pressure = result.Pressure,   // A-LOOP q24 5a: the tally the run's share effects were read off
                Electorate = run.Setup.NationalAudience,
                PersuasionNorm = Elections.CampaignPersuasion.ElectorateMean(run.Setup),
                BaseCompatibility = run.Setup.Compatibility,
                PriorShares = run.Prior,   // AF-23: the run's own normalised prior, the one its truth was computed on
                LoyaltyPerParty = run.Setup.LoyaltyPerParty,
                Standing = Elections.CampaignRun.CloseStanding(run),   // AF-23 (q23): one standing for a created party, the campaign's and the count's
            };
            PlayerCampaignLedger = Elections.VoteAttribution.Explain(input, me);
            PlayerCampaignLedgerParty = run.Setup.Parties != null && me < run.Setup.Parties.Length ? run.Setup.Parties[me].Name : Elections.LiveCampaignSetup.Keys(PlayerCountryId.Value)[me];   // the setup's party names - State.Names are the regions; §675 (the review): the fallback reads the cast, which a created party's index is inside
        }

        public int PlayerPartyIndexForCampaign()
        {
            if (!PlayerCountryId.HasValue || _world == null) { return -1; }
            Country country = _world.GetCountry(PlayerCountryId.Value);
            if (country == null || string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return -1; }
            return System.Array.IndexOf(Elections.LiveCampaignSetup.Keys(PlayerCountryId.Value), country.PlayerPartyAbbrev);   // §675: the cast, created parties included
        }

        /// <summary>
        /// The player's party's script: each day, exactly what the HQ queued for that day, in order
        /// (`PartySetup.Script`, W-C2's seam). Closed over the RECORD, so the replay on load feeds the
        /// same queue the original run did. ⚠ A scripted party is NOT played by its personality any
        /// more: an empty day is a day of nothing, and that is the player's, not the AI's.
        /// </summary>
        private System.Func<int, Elections.AiDecision[]> PlayerScriptOver(Elections.PlayerCampaignRecord record)
        {
            if (record == null) { return null; }
            // The Setup the labels come from is the running state's own - Begin has returned before any
            // day is stepped, so PlayerCampaign is set by the time the script is first asked.
            return day =>
            {
                if (PlayerCampaign == null) { return new Elections.AiDecision[0]; }
                System.Collections.Generic.List<Elections.QueuedDecisionRecord> queued = record.QueuedFor(day);
                var decisions = new Elections.AiDecision[queued.Count];
                for (int i = 0; i < queued.Count; i++) { decisions[i] = queued[i].ToDecision(PlayerCampaign.Setup); }
                return decisions;
            };
        }

        /// <summary>
        /// CL-2 (2026-09-13): the player's answer to a story - the record's answer for the day the run steps next
        /// (`PlayerCampaignRecord.AnswerFor`, read by `PartySetup.ScandalScript`); null when none is queued, and the run answers
        /// on the party's instinct. Closed over the record like the action script, so a load replays the same answers.
        /// </summary>
        private System.Func<int, Elections.ScandalResponse?> PlayerScandalScriptOver(Elections.PlayerCampaignRecord record)
        {
            if (record == null) { return null; }
            return day => record.AnswerFor(day);
        }

        /// <summary>CL-2: the story that broke for the player's party and waits for its answer - the day it broke, the story, the evidence as the party's people read it; null when none is pending.</summary>
        public (int Day, Elections.Scandal Scandal, double Seen)? PendingPlayerScandal()
        {
            // A finished run holds nothing: no morning is left for an answer to land on, and QueueScandalResponse refuses one - a
            // story still listed then would hold the clock with no way to release it.
            if (PlayerCampaign == null || PlayerCampaign.Finished) { return null; }
            int me = PlayerPartyIndexForCampaign();
            if (me < 0) { return null; }
            foreach ((int day, int party, Elections.Scandal scandal, double seen) in PlayerCampaign.PendingScandals) { if (party == me) { return (day, scandal, seen); } }
            return null;
        }

        /// <summary>CL-2: the answer queued for the day the run steps next, or null.</summary>
        public Elections.ScandalResponse? QueuedScandalResponse()
        {
            if (PlayerCampaign == null || CampaignRecord == null) { return null; }
            return CampaignRecord.AnswerFor(PlayerCampaign.Day);
        }

        /// <summary>CL-2: queue the answer to the pending story for the day the run steps next - one answer, the last pressed standing; refused with the reason when no story waits.</summary>
        public bool QueueScandalResponse(Elections.ScandalResponse response, out string refusal)
        {
            string why = null;   // FORK-2: the answer queued again in every except-world's campaign on its day
            bool acted = Answered(new PlayerAnswer("a story's answer", PlayerCountryId, sim => sim.QueueScandalResponse(response, out _)), () => QueueScandalResponseNow(response, out why));
            refusal = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="QueueScandalResponse"/>'s act.</summary>
        private bool QueueScandalResponseNow(Elections.ScandalResponse response, out string refusal)
        {
            refusal = null;
            if (PlayerCampaign == null || CampaignRecord == null) { refusal = "no campaign is running"; return false; }
            if (PlayerCampaign.Finished) { refusal = "the campaign is over"; return false; }
            if (!PendingPlayerScandal().HasValue) { refusal = "no story is waiting for an answer"; return false; }
            if (response == Elections.ScandalResponse.Resign) { refusal = "no second candidate is cast - a candidate who resigns has nobody to hand the campaign to in this game"; return false; }
            if (response == Elections.ScandalResponse.SacrificeStaffMember && PlayerCampaign.Staff[PlayerPartyIndexForCampaign()].Count == 0) { refusal = "nobody is on the roster to sacrifice"; return false; }
            int day = PlayerCampaign.Day;
            CampaignRecord.ScandalAnswers.RemoveAll(a => a.Day == day);
            CampaignRecord.ScandalAnswers.Add(new Elections.ScandalAnswerRecord { Day = day, Response = (int)response });
            return true;
        }

        /// <summary>
        /// Queue a decision for the NEXT day the run will step - a campaign day, or (CL-1) a run-up day, whose queue is keyed
        /// by the negative day counted back from the campaign's first. Refused when nothing runs, when the kind is not legal
        /// in that day's phase, when the run-up has no price for it (`PreCampaignRun.Refusal` - the four the model cannot
        /// price), or when the player's party is not in the campaign - and the refusal is the return value, so the screen
        /// can say why. `role` is the role a hire fills; an office queued with no region goes to the largest electorate
        /// without one (the picker is the plan's S-C2). K3 (2026-10-23): `againstParty` is the party a campaign act attacks (an index into the
        /// campaign's parties, never the player's own; −1 for an act on the party's own case) - refused in the run-up, which has no contacts.
        /// </summary>
        public bool QueueCampaignDecision(Elections.CampaignActionKind kind, int regionIndex, Elections.IssueId? issue, double spend, out string refusal, int role = -1, int againstParty = -1)
        {
            string why = null;   // FORK-2: the decision queued again in every except-world's campaign on its day
            bool acted = Answered(new PlayerAnswer("a campaign decision", PlayerCountryId, sim => sim.QueueCampaignDecision(kind, regionIndex, issue, spend, out _, role, againstParty)),
                () => QueueCampaignDecisionNow(kind, regionIndex, issue, spend, out why, role, againstParty));
            refusal = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="QueueCampaignDecision"/>'s act.</summary>
        private bool QueueCampaignDecisionNow(Elections.CampaignActionKind kind, int regionIndex, Elections.IssueId? issue, double spend, out string refusal, int role, int againstParty = -1)
        {
            refusal = null;
            if ((PlayerCampaign == null && PlayerPreCampaign == null) || CampaignRecord == null) { refusal = "no campaign is running"; return false; }
            if (PlayerCampaign == null)
            {
                if (PlayerPreCampaign.Finished) { refusal = "the run-up is over - the campaign opens tomorrow"; return false; }
                if (PlayerPartyIndexForCampaign() < 0) { refusal = "your party is not in this campaign"; return false; }
                if (!Elections.CampaignLegality.IsLegal(kind, Elections.CampaignPhase.PreCampaign)) { refusal = $"{kind} is not legal in the PreCampaign phase"; return false; }
                string standing = Elections.PreCampaignRun.Refusal(kind);
                if (standing != null) { refusal = $"{kind}: {standing}"; return false; }
                if (kind == Elections.CampaignActionKind.EstablishOffice && regionIndex < 0) { regionIndex = Elections.PreCampaignRun.NextOfficeRegion(PlayerPreCampaign); }
                if (regionIndex >= PlayerPreCampaign.Staged.Regions.Length) { refusal = "no such region"; return false; }
                if (againstParty >= 0) { refusal = "the run-up has no contacts to attack with - an attack is a campaign act"; return false; }   // K3
                CampaignRecord.Queue.Add(new Elections.QueuedDecisionRecord { Day = PlayerPreCampaign.Day - PlayerPreCampaign.TotalDays, Kind = kind, RegionIndex = regionIndex, Issue = -1, Role = role, Spend = spend });
                return true;
            }
            if (PlayerCampaign.Finished) { refusal = "the campaign is over"; return false; }
            if (PlayerPartyIndexForCampaign() < 0) { refusal = "your party is not in this campaign"; return false; }
            if (PlayerCampaign.Setup.StandingShare(PlayerPartyIndexForCampaign()) <= 0.0) { refusal = "your party stands nowhere in this contest - its candidate is not in this round"; return false; }   // PL-C: a Polish run-off's cast keeps every party, only the finalists stand
            int day = PlayerCampaign.Day;
            Elections.CampaignPhase phase = PlayerCampaign.Setup.Calendar.PhaseOn(PlayerCampaign.Setup.Calendar.CampaignStart.AddDays(day));
            if (!Elections.CampaignLegality.IsLegal(kind, phase)) { refusal = $"{kind} is not legal in the {phase} phase"; return false; }
            if (regionIndex >= PlayerCampaign.Setup.Regions.Length) { refusal = "no such region"; return false; }
            // K3 (2026-10-23): the attack verb - a party of the campaign other than the player's own
            if (againstParty >= PlayerCampaign.Setup.Parties.Length || (againstParty >= 0 && againstParty == PlayerPartyIndexForCampaign())) { refusal = "no such party to attack"; return false; }
            CampaignRecord.Queue.Add(new Elections.QueuedDecisionRecord { Day = day, Kind = kind, RegionIndex = regionIndex, Issue = issue.HasValue ? (int)issue.Value : -1, Spend = spend, AgainstParty = againstParty < 0 ? -1 : againstParty });
            return true;
        }

        /// <summary>Empties the next day's queue - the campaign's, or (CL-1) the run-up's.</summary>
        public void ClearCampaignQueue() =>
            Answered(new PlayerAnswer("the campaign's day cleared", PlayerCountryId, sim => { sim.ClearCampaignQueue(); return true; }), () => { ClearCampaignQueueNow(); return true; });   // FORK-2 (the review's H1)

        /// <summary>FORK-2: <see cref="ClearCampaignQueue"/>'s act.</summary>
        private void ClearCampaignQueueNow()
        {
            if (CampaignRecord == null) { return; }
            if (PlayerCampaign != null) { int day = PlayerCampaign.Day; CampaignRecord.Queue.RemoveAll(q => q.Day == day); return; }
            if (PlayerPreCampaign != null) { int preDay = PlayerPreCampaign.Day - PlayerPreCampaign.TotalDays; CampaignRecord.Queue.RemoveAll(q => q.Day == preDay); }
        }

        /// <summary>
        /// Load: rebuild the campaign the record describes by replay - the same Setup from the runtime
        /// tables, the three streams rewound to their counts at the campaign's start, the same days
        /// stepped. The streams end where the save recorded them, and that is asserted rather than
        /// assumed: a mismatch is logged as an error and the campaign is still restored, because a
        /// warned campaign beats a vanished one.
        /// </summary>
        public void RestoreCampaign(Elections.PlayerCampaignRecord record, int masterSeed, System.Collections.Generic.Dictionary<SimulationRandom.Stream, int> savedCounts)
        {
            CampaignRecord = record;
            PlayerCampaign = null;
            PlayerPreCampaign = null;
            PlayerCampaignResult = null;
            if (record == null || !PlayerCountryId.HasValue) { return; }
            if (record.DaysStepped <= 0 && record.PreCampaignDaysStepped <= 0) { return; }
            Elections.CampaignCalendar calendar = CampaignCalendarOfRecord(record.ElectionDate);   // §697: the calendar the record was stepped on (PL-C: a Polish run-off's two weeks)
            int me = PlayerPartyIndexForCampaign();
            // CL-1: the run-up first - its one stream rewound to its count at the run-up's start, the same days re-stepped on
            // the same queue; its outcome is then what the campaign's replay hands the player's party, as the original run did.
            Elections.PreCampaignRun.Outcome? brought = null;
            if (record.PreCampaignDays > 0 && record.PreCampaignDaysStepped > 0 && me >= 0
                && Elections.LiveCampaignSetup.TryFor(PlayerCountryId.Value, new (int, int, Elections.Scandal)[0], calendar,
                    out Elections.CampaignRun.Setup staged, out _, onVoteModelCompatibility: true, playerParty: me, played: _world.GetCountry(PlayerCountryId.Value)))   // PS-6 US-26: staged on the history rolled forward
            {
                EventSalience.ApplyToCampaign(record.SalienceShift, staged.TrueSalience);   // A-LOOP q5: the salience the record's window opened on, replayed
                var preRewound = new System.Collections.Generic.Dictionary<SimulationRandom.Stream, int>(savedCounts);
                if (record.DrawCountsAtStart.TryGetValue(PreCampaignStream, out int preAtStart)) { preRewound[PreCampaignStream] = preAtStart; }
                SimulationRandom.RestoreState(masterSeed, preRewound);
                PlayerPreCampaign = Elections.PreCampaignRun.Begin(staged, me, SimulationRandom.For(PreCampaignStream), PlayerCountryId);   // K84: the country's postal tariff prices a letter
                for (int i = 0; i < record.PreCampaignDaysStepped && !PlayerPreCampaign.Finished; i++)
                {
                    Elections.PreCampaignRun.StepDay(PlayerPreCampaign, PreCampaignDecisionsFor(record, PlayerPreCampaign, PlayerPreCampaign.Day));
                }
                if (PlayerPreCampaign.Finished) { brought = Elections.PreCampaignRun.Finish(PlayerPreCampaign); }
                UnityEngine.Debug.Log($"CAMPAIGN REPLAY: {record.PreCampaignDaysStepped} run-up day(s) re-stepped for {PlayerCountryId.Value} toward {record.ElectionDate:yyyy-MM-dd}");
            }
            if (record.DaysStepped <= 0)
            {
                AssertReplayLandedOn(savedCounts, record.PreCampaignDaysStepped, new[] { PreCampaignStream });
                return;
            }
            if (!Elections.LiveCampaignSetup.TryFor(PlayerCountryId.Value, new (int, int, Elections.Scandal)[0], calendar,
                    out Elections.CampaignRun.Setup setup, out _, onVoteModelCompatibility: true,
                    playerParty: me, playerScript: PlayerScriptOver(record), playerOutcome: brought,
                    playerScandalScript: PlayerScandalScriptOver(record), liveScandalRate: Elections.Scandals.LiveRatePerPartyDay, played: _world.GetCountry(PlayerCountryId.Value)))   // PS-6 US-26: staged on the history rolled forward
            {
                return;
            }
            EventSalience.ApplyToCampaign(record.SalienceShift, setup.TrueSalience);   // A-LOOP q5: the salience the record's window opened on, replayed
            setup = setup.WithRecordShift(RecordShiftFor(setup, record.RecordShift));   // PS-3k (§638): the record the campaign opened on, replayed
            var rewound = new System.Collections.Generic.Dictionary<SimulationRandom.Stream, int>(savedCounts);
            foreach (SimulationRandom.Stream stream in CampaignStreams)
            {
                if (record.DrawCountsAtStart.TryGetValue(stream, out int atStart)) { rewound[stream] = atStart; }
            }
            SimulationRandom.RestoreState(masterSeed, rewound);
            PlayerCampaign = Elections.CampaignRun.Begin(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi),
                SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            for (int i = 0; i < record.DaysStepped && !PlayerCampaign.Finished; i++) { StepPlayerCampaignDay(record, live: false); }
            if (PlayerCampaign.Finished) { PlayerCampaignResult = Elections.CampaignRun.Finish(PlayerCampaign); RecordCampaignLedger(); }
            UnityEngine.Debug.Log($"CAMPAIGN REPLAY: {record.DaysStepped} day(s) re-stepped for {PlayerCountryId.Value} toward {record.ElectionDate:yyyy-MM-dd}");
            AssertReplayLandedOn(savedCounts, record.DaysStepped, CampaignStreams);
        }

        /// <summary>
        /// The replay's own check: every named stream ends at the draw count the save recorded. A mismatch is logged as an
        /// error and the campaign is still restored, because a warned campaign beats a vanished one.
        /// </summary>
        private static void AssertReplayLandedOn(System.Collections.Generic.Dictionary<SimulationRandom.Stream, int> savedCounts, int days, SimulationRandom.Stream[] streams)
        {
            System.Collections.Generic.Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            foreach (SimulationRandom.Stream stream in streams)
            {
                int expected = savedCounts.TryGetValue(stream, out int e) ? e : 0;
                int actual = after.TryGetValue(stream, out int a) ? a : 0;
                if (actual != expected)
                {
                    UnityEngine.Debug.LogError($"CAMPAIGN REPLAY: stream {stream} ended at draw {actual} against the save's {expected} after {days} day(s) - the replay did not reproduce the campaign the save left, which means a decision or a stream changed between the save and this build");
                }
            }
        }

        public void RestoreSaveState(World world, int currentTurn, System.DateTime currentDate, Persistence.SimulationPendingState state)
        {
            SetWorld(world);
            CurrentTurn = currentTurn;
            CurrentDate = currentDate;
            CommitCalendarYear();   // PN-1's driver (§520): a restored world reads the restored clock's year, not the seed's

            // OLD-SAVE BASE ADOPTION (pass 3, coexistence ruling 2026-08-26): a save written
            // before the statutory-base fields existed restores them at their -1 sentinel; adopt
            // the saved dial value as the base, which is exactly right because no pre-pass-3 save
            // can hold an enacted labor law (effective == base there by construction). A -1 never
            // survives into a post-pass-3 save.
            foreach (Country restored in world.Countries)
            {
                if (restored.MinimumWagePercentOfMedianBase < 0f) { restored.MinimumWagePercentOfMedianBase = restored.MinimumWagePercentOfMedian; }
                if (restored.PaidFamilyLeaveWeeksBase < 0f) { restored.PaidFamilyLeaveWeeksBase = restored.PaidFamilyLeaveWeeks; }
                if (restored.OvertimeRegulationBase < 0f) { restored.OvertimeRegulationBase = restored.OvertimeRegulationLevel; }
                if (restored.RetrainingProgramBase < 0f) { restored.RetrainingProgramBase = restored.RetrainingProgramLevel; }
                if (restored.FamilyPolicyBase < 0f) { restored.FamilyPolicyBase = restored.FamilyPolicyLevel; }
                if (restored.ImmigrationPolicyBase < 0f) { restored.ImmigrationPolicyBase = restored.ImmigrationPolicyLevel; }
                // A-LOOP q34 (2026-10-19, additive under ruling A): a save from before carries no seed set, and it is read as the lines levied at the load -
                // the older build's burden counted only today's levied lines on both sides, so the game loads to the burden it was saved at. Not migrated:
                // a line levied or removed before such a save stays as the old build read it (no gap from it); a move after the load reads q34's way.
                if (restored.SeedLevied == null)
                {
                    restored.SeedLevied = new List<TaxType>();
                    foreach (TaxLine line in restored.TaxLines) { if (line.IsImplemented) { restored.SeedLevied.Add(line.Type); } }
                }
            }

            _fiscalPeriods.Clear();
            _pendingBudgetProcessByCountry.Clear();
            _incomingBudgetWindowUsed.Clear();
            _incomingBudgetWindowOpenNow.Clear();
            _pendingBudgetBillByCountry.Clear();
            _pendingMajorityReconciliationByCountry.Clear();   // PS-6 US-23
            _pendingDebtLimitBillByCountry.Clear();   // PS-6 US-24
            _pendingTaxProgramBillsByCountry.Clear();
            _pendingWelfareProgramBillsByCountry.Clear();
            _pendingLaborBillByCountry.Clear();
            _pendingCrimeJusticeBillByCountry.Clear();
            _pendingSectorBillByCountry.Clear();
            _pendingTradeBillByCountry.Clear();
            _pendingSwfDrawdownBillByCountry.Clear();
            _pendingLawBillsByCountry.Clear();
            _pendingForeignPolicyMeetingByCountry.Clear();
            _pendingCabinetDecisionsByCountry.Clear();
            _lastFiscalReports.Clear();
            _lastEventsByCountry.Clear();

            if (state == null)
            {
                return;
            }

            CopyInto(state.FiscalPeriods, _fiscalPeriods);
            if (state.PendingBudgetProcess != null)
            {
                foreach (CountryId id in state.PendingBudgetProcess)
                {
                    _pendingBudgetProcessByCountry.Add(id);
                }
            }

            // C-C2: ⚠ null on every save written before this field existed, and an empty set grants a
            // mid-term government one arrival window. That is RIGHT for a pre-C-C2 save - it never had
            // the window, so it is owed one - and is why this is additive rather than a version bump.
            if (state.IncomingBudgetWindowUsed != null)
            {
                foreach (CountryId id in state.IncomingBudgetWindowUsed)
                {
                    _incomingBudgetWindowUsed.Add(id);
                }
            }

            // PS-6 US-7 (format 39): which open process is the arrival window, as the continuous run holds it - every writer keeps it inside the open
            // processes (opened with one, removed with one), so a country outside them is not restored; null or empty on an older save, as before
            if (state.IncomingBudgetWindowOpenNow != null)
            {
                foreach (CountryId id in state.IncomingBudgetWindowOpenNow)
                {
                    if (_pendingBudgetProcessByCountry.Contains(id)) { _incomingBudgetWindowOpenNow.Add(id); }
                }
            }

            CopyInto(state.PendingBudgetBills, _pendingBudgetBillByCountry);
            CopyInto(state.PendingMajorityReconciliations, _pendingMajorityReconciliationByCountry);   // PS-6 US-23: null or empty on an older save - none was ever tabled there
            CopyInto(state.PendingDebtLimitBills, _pendingDebtLimitBillByCountry);   // PS-6 US-24 (format 53; an older save is refused)
            _pendingBudgetAlternativeByCountry.Clear(); CopyInto(state.PendingBudgetAlternatives, _pendingBudgetAlternativeByCountry);   // PS-3e (§632): cleared first - a v29 save carries its own, an older none
            _extraElectionDate = state.ExtraElectionDate; _extraElectionOrderedOn = state.ExtraElectionOrderedOn;   // PS-3i (§636)
            CopyInto(state.PendingLaborBills, _pendingLaborBillByCountry);
            CopyInto(state.PendingCrimeJusticeBills, _pendingCrimeJusticeBillByCountry);
            CopyInto(state.PendingSectorBills, _pendingSectorBillByCountry);
            CopyInto(state.PendingTradeBills, _pendingTradeBillByCountry);
            CopyInto(state.PendingSwfDrawdownBills, _pendingSwfDrawdownBillByCountry);
            CopyInto(state.PendingForeignPolicyMeetings, _pendingForeignPolicyMeetingByCountry);
            CopyInto(state.LastFiscalReports, _lastFiscalReports);
            // AF-17 (M2): a report from a save older than AF-17 carries no flow figure - its BudgetBalance WAS the flow, so it is the flow
            foreach (FiscalTurnReport loaded in _lastFiscalReports.Values) { if (loaded != null && float.IsNaN(loaded.FlowBalance)) { loaded.FlowBalance = loaded.BudgetBalance; } }
            CopyInto(state.LastEvents, _lastEventsByCountry);

            if (state.PendingTaxProgramBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<TaxType, TaxProgramBill>> pair in state.PendingTaxProgramBills)
                {
                    _pendingTaxProgramBillsByCountry[pair.Key] = new Dictionary<TaxType, TaxProgramBill>(pair.Value);
                }
            }

            if (state.PendingWelfareProgramBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> pair in state.PendingWelfareProgramBills)
                {
                    _pendingWelfareProgramBillsByCountry[pair.Key] = new Dictionary<WelfareProgramType, WelfareProgramBill>(pair.Value);
                }
            }

            if (state.PendingLawBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<string, LawBill>> pair in state.PendingLawBills)
                {
                    _pendingLawBillsByCountry[pair.Key] = new Dictionary<string, LawBill>(pair.Value);
                }
            }

            if (state.PendingCabinetDecisions != null)
            {
                foreach (KeyValuePair<CountryId, List<Persistence.PendingCabinetDecisionRecord>> pair in state.PendingCabinetDecisions)
                {
                    var pending = new List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>(pair.Value.Count);
                    foreach (Persistence.PendingCabinetDecisionRecord record in pair.Value)
                    {
                        pending.Add((record.Portfolio, record.Decision));
                    }

                    _pendingCabinetDecisionsByCountry[pair.Key] = pending;
                }
            }
        }

        private static void CopyInto<TKey, TValue>(Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                target[pair.Key] = pair.Value;
            }
        }

        private void Awake()
        {
            if (_world == null)
            {
                _world = WorldFactory.CreateDefault();
            }

            SeedPublishedHistory();
        }

        /// <summary>Gives every country the one inherited published quarter it takes office with - see PublicationSystem.SeedInheritedHistory. Called from BOTH world-entry paths (injection and Awake) so batch runs and real play start from an identical published record; the seeding method is itself idempotent, so the overlap is harmless.</summary>
        private void SeedPublishedHistory()
        {
            if (_world == null)
            {
                return;
            }

            foreach (Country country in _world.Countries)
            {
                PublicationSystem.SeedInheritedHistory(country);
                Elections.FiscalVote.SeedPreRun(country, EpochDate);   // K62 (2026-10-23): the vote's fiscal book enters with the sourced years before the start - idempotent (a save's book stands), read by the vote alone
            }
        }

        /// <summary>
        /// Advances the simulation by one turn for every country. Countries with no entry in
        /// <paramref name="decisions"/> get a no-op policy decision for the turn.
        ///
        /// Order matters: interest rates and currency strength must resolve before trade (export
        /// competitiveness depends on this turn's currency strength), and trade must resolve before
        /// domestic policy (the national accounts identity needs this turn's TradeBalance as NX).
        /// </summary>
        public void AdvanceTurn(Dictionary<CountryId, PolicyDecision> decisions)
        {
            // EN-3 (2026-09-11): the water value Sweden's zones clear at this turn - Germany's and Poland's block prices, each over the price level it was cleared at (FT-10 · P-B, §552: the water value is kept in the seed's prices) - fixed
            // once at the top of the boundary, before any family's yearly step reads a clearing.
            // P6-F2 (§539, moved here by §544's review): the queue's due orders land FIRST - the water value just below and the power CO₂ in the families' step
            // both clear before the ledger does, and read last year's fleet while the landing sat beside the ledger.
            foreach (Country country in _world.Countries) { EnergyFleet.Advance(country, CurrentTurn + 1, country.CalendarYear); }
            EnergyMarket.BeginTurn(_world);
            // F2 step 4: the year the days just read is COMMITTED to the pyramid first, with the levers
            // as they stood through it - before this turn's decisions move them (a lever changed at the
            // boundary reaches the substrate from the next year's first day, the Phase 4 timing shift).
            foreach (Country country in _world.Countries)
            {
                CohortDemographics.CommitYear(country, CohortDemographics.SubstrateYear(CurrentTurn));
            }
            CurrencySystem.ApplyInterestRateChanges(_world, decisions);

            foreach (Country country in _world.Countries)
            {
                CurrencySystem.ApplyCurrencyStrength(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision tariffDecision = decisions != null && decisions.TryGetValue(country.Id, out var td)
                    ? td
                    : PolicyDecision.None();

                ApplyTariffRateChange(country, tariffDecision);
                ApplyPartnerTariffOverrides(country, tariffDecision);
            }

            var tariffRevenueByCountry = new Dictionary<CountryId, float>();
            foreach (Country country in _world.Countries)
            {
                tariffRevenueByCountry[country.Id] = TradeSystem.ApplyTradeEffects(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision decision = decisions != null && decisions.TryGetValue(country.Id, out var d)
                    ? d
                    : PolicyDecision.None();

                // THE AI FINANCE MINISTRY (2026-09-07, §387 the page, §388 the build): a country the player does not govern answers last year's closed
                // balance and its debt ratio with the player's own levers under its sourced rule (AiFinanceMinistry) - written into the decision it was
                // handed where that decision carries nothing of its own for a line or a tax, then observed for the debt-ratio memory (the EU rule's fall).
                bool aiGovernment = AiFinanceMinistryEnabled && !PlayerGoverns(country);   // PS-3a (§628): the test is the player's ROLE, not the country
                // PS-3e (§632): for THE PLAYER'S country the ministry's rule goes to the chamber as the government's budget bill (TableGovernmentBudget) and
                // applies on the day the chamber adopts it - not into the boundary's decision. Observe still runs (the debt-ratio memory).
                bool ministryByBill = aiGovernment && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id;
                AiFinanceMinistry.Written ministryWrote = null;
                if (aiGovernment)
                {
                    if (!ministryByBill) { ministryWrote = AiFinanceMinistry.Apply(country, GetLastFiscalReport(country.Id), decision); }
                    AiFinanceMinistry.Observe(country);
                }
                // §755 (Elias's ruling A2): a partner holding Finance moves the FISCAL STANCE toward the one it asks, whoever leads and whoever holds it -
                // after the ministry, whose treaty rule has first claim on a line or rate it wrote; withdrawn after the turn as the ministry's is.
                FinancePartner.Written partnerWrote = PartnerStepsAtBoundary(country) ? FinancePartner.Apply(country, decision, ComingBoundaryDate) : null;
                // §773 (Elias's ruling E2): "A rate moves only through the tax act (Sejm vote, then the veto), whoever proposes it." In the player's Poland
                // the step's household rates are a tax act the partner's party tables, voted and put to the President before the turn applies them
                if (partnerWrote != null && partnerWrote.Taxes.Count > 0 && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id && Elections.WorldClock.StatutePartsAreActs(country.Id))
                {
                    PartnerTaxAct(country, decision, partnerWrote, ComingBoundaryDate);
                }
                FinancePartner.Record(country, partnerWrote, ComingBoundaryDate);
                if (partnerWrote != null && partnerWrote.Moves.Count > 0 && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id) { Debug.Log($"LEVERS: {country.Id} - the Finance minister's party ({partnerWrote.Holder}) moves {string.Join("; ", partnerWrote.Moves)}"); }
                // THE AI ENERGY MINISTRY (P6-F2d, §544): HELD - AiEnergyMinistry.Live is false until its family is dumped and ruled; with it on, a country the player
                // does not govern answers its own statute with orders in its connection queue, which land through EnergyFleet.Advance like the player's.
                if (AiEnergyMinistry.Live && !PlayerGoverns(country)) { AiEnergyMinistry.Decide(country, PensionAgeStatute.SeedYear + CurrentTurn + 1, CurrentTurn + 1); }   // the year about to be played and its turn: an order placed at the boundary waits its lead time from the coming year
                ApplyDomesticPolicy(country, decision, tariffRevenueByCountry[country.Id]);
                // §388: what the ministry wrote leaves the decision again - a caller (the trajectory dump, a harness) may hand the same object in next turn,
                // and the first builds re-applied year 2's cuts for a century because it did not.
                if (ministryWrote != null) { AiFinanceMinistry.Withdraw(decision, ministryWrote); }
                if (partnerWrote != null) { FinancePartner.Withdraw(decision, partnerWrote); }

                // Political Systems Overhaul Part B (Parliament), Master Sequence step 4: recomputed
                // for EVERY country every turn (not just the player's - see ParliamentSeats' own doc
                // comment), after ApplyDomesticPolicy so this turn's freshly-updated ApprovalRating is
                // what the seat-share formula actually reads, not last turn's stale value.
                ParliamentSystem.UpdateSeats(country);
                TrackAgreements(country);   // PS-3h (§635)

                // PHASE 4 FINDING (2026-08-16): History.Append lived HERE, once per turn, from Phase 0
                // until this pass - which meant the multi-resolution buckets built FOR daily data had
                // never received a daily offer, and Daily/Weekly/Monthly/Quarterly held identical
                // one-point-per-turn series through Phases 1-3's genuinely daily variation. The append
                // now runs in AdvanceDay (offered daily; each resolution's own gate accepts at its
                // cadence, exactly the design intent its class doc always stated). PreviewTurn's
                // phantom-point protection carries over unchanged: AdvanceDay never runs on the
                // preview clone at all - stronger isolation than the old once-per-turn site had.
            }

            CurrentTurn++;
        }

        /// <summary>
        /// Applies one country's domestic feedback rules for the turn, in place: fiscal policy,
        /// the national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve
        /// (inflation), and approval. <paramref name="tariffRevenue"/> was already collected (and
        /// already added to Budget) by TradeSystem earlier this same turn - it's threaded through
        /// only to record it on this turn's FiscalTurnReport, not applied again here.
        ///
        /// CONTINUOUS TIME PHASE 3: "fiscal policy" here no longer means moving money. The budget is
        /// RESOLVED at this boundary and then EXECUTED daily by AccrueDailyFiscalFlows over the 121 days
        /// that follow; what remains in this method is the resolution, the close-out of the period that
        /// just ended, and everything downstream of them that is still turn-shaped.
        /// </summary>
        /// <summary>§403 (FT-9's decomposition): a measurement seam, null in play. A subscriber is told the name of each boundary checkpoint as the
        /// country passes it - "open" (the per-country boundary begins: the cohort commit, the zone rate, currency, tariffs and trade have run), "policy"
        /// (the decision's dials applied), "labour" (the supply shock and the natural rate), "spending" (the lines resolved and their effects),
        /// "families" (the six yearly readouts), "close" (the fiscal period closed, the Phillips curve, expectations, approval), "events" (the year's
        /// event landed) - and reads whatever it wants from the country between them. Invocations only: no statement of the boundary is changed,
        /// and the p5bnd diff against p5nairu proves it (§403), as the numeric-inertness rule requires.</summary>
        public static System.Action<Country, string> BoundaryLedger;

        private void ApplyDomesticPolicy(Country country, PolicyDecision decision, float tariffRevenue)
        {
            BoundaryLedger?.Invoke(country, "open");
            EconomyState state = country.State;

            CarbonRateStatute.AdvanceYear(country, CurrentTurn, ComingBoundaryDate.Year);   // EN-4e (§471): the carbon rate moves as its statute moves it, before any override - a passed bill's figure wins; a left dial gets the law's
            float taxTermImpulse = ApplyTaxRateChanges(country, decision);
            ApplyWelfareGenerosityChanges(country, decision);
            ApplyMinimumWageChange(country, decision);
            ApplyCrimePolicyChanges(country, decision);
            ApplySectorPolicyChanges(country, decision);
            ApplySwfPolicyChanges(country, decision);
            ApplyLaborPolicyChanges(country, decision);
            ApplyCrimeJusticeDeeperChanges(country, decision);
            // Round 3 item 5, Part B: must run BEFORE CohortDemographics.Apply, same reasoning as
            // ApplyCrimeJusticeDeeperChanges above.
            ApplyDemographicPolicyChanges(country, decision);
            BoundaryLedger?.Invoke(country, "policy");
            // CONTINUOUS TIME PHASE 4: the demographic rates and population growth have already been
            // charged day by day in AdvanceDay - applying either again here would double-count a full
            // turn's worth on top of the daily steps (Phase 1's exact wording, same reason). The old
            // ordering contract this comment carried - "must run BEFORE ResolveSpendingForTurn, which
            // reads this turn's freshly-updated DependencyRatio" - still holds by construction:
            // AdvanceDay finishes the boundary day before AdvanceTurn runs, so the DependencyRatio
            // read below completed this period's accumulation. PreviewTurn keeps the turn forms as
            // their only remaining callers, correctly rather than as dead code.
            // FT-7, the first seat (§391): the year's change in labour supply enters unemployment at the TOP of the boundary - before the spending lines read
            // the unemployment driver (the labour-market and income-security lines are the new entrants' caseload) and before the Phillips curve reads the print.
            // The first tree placed it after the indexation and the line-indexation diagnostic caught the split (two lines 1-2 % off their country's factor).
            MacroSystem.ApplySupplyShockToUnemployment(country);
            MacroSystem.ApplyNaturalRateFromLabourForce(country);   // FT-8 (§398): the natural rate reads the labour force, at the same boundary
            BoundaryLedger?.Invoke(country, "labour");
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(country, decision);
            MacroSystem.ApplyCategorySpendingEffects(country, spendingResult.EffectiveDecision);
            // Phase 1: the DECAY and the sector reversion have already been charged day by day in
            // AdvanceDay. Only the discrete investment credit belongs here now - applying either of the
            // continuous flows again would double-count a full turn's worth on top of the daily steps.
            MacroSystem.ApplyInfrastructureInvestment(country, spendingResult.EffectiveDecision);
            MacroSystem.ApplySectorGrowthEffect(country, ComingBoundaryDate);   // K210: the productivity anchor read for this boundary - the preview passes the same date
            MacroSystem.ApplyWelfareProgramEffects(country);
            BoundaryLedger?.Invoke(country, "spending");
            HealthFamily.AdvanceYear(country);   // P5-C2 (2026-09-05): the family's yearly step - a readout of the year's health line, the age-cost index and the minister; no feedback into the model
            EducationFamily.AdvanceYear(country);   // P5-C3 (2026-09-06): the education family's yearly step - readouts, no feedback
            InfrastructureFamily.AdvanceYear(country);   // P5-C4 (2026-09-06): readouts, no feedback
            EnvironmentFamily.AdvanceYear(country);   // P5-C5 (2026-09-06): the intensities' yearly step; since §349 (2026-09-07) the carbon tax's base reads them (TaxBaseDriver.Emissions) - the one feedback this family has
            double electricityTaxRevenueChange = EnergyLedger.AdvanceYear(country);   // EN-4 (2026-09-11): the retail stack and the two ledgers at this year's dispatch; the industrial bill's change reaches BusinessConfidence next boundary (MacroSystem.ApplyCategorySpendingEffects). EN-7b: its electricity-tax receipts above the statute, planned below
            MigrationPovertyFamily.AdvanceYear(country);   // P5-C6 (2026-09-06): readouts, no feedback
            BoundaryLedger?.Invoke(country, "families");

            // CONTINUOUS TIME PHASE 3: the money already moved, day by day, in AccrueDailyFiscalFlows.
            // What is left at a boundary is what a boundary is actually for - CLOSING the period that
            // just ended and RESOLVING the plan the next one executes. No cash changes hands here, and
            // deliberately so: charging a period's flows here on top of 121 daily accruals is the
            // double-count Phase 1 already had to reason about for infrastructure investment.
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);

            // Every flow below is the SUM of this period's 121 daily accruals, not a single step's
            // figure. BaselineGovernmentSpending/DiscretionarySpending are the decomposition of the plan
            // those days were spending - read BEFORE the re-plan below overwrites it - so their sum still
            // equals the government spending actually accrued, which is the property every existing
            // caller of this report relies on.
            //
            // Pass 5 (2026-08-26): TariffRevenue is no longer the exception. TradeSystem still computes
            // the figure once per boundary, but it is the coming period's PLAN, accrued daily inside
            // Revenue like every other flow - so the report shows the tariff portion that actually
            // accrued over the period that just closed, a true reading of the one real path. (Under the
            // pre-pass-5 books it was a turn figure booked to the accumulator alone.)
            // AF-17 (q17, 2026-10-18): the year's one-time settlements count in the year, itemised - read off the debt ledger's accruing period, which is
            // this fiscal period (it closes just below, at the same boundary). With none the sum is 0f and the balance is the flow exactly (x + 0).
            List<FiscalOneOff> oneOffLines = FiscalOneOff.FromLedger(country.FiscalLedgerAccruing, out float oneOffs);
            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = period.AccruedRevenue,
                BaselineGovernmentSpending = period.PlannedBaselineGovernmentSpending,
                DiscretionarySpending = period.PlannedDiscretionarySpending,
                MandatorySpending = period.AccruedMandatorySpending,
                UnemploymentBenefitCost = period.AccruedUnemploymentBenefitCost,
                InterestOnDebt = period.AccruedInterestOnDebt,
                TariffRevenue = period.AccruedTariffRevenue,
                ElectricityTaxRevenue = period.AccruedElectricityTaxRevenue,   // EN-7b
                TariffPassThroughPp = period.PlannedTariffPassThroughPp,   // EN-5: the planned tariff term (the applied print carries two terms now - PriceLevelTermsAppliedPp)
                EnergyPassThroughPp = period.PlannedEnergyPassThroughPp,
                PriceLevelTermsAppliedPp = period.AppliedPriceLevelTermsPp,
                WelfareCost = period.AccruedWelfareCost,
                SwfContribution = period.AccruedSwfContribution,
                SwfReturns = period.AccruedSwfReturns,
                TotalSpending = period.AccruedTotalSpending,
                DebtLimitCut = period.AccruedDebtLimitCut,   // PS-6 US-24 (M4): the cut's own line
                CrisisCut = period.AccruedCrisisCut,   // MF q33 (the q32 review's M3): the closed market's cut, its own line
                BudgetBalance = period.AccruedBudgetBalance + oneOffs,
                FlowBalance = period.AccruedBudgetBalance,   // AF-17 (M2): the pre-AF-17 store, unchanged - the rules read it
                OneOffs = oneOffs,
                WriteOffs = FiscalOneOff.WriteOffsOf(country.FiscalLedgerAccruing),   // MF q33 (N1): the haircut, outside the balance
                OneOffLines = oneOffLines
            };
            // P2-0.4: the closed period's balance joins the annual series the sheet draws - observation only. AF-17: the year as read, one-offs counted.
            country.History.BudgetBalanceAnnual.Add(_lastFiscalReports[country.Id].BudgetBalance);
            country.History.BudgetBalanceAnnualDates.Add(CurrentDate);
            Elections.FiscalVote.CloseYear(country, CurrentDate, _lastFiscalReports[country.Id].BudgetBalance, state.NominalGdp);   // A-LOOP q13 (§4 (2)): the same year and nominal GDP at its close join the vote's book - observation only
            // P2-3.3: the chamber's compass position joins its trail at the same close - observation only.
            PoliSim.Elections.CompassPositions.RecordTrailPoint(country, CurrentDate);

            // Step 2's third section (2026-08-25): the debt ledger closes exactly where the
            // FiscalTurnReport does - the last daily slice has been observed (AdvanceDay finished
            // the boundary day before AdvanceTurn ran), the stock is the period's closing stock,
            // and the rate pair read here is the same pair the boundary day's slice was charged
            // at. The close runs the Σ(terms)+Σ(events)+clamp == observed-Δ self-audit (ATTRIB:
            // on red) and opens the next period's ledger at the post-boundary stock.
            DebtLedgerRecorder.CloseAtBoundary(country, CurrentDate, GetDebtIssuanceRate(country),
                country.EffectiveDebtInterestRate >= 0f ? country.EffectiveDebtInterestRate : GetDebtIssuanceRate(country));

            // CONTINUOUS TIME PHASE 5: "this turn's realized growth" is now the PERIOD's growth,
            // measured from the GDP the closing period opened at - by the time a boundary runs, the
            // days have already moved GDP, so the old top-of-method snapshot would read ~zero. The
            // guard covers a pre-Phase-5 save whose restored period carries no opening GDP.
            EnergyReliability.LiftOverlay(country);   // MF q26: the unserved-energy overlay lifted with the lapse's, for the same readings (nothing where none is applied)
            UsLapse.LiftOverlay(country);   // PS-6 US-22b (the review's M3): the year's realized growth, the tariff pass-through's denominator, the fiscal stance and the next period's opening GDP read the identity's level, not a lapse's overlay - put back after them (nothing where none is applied)
            float actualGrowthRate = period.GdpAtPeriodOpen > 0f
                ? (state.GDP - period.GdpAtPeriodOpen) / Mathf.Max(period.GdpAtPeriodOpen, 1f) * 100f
                : 0f;

            // Pass 6: the closing period's tariff pass-through as it actually printed on the boundary
            // day, and the take it planned - both captured before the re-plan below overwrites the
            // period. The expectations step runs AFTER the re-plan and must look through the CLOSING
            // period's term, not the coming one's (the ordering trap, named).
            float closingAppliedPriceLevelTermsPp = period.AppliedPriceLevelTermsPp;   // EN-5: the tariff and electricity terms together, as they printed
            float closingPlannedTariffRevenue = RealPlannedTariffRevenue(period, country);   // P5-B6: real against real

            // Open the next period. The SWF return is drawn ONCE here and accrued daily - see
            // FiscalPeriod's doc comment for that decision and why daily draws were rejected. The draw
            // sits at the same point in ApplyDomesticPolicy the old ApplyReturns call did, so the
            // SovereignWealthFund random stream still advances exactly once per country per turn.
            period.ResetAccrual();
            period.PlannedGovernmentSpending = spendingResult.GovernmentSpending;
            period.PlannedMandatorySpending = spendingResult.MandatorySpending;
            period.PlannedBaselineGovernmentSpending = spendingResult.BaselineGovernmentSpending;
            period.PlannedDiscretionarySpending = spendingResult.DiscretionarySpendingChangeThisTurn;
            period.PlannedSwfReturn = country.SovereignWealthFund != null
                ? SovereignWealthFundSystem.DrawPeriodReturn(country.SovereignWealthFund)
                : 0f;
            period.SwfAssetsAtPeriodOpen = country.SovereignWealthFund != null ? country.SovereignWealthFund.TotalAssets : 0f;   // AF-35: what the return was drawn on
            // Pass 5: this boundary's tariff figure (rates and volumes as they stand after this
            // turn's tariff decisions resolved) is the coming period's tariff flow.
            // Pass 6: the CHANGE in that figure against the closing period's plan is the coming
            // period's price pass-through (TradeCosts.ImportPricePassThrough) - the tariff-weighted
            // import-price change, as inflation points for one year, over the GDP this period opens
            // at. Exactly 0f when no rate changed: the same pure sum on unchanged state. A branch, not
            // a multiply, so the wired-inert control is the old code to the bit.
            period.PlannedTariffPassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                    * 100f * (tariffRevenue - closingPlannedTariffRevenue) / Mathf.Max(state.GDP, 1f)
                : 0f;
            period.PlannedTariffRevenueReal = tariffRevenue;
            period.PlannedTariffRevenue = tariffRevenue * country.State.PriceLevel;   // P5-B6: the take is computed on real trade; the book is nominal
            // EN-5 (2026-09-11): the electricity pass-through for the coming period - the household price the ledger wrote above (the families ran before
            // this re-plan), relative to the general price level, in the index's weight; a price-level term the expectations step looks through
            period.PlannedEnergyPassThroughPp = EnergyPassThrough.Planned(country);
            // EN-7b (2026-09-15): the electricity tax's receipts above the statute for the coming period - the book the ledger wrote above, booked as a flow
            period.PlannedElectricityTaxRevenue = (float)electricityTaxRevenueChange;

            // Read AFTER 121 days of accrual have finished moving the debt stock, so the stance the next
            // period adopts responds to the debt the country actually ended this one with - the same
            // instant the turn form read it, for the same reason.
            period.PlannedFiscalReactionMultiplier = GetFiscalReactionMultiplier(country);
            period.GdpAtPeriodOpen = state.GDP;
            UsLapse.LowerOverlay(country);   // PS-6 US-22b (M3): the overlay back on the level the day left
            EnergyReliability.LowerOverlay(country);   // MF q26: and back on after it
            period.UnemploymentAtPeriodOpen = state.Unemployment;
            period.PotentialGdpAtPeriodOpen = state.PotentialGDP;
            // Q5: the gap the daily identity consumes now includes productivity's hoarding cycle,
            // computed from the unemployment this period is opening at - the same value recorded on
            // the line above, so the two anchors describe one instant.
            period.WageGrowthGapAtPeriodOpen = MacroSystem.RealWageGrowthGapPerTurnPercent(country,
                MacroSystem.ProductivityCycleGrowthPerTurnPercent(country, state.Unemployment));

            // CONTINUOUS TIME PHASE 5: the identity, trend growth, Okun, Phillips and expectations
            // have already been charged day by day in AdvanceDay - applying any of them again here
            // would double-count a full turn's worth on top of the daily steps (the standing Phase
            // 1/4 wording, for the last time: this was the final turn-stepped economic system).
            // PreviewTurn keeps ALL the turn forms as their only remaining callers - it models one
            // whole turn on a throwaway clone without advancing days, which is exactly what the turn
            // forms are. ApplySectorGrowthEffect stays at the boundary above deliberately: it
            // FINALIZES PotentialGrowthRate from sources Phases 1-3 already move daily, and a
            // finalization tied to the spending resolution is a boundary decision, not a flow -
            // PotentialGDP then compounds daily at that standing rate.
            // Phase 2/3: PovertyRate, LFPR, the three crime indices and prison population run DAILY
            // in AdvanceDay; the Round 3 ordering constraint moved with them and is preserved there.

            // PHASE 5: the ONE macro step that stays at the boundary, deliberately - adaptive
            // expectations anchor to the period's closing print, and every daily form measurably
            // fails the equivalence bar (see MacroSystem's Phase 5 block comment). Reads the
            // boundary day's inflation, exactly what the turn regime always read.
            // Pass 6: NET of the tariff pass-through that actually printed on that day - the closing
            // period's applied term, captured above before the re-plan - so a price-level wedge never
            // enters the rate expectations (see ApplyInflationExpectations). Named, never positional.
            MacroSystem.ApplyInflationExpectations(state, lookThroughPp: closingAppliedPriceLevelTermsPp,
                anchorPercent: country.CurrencyZone != null ? country.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget);   // §385: forgotten toward the zone's target

            // Step 2: the formula keeps its exact pre-ledger body (the observation gate measured
            // a one-ulp codegen shift when recording lived inside it); the recorder recomputes
            // the terms AFTER, under the boundary audit's twin-drift detector. EnsureAccruing
            // runs BEFORE the formula: a run's FIRST boundary lazy-creates the ledger, and a
            // ledger created after the formula would open at the post-formula value - the audit
            // caught exactly that (observed Δ 0 vs nonzero terms, 2026-08-18).
            // MF-13 (K191): the formula carries the turn's one-off terms alone (the tax impulse, the spending change); the reversion and the sustained
            // terms ran on every day of the period (MacroSystem.ApplyApprovalDay in AdvanceDay), so approval takes no reversion step at the boundary.
            TaxTerm.Reclamp(country);   // A-LOOP q37 (review 878b's M-5): a base a clamped write left outside [0, 100] since the day began is put back first (T only; nothing where T is 0)
            float approvalBeforeFormula = country.State.ApprovalRating;
            ApprovalLedgerRecorder.EnsureAccruing(country, CurrentDate, approvalBeforeFormula);
            MacroSystem.ApplyApprovalRating(country, spendingResult.EffectiveDecision, taxTermImpulse, spendingResult.MandatorySpendingChangeThisTurn);
            MacroSystem.RecordApprovalAttribution(country, spendingResult.EffectiveDecision, taxTermImpulse, spendingResult.MandatorySpendingChangeThisTurn, CurrentDate, approvalBeforeFormula);
            TaxTerm.BookFormula(country, taxTermImpulse, approvalBeforeFormula, country.ApprovalLedgerAccruing);   // A-LOOP q37: what landed of the turn's impulse into T, after the formula and its twin read T (the reversion reads approval + T)

            BoundaryLedger?.Invoke(country, "close");
            EconomicEvent economicEvent = EventSystem.TryRollEvent();
            _lastEventsByCountry[country.Id] = economicEvent;
            // Step 2: observed, not recomputed - the ledger records the post-clamp delta the
            // event actually landed (which can be smaller than its face value at the [0,100]
            // edges). Zero deltas are skipped inside RecordEvent.
            float approvalBeforeEvent = country.State.ApprovalRating;
            EventSystem.ApplyEvent(country, economicEvent);
            BoundaryLedger?.Invoke(country, "events");
            if (economicEvent != null)
            {
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, economicEvent.Name, country.State.ApprovalRating - approvalBeforeEvent);
            }

            // Step 2: the period closes AFTER the boundary event lands, so a boundary-day shock
            // belongs to the period the player just watched, never silently to the next one. The
            // close runs the Σ(events)+Σ(terms)+clamp == observed-Δ self-audit (ATTRIB: on red).
            ApprovalLedgerRecorder.CloseAtBoundary(country, CurrentDate);

            // Political Systems Overhaul Part A: unlike EconomicEvent, a fired decision needs a
            // player-picked response before its effect lands (see ResolveCabinetDecision) - appended,
            // not overwritten, though in practice this list is always empty going in, since
            // GameController blocks Advance Turn while any previous decision is still unresolved.
            if (!_pendingCabinetDecisionsByCountry.TryGetValue(country.Id, out var pendingDecisions))
            {
                pendingDecisions = new List<(CabinetPortfolio, CabinetDecision)>();
                _pendingCabinetDecisionsByCountry[country.Id] = pendingDecisions;
            }
            // PS-3g (§634): a decision reaches the player where its portfolio is the player's - and the roll is not made at all for a player holding none (the reader: a roll draws the RNG per minister; the whole skip keeps a film's frames where they were).
            Country rolling = _world.GetCountry(country.Id);
            bool holdsAny = PlayerMayIntroduce(country.Id, out _) || (rolling?.Government != null && rolling.Government.Portfolios.TryGetValue(rolling.PlayerPartyAbbrev ?? string.Empty, out List<CabinetPortfolio> heldByPlayer) && heldByPlayer.Count > 0);
            // §755 (ruling A2: nothing is frozen; the partner acts through the stance only): the Finance decision is the head's like any other - §716's
            // partner no longer resolves it over a player head
            if (holdsAny) { foreach ((CabinetPortfolio portfolio, CabinetDecision rolled) in CabinetSystem.TryRollDecisions(country)) { if (PlayerMayIntroduce(country.Id, portfolio, out _)) { pendingDecisions.Add((portfolio, rolled)); } } }   // PS-3c (§630): a cabinet decision is the government's - none rolls for a player country the AI governs
            // P2-5.2: LOYALTY's term - under pressure, a disloyal minister may resign or leak; the record goes to the
            // country for the Docket, the approval move to the ledger like any cabinet event.
            foreach (CabinetEventRecord cabinetEvent in CabinetSystem.TryRollCabinetEvents(country, CurrentDate))
            {
                UnityEngine.Debug.Log($"CABINET: {cabinetEvent.Text}");
                if (cabinetEvent.Kind == CabinetEventKind.Leak) { ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"Cabinet leak ({cabinetEvent.Portfolio})", cabinetEvent.ApprovalDelta); }
            }
        }

        /// <summary>
        /// Estimates what ApplyDomesticPolicy would do to <paramref name="countryId"/> this turn
        /// under <paramref name="decision"/>, WITHOUT mutating the real World/Country/EconomyState
        /// or recording a FiscalTurnReport - runs the same formulas (this class's own private fiscal
        /// helpers plus MacroSystem's national accounts identity, Okun's Law, Phillips Curve, and
        /// ApplyApprovalRating) against a throwaway clone of the country's EconomyState, so the
        /// result stays grounded in the actual model rather than a separate hand-rolled estimate.
        ///
        /// Two deliberate simplifications, both because they'd otherwise require mutating shared
        /// state (the country's CurrencyZone can be shared with other countries, e.g. the Eurozone)
        /// just to compute a display-only estimate: the previewed interest rate is threaded through
        /// as a local value into ApplyNationalAccounts rather than actually changing the
        /// CurrencyZone, so GetInterestOnDebt's rate still reflects the current (not previewed) rate;
        /// and this turn's CurrencyStrength is used as-is rather than re-deriving its (heavily
        /// damped, slow-moving) drift for the preview's trade-balance estimate.
        ///
        /// Never rolls an EventSystem event and never advances CurrentTurn - a preview should be
        /// deterministic and side-effect-free, not spend part of the "will an event fire" randomness
        /// budget on a turn the player might not even commit to.
        /// </summary>
        public PolicyPreview PreviewTurn(CountryId countryId, PolicyDecision decision)
            => PreviewTurnOnClone(ClonePreviewCountry(_world.GetCountry(countryId)), countryId, decision);

        /// <summary>GA-8-1 (2026-10-14, the review's finding 5 and the second pass's findings 1-2): the calendar year a bill would pass in - the year a
        /// preview stamps on a carbon figure it passes, so it shows the boundary the real passage will meet. While the government's bill is before the
        /// chamber every bill is decided on its day (an alternative's own count is copied at tabling and never run down); a draft not yet tabled is
        /// tabled when a budget process is next open - now, or at the country's next fiscal-year start - and decided a bill's whole course later.</summary>
        private int PassageYear(CountryId countryId, BudgetBill bill)
        {
            BudgetBill government = GetPendingBudgetBill(countryId);
            DocketEntry desk = government?.Docket ?? (government?.ActsHeld != null && government.ActsHeld.Count > 0 ? government.ActsHeld[0] : null);
            if (desk != null) { return Elections.WorldClock.StatutePartsAreActs(countryId) ? desk.PresentedOn.Year : DocketLawDay(countryId, desk).Year; }   // SP-7b (the review's L1): a held Polish budget applies with its adoption year (FinishPolishBudget) - US-20: keyed on Poland's procedure, as the held budget's day is, not on its veto;   // PS-6 US-21b: voted and on the player-president's desk - law on that day at the latest (DECLARED: the year it is law unanswered); US-22c: its own deadline where it carries one
            if (government != null && government.DaysRemaining > 0) { return CurrentDate.AddDays(government.DaysRemaining).Year; }
            if (bill != null && bill.DaysRemaining > 0) { return CurrentDate.AddDays(bill.DaysRemaining).Year; }
            System.DateTime tabled = CurrentDate;
            if (!GetPendingBudgetProcess(countryId))
            {
                // PS-6 US-22: the next request day (every country's fiscal-year start but the USA's; for those the same date as before, a year on where passed)
                tabled = FiscalYearData.BudgetRequestDay(countryId, CurrentDate.Year);
                if (tabled <= CurrentDate) { tabled = FiscalYearData.BudgetRequestDay(countryId, CurrentDate.Year + 1); }
            }
            return tabled.AddDays(ParliamentSystem.BillDurationDays).Year;
        }

        /// <summary>AF-36 (Elias's ruling q36, 2026-10-18): the calendar year the player's budget draft would pass in (<see cref="PassageYear"/>) - the
        /// year a carry counts its next year from.</summary>
        public int DraftPassageYear(CountryId countryId) => PassageYear(countryId, null);

        /// <summary>AF-36: the calendar year of the last boundary crossed, 0 before the first - what tells a passed figure still standing from a stamp
        /// an older save left after its year (`CarbonRateStatute.PassedFigureStands`).</summary>
        public int LastBoundaryYear => CurrentTurn > 0 ? TurnBoundary(CurrentTurn).Year : 0;

        /// <summary>AF-36 (q36: "the slip and the Desk show the date the rate reverts"): the boundary at which a carbon figure standing for
        /// <paramref name="standsForYear"/> yields to its statute, from the coming boundary on (`CarbonRateStatute.YieldsOn`).</summary>
        public System.DateTime CarbonFigureYieldsOn(int standsForYear) => CarbonRateStatute.YieldsOn(standsForYear, CurrentTurn + 1, TurnBoundary);

        /// <summary>AF-36: the country's carbon line where a passed figure stands on it, with the day its statute takes it back; false where none
        /// stands (never passed, taken back, or a statute that moves nothing).</summary>
        public bool TryPassedCarbonFigure(CountryId countryId, out TaxLine line, out System.DateTime yieldsOn)
        {
            yieldsOn = default;
            Country country = _world?.GetCountry(countryId);
            line = country?.TaxLines.Find(t => t.IsPerTonne);
            if (country == null || !CarbonRateStatute.PassedFigureStands(country.Id, line, LastBoundaryYear)) { return false; }
            yieldsOn = CarbonFigureYieldsOn(line.PassedForYear);
            return true;
        }

        /// <summary>
        /// P3-C1 (2026-09-03): the preview WITH the budget draft enacted - the draft applied to the clone first
        /// (`ParliamentSystem.ApplyBillResult` as a PASS, the same application the boundary makes, on the clone's
        /// own tax lines, spending lines, welfare programs and fund), then the turn previewed on it. Against
        /// <see cref="PreviewTurn"/> on the same decision this is "with vs without this draft" - the scope line
        /// under the effects panel, made true. The clone comes from `ClonePreviewCountry` and nothing else.
        /// </summary>
        public PolicyPreview PreviewTurnWithBudgetDraft(CountryId countryId, PolicyDecision decision, BudgetBill draft)
        {
            Country clone = ClonePreviewCountry(_world.GetCountry(countryId));
            // MF-13 (review 3's R3-1): the draft's bill charges its tax impulse to approval at once (TaxTerm.ChargeApproval: approval - I, T + I); the
            // preview's approval change is measured from BEFORE the bill, so the charge is in it and the year's fading hands back only part of it -
            // the draft previews -0.81^4 x I a year on, the figure the game reaches, never the +0.57 I of the fading alone
            float approvalOrigin = clone.State.ApprovalRating;
            if (draft != null) { ParliamentSystem.ApplyBillResult(clone, draft, passed: true, ApplyBudgetBillSpendingAndSwf, PassageYear(countryId, draft)); }
            return PreviewTurnOnCloneFrom(clone, countryId, decision, approvalOrigin);
        }

        /// <summary>P4-B3: each sector's change over the previewed turn, from the shares recorded before it.</summary>
        private static Dictionary<SectorType, (float Output, float Employment, float Metric)> SectorDeltasSince(Country previewCountry, Dictionary<SectorType, (float Output, float Employment, float Metric)> before)
        {
            var deltas = new Dictionary<SectorType, (float Output, float Employment, float Metric)>();
            foreach (Sector sector in previewCountry.Sectors)
            {
                if (!before.TryGetValue(sector.Type, out (float Output, float Employment, float Metric) b)) { continue; }
                deltas[sector.Type] = (sector.OutputShareOfGdp - b.Output, sector.EmploymentShare - b.Employment, sector.SectorMetric - b.Metric);
            }
            return deltas;
        }

        /// <summary>
        /// P4-B3 (2026-09-04): the preview WITH a sector draft - the bill's five override tables handed to the clone the
        /// way a passed sector bill's are (ApplySectorBillEffects), so the sectors page's cost line and arrows read the
        /// preview's own figures. The decision's other overrides are the caller's as ever.
        /// </summary>
        public PolicyPreview PreviewTurnWithSectorDraft(CountryId countryId, PolicyDecision decision, SectorPolicyBill draft)
        {
            Country clone = ClonePreviewCountry(_world.GetCountry(countryId));
            float approvalOrigin = clone.State.ApprovalRating;
            if (draft != null)
            {
                var sectorDecision = new PolicyDecision
                {
                    SectorSubsidyOverrides = draft.SubsidyLevels,
                    SectorRegulationOverrides = draft.RegulationLevels,
                    SectorTaxCreditOverrides = draft.TaxCreditLevels,
                    SectorResearchGrantsOverrides = draft.ResearchGrantsLevels,
                    SectorDeregulationNationalizationOverrides = draft.DeregulationLevels
                };
                ApplySectorPolicyChanges(clone, sectorDecision);
            }
            return PreviewTurnOnCloneFrom(clone, countryId, decision, approvalOrigin);   // MF-13 (R3-1): measured from before the draft, as the budget draft is
        }

        /// <summary>
        /// C-C1: `PreviewTurn`'s body, taking a clone the caller has already made — so a caller can
        /// modify that clone FIRST (apply a draft bill to it) and preview the result. `PreviewTurn`
        /// itself is unchanged in behaviour: it makes the clone and calls this.
        ///
        /// ⚠ **The clone must come from `ClonePreviewCountry` and nothing else.** That method's
        /// hand-list is what keeps a preview from writing into the real game, and its own doc records
        /// three separate escapes (the R4-1 class, `BaselineGini`, the fiscal ledger) caught the hard
        /// way. A caller passing anything else is outside every guarantee this method has.
        /// </summary>
        private PolicyPreview PreviewTurnOnClone(Country previewCountry, CountryId countryId, PolicyDecision decision)
            => PreviewTurnOnCloneFrom(previewCountry, countryId, decision, previewCountry.State.ApprovalRating);

        /// <summary>MF-13 (review 3's R3-1): <see cref="PreviewTurnOnClone"/> with the approval its change is measured from given - a caller that
        /// applied a draft's bill to the clone first passes approval as it stood BEFORE the bill, so the bill's immediate charge is in the change.</summary>
        private PolicyPreview PreviewTurnOnCloneFrom(Country previewCountry, CountryId countryId, PolicyDecision decision, float approvalOrigin)
        {
            EconomyState state = previewCountry.State;
            // PS-6 K26 (Elias's ruling of 2026-10-23: "the budget preview shows the cut when the limit binds"): where the day tick has armed the played country's
            // cut (the limit binds, no raise law), the clone's year is cut as its days will be - its flows armed for the date the preview's flows read
            // (UsDebtLimit.CutToday); everywhere else nothing is armed and the preview is as before
            Country armedFor = _world?.GetCountry(countryId);
            if (armedFor != null && armedFor.DebtLimitCutFlowDay != System.DateTime.MinValue) { previewCountry.DebtLimitCutFlowDay = CurrentDate; }
            float debtLimitCutBefore = previewCountry.DebtLimitCutBillions;
            float crisisCutBefore = previewCountry.Crisis.CutBillions;   // MF q33

            float gdpBeforeThisTurn = state.GDP;
            float unemploymentBefore = state.Unemployment;
            float inflationBefore = state.Inflation;
            float approvalBefore = approvalOrigin;   // MF-13 (R3-1): the clone's approval, or the caller's before a draft's bill
            float budgetBefore = state.Budget;
            float povertyBefore = state.PovertyRate;
            float laborForceParticipationBefore = state.LaborForceParticipationRate;
            float crimeIndexBefore = state.CrimeIndex;
            // P4-B3: every sector's shares before the turn, for the deltas the sectors page draws as arrows.
            var sectorsBefore = new Dictionary<SectorType, (float Output, float Employment, float Metric)>();
            foreach (Sector sector in previewCountry.Sectors) { sectorsBefore[sector.Type] = (sector.OutputShareOfGdp, sector.EmploymentShare, sector.SectorMetric); }

            // §506 (2026-09-15): the boundary's FIRST step commits the year to the pyramid, with the levers as they stood through it (AdvanceTurn, CohortDemographics.CommitYear).
            // The preview never committed, so every line on a demographic driver indexed on the pyramid the year started from - a ratio of one, a year behind the
            // boundary (PreviewParityDiagnostic's identity section named them line by line). The step is taken here, before the decision moves a lever, and
            // written to the clone's own pyramid after the year's readings below, which read the pyramid the year started from as the day did.
            CohortDemographics.YearStep previewYearStep = CohortDemographics.Step(previewCountry, CohortDemographics.SubstrateYear(CurrentTurn));
            ApplyTariffRateChange(previewCountry, decision);
            ApplyPartnerTariffOverrides(previewCountry, decision);
            // Pass 5: the clone's tariff figure is threaded into its fiscal step below, exactly as the
            // real boundary plans it - the preview's NetBudgetImpact stays a true reading.
            float previewTariffRevenue = TradeSystem.ApplyTradeEffects(previewCountry, _world);
            // Pass 6: the price pass-through this turn's tariff decision would plan, against the take
            // the current period planned - the boundary's own expression (a preview seeds nothing:
            // StandingPlannedTariffRevenue reads the period with TryGetValue).
            float previewTariffPassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                    * 100f * (previewTariffRevenue - StandingPlannedTariffRevenue(countryId)) / Mathf.Max(gdpBeforeThisTurn, 1f)
                : 0f;

            CarbonRateStatute.AdvanceYear(previewCountry, CurrentTurn, ComingBoundaryDate.Year);   // EN-4e: the preview's boundary reads the same statute the turn will (the clone's own lines and reference)
            // §716 (the review's defect 4): the Finance partner's boundary step, as the boundary takes it - after the ministry's claim, which the preview
            // reads (Decide is pure) without running it. Computed on the real country (the clone carries no government; its lines are the real ones'),
            // written into the caller's decision for the preview and withdrawn before it returns. PREMISE, DECLARED (§773's review), accepted (Elias's ruling F7): in the player's Poland
            // the boundary puts the step's household rates to the Sejm as a tax act (PartnerTaxAct); the preview never votes it and counts those rates
            // whole, as D4's accepted premise counts a draft whole - the Finance-stance tile's veto clause is the warning beside it.
            Country previewedReal = _world.GetCountry(countryId);
            FinancePartner.Written previewPartner = null;
            if (previewedReal != null && PartnerStepsAtBoundary(previewedReal))
            {
                PolicyDecision claimedByMinistry = AiFinanceMinistryEnabled && !PlayerGoverns(previewedReal) ? AiFinanceMinistry.Decide(previewedReal, GetLastFiscalReport(countryId)) : null;
                previewPartner = FinancePartner.Apply(previewedReal, decision, ComingBoundaryDate, claimedByMinistry?.SpendingLineChanges.Keys, claimedByMinistry?.TaxRateOverrides.Keys);   // due as of the boundary the preview stands for; recorded never
            }
            float taxTermImpulse = ApplyTaxRateChanges(previewCountry, decision);
            ApplyWelfareGenerosityChanges(previewCountry, decision);
            ApplyMinimumWageChange(previewCountry, decision);
            ApplyCrimePolicyChanges(previewCountry, decision);
            ApplySectorPolicyChanges(previewCountry, decision);
            ApplySwfPolicyChanges(previewCountry, decision);
            ApplyLaborPolicyChanges(previewCountry, decision);
            ApplyCrimeJusticeDeeperChanges(previewCountry, decision);
            ApplyDemographicPolicyChanges(previewCountry, decision);
            // FT-8 (§398): the rule is read HERE, on the readings as they stand at the top of the turn - the real turn sets the zone rate in
            // CurrencySystem.ApplyInterestRateChanges before any country's boundary runs. Reading it after the clone's supply shock and natural-rate step
            // (as the preview did until now, masked because the clone carried no boundary reference) previewed a rate the turn never sets; PreviewParityDiagnostic caught it.
            float ruleReadingAtOpen = previewCountry.CurrentFedChair != null ? TaylorRule.GetSuggestedInterestRate(previewCountry) : 0f;
            float blendedAtOpen = previewCountry.CurrentFedChair == null && CurrencySystem.SharesCurrencyZoneWithOthers(previewCountry, _world) ? EurozoneRateSystem.GetBlendedSuggestedRate(_world, previewCountry) : 0f;
            CohortDemographics.ApplyTurn(previewCountry, CohortDemographics.SubstrateYear(CurrentTurn));   // F2 step 4: the year's readings on the clone's own pyramid
            if (previewYearStep.IsValid) { previewCountry.Cohorts.Counts = previewYearStep.Next; }   // §506: then the year committed on the clone, as the boundary commits it
            MacroSystem.ApplySupplyShockToUnemployment(previewCountry);   // FT-7 (§391): the preview reads the same boundary step, in the same place
            MacroSystem.ApplyNaturalRateFromLabourForce(previewCountry);   // FT-8 (§398)
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(previewCountry, decision);
            // EN-5: the electricity pass-through the clone's own year would plan - its household price against the standing real price, in the index's
            // weight (EnergyPassThrough.PlannedForPreview). EN-7a: read HERE, after the clone's spending resolves, as the boundary plans after its pressures
            // and ledger - read before, it missed the Energy subsidy's move of the energy line (the levy it displaces) and the sector draft, which the clone
            // takes only at ApplySectorPolicyChanges.
            EnergyFleet.Advance(previewCountry, CurrentTurn + 1, previewCountry.CalendarYear);   // §544: the clone lands ITS OWN due orders (a copy of the queue), as the boundary will - the preview cleared on the old fleet while the turn planned on the new
            float previewEnergyPassThroughPp = EnergyPassThrough.PlannedForPreview(previewCountry, out EnergyLedger.Book previewEnergyBook);
            float previewElectricityTaxRevenue = previewEnergyBook != null ? (float)previewEnergyBook.ElectricityTaxRevenueChange : 0f;   // EN-7b: the flow the turn will plan, from the same clearing
            MacroSystem.ApplyCategorySpendingEffects(previewCountry, spendingResult.EffectiveDecision);
            // Phase 1: the preview deliberately keeps the TURN-level forms. It models one whole turn on a
            // throwaway clone WITHOUT advancing any days, so the daily methods would never be called on it
            // - the turn forms are exactly the 121-day aggregate the preview is trying to show. These are
            // now their only remaining callers, which is correct rather than dead code.
            MacroSystem.ApplyInfrastructureCondition(previewCountry, spendingResult.EffectiveDecision);
            MacroSystem.ApplySectorEffects(previewCountry);
            MacroSystem.ApplySectorGrowthEffect(previewCountry, ComingBoundaryDate);   // K210 (the review's M1): the anchor the boundary it previews will apply, whatever day the preview is drawn
            MacroSystem.ApplyWelfareProgramEffects(previewCountry);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(previewCountry);
            float interestOnDebt = GetInterestOnDebt(previewCountry);
            float welfareCost = GetTotalWelfareCost(previewCountry);

            // Preview uses the deterministic AVERAGE return, never an actual random draw - PreviewTurn
            // is documented to be side-effect-free/deterministic (it never rolls an EventSystem event
            // either), and rolling SovereignWealthFundSystem's real isolated RNG here would consume part
            // of its sequence for a turn the player might not even commit to.
            float swfContribution = GetSwfContribution(previewCountry);
            float swfReturns = 0f;
            float swfDraw = 0f;
            float swfUnpaid = 0f;   // GA-2-1: as the day's - a draw delivers what the fund holds, summed in at each use
            float swfRefused = 0f;   // MF q33: as the day's - the ceiling refuses at booking
            float swfOverflow = 0f;   // MF q33: as the day's - what stands above the ceiling leaves with the draw
            if (previewCountry.SovereignWealthFund != null)
            {
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.NominalGdp;
                swfUnpaid = Mathf.Max(0f, -(previewCountry.SovereignWealthFund.TotalAssets + swfContribution));
                swfRefused = SwfContributionRefused(previewCountry.SovereignWealthFund, swfContribution, maxSwfAssets);
                previewCountry.SovereignWealthFund.TotalAssets += swfContribution + swfUnpaid - swfRefused;
                swfReturns = SovereignWealthFundSystem.GetAverageReturnEstimate(previewCountry.SovereignWealthFund);
                swfOverflow = Mathf.Max(0f, previewCountry.SovereignWealthFund.TotalAssets - maxSwfAssets);
                previewCountry.SovereignWealthFund.TotalAssets = Mathf.Max(0f, previewCountry.SovereignWealthFund.TotalAssets - swfOverflow);

                // Mirrors AdvanceTurn exactly: the budget sees the structural DRAW, not the return. The
                // preview must model the same flow the turn will, or the estimate it shows is of a
                // mechanism the game no longer has.
                swfDraw = previewCountry.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction();
                swfDraw = Mathf.Clamp(swfDraw, 0f, previewCountry.SovereignWealthFund.TotalAssets);
                previewCountry.SovereignWealthFund.TotalAssets -= swfDraw;
            }

            // C-C1: the two legs the Budget surfaces need are already produced here - the method
            // RETURNS revenue and OUTS total spending - and were being discarded into `out _`.
            // Capturing them adds no arithmetic and no second estimate: the figures a draft's
            // fiscal impact is built from are the boundary's own, which is pass 5's lesson on the
            // Budget "Net" line (never a hand sum beside the model).
            float previewRevenue = ApplyRevenueAndSpending(previewCountry, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution + swfUnpaid - swfRefused, swfDraw + swfOverflow, previewTariffRevenue, previewElectricityTaxRevenue, out float previewSpending, out _);

            // AF-7 (q10, GA-0-53): THE PREVIEW IS DAMPED AS THE PROJECTION IS - the year runs at the rate the boundary sets, the head closing its speed's
            // share of the gap to the target (RatePathProjection.Move; EurozoneRateSystem.ApplyEurozoneRate), where it ran at the undamped target until now.
            // The own-currency dial's branch is retired with the dial: a zone with neither a head nor partners holds its rate, as the boundary does.
            float previewedInterestRate = previewCountry.CurrencyZone.InterestRate;
            if (previewCountry.CurrentFedChair != null)
            {
                previewedInterestRate = RatePathProjection.Move(previewCountry.CurrencyZone.InterestRate,
                    RatePathProjection.TargetFor(ruleReadingAtOpen, FederalReserveSystem.PolicyHead(previewCountry)));   // FT-8 (§398): the reading at the top of the turn; K15: the acting Vice Chair on a holdover's days
            }
            else if (CurrencySystem.SharesCurrencyZoneWithOthers(previewCountry, _world))
            {
                float blended = blendedAtOpen;   // FT-8 (§398): the reading at the top of the turn
                float push = Mathf.Clamp(decision.InterestRateChange, -EurozoneRateSystem.MemberRatePushRange, EurozoneRateSystem.MemberRatePushRange);
                float target = Mathf.Clamp(blended + push + EurozoneRateSystem.HeadLean(previewCountry.CurrencyZone), CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                float current = previewCountry.CurrencyZone.InterestRate;
                previewedInterestRate = current + (target - current) * EurozoneRateSystem.RateAdjustmentSpeed;
            }
            // §506 (2026-09-15): the identity's G deflated as the day's is (P5-B6) - the preview handed the plan nominal, P times the G the day will read; PreviewParityDiagnostic
            // holds this figure to the boundary's. The boundary leaves the price level where the preview reads it; only the day moves it.
            float previewIdentityGovernment = MacroSystem.IdentityGovernmentConsumption(previewCountry, spendingResult.GovernmentSpending);   // T-3 form A (§507): the day's own method - k on the plan, deflated
            MacroSystem.ApplyNationalAccounts(previewCountry, previewIdentityGovernment, previewedInterestRate);
            MacroSystem.ApplyPotentialGdpGrowth(previewCountry);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(previewCountry, actualGrowthRate);
            float previewAppliedPriceLevelTermsPp = MacroSystem.ApplyPhillipsCurveInflation(previewCountry, previewTariffPassThroughPp + previewEnergyPassThroughPp);   // EN-5: both level terms
            MacroSystem.ApplyInflationExpectations(state, lookThroughPp: previewAppliedPriceLevelTermsPp,
                anchorPercent: previewCountry.CurrencyZone != null ? previewCountry.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget);   // §385: the preview reads the same form
            MacroSystem.ApplyPovertyRate(previewCountry);
            MacroSystem.ApplyLaborForceParticipationRate(previewCountry);
            MacroSystem.ApplyOrganizedCrimeIndex(previewCountry);
            MacroSystem.ApplyCorruptionIndex(previewCountry);
            MacroSystem.ApplyCrimeIndex(previewCountry);
            MacroSystem.ApplyCrimeEffects(previewCountry);
            MacroSystem.ApplyPrisonPopulationRate(previewCountry);

            TaxTerm.Reclamp(previewCountry);   // A-LOOP q37 (M-5): as the boundary does
            float previewApprovalBeforeFormula = state.ApprovalRating;
            MacroSystem.ApplyApprovalRating(previewCountry, spendingResult.EffectiveDecision, taxTermImpulse, spendingResult.MandatorySpendingChangeThisTurn);
            MacroSystem.RecordApprovalAttribution(previewCountry, spendingResult.EffectiveDecision, taxTermImpulse, spendingResult.MandatorySpendingChangeThisTurn, CurrentDate, previewApprovalBeforeFormula);
            TaxTerm.BookFormula(previewCountry, taxTermImpulse, previewApprovalBeforeFormula, previewCountry.ApprovalLedgerAccruing);   // A-LOOP q37: as the boundary books it
            // MF-13 (K191): the coming year of approval on the clone, as the real game will run it day by day - the tax term's fading first in each day
            // (TaxTerm.AdvanceDay: it hands back what fades of the impulse just booked, so a draft tax move previews its cost a year on, not its day-one
            // size; its fiscal-year reset empties only the clone's own net), then approval's daily step at the state the preview just resolved and its
            // turn's growth; both record into the clone's own fresh ledger (review 2's N-2)
            for (int d = 1; d <= DaysPerTurn; d++)
            {
                System.DateTime previewDay = CurrentDate.AddDays(d);
                TaxTerm.AdvanceDay(previewCountry, previewDay);
                MacroSystem.ApplyApprovalDay(previewCountry, actualGrowthRate, previewDay);
            }
            if (previewPartner != null) { FinancePartner.Withdraw(decision, previewPartner); }   // §716: the caller's decision as it came in, after every reader the boundary keeps it for

            return new PolicyPreview
            {
                // Step 2: the clone's own ledger, filled by the boundary formula's twin above and,
                // since MF-13 (K191), by the projected year's daily approval steps and tax-term fading -
                // the preview-parity diagnostic compares these terms against the real boundary's,
                // which is what turns a clone-escape into a mismatch that names its term.
                ApprovalTerms = previewCountry.ApprovalLedgerAccruing,
                GdpGrowthPercent = actualGrowthRate,
                UnemploymentChange = state.Unemployment - unemploymentBefore,
                InflationChange = state.Inflation - inflationBefore,
                ApprovalChange = state.ApprovalRating - approvalBefore,
                NetBudgetImpact = state.Budget - budgetBefore,
                RevenueEstimate = previewRevenue,
                SpendingEstimate = previewSpending,
                DebtLimitCut = previewCountry.DebtLimitCutBillions - debtLimitCutBefore,   // PS-6 K26: the year's cut on the clone, inside the spending above
                CrisisCut = previewCountry.Crisis.CutBillions - crisisCutBefore,   // MF q33: the closed market's cut on the clone, inside the spending above (L7: read at today, for the whole year)
                PovertyRateChange = state.PovertyRate - povertyBefore,
                LaborForceParticipationRateChange = state.LaborForceParticipationRate - laborForceParticipationBefore,
                CrimeIndexChange = state.CrimeIndex - crimeIndexBefore,
                SwfContributionEstimate = swfContribution + swfUnpaid - swfRefused,   // MF q33: less what the ceiling refuses. GA-2-1: the period's draw capped once, at the fund's assets today - the days cap it day by day, so the two agree unless the fund drains within the period
                SwfReturnsEstimate = swfReturns,
                SectorDeltas = SectorDeltasSince(previewCountry, sectorsBefore),   // P4-B3
                PreviewInflation = state.Inflation,
                PreviewEnergyPassThroughPp = previewEnergyPassThroughPp,   // EN-7a
                PreviewElectricityTaxRevenue = previewElectricityTaxRevenue,   // EN-7b
                PreviewUnemployment = state.Unemployment,
                PreviewNaturalUnemployment = previewCountry.EffectiveNaturalUnemploymentRate,   // FT-8 (§398): the figure the rule reads
                PreviewOutputGapPercent = TaylorRule.GetOutputGapPercent(previewCountry),
                PreviewRuleRate = TaylorRule.GetSuggestedInterestRate(previewCountry),
                PreviewedInterestRate = previewedInterestRate,
                PreviewIdentityGovernment = previewIdentityGovernment,   // §506
            };
        }

        /// <summary>
        /// C-C1 (Playtest-1 finding 3): **what a Budget bill DRAFT would do to the year's fiscal
        /// position, before it is enacted.**
        ///
        /// <para><b>Why this had to exist at all.</b> The Estimated Effects panel does not see a
        /// tax or spending draft and never has: `PolicyInputsChangedSinceLastPreview`'s own comment
        /// records that since step 5c/5d those drafts *"no longer change what the preview would show
        /// at all — they only ever reach the simulation via a passed bill"*. So the one screen a
        /// player reads before enacting was structurally blind to the change being drafted. This is
        /// the same hole `EstimateTradeBill` was built to fill for tariffs, and it is filled the same
        /// way.</para>
        ///
        /// <para><b>How the figures are produced.</b> Two throwaway clones, both from
        /// `ClonePreviewCountry`: one left standing, one with the draft applied through
        /// `ParliamentSystem.ApplyBillResult` — <b>the same delegate a PASSED bill uses</b>, so the
        /// estimate cannot drift from enactment by construction. Each is run through
        /// `PreviewTurnOnClone`, the model's own boundary. The deltas are the difference. **No
        /// figure here is a hand sum** (pass 5's lesson on the Budget "Net" line).</para>
        ///
        /// <para>⚠ <b>Applying a bill to a clone is only safe because `ClonePreviewCountry`'s
        /// hand-list already covers everything a bill mutates</b> — `TaxLines` (deep-cloned because
        /// `ApplyTaxRateChanges` writes `TaxLine.Rate`), `SpendingLines`, `WelfarePrograms` and
        /// `SovereignWealthFund?.Clone()`. That was verified field by field against
        /// `ApplyBillResult` and `ApplyBudgetBillSpendingAndSwf` before this method was written, and
        /// `BudgetDraftEstimateDiagnostic` asserts the real country is unchanged across an estimate
        /// rather than leaving it to the reading. A shared reference here would let a player's
        /// *draft* silently edit the running game.</para>
        ///
        /// <para>⚠ <b>The estimate is a POINT and carries no margin, and that is the honest form.</b>
        /// `PreviewTurn`'s own contract is that it *"never rolls an EventSystem event"* and is
        /// deterministic, so two runs of it have no spread to report: a ± here would be authored
        /// rather than measured, which is exactly what W-E3 ruled against when it printed a
        /// zero-width band as a point with its reason instead of as `x – x`. What the figure
        /// EXCLUDES is stated on the surface instead — event shocks, and `PreviewTurn`'s two recorded
        /// simplifications (the previewed interest rate is threaded as a local rather than written to
        /// a possibly-shared `CurrencyZone`; this turn's `CurrencyStrength` is used as-is).</para>
        ///
        /// <para>A full turn IS a year (`YearsPerTurn` = 1.0), so these are annual figures with no
        /// scaling applied — unlike the Estimated Effects panel, whose non-full-turn horizons are an
        /// explicitly linear/compounding DISPLAY re-scaling.</para>
        ///
        /// Side-effect-free: clones only, the real World untouched.
        /// </summary>
        public BudgetBillEstimate EstimateBudgetBill(CountryId countryId, BudgetBill bill)
        {
            Country real = _world.GetCountry(countryId);
            PolicyDecision decision = new PolicyDecision();

            PolicyPreview standing = PreviewTurnOnClone(ClonePreviewCountry(real), countryId, decision);

            Country proposed = ClonePreviewCountry(real);
            float approvalOrigin = proposed.State.ApprovalRating;   // MF-13 (R3-1): measured from before the bill, as the budget draft's preview is
            ParliamentSystem.ApplyBillResult(proposed, bill, passed: true, ApplyBudgetBillSpendingAndSwf, PassageYear(countryId, bill));
            PolicyPreview withBill = PreviewTurnOnCloneFrom(proposed, countryId, decision, approvalOrigin);

            return new BudgetBillEstimate
            {
                RevenueDelta = withBill.RevenueEstimate - standing.RevenueEstimate,
                SpendingDelta = withBill.SpendingEstimate - standing.SpendingEstimate,
                NetDelta = withBill.NetBudgetImpact - standing.NetBudgetImpact
            };
        }

        /// <summary>The tariff take the country's CURRENT fiscal period planned (what the next boundary's pass-through is measured against), read with TryGetValue so a preview or an estimate never seeds a period; before the first period exists, the seed take from the same pure function.</summary>
        /// <summary>§506 (2026-09-15): the government consumption the daily identity is handed today - the period's plan over the price level as it stands (since T-3 form A, §507, scaled by k), the day's own
        /// expression; NaN before the first period exists. The preview's parity section reads it right after a boundary.</summary>
        public float GetIdentityGovernmentConsumption(CountryId countryId)
            => _fiscalPeriods.TryGetValue(countryId, out FiscalPeriod period) ? MacroSystem.IdentityGovernmentConsumption(_world.GetCountry(countryId), period.PlannedGovernmentSpending) : float.NaN;   // T-3 form A (§507): the day's own method

        /// <summary>P5-B6: the period's planned take at the seed's prices - the real field when the period carries it, else the
        /// nominal figure over today's level (an older save). The pass-through compares real to real: a rate change moves it,
        /// the price level never does.</summary>
        private static float RealPlannedTariffRevenue(FiscalPeriod period, Country country)
            => period.PlannedTariffRevenueReal > 0f
                ? period.PlannedTariffRevenueReal
                : period.PlannedTariffRevenue / Mathf.Max(0.0001f, country.State.PriceLevel);

        private float StandingPlannedTariffRevenue(CountryId countryId)
        {
            return _fiscalPeriods.TryGetValue(countryId, out FiscalPeriod period)
                ? RealPlannedTariffRevenue(period, _world.GetCountry(countryId))
                : TradeSystem.ComputeTariffRevenue(_world.GetCountry(countryId), _world);
        }

        /// <summary>
        /// Pass 6 (2026-08-27): what a Trade bill DRAFT would do at the next boundary, for the Trade
        /// bill card - computed by the REAL functions on throwaway clones, never a hand sum (pass 5's
        /// own lesson on the Budget "Net" line): one clone at the standing rates, one with the bill
        /// applied through the same delegate a passed bill uses, both run through
        /// TradeSystem.ApplyTradeEffects (so the currency factor and the partners' mirrored rates are
        /// inside the figure) and ComputeTariffRevenue; the pass-through is the boundary's own
        /// expression against the take the current period planned. Drafts never reach PreviewTurn
        /// (BuildPlayerDecision carries no tariff terms), so this is the one surface a draft's cost can
        /// be read from. Side-effect-free: clones only, the real World untouched.
        /// </summary>
        public TradeBillEstimate EstimateTradeBill(CountryId countryId, TradePolicyBill bill)
        {
            Country real = _world.GetCountry(countryId);
            Country standing = ClonePreviewCountry(real);
            Country proposed = ClonePreviewCountry(real);
            ApplyTradeBillEffects(proposed, bill);

            float standingTake = TradeSystem.ApplyTradeEffects(standing, _world);
            float proposedTake = TradeSystem.ApplyTradeEffects(proposed, _world);
            return new TradeBillEstimate
            {
                Take = proposedTake,
                TakeDelta = proposedTake - standingTake,
                TradeBalanceDelta = proposed.State.TradeBalance - standing.State.TradeBalance,
                PassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                    ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                        * 100f * (proposedTake - StandingPlannedTariffRevenue(countryId)) / Mathf.Max(real.State.GDP, 1f)
                    : 0f
            };
        }

        /// <summary>
        /// A throwaway Country for PreviewTurn: its own EconomyState clone (so GDP/Inflation/etc.
        /// mutations never touch the real one), its own copy of the structural fields that
        /// MacroSystem.ApplyCategorySpendingEffects/ApplyTariffRateChange mutate (PotentialGrowthRate,
        /// BaseTariffRate), and its own deep-cloned TaxLines (ApplyTaxRateChanges mutates TaxLine.Rate,
        /// so these can't be shared references the way the CurrencyZone reference is) - but the SAME
        /// CurrencyZone reference (read-only here - see PreviewTurn's remarks on why its InterestRate
        /// is never written). CollectionEfficiency, BaseDebtInterestRateOverride,
        /// RiskPremiumSensitivity, ComfortableDebtToGdpPercent, and BaselinePovertyRate are all copied
        /// explicitly since none is a constructor parameter (each defaults the same as on a real
        /// Country) - without this the preview would overstate revenue for every country whose real
        /// CollectionEfficiency is below 1, overstate InterestOnDebt for a reserve-currency issuer (the
        /// USA) whose real risk-premium sensitivity is near zero, misjudge GetFiscalReactionMultiplier
        /// for every country whose real comfort anchor isn't the 60f default, and misjudge
        /// ApplyPovertyRate's baseline for every country whose real baseline isn't the 10f default.
        /// BaselineLaborForceParticipationRate (retired at F2 step 5), MinimumWageImplemented, MinimumWagePercentOfMedian, and
        /// BaselineMinimumWagePercentOfMedian are copied for the same reason - none is a constructor
        /// parameter, and ApplyMinimumWageChange mutates MinimumWagePercentOfMedian directly.
        /// SovereignWealthFund is deep-cloned (via SovereignWealthFund.Clone(), null-safe) for the
        /// same reason TaxLines/SpendingLines/WelfarePrograms are - ApplySwfPolicyChanges and the
        /// contribution/returns steps in PreviewTurn both mutate it, so it can't be a shared reference
        /// the way CurrentFedChair is.
        /// SpendingLines is deep-cloned for the same reason TaxLines is -
        /// ApplySpendingLineChanges mutates SpendingLine.Amount, so these can't be shared references
        /// either. WelfarePrograms is ALSO deep-cloned (via WelfareProgram.Clone()) for the same reason -
        /// ApplyWelfareGenerosityChanges mutates WelfareProgram.GenerosityLevel. TradePartners is ALSO
        /// deep-cloned (via TradePartner.Clone()) for the same reason -
        /// ApplyPartnerTariffOverrides mutates TradePartner.PlayerTariffOverride, so a shared reference
        /// would leak the preview's draft override into the real World the moment PreviewTurn ran, not
        /// just when the player commits it. CurrentFedChair is a shared (not cloned) reference -
        /// nothing in PreviewTurn or ApplyDomesticPolicy ever mutates a FedChair's own fields, only
        /// reads RateBias, so this is safe unlike TaxLines/SpendingLines/TradePartners/WelfarePrograms.
        /// </summary>
        private static Country ClonePreviewCountry(Country country)
        {
            return new Country(
                country.Id, country.Name, country.State.Clone(), country.CurrencyZone, country.BaseTariffRate,
                country.NaturalUnemploymentRate, country.PotentialGrowthRate, country.GovernmentSpendingRate,
                country.BenefitRatePerUnemployed)
            {
                TradePartners = ClonePreviewTradePartners(country.TradePartners),
                TaxLines = ClonePreviewTaxLines(country.TaxLines),
                RevenueBaseSeedGdp = country.RevenueBaseSeedGdp,   // P5-B3: the preview's revenue reads the same bases as the turn's
                RevenueBaseSeeds = country.RevenueBaseSeeds == null ? null : (float[])country.RevenueBaseSeeds.Clone(),
                PotentialGdpSeed = country.PotentialGdpSeed,   // P5-B7: the preview's potential is the turn's
                PotentialLabourSeed = country.PotentialLabourSeed,
                PotentialProductivityIndex = country.PotentialProductivityIndex,
                PotentialLabourAtLastTurn = country.PotentialLabourAtLastTurn,
                GovernmentConsumptionScale = country.GovernmentConsumptionScale,   // T-3 form A (§507): without it the clone's k is 0 and the preview's identity reads no government
                IdentityPropensitySeed = country.IdentityPropensitySeed,   // MF q29: the identity's seed references - without them the preview's identity keeps the old reversion
                IdentityAutonomousShareSeed = country.IdentityAutonomousShareSeed,
                DemandReference = country.DemandReference,   // K209: the reference the preview's identity reads
                TrendLabourAtLastTurn = country.TrendLabourAtLastTurn,   // K209 (resp929, sim929): the trend growth the gap form expects
                TrendPotentialGrowthRate = country.TrendPotentialGrowthRate,
                PriceLevelAtLastIndex = country.PriceLevelAtLastIndex,   // P5-B6
                Health = country.Health,   // P5-C2: the seeds are immutable after the seed; the preview reads them
                Education = country.Education,   // P5-C3: seeds, immutable after the seed
                Infrastructure = country.Infrastructure,   // P5-C4
                Environment = country.Environment,   // P5-C5
                MigrationPoverty = country.MigrationPoverty,   // P5-C6
                Effectiveness = new Dictionary<CabinetPortfolio, PortfolioEffectiveness>(country.Effectiveness),   // P5-C7: the preview records into its own copy
                SpendingLines = ClonePreviewSpendingLines(country.SpendingLines),
                WelfarePrograms = ClonePreviewWelfarePrograms(country.WelfarePrograms),
                // Seed-spread ruling (2026-08-27): the welfare anchor rides the hand-list too (the
                // R4-1 Clone-escape class) - without it the clone's WelfareEffectDelta would measure
                // every seeded program as a fresh implementation.
                BaselineWelfarePrograms = ClonePreviewWelfarePrograms(country.BaselineWelfarePrograms),
                // ⚠ C-N4 (2026-08-31): the TAX anchor rides the hand-list for exactly the same reason, and
                // it is the R4-1 clone-escape class caught a third time. `MacroSystem`'s disposable-income
                // term measures the household burden against these seeded rates; a clone without them
                // falls back to the LIVE rate for every line, so the clone's delta is always zero and the
                // PREVIEW would silently show a tax change as having no effect on output while the real
                // turn showed one. A preview that disagrees with the turn it previews is the one thing
                // `EstimateBudgetBill` exists to prevent.
                BaselineTaxRates = new Dictionary<TaxType, float>(country.BaselineTaxRates),
                SeedLevied = country.SeedLevied == null ? null : new List<TaxType>(country.SeedLevied),   // A-LOOP q34: the burden's seed side, as the anchor above
                TaxTermStock = country.TaxTermStock,   // A-LOOP q37: the tax term T - the preview books the turn's impulse into the clone's own
                TaxTermNet = country.TaxTermNet == null ? null : new Dictionary<TaxType, float>(country.TaxTermNet),   // A-LOOP q37: the year's booked net the impulse is read against, the clone's own copy
                // §497: the dial costs' trackers - the R4-1 clone-escape class a fourth time: a clone with zero trackers re-applied every
                // standing dial's whole cost on top of lines that already carried it, so the preview overstated spending off neutral
                AppliedJusticeEnforcementCost = country.AppliedJusticeEnforcementCost,
                AppliedBorderEnforcementCost = country.AppliedBorderEnforcementCost,
                AppliedSectorSupportCost = country.AppliedSectorSupportCost,
                AppliedEnergySupportCost = country.AppliedEnergySupportCost,   // EN-7a
                ApprovalEconomyStock = country.ApprovalEconomyStock,   // A-LOOP q15: the vote's economy stock - a value; the preview's projected year (MF-13, K191) moves the clone's copy day by day, never the real one
                VoteApprovalMarks = country.VoteApprovalMarks == null ? null : new List<ApprovalMark>(country.VoteApprovalMarks),   // A-LOOP q15: the approval term's references, the clone's own list
                FiscalVoteRecord = country.FiscalVoteRecord?.Copy(),   // A-LOOP q13: the vote's fiscal book, the clone's own copy (the preview runs no fiscal day and closes no year - its projected year runs approval's days alone, MF-13 - so it stands)
                SalienceShift = country.SalienceShift == null ? null : (float[])country.SalienceShift.Clone(),   // A-LOOP q5: what events hold of salience - the stance model's opinion cost reads it; the clone's own copy
                SalienceRampPerDay = country.SalienceRampPerDay == null ? null : (float[])country.SalienceRampPerDay.Clone(),   // K94: each issue's ramp step to its peak, the clone's own copy
                SalienceHoldDays = country.SalienceHoldDays == null ? null : (int[])country.SalienceHoldDays.Clone(),   // A-LOOP q5: the days each issue's jump is still held, the clone's own copy

                // P-I2 stage 1: the pyramid, CLONED. Nothing reads it on the preview path yet, so this line
                // buys nothing today - it is here because the clone-escape class has now cost this pass twice
                // (BaselineTaxRates at C-N4, found only because two film frames differed), and a field added
                // to Country and not to this hand-list is a defect waiting for its first reader.
                Cohorts = country.Cohorts?.Clone(),
                // FT-7/FT-8 (§398): the boundary references ride the hand-list. Found while landing FT-8: the clone carried NO ParticipationAtLastBoundary since
                // §391, so the preview's supply shock seeded itself and moved nothing that year - the R4-1 clone-escape class, a fifth time. The three references
                // are copied so the preview's boundary reads the same year the turn will.
                ParticipationAtLastBoundary = country.ParticipationAtLastBoundary,
                StructuralParticipationAtLastBoundary = country.StructuralParticipationAtLastBoundary,
                CompositionNaturalRateAtSeed = country.CompositionNaturalRateAtSeed,
                // PS-3b (§629): WHO GOVERNS rides the hand-list - the R4-1 clone-escape class a sixth time, caught by the parity audit the day the
                // gate began to throw on a null: the preview's turn asks PlayerGoverns on the clone, and a clone without the record read as the
                // player's (§628) or throws (§629). A shared reference: nothing on the preview path writes the record.
                Government = country.Government,
                // PS-6 US-7: the US presidential contests ride beside who governs - a shared reference, for the same reason: nothing on the preview path writes them,
                // and the oath that reads them runs on the day loop, never on a clone
                UsPresidentialElections = country.UsPresidentialElections,
                // PS-6 US-16: the Congress contests ride beside them - shared, for the same reason (the count and the seating run on the day loop, never on a clone)
                UsCongressElections = country.UsCongressElections,
                // SP-7b: the player's candidate, the start and the office ride beside who governs - the preview's turn asks PlayerGoverns on the clone, which
                // reads the office (a player-president's party is the AI's)
                PlayerCandidate = country.PlayerCandidate,
                PlayerStart = country.PlayerStart,
                PlayerHoldsPresidency = country.PlayerHoldsPresidency,
                // PS-6 US-15: the second chamber rides the hand-list, COPIED - so a write on a clone never reaches the country (PreviewParityDiagnostic marks it by
                // content, a dictionary its value marks cannot reach)
                SecondChamberSeats = country.SecondChamberSeats == null ? null : new Dictionary<string, int>(country.SecondChamberSeats),
                // AF-30 (the review's H1): the chairs ride the hand-list, COPIED - the preview's turn reads EFFICIENCY (CabinetSystem.EfficiencyScale) and the
                // competence biases off them, and a clone without them previewed every chair of a governing player as vacant. The ministers are shared: nothing on
                // the preview path writes one (PreviewParityDiagnostic marks the dictionary by content, as the second chamber)
                CabinetMinisters = country.CabinetMinisters == null ? null : new Dictionary<CabinetPortfolio, CabinetMinister>(country.CabinetMinisters),
                Sectors = ClonePreviewSectors(country.Sectors),
                InfrastructureAssets = ClonePreviewInfrastructureAssets(country.InfrastructureAssets),
                CollectionEfficiency = country.CollectionEfficiency,
                BaseDebtInterestRateOverride = country.BaseDebtInterestRateOverride,
                RiskPremiumSensitivity = country.RiskPremiumSensitivity,
                // EN-7b: the electricity tax's statute AND its base - the ledger reads their difference on the preview path (EnergyPassThrough.PlannedForPreview); a value
                // without its base would preview the whole statute on top, a base without its value a repeal (the R4-1 clone-escape class)
                ElectricityTaxHouseholds = country.ElectricityTaxHouseholds,
                ElectricityTaxHouseholdsBase = country.ElectricityTaxHouseholdsBase,
                ElectricityTaxNonHouseholds = country.ElectricityTaxNonHouseholds,
                ElectricityTaxNonHouseholdsBase = country.ElectricityTaxNonHouseholdsBase,
                // R4: both maturity-lag fields ride the preview clone's hand-list - the R4-1
                // Clone-escape lesson applies to THIS list too (a missed field here silently
                // previews at the sentinel fallback, i.e. instant repricing).
                AverageDebtMaturityYears = country.AverageDebtMaturityYears,
                EffectiveDebtInterestRate = country.EffectiveDebtInterestRate,
                // Q1: BaselineGini joins the hand-list the day ApplyApprovalRating started reading
                // it - without this line the preview computes the Gini gap against the 30f field
                // default (a phantom -0.5/turn for the USA at its 39.5 seed). The R4-1
                // Clone-escape class, caught by the containment check BEFORE the bar this time.
                BaselineGini = country.BaselineGini,
                BillLostOn = country.BillLostOn,   // F5 (§779): the day's loss - nothing on the preview path reads it; a value, so a write on the clone never reaches the country
                AppropriatedThrough = country.AppropriatedThrough,   // PS-6 US-22: the funding horizon, the lapse and the withheld resolution - nothing on the preview path reads them; values, carried so the audit holds
                LapseSince = country.LapseSince,
                LapseRestoredOn = country.LapseRestoredOn,   // K42: read only by the Desk's chip, never on the preview path; a value, carried so the audit holds
                ContinuingResolutionWithheldBy = country.ContinuingResolutionWithheldBy,
                ContinuingResolutionWithheldOn = country.ContinuingResolutionWithheldOn,   // PS-6 K75: read only by the day tick, which the preview never runs; a value, carried so the audit holds
                ContinuingResolutionWithheldInOffice = country.ContinuingResolutionWithheldInOffice,   // PS-6 K75 (the review's L1): read only by the day tick; a value, carried so the audit holds
                LapseFlowDay = country.LapseFlowDay,   // PS-6 US-22b: the lapse's economics (UsLapse) - read only on the daily path, which the preview never runs; values, carried so the audit holds
                LapseDeferredOutlays = country.LapseDeferredOutlays,
                LapseOutputWedge = country.LapseOutputWedge,
                LapseOutputDays = country.LapseOutputDays,
                LapseProfileQuarter = country.LapseProfileQuarter,
                UnservedOutputLossRate = country.UnservedOutputLossRate,   // MF q26: the year's output loss and today's overlay - read only on the daily path, which the preview never runs; values, carried so the audit holds
                UnservedOutputWedge = country.UnservedOutputWedge,
                FurloughCountedMonth = country.FurloughCountedMonth,   // PS-6 US-22c: read only by the publication of the day, which the preview never runs; a value, carried so the audit holds
                MajorityReconciliationYear = country.MajorityReconciliationYear,   // PS-6 US-23: read only by the day tick, which the preview never runs; a value, carried so the audit holds
                DebtLimitUsd = country.DebtLimitUsd,   // PS-6 US-24: the debt limit - written by the day's bookkeeping and the day tick, which the preview never runs; values, carried so the audit holds
                DebtLimitSince = country.DebtLimitSince,
                DebtLimitSuspendedThrough = country.DebtLimitSuspendedThrough,
                DebtLimitReachedOn = country.DebtLimitReachedOn,
                DebtLimitCutFlowDay = country.DebtLimitCutFlowDay,   // read by the turn-form flows only on the day it is armed for, which the preview's own date is not (it runs on the day the tick armed the next)
                DebtLimitCutBillions = country.DebtLimitCutBillions,
                Crisis = country.Crisis?.Copy() ?? new SovereignCrisisState(),   // MF q33: the crisis state, the clone's own copy - the preview's issuance rate and its cut read it
                DebtLimitRaises = country.DebtLimitRaises,
                DebtLimitWithheldBy = country.DebtLimitWithheldBy,   // PS-6 K22: read only by the day tick's vote, which the preview never runs; a value, carried so the audit holds
                DebtLimitLetBind = country.DebtLimitLetBind,   // PS-6 K90: read only by the day tick and the hold, which the preview never runs; a value, carried so the audit holds
                // Step 2: a FRESH ledger, never the real country's reference - ApplyApprovalRating
                // records into it on the clone, and sharing the reference would corrupt the real
                // period's attribution the moment a preview ran. LastPeriod stays null: a preview
                // has no history and nothing reads it. The open date mirrors the real accruing
                // ledger's (this method is static, so no CurrentDate here); only the TERMS matter
                // to the parity diagnostic either way.
                // Step 2's third section: the clone carries NO fiscal ledger at all. The preview
                // never runs the daily path, so nothing on the clone could record into one - but
                // a memberwise clone would share the real country's REFERENCE, and the hand-list's
                // whole lesson (the BaselineGini class) is that a shared reference is a latent
                // escape. Null on both, and the parity diagnostic asserts the real ledger is
                // untouched across a preview.
                FiscalLedgerAccruing = null,
                FiscalLedgerLastPeriod = null,
                ApprovalLedgerAccruing = new ApprovalAttribution
                {
                    PeriodOpenDate = country.ApprovalLedgerAccruing != null ? country.ApprovalLedgerAccruing.PeriodOpenDate : default,
                    ApprovalAtPeriodOpen = country.State.ApprovalRating,
                    ApprovalAfterFormula = country.State.ApprovalRating   // MF-13: as every fresh ledger opens (ApprovalLedgerRecorder)
                },
                // Q3: the trend field rides too (its sentinel fallback happens to be exact here
                // - the property falls back to PotentialGrowthRate, copied above via the ctor -
                // but the hand-list carries it anyway: exact-by-fallback is a coincidence to a
                // future edit, exact-by-copy is not).
                ProductivityTrendGrowthRate = country.ProductivityTrendGrowthRate,
                ComfortableDebtToGdpPercent = country.ComfortableDebtToGdpPercent,
                BaselinePovertyRate = country.BaselinePovertyRate,
                MinimumWageImplemented = country.MinimumWageImplemented,
                MinimumWagePercentOfMedian = country.MinimumWagePercentOfMedian,
                BaselineMinimumWagePercentOfMedian = country.BaselineMinimumWagePercentOfMedian,
                BaselineCrimeIndex = country.BaselineCrimeIndex,
                PoliceFundingLevel = country.PoliceFundingLevel,
                SentencingSeverity = country.SentencingSeverity,
                CurrentFedChair = country.CurrentFedChair,
                FedViceChair = country.FedViceChair,   // K15: shared as CurrentFedChair is - the preview only reads its lean
                FedViceChairActing = country.FedViceChairActing,
                FedNominationPending = country.FedNominationPending,   // K16: shared - the preview never votes or lodges one
                FedSenateVotes = country.FedSenateVotes,
                SovereignWealthFund = country.SovereignWealthFund?.Clone(),
                FleetOrders = country.FleetOrders?.ConvertAll(o => o.Clone()),   // P6-F2 (§539): the connection queue, order by order
                PaidFamilyLeaveWeeks = country.PaidFamilyLeaveWeeks,
                BaselinePaidFamilyLeaveWeeks = country.BaselinePaidFamilyLeaveWeeks,
                OvertimeRegulationLevel = country.OvertimeRegulationLevel,
                RetrainingProgramLevel = country.RetrainingProgramLevel,
                BaselinePrisonPopulationRate = country.BaselinePrisonPopulationRate,
                BailReformLevel = country.BailReformLevel,
                DrugPolicyLevel = country.DrugPolicyLevel,
                BaselineOrganizedCrimeIndex = country.BaselineOrganizedCrimeIndex,
                BaselineCorruptionIndex = country.BaselineCorruptionIndex,
                JudicialFundingLevel = country.JudicialFundingLevel,
                BorderEnforcementLevel = country.BorderEnforcementLevel,
                BaselineDependencyRatio = country.BaselineDependencyRatio,
                BaselineNetMigrationRate = country.BaselineNetMigrationRate,
                FamilyPolicyLevel = country.FamilyPolicyLevel,
                ImmigrationPolicyLevel = country.ImmigrationPolicyLevel,
                BasePotentialGrowthRate = country.BasePotentialGrowthRate,
                InfrastructureSpendingGrowthAdjustment = country.InfrastructureSpendingGrowthAdjustment,
                // §506 (2026-09-15) - FOUND BY THE CLONE AUDIT T-3's condition asked for ("probe the preview path before it lands"): the value fields this list never carried,
                // each reading the constructor's default on the clone. The structural laws' bases (no preview path composes a law today; the day one does, it composes on these), the seeds FT-5, F4-2 and
                // RF-2 read (LaborTaxRateSeed at 0 dropped the preview's participation term; RealWageIndexAtLastIndex at 0 indexed an AI clone's caseload lines by no real wage
                // growth - with it alone dropped, the preview's identity G reads 0.09 to 1.20 per cent below the boundary's), the AI ministry's memory, the society baselines, the party fields.
                // PreviewParityDiagnostic's audit marks every value field and names any this list drops, so the next field added to Country is covered the day it lands.
                NaturalUnemploymentRateBase = country.NaturalUnemploymentRateBase,
                ComfortableDebtToGdpPercentBase = country.ComfortableDebtToGdpPercentBase,
                AverageDebtMaturityYearsBase = country.AverageDebtMaturityYearsBase,
                RiskPremiumSensitivityBase = country.RiskPremiumSensitivityBase,
                CollectionEfficiencyBase = country.CollectionEfficiencyBase,
                GovernmentSpendingRateBase = country.GovernmentSpendingRateBase,
                BenefitRatePerUnemployedBase = country.BenefitRatePerUnemployedBase,
                MinimumWagePercentOfMedianBase = country.MinimumWagePercentOfMedianBase,
                PaidFamilyLeaveWeeksBase = country.PaidFamilyLeaveWeeksBase,
                OvertimeRegulationBase = country.OvertimeRegulationBase,
                RetrainingProgramBase = country.RetrainingProgramBase,
                FamilyPolicyBase = country.FamilyPolicyBase,
                ImmigrationPolicyBase = country.ImmigrationPolicyBase,
                LaborTaxRateSeed = country.LaborTaxRateSeed,
                IncomeTaxSeedAer = country.IncomeTaxSeedAer,
                RealWageIndexAtLastIndex = country.RealWageIndexAtLastIndex,
                CalendarYear = country.CalendarYear,   // PN-1's driver (§520): the year the pension line's driver reads - the clone audit names it the day it is dropped
                CalendarDay = country.CalendarDay,   // K80: the day the dated sides are read on
                PensionAgeOverride = country.PensionAgeOverride,   // PN-1's dial (§590): a passed bill's pension age - the preview's line reads it as the turn's does
                PensionParticipationApplied = country.PensionParticipationApplied,   // §596 review D1: the response the state's rate already carries
                DebtRatioSeed = country.DebtRatioSeed,
                DebtRatioLastReport = country.DebtRatioLastReport,
                DebtRatioReportBefore = country.DebtRatioReportBefore,
                BaselineYouthUnemploymentRate = country.BaselineYouthUnemploymentRate,
                BaselineLifeExpectancy = country.BaselineLifeExpectancy,
                BaselineIncomeTaxRate = country.BaselineIncomeTaxRate,
                TracksHousingOverburden = country.TracksHousingOverburden,
                BaselineHousingOverburden = country.BaselineHousingOverburden,
                BaselineHomeownership = country.BaselineHomeownership,
                PlayerPartyAbbrev = country.PlayerPartyAbbrev,
                PartyApprovalRating = country.PartyApprovalRating
            };
        }

        private static List<TradePartner> ClonePreviewTradePartners(List<TradePartner> tradePartners)
        {
            var clones = new List<TradePartner>(tradePartners.Count);
            foreach (TradePartner tradePartner in tradePartners)
            {
                clones.Add(tradePartner.Clone());
            }
            return clones;
        }

        private static List<TaxLine> ClonePreviewTaxLines(List<TaxLine> taxLines)
        {
            var clones = new List<TaxLine>(taxLines.Count);
            foreach (TaxLine taxLine in taxLines)
            {
                clones.Add(taxLine.Clone());
            }
            return clones;
        }

        private static List<InfrastructureAsset> ClonePreviewInfrastructureAssets(List<InfrastructureAsset> infrastructureAssets)
        {
            var clones = new List<InfrastructureAsset>(infrastructureAssets.Count);
            foreach (InfrastructureAsset asset in infrastructureAssets)
            {
                clones.Add(asset.Clone());
            }
            return clones;
        }

        private static List<Sector> ClonePreviewSectors(List<Sector> sectors)
        {
            var clones = new List<Sector>(sectors.Count);
            foreach (Sector sector in sectors)
            {
                clones.Add(sector.Clone());
            }
            return clones;
        }

        private static List<SpendingLine> ClonePreviewSpendingLines(List<SpendingLine> spendingLines)
        {
            var clones = new List<SpendingLine>(spendingLines.Count);
            foreach (SpendingLine spendingLine in spendingLines)
            {
                clones.Add(spendingLine.Clone());
            }
            return clones;
        }

        private static List<WelfareProgram> ClonePreviewWelfarePrograms(List<WelfareProgram> welfarePrograms)
        {
            var clones = new List<WelfareProgram>(welfarePrograms.Count);
            foreach (WelfareProgram welfareProgram in welfarePrograms)
            {
                clones.Add(welfareProgram.Clone());
            }
            return clones;
        }

        /// <summary>Direct tariff-policy control: the country's own BaseTariffRate moves by the requested change, clamped to a sane range.</summary>
        private void ApplyTariffRateChange(Country country, PolicyDecision decision)
        {
            country.BaseTariffRate = Mathf.Clamp(country.BaseTariffRate + decision.TariffRateChange, MinBaseTariffRate, MaxBaseTariffRate);
        }

        /// <summary>
        /// Sets a per-partner tariff override directly (an absolute target, not a delta - same
        /// semantics as ApplyTaxRateChanges) for every partner with an entry in
        /// PolicyDecision.PartnerTariffOverrides, clamped to the same [MinBaseTariffRate,
        /// MaxBaseTariffRate] range BaseTariffRate itself uses. A no-op for any partner with no entry
        /// this turn - once set, TradePartner.PlayerTariffOverride persists turn to turn like
        /// TaxLine.Rate does, until the player either changes it again or resets it back to "no
        /// override" (a separate, immediate action - see TradePartner.PlayerTariffOverride's doc
        /// comment). Runs before TradeSystem.ApplyTradeEffects each turn (same ordering as
        /// ApplyTariffRateChange) so this turn's override is what this turn's trade actually resolves
        /// against.
        /// </summary>
        private void ApplyPartnerTariffOverrides(Country country, PolicyDecision decision)
        {
            foreach (TradePartner tradePartner in country.TradePartners)
            {
                if (decision.PartnerTariffOverrides.TryGetValue(tradePartner.PartnerId, out float requestedRate))
                {
                    tradePartner.PlayerTariffOverride = Mathf.Clamp(requestedRate, MinBaseTariffRate, MaxBaseTariffRate);
                }
            }
        }

        /// <summary>
        /// Sets every currently-implemented TaxLine's Rate directly to this turn's requested
        /// PolicyDecision.TaxRateOverrides value (clamped to that TaxLine's own TaxTypeRateRanges), a
        /// no-op for any TaxType with no entry or that isn't implemented - implementing/removing a
        /// tax is a separate, immediate action on TaxLine.IsImplemented, not something this method
        /// does. Returns the tax term's impulse of the moves (A-LOOP q37, `TaxTerm.Move`: each line's P(net after) − P(net before) against
        /// the fiscal year's opening, a cut at the smaller mirror; the caller books what lands into `Country.TaxTermStock` after the formula, `TaxTerm.BookFormula`), computed here - around the write of
        /// TaxLine.Rate - since PolicyDecision only carries the absolute target, not a delta; the caller threads this
        /// into MacroSystem.ApplyApprovalRating's tax term (before q37: the sum of every positive rate increase).
        /// </summary>
        private float ApplyTaxRateChanges(Country country, PolicyDecision decision)
        {
            float taxTermImpulse = 0f;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented)
                {
                    continue;
                }

                if (!decision.TaxRateOverrides.TryGetValue(taxLine.Type, out float requestedRate))
                {
                    continue;
                }

                float clampedRate = Mathf.Clamp(requestedRate, taxLine.MinRate, taxLine.MaxRate);
                if (clampedRate == taxLine.Rate) { continue; }   // no move, no impulse (exactly 0)

                // A-LOOP q37 (`docs/specs/A_LOOP_PLAN.md` §2 (2)): the move's impulse against the fiscal year's opening, in the term's one unit (D-33:
                // effective-rate points, the dial's per cent for a per-tonne line - EN-4c's scale), added to the line's net and returned for the formula
                float fromPoints = ParliamentSystem.TaxLevelPoints(country, taxLine);
                taxLine.Rate = clampedRate;
                taxTermImpulse += TaxTerm.Move(country, taxLine.Type, fromPoints, ParliamentSystem.TaxLevelPoints(country, taxLine));
            }

            return taxTermImpulse;
        }

        /// <summary>
        /// Sets every currently-implemented WelfareProgram's GenerosityLevel directly to this turn's
        /// requested PolicyDecision.WelfareGenerosityOverrides value (clamped to [MinGenerosityLevel,
        /// MaxGenerosityLevel]), a no-op for any WelfareProgramType with no entry or that isn't
        /// implemented - implementing/removing a welfare program is a separate, immediate action on
        /// WelfareProgram.IsImplemented, not something this method does. Mirrors ApplyTaxRateChanges
        /// exactly, minus the hike-tracking return value - welfare's approval effect is an ongoing
        /// STOCK effect of the current GenerosityLevel (see MacroSystem.GetWelfareApprovalEffect), not
        /// a one-time "this-turn change" penalty/bonus like the tax-hike term, so there's nothing
        /// equivalent to thread through here.
        /// </summary>
        private void ApplyWelfareGenerosityChanges(Country country, PolicyDecision decision)
        {
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                if (!decision.WelfareGenerosityOverrides.TryGetValue(program.Type, out float requestedGenerosity))
                {
                    continue;
                }

                program.GenerosityLevel = Mathf.Clamp(requestedGenerosity, MinGenerosityLevel, MaxGenerosityLevel);
            }
        }

        /// <summary>
        /// Sets Country.MinimumWagePercentOfMedian directly to this turn's requested
        /// PolicyDecision.MinimumWageOverride value (clamped to [MinMinimumWagePercent,
        /// MaxMinimumWagePercent]) - a no-op if the country has no statutory minimum wage
        /// (Country.MinimumWageImplemented false, e.g. Sweden/Italy) or no request was made this
        /// turn (the -1 sentinel). Mirrors ApplyTaxRateChanges/ApplyWelfareGenerosityChanges' "SET,
        /// not delta" pattern - there is no implement/remove action, since whether a country has a
        /// statutory minimum wage at all is a structural fact, not a player choice.
        /// </summary>
        private void ApplyMinimumWageChange(Country country, PolicyDecision decision)
        {
            if (!country.MinimumWageImplemented || decision.MinimumWageOverride < 0f)
            {
                return;
            }

            country.MinimumWagePercentOfMedian = Mathf.Clamp(decision.MinimumWageOverride, MinMinimumWagePercent, MaxMinimumWagePercent);
        }

        /// <summary>
        /// Sets Country.PoliceFundingLevel/SentencingSeverity directly to this turn's requested
        /// PolicyDecision overrides (each clamped to [MinPolicyDialLevel, MaxPolicyDialLevel]) - a
        /// no-op for either dial with no request this turn (the -1 sentinel). Every country has both
        /// dials (unlike minimum wage's country-specific asymmetry) - police funding and sentencing
        /// policy are universal government functions.
        /// </summary>
        private void ApplyCrimePolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.PoliceFundingOverride >= 0f)
            {
                country.PoliceFundingLevel = Mathf.Clamp(decision.PoliceFundingOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.SentencingSeverityOverride >= 0f)
            {
                country.SentencingSeverity = Mathf.Clamp(decision.SentencingSeverityOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// Sets each Sector's SubsidyLevel/RegulationLevel/TaxCreditLevel/ResearchGrantsLevel/
        /// DeregulationNationalizationLevel directly to this turn's requested PolicyDecision overrides
        /// (clamped to [MinSectorDialLevel, MaxSectorDialLevel]) - a no-op for any SectorType with no
        /// entry in the corresponding dictionary, the same "only requested entries matter" pattern
        /// TaxRateOverrides/WelfareGenerosityOverrides already use.
        /// </summary>
        private void ApplySectorPolicyChanges(Country country, PolicyDecision decision)
        {
            foreach (Sector sector in country.Sectors)
            {
                if (decision.SectorSubsidyOverrides.TryGetValue(sector.Type, out float requestedSubsidy))
                {
                    sector.SubsidyLevel = Mathf.Clamp(requestedSubsidy, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorRegulationOverrides.TryGetValue(sector.Type, out float requestedRegulation))
                {
                    sector.RegulationLevel = Mathf.Clamp(requestedRegulation, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorTaxCreditOverrides.TryGetValue(sector.Type, out float requestedTaxCredit))
                {
                    sector.TaxCreditLevel = Mathf.Clamp(requestedTaxCredit, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorResearchGrantsOverrides.TryGetValue(sector.Type, out float requestedResearchGrants))
                {
                    sector.ResearchGrantsLevel = Mathf.Clamp(requestedResearchGrants, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorDeregulationNationalizationOverrides.TryGetValue(sector.Type, out float requestedDeregulation))
                {
                    sector.DeregulationNationalizationLevel = Mathf.Clamp(requestedDeregulation, MinSectorDialLevel, MaxSectorDialLevel);
                }
            }
        }

        /// <summary>
        /// Sets SovereignWealthFund's contribution rate/allocation/asset weights directly to this
        /// turn's requested PolicyDecision overrides (each clamped to its own range) - a no-op entirely
        /// if the country has no fund (Country.SovereignWealthFund null) or for any individual field
        /// with no request this turn (the -1 sentinel - SwfContributionRateOverride uses
        /// float.MinValue instead, see PolicyDecision's own remarks on that field).
        /// </summary>
        private void ApplySwfPolicyChanges(Country country, PolicyDecision decision)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            if (fund == null)
            {
                return;
            }

            if (decision.SwfContributionRateOverride > float.MinValue)
            {
                fund.ContributionRatePercent = Mathf.Clamp(decision.SwfContributionRateOverride, MinSwfContributionRate, MaxSwfContributionRate);
            }

            if (decision.SwfDomesticAllocationOverride >= 0f)
            {
                fund.DomesticAllocationPercent = Mathf.Clamp(decision.SwfDomesticAllocationOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfEquitiesWeightOverride >= 0f)
            {
                fund.EquitiesWeight = Mathf.Clamp(decision.SwfEquitiesWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfBondsWeightOverride >= 0f)
            {
                fund.BondsWeight = Mathf.Clamp(decision.SwfBondsWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfInfrastructureWeightOverride >= 0f)
            {
                fund.InfrastructureWeight = Mathf.Clamp(decision.SwfInfrastructureWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfRealEstateWeightOverride >= 0f)
            {
                fund.RealEstateWeight = Mathf.Clamp(decision.SwfRealEstateWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }
        }

        /// <summary>
        /// This turn's sovereign-wealth-fund contribution - GDP * ContributionRatePercent/100. 0 for
        /// a country with no fund. A NEW BUDGET EXPENSE when ContributionRatePercent is positive (the
        /// original mechanic); when the player has set it negative (a drawdown - see
        /// MinSwfContributionRate), this same figure is negative, which ApplyRevenueAndSpending's
        /// plain sum already treats as a revenue offset rather than an expense - no separate
        /// withdrawal code path was needed. Whichever sign it has, TotalAssets and the budget move by
        /// this amount (see AccrueDailyFiscalFlows/PreviewTurn) - except that a drawdown delivers only
        /// what the fund holds: the fund can't be drawn down past empty, and the budget books the part
        /// the fund paid. ⚠ CORRECTED, GA-2-1 (2026-10-14): this doc used to say the clamp at 0 made
        /// the drawdown correct, while the budget booked the whole figure - an empty fund at a negative
        /// rate paid the budget from nothing every day.
        /// </summary>
        private float GetSwfContribution(Country country)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            return fund == null ? 0f : country.State.NominalGdp * (fund.ContributionRatePercent / 100f);   // P5-B6: nominal
        }

        /// <summary>
        /// Sets Country.PaidFamilyLeaveWeeks/OvertimeRegulationLevel/RetrainingProgramLevel directly
        /// to this turn's requested PolicyDecision overrides (each clamped to its own range) - a
        /// no-op for any individual field with no request this turn (the -1 sentinel). Every country
        /// has all three (unlike MinimumWage's country-specific asymmetry) - these are universal
        /// government policy levers, even for a country with 0 weeks of paid leave today.
        /// </summary>
        private void ApplyLaborPolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.PaidFamilyLeaveWeeksOverride >= 0f)
            {
                country.PaidFamilyLeaveWeeks = Mathf.Clamp(decision.PaidFamilyLeaveWeeksOverride, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks);
            }

            if (decision.OvertimeRegulationOverride >= 0f)
            {
                country.OvertimeRegulationLevel = Mathf.Clamp(decision.OvertimeRegulationOverride, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (decision.RetrainingProgramOverride >= 0f)
            {
                country.RetrainingProgramLevel = Mathf.Clamp(decision.RetrainingProgramOverride, MinLaborDialLevel, MaxLaborDialLevel);
            }
        }

        /// <summary>
        /// Sets Country.BailReformLevel/DrugPolicyLevel/JudicialFundingLevel/BorderEnforcementLevel
        /// directly to this turn's requested PolicyDecision overrides (each clamped to
        /// [MinPolicyDialLevel, MaxPolicyDialLevel], the same range Crime &amp; Justice Basics' own
        /// dials use) - a no-op for any with no request this turn (the -1 sentinel). The last two were
        /// added in Round 3 item 3.
        /// </summary>
        private void ApplyCrimeJusticeDeeperChanges(Country country, PolicyDecision decision)
        {
            if (decision.BailReformOverride >= 0f)
            {
                country.BailReformLevel = Mathf.Clamp(decision.BailReformOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.DrugPolicyOverride >= 0f)
            {
                country.DrugPolicyLevel = Mathf.Clamp(decision.DrugPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.JudicialFundingOverride >= 0f)
            {
                country.JudicialFundingLevel = Mathf.Clamp(decision.JudicialFundingOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.BorderEnforcementOverride >= 0f)
            {
                country.BorderEnforcementLevel = Mathf.Clamp(decision.BorderEnforcementOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// Round 3 item 5, Part B: sets Country.FamilyPolicyLevel/ImmigrationPolicyLevel directly to
        /// this turn's requested PolicyDecision overrides (each clamped to [MinPolicyDialLevel,
        /// MaxPolicyDialLevel]) - a no-op for either with no request this turn (the -1 sentinel), the
        /// same pattern ApplyCrimeJusticeDeeperChanges already uses. Must run BEFORE
        /// CohortDemographics.Apply, which reads these same-turn freshly-set levels - the
        /// same "avoid a one-turn lag" timing requirement every other policy-apply method before it
        /// already follows.
        /// </summary>
        private void ApplyDemographicPolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.FamilyPolicyOverride >= 0f)
            {
                country.FamilyPolicyLevel = Mathf.Clamp(decision.FamilyPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.ImmigrationPolicyOverride >= 0f)
            {
                country.ImmigrationPolicyLevel = Mathf.Clamp(decision.ImmigrationPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// This turn's total welfare cost: the sum, over every implemented WelfareProgram, of
        /// GDP * (CostShareOfGdp / 100) * (GenerosityLevel / 100) - a new spending category alongside
        /// Mandatory/Discretionary/UnemploymentBenefitCost/InterestOnDebt (see ApplyRevenueAndSpending),
        /// deliberately NOT touching IncomeSecurity or any other existing SpendingLine. Treated as a
        /// transfer (excluded from MacroSystem's national accounts G term), the same reasoning already
        /// applied to Mandatory SpendingLines/UnemploymentBenefitCost/InterestOnDebt - welfare programs
        /// (UBI, means-tested transfers, healthcare/housing/childcare subsidies) are payments to
        /// individuals, not government purchases of goods and services.
        /// </summary>
        private float GetTotalWelfareCost(Country country)
        {
            // Seed-spread ruling (2026-08-27): live cost minus the cost of the portfolio AS SEEDED -
            // the sourced spending seeds already carry each country's real programs, so a program
            // implemented at seed books nothing here and only a player's deviation moves the budget
            // (a removal below the seed books NEGATIVE cost: spending below the sourced line). Bit-
            // identical to the pre-ruling sum while no country seeds a program (x - 0f == x).
            return WelfareCostOf(country.WelfarePrograms, country.State.NominalGdp) - WelfareCostOf(country.BaselineWelfarePrograms, country.State.NominalGdp);   // P5-B6: nominal
        }

        private static float WelfareCostOf(List<WelfareProgram> programs, float gdp)
        {
            float cost = 0f;
            if (programs == null)
            {
                return cost;
            }

            foreach (WelfareProgram program in programs)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                cost += gdp * (program.CostShareOfGdp / 100f) * (program.GenerosityLevel / 100f);
            }

            return cost;
        }

        /// <summary>
        /// This turn's total THEORETICAL tax revenue (before CollectionEfficiency): the sum, over
        /// every implemented TaxLine, of GDP * (Rate / 100) * BaseShareOfGdp. Tariffs is explicitly
        /// skipped even though it's never constructed as a TaxLine (see TaxType's doc comment) -
        /// defensive, so a future TaxLine accidentally created for it could never double-count the
        /// tariff flow, which enters ApplyRevenueAndSpending as its own term (pass 5: TradeSystem's
        /// figure, planned per period and accrued daily, outside CollectionEfficiency). See
        /// ApplyRevenueAndSpending for where CollectionEfficiency is applied to get the actual
        /// collected revenue.
        /// </summary>
        private float GetTotalTaxRevenue(Country country)
        {
            float revenue = 0f;
            float gdp = country.State.GDP;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented || taxLine.Type == TaxType.Tariffs)
                {
                    continue;
                }

                revenue += TaxBases.Revenue(country, taxLine);   // P5-B3: rate x the base, and the base follows its driver (the wage bill, consumption, housing, output) from its sourced share at the seed (TaxBases; D-16's share table beneath)
            }

            return revenue;
        }

        /// <summary>
        /// This turn's baseline government consumption expenditure - the country's structural share
        /// of GDP, before the player's discretionary category spending is added on top by the
        /// caller. Split out (rather than returning the combined total) so callers can report
        /// baseline and discretionary spending as separate line items. Only used for a country
        /// WITHOUT a detailed SpendingLines portfolio - see ResolveSpendingForTurn.
        /// </summary>
        private float GetBaselineGovernmentSpending(Country country)
        {
            return country.State.NominalGdp * (country.GovernmentSpendingRate / 100f);   // P5-B6: a share of nominal GDP
        }

        /// <summary>Bundles what ResolveSpendingForTurn resolves this turn's spending down to, for either mechanism (detailed SpendingLines or the legacy baseline+category-delta one).</summary>
        private class DetailedSpendingResult
        {
            public float BaselineGovernmentSpending;
            public float GovernmentSpending;
            public float DiscretionarySpendingChangeThisTurn;
            public float MandatorySpending;
            public float MandatorySpendingChangeThisTurn;
            public PolicyDecision EffectiveDecision;
        }

        /// <summary>Per-category actual dollar change observed this turn (post-clamp against MinSpendingLineAmountRatio/MaxSpendingLineAmountRatio) from ApplySpendingLineChanges, plus the total across Mandatory lines.</summary>
        private class SpendingLineChangeResult
        {
            public readonly Dictionary<SpendingCategory, float> ActualDollarChangeByCategory = new Dictionary<SpendingCategory, float>();
            public float MandatoryDollarChangeTotal;
        }

        /// <summary>
        /// For a country with a detailed SpendingLines portfolio (Phase 1: USA only): applies this
        /// turn's SpendingLineChanges to the Discretionary lines (ApplySpendingLineChanges), then G (the book's plan; the identity reads it
        /// scaled to the sourced share by k - T-3 form A, §507) is the sum of Discretionary line Amounts AFTER that change (Mandatory lines are transfers,
        /// excluded from G - same reasoning as UnemploymentBenefitCost/InterestOnDebt) and
        /// MandatorySpending is reported separately for ApplyRevenueAndSpending to add to total
        /// budget outflow. BaselineGovernmentSpending/DiscretionarySpendingChangeThisTurn are split
        /// as before-this-turn's-total / actual-net-change-observed (not the raw requested delta sum,
        /// since ApplySpendingLineChanges' seed-ratio clamp can clip an individual line's requested
        /// change) so
        /// their SUM always equals GovernmentSpending, matching the legacy mechanic's semantics below
        /// exactly - callers (e.g. GameController's "net" display) can keep subtracting both without
        /// double-counting this turn's change. EffectiveDecision maps this turn's per-category deltas
        /// onto the four legacy category-spending fields (see BuildEffectiveDecisionForDetailedSpending)
        /// so MacroSystem's existing category-effect/approval formulas keep working unmodified.
        ///
        /// For a country without one, this is exactly the old baselineGovernmentSpending +
        /// decision.TotalDiscretionarySpending mechanic, byte-for-byte unchanged.
        /// </summary>
        private DetailedSpendingResult ResolveSpendingForTurn(Country country, PolicyDecision decision)
        {
            if (country.SpendingLines.Count > 0)
            {
                IndexSpendingLines(country);   // P5-B2: the driver on every line that has one (the AI's real growth besides); the four growth and pressure calls this replaces are named in its doc
                // P5-C7 (board 9d): the REQUEST is the indexed line - every line's amount now, before the player's figure lands.
                var requestedByCategory = new Dictionary<SpendingCategory, float>();
                foreach (SpendingLine indexed in country.SpendingLines) { requestedByCategory[indexed.Category] = indexed.Amount; }
                ApplyEnforcementCostPressure(country);
                ApplySectorSupportCostPressure(country);   // P4-B3: the sector dials' support cost, the same idiom
                ApplyEnergySupportCostPressure(country);   // EN-7a: the Energy sector's subsidy on the energy line, where the levy rule reads it
                float discretionaryTotalBefore = GetSpendingLineTotal(country, mandatory: false);
                SpendingLineChangeResult changeResult = ApplySpendingLineChanges(country, decision);
                Effectiveness.Record(country, requestedByCategory);   // P5-C7: allocated / requested x the minister's efficiency, per portfolio
                float discretionaryTotalAfter = GetSpendingLineTotal(country, mandatory: false);
                float mandatoryTotal = GetSpendingLineTotal(country, mandatory: true);

                return new DetailedSpendingResult
                {
                    BaselineGovernmentSpending = discretionaryTotalBefore,
                    GovernmentSpending = discretionaryTotalAfter,
                    DiscretionarySpendingChangeThisTurn = discretionaryTotalAfter - discretionaryTotalBefore,
                    MandatorySpending = mandatoryTotal,
                    MandatorySpendingChangeThisTurn = changeResult.MandatoryDollarChangeTotal,
                    EffectiveDecision = BuildEffectiveDecisionForDetailedSpending(decision, changeResult)
                };
            }

            float baselineGovernmentSpending = GetBaselineGovernmentSpending(country);
            return new DetailedSpendingResult
            {
                BaselineGovernmentSpending = baselineGovernmentSpending,
                GovernmentSpending = baselineGovernmentSpending + decision.TotalDiscretionarySpending,
                DiscretionarySpendingChangeThisTurn = decision.TotalDiscretionarySpending,
                MandatorySpending = 0f,
                MandatorySpendingChangeThisTurn = 0f,
                EffectiveDecision = decision
            };
        }

        /// <summary>
        /// P5-B2 (2026-09-05) - THE LINES INDEX. Between the player's own changes every spending line follows its
        /// DRIVER where it has one (SpendingDrivers: pensions to the 65+ cohort, unemployment benefits to the
        /// unemployment rate, education to the 0–19 cohort, health to the age-cost index, …): the RATIO of the
        /// driver's level now to its level at the last index (the seed's level is captured by
        /// Country.CaptureStructuralBases, so the first year's change counts). A PINNED line holds its amount and
        /// takes none of it (its driver reference still advances, so unpinning resumes from today, not from the
        /// pin). The seed anchor indexes by the same factor, so the [0.2x, 3x] band the player's changes are clamped
        /// to follows the line.
        ///
        /// <para>THE PRICE TERM (P5-B6, 2026-09-05): every line carries the year's prices as the ratio of EconomyState.PriceLevel now
        /// to its level at the last index - the book is in current prices since B6 (GDP, the bases, the flows, the debt all
        /// carry the level), so a nominal line that follows it holds its real share. The paragraph that follows is B2's
        /// measurement of what the same term did BEFORE the book carried prices, kept as the reason B6 exists.</para>
        ///
        /// <para>NO PRICE TERM (B2, superseded by B6) - measured, not assumed. The sheet asked for inflation on every line. This
        /// model kept its book in CONSTANT PRICES: PotentialGDP grows at PotentialGrowthRate alone
        /// (MacroSystem.ApplyPotentialGdpGrowthDaily), GDP is solved against it, revenue is rates times that GDP,
        /// and the Inflation stat is a side book (the Phillips curve, read by the central bank, the real wage and
        /// approval) that no $B figure carries. The first build of this pass put prices on the lines, and the
        /// trajectory family showed what that is in a constant-price book: real spending growing at the inflation
        /// rate every year - every country's lines from a fifth or a half of GDP to three times GDP inside sixty
        /// years, the output-gap fixed point pulled up by a G that outgrows potential, unemployment to zero,
        /// inflation to its cap, GDP past 1e38 by t340 (§314 has the table). Prices belong on the lines the day the
        /// book is in current prices, which is its own BASELINE family (sheeted as P5-B6); until then a price term
        /// here is a real growth term wearing the wrong name.</para>
        ///
        /// <para>The AI's budget rule, stated (and RE-STATED at RF-2, 2026-09-07, §370: the real-growth factor below applies to a
        /// DRIVERLESS line only - a line with a B2 driver follows prices × its driver for the AI as for the player): a country the player
        /// does not govern has nobody to raise its lines, so its DRIVERLESS lines grow at the country's real PotentialGrowthRate each year - the annual budget its
        /// finance ministry would pass - which is what every line did before this pass (§312 measured it: potential
        /// growth exactly, nothing else). The player's own lines take their drivers ONLY: the budget the player set
        /// is still there next year, grown by what its drivers did, and real provision grows only when the player
        /// raises it. [AUTHORED-DRAFT] as a rule; the alternative (drivers alone for the AI too) drifts every AI
        /// budget below its economy at the real growth rate and re-opens the "debt to zero" path CLAUDE.md records
        /// for a G that stopped tracking GDP.</para>
        ///
        /// <para>Replaced here: ApplyDiscretionarySpendingGrowth and ApplyMandatorySpendingGrowth (both potential-rate
        /// growth on every line, the AI rule's ancestor) and the two bounded demographic pressures on the pension and
        /// health lines (ApplyDemographicPensionPressure, ApplyDemographicHealthcarePressure - at most 0.5 % and 0.4 %
        /// a year on a dependency gap), which the 65+ and age-cost drivers replace in full: a driver is the whole
        /// headcount, a pressure was a nudge on its change.</para>
        /// </summary>
        private void IndexSpendingLines(Country country)
        {
            bool aiBudget = !PlayerGoverns(country) || HoldsNoPolicyChange(country);   // PS-3a (§628): the AI's book wherever the player's party does not lead the government; K218: and in the no-policy-change book
            float realGrowth = aiBudget ? 1f + country.PotentialGrowthRate / 100f : 1f;
            // P5-B6 (2026-09-05): THE PRICE TERM RETURNS - the book is in current prices now, so a nominal line carries the
            // year's prices as the ratio of the price level now to the level at the last index (0 = a save from before: a
            // factor of 1 once). §314 withdrew this term because the book did not carry prices; B6 gave the book its prices
            // first (GDP, the bases, the flows, the debt), and the line follows.
            float prices = country.PriceLevelAtLastIndex > 0f ? country.State.PriceLevel / country.PriceLevelAtLastIndex : 1f;
            country.PriceLevelAtLastIndex = country.State.PriceLevel;
            // RF-2 re-formed as ruled (2026-09-07, §384): an AI country's CASELOAD lines carry the year's REAL WAGE growth on top of prices and their driver -
            // the ratio of the real wage index now to the index at the last indexation. The legal anchor is Sweden's income pension: Socialförsäkringsbalken
            // (2010:110) 58 kap. 10-12 §§ define the inkomstindex as the general income development (the relative change in average pensionable income,
            // 1999 = 100) and 62 kap. 42-43 §§ recalculate every income pension in payment by it each year (följsamhetsindexering); the service lines
            // (health, education, family, justice) are staff costs that move with wages - Baumol's finding, the reason stated, its paper billed (§384).
            // The driver-only form (§370) froze a caseload's real cost per head at the seed for a century and made five AI states net creditors; the
            // pre-RF-2 form grew every line with potential growth, a policy nobody took (§367). This form is the law's: a caseload, at the going wage.
            float wages = aiBudget && country.RealWageIndexAtLastIndex > 0f ? country.State.RealWageIndex / country.RealWageIndexAtLastIndex : 1f;
            country.RealWageIndexAtLastIndex = country.State.RealWageIndex;
            foreach (SpendingLine line in country.SpendingLines)
            {
                SpendingDriver driver = SpendingDrivers.Of(line.Category);
                float level = SpendingDrivers.Level(driver, country);
                float driverRatio = line.DriverReference > 0f && level > 0f ? level / line.DriverReference : 1f;   // the reference is the seed's level (Country.CaptureStructuralBases) or the last index's; 0 only on a save from before this pass
                line.DriverReference = level;
                line.LastYearAmount = line.Amount;   // P5-B5: the row's delta against last year reads this
                line.LastDriverRatio = driverRatio;  // P5-B5: the row's projection carries this forward
                // RF-2 (2026-09-07, §370): the real-growth factor applies to a DRIVERLESS line only. A line with a B2 driver is a caseload -
                // pensions on the 65+ cohort, health on the age-cost index, education on the youth cohort - and its real cost per head is the policy,
                // not a thing that grows with output on its own; the rule that grew it (§314) raised real provision per head with potential growth on
                // every line, which §367 measured at ×4.55 for the USA in a century against output ×4.29. A line with no driver is the discretionary
                // block a finance ministry sizes against the economy, and follows real potential growth as before. The player's lines are unchanged.
                bool driverless = driver == SpendingDriver.None;
                float factor = prices * driverRatio * (driverless ? realGrowth : wages);   // RF-2 re-formed (§384): a caseload at the going real wage, a discretionary line at the economy's growth
                factor = PensionNominalGuarantee(country.Id, line.Category, prices, driverRatio, driverless ? realGrowth : wages, factor);   // MF-14 (K201): no nominal pension cut in the USA and Germany
                line.SeedAmount *= factor;
                if (line.Pinned) { continue; }
                // SC-1 (ruled 2026-09-15, §503): the line's OWN PATH indexes and is clamped to its seed band; the applied dial cost rides the same factor
                // outside it (§497: a tracker left unindexed made the next boundary's difference re-apply the index's share of the cost every year).
                // The own path's product is taken here, before the trackers move. Zero cost at no policy: the old expression's value to the bit.
                IndexComposedLine(country, line, (line.Amount - AppliedDialCostOn(country, line)) * factor, factor);
            }
        }

        /// <summary>MF-14 (K201, 2026-10-28; prices may fall since MF-14): the pension line (`SpendingCategory.SocialSecurity`) of the USA and Germany is never
        /// cut in nominal terms per pensioner by deflation - the statutes' own guarantees, stored under `ElectionsData/macro/raw/pension_guarantee/`:
        /// <list type="bullet">
        /// <item>the USA - 42 U.S.C. 415(i) (USCODE-2024, title 42, sec. 415(i)(1)(B)): a cost-of-living computation quarter is one "with respect to which the
        /// applicable increase percentage is greater than zero" - a falling CPI pays no COLA and cuts nothing, so the line's PRICE term is floored at 1
        /// (the caseload and the real term still move);</item>
        /// <item>Germany - SGB VI § 68a (1) (Schutzklausel): "Abweichend von § 68 vermindert sich der bisherige aktuelle Rentenwert nicht" - the pension
        /// value, which follows the wage development, does not fall, so the line's PER-PENSIONER term (prices × the real term) is floored at 1 and the
        /// caseload alone moves it. DECLARED: § 68a (2)-(3)'s catch-up (the forgone cut offset against later rises, which are halved until it is
        /// cleared) is not modelled - the line keeps the floor and later rises in full.</item>
        /// </list>
        /// Every other country and line returns <paramref name="factor"/> unchanged, and so do these two wherever the guarantee does not bind.</summary>
        public static float PensionNominalGuarantee(CountryId id, SpendingCategory category, float prices, float driverRatio, float realTerm, float factor)
        {
            if (category != SpendingCategory.SocialSecurity) { return factor; }
            if (id == CountryId.USA && prices < 1f) { return driverRatio * realTerm; }
            if (id == CountryId.Germany && prices * realTerm < 1f) { return driverRatio; }
            return factor;
        }

        /// <summary>MF-14 (K201; the q14 review 2's N-2): a line's NEXT YEAR projection (`SpendingLine.ProjectNextYear`) under the same guarantee the boundary
        /// applies - the USA's and Germany's pension line projects no nominal fall per pensioner at a negative print (the projection carries no real term,
        /// so the per-pensioner term is the price term for both). Every other line, and these where the guarantee does not bind, project exactly as before.</summary>
        public static float ProjectLineNextYear(Country country, SpendingLine line, float inflationPercent)
        {
            float plain = line.ProjectNextYear(inflationPercent);
            if (country == null || line.Pinned) { return plain; }
            float prices = 1f + Mathf.Max(-50f, inflationPercent) / 100f;
            float driverRatio = line.LastDriverRatio > 0f ? line.LastDriverRatio : 1f;
            float factor = prices * driverRatio;
            float guaranteed = PensionNominalGuarantee(country.Id, line.Category, prices, driverRatio, 1f, factor);
            return guaranteed == factor ? plain : line.Amount * guaranteed;
        }

        /// <summary>SC-1: one line's index step - its dial costs ride the factor, then the line is written from its indexed own path (<see cref="ComposeLine"/>).</summary>
        private static void IndexComposedLine(Country country, SpendingLine line, float indexedOwnPath, float factor)
        {
            if (line == JusticeCostLine(country)) { country.AppliedJusticeEnforcementCost *= factor; }
            if (line == BorderCostLine(country)) { country.AppliedBorderEnforcementCost *= factor; }
            if (line == SectorCostLine(country)) { country.AppliedSectorSupportCost *= factor; }
            if (line == EnergyCostLine(country)) { country.AppliedEnergySupportCost *= factor; }
            ComposeLine(country, line, indexedOwnPath);
        }

        /// <summary>
        /// SC-1 (Elias's ruling, 2026-09-15, `COMPLETED.md` §503): the applied dial cost a line carries - the trackers whose landing line it is
        /// (<see cref="JusticeCostLine"/>, <see cref="BorderCostLine"/>, <see cref="SectorCostLine"/>, <see cref="EnergyCostLine"/>); zero on every other
        /// line and on every line at neutral dials. A composed line is its own path plus this.
        /// </summary>
        private static float AppliedDialCostOn(Country country, SpendingLine line)
        {
            float cost = 0f;
            if (line == JusticeCostLine(country)) { cost += country.AppliedJusticeEnforcementCost; }
            if (line == BorderCostLine(country)) { cost += country.AppliedBorderEnforcementCost; }
            if (line == SectorCostLine(country)) { cost += country.AppliedSectorSupportCost; }
            if (line == EnergyCostLine(country)) { cost += country.AppliedEnergySupportCost; }
            return cost;
        }

        /// <summary>
        /// SC-1 (ruled 2026-09-15, §503: *"the seed-relative clamp bounds the line's own path, not a cost the player deliberately set; the applied dial
        /// cost sits outside it and the composed total is the clamped line plus the applied cost"*): writes a line from its OWN PATH - clamped to the seed
        /// band - plus the applied dial costs it carries. Every writer of a line's amount comes through here: the index, the dial costs, the percent
        /// changes and the nominal targets. The one bound a cost meets is ZERO (a decision taken and sheeted in §504): a line carries no negative spending,
        /// so where a cut below neutral would take the total under, the cuts on the line record what the line could give - §498's idiom at that one
        /// bound - and the next boundary asks for the rest again.
        /// </summary>
        private static void ComposeLine(Country country, SpendingLine line, float ownPath)
        {
            float own = ClampToSeedRange(line, ownPath);
            float cost = AppliedDialCostOn(country, line);
            if (own + cost < 0f) { cost = HoldCutsAtZero(country, line, own); }
            line.Amount = own + cost;
        }

        /// <summary>SC-1: a line's own path - its amount less the applied dial costs it carries; the figure the seed band bounds and a percentage moves.
        /// The Budget row's track and the chamber's weight read it.</summary>
        internal static float OwnPathOf(Country country, SpendingLine line) => line.Amount - AppliedDialCostOn(country, line);

        /// <summary>SC-1: the applied dial cost a line carries, for a reader outside the boundary (the Budget row).</summary>
        internal static float DialCostOf(Country country, SpendingLine line) => AppliedDialCostOn(country, line);

        /// <summary>SC-1: the own path a figure set on a line would land it on - the figure, the line's total, less the cost it carries, clamped to the seed band
        /// (<see cref="ComposeLine"/>'s arithmetic on a nominal target, no tracker touched).</summary>
        internal static float LandedOwnPathOf(Country country, SpendingLine line, float figure) => ClampToSeedRange(line, Mathf.Max(0f, figure) - AppliedDialCostOn(country, line));

        /// <summary>SC-1: the total a figure set on a line would land at - its landed own path plus the cost, never below zero.</summary>
        internal static float LandedTotalOf(Country country, SpendingLine line, float figure) => Mathf.Max(0f, LandedOwnPathOf(country, line, figure) + AppliedDialCostOn(country, line));

        /// <summary>SC-1's zero bound: trims the negative dial costs on a line until the line's total is zero, and returns the cost it now carries (minus its own path).</summary>
        private static float HoldCutsAtZero(Country country, SpendingLine line, float own)
        {
            float excess = -(own + AppliedDialCostOn(country, line));
            if (line == JusticeCostLine(country)) { TrimCut(ref country.AppliedJusticeEnforcementCost, ref excess); }
            if (line == BorderCostLine(country)) { TrimCut(ref country.AppliedBorderEnforcementCost, ref excess); }
            if (line == SectorCostLine(country)) { TrimCut(ref country.AppliedSectorSupportCost, ref excess); }
            if (line == EnergyCostLine(country)) { TrimCut(ref country.AppliedEnergySupportCost, ref excess); }
            return -own;
        }

        private static void TrimCut(ref float applied, ref float excess)
        {
            if (excess <= 0f || applied >= 0f) { return; }
            float trim = Mathf.Min(excess, -applied);
            applied += trim;
            excess -= trim;
        }

        /// <summary>§497: the line the justice dials' cost lands on - Justice, else PublicServices (the one lookup the pressure and the index share).</summary>
        private static SpendingLine JusticeCostLine(Country country) => FindSpendingLine(country, SpendingCategory.Justice) ?? FindSpendingLine(country, SpendingCategory.PublicServices);

        /// <summary>§497: the line border enforcement's cost lands on - HomelandSecurity, else Migration, else PublicServices.</summary>
        private static SpendingLine BorderCostLine(Country country) => FindSpendingLine(country, SpendingCategory.HomelandSecurity) ?? FindSpendingLine(country, SpendingCategory.Migration) ?? FindSpendingLine(country, SpendingCategory.PublicServices);

        /// <summary>§497: the line the sector dials' support cost lands on - SectorCouplings.SupportLine, which the Sectors page shares (SC-1, ruled 2026-09-15: Business-and-industry, the USA's Commerce).</summary>
        private static SpendingLine SectorCostLine(Country country) => SectorCouplings.SupportLine(country);

        /// <summary>EN-7a: the line the Energy sector's subsidy lands on - the book's energy line, or none (Germany).</summary>
        private static SpendingLine EnergyCostLine(Country country) => SectorCouplings.EnergyLine(country);

        /// <summary>First SpendingLine matching category, or null if the country has none - a plain linear search, matching this file's existing no-LINQ style.</summary>
        private static SpendingLine FindSpendingLine(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.Category == category)
                {
                    return line;
                }
            }

            return null;
        }


        /// <summary>
        /// THE COUPLINGS PASS (build-order item 2, terminal rulings 2026-08-26, "line-resident,
        /// feeds G"): enforcement costs money, and the money lands on REAL spending lines - never
        /// an abstract Budget delta, which would invent money outside the line structure the
        /// recalibration just made honest. Two cost targets, both NEUTRAL-ANCHORED (zero at dial
        /// 50 / prison rate at baseline, because the status-quo enforcement apparatus is already
        /// inside the recalibrated seed totals): the JUSTICE target = the police + judicial dial
        /// gaps at their ruled shares of GDP, PLUS the incarceration variable cost - the prison
        /// stock's gap above its own baseline at the ruled cost-per-inmate (the honest chain:
        /// sentencing -&gt; prison stock, on its ~4-year half-life -&gt; budget); the BORDER target =
        /// the border dial gap at its share. Line routing, perimeter-consistent per country:
        /// USA Justice + HomelandSecurity (CBP/ICE's real federal line); Sweden Justice (UO4
        /// rattsvasendet) + Migration (UO8); the four statute books that replaced the generic seeds carry
        /// Justice and HomelandSecurity too (JusticeCostLine, BorderCostLine; PublicServices only the fallback). All lines are Discretionary, so the
        /// cost flows into the national-accounts G term through the existing line sum - police
        /// and prisons are government PURCHASES, and this is the ruling's point.
        ///
        /// STATELESS TARGET ON A STATEFUL LINE: the Applied* trackers on Country (four since EN-7a - justice, border,
        /// sector support, energy) record the dollars applied, and each boundary applies only the DIFFERENCE - so the
        /// dial cost composes with the line's other writers (the index, the percent changes, the
        /// nominal targets) instead of overwriting them. SC-1 (ruled 2026-09-15, §503): the seed band
        /// bounds the line's OWN path, never the cost - the line is the clamped own path plus the applied
        /// cost (<see cref="ComposeLine"/>), so the USA's small federal Justice line carries an extreme
        /// SWEEPING-law stack's whole cost; the one bound a cost meets is zero. Amount only, never SeedAmount, per the pressure methods'
        /// own reconciliation rule. Runs inside ResolveSpendingForTurn (boundary-resident: dials
        /// change only at boundaries via law composition, and the period plan idiom carries the
        /// cost through the daily accrual automatically). Old saves carry Applied* = 0 and
        /// self-correct at their first boundary (at neutral dials the target IS zero).
        /// </summary>
        private void ApplyEnforcementCostPressure(Country country)
        {
            float gdp = country.State.NominalGdp;   // §497: the lines are in current prices (P5-B6); a target on real GDP crossed the two books
            float justiceTarget = gdp / 100f * (
                    CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint * (country.PoliceFundingLevel - CrimeJusticeCouplings.NeutralDialLevel)
                  + CrimeJusticeCouplings.JudicialFundingBudgetCostPercentOfGdpPerPoint * (country.JudicialFundingLevel - CrimeJusticeCouplings.NeutralDialLevel))
                + gdp * CrimeJusticeCouplings.IncarcerationCostGdpPerCapitaPerInmate
                      * (country.State.PrisonPopulationRate - country.BaselinePrisonPopulationRate) / 100000f;
            float borderTarget = gdp / 100f * CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint
                * (country.BorderEnforcementLevel - CrimeJusticeCouplings.NeutralDialLevel);

            SpendingLine justiceLine = JusticeCostLine(country);
            if (justiceLine != null) { ApplyCostOnLine(country, justiceLine, justiceTarget, ref country.AppliedJusticeEnforcementCost); }

            SpendingLine borderLine = BorderCostLine(country);
            if (borderLine != null) { ApplyCostOnLine(country, borderLine, borderTarget, ref country.AppliedBorderEnforcementCost); }
        }

        /// <summary>
        /// The applied-difference idiom's one step, shared by the four dial costs. SC-1 (ruled 2026-09-15, §503): the cost is a figure the player set, so the
        /// seed band does not bound it - the line's own path is recovered (its amount less the costs it carries), the tracker takes the target, and the line
        /// is written as the clamped own path plus its costs (<see cref="ComposeLine"/>, where a cut below neutral meets the one bound a cost has: zero).
        /// History: until EN-7a the tracker recorded the REQUESTED target inside the clamp, which left a line off its path after a round trip; EN-7a
        /// (§498) recorded the move the line took; SC-1 takes the cost out of the clamp, so the target is what lands. Zero asked (every dial at neutral):
        /// the line's own amount, unchanged.
        /// </summary>
        private static void ApplyCostOnLine(Country country, SpendingLine line, float target, ref float applied)
        {
            float own = line.Amount - AppliedDialCostOn(country, line);
            applied = target;
            ComposeLine(country, line, own);
        }

        /// <summary>
        /// P4-B3 (2026-09-04): the sector dials' cost, in the enforcement cost's own shape - a stateless target
        /// (<see cref="SectorCouplings.SupportCostTarget"/>: every sector's subsidy, tax credits and research grants above
        /// the neutral dial, at their standing levels) composed with the stateful support line - each statute budget's
        /// Business-and-industry line, the USA's Commerce (SC-1, ruled 2026-09-15) - through <see cref="Country.AppliedSectorSupportCost"/>, so each boundary applies only the difference. Zero at
        /// neutral dials, so the seed's trajectory does not move; the target moves only when a sector bill passes.
        /// </summary>
        private void ApplySectorSupportCostPressure(Country country)
        {
            float target = SectorCouplings.SupportCostTarget(country);
            SpendingLine line = SectorCostLine(country);
            if (line != null) { ApplyCostOnLine(country, line, target, ref country.AppliedSectorSupportCost); }
        }

        /// <summary>
        /// EN-7a (2026-09-14): the Energy sector's SUBSIDY as retail intervention's money side - its cost (`SectorCouplings.EnergySupportCostTarget`)
        /// composed onto the book's energy line through <see cref="Country.AppliedEnergySupportCost"/>, the idiom the other dial costs use (§497's form:
        /// the tracker rides the line's index). EN-4's levy rule reads this cost IN FULL and the line's own move at its sourced support share (`EnergyLedger.Compute`; FT-10 · P-A′, §553), so the
        /// subsidy takes the policy levy down one for one (the player's path rides prices alone, so FT-10 · P-A's share-of-path form is the same arithmetic here) and past the whole levy is taxpayers' support with no retail effect. Zero at the neutral 50;
        /// no line (Germany), nothing here - the subsidy's cost stays in the other sectors' target (`SectorCouplings.SupportCostTarget`).
        /// </summary>
        private void ApplyEnergySupportCostPressure(Country country)
        {
            SpendingLine line = EnergyCostLine(country);
            if (line == null) { return; }
            ApplyCostOnLine(country, line, SectorCouplings.EnergySupportCostTarget(country), ref country.AppliedEnergySupportCost);
        }


        /// <summary>
        /// Applies this turn's requested PERCENTAGE change (PolicyDecision.SpendingLineChanges) to
        /// every SpendingLine - both Mandatory and Discretionary are adjustable, but the requested
        /// percentage is clamped to a narrower range for Mandatory (MandatoryPercentChangeRange) than
        /// Discretionary (DiscretionaryPercentChangeRange) before being applied to that line's OWN
        /// current Amount (not a flat dollar figure - a +15% slider on an $850B line and a +15%
        /// slider on a $1B line move by proportionally different dollar amounts). The result is then
        /// clamped to [MinSpendingLineAmountRatio, MaxSpendingLineAmountRatio] of the line's own fixed
        /// SeedAmount (replacing the old floor-at-0 - 0.2x SeedAmount is always a stricter, higher
        /// floor than 0) - this is what actually stops the sustained-compounding exploit (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md): holding a slider at its max every turn
        /// for 100 turns previously produced a ~geometric blowup, and now flattens out once the line
        /// hits 3x its starting size, however many turns that took. Returns the actual dollar change
        /// observed per category (post-clamp) and the total across Mandatory lines, so callers can
        /// feed the real applied effect into MacroSystem's category-effect/approval formulas rather
        /// than the raw requested percentage.
        /// </summary>
        private SpendingLineChangeResult ApplySpendingLineChanges(Country country, PolicyDecision decision)
        {
            var result = new SpendingLineChangeResult();
            foreach (SpendingLine line in country.SpendingLines)
            {
                float amountBefore = line.Amount;

                if (decision.SpendingLineChanges.TryGetValue(line.Category, out float requestedPercent) && requestedPercent != 0f)
                {
                    float maxRange = line.IsMandatory ? MandatoryPercentChangeRange : DiscretionaryPercentChangeRange;
                    float clampedPercent = Mathf.Clamp(requestedPercent, -maxRange, maxRange);
                    ComposeLine(country, line, (line.Amount - AppliedDialCostOn(country, line)) * (1f + clampedPercent / 100f));   // SC-1: the percent moves the line's own path; the dial cost it carries stays as set
                }
                // P5-B2: a line SET to a nominal amount takes it (clamped to the seed band), and a pin holds it there until unpinned. SC-1: the figure is the line's
                // total, so its own path is the figure less the dial cost the line carries, clamped to the band, the cost outside it.
                if (decision.SpendingNominalTargets.TryGetValue(line.Category, out float nominalTarget))
                {
                    ComposeLine(country, line, Mathf.Max(0f, nominalTarget) - AppliedDialCostOn(country, line));
                }
                if (decision.SpendingPinChanges.TryGetValue(line.Category, out bool pin))
                {
                    line.Pinned = pin;
                }

                float actualChange = line.Amount - amountBefore;
                result.ActualDollarChangeByCategory[line.Category] = actualChange;
                if (line.IsMandatory)
                {
                    result.MandatoryDollarChangeTotal += actualChange;
                }
            }

            return result;
        }

        private static float GetSpendingLineTotal(Country country, bool mandatory)
        {
            float total = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory == mandatory)
                {
                    total += line.Amount;
                }
            }
            return total;
        }

        private static float GetActualDollarChange(SpendingLineChangeResult changeResult, SpendingCategory category)
        {
            return changeResult.ActualDollarChangeByCategory.TryGetValue(category, out float value) ? value : 0f;
        }

        /// <summary>
        /// Maps this turn's ACTUAL dollar change per Discretionary category (not the raw requested
        /// percentage - PolicyDecision.SpendingLineChanges is now a percentage of that line's own
        /// Amount, so it must be converted through ApplySpendingLineChanges' real, clamped effect
        /// before it means anything in dollar terms) onto the four legacy category-spending-effect
        /// fields (Infrastructure -&gt; Transportation, Healthcare -&gt; HHSDiscretionary,
        /// Education -&gt; Education, Defense -&gt; Defense) so MacroSystem.ApplyCategorySpendingEffects/
        /// ApplyApprovalRating (unmodified) still read a meaningful this-turn dollar delta for each of
        /// their four existing effects, without MacroSystem needing to know about SpendingCategory at
        /// all. Medicaid (Mandatory) is deliberately NOT folded into the Healthcare bucket here anymore -
        /// now that Mandatory categories are player-adjustable, doing so would double-count Medicaid's
        /// effect through both this legacy bucket AND the new, distinctly-weighted Mandatory approval
        /// term (see MacroSystem.MandatorySpendingApprovalMultiplier) that now covers it uniformly with
        /// every other Mandatory category. Four more categories (Justice/HomelandSecurity/Energy/
        /// Housing) were given their own effects in Phase 2 (see CLAUDE.md's "Detailed Spending
        /// Portfolio Phase 2") and are mapped here the same way - every other Discretionary category
        /// still gets zero effect, since Phase 2 only extended 4 of the remaining 15 effect-less
        /// categories, not an exhaustive list. Country-selection task, Part 2: InfrastructureSpendingChange
        /// now also checks SpendingCategory.InfrastructureAndDevelopment alongside Transportation - the
        /// generic category Sweden/Germany/France/Italy/Poland's decomposition uses in place of USA's
        /// own Transportation line (see WorldFactory.SeedGenericSpendingLines). Safe to simply ADD both
        /// GetActualDollarChange calls rather than branch on which one applies: a given country's
        /// SpendingLines can only ever contain one of the two (USA has Transportation, the other five
        /// have InfrastructureAndDevelopment, never both), so the OTHER call always resolves to 0 via
        /// GetActualDollarChange's own "not present in the dictionary" fallback. DefenseSpendingChange
        /// needed NO change at all - SpendingCategory.Defense is reused directly (not a new category)
        /// by the same five countries' portfolios, so this already-existing line picks it up
        /// automatically.
        /// </summary>
        private static PolicyDecision BuildEffectiveDecisionForDetailedSpending(PolicyDecision decision, SpendingLineChangeResult changeResult)
        {
            return new PolicyDecision
            {
                TaxRateOverrides = decision.TaxRateOverrides,
                InterestRateChange = decision.InterestRateChange,
                TariffRateChange = decision.TariffRateChange,
                InfrastructureSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Transportation)
                    + GetActualDollarChange(changeResult, SpendingCategory.InfrastructureAndDevelopment),
                // Playtest-2 item 4: Sweden's UO9 line joins the healthcare effect exactly as the
                // generic InfrastructureAndDevelopment joined Transportation's - one country's
                // portfolio has HHSDiscretionary, the other HealthcareAndSocialCare, never both, so
                // the other term resolves to 0 via GetActualDollarChange's own fallback. Zero at
                // seed (changes only), so the byte-identity bar is untouched.
                HealthcareSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HHSDiscretionary)
                    + GetActualDollarChange(changeResult, SpendingCategory.HealthcareAndSocialCare),
                EducationSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Education),
                DefenseSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Defense),
                JusticeSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Justice),
                HomelandSecuritySpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HomelandSecurity),
                EnergySpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Energy),
                HousingSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Housing)
            };
        }

        /// <summary>
        /// Automatic stabilizer: unemployment benefit spending that scales with the unemployment
        /// rate with no player input, via the country's own BenefitRatePerUnemployed. Uses this
        /// turn's starting (prior-turn) Unemployment, matching how GetBaselineGovernmentSpending
        /// uses prior GDP - the value known at the start of the turn, before this turn's updates run.
        /// </summary>
        private float GetUnemploymentBenefitCost(Country country)
        {
            EconomyState state = country.State;
            return country.BenefitRatePerUnemployed * state.Unemployment / 100f * state.NominalGdp;   // P5-B6: a nominal flow
        }

        /// <summary>Sovereign risk premium, percentage points: MF q32's convex spread (Pamies, Carnot and Patarau 2021, Table 1 regression (2)) - the quadratic's
        /// rise above its minimum, b2 (d - d*)^2 for a debt ratio d above d* = -b1 / (2 b2), and zero below. UNCAPPED: the cap stopped interest running quadratic
        /// in debt; the loss of market access (`SovereignCrisis`, at CCC) is what stops the run now.</summary>
        private float GetDebtRiskPremium(EconomyState state)
        {
            float excessDebtToGdp = Mathf.Max(0f, state.DebtToGdpRatio - SpreadFloorDebtToGdpPercent);
            return SpreadDebtSquaredCoefficient * excessDebtToGdp * excessDebtToGdp;
        }

        /// <summary>MF q32: the premium the convex spread charges at <paramref name="debtToGdpPercent"/> - the same expression, for the diagnostics.</summary>
        public static float ConvexSpreadAt(float debtToGdpPercent)
        {
            float excess = Mathf.Max(0f, debtToGdpPercent - SpreadFloorDebtToGdpPercent);
            return SpreadDebtSquaredCoefficient * excess * excess;
        }

        /// <summary>
        /// F1 (mechanism-report finding, ruling R5; built as the close-out's Phase 2): the ONE
        /// path an interrupt-layer BudgetImpact takes to the books. Before this method existed,
        /// CabinetSystem.ApplyDecisionOption and ForeignPolicySystem.ApplyMeetingOption wrote
        /// state.Budget ONLY - the cumulative display accumulator - and the debt stock, which
        /// moves solely by budgetBalance in ApplyRevenueAndSpending, never saw them: "Bank it
        /// against the debt: +200" had never touched the debt path. (EventSystem was named in
        /// F1's first statement and is corrected here: EconomicEvent carries no budget field -
        /// the writers were exactly two.) ⚠ Corrected again, pass 5 (2026-08-26): there was a
        /// THIRD - ApplySwfDrawdownBillEffects wrote the accumulator alone - closed onto this same
        /// path by pass 5's retirement sweep. State.Budget's writers are now budgetBalance, this
        /// helper's callers (cabinet, foreign policy, the drawdown), and nothing else.
        ///
        /// THE ROUTING CLAIM, derived before wiring: every authored interrupt impact is a
        /// ONE-TIME SETTLEMENT (a windfall banked, a package funded, an evacuation chartered) -
        /// none is a recurring cost - so the honest entry is STOCK-SIDE: debt falls by a positive
        /// impact and rises by a negative one, clamped exactly as the main stock update clamps,
        /// with the SAME entry recorded in the Budget accumulator so it remains a TRUE READING of
        /// the one real path rather than a parallel ledger (two books for one quantity was the
        /// defect). ⚠ THE BOUNDARY, for future authors: a RECURRING cost is the budget process's
        /// channel (spending lines, welfare programs) and must never be expressed as a repeated
        /// interrupt impact - if a decision wants an ongoing program, it routes through Part B.
        /// </summary>
        public static void ApplyOneTimeBudgetImpact(Country country, float amount)
        {
            EconomyState state = country.State;
            state.Budget += amount;
            StoreDebt(country, state.GovernmentDebt - amount);   // MF q33: no ceiling - the runaway guards alone, and what they absorb booked
        }

        /// <summary>MF q33 ("no money vanishes"): THE ONE STORE of the debt stock's new value - inside the two runaway guards (the net-creditor one and, since
        /// the 300 % ceiling retired, its positive twin), which nothing should ever reach. Where a guard binds, what it cut is booked on the country
        /// (`SovereignCrisisState.GuardAbsorbedBillions`) and logged as the bug report it is - never silently swallowed.</summary>
        private static void StoreDebt(Country country, float unguarded)
        {
            EconomyState state = country.State;
            float upper = DebtRunawayGuardPercent / 100f * state.NominalGdp;   // P5-B6: bounds on nominal GDP
            float lower = -NetCreditorRunawayGuardPercent / 100f * state.NominalGdp;
            float stored = Mathf.Clamp(unguarded, lower, upper);
            state.GovernmentDebt = stored;
            if (stored != unguarded)
            {
                if (country.Crisis == null) { country.Crisis = new SovereignCrisisState(); }
                country.Crisis.GuardAbsorbedBillions += Mathf.Abs(unguarded - stored);
                Debug.LogError($"MONEY: {country.Id} - a debt runaway guard bound: {unguarded:0.###} stored as {stored:0.###} (a bug report: the guard is set where nothing should reach it)");
            }
        }

        /// <summary>MF q33: a debt stock at either runaway guard - the debt ledger's "clamp bound" day.</summary>
        private static bool DebtAtGuard(EconomyState state, float debt) =>
            debt >= DebtRunawayGuardPercent / 100f * state.NominalGdp || debt <= -NetCreditorRunawayGuardPercent / 100f * state.NominalGdp;

        /// <summary>
        /// THE MATURITY RATE-LAG (ruling R4): the rate NEW ISSUANCE prices at today - base rate
        /// (the reserve-currency override where set, else the zone's spot rate) plus the risk
        /// premium scaled by sensitivity. **The premium reprices at ISSUANCE ONLY** - it is a
        /// market price set at auction, so existing bondholders keep their coupons and today's
        /// risk price reaches the stock only at rollover speed. That is the derivation's answer
        /// to "what does the premium reprice": the claim it makes about the world is that a
        /// sovereign's borrowing cost responds to its debt ratio with a lag of years, not days -
        /// and it is exactly the channel the erosion pass measured as the residual (instant
        /// repricing at premium-loaded zone rates outrunning π).
        ///
        /// THE OVERRIDE'S FATE, derived as the build directive required: the general lag with the
        /// USA's real maturity does NOT reproduce the frozen 3.3% - the model's own Fed path sits
        /// at spot 0-6% for long stretches, so a lag toward SPOT would drift the USA's rate away
        /// from the validated blended figure. What the override captures that a spot-lag doesn't
        /// is reserve-currency ISSUANCE-YIELD ANCHORING (Treasury auctions clear near the blended
        /// rate regardless of the model Fed's excursions - the global-reserve-asset demand the
        /// original treatment reasoned from). So the override RETIRES INTO the general mechanism
        /// as the USA's issuance-rate claim rather than being deleted or duplicated: it feeds the
        /// lag TARGET here, the lag applies uniformly to all six, and the USA's effective rate
        /// stays anchored (~3.3 + its near-zero premium) by construction - the boundary named,
        /// both mechanisms kept, one line each.
        /// </summary>
        private float GetDebtIssuanceRate(Country country)
        {
            float baseRate = country.BaseDebtInterestRateOverride >= 0f
                ? country.BaseDebtInterestRateOverride
                : country.CurrencyZone.InterestRate;
            if (SovereignCrisis.OfficialLending(country)) { return baseRate; }   // MF q33 (K142, K144): a programme's loans carry no market premium
            return baseRate + GetDebtRiskPremium(country.State) * country.RiskPremiumSensitivity;
        }

        /// <summary>
        /// Advances the effective (blended) rate toward the current issuance rate at
        /// 1/AverageDebtMaturityYears per year - a stock with average maturity M rolls ~1/M of
        /// itself over each year, and only that share reprices. SHAPE (the taxonomy): a reversion
        /// whose speed is an annual fraction, so the daily form is the standard
        /// 1-(1-s)^fraction slice - PerDayReversion's exact composition at constant target; the
        /// target moves with the ratio's premium, which is the Phase-3 within-period feedback
        /// class, budgeted as such in the equivalence check's enumeration. Sentinel init on first
        /// advance: a pre-mechanism save starts AT the issuance rate - exactly what the old code
        /// charged. A fresh world is seeded at the national accounts' implied rate instead (K252,
        /// WorldFactory), so its stock converges toward issuance from what it paid. Called once per
        /// daily accrual and once per turn-form validation step; never from preview.
        /// </summary>
        private void AdvanceEffectiveDebtRate(Country country, float periodFraction)
        {
            float issuanceRate = GetDebtIssuanceRate(country);
            if (country.EffectiveDebtInterestRate < 0f)
            {
                country.EffectiveDebtInterestRate = issuanceRate;
                return;
            }

            float rolloverPerYear = 1f / Mathf.Max(1f, country.AverageDebtMaturityYears);
            float slice = 1f - Mathf.Pow(1f - rolloverPerYear, periodFraction);
            country.EffectiveDebtInterestRate += slice * (issuanceRate - country.EffectiveDebtInterestRate);
        }

        private float GetInterestOnDebt(Country country)
        {
            EconomyState state = country.State;
            // A NET CREDITOR PAYS NOTHING AND EARNS NOTHING (2026-08-02, Elias's ruling, made when the
            // zero floor was removed and negative debt became representable).
            //
            // Without this guard the arithmetic would silently invert: negative debt times a positive
            // rate is negative interest, which flows through ApplyRevenueAndSpending as a REDUCTION in
            // total spending - free money, growing with the size of the surplus, compounding into
            // exactly the runaway the floor was masking. That is a worse defect than the one being
            // fixed, and it would look like a well-run economy rather than like a bug.
            //
            // Zero rather than an interest income is the deliberately conservative half of the ruling.
            // Interest earned on net assets is a real thing a net creditor gets, and modelling it is a
            // SEPARATE decision Elias has explicitly deferred - the SWF already models the return on
            // government assets, so paying a second return on the same position here would double-count
            // it. Scoping this to "no free money" keeps the debt fix a debt fix.
            if (state.GovernmentDebt <= 0f)
            {
                return 0f;
            }

            // THE MATURITY RATE-LAG (ruling R4): charge the BLENDED rate the stock actually pays,
            // not today's issuance price - the sentinel fallback is READ-ONLY (the R4-3 pattern:
            // preview must never mutate), returning the issuance rate itself, which is bit-for-bit
            // what this method charged before the lag existed.
            float effectiveRate = country.EffectiveDebtInterestRate >= 0f
                ? country.EffectiveDebtInterestRate
                : GetDebtIssuanceRate(country);
            return state.GovernmentDebt * (effectiveRate / 100f);
        }

        /// <summary>
        /// This country's in-flight fiscal period, seeded on first use.
        ///
        /// **The seed is not a formality.** Day 1 of a new game arrives 121 days BEFORE the first
        /// AdvanceTurn, so no plan has been resolved yet and without one every country would spend
        /// nothing at all for its opening third of a year. It is derived directly from the portfolio the
        /// country was seeded with, and deliberately WITHOUT calling ResolveSpendingForTurn: that method
        /// is not idempotent - it applies a period of spending growth and demographic pressure - so
        /// calling it here would charge the world an extra turn of both before the game had advanced one.
        ///
        /// PlannedSwfReturn seeds to the fund's DETERMINISTIC average estimate rather than a random draw,
        /// so no RNG is consumed before the first budget resolution. A draw here would shift the
        /// SovereignWealthFund stream by one per country and invalidate every recorded baseline on day 1,
        /// which is a large price for the opening period's return being drawn rather than expected.
        /// </summary>
        private FiscalPeriod GetOrSeedFiscalPeriod(Country country)
        {
            if (_fiscalPeriods.TryGetValue(country.Id, out FiscalPeriod existing))
            {
                return existing;
            }

            bool hasDetailedPortfolio = country.SpendingLines.Count > 0;
            float governmentSpending = hasDetailedPortfolio
                ? GetSpendingLineTotal(country, mandatory: false)
                : GetBaselineGovernmentSpending(country);

            var seeded = new FiscalPeriod
            {
                PlannedGovernmentSpending = governmentSpending,
                PlannedMandatorySpending = hasDetailedPortfolio ? GetSpendingLineTotal(country, mandatory: true) : 0f,
                // The opening period has no player decision behind it, so all of G is baseline and none
                // of it is this period's discretionary CHANGE - the same split ResolveSpendingForTurn
                // reports for a turn in which the player changed nothing.
                PlannedBaselineGovernmentSpending = governmentSpending,
                PlannedDiscretionarySpending = 0f,
                PlannedSwfReturn = country.SovereignWealthFund != null
                    ? SovereignWealthFundSystem.GetAverageReturnEstimate(country.SovereignWealthFund)
                    : 0f,
                SwfAssetsAtPeriodOpen = country.SovereignWealthFund != null ? country.SovereignWealthFund.TotalAssets : 0f,   // AF-35: what the seed's return is estimated on
                // Pass 5: the opening period's tariff flow, from the same pure function the boundary
                // reports - so turn 1 accrues the seed rates' take rather than a period of nothing.
                PlannedTariffRevenueReal = TradeSystem.ComputeTariffRevenue(country, _world),
                PlannedTariffRevenue = TradeSystem.ComputeTariffRevenue(country, _world) * country.State.PriceLevel,   // P5-B6: nominal
                // Pass 6: no previous boundary, so no tariff change to pass through; EN-5: nor an electricity year written yet.
                PlannedTariffPassThroughPp = 0f,
                PlannedEnergyPassThroughPp = 0f,
                PlannedElectricityTaxRevenue = 0f,   // EN-7b: every statute at its base at the seed
                AppliedPriceLevelTermsPp = 0f,
                PlannedFiscalReactionMultiplier = GetFiscalReactionMultiplier(country),
                GdpAtPeriodOpen = country.State.GDP,
                UnemploymentAtPeriodOpen = country.State.Unemployment,
                PotentialGdpAtPeriodOpen = country.State.PotentialGDP,
                WageGrowthGapAtPeriodOpen = MacroSystem.RealWageGrowthGapPerTurnPercent(country,
                    MacroSystem.ProductivityCycleGrowthPerTurnPercent(country, country.State.Unemployment))
            };

            _fiscalPeriods[country.Id] = seeded;
            return seeded;
        }

        /// <summary>MF q33 (ruled 2026-10-18 q33: "The fund ceiling refuses a contribution at booking"): the part of a positive <paramref name="contribution"/>
        /// that would carry <paramref name="fund"/> past <paramref name="ceiling"/> - refused, so neither the fund nor the budget's spending books it and the
        /// money stays in the budget. Zero for a draw (a negative contribution) and wherever the fund has the room.</summary>
        private static float SwfContributionRefused(SovereignWealthFund fund, float contribution, float ceiling)
        {
            if (fund == null || contribution <= 0f) { return 0f; }
            return Mathf.Max(0f, contribution - Mathf.Max(0f, ceiling - fund.TotalAssets));
        }

        /// <summary>
        /// AF-35 (Elias's ruling q35, 2026-10-18: "a fund earns on what it holds"; GA-2-10, §814's review): the part of a day's slice of the period's return
        /// the fund does NOT earn. The return is drawn once on the period-open assets (<see cref="FiscalPeriod.SwfAssetsAtPeriodOpen"/>) and the day earns it
        /// on the share of those the fund still holds as PRINCIPAL - min(principal, open) / open. The principal is the period-open assets plus the
        /// period's contributions, less its draws and drawdowns, WITHOUT the period's own returns (the balance after the day's contribution less what the
        /// period has earned so far, <see cref="FiscalPeriod.AccruedSwfReturns"/>) - the integrator's reading of q35 after the review (M2, DECLARED and
        /// reversible): measured on the balance with its returns, a loss year softened itself (a drawn loss realised short of its draw) while a gain never
        /// compounded above the flat slice, a bias upward. On the principal a fund that keeps it earns the drawn return exactly, gain or loss. Assets paid in
        /// during the period earn from the next boundary's draw (the share is capped at one); a fund drawn below its opening earns the share it keeps. A
        /// fund emptied within a period earns nothing from the day it is empty (the balance itself, <paramref name="held"/>, at or below zero), where it used
        /// to earn the flat slice on assets it no longer had (and since §814 the next day's capped draw paid that slice to the budget). Zero while the
        /// principal is whole; the whole slice where the fund holds nothing; an older save's zero for the open assets reads as whole (the flat slice).
        /// Where the zero floor binds, the period accrues what landed rather than the return intended (the second review's N2), so the
        /// principal stays the identity above; a dissolution zeroes the period's return figures (N3), so a fund created after it earns from the next boundary.
        /// ⚠ STATED (the third review; MF q33): the ceiling cuts no return - what stands above it after the return (GDP fell under a fund already near it, or
        /// the return carried it past) leaves as a draw, paid into the budget (<see cref="SwfContributionRefused"/> beside it); a dissolution's zeroing drops that period's earned returns from the year's reported `SwfReturns`
        /// (P3-3) - they reached the debt inside the assets the dissolution credited; and an emergency drawdown scales the period's accrued returns by the
        /// share of the holdings it leaves (P3-1, `ApplySwfDrawdownBillEffects`), so the principal keeps its ratio to the holdings and the drawn share of
        /// the year's returns leaves the year's `SwfReturns` with the assets.
        /// ⚠ KNOWN LIMITS, stated (the second review's N4, N5): the turn-shaped validation step (<see cref="ApplyPeriodFiscalStepForValidation"/>) and the
        /// preview (<see cref="PreviewTurn"/>) still credit the flat return - the preview estimates a period ahead, where the principal is whole by
        /// construction, and the validation step is the turn form the daily one is held against; and the structural draw, taken on the assets after the
        /// day's return, comes out of the principal whole, though part of it is drawn on returns - a small bias toward earning less.
        /// </summary>
        private static float SwfReturnNotEarned(float slice, float held, float principal, float openAssets)
        {
            if (held <= 0f || principal <= 0f && openAssets > 0f) { return slice; }
            if (openAssets <= 0f || principal >= openAssets) { return 0f; }
            return slice * (1f - principal / openAssets);
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3: one day's money. This is the fiscal engine's daily step - the half of
        /// Phase 3 the part-1 commit scoped and deliberately did not start.
        ///
        /// Every flow is one period's figure times FiscalFlowPerDayFraction (shape #1, linear). Tax
        /// revenue, benefits, welfare, interest and the SWF contribution are all RECOMPUTED from live
        /// country state each day rather than frozen at plan time. Today that only matters for interest -
        /// GDP, unemployment and every policy rate are still turn-level until Phase 5, so nothing else
        /// can move between two days - but it is the shape the rest of the migration needs, and interest
        /// genuinely should be charged on the debt the country holds today rather than the debt it held
        /// four months ago. That intra-period interest accrual is the ONE deliberate difference from the
        /// turn form, and the aggregation-equivalence check exists to keep it inside tolerance.
        ///
        /// The fiscal reaction multiplier is the exception and is held fixed for the period; recomputing
        /// it daily failed the bar by a wide margin, and FiscalPeriod records both the measurement and
        /// why freezing it is the better model rather than merely the passing one.
        ///
        /// The SWF sequence is the turn form's, unchanged in ORDER: contribute, earn, clamp, then draw -
        /// the structural draw comes out of post-return assets, not pre-. Only the size of each step
        /// changed.
        ///
        /// Runs for every country including NPCs, exactly as the turn form did.
        /// </summary>
        private void AccrueDailyFiscalFlows(Country country)
        {
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);
            EconomyState state = country.State;

            // THE MATURITY RATE-LAG (ruling R4): the blended rate rolls toward today's issuance
            // price before interest is charged - repricing is continuous, so advance-then-charge;
            // the one-day ordering difference is inside Phase 3's stated drift class either way.
            AdvanceEffectiveDebtRate(country, FiscalFlowPerDayFraction);

            // PS-6 US-22b (I13 (1)): a lapse stops the day's non-excepted outlays and the first funded day pays them in full (UsLapse.OutlayToday) - 0 on
            // every other day and in every other country, and summed in one expression (§501: x + 0 is exact, so the store is the one it always was)
            float lapseOutlay = UsLapse.OutlayToday(country, CurrentDate, period.PlannedGovernmentSpending * FiscalFlowPerDayFraction);
            float governmentSpending = period.PlannedGovernmentSpending * FiscalFlowPerDayFraction + lapseOutlay;
            float mandatorySpending = period.PlannedMandatorySpending * FiscalFlowPerDayFraction;
            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country) * FiscalFlowPerDayFraction;
            float interestOnDebt = GetInterestOnDebt(country) * FiscalFlowPerDayFraction;
            float welfareCost = GetTotalWelfareCost(country) * FiscalFlowPerDayFraction;
            float swfContribution = GetSwfContribution(country) * FiscalFlowPerDayFraction;
            // Pass 5: the planned tariff flow, sliced exactly like mandatory spending - a fixed period
            // figure distributed linearly, so the daily form sums to the turn form by construction.
            float tariffRevenue = period.PlannedTariffRevenue * FiscalFlowPerDayFraction;
            // EN-7b: the electricity tax's planned flow, sliced the same way
            float electricityTaxRevenue = period.PlannedElectricityTaxRevenue * FiscalFlowPerDayFraction;

            float swfReturns = 0f;
            float swfDraw = 0f;
            // GA-2-1 (2026-10-14): the part of a negative standing contribution the fund does not hold. The budget used to book the whole draw while
            // the fund floored at 0 - money from nothing once it was empty. Now a draw delivers what is there, to the fund and the budget alike (the
            // drawdown bill's own rule). SUMMED IN at each use, never written back into swfContribution: 0 for every fund that can pay, and §501's
            // lesson is that a second write to a local moves the no-policy trajectory by a float's last digit.
            float swfUnpaid = 0f;
            // AF-35 (Elias's ruling q35, 2026-10-18: "a fund earns on what it holds"; GA-2-10): the part of the day's slice of the period's return the fund
            // does not earn, because its principal (the balance less the period's own returns so far) no longer covers the assets the return was drawn on.
            // SUMMED IN at each use like swfUnpaid, never written back into swfReturns: 0 for every fund whose principal is whole, so such a fund reads the
            // old expressions to the bit.
            float swfReturnUnearned = 0f;
            bool swfReturnBound = false;   // N2: the floor bound the day's return
            float swfReturnLanded = 0f;
            float swfRefused = 0f;   // MF q33: the part of the day's contribution the ceiling refused at booking - it stays in the budget
            float swfOverflow = 0f;   // MF q33: what stood above the ceiling after the return, paid out into the budget with the draw
            if (country.SovereignWealthFund != null)
            {
                // MF q33 ("the fund ceiling refuses a contribution at booking"): the ceiling is read FIRST, and the part of a contribution that would carry
                // the fund past it is refused - booked neither to the fund nor as spending, so the money stays in the budget
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.NominalGdp;
                swfUnpaid = Mathf.Max(0f, -(country.SovereignWealthFund.TotalAssets + swfContribution));
                swfRefused = SwfContributionRefused(country.SovereignWealthFund, swfContribution, maxSwfAssets);
                country.SovereignWealthFund.TotalAssets += swfContribution + swfUnpaid - swfRefused;

                swfReturns = period.PlannedSwfReturn * FiscalFlowPerDayFraction;
                swfReturnUnearned = SwfReturnNotEarned(swfReturns, country.SovereignWealthFund.TotalAssets, country.SovereignWealthFund.TotalAssets - period.AccruedSwfReturns, period.SwfAssetsAtPeriodOpen);
                float swfBeforeReturn = country.SovereignWealthFund.TotalAssets;
                country.SovereignWealthFund.TotalAssets = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets + swfReturns - swfReturnUnearned);

                // MF q33: what stands above the ceiling after the return (a return, or a fall in GDP, can carry the fund past it) is paid OUT into the budget with
                // the structural draw - where the clamp used to delete it
                swfOverflow = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets - maxSwfAssets);
                country.SovereignWealthFund.TotalAssets -= swfOverflow;
                // AF-35 (the second review's N2): where the zero floor bound, the period accrues the change that LANDED, not the return intended, so the
                // principal (TotalAssets - AccruedSwfReturns) stays the period-open assets plus contributions less draws - a loss the floor cut short is not
                // counted as principal kept, and no later contribution goes into a loss on assets the fund no longer had. Since MF q33 the ceiling cuts no
                // return: the return lands whole and the overflow leaves as a draw.
                bool swfFloored = country.SovereignWealthFund.TotalAssets == 0f && swfBeforeReturn > 0f && swfReturns - swfReturnUnearned < 0f;
                if (swfFloored)
                {
                    swfReturnBound = true;
                    swfReturnLanded = country.SovereignWealthFund.TotalAssets - swfBeforeReturn;
                }

                swfDraw = country.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction() * FiscalFlowPerDayFraction;
                swfDraw = Mathf.Clamp(swfDraw, 0f, country.SovereignWealthFund.TotalAssets);
                country.SovereignWealthFund.TotalAssets -= swfDraw;
                country.Crisis.SwfRefusedBillions += swfRefused;
                country.Crisis.SwfOverflowBillions += swfOverflow;
            }

            // Step 2's third section (2026-08-25): the debt ledger observes THIS write. The three
            // inputs the split needs beyond what the call already returns are read here, before
            // the write, from exactly the state the charge above read - the stock GetInterestOnDebt
            // charged, the issuance rate AdvanceEffectiveDebtRate just targeted, and the blended
            // rate it just advanced - so the recorded interest split is the model's own pair, not a
            // reconstruction. The ledger opens at the pre-write stock on a period's first day.
            float debtBeforeWrite = state.GovernmentDebt;
            float issuanceRateToday = GetDebtIssuanceRate(country);
            float effectiveRateToday = country.EffectiveDebtInterestRate >= 0f ? country.EffectiveDebtInterestRate : issuanceRateToday;
            float interestAtIssuanceToday = debtBeforeWrite > 0f ? debtBeforeWrite * (issuanceRateToday / 100f) * FiscalFlowPerDayFraction : 0f;
            DebtLedgerRecorder.EnsureAccruing(country, CurrentDate, debtBeforeWrite);

            float revenue = ApplyRevenueAndSpending(country, governmentSpending, mandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution + swfUnpaid - swfRefused, swfDraw + swfOverflow, tariffRevenue, electricityTaxRevenue, out float totalSpending, out float budgetBalance, FiscalFlowPerDayFraction, period.PlannedFiscalReactionMultiplier);

            period.AccruedRevenue += revenue;
            period.AccruedTariffRevenue += tariffRevenue;
            period.AccruedElectricityTaxRevenue += electricityTaxRevenue;   // EN-7b
            period.AccruedMandatorySpending += mandatorySpending;
            period.AccruedUnemploymentBenefitCost += unemploymentBenefitCost;
            period.AccruedInterestOnDebt += interestOnDebt;
            period.AccruedWelfareCost += welfareCost;
            period.AccruedSwfContribution += swfContribution + swfUnpaid - swfRefused;   // GA-2-1: what was booked (MF q33: less what the ceiling refused)
            if (swfReturnBound) { period.AccruedSwfReturns += swfReturnLanded; }   // N2: what landed where the floor bound
            else { period.AccruedSwfReturns += swfReturns - swfReturnUnearned; }   // AF-35: what the fund earned
            period.AccruedSwfDraw += swfDraw + swfOverflow;   // MF q33: what left the fund for the budget
            period.AccruedTotalSpending += totalSpending;
            period.AccruedBudgetBalance += budgetBalance;
            period.AccruedDebtLimitCut += _debtLimitCutToday;   // PS-6 US-24 (the review's M4): the day's cut as its own line - 0 on every day the limit does not bind
            period.AccruedCrisisCut += _crisisCutToday;   // MF q33 (the q32 review's M3): the closed market's cut as its own line - 0 on every financed day

            // The erosion factor is the ONE recomputation - the same expression as the stock
            // update's, on the same state - and the ledger's twin-drift detector is what keeps it
            // so. The clamp is detected by landing ON either bound, tested against the same two
            // limits the update clamps to (MF q33: the two runaway guards - the ceiling is retired).
            float erosionFactorApplied = 1f;   // P5-B6: no erosion - the book is nominal, and a nominal stock is not eroded by the prices it is written in
            float debtAfterWrite = state.GovernmentDebt;
            bool clampBoundToday = DebtAtGuard(state, debtAfterWrite);
            DebtLedgerRecorder.RecordDay(country, CurrentDate, debtBeforeWrite, debtAfterWrite, erosionFactorApplied,
                revenue, totalSpending, interestOnDebt, interestAtIssuanceToday, budgetBalance,
                period.PlannedFiscalReactionMultiplier, issuanceRateToday, effectiveRateToday, clampBoundToday, swfDraw + swfOverflow);   // K213: the draw sits outside the stance
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3 VALIDATION HOOK - the money resolution EXACTLY as AdvanceTurn applied
        /// it before this phase: one whole period's flows, in one step, against a caller-supplied plan.
        ///
        /// Kept rather than deleted because it IS the aggregation-equivalence bar - the already-validated
        /// turn-level answer that 121 daily accruals have to land within tolerance of. Nothing in the
        /// simulation loop calls it; its only caller is AggregationEquivalenceCheck.
        /// </summary>
        public void ApplyPeriodFiscalStepForValidation(Country country, float governmentSpending, float mandatorySpending, float swfPeriodReturn, float tariffRevenue)
        {
            EconomyState state = country.State;
            // R4: the turn form advances the rate lag one whole period, mirroring the daily path's
            // advance-then-charge order - the equivalence bar's two sides must share the shape.
            AdvanceEffectiveDebtRate(country, 1f);
            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float welfareCost = GetTotalWelfareCost(country);
            float swfContribution = GetSwfContribution(country);

            float swfDraw = 0f;
            float swfUnpaid = 0f;   // GA-2-1: as the day's
            float swfRefused = 0f;   // MF q33: as the day's
            float swfOverflow = 0f;   // MF q33: as the day's
            if (country.SovereignWealthFund != null)
            {
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.NominalGdp;
                swfUnpaid = Mathf.Max(0f, -(country.SovereignWealthFund.TotalAssets + swfContribution));
                swfRefused = SwfContributionRefused(country.SovereignWealthFund, swfContribution, maxSwfAssets);
                country.SovereignWealthFund.TotalAssets += swfContribution + swfUnpaid - swfRefused;
                country.SovereignWealthFund.TotalAssets = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets + swfPeriodReturn);
                swfOverflow = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets - maxSwfAssets);
                country.SovereignWealthFund.TotalAssets -= swfOverflow;

                swfDraw = country.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction();
                swfDraw = Mathf.Clamp(swfDraw, 0f, country.SovereignWealthFund.TotalAssets);
                country.SovereignWealthFund.TotalAssets -= swfDraw;
            }

            ApplyRevenueAndSpending(country, governmentSpending, mandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution + swfUnpaid - swfRefused, swfDraw + swfOverflow, tariffRevenue, 0f, out _, out _);   // EN-7b: the equivalence bar runs no law - no electricity-tax flow on either side
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3 VALIDATION HOOK - installs an explicit plan and runs exactly one day of
        /// the REAL production accrual (AccrueDailyFiscalFlows), so AggregationEquivalenceCheck can drive
        /// 121 of them against the turn-shaped step above with both paths spending a plan it chose.
        ///
        /// It delegates rather than reproducing the daily maths on purpose: a validation path that
        /// re-implements what it validates can pass while the shipped code is wrong.
        /// </summary>
        public void AccrueDayForValidation(Country country, float governmentSpending, float mandatorySpending, float swfPeriodReturn, float tariffRevenue)
        {
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);
            // AF-35 (the review's L3): the plan's period opens on its first day - nothing accrued yet - on the assets the fund holds then, as the boundary sets
            // them where it draws the return, so the daily form here earns on the principal the turn form's whole-period step starts from
            if (period.AccruedTotalSpending == 0f) { period.SwfAssetsAtPeriodOpen = country.SovereignWealthFund != null ? country.SovereignWealthFund.TotalAssets : 0f; }
            period.PlannedGovernmentSpending = governmentSpending;
            period.PlannedMandatorySpending = mandatorySpending;
            period.PlannedSwfReturn = swfPeriodReturn;
            period.PlannedTariffRevenueReal = tariffRevenue;
            period.PlannedTariffRevenue = tariffRevenue * country.State.PriceLevel;   // P5-B6: the take is computed on real trade; the book is nominal
            period.PlannedElectricityTaxRevenue = 0f;   // EN-7b: the same plan the turn side runs - no law, no flow
            AccrueDailyFiscalFlows(country);
        }

        /// <summary>
        /// Government revenue is GetTotalTaxRevenue's theoretical figure scaled down by the country's
        /// CollectionEfficiency (enforcement quality/informal economy/evasion - see Country's doc
        /// comment), nudged by the Finance/Treasury Cabinet minister's passive competence bias if one
        /// is appointed (Political Systems Overhaul Part A - see CabinetSystem.GetCompetenceBias,
        /// applied at point-of-use here rather than mutating the stored CollectionEfficiency field,
        /// since that field has no reversion mechanism of its own to correct a permanent drift - the
        /// same "may be safer to land somewhere more contained" reasoning the Master Roadmap's own
        /// Part A spec calls for), and then by GetFiscalReactionMultiplier (the automatic
        /// fiscal-tightening/-loosening response to this country's own debt-to-GDP gap - see that
        /// method and "Fiscal Reaction Function" in CLAUDE.md); this turn's budget balance is that
        /// actual revenue minus total spending (government spending, Mandatory SpendingLine total (0
        /// for a country without a detailed portfolio), unemployment benefits, interest on debt, and
        /// welfare program cost - see GetTotalWelfareCost - benefits, mandatory transfers, interest,
        /// and welfare are all transfers, not purchases, so they're deliberately excluded from
        /// MacroSystem's national accounts G term). A deficit adds to GovernmentDebt, a surplus
        /// reduces it, hard-clamped to a sane debt-to-GDP range. Returns the actual (post-efficiency,
        /// post-reaction) revenue so the caller can record it on this turn's FiscalTurnReport.
        ///
        /// PASS 5 (2026-08-26): <paramref name="tariffRevenue"/> is the tariff flow for the same slice
        /// the spending figures cover (the caller applies the period fraction, as for spending) - a
        /// RECURRING revenue that joins actual revenue here beside taxes and the fund draw, INSIDE the
        /// fiscal-reaction multiplier (the 2026-08-02 SWF ruling - "returns run INSIDE the fiscal
        /// reaction multiplier" - extended by analogy: a stance that loosens on a windfall should see
        /// every windfall; it is also what keeps the debt ledger's revenue/m split exact with no new
        /// term, and it is what bounds the tariff lever - a 50%-override windfall is partly given back
        /// as the stance loosens) and OUTSIDE CollectionEfficiency (customs are not the tax
        /// administration's collection). Derived, not inherited from F1: F1's interrupt impacts are
        /// one-time settlements and go stock-side; a flow that recurs every period is the budget
        /// process's channel, which F1's own boundary rule names. Revenue-neutral at seed by
        /// construction: each country's CollectionEfficiency gives back exactly its seed take
        /// (WorldFactory) so the recalibration's landed T1 primaries - the anchored quantity - are
        /// unchanged; whether the real tax targets already contained customs is recorded as unverified.
        ///
        /// CONTINUOUS TIME PHASE 3: every spending figure is passed IN, so the caller decides whether it
        /// is handing over a whole period's or one day's. Revenue is the exception - it is computed here,
        /// from the country's own portfolio - so <paramref name="revenuePeriodFraction"/> is what tells
        /// this method which of the two it is being asked for. It defaults to a whole period, which keeps
        /// PreviewTurn (the turn form's remaining caller) reading exactly as it did.
        ///
        /// <paramref name="fiscalReactionMultiplierOverride"/> is the -1 sentinel by default (this file's
        /// existing idiom for "unset" - see Country.BaseDebtInterestRateOverride), meaning "compute the
        /// multiplier from the debt ratio right now", which is what the turn form always did. The daily
        /// path passes the value its period opened with instead; see FiscalPeriod for why that one is
        /// held fixed while every other component is recomputed daily.
        /// </summary>
        private float ApplyRevenueAndSpending(Country country, float governmentSpending, float mandatorySpending, float unemploymentBenefitCost, float interestOnDebt, float welfareCost, float swfContribution, float swfReturns, float tariffRevenue, float electricityTaxRevenue, out float totalSpending, out float budgetBalance, float revenuePeriodFraction = 1f, float fiscalReactionMultiplierOverride = -1f)
        {
            EconomyState state = country.State;
            float theoreticalRevenue = GetTotalTaxRevenue(country) * revenuePeriodFraction;
            float fiscalReactionMultiplier = fiscalReactionMultiplierOverride >= 0f
                ? fiscalReactionMultiplierOverride
                : GetFiscalReactionMultiplier(country);
            float financeTreasuryCompetenceBias = CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.FinanceTreasury);
            // D-16 (a), 2026-09-04: CollectionEfficiency is the COVERAGE BRIDGE between the four modelled instruments
            // and a whole tax system, solved on the sourced per-country bases (TaxBaseTable), and it EXCEEDS 1 for
            // the five countries whose four instruments under-cover their real receipts (Country.CollectionEfficiency's
            // doc). The old Clamp01 would have cut those to 1 and knocked the anchored primary balance off in all
            // five; only the floor at 0 is a bound the quantity has.
            float effectiveCollectionEfficiency = Mathf.Max(0f, country.CollectionEfficiency + financeTreasuryCompetenceBias);
            // NOTE: `swfReturns` here is now the STRUCTURAL DRAW, not the realised market return - see
            // SwfStructuralDrawPercentPerYear. The parameter name is retained because it is the fund's
            // contribution to revenue either way, and renaming it across every call site would obscure
            // the diff that matters. The realised return no longer reaches the budget at all.
            //
            // SWF INCOME WAS PUT INSIDE THE MULTIPLIER (2026-08-02, Elias's ruling: fix the cause) - superseded for the draw by K213, below.
            //
            // They used to be added AFTER it - `... * fiscalReactionMultiplier + swfReturns` - which put
            // the fastest-growing component of a net creditor's revenue permanently beyond the reach of
            // the one mechanism that pushes back. Measured, not assumed: Sweden's multiplier pinned at its
            // 0.5 floor for 104 of 120 turns while fund returns reached 405% of the tax revenue the
            // multiplier could still act on, and the fund compounded 386 -> 10,902 while tax revenue grew
            // 174 -> 767. The stabiliser worked perfectly on the shrinking half and not at all on the
            // growing one.
            //
            // Inside the multiplier, the semantics are also the more defensible ones: a government whose
            // debt is far below its comfortable level LOOSENS - it spends its fund's windfall rather than
            // banking it - which is exactly what the fiscal reaction function already claims to model for
            // tax revenue. The symmetry holds at the other end too: a heavily indebted government leans
            // harder on its fund.
            // Pass 5: the tariff flow sat with the fund draw - inside the multiplier, outside CE (since K213 the draw is outside it, below).
            // EN-7b: the electricity tax's change sits with them - inside the multiplier, outside CE (the seeded taxes are inside CE's coverage bridge). ONE
            // expression, the flow always summed (0 at no law): a ternary over the two expressions - and even a dead if-block writing actualRevenue a second
            // time - moved the no-policy budget by a float's last digit on turn 1 (the sentinel caught both, 2026-09-15; §402's class)
            //
            // MF q33, K213 (2026-10-28, "q33 reaches fund draws"): THE DRAW IS BOOKED WHOLE - the budget receives exactly what left the fund, and the
            // multiplier acts on the taxes and the tariff beside it, no longer on the draw. Read against the ruling above and declared: its measured
            // runaway was the REALISED RETURN's (405 % of the reachable tax take), and the realised return stopped reaching the budget the same day (the
            // structural draw, SwfStructuralDrawPercentPerYear) - the draw is a bounded share of the fund, so booking it whole cannot compound as the
            // return did. The multiplier was to scale only the draw's demand effect, and the draw has none to scale: the national-accounts identity
            // reads the period's planned government consumption, never revenue, so scaling the draw moved only the money booked - money that left the
            // fund and never reached the budget. The stance keeps its whole reach on the taxes.
            float actualRevenue = (theoreticalRevenue * effectiveCollectionEfficiency + tariffRevenue + electricityTaxRevenue) * fiscalReactionMultiplier + swfReturns;
            // PS-6 US-24 (R-US21 (iv)): on a day the binding debt limit's cut is armed, the non-interest outlays are cut pro rata to what revenue leaves after
            // interest (UsDebtLimit.CutToday) - 0 on every other day and in every other country, and summed in one expression (§501: x - 0 is exact)
            float debtLimitCut = UsDebtLimit.CutToday(country, CurrentDate, actualRevenue, interestOnDebt, governmentSpending + mandatorySpending + unemploymentBenefitCost + welfareCost + swfContribution);
            // MF q33 (K142): in default's exclusion, or in a programme year whose disbursement was withheld, no borrowing finances the deficit - the
            // non-interest outlays left after the debt limit's cut are cut to revenue less interest (SovereignCrisis.CutToday) - 0 on every other day
            float crisisCut = SovereignCrisis.CutToday(country, CurrentDate, actualRevenue, interestOnDebt, governmentSpending + mandatorySpending + unemploymentBenefitCost + welfareCost + swfContribution - debtLimitCut);
            totalSpending = governmentSpending + mandatorySpending + unemploymentBenefitCost + interestOnDebt + welfareCost + swfContribution - debtLimitCut - crisisCut;
            _debtLimitCutToday = debtLimitCut;   // the review's M4: read by the daily accrual right after this call, to book the cut as its own line
            _crisisCutToday = crisisCut;   // MF q33 (the q32 review's M3): likewise for the closed market's cut
            budgetBalance = actualRevenue - totalSpending;

            state.Budget += budgetBalance;

            // NO FLOOR (2026-08-02, Elias's ruling). Debt may go NEGATIVE, which means the country is a
            // net creditor - a real fiscal state, and specifically Norway's, the country this project
            // already used to calibrate SWF returns. The game has always DISPLAYED "Net Government
            // Position (debt minus fund assets)"; only the simulation refused to represent it.
            //
            // The old `Mathf.Clamp(..., 0f, maxDebt)` is what produced the debt-to-zero bimodality:
            // a stock driven below zero was held at zero and then released, which is the exact shape of
            // a bimodal attractor. Sweden hit the floor on 67 of 120 baseline turns, France on 14, and
            // Germany only under `swfstress` - precisely the set whose SWF drives net position negative.
            //
            // THE NEGATIVE SIDE IS A RUNAWAY GUARD, NOT A BOUND (2026-08-02, Elias's ruling).
            //
            // A -300% symmetric bound was tried first and rejected: France settled at -297.6% against it,
            // which is not "close to a bound" but PINNED TO one, so every downstream reader - C4's rating
            // above all - was reading the clamp rather than the model. The cause is now fixed upstream
            // (SWF returns run through the fiscal reaction multiplier), and this exists only to stop an
            // unbounded excursion during and after that fix.
            //
            // ⚠ IT IS SET WHERE NOTHING SHOULD EVER REACH IT. If a country touches -1000% of GDP that is
            // a BUG REPORT, not a clamp doing its job - the guard has caught a runaway the stabiliser
            // should have prevented, and the correct response is to investigate rather than to widen it.
            //
            // MF q33 (2026-10-28): THE CEILING IS RETIRED. The 300 % bound absorbed deficits once a country reached it (GA-12-1, the audit's deficit pole);
            // the debt crisis at CCC does its work now, and the positive side carries a runaway guard of the same
            // kind as this one (DebtRunawayGuardPercent, in StoreDebt).

            // THE EROSION TERM (2026-08-17; mechanism-report rulings R1-R3). The standard
            // debt-dynamics identity's −π·b, previously missing: per R2's DECLARATION (recorded in
            // CLAUDE.md's "Accounting Convention" section) this model's dollars are constant-price
            // units and GovernmentDebt is the ONE nominal quantity - as sovereign debt is - so
            // inflation erodes its real value at π per year and deflation grows it, correctly
            // signed with no special case. SYMMETRIC per R3's reasoning: a net creditor's real
            // claim erodes the same way, so the factor applies to whichever position exists and
            // always shrinks it toward zero under inflation - no free money in either direction,
            // and no interaction with the surplus-spiral hazard (the term is self-limiting: it
            // scales with the position it erodes).
            //
            // SHAPE (the taxonomy): an annual rate on a SELF-REFERENCE, so the daily form is the
            // COMPOUNDING (power) slice - (1 − π/100)^fraction - not the linear slice. At constant
            // π the slices compose to the turn factor exactly; the interleaving with the daily
            // budgetBalance subtraction is the same within-period feedback Phase 3's stated drift
            // budget already covers (see the equivalence check's erosion enumeration). The inner
            // Max is defensive only - Inflation is produced clamped far below 100 everywhere.
            // P5-B6 (2026-09-05): THE EROSION TERM IS RETIRED. R2 (2026-08-17) declared the book constant-price with the debt as
            // its one nominal stock and bridged the two with −π·b; B6 makes the book nominal - revenue, spending and the balance
            // are in current prices - so the debt accrues nominal balances and the bridge would count inflation twice.
            // What erodes now is the debt-to-GDP RATIO, through nominal GDP, which is what the identity says.
            // MF q33 (ruled 2026-10-18 q33: "the clamp no longer absorbs deficits"): the stock takes the whole balance - the 300 % ceiling is retired, the
            // two runaway guards alone stand, and what they would absorb is booked (StoreDebt)
            StoreDebt(country, state.GovernmentDebt - budgetBalance);

            return actualRevenue;
        }
    }
}
