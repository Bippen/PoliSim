using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// TEMPORARY investigation tool for the debt-to-zero bimodality. Not production code, and not part of
    /// any validation gate — it exists to CONFIRM A MECHANISM before anything is implemented, because
    /// three wrong theories preceded the right one on the Unity batch-run hang.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.DebtClampDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// **Reads only public API** — no production code is instrumented. The unclamped debt is
    /// RECONSTRUCTED rather than observed: `ApplyRevenueAndSpending` computes
    /// `Clamp(GovernmentDebt - budgetBalance, 0, maxDebt)`, so with the previous turn's debt and this
    /// turn's `FiscalTurnReport.BudgetBalance` the pre-clamp value is exactly `prevDebt - budgetBalance`.
    /// Comparing that to the debt actually stored tells us, per turn, whether a bound bound.
    /// </summary>
    public static class DebtClampDiagnostic
    {
        private const float MaxDebtToGdpPercent = 300f;   // mirrors SimulationManager's own constant
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        private const int Turns = 120;

        public static void Run()
        {
            SimulationRandom.Seed(777);

            var go = new GameObject("DebtClampDiagnostic");
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            var prevDebt = new Dictionary<CountryId, float>();
            var floorHits = new Dictionary<CountryId, int>();
            var ceilingHits = new Dictionary<CountryId, int>();
            var netCreditorTurns = new Dictionary<CountryId, int>();
            foreach (Country c in world.Countries)
            {
                decisions[c.Id] = PolicyDecision.None();
                prevDebt[c.Id] = c.State.GovernmentDebt;
                floorHits[c.Id] = 0; ceilingHits[c.Id] = 0; netCreditorTurns[c.Id] = 0;
            }

            // SELF-TEST FIRST, per the standing rule: print seeded starting state so a broken parse or a
            // broken run is distinguishable from a real finding AT READ TIME. Sweden seeds at 35% debt
            // and Germany at 63%; if these do not read that way, everything below is void.
            foreach (Country c in world.Countries)
            {
                Debug.Log($"SELFTEST seed {c.Name,-14} debtToGdp={c.State.DebtToGdpRatio.ToString("F2", Inv)}% " +
                    $"debt={c.State.GovernmentDebt.ToString("F2", Inv)} gdp={c.State.GDP.ToString("F2", Inv)}");
            }

            var sb = new StringBuilder();
            sb.AppendLine("turn;country;gdp;debt;debtToGdp;budgetBalance;unclampedDebt;clampedBy;swfAssets;netPosition");

            for (int turn = 1; turn <= Turns; turn++)
            {
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);

                foreach (Country c in world.Countries)
                {
                    FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                    float bb = r != null ? r.BudgetBalance : 0f;
                    float unclamped = prevDebt[c.Id] - bb;
                    float maxDebt = MaxDebtToGdpPercent / 100f * c.State.GDP;

                    string clampedBy = "none";
                    if (unclamped < 0f) { clampedBy = "FLOOR"; floorHits[c.Id]++; }
                    else if (unclamped > maxDebt) { clampedBy = "CEILING"; ceilingHits[c.Id]++; }

                    float swf = c.SovereignWealthFund != null ? c.SovereignWealthFund.TotalAssets : 0f;
                    float net = c.State.GovernmentDebt - swf;
                    if (net < 0f) { netCreditorTurns[c.Id]++; }

                    // InvariantCulture and a SEMICOLON separator, both deliberate. The first version of
                    // this file used "{x:F2}" and commas; under a Swedish locale that writes decimal
                    // COMMAS into a comma-separated file, so every row split into the wrong number of
                    // fields and the parse produced Germany at 4644% debt. The integer summary counts
                    // were unaffected, which is exactly how a half-broken output misleads.
                    sb.AppendLine(string.Join(";", new[]
                    {
                        turn.ToString(Inv),
                        c.Name,
                        c.State.GDP.ToString("F2", Inv),
                        c.State.GovernmentDebt.ToString("F2", Inv),
                        c.State.DebtToGdpRatio.ToString("F2", Inv),
                        bb.ToString("F2", Inv),
                        unclamped.ToString("F2", Inv),
                        clampedBy,
                        swf.ToString("F2", Inv),
                        net.ToString("F2", Inv)
                    }));

                    prevDebt[c.Id] = c.State.GovernmentDebt;
                }
            }

            Debug.Log("=== DEBT CLAMP DIAGNOSTIC (seed 777, " + Turns + " turns) ===");
            Debug.Log("CSV_BEGIN\n" + sb.ToString() + "CSV_END");

            Debug.Log("=== per-country clamp summary ===");
            foreach (Country c in world.Countries)
            {
                Debug.Log($"SUMMARY {c.Name,-8} floorHits={floorHits[c.Id],4}/{Turns}  ceilingHits={ceilingHits[c.Id],4}  " +
                    $"netCreditorTurns={netCreditorTurns[c.Id],4}  finalDebtToGdp={c.State.DebtToGdpRatio:F2}%");
            }

            EditorApplication.Exit(0);
        }
    }
}
