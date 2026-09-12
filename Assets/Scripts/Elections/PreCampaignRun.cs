using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PoliSim.Elections
{
    /// <summary>
    /// CL-1 (2026-09-12, the backlog plan's S-C1) — **the pre-campaign, stepped one day at a time for the
    /// PLAYER's party alone.** The calendar has carried a 26-week pre-campaign and §3's eight preparation
    /// verbs since W-B1, and the HQ has drawn the phase since W-E1 - as a FILM. Nothing ever ran it:
    /// `SimulationManager.AdvanceCampaign` returned before `CampaignCalendar.CampaignStart`, so the played
    /// game had a 56-day campaign and no run-up, and three of the twenty play-calibration constants
    /// (the window's length, the offices, the staff) could never be judged.
    ///
    /// <para><b>Why this is not `CampaignRun` with a longer calendar.</b> The run's day is every party's
    /// day: the AI parties poll (`CampaignAi.WantsPoll` is legal in the pre-campaign), pay their offices and
    /// staff and draw from the three campaign streams. Stepping them through 182 more days would move
    /// every digest the harnesses hold and re-stage every party's day 0 by accident. **The AI parties'
    /// pre-campaign is the staging**: `LiveCampaignSetup` already states each cast personality's offices,
    /// staff and television plan as what it brings to the campaign's first day - that IS its
    /// preparation, authored once. So the pre-campaign runs for the player's party only, with no RNG but
    /// its own stream (`SimulationRandom.Stream.PreCampaign`, for the polls it buys), and its OUTCOME is
    /// applied to the player's `PartySetup` when the campaign opens. ⚠ Asserted, not assumed
    /// (`CampaignClockHarness` 7a): a pre-campaign in which nothing is queued leaves the Setup at
    /// `CampaignStart` byte-identical to the staging, so every campaign digest holds.</para>
    ///
    /// <para><b>Priced from the constants that exist, and nothing else</b> (the plan's own words). Four of
    /// the eight verbs have a price in the model and are stepped: an OFFICE at `CampaignOffices.OpenCost`
    /// (planned here, opened and paid on the campaign's day 0 by `CampaignRun.Begin` under the same
    /// affordability rule every party's plan faces - D-1 (c)); a HIRE at `CampaignStaff.SalaryPerDay`, paid
    /// every pre-campaign day from the day after hiring (hire early, pay longer - the trade-off the verb
    /// is for); a POLL at the internal house's cost, conducted on the prior (the electorate has not moved
    /// before the campaign - the poll differs from the 2022 result by its sampling error and nothing else);
    /// a television BUY prepared into the manager's plan (the plan sets the money aside from the campaign's
    /// own releases, as W-B12 built it - no manager, no plan, refused). The other four are drawn and
    /// REFUSED with their reason rather than priced with an invented figure: FUNDRAISE has no donor model
    /// (C-D2's bill stands), DEVELOP POLICY and TRAIN CANDIDATE have no price and no seam in the run, SET
    /// STRATEGY has no seam - a scripted party's strategy is its personality's. Refusing on the screen is
    /// the honest form; a chip that quietly did nothing would be the lie.</para>
    ///
    /// <para><b>Replay.</b> Deterministic under the record's queue and its one stream, so a save inside
    /// the pre-campaign restores by re-stepping (`SimulationManager.RestoreCampaign`), the same idiom the
    /// campaign uses. The digest here is the proof: two replays of one queue print the same one.</para>
    /// </summary>
    public static class PreCampaignRun
    {
        /// <summary>The verbs the pre-campaign steps at a price the model already carries.</summary>
        public static readonly CampaignActionKind[] Priced =
        {
            CampaignActionKind.EstablishOffice, CampaignActionKind.RecruitStaff, CampaignActionKind.CommissionPolling, CampaignActionKind.PrepareAdvertising,
        };

        /// <summary>The verbs the pre-campaign REFUSES, each with the standing reason it prints - null for a priced verb.</summary>
        public static string Refusal(CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.Fundraise: return "no donor model exists to raise from";
                case CampaignActionKind.DevelopPolicy: return "no price and no seam in the run";
                case CampaignActionKind.TrainCandidate: return "no price in the model - a figure would be invented";
                case CampaignActionKind.SetStrategy: return "no seam in the run - a played party's strategy is its personality's";
                default: return Array.IndexOf(Priced, kind) >= 0 ? null : $"{kind} is not a preparation verb";
            }
        }

        /// <summary>One queued preparation decision: the verb, the region an office goes to (−1 otherwise), the role a hire fills (−1 otherwise).</summary>
        public readonly struct Decision
        {
            public readonly CampaignActionKind Kind;
            public readonly int RegionIndex;
            public readonly int Role;
            public Decision(CampaignActionKind kind, int regionIndex = -1, int role = -1) { Kind = kind; RegionIndex = regionIndex; Role = role; }
        }

        /// <summary>What one decision came to on its day: done at a cost, or refused with the reason.</summary>
        public sealed class Entry
        {
            public int Day;
            public CampaignActionKind Kind;
            public string Target;
            public double Cost;
            /// <summary>Null when the decision was taken; otherwise why it was not.</summary>
            public string Refusal;
        }

        public sealed class State
        {
            public CampaignCalendar Calendar;
            public int Party;
            public string PartyName;
            /// <summary>The staging the pre-campaign started from - the campaign's own Setup, read for the party, the regions, the prior and the polling house.</summary>
            public CampaignRun.Setup Staged;
            public System.Random PollRandom;
            public double Money;
            public double MoneyAtStart;
            public int Volunteers;
            /// <summary>Regions an office is PLANNED in, beyond the staging's - opened on the campaign's day 0.</summary>
            public List<int> PlannedOffices = new List<int>();
            /// <summary>Roles hired here, with the day - on the campaign's payroll from its day 0, on this one's from the day after the hire.</summary>
            public List<(StaffRole Role, int Day)> Hired = new List<(StaffRole Role, int Day)>();
            public int UnpaidStaffDays;
            public int TelevisionBuysPrepared;
            public Poll? LatestPoll;
            public int LastPollDay = int.MinValue;
            public int PollsBought;
            public List<Entry> Log = new List<Entry>();
            public StringBuilder Digest = new StringBuilder();
            public int Day, TotalDays;

            public bool Finished => Day >= TotalDays;
            /// <summary>The calendar date of the day <see cref="StepDay"/> will step next.</summary>
            public DateTime Today => Calendar.PreCampaignStart.AddDays(Day);

            public bool HasRole(StaffRole role)
            {
                foreach (StaffRole r in Staged.Parties[Party].Staff) { if (r == role) { return true; } }
                foreach ((StaffRole Role, int Day) h in Hired) { if (h.Role == role) { return true; } }
                return false;
            }

            public bool HasOffice(int region)
            {
                foreach (int r in Staged.Parties[Party].Offices) { if (r == region) { return true; } }
                return PlannedOffices.Contains(region);
            }
        }

        /// <summary>The pre-campaign's outcome as the campaign's Setup takes it: the chest, the ground, the offices and staff the party brings to day 0, the plan's buys.</summary>
        public readonly struct Outcome
        {
            public readonly double Money;
            public readonly int Volunteers;
            public readonly int[] Offices;
            public readonly StaffRole[] Staff;
            public readonly int TelevisionBuys;
            public readonly string Digest;
            public Outcome(double money, int volunteers, int[] offices, StaffRole[] staff, int televisionBuys, string digest)
            {
                Money = money; Volunteers = volunteers; Offices = offices; Staff = staff; TelevisionBuys = televisionBuys; Digest = digest;
            }
        }

        /// <summary>Day 0 of the pre-campaign for one party of the staged campaign: the staging's chest and ground, nothing planned, nothing hired.</summary>
        public static State Begin(CampaignRun.Setup staged, int party, System.Random pollRandom)
        {
            if (party < 0 || party >= staged.Parties.Length) { throw new ArgumentOutOfRangeException(nameof(party)); }
            if (pollRandom == null) { throw new ArgumentNullException(nameof(pollRandom)); }
            CampaignRun.PartySetup p = staged.Parties[party];
            return new State
            {
                Calendar = staged.Calendar, Party = party, PartyName = p.Name, Staged = staged, PollRandom = pollRandom,
                Money = p.StartingMoney, MoneyAtStart = p.StartingMoney, Volunteers = p.Volunteers,
                Day = 0, TotalDays = 7 * staged.Calendar.PreCampaignWeeks,
            };
        }

        /// <summary>The price a verb's chip shows today: an office's opening cost, a day's salary, the internal poll, a television buy at the ad's price; 0 for a refused verb.</summary>
        public static double Price(State s, CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.EstablishOffice: return CampaignOffices.OpenCost;
                case CampaignActionKind.RecruitStaff: return CampaignStaff.SalaryPerDay;
                case CampaignActionKind.CommissionPolling: return s.Staged.InternalHouse.Cost;
                case CampaignActionKind.PrepareAdvertising: return CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
                default: return 0.0;
            }
        }

        /// <summary>The largest electorate the party has no office in, staged or planned - the one-region rule until the picker (the plan's S-C2); −1 when every region has one.</summary>
        public static int NextOfficeRegion(State s)
        {
            int best = -1; double bestAudience = -1.0;
            for (int r = 0; r < s.Staged.Regions.Length; r++)
            {
                if (s.HasOffice(r)) { continue; }
                if (s.Staged.Regions[r].Audience > bestAudience) { bestAudience = s.Staged.Regions[r].Audience; best = r; }
            }
            return best;
        }

        /// <summary>
        /// One pre-campaign day: the hires' payroll first (an unpaid day is counted, never forgiven), then the
        /// day's decisions in order, each done at its price or refused with its reason - and every one of
        /// them into the digest.
        /// </summary>
        public static void StepDay(State s, IReadOnlyList<Decision> decisions)
        {
            if (s == null) { throw new ArgumentNullException(nameof(s)); }
            if (s.Finished) { return; }
            int day = s.Day;
            foreach ((StaffRole Role, int Day) h in s.Hired)
            {
                if (h.Day >= day) { continue; }   // hired today: on the payroll from tomorrow
                if (s.Money >= CampaignStaff.SalaryPerDay) { s.Money -= CampaignStaff.SalaryPerDay; }
                else { s.UnpaidStaffDays++; }
            }

            if (decisions != null)
            {
                foreach (Decision d in decisions) { Apply(s, day, d); }
            }

            s.Digest.Append(string.Format(CultureInfo.InvariantCulture, "d{0}:{1:F0};", day, s.Money));
            s.Day = day + 1;
        }

        private static void Apply(State s, int day, Decision d)
        {
            var entry = new Entry { Day = day, Kind = d.Kind, Target = "national", Cost = 0.0 };
            string standing = Refusal(d.Kind);
            if (standing != null) { entry.Refusal = standing; }
            else
            {
                switch (d.Kind)
                {
                    case CampaignActionKind.EstablishOffice:
                    {
                        int region = d.RegionIndex >= 0 ? d.RegionIndex : NextOfficeRegion(s);
                        if (region < 0 || region >= s.Staged.Regions.Length) { entry.Refusal = "no region without an office is left"; break; }
                        entry.Target = s.Staged.Regions[region].Name;
                        entry.Cost = CampaignOffices.OpenCost;
                        if (s.HasOffice(region)) { entry.Refusal = "an office is already planned there"; break; }
                        // The opening is paid on the campaign's day 0 by Begin; the plan must at least be affordable to open now.
                        if (s.Money < (s.PlannedOffices.Count + 1) * CampaignOffices.OpenCost) { entry.Refusal = "the chest would not open every office planned"; break; }
                        s.PlannedOffices.Add(region);
                        break;
                    }
                    case CampaignActionKind.RecruitStaff:
                    {
                        if (d.Role < 0 || d.Role >= CampaignStaff.TheFive.Length) { entry.Refusal = "no role named"; break; }
                        StaffRole role = (StaffRole)d.Role;
                        entry.Target = RoleName(role);
                        entry.Cost = CampaignStaff.SalaryPerDay;
                        if (s.HasRole(role)) { entry.Refusal = "already on the roster"; break; }
                        if (s.Money < CampaignStaff.SalaryPerDay) { entry.Refusal = "the chest cannot pay a day's salary"; break; }
                        s.Hired.Add((role, day));
                        break;
                    }
                    case CampaignActionKind.CommissionPolling:
                    {
                        PollingHouse house = s.Staged.InternalHouse;
                        entry.Target = house.Name;
                        entry.Cost = house.Cost;
                        if (s.LastPollDay == day) { entry.Refusal = "one poll a day"; break; }
                        if (s.Money < house.Cost) { entry.Refusal = "the chest cannot pay the house"; break; }
                        s.Money -= house.Cost;
                        s.LatestPoll = PollingSystem.Conduct(s.Staged.PriorShares, house, s.Today, s.PollRandom);
                        s.LastPollDay = day;
                        s.PollsBought++;
                        break;
                    }
                    case CampaignActionKind.PrepareAdvertising:
                    {
                        entry.Target = "television";
                        entry.Cost = CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
                        if (!s.HasRole(StaffRole.CampaignManager)) { entry.Refusal = "no campaign manager - the plan is the manager's"; break; }
                        s.TelevisionBuysPrepared++;
                        break;
                    }
                    default:
                        entry.Refusal = $"{d.Kind} is not a preparation verb";
                        break;
                }
            }

            s.Log.Add(entry);
            s.Digest.Append(string.Format(CultureInfo.InvariantCulture, "d{0} {1} {2} {3:F0}{4};", day, entry.Kind, entry.Target, entry.Cost,
                entry.Refusal == null ? "" : " REFUSED " + entry.Refusal));
        }

        /// <summary>The outcome the campaign's Setup takes for the party - the staging's offices and staff with the pre-campaign's added, the chest as the pre-campaign left it.</summary>
        public static Outcome Finish(State s)
        {
            if (s == null) { throw new ArgumentNullException(nameof(s)); }
            CampaignRun.PartySetup p = s.Staged.Parties[s.Party];
            var offices = new List<int>(p.Offices);
            foreach (int r in s.PlannedOffices) { if (!offices.Contains(r)) { offices.Add(r); } }
            var staff = new List<StaffRole>(p.Staff);
            foreach ((StaffRole Role, int Day) h in s.Hired) { if (!staff.Contains(h.Role)) { staff.Add(h.Role); } }
            return new Outcome(s.Money, s.Volunteers, offices.ToArray(), staff.ToArray(), p.TelevisionBuys + s.TelevisionBuysPrepared, s.Digest.ToString());
        }

        /// <summary>A role's name as the screen prints it - "Media advisor", not the enum's spelling.</summary>
        public static string RoleName(StaffRole role)
        {
            string name = role.ToString();
            var sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) { sb.Append(' '); }
                sb.Append(i == 0 ? name[i] : char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }

        /// <summary>The entries of one day, in order - what the HQ prints as yesterday's result.</summary>
        public static List<Entry> EntriesOn(State s, int day)
        {
            var list = new List<Entry>();
            foreach (Entry e in s.Log) { if (e.Day == day) { list.Add(e); } }
            return list;
        }
    }
}
