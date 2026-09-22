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
        /// <summary>
        /// §574 (2026-09-22, the efficiency pass): THE HORIZONS A FAMILY DUMPS. A family needs the 20-turn sentinel and the 100-turn bounds pass;
        /// the 500 and the 1000 are a TRACK'S CLOSE, where a long-run drift is the question being asked. Measured: the three horizons for two seeds
        /// cost 513 s (`pap_dump`, 2026-09-22) and the 100s alone cost about a third of it - **3.4 minutes a family**, paid on every family this
        /// project has landed. Pass `-trajhorizons=100,500,1000` at a track's close; the default is what a family owes.
        /// </summary>
        private static readonly int[] FamilyHorizons = { 100 };

        /// <summary>See <see cref="FamilyHorizons"/> - what a track's close asks for, by name.</summary>
        private static readonly int[] CloseHorizons = { 100, 500, 1000 };
        private const string DefaultOutputDirectory = "../PoliSim-captures/trajectories";

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; a measurement taken while the model's self-audit fails is meaningless, so an ATTRIB during it exits nonzero even though this tool exits 0 by design otherwise.
            string label = Arg("-trajlabel=", "run");
            string outDir = Arg("-trajout=", DefaultOutputDirectory);
            Directory.CreateDirectory(outDir);

            FieldInfo[] stateFields = StateFields();
            // §574: the horizons are the family's unless the caller names others - `-trajhorizons=100,500,1000` at a track's close, `-trajhorizons=all` for the same.
            string asked = Arg("-trajhorizons=", string.Empty);
            int[] horizons = FamilyHorizons;
            if (string.Equals(asked, "all", StringComparison.OrdinalIgnoreCase)) { horizons = CloseHorizons; }
            else if (asked.Length > 0)
            {
                string[] pieces = asked.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var parsed = new List<int>();
                foreach (string p in pieces) { if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) && t > 0) { parsed.Add(t); } }
                if (parsed.Count == 0) { Debug.LogError($"TRAJ: -trajhorizons='{asked}' named no turn count - refusing rather than dumping a horizon nobody asked for."); CheckExit.Finish(1); return; }
                horizons = parsed.ToArray();
            }

            Debug.Log($"TRAJ: {stateFields.Length} public EconomyState fields per country per turn, label '{label}', horizons {string.Join(", ", horizons)}.");

            foreach (int seed in Seeds)
            {
                foreach (int horizon in horizons)
                {
                    string path = Path.Combine(outDir, $"traj_{label}_s{seed}_t{horizon}.csv");
                    DumpOne(seed, horizon, stateFields, path);
                }
            }

            Debug.Log("TRAJ: done.");
            CheckExit.Finish(0);
        }

        /// <summary>Every public EconomyState field, in the dump's order (ordinal by name).</summary>
        public static FieldInfo[] StateFields()
        {
            FieldInfo[] stateFields = typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(stateFields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            return stateFields;
        }

        private static void DumpOne(int seed, int horizon, FieldInfo[] stateFields, string path)
        {
            File.WriteAllText(path, Build(seed, horizon, stateFields));
            Debug.Log($"TRAJ: wrote {path} ({horizon} turns, seed {seed}).");
        }

        /// <summary>The dump's text for one seed and horizon - what <see cref="DumpOne"/> writes and what `TrajectorySentinelCheck` hashes (§496).</summary>
        public static string Build(int seed, int horizon, FieldInfo[] stateFields)
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
                        // FT-8 (§398): the natural rate the macro core reads - seed-plus-laws plus the labour force's demographic shift - so the gap is in every dump.
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Country.EffectiveNaturalRate,")
                          .Append(country.EffectiveNaturalUnemploymentRate.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                        // Pass 4 (the Taylor-path gap fix): the rule's own reading, so a before/after
                        // diff carries the suggested-rate trajectory directly - B-only NEW fields to
                        // any pre-pass-4 dump, per the allowance. GapTermPp is the rule's cyclical
                        // term in percentage points, read through the rule's own accessor so the
                        // column follows whatever gap the rule reads (the pre_pass4 dumps carry the
                        // level-gap term it read then); ChairTarget is -999 where no chair sits.
                        float gapTermPp = TaylorRule.GetGapTermPercentagePoints(country);
                        float suggested = TaylorRule.GetSuggestedInterestRate(country);
                        float chairTarget = country.CurrentFedChair == null
                            ? -999f
                            : Mathf.Clamp(suggested + country.CurrentFedChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Taylor.GapTermPp,")
                          .Append(gapTermPp.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Taylor.SuggestedRate,")
                          .Append(suggested.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                        sb.Append(turn).Append(',').Append(country.Id).Append(",Taylor.ChairTarget,")
                          .Append(chairTarget.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                    }
                }

                return sb.ToString();
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
