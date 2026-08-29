using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E3 — everything the action screen draws. PURE DATA (R-N2), assembled by the harness and
    /// handed to the view, which computes nothing of its own.
    ///
    /// It CONTAINS a <see cref="CampaignSnapshot"/> rather than duplicating it, so the masthead, the
    /// resource figures and the campaign-window strip are the same values Campaign HQ shows. Two
    /// screens disagreeing about how much money the campaign has would be worse than either being
    /// wrong alone.
    ///
    /// **Every option carries its own estimate, not just the selected one.** The decision this
    /// screen exists to serve is *which* action to run, and that is a comparison; a screen that can
    /// only price the row you already clicked makes the player click all eight to find out. The
    /// estimate is a <see cref="CampaignActions.ChainBand"/> in every case, so the view could not
    /// print a false point estimate even if a later edit tried to.
    /// </summary>
    public readonly struct ActionScreenSnapshot
    {
        public readonly CampaignSnapshot Campaign;
        public readonly ActionOption[] Options;
        /// <summary>Index into <see cref="Options"/> of the action being detailed; -1 for none selected.</summary>
        public readonly int SelectedIndex;
        public readonly EstimateProvenance Provenance;

        public ActionScreenSnapshot(CampaignSnapshot campaign, ActionOption[] options, int selectedIndex,
            EstimateProvenance provenance)
        {
            Campaign = campaign; Options = options; SelectedIndex = selectedIndex; Provenance = provenance;
        }

        public bool HasSelection => SelectedIndex >= 0 && Options != null && SelectedIndex < Options.Length;

        public ActionOption Selected => Options[SelectedIndex];

        public CampaignActions.ChainBand Estimate => Selected.Estimate;

        /// <summary>
        /// The largest high-end estimate across every option, so the per-row bands can share one
        /// scale and be COMPARED. A per-row scale would make every action look equally promising,
        /// which is the opposite of what a comparison is for.
        /// </summary>
        public double PersuasionScale
        {
            get
            {
                double top = 0.0;
                if (Options == null) { return 1.0; }

                foreach (ActionOption option in Options)
                {
                    if (option.Estimate.Measured && option.Estimate.High.Persuasion > top)
                    {
                        top = option.Estimate.High.Persuasion;
                    }
                }

                return top > 0.0 ? top : 1.0;
            }
        }
    }

    /// <summary>One action the player could run today: its spec's costs, its target, whether it is affordable, and what it would do.</summary>
    public readonly struct ActionOption
    {
        public readonly CampaignActionKind Kind;
        public readonly string TargetLabel;
        public readonly double MoneyCost;
        public readonly double Hours;
        /// <summary>Both resources available. `ResourcePool.TrySpend` REFUSES rather than clamping (W-B2), so this is a real gate, not a warning.</summary>
        public readonly bool Affordable;
        public readonly CampaignActions.ChainBand Estimate;

        public ActionOption(CampaignActionKind kind, string targetLabel, double moneyCost, double hours,
            bool affordable, CampaignActions.ChainBand estimate)
        {
            Kind = kind; TargetLabel = targetLabel; MoneyCost = moneyCost; Hours = hours;
            Affordable = affordable; Estimate = estimate;
        }
    }

    /// <summary>
    /// Where the estimate's uncertainty comes from — so the range on screen can be attributed rather
    /// than merely displayed. A range with no stated provenance is indistinguishable from a
    /// decorative ±, which is exactly what W-E3 forbids.
    /// </summary>
    public readonly struct EstimateProvenance
    {
        public readonly string PollHouse;
        public readonly int SampleSize;
        public readonly DateTime FieldDate;
        /// <summary>Half-width of the measured 95 % interval on issue salience, on the 0–1 scale.</summary>
        public readonly double SalienceError;
        public readonly double MatchError;
        /// <summary>§36: false when regional detail was never bought, so the region's own figures are unknown.</summary>
        public readonly bool RegionalDetailBought;

        public EstimateProvenance(string pollHouse, int sampleSize, DateTime fieldDate,
            double salienceError, double matchError, bool regionalDetailBought)
        {
            PollHouse = pollHouse; SampleSize = sampleSize; FieldDate = fieldDate;
            SalienceError = salienceError; MatchError = matchError;
            RegionalDetailBought = regionalDetailBought;
        }
    }
}
