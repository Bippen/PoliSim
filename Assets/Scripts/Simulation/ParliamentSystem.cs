using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// C-B3 / R-CL2: which published axis a bill is scored against.
    ///
    /// Until 2026-08-31 there was one axis and it was implicit — every bill, tariffs included, was
    /// weighed on CHES `lrecon`. Pass 6 deferred the Trade bill's own axis to "where real parties
    /// land" and recorded that reading the fiscal axis for it was Elias's ruling **until then**.
    /// </summary>
    public enum BillAxis
    {
        /// <summary>CHES `lrecon` through `PartySystems.FiscalStance` — every bill but the tariff one.</summary>
        Fiscal = 0,

        /// <summary>CHES `eu_position` through `PartySystems.TradeStance` — the Trade bill only.</summary>
        Trade = 1,
    }

    /// <summary>
    /// Political Systems Overhaul Part B, full rollout. Two independent pieces: seat composition
    /// (W-G1: seeded from each country's own most recent real election and changed ONLY by an
    /// election - it was recomputed every turn from ApprovalRating while parties were fictional) and the gated-legislation flow, now covering all three bill tiers - the omnibus Annual
    /// Budget bill (BudgetBill), standalone program add/remove bills (TaxProgramBill/
    /// WelfareProgramBill), and standalone non-budget policy bills (LaborPolicyBill/
    /// CrimeJusticePolicyBill/SectorPolicyBill/TradePolicyBill) - each introduce -&gt; wait
    /// BillDurationDays -&gt; pass/fail against the SAME seat-weighted party alignment core
    /// (WouldBillPass(Country, float)). Master Sequence step 4 piloted this on Tax alone via a
    /// Tax-only TaxBill; step 5c generalized it to BudgetBill (Tax, Spending, Welfare, SWF together);
    /// step 5d adds the other two tiers, reusing this same mechanism per the roadmap's own explicit
    /// instruction not to build a second standalone-bill system.
    /// </summary>
    public static class ParliamentSystem
    {
        /// <summary>Real in-game days a bill spends "in Parliament" before resolving - stands in for the roadmap's introduction/committee/debate stages without modeling them separately, a deliberately simple first pass. A placeholder like Phase 0's own GameSpeed pacing, not tuned against playtesting.</summary>
        public const int BillDurationDays = 21;

        // W-G1 RETIRED: MaxSeatsChangePerTurn, MaxSeatJitter and MinTargetShare governed the
        // per-turn drift of seats toward an ApprovalRating-derived target. Seats now change only at
        // an election, so a per-turn step, its jitter and a floor on a target share have nothing
        // left to govern. The Parliament random stream is untouched and stays declared.

        /// <summary>Flat ApprovalRating cost when a bill fails - a smaller, "not really the player's fault" magnitude than Cabinet's own 2-point ReshuffleApprovalCost, since failure here is Parliament's decision, not a player misstep.</summary>
        public const float BillFailedApprovalCost = 1.5f;

        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.Parliament);

        /// <summary>
        /// W-G1: seats are seeded from the country's OWN most recent real election
        /// (`PartySystems.InitialSeats`) and then **do not move between elections**.
        ///
        /// **What this replaced, and why.** Until W-G1 this method recomputed every country's seat
        /// shares EVERY TURN from `ApprovalRating`, using a per-archetype `ApprovalSensitivity`
        /// (+0.35 for the three "establishment" archetypes, −0.90 for the "protest" one) plus a
        /// bounded step and jitter. That existed because there were four generic fictional
        /// archetypes and no real parties, so a seat composition had to come from somewhere.
        ///
        /// **It cannot survive real parties, and should not.** `ApprovalSensitivity` per real party
        /// is not published by CHES, by GPS, or by anyone else — inventing eight of them for Sweden
        /// and nine for Germany is exactly what §0.4 forbids. And the behaviour it produced was
        /// wrong anyway: **a parliament's composition does not drift week by week with the
        /// government's approval. It changes at an election.** That is what item 10 builds, and this
        /// is the placeholder it replaces.
        ///
        /// Called once per turn for every country from `SimulationManager.AdvanceTurn`; seeding is
        /// idempotent, so the call stays where it was and simply has nothing to do once seeded.
        /// </summary>
        public static void UpdateSeats(Country country)
        {
            if (country.ParliamentSeats == null || country.ParliamentSeats.Count == 0)
            {
                country.ParliamentSeats = PartySystems.InitialSeats(country.Id);
            }
        }

        /// <summary>
        /// W-G1: the ONLY thing that changes a chamber — an election result, keyed by the same
        /// abbreviations `PartySystems` uses. Seats not named by the result are set to zero rather
        /// than left at their old value, because a party that won nothing holds nothing.
        /// </summary>
        public static void SetSeatsFromElection(Country country, IReadOnlyDictionary<string, int> wonSeats)
        {
            var seats = new Dictionary<string, int>();
            foreach (PoliticalParty p in PartySystems.For(country.Id))
            {
                seats[p.Abbrev] = wonSeats != null && wonSeats.TryGetValue(p.Abbrev, out int w) ? w : 0;
            }

            country.ParliamentSeats = seats;
        }

        /// <summary>
        /// Bill's net fiscal direction: positive = net expansionary (more tax revenue and/or more
        /// spending), negative = net contractionary, 0 = neutral/mixed - the SAME "big government vs.
        /// small government" axis `PartySystems.FiscalStance` scores against (W-G1: DERIVED from CHES
        /// `lrecon`, replacing four hand-set placeholder constants), generalized
        /// from Master Sequence step 4's Tax-only version (which compared bill-requested vs. standing
        /// rate per TaxType) to also sum Spending's requested percentage changes and Welfare's
        /// requested generosity deltas. Master Sequence step 5d moved implement/remove OUT of this
        /// bill entirely (see BudgetBill's own doc comment) - TaxLines/WelfarePrograms now only ever
        /// carry a rate/generosity value for a program the country ALREADY has implemented, so this
        /// simply skips any entry for a program that isn't (or a concurrent ProgramBill un-implemented
        /// between introduce and resolve), rather than needing the old "effective value, 0 if not
        /// implemented" trick. Deliberately does NOT fold in SWF - contribution rate/allocation/
        /// asset-mix changes are a savings-and-allocation decision, not a tax-and-spend one, and
        /// there's no principled way to weigh "5% more equities" against "2 points of income tax" on
        /// the same scale without an arbitrary conversion factor, so SWF terms are simply excluded
        /// from the vote (they still apply on PASS, they just don't sway it) - a stated simplification,
        /// not an oversight. Units are summed unnormalized across categories (tax rate points, spending
        /// % change, welfare generosity points) - consistent with this formula's own existing precedent
        /// as a stated proposal rather than a rigorously-derived one.
        /// </summary>
        public static float GetBillDirection(Country country, BudgetBill bill)
        {
            float direction = 0f;

            foreach (KeyValuePair<TaxType, float> kvp in bill.TaxLines)
            {
                TaxLine standing = FindTaxLine(country, kvp.Key);
                if (standing == null || !standing.IsImplemented)
                {
                    continue;
                }
                direction += kvp.Value - standing.Rate;
            }

            foreach (KeyValuePair<SpendingCategory, float> kvp in bill.SpendingPercentChanges)
            {
                direction += kvp.Value;
            }

            foreach (KeyValuePair<WelfareProgramType, float> kvp in bill.WelfarePrograms)
            {
                WelfareProgram standing = FindWelfareProgram(country, kvp.Key);
                if (standing == null || !standing.IsImplemented)
                {
                    continue;
                }
                direction += kvp.Value - standing.GenerosityLevel;
            }

            return direction;
        }

        /// <summary>
        /// PASS/FAIL formula (Master Roadmap Open Question, resolved in the step 4 pilot as a stated
        /// proposal): a genuinely neutral bill (direction == 0) auto-passes - there's no real fiscal
        /// shift to contest. Otherwise, each party's seat share is weighted by how well its own
        /// FiscalStance aligns with the bill's direction (sign-matched, so a contractionary bill scores
        /// POSITIVE against a low-tax/small-government party and NEGATIVE against a high-tax/
        /// big-government one); the bill passes if this seat-weighted alignment sums positive - i.e.
        /// parties whose stance matches the bill's direction, weighted by how many seats they hold,
        /// outweigh those opposed. An exact tie (weightedAlignment == 0, only reachable via a specific
        /// seat/stance coincidence) fails, not passes - a majority system doesn't grant ties to the
        /// proposer. Master Sequence step 5d generalized this to a plain float direction (was
        /// BudgetBill-specific) precisely so every bill tier - BudgetBill, the standalone program
        /// add/remove bills, and the standalone non-budget policy bills - can share this ONE scoring
        /// core rather than each reimplementing it; callers compute their own bill's direction first
        /// (each via its own GetXBillDirection, with its own stated sign convention documented there)
        /// and pass the resulting float in here.
        /// </summary>
        /// <summary>
        /// The seat-weighted alignment score a bill is actually decided on: positive passes, negative
        /// fails, and the magnitude is how comfortably. Extracted from WouldBillPass (which now calls
        /// it) purely so the UI can VISUALISE the deciding quantity instead of only its sign - the math
        /// is unchanged and there is deliberately still exactly one implementation, so the two can never
        /// drift apart and report different verdicts.
        ///
        /// Worth understanding before displaying it: this is NOT a headcount, and there is no
        /// seats-based majority threshold anywhere in this model. Each party contributes its seat share
        /// weighted by the STRENGTH of its fiscal stance, so a bill can pass with fewer aligned seats
        /// than opposed ones (if its supporters care more) and fail with more. Any UI that draws this as
        /// "N of 200 seats, needs 101" would be asserting something the simulation does not do - see
        /// GameController.DrawPendingLegislation for the diverging-lean bar used instead.
        /// </summary>
        /// <summary>
        /// W-G1: **re-expressed on real parties, not left alone.** The old version summed over four
        /// fictional archetypes with hand-set fiscal stances, normalised by a fictional 200-seat
        /// chamber. It now sums over the country's real seat-holders, with each stance DERIVED from
        /// that party's published CHES `lrecon` (`PartySystems.FiscalStance`) and normalised by that
        /// country's real chamber size.
        ///
        /// ⚠ **A unit with no published position contributes NOTHING and is not counted in the
        /// denominator either** — France's `UG` bloc, Poland's `TD` committee, Italy's minor lists.
        /// The alignment is therefore over the MEASURED part of the chamber, and
        /// `MeasuredSeatShare` says how much of it that is, so a caller can state the coverage
        /// instead of implying the whole chamber was weighed. §36: what is unmeasured is reported
        /// as unmeasured, never folded in at zero and never at the centre.
        /// </summary>
        public static float GetSeatWeightedAlignment(Country country, float direction)
            => GetSeatWeightedAlignment(country, direction, BillAxis.Fiscal);

        /// <summary>
        /// C-B3 / R-CL2: the same seat-weighted sum, over a NAMED axis.
        ///
        /// Pass 6 deferred the Trade bill's own axis to "where real parties land"; they have landed,
        /// and <see cref="BillAxis.Trade"/> scores a tariff bill against each party's OPENNESS
        /// (`PartySystems.TradeStance`, derived from CHES `eu_position`) instead of its fiscal
        /// position. Every other bill keeps <see cref="BillAxis.Fiscal"/> and is untouched.
        ///
        /// ⚠ **THE FALLBACK IS EXPLICIT AND ITS REASON TRAVELS WITH IT.** GPS 2019 carries no EU item,
        /// so no US party has an openness position and the trade axis measures **zero seats** of the
        /// House. Returning 0 there would be read by <see cref="WouldBillPass"/> as "fails", making
        /// every US tariff bill fail for want of DATA rather than for want of votes — so the axis
        /// falls back to fiscal, and <see cref="TradeAxisAvailable"/> lets a caller say which axis
        /// produced the verdict. Falling back is a stated approximation; silently scoring a chamber
        /// at zero would be a lie.
        /// </summary>
        public static float GetSeatWeightedAlignment(Country country, float direction, BillAxis axis)
        {
            float billSign = Mathf.Sign(direction);
            float weightedAlignment = 0f;
            float measuredSeats = 0f;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                bool measured = axis == BillAxis.Trade ? party.HasEuPosition : party.HasPosition;
                if (!measured) { continue; }

                int seats = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                measuredSeats += seats;
                float stance = axis == BillAxis.Trade
                    ? PartySystems.TradeStance(party)
                    : PartySystems.FiscalStance(party);
                weightedAlignment += seats * stance * billSign;
            }

            if (measuredSeats > 0f) { return weightedAlignment / measuredSeats; }

            // No seat in this chamber carries a position on this axis. For Trade that is the USA, by
            // construction; fall back rather than report a chamber that opposes everything.
            return axis == BillAxis.Trade
                ? GetSeatWeightedAlignment(country, direction, BillAxis.Fiscal)
                : 0f;
        }

        /// <summary>
        /// True when at least one SEATED unit in this chamber carries a published EU position — i.e.
        /// when a Trade bill is scored on the openness axis R-CL2 ruled in, rather than falling back
        /// to the fiscal one. **False for the USA**, whose GPS-2019 source has no EU item at all. A
        /// screen printing a trade verdict should say which axis produced it.
        /// </summary>
        public static bool TradeAxisAvailable(Country country)
        {
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (!party.HasEuPosition) { continue; }
                if (country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) && s > 0) { return true; }
            }

            return false;
        }

        /// <summary>
        /// W-G1: the fraction of this country's chamber whose fiscal position is actually published,
        /// so a screen can say "weighed over 83 % of the chamber" rather than implying all of it.
        /// Sweden, Germany (bar SSW's 1 seat) and the USA are near-complete; **France is the low
        /// one**, because the Ministry's nuance grid reports 178 seats for a joint left candidacy
        /// that no position survey scores as one party.
        /// </summary>
        public static float MeasuredSeatShare(Country country)
        {
            int chamber = PartySystems.ChamberSeats(country.Id);
            if (chamber <= 0) { return 0f; }

            int measured = 0;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (!party.HasPosition) { continue; }
                measured += country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
            }

            return (float)measured / chamber;
        }

        public static bool WouldBillPass(Country country, float direction)
            => WouldBillPass(country, direction, BillAxis.Fiscal);

        /// <summary>C-B3 / R-CL2: the same verdict, scored against a named axis. A zero-direction bill
        /// still passes unconditionally on either — a draft introduced unchanged asks the chamber for
        /// nothing.</summary>
        public static bool WouldBillPass(Country country, float direction, BillAxis axis)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return true;
            }

            return GetSeatWeightedAlignment(country, direction, axis) > 0f;
        }

        /// <summary>Convenience overload - computes BudgetBill's own direction, then scores it via the shared WouldBillPass(Country, float) core.</summary>
        public static bool WouldBillPass(Country country, BudgetBill bill)
        {
            return WouldBillPass(country, GetBillDirection(country, bill));
        }

        /// <summary>
        /// Records one resolved division into the country's bounded DivisionLog.
        ///
        /// **Why this is not inside the eight Apply*BillResult methods.** There is no single choke
        /// point to put it in: each tier resolves through its own Apply*BillResult - eight in total -
        /// and none of them receives the bill's direction or the current date, only an already-decided
        /// `passed` flag. Threading two more parameters through eight signatures would buy nothing over
        /// calling this once per resolution site, where direction, verdict and date are all already in
        /// scope. So: ONE implementation, eight callers - the same shape WouldBillPass already has, and
        /// for the same reason.
        ///
        /// ALIGNMENT IS CAPTURED, NEVER RECOMPUTED. The caller passes the same `direction` it just
        /// scored, so a record can never disagree with the verdict printed beside it. The
        /// zero-direction case mirrors the live estimate's own handling exactly (see
        /// GameController.DrawBillLiveEstimate): an uncontested bill records alignment 0 rather than
        /// GetSeatWeightedAlignment's raw net stance, which is negative in the documented tied-parties
        /// case and would contradict a PASSED verdict.
        ///
        /// WRITE-ONLY. Nothing in the Simulation namespace reads country.Divisions back. It is a record
        /// OF the simulation, never an input TO it - see DivisionLog's own doc comment for why that
        /// constraint is load-bearing rather than tidiness.
        /// </summary>
        public static void RecordDivision(Country country, string title, float direction, bool passed, System.DateTime date)
        {
            bool contested = !Mathf.Approximately(direction, 0f);
            float alignment = contested ? GetSeatWeightedAlignment(country, direction) : 0f;
            country.Divisions.Append(title, date, alignment, passed);
        }

        /// <summary>
        /// Applies a resolved BudgetBill's effect. PASS: every ALREADY-IMPLEMENTED TaxLine's Rate is
        /// written directly (clamped to that TaxType's own range, the same clamp
        /// SimulationManager.ApplyTaxRateChanges already applies) with the SAME one-time TaxHike
        /// ApprovalRating penalty the step 4 pilot already charged (MacroSystem.TaxHikeApprovalSensitivity,
        /// applied as a single immediate shock rather than threaded through the turn-scoped
        /// ApplyApprovalRating formula, since a bill can resolve on any day, not just a turn boundary);
        /// Welfare's GenerosityLevel is written the same way, for already-implemented programs only.
        /// Master Sequence step 5d moved implement/remove out of this bill entirely (see BudgetBill's
        /// own doc comment) - a bill entry for a program the country no longer has implemented (e.g. a
        /// concurrent ProgramBill removed it between introduce and resolve) is simply skipped, not an
        /// error. Spending's requested percentage changes and the SWF fields are applied via
        /// SimulationManager's own existing ApplySpendingLineChanges/ApplySwfPolicyChanges (reused
        /// as-is, via a throwaway PolicyDecision, rather than duplicating their clamping logic here)
        /// plus direct SWF create/dissolve handling, which those functions don't own. Deliberately does
        /// NOT charge a separate approval cost for Spending/Welfare/SWF changes - those applied with
        /// zero structural-change cost before this bill gated them, so passing them through this bill
        /// doesn't newly invent one; only the pre-existing TaxHike penalty carries over unchanged.
        /// FAIL: every standing value untouched, flat BillFailedApprovalCost charged, draft isn't lost
        /// (GameController's own draft dictionaries are never cleared by this).
        /// </summary>
        public static void ApplyBillResult(Country country, BudgetBill bill, bool passed, System.Action<Country, BudgetBill> applySpendingAndSwf)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            float totalHike = 0f;
            foreach (KeyValuePair<TaxType, float> kvp in bill.TaxLines)
            {
                TaxLine line = FindTaxLine(country, kvp.Key);
                if (line == null || !line.IsImplemented)
                {
                    continue;
                }

                float clampedRate = Mathf.Clamp(kvp.Value, line.MinRate, line.MaxRate);
                float hike = clampedRate - line.Rate;
                if (hike > 0f)
                {
                    totalHike += hike;
                }

                line.Rate = clampedRate;
            }

            foreach (KeyValuePair<WelfareProgramType, float> kvp in bill.WelfarePrograms)
            {
                WelfareProgram program = FindWelfareProgram(country, kvp.Key);
                if (program == null || !program.IsImplemented)
                {
                    continue;
                }

                program.GenerosityLevel = Mathf.Clamp(kvp.Value, 0f, 100f);
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

        /// <summary>
        /// Master Sequence step 5d, tier 2: a TaxProgramBill's direction reuses the exact "effective
        /// value, 0 if not implemented" trick BudgetBill's tax term used before 5d - implementing turns
        /// the TaxLine's own already-persistent Rate "on" (same sign as a rate hike, since the
        /// country's effective rate goes from 0 to Rate), removing turns it "off" (same sign as a rate
        /// cut). No new sign convention needed: this is just BudgetBill's original tax-term formula,
        /// scoped to one TaxType instead of summed across a dictionary.
        /// </summary>
        public static float GetTaxProgramBillDirection(Country country, TaxProgramBill bill)
        {
            TaxLine standing = FindTaxLine(country, bill.Type);
            if (standing == null)
            {
                return 0f;
            }

            float standingEffective = standing.IsImplemented ? standing.Rate : 0f;
            float billEffective = bill.IsAdd ? standing.Rate : 0f;
            return billEffective - standingEffective;
        }

        /// <summary>PASS: TaxLine.IsImplemented is set directly (Rate is untouched - a separate rate-change bill, or a later BudgetBill, is how the rate itself is adjusted). No approval cost beyond the shared BillFailedApprovalCost on FAIL - implement/remove never carried an extra cost before 5d either (see BudgetBill.ApplyBillResult's own doc comment on why structural changes stay free).</summary>
        public static void ApplyTaxProgramBillResult(Country country, TaxProgramBill bill, bool passed)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            TaxLine line = FindTaxLine(country, bill.Type);
            if (line != null)
            {
                line.IsImplemented = bill.IsAdd;
            }
        }

        /// <summary>WelfareProgramType equivalent of GetTaxProgramBillDirection - see that method's own doc comment, same pattern (GenerosityLevel in place of Rate).</summary>
        public static float GetWelfareProgramBillDirection(Country country, WelfareProgramBill bill)
        {
            WelfareProgram standing = FindWelfareProgram(country, bill.Type);
            if (standing == null)
            {
                return 0f;
            }

            float standingEffective = standing.IsImplemented ? standing.GenerosityLevel : 0f;
            float billEffective = bill.IsAdd ? standing.GenerosityLevel : 0f;
            return billEffective - standingEffective;
        }

        /// <summary>WelfareProgramType equivalent of ApplyTaxProgramBillResult - see that method's own doc comment, same pattern.</summary>
        public static void ApplyWelfareProgramBillResult(Country country, WelfareProgramBill bill, bool passed)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            WelfareProgram program = FindWelfareProgram(country, bill.Type);
            if (program != null)
            {
                program.IsImplemented = bill.IsAdd;
            }
        }

        /// <summary>
        /// Master Sequence step 5d, tier 3: LaborPolicyBill's direction - every dial here reads as
        /// "more support/intervention = positive" on the same FiscalStance axis BudgetBill's own terms
        /// use, per the roadmap's stated sign-convention proposal: higher minimum wage, more paid
        /// leave, stricter overtime regulation, more retraining funding, more family-policy support,
        /// and more open immigration policy are all coded as expansionary (positive); their opposites
        /// as contractionary (negative). No dial needs a sign flip, unlike CrimeJusticePolicyBill's
        /// mixed set - kept here anyway (rather than a shared unsigned-sum helper) so each bill type's
        /// convention stays explicit and independently readable, per this codebase's own "small
        /// explicit named methods over one generic monolith" convention.
        /// </summary>
        public static float GetLaborBillDirection(Country country, LaborPolicyBill bill)
        {
            // Pass 3 (coexistence ruling 2026-08-26): a labor bill's targets are STATUTORY BASE
            // targets now, so the direction measures the statutory change requested - bill minus
            // the country's *Base fields, not minus the law-composed effective dials (a bill that
            // merely restates the current base while laws offset the dial is correctly Neutral).
            float direction = 0f;
            if (country.MinimumWageImplemented)
            {
                direction += bill.MinimumWage - country.MinimumWagePercentOfMedianBase;
            }
            direction += bill.PaidFamilyLeaveWeeks - country.PaidFamilyLeaveWeeksBase;
            direction += bill.OvertimeRegulation - country.OvertimeRegulationBase;
            direction += bill.RetrainingProgram - country.RetrainingProgramBase;
            direction += bill.FamilyPolicy - country.FamilyPolicyBase;
            direction += bill.ImmigrationPolicy - country.ImmigrationPolicyBase;
            return direction;
        }

        /// <summary>PASS: every dial written directly via the caller-supplied delegate (SimulationManager.ApplyLaborBillEffects, which reuses the existing private Apply*Changes methods and their clamp bounds - see BudgetBill.ApplyBillResult's own doc comment for why this stays a delegate rather than ParliamentSystem owning the clamps itself). No new approval cost on PASS, matching every other non-tax bill tier; shared BillFailedApprovalCost on FAIL.</summary>
        public static void ApplyLaborBillResult(Country country, LaborPolicyBill bill, bool passed, System.Action<Country, LaborPolicyBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
        }

        /// <summary>
        /// CrimeJusticePolicyBill's direction - a MIXED sign convention, per the roadmap's stated
        /// proposal (see this class's own summary doc comment): Police Funding and Judicial Funding
        /// read as spending-like, so more funding = positive, matching BudgetBill's own Spending term.
        /// Sentencing Severity, Drug Policy (higher = stricter criminalization per its own UI label),
        /// and Border Enforcement all read as "tougher/more restrictive = conservative-coded", so
        /// higher = NEGATIVE; Bail Reform reads the opposite way (higher = more reform = progressive-
        /// coded = positive), matching its own UI label ("0 = traditional cash bail, 100 = full
        /// reform").
        /// </summary>
        public static float GetCrimeJusticeBillDirection(Country country, CrimeJusticePolicyBill bill)
        {
            float direction = 0f;
            direction += bill.PoliceFunding - country.PoliceFundingLevel;
            direction -= bill.SentencingSeverity - country.SentencingSeverity;
            direction += bill.BailReform - country.BailReformLevel;
            direction -= bill.DrugPolicy - country.DrugPolicyLevel;
            direction += bill.JudicialFunding - country.JudicialFundingLevel;
            direction -= bill.BorderEnforcement - country.BorderEnforcementLevel;
            return direction;
        }

        /// <summary>See ApplyLaborBillResult's own doc comment - identical pattern, different delegate (SimulationManager.ApplyCrimeJusticeBillEffects).</summary>
        public static void ApplyCrimeJusticeBillResult(Country country, CrimeJusticePolicyBill bill, bool passed, System.Action<Country, CrimeJusticePolicyBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
        }

        /// <summary>
        /// LawBill's direction - unlike every other bill's Get*Direction (which computes bill-value
        /// MINUS country's-current-value, since those bills carry an absolute target the player
        /// just submitted), a law's own deltas ARE already the requested change, so no country-state
        /// subtraction is needed. Reuses GetCrimeJusticeBillDirection's own sign convention exactly,
        /// since a law's deltas apply to the identical six dials: funding/reform-coded deltas are
        /// positive, harsher/stricter-coded deltas are negative. A repeal negates every term -
        /// undoing the law reads as the opposite lean of enacting it.
        /// </summary>
        /// <summary>Code-review pass (2026-08-25): one sign per DialDeltas index (funding/reform-coded
        /// positive, harsher/stricter-coded negative - the same convention this method's own class
        /// doc states), read alongside LawDefinition.DialDeltas so the two arrays stay the same
        /// length by construction rather than by two hand-written lists staying in sync.</summary>
        private static readonly float[] LawDialSigns =
        {
            // The Crime & Justice six (GetCrimeJusticeBillDirection's own convention).
            1f, -1f, 1f, -1f, 1f, -1f,
            // The labor six (pass 3, 2026-08-26): ALL positive - GetLaborBillDirection's own
            // all-positive coding ("more support/intervention = positive"), applied per delta:
            // minimum wage, paid leave, overtime regulation, retraining, family policy,
            // immigration openness.
            1f, 1f, 1f, 1f, 1f, 1f
        };

        /// <summary>DialDeltas index of MinimumWageDelta (the first labor field) - the one delta
        /// whose direction term must be SKIPPED for a country with no statutory minimum wage,
        /// exactly as GetLaborBillDirection skips the bill's own MinimumWage term. Index-locked to
        /// LawDefinition.DialDeltas' documented order.</summary>
        private const int MinimumWageDeltaIndex = 6;

        public static float GetLawBillDirection(Country country, LawBill bill)
        {
            LawDefinition law = LawCatalog.GetById(bill.LawId);
            if (law == null)
            {
                return 0f;
            }

            float sign = bill.IsRepeal ? -1f : 1f;
            float[] deltas = law.DialDeltas;
            float direction = 0f;
            for (int i = 0; i < deltas.Length; i++)
            {
                // Pass 3 (2026-08-26): a minimum-wage delta is inert for a country with no
                // statutory minimum (Sweden/Italy - the recompute's own ApplyMinimumWageChange
                // no-op), so Parliament must not lean on it either - the `country` parameter,
                // unused since this method shipped, earns its keep.
                if (i == MinimumWageDeltaIndex && !country.MinimumWageImplemented)
                {
                    continue;
                }

                direction += sign * LawDialSigns[i] * deltas[i];
            }

            return direction;
        }

        /// <summary>See ApplyCrimeJusticeBillResult's own doc comment - identical pattern, different delegate (SimulationManager.ApplyLawBillEffects).</summary>
        public static void ApplyLawBillResult(Country country, LawBill bill, bool passed, System.Action<Country, LawBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
        }

        /// <summary>
        /// SectorPolicyBill's direction - summed unnormalized across every Sector and every dial, the
        /// same "no principled per-category weighting" precedent BudgetBill's own formula already
        /// established. Subsidy/Regulation/Tax Credits/Research Grants all read as "more government
        /// intervention/spending = positive" (Sector.cs's own doc comments already describe Tax
        /// Credits and Research Grants as functionally similar to a direct subsidy); Deregulation/
        /// Nationalization reads the OPPOSITE way from its own name's first half - its UI label is "0 =
        /// fully nationalized, 100 = fully deregulated/private", so higher (more deregulated) is
        /// conservative-coded = NEGATIVE, matching Sentencing Severity/Drug Policy/Border Enforcement's
        /// own "higher on this particular dial leans right" reading in GetCrimeJusticeBillDirection.
        /// </summary>
        public static float GetSectorBillDirection(Country country, SectorPolicyBill bill)
        {
            float direction = 0f;
            foreach (Sector sector in country.Sectors)
            {
                if (bill.SubsidyLevels.TryGetValue(sector.Type, out float subsidy))
                {
                    direction += subsidy - sector.SubsidyLevel;
                }
                if (bill.RegulationLevels.TryGetValue(sector.Type, out float regulation))
                {
                    direction += regulation - sector.RegulationLevel;
                }
                if (bill.TaxCreditLevels.TryGetValue(sector.Type, out float taxCredit))
                {
                    direction += taxCredit - sector.TaxCreditLevel;
                }
                if (bill.ResearchGrantsLevels.TryGetValue(sector.Type, out float researchGrants))
                {
                    direction += researchGrants - sector.ResearchGrantsLevel;
                }
                if (bill.DeregulationLevels.TryGetValue(sector.Type, out float deregulation))
                {
                    direction -= deregulation - sector.DeregulationNationalizationLevel;
                }
            }
            return direction;
        }

        /// <summary>See ApplyLaborBillResult's own doc comment - identical pattern, different delegate (SimulationManager.ApplySectorBillEffects).</summary>
        public static void ApplySectorBillResult(Country country, SectorPolicyBill bill, bool passed, System.Action<Country, SectorPolicyBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
        }

        /// <summary>
        /// TradePolicyBill's direction - a higher tariff is, structurally, a tax on trade (more
        /// government revenue = expansionary = positive), the same reading BudgetBill's own TaxLines
        /// term already uses for an ordinary tax rate.
        ///
        /// Pass 6 (2026-08-27, TradeCosts.OverrideDirectionWeight): per-partner overrides enter the
        /// vote. They used to be EXCLUDED by analogy to SWF's asset-mix terms, whose reason is a missing
        /// conversion factor onto the fiscal scale - but an override is a tariff rate in the very points
        /// the base term scores, and an unnormalized per-partner sum would be a partner-count artefact
        /// (three links vs five scoring the same policy differently), so the direction is the CHANGE IN
        /// THE IMPORT-WEIGHTED AVERAGE TARIFF on the country's imports, in points - the tau-bar the
        /// pass-through formula uses. For a country whose whole import set resolves at its base rate
        /// (the USA) a base-only bill scores NewBase - Base exactly as before; for an EU member, whose
        /// base rate is never charged, a base-only bill scores 0 - correct, and the old formula's
        /// nonzero reading for it was a phantom. The vote reads the SIGN of this
        /// (GetSeatWeightedAlignment), so the magnitude is a label, not a lever - stated in the pass-6
        /// record as the pass's taste-adjacent call (the fiscal axis is the model's only axis until
        /// item 10). With the weight at 0 the pre-pass-6 formula is returned verbatim.
        /// </summary>
        public static float GetTradeBillDirection(Country country, TradePolicyBill bill, World world)
        {
            if (TradeCosts.OverrideDirectionWeight <= 0f)
            {
                return bill.NewBaseTariffRate - country.BaseTariffRate;
            }

            float weightedChange = 0f;
            float imports = 0f;
            foreach (TradePartner link in country.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                float current = TradeSystem.GetOwnTariffRate(country, partner, world.TradeBlocs);
                float proposed;
                if (bill.PartnerTariffOverrides.TryGetValue(link.PartnerId, out float requested))
                {
                    proposed = requested;
                }
                else if (link.HasPlayerTariffOverride)
                {
                    proposed = link.PlayerTariffOverride;
                }
                else
                {
                    proposed = TradeSystem.GetStandingTariffRate(country, partner, world.TradeBlocs, bill.NewBaseTariffRate);
                }

                weightedChange += link.ImportVolume * (proposed - current);
                imports += link.ImportVolume;
            }

            return imports > 0f ? weightedChange / imports : 0f;
        }

        /// <summary>See ApplyLaborBillResult's own doc comment - identical pattern, different delegate (SimulationManager.ApplyTradeBillEffects).</summary>
        public static void ApplyTradeBillResult(Country country, TradePolicyBill bill, bool passed, System.Action<Country, TradePolicyBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
        }

        /// <summary>
        /// SwfDrawdownBill's direction. Drawing on the fund puts money into the economy now, so it reads
        /// EXPANSIONARY - positive on the same FiscalStance axis every other bill here scores against.
        ///
        /// The withdrawal is already a percentage of GDP, the same unit BudgetBill's own SWF contribution
        /// term uses, so it is directly comparable against the other bills' magnitudes without rescaling.
        /// The sign flip lives in the field's NAME rather than in arithmetic here:
        /// <see cref="SwfDrawdownBill.WithdrawalPercentOfGdp"/> is positive when taking money out, where
        /// `ContributionRatePercent` is positive when paying in.
        /// </summary>
        public static float GetSwfDrawdownBillDirection(Country country, SwfDrawdownBill bill)
        {
            return bill.WithdrawalPercentOfGdp;
        }

        /// <summary>See ApplyLaborBillResult's own doc comment - identical pattern, different delegate (SimulationManager.ApplySwfDrawdownBillEffects).</summary>
        public static void ApplySwfDrawdownBillResult(Country country, SwfDrawdownBill bill, bool passed, System.Action<Country, SwfDrawdownBill> applyEffects)
        {
            if (!passed)
            {
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - BillFailedApprovalCost, 0f, 100f);
                return;
            }

            applyEffects(country, bill);
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
