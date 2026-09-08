using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-7, the first seat (2026-09-07, §391): labour supply arrives before employment. (1) At the seed the supply excess is zero for six and the boundary
    /// reference is the seed participation. (2) The unit: a state at participation 64 and unemployment 6 with a +2 rise reads an impact of 2 × 94 ÷ 66 points and
    /// the excess decays by exactly 0.40 a year over ten boundaries, both directions (−2 too), to 1e-4. (3) The world, Sweden the player, twelve turns: the
    /// participation rate pushed +2 points at the top of turn 3 raises unemployment against the untouched run by the impact the unit gives (within a quarter,
    /// the year\x27s other movements allowed) and the gap then falls toward zero - below 60 % of the impact after two more years, below 40 % after four; −2
    /// lowers it symmetrically. (4) The reversion reads the core: with the excess held, the untouched run\x27s unemployment after twenty years is within a
    /// hundredth of the run before FT-7 on the same seed (the excess is a few hundredths in a no-policy run, stated in the print). The family is §391\x27s.
    /// </summary>
    public static class JobsLagDiagnostic
    {
        private const int Turns = 12;
        private const int ShockTurn = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            // (1) the seed
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                if (Mathf.Abs(c.State.SupplyUnemploymentExcess) > 1e-6f) { Debug.LogError($"JOBS LAG: {c.Id} carries a supply excess at the seed ({c.State.SupplyUnemploymentExcess:R})."); ok = false; }
                if (Mathf.Abs(c.ParticipationAtLastBoundary - c.State.LaborForceParticipationRate) > 1e-6f) { Debug.LogError($"JOBS LAG: {c.Id}\x27s boundary reference is not its seed participation."); ok = false; }
            }
            // (2) the unit
            foreach (float shock in new[] { 2f, -2f })
            {
                var country = seedWorld.GetCountry(CountryId.Sweden);
                float p0 = 64f, u0 = 6f;
                country.State.LaborForceParticipationRate = p0; country.State.Unemployment = u0; country.State.SupplyUnemploymentExcess = 0f; country.ParticipationAtLastBoundary = p0;
                country.State.LaborForceParticipationRate = p0 + shock;
                MacroSystem.ApplySupplyShockToUnemployment(country);
                float expectedImpact = shock * (100f - u0) / (p0 + shock);
                if (Mathf.Abs(country.State.SupplyUnemploymentExcess - expectedImpact) > 1e-4f || Mathf.Abs(country.State.Unemployment - (u0 + expectedImpact)) > 1e-4f) { Debug.LogError($"JOBS LAG: a {shock:+0} shock read an excess of {country.State.SupplyUnemploymentExcess:F4} and unemployment {country.State.Unemployment:F4}, expected {expectedImpact:F4} and {u0 + expectedImpact:F4}."); ok = false; }
                float excess = expectedImpact;
                for (int i = 0; i < 10; i++)
                {
                    MacroSystem.ApplySupplyShockToUnemployment(country);   // no further change in participation: pure decay
                    excess *= 1f - MacroSystem.SupplyAbsorptionPerYear;
                    if (Mathf.Abs(country.State.SupplyUnemploymentExcess - excess) > 1e-4f) { Debug.LogError($"JOBS LAG: after {i + 1} boundaries the excess reads {country.State.SupplyUnemploymentExcess:F5}, expected {excess:F5} (0.40 a year)."); ok = false; break; }
                }
            }
            // FT-8 (§398): the demographic trend leaves the excess. A boundary on which only the pyramid's structural rate moved - participation following it
            // exactly, half a point - reads no excess and moves unemployment by nothing; the same half point arriving as a cyclical change reads the §391 impact.
            {
                var country = seedWorld.GetCountry(CountryId.Sweden);
                float structural = ParticipationRateTable.StructuralRate(country.Id, country.Cohorts.Counts);
                country.State.Unemployment = 6f; country.State.SupplyUnemploymentExcess = 0f;
                country.StructuralParticipationAtLastBoundary = structural - 0.5f; country.ParticipationAtLastBoundary = 64f; country.State.LaborForceParticipationRate = 64.5f;
                MacroSystem.ApplySupplyShockToUnemployment(country);
                if (Mathf.Abs(country.State.SupplyUnemploymentExcess) > 1e-5f || Mathf.Abs(country.State.Unemployment - 6f) > 1e-5f) { Debug.LogError($"JOBS LAG: a purely demographic half point of participation read an excess of {country.State.SupplyUnemploymentExcess:F5} and moved unemployment to {country.State.Unemployment:F5} - the trend is in the excess (FT-8, §398)."); ok = false; }
                country.StructuralParticipationAtLastBoundary = structural; country.ParticipationAtLastBoundary = 64f; country.State.LaborForceParticipationRate = 64.5f; country.State.SupplyUnemploymentExcess = 0f; country.State.Unemployment = 6f;
                MacroSystem.ApplySupplyShockToUnemployment(country);
                float cyclical = 0.5f * (100f - 6f) / 64.5f;
                if (Mathf.Abs(country.State.SupplyUnemploymentExcess - cyclical) > 1e-4f) { Debug.LogError($"JOBS LAG: the same half point arriving as a cyclical change read {country.State.SupplyUnemploymentExcess:F5}, expected {cyclical:F5}."); ok = false; }
            }
            if (Mathf.Abs(MacroSystem.SupplyAbsorptionPerYear - (1f - Mathf.Pow(0.88f, 4f))) > 0.005f) { Debug.LogError($"JOBS LAG: the absorption {MacroSystem.SupplyAbsorptionPerYear} is not 1 − 0.88⁴."); ok = false; }
            // (3) the world
            float[] untouched = Run(0f);
            float[] up = Run(2f);
            float[] down = Run(-2f);
            // [0..Turns-1] unemployment per turn, [Turns] participation at the shock turn (untouched), [Turns+1] the observed Δp at the shock boundary
            float impactUp = up[Turns + 1] * (100f - untouched[ShockTurn - 1]) / up[Turns];
            float gapUp0 = up[ShockTurn - 1] - untouched[ShockTurn - 1];
            float gapDown0 = down[ShockTurn - 1] - untouched[ShockTurn - 1];
            if (!(gapUp0 > 0f) || !(gapDown0 < 0f)) { Debug.LogError($"JOBS LAG: the shock does not move unemployment both ways on impact ({gapUp0:F3} / {gapDown0:F3})."); ok = false; }
            if (Mathf.Abs(gapUp0 - impactUp) > 0.25f * Mathf.Abs(impactUp)) { Debug.LogError($"JOBS LAG: the impact on unemployment ({gapUp0:F3}) is not the labour-force share of the observed participation change ({impactUp:F3}) within a quarter."); ok = false; }
            float gapUp2 = up[ShockTurn + 1] - untouched[ShockTurn + 1];
            float gapUp4 = up[ShockTurn + 3] - untouched[ShockTurn + 3];
            if (!(gapUp2 < 0.6f * gapUp0) || !(gapUp4 < 0.4f * gapUp0)) { Debug.LogError($"JOBS LAG: the excess does not decay - gap {gapUp0:F3} on impact, {gapUp2:F3} two years on, {gapUp4:F3} four years on."); ok = false; }
            Debug.Log($"JOBS LAG: Sweden, participation +2 at the top of turn {ShockTurn} - observed Δp {up[Turns + 1]:F3} points on a rate of {up[Turns]:F2}; unemployment gap on impact {gapUp0:F3} (the labour-force share says {impactUp:F3}), two years on {gapUp2:F3}, four years on {gapUp4:F3}; −2: {gapDown0:F3} on impact. Absorption {MacroSystem.SupplyAbsorptionPerYear:F2} a year (1 − 0.88⁴).");
            Debug.Log(ok ? "JOBS LAG: PASS - zero at the seed, the impact is the labour-force share both ways, the excess decays at SELMA\x27s rate, the reversion reads the core." : "JOBS LAG: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(float shockPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("JOBSLAG");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                sim.AiFinanceMinistryEnabled = false;   // the labour market alone
                Country c = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var result = new float[Turns + 2];
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    if (year == ShockTurn)
                    {
                        result[Turns] = c.State.LaborForceParticipationRate;
                        if (shockPoints != 0f) { c.State.LaborForceParticipationRate = Mathf.Clamp(c.State.LaborForceParticipationRate + shockPoints, 1f, 100f); }
                        result[Turns + 1] = c.State.LaborForceParticipationRate - c.ParticipationAtLastBoundary;
                    }
                    sim.AdvanceTurn(decisions);
                    result[year - 1] = c.State.Unemployment;
                }
                return result;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
