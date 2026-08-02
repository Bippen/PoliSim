using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MECHANISM investigation for the net-creditor runaway, run BEFORE any change — the standing
    /// requirement from Elias's ruling, and the same shape as the debt-floor investigation that preceded
    /// it. Three wrong theories preceded the right one on the batch-run hang; that precedent is why this
    /// exists rather than a patch.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SwfReturnsDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// **The hypothesis under test**, stated up front so a null result is recognisable:
    /// `ApplyRevenueAndSpending` computes
    /// `actualRevenue = theoretical * efficiency * fiscalReactionMultiplier + swfReturns`, adding SWF
    /// returns AFTER the multiplier. If that is the runaway's engine, then for a net creditor we should
    /// see the multiplier pinned at its 0.5 floor while the SWF share of revenue climbs without bound —
    /// the stabiliser working perfectly on the half of revenue it can reach, and not at all on the half
    /// that is growing.
    ///
    /// **Reads only public API.** The multiplier is RECONSTRUCTED from `DebtToGdpRatio` and
    /// `ComfortableDebtToGdpPercent` using the same formula and clamps as `GetFiscalReactionMultiplier`;
    /// the constants are mirrored below and must be kept in step with it.
    /// </summary>
    public static class SwfReturnsDiagnostic
    {
        private const float FiscalReactionSensitivity = 1.5f;   // mirrors SimulationManager
        private const float MinFiscalReactionMultiplier = 0.5f; // mirrors SimulationManager
        private const float MaxFiscalReactionMultiplier = 1.5f; // mirrors SimulationManager
        private const int Turns = 120;
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

        private static float Multiplier(Country c)
        {
            float gap = c.State.DebtToGdpRatio - c.ComfortableDebtToGdpPercent;
            return Mathf.Clamp(1f + FiscalReactionSensitivity * gap / 100f, MinFiscalReactionMultiplier, MaxFiscalReactionMultiplier);
        }

        public static void Run()
        {
            SimulationRandom.Seed(777);
            var go = new GameObject("SwfReturnsDiagnostic");
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

            // SELF-TEST: the multiplier reconstruction must reproduce the documented starting state. Every
            // country seeds AT its own comfortable level, so gap = 0 and the multiplier must be exactly 1.
            foreach (Country c in world.Countries)
            {
                float m = Multiplier(c);
                Debug.Log($"SELFTEST {c.Name,-14} debtToGdp={c.State.DebtToGdpRatio.ToString("F2", Inv)}% " +
                    $"comfortable={c.ComfortableDebtToGdpPercent.ToString("F0", Inv)}% multiplier={m.ToString("F3", Inv)} " +
                    $"-> {(Mathf.Abs(m - 1f) < 0.01f ? "OK (expect 1.000)" : "UNEXPECTED - reconstruction may be wrong")}");
            }

            var pinnedTurns = new Dictionary<CountryId, int>();
            var maxSwfShare = new Dictionary<CountryId, float>();
            var negativeRevenueTurns = new Dictionary<CountryId, int>();
            foreach (Country c in world.Countries) { pinnedTurns[c.Id] = 0; maxSwfShare[c.Id] = 0f; negativeRevenueTurns[c.Id] = 0; }

            Debug.Log("CSV_BEGIN");
            Debug.Log("turn;country;debtToGdp;multiplier;pinnedLow;taxRevenuePostMult;swfReturns;swfSharePct;fundAssets;budgetBalance");

            for (int turn = 1; turn <= Turns; turn++)
            {
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);

                foreach (Country c in world.Countries)
                {
                    FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                    if (r == null) { continue; }

                    float m = Multiplier(c);
                    bool pinned = m <= MinFiscalReactionMultiplier + 0.0001f;
                    if (pinned) { pinnedTurns[c.Id]++; }

                    // report.Revenue is actualRevenue, which ALREADY INCLUDES swfReturns - so the
                    // multiplier-governed part is the remainder. That decomposition is the whole point.
                    float swf = r.SwfReturns;
                    float taxPart = r.Revenue - swf;

                    // Measured against the TAX PART, not against total revenue. Dividing by total revenue
                    // is ambiguous by construction: SWF returns are signed, so a large market LOSS drags
                    // total revenue negative and a negative/negative ratio reads as a huge positive
                    // "share". This ratio answers the question that actually matters - how large is the
                    // component the stabiliser CANNOT reach, relative to the component it can.
                    //
                    // ⚠ THIS DECOMPOSITION IS ONLY MEANINGFUL PRE-FIX, and the tool is kept for the
                    // before/after comparison rather than as a live metric. Since SWF returns moved
                    // INSIDE the multiplier, `Revenue - SwfReturns` is no longer the tax component: it is
                    // `tax*m + swf*(m-1)`, which goes negative whenever swf exceeds tax at m = 0.5. A
                    // negative "taxPart" in a post-fix run is an artefact of this arithmetic, NOT a model
                    // defect - do not report it as one.
                    float share = Mathf.Abs(taxPart) > 0.001f ? Mathf.Abs(swf) / taxPart * 100f : 0f;
                    if (share > maxSwfShare[c.Id]) { maxSwfShare[c.Id] = share; }
                    if (r.Revenue < 0f) { negativeRevenueTurns[c.Id]++; }

                    float assets = c.SovereignWealthFund != null ? c.SovereignWealthFund.TotalAssets : 0f;

                    if (turn % 10 == 0 || turn <= 3)
                    {
                        Debug.Log(string.Join(";", new[]
                        {
                            turn.ToString(Inv), c.Name,
                            c.State.DebtToGdpRatio.ToString("F2", Inv),
                            m.ToString("F3", Inv),
                            pinned ? "PINNED" : "-",
                            taxPart.ToString("F2", Inv),
                            swf.ToString("F2", Inv),
                            share.ToString("F1", Inv),
                            assets.ToString("F2", Inv),
                            r.BudgetBalance.ToString("F2", Inv)
                        }));
                    }
                }
            }
            Debug.Log("CSV_END");

            Debug.Log("=== MECHANISM SUMMARY ===");
            foreach (Country c in world.Countries)
            {
                Debug.Log($"MECH {c.Name,-14} multiplierPinnedLow={pinnedTurns[c.Id],4}/{Turns}  " +
                    $"peak|swf|/taxPart={maxSwfShare[c.Id].ToString("F0", Inv),6}%  " +
                    $"negativeRevenueTurns={negativeRevenueTurns[c.Id],4}  " +
                    $"finalDebtToGdp={c.State.DebtToGdpRatio.ToString("F2", Inv),9}%");
            }

            EditorApplication.Exit(0);
        }
    }
}
