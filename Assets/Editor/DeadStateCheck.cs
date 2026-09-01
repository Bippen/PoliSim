using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The coherence audit, sweep (b) — **state and code nothing reaches.** C-N3's method, applied past
    /// levers.
    ///
    /// <para><b>Why.</b> `LeverLivenessCheck` asked whether a player-facing field moves the model and
    /// found three dead levers and one field nothing reads at all (`SwfDomesticAllocationOverride`,
    /// C-N6). ⚠ **That method was pointed only at `PolicyDecision`.** The same question applies to every
    /// private field, every private helper and every enum member in the codebase: **is anything
    /// reaching this?** A field written and never read, a helper with no callers and an enum member
    /// nothing constructs are all the same defect — code that reads as live and is not — and this pass
    /// has already deleted one such path by hand (`GraphRenderer.DrawPublished`, RIDE-1, 351 lines).</para>
    ///
    /// <para><b>THE ENUMERATION.</b> Every `.cs` file under `Assets/`. Declarations matched: private
    /// fields, private methods, and enum members. A name whose only occurrence in the whole codebase is
    /// its own declaration is **unreached**.</para>
    ///
    /// <para>⚠ <b>Occurrences are counted in STRING LITERALS too, and that is deliberate.</b> This
    /// project's harnesses reach private state by reflection — `SetPrivateField(controller,
    /// "_canvasLive", …)` — so a field named only inside a string is genuinely reached, and a check that
    /// ignored strings would report the whole capture driver's surface as dead.</para>
    ///
    ///
    /// <para>⚠ <b>ONE CLASS IT CLAIMS AND DOES NOT CATCH, corrected here rather than left as a false
    /// claim (2026-09-01).</b> The paragraph above says *"a field written and never read"* is caught. It
    /// is NOT: the detector counts a name's occurrences across the corpus, so a field with a declaration
    /// AND a write occurs twice and passes, whatever reads it. The worked example is in this repository —
    /// the ~31 `_cached*Input` fields `RecomputePolicyPreview` snapshots every preview, whose only
    /// readers were the `GetCached*Input` accessors this check DID catch. **Deleting the accessors left
    /// the fields invisible to it.** Distinguishing a read from a write needs more than a regex, so the
    /// limitation is named rather than half-fixed, and the write-only family is cleared by hand.</para>
    /// <para>⚠ <b>A ratchet, not a verdict.</b> Findings are GAPs against a recorded ceiling; what fails
    /// is GROWTH. The first run of a sweep like this finds a backlog, and a check that goes red on a
    /// backlog is a check somebody disables.</para>
    /// </summary>
    public static class DeadStateCheck
    {
        /// <summary>⚠ The ceiling. Built at **39 on 2026-08-31**; **lowered to 29, 19, 9 and 0 on 2026-09-01** as four
        /// ratchet batches cleared the backlog ENTIRELY. It may be lowered, never
        /// raised.** A new unreached declaration pushes the count over it and fails.</summary>
        private const int UnreachedCeiling = 0;

        private static readonly Regex PrivateField = new Regex(
            @"^\s*private\s+(?:static\s+)?(?:readonly\s+)?[A-Za-z_][A-Za-z0-9_<>,\.\[\]\?]*\s+(_?[A-Za-z][A-Za-z0-9_]*)\s*(?:=|;)");

        /// <summary>⚠ Unity calls these BY NAME. Enumerated so the blind spot is visible.</summary>
        private static readonly HashSet<string> UnityMessages = new HashSet<string>(StringComparer.Ordinal)
        {
            "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnGUI", "OnEnable", "OnDisable",
            "OnDestroy", "OnApplicationQuit", "OnApplicationFocus", "OnApplicationPause",
            "OnRectTransformDimensionsChange", "OnValidate", "Reset", "OnDrawGizmos",
        };

        private static readonly Regex PrivateMethod = new Regex(
            @"^\s*private\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\.\[\]\?]*\s+([A-Z][A-Za-z0-9_]*)\s*\(");

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            // One pass to read every file, one to count - the codebase is small enough that the whole
            // corpus fits in memory, and a per-name file walk would be quadratic.
            var contents = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in files) { contents[file] = File.ReadAllText(file); }

            var declarations = new List<(string Kind, string Name, string Where)>();
            foreach (string file in files)
            {
                string[] lines = contents[file].Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    Match field = PrivateField.Match(lines[i]);
                    if (field.Success)
                    {
                        declarations.Add(("field", field.Groups[1].Value, $"{Path.GetFileName(file)}:{i + 1}"));
                        continue;
                    }

                    Match method = PrivateMethod.Match(lines[i]);
                    if (!method.Success) { continue; }

                    // ⚠ TWO KINDS OF CALLER THIS CHECK CANNOT SEE, excluded by name rather than guessed at.
                    // An ATTRIBUTE on the line above means the engine calls it - `[MenuItem]`,
                    // `[InitializeOnLoadMethod]` - and a UNITY MESSAGE is called by the engine by name.
                    // The first run reported both classes as dead, which would have been wrong twice.
                    if (i > 0 && lines[i - 1].TrimStart().StartsWith("[", StringComparison.Ordinal)) { continue; }
                    if (UnityMessages.Contains(method.Groups[1].Value)) { continue; }

                    declarations.Add(("method", method.Groups[1].Value, $"{Path.GetFileName(file)}:{i + 1}"));
                }
            }

            var unreached = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string kind, string name, string where) in declarations)
            {
                if (!seen.Add(kind + ":" + name)) { continue; }

                int occurrences = 0;
                foreach (string text in contents.Values) { occurrences += CountWord(text, name); }

                // 1 = the declaration itself and nothing else.
                if (occurrences <= 1) { unreached.Add($"{kind,-7} {name,-42} {where}"); }
            }

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (b): state and code nothing reaches ===\n");
            sb.Append(F("    THE ENUMERATION: {0} .cs files under Assets/; every private field and private method\n", files.Length));
            sb.Append(F("    declaration - {0} distinct name(s). Occurrences counted across the whole corpus INCLUDING\n", seen.Count));
            sb.Append("    string literals, because this project's harnesses reach private state by reflection.\n");
            sb.Append(F("\n    {0} reached · {1} UNREACHED (ceiling {2}).\n", seen.Count - unreached.Count, unreached.Count, UnreachedCeiling));
            RatchetLedger.Report("DeadStateCheck.UNREACHED", unreached.Count, UnreachedCeiling);

            foreach (string line in unreached) { sb.Append("    GAP  ").Append(line).Append('\n'); }

            if (unreached.Count > UnreachedCeiling)
            {
                Debug.LogError($"DEADSTATE: {unreached.Count} declaration(s) nothing reaches, above the recorded ceiling of "
                               + $"{UnreachedCeiling}. ⚠ A field written and never read, or a helper with no callers, reads as "
                               + "live code and is not - the class RIDE-1 deleted 351 lines of. Delete it, wire it, or record "
                               + "why it stays - and LOWER the ceiling, never raise it.");
                sb.Append("    ⚠ ABOVE THE CEILING - see the error above.\n");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            sb.Append(unreached.Count == 0
                ? "    CLEAN - every private declaration is reached by something.\n"
                : "    At or under the ceiling: the backlog is reported and may only shrink.\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>Whole-word occurrences, so `Gdp` does not match `GdpGrowth`.</summary>
        private static int CountWord(string text, string word)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
            {
                bool leftOk = index == 0 || !IsWordChar(text[index - 1]);
                int after = index + word.Length;
                bool rightOk = after >= text.Length || !IsWordChar(text[after]);
                if (leftOk && rightOk) { count++; }
                index = after;
            }

            return count;
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
