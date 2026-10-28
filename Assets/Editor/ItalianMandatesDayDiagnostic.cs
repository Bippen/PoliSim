using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-7 (K204, K206; the delta review's L4'): ITALY'S MANDATES ON THE DAY LOOP, planted. A world on Italy's start with the count of 25 Sep 2022 planted (the
    /// chambers the count seats on the record's own shares, the record's day):
    /// <list type="bullet">
    /// <item><description>(1) nothing changes before the first sitting, 13 Oct 2022; (2) on it the mandates run once - a government installed that day, or the
    /// caretaker discharged and an extra election ordered; (3) not again the next day; (4) not again after a save round-trip;</description></item>
    /// <item><description>(5) the next polling day re-anchors on the latest count (a count planted on 1 Oct 2022 - first sitting 21 Oct 2022 - gives 17 Oct 2027);</description></item>
    /// <item><description>(6) three failed mandates inside the semestre bianco (a world opened on 1 Sep 2028, the Camera one party's and the Senato another's) order the extra
    /// election from the day the bar lifts, 3 Feb 2029, by art. 61's seventy days.</description></item>
    /// </list>
    /// </summary>
    public static class ItalianMandatesDayDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ItalianMandatesDayDiagnostic (PS-7, K204/K206): Italy's mandates on the day loop ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    (SimulationManager sim, Country italy) = Open(hosts, null);
                    italy.PlayerPartyAbbrev = "PD";
                    var shares = RecordShares(out double rho);
                    var poll = new DateTime(2022, 9, 25);
                    Plant(italy, shares, rho, poll);
                    GovernmentRecord eve = italy.Government;
                    var sitting = WorldClock.ItalyFirstSittingAfter(poll);
                    bool untouched = true;
                    while (sim.CurrentDate < sitting) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Italy); if (sim.CurrentDate < sitting && !ReferenceEquals(italy.Government, eve)) { untouched = false; } }
                    Check(untouched && sim.CurrentDate == sitting, F("(1) nothing changes before the first sitting - {0} governs to {1:yyyy-MM-dd}", Who(eve), sitting));
                    GovernmentRecord onSitting = italy.Government;
                    bool installed = !ReferenceEquals(onSitting, eve) && onSitting.FormedOn == sitting;
                    bool dissolved = ReferenceEquals(onSitting, eve) && eve.Caretaker && eve.CaretakerSince == sitting && sim.ExtraElectionDate != DateTime.MinValue;
                    Check(installed || dissolved, F("(2) on {0:yyyy-MM-dd} the mandates ran - {1}", sitting, installed ? "installed " + Who(onSitting) : dissolved ? F("none granted, the extra election on {0:yyyy-MM-dd}", sim.ExtraElectionDate) : "NOTHING"));
                    int version = onSitting.Version, breaks = onSitting.Breaks.Count;
                    sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Italy);
                    Check(ReferenceEquals(italy.Government, onSitting) && onSitting.Version == version && onSitting.Breaks.Count == breaks, "(3) the next day the mandates do not run again");

                    Persistence.SaveGame back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(sim, sim.World, CountryId.Italy, null)));
                    var host2 = new GameObject("ItalianMandatesDayDiagnostic.loaded"); hosts.Add(host2);
                    SimulationManager loaded = host2.AddComponent<SimulationManager>();
                    Persistence.SaveGameService.RestoreInto(loaded, back);
                    Country italyLoaded = loaded.World.GetCountry(CountryId.Italy);
                    GovernmentRecord gLoaded = italyLoaded.Government;
                    int loadedBreaks = gLoaded.Breaks.Count;
                    DateTime loadedFormed = gLoaded.FormedOn;
                    loaded.AdvanceDay(); loaded.AdvanceCountryDayTick(CountryId.Italy);
                    Check(ReferenceEquals(italyLoaded.Government, gLoaded) && gLoaded.Breaks.Count == loadedBreaks && gLoaded.FormedOn == loadedFormed,
                        F("(4) after a save round-trip the mandates do not run again - {0}", Who(gLoaded)));

                    italyLoaded.ElectionHistory.Add(new ElectionRecord { Date = new DateTime(2022, 10, 1), CountryId = CountryId.Italy.ToString(), Method = ElectionMethod.ItalyRosatellum });
                    bool next = loaded.TryPlayerPollingDay(out DateTime nextDay);
                    Check(next && nextDay == new DateTime(2027, 10, 17), F("(5) a count planted on 1 Oct 2022 (first sitting 21 Oct 2022) re-anchors the next ordinary polling day on its own term: {0:yyyy-MM-dd}", nextDay));
                }

                using (SimulationManager.EpochScope())
                {
                    (SimulationManager sim, Country italy) = Open(hosts, new DateTime(2028, 9, 1));
                    italy.PlayerPartyAbbrev = "PD";
                    DateTime count = new DateTime(2028, 8, 15);
                    var hung = new Dictionary<string, int> { { "FdI", 400 } };   // a hung pair: one party holds the Camera whole, another the Senato - no government holds both
                    ParliamentSystem.SetSeatsFromElection(italy, hung);
                    ParliamentSystem.SetSecondChamberFromElection(italy, new Dictionary<string, int> { { "PD", 206 } });
                    italy.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Italy.ToString(), Method = ElectionMethod.ItalyRosatellum, Seats = hung });
                    MethodInfo install = typeof(SimulationManager).GetMethod("InstallByMandates", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool granted = (bool)install.Invoke(sim, new object[] { italy, DeclarationReading.OfElection(CountryId.Italy, count) });
                    DateTime expected = ConfidenceProcedure.ItalianElectionDay(new DateTime(2029, 2, 3));
                    Check(!granted && italy.Government.Caretaker && sim.ExtraElectionDate == expected && expected > new DateTime(2029, 2, 3),
                        F("(6) three mandates failing on {0:yyyy-MM-dd}, in the President's last six months (art. 88 c. 2): the caretaker governs on, and the extra election falls on {1:yyyy-MM-dd} - art. 61's day from the bar lifting on 3 Feb 2029 ({2:yyyy-MM-dd})",
                            sim.CurrentDate, sim.ExtraElectionDate, expected));
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.GetType().Name).Append(": ").Append(e.InnerException?.Message ?? e.Message).Append('\n'); }
            finally
            {
                foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); }
                EnergyMarket.ResetTurnState();
            }

            sb.Append(failures == 0 ? "=== ItalianMandatesDayDiagnostic: the mandates run once, on their day ===" : F("=== ItalianMandatesDayDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>A world as a new game builds it on Italy's start (or on <paramref name="epoch"/>), Italy the player's country.</summary>
        private static (SimulationManager, Country) Open(List<GameObject> hosts, DateTime? epoch)
        {
            if (epoch.HasValue) { SimulationManager.SetEpoch(epoch.Value); } else { WorldClock.ApplyStart(CountryId.Italy); }
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ItalianMandatesDayDiagnostic"); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Italy;
            return (sim, world.GetCountry(CountryId.Italy));
        }

        /// <summary>The record's 2022 Camera shares over the roster (summing to one) and the roster's share of the valid vote.</summary>
        private static Dictionary<string, double> RecordShares(out double rho)
        {
            var shares = new Dictionary<string, double>();
            double sum = 0.0;
            foreach (long v in Elections.Generated.ItalianCount2022.CameraPublished2022Votes) { sum += v; }
            for (int k = 0; k < Elections.Generated.ItalianCount2022.Keys.Length; k++) { shares[Elections.Generated.ItalianCount2022.Keys[k]] = Elections.Generated.ItalianCount2022.CameraPublished2022Votes[k] / sum; }
            rho = sum / Elections.Generated.ItalianCount2022.CameraPublished2022Valid;
            return shares;
        }

        /// <summary>The count of <paramref name="poll"/> planted as the player's election seats it: the Camera, the Senato with its senators for life, the record.</summary>
        private static void Plant(Country italy, Dictionary<string, double> shares, double rho, DateTime poll)
        {
            ItalianCount.Result camera = ItalianCount.Camera(shares, rho);
            ParliamentSystem.SetSeatsFromElection(italy, camera.Seats);
            ParliamentSystem.SetSecondChamberFromElection(italy, ItalianCount.SenatoSeated(shares, WorldClock.ItalyFirstSittingAfter(poll), ItalianCount.SenatoRosterShare2022));
            var record = new ElectionRecord { Date = poll, CountryId = CountryId.Italy.ToString(), Method = ElectionMethod.ItalyRosatellum };
            foreach (KeyValuePair<string, int> kv in camera.Seats) { record.Seats[kv.Key] = kv.Value; }
            foreach (KeyValuePair<string, double> kv in shares) { record.Shares[kv.Key] = kv.Value; }
            italy.ElectionHistory.Add(record);
        }

        private static string Who(GovernmentRecord g) => g == null ? "no government" : string.Join("+", g.Cabinet) + " led by " + (g.PmParty ?? "-") + (g.Caretaker ? " (a caretaker)" : string.Empty);

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
