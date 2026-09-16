using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// CL-3 (2026-09-16, §523; the plan's S-C3): **THE STAGED SAVE AT THE PRE-CAMPAIGN'S FIRST DAY.** The play protocol opens on a save cut
    /// here: the seed world with Sweden as the player, advanced day by day from the epoch to the first day of the 26-week run-up
    /// (`CampaignCalendar.Sweden2026.PreCampaignStart`, 2026-01-18 - 17 days in), and written with the save service the game itself uses.
    /// The seed is stated on the save (`MasterSeed`, 777 - the harness's own, so a played run and a filmed one open on one world) and the
    /// protocol document names it. Nothing is drafted into it: a playtester opens a clean book.
    ///
    /// <para><see cref="PlayProtocolCheck"/> (the cheap bar) cuts the same save to a temporary path, loads it back into a second manager and
    /// asserts what the protocol relies on - the format is the current one, the date is the run-up's first day, the calendar reads
    /// PRE-CAMPAIGN on it, the player is Sweden - so the day a save from this tool would refuse to load, the bar says so before Elias does.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.PlayProtocolStaging.Run -logFile &lt;path&gt;`
    /// - writes `playtest_4_precampaign_day1.json` into the game's saves directory beside the three felt-verdict saves the film harness stages (`-shotsaves`).
    /// </summary>
    public static class PlayProtocolStaging
    {
        public const string PreCampaignSaveName = "playtest_4_precampaign_day1";
        public const int Seed = 777;
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        /// <summary>Cuts the pre-campaign save to <paramref name="path"/>, loads it back into a fresh manager and returns null, or the first assertion that failed.</summary>
        public static string CutAndVerify(string path, out SaveGame loaded)
        {
            loaded = null;
            CampaignCalendar calendar = CampaignCalendar.Sweden2026;
            DateTime target = calendar.PreCampaignStart;
            var goA = new GameObject("PlayProtocolStaging.A");
            var goB = new GameObject("PlayProtocolStaging.B");
            try
            {
                SimulationRandom.Seed(Seed);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = goA.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                int days = 0;
                while (sim.CurrentDate < target && days < 400) { sim.AdvanceDay(); days++; }
                if (sim.CurrentDate != target) { return F("the manager advanced {0} days and stands at {1:yyyy-MM-dd}, not the run-up's first day {2:yyyy-MM-dd}", days, sim.CurrentDate, target); }
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                SaveGameService.SaveToFile(path, save);

                loaded = SaveGameService.LoadFromFile(path);
                if (loaded.SaveVersion != SaveGameService.CurrentSaveVersion) { return F("the save carries format {0}, this build reads {1}", loaded.SaveVersion, SaveGameService.CurrentSaveVersion); }
                if (loaded.CurrentDate != target) { return F("the save's date is {0:yyyy-MM-dd}, not {1:yyyy-MM-dd}", loaded.CurrentDate, target); }
                if (loaded.PlayerCountryId != CountryId.Sweden) { return F("the save's player is {0}, not Sweden", loaded.PlayerCountryId); }
                if (loaded.MasterSeed != Seed) { return F("the save's master seed is {0}, not the stated {1}", loaded.MasterSeed, Seed); }
                SimulationManager simB = goB.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                if (simB.CurrentDate != target) { return F("restored, the manager reads {0:yyyy-MM-dd}", simB.CurrentDate); }
                if (calendar.PhaseOn(simB.CurrentDate) != CampaignPhase.PreCampaign) { return F("the calendar reads {0} on the restored date, not PreCampaign", calendar.PhaseOn(simB.CurrentDate)); }
                if (simB.PlayerCountryId != CountryId.Sweden) { return "restored, the player is not Sweden"; }
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(goA);
                UnityEngine.Object.DestroyImmediate(goB);
            }
        }

        private static string Digest(string path)
        {
            using (var sha = SHA256.Create()) using (FileStream s = File.OpenRead(path)) { return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant().Substring(0, 16); }
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string dir = SaveGameService.DefaultSaveDirectory;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, PreCampaignSaveName + ".json");
            string failure = CutAndVerify(path, out SaveGame loaded);
            if (failure != null) { Debug.LogError("PLAY PROTOCOL: the pre-campaign save was NOT staged clean - " + failure); CheckExit.Finish(1); return; }
            Debug.Log(F("PLAY PROTOCOL: staged {0} ({1} bytes, sha256 {2}…) - format {3}, seed {4}, Sweden, {5:yyyy-MM-dd} = the run-up's first day ({6} weeks before the campaign, {7} before polling day {8:yyyy-MM-dd}).",
                path, new FileInfo(path).Length, Digest(path), loaded.SaveVersion, loaded.MasterSeed, loaded.CurrentDate, CampaignCalendar.DefaultPreCampaignWeeks, CampaignCalendar.DefaultPreCampaignWeeks + CampaignCalendar.DefaultCampaignWeeks, CampaignCalendar.Sweden2026.ElectionDate));
            CheckExit.Finish(0);
        }
    }

}
