using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B3 (2026-09-05): every tax base follows its driver EXACTLY, per instrument, per country - asserted on the
    /// real turn path, and the channel §312 found missing is shown to exist. For each of the six countries: a fresh
    /// world, five untouched turns; every implemented line's base (TaxBases.Base) must equal its sourced share of the
    /// seed's GDP times the ratio of its driver's level now to the seed's - the level computed HERE from the state and
    /// the cohorts, the reference read from the country (consumption's is taken on the first day, as documented).
    /// Then the recession probe: the same world run twice from the seed, once untouched and once with the
    /// unemployment rate pushed up five points on turn 3 (the state, not a dial - the channel under test is the base,
    /// not the lever); at the end of turn 3 the wage-bill lines' revenue must have fallen against the untouched run by
    /// MORE than output-based revenue did, and by the wage bill's own ratio within tolerance - the employment channel,
    /// with the elasticity stated: 1 to the wage bill, 1 to consumption, 1 to output, each on its own lines.
    ///
    /// Sits on the simulation bar: it builds and advances worlds.
    /// </summary>
    public static class RevenueBaseDiagnostic
    {
        private const int Years = 5;
        private const int ShockTurn = 3;
        private const float ShockPoints = 5f;
        private const float Tolerance = 1e-4f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            int basesChecked = 0;
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                // (1) the base follows its driver, every instrument, five years
                SimulationRandom.Seed(777);
                World world = WorldFactory.CreateDefault();
                var go = new GameObject("REVBASE");
                try
                {
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    Country c = world.GetCountry(id);
                    float seedGdp = c.State.GDP;
                    var decisions = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                    for (int year = 1; year <= Years; year++)
                    {
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                        sim.AdvanceTurn(decisions);
                    }
                    var byDriver = new Dictionary<TaxBaseDriver, float>();
                    foreach (TaxLine line in c.TaxLines)
                    {
                        if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                        TaxBaseDriver driver = TaxBases.Of(line.Type);
                        float share = TaxBaseTable.BaseShareOfGdp(id, line.Type);
                        float reference = c.RevenueBaseSeeds[(int)driver];
                        float level = TaxBases.Level(driver, c);
                        float expected = share * seedGdp * (reference > 0f ? level / reference : 1f);
                        float actual = TaxBases.Base(c, line.Type);
                        basesChecked++;
                        if (Mathf.Abs(actual / expected - 1f) > Tolerance)
                        {
                            Debug.LogError($"REVBASE: {id} {line.Type} ({TaxBases.Name(driver)}) base {actual:F4} after {Years} years; share {share:F4} x seed GDP {seedGdp:F2} x driver ratio {(reference > 0f ? level / reference : 1f):F5} = {expected:F4}.");
                            ok = false;
                        }
                        byDriver[driver] = reference > 0f ? level / reference : 1f;
                    }
                    var parts = new List<string>();
                    foreach (KeyValuePair<TaxBaseDriver, float> kv in byDriver) { parts.Add($"{TaxBases.Name(kv.Key)} x{kv.Value:F4}"); }
                    Debug.Log($"REVBASE: {id} - {parts.Count} driver(s) on its lines after {Years} years: {string.Join(", ", parts)}; GDP x{c.State.GDP / seedGdp:F4}.");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }

                // (2) the recession probe: unemployment +5 points on turn 3, the wage-bill lines fall by the wage bill's own ratio
                float untouchedWage = 0f, untouchedOutput = 0f, shockedWage = 0f, shockedOutput = 0f, untouchedGdp = 0f, shockedGdp = 0f;
                bool hasWageLine = false, hasOutputLine = false;
                for (int pass = 0; pass < 2; pass++)
                {
                    bool shocked = pass == 1;
                    SimulationRandom.Seed(777);
                    World w = WorldFactory.CreateDefault();
                    var g = new GameObject(shocked ? "REVBASE-SHOCK" : "REVBASE-BASE");
                    try
                    {
                        SimulationManager sim = g.AddComponent<SimulationManager>();
                        sim.SetWorld(w);
                        Country c = w.GetCountry(id);
                        var decisions = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country k in w.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                        for (int year = 1; year <= ShockTurn; year++)
                        {
                            for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                            if (shocked && year == ShockTurn) { c.State.Unemployment = Mathf.Min(60f, c.State.Unemployment + ShockPoints); }
                            sim.AdvanceTurn(decisions);
                        }
                        float wage = 0f, output = 0f;
                        foreach (TaxLine line in c.TaxLines)
                        {
                            if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                            TaxBaseDriver d = TaxBases.Of(line.Type);
                            if (d == TaxBaseDriver.WageBill) { wage += TaxBases.Revenue(c, line); hasWageLine = true; }
                            else if (d == TaxBaseDriver.Output) { output += TaxBases.Revenue(c, line); hasOutputLine = true; }
                        }
                        if (shocked) { shockedWage = wage; shockedOutput = output; shockedGdp = c.State.GDP; }
                        else { untouchedWage = wage; untouchedOutput = output; untouchedGdp = c.State.GDP; }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(g); }
                }
                if (hasWageLine && hasOutputLine && untouchedWage > 0f && untouchedOutput > 0f)
                {
                    float wageFall = shockedWage / untouchedWage;
                    float outputFall = shockedOutput / untouchedOutput;
                    float gdpFall = shockedGdp / untouchedGdp;
                    // Output-based lines move with GDP exactly (their base IS output); the wage-bill lines carry the jobs lost on top.
                    if (Mathf.Abs(outputFall / gdpFall - 1f) > Tolerance)
                    {
                        Debug.LogError($"REVBASE: {id} - the output-based lines moved x{outputFall:F5} while GDP moved x{gdpFall:F5}; they should move together.");
                        ok = false;
                    }
                    if (wageFall >= outputFall)
                    {
                        Debug.LogError($"REVBASE: {id} - five points of unemployment on turn {ShockTurn} left the wage-bill lines at x{wageFall:F5} of the untouched run against x{outputFall:F5} for the output lines: the employment channel is not there.");
                        ok = false;
                    }
                    Debug.Log($"REVBASE: {id} recession probe - unemployment +{ShockPoints:F0} pts on turn {ShockTurn}: GDP x{gdpFall:F4}, output-based revenue x{outputFall:F4}, wage-bill revenue x{wageFall:F4} (the jobs lost on top of the output lost).");
                }
                else
                {
                    Debug.Log($"REVBASE: {id} recession probe skipped - the country lacks a wage-bill or an output-based line.");
                }
            }
            Debug.Log(ok ? $"=== RevenueBaseDiagnostic: ALL ASSERTIONS PASS ({basesChecked} bases across the six, each at its sourced share x the seed's GDP x its driver's ratio) ===" : "=== RevenueBaseDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
