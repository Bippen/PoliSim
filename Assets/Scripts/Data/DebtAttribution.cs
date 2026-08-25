using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// One debt-stock event as it LANDED: the post-clamp change in GovernmentDebt observed at the
    /// write site (an interrupt-layer BudgetImpact, a scenario's seed delta), with the day it
    /// happened and a label naming the writer. Recorded by observation, never recomputed - the
    /// same principle ApprovalEventRecord follows, in dollars (billions) instead of points.
    /// </summary>
    public class DebtEventRecord
    {
        public DateTime Date;
        public string Label;
        public float AppliedDelta;
    }

    /// <summary>
    /// One fiscal period's debt attribution - Step 2's THIRD section (the fiscal chain), built
    /// 2026-08-25 on the trigger Step 2 named for it ("the first playtest asking why did the
    /// deficit move"), which Italy Debt Crisis's own playtest session fired.
    ///
    /// <para><b>The honesty classes govern what each field may claim</b> (the scoping package's
    /// §1). The stock update is daily (`ApplyRevenueAndSpending`, one slice per day), so every
    /// term here is the SUM over the period's days of a quantity OBSERVED at the write site on
    /// the actual daily stock - which is what makes the debt step decompose EXACTLY rather than
    /// approximately: the offline erosion-standard decomposition estimated π·b on an average
    /// stock and carried an interleaving remainder; this ledger records the erosion the model
    /// actually applied to the stock it actually had, day by day, so the compounding class's
    /// residual is EMPTY by construction here and the only remainder is float noise, carried in
    /// <see cref="ClampLoss"/> under the same observed-minus-terms definition the approval
    /// ledger uses. The five term fields are Class A by observation; <see cref="Events"/> is
    /// Class B (dated, face-valued, post-clamp); <see cref="FiscalReactionMultiplier"/> is the
    /// Class C stance the period consumed (frozen at open - FiscalPeriod's own rule), shown as
    /// the number the identity used, never the live one.</para>
    ///
    /// <para><b>The self-audit identity</b>: DebtAtClose − DebtAtPeriodOpen == Σ(term fields) +
    /// Σ(Events.AppliedDelta) + ClampLoss, asserted at every boundary by
    /// <c>DebtLedgerRecorder.CloseAtBoundary</c> with the ATTRIB: prefix the matrix bar greps.</para>
    ///
    /// <para><b>The ratio's own identity</b>, exact with no residual: Δratio =
    /// ΔDebt / GdpAtClose + DebtAtPeriodOpen × (1/GdpAtClose − 1/GdpAtPeriodOpen) - the stock
    /// change at closing GDP, plus GDP's movement diluting the opening stock. Both from the four
    /// recorded values; GDP's OWN drivers are Class D and are not this section's claim.</para>
    ///
    /// Lives on <see cref="Country"/> (never EconomyState - the trajectory dump reflects
    /// EconomyState's public fields, and recording is OBSERVATION: it must not change the dump).
    /// Persisted in the save per R-S2e; an old save's null degrades to "no period recorded yet".
    /// </summary>
    public class DebtAttribution
    {
        public DateTime PeriodOpenDate;
        public DateTime PeriodCloseDate;
        public bool Closed;

        // The four recorded anchors of both identities. Debt and GDP in the model's own units
        // (billions); ratios recorded at the instant, not recomputed for display.
        public float DebtAtPeriodOpen;
        public float DebtAtClose;
        public float GdpAtPeriodOpen;
        public float GdpAtClose;
        public float RatioAtPeriodOpen;
        public float RatioAtClose;

        // Class C - the period's stance and the rate pair at open and close, for the trailing
        // text beside the terms. Recorded on the first and last day's observation.
        public float FiscalReactionMultiplier;
        public float IssuanceRateAtOpen;
        public float IssuanceRateAtClose;
        public float EffectiveRateAtOpen;
        public float EffectiveRateAtClose;
        public float InflationAtOpen;
        public float InflationAtClose;

        // Class A by observation - signed as CONTRIBUTIONS TO THE DEBT CHANGE (positive raises
        // debt), summed over the period's daily slices.
        /// <summary>−Σ(primary balance before the fiscal reaction): revenue at stance 1 minus
        /// non-interest spending. A primary surplus reduces debt, so it lands negative here.</summary>
        public float PrimaryBalanceEffect;
        /// <summary>−Σ(the fiscal reaction's revenue effect): revenue × (1 − 1/m). Tightening
        /// (m &gt; 1) raises revenue and lands negative; loosening lands positive - the FRF's
        /// give-back, made visible.</summary>
        public float FiscalReactionEffect;
        /// <summary>+Σ(debt × the ISSUANCE rate slice) - what the stock would have paid at today's
        /// spot price, every day.</summary>
        public float InterestAtIssuance;
        /// <summary>+Σ(interest actually charged − interest at issuance): the maturity lag's
        /// effective-rate term. Negative when the blended rate the stock still pays sits below
        /// today's issuance price; positive when it sits above (the lag slows the benefit of cuts
        /// exactly as it slows the pain of hikes).</summary>
        public float RateLagEffect;
        /// <summary>Σ(debt × (erosionFactor − 1)) - the −π·b term as the model actually applied
        /// it to the actual daily stock. Negative under inflation; positive under deflation;
        /// symmetric on a net-creditor position per ruling R3.</summary>
        public float Erosion;

        /// <summary>Observed daily change minus the recorded daily terms, summed: the guard/ceiling
        /// clamp's truncation when it bound (see <see cref="ClampBoundDays"/>), float rounding
        /// otherwise - the approval ledger's ClampLoss definition, applied to the stock. The
        /// twin-drift detector fires when this is non-trivial on a period the clamp never bound.</summary>
        public float ClampLoss;

        /// <summary>Days on which the stock update landed ON the runaway guard or the ceiling -
        /// the only days a non-trivial ClampLoss is legitimate.</summary>
        public int ClampBoundDays;

        /// <summary>Daily observations recorded this period - 365 for a full one; fewer for the
        /// opening period of a new game or a period cut by a load. Zero means the ledger opened
        /// but no slice has been observed yet.</summary>
        public int DaysRecorded;

        // Class B - dated, labeled, post-clamp actuals.
        public List<DebtEventRecord> Events = new List<DebtEventRecord>();

        public float TermSum =>
            PrimaryBalanceEffect + FiscalReactionEffect + InterestAtIssuance + RateLagEffect + Erosion;

        public float EventSum
        {
            get
            {
                float sum = 0f;
                foreach (DebtEventRecord e in Events) { sum += e.AppliedDelta; }
                return sum;
            }
        }
    }
}
