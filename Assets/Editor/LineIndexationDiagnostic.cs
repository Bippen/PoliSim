using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-2 re-formed (2026-09-07, §384; the driver-only form of §370 reverted). Sweden the player, twenty years at seed policy, every country read: (1) for an
    /// AI country, a line WITH a driver reads real amount ÷ (seed × driver ratio) = the country's compounded REAL WAGE growth (the real wage index at the last
    /// indexation over its seed) to 2 % - a caseload at the going wage, the income index's rule; (2) a line WITHOUT a driver reads the compounded real potential
    /// growth, Π(1 + g), the same for every driverless line of that country; (3) the player's lines read 1 on both classes (prices × driver only); (4) both
    /// directions on the driverless class: a country whose potential shrank (Italy) reads below 1. Proves the split; the fiscal path is the trajectory suite's.
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
                var wage = new Dictionary<CountryId, float>();
                var wageSeed = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    growth[c.Id] = 1f; wage[c.Id] = 1f; wageSeed[c.Id] = Mathf.Max(0.0001f, c.State.RealWageIndex);
                    foreach (SpendingLine line in c.SpendingLines) { seedAmount[(c.Id, line.Category)] = line.Amount; seedDriver[(c.Id, line.Category)] = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)); }
                }
                for (int year = 1; year <= Years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // the rule reads PotentialGrowthRate and the real wage index as they stand when the lines are indexed at the top of the turn - the same figures here
                    foreach (Country c in world.Countries) { growth[c.Id] *= 1f + c.PotentialGrowthRate / 100f; wage[c.Id] = c.State.RealWageIndex / wageSeed[c.Id]; }
                    sim.AdvanceTurn(decisions);
                }
                var report = new List<string>();
                bool sawShrink = false;
                foreach (Country c in world.Countries)
                {
                    bool player = c.Id == CountryId.Sweden;
                    float price = Mathf.Max(0.0001f, c.State.PriceLevel);
                    int withDriver = 0, without = 0; float driverlessRatio = float.NaN, caseloadRatio = float.NaN;
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
                            else if (Mathf.Abs(residual - driverlessRatio) > 1e-3f * Mathf.Max(1f, driverlessRatio)) { Debug.LogError($"LINE INDEXATION: {c.Id} driverless lines do not carry one factor ({line.Category} {residual:F4} vs {driverlessRatio:F4})."); ok = false; }
                            without++;
                        }
                        else
                        {
                            if (float.IsNaN(caseloadRatio)) { caseloadRatio = residual; }
                            else if (Mathf.Abs(residual - caseloadRatio) > 1e-3f * Mathf.Max(1f, caseloadRatio)) { Debug.LogError($"LINE INDEXATION: {c.Id} caseload lines do not carry one factor ({line.Category} {residual:F4} vs {caseloadRatio:F4})."); ok = false; }
                            withDriver++;
                        }
                    }
                    if (player)
                    {
                        if (!float.IsNaN(driverlessRatio) && Mathf.Abs(driverlessRatio - 1f) > 1e-3f) { Debug.LogError($"LINE INDEXATION: the player's driverless lines read {driverlessRatio:F4}, not 1 - the player's rule moved."); ok = false; }
                        if (!float.IsNaN(caseloadRatio) && Mathf.Abs(caseloadRatio - 1f) > 1e-3f) { Debug.LogError($"LINE INDEXATION: the player's caseload lines read {caseloadRatio:F4}, not 1 - the player's rule moved."); ok = false; }
                    }
                    else
                    {
                        // the rule's own figures: potential growth compounded over the twenty indexations, and the real wage index at the last indexation over its seed
                        if (!float.IsNaN(driverlessRatio) && Mathf.Abs(driverlessRatio - growth[c.Id]) > 0.02f * growth[c.Id]) { Debug.LogError($"LINE INDEXATION: {c.Id} driverless factor {driverlessRatio:F4} is not the compounded potential growth {growth[c.Id]:F4}."); ok = false; }
                        if (!float.IsNaN(caseloadRatio) && Mathf.Abs(caseloadRatio - wage[c.Id]) > 0.02f * wage[c.Id]) { Debug.LogError($"LINE INDEXATION: {c.Id} caseload factor {caseloadRatio:F4} is not the real wage growth {wage[c.Id]:F4} - the caseload is not at the going wage."); ok = false; }
                        if (!float.IsNaN(driverlessRatio) && driverlessRatio < 1f) { sawShrink = true; }
                    }
                    report.Add($"{c.Id}{(player ? " (player)" : "")}: {withDriver} caseload line(s) at {(float.IsNaN(caseloadRatio) ? 1f : caseloadRatio):F3} (real wage {wage[c.Id]:F3}), {without} driverless at {(float.IsNaN(driverlessRatio) ? 1f : driverlessRatio):F3}");
                }
                if (!sawShrink) { Debug.LogError("LINE INDEXATION: no AI country's driverless factor sits below 1 - the shrinking direction (a negative potential) was not exercised."); ok = false; }
                Debug.Log($"LINE INDEXATION: after {Years} years - {string.Join("; ", report)}. A caseload line carries the country's real wage growth on top of prices and its driver (the income index's rule); a driverless line carries the compounded potential growth; the player's lines read 1 on both.");
            }
            finally { Object.DestroyImmediate(go); }
            Debug.Log(ok ? "LINE INDEXATION: PASS - caseload lines at the going real wage, driverless lines at potential growth, the player unchanged, both directions." : "LINE INDEXATION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
