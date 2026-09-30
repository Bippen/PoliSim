using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §668: **THE WINDOWS PLAYER BUILD** - the first this project has made. `-executeMethod PoliSim.EditorTools.PlayerBuild.BuildWindows
    /// -buildout=&lt;folder&gt;` builds the scenes the build settings enable into <c>&lt;folder&gt;/Incumbent.exe</c>, 64-bit Windows, with the
    /// scripting backend the project sets; companyName DWELOP Games and productName Incumbent (§693, the studio name ruled - the window title is productName's). The folder is outside the repository and is
    /// never committed. `Tools/build_player.ps1` runs it, then the smoke check (`PoliSim.Testing.PlayerSmoke`).
    /// </summary>
    public static class PlayerBuild
    {
        public static void BuildWindows()
        {
            string folder = null;
            foreach (string a in Environment.GetCommandLineArgs()) { if (a.StartsWith("-buildout=", StringComparison.Ordinal)) { folder = a.Substring(10); } }
            if (string.IsNullOrEmpty(folder)) { Debug.LogError("BUILD: -buildout=<folder> is required"); EditorApplication.Exit(2); return; }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (Path.GetFullPath(folder).StartsWith(root, StringComparison.OrdinalIgnoreCase)) { Debug.LogError("BUILD: the output folder is inside the repository - refused"); EditorApplication.Exit(2); return; }
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) { if (s.enabled) { scenes.Add(s.path); } }
            if (scenes.Count == 0) { Debug.LogError("BUILD: no scene is enabled in the build settings"); EditorApplication.Exit(2); return; }
            Directory.CreateDirectory(folder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(folder, "Incumbent.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            DisableUnityServices();
            Debug.Log($"BUILD: {scenes.Count} scene(s) ({string.Join(", ", scenes)}) -> {options.locationPathName}; product '{PlayerSettings.productName}', company '{PlayerSettings.companyName}'");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage m in step.messages)
                {
                    if (m.type == LogType.Error || m.type == LogType.Exception) { Debug.Log($"BUILD: ERROR in '{step.name}': {m.content}"); }
                }
            }
            Debug.Log($"BUILD: {summary.result} - {summary.totalErrors} error(s), {summary.totalWarnings} warning(s), {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0} s");
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>§678 (Elias's ruling: analytics OFF): the Unity services the Editor's API documents (`UnityEditor.Analytics.AnalyticsSettings`,
        /// Unity 6.6 scripting reference), turned off before every build and each value read back into the build log. The second layer: the
        /// built-in analytics module (`com.unity.modules.unityanalytics`) is out of the package manifest, so no analytics code ships at all.
        /// What the built player contacts is measured, not assumed: `Tools/player_net_watch.ps1`, run by `Tools/build_player.ps1` on every build.</summary>
        private static void DisableUnityServices()
        {
            UnityEditor.Analytics.AnalyticsSettings.enabled = false;
            UnityEditor.Analytics.AnalyticsSettings.initializeOnStartup = false;
            // deviceStatsEnabledInBuild is READ-ONLY in 6000.5.6f1 (CS0200) although the 6.6 page lists it read-write - the hardware statistics are not ours to set; read back below
            UnityEditor.Analytics.PerformanceReportingSettings.enabled = false;
            UnityEditor.CrashReporting.CrashReportingSettings.enabled = false;
            Debug.Log($"BUILD: Unity services - analytics {UnityEditor.Analytics.AnalyticsSettings.enabled}, initialise on startup {UnityEditor.Analytics.AnalyticsSettings.initializeOnStartup}, "
                      + $"device stats {UnityEditor.Analytics.AnalyticsSettings.deviceStatsEnabledInBuild}, performance reporting {UnityEditor.Analytics.PerformanceReportingSettings.enabled}, "
                      + $"crash reporting {UnityEditor.CrashReporting.CrashReportingSettings.enabled}");
        }
    }
}
