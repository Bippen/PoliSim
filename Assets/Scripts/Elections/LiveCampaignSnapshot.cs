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
        public static CampaignSnapshot? Build(CampaignRun.State s, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record = null)
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
                    CampaignActions.ActionSpec spec = CampaignActions.Spec(q.Kind);
                    string label = q.RegionIndex >= 0 && q.RegionIndex < setup.Regions.Length ? setup.Regions[q.RegionIndex].Name : "national";
                    queue.Add(new QueuedAction(q.Kind, label, q.Spend, spec.Hours));
                }
            }
            else
            {
                // An AI-played party: its decisions of the day just stepped, from the ledger's log.
                foreach (CampaignRun.DecisionRecord d in s.Ledgers[p].Log)
                {
                    if (d.Day != shownDay) { continue; }
                    CampaignActions.ActionSpec spec = CampaignActions.Spec(d.Kind);
                    queue.Add(new QueuedAction(d.Kind, d.Target, d.Spend, spec.Hours));
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

            return new CampaignSnapshot(setup.Parties[p].Name, markKey, country.Name, phase, today, calendar,
                s.Pools[p], setup.Parties[p].StartingMoney, poll, names, p, (double[])s.MomentumPp.Clone(),
                queue.ToArray(), staff.ToArray(), offices.ToArray(), perceivedEconomyIndex);
        }

        /// <summary>
        /// CL-1 (2026-09-12): the HQ's snapshot for the RUN-UP - the player's party's pre-campaign state, on the same
        /// struct the campaign's day fills, so the screen is one screen: the chest as the run-up has left it (no hours -
        /// the day's twelve are the campaign's), the poll last bought on the prior, the queue for the day the run-up
        /// steps next at each verb's price, the staff hired here beside the staging's, the offices planned beside the
        /// staging's, and yesterday's entries as notes. Null when the state carries no such party.
        /// </summary>
        public static CampaignSnapshot? BuildPreCampaign(PreCampaignRun.State pre, Country country, double perceivedEconomyIndex, PlayerCampaignRecord record = null)
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
            if (pre.Day > 0)
            {
                foreach (PreCampaignRun.Entry e in PreCampaignRun.EntriesOn(pre, pre.Day - 1))
                {
                    notes.Add(e.Refusal == null
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "YESTERDAY · {0} {1} · {2:N0} KR", SpacedUpper(e.Kind), e.Target.ToUpperInvariant(), e.Cost)
                        : string.Format(System.Globalization.CultureInfo.InvariantCulture, "YESTERDAY · {0} {1} · REFUSED: {2}", SpacedUpper(e.Kind), e.Target.ToUpperInvariant(), e.Refusal.ToUpperInvariant()));
                }
            }

            string markKey = null;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (party.Abbrev == setup.Parties[p].Name) { markKey = party.MarkName; break; }
            }

            return new CampaignSnapshot(setup.Parties[p].Name, markKey, country.Name, phase, today, calendar,
                new ResourcePool(pre.Money, 0.0, pre.Volunteers), pre.MoneyAtStart, poll, names, p, new double[setup.Parties.Length],
                queue.ToArray(), staff.ToArray(), offices.ToArray(), perceivedEconomyIndex, notes.ToArray());
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
