using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE GAMEPLAY AUDIT'S HARNESS (`docs/audit/AUDIT_PROTOCOL.md`, Phase 1; ruled by Elias 2026-10-13, `COMPLETED.md` §803).
    ///
    /// <para><b>The route, and why it is this one.</b> The protocol asks for runs "through BatchSimulationRunner, using the same entry
    /// points the UI calls (no private shortcuts)". `BatchSimulationRunner.RunAudit` is the entry; it hands the plan to this class, which
    /// plays each run in a play-mode session of its own (a fresh domain each time, so the epoch, the random streams, the created-party
    /// registry and every other process-wide static start clean - the front-door inventory's finding D-11) beside the scene's REAL
    /// `GameController`. The controller's own `Update` advances the days; the audit's tick runs once a frame AFTER it (a player-loop
    /// system appended to the Update phase), reads what the day loop did, answers what holds the clock, acts for the play style, and
    /// then delivers the controller's `OnGUI` through `DryGuiPass` - the dry film's pump - so the Canvas seam (the main menu, the
    /// selector, every signing and election night) ticks exactly as it does under a Game View.</para>
    ///
    /// <para><b>What "the same entry point" means here.</b> A Canvas control is clicked: its `Button.onClick` is invoked, which IS the
    /// click (the menu's NEW GAME, the country folder, the start card and SELECT, the party row, SIGN, election night's CONTINUE). An
    /// IMGUI control cannot be clicked without a pointer, so its BODY is run - the very manager verb with the very builder the button
    /// passes, private members reached by reflection and named in the log; where a body writes the model inline (the central bank's
    /// chair), the same lines run. Nothing calls a route the UI does not take: no `HoldElectionWithoutTheNight`, no manager-only day
    /// loop, no model written that a control does not write.</para>
    ///
    /// <para><b>Time.</b> The controller paces days by `Time.deltaTime`; the harness sets `Time.captureDeltaTime`, so every frame
    /// carries the same delta and the days a frame advances are a function of the run, not of the machine. At 3x (0.25 s a day) and
    /// the default delta of 0.25 s a frame is one day; a smaller delta makes several frames a day, which is how the protocol's 1x
    /// against 3x comparison exercises the per-frame code.</para>
    ///
    /// <para><b>Launch</b> (one Unity job at a time, through the launcher):
    /// `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/unity_run.ps1 -Method PoliSim.EditorTools.BatchSimulationRunner.RunAudit
    /// -Label &lt;label&gt; -Extra "-skipsimulationtestrunner -auditplan=&lt;plan file&gt; -auditlabel=&lt;label&gt;"`. `-batchmode` and
    /// a graphics device are required (the Canvas screens do not build under `-nographics`), and `-skipsimulationtestrunner` is required
    /// (without it the scene's validation runner plays a whole run on the shared random streams before the player's game exists - D-02).
    /// A plan file holds one run per line (`key=value` pairs, `#` comments); `AuditSpec` names the keys. Each run writes
    /// `&lt;id&gt;.traj.csv` and `&lt;id&gt;.events.log` under `PoliSim-captures/logs/audit/&lt;label&gt;/`, and the plan ends with
    /// `AUDIT: done - ... exiting N.`</para>
    /// </summary>
    public static class AuditHarness
    {
        private const string PlanKey = "PoliSim.Audit.Plan";
        private const string IndexKey = "PoliSim.Audit.Index";
        private const string ActiveKey = "PoliSim.Audit.Active";
        private const string ResultsKey = "PoliSim.Audit.Results";
        private const string WorstKey = "PoliSim.Audit.Worst";
        private const string LabelKey = "PoliSim.Audit.Label";
        private const string DirKey = "PoliSim.Audit.Dir";
        private const string PlanStartKey = "PoliSim.Audit.PlanStart";
        private const string RunStartKey = "PoliSim.Audit.RunStart";
        private const string DigestsKey = "PoliSim.Audit.Digests";

        /// <summary>Out of tree, beside the films' logs: run files never enter git.</summary>
        public const string DefaultOutputRoot = "../PoliSim-captures/logs/audit";

        /// <summary>The entry `BatchSimulationRunner.RunAudit` calls.</summary>
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!Application.isBatchMode)
            {
                Debug.LogError("AUDIT: REFUSED outside -batchmode - a Game View would deliver OnGUI as well as the harness, and every frame would be laid out twice.");
                EditorApplication.Exit(2);
                return;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogError("AUDIT: REFUSED under -nographics - the Canvas screens (the menu, the selector, signing, election night) do not build without a graphics device, and the audit plays through them.");
                EditorApplication.Exit(2);
                return;
            }

            if (!args.Contains("-skipsimulationtestrunner"))
            {
                Debug.LogError("AUDIT: REFUSED without -skipsimulationtestrunner - the scene's SimulationTestRunner would play its own validation run on the shared random streams before the player's game exists, so no seed would mean what it says.");
                EditorApplication.Exit(2);
                return;
            }

            string planPath = Arg(args, "-auditplan=", string.Empty);
            string label = Arg(args, "-auditlabel=", "audit");
            if (planPath.Length == 0 || !File.Exists(planPath))
            {
                Debug.LogError($"AUDIT: REFUSED - no plan file ('-auditplan={planPath}'): one run per line, key=value pairs.");
                EditorApplication.Exit(2);
                return;
            }

            var lines = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int lineNo = 0;
            foreach (string raw in File.ReadAllLines(planPath))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                AuditSpec spec = AuditSpec.Parse(line, out string error);
                if (spec == null)
                {
                    Debug.LogError($"AUDIT: REFUSED - plan line {lineNo}: {error}");
                    EditorApplication.Exit(2);
                    return;
                }

                if (!ids.Add(spec.Id))
                {
                    Debug.LogError($"AUDIT: REFUSED - plan line {lineNo}: the id '{spec.Id}' is used twice.");
                    EditorApplication.Exit(2);
                    return;
                }

                lines.Add(line);
            }

            if (lines.Count == 0)
            {
                Debug.LogError("AUDIT: REFUSED - the plan holds no run.");
                EditorApplication.Exit(2);
                return;
            }

            // The player's own saves are never written over: a plan that autosaves, or resumes from that folder, runs only once the folder has
            // been moved aside (empty or absent) - and is put back afterwards, digest by digest (Tools/audit/real_saves.ps1).
            if (lines.Select(l => AuditSpec.Parse(l, out _)).Any(s => s != null && s.UsesRealSaves))
            {
                string real = PoliSim.Persistence.SaveGameService.DefaultSaveDirectory;
                if (Directory.Exists(real) && Directory.EnumerateFileSystemEntries(real).Any() && lines.Any(l => AuditSpec.Parse(l, out _).Autosave > 0))
                {
                    Debug.LogError($"AUDIT: REFUSED - the player's saves folder ({real}) holds files and the plan autosaves into it. Move it aside first (Tools/audit/real_saves.ps1 -Aside).");
                    EditorApplication.Exit(2);
                    return;
                }

                Debug.Log($"AUDIT: the plan uses the player's saves folder ({real}) - {(Directory.Exists(real) ? Directory.EnumerateFileSystemEntries(real).Count() + " entr(ies) in it at the start" : "absent at the start")}.");
            }

            string dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), DefaultOutputRoot, label));
            Directory.CreateDirectory(dir);
            AuditPrefs.RestoreIfStale(dir);

            SessionState.SetString(PlanKey, string.Join("\n", lines));
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetString(ResultsKey, string.Empty);
            SessionState.SetString(DigestsKey, string.Empty);
            SessionState.SetInt(WorstKey, 0);
            SessionState.SetString(LabelKey, label);
            SessionState.SetString(DirKey, dir);
            SessionState.SetString(PlanStartKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            Debug.Log($"AUDIT: plan '{label}' - {lines.Count} run(s) from {planPath}; output {dir}.");
            BeginRun();
        }

        private static void BeginRun()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetString(RunStartKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ReattachAfterDomainReload()
        {
            if (!SessionState.GetBool(ActiveKey, false)) { return; }
            SessionState.SetBool(ActiveKey, false);
            _attachAttempts = 0;
            EditorApplication.delayCall += Attach;
        }

        private static int _attachAttempts;
        private static AuditDriver _driver;
        private static PlayerLoopSystem _savedLoop;
        private static bool _installed;

        /// <summary>The player-loop system's marker type: the audit's tick, appended to the Update phase so it runs after every MonoBehaviour.Update.</summary>
        private struct AuditTick { }

        private static void Attach()
        {
            if (!EditorApplication.isPlaying)
            {
                // Bounded, as the film's attach is: an unbounded retry is a hang with no log line.
                if (++_attachAttempts > 600)
                {
                    Debug.LogError("AUDIT: play mode never started - giving up rather than hanging.");
                    EditorApplication.Exit(1);
                    return;
                }

                EditorApplication.delayCall += Attach;
                return;
            }

            Application.runInBackground = true;
            string[] plan = SessionState.GetString(PlanKey, string.Empty).Split('\n');
            int index = SessionState.GetInt(IndexKey, 0);
            AuditSpec spec = AuditSpec.Parse(plan[index], out string error);
            if (spec == null)
            {
                Debug.LogError($"AUDIT: the plan's run {index + 1} no longer parses ({error}).");
                EditorApplication.Exit(1);
                return;
            }

            string dir = SessionState.GetString(DirKey, string.Empty);
            Debug.Log($"AUDIT: RUN {index + 1} of {plan.Length} - {spec.Describe()}");
            _driver = new AuditDriver(spec, dir, OnRunEnd);
            Install();
        }

        private static void Install()
        {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            _savedLoop = root;
            PlayerLoopSystem[] phases = (PlayerLoopSystem[])root.subSystemList.Clone();
            bool placed = false;
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(UnityEngine.PlayerLoop.Update)) { continue; }
                var subs = new List<PlayerLoopSystem>(phases[i].subSystemList ?? Array.Empty<PlayerLoopSystem>())
                {
                    new PlayerLoopSystem { type = typeof(AuditTick), updateDelegate = TickOnce },
                };
                phases[i].subSystemList = subs.ToArray();
                placed = true;
            }

            if (!placed)
            {
                Debug.LogError("AUDIT: the player loop has no Update phase on this Unity - the tick cannot be placed after the controller's Update.");
                EditorApplication.Exit(1);
                return;
            }

            root.subSystemList = phases;
            PlayerLoop.SetPlayerLoop(root);
            _installed = true;
        }

        private static void Uninstall()
        {
            if (!_installed) { return; }
            PlayerLoop.SetPlayerLoop(_savedLoop);
            _installed = false;
        }

        private static void TickOnce()
        {
            AuditDriver driver = _driver;
            if (driver == null) { return; }
            driver.Tick();
        }

        /// <summary>A run ended: its line is kept, the tick removed, and play mode left so the next run starts in a fresh domain.</summary>
        private static void OnRunEnd(int code, string summary, string digest)
        {
            _driver = null;
            Uninstall();
            Time.captureDeltaTime = 0f;
            string[] plan = SessionState.GetString(PlanKey, string.Empty).Split('\n');
            int index = SessionState.GetInt(IndexKey, 0);
            AuditSpec spec = AuditSpec.Parse(plan[index], out _);
            string id = spec != null ? spec.Id : "?";
            string line = string.Format(CultureInfo.InvariantCulture, "{0}: exit {1} ({2:F1} s) - {3}", id, code, SecondsSince(RunStartKey), summary);

            // The protocol's determinism pairs: a run naming another with compare= must reach the same trajectory digest.
            string digests = SessionState.GetString(DigestsKey, string.Empty) + id + "=" + digest + "\n";
            SessionState.SetString(DigestsKey, digests);
            if (spec != null && !string.IsNullOrEmpty(spec.Compare))
            {
                string other = Lookup(digests, spec.Compare);
                string verdict;
                if (other == null) { verdict = $"COMPARE {id} with {spec.Compare}: NOT RUN before it - nothing compared"; code = Math.Max(code, 1); }
                else if (other == digest) { verdict = $"COMPARE {id} with {spec.Compare}: IDENTICAL (digest {Short(digest)})"; }
                else
                {
                    // The digests part; the dates decide whether the STATES did (a coarser frame writes fewer days, a resumed run starts at its file's day).
                    string dir = SessionState.GetString(DirKey, string.Empty);
                    (bool commonIdentical, string detail) = AuditTrajectory.CompareByDate(dir, spec.Compare, id);
                    if (commonIdentical)
                    {
                        verdict = $"COMPARE {id} with {spec.Compare}: IDENTICAL BY DATE ({Short(digest)} against {Short(other)}: the lines differ, the states do not) - {detail}";
                    }
                    else
                    {
                        verdict = $"COMPARE {id} with {spec.Compare}: DIFFERENT ({Short(digest)} against {Short(other)}) - {detail}; by line: {AuditTrajectory.FirstDifference(dir, id, spec.Compare)}";
                        code = Math.Max(code, 1);
                    }
                }

                Debug.Log("AUDIT: " + verdict);
                line += "\n    " + verdict;
            }

            SessionState.SetString(ResultsKey, SessionState.GetString(ResultsKey, string.Empty) + line + "\n");
            SessionState.SetInt(WorstKey, Math.Max(SessionState.GetInt(WorstKey, 0), code));
            SessionState.SetInt(IndexKey, index + 1);
            Debug.Log("AUDIT: RUN done - " + line);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) { return; }
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            string[] plan = SessionState.GetString(PlanKey, string.Empty).Split('\n');
            int index = SessionState.GetInt(IndexKey, 0);
            if (index < plan.Length)
            {
                BeginRun();
                return;
            }

            int worst = SessionState.GetInt(WorstKey, 0);
            string dir = SessionState.GetString(DirKey, string.Empty);
            string results = SessionState.GetString(ResultsKey, string.Empty);
            string summary = $"AUDIT: done - plan '{SessionState.GetString(LabelKey, "audit")}', {plan.Length} run(s) in {SecondsSince(PlanStartKey):F1} s, exiting {worst}.\n{results}";
            try { File.WriteAllText(Path.Combine(dir, "summary.txt"), summary, new UTF8Encoding(false)); }
            catch (Exception e) { Debug.LogError($"AUDIT: could not write the summary file ({e.Message})."); }
            SessionState.SetString(PlanKey, string.Empty);
            Debug.Log(summary);
            EditorApplication.Exit(worst);
        }

        private static string Lookup(string digests, string id)
        {
            foreach (string pair in digests.Split('\n'))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq) == id) { return pair.Substring(eq + 1); }
            }

            return null;
        }

        private static string Short(string digest) => string.IsNullOrEmpty(digest) ? "none" : digest.Substring(0, Math.Min(12, digest.Length));

        private static double SecondsSince(string key) =>
            long.TryParse(SessionState.GetString(key, "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) && ticks > 0
                ? (DateTime.UtcNow.Ticks - ticks) / 1e7
                : -1.0;

        private static string Arg(string[] args, string prefix, string fallback)
        {
            foreach (string a in args)
            {
                if (a.StartsWith(prefix, StringComparison.Ordinal)) { return a.Substring(prefix.Length); }
            }

            return fallback;
        }
    }

    /// <summary>
    /// The machine's preferences, kept safe across a run. `PreferencePins` snapshots in memory, and a process that dies mid-run loses the
    /// snapshot - the player's own settings then stay pinned to the harness's. The audit writes the snapshot to a file first and restores
    /// from it at the next plan's start if a run never reached its end.
    /// </summary>
    internal static class AuditPrefs
    {
        private const string FileName = "prefs_snapshot.tsv";

        internal static void Save(string dir)
        {
            var sb = new StringBuilder();
            foreach ((string Key, PoliSim.UI.PreferenceKind Kind) p in PoliSim.UI.PreferenceKeys.All)
            {
                bool present = PlayerPrefs.HasKey(p.Key);
                string value = !present ? string.Empty
                    : p.Kind == PoliSim.UI.PreferenceKind.Float ? PlayerPrefs.GetFloat(p.Key).ToString("R", CultureInfo.InvariantCulture)
                    : PlayerPrefs.GetInt(p.Key).ToString(CultureInfo.InvariantCulture);
                sb.Append(p.Key).Append('\t').Append(p.Kind).Append('\t').Append(present ? "1" : "0").Append('\t').Append(value).Append('\n');
            }

            File.WriteAllText(Path.Combine(dir, FileName), sb.ToString(), new UTF8Encoding(false));
        }

        internal static void Done(string dir)
        {
            string path = Path.Combine(dir, FileName);
            if (File.Exists(path)) { File.Delete(path); }
        }

        internal static void RestoreIfStale(string dir)
        {
            string path = Path.Combine(dir, FileName);
            if (!File.Exists(path)) { return; }
            int restored = 0;
            foreach (string line in File.ReadAllLines(path))
            {
                string[] cells = line.Split('\t');
                if (cells.Length < 4) { continue; }
                if (cells[2] == "1")
                {
                    if (cells[1] == PoliSim.UI.PreferenceKind.Float.ToString()) { PlayerPrefs.SetFloat(cells[0], float.Parse(cells[3], CultureInfo.InvariantCulture)); }
                    else { PlayerPrefs.SetInt(cells[0], int.Parse(cells[3], CultureInfo.InvariantCulture)); }
                }
                else
                {
                    PlayerPrefs.DeleteKey(cells[0]);
                }

                restored++;
            }

            PlayerPrefs.Save();
            PoliSim.UI.PreferenceKeys.ReloadAll();
            File.Delete(path);
            Debug.LogWarning($"AUDIT: a previous run died with the preferences pinned - {restored} key(s) restored from {path} before this plan.");
        }
    }
}
