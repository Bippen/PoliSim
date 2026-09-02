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
    /// W-B9's harness — §13's media system and §14's audience segmentation, and the standing
    /// ruling that media INTEREST is availability.
    ///
    /// The done-when, asserted:
    /// 1. **a coverage spike decays on the measured curve** — one day of national attention, then
    ///    silence: the stock halves every `CoverageHalfLifeDays` to within 1e-9 of the declared
    ///    exponential, and is essentially gone within a month;
    /// 2. **coverage cannot spiral** — a party that does the most newsworthy thing possible every
    ///    day for a year stays under `MediaCoverage.Ceiling()`, and the interest it produces stays
    ///    under 1;
    /// 3. **the same message performs differently by outlet audience** — one climate message from
    ///    one party through a young-urban outlet and an older-rural one, resolved for a two-group
    ///    electorate whose groups differ only in what they care about; the ratio is printed and
    ///    asserted to be material;
    /// 4. **media interest is availability, not a cost** — a party with no coverage, no momentum
    ///    and a small polled share is booked by NO outlet, whatever money it has; the same party
    ///    after a day of news is booked; a bigger party is booked more; the interview's spec still
    ///    costs 0 kr (no cap, no fee was added — the ruling);
    /// 5. **coverage creates momentum, bounded** — the momentum shock a day of maximal news can
    ///    produce is `MomentumPpPerCoverage × CoverageScale` and no more.
    /// </summary>
    public static class MediaHarness
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B9: the media system (§13) and audience segmentation (§14) ===\n");

            // ---------- 1. the spike decays on the declared curve ----------
            var coverage = new MediaCoverage(1);
            coverage.AddRaw(0, 10.0);                     // a very newsworthy day
            double[] gain0 = coverage.CloseDay();
            double c0 = coverage.Coverage(0);
            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  a spike: raw 10.0 -> gain {0:F4} (saturated at scale {1}); then silence:\n  day  coverage  expected\n", gain0[0], MediaSystem.CoverageScale));
            double worst = 0.0;
            for (int day = 1; day <= 30; day++)
            {
                coverage.CloseDay();
                double expected = c0 * Math.Pow(0.5, day / MediaSystem.CoverageHalfLifeDays);
                worst = Math.Max(worst, Math.Abs(coverage.Coverage(0) - expected));
                if (day <= 9 || day % 10 == 0)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0,3}  {1,8:F5}  {2,8:F5}\n", day, coverage.Coverage(0), expected));
                }
            }

            failures += Assert(sb, "1a. the spike decays on the declared half-life curve (max deviation over 30 days)",
                worst < 1e-9, string.Format(CultureInfo.InvariantCulture, "{0:E2}; half-life {1} days", worst, MediaSystem.CoverageHalfLifeDays));
            failures += Assert(sb, "1b. a spike is essentially gone within a month",
                coverage.Coverage(0) < 0.001 * c0, string.Format(CultureInfo.InvariantCulture, "day 30 at {0:P2} of the spike", coverage.Coverage(0) / c0));

            // ---------- 2. it cannot spiral ----------
            var relentless = new MediaCoverage(1);
            double peak = 0.0;
            double peakInterest = 0.0;
            for (int day = 0; day < 365; day++)
            {
                relentless.AddRaw(0, 100.0);   // far beyond any real day
                double[] gain = relentless.CloseDay();
                peak = Math.Max(peak, relentless.Coverage(0));
                peakInterest = Math.Max(peakInterest, MediaSystem.Interest(relentless.Coverage(0), 50.0, 1.0, 1.0));
            }

            failures += Assert(sb, "2a. a year of maximal news stays under the stated ceiling (coverage cannot spiral)",
                peak < MediaCoverage.Ceiling(), string.Format(CultureInfo.InvariantCulture, "peak {0:F4} < ceiling {1:F4}", peak, MediaCoverage.Ceiling()));
            failures += Assert(sb, "2b. interest is bounded below 1 whatever the inputs",
                peakInterest < 1.0, string.Format(CultureInfo.InvariantCulture, "peak interest {0:F6}", peakInterest));

            // ---------- 3. the same message by outlet audience ----------
            // Two groups differing ONLY in what they care about: young-urban (climate 0.6, crime 0.1),
            // older-rural (climate 0.1, crime 0.6). Same match, same credibility, same spend.
            double[] shares = { 0.5, 0.5 };
            double[] climateSalience = { 0.6, 0.1 };
            double[] crimeSalience = { 0.1, 0.6 };
            double[] match = { 0.6, 0.6 };
            MediaOutlet[] outlets = MediaCatalog.Archetypes(2);
            MediaOutlet youngOutlet = outlets[2];   // tabloid: 75 % young-urban
            MediaOutlet olderOutlet = outlets[1];   // commercial television: 70 % older-rural
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            const double electorate = 1_000_000;

            CampaignActions.ChainTrace climateYoung = MediaSystem.ResolveThroughOutlet(tv, youngOutlet, electorate, shares, climateSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity);
            CampaignActions.ChainTrace climateOlder = MediaSystem.ResolveThroughOutlet(tv, olderOutlet, electorate, shares, climateSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity);
            CampaignActions.ChainTrace crimeYoung = MediaSystem.ResolveThroughOutlet(tv, youngOutlet, electorate, shares, crimeSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity);
            CampaignActions.ChainTrace crimeOlder = MediaSystem.ResolveThroughOutlet(tv, olderOutlet, electorate, shares, crimeSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity);

            // Compare per person reached, so the two outlets' different reach ceilings do not do the work.
            double climateRatio = (climateYoung.Persuasion / climateYoung.Reach) / (climateOlder.Persuasion / climateOlder.Reach);
            double crimeRatio = (crimeOlder.Persuasion / crimeOlder.Reach) / (crimeYoung.Persuasion / crimeYoung.Reach);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  the same television message by outlet (persuasion per person reached):\n" +
                "  climate: {0} {1:F5} vs {2} {3:F5} -> x{4:F2}\n  crime:   {2} {5:F5} vs {0} {6:F5} -> x{7:F2}\n",
                youngOutlet.Name, climateYoung.Persuasion / climateYoung.Reach, olderOutlet.Name, climateOlder.Persuasion / climateOlder.Reach, climateRatio,
                crimeOlder.Persuasion / crimeOlder.Reach, crimeYoung.Persuasion / crimeYoung.Reach, crimeRatio));
            failures += Assert(sb, "3a. a climate message performs materially better through the young-urban outlet (x1.5 or more per head)",
                climateRatio >= 1.5, string.Format(CultureInfo.InvariantCulture, "x{0:F2}", climateRatio));
            failures += Assert(sb, "3b. and a crime message the other way round - the audience, not the outlet, decides",
                crimeRatio >= 1.5, string.Format(CultureInfo.InvariantCulture, "x{0:F2}", crimeRatio));
            failures += Assert(sb, "3c. through the general-population outlet the two messages perform alike (the composition is the population)",
                Math.Abs(MediaSystem.ResolveThroughOutlet(tv, outlets[0], electorate, shares, climateSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity).Persuasion
                         - MediaSystem.ResolveThroughOutlet(tv, outlets[0], electorate, shares, crimeSalience, match, 0.7, tv.MoneyCost, StrategyModifiers.Identity).Persuasion) < 1e-9,
                "identical to 1e-9");

            // ---------- 4. interest is availability ----------
            // Eight parties: one with nothing, one after a day of news, one big by the polls.
            double[] interest = new double[8];
            double[] polled = { 0.30, 0.20, 0.19, 0.07, 0.07, 0.05, 0.05, 0.04 };
            for (int p = 0; p < 8; p++) { interest[p] = MediaSystem.Interest(0.0, 0.0, polled[p]); }
            List<InterviewBooking> quiet = MediaInterest.Bookings(outlets, interest);
            int smallQuiet = MediaInterest.SlotsFor(quiet, 7);
            int bigQuiet = MediaInterest.SlotsFor(quiet, 0);

            // The small party makes news: a policy announcement carried at full media interest (reach
            // 0.18 of the electorate) and a rally in the largest valkrets (0.127 x 0.06). D-20 (a): the
            // raw figure is each kind's fraction of the act's OWN reach, so the day's raw is
            // 0.5 x 0.18 + 0.3 x 0.0076 = 0.092 - the news reached 9 % of the electorate.
            var newsDay = new MediaCoverage(8);
            StrategyModifiers none = StrategyModifiers.Identity;
            newsDay.AddRaw(7, MediaSystem.RawNewsworthiness(CampaignActions.Spec(CampaignActionKind.PolicyAnnouncement), none, 1.0 * CampaignActions.Spec(CampaignActionKind.PolicyAnnouncement).ChannelReach)
                              + MediaSystem.RawNewsworthiness(CampaignActions.Spec(CampaignActionKind.Rally), none, 0.127 * CampaignActions.Spec(CampaignActionKind.Rally).ChannelReach));
            double[] gains = newsDay.CloseDay();
            double[] interestAfter = (double[])interest.Clone();
            interestAfter[7] = MediaSystem.Interest(newsDay.Coverage(7), MediaSystem.MomentumPpPerCoverage * gains[7], polled[7]);
            List<InterviewBooking> loud = MediaInterest.Bookings(outlets, interestAfter);
            int smallLoud = MediaInterest.SlotsFor(loud, 7);

            sb.Append("\n  bookings on a quiet day (interest from polled share alone):\n");
            for (int p = 0; p < 8; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    party {0} polled {1:P0} interest {2:F3} slots {3}\n", p, polled[p], interest[p], MediaInterest.SlotsFor(quiet, p)));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture, "  party 7 after a policy announcement and a rally: coverage {0:F3}, interest {1:F3}, slots {2}\n",
                newsDay.Coverage(7), interestAfter[7], smallLoud));

            failures += Assert(sb, "4a. a small party with no coverage is booked by NO outlet - it cannot buy its way onto the air",
                smallQuiet == 0, $"slots {smallQuiet} at interest {interest[7]:F3}");
            failures += Assert(sb, "4b. the same party after a day of news IS booked",
                smallLoud > 0, $"slots {smallLoud} at interest {interestAfter[7]:F3}");
            failures += Assert(sb, "4c. a bigger party is booked more on a quiet day (candidate popularity is a §13 input)",
                bigQuiet > smallQuiet, $"party 0 slots {bigQuiet}, party 7 slots {smallQuiet}");
            failures += Assert(sb, "4d. the interview's spec is untouched: 0 kr, no cap (the ruling - availability, never a fee)",
                CampaignActions.Spec(CampaignActionKind.Interview).MoneyCost == 0.0, "0 kr");

            int totalSlots = 0;
            foreach (MediaOutlet o in outlets) { totalSlots += o.DailySlots; }
            failures += Assert(sb, "4e. bookings never exceed the outlets' slots",
                quiet.Count <= totalSlots && loud.Count <= totalSlots, $"{quiet.Count} and {loud.Count} of {totalSlots}");

            // 4f. Fairness over time: with the ledger, a party owed a fraction of a slot a day is
            // booked in proportion over a month, not never.
            var ledger = new MediaInterest.BookingLedger(outlets.Length, 8);
            double[] steady = { 0.99, 0.99, 0.70, 0.45, 0.20, 0.10, 0.05, 0.02 };
            var booked = new int[8];
            int offered = 0;
            for (int d = 0; d < 30; d++)
            {
                List<InterviewBooking> today = ledger.Allocate(outlets, steady);
                offered += today.Count;
                foreach (InterviewBooking b in today) { booked[b.PartyIndex]++; }
            }

            sb.Append("\n  thirty days at steady interest (0.99 0.99 0.70 0.45 0.20 0.10 0.05 0.02): slots booked ");
            for (int p = 0; p < 8; p++) { sb.Append(booked[p]).Append(' '); }
            sb.Append($"of {offered}\n");
            failures += Assert(sb, "4f. the fourth most newsworthy party (interest 0.45) is booked in proportion over a month, not starved",
                booked[3] >= 10 && booked[3] < booked[2] && booked[2] < booked[0],
                $"party 3 booked {booked[3]} times; party 2 {booked[2]}; party 0 {booked[0]}");
            failures += Assert(sb, "4g. a party under every threshold is still never booked, whatever the ledger carries",
                booked[7] == 0 && booked[6] == 0, $"parties 6 and 7: {booked[6]}, {booked[7]}");

            // ---------- 5. coverage creates momentum, bounded ----------
            double maxShock = MediaSystem.MomentumPpPerCoverage * MediaSystem.SaturatedGain(1e9);
            failures += Assert(sb, "5. the momentum a day of news can create is bounded (MomentumPpPerCoverage x CoverageScale)",
                Math.Abs(maxShock - MediaSystem.MomentumPpPerCoverage * MediaSystem.CoverageScale) < 1e-6,
                string.Format(CultureInfo.InvariantCulture, "{0:F3} pp at most per day", maxShock));

            sb.Append($"\n=== MediaHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
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
