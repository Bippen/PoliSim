using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// R-EL9 (ruled by Elias, Day-1 2026-08-29): Italy's Rosatellum — the NATIONAL PROPORTIONAL
    /// STAGE of the Camera allocation, implemented from the statute now that it is sourced
    /// (`ElectionsData/italy/rosatellum_allocation.md`; DPR 361/1957 art. 83 as consolidated,
    /// in force at 25-9-2022). PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// The method is **Hare with largest remainders, applied twice, with the quotient TRUNCATED
    /// to an integer both times** — not a divisor method, which is why it could not be run on the
    /// existing <see cref="SeatAllocation"/> machinery:
    /// - **lett. b)** the threshold denominator is the total valid votes of ALL lists, including
    ///   those below every threshold.
    /// - **lett. c)** a coalition's figure sums its members' votes EXCEPT members below 1 %, whose
    ///   votes are discarded (not redistributed) — while remaining in the lett. b) denominator.
    /// - **lett. e)** a coalition is admitted at >= 10 % AND with a member >= 3 % (or a recognised
    ///   minority member); a standalone list at >= 3 %; a list in a failed coalition falls back to
    ///   the standalone test; a qualifying minority list is admitted regardless of the 3 %.
    /// - **lett. f)** quotient = floor(sum of admitted figures / seats); integer parts; the rest by
    ///   largest remainders.
    /// - **lett. g)** inside each coalition the divisor is the sum of the ADMITTED member lists'
    ///   votes — NOT the coalition's own figure. (Reading it as the coalition figure puts the 2022
    ///   result nine seats out; the data file records the check.) Quotient floored again.
    ///
    /// The two tiers are PARALLEL: the single-member college seats are subtracted from the seat
    /// POOL before this runs, never from a party's entitlement. There is no scorporo, so nothing
    /// here takes a party's college wins as input — that absence is the model, not an omission.
    ///
    /// Scope, stated so silence claims nothing: this is the NATIONAL stage only. The devolution to
    /// the 28 circoscrizioni (lett. h/i) and the 49 collegi plurinominali (art. 83-bis), and the
    /// art. 84 incapienza cascade, are NOT implemented — they need per-circoscrizione and
    /// per-collegio cifre elettorali that exist only as HTML on Eligendo. Those stages change WHICH
    /// deputies sit, not the per-list national totals this returns.
    /// </summary>
    public static class Rosatellum
    {
        /// <summary>A list as art. 83 sees it: its votes, which coalition it is linked to (-1 = standalone), and whether it is a recognised-minority list admitted by the lett. e) n.2 route.</summary>
        public readonly struct ListEntry
        {
            public readonly string Name;
            public readonly long Votes;
            public readonly int CoalitionId;
            public readonly bool MinorityQualified;

            public ListEntry(string name, long votes, int coalitionId, bool minorityQualified = false)
            {
                Name = name;
                Votes = votes;
                CoalitionId = coalitionId;
                MinorityQualified = minorityQualified;
            }
        }

        public const double MinorityCoalitionFloor = 0.01;
        public const double ListThreshold = 0.03;
        public const double CoalitionThreshold = 0.10;

        /// <summary>
        /// The national proportional seats per list. <paramref name="totalValidAllLists"/> is the
        /// lett. b) denominator (every list's votes, admitted or not);
        /// <paramref name="seats"/> is the proportional pool (245 for the Camera in 2022, i.e.
        /// after the college seats are taken out). <paramref name="trace"/> returns the working so
        /// a run can be audited line by line against the statute.
        /// </summary>
        public static int[] AllocateNational(ListEntry[] lists, long totalValidAllLists, int seats, out string trace)
        {
            var log = new System.Text.StringBuilder();
            double onePercent = MinorityCoalitionFloor * totalValidAllLists;
            double threePercent = ListThreshold * totalValidAllLists;
            double tenPercent = CoalitionThreshold * totalValidAllLists;

            // lett. c): coalition figures, sub-1% members struck (minority members exempt).
            var coalitionFigure = new Dictionary<int, long>();
            var coalitionHasQualifyingMember = new Dictionary<int, bool>();
            foreach (ListEntry list in lists)
            {
                if (list.CoalitionId < 0) { continue; }

                bool countsToward = list.MinorityQualified || list.Votes >= onePercent;
                if (countsToward)
                {
                    coalitionFigure.TryGetValue(list.CoalitionId, out long figure);
                    coalitionFigure[list.CoalitionId] = figure + list.Votes;
                }

                bool qualifies = list.MinorityQualified || list.Votes >= threePercent;
                coalitionHasQualifyingMember.TryGetValue(list.CoalitionId, out bool had);
                coalitionHasQualifyingMember[list.CoalitionId] = had || qualifies;
            }

            // lett. e): which coalitions are admitted.
            var admittedCoalitions = new HashSet<int>();
            foreach (KeyValuePair<int, long> pair in coalitionFigure)
            {
                bool admitted = pair.Value >= tenPercent
                                && coalitionHasQualifyingMember.TryGetValue(pair.Key, out bool q) && q;
                if (admitted) { admittedCoalitions.Add(pair.Key); }

                log.Append($"  coalition {pair.Key}: figure {pair.Value} ({100.0 * pair.Value / totalValidAllLists:F2}%) -> {(admitted ? "ADMITTED" : "rejected")}\n");
            }

            // lett. e): standalone entities - a list not in a coalition, or stranded in a failed
            // one, admitted on its own 3% (or the minority route).
            var entityIndex = new Dictionary<int, int>();   // coalitionId -> entity slot
            var entityFigure = new List<long>();
            var entityIsCoalition = new List<int>();        // coalitionId or -1
            var standaloneListIndex = new List<int>();      // list index for standalone entities

            foreach (int coalitionId in admittedCoalitions)
            {
                entityIndex[coalitionId] = entityFigure.Count;
                entityFigure.Add(coalitionFigure[coalitionId]);
                entityIsCoalition.Add(coalitionId);
                standaloneListIndex.Add(-1);
            }

            for (int i = 0; i < lists.Length; i++)
            {
                ListEntry list = lists[i];
                bool inAdmittedCoalition = list.CoalitionId >= 0 && admittedCoalitions.Contains(list.CoalitionId);
                if (inAdmittedCoalition) { continue; }

                bool admitted = list.MinorityQualified || list.Votes >= threePercent;
                log.Append($"  standalone {list.Name}: {list.Votes} ({100.0 * list.Votes / totalValidAllLists:F2}%) -> {(admitted ? "ADMITTED" : "rejected")}\n");
                if (!admitted) { continue; }

                entityFigure.Add(list.Votes);
                entityIsCoalition.Add(-1);
                standaloneListIndex.Add(i);
            }

            // lett. f): floored-Hare over the admitted entities.
            long[] figures = entityFigure.ToArray();
            int[] entitySeats = FlooredHare(figures, seats, out long quotientF);
            log.Append($"  lett.f: sum {Sum(figures)} / {seats} -> floored quotient {quotientF}\n");

            // lett. g): inside each admitted coalition, over its ADMITTED member lists only.
            var result = new int[lists.Length];
            for (int e = 0; e < entitySeats.Length; e++)
            {
                if (entityIsCoalition[e] < 0)
                {
                    result[standaloneListIndex[e]] = entitySeats[e];
                    continue;
                }

                int coalitionId = entityIsCoalition[e];
                var memberIdx = new List<int>();
                var memberVotes = new List<long>();
                for (int i = 0; i < lists.Length; i++)
                {
                    if (lists[i].CoalitionId != coalitionId) { continue; }
                    if (!(lists[i].MinorityQualified || lists[i].Votes >= threePercent)) { continue; }

                    memberIdx.Add(i);
                    memberVotes.Add(lists[i].Votes);
                }

                long[] mv = memberVotes.ToArray();
                int[] memberSeats = FlooredHare(mv, entitySeats[e], out long quotientG);
                log.Append($"  lett.g coalition {coalitionId}: admitted-list sum {Sum(mv)} / {entitySeats[e]} -> floored quotient {quotientG}\n");
                for (int m = 0; m < memberIdx.Count; m++)
                {
                    result[memberIdx[m]] = memberSeats[m];
                }
            }

            trace = log.ToString();
            return result;
        }

        /// <summary>Hare with the quotient TRUNCATED to an integer, then largest remainders — the operation art. 83 prescribes at every stage ("non tiene conto dell'eventuale parte frazionaria").</summary>
        public static int[] FlooredHare(long[] votes, int seats, out long quotient)
        {
            var seatsOut = new int[votes.Length];
            long total = Sum(votes);
            quotient = seats > 0 ? total / seats : 0;   // integer division = the statute's truncation
            if (quotient <= 0) { return seatsOut; }

            int assigned = 0;
            var remainder = new double[votes.Length];
            for (int i = 0; i < votes.Length; i++)
            {
                double exact = (double)votes[i] / quotient;
                seatsOut[i] = (int)Math.Floor(exact);
                remainder[i] = exact - seatsOut[i];
                assigned += seatsOut[i];
            }

            // Largest remainders; ties by the larger raw figure, then the lower index (the statute
            // draws lots at full equality - stated rather than silently ordered).
            while (assigned < seats)
            {
                int best = -1;
                for (int i = 0; i < votes.Length; i++)
                {
                    if (remainder[i] < 0) { continue; }
                    if (best < 0
                        || remainder[i] > remainder[best]
                        || (remainder[i] == remainder[best] && votes[i] > votes[best]))
                    {
                        best = i;
                    }
                }

                if (best < 0) { break; }

                seatsOut[best]++;
                remainder[best] = -1;   // one remainder seat per entity per pass
                assigned++;
            }

            return seatsOut;
        }

        private static long Sum(long[] values)
        {
            long total = 0;
            foreach (long v in values) { total += v; }
            return total;
        }
    }
}
