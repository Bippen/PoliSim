using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **C-0.2's done-when, made a check instead of a read.**
    ///
    /// <para><b>The row already contained its own test.</b> C-0.2 read *"no live document asserts a
    /// pre-wiring premise"* and then, in brackets, gave the **grep**: `PartyArchetype`, `TotalSeats = 200`,
    /// *"not wired"*, *"unreachable from any gameplay path"*, *"VERIFIED NOTHING"*, *"no party seeds exist
    /// on main"*, *"UNINSPECTED"*. ⚠ **A done-when that is a grep is a check that nobody wrote**, and it
    /// was sized in the master list as a READ — the largest remaining SAFE item — because a read is what a
    /// grep becomes when a person has to run it.</para>
    ///
    /// <para>⚠ <b>And a read decays the moment it finishes.</b> Somebody re-reads every document, finds
    /// nothing, closes the row — and the next document written reintroduces the premise with nothing
    /// watching. **The one-off read cannot be the closure; it can only be the first run of the check.**</para>
    ///
    /// <para><b>History is kept on purpose, so it must not fail.</b> This repo's live documents are full of
    /// sentences RECORDING that a premise was once true, and those are correct — M-R3 turned on exactly that
    /// distinction. ⚠ **The past-tense rule is BORROWED from <see cref="CommentClaimCheck"/> rather than
    /// restated**: the same judgement stated twice is two things to keep true, and this project has
    /// catalogued that failure enough times to stop repeating it. A line asserting a stale premise fails; a
    /// line recording that it was once so is reported as HISTORY and passes.</para>
    ///
    /// <para>⚠ <b>WHAT IT CANNOT SEE.</b> A pre-wiring premise phrased in words not on the list. The terms
    /// come from C-0.2's own enumeration and are a NAMED SET, not a semantic test — the same soft spot
    /// `RatchetResidency` and `SharedMidpointCheck` both admit to. Stated here rather than discovered by
    /// somebody trusting a green run.</para>
    /// </summary>
    public static class PreWiringPremiseCheck
    {
        /// <summary>⚠ **C-0.2's own grep, verbatim.** Not re-derived, because re-deriving the list would
        /// make this check a claim about a DIFFERENT question than the row it closes.</summary>
        /// <summary>⚠ **A RATCHET, set from the FIRST measurement and never to zero.** C-0.2's done-when
        /// is *"no live document asserts a pre-wiring premise"* and the honest state of that was **14** on
        /// its first run and is **0** now — each of the fourteen a document sentence read and either
        /// corrected or re-tensed, in the same session that armed the check.
        ///
        /// <para>⚠ **Arming this at 0 would have been choosing a number for how it looks.** The check would
        /// have gone in red, or the fourteen would have been swept into the exclusion list to make it
        /// green — and an exclusion list used that way is the thing every other exclusion list in this repo
        /// is policed against. **The backlog is measured, printed by name every run, and can only fall.**</para>
        ///
        /// <para><b>Lower it as each is corrected; never raise it.</b></para></summary>
        private const int AssertedCeiling = 0;

        private static readonly string[] PreWiringPremises =
        {
            "PartyArchetype",
            "TotalSeats = 200",
            "not wired",
            "unreachable from any gameplay path",
            "VERIFIED NOTHING",
            "no party seeds exist on main",
            "UNINSPECTED",
        };

        /// <summary>Documents that are RECORDS rather than live claims. ⚠ Each is here because its whole
        /// purpose is to state what was true at a moment, and a check that failed them would be demanding
        /// the project forget its own history.</summary>
        private static readonly (string File, string Reason)[] HistoricalDocuments =
        {
            ("COMPLETED.md", "the record of finished work - every entry is a statement about a past state"),
            ("ELECTIONS_PROTOTYPE_LOG.md", "a LOG. Its entries are dated observations of what was true when "
             + "they were written, which is the same contract COMPLETED.md has; failing it would be asking "
             + "the project to go back and edit its own notebook."),
        };

        /// <summary>Whether every occurrence of <paramref name="term"/> on this line sits inside double
        /// quotes. ⚠ ALL of them, not any: a line that quotes the phrase once and asserts it once is
        /// asserting it, and taking the lenient reading there would be the check excusing itself.</summary>
        /// <summary>A backticked commit hash. ⚠ The line is then a record of what was true at that
        /// commit, not a claim about now.</summary>
        private static readonly System.Text.RegularExpressions.Regex CommitAnchored =
            new System.Text.RegularExpressions.Regex(@"`[0-9a-f]{7,40}`");

        /// <summary>A heading carrying an ISO date — the section is an account of that day.</summary>
        private static readonly System.Text.RegularExpressions.Regex DatedHeading =
            new System.Text.RegularExpressions.Regex(@"20[0-9]{2}-[01][0-9]-[0-3][0-9]");

        private static bool Quoted(string line, string term)
        {
            int from = 0;
            bool sawOne = false;
            while (true)
            {
                int at = line.IndexOf(term, from, StringComparison.Ordinal);
                if (at < 0) { return sawOne; }

                sawOne = true;
                int quotesBefore = 0;
                for (int i = 0; i < at; i++) { if (line[i] == '"') { quotesBefore++; } }
                if (quotesBefore % 2 == 0) { return false; }   // this one is outside quotes

                from = at + term.Length;
            }
        }

        /// <summary>
        /// ⚠ **Lines that CONTAIN the premises because they are ABOUT them** — self-reference: the register
        /// row whose done-when IS the grep. It is the same exclusion `MetaTextCheck` needs for its
        /// banned-pattern table: **a check that scans for words cannot be blind to the place those words
        /// have to be written down.** Keyed by a stable ANCHOR in the line itself, and policed.
        ///
        /// <para>⚠ <b>This list used to hold a LINE NUMBER, and it broke the first time a document was
        /// edited above it (2026-09-01).</b> The entry read <c>("POLISIM_BACKLOG.md", 947, …)</c>; a
        /// decision sheet was inserted forty-one lines higher, the row moved to 988, the exemption stopped
        /// matching and this check failed the bar on a row nobody had touched. **The check was carrying a
        /// transcribed fact about a document — the exact coupling the claim convention forbids, inside an
        /// instrument.**</para>
        ///
        /// <para><b>The anchor is the row's own id</b>, which is stable under every edit that does not
        /// rewrite the row. ⚠ **An anchor that matches nothing is a FAILURE, not a silent pass** — a stale
        /// exemption must announce itself rather than quietly stop exempting, which is the failure mode a
        /// line number had.</para>
        /// </summary>
        private static readonly (string File, string Anchor, string Reason)[] AboutTheTerms =
        {
            ("POLISIM_BACKLOG.md", "| C-0.2 |", "C-0.2's own row, whose done-when IS the grep - it has to spell the terms"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Directory.GetCurrentDirectory();
            var asserted = new List<string>();
            var history = new List<string>();
            var aboutHits = new List<string>();
            int scanned = 0;
            int linesRead = 0;

            var historical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in HistoricalDocuments) { historical.Add(h.File); }

            foreach (string path in Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(path);
                if (historical.Contains(name)) { continue; }

                scanned++;
                string[] lines = File.ReadAllLines(path);
                linesRead += lines.Length;

                // ⚠ A LINE UNDER A DATED HEADING IS A RECORD OF THAT DATE. `CLAUDE.md` is part standing
                // rules and part session log, and its log sections carry their date in the heading -
                // "The omnibus pass, Phase 1 - the chrome sweep (2026-08-28)". A sentence there is an
                // account of that day. **Editing it to satisfy a checker would be rewriting the log**, the
                // same fault the commit-anchor rule avoids, arriving through the heading instead.
                // ⚠ It is the HEADING that must be dated, not the line: this does not exempt a file, and a
                // standing rule under an undated heading is judged exactly as before.
                string sectionHeading = string.Empty;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("#", StringComparison.Ordinal)) { sectionHeading = lines[i]; }

                    foreach (string premise in PreWiringPremises)
                    {
                        if (lines[i].IndexOf(premise, StringComparison.Ordinal) < 0) { continue; }

                        string where = name + ":" + (i + 1) + "  " + premise;

                        // ⚠ MENTION IS NOT USE. A line carrying the phrase inside DOUBLE QUOTES is
                        // talking ABOUT it - `CLAUDE.md` documents `"VERIFIED NOTHING"` as a check's
                        // output string, and a standing rule quotes "built but not wired" as the thing a
                        // session must not leave behind. **Failing those would be asking the project to
                        // stop being able to name its own defects**, which is the opposite of what C-0.2
                        // is for.
                        if (Quoted(lines[i], premise)) { history.Add(where + "  (quoted - mention, not use)"); continue; }

                        bool aboutTheTerms = false;
                        foreach (var a in AboutTheTerms)
                        {
                            if (a.File == name && lines[i].IndexOf(a.Anchor, StringComparison.Ordinal) >= 0)
                            {
                                aboutTheTerms = true;
                                aboutHits.Add(a.File + " " + a.Anchor + " (line " + (i + 1) + ")");
                                break;
                            }
                        }

                        if (aboutTheTerms) { history.Add(where + "  (about the terms, not asserting them)"); continue; }

                        // ⚠ A LINE ANCHORED TO A COMMIT IS A STATEMENT ABOUT THAT COMMIT. `CLAUDE.md` is
                        // part standing rules and part running log, and its log entries open with the hash
                        // they describe - "`6c1483a` - AreaIconCoverageCheck (... every PartyArchetype
                        // emblem ...)". That sentence was true at `6c1483a` and rewriting it would be
                        // FALSIFYING A LOG, which is a worse fault than the stale reference it removes.
                        // Decidable, and narrower than exempting the file: only lines that name a hash.
                        if (CommitAnchored.IsMatch(lines[i])) { history.Add(where + "  (anchored to a commit)"); continue; }

                        if (DatedHeading.IsMatch(sectionHeading))
                        {
                            history.Add(where + "  (under a dated heading - a record of that date)");
                            continue;
                        }

                        // ⚠ PROSE WRAPS, AND A SENTENCE IS NOT A LINE. `ELECTIONS_GAP_TABLE.md` opens a
                        // paragraph "the migration RAN on 2026-08-30 ... What retired:" and the terms land
                        // on the NEXT line. Judging strictly per line would have demanded that a document
                        // repeat its own tense marker on every wrapped line - which is not writing, it is
                        // appeasing a checker. ⚠ ONE line of context, not a paragraph: a marker further
                        // away than that stops governing the sentence, and a wide window would let any
                        // nearby past tense excuse anything.
                        string context = i > 0 ? lines[i - 1] + " " + lines[i] : lines[i];
                        if (CommentClaimCheck.ReadsAsHistory(context)) { history.Add(where); }
                        else { asserted.Add(where); }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== C-0.2: no live document asserts a pre-wiring premise ===\n");
            sb.Append("    THE ENUMERATION: ").Append(scanned).Append(" live document(s) at the repository root, ")
              .Append(linesRead).Append(" line(s); ").Append(PreWiringPremises.Length)
              .Append(" premise term(s), taken VERBATIM from C-0.2's own done-when.\n");
            sb.Append("    ").Append(history.Count).Append(" occurrence(s) read as HISTORY and pass; ")
              .Append(asserted.Count).Append(" ASSERT a stale premise (ceiling ").Append(AssertedCeiling).Append(").\n\n");

            sb.Append("    HISTORICAL DOCUMENTS, skipped with their reason:\n");
            foreach (var h in HistoricalDocuments) { sb.Append("      ").Append(h.File).Append(" - ").Append(h.Reason).Append('\n'); }

            sb.Append("\n    ⚠ The past-tense rule is BORROWED from CommentClaimCheck, not restated: the same\n");
            sb.Append("    judgement written twice is two things to keep true. History is kept on purpose here and\n");
            sb.Append("    a line RECORDING that a premise was once so is correct; a line still ASSERTING it is not.\n");

            foreach (string h in history) { sb.Append("    history  ").Append(h).Append('\n'); }
            foreach (string a in asserted) { sb.Append("    ⚠ ASSERTS ").Append(a).Append('\n'); }

            sb.Append("\n    ⚠ WHAT THIS CANNOT SEE: a pre-wiring premise phrased in words not on the list. The terms\n");
            sb.Append("    are C-0.2's own enumeration - a NAMED SET, not a semantic test - which is the same soft\n");
            sb.Append("    spot RatchetResidency and SharedMidpointCheck each admit to.\n");

            // ⚠ THE EXCLUSION LIST, POLICED. An entry that matches nothing reads as coverage while covering
            // nothing, and outlives the line it named - the same clause SharedMidpointCheck and
            // PartyMarkCoverageCheck both carry, for the same reason.
            // ⚠ The key is now the ANCHOR, not a line number. The comment above was already right about
            // why — "a line number moves whenever the file above it is edited" — and the list used one
            // anyway until it broke on 2026-09-01. Policing it is still necessary: an anchor can go stale
            // too, by the row being renamed, and it must say so rather than quietly stop exempting.
            var deadExclusions = new List<string>();
            foreach (var a in AboutTheTerms)
            {
                bool hit = false;
                foreach (string h in aboutHits)
                {
                    if (h.StartsWith(a.File + " " + a.Anchor, StringComparison.Ordinal)) { hit = true; break; }
                }

                if (!hit) { deadExclusions.Add(a.File + " " + a.Anchor); }
            }

            foreach (string d in deadExclusions) { sb.Append("    ⚠ DEAD EXCLUSION  ").Append(d).Append('\n'); }

            if (deadExclusions.Count > 0)
            {
                Debug.LogError("PREWIRING: " + deadExclusions.Count + " exclusion(s) name a line that carries no "
                               + "premise term - " + string.Join(", ", deadExclusions.ToArray())
                               + ". A stale exclusion "
                               + "here is not a small fault: it silently stops covering the line it was written for "
                               + "AND starts excusing whatever moved into its place. Re-point it or delete it.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            // The enumeration rule. No documents means the tree moved and this verified nothing.
            if (scanned == 0)
            {
                Debug.LogError("PREWIRING: no live documents found at the repository root. This run verified NOTHING, "
                               + "which is not the same as finding nothing.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            RatchetLedger.Report("PreWiringPremiseCheck.ASSERTED", asserted.Count, AssertedCeiling);

            if (asserted.Count > AssertedCeiling)
            {
                Debug.LogError("PREWIRING: " + asserted.Count + " line(s) in live documents ASSERT a pre-wiring "
                               + "premise - " + string.Join(" | ", asserted.ToArray())
                               + ". ⚠ C-0.2's done-when was a GREP, which means it was a check nobody had written, "
                               + "and it was sized as a READ. A read decays the moment it finishes: the next document "
                               + "reintroduces the premise with nothing watching. Either put the sentence in the past "
                               + "tense, or correct it.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
