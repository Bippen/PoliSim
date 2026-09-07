using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-3 (2026-09-07, §377) - what LANDED after the measured stop. (1) The household burden gap is zero for six at the seed. (2) Sweden over six turns:
    /// the income tax +5 points reads a POSITIVE gap and −5 a negative one (the rate reaches consumption, C-N4). (3) A household tax the country did not
    /// levy at the seed, implemented at 5 %, reads a POSITIVE gap - the C-N4 anchor defect closed here (before, its "seed" rate was today's and it cancelled
    /// itself). (4) Unemployment +5 points on turn 3 leaves the gap where the untouched run has it, to 1e-6: the base is read on both sides and cancels BY
    /// DESIGN - the stabiliser's tax side is NOT in this term; §377 measured the seed-share anchor and stopped it (the equivalence bar, the secular drift).
    /// </summary>
    public static class HouseholdBurdenAnchorDiagnostic
    {
        private const int Turns = 6;
        private const int ShockTurn = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                float gap = MacroSystem.HouseholdBurdenGap(c);
                if (Mathf.Abs(gap) > 1e-5f) { Debug.LogError($"BURDEN ANCHOR: {c.Id} carries a nonzero burden gap at the seed ({gap:R})."); ok = false; }
            }
            float untouched = Run(0f, 0f, false);
            float rateUp = Run(5f, 0f, false);
            float rateDown = Run(-5f, 0f, false);
            float jobsShock = Run(0f, 5f, false);
            float newTax = Run(0f, 0f, true);
            if (!(rateUp > untouched + 1e-4f) || !(rateDown < untouched - 1e-4f)) { Debug.LogError($"BURDEN ANCHOR: the income tax +/-5 points does not move the gap both ways ({rateUp:F5} / {untouched:F5} / {rateDown:F5})."); ok = false; }
            if (Mathf.Abs(jobsShock - untouched) > 1e-6f) { Debug.LogError($"BURDEN ANCHOR: unemployment +5 points moved the gap ({jobsShock:F6} vs {untouched:F6}) - the base no longer cancels; that is FT-3's stopped form, not this build."); ok = false; }
            if (float.IsNaN(newTax)) { Debug.Log("BURDEN ANCHOR: Sweden levies every household tax at the seed - the new-tax anchor is not testable on it (stated, not failed)."); }
            else if (!(newTax > untouched + 1e-4f)) { Debug.LogError($"BURDEN ANCHOR: a household tax absent at the seed, implemented at 5 %, does not raise the gap ({newTax:F5} vs {untouched:F5})."); ok = false; }
            Debug.Log($"BURDEN ANCHOR: Sweden at turn {Turns} - untouched gap {untouched:F6}; income tax +5: {rateUp:F5}; -5: {rateDown:F5}; unemployment +5 on turn {ShockTurn}: {jobsShock:F6} (the base cancels by design); "
                + (float.IsNaN(newTax) ? "new-tax anchor untestable." : $"a household tax absent at the seed at 5 %: {newTax:F5}."));
            Debug.Log(ok ? "BURDEN ANCHOR: PASS - zero at the seed, the rate both ways, the base cancels, a new tax anchors at zero." : "BURDEN ANCHOR: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float Run(float ratePoints, float shockPoints, bool implementNewTax)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("BURDEN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                float seedRate = 0f; foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax) { seedRate = line.Rate; } }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                bool newTaxFound = false;
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    if (shockPoints != 0f && year == ShockTurn) { c.State.Unemployment = Mathf.Clamp(c.State.Unemployment + shockPoints, 0f, 60f); }
                    if (implementNewTax && year == ShockTurn)
                    {
                        foreach (TaxLine line in c.TaxLines)
                        {
                            if (line.IsImplemented || line.Type == TaxType.CorporateTax || line.Type == TaxType.Tariffs) { continue; }
                            line.IsImplemented = true; line.Rate = 5f; newTaxFound = true; break;
                        }
                    }
                    PolicyDecision d = PolicyDecision.None();
                    if (ratePoints != 0f) { d.TaxRateOverrides[TaxType.IncomeTax] = seedRate + ratePoints; }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                }
                if (implementNewTax && !newTaxFound) { return float.NaN; }
                return MacroSystem.HouseholdBurdenGap(c);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
