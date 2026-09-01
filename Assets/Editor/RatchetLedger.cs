using System.Collections.Generic;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Where every ratchet reports what it MEASURED beside what its ceiling ALLOWS, so the two can be
    /// compared by something other than a person remembering to.
    ///
    /// <para><b>Why this exists — the eighth sweep's subject.</b> Every ratchet in this project is a
    /// claim: *"the backlog is exactly N, and the run fails if it grows."* ⚠ **A ceiling standing above
    /// its own measurement is SLACK, and slack is evidence that cannot discriminate** — the check goes on
    /// printing green while the thing it guards could get worse by the size of the gap before anything
    /// fires. Every ceiling in this repo carries the same instruction in its doc — *lower it as the
    /// backlog clears, never raise it* — and that instruction has, until now, been enforced by nobody.</para>
    ///
    /// <para><b>The ledger is per-process and order-dependent on purpose.</b> `CheckSuite.RunAllBatch`
    /// runs every check in one process, so the entries a check reports are still here when
    /// <see cref="RatchetSlackCheck"/> runs last. ⚠ Run that check alone and the ledger is empty — which
    /// it treats as a FAILURE, not a pass, because a slack audit that audited nothing looks exactly like
    /// one that found no slack.</para>
    /// </summary>
    public static class RatchetLedger
    {
        public struct Entry
        {
            public string Name;
            public int Measured;
            public int Ceiling;

            /// <summary>⚠ **A FLOOR ratchet, where the measurement must not fall BELOW the bound** — the
            /// mirror of the usual case. `PublicationCadenceCheck`'s reachable-preliminary count is one:
            /// the driver's warm-up breaks when it goes DOWN. Reporting a floor as if it were a ceiling
            /// would have made "tight" mean the opposite of what it says, which is precisely the kind of
            /// inverted label this audit exists to catch — so the direction is carried, not implied.</summary>
            public bool IsFloor;
        }

        private static readonly List<Entry> Reported = new List<Entry>();

        /// <summary>Report one ratchet. Call it once, next to the ceiling comparison it already makes —
        /// the point is that the measurement and the ceiling are the check's OWN, not re-derived here.</summary>
        public static void Report(string name, int measured, int ceiling, bool isFloor = false)
        {
            for (int i = 0; i < Reported.Count; i++)
            {
                if (Reported[i].Name == name)
                {
                    Reported[i] = new Entry { Name = name, Measured = measured, Ceiling = ceiling, IsFloor = isFloor };
                    return;
                }
            }

            Reported.Add(new Entry { Name = name, Measured = measured, Ceiling = ceiling, IsFloor = isFloor });
        }

        public static IReadOnlyList<Entry> Entries => Reported;

        public static void Clear() { Reported.Clear(); }
    }
}
