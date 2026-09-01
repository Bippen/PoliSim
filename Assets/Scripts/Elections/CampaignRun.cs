using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-C1 — an AI-ONLY campaign, run day by day through the same pieces the player's screens
    /// read: W-B1's calendar, W-B2's resources, W-B3's actions and §42 chain, W-B10's polls, W-A1's
    /// derived loyalty and §8's preference model. PURE, WIRED TO NOTHING (R-N2): the harness is the
    /// only caller, it is handed everything in a <see cref="Setup"/>, and it touches no `World`.
    ///
    /// **This type IS the world for the duration of a run, and is therefore the one place the
    /// truth lives**: it holds the electorate's real salience and each party's real issue-match,
    /// resolves every action against them, recomputes the true preference at each day's end, and
    /// conducts every poll from it. The AIs see none of that directly — they are handed an
    /// <see cref="AiView"/> built from the polls they have bought or been published, exactly as a
    /// player would be (§36). The seam is the point: `CampaignAi` cannot read a field this type
    /// does not put on the view.
    ///
    /// **What moves and what does not.** The true preference is
    /// `PreferenceModel.Preference(compatibility + campaignBonus, prior, loyaltyPerParty)` — the
    /// same recomputation W-B3's harness proved, so a campaign changes inputs and the shares are
    /// always derived; with no actions at all the shares equal the prior by construction (asserted).
    /// Momentum (§22) is shocked by the day's COVERAGE gain (W-B9: `MediaCoverage.CloseDay` ×
    /// `MediaSystem.MomentumPpPerCoverage`, bounded because the gain saturates) and by nothing
    /// else yet — debates (§15) and events (§18) will add their own shocks. Interviews are
    /// BOOKINGS: each day the outlets allocate their slots by media interest (coverage, momentum,
    /// the PUBLISHED race), and a party without a booking has no interview to give. Pre-campaign
    /// days are not simulated: §3's preparation verbs have no
    /// price yet, so the run covers the campaign proper (W-B1's `CampaignStart` to the day before
    /// polling day) and stops at election day, which is W-D1's.
    /// </summary>
    public static class CampaignRun
    {
        /// <summary>One party as the run needs it: who it is, how it decides, what it knows about itself, and the truth about it the AI cannot see.</summary>
        public readonly struct PartySetup
        {
            public readonly string Name;
            public readonly AiPersonality Personality;
            public readonly double Credibility;
            public readonly double StartingMoney;
            /// <summary>The TRUE issue-match per <see cref="IssueId"/> (NaN where the issue is not contested). Never handed to the AI; measured through polling.</summary>
            public readonly double[] TrueIssueMatch;
            /// <summary>W-B11: the party's volunteers (§9) - their hours bound how many doors a day it can knock (`GotvModel.Contacts`); §10's offices (W-B4) grow them.</summary>
            public readonly int Volunteers;
            /// <summary>W-B7: the party's candidate (§16's attributes, [AUTHORED-DRAFT] game fiction, W-F6's to label) - who stands in its debates. Default: a flat 60 everywhere.</summary>
            public readonly CandidateProfile Candidate;
            /// <summary>W-B4: the regions the party opens §10 offices in on day 0 (its plan - [AUTHORED-DRAFT] staging per personality until W-B5/W-C2 site them), and what each office spends on its own daily operation. Empty = no offices.</summary>
            public readonly int[] Offices;
            public readonly double OfficeOperationsPerDay;
            /// <summary>W-B5: the roles the party hires on day 0 (§9, [AUTHORED-DRAFT] staging per personality) and, if it hires a campaign manager, how many television buys the manager's budget plan sets money aside for.</summary>
            public readonly StaffRole[] Staff;
            public readonly int TelevisionBuys;
            /// <summary>W-C2: a SCRIPTED party - the harness's stand-in for the player: given the campaign day, the decisions it makes that day, in order, resolved through the same seams as an AI's (paid, resolved, seen). Null = an AI party.</summary>
            public readonly Func<int, AiDecision[]> Script;

            public PartySetup(string name, AiPersonality personality, double credibility, double startingMoney, double[] trueIssueMatch, int volunteers = 0,
                CandidateProfile? candidate = null, int[] offices = null, double officeOperationsPerDay = 0.0, StaffRole[] staff = null, int televisionBuys = 0,
                Func<int, AiDecision[]> script = null)
            {
                Name = name; Personality = personality; Credibility = credibility; StartingMoney = startingMoney;
                TrueIssueMatch = trueIssueMatch; Volunteers = volunteers;
                Candidate = candidate ?? new CandidateProfile(name, 60, 60, 60, 60, 60, 60, 60, 60, 60);
                Offices = offices ?? new int[0]; OfficeOperationsPerDay = officeOperationsPerDay;
                Staff = staff ?? new StaffRole[0]; TelevisionBuys = televisionBuys;
                Script = script;
            }
        }

        public readonly struct Setup
        {
            public readonly CampaignCalendar Calendar;
            public readonly PartySetup[] Parties;
            /// <summary>Where the electorate's vote sat at the last election (the §8 prior), one per party, summing to 1.</summary>
            public readonly double[] PriorShares;
            /// <summary>W-A1's derived loyalty per party.</summary>
            public readonly double[] LoyaltyPerParty;
            /// <summary>Each party's compatibility with the electorate at the campaign's start (0–100).</summary>
            public readonly double[] Compatibility;
            /// <summary>The TRUE salience per <see cref="IssueId"/> (NaN = not an issue in this contest).</summary>
            public readonly double[] TrueSalience;
            public readonly double NationalAudience;
            public readonly RegionAudience[] Regions;
            /// <summary>The published tracker every party sees, and how often it fields.</summary>
            public readonly PollingHouse PublicHouse;
            public readonly int PublicPollEveryDays;
            /// <summary>What a party buys when it commissions its own poll (§21): horse race AND issue detail.</summary>
            public readonly PollingHouse InternalHouse;
            /// <summary>The electorate's loyalty as ONE group (0–100) — W-A1's size-weighted mean, a public derivation from past returns — until W-F4's voter groups give §11's strategies their per-group targets.</summary>
            public readonly double ElectorateLoyalty;
            /// <summary>W-B9: the outlets that book interviews and carry television (§13/§14). Null = the archetype roster over a one-group electorate.</summary>
            public readonly MediaOutlet[] Outlets;
            /// <summary>W-B7: the campaign days (0-based) on which the two parties leading the PUBLISHED poll debate. Null = the default two, at the end of weeks three and six.</summary>
            public readonly int[] DebateDays;
            /// <summary>W-B8: scandals staged to break - (campaign day, party, the scandal). Null = none. §17's dynamic generation (a probability per day from §36's hidden variables) is a later item; today the harness stages them.</summary>
            public readonly (int Day, int Party, Scandal Scandal)[] Scandals;

            public Setup(CampaignCalendar calendar, PartySetup[] parties, double[] priorShares, double[] loyaltyPerParty,
                double[] compatibility, double[] trueSalience, double nationalAudience, RegionAudience[] regions,
                PollingHouse publicHouse, int publicPollEveryDays, PollingHouse internalHouse, double electorateLoyalty = 50.0,
                MediaOutlet[] outlets = null, int[] debateDays = null, (int Day, int Party, Scandal Scandal)[] scandals = null)
            {
                ElectorateLoyalty = electorateLoyalty;
                Scandals = scandals ?? new (int, int, Scandal)[0];
                Outlets = outlets ?? MediaCatalog.Archetypes(1);
                DebateDays = debateDays ?? new[] { 20, 41 };
                if (parties == null || parties.Length == 0) { throw new ArgumentException("no parties"); }
                if (priorShares.Length != parties.Length || loyaltyPerParty.Length != parties.Length || compatibility.Length != parties.Length)
                {
                    throw new ArgumentException("prior, loyalty and compatibility must be one per party");
                }

                Calendar = calendar; Parties = parties; PriorShares = priorShares; LoyaltyPerParty = loyaltyPerParty;
                Compatibility = compatibility; TrueSalience = trueSalience; NationalAudience = nationalAudience;
                Regions = regions; PublicHouse = publicHouse; PublicPollEveryDays = publicPollEveryDays;
                InternalHouse = internalHouse;
            }
        }

        public readonly struct DecisionRecord
        {
            public readonly int Day;
            public readonly CampaignActionKind Kind;
            public readonly string Target;
            public readonly double Spend;
            public readonly double Score;
            public readonly bool Blind;

            public DecisionRecord(int day, CampaignActionKind kind, string target, double spend, double score, bool blind)
            {
                Day = day; Kind = kind; Target = target; Spend = spend; Score = score; Blind = blind;
            }
        }

        /// <summary>What one party did over the campaign — the action mix the done-when compares.</summary>
        public sealed class PartyLedger
        {
            public readonly string Name;
            public readonly AiPersonality Personality;
            /// <summary>Count per §12 action, in `TheEight`'s order.</summary>
            public readonly int[] ActionCount = new int[CampaignActions.TheEight.Length];
            public readonly double[] MoneyByAction = new double[CampaignActions.TheEight.Length];
            public int PollsBought;
            public double PollMoney;
            public int BlindDecisions;
            /// <summary>W-B9: interview slots the outlets offered this party over the campaign, and its coverage stock at the end.</summary>
            public int SlotsOffered;
            public double CoverageAtEnd;
            /// <summary>W-B7: debates stood and won.</summary>
            public int DebatesStood;
            public int DebatesWon;
            /// <summary>W-B8: the party's credibility at the end - its starting figure less every scandal's lasting cost.</summary>
            public double CredibilityAtEnd;
            public int ScandalsSurvived;
            /// <summary>W-B4: the party's offices - how many opened, what they cost over the campaign (opening, maintenance, operations), the doors their own operations knocked, their volunteers at the end.</summary>
            public int OfficesOpened;

            /// <summary>D-1 (c): offices the party PLANNED and could not afford to keep, so did not open.
            /// Reported rather than silently dropped - a plan quietly shrinking is the kind of change that
            /// looks like a bug in a later measurement, and this is the number that explains it.</summary>
            public int OfficesUnaffordable;
            public double OfficeMoney;
            public double OfficeContacts;
            public int OfficeVolunteersAtEnd;
            /// <summary>W-B5: the payroll - staff hired, what their salaries cost over the campaign (the resource ledger's line), days a member went unpaid, and what the manager's plan still held at the end.</summary>
            public int StaffHired;
            public double StaffMoney;
            public int UnpaidStaffDays;
            public double TelevisionFundAtEnd;
            /// <summary>W-D4: the persuasion pressure this party's own actions delivered, per §12 action kind - RECORDED where it lands, never recomputed (the ApprovalAttribution principle), so §31's ledger sums to the movement it explains.</summary>
            public double[] PersuasionByAction = new double[CampaignActions.TheEight.Length];
            /// <summary>W-D4: persuasion pressure aimed AGAINST this party by others' negative campaigning, as a positive magnitude.</summary>
            public double PersuasionAgainstMe;
            /// <summary>C-N1: persuasion the day's earned coverage carried (§39's Media Effects layer) - not any action's, so not in <see cref="PersuasionByAction"/>; included in <see cref="PersuasionDelivered"/>.</summary>
            public double PersuasionFromCoverage;
            /// <summary>W-C2: the party's reactions - offices opened in contested regions, town halls held there, announcements answering attacks.</summary>
            public int OfficesOpenedInReaction;
            public int Defences;
            public int Answers;
            /// <summary>The campaign day on which the party had spent 80 % of its war chest (the total day count if it never did) - front-loading against pacing.</summary>
            public int DayEightyPercentSpent = -1;
            public double MoneyLeft;
            public double PersuasionDelivered;
            public double EnthusiasmDelivered;
            public readonly List<DecisionRecord> Log = new List<DecisionRecord>();
            /// <summary>Action counts per campaign day (day x TheEight) - how consistent the party's strategy was from one day to the next.</summary>
            public int[][] DailyActionCount;

            public PartyLedger(string name, AiPersonality personality)
            {
                Name = name; Personality = personality;
            }

            public int TotalActions
            {
                get { int n = 0; foreach (int c in ActionCount) { n += c; } return n; }
            }

            public double MoneySpentOnActions
            {
                get { double m = 0.0; foreach (double x in MoneyByAction) { m += x; } return m; }
            }

            /// <summary>The action mix as fractions of all actions taken (zeros when none) — the vector two personalities are compared on.</summary>
            public double[] Mix()
            {
                var mix = new double[ActionCount.Length];
                int total = TotalActions;
                if (total == 0) { return mix; }
                for (int i = 0; i < mix.Length; i++) { mix[i] = (double)ActionCount[i] / total; }
                return mix;
            }
        }

        public sealed class Result
        {
            public double[] FinalShares;
            /// <summary>The shares with no campaign at all — the prior, by construction; asserted rather than assumed.</summary>
            public double[] BaselineShares;
            public PartyLedger[] Parties;
            public int DaysRun;
            public int PublicPolls;
            /// <summary>W-B9: §22 momentum per party at the end — moved by coverage and nothing else.</summary>
            public double[] MomentumPpAtEnd;
            /// <summary>W-B11 → W-D1: every party's ground contacts per valkrets over the campaign - what election day's turnout reads.</summary>
            public RegionalMobilization Gotv;
            /// <summary>W-B4: every party's office network at the end.</summary>
            public OfficeNetwork[] Offices;
            /// <summary>W-B5: every party's staff roster at the end.</summary>
            public StaffRoster[] Staff;
            /// <summary>W-C2: the public record of visible acts at the end - what any party could see.</summary>
            public PublicActivity Activity;
            /// <summary>W-D4: every party's total persuasion pressure at the close - §31's ledger needs the opponents' bloc as well as the party's own lines.</summary>
            public double[] PersuasionPerParty;
            /// <summary>W-B7: every debate held - day, the two parties, the margin (positive = the first won), the shocks.</summary>
            public List<(int Day, int A, int B, double Margin, double CoverageShock, double MomentumShockPp)> Debates;
            /// <summary>W-B8: every scandal - day, party, the response chosen, the outcome.</summary>
            public List<(int Day, int Party, ScandalResponse Response, ScandalOutcome Outcome)> Scandals;
            /// <summary>The valkretsar, in the run's order - election day's names.</summary>
            public string[] RegionNames;
            /// <summary>A deterministic digest of every decision and the final shares — two runs of one seed must print the same one.</summary>
            public string Digest;
        }

        public static Result Simulate(Setup setup, System.Random random, System.Random debateRandom = null, System.Random scandalRandom = null)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }
            debateRandom = debateRandom ?? random;
            scandalRandom = scandalRandom ?? random;
            var debates = new List<(int Day, int A, int B, double Margin, double CoverageShock, double MomentumShockPp)>();
            var scandals = new List<(int Day, int Party, ScandalResponse Response, ScandalOutcome Outcome)>();
            var credibility = new double[setup.Parties.Length];
            for (int p = 0; p < credibility.Length; p++) { credibility[p] = setup.Parties[p].Credibility; }
            var pendingCoverage = new Dictionary<int, List<(int Party, double Raw)>>();   // a scandal's story, day by day

            int partyCount = setup.Parties.Length;
            int issueCount = IssueVector.IssueCount;

            // --- the truth, held here and nowhere the AI can reach ---
            double[] prior = Normalised(setup.PriorShares);
            var pressure = new CampaignPressure(partyCount);
            double[] truePreference = CurrentPreference(setup, prior, pressure);
            double[] baseline = (double[])truePreference.Clone();

            var momentum = new MomentumTracker(partyCount);
            var momentumPp = new double[partyCount];

            // --- W-B9: the media, an independent force ---
            var coverage = new MediaCoverage(partyCount);
            var bookingLedger = new MediaInterest.BookingLedger(setup.Outlets.Length, partyCount);   // the outlets' diary - entitlement carried day to day
            var bookedReach = new List<double>[partyCount];
            Poll? publicPoll = null;   // what the MEDIA see of the race: the published tracker, never an internal poll
            double bestOutletReach = MediaOutlet.TelevisionReach(setup.Outlets);   // a television buy runs across the television outlets

            // --- what each party knows ---
            var ledgers = new PartyLedger[partyCount];
            var pools = new ResourcePool[partyCount];
            var latestPoll = new Poll?[partyCount];
            var issues = new IssueMeasurement[partyCount][];
            var lastOwnPollDay = new int[partyCount];
            var reserve = new double[partyCount];
            var volunteerHoursLeft = new double[partyCount];   // W-B11: a day's volunteer-hours, the bound on doors knocked
            var regionEligible = new double[setup.Regions.Length];
            for (int r = 0; r < regionEligible.Length; r++) { regionEligible[r] = setup.Regions[r].Audience; }
            var gotv = new RegionalMobilization(regionEligible, partyCount);   // W-B11: the ground game election day will read (W-D1)
            var offices = new OfficeNetwork[partyCount];                       // W-B4: each party's §10 offices
            var officeHoursLeft = new double[partyCount][];                     // W-B4: each office's volunteer-hours still unspent today, per region
            var staff = new StaffRoster[partyCount];                            // W-B5: each party's §9 staff and, with a manager, its budget plan
            var activity = new PublicActivity(partyCount, setup.Regions.Length);   // W-C2: the public record of everyone's visible acts
            var lastDefence = new int[partyCount];                              // W-C2: the cooldowns on a party's reactions
            var lastAnswer = new int[partyCount];   // W-C2: ONE answer at a time - a campaign answers the attack it is under, not each attacker separately
            for (int p = 0; p < partyCount; p++)
            {
                lastDefence[p] = int.MinValue / 2;
                lastAnswer[p] = int.MinValue / 2;
            }
            var profiles = new PersonalityProfile[partyCount];
            for (int p = 0; p < partyCount; p++)
            {
                ledgers[p] = new PartyLedger(setup.Parties[p].Name, setup.Parties[p].Personality);
                ledgers[p].DailyActionCount = new int[setup.Calendar.TotalCampaignDays][];
                for (int d0 = 0; d0 < ledgers[p].DailyActionCount.Length; d0++) { ledgers[p].DailyActionCount[d0] = new int[CampaignActions.TheEight.Length]; }
                pools[p] = new ResourcePool(setup.Parties[p].StartingMoney, 0.0, setup.Parties[p].Volunteers);

                // W-B4: the party's offices open on day 0, each paying its opening cost from the war chest.
                offices[p] = new OfficeNetwork(setup.Regions.Length);
                officeHoursLeft[p] = new double[setup.Regions.Length];
                double chest = pools[p].Money;
                // D-1 (c), RULED 2026-08-31: **the office plan is scaled to what the party can afford
                // to KEEP, not merely to open.** W-F5 measured a mandate-proportional pool bankrupting
                // five of eight parties, and C-D2 found the driver: V and MP plan 1.91 M kr of offices
                // against 0.10 M of payroll, a personality choice uncorrelated with seats. `Open` only
                // ever checked the OPENING cost, so a party bought every office it could afford on day 0
                // and then STARVED them - no recruiting, no operation, influence bleeding away, for money
                // already spent. An office it cannot keep is worse than an office it never opened.
                //
                // The reserve is the CAMPAIGN'S OWN LENGTH, `setup.Calendar.TotalCampaignDays` - derived
                // from the calendar the party is planning for, not a figure typed in. ⚠ It was first
                // written as `CampaignAi.OfficeUpkeepDaysReserved` (10 days), reusing the reactive path's
                // constant, and MEASURED: at the mandate split it dropped ZERO of 27 planned offices,
                // because ten days of upkeep is small beside the 100,000 kr opening cost. **A ruling
                // verified only where it cannot bite is not verified.** Ten days is the right horizon for
                // a TACTICAL office opened mid-campaign to answer an attack; it is the wrong horizon for
                // a PLAN, which is a commitment to election day. ⚠ **D-1 (c) invents no money, and this
                // keeps that promise** - the campaign's length is already in the model.
                // ⚠ The reserve is for the NETWORK, not for one office. A party planning six offices must
                // keep six of them standing, and reserving one office's upkeep six times over is the same
                // arithmetic error as reserving none - measured, again: at one-office-at-a-time it dropped
                // ZERO of 27 even at 56 days, because each individual check passed while the network as a
                // whole was unaffordable. That is precisely the shape of the starvation being fixed.
                double perOfficeUpkeep = setup.Calendar.TotalCampaignDays
                    * (CampaignOffices.MaintenancePerDay + setup.Parties[p].OfficeOperationsPerDay);
                foreach (int region in setup.Parties[p].Offices)
                {
                    int wouldHold = offices[p].Count + 1;
                    if (chest - CampaignOffices.OpenCost < wouldHold * perOfficeUpkeep)
                    {
                        ledgers[p].OfficesUnaffordable++;
                        continue;
                    }

                    if (offices[p].Open(region, 0, setup.Parties[p].OfficeOperationsPerDay, ref chest)) { ledgers[p].OfficesOpened++; }
                }

                ledgers[p].OfficeMoney += pools[p].Money - chest;
                pools[p] = pools[p].WithMoney(chest);

                // W-B5: the party's staff, hired on day 0; a manager brings the budget plan (television buys at the action's price).
                staff[p] = new StaffRoster();
                foreach (StaffRole role in setup.Parties[p].Staff)
                {
                    staff[p].Hire(role, 0, role == StaffRole.CampaignManager
                        ? new BudgetPlan(setup.Parties[p].TelevisionBuys, CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost)
                        : null);
                }

                ledgers[p].StaffHired = staff[p].Count;
                issues[p] = new IssueMeasurement[issueCount];
                lastOwnPollDay[p] = int.MinValue;
                profiles[p] = PersonalityCatalog.Profile(setup.Parties[p].Personality);
            }

            var digest = new StringBuilder();
            int publicPolls = 0;
            int totalDays = setup.Calendar.TotalCampaignDays;
            var names = new string[setup.Regions.Length];
            for (int r = 0; r < names.Length; r++) { names[r] = setup.Regions[r].Name; }

            for (int day = 0; day < totalDays; day++)
            {
                DateTime today = setup.Calendar.CampaignStart.AddDays(day);
                CampaignPhase phase = setup.Calendar.PhaseOn(today);

                // The published tracker: fielded from the true preference, seen by everyone.
                if (setup.PublicPollEveryDays > 0 && day % setup.PublicPollEveryDays == 0)
                {
                    Poll tracker = PollingSystem.Conduct(momentum.Apply(truePreference), setup.PublicHouse, today, random);
                    publicPolls++;
                    publicPoll = tracker;
                    for (int p = 0; p < partyCount; p++) { latestPoll[p] = tracker; }
                }

                // W-B9: the outlets decide whom to book today - from coverage, momentum and the
                // PUBLISHED race, the ruling's "someone else decides whether to book you".
                var interest = new double[partyCount];
                for (int p = 0; p < partyCount; p++)
                {
                    double polledShare = publicPoll.HasValue ? publicPoll.Value.Share(p) : 0.0;
                    interest[p] = MediaSystem.Interest(coverage.Coverage(p), momentumPp[p], polledShare);
                    bookedReach[p] = new List<double>();
                }

                foreach (InterviewBooking booking in bookingLedger.Allocate(setup.Outlets, interest))
                {
                    bookedReach[booking.PartyIndex].Add(setup.Outlets[booking.OutletIndex].Reach);
                    ledgers[booking.PartyIndex].SlotsOffered++;
                }

                // W-B9: what each national channel can reach for each party today - television and the
                // platforms as ceilings, a post to the party's own following, an announcement carried
                // in proportion to the press's interest. The AI is handed the same figures.
                var audienceByKind = new double[partyCount][];
                for (int p = 0; p < partyCount; p++)
                {
                    double polledShare = publicPoll.HasValue ? publicPoll.Value.Share(p) : 0.0;
                    audienceByKind[p] = new double[CampaignActions.TheEight.Length];
                    for (int k = 0; k < CampaignActions.TheEight.Length; k++)
                    {
                        audienceByKind[p][k] = MediaSystem.NationalAudience(CampaignActions.TheEight[k], setup.NationalAudience, setup.Outlets, polledShare, interest[p]);
                    }
                }

                for (int p = 0; p < partyCount; p++)
                {
                    pools[p] = pools[p].StartDay();
                    PartyLedger ledger = ledgers[p];

                    // W-B12: the manager's plan is told what the organisation costs TODAY - the
                    // payroll plus every office's maintenance and operation - before the pace
                    // releases anything, because the release is capped by what the organisation
                    // still needs. A party without a manager has no plan and no such discipline,
                    // which is the difference §9 says a campaign manager makes.
                    {
                        BudgetPlan planning = staff[p].ActivePlan;
                        if (planning != null)
                        {
                            planning.DailyFixedCost = staff[p].DailySalaryBill() + offices[p].DailyCost;
                        }
                    }

                    // The pace releases today's money into the reserve - capped at what the party
                    // has, and for a managed party ALSO at what is left after the organisation's
                    // bill to polling day. Pay the organisation first, release the rest (W-B12).
                    double release = CampaignAi.DailyRelease(profiles[p], pools[p].Money, totalDays - day);
                    double spendable = pools[p].Money;
                    {
                        BudgetPlan planning = staff[p].ActivePlan;
                        if (planning != null)
                        {
                            spendable = Math.Max(0.0, spendable - planning.CommittedToOrganisation(totalDays - day));
                        }
                    }

                    reserve[p] = Math.Min(spendable, reserve[p] + release);
                    volunteerHoursLeft[p] = CampaignEconomy.VolunteerHours(pools[p].Volunteers);

                    // W-B4: the offices' day - maintenance paid (or the office starves), volunteers recruited,
                    // each office's own operation into the ground game; their hours are then the region's
                    // extra ceiling on doors for the party's door-to-door actions today.
                    {
                        double chest = pools[p].Money;
                        ledger.OfficeMoney += offices[p].Day(gotv, p, GotvOperation.DoorKnocking, ref chest, out double officeContacts);
                        ledger.OfficeContacts += officeContacts;
                        pools[p] = pools[p].WithMoney(chest);
                        reserve[p] = Math.Min(reserve[p], pools[p].Money);
                        for (int r = 0; r < officeHoursLeft[p].Length; r++) { officeHoursLeft[p][r] = offices[p].VolunteerHours(r); }
                    }

                    // W-B5: payday - salaries from the party's money (an unpaid member gives nothing today);
                    // the manager's plan sets aside its share of today's release for television; the
                    // advisor's and the strategist's multipliers land on the audiences the AI is handed.
                    {
                        double chest = pools[p].Money;
                        int unpaidBefore = 0;
                        foreach (CampaignStaffMember m in staff[p].Members) { unpaidBefore += m.UnpaidDays; }
                        ledger.StaffMoney += staff[p].PayDay(ref chest);
                        int unpaidAfter = 0;
                        foreach (CampaignStaffMember m in staff[p].Members) { unpaidAfter += m.UnpaidDays; }
                        ledger.UnpaidStaffDays += unpaidAfter - unpaidBefore;
                        pools[p] = pools[p].WithMoney(chest);
                        reserve[p] = Math.Min(reserve[p], pools[p].Money);
                        BudgetPlan plan = staff[p].ActivePlan;
                        if (plan != null) { reserve[p] -= plan.Save(Math.Min(release, reserve[p])); }
                        for (int k = 0; k < audienceByKind[p].Length; k++) { audienceByKind[p][k] *= staff[p].ReachMultiplier(CampaignActions.TheEight[k]); }
                    }

                    // The poll decision is taken on the view BEFORE today's measurement, and the
                    // measurement then feeds the same day's action estimates (a fresh poll is
                    // what you act on, not what you file).
                    AiView view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p], bookedReach[p], bestOutletReach, audienceByKind[p], volunteerHoursLeft[p], credibility, offices[p], officeHoursLeft[p], staff[p], activity);
                    if (CampaignAi.WantsPoll(view, profiles[p], setup.InternalHouse))
                    {
                        if (pools[p].TrySpend(setup.InternalHouse.Cost, CampaignAi.PollingHours, out ResourcePool afterPoll))
                        {
                            pools[p] = afterPoll;
                            reserve[p] -= setup.InternalHouse.Cost;
                            latestPoll[p] = PollingSystem.Conduct(momentum.Apply(truePreference), staff[p].Improve(setup.InternalHouse), today, random);
                            issues[p] = CampaignIntelligence.MeasureIssues(setup.TrueSalience, setup.Parties[p].TrueIssueMatch,
                                staff[p].Improve(setup.InternalHouse).SampleSize, random);
                            lastOwnPollDay[p] = day;
                            ledger.PollsBought++;
                            ledger.PollMoney += setup.InternalHouse.Cost;
                            ledger.Log.Add(new DecisionRecord(day, CampaignActionKind.CommissionPolling, setup.InternalHouse.Name,
                                setup.InternalHouse.Cost, 0.0, false));
                            Append(digest, day, p, CampaignActionKind.CommissionPolling, setup.InternalHouse.Name, setup.InternalHouse.Cost);
                        }
                    }

                    view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p], bookedReach[p], bestOutletReach, audienceByKind[p], volunteerHoursLeft[p], credibility, offices[p], officeHoursLeft[p], staff[p], activity);

                    // Actions: the AI's plan, applied one by one against the TRUE inputs - the
                    // world's response, which the AI estimated but did not see.
                    AiDecision[] scripted = setup.Parties[p].Script?.Invoke(day);

                    // W-C2: what the personality commits to first, on the public record it can see -
                    // an office in a contested region (paid now, kept from tomorrow), the local act its
                    // own affinities prefer there today, and one message answering the attack it is
                    // under. The decisions then go through the same seams as any other and are paid
                    // from the same day's pace; the AI's own weighing takes the hours that remain.
                    var committed = new List<AiDecision>();
                    if (scripted == null)
                    {
                        AiReaction reaction = CampaignAi.Reactions(view, profiles[p]);
                        if (reaction.DefendRegion >= 0 && day - lastDefence[p] >= CampaignAi.DefenceCooldownDays)
                        {
                            int r = reaction.DefendRegion;
                            bool opened = false;
                            if (!offices[p].HasOffice(r) && pools[p].Money >= CampaignOffices.OpenCost + CampaignAi.OfficeUpkeepDaysReserved * (CampaignOffices.MaintenancePerDay + setup.Parties[p].OfficeOperationsPerDay))
                            {
                                double chest = pools[p].Money;
                                if (offices[p].Open(r, day, setup.Parties[p].OfficeOperationsPerDay, ref chest))
                                {
                                    opened = true;
                                    ledger.OfficesOpened++;
                                    ledger.OfficesOpenedInReaction++;
                                    ledger.OfficeMoney += pools[p].Money - chest;
                                    pools[p] = pools[p].WithMoney(chest);
                                    reserve[p] = Math.Min(reserve[p], pools[p].Money);
                                    Append(digest, day, p, CampaignActionKind.EstablishOffice, setup.Regions[r].Name, CampaignOffices.OpenCost);
                                }
                            }

                            // The office is bought first, so the act is committed only if the party
                            // can still pay for it out of today's PACE - a reaction is priority, not
                            // extra money, and a commitment it cannot honour would break the whole
                            // day's plan at the first TrySpend. The counters count what was made.
                            CampaignActions.ActionSpec defence = CampaignActions.Spec(reaction.DefendWith);
                            bool acts = defence.MoneyCost <= Math.Min(pools[p].Money, reserve[p]) && defence.Hours <= pools[p].Hours;
                            if (acts)
                            {
                                committed.Add(new AiDecision(reaction.DefendWith, new CampaignActions.ActionTarget(r, -1, null), setup.Regions[r].Name,
                                    defence.MoneyCost, defence.Hours, 0.0, false));
                            }

                            if (acts || opened)
                            {
                                ledger.Defences++;
                                lastDefence[p] = day;
                                if (opened) { lastDefence[p] = int.MaxValue / 2; }   // the office is the defence from here; the AI's own weighing sees a full region now
                            }
                        }

                        // ONE answer at a time, on the party's own cooldown: a campaign under attack
                        // from six directions makes its own case once a week, it does not spend the
                        // campaign replying. A per-attacker cooldown made every attacked party answer
                        // almost every day, which turned five personalities into one.
                        if (reaction.AnswerTo.Length > 0 && day - lastAnswer[p] >= CampaignAi.AnswerCooldownDays)
                        {
                            CampaignActions.ActionSpec message = CampaignActions.Spec(reaction.AnswerWith);
                            if (message.MoneyCost <= Math.Min(pools[p].Money, reserve[p]) && message.Hours <= pools[p].Hours)
                            {
                                committed.Add(new AiDecision(reaction.AnswerWith, CampaignActions.ActionTarget.National(reaction.AnswerIssue), "national",
                                    message.MoneyCost, message.Hours, 0.0, false));
                                ledger.Answers++;
                                lastAnswer[p] = day;
                            }
                        }

                        if (committed.Count > 0)
                        {
                            view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p], bookedReach[p], bestOutletReach, audienceByKind[p], volunteerHoursLeft[p], credibility, offices[p], officeHoursLeft[p], staff[p], activity);
                        }
                    }

                    for (int guard = 0; guard < 64; guard++)
                    {
                        AiDecision d;
                        if (scripted != null)
                        {
                            // W-C2: a scripted party (the player's stand-in) plays its day as written.
                            if (guard >= scripted.Length) { break; }
                            d = scripted[guard];
                        }
                        else if (guard < committed.Count)
                        {
                            d = committed[guard];
                        }
                        else
                        {
                            List<ScoredCandidate> candidates = CampaignAi.Evaluate(view, profiles[p], pools[p], reserve[p]);
                            ScoredCandidate? chosen = CampaignAi.Choose(candidates, profiles[p].Temperature, random);
                            if (chosen == null) { break; }
                            d = chosen.Value.Decision;
                        }

                        if (!pools[p].TrySpend(d.Spend, d.Hours, out ResourcePool after)) { break; }
                        pools[p] = after;
                        reserve[p] -= d.Spend;
                        if (d.Kind == CampaignActionKind.TelevisionAd && staff[p].ActivePlan != null)
                        {
                            reserve[p] += staff[p].ActivePlan.Pay(d.Spend);   // W-B5: the fund pays first; what it covers was never the day's reserve
                        }
                        if (ledger.DayEightyPercentSpent < 0 && pools[p].Money <= 0.2 * setup.Parties[p].StartingMoney) { ledger.DayEightyPercentSpent = day; }

                        CampaignActions.ActionSpec spec = CampaignActions.Spec(d.Kind);
                        // W-B4: a local action's audience is the region's electorate scaled by the party's
                        // organisation there (a visit without an office draws a quarter of a full office's).
                        double audience = d.Target.RegionIndex >= 0
                            ? CampaignOffices.LocalAudience(setup.Regions[d.Target.RegionIndex].Audience, offices[p].Influence(d.Target.RegionIndex))
                            : audienceByKind[p][CampaignAi.IndexOfAction(d.Kind)];

                        // W-B9: an interview goes out through the outlet that booked it (and consumes
                        // the booking); television across the television outlets - their combined reach is a
                        // ceiling on the whole electorate, a viewership rather than the country.
                        // W-B11: door-to-door reaches the doors the volunteers can actually knock in the
                        // hours they have (money and hours both bind) - an absolute count, not 2 % of
                        // a region. W-B3's placeholder reach fraction no longer applies to it.
                        if (d.Kind == CampaignActionKind.DoorToDoor)
                        {
                            int doorRegion = d.Target.RegionIndex >= 0 ? d.Target.RegionIndex : 0;
                            audience = gotv.Operate(doorRegion, p, GotvOperation.DoorKnocking, d.Spend, volunteerHoursLeft[p] + officeHoursLeft[p][doorRegion], out _, out double doorHours);
                            double fromOffice = Math.Min(doorHours, officeHoursLeft[p][doorRegion]);   // W-B4: the office's volunteers first, headquarters' after
                            officeHoursLeft[p][doorRegion] -= fromOffice;
                            volunteerHoursLeft[p] -= doorHours - fromOffice;
                        }
                        else if (d.Kind == CampaignActionKind.Interview)
                        {
                            if (bookedReach[p].Count == 0) { break; }   // no booking - cannot happen (Evaluate filtered), refuse rather than invent one
                            audience = setup.NationalAudience * bookedReach[p][0];
                            bookedReach[p].RemoveAt(0);
                        }

                        TrueMessage(setup, p, d.Target.Issue, out double salience, out double match);

                        // W-B6: the party's strategy modifies the world's response - the electorate
                        // as one group at its derived loyalty (W-F4's groups make this per group),
                        // the message prioritised when it is on the electorate's most salient issue.
                        StrategyModifiers modifiers = CampaignStrategyModel.Modifiers(profiles[p].Strategy, setup.ElectorateLoyalty,
                            d.Target.Issue.HasValue && IsTopSalience(setup, d.Target.Issue.Value));
                        CampaignActions.ChainTrace trace = CampaignStrategyModel.Resolve(spec, audience, salience, match,
                            credibility[p], d.Spend, modifiers);
                        pressure.Add(p, trace);
                        if (modifiers.OpponentShare > 0.0)
                        {
                            int target = view.PolledLeaderOtherThanSelf;   // chosen from the Poll, not the truth
                            if (target >= 0)
                            {
                                pressure.AddAgainst(target, trace.Persuasion * modifiers.OpponentShare);
                                activity.ObserveAttack(p, target, modifiers.OpponentShare);
                                ledgers[target].PersuasionAgainstMe += trace.Persuasion * modifiers.OpponentShare;   // W-D4
                            }
                        }

                        // W-C2: a scripted attack lands as a negative campaign's would, and is seen.
                        if (d.AgainstParty >= 0 && d.AgainstParty != p)
                        {
                            pressure.AddAgainst(d.AgainstParty, trace.Persuasion * CampaignStrategyModel.NegativeOpponentShare);
                            activity.ObserveAttack(p, d.AgainstParty, 1.0);
                            ledgers[d.AgainstParty].PersuasionAgainstMe += trace.Persuasion * CampaignStrategyModel.NegativeOpponentShare;   // W-D4
                        }

                        // W-C2: a local act is public - the press was there; a rally counts one, the rest half.
                        if (spec.IsLocal && d.Target.RegionIndex >= 0)
                        {
                            activity.ObserveLocal(p, d.Target.RegionIndex, d.Kind == CampaignActionKind.Rally ? 1.0 : 0.5);
                        }

                        // W-B9: every action makes (some) news; the strategy's media attention scales it.
                        coverage.AddRaw(p, MediaSystem.RawNewsworthiness(spec, d.Spend, modifiers));

                        int slot = CampaignAi.IndexOfAction(d.Kind);
                        ledger.ActionCount[slot]++;
                        ledger.DailyActionCount[day][slot]++;
                        ledger.MoneyByAction[slot] += d.Spend;
                        if (d.Blind) { ledger.BlindDecisions++; }
                        ledger.PersuasionDelivered += trace.Persuasion;
                        ledger.PersuasionByAction[slot] += trace.Persuasion;   // W-D4: recorded where it lands
                        ledger.EnthusiasmDelivered += trace.Enthusiasm;
                        ledger.Log.Add(new DecisionRecord(day, d.Kind, d.TargetLabel + IssueSuffix(d.Target.Issue), d.Spend, d.Score, d.Blind));
                        Append(digest, day, p, d.Kind, d.TargetLabel + IssueSuffix(d.Target.Issue), d.Spend);

                        view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p], bookedReach[p], bestOutletReach, audienceByKind[p], volunteerHoursLeft[p], credibility, offices[p], officeHoursLeft[p], staff[p], activity);
                    }
                }

                // The day closes: the true preference is RECOMPUTED from the moved inputs, never patched -
                // at the very end of the day, after the coverage close below (C-N1), so the last day's
                // coverage is in the count. The attribution ledger asserts the close matches (W-D4 1c).

                // W-B8: a staged scandal breaks for its party; the party responds by personality on the
                // evidence AS IT SEES IT (§36); the story's days queue into coverage, the momentum shock
                // lands now, the credibility cost is lasting and the chain prices it from tomorrow.
                foreach ((int sDay, int sParty, Scandal scandal) in setup.Scandals)
                {
                    if (sDay != day) { continue; }
                    double seen = Scandals.EvidenceAsSeen(scandal, scandalRandom);
                    ScandalResponse response = ScandalResponseFor(profiles[sParty].Kind, seen);
                    ScandalOutcome outcome = Scandals.Resolve(scandal, response, scandalRandom);
                    for (int k = 0; k < outcome.CoverageShockPerDay.Length; k++)
                    {
                        if (!pendingCoverage.TryGetValue(day + k, out List<(int Party, double Raw)> list)) { list = new List<(int, double)>(); pendingCoverage[day + k] = list; }
                        list.Add((sParty, outcome.CoverageShockPerDay[k]));
                    }

                    momentum.AddShock(sParty, outcome.MomentumShockPp);
                    credibility[sParty] *= 1.0 - outcome.CredibilityCost;
                    ledgers[sParty].ScandalsSurvived++;
                    scandals.Add((day, sParty, response, outcome));
                    Append(digest, day, sParty, CampaignActionKind.DevelopPolicy, "scandal " + scandal.Kind + " " + response, outcome.CredibilityCost);
                }

                if (pendingCoverage.TryGetValue(day, out List<(int Party, double Raw)> today0))
                {
                    foreach ((int cParty, double raw) in today0) { coverage.AddShock(cParty, raw); }
                }

                // W-B7: on a debate day the two parties leading the PUBLISHED poll debate - each on its
                // personality's plan, on its own ground (its most salient true issue), with its candidate's
                // attributes and a fixed preparation. The result shocks coverage and momentum and nothing else.
                if (publicPoll.HasValue && Array.IndexOf(setup.DebateDays, day) >= 0)
                {
                    int a = -1, b = -1;
                    for (int p = 0; p < partyCount; p++)
                    {
                        if (a < 0 || publicPoll.Value.Share(p) > publicPoll.Value.Share(a)) { b = a; a = p; }
                        else if (b < 0 || publicPoll.Value.Share(p) > publicPoll.Value.Share(b)) { b = p; }
                    }

                    if (a >= 0 && b >= 0)
                    {
                        DebateResult debate = Debates.Resolve(
                            setup.Parties[a].Candidate, DebatePlanFor(profiles[a].Kind, TopIssue(setup, a)), issue => OwnershipOf(setup, a, issue),
                            setup.Parties[b].Candidate, DebatePlanFor(profiles[b].Kind, TopIssue(setup, b)), issue => OwnershipOf(setup, b, issue),
                            DebateExchanges, debateRandom);
                        int winner = debate.Winner == 0 ? a : (debate.Winner == 1 ? b : -1);
                        int loser = winner == a ? b : a;
                        coverage.AddShock(a, debate.CoverageShock);
                        coverage.AddShock(b, debate.CoverageShock);
                        if (winner >= 0) { momentum.AddShock(winner, debate.MomentumShockPp); momentum.AddShock(loser, -debate.MomentumShockPp); }
                        ledgers[a].DebatesStood++; ledgers[b].DebatesStood++;
                        if (winner >= 0) { ledgers[winner].DebatesWon++; }
                        debates.Add((day, a, b, debate.Margin, debate.CoverageShock, debate.MomentumShockPp));
                        Append(digest, day, a, CampaignActionKind.TrainCandidate, "debate v " + setup.Parties[b].Name, debate.Margin);
                    }
                }

                // W-B9 -> §22: the day's coverage GAIN (saturated, so bounded) is the momentum shock;
                // then §22's own decay. Coverage is the only thing that shocks momentum today.
                activity.Decay();   // W-C2: the public record fades on its half-life
                double[] gains = coverage.CloseDay();
                for (int p = 0; p < partyCount; p++) { momentum.AddShock(p, MediaSystem.MomentumPpPerCoverage * gains[p]); }
                // C-N1 -> §39's Media Effects layer: the same day's coverage gain is ALSO a message
                // the press carried, resolved through §42's chain into persuasion (see
                // MediaSystem.CoverageSpec). Momentum still reaches only the poll; this reaches the
                // vote. The NONE strategy's modifiers: the party's strategy already scaled the raw
                // newsworthiness this gain came from, and is not applied twice.
                StrategyModifiers coverageModifiers = CampaignStrategyModel.Modifiers(CampaignStrategy.None, setup.ElectorateLoyalty, false);
                for (int p = 0; p < partyCount; p++)
                {
                    if (gains[p] <= 0.0) { continue; }
                    TrueMessage(setup, p, null, out double reportedSalience, out double reportedMatch);
                    CampaignActions.ChainTrace carried = MediaSystem.ResolveCoverage(gains[p], setup.NationalAudience, setup.Outlets,
                        reportedSalience, reportedMatch, credibility[p], coverageModifiers);
                    pressure.Add(p, carried);
                    ledgers[p].PersuasionFromCoverage += carried.Persuasion;   // W-D4: recorded where it lands - its own attribution line
                    ledgers[p].PersuasionDelivered += carried.Persuasion;
                    ledgers[p].EnthusiasmDelivered += carried.Enthusiasm;
                }
                truePreference = CurrentPreference(setup, prior, pressure);
                momentum.Advance(1.0);
                for (int p = 0; p < partyCount; p++) { momentumPp[p] = momentum.MomentumPp(p); }
            }

            for (int p = 0; p < partyCount; p++)
            {
                ledgers[p].MoneyLeft = pools[p].Money;
                ledgers[p].CoverageAtEnd = coverage.Coverage(p);
                ledgers[p].CredibilityAtEnd = credibility[p];
                ledgers[p].OfficeVolunteersAtEnd = offices[p].TotalVolunteers;
                ledgers[p].TelevisionFundAtEnd = staff[p].Plan?.Fund ?? 0.0;
                if (ledgers[p].DayEightyPercentSpent < 0) { ledgers[p].DayEightyPercentSpent = totalDays; }
            }

            for (int p = 0; p < partyCount; p++)
            {
                digest.Append(truePreference[p].ToString("F9", CultureInfo.InvariantCulture)).Append('|');
            }

            return new Result
            {
                FinalShares = truePreference,
                BaselineShares = baseline,
                Parties = ledgers,
                DaysRun = totalDays,
                PublicPolls = publicPolls,
                MomentumPpAtEnd = (double[])momentumPp.Clone(),
                Gotv = gotv,
                Offices = offices,
                Staff = staff,
                Activity = activity,
                PersuasionPerParty = FinalPersuasion(pressure, partyCount),
                RegionNames = names,
                Debates = debates,
                Scandals = scandals,
                Digest = Fnv1a64(digest.ToString()),
            };
        }

        // ---------- the seam: what the AI is handed ----------

        private static AiView BuildView(Setup setup, int party, CampaignPhase phase, DateTime today, ResourcePool pool,
            double reserve, Poll? latest, double[] momentumPp, IssueMeasurement[] issues, int day, int lastOwnPollDay,
            List<double> bookedReach, double bestOutletReach, double[] audienceByKind, double volunteerHoursToday, double[] credibility,
            OfficeNetwork offices = null, double[] officeHoursLeft = null, StaffRoster staff = null, PublicActivity activity = null)
        {
            int since = lastOwnPollDay == int.MinValue ? -1 : day - lastOwnPollDay;

            // W-B4: the regions as THIS party can reach them - the electorate scaled by its own organisation
            // there, and its own office's unspent volunteer-hours (both its own books, no truth).
            RegionAudience[] regions = setup.Regions;
            if (offices != null)
            {
                regions = new RegionAudience[setup.Regions.Length];
                for (int r = 0; r < regions.Length; r++)
                {
                    regions[r] = new RegionAudience(setup.Regions[r].Name,
                        CampaignOffices.LocalAudience(setup.Regions[r].Audience, offices.Influence(r)),
                        officeHoursLeft != null ? officeHoursLeft[r] : offices.VolunteerHours(r),
                        offices.HasOffice(r));
                }
            }

            return new AiView(party, phase, setup.Calendar.DaysUntilElection(today), pool, reserve,
                latest.HasValue, latest ?? default, (double[])momentumPp.Clone(), issues,
                credibility[party], setup.NationalAudience, regions, since,
                PersonalityCatalog.Profile(setup.Parties[party].Personality).Strategy, setup.ElectorateLoyalty,
                bookedReach.ToArray(), bestOutletReach, setup.InternalHouse.Cost, audienceByKind, volunteerHoursToday,
                staff?.ActivePlan?.Fund ?? 0.0,
                activity?.PressureSeenBy(party), activity?.PushSeenBy(party), activity?.AttackersOf(party));
        }

        /// <summary>[AUTHORED-DRAFT] W-B8: how each personality answers a scandal, on the evidence as it sees it: the professional explains, the establishment apologises, the grassroots party apologises, the populist attacks the source, the chaotic denies - and every one of them denies when the evidence looks weak enough (below 0.3 as seen), because that is what §17 says a denial is for.</summary>
        public static ScandalResponse ScandalResponseFor(AiPersonality personality, double evidenceAsSeen)
        {
            if (evidenceAsSeen < 0.3) { return ScandalResponse.Deny; }
            switch (personality)
            {
                case AiPersonality.Professional: return ScandalResponse.Explain;
                case AiPersonality.Establishment: return ScandalResponse.Apologize;
                case AiPersonality.Grassroots: return ScandalResponse.Apologize;
                case AiPersonality.Populist: return ScandalResponse.AttackSource;
                default: return ScandalResponse.Deny;
            }
        }

        /// <summary>[AUTHORED-DRAFT] W-B7: exchanges per debate and the preparation every AI puts in (it does not plan hours yet - W-B5's staff would).</summary>
        public const int DebateExchanges = 6;
        public const double DebatePreparationHours = 8.0;

        /// <summary>[AUTHORED-DRAFT] W-B7: each personality's debate plan, §32's bullets as §15's moves.</summary>
        public static DebatePreparation DebatePlanFor(AiPersonality personality, IssueId topic)
        {
            DebateMove[] plan;
            switch (personality)
            {
                case AiPersonality.Populist: plan = new[] { DebateMove.AppealEmotionally, DebateMove.AttackOpponent, DebateMove.ChangeSubject }; break;
                case AiPersonality.Professional: plan = new[] { DebateMove.PresentStatistics, DebateMove.DefendPolicy, DebateMove.Counterattack }; break;
                case AiPersonality.Establishment: plan = new[] { DebateMove.DefendPolicy, DebateMove.PresentStatistics, DebateMove.IgnoreAttack }; break;
                case AiPersonality.Grassroots: plan = new[] { DebateMove.AppealEmotionally, DebateMove.DefendPolicy, DebateMove.PresentStatistics }; break;
                default: plan = new[] { DebateMove.AttackOpponent, DebateMove.Counterattack, DebateMove.ChangeSubject }; break;
            }

            return new DebatePreparation(DebatePreparationHours, new[] { topic }, plan);
        }

        /// <summary>A party's ground: its most salient contested issue weighted by its own match (the world's truth - the run is the world).</summary>
        private static IssueId TopIssue(Setup setup, int party)
        {
            int best = -1;
            for (int i = 0; i < setup.TrueSalience.Length; i++)
            {
                if (double.IsNaN(setup.TrueSalience[i]) || double.IsNaN(setup.Parties[party].TrueIssueMatch[i])) { continue; }
                if (best < 0 || setup.TrueSalience[i] * setup.Parties[party].TrueIssueMatch[i] > setup.TrueSalience[best] * setup.Parties[party].TrueIssueMatch[best]) { best = i; }
            }

            return best < 0 ? IssueId.Economy : (IssueId)best;
        }

        /// <summary>§15's issue ownership: the party's true issue-match on the topic (0 where it takes no position).</summary>
        private static double OwnershipOf(Setup setup, int party, IssueId issue)
        {
            double m = setup.Parties[party].TrueIssueMatch[(int)issue];
            return double.IsNaN(m) ? 0.0 : m;
        }

        /// <summary>Whether an issue is the electorate's most salient (the populist's "prioritised" test for a one-group electorate).</summary>
        private static bool IsTopSalience(Setup setup, IssueId issue)
        {
            double top = double.NegativeInfinity;
            int topIndex = -1;
            for (int i = 0; i < setup.TrueSalience.Length; i++)
            {
                if (double.IsNaN(setup.TrueSalience[i])) { continue; }
                if (setup.TrueSalience[i] > top) { top = setup.TrueSalience[i]; topIndex = i; }
            }

            return topIndex == (int)issue;
        }

        // ---------- the truth ----------

        /// <summary>W-D4: each party's accumulated persuasion at the close, read off the pressure the campaign actually applied.</summary>
        private static double[] FinalPersuasion(CampaignPressure pressure, int partyCount)
        {
            var p = new double[partyCount];
            for (int i = 0; i < partyCount; i++) { p[i] = pressure.Persuasion(i); }
            return p;
        }

        private static double[] CurrentPreference(Setup setup, double[] prior, CampaignPressure pressure)
        {
            double[] bonus = pressure.ToCompatibilityBonus();
            var compatibility = new double[setup.Compatibility.Length];
            for (int i = 0; i < compatibility.Length; i++) { compatibility[i] = setup.Compatibility[i] + bonus[i]; }
            return PreferenceModel.Preference(compatibility, prior, setup.LoyaltyPerParty);
        }

        /// <summary>The TRUE salience and match behind a message: one issue's, or the mean over the contested issues for a general message.</summary>
        private static void TrueMessage(Setup setup, int party, IssueId? issue, out double salience, out double match)
        {
            double[] matchRow = setup.Parties[party].TrueIssueMatch;
            if (issue.HasValue)
            {
                salience = setup.TrueSalience[(int)issue.Value];
                match = matchRow[(int)issue.Value];
                if (double.IsNaN(salience) || double.IsNaN(match)) { salience = 0.0; match = 0.0; }
                return;
            }

            double s = 0.0, m = 0.0;
            int n = 0;
            for (int i = 0; i < setup.TrueSalience.Length; i++)
            {
                if (double.IsNaN(setup.TrueSalience[i]) || double.IsNaN(matchRow[i])) { continue; }
                s += setup.TrueSalience[i]; m += matchRow[i]; n++;
            }

            salience = n > 0 ? s / n : 0.0;
            match = n > 0 ? m / n : 0.0;
        }

        private static double[] Normalised(double[] shares)
        {
            double sum = 0.0;
            foreach (double s in shares) { sum += s; }
            if (sum <= 0.0) { throw new ArgumentException("prior shares sum to zero"); }
            var result = new double[shares.Length];
            for (int i = 0; i < result.Length; i++) { result[i] = shares[i] / sum; }
            return result;
        }

        private static string IssueSuffix(IssueId? issue) => issue.HasValue ? " / " + issue.Value : " / general";

        private static void Append(StringBuilder digest, int day, int party, CampaignActionKind kind, string target, double spend)
        {
            digest.Append(day).Append(':').Append(party).Append(':').Append((int)kind).Append(':')
                .Append(target).Append(':').Append(spend.ToString("F2", CultureInfo.InvariantCulture)).Append(';');
        }

        /// <summary>FNV-1a over the decision text, hex — small, stable, and independent of the platform's string hash.</summary>
        private static string Fnv1a64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
