using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;

namespace PoliSim.Elections
{
    public static partial class LiveCampaignSetup
    {
        /// <summary>
        /// PL-C (Elias's rulings K167-K169 of 2026-10-27): <b>POLAND'S CAMPAIGN</b> - its own staging, not a copy of another country's. The calendar's polling
        /// day says which contest it is (`PolishCampaign.ContestOn`): the Sejm's, a presidential first vote, or (with <paramref name="played"/> holding the
        /// first vote) its run-off, which <see cref="PolandRunOff"/> stages.
        /// <list type="bullet">
        /// <item><description><b>The vote model</b>: every party on the Polish vote model the prediction is made of (`NationalElection.TryCompatibility` on the
        /// history rolled forward to the polling day, mapped by key), so an idle campaign reproduces the day's prediction and a campaign moves it - the Sejm's
        /// count reads the run's final shares, a first vote reads them as its poll (`PresidentialElection.HoldRounds`).</description></item>
        /// <item><description><b>The regions</b> (K169): the Sejm on the 41 okregi (the PKW's 2023 valid list votes the audience, its electorates the
        /// eligible), each list where it stands (`PolishCampaign.SejmStands`: the MN in okreg 21 alone); the presidency on the 16 wojewodztwa, every
        /// candidate's party everywhere (DECLARED: the nomination's signature gate is read at the count, not by the staging).</description></item>
        /// <item><description><b>The purse</b> (K167): each committee's chest IS its statutory limit (`PolishCampaign.SejmLimit` / `PresidentialLimit`),
        /// re-denominated as K110 re-denominates the USA's - one unit of the model's price table is the full committee's limit over the model's chest
        /// (<see cref="CampaignRun.Setup.PurseUnit"/>), so a committee standing in every okreg holds the model's chest and a list standing in fewer holds its
        /// mandates' share; every act's price is the model's times that ratio. No Polish act adds money (no postal tariff is on disk, so a run-up mailing is
        /// refused; the campaign proper raises none), so what a committee spends stops at its limit; a player's chest brought from the run-up is capped at
        /// it. [AUTHORED-DRAFT] the relative prices (the model's one table).</description></item>
        /// <item><description><b>The rest</b>: the casts by the CHES rule (§695); the salience the Polish seed (`PolishCampaign.Salience`, EB105); the AI's offices
        /// by audience where it stands; the Sejm's count reads the doors at [CP20]'s turnout CACE (`GotvTurnout.EuropeanDoorCacePp`, K7) on the okregi's
        /// electorates and a regional poll can be bought (the okregi's offsets on the count's own swing); the presidential count is national, so its staging
        /// reads no door turnout and offers no regional poll (DECLARED).</description></item>
        /// </list>
        /// False, with the reason, where the vote model, the calendar or the statute's figures cannot be read.
        /// </summary>
        public static bool TryPoland((int Day, int Party, Scandal Scandal)[] scandals, CampaignCalendar? calendar, out CampaignRun.Setup setup, out string note,
            int playerParty = -1, Func<int, AiDecision[]> playerScript = null, PreCampaignRun.Outcome? playerOutcome = null,
            Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0, Country played = null)
        {
            setup = default;
            CampaignCalendar cal;
            if (calendar.HasValue) { cal = calendar.Value; }
            else
            {
                DateTime start = WorldClock.StartDate(CountryId.Poland);
                if (!WorldClock.TryNextPollingDay(CountryId.Poland, start, out DateTime polling)) { note = "no campaign is staged for Poland: no polling day follows its start"; return false; }
                cal = CampaignCalendar.FromWorldStart(polling, start);
            }
            CampaignContest contest = PolishCampaign.ContestOn(cal.ElectionDate, played, out PresidentialElection.Contest pending);
            if (contest == CampaignContest.PresidentialRunOff)
            {
                return PolandRunOff(pending, scandals, cal, out setup, out note, playerParty, playerScript, playerScandalScript, liveScandalRate, played);
            }
            if (!NationalElection.TryCompatibility(CountryId.Poland, out string[] modelKeys, out double[] modelCompatibility, out double[] modelPrior, out double[] modelLoyalty, played, cal.ElectionDate))
            {
                note = "no campaign is staged for Poland on the vote model: it has no fitted electorate or no two-election history";
                return false;
            }
            bool sejm = contest == CampaignContest.Chamber;
            string[] cast = Keys(CountryId.Poland);
            var compatibility = new double[cast.Length];
            var prior = new double[cast.Length];
            var loyalty = new double[cast.Length];
            for (int p = 0; p < cast.Length; p++)
            {
                int k = Array.IndexOf(modelKeys, cast[p]);   // mapped by key, never by position
                compatibility[p] = k >= 0 ? modelCompatibility[k] : 0.0;
                prior[p] = k >= 0 ? modelPrior[k] : 0.0;
                loyalty[p] = k >= 0 ? modelLoyalty[k] : NationalElection.UnplacedLoyalty;
            }
            double priorSum = 0.0;
            foreach (double v in prior) { priorSum += v; }
            if (!(priorSum > 0.0)) { note = "no campaign is staged for Poland: the prior carries no share"; return false; }

            double national;
            int presidentialYear = 0;
            RegionAudience[] regions;
            if (sejm) { regions = RegionLayer.Of(CountryId.Poland, cal.CampaignStart, out string _); national = 0.0; foreach (RegionAudience r in regions) { national += r.Audience; } }
            else { regions = PolishCampaign.Wojewodztwa(cal.ElectionDate, out national, out presidentialYear); }
            bool[][] stands;
            if (sejm) { stands = PolishCampaign.SejmStands(cast, prior, regions.Length); }
            else
            {
                stands = new bool[cast.Length][];
                for (int p = 0; p < cast.Length; p++) { stands[p] = new bool[regions.Length]; for (int r = 0; r < regions.Length; r++) { stands[p][r] = prior[p] > 0.0; } }
            }

            // K167: each committee's limit, and the purse's unit - the full committee's limit over the model's chest
            string limitBasis;
            double full = sejm ? PolishCampaign.SejmLimit(cal.CampaignStart, cal.ElectionDate, PolishCampaign.SejmSeats, out limitBasis)
                               : PolishCampaign.PresidentialLimit(cal.CampaignStart, cal.ElectionDate, out limitBasis);
            if (double.IsNaN(full) || !(full > 0.0)) { note = "no campaign is staged for Poland: the statute's spending limit cannot be read - " + limitBasis; return false; }
            double unit = full / WarChest;
            var chest = new double[cast.Length];
            var mandates = new int[cast.Length];
            for (int p = 0; p < cast.Length; p++)
            {
                bool anywhere = false;
                foreach (bool s in stands[p]) { anywhere |= s; }
                if (sejm)
                {
                    mandates[p] = PolishCampaign.MandatesWhere(stands[p]);
                    chest[p] = PolishCampaign.SejmLimit(cal.CampaignStart, cal.ElectionDate, mandates[p], out string _) / unit;   // the committee's own limit, in the purse's units
                }
                else { chest[p] = anywhere ? WarChest : 0.0; }
            }

            double[] salience = PolishCampaign.Salience(cal.CampaignStart, out string salienceLabel);
            var parties = new CampaignRun.PartySetup[cast.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = PersonalityOf(CountryId.Poland, cast[p]);
                PreCampaignRun.Outcome? brought = p == playerParty ? playerOutcome : null;
                bool stands0 = chest[p] > 0.0;
                parties[p] = new CampaignRun.PartySetup(cast[p], personality, FlatCredibility,
                    brought.HasValue ? Math.Min(brought.Value.Money, chest[p]) : chest[p], match,   // K167: a chest brought from the run-up never past the limit
                    brought.HasValue ? brought.Value.Volunteers : stands0 ? Volunteers : 0,
                    CandidateFor(personality, cast[p]), brought.HasValue ? brought.Value.Offices : stands0 ? OfficesFor(personality, regions, stands[p]) : new int[0], OfficeOperationsPerDay,
                    brought.HasValue ? brought.Value.Staff : stands0 ? StaffFor(personality) : new StaffRole[0], brought.HasValue ? brought.Value.TelevisionBuys : stands0 ? TelevisionBuysFor(personality) : 0,
                    p == playerParty ? playerScript : null, p == playerParty ? playerScandalScript : null);
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[cast.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[cast.Length], isInternal: true);
            var sb = new StringBuilder();
            sb.Append("\n  staging: " + cast.Length.ToString(CultureInfo.InvariantCulture) + " parties on Poland's vote model for " + (sejm ? "the Sejm" : "the presidential first vote")
                + " of " + cal.ElectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " (compatibility, prior and loyalty as the prediction reads them):\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                int where = 0;
                foreach (bool s in stands[p]) { if (s) { where++; } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}/{3}R  ", cast[p], loyalty[p], compatibility[p], where));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} {1} (SOURCED {2}), national audience {3:N0}; salience {4}\n    K167: the purse is the committee's statutory limit - {5}: {6:N0} zł for a full committee, one unit of the model's table {7:N4} zł",
                regions.Length, sejm ? "okregi" : "wojewodztwa", sejm ? "the PKW's 2023 valid list votes and electorates" : "the PKW's " + presidentialYear.ToString(CultureInfo.InvariantCulture) + " first-round valid votes and electorates",
                national, salienceLabel, limitBasis, full, unit));
            if (sejm)
            {
                for (int p = 0; p < cast.Length; p++) { if (mandates[p] > 0 && mandates[p] < PolishCampaign.SejmSeats) { sb.Append(string.Format(CultureInfo.InvariantCulture, "; {0} on {1} seats, {2:N0} zł", cast[p], mandates[p], chest[p] * unit)); } }
            }
            sb.Append("\n    [AUTHORED-DRAFT] issue-match " + FlatIssueMatch.ToString("F2", CultureInfo.InvariantCulture) + " flat, credibility " + FlatCredibility.ToString("F2", CultureInfo.InvariantCulture)
                + " flat, the relative prices the model's one table\n");
            note = sb.ToString();
            setup = new CampaignRun.Setup(cal, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, LoyaltyModel.WeightedMeanLoyalty(loyalty, prior), null, null, scandals, liveScandalRate,
                null, PartyFamilies.For(CountryId.Poland, cast), NationalElection.EntrantAwareness(CountryId.Poland, cast), EntrantLayer.GroupingOf(CountryId.Poland, cast), EntrantSimilarity.For(CountryId.Poland, cast),
                stands, doorCacePp: sejm ? GotvTurnout.EuropeanDoorCacePp : 0.0, purseUnit: unit, contest: contest);
            if (sejm) { setup = WithOffsets(setup, CountryId.Poland, cal.CampaignStart, played); }   // Regional 5a (K149): the okregi's offsets a regional poll reads
            return true;
        }

        /// <summary>
        /// PL-C (K168: "the two finalists campaign through the 14 days between rounds"): <b>THE RUN-OFF'S CAMPAIGN</b> for the pending <paramref name="contest"/>
        /// on <paramref name="calendar"/> (`PolandRunOffCalendar`: the run-off day, two weeks, no run-up). Every roster party is in the cast so the indices
        /// stay the first round's, but only the two finalists' parties stand (in every wojewodztwo) and hold a purse; the rest stand nowhere, so nothing of
        /// theirs lands. The prior is B4's transfer (`PresidentialElection.RunOffShareOf`) - finalist A's share of the two and B's the rest - and the
        /// compatibility its fixed point, every party at one loyalty (the finalists' parties' loyalties weighted by that prior, DECLARED: one value, so an idle
        /// run-off reproduces B4's share of the two exactly). K167: one limit covers both rounds (art. 327 § 1, a committee's spending for the election), so
        /// each finalist's chest is its first round's chest less what its first-round campaign spent (`PresidentialElection.Contest.FirstRoundSpent`; none
        /// recorded, the whole chest). The count is the campaign's share of the two in place of B4's (`PresidentialElection.HoldRounds`). False, with the
        /// reason, where a finalist has no roster party or no position.
        /// </summary>
        public static bool PolandRunOff(PresidentialElection.Contest contest, (int Day, int Party, Scandal Scandal)[] scandals, CampaignCalendar calendar, out CampaignRun.Setup setup,
            out string note, int playerParty = -1, Func<int, AiDecision[]> playerScript = null, Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0,
            Country played = null)
        {
            setup = default;
            if (contest == null || contest.RunOffA == null || contest.RunOffB == null) { note = "no run-off campaign is staged: no run-off is pending"; return false; }
            PresidentialElection.Candidate a = contest.Field.Find(c => c.Name == contest.RunOffA), b = contest.Field.Find(c => c.Name == contest.RunOffB);
            string[] cast = Keys(CountryId.Poland);
            int ia = a?.Party != null ? Array.IndexOf(cast, a.Party) : -1, ib = b?.Party != null ? Array.IndexOf(cast, b.Party) : -1;
            if (ia < 0 || ib < 0 || ia == ib) { note = "no run-off campaign is staged: a finalist has no party of its own on the roster"; return false; }
            double shareA = PresidentialElection.RunOffShareOf(contest, out double voting);
            if (double.IsNaN(shareA)) { note = "no run-off campaign is staged: a finalist has no position in the sovereignty space"; return false; }
            var prior = new double[cast.Length];
            prior[ia] = shareA / 100.0;
            prior[ib] = 1.0 - prior[ia];
            // the finalists' parties' loyalties as the vote model reads them, weighted by the prior, one value for every party
            double la = NationalElection.UnplacedLoyalty, lb = NationalElection.UnplacedLoyalty;
            if (NationalElection.TryCompatibility(CountryId.Poland, out string[] modelKeys, out double[] _, out double[] _, out double[] modelLoyalty, played, contest.FirstVote))
            {
                int ka = Array.IndexOf(modelKeys, cast[ia]), kb = Array.IndexOf(modelKeys, cast[ib]);
                if (ka >= 0) { la = modelLoyalty[ka]; }
                if (kb >= 0) { lb = modelLoyalty[kb]; }
            }
            double one = prior[ia] * la + prior[ib] * lb;
            var loyalty = new double[cast.Length];
            var compatibility = new double[cast.Length];
            double maxPrior = Math.Max(prior[ia], prior[ib]);
            for (int p = 0; p < cast.Length; p++)
            {
                loyalty[p] = one;
                compatibility[p] = maxPrior > 0.0 && prior[p] > 0.0 ? CompatibilityCeiling * Math.Pow(prior[p] / maxPrior, 1.0 / PreferenceModel.Sharpness) : 0.0;   // DERIVED: the fixed point
            }
            RegionAudience[] regions = PolishCampaign.Wojewodztwa(contest.FirstVote, out double national, out int year);
            var stands = new bool[cast.Length][];
            for (int p = 0; p < cast.Length; p++) { stands[p] = new bool[regions.Length]; for (int r = 0; r < regions.Length; r++) { stands[p][r] = p == ia || p == ib; } }
            // K167: the election's one limit - read on the first round's opening, so both rounds divide the same figure - and what the first round left
            DateTime firstOpens = new CampaignCalendar(contest.FirstVote).CampaignStart;
            double full = PolishCampaign.PresidentialLimit(firstOpens, contest.FirstVote, out string limitBasis);
            if (double.IsNaN(full) || !(full > 0.0)) { note = "no run-off campaign is staged: the statute's spending limit cannot be read - " + limitBasis; return false; }
            double unit = full / WarChest;
            double[] salience = PolishCampaign.Salience(calendar.CampaignStart, out string salienceLabel);
            var parties = new CampaignRun.PartySetup[cast.Length];
            var left = new double[cast.Length];
            for (int p = 0; p < cast.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = PersonalityOf(CountryId.Poland, cast[p]);
                bool finalist = p == ia || p == ib;
                double spent = finalist && contest.FirstRoundSpent != null && contest.FirstRoundSpent.TryGetValue(cast[p], out double s) ? s : 0.0;
                left[p] = finalist ? Math.Max(0.0, WarChest - spent) : 0.0;
                parties[p] = new CampaignRun.PartySetup(cast[p], personality, FlatCredibility, left[p], match, finalist ? Volunteers : 0,
                    CandidateFor(personality, cast[p]), finalist ? OfficesFor(personality, regions, stands[p]) : new int[0], OfficeOperationsPerDay,
                    finalist ? StaffFor(personality) : new StaffRole[0], finalist ? TelevisionBuysFor(personality) : 0,
                    p == playerParty ? playerScript : null, p == playerParty ? playerScandalScript : null);
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[cast.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[cast.Length], isInternal: true);
            note = string.Format(CultureInfo.InvariantCulture,
                "\n  staging: the run-off of {0:yyyy-MM-dd} - {1} ({2}) and {3} ({4}) through the {5} days from the first vote of {6:yyyy-MM-dd} (K168); the prior B4's transfer, {1} {7:0.00} % of the two ({8:0.00} % of the first round's voters voting), loyalty {9:F1} for every party (the fixed point)\n" +
                "    16 wojewodztwa (SOURCED the PKW's {10} first-round valid votes and electorates); salience {11}\n" +
                "    K167: one limit for both rounds - {12}: {13:N0} zł; {2} has {14:N0} zł left, {4} {15:N0} zł (the first round's spending taken off)\n",
                calendar.ElectionDate, contest.RunOffA, cast[ia], contest.RunOffB, cast[ib], calendar.TotalCampaignDays, contest.FirstVote, shareA, voting, one,
                year, salienceLabel, limitBasis, full, left[ia] * unit, left[ib] * unit);
            setup = new CampaignRun.Setup(calendar, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, one, null, null, scandals, liveScandalRate,
                null, PartyFamilies.For(CountryId.Poland, cast), NationalElection.EntrantAwareness(CountryId.Poland, cast), EntrantLayer.GroupingOf(CountryId.Poland, cast), EntrantSimilarity.For(CountryId.Poland, cast),
                stands, doorCacePp: 0.0, purseUnit: unit, contest: CampaignContest.PresidentialRunOff);
            return true;
        }

        /// <summary>PL-C (K168): the run-off's calendar - the two weeks from the first vote to the run-off (Art. 127 ust. 4's 14th day), no run-up.</summary>
        public static CampaignCalendar PolandRunOffCalendar(DateTime runOff) => new CampaignCalendar(runOff, 2, 0);

        /// <summary>PL-C (K167): the HQ's statement of the purse's cap for <paramref name="party"/> in <paramref name="setup"/> - its limit in złoty and the article -
        /// or null where the staging carries none (every country but Poland).</summary>
        public static string SpendingLimitLine(CampaignRun.Setup? setup, int party)
        {
            double limit = SpendingLimit(setup, party);
            if (double.IsNaN(limit)) { return null; }
            return string.Format(CultureInfo.InvariantCulture, "CAP {0:N0} PLN (KODEKS WYBORCZY ART. {1})", limit, setup.Value.Contest == CampaignContest.Chamber ? "199" : "327");
        }

        /// <summary>PL-C (K167): <paramref name="party"/>'s statutory limit in złoty in <paramref name="setup"/> - the committee's own, never its brought chest; NaN where
        /// the staging carries none (every country but Poland). The HQ's caption prints it; the article is the log's and the staging note's (<see cref="SpendingLimitLine"/>).</summary>
        public static double SpendingLimit(CampaignRun.Setup? setup, int party)
        {
            if (!setup.HasValue || !(setup.Value.PurseUnit > 0.0) || setup.Value.Parties == null || party < 0 || party >= setup.Value.Parties.Length) { return double.NaN; }
            CampaignRun.Setup s = setup.Value;
            bool sejm = s.Contest == CampaignContest.Chamber;
            bool[] where = s.Stands != null && party < s.Stands.Length ? s.Stands[party] : null;
            bool anywhere = where == null;
            if (where != null) { foreach (bool w in where) { anywhere |= w; } }
            return !anywhere ? 0.0 : sejm && where != null ? WarChest * PolishCampaign.MandatesWhere(where) / PolishCampaign.SejmSeats * s.PurseUnit : WarChest * s.PurseUnit;
        }
    }
}
