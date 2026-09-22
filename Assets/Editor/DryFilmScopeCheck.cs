using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §574 (2026-09-22, the efficiency pass's item 1) — **THE DRY FILM RUNS WHAT THE ITEM NAMES, AND THE BAR SAYS SO.**
    ///
    /// <para><b>The finding this exists for.</b> The tier tool has said since §524 that a UI item's dry film runs *"every width and
    /// country the item names"*. The sitting pass of 2026-09-22 ran ALL TWELVE sessions on every one of its ten items, and the
    /// efficiency review measured what that cost: **522 s a run against 145 s for the two sessions the item actually named - 6.3
    /// minutes an item, about an hour across the pass, the single biggest sink of the month.** A correct rule was not followed, and a
    /// rule nothing checks is a wish. So it is enforced the way the review ledger enforces its own: a ROW states what the item names,
    /// the check reads the FILM'S OWN LOG, and a film that ran a session its row does not name fails the bar.</para>
    ///
    /// <para><b>What it decides.</b> For every `scope` row in <c>Tools/film_scope.tsv</c>: the dry film's log must exist under
    /// <c>PoliSim-captures/logs/&lt;label&gt;.log</c>, and the runner's own plan line - <c>SHOT: DRY plan - N session(s): …</c>, printed
    /// by <see cref="UiScreenshotCapture.RunDry"/> before it films anything - must name EXACTLY the sessions the row does. Both
    /// directions fail: a session filmed and not named is the waste this check exists to stop; a session named and not filmed is a claim
    /// the film does not support.</para>
    ///
    /// <para>⚠ <b>What it deliberately does not do.</b> It does not decide whether the named scope is the RIGHT scope - that is the
    /// item's judgment and the row's `why` column is where it is written down. It does not read git, so it cannot fail a UI commit that
    /// wrote no row at all; <c>Tools/bar_tier.ps1</c> is what says REQUIRED before the commit, and the row is what this check holds to
    /// the film. A check that guessed which commit was a UI commit would be guessing at exactly the point this project needs certainty.
    /// A log that has been cleaned away is a missing row's equal: the row names a film that must be on disk to be read.</para>
    /// </summary>
    public static class DryFilmScopeCheck
    {
        public const string LedgerPath = "Tools/film_scope.tsv";

        /// <summary>The runner's own plan line: `SHOT: DRY plan - 12 session(s): Sweden@1280x720, Germany@1280x720, …`.</summary>
        private static readonly Regex PlanLine = new Regex(@"SHOT: DRY plan - \d+ session\(s\): ([^\r\n\.]+)");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string ledger = Path.Combine(projectRoot, LedgerPath);
            var sb = new StringBuilder();
            sb.Append("=== DryFilmScopeCheck: a dry film runs the sessions its item named, and no others ===\n");

            if (!File.Exists(ledger))
            {
                Debug.LogError($"DRY FILM SCOPE: no ledger at {LedgerPath} - the rule has nowhere to be written down.");
                CheckExit.Finish(1);
                return;
            }

            string logs = Path.GetFullPath(Path.Combine(projectRoot, "..", "PoliSim-captures", "logs"));
            int rows = 0, failures = 0, unread = 0;
            foreach (string raw in File.ReadAllLines(ledger))
            {
                if (raw.Length == 0 || raw[0] == '#') { continue; }

                string[] c = raw.Split('\t');
                if (c.Length < 6 || c[0] != "scope") { continue; }

                rows++;
                string label = c[1], declared = c[2], record = c[4];
                var want = new List<string>(declared.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                for (int i = 0; i < want.Count; i++) { want[i] = want[i].Trim(); }

                string log = Path.Combine(logs, label + ".log");
                if (!File.Exists(log))
                {
                    // A film whose log is gone cannot be held to its row. Reported, not failed: the captures directory is outside the
                    // repository and is cleaned by hand, and a row from a past pass would otherwise fail every bar for ever.
                    sb.Append($"  {label,-14} {record,-6} {want.Count} session(s) declared - NO LOG on disk, not read\n");
                    unread++;
                    continue;
                }

                Match m = PlanLine.Match(File.ReadAllText(log));
                if (!m.Success)
                {
                    sb.Append($"  {label,-14} {record,-6} ⚠ the log has no plan line - it is not a dry film's log\n");
                    failures++;
                    continue;
                }

                var got = new List<string>();
                foreach (string piece in m.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) { got.Add(piece.Trim()); }

                var extra = new List<string>();
                foreach (string g in got) { if (!want.Contains(g)) { extra.Add(g); } }

                var missing = new List<string>();
                foreach (string w in want) { if (!got.Contains(w)) { missing.Add(w); } }

                if (extra.Count == 0 && missing.Count == 0)
                {
                    sb.Append($"  {label,-14} {record,-6} {got.Count} session(s) filmed, exactly as named\n");
                    continue;
                }

                failures++;
                if (extra.Count > 0)
                {
                    sb.Append($"  {label,-14} {record,-6} ⚠ FILMED AND NOT NAMED: {string.Join(", ", extra)} - {extra.Count} session(s) of waste; name them in the row or do not film them\n");
                }

                if (missing.Count > 0)
                {
                    sb.Append($"  {label,-14} {record,-6} ⚠ NAMED AND NOT FILMED: {string.Join(", ", missing)} - the row claims a film that did not run\n");
                }
            }

            sb.Append($"\n  {rows} row(s): {rows - failures - unread} held, {failures} failed, {unread} whose log is no longer on disk.\n");
            sb.Append($"\n=== DryFilmScopeCheck: {(failures == 0 ? "CLEAN" : failures + " row(s) failed")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
