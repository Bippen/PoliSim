using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>CL-3 (2026-09-16, §523): the protocol's save can be cut and loads back onto the run-up's first day - the cut is
    /// <see cref="PlayProtocolStaging.CutAndVerify"/>'s, to a temporary path, deleted after. Cheap bar. (Its own file: the evidence check
    /// reads a registered check off the file named after it.)
    /// <para><b>AND THE GAME CAN LOAD IT</b> (2026-09-21, §557). Until then the check loaded the save into a bare manager and stopped - and the CONTROLLER's load path, the one
    /// the player's Load button takes, threw on it from the day the save was first cut: a batch-written save carries no UI layer, and one line of `RestoreUiDrafts`
    /// (the bracket drafts, §490) read the layer without asking whether it was there. The game said *Load FAILED* and no bar did. So the check now adopts the save through
    /// `GameController.RestoreFromSave` itself - a controller in edit mode, its manager handed to it - and fails with the exception's own words.</para></summary>
    public static class PlayProtocolCheck
    {
        /// <summary>Loads the save afresh and hands it to a controller's own `RestoreFromSave` (the body of `LoadFromPath`, without its catch - so the exception is read, not
        /// folded into a status string). Edit mode: no Awake, no OnGUI; the controller's dictionaries are its field initialisers', its manager is handed to it.</summary>
        private static string AdoptThroughTheController(string path)
        {
            var go = new GameObject("PlayProtocolCheck.Controller");
            try
            {
                SaveGame save = SaveGameService.LoadFromFile(path);
                if (save.Ui != null) { return "the staged save carries a UI layer - the case this assertion exists for (a batch-written save with none) is not the one under test"; }
                SimulationManager sim = go.AddComponent<SimulationManager>();
                PoliSim.UI.GameController controller = go.AddComponent<PoliSim.UI.GameController>();
                const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo simField = typeof(PoliSim.UI.GameController).GetField("_simulationManager", Private);
                FieldInfo playerField = typeof(PoliSim.UI.GameController).GetField("_playerCountry", Private);
                FieldInfo speedField = typeof(PoliSim.UI.GameController).GetField("_gameSpeed", Private);
                MethodInfo restore = typeof(PoliSim.UI.GameController).GetMethod("RestoreFromSave", Private);
                if (simField == null || playerField == null || speedField == null || restore == null) { return "the controller's load path could not be reached by name (_simulationManager / _playerCountry / _gameSpeed / RestoreFromSave) - this assertion VERIFIED NOTHING"; }
                simField.SetValue(controller, sim);
                try { restore.Invoke(controller, new object[] { save }); }
                catch (TargetInvocationException e)
                {
                    Exception inner = e.InnerException ?? e;
                    string where = (inner.StackTrace ?? string.Empty).Split('\n')[0].Trim();
                    return $"the CONTROLLER cannot adopt the protocol's save - {inner.GetType().Name}: {inner.Message} ({where}). The player's Load reads 'Load FAILED' on it.";
                }
                var player = playerField.GetValue(controller) as PoliSim.Data.Country;
                if (player == null || player.Id != PoliSim.Data.CountryId.Sweden) { return $"adopted, the controller governs {(player == null ? "no country" : player.Id.ToString())}, not Sweden"; }
                if (!ReferenceEquals(sim.World, save.World)) { return "adopted, the manager and the save hold two worlds"; }
                if (speedField.GetValue(controller).ToString() != "Paused") { return $"adopted, the game is {speedField.GetValue(controller)}, not PAUSED - a load resumes paused"; }
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                EnergyMarket.ResetTurnState();   // the controller's restore begins the market's turn on the loaded world; the process's turn state is put back
            }
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string path = Path.Combine(Application.temporaryCachePath, "play_protocol_check_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                string failure = PlayProtocolStaging.CutAndVerify(path, out SaveGame loaded);
                if (failure != null) { Debug.LogError("PLAY PROTOCOL: " + failure); CheckExit.Finish(1); return; }
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "PLAY PROTOCOL: the pre-campaign save cuts and loads back - format {0}, seed {1}, Sweden, {2:yyyy-MM-dd}, PRE-CAMPAIGN on the calendar.", loaded.SaveVersion, loaded.MasterSeed, loaded.CurrentDate));
                string adoption = AdoptThroughTheController(path);
                if (adoption != null) { Debug.LogError("PLAY PROTOCOL: " + adoption); CheckExit.Finish(1); return; }
                Debug.Log("PLAY PROTOCOL: the CONTROLLER adopts it - GameController.RestoreFromSave on a save with no UI layer, the path the player's Load takes; the controller governs Sweden in the loaded world, PAUSED.");
                Debug.Log("=== PlayProtocolCheck: ALL ASSERTIONS PASS ===");
                CheckExit.Finish(0);
            }
            finally
            {
                try { if (File.Exists(path)) { File.Delete(path); } if (File.Exists(path + ".bak")) { File.Delete(path + ".bak"); } } catch (Exception) { }
            }
        }
    }
}
