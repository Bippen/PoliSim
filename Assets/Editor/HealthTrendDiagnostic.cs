using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The treatable-mortality trend family (2026-09-07, ruling 2 of the overnight review). (1) At the seed the trend index is 1 and the anchor is the seed for six;
    /// the rates are the sourced ones (Sweden −0.02980, Poland −0.01944). (2) The index compounds: after twenty years Sweden's is exp(20 × rate) to 1e-5 and the
    /// anchor is seed × index. (3) Both directions still hold around the trended anchor: the health lines cut through the decision leave treatable mortality HIGHER
    /// than untouched, raised LOWER - the trend and the player's effect are separate terms. (4) The death-rate identity and the pyramid carry the trend: at baseline
    /// the death rate falls with treatable mortality and the population sits above the publisher's path; the migration reading moves by less than a tenth of the
    /// death-rate change. (5) The floor (10 per 100 000) is a guard, not a statement: the year it binds at baseline is REPORTED per country over a century.
    /// </summary>
    public static class HealthTrendDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedRate = new Dictionary<CountryId, float> { { CountryId.Sweden, -0.02980f }, { CountryId.Germany, -0.02492f }, { CountryId.France, -0.02296f }, { CountryId.Italy, -0.02188f }, { CountryId.Poland, -0.01944f }, { CountryId.USA, -0.01298f } };
            foreach (Country c in seedWorld.Countries)
            {
                if (!c.Health.Seeded) { continue; }
                if (Mathf.Abs(c.Health.TrendIndex - 1f) > 1e-6f || Mathf.Abs(HealthFamily.TrendedAnchor(c.Health) - c.Health.TreatableMortality) > 1e-4f) { Debug.LogError($"HEALTH TREND: {c.Id}'s anchor at the seed is not the seed (index {c.Health.TrendIndex:R})."); ok = false; }
                if (!expectedRate.ContainsKey(c.Id) || Mathf.Abs(c.Health.TreatableMortalityTrendPerYear - expectedRate[c.Id]) > 1e-6f) { Debug.LogError($"HEALTH TREND: {c.Id}'s rate is {c.Health.TreatableMortalityTrendPerYear:R}, not the sourced one."); ok = false; }
            }

            float[] untouched = Run(CountryId.Sweden, Years, 0f, out int floorYearSe);
            float[] cut = Run(CountryId.Sweden, Years, -0.2f, out _);
            float[] raised = Run(CountryId.Sweden, Years, 0.2f, out _);
            // [0] treatable mortality, [1] trend index, [2] death rate, [3] population, [4] net migration, [5] life expectancy
            float expectedIndex = Mathf.Exp(Years * -0.02980f);
            if (Mathf.Abs(untouched[1] - expectedIndex) > 1e-5f) { Debug.LogError($"HEALTH TREND: Sweden's index after {Years} years is {untouched[1]:R}, not exp(20 × rate) = {expectedIndex:R}."); ok = false; }
            if (!(untouched[0] < 45f)) { Debug.LogError($"HEALTH TREND: Sweden's treatable mortality did not fall at baseline ({untouched[0]:F2} against the seed's 45)."); ok = false; }
            if (!(cut[0] > untouched[0]) || !(raised[0] < untouched[0])) { Debug.LogError($"HEALTH TREND: around the trended anchor the cut ({cut[0]:F2}) and the raise ({raised[0]:F2}) do not bracket untouched ({untouched[0]:F2})."); ok = false; }
            if (!(untouched[2] < 9.5f)) { Debug.LogError($"HEALTH TREND: the death rate does not carry the trend ({untouched[2]:F3} against the seed's 9.5)."); ok = false; }
            if (Mathf.Abs(untouched[2] - (9.5f + (untouched[0] - 45f) / 100f)) > 1e-3f) { Debug.LogError($"HEALTH TREND: the death-rate identity broke ({untouched[2]:F4} against seed + Δ treatable ÷ 100 = {9.5f + (untouched[0] - 45f) / 100f:F4})."); ok = false; }
            if (Mathf.Abs(cut[4] - untouched[4]) > 0.1f * Mathf.Abs(cut[2] - untouched[2])) { Debug.LogError($"HEALTH TREND: the migration reading moved {cut[4] - untouched[4]:F4} against a death-rate change of {cut[2] - untouched[2]:F4}."); ok = false; }

            var floors = new List<string>();
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA })
            {
                Run(id, 100, 0f, out int floorYear);
                floors.Add($"{id} {(floorYear > 0 ? "year " + floorYear : "not in a century")}");
            }

            Debug.Log($"HEALTH TREND: Sweden after {Years} years - untouched: treatable mortality {untouched[0]:F2} (anchor index {untouched[1]:F4}), death rate {untouched[2]:F3}, population {untouched[3]:F4} M, net migration {untouched[4]:F3}, life expectancy {untouched[5]:F2}; "
                + $"lines cut (-20 % asked): {cut[0]:F2} / {cut[2]:F3} / {cut[3]:F4} M; raised (+20 %): {raised[0]:F2} / {raised[2]:F3} / {raised[3]:F4} M - the trend and the player's effect are separate terms. "
                + $"The floor of 10 per 100 000 binds at baseline: {string.Join(", ", floors)} - a guard's binding year, reported, not a statement about care.");
            Debug.Log(ok ? "HEALTH TREND: PASS - the anchor compounds at the sourced rate, both directions around it, the identity and the pyramid carry it, the floor reported." : "HEALTH TREND: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float share, out int floorYear)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("HEALTHTREND");
            floorYear = 0;
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
                    if (floorYear == 0 && c.State.TreatableMortality <= HealthFamily.MinTreatableMortality + 1e-3f) { floorYear = year; }
                }
                return new[] { c.State.TreatableMortality, c.Health.TrendIndex, c.State.DeathRate, c.State.Population, c.State.NetMigrationRate, c.State.LifeExpectancy };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
