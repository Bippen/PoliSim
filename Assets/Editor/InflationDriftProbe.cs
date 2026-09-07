using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-6 (2026-09-07, §379): WHY inflation drifts upward over the long horizon. A probe, nothing asserted: the no-policy world (no player), seed 777,
    /// 300 turns; every 25 turns per country - unemployment, NAIRU, the gap, real growth against potential growth, inflation, expectations, the zone rate
    /// and the Taylor suggestion - and over the whole run the MEAN unemployment gap and the mean of (inflation − expectations), which the Phillips curve
    /// makes −slope × gap. If the mean gap is negative the drift is a persistent overheating bias read through adaptive expectations, a ratchet.
    /// </summary>
    public static class InflationDriftProbe
    {
        private const int Turns = 300;
        /// <summary>MacroSystem.PhillipsCurveSlope (private there), restated for the arithmetic below.</summary>
        private const float PhillipsSlope = 0.3f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("INFLDRIFT");
            var sb = new StringBuilder();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var gapSum = new Dictionary<CountryId, double>(); var wedgeSum = new Dictionary<CountryId, double>(); var growthGapSum = new Dictionary<CountryId, double>();
                var belowCount = new Dictionary<CountryId, int>();
                var lastGdp = new Dictionary<CountryId, float>();
                foreach (Country k in world.Countries) { gapSum[k.Id] = 0; wedgeSum[k.Id] = 0; growthGapSum[k.Id] = 0; belowCount[k.Id] = 0; lastGdp[k.Id] = k.State.GDP; }
                sb.Append("INFLDRIFT: turn  country   U      NAIRU   gap     growth-pot  infl    exp     rate    taylor\n");
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    foreach (Country c in world.Countries)
                    {
                        EconomyState s = c.State;
                        float gap = s.Unemployment - c.NaturalUnemploymentRate;
                        float growth = lastGdp[c.Id] > 0f ? (s.GDP / lastGdp[c.Id] - 1f) * 100f : 0f;
                        float growthGap = growth - c.PotentialGrowthRate;
                        lastGdp[c.Id] = s.GDP;
                        if (year > 5) { gapSum[c.Id] += gap; wedgeSum[c.Id] += s.Inflation - s.InflationExpectations; growthGapSum[c.Id] += growthGap; if (gap < 0f) { belowCount[c.Id]++; } }
                        if (year % 25 == 0 || year == 1)
                        {
                                                        sb.Append($"INFLDRIFT: {year,4}  {c.Id,-8} {s.Unemployment,6:F2} {c.NaturalUnemploymentRate,6:F2} {gap,7:F3} {growthGap,10:F3} {s.Inflation,7:F2} {s.InflationExpectations,7:F2} {(c.CurrencyZone?.InterestRate ?? -999f),7:F2} {TaylorRule.GetSuggestedInterestRate(c),7:F2}\n");
                        }
                    }
                }
                int n = Turns - 5;
                foreach (Country c in world.Countries)
                {
                    sb.Append($"INFLDRIFT: MEAN {c.Id,-8} gap {gapSum[c.Id] / n,+8:F4} (below NAIRU {100.0 * belowCount[c.Id] / n:F0} % of turns)  growth−potential {growthGapSum[c.Id] / n,+8:F4}  inflation−expectations {wedgeSum[c.Id] / n,+8:F4}  (the Phillips slope 0.3 × −gap → {-PhillipsSlope * gapSum[c.Id] / n,+8:F4}); the nudges inferred: reversion 0.7 × gap + Okun 0.5 × growth gap = {0.7 * gapSum[c.Id] / n + 0.5 * growthGapSum[c.Id] / n,+8:F4} per turn\n");
                }
                Debug.Log(sb.ToString());
                Debug.Log("INFLDRIFT: done (a probe - nothing asserted).");
                CheckExit.Finish(0);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
