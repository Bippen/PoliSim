using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PASS 5 MEASUREMENT (tariff-to-stock, 2026-08-26): the tariff take per country, measured
    /// before anything is routed - the recalibration interaction the pass turns on. Pass 1 solved
    /// each country's CollectionEfficiency against a real tax-to-GDP target WITHOUT tariff revenue
    /// in the fiscal path; routing a real flow on top moves every primary balance off its
    /// calibrated target by the take's size, so the size is measured here per country, against
    /// GDP, against theoretical revenue (the CE decrement that would keep year-1 revenue
    /// unchanged), and against the primary balance pass 1 landed.
    ///
    /// Two readings of the same figure, asserted equal: the real path (TradeSystem's return, as
    /// recorded on the turn-1 FiscalTurnReport) and the pure sum over the country's own links
    /// (ImportVolume x GetTariffRate), so the measurement cannot silently read a different formula
    /// than the model runs. Then the lever's reach - every partner at MaxBaseTariffRate - and the
    /// static-volume decay: trade volumes never grow, so the take's share of GDP at t100 is what a
    /// seed-neutral CE decrement leaves behind at the ruled window.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.TariffTakeDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class TariffTakeDiagnostic
    {
        [MenuItem("PoliSim/Run Tariff Take Measurement (pass 5)")]
        private static void RunFromMenu() => Run();

        private const int Seed = 777;
        private const int Turns = 100;
        private const float MaxTariffRatePercent = 50f; // SimulationManager.MaxBaseTariffRate, private there
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static float TheoreticalRevenue(Country c)
        {
            float revenue = 0f;
            foreach (TaxLine line in c.TaxLines)
            {
                if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                revenue += c.State.GDP * (line.Rate / 100f) * line.BaseShareOfGdp;
            }

            return revenue;
        }

        private static float PureTariffTake(Country c, World world, float rateOverride = -1f)
        {
            float take = 0f;
            foreach (TradePartner link in c.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null) { continue; }
                float rate = rateOverride >= 0f ? rateOverride : TradeSystem.GetTariffRate(c, partner, world.TradeBlocs);
                take += link.ImportVolume * (rate / 100f);
            }

            return take;
        }

        private static float TotalImports(Country c)
        {
            float total = 0f;
            foreach (TradePartner link in c.TradePartners) { total += link.ImportVolume; }
            return total;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            System.Threading.Thread.CurrentThread.CurrentCulture = Inv;
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("TariffTakeDiagnostic");
            bool ok = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                // Seed-state anatomy, before any turn: the pure take against GDP, theoretical revenue
                // and the lever's reach. GDP and volumes are seed values here.
                var seedGdp = new Dictionary<CountryId, float>();
                var seedTheoretical = new Dictionary<CountryId, float>();
                var seedTake = new Dictionary<CountryId, float>();
                Debug.Log("TARIFF: === SEED TAKE (pure sum over links; GDP and volumes at seed) ===");
                foreach (Country c in world.Countries)
                {
                    float gdp = c.State.GDP;
                    float theoretical = TheoreticalRevenue(c);
                    float take = PureTariffTake(c, world);
                    float maxTake = PureTariffTake(c, world, MaxTariffRatePercent);
                    float imports = TotalImports(c);
                    seedGdp[c.Id] = gdp; seedTheoretical[c.Id] = theoretical; seedTake[c.Id] = take;
                    float neutralCe = c.CollectionEfficiency - take / Mathf.Max(1e-6f, theoretical);
                    Debug.Log($"TARIFF[{c.Id}] imports {imports:F1} ({imports / gdp * 100f:F2}% GDP) | take {take:F3} = {take / gdp * 100f:F4}% GDP = {take / (theoretical * c.CollectionEfficiency) * 100f:F3}% of year-1 tax revenue | " +
                              $"theoretical {theoretical:F1} CE {c.CollectionEfficiency:F4} -> seed-neutral CE {neutralCe:F4} (delta {neutralCe - c.CollectionEfficiency:+0.0000;-0.0000}) | " +
                              $"every partner at {MaxTariffRatePercent:F0}%: {maxTake:F1} = {maxTake / gdp * 100f:F2}% GDP | base rate {c.BaseTariffRate:F1}%");
                }

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int turn = 1; turn <= Turns; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn == 1)
                    {
                        Debug.Log("TARIFF: === TURN 1 (the real path: FiscalTurnReport, seed 777) ===");
                        foreach (Country c in world.Countries)
                        {
                            FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                            float pure = seedTake[c.Id];
                            bool same = Mathf.Abs(r.TariffRevenue - pure) < 1e-3f;
                            ok &= same;
                            float primary = r.BudgetBalance + r.InterestOnDebt;
                            Debug.Log($"TARIFF[{c.Id}] t1 report.TariffRevenue {r.TariffRevenue:F3} vs pure {pure:F3} -> {(same ? "SAME" : "DIFFERENT")} | " +
                                      $"report.Revenue {r.Revenue:F1} | primary balance {primary:F1} = {primary / c.State.GDP * 100f:F2}% GDP; with the take routed and NOT neutralized it would read {(primary + r.TariffRevenue) / c.State.GDP * 100f:F2}% GDP | " +
                                      $"take/GDP {r.TariffRevenue / c.State.GDP * 100f:F4}%");
                        }
                    }
                }

                Debug.Log($"TARIFF: === TURN {Turns} (static volumes against grown GDP) ===");
                foreach (Country c in world.Countries)
                {
                    FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                    Debug.Log($"TARIFF[{c.Id}] t{Turns} take {r.TariffRevenue:F3} = {r.TariffRevenue / c.State.GDP * 100f:F4}% GDP (seed share {seedTake[c.Id] / seedGdp[c.Id] * 100f:F4}%) | GDP x{c.State.GDP / seedGdp[c.Id]:F2}");
                }

                Debug.Log(ok ? "TARIFF: PASS - the real path and the pure sum agree for all six." : "TARIFF: FAIL - the real path and the pure sum DISAGREE somewhere above.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            MeasureExploit();
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>
        /// THE FREE LEVER, measured (pass 5's trade-war finding): a player who sets the 50% cap on
        /// every partner override collects imports x 0.5 with no import reduction and no retaliation
        /// (static volumes; overrides excluded from the Parliament vote by stated simplification).
        /// Sweden - the country with the largest import share of GDP - runs 30 turns with every
        /// override at the cap against a same-seed no-policy control; the debt path difference is the
        /// lever's fiscal reach under whichever books are in force (nothing under the pre-pass-5
        /// accumulator, the whole take under the routed books).
        /// </summary>
        private static void MeasureExploit()
        {
            const int ExploitTurns = 30;
            float[] controlRatio = RunSweden(ExploitTurns, exploit: false, out float controlTake);
            float[] exploitRatio = RunSweden(ExploitTurns, exploit: true, out float exploitTake);
            Debug.Log($"TARIFF EXPLOIT: Sweden, every partner override at {MaxTariffRatePercent:F0}% from t1 (seed {Seed}, {ExploitTurns} turns): take/turn {exploitTake:F2} vs {controlTake:F2} no-policy; " +
                      $"debt-to-GDP t10 {controlRatio[10]:F1} -> {exploitRatio[10]:F1}, t20 {controlRatio[20]:F1} -> {exploitRatio[20]:F1}, t30 {controlRatio[30]:F1} -> {exploitRatio[30]:F1} " +
                      $"(delta t30 {exploitRatio[30] - controlRatio[30]:+0.0;-0.0} ratio-points).");
        }

        private static float[] RunSweden(int turns, bool exploit, out float takePerTurn)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"TariffExploit_{(exploit ? "on" : "off")}");
            var ratio = new float[turns + 1];
            takePerTurn = 0f;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country sweden = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int turn = 1; turn <= turns; turn++)
                {
                    if (exploit && turn == 1)
                    {
                        var overrides = new Dictionary<CountryId, float>();
                        foreach (TradePartner link in sweden.TradePartners) { overrides[link.PartnerId] = MaxTariffRatePercent; }
                        decisions[CountryId.Sweden] = new PolicyDecision { PartnerTariffOverrides = overrides };
                    }
                    else
                    {
                        decisions[CountryId.Sweden] = PolicyDecision.None();
                    }

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ratio[turn] = sweden.State.DebtToGdpRatio;
                    if (turn == turns) { takePerTurn = sim.GetLastFiscalReport(CountryId.Sweden).TariffRevenue; }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return ratio;
        }
    }
}
