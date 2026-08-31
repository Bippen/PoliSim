using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// S-21 — **the rule-15 diff, as a tool instead of an ad-hoc shell comparison.**
    ///
    /// <para><b>Why this exists, and it is not the reason first written down.</b> The closing gate of
    /// 2026-08-31 reported that two frames beyond the `det_*` record's named three were not byte-stable,
    /// "alternating rather than progressing". ⚠ <b>That claim was WRONG, and measuring it properly is what
    /// this file is.</b> Three controlled back-to-back film runs on one unchanged tree show
    /// `02a_statistics_domestic` and `06d_policylaws_policyweb_rows` <b>byte-identical every time</b>;
    /// the earlier variation was code-driven — the `StatNodeId` enum gained two members mid-window — and
    /// was misread as instability because the comparison spanned a code change.</para>
    ///
    /// <para><b>So the real defect was never those two frames. It was that the rule-15 diff had no tool.</b>
    /// It was re-typed as a shell loop at each pass end, with no named exclusion list, so **noise and
    /// change looked identical** and each reading depended on whoever ran it remembering which frames
    /// flicker. An evidence tool that reports noise as difference is worse than no tool — and one that
    /// exists only as a habit is not a tool at all.</para>
    ///
    /// <para><b>THE ENUMERATION.</b> Every PNG whose name begins `&lt;a&gt;_` is matched to its
    /// `&lt;b&gt;_` twin and compared by SHA-256. Three frames are EXCLUDED BY NAME, each with its reason
    /// — they are wall-clock or mid-envelope frames whose content is time-dependent by construction, and
    /// they are the same three the `det_*` record has named since 2026-08-28.</para>
    ///
    /// <para>⚠ <b>What FAILS, and what merely reports.</b> A difference is a REPORT — that is the tool's
    /// output, not a verdict. What fails is the tool being unable to do its job: a missing set, or a
    /// ROSTER MISMATCH (a frame present in one set and absent from the other), because that is a capture
    /// that silently did not happen, and this project has already lost a run to exactly that.</para>
    ///
    /// <para>Run: `-executeMethod PoliSim.EditorTools.FilmDiffCheck.Run -filma=&lt;label&gt;
    /// -filmb=&lt;label&gt;`.</para>
    /// </summary>
    public static class FilmDiffCheck
    {
        /// <summary>
        /// ⚠ **The excluded frames, each with the reason it cannot be compared byte-for-byte.** Measured,
        /// not assumed: three controlled runs on one unchanged tree differ on exactly these and on nothing
        /// else. Adding a name here needs the same evidence.
        /// </summary>
        private static readonly (string Frame, string Why)[] Excluded =
        {
            ("01a_selector_yielding", "a mid-envelope frame: the scrim's alpha is time-based, so the capture pins PRESENCE, not a pixel value"),
            ("89d_signing_entrance", "the same class - the document is mid-rise when this frame is taken"),
            ("92_saves_menu", "the harness's own staged saves print the real save MINUTE, so the sheet changes with the wall clock"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string a = Arg("-filma=", null);
            string b = Arg("-filmb=", null);
            string dir = Arg("-filmdir=", "../PoliSim-captures");

            var sb = new StringBuilder();
            sb.Append("=== S-21: the rule-15 film diff ===\n");

            if (a == null || b == null)
            {
                sb.Append("    self-test only (no -filma/-filmb given) - the exclusion list is:\n");
                foreach ((string frame, string why) in Excluded) { sb.Append("        ").Append(frame).Append("  -  ").Append(why).Append('\n'); }
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            if (!Directory.Exists(dir))
            {
                Debug.LogError($"FILMDIFF: no capture directory at '{dir}'. Reporting nothing rather than reporting clean.");
                CheckExit.Finish(1);
                return;
            }

            Dictionary<string, string> left = Collect(dir, a);
            Dictionary<string, string> right = Collect(dir, b);

            if (left.Count == 0 || right.Count == 0)
            {
                Debug.LogError($"FILMDIFF: '{a}' has {left.Count} frame(s) and '{b}' has {right.Count}. "
                               + "⚠ An empty set is not a clean diff - it is a comparison that did not happen.");
                CheckExit.Finish(1);
                return;
            }

            var excluded = new HashSet<string>(Excluded.Select(e => e.Frame), StringComparer.Ordinal);

            var onlyLeft = new List<string>();
            var onlyRight = new List<string>();
            foreach (string frame in left.Keys) { if (!right.ContainsKey(frame)) { onlyLeft.Add(frame); } }
            foreach (string frame in right.Keys) { if (!left.ContainsKey(frame)) { onlyRight.Add(frame); } }

            int identical = 0, differ = 0, skipped = 0;
            var changed = new List<string>();
            foreach (KeyValuePair<string, string> frame in left)
            {
                if (!right.TryGetValue(frame.Key, out string other)) { continue; }
                if (excluded.Contains(frame.Key)) { skipped++; continue; }

                if (string.Equals(frame.Value, other, StringComparison.Ordinal)) { identical++; }
                else { differ++; changed.Add(frame.Key); }
            }

            sb.Append(F("    '{0}' ({1} frames) vs '{2}' ({3} frames)\n", a, left.Count, b, right.Count));
            sb.Append(F("    {0} IDENTICAL, {1} DIFFER, {2} excluded by name.\n\n", identical, differ, skipped));

            foreach ((string frame, string why) in Excluded)
            {
                sb.Append("    excluded  ").Append(frame).Append("  -  ").Append(why).Append('\n');
            }

            if (changed.Count > 0)
            {
                sb.Append("\n    DIFFER — each of these is a real change to look at:\n");
                changed.Sort(StringComparer.Ordinal);
                foreach (string frame in changed) { sb.Append("        ").Append(frame).Append('\n'); }
            }

            int failures = 0;
            foreach (string frame in onlyLeft)
            {
                failures++;
                Debug.LogError($"FILMDIFF: '{frame}' is in '{a}' and MISSING from '{b}'. A frame present in one set and absent "
                               + "from the other is a capture that silently did not happen - which this project has already lost "
                               + "a run to. Failing rather than diffing what is left and calling it complete.");
            }

            foreach (string frame in onlyRight)
            {
                failures++;
                Debug.LogError($"FILMDIFF: '{frame}' is in '{b}' and MISSING from '{a}'. Same reason.");
            }

            sb.Append(F("\n    VERDICT: {0}\n", failures == 0
                ? "the rosters match, so the counts above are a comparison of the same set twice."
                : "⚠ ROSTER MISMATCH - see the errors above. The counts are NOT a like-for-like comparison."));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static Dictionary<string, string> Collect(string dir, string label)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            string prefix = label + "_";
            foreach (string path in Directory.GetFiles(dir, prefix + "*.png"))
            {
                string frame = Path.GetFileNameWithoutExtension(path).Substring(prefix.Length);
                map[frame] = Hash(path);
            }

            return map;
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream));
            }
        }

        private static string Arg(string prefix, string fallback)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal)) { return arg.Substring(prefix.Length); }
            }

            return fallback;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
