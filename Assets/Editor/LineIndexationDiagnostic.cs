using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-2 re-formed (2026-09-07, §384; the driver-only form of §370 reverted). Sweden the player, twenty years at seed policy, every country read: (1) for an
    /// AI country, a line WITH a driver reads real amount ÷ (seed × driver ratio) = the country's compounded REAL WAGE growth (the real wage index at the last
    /// indexation over its seed) to 2 % - a caseload at the going wage, the income index's rule; (2) a line WITHOUT a driver reads the compounded real potential
    /// growth, Π(1 + g), the same for every driverless line of that country; (3) K269 (a) (2026-10-29): the PLAYER'S lines read the same two factors - one rule
    /// in every book, the sources' unchanged policy (`SimulationManager.IndexSpendingLines`) - where until then they read 1 on both classes (prices × driver only);
    /// (4) both directions on the driverless class: a country whose potential shrank reads below 1. Proves the split; the fiscal path is the trajectory suite's.
    /// <para>K210 (s937): the shrinking direction is PLANTED in a world of its own - Italy's no-policy potential used to shrink and exercise it, and the
    /// productivity pull made it grow. In the planted run an AI country's PotentialGrowthRate is set to <see cref="PlantedGrowth"/> at each boundary's
    /// labour point (SimulationManager.BoundaryLedger), just before its lines are indexed, and the boundary's own finalizer (MacroSystem.ApplySectorGrowthEffect)
    /// writes the derived rate back later in the same boundary; the hook is removed and the world destroyed after. The main run's statements are unchanged
    /// but the shrink, which no longer depends on any country's trend.</para>
    /// </summary>
    public static class LineIndexationDiagnostic
    {
        private const int Years = 20;

        /// <summary>K210: the planted run's length, years, its country and its potential growth, per cent a year - CONVENTION, a shrinking potential well clear
        /// of zero so the driverless factor's direction is unambiguous after a few indexations.</summary>
        private const int PlantedYears = 3;
        private const CountryId PlantedCountry = CountryId.Italy;
        private const float PlantedGrowth = -1.5f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = Measure(Years, null, out _);
            ok &= Measure(PlantedYears, PlantedCountry, out float plantedFactor);
            if (!(plantedFactor < 1f)) { Debug.LogError($"LINE INDEXATION: the planted run's {PlantedCountry} driverless factor {plantedFactor:F4} does not sit below 1 under a potential growth of {PlantedGrowth:F1} % a year - the shrinking direction failed."); ok = false; }
            else { Debug.Log($"LINE INDEXATION: the shrinking direction - {PlantedCountry}'s driverless factor {plantedFactor:F4} after {PlantedYears} indexations at a planted {PlantedGrowth:F1} % a year."); }
            Debug.Log(ok ? "LINE INDEXATION: PASS - caseload lines at the going real wage, driverless lines at potential growth, in the player's book as in the AI's (K269 (a)), both directions (the shrinking one planted)." : "LINE INDEXATION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>One run: <paramref name="years"/> indexations from the seed, Sweden the player; with <paramref name="planted"/>, that AI country's potential
        /// growth planted at <see cref="PlantedGrowth"/> for each indexation. Returns whether every statement held; <paramref name="plantedFactor"/> is the planted
        /// country's driverless factor (NaN without a plant).</summary>
        private static bool Measure(int years, CountryId? planted, out float plantedFactor)
        {
            plantedFactor = float.NaN;
            bool ok = true;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LINEINDEX");
            System.Action<Country, string> plant = (k, point) =>
            {
                if (planted.HasValue && point == "labour" && ReferenceEquals(k, world.GetCountry(planted.Value))) { k.PotentialGrowthRate = PlantedGrowth; }   // the indexation reads it next; the finalizer rewrites it
            };
            if (planted.HasValue) { SimulationManager.BoundaryLedger += plant; }
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;   // §388: this check measures its own rule alone - the ministry's cuts would move the lines it reads
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var seedAmount = new Dictionary<(CountryId, SpendingCategory), float>();
                var seedDriver = new Dictionary<(CountryId, SpendingCategory), float>();
                var growth = new Dictionary<CountryId, float>();
                var wage = new Dictionary<CountryId, float>();
                var wageSeed = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    growth[c.Id] = 1f; wage[c.Id] = 1f; wageSeed[c.Id] = Mathf.Max(0.0001f, c.State.RealWageIndex);
                    foreach (SpendingLine line in c.SpendingLines) { seedAmount[(c.Id, line.Category)] = line.Amount; seedDriver[(c.Id, line.Category)] = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)); }
                }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // the rule reads PotentialGrowthRate and the real wage index as they stand when the lines are indexed at the top of the turn - the same figures here
                    foreach (Country c in world.Countries) { growth[c.Id] *= 1f + (planted.HasValue && c.Id == planted.Value ? PlantedGrowth : c.PotentialGrowthRate) / 100f; wage[c.Id] = c.State.RealWageIndex / wageSeed[c.Id]; }
                    sim.AdvanceTurn(decisions);
                }
                var report = new List<string>();
                var benefits = new List<string>();   // pbr: the benefit ratio's index each pension line carried
                foreach (Country c in world.Countries)
                {
                    bool player = c.Id == CountryId.Sweden;
                    float price = Mathf.Max(0.0001f, c.State.PriceLevel);
                    int withDriver = 0, without = 0; float driverlessRatio = float.NaN, caseloadRatio = float.NaN;
                    foreach (SpendingLine line in c.SpendingLines)
                    {
                        float seed = seedAmount[(c.Id, line.Category)];
                        if (seed <= 0f || line.Pinned) { continue; }
                        float real = line.Amount / seed / price;
                        float driverRatio = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDrivers.Of(line.Category), c)) / seedDriver[(c.Id, line.Category)];
                        float residual = real / driverRatio;
                        // pbr (K314; the prereg's (A0) 3): the pension line carries the benefit ratio's path on top of the going wage - its residual over B's index
                        // (B at the last boundary's year over B at the epoch's year, read from the generated table) joins the caseload lines' one factor
                        if (line.Category == SpendingCategory.SocialSecurity)
                        {
                            float benefitIndex = SpendingIndexationDiagnostic.BenefitRatioFromTable(c.Id, c.CalendarYear) / SpendingIndexationDiagnostic.BenefitRatioFromTable(c.Id, SimulationManager.EpochDate.Year);
                            residual /= benefitIndex;
                            benefits.Add($"{c.Id} {benefitIndex:F4}");
                        }
                        bool driverless = SpendingDrivers.Of(line.Category) == SpendingDriver.None;
                        if (driverless)
                        {
                            if (float.IsNaN(driverlessRatio)) { driverlessRatio = residual; }
                            else if (Mathf.Abs(residual - driverlessRatio) > 1e-3f * Mathf.Max(1f, driverlessRatio)) { Debug.LogError($"LINE INDEXATION: {c.Id} driverless lines do not carry one factor ({line.Category} {residual:F4} vs {driverlessRatio:F4})."); ok = false; }
                            without++;
                        }
                        else
                        {
                            if (float.IsNaN(caseloadRatio)) { caseloadRatio = residual; }
                            else if (Mathf.Abs(residual - caseloadRatio) > 1e-3f * Mathf.Max(1f, caseloadRatio)) { Debug.LogError($"LINE INDEXATION: {c.Id} caseload lines do not carry one factor ({line.Category} {residual:F4} vs {caseloadRatio:F4})."); ok = false; }
                            withDriver++;
                        }
                    }
                    // the rule's own figures, in every book since K269 (a) - the player's included: potential growth compounded over the indexations, and the real wage
                    // index at the last indexation over its seed
                    string who = player ? $"{c.Id} (the player's book)" : c.Id.ToString();
                    if (!float.IsNaN(driverlessRatio) && Mathf.Abs(driverlessRatio - growth[c.Id]) > 0.02f * growth[c.Id]) { Debug.LogError($"LINE INDEXATION: {who} driverless factor {driverlessRatio:F4} is not the compounded potential growth {growth[c.Id]:F4}."); ok = false; }
                    if (!float.IsNaN(caseloadRatio) && Mathf.Abs(caseloadRatio - wage[c.Id]) > 0.02f * wage[c.Id]) { Debug.LogError($"LINE INDEXATION: {who} caseload factor {caseloadRatio:F4} is not the real wage growth {wage[c.Id]:F4} - the caseload is not at the going wage."); ok = false; }
                    if (player && (float.IsNaN(driverlessRatio) || Mathf.Abs(driverlessRatio - 1f) <= 1e-3f || float.IsNaN(caseloadRatio) || Mathf.Abs(caseloadRatio - 1f) <= 1e-3f)) { Debug.LogError($"LINE INDEXATION: the player's lines read {driverlessRatio:F4} driverless and {caseloadRatio:F4} caseload - a factor of 1 is the driver-only rule K269 (a) retired."); ok = false; }
                    if (planted.HasValue && c.Id == planted.Value) { plantedFactor = driverlessRatio; }
                    report.Add($"{c.Id}{(player ? " (player)" : "")}: {withDriver} caseload line(s) at {(float.IsNaN(caseloadRatio) ? 1f : caseloadRatio):F3} (real wage {wage[c.Id]:F3}), {without} driverless at {(float.IsNaN(driverlessRatio) ? 1f : driverlessRatio):F3}");
                }
                Debug.Log($"LINE INDEXATION: {(planted.HasValue ? $"the planted run ({planted.Value} at {PlantedGrowth:F1} % a year), " : "")}after {years} years - {string.Join("; ", report)}. A caseload line carries the country's real wage growth on top of prices and its driver (the income index's rule); a driverless line carries the compounded potential growth; in the player's book as in the AI's (K269 (a)). The pension line carries the benefit ratio's index on top (pbr, K314): {string.Join(", ", benefits)}.");
            }
            finally
            {
                if (planted.HasValue) { SimulationManager.BoundaryLedger -= plant; }
                Object.DestroyImmediate(go);
            }
            return ok;
        }
    }
}
