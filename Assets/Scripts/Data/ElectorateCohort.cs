namespace PoliSim.Data
{
    /// <summary>
    /// One slice of the electorate: how big it is, and how reliably it actually votes.
    ///
    /// <para><b>This is the half of the hybrid vote model that makes campaigning mean something.</b>
    /// Elias chose the hybrid over pure national swing on 2026-08-11 precisely so that turnout is a
    /// separate, movable term: a ground game raises <see cref="MobilisedTurnout"/> for the cohorts it
    /// targets, while advertising moves vote share. If turnout were a single national constant there
    /// would be nothing for canvassing to act on, and half the campaign screen would be decoration.</para>
    ///
    /// <para><b>Turnout is per-ELECTION-TYPE, not a single number, and in the USA that is the whole
    /// story of the midterms.</b> National turnout falls by roughly a third between a presidential year
    /// and a midterm, and it does not fall evenly - the youngest cohort drops furthest while the oldest
    /// barely moves, which mechanically ages the midterm electorate. That differential, not an arbitrary
    /// "anti-incumbent bonus", is what should produce the midterm's historical bias against the
    /// president's party.</para>
    /// </summary>
    public class ElectorateCohort
    {
        /// <summary>Display label - "18-24", "65+".</summary>
        public string Label;

        /// <summary>Share of the voting-eligible population, 0-1. Across a country's cohorts these sum to 1.</summary>
        public double ShareOfElectorate;

        /// <summary>Turnout at a high-salience national election (a presidential year in the USA, a general election elsewhere), 0-1.</summary>
        public double HighSalienceTurnout;

        /// <summary>Turnout at a low-salience election (a US midterm), 0-1. Where a country has no such distinction this equals <see cref="HighSalienceTurnout"/> rather than zero, so no caller has to branch.</summary>
        public double LowSalienceTurnout;

        /// <summary>
        /// Turnout actually achieved this cycle after campaign effects, 0-1. Reset to the baseline for
        /// the election type at the start of each campaign, then moved by ground-game spending.
        ///
        /// <para>Kept separate from the two baselines so a campaign can never permanently ratchet a
        /// cohort's habits - a party that canvasses hard once does not inherit a more reliable
        /// electorate forever, which is the degenerate strategy this separation forecloses.</para>
        /// </summary>
        public double MobilisedTurnout;

        /// <summary>Effective weight in the result: how much of the total vote this cohort casts, before normalisation. The product that makes a demobilised young electorate genuinely cost the left seats.</summary>
        public double EffectiveWeight => ShareOfElectorate * MobilisedTurnout;

        /// <summary>Resets <see cref="MobilisedTurnout"/> to the baseline for the coming election type. Call at campaign start, before any ground-game effect is applied.</summary>
        public void ResetForElection(bool highSalience)
        {
            MobilisedTurnout = highSalience ? HighSalienceTurnout : LowSalienceTurnout;
        }
    }
}
