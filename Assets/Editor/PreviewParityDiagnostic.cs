using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Step 2's hand-list class killer: runs PreviewTurn and the real boundary FROM THE SAME
    /// STATE and compares approval ledgers TERM BY TERM - a preview-clone escape (the
    /// BaselineGini class, three recorded appearances) surfaces as a mismatched term that NAMES
    /// ITSELF, and every future term is covered the day it is added, with no list to maintain.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the assert covers the SEVEN terms whose
    /// inputs the preview does not advance - Reversion, TaxHikePenalty (0 under a no-op
    /// decision), SpendingEffect (likewise 0), WelfareEffect, PaidLeaveEffect, DrugPolicyEffect,
    /// GiniEffect (the preview never runs a Gini update; verified against PreviewTurn's body).
    /// The remaining five - GrowthEffect and the four misery gaps - are EXPECTED-DIFFERENT BY
    /// DESIGN: the preview models the COMING period (its clone runs the turn-form identity,
    /// Okun, Phillips and the crime/corruption updates before its formula), while the real
    /// boundary measures the elapsed one. They are printed for the record, never asserted, and
    /// an escape in THEIR constants (e.g. NAIRU) is outside this check's evidence - said here
    /// so the check is never cited for it.</para>
    ///
    /// <para><b>THE FISCAL LEDGER (Step 2's third section, 2026-08-25) - which side of the
    /// boundary each term sits on, stated per the build directive.</b> ZERO of its five terms
    /// (primary balance, fiscal reaction, interest at issuance, rate lag, erosion) are asserted
    /// here, and none are printed, BY DESIGN and not by omission: every one is accrued across
    /// 365 daily slices on a MOVING stock (the Phase-3 within-period feedback class), while the
    /// preview runs the single-step turn form on a clone that never enters the daily path and
    /// so has no ledger to print. Their turn-vs-daily agreement is the aggregation-equivalence
    /// bar's question (117/117 within 3%), not parity's; a term that sits on the "unadvanced
    /// inputs" side of this check's boundary does not exist in the fiscal chain. What this
    /// check DOES assert for the fiscal ledger is the hand-list property itself: the REAL
    /// country's accruing debt ledger is byte-untouched across a preview (days recorded and
    /// term sum identical before and after), so a future clone escape that reached the ledger
    /// would name itself here.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.PreviewParityDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class PreviewParityDiagnostic
    {
        private const float Tolerance = 0.0005f;

        [MenuItem("PoliSim/Run Preview Parity Diagnostic (approval terms)")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "PARITY: clean from menu." : $"PARITY: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PARITY");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                // One full period of days so every daily system has real state, then previews
                // from the exact state the boundary will resolve.
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }

                // Step 2's third section: the real accruing DEBT ledger before any preview runs -
                // 365 observed days by now. A preview must leave it byte-untouched (the clone
                // carries null ledgers by the hand-list); this is the assert with teeth for that.
                var fiscalDaysBefore = new Dictionary<CountryId, int>();
                var fiscalTermSumBefore = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    fiscalDaysBefore[c.Id] = c.FiscalLedgerAccruing?.DaysRecorded ?? -1;
                    fiscalTermSumBefore[c.Id] = c.FiscalLedgerAccruing?.TermSum ?? float.NaN;
                }

                var previews = new Dictionary<CountryId, ApprovalAttribution>();
                foreach (Country c in world.Countries)
                {
                    previews[c.Id] = sim.PreviewTurn(c.Id, PolicyDecision.None()).ApprovalTerms;
                }

                int failures = 0;
                // P2-3.4 (2026-09-02): THE PROJECTED RATE PATH IS THE RULE ON THE PROJECTION. For every country
                // with a sitting chair: the preview's rule reading re-derived from the rule's own formula on the
                // preview's year-end readings equals PolicyPreview.PreviewRuleRate; the path's three steps are the
                // chair's target arithmetic (reading + lean, clamped) and the adjustment speed applied twice, from
                // the rate today; and the previewed year ran at the target on today's readings.
                foreach (Country c in world.Countries)
                {
                    if (c.CurrentFedChair == null) { continue; }
                    PolicyPreview p = sim.PreviewTurn(c.Id, PolicyDecision.None());
                    RatePathProjection.Step[] path = RatePathProjection.Project(c, p);
                    float ruleOnPreview = Mathf.Max(0f, TaylorRule.NeutralRealRate(c) + p.PreviewInflation
                        + TaylorRule.InflationGapWeight(c) * (p.PreviewInflation - TaylorRule.InflationTarget(c))
                        + TaylorRule.UnemploymentGapWeight(c) * (p.PreviewNaturalUnemployment - p.PreviewUnemployment));
                    float ruleNow = TaylorRule.GetSuggestedInterestRate(c);
                    float targetNow = Mathf.Clamp(ruleNow + c.CurrentFedChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float targetNext = Mathf.Clamp(ruleOnPreview + c.CurrentFedChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float r0 = c.CurrencyZone.InterestRate;
                    float r1 = r0 + (targetNow - r0) * FederalReserveSystem.RateAdjustmentSpeed;
                    float r2 = r1 + (targetNext - r1) * FederalReserveSystem.RateAdjustmentSpeed;
                    bool ok = path != null && path.Length == 3
                        && Mathf.Abs(p.PreviewRuleRate - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[2].RuleReading - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[0].Rate - r0) < 1e-5f && Mathf.Abs(path[1].Rate - r1) < 1e-5f && Mathf.Abs(path[2].Rate - r2) < 1e-5f
                        && Mathf.Abs(path[1].Target - targetNow) < 1e-5f && Mathf.Abs(path[2].Target - targetNext) < 1e-5f
                        && Mathf.Abs(p.PreviewedInterestRate - targetNow) < 1e-4f;
                    if (!ok)
                    {
                        failures++;
                        Debug.LogError($"PARITY: {c.Id} rate path does not re-derive: preview rule {p.PreviewRuleRate:F4} vs formula {ruleOnPreview:F4}; path "
                                       + (path == null ? "null" : $"{path[0].Rate:F4} -> {path[1].Rate:F4} -> {path[2].Rate:F4}") + $" vs {r0:F4} -> {r1:F4} -> {r2:F4}; previewed rate {p.PreviewedInterestRate:F4} vs target {targetNow:F4}.");
                    }
                    Debug.Log($"PARITY: {c.Id} rate path {r0:F2}% -> {r1:F2}% -> {r2:F2}% (rule now {ruleNow:F2}%, on the preview {ruleOnPreview:F2}%; preview inflation {p.PreviewInflation:F2}%, unemployment {p.PreviewUnemployment:F2}% vs NAIRU {p.PreviewNaturalUnemployment:F2}%) {(ok ? "ok" : "FAIL")}.");
                }
                foreach (Country c in world.Countries)
                {
                    int daysAfter = c.FiscalLedgerAccruing?.DaysRecorded ?? -1;
                    float termSumAfter = c.FiscalLedgerAccruing?.TermSum ?? float.NaN;
                    bool untouched = daysAfter == fiscalDaysBefore[c.Id]
                        && (float.IsNaN(termSumAfter) ? float.IsNaN(fiscalTermSumBefore[c.Id]) : termSumAfter == fiscalTermSumBefore[c.Id]);
                    if (!untouched)
                    {
                        Debug.LogError($"PARITY: {c.Id} FISCAL LEDGER TOUCHED BY A PREVIEW - days {fiscalDaysBefore[c.Id]}->{daysAfter}, " +
                                       $"terms {fiscalTermSumBefore[c.Id]:F4}->{termSumAfter:F4}. A preview-clone reference escaped into the real country's ledger.");
                        failures++;
                    }
                }
                Debug.Log("PARITY: fiscal ledger - 0 of 5 terms asserted or printed BY DESIGN (every term is daily-accrued on a moving stock; " +
                          "turn-vs-daily agreement is the equivalence bar's question); the real accruing ledger asserted UNTOUCHED across the preview for all 6 countries.");

                sim.AdvanceTurn(decisions);

                foreach (Country c in world.Countries)
                {
                    ApprovalAttribution real = c.ApprovalLedgerLastPeriod;
                    ApprovalAttribution prev = previews[c.Id];
                    if (real == null || prev == null)
                    {
                        Debug.LogError($"PARITY: {c.Id} ledger missing (real={(real != null)}, preview={(prev != null)}) - the recording itself is broken.");
                        failures++;
                        continue;
                    }

                    failures += AssertTerm(c.Id, "Reversion", real.Reversion, prev.Reversion);
                    failures += AssertTerm(c.Id, "TaxHikePenalty", real.TaxHikePenalty, prev.TaxHikePenalty);
                    failures += AssertTerm(c.Id, "SpendingEffect", real.SpendingEffect, prev.SpendingEffect);
                    failures += AssertTerm(c.Id, "WelfareEffect", real.WelfareEffect, prev.WelfareEffect);
                    failures += AssertTerm(c.Id, "PaidLeaveEffect", real.PaidLeaveEffect, prev.PaidLeaveEffect);
                    failures += AssertTerm(c.Id, "DrugPolicyEffect", real.DrugPolicyEffect, prev.DrugPolicyEffect);
                    failures += AssertTerm(c.Id, "GiniEffect", real.GiniEffect, prev.GiniEffect);

                    Debug.Log($"PARITY: {c.Id} expected-different (the preview models the coming period): " +
                              $"Growth {real.GrowthEffect:F4}/{prev.GrowthEffect:F4} · " +
                              $"MiseryU {real.MiseryUnemployment:F4}/{prev.MiseryUnemployment:F4} · " +
                              $"MiseryPi {real.MiseryInflation:F4}/{prev.MiseryInflation:F4} · " +
                              $"MiseryCrime {real.MiseryCrime:F4}/{prev.MiseryCrime:F4} · " +
                              $"MiseryCorr {real.MiseryCorruption:F4}/{prev.MiseryCorruption:F4}");
                }

                Debug.Log(failures == 0
                    ? "PARITY: 7 of 7 asserted terms match for all 6 countries - no clone escape in the covered set."
                    : $"PARITY: {failures} term mismatches - each names the escaped input above.");
                CheckExit.Finish(failures == 0 ? 0 : 1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static int AssertTerm(CountryId id, string name, float real, float preview)
        {
            if (Mathf.Abs(real - preview) <= Tolerance)
            {
                return 0;
            }

            Debug.LogError($"PARITY: {id}.{name} MISMATCH - real {real:F5} vs preview {preview:F5}. " +
                           "A term that reads only unadvanced state and Country constants diverged: " +
                           "a preview-clone input escaped the hand-list. The term name points at its inputs.");
            return 1;
        }
    }
}
