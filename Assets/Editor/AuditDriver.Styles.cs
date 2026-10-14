using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The protocol's five play styles (`docs/audit/AUDIT_PROTOCOL.md`, PHASE 1). A style moves the player's levers the way a player does: it writes
    /// the dial's own draft (the value a drag leaves, inside the dial's own track) and then runs the control's body - "Introduce budget bill" is
    /// `IntroduceBudgetBill(BuildBudgetBillFromDrafts())`, a law's button `IntroduceLawBill`, a tax's switch `IntroduceTaxProgramBill`, a
    /// programme's switch `IntroduceWelfareProgramBill`. The manager refuses what the role or the chamber does not allow, as it refuses a player.
    /// <list type="bullet">
    /// <item><b>idle</b> - no input: the standing budget restated at each window, every hold answered with its first control.</item>
    /// <item><b>historical</b> - follows the record: the standing budget, and at a formation the choice that seats the government of record
    /// (`SeatedGovernment.TryAt` a season after polling day) where the record holds it; otherwise as idle.</item>
    /// <item><b>fuzz</b> - random legal actions from the run's own generator: every dial anywhere on its track at each window, every hold's answer
    /// drawn, and every `every=` days one of a law, a tax's or a programme's switch, a fund drawdown.</item>
    /// <item><b>exploit</b> - maximises approval or money through a named lead of the inventory (`exploit=`): X-01 a fund at -10 % of GDP
    /// (money from an empty fund), X-02 the income tax at its floor, X-02S its adjustable sub-rows at their floor with the lever left standing (the
    /// household burden against the sub-rows, GA-2-11), X-02R the same and then each restored to the statute's figure (a sub-row put back, GA-2-12),
    /// X-04 every programme at full generosity (approval through welfare), X-16 the most dovish central-bank head.</item>
    /// <item><b>extreme</b> - every dial at an end, by `pole=`: deficit (taxes at their floor, spending and welfare at their top, the fund drawing,
    /// the pension age at 60), surplus (the reverse), big (taxes and spending at their top), small (both at their floor).</item>
    /// </list>
    /// </summary>
    internal sealed partial class AuditDriver
    {
        private int _lastActDay = -1;
        private bool _exploitArmed;

        // ------------------------------------------------------------------ the budget a style drafts

        /// <summary>The style's budget, written into the dials' drafts before the window's bill is tabled. Idle and historical leave the drafts as they stand.</summary>
        private void DraftBudget(Country c)
        {
            switch (_spec.Style)
            {
                case "extreme": DraftPole(c, _spec.Pole); break;
                case "fuzz": DraftRandom(c); break;
                case "exploit": DraftExploit(c); break;
            }
        }

        private void DraftPole(Country c, string pole)
        {
            if (pole == "spendup" || pole == "spenddown")   // the spending lines alone - what the families and the category effects should read (inventory E SD-30, B D-05)
            {
                bool up = pole == "spendup";
                foreach (SpendingLine line in c.SpendingLines) { (float min, float max) = SpendingTrack(c, line); DraftSpending(line, up ? max : min); }
                Event($"STYLE extreme ({pole}): every spending line at its track's {(up ? "top" : "floor")}; taxes, welfare, the fund and the pension age as they stand");
                return;
            }

            bool taxesUp = pole == "surplus" || pole == "big";
            bool spendUp = pole == "deficit" || pole == "big";
            foreach (TaxLine t in c.TaxLines.Where(t => t.IsImplemented)) { DraftTax(t, taxesUp ? t.MaxRate : t.MinRate); }
            foreach (SpendingLine line in c.SpendingLines) { (float min, float max) = SpendingTrack(c, line); DraftSpending(line, spendUp ? max : min); }
            foreach (WelfareProgram w in c.WelfarePrograms.Where(w => w.IsImplemented)) { DraftWelfare(w, spendUp ? 100f : 0f); }
            if (c.SovereignWealthFund != null) { Set("_swfContributionRateInput", pole == "deficit" ? -10f : pole == "surplus" ? 10f : (float?)null); }
            if (PensionAgeStatute.Has(c.Id)) { Set("_pensionAgeInput", (float?)(spendUp ? BudgetBill.PensionAgeMin : BudgetBill.PensionAgeMax)); }
            Event($"STYLE extreme ({pole}): taxes at their {(taxesUp ? "ceiling" : "floor")}, spending and welfare at their {(spendUp ? "top" : "floor")}, the fund {(c.SovereignWealthFund == null ? "absent" : pole == "deficit" ? "drawing 10 %" : pole == "surplus" ? "paying in 10 %" : "as it stands")}, the pension age {(PensionAgeStatute.Has(c.Id) ? (spendUp ? "60" : "70") : "no dial")}");
        }

        private void DraftRandom(Country c)
        {
            foreach (TaxLine t in c.TaxLines.Where(t => t.IsImplemented))
            {
                float grain = Mathf.Max(0.0001f, t.DialGrain);
                DraftTax(t, Mathf.Clamp(Mathf.Round((t.MinRate + (float)Rng("budget").NextDouble() * (t.MaxRate - t.MinRate)) / grain) * grain, t.MinRate, t.MaxRate));
            }

            foreach (SpendingLine line in c.SpendingLines)
            {
                (float min, float max) = SpendingTrack(c, line);
                DraftSpending(line, min + (float)Rng("budget").NextDouble() * (max - min));
            }

            foreach (WelfareProgram w in c.WelfarePrograms.Where(w => w.IsImplemented)) { DraftWelfare(w, Mathf.Round((float)Rng("budget").NextDouble() * 100f)); }
            if (c.SovereignWealthFund != null) { Set("_swfContributionRateInput", (float?)Mathf.Round(-10f + (float)Rng("budget").NextDouble() * 20f)); }
            if (PensionAgeStatute.Has(c.Id)) { Set("_pensionAgeInput", (float?)(Mathf.Round((BudgetBill.PensionAgeMin + (float)Rng("budget").NextDouble() * (BudgetBill.PensionAgeMax - BudgetBill.PensionAgeMin)) * 12f) / 12f)); }
            Event("STYLE fuzz: every budget dial drawn on its own track");
        }

        private void DraftExploit(Country c)
        {
            switch (_spec.Exploit)
            {
                case "X-01":
                    Set("_swfExistsDraft", (bool?)true);
                    Set("_swfContributionRateInput", (float?)-10f);
                    Event("STYLE exploit X-01: the fund exists, its contribution at -10 % of GDP (a draw on an empty fund)");
                    break;
                case "X-02":
                    TaxLine income = c.TaxLines.FirstOrDefault(t => t.IsImplemented && t.Type == TaxType.IncomeTax);
                    if (income != null) { DraftTax(income, income.MinRate); }
                    Event($"STYLE exploit X-02: the income tax at its floor ({(income == null ? "not levied" : income.MinRate.ToString("R", CultureInfo.InvariantCulture))})");
                    break;
                case "X-02S":
                case "X-02R":
                    DraftSubRows(c, _spec.Exploit == "X-02R");
                    break;
                case "X-04":
                    foreach (WelfareProgram w in c.WelfarePrograms.Where(w => w.IsImplemented)) { DraftWelfare(w, 100f); }
                    Event("STYLE exploit X-04: every levied programme at full generosity");
                    break;
                case "X-15":
                    if (c.SovereignWealthFund == null) { Event("STYLE exploit X-15: no fund to dissolve"); break; }
                    Set("_swfExistsDraft", (bool?)false);
                    Event($"STYLE exploit X-15: the fund dissolved in the bill (assets {c.SovereignWealthFund.TotalAssets.ToString("R", CultureInfo.InvariantCulture)})");
                    break;
            }
        }

        private int _exploitLastDay = -1000;
        private bool _exploitDone;

        /// <summary>The sweep's levers outside the budget, each through its control's own body, retried every 30 days until the bill is introduced:
        /// X-05 levies every tax the country does not (the tax's switch, `IntroduceTaxProgramBill`, one at a time); X-17 paid family leave to 104
        /// weeks (the labour tab's draft and its Introduce, `BuildLaborBillFromDrafts`); X-07S every sector at regulation 0 and deregulation 100
        /// (the sector tab's drafts and its Introduce, `BuildSectorBillFromDrafts`).</summary>
        private void ExploitActs(Country c)
        {
            if (_spec.Style != "exploit" || _exploitDone || _day - _exploitLastDay < 30) { return; }
            _exploitLastDay = _day;
            switch (_spec.Exploit)
            {
                case "X-05":
                {
                    TaxLine t = c.TaxLines.FirstOrDefault(l => !l.IsImplemented && !_sm.GetPendingTaxProgramBills(c.Id).Any());
                    if (t == null) { if (c.TaxLines.All(l => l.IsImplemented)) { _exploitDone = true; Event($"ACT exploit X-05 day {_day}: every tax levied"); } return; }
                    bool ok = _sm.IntroduceTaxProgramBill(c.Id, t.Type, true);
                    Count("act exploit levy");
                    Event($"ACT exploit X-05 day {_day}: levy {t.Type} - {(ok ? "a bill introduced" : "refused")} (IntroduceTaxProgramBill, the tax's switch)");
                    break;
                }
                case "X-17":
                {
                    Set("_paidFamilyLeaveWeeksInput", (float?)104f);
                    bool ok = _sm.IntroduceLaborBill(c.Id, (LaborPolicyBill)Call("BuildLaborBillFromDrafts"));
                    Count("act exploit leave");
                    if (ok) { _exploitDone = true; }
                    Event($"ACT exploit X-17 day {_day}: paid family leave to 104 weeks - {(ok ? "a bill introduced" : "refused")} (IntroduceLaborBill(BuildLaborBillFromDrafts()), the labour tab's Introduce)");
                    break;
                }
                case "X-07S":
                {
                    var regulation = (Dictionary<SectorType, float>)Get("_sectorRegulationInputs");
                    var deregulation = (Dictionary<SectorType, float>)Get("_sectorDeregulationInputs");
                    foreach (Sector s in c.Sectors) { regulation[s.Type] = 0f; deregulation[s.Type] = 100f; }
                    bool ok = _sm.IntroduceSectorBill(c.Id, (SectorPolicyBill)Call("BuildSectorBillFromDrafts"));
                    Count("act exploit sectors");
                    if (ok) { _exploitDone = true; }
                    Event($"ACT exploit X-07S day {_day}: every sector at regulation 0 and deregulation 100 - {(ok ? "a bill introduced" : "refused")} (IntroduceSectorBill(BuildSectorBillFromDrafts()))");
                    break;
                }
            }
        }

        private void DraftTax(TaxLine t, float rate) => ((IDictionary)Get("_taxRateInputs"))[t.Type] = rate;

        /// <summary>X-02S (GA-2-11's repro): every adjustable sub-row of the income tax at its dial's floor, the lever where it stands - the Budget
        /// page's schedule rows as a drag to each track's left end leaves them, through the dial's own body (DragSubRow, given the rate without the
        /// lever's shift and the knob's step); the window's bill carries them (`BuildBudgetBillFromDrafts`). X-02R (GA-2-12's repro): the same while the
        /// line carries no rate of its own, and once one has passed, every sub-row carrying its own rate dragged back to the statute's figure - a player's
        /// restore. With `snap=` the drag lands where the knob does (`LedgerRow.Snap`: the grid, clamped to the track, with the dial's detent at the
        /// statute's figure); without, on the exact figure, clamped to the track. With `leverdelta=` the lever is drafted that far from its seed at the
        /// floor window, so the track carries a shift.</summary>
        private void DraftSubRows(Country c, bool restore)
        {
            string key = restore ? "X-02R" : "X-02S";
            TaxLine line = c.TaxLines.FirstOrDefault(t => t.IsImplemented && t.Type == TaxType.IncomeTax);
            if (line == null || !TaxSchedule.Responds(c.Id)) { Event($"STYLE exploit {key}: no income schedule with sub-rows to draft"); return; }
            List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(TaxSchedule.Of(c.Id));
            bool back = restore && line.BracketRates != null && line.BracketRates.Any(rate => rate >= 0f);
            if (!back && _spec.LeverDelta != 0f) { DraftTax(line, Mathf.Clamp(TaxSchedule.RateSeedOf(line) + _spec.LeverDelta, line.MinRate, line.MaxRate)); }
            var levers = (IDictionary)Get("_taxRateInputs");
            float lever = levers.Contains(TaxType.IncomeTax) ? (float)levers[TaxType.IncomeTax] : line.Rate;
            float shift = lever - TaxSchedule.RateSeedOf(line);   // the dial reads the rate with the lever's shift; the drag leaves the rate without it
            float step = _spec.Snap;
            var landings = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (!rows[i].Adjustable) { continue; }
                if (back && !(i < line.BracketRates.Length && line.BracketRates[i] >= 0f)) { continue; }
                float aim = back ? (float)rows[i].StatuteRate : 0f - shift;   // the rate without the lever's shift the drag aims at: the statute's figure, or the track's left end
                float top = GameController.SubRowTrackTop;
                float detent = Mathf.Clamp((float)(rows[i].StatuteRate + shift), 0f, top);   // summed as the dial's is
                float knob = step > 0f ? LedgerRow.Snap(aim + shift, step, 0f, top, detent) : Mathf.Clamp(aim + shift, 0f, top);
                float landed = knob - shift;
                Call("DragSubRow", rows[i], i, landed, step > 0f ? step : LedgerRow.SnapStep, shift);
                landings.Add(landed.ToString("0.###", CultureInfo.InvariantCulture));
            }
            int drafts = ((IDictionary)Get("_bracketRateInputs")).Count;
            string grid = step > 0f ? "on a " + step.ToString("0.##", CultureInfo.InvariantCulture) + " step" : "unsnapped";
            string leverWords = "the lever at " + lever.ToString("0.###", CultureInfo.InvariantCulture) + (!back && _spec.LeverDelta != 0f ? " (drafted " + _spec.LeverDelta.ToString("+0.##;-0.##", CultureInfo.InvariantCulture) + " from its seed)" : "");
            Event(back ? $"STYLE exploit {key}: the income tax's own sub-row rates dragged back to the statute's figures, {grid} (landed {string.Join(";", landings)}; {drafts} draft(s) standing), {leverWords}"
                       : $"STYLE exploit {key}: the income tax's adjustable sub-rows at their dials' floor, {grid} (landed {string.Join(";", landings)}; {drafts} draft(s) standing), {leverWords}");
        }

        private void DraftWelfare(WelfareProgram w, float generosity) => ((IDictionary)Get("_welfareGenerosityInputs"))[w.Type] = Mathf.Clamp(generosity, 0f, 100f);

        /// <summary>The spending dial's draft: removed where it lands on the standing figure, as the dial removes it.</summary>
        private void DraftSpending(SpendingLine line, float figure)
        {
            var drafts = (IDictionary)Get("_spendingLineInputs");
            if (Mathf.Approximately(figure, line.Amount)) { drafts.Remove(line.Category); } else { drafts[line.Category] = figure; }
        }

        /// <summary>The spending dial's track, as the Budget page computes it: this year's allowed change around the standing figure (the mandatory and
        /// discretionary ranges are the controller's own constants), on the line's own path where a dial cost stands outside it.</summary>
        private (float Min, float Max) SpendingTrack(Country c, SpendingLine line)
        {
            float range = (float)StaticOf(typeof(GameController), line.IsMandatory ? "MandatoryPercentChangeRange" : "DiscretionaryPercentChangeRange");
            float standing = line.Amount;
            float min = standing * (1f - range / 100f), max = standing * (1f + range / 100f);
            float dialCost = (float)InvokeStatic(typeof(SimulationManager), "DialCostOf", c, line);
            if (dialCost != 0f)
            {
                float own = standing - dialCost;
                min = (float)InvokeStatic(typeof(SimulationManager), "LandedTotalOf", c, line, own * (1f - range / 100f) + dialCost);
                max = (float)InvokeStatic(typeof(SimulationManager), "LandedTotalOf", c, line, own * (1f + range / 100f) + dialCost);
            }

            return (min, max);
        }

        // ------------------------------------------------------------------ the style's own actions between windows

        /// <summary>The style's actions on the days it chooses (the budget is drafted at the window, in the hold's answer).</summary>
        private bool _cabinetDone;

        /// <summary>The cabinet card's Search: a shortlist drawn for every portfolio neither held nor already searched, in the enum's order (the Search
        /// button's body, GameController.PoliticsV35.cs) - it draws the simulation's cabinet stream.</summary>
        private int SearchCabinet(Country c)
        {
            var shortlists = (IDictionary)Get("_cabinetCandidatesByPortfolio");
            int searched = 0;
            foreach (CabinetPortfolio portfolio in Enum.GetValues(typeof(CabinetPortfolio)))
            {
                if (c.CabinetMinisters.ContainsKey(portfolio) || shortlists.Contains(portfolio)) { continue; }
                shortlists[portfolio] = CabinetSystem.GenerateCandidates(portfolio);
                searched++;
            }

            Count("act cabinet search");
            return searched;
        }

        /// <summary>The Appoint button on each shortlisted card the player may act on: the first candidate, the card's own body.</summary>
        private int AppointCabinet(Country c)
        {
            var shortlists = (IDictionary)Get("_cabinetCandidatesByPortfolio");
            var appointed = new List<string>();
            foreach (CabinetPortfolio portfolio in Enum.GetValues(typeof(CabinetPortfolio)))
            {
                if (!shortlists.Contains(portfolio) || !_sm.PlayerMayIntroduce(c.Id, portfolio, out _)) { continue; }
                var candidates = (IList)shortlists[portfolio];
                if (candidates == null || candidates.Count == 0) { continue; }
                var candidate = (CabinetMinister)candidates[0];
                c.CabinetMinisters[portfolio] = candidate;
                shortlists.Remove(portfolio);
                Call("RecomputePolicyPreview");
                appointed.Add(portfolio + " " + candidate.Name);
            }

            Count("act cabinet appoint");
            Event($"ACT cabinet day {_day}: appointed {(appointed.Count == 0 ? "nobody (no shortlist the player may act on)" : string.Join(", ", appointed))} (the cards' Appoint)");
            return appointed.Count;
        }

        private readonly HashSet<int> _motionsDone = new HashSet<int>();
        private bool _withdrawDone, _leaveDone;

        /// <summary>The Parliament tab's rows, on the days the plan names: MOVE NO CONFIDENCE / ELECT A SUCCESSOR (`MoveNoConfidence`), WITHDRAW
        /// SUPPORT (`WithdrawSupport`), LEAVE THE GOVERNMENT (`LeaveGovernment`) - each the row's own call, refused by the manager where the role or
        /// the procedure does not allow it, as a row a player cannot press.</summary>
        private void ParliamentActions(Country c)
        {
            foreach (int day in _spec.Motion)
            {
                if (day > _day || _motionsDone.Contains(day)) { continue; }
                _motionsDone.Add(day);
                bool ok = _sm.MoveNoConfidence(c.Id, out string refused, out ConfidenceProcedure.MotionVote vote);
                Count("act motion");
                Event($"ACT motion day {_day}: MoveNoConfidence {(ok ? "moved" : "REFUSED - " + refused)}{(ok ? " (" + vote + ")" : string.Empty)}");
            }

            if (!_withdrawDone && _spec.Withdraw >= 0 && _day >= _spec.Withdraw)
            {
                _withdrawDone = true;
                bool ok = _sm.WithdrawSupport(c.Id, out string refused);
                Count("act withdraw");
                Event($"ACT withdraw day {_day}: WithdrawSupport {(ok ? "done" : "REFUSED - " + refused)}");
            }

            if (!_leaveDone && _spec.Leave >= 0 && _day >= _spec.Leave)
            {
                _leaveDone = true;
                bool ok = _sm.LeaveGovernment(c.Id, out string refused);
                Count("act leave");
                Event($"ACT leave day {_day}: LeaveGovernment {(ok ? "done" : "REFUSED - " + refused)}");
            }
        }

        // ------------------------------------------------------------------ the campaign (area 5)

        private static readonly CampaignActionKind[] RunUpVerbs = { CampaignActionKind.EstablishOffice, CampaignActionKind.CommissionPolling, CampaignActionKind.PrepareAdvertising };
        private static readonly CampaignActionKind[] GroundActs = { CampaignActionKind.Rally, CampaignActionKind.TownHall, CampaignActionKind.DoorToDoor };
        private static readonly CampaignActionKind[] AirActs = { CampaignActionKind.TelevisionAd, CampaignActionKind.DigitalAd, CampaignActionKind.SocialPost };
        private static readonly CampaignActionKind[] AllActs =
        {
            CampaignActionKind.Rally, CampaignActionKind.TownHall, CampaignActionKind.DoorToDoor, CampaignActionKind.TelevisionAd,
            CampaignActionKind.DigitalAd, CampaignActionKind.SocialPost, CampaignActionKind.PolicyAnnouncement, CampaignActionKind.Interview,
        };
        private DateTime _campaignActDate = DateTime.MinValue;
        private bool _runUpDone;
        private int _campaignLogDay = -1;

        /// <summary>The HQ's chips, pressed as their bodies press them (`GameController.Campaign.cs`): in the run-up the three priced verbs at
        /// `PreCampaignRun.Price` and the five hires at the salary with their role, once; in the campaign the plan's mix - ground (rally, town hall,
        /// doors), air (TV, digital, post) or all (the eight) - each legal act once a day at its `ActionSpec.MoneyCost`, a local one aimed by the HQ's own
        /// `LocalActRegion` (the map's pick, else the one-region rule). A refusal is logged as the chip logs it.</summary>
        private void CampaignActs()
        {
            if (string.IsNullOrEmpty(_spec.Campaign) || _lastDate == _campaignActDate) { return; }
            PreCampaignRun.State pre = _sm.PlayerPreCampaign;
            CampaignRun.State live = _sm.PlayerCampaign;
            if (live == null && pre == null) { return; }
            _campaignActDate = _lastDate;
            if (live == null)
            {
                if (_runUpDone) { return; }
                _runUpDone = true;
                foreach (CampaignActionKind verb in RunUpVerbs)
                {
                    if (PreCampaignRun.Refusal(verb) != null) { continue; }   // the chip is drawn disabled
                    bool ok = _sm.QueueCampaignDecision(verb, -1, null, PreCampaignRun.Price(pre, verb), out string refusal);
                    Count("act run-up verb");
                    Event($"ACT campaign day {_day}: run-up {verb} {(ok ? "queued" : "REFUSED - " + refusal)}");
                }
                foreach (StaffRole role in CampaignStaff.TheFive)
                {
                    if (pre.HasRole(role)) { continue; }   // the chip is dimmed once the role is on the roster
                    bool ok = _sm.QueueCampaignDecision(CampaignActionKind.RecruitStaff, -1, null, CampaignStaff.SalaryPerDay, out string refusal, role: (int)role);
                    Count("act run-up hire");
                    Event($"ACT campaign day {_day}: hire {role} {(ok ? "queued" : "REFUSED - " + refusal)}");
                }
                return;
            }

            int me = _sm.PlayerPartyIndexForCampaign();
            if (live.Finished || me < 0 || live.Ledgers == null || me >= live.Ledgers.Length) { return; }
            CampaignPhase phase = live.Setup.Calendar.PhaseOn(_sm.CurrentDate);
            CampaignActionKind[] mix = _spec.Campaign == "ground" ? GroundActs : _spec.Campaign == "air" ? AirActs : _spec.Campaign == "all" || _spec.Campaign == "paced" ? AllActs : Array.Empty<CampaignActionKind>();
            // paced: what is left spread over the days left, within the day's hours - the acts taken in a turn that rotates by day
            double budget = double.MaxValue, hours = double.MaxValue;
            if (_spec.Campaign == "paced" && live.Pools != null && me < live.Pools.Length)
            {
                budget = live.Pools[me].Money / Math.Max(1, live.TotalDays - live.Day);
                hours = live.Pools[me].Hours;
            }
            int queued = 0, refused = 0;
            string firstRefusal = null;
            for (int i = 0; i < mix.Length; i++)
            {
                CampaignActionKind kind = mix[_spec.Campaign == "paced" ? (i + _day) % mix.Length : i];
                if (!CampaignLegality.IsLegal(kind, phase)) { continue; }   // the chip is drawn disabled
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                if (spec.MoneyCost > budget || spec.Hours > hours) { continue; }
                int region = spec.IsLocal ? (int)Call("LocalActRegion") : -1;
                if (_sm.QueueCampaignDecision(kind, region, null, spec.MoneyCost, out string refusal)) { queued++; budget -= spec.MoneyCost; hours -= spec.Hours; }
                else { refused++; firstRefusal = firstRefusal ?? kind + ": " + refusal; }
                Count("act campaign " + kind);
            }

            if (_day % 7 == 0 || refused > 0 && _campaignLogDay < 0)
            {
                if (refused > 0) { _campaignLogDay = _day; }
                CampaignRun.PartyLedger l = live.Ledgers[me];
                Event(string.Format(CultureInfo.InvariantCulture, "ACT campaign day {0} ({1}, campaign day {2} of {3}): {4} queued, {5} refused{6}; done so far {7} act(s) [{8}], {9:N0} spent on acts, chest {10:N0}, persuasion {11:F4}",
                    _day, phase, live.Day, live.TotalDays, queued, refused, firstRefusal == null ? string.Empty : " - first: " + firstRefusal, l.TotalActions,
                    string.Join(" ", CampaignActions.TheEight.Select((k, j) => k + " " + l.ActionCount[j])), l.MoneySpentOnActions,
                    live.Pools != null && me < live.Pools.Length ? live.Pools[me].Money : double.NaN, l.PersuasionDelivered));
            }
        }

        // ------------------------------------------------------------------ the fleet (area 6)

        private DateTime _fleetActDate = DateTime.MinValue;
        private readonly HashSet<int> _fleetDone = new HashSet<int>();

        /// <summary>The Energy page's plant buttons as their bodies press them (`GameController.EnergyV35.cs` DrawEnergyPlantTile): − places a
        /// retirement of one step (`EnergyFleet.Place` with minus the step), + a build of one step or the queue's room (`EnergyConnectionQueue.StepUpMw`),
        /// each only where the tile draws its button enabled (`EnergyFleet.CanOrder`, the government's `PlayerMayIntroduce`, the queue's `FullText`),
        /// the page's cached preview dropped as the body drops it. The plan's `energy=` names the presses, once a technology a day: retirefossil (coal
        /// and gas until nothing is left to retire), retirenuclear, buildgreen (wind and solar while the queue has room).</summary>
        private void FleetActs(Country c)
        {
            if (string.IsNullOrEmpty(_spec.Energy) || _lastDate == _fleetActDate) { return; }
            _fleetActDate = _lastDate;
            if (!_sm.PlayerMayIntroduce(c.Id, out string _)) { return; }   // the tiles' buttons are drawn disabled
            int year = c.CalendarYear;
            double step = EnergyFleet.StepMw(c.Id);
            int[] retire = _spec.Energy == "retirefossil" ? new[] { 0, 1 } : _spec.Energy == "retirenuclear" ? new[] { 2 } : Array.Empty<int>();
            int[] build = _spec.Energy == "buildgreen" ? new[] { 4, 5 } : Array.Empty<int>();
            foreach (int k in retire)
            {
                if (_fleetDone.Contains(k) || !EnergyFleet.CanOrder(c.Id, k)) { continue; }
                EnergyFleet.Order o = EnergyFleet.Place(c, k, -step, year, _sm.CurrentTurn);
                Set("_hasCachedPreview", false);
                Count("act fleet retire " + EnergyLayerData.Labels[k]);
                if (o == null) { _fleetDone.Add(k); Event($"ACT fleet day {_day}: {EnergyLayerData.Labels[k]} - nothing left to retire (capacity {EnergyFleet.CapacityMw(c, k):F0} MW, retirements queued {EnergyFleet.QueuedMw(c, k, retirementsOnly: true):F0} MW)"); }
            }

            foreach (int k in build)
            {
                if (!EnergyFleet.CanOrder(c.Id, k)) { continue; }
                string full = EnergyConnectionQueue.FullText(c, k, year);
                if (full != null) { if (_fleetDone.Add(k)) { Event($"ACT fleet day {_day}: {EnergyLayerData.Labels[k]} - the queue is full ({full})"); } continue; }
                EnergyFleet.Order o = EnergyFleet.Place(c, k, EnergyConnectionQueue.StepUpMw(c, k, step, year), year, _sm.CurrentTurn);
                Set("_hasCachedPreview", false);
                Count("act fleet build " + EnergyLayerData.Labels[k]);
                if (o == null && _fleetDone.Add(k)) { Event($"ACT fleet day {_day}: {EnergyLayerData.Labels[k]} - the build was refused"); }
            }

            if (_day % 90 == 0)
            {
                Event($"ACT fleet day {_day}: capacity {string.Join(" ", EnergyLayerData.Labels.Select((label, k) => label + " " + EnergyFleet.CapacityMw(c, k).ToString("F0", CultureInfo.InvariantCulture)))} MW; queued {string.Join(" ", EnergyLayerData.Labels.Select((label, k) => label + " " + EnergyFleet.QueuedMw(c, k).ToString("F0", CultureInfo.InvariantCulture)))} MW");
            }
        }

        private void Act(Country c)
        {
            if (AtSafePoint()) { ParliamentActions(c); CampaignActs(); FleetActs(c); ExploitActs(c); }

            if (!_cabinetDone &&(_day >= _spec.Cabinet && _spec.Cabinet >= 0 || _day >= _spec.SearchSave && _spec.SearchSave >= 0) && AtSafePoint())
            {
                _cabinetDone = true;
                int searched = SearchCabinet(c);
                Event($"ACT cabinet day {_day}: Search drew shortlists for {searched} portfolio(s)");
                if (_spec.SearchSave >= 0) { SaveFile(_day); }   // GA-1-2: the shortlist is in the save - a resumed game's Search finds it drawn and appoints from it (inventory A S-24 found it dropped)
                AppointCabinet(c);
            }

            // With the window's hold off the clock runs on through it: a style with a budget of its own tables it while the window is open; idle,
            // as a player who presses nothing, lets the window pass.
            if (!GameSettings.HoldOnBudgetWindow && _spec.Style != "idle" && _sm.GetPendingBudgetProcess(c.Id) && AtSafePoint()) { TableBudget(c, "WINDOW"); }

            if (_spec.Style == "exploit" && !_exploitArmed && _spec.Exploit == "X-04")
            {
                // Approval through welfare: the programmes not levied are switched on through their own switch, once.
                _exploitArmed = true;
                foreach (WelfareProgram w in c.WelfarePrograms.Where(w => !w.IsImplemented).ToList())
                {
                    bool ok = _sm.IntroduceWelfareProgramBill(c.Id, w.Type, true);
                    Count("act welfare switch");
                    Event($"ACT exploit X-04: {w.Type} switched on - {(ok ? "a bill introduced" : "REFUSED")} (IntroduceWelfareProgramBill, the switch's call)");
                    if (ok) { break; }   // one bill of the kind is pending at a time; the next window's bill carries the rest
                }
            }

            if (_spec.Style != "fuzz" || _spec.Every <= 0 || _day == _lastActDay || _day % _spec.Every != 0 || !AtSafePoint()) { return; }
            _lastActDay = _day;
            int pick = Rng("act").Next(4);
            switch (pick)
            {
                case 0:
                {
                    List<LawDefinition> laws = LawCatalog.All;
                    LawDefinition law = laws[Rng("act").Next(laws.Count)];
                    bool enacted = c.EnactedLaws.Any(e => e.LawId == law.Id);
                    bool ok = _sm.IntroduceLawBill(c.Id, new LawBill { LawId = law.Id, IsRepeal = enacted });
                    Count("act law");
                    Event($"ACT fuzz day {_day}: {(enacted ? "repeal" : "enact")} '{law.Id}' - {(ok ? "a bill introduced" : "refused")} (IntroduceLawBill, the law's button)");
                    break;
                }
                case 1:
                {
                    TaxLine t = c.TaxLines[Rng("act").Next(c.TaxLines.Count)];
                    bool ok = _sm.IntroduceTaxProgramBill(c.Id, t.Type, !t.IsImplemented);
                    Count("act tax switch");
                    Event($"ACT fuzz day {_day}: {(t.IsImplemented ? "remove" : "levy")} {t.Type} - {(ok ? "a bill introduced" : "refused")} (IntroduceTaxProgramBill, the tax's switch)");
                    break;
                }
                case 2:
                {
                    WelfareProgram w = c.WelfarePrograms[Rng("act").Next(c.WelfarePrograms.Count)];
                    bool ok = _sm.IntroduceWelfareProgramBill(c.Id, w.Type, !w.IsImplemented);
                    Count("act welfare switch");
                    Event($"ACT fuzz day {_day}: {(w.IsImplemented ? "end" : "start")} {w.Type} - {(ok ? "a bill introduced" : "refused")} (IntroduceWelfareProgramBill, the programme's switch)");
                    break;
                }
                default:
                {
                    if (c.SovereignWealthFund == null) { Event($"ACT fuzz day {_day}: a drawdown drawn, but no fund exists - nothing to press"); break; }
                    float percent = Mathf.Round((float)Rng("act").NextDouble() * 25f);
                    bool ok = _sm.IntroduceSwfDrawdownBill(c.Id, new SwfDrawdownBill { WithdrawalPercentOfGdp = percent });
                    Count("act drawdown");
                    Event($"ACT fuzz day {_day}: a drawdown of {percent} % of GDP - {(ok ? "a bill introduced" : "refused")} (IntroduceSwfDrawdownBill)");
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ a hold's answer, as the style chooses it

        /// <summary>The chair's candidate: idle and historical the first drawn; fuzz any; extreme the hawk or the dove by pole; the exploit X-16 the dove;
        /// a plan's `chair=` over all of them.</summary>
        private int PickChair(IList candidates)
        {
            if (_spec.Chair == "first") { return 0; }
            if (_spec.Chair == null && _spec.Style == "fuzz") { return Rng("chair").Next(candidates.Count); }
            bool dove = _spec.Chair == "dove" || (_spec.Chair == null && ((_spec.Style == "exploit" && _spec.Exploit == "X-16") || (_spec.Style == "extreme" && (_spec.Pole == "deficit" || _spec.Pole == "big"))));
            bool hawk = _spec.Chair == "hawk" || (_spec.Chair == null && _spec.Style == "extreme" && !dove);
            if (!dove && !hawk) { return 0; }
            int best = 0;
            for (int i = 1; i < candidates.Count; i++)
            {
                float bias = ((FedChair)candidates[i]).RateBias, held = ((FedChair)candidates[best]).RateBias;
                if (dove ? bias < held : bias > held) { best = i; }
            }

            return best;
        }

        /// <summary>Historical: does the record's government after this election seat the player's party? Null where the record holds no government a season on.</summary>
        private bool? RecordSeats(Country c)
        {
            if (!SeatedGovernment.TryAt(c.Id, _sm.CurrentDate.AddDays(120), out SeatedGovernment.Record record) || record.Cabinet == null) { return null; }
            return record.Cabinet.Contains(c.PlayerPartyAbbrev) || (record.Support != null && record.Support.Contains(c.PlayerPartyAbbrev));
        }

        // ------------------------------------------------------------------ reflection on statics

        private static object StaticOf(Type type, string name)
        {
            FieldInfo f = type.GetField(name, Any);
            if (f == null) { throw new MissingFieldException(type.Name, name); }
            return f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null);
        }

        private static object InvokeStatic(Type type, string name, params object[] args)
        {
            MethodInfo m = type.GetMethods(Any).FirstOrDefault(x => x.Name == name && x.IsStatic && x.GetParameters().Length == args.Length);
            if (m == null) { throw new MissingMethodException(type.Name, name); }
            try { return m.Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
    }
}
