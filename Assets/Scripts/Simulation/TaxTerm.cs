using System;
using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// A-LOOP q37 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §2, Elias's ruling J1): <b>the tax term reads a change against the fiscal year's opening
    /// rates; a cut earns a smaller mirror; the term decays.</b>
    /// <list type="bullet">
    /// <item><b>The reference.</b> A line's net is its position now against its position at the fiscal year's opening (`FiscalYearData.GetFiscalYearStart`),
    /// in ONE unit - `ParliamentSystem.TaxLevelPoints`: effective-rate points for a rate line (the income tax's sub-rows included), the dial's per cent
    /// for a per-tonne line, 0 for a line not levied whatever its placeholder rate (D-33's fix; review 877's M1). It is kept as the year's BOOKED net
    /// per line (`Country.TaxTermNet`, emptied on the fiscal year's first day): the sum of the moves the government made since the opening, each
    /// measured as it lands. That is the opening shifted by every move no one booked (review 878's M-3) - the carbon statute's yearly step and a passed
    /// figure's overwrite (EN-4e), the income line's effective rate moving with indexation and bracket creep, a congressional majority's bill the
    /// government does not bear (`Congress.GovernmentBears`) - so the net is the government's own and no drift is priced at the kink.</item>
    /// <item><b>The charged position</b> of a line is P(net): s·x for a rise, m·s·x for a cut (<see cref="Charge"/>; s = `MacroSystem.TaxHikeApprovalSensitivity`,
    /// m = <see cref="MirrorFraction"/>).</item>
    /// <item><b>Each move books the impulse P(net after) − P(net before)</b> (<see cref="Move"/>) from every site that moves a rate: the turn's policy decision
    /// (`SimulationManager.ApplyTaxRateChanges`, charged through `MacroSystem.ApplyApprovalRating` and booked after it, <see cref="BookFormula"/>), every
    /// passed budget bill (`ParliamentSystem.ApplyBillResult`) and q34's levy or removal (<see cref="ChargeLevyMove"/>, at
    /// `SimulationManager.AdvanceTaxProgramBillsDay`). A cut and its restore inside one fiscal year book impulses that net to zero; across a year
    /// boundary the restore is a rise from the new opening.</item>
    /// <item><b>The stock T</b> (`Country.TaxTermStock`, approval points held against the government) is the distance between the run without the
    /// term and the run with it: <b>the base B = approval + T is the run without the term, and it lives in [0, 100] as approval does</b> (review 878's
    /// M-2, review 878b's M-5). So T takes what LANDED - the whole impulse where approval's [0, 100] clamp does not bind, only the approval actually
    /// moved where it does - and a clamp that absorbs a standing T in its own direction forgives the part past the bound (<see cref="Reclamp"/>, first in
    /// every day and at every booking; <see cref="BookFormula"/> at the boundary; and every other clamped approval writer goes through <see cref="Shock"/>, which also catches a shock that meets a bound AGAINST a standing T, review 879b's M-1): the run without the move sits at the same bound. A rise at the
    /// floor buys no later hand-back, a cut at the ceiling owes none, a rise absorbed at the ceiling is no later gift and a cut absorbed at the floor no
    /// later debt.</item>
    /// <item><b>T fades</b> geometrically, daily, at <see cref="QuarterlyPersistence"/> a quarter (<see cref="AdvanceDay"/>): what fades is handed back to
    /// approval and recorded as the approval ledger's named term `ApprovalAttribution.TaxTermFading` (with B inside the range the hand-back never
    /// meets a bound). <b>The fading is the ONLY way the term's points return</b> (review 878's M-1): approval's reversion reads approval
    /// without the standing term (approval + T), so it neither pays the term back a second time nor overshoots. Its speed and its other terms are
    /// untouched (Q3: the reversion stays out of A-LOOP).</item>
    /// </list>
    /// <para>⚠ <b>Declared readings</b> (the plan leaves them open): a clamped write between a day's start and the boundary formula is re-clamped at
    /// the formula (the reversion reads a base inside the range); a cut and its restore apart in time leave the cut's faded part standing,
    /// m·s·x·(1 − p^days), itself fading - the plan's "|Δapproval| ≤ 0.01" holds for a pair on one day; the income line's unit is its effective rate,
    /// which moves with its base daily, so a band cut and its restore months apart net to a residue of order 1e-3 points, priced at s or m·s (review
    /// 878b's L-7: exact for a flat line only); the preview books the turn's impulse and does not run the coming year's fading.</para>
    /// </summary>
    public static class TaxTerm
    {
        /// <summary>m, <b>the mirror</b>: a cut's approval gain per point, as a fraction of a rise's cost per point (`docs/specs/A_LOOP_PLAN.md` §2 (3)).
        /// The DIRECTION - a cut earns less than a rise costs - is Elias's ruling (2026-10-18; installed with J1, 2026-10-19); the grievance asymmetry
        /// Nannestad &amp; Paldam name is the literature's support, but no reachable page gives a magnitude (§8: their 1997 micro study BILLED).</summary>
        /// <remarks>[AUTHORED-DRAFT] - 0.5 drafted. On the play-calibration list: "q37's mirror `m` (a cut's approval gain per point, as a fraction of a
        /// rise's cost): 0.5 drafted; the direction ruled 2026-10-18. Look for in play: does a tax cut buy anything a player notices, and does the
        /// restoring bill still sting?"</remarks>
        public const float MirrorFraction = 0.5f;

        /// <summary><b>The tax term's persistence a quarter</b>: the share of T standing one quarter later. SOURCED [LBS00] - Lewis-Beck &amp; Stegmaier,
        /// "Economic Determinants of Electoral Outcomes", Annu. Rev. Polit. Sci. 3 (2000) 183-219 (`docs/reference/raw/aloop/almendron_mirror_economicdeterminants.pdf`,
        /// its digest in that folder's `SHA256SUMS.txt`), the coefficient on lagged popularity in the US popularity functions it tabulates (SECONDARY reports of the originals):
        /// <list type="bullet">
        /// <item>TABLE 2 "Later examples of US popularity functions (figures in parentheses are standard errors)": "Popularity lagged 0.81 0.79", "(0.04) (0.04)";
        /// its column (2): "Quarterly presidential popularity (Gallup percentage approval), 1954--1996, predicted from the political variables (), popularity
        /// lagged, ... estimated from OLS. (Erikson et al 2000)".</item>
        /// <item>TABLE 1 "Earlier examples of US popularity functions": "Popularity lagged 0.84 0.89" (Beck 1991; the plan reads 0.84 as quarterly 1953-1986, 0.89 monthly).</item>
        /// </list>
        /// The quarterly ones span 0.79-0.84; 0.81 is Table 2's. A one-off impulse halves in ln 0.5 ⁄ ln 0.81 ≈ 3.29 quarters, some 300 days.
        /// <b>TRANSFERRED</b>: the figure is US presidential approval's, carried to every country - the authored part. On the play-calibration list:
        /// "q37's tax-term persistence: 0.81 a quarter, from the US popularity functions [LBS00] Table 2 (0.79-0.84 across the tabulated quarterly ones),
        /// transferred to every country. Look for in play: a year after a rise, is the player still being punished for it?"</summary>
        public const float QuarterlyPersistence = 0.81f;

        /// <summary>DERIVED: a quarter of the game's year, `SimulationManager.DaysPerTurn` days to the year.</summary>
        public const float DaysPerQuarter = SimulationManager.DaysPerTurn / 4f;

        /// <summary>DERIVED: the daily persistence, <see cref="QuarterlyPersistence"/> to the power 1 ⁄ <see cref="DaysPerQuarter"/>, so
        /// <see cref="DaysPerQuarter"/> days of it are one quarter's.</summary>
        public static readonly float DailyPersistence = Mathf.Pow(QuarterlyPersistence, 1f / DaysPerQuarter);

        /// <summary>CONVENTION - an epsilon, not a claim about the world: a stock smaller than this in approval points is handed back whole and T
        /// set to zero, so a faded term stops writing approval rather than shrinking through the denormals for a century.</summary>
        private const float SettledBelowPoints = 1e-6f;

        /// <summary>The half-life the persistence declares, in days - DERIVED: ln 0.5 ⁄ ln <see cref="DailyPersistence"/>.</summary>
        public static double HalfLifeDays => Math.Log(0.5) / Math.Log(DailyPersistence);

        /// <summary>P(net): the year's charged position of a line standing <paramref name="netPoints"/> off its opening, in approval points held
        /// against the government - s·x for a rise, m·s·x for a cut (negative: approval gained).</summary>
        public static float Charge(float netPoints)
            => netPoints >= 0f ? MacroSystem.TaxHikeApprovalSensitivity * netPoints : MirrorFraction * MacroSystem.TaxHikeApprovalSensitivity * netPoints;

        /// <summary>The line's booked net this fiscal year, in the term's unit - 0 where the government has moved it by nothing since the opening.</summary>
        public static float NetPoints(Country country, TaxType type)
            => country.TaxTermNet != null && country.TaxTermNet.TryGetValue(type, out float net) ? net : 0f;

        /// <summary>A move the government makes, from <paramref name="fromPoints"/> to <paramref name="toPoints"/> (`ParliamentSystem.TaxLevelPoints`, read on
        /// either side of the write): adds it to the line's net and returns its impulse P(net after) − P(net before), in approval points (positive: approval
        /// lost). Nothing is charged or booked into T here - the caller charges approval and books what landed.</summary>
        public static float Move(Country country, TaxType type, float fromPoints, float toPoints)
        {
            float before = NetPoints(country, type);
            float after = before + (toPoints - fromPoints);
            if (country.TaxTermNet == null) { country.TaxTermNet = new Dictionary<TaxType, float>(); }
            country.TaxTermNet[type] = after;
            return Charge(after) - Charge(before);
        }

        /// <summary>A bill's or a levy's impulse charged to approval at once, and what LANDED booked into T: the whole impulse where approval stays inside
        /// [0, 100], the approval actually moved where the clamp binds (review 878's M-2). The caller records the approval move around it.</summary>
        public static void ChargeApproval(Country country, float impulse)
        {
            Reclamp(country);
            float before = country.State.ApprovalRating;
            float wanted = before - impulse;
            if (wanted >= 0f && wanted <= 100f)
            {
                country.State.ApprovalRating = wanted;
                country.TaxTermStock += impulse;
                return;
            }
            country.State.ApprovalRating = Mathf.Clamp(wanted, 0f, 100f);
            country.TaxTermStock = Mathf.Clamp(before + country.TaxTermStock, 0f, 100f) - country.State.ApprovalRating;   // the base (approval + T) is the run without the move, which a bill leaves where it was
        }

        /// <summary>Review 878b's M-5: <b>the base B = approval + T is the approval of the run without the term, and it lives in [0, 100] as approval
        /// does</b>. Where a clamped write (an event, a cabinet or foreign-policy shock, a failed bill's cost, the formula) has pushed approval into a
        /// bound while T stood in the bound's direction, the run without the term sits at the same bound, so the part of T past it is forgiven:
        /// T = Clamp(approval + T, 0, 100) − approval. Approval does not move, so nothing is recorded. Writes T only where B stands outside the range,
        /// so a T of zero, or a base inside the range, is untouched to the bit.</summary>
        public static void Reclamp(Country country)
        {
            float a = country.State.ApprovalRating, b = a + country.TaxTermStock;
            if (b < 0f || b > 100f) { country.TaxTermStock = Mathf.Clamp(b, 0f, 100f) - a; }
        }

        /// <summary>Review 879b's M-1: <b>every clamped approval writer that is not the tax term's own</b> (an event, a cabinet decision or leak, a foreign
        /// meeting, a failed bill's cost, a law's enactment cost) writes through here: approval′ = Clamp(approval + <paramref name="delta"/>, 0, 100), the
        /// writer's own expression, and the run without the term takes the same shock through its own clamp - T′ = Clamp(approval + T + delta, 0, 100) −
        /// approval′ - wherever either clamp binds. <see cref="Reclamp"/> sees only a T standing TOWARD the bound; a shock that meets a bound AGAINST T
        /// (a rise at low approval, then a negative event) clamps approval while the base stays inside the range, and only the shock itself can tell.
        /// With T zero, or neither clamp binding, approval is written exactly as the writer wrote it and T is untouched to the bit. Nothing is recorded:
        /// each writer records its own move around the call, as before.</summary>
        public static void Shock(Country country, float delta)
        {
            float a = country.State.ApprovalRating;
            country.State.ApprovalRating = Mathf.Clamp(a + delta, 0f, 100f);
            AfterShock(country, a, delta);
        }

        /// <summary>As <see cref="Shock"/>, for a writer that has already written approval with its own expression (kept as it was, so its rounding is
        /// the old one): <paramref name="approvalBefore"/> is approval before that write, <paramref name="delta"/> the shock it meant. Adjusts T only.</summary>
        public static void AfterShock(Country country, float approvalBefore, float delta)
        {
            float stock = country.TaxTermStock;
            if (stock == 0f) { return; }
            float wanted = approvalBefore + delta, baseWanted = approvalBefore + stock + delta;
            if (wanted < 0f || wanted > 100f || baseWanted < 0f || baseWanted > 100f) { country.TaxTermStock = Mathf.Clamp(baseWanted, 0f, 100f) - country.State.ApprovalRating; }
        }

        /// <summary>The turn's impulse, charged inside the boundary formula (`MacroSystem.ApplyApprovalRating`), booked into T AFTER the formula and its
        /// ledger twin (both read T before it - approval's reversion reads approval + T). <b>The contract</b> (review 878b's M-5, which generalises the
        /// same-direction rule of review 878's M-2): T after = the run without the term less the run with it, each through the formula's own clamp -
        /// <c>T = Clamp(approvalBefore + T + (FormulaTermSum + impulse), 0, 100) − ApprovalAfterFormula</c>, where FormulaTermSum carries the impulse
        /// as −impulse (`ApprovalAttribution.TaxHikePenalty`), so FormulaTermSum + impulse is the formula's step with the term absent. Where nothing binds
        /// - the formula left approval strictly inside the range and the base's step stays inside it - this is T + impulse, booked exactly as that. It
        /// reduces to "the clamp's truncation is taken from the impulse first, never past zero" in the two same-direction quadrants (a penalty at the
        /// floor, a mirror at the ceiling), and forgives in the two opposite ones (a rise at the ceiling, a cut at the floor, where the run without the
        /// move stands at the same bound).</summary>
        public static void BookFormula(Country country, float impulse, float approvalBeforeFormula, ApprovalAttribution ledger)
        {
            if (ledger == null) { country.TaxTermStock += impulse; return; }
            float after = ledger.ApprovalAfterFormula;
            float baseAfter = approvalBeforeFormula + country.TaxTermStock + (ledger.FormulaTermSum + impulse);
            if (after > 0f && after < 100f && baseAfter >= 0f && baseAfter <= 100f) { country.TaxTermStock += impulse; return; }
            country.TaxTermStock = Mathf.Clamp(baseAfter, 0f, 100f) - after;
        }

        /// <summary>q34's levy or removal (`ParliamentSystem.ApplyTaxProgramBillResult`'s move): its impulse charged and what landed booked, where the
        /// government bears the bill (`Congress.GovernmentBears`, review 878's L-1: a congressional majority's levy is no move of the president's and
        /// enters no net); nothing where the bill moved nothing. The caller records the bill's approval move around it.</summary>
        public static void ChargeLevyMove(Country country, TaxLevyMove move, bool governmentBears)
        {
            if (!move.Moved || !governmentBears) { return; }
            ChargeApproval(country, Move(country, move.Type, move.FromPoints, move.ToPoints));
        }

        /// <summary>True on the country's fiscal-year start (`FiscalYearData.GetFiscalYearStart`).</summary>
        public static bool IsFiscalYearStart(CountryId id, DateTime date)
        {
            (int month, int day) = FiscalYearData.GetFiscalYearStart(id);
            return date.Month == month && date.Day == day;
        }

        /// <summary>The day's step, first in the country's day (`SimulationManager.AdvanceDay`): on the fiscal year's first day the year's net is emptied -
        /// every line stands at its opening - before any of the day's moves; then T fades by <see cref="DailyPersistence"/> and what fades is handed back to
        /// approval and recorded as the ledger's `TaxTermFading`. Before the fading the base is re-clamped (<see cref="Reclamp"/>, M-5); the hand-back then
        /// stays inside the range, and where it would not, T shrinks only by what was handed back. Nothing is
        /// written while T is zero.</summary>
        public static void AdvanceDay(Country country, DateTime date)
        {
            if (country.TaxTermNet != null && IsFiscalYearStart(country.Id, date)) { country.TaxTermNet = null; }
            if (country.TaxTermStock == 0f) { return; }
            Reclamp(country);   // M-5: what a clamped write absorbed since yesterday is forgiven before the fading - the hand-back below then never meets a bound
            float before = country.TaxTermStock;
            if (before == 0f) { return; }
            float target = Mathf.Abs(before) < SettledBelowPoints ? 0f : before * DailyPersistence;
            float approvalBefore = country.State.ApprovalRating;
            float wanted = approvalBefore + (before - target);
            if (wanted >= 0f && wanted <= 100f)
            {
                country.State.ApprovalRating = wanted;
                country.TaxTermStock = target;
            }
            else
            {
                country.State.ApprovalRating = Mathf.Clamp(wanted, 0f, 100f);
                country.TaxTermStock = before - (country.State.ApprovalRating - approvalBefore);
            }
            ApprovalLedgerRecorder.RecordTaxTermFading(country, date, country.State.ApprovalRating - approvalBefore);
        }
    }
}
