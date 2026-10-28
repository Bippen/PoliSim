using System;

namespace PoliSim.Elections
{
    /// <summary>A-LOOP q24 (5a): the three contact classes a campaign's effect is read in - Kalla &amp; Broockman's personal and impersonal panels, and earned media, which they do not split out.</summary>
    public enum ContactClass
    {
        Personal = 0,
        Impersonal = 1,
        Earned = 2,
    }

    /// <summary>
    /// A-LOOP q24, piece 5a (`docs/specs/A_LOOP_PLAN.md` §5, ruled J1 2026-10-19): **a campaign moves vote SHARES directly, within sourced
    /// bounds, and never passes through the compatibility clamp.** Until this piece the chain's persuasion pressure became compatibility
    /// points and the preference model clamped compatibility at 100, so at a national audience every party closed past the ceiling and the
    /// clamp's arithmetic, not the campaign, decided the result (§846; GA-5-1: every mix within 0.1 point of an idle player's).
    ///
    /// <para><b>The form</b> (the plan's (2)). Per party and per contact class, the chain's EXPOSURE (people who noticed) accumulates over the
    /// campaign (<see cref="CampaignPressure"/>'s tally). Coverage is the share of the WHOLE electorate covered at least once, over the part of
    /// it the party can reach (where it stands): r·(1 − exp(−exposures / (r·electorate))) - each voter is persuadable once, so repeated contact
    /// covers the same voters, and a party on a sixth of the ballots covers at most a sixth of the electorate. The effect on party p's share is
    /// Σ_class CACE_class × c_class,p × q_p, where q_p is the relevance × credibility the chain resolved for p's contacts as a ratio to the
    /// electorate-wide mean (<see cref="ElectorateMean"/>), so the chain still decides WHERE an effect lands and a zero anywhere stays zero
    /// (§42's invariant). The per-contact effect CACE × q is held at the class's sourced upper limit (<see cref="UpperOf"/>; DECLARED: the
    /// 95 % limit is an interval on the pooled mean, read here as the most any one contact can do). An attack enters with the sign against its
    /// target. Every other party absorbs a party's effect in proportion to its share, as the record's shift is absorbed (<see cref="Apply"/>).</para>
    ///
    /// <para><b>Timing.</b> The live campaign is the eight-week window, so every contact in it takes Kalla &amp; Broockman's close-to-election
    /// CACE, which is measured on the vote and does not decay before it. The run-up's one purchase with an effect, a television buy, takes
    /// Gerber et al.'s weekly effect on the polls, gone a week later (<see cref="RunUpTelevisionEffectPp"/>).</para>
    /// </summary>
    public static class CampaignPersuasion
    {
        /// <summary>
        /// SOURCED [KB18] (Kalla &amp; Broockman 2018, APSR 112(1), p. 159, and Figure 4 panel (a) p. 160, "Effects Within 2 Months of Election Day
        /// With Original Studies, By Contact Type", personal contact): "The average effect from our meta-analysis is 0.58 percentage points, with a 95%
        /// confidence interval ranging from -0.50 to 1.66 percentage points" - points of vote share among the contacted.
        /// </summary>
        public const double PersonalCacePp = 0.58;

        /// <summary>SOURCED [KB18] p. 159 and Figure 4 panel (a): the 95 % interval's upper limit, 1.66 points - the bound the effect is held within.</summary>
        public const double PersonalUpperPp = 1.66;

        /// <summary>SOURCED [KB18] Figure 4 panel (b) p. 160, impersonal contact within two months of election day, the pooled random-effects row: "0.28 [ -0.27, 0.83]".</summary>
        public const double ImpersonalCacePp = 0.28;

        /// <summary>SOURCED [KB18] Figure 4 panel (b): the upper limit, 0.83 points.</summary>
        public const double ImpersonalUpperPp = 0.83;

        /// <summary>
        /// DERIVED from SOURCED [KB18] p. 153: "the most optimistic estimate that could be warranted from the literature is that campaign
        /// contact persuades about 1 in 175 voters, but that our best guess is that it persuades about 1 in 800 voters" - 100 / 800 = 0.125
        /// points, all contact pooled. Earned media (an interview, an announcement, the press's coverage) is not one of the paper's contact
        /// classes, so it takes the all-contact estimate (DECLARED reading). ⚠ DECLARED, the vintage: this is Figure 1's estimate from the
        /// literature BEFORE the authors' own studies, while the personal and impersonal rows are Figure 4's WITH them (p. 160), so earned media
        /// persuades less per contact than impersonal (0.125 against 0.28) by the mix of two estimates, not by a finding about earned media.
        /// </summary>
        public const double EarnedCacePp = 100.0 / 800.0;

        /// <summary>DERIVED from SOURCED [KB18] p. 153's "1 in 175 voters": 100 / 175 ≈ 0.571 points, the bound for earned media.</summary>
        public const double EarnedUpperPp = 100.0 / 175.0;

        /// <summary>
        /// SOURCED [GGGS11] (Gerber, Gimpel, Green &amp; Shaw 2011, APSR 105(1), pp. 144 and 146): "the current week's advertising raises Perry's vote
        /// share by 4.73 percentage points per 1,000 GRPs (SE = 1.42); a week later, the effects of these ads have receded to -0.17 percentage
        /// points (SE = 1.42)". DECLARED readings: (1) the lagged week is read as zero - within its own SE of zero, and the paper's own summary,
        /// "just a week or two later, the advertisement's effects have all but disappeared" (p. 135); Table 5's distributed-lag columns (4)-(8)
        /// do carry some of it into the following week (0.42 to 3.05 on the one-week lag, p. 146), which this reading leaves out. (2) Its outcome
        /// is "the percent of all respondents (including "don't know" responses) ... [who] favor Perry" (Table 4's note, p. 145) and its GRP base a
        /// market's viewing audience, applied here to a party's share of the valid vote.
        /// </summary>
        public const double TelevisionPpPer1000Grp = 4.73;

        /// <summary>SOURCED [GGGS11] p. 144: the standard error of the current week's estimate, 1.42 points per 1 000 GRPs.</summary>
        public const double TelevisionSePer1000Grp = 1.42;

        /// <summary>DERIVED from SOURCED [GGGS11]: the estimate is weekly ("the current week's advertising"), so a buy shows on the polls for the seven days from the day it airs.</summary>
        public const int TelevisionWeekDays = 7;

        /// <summary>The class a source slot belongs to: the eight actions in `CampaignActions.TheEight`'s order, then the earned coverage (DECLARED, the plan's (2): rally, town hall and doors personal; television, digital and the post impersonal; the interview, the announcement and the coverage earned).</summary>
        public static ContactClass ClassOf(CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.Rally:
                case CampaignActionKind.TownHall:
                case CampaignActionKind.DoorToDoor:
                    return ContactClass.Personal;
                case CampaignActionKind.TelevisionAd:
                case CampaignActionKind.DigitalAd:
                case CampaignActionKind.SocialPost:
                    return ContactClass.Impersonal;
                default:
                    return ContactClass.Earned;
            }
        }

        /// <summary>The class's close-to-election CACE, in points of share among the contacted.</summary>
        public static double CaceOf(ContactClass c) => c == ContactClass.Personal ? PersonalCacePp : c == ContactClass.Impersonal ? ImpersonalCacePp : EarnedCacePp;

        /// <summary>The class's sourced upper limit, in points of share among the contacted.</summary>
        public static double UpperOf(ContactClass c) => c == ContactClass.Personal ? PersonalUpperPp : c == ContactClass.Impersonal ? ImpersonalUpperPp : EarnedUpperPp;

        /// <summary>The number of contact classes.</summary>
        public static int ClassCount => 3;

        /// <summary>
        /// DERIVED (the Poisson arithmetic): the share of the WHOLE electorate of <paramref name="electorate"/> that <paramref name="exposures"/>
        /// contacts cover at least once, when they fall on the share <paramref name="reach"/> of it the party can reach (where it stands):
        /// r·(1 − exp(−exposures / (r·electorate))), never above r. [AUTHORED-DRAFT] premise: a second contact adds nothing.
        /// </summary>
        public static double Coverage(double exposures, double electorate, double reach = 1.0)
        {
            double r = Clamp01(reach);
            return exposures <= 0.0 || electorate <= 0.0 || r <= 0.0 ? 0.0 : r * (1.0 - Math.Exp(-exposures / (r * electorate)));
        }

        /// <summary>The class's per-contact effect in points among the contacted: its CACE times q (the contacts' mean relevance × credibility over <paramref name="norm"/>), held at its sourced upper limit; 0 when nothing is exposed or the norm is not positive.</summary>
        public static double PerContactPp(ContactClass c, double exposure, double quality, double norm)
        {
            if (exposure <= 0.0 || quality <= 0.0 || norm <= 0.0) { return 0.0; }
            return Math.Min(CaceOf(c) * quality / (exposure * norm), UpperOf(c));
        }

        /// <summary>Whether the class's upper limit binds on these contacts (CACE × q above it) - what the diagnostic counts.</summary>
        public static bool CapBinds(ContactClass c, double exposure, double quality, double norm) =>
            exposure > 0.0 && quality > 0.0 && norm > 0.0 && CaceOf(c) * quality / (exposure * norm) > UpperOf(c);

        /// <summary>
        /// The effect, in points of share before the size scaling, of one class's contacts: <see cref="PerContactPp"/> times the coverage of the
        /// electorate over the party's reach. Zero when nothing was exposed or the norm is not positive.
        /// </summary>
        public static double ClassEffectPp(ContactClass c, double exposure, double quality, double electorate, double norm, double reach = 1.0) =>
            PerContactPp(c, exposure, quality, norm) * Coverage(exposure, electorate, reach);

        /// <summary>The bound the plan's (4)(b) holds every party's effect within: the class's upper limit × the coverage, in points, read off the same exposures over the same reach.</summary>
        public static double BoundPp(ContactClass c, double exposure, double electorate, double reach = 1.0) => UpperOf(c) * Coverage(exposure, electorate, reach);

        /// <summary>
        /// K66 (Elias, 2026-10-23: "An attack shares coverage with the attacker's own contacts"): the attacker's OWN part of one class's effect
        /// when it has also attacked in that class - its own contacts' per-contact effect times the coverage of ALL its contacts in the class,
        /// own and attack together, times the own part's share of them. DERIVED (the Poisson arithmetic of the plan's (2), applied to the one
        /// pool of contacts the party made): a contact spent on the opponent is one the party did not spend on itself, and the voters its attacks
        /// reach are the voters its own contacts reach. With no attack it is `ClassEffectPp` exactly.
        /// </summary>
        public static double SharedClassEffectPp(ContactClass c, double ownExposure, double ownQuality, double attackExposure, double electorate, double norm, double reach = 1.0)
        {
            if (attackExposure <= 0.0) { return ClassEffectPp(c, ownExposure, ownQuality, electorate, norm, reach); }
            if (ownExposure <= 0.0) { return 0.0; }
            double all = ownExposure + attackExposure;
            return PerContactPp(c, ownExposure, ownQuality, norm) * Coverage(all, electorate, reach) * (ownExposure / all);
        }

        /// <summary>
        /// K3 with K66: what one attacker's attacks in one class take from their target, in points - the class's per-contact effect on those
        /// contacts (CACE × q, held at the upper limit; K3: "the persuasion class effect signed against the target", [AUTHORED-DRAFT] until Lau,
        /// Sigelman and Rovner 2007 is read) times the coverage of all the attacker's contacts in the class (<paramref name="attackerAllExposure"/>,
        /// own and attack: K66) times the attacks' share of them, times <paramref name="targetShareOfReach"/>: the share of the attacker's reach where
        /// the target is on the ballot - the electorate where BOTH stand over the attacker's (review M2 of 2026-10-23: read from the candidacies,
        /// region by region, `CampaignPressure.Overlap`; the CDU and the CSU share no voter, so a CDU attack on the CSU takes nothing). DERIVED: a
        /// contact can move a vote away from a party only where that party is on the ballot. Exactly 1 in Sweden, where every party stands everywhere.
        /// </summary>
        public static double AttackEffectPp(ContactClass c, double attackExposure, double attackQuality, double attackerAllExposure, double electorate, double norm,
            double attackerReach = 1.0, double targetShareOfReach = 1.0)
        {
            if (attackExposure <= 0.0 || attackerAllExposure <= 0.0) { return 0.0; }
            return PerContactPp(c, attackExposure, attackQuality, norm) * Coverage(attackerAllExposure, electorate, attackerReach) * (attackExposure / attackerAllExposure) * Clamp01(targetShareOfReach);
        }

        /// <summary>
        /// K65, K81, K91 and K92 (Elias, 2026-10-23 and 10-24; reviews M1 and H1): <b>the share-aware balanced absorption.</b> Each party p's
        /// share moves by Δ_p = e_p − Σ_q W_pq e_q + g_p − l_p: its own sourced effect whole (W's diagonal is zero), less what it gives up of every
        /// other party's own effect, plus what its attacks took (g_p) less what attacks took from it (l_p).
        /// <para><b>W</b> (`BalancedAbsorption`) is the doubly balanced absorption: each column sums to one (q's gain is taken whole from the others,
        /// the shares still sum to one) and equal effort per voter reached moves nobody (Σ_q W_pq r_q = r_p, r each party's reach). So the field's
        /// effort is what p's own is read against, and a party campaigning ALONE moves by exactly its sourced effect (S alone at +0.40 moves +0.40).
        /// ⚠ <b>W cannot be share-weighted</b> (K91, ruled 2026-10-24, measured and PROVED infeasible on this branch - an ask to Elias): with zero
        /// diagonal, columns summing to one and rows summing to one (equal effort, everyone everywhere), every row's total is one, so if in every
        /// column the bigger party absorbed more, the smallest party's row would sum below one - unless every column is uniform. The uniform matrix
        /// is therefore the ONLY one meeting the three conditions in which no smaller party absorbs more than a bigger one: where every party stands
        /// everywhere (Sweden, the USA) W_pq = 1/(n − 1), each party giving up the same points of a lone campaigner's gain whatever its size; where
        /// candidacies differ (Germany) it is the balancing of who meets whom. The share-weighted alternative measured - the projection of the
        /// proportional absorption onto the two conditions - inverts the order (S alone at +0.40: SD loses 0.042, L 0.064), and was not kept.</para>
        /// <para><b>An attack</b> (K92) moves only its two parties: the target loses what the attack takes, the attacker gains exactly that, and
        /// every idle party stays where it was - an attack's loss is not the field's to absorb.</para>
        /// <para><b>A party with no prior share</b> has no voters to lose: it gives up nothing, and its own effect is read against the field's (the
        /// share-weighted mean of the held parties' effects per voter reached, times its reach), floored at zero, then taken from the held parties
        /// in proportion to their shares where it stands - so under equal effort it gains nothing (review 2's L3), and campaigning alone it gains its
        /// sourced effect. Returns <paramref name="shares"/> itself when nothing moved; a share pushed below zero is held at zero and the vector
        /// renormalised, as in `Apply`.</para>
        /// </summary>
        public static double[] ApplyCampaign(double[] shares, double[] effects, double[] reach = null, double[][] overlap = null, double[] attackLoss = null, double[] attackGain = null)
        {
            if (shares == null || effects == null || effects.Length != shares.Length) { return shares; }
            if (!AnyNonZero(effects) && !AnyNonZero(attackLoss) && !AnyNonZero(attackGain)) { return shares; }
            return ApplyBalanced(shares, effects, BalancedAbsorption(shares, reach, overlap), attackLoss, attackGain);
        }

        private static bool AnyNonZero(double[] v)
        {
            if (v == null) { return false; }
            foreach (double x in v) { if (x != 0.0) { return true; } }
            return false;
        }

        /// <summary>K91: the absorption a campaign's effects are laid on - W[p][q] the share of party q's own effect that party p gives up; which parties hold a share; their reaches; and, where the two conditions could not be met, why (null when they were) - the proportional absorption then stands in, so nothing collapses (review 2's L2).</summary>
        public sealed class Absorption
        {
            public double[][] W;
            public bool[] Held;
            public double[] Reach;
            public string Fallback;
        }

        /// <summary>
        /// K91 with K92: <paramref name="effects"/> (each party's own effect less the attacks it received, as `CampaignPressure.ShareEffects`
        /// returns them) laid on <paramref name="shares"/> through <paramref name="a"/> (`BalancedAbsorption` of the same shares). The attacks'
        /// <paramref name="attackLoss"/> (received, positive) is added back to find each party's own effect, and moved directly to the attackers'
        /// <paramref name="attackGain"/>; null for none. A caller laying many vectors on one set of shares (the night's attribution) builds the
        /// absorption once.
        /// </summary>
        public static double[] ApplyBalanced(double[] shares, double[] effects, Absorption a, double[] attackLoss = null, double[] attackGain = null)
        {
            int n = shares.Length;
            var own = new double[n];
            for (int p = 0; p < n; p++) { own[p] = effects[p] + (attackLoss != null && p < attackLoss.Length ? attackLoss[p] : 0.0); }
            // a party with no share: its effect read against the held field's, per voter reached, floored at zero (review 2's L3)
            double mean = 0.0, weight = 0.0;
            for (int p = 0; p < n; p++) { if (a.Held[p] && a.Reach[p] > 0.0) { mean += shares[p] * own[p] / a.Reach[p]; weight += shares[p]; } }
            mean = weight > 0.0 ? mean / weight : 0.0;
            for (int z = 0; z < n; z++) { if (!a.Held[z]) { own[z] = Math.Max(0.0, own[z] - a.Reach[z] * mean); } }
            var result = (double[])shares.Clone();
            for (int p = 0; p < n; p++)
            {
                double absorbed = 0.0;
                for (int q = 0; q < n; q++) { if (q != p && own[q] != 0.0) { absorbed += a.W[p][q] * own[q]; } }
                double moved = own[p] - absorbed;
                if (attackGain != null && p < attackGain.Length) { moved += attackGain[p]; }
                if (attackLoss != null && p < attackLoss.Length) { moved -= attackLoss[p]; }
                result[p] += moved;
            }
            double total = 0.0;
            for (int i = 0; i < n; i++) { if (result[i] < 0.0) { result[i] = 0.0; } total += result[i]; }
            if (total <= 0.0) { return shares; }
            for (int i = 0; i < n; i++) { result[i] /= total; }
            return result;
        }

        /// <summary>
        /// Regional 5a (K147): `ApplyBalanced`'s movement alone - each party's Δ_p = e_p − Σ_q W_pq e_q + g_p − l_p, with a zero-share party's effect
        /// read against the held field's as there - before the floor at zero and the renormalisation, so one region's movements can be summed over the
        /// regions (`CampaignPressure.RegionalShifts`). The columns of W sum to one over the held parties, so the movements sum to zero.
        /// </summary>
        public static double[] Moved(double[] shares, double[] effects, Absorption a, double[] attackLoss = null, double[] attackGain = null)
        {
            int n = shares.Length;
            var own = new double[n];
            for (int p = 0; p < n; p++) { own[p] = effects[p] + (attackLoss != null && p < attackLoss.Length ? attackLoss[p] : 0.0); }
            double mean = 0.0, weight = 0.0;
            for (int p = 0; p < n; p++) { if (a.Held[p] && a.Reach[p] > 0.0) { mean += shares[p] * own[p] / a.Reach[p]; weight += shares[p]; } }
            mean = weight > 0.0 ? mean / weight : 0.0;
            for (int z = 0; z < n; z++) { if (!a.Held[z]) { own[z] = Math.Max(0.0, own[z] - a.Reach[z] * mean); } }
            var moved = new double[n];
            for (int p = 0; p < n; p++)
            {
                double absorbed = 0.0;
                for (int q = 0; q < n; q++) { if (q != p && own[q] != 0.0) { absorbed += a.W[p][q] * own[q]; } }
                moved[p] = own[p] - absorbed;
                if (attackGain != null && p < attackGain.Length) { moved[p] += attackGain[p]; }
                if (attackLoss != null && p < attackLoss.Length) { moved[p] -= attackLoss[p]; }
            }
            return moved;
        }

        /// <summary>Regional 5a: <paramref name="shift"/> (a movement per party, fractions) laid on <paramref name="shares"/> - a share pushed below zero held at
        /// zero and the vector renormalised, as `ApplyBalanced` closes; the shares themselves when nothing moved.</summary>
        public static double[] ApplyShift(double[] shares, double[] shift)
        {
            if (shares == null || shift == null || !AnyNonZero(shift)) { return shares; }
            var result = (double[])shares.Clone();
            double total = 0.0;
            for (int i = 0; i < result.Length; i++) { result[i] += i < shift.Length ? shift[i] : 0.0; if (result[i] < 0.0) { result[i] = 0.0; } total += result[i]; }
            if (total <= 0.0) { return shares; }
            for (int i = 0; i < result.Length; i++) { result[i] /= total; }
            return result;
        }

        /// <summary>CONVENTION: the alternating scaling's iterations and the residual it must reach to count as balanced - a numerical solver's limits, not a model figure.</summary>
        private const int BalanceIterations = 5000;
        private const double BalanceTolerance = 1e-12;

        /// <summary>
        /// K81 (K91's note above): the absorption for <paramref name="shares"/>. Over the parties holding a share, the seed is who meets whom -
        /// o_pq, the share of the electorate where both stand (<paramref name="overlap"/>; null, the smaller reach), zero on the diagonal - scaled
        /// row and column by the alternating (Sinkhorn) iteration until each column sums to one and each row to Σ_q W_pq r_q = r_p (the
        /// equal-effort condition; the two conditions are consistent, both summing to Σ r). The shares only decide which parties are held: a party
        /// with no share gives up nothing, and its effect is taken in proportion to the held parties' shares where it stands. DECLARED (review 2's
        /// L2): where the iteration cannot meet both conditions (two parties of unequal reach, no third), it does NOT stand - the proportional
        /// absorption (each column in proportion to the shares that meet it, `Apply`'s form) stands in, nothing collapses, and the absorption's
        /// Fallback says why (the run records it, the game logs it).
        /// </summary>
        public static Absorption BalancedAbsorption(double[] shares, double[] reach, double[][] overlap)
        {
            int n = shares.Length;
            var a = new Absorption { W = new double[n][], Held = new bool[n], Reach = new double[n] };
            for (int p = 0; p < n; p++) { a.W[p] = new double[n]; a.Reach[p] = ReachOf(reach, p); }
            double O(int p, int q) => overlap != null && p < overlap.Length && overlap[p] != null && q < overlap[p].Length ? overlap[p][q] : Math.Min(a.Reach[p], a.Reach[q]);
            for (int p = 0; p < n; p++) { a.Held[p] = shares[p] > 0.0 && a.Reach[p] > 0.0; }
            // the proportional form - what a zero-share column always takes, and the fallback for the held ones
            var P = new double[n][];
            for (int p = 0; p < n; p++) { P[p] = new double[n]; }
            for (int q = 0; q < n; q++)
            {
                double den = 0.0;
                for (int p = 0; p < n; p++) { if (p != q && a.Held[p]) { den += shares[p] * O(p, q); } }
                for (int p = 0; p < n; p++) { if (p != q && a.Held[p] && den > 0.0) { P[p][q] = shares[p] * O(p, q) / den; } }
            }
            var x = new double[n];
            var y = new double[n];
            for (int p = 0; p < n; p++) { x[p] = a.Held[p] ? 1.0 : 0.0; y[p] = a.Held[p] ? 1.0 : 0.0; }
            double worst = double.PositiveInfinity;
            for (int it = 0; it < BalanceIterations && worst > BalanceTolerance; it++)
            {
                for (int p = 0; p < n; p++)
                {
                    if (!a.Held[p]) { continue; }
                    double den = 0.0;
                    for (int q = 0; q < n; q++) { if (q != p && a.Held[q]) { den += O(p, q) * y[q] * a.Reach[q]; } }
                    x[p] = den > 0.0 ? a.Reach[p] / den : 0.0;
                }
                for (int q = 0; q < n; q++)
                {
                    if (!a.Held[q]) { continue; }
                    double den = 0.0;
                    for (int p = 0; p < n; p++) { if (p != q && a.Held[p]) { den += x[p] * O(p, q); } }
                    y[q] = den > 0.0 ? 1.0 / den : 0.0;
                }
                worst = 0.0;
                for (int p = 0; p < n; p++)
                {
                    if (!a.Held[p]) { continue; }
                    double row = 0.0;
                    for (int q = 0; q < n; q++) { if (q != p && a.Held[q]) { row += x[p] * O(p, q) * y[q] * a.Reach[q]; } }
                    worst = Math.Max(worst, Math.Abs(row - a.Reach[p]));
                }
            }
            bool balanced = worst <= BalanceTolerance;
            for (int p = 0; p < n; p++)
            {
                for (int q = 0; q < n; q++)
                {
                    a.W[p][q] = !a.Held[q] || !balanced ? P[p][q] : (p != q && a.Held[p] ? x[p] * O(p, q) * y[q] : 0.0);
                }
            }
            if (!balanced) { a.Fallback = "the equal-effort balancing cannot be met on these shares and candidacies - the proportional absorption stands in"; }
            return a;
        }

        private static double ReachOf(double[] reach, int p) => reach == null || p >= reach.Length ? 1.0 : Clamp01(reach[p]);

        /// <summary>
        /// The electorate-wide mean of relevance × credibility the CACEs are read against (DECLARED, the plan's "[AUTHORED-DRAFT]
        /// normalisation"): each party's general message - the mean true salience times its mean true issue-match over the contested issues,
        /// as `CampaignRun`'s general message reads them - times its opening credibility, weighted by its prior share, so a contact of the
        /// average voter's party at its average message takes the sourced CACE exactly. One figure per staging, the same for every party.
        /// </summary>
        public static double ElectorateMean(CampaignRun.Setup setup)
        {
            double sum = 0.0, weight = 0.0;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double s = 0.0, m = 0.0;
                int n = 0;
                double[] match = setup.Parties[p].TrueIssueMatch;
                for (int i = 0; setup.TrueSalience != null && match != null && i < setup.TrueSalience.Length && i < match.Length; i++)
                {
                    if (double.IsNaN(setup.TrueSalience[i]) || double.IsNaN(match[i])) { continue; }
                    s += setup.TrueSalience[i]; m += match[i]; n++;
                }
                double relevance = n > 0 ? Clamp01(s / n) * Clamp01(m / n) : 0.0;
                double w = setup.PriorShares[p] > 0.0 ? setup.PriorShares[p] : 0.0;
                sum += w * relevance * Clamp01(setup.Parties[p].Credibility);
                weight += w;
            }
            return weight > 0.0 ? sum / weight : 0.0;
        }

        /// <summary>
        /// Each party's effect (a share FRACTION, positive or negative, the sourced CACE in points as measured - no scaling to the party's size)
        /// laid onto <paramref name="shares"/>, every other party absorbing it in proportion to its share - the record shift's absorption,
        /// generalised to every party moving at once: party q gains its own e_q and gives up e_p × s_q / (1 − s_p) of every other party p's. The
        /// sum is preserved exactly. A share pushed below zero is held at zero and the vector renormalised: a guard that CAN bind - a small party
        /// under sustained attack can be pushed past its own share (three classes' upper limits, some 3 points, against a party of 1 %). Null or
        /// all-zero effects return the shares as they came.
        /// <para><b>The equal-effort drift, RULED (K65, 2026-10-23).</b> With two parties equal effort cancels exactly (the sources' setting).
        /// With more it does not: every party campaigning equally moves share from the large parties to the small ones, because each gains its
        /// effect whole and gives up others' in proportion to its share. Of §881's two candidates Elias ruled the second: a campaign's effects are
        /// read relative to the field (K65), through the balanced absorption that holds a lone campaigner at its sourced size (K81, `ApplyCampaign`); this proportional form stays for the run-up's one buyer. (The first, s(1 − s)/0.25
        /// scaling, shrank every small party's campaign four- to five-fold and broke criterion (d); review 881b's N1, 83f178ae reverted.)</para>
        /// </summary>
        public static double[] Apply(double[] shares, double[] effects)
        {
            if (shares == null || effects == null || effects.Length != shares.Length) { return shares; }
            bool any = false;
            foreach (double e in effects) { if (e != 0.0) { any = true; break; } }
            if (!any) { return shares; }
            var result = (double[])shares.Clone();
            for (int p = 0; p < shares.Length; p++)
            {
                double e = effects[p];
                if (e == 0.0) { continue; }
                double rest = 1.0 - shares[p];
                result[p] += e;
                if (rest <= 0.0) { continue; }
                for (int q = 0; q < shares.Length; q++) { if (q != p) { result[q] -= e * shares[q] / rest; } }
            }
            double total = 0.0;
            for (int i = 0; i < result.Length; i++) { if (result[i] < 0.0) { result[i] = 0.0; } total += result[i]; }
            if (total <= 0.0) { return shares; }
            for (int i = 0; i < result.Length; i++) { result[i] /= total; }
            return result;
        }

        /// <summary>
        /// DERIVED: the gross rating points one run-up television buy delivers - each point 1 % of the viewing audience ([GGGS11] p. 136 note 2: "One
        /// GRP is equal to 1% of the viewing audience") - as the television action's reach of its audience at its own price, 100 × the channel's
        /// reach × §35's effectiveness at the price.
        /// </summary>
        public static double RunUpTelevisionGrp()
        {
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            return 100.0 * tv.ChannelReach * CampaignEconomy.Effectiveness(tv.MoneyCost, tv.MoneyCost);
        }

        /// <summary>
        /// The run-up television effect on the buyer's share on <paramref name="day"/> of the run-up, in points: [GGGS11]'s
        /// 4.73 per 1 000 GRPs for every buy that aired in the week up to that day, zero for an older one. It reaches the run-up's polls and never
        /// the count: nothing of it is carried into the campaign (`PreCampaignRun.Outcome` holds no share). DECLARED, for Elias: one buy is some
        /// 35 GRPs, +0.16 points, inside the internal poll's own sampling error, so the one place it shows is a place the player cannot see it.
        /// </summary>
        public static double RunUpTelevisionEffectPp(System.Collections.Generic.IReadOnlyList<int> airedDays, int day)
        {
            if (airedDays == null) { return 0.0; }
            int live = 0;
            foreach (int aired in airedDays) { if (day >= aired && day - aired < TelevisionWeekDays) { live++; } }
            return TelevisionPpPer1000Grp * live * RunUpTelevisionGrp() / 1000.0;
        }

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);
    }
}
