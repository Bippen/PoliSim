using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// C-R4b step 4a (2026-09-02) — **the Campaign HQ screen over the LIVE campaign.** Until this the
    /// only thing that ever filled a <see cref="CampaignSnapshot"/> was the screenshot driver's staged
    /// `CampaignFilmState`; the screen was a film. This builds the same snapshot from
    /// `SimulationManager.PlayerCampaign` — the party's own books as the run holds them (its pool, its
    /// staff, its offices, the poll it last saw, the momentum on the view) — so the rail's CAMPAIGN cell
    /// shows the campaign that is actually running for the player's party.
    ///
    /// ⚠ **What the "queue" is here.** The film staged a queue of intentions. The live party is
    /// AI-played until the HQ has an input path (step 4b), so the list shown is the party's DECISIONS
    /// of the last stepped day, read off its ledger — what it did, not what the player asked for.
    /// The screen's caption says so. Nothing here reads the truth: every figure comes from the party's
    /// ledger, pool, staff, offices and the polls it holds, which is the §36 seam the run itself keeps.
    /// </summary>
    public static class LiveCampaignSnapshot
    {
        /// <summary>The snapshot for the player's party in a running (or just finished) campaign, or null when the state carries no such party.</summary>
        public static CampaignSnapshot? Build(CampaignRun.State s, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record = null, int pickedRegion = -1)
        {
            if (s == null || country == null) { return null; }
            int p = PlayerPartyIndex(s, country);
            if (p < 0) { return null; }
            CampaignRun.Setup setup = s.Setup;
            CampaignCalendar calendar = setup.Calendar;
            bool playerRun = setup.Parties[p].Script != null && record != null;
            // A player-run party's screen is about the day it will step NEXT (what is queued for it); an
            // AI-played party's about the last day stepped (what it did). Before the first step, day 0.
            int shownDay = playerRun ? System.Math.Min(s.Day, System.Math.Max(0, s.TotalDays - 1)) : (s.Day > 0 ? s.Day - 1 : 0);
            System.DateTime today = calendar.CampaignStart.AddDays(shownDay);
            CampaignPhase phase = calendar.PhaseOn(today);

            var names = new string[setup.Parties.Length];
            for (int i = 0; i < names.Length; i++) { names[i] = setup.Parties[i].Name; }

            // The poll the party last saw: its own commissioned poll if it bought one, else the published tracker.
            Poll poll = s.LatestPoll[p] ?? s.PublicPoll ?? default;

            var queue = new List<QueuedAction>();
            if (playerRun)
            {
                // C-R4b step 4b: the player's queue for the next day, as the record holds it.
                foreach (QueuedDecisionRecord q in record.QueuedFor(s.Day))
                {
                    if (q.Kind == CampaignActionKind.CommissionPolling)   // K2: the campaign poll, the party's own house
                    {
                        queue.Add(new QueuedAction(q.Kind, setup.InternalHouse.Name, q.Spend, CampaignAi.PollingHours));
                        continue;
                    }
                    if (q.Kind == CampaignActionKind.PhoneBank)   // K82: the phone bank, in its region
                    {
                        queue.Add(new QueuedAction(q.Kind, q.RegionIndex >= 0 && q.RegionIndex < setup.Regions.Length ? setup.Regions[q.RegionIndex].Name : "national", q.Spend, GotvTurnout.PhoneBankHours));
                        continue;
                    }
                    CampaignActions.ActionSpec spec = CampaignActions.Spec(q.Kind);
                    string label = q.RegionIndex >= 0 && q.RegionIndex < setup.Regions.Length ? setup.Regions[q.RegionIndex].Name : "national";
                    // K3: an act that attacks names its target, as the authority writes the party
                    if (q.AgainstParty >= 0 && q.AgainstParty < setup.Parties.Length) { label += " · v " + PartySystems.ShortName(country.Id, setup.Parties[q.AgainstParty].Name); }
                    queue.Add(new QueuedAction(q.Kind, label, q.Spend, spec.Hours));
                }
            }
            else
            {
                // An AI-played party: its decisions of the day just stepped, from the ledger's log.
                foreach (CampaignRun.DecisionRecord d in s.Ledgers[p].Log)
                {
                    if (d.Day != shownDay) { continue; }
                    // the AI's polls ride the same log (K2's poll queues the same kind): a poll's hours are the poll's
                    double logHours = d.Kind == CampaignActionKind.CommissionPolling ? CampaignAi.PollingHours : d.Kind == CampaignActionKind.PhoneBank ? GotvTurnout.PhoneBankHours : CampaignActions.Spec(d.Kind).Hours;
                    queue.Add(new QueuedAction(d.Kind, d.Target, d.Spend, logHours));
                }
            }

            var staff = new List<StaffMember>();
            foreach (CampaignStaffMember m in s.Staff[p].Members)
            {
                bool paid = s.Staff[p].Active(m.Role);
                staff.Add(new StaffMember(SpacedRole(m.Role), paid ? "Paid today" : "Unpaid today",
                    paid ? "on the job" : "gives nothing today", m.SalaryPerDay));
            }

            var offices = new List<RegionalOffice>();
            foreach (CampaignOffice office in s.Offices[p].Offices)
            {
                offices.Add(new RegionalOffice(setup.Regions[office.Region].Name, office.Volunteers,
                    CampaignOffices.MaintenancePerDay + office.OperationsPerDay));
            }

            string markKey = null;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (party.Abbrev == setup.Parties[p].Name) { markKey = party.MarkName; break; }
            }

            // CL-2: the campaign's public events of the day just stepped - every party's, as the press carried them. A debate
            // names the two who stood and who won by how much (the margin is A's); a story names its party, the kind, the
            // severity and the answer, and whether the evidence surfaced on it. The run's own record, never a guess.
            var notes = new List<string>();
            int yesterday = s.Day - 1;
            if (yesterday >= 0)
            {
                // the party's own stories first, then the debate and the others' (the HQ shows two lines and counts the rest); on a
                // finished run the last stepped day is the one the strip calls today, so its events are THE LAST DAY's
                string when = s.Finished ? "THE LAST DAY · " : "YESTERDAY · ";
                var others = new List<string>();
                foreach ((int dDay, int a, int b, double margin, double dCoverage, double dMomentum) in s.Debates)
                {
                    if (dDay != yesterday) { continue; }
                    string verdict = margin > 0 ? names[a] + " BY " + margin.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                        : margin < 0 ? names[b] + " BY " + (-margin).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "A DRAW";
                    others.Add(when + "THE DEBATE: " + names[a] + " v " + names[b] + " · " + verdict);
                }
                for (int i = 0; i < s.Scandals.Count; i++)
                {
                    (int sDay, int sParty, ScandalResponse response, ScandalOutcome outcome) = s.Scandals[i];
                    if (sDay != yesterday) { continue; }
                    // by index: a party can answer two stories on one day (yesterday's held one and today's own), and the run appends
                    // `Stories` beside `Scandals` in the one place a story resolves
                    string line = when + names[sParty] + " · " + Scandals.KindCaption(s.Stories[i].Scandal.Kind) + " · " + Scandals.PastTense(response) + (outcome.Escalated ? " · THE EVIDENCE SURFACED" : "");
                    if (sParty == p) { notes.Add(line); } else { others.Add(line); }
                }
                // review 2's L5: what the player's queue asked for and the run could not make yesterday, with why
                foreach ((int rDay, CampaignActionKind rKind, string rReason) in s.Ledgers[p].Refused)
                {
                    if (rDay == yesterday) { notes.Insert(0, when + SpacedUpper(rKind) + " NOT MADE - " + rReason.ToUpperInvariant()); }
                }
                notes.AddRange(others);
            }

            // CL-2: the story waiting for the player's answer, with the answer queued for the morning if any.
            PendingScandalView? pending = null;
            if (playerRun)
            {
                foreach ((int pDay, int pParty, Scandal pScandal, double pSeen) in s.PendingScandals)
                {
                    if (pParty != p) { continue; }
                    ScandalResponse? queuedAnswer = record.AnswerFor(s.Day);
                    int roster = 0;
                    foreach (CampaignStaffMember m in s.Staff[p].Members) { roster++; }
                    pending = new PendingScandalView(pScandal.Kind, pScandal.Severity, pSeen, pDay, calendar.CampaignStart.AddDays(pDay), queuedAnswer, roster > 0);
                    break;
                }
            }

            // CL-2: the debate announced from the calendar, and where the next local act goes.
            int nextDebate = -1;
            foreach (int d in setup.DebateDays) { if (d >= shownDay && (nextDebate < 0 || d < nextDebate)) { nextDebate = d; } }
            string localActs = null;
            if (playerRun)
            {
                if (pickedRegion >= 0 && pickedRegion < setup.Regions.Length) { localActs = setup.Regions[pickedRegion].Name + " · PICKED ON THE MAP"; }
                else
                {
                    int strongest = StrongestRegion(s, p, out bool byOffice);
                    if (strongest >= 0) { localActs = setup.Regions[strongest].Name + (byOffice ? " · YOUR STRONGEST OFFICE" : " · THE LARGEST ELECTORATE"); }
                }
            }

            return new CampaignSnapshot(setup.Parties[p].Name, markKey, country.Name, phase, today, calendar,
                s.Pools[p], setup.Parties[p].StartingMoney, poll, names, p, (double[])s.MomentumPp.Clone(),
                queue.ToArray(), staff.ToArray(), offices.ToArray(), perceivedEconomyIndex, notes.ToArray(),
                (int[])setup.DebateDays.Clone(), nextDebate, pending, localActs);
        }

        /// <summary>
        /// C-R4b step 4b's one-region rule, moved here for the HQ and the map to share (CL-2): where a local act goes when
        /// nothing is picked - the region of the party's largest office by volunteers, else the largest electorate.
        /// </summary>
        public static int StrongestRegion(CampaignRun.State s, int p, out bool byOffice)
        {
            byOffice = false;
            if (s == null || p < 0 || p >= s.PartyCount) { return -1; }
            int best = -1;
            int bestVolunteers = -1;
            foreach (CampaignOffice office in s.Offices[p].Offices)
            {
                if (office.Volunteers > bestVolunteers) { bestVolunteers = office.Volunteers; best = office.Region; }
            }
            if (best >= 0) { byOffice = true; return best; }
            double bestAudience = -1.0;
            for (int r = 0; r < s.Setup.Regions.Length; r++)
            {
                if (!s.Setup.StandsIn(p, r)) { continue; }   // §697: the largest electorate where the party stands
                if (s.Setup.Regions[r].Audience > bestAudience) { bestAudience = s.Setup.Regions[r].Audience; best = r; }
            }
            return best;
        }

        /// <summary>
        /// CL-2: the campaign map over the LIVE run - board 4a's cartogram as the region picker. Every valkrets reads as
        /// UNKNOWN, honestly: regional detail is not on sale in the live run (W-E4's ladder is the film's), and §36 says the
        /// map must not tell the player where the race is close until they have paid to find out. What the sheet CAN say it
        /// says - the party's own offices, framed, with their volunteers; the region picked; where the next local act goes.
        /// </summary>
        public static CampaignMapSnapshot? BuildMap(CampaignRun.State s, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record, int pickedRegion)
        {
            CampaignSnapshot? campaign = Build(s, country, perceivedEconomyIndex, record, pickedRegion);
            if (!campaign.HasValue) { return null; }
            int p = campaign.Value.PlayerPartyIndex;
            CampaignRun.Setup setup = s.Setup;
            double national = 0.0;
            foreach (RegionAudience r in setup.Regions) { national += r.Audience; }
            var regions = new MapRegionReading[setup.Regions.Length];
            var volunteers = new int[setup.Regions.Length];
            for (int r = 0; r < regions.Length; r++)
            {
                regions[r] = SwingRegions.Unknown(setup.Regions[r].Name, national > 0.0 ? setup.Regions[r].Audience / national : 0.0);
                volunteers[r] = -1;
            }
            foreach (CampaignOffice office in s.Offices[p].Offices)
            {
                if (office.Region >= 0 && office.Region < volunteers.Length) { volunteers[office.Region] = office.Volunteers; }
            }
            const string offer = "NO REGIONAL POLL CAN BE BOUGHT IN THIS CAMPAIGN - EVERY VALKRETS READS AS UNKNOWN, AND THE SHEET DOES NOT GUESS FOR YOU. " +
                                 "THE MAP IS YOUR PICKER: A TILE SETS WHERE THE NEXT LOCAL ACT GOES.";
            // the heavy frame is where the next local act goes - the pick, else the one-region rule's region, as the ledger names it
            int nextLocalAct = pickedRegion >= 0 && pickedRegion < setup.Regions.Length ? pickedRegion : StrongestRegion(s, p, out _);
            return new CampaignMapSnapshot(campaign.Value, regions, campaign.Value.PartyNames, p, "", 0, s.Today, offer,
                live: true, nextLocalActRegion: nextLocalAct, officeVolunteers: volunteers);
        }

        /// <summary>
        /// CL-1 (2026-09-12): the HQ's snapshot for the RUN-UP - the player's party's pre-campaign state, on the same
        /// struct the campaign's day fills, so the screen is one screen: the chest as the run-up has left it (no hours -
        /// the day's twelve are the campaign's), the poll last bought on the prior, the queue for the day the run-up
        /// steps next at each verb's price, the staff hired here beside the staging's, the offices planned beside the
        /// staging's, and yesterday's entries as notes. Null when the state carries no such party.
        /// </summary>
        public static CampaignSnapshot? BuildPreCampaign(PreCampaignRun.State pre, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record = null, int pickedRegion = -1)
        {
            if (pre == null || country == null) { return null; }
            CampaignRun.Setup setup = pre.Staged;
            int p = pre.Party;
            if (p < 0 || p >= setup.Parties.Length || setup.Parties[p].Name != country.PlayerPartyAbbrev) { return null; }
            CampaignCalendar calendar = setup.Calendar;
            int shownDay = System.Math.Min(pre.Day, System.Math.Max(0, pre.TotalDays - 1));
            System.DateTime today = calendar.PreCampaignStart.AddDays(shownDay);
            CampaignPhase phase = calendar.PhaseOn(today);

            var names = new string[setup.Parties.Length];
            for (int i = 0; i < names.Length; i++) { names[i] = setup.Parties[i].Name; }
            // Before the first poll is bought the race the HQ draws is the last count itself - the prior, which the
            // campaign's fixed point reproduces - named as such with no sampling error, so the screen never reads a
            // null poll (the first film of the run-up threw on exactly that, at `DrawCampaignRace`).
            Poll poll = pre.LatestPoll ?? new Poll(calendar.PreCampaignStart, 0, "no poll bought yet - the last count, no sampling error",
                (double[])setup.PriorShares.Clone(), new double[setup.Parties.Length]);

            var queue = new List<QueuedAction>();
            if (record != null)
            {
                foreach (QueuedDecisionRecord q in record.QueuedFor(pre.Day - pre.TotalDays))
                {
                    string label = q.Kind == CampaignActionKind.RecruitStaff && q.Role >= 0 && q.Role < CampaignStaff.TheFive.Length ? SpacedRole((StaffRole)q.Role)
                        : q.RegionIndex >= 0 && q.RegionIndex < setup.Regions.Length ? setup.Regions[q.RegionIndex].Name
                        : q.Kind == CampaignActionKind.CommissionPolling ? setup.InternalHouse.Name
                        : q.Kind == CampaignActionKind.PrepareAdvertising ? "television" : "national";
                    queue.Add(new QueuedAction(q.Kind, label, PreCampaignRun.Price(pre, q.Kind), 0.0));
                }
            }

            var staff = new List<StaffMember>();
            foreach (StaffRole role in setup.Parties[p].Staff)
            {
                staff.Add(new StaffMember(SpacedRole(role), "Staged", "hired on the campaign's first day", CampaignStaff.SalaryPerDay));
            }
            foreach ((StaffRole Role, int Day) h in pre.Hired)
            {
                staff.Add(new StaffMember(SpacedRole(h.Role), $"Hired day {h.Day}", "paid from the day after the hire", CampaignStaff.SalaryPerDay));
            }

            var offices = new List<RegionalOffice>();
            foreach (int region in setup.Parties[p].Offices)
            {
                offices.Add(new RegionalOffice(setup.Regions[region].Name + " · staged", 0, CampaignOffices.MaintenancePerDay + setup.Parties[p].OfficeOperationsPerDay));
            }
            foreach (int region in pre.PlannedOffices)
            {
                offices.Add(new RegionalOffice(setup.Regions[region].Name + " · planned", 0, CampaignOffices.MaintenancePerDay + setup.Parties[p].OfficeOperationsPerDay));
            }

            var notes = new List<string>();
            // review 2's M2: the party's own currency (the HQ's purse - kr in Sweden, else the country's code), never a Swedish krona abroad
            string purse = country.Id == CountryId.Sweden ? "KR" : EnergyLayer.CurrencyCode(country.Id);
            if (pre.Day > 0)
            {
                foreach (PreCampaignRun.Entry e in PreCampaignRun.EntriesOn(pre, pre.Day - 1))
                {
                    notes.Add(e.Refusal == null
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "YESTERDAY · {0} {1} · {2:N0} {4}{3}", SpacedUpper(e.Kind), e.Target.ToUpperInvariant(), e.Cost,
                            e.Raised > 0.0 ? string.Format(System.Globalization.CultureInfo.InvariantCulture, " · RAISED {0:N0} {1} · NET {2:+#,0;-#,0} {1}", e.Raised, purse, e.Raised - e.Cost) : string.Empty, purse)   // K84: a mailing's revenue
                        : string.Format(System.Globalization.CultureInfo.InvariantCulture, "YESTERDAY · {0} {1} · REFUSED: {2}", SpacedUpper(e.Kind), e.Target.ToUpperInvariant(), e.Refusal.ToUpperInvariant()));
                }
            }

            // A-LOOP q24 5a (Q7): a television buy moves the run-up's polls for its week and never the count - the HQ says so while it shows.
            int televisionUntil = PreCampaignRun.TelevisionShowsUntil(pre, shownDay);
            // K67 (Elias, 2026-10-23: "the HQ shows its estimated effect beside the poll"): the buy's sourced size on the party's standing today -
            // [GGGS11]'s 4.73 points per 1 000 GRPs times the buys airing - with its 95 % band (1.96 x the estimate's SE, the poll's own band's
            // width convention), drawn beside the party's poll figure; DECLARED: the band is the estimate's, not the poll's sampling error
            string pollNote = null;
            double televisionPp = CampaignPersuasion.RunUpTelevisionEffectPp(pre.TelevisionAired, shownDay);
            if (televisionPp > 0.0 && televisionUntil >= 0)
            {
                double bandPp = 1.96 * televisionPp * CampaignPersuasion.TelevisionSePer1000Grp / CampaignPersuasion.TelevisionPpPer1000Grp;
                pollNote = string.Format(System.Globalization.CultureInfo.InvariantCulture, "TV BUY {0:+0.00} ±{1:0.00} PP UNTIL {2}", televisionPp, bandPp,
                    calendar.PreCampaignStart.AddDays(televisionUntil).ToString("d MMM").ToUpperInvariant());
            }
            if (televisionUntil >= 0)
            {
                notes.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "TV BUY · ON THE POLLS UNTIL {0} · FADED BY POLLING DAY",
                    calendar.PreCampaignStart.AddDays(televisionUntil).ToString("d MMM").ToUpperInvariant()));
            }

            string markKey = null;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (party.Abbrev == setup.Parties[p].Name) { markKey = party.MarkName; break; }
            }

            // K1 row 5 (S-C2): where the next office opens - the region picked on the map, else the largest electorate without one (CL-1's rule)
            int site = NextOfficeSite(pre, pickedRegion, out bool sitePicked);
            string siteLine = site < 0 ? null : setup.Regions[site].Name + (sitePicked ? " · PICKED ON THE MAP" : " · THE LARGEST ELECTORATE WITHOUT ONE");

            return new CampaignSnapshot(setup.Parties[p].Name, markKey, country.Name, phase, today, calendar,
                new ResourcePool(pre.Money, 0.0, pre.Volunteers), pre.MoneyAtStart, poll, names, p, new double[setup.Parties.Length],
                queue.ToArray(), staff.ToArray(), offices.ToArray(), perceivedEconomyIndex, notes.ToArray(), localActsRegion: siteLine, playerPollNote: pollNote);
        }

        /// <summary>K1 row 5 (S-C2): the region the next office opens in - the map's pick where the party stands and has no office planned, else CL-1's rule (`PreCampaignRun.NextOfficeRegion`); −1 when none is left.</summary>
        public static int NextOfficeSite(PreCampaignRun.State pre, int pickedRegion, out bool picked)
        {
            picked = pre != null && pickedRegion >= 0 && pickedRegion < pre.Staged.Regions.Length && pre.Staged.StandsIn(pre.Party, pickedRegion) && !pre.HasOffice(pickedRegion);
            return picked ? pickedRegion : (pre == null ? -1 : PreCampaignRun.NextOfficeRegion(pre));
        }

        /// <summary>
        /// K1 row 5 (S-C2, 2026-10-23): the campaign map over the RUN-UP - board 4a's cartogram as the OFFICE SITING picker. A tile sets where the
        /// next queued office opens (on the campaign's first day, as CL-1 plans it); the party's offices, staged and planned, are framed; every
        /// valkrets reads as unknown, as on the campaign's own map (no regional poll is on sale). Null when the state carries no such party.
        /// </summary>
        public static CampaignMapSnapshot? BuildPreCampaignMap(PreCampaignRun.State pre, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record, int pickedRegion)
        {
            CampaignSnapshot? campaign = BuildPreCampaign(pre, country, perceivedEconomyIndex, record, pickedRegion);
            if (!campaign.HasValue) { return null; }
            CampaignRun.Setup setup = pre.Staged;
            double national = 0.0;
            foreach (RegionAudience r in setup.Regions) { national += r.Audience; }
            var regions = new MapRegionReading[setup.Regions.Length];
            var volunteers = new int[setup.Regions.Length];
            for (int r = 0; r < regions.Length; r++)
            {
                regions[r] = SwingRegions.Unknown(setup.Regions[r].Name, national > 0.0 ? setup.Regions[r].Audience / national : 0.0);
                volunteers[r] = pre.HasOffice(r) ? 0 : -1;   // an office not yet open has no volunteers of its own
            }
            const string offer = "IN THE RUN-UP THE MAP SITES YOUR NEXT OFFICE: A TILE SETS WHERE IT OPENS ON THE CAMPAIGN'S FIRST DAY. " +
                                 "EVERY VALKRETS READS AS UNKNOWN - NO REGIONAL POLL CAN BE BOUGHT.";
            int site = NextOfficeSite(pre, pickedRegion, out _);
            return new CampaignMapSnapshot(campaign.Value, regions, campaign.Value.PartyNames, campaign.Value.PlayerPartyIndex, "", 0, pre.Today, offer,
                live: true, nextLocalActRegion: site, officeVolunteers: volunteers, siting: true);
        }

        private static string SpacedUpper(CampaignActionKind kind)
        {
            string name = kind.ToString();
            var sb = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) { sb.Append(' '); }
                sb.Append(char.ToUpperInvariant(name[i]));
            }
            return sb.ToString();
        }

        /// <summary>The player's party's index in the run's party order, by the abbreviation the country records; −1 when the player has no party or it is not in the run.</summary>
        public static int PlayerPartyIndex(CampaignRun.State s, Country country)
        {
            if (s == null || country == null || string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return -1; }
            for (int i = 0; i < s.Setup.Parties.Length; i++)
            {
                if (s.Setup.Parties[i].Name == country.PlayerPartyAbbrev) { return i; }
            }
            return -1;
        }

        private static string SpacedRole(StaffRole role)
        {
            string name = role.ToString();
            var sb = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) { sb.Append(' '); }
                sb.Append(i == 0 ? name[i] : char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }
    }
}
