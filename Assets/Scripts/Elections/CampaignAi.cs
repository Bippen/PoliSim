using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-C1 / SPEC §32 + §33 — AI parties that run THE SAME campaign system the player does, each
    /// with a strategy personality, choosing actions by expected value. PURE FUNCTIONS, WIRED TO
    /// NOTHING (R-N2): nothing here advances a day, touches a `World`, or is reachable from a
    /// gameplay path; <see cref="CampaignRun"/> drives it from the harness.
    ///
    /// **The item's bar, stated as a type: an AI cannot access hidden state the player cannot
    /// buy (§36).** Every decision is a function of an <see cref="AiView"/>, and an `AiView` is
    /// built from a <see cref="Poll"/> (which cannot carry the truth — W-B10), from
    /// <see cref="IssueMeasurement"/>s that come out of a commissioned poll with the sampling error
    /// that sample size buys (§21), and from public facts (the calendar, the party's own books, the
    /// electorate's size, its own record). There is no member on the view that could hold a true
    /// preference vector, a true salience or a true issue-match — `CampaignAiHarness` reflects over
    /// it to prove that, the way `PollingHarness` proves it for `Poll`. An AI that has not bought a
    /// poll either acts BLIND — on a flat prior over what it has not measured — or, if its
    /// personality will not (<see cref="PersonalityProfile.ActsBlind"/> false), does not act at
    /// all until it has polled. That is what makes "uses polling" a behaviour rather than a label.
    ///
    /// **§33's scoring, term for term, in ONE unit — the model's own compatibility points:**
    /// <code>
    /// value  = expectedGain × targetImportance × probabilityOfSuccess
    /// score  = (value − cost) × (1 − riskAversion × riskScale × relativeWidth) / hours
    /// </code>
    /// - *expectedGain* — §42's chain evaluated through <see cref="CampaignActions.ResolveBand"/>
    ///   on the MEASURED salience and issue-match (the same function W-E3's action screen prices
    ///   with), read at the personality's <see cref="PersonalityProfile.Optimism"/> point of the
    ///   band, converted to points by `CampaignPressure.PersuasionPerCompatibilityPoint` plus
    ///   enthusiasm weighted by <see cref="PersonalityProfile.EnthusiasmValue"/>;
    /// - *targetImportance* — the personality's affinity for the action (§32's descriptions as
    ///   multipliers) times how salient the message's issue is relative to the most salient one
    ///   measured;
    /// - *probabilityOfSuccess* — how much of the band's high end its low end guarantees: a tight
    ///   measurement approaches 1, a blind guess is 0.5;
    /// - *cost* — money, priced at the action's OWN efficiency at its smallest outlay: a bigger
    ///   spend has to keep <see cref="PersonalityProfile.CostWeight"/> of that efficiency to be
    ///   worth it (§35 makes this a real trade — the curve is concave, so the big outlay is always
    ///   less efficient and the personality decides how much less it tolerates). There is no
    ///   authored exchange rate between kronor and votes; the action prices its own money.
    ///   Money is otherwise a CONSTRAINT: a party spends from a reserve it tops up at its own pace
    ///   (<see cref="PersonalityProfile.SpendPace"/> × even pacing over the days left), so a
    ///   500 000 kr television buy is something a party saves for, not something priced against
    ///   one day;
    /// - *hours* — the binding daily resource (W-B2: they cannot be banked), so candidates rank by
    ///   value per hour of the candidate's day;
    /// - *risk* — the band's width relative to its expected value, times the personality's
    ///   <see cref="PersonalityProfile.RiskAversion"/> (negative for the risk-seeker) at
    ///   <see cref="RiskScale"/>.
    ///
    /// Actions are then chosen by a softmax over positive scores at the personality's
    /// <see cref="PersonalityProfile.Temperature"/> (0 = always the best), drawn from a
    /// `System.Random` the CALLER supplies — the harness hands in `SimulationRandom`'s appended
    /// `CampaignAi` stream, so an AI-only campaign reproduces exactly under a seed (0.6).
    ///
    /// **What is NOT here, by ruling.** §33's example scores an "Attack Opponent" action; §12's
    /// eight have no attack verb and W-B8/§11's negative campaign is where it belongs, so no attack
    /// is invented. Media interest (W-B9) does not exist yet, so a free interview scores exactly as
    /// dominantly here as W-B3 and W-E3 recorded — the AI makes the recorded finding visible in a
    /// third place rather than hiding it behind a cap (the standing ruling). The horse-race poll is
    /// on the view but does not enter the score: "targets swing voters" needs §25's swing index
    /// (W-E2) and "reacts quickly to events" needs §18's events — both later, neither invented.
    ///
    /// **[AUTHORED-DRAFT] throughout** (R-N4): every personality parameter in
    /// <see cref="PersonalityCatalog"/>, <see cref="RiskScale"/>, <see cref="PollingHours"/>,
    /// <see cref="LocalCandidateRegions"/>, <see cref="IssueCandidates"/>. One line each in the
    /// prototype log; the play-calibration list carries them as a block.
    /// </summary>
    public enum AiPersonality
    {
        Professional = 0,
        Populist = 1,
        Establishment = 2,
        Grassroots = 3,
        Chaotic = 4,
    }

    /// <summary>
    /// What a party has MEASURED about one issue: a polled salience and issue-match with the ±
    /// their sample size bought (§21), or nothing at all. <see cref="Measured"/> false means no
    /// estimate exists — never a wide one (§36, the W-E3 rule).
    /// </summary>
    public readonly struct IssueMeasurement
    {
        public readonly bool Measured;
        public readonly double Salience;
        public readonly double SalienceError;
        public readonly double Match;
        public readonly double MatchError;
        public readonly int SampleSize;

        public IssueMeasurement(double salience, double salienceError, double match, double matchError, int sampleSize)
        {
            Measured = true;
            Salience = salience; SalienceError = salienceError;
            Match = match; MatchError = matchError;
            SampleSize = sampleSize;
        }

        public static IssueMeasurement None => default;
    }

    /// <summary>A region as a public fact: its name and how many people an action there can reach.</summary>
    public readonly struct RegionAudience
    {
        public readonly string Name;
        public readonly double Audience;
        /// <summary>W-B4: the volunteer-hours the party's own office in this region still has today (0 without an office) - added to the ceiling on doors a door-to-door action can knock there.</summary>
        public readonly double VolunteerHours;

        public RegionAudience(string name, double audience, double volunteerHours = 0.0)
        {
            Name = name; Audience = audience; VolunteerHours = volunteerHours;
        }
    }

    /// <summary>
    /// Everything an AI party is allowed to know when it decides. **No truth can be expressed
    /// here**: the race is a <see cref="Poll"/>, the issues are <see cref="IssueMeasurement"/>s,
    /// and the rest is public (the calendar, the party's own books, the electorate's size, its own
    /// record). The harness asserts by reflection that no member's name suggests otherwise.
    /// </summary>
    public readonly struct AiView
    {
        public readonly int PartyIndex;
        public readonly CampaignPhase Phase;
        public readonly int DaysUntilElection;
        public readonly ResourcePool Resources;
        /// <summary>The party's own spending reserve — what its pace has released for use so far and not yet spent. Its own bookkeeping, not a fact about the world.</summary>
        public readonly double SpendingReserve;
        /// <summary>The most recent poll this party has seen — its own commission or the public tracker; false when no poll has been published or bought yet.</summary>
        public readonly bool HasPoll;
        public readonly Poll LatestPoll;
        /// <summary>§22's momentum as a poll-watcher sees it (percentage points per party); public, because momentum is a property of the published race.</summary>
        public readonly double[] MomentumPp;
        /// <summary>One entry per <see cref="IssueId"/>; <see cref="IssueMeasurement.Measured"/> false where this party has never polled the issue.</summary>
        public readonly IssueMeasurement[] Issues;
        /// <summary>The party's own credibility with the electorate — its own record, which it knows.</summary>
        public readonly double OwnCredibility;
        public readonly double NationalAudience;
        public readonly RegionAudience[] Regions;
        /// <summary>Days since this party last commissioned a poll; -1 when it never has.</summary>
        public readonly int DaysSinceOwnPoll;
        /// <summary>§11's strategy this party has chosen for itself (W-B6) — its own decision, which it knows.</summary>
        public readonly CampaignStrategy OwnStrategy;
        /// <summary>The electorate's loyalty (0–100) as W-A1 derives it from PUBLISHED past returns — a public fact, so a strategy can be priced against it.</summary>
        public readonly double ElectorateLoyalty;
        /// <summary>W-B9: the interview bookings this party still holds today, as the reach (share of the electorate) of each booking outlet — its own diary. Empty = nobody will book it today, whatever it would pay.</summary>
        public readonly double[] InterviewReachToday;
        /// <summary>W-B9: the largest outlet's reach ceiling — where a television buy goes; a public fact about the media.</summary>
        public readonly double BestOutletReach;
        /// <summary>What this party's own poll costs (§21's price list, W-E4) — so a party that polls can keep the money back for it.</summary>
        public readonly double PollCost;
        /// <summary>W-B9: the audience each NATIONAL §12 action can reach through the media landscape today, in `TheEight`'s order (television's and the platforms' ceilings, the party's own following, the press's interest in it) — public facts, or its own; null = the whole electorate for every kind (W-B3's placeholder).</summary>
        public readonly double[] NationalAudienceByKind;
        /// <summary>W-B11: the volunteer-hours this party still has today - the bound on how many doors a door-to-door action can knock (its own books).</summary>
        public readonly double VolunteerHoursToday;
        /// <summary>W-B5: what the campaign manager's budget plan has set aside for television (its own books) - spendable on a television buy and on nothing else; 0 without a manager.</summary>
        public readonly double TelevisionFund;

        public AiView(int partyIndex, CampaignPhase phase, int daysUntilElection, ResourcePool resources,
            double spendingReserve, bool hasPoll, Poll latestPoll, double[] momentumPp, IssueMeasurement[] issues,
            double ownCredibility, double nationalAudience, RegionAudience[] regions, int daysSinceOwnPoll,
            CampaignStrategy ownStrategy = CampaignStrategy.None, double electorateLoyalty = 50.0,
            double[] interviewReachToday = null, double bestOutletReach = 1.0, double pollCost = 0.0,
            double[] nationalAudienceByKind = null, double volunteerHoursToday = 0.0, double televisionFund = 0.0)
        {
            NationalAudienceByKind = nationalAudienceByKind; VolunteerHoursToday = volunteerHoursToday; TelevisionFund = televisionFund;
            PartyIndex = partyIndex; Phase = phase; DaysUntilElection = daysUntilElection; Resources = resources;
            SpendingReserve = spendingReserve; HasPoll = hasPoll; LatestPoll = latestPoll; MomentumPp = momentumPp;
            Issues = issues; OwnCredibility = ownCredibility; NationalAudience = nationalAudience; Regions = regions;
            DaysSinceOwnPoll = daysSinceOwnPoll; OwnStrategy = ownStrategy; ElectorateLoyalty = electorateLoyalty;
            InterviewReachToday = interviewReachToday ?? new double[0]; BestOutletReach = bestOutletReach;
            PollCost = pollCost;
        }

        /// <summary>
        /// The leading OTHER party in the latest poll this party has seen — the negative campaign's
        /// target, chosen from a Poll and nothing else. -1 when no poll has been seen.
        /// </summary>
        public int PolledLeaderOtherThanSelf
        {
            get
            {
                if (!HasPoll) { return -1; }
                int leader = -1;
                double best = -1.0;
                for (int i = 0; i < LatestPoll.PartyCount; i++)
                {
                    if (i == PartyIndex) { continue; }
                    if (LatestPoll.Share(i) > best) { best = LatestPoll.Share(i); leader = i; }
                }

                return leader;
            }
        }

        public bool HasAnyIssueMeasurement
        {
            get
            {
                if (Issues == null) { return false; }
                foreach (IssueMeasurement m in Issues) { if (m.Measured) { return true; } }
                return false;
            }
        }
    }

    /// <summary>§32's personality as parameters over §33's terms. [AUTHORED-DRAFT] throughout; see <see cref="PersonalityCatalog"/> for the five.</summary>
    public readonly struct PersonalityProfile
    {
        public readonly AiPersonality Kind;
        public readonly string Name;
        private readonly double[] _affinity;
        /// <summary>Softmax temperature over positive scores (relative to the best); 0 always takes the best.</summary>
        public readonly double Temperature;
        /// <summary>Weight on the estimate band's relative width; negative prefers wide bands (the risk-seeker).</summary>
        public readonly double RiskAversion;
        /// <summary>Where in the band the expected gain is read: 0 = the low end, 1 = the high end.</summary>
        public readonly double Optimism;
        /// <summary>How much of an action's efficiency at its smallest outlay a bigger outlay must keep to be worth it (1 = only the most efficient level; 0 = money is no object).</summary>
        public readonly double CostWeight;
        /// <summary>Multiplier on even pacing: the reserve grows by SpendPace × (money / days left) per day.</summary>
        public readonly double SpendPace;
        /// <summary>How much an enthusiasm (turnout) point is worth relative to a persuasion point.</summary>
        public readonly double EnthusiasmValue;
        /// <summary>Commission a poll every N campaign days; 0 = never polls.</summary>
        public readonly int PollEveryDays;
        /// <summary>True: every message is aimed at the single highest-measured-salience issue (§32's populist).</summary>
        public readonly bool FocusOnTopSalience;
        /// <summary>False: this personality will not act on an unmeasured estimate at all — it polls first or waits.</summary>
        public readonly bool ActsBlind;
        /// <summary>The spend levels evaluated per money action, as multiples of the spec's cost (§35 makes the choice real).</summary>
        public readonly double[] SpendMultipliers;
        /// <summary>§11's strategy this personality runs (W-B6) — the modifiers over the whole chain that its own estimates and the world's response both apply.</summary>
        public readonly CampaignStrategy Strategy;

        public PersonalityProfile(AiPersonality kind, string name, double[] affinity, double temperature,
            double riskAversion, double optimism, double costWeight, double spendPace, double enthusiasmValue,
            int pollEveryDays, bool focusOnTopSalience, bool actsBlind, double[] spendMultipliers,
            CampaignStrategy strategy = CampaignStrategy.None)
        {
            Strategy = strategy;
            if (affinity == null || affinity.Length != CampaignActions.TheEight.Length)
            {
                throw new ArgumentException("one affinity per §12 action, in TheEight's order");
            }

            if (spendMultipliers == null || spendMultipliers.Length == 0)
            {
                throw new ArgumentException("at least one spend level");
            }

            Kind = kind; Name = name; _affinity = affinity; Temperature = temperature;
            RiskAversion = riskAversion; Optimism = optimism; CostWeight = costWeight;
            SpendPace = spendPace; EnthusiasmValue = enthusiasmValue; PollEveryDays = pollEveryDays;
            FocusOnTopSalience = focusOnTopSalience; ActsBlind = actsBlind; SpendMultipliers = spendMultipliers;
        }

        public double Affinity(CampaignActionKind kind) => _affinity[CampaignAi.IndexOfAction(kind)];

        /// <summary>The smallest spend level — the efficiency anchor money is priced against.</summary>
        public double SmallestSpendMultiplier
        {
            get
            {
                double min = double.MaxValue;
                foreach (double m in SpendMultipliers) { if (m < min) { min = m; } }
                return min;
            }
        }
    }

    /// <summary>
    /// The five personalities of §32, each parameter a reading of the spec's own bullet list.
    /// [AUTHORED-DRAFT] — every number strikeable, all to be calibrated by play against a loop
    /// (the play-calibration list carries the block). Affinities are in `TheEight`'s order:
    /// rally, town hall, door-to-door, TV ad, digital ad, social post, interview, policy announcement.
    /// </summary>
    public static class PersonalityCatalog
    {
        public static readonly AiPersonality[] TheFive =
        {
            AiPersonality.Professional, AiPersonality.Populist, AiPersonality.Establishment,
            AiPersonality.Grassroots, AiPersonality.Chaotic,
        };

        public static PersonalityProfile Profile(AiPersonality kind)
        {
            switch (kind)
            {
                case AiPersonality.Professional:
                    // "Uses polling, targets swing voters, allocates money efficiently, reacts quickly":
                    // neutral affinities (the numbers decide), always the best action, risk-averse,
                    // reads the band conservatively, polls weekly, will not act blind, only the
                    // most efficient outlay, even pacing.
                    return new PersonalityProfile(kind, "Professional",
                        new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 },
                        temperature: 0.0, riskAversion: 1.0, optimism: 0.35, costWeight: 1.0,
                        spendPace: 1.0, enthusiasmValue: 0.5, pollEveryDays: 7,
                        focusOnTopSalience: false, actsBlind: false, spendMultipliers: new[] { 0.5, 1.0, 2.0 },
                        strategy: CampaignStrategy.SwingVoter);

                case AiPersonality.Populist:
                    // "High-salience issues, large rallies, social media heavy, aggressive": rallies and
                    // social posts weighted up, every message on the top issue, optimistic, big
                    // outlays tolerated, front-loaded pacing.
                    return new PersonalityProfile(kind, "Populist",
                        new[] { 1.8, 0.8, 0.8, 0.9, 1.1, 1.8, 1.2, 0.6 },
                        temperature: 0.15, riskAversion: 0.3, optimism: 0.7, costWeight: 0.4,
                        spendPace: 1.6, enthusiasmValue: 1.0, pollEveryDays: 14,
                        focusOnTopSalience: true, actsBlind: true, spendMultipliers: new[] { 1.0, 2.0, 3.0 },
                        strategy: CampaignStrategy.Populist);

                case AiPersonality.Establishment:
                    // "Strong traditional media, broad messaging, moderate policies": television,
                    // interviews and policy announcements weighted up, general messages, cautious,
                    // even pacing, a bigger buy tolerated at 70 % efficiency.
                    return new PersonalityProfile(kind, "Establishment",
                        new[] { 0.8, 1.0, 0.6, 1.8, 1.0, 0.6, 1.6, 1.4 },
                        temperature: 0.05, riskAversion: 1.2, optimism: 0.5, costWeight: 0.7,
                        spendPace: 1.0, enthusiasmValue: 0.4, pollEveryDays: 14,
                        focusOnTopSalience: false, actsBlind: true, spendMultipliers: new[] { 0.5, 1.0, 2.0 },
                        strategy: CampaignStrategy.BroadAppeal);

                case AiPersonality.Grassroots:
                    // "Low advertising budget, strong volunteers, door-to-door, high turnout":
                    // door-knocking and town halls up, broadcast down, enthusiasm valued, thrifty
                    // pacing, only the most efficient outlay.
                    return new PersonalityProfile(kind, "Grassroots",
                        new[] { 1.0, 1.5, 2.2, 0.2, 0.5, 1.2, 1.0, 0.8 },
                        temperature: 0.10, riskAversion: 0.8, optimism: 0.5, costWeight: 1.0,
                        spendPace: 0.7, enthusiasmValue: 1.6, pollEveryDays: 21,
                        focusOnTopSalience: false, actsBlind: true, spendMultipliers: new[] { 0.5, 1.0 },
                        strategy: CampaignStrategy.BaseMobilization);

                case AiPersonality.Chaotic:
                    // "Inconsistent strategy, high-risk decisions, unpredictable messaging": a hot
                    // softmax, risk-SEEKING, reads the top of every band, never polls, acts blind,
                    // money no object, burns the war chest.
                    return new PersonalityProfile(kind, "Chaotic",
                        new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 },
                        temperature: 1.0, riskAversion: -0.6, optimism: 1.0, costWeight: 0.2,
                        spendPace: 2.5, enthusiasmValue: 0.8, pollEveryDays: 0,
                        focusOnTopSalience: false, actsBlind: true, spendMultipliers: new[] { 0.5, 1.0, 3.0 },
                        strategy: CampaignStrategy.NegativeCampaign);

                default:
                    throw new ArgumentException($"{kind} is not one of §32's five personalities");
            }
        }
    }

    /// <summary>One decision for the day: what, where, about what, how much, and why (the score, for the log).</summary>
    public readonly struct AiDecision
    {
        public readonly CampaignActionKind Kind;
        public readonly CampaignActions.ActionTarget Target;
        public readonly string TargetLabel;
        public readonly double Spend;
        public readonly double Hours;
        public readonly double Score;
        /// <summary>True when the estimate behind the score was a blind flat prior rather than a measurement.</summary>
        public readonly bool Blind;

        public AiDecision(CampaignActionKind kind, CampaignActions.ActionTarget target, string targetLabel,
            double spend, double hours, double score, bool blind)
        {
            Kind = kind; Target = target; TargetLabel = targetLabel; Spend = spend; Hours = hours;
            Score = score; Blind = blind;
        }
    }

    /// <summary>A scored candidate — every term §33 names, kept so the harness can print them. Points are compatibility points.</summary>
    public readonly struct ScoredCandidate
    {
        public readonly AiDecision Decision;
        public readonly double ExpectedPoints;
        public readonly double TargetImportance;
        public readonly double ProbabilityOfSuccess;
        public readonly double CostPoints;
        public readonly double RiskFactor;
        public readonly bool Measured;

        public ScoredCandidate(AiDecision decision, double expectedPoints, double targetImportance,
            double probabilityOfSuccess, double costPoints, double riskFactor, bool measured)
        {
            Decision = decision; ExpectedPoints = expectedPoints; TargetImportance = targetImportance;
            ProbabilityOfSuccess = probabilityOfSuccess; CostPoints = costPoints; RiskFactor = riskFactor; Measured = measured;
        }
    }

    public static class CampaignAi
    {
        /// <summary>[AUTHORED-DRAFT] how much a band as wide as its own expected value discounts the score at risk aversion 1 (a blind estimate at optimism 0.5 is twice as wide as its expectation).</summary>
        public const double RiskScale = 0.25;

        /// <summary>[AUTHORED-DRAFT] §9 gives no hour cost for commissioning a poll; one hour of a campaign day.</summary>
        public const double PollingHours = 1.0;

        /// <summary>[AUTHORED-DRAFT] how many of the largest regions a local action is evaluated in (the rest are not considered — a bound on evaluation, not a rule of the world).</summary>
        public const int LocalCandidateRegions = 4;

        /// <summary>[AUTHORED-DRAFT] how many of the highest-measured-salience issues a non-focused personality considers, beside the general message.</summary>
        public const int IssueCandidates = 2;

        public static int IndexOfAction(CampaignActionKind kind)
        {
            for (int i = 0; i < CampaignActions.TheEight.Length; i++)
            {
                if (CampaignActions.TheEight[i] == kind) { return i; }
            }

            throw new ArgumentException($"{kind} is not one of §12's eight campaign actions");
        }

        /// <summary>What the pace releases into the reserve for one day: SpendPace × even pacing over the days left.</summary>
        public static double DailyRelease(PersonalityProfile profile, double money, int daysLeft)
        {
            if (money <= 0.0) { return 0.0; }
            return profile.SpendPace * money / Math.Max(1, daysLeft);
        }

        /// <summary>
        /// Whether this party commissions a poll today: it polls at all, it is due, and it can pay
        /// from its reserve. Decided BEFORE the action loop so the day's estimates use the fresh
        /// measurement.
        /// </summary>
        public static bool WantsPoll(AiView view, PersonalityProfile profile, PollingHouse house)
        {
            if (profile.PollEveryDays <= 0) { return false; }
            if (!CampaignLegality.IsLegal(CampaignActionKind.CommissionPolling, view.Phase)) { return false; }

            bool due = view.DaysSinceOwnPoll < 0 || view.DaysSinceOwnPoll >= profile.PollEveryDays;
            if (!due) { return false; }

            return house.Cost <= view.Resources.Money && house.Cost <= view.SpendingReserve && PollingHours <= view.Resources.Hours;
        }

        /// <summary>
        /// Every candidate (action × spend level × target × issue) this party could run RIGHT NOW,
        /// scored by §33 against the given resources and reserve. Legal in the phase, affordable
        /// from the reserve, and — for a personality that will not act blind — measured, or not
        /// listed: the AI never plans an action it cannot pay for (W-B2's refusal, honoured here).
        /// </summary>
        public static List<ScoredCandidate> Evaluate(AiView view, PersonalityProfile profile, ResourcePool available, double reserve)
        {
            // A party that polls keeps its poll's price back once the poll is due: the measurement
            // comes before the spending it would inform.
            double reservation = 0.0;
            if (profile.PollEveryDays > 0 && (view.DaysSinceOwnPoll < 0 || view.DaysSinceOwnPoll >= profile.PollEveryDays))
            {
                reservation = view.PollCost;
            }

            double spendableNow = Math.Max(0.0, Math.Min(available.Money, reserve) - reservation);
            return Candidates(view, profile, available, spendableNow);

            // ⚠ There is deliberately NO saving rule here. Two were tried at W-B9 and both were
            // worse than none: "save for the better action, do only free things meanwhile" left a
            // party with no bookings idle for days; "save, but keep doing the cheap things" left
            // the establishment party posting on social media for a week at a time to afford one
            // television buy, making no news and never getting booked. A big-ticket buy needs a
            // BUDGET PLAN - a share of the war chest set aside per channel before the campaign -
            // which is a campaign manager's job (§9's staff, W-B5) and is recorded there, not
            // improvised as a greedy heuristic here. Until then the pace releases money evenly
            // and a party buys what it can afford on the day.
        }

        private static List<ScoredCandidate> Candidates(AiView view, PersonalityProfile profile, ResourcePool available, double spendable)
        {
            var result = new List<ScoredCandidate>();

            // Issue candidates: the measured issues by measured salience, the general message beside them.
            List<int> issueOrder = MeasuredIssuesBySalience(view);
            double topSalience = issueOrder.Count > 0 ? Math.Max(1e-9, view.Issues[issueOrder[0]].Salience) : 1.0;
            IssueId? topIssue = issueOrder.Count > 0 ? (IssueId)issueOrder[0] : (IssueId?)null;
            var issueChoices = new List<IssueId?>();
            if (issueOrder.Count == 0)
            {
                issueChoices.Add(null);   // nothing measured - the only message is a blind general one
            }
            else if (profile.FocusOnTopSalience)
            {
                issueChoices.Add((IssueId)issueOrder[0]);
            }
            else
            {
                issueChoices.Add(null);
                for (int i = 0; i < issueOrder.Count && i < IssueCandidates; i++) { issueChoices.Add((IssueId)issueOrder[i]); }
            }

            // Region candidates for local actions: the largest few, by public audience.
            List<int> regionChoices = LargestRegions(view, LocalCandidateRegions);

            foreach (CampaignActionKind kind in CampaignActions.TheEight)
            {
                if (!CampaignLegality.IsLegal(kind, view.Phase)) { continue; }

                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                if (spec.Hours > available.Hours) { continue; }

                double[] spends = spec.MoneyCost > 0 ? profile.SpendMultipliers : new[] { 1.0 };
                double smallest = spec.MoneyCost > 0 ? spec.MoneyCost * profile.SmallestSpendMultiplier : 0.0;

                foreach (double multiplier in spends)
                {
                    double spend = spec.MoneyCost > 0 ? spec.MoneyCost * multiplier : 0.0;
                    // W-B5: the manager's television fund is spendable on television and on nothing else.
                    if (spend > spendable + (kind == CampaignActionKind.TelevisionAd ? view.TelevisionFund : 0.0)) { continue; }

                    if (spec.IsLocal)
                    {
                        foreach (int region in regionChoices)
                        {
                            // W-B11: a door-to-door action reaches the doors the volunteers can knock (money and
                            // hours both bind), wherever it is aimed; a rally or town hall still draws on the region.
                            double localAudience = kind == CampaignActionKind.DoorToDoor
                                ? GotvModel.Contacts(GotvModel.Spec(GotvOperation.DoorKnocking), spend, view.VolunteerHoursToday + view.Regions[region].VolunteerHours, out _, out _)
                                : view.Regions[region].Audience;
                            if (localAudience <= 0.0) { continue; }

                            foreach (IssueId? issue in issueChoices)
                            {
                                ScoredCandidate? c = Score(view, profile, spec, spend, smallest, region, view.Regions[region].Name,
                                    localAudience, issue, topSalience, topIssue);
                                if (c.HasValue) { result.Add(c.Value); }
                            }
                        }
                    }
                    else
                    {
                        // W-B9: an interview needs a booking (its audience is the booking outlet's
                        // reach - no booking, no candidate: availability, not a price); television
                        // goes through the biggest outlet, whose reach is a ceiling on the electorate.
                        double nationalAudience = view.NationalAudienceByKind != null
                            ? view.NationalAudienceByKind[IndexOfAction(kind)]
                            : view.NationalAudience;
                        string label = "National";
                        if (kind == CampaignActionKind.Interview)
                        {
                            if (view.InterviewReachToday == null || view.InterviewReachToday.Length == 0) { continue; }
                            nationalAudience = view.NationalAudience * view.InterviewReachToday[0];
                            label = "Booked outlet";
                        }
                        else if (kind == CampaignActionKind.TelevisionAd)
                        {
                            if (view.NationalAudienceByKind == null) { nationalAudience = view.NationalAudience * view.BestOutletReach; }
                            label = "Television";
                        }
                        else if (kind == CampaignActionKind.SocialPost) { label = "Own following"; }
                        else if (kind == CampaignActionKind.DigitalAd) { label = "Platforms"; }
                        else if (kind == CampaignActionKind.PolicyAnnouncement) { label = "The press"; }

                        foreach (IssueId? issue in issueChoices)
                        {
                            ScoredCandidate? c = Score(view, profile, spec, spend, smallest, -1, label,
                                nationalAudience, issue, topSalience, topIssue);
                            if (c.HasValue) { result.Add(c.Value); }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Softmax over candidates with a positive score; null when none has one — doing nothing
        /// is a legal, and sometimes correct, decision. Scores are taken RELATIVE to the best so
        /// the temperature means the same thing whatever the day's magnitudes. The
        /// <paramref name="random"/> is the caller's (the harness passes SimulationRandom's
        /// `CampaignAi` stream) and is drawn from ONLY when the temperature is positive, so a
        /// temperature-0 personality consumes no randomness at all. A day is planned by calling
        /// this repeatedly against a shrinking pool (<see cref="CampaignRun"/> does, applying each
        /// choice to the world in between).
        /// </summary>
        public static ScoredCandidate? Choose(List<ScoredCandidate> candidates, double temperature, System.Random random)
        {
            ScoredCandidate? best = null;
            foreach (ScoredCandidate c in candidates)
            {
                if (c.Decision.Score <= 0.0) { continue; }
                if (best == null || c.Decision.Score > best.Value.Decision.Score) { best = c; }
            }

            if (best == null) { return null; }
            if (temperature <= 0.0) { return best; }

            double top = best.Value.Decision.Score;
            double total = 0.0;
            var weights = new double[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                double s = candidates[i].Decision.Score;
                if (s <= 0.0) { continue; }
                weights[i] = Math.Exp((s / top - 1.0) / temperature);
                total += weights[i];
            }

            double u = random.NextDouble() * total;
            double running = 0.0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (weights[i] <= 0.0) { continue; }
                running += weights[i];
                if (u <= running) { return candidates[i]; }
            }

            return best;
        }

        // ---------- the terms ----------

        private static ScoredCandidate? Score(AiView view, PersonalityProfile profile, CampaignActions.ActionSpec spec,
            double spend, double smallestSpend, int regionIndex, string targetLabel, double audience, IssueId? issue,
            double topSalience, IssueId? topIssue)
        {
            // What the party has MEASURED about the message's subject - or nothing.
            bool measured = TryMeasurement(view, issue, out double salience, out double salienceError,
                out double match, out double matchError);
            if (!measured && !profile.ActsBlind) { return null; }   // no estimate, and this personality will not guess

            // The party's own strategy (W-B6) modifies its own estimate exactly as it will modify
            // the world's response: the electorate as one group at its published loyalty, the
            // message "prioritised" when it is on the most salient issue the party has measured.
            StrategyModifiers m = CampaignStrategyModel.Modifiers(view.OwnStrategy, view.ElectorateLoyalty,
                issue.HasValue && topIssue.HasValue && issue.Value == topIssue.Value);

            Band(spec, audience, view.OwnCredibility, spend, measured, salience, salienceError, match, matchError,
                profile, m, out double expectedPts, out double spanPts, out double lowPts, out double highPts);

            double importance = profile.Affinity(spec.Kind) * (measured ? Math.Max(0.0, salience) / topSalience : 0.5);
            double probabilityOfSuccess = highPts > 0 ? 0.5 + 0.5 * (lowPts / highPts) : 0.5;
            double value = expectedPts * importance * probabilityOfSuccess;

            // Money, priced at the action's own efficiency at its smallest outlay (§35: concave,
            // so every bigger outlay is less efficient; CostWeight says how much less is tolerated).
            // ⚠ Only the INCREMENT above the smallest outlay is priced. The first draft priced the
            // whole spend, so at CostWeight 1.0 the smallest outlay's cost equalled its value and
            // every money action scored exactly zero for the professional and grassroots
            // personalities - floating-point noise then decided whether a party announced policy
            // or held town halls all campaign. Money's first claim is the reserve (a constraint);
            // the money term is what a BIGGER outlay costs beyond it.
            double costPts = 0.0;
            if (spend > smallestSpend && smallestSpend > 0.0)
            {
                Band(spec, audience, view.OwnCredibility, smallestSpend, measured, salience, salienceError, match, matchError,
                    profile, m, out double smallExpected, out _, out double smallLow, out double smallHigh);
                double smallSuccess = smallHigh > 0 ? 0.5 + 0.5 * (smallLow / smallHigh) : 0.5;
                double smallValue = smallExpected * importance * smallSuccess;
                double efficiency = smallValue / smallestSpend;   // points per krona at the smallest outlay
                costPts = profile.CostWeight * (spend - smallestSpend) * efficiency;
            }

            double relativeWidth = expectedPts > 0 ? spanPts / expectedPts : 0.0;
            double riskFactor = 1.0 - profile.RiskAversion * RiskScale * relativeWidth;
            if (riskFactor < 0.0) { riskFactor = 0.0; }

            double score = (value - costPts) * riskFactor / Math.Max(0.25, spec.Hours);

            var target = new CampaignActions.ActionTarget(regionIndex, -1, issue);
            var decision = new AiDecision(spec.Kind, target, targetLabel, spend, spec.Hours, score, !measured);
            return new ScoredCandidate(decision, expectedPts, importance, probabilityOfSuccess, costPts, riskFactor, measured);
        }

        /// <summary>§42's band at this spend, read in points at the personality's optimism; blind = a flat prior over both unmeasured inputs.</summary>
        private static void Band(CampaignActions.ActionSpec spec, double audience, double credibility, double spend, bool measured,
            double salience, double salienceError, double match, double matchError, PersonalityProfile profile, StrategyModifiers m,
            out double expectedPts, out double spanPts, out double lowPts, out double highPts)
        {
            CampaignActions.ChainBand band = measured
                ? CampaignActions.ResolveBand(spec, audience, salience, salienceError, match, matchError, credibility, spend)
                : CampaignActions.ResolveBand(spec, audience, 0.5, 0.5, 0.5, 0.5, credibility, spend);

            lowPts = Points(band.Low, profile, m);
            highPts = Points(band.High, profile, m);
            expectedPts = lowPts + profile.Optimism * (highPts - lowPts);
            spanPts = highPts - lowPts;
        }

        /// <summary>
        /// Persuasion and enthusiasm pressures converted to the model's own units and weighted by
        /// what this personality values, under the party's own strategy modifiers (W-B6): reach and
        /// credibility scale both pressures linearly in the chain, so applying them here is exactly
        /// what `CampaignStrategyModel.Resolve` does to the world's response.
        /// </summary>
        private static double Points(CampaignActions.ChainTrace trace, PersonalityProfile profile, StrategyModifiers m)
        {
            double linear = m.ReachMultiplier * m.CredibilityMultiplier;
            return trace.Persuasion * linear * m.PersuasionMultiplier / CampaignPressure.PersuasionPerCompatibilityPoint
                   + profile.EnthusiasmValue * trace.Enthusiasm * linear * m.EnthusiasmMultiplier / CampaignPressure.EnthusiasmPerTurnoutPoint;
        }

        /// <summary>The measurement behind a message: one issue's, or for a general message the mean over the issues measured.</summary>
        private static bool TryMeasurement(AiView view, IssueId? issue, out double salience, out double salienceError,
            out double match, out double matchError)
        {
            salience = salienceError = match = matchError = 0.0;
            if (view.Issues == null) { return false; }

            if (issue.HasValue)
            {
                IssueMeasurement m = view.Issues[(int)issue.Value];
                if (!m.Measured) { return false; }
                salience = m.Salience; salienceError = m.SalienceError; match = m.Match; matchError = m.MatchError;
                return true;
            }

            int n = 0;
            foreach (IssueMeasurement m in view.Issues)
            {
                if (!m.Measured) { continue; }
                salience += m.Salience; salienceError += m.SalienceError; match += m.Match; matchError += m.MatchError;
                n++;
            }

            if (n == 0) { return false; }
            salience /= n; salienceError /= n; match /= n; matchError /= n;
            return true;
        }

        private static List<int> MeasuredIssuesBySalience(AiView view)
        {
            var order = new List<int>();
            if (view.Issues == null) { return order; }
            for (int i = 0; i < view.Issues.Length; i++) { if (view.Issues[i].Measured) { order.Add(i); } }
            order.Sort((a, b) => view.Issues[b].Salience.CompareTo(view.Issues[a].Salience));
            return order;
        }

        private static List<int> LargestRegions(AiView view, int count)
        {
            var order = new List<int>();
            if (view.Regions == null) { return order; }
            for (int i = 0; i < view.Regions.Length; i++) { order.Add(i); }
            order.Sort((a, b) => view.Regions[b].Audience.CompareTo(view.Regions[a].Audience));
            if (order.Count > count) { order.RemoveRange(count, order.Count - count); }
            return order;
        }
    }

    /// <summary>
    /// §21's internal polling applied to ISSUES — what a commissioned poll tells a party about
    /// salience and issue-match, with the sampling error its sample size buys. **The only function
    /// in this file that touches a true value**, and it returns <see cref="IssueMeasurement"/>s
    /// that carry the measured figure and its ± but not the truth — the `PollingSystem.Conduct`
    /// idiom. Each measured proportion is a genuine binomial draw at the sample size, so the error
    /// has the right distribution and the reported ± is honest in the same sense W-B10 proved.
    /// </summary>
    public static class CampaignIntelligence
    {
        public static IssueMeasurement[] MeasureIssues(double[] trueSalience, double[] trueMatch, int sampleSize, System.Random random)
        {
            if (trueSalience == null || trueMatch == null || trueSalience.Length != trueMatch.Length)
            {
                throw new ArgumentException("salience and match must be one per issue");
            }

            if (sampleSize <= 0) { throw new ArgumentException("sample size must be positive"); }
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            var result = new IssueMeasurement[trueSalience.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (double.IsNaN(trueSalience[i]) || double.IsNaN(trueMatch[i]))
                {
                    result[i] = IssueMeasurement.None;   // not an issue in this contest - nothing to measure
                    continue;
                }

                double s = Sample(trueSalience[i], sampleSize, random);
                double m = Sample(trueMatch[i], sampleSize, random);
                result[i] = new IssueMeasurement(s, HalfWidth(s, sampleSize), m, HalfWidth(m, sampleSize), sampleSize);
            }

            return result;
        }

        /// <summary>A proportion measured by asking <paramref name="n"/> respondents, one at a time.</summary>
        private static double Sample(double p, int n, System.Random random)
        {
            double clamped = p < 0 ? 0 : (p > 1 ? 1 : p);
            int yes = 0;
            for (int r = 0; r < n; r++) { if (random.NextDouble() < clamped) { yes++; } }
            return (double)yes / n;
        }

        /// <summary>The 95 % half-width on the 0–1 scale — `PollingSystem.MarginOfErrorPp` in fractions rather than points.</summary>
        private static double HalfWidth(double share, int n) => PollingSystem.MarginOfErrorPp(share, n) / 100.0;
    }
}
