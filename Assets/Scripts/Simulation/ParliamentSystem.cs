using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Political Systems Overhaul Part B PILOT (Tax Policy tab only), Master Sequence step 4. Two
    /// independent pieces: seat composition (recomputed every turn for every country from
    /// ApprovalRating, per PartyArchetypeData's own doc comment) and the gated-legislation flow for
    /// Tax bills specifically (introduce -&gt; wait BillDurationDays -&gt; pass/fail against seat-
    /// weighted party alignment). Deliberately does NOT generalize past Tax - see
    /// SimulationManager's TaxBill-specific pending/resolve methods, the only entry points this pilot
    /// wires up.
    /// </summary>
    public static class ParliamentSystem
    {
        /// <summary>Real in-game days a bill spends "in Parliament" before resolving - stands in for the roadmap's introduction/committee/debate stages without modeling them separately, a deliberately simple first pass. A placeholder like Phase 0's own GameSpeed pacing, not tuned against playtesting.</summary>
        public const int BillDurationDays = 21;

        /// <summary>Bounded inertia: actual seats move toward the ApprovalRating-derived target by at most this many seats per turn - never snapping instantly, the same "gap-based target + reversion rate, clamped" idiom this codebase already uses for ApprovalRating/CrimeIndex/PovertyRate.</summary>
        private const int MaxSeatsChangePerTurn = 6;

        /// <summary>Small bounded per-party random walk applied alongside the inertia step, per the roadmap's own "plus bounded inertia/randomness" instruction - ordinary political volatility, not a swing election every turn.</summary>
        private const int MaxSeatJitter = 1;

        /// <summary>Floor so no archetype's TARGET share is ever computed as exactly 0 (a party can still shrink toward 0 actual seats via inertia over many turns, but the formula itself never assigns it a literal-zero target).</summary>
        private const float MinTargetShare = 0.02f;

        /// <summary>Flat ApprovalRating cost when a bill fails - a smaller, "not really the player's fault" magnitude than Cabinet's own 2-point ReshuffleApprovalCost, since failure here is Parliament's decision, not a player misstep.</summary>
        public const float BillFailedApprovalCost = 1.5f;

        private static readonly System.Random RandomSource = new System.Random();

        /// <summary>
        /// Recomputes this country's target seat shares from its current ApprovalRating
        /// (PartyArchetypeData's own doc comment explains each archetype's ApprovalSensitivity), then
        /// moves actual seats toward that target by a bounded step plus small jitter - never snapping.
        /// Called once per turn, for every country, from SimulationManager.AdvanceTurn.
        /// </summary>
        public static void UpdateSeats(Country country)
        {
            if (country.ParliamentSeats == null || country.ParliamentSeats.Count == 0)
            {
                country.ParliamentSeats = PartyArchetypeData.GetInitialSeats();
                return;
            }

            var targetShares = new Dictionary<PartyArchetype, float>();
            float totalShare = 0f;
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                float share = PartyArchetypeData.GetBaseSupportShare(archetype)
                    + PartyArchetypeData.GetApprovalSensitivity(archetype) * (country.State.ApprovalRating - 50f) / 100f;
                share = Mathf.Max(MinTargetShare, share);
                targetShares[archetype] = share;
                totalShare += share;
            }

            var targetSeats = new Dictionary<PartyArchetype, int>();
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                targetSeats[archetype] = Mathf.RoundToInt(targetShares[archetype] / totalShare * ParliamentConstants.TotalSeats);
            }
            ReconcileToTotal(targetSeats);

            var newSeats = new Dictionary<PartyArchetype, int>();
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                int current = country.ParliamentSeats.TryGetValue(archetype, out int c) ? c : 0;
                int step = Mathf.Clamp(targetSeats[archetype] - current, -MaxSeatsChangePerTurn, MaxSeatsChangePerTurn);
                int jitter = RandomSource.Next(-MaxSeatJitter, MaxSeatJitter + 1);
                newSeats[archetype] = Mathf.Max(0, current + step + jitter);
            }
            ReconcileToTotal(newSeats);

            country.ParliamentSeats = newSeats;
        }

        /// <summary>Rounding/jitter can drift the sum away from TotalSeats by a couple of seats either way - reconciled deterministically onto whichever party currently holds the most seats, simplest possible fix-up rather than a proportional redistribution.</summary>
        private static void ReconcileToTotal(Dictionary<PartyArchetype, int> seats)
        {
            int sum = 0;
            foreach (KeyValuePair<PartyArchetype, int> kvp in seats)
            {
                sum += kvp.Value;
            }

            int diff = ParliamentConstants.TotalSeats - sum;
            if (diff == 0)
            {
                return;
            }

            PartyArchetype largest = PartyArchetype.ProgressiveAlliance;
            int largestSeats = -1;
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                if (seats[archetype] > largestSeats)
                {
                    largestSeats = seats[archetype];
                    largest = archetype;
                }
            }

            seats[largest] = Mathf.Max(0, seats[largest] + diff);
        }

        /// <summary>Bill's net fiscal direction: positive = net tax increase, negative = net decrease, 0 = neutral/mixed - compares each line's bill-requested effective rate (0 if the bill doesn't implement it) against the country's CURRENT standing effective rate (0 if not currently implemented).</summary>
        public static float GetBillDirection(Country country, TaxBill bill)
        {
            float direction = 0f;
            foreach (KeyValuePair<TaxType, TaxBillLine> kvp in bill.Lines)
            {
                TaxLine standing = FindTaxLine(country, kvp.Key);
                float standingEffectiveRate = standing != null && standing.IsImplemented ? standing.Rate : 0f;
                float billEffectiveRate = kvp.Value.IsImplemented ? kvp.Value.Rate : 0f;
                direction += billEffectiveRate - standingEffectiveRate;
            }
            return direction;
        }

        /// <summary>
        /// PASS/FAIL formula (Master Roadmap Open Question, resolved here as a stated proposal): a
        /// genuinely neutral bill (direction == 0) auto-passes - there's no real fiscal shift to
        /// contest. Otherwise, each party's seat share is weighted by how well its own FiscalStance
        /// aligns with the bill's direction (sign-matched, so a tax-cut bill scores POSITIVE against a
        /// low-tax party and NEGATIVE against a high-tax party); the bill passes if this seat-weighted
        /// alignment sums positive - i.e. parties whose stance matches the bill's direction, weighted
        /// by how many seats they hold, outweigh those opposed. An exact tie (weightedAlignment == 0,
        /// only reachable via a specific seat/stance coincidence) fails, not passes - a majority
        /// system doesn't grant ties to the proposer.
        /// </summary>
        public static bool WouldBillPass(Country country, TaxBill bill)
        {
            float direction = GetBillDirection(country, bill);
            if (Mathf.Approximately(direction, 0f))
            {
                return true;
            }

            float billSign = Mathf.Sign(direction);
            float weightedAlignment = 0f;
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                int seats = country.ParliamentSeats.TryGetValue(archetype, out int s) ? s : 0;
                float seatShare = (float)seats / ParliamentConstants.TotalSeats;
                weightedAlignment += seatShare * PartyArchetypeData.GetFiscalStance(archetype) * billSign;
            }

            return weightedAlignment > 0f;
        }

        /// <summary>
        /// Applies a resolved bill's effect. PASS: every bill line's Rate/IsImplemented is written
        /// directly onto the country's standing TaxLines (clamped to each TaxType's own range, the
        /// same clamp SimulationManager.ApplyTaxRateChanges already applies), and a one-time
        /// ApprovalRating penalty is charged for any net rate increase, reusing
        /// MacroSystem.TaxHikeApprovalSensitivity - the SAME coefficient a direct tax hike already
        /// cost before this pilot, so a passed hike of a given size costs the same approval it always
        /// did, applied as a single immediate shock here (Cabinet/ForeignPolicy's own idiom) rather
        /// than threaded through the turn-scoped ApplyApprovalRating formula, since a bill can resolve
        /// on any day, not just a turn boundary. FAIL: standing values untouched, flat
        /// BillFailedApprovalCost charged, draft isn't lost (GameController's own draft dictionaries
        /// are never cleared by this).
        /// </summary>
        public static void ApplyBillResult(Country country, TaxBill bill, bool passed)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            float totalHike = 0f;
            foreach (KeyValuePair<TaxType, TaxBillLine> kvp in bill.Lines)
            {
                TaxLine line = FindTaxLine(country, kvp.Key);
                if (line == null)
                {
                    continue;
                }

                float clampedRate = Mathf.Clamp(kvp.Value.Rate, line.MinRate, line.MaxRate);
                if (kvp.Value.IsImplemented && line.IsImplemented)
                {
                    float hike = clampedRate - line.Rate;
                    if (hike > 0f)
                    {
                        totalHike += hike;
                    }
                }

                line.Rate = clampedRate;
                line.IsImplemented = kvp.Value.IsImplemented;
            }

            float taxHikePenalty = MacroSystem.TaxHikeApprovalSensitivity * totalHike;
            country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - taxHikePenalty, 0f, 100f);
        }

        private static TaxLine FindTaxLine(Country country, TaxType type)
        {
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type == type)
                {
                    return line;
                }
            }
            return null;
        }
    }
}
