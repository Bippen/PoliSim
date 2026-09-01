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
    /// <para>⚠ <b>WHAT IT CANNOT SEE.</b> A ratchet that does not report is invisible to it, so the
    /// coverage is printed rather than implied: the ledger names what it holds, and anything absent is
    /// unguarded. **A ratchet added without a `Report` call is exactly the hole this check would have
    /// caught in someone else's code**, and there is no way to make it self-detecting short of the
    /// mutation probe the sixth sweep already billed.</para>
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

            var slack = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (h): a ratchet whose ceiling has stopped discriminating ===\n");
            sb.Append(F("    THE ENUMERATION: {0} ratchet(s) reported to the ledger this run. A ratchet that does not report\n"
                        + "    is INVISIBLE here - the coverage is the list below and nothing else.\n\n", entries.Count));
            sb.Append("    ratchet                                          measured   ceiling   verdict\n");
            sb.Append("    ---------------------------------------------------------------------------\n");

            foreach (RatchetLedger.Entry e in entries)
            {
                string verdict;
                if (e.Measured > e.Ceiling)
                {
                    // Not this check's business: the owning check has already failed for it.
                    verdict = "OVER - its own check fails";
                }
                else if (e.Measured < e.Ceiling)
                {
                    verdict = "⚠ SLACK by " + (e.Ceiling - e.Measured);
                    slack.Add(F("{0}: measured {1}, ceiling {2} - slack by {3}", e.Name, e.Measured, e.Ceiling, e.Ceiling - e.Measured));
                }
                else
                {
                    verdict = "tight";
                }

                sb.Append(F("    {0,-46} {1,8} {2,9}   {3}\n", e.Name, e.Measured, e.Ceiling, verdict));
            }

            sb.Append("\n    ⚠ SLACK IS NOT A COSMETIC FAULT. A ceiling above its measurement lets the thing it guards get\n");
            sb.Append("    worse by the size of the gap while the check keeps printing green - a guard whose evidence has\n");
            sb.Append("    stopped discriminating, which is the class this sweep exists for. Every ratchet's own doc already\n");
            sb.Append("    says to lower it as the backlog clears; until now nothing but memory enforced that.\n");
            sb.Append("    THE FIX IS ALWAYS THE SAME: lower the ceiling to what was measured. NEVER raise a measurement to\n");
            sb.Append("    meet a ceiling, and never widen a ceiling to silence this.\n");

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
