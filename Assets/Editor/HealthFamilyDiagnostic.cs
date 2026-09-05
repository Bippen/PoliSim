using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C2 (2026-09-05): the health family's proof. (1) The seeds are the spine's tables, six of six on coverage and treatable
    /// mortality, three of six on the waits with the other three ABSENT (-1), five of six on the supporting readouts. (2) Sweden
    /// at no policy for twenty years: every figure stays within the runaway guards and coverage never leaves its ceiling; Germany's
    /// waits stay absent through the run (the coupling does not run on an absent figure). (3) Sweden with its health line held a
    /// fifth below the seed for twenty years against the untouched run: treatable mortality HIGHER and both waits LONGER - the
    /// couplings move the stated way. (4) The elasticity Poland's 106 against Sweden's 45 implies on the game's own seeded spending
    /// per head is PRINTED beside the authored one - read together, never tuned to meet.
    /// </summary>
    public static class HealthFamilyDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;

            // (1) the seeds
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedTm = new Dictionary<CountryId, float> { { CountryId.Sweden, 45f }, { CountryId.Germany, 63f }, { CountryId.France, 46f }, { CountryId.Italy, 51f }, { CountryId.Poland, 106f }, { CountryId.USA, 92f } };
            var expectedCoverage = new Dictionary<CountryId, float> { { CountryId.Sweden, 100f }, { CountryId.Germany, 99.9f }, { CountryId.France, 99.9f }, { CountryId.Italy, 100f }, { CountryId.Poland, 92.1f }, { CountryId.USA, 91.8f } };
            var reportsWaits = new HashSet<CountryId> { CountryId.Sweden, CountryId.Italy, CountryId.Poland };
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedTm.ContainsKey(c.Id)) { continue; }
                if (!c.Health.Seeded) { Debug.LogError($"HEALTH: {c.Id} carries no seeded family."); ok = false; continue; }
                if (Mathf.Abs(c.State.TreatableMortality - expectedTm[c.Id]) > 1e-4f) { Debug.LogError($"HEALTH: {c.Id} treatable mortality seeds {c.State.TreatableMortality}, the spine says {expectedTm[c.Id]}."); ok = false; }
                if (Mathf.Abs(c.State.HealthCoverage - expectedCoverage[c.Id]) > 1e-4f) { Debug.LogError($"HEALTH: {c.Id} coverage seeds {c.State.HealthCoverage}, the spine says {expectedCoverage[c.Id]}."); ok = false; }
                bool has = c.State.WaitCataractDays >= 0f && c.State.WaitKneeDays >= 0f;
                if (has != reportsWaits.Contains(c.Id)) { Debug.LogError($"HEALTH: {c.Id} waits {(has ? "seeded" : "absent")}; the spine says {(reportsWaits.Contains(c.Id) ? "three of six report and this is one" : "absent and stated")}."); ok = false; }
                if (c.Health.SpendPerHeadSeed <= 0f || c.Health.SpendPerAgeCostSeed <= 0f) { Debug.LogError($"HEALTH: {c.Id} has no spending base ({c.Health.SpendPerHeadSeed:F4} per head, {c.Health.SpendPerAgeCostSeed:F4} per age-cost unit) - the health line was not found."); ok = false; }
                if (c.Id == CountryId.France && c.Health.HasSupporting) { Debug.LogError("HEALTH: France carries supporting readouts; the spine says absent."); ok = false; }
                if (c.Id == CountryId.USA && Mathf.Abs(c.Health.CoverageCeiling - 91.8f) > 1e-4f) { Debug.LogError($"HEALTH: the USA's coverage ceiling is {c.Health.CoverageCeiling}; the spine says its own 91.8."); ok = false; }
            }
            Country se = seedWorld.GetCountry(CountryId.Sweden), pl = seedWorld.GetCountry(CountryId.Poland);
            float implied = HealthFamily.ImpliedQualityElasticity(pl, se);
            Debug.Log($"HEALTH: seeds - six of six on coverage and treatable mortality, waits for Sweden, Italy and Poland only, supporting readouts for five. "
                + $"Real health spending per head at the seed: Sweden {se.Health.SpendPerHeadSeed:F4}, Poland {pl.Health.SpendPerHeadSeed:F4} ($B per million, the game's own lines); "
                + $"the elasticity Poland's 106 against Sweden's 45 IMPLIES on that gap is q = {implied:F2}; the authored q is {HealthFamily.QualitySpendingElasticity:F2} - read together, not tuned.");

            // (2) twenty years at no policy, Sweden the player; Germany's waits watched
            float[] untouched = RunSweden(Years, heldShare: 0f, out bool guardsHeld, out bool germanyAbsent, out float coverageMin);
            if (!guardsHeld) { Debug.LogError("HEALTH: a figure left its runaway guard in twenty years at no policy."); ok = false; }
            if (!germanyAbsent) { Debug.LogError("HEALTH: Germany's waits stopped being absent - the coupling ran on an absent figure."); ok = false; }
            if (coverageMin < 99f) { Debug.LogError($"HEALTH: Sweden's coverage fell to {coverageMin:F2} at no policy; a country at its ceiling stays there until spending per head falls."); ok = false; }

            // (3) the health line held a fifth below the seed for twenty years
            float[] starved = RunSweden(Years, heldShare: -0.2f, out _, out _, out _);
            if (!(starved[0] > untouched[0])) { Debug.LogError($"HEALTH: treatable mortality with the health line a fifth down ({starved[0]:F2}) is not above the untouched run ({untouched[0]:F2})."); ok = false; }
            if (!(starved[1] > untouched[1]) || !(starved[2] > untouched[2])) { Debug.LogError($"HEALTH: the waits with the health line a fifth down ({starved[1]:F1} / {starved[2]:F1}) are not longer than the untouched run's ({untouched[1]:F1} / {untouched[2]:F1})."); ok = false; }
            if (!(starved[3] <= untouched[3])) { Debug.LogError($"HEALTH: coverage with the health line a fifth down ({starved[3]:F2}) is above the untouched run ({untouched[3]:F2})."); ok = false; }
            Debug.Log($"HEALTH: Sweden after {Years} years - untouched: treatable mortality {untouched[0]:F1}, cataract {untouched[1]:F1} d, knee {untouched[2]:F1} d, coverage {untouched[3]:F2} %; "
                + $"health lines cut through the decision each year (-20 % asked, the seed range deciding): {starved[0]:F1}, {starved[1]:F1} d, {starved[2]:F1} d, {starved[3]:F2} % - quality worse, waits longer, coverage no higher: the couplings move the stated way.");

            Debug.Log(ok ? "HEALTH: PASS - the seeds are the spine's, absent stays absent, the couplings move the stated way." : "HEALTH: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>Runs Sweden for <paramref name="years"/> turns; a nonzero <paramref name="heldShare"/> pins every health line at seed x (1 + share) each year
        /// before the turn resolves. Returns treatable mortality, the two waits and coverage at the end.</summary>
        private static float[] RunSweden(int years, float heldShare, out bool guardsHeld, out bool germanyAbsent, out float coverageMin)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("HEALTH");
            guardsHeld = true; germanyAbsent = true; coverageMin = 100f;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                Country de = world.GetCountry(CountryId.Germany);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (heldShare != 0f)
                    {
                        // P5-C7: the cut goes through the DECISION (the mechanic's own path - the request is the indexed line, the allocation the player's figure), so the
                        // Health ministry's effectiveness falls below one and the waits, which read it, lengthen; the seed-range clamp decides how much of the ask lands.
                        foreach (SpendingLine line in se.SpendingLines) { if (HealthFamily.IsHealthLine(line.Category)) { d.SpendingLineChanges[line.Category] = heldShare * 100f; } }
                    }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    EconomyState s = se.State;
                    if (s.TreatableMortality < HealthFamily.MinTreatableMortality || s.TreatableMortality > HealthFamily.MaxTreatableMortality || s.WaitKneeDays > HealthFamily.MaxWaitDays || s.HealthCoverage > se.Health.CoverageCeiling + 1e-3f) { guardsHeld = false; }
                    if (de.State.WaitCataractDays >= 0f || de.State.WaitKneeDays >= 0f) { germanyAbsent = false; }
                    coverageMin = Mathf.Min(coverageMin, s.HealthCoverage);
                }
                return new[] { se.State.TreatableMortality, se.State.WaitCataractDays, se.State.WaitKneeDays, se.State.HealthCoverage };
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
