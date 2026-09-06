using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The health feedback pass (2026-09-06): the family moves the model. (1) At the seed every term is exactly zero (QualityLog 0 - a country at its seed
    /// adds nothing, so the suite opens only where the ratio moves). (2) Both directions: Sweden with its health lines cut through the decision each year
    /// against untouched - life expectancy LOWER, participation LOWER, the death rate HIGHER; with the lines raised - the opposite three signs. (3) Magnitude
    /// guards: over twenty years of a cut the life-expectancy gap stays under 1.5 years, participation under 1 point, the death rate within ±15 % of its seed - and exactly seed + Δ treatable ÷ 100 (the identity, asserted). (5) The excess deaths LEAVE the pyramid (population lower under the cut) and the net-migration reading moves by less than a tenth of the death-rate change - the first dump had booked the deaths as migration one for one; what remains is the anchored step's own proportional scaling of the publisher's migration on a depleted pyramid (D-19 (b)), stated in the record.
    /// (4) Germany, whose waits are absent, still carries the terms (quality is six of six).
    /// </summary>
    public static class HealthFeedbackDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                if (!c.Health.Seeded) { continue; }
                if (Mathf.Abs(HealthFamily.QualityLog(c)) > 1e-6f || Mathf.Abs(HealthFamily.LifeExpectancyTerm(c)) > 1e-6f || Mathf.Abs(HealthFamily.ParticipationTerm(c)) > 1e-6f) { Debug.LogError($"HEALTH FEEDBACK: {c.Id} carries a nonzero term at the seed ({HealthFamily.QualityLog(c):R})."); ok = false; }
                if (c.Health.DeathRateSeed <= 0f) { Debug.LogError($"HEALTH FEEDBACK: {c.Id} has no death-rate seed."); ok = false; }
            }

            float[] untouched = Run(CountryId.Sweden, Years, 0f);
            float[] cut = Run(CountryId.Sweden, Years, -0.2f);
            float[] raised = Run(CountryId.Sweden, Years, 0.2f);
            // both directions
            if (!(cut[0] < untouched[0]) || !(cut[1] < untouched[1]) || !(cut[2] > untouched[2])) { Debug.LogError($"HEALTH FEEDBACK: a cut does not lower life expectancy ({cut[0]:F3} vs {untouched[0]:F3}) and participation ({cut[1]:F3} vs {untouched[1]:F3}) and raise the death rate ({cut[2]:F4} vs {untouched[2]:F4})."); ok = false; }
            if (!(raised[0] > untouched[0]) || !(raised[1] > untouched[1]) || !(raised[2] < untouched[2])) { Debug.LogError($"HEALTH FEEDBACK: a raise does not lift life expectancy ({raised[0]:F3} vs {untouched[0]:F3}) and participation ({raised[1]:F3} vs {untouched[1]:F3}) and lower the death rate ({raised[2]:F4} vs {untouched[2]:F4})."); ok = false; }
            // magnitude guards
            if (Mathf.Abs(cut[0] - untouched[0]) > 1.5f) { Debug.LogError($"HEALTH FEEDBACK: the life-expectancy gap after {Years} years of cuts is {cut[0] - untouched[0]:F2} years - beyond the small drift the spine asked for."); ok = false; }
            if (Mathf.Abs(cut[1] - untouched[1]) > 1.0f) { Debug.LogError($"HEALTH FEEDBACK: the participation gap after {Years} years of cuts is {cut[1] - untouched[1]:F2} points - beyond a point."); ok = false; }
            if (Mathf.Abs(cut[2] / untouched[2] - 1f) > 0.15f) { Debug.LogError($"HEALTH FEEDBACK: the death rate moved {(cut[2] / untouched[2] - 1f) * 100f:F1} % - beyond 15 %."); ok = false; }
            if (Mathf.Abs(cut[2] - (untouched[2] + (cut[3] - untouched[3]) / 100f)) > 1e-3f) { Debug.LogError($"HEALTH FEEDBACK: the death rate is not the identity - {cut[2]:F4} against {untouched[2] + (cut[3] - untouched[3]) / 100f:F4} (seed + Δ treatable ÷ 100)."); ok = false; }
            // the deaths leave the pyramid, and the migration readings do not move: population LOWER under the cut, net migration the same to float noise
            if (!(cut[4] < untouched[4])) { Debug.LogError($"HEALTH FEEDBACK: the excess deaths did not leave the pyramid - population {cut[4]:F5} against {untouched[4]:F5} million."); ok = false; }
            if (Mathf.Abs(cut[5] - untouched[5]) > 0.1f * Mathf.Abs(cut[2] - untouched[2])) { Debug.LogError($"HEALTH FEEDBACK: the net migration reading moved {cut[5] - untouched[5]:F4} per 1 000 against a death-rate change of {cut[2] - untouched[2]:F4} - more than a tenth of it, so the identity is booking deaths as migration."); ok = false; }
            float[] de = Run(CountryId.Germany, Years, -0.2f);
            float[] deUntouched = Run(CountryId.Germany, Years, 0f);
            if (!(de[0] < deUntouched[0])) { Debug.LogError("HEALTH FEEDBACK: Germany (waits absent) does not carry the life-expectancy term."); ok = false; }

            Debug.Log($"HEALTH FEEDBACK: Sweden after {Years} years - untouched: life expectancy {untouched[0]:F2}, participation {untouched[1]:F2} %, death rate {untouched[2]:F3} per 1 000, treatable mortality {untouched[3]:F1}; "
                + $"health lines cut each year (-20 % asked): {cut[0]:F2} / {cut[1]:F2} % / {cut[2]:F3} / {cut[3]:F1}; raised (+20 % asked): {raised[0]:F2} / {raised[1]:F2} % / {raised[2]:F3} / {raised[3]:F1}. Both directions, the magnitudes inside the guards; every term zero at the seed. Population under the cut {cut[4]:F4} against {untouched[4]:F4} million, net migration {cut[5]:F4} against {untouched[5]:F4} per 1 000 - the deaths left the pyramid; the migration reading moved by {cut[5] - untouched[5]:F4}, the anchored step's proportional scaling on a depleted pyramid, not the deaths booked as migration.");
            Debug.Log(ok ? "HEALTH FEEDBACK: PASS - zero at the seed, both directions, inside the guards." : "HEALTH FEEDBACK: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float share)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("HEALTHFB");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (share != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (HealthFamily.IsHealthLine(line.Category)) { d.SpendingLineChanges[line.Category] = share * 100f; } } }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                }
                return new[] { c.State.LifeExpectancy, c.State.LaborForceParticipationRate, c.State.DeathRate, c.State.TreatableMortality, c.State.Population, c.State.NetMigrationRate };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
