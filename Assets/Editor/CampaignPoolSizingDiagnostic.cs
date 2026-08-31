using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-D2 (W-F5's pool question) — **what pool an eight-party field needs to stay playable under a
    /// mandate-proportional split. It MEASURES and PROPOSES; it applies nothing.**
    ///
    /// <para><b>The finding this builds on</b> (`COMPLETED.md` §83, W-F5): Sweden's public party funding is
    /// paid per mandate — the *mandatbidrag* of lag (1972:625) — so a seat-proportional war chest is the
    /// SOURCED shape, not a modelling convenience. Run on it, the split clears both standing PEND lines
    /// (prof/est 0.306 → 0.430, est/grass 0.269 → 1.405) **and bankrupts five of eight parties**: KD 0 → 16
    /// unpaid staff-days and both television buys lost, L 0 → 36, MP 12 → 40, V 12 → 33. The separation is
    /// bankruptcy, not strategy, so W-F5 refused to bank it and reported the defect as being in the POOL
    /// rather than the split. ⚠ It also refused to raise the pool, because <b>picking a bigger number to
    /// turn assertions green is the one thing the standing rules forbid outright.</b></para>
    ///
    /// <para><b>What C-D2 adds is the number the refusal left open — DERIVED, not picked.</b> The route was
    /// named in the bill itself: <i>a DERIVED floor from the organisation's own bill</i>
    /// (`BudgetPlan.CommittedToOrganisation`, W-B12). A party is playable when it can pay its organisation
    /// to polling day; that is not an authored threshold, it is the definition W-B12 already built the
    /// campaign around, and the bankruptcies above are measured as failures of exactly it.</para>
    ///
    /// <para><b>Two independent methods, reported against each other.</b></para>
    /// <list type="number">
    /// <item><description><b>ANALYTIC.</b> Run every party with money not binding, and read what its
    /// organisation actually cost over the campaign (`StaffMoney + OfficeMoney`). Then the pool a
    /// mandate split needs is <c>max over parties of (bill / seat-share)</c> — set by whichever party has
    /// the worst bill-to-seats ratio, which is a fact about the roster, not a choice.</description></item>
    /// <item><description><b>MEASURED.</b> Bisect the pool on the criterion the failure itself used:
    /// <b>every party finishes with ZERO unpaid staff-days</b>. No arithmetic model at all — just the
    /// campaign, run.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>The two are reported side by side and the gap between them is the finding, not an
    /// error.</b> The analytic figure assumes the organisation's bill is independent of how much money a
    /// party has; if the measured floor is lower, parties economise, and if it is higher, spending on
    /// actions competes with payroll in a way the arithmetic cannot see.</para>
    ///
    /// <para>⚠ <b>NOTHING IS APPLIED.</b> This harness writes no constant. `CampaignAiHarness.WarChestPool`
    /// is untouched and the chests stay equal, exactly as W-F5 left them; the proposal is a line in
    /// `COMPLETED.md` for Elias to strike or bless.</para>
    /// </summary>
    public static class CampaignPoolSizingDiagnostic
    {
        private const int Seed = 777;

        /// <summary>Money large enough that no decision in the run can be money-bound — the control that
        /// makes the measured organisational bill the organisation's, not the budget's.</summary>
        private const double Unbound = 1e11;

        /// <summary>The television buy's price, read from the action catalogue rather than repeated here,
        /// so the "can it still afford one buy" floor cannot drift from what a buy costs.</summary>
        private static double TelevisionCost => CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-D2: what pool does a mandate-proportional eight-party field need? MEASURED, PROPOSED, NOTHING APPLIED ===\n");

            CampaignRun.Setup setup = CampaignAiHarness.BuildSetup(out _);
            int[] seats = CampaignAiHarness.Seats2022;
            int totalSeats = 0;
            foreach (int s in seats) { totalSeats += s; }

            // ---- 1. the organisation's bill, with money not binding ----
            CampaignRun.Result unbound = CampaignAiHarness.RunSeeded(WithChests(setup, Flat(setup.Parties.Length, Unbound)), Seed);

            var bills = new double[setup.Parties.Length];
            int boundAnyway = 0;
            sb.Append("\n    THE ORGANISATION'S OWN BILL, measured with money not binding\n");
            sb.Append("    party   seats   share      staff kr      offices kr        bill kr   bill/share (the pool it implies)\n");
            sb.Append("    --------------------------------------------------------------------------------------------------\n");

            double analytic = 0;
            string analyticSetBy = "";
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                CampaignRun.PartyLedger l = unbound.Parties[p];
                bills[p] = l.StaffMoney + l.OfficeMoney;
                double share = seats[p] / (double)totalSeats;
                double implied = bills[p] / share;

                if (l.UnpaidStaffDays > 0)
                {
                    boundAnyway++;
                    Debug.LogError($"C-D2: {l.Name} finished with {l.UnpaidStaffDays} unpaid staff-days on an UNBOUNDED chest. "
                                   + "The control run is supposed to measure what the organisation costs when money is not the "
                                   + "constraint; if money was still binding, the bill below is not the organisation's.");
                }

                if (implied > analytic) { analytic = implied; analyticSetBy = l.Name; }

                sb.Append(F("    {0,-6} {1,5}  {2,6:P2} {3,13:N0} {4,15:N0} {5,14:N0} {6,15:N0}\n",
                    l.Name, seats[p], share, l.StaffMoney, l.OfficeMoney, bills[p], implied));
            }

            sb.Append(F("\n    ANALYTIC FLOOR: {0:N0} kr, set by {1} - the party with the worst bill-to-seats ratio.\n", analytic, analyticSetBy));

            // ---- 2. the measured floor: bisect on "nobody goes unpaid" ----
            double lo = 0, hi = Math.Max(analytic * 4.0, 8 * 2_400_000.0 * 4.0);
            if (!AllPaid(setup, seats, totalSeats, hi))
            {
                Debug.LogError($"C-D2: even a pool of {hi:N0} kr leaves someone unpaid, so the bisection has no upper bound and "
                               + "the measured floor is NOT established. Reporting the analytic figure alone would be reporting "
                               + "half a measurement as a whole one.");
                sb.Append("    MEASURED FLOOR: ⚠ NOT ESTABLISHED - see the error above.\n");
                Finish(sb, 1);
                return;
            }

            for (int i = 0; i < 24; i++)
            {
                double mid = (lo + hi) / 2.0;
                if (AllPaid(setup, seats, totalSeats, mid)) { hi = mid; } else { lo = mid; }
            }

            double measured = hi;
            sb.Append(F("    MEASURED FLOOR: {0:N0} kr - the smallest pool at which all eight finish with ZERO unpaid staff-days\n", measured));
            sb.Append(F("                    (bisected in 24 steps; the criterion is W-B12's own, not a threshold invented here).\n"));

            // ---- 3. what that pool actually hands each party ----
            sb.Append("\n    WHAT THE MEASURED POOL HANDS EACH PARTY, and whether a television buy survives it\n");
            sb.Append("    party   seats        chest kr     less its bill        left kr    one buy?\n");
            sb.Append("    ---------------------------------------------------------------------------\n");
            double poolForOneBuy = 0;
            string buySetBy = "";
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double share = seats[p] / (double)totalSeats;
                double chest = measured * share;
                double left = chest - bills[p];
                double needed = (bills[p] + TelevisionCost) / share;
                if (needed > poolForOneBuy) { poolForOneBuy = needed; buySetBy = setup.Parties[p].Name; }

                sb.Append(F("    {0,-6} {1,5} {2,15:N0} {3,17:N0} {4,14:N0}    {5}\n",
                    setup.Parties[p].Name, seats[p], chest, bills[p], left, left >= TelevisionCost ? "yes" : "NO"));
            }

            double today = 8 * 2_400_000.0;
            sb.Append(F("\n    THE ANALYTIC ONE-BUY FLOOR: {0:N0} kr - where the worst-placed party ({1}) could pay its organisation\n", poolForOneBuy, buySetBy));
            sb.Append(F("    AND still afford ONE television buy at {0:N0} kr. {1}\n", TelevisionCost,
                poolForOneBuy <= measured
                    ? "⚠ It does NOT bind: the measured floor is already higher,\n    which is why every party shows 'yes' above."
                    : "⚠ It binds ABOVE the measured floor - paying the\n    organisation is not enough to campaign on television."));

            // ⚠ THE TENSION THE ITEM WAS ASKED TO STATE, as a number rather than a sentence: mandatbidrag
            // allocates by SEATS, and the campaign's costs are driven by the party's own organisation -
            // chiefly its office network, which is a personality choice. The two are uncorrelated, and the
            // spread below is exactly how uncorrelated.
            double worst = 0, best = double.MaxValue;
            string worstName = "", bestName = "";
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double implied = bills[p] / (seats[p] / (double)totalSeats);
                if (implied > worst) { worst = implied; worstName = setup.Parties[p].Name; }
                if (implied < best) { best = implied; bestName = setup.Parties[p].Name; }
            }

            sb.Append(F("\n    ⚠ THE TENSION, QUANTIFIED: the pool a party needs to cover its own organisation on its mandate share\n"));
            sb.Append(F("    spans {0:N0} kr ({1}) to {2:N0} kr ({3}) - a factor of {4:F1}. Public funding is allocated by SEATS;\n",
                best, bestName, worst, worstName, worst / Math.Max(1.0, best)));
            sb.Append("    the campaign's bill is driven by the party's OFFICE NETWORK, which is a personality choice. The two are\n");
            sb.Append("    uncorrelated, so ANY mandate-proportional split is set by whichever small party builds the most.\n");


            // ⚠ D-1 (c) — WHAT THE RULING ACTUALLY DOES, measured at the pool the game really has, on the
            // mandate split that produced the finding. The AI harness's parties run on EQUAL chests large
            // enough that the reserve never binds, so it reported no change at all; a ruling verified only
            // where it cannot bite is not verified. This is where it bites.
            sb.Append("\n    ⚠ D-1 (c) RULED: THE OFFICE PLAN IS SCALED TO WHAT A PARTY CAN AFFORD TO KEEP\n");
            sb.Append("    An office costs 100,000 kr to open and 2,000 kr/day to maintain plus its operation. `Open` only ever\n");
            sb.Append(F("    checked the OPENING cost, so a party bought every office it could and then STARVED them. The reserve\n"));
            sb.Append(F("    is the CAMPAIGN'S OWN LENGTH ({0} days), derived from the calendar rather than typed. It was FIRST\n",
                setup.Calendar.TotalCampaignDays));
            sb.Append("    written as CampaignAi.OfficeUpkeepDaysReserved (10 days) and measured: it dropped ZERO of 27 offices,\n");
            sb.Append("    because ten days of upkeep is small beside a 100,000 kr opening cost. Ten days is the right horizon\n");
            sb.Append("    for a TACTICAL office answering an attack; a PLAN is a commitment to election day.\n\n");
            sb.Append("    party   seats   mandate chest kr   offices planned   affordable   dropped\n");
            sb.Append("    ------------------------------------------------------------------------\n");

            int totalPlanned = 0, totalAffordable = 0;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double share = seats[p] / (double)totalSeats;
                double chest = today * share;
                double reserve = setup.Calendar.TotalCampaignDays
                    * (CampaignOffices.MaintenancePerDay + setup.Parties[p].OfficeOperationsPerDay);

                int planned = setup.Parties[p].Offices.Length;
                int affordable = 0;
                double left = chest;
                for (int o = 0; o < planned; o++)
                {
                    // The reserve is for the NETWORK the party would then hold, not for one office.
                    if (left - CampaignOffices.OpenCost < (affordable + 1) * reserve) { break; }
                    left -= CampaignOffices.OpenCost;
                    affordable++;
                }

                totalPlanned += planned;
                totalAffordable += affordable;
                sb.Append(F("    {0,-6} {1,5} {2,17:N0} {3,17} {4,12} {5,9}\n",
                    setup.Parties[p].Name, seats[p], chest, planned, affordable, planned - affordable));
            }

            sb.Append(F("\n    {0} offices planned across the eight, {1} affordable, {2} DROPPED rather than opened and starved.\n",
                totalPlanned, totalAffordable, totalPlanned - totalAffordable));
            sb.Append("    ⚠ An office a party cannot keep is WORSE than one it never opened: the opening cost is spent, and the\n");
            sb.Append("    office then recruits nothing, runs no operation, and bleeds influence every unpaid day. Dropping it\n");
            sb.Append("    returns that money to the campaign. ⚠ NO NEW FIGURE ENTERS THE MODEL - which is why (c) was the\n");
            sb.Append("    recommendation over (a), the only option that authors one.\n");
            sb.Append(F("\n    AGAINST TODAY'S POOL of {0:N0} kr (8 x 2 400 000, equal, [AUTHORED-DRAFT]):\n", today));
            sb.Append(F("        the MEASURED pay-the-organisation floor is x{0:F2}\n", measured / today));
            sb.Append(F("        the ANALYTIC floor (bill / share) is       x{0:F2}, i.e. the arithmetic UNDERSTATES the need by x{1:F2}\n",
                analytic / today, measured / analytic));

            sb.Append("\n    ⚠ NOTHING WAS APPLIED. CampaignAiHarness.WarChestPool is untouched and the chests stay EQUAL,\n");
            sb.Append("    exactly as W-F5 left them. The proposal is Elias's to strike or bless.\n");

            Finish(sb, boundAnyway);
        }

        private static void Finish(StringBuilder sb, int failures)
        {
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>Does every party finish with zero unpaid staff-days when this pool is split by mandate?</summary>
        private static bool AllPaid(CampaignRun.Setup setup, int[] seats, int totalSeats, double pool)
        {
            var chests = new double[setup.Parties.Length];
            for (int p = 0; p < chests.Length; p++) { chests[p] = pool * seats[p] / totalSeats; }

            CampaignRun.Result result = CampaignAiHarness.RunSeeded(WithChests(setup, chests), Seed);
            foreach (CampaignRun.PartyLedger l in result.Parties)
            {
                if (l.UnpaidStaffDays > 0) { return false; }
            }

            return true;
        }

        private static double[] Flat(int count, double value)
        {
            var v = new double[count];
            for (int i = 0; i < count; i++) { v[i] = value; }
            return v;
        }

        /// <summary>
        /// The same setup with different war chests. ⚠ `Setup` and `PartySetup` are readonly structs, so
        /// this REBUILDS them rather than mutating — which also means every other field is copied across
        /// explicitly, and a field added to either type will fail to compile here rather than being
        /// silently dropped from the sizing runs. That is the intended failure mode.
        /// </summary>
        private static CampaignRun.Setup WithChests(CampaignRun.Setup setup, double[] chests)
        {
            var parties = new CampaignRun.PartySetup[setup.Parties.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                CampaignRun.PartySetup s = setup.Parties[p];
                parties[p] = new CampaignRun.PartySetup(s.Name, s.Personality, s.Credibility, chests[p], s.TrueIssueMatch,
                    s.Volunteers, s.Candidate, s.Offices, s.OfficeOperationsPerDay, s.Staff, s.TelevisionBuys, s.Script);
            }

            return new CampaignRun.Setup(setup.Calendar, parties, setup.PriorShares, setup.LoyaltyPerParty,
                setup.Compatibility, setup.TrueSalience, setup.NationalAudience, setup.Regions,
                setup.PublicHouse, setup.PublicPollEveryDays, setup.InternalHouse, setup.ElectorateLoyalty,
                setup.Outlets, setup.DebateDays, setup.Scandals);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
