using System;
using System.Globalization;
using System.IO;
using PoliSim.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>CL-3 (2026-09-16, §523): the protocol's save can be cut and loads back onto the run-up's first day - the cut is
    /// <see cref="PlayProtocolStaging.CutAndVerify"/>'s, to a temporary path, deleted after. Cheap bar. (Its own file: the evidence check
    /// reads a registered check off the file named after it.)</summary>
    public static class PlayProtocolCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            string path = Path.Combine(Application.temporaryCachePath, "play_protocol_check_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                string failure = PlayProtocolStaging.CutAndVerify(path, out SaveGame loaded);
                if (failure != null) { Debug.LogError("PLAY PROTOCOL: " + failure); CheckExit.Finish(1); return; }
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "PLAY PROTOCOL: the pre-campaign save cuts and loads back - format {0}, seed {1}, Sweden, {2:yyyy-MM-dd}, PRE-CAMPAIGN on the calendar.", loaded.SaveVersion, loaded.MasterSeed, loaded.CurrentDate));
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
