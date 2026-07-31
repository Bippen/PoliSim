using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Political Systems Overhaul Part B, full rollout. Two independent pieces: seat composition
    /// (recomputed every turn for every country from ApprovalRating, per PartyArchetypeData's own doc
    /// comment) and the gated-legislation flow for the omnibus Annual Budget bill (introduce -&gt; wait
    /// BillDurationDays -&gt; pass/fail against seat-weighted party alignment). Master Sequence step 4
    /// piloted this on Tax alone via a Tax-only TaxBill; step 5c generalizes it to BudgetBill, covering
    /// Tax, Spending, Welfare, and Sovereign Wealth Fund together - see BudgetBill's own doc comment.
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

        /// <summary>
        /// Bill's net fiscal direction: positive = net expansionary (more tax revenue and/or more
        /// spending), negative = net contractionary, 0 = neutral/mixed - the SAME "big government vs.
        /// small government" axis PartyArchetypeData.FiscalStance already scores against, generalized
        /// from Master Sequence step 4's Tax-only version (which compared bill-requested vs. standing
        /// effective rate per TaxType) to also sum Spending's requested percentage changes and
        /// Welfare's requested generosity deltas (same "effective value, 0 if not implemented" trick
        /// TaxLine already used, applied to WelfarePrograms too). Deliberately does NOT fold in SWF -
        /// contribution rate/allocation/asset-mix changes are a savings-and-allocation decision, not a
        /// tax-and-spend one, and there's no principled way to weigh "5% more equities" against "2
        /// points of income tax" on the same scale without an arbitrary conversion factor, so SWF
        /// terms are simply excluded from the vote (they still apply on PASS, they just don't sway it) -
        /// a stated simplification, not an oversight. Units are summed unnormalized across categories
        /// (tax rate points, spending % change, welfare generosity points) - consistent with this
        /// formula's own existing precedent as a stated proposal rather than a rigorously-derived one.
        /// </summary>
        public static float GetBillDirection(Country country, BudgetBill bill)
        {
            float direction = 0f;

            foreach (KeyValuePair<TaxType, TaxBillLine> kvp in bill.TaxLines)
            {
                TaxLine standing = FindTaxLine(country, kvp.Key);
                float standingEffectiveRate = standing != null && standing.IsImplemented ? standing.Rate : 0f;
                float billEffectiveRate = kvp.Value.IsImplemented ? kvp.Value.Rate : 0f;
                direction += billEffectiveRate - standingEffectiveRate;
            }

            foreach (KeyValuePair<SpendingCategory, float> kvp in bill.SpendingPercentChanges)
            {
                direction += kvp.Value;
            }

            foreach (KeyValuePair<WelfareProgramType, WelfareBillLine> kvp in bill.WelfarePrograms)
            {
                WelfareProgram standing = FindWelfareProgram(country, kvp.Key);
                float standingEffectiveGenerosity = standing != null && standing.IsImplemented ? standing.GenerosityLevel : 0f;
                float billEffectiveGenerosity = kvp.Value.IsImplemented ? kvp.Value.GenerosityLevel : 0f;
                direction += billEffectiveGenerosity - standingEffectiveGenerosity;
            }

            return direction;
        }

        /// <summary>
        /// PASS/FAIL formula (Master Roadmap Open Question, resolved in the step 4 pilot as a stated
        /// proposal, unchanged here beyond generalizing to BudgetBill's wider direction score): a
        /// genuinely neutral bill (direction == 0) auto-passes - there's no real fiscal shift to
        /// contest. Otherwise, each party's seat share is weighted by how well its own FiscalStance
        /// aligns with the bill's direction (sign-matched, so a contractionary bill scores POSITIVE
        /// against a low-tax/small-government party and NEGATIVE against a high-tax/big-government
        /// one); the bill passes if this seat-weighted alignment sums positive - i.e. parties whose
        /// stance matches the bill's direction, weighted by how many seats they hold, outweigh those
        /// opposed. An exact tie (weightedAlignment == 0, only reachable via a specific seat/stance
        /// coincidence) fails, not passes - a majority system doesn't grant ties to the proposer.
        /// </summary>
        public static bool WouldBillPass(Country country, BudgetBill bill)
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
        /// Applies a resolved BudgetBill's effect. PASS: every TaxLine's Rate/IsImplemented is written
        /// directly (clamped to each TaxType's own range, the same clamp
        /// SimulationManager.ApplyTaxRateChanges already applies) with the SAME one-time TaxHike
        /// ApprovalRating penalty the step 4 pilot already charged (MacroSystem.TaxHikeApprovalSensitivity,
        /// applied as a single immediate shock rather than threaded through the turn-scoped
        /// ApplyApprovalRating formula, since a bill can resolve on any day, not just a turn boundary);
        /// Welfare's IsImplemented/GenerosityLevel are written the same way; Spending's requested
        /// percentage changes and the SWF fields are applied via SimulationManager's own existing
        /// ApplySpendingLineChanges/ApplySwfPolicyChanges (reused as-is, via a throwaway PolicyDecision,
        /// rather than duplicating their clamping logic here) plus direct SWF create/dissolve handling,
        /// which those functions don't own. Deliberately does NOT charge a separate approval cost for
        /// Spending/Welfare/SWF changes - those applied with zero structural-change cost before this
        /// bill gated them, so passing them through this bill doesn't newly invent one; only the
        /// pre-existing TaxHike penalty carries over unchanged. FAIL: every standing value untouched,
        /// flat BillFailedApprovalCost charged, draft isn't lost (GameController's own draft
        /// dictionaries are never cleared by this).
        /// </summary>
        public static void ApplyBillResult(Country country, BudgetBill bill, bool passed, System.Action<Country, BudgetBill> applySpendingAndSwf)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            float totalHike = 0f;
            foreach (KeyValuePair<TaxType, TaxBillLine> kvp in bill.TaxLines)
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

            foreach (KeyValuePair<WelfareProgramType, WelfareBillLine> kvp in bill.WelfarePrograms)
            {
                WelfareProgram program = FindWelfareProgram(country, kvp.Key);
                if (program == null)
                {
                    continue;
                }

                program.IsImplemented = kvp.Value.IsImplemented;
                if (program.IsImplemented)
                {
                    program.GenerosityLevel = Mathf.Clamp(kvp.Value.GenerosityLevel, 0f, 100f);
                }
            }

            // Spending amounts and SWF rate/allocation/weights reuse SimulationManager's own existing
            // apply functions (private to that class) rather than duplicating their clamping logic here -
            // passed in as a delegate so ParliamentSystem never needs a direct dependency on
            // SimulationManager's internals. SWF create/dissolve is handled by the caller too, since it's
            // not something either existing apply function owns (they both assume the fund's existence
            // is already settled).
            applySpendingAndSwf(country, bill);

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

        private static WelfareProgram FindWelfareProgram(Country country, WelfareProgramType type)
        {
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (program.Type == type)
                {
                    return program;
                }
            }
            return null;
        }
    }
}
