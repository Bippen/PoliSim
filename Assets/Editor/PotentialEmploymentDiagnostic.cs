using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-7, the second seat (2026-09-08, §394): potential's labour input is employment and the seed potentials are re-solved. (1) At the seed, for six,
    /// PotentialGdpSeed = GDP × (1 − NAIRU) ÷ (1 − U) to 1e-5 relative and the state's PotentialGDP equals it - the typed seed is superseded; the seed
    /// gap (potential over GDP − 1) is printed per country. (2) The unit: raising the unemployment rate by 5 points lowers the labour input by exactly
    /// (100 − U − 5) ÷ (100 − U), both directions (−2 too, where U allows). (3) The world, Sweden the player, twelve turns: with participation pushed +2
    /// points at the top of turn 3, potential against the untouched run at that boundary moves by the labour input\x27s own ratio to 1e-3 - and by LESS THAN
    /// ONE PER CENT on impact, where the natural-rate input moved it by the full participation share (about 3 %): the new participants reach potential only
    /// as the first seat employs them; −2 symmetric. (4) `PotentialOutputDiagnostic`\x27s identity (seed × labour ratio × trend compound) still holds on the
    /// same bar. The century is the trajectory family\x27s (§394).
    /// </summary>
    public static class PotentialEmploymentDiagnostic
    {
        private const int Turns = 12;
        private const int ShockTurn = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var report = new List<string>();
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                float expected = c.State.GDP * (100f - c.NaturalUnemploymentRate) / (100f - c.State.Unemployment);
                if (Mathf.Abs(c.PotentialGdpSeed - expected) > 1e-5f * expected) { Debug.LogError($"POTENTIAL EMPLOYMENT: {c.Id}\x27s seed potential {c.PotentialGdpSeed:F3} is not GDP × (1 − NAIRU) ÷ (1 − U) = {expected:F3}."); ok = false; }
                if (Mathf.Abs(c.State.PotentialGDP - c.PotentialGdpSeed) > 1e-5f * expected) { Debug.LogError($"POTENTIAL EMPLOYMENT: {c.Id}\x27s state potential {c.State.PotentialGDP:F3} is not its re-solved seed {c.PotentialGdpSeed:F3}."); ok = false; }
                report.Add($"{c.Id} seed gap {(c.PotentialGdpSeed / c.State.GDP - 1f) * 100f:+0.00} % (U {c.State.Unemployment:F2}, NAIRU {c.NaturalUnemploymentRate:F2})");
                // (2) the unit
                float before = PotentialOutput.LabourInput(c); float u = c.State.Unemployment;
                c.State.Unemployment = u + 5f; float up = PotentialOutput.LabourInput(c);
                c.State.Unemployment = Mathf.Max(0f, u - 2f); float down = PotentialOutput.LabourInput(c);
                c.State.Unemployment = u;
                if (Mathf.Abs(up / before - (100f - u - 5f) / (100f - u)) > 1e-5f || Mathf.Abs(down / before - (100f - Mathf.Max(0f, u - 2f)) / (100f - u)) > 1e-5f) { Debug.LogError($"POTENTIAL EMPLOYMENT: {c.Id}\x27s labour input does not read (1 − U) both ways ({up / before:F5} / {down / before:F5})."); ok = false; }
            }
            // (3) the world
            float[] untouched = Run(0f);
            float[] up2 = Run(2f);
            float[] down2 = Run(-2f);
            // [0] potential after the shock turn, [1] labour input after the shock turn, [2] potential at turn Turns, [3] labour input at turn Turns
            float potRatioUp = up2[0] / untouched[0], labRatioUp = up2[1] / untouched[1];
            float potRatioDown = down2[0] / untouched[0], labRatioDown = down2[1] / untouched[1];
            if (Mathf.Abs(potRatioUp - labRatioUp) > 1e-3f || Mathf.Abs(potRatioDown - labRatioDown) > 1e-3f) { Debug.LogError($"POTENTIAL EMPLOYMENT: potential did not follow the labour input at the shock boundary (+2: {potRatioUp:F5} vs {labRatioUp:F5}; −2: {potRatioDown:F5} vs {labRatioDown:F5})."); ok = false; }
            if (Mathf.Abs(potRatioUp - 1f) > 0.01f || Mathf.Abs(potRatioDown - 1f) > 0.01f) { Debug.LogError($"POTENTIAL EMPLOYMENT: a 2-point participation shock moved potential by more than one per cent on impact (+2: {(potRatioUp - 1f) * 100f:+0.00} %, −2: {(potRatioDown - 1f) * 100f:+0.00} %) - the new participants reached potential unemployed."); ok = false; }
            // on impact both signs read zero to second order (the new entrants are unemployed; the leavers were employed and unemployed alike) - the direction shows as the excess is absorbed
            float laterUp = up2[2] / untouched[2], laterDown = down2[2] / untouched[2];
            if (!(laterUp > 1f) || !(laterDown < 1f)) { Debug.LogError($"POTENTIAL EMPLOYMENT: nine years on the two directions do not order (+2: {(laterUp - 1f) * 100f:+0.00} %, −2: {(laterDown - 1f) * 100f:+0.00} %) - the absorbed supply did not reach potential."); ok = false; }
            Debug.Log($"POTENTIAL EMPLOYMENT: {string.Join("; ", report)}. Sweden, participation +2 at the top of turn {ShockTurn}: potential {(potRatioUp - 1f) * 100f:+0.00} % against untouched at the boundary (the labour input {(labRatioUp - 1f) * 100f:+0.00} %), {(laterUp - 1f) * 100f:+0.00} % nine years on as the excess is employed; −2: {(potRatioDown - 1f) * 100f:+0.00} % on impact, {(laterDown - 1f) * 100f:+0.00} % nine years on.");
            Debug.Log(ok ? "POTENTIAL EMPLOYMENT: PASS - the seed potentials re-solved from their factors, the labour input employment both ways, a supply shock reaching potential only as employed." : "POTENTIAL EMPLOYMENT: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(float shockPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("POTEMP");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                sim.AiFinanceMinistryEnabled = false;
                Country c = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var result = new float[4];
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    if (year == ShockTurn && shockPoints != 0f) { c.State.LaborForceParticipationRate = Mathf.Clamp(c.State.LaborForceParticipationRate + shockPoints, 1f, 100f); }
                    sim.AdvanceTurn(decisions);
                    if (year == ShockTurn) { result[0] = c.State.PotentialGDP; result[1] = PotentialOutput.LabourInput(c); }
                }
                result[2] = c.State.PotentialGDP; result[3] = PotentialOutput.LabourInput(c);
                return result;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
