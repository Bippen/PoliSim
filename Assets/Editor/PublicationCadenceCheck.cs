using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Measures, per published stat, HOW LONG A PLAYER (or a capture) MUST WAIT to see a preliminary
    /// release - and how long that state then lasts before a revision replaces it.
    ///
    /// **This exists because "capturable" turned out to be a real constraint rather than a detail.**
    /// Behaviour 6's provisional state could not be captured at all until the screenshot driver was
    /// taught to advance until `AnyPreliminary` reported one; a fixed warm-up had left every series
    /// reading FINAL, because anything old enough to plot is also old enough to have been revised. That
    /// worked for GDP, which is quarterly and landed at day 1125. The open question is whether an
    /// ANNUAL-cadence series (Population, PovertyRate, CrimeIndex) ever reaches a preliminary state
    /// inside a reachable horizon - because if it needs four thousand days, "wait for it" stops being a
    /// strategy and the driver has to force a publication state instead.
    ///
    /// Runs headless: `PublicationSystem.PublishDueFigures` is static and takes a Country and a date, so
    /// this needs no play mode and no graphics device, unlike the capture driver.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.PublicationCadenceCheck.Run -logFile &lt;path&gt;`
    /// </summary>
    public static class PublicationCadenceCheck
    {
        private const int HorizonDays = 365 * 12;

        /// <summary>The capture driver's own five-year warm-up ceiling — the horizon inside which
        /// "advance until something is preliminary" is a strategy rather than a spin.</summary>
        private const int DriverCeilingDays = 1825;

        /// <summary>⚠ A RATCHET, measured 2026-09-01, not authored: exactly ONE stat (GDP, first
        /// preliminary at day 119) reaches a preliminary state inside the driver's ceiling. Every other
        /// series is FINAL on first release and has no revision stage at all. **Raise it when another
        /// series gains one; a fall below it is the driver's warm-up strategy breaking.**</summary>
        private const int ReachablePreliminaryRatchet = 1;

        public static void Run()
        {
            World world = WorldFactory.CreateDefault();
            Country country = world.GetCountry(CountryId.USA);
            PublicationSystem.SeedInheritedHistory(country);

            var firstPreliminary = new Dictionary<PublishedStat, int>();
            var preliminaryDays = new Dictionary<PublishedStat, int>();
            var releaseCount = new Dictionary<PublishedStat, int>();
            var everPublished = new Dictionary<PublishedStat, int>();

            System.DateTime date = SimulationManager.EpochDate;
            for (int day = 1; day <= HorizonDays; day++)
            {
                date = date.AddDays(1);
                PublicationSystem.PublishDueFigures(country, date);

                foreach (KeyValuePair<PublishedStat, PublishedSeries> kv in country.Published.Series)
                {
                    PublishedEntry latest = kv.Value.Latest();
                    if (latest == null)
                    {
                        continue;
                    }

                    if (!everPublished.ContainsKey(kv.Key))
                    {
                        everPublished[kv.Key] = day;
                    }

                    releaseCount[kv.Key] = kv.Value.Entries.Count;

                    if (latest.Status != RevisionStatus.Preliminary)
                    {
                        continue;
                    }

                    if (!firstPreliminary.ContainsKey(kv.Key))
                    {
                        firstPreliminary[kv.Key] = day;
                    }

                    preliminaryDays.TryGetValue(kv.Key, out int held);
                    preliminaryDays[kv.Key] = held + 1;
                }
            }

            Debug.Log($"=== PUBLICATION CADENCE over {HorizonDays} days ({HorizonDays / 365} years), USA ===");
            Debug.Log($"{"stat",-16}{"1st release",14}{"1st PRELIM",13}{"days PRELIM",13}{"releases",10}   verdict");

            // ⚠ THE ASSERTIONS THIS CHECK DID NOT HAVE (the sixth coherence sweep, 2026-09-01).
            // It is registered in the simulation group as a CHECK and its only exit was `Finish(0)`: it
            // reported clean BY CONSTRUCTION, and "8 of 8 simulation checks clean" counted it every run.
            // Its own doc already named the question - whether a series ever reaches a preliminary state
            // inside a reachable horizon, "because if it needs four thousand days, 'wait for it' stops
            // being a strategy and the driver has to force a publication state instead". The assertions
            // below are ITS OWN, promoted from prose to code. Nothing here is a threshold invented to
            // give the check something to fail against: the ratchet is the measurement.
            int examined = 0, reachablePreliminary = 0;
            var incoherent = new List<string>();

            foreach (PublishedStat stat in System.Enum.GetValues(typeof(PublishedStat)))
            {
                everPublished.TryGetValue(stat, out int firstRelease);
                firstPreliminary.TryGetValue(stat, out int firstPrelim);
                preliminaryDays.TryGetValue(stat, out int prelimDays);
                releaseCount.TryGetValue(stat, out int releases);

                examined++;
                if (firstPrelim > 0 && firstPrelim <= DriverCeilingDays) { reachablePreliminary++; }

                // A stat with releases but no first release - or a first release with no releases - is a
                // bookkeeping contradiction inside this measurement, and it makes every number beside it
                // unreadable.
                if ((releases > 0) != (firstRelease > 0))
                {
                    incoherent.Add(stat + " (first release " + firstRelease + ", releases " + releases + ")");
                }

                string verdict = firstPrelim == 0
                    ? "NEVER PRELIMINARY - no revision stage, so B6 channel 2 has one state only"
                    : firstPrelim <= 1825
                        ? "reachable by the capture driver's 5-year ceiling"
                        : "BEYOND the driver's ceiling - would need forcing rather than waiting";

                Debug.Log($"{stat,-16}{(firstRelease == 0 ? "never" : firstRelease.ToString()),14}" +
                          $"{(firstPrelim == 0 ? "never" : firstPrelim.ToString()),13}{prelimDays,13}{releases,10}   {verdict}");
            }

            int failures = 0;

            // The enumeration rule: a run that examined no stat has measured nothing, and its clean
            // summary would read exactly like a run where everything was fine.
            if (examined == 0)
            {
                failures++;
                Debug.LogError("CADENCE: not one PublishedStat was examined, so this run verified NOTHING about the "
                               + "publication cadence rather than finding nothing wrong with it.");
            }

            // ⚠ THE ONE THE DOC ASKED FOR. The capture driver reaches a preliminary state by ADVANCING
            // until `AnyPreliminary` reports one. If no stat ever reaches PRELIMINARY inside the driver's
            // ceiling, that strategy silently becomes a spin to the ceiling that films the wrong state -
            // which is S-20's family exactly. Measured 2026-09-01: exactly ONE stat qualifies (GDP, at
            // day 119), so the ratchet is 1. Raise it when another series gains a revision stage; a fall
            // below it is the strategy breaking, and it fails here rather than in a film nobody re-reads.
            // ⚠ A FLOOR ratchet: the measurement must not fall BELOW the bound, because the driver's
            // warm-up breaks when this goes DOWN. The direction is CARRIED rather than implied - reporting
            // a floor as if it were a ceiling would make "tight" mean the opposite of what it says.
            RatchetLedger.Report("PublicationCadenceCheck.REACHABLE_PRELIMINARY", reachablePreliminary, ReachablePreliminaryRatchet, isFloor: true);
            if (reachablePreliminary < ReachablePreliminaryRatchet)
            {
                failures++;
                Debug.LogError($"CADENCE: {reachablePreliminary} stat(s) reach PRELIMINARY within the driver's "
                               + $"{DriverCeilingDays}-day ceiling, below the recorded {ReachablePreliminaryRatchet}. "
                               + "The capture driver waits for `AnyPreliminary`; with none reachable that wait becomes a "
                               + "spin to the ceiling that films the wrong publication state and reports success.");
            }

            if (incoherent.Count > 0)
            {
                failures++;
                Debug.LogError("CADENCE: " + incoherent.Count + " stat(s) report a release count and a first-release day "
                               + "that contradict each other - " + string.Join("; ", incoherent.ToArray()));
            }

            Debug.Log($"CADENCE: {examined} stat(s) examined, {reachablePreliminary} reaching PRELIMINARY inside the "
                      + $"{DriverCeilingDays}-day driver ceiling (ratchet {ReachablePreliminaryRatchet}), "
                      + $"{incoherent.Count} incoherent.");

            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
