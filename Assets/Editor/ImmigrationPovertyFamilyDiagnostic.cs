using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C6 (2026-09-06): the immigration-and-poverty-depth family's proof. (1) The seeds are the spine's for six, with the two definitions carried as
    /// states: the USA's migration figure is the STOCK, the five's the FLOW; the USA's gap the OECD's, the five's Eurostat's with the OECD's second
    /// reading beside it; every country's homelessness carries a definition and a year. (2) Sweden at no policy for twenty years: every figure inside
    /// its guard. (3) Sweden with border enforcement raised through the decision: the flow of apprehensions HIGHER (the flow's sign); the USA with the
    /// same: its stock LOWER (the stock's sign) - the two definitions move as their definitions say. (4) Sweden with the housing line cut through the
    /// decision each year: homelessness HIGHER; with welfare generosity raised: the poverty gap LOWER.
    /// </summary>
    public static class ImmigrationPovertyFamilyDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedMigration = new Dictionary<CountryId, float> { { CountryId.Sweden, 2.8f }, { CountryId.Germany, 29.9f }, { CountryId.France, 20.8f }, { CountryId.Italy, 18.5f }, { CountryId.Poland, 4.4f }, { CountryId.USA, 326f } };
            var expectedGap = new Dictionary<CountryId, float> { { CountryId.Sweden, 23.1f }, { CountryId.Germany, 21.7f }, { CountryId.France, 20.6f }, { CountryId.Italy, 24.6f }, { CountryId.Poland, 19.7f }, { CountryId.USA, 37.2f } };
            var expectedHomeless = new Dictionary<CountryId, float> { { CountryId.Sweden, 33f }, { CountryId.Germany, 31f }, { CountryId.France, 49f }, { CountryId.Italy, 16f }, { CountryId.Poland, 8f }, { CountryId.USA, 19f } };
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedMigration.ContainsKey(c.Id)) { continue; }
                MigrationPovertySeeds m = c.MigrationPoverty;
                if (!m.Seeded) { Debug.LogError($"MIGRATION: {c.Id} carries no seeded family."); ok = false; continue; }
                if (Mathf.Abs(c.State.IrregularMigrationPer10k - expectedMigration[c.Id]) > 1e-3f) { Debug.LogError($"MIGRATION: {c.Id} migration seeds {c.State.IrregularMigrationPer10k}, the spine says {expectedMigration[c.Id]}."); ok = false; }
                if (Mathf.Abs(c.State.PovertyGap - expectedGap[c.Id]) > 1e-3f) { Debug.LogError($"MIGRATION: {c.Id} poverty gap seeds {c.State.PovertyGap}, the spine says {expectedGap[c.Id]}."); ok = false; }
                if (Mathf.Abs(c.State.HomelessPer10k - expectedHomeless[c.Id]) > 1e-3f) { Debug.LogError($"MIGRATION: {c.Id} homelessness seeds {c.State.HomelessPer10k}, the spine says {expectedHomeless[c.Id]}."); ok = false; }
                if (m.MigrationIsStock != (c.Id == CountryId.USA)) { Debug.LogError($"MIGRATION: {c.Id} carries the wrong migration definition."); ok = false; }
                if (m.PovertyGapIsOecd != (c.Id == CountryId.USA)) { Debug.LogError($"MIGRATION: {c.Id} carries the wrong poverty-gap vintage."); ok = false; }
                if (c.Id != CountryId.USA && m.PovertyGapOecd <= 0f) { Debug.LogError($"MIGRATION: {c.Id} has no OECD second reading of the gap."); ok = false; }
                if (string.IsNullOrEmpty(m.HomelessDefinition) || m.HomelessYear <= 0) { Debug.LogError($"MIGRATION: {c.Id} homelessness has no definition or year."); ok = false; }
                if (c.State.Underemployment <= 0f) { Debug.LogError($"MIGRATION: {c.Id} underemployment not seeded."); ok = false; }
            }

            float[] untouchedSe = Run(CountryId.Sweden, Years, enforcement: 0f, housingCut: 0f, generosityUp: 0f, out bool guardsHeld);
            if (!guardsHeld) { Debug.LogError("MIGRATION: a figure left its runaway guard in twenty years at no policy."); ok = false; }
            float[] enforcedSe = Run(CountryId.Sweden, Years, enforcement: 30f, housingCut: 0f, generosityUp: 0f, out _);
            if (!(enforcedSe[0] > untouchedSe[0])) { Debug.LogError($"MIGRATION: Sweden's FLOW with enforcement up ({enforcedSe[0]:F2}) is not above untouched ({untouchedSe[0]:F2}) - apprehensions should rise."); ok = false; }
            float[] untouchedUs = Run(CountryId.USA, Years, enforcement: 0f, housingCut: 0f, generosityUp: 0f, out _);
            float[] enforcedUs = Run(CountryId.USA, Years, enforcement: 30f, housingCut: 0f, generosityUp: 0f, out _);
            if (!(enforcedUs[0] < untouchedUs[0])) { Debug.LogError($"MIGRATION: the USA's STOCK with enforcement up ({enforcedUs[0]:F1}) is not below untouched ({untouchedUs[0]:F1}) - the resident stock should fall."); ok = false; }
            float[] housingCutSe = Run(CountryId.Sweden, Years, enforcement: 0f, housingCut: -0.2f, generosityUp: 0f, out _);
            if (!(housingCutSe[3] > untouchedSe[3])) { Debug.LogError($"MIGRATION: Sweden's homelessness with the housing line cut ({housingCutSe[3]:F2}) is not above untouched ({untouchedSe[3]:F2})."); ok = false; }
            float[] generousSe = Run(CountryId.Sweden, Years, enforcement: 0f, housingCut: 0f, generosityUp: 20f, out _);
            if (!(generousSe[1] < untouchedSe[1])) { Debug.LogError($"MIGRATION: Sweden's poverty gap with generosity up ({generousSe[1]:F2}) is not below untouched ({untouchedSe[1]:F2})."); ok = false; }

            Debug.Log($"MIGRATION: seeds for six with their definitions (the five's flow and the USA's stock; Eurostat's gap and the OECD's; each country's own homelessness). Sweden after {Years} years - untouched: flow {untouchedSe[0]:F2} per 10 000, gap {untouchedSe[1]:F2} %, underemployment {untouchedSe[2]:F2} %, homeless {untouchedSe[3]:F1} per 10 000; "
                + $"enforcement +30: flow {enforcedSe[0]:F2}; the USA's stock {untouchedUs[0]:F0} → {enforcedUs[0]:F0} with the same; housing line cut each year: homeless {housingCutSe[3]:F1}; generosity +20: gap {generousSe[1]:F2}. The two definitions move as their definitions say.");
            Debug.Log(ok ? "MIGRATION: PASS - the seeds are the spine's with their definitions, absent stays absent, the couplings move the stated way." : "MIGRATION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float enforcement, float housingCut, float generosityUp, out bool guardsHeld)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("MIGRATION");
            guardsHeld = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                if (enforcement != 0f) { c.BorderEnforcementLevel = Mathf.Clamp(c.BorderEnforcementLevel + enforcement, 0f, 100f); }
                if (generosityUp != 0f) { foreach (WelfareProgram p in c.WelfarePrograms) { if (p.IsImplemented) { p.GenerosityLevel = Mathf.Clamp(p.GenerosityLevel + generosityUp, 0f, 100f); } } }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (housingCut != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (MigrationPovertyFamily.IsHousingLine(line.Category)) { d.SpendingLineChanges[line.Category] = housingCut * 100f; } } }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    EconomyState s = c.State;
                    if (s.IrregularMigrationPer10k > MigrationPovertyFamily.MaxMigration || s.PovertyGap > MigrationPovertyFamily.MaxGap || s.Underemployment > MigrationPovertyFamily.MaxUnderemployment || s.HomelessPer10k > MigrationPovertyFamily.MaxHomeless) { guardsHeld = false; }
                }
                return new[] { c.State.IrregularMigrationPer10k, c.State.PovertyGap, c.State.Underemployment, c.State.HomelessPer10k };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
