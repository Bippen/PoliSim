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
        public static CampaignSnapshot? Build(CampaignRun.State s, Country country, double perceivedEconomyIndex)
        {
            if (s == null || country == null) { return null; }
            int p = PlayerPartyIndex(s, country);
            if (p < 0) { return null; }
            CampaignRun.Setup setup = s.Setup;
            CampaignCalendar calendar = setup.Calendar;
            // The day the screen shows is the last day stepped (the run's "today" is the NEXT day to step);
            // before the first step it is the campaign's first day.
            int shownDay = s.Day > 0 ? s.Day - 1 : 0;
            System.DateTime today = calendar.CampaignStart.AddDays(shownDay);
            CampaignPhase phase = calendar.PhaseOn(today);

            var names = new string[setup.Parties.Length];
            for (int i = 0; i < names.Length; i++) { names[i] = setup.Parties[i].Name; }

            // The poll the party last saw: its own commissioned poll if it bought one, else the published tracker.
            Poll poll = s.LatestPoll[p] ?? s.PublicPoll ?? default;

            // Today's decisions, from the ledger's log - the day just stepped.
            var queue = new List<QueuedAction>();
            foreach (CampaignRun.DecisionRecord d in s.Ledgers[p].Log)
            {
                if (d.Day != shownDay) { continue; }
                CampaignActions.ActionSpec spec = CampaignActions.Spec(d.Kind);
                queue.Add(new QueuedAction(d.Kind, d.Target, d.Spend, spec.Hours));
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
