using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B1 / SPEC §3 — the campaign calendar, laid on the game's EXISTING day clock. PURE
    /// FUNCTIONS, WIRED TO NOTHING (R-N2): this type computes what phase a date falls in and what
    /// is legal there; it never advances anything and the turn loop is untouched.
    ///
    /// The spec's three phases plus the two states either side of them, because a prototype needs
    /// to answer "what now?" before and after as well as during:
    /// <code>
    /// Dormant -> PreCampaign -> Campaign -> ElectionDay -> Concluded
    /// </code>
    ///
    /// **Sweden's real window is the prototype default** (0.1). Swedish general elections fall on
    /// the second Sunday in September — 13 September 2026 — and the worklist sets the campaign
    /// proper at the final **8 weeks**. The pre-campaign window before it is where §3's preparation
    /// verbs live (recruit, fundraise, open offices, poll, choose a strategy); the default of **26
    /// weeks** is an [AUTHORED-DRAFT] figure chosen so the player has a meaningful build-up without
    /// the game becoming a spreadsheet for half a year — strikeable, and calibrated by play.
    ///
    /// **Phases gate legality, and that is the point** (§3 lists different verbs per phase). A
    /// rally in the pre-campaign is not a small bonus, it is not a thing; door-knocking on election
    /// day is GOTV and nothing else. `IsLegal` is the single place that answers it, so no action
    /// can quietly work outside its window.
    /// </summary>
    public enum CampaignPhase
    {
        /// <summary>No campaign in view — ordinary governing time.</summary>
        Dormant = 0,
        PreCampaign = 1,
        Campaign = 2,
        ElectionDay = 3,
        Concluded = 4,
    }

    /// <summary>Spec §3 and §12's verbs, as one enum so legality is answerable in one place.</summary>
    public enum CampaignActionKind
    {
        // §3 pre-campaign
        RecruitStaff = 0,
        Fundraise = 1,
        EstablishOffice = 2,
        CommissionPolling = 3,
        SetStrategy = 4,
        DevelopPolicy = 5,
        TrainCandidate = 6,
        PrepareAdvertising = 7,

        // §12 campaign
        Rally = 20,
        TownHall = 21,
        DoorToDoor = 22,
        TelevisionAd = 23,
        DigitalAd = 24,
        SocialPost = 25,
        Interview = 26,
        PolicyAnnouncement = 27,

        // §26 election-day operations
        GetOutTheVote = 40,
    }

    /// <summary>
    /// One campaign's dates. Immutable; every question about "when" is answered from the election
    /// date and two window lengths, so a different country or a snap election is data, not code.
    /// </summary>
    public readonly struct CampaignCalendar
    {
        /// <summary>[AUTHORED-DRAFT] the prototype default: the campaign proper is the final 8 weeks (the worklist's figure for Sweden).</summary>
        public const int DefaultCampaignWeeks = 8;

        /// <summary>[AUTHORED-DRAFT] a 26-week run-up for §3's preparation verbs — long enough to matter, short enough not to bore. Strikeable.</summary>
        public const int DefaultPreCampaignWeeks = 26;

        public readonly DateTime ElectionDate;
        public readonly int CampaignWeeks;
        public readonly int PreCampaignWeeks;

        public CampaignCalendar(DateTime electionDate,
            int campaignWeeks = DefaultCampaignWeeks, int preCampaignWeeks = DefaultPreCampaignWeeks)
        {
            if (campaignWeeks < 0 || preCampaignWeeks < 0)
            {
                throw new ArgumentException("campaign windows cannot be negative");
            }

            ElectionDate = electionDate.Date;
            CampaignWeeks = campaignWeeks;
            PreCampaignWeeks = preCampaignWeeks;
        }

        /// <summary>Sweden's real 2026 general election — the second Sunday in September.</summary>
        public static CampaignCalendar Sweden2026 => new CampaignCalendar(new DateTime(2026, 9, 13));

        public DateTime CampaignStart => ElectionDate.AddDays(-7 * CampaignWeeks);

        public DateTime PreCampaignStart => CampaignStart.AddDays(-7 * PreCampaignWeeks);

        /// <summary>Which phase a date falls in. Boundaries are inclusive at the start of each window, so the campaign's first day is a campaign day.</summary>
        public CampaignPhase PhaseOn(DateTime date)
        {
            DateTime d = date.Date;
            if (d > ElectionDate) { return CampaignPhase.Concluded; }
            if (d == ElectionDate) { return CampaignPhase.ElectionDay; }
            if (d >= CampaignStart) { return CampaignPhase.Campaign; }
            if (d >= PreCampaignStart) { return CampaignPhase.PreCampaign; }
            return CampaignPhase.Dormant;
        }

        /// <summary>Days from <paramref name="date"/> to polling day; negative once it has passed.</summary>
        public int DaysUntilElection(DateTime date) => (int)(ElectionDate - date.Date).TotalDays;

        /// <summary>Campaign days elapsed, 0 before the campaign opens — what a UI counts up and a resource budget counts down.</summary>
        public int CampaignDaysElapsed(DateTime date)
        {
            DateTime d = date.Date;
            if (d < CampaignStart) { return 0; }
            DateTime capped = d > ElectionDate ? ElectionDate : d;
            return (int)(capped - CampaignStart).TotalDays;
        }

        public int TotalCampaignDays => 7 * CampaignWeeks;
    }

    /// <summary>What may be done when. The single authority on §3's phase gating.</summary>
    public static class CampaignLegality
    {
        public static bool IsLegal(CampaignActionKind action, CampaignPhase phase)
        {
            switch (phase)
            {
                case CampaignPhase.PreCampaign:
                    // Preparation only. Notably a rally is not a "weaker" pre-campaign action - it
                    // is not available at all, which is what makes the pre-campaign a different
                    // game rather than a slower version of the same one.
                    return action <= CampaignActionKind.PrepareAdvertising;

                case CampaignPhase.Campaign:
                    // §12's eight, plus the preparation verbs that plainly continue (fundraising,
                    // hiring, polling, opening an office, changing strategy mid-campaign - §11 says
                    // strategy may change during). Training and ad-preparation stay pre-campaign:
                    // by the campaign they are what you already have.
                    if (action == CampaignActionKind.TrainCandidate) { return false; }
                    if (action == CampaignActionKind.PrepareAdvertising) { return false; }
                    if (action == CampaignActionKind.DevelopPolicy) { return false; }
                    return action != CampaignActionKind.GetOutTheVote;

                case CampaignPhase.ElectionDay:
                    // Only the ground game (§26). Persuasion is over; turnout is not.
                    return action == CampaignActionKind.GetOutTheVote
                           || action == CampaignActionKind.DoorToDoor;

                case CampaignPhase.Dormant:
                case CampaignPhase.Concluded:
                default:
                    return false;
            }
        }

        /// <summary>Every action legal in a phase — what a UI would offer, derived rather than listed twice.</summary>
        public static CampaignActionKind[] LegalActions(CampaignPhase phase)
        {
            var all = (CampaignActionKind[])Enum.GetValues(typeof(CampaignActionKind));
            int count = 0;
            foreach (CampaignActionKind a in all) { if (IsLegal(a, phase)) { count++; } }

            var result = new CampaignActionKind[count];
            int cursor = 0;
            foreach (CampaignActionKind a in all) { if (IsLegal(a, phase)) { result[cursor++] = a; } }
            return result;
        }
    }
}
