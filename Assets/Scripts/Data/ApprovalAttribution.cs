using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// One approval event as it LANDED: the post-clamp delta observed at the write site, with the
    /// day it happened and a label naming the writer. Recorded by observation at the call sites
    /// (the FiscalTurnReport principle - recorded, not recomputed), which is what lets the
    /// period's ledger sum EXACTLY to the stat's movement.
    /// </summary>
    public class ApprovalEventRecord
    {
        public DateTime Date;
        public string Label;
        public float AppliedDelta;
    }

    /// <summary>A-LOOP q15 (2026-10-19): net approval (approval less its economy stock, held in [0, 100]) on the first day the country's daily step
    /// saw a stored government, keyed by that government's formation day - the approval term's reference (`Country.VoteApprovalMarks`).</summary>
    public class ApprovalMark
    {
        /// <summary>The stored government's formation day (`GovernmentRecord.FormedOn`) - a new day is a new government.</summary>
        public DateTime FormedOn;
        /// <summary>The day the mark was taken.</summary>
        public DateTime On;
        /// <summary>Net approval that day, in approval points.</summary>
        public double Net;
    }

    /// <summary>
    /// One period's approval attribution - Step 2's ledger, the minimum slice (R-S2d).
    ///
    /// <para><b>The honesty classes govern what each field may claim</b> (the scoping package's
    /// §1): the term fields are Class A - the boundary formula's own named locals, recorded AS
    /// SIGNED CONTRIBUTIONS to the period delta at the moment they are computed, never
    /// recomputed; <see cref="Events"/> is Class B - discrete shocks recorded per event with
    /// their dates; <see cref="ClampLoss"/> is the [0,100] clamp's truncation when it binds,
    /// carried as its own term so the audit identity stays exact.</para>
    ///
    /// <para><b>The self-audit identity</b>: ApprovalAtClose − ApprovalAtPeriodOpen ==
    /// Σ(Events.AppliedDelta) + Σ(term fields) + ClampLoss, asserted at every boundary by
    /// <c>ApprovalLedgerRecorder.CloseAtBoundary</c>. An explanation layer that shares the
    /// model's arithmetic and proves it every period cannot drift from the model.</para>
    ///
    /// Lives on <see cref="Country"/> (never EconomyState - the trajectory dump reflects
    /// EconomyState's public fields, and recording is OBSERVATION: it must not change the dump).
    /// Persisted in the save per R-S2e; an old save's null degrades to "no period recorded yet"
    /// at every reader.
    /// </summary>
    public class ApprovalAttribution
    {
        public DateTime PeriodOpenDate;
        public DateTime PeriodCloseDate;
        public float ApprovalAtPeriodOpen;
        public float ApprovalAtClose;
        public bool Closed;

        // Class A - the formula's terms, signed as contributions to the period delta.
        public float Reversion;
        public float GrowthEffect;
        public float MiseryUnemployment;
        public float MiseryInflation;
        public float MiseryCrime;
        public float MiseryCorruption;
        public float TaxHikePenalty;
        public float SpendingEffect;
        public float WelfareEffect;
        public float PaidLeaveEffect;
        public float DrugPolicyEffect;
        public float GiniEffect;

        /// <summary>A-LOOP q37 (2026-10-19): <b>the tax term's fading</b> - the approval handed back over the period as the tax term T fades
        /// (`TaxTerm.AdvanceDay`), the post-clamp actuals summed day by day. A named term of the period, not of the boundary formula: it is in
        /// <see cref="TermSum"/> and so in the self-audit, and out of <see cref="FormulaTermSum"/>, which the clamp term is measured against. The term's
        /// impulses ride where the moves land: the turn's decision in <see cref="TaxHikePenalty"/>, a bill's in its event.</summary>
        public float TaxTermFading;

        /// <summary>The [0,100] clamp's truncation of the formula delta, 0 when it never bound.</summary>
        public float ClampLoss;

        /// <summary>Approval as the boundary FORMULA left it, before any boundary-day event -
        /// the value whose position at exactly 0 or 100 is what legitimizes a nonzero
        /// <see cref="ClampLoss"/> (a boundary event can pull approval back INSIDE the range
        /// by close time, which is why the twin-drift detector must test this, not
        /// ApprovalAtClose - the matrix measured 61 false positives on the close-time test,
        /// 2026-08-18).</summary>
        public float ApprovalAfterFormula;

        // Class B - dated, labeled, post-clamp actuals.
        public List<ApprovalEventRecord> Events = new List<ApprovalEventRecord>();

        /// <summary>The boundary formula's own terms (`MacroSystem.RecordApprovalAttribution`), which the clamp term is measured against.</summary>
        public float FormulaTermSum =>
            Reversion + GrowthEffect + MiseryUnemployment + MiseryInflation + MiseryCrime
            + MiseryCorruption + TaxHikePenalty + SpendingEffect + WelfareEffect
            + PaidLeaveEffect + DrugPolicyEffect + GiniEffect;

        /// <summary>Every named term of the period: the formula's and the tax term's fading (A-LOOP q37).</summary>
        public float TermSum => FormulaTermSum + TaxTermFading;

        public float EventSum
        {
            get
            {
                float sum = 0f;
                foreach (ApprovalEventRecord e in Events) { sum += e.AppliedDelta; }
                return sum;
            }
        }
    }
}
