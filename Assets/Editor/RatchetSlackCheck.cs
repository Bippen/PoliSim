using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The coherence audit's **EIGHTH sweep — a claim whose evidence cannot discriminate.**
    ///
    /// <para><b>Why this shape, and not a general one.</b> The class now has six recorded instances —
    /// C-C2's trajectory diff, S-20's void films, assertion 4 reporting itself untested, the Ramey basis
    /// nobody published, the CRLF glance that would have failed a correct paste, and
    /// `PublicationCadenceCheck`'s `Finish(0)`-only exit. **Six is not a class, it is this project's
    /// signature defect.** The sixth sweep took the narrowest decidable slice — *a registered check must
    /// be able to fail at all.* ⚠ **This one takes the next slice: a check that CAN fail, but not until
    /// things get much worse than they are.**</para>
    ///
    /// <para><b>The rule.</b> Every ratchet in this repo carries the same instruction in its own doc —
    /// *lower it as the backlog clears, never raise it.* ⚠ **A ceiling standing ABOVE its own measurement
    /// is SLACK**: the check prints green, and the thing it guards can get worse by the size of the gap
    /// before anything fires. **That is a guard whose evidence has stopped discriminating**, and until now
    /// the instruction was enforced by nobody but memory — the mechanism this project has recorded twice
    /// as failing.</para>
    ///
    /// <para><b>How it knows.</b> <see cref="RatchetLedger"/>. Each ratchet reports **its own** measured
    /// count beside **its own** ceiling, next to the comparison it already makes. ⚠ **Nothing is
    /// re-derived here** — a second measurement of the same thing would be a second thing to keep true,
    /// and the first sweep to do that would be committing the class it audits.</para>
    ///
    /// <para>⚠ <b>It is ORDER-DEPENDENT, and that is load-bearing.</b> `CheckSuite.RunAllBatch` runs every
    /// check in one process and this one is registered LAST, so the ledger holds what the others reported.
    /// **Run it alone and the ledger is empty — which it treats as a FAILURE**, because a slack audit that
    /// audited nothing looks exactly like one that found no slack. That is the enumeration rule applied to
    /// a check whose input is other checks.</para>
    ///
    /// <para>⚠ <b>WHAT IT CAN SEE, AND HOW THAT GREW TWICE.</b> It began by SAYING an unreporting ratchet
    /// was invisible to it; §161 made that a failing condition instead. ⚠ **And on 2026-09-01 the
    /// enrolment's own honesty was found to be the next hiding place**: it asked whether a
    /// `RatchetLedger.Report` call was WRITTEN, printed `0 unreported`, and two ratchets — one of them a
    /// FLOOR — were reporting into the simulation batch's ledger, which no audit read. **A written call
    /// is not an executed one.** <see cref="RatchetResidency"/> now supplies which batch registers each
    /// declaring check, this audit runs at the end of BOTH batches, and four states are separated:
    /// UNREPORTED, UNREGISTERED, SILENT (registered here, in source, absent from the ledger) and DEFERRED
    /// (named, with the batch that audits it). The coverage is printed rather than implied, and the one
    /// remaining soft spot is stated on <see cref="RatchetResidency"/>: a bound not named `*Ceiling` or
    /// `*Ratchet` escapes the enrolment entirely.</para>
    /// </summary>
    public static class RatchetSlackCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            IReadOnlyList<RatchetLedger.Entry> entries = RatchetLedger.Entries;

            // The enumeration rule, and here it is the whole check: an empty ledger is a run that audited
            // nothing, and its clean summary would be indistinguishable from a repo with no slack in it.
            if (entries.Count == 0)
            {
                Debug.LogError("SLACK: the ratchet ledger is EMPTY. This check reads what other checks reported in the same "
                               + "process, so it is registered last in `CheckSuite` and cannot be run alone. An empty ledger "
                               + "means this run audited NOTHING - which is not the same as finding no slack.");
                CheckExit.Finish(1);
                return;
            }

            // ⚠ THE HOLE, CLOSED BY ENROLMENT (2026-09-01). This check used to end by SAYING that a
            // ratchet which does not report is invisible to it. **A named hole is still a hole**, and the
            // ninth sweep had the same shape - two instances make a class. So: every check file declaring
            // a ceiling or ratchet constant must call `RatchetLedger.Report`, and one that does not FAILS
            // rather than sitting outside the audit in silence.
            // ⚠ AND THE HOLE THAT ENROLMENT LEFT, CLOSED 2026-09-01. The scan above used to ask one
            // question - does this file's source contain a RatchetLedger.Report call? - and report "0
            // unreported" on the strength of it. **A written call is not an executed one.**
            // CohortAgingStepDiagnostic and PublicationCadenceCheck both passed that question while
            // reporting into the SIMULATION batch's ledger, which no audit ever read: two ratchets, one of
            // them a FLOOR, sitting outside the audit with the audit printing green. That is §161's own
            // class one level up, and the honest-looking number is what hid it.
            //
            // So the enrolment now asks THREE questions of every declaring file, via RatchetResidency,
            // which reads the registration TABLES rather than scanning for them:
            //   1. does it report at all?                        -> UNREPORTED (the original clause)
            //   2. is it registered in any batch?                -> UNREGISTERED - armed for nobody
            //   3. is it in the RUNNING batch and yet absent
            //      from this run's ledger?                       -> SILENT - the call did not execute
            // A declaring check registered in the OTHER batch is neither a pass nor a fault: it is named
            // as DEFERRED, with the batch that audits it, so the coverage below stops reading as complete
            // when it is not.
            var unreported = new List<string>();
            var unregistered = new List<string>();
            var silent = new List<string>();
            var deferred = new List<string>();
            int ratchetFiles = 0;

            var reportedNames = new HashSet<string>();
            foreach (RatchetLedger.Entry e in entries)
            {
                int dot = e.Name.IndexOf('.');
                reportedNames.Add(dot > 0 ? e.Name.Substring(0, dot) : e.Name);
            }

            foreach (RatchetResidency.Entry r in RatchetResidency.Enumerate())
            {
                ratchetFiles++;

                if (!r.HasReportCall) { unreported.Add(r.Check); continue; }

                if (r.Registers == RatchetResidency.Group.None)
                {
                    unregistered.Add(r.Check);
                    continue;
                }

                if (r.Registers != RatchetResidency.ActiveGroup)
                {
                    deferred.Add(F("{0} - audited in the {1} batch", r.Check, r.Registers));
                    continue;
                }

                if (!reportedNames.Contains(r.Check)) { silent.Add(r.Check); }
            }

            var slack = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (h): a ratchet whose ceiling has stopped discriminating ===\n");
            sb.Append(F("    THE ENUMERATION: {0} ratchet(s) reported to the ledger this run. A ratchet that does not report\n"
                        + "    is INVISIBLE here - the coverage is the list below and nothing else.\n\n", entries.Count));
            sb.Append("    ratchet                                          measured   ceiling   verdict\n");
            sb.Append("    ---------------------------------------------------------------------------\n");

            foreach (RatchetLedger.Entry e in entries)
            {
                // ⚠ A FLOOR ratchet is the mirror: its measurement must not fall BELOW the bound, so slack
                // is the measurement standing ABOVE it. Carrying the direction is what stops "tight"
                // meaning the opposite of what it says on half the ledger.
                int over = e.IsFloor ? e.Ceiling - e.Measured : e.Measured - e.Ceiling;
                int gap = e.IsFloor ? e.Measured - e.Ceiling : e.Ceiling - e.Measured;

                string verdict;
                if (over > 0)
                {
                    // Not this check's business: the owning check has already failed for it.
                    verdict = "OVER - its own check fails";
                }
                else if (gap > 0)
                {
                    verdict = "⚠ SLACK by " + gap;
                    slack.Add(F("{0}: measured {1}, bound {2}{3} - slack by {4}",
                        e.Name, e.Measured, e.Ceiling, e.IsFloor ? " (floor)" : string.Empty, gap));
                }
                else
                {
                    verdict = "tight";
                }

                sb.Append(F("    {0,-46} {1,8} {2,9}{3}   {4}\n",
                    e.Name, e.Measured, e.Ceiling, e.IsFloor ? " floor" : "      ", verdict));
            }

            sb.Append("\n    ⚠ SLACK IS NOT A COSMETIC FAULT. A ceiling above its measurement lets the thing it guards get\n");
            sb.Append("    worse by the size of the gap while the check keeps printing green - a guard whose evidence has\n");
            sb.Append("    stopped discriminating, which is the class this sweep exists for. Every ratchet's own doc already\n");
            sb.Append("    says to lower it as the backlog clears; until now nothing but memory enforced that.\n");
            sb.Append(F("\n    THE ENROLMENT: {0} check file(s) declare a ceiling or ratchet constant. Of those: {1} never call\n"
                        + "    Report, {2} are registered in NO batch, {3} are registered in the {4} batch and stayed SILENT\n"
                        + "    this run, and {5} are DEFERRED to the other batch, which audits them there.\n"
                        + "    ⚠ A ratchet whose Report call is WRITTEN but never EXECUTED reads exactly like one that reported.\n",
                ratchetFiles, unreported.Count, unregistered.Count, silent.Count,
                RatchetResidency.ActiveGroup, deferred.Count));
            foreach (string u in unreported) { sb.Append("    ⚠ UNREPORTED   ").Append(u).Append('\n'); }
            foreach (string u in unregistered) { sb.Append("    ⚠ UNREGISTERED ").Append(u).Append('\n'); }
            foreach (string u in silent) { sb.Append("    ⚠ SILENT       ").Append(u).Append('\n'); }
            foreach (string d in deferred) { sb.Append("      deferred     ").Append(d).Append('\n'); }

            sb.Append("    THE FIX IS ALWAYS THE SAME: lower the ceiling to what was measured. NEVER raise a measurement to\n");
            sb.Append("    meet a ceiling, and never widen a ceiling to silence this.\n");

            if (unregistered.Count > 0)
            {
                Debug.LogError("SLACK: " + unregistered.Count + " check(s) declare a ratchet and are registered in NO batch - "
                               + string.Join(", ", unregistered.ToArray())
                               + ". ⚠ A ratchet armed for a group that never runs is armed for nobody: it can neither fail "
                               + "nor be audited, and its ceiling is a number in a file. Register the check in a batch or "
                               + "delete the bound.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (silent.Count > 0)
            {
                Debug.LogError("SLACK: " + silent.Count + " check(s) are registered in the running batch, have a Report call "
                               + "in source, and did not appear in this run's ledger - " + string.Join(", ", silent.ToArray())
                               + ". ⚠ THE CALL WAS WRITTEN AND DID NOT EXECUTE, which is the difference between a source scan "
                               + "and a measurement - and until 2026-09-01 this audit could not tell them apart.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (unreported.Count > 0)
            {
                Debug.LogError("SLACK: " + unreported.Count + " check(s) declare a ratchet and never report it - "
                               + string.Join(", ", unreported.ToArray())
                               + ". ⚠ A ratchet outside the ledger is outside the audit: nothing compares its ceiling to "
                               + "its measurement, which is the exact condition this sweep exists to make impossible. Add "
                               + "a `RatchetLedger.Report` call beside the comparison the check already makes.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (slack.Count > 0)
            {
                Debug.LogError("SLACK: " + slack.Count + " ratchet(s) have a ceiling above their own measurement - "
                               + string.Join(" | ", slack.ToArray())
                               + ". Lower each ceiling to what it measured. A ratchet that has stopped being tight has "
                               + "stopped discriminating, and it will not fire until the backlog grows past a stale number.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
