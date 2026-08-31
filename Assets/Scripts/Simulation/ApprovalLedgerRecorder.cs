using System;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Step 2's ledger lifecycle: events accrue during a period by OBSERVATION at the call sites
    /// (snapshot approval around the write, record the applied post-clamp delta with date and
    /// label - zero signature changes in the political systems, and it records what the
    /// simulation DID, the FiscalTurnReport principle); the boundary formula records its terms
    /// into the same accruing ledger (see MacroSystem.ApplyApprovalRating); then
    /// <see cref="CloseAtBoundary"/> runs the SELF-AUDIT and swaps closed-for-accruing.
    ///
    /// <para><b>The self-audit</b>: |observed period Δ − (events + terms + clamp)| must sit
    /// inside float-noise tolerance at EVERY boundary. A failure logs loudly with the ATTRIB:
    /// prefix and degrades (the class-8 lesson: never throw mid-simulation) - the matrix bar
    /// greps for that prefix, so a red audit cannot pass validation silently.</para>
    /// </summary>
    public static class ApprovalLedgerRecorder
    {
        /// <summary>CONVENTION - a tolerance, not a claim about the world. Audit tolerance in approval
        /// points: float addition noise across ~15 terms is orders of magnitude below this; a real
        /// attribution miss (an unrecorded writer) is orders of magnitude above it. **The gap between
        /// those two magnitudes is what makes the value uncritical** - anything between roughly 1e-5 and
        /// 1e-1 would separate the same two cases.</summary>
        private const float AuditTolerance = 0.001f;

        /// <summary>Opens the accruing ledger at <paramref name="openingApproval"/> if none exists.
        /// Every caller passes the approval AS IT WAS BEFORE ITS OWN WRITE - the same by-construction
        /// closure DebtLedgerRecorder.EnsureAccruing has carried since 2026-08-18, brought to the
        /// approval side by the 2026-08-25 playtest finding: Sweden's FIRST boundary failed its audit
        /// by exactly +1.5000 because a foreign-policy meeting resolved mid-first-year was this
        /// ledger's first touch, and the old parameterless lazy-create opened the window at the
        /// POST-write approval - the event in Events, its effect outside [open, close].</summary>
        public static ApprovalAttribution EnsureAccruing(Country country, DateTime date, float openingApproval)
        {
            if (country.ApprovalLedgerAccruing == null)
            {
                country.ApprovalLedgerAccruing = new ApprovalAttribution
                {
                    PeriodOpenDate = date,
                    ApprovalAtPeriodOpen = openingApproval
                };
            }

            return country.ApprovalLedgerAccruing;
        }

        /// <summary>Records one observed event write. Zero deltas are skipped - a writer that
        /// moved nothing (e.g. a fully clamped shock at the 0 floor) still moved nothing, and
        /// zero rows are noise, not honesty. Opens the ledger at the PRE-write approval (current
        /// minus the applied delta) if this is the period's first touch - the first-touch window
        /// class, closed by construction exactly as the debt twin closes it.</summary>
        public static void RecordEvent(Country country, DateTime date, string label, float appliedDelta)
        {
            if (appliedDelta == 0f)
            {
                return;
            }

            EnsureAccruing(country, date, country.State.ApprovalRating - appliedDelta).Events.Add(new ApprovalEventRecord
            {
                Date = date,
                Label = label,
                AppliedDelta = appliedDelta
            });
        }

        /// <summary>
        /// Closes the period: audits Σ(events)+Σ(terms)+clamp against the observed movement,
        /// promotes the accruing ledger to <see cref="Country.ApprovalLedgerLastPeriod"/>, and
        /// opens a fresh accruing ledger from the post-boundary approval. Called from
        /// AdvanceTurn only - the preview closes nothing (its clone's accruing ledger is read
        /// directly by the parity diagnostic and PolicyPreview).
        /// </summary>
        public static void CloseAtBoundary(Country country, DateTime date)
        {
            // A null ledger at close is degenerate (a period opening and closing in the same
            // instant) - the current approval IS the pre-write value here, since the close writes
            // nothing before reading it.
            ApprovalAttribution ledger = EnsureAccruing(country, date, country.State.ApprovalRating);
            ledger.PeriodCloseDate = date;
            ledger.ApprovalAtClose = country.State.ApprovalRating;
            ledger.Closed = true;

            float observed = ledger.ApprovalAtClose - ledger.ApprovalAtPeriodOpen;
            float explained = ledger.EventSum + ledger.TermSum + ledger.ClampLoss;
            if (Mathf.Abs(observed - explained) > AuditTolerance)
            {
                Debug.LogError($"ATTRIB: {country.Id} approval ledger FAILS its audit at {date:yyyy-MM-dd}: " +
                               $"observed Δ {observed:F4} vs explained {explained:F4} " +
                               $"(events {ledger.EventSum:F4} + terms {ledger.TermSum:F4} + clamp {ledger.ClampLoss:F4}). " +
                               "An approval writer is not recording - find it; do not tune the tolerance.");
            }

            // The twin-drift detector: ClampLoss is defined as observed-minus-terms, so the sum
            // audit above is exact by construction on the formula side - its remaining teeth are
            // the events. THIS check is the teeth for the recomputed terms (see
            // RecordApprovalAttribution's twin warning): the [0,100] clamp can only have bound
            // if the formula LEFT approval at exactly 0 or 100 - tested on ApprovalAfterFormula,
            // never on the close-time value, which a boundary event may have pulled back inside
            // the range (61 measured false positives on the close-time form, 2026-08-18). Away
            // from a bound, a non-trivial ClampLoss means the recomputation drifted.
            bool formulaClamped = ledger.ApprovalAfterFormula <= 0f || ledger.ApprovalAfterFormula >= 100f;
            if (!formulaClamped && Mathf.Abs(ledger.ClampLoss) > AuditTolerance)
            {
                Debug.LogError($"ATTRIB: {country.Id} approval TERM RECOMPUTATION drifted at {date:yyyy-MM-dd}: " +
                               $"clamp term {ledger.ClampLoss:F5} with the formula leaving approval at {ledger.ApprovalAfterFormula:F2}, " +
                               "not at a bound. RecordApprovalAttribution no longer mirrors ApplyApprovalRating - re-twin them.");
            }

            country.ApprovalLedgerLastPeriod = ledger;
            country.ApprovalLedgerAccruing = new ApprovalAttribution
            {
                PeriodOpenDate = date,
                ApprovalAtPeriodOpen = country.State.ApprovalRating
            };
        }
    }
}
