using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Regional 5a (`docs/RULINGS_2026-10-23.md` K146-K149 with K83, Elias 2026-10-27): <b>the regional campaign, proved where an Editor harness can
    /// prove it.</b> Each statement below fails this check while it is false.
    /// <list type="number">
    /// <item><b>(1) K146, the regions</b>: Sweden, Germany, the USA and Poland are regional and Italy and France national, each saying why; Poland's 41
    /// okregi are the PKW's - the two stored zips held to their folder's SHA256SUMS, the generated table's every name, electorate and valid vote re-read
    /// from the zips' own CSVs, the electorates summing to the notice's 29 532 595 and the valid votes to its 21 596 674.</item>
    /// <item><b>(2) K147, the identity</b>: a campaign of national acts alone, tallied by region, has exactly the national formula's effects (the
    /// regions' audience-weighted sum against a twin tally kept national, to 1e-12 points); a contact held in region R moves R and no other region
    /// (every other region's movement exactly zero), each region's movements sum to zero, and the national movement is the regions' weighted sum.</item>
    /// <item><b>(3) K81 read per region</b>: equal effort moves nobody, and a party campaigning alone - the CSU on Bayern's ballots, a party standing
    /// everywhere - moves nationally by exactly its own sourced effect.</item>
    /// <item><b>(4) K147, the count</b> (Sweden's and Germany's counts at σ 0, no doors): a movement laid on one valkrets, with the national shares carrying its weighted
    /// part as the campaign's do, is the uniform swing's own re-solve on the shifted priors to two votes a region, the region keeping at least 0.9 of the point and no other moving a tenth of it; the counted national shares are the
    /// counted valkretsar's vote-weighted sum (to 1e-12) and the shares handed (to the rounding of a vote).</item>
    /// <item><b>(5) The US states</b>: the persuasion pattern the count reads per state sums to zero over the country, audience-weighted, in each column, and keeps each state's three summing to zero where no other candidate stands,
    /// and keeps a movement's difference between two states.</item>
    /// <item><b>(6) K149, the poll</b>: a regional poll's margin of error is the binomial formula at its own sample for every party, and the lead's the one-sample formula (review M2's worked case too close to call); a second regional poll the same day is refused; a scripted Swedish
    /// campaign that rallies in one valkrets and buys one regional poll there pays its price once off the chest (against the same run without it), files the poll on its region at its sample, reads that
    /// region polled and every other unknown on the swing map, moves its own share most where it rallied, and every region's movement sums to zero.</item>
    /// <item><b>(7) The AI</b> values a local act at the run's own regional difference (its region's effect with the contact less without, weighted).</item>
    /// </list>
    /// Prints `REGIONAL CAMPAIGN: PASS` and exits 0 when every check holds; each failure is a `FAIL` line naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.RegionalCampaignDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class RegionalCampaignDiagnostic
    {
        private const double Exact = 1e-12;
        private const int Seed = 777;
        /// <summary>SOURCED (Dz.U. 2023 poz. 2234, Dzial I rozdz. 2, as `ElectionsData/poland/returns_2023.md` quotes it): the national electorate and the valid votes.</summary>
        private const long NoticeEligible = 29_532_595L, NoticeValid = 21_596_674L;
        /// <summary>CONVENTION: the shares handed against the count's, a fraction - the rounding of a vote in each of the regions.</summary>
        private const double VoteRounding = 1e-5;
        /// <summary>CONVENTION: the test movement laid on one region (a fraction, one point) - a probe size, not a model figure.</summary>
        private const double Probe = 0.01;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("REGIONAL CAMPAIGN: Regional 5a (K146-K149) - a contact moves the region it lands in, the national total and the count consistent\n");
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Regions();
                    Identity();
                    FieldRelative();
                    Count();
                    UsStates();
                    Poll();
                }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "REGIONAL CAMPAIGN: PASS - the regions per K146 (Poland's okregi the PKW's, Italy and France national); national acts tallied by region have the national effects exactly; a contact in one region moves that region and no other, the national movement the regions' weighted sum; equal effort moves nobody and a lone campaigner its sourced effect worked by hand; the count (Sweden and Germany) is the re-solve's own arithmetic on the shifted priors to two votes a region, keeps at least 0.9 of a region's movement there and sums its regions to the national shares; every staging carries its regions' offsets; the US pattern sums to zero and a ballot with no other candidate gives the Others movement to the nominees; the regional poll's margin the binomial formula and the lead's the one-sample formula (600 at 33.5/28.0 too close to call), one a day, paid off the chest, filed and mapped; the AI's value the run's own regional difference."
                : F("REGIONAL CAMPAIGN: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) K146
        private static void Regions()
        {
            _sb.Append("  (1) K146 - what a region is:\n");
            foreach (CountryId c in new[] { CountryId.Sweden, CountryId.Germany, CountryId.USA, CountryId.Poland })
            {
                WorldClock.ApplyStart(c);
                RegionAudience[] regions = RegionLayer.Of(c, WorldClock.StartDate(c), out string why);
                _sb.Append(F("      {0}: {1} {2}(s), audience {3:N0}, electorate {4:N0}\n", c, regions.Length, RegionLayer.UnitOf(c), regions.Sum(r => r.Audience), regions.Sum(r => r.Eligible)));
                Check(RegionLayer.IsRegional(c) && regions.Length > 1 && why == null && regions.All(r => r.Audience > 0.0 && r.Eligible >= r.Audience),
                    F("{0} is campaigned by {1}, every region with an audience and an electorate no smaller", c, RegionLayer.UnitOf(c)));
            }
            foreach (CountryId c in new[] { CountryId.Italy, CountryId.France })
            {
                RegionAudience[] none = RegionLayer.Of(c, WorldClock.StartDate(c), out string why);
                Check(!RegionLayer.IsRegional(c) && none.Length == 0 && why != null, F("{0} stays national: \"{1}\"", c, why));
            }

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland", "raw", "okreg_electorates_2023"));
            List<string> faults = ItalySeatingDiagnostic.DigestFaults(folder, out int held);
            Check(held == 2 && faults.Count == 0, F("Poland's two PKW zips held to their SHA256SUMS.txt ({0} held){1}", held, faults.Count == 0 ? string.Empty : " - " + string.Join("; ", faults)));
            if (faults.Count > 0) { return; }
            string[][] results = Csv(Path.Combine(folder, "wyniki_gl_na_listy_po_okregach_sejm_csv.zip"), "wyniki_gl_na_listy_po_okregach_sejm_utf8.csv");
            string[][] districts = Csv(Path.Combine(folder, "okregi_sejm_csv.zip"), "okregi_sejm_utf8.csv");
            int iEligible = Array.FindIndex(results[0], h => h.StartsWith("Liczba wyborców uprawnionych", StringComparison.Ordinal));
            int iValid = Array.FindIndex(results[0], h => h.StartsWith("Liczba głosów ważnych oddanych łącznie", StringComparison.Ordinal));
            int iSeat = Array.IndexOf(districts[0], "Siedziba OKW");
            var g = PoliSim.Elections.Generated.PolishOkregElectorates2023.Names;
            int mismatched = 0;
            long sumE = 0, sumV = 0;
            for (int k = 0; k < 41; k++)
            {
                long e = long.Parse(results[k + 1][iEligible], CultureInfo.InvariantCulture), v = long.Parse(results[k + 1][iValid], CultureInfo.InvariantCulture);
                sumE += e; sumV += v;
                string name = "okreg " + (k + 1).ToString(CultureInfo.InvariantCulture);
                if (e != PoliSim.Elections.Generated.PolishOkregElectorates2023.Eligible[k] || v != PoliSim.Elections.Generated.PolishOkregElectorates2023.Valid[k] || name != g[k]
                    || districts[k + 1][iSeat] != PoliSim.Elections.Generated.PolishOkregElectorates2023.CommissionSeats[k]) { mismatched++; }
            }
            Check(iEligible >= 0 && iValid >= 0 && iSeat >= 0 && g.Length == 41 && mismatched == 0 && sumE == NoticeEligible && sumV == NoticeValid,
                F("the generated okreg table is the PKW files' own: 41 okregi, {0} differing; electorates {1:N0} (the notice {2:N0}), valid votes {3:N0} (the notice {4:N0})", mismatched, sumE, NoticeEligible, sumV, NoticeValid));
        }

        private static string[][] Csv(string zip, string member)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zip))
            using (var reader = new StreamReader(archive.GetEntry(member).Open(), new UTF8Encoding(false)))
            {
                string text = reader.ReadToEnd().TrimStart('﻿');
                return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split(';').Select(f => f.Trim('"')).ToArray()).ToArray();
            }
        }

        // ============================================================ (2) K147 - the identity
        private static CampaignActions.ChainTrace Contact(CampaignActionKind kind, double exposure, double quality)
        {
            double w = CampaignActions.Spec(kind).PersuasionWeight;
            return new CampaignActions.ChainTrace(kind, exposure, 1.0, exposure, quality, 1.0, exposure * quality * w, 0.0, 0.0);
        }

        private static void Identity()
        {
            _sb.Append("  (2) K147 - the regional tally and the national formula (three regions of 500 000, 300 000 and 200 000; three parties; the norm 0.25):\n");
            double[] audience = { 500_000.0, 300_000.0, 200_000.0 };
            double all = audience.Sum(), norm = 0.25;
            var regional = new CampaignPressure(3);
            regional.ConfigureRegions(audience, null);
            var national = new CampaignPressure(3);
            foreach ((int p, CampaignActionKind kind, double e) in new[] { (0, CampaignActionKind.TelevisionAd, 300_000.0), (1, CampaignActionKind.DigitalAd, 120_000.0), (2, CampaignActionKind.Interview, 80_000.0), (0, CampaignActionKind.SocialPost, 50_000.0) })
            {
                regional.Add(p, Contact(kind, e, norm));
                national.Add(p, Contact(kind, e, norm));
            }
            double[] byRegion = regional.ShareEffects(all, norm);
            double[] byNation = national.ShareEffects(all, norm);
            double worst = Enumerable.Range(0, 3).Max(p => Math.Abs(byRegion[p] - byNation[p]));
            Check(regional.Regional && !national.Regional && worst * 100.0 <= Exact,
                F("national acts tallied by region have the national formula's effects: {0} against {1} points (largest gap {2:E1})",
                    string.Join(" ", byRegion.Select(v => F("{0:+0.000000}", 100.0 * v))), string.Join(" ", byNation.Select(v => F("{0:+0.000000}", 100.0 * v))), 100.0 * worst));

            var local = new CampaignPressure(3);
            local.ConfigureRegions(audience, null);
            local.Add(0, Contact(CampaignActionKind.Rally, 40_000.0, norm), 1);
            double[] shares = { 0.5, 0.3, 0.2 };
            double[][] shifts = local.RegionalShifts(shares, norm);
            double others = Math.Max(shifts[0].Max(Math.Abs), shifts[2].Max(Math.Abs));
            double rowSum = shifts.Max(row => Math.Abs(row.Sum()));
            double[] nationalShift = local.NationalShift(shifts);
            double weighted = Enumerable.Range(0, 3).Max(p => Math.Abs(nationalShift[p] - Enumerable.Range(0, 3).Sum(x => audience[x] / all * shifts[x][p])));
            double[] e1 = local.RegionEffects(1, norm, null, null, out _, out _);
            Check(shifts[1][0] > 0.0 && others == 0.0 && rowSum <= Exact && weighted <= Exact && Math.Abs(shifts[1][0] - e1[0]) <= Exact,
                F("a rally held in region 1 moves region 1 (party 0 {0:+0.000000} points, its sourced effect there {1:+0.000000}) and no other region (largest other movement {2}); each region's movements sum to zero ({3:E1}); the national movement is their weighted sum ({4:E1})",
                    100.0 * shifts[1][0], 100.0 * e1[0], others, rowSum, weighted));
        }

        // ============================================================ (3) K81 per region
        private static void FieldRelative()
        {
            _sb.Append("  (3) K81 read per region:\n");
            double[] audience = { 500_000.0, 300_000.0, 200_000.0 };
            double norm = 0.25;
            double[] shares = { 0.5, 0.3, 0.2 };
            var equal = new CampaignPressure(3);
            equal.ConfigureRegions(audience, null);
            for (int p = 0; p < 3; p++) { equal.Add(p, Contact(CampaignActionKind.TelevisionAd, 200_000.0, norm)); }
            double[] moved = equal.ApplyRegional(shares, norm);
            double drift = Enumerable.Range(0, 3).Max(p => Math.Abs(moved[p] - shares[p]));
            Check(100.0 * drift <= Exact, F("equal effort moves nobody: every party the same television, the largest move {0:E1} points", 100.0 * drift));

            WorldClock.ApplyStart(CountryId.Germany);
            CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Germany), WorldClock.StartDate(CountryId.Germany));
            if (!LiveCampaignSetup.TryFor(CountryId.Germany, new (int, int, Scandal)[0], cal, out CampaignRun.Setup de, out string note, onVoteModelCompatibility: true)) { Check(false, "no German staging: " + note); return; }
            int csu = Array.FindIndex(de.Parties, x => x.Name == "CSU"), spd = Array.FindIndex(de.Parties, x => x.Name == "SPD");
            double sum = de.PriorShares.Sum();
            double[] prior = de.PriorShares.Select(v => v / sum).ToArray();
            double deNorm = CampaignPersuasion.ElectorateMean(de);
            foreach (int solo in new[] { csu, spd })
            {
                var alone = new CampaignPressure(de.Parties.Length);
                alone.ConfigureRegions(de.Regions.Select(x => x.Audience).ToArray(), de.Stands);
                double reach = de.StandingShare(solo), exposure = 0.3 * de.NationalAudience * reach;
                alone.Add(solo, Contact(CampaignActionKind.TelevisionAd, exposure, deNorm));
                // by hand, from the formula's inputs alone: at the mean quality a contact takes the impersonal CACE, [KB18]'s 0.28 points, over the coverage of
                // the party's reach - r (1 - e^(-E / (r A))) - which is what 30 % of its reach in exposures covers
                double expected = 0.28 / 100.0 * reach * (1.0 - Math.Exp(-exposure / (reach * de.NationalAudience)));
                double[] after = alone.ApplyRegional(prior, deNorm);
                Check(expected > 0.0 && Math.Abs((after[solo] - prior[solo]) - expected) * 100.0 <= 1e-9,
                    F("{0} campaigning alone (on {1:0.0} % of the ballots) moves nationally by exactly its sourced effect worked by hand: {2:+0.000000} against 0.28 x {1:0.0} % x (1 - e^-0.3) = {3:+0.000000} points", de.Parties[solo].Name, 100.0 * reach, 100.0 * (after[solo] - prior[solo]), 100.0 * expected));
            }
        }

        // ============================================================ (4) K147 - the count
        private static void Count()
        {
            _sb.Append("  (4) K147 - the count (sigma 0, no doors; the tolerance two votes over the region's votes, the count's own rounding):\n");
            WorldClock.ApplyStart(CountryId.Sweden);
            CountCase(CountryId.Sweden, LiveCampaignSetup.Sweden(new (int, int, Scandal)[0], out _));
            WorldClock.ApplyStart(CountryId.Germany);
            CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Germany), WorldClock.StartDate(CountryId.Germany));
            if (LiveCampaignSetup.TryFor(CountryId.Germany, new (int, int, Scandal)[0], cal, out CampaignRun.Setup de, out string note, onVoteModelCompatibility: true)) { CountCase(CountryId.Germany, de); }
            else { Check(false, "no German staging: " + note); }
        }

        /// <summary>One country's count case: a point moved from the second-listed party to the first in the largest region where both stand, the national shares
        /// carrying its weighted part as the campaign's do; that region moves by the point and every other by under two votes, and the national shares are the
        /// counted regions' sum and the shares handed.</summary>
        private static void CountCase(CountryId country, CampaignRun.Setup s)
        {
            int n = s.Parties.Length, k = s.Regions.Length;
            int region = Enumerable.Range(0, k).Where(x => s.StandsIn(0, x) && s.StandsIn(1, x)).OrderByDescending(x => s.Regions[x].Audience).First();
            double all = s.Regions.Sum(x => x.Audience), w = s.Regions[region].Audience / all;
            double sum = s.PriorShares.Sum();
            var handed = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int p = 0; p < n; p++) { handed[s.Parties[p].Name] = s.PriorShares[p] / sum; }
            var shift = new double[k][];
            for (int x = 0; x < k; x++) { shift[x] = new double[n]; }
            shift[region][0] = Probe; shift[region][1] = -Probe;
            var moved = new Dictionary<string, double>(handed);
            moved[s.Parties[0].Name] += w * Probe; moved[s.Parties[1].Name] -= w * Probe;
            DateTime day = s.Calendar.ElectionDate;
            ParliamentaryCount.Result before, after;
            using (ParliamentaryCount.NoiseAtZero())
            {
                before = ParliamentaryCount.Count(country, day, handed, s, null, writeReadout: false);
                after = ParliamentaryCount.Count(country, day, moved, s, null, writeReadout: false, regionalShift: shift);
            }
            if (before?.Regional == null || after?.Regional == null) { Check(false, country + "'s count returned no counted regions"); return; }
            double Share(ParliamentaryCount.Result r, int x, string key)
            {
                double[] v = r.Regional.Regions[x].Votes;
                return v[r.RegionKeys.IndexOf(key)] / v.Sum();
            }
            double TwoVotesPp(ParliamentaryCount.Result r, int x) => 100.0 * 2.0 / r.Regional.Regions[x].Votes.Sum();
            int at = Array.IndexOf(after.RegionNames, s.Regions[region].Name);
            if (at < 0) { Check(false, F("{0}'s count has no region named {1}", country, s.Regions[region].Name)); return; }
            string a = s.Parties[0].Name;
            double here = 100.0 * (Share(after, at, a) - Share(before, at, a));
            // The re-solve's own arithmetic, from its inputs by hand: the regions' priors in the count's key order (`SwedishRegions` / `GermanRegions`, the
            // tables the count reads), the point laid on the region's row, and the uniform swing re-solved to the shares handed
            // (`RegionalVoteModel.RegionalSharesByUniformSwing`). The swing renormalises each row and corrects one figure per party until the national totals
            // agree, so the region keeps somewhat less than the point and every other region moves a little: the count must equal THIS, to two votes a region.
            List<string> keys = after.RegionKeys;
            RegionalVoteModel.RegionInput[] inputs = country == CountryId.Sweden ? SwedishRegions.Regions(keys) : GermanRegions.Regions(keys, day);
            double[][] priors = country == CountryId.Sweden ? SwedishRegions.PriorShares(keys) : GermanRegions.PriorShares(keys, day);
            int laidAt = Array.FindIndex(inputs, x => x.Name == s.Regions[region].Name);
            priors[laidAt][keys.IndexOf(s.Parties[0].Name)] += Probe;
            priors[laidAt][keys.IndexOf(s.Parties[1].Name)] -= Probe;
            double[] handedNational = keys.Select(key => moved[key]).ToArray();
            double[][] expected = RegionalVoteModel.RegionalSharesByUniformSwing(handedNational, inputs, priors, out double _);
            double worstExpected = 0.0;
            for (int x = 0; x < after.RegionNames.Length; x++)
            {
                int e = Array.FindIndex(inputs, y => y.Name == after.RegionNames[x]);
                foreach (string key in keys) { worstExpected = Math.Max(worstExpected, Math.Abs(100.0 * (Share(after, x, key) - expected[e][keys.IndexOf(key)])) - TwoVotesPp(after, x)); }
            }
            Check(laidAt >= 0 && worstExpected <= 0.0,
                F("{0}: every counted region is the re-solve's own arithmetic on the shifted priors, to two of its votes (the worst {1:+0.000000;-0.000000} points past them); {2} keeps {3:0.0000} of the laid point", country, worstExpected, s.Regions[region].Name, here / (100.0 * Probe)));
            double elsewhere = 0.0;
            for (int x = 0; x < after.RegionNames.Length; x++)
            {
                if (x == at) { continue; }
                foreach (string key in after.RegionKeys)
                {
                    elsewhere = Math.Max(elsewhere, Math.Abs(100.0 * (Share(after, x, key) - Share(before, x, key))));
                }
            }
            // DECLARED bound (the first real run, 2026-10-28: Sweden kept 0.9950 of the point, the others moved up to 0.0044 points; Germany 0.9820 and 0.0064): the
            // region keeps at least nine tenths of the point and no other region moves by more than a tenth of it - what "a contact moves the region it lands in"
            // means once the re-solve has renormalised the rows; the exact figures are the check above
            Check(after.CampaignRegional && after.CampaignRegionsMoved == 1 && here >= 0.9 * 100.0 * Probe && here <= 100.0 * Probe + TwoVotesPp(after, at) && elsewhere <= 0.1 * 100.0 * Probe,
                F("{0}: a point laid on {1} ({2} +1, {3} -1, the national shares carrying {4:0.0000} of it) moves {1} by {5:+0.000000} points for {2} and every other region by at most {6:0.000000} (DECLARED: the region keeps at least 0.9 of it, no other moves a tenth of it)",
                    country, s.Regions[region].Name, a, s.Parties[1].Name, w, here, elsewhere));
            double aggregate = 0.0, handedGap = 0.0;
            double votes = after.Regional.Regions.Sum(r => r.Votes.Sum());
            for (int i = 0; i < after.RegionKeys.Count; i++)
            {
                double byHand = after.Regional.Regions.Sum(r => r.Votes[i]) / votes;
                aggregate = Math.Max(aggregate, Math.Abs(byHand - after.CountedShares[after.RegionKeys[i]]));
                handedGap = Math.Max(handedGap, Math.Abs(after.CountedShares[after.RegionKeys[i]] - moved[after.RegionKeys[i]]));
            }
            Check(aggregate <= Exact && handedGap <= VoteRounding,
                F("{0}: the counted national shares are the counted regions' vote-weighted sum (largest gap {1:E1}) and the shares handed (largest gap {2:E1}, the rounding of a vote)", country, aggregate, handedGap));
        }

        // ============================================================ (5) the US states
        private static void UsStates()
        {
            _sb.Append("  (5) the US states' persuasion pattern:\n");
            WorldClock.ApplyStart(CountryId.USA);
            if (!LiveCampaignSetup.TryFor(CountryId.USA, new (int, int, Scandal)[0], null, out CampaignRun.Setup us, out string note, onVoteModelCompatibility: true)) { Check(false, "no US staging: " + note); return; }
            int n = us.Parties.Length, k = us.Regions.Length;
            int dem = Array.FindIndex(us.Parties, x => x.Name == "DEM"), rep = Array.FindIndex(us.Parties, x => x.Name == "REP");
            var shift = new double[k][];
            for (int x = 0; x < k; x++) { shift[x] = new double[n]; }
            int r0 = 0, r1 = k - 1;
            shift[r0][dem] = Probe; shift[r0][rep] = -Probe;
            var result = new CampaignRun.Result { Gotv = new RegionalMobilization(us.Regions.Select(x => x.Eligible).ToArray(), n), RegionalShift = shift };
            UsGroundGame.Campaign c = UsGroundGame.Of(us, result);
            if (c?.DemocratShift == null) { Check(false, "the US ground game carries no persuasion pattern"); return; }
            double Centred(double[] v) => Math.Abs(Enumerable.Range(0, k).Sum(x => c.Audience[x] * v[x]) / c.Audience.Sum());
            double worst = Math.Max(Centred(c.DemocratShift), Math.Max(Centred(c.RepublicanShift), Centred(c.OthersShift)));
            double keeps = (c.DemocratShift[r0] - c.DemocratShift[r1]) - Probe;
            Check(worst <= Exact && Math.Abs(keeps) <= Exact && Math.Abs(c.DemocratShift[r0] + c.RepublicanShift[r0] + c.OthersShift[r0]) <= Exact,
                F("each column sums to zero over the country, audience-weighted (largest {0:E1}); {1} keeps the point over {2} ({3:E1}); a state's three sum to zero", worst, us.Regions[r0].Name, us.Regions[r1].Name, keeps));
            // review L4: where no other candidate is on a state's ballot the Others movement goes to the two nominees by their shares, so nothing is rescaled
            UsGroundGame.Campaign planted = UsGroundGame.Campaign.Empty(1);
            planted.DemocratShift = new[] { 0.01 }; planted.RepublicanShift = new[] { -0.02 }; planted.OthersShift = new[] { 0.01 };
            double[] laid = UsGroundGame.WithPattern(new[] { 0.6, 0.4, 0.0 }, planted, 0);
            Check(Math.Abs(laid[0] - 0.616) <= Exact && Math.Abs(laid[1] - 0.384) <= Exact && laid[2] == 0.0,
                F("a state with no other candidate on its ballot: the Others movement +0.01 goes to the nominees by their shares - 60/40 moved by +0.01/-0.02 reads {0:0.000000}/{1:0.000000}/{2} (by hand 0.616/0.384/0)", laid[0], laid[1], laid[2]));
        }

        // ============================================================ (6) K149 - the poll, (7) the AI
        private static void Poll()
        {
            _sb.Append("  (6) K149 - the regional poll, (7) the AI (Sweden's staging, seed 777):\n");
            var truth = new[] { 0.35, 0.2, 0.2, 0.1, 0.05, 0.05, 0.03, 0.02 };
            Poll polled = PollingSystem.Conduct(truth, RegionalPolling.House("probe"), new DateTime(2026, 8, 1), new System.Random(Seed));
            double worstMoe = Enumerable.Range(0, truth.Length).Max(p => Math.Abs(polled.MarginOfErrorPp(p) - 100.0 * 1.96 * Math.Sqrt(polled.Share(p) * (1.0 - polled.Share(p)) / RegionalPolling.Sample)));
            Check(polled.SampleSize == RegionalPolling.Sample && worstMoe <= Exact,
                F("the margin of error is 1.96 x sqrt(s(1 - s)/n) at the poll's own {0} interviews for every party (largest gap {1:E1}); at a share of one half {2:0.00} points", RegionalPolling.Sample, worstMoe, RegionalPolling.MarginOfErrorPp(0.5, RegionalPolling.Sample)));

            // review M2: the lead's margin read from ONE sample - 600 interviews at 33.5 against 28.0: ±6.26 points, the lead's 5.5 inside it
            var close = new[] { 0.335, 0.28, 0.17, 0.12, 0.095 };
            Poll worked = new Poll(new DateTime(2026, 8, 1), RegionalPolling.Sample, "worked case", close, close.Select(x => RegionalPolling.MarginOfErrorPp(x, RegionalPolling.Sample)).ToArray());
            MapRegionReading reading = SwingRegions.FromPoll("worked case", 1.0, worked);
            double oneSample = 100.0 * 1.96 * Math.Sqrt((0.335 + 0.28 - 0.055 * 0.055) / RegionalPolling.Sample);
            double independent = Math.Sqrt(Math.Pow(worked.MarginOfErrorPp(0), 2) + Math.Pow(worked.MarginOfErrorPp(1), 2));
            Check(Math.Abs(reading.GapErrorPp - oneSample) <= Exact && Math.Abs(reading.GapErrorPp - 6.26) < 0.01 && reading.TooCloseToCall && RegionalPolling.StateOf(reading) == RegionalPolling.MapState.TooCloseToCall,
                F("the lead's margin is one sample's, 1.96 x sqrt((sL + sR - (sL - sR)^2)/n): {0} interviews at 33.5 against 28.0 read a lead of {1:0.0} +- {2:0.00} points - TOO CLOSE TO CALL (the independent-samples sum would have read +-{3:0.00} and called it)",
                    RegionalPolling.Sample, reading.GapPp, reading.GapErrorPp, independent));

            WorldClock.ApplyStart(CountryId.Sweden);
            var none = new (int, int, Scandal)[0];
            CampaignRun.Setup probe = LiveCampaignSetup.Sweden(none, out _);
            const int party = 0;
            int region = Enumerable.Range(0, probe.Regions.Length).OrderByDescending(x => probe.Regions[x].Audience).First();
            CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
            AiDecision regionalPoll = new AiDecision(CampaignActionKind.CommissionPolling, new CampaignActions.ActionTarget(region, -1, null), probe.Regions[region].Name, RegionalPolling.Cost, CampaignAi.PollingHours, 0.0, false);
            Func<int, AiDecision[]> script = d => d == 5
                ? new[] { regionalPoll, regionalPoll }   // the second is refused: one regional poll a day
                : new[] { new AiDecision(CampaignActionKind.Rally, new CampaignActions.ActionTarget(region, -1, null), probe.Regions[region].Name, rally.MoneyCost, rally.Hours, 0.0, false) };
            CampaignRun.Setup s = LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: script);
            Check(s.RegionalOffset != null && s.RegionalOffset.Length == s.Regions.Length, "Sweden's game staging (its prior on the chamber seated at the start) carries every valkrets's offset, so a regional poll is on sale");
            Offsets();
            SimulationRandom.Seed(Seed);
            CampaignRun.State state = CampaignRun.Begin(s, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            int expectedSample = -1;
            double chestAfterDay5 = double.NaN;
            while (!state.Finished)
            {
                CampaignRun.StepDay(state);
                // review L-r5: the roster as of the day the poll was bought (`Improve` reads the pollster on the roster and paid that day); and the chest at that day's close
                if (state.Day == 6) { expectedSample = state.Staff[party].Improve(RegionalPolling.House(probe.Regions[region].Name)).SampleSize; chestAfterDay5 = state.Pools[party].Money; }
            }
            CampaignRun.Result r = CampaignRun.Finish(state);
            // review L7(b): the price read off the chest - the same run with day 5 left empty, its chest read at the SAME day's close (the first real run compared the
            // campaigns' ends and read 0: a daily rally spends the chest out in both runs, so the money left at the close is the same and says nothing of day 5).
            // Days 0-4 are the same in both; on day 5 the one buys the poll and the other does nothing, so the two chests differ by exactly its price.
            Func<int, AiDecision[]> twinScript = d => d == 5 ? new AiDecision[0] : script(d);
            SimulationRandom.Seed(Seed);
            CampaignRun.State twinState = CampaignRun.Begin(LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: twinScript), SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            while (twinState.Day < 6) { CampaignRun.StepDay(twinState); }
            double charged = twinState.Pools[party].Money - chestAfterDay5;
            _sb.Append(F("      the chest at day 5's close: {0:N0} with the poll, {1:N0} without; at the campaign's close {2:N0} (the run's money left, spent out by the daily rally)\n", chestAfterDay5, twinState.Pools[party].Money, r.Parties[party].MoneyLeft));
            List<(int Day, int Region, Poll Poll)> bought = r.RegionalPolls[party];
            MapRegionReading[] readings = RegionalPolling.Readings(state, party);
            Check(r.Parties[party].Refused.Count(x => x.Item1 == 5 && x.Item2 == CampaignActionKind.CommissionPolling) == 1, "a second regional poll queued on day 5 is refused - one a day");
            int measured = readings.Count(x => x.Measured);
            Check(bought.Count == 1 && bought[0].Region == region && bought[0].Day == 5 && bought[0].Poll.SampleSize == expectedSample
                  && Math.Abs(charged - RegionalPolling.Cost) < 1e-6 && Math.Abs(r.Parties[party].PollMoney - RegionalPolling.Cost) < 1e-9 && readings[region].Measured && measured == 1,
                F("one regional poll of {0} bought on day 5: {1} filed (sample {2}; the roster that day fields {9} - a pollster multiplies the base {8}, none leaves it), {3:N0} off the chest at day 5's close against the same run without it (its price {4:N0}); the swing map reads {0} {5} (index {6:0}) and {7} other region(s) polled",
                    probe.Regions[region].Name, bought.Count, bought.Count > 0 ? bought[0].Poll.SampleSize : 0, charged, RegionalPolling.Cost,
                    readings[region].Measured ? RegionalPolling.StateOf(readings[region]).ToString() : "unknown", readings[region].SwingIndex, measured - (readings[region].Measured ? 1 : 0), RegionalPolling.Sample, expectedSample));
            double[][] shift = r.RegionalShift;
            double rowSum = shift.Max(row => Math.Abs(row.Sum()));
            double elsewhere = Enumerable.Range(0, shift.Length).Where(x => x != region).Max(x => shift[x][party]);
            Check(rowSum <= Exact && shift[region][party] > elsewhere,
                F("rallying in {0} moves the player's party most there: {1:+0.0000} points against at most {2:+0.0000} anywhere else; every valkrets's movements sum to zero ({3:E1})", probe.Regions[region].Name, 100.0 * shift[region][party], 100.0 * elsewhere, rowSum));

            // (7) the AI's value of one more rally there is the run's own regional difference
            CampaignPressure pressure = r.Pressure;
            double norm = CampaignPersuasion.ElectorateMean(s);
            CampaignActions.ChainTrace extra = Contact(CampaignActionKind.Rally, 20_000.0, norm);
            double valued = pressure.RegionalOwnView(party).ValuePp(ContactClass.Personal, extra.Exposure, extra.Persuasion / rally.PersuasionWeight, 0.0, region, norm);
            double beforeEffect = pressure.RegionEffects(region, norm, (p, slot) => p == party, (a, slot, t) => false, out _, out _)[party];
            pressure.Add(party, extra, region);
            double afterEffect = pressure.RegionEffects(region, norm, (p, slot) => p == party, (a, slot, t) => false, out _, out _)[party];
            double difference = 100.0 * pressure.RegionWeight(region) * (afterEffect - beforeEffect);
            Check(Math.Abs(valued - difference) <= 1e-9 && valued > 0.0,
                F("the AI values one more rally in {0} at {1:0.0000000} points, the run's own regional difference {2:0.0000000}", probe.Regions[region].Name, valued, difference));
        }

        /// <summary>(2) of the first run's findings: each staged country's game staging carries every region's offset (a regional poll on sale), or says why not.</summary>
        private static void Offsets()
        {
            var none = new (int, int, Scandal)[0];
            foreach (CountryId c in new[] { CountryId.Sweden, CountryId.Germany, CountryId.USA })
            {
                WorldClock.ApplyStart(c);
                CampaignCalendar? cal = null;
                if (c == CountryId.Germany) { cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(c), WorldClock.StartDate(c)); }
                if (!LiveCampaignSetup.TryFor(c, none, cal, out CampaignRun.Setup st, out string note, onVoteModelCompatibility: true)) { Check(false, F("no {0} staging: {1}", c, note)); continue; }
                string why = null;
                if (st.RegionalOffset == null)
                {
                    string[] keys = st.Parties.Select(x => x.Name).ToArray();
                    RegionalPolling.OffsetsFor(c, keys, st.PriorShares, c == CountryId.USA ? st.Calendar.ElectionDate : st.Calendar.CampaignStart, st.Regions, st.Stands, null, out why);
                }
                Check(st.RegionalOffset != null && st.RegionalOffset.Length == st.Regions.Length,
                    F("{0}'s game staging carries the offsets of all {1} regions, so a regional poll is on sale{2}", c, st.Regions.Length, why == null ? string.Empty : " - not: " + why));
            }
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "    ok    " : "    FAIL  ").Append(what).Append('\n');
            if (!ok) { _failures++; }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
