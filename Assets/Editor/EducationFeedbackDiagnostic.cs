using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The education feedback pass (2026-09-07): the family moves the model. (1) At the seed both terms are exactly zero for six and the wage index is the seed's.
    /// (2) Both directions, Sweden, twenty years: the education lines cut through the decision each year against untouched - the below-upper-secondary share
    /// HIGHER, participation LOWER, the summed productivity term NEGATIVE, potential LOWER; the lines raised - the opposite signs. (3) Absent stays zero: the USA's
    /// leavers are absent, so its stock holds and both terms stay exactly zero through a cut. (4) Magnitude guards: the participation gap under a point, every
    /// yearly trend term inside ±0.5, the twenty-year sum of terms inside ±2 points. (5) The gaps are the sourced ones - Sweden's activity gap 10.5, Poland's 24.3.
    /// </summary>
    public static class EducationFeedbackDiagnostic
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
                if (!c.Education.Seeded) { continue; }
                if (Mathf.Abs(EducationFamily.ParticipationTerm(c)) > 1e-6f || Mathf.Abs(EducationFamily.ProductivityTrendTerm(c)) > 1e-6f) { Debug.LogError($"EDUCATION FEEDBACK: {c.Id} carries a nonzero term at the seed."); ok = false; }
                if (c.Education.ActivityGapBelowToUpper < 0f || c.Education.RelEarnBelow < 0f) { Debug.LogError($"EDUCATION FEEDBACK: {c.Id} has no sourced gap."); ok = false; }
                if (Mathf.Abs(c.Education.WageIndexLastYear - EducationFamily.WageIndex(c.State, c.Education)) > 1e-6f) { Debug.LogError($"EDUCATION FEEDBACK: {c.Id}'s wage index at the seed is not the seed's."); ok = false; }
            }
            Country se0 = seedWorld.GetCountry(CountryId.Sweden), pl0 = seedWorld.GetCountry(CountryId.Poland);
            if (Mathf.Abs(se0.Education.ActivityGapBelowToUpper - 10.5f) > 1e-3f || Mathf.Abs(pl0.Education.ActivityGapBelowToUpper - 24.3f) > 1e-3f) { Debug.LogError($"EDUCATION FEEDBACK: the activity gaps are not the sourced ones (Sweden {se0.Education.ActivityGapBelowToUpper:F1}, Poland {pl0.Education.ActivityGapBelowToUpper:F1})."); ok = false; }

            float[] untouched = Run(CountryId.Sweden, Years, 0f);
            float[] cut = Run(CountryId.Sweden, Years, -0.2f);
            float[] raised = Run(CountryId.Sweden, Years, 0.2f);
            // [0] below share, [1] participation, [2] summed trend term, [3] potential GDP, [4] max |yearly term|
            if (!(cut[0] > untouched[0]) || !(cut[1] < untouched[1]) || !(cut[2] < 0f) || !(cut[3] < untouched[3])) { Debug.LogError($"EDUCATION FEEDBACK: a cut does not raise the below share ({cut[0]:F3} vs {untouched[0]:F3}), lower participation ({cut[1]:F4} vs {untouched[1]:F4}), sum a negative trend term ({cut[2]:F4}) and lower potential ({cut[3]:F2} vs {untouched[3]:F2})."); ok = false; }
            if (!(raised[0] < untouched[0]) || !(raised[1] > untouched[1]) || !(raised[2] > 0f) || !(raised[3] > untouched[3])) { Debug.LogError($"EDUCATION FEEDBACK: a raise does not lower the below share ({raised[0]:F3} vs {untouched[0]:F3}), lift participation ({raised[1]:F4} vs {untouched[1]:F4}), sum a positive trend term ({raised[2]:F4}) and lift potential ({raised[3]:F2} vs {untouched[3]:F2})."); ok = false; }
            if (Mathf.Abs(cut[1] - untouched[1]) > 1.0f) { Debug.LogError($"EDUCATION FEEDBACK: the participation gap after {Years} years is {cut[1] - untouched[1]:F3} points - beyond a point."); ok = false; }
            if (cut[4] > 0.5f + 1e-6f || raised[4] > 0.5f + 1e-6f) { Debug.LogError("EDUCATION FEEDBACK: a yearly trend term left its ±0.5 guard."); ok = false; }
            if (Mathf.Abs(cut[2]) > 2f || Mathf.Abs(raised[2]) > 2f) { Debug.LogError($"EDUCATION FEEDBACK: the summed trend term ({cut[2]:F3} / {raised[2]:F3}) is beyond two points in twenty years."); ok = false; }
            float[] us = Run(CountryId.USA, Years, -0.2f);
            if (us[5] != 0f || us[2] != 0f) { Debug.LogError($"EDUCATION FEEDBACK: the USA (leavers absent, the stock holds) carries a term through a cut (participation term {us[5]:R}, summed trend term {us[2]:R})."); ok = false; }

            Debug.Log($"EDUCATION FEEDBACK: Sweden after {Years} years - untouched: below upper secondary {untouched[0]:F2} %, participation {untouched[1]:F3} %, potential {untouched[3]:F1}; "
                + $"education lines cut each year (-20 % asked): {cut[0]:F2} % / {cut[1]:F3} % / trend terms summing {cut[2]:F3} points / potential {cut[3]:F1}; raised (+20 % asked): {raised[0]:F2} % / {raised[1]:F3} % / {raised[2]:F3} / {raised[3]:F1}. "
                + "The USA's terms stay zero (its leavers absent, its stock holds). Both directions, inside the guards, zero at the seed.");
            Debug.Log(ok ? "EDUCATION FEEDBACK: PASS - zero at the seed, both directions, absent stays zero, inside the guards." : "EDUCATION FEEDBACK: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float share)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("EDUFB");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                float sum = 0f, maxAbs = 0f;
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (share != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (EducationFamily.IsEducationLine(line.Category)) { d.SpendingLineChanges[line.Category] = share * 100f; } } }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    float term = EducationFamily.ProductivityTrendTerm(c);
                    sum += term; maxAbs = Mathf.Max(maxAbs, Mathf.Abs(term));
                }
                return new[] { c.State.AttainmentBelowUpperSecondary, c.State.LaborForceParticipationRate, sum, c.State.PotentialGDP, maxAbs, EducationFamily.ParticipationTerm(c) };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
