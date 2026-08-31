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
    /// The coherence audit, sweep (d) — **every numeric constant on a simulation path must say where it
    /// came from.**
    ///
    /// <para><b>The standing rule this enforces</b> is the project's oldest: *never invent a figure; every
    /// real-world number carries source, vintage and basis.* It has been enforced by review, which means
    /// it has been enforced unevenly — and this pass alone found `[AUTHORED-DRAFT]` values that had
    /// drifted into being cited as measurements (the ±5–10 % margin at C-C14) and a sourced shape whose
    /// scale was authored and read as sourced (the war chest at C-D2).</para>
    ///
    /// <para><b>THE ENUMERATION.</b> Every `const`/`static readonly` <c>float</c>, <c>double</c> or
    /// <c>int</c> declared under <c>Assets/Scripts/Simulation</c> — **the simulation path, where a number
    /// changes what the model does** — must carry, in its own doc comment or in the comment lines
    /// immediately above it, one of four provenance marks:</para>
    ///
    /// <list type="bullet">
    /// <item><description><b>SOURCED</b> — a citation: a named source, an institution, a year, a URL, or
    /// one of this project's own source tags.</description></item>
    /// <item><description><b>[AUTHORED-DRAFT]</b> — a game figure, honestly labelled.</description></item>
    /// <item><description><b>DERIVED</b> — computed from something else that is itself accounted
    /// for.</description></item>
    /// <item><description><b>CONVENTION</b> — a bound, a scale, an epsilon or a gameplay choice that is
    /// not a claim about the world, said as such.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>A constant with none of the four is a FINDING, not a failure.</b> It is reported as a
    /// GAP — `PartyMarkCoverageCheck`'s precedent — because the first run of any sweep like this finds a
    /// backlog, and a check that goes red on a backlog is a check that gets disabled. **What FAILS is
    /// growth**: the count is compared against a recorded ceiling, and a new unmarked constant pushes it
    /// over and fails. The backlog can only shrink.</para>
    /// </summary>
    public static class ConstantProvenanceCheck
    {
        /// <summary>
        /// ⚠ **The ceiling, and the only thing that fails.** Set to the count measured when this check was
        /// built - ⚠ Built at **212 of 285 on 2026-08-31**, reported as a BACKLOG rather than a failure; **lowered to 202 on 2026-09-01** by the first ratchet batch.
        /// A new unmarked constant raises the count above it and fails; marking an old one lowers
        /// the count, and the ceiling should be lowered with it. **It may never be raised** — raising it is
        /// how a ratchet becomes a rubber stamp.
        /// </summary>
        private const int UnmarkedCeiling = 202;

        private static readonly Regex Declaration = new Regex(
            @"^\s*(?:public|private|protected|internal)?\s*(?:const|static\s+readonly)\s+(float|double|int)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=");

        /// <summary>⚠ Every mark this check accepts, enumerated so the bar is visible rather than implied.
        /// Case-insensitive; one of these anywhere in the constant's own comment block is enough.</summary>
        private static readonly string[] ProvenanceMarks =
        {
            "sourced", "source:", "[verified]", "[provisional]", "[estimated]", "http", "oecd", "eurostat",
            "imf", "scb", "riksbank", "bea", "aer ", "journal", "20 06", "et al",
            "authored-draft", "authored draft",
            "derived", "computed from", "read off",
            "convention", "gameplay", "a bound", "bounds", "clamp", "epsilon", "scale factor",
            "not a researched figure", "not researched", "arbitrary", "ceiling", "floor",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "Simulation");
            if (!Directory.Exists(root))
            {
                Debug.LogError("PROVENANCE: no Assets/Scripts/Simulation directory - reporting nothing rather than reporting clean.");
                CheckExit.Finish(1);
                return;
            }

            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            int total = 0;
            var unmarked = new List<string>();
            var byMark = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    Match declaration = Declaration.Match(lines[i]);
                    if (!declaration.Success) { continue; }

                    total++;
                    string context = CommentBlockAbove(lines, i);
                    string mark = FindMark(context);
                    if (mark != null)
                    {
                        byMark.TryGetValue(mark, out int count);
                        byMark[mark] = count + 1;
                        continue;
                    }

                    unmarked.Add($"{Path.GetFileName(file)}:{i + 1}  {declaration.Groups[2].Value}");
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (d): constant provenance on the simulation path ===\n");
            sb.Append(F("    THE ENUMERATION: {0} file(s) under Assets/Scripts/Simulation; every const / static readonly\n", files.Length));
            sb.Append(F("    float, double or int - {0} constant(s). Each must carry SOURCED, [AUTHORED-DRAFT], DERIVED or\n", total));
            sb.Append("    CONVENTION in its own comment block.\n\n");

            var marks = new List<string>(byMark.Keys);
            marks.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string mark in marks) { sb.Append(F("    {0,-24} {1,4}\n", mark, byMark[mark])); }

            sb.Append(F("\n    {0} marked · {1} UNMARKED (ceiling {2}).\n", total - unmarked.Count, unmarked.Count, UnmarkedCeiling));

            foreach (string line in unmarked) { sb.Append("    GAP  ").Append(line).Append('\n'); }

            if (unmarked.Count > UnmarkedCeiling)
            {
                Debug.LogError($"PROVENANCE: {unmarked.Count} unmarked constant(s) on the simulation path, above the recorded "
                               + $"ceiling of {UnmarkedCeiling}. ⚠ A number that changes what the model does and says nothing "
                               + "about where it came from is the exact thing the standing rule forbids. Mark it SOURCED, "
                               + "[AUTHORED-DRAFT], DERIVED or CONVENTION - and LOWER the ceiling, never raise it.");
                sb.Append("    ⚠ ABOVE THE CEILING - see the error above.\n");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            sb.Append(unmarked.Count == 0
                ? "    CLEAN - every simulation constant says where it came from.\n"
                : "    At or under the ceiling: the backlog is reported and may only shrink.\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>The unbroken run of comment lines immediately above a declaration — a constant's
        /// provenance lives in its own doc comment, not three declarations up.</summary>
        private static string CommentBlockAbove(string[] lines, int index)
        {
            var sb = new StringBuilder();
            for (int i = index - 1; i >= 0; i--)
            {
                string trimmed = lines[i].TrimStart();
                if (!trimmed.StartsWith("//", StringComparison.Ordinal)) { break; }
                sb.Append(trimmed).Append(' ');
            }

            // Some declarations carry their note on the same line.
            sb.Append(lines[index]);
            return sb.ToString();
        }

        private static string FindMark(string context)
        {
            foreach (string mark in ProvenanceMarks)
            {
                if (context.IndexOf(mark, StringComparison.OrdinalIgnoreCase) >= 0) { return mark; }
            }

            return null;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
