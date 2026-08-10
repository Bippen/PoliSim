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

            foreach (PublishedStat stat in System.Enum.GetValues(typeof(PublishedStat)))
            {
                everPublished.TryGetValue(stat, out int firstRelease);
                firstPreliminary.TryGetValue(stat, out int firstPrelim);
                preliminaryDays.TryGetValue(stat, out int prelimDays);
                releaseCount.TryGetValue(stat, out int releases);

                string verdict = firstPrelim == 0
                    ? "NEVER PRELIMINARY - no revision stage, so B6 channel 2 has one state only"
                    : firstPrelim <= 1825
                        ? "reachable by the capture driver's 5-year ceiling"
                        : "BEYOND the driver's ceiling - would need forcing rather than waiting";

                Debug.Log($"{stat,-16}{(firstRelease == 0 ? "never" : firstRelease.ToString()),14}" +
                          $"{(firstPrelim == 0 ? "never" : firstPrelim.ToString()),13}{prelimDays,13}{releases,10}   {verdict}");
            }

            EditorApplication.Exit(0);
        }
    }
}
