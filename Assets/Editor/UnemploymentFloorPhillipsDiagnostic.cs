using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF-14 (q14, `docs/RULINGS_2026-10-18.md`; Elias's ruling K138, 2026-10-27; the audit's GA-2-4): <b>unemployment floors at the lowest rate the OECD's
    /// harmonised annual series records since 1990, inflation rises convexly with the gap, and prices may fall.</b>
    /// <list type="number">
    /// <item><b>(a) The floors are the source's</b>: the stored CSV (`UnemploymentFloorData.SourcePath`) is the file its folder's SHA256SUMS.txt lists and the
    /// digest the generated table carries; re-read here on its own (every row's code columns the series' - UNE_LF_M, PT_LF_SUB, N, _T, Y_GE15, A), each
    /// country's lowest annual observation from 1990 on - or from the series' first year where it opens later, printed - is the table's floor, its year and
    /// its series start; `MacroSystem.UnemploymentFloorPercent` hands the table's floor for every seeded country; and France's seed is the series' 2024
    /// observation (K200).</item>
    /// <item><b>(b) The floor binds</b>: on every seeded country a boom far past it (Okun's daily step at +60 points of annual growth, a day at a time for a
    /// year) and a supply shock past it (participation falling fifteen points cyclically) leave unemployment exactly at the floor, never below; a rate
    /// seeded below its floor is lifted to it by its first step; the Okun ledger's three terms plus the clamp's share (`MacroSystem.OkunClampLedger`)
    /// sum to each day's move; the supply excess books the realised move, and at the floor a participation fall leaves the excess where it was.</item>
    /// <item><b>(c) The curve is convex</b>: `MacroSystem.PhillipsSlack` is NAIRU × ln(U ÷ NAIRU) by hand; on every seeded country the live print's response
    /// to unemployment g points below the natural rate exceeds the mirror's at g points above by the slope × NAIRU × (−ln(1 − g ÷ NAIRU) − ln(1 + g ÷ NAIRU)),
    /// both by hand, at g = 0.5, 1 and 2; the slope at the natural rate is `MacroSystem.PhillipsCurveSlope` (a central difference); and the USA's
    /// one-point ratio against the line sits between 1 and 40 ÷ 30 (a sense of size from the memo's simulation of its other convex form, not a check on this one).</item>
    /// <item><b>(d) Prices may fall</b>: a print below 0 stands (no floor at 0) and stays above `MacroSystem.MinInflationPercent` in the planted slump; the
    /// expectations step carries a negative print below 0; a spending line projects down at a negative print; and a world played a period with every
    /// country planted in a slump (unemployment at three times its natural rate, expectations 0.5) prints deflation, its price level falling on every
    /// deflating day, the Taylor rule's suggestion its four terms by hand floored at 0 (never below) and every zone's rate at or above 0; the
    /// boundary day's closing print, the one the expectations step reads, is printed per country.</item>
    /// <item><b>(e) The natural rate is never below the floor</b> (K199): France with the Unemployment Benefit Consolidation Act's delta composes a natural rate
    /// below its floor; every reader sees the floor; at the floor the Phillips slack, the Taylor rule's unemployment gap and Okun's reversion term are 0;
    /// held there for 80 boundaries, inflation converges to the target.</item>
    /// <item><b>(f) No nominal pension cut</b> (K201): `SimulationManager.PensionNominalGuarantee` floors the USA's pension price term (42 U.S.C. 415(i)) and
    /// Germany's per-pensioner term (SGB VI 68a) at 1 in a deflating year, leaves France's and every other line indexing down, and changes nothing in a
    /// year the guarantee does not bind.</item>
    /// <item><b>(g) The guarantee is wired</b> (review 2's N-3, N-2): the boundary's own index step (`SimulationManager.IndexSpendingLines`, by reflection) on a
    /// price level down 2 % holds the USA's and Germany's pension lines and lowers France's and the USA's Medicare; `SimulationManager.ProjectLineNextYear`
    /// projects the same; and (N-1) `MacroSystem.EffectiveNaturalRateChange` gives France's law card the room above its floor for a -1.2 act.</item>
    /// </list>
    /// Prints `FLOOR PHILLIPS: PASS` and exits 0 when every check holds; each failure is a `FLOOR PHILLIPS:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.UnemploymentFloorPhillipsDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class UnemploymentFloorPhillipsDiagnostic
    {
        private const string Folder = "ElectionsData/macro/raw/oecd_hur/";

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();   // section d plays a world
            _failures = 0;
            _sb = new StringBuilder("FLOOR PHILLIPS: MF-14 - the unemployment floor from the OECD harmonised series, the convex Phillips curve, deflation allowed (seed 777)\n");
            try
            {
                FloorsAreTheSource();
                FloorBinds();
                CurveIsConvex();
                PricesMayFall();
                NaturalRateNeverBelowFloor();
                PensionGuarantee();
                PensionGuaranteeWired();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "FLOOR PHILLIPS: PASS - each floor is the stored OECD series' lowest since 1990 (or its start), the floor binds on Okun's step and the supply shock, the curve's response below the natural rate exceeds its mirror by the log form, a planted slump deflates with the Taylor rule, the price level, the expectations and indexation reading it, the natural rate never below the floor (K199), and no nominal pension cut in the USA and Germany (K201) - in the boundary's index step and the NEXT YEAR projection, the law card reading the room above the floor."
                : F("FLOOR PHILLIPS: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (a) the floors are the source's
        private static void FloorsAreTheSource()
        {
            _sb.Append("    a. the floors are the stored OECD series' lowest annual rate since 1990 (K138)\n");
            string file = Path.GetFileName(UnemploymentFloorData.SourcePath);
            byte[] bytes = File.ReadAllBytes(PathOf(UnemploymentFloorData.SourcePath));
            string digest = Sha(bytes);
            string[] sums = File.ReadAllText(PathOf(Folder + "SHA256SUMS.txt"), Encoding.ASCII).Replace("\r\n", "\n").Split('\n');
            Check(sums.Contains(digest + " *" + file) && digest == UnemploymentFloorData.SourceDigest, F("{0} {1}: listed in SHA256SUMS.txt and the digest the generated table carries", file, digest));

            string[] lines = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Split('\n');
            string[] head = lines[0].Split(',');
            int Col(string name) => Array.IndexOf(head, name);   // the code column precedes its label twin, which differs in case
            var want = new Dictionary<string, string> { { "MEASURE", "UNE_LF_M" }, { "UNIT_MEASURE", "PT_LF_SUB" }, { "TRANSFORMATION", "_Z" }, { "ADJUSTMENT", "N" }, { "SEX", "_T" }, { "AGE", "Y_GE15" }, { "ACTIVITY", "_Z" }, { "FREQ", "A" } };
            var area = new Dictionary<string, CountryId> { { "USA", CountryId.USA }, { "SWE", CountryId.Sweden }, { "DEU", CountryId.Germany }, { "FRA", CountryId.France }, { "ITA", CountryId.Italy }, { "POL", CountryId.Poland } };
            int cArea = Col("REF_AREA"), cYear = Col("TIME_PERIOD"), cValue = Col("OBS_VALUE");
            bool keyed = cArea >= 0 && cYear >= 0 && cValue >= 0 && want.Keys.All(k => Col(k) >= 0);
            var series = new Dictionary<CountryId, SortedDictionary<int, (double Value, string Text)>>();
            for (int i = 1; i < lines.Length && keyed; i++)
            {
                if (lines[i].Length == 0) { continue; }
                string[] f = lines[i].Split(',');
                if (f.Length != head.Length || !want.All(w => f[Col(w.Key)] == w.Value) || !area.TryGetValue(f[cArea], out CountryId id)) { keyed = false; break; }
                if (!series.TryGetValue(id, out var years)) { series[id] = years = new SortedDictionary<int, (double, string)>(); }
                years[int.Parse(f[cYear], CultureInfo.InvariantCulture)] = (double.Parse(f[cValue], NumberStyles.Float, CultureInfo.InvariantCulture), f[cValue]);
            }
            Check(keyed, "every row of the CSV is the series (UNE_LF_M, PT_LF_SUB, _Z, N, _T, Y_GE15, _Z, A) for one of the six areas");

            World world = SeededWorld();
            foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                if (!series.TryGetValue(id, out var years) || !UnemploymentFloorData.Floors.TryGetValue(id, out var row)) { Check(false, F("{0}: the series and the table both hold it", id)); continue; }
                int start = years.Keys.First();
                int from = Math.Max(UnemploymentFloorData.FromYear, start);
                var read = years.Where(y => y.Key >= from).ToList();
                var low = read.OrderBy(y => y.Value.Value).ThenBy(y => y.Key).First();
                bool same = row.Floor == float.Parse(low.Value.Text, CultureInfo.InvariantCulture) && row.Year == low.Key && row.SeriesStart == start && row.Years == read.Count
                            && (UnemploymentFloorData.Floors[id].SeriesStart > UnemploymentFloorData.FromYear ? UnemploymentFloorData.Floors[id].SeriesStart : UnemploymentFloorData.FromYear) == from;
                Check(same, F("{0}: floor {1} ({2}) is the lowest of {3} annual observations from {4}{5} - the table reads {6} ({7}), its series from {8}",
                    id, low.Value.Text, low.Key, read.Count, from, start > UnemploymentFloorData.FromYear ? F(" (the series opens {0}, after 1990: the floor is from its start)", start) : "",
                    row.Floor.ToString("R", CultureInfo.InvariantCulture), row.Year, row.SeriesStart));
                Country c = world.GetCountry(id);
                Check(MacroSystem.UnemploymentFloorPercent(c) == row.Floor, F("{0}: the model's floor {1} is the table's; seeded unemployment {2:0.###}, the natural rate {3:0.###}{4}",
                    id, MacroSystem.UnemploymentFloorPercent(c).ToString("R", CultureInfo.InvariantCulture), c.State.Unemployment, c.EffectiveNaturalUnemploymentRate,
                    c.State.Unemployment < row.Floor ? " - the SEED sits below the floor and is lifted to it on its first step" : ""));
            }
            // K200: France's seed is the series' 2024 observation
            if (series.TryGetValue(CountryId.France, out var fra) && fra.TryGetValue(2024, out var fra2024))
            {
                float seed = world.GetCountry(CountryId.France).State.Unemployment;
                Check(seed == float.Parse(fra2024.Text, CultureInfo.InvariantCulture) && seed >= UnemploymentFloorData.Floors[CountryId.France].Floor,
                    F("France: the seed unemployment {0} is the series' 2024 observation {1} (K200), at or above its floor", seed.ToString("R", CultureInfo.InvariantCulture), fra2024.Text));
            }
            else { Check(false, "France: the series holds a 2024 observation"); }
        }

        // ============================================================ (b) the floor binds
        private static void FloorBinds()
        {
            _sb.Append("    b. the floor binds on Okun's step and on the supply shock\n");
            World world = SeededWorld();
            foreach (Country c in world.Countries)
            {
                float floor = MacroSystem.UnemploymentFloorPercent(c);
                EconomyState saved = c.State.Clone();
                float lowest = float.MaxValue;
                // L-5: the ledger's three terms plus the clamp's share sum to the move on every day, floor days included
                double terms = 0, clampShare = 0, worstGap = 0, clampTotal = 0;
                Action<Country, float, float, float, float> ledger = (k, g, r, a, slice) => { if (k == c) { terms = (double)g + r + a; } };
                Action<Country, float> clampLedger = (k, s) => { if (k == c) { clampShare = s; } };
                MacroSystem.OkunLedger += ledger;
                MacroSystem.OkunClampLedger += clampLedger;
                try
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++)
                    {
                        float before = c.State.Unemployment;
                        terms = double.NaN; clampShare = double.NaN;
                        MacroSystem.ApplyOkunsLawDaily(c, c.PotentialGrowthRate + 60f, saved.Unemployment);
                        lowest = Math.Min(lowest, c.State.Unemployment);
                        worstGap = Math.Max(worstGap, double.IsNaN(terms + clampShare) ? double.PositiveInfinity : Math.Abs(terms + clampShare - (c.State.Unemployment - before)));
                        clampTotal += double.IsNaN(clampShare) ? 0 : clampShare;
                    }
                }
                finally { MacroSystem.OkunLedger -= ledger; MacroSystem.OkunClampLedger -= clampLedger; }
                Check(lowest >= floor && c.State.Unemployment == floor, F("{0}: a year's boom at +60 points of growth stops at the floor {1} exactly (lowest {2}, last {3})",
                    c.Id, floor.ToString("R", CultureInfo.InvariantCulture), lowest.ToString("R", CultureInfo.InvariantCulture), c.State.Unemployment.ToString("R", CultureInfo.InvariantCulture)));
                Check(worstGap < 1e-5 && clampTotal > 0, F("{0}: the Okun ledger's three terms plus the clamp's share sum to each day's move (worst gap {1:0.0e0}); the clamp absorbed {2:0.000} points over the boom",
                    c.Id, worstGap, clampTotal));

                c.State = saved.Clone();
                c.State.Unemployment = floor - 0.25f;
                MacroSystem.ApplyOkunsLawDaily(c, c.PotentialGrowthRate, floor - 0.25f);
                Check(c.State.Unemployment >= floor, F("{0}: a rate planted 0.25 below the floor reads {1} after one step - lifted to the floor or above",
                    c.Id, c.State.Unemployment.ToString("R", CultureInfo.InvariantCulture)));

                c.State = saved.Clone();
                c.State.SupplyUnemploymentExcess = 0f;
                float structural = ParticipationRateTable.StructuralRate(c);
                if (!float.IsNaN(structural)) { c.StructuralParticipationAtLastBoundary = structural; }
                c.ParticipationAtLastBoundary = Mathf.Clamp(c.State.LaborForceParticipationRate, 1f, 100f) + 15f;
                float uBefore = c.State.Unemployment;
                MacroSystem.ApplySupplyShockToUnemployment(c);
                Check(c.State.Unemployment == floor, F("{0}: a cyclical fall of fifteen points of participation leaves unemployment at the floor {1} exactly (reads {2})",
                    c.Id, floor.ToString("R", CultureInfo.InvariantCulture), c.State.Unemployment.ToString("R", CultureInfo.InvariantCulture)));
                // M-1: the excess books the move that happened, not the one the floor absorbed
                Check(Math.Abs(c.State.SupplyUnemploymentExcess - (c.State.Unemployment - uBefore)) < 1e-5,
                    F("{0}: the excess reads {1:0.00000} - the realised move {2:0.00000}, not the shock's full impact", c.Id, c.State.SupplyUnemploymentExcess, c.State.Unemployment - uBefore));

                c.State = saved.Clone();
                c.State.Unemployment = floor;
                c.State.SupplyUnemploymentExcess = 0f;
                if (!float.IsNaN(structural)) { c.StructuralParticipationAtLastBoundary = structural; }
                c.ParticipationAtLastBoundary = Mathf.Clamp(c.State.LaborForceParticipationRate, 1f, 100f) + 1f;
                MacroSystem.ApplySupplyShockToUnemployment(c);
                Check(c.State.Unemployment == floor && c.State.SupplyUnemploymentExcess == 0f,
                    F("{0}: at the floor, a one-point cyclical fall in participation is absorbed whole - unemployment {1}, the excess {2} (no phantom rise booked for the next boundary)",
                        c.Id, c.State.Unemployment.ToString("R", CultureInfo.InvariantCulture), c.State.SupplyUnemploymentExcess.ToString("R", CultureInfo.InvariantCulture)));
                c.State = saved;
            }
        }

        // ============================================================ (c) the curve is convex
        private static void CurveIsConvex()
        {
            _sb.Append("    c. the curve is convex: NAIRU x ln(U / NAIRU), the slope at the NAIRU the old one\n");
            double slope = MacroSystem.PhillipsCurveSlope;
            Check(Math.Abs(MacroSystem.PhillipsSlack(6f, 4f) - 4.0 * Math.Log(1.5)) < 1e-5 && MacroSystem.PhillipsSlack(4f, 4f) == 0f,
                F("the slack at U 6, NAIRU 4 is 4 ln 1.5 = {0:0.000000} by hand (reads {1:0.000000}); at the NAIRU it is 0", 4.0 * Math.Log(1.5), MacroSystem.PhillipsSlack(6f, 4f)));
            World world = SeededWorld();
            foreach (Country c in world.Countries)
            {
                EconomyState saved = c.State.Clone();
                double n = c.EffectiveNaturalUnemploymentRate;
                float Print(double u)
                {
                    c.State = saved.Clone();
                    c.State.InflationExpectations = 2f;
                    c.State.Unemployment = (float)u;
                    MacroSystem.ApplyPhillipsCurveInflation(c);
                    return c.State.Inflation;
                }
                foreach (double g in new[] { 0.5, 1.0, 2.0 })
                {
                    double below = Print(n - g) - 2.0, above = 2.0 - Print(n + g);
                    double handBelow = -slope * n * Math.Log(1.0 - g / n), handAbove = slope * n * Math.Log(1.0 + g / n);
                    bool ok = below > slope * g && above < slope * g && above > 0
                              && Math.Abs(below - handBelow) < 1e-4 && Math.Abs(above - handAbove) < 1e-4 && Math.Abs((below - above) - (handBelow - handAbove)) < 2e-4;
                    Check(ok, F("{0}: {1} point(s) below the natural rate {2:0.###} raise inflation {3:0.00000} (by hand {4:0.00000}); the mirror above lowers it {5:0.00000} ({6:0.00000}); the line {7:0.00000} lies between - the excess {8:0.00000}",
                        c.Id, g, n, below, handBelow, above, handAbove, slope * g, below - above));
                }
                const double h = 0.01;
                double local = (Print(n - h) - Print(n + h)) / (2 * h);
                Check(Math.Abs(local - slope) < 2e-3, F("{0}: the slope at the natural rate reads {1:0.0000} (a central difference of 0.01) - the old line's {2}", c.Id, local, slope));
                if (c.Id == CountryId.USA)
                {
                    double ratio = (Print(n - 1.0) - 2.0) / slope;
                    Check(ratio > 1.0 && ratio < 40.0 / 30.0, F("USA: one point below its natural rate {0:0.###} the convex response is {1:0.000} times the line's - a sense of size, not a check on the log form: inside the range of the memo's simulation (Murphy 2018, run on its other convex form, ugap / u: 30 bp linear against \"less than 40 bps\" convex)", n, ratio));
                }
                c.State = saved;
            }
        }

        // ============================================================ (d) prices may fall
        private static void PricesMayFall()
        {
            _sb.Append("    d. prices may fall: no floor at 0, the readers behave, a planted slump deflates in a played world\n");
            Check(MacroSystem.MinInflationPercent < 0f, F("the inflation floor {0} lies below 0", MacroSystem.MinInflationPercent));
            {
                World world = SeededWorld();
                Country c = world.GetCountry(CountryId.Sweden);
                c.State.InflationExpectations = 0.5f;
                c.State.Unemployment = 3f * c.EffectiveNaturalUnemploymentRate;
                MacroSystem.ApplyPhillipsCurveInflation(c);
                double hand = 0.5 - MacroSystem.PhillipsCurveSlope * c.EffectiveNaturalUnemploymentRate * Math.Log(3.0);
                Check(c.State.Inflation < 0f && c.State.Inflation > MacroSystem.MinInflationPercent && Math.Abs(c.State.Inflation - hand) < 1e-4,
                    F("Sweden at three times its natural rate, expectations 0.5: the print {0:0.0000} stands below 0 (by hand {1:0.0000}), above the guard", c.State.Inflation, hand));
                float print = c.State.Inflation;
                MacroSystem.ApplyInflationExpectations(c.State);
                Check(c.State.InflationExpectations < 0f && Math.Abs(c.State.InflationExpectations - (0.5f + (print - 0.5f) * 0.5f)) < 1e-5,
                    F("the expectations step carries it below 0: 0.5 -> {0:0.0000} (half the way to {1:0.0000}), no reset", c.State.InflationExpectations, print));
                var line = new SpendingLine(SpendingCategory.SocialSecurity, 100f, true);
                Check(Math.Abs(line.ProjectNextYear(-2f) - 98f) < 1e-3, F("a spending line of 100 projects {0:0.000} at -2 % - it follows prices down", line.ProjectNextYear(-2f)));
            }

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World played = WorldFactory.CreateDefault();
            foreach (Country c in played.Countries)
            {
                c.State.Unemployment = Math.Min(25f, 3f * c.EffectiveNaturalUnemploymentRate);
                c.State.InflationExpectations = 0.5f;
            }
            var go = new GameObject("FLOORPHILLIPS");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(played);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in played.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var deflatingDays = played.Countries.ToDictionary(k => k.Id, k => 0);
                var lowest = played.Countries.ToDictionary(k => k.Id, k => float.MaxValue);
                var closing = played.Countries.ToDictionary(k => k.Id, k => float.NaN);
                var priceBreaks = new List<string>();
                var taylorBreaks = new List<string>();
                bool crossed = false;
                for (int d = 0; d < SimulationManager.DaysPerTurn + 2 && !crossed; d++)
                {
                    var levelBefore = played.Countries.ToDictionary(k => k.Id, k => k.State.PriceLevel);
                    bool boundary = sim.AdvanceDay();
                    foreach (Country k in played.Countries)
                    {
                        float pi = k.State.Inflation;
                        lowest[k.Id] = Math.Min(lowest[k.Id], pi);
                        if (pi < 0f)
                        {
                            deflatingDays[k.Id]++;
                            // a day's slice at -0.05 % moves a level near 1 by about 1.4e-6, past a float's step there; a smaller print may round to no move, never up
                            if (k.State.PriceLevel > levelBefore[k.Id] || (pi < -0.05f && !(k.State.PriceLevel < levelBefore[k.Id]))) { priceBreaks.Add(F("{0} day {1}: inflation {2:0.0000} and the price level {3} -> {4}", k.Id, d + 1, pi, levelBefore[k.Id], k.State.PriceLevel)); }
                        }
                        float terms = TaylorRule.NeutralRealRate(k) + pi + TaylorRule.InflationGapWeight(k) * (pi - TaylorRule.InflationTarget(k)) + TaylorRule.GetGapTermPercentagePoints(k);
                        float suggested = TaylorRule.GetSuggestedInterestRate(k);
                        if (suggested < 0f || Math.Abs(suggested - Math.Max(0f, terms)) > 1e-4f || (k.CurrencyZone != null && k.CurrencyZone.InterestRate < 0f))
                        {
                            taylorBreaks.Add(F("{0} day {1}: the terms sum {2:0.0000}, the suggestion {3:0.0000}, the zone's rate {4:0.0000}", k.Id, d + 1, terms, suggested, k.CurrencyZone != null ? k.CurrencyZone.InterestRate : float.NaN));
                        }
                    }
                    if (!boundary) { continue; }
                    foreach (Country k in played.Countries) { closing[k.Id] = k.State.Inflation; }
                    sim.AdvanceTurn(decisions);
                    crossed = true;
                }
                Check(crossed, "the played world crossed its first boundary");
                foreach (Country k in played.Countries)
                {
                    Check(deflatingDays[k.Id] > 0 && lowest[k.Id] > MacroSystem.MinInflationPercent,
                        F("{0}: the planted slump deflates on {1} day(s) of the period, the lowest print {2:0.0000} above the guard {3}", k.Id, deflatingDays[k.Id], lowest[k.Id], MacroSystem.MinInflationPercent));
                    _sb.Append(F("        info {0}: the boundary day's closing print {1:0.0000}, the expectations after the step {2:0.0000}", k.Id, closing[k.Id], k.State.InflationExpectations)).Append('\n');
                }
                Check(priceBreaks.Count == 0, F("the price level fell on every deflating day{0}", priceBreaks.Count == 0 ? "" : " - not on: " + string.Join("; ", priceBreaks.Take(6))));
                Check(taylorBreaks.Count == 0, F("the Taylor rule's suggestion is its four terms floored at 0 on every day, and every zone's rate at or above 0{0}", taylorBreaks.Count == 0 ? "" : " - not on: " + string.Join("; ", taylorBreaks.Take(6))));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // ============================================================ (e) the natural rate never below the floor (K199)
        private static void NaturalRateNeverBelowFloor()
        {
            _sb.Append("    e. the natural rate every reader sees is never below the floor (K199): France with the Unemployment Benefit Consolidation Act\n");
            World world = SeededWorld();
            Country c = world.GetCountry(CountryId.France);
            LawDefinition law = LawCatalog.GetById("hartz_benefit_reform_act");
            float delta = law != null ? law.Structural.Where(d => d.Parameter == StructuralParameter.NaturalUnemploymentRate).Sum(d => d.Delta) : 0f;
            StructuralParameters.Spec spec = StructuralParameters.All.First(s => s.Parameter == StructuralParameter.NaturalUnemploymentRate);
            c.NaturalUnemploymentRate = Mathf.Clamp(c.NaturalUnemploymentRateBase + delta, spec.Min, spec.Max);   // the composition RecomputeStructuralParametersFromEnactedLaws applies, the act alone enacted
            float floor = c.UnemploymentFloorPercent;
            float composed = c.NaturalUnemploymentRate + c.State.NaturalRateDemographicShift;
            Check(delta < 0f && composed < floor && c.EffectiveNaturalUnemploymentRate == floor,
                F("France: the act's delta {0} composes the natural rate {1:0.###} below the floor {2}; every reader sees {3} - the floor",
                    delta, composed, floor.ToString("R", CultureInfo.InvariantCulture), c.EffectiveNaturalUnemploymentRate.ToString("R", CultureInfo.InvariantCulture)));

            c.State.Unemployment = floor;
            c.State.SupplyUnemploymentExcess = 0f;
            float slack = MacroSystem.PhillipsSlack(c.State.Unemployment, c.EffectiveNaturalUnemploymentRate);
            float taylorGap = TaylorRule.GetUnemploymentGapPercent(c);
            double reversion = double.NaN;
            Action<Country, float, float, float, float> ledger = (k, g, r, a, s) => { if (k == c) { reversion = r; } };
            MacroSystem.OkunLedger += ledger;
            EconomyState before = c.State.Clone();
            try { MacroSystem.ApplyOkunsLawDaily(c, c.PotentialGrowthRate, floor); }
            finally { MacroSystem.OkunLedger -= ledger; }
            c.State = before;
            Check(slack == 0f && taylorGap == 0f && reversion == 0.0,
                F("France at its floor: the Phillips slack {0}, the Taylor rule's unemployment gap {1}, Okun's reversion term {2} - all 0 (without K199: slack {3:0.0000}, gap {4:0.0000})",
                    slack, taylorGap, reversion, composed * Math.Log(floor / composed), composed - floor));

            float target = c.CurrencyZone != null ? c.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget;
            c.State.InflationExpectations = 0.5f;
            for (int year = 0; year < 80; year++)
            {
                c.State.Unemployment = floor;
                MacroSystem.ApplyPhillipsCurveInflation(c);
                MacroSystem.ApplyInflationExpectations(c.State, anchorPercent: target);
            }
            MacroSystem.ApplyPhillipsCurveInflation(c);
            Check(Math.Abs(c.State.Inflation - target) < 1e-3,
                F("France held at its floor for 80 boundaries from expectations of 0.5: inflation {0:0.0000} converges to the target {1} (without K199 it would settle about 0.75 x slope x {2:0.000} below)",
                    c.State.Inflation, target, composed * Math.Log(floor / composed)));
        }

        // ============================================================ (f) no nominal pension cut in the USA and Germany (K201)
        private static void PensionGuarantee()
        {
            _sb.Append("    f. no nominal pension cut under deflation in the USA (42 U.S.C. 415(i)) and Germany (SGB VI 68a); the others index down\n");
            const float prices = 0.98f, driver = 1.01f, real = 1.005f;
            float plain = prices * driver * real;
            float us = SimulationManager.PensionNominalGuarantee(CountryId.USA, SpendingCategory.SocialSecurity, prices, driver, real, plain);
            float de = SimulationManager.PensionNominalGuarantee(CountryId.Germany, SpendingCategory.SocialSecurity, prices, driver, real, plain);
            float fr = SimulationManager.PensionNominalGuarantee(CountryId.France, SpendingCategory.SocialSecurity, prices, driver, real, plain);
            float usOther = SimulationManager.PensionNominalGuarantee(CountryId.USA, SpendingCategory.Medicare, prices, driver, real, plain);
            float usRising = SimulationManager.PensionNominalGuarantee(CountryId.USA, SpendingCategory.SocialSecurity, 1.02f, driver, real, 1.02f * driver * real);
            float deRealUp = SimulationManager.PensionNominalGuarantee(CountryId.Germany, SpendingCategory.SocialSecurity, 0.99f, driver, 1.02f, 0.99f * driver * 1.02f);
            Check(us == driver * real && de == driver && fr == plain && usOther == plain && usRising == 1.02f * driver * real && deRealUp == 0.99f * driver * 1.02f,
                F("prices -2 %, caseload +1 %, real +0.5 %: the USA's pension line x{0:0.0000} (the price term floored at 1), Germany's x{1:0.0000} (the pension value floored, the caseload alone), France's x{2:0.0000} (indexes down); the USA's Medicare x{3:0.0000}; a rising year untouched (x{4:0.0000}); Germany where wages outrun the price fall untouched (x{5:0.0000})",
                    us, de, fr, usOther, usRising, deRealUp));
        }

        // ============================================================ (g) the guarantee is wired (the q14 review 2's N-3 and N-2), and the law card's effect (N-1)
        private static void PensionGuaranteeWired()
        {
            _sb.Append("    g. the pension guarantee in the boundary's own index step and in the NEXT YEAR projection: a price level down 2 %; the law card's natural-rate effect\n");
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FLOORPHILLIPS_PENSION");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                System.Reflection.MethodInfo index = typeof(SimulationManager).GetMethod("IndexSpendingLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (index == null) { Check(false, "SimulationManager.IndexSpendingLines found by reflection - the wiring is UNVERIFIED, not clean"); return; }
                var ratios = new Dictionary<string, double>();
                var projected = new Dictionary<string, (double Projection, double Amount, double Real)>();   // K269 (a): with the real term the projection carries (SimulationManager.ProjectedRealTerm)
                foreach (CountryId id in new[] { CountryId.USA, CountryId.Germany, CountryId.France })
                {
                    Country c = world.GetCountry(id);
                    // the NEXT YEAR projection first, on the seeded lines; then one index step: the price level down 2 % since the last index, nothing else moved
                    foreach (SpendingLine l in c.SpendingLines) { projected[id + " " + l.Category] = (SimulationManager.ProjectLineNextYear(c, l, -2f), l.Amount * (l.LastDriverRatio > 0f ? l.LastDriverRatio : 1f), SimulationManager.ProjectedRealTerm(c, l)); }
                    var seeds = c.SpendingLines.ToDictionary(l => l, l => (double)l.SeedAmount);
                    c.State.PriceLevel *= 0.98f;
                    index.Invoke(sim, new object[] { c });
                    foreach (SpendingLine l in c.SpendingLines) { ratios[id + " " + l.Category] = seeds[l] > 0 ? l.SeedAmount / seeds[l] : double.NaN; }
                }
                double R(string key) => ratios.TryGetValue(key, out double v) ? v : double.NaN;
                Check(Math.Abs(R("USA SocialSecurity") - 1.0) < 1e-3 && Math.Abs(R("Germany SocialSecurity") - 1.0) < 1e-3
                      && Math.Abs(R("France SocialSecurity") - 0.98) < 1e-3 && Math.Abs(R("USA Medicare") - 0.98) < 1e-3,
                    F("the boundary's index step: the USA's pension line x{0:0.0000} and Germany's x{1:0.0000} (held), France's x{2:0.0000} and the USA's Medicare x{3:0.0000} (down with prices)",
                        R("USA SocialSecurity"), R("Germany SocialSecurity"), R("France SocialSecurity"), R("USA Medicare")));
                (double Projection, double Amount, double Real) P(string key) => projected.TryGetValue(key, out var v) ? v : (double.NaN, double.NaN, double.NaN);
                var us = P("USA SocialSecurity"); var de = P("Germany SocialSecurity"); var fr = P("France SocialSecurity"); var med = P("USA Medicare");
                // K269 (a): every projection carries its line's real term (the standing real wage growth on these caseloads). The USA's guarantee floors the PRICE term
                // at 1 (the real term still moves); Germany's floors the per-pensioner term, prices x the real term, at 1 where it would fall; France's and Medicare's
                // follow prices down by the -2 % with the real term on top
                double deExpected = 0.98 * de.Real < 1.0 ? de.Amount : 0.98 * de.Real * de.Amount;
                Check(Math.Abs(us.Projection - us.Real * us.Amount) < 1e-3 * us.Amount && Math.Abs(de.Projection - deExpected) < 1e-3 * de.Amount
                      && Math.Abs(fr.Projection - 0.98 * fr.Real * fr.Amount) < 1e-3 * fr.Amount && Math.Abs(med.Projection - 0.98 * med.Real * med.Amount) < 1e-3 * med.Amount,
                    F("NEXT YEAR at -2 %: the USA's pension line projects {0:0.000} of {1:0.000} (no price fall; the real term x{8:0.0000} on top) and Germany's {2:0.000} of {3:0.000} (floored where prices x the real term x{9:0.0000} fall), France's {4:0.000} of {5:0.000} and the USA's Medicare {6:0.000} of {7:0.000} (down with prices, the real term on top)",
                        us.Projection, us.Amount, de.Projection, de.Amount, fr.Projection, fr.Amount, med.Projection, med.Amount, us.Real, de.Real));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // N-1: the law card's effect for a natural-rate act - France's room above the floor, not the act's full cut
            World w = SeededWorld();
            Country france = w.GetCountry(CountryId.France);
            float room = france.EffectiveNaturalUnemploymentRate - france.UnemploymentFloorPercent;
            float effect = MacroSystem.EffectiveNaturalRateChange(france, -1.2f);
            float small = MacroSystem.EffectiveNaturalRateChange(france, -0.1f);
            Check(Math.Abs(effect + room) < 1e-5 && Math.Abs(small + 0.1f) < 1e-5,
                F("the law card's effect in France: a -1.2 act delivers {0:0.000} (the room above the floor, {1:0.000}); a -0.1 act delivers {2:0.000} (all of it)", effect, room, small));
        }

        // ============================================================ the harness's hands
        /// <summary>The seeded world (seed 777), unplayed.</summary>
        private static World SeededWorld()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            return WorldFactory.CreateDefault();
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("FLOOR PHILLIPS: FAIL - " + what); }
        }

        private static string Sha(byte[] bytes)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
