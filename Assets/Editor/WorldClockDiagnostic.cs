using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-1 (2026-09-25, §618): THE WORLD CLOCK, ASSERTED. For every country: its start follows the ruled rule (the standard run-up before
    /// its polling day, or the snap trigger's day); the chamber of record on that date is one of the records' and its seat table sums to
    /// the chamber's size with every key in the roster; a world built on that start seats exactly that table and is governed by the
    /// government of record where its cabinet is sourced; the two chambers billed as E-47 are reported as deviations, not seated silently;
    /// no other start falls inside France's window without a chamber (2024-06-09 to 2024-07-18, ruled); the default epoch keeps K-1's
    /// standing (Sweden's 2026 chamber, its government PROVISIONAL). The epoch is restored to the default afterwards, whatever happens.
    /// </summary>
    public static class WorldClockDiagnostic
    {
        private static readonly CountryId[] All = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            DateTime epochBefore = SimulationManager.EpochDate;
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== WorldClockDiagnostic (PS-1, §618): each country's start, its chamber and government of record ===\n");
            try
            {
                var starts = new Dictionary<CountryId, DateTime>();
                foreach (CountryId id in All)
                {
                    DateTime start = WorldClock.StartDate(id);
                    starts[id] = start;
                    // the start rule
                    DateTime expected = WorldClock.IsSnapStart(id) || WorldClock.GoverningModeOnly(id) ? start : new CampaignCalendar(WorldClock.LatestElectionDay(id)).PreCampaignStart;
                    if (!WorldClock.IsSnapStart(id) && !WorldClock.GoverningModeOnly(id) && start != expected)
                    {
                        failures++; sb.Append(F("    FAIL {0}: the start {1:yyyy-MM-dd} is not the run-up's first day {2:yyyy-MM-dd}\n", id, start, expected));
                    }

                    // the chamber of record at the start
                    WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(id, start);
                    if (!chamber.Holds(start)) { failures++; sb.Append(F("    FAIL {0}: no chamber of record holds the start {1:yyyy-MM-dd}\n", id, start)); }
                    bool sourced = PartySystems.SeatsSourced(chamber.Vintage);
                    ElectionVintage seatedVintage = WorldClock.SeatedVintage(id, start);
                    string deviation = WorldClock.SeatingDeviation(id, start);
                    if (!sourced && deviation == null) { failures++; sb.Append(F("    FAIL {0}: {1} is unsourced and no deviation is stated\n", id, chamber.Vintage)); }
                    if (sourced && deviation != null) { failures++; sb.Append(F("    FAIL {0}: {1} is sourced but a deviation is stated\n", id, chamber.Vintage)); }

                    // every sourced table sums to the chamber and names roster keys
                    foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(id))
                    {
                        IReadOnlyList<(string Abbrev, int Seats)> table = PartySystems.SeatsAt(c.Vintage);
                        if (table == null) { continue; }
                        int sum = 0;
                        foreach ((string abbrev, int n) in table)
                        {
                            sum += n;
                            bool known = false;
                            foreach (PoliticalParty p in PartySystems.For(id)) { if (p.Abbrev == abbrev) { known = true; break; } }
                            if (!known) { failures++; sb.Append(F("    FAIL {0} {1}: '{2}' is not a roster key\n", id, c.Vintage, abbrev)); }
                        }

                        int size = PartySystems.ChamberSizeAt(id, c.Vintage);   // the record's own total: 736 for the 20th Bundestag, 434 for the 117th House's election-day figures
                        if (sum != size) { failures++; sb.Append(F("    FAIL {0} {1}: the table sums to {2}, the record accounts for {3}\n", id, c.Vintage, sum, size)); }
                    }

                    // a world on this start
                    WorldClock.ApplyStart(id);
                    World world = WorldFactory.CreateDefault();
                    Country country = world.GetCountry(id);
                    Dictionary<string, int> want = PartySystems.InitialSeats(id, seatedVintage);
                    foreach (KeyValuePair<string, int> kv in want)
                    {
                        country.ParliamentSeats.TryGetValue(kv.Key, out int got);
                        if (got != kv.Value) { failures++; sb.Append(F("    FAIL {0}: the world seats {1} at {2}, the {3} table says {4}\n", id, kv.Key, got, seatedVintage, kv.Value)); }
                    }

                    string government;
                    if (WorldClock.TryGovernmentAt(id, start, out WorldClock.GovernmentOfRecord g))
                    {
                        government = g.Head + (g.CabinetSourced ? " [" + string.Join("+", g.Cabinet) + (g.Support != null ? " / support " + string.Join("+", g.Support) : "") + (g.CabinetDerived ? " - DERIVED by the record" : "") + "]" : " [cabinet unsourced - PROVISIONAL]");
                        if (g.CabinetSourced)
                        {
                            foreach (string key in g.Cabinet) { if (!want.ContainsKey(key)) { failures++; sb.Append(F("    FAIL {0}: the cabinet names '{1}', not a roster key\n", id, key)); } }
                            bool installed = SeatedGovernment.TryInstalled(country, out SeatedGovernment.Record rec) && rec.Standing == SeatedGovernment.Standing.Installed;
                            if (!installed) { failures++; sb.Append(F("    FAIL {0}: the government of record is sourced but the world does not read it as INSTALLED\n", id)); }
                            IReadOnlyList<string> cabinet = GovernmentFormation.Cabinet(country);
                            var wantCab = new HashSet<string>(g.Cabinet);
                            var gotCab = new HashSet<string>(cabinet);
                            if (!wantCab.SetEquals(gotCab)) { failures++; sb.Append(F("    FAIL {0}: the formation reads the cabinet as [{1}], the record says [{2}]\n", id, string.Join("+", cabinet), string.Join("+", g.Cabinet))); }
                        }
                        else if (!SeatedGovernment.IsProvisional(country))
                        {
                            failures++; sb.Append(F("    FAIL {0}: the cabinet is unsourced but the world does not mark the government PROVISIONAL\n", id));
                        }
                    }
                    else
                    {
                        government = "outside the formation model (§6, §8)";
                    }

                    sb.Append(F("    {0,-8} opens {1:yyyy-MM-dd} ({2}); chamber of record {3} (elected {4:yyyy-MM-dd}, convened {5:yyyy-MM-dd}); seated {6}{7}; government {8}\n",
                        id, start, WorldClock.GoverningModeOnly(id) ? "governing mode" : WorldClock.IsSnapStart(id) ? "snap trigger" : "standard run-up",
                        chamber.Vintage, chamber.ElectionDay, chamber.Convened, seatedVintage, deviation != null ? " - DEVIATION: " + deviation : "", government));
                }

                // France's window: no other start inside 2024-06-09 .. 2024-07-18
                var lo = new DateTime(2024, 6, 9); var hi = new DateTime(2024, 7, 18);
                foreach (KeyValuePair<CountryId, DateTime> kv in starts)
                {
                    if (kv.Key == CountryId.France) { continue; }
                    if (kv.Value >= lo && kv.Value <= hi) { failures++; sb.Append(F("    FAIL {0}: its start {1:yyyy-MM-dd} falls inside France's window without a chamber\n", kv.Key, kv.Value)); }
                }
                sb.Append("    France's window 2024-06-09..2024-07-18 holds no other country's start.\n");

                // the default epoch keeps K-1's standing
                SimulationManager.SetEpoch(SimulationManager.DefaultEpoch);
                World k1 = WorldFactory.CreateDefault();
                Country sweden = k1.GetCountry(CountryId.Sweden);
                if (WorldClock.SeatedVintage(CountryId.Sweden, SimulationManager.DefaultEpoch) != ElectionVintage.Sweden2026) { failures++; sb.Append("    FAIL: the default epoch does not seat Sweden's 2026 chamber\n"); }
                if (!SeatedGovernment.IsProvisional(sweden)) { failures++; sb.Append("    FAIL: at the default epoch Sweden's government is not PROVISIONAL (K-1 part 4)\n"); }
                sb.Append(F("    the default epoch {0:yyyy-MM-dd} seats Sweden's 2026 chamber with its government PROVISIONAL, as K-1 left it.\n", SimulationManager.DefaultEpoch));
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n");
            }
            finally
            {
                SimulationManager.SetEpoch(epochBefore);
            }

            if (failures > 0)
            {
                Debug.LogError($"WORLD CLOCK: {failures} failure(s).\n{sb}");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
