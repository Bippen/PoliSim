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
    /// P3-C3 (2026-09-03): every dial's LEFT label, RIGHT label and RANGE agree. A dial row is
    /// `DrawDialRow("Name", standing, draft, Min, Max, format, suffix, "trailing")`; the trailing text names
    /// the ends ("0 light - 100 heavy"). The finding: "Deregulation / Nationalization" on the left against
    /// "0 nationalized - 100 private" on the right - one axis, two wordings, the ends reversed. The rule this
    /// asserts, over every call site in `GameController.cs` (read as text, the way MetaTextCheck reads
    /// literals): (1) a trailing of the form "N word - M word" carries the range's own ends - N and M equal
    /// the Min and Max constants the call names (reflected from GameController, never restated here);
    /// (2) when the name is two-ended ("A / B"), the trailing's first word belongs to A and its second to B,
    /// judged by a shared five-letter stem (nationaliz~ / deregulat~), so the order and the wording agree;
    /// (3) a two-ended name with no trailing is reported, because the ends are unsaid. Exit 1 on any
    /// disagreement; every dial is printed with its verdict.
    /// </summary>
    public static class DialLabelCheck
    {
        private static readonly Regex Call = new Regex(
            @"DrawDialRow\(\s*""(?<name>[^""]*)""\s*,(?<args>(?:[^;])*?)\)\s*;",
            RegexOptions.Singleline);
        private static readonly Regex Trailing = new Regex(@"""(?<t>[^""]*)""\s*(?:,\s*interactive:[^)]*)?$");
        private static readonly Regex Ends = new Regex(@"^\s*(?<n>-?\d+(?:\.\d+)?)\s+(?<a>[A-Za-z][\w-]*)[^-]*-\s*(?<m>-?\d+(?:\.\d+)?)\s+(?<b>[A-Za-z][\w-]*)");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            string path = Path.Combine(Application.dataPath, "Scripts", "UI", "GameController.cs");
            string text = File.ReadAllText(path);
            Type controller = typeof(PoliSim.UI.GameController);
            int dials = 0;
            sb.Append("=== DialLabelCheck (P3-C3): every dial's left label, right label and range ===\n");
            foreach (Match m in Call.Matches(text))
            {
                dials++;
                string name = m.Groups["name"].Value;
                // The call's own line comments are not arguments (a comma inside one split the sector dial's list).
                string args = Regex.Replace(m.Groups["args"].Value, @"//[^\n]*", string.Empty);
                string[] parts = SplitTopLevel(args);
                // args: standing, draft, min, max, format, suffix, trailing[, interactive]
                string minExpr = parts.Length > 2 ? parts[2].Trim() : "";
                string maxExpr = parts.Length > 3 ? parts[3].Trim() : "";
                string trailing = null;
                if (parts.Length > 6)
                {
                    string t = parts[6].Trim();
                    // A trailing wrapped in a helper (LaborDialTrailing("0 unregulated - 100 strict", …)) carries its ends in
                    // the first literal; a bare literal is itself; null and string.Empty are no trailing.
                    Match wrapped = Regex.Match(t, @"^[A-Za-z_]\w*\(\s*""(?<lit>[^""]*)""");
                    if (wrapped.Success) { trailing = wrapped.Groups["lit"].Value; }
                    else if (t.StartsWith("\"", StringComparison.Ordinal) && t.EndsWith("\"", StringComparison.Ordinal)) { trailing = t.Substring(1, t.Length - 2); }
                    else if (t == "null" || t == "string.Empty" || t.StartsWith("null", StringComparison.Ordinal)) { trailing = null; }
                    else { trailing = t; }
                }
                bool twoEnded = name.Contains(" / ");
                string verdict = "ok";
                if (trailing != null)
                {
                    Match e = Ends.Match(trailing);
                    if (e.Success)
                    {
                        float n = float.Parse(e.Groups["n"].Value, CultureInfo.InvariantCulture);
                        float mx = float.Parse(e.Groups["m"].Value, CultureInfo.InvariantCulture);
                        float? min = Constant(controller, minExpr), max = Constant(controller, maxExpr);
                        if (min.HasValue && max.HasValue && (Mathf.Abs(min.Value - n) > 0.001f || Mathf.Abs(max.Value - mx) > 0.001f))
                        {
                            verdict = $"FAIL: the trailing's ends {n}–{mx} are not the range {min.Value}–{max.Value} ({minExpr}, {maxExpr})";
                        }
                        else if (twoEnded)
                        {
                            string left = name.Substring(0, name.IndexOf(" / ", StringComparison.Ordinal)).Trim();
                            string right = name.Substring(name.IndexOf(" / ", StringComparison.Ordinal) + 3).Trim();
                            bool aLeft = Stem(e.Groups["a"].Value, left), bRight = Stem(e.Groups["b"].Value, right);
                            bool aRight = Stem(e.Groups["a"].Value, right), bLeft = Stem(e.Groups["b"].Value, left);
                            if (aLeft && bRight) { verdict = "ok (both ends named, in order)"; }
                            else if (aRight || bLeft) { verdict = $"FAIL: the ends are REVERSED - '{name}' reads left-to-right but the trailing '{trailing}' puts {e.Groups["a"].Value} at {n}"; }
                            else if (aLeft || bRight) { verdict = $"FAIL: the trailing '{trailing}' names only one end of '{name}'"; }
                            else { verdict = "ok (one concept with a slash; the trailing names its own ends)"; }
                        }
                    }
                    else if (twoEnded)
                    {
                        verdict = $"FAIL: two-ended '{name}' has a trailing that names no ends: '{trailing}'";
                    }
                }
                else if (twoEnded)
                {
                    verdict = $"FAIL: two-ended '{name}' has no trailing - its ends are unsaid";
                }
                if (verdict.StartsWith("FAIL", StringComparison.Ordinal)) { failures.Add(verdict); }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-40} range {1}..{2}  trailing {3,-32} {4}\n", name, minExpr, maxExpr, trailing == null ? "-" : "'" + Regex.Replace(trailing, @"s+", " ") + "'", verdict));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} dial row(s) read from GameController.cs.\n", dials));
            if (dials == 0) { failures.Add("no DrawDialRow call sites were read - the pattern no longer matches the code, and this verified nothing"); }

            if (failures.Count == 0)
            {
                sb.Append("\n=== DialLabelCheck: ALL ASSERTIONS PASS ===\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append($"\n=== DialLabelCheck: {failures.Count} FAILURE(S) ===\n");
                foreach (string f in failures) { sb.Append("    ").Append(f).Append('\n'); }
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }

        /// <summary>A five-letter stem shared between the trailing's word and the name's half ("nationalized" ↔ "Nationalization"), case-insensitive; "private" does not stem to "Deregulation".</summary>
        private static bool Stem(string word, string half)
        {
            string w = word.ToLowerInvariant();
            foreach (string token in half.ToLowerInvariant().Split(' ', '-', '/'))
            {
                if (token.Length >= 5 && w.Length >= 5 && w.StartsWith(token.Substring(0, 5), StringComparison.Ordinal)) { return true; }
            }
            return false;
        }

        private static float? Constant(Type controller, string expr)
        {
            FieldInfo f = controller.GetField(expr, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) { return null; }
            object v = f.GetValue(null);
            return v is float fv ? fv : v is int iv ? iv : (float?)null;
        }

        private static string[] SplitTopLevel(string args)
        {
            var parts = new List<string>();
            int depth = 0; bool inString = false; var current = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '"' && (i == 0 || args[i - 1] != '\\')) { inString = !inString; }
                if (!inString)
                {
                    if (c == '(' || c == '[' || c == '{') { depth++; }
                    else if (c == ')' || c == ']' || c == '}') { depth--; }
                    else if (c == ',' && depth == 0) { parts.Add(current.ToString()); current.Clear(); continue; }
                }
                current.Append(c);
            }
            parts.Add(current.ToString());
            return parts.ToArray();
        }
    }
}
