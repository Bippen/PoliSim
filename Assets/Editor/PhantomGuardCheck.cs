using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-E3 (S-11) — **a doc comment that names a guard must name a guard that exists.**
    ///
    /// <para><b>Why this exists: it happened twice in one pass.</b> `PoliSimTheme` cited a
    /// `PartyInkHarness` that did not exist (found at C-B2, which then had to build it), and the stranded
    /// branch's `ApplyThreshold` named a `CoalitionShare` rule it never read (C-0.3). ⚠ <b>A comment
    /// naming a check is a promise the build should keep, and nothing was keeping it</b> — the comment
    /// reads as evidence, the reader believes the thing is covered, and nobody looks again.</para>
    ///
    /// <para><b>THE ENUMERATION.</b> Every `.cs` file under `Assets/` is scanned; inside COMMENT text only
    /// (`//` and `///`), every identifier ending in <b>Check</b>, <b>Harness</b> or <b>Diagnostic</b> is
    /// collected and must resolve to a type that actually exists in the loaded assemblies. That suffix
    /// set is this project's own naming convention for its guards, and it is stated here rather than
    /// implied.</para>
    ///
    /// <para>⚠ <b>What it deliberately does NOT do.</b> It does not check that the named guard actually
    /// covers what the comment claims — that is a judgement no regex can make. It checks the one thing a
    /// machine can: <b>the guard exists.</b> Both real instances were of exactly that kind, and a check
    /// that overreached here would produce false alarms nobody would keep green.</para>
    ///
    /// <para>⚠ <b>RETIRED NAMES ARE A GAP, NOT A FAILURE.</b> A comment recording that something USED to
    /// exist ("…until C-C14 deleted that field") is history, and history is what this project keeps most
    /// carefully. So a name inside a sentence that marks it as past is reported and not failed; anything
    /// else that does not resolve FAILS.</para>
    /// </summary>
    public static class PhantomGuardCheck
    {
        private static readonly Regex GuardName = new Regex(@"\b([A-Z][A-Za-z0-9]*(?:Check|Harness|Diagnostic))\b");

        /// <summary>Words that mark a name as HISTORY rather than a live claim. A comment saying a guard
        /// was deleted is a record, and records are not defects.</summary>
        private static readonly string[] PastTenseMarkers =
        {
            "deleted", "removed", "retired", "used to", "no longer", "did not exist", "does not exist",
            "was renamed", "replaced by", "superseded",
        };

        /// <summary>⚠ Names that are generic English rather than a guard: the regex cannot tell
        /// "the harness" from a type name, and a check that failed on prose would be
        /// abandoned within a week. Enumerated so the exclusion is visible.</summary>
        private static readonly HashSet<string> NotTypeNames = new HashSet<string>
        {
            "Check", "Harness", "Diagnostic",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                foreach (Type type in types)
                {
                    if (type?.Name != null) { known.Add(type.Name); }
                }
            }

            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            var missing = new List<string>();
            var historical = new List<string>();
            int scanned = 0;
            int names = 0;

            foreach (string file in files)
            {
                scanned++;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int comment = line.IndexOf("//", StringComparison.Ordinal);
                    if (comment < 0) { continue; }

                    string text = line.Substring(comment);
                    foreach (Match match in GuardName.Matches(text))
                    {
                        string name = match.Groups[1].Value;
                        if (NotTypeNames.Contains(name)) { continue; }

                        names++;
                        if (known.Contains(name)) { continue; }

                        string where = $"{Relative(file, root)}:{i + 1}  {name}";
                        if (LooksHistorical(text)) { historical.Add(where); }
                        else { missing.Add(where); }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== C-E3: the phantom-guard check ===\n");
            sb.Append(F("    THE ENUMERATION: {0} .cs files under Assets/, comment text only, every identifier ending in\n", scanned));
            sb.Append(F("    Check / Harness / Diagnostic - {0} name(s) found, each required to resolve to a real type.\n", names));

            foreach (string line in historical)
            {
                sb.Append("    HISTORY  ").Append(line).Append("  (the sentence marks it as past - reported, not failed)\n");
            }

            foreach (string line in missing)
            {
                Debug.LogError($"PHANTOM: {line} - a comment names this guard and no such type exists. A comment naming a "
                               + "check is a promise the build should keep; either build it, correct the name, or say plainly "
                               + "that it was removed.");
                sb.Append("    ⚠ MISSING ").Append(line).Append('\n');
            }

            sb.Append(F("\n    {0} resolved, {1} historical, {2} MISSING.\n", names - historical.Count - missing.Count, historical.Count, missing.Count));

            if (missing.Count == 0)
            {
                sb.Append("    CLEAN - every guard a comment names exists.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        private static bool LooksHistorical(string text)
        {
            foreach (string marker in PastTenseMarkers)
            {
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) { return true; }
            }

            return false;
        }

        private static string Relative(string path, string root) =>
            path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length + 1) : path;

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
