using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// R-EL8 (ruled by Elias, Day-1 2026-08-29): the United States' REAL presidential elector
    /// allocation — per-state winner-take-all PLUS Maine's and Nebraska's congressional-district
    /// method — implemented from the statutes, to the standard the other five countries' rules
    /// were held to. PURE FUNCTIONS, WIRED TO NOTHING (R-N2); the only caller is the editor
    /// backtest harness.
    ///
    /// The rules, each with its citation (full text and URLs in
    /// `ElectionsData/usa/district_method_2024.md`):
    /// - **The federal frame.** Each state appoints electors "in such Manner as the Legislature
    ///   thereof may direct" (U.S. Const. art. II §1 cl. 2), a number equal to its Senators plus
    ///   Representatives; 538 total, 270 to elect (NARA).
    /// - **Winner-take-all** is a STATE CHOICE, not a federal rule — 48 states and DC direct it.
    ///   Modelled as: the statewide plurality winner takes every elector of that jurisdiction.
    /// - **Maine**, Me. Rev. Stat. tit. 21-A §802: "One presidential elector shall be chosen from
    ///   each congressional district and 2 at large." → 2 at-large by statewide plurality + 1 per
    ///   congressional district by that district's plurality (4 total).
    /// - **Nebraska**, Neb. Rev. Stat. §32-710 (structure: one per district plus two at large) and
    ///   §32-1038(1) (allocation: the highest statewide vote elects the two at-large electors, the
    ///   highest vote in a congressional district elects that district's elector) → 5 total.
    ///   ⚠ §32-714 is NOT this rule (it governs vacancies and faithless electors) — the correction
    ///   is recorded in the data file.
    ///
    /// What this deliberately does NOT model, so its silence claims nothing: faithless electors
    /// (Me. §805, Neb. §32-714 pledge provisions), the Twelfth-Amendment contingent election when
    /// no candidate reaches 270, NPVIC activation (Me. §723-A(7)), or any ranked-choice tabulation
    /// (Maine's presidential race is legally RCV-eligible but was decided in round one in 2024 —
    /// see the data file; a model that assumed plurality forever would be wrong, and this one does
    /// not assume it, it simply takes the winner it is handed).
    /// </summary>
    public static class ElectoralCollege
    {
        /// <summary>Total electors and the majority, per NARA — asserted rather than assumed by the harness.</summary>
        public const int TotalElectors = 538;
        public const int MajorityToElect = 270;

        /// <summary>
        /// One jurisdiction's rule and result. <paramref name="districtWinners"/> is null or empty
        /// for a winner-take-all jurisdiction; where present, each entry is the winning candidate
        /// index in one congressional district, and <paramref name="atLargeElectors"/> go to the
        /// statewide winner (Maine 2 + 2 districts; Nebraska 2 + 3 districts).
        /// </summary>
        public readonly struct Jurisdiction
        {
            public readonly string Name;
            public readonly int TotalEv;
            public readonly int StatewideWinner;
            public readonly int AtLargeElectors;
            public readonly int[] DistrictWinners;

            public Jurisdiction(string name, int totalEv, int statewideWinner)
            {
                Name = name;
                TotalEv = totalEv;
                StatewideWinner = statewideWinner;
                AtLargeElectors = totalEv;
                DistrictWinners = null;
            }

            public Jurisdiction(string name, int totalEv, int statewideWinner, int atLargeElectors, int[] districtWinners)
            {
                Name = name;
                TotalEv = totalEv;
                StatewideWinner = statewideWinner;
                AtLargeElectors = atLargeElectors;
                DistrictWinners = districtWinners;
            }

            public bool UsesDistrictMethod => DistrictWinners != null && DistrictWinners.Length > 0;
        }

        /// <summary>
        /// Electors won by each candidate in one jurisdiction, by its own rule. A district-method
        /// jurisdiction's at-large electors follow the statewide plurality and each district's
        /// elector follows that district's plurality; a winner-take-all jurisdiction gives every
        /// elector to the statewide plurality winner.
        /// </summary>
        public static int[] AllocateJurisdiction(Jurisdiction jurisdiction, int candidateCount)
        {
            var result = new int[candidateCount];
            if (!jurisdiction.UsesDistrictMethod)
            {
                result[jurisdiction.StatewideWinner] += jurisdiction.TotalEv;
                return result;
            }

            if (jurisdiction.AtLargeElectors + jurisdiction.DistrictWinners.Length != jurisdiction.TotalEv)
            {
                throw new ArgumentException(
                    $"{jurisdiction.Name}: {jurisdiction.AtLargeElectors} at-large + " +
                    $"{jurisdiction.DistrictWinners.Length} district electors != {jurisdiction.TotalEv} total");
            }

            result[jurisdiction.StatewideWinner] += jurisdiction.AtLargeElectors;
            foreach (int districtWinner in jurisdiction.DistrictWinners)
            {
                result[districtWinner]++;
            }

            return result;
        }

        /// <summary>The whole college: every jurisdiction by its own rule, summed. Throws if the electors do not total <see cref="TotalElectors"/> — a silent miscount is the one failure this must never produce.</summary>
        public static int[] Allocate(Jurisdiction[] jurisdictions, int candidateCount)
        {
            var total = new int[candidateCount];
            int evSum = 0;
            foreach (Jurisdiction jurisdiction in jurisdictions)
            {
                int[] allocated = AllocateJurisdiction(jurisdiction, candidateCount);
                for (int c = 0; c < candidateCount; c++)
                {
                    total[c] += allocated[c];
                }

                evSum += jurisdiction.TotalEv;
            }

            if (evSum != TotalElectors)
            {
                throw new ArgumentException($"jurisdictions total {evSum} electors, not {TotalElectors}");
            }

            return total;
        }

        /// <summary>
        /// The same college computed as if EVERY jurisdiction were winner-take-all — the
        /// counterfactual that measures what the district method is actually worth in a given
        /// cycle. (In 2024 it happens to produce the same national totals, because Maine's and
        /// Nebraska's district effects cancel; that coincidence is the reason the real rule had to
        /// be built rather than inferred from a matching total.)
        /// </summary>
        public static int[] AllocateAsIfWinnerTakeAll(Jurisdiction[] jurisdictions, int candidateCount)
        {
            var total = new int[candidateCount];
            foreach (Jurisdiction jurisdiction in jurisdictions)
            {
                total[jurisdiction.StatewideWinner] += jurisdiction.TotalEv;
            }

            return total;
        }

        /// <summary>The winning candidate index, or -1 when nobody reaches <see cref="MajorityToElect"/> (the Twelfth-Amendment contingent case, which this model does not resolve — it reports it).</summary>
        public static int Winner(int[] electors)
        {
            for (int c = 0; c < electors.Length; c++)
            {
                if (electors[c] >= MajorityToElect) { return c; }
            }

            return -1;
        }
    }
}
