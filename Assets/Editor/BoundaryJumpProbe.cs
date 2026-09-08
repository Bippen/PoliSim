using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §403 (FT-9): the boundary's GDP jump decomposed by step. A probe, nothing asserted: the no-policy world (no player), seed 777, 300 turns, subscribed to
    /// SimulationManager.BoundaryLedger; at every checkpoint of every country's boundary it reads GDP and attributes the change since the previous checkpoint
    /// to the step just passed - "open" (the cohort commit, the zone rate, currency, tariffs, trade), "policy", "labour", "spending", "families", "close",
    /// "events" - and the tail after "events" to the rest of the boundary. Per country over years 6-300: the mean jump per step in percent of the GDP the
    /// last day left, the share of the total, and how many years each step moved GDP at all. The ruling applies to whatever carries the jump.
    /// </summary>
    public static class BoundaryJumpProbe
    {
        private const int Turns = 300;
        private const int Skip = 5;
        private static readonly string[] Steps = { "open", "policy", "labour", "spending", "families", "close", "events", "tail" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("BOUNDARY");
            var sb = new StringBuilder();
            var lastGdp = new Dictionary<CountryId, float>();
            var yearSum = new Dictionary<CountryId, Dictionary<string, double>>();
            var yearMoved = new Dictionary<CountryId, Dictionary<string, int>>();
            var gdpBeforeBoundary = new Dictionary<CountryId, float>();
            int year = 0;
            foreach (Country c in world.Countries)
            {
                yearSum[c.Id] = new Dictionary<string, double>(); yearMoved[c.Id] = new Dictionary<string, int>();
                foreach (string s in Steps) { yearSum[c.Id][s] = 0; yearMoved[c.Id][s] = 0; }
            }
            System.Action<Country, string> ledger = (c, step) =>
            {
                if (year <= Skip) { lastGdp[c.Id] = c.State.GDP; return; }
                float before = lastGdp[c.Id];
                float pct = before > 0f ? (c.State.GDP / before - 1f) * 100f : 0f;
                yearSum[c.Id][step] += pct; if (Mathf.Abs(pct) > 1e-6f) { yearMoved[c.Id][step]++; }
                lastGdp[c.Id] = c.State.GDP;
            };
            SimulationManager.BoundaryLedger += ledger;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    foreach (Country c in world.Countries) { gdpBeforeBoundary[c.Id] = c.State.GDP; lastGdp[c.Id] = c.State.GDP; }
                    sim.AdvanceTurn(decisions);
                    if (year > Skip)
                    {
                        foreach (Country c in world.Countries)
                        {
                            float before = lastGdp[c.Id];
                            float pct = before > 0f ? (c.State.GDP / before - 1f) * 100f : 0f;   // whatever moved GDP after the last checkpoint
                            yearSum[c.Id]["tail"] += pct; if (Mathf.Abs(pct) > 1e-6f) { yearMoved[c.Id]["tail"]++; }
                        }
                    }
                }
                int n = Turns - Skip;
                sb.Append("BOUNDARY: mean jump per step, % of the GDP the last day left, years 6-300, seed 777, no player (years the step moved GDP at all in brackets)\n");
                foreach (Country c in world.Countries)
                {
                    double total = 0; foreach (string s in Steps) { total += yearSum[c.Id][s]; }
                    sb.Append($"BOUNDARY: {c.Id,-8} total {total / n:+0.0000} %:");
                    foreach (string s in Steps) { sb.Append($"  {s} {yearSum[c.Id][s] / n:+0.0000} [{yearMoved[c.Id][s]}]"); }
                    sb.Append('\n');
                }
                sb.Append("BOUNDARY: a step's mean is the sum over years of its percentage move divided by the years; the total is the boundary's jump as OkunAsymmetryProbe reads it, to the rounding of chained percentages.\n");
                Debug.Log(sb.ToString());
                Debug.Log("BOUNDARY: done (a probe - nothing asserted).");
                CheckExit.Finish(0);
            }
            finally { SimulationManager.BoundaryLedger -= ledger; Object.DestroyImmediate(go); }
        }
    }
}
