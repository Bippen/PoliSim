using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-1's measurement (2026-09-07): WHY does road quality drift to its cap at baseline? Six countries at seed policy for a century, every ten years: the real
    /// infrastructure spending per head against its seed (the rebuild term's ratio), the nominal line against its seed, the price level, the population against its
    /// seed, real GDP per head against its seed, and the road-quality score. A probe: it prints, it asserts nothing, and it is not in either bar.
    /// </summary>
    public static class InfrastructureDriftProbe
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("INFRADRIFT");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var seedPerHead = new Dictionary<CountryId, float>(); var seedNominal = new Dictionary<CountryId, float>(); var seedPop = new Dictionary<CountryId, float>(); var seedGdpHead = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries) { seedPerHead[c.Id] = InfrastructureFamily.SpendPerHead(c); seedNominal[c.Id] = InfrastructureFamily.SpendingNominal(c); seedPop[c.Id] = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, c)); seedGdpHead[c.Id] = c.State.GDP / Mathf.Max(0.0001f, c.State.Population); }
                var sb = new StringBuilder();
                sb.Append("INFRADRIFT: year  country   realPerHead/seed  nominalLine/seed  priceLevel  pop/seed  realGdpPerHead/seed  roadQuality\n");
                for (int year = 1; year <= 100; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (year % 10 != 0 && year != 1) { continue; }
                    foreach (Country c in world.Countries)
                    {
                        if (!c.Infrastructure.Seeded) { continue; }
                        float pop = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, c));
                        sb.Append($"INFRADRIFT: {year,4}  {c.Id,-8}  {InfrastructureFamily.SpendPerHead(c) / seedPerHead[c.Id],14:F4}  {InfrastructureFamily.SpendingNominal(c) / seedNominal[c.Id],14:F4}  {c.State.PriceLevel,10:F4}  {pop / seedPop[c.Id],8:F4}  {(c.State.GDP / Mathf.Max(0.0001f, c.State.Population)) / seedGdpHead[c.Id],17:F4}  {c.State.RoadQuality,10:F2}\n");
                    }
                }
                Debug.Log(sb.ToString());
                Debug.Log("INFRADRIFT: done (a probe - nothing asserted).");
            }
            finally { Object.DestroyImmediate(go); }
            CheckExit.Finish(0);
        }
    }
}
