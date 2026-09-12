using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The environment feedback pass (2026-09-07): the carbon tax's base is the taxed CO₂. (1) The driver: TaxBases.Of(CarbonTax) is Emissions, its level at
    /// the seed is TRANSPORT CO₂ per head × population for six (EN-4d, ruled 2026-09-11, §467: the ETS-covered power fleet is exempt of the national carbon tax
    /// by statute, applied at sector level; before it the level was power + transport), and the captured reference equals it. (2) The identity: after twenty years
    /// the carbon base's driver ratio IS the emissions level over its seed, for a country with an implemented carbon tax (Poland since EN-3; Sweden before). (3) The
    /// erosion: Poland with the tax raised twenty points through the decision against untouched - transport LOWER through the readout coupling, POWER HELD (the tax
    /// does not reach the fleet; its carbon price is the ETS), the driver ratio LOWER, and the carbon revenue rising LESS than the rate did (rate × base, the base
    /// eroding). (4) Output's own bases are untouched by the move: the corporate line's driver is still output.
    /// </summary>
    public static class EnvironmentFeedbackDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            if (TaxBases.Of(TaxType.CarbonTax) != TaxBaseDriver.Emissions) { Debug.LogError("ENVIRONMENT FEEDBACK: the carbon tax's driver is not Emissions."); ok = false; }
            if (TaxBases.Of(TaxType.CorporateTax) != TaxBaseDriver.Output) { Debug.LogError("ENVIRONMENT FEEDBACK: the corporate tax's driver left output - the move was meant for the carbon tax alone."); ok = false; }
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                if (!c.Environment.Seeded) { continue; }
                float level = TaxBases.Level(TaxBaseDriver.Emissions, c);
                float expected = c.State.TransportCo2PerCapita * c.State.Population;   // EN-4d: transport's tonnes alone
                if (level <= 0f || Mathf.Abs(level - expected) > 1e-4f * Mathf.Max(1f, expected)) { Debug.LogError($"ENVIRONMENT FEEDBACK: {c.Id}'s emissions level at the seed is {level:F4}, not transport × population = {expected:F4} (the power fleet is exempt of the tax, EN-4d)."); ok = false; }
                if (c.RevenueBaseSeeds == null || c.RevenueBaseSeeds.Length < TaxBases.DriverCount || Mathf.Abs(c.RevenueBaseSeeds[(int)TaxBaseDriver.Emissions] - level) > 1e-4f * Mathf.Max(1f, level)) { Debug.LogError($"ENVIRONMENT FEEDBACK: {c.Id}'s captured emissions reference is not the seed's level."); ok = false; }
            }

            // EN-3 (2026-09-11): the probed country is POLAND - the other implemented carbon tax line, and the one with a fossil fleet the dispatch can move (Sweden's power
            // figure no longer answers a carbon tax: its fossil thermal is heat-led CHP outside the merit order, so the erosion can only be shown where the coal is).
            float[] untouched = Run(CountryId.Poland, Years, 0f);
            float[] raised = Run(CountryId.Poland, Years, 20f);
            // [0] power, [1] transport, [2] driver ratio, [3] identity ratio (level now / seed), [4] carbon revenue, [5] carbon rate
            if (Mathf.Abs(untouched[2] - untouched[3]) > 1e-4f || Mathf.Abs(raised[2] - raised[3]) > 1e-4f) { Debug.LogError($"ENVIRONMENT FEEDBACK: the carbon base's driver ratio ({untouched[2]:F5} / {raised[2]:F5}) is not the emissions level over its seed ({untouched[3]:F5} / {raised[3]:F5})."); ok = false; }
            // EN-4d: the tax reaches transport and the base; the power figure is the dispatch's, whose carbon price is the ETS - it holds under the raise
            if (Mathf.Abs(raised[0] - untouched[0]) > 1e-5f * Mathf.Max(1e-6f, untouched[0]) || !(raised[1] < untouched[1]) || !(raised[2] < untouched[2])) { Debug.LogError($"ENVIRONMENT FEEDBACK: the raise must hold power and lower transport and the base - power {untouched[0]:F4} → {raised[0]:F4}, transport {untouched[1]:F3} → {raised[1]:F3}, the base ratio {untouched[2]:F4} → {raised[2]:F4}."); ok = false; }
            float rateRatio = raised[5] / Mathf.Max(0.0001f, untouched[5]);
            float revenueRatio = raised[4] / Mathf.Max(0.0001f, untouched[4]);
            if (!(revenueRatio < rateRatio) || !(revenueRatio > 1f)) { Debug.LogError($"ENVIRONMENT FEEDBACK: the carbon revenue rose x{revenueRatio:F4} against a rate x{rateRatio:F4} - the base did not erode as the tax worked, or the tax lost revenue outright."); ok = false; }

            Debug.Log($"ENVIRONMENT FEEDBACK: Poland after {Years} years - untouched: power {untouched[0]:F3} t, transport {untouched[1]:F3} t, carbon base x{untouched[2]:F4} of its seed, revenue {untouched[4]:F3} at {untouched[5]:F0} %; "
                + $"the tax raised twenty points through the decision: power {raised[0]:F3} t (held - the fleet pays the ETS, not the tax; EN-4d), transport {raised[1]:F3} t, base x{raised[2]:F4}, revenue {raised[4]:F3} at {raised[5]:F0} - the rate x{rateRatio:F3}, the revenue x{revenueRatio:F3}: the base erodes as the tax works. "
                + "The driver ratio is the emissions level over its seed to 1e-4; the base is transport's tonnes (EN-4d); output's own bases are untouched by the move.");
            Debug.Log(ok ? "ENVIRONMENT FEEDBACK: PASS - the carbon base is the taxed CO₂, transport's since EN-4d: the identity holds, the base erodes under a raise, power holds, output's bases stand." : "ENVIRONMENT FEEDBACK: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float extraPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ENVFB");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                // EN-3: Poland's carbon tax line is seeded unimplemented and an override is a no-op on it - the diagnostic implements it at 5 in both runs (the player's immediate action), so the comparison is the raise alone
                foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.CarbonTax && !line.IsImplemented) { line.IsImplemented = true; line.Rate = 5f; } }
                float seedRate = EnvironmentFamily.CarbonTaxRate(c);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (extraPoints != 0f) { d.TaxRateOverrides[TaxType.CarbonTax] = seedRate + extraPoints; }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                }
                TaxLine carbon = null;
                foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.CarbonTax) { carbon = line; } }
                float revenue = carbon != null ? TaxBases.Revenue(c, carbon) : 0f;
                float rate = carbon != null ? carbon.Rate : 0f;
                float level = TaxBases.Level(TaxBaseDriver.Emissions, c);
                float reference = c.RevenueBaseSeeds[(int)TaxBaseDriver.Emissions];
                return new[] { c.State.PowerCo2PerCapita, c.State.TransportCo2PerCapita, TaxBases.DriverRatio(c, TaxType.CarbonTax), reference > 0f ? level / reference : 1f, revenue, rate };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
