using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PASS 5's third-writer closure, checked through the REAL path (2026-08-26). Before pass 5 a
    /// passed SWF emergency drawdown debited the fund and wrote the Budget display accumulator only -
    /// the debt stock never saw it. Now it routes through ApplyOneTimeBudgetImpact and the resolution
    /// site records the event on the debt ledger. Neither the no-policy dumps nor the round-trip
    /// harness resolve a drawdown (the round-trip only snapshots the pending bill's days), so this
    /// harness is the one that does: it introduces a drawdown bill for a country with a fund, lets
    /// the real 21-day resolution run through SimulationManager's own site, asserts the fund, the
    /// stock and the accumulator all moved by the delivered withdrawal, and then runs a full period
    /// to the next boundary so the debt ledger's self-audit runs against the stock that moved off-path
    /// - an unrecorded writer would raise ATTRIB there and the log fold would exit nonzero.
    ///
    /// The house is pinned to pass expansionary bills (every seat to the most expansionary
    /// archetype) so the vote is not the variable under test; the bill's direction is its own
    /// withdrawal, positive.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SwfDrawdownBooksDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class SwfDrawdownBooksDiagnostic
    {
        [MenuItem("PoliSim/Run SWF Drawdown Books Check (pass 5)")]
        private static void RunFromMenu() => Run();

        private const int Seed = 777;
        private const float WithdrawalPercentOfGdp = 2f;
        private const float Tolerance = 1e-3f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SwfDrawdownBooksDiagnostic");
            bool ok = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country sweden = world.GetCountry(CountryId.Sweden);

                if (sweden.SovereignWealthFund == null)
                {
                    // Sweden seeds with a fund in the current WorldFactory; if that ever changes, seed a
                    // diagnostic fund of a fifth of GDP so the check still has something to draw on.
                    sweden.SovereignWealthFund = new SovereignWealthFund { ContributionRatePercent = 0f, EquitiesWeight = 100f };
                    sweden.SovereignWealthFund.TotalAssets = 0.2f * sweden.State.GDP;
                    Debug.Log("DRAWDOWN: Sweden had no fund at seed - a diagnostic fund of 20% of GDP was created.");
                }

                // Pin the house so the vote is not the variable under test.
                PartyArchetype mostExpansionary = PartyArchetypeData.AllArchetypes[0];
                foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
                {
                    if (PartyArchetypeData.GetFiscalStance(archetype) > PartyArchetypeData.GetFiscalStance(mostExpansionary)) { mostExpansionary = archetype; }
                }
                foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes) { sweden.ParliamentSeats[archetype] = 0; }
                sweden.ParliamentSeats[mostExpansionary] = ParliamentConstants.TotalSeats;

                var bill = new SwfDrawdownBill { WithdrawalPercentOfGdp = WithdrawalPercentOfGdp };
                bool wouldPass = ParliamentSystem.WouldBillPass(sweden, ParliamentSystem.GetSwfDrawdownBillDirection(sweden, bill));
                Debug.Log($"DRAWDOWN: house pinned to {mostExpansionary} ({ParliamentConstants.TotalSeats} seats); a {WithdrawalPercentOfGdp:F1}%-of-GDP drawdown would pass: {wouldPass}");
                ok &= wouldPass;

                float fundBefore = sweden.SovereignWealthFund.TotalAssets;
                float debtBefore = sweden.State.GovernmentDebt;
                float budgetBefore = sweden.State.Budget;
                float expectedWithdrawal = Mathf.Min(sweden.State.GDP * WithdrawalPercentOfGdp / 100f, fundBefore);

                bool introduced = sim.IntroduceSwfDrawdownBill(CountryId.Sweden, bill);
                ok &= introduced;
                for (int day = 0; day < ParliamentSystem.BillDurationDays && sim.GetPendingSwfDrawdownBill(CountryId.Sweden) != null; day++)
                {
                    sim.AdvanceSwfDrawdownBillDay(CountryId.Sweden);
                }

                bool resolved = sim.GetPendingSwfDrawdownBill(CountryId.Sweden) == null;
                float fundDelta = sweden.SovereignWealthFund.TotalAssets - fundBefore;
                float debtDelta = sweden.State.GovernmentDebt - debtBefore;
                float budgetDelta = sweden.State.Budget - budgetBefore;
                bool fundOk = Mathf.Abs(fundDelta + expectedWithdrawal) < Tolerance;
                bool debtOk = Mathf.Abs(debtDelta + expectedWithdrawal) < Tolerance;
                bool budgetOk = Mathf.Abs(budgetDelta - expectedWithdrawal) < Tolerance;
                ok &= resolved && fundOk && debtOk && budgetOk;
                Debug.Log($"DRAWDOWN: introduced {introduced}, resolved {resolved}; delivered {expectedWithdrawal:F3} | fund {fundBefore:F3} -> {sweden.SovereignWealthFund.TotalAssets:F3} ({(fundOk ? "OK" : "WRONG")}) | " +
                          $"debt {debtBefore:F3} -> {sweden.State.GovernmentDebt:F3} ({(debtOk ? "OK - the stock moved" : "WRONG - the stock did not move by the withdrawal")}) | " +
                          $"Budget {budgetBefore:F3} -> {sweden.State.Budget:F3} ({(budgetOk ? "OK - a true reading" : "WRONG")})");

                // A full period to the next boundary: the debt ledger closes and audits the stock it
                // observed against the terms and events it recorded. The recorded drawdown event is what
                // keeps this green; the log fold turns an ATTRIB into a nonzero exit.
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                Debug.Log("DRAWDOWN: the boundary after the drawdown closed - if the ledger's audit had failed, an ATTRIB line would sit above this one and the exit would fold.");
                Debug.Log(ok ? "DRAWDOWN: PASS - the drawdown reaches the fund, the stock and the accumulator through one path, and the ledger observed it." : "DRAWDOWN: FAIL - see the lines above.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
