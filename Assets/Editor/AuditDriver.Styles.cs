using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PoliSim.Data;
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
    /// (money from an empty fund), X-02 the income tax at its floor, X-04 every programme at full generosity (approval through welfare), X-16 the
    /// most dovish central-bank head.</item>
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
                case "X-04":
                    foreach (WelfareProgram w in c.WelfarePrograms.Where(w => w.IsImplemented)) { DraftWelfare(w, 100f); }
                    Event("STYLE exploit X-04: every levied programme at full generosity");
                    break;
            }
        }

        private void DraftTax(TaxLine t, float rate) => ((IDictionary)Get("_taxRateInputs"))[t.Type] = rate;

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

        private void Act(Country c)
        {
            if (AtSafePoint()) { ParliamentActions(c); }

            if (!_cabinetDone &&(_day >= _spec.Cabinet && _spec.Cabinet >= 0 || _day >= _spec.SearchSave && _spec.SearchSave >= 0) && AtSafePoint())
            {
                _cabinetDone = true;
                int searched = SearchCabinet(c);
                Event($"ACT cabinet day {_day}: Search drew shortlists for {searched} portfolio(s)");
                if (_spec.SearchSave >= 0) { SaveFile(_day); }   // the shortlist is not in the save (inventory A S-24): a resumed game draws it again
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
