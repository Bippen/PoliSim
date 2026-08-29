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
