using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B9 / SPEC §13 + §14 — the media as an INDEPENDENT force: outlets with audiences,
    /// coverage as a derived, decaying stock, and **media interest as availability** — the
    /// standing ruling from the W-B3 / W-B10 review, executed here and nowhere else. PURE
    /// FUNCTIONS AND SMALL STATE, WIRED TO NOTHING (R-N2).
    ///
    /// **The ruling, verbatim in substance:** earned media is free because *someone else decides
    /// whether to book you*, so the scarce resource is media INTEREST — implemented as
    /// availability driven by newsworthiness (coverage, momentum, recent events, popularity),
    /// never as a flat cost or a cap bolted onto the interview. A party nobody covers cannot buy
    /// its way onto the air; a party that makes news gets booked. `MediaInterest.Bookings`
    /// allocates each outlet's daily interview slots to the parties it finds most newsworthy,
    /// one slot per party per round, highest interest first — the booker's decision, not the
    /// party's.
    ///
    /// **Coverage cannot spiral (§13's own requirement).** Coverage is a stock per party that
    /// decays on a NEWS-CYCLE half-life (3 days — deliberately distinct from §22's 7-day momentum
    /// half-life, the second mechanism W-B10's doc named rather than a fudged exponent), and every
    /// day's gain passes through a saturating curve (`1 − exp(−raw / CoverageScale)`), so a
    /// party's coverage is bounded above by `CoverageScale / (1 − decay)` however much it does.
    /// Coverage creates momentum (§13's chain: coverage → awareness → polling → more attention),
    /// as a shock on `MomentumTracker` proportional to the day's coverage GAIN — bounded because
    /// the gain is.
    ///
    /// **§14 — the same message performs differently by outlet.** An outlet is a reach ceiling
    /// (the share of the electorate it can touch) and an audience COMPOSITION over voter groups.
    /// A message through an outlet is resolved for each group in the audience with that group's
    /// own salience and issue-match, weighted by the composition — so a climate message through a
    /// young urban outlet and the same message through an older rural one differ by exactly what
    /// the two audiences care about, and nothing else. Paid television chooses its outlet (the
    /// player's targeting, §14); an interview goes out through the outlet that booked it.
    ///
    /// **[AUTHORED-DRAFT] throughout** (R-N4; one line each in the prototype log): the outlet
    /// roster is ARCHETYPES (public broadcaster, commercial television, tabloid, broadsheet) with
    /// authored reach, slots, thresholds and compositions — real Swedish outlets' reach is
    /// sourceable (Kantar / Orvesto) and is billed as a data line, not typed here under a real
    /// name; the newsworthiness per action; the half-life; the scale; the momentum-per-coverage
    /// constant; the interest formula's weights.
    /// </summary>
    public readonly struct MediaOutlet
    {
        public readonly string Name;
        /// <summary>Share of the electorate the outlet can reach at all (its reach ceiling).</summary>
        public readonly double Reach;
        /// <summary>Interview slots the outlet books per campaign day.</summary>
        public readonly int DailySlots;
        /// <summary>The media interest (0–1) a party needs before this outlet will consider booking it.</summary>
        public readonly double InterestThreshold;
        /// <summary>The outlet's audience as shares of each voter group (index-aligned with the group table; sums to 1). Null = the general population (every group by its population share).</summary>
        public readonly double[] AudienceComposition;
        /// <summary>True for a television outlet — a paid television buy runs across all of them, so its ceiling is their reach combined (capped at the whole electorate).</summary>
        public readonly bool IsTelevision;

        public MediaOutlet(string name, double reach, int dailySlots, double interestThreshold, double[] audienceComposition, bool isTelevision = false)
        {
            Name = name; Reach = reach; DailySlots = dailySlots; InterestThreshold = interestThreshold;
            AudienceComposition = audienceComposition; IsTelevision = isTelevision;
        }

        /// <summary>The reach ceiling of a television buy across the roster: the television outlets' reach summed, capped at 1. Zero if the roster has no television.</summary>
        public static double TelevisionReach(MediaOutlet[] outlets)
        {
            double sum = 0.0;
            foreach (MediaOutlet o in outlets) { if (o.IsTelevision) { sum += o.Reach; } }
            return sum > 1.0 ? 1.0 : sum;
        }
    }

    /// <summary>A booking: which outlet will interview which party today.</summary>
    public readonly struct InterviewBooking
    {
        public readonly int OutletIndex;
        public readonly int PartyIndex;

        public InterviewBooking(int outletIndex, int partyIndex)
        {
            OutletIndex = outletIndex; PartyIndex = partyIndex;
        }
    }

    public static class MediaSystem
    {
        /// <summary>[AUTHORED-DRAFT] the news cycle's half-life in days — distinct from §22's momentum half-life (7) by design.</summary>
        public const double CoverageHalfLifeDays = 3.0;

        /// <summary>[AUTHORED-DRAFT] the coverage a day of total national attention is worth; the saturating gain's scale.</summary>
        public const double CoverageScale = 1.0;

        /// <summary>[AUTHORED-DRAFT] percentage points of §22 momentum per unit of coverage GAINED in a day.
        /// ⚠ D-20 (a) changed coverage's UNIT (a share of the electorate the news reached, not a share of
        /// "national attention"), and this figure is per unit of it. Converted on the media harness's own
        /// news day - an announcement at full interest plus a rally in the largest valkrets, raw 0.40
        /// under the old table and 0.092 under the new, ×4.35 - so that day shocks momentum by what it
        /// did (0.225 → 0.60 pp per unit is the same shock for that day). A unit conversion on one
        /// measured anchor, not a re-calibration: what changed is which acts make news, not how much
        /// news moves momentum.</summary>
        public const double MomentumPpPerCoverage = 6.5;

        /// <summary>[AUTHORED-DRAFT] the share of the electorate the advertising platforms can put a paid digital ad in front of — the digital channel's ceiling, as the television outlets' combined reach is television's.</summary>
        public const double PlatformReach = 0.55;

        /// <summary>[AUTHORED-DRAFT] a party's organic following as a fraction of its polled share of the electorate — who sees an unpaid post at all. W-F5/F6 can source real follower counts.</summary>
        public const double FollowingRatio = 0.30;

        /// <summary>
        /// The audience a NATIONAL action can reach through the media landscape, before the
        /// action's own channel reach and attention apply (W-B9's answer to W-B3's placeholder,
        /// under which every national action addressed the whole electorate):
        /// - television: the television outlets' combined reach;
        /// - a digital ad: the platforms' reach;
        /// - a social post: the party's own following (its polled share × <see cref="FollowingRatio"/>) —
        ///   a party nobody follows posts to nobody;
        /// - a policy announcement: the press carries it in proportion to the party's media
        ///   interest — the same figure that books its interviews;
        /// - an interview: the booking outlet's reach (handled by the caller; not here).
        /// Local actions are the region's, not the media's, and are not here either (W-B4/B11).
        /// </summary>
        public static double NationalAudience(CampaignActionKind kind, double electorate, MediaOutlet[] outlets,
            double polledShare, double mediaInterest)
        {
            switch (kind)
            {
                case CampaignActionKind.TelevisionAd: return electorate * MediaOutlet.TelevisionReach(outlets);
                case CampaignActionKind.DigitalAd: return electorate * PlatformReach;
                case CampaignActionKind.SocialPost: return electorate * Math.Max(0.0, polledShare) * FollowingRatio;
                case CampaignActionKind.PolicyAnnouncement: return electorate * Math.Max(0.0, Math.Min(1.0, mediaInterest));
                default: return electorate;
            }
        }

        /// <summary>[AUTHORED-DRAFT] interest formula weights: coverage, |momentum| in pp, polled share, recent-event salience.</summary>
        public const double InterestPerCoverage = 4.35;   // D-20 (a): converted with coverage's unit on the same news-day anchor as MomentumPpPerCoverage (1.0 × 0.40 / 0.092); the bookings a day of news buys are what W-B9 measured
        public const double InterestPerMomentumPp = 0.15;
        public const double InterestPerPolledShare = 0.8;
        public const double InterestPerEventSalience = 1.0;

        /// <summary>
        /// [AUTHORED-DRAFT] how newsworthy each §12 action is at full spend, in coverage units before
        /// saturation — a policy announcement makes the most news, door-knocking almost none, and an
        /// interview begets coverage (earned media compounds, but through the saturating gain).
        /// <para>⚠ <b>RE-AUTHORED ON ITS ATTENTION-SHARE MEANING — D-20 (a), ruled by Elias
        /// 2026-09-02.</b> The first table (rally 0.15, town hall 0.05, doors 0.01, television 0.10,
        /// digital 0.05, post 0.03, interview 0.20, announcement 0.25) was drafted for one meaning —
        /// how much a kind shocks §22's momentum — and C-N1 gave the same figures a second: the share
        /// of the electorate the press carries the party to. On that meaning a rally making 15 % of
        /// the nation's news persuaded five times the rally, a town hall's coverage 3–27 times the
        /// room (D-20's table). The ruling: <i>a town hall's coverage is a fraction of the town
        /// hall's own reach, never a multiple</i>. So the figure is now a FRACTION OF THE ACT'S OWN
        /// RESOLVED REACH — how much of what an act reached the press carries onward — and the raw
        /// newsworthiness is that fraction × the act's reach share (<see cref="RawNewsworthiness"/>),
        /// which makes the ruling hold for every kind in every region by construction: coverage can
        /// never reach more people than the act did. The fractions keep the old table's relative
        /// order, scaled so the interview keeps the ratio it already had (its coverage reached 0.40
        /// of its own audience under the old figures): old / 0.20 × 0.40. Nothing else is typed.</para></summary>
        public static double Newsworthiness(CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.Rally: return 0.30;
                case CampaignActionKind.TownHall: return 0.10;
                case CampaignActionKind.DoorToDoor: return 0.02;
                case CampaignActionKind.TelevisionAd: return 0.20;
                case CampaignActionKind.DigitalAd: return 0.10;
                case CampaignActionKind.SocialPost: return 0.06;   // a post is not news unless it travels; virality is not modelled (a §13 hook)
                case CampaignActionKind.Interview: return 0.40;
                case CampaignActionKind.PolicyAnnouncement: return 0.50;
                default: return 0.0;
            }
        }

        /// <summary>§13's decay factor over a number of days on the news-cycle half-life.</summary>
        public static double CoverageDecay(double days, double halfLifeDays = CoverageHalfLifeDays)
        {
            if (days <= 0) { return 1.0; }
            return Math.Pow(0.5, days / halfLifeDays);
        }

        /// <summary>The saturating gain: raw newsworthiness in, bounded coverage out — the reason coverage cannot spiral.</summary>
        public static double SaturatedGain(double raw, double scale = CoverageScale)
        {
            if (raw <= 0.0) { return 0.0; }
            return scale * (1.0 - Math.Exp(-raw / scale));
        }

        /// <summary>The raw newsworthiness of one action as resolved: the kind's figure × the strategy's media-attention multiplier × §35's spend effectiveness (a free action counts in full).        /// <para>D-20 (a): the raw figure is the kind's FRACTION × the act's own resolved reach as a
        /// share of the electorate (<paramref name="reachShare"/> = the chain's `Reach` ÷ the
        /// electorate, so §35's spend curve and the channel's reach are already inside it) × the
        /// strategy's media attention. Its unit is therefore a share of the electorate the act's news
        /// reaches — the attention-share meaning — and it can never exceed the act's own reach.</para></summary>
        public static double RawNewsworthiness(CampaignActions.ActionSpec spec, StrategyModifiers modifiers, double reachShare)
        {
            return Newsworthiness(spec.Kind) * Math.Max(0.0, Math.Min(1.0, reachShare)) * modifiers.MediaAttentionMultiplier;
        }

        /// <summary>
        /// C-N1 (2026-09-02) — §39's "Media Effects" layer: the day's earned coverage, resolved
        /// through §42's chain as a message of its own. Until this, coverage reached only §22's
        /// momentum, and momentum reaches only the poll (`MomentumTracker.Apply`'s call sites are
        /// all the argument to `PollingSystem.Conduct`), so no amount of coverage could change a
        /// vote — the media system was perception-only, which §39 does not say: it lists Media
        /// Effects and Momentum as two layers of the vote, not one.
        ///
        /// What is resolved, and with which figures. Since D-20 (a) (2026-09-02) the coverage GAIN is a
        /// SHARE OF THE ELECTORATE the day's news reached — each act's own resolved reach × its kind's
        /// fraction (<see cref="RawNewsworthiness"/>), saturated — so the coverage's audience is the
        /// electorate × the press's reach × the gain, through a spec whose channel reach is 1: the
        /// gain already IS the reached share, and a second reach fraction would count the same
        /// people twice. Attention, persuasion and enthusiasm weights are the interview's, verbatim —
        /// an interview IS the roster's earned-media action and no other earned-media figure is on
        /// record. The message is the party as the press reports it, not any one of its actions:
        /// the party's platform on average (the caller's `TrueMessage` with no issue). ⚠ History, so
        /// the shape is not re-argued: C-N1's first build had channel reach 1.0 over the OLD table
        /// (rally = 15 % of the nation's attention) and coverage dwarfed the actions; the interview
        /// spec verbatim over that table made it a peer at the ledger but 3–27× a town hall per act
        /// (D-20); the ruling re-authored the TABLE as fractions of reach, and with that the channel
        /// reach of 1 is the honest unit again. Coverage costs nothing (`spend` = 0 through a spec
        /// whose money cost is 0); the trace's salience shift is not applied (§18's channel stays
        /// the actions'). The strategy's media-attention multiplier already scaled the raw
        /// newsworthiness that became this gain; the caller passes the NONE strategy's modifiers so
        /// no strategy is applied twice.
        /// </summary>
        public static CampaignActions.ActionSpec CoverageSpec
        {
            get
            {
                CampaignActions.ActionSpec i = CampaignActions.Spec(CampaignActionKind.Interview);
                return new CampaignActions.ActionSpec(CampaignActionKind.Interview, 0.0, 0.0, 1.0,
                    i.Attention, i.PersuasionWeight, i.EnthusiasmWeight, 0.0, false);
            }
        }

        /// <summary>The share of the electorate the press can reach at all: the roster's reach summed, capped at 1 (the same cap as <see cref="MediaOutlet.TelevisionReach"/>).</summary>
        public static double PressReach(MediaOutlet[] outlets)
        {
            double sum = 0.0;
            foreach (MediaOutlet o in outlets) { sum += o.Reach; }
            return sum > 1.0 ? 1.0 : sum;
        }

        /// <summary>See <see cref="CoverageSpec"/>. `gain` is one day's saturated coverage gain for one party (0–1).</summary>
        public static CampaignActions.ChainTrace ResolveCoverage(double gain, double electorate, MediaOutlet[] outlets,
            double salience, double match, double credibility, StrategyModifiers modifiers) =>
            ResolveCoverage(gain, electorate, PressReach(outlets), salience, match, credibility, modifiers);

        /// <summary>The same with the press's reach already known (C-N7: the AI carries it on its view as a public fact).</summary>
        public static CampaignActions.ChainTrace ResolveCoverage(double gain, double electorate, double pressReach,
            double salience, double match, double credibility, StrategyModifiers modifiers)
        {
            if (gain <= 0.0) { return new CampaignActions.ChainTrace(CampaignActionKind.Interview, 0, 0, 0, 0, 0, 0, 0, 0); }
            double audience = electorate * Math.Max(0.0, Math.Min(1.0, pressReach)) * Math.Min(1.0, gain);
            return CampaignStrategyModel.Resolve(CoverageSpec, audience, salience, match, credibility, 0.0, modifiers);
        }

        /// <summary>
        /// §13's inputs folded into one bounded figure: how interesting a party is to the media
        /// today, 0–1. Concave and saturating, so nothing can push it past 1 and every input has
        /// diminishing effect. `polledShare` is the party's share in the latest PUBLISHED poll
        /// (candidate popularity, as the media see it — never the truth); `eventSalience` is §18's
        /// hook, 0 until events exist.
        /// </summary>
        public static double Interest(double coverage, double momentumPp, double polledShare, double eventSalience = 0.0)
        {
            double raw = InterestPerCoverage * Math.Max(0.0, coverage)
                         + InterestPerMomentumPp * Math.Abs(momentumPp)
                         + InterestPerPolledShare * Math.Max(0.0, polledShare)
                         + InterestPerEventSalience * Math.Max(0.0, eventSalience);
            return 1.0 - Math.Exp(-raw);
        }

        /// <summary>
        /// §14 — a message through an outlet: resolved once per voter group in the outlet's
        /// audience with THAT group's salience and issue-match, weighted by the composition. The
        /// audience is the outlet's reach ceiling × the electorate × the group's share of it. The
        /// returned trace is the composition-weighted sum, and still a `ChainTrace`.
        /// </summary>
        public static CampaignActions.ChainTrace ResolveThroughOutlet(CampaignActions.ActionSpec spec, MediaOutlet outlet,
            double electorate, double[] groupPopulationShares, double[] groupSalience, double[] groupMatch,
            double credibility, double spend, StrategyModifiers modifiers, double[] groupLoyalty = null,
            CampaignStrategy strategy = CampaignStrategy.None, bool[] groupPrioritises = null)
        {
            int groups = groupPopulationShares.Length;
            double[] composition = outlet.AudienceComposition ?? groupPopulationShares;
            if (composition.Length != groups || groupSalience.Length != groups || groupMatch.Length != groups)
            {
                throw new ArgumentException("one entry per voter group");
            }

            double reach = 0, salienceSum = 0, exposure = 0, relevanceSum = 0, cred = 0, persuasion = 0, enthusiasm = 0, shift = 0;
            double weightSum = 0.0;
            for (int g = 0; g < groups; g++)
            {
                double w = composition[g];
                if (w <= 0.0) { continue; }

                StrategyModifiers m = groupLoyalty != null
                    ? CampaignStrategyModel.Modifiers(strategy, groupLoyalty[g], groupPrioritises != null && groupPrioritises[g])
                    : modifiers;

                double audience = electorate * outlet.Reach * w;
                CampaignActions.ChainTrace t = CampaignStrategyModel.Resolve(spec, audience, groupSalience[g], groupMatch[g], credibility, spend, m);
                reach += t.Reach; exposure += t.Exposure; persuasion += t.Persuasion; enthusiasm += t.Enthusiasm; shift += t.SalienceShift;
                salienceSum += w * t.Salience; relevanceSum += w * t.Relevance; cred += w * t.Credibility;
                weightSum += w;
            }

            if (weightSum <= 0.0) { return new CampaignActions.ChainTrace(spec.Kind, 0, 0, 0, 0, 0, 0, 0, 0); }

            return new CampaignActions.ChainTrace(spec.Kind, reach, salienceSum / weightSum, exposure, relevanceSum / weightSum,
                cred / weightSum, persuasion, enthusiasm, shift);
        }
    }

    /// <summary>
    /// Coverage per party as a decaying stock (§13). A day's raw newsworthiness is accumulated,
    /// then `CloseDay` saturates it, adds it to the stock, decays the stock, and reports the day's
    /// GAIN — which is what creates momentum. Nothing here can grow without bound.
    /// </summary>
    public sealed class MediaCoverage
    {
        private readonly double[] _coverage;
        private readonly double[] _rawToday;

        public MediaCoverage(int partyCount)
        {
            _coverage = new double[partyCount];
            _rawToday = new double[partyCount];
        }

        public int PartyCount => _coverage.Length;

        public double Coverage(int party) => _coverage[party];

        /// <summary>Record one action's raw newsworthiness for today.</summary>
        public void AddRaw(int party, double raw) => _rawToday[party] += Math.Max(0.0, raw);

        /// <summary>Add an external shock (a debate, an event, a scandal — §15/§17/§18's hooks) straight into today's raw figure.</summary>
        public void AddShock(int party, double raw) => AddRaw(party, raw);

        /// <summary>
        /// Close the day: saturate today's raw figure, decay the stock, add the gain, clear the
        /// day. Returns the gain per party — the input to §13's momentum link.
        /// </summary>
        public double[] CloseDay(double days = 1.0)
        {
            double decay = MediaSystem.CoverageDecay(days);
            var gain = new double[_coverage.Length];
            for (int p = 0; p < _coverage.Length; p++)
            {
                gain[p] = MediaSystem.SaturatedGain(_rawToday[p]);
                _coverage[p] = _coverage[p] * decay + gain[p];
                _rawToday[p] = 0.0;
            }

            return gain;
        }

        /// <summary>The ceiling a party's coverage can approach however much it does — the "cannot spiral" bound, stated as a number.</summary>
        public static double Ceiling(double days = 1.0) => MediaSystem.CoverageScale / (1.0 - MediaSystem.CoverageDecay(days));
    }

    /// <summary>Media interest as AVAILABILITY: who gets booked today, decided by the outlets.</summary>
    public static class MediaInterest
    {
        /// <summary>
        /// Each outlet's slots go to the parties above its threshold IN PROPORTION to their
        /// interest (largest-remainder rounding, ties to the lower index). Deterministic — the
        /// booker's rule, not a draw. A party under every threshold gets nothing, whatever it
        /// would pay; a party twice as newsworthy as another gets about twice its airtime, and
        /// the most newsworthy cannot monopolise an outlet that finds several parties worth
        /// booking.
        ///
        /// ⚠ The first draft booked each outlet's slots to its most interesting parties one per
        /// round; with four outlets each restarting at the top, the two most newsworthy parties
        /// took every slot in the country and the third-largest party was never booked in 56
        /// days. Proportional allocation is what "candidate popularity influences coverage"
        /// means without meaning "only the popular exist".
        /// </summary>
        public static List<InterviewBooking> Bookings(MediaOutlet[] outlets, double[] interest)
        {
            var bookings = new List<InterviewBooking>();
            int parties = interest.Length;

            for (int o = 0; o < outlets.Length; o++)
            {
                MediaOutlet outlet = outlets[o];
                var eligible = new List<int>();
                double total = 0.0;
                for (int p = 0; p < parties; p++)
                {
                    if (interest[p] >= outlet.InterestThreshold && interest[p] > 0.0) { eligible.Add(p); total += interest[p]; }
                }

                if (eligible.Count == 0 || outlet.DailySlots <= 0) { continue; }

                // Largest remainder: floors first, then the leftover slots to the largest fractions
                // (one remainder slot per party per pass, so a leftover cannot all go to one party).
                var whole = new int[eligible.Count];
                var fraction = new double[eligible.Count];
                int given = 0;
                for (int i = 0; i < eligible.Count; i++)
                {
                    double exact = outlet.DailySlots * interest[eligible[i]] / total;
                    whole[i] = (int)Math.Floor(exact);
                    fraction[i] = exact - whole[i];
                    given += whole[i];
                }

                while (given < outlet.DailySlots)
                {
                    int best = -1;
                    for (int i = 0; i < eligible.Count; i++)
                    {
                        if (fraction[i] < 0.0) { continue; }
                        if (best < 0 || fraction[i] > fraction[best]) { best = i; }
                    }

                    if (best < 0)
                    {
                        for (int i = 0; i < fraction.Length; i++) { fraction[i] = 0.0; }
                        continue;
                    }

                    whole[best]++;
                    fraction[best] = -1.0;
                    given++;
                }

                for (int i = 0; i < eligible.Count; i++)
                {
                    for (int s = 0; s < whole[i]; s++) { bookings.Add(new InterviewBooking(o, eligible[i])); }
                }
            }

            return bookings;
        }

        /// <summary>
        /// Bookings with memory: each outlet carries every party's fractional entitlement from one
        /// day to the next, so a party owed half a slot a day is booked every other day instead of
        /// never. ⚠ Day-by-day largest-remainder rounding booked the same three parties every day
        /// and starved the fourth most newsworthy — a party at 19 % of the vote went eight weeks
        /// without an interview. The ledger is the outlets' diary, not a party's; it lives in
        /// `CampaignRun` beside the coverage stock.
        /// </summary>
        public sealed class BookingLedger
        {
            private readonly double[][] _credit;

            public BookingLedger(int outletCount, int partyCount)
            {
                _credit = new double[outletCount][];
                for (int o = 0; o < outletCount; o++) { _credit[o] = new double[partyCount]; }
            }

            /// <summary>Today's bookings: each outlet adds each eligible party's exact share of its slots to that party's credit, then books the highest credits one slot at a time, charging one slot per booking.</summary>
            public List<InterviewBooking> Allocate(MediaOutlet[] outlets, double[] interest)
            {
                var bookings = new List<InterviewBooking>();
                for (int o = 0; o < outlets.Length; o++)
                {
                    MediaOutlet outlet = outlets[o];
                    double total = 0.0;
                    for (int p = 0; p < interest.Length; p++)
                    {
                        if (interest[p] >= outlet.InterestThreshold && interest[p] > 0.0) { total += interest[p]; }
                    }

                    if (total <= 0.0 || outlet.DailySlots <= 0) { continue; }

                    double[] credit = _credit[o];
                    for (int p = 0; p < interest.Length; p++)
                    {
                        if (interest[p] >= outlet.InterestThreshold && interest[p] > 0.0)
                        {
                            credit[p] += outlet.DailySlots * interest[p] / total;
                        }
                        else
                        {
                            credit[p] = 0.0;   // out of the outlet's sight today - no entitlement carried
                        }
                    }

                    for (int s = 0; s < outlet.DailySlots; s++)
                    {
                        int best = -1;
                        for (int p = 0; p < interest.Length; p++)
                        {
                            if (credit[p] <= 0.0) { continue; }
                            if (best < 0 || credit[p] > credit[best]) { best = p; }
                        }

                        if (best < 0) { break; }
                        credit[best] -= 1.0;
                        bookings.Add(new InterviewBooking(o, best));
                    }
                }

                return bookings;
            }
        }

        /// <summary>How many interview slots a party holds in a day's bookings.</summary>
        public static int SlotsFor(List<InterviewBooking> bookings, int party)
        {
            int n = 0;
            foreach (InterviewBooking b in bookings) { if (b.PartyIndex == party) { n++; } }
            return n;
        }
    }

    /// <summary>
    /// [AUTHORED-DRAFT] the outlet roster as ARCHETYPES over a staged group table. Real outlets'
    /// reach (Kantar / Orvesto) is billed as a data line; nothing here carries a real name.
    /// Compositions are given for the harness's two-group table (young-urban / older-rural) and
    /// collapse to reach alone for a one-group electorate.
    /// </summary>
    public static class MediaCatalog
    {
        public static MediaOutlet[] Archetypes(int groupCount)
        {
            double[] general = null;
            double[] young = groupCount == 2 ? new[] { 0.75, 0.25 } : null;
            double[] older = groupCount == 2 ? new[] { 0.30, 0.70 } : null;
            double[] urban = groupCount == 2 ? new[] { 0.65, 0.35 } : null;
            return new[]
            {
                new MediaOutlet("Public broadcaster", 0.45, 3, 0.15, general, isTelevision: true),
                new MediaOutlet("Commercial television", 0.35, 2, 0.25, older, isTelevision: true),
                new MediaOutlet("Tabloid", 0.30, 2, 0.10, young),
                new MediaOutlet("Broadsheet", 0.15, 2, 0.30, urban),
            };
        }
    }
}
