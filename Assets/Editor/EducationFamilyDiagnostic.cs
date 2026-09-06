using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C3 (2026-09-06): the education family's proof. (1) The seeds are the spine's tables - attainment for six summing to 100 within a tenth,
    /// early leavers for five with the USA ABSENT, students per teacher for six, every country carrying a spending base. (2) Sweden at no policy
    /// for twenty years: every figure inside its guard, the distribution still summing to 100, the USA's leavers absent throughout. (3) Sweden
    /// with its education lines cut through the decision each year against the untouched run: students per teacher HIGHER, early leavers HIGHER,
    /// the below-upper-secondary share no lower - the couplings move the stated way. No PISA figure exists anywhere in the model (a fetch, ruled).
    /// </summary>
    public static class EducationFamilyDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;

            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedTertiary = new Dictionary<CountryId, float> { { CountryId.Sweden, 50.8f }, { CountryId.Germany, 35.5f }, { CountryId.France, 43.4f }, { CountryId.Italy, 22.3f }, { CountryId.Poland, 40.0f }, { CountryId.USA, 52.2f } };
            var expectedLeavers = new Dictionary<CountryId, float> { { CountryId.Sweden, 6.7f }, { CountryId.Germany, 13.1f }, { CountryId.France, 7.2f }, { CountryId.Italy, 8.2f }, { CountryId.Poland, 4.0f }, { CountryId.USA, EducationSeeds.Absent } };
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedTertiary.ContainsKey(c.Id)) { continue; }
                EconomyState s = c.State;
                if (!c.Education.Seeded) { Debug.LogError($"EDUCATION: {c.Id} carries no seeded family."); ok = false; continue; }
                if (Mathf.Abs(s.AttainmentTertiary - expectedTertiary[c.Id]) > 1e-4f) { Debug.LogError($"EDUCATION: {c.Id} tertiary seeds {s.AttainmentTertiary}, the spine says {expectedTertiary[c.Id]}."); ok = false; }
                float sum = s.AttainmentBelowUpperSecondary + s.AttainmentUpperSecondary + s.AttainmentTertiary;
                if (Mathf.Abs(sum - 100f) > 0.15f) { Debug.LogError($"EDUCATION: {c.Id} attainment sums to {sum:F2}, not 100 (the OECD's three bands, rounded)."); ok = false; }
                bool hasLeavers = s.EarlyLeavers >= 0f;
                if (hasLeavers != (expectedLeavers[c.Id] >= 0f)) { Debug.LogError($"EDUCATION: {c.Id} early leavers {(hasLeavers ? "seeded" : "absent")}; the spine says otherwise."); ok = false; }
                if (hasLeavers && Mathf.Abs(s.EarlyLeavers - expectedLeavers[c.Id]) > 1e-4f) { Debug.LogError($"EDUCATION: {c.Id} early leavers seed {s.EarlyLeavers}, the spine says {expectedLeavers[c.Id]}."); ok = false; }
                if (s.StudentsPerTeacherPrimary <= 0f || s.StudentsPerTeacherLowerSecondary <= 0f) { Debug.LogError($"EDUCATION: {c.Id} students per teacher not seeded."); ok = false; }
                if (c.Education.SpendPerPupilSeed <= 0f) { Debug.LogError($"EDUCATION: {c.Id} has no spending base - the education lines were not found."); ok = false; }
            }

            float[] untouched = RunSweden(Years, cutShare: 0f, out bool guardsHeld, out bool usaAbsent, out float sumDrift);
            if (!guardsHeld) { Debug.LogError("EDUCATION: a figure left its runaway guard in twenty years at no policy."); ok = false; }
            if (!usaAbsent) { Debug.LogError("EDUCATION: the USA's early leavers stopped being absent."); ok = false; }
            if (sumDrift > 0.2f) { Debug.LogError($"EDUCATION: the attainment distribution drifted from 100 by {sumDrift:F3} at no policy."); ok = false; }

            float[] cut = RunSweden(Years, cutShare: -0.2f, out _, out _, out _);
            if (!(cut[0] > untouched[0]) || !(cut[1] > untouched[1])) { Debug.LogError($"EDUCATION: with the education lines cut, students per teacher ({cut[0]:F2} / {cut[1]:F2}) are not above the untouched run ({untouched[0]:F2} / {untouched[1]:F2})."); ok = false; }
            if (!(cut[2] > untouched[2])) { Debug.LogError($"EDUCATION: with the education lines cut, early leavers {cut[2]:F2} are not above the untouched run's {untouched[2]:F2}."); ok = false; }
            if (!(cut[3] >= untouched[3] - 1e-4f)) { Debug.LogError($"EDUCATION: with the education lines cut, the below-upper-secondary share {cut[3]:F3} is below the untouched run's {untouched[3]:F3}."); ok = false; }

            Debug.Log($"EDUCATION: seeds - attainment for six summing to 100, early leavers for five (the USA absent), students per teacher for six. Sweden after {Years} years - untouched: students per teacher {untouched[0]:F1} / {untouched[1]:F1}, early leavers {untouched[2]:F2} %, below upper secondary {untouched[3]:F2} %; "
                + $"education lines cut through the decision each year (-20 % asked, the seed range deciding): {cut[0]:F1} / {cut[1]:F1}, {cut[2]:F2} %, {cut[3]:F2} % - more pupils per teacher, more leavers, the stock no better: the couplings move the stated way. No PISA figure exists in the model: the row is a fetch with no score.");
            Debug.Log(ok ? "EDUCATION: PASS - the seeds are the spine's, absent stays absent, the distribution holds 100, the couplings move the stated way." : "EDUCATION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] RunSweden(int years, float cutShare, out bool guardsHeld, out bool usaAbsent, out float sumDrift)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("EDUCATION");
            guardsHeld = true; usaAbsent = true; sumDrift = 0f;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                Country us = world.GetCountry(CountryId.USA);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (cutShare != 0f)
                    {
                        foreach (SpendingLine line in se.SpendingLines) { if (EducationFamily.IsEducationLine(line.Category)) { d.SpendingLineChanges[line.Category] = cutShare * 100f; } }
                    }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    EconomyState s = se.State;
                    if (s.StudentsPerTeacherPrimary < EducationFamily.MinRatio || s.StudentsPerTeacherPrimary > EducationFamily.MaxRatio || s.EarlyLeavers > EducationFamily.MaxLeavers) { guardsHeld = false; }
                    if (us.State.EarlyLeavers >= 0f) { usaAbsent = false; }
                    sumDrift = Mathf.Max(sumDrift, Mathf.Abs(s.AttainmentBelowUpperSecondary + s.AttainmentUpperSecondary + s.AttainmentTertiary - 100f));
                }
                return new[] { se.State.StudentsPerTeacherPrimary, se.State.StudentsPerTeacherLowerSecondary, se.State.EarlyLeavers, se.State.AttainmentBelowUpperSecondary };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
