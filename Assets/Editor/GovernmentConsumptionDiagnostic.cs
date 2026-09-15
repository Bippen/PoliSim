using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// T-3, FORM A (Elias, 2026-09-15, `COMPLETED.md` §507): *"G is the sourced final-consumption share of GDP at the seed, grown with the lines."* It builds
    /// and advances worlds, so it belongs to the simulation group.
    ///
    /// <para>(1) THE SEED: for six, k (Country.GovernmentConsumptionScale) is finite and positive and k × the seed's discretionary lines ÷ the seed's nominal GDP
    /// is the generated share (GovernmentConsumptionData) to a thousandth of a point. (2) THE DAY: the government consumption the daily identity was handed on
    /// the seed's first day (EconomyState.GovernmentConsumption), over the seed's nominal GDP, is the same share - the price level is 1 on that day, so this is
    /// the call site scaling by k and nothing else. (3) GROWN WITH THE LINES, THE BOOK UNSCALED: a year on, each country's first discretionary line raised ten
    /// per cent at the boundary against the same world untouched (no AI ministry, so nothing else moves a line) - the book's plan is the lines' own sum, and
    /// the identity's G on the next day is k × that plan ÷ the price level it was read at, in both worlds; the policy's move of G is k times the plan's move,
    /// deflated. (4) THE POTENTIALS, printed: FT-7's closed-form seed potential (unchanged - it carries no G; PotentialEmploymentDiagnostic asserts it) beside
    /// the potential at which the seed's GDP is the identity's own fixed point with the landed G, P = Y (2 − a) − (G + NX), a read off the first day - §505's
    /// comparison on the landed tree.</para>
    /// </summary>
    public static class GovernmentConsumptionDiagnostic
    {
        private const float SharePointsTolerance = 0.001f;
        private const float RaisePercent = 10f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder("=== GOVERNMENT CONSUMPTION (T-3, form A): the identity's G is the sourced share at the seed, grown with the lines ===\n");

            // (1) the seed and (2) the day
            SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var seedLines = new Dictionary<CountryId, float>();
            var seedNominal = new Dictionary<CountryId, float>();
            var seedGdp = new Dictionary<CountryId, float>();
            var seedPrice = new Dictionary<CountryId, float>();
            sb.Append("\n    1-2. THE SEED AND ITS FIRST DAY: k, the lines' share, the sourced share, and the share the identity read on day one\n");
            foreach (Country c in world.Countries)
            {
                float lines = 0f;
                foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { lines += line.Amount; } }
                seedLines[c.Id] = lines; seedNominal[c.Id] = c.State.NominalGdp; seedGdp[c.Id] = c.State.GDP; seedPrice[c.Id] = c.State.PriceLevel;
                float k = c.GovernmentConsumptionScale;
                float sourced = GovernmentConsumptionData.SharePct[c.Id];
                float captured = k * lines / c.State.NominalGdp * 100f;
                if (!(k > 0f) || float.IsInfinity(k)) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {c.Id}'s k reads {k} - not a finite positive scale."); }
                if (Mathf.Abs(captured - sourced) > SharePointsTolerance) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {c.Id}'s k × seed lines ÷ seed nominal GDP is {captured:F4} % against the sourced {sourced:F4} % - k was not captured from the share."); }
            }
            var go = new GameObject("GOVCONS_SEED");
            var a = new Dictionary<CountryId, float>();
            var dayOneG = new Dictionary<CountryId, float>();
            var dayOneNx = new Dictionary<CountryId, float>();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AdvanceDay();
                foreach (Country c in world.Countries)
                {
                    float sourced = GovernmentConsumptionData.SharePct[c.Id];
                    float read = c.State.GovernmentConsumption * seedPrice[c.Id] / seedNominal[c.Id] * 100f;
                    if (seedPrice[c.Id] != 1f) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {c.Id}'s seed price level reads {seedPrice[c.Id]} - the day-one reading assumes 1."); }
                    if (Mathf.Abs(read - sourced) > SharePointsTolerance) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: the identity read {c.Id}'s G on day one at {read:F4} % of the seed's GDP against the sourced {sourced:F4} % - the daily call site does not hand k × the plan."); }
                    a[c.Id] = (c.State.Consumption + c.State.Investment) / seedGdp[c.Id];
                    dayOneG[c.Id] = c.State.GovernmentConsumption;
                    dayOneNx[c.Id] = c.State.TradeBalance;
                    string federal = GovernmentConsumptionData.FederalSharePct.TryGetValue(c.Id, out float fed) ? F(" (federal-only {0:F2} %: the lines are federal, the share general government)", fed) : "";
                    sb.Append(F("    {0,-8} k {1:F4}   the lines {2,6:F2} % of GDP   sourced {3,6:F2} %{4}   the identity on day one {5,6:F2} %\n",
                        c.Id, c.GovernmentConsumptionScale, seedLines[c.Id] / seedNominal[c.Id] * 100f, sourced, GovernmentConsumptionData.Flag[c.Id] == "p" ? " (p)" : "    ", read) + federal + (federal.Length > 0 ? "\n" : ""));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // (3) grown with the lines, the book unscaled
            sb.Append(F("\n    3. GROWN WITH THE LINES, THE BOOK UNSCALED: a year of days, each country's first discretionary line +{0:F0} % at the boundary against untouched, the next day's identity\n", RaisePercent));
            Outcome untouched = GrowWorld(0f);
            Outcome raised = GrowWorld(RaisePercent);
            foreach (CountryId id in untouched.Countries)
            {
                foreach ((string name, Outcome o) in new[] { ("untouched", untouched), ("raised", raised) })
                {
                    float lines = o.Lines[id], plan = o.Plan[id], k = o.K[id], price = o.PriceAtRead[id], g = o.G[id];
                    if (Mathf.Abs(plan - lines) > 1e-4f * lines) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {id} ({name}) - the book's plan {plan:F3} is not the discretionary lines' sum {lines:F3}; the book was scaled or the plan left the lines."); }
                    float expected = k * plan / price;
                    if (Mathf.Abs(g - expected) > 1e-5f * expected) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {id} ({name}) - the identity read G {g:F4} against k × plan ÷ P = {expected:F4}; the lines do not carry the identity's G."); }
                }
                float planMove = raised.Plan[id] - untouched.Plan[id];
                float gMove = raised.G[id] - untouched.G[id];
                float expectedMove = untouched.K[id] * planMove / raised.PriceAtRead[id];
                bool moveOk = planMove > 0f && Mathf.Abs(gMove - expectedMove) <= 2e-3f * Mathf.Abs(expectedMove) + 1e-3f;
                if (!moveOk) { ok = false; Debug.LogError($"GOVERNMENT CONSUMPTION: {id} - the raise moved the book's plan by {planMove:F3} and the identity's G by {gMove:F4}, against k × the move ÷ P = {expectedMove:F4}."); }
                sb.Append(F("    {0,-8} the plan moved {1,9:F3} (the book, unscaled)   G moved {2,9:F4}   k × move ÷ P {3,9:F4}   {4}\n", id, planMove, gMove, expectedMove, moveOk ? "ok" : "FAIL"));
            }

            // (4) the potentials
            sb.Append("\n    4. THE POTENTIALS AT THE SEED: FT-7's closed form (no G) beside the identity's fixed point with the landed G, P = Y (2 − a) − (G + NX)\n");
            foreach (Country c in world.Countries)
            {
                float implied = seedGdp[c.Id] * (2f - a[c.Id]) - (dayOneG[c.Id] + dayOneNx[c.Id]);
                sb.Append(F("    {0,-8} GDP {1,10:F1}   FT-7 {2,10:F1} ({3:+0.00;-0.00} % of GDP)   the identity's fixed point {4,10:F1} ({5:+0.00;-0.00} % against FT-7)   a {6:F4}\n",
                    c.Id, seedGdp[c.Id], c.PotentialGdpSeed, (c.PotentialGdpSeed / seedGdp[c.Id] - 1f) * 100f, implied, (implied / c.PotentialGdpSeed - 1f) * 100f, a[c.Id]));
            }

            sb.Append(ok ? "=== GovernmentConsumptionDiagnostic: PASS - k captured from the sourced share, the day's identity handed it, the lines carrying it and the book unscaled ===\n"
                         : "=== GovernmentConsumptionDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private sealed class Outcome
        {
            public readonly List<CountryId> Countries = new List<CountryId>();
            public readonly Dictionary<CountryId, float> Lines = new Dictionary<CountryId, float>(), Plan = new Dictionary<CountryId, float>(), K = new Dictionary<CountryId, float>(),
                PriceAtRead = new Dictionary<CountryId, float>(), G = new Dictionary<CountryId, float>();
        }

        /// <summary>A year of days with the AI ministry off, the boundary with each country's first discretionary line raised by <paramref name="raisePercent"/>, then
        /// the lines' sum and the book's plan (read from the period by reflection) and the price level as they stand, one more day, and the G the identity read on it.</summary>
        private static Outcome GrowWorld(float raisePercent)
        {
            SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("GOVCONS_GROW");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries)
                {
                    PolicyDecision d = PolicyDecision.None();
                    if (raisePercent != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { d.SpendingLineChanges[line.Category] = raisePercent; break; } } }
                    decisions[c.Id] = d;
                }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                var periods = (System.Collections.IDictionary)typeof(SimulationManager).GetField("_fiscalPeriods", instance).GetValue(sim);
                var o = new Outcome();
                foreach (Country c in world.Countries)
                {
                    float lines = 0f;
                    foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { lines += line.Amount; } }
                    o.Countries.Add(c.Id);
                    o.Lines[c.Id] = lines;
                    o.Plan[c.Id] = ((SimulationManager.FiscalPeriod)periods[c.Id]).PlannedGovernmentSpending;
                    o.K[c.Id] = c.GovernmentConsumptionScale;
                    o.PriceAtRead[c.Id] = Mathf.Max(0.0001f, c.State.PriceLevel);
                }
                sim.AdvanceDay();
                foreach (Country c in world.Countries) { o.G[c.Id] = c.State.GovernmentConsumption; }
                return o;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
