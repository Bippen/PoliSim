using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C5 (2026-09-06): the environment family's proof. (1) The seeds are the spine's EDGAR 2023 figures for six (greenhouse gases, power and
    /// transport CO₂ per person), the headline at the seed equal to the seed; no figure exists for the electricity mix. (2) Sweden at no policy for
    /// twenty years: both intensities inside their guards and the headline never below the sum of its sector keys. (3) Sweden with its carbon tax
    /// raised twenty points through the decision against the untouched run: transport CO₂ per person LOWER and the headline lower, power HELD (Sweden
    /// has no dispatchable fossil fleet, and since EN-4d, §467, the national carbon tax reaches no fleet - the ETS is the fleet's carbon price) - the
    /// couplings move the stated way; the carbon tax's own revenue base is the taxed CO₂ since the feedback pass of 2026-09-07, transport's since EN-4d (EnvironmentFeedbackDiagnostic proves both moves).
    /// </summary>
    public static class EnvironmentFamilyDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedGhg = new Dictionary<CountryId, float> { { CountryId.Sweden, 4.76f }, { CountryId.Germany, 8.26f }, { CountryId.France, 5.81f }, { CountryId.Italy, 6.36f }, { CountryId.Poland, 9.67f }, { CountryId.USA, 17.61f } };
            var expectedPower = new Dictionary<CountryId, float> { { CountryId.Sweden, 0.56f }, { CountryId.Germany, 2.13f }, { CountryId.France, 0.35f }, { CountryId.Italy, 1.43f }, { CountryId.Poland, 3.21f }, { CountryId.USA, 4.35f } };
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedGhg.ContainsKey(c.Id)) { continue; }
                if (!c.Environment.Seeded) { Debug.LogError($"ENVIRONMENT: {c.Id} carries no seeded family."); ok = false; continue; }
                if (Mathf.Abs(EnvironmentFamily.GhgPerCapitaNow(c) - expectedGhg[c.Id]) > 1e-3f) { Debug.LogError($"ENVIRONMENT: {c.Id} headline at the seed {EnvironmentFamily.GhgPerCapitaNow(c):F2}, the spine says {expectedGhg[c.Id]}."); ok = false; }
                if (Mathf.Abs(c.State.PowerCo2PerCapita - expectedPower[c.Id]) > 1e-4f) { Debug.LogError($"ENVIRONMENT: {c.Id} power CO₂ seeds {c.State.PowerCo2PerCapita}, the spine says {expectedPower[c.Id]}."); ok = false; }
                if (c.State.TransportCo2PerCapita <= 0f) { Debug.LogError($"ENVIRONMENT: {c.Id} transport CO₂ not seeded."); ok = false; }
            }
            // §342: the electricity mix - seven shares summing to 100 within half a point for six; Germany's coal share against Eurostat nrg_bal_peh 2023 (124 786 of 522 871 GWh = 23.9 %) within 1.5 points, the cross-check gate's own figure.
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedGhg.ContainsKey(c.Id)) { continue; }
                if (!c.Environment.HasMix) { Debug.LogError($"ENVIRONMENT: {c.Id} carries no electricity mix."); ok = false; continue; }
                float sum = 0f; foreach (float v in c.Environment.MixShares) { sum += v; }
                if (Mathf.Abs(sum - 100f) > 0.5f) { Debug.LogError($"ENVIRONMENT: {c.Id}'s mix sums to {sum:F2}, not 100."); ok = false; }
            }
            if (Mathf.Abs(seedWorld.GetCountry(CountryId.Germany).Environment.MixShares[0] - 23.9f) > 1.5f) { Debug.LogError($"ENVIRONMENT: Germany's coal share {seedWorld.GetCountry(CountryId.Germany).Environment.MixShares[0]:F1} is not within 1.5 points of Eurostat's 23.9."); ok = false; }
            Country seSeed = seedWorld.GetCountry(CountryId.Sweden);
            bool hasCarbonTax = false;
            foreach (TaxLine line in seSeed.TaxLines) { if (line.Type == TaxType.CarbonTax && line.IsImplemented) { hasCarbonTax = true; } }
            if (!hasCarbonTax) { Debug.LogError("ENVIRONMENT: Sweden has no implemented carbon tax line to move - the coupling's lever is missing."); ok = false; }

            float[] untouched = RunSweden(Years, taxDeltaPoints: 0f, out bool guardsHeld, out bool headlineHeld);
            if (!guardsHeld) { Debug.LogError("ENVIRONMENT: an intensity left its runaway guard in twenty years at no policy."); ok = false; }
            if (!headlineHeld) { Debug.LogError("ENVIRONMENT: the headline fell below the sum of its sector keys."); ok = false; }
            float[] taxed = RunSweden(Years, taxDeltaPoints: 20f, out _, out _);
            // EN-3 (2026-09-11): Sweden's power figure is written by the dispatch, and Sweden has no dispatchable fossil fleet (its fossil thermal is heat-led CHP), so a carbon tax cannot lower
            // it - the figure HOLDS; transport and the headline still fall through the readout coupling. Since EN-4d (§467) no fleet reads the national carbon tax at all: the fleet's
            // carbon price is the ETS, and EnergyMarketDiagnostic proves the mechanism where the coal is, Poland, by stepping that price.
            if (!(taxed[0] <= untouched[0] + 1e-6f) || !(taxed[1] < untouched[1]) || !(taxed[2] < untouched[2])) { Debug.LogError($"ENVIRONMENT: with the carbon tax up twenty points, power {taxed[0]:F3} / transport {taxed[1]:F3} / headline {taxed[2]:F3} are not below untouched {untouched[0]:F3} / {untouched[1]:F3} / {untouched[2]:F3}."); ok = false; }

            Debug.Log($"ENVIRONMENT: seeds - greenhouse gases and power and transport CO₂ per person for six (EDGAR 2024, 2023), the electricity mix a fetch. Sweden after {Years} years - untouched: power {untouched[0]:F2} t, transport {untouched[1]:F2} t, headline {untouched[2]:F2} t; "
                + $"carbon tax raised twenty points through the decision: {taxed[0]:F2} t (held), {taxed[1]:F2} t, {taxed[2]:F2} t - transport and the headline lower: the couplings move the stated way. The tax's revenue base is the taxed CO₂ since 2026-09-07, transport's tonnes since EN-4d (EnvironmentFeedbackDiagnostic).");
            Debug.Log(ok ? "ENVIRONMENT: PASS - the seeds are the spine's, the headline is its keys, the couplings move the stated way." : "ENVIRONMENT: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] RunSweden(int years, float taxDeltaPoints, out bool guardsHeld, out bool headlineHeld)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ENVIRONMENT");
            guardsHeld = true; headlineHeld = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    // an override, not a delta: the rate asked for in year 1 - the seed as the statute will have indexed it at this boundary plus the raise (EN-4e: the statute steps before the override, so an
                    // override of the pre-index figure plus twenty would CUT below the indexed path when the year's inflation exceeds 1.5 per cent - the first run read it so); thereafter the statute's
                    if (taxDeltaPoints != 0f && year == 1) { d.TaxRateOverrides[TaxType.CarbonTax] = EnvironmentFamily.CarbonTaxRate(se) * CarbonRateStatute.YearRatio(se) + taxDeltaPoints; }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    EconomyState s = se.State;
                    if (s.PowerCo2PerCapita < EnvironmentFamily.MinIntensity || s.PowerCo2PerCapita > EnvironmentFamily.MaxIntensity || s.TransportCo2PerCapita > EnvironmentFamily.MaxIntensity) { guardsHeld = false; }
                    if (EnvironmentFamily.GhgPerCapitaNow(se) < s.PowerCo2PerCapita + s.TransportCo2PerCapita - 1e-4f) { headlineHeld = false; }
                }
                return new[] { se.State.PowerCo2PerCapita, se.State.TransportCo2PerCapita, EnvironmentFamily.GhgPerCapitaNow(se) };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
