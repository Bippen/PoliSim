using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// T-3, MEASURED BEFORE A RULING (Elias, 2026-09-15, `COMPLETED.md` §503: *"measure before I rule. Build neither form; compute both — G as the sourced
    /// final-consumption share grown with the lines, and the lines' own sum re-based to it — and report each form's implied potential and year-100 path
    /// per country."*). A measurement, not a check: it is on no bar, and nothing of either form is in the model.
    ///
    /// <para><b>The identity's G today</b> is the discretionary lines' sum, deflated (`SimulationManager` hands `FiscalPeriod.PlannedGovernmentSpending /
    /// PriceLevel` to `MacroSystem.ApplyNationalAccountsDaily`); the book spends the same plan. <b>Form A</b> - the sourced share grown with the lines - is
    /// the identity's G scaled by k = share × seed GDP ÷ the seed's lines, the book untouched: it needs a factor on the identity's argument, which is not in
    /// the model, so it runs only on a tree a probe has edited - the probe adds a per-country scale on MacroSystem named ProbeIdentityGovernmentScale, which
    /// this tool finds by reflection; on a tree without it (every committed tree) form A is not armed, and the tool says so. <b>Form B</b> - the lines' own sum re-based to the share - scales every discretionary line's amount and seed anchor by the same k after
    /// the world is built and re-runs the families' seeding the world's builder runs after the lines, so the identity AND the book carry it.</para>
    ///
    /// <para><b>The implied potential</b> of a form is the potential at which the seed's GDP is the identity's own fixed point: the daily solve settles at
    /// Y* = (0.5 (G + NX + DI) + 0.5 P) ÷ (1 − 0.5 a) (`MacroSystem.ApplyNationalAccountsDaily`'s affine map), so P = Y (2 − a) − (G + NX + DI), with a the
    /// day's consumption-and-investment propensity read off the model's own first day ((C + I) ÷ yesterday's GDP; DI is zero at the seed's rates). It is
    /// printed beside FT-7's closed-form seed potential (`Country.PotentialGdpSeed`, which carries no G) - §394's comparison, the one that found the USA's
    /// typed potential 14.7 % adrift. <b>The year-100 path</b> is the real model run a hundred years from each form's world, no player, every country on the
    /// AI rule - the trajectory dump's settings - at both of its seeds.</para>
    /// </summary>
    public static class GovernmentFormMeasurement
    {
        /// <summary>General government final consumption expenditure (P3_S13), per cent of GDP, 2023 - `COMPLETED.md` §376's table, on the cross-check gate:
        /// Eurostat nama_10_gdp (updated 2026-09-07) for the five members, the World Bank's NE.CON.GOVT.ZS for the USA (single-source, itself BEA NIPA; the second
        /// reading billed). A measurement's input, not the model's.</summary>
        private static readonly Dictionary<CountryId, double> P3S13Share2023 = new Dictionary<CountryId, double>
        {
            { CountryId.USA, 13.5 }, { CountryId.Sweden, 26.4 }, { CountryId.Germany, 21.1 }, { CountryId.France, 24.1 }, { CountryId.Italy, 17.9 }, { CountryId.Poland, 18.9 },
        };
        private static readonly int[] Seeds = { 777, 424242 };
        private static readonly int[] ReportYears = { 1, 10, 25, 50, 100 };
        private const int Horizon = 100;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string forms = "lines,B";
            foreach (string arg in Environment.GetCommandLineArgs()) { if (arg.StartsWith("-t3forms=", StringComparison.Ordinal)) { forms = arg.Substring("-t3forms=".Length); } }
            FieldInfo probe = typeof(MacroSystem).GetField("ProbeIdentityGovernmentScale", BindingFlags.Static | BindingFlags.Public);
            var sb = new StringBuilder("=== T-3 MEASURED: the identity's G in two forms, neither built (Elias, 2026-09-15) ===\n");
            bool ok = true;
            foreach (string form in forms.Split(','))
            {
                if (form == "A" && probe == null) { sb.Append("    form A: NOT ARMED - no probe on the identity's argument in this tree\n"); ok = false; continue; }
                foreach (int seed in Seeds) { ok &= RunForm(sb, form, seed, probe); }
            }
            sb.Append(ok ? "=== GovernmentFormMeasurement: MEASURED ===\n" : "=== GovernmentFormMeasurement: INCOMPLETE ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static bool RunForm(StringBuilder sb, string form, int seed, FieldInfo probe)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();   // as the trajectory dump builds its world, so form "lines" is the dump's own run
            var k = new Dictionary<CountryId, double>();
            var lines0 = new Dictionary<CountryId, double>();
            foreach (Country c in world.Countries)
            {
                double lines = 0.0;
                foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { lines += line.Amount; } }
                lines0[c.Id] = lines;
                k[c.Id] = P3S13Share2023[c.Id] / 100.0 * c.State.NominalGdp / lines;
            }
            float[] scale = null;
            if (form == "B")
            {
                foreach (Country c in world.Countries)
                {
                    foreach (SpendingLine line in c.SpendingLines)
                    {
                        if (line.IsMandatory) { continue; }
                        line.Amount = (float)(line.Amount * k[c.Id]);
                        line.SeedAmount = (float)(line.SeedAmount * k[c.Id]);
                    }
                }
                // the families' seeding, as WorldFactory.CreateDefault runs it after the lines (their bases read the line sums)
                HealthFamily.SeedAll(world);
                EducationFamily.SeedAll(world);
                InfrastructureFamily.SeedAll(world);
                EnergyMarket.ResetTurnState();
                EnvironmentFamily.SeedAll(world);
                MigrationPovertyFamily.SeedAll(world);
            }
            else if (form == "A")
            {
                scale = (float[])probe.GetValue(null);
                foreach (Country c in world.Countries) { scale[(int)c.Id] = (float)k[c.Id]; }
            }

            var go = new GameObject($"T3_{form}_{seed}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                // the seed, read before the first day: real and nominal GDP, the price level and the trade balance the identity is handed on day one
                var y0 = new Dictionary<CountryId, double>();
                var nominal0 = new Dictionary<CountryId, double>();
                var price0 = new Dictionary<CountryId, double>();
                var nx0 = new Dictionary<CountryId, double>();
                foreach (Country c in world.Countries) { y0[c.Id] = c.State.GDP; nominal0[c.Id] = c.State.NominalGdp; price0[c.Id] = Math.Max(0.0001, c.State.PriceLevel); nx0[c.Id] = c.State.TradeBalance; }

                // the seed's first day: the propensity the identity solved with, and the implied potential
                sim.AdvanceDay();
                foreach (Country c in world.Countries)
                {
                    double a = (c.State.Consumption + c.State.Investment) / y0[c.Id];
                    double identityG = lines0[c.Id] * k[c.Id] / price0[c.Id];   // both forms put the identity's G at the sourced share on the seed's day
                    if (form == "lines") { identityG = lines0[c.Id] / price0[c.Id]; }
                    double implied = y0[c.Id] * (2.0 - a) - (identityG + nx0[c.Id]);
                    double ft7 = c.PotentialGdpSeed;
                    sb.Append(F("T3SEED|{0}|{1}|{2}|{3:R}|{4:R}|{5:R}|{6:R}|{7:R}|{8:R}|{9:R}|{10:R}|{11:R}\n",
                        form, seed, c.Id, y0[c.Id], lines0[c.Id] / nominal0[c.Id] * 100.0, P3S13Share2023[c.Id], k[c.Id], a, identityG, nx0[c.Id], implied, ft7));
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
                            double lines = 0.0;
                            foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { lines += line.Amount; } }
                            double gShare = lines * (form == "A" ? k[c.Id] : 1.0) / c.State.NominalGdp * 100.0;
                            sb.Append(F("T3|{0}|{1}|{2}|{3}|{4:R}|{5:R}|{6:R}|{7:R}|{8:R}|{9:R}\n",
                                form, seed, c.Id, year, c.State.GDP, c.State.PotentialGDP, c.State.Unemployment, c.State.DebtToGdpRatio, gShare, c.State.Inflation));
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
            finally
            {
                if (scale != null) { for (int i = 0; i < scale.Length; i++) { scale[i] = 1f; } }
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
