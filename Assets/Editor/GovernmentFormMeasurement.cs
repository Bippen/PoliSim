using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// T-3, the identity's G - a measurement, not a check: it is on no bar. §505 (2026-09-15) measured the two forms before the ruling on a probe-edited tree;
    /// form A was RULED and LANDED the same day (§507), so this tool now reads the landed tree in three worlds, at the trajectory dump's settings (no player,
    /// every country on the AI rule, a hundred years, both seeds).
    ///
    /// <para><b>landed</b> - the model as built: the identity's G is k × the lines' plan ÷ the price level (MacroSystem.IdentityGovernmentConsumption), the seed
    /// potentials FT-7's closed form (§394, GDP × (1 − NAIRU) ÷ (1 − U) - no G). This is the dump's own run. <b>lines</b> - every country's k set to 1 after the
    /// world is built, so the identity reads the lines as it did before T-3: the counterfactual the family is explained against. <b>fixed</b> - the landed G with
    /// the six seed potentials re-solved at the identity's own fixed point instead of FT-7's closed form: the daily solve settles at
    /// Y* = (0.5 (G + NX + DI) + 0.5 P) ÷ (1 − 0.5 a) (`MacroSystem.ApplyNationalAccountsDaily`'s affine map), so P = Y (2 − a) − (G + NX), with a the day's
    /// consumption-and-investment propensity read off the landed world's first day ((C + I) ÷ the seed's GDP; DI is zero at the seed's rates, NX zero on the
    /// seed's day) and G the government consumption the identity read that day. The fixed world is the reading of "six potentials re-solved with G inside the
    /// identity" that replaces FT-7's ruled closed form; it is measured beside the landed world, not built.</para>
    /// </summary>
    public static class GovernmentFormMeasurement
    {
        private static readonly int[] Seeds = { 777, 424242 };
        private static readonly int[] ReportYears = { 1, 10, 25, 50, 100 };
        private const int Horizon = 100;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string forms = "landed,lines,fixed";
            foreach (string arg in Environment.GetCommandLineArgs()) { if (arg.StartsWith("-t3forms=", StringComparison.Ordinal)) { forms = arg.Substring("-t3forms=".Length); } }
            var sb = new StringBuilder("=== T-3 MEASURED ON THE LANDED TREE: the identity's G as built, the lines' counterfactual, and the fixed-point potentials (§507) ===\n");
            bool ok = true;
            foreach (string form in forms.Split(','))
            {
                foreach (int seed in Seeds) { ok &= RunForm(sb, form, seed); }
            }
            sb.Append(ok ? "=== GovernmentFormMeasurement: MEASURED ===\n" : "=== GovernmentFormMeasurement: INCOMPLETE ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>The landed world's first day: per country the seed's GDP, the propensity a, the G the identity read and the trade balance, and the potential at
        /// which the seed's GDP is the identity's fixed point.</summary>
        private static Dictionary<CountryId, double> ImpliedPotentials(int seed)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();
            var y0 = new Dictionary<CountryId, double>();
            foreach (Country c in world.Countries) { y0[c.Id] = c.State.GDP; }
            var go = new GameObject($"T3_implied_{seed}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AdvanceDay();
                var implied = new Dictionary<CountryId, double>();
                foreach (Country c in world.Countries)
                {
                    double a = (c.State.Consumption + c.State.Investment) / y0[c.Id];
                    implied[c.Id] = y0[c.Id] * (2.0 - a) - (c.State.GovernmentConsumption + c.State.TradeBalance);
                }
                return implied;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static bool RunForm(StringBuilder sb, string form, int seed)
        {
            Dictionary<CountryId, double> implied = form == "fixed" ? ImpliedPotentials(seed) : null;
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();   // as the trajectory dump builds its world, so form "landed" is the dump's own run
            var ft7 = new Dictionary<CountryId, double>();
            foreach (Country c in world.Countries)
            {
                ft7[c.Id] = c.PotentialGdpSeed;
                if (form == "lines") { c.GovernmentConsumptionScale = 1f; }
                else if (form == "fixed") { c.PotentialGdpSeed = (float)implied[c.Id]; c.State.PotentialGDP = c.PotentialGdpSeed; }
                else if (form != "landed") { Debug.LogError($"T-3: no form '{form}' (landed, lines, fixed)."); return false; }
            }

            var go = new GameObject($"T3_{form}_{seed}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                var y0 = new Dictionary<CountryId, double>();
                var nominal0 = new Dictionary<CountryId, double>();
                var lines0 = new Dictionary<CountryId, double>();
                foreach (Country c in world.Countries)
                {
                    y0[c.Id] = c.State.GDP; nominal0[c.Id] = c.State.NominalGdp;
                    double lines = 0.0;
                    foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { lines += line.Amount; } }
                    lines0[c.Id] = lines;
                }

                sim.AdvanceDay();
                foreach (Country c in world.Countries)
                {
                    double a = (c.State.Consumption + c.State.Investment) / y0[c.Id];
                    double g = c.State.GovernmentConsumption;
                    double fixedPoint = y0[c.Id] * (2.0 - a) - (g + c.State.TradeBalance);
                    sb.Append(F("T3SEED|{0}|{1}|{2}|{3:R}|{4:R}|{5:R}|{6:R}|{7:R}|{8:R}|{9:R}|{10:R}|{11:R}\n",
                        form, seed, c.Id, y0[c.Id], lines0[c.Id] / nominal0[c.Id] * 100.0, g / y0[c.Id] * 100.0, c.GovernmentConsumptionScale, a, g, c.State.TradeBalance, fixedPoint, ft7[c.Id]));
                }
                for (int day = 1; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                int year = 1;
                while (true)
                {
                    if (Array.IndexOf(ReportYears, year) >= 0)
                    {
                        foreach (Country c in world.Countries)
                        {
                            sb.Append(F("T3|{0}|{1}|{2}|{3}|{4:R}|{5:R}|{6:R}|{7:R}|{8:R}|{9:R}\n",
                                form, seed, c.Id, year, c.State.GDP, c.State.PotentialGDP, c.State.Unemployment, c.State.DebtToGdpRatio, c.State.GovernmentConsumption / c.State.GDP * 100.0, c.State.Inflation));
                        }
                    }
                    if (year == Horizon) { break; }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    year++;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"T-3: form {form} seed {seed} threw: {e}");
                return false;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
