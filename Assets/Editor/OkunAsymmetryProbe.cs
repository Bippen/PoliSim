using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §400: the remaining half of the NAIRU gap, read. A probe, nothing asserted: the no-policy world (no player), seed 777, 300 turns, subscribed to
    /// MacroSystem.OkunLedger so every daily application of Okun's law is decomposed into its growth term, its reversion term and its policy adjustments,
    /// summed per country per year. Beside them, from the state itself: the year's growth WITHIN the days (the GDP the last day left against the GDP the
    /// first day found), the jump AT the boundary (the GDP after AdvanceTurn against the GDP the last day left), the annual growth the print shows (after
    /// boundary to after boundary), the potential growth Okun's law reads (Country.PotentialGrowthRate at the year's open), unemployment at the open, before
    /// and after the boundary, and the effective natural rate. What the growth term reads is the within-year growth against the potential growth; what the
    /// year did is the annual growth. If the two differ on average, the difference is the boundary's own jump, and Okun's law reads growth the year never
    /// printed - the asymmetry this probe exists to measure. Every 25 turns per country a row; over years 6-300 the means.
    /// </summary>
    public static class OkunAsymmetryProbe
    {
        private const int Turns = 300;
        private const int Skip = 5;

        private sealed class Year
        {
            public float GdpOpen, GdpBeforeBoundary, GdpAfterBoundary, PotentialGrowthAtOpen, UOpen, UBeforeBoundary, UAfterBoundary, NaturalAfter, ExcessAfter;
            public double GrowthTerm, ReversionTerm, AdjustmentTerm; public int DailyCalls;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("OKUNASYM");
            var sb = new StringBuilder();
            var years = new Dictionary<CountryId, List<Year>>();
            var current = new Dictionary<CountryId, Year>();
            foreach (Country c in world.Countries) { years[c.Id] = new List<Year>(); }
            System.Action<Country, float, float, float, float> ledger = (c, g, r, a, slice) =>
            {
                if (!current.TryGetValue(c.Id, out Year y) || slice >= 1f) { return; }   // the turn form (the preview's) is not part of this run
                y.GrowthTerm += g; y.ReversionTerm += r; y.AdjustmentTerm += a; y.DailyCalls++;
            };
            MacroSystem.OkunLedger += ledger;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                sb.Append("OKUNASYM: turn  country   within%  boundary%  annual%  potential%  | growthΣ  reversionΣ  adjustΣ  boundaryΔU  ΔU_year | U      NAIRUeff  gap\n");
                for (int year = 1; year <= Turns; year++)
                {
                    foreach (Country c in world.Countries)
                    {
                        var y = new Year { GdpOpen = c.State.GDP, PotentialGrowthAtOpen = c.PotentialGrowthRate, UOpen = c.State.Unemployment };
                        current[c.Id] = y;
                    }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    foreach (Country c in world.Countries) { current[c.Id].GdpBeforeBoundary = c.State.GDP; current[c.Id].UBeforeBoundary = c.State.Unemployment; }
                    sim.AdvanceTurn(decisions);
                    foreach (Country c in world.Countries)
                    {
                        Year y = current[c.Id];
                        y.GdpAfterBoundary = c.State.GDP; y.UAfterBoundary = c.State.Unemployment; y.NaturalAfter = c.EffectiveNaturalUnemploymentRate; y.ExcessAfter = c.State.SupplyUnemploymentExcess;
                        years[c.Id].Add(y);
                        if (year % 25 == 0 || year == 1)
                        {
                            sb.Append($"OKUNASYM: {year,4}  {c.Id,-8} {Within(y),7:F3} {Boundary(y),9:F3} {Annual(y),8:F3} {y.PotentialGrowthAtOpen,10:F3}  | {y.GrowthTerm,7:+0.000} {y.ReversionTerm,10:+0.000} {y.AdjustmentTerm,8:+0.000} {y.UAfterBoundary - y.UBeforeBoundary,10:+0.000} {y.UAfterBoundary - y.UOpen,8:+0.000} | {y.UAfterBoundary,5:F2} {y.NaturalAfter,8:F2} {y.UAfterBoundary - y.NaturalAfter,6:+0.000}\n");
                        }
                    }
                }
                foreach (Country c in world.Countries)
                {
                    List<Year> ys = years[c.Id];
                    double within = 0, boundary = 0, annual = 0, potential = 0, g = 0, r = 0, a = 0, bdu = 0, du = 0, gap = 0, excess = 0; int n = 0, boundaryDown = 0;
                    for (int i = Skip; i < ys.Count; i++)
                    {
                        Year y = ys[i]; n++;
                        within += Within(y); boundary += Boundary(y); annual += Annual(y); potential += y.PotentialGrowthAtOpen;
                        g += y.GrowthTerm; r += y.ReversionTerm; a += y.AdjustmentTerm; bdu += y.UAfterBoundary - y.UBeforeBoundary; du += y.UAfterBoundary - y.UOpen;
                        gap += y.UAfterBoundary - y.NaturalAfter; excess += y.ExcessAfter; if (Boundary(y) < 0f) { boundaryDown++; }
                    }
                    sb.Append($"OKUNASYM: MEAN {c.Id,-8} years {Skip + 1}-{Turns}: within-year growth {within / n:+0.000} %, boundary jump {boundary / n:+0.000} % (down in {100.0 * boundaryDown / n:F0} % of years), annual growth {annual / n:+0.000} %, potential growth read {potential / n:+0.000} % - Okun reads a gap of {within / n - potential / n:+0.000} where the year printed {annual / n - potential / n:+0.000}; per year: growth term {g / n:+0.0000}, reversion {r / n:+0.0000}, adjustments {a / n:+0.0000}, boundary ΔU {bdu / n:+0.0000}, ΔU {du / n:+0.0000}; mean gap {gap / n:+0.0000} (excess {excess / n:+0.0000}).\n");
                }
                sb.Append("OKUNASYM: reading - the growth term Okun's law integrates over the days is −0.5 × (within-year growth − potential growth); the reversion offsets it at the welfare-adjusted speed on the core; whatever the two leave, the boundary's own movers (the supply shock, the spending and identity steps) add. A within-year growth above the printed annual growth is the boundary's jump, read by Okun as growth the year did not have.\n");
                Debug.Log(sb.ToString());
                Debug.Log("OKUNASYM: done (a probe - nothing asserted).");
                CheckExit.Finish(0);
            }
            finally { MacroSystem.OkunLedger -= ledger; Object.DestroyImmediate(go); }
        }

        private static float Within(Year y) => y.GdpOpen > 0f ? (y.GdpBeforeBoundary / y.GdpOpen - 1f) * 100f : 0f;
        private static float Boundary(Year y) => y.GdpBeforeBoundary > 0f ? (y.GdpAfterBoundary / y.GdpBeforeBoundary - 1f) * 100f : 0f;
        private static float Annual(Year y) => y.GdpOpen > 0f ? (y.GdpAfterBoundary / y.GdpOpen - 1f) * 100f : 0f;
    }
}
