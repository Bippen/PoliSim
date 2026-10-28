using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;

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
    /// <para><b>A-LOOP q24 5a (ruled J1 2026-10-19, Q7: `docs/specs/A_LOOP_PLAN.md` §5's purchase table): each purchase buys a measured
    /// effect or is removed.</b> The TELEVISION BUY is charged at the ad's price the day it is made and airs that day: Gerber et al.'s
    /// weekly effect on the buyer's share shows on the run-up's polls for its week and is gone after it (`CampaignPersuasion.RunUpTelevisionEffectPp`),
    /// and nothing of it reaches the count - it no longer adds a buy to the campaign manager's plan (SD-33: it used to move only the
    /// AI's pace reserve, uncharged). The POLL stays as information: its effect is the ± its sample buys, and it changes nothing else.
    /// The OFFICE was refused until q24 5b counted an office's contacts on polling day's turnout; since 5b (GATE-18 q2) it is taken again, as
    /// CL-1 built it - planned here in the region the decision names (its siting: where its doors go; none named, the largest electorate the
    /// party has no office in), opened and paid on the campaign's day 0 - and its daily doors reach the count at the turnout CACE
    /// (`GotvTurnout`). The five hires stand; three of them
    /// (the pollster, the media advisor, the digital director) have no sourced effect of their own and go to q4's list.</para>
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
            CampaignActionKind.Fundraise,
        };

        /// <summary>
        /// K84 (Elias, 2026-10-23; q4 row 1): SOURCED [BCP12] (Barton, Castillo &amp; Petrie, ICES discussion paper, Nov 2012, Table 4, pp. 19-21;
        /// `docs/reference/raw/aloop/q4/gmu_ices_wp1037_barton_castillo_petrie.pdf`): the revenue per solicitation of a candidate's fundraising
        /// letter to partisans, US local general elections 2010 - $0.401 for the positive letter ($0.305 for the negative; tone no different,
        /// p = 0.59). DECLARED: the positive letter, a party writing to its own supporters about its own case. Its transfer to 2026 and to Europe
        /// is [AUTHORED-DRAFT] (K1, the play-calibration list): the dollar is crossed into the national currency at the game's own ECB 2023 rate
        /// (`EnergyLayer.NationalPerUsd`), unrevalued.
        /// </summary>
        public const double RevenuePerLetterUsd = 0.401;

        /// <summary>CONVENTION: the letters one FUNDRAISE mailing sends - the party writes to this many of its supporters not yet written to, and may mail again.</summary>
        public const int MailingLetters = 100_000;

        /// <summary>
        /// K84: the letter's cost - the country's own postal tariff for one domestic letter, national currency (fetched byte-exact, each folder's
        /// `SHA256SUMS.txt`). SOURCED Germany: Deutsche Post, "Briefe in Deutschland", Standard up to 20 g, 0,95 € (`ElectionsData/germany/raw/postage/`);
        /// the USA: USPS Notice 123 effective October 4, 2026, First-Class Mail retail single-piece letter (stamped), 1 oz, $0.82
        /// (`ElectionsData/usa/raw/postage/`). BILLED Sweden: PostNord's price page refused the fetch (HTTP 403) and the procurement annex quoting it
        /// answered 404 (`ElectionsData/sweden/raw/postage/BILLED.txt`). NaN where no tariff is on disk - the mailing is refused, never priced
        /// with an invented stamp. DECLARED: the single-piece public tariff, no bulk or contract rate. DECLARED (review 2's L10): a real stamp is netted against the war chest, an [AUTHORED-DRAFT] playability scale (`LiveCampaignSetup.WarChest`), so the mailing's size against the chest is as authored as the chest - in the USA the chest is K19's, from FEC spending (`LiveCampaignSetup.UsWarChest`).
        /// </summary>
        public static double LetterTariff(CountryId? country)
        {
            switch (country)
            {
                case CountryId.Germany: return 0.95;
                case CountryId.USA: return 0.82;
                default: return double.NaN;
            }
        }

        /// <summary>K84: what one letter raises, in the national currency - [BCP12]'s revenue per solicitation at the game's rate.</summary>
        public static double RevenuePerLetter(CountryId? country) => country.HasValue ? RevenuePerLetterUsd * PoliSim.Data.EnergyLayer.NationalPerUsd(country.Value) : double.NaN;

        /// <summary>K84: the run-up HQ's line - the next mailing's letters, postage and revenue in <paramref name="purse"/>'s currency, or why none can go (no tariff on disk; every supporter written to).</summary>
        public static string FundraiseLine(State s, string purse)
        {
            double tariff = LetterTariff(s.Country);
            if (double.IsNaN(tariff)) { return "FUNDRAISE: NO POSTAL TARIFF ON DISK FOR THIS COUNTRY - NO LETTER CAN BE PRICED"; }
            double letters = Math.Floor(Math.Min(MailingLetters, SupportersLeft(s)));
            if (letters <= 0.0) { return "FUNDRAISE: EVERY SUPPORTER HAS BEEN WRITTEN TO"; }
            // review 2's M2: the NET beside the postage and the revenue (K88)
            return string.Format(CultureInfo.InvariantCulture, "FUNDRAISE: {0:N0} LETTERS \u00B7 POSTAGE {1:N0} \u00B7 RAISES {2:N0} \u00B7 NET {4:+#,0;-#,0} {3}", letters, letters * tariff,
                letters * RevenuePerLetter(s.Country), purse.ToUpperInvariant(), letters * (RevenuePerLetter(s.Country) - tariff));
        }

        /// <summary>Review 2's L1: whether a FUNDRAISE mailing can be sent now - a tariff on disk, a supporter not yet written to, the chest able to pay the postage.</summary>
        public static bool FundraiseOpen(State s)
        {
            double tariff = LetterTariff(s.Country);
            if (double.IsNaN(tariff)) { return false; }
            double letters = Math.Floor(Math.Min(MailingLetters, SupportersLeft(s)));
            return letters > 0.0 && s.Money >= letters * tariff / LiveCampaignSetup.PursePerUnit(s.Country, s.Staged);   // K110: the postage in the purse's units
        }

        /// <summary>K84: the party's supporters the run-up can write to - its prior share of the staging's electorate (the regions' eligible), less the letters already sent.</summary>
        public static double SupportersLeft(State s)
        {
            double eligible = 0.0, sum = 0.0;
            foreach (RegionAudience r in s.Staged.Regions) { eligible += r.Eligible; }
            foreach (double v in s.Staged.PriorShares) { sum += v; }
            double supporters = sum > 0.0 ? eligible * s.Staged.PriorShares[s.Party] / sum : 0.0;
            return Math.Max(0.0, supporters - s.LettersSent);
        }

        /// <summary>The verbs the pre-campaign REFUSES, each with the standing reason it prints - null for a priced verb.</summary>
        public static string Refusal(CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.Fundraise: return null;   // K84: priced by the country's postal tariff, refused where none is on disk (`LetterTariff`)
                case CampaignActionKind.DevelopPolicy: return "cut - voters do not respond to policy statements in a campaign";   // K1: q4 row 2, [AES11]
                case CampaignActionKind.TrainCandidate: return "cut - no study measures what candidate training does to a vote";   // K1: q4 row 3
                case CampaignActionKind.SetStrategy: return "cut - a strategy is its parts: the attack, the topic, the targeting";   // K1 and K6: q4 row 4, the player's choice cut
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
            /// <summary>K84: what a mailing raised (0 for every other verb).</summary>
            public double Raised;
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
            /// <summary>A-LOOP q24 5a: the run-up days a television buy aired on, each charged when it was made - its effect on the run-up's polls lasts its week.</summary>
            public List<int> TelevisionAired = new List<int>();
            /// <summary>K84: the country whose tariff prices a letter (null: none - the mailing is refused), the letters sent and what they raised.</summary>
            public CountryId? Country;
            public double LettersSent;
            public double Raised;
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
        public static State Begin(CampaignRun.Setup staged, int party, System.Random pollRandom, CountryId? country = null)
        {
            if (party < 0 || party >= staged.Parties.Length) { throw new ArgumentOutOfRangeException(nameof(party)); }
            if (pollRandom == null) { throw new ArgumentNullException(nameof(pollRandom)); }
            CampaignRun.PartySetup p = staged.Parties[party];
            return new State
            {
                Calendar = staged.Calendar, Party = party, PartyName = p.Name, Staged = staged, PollRandom = pollRandom,
                Money = p.StartingMoney, MoneyAtStart = p.StartingMoney, Volunteers = p.Volunteers,
                Day = 0, TotalDays = 7 * staged.Calendar.PreCampaignWeeks, Country = country,
            };
        }

        /// <summary>The price a verb's chip shows: a day's salary, the internal poll, a television buy at the ad's price (charged when made); an office's opening cost (paid on the campaign's day 0); 0 for a refused verb.</summary>
        public static double Price(State s, CampaignActionKind kind)
        {
            switch (kind)
            {
                case CampaignActionKind.EstablishOffice: return CampaignOffices.OpenCost;
                case CampaignActionKind.RecruitStaff: return CampaignStaff.SalaryPerDay;
                case CampaignActionKind.CommissionPolling: return s.Staged.InternalHouse.Cost;
                case CampaignActionKind.PrepareAdvertising: return CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
                case CampaignActionKind.Fundraise: { double tariff = LetterTariff(s.Country); return double.IsNaN(tariff) ? 0.0 : Math.Min(MailingLetters, SupportersLeft(s)) * tariff / LiveCampaignSetup.PursePerUnit(s.Country, s.Staged); }   // K84: the mailing's postage, in the purse's units (K110)
                default: return 0.0;
            }
        }

        /// <summary>The largest electorate the party has no office in, staged or planned - the one-region rule until the picker (the plan's S-C2); −1 when every region has one.</summary>
        public static int NextOfficeRegion(State s)
        {
            int best = -1; double bestAudience = -1.0;
            for (int r = 0; r < s.Staged.Regions.Length; r++)
            {
                if (s.HasOffice(r) || !s.Staged.StandsIn(s.Party, r)) { continue; }   // §697: only where the party stands
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
                        // A-LOOP q24 5b: back in the run-up, CL-1's rule as it stood before 5a - the siting the decision's region, else the one-region rule
                        int region = d.RegionIndex >= 0 ? d.RegionIndex : NextOfficeRegion(s);
                        if (region >= s.Staged.Regions.Length) { entry.Refusal = "no such region"; break; }
                        if (region < 0) { entry.Refusal = "no region without an office is left"; break; }
                        entry.Target = s.Staged.Regions[region].Name;
                        entry.Cost = CampaignOffices.OpenCost;
                        if (!s.Staged.StandsIn(s.Party, region)) { entry.Refusal = "the party has no list there"; break; }   // §697
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
                        s.LatestPoll = PollingSystem.Conduct(Standing(s, day), house, s.Today, s.PollRandom);   // q24 5a: the prior, with any television buy of the week on it
                        s.LastPollDay = day;
                        s.PollsBought++;
                        break;
                    }
                    case CampaignActionKind.PrepareAdvertising:
                    {
                        entry.Target = "television";
                        entry.Cost = CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
                        if (!s.HasRole(StaffRole.CampaignManager)) { entry.Refusal = "no campaign manager - the plan is the manager's"; break; }
                        if (s.Money < entry.Cost) { entry.Refusal = "the chest cannot pay the buy"; break; }
                        s.Money -= entry.Cost;   // A-LOOP q24 5a (SD-33's fix): charged the day it airs
                        s.TelevisionAired.Add(day);
                        break;
                    }
                    case CampaignActionKind.Fundraise:
                    {
                        // K84: a mailing to the party's own supporters - the postage paid, the letters' revenue into the chest; it buys contacts, never votes
                        entry.Target = "supporters by letter";
                        double tariff = LetterTariff(s.Country);
                        if (double.IsNaN(tariff)) { entry.Refusal = "no postal tariff is on disk for this country"; break; }
                        double letters = Math.Floor(Math.Min(MailingLetters, SupportersLeft(s)));
                        if (letters <= 0.0) { entry.Refusal = "every supporter has been written to"; break; }
                        double perUnit = LiveCampaignSetup.PursePerUnit(s.Country, s.Staged);   // K110: a sourced national price enters the purse in its units
                        entry.Cost = letters * tariff / perUnit;
                        if (s.Money < entry.Cost) { entry.Refusal = "the chest cannot pay the postage"; break; }
                        double raised = letters * RevenuePerLetter(s.Country) / perUnit;
                        s.Money += raised - entry.Cost;
                        s.LettersSent += letters;
                        s.Raised += raised;
                        entry.Raised = raised;
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
            return new Outcome(s.Money, s.Volunteers, offices.ToArray(), staff.ToArray(), p.TelevisionBuys, s.Digest.ToString());   // q24 5a: a run-up buy aired in the run-up; the campaign's plan is the staging's
        }

        /// <summary>
        /// A-LOOP q24 5a: the electorate's standing on run-up day <paramref name="day"/> - the prior (the electorate has not moved before the
        /// campaign), with the party's television buys of the week laid on it at [GGGS11]'s weekly size (`CampaignPersuasion.RunUpTelevisionEffectPp`), the sourced size whatever the buyer's (`CampaignPersuasion.Apply`),
        /// the other parties absorbing it. What a run-up poll is fielded from; the count never reads it.
        /// </summary>
        public static double[] Standing(State s, int day)
        {
            if (s == null) { throw new ArgumentNullException(nameof(s)); }
            double[] prior = s.Staged.PriorShares;
            double pp = CampaignPersuasion.RunUpTelevisionEffectPp(s.TelevisionAired, day);
            if (pp == 0.0) { return prior; }
            double sum = 0.0;
            foreach (double v in prior) { sum += v; }
            if (sum <= 0.0) { return prior; }
            var normalised = new double[prior.Length];
            for (int i = 0; i < prior.Length; i++) { normalised[i] = prior[i] / sum; }
            var effects = new double[prior.Length];
            effects[s.Party] = pp / 100.0;
            return CampaignPersuasion.Apply(normalised, effects);   // on the prior as a distribution, so the other parties absorb exactly the buy
        }

        /// <summary>A-LOOP q24 5a: the last run-up day a television buy aired on <paramref name="day"/> or before still shows on the polls, or −1 when none does - the HQ's note.</summary>
        public static int TelevisionShowsUntil(State s, int day)
        {
            int until = -1;
            if (s == null) { return until; }
            foreach (int aired in s.TelevisionAired)
            {
                int last = aired + CampaignPersuasion.TelevisionWeekDays - 1;
                if (aired <= day && last >= day && last > until) { until = last; }
            }
            return until;
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
