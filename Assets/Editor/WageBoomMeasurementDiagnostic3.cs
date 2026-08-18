using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Round 3: testing the REFRAMED premise after rounds 1-2 falsified the original one (a
    /// one-shot tightness impulse self-corrects within ~5 turns via Okun's 0.7/turn reversion,
    /// and the only long-lived effect only shows up past 100+ turns, dominated by random
    /// EventSystem shocks rather than the boom mechanism). The reframe: rather than an INHERITED
    /// boom that fades on its own, can the PLAYER actively SUSTAIN tightness against Okun's own
    /// pull - which requires fighting a 0.7/turn reversion every turn - while keeping inflation
    /// under control? That is a genuine two-sided tension IF sustaining is actually hard and IF
    /// the inflation cost is real, both measured here rather than assumed.
    /// </summary>
    public static class WageBoomMeasurementDiagnostic3
    {
        [MenuItem("PoliSim/Run Wage Boom Measurement 3 (sustain against reversion)")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            Debug.Log("WAGEBOOM3: --- §7 can a rate CUT sustain tightness against Okun's reversion? Sweden, 25 turns ---");
            RunSustainTest(CountryId.Sweden, startImpulsePp: 2f, rateCutPp: 0f, turns: 25, label: "sweden_nocut");
            RunSustainTest(CountryId.Sweden, startImpulsePp: 2f, rateCutPp: -1f, turns: 25, label: "sweden_cut1");
            RunSustainTest(CountryId.Sweden, startImpulsePp: 2f, rateCutPp: -1.75f, turns: 25, label: "sweden_cut1.75_to_floor");

            Debug.Log("WAGEBOOM3: --- §8 spending stimulus (the other lever that feeds the growth gap directly) ---");
            RunSustainTestSpending(CountryId.Sweden, startImpulsePp: 2f, spendingBoostPercent: 15f, turns: 25, label: "sweden_spend15pct");

            CheckExit.Finish(0);
        }

        /// <summary>Holds a rate cut for the WHOLE run (applied once at t1 - Sweden has no rate
        /// reversion, so a single change persists) and reports, each turn, how many consecutive
        /// turns U has stayed at least 1pp below NAIRU (the Sustained candidate condition) plus
        /// inflation.</summary>
        private static void RunSustainTest(CountryId id, float startImpulsePp, float rateCutPp, int turns, string label)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"WAGEBOOM3_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                c.State.Unemployment = Mathf.Max(0f, c.NaturalUnemploymentRate - startImpulsePp);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                int consecutiveBelow1pp = 0;
                int maxConsecutive = 0;

                for (int turn = 1; turn <= turns; turn++)
                {
                    decisions[id] = (turn == 1 && rateCutPp != 0f)
                        ? new PolicyDecision { InterestRateChange = rateCutPp }
                        : PolicyDecision.None();

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    float gap = c.NaturalUnemploymentRate - c.State.Unemployment;
                    consecutiveBelow1pp = gap >= 1f ? consecutiveBelow1pp + 1 : 0;
                    maxConsecutive = Mathf.Max(maxConsecutive, consecutiveBelow1pp);

                    EconomyState s = c.State;
                    Debug.Log($"WAGEBOOM3[{label}] t{turn}: U={s.Unemployment:F3} gap={gap:+0.000;-0.000} " +
                              $"streak(>=1pp)={consecutiveBelow1pp} π={s.Inflation:F3} rate={c.CurrencyZone.InterestRate:F3} " +
                              $"Approval={s.ApprovalRating:F2}");
                }

                Debug.Log($"WAGEBOOM3[{label}]: FINAL max consecutive turns with gap>=1pp = {maxConsecutive}");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Same measurement, spending-stimulus lever instead of a rate cut - a sustained
        /// percentage increase across the discretionary categories, applied EVERY turn (spending
        /// changes are per-turn deltas, not persistent, unlike the interest rate).</summary>
        private static void RunSustainTestSpending(CountryId id, float startImpulsePp, float spendingBoostPercent, int turns, string label)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"WAGEBOOM3_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                c.State.Unemployment = Mathf.Max(0f, c.NaturalUnemploymentRate - startImpulsePp);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                int consecutiveBelow1pp = 0;
                int maxConsecutive = 0;

                for (int turn = 1; turn <= turns; turn++)
                {
                    var boosted = PolicyDecision.None();
                    boosted.InfrastructureSpendingChange = spendingBoostPercent;
                    decisions[id] = boosted;

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    float gap = c.NaturalUnemploymentRate - c.State.Unemployment;
                    consecutiveBelow1pp = gap >= 1f ? consecutiveBelow1pp + 1 : 0;
                    maxConsecutive = Mathf.Max(maxConsecutive, consecutiveBelow1pp);

                    EconomyState s = c.State;
                    Debug.Log($"WAGEBOOM3[{label}] t{turn}: U={s.Unemployment:F3} gap={gap:+0.000;-0.000} " +
                              $"streak(>=1pp)={consecutiveBelow1pp} π={s.Inflation:F3} Budget={s.Budget:F1} " +
                              $"Approval={s.ApprovalRating:F2}");
                }

                Debug.Log($"WAGEBOOM3[{label}]: FINAL max consecutive turns with gap>=1pp = {maxConsecutive}");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
