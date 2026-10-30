using System;
using System.Collections.Generic;
using System.Linq;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B2 (2026-09-05): the lines follow their drivers EXACTLY, per driver, per country - asserted on the real
    /// turn path. For each of the six countries as the player: a fresh world, five untouched turns; every spending
    /// line's amount must equal its seed times the ratio of its driver's level at year five to its level at the seed
    /// (the expected figure computed HERE from the state and the cohorts, not from the recompute). The turn reads a
    /// driver either as it stands when the turn opens or as this turn committed it (the cohorts: CohortDemographics
    /// commits at the top of AdvanceTurn); the diagnostic computes both, accepts either, and REPORTS which one each
    /// driver takes. K269 (a) (2026-10-29): every line also carries its REAL term - the real wage index at the last index over its seed on a caseload,
    /// the compounded real potential growth on a line with no driver - in the player's book as in the AI's. Then a pinned line (pinned on turn 1
    /// through PolicyDecision.SpendingPinChanges) must read, at year five, exactly what it read at year one; and a
    /// line set to a nominal target on turn 1 must read that target (clamped) at year one and index from it after.
    /// The AI rule is asserted on one country that is NOT the player: its lines carry the same real-growth factor. Every line carries the year's prices
    /// (P5-B6: the price level at the last index over 1 at the seed). pbr (K314, 2026-10-30): the pension line's real term also carries the benefit ratio's
    /// path - B at the last boundary's calendar year over B at the epoch's year, read here from the generated table - within the same tolerance.
    ///
    /// Sits on the simulation bar: it builds and advances worlds.
    /// </summary>
    public static class SpendingIndexationDiagnostic
    {
        private const int Years = 5;
        private const float Tolerance = 1e-4f;   // relative; float compounding over five factors
        /// <summary>K269 (a) (the review's L4): the relative tolerance on a line with no driver - the same as every other line's. Its expected real term compounds potential
        /// growth as read before each turn, which is the figure the boundary indexes with: measured out of the engine, the widest driverless miss over the six is 2.4e-7, so a
        /// dropped year of the slowest growth (Italy's) fails by an order of magnitude more than the tolerance.</summary>
        private const float GrowthTolerance = Tolerance;

        /// <summary>pbr (K314; the prereg's (A0) 2): the benefit ratio the pension line carries, read HERE from the generated table (not from the recompute):
        /// B in a year, held at the table's first year before it and at its last after it, as the ruling reads the path.</summary>
        internal static float BenefitRatioFromTable(CountryId id, int year)
        {
            if (!PoliSim.Data.Generated.PensionBenefitRatioPath.Ratio.TryGetValue(id, out float[] path)) { return 1f; }
            int first = id == CountryId.USA ? PoliSim.Data.Generated.PensionBenefitRatioPath.UsFirstYear : PoliSim.Data.Generated.PensionBenefitRatioPath.EuFirstYear;
            return path[Math.Max(0, Math.Min(path.Length - 1, year - first))];
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            int linesChecked = 0, held = 0;
            var readingByDriver = new Dictionary<string, HashSet<string>>();   // driver name -> how the turn reads it
            foreach (CountryId player in Enum.GetValues(typeof(CountryId)))
            {
                SimulationRandom.Seed(777);
                World world = WorldFactory.CreateDefault();
                var go = new GameObject("INDEXATION");
                try
                {
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.AiFinanceMinistryEnabled = false;   // §388: this check measures its own rule alone - the ministry's cuts would move the lines it reads
                    sim.PlayerCountryId = player;
                    Country country = world.GetCountry(player);
                    if (country.SpendingLines.Count == 0) { continue; }

                    // The seed's levels, read before any turn.
                    var seedAmount = new Dictionary<SpendingCategory, float>();
                    var seedLevel = new Dictionary<SpendingCategory, float>();
                    foreach (SpendingLine l in country.SpendingLines)
                    {
                        seedAmount[l.Category] = l.Amount;
                        seedLevel[l.Category] = SpendingDrivers.Level(SpendingDrivers.Of(l.Category), country);
                    }
                    SpendingCategory pinned = country.SpendingLines[0].Category;
                    SpendingCategory targeted = country.SpendingLines[country.SpendingLines.Count - 1].Category;
                    float target = seedAmount[targeted] * 1.2f;

                    var decisions = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                    PolicyDecision first = PolicyDecision.None();
                    first.SpendingPinChanges[pinned] = true;
                    first.SpendingNominalTargets[targeted] = target;
                    decisions[player] = first;

                    float pinnedAtOne = float.NaN, targetedAtOne = float.NaN;
                    var levelStanding = new Dictionary<SpendingCategory, float>();   // each driver as it stood when the last turn opened
                    var projected = new Dictionary<SpendingCategory, float>();   // P5-B5: each line's projection read before the last turn
                    float seedWage = Mathf.Max(0.0001f, country.State.RealWageIndex), growth = 1f;   // K269 (a): the real terms' references - the seed's real wage, and potential growth compounded over the turns
                    for (int year = 1; year <= Years; year++)
                    {
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                        growth *= 1f + country.PotentialGrowthRate / 100f;   // K269 (a): the rule reads PotentialGrowthRate as it stands when the lines are indexed at the top of the turn (LineIndexationDiagnostic's reading)
                        if (year == Years)
                        {
                            foreach (SpendingLine l in country.SpendingLines) { levelStanding[l.Category] = SpendingDrivers.Level(SpendingDrivers.Of(l.Category), country); projected[l.Category] = SimulationManager.ProjectLineNextYear(country, l, country.State.Inflation); }
                        }
                        sim.AdvanceTurn(decisions);
                        decisions[player] = PolicyDecision.None();
                        if (year == 1)
                        {
                            pinnedAtOne = country.SpendingLines.Find(l => l.Category == pinned).Amount;
                            targetedAtOne = country.SpendingLines.Find(l => l.Category == targeted).Amount;
                        }
                    }

                    foreach (SpendingLine l in country.SpendingLines)
                    {
                        if (l.Category == pinned || l.Category == targeted) { continue; }
                        SpendingDriver driver = SpendingDrivers.Of(l.Category);
                        string driverName = SpendingDrivers.Name(driver);
                        float levelCommitted = SpendingDrivers.Level(driver, country);
                        float ratioCommitted = seedLevel[l.Category] > 0f ? levelCommitted / seedLevel[l.Category] : 1f;
                        float ratioStanding = seedLevel[l.Category] > 0f ? levelStanding[l.Category] / seedLevel[l.Category] : 1f;
                        // P5-B6: the lines carry the year's prices as well - the price level at the last index (the end of the turn's days) over 1 at the seed.
                        float prices = country.State.PriceLevel;
                        // K269 (a): and its real term - the real wage index at the last index over the seed's on a caseload, the compounded potential growth with no driver
                        float real = driver == SpendingDriver.None ? growth : country.RealWageIndexAtLastIndex / seedWage;
                        // pbr (K314): and the pension line's benefit ratio - B at the last boundary's calendar year over B at the epoch's year, from the table
                        if (l.Category == SpendingCategory.SocialSecurity) { real *= BenefitRatioFromTable(player, country.CalendarYear) / BenefitRatioFromTable(player, SimulationManager.EpochDate.Year); }
                        float expectedCommitted = seedAmount[l.Category] * ratioCommitted * prices * real;
                        float expectedStanding = seedAmount[l.Category] * ratioStanding * prices * real;
                        linesChecked++;
                        float tolerance = driver == SpendingDriver.None ? GrowthTolerance : Tolerance;   // K269 (a): potential growth compounded as read before each turn, against the boundary's own reading
                        bool matchesCommitted = Mathf.Abs(l.Amount / expectedCommitted - 1f) <= tolerance;
                        bool matchesStanding = Mathf.Abs(l.Amount / expectedStanding - 1f) <= tolerance;
                        if (!matchesCommitted && !matchesStanding)
                        {
                            Debug.LogError($"INDEXATION: {player} {l.Category} ({driverName}) reads {l.Amount:F4} after {Years} years; seed {seedAmount[l.Category]:F4} x driver {ratioCommitted:F5} (as committed this turn) x prices {prices:F5} x the real term {real:F5} = {expectedCommitted:F4}, or x {ratioStanding:F5} (as it stood when the turn opened) = {expectedStanding:F4}.");
                            ok = false;
                            continue;
                        }
                        if (driver == SpendingDriver.None)
                        {
                            // No driver: the player's line is the seed at today's prices and the economy's real growth (K269 (a); P5-B6 the prices)
                            if (Mathf.Abs(l.Amount / (seedAmount[l.Category] * prices * growth) - 1f) > GrowthTolerance)
                            {
                                Debug.LogError($"INDEXATION: {player} {l.Category} has no driver and reads {l.Amount:F4} against its seed {seedAmount[l.Category]:F4} at the price level {prices:F5} and the compounded potential growth {growth:F5}.");
                                ok = false;
                            }
                            else { held++; }
                            continue;
                        }
                        if (!readingByDriver.TryGetValue(driverName, out HashSet<string> readings)) { readings = readingByDriver[driverName] = new HashSet<string>(); }
                        if (Mathf.Abs(ratioCommitted - ratioStanding) <= 1e-6f) { readings.Add("either (the driver did not move within the turn)"); }
                        else { readings.Add(matchesCommitted ? "as committed this turn" : "as it stood when the turn opened"); }
                    }
                    // P5-B5: the row's projection. A line with no driver, or a pinned line, must read exactly what it projected; a line
                    // with a driver misses by the driver's own year-to-year move (the projection carries LAST year's ratio), reported.
                    float worstProjectionMiss = 0f;
                    foreach (SpendingLine l in country.SpendingLines)
                    {
                        if (l.Category == targeted) { continue; }
                        float miss = Mathf.Abs(l.Amount / projected[l.Category] - 1f);
                        if (l.Pinned)
                        {
                            if (miss > Tolerance) { Debug.LogError($"INDEXATION: {player} {l.Category} is pinned, projected {projected[l.Category]:F4} for year {Years} and reads {l.Amount:F4}."); ok = false; }
                        }
                        else if (SpendingDrivers.Of(l.Category) == SpendingDriver.None)
                        {
                            // P5-B6: the projection carries the standing inflation; the year's days print a moving one, so a no-driver line misses by the
                            // year's inflation path against its opening rate - a cent on the figure, asserted within 1 %.
                            if (miss > 0.01f) { Debug.LogError($"INDEXATION: {player} {l.Category} has no driver, projected {projected[l.Category]:F4} for year {Years} and reads {l.Amount:F4} - more than the year's inflation path explains."); ok = false; }
                        }
                        else { worstProjectionMiss = Mathf.Max(worstProjectionMiss, miss); }
                    }
                    Debug.Log($"INDEXATION: {player} - the projection is exact on every line without a driver and on the pinned line; on the driver lines it misses by at most {worstProjectionMiss * 100f:F3} %, the driver's own year-to-year move.");
                    SpendingLine pinnedLine = country.SpendingLines.Find(l => l.Category == pinned);
                    if (BitConverter.SingleToInt32Bits(pinnedLine.Amount) != BitConverter.SingleToInt32Bits(pinnedAtOne))
                    {
                        Debug.LogError($"INDEXATION: {player} {pinned} was pinned on turn 1 and still moved: {pinnedAtOne:R} -> {pinnedLine.Amount:R}.");
                        ok = false;
                    }
                    if (Mathf.Abs(targetedAtOne / target - 1f) > Tolerance)
                    {
                        Debug.LogError($"INDEXATION: {player} {targeted} was set to {target:F4} on turn 1 and read {targetedAtOne:F4} that year.");
                        ok = false;
                    }
                    Debug.Log($"INDEXATION: {player} as the player - {country.SpendingLines.Count} lines over {Years} years; the pinned line {pinned} held at {pinnedAtOne:F3}; the set line {targeted} took {target:F3} and reads {country.SpendingLines.Find(l => l.Category == targeted).Amount:F3} after indexing.");

                    // The AI rule on a country that is not the player: real growth rides along.
                    Country other = world.GetCountry(player == CountryId.Sweden ? CountryId.Germany : CountryId.Sweden);
                    if (other.SpendingLines.Count > 0 && player == CountryId.Sweden)
                    {
                        SimulationRandom.Seed(777);
                        World w2 = WorldFactory.CreateDefault();
                        var go2 = new GameObject("INDEXATION-AI");
                        try
                        {
                            SimulationManager sim2 = go2.AddComponent<SimulationManager>();
                            sim2.SetWorld(w2);
                            sim2.AiFinanceMinistryEnabled = false;   // §388: this check measures its own rule alone - the ministry's cuts would move the lines it reads
                            sim2.PlayerCountryId = player;
                            Country ai = w2.GetCountry(other.Id);
                            SpendingLine aiLine = ai.SpendingLines[0];
                            float aiSeed = aiLine.Amount;
                            float aiSeedLevel = SpendingDrivers.Level(SpendingDrivers.Of(aiLine.Category), ai);
                            var d2 = new Dictionary<CountryId, PolicyDecision>();
                            foreach (Country c in w2.Countries) { d2[c.Id] = PolicyDecision.None(); }
                            float aiGrowth = 1f, aiStanding = aiSeedLevel;
                            for (int year = 1; year <= Years; year++)
                            {
                                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim2.AdvanceDay(); }
                                aiGrowth *= 1f + ai.PotentialGrowthRate / 100f;
                                aiStanding = SpendingDrivers.Level(SpendingDrivers.Of(aiLine.Category), ai);
                                sim2.AdvanceTurn(d2);
                            }
                            float aiCommitted = SpendingDrivers.Level(SpendingDrivers.Of(aiLine.Category), ai);
                            float aiRatioCommitted = aiSeedLevel > 0f ? aiCommitted / aiSeedLevel : 1f;
                            float aiRatioStanding = aiSeedLevel > 0f ? aiStanding / aiSeedLevel : 1f;
                            float aiExpectedCommitted = aiSeed * aiRatioCommitted * aiGrowth * ai.State.PriceLevel;   // P5-B6: prices ride along for the AI too
                            float aiExpectedStanding = aiSeed * aiRatioStanding * aiGrowth * ai.State.PriceLevel;
                            if (Mathf.Abs(aiLine.Amount / aiExpectedCommitted - 1f) > Tolerance && Mathf.Abs(aiLine.Amount / aiExpectedStanding - 1f) > Tolerance)
                            {
                                Debug.LogError($"INDEXATION: the AI rule - {ai.Id} {aiLine.Category} reads {aiLine.Amount:F4}; seed x driver x real growth = {aiExpectedCommitted:F4} (driver as committed) or {aiExpectedStanding:F4} (driver as it stood).");
                                ok = false;
                            }
                            else
                            {
                                Debug.Log($"INDEXATION: the AI rule - {ai.Id} {aiLine.Category} carries its driver x{aiRatioCommitted:F4} and real growth x{aiGrowth:F4}, as the player's lines do since K269 (a).");
                            }
                        }
                        finally { UnityEngine.Object.DestroyImmediate(go2); }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            foreach (KeyValuePair<string, HashSet<string>> kv in readingByDriver.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                Debug.Log($"INDEXATION: {kv.Key} - the index reads it {string.Join(" / ", kv.Value.OrderBy(s => s, StringComparer.Ordinal))}.");
            }
            Debug.Log(ok ? $"=== SpendingIndexationDiagnostic: ALL ASSERTIONS PASS ({linesChecked} lines across the six, each at seed x driver x prices x its real term; {held} with no driver at the seed x prices x the compounded potential growth) ===" : "=== SpendingIndexationDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
