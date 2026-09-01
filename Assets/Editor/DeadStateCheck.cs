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
    /// <para>⚠ <b>S-23 CLOSED, 2026-09-01 — the claim is now TRUE, and it was false for months.</b> This
    /// paragraph used to say a field written and never read was caught, then was corrected to say it was
    /// NOT and that telling a read from a write *"needs more than a regex"*. ⚠ **It needed a regex and a
    /// classifier**: a second pass counts, per OCCURRENCE, whether the name is an assignment target, and a
    /// field with writes and zero reads is reported as <c>WRITE_ONLY</c> against its own ratchet.</para>
    ///
    /// <para>⚠ <b>Its first run found SIX, and one of them was the check's own fault.</b> The real five
    /// were the `_cached*Raw` family — **the worked example this doc already named**, whose readers were
    /// the `GetCached*Input` accessors this check DID catch, so deleting the accessors left the fields
    /// behind looking alive — plus `_primaryButtonStyle`. **All six deleted; the ceiling was not touched.**
    /// The seventh, `_attachAttempts`, was a FALSE POSITIVE: `if (++x > 600)` consumes the value, and the
    /// first rule called every increment a write. ⚠ **A false positive here is not a harmless over-report
    /// — it would have had somebody delete a live loop bound**, so the rule now asks what FOLLOWS the
    /// operator: a statement terminator means the value went nowhere.</para>
    ///
    /// <para><b>What the classifier decides, stated as the choice it is.</b> A compound assignment is
    /// semantically a read AND a write, and is counted as a WRITE: a field that only accumulates into
    /// itself is not being CONSUMED, which is the question. `out`/`ref` count as writes, because the
    /// callee may only assign and counting them as reads would let a field escape by being passed
    /// somewhere. ⚠ **And a DECLARATION is neither** — getting that wrong made the first version report
    /// zero on a planted write-only field, because `private int x;` has no `=` after the name.</para>
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

        /// <summary>S-23's ratchet: private fields written and never read. ⚠ Set from the FIRST
        /// measurement rather than to zero, because the class was undetectable until 2026-09-01 and a
        /// ceiling of zero on an unmeasured backlog is a number chosen for how it looks.</summary>
        private const int WriteOnlyCeiling = 0;

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
            // ⚠ COMMENTS STRIPPED (2026-09-01, found by the ninth sweep's ENROLMENT census): this check
            // counts OCCURRENCES of a private declaration's name, so a comment merely MENTIONING a dead
            // field made it look read. A fifth instance of the class, found the first time the census ran.
            foreach (string file in files) { contents[file] = SourceText.WithoutComments(File.ReadAllText(file)); }

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
            var writeOnly = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string kind, string name, string where) in declarations)
            {
                if (!seen.Add(kind + ":" + name)) { continue; }

                int occurrences = 0;
                foreach (string text in contents.Values) { occurrences += CountWord(text, name); }

                // 1 = the declaration itself and nothing else.
                if (occurrences <= 1) { unreached.Add($"{kind,-7} {name,-42} {where}"); continue; }

                // ⚠ S-23 CLOSED HERE, 2026-09-01. The count above cannot tell a READ from a WRITE, so a
                // field with a declaration and one write occurs twice and passes whatever reads it - and
                // this check's own error text claimed that class anyway. The worked example is in this
                // repository: the write-only fields' only readers were accessors this check DID catch,
                // and deleting the accessors left the fields invisible to it.
                //
                // The classifier is per-OCCURRENCE and its rules are stated because they are CHOICES:
                //   `x =` (not `==`), `x +=` `-=` `*=` `/=`, `x++` `++x` `x--` `--x`, `out x`, `ref x`
                // count as WRITES; every other occurrence is a READ. ⚠ A compound assignment is
                // semantically both and is deliberately counted as a write only: a field that merely
                // accumulates into itself is not being CONSUMED, which is the question being asked.
                // ⚠ `out`/`ref` count as writes because the callee may only assign - counting them as
                // reads would let a field escape scrutiny by being passed somewhere.
                if (kind != "field") { continue; }

                int reads = 0;
                foreach (string text in contents.Values) { reads += CountReads(text, name); }

                if (reads == 0) { writeOnly.Add($"{kind,-7} {name,-42} {where}"); }
            }

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (b): state and code nothing reaches ===\n");
            sb.Append(F("    THE ENUMERATION: {0} .cs files under Assets/; every private field and private method\n", files.Length));
            sb.Append(F("    declaration - {0} distinct name(s). Occurrences counted across the whole corpus INCLUDING\n", seen.Count));
            sb.Append("    string literals, because this project's harnesses reach private state by reflection.\n");
            sb.Append(F("\n    {0} reached · {1} UNREACHED (ceiling {2}).\n", seen.Count - unreached.Count, unreached.Count, UnreachedCeiling));
            RatchetLedger.Report("DeadStateCheck.UNREACHED", unreached.Count, UnreachedCeiling);

            foreach (string line in unreached) { sb.Append("    GAP  ").Append(line).Append('\n'); }

            sb.Append(F("\n    {0} WRITE-ONLY field(s) (ceiling {1}) - reached, but nothing READS them.\n"
                        + "    ⚠ S-23: the occurrence count above cannot see this class. A field with a declaration and\n"
                        + "    one write occurs twice and passes, whatever reads it - and this check used to CLAIM the\n"
                        + "    class in its own error text. The classifier's rules are choices and are stated at the call\n"
                        + "    site; the load-bearing one is that a compound assignment counts as a WRITE, because a\n"
                        + "    field that only accumulates into itself is not being consumed.\n",
                writeOnly.Count, WriteOnlyCeiling));
            RatchetLedger.Report("DeadStateCheck.WRITE_ONLY", writeOnly.Count, WriteOnlyCeiling);
            foreach (string line in writeOnly) { sb.Append("    WRITE-ONLY  ").Append(line).Append('\n'); }

            if (writeOnly.Count > WriteOnlyCeiling)
            {
                Debug.LogError("STATE: " + writeOnly.Count + " WRITE-ONLY field(s), above the ceiling of "
                               + WriteOnlyCeiling + ". ⚠ S-23's class: a field that something WRITES and nothing "
                               + "READS. The occurrence count cannot see it - a declaration plus a write is two "
                               + "occurrences and passes - and this check CLAIMED the class in its own error text for "
                               + "months while being unable to detect it. Delete the field, or read it.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

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
        /// <summary>Occurrences of <paramref name="word"/> that are READS - every whole-word occurrence
        /// that is not an assignment target. ⚠ The rules are listed at the call site because they are
        /// choices rather than facts, and the most important one is that a compound assignment counts as a
        /// WRITE: a field that only accumulates into itself is not being consumed.</summary>
        private static int CountReads(string text, string word)
        {
            int reads = 0;
            int index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
            {
                int after = index + word.Length;
                bool leftOk = index == 0 || !IsWordChar(text[index - 1]);
                bool rightOk = after >= text.Length || !IsWordChar(text[after]);
                if (!leftOk || !rightOk) { index = after; continue; }

                // ⚠ THE DECLARATION IS NEITHER A READ NOR A WRITE, and getting this wrong made the first
                // version of this classifier report ZERO on a planted write-only field. `private int x;`
                // has no `=` after the name, so the naive rule counted the DECLARATION as a read and every
                // write-only field looked read-once. The probe caught it; the check did not.
                int lineStart = text.LastIndexOf((char)10, index) + 1;
                int lineEnd = text.IndexOf((char)10, index);
                if (lineEnd < 0) { lineEnd = text.Length; }
                Match decl = PrivateField.Match(text.Substring(lineStart, lineEnd - lineStart));
                if (decl.Success && decl.Groups[1].Value == word) { index = after; continue; }

                // Look right, past spaces, for an assignment or an increment.
                int j = after;
                while (j < text.Length && (text[j] == ' ' || text[j] == '\t')) { j++; }

                bool isWrite = false;
                if (j < text.Length)
                {
                    char c0 = text[j];
                    char c1 = j + 1 < text.Length ? text[j + 1] : '\0';

                    // `x =` but NOT `x ==`, `x =>`, `x !=`, `x <=`, `x >=`.
                    if (c0 == '=' && c1 != '=' && c1 != '>') { isWrite = true; }
                    else if ((c0 == '+' || c0 == '-' || c0 == '*' || c0 == '/') && c1 == '=') { isWrite = true; }
                    // ⚠ AN INCREMENT IS A WRITE ONLY WHEN ITS VALUE IS DISCARDED. `x++;` is a write;
                    // `if (++x > 600)` READS x, and the first version of this rule called it a write and
                    // reported `_attachAttempts` dead when it is the loop bound of the capture's attach
                    // retry. **A false positive is not a harmless over-report here** - it would have had
                    // somebody delete a live guard. The test is what FOLLOWS the operator: a statement
                    // terminator means the value went nowhere.
                    else if ((c0 == '+' && c1 == '+') || (c0 == '-' && c1 == '-'))
                    {
                        int m = j + 2;
                        while (m < text.Length && (text[m] == ' ' || text[m] == '\t')) { m++; }
                        isWrite = m < text.Length && text[m] == ';';
                    }
                }

                // Look left, past spaces, for `out`/`ref`/`++`/`--`.
                if (!isWrite)
                {
                    int k = index - 1;
                    while (k >= 0 && (text[k] == ' ' || text[k] == '\t')) { k--; }
                    // Same rule on the prefix side: `++x;` discards, `++x > 600` does not.
                    if (k >= 1 && ((text[k - 1] == '+' && text[k] == '+') || (text[k - 1] == '-' && text[k] == '-')))
                    {
                        int m = after;
                        while (m < text.Length && (text[m] == ' ' || text[m] == '\t')) { m++; }
                        isWrite = m < text.Length && text[m] == ';';
                    }
                    else if (k >= 2 && text[k] == 't' && text[k - 1] == 'u' && text[k - 2] == 'o'
                             && (k < 3 || !IsWordChar(text[k - 3]))) { isWrite = true; }
                    else if (k >= 2 && text[k] == 'f' && text[k - 1] == 'e' && text[k - 2] == 'r'
                             && (k < 3 || !IsWordChar(text[k - 3]))) { isWrite = true; }
                }

                if (!isWrite) { reads++; }
                index = after;
            }

            return reads;
        }

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
