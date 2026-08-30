using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>§9's staff, at the prototype's depth: the five roles the worklist names. §9's other four (policy advisor, fundraiser, press secretary, volunteer coordinator) wait with §37.</summary>
    public enum StaffRole
    {
        CampaignManager = 0,
        MediaAdvisor = 1,
        Pollster = 2,
        FieldOrganizer = 3,
        DigitalStrategist = 4,
    }

    /// <summary>
    /// One hire. **§37's progression is DEFERRED and this type records the deferral by having no
    /// experience, level, speciality or growth member** — a staff member is a role and a salary,
    /// asserted by reflection in the harness. Between-election progression is a later item.
    /// </summary>
    public sealed class CampaignStaffMember
    {
        public readonly StaffRole Role;
        public readonly double SalaryPerDay;
        public readonly int DayHired;
        public int UnpaidDays;

        public CampaignStaffMember(StaffRole role, double salaryPerDay, int dayHired)
        {
            Role = role; SalaryPerDay = salaryPerDay; DayHired = dayHired;
        }
    }

    /// <summary>
    /// The campaign manager's budget plan — the thing W-B9 found a greedy AI cannot improvise:
    /// a share of the war chest set aside for the big-ticket buys BEFORE the campaign, so a party
    /// that wants television can afford it on the day instead of posting for a week to save up.
    /// Without a manager there is no plan: the pace releases money evenly and the party buys what
    /// it can afford (W-C1's rule, unchanged).
    /// </summary>
    public sealed class BudgetPlan
    {
        /// <summary>Television buys the plan intends over the campaign.</summary>
        public int TelevisionBuys;
        /// <summary>What one buy costs (the action's price; the plan saves toward it).</summary>
        public readonly double TelevisionCost;
        /// <summary>Money set aside so far and not yet spent.</summary>
        public double Fund;

        /// <summary>
        /// W-B12: what the ORGANISATION costs the party every day - the payroll, every office's
        /// maintenance and its daily operation. Set by the caller each morning, because a party
        /// that opens an office or hires today has a different bill tomorrow.
        ///
        /// This is the number W-B5's plan did not have, and its absence is what sent every party
        /// broke before polling day: the spending pace released money for ACTIONS against the
        /// whole war chest, while the fixed costs were charged from the same chest afterwards,
        /// so the two claims on the money never met until the money ran out.
        /// </summary>
        public double DailyFixedCost;

        /// <summary>
        /// W-B12: what must still be kept back to pay the organisation to polling day. The
        /// campaign manager's actual job, in one line: **pay the organisation first, release the
        /// rest.** A party without a manager has no plan and therefore no such discipline, which
        /// is exactly the difference §9 says a manager makes.
        /// </summary>
        public double CommittedToOrganisation(int daysLeft) => DailyFixedCost * Math.Max(0, daysLeft);

        public BudgetPlan(int televisionBuys, double televisionCost)
        {
            TelevisionBuys = Math.Max(0, televisionBuys); TelevisionCost = Math.Max(0.0, televisionCost);
        }

        /// <summary>Whether the fund can pay for a buy today.</summary>
        public bool Affords => TelevisionBuys > 0 && Fund >= TelevisionCost;

        /// <summary>
        /// One day of saving: <see cref="CampaignStaff.ManagerFundShare"/> of today's release goes to
        /// the fund while a planned buy is still short of its price. Returns what was set aside.
        /// </summary>
        public double Save(double release)
        {
            if (TelevisionBuys <= 0 || release <= 0.0) { return 0.0; }
            double shortfall = Math.Max(0.0, TelevisionBuys * TelevisionCost - Fund);
            double aside = Math.Min(release * CampaignStaff.ManagerFundShare, shortfall);
            Fund += aside;
            return aside;
        }

        /// <summary>Pay for a buy from the fund first; returns what the fund covered (the rest is the day's money).</summary>
        public double Pay(double spend)
        {
            double covered = Math.Min(spend, Fund);
            Fund -= covered;
            if (TelevisionBuys > 0) { TelevisionBuys--; }
            return covered;
        }
    }

    /// <summary>
    /// W-B5 / SPEC §9 — campaign staff at the prototype's depth. PURE, WIRED TO NOTHING (R-N2);
    /// the AI campaign carries one <see cref="StaffRoster"/> per party.
    ///
    /// **A hire changes the effectiveness of the actions its role touches and nothing else, and
    /// costs a salary every day it stands** — the payroll is a line in the party's books
    /// (`PartyLedger.StaffMoney`, the Campaign HQ ledger's line, W-E1). All **[AUTHORED-DRAFT]**,
    /// one line each in calibration entry 15:
    /// - the MEDIA ADVISOR raises the audience an interview or a policy announcement reaches
    ///   (<see cref="MediaAdvisorReach"/>) — the press is briefed, the line is quotable;
    /// - the DIGITAL STRATEGIST raises a social post's and a digital ad's (<see cref="DigitalReach"/>);
    /// - the POLLSTER buys a larger sample for the party's own poll at the same price
    ///   (<see cref="PollsterSample"/>) — a narrower ± on the horse race and the issues;
    /// - the FIELD ORGANIZER makes every office recruit faster and hold more
    ///   (<see cref="FieldOrganizerScale"/>);
    /// - the CAMPAIGN MANAGER carries the <see cref="BudgetPlan"/> — the only role whose effect is a
    ///   decision rule rather than a multiplier, because that is what a manager is.
    /// A member unpaid on a day gives nothing that day (the bonus is suspended, not the person
    /// dismissed). §37 is deferred (see <see cref="CampaignStaffMember"/>).
    /// </summary>
    public static class CampaignStaff
    {
        public const double SalaryPerDay = 1_800.0;
        public const double MediaAdvisorReach = 1.20;
        public const double DigitalReach = 1.25;
        public const double PollsterSample = 1.5;
        public const double FieldOrganizerScale = 1.5;
        /// <summary>The share of a day's release the manager sets aside while a planned buy is short.</summary>
        public const double ManagerFundShare = 0.5;

        public static readonly StaffRole[] TheFive =
        {
            StaffRole.CampaignManager, StaffRole.MediaAdvisor, StaffRole.Pollster, StaffRole.FieldOrganizer, StaffRole.DigitalStrategist,
        };

        /// <summary>Which §12 actions a role's multiplier touches.</summary>
        public static bool Touches(StaffRole role, CampaignActionKind kind)
        {
            switch (role)
            {
                case StaffRole.MediaAdvisor: return kind == CampaignActionKind.Interview || kind == CampaignActionKind.PolicyAnnouncement;
                case StaffRole.DigitalStrategist: return kind == CampaignActionKind.SocialPost || kind == CampaignActionKind.DigitalAd;
                default: return false;
            }
        }
    }

    /// <summary>One party's staff: who is on the payroll, what it costs today, what it changes.</summary>
    public sealed class StaffRoster
    {
        private readonly List<CampaignStaffMember> _members = new List<CampaignStaffMember>();
        private readonly bool[] _paidToday = new bool[CampaignStaff.TheFive.Length];

        public IReadOnlyList<CampaignStaffMember> Members => _members;
        public int Count => _members.Count;
        public BudgetPlan Plan { get; private set; }

        public bool Has(StaffRole role)
        {
            foreach (CampaignStaffMember m in _members) { if (m.Role == role) { return true; } }
            return false;
        }

        /// <summary>Whether the role is on the roster AND was paid today (an unpaid member gives nothing).</summary>
        public bool Active(StaffRole role) => Has(role) && _paidToday[(int)role];

        public double PayrollPerDay
        {
            get { double p = 0.0; foreach (CampaignStaffMember m in _members) { p += m.SalaryPerDay; } return p; }
        }

        /// <summary>Hire a role (one of each). A manager brings the plan; the salary starts tomorrow.</summary>
        public CampaignStaffMember Hire(StaffRole role, int day, BudgetPlan plan = null)
        {
            if (Has(role)) { throw new InvalidOperationException($"{role} already on the roster"); }
            var m = new CampaignStaffMember(role, CampaignStaff.SalaryPerDay, day);
            _members.Add(m);
            if (role == StaffRole.CampaignManager) { Plan = plan ?? new BudgetPlan(0, 0.0); }
            return m;
        }

        /// <summary>W-B12: what the roster costs for one day if everyone is paid - the payroll half of the organisation's daily bill, which the manager's plan must hold back.</summary>
        public double DailySalaryBill()
        {
            double bill = 0.0;
            foreach (CampaignStaffMember m in Members) { bill += m.SalaryPerDay; }
            return bill;
        }

        /// <summary>Pay today's salaries from <paramref name="money"/>, member by member; one the party cannot pay is unpaid today. Returns what was paid.</summary>
        public double PayDay(ref double money)
        {
            double paid = 0.0;
            Array.Clear(_paidToday, 0, _paidToday.Length);
            foreach (CampaignStaffMember m in _members)
            {
                if (money < m.SalaryPerDay) { m.UnpaidDays++; continue; }
                money -= m.SalaryPerDay;
                paid += m.SalaryPerDay;
                _paidToday[(int)m.Role] = true;
            }

            return paid;
        }

        /// <summary>The multiplier on the audience a national action reaches today: the advisor's, the strategist's, or 1.</summary>
        public double ReachMultiplier(CampaignActionKind kind)
        {
            double m = 1.0;
            if (Active(StaffRole.MediaAdvisor) && CampaignStaff.Touches(StaffRole.MediaAdvisor, kind)) { m *= CampaignStaff.MediaAdvisorReach; }
            if (Active(StaffRole.DigitalStrategist) && CampaignStaff.Touches(StaffRole.DigitalStrategist, kind)) { m *= CampaignStaff.DigitalReach; }
            return m;
        }

        /// <summary>The sample the party's own poll gets for the house's price: ×1.5 with a pollster, else the house's own.</summary>
        public double PollSampleMultiplier => Active(StaffRole.Pollster) ? CampaignStaff.PollsterSample : 1.0;

        /// <summary>How much faster the offices recruit and how much more they hold: ×1.5 with a field organizer, else 1.</summary>
        public double OfficeScale => Active(StaffRole.FieldOrganizer) ? CampaignStaff.FieldOrganizerScale : 1.0;

        /// <summary>The plan, if a manager is on the roster and paid today.</summary>
        public BudgetPlan ActivePlan => Active(StaffRole.CampaignManager) ? Plan : null;

        /// <summary>The party's own poll house as the pollster improves it (the same price, a larger sample).</summary>
        public PollingHouse Improve(PollingHouse house)
        {
            double k = PollSampleMultiplier;
            if (k == 1.0) { return house; }
            return new PollingHouse(house.Name, (int)Math.Round(house.SampleSize * k), house.Cost, house.HouseEffectPp, house.IsInternal);
        }
    }
}
