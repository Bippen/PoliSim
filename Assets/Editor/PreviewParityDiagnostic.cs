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

                var previews = new Dictionary<CountryId, ApprovalAttribution>();
                foreach (Country c in world.Countries)
                {
                    previews[c.Id] = sim.PreviewTurn(c.Id, PolicyDecision.None()).ApprovalTerms;
                }

                sim.AdvanceTurn(decisions);

                int failures = 0;
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
