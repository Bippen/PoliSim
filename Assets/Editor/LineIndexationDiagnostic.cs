using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-2 (2026-09-07, §370): the AI's line rule bound to B2's drivers. Sweden the player, twenty years at seed policy, every country read: (1) for an AI
    /// country, a line WITH a driver reads real amount ÷ (seed × driver ratio) = 1 to 1e-3 - the caseload's real cost per head holds; (2) a line WITHOUT a
    /// driver reads the compounded real potential growth, Π(1 + g), the same for every driverless line of that country and above 1 where potential grew;
    /// (3) the player's lines read 1 on both classes (prices × driver only), as before; (4) both directions on the driverless class: a country whose potential
    /// shrank (Italy) reads below 1. Proves the split and nothing else; the fiscal path it produces is the trajectory suite's to explain.
    /// </summary>
    public static class LineIndexationDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LINEINDEX");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var seedAmount = new Dictionary<(CountryId, SpendingCategory), float>();
                var seedDriver = new Dictionary<(CountryId, SpendingCategory), float>();
                var growth = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    growth[c.Id] = 1f;
                    foreach (SpendingLine line in c.SpendingLines) { seedAmount[(c.Id, line.Category)] = line.Amount; seedDriver[(c.Id, line.Category)] = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)); }
                }
                for (int year = 1; year <= Years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // the rule reads PotentialGrowthRate as it stands when the lines are indexed at the top of the turn - accumulate the same figure here
                    foreach (Country c in world.Countries) { growth[c.Id] *= 1f + c.PotentialGrowthRate / 100f; }
                    sim.AdvanceTurn(decisions);
                }
                var report = new List<string>();
                bool sawShrink = false;
                foreach (Country c in world.Countries)
                {
                    bool player = c.Id == CountryId.Sweden;
                    float price = Mathf.Max(0.0001f, c.State.PriceLevel);
                    int withDriver = 0, without = 0; float driverlessRatio = float.NaN;
                    foreach (SpendingLine line in c.SpendingLines)
                    {
                        float seed = seedAmount[(c.Id, line.Category)];
                        if (seed <= 0f || line.Pinned) { continue; }
                        float real = line.Amount / seed / price;
                        float driverRatio = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)) / seedDriver[(c.Id, line.Category)];
                        float residual = real / driverRatio;
                        bool driverless = SpendingDrivers.Of(line.Category) == SpendingDriver.None;
                        if (driverless)
                        {
                            if (float.IsNaN(driverlessRatio)) { driverlessRatio = residual; }
                            else if (Mathf.Abs(residual - driverlessRatio) > 1e-3f * Mathf.Max(1f, driverlessRatio)) { Debug.LogError($"LINE INDEXATION: {c.Id}'s driverless lines do not carry one factor ({line.Category} {residual:F4} against {driverlessRatio:F4})."); ok = false; }
                            without++;
                        }
                        else
                        {
                            if (Mathf.Abs(residual - 1f) > 1e-3f) { Debug.LogError($"LINE INDEXATION: {c.Id}'s {line.Category} carries a driver ({SpendingDrivers.Of(line.Category)}) and still reads {residual:F4} after prices and the driver - the caseload's real cost per head moved."); ok = false; }
                            withDriver++;
                        }
                    }
                    if (player)
                    {
                        if (!float.IsNaN(driverlessRatio) && Mathf.Abs(driverlessRatio - 1f) > 1e-3f) { Debug.LogError($"LINE INDEXATION: the player's driverless lines read {driverlessRatio:F4}, not 1 - the player's rule moved."); ok = false; }
                    }
                    else if (!float.IsNaN(driverlessRatio))
                    {
                        // the rule's own figure: the potential growth compounded over the twenty indexations (the seed year's indexation uses the seed's rate)
                        if (Mathf.Abs(driverlessRatio - growth[c.Id]) > 0.02f * growth[c.Id]) { Debug.LogError($"LINE INDEXATION: {c.Id}'s driverless factor {driverlessRatio:F4} is not the compounded potential growth {growth[c.Id]:F4}."); ok = false; }
                        if (driverlessRatio < 1f) { sawShrink = true; }
                    }
                    report.Add($"{c.Id}{(player ? " (player)" : "")}: {withDriver} driver-bearing line(s) at 1.000, {without} driverless at {(float.IsNaN(driverlessRatio) ? 1f : driverlessRatio):F3}");
                }
                if (!sawShrink) { Debug.LogError("LINE INDEXATION: no AI country's driverless factor sits below 1 - the shrinking direction (a negative potential) was not exercised."); ok = false; }
                Debug.Log($"LINE INDEXATION: after {Years} years - {string.Join("; ", report)}. A caseload line holds its real cost per head for the AI as for the player; a driverless line carries the country's compounded potential growth, above 1 where it grew and below where it shrank.");
            }
            finally { Object.DestroyImmediate(go); }
            Debug.Log(ok ? "LINE INDEXATION: PASS - caseload lines to their drivers, driverless lines to potential growth, the player unchanged, both directions." : "LINE INDEXATION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
