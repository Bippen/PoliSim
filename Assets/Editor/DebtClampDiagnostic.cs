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
    ///
    /// **REPURPOSED 2026-08-02, when the floor was removed.** It confirmed the mechanism, the fix landed,
    /// and it now measures the fix instead of the bug. The `FLOOR` column became `NEGATIVE`: the same
    /// arithmetic, but where it once meant "this turn was clamped" it now means "this turn the country
    /// was a net debtor no longer" — an outcome, not an artefact. The column is kept rather than deleted
    /// so a before/after comparison against the pre-fix CSVs stays possible.
    ///
    /// It also now answers the question the roadmap gated the fix's SUCCESS on, which the mechanism
    /// confirmation could not: **does removing the floor eliminate the rating thrash, or was the floor
    /// hiding budget-balance volatility that will still clear the notch threshold?** The year-over-year
    /// debt-ratio deltas below are that measurement.
    /// </summary>
    public static class DebtClampDiagnostic
    {
        private const float MaxDebtToGdpPercent = 300f;   // mirrors SimulationManager's own constant
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        private const int Turns = 120;

        /// <summary>
        /// The rating notch a given debt ratio implies on its own, through the SAME curve the game uses -
        /// deficit and growth terms omitted deliberately, because this isolates the debt stock's own
        /// contribution, which is the thing the floor was distorting.
        /// </summary>
        private static int NotchFor(float debtToGdp, float riskPremiumSensitivity)
        {
            return (int)CreditRatingSystem.EvaluateFrom(debtToGdp, riskPremiumSensitivity, null, null).Rating;
        }

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
            var debtRatioByTurn = new Dictionary<CountryId, List<float>>();
            foreach (Country c in world.Countries)
            {
                decisions[c.Id] = PolicyDecision.None();
                prevDebt[c.Id] = c.State.GovernmentDebt;
                floorHits[c.Id] = 0; ceilingHits[c.Id] = 0; netCreditorTurns[c.Id] = 0;
                debtRatioByTurn[c.Id] = new List<float>();
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

                    // "NEGATIVE" no longer means clamped - the floor is gone, so this is simply a turn on
                    // which the country's debt stock went below zero and was ALLOWED to.
                    string clampedBy = "none";
                    if (unclamped < 0f) { clampedBy = "NEGATIVE"; floorHits[c.Id]++; }
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

                    debtRatioByTurn[c.Id].Add(c.State.DebtToGdpRatio);
                    prevDebt[c.Id] = c.State.GovernmentDebt;
                }
            }

            Debug.Log("=== DEBT CLAMP DIAGNOSTIC (seed 777, " + Turns + " turns) ===");
            Debug.Log("CSV_BEGIN\n" + sb.ToString() + "CSV_END");

            Debug.Log("=== per-country clamp summary ===");
            foreach (Country c in world.Countries)
            {
                Debug.Log($"SUMMARY {c.Name,-8} negativeTurns={floorHits[c.Id],4}/{Turns}  ceilingHits={ceilingHits[c.Id],4}  " +
                    $"netCreditorTurns={netCreditorTurns[c.Id],4}  finalDebtToGdp={c.State.DebtToGdpRatio:F2}%");
            }

            // THE QUESTION THE FIX IS JUDGED ON, and it is not "did the floor stop binding" - that is
            // guaranteed by deleting the clamp and would be a vacuous pass. It is whether the debt stock
            // still moves enough YEAR OVER YEAR to shift a credit rating, because if it does, the floor
            // was hiding budget-balance volatility that is a separate defect.
            //
            // A year is three turns (121 days each). The rating is compared as NOTCHES rather than as a
            // debt percentage, because a notch is what a player sees: 8 points of debt matters enormously
            // at 60% and barely at 250%, and only the real curve knows that. Reusing
            // CreditRatingSystem.EvaluateFrom means this cannot drift from the rating the game shows.
            Debug.Log("=== year-over-year rating movement (the thrash test) ===");
            const int turnsPerYear = 3;
            foreach (Country c in world.Countries)
            {
                List<float> ratios = debtRatioByTurn[c.Id];
                int notchMoves = 0, biggestMove = 0;
                float biggestRatioSwing = 0f;
                for (int t = turnsPerYear; t < ratios.Count; t++)
                {
                    int before = NotchFor(ratios[t - turnsPerYear], c.RiskPremiumSensitivity);
                    int after = NotchFor(ratios[t], c.RiskPremiumSensitivity);
                    int move = Mathf.Abs(after - before);
                    if (move > 0) { notchMoves++; }
                    if (move > biggestMove) { biggestMove = move; }
                    biggestRatioSwing = Mathf.Max(biggestRatioSwing, Mathf.Abs(ratios[t] - ratios[t - turnsPerYear]));
                }
                Debug.Log($"THRASH {c.Name,-8} yearsWithANotchMove={notchMoves,4}/{ratios.Count - turnsPerYear}  " +
                    $"largestMove={biggestMove} notches  largestYoYSwing={biggestRatioSwing:F1} pts of GDP");
            }

            EditorApplication.Exit(0);
        }
    }
}
