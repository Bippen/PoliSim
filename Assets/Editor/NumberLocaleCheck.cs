using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §568 (2026-09-22, the sitting pass's Track 4, drift row D1) — **the desk's one number locale is installed,
    /// and it is installed in one place.**
    ///
    /// <para><b>What it decides.</b> Two things, and nothing else. (1) <see cref="UiCulture.Install"/> is CALLED from
    /// the game's own startup - exactly once in the runtime sources - so a desk that draws has had its number locale
    /// set; the harness's own call is allowed and expected beside it. (2) The culture that call installs formats a
    /// number with a POINT and no group separator, which is what every capture, every record and every claim about a
    /// figure in this repo assumes.</para>
    ///
    /// <para>⚠ <b>Why it is not a scan for bad call sites.</b> The defect D1 names is a number reaching a player
    /// surface through the machine's culture, and there were 195 such sites - every interpolated hole with a numeric
    /// format. Editing 195 sites would leave the 196th for a reader to find, which is how this drift arrived. The
    /// locale is a property of the SURFACE, so it is set once for the thread that draws it, and what this check
    /// guards is that the one statement is still there and still says what it said.</para>
    /// </summary>
    public static class NumberLocaleCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var runtimeCallers = new List<string>();
            var harnessCallers = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(projectRoot, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                if (rel.EndsWith("UiCulture.cs", System.StringComparison.Ordinal)) { continue; }

                // COMMENTS STRIPPED (the immunity census enrols this check): a commented-out call is history, not an install site.
                string code = SourceText.ReadWithoutComments(file);
                int at = code.IndexOf("UiCulture.Install(", System.StringComparison.Ordinal);
                while (at >= 0)
                {
                    int line = 1;
                    for (int i = 0; i < at; i++) { if (code[i] == '\n') { line++; } }

                    string where = rel + ":" + line.ToString(CultureInfo.InvariantCulture);
                    if (rel.StartsWith("Assets/Scripts/Testing/", System.StringComparison.Ordinal)) { harnessCallers.Add(where); }
                    else { runtimeCallers.Add(where); }

                    at = code.IndexOf("UiCulture.Install(", at + 1, System.StringComparison.Ordinal);
                }
            }

            UiCulture.Install();
            string sample = 1234.5f.ToString("F1", CultureInfo.CurrentCulture);
            bool separatorOk = sample == "1234.5";

            var sb = new StringBuilder();
            sb.Append("=== NumberLocaleCheck: the desk's one number locale ===\n");
            sb.Append($"  runtime call site(s): {(runtimeCallers.Count == 0 ? "NONE" : string.Join(", ", runtimeCallers))}\n");
            sb.Append($"  harness call site(s): {(harnessCallers.Count == 0 ? "none" : string.Join(", ", harnessCallers))}\n");
            sb.Append($"  machine culture: {CultureInfo.InstalledUICulture.Name}; installed: {UiCulture.Numbers.Name} with the invariant number format\n");
            sb.Append($"  a number through the installed culture: 1234.5 renders \"{sample}\"\n");

            int failures = 0;
            if (runtimeCallers.Count != 1)
            {
                sb.Append($"  ⚠ FAULT  the runtime installs the number locale in {runtimeCallers.Count} place(s); it is stated ONCE, at the game's start\n");
                failures++;
            }

            if (!separatorOk)
            {
                sb.Append($"  ⚠ FAULT  the installed culture renders 1234.5 as \"{sample}\" - the desk's numbers take a point and no group separator\n");
                failures++;
            }

            sb.Append($"\n=== NumberLocaleCheck: {(failures == 0 ? "CLEAN" : failures + " fault(s)")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
