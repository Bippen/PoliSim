using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-C1's harness — §32's five personalities and §33's expected-value choice, proven on an
    /// AI-only Swedish campaign run through <see cref="CampaignRun"/>.
    ///
    /// The done-when, asserted:
    /// 1. **an AI-only campaign completes deterministically** — the same seed twice gives the same
    ///    decision digest and the same final shares, byte for byte; every party's money stays
    ///    non-negative (W-B2's refusal); the run covers every campaign day;
    /// 2. **the five personalities produce measurably different action mixes** — pairwise L1
    ///    distance between mix vectors, and each of §32's own claims checked as a claim. ⚠ **Met
    ///    for what the environment can distinguish, PENDING for the rest, and the pending lines
    ///    say why.** Asserted today: the chaotic and populist mixes differ from every other's; the
    ///    grassroots party buys the least broadcast; the establishment party buys the most
    ///    television; the professional buys the most polling and never acts blind; the populist
    ///    front-loads its money while the professional paces; the chaotic mix varies most from
    ///    seed to seed. PENDING (printed with their measurements, never forced by an affinity):
    ///    the professional / establishment / grassroots separation, the populist's rallies and the
    ///    grassroots party's door-knocking — because in W-B3's placeholder environment a free
    ///    national interview six times a day dominates every other action for every rational
    ///    personality (the finding W-B3 and W-E3 recorded, now measured a third way), and a local
    ///    action reaches a fraction of one region against a national one's whole electorate.
    ///    W-B9 (media interest) and W-B4/B11 (ground-game reach) unblock them and re-assert here;
    /// 3. **no AI accesses hidden state the player cannot buy** — structurally (reflection over
    ///    `AiView` finds no member that could hold a truth) and behaviourally (an AI with no poll
    ///    gets only BLIND estimates; the never-polling personality's every decision is logged blind).
    /// Plus the §42 bar carried over: shares still sum to 1, an idle campaign leaves them at the
    /// prior exactly, and the campaign moved them only through `CampaignPressure`.
    ///
    /// **The staging, and its data classes.** Parties, prior and loyalty are SOURCED (Sweden 2022
    /// and 2018, Valmyndigheten, W-A1's derivation); regions' audiences are SOURCED (the 29
    /// valkretsar's valid votes, 2022 - W-F1); salience is SOURCED (EB105 Spring 2026, Sweden's top five,
    /// the four that map onto §6's list: climate 26, crime 18, defence 17, education 16 — "threats
    /// to democracy" has no §6 issue and is billed); compatibility is DERIVED as the fixed point at
    /// which the persuaded shares equal the prior, so the run starts at rest on the real result.
    /// [AUTHORED-DRAFT]: issue-match flat 0.5 for every party (W-F2 sources positions per issue),
    /// credibility 0.6 flat (W-F6), war chest 2.4 m kr each and EQUAL by design so the mixes differ
    /// by personality alone (W-F5), the two polling houses from W-E4's ladder.
    /// </summary>
    public static class CampaignAiHarness
    {
        // Sweden 2022 and 2018 Riksdag shares, Valmyndigheten final counts, in the driver's party
        // order (S, SD, M, V, C, KD, MP, L). 2018 re-ordered from LoyaltyHarness's series.
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        private static readonly double[] Shares2022 = { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
        private static readonly double[] Shares2018 = { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 };

        private static readonly AiPersonality[] Assignment =
        {
            AiPersonality.Professional,   // S
            AiPersonality.Populist,       // SD
            AiPersonality.Establishment,  // M
            AiPersonality.Grassroots,     // V
            AiPersonality.Chaotic,        // C
            AiPersonality.Establishment,  // KD
            AiPersonality.Grassroots,     // MP
            AiPersonality.Professional,   // L
        };

        private const double CompatibilityCeiling = 70.0;   // DERIVED scaling anchor: the largest party's compatibility; the rest follow the fixed point
        private const double FlatIssueMatch = 0.5;          // [AUTHORED-DRAFT] W-F2 sources per-issue positions
        private const double FlatCredibility = 0.6;         // [AUTHORED-DRAFT] W-F6
        /// <summary>
        /// W-F5: the campaign's TOTAL money across the eight parties, unchanged from the equal
        /// staging (8 x 2 400 000). ⚠ **[AUTHORED-DRAFT]** — the scale is a playability figure, not
        /// a sourced one, and `DATA_BILL.md` carries the bill.
        ///
        /// **What changed at W-F5 is the DISTRIBUTION, not the total**, so that anything the
        /// re-measurement moves is attributable to inequality alone and not to the campaign having
        /// more or less money to spend.
        /// </summary>
        private const double WarChestPool = 8 * 2_400_000.0;

        /// <summary>
        /// W-F5: each party's chest, **proportional to its seats**.
        ///
        /// ⚠ **The SHAPE of this is SOURCED, and only the scale is authored.** Sweden's public party
        /// funding is allocated BY MANDATE in law — the *mandatbidrag* of lag (1972:625) om statligt
        /// stöd till politiska partier is paid per seat held — so a seat-proportional chest is not a
        /// modelling convenience, it is how the largest component of a Swedish party's money
        /// actually arrives. What is authored is the TOTAL (above) and the assumption that private
        /// income scales the same way, which is the part `DATA_BILL.md` bills.
        ///
        /// The spread this produces is real: **S 5.89 M kr against L 0.88 M — 6.7 to 1.**
        ///
        /// ⚠ **MEASURED AND NOT ADOPTED (W-F5, 2026-08-30).** Run on this staging, the seat-
        /// proportional split DOES clear C1's two standing PEND lines — prof/est 0.306 → **0.430**,
        /// est/grass 0.269 → **1.405** — and that looks like the answer the 2026-08-30 ruling was
        /// waiting for. **It is not.** The same run puts four other assertions into FAIL, and the
        /// reason is visible in the ledger: KD goes from **0 unpaid staff-days and both its planned
        /// television buys** to **16 unpaid days and none**; L from 0 and one buy to **36 and none**;
        /// MP 12 → 40; V 12 → 33. The grassroots party's day-to-day mix change falls to **0.000** —
        /// it stops acting at all.
        ///
        /// **The personalities separate because the small parties go BANKRUPT, not because they
        /// choose differently** — the harness's own annotation on those two lines reads
        /// `[holds early]`. That is a separation this project must not bank: it would clear a gate
        /// by breaking W-B12, whose whole result was four of five managed parties paying their
        /// organisation to polling day.
        ///
        /// **So the chests stay EQUAL and the finding is reported instead.** The real defect it
        /// exposes is in the POOL, not the split: 2 400 000 kr was calibrated as what one party
        /// needs, and W-B12 showed it barely covers a managed campaign — so any distribution that
        /// gives a party LESS than that cannot fund one. Making the pool bigger to survive the split
        /// would be inventing a larger authored number to turn assertions green, which is the one
        /// thing the standing rules forbid outright. **What it needs is the sourced figures
        /// Kammarkollegiet holds, or a DERIVED floor from the organisation's own bill (W-B12's
        /// `CommittedToOrganisation`) — both are billed, neither is guessed.**
        ///
        /// ⚠ Kammarkollegiet's register of declared party income EXISTS and is public
        /// (`kammarkollegiet.se/vara-tjanster/insyn-i-partiers-finansiering`), but its figures sit
        /// behind a JavaScript comparison tool backed by `api.kammarkollegiet.se/PartiinsynPublicService.svc`,
        /// which does not answer an ordinary request. That is the bill, and it is a better-specified
        /// one than "nothing on disk".
        /// </summary>
        private static double WarChestFor(int party)
        {
            int seats = Seats2022[party];
            int total = 0;
            foreach (int s in Seats2022) { total += s; }
            return WarChestPool * seats / total;
        }

        /// <summary>SOURCED - Valmyndigheten, the 2022 Riksdag result, in this harness's party order (S, SD, M, V, C, KD, MP, L).</summary>
        private static readonly int[] Seats2022 = { 107, 73, 68, 24, 24, 19, 18, 16 };

        private const double WarChest = 2_400_000.0;        // the equal figure, kept for the blind-view probe below
        private const int Volunteers = 800;                  // [AUTHORED-DRAFT] W-B11: 800 volunteers x 3 h a day = 2 400 volunteer-hours, equal for all by design (W-B4's offices grow them)
        private const double OfficeOperationsPerDay = 2_000.0;   // [AUTHORED-DRAFT] W-B4: what each staged office puts into its own daily ground operation (400 doors a day at 5 kr)

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            int pending = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-C1: AI parties (§32) and expected-value decisions (§33) ===\n");

            // ---------- 3a. structural: the AI's view cannot hold a truth ----------
            var forbidden = new[] { "truth", "true", "actual", "underlying", "real", "preference", "hidden" };
            var offenders = new StringBuilder();
            foreach (MemberInfo m in typeof(AiView).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                string lower = m.Name.ToLowerInvariant();
                foreach (string bad in forbidden)
                {
                    if (lower.Contains(bad) && m.Name != "GetType") { offenders.Append(m.Name).Append(' '); }
                }
            }

            failures += Assert(sb, "3a. AiView cannot carry a truth (no truth/actual/underlying/preference member)",
                offenders.Length == 0, offenders.Length == 0 ? "clean" : $"offenders: {offenders}");

            // The scoring entry points take the view and nothing that could smuggle a truth past it.
            var evaluate = typeof(CampaignAi).GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);
            var paramNames = new StringBuilder();
            bool onlyView = evaluate != null;
            if (evaluate != null)
            {
                foreach (ParameterInfo p in evaluate.GetParameters())
                {
                    paramNames.Append(p.ParameterType.Name).Append(' ');
                    if (p.ParameterType != typeof(AiView) && p.ParameterType != typeof(PersonalityProfile)
                        && p.ParameterType != typeof(ResourcePool) && p.ParameterType != typeof(double)) { onlyView = false; }
                }
            }

            failures += Assert(sb, "3b. CampaignAi.Evaluate takes the view, the personality, a resource pool and the reserve - nothing else",
                onlyView, paramNames.ToString().Trim());

            // ---------- staging ----------
            CampaignRun.Setup setup = BuildSetup(out string stagingNote);
            sb.Append(stagingNote);

            // ---------- 1. determinism ----------
            const int seed = 777;
            CampaignRun.Result first = RunSeeded(setup, seed);
            CampaignRun.Result second = RunSeeded(setup, seed);

            failures += Assert(sb, "1a. the same seed reproduces the decision digest exactly",
                first.Digest == second.Digest, $"{first.Digest} vs {second.Digest}");

            bool sharesIdentical = true;
            for (int i = 0; i < first.FinalShares.Length; i++)
            {
                if (first.FinalShares[i] != second.FinalShares[i]) { sharesIdentical = false; }
            }

            failures += Assert(sb, "1b. the same seed reproduces the final shares bit for bit", sharesIdentical, "8 of 8");
            failures += Assert(sb, "1c. the run covered every campaign day",
                first.DaysRun == setup.Calendar.TotalCampaignDays, $"{first.DaysRun} of {setup.Calendar.TotalCampaignDays} days, {first.PublicPolls} public polls");

            bool noneNegative = true;
            foreach (CampaignRun.PartyLedger l in first.Parties) { if (l.MoneyLeft < 0.0) { noneNegative = false; } }
            failures += Assert(sb, "1d. no party's money went negative (TrySpend refuses, never clamps)", noneNegative, "8 of 8");

            // ---------- the mixes, printed ----------
            sb.Append("\n  the campaign (seed 777): action counts per party, in TheEight's order\n");
            sb.Append("  party  personality     rally  town  door   tv   digi  soc   intv  pol | polls  blind  slots  cover  mom.pp   spent(kr)   left(kr)   persuasion   +pp\n");
            for (int p = 0; p < first.Parties.Length; p++)
            {
                CampaignRun.PartyLedger l = first.Parties[p];
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  {0,-5}  {1,-14} {2,5} {3,5} {4,5} {5,5} {6,5} {7,5} {8,5} {9,4} | {10,5} {11,6} {12,5} {13,6:F2} {14,7:F2} {15,10:N0} {16,10:N0} {17,12:N0} {18:+0.000;-0.000}\n",
                    l.Name, l.Personality, l.ActionCount[0], l.ActionCount[1], l.ActionCount[2], l.ActionCount[3],
                    l.ActionCount[4], l.ActionCount[5], l.ActionCount[6], l.ActionCount[7], l.PollsBought, l.BlindDecisions,
                    l.SlotsOffered, l.CoverageAtEnd, first.MomentumPpAtEnd[p],
                    l.MoneySpentOnActions + l.PollMoney, l.MoneyLeft, l.PersuasionDelivered,
                    100.0 * (first.FinalShares[p] - first.BaselineShares[p])));
            }

            // W-B9: interviews are bookings now - no party can give more than it was offered.
            bool withinSlots = true;
            int interviewSlot = CampaignAi.IndexOfAction(CampaignActionKind.Interview);
            foreach (CampaignRun.PartyLedger l in first.Parties) { if (l.ActionCount[interviewSlot] > l.SlotsOffered) { withinSlots = false; } }
            failures += Assert(sb, "1e. W-B9: no party gave more interviews than the outlets offered it (availability, not a price)",
                withinSlots, "8 of 8 within their bookings");

            // W-B7: the debates held, and what they did - coverage and momentum, never the preference.
            sb.Append("\n  debates (W-B7): ");
            foreach ((int dDay, int dA, int dB, double dMargin, double dCoverage, double dMomentum) in first.Debates)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "day {0}: {1} v {2}, margin {3:+0.0;-0.0}, coverage +{4:F2}, momentum +/-{5:F2} pp  ", dDay, first.Parties[dA].Name, first.Parties[dB].Name, dMargin, dCoverage, dMomentum));
            }

            sb.Append('\n');
            failures += Assert(sb, "1f. W-B7: the two scheduled debates were held between the parties leading the PUBLISHED poll, each shocking coverage and momentum",
                first.Debates.Count == 2 && first.Debates[0].CoverageShock > 0 && first.Debates[0].MomentumShockPp > 0,
                $"{first.Debates.Count} debates");

            // W-B8: the staged scandal broke, was answered, and the campaign went on - the credibility cost is
            // on the party's live figure and nowhere else.
            sb.Append("  scandals (W-B8): ");
            foreach ((int xDay, int xParty, ScandalResponse xResponse, ScandalOutcome xOutcome) in first.Scandals)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "day {0}: {1} - {2}, escalated {3}, momentum {4:+0.00;-0.00} pp, credibility cost {5:F3}, {6} days in the news  ",
                    xDay, first.Parties[xParty].Name, xResponse, xOutcome.Escalated, xOutcome.MomentumShockPp, xOutcome.CredibilityCost, xOutcome.DaysInTheNews));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture, "credibility at the end: S {0:F3}, SD {1:F3}\n", first.Parties[0].CredibilityAtEnd, first.Parties[1].CredibilityAtEnd));
            failures += Assert(sb, "1g. W-B8: the staged scandal broke on day 30, the party answered, lost credibility on its live figure only, and campaigned to the end",
                first.Scandals.Count == 1 && first.Parties[0].CredibilityAtEnd < FlatCredibility && first.Parties[1].CredibilityAtEnd == FlatCredibility && first.Parties[0].TotalActions > 0 && first.DaysRun == setup.Calendar.TotalCampaignDays,
                $"{first.Scandals.Count} scandal, S credibility {first.Parties[0].CredibilityAtEnd:F3} of {FlatCredibility:F2}");

            // W-B4: every party's staged offices opened, were paid for day by day, recruited, and their own
            // operations knocked doors in their regions - the ground game election day reads (W-B11 -> W-D1).
            {
                bool officesRan = true;
                var offices = new StringBuilder();
                for (int p = 0; p < first.Parties.Length; p++)
                {
                    CampaignRun.PartyLedger l = first.Parties[p];
                    // W-C2: the staged plan is no longer the whole network - a defence opens an office
                    // in a contested region, and the ledger counts those separately. An office opened
                    // late has not recruited to capacity by polling day, so the volunteers are bounded
                    // rather than equated.
                    int staged = setup.Parties[p].Offices.Length;
                    int total = staged + l.OfficesOpenedInReaction;
                    officesRan &= l.OfficesOpened == total && l.OfficeMoney > total * CampaignOffices.OpenCost && l.OfficeContacts > 0
                        && l.OfficeVolunteersAtEnd >= staged * CampaignOffices.VolunteerCapacity
                        && l.OfficeVolunteersAtEnd <= total * CampaignOffices.VolunteerCapacity;
                    officesRan &= first.Offices[p].Count == total;
                    offices.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} offices ({2} staged + {3} in reaction) {4:N0} kr {5:N0} doors {6} volunteers; ", l.Name, l.OfficesOpened, staged, l.OfficesOpenedInReaction, l.OfficeMoney, l.OfficeContacts, l.OfficeVolunteersAtEnd));
                }

                failures += Assert(sb, "1h. W-B4: every party opened its staged offices and any it opened in reaction (W-C2), paid for them through the campaign, recruited them toward capacity, and their operations knocked doors", officesRan, offices.ToString());
            }

            // W-B5: every party's staged hires stand, their payroll is on the ledger to the krona (a
            // salary a day each for every day the party could pay, the unpaid days counted - a party
            // that burns its war chest on offices and a front-loaded pace goes broke before polling
            // day, and the ledger says so rather than pretending), and the managed parties' plans
            // bought the television they were made for.
            {
                bool payrollHolds = true;
                var payroll = new StringBuilder();
                int tvSlot = CampaignAi.IndexOfAction(CampaignActionKind.TelevisionAd);
                for (int p = 0; p < first.Parties.Length; p++)
                {
                    CampaignRun.PartyLedger l = first.Parties[p];
                    int hires = setup.Parties[p].Staff.Length;
                    payrollHolds &= l.StaffHired == hires && first.Staff[p].Count == hires
                        && Math.Abs(l.StaffMoney - (hires * first.DaysRun - l.UnpaidStaffDays) * CampaignStaff.SalaryPerDay) < 1e-6;
                    if (setup.Parties[p].TelevisionBuys > 0) { payrollHolds &= l.ActionCount[tvSlot] >= setup.Parties[p].TelevisionBuys; }
                    payroll.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} hired {2:N0} kr ({3} unpaid staff-days), TV buys {4} of {5} planned; ",
                        l.Name, l.StaffHired, l.StaffMoney, l.UnpaidStaffDays, l.ActionCount[tvSlot], setup.Parties[p].TelevisionBuys));
                }

                failures += Assert(sb, "1i. W-B5: every party's hires stand, the payroll is on the ledger to the krona (unpaid days counted, not hidden), and the managed parties bought the television their plans were made for", payrollHolds, payroll.ToString());
            }

            // ---------- 2. five personalities, measurably different ----------
            var mixes = new double[5][];
            for (int p = 0; p < 5; p++) { mixes[p] = first.Parties[p].Mix(); }

            // The pairwise table, printed whole so the finding below can be read off it.
            sb.Append("\n  pairwise L1 distance between action mixes (0 = identical, 2 = disjoint):\n");
            var pairwise = new double[5, 5];
            for (int a = 0; a < 5; a++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0,-14}", Assignment[a]));
                for (int b = 0; b < 5; b++)
                {
                    pairwise[a, b] = L1(mixes[a], mixes[b]);
                    sb.Append(string.Format(CultureInfo.InvariantCulture, " {0,6:F3}", pairwise[a, b]));
                }

                sb.Append('\n');
            }

            failures += Assert(sb, "2a-i. the chaotic personality's mix differs from every other's (L1 >= 0.30)",
                MinAgainstOthers(pairwise, 4) >= 0.30, string.Format(CultureInfo.InvariantCulture, "min {0:F3}", MinAgainstOthers(pairwise, 4)));
            // Since W-B9 the two largest parties' days are both filled by the interviews the media
            // book them, so the populist and the professional converge on the media's schedule;
            // what would separate them is the populist's rallies (local, W-B4's reach) - PENDING.
            // W-B4 landed (2026-08-30): the populist's four staged offices give its rallies four full
            // regions, but the grassroots party's six give it more; the populist is 0.274 from its
            // nearest neighbour. Where a party sites its offices is the staged plan's - W-B5/W-C2.
            // CLEARED at W-B5 (2026-08-30): the populist's digital strategist and its manager's one
            // television buy separate it from every other personality (min 0.419).
            failures += Assert(sb, "2a-ii. the populist personality's mix differs from every other's (L1 >= 0.30) - PENDING W-B4 then W-B5, CLEARED at W-B5: its digital strategist and its manager's television buy",
                MinAgainstOthers(pairwise, 1) >= 0.30, string.Format(CultureInfo.InvariantCulture, "min {0:F3}", MinAgainstOthers(pairwise, 1)));

            // ⚠ The rational three collapse onto one strategy, and that is the ENVIRONMENT's fact,
            // not the AI's: with a free national interview available six times a day (W-B3's
            // recorded dominance, W-B9's mechanism absent) and local reach a fraction of a region's
            // electorate (W-B3's placeholder; W-B4/B11 make it volunteer-hours), every personality
            // that maximises expected value ends up interviewing all day. The separation is
            // PENDING on those items and re-asserted there - never forced here by an affinity.
            // W-B9 landed (2026-08-29) and the interview stopped being free six times a day for
            // everyone; the grassroots party now separates from both media personalities (door-
            // knocking, social), but the professional and the establishment converge on what the
            // media's fair bookings, the press's interest and an even spending pace leave them:
            // interviews, announcements, posts. What separates them is a budget plan (television,
            // W-B5) - still PENDING, and never an affinity chosen to pass this line.
            // W-B5 landed (2026-08-30): both rational personalities hire a manager with a television
            // plan on equal money and converge on the same schedule (0.061 apart); what would separate
            // them is unequal money (W-F5) or a plan that reacts (W-C2), never an affinity.
            pending += Pending(sb, "2a-iii. professional / establishment / grassroots separate (L1 >= 0.30) - grassroots separates since W-B9; professional / establishment PENDING W-C2 / W-F5 since W-B5 (two rational planners on equal money converge)",
                string.Format(CultureInfo.InvariantCulture, "prof/est {0:F3}, prof/grass {1:F3}, est/grass {2:F3}",
                    pairwise[0, 2], pairwise[0, 3], pairwise[2, 3]),
                Math.Min(pairwise[0, 2], Math.Min(pairwise[0, 3], pairwise[2, 3])) >= 0.30);
            // ⚠ Asserted at W-B9, back to PENDING at W-B11: the grassroots separation W-B9 produced
            // was the 2 %-of-a-region placeholder knocking 16 000 doors an afternoon. With doors
            // counted as the volunteers can actually knock them (W-B11), 3 000 doors at W-B3's
            // per-contact persuasion weight are not worth the hours against a post to a party's
            // whole following, and the grassroots party stops knocking. What remains is the
            // ground game's SCALE - offices and volunteers (W-B4) - and the persuasion a personal
            // contact is worth (calibration entry 10), never a weight raised to pass this line.
            // CLEARED at W-B4 (2026-08-30): with six offices the grassroots party's local audience is
            // six full regions, and its RALLIES (not its door-knocking - 2c still pends on entry 10)
            // separate it from both media personalities; the doors are knocked by the offices' own
            // operations, outside the action mix this line measures.
            // ⚠ CLEARED at W-B4, back to PENDING at W-C2 (2026-08-30). Reactivity puts the
            // ESTABLISHMENT on the ground: a broadcast party that sees an opponent's push into a
            // region it has no office in defends it with the local act its own affinities prefer (a
            // town hall), which is a thing it otherwise almost never does. Its mix moves toward the
            // grassroots party's and the pair closes to 0.291 against the 0.300 line - nine
            // thousandths, and NOT to be recovered by moving a threshold, a cooldown or an affinity
            // (the whole point of the line). What separates two parties that both react is what
            // they can afford to react WITH: unequal war chests and unequal paces (W-F5). Held as a
            // PEND with its measurement so the day it clears is visible.
            //
            // RULED 2026-08-30 (Elias, on the W-C2 review): the line STAYS at 0.291 and the
            // threshold is not to be nudged. Nine thousandths is precisely the distance at
            // which a threshold move stops being calibration and becomes making the test
            // pass. It waits on W-F5's unequal war chests, following this line's own
            // precedent from W-B11. AND: if W-F5 lands and this still reads 0.291, that is a
            // FINDING ABOUT THE MODEL - two parties that both react converging is something
            // to report and explain - not a rounding problem to be closed.
            pending += Pending(sb, "2a-iv. the grassroots personality's mix differs from both media personalities' (L1 >= 0.30) - CLEARED at W-B4, back to PENDING W-F5 at W-C2: reactivity puts the establishment on the ground in a contested region and closes est/grass to within 0.01 of the line",
                string.Format(CultureInfo.InvariantCulture, "prof/grass {0:F3}, est/grass {1:F3}", pairwise[0, 3], pairwise[2, 3]),
                pairwise[0, 3] >= 0.30 && pairwise[2, 3] >= 0.30);

            int rally = CampaignAi.IndexOfAction(CampaignActionKind.Rally);
            int town = CampaignAi.IndexOfAction(CampaignActionKind.TownHall);
            int door = CampaignAi.IndexOfAction(CampaignActionKind.DoorToDoor);
            int tv = CampaignAi.IndexOfAction(CampaignActionKind.TelevisionAd);
            int digi = CampaignAi.IndexOfAction(CampaignActionKind.DigitalAd);
            int social = CampaignAi.IndexOfAction(CampaignActionKind.SocialPost);
            int interview = CampaignAi.IndexOfAction(CampaignActionKind.Interview);

            pending += Pending(sb, "2b. §32 populist: the largest rally + social-post share of any personality - PENDING W-B5/W-C2 since W-B4 (a rally now draws on the party's organisation in the region; the staged plan gives the grassroots party more offices, so it rallies more)",
                Describe(mixes, rally, social), Leads(mixes, 1, rally, social));
            pending += Pending(sb, "2c. §32 grassroots: the largest door-to-door share of any personality - PENDING calibration entry 10 since W-B4 (the ground game's doors are knocked by the offices' own operations, outside the action mix; a door-to-door ACTION at 15 000 kr for 3 000 doors is still not worth its hours to any rational personality)",
                Describe(mixes, door), Leads(mixes, 3, door));

            double[] broadcast = new double[5];
            for (int p = 0; p < 5; p++)
            {
                broadcast[p] = first.Parties[p].MoneyByAction[tv] + first.Parties[p].MoneyByAction[digi];
            }

            // "Low advertising budget" is a claim about the grassroots party's OWN books - the share
            // of its spending that went to broadcast - and about the two media personalities it is
            // contrasted with; a populist that spends nothing on advertising because it prefers
            // social media is not a counter-example.
            var adShare = new double[5];
            for (int p = 0; p < 5; p++)
            {
                double spent = first.Parties[p].MoneySpentOnActions;
                adShare[p] = spent > 0 ? broadcast[p] / spent : 0.0;
            }

            // Advertising claims are PENDING on a budget plan (W-B5's campaign manager): with even
            // pacing and no plan, no party can afford a 500 000 kr television buy on the day, and
            // the only advertiser is whoever the media will not book. Recorded, not forced.
            // CLEARED at W-B5 (2026-08-30): with a manager's plan the professional and the establishment
            // advertise (30 %, 40 % of spend); the grassroots party, with a field organizer and no
            // plan, advertises nothing.
            failures += Assert(sb, "2d. §32 grassroots: a low advertising budget (broadcast at most a quarter of its spending, and below the professional's and the establishment's) - PENDING W-B5, CLEARED at W-B5: the others' managers plan television, the grassroots party plans none",
                adShare[3] <= 0.25 && adShare[3] < adShare[0] && adShare[3] < adShare[2],
                string.Format(CultureInfo.InvariantCulture, "ad share of spend: prof {0:P0}, pop {1:P0}, est {2:P0}, grass {3:P0}, chaos {4:P0}",
                    adShare[0], adShare[1], adShare[2], adShare[3], adShare[4]));

            // W-B5 landed (2026-08-30): television is bought by plan now, but the interview half of
            // this line is the media's - they book the newsworthy, and the populist (rallies, a
            // digital strategist, a television buy) makes more news than the establishment.
            pending += Pending(sb, "2e. §32 establishment: strong traditional media - the largest television + interview share of any personality - PENDING W-B9's media interest since W-B5 (the outlets book the newsworthy; the populist makes more news) - W-C2 / W-F5",
                Describe(mixes, tv, interview), Leads(mixes, 2, tv, interview));

            // CLEARED at W-B5 (2026-08-30): the establishment's manager plans two buys and buys them;
            // the count is the STAGED plan's (calibration entry 15) - what a budget plan is.
            failures += Assert(sb, "2e-ii. §32 establishment: buys the most television of any personality - PENDING W-B5, CLEARED at W-B5: its manager's plan holds two buys and it makes them (the count is the staged plan's)",
                LeadsCount(first, 2, tv),
                $"TV buys: prof {first.Parties[0].ActionCount[tv]}, pop {first.Parties[1].ActionCount[tv]}, est {first.Parties[2].ActionCount[tv]}, grass {first.Parties[3].ActionCount[tv]}, chaos {first.Parties[4].ActionCount[tv]}");

            failures += Assert(sb, "2h. §32 populist front-loads and the professional paces: the populist reaches 80 % of its war chest spent on an earlier day",
                first.Parties[1].DayEightyPercentSpent < first.Parties[0].DayEightyPercentSpent,
                $"80 % spent by day: pop {first.Parties[1].DayEightyPercentSpent}, prof {first.Parties[0].DayEightyPercentSpent}, est {first.Parties[2].DayEightyPercentSpent}, grass {first.Parties[3].DayEightyPercentSpent}, chaos {first.Parties[4].DayEightyPercentSpent} (of {first.DaysRun})");

            bool professionalPollsMost = true;
            for (int p = 0; p < 5; p++)
            {
                if (p != 0 && first.Parties[p].PollsBought >= first.Parties[0].PollsBought) { professionalPollsMost = false; }
            }

            failures += Assert(sb, "2f. §32 professional: buys the most polling and never acts blind",
                professionalPollsMost && first.Parties[0].BlindDecisions == 0,
                $"polls prof {first.Parties[0].PollsBought}, pop {first.Parties[1].PollsBought}, est {first.Parties[2].PollsBought}, " +
                $"grass {first.Parties[3].PollsBought}, chaos {first.Parties[4].PollsBought}; professional blind decisions {first.Parties[0].BlindDecisions}");

            // Chaotic: "inconsistent strategy" - the party whose mix changes most from one day to the
            // NEXT within a campaign (mean L1 between consecutive days' action mixes, days with
            // actions only). ⚠ The first form of this test compared mixes across seeds, which
            // measures how much a rational party's choices swing with its poll's sampling error -
            // a knife-edge between two near-equal actions flips whole campaigns - and is not what
            // §32's bullet means. Day-to-day inconsistency is.
            var variability = new double[5];
            for (int p = 0; p < 5; p++) { variability[p] = DayToDayVariability(first.Parties[p]); }

            bool chaoticMostVariable = true;
            for (int p = 0; p < 5; p++) { if (p != 4 && variability[p] >= variability[4]) { chaoticMostVariable = false; } }
            failures += Assert(sb, "2g. §32 chaotic: the most inconsistent strategy - the largest day-to-day change in its action mix (mean L1 between consecutive days)",
                chaoticMostVariable, string.Format(CultureInfo.InvariantCulture,
                    "prof {0:F3}, pop {1:F3}, est {2:F3}, grass {3:F3}, chaos {4:F3}",
                    variability[0], variability[1], variability[2], variability[3], variability[4]));

            // Across seeds, for the record only: how far each personality's campaign moves with the polls' sampling.
            var seeds = new[] { 1, 2, 3, 4, 5 };
            var mixBySeed = new double[seeds.Length][][];
            for (int s = 0; s < seeds.Length; s++)
            {
                CampaignRun.Result r = RunSeeded(setup, seeds[s]);
                mixBySeed[s] = new double[5][];
                for (int p = 0; p < 5; p++) { mixBySeed[s][p] = r.Parties[p].Mix(); }
            }

            sb.Append("  (for the record) mix change across seeds 1-5, mean L1 between consecutive seeds: ");
            for (int p = 0; p < 5; p++)
            {
                double sum = 0.0;
                for (int s = 1; s < seeds.Length; s++) { sum += L1(mixBySeed[s][p], mixBySeed[s - 1][p]); }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1:F3}  ", Assignment[p], sum / (seeds.Length - 1)));
            }

            sb.Append('\n');

            // ---------- 3c. behavioural: no poll, no estimate ----------
            AiView blind = new AiView(0, CampaignPhase.Campaign, 30, new ResourcePool(WarChest, CampaignEconomy.HoursPerCampaignDay, 0),
                spendingReserve: WarChest, hasPoll: false, latestPoll: default, momentumPp: new double[8], issues: new IssueMeasurement[IssueVector.IssueCount],
                ownCredibility: FlatCredibility, nationalAudience: setup.NationalAudience, regions: setup.Regions, daysSinceOwnPoll: -1);
            List<ScoredCandidate> blindCandidates = CampaignAi.Evaluate(blind, PersonalityCatalog.Profile(AiPersonality.Chaotic), blind.Resources, WarChest);
            bool allBlind = blindCandidates.Count > 0;
            foreach (ScoredCandidate c in blindCandidates) { if (c.Measured) { allBlind = false; } }
            failures += Assert(sb, "3c. with no poll bought, every candidate estimate is BLIND - there is no measured estimate to act on",
                allBlind, $"{blindCandidates.Count} candidates, all unmeasured");

            failures += Assert(sb, "3d. the never-polling personality (chaotic) made every decision blind",
                first.Parties[4].BlindDecisions == first.Parties[4].TotalActions && first.Parties[4].PollsBought == 0,
                $"{first.Parties[4].BlindDecisions} blind of {first.Parties[4].TotalActions}, polls {first.Parties[4].PollsBought}");

            // ---------- the §42 bar, carried over ----------
            failures += Assert(sb, "4a. final shares sum to 1 (the campaign redistributed, it did not create votes)",
                Math.Abs(Sum(first.FinalShares) - 1.0) < 1e-9, $"sum {Sum(first.FinalShares):F12}");

            bool baselineIsPrior = true;
            double[] prior = Normalised(Shares2022);
            for (int i = 0; i < prior.Length; i++) { if (Math.Abs(first.BaselineShares[i] - prior[i]) > 1e-9) { baselineIsPrior = false; } }
            failures += Assert(sb, "4b. with no campaign the shares ARE the 2022 result (the derived compatibility is the fixed point)",
                baselineIsPrior, "8 of 8 within 1e-9");

            bool moved = false;
            for (int i = 0; i < prior.Length; i++) { if (Math.Abs(first.FinalShares[i] - first.BaselineShares[i]) > 1e-9) { moved = true; } }
            failures += Assert(sb, "4c. the campaign moved the shares (through CampaignPressure and nothing else)", moved, "yes");

            // ---------- findings, reported not asserted ----------
            sb.Append("\n  findings (measured, not asserted):\n");
            double bestPerKrona = 0.0;
            string bestPersonality = "";
            for (int p = 0; p < 5; p++)
            {
                CampaignRun.PartyLedger l = first.Parties[p];
                double spent = l.MoneySpentOnActions + l.PollMoney;
                double perKrona = spent > 0 ? l.PersuasionDelivered / spent : 0.0;
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  - {0,-14} persuasion per krona {1:E2}; interviews {2} of {3} actions ({4:P0})\n",
                    l.Personality, perKrona, l.ActionCount[interview], l.TotalActions,
                    l.TotalActions > 0 ? (double)l.ActionCount[interview] / l.TotalActions : 0.0));
                if (perKrona > bestPerKrona) { bestPerKrona = perKrona; bestPersonality = l.Personality.ToString(); }
            }

            sb.Append($"  - most persuasion per krona: {bestPersonality}\n");
            sb.Append("  - the free interview's dominance (W-B3, W-E3) is visible in every mix above; it stays W-B9's, as a mechanism\n");

            // The saturation finding: at the REAL national audience the chain's linear reach turns
            // a campaign's persuasion into hundreds of compatibility points, and ElectionScales
            // clamps every party at 100 - the final-share column is the clamp's arithmetic.
            sb.Append("  - compatibility bonus delivered (persuasion / PersuasionPerCompatibilityPoint), clamped at 100 by ElectionScales:\n    ");
            for (int p = 0; p < first.Parties.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} +{1:F0}  ", first.Parties[p].Name,
                    first.Parties[p].PersuasionDelivered / CampaignPressure.PersuasionPerCompatibilityPoint));
            }

            sb.Append("\n    -> every party saturates; the +pp column above is the clamp's arithmetic, not the campaign's difference.\n" +
                      "       W-B3 measured +0.19 pp for a hard week at a 100 000 audience; at 6.5 million the same chain is 65x that.\n" +
                      "       Reach is linear in audience and in repetition (the same electorate reached six times a day) - a MECHANISM\n" +
                      "       question (bounded reach, repeated-exposure decay; W-B9's media interest) before it is a calibration one.\n");

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n=== CampaignAiHarness: {0}; {1} PENDING on W-F5 (unequal war chests and unequal paces - W-C2 gave every personality a reaction, and what now separates two parties that both react is what they can afford to react WITH) and calibration entry 10 (persuasion per personal contact) - printed with their measurements, not counted as passes ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED", pending));
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ---------- staging ----------

        internal static CampaignRun.Result RunSeeded(CampaignRun.Setup setup, int seed)
        {
            SimulationRandom.Seed(seed);
            return CampaignRun.Simulate(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
        }

        /// <summary>[AUTHORED-DRAFT] W-B7: a candidate per personality - §16's attributes as the personality's own emphasis, no names (W-F6 labels real leaders). Game fiction, equal in sum by design.</summary>
        /// <summary>
        /// [AUTHORED-DRAFT] W-B4: each personality's office plan, as §32 describes its ground game -
        /// the grassroots party six offices, the populist four (its rallies are local), the
        /// professional three, the establishment two, the chaotic one - each in the largest
        /// valkretsar by electorate. Where a party SHOULD site them (its swing regions, W-E2) is
        /// W-B5's plan and W-C2's reactivity; today the plan is staged, and the harness says so.
        /// </summary>
        private static int[] OfficesFor(AiPersonality personality, RegionAudience[] regions)
        {
            int count;
            switch (personality)
            {
                case AiPersonality.Grassroots: count = 6; break;
                case AiPersonality.Populist: count = 4; break;
                case AiPersonality.Professional: count = 3; break;
                case AiPersonality.Establishment: count = 2; break;
                default: count = 1; break;
            }

            var order = new List<int>();
            for (int r = 0; r < regions.Length; r++) { order.Add(r); }
            order.Sort((a, b) => regions[b].Audience.CompareTo(regions[a].Audience));
            return order.GetRange(0, count).ToArray();
        }

        private static CandidateProfile CandidateFor(AiPersonality personality, string party)
        {
            switch (personality)
            {
                //                                        charisma debate comm cred integ knowledge campaign popularity scandal
                case AiPersonality.Populist: return new CandidateProfile(party, 85, 80, 75, 50, 55, 45, 65, 70, 55);
                case AiPersonality.Professional: return new CandidateProfile(party, 65, 70, 70, 70, 70, 70, 75, 60, 70);
                case AiPersonality.Establishment: return new CandidateProfile(party, 55, 65, 65, 80, 75, 80, 60, 60, 75);
                case AiPersonality.Grassroots: return new CandidateProfile(party, 70, 60, 65, 80, 85, 60, 60, 55, 70);
                default: return new CandidateProfile(party, 75, 75, 60, 45, 45, 50, 55, 65, 40);
            }
        }

        /// <summary>
        /// [AUTHORED-DRAFT] W-B5: each personality's hires, as §32 describes it - the professional a
        /// manager and a pollster; the populist a manager and a digital strategist; the establishment
        /// a manager and a media advisor; the grassroots party a field organizer; the chaotic nobody.
        /// The manager's plan: television buys the establishment 2, the professional and the populist
        /// 1 - the "budget plan" W-B9 found a greedy AI cannot improvise.
        /// </summary>
        private static StaffRole[] StaffFor(AiPersonality personality)
        {
            switch (personality)
            {
                case AiPersonality.Professional: return new[] { StaffRole.CampaignManager, StaffRole.Pollster };
                case AiPersonality.Populist: return new[] { StaffRole.CampaignManager, StaffRole.DigitalStrategist };
                case AiPersonality.Establishment: return new[] { StaffRole.CampaignManager, StaffRole.MediaAdvisor };
                case AiPersonality.Grassroots: return new[] { StaffRole.FieldOrganizer };
                default: return new StaffRole[0];
            }
        }

        private static int TelevisionBuysFor(AiPersonality personality)
        {
            switch (personality)
            {
                case AiPersonality.Establishment: return 2;
                case AiPersonality.Professional: return 1;
                case AiPersonality.Populist: return 1;
                default: return 0;
            }
        }

        internal static CampaignRun.Setup BuildSetup(out string note)
        {
            var sb = new StringBuilder();

            double[] prior = Normalised(Shares2022);
            double[] loyalty = LoyaltyModel.PartyLoyalties(Shares2022, Shares2018);

            // DERIVED: compatibility at the fixed point where PersuadedShares == prior, so an idle
            // campaign reproduces the 2022 result exactly. c_i = ceiling * (prior_i / max prior)^(1/Sharpness).
            double maxPrior = 0.0;
            foreach (double p in prior) { if (p > maxPrior) { maxPrior = p; } }
            var compatibility = new double[prior.Length];
            for (int i = 0; i < prior.Length; i++)
            {
                compatibility[i] = CompatibilityCeiling * Math.Pow(prior[i] / maxPrior, 1.0 / PreferenceModel.Sharpness);
            }

            // SOURCED salience: EB105 Spring 2026, Sweden - the four top-five issues §6 has a slot for.
            var salience = new double[IssueVector.IssueCount];
            for (int i = 0; i < salience.Length; i++) { salience[i] = double.NaN; }
            salience[(int)IssueId.Climate] = 0.26;
            salience[(int)IssueId.Crime] = 0.18;
            salience[(int)IssueId.Defense] = 0.17;
            salience[(int)IssueId.Education] = 0.16;

            // SOURCED regions: the 29 valkretsar's valid votes, 2022 (W-F1).
            RegionAudience[] regions = ReadValkretsar(out double national);

            var parties = new CampaignRun.PartySetup[Parties.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                parties[p] = new CampaignRun.PartySetup(Parties[p], Assignment[p], FlatCredibility, WarChest, match, Volunteers, CandidateFor(Assignment[p], Parties[p]),
                    OfficesFor(Assignment[p], regions), OfficeOperationsPerDay, StaffFor(Assignment[p]), TelevisionBuysFor(Assignment[p]));
            }

            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[Parties.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[Parties.Length], isInternal: true);

            sb.Append("\n  staging: 8 parties on Sweden 2022 (SOURCED prior), loyalty derived from 2018->2022 (W-A1):\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}  ", Parties[p], loyalty[p], compatibility[p]));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} valkretsar (SOURCED 2022 valid votes, W-F1), national audience {1:N0}; salience EB105 SE: climate .26 crime .18 defence .17 education .16\n" +
                "    [AUTHORED-DRAFT] issue-match {2:F2} flat, credibility {3:F2} flat, war chest {4:N0} kr each - EQUAL, and W-F5 measured why " +
                "(a seat-proportional split starves the small parties before it separates the personalities; see WarChestFor); houses from W-E4's ladder\n",
                regions.Length, national, FlatIssueMatch, FlatCredibility, WarChest));

            note = sb.ToString();
            // W-B6: the electorate as one group at W-A1's size-weighted mean loyalty (a public
            // derivation from past returns), until W-F4's voter groups give the strategies their
            // per-group targets.
            double electorateLoyalty = LoyaltyModel.WeightedMeanLoyalty(loyalty, prior);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    strategies (W-B6): prof SwingVoter, pop Populist, est BroadAppeal, grass BaseMobilization, chaos NegativeCampaign; electorate loyalty {0:F1} (one group, W-A1 weighted mean)\n",
                electorateLoyalty));

            // W-B8: one staged scandal - a MAJOR corruption story breaks for the leading party (S) on day 30 with
            // middling evidence; the AI responds by personality on the evidence as it sees it. [AUTHORED-DRAFT] staging.
            var scandals = new[] { (30, 0, new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, 0.5)) };

            return new CampaignRun.Setup(CampaignCalendar.Sweden2026, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, 7, internalHouse, electorateLoyalty, null, null, scandals);
        }

        private static RegionAudience[] ReadValkretsar(out double national)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2022.csv"));
            var regions = new List<RegionAudience>();
            national = 0.0;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                double valid = double.Parse(cells[1], CultureInfo.InvariantCulture);
                regions.Add(new RegionAudience(cells[0], valid));
                national += valid;
            }

            if (regions.Count != 29) { throw new InvalidDataException($"expected 29 valkretsar, read {regions.Count} from {path}"); }
            return regions.ToArray();
        }

        // ---------- helpers ----------

        private static bool Leads(double[][] mixes, int party, params int[] slots)
        {
            double own = Share(mixes[party], slots);
            for (int p = 0; p < mixes.Length; p++)
            {
                if (p != party && Share(mixes[p], slots) >= own) { return false; }
            }

            return own > 0.0;
        }

        private static string Describe(double[][] mixes, params int[] slots)
        {
            var sb = new StringBuilder();
            string[] names = { "prof", "pop", "est", "grass", "chaos" };
            for (int p = 0; p < mixes.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1:P0}  ", names[p], Share(mixes[p], slots)));
            }

            return sb.ToString().Trim();
        }

        private static double Share(double[] mix, int[] slots)
        {
            double s = 0.0;
            foreach (int slot in slots) { s += mix[slot]; }
            return s;
        }

        private static double DayToDayVariability(CampaignRun.PartyLedger ledger)
        {
            double sum = 0.0;
            int pairs = 0;
            double[] previous = null;
            foreach (int[] counts in ledger.DailyActionCount)
            {
                int total = 0;
                foreach (int c in counts) { total += c; }
                if (total == 0) { continue; }

                var mix = new double[counts.Length];
                for (int i = 0; i < mix.Length; i++) { mix[i] = (double)counts[i] / total; }
                if (previous != null) { sum += L1(mix, previous); pairs++; }
                previous = mix;
            }

            return pairs > 0 ? sum / pairs : 0.0;
        }

        private static double L1(double[] a, double[] b)
        {
            double d = 0.0;
            for (int i = 0; i < a.Length; i++) { d += Math.Abs(a[i] - b[i]); }
            return d;
        }

        private static double Sum(double[] v)
        {
            double s = 0.0;
            foreach (double x in v) { s += x; }
            return s;
        }

        private static double[] Normalised(double[] v)
        {
            double sum = Sum(v);
            var r = new double[v.Length];
            for (int i = 0; i < r.Length; i++) { r[i] = v[i] / sum; }
            return r;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }

        /// <summary>
        /// A claim the current environment cannot support, printed with its measurement and the
        /// item that unblocks it. Counted separately, never as a pass; when it already holds it
        /// says so ("holds early") so the day it can become an assertion is visible in the log.
        /// </summary>
        private static int Pending(StringBuilder sb, string label, string detail, bool holdsAlready)
        {
            sb.Append($"  PEND {label}: {detail}{(holdsAlready ? " [holds early]" : "")}\n");
            return 1;
        }

        private static double MinAgainstOthers(double[,] pairwise, int party)
        {
            double min = double.MaxValue;
            for (int p = 0; p < 5; p++)
            {
                if (p != party && pairwise[party, p] < min) { min = pairwise[party, p]; }
            }

            return min;
        }

        private static bool LeadsCount(CampaignRun.Result result, int party, int slot)
        {
            int own = result.Parties[party].ActionCount[slot];
            for (int p = 0; p < 5; p++)
            {
                if (p != party && result.Parties[p].ActionCount[slot] >= own) { return false; }
            }

            return own > 0;
        }
    }
}
