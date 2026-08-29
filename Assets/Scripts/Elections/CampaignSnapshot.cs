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

        public CampaignSnapshot(string partyName, string markKey, string countryName, CampaignPhase phase,
            DateTime today, CampaignCalendar calendar, ResourcePool resources, double moneyAtCampaignStart,
            Poll latestPoll, string[] partyNames, int playerPartyIndex, double[] momentumPp,
            QueuedAction[] queue, StaffMember[] staff, RegionalOffice[] offices, double perceivedEconomyIndex)
        {
            PartyName = partyName; MarkKey = markKey; CountryName = countryName; Phase = phase;
            Today = today; Calendar = calendar; Resources = resources;
            MoneyAtCampaignStart = moneyAtCampaignStart; LatestPoll = latestPoll;
            PartyNames = partyNames; PlayerPartyIndex = playerPartyIndex; MomentumPp = momentumPp;
            Queue = queue; Staff = staff; Offices = offices;
            PerceivedEconomyIndex = perceivedEconomyIndex;
        }

        public int DaysUntilElection => Calendar.DaysUntilElection(Today);

        public int CampaignDay => Calendar.CampaignDaysElapsed(Today);

        public double MoneySpentShare => MoneyAtCampaignStart > 0
            ? 1.0 - Resources.Money / MoneyAtCampaignStart
            : 0.0;
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

        public StaffMember(string role, string name, string bonusLabel)
        {
            Role = role; Name = name; BonusLabel = bonusLabel;
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
}
