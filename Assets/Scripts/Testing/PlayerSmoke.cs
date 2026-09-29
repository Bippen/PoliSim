using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// §668: **THE PLAYER BUILD'S SMOKE CHECK.** Only with <c>-smoke=&lt;save name&gt;</c> on the player's command line (`Tools/build_player.ps1`
    /// passes it; a player launched by hand never sees this): the named save is loaded through the game's own load path
    /// (`GameController.LoadFromPath`, the one F9 and the menu's Load share), 30 game days run through the real day path (`AdvanceDay`, the
    /// turn on a boundary, the player's day tick - the harness's own), and the player quits 0; any failure logs `SMOKE: FAILED` and quits 1.
    /// </summary>
    public sealed class PlayerSmoke : MonoBehaviour
    {
        private const int Days = 30;
        private string _save;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arm()
        {
            string save = null;
            foreach (string a in Environment.GetCommandLineArgs()) { if (a.StartsWith("-smoke=", StringComparison.Ordinal)) { save = a.Substring(7); } }
            if (string.IsNullOrEmpty(save)) { return; }
            var go = new GameObject("PlayerSmoke");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerSmoke>()._save = save;
        }

        private void Start() { StartCoroutine(Run()); }

        private static void Fail(string why) { Debug.LogError("SMOKE: FAILED - " + why); Application.Quit(1); }

        private IEnumerator Run()
        {
            Debug.Log($"SMOKE: armed - {Application.productName} {Application.version}, {Application.platform}, save '{_save}'");
            for (int i = 0; i < 10; i++) { yield return null; }   // the scene's controller starts first
            var controller = FindFirstObjectByType<PoliSim.UI.GameController>();
            if (controller == null) { Fail("the scene carries no GameController"); yield break; }
            string path = Path.Combine(PoliSim.Persistence.SaveGameService.DefaultSaveDirectory, _save + ".json");
            if (!File.Exists(path)) { Fail($"no such save at {path}"); yield break; }
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo load = typeof(PoliSim.UI.GameController).GetMethod("LoadFromPath", Private);
            FieldInfo simField = typeof(PoliSim.UI.GameController).GetField("_simulationManager", Private);
            FieldInfo statusField = typeof(PoliSim.UI.GameController).GetField("_savesMenuStatus", Private);
            if (load == null || simField == null || statusField == null) { Fail("the controller's load path could not be reached by name"); yield break; }
            try { load.Invoke(controller, new object[] { path }); }
            catch (Exception e) { Fail("the load threw: " + (e.InnerException ?? e).Message); yield break; }
            string status = statusField.GetValue(controller) as string;
            var sim = simField.GetValue(controller) as SimulationManager;
            if (!string.IsNullOrEmpty(status) || sim == null || !sim.PlayerCountryId.HasValue) { Fail("the game did not take the save: " + (status ?? "no manager")); yield break; }
            CountryId player = sim.PlayerCountryId.Value;
            DateTime from = sim.CurrentDate;
            Debug.Log($"SMOKE: loaded {path} - {from:yyyy-MM-dd}, turn {sim.CurrentTurn}, player {player}");
            var none = new Dictionary<CountryId, PolicyDecision>();
            for (int d = 0; d < Days; d++)
            {
                try
                {
                    if (sim.AdvanceDay()) { sim.AdvanceTurn(none); }
                    sim.AdvanceCountryDayTick(player);
                }
                catch (Exception e) { Fail($"day {d + 1} threw: {e.GetType().Name}: {e.Message}"); yield break; }
                yield return null;
            }
            int days = (sim.CurrentDate - from).Days;
            if (days != Days) { Fail($"{Days} days asked, the calendar moved {days}"); yield break; }
            Debug.Log($"SMOKE: PASSED - {Days} game days run, {from:yyyy-MM-dd} to {sim.CurrentDate:yyyy-MM-dd}, turn {sim.CurrentTurn}; exiting 0");
            Application.Quit(0);
        }
    }
}
