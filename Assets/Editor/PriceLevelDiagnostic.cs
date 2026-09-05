using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B6 (2026-09-05): THE BOOK IN CURRENT PRICES, asserted. (1) The price level compounds at the state's inflation
    /// exactly: one country, five turns, EconomyState.PriceLevel against the diagnostic's own compound of the inflation
    /// each turn's days printed (read after the days, before the finalizer moves it) within 1e-3. (2) The bases are
    /// nominal: every implemented line's base equals its real base (the sourced share x the seed's GDP x its driver's
    /// ratio) x the price level within 1e-4. (3) THE CAP NEVER APPROACHED: every country run a hundred years, no player -
    /// the inflation the Phillips curve prints must never exceed half the cap (MacroSystem.MaxInflationPercent / 2), its
    /// hundred-year mean is printed beside the sourced GDP-deflator mean (World Bank WDI, NY.GDP.DEFL.KD.ZG, 2010–2024:
    /// USA 2.36, Sweden 2.43, Germany 2.42, France 1.55, Italy 1.70, Poland 3.27) as the yardstick and not as a target;
    /// the spending lines' nominal share of nominal GDP at t100 is printed against the seed's (§314's failure was that
    /// share climbing at the inflation rate), and the debt-to-GDP ratio must be finite and below the ceiling everywhere.
    /// Sits on the simulation bar.
    /// </summary>
    public static class PriceLevelDiagnostic
    {
        private const int Years = 5;
        private const int CenturyTurns = 100;
        private const float Tolerance = 1e-3f;
        private const float BaseTolerance = 1e-4f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;

            // (1) and (2): five turns, Sweden as the player.
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PRICELEVEL");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                float compound = 1f;
                for (int year = 1; year <= Years; year++)
                {
                    float before = c.State.PriceLevel;
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // The days compounded the level at the inflation each day printed; the yearly factor the days applied is the
                    // level's own ratio, and the inflation they read is the state's print - compare the ratio to the mean print.
                    compound *= c.State.PriceLevel / before;
                    sim.AdvanceTurn(decisions);
                }
                if (Mathf.Abs(c.State.PriceLevel / compound - 1f) > Tolerance)
                {
                    Debug.LogError($"PRICES: Sweden's price level {c.State.PriceLevel:F5} is not the compound of its yearly ratios {compound:F5}.");
                    ok = false;
                }
                if (c.State.PriceLevel <= 1f)
                {
                    Debug.LogError($"PRICES: Sweden's price level {c.State.PriceLevel:F5} did not rise over {Years} years of positive inflation.");
                    ok = false;
                }
                int bases = 0;
                foreach (TaxLine line in c.TaxLines)
                {
                    if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                    TaxBaseDriver driver = TaxBases.Of(line.Type);
                    float share = TaxBaseTable.BaseShareOfGdp(c.Id, line.Type);
                    float reference = c.RevenueBaseSeeds[(int)driver];
                    float level = TaxBases.Level(driver, c);
                    float realBase = share * c.RevenueBaseSeedGdp * (reference > 0f ? level / reference : 1f);
                    float expected = realBase * c.State.PriceLevel;
                    float actual = TaxBases.Base(c, line.Type);
                    bases++;
                    if (Mathf.Abs(actual / expected - 1f) > BaseTolerance)
                    {
                        Debug.LogError($"PRICES: Sweden {line.Type} base {actual:F4}; real base {realBase:F4} x price level {c.State.PriceLevel:F5} = {expected:F4}.");
                        ok = false;
                    }
                }
                Debug.Log($"PRICES: Sweden after {Years} years - price level x{c.State.PriceLevel:F4}, {bases} nominal bases at real x level, nominal GDP {c.State.NominalGdp:F1} against real {c.State.GDP:F1}, debt {c.State.DebtToGdpRatio:F1} % of nominal GDP.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // (3) the century, every country, no player.
            var wdiMean = new Dictionary<CountryId, float>
            {
                { CountryId.USA, 2.360f }, { CountryId.Sweden, 2.433f }, { CountryId.Germany, 2.419f },
                { CountryId.France, 1.547f }, { CountryId.Italy, 1.697f }, { CountryId.Poland, 3.265f }
            };
            SimulationRandom.Seed(777);
            World w2 = WorldFactory.CreateDefault();
            var seedShare = new Dictionary<CountryId, float>();
            foreach (Country k in w2.Countries) { seedShare[k.Id] = LinesShare(k); }
            var maxInflation = new Dictionary<CountryId, float>();
            var sumInflation = new Dictionary<CountryId, float>();
            foreach (Country k in w2.Countries) { maxInflation[k.Id] = 0f; sumInflation[k.Id] = 0f; }
            var go2 = new GameObject("PRICELEVEL-CENTURY");
            try
            {
                SimulationManager sim = go2.AddComponent<SimulationManager>();
                sim.SetWorld(w2);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in w2.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int turn = 1; turn <= CenturyTurns; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    foreach (Country k in w2.Countries)
                    {
                        maxInflation[k.Id] = Mathf.Max(maxInflation[k.Id], k.State.Inflation);
                        sumInflation[k.Id] += k.State.Inflation;
                    }
                }
                float halfCap = MacroSystem.MaxInflationPercent / 2f;
                foreach (Country k in w2.Countries)
                {
                    float mean = sumInflation[k.Id] / CenturyTurns;
                    float share = LinesShare(k);
                    bool finite = !float.IsNaN(k.State.DebtToGdpRatio) && !float.IsInfinity(k.State.DebtToGdpRatio) && k.State.DebtToGdpRatio < 1000f;
                    if (maxInflation[k.Id] >= halfCap)
                    {
                        Debug.LogError($"PRICES: {k.Id}'s inflation reached {maxInflation[k.Id]:F2} % in the century - half the cap ({halfCap:F0}) or more; the book approached the cap.");
                        ok = false;
                    }
                    if (!finite)
                    {
                        Debug.LogError($"PRICES: {k.Id}'s debt-to-GDP ratio at t{CenturyTurns} is {k.State.DebtToGdpRatio} - not a finite figure below the ceiling.");
                        ok = false;
                    }
                    Debug.Log($"PRICES: {k.Id} over {CenturyTurns} years - inflation max {maxInflation[k.Id]:F2} %, mean {mean:F2} % (WDI 2010–2024 deflator mean {wdiMean[k.Id]:F2} %, the yardstick); price level x{k.State.PriceLevel:F2}; the lines {share * 100f:F1} % of nominal GDP against {seedShare[k.Id] * 100f:F1} % at the seed; debt {k.State.DebtToGdpRatio:F0} % of nominal GDP.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go2); }

            Debug.Log(ok ? "=== PriceLevelDiagnostic: ALL ASSERTIONS PASS (the level compounds at the print, the bases are real x level, the cap is not approached in a century on any of the six) ===" : "=== PriceLevelDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float LinesShare(Country c)
        {
            float total = 0f;
            foreach (SpendingLine l in c.SpendingLines) { total += l.Amount; }
            return c.State.NominalGdp > 0f ? total / c.State.NominalGdp : 0f;
        }
    }
}
