using System;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// A-LOOP q24, piece 5b (`docs/specs/A_LOOP_PLAN.md` §5, ruled J1 2026-10-19, Q6: it lands with GATE-18's q2; the q4 table's rows 5, 14, 15 and
    /// Elias's ruling K7 of 2026-10-23, `docs/RULINGS_2026-10-23.md`): <b>MOBILISATION - a contacted supporter's turnout rises by the sourced turnout
    /// CACE, and the share effect is the extra votes over the votes cast.</b>
    /// <para><b>The form.</b> A party's ground operation makes door contacts (opened doors) in a region (`RegionalMobilization`, W-B11: an office's daily operation and the
    /// door-to-door act). The doors go to the party's own supporters there - the region's electorate times the party's share of the region's
    /// preference (DECLARED: a get-out-the-vote canvass works its own side's list). Each supporter can be mobilised once, so the share of supporters
    /// reached is `Coverage` = 1 − exp(−doors / supporters) (DERIVED, the Poisson arithmetic 5a's persuasion uses; that a second visit adds nothing is
    /// [AUTHORED-DRAFT], as 5a's). A reached supporter turns out at the region's base turnout plus the CACE; the party's votes there are its supporters
    /// times that turnout (`PartyTurnout`). Nothing else moves: the preference is the count's, untouched, so the share effect is DERIVED - the extra
    /// votes over the votes cast, absorbed by every other party in proportion to its votes, which is what a share of a larger total is.</para>
    /// <para><b>A contact is an opened door</b> (DECLARED): the CACE is the effect among the voters the canvass actually reached (the complier
    /// average), so a `GotvModel` contact is read as a conversation at the door, not a knock. Read as a knock, [CP20]'s own effect per door
    /// knocked is its intent-to-treat, 1.8 points (5.01 × 0.359), and the size here would be some 2.8 times too large.</para>
    /// <para><b>Never at the clamp.</b> A reached supporter's turnout is the base plus the CACE, and the CACE is a few points, so no rate reaches
    /// 1 at any base turnout a seeded European count reads (`GroundGameCountDiagnostic` prints the highest it reaches): the clamp in `PartyTurnout` is a guard that binds nowhere,
    /// and the effect is bounded by the CACE times the coverage, which is below 1 - no budget buys more than every supporter reached once.</para>
    /// <para><b>The anchors</b> (K7): a European count reads Cantoni &amp; Pons's volunteer canvass, Italy 2014 (`EuropeanDoorCacePp`); the USA keeps
    /// Gerber &amp; Green's band at the electorate's base turnout (`UsDoorCacePp`). The US count itself stays on W-B11's mobilisation curve until
    /// its card is re-cut (`UsGroundGame`; Elias's K18), so the US band is the ruled figure the US half of 5b will read, printed beside the count.</para>
    /// <para><b>What is not here.</b> Phone banking's effect (row 14: [GG14] pp. 16-18, the final week only) has no verb that makes calls yet;
    /// transport and election-day reminders are cut by q4 (rows 16, 17). Enthusiasm from rallies has no sourced turnout effect and counts nothing.</para>
    /// </summary>
    public static class GotvTurnout
    {
        /// <summary>
        /// SOURCED [CP20] (Cantoni &amp; Pons, "Do Interactions with Candidates Increase Voter Support and Participation? Experimental Evidence from
        /// Italy", NBER Working Paper 27433, June 2020, p. 17; `docs/reference/raw/aloop/q4/nber_w27433_cantoni_pons.pdf`): "We obtain CACEs of,
        /// respectively, 0.018/0.359 = 5.01% and -0.002/0.455 = -0.4% for the volunteer and candidate treatments" - points of turnout among the
        /// voters whose door opened, volunteers canvassing for a list, the 2014 municipal elections. The transfer from an Italian municipal election
        /// to a national parliamentary one in Sweden, Germany and Poland is [AUTHORED-DRAFT] (K7 chose the anchor; the play-calibration list carries
        /// the transfer): [CP20]'s control turnout is 74.7 % (`CantoniPonsControlTurnout`), inside Gerber &amp; Green's 70 %-and-higher band, where
        /// their US meta-analysis finds 0.6 [−1.5, 2.8] - so the European anchor is some eight times the US band at a like base.
        /// </summary>
        public const double EuropeanDoorCacePp = 5.01;

        /// <summary>SOURCED [CP20] Table 4 col. 1, p. 29: "Canvassed by volunteers 0.018" with its standard error "(0.008)" - the intent-to-treat on turnout.</summary>
        private const double CantoniPonsIntentToTreat = 0.018;

        /// <summary>SOURCED [CP20] Table 4 col. 1, p. 29: the intent-to-treat's standard error, 0.008.</summary>
        private const double CantoniPonsIntentToTreatSe = 0.008;

        /// <summary>SOURCED [CP20] p. 16: "the fraction of opened doors, respectively 45.5% and 35.9% in the candidates' and volunteers' groups".</summary>
        private const double CantoniPonsOpenedDoors = 0.359;

        /// <summary>
        /// SOURCED [CP20] p. 17 and Table 4 ("Mean dep. var. 0.747"): the study's control turnout - the authors divide by "1 minus the fraction of
        /// nonvoters in the control group", "(1 - 0.747)". The base the European anchor was measured at, printed beside the count's own.
        /// </summary>
        public const double CantoniPonsControlTurnout = 0.747;

        /// <summary>CONVENTION: the two-sided 95 % normal quantile the upper limit is read at.</summary>
        private const double NormalQuantile95 = 1.96;

        /// <summary>DERIVED from SOURCED [CP20]: the 95 % upper limit of the volunteer CACE, (intent-to-treat + 1.96 × its SE) over the opened doors
        /// - (0.018 + 0.01568) / 0.359, some 9.38 points (DECLARED: the opened-door share is read as fixed). The bound a count's effect is held within.</summary>
        public static double EuropeanDoorCaceUpperPp => 100.0 * (CantoniPonsIntentToTreat + NormalQuantile95 * CantoniPonsIntentToTreatSe) / CantoniPonsOpenedDoors;

        /// <summary>SOURCED [GG14] (Gerber &amp; Green, "Field Experiments on Voter Mobilization: An Overview of a Burgeoning Literature", 5 Apr 2014,
        /// marked "[EARLY DRAFT: NOT FOR CITATION]", Table 4 "Meta-Analysis Results, by Base Rate of Turnout", all canvassing, average CACE): the band
        /// edges of the control group's turnout, "Under 30%", "30% to 50%", "50% to 70%", "70% and Higher".</summary>
        private static readonly double[] UsBandFloors = { 0.0, 0.30, 0.50, 0.70 };

        /// <summary>SOURCED [GG14] Table 4, all canvassing: 4.6, 6.2, 1.4, 0.6 points of turnout, band by band.</summary>
        private static readonly double[] UsBandCacePp = { 4.6, 6.2, 1.4, 0.6 };

        /// <summary>SOURCED [GG14] Table 4: the 95 % intervals' upper limits, 6.9, 8.2, 4.0, 2.8.</summary>
        private static readonly double[] UsBandUpperPp = { 6.9, 8.2, 4.0, 2.8 };

        /// <summary>The door CACE (points of turnout among the reached) a count in <paramref name="country"/> reads at a base turnout of
        /// <paramref name="baseTurnout"/> (a rate): the USA Gerber &amp; Green's band, every other country Cantoni &amp; Pons's figure (K7).</summary>
        public static double DoorCacePp(CountryId country, double baseTurnout) => country == CountryId.USA ? UsDoorCacePp(baseTurnout) : EuropeanDoorCacePp;

        /// <summary>The upper limit the effect is held within, as `DoorCacePp` picks its figure.</summary>
        public static double DoorCaceUpperPp(CountryId country, double baseTurnout) => country == CountryId.USA ? UsBandUpperPp[BandOf(baseTurnout)] : EuropeanDoorCaceUpperPp;

        /// <summary>Gerber &amp; Green's band at <paramref name="baseTurnout"/>: the average CACE of the band the base turnout falls in.</summary>
        public static double UsDoorCacePp(double baseTurnout) => UsBandCacePp[BandOf(baseTurnout)];

        private static int BandOf(double baseTurnout)
        {
            int band = 0;
            for (int b = 0; b < UsBandFloors.Length; b++) { if (baseTurnout >= UsBandFloors[b]) { band = b; } }
            return band;
        }

        /// <summary>The share of <paramref name="supporters"/> reached at least once by <paramref name="doors"/> opened doors: 1 − exp(−doors /
        /// supporters), 0 with no supporters or no doors (DERIVED, persuadable once).</summary>
        public static double Coverage(double doors, double supporters) => supporters > 0.0 && doors > 0.0 ? 1.0 - Math.Exp(-doors / supporters) : 0.0;

        /// <summary>The votes <paramref name="doors"/> add among <paramref name="supporters"/> at a CACE of <paramref name="cacePp"/> points: the
        /// supporters reached times the CACE (before the clamp, which never binds - see the class's note).</summary>
        public static double ExtraVotes(double doors, double supporters, double cacePp) => supporters * Coverage(doors, supporters) * cacePp / 100.0;

        /// <summary>A party's supporters' turnout in a region: the base plus the reached share times the CACE, held within [0, 1].</summary>
        public static double PartyTurnout(double baseTurnout, double doors, double supporters, double cacePp)
        {
            double rate = baseTurnout + Coverage(doors, supporters) * cacePp / 100.0;
            return rate < 0.0 ? 0.0 : (rate > 1.0 ? 1.0 : rate);
        }

        /// <summary>
        /// One region's votes per party under 5b: the region's electorate <paramref name="eligible"/> split by <paramref name="preference"/> (each
        /// party's share of the region's preference, normalised), each party's supporters at `PartyTurnout` with its own doors there
        /// (<paramref name="doors"/>, per party) - a party nobody canvassed votes at exactly the base.
        /// </summary>
        public static double[] RegionVotes(double eligible, double[] preference, double baseTurnout, double[] doors, double cacePp)
        {
            var votes = new double[preference.Length];
            double sum = 0.0;
            foreach (double p in preference) { sum += p; }
            if (sum <= 0.0 || eligible <= 0.0) { return votes; }
            for (int p = 0; p < preference.Length; p++)
            {
                double supporters = eligible * preference[p] / sum;
                votes[p] = supporters * PartyTurnout(baseTurnout, doors != null && p < doors.Length ? doors[p] : 0.0, supporters, cacePp);
            }
            return votes;
        }

        /// <summary>
        /// What <paramref name="moreDoors"/> opened doors add to a party's NATIONAL share, in points, read off what the party can know: its supporters
        /// in the region the region's electorate <paramref name="eligible"/> times its published share <paramref name="share"/> (DECLARED: the AI
        /// reads its national poll in every region - it measures no region's vote), its own doors there so far <paramref name="doorsSoFar"/>, the votes
        /// cast nationally <paramref name="votesCast"/>. The marginal extra votes over the votes cast, absorbed: × (1 − share) (DERIVED, first order).
        /// </summary>
        public static double ShareValuePp(double eligible, double share, double doorsSoFar, double moreDoors, double votesCast, double cacePp)
        {
            if (eligible <= 0.0 || share <= 0.0 || share >= 1.0 || votesCast <= 0.0 || cacePp <= 0.0 || moreDoors <= 0.0) { return 0.0; }
            double supporters = eligible * share;
            double extra = ExtraVotes(doorsSoFar + moreDoors, supporters, cacePp) - ExtraVotes(doorsSoFar, supporters, cacePp);
            return 100.0 * extra * (1.0 - share) / votesCast;
        }
    }
}
