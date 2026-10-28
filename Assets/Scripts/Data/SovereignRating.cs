namespace PoliSim.Data
{
    /// <summary>Sovereign credit rating notches, S&amp;P's ladder. Index order IS the ordering - 0 is best.</summary>
    public enum CreditRating
    {
        AAA = 0, AAplus = 1, AA = 2, AAminus = 3,
        Aplus = 4, A = 5, Aminus = 6,
        BBBplus = 7, BBB = 8, BBBminus = 9,
        BBplus = 10, BB = 11, BBminus = 12,
        Bplus = 13, B = 14, Bminus = 15,
        CCC = 16
    }

    /// <summary>Outlook is a separate signal from the rating - a cheap way to telegraph a downgrade before it lands.</summary>
    public enum RatingOutlook { Positive, Stable, Negative }

    /// <summary>
    /// A country's standing sovereign rating, as last set by a scheduled review.
    ///
    /// **Why this is stored rather than computed on demand.** Elias's A1 ruling (2026-08-02) replaced
    /// per-turn recomputation with a scheduled review cycle: agencies review sovereigns on a cadence
    /// rather than re-rating continuously as quarterly figures move, and the scheduled review IS the
    /// real-world mechanism that prevents real-world thrash. "Between reviews the rating is unchanged"
    /// is the point of the design, not a limitation - and an unchanging value has to live somewhere.
    ///
    /// **One reader in the simulation, since MF q33.** It is a derived output written by
    /// `CreditRatingSystem.ReviewIfDue` and read by the UI, and the debt crisis reads it as its
    /// trigger: a review that leaves it at CCC closes the market (K141, `SovereignCrisis`; the USA's read without its reserve discount, K212). Otherwise
    /// it feeds no model input, folds into no ceiling, and draws no randomness.
    /// </summary>
    public class SovereignRatingState
    {
        /// <summary>False until the first scheduled review has run. Callers must handle it rather than assuming AAA - an unrated sovereign is not a top-rated one.</summary>
        public bool HasBeenReviewed;

        public CreditRating Rating;
        public RatingOutlook Outlook;

        /// <summary>When the standing rating was set. Shown to the player so an unchanged rating reads as "reviewed and affirmed" rather than "stale".</summary>
        public System.DateTime LastReviewDate;

        /// <summary>The settled inputs the standing rating was actually derived from, kept for display and for the harness's own checks. Storing them means a rating can always be explained without re-deriving it from state that has since moved on.</summary>
        public float ReviewedDebtBurden;
        public float? ReviewedDeficitPercent;
        public float? ReviewedGrowthPercent;
    }
}
