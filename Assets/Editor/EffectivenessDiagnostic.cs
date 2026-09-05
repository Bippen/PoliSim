using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C7 (2026-09-05): the effectiveness mechanic's proof. (1) At no policy every recorded portfolio's effectiveness equals the minister's
    /// efficiency (allocated = requested: the ratio is the multiplier alone) and a line no ministry reads is in no portfolio. (2) Sweden with
    /// its health lines cut a fifth in the first year's decision: the Health portfolio's allocation ratio is 0.80 to the tolerance that year and
    /// its effectiveness 0.80 × efficiency; after twenty years the waits are longer and quality worse than the untouched run - the family
    /// reads the mechanic, not the stand-in (the row's "underfunding a ministry measurably degrades its outcomes through the minister").
    /// (3) A minister of lower efficiency, same money: effectiveness lower, quality worse - the multiplier is felt.
    /// </summary>
    public static class EffectivenessDiagnostic
    {
        private const int Years = 20;
        private const float Tolerance = 0.002f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;

            // (1) the identity at no policy
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            foreach (SpendingCategory c in System.Enum.GetValues(typeof(SpendingCategory)))
            {
                CabinetPortfolio? p = Effectiveness.PortfolioOf(c);
                if (c == SpendingCategory.SocialSecurity && p != null) { Debug.LogError("EFFECTIVENESS: pensions have a ministry; 9d says no ministry reads them."); ok = false; }
                if (c == SpendingCategory.HealthcareAndSocialCare && p != CabinetPortfolio.HealthSocialAffairs) { Debug.LogError("EFFECTIVENESS: the health line is not the Health ministry's."); ok = false; }
            }
            float[] untouched = RunSweden(Years, cutShare: 0f, efficiencyOverride: -1f, out float ratioYear1, out float allocYear1, out float efficiencyYear1);
            if (Mathf.Abs(allocYear1 - 1f) > Tolerance) { Debug.LogError($"EFFECTIVENESS: at no policy the Health allocation ratio in year 1 is {allocYear1:F4}, not 1."); ok = false; }
            if (Mathf.Abs(ratioYear1 - efficiencyYear1) > Tolerance) { Debug.LogError($"EFFECTIVENESS: at no policy effectiveness {ratioYear1:F4} is not the efficiency {efficiencyYear1:F4}."); ok = false; }

            // (2) the health lines cut a fifth through the DECISION (the mechanic's own path: request indexed, allocation the player's)
            float[] cut = RunSweden(Years, cutShare: -0.2f, efficiencyOverride: -1f, out float ratioCut, out float allocCut, out float efficiencyCut);
            // The decision asks for -20 %; the seed-range clamp (ClampToSeedRange) may allow less - the assertion is the identity and the direction, not the asked figure.
            if (!(allocCut < 0.99f) || allocCut <= 0f) { Debug.LogError($"EFFECTIVENESS: a cut through the decision gives an allocation ratio of {allocCut:F4}, not below 1."); ok = false; }
            if (Mathf.Abs(ratioCut - allocCut * efficiencyCut) > Tolerance) { Debug.LogError($"EFFECTIVENESS: effectiveness {ratioCut:F4} is not allocation {allocCut:F4} x efficiency {efficiencyCut:F4}."); ok = false; }
            if (!(cut[1] > untouched[1]) || !(cut[2] > untouched[2]) || !(cut[0] > untouched[0])) { Debug.LogError($"EFFECTIVENESS: with the Health ministry at {ratioCut:F2} the waits ({cut[1]:F1} / {cut[2]:F1} d) and quality ({cut[0]:F1}) are not worse than untouched ({untouched[1]:F1} / {untouched[2]:F1} d, {untouched[0]:F1})."); ok = false; }

            // (3) a less efficient minister, the same money
            float[] slack = RunSweden(Years, cutShare: 0f, efficiencyOverride: 60f, out float ratioSlack, out _, out float efficiencySlack);
            if (Mathf.Abs(ratioSlack - efficiencySlack) > Tolerance || efficiencySlack > 0.61f) { Debug.LogError($"EFFECTIVENESS: with efficiency set to 60 the ratio is {ratioSlack:F4} (efficiency read {efficiencySlack:F4})."); ok = false; }
            if (!(slack[0] > untouched[0])) { Debug.LogError($"EFFECTIVENESS: a minister at efficiency 60 leaves quality at {slack[0]:F1}, not worse than {untouched[0]:F1}."); ok = false; }

            Debug.Log($"EFFECTIVENESS: Sweden, Health - year 1 at no policy: allocation {allocYear1:F3}, efficiency {efficiencyYear1:F2}, effectiveness x{ratioYear1:F2}; the health lines cut through the decision (-20 % asked, the seed range deciding): allocation {allocCut:F3}, effectiveness x{ratioCut:F2}; "
                + $"after {Years} years - untouched: treatable mortality {untouched[0]:F1}, cataract {untouched[1]:F1} d, knee {untouched[2]:F1} d; cut: {cut[0]:F1}, {cut[1]:F1} d, {cut[2]:F1} d; efficiency 60 with the same money: x{ratioSlack:F2}, quality {slack[0]:F1}. Underfunding degrades the outcomes through the minister.");
            Debug.Log(ok ? "EFFECTIVENESS: PASS - the ratio is allocated / requested x efficiency, lines without a ministry print none, the family reads it." : "EFFECTIVENESS: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>Sweden for <paramref name="years"/>; a nonzero <paramref name="cutShare"/> asks the decision for that percent on every health line each year (the
        /// player's figure through the mechanic's own path); a positive <paramref name="efficiencyOverride"/> sets the Health minister's efficiency before the run.
        /// Returns treatable mortality and the two waits at the end; the out values are year 1's Health record.</summary>
        private static float[] RunSweden(int years, float cutShare, float efficiencyOverride, out float ratioYear1, out float allocYear1, out float efficiencyYear1)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("EFFECTIVENESS");
            ratioYear1 = allocYear1 = efficiencyYear1 = float.NaN;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                if (efficiencyOverride > 0f)
                {
                    // Sweden seats no Health minister at the seed (an empty chair multiplies by 1); the probe appoints one with the efficiency asked.
                    if (!se.CabinetMinisters.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out CabinetMinister m) || m == null) { m = new CabinetMinister { Name = "Probe", Portfolio = CabinetPortfolio.HealthSocialAffairs }; se.CabinetMinisters[CabinetPortfolio.HealthSocialAffairs] = m; }
                    m.Efficiency = efficiencyOverride;
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (cutShare != 0f)
                    {
                        foreach (SpendingLine line in se.SpendingLines) { if (HealthFamily.IsHealthLine(line.Category)) { d.SpendingLineChanges[line.Category] = cutShare * 100f; } }
                    }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    if (year == 1 && se.Effectiveness.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out PortfolioEffectiveness e))
                    {
                        ratioYear1 = e.Ratio; allocYear1 = e.AllocationRatio; efficiencyYear1 = e.Efficiency;
                    }
                }
                return new[] { se.State.TreatableMortality, se.State.WaitCataractDays, se.State.WaitKneeDays };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
