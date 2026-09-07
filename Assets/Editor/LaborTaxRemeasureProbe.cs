using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §392: FT-5 re-measured behind the jobs lag. A probe, nothing asserted and nothing landed: with MacroSystem.LaborTaxTermInTarget set for these runs only,
    /// Sweden the player takes a 5-point income-tax CUT (and a 5-point rise) at turn 3 against an untouched run, and the print reads real GDP and unemployment
    /// against the untouched run at L, L+1, L+2 and L+4 - the output multiplier on the statutory-revenue basis (the harness\x27s) and the unemployment response,
    /// beside SELMA\x27s 0.17 over two years with unemployment +0.48 (§355). The same runs with the term OUT show what the jobs lag alone does to the tax channel.
    /// </summary>
    public static class LaborTaxRemeasureProbe
    {
        private const int Turns = 8;
        private const int Landing = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            try
            {
                foreach (bool wired in new[] { false, true })
                {
                    MacroSystem.LaborTaxTermInTarget = wired;
                    float[][] untouched = Trajectory(0f);
                    foreach (float delta in new[] { -5f, 5f })
                    {
                        float[][] moved = Trajectory(delta);
                        // the statutory impulse: Δrate × the income-tax base at landing, over nominal GDP, as the harness reads it
                        float impulse = moved[3][Landing - 1];
                        sb.Append($"REMEASURE: term {(wired ? "IN" : "OUT")}, income tax {delta:+0} points at turn {Landing} - impulse {impulse:F2} % of GDP (statutory);");
                        foreach (int h in new[] { 0, 1, 2, 4 })
                        {
                            int t = Landing - 1 + h;
                            float dGdp = (moved[0][t] / untouched[0][t] - 1f) * 100f;
                            float dU = moved[1][t] - untouched[1][t];
                            float dP = moved[2][t] - untouched[2][t];
                            sb.Append($" L+{h}: ΔGDP {dGdp:+0.00} % (multiplier {(impulse != 0f ? -dGdp / impulse : 0f):F2}), ΔU {dU:+0.000}, Δparticipation {dP:+0.000};");
                        }
                        sb.Append('\n');
                    }
                }
                sb.Append("REMEASURE: SELMA (§355) reads a labour-income-tax cut at 0.17 of output over two years with unemployment RISING +0.48 points - supply arriving before jobs.\n");
                Debug.Log(sb.ToString());
                Debug.Log("REMEASURE: done (a probe - nothing asserted, nothing landed).");
            }
            finally { MacroSystem.LaborTaxTermInTarget = false; }
            CheckExit.Finish(0);
        }

        /// <summary>[0] real GDP, [1] unemployment, [2] participation, [3] the statutory impulse (% of GDP) at each turn, Sweden the player, the decision at Landing.</summary>
        private static float[][] Trajectory(float deltaPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("REMEASURE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                float seedRate = c.LaborTaxRateSeed;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var gdp = new float[Turns]; var u = new float[Turns]; var p = new float[Turns]; var imp = new float[Turns];
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (deltaPoints != 0f && year >= Landing) { d.TaxRateOverrides[TaxType.IncomeTax] = seedRate + deltaPoints; }
                    if (deltaPoints != 0f && year == Landing) { foreach (TaxLine t in c.TaxLines) { if (t.Type == TaxType.IncomeTax) { imp[year - 1] = deltaPoints / 100f * TaxBases.Base(c, TaxType.IncomeTax) / c.State.NominalGdp * 100f; } } }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    gdp[year - 1] = c.State.GDP; u[year - 1] = c.State.Unemployment; p[year - 1] = c.State.LaborForceParticipationRate;
                }
                return new[] { gdp, u, p, imp };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
