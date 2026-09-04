using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B7 (2026-09-05): THE PREMISE OF POTENTIAL OUTPUT, MEASURED. Writes `POTENTIAL_PREMISE.md` at the repo root:
    /// what sets PotentialGrowthRate (the ledger: the seeded base plus the two ceilinged adjustments, read as trend
    /// productivity and assigned 1:1 to potential), and what it ignores - the working-age cohort, participation and
    /// the natural rate, i.e. the labour input. For every country, no player, one hundred turns: PotentialGDP against
    /// its seed, GDP against its seed, the labour input at the natural rate (the 20–64 cohort × participation × (1 −
    /// NAIRU)) against its seed, the Productivity stat against its seed, and the ratio potential would carry if it
    /// were labour × productivity. Re-run after the build, the same columns say what changed.
    /// </summary>
    public static class PotentialPremiseDump
    {
        private const int Horizon = 100;
        private static readonly int[] Rows = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        private static string Inv(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("# The premise of potential output - measured, no player, 100 turns (P5-B7)\n\n");
            sb.Append("**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.\n\n");
            sb.Append("**Labour input at the natural rate** = the 20–64 cohort (`SpendingDrivers.Level`, WorkingAge20To64) × `EconomyState.LaborForceParticipationRate` / 100 × (1 − `Country.NaturalUnemploymentRate` / 100). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.\n\n");

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var seedPot = new Dictionary<CountryId, float>();
            var seedGdp = new Dictionary<CountryId, float>();
            var seedLab = new Dictionary<CountryId, float>();
            var seedProd = new Dictionary<CountryId, float>();
            var seedDebt = new Dictionary<CountryId, float>();
            var seedCohort = new Dictionary<CountryId, float>();
            foreach (Country c in world.Countries)
            {
                seedPot[c.Id] = c.State.PotentialGDP; seedGdp[c.Id] = c.State.GDP; seedLab[c.Id] = Labour(c); seedProd[c.Id] = c.State.Productivity; seedDebt[c.Id] = c.State.GovernmentDebt; seedCohort[c.Id] = SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, c);
            }
            var lines = new Dictionary<CountryId, List<string>>();
            foreach (Country c in world.Countries) { lines[c.Id] = new List<string>(); }
            var go = new GameObject("POTENTIAL-PREMISE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int turn = 1; turn <= Horizon; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (System.Array.IndexOf(Rows, turn) < 0) { continue; }
                    foreach (Country c in world.Countries)
                    {
                        float lab = Labour(c) / seedLab[c.Id];
                        float prod = c.State.Productivity / seedProd[c.Id];
                        lines[c.Id].Add($"| {turn} | {Inv(c.PotentialGrowthRate)} | {Inv(c.State.PotentialGDP / seedPot[c.Id])} | {Inv(c.State.GDP / seedGdp[c.Id])} | {Inv(SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, c) / seedCohort[c.Id])} | {Inv(c.State.LaborForceParticipationRate)} | {Inv(lab)} | {Inv(prod)} | {Inv(lab * prod)} | {Inv(c.State.GovernmentDebt / Mathf.Max(1f, c.State.GDP) * 100f)} |");
                    }
                }
            }
            finally { Object.DestroyImmediate(go); }

            foreach (Country c in world.Countries)
            {
                sb.Append($"## {c.Id} - base trend {Inv(c.BasePotentialGrowthRate)} % a year; seed potential {Inv(seedPot[c.Id])}, GDP {Inv(seedGdp[c.Id])}, labour input {Inv(seedLab[c.Id])} M at the natural rate, productivity {Inv(seedProd[c.Id])}\n\n");
                sb.Append("| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |\n|---|---|---|---|---|---|---|---|---|---|\n");
                foreach (string l in lines[c.Id]) { sb.Append(l).Append('\n'); }
                sb.Append('\n');
            }
            sb.Append("**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; the debt column is the fiscal book paying for the difference since P5-B3 put the tax bases on the wage bill.\n");

            string outPath = Path.Combine(Application.dataPath, "..", "POTENTIAL_PREMISE.md");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"POTENTIAL: wrote {Path.GetFullPath(outPath)}");
            CheckExit.Finish(0);
        }

        /// <summary>The labour input at the natural rate: the 20–64 cohort × participation × (1 − NAIRU).</summary>
        private static float Labour(Country c)
            => SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, c) * Mathf.Clamp(c.State.LaborForceParticipationRate, 0f, 100f) / 100f * Mathf.Clamp(100f - c.NaturalUnemploymentRate, 0f, 100f) / 100f;
    }
}
