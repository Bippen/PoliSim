using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Continuous Time Phase 4, step 0: full-trajectory reference dumps, captured BEFORE any
    /// conversion exists (the Step A lesson: an untainted reference cannot be reconstructed after
    /// the change). One CSV per (seed, horizon): every public EconomyState field for every country
    /// at every turn boundary, by reflection - a field added later joins the dump without editing
    /// this file - plus the two Country-level figures the phase directive names for per-country
    /// reporting (PotentialGrowthRate, and the zone interest rate for context).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.TrajectoryBaselineDump.Run -logFile &lt;path&gt;
    /// [-trajlabel=&lt;era&gt;] [-trajout=&lt;dir&gt;]`
    ///
    /// Driving idiom is SimulationTestRunner's, not the play loop's: AdvanceDay x DaysPerTurn then
    /// AdvanceTurn with all-None decisions, no player country, no bills - a pure simulation
    /// trajectory, which is what the phase matrices have always compared. Output goes OUT OF TREE
    /// (beside the captures) so trajectory files never enter git history - the repository-weight
    /// lesson applied before the first file exists.
    /// </summary>
    public static class TrajectoryBaselineDump
    {
        private static readonly int[] Seeds = { 777, 424242 };
        private static readonly int[] Horizons = { 100, 500, 1000 };
        private const string DefaultOutputDirectory = "../PoliSim-captures/trajectories";

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; a measurement taken while the model's self-audit fails is meaningless, so an ATTRIB during it exits nonzero even though this tool exits 0 by design otherwise.
            string label = Arg("-trajlabel=", "run");
            string outDir = Arg("-trajout=", DefaultOutputDirectory);
            Directory.CreateDirectory(outDir);

            FieldInfo[] stateFields = typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(stateFields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            Debug.Log($"TRAJ: {stateFields.Length} public EconomyState fields per country per turn, label '{label}'.");

            foreach (int seed in Seeds)
            {
                foreach (int horizon in Horizons)
                {
                    string path = Path.Combine(outDir, $"traj_{label}_s{seed}_t{horizon}.csv");
                    DumpOne(seed, horizon, stateFields, path);
                }
            }

            Debug.Log("TRAJ: done.");
            CheckExit.Finish(0);
        }

        private static void DumpOne(int seed, int horizon, FieldInfo[] stateFields, string path)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"TRAJ_{seed}_{horizon}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country country in world.Countries)
                {
                    decisions[country.Id] = PolicyDecision.None();
                }

                var sb = new StringBuilder(1 << 22);
                sb.Append("turn,country,field,value\n");

                for (int turn = 1; turn <= horizon; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                    }

                    sim.AdvanceTurn(decisions);

                    foreach (Country country in world.Countries)
                    {
                        foreach (FieldInfo field in stateFields)
                        {
                            sb.Append(turn).Append(',').Append(country.Id).Append(',').Append(field.Name).Append(',')
                              .Append(ToInvariant(field.GetValue(country.State))).Append('\n');
                        }

                        sb.Append(turn).Append(',').Append(country.Id).Append(",Country.PotentialGrowthRate,")
                          .Append(country.PotentialGrowthRate.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Zone.InterestRate,")
                          .Append((country.CurrencyZone?.InterestRate ?? -999f).ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                        // R4 (maturity rate-lag): the third Country-level extra, so the lag effect
                        // is directly decomposable from any dump (spot vs effective, per turn) -
                        // a B-only NEW field to any pre-R4 diff, which the allowance names.
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Country.EffectiveDebtRate,")
                          .Append(country.EffectiveDebtInterestRate.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                    }
                }

                File.WriteAllText(path, sb.ToString());
                Debug.Log($"TRAJ: wrote {path} ({horizon} turns, seed {seed}).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string ToInvariant(object value)
        {
            switch (value)
            {
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case bool b: return b ? "1" : "0";
                default: return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string Arg(string prefix, string fallback)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return fallback;
        }
    }
}
