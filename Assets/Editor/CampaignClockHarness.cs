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

            // 8. CL-2 (2026-09-13, DS-10): stories in the live run - a rate per party-day, every party equal, drawn from the appended
            //    Scandal stream; a scripted party's story held for its answer; the run's determinism kept. 8c is the load-bearing one:
            //    the rate is a Setup figure every harness leaves at 0, so the AI harness's digest (its own run, in this chain) cannot
            //    have moved; 8a says the live rate breaks stories at about its expectation.
            {
                var none = new (int Day, int Party, Scandal Scandal)[0];
                CampaignRun.Setup live = LiveCampaignSetup.Sweden(none, out _, liveScandalRate: Scandals.LiveRatePerPartyDay);
                CampaignRun.Result liveRun = CampaignAiHarness.RunSeeded(live, 777);
                var perParty = new int[live.Parties.Length];
                foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in liveRun.Scandals) { perParty[xParty]++; }
                int total = liveRun.Scandals.Count;
                double expectedStories = live.Parties.Length * (live.Calendar.TotalCampaignDays - 1) * Scandals.LiveRatePerPartyDay;
                int maxParty = 0;
                foreach (int n in perParty) { if (n > maxParty) { maxParty = n; } }
                failures += Assert(sb, "8a. at the live rate stories break for the parties at about the expectation (seed 777), none hoarding them",
                    total >= 1 && total <= (int)(expectedStories * 3) + 2 && maxParty <= 6,
                    $"{total} stories against {expectedStories:F1} expected ({live.Parties.Length} parties x {live.Calendar.TotalCampaignDays - 1} days before the last x 1/{1.0 / Scandals.LiveRatePerPartyDay:F0}); per party {string.Join("/", perParty)}");
                failures += Assert(sb, "8b. the same seed replays the live run to the same digest",
                    CampaignAiHarness.RunSeeded(live, 777).Digest == liveRun.Digest, $"digest {liveRun.Digest}, {total} stories");
                CampaignRun.Result plain = CampaignAiHarness.RunSeeded(LiveCampaignSetup.Sweden(none, out _), 777);
                CampaignRun.Result zero = CampaignAiHarness.RunSeeded(LiveCampaignSetup.Sweden(none, out _, liveScandalRate: 0.0), 777);
                failures += Assert(sb, "8c. at a rate of 0 the staging's digest is the staging's own and no story breaks (no stream drawn)",
                    plain.Digest == zero.Digest && zero.Scandals.Count == 0 && plain.Digest != liveRun.Digest,
                    $"digest {plain.Digest} with and without the figure; live {liveRun.Digest}");

                // A scripted party (0, the professional) whose answer is always to apologise: every story of its own resolves the
                // morning after it broke on that answer, and a day's pending list holds only that day's stories.
                CampaignRun.Setup answered = LiveCampaignSetup.Sweden(none, out _, playerParty: 0, playerScript: d => new AiDecision[0],
                    playerScandalScript: d => ScandalResponse.Apologize, liveScandalRate: 0.2);
                CampaignRun.State st = CampaignRun.Begin(answered, new System.Random(777), new System.Random(778), new System.Random(779));
                bool pendingOnlyToday = true;
                int heldOvernight = 0;
                while (!st.Finished)
                {
                    heldOvernight += st.PendingScandals.Count;
                    CampaignRun.StepDay(st);
                    foreach ((int pDay, int pParty, Scandal pStory, double pSeen) in st.PendingScandals) { if (pDay != st.Day - 1 || pParty != 0) { pendingOnlyToday = false; } }
                }
                CampaignRun.Result answeredRun = CampaignRun.Finish(st);
                int mine = 0, apologies = 0;
                foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in answeredRun.Scandals) { if (xParty == 0) { mine++; if (xResponse == ScandalResponse.Apologize) { apologies++; } } }
                failures += Assert(sb, "8d. a scripted party's stories are held for its answer and resolve the next morning on it (rate 0.2, always APOLOGIZE)",
                    mine >= 1 && apologies == mine && heldOvernight == mine && pendingOnlyToday && st.PendingScandals.Count == 0,
                    $"{mine} stories of party 0, {apologies} apologised; held overnight {heldOvernight}, {st.PendingScandals.Count} pending at the close; pending lists carried only the day's own: {pendingOnlyToday}");

                // The same party with a script that answers nothing: the run answers as its personality would (the professional explains,
                // or denies when the evidence looks weak) - a run no player watches never blocks.
                CampaignRun.Setup unanswered = LiveCampaignSetup.Sweden(none, out _, playerParty: 0, playerScript: d => new AiDecision[0],
                    playerScandalScript: d => null, liveScandalRate: 0.2);
                CampaignRun.Result instinct = CampaignAiHarness.RunSeeded(unanswered, 777);
                int mine2 = 0, byInstinct = 0;
                foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in instinct.Scandals) { if (xParty == 0) { mine2++; if (xResponse == ScandalResponse.Explain || xResponse == ScandalResponse.Deny) { byInstinct++; } } }
                failures += Assert(sb, "8e. a scripted party that answers nothing is answered by its personality's instinct (the professional: EXPLAIN, or DENY on weak evidence)",
                    mine2 >= 1 && byInstinct == mine2, $"{mine2} stories of party 0, {byInstinct} by instinct");

                // The picker's seam: a queued act carrying a region lands in that region - the ledger's own log names it.
                int regionPick = 3;
                CampaignRun.Setup baseSetup = LiveCampaignSetup.Sweden(none, out _);
                CampaignActions.ActionSpec townHall = CampaignActions.Spec(CampaignActionKind.TownHall);
                AiDecision pickedAct = new QueuedDecisionRecord { Day = 0, Kind = CampaignActionKind.TownHall, RegionIndex = regionPick, Spend = townHall.MoneyCost }.ToDecision(baseSetup);
                CampaignRun.Setup pickedSetup = LiveCampaignSetup.Sweden(none, out _, playerParty: 0, playerScript: d => d == 0 ? new[] { pickedAct } : new AiDecision[0]);
                CampaignRun.Result pickedRun = CampaignAiHarness.RunSeeded(pickedSetup, 777);
                bool landed = false;
                foreach (CampaignRun.DecisionRecord d in pickedRun.Parties[0].Log)
                {
                    if (d.Day == 0 && d.Kind == CampaignActionKind.TownHall && d.Target.StartsWith(pickedSetup.Regions[regionPick].Name, StringComparison.Ordinal)) { landed = true; }
                }
                failures += Assert(sb, "8f. a queued act carrying the map's pick lands in that region (the ledger's own log names it)",
                    landed, $"region {regionPick} = {pickedSetup.Regions[regionPick].Name}; day-0 log entries {pickedRun.Parties[0].Log.Count}");

                // The final day: no story breaks on the run's last day, for any party - the player's would have no morning left to be
                // answered on, and a story held then would hold the game's clock with no answer the run could still take. At a rate of
                // 1 every other day breaks one for every party, so the day before's story is certain and is apologised on the last day,
                // no AI party's story is resolved on the last day, and nothing waits at the close.
                CampaignRun.Setup everyDay = LiveCampaignSetup.Sweden(none, out _, playerParty: 0, playerScript: d => new AiDecision[0],
                    playerScandalScript: d => ScandalResponse.Apologize, liveScandalRate: 1.0);
                CampaignRun.State close = CampaignRun.Begin(everyDay, new System.Random(777), new System.Random(778), new System.Random(779));
                while (!close.Finished) { CampaignRun.StepDay(close); }
                int finalDay = close.TotalDays - 1, finalApologies = 0, everyApology = 0, othersOnFinalDay = 0;
                foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in close.Scandals)
                {
                    if (xParty != 0) { if (xDay == finalDay) { othersOnFinalDay++; } continue; }
                    if (xResponse == ScandalResponse.Apologize) { everyApology++; if (xDay == finalDay) { finalApologies++; } }
                }
                failures += Assert(sb, "8g. no story breaks on the final day for any party, the day before's is answered on the last day, none waits at the close (rate 1, always APOLOGIZE)",
                    finalApologies == 1 && everyApology == finalDay && othersOnFinalDay == 0 && close.PendingScandals.Count == 0,
                    $"final day {finalDay}: {finalApologies} apologised (the day before's story), {othersOnFinalDay} other parties' stories resolved on it; {everyApology} apologies in all; {close.PendingScandals.Count} pending at the close");

                // SACRIFICE STAFF carried out: each sacrifice takes the most recently hired member off the roster, so a party that
                // sacrifices for every story ends with its roster shorter by its sacrifices (never below empty).
                CampaignRun.Setup sacrificing = LiveCampaignSetup.Sweden(none, out _, playerParty: 0, playerScript: d => new AiDecision[0],
                    playerScandalScript: d => ScandalResponse.SacrificeStaffMember, liveScandalRate: 0.2);
                CampaignRun.State sac = CampaignRun.Begin(sacrificing, new System.Random(777), new System.Random(778), new System.Random(779));
                int rosterAtStart = sac.Staff[0].Count;
                while (!sac.Finished) { CampaignRun.StepDay(sac); }
                int sacrifices = 0;
                foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in sac.Scandals) { if (xParty == 0 && xOutcome.StaffMemberSacrificed) { sacrifices++; } }
                failures += Assert(sb, "8h. SACRIFICE STAFF takes a member off the roster for every story answered with it (rate 0.2, always SACRIFICE STAFF)",
                    sacrifices >= 1 && rosterAtStart >= 1 && sac.Staff[0].Count == Math.Max(0, rosterAtStart - sacrifices),
                    $"{sacrifices} sacrifices; the roster {rosterAtStart} at the start, {sac.Staff[0].Count} at the close");
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
