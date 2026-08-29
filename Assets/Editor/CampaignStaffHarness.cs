using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B5's harness — §9's staff at the prototype's depth.
    ///
    /// The done-when, asserted:
    /// 1. **hiring changes the relevant action's effectiveness** — each of the five roles, on the
    ///    action it touches, and on nothing else: the media advisor's interview persuades more
    ///    (the rally to the bit the same), the digital strategist's post reaches more (the
    ///    interview the same), the pollster's poll is narrower at the same price, the field
    ///    organizer's office is at full strength sooner, the manager's plan holds the television
    ///    money a party without a manager never has on the day;
    /// 2. **the payroll appears in the resource ledger** — the roster's payroll is the sum of its
    ///    salaries, paid daily from the party's money; a member the party cannot pay is unpaid
    ///    that day and gives nothing; nothing is spent the party does not have;
    /// 3. **§37's progression is deferred, and the deferral is recorded** — `CampaignStaffMember` has no
    ///    experience, level, speciality or growth member (by reflection).
    /// </summary>
    public static class CampaignStaffHarness
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B5: campaign staff (§9, prototype depth) - five roles, a salary a day, a bonus on the action the role touches, the manager's budget plan; §37 deferred ===\n");

            const double audience = 1_000_000.0, salience = 0.6, match = 0.5, credibility = 0.6;

            // ---------- 1. each role on its action, and on nothing else ----------
            {
                var roster = new StaffRoster();
                double money = 1e6;
                roster.Hire(StaffRole.MediaAdvisor, 0);
                roster.PayDay(ref money);
                CampaignActions.ActionSpec interview = CampaignActions.Spec(CampaignActionKind.Interview);
                CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
                CampaignActions.ChainTrace plain = CampaignActions.Resolve(interview, audience, salience, match, credibility, 0.0);
                CampaignActions.ChainTrace briefed = CampaignActions.Resolve(interview, audience * roster.ReachMultiplier(CampaignActionKind.Interview), salience, match, credibility, 0.0);
                CampaignActions.ChainTrace rallyPlain = CampaignActions.Resolve(rally, audience, salience, match, credibility, 300_000);
                CampaignActions.ChainTrace rallyStaffed = CampaignActions.Resolve(rally, audience * roster.ReachMultiplier(CampaignActionKind.Rally), salience, match, credibility, 300_000);
                failures += Assert(sb, "1a. the media advisor: an interview persuades more, a rally to the bit the same",
                    briefed.Persuasion > plain.Persuasion && rallyStaffed.Persuasion == rallyPlain.Persuasion && roster.ReachMultiplier(CampaignActionKind.Rally) == 1.0,
                    string.Format(CultureInfo.InvariantCulture, "interview {0:F1} -> {1:F1} (x{2:F2}); rally {3:F1} both", plain.Persuasion, briefed.Persuasion, briefed.Persuasion / plain.Persuasion, rallyPlain.Persuasion));

                var digital = new StaffRoster();
                digital.Hire(StaffRole.DigitalStrategist, 0);
                digital.PayDay(ref money);
                CampaignActions.ActionSpec post = CampaignActions.Spec(CampaignActionKind.SocialPost);
                CampaignActions.ChainTrace postPlain = CampaignActions.Resolve(post, audience, salience, match, credibility, 5_000);
                CampaignActions.ChainTrace postStaffed = CampaignActions.Resolve(post, audience * digital.ReachMultiplier(CampaignActionKind.SocialPost), salience, match, credibility, 5_000);
                failures += Assert(sb, "1b. the digital strategist: a social post reaches more, an interview the same",
                    postStaffed.Reach > postPlain.Reach && digital.ReachMultiplier(CampaignActionKind.Interview) == 1.0,
                    string.Format(CultureInfo.InvariantCulture, "post reach {0:N0} -> {1:N0}; interview x{2:F2}", postPlain.Reach, postStaffed.Reach, digital.ReachMultiplier(CampaignActionKind.Interview)));

                var polling = new StaffRoster();
                polling.Hire(StaffRole.Pollster, 0);
                polling.PayDay(ref money);
                var house = new PollingHouse("Standard commission", 1_200, 120_000, new double[8], isInternal: true);
                PollingHouse improved = polling.Improve(house);
                double moeBefore = PollingSystem.MarginOfErrorPp(0.30, house.SampleSize), moeAfter = PollingSystem.MarginOfErrorPp(0.30, improved.SampleSize);
                failures += Assert(sb, "1c. the pollster: the party's own poll is narrower at the same price",
                    improved.SampleSize > house.SampleSize && improved.Cost == house.Cost && moeAfter < moeBefore,
                    string.Format(CultureInfo.InvariantCulture, "sample {0} -> {1}, +/- {2:F2} -> {3:F2} pp at 30 %, {4:N0} kr both", house.SampleSize, improved.SampleSize, moeBefore, moeAfter, house.Cost));

                var field = new StaffRoster();
                field.Hire(StaffRole.FieldOrganizer, 0);
                field.PayDay(ref money);
                int daysPlain = DaysToFullStrength(1.0), daysStaffed = DaysToFullStrength(field.OfficeScale);
                failures += Assert(sb, "1d. the field organizer: an office is at full strength sooner and holds more volunteers",
                    daysStaffed < daysPlain && VolunteersAtFull(field.OfficeScale) > VolunteersAtFull(1.0),
                    string.Format(CultureInfo.InvariantCulture, "{0} days -> {1}; {2} volunteers -> {3}", daysPlain, daysStaffed, VolunteersAtFull(1.0), VolunteersAtFull(field.OfficeScale)));

                var managed = new StaffRoster();
                double tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
                managed.Hire(StaffRole.CampaignManager, 0, new BudgetPlan(1, tv));
                managed.PayDay(ref money);
                var unmanaged = new StaffRoster();
                int dayAfforded = -1;
                double release = 2_400_000.0 / 60.0;   // an even pace over a 60-day campaign
                for (int d = 0; d < 60; d++)
                {
                    managed.ActivePlan.Save(release);
                    if (dayAfforded < 0 && managed.ActivePlan.Affords) { dayAfforded = d; }
                }

                failures += Assert(sb, "1e. the manager's plan holds the television money by a day a party without a manager never has it (no plan, no fund)",
                    dayAfforded >= 0 && unmanaged.ActivePlan == null && managed.ActivePlan.Fund >= tv,
                    string.Format(CultureInfo.InvariantCulture, "fund {0:N0} kr on day {1} at {2:N0} kr a day released, half set aside", managed.ActivePlan.Fund, dayAfforded, release));
                double covered = managed.ActivePlan.Pay(tv);
                failures += Assert(sb, "1f. the buy is paid from the fund first and the plan's count falls", covered == tv && managed.ActivePlan.TelevisionBuys == 0 && managed.ActivePlan.Fund == 0.0,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} kr from the fund, {1} buys left", covered, managed.ActivePlan.TelevisionBuys));
            }

            // ---------- 2. the payroll ----------
            {
                var roster = new StaffRoster();
                foreach (StaffRole role in CampaignStaff.TheFive) { roster.Hire(role, 0, role == StaffRole.CampaignManager ? new BudgetPlan(0, 0) : null); }
                double money = 100_000.0;
                double paid = roster.PayDay(ref money);
                failures += Assert(sb, "2a. five on the roster: the payroll is five salaries, paid from the party's money exactly",
                    Math.Abs(roster.PayrollPerDay - 5 * CampaignStaff.SalaryPerDay) < 1e-9 && paid == roster.PayrollPerDay && Math.Abs(100_000.0 - money - paid) < 1e-9,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} kr a day; {1:N0} left", roster.PayrollPerDay, money));

                double thin = 2 * CampaignStaff.SalaryPerDay + 100.0;
                double thinPaid = roster.PayDay(ref thin);
                int unpaid = 0;
                foreach (CampaignStaffMember m in roster.Members) { unpaid += m.UnpaidDays; }
                failures += Assert(sb, "2b. a party that cannot pay everyone pays whom it can; the unpaid give nothing today; nothing is spent it does not have",
                    thinPaid == 2 * CampaignStaff.SalaryPerDay && unpaid == 3 && thin >= 0.0 && roster.Active(StaffRole.CampaignManager) && roster.Active(StaffRole.MediaAdvisor) && !roster.Active(StaffRole.Pollster)
                    && roster.PollSampleMultiplier == 1.0 && roster.ReachMultiplier(CampaignActionKind.SocialPost) == 1.0,
                    string.Format(CultureInfo.InvariantCulture, "paid {0:N0} of {1:N0}; {2} unpaid; {3:N0} kr left", thinPaid, roster.PayrollPerDay, unpaid, thin));
            }

            // ---------- 3. section 37 deferred ----------
            {
                var offenders = new StringBuilder();
                foreach (MemberInfo m in typeof(CampaignStaffMember).GetMembers(BindingFlags.Public | BindingFlags.Instance))
                {
                    string lower = m.Name.ToLowerInvariant();
                    foreach (string bad in new[] { "experience", "level", "special", "xp", "senior", "junior", "elite", "grow", "progress", "skill" })
                    {
                        if (lower.Contains(bad)) { offenders.Append(m.Name).Append(' '); }
                    }
                }

                failures += Assert(sb, "3a. section 37 deferred: a staff member is a role and a salary - no experience, level, speciality or growth member",
                    offenders.Length == 0, offenders.Length == 0 ? "clean" : $"offenders: {offenders}");

                var roster = new StaffRoster();
                foreach (StaffRole role in CampaignStaff.TheFive) { roster.Hire(role, 0, role == StaffRole.CampaignManager ? new BudgetPlan(0, 0) : null); }
                double money = 1e6;
                roster.PayDay(ref money);
                bool untouched = true;
                foreach (CampaignActionKind kind in new[] { CampaignActionKind.Rally, CampaignActionKind.TownHall, CampaignActionKind.DoorToDoor, CampaignActionKind.TelevisionAd })
                {
                    untouched &= roster.ReachMultiplier(kind) == 1.0;
                }

                failures += Assert(sb, "3b. with all five hired, the actions no role touches (rally, town hall, door-to-door, television) reach exactly what they did", untouched, "x1.00 each");
            }

            sb.Append($"\nSTAFF: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int DaysToFullStrength(double scale)
        {
            var net = new OfficeNetwork(1);
            double money = 1e9;
            net.Open(0, 0, 0.0, ref money);
            for (int d = 1; d <= 120; d++)
            {
                net.Day(null, 0, GotvOperation.DoorKnocking, ref money, out _, scale);
                if (net.Influence(0) >= 1.0) { return d; }
            }

            return int.MaxValue;
        }

        private static int VolunteersAtFull(double scale)
        {
            var net = new OfficeNetwork(1);
            double money = 1e9;
            net.Open(0, 0, 0.0, ref money);
            for (int d = 0; d < 120; d++) { net.Day(null, 0, GotvOperation.DoorKnocking, ref money, out _, scale); }
            return net.At(0).Volunteers;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
