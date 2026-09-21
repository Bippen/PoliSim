using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **FT-10's instrument (2026-09-21): the energy layer's price indices, decomposed against the price level, per country, over the
    /// no-policy century and past it.** §538's diff found Poland's `EnergyIndustryPrice` past 10⁶ at t361 and near 10²⁰ at t1000, and its
    /// REAL household price at 7 × 10⁸ - in `traj_p6d1` and `traj_p6e1` alike. This measures the mechanism and applies nothing: it steps
    /// the same world the baseline dump steps (seed 777, no decisions) and at each checkpoint recomputes the year's book the way the
    /// boundary does (`EnergyMarket.BeginTurn`, `Clear`, `EnergyLedger.Compute` - all pure reads) and prints every component of both
    /// classes' stacks DEFLATED BY THE COUNTRY'S OWN PRICE LEVEL. A component that carries the price level once prints a constant; one
    /// that carries it twice prints a figure growing as the price level; one that imports another country's price level prints the ratio
    /// of the two. Beside them: the clearing's blocks (price, scarcity, unserved), the support line against its seed, the levy scale,
    /// the liberalisation gap and the electricity tax's delta - every term `EnergyLedger.Compute` reads.
    ///
    /// <para>A measurement, not a check: it asserts nothing and is not in any bar. Run it with
    /// `-executeMethod PoliSim.EditorTools.EnergyRunawayDiagnostic.Run`; `-ft10turns=` overrides the horizon.</para>
    /// </summary>
    public static class EnergyRunawayDiagnostic
    {
        private static readonly int[] Checkpoints = { 1, 5, 20, 50, 100, 200, 361, 500, 750, 1000 };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int horizon = 1000;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-ft10turns=", StringComparison.Ordinal) && int.TryParse(arg.Substring(11), out int t) && t > 0) { horizon = t; }
            }

            var sb = new StringBuilder();
            sb.Append(F("=== EnergyRunawayDiagnostic (FT-10): the energy price stack deflated by each country's own price level, seed 777, no policy, {0} turns ===\n", horizon));
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FT10");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                Snapshot(sb, world, 0);
                int next = 0;
                for (int turn = 1; turn <= horizon; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    while (next < Checkpoints.Length && Checkpoints[next] < turn) { next++; }
                    if (next < Checkpoints.Length && Checkpoints[next] == turn) { Snapshot(sb, world, turn); next++; }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                EnergyMarket.ResetTurnState();
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static void Snapshot(StringBuilder sb, World world, int turn)
        {
            sb.Append(F("\n--- t{0} ---------------------------------------------------------------------------------------------\n", turn));
            EnergyMarket.BeginTurn(world);   // what the boundary does before it clears: Sweden's water value from Germany's and Poland's clearings
            try
            {
                foreach (Country country in world.Countries)
                {
                    if (!EnergyLayer.Has(country.Id) || country.Environment == null || !country.Environment.Seeded) { continue; }
                    double p = Math.Max(0.0001f, country.State.PriceLevel);
                    EnergyMarket.Result r = EnergyMarket.Clear(country);
                    EnergyLedger.Book b = EnergyLedger.Compute(country, r, p, EnergyLedger.CreditFor(country));
                    SpendingLine line = null;
                    foreach (SpendingLine l in country.SpendingLines) { if (l.Category == SpendingCategory.Energy) { line = l; break; } }

                    sb.Append(F("{0,-8} P {1:G6} · inflation {2:F2} · state: industry price/P {3:G5}, household real {4:G5}, real change {5:F2} %, pass-through {6:F3} pp\n",
                        country.Id, p, country.State.Inflation, country.State.EnergyIndustryPrice / p, country.State.EnergyHouseholdPriceReal,
                        country.State.EnergyHouseholdPriceRealChange, EnergyPassThrough.Planned(country)));
                    for (int c = 0; c < b.Classes.Length; c++)
                    {
                        EnergyLedger.ClassStack s = b.Classes[c];
                        sb.Append(F("         {0,-14} /P: wholesale {1:G5} · margin {2:G5} · network {3:G5} · policy {4:G5} · taxenv {5:G5} · vat {6:G5} · TOTAL {7:G5}   (tax shift {8:G4})\n",
                            s.Class, s.Wholesale / p, s.Margin / p, s.Network / p, s.Policy / p, s.TaxEnv / p, s.Vat / p, s.Total / p, s.ElectricityTaxShift / p));
                    }
                    sb.Append(F("         levy scale {0:G5} · support deviation {1:G5} bn (line {2:G5}, path {3:G5}, line/path {4:G5}, K {9:G5}) · network credit/P {5:G4} · liberalisation gap {6:G4} · el.tax delta hh {7:G4} nh {8:G4}\n",
                        b.LevyScale, b.SupportDeviation, line != null ? line.Amount : 0f, line != null ? line.SeedAmount : 0f,
                        line != null && line.SeedAmount > 0f ? (double)line.Amount / line.SeedAmount : 0.0, b.NetworkCreditPerKwh / p,
                        EnergyLedger.LiberalisationGap(country), EnergyLedger.ElectricityTaxDeltaEurPerMwh(country, 0), EnergyLedger.ElectricityTaxDeltaEurPerMwh(country, 1), country.Environment.EnergySupportLineToLevySeed));
                    if (r.Zones != null)
                    {
                        for (int z = 0; z < r.Zones.Length; z++)
                        {
                            sb.Append(F("         clearing {0,-4}", r.ZoneNames != null && z < r.ZoneNames.Length ? r.ZoneNames[z] : "?"));
                            for (int k = 0; k < r.Zones[z].Length; k++)
                            {
                                EnergyMarket.BlockResult br = r.Zones[z][k];
                                if (br == null) { continue; }
                                sb.Append(F(" | b{0} price/P {1:G5} (nominal {2:G5}) scarcity/P {3:G4} unserved {4:F0} MW", k, br.Price / p, br.Price, br.Scarcity / p, br.UnservedMw));
                            }
                            sb.Append('\n');
                        }
                    }
                }
            }
            finally { EnergyMarket.EndTurn(); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
