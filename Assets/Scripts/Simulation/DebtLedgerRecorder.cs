using System;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Step 2's third section (2026-08-25): the debt ledger's lifecycle, on ApprovalLedgerRecorder's
    /// exact shape. The stock moves DAILY (one ApplyRevenueAndSpending slice per day), so unlike
    /// approval's single boundary formula the terms here accrue by OBSERVATION one day at a time -
    /// <see cref="RecordDay"/> is called from AccrueDailyFiscalFlows with the values that call
    /// site already holds (the FiscalTurnReport principle: recorded, not recomputed), and
    /// <see cref="CloseAtBoundary"/> runs the SELF-AUDIT where the FiscalTurnReport itself closes.
    ///
    /// <para><b>Why observation makes the decomposition exact where the offline one was not.</b>
    /// The erosion-standard decompositions (CLAUDE.md, "The erosion term ships") reconstructed a
    /// period's debt step as π·b on an average stock plus an interleaving remainder. The daily
    /// write is `debt = debt × e − balance`; observing `debt × (e − 1)` on the ACTUAL stock each
    /// day and summing puts the interleaving inside the term rather than beside it. What remains
    /// is float rounding, carried in ClampLoss (observed − recorded, per day, in double) under the
    /// approval ledger's own definition - so the compounding class's "named residual" line exists
    /// structurally, and reads as the clamp when the clamp bound and as noise otherwise.</para>
    ///
    /// <para><b>The observation gate</b>: nothing here writes EconomyState; every input is a value
    /// the daily site already computed or a pure function of state it already read. The recorded
    /// split (revenue into stance-1 revenue + reaction effect; interest into issuance-rate interest
    /// + lag) is arithmetic on returned values, never a second run of the formula. The one
    /// recomputation - the erosion factor - is the same expression as the model's, and the audit
    /// is what enforces that it stays so (the twin-drift detector below).</para>
    ///
    /// <para><b>The self-audit</b>: |observed period Δ − (terms + events + clamp)| within a
    /// stock-scaled tolerance at EVERY boundary; failure logs with the ATTRIB: prefix and degrades
    /// (never throws mid-simulation), so the matrix bar's grep catches a red audit.</para>
    /// </summary>
    public static class DebtLedgerRecorder
    {
        /// <summary>Relative tolerance on the larger of the period's open/close stock. float32
        /// rounding on a stock of tens of thousands (the USA's) is ~0.004 per daily update and
        /// compounds across 365 slices to at most a few units - far below 2e-4 of the stock; an
        /// unrecorded writer (an interrupt impact is ±hundreds, a scenario seed thousands) is far
        /// above it. Floored so a near-zero stock still has a real gate.</summary>
        private const float RelativeTolerance = 2e-4f;
        /// <summary>CONVENTION - the absolute floor under the relative tolerance above, so a near-zero debt stock still has a real gate rather than an unreachable one.</summary>
        private const float MinimumTolerance = 0.01f;

        /// <summary>Opens the accruing ledger at <paramref name="openingDebt"/> if none exists.
        /// Every writer passes the stock AS IT WAS BEFORE ITS OWN WRITE, so a ledger whose first
        /// touch is an event still opens at the pre-event value - the first-boundary-open class
        /// the approval ledger's audit caught (2026-08-18), closed here by construction.</summary>
        public static DebtAttribution EnsureAccruing(Country country, DateTime date, float openingDebt)
        {
            if (country.FiscalLedgerAccruing == null)
            {
                EconomyState state = country.State;
                country.FiscalLedgerAccruing = new DebtAttribution
                {
                    PeriodOpenDate = date,
                    DebtAtPeriodOpen = openingDebt,
                    GdpAtPeriodOpen = state.NominalGdp,
                    RatioAtPeriodOpen = state.NominalGdp > 0f ? openingDebt / state.NominalGdp * 100f : 0f,
                    InflationAtOpen = state.Inflation
                };
            }

            return country.FiscalLedgerAccruing;
        }

        /// <summary>
        /// One daily slice, observed at AccrueDailyFiscalFlows' own write. The split, in double so
        /// the recorded terms sum to the observed change to well inside the audit tolerance:
        /// revenue = stanceOneRevenue + reactionEffect (revenue/m and the remainder); interest =
        /// interestAtIssuance + lag (the remainder); primary = stanceOneRevenue − non-interest
        /// spending; erosion = debt × (e − 1); residual = observed − (erosion − balance), which is
        /// the clamp's truncation on a bound day and rounding otherwise.
        /// </summary>
        public static void RecordDay(Country country, DateTime date, float debtBefore, float debtAfter,
            float erosionFactor, float revenue, float totalSpending, float interestOnDebt,
            float interestAtIssuance, float budgetBalance, float fiscalReactionMultiplier,
            float issuanceRate, float effectiveRate, bool clampBound)
        {
            DebtAttribution ledger = EnsureAccruing(country, date, debtBefore);

            double stanceOneRevenue = fiscalReactionMultiplier > 0f ? revenue / (double)fiscalReactionMultiplier : revenue;
            double reactionEffect = revenue - stanceOneRevenue;
            double nonInterestSpending = (double)totalSpending - interestOnDebt;
            double primaryBalance = stanceOneRevenue - nonInterestSpending;
            double lag = (double)interestOnDebt - interestAtIssuance;
            double erosion = (double)debtBefore * ((double)erosionFactor - 1.0);
            double observed = (double)debtAfter - debtBefore;
            double residual = observed - (erosion - budgetBalance);

            ledger.PrimaryBalanceEffect += (float)(-primaryBalance);
            ledger.FiscalReactionEffect += (float)(-reactionEffect);
            ledger.InterestAtIssuance += interestAtIssuance;
            ledger.RateLagEffect += (float)lag;
            ledger.Erosion += (float)erosion;
            ledger.ClampLoss += (float)residual;
            if (clampBound) { ledger.ClampBoundDays++; }

            if (ledger.DaysRecorded == 0)
            {
                ledger.IssuanceRateAtOpen = issuanceRate;
                ledger.EffectiveRateAtOpen = effectiveRate;
            }

            // The stance is frozen for the period (FiscalPeriod's own rule), so every day carries
            // the same value; recording it daily rather than once keeps this method the single
            // writer of the ledger's Class C fields.
            ledger.FiscalReactionMultiplier = fiscalReactionMultiplier;
            ledger.IssuanceRateAtClose = issuanceRate;
            ledger.EffectiveRateAtClose = effectiveRate;
            ledger.InflationAtClose = country.State.Inflation;
            ledger.DaysRecorded++;
        }

        /// <summary>Records one observed off-path write to the stock (an interrupt impact, a
        /// scenario seed) as a dated Class B event. Zero deltas are skipped - a writer that moved
        /// nothing still moved nothing. Opens the ledger at the PRE-write value if this is the
        /// period's first touch.</summary>
        public static void RecordEvent(Country country, DateTime date, string label, float debtBefore, float debtAfter)
        {
            float appliedDelta = debtAfter - debtBefore;
            if (appliedDelta == 0f)
            {
                return;
            }

            EnsureAccruing(country, date, debtBefore).Events.Add(new DebtEventRecord
            {
                Date = date,
                Label = label,
                AppliedDelta = appliedDelta
            });
        }

        /// <summary>
        /// Closes the period where the FiscalTurnReport closes it: audits Σ(terms) + Σ(events) +
        /// clamp against the observed stock movement, promotes the accruing ledger to
        /// <see cref="Country.FiscalLedgerLastPeriod"/>, and opens a fresh accruing ledger at the
        /// post-boundary stock. Called from ApplyDomesticPolicy only - the preview closes nothing
        /// and records nothing (it never runs the daily path).
        /// </summary>
        public static void CloseAtBoundary(Country country, DateTime date, float issuanceRateNow, float effectiveRateNow)
        {
            EconomyState state = country.State;
            DebtAttribution ledger = EnsureAccruing(country, date, state.GovernmentDebt);
            ledger.PeriodCloseDate = date;
            ledger.DebtAtClose = state.GovernmentDebt;
            ledger.GdpAtClose = state.NominalGdp;
            ledger.RatioAtClose = state.DebtToGdpRatio;
            ledger.InflationAtClose = state.Inflation;
            if (ledger.DaysRecorded == 0)
            {
                // A boundary with no slice observed (a load landing exactly on one): the rate
                // pair is the close-time pair on both ends - stated, not left at zero.
                ledger.IssuanceRateAtOpen = issuanceRateNow;
                ledger.EffectiveRateAtOpen = effectiveRateNow;
            }
            ledger.IssuanceRateAtClose = issuanceRateNow;
            ledger.EffectiveRateAtClose = effectiveRateNow;
            ledger.Closed = true;

            float tolerance = Mathf.Max(MinimumTolerance,
                RelativeTolerance * Mathf.Max(Mathf.Abs(ledger.DebtAtPeriodOpen), Mathf.Abs(ledger.DebtAtClose)));
            float observed = ledger.DebtAtClose - ledger.DebtAtPeriodOpen;
            float explained = ledger.TermSum + ledger.EventSum + ledger.ClampLoss;
            if (Mathf.Abs(observed - explained) > tolerance)
            {
                Debug.LogError($"ATTRIB: {country.Id} DEBT ledger FAILS its audit at {date:yyyy-MM-dd}: " +
                               $"observed Δ {observed:F4} vs explained {explained:F4} " +
                               $"(terms {ledger.TermSum:F4} + events {ledger.EventSum:F4} + clamp {ledger.ClampLoss:F4}, tolerance {tolerance:F4}, {ledger.DaysRecorded} days). " +
                               "A debt writer is not recording - find it; do not tune the tolerance.");
            }

            // The twin-drift detector: ClampLoss is observed-minus-recorded, so the sum audit above
            // is exact by construction on the daily side - its teeth are the EVENTS. THIS check is
            // the teeth for the recorded terms: away from the guard and the ceiling the daily
            // residual is rounding only, so a non-trivial ClampLoss on a period the clamp never
            // bound means the recorder's erosion expression no longer mirrors the model's.
            if (ledger.ClampBoundDays == 0 && Mathf.Abs(ledger.ClampLoss) > tolerance)
            {
                Debug.LogError($"ATTRIB: {country.Id} DEBT TERM RECOMPUTATION drifted at {date:yyyy-MM-dd}: " +
                               $"clamp/residual {ledger.ClampLoss:F5} with the clamp never bound this period. " +
                               "DebtLedgerRecorder.RecordDay no longer mirrors ApplyRevenueAndSpending's stock update - re-twin them.");
            }

            country.FiscalLedgerLastPeriod = ledger;
            country.FiscalLedgerAccruing = new DebtAttribution
            {
                PeriodOpenDate = date,
                DebtAtPeriodOpen = state.GovernmentDebt,
                GdpAtPeriodOpen = state.NominalGdp,
                RatioAtPeriodOpen = state.DebtToGdpRatio,
                InflationAtOpen = state.Inflation
            };
        }
    }
}
