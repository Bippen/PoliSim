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
    /// **A delivered screen the player cannot reach is a FAILURE, not a curiosity.** (S-32, armed
    /// 2026-09-01.)
    ///
    /// <para><b>The instance.</b> `ElectionNightScreen` — board 1h — is built, filmed at four widths,
    /// recorded as delivered, and **named by nothing except `UiScreenshotDriver`**. It is in no scene and
    /// no prefab. ⚠ **The running game has no route to it**, and every guard stayed green for as long as
    /// it existed: containment and text-fitting check *what was drawn*, `ScreenEdgeCheck` checks *the
    /// pixels*, `CaptureIdentity` checks *that the frame shows its subject* — and not one of them asks
    /// whether a player could ever have got there.</para>
    ///
    /// <para><b>What it enumerates.</b> Every `.cs` under `Assets/Scripts` that calls
    /// <c>CanvasChrome.EnsureHost</c> — the one call that mounts a Canvas TAKEOVER, the screens that
    /// replace the whole frame. There are three, and the set is exactly right: the country selector, the
    /// signing ceremony and election night. ⚠ **The rule is deliberately narrow**: it is about takeovers,
    /// not about every UI helper, because a takeover is the only kind of screen whose unreachability is
    /// invisible from inside the game.</para>
    ///
    /// <para><b>The rule.</b> A takeover's type must be named in <c>GameController.cs</c> — **the only
    /// place a player path can begin.** A takeover named only by the capture driver is filmable and
    /// unplayable, which is the exact shape S-20 found from the other side.</para>
    ///
    /// <para>⚠ <b>WHAT IT CANNOT SEE</b>, said here rather than discovered later: a type named in
    /// `GameController` but on a branch no state can enter would pass. That is the same limit the sixth
    /// sweep records — a text scan proves a mention, not a reachable path — and the honest response is to
    /// name it rather than to imply more.</para>
    /// </summary>
    public static class PlayerReachabilityCheck
    {
        /// <summary>
        /// ✅ **A RATCHET at ZERO since 2026-09-01 (F1 step 4).** It stood at 1 for `ElectionNightScreen` — board 1h, built, filmed at four widths and recorded as delivered while nothing in the game could open it. ⚠ **The screen was never the problem**: its per-constituency numbers did not exist at runtime, because the data they come from lived outside `Assets/`. `GameController.ShowElectionNight` is the door, and it opens only when there is a real count to put behind it.
        ///
        /// <para>It is not wired in the commit that armed this check, and the reason is a dependency
        /// rather than a missing wire: **board 1h needs a per-constituency count, and the live election
        /// does not produce one.** `NationalElection.Run` takes national shares and allocates a chamber;
        /// the sourced per-valkrets file is staged only by a harness. The subsystem that WOULD produce a
        /// regional count is `RegionalVoteModel` — ⚠ **which is itself unreachable**, so the two unwired
        /// systems are each other's answer.</para>
        ///
        /// <para>**Lower it to 0 when board 1h has a route. Never raise it.** ⚠ And do not "fix" this by
        /// naming the type in `GameController` without a path — that would satisfy the scan and not the
        /// rule, which is the failure mode this whole audit exists for.</para>
        /// </summary>
        private const int UnreachableTakeoverCeiling = 0;

        private static readonly Regex PublicType = new Regex(
            @"^\s*public\s+(?:(?:sealed|abstract|static|partial|readonly|unsafe)\s+)*(?:class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)");

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            string controller = Path.Combine(scripts, "UI", "GameController.cs");
            if (!Directory.Exists(scripts) || !File.Exists(controller))
            {
                Debug.LogError("REACHABILITY: Assets/Scripts or GameController.cs is missing, so no player path can be "
                               + "read and this run verified NOTHING rather than finding nothing.");
                CheckExit.Finish(1);
                return;
            }

            string controllerText = SourceText.WithoutComments(File.ReadAllText(controller));   // a comment naming a takeover is not a route
            var takeovers = new List<string>();
            var unreachable = new List<string>();

            foreach (string path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                if (text.IndexOf("CanvasChrome.EnsureHost", StringComparison.Ordinal) < 0) { continue; }
                if (string.Equals(path, controller, StringComparison.OrdinalIgnoreCase)) { continue; }

                string name = null;
                foreach (string line in text.Split('\n'))
                {
                    Match m = PublicType.Match(line);
                    if (m.Success) { name = m.Groups[1].Value; break; }
                }

                if (name == null)
                {
                    Debug.LogError("REACHABILITY: " + Path.GetFileName(path) + " mounts a Canvas takeover and declares no "
                                   + "public type, so nothing can name it and its reachability cannot be judged.");
                    continue;
                }

                takeovers.Add(name);
                if (CountWord(controllerText, name) == 0) { unreachable.Add(name); }
            }

            takeovers.Sort(StringComparer.Ordinal);
            unreachable.Sort(StringComparer.Ordinal);

            // The enumeration rule. A run that found NO takeover has checked nothing, and would print
            // exactly like a run in which every takeover was reachable.
            if (takeovers.Count == 0)
            {
                Debug.LogError("REACHABILITY: not one Canvas takeover was found under Assets/Scripts. Either they are gone "
                               + "or the mounting call changed name - either way this run verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("=== A delivered screen the player cannot reach (S-32) ===\n");
            sb.Append(F("    THE ENUMERATION: every .cs under Assets/Scripts calling CanvasChrome.EnsureHost - the one call\n"
                        + "    that mounts a Canvas TAKEOVER. {0} found: {1}.\n",
                takeovers.Count, string.Join(", ", takeovers.ToArray())));
            sb.Append("    A takeover must be NAMED in GameController.cs, the only place a player path can begin. One named\n");
            sb.Append("    only by the capture driver is filmable and unplayable.\n\n");
            sb.Append(F("    {0} of {1} UNREACHABLE from any player path (ceiling {2}).\n",
                unreachable.Count, takeovers.Count, UnreachableTakeoverCeiling));
            foreach (string t in unreachable) { sb.Append("    GAP  ").Append(t).Append('\n'); }
            RatchetLedger.Report("PlayerReachabilityCheck.UNREACHABLE_TAKEOVER", unreachable.Count, UnreachableTakeoverCeiling);

            sb.Append("\n    ⚠ WHAT THIS CANNOT SEE: a type named in GameController but on a branch no state can enter would\n");
            sb.Append("    pass. A text scan proves a mention, not a reachable path - the sixth sweep's own limit, named here\n");
            sb.Append("    rather than implied away. ⚠ AND DO NOT SATISFY IT BY NAMING A TYPE WITHOUT A PATH: that would pass\n");
            sb.Append("    the scan and fail the rule, which is the failure mode this audit exists for.\n");

            if (unreachable.Count > UnreachableTakeoverCeiling)
            {
                Debug.LogError($"REACHABILITY: {unreachable.Count} Canvas takeover(s) are named nowhere in GameController - "
                               + string.Join(", ", unreachable.ToArray())
                               + ". A delivered screen the player cannot reach is a failure, not a curiosity: board 1h was "
                               + "built, filmed at four widths and recorded as delivered while the game had no route to it.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>Whole-word occurrences, so `SigningScreen` is not matched inside a longer identifier.</summary>
        private static int CountWord(string haystack, string word)
            => Regex.Matches(haystack, @"\b" + Regex.Escape(word) + @"\b").Count;

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
