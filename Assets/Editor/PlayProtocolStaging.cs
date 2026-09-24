using System;
using System.Collections.Generic;
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
    /// (the game's own run-up - since PS-2 / CL-4 (§619) the run-up to Sweden's REAL polling day, `WorldClock.TryNextPollingDay` from the epoch, so the
    /// save's day is the epoch itself, 18 January 2026, and the turn is 0; between CL-5 (§579) and §619 it was the run-up to turn 4's boundary), and written with the save service the game itself uses.
    /// The seed is stated on the save (`MasterSeed`, 777 - the harness's own, so a played run and a filmed one open on one world) and the
    /// protocol document names it. Nothing is drafted into it: a playtester opens a clean book. **The player is seated as the largest party of Sweden's seeded chamber**
    /// (§558 - the fresh game's own fallback and the film harness's seat; until 2026-09-21 the staging seated none, and a player with no party has no run-up).
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
            // CL-5 RULED (2026-09-22, §579): THE GAME IS RIGHT AND THE PROTOCOL WAS WRONG. Until this day the save was cut on CampaignCalendar.Sweden2026 - the REAL
            // Swedish election's dates - while the live day path reads the NEXT ELECTION TURN'S BOUNDARY, so the save opened years before any run-up began and the
            // protocol's first step described a game that was not running (§558). The target is now the game's own run-up, computed from the same expression the day
            // path uses, and the protocol takes its dates FROM THIS SAVE rather than from the calendar a person typed.
            using System.IDisposable epoch = SimulationManager.EpochScope();   // PS-1 (§618): the save opens on Sweden's own start, as the game does at selection; the epoch is put back after
            PoliSim.Elections.WorldClock.ApplyStart(CountryId.Sweden);
            // PS-2 / CL-4 (§619): the game's election is the country's own polling day; the save is cut at that election's run-up, which for Sweden is its start.
            if (!PoliSim.Elections.WorldClock.TryNextPollingDay(CountryId.Sweden, SimulationManager.EpochDate, out DateTime pollingDay)) { return "Sweden has no polling day on its calendar - nothing to stage a run-up for"; }
            var calendar = new CampaignCalendar(pollingDay);
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
                // §558: the player's party, seated on day 0 as the game seats it at selection - THE LARGEST PARTY OF THE COUNTRY'S OWN SEEDED CHAMBER, the rule
                // `GameController.SelectPlayerCountry` falls back on and the seat every film takes, so a played run, a filmed one and this save open as one party in one
                // world. Without it the run-up never begins (the day path asks for the player's party index and returns on none).
                Country player = world.GetCountry(CountryId.Sweden);
                PoliticalParty largest = default;
                int largestSeats = -1;   // PS-1 (§618): the largest of the chamber the world SEATS, as SelectPlayerCountry picks it
                foreach (PoliticalParty party in PartySystems.For(CountryId.Sweden)) { int held = player.ParliamentSeats.TryGetValue(party.Abbrev, out int n) ? n : 0; if (largest.Abbrev == null || held > largestSeats) { largest = party; largestSeats = held; } }
                if (player == null || largest.Abbrev == null) { return "Sweden's seeded chamber could not be read - no party to seat"; }
                player.PlayerPartyAbbrev = largest.Abbrev;
                player.PartyApprovalRating = player.State.ApprovalRating;
                int days = 0;
                // K-1 part (5) (2026-09-24, §606): THE TURNS RUN. The game crosses a boundary by calling AdvanceTurn when AdvanceDay reports one
                // (GameController.Update); this staging advanced days alone, so the save opened years in at TURN 0 with no turn ever run - its next
                // boundary would have been turn 1, not the election turn, and polling day would have passed with no election held. Each boundary now
                // runs the simulation's turn with every country's decision None, as the no-policy dump does: the player's clean book is no change,
                // and the AI ministries decide their own. The player's DAY tick (budget windows, foreign-policy rolls, bill countdowns) is not
                // played: it opens pauses only a player answers, and the staged book is clean.
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country country in world.Countries) { none[country.Id] = PolicyDecision.None(); }
                // §579: the game's run-up is four turns out, not seventeen days - the cap is the boundary's own distance plus a year's slack, and a day that does not move is the guard.
                while (sim.CurrentDate < target && days < 2000) { if (sim.AdvanceDay()) { sim.AdvanceTurn(none); } days++; }
                if (sim.CurrentDate != target) { return F("the manager advanced {0} days and stands at {1:yyyy-MM-dd}, not the run-up's first day {2:yyyy-MM-dd}", days, sim.CurrentDate, target); }
                int boundaries = 0;
                for (int k = 1; SimulationManager.TurnBoundary(k) <= target; k++) { boundaries++; }
                if (sim.CurrentTurn != boundaries) { return F("the manager stands at turn {0} on {1:yyyy-MM-dd}, where {2} boundaries were crossed - the turns did not run", sim.CurrentTurn, sim.CurrentDate, boundaries); }
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
                if (loaded.CurrentTurn != sim.CurrentTurn || simB.CurrentTurn != sim.CurrentTurn) { return F("the save carries turn {0} and restores at turn {1}, where the staging stood at {2}", loaded.CurrentTurn, simB.CurrentTurn, sim.CurrentTurn); }
                if (calendar.PhaseOn(simB.CurrentDate) != CampaignPhase.PreCampaign) { return F("the GAME's calendar reads {0} on the restored date, not PreCampaign", calendar.PhaseOn(simB.CurrentDate)); }
                if (simB.PlayerCountryId != CountryId.Sweden) { return "restored, the player is not Sweden"; }
                // §558 (2026-09-21): THE PROTOCOL'S FIRST STEP, HELD. *"Load it. The Desk opens on 18 January 2026; the rail's CAMPAIGN cell reads the run-up."* A player with no
                // party has no run-up and is refused every campaign verb (`AdvancePreCampaign`: no party index, no run) - and until this day the staging seated none, so the
                // save the protocol opens on could not be played as the protocol says.
                if (simB.PlayerPartyIndexForCampaign() < 0) { return "restored, the player has NO PARTY in the campaign - no run-up begins and every campaign verb is refused; the protocol's first step cannot be taken"; }
                // CL-5, CLOSED (2026-09-22, §579), and PS-2 / CL-4 (§619): the save is cut on the GAME's calendar, which since §619 IS the real one - the live day path
                // reads the player's country's next polling day. ⚠ The guard is that the two agree AFTER the restore - if the restored manager pointed at a different
                // election, the save would open on a run-up the game is not about to run.
                if (!simB.TryPlayerPollingDay(out DateTime livePollingDay)) { return "restored, the manager offers no polling day for Sweden"; }
                var live = new CampaignCalendar(livePollingDay);
                if (live.ElectionDate != calendar.ElectionDate)
                {
                    return F("the save was cut for the election of {0:yyyy-MM-dd} and the restored manager points at {1:yyyy-MM-dd} - the save opens on a run-up the game is not about to run", calendar.ElectionDate, live.ElectionDate);
                }
                if (live.ElectionDate != CampaignCalendar.Sweden2026.ElectionDate) { return F("the game's polling day is {0:yyyy-MM-dd}, not the real election's {1:yyyy-MM-dd}", live.ElectionDate, CampaignCalendar.Sweden2026.ElectionDate); }
                Debug.Log(F("PLAY PROTOCOL: the game's own calendar, which this save opens in - run-up {0:yyyy-MM-dd}, campaign {1:yyyy-MM-dd}, polling day {2:yyyy-MM-dd}; the run-up has {3} on the save's day. THE PROTOCOL TAKES ITS DATES FROM HERE (§579, CL-5; §619: they are the real election's since CL-4 landed).",
                    calendar.PreCampaignStart, calendar.CampaignStart, calendar.ElectionDate, simB.PlayerPreCampaign != null ? "BEGUN" : "not begun"));
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
            Debug.Log(F("PLAY PROTOCOL: staged {0} ({1} bytes, sha256 {2}…) - format {3}, seed {4}, turn {9}, Sweden, {5:yyyy-MM-dd} = the run-up's first day ({6} weeks before the campaign, {7} before polling day {8:yyyy-MM-dd}).",
                path, new FileInfo(path).Length, Digest(path), loaded.SaveVersion, loaded.MasterSeed, loaded.CurrentDate, CampaignCalendar.DefaultPreCampaignWeeks, CampaignCalendar.DefaultPreCampaignWeeks + CampaignCalendar.DefaultCampaignWeeks, loaded.CurrentDate.AddDays(7 * (CampaignCalendar.DefaultPreCampaignWeeks + CampaignCalendar.DefaultCampaignWeeks)), loaded.CurrentTurn));
            CheckExit.Finish(0);
        }
    }

}
