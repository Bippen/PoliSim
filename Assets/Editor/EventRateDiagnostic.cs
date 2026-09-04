using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-D1 (2026-09-04): the event rate, MEASURED rather than read off the constant. Runs <see cref="Seeds"/> fresh
    /// worlds for <see cref="Years"/> turns each (one turn is one year - SimulationManager.DaysPerTurn is 365, which is
    /// the fact the old 0.12 hid: "12 % per turn" read as frequent and meant 1.2 events per ten-year game), counts the
    /// events that fire for every country, and reports the realised rate per country-year, the share of ten-year games
    /// with no event at all, and the distinct-event coverage of the pool. Asserts the realised rate sits within three
    /// binomial standard errors of `EventSystem.EventChancePerTurn` - a drift here means the roll is not the constant's.
    ///
    /// Sits on the simulation bar for the group's own reason: it builds and advances worlds.
    /// </summary>
    public static class EventRateDiagnostic
    {
        private const int Seeds = 60;     // 60 worlds x 10 years x 6 countries = 3,600 country-years: the rate to about +-0.8 pp
        private const int Years = 10;     // the sheet's own unit - "what a player sees in a ten-year game"
        private const int FirstSeed = 1000;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int countryYears = 0;
            int fired = 0;
            int gamesWithNone = 0;
            int games = 0;
            var distinct = new HashSet<string>();
            for (int s = 0; s < Seeds; s++)
            {
                SimulationRandom.Seed(FirstSeed + s);
                World world = WorldFactory.CreateDefault();
                var go = new GameObject("EVENTRATE");
                try
                {
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    var decisions = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                    var firedFor = new Dictionary<CountryId, int>();
                    foreach (Country c in world.Countries) { firedFor[c.Id] = 0; }
                    for (int year = 1; year <= Years; year++)
                    {
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                        sim.AdvanceTurn(decisions);
                        foreach (Country c in world.Countries)
                        {
                            countryYears++;
                            EconomicEvent e = sim.GetLastEvent(c.Id);
                            if (e == null) { continue; }
                            fired++;
                            firedFor[c.Id]++;
                            distinct.Add(e.Name);
                        }
                    }
                    foreach (Country c in world.Countries)
                    {
                        games++;
                        if (firedFor[c.Id] == 0) { gamesWithNone++; }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            float p = EventSystem.EventChancePerTurn;
            float realised = countryYears > 0 ? fired / (float)countryYears : 0f;
            float standardError = Mathf.Sqrt(p * (1f - p) / Mathf.Max(1, countryYears));
            float expectedNone = Mathf.Pow(1f - p, Years);
            float realisedNone = games > 0 ? gamesWithNone / (float)games : 0f;
            bool ok = Mathf.Abs(realised - p) <= 3f * standardError;
            Debug.Log($"EVENTRATE: {fired} events in {countryYears} country-years - {realised:0.000} per country-year against the constant's {p:0.00} " +
                      $"(+-{standardError:0.000} s.e.); {realised * Years:0.0} events per ten-year game; {realisedNone:P1} of {games} ten-year games saw none " +
                      $"(the constant predicts {expectedNone:P1}); {distinct.Count} distinct events of the pool's drawn.");
            if (!ok)
            {
                Debug.LogError($"EVENTRATE: the realised rate {realised:0.000} is more than three standard errors from {p:0.00} - the roll is not the constant's.");
            }
            Debug.Log(ok ? "=== EventRateDiagnostic: ALL ASSERTIONS PASS ===" : "=== EventRateDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
