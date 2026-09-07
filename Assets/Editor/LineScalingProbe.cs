using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-2's measurement (2026-09-07): which spending lines does the AI's rule scale with output, and by how much, per country over a century? Every AI country
    /// at seed policy (Sweden the player, no decisions); at years 1, 50 and 100, per line: the nominal amount over its seed, that ratio over the price level (the
    /// real line), the real line over the line's own driver ratio (what is left after B2's driver), and real GDP over its seed - so a line indexed to its driver
    /// and prices reads 1.00 in the third column, and a line scaled with output reads the GDP column there. A probe: it prints, asserts nothing, and is in no bar.
    /// </summary>
    public static class LineScalingProbe
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LINESCALE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var seedAmount = new Dictionary<(CountryId, SpendingCategory), float>();
                var seedDriver = new Dictionary<(CountryId, SpendingCategory), float>();
                var seedGdp = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    seedGdp[c.Id] = c.State.GDP;
                    foreach (SpendingLine line in c.SpendingLines)
                    {
                        seedAmount[(c.Id, line.Category)] = line.Amount;
                        seedDriver[(c.Id, line.Category)] = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c));
                    }
                }
                var sb = new StringBuilder();
                sb.Append("LINESCALE: year  country   line                              nominal/seed  real/seed  real/(seed x driver)  realGDP/seed  driver\n");
                for (int year = 1; year <= 100; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (year != 1 && year != 50 && year != 100) { continue; }
                    foreach (Country c in world.Countries)
                    {
                        float price = Mathf.Max(0.0001f, c.State.PriceLevel);
                        float gdpRatio = c.State.GDP / Mathf.Max(0.0001f, seedGdp[c.Id]);
                        foreach (SpendingLine line in c.SpendingLines)
                        {
                            float seed = seedAmount[(c.Id, line.Category)];
                            if (seed <= 0f) { continue; }
                            float nominal = line.Amount / seed;
                            float real = nominal / price;
                            float driver = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)) / seedDriver[(c.Id, line.Category)];
                            sb.Append($"LINESCALE: {year,4}  {c.Id,-8}  {line.Category,-32}  {nominal,11:F3}  {real,9:F3}  {real / driver,20:F3}  {gdpRatio,12:F3}  {SpendingDrivers.Of(line.Category)}\n");
                        }
                    }
                }
                Debug.Log(sb.ToString());
                Debug.Log("LINESCALE: done (a probe - nothing asserted).");
            }
            finally { Object.DestroyImmediate(go); }
            CheckExit.Finish(0);
        }
    }
}
