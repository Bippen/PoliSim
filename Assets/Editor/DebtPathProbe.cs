using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-2's second measurement (2026-09-07): what the AI's line-growth rule does to the fiscal path. Six countries at seed policy (Sweden the player) for a
    /// century; at years 1, 25, 50, 75 and 100 per country: the discretionary lines over nominal GDP (G's share), the balance over nominal GDP, debt over
    /// nominal GDP, and real GDP over its seed. Run once on the rule as it stands and once with the rule's real-growth factor held at 1 (an edit-and-restore
    /// probe), so the alternative the rule's own doc comment warns about - AI budgets drifting below their economies - is a measured path, not a sentence.
    /// A probe: prints, asserts nothing, in no bar.
    /// </summary>
    public static class DebtPathProbe
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("DEBTPATH");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var seedGdp = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries) { seedGdp[c.Id] = c.State.GDP; }
                var sb = new StringBuilder();
                sb.Append("DEBTPATH: year  country   lines/nominalGDP  balance/nominalGDP  debt/nominalGDP  realGDP/seed  unemployment\n");
                for (int year = 1; year <= 100; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (year != 1 && year % 25 != 0) { continue; }
                    foreach (Country c in world.Countries)
                    {
                        float ngdp = Mathf.Max(0.0001f, c.State.NominalGdp);
                        float lines = 0f;
                        foreach (SpendingLine line in c.SpendingLines) { lines += line.Amount; }
                        sb.Append($"DEBTPATH: {year,4}  {c.Id,-8}  {lines / ngdp,16:F4}  {c.State.Budget / ngdp,18:F4}  {c.State.GovernmentDebt / ngdp,15:F4}  {c.State.GDP / Mathf.Max(0.0001f, seedGdp[c.Id]),12:F3}  {c.State.Unemployment,12:F2}\n");
                    }
                }
                Debug.Log(sb.ToString());
                Debug.Log("DEBTPATH: done (a probe - nothing asserted).");
            }
            finally { Object.DestroyImmediate(go); }
            CheckExit.Finish(0);
        }
    }
}
