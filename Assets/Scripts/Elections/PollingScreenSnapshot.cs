using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E4 — everything the polling screen draws. PURE DATA (R-N2); contains a
    /// <see cref="CampaignSnapshot"/> so the last poll, the momentum and the money are the same
    /// values the other two campaign screens show.
    ///
    /// **§21's whole demand is one sentence: "the player should have to decide whether additional
    /// information is worth the cost."** A screen can only serve that if both sides of the trade are
    /// real numbers — kronor on one side, and on the other the precision those kronor actually buy.
    /// So an offer carries its sample size and its price, and every uncertainty figure beside it is
    /// DERIVED from that sample size by `PollingSystem.MarginOfErrorPp`, the same function the real
    /// polls use. Nothing here is a claimed accuracy.
    /// </summary>
    public readonly struct PollingScreenSnapshot
    {
        public readonly CampaignSnapshot Campaign;
        public readonly PollOffer[] Offers;
        /// <summary>The offer whose detail is shown; -1 for none.</summary>
        public readonly int SelectedIndex;
        /// <summary>The share the ± figures are quoted at — a poll's margin depends on the proportion measured, so the screen must say which one.</summary>
        public readonly double QuotedShare;
        public readonly string QuotedPartyName;

        public PollingScreenSnapshot(CampaignSnapshot campaign, PollOffer[] offers, int selectedIndex,
            double quotedShare, string quotedPartyName)
        {
            Campaign = campaign; Offers = offers; SelectedIndex = selectedIndex;
            QuotedShare = quotedShare; QuotedPartyName = quotedPartyName;
        }

        public bool HasSelection => SelectedIndex >= 0 && Offers != null && SelectedIndex < Offers.Length;
    }

    /// <summary>
    /// One purchasable poll (§21). Sample size and price are **[AUTHORED-DRAFT]** — real Swedish
    /// polling prices are W-F5's to source — but the PRECISION is derived, never authored: see
    /// <see cref="MarginOfErrorPp"/>.
    /// </summary>
    public readonly struct PollOffer
    {
        public readonly string Name;
        public readonly int SampleSize;
        public readonly double Cost;
        /// <summary>§21's "regional data" — without it a poll answers only nationally, and §36's regional gate stays shut.</summary>
        public readonly bool RegionalBreakdown;
        /// <summary>§21's "demographic segmentation".</summary>
        public readonly bool DemographicSegmentation;
        /// <summary>§21's "turnout modeling" — the error source §20 names and a headline share cannot show.</summary>
        public readonly bool TurnoutModelling;
        public readonly bool Affordable;

        public PollOffer(string name, int sampleSize, double cost, bool regionalBreakdown,
            bool demographicSegmentation, bool turnoutModelling, bool affordable)
        {
            Name = name; SampleSize = sampleSize; Cost = cost;
            RegionalBreakdown = regionalBreakdown; DemographicSegmentation = demographicSegmentation;
            TurnoutModelling = turnoutModelling; Affordable = affordable;
        }

        /// <summary>
        /// The ± this offer would deliver at a given share — DERIVED by the same
        /// `PollingSystem.MarginOfErrorPp` a conducted poll reports, so the price list cannot promise
        /// a precision the polls do not then deliver.
        /// </summary>
        public double MarginOfErrorPp(double share) => PollingSystem.MarginOfErrorPp(share, SampleSize);

        /// <summary>
        /// Kronor per percentage point of precision GAINED over <paramref name="baseline"/> — the
        /// number that turns §21's question into arithmetic. Positive infinity when an offer buys no
        /// precision at all (it may still buy regional or demographic depth, which this figure
        /// deliberately does not price: those are capabilities, not accuracy, and averaging them into
        /// one score would hide the trade rather than show it).
        /// </summary>
        public double CostPerPointGained(PollOffer baseline, double share)
        {
            double gain = baseline.MarginOfErrorPp(share) - MarginOfErrorPp(share);
            if (gain <= 0.0) { return double.PositiveInfinity; }

            return (Cost - baseline.Cost) / gain;
        }
    }
}
