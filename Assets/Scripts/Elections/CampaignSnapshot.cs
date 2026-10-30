using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E1 — everything a campaign screen draws, assembled from the model and handed to the view.
    /// PURE DATA (R-N2): the screens read this, never the simulation, and nothing in a gameplay
    /// path builds one — the screenshot harness does, exactly as `_instrumentLadder` works.
    ///
    /// **Every field is DERIVED.** The screen's job is to lay out numbers this type already holds;
    /// it computes nothing of its own, so "is that figure real?" is answerable by looking at who
    /// filled the snapshot rather than by reading the drawing code.
    ///
    /// **The poll is a <see cref="Poll"/>, not a preference vector** — the W-B10 rule that the UI
    /// never sees the truth is carried into the view layer by the type it is given.
    /// </summary>
    public readonly struct CampaignSnapshot
    {
        public readonly string PartyName;
        /// <summary>The party mark's FULL file stem (e.g. "mark_party_se_s"), or null to draw none — the same key IconLibrary.GetPartyMark and PartyMarkCoverageCheck use.</summary>
        public readonly string MarkKey;
        public readonly string CountryName;

        public readonly CampaignPhase Phase;
        public readonly DateTime Today;
        public readonly CampaignCalendar Calendar;

        public readonly ResourcePool Resources;
        public readonly double MoneyAtCampaignStart;

        /// <summary>The most recent published poll — the ONLY view the screen has of where the race stands.</summary>
        public readonly Poll LatestPoll;
        public readonly string[] PartyNames;
        /// <summary>This party's index inside <see cref="PartyNames"/> and the poll.</summary>
        public readonly int PlayerPartyIndex;
        public readonly double[] MomentumPp;

        public readonly QueuedAction[] Queue;
        public readonly StaffMember[] Staff;
        public readonly RegionalOffice[] Offices;

        /// <summary>§19's perceived economy index (0–100) — what the electorate reacts to, not the truth.</summary>
        public readonly double PerceivedEconomyIndex;
        /// <summary>CL-1: what the run-up's last stepped day came to - each decision done at its price or refused with its reason. CL-2: on a campaign day, the campaign's PUBLIC events of the day just stepped - a debate's result, a story that broke and how its party answered - every party's, as the press carried them.</summary>
        public readonly string[] Notes;
        /// <summary>CL-2: the campaign days (0-based) the calendar's debates fall on, and the next of them from the day shown (−1 when the debates are past). Who stands is the run's rule: the two leading the published poll that day.</summary>
        public readonly int[] DebateDays;
        public readonly int NextDebateDay;
        /// <summary>CL-2: the story that broke for the party and waits for its answer, or null.</summary>
        public readonly PendingScandalView? Scandal;
        /// <summary>CL-2: where a queued local act goes - the region picked on the map, else the party's strongest office's, else the largest electorate - named with which it is; null in the run-up.</summary>
        public readonly string LocalActsRegion;
        /// <summary>K67 (2026-10-23): a line the race plate draws beside the player's own poll figure - in the run-up, a television buy's estimated effect on the party's standing while it airs; null for none.</summary>
        public readonly string PlayerPollNote;
        /// <summary>S4 (Design's return of 2026-10-30, the HQ re-lay): what the re-laid HQ reads beyond the fields above - the contest, who is on the ballot,
        /// where the race's reading comes from, the ground (the regions as the party reads them) and yesterday's acts. Null for a staged film's snapshot,
        /// which the HQ draws with every region unread and every party with a share standing.</summary>
        public readonly CampaignHqReading Hq;

        public CampaignSnapshot(string partyName, string markKey, string countryName, CampaignPhase phase,
            DateTime today, CampaignCalendar calendar, ResourcePool resources, double moneyAtCampaignStart,
            Poll latestPoll, string[] partyNames, int playerPartyIndex, double[] momentumPp,
            QueuedAction[] queue, StaffMember[] staff, RegionalOffice[] offices, double perceivedEconomyIndex, string[] notes = null,
            int[] debateDays = null, int nextDebateDay = -1, PendingScandalView? scandal = null, string localActsRegion = null, string playerPollNote = null,
            CampaignHqReading hq = null)
        {
            PartyName = partyName; MarkKey = markKey; CountryName = countryName; Phase = phase;
            Today = today; Calendar = calendar; Resources = resources;
            MoneyAtCampaignStart = moneyAtCampaignStart; LatestPoll = latestPoll;
            PartyNames = partyNames; PlayerPartyIndex = playerPartyIndex; MomentumPp = momentumPp;
            Queue = queue; Staff = staff; Offices = offices;
            PerceivedEconomyIndex = perceivedEconomyIndex;
            Notes = notes ?? new string[0];
            DebateDays = debateDays ?? new int[0]; NextDebateDay = nextDebateDay; Scandal = scandal; LocalActsRegion = localActsRegion;
            PlayerPollNote = playerPollNote;
            Hq = hq;
        }

        /// <summary>
        /// The days from <see cref="Today"/> to polling day. ⚠ S4: <see cref="Today"/> is the day the HQ's queue steps on - for a player-run party the run's
        /// NEXT day (`CampaignRun.State.Today`), which after the day's tick is the world's tomorrow - so this is the QUEUE's count, the one that gates what may
        /// be queued (a phone bank is legal by the day it steps on, as the run refuses it). The days the HQ PRINTS are counted from the world's date, the
        /// strip's (Design's return, bug 4: the run-off film's strip read "in 12 days" while its HQ read 11).
        /// </summary>
        public int DaysUntilElection => Calendar.DaysUntilElection(Today);

        public int CampaignDay => Calendar.CampaignDaysElapsed(Today);

        public double MoneySpentShare => MoneyAtCampaignStart > 0
            ? 1.0 - Resources.Money / MoneyAtCampaignStart
            : 0.0;
    }

    /// <summary>
    /// CL-2 (DS-10): a story that broke for the party and waits for its answer, as the HQ shows it - what, how bad, the
    /// evidence AS THE PARTY'S PEOPLE READ IT (§36's estimate, never the truth), the day it broke, the answer queued for the
    /// morning (or null), and whether anyone is on the roster to sacrifice.
    /// </summary>
    public readonly struct PendingScandalView
    {
        public readonly ScandalKind Kind;
        public readonly ScandalSeverity Severity;
        public readonly double SeenEvidence;
        public readonly int BrokeOnDay;
        public readonly DateTime BrokeOn;
        public readonly ScandalResponse? Queued;
        public readonly bool StaffOnRoster;

        public PendingScandalView(ScandalKind kind, ScandalSeverity severity, double seenEvidence, int brokeOnDay, DateTime brokeOn, ScandalResponse? queued, bool staffOnRoster)
        {
            Kind = kind; Severity = severity; SeenEvidence = seenEvidence; BrokeOnDay = brokeOnDay; BrokeOn = brokeOn; Queued = queued; StaffOnRoster = staffOnRoster;
        }
    }

    /// <summary>One action sitting in the day's queue: what, where, and what it costs.</summary>
    public readonly struct QueuedAction
    {
        public readonly CampaignActionKind Kind;
        public readonly string TargetLabel;
        public readonly double MoneyCost;
        public readonly double Hours;

        public QueuedAction(CampaignActionKind kind, string targetLabel, double moneyCost, double hours)
        {
            Kind = kind; TargetLabel = targetLabel; MoneyCost = moneyCost; Hours = hours;
        }
    }

    /// <summary>§9's staff. [AUTHORED-DRAFT] bonuses; §37 progression is deferred and recorded as such.</summary>
    public readonly struct StaffMember
    {
        public readonly string Role;
        public readonly string Name;
        public readonly string BonusLabel;
        /// <summary>W-B5: the salary a day (the payroll line on the ledger); 0 for a vacant post.</summary>
        public readonly double SalaryPerDay;

        public StaffMember(string role, string name, string bonusLabel, double salaryPerDay = 0.0)
        {
            Role = role; Name = name; BonusLabel = bonusLabel; SalaryPerDay = salaryPerDay;
        }
    }

    /// <summary>§10's regional office.</summary>
    public readonly struct RegionalOffice
    {
        public readonly string RegionName;
        public readonly int Volunteers;
        public readonly double UpkeepPerDay;

        public RegionalOffice(string regionName, int volunteers, double upkeepPerDay)
        {
            RegionName = regionName; Volunteers = volunteers; UpkeepPerDay = upkeepPerDay;
        }
    }

    /// <summary>S4: where the race's reading comes from - the race head's stamp (Design's return, item 2).</summary>
    public enum CampaignPollSource
    {
        /// <summary>The last count, carried as the reading before any poll is bought - no sampling error, so no band (the run-up's prior).</summary>
        LastCount = 0,
        /// <summary>The published tracker every party sees.</summary>
        PublicTracker = 1,
        /// <summary>The party's own commissioned poll, which replaces the tracker's figures.</summary>
        OwnPoll = 2,
    }

    /// <summary>S4: a region's office mark on the HQ's map - open (the campaign's), staged (the staging's, opening on the campaign's first day) or planned (queued in the run-up).</summary>
    public enum OfficeMark
    {
        None = 0,
        Open = 1,
        Staged = 2,
        Planned = 3,
    }

    /// <summary>S4 (K83's intelligence, presented as information): one office's field report - its region, its volunteers, and the doors opened and calls
    /// completed there by the party so far (its own books, `RegionalMobilization`).</summary>
    public readonly struct FieldReport
    {
        public readonly int Region;
        public readonly int Volunteers;
        public readonly double Doors;
        public readonly double Calls;

        public FieldReport(int region, int volunteers, double doors, double calls)
        {
            Region = region; Volunteers = volunteers; Doors = doors; Calls = calls;
        }
    }

    /// <summary>
    /// S4 (Design's return of 2026-10-30, "For Code" 1-2): THE GROUND - the staging's regions as the party reads them, the HQ's middle column. Every region is
    /// UNKNOWN unless the party bought a poll of it (§36: absence, never a guess); its office mark; where the next local act goes (or, in the run-up, where the
    /// next office opens); whether a regional poll is on sale and at what sample and price; and the field reports. PURE DATA, read off the run by
    /// <see cref="LiveCampaignSnapshot"/>; nothing here is computed for the view that the run does not already hold.
    /// </summary>
    public sealed class CampaignGround
    {
        /// <summary>The staging's region names (its keys - "okreg 3", "Stockholms län"), in the staging's order.</summary>
        public readonly string[] Names;
        /// <summary>Per region: the newest regional poll's reading, or UNKNOWN (`RegionalPolling.Readings`).</summary>
        public readonly MapRegionReading[] Readings;
        /// <summary>Per region: the price that region's newest poll was bought at (purse units), NaN where none was bought.</summary>
        public readonly double[] PollPrice;
        public readonly OfficeMark[] Offices;
        /// <summary>Per region: the volunteers at the party's office there, −1 where it has none (a staged or planned office has none of its own yet: 0).</summary>
        public readonly int[] OfficeVolunteers;
        /// <summary>The upkeep a day of one of the party's offices (purse units) - maintenance and operations.</summary>
        public readonly double OfficeUpkeepPerDay;
        /// <summary>Where the next local act goes (the map's pick, else the strongest office's, else the largest electorate) - or, in the run-up, where the next
        /// office opens (the pick, else the largest electorate without one); −1 for none.</summary>
        public readonly int Target;
        /// <summary>Whether <see cref="Target"/> is the map's pick (else the rule's).</summary>
        public readonly bool TargetPicked;
        /// <summary>True over the run-up: the map sites the next office; false over the campaign: it sets where the next local act goes.</summary>
        public readonly bool Siting;
        /// <summary>Whether a regional poll can be bought in this campaign (the staging carries the regions' offsets - `CampaignRun.Setup.RegionalOffset`).</summary>
        public readonly bool RegionalPollOnSale;
        /// <summary>A regional poll's sample as it would be fielded today (the pollster's boost in) and its price in purse units.</summary>
        public readonly int RegionalSample;
        public readonly double RegionalPrice;
        public readonly FieldReport[] Reports;
        /// <summary>The staging's candidacies (`CampaignRun.Setup.Stands`): [party][region], null = every party everywhere.</summary>
        public readonly bool[][] Stands;
        /// <summary>Per region, its fixed seats where the HQ has a table for them, else null.</summary>
        public readonly int[] Seats;

        public CampaignGround(string[] names, MapRegionReading[] readings, double[] pollPrice, OfficeMark[] offices, int[] officeVolunteers, double officeUpkeepPerDay,
            int target, bool targetPicked, bool siting, bool regionalPollOnSale, int regionalSample, double regionalPrice, FieldReport[] reports, bool[][] stands, int[] seats)
        {
            Names = names; Readings = readings; PollPrice = pollPrice; Offices = offices; OfficeVolunteers = officeVolunteers; OfficeUpkeepPerDay = officeUpkeepPerDay;
            Target = target; TargetPicked = targetPicked; Siting = siting; RegionalPollOnSale = regionalPollOnSale; RegionalSample = regionalSample; RegionalPrice = regionalPrice;
            Reports = reports ?? new FieldReport[0]; Stands = stands; Seats = seats;
        }

        /// <summary>Whether <paramref name="party"/> stands in <paramref name="region"/> - everywhere where no candidacy is staged (`CampaignRun.Setup.StandsIn`'s rule).</summary>
        public bool StandsIn(int party, int region) => Stands == null || party < 0 || party >= Stands.Length || Stands[party] == null || region < 0 || region >= Stands[party].Length || Stands[party][region];
    }

    /// <summary>
    /// S4 (Design's return of 2026-10-30): what the re-laid Campaign HQ reads beyond <see cref="CampaignSnapshot"/>'s original fields - PURE DATA, filled by
    /// <see cref="LiveCampaignSnapshot"/> from the run (null on a staged film's snapshot).
    /// </summary>
    public sealed class CampaignHqReading
    {
        /// <summary>The contest the staging campaigns for - the chamber, a presidential first vote, or its run-off (`CampaignRun.Setup.Contest`).</summary>
        public readonly CampaignContest Contest;
        /// <summary>Per party: whether it is on this contest's ballot - stands in at least one region (bug 2: SLD and PSL, inside NL and TD since 2023, printed
        /// at 0.0; a run-off's cast keeps every roster party and only the two finalists stand).</summary>
        public readonly bool[] OnBallot;
        public readonly CampaignPollSource PollSource;
        public readonly CampaignGround Ground;
        /// <summary>Whether the HQ shows the player's own queue (a player-run party) or the AI's log of the last day.</summary>
        public readonly bool PlayerRun;
        /// <summary>The party's own acts of the last stepped day - their count, their spend (purse units) and each as a line - for the HQ's "Yesterday" row and its slip.</summary>
        public readonly int YesterdayActs;
        public readonly double YesterdaySpend;
        public readonly string[] YesterdayLines;
        /// <summary>K67's run-up television effect on the party's standing while a buy airs, in points with its 95 % band, and the last day it shows; 0 / null for none.
        /// The same figures <see cref="CampaignSnapshot.PlayerPollNote"/> words.</summary>
        public readonly double TelevisionPp;
        public readonly double TelevisionBandPp;
        public readonly DateTime? TelevisionUntil;
        /// <summary>The five posts (`CampaignStaff.TheFive`, in its order): 0 not hired, 1 staged for the campaign's first day, 2 hired (from the run-up, or unpaid today),
        /// 3 on the job (paid today) - the organisation's five cells.</summary>
        public readonly int[] StaffStates;

        public CampaignHqReading(CampaignContest contest, bool[] onBallot, CampaignPollSource pollSource, CampaignGround ground, bool playerRun,
            int yesterdayActs, double yesterdaySpend, string[] yesterdayLines, int[] staffStates, double televisionPp = 0.0, double televisionBandPp = 0.0, DateTime? televisionUntil = null)
        {
            Contest = contest; OnBallot = onBallot; PollSource = pollSource; Ground = ground; PlayerRun = playerRun;
            YesterdayActs = yesterdayActs; YesterdaySpend = yesterdaySpend; YesterdayLines = yesterdayLines ?? new string[0];
            TelevisionPp = televisionPp; TelevisionBandPp = televisionBandPp; TelevisionUntil = televisionUntil;
            StaffStates = staffStates ?? new int[5];
        }

        /// <summary>Whether <paramref name="party"/> is on the ballot; true where the reading carries no ballot (every party stands).</summary>
        public bool Stands(int party) => OnBallot == null || party < 0 || party >= OnBallot.Length || OnBallot[party];
    }
}
