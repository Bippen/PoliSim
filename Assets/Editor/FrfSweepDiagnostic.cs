using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE FRF SWEEP (ruled 2026-08-16, queued after Phase 5, run in the daily regime): re-derives
    /// `FiscalReactionSensitivity` + the `[0.5, 1.5]` bounds IN REAL UNITY, as the pair they were
    /// fitted as - because the original 2026-07-22 sweep fitted them in the standalone harness,
    /// which reported four-significant-figure stability for a system real Unity showed diverging.
    /// The honest expected outcome is EMPTY, and empty is the deliverable.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.FrfSweepDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// THE GRID, stated so coverage is a claim with its enumeration attached. U is walled at 1.5
    /// (the revenue-capacity ruling - enforced by the hook itself, which throws above it); the
    /// sweep therefore varies the SLOPE (how early the stance saturates: the cap is reached at
    /// (U-1)/S x 100 points above ComfortableDebtToGdpPercent) and each bound within the wall:
    ///   0: S=1.5  [0.5,1.5]   - the CURRENT pair, the reference run
    ///   1: S=2.5  [0.5,1.5]   - full tightening from 20pts over comfortable (was 33.3)
    ///   2: S=4.0  [0.5,1.5]   - from 12.5pts over
    ///   3: S=6.0  [0.5,1.5]   - from 8.3pts over
    ///   4: S=10.0 [0.5,1.5]   - near-bang-bang, from 5pts over: if earlier-full-tightening can
    ///                           prevent entry into the compounding zone AT ALL, this point shows it
    ///   5: S=1.5  [0.8,1.5]   - the lower-bound arm: less pro-cyclical loosening below comfortable
    ///   6: S=1.5  [0.5,1.25]  - wall-DIRECTION probe: a tighter cap must WORSEN the climbers -
    ///                           a monotonicity check that doubles as sweep reachability evidence
    ///
    /// Per point x seed (777, 424242): six countries, no-policy baseline, 200 turns via the real
    /// day loop; judged at turns 100-200 per the standing scoping (mechanism present and correctly
    /// signed; NEVER turn-1000 convergence - the 1000-turn run is a shape check only, taken
    /// separately at the most-different point). Reported per country: debt/GDP at t100/150/200,
    /// the t150-200 slope (pts/turn), and the implied stance at t200 with its saturation.
    ///
    /// ⚠ THE BYTE-IDENTICAL-DISTRUST RULE APPLIES (2026-08-11's own lesson, from this exact
    /// mechanism's trend-term bug): a candidate point whose t200 debt matches the reference
    /// byte-for-byte on ALL SIX countries is flagged NOT-REACHED, never read as "no effect".
    /// </summary>
    public static class FrfSweepDiagnostic
    {
        private static readonly (float S, float L, float U)[] Grid =
        {
            (1.5f, 0.5f, 1.5f),
            (2.5f, 0.5f, 1.5f),
            (4.0f, 0.5f, 1.5f),
            (6.0f, 0.5f, 1.5f),
            (10f, 0.5f, 1.5f),
            (1.5f, 0.8f, 1.5f),
            (1.5f, 0.5f, 1.25f)
        };

        private static readonly int[] Seeds = { 777, 424242 };
        private const int Turns = 200;

        /// <summary>The 1000-turn SHAPE CHECK (diagnostic only, never a target - the standing
        /// scoping): does an in-wall steeper slope change the DEEP divergence shape, or merely
        /// delay the same wall? Runs S=2.5 and S=4.0 (the plausible alternatives; S≥6 already
        /// disqualified itself by oscillation in the main window) at seed 777, reporting the ratio
        /// at t500/t1000 with the t900-1000 slope, beside the current pair's known post5 values.</summary>
        public static void RunShape()
        {
            try
            {
                foreach ((float s, float l, float u) in new[] { (2.5f, 0.5f, 1.5f), (4.0f, 0.5f, 1.5f) })
                {
                    SimulationManager.SetFiscalReactionPairForSweep(s, l, u);
                    SimulationRandom.Seed(777);
                    World world = WorldFactory.CreateDefault();
                    var go = new GameObject($"FRFShape_{s}");
                    try
                    {
                        SimulationManager sim = go.AddComponent<SimulationManager>();
                        sim.SetWorld(world);
                        var decisions = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country country in world.Countries)
                        {
                            decisions[country.Id] = PolicyDecision.None();
                        }

                        var at500 = new Dictionary<CountryId, float>();
                        var at900 = new Dictionary<CountryId, float>();
                        for (int turn = 1; turn <= 1000; turn++)
                        {
                            for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                            {
                                sim.AdvanceDay();
                            }

                            sim.AdvanceTurn(decisions);
                            if (turn == 500 || turn == 900 || turn == 1000)
                            {
                                foreach (Country country in world.Countries)
                                {
                                    if (turn == 500) { at500[country.Id] = country.State.DebtToGdpRatio; }
                                    else if (turn == 900) { at900[country.Id] = country.State.DebtToGdpRatio; }
                                    else
                                    {
                                        float r = country.State.DebtToGdpRatio;
                                        float gap = r - GetComfortable(country.Id);
                                        float stance = Mathf.Clamp(1f + s * gap / 100f, l, u);
                                        Debug.Log($"SHAPE S={s:F1}: {country.Id,-8} t500={at500[country.Id],7:F1} t1000={r,7:F1} slope(t900-1000)={(r - at900[country.Id]) / 100f,7:F3}/turn stance={stance:F3}{(stance >= u - 0.0001f ? " AT CAP" : "")}");
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(go);
                    }
                }

                CheckExit.Finish(0);
            }
            finally
            {
                SimulationManager.ResetFiscalReactionPair();
            }
        }

        public static void Run()
        {
            try
            {
                var reference = new Dictionary<(int Seed, CountryId Country), float>();
                int notReached = 0;

                for (int p = 0; p < Grid.Length; p++)
                {
                    (float s, float l, float u) = Grid[p];
                    SimulationManager.SetFiscalReactionPairForSweep(s, l, u);
                    foreach (int seed in Seeds)
                    {
                        Dictionary<CountryId, (float R100, float R150, float R200)> result = RunOne(seed);
                        var sb = new StringBuilder();
                        sb.Append($"SWEEP point {p} S={s:F1} [{l:F2},{u:F2}] seed {seed}:\n");
                        bool anyDiffers = p == 0;
                        foreach (KeyValuePair<CountryId, (float R100, float R150, float R200)> pair in result)
                        {
                            (float r100, float r150, float r200) = pair.Value;
                            float slope = (r200 - r150) / 50f;
                            float gap = r200 - GetComfortable(pair.Key);
                            float stance = Mathf.Clamp(1f + s * gap / 100f, l, u);
                            string saturation = stance >= u - 0.0001f ? "AT CAP" : stance <= l + 0.0001f ? "AT FLOOR" : "free";
                            sb.Append($"  {pair.Key,-8} t100={r100,7:F1} t150={r150,7:F1} t200={r200,7:F1} slope={slope,7:F3}/turn stance={stance:F3} ({saturation})\n");

                            if (p == 0)
                            {
                                reference[(seed, pair.Key)] = r200;
                            }
                            else if (reference[(seed, pair.Key)] != r200)
                            {
                                anyDiffers = true;
                            }
                        }

                        if (!anyDiffers)
                        {
                            sb.Append("  ⚠ NOT-REACHED FLAG: t200 byte-identical to the reference on all six countries - distrust, per the 2026-08-11 rule.\n");
                            notReached++;
                        }

                        Debug.Log(sb.ToString());
                    }
                }

                Debug.Log(notReached == 0
                    ? "SWEEP: complete, every candidate point measurably reached."
                    : $"SWEEP: complete with {notReached} NOT-REACHED flags - those points verified nothing.");
                CheckExit.Finish(0);
            }
            finally
            {
                SimulationManager.ResetFiscalReactionPair();
            }
        }

        private static Dictionary<CountryId, (float, float, float)> RunOne(int seed)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"FRF_{seed}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country country in world.Countries)
                {
                    decisions[country.Id] = PolicyDecision.None();
                }

                var at100 = new Dictionary<CountryId, float>();
                var at150 = new Dictionary<CountryId, float>();
                var result = new Dictionary<CountryId, (float, float, float)>();
                for (int turn = 1; turn <= Turns; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                    }

                    sim.AdvanceTurn(decisions);
                    if (turn == 100 || turn == 150 || turn == Turns)
                    {
                        foreach (Country country in world.Countries)
                        {
                            if (turn == 100) { at100[country.Id] = country.State.DebtToGdpRatio; }
                            else if (turn == 150) { at150[country.Id] = country.State.DebtToGdpRatio; }
                            else { result[country.Id] = (at100[country.Id], at150[country.Id], country.State.DebtToGdpRatio); }
                        }
                    }
                }

                return result;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Dictionary<CountryId, float> _comfortable;

        private static float GetComfortable(CountryId id)
        {
            // Read from a default world rather than duplicated (the anchor is seed data), cached
            // once - it cannot change between grid points.
            if (_comfortable == null)
            {
                _comfortable = new Dictionary<CountryId, float>();
                World world = WorldFactory.CreateDefault();
                foreach (Country country in world.Countries)
                {
                    _comfortable[country.Id] = country.ComfortableDebtToGdpPercent;
                }
            }

            return _comfortable[id];
        }
    }
}
