using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B1's harness — advances a **full campaign day by day** and checks that every phase
    /// transition fires on its own date, that legality changes with the phase, and that the whole
    /// thing is a pure computation over dates (the existing turn loop is never touched).
    ///
    /// The done-when, asserted:
    /// 1. every day from before the pre-campaign to after the election resolves to exactly one
    ///    phase, and the sequence is monotonic — Dormant → PreCampaign → Campaign → ElectionDay →
    ///    Concluded, with no phase ever revisited;
    /// 2. the transitions land on the computed dates, to the day;
    /// 3. Sweden's real window is right: election 2026-09-13, campaign opening 8 weeks earlier;
    /// 4. legality changes at the boundaries — a rally is illegal the day before the campaign opens
    ///    and legal the day it does; on election day only the ground game remains;
    /// 5. a snap election (a shorter window) works without a code change — the calendar is data.
    /// </summary>
    public static class CampaignClockHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B1: the campaign clock (§3) ===\n");

            CampaignCalendar sweden = CampaignCalendar.Sweden2026;
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  Sweden 2026: pre-campaign opens {0:yyyy-MM-dd}, campaign opens {1:yyyy-MM-dd}, election {2:yyyy-MM-dd} ({3} campaign days)\n",
                sweden.PreCampaignStart, sweden.CampaignStart, sweden.ElectionDate, sweden.TotalCampaignDays));

            // 3. The real window.
            failures += Assert(sb, "3a. election date is Sweden's real one (2nd Sunday in September 2026)",
                sweden.ElectionDate == new DateTime(2026, 9, 13) && sweden.ElectionDate.DayOfWeek == DayOfWeek.Sunday,
                $"{sweden.ElectionDate:yyyy-MM-dd}, a {sweden.ElectionDate.DayOfWeek}");
            failures += Assert(sb, "3b. the campaign is the final 8 weeks",
                (sweden.ElectionDate - sweden.CampaignStart).TotalDays == 56,
                $"{(sweden.ElectionDate - sweden.CampaignStart).TotalDays} days");

            // 1 + 2. Walk every day of the whole span and record transitions.
            DateTime from = sweden.PreCampaignStart.AddDays(-20);
            DateTime to = sweden.ElectionDate.AddDays(20);
            var order = new List<CampaignPhase>();
            var transitions = new List<(DateTime Date, CampaignPhase Phase)>();
            CampaignPhase previous = (CampaignPhase)(-1);
            int days = 0;

            for (DateTime d = from; d <= to; d = d.AddDays(1))
            {
                days++;
                CampaignPhase phase = sweden.PhaseOn(d);
                if (phase != previous)
                {
                    transitions.Add((d, phase));
                    order.Add(phase);
                    previous = phase;
                }
            }

            sb.Append($"  walked {days} days; {transitions.Count} transitions:\n");
            foreach (var t in transitions)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0:yyyy-MM-dd}  -> {1}\n", t.Date, t.Phase));
            }

            var expected = new[]
            {
                CampaignPhase.Dormant, CampaignPhase.PreCampaign, CampaignPhase.Campaign,
                CampaignPhase.ElectionDay, CampaignPhase.Concluded,
            };
            bool sequenceOk = order.Count == expected.Length;
            for (int i = 0; sequenceOk && i < expected.Length; i++) { sequenceOk = order[i] == expected[i]; }

            failures += Assert(sb, "1. the phase sequence is monotonic and complete, no phase revisited",
                sequenceOk, string.Join(" -> ", order));

            failures += Assert(sb, "2a. pre-campaign fires on its computed date",
                transitions[1].Date == sweden.PreCampaignStart, $"{transitions[1].Date:yyyy-MM-dd}");
            failures += Assert(sb, "2b. campaign fires on its computed date",
                transitions[2].Date == sweden.CampaignStart, $"{transitions[2].Date:yyyy-MM-dd}");
            failures += Assert(sb, "2c. election day is the election date",
                transitions[3].Date == sweden.ElectionDate, $"{transitions[3].Date:yyyy-MM-dd}");
            failures += Assert(sb, "2d. concluded fires the day after",
                transitions[4].Date == sweden.ElectionDate.AddDays(1), $"{transitions[4].Date:yyyy-MM-dd}");

            // 4. Legality flips at the boundary.
            CampaignPhase dayBefore = sweden.PhaseOn(sweden.CampaignStart.AddDays(-1));
            CampaignPhase firstDay = sweden.PhaseOn(sweden.CampaignStart);
            failures += Assert(sb, "4a. a rally is illegal the day before the campaign opens",
                !CampaignLegality.IsLegal(CampaignActionKind.Rally, dayBefore), $"phase {dayBefore}");
            failures += Assert(sb, "4b. a rally is legal on the day it opens",
                CampaignLegality.IsLegal(CampaignActionKind.Rally, firstDay), $"phase {firstDay}");
            failures += Assert(sb, "4c. candidate training is a pre-campaign verb only",
                CampaignLegality.IsLegal(CampaignActionKind.TrainCandidate, CampaignPhase.PreCampaign)
                && !CampaignLegality.IsLegal(CampaignActionKind.TrainCandidate, CampaignPhase.Campaign),
                "legal in pre-campaign, not in campaign");
            failures += Assert(sb, "4d. election day leaves only the ground game",
                CampaignLegality.IsLegal(CampaignActionKind.GetOutTheVote, CampaignPhase.ElectionDay)
                && !CampaignLegality.IsLegal(CampaignActionKind.TelevisionAd, CampaignPhase.ElectionDay),
                $"{CampaignLegality.LegalActions(CampaignPhase.ElectionDay).Length} actions legal");
            failures += Assert(sb, "4e. nothing is legal once concluded",
                CampaignLegality.LegalActions(CampaignPhase.Concluded).Length == 0, "0 actions");

            sb.Append("  legal actions by phase: ");
            foreach (CampaignPhase p in new[] { CampaignPhase.Dormant, CampaignPhase.PreCampaign, CampaignPhase.Campaign, CampaignPhase.ElectionDay, CampaignPhase.Concluded })
            {
                sb.Append($"{p}={CampaignLegality.LegalActions(p).Length} ");
            }

            sb.Append('\n');

            // 5. A snap election - a different calendar, no code change.
            var snap = new CampaignCalendar(new DateTime(2027, 3, 21), campaignWeeks: 3, preCampaignWeeks: 1);
            // Election 2027-03-21 less 3 weeks = campaign opens 2027-02-28; pre-campaign one week
            // earlier = 2027-02-21. The boundaries are checked on the exact days either side.
            failures += Assert(sb, "5. a snap election works as data (3-week campaign, 1-week run-up)",
                snap.CampaignStart == new DateTime(2027, 2, 28)
                && snap.PreCampaignStart == new DateTime(2027, 2, 21)
                && snap.PhaseOn(new DateTime(2027, 2, 28)) == CampaignPhase.Campaign
                && snap.PhaseOn(new DateTime(2027, 2, 27)) == CampaignPhase.PreCampaign
                && snap.PhaseOn(new DateTime(2027, 2, 20)) == CampaignPhase.Dormant
                && snap.TotalCampaignDays == 21,
                $"campaign opens {snap.CampaignStart:yyyy-MM-dd}, pre-campaign {snap.PreCampaignStart:yyyy-MM-dd}, {snap.TotalCampaignDays} days");

            // Campaign-day counter, what a resource budget will run on.
            failures += Assert(sb, "6. campaign-day counter is 0 before, and the full length on polling day",
                sweden.CampaignDaysElapsed(sweden.CampaignStart.AddDays(-1)) == 0
                && sweden.CampaignDaysElapsed(sweden.ElectionDate) == sweden.TotalCampaignDays,
                $"0 .. {sweden.TotalCampaignDays}");

            // 7. CL-1 (2026-09-12): the pre-campaign, stepped for the player's party alone on the campaign's own staging
            //    (`PreCampaignRun`). 7a is the load-bearing one: a run-up in which nothing is queued leaves the campaign's
            //    Setup identical - proven on the campaign's own decision digest at seed 777, the proof every other campaign
            //    harness stands on - so wiring the run-up moved no digest anywhere.
            {
                // The same staging call twice - the AI harness's own (its staged scandal included) - once plain and once
                // handed the idle run-up's outcome for party 0; the digest must not move.
                var stagedScandals = new[] { (30, 0, new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, 0.5)) };
                CampaignRun.Setup staging = LiveCampaignSetup.Sweden(stagedScandals, out _);
                PreCampaignRun.State idle = PreCampaignRun.Begin(staging, 0, new System.Random(7));
                while (!idle.Finished) { PreCampaignRun.StepDay(idle, null); }
                PreCampaignRun.Outcome idleOutcome = PreCampaignRun.Finish(idle);
                CampaignRun.Setup withIdle = LiveCampaignSetup.Sweden(stagedScandals, out _, playerParty: 0, playerOutcome: idleOutcome);
                CampaignRun.Result control = CampaignAiHarness.RunSeeded(staging, 777);
                CampaignRun.Result idleRun = CampaignAiHarness.RunSeeded(withIdle, 777);
                failures += Assert(sb, "7a. an idle run-up leaves the campaign's decision digest byte-identical (seed 777)",
                    control.Digest == idleRun.Digest,
                    $"{idle.TotalDays} days stepped; chest {idleOutcome.Money:F0} against the staging's {staging.Parties[0].StartingMoney:F0}; {idleOutcome.Offices.Length} offices, {idleOutcome.Staff.Length} staff, {idleOutcome.TelevisionBuys} buys");
                failures += Assert(sb, "7b. the run-up is the calendar's 26 weeks and ends the day before the campaign opens",
                    idle.TotalDays == 7 * CampaignCalendar.DefaultPreCampaignWeeks && staging.Calendar.PreCampaignStart.AddDays(idle.TotalDays) == staging.Calendar.CampaignStart,
                    $"{idle.TotalDays} days, {staging.Calendar.PreCampaignStart:yyyy-MM-dd} .. {staging.Calendar.CampaignStart:yyyy-MM-dd}");

                PreCampaignRun.State busy = PreCampaignRun.Begin(staging, 0, new System.Random(7));
                int region = PreCampaignRun.NextOfficeRegion(busy);
                // A role the party's staging does NOT already hold (party 0's cast hires a manager and a pollster on day 0).
                StaffRole hireRole = StaffRole.Pollster;
                foreach (StaffRole candidate in CampaignStaff.TheFive) { if (!busy.HasRole(candidate)) { hireRole = candidate; break; } }
                var day0 = new List<PreCampaignRun.Decision>
                {
                    new PreCampaignRun.Decision(CampaignActionKind.RecruitStaff, role: (int)hireRole),
                    new PreCampaignRun.Decision(CampaignActionKind.EstablishOffice, region),
                };
                PreCampaignRun.StepDay(busy, day0);
                while (!busy.Finished) { PreCampaignRun.StepDay(busy, null); }
                PreCampaignRun.Outcome busyOutcome = PreCampaignRun.Finish(busy);
                double expectedChest = staging.Parties[0].StartingMoney - (busy.TotalDays - 1) * CampaignStaff.SalaryPerDay;
                failures += Assert(sb, $"7c. a {hireRole} hired on day 0 costs the salary on every later run-up day; an office planned costs nothing until the campaign opens",
                    Math.Abs(busyOutcome.Money - expectedChest) < 1e-6 && Array.IndexOf(busyOutcome.Staff, hireRole) >= 0
                    && Array.IndexOf(busyOutcome.Offices, region) >= 0 && busy.Log.Count == 2 && busy.Log[0].Refusal == null && busy.Log[1].Refusal == null,
                    $"chest {busyOutcome.Money:F0} against {expectedChest:F0}; staff {string.Join("/", busyOutcome.Staff)}; offices {busyOutcome.Offices.Length} (region {region} in); log {busy.Log.Count}");

                PreCampaignRun.State refused = PreCampaignRun.Begin(staging, 0, new System.Random(7));
                var four = new List<PreCampaignRun.Decision>
                {
                    new PreCampaignRun.Decision(CampaignActionKind.Fundraise), new PreCampaignRun.Decision(CampaignActionKind.DevelopPolicy),
                    new PreCampaignRun.Decision(CampaignActionKind.TrainCandidate), new PreCampaignRun.Decision(CampaignActionKind.SetStrategy),
                };
                PreCampaignRun.StepDay(refused, four);
                int refusals = 0;
                foreach (PreCampaignRun.Entry e in refused.Log) { if (e.Refusal != null) { refusals++; } }
                failures += Assert(sb, "7d. fundraise, policy, training and strategy are refused with a reason, the chest untouched",
                    refusals == 4 && refused.Log.Count == 4 && Math.Abs(refused.Money - staging.Parties[0].StartingMoney) < 1e-9,
                    $"{refusals} refused of {refused.Log.Count}; chest {refused.Money:F0}");

                var onePoll = new List<PreCampaignRun.Decision> { new PreCampaignRun.Decision(CampaignActionKind.CommissionPolling) };
                PreCampaignRun.State polled = PreCampaignRun.Begin(staging, 0, new System.Random(11));
                PreCampaignRun.StepDay(polled, onePoll);
                PreCampaignRun.State polledAgain = PreCampaignRun.Begin(staging, 0, new System.Random(11));
                PreCampaignRun.StepDay(polledAgain, onePoll);
                failures += Assert(sb, "7e. a poll costs the internal house's fee, reads the prior within the house's error, and replays to the same digest",
                    polled.PollsBought == 1 && Math.Abs(polled.Money - (staging.Parties[0].StartingMoney - staging.InternalHouse.Cost)) < 1e-6
                    && polled.LatestPoll.HasValue && polled.Digest.ToString() == polledAgain.Digest.ToString()
                    && Math.Abs(polled.LatestPoll.Value.Share(0) - staging.PriorShares[0]) < 0.10,
                    $"chest {polled.Money:F0}; share(0) {(polled.LatestPoll.HasValue ? polled.LatestPoll.Value.Share(0) : double.NaN):F3} against prior {staging.PriorShares[0]:F3}; digest {polled.Digest.Length} chars");
            }

            sb.Append($"\n=== CampaignClockHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
