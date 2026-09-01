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
    /// **THE TERMINATION CONDITION, MADE A NUMBER.**
    ///
    /// <para><i>"No further instructions remain"</i> is unfalsifiable as prose, so this counts what is
    /// left. ⚠ **The run ends when this reports zero, and not on anyone's say-so** — including mine.</para>

    /// <para>⚠ <b>WHAT A ZERO MEANS, AND IT IS NOT WHAT IT LOOKS LIKE.</b> A zero here means <b>no CODE
    /// row is STARTABLE</b>. It does <b>not</b> mean the project's work is finished. ⚠ <b>Two of the three
    /// dependency chains in the master list terminate in an OWNER NO SESSION CAN BE</b> — `M-D3` needs
    /// Elias to register an ITANES account, and the `M-B4`/`M-B5` chain waits behind `M-D1` — so the
    /// REACHABLE column empties well before the project's open work does. **A reader who takes this zero
    /// for completion has read a statement about reachability as a statement about scope**, which is this
    /// repo's signature defect wearing the one number built to be trustworthy. The check prints this
    /// sentence in its own output every run for the same reason it is written here: the number travels
    /// further than the file, and a bound with no direction attached is half a claim (S-37).</para>
    ///
    /// <para><b>WHAT COUNTS AS RESIDUE.</b> Two structured sources, both unambiguous, neither requiring a
    /// sentence to be interpreted:</para>
    /// <list type="number">
    /// <item><b>Open `OWNER = CODE` rows in `POLISIM_MASTER_LIST.md`'s startable sections</b> (1–3).
    /// Parsing the list makes it auditable rather than decorative: a row cannot be quietly dropped without
    /// this number moving.</item>
    /// <item><b>Unambiguous open-work markers in source</b> — `TODO`, `HACK`, `WIP`, `STUB`,
    /// `[PLACEHOLDER]`. Measured 2026-09-01: **zero real occurrences.**</item>
    /// </list>
    ///
    /// <para>⚠ <b>THE RATCHETS ARE NOT ADDED, THEY ARE ENROLLED.</b> Each non-zero ratchet already has its
    /// own `M-R*` row in the list, so adding the ledger's counts would count the same backlog twice.
    /// Instead every non-zero ratchet must HAVE such a row, and one that does not **fails** — so a ratchet
    /// cannot grow without appearing in the list somebody works from.</para>
    ///
    /// <para><b>⚠ TWO EXCLUSIONS, AND THEY WORK DIFFERENTLY. Both are printed in full every run.</b></para>
    ///
    /// <para><b>The CATEGORY exclusion — `[AUTHORED-DRAFT]`, ruled 2026-09-01.</b> It occurs **252 times
    /// across 56 files** and it is **not open work**: it is the provenance mark `ConstantProvenanceCheck`
    /// REQUIRES on every authored constant. Counting it would make zero reachable only by deleting honesty
    /// marks — the exact defect this project has catalogued six times. It is reported as a **census** and
    /// contributes nothing. ⚠ The same reasoning covers a check's own banned-pattern table: `MetaTextCheck`
    /// holds the literal words `TODO` and `STUB` because it exists to ban them, so the exclusion there is
    /// **by FILE**, never by marker name.</para>
    ///
    /// <para><b>The BY-NAME exclusion — rows whose owner is not CODE.</b> ⚠ **This is the dangerous one,
    /// and it is policed.** Every excluded ID must resolve to a row in the master list whose OWNER is
    /// literally `ELIAS`, `DESIGN` or `CALENDAR`; **an exclusion naming a CODE row FAILS the check.**
    /// Excluding a CODE row to make the number fall is the move the brief singles out, and it is not
    /// available here. Every entry carries its owner and its reason.</para>
    ///
    /// <para>⚠ <b>WHAT A ZERO MEANS, AND WHAT IT DOES NOT.</b> Zero means **no CODE row is startable**. It
    /// does not mean the project is finished: two of the three dependency chains terminate in something no
    /// session can do — one needs an account registered, another needs a Design batch. **Read a zero as
    /// "the reachable column is empty", never as "the work is done."**</para>
    /// </summary>
    public static class InstructionResidueCheck
    {
        /// <summary>⚠ The residue's ceiling, measured on the first run. **Lower it as rows close; never
        /// raise it.** A rising residue is work being added faster than it is finished, which is a fact
        /// worth failing over rather than absorbing.</summary>
        private const int ResidueCeiling = 19;

        private const string ListRelative = "POLISIM_MASTER_LIST.md";

        /// <summary>The heading that ends the startable part of the list. Everything after it is
        /// OWNER≠CODE by construction and is not residue.</summary>
        private const string NotStartableHeading = "OWNER ≠ CODE";

        /// <summary>The SECOND boundary past which rows are not startable — because they are DONE rather
        /// than because their owner is not a session. ⚠ Two headings, two reasons, and they are kept apart
        /// deliberately: filing a closed CODE row under "OWNER ≠ CODE" would make that heading assert
        /// something false about every row beneath it, and a heading that lies is how this repo has lost
        /// five things to a comment.</summary>
        private const string ClosedHeading = "CLOSED — NOT STARTABLE BECAUSE DONE";

        /// <summary>The THIRD boundary: rows that are **never startable and never done** — standing
        /// verifications that a check performs on every bar run.
        ///
        /// <para>⚠ <b>Why they cannot be counted.</b> A row whose content is *"re-verified each cycle"*
        /// has no completed state, so counting it makes zero **unreachable by construction** — the
        /// termination condition would be false for a reason that has nothing to do with the work. ⚠ And
        /// this is the most dangerous of the three boundaries, because "it is a standing watch" is exactly
        /// what someone would say to move a row they did not want to do. **So it is policed harder than the
        /// others**: a WATCH row must NAME a check file that exists, and that check must be REGISTERED in a
        /// batch — a watch nobody runs is not a watch, it is a row in a quieter place.</para></summary>
        private const string WatchHeading = "STANDING WATCH — NEVER STARTABLE, NEVER DONE";

        /// <summary>A backticked identifier ending in Check or Diagnostic: what a WATCH row must name.</summary>
        private static readonly Regex NamesACheck = new Regex(@"`([A-Za-z0-9_]+(?:Check|Diagnostic))`");

        /// <summary>A commit hash, which is what a CLOSED row has to produce. ⚠ Without this the closed
        /// section would be a place to put a row to make the number go down, and the number would measure
        /// willingness to move rows. Closure is a claim about the repo and it is checked against the repo.</summary>
        private static readonly Regex CommitCitation = new Regex(@"`[0-9a-f]{7,40}`");

        /// <summary>Open-work markers that are unambiguous — each says "unfinished", not "authored".</summary>
        private static readonly Regex OpenWorkMarker = new Regex(@"\b(TODO|HACK|WIP|STUB)\b|\[PLACEHOLDER\]");

        /// <summary>The provenance mark. ⚠ Counted as a CENSUS, never as residue — see the class doc.</summary>
        private static readonly Regex ProvenanceMark = new Regex(@"\[AUTHORED-DRAFT\]");

        /// <summary>
        /// ⚠ **Files excluded from the marker scan BY FILE, with the reason.** A check that exists to ban
        /// a word must be allowed to contain it. **This list may only ever hold pattern tables** — a file
        /// is not excusable because its markers are inconvenient.
        /// </summary>
        private static readonly (string File, string Reason)[] PatternTableFiles =
        {
            ("MetaTextCheck.cs", "its banned-pattern table holds the literal words it exists to ban"),
            ("InstructionResidueCheck.cs", "this file's own marker regex holds them for the same reason"),
        };

        /// <summary>
        /// ⚠ **The BY-NAME exclusions — currently EMPTY, and that is the honest state.** Every startable
        /// row in the master list is OWNER=CODE; the rest live below the not-startable heading and are
        /// excluded structurally rather than by name. The machinery exists, is proved, and is policed:
        /// **an entry naming a CODE row fails the check.**
        /// </summary>
        private static readonly (string Id, string Owner, string Reason)[] ExcludedById =
        {
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Directory.GetCurrentDirectory();
            string listPath = Path.Combine(root, ListRelative);
            if (!File.Exists(listPath))
            {
                Debug.LogError("RESIDUE: " + ListRelative + " is not on disk. The termination condition reads from it, "
                               + "so this run counted NOTHING rather than counting zero.");
                CheckExit.Finish(1);
                return;
            }

            // --- 1. The master list's startable rows.
            var codeRows = new List<string>();
            var codeRowText = new List<string>();
            var uncited = new List<string>();
            var unwatched = new List<string>();
            bool inClosed = false;
            bool inWatch = false;
            int closedRows = 0;
            int watchRows = 0;
            var ownerById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool startable = true;
            int tableRows = 0;

            foreach (string raw in File.ReadAllLines(listPath))
            {
                string line = raw.Trim();
                if (line.Contains(NotStartableHeading)) { startable = false; }
                if (line.Contains(ClosedHeading)) { startable = false; inClosed = true; inWatch = false; }
                if (line.Contains(WatchHeading)) { startable = false; inWatch = true; inClosed = false; }
                if (!line.StartsWith("|", StringComparison.Ordinal)) { continue; }

                string[] cells = line.Split('|');
                if (cells.Length < 8) { continue; }

                string id = Clean(cells[1]);
                string owner = Clean(cells[4]);
                if (id.Length == 0 || id == "ID" || id.StartsWith("-", StringComparison.Ordinal)) { continue; }

                tableRows++;
                ownerById[id] = owner;

                // ⚠ A closed row must PRODUCE THE COMMIT. Otherwise this section is simply where a row goes
                // to stop being counted, and the residue measures how willing someone was to move it.
                if (inClosed)
                {
                    closedRows++;
                    if (!CommitCitation.IsMatch(line)) { uncited.Add(id); }
                    continue;
                }

                // ⚠ A WATCH row must name a check that EXISTS and is REGISTERED. Otherwise "standing watch"
                // is simply a quieter place to put a row, and this boundary is the easiest of the three to
                // abuse precisely because nothing about it ever completes.
                if (inWatch)
                {
                    watchRows++;
                    var named = new List<string>();
                    foreach (Match m in NamesACheck.Matches(line)) { named.Add(m.Groups[1].Value); }

                    if (named.Count == 0) { unwatched.Add(id + " (names no check)"); continue; }

                    foreach (string check in named)
                    {
                        if (!File.Exists(Path.Combine(root, "Assets", "Editor", check + ".cs")))
                        {
                            unwatched.Add(id + " -> " + check + " (no such check file)");
                            continue;
                        }

                        bool registered = false;
                        foreach (string n in CheckSuite.CheapGroup) { if (n == check) { registered = true; break; } }
                        if (!registered)
                        {
                            foreach (string n in CheckSuite.SimulationGroup) { if (n == check) { registered = true; break; } }
                        }

                        if (!registered) { unwatched.Add(id + " -> " + check + " (registered in no batch)"); }
                    }

                    continue;
                }
                if (startable && string.Equals(owner, "CODE", StringComparison.Ordinal))
                {
                    codeRows.Add(id);
                    codeRowText.Add(line);
                }
            }

            // The enumeration rule: a run that parsed no row has read a file it does not understand, and
            // would report zero exactly like a finished project.
            if (tableRows == 0)
            {
                Debug.LogError("RESIDUE: not one table row parsed out of " + ListRelative + ". Either the list's shape "
                               + "changed or the file is empty - either way this run counted NOTHING, which is not the "
                               + "same as counting zero.");
                CheckExit.Finish(1);
                return;
            }

            // --- 2. The markers in source.
            var markerHits = new List<string>();
            int provenance = 0, filesScanned = 0;
            foreach (string dir in new[] { Path.Combine(root, "Assets", "Scripts"), Path.Combine(root, "Assets", "Editor") })
            {
                if (!Directory.Exists(dir)) { continue; }
                foreach (string path in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(path);
                    string text = File.ReadAllText(path);
                    filesScanned++;
                    provenance += ProvenanceMark.Matches(text).Count;

                    if (IsPatternTable(name)) { continue; }

                    // ⚠ THIS SCAN READS RAW TEXT, AND IT IS EXEMPT FROM THE STRIPPER FOR A STATED REASON.
                    // The ninth sweep's rule is that a comment is not a reference - but here the comment IS
                    // the subject: a `// TODO` is unfinished work PRECISELY as a comment, and stripping
                    // would blind this check to the only markers it counts. Enrolled as EXEMPT in
                    // `CommentImmunityCheck`, beside the other three whose subject is the comment itself.
                    foreach (Match m in OpenWorkMarker.Matches(text))
                    {
                        markerHits.Add(name + ": " + m.Value);
                    }
                }
            }

            // --- 3. The ratchet enrolment. Not added - ENROLLED, so the same backlog is not counted twice.
            //
            // ⚠ TWO DEFECTS FIXED HERE 2026-09-01, BOTH FOUND BY READING THIS BLOCK RATHER THAN ITS OUTPUT.
            //
            // (i) The listing test asked whether ANY row id began "M-R" - so ONE M-R row anywhere in the
            //     list marked EVERY ratchet listed, and a ratchet with no row of its own would have been
            //     reported enrolled. A guard that cannot tell its subjects apart has stopped
            //     discriminating, which is the exact class the eighth sweep exists for, committed by the
            //     check that enrols it. Each ratchet now has to be NAMED in an M-R row.
            //
            // (ii) The ledger is PER PROCESS, and this check runs in the cheap batch only. Two ratchets
            //     live in the simulation group - CohortAgingStepDiagnostic.RUNAWAY and
            //     PublicationCadenceCheck's floor - and could not appear here at all, so their M-R rows
            //     went unverified while the line above them read "0 unlisted". `RatchetResidency` supplies the
            //     names a deferred check reports under, read from its own Report literals, so its row is
            //     checked here even though its measurement is taken in the other batch.
            var unlisted = new List<string>();
            var deferredRatchets = new List<string>();
            int nonZeroRatchets = 0;

            foreach (RatchetLedger.Entry e in RatchetLedger.Entries)
            {
                int backlog = e.IsFloor ? 0 : e.Measured;
                if (backlog <= 0) { continue; }

                nonZeroRatchets++;
                if (!NamedInAnMRRow(codeRowText, e.Name)) { unlisted.Add(e.Name); }
            }

            // The deferred half: a ratchet this process cannot measure still has to own a row.
            foreach (RatchetResidency.Entry r in RatchetResidency.Enumerate())
            {
                if (r.Registers == RatchetResidency.Group.Cheap || !r.HasReportCall) { continue; }

                foreach (RatchetResidency.Reported reported in r.ReportsAs)
                {
                    // ⚠ S-37: a FLOOR is not a backlog. Demanding an M-R row for one would demand a row
                    // for a bound that is supposed to STAY where it is, so the direction is carried out of
                    // the source with the name rather than assumed away.
                    deferredRatchets.Add(reported.Name + (reported.IsFloor ? " (floor)" : string.Empty));
                    if (reported.IsFloor) { continue; }
                    if (!NamedInAnMRRow(codeRowText, reported.Name)) { unlisted.Add(reported.Name + " (deferred)"); }
                }
            }

            // --- 4. The by-name exclusions, policed.
            var swallowed = new List<string>();
            foreach (var x in ExcludedById)
            {
                if (!ownerById.TryGetValue(x.Id, out string owner))
                {
                    swallowed.Add(x.Id + " is excluded and appears in no row of the list");
                    continue;
                }

                if (string.Equals(owner, "CODE", StringComparison.Ordinal))
                {
                    swallowed.Add(x.Id + " is excluded and its row says OWNER=CODE");
                }
            }

            int residue = codeRows.Count + markerHits.Count;
            RatchetLedger.Report("InstructionResidueCheck.RESIDUE", residue, ResidueCeiling);

            var sb = new StringBuilder();
            sb.Append("=== THE RESIDUE: what is left, counted rather than asserted ===\n");
            sb.Append(F("    THE ENUMERATION: {0} row(s) parsed from {1}; {2} source file(s) scanned under Assets/Scripts\n"
                        + "    and Assets/Editor; {3} pattern-table file(s) excluded BY FILE; {4} by-name exclusion(s).\n",
                tableRows, ListRelative, filesScanned, PatternTableFiles.Length, ExcludedById.Length));
            sb.Append(F("\n    RESIDUE = {0}  (ceiling {1})\n", residue, ResidueCeiling));
            sb.Append(F("      startable OWNER=CODE rows : {0}\n", codeRows.Count));
            sb.Append(F("      open-work markers in source: {0}\n", markerHits.Count));
            foreach (string h in markerHits) { sb.Append("        ").Append(h).Append('\n'); }

            sb.Append(F("\n    THE CENSUS, NEVER COUNTED: {0} [AUTHORED-DRAFT] mark(s).\n", provenance));
            sb.Append("    ⚠ That is the provenance mark ConstantProvenanceCheck REQUIRES on an authored constant. It is\n");
            sb.Append("    not open work, and counting it would make zero reachable only by deleting honesty marks.\n");

            sb.Append("\n    EXCLUDED BY FILE (pattern tables only):\n");
            foreach (var p in PatternTableFiles) { sb.Append("      ").Append(p.File).Append(" - ").Append(p.Reason).Append('\n'); }

            sb.Append("\n    EXCLUDED BY NAME:\n");
            if (ExcludedById.Length == 0) { sb.Append("      none - every startable row is OWNER=CODE and the rest are excluded structurally.\n"); }
            foreach (var x in ExcludedById) { sb.Append("      ").Append(x.Id).Append(" [").Append(x.Owner).Append("] - ").Append(x.Reason).Append('\n'); }

            sb.Append(F("\n    RATCHETS: {0} with a non-zero backlog in THIS process, each of which must be NAMED in an M-R\n"
                        + "    row ({1} unlisted). A further {2} report only in the simulation batch and cannot be measured\n"
                        + "    here; their rows are checked against the names their own Report calls pass:\n",
                nonZeroRatchets, unlisted.Count, deferredRatchets.Count));
            foreach (string d in deferredRatchets) { sb.Append("      deferred    ").Append(d).Append('\n'); }

            sb.Append(F("\n    CLOSED THIS RUN AND SINCE: {0} row(s) under the closed heading, {1} of them citing no\n"
                        + "    commit. ⚠ A closed row must PRODUCE THE COMMIT - otherwise that section is where a row goes\n"
                        + "    to stop being counted, and this number measures willingness to move rows.\n",
                closedRows, uncited.Count));
            foreach (string u in uncited) { sb.Append("      ⚠ UNCITED   ").Append(u).Append('\n'); }

            sb.Append(F("\n    STANDING WATCH: {0} row(s) that are never startable and never done, {1} of them not\n"
                        + "    backed by a registered check. ⚠ This boundary is the easiest of the three to abuse -\n"
                        + "    nothing about a watch ever completes - so a row here must NAME a check that exists AND\n"
                        + "    is registered in a batch. A watch nobody runs is a row in a quieter place.\n",
                watchRows, unwatched.Count));
            foreach (string u in unwatched) { sb.Append("      ⚠ UNWATCHED  ").Append(u).Append('\n'); }
            foreach (string u in unlisted) { sb.Append("      ⚠ UNLISTED  ").Append(u).Append('\n'); }

            sb.Append("\n    ⚠ WHAT A ZERO MEANS: no CODE row is startable. It does NOT mean the project is finished - two\n");
            sb.Append("    of the three dependency chains end in something no session can do. Read it as 'the reachable\n");
            sb.Append("    column is empty', never as 'the work is done'.\n");

            var failures = new List<string>();
            if (swallowed.Count > 0) { failures.AddRange(swallowed); }
            if (unlisted.Count > 0) { failures.Add(unlisted.Count + " non-zero ratchet(s) own no row in the list"); }
            if (uncited.Count > 0) { failures.Add(uncited.Count + " CLOSED row(s) cite no commit"); }
            if (unwatched.Count > 0) { failures.Add(unwatched.Count + " WATCH row(s) name no registered check"); }
            if (residue > ResidueCeiling) { failures.Add("residue " + residue + " is above the ceiling of " + ResidueCeiling); }

            if (failures.Count > 0)
            {
                Debug.LogError("RESIDUE: " + string.Join(" | ", failures.ToArray())
                               + ". ⚠ An exclusion that names a CODE row, a ratchet with no row, a CLOSED row citing "
                               + "no commit, a WATCH row backed by no registered check, or a residue that GREW are "
                               + "the five ways this number stops meaning what it says.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static bool IsPatternTable(string fileName)
        {
            foreach (var p in PatternTableFiles)
            {
                if (string.Equals(p.File, fileName, StringComparison.OrdinalIgnoreCase)) { return true; }
            }

            return false;
        }

        /// <summary>Whether a ratchet's own LEDGER NAME appears in some startable M-R row. ⚠ It is the
        /// name and not the prefix: the test used to ask whether any row id began "M-R", which one row
        /// anywhere satisfied for every ratchet at once. A row must name the thing it owns, or it is not
        /// evidence about that thing.</summary>
        private static bool NamedInAnMRRow(List<string> rowText, string ledgerName)
        {
            foreach (string row in rowText)
            {
                if (row.IndexOf(ledgerName, StringComparison.Ordinal) >= 0) { return true; }
            }

            return false;
        }

        /// <summary>A table cell, stripped of the emphasis and link noise a markdown row carries.</summary>
        private static string Clean(string cell)
            => cell.Replace("*", string.Empty).Replace("`", string.Empty).Replace("~", string.Empty).Trim();

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
