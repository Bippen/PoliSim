using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PL-C (`POLISIM_FEATURE_LIST.md` row PL-C; Elias's rulings K167-K169 of 2026-10-27 with K147): <b>A POLISH CAMPAIGN STAGED AND COUNTED</b>, proved where
    /// an Editor harness can prove it. Each statement below fails this check while it is false.
    /// <list type="number">
    /// <item><b>(1) The sources</b>: the spending-limit folder held to its SHA256SUMS; the statute's own amounts and divisor in the Kodeks wyborczy as
    /// published (art. 199 § 1: 82 groszy and /460; art. 327 § 1: 60 groszy) and the regulations' raised amounts (1 zł 10 gr and 1 zł 17 gr for the Sejm,
    /// 85 gr for the President) in their HTML texts; the generated wojewodztwa table re-read from its CSV, figure for figure.</item>
    /// <item><b>(2) The Sejm's staging</b> (K169): Poland's 2023 campaign on the 41 okregi - their audiences the PKW's valid list votes, their electorates
    /// the PKW's - each list where it stands (the MN in okreg 21 alone); a regional poll on sale; the doors at the turnout CACE.</item>
    /// <item><b>(3) The purse</b> (K167): a full committee's chest is art. 199 § 1's limit worked by hand (the register of M.P. 2023 poz. 867 times
    /// 1.10 zł), the MN's its twelve seats' share; a party scripted to buy television every day stops with less than one ad's price left of its limit,
    /// and no party spends past its own.</item>
    /// <item><b>(4) The campaign runs</b>: an idle campaign (no act, no office, no staff) reproduces the day's prediction; the AI campaigns (every
    /// standing party acts), each okreg's movement sums to zero.</item>
    /// <item><b>(5) The count reads the shift</b> (K147): a movement laid on one okreg (KO +1 point, PiS -1 in okreg 19) is that okreg's alone less its
    /// centring, each okreg still sums to its 2023 size and each list to its national share times the base, the count's national shares are the
    /// shares handed, d'Hondt by hand in every okreg is the chamber's; on a real run the count reads the campaign's okreg movement and its doors, the
    /// chamber is divided on the same votes and the night declares the chamber's seats.</item>
    /// <item><b>(6) The presidential campaign</b> (K168, K169): the first vote of 18 May 2025 staged on the 16 wojewodztwa (2020's first round), the
    /// purse art. 327 § 1's limit by hand (M.P. 2025 poz. 66 times 0.85 zł); its idle run the prediction, its AI run counted as the first vote's poll;
    /// the run-off staged on the 14 days between rounds with the two finalists alone (every other party standing nowhere, holding nothing), each
    /// finalist's chest its limit less its first round's spending; its idle run B4's share of the two, its AI run counted in B4's place.</item>
    /// </list>
    /// Prints `POLISH CAMPAIGN: PASS` and exits 0 when every check holds; each failure is a `FAIL` line naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.PolishCampaignDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class PolishCampaignDiagnostic
    {
        private const int Seed = 777;
        /// <summary>CONVENTION: a share or a fraction against its own arithmetic - the rounding of a double.</summary>
        private const double Exact = 1e-9;
        /// <summary>CONVENTION: a vote count against its own arithmetic.</summary>
        private const double VoteExact = 1e-4;
        /// <summary>CONVENTION: the test movement laid on one okreg (a fraction, one point) - a probe size, not a model figure.</summary>
        private const double Probe = 0.01;
        /// <summary>CONVENTION: the television ads the spender is scripted to buy each day - more than any chest affords.</summary>
        private const int AdsPerDay = 6;
        /// <summary>SOURCED: the register the PKW announced for the Sejm of 15 Oct 2023 (M.P. 2023 poz. 867) and for the President of 18 May 2025 (M.P. 2025 poz. 66).</summary>
        private const long Register2023 = 29_097_503L, Register2025 = 28_945_200L;
        /// <summary>SOURCED: the amounts per voter in force on each campaign's opening (Dz.U. 2023 poz. 132; Dz.U. 2024 poz. 590).</summary>
        private const double SejmPerVoter2023 = 1.10, PresidentialPerVoter2025 = 0.85;
        /// <summary>SOURCED: okreg 21's seats (`PolishSejmDistricts2023.Magnitudes`, the PKW's) - the MN's m.</summary>
        private const int Okreg21Seats = 12;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("POLISH CAMPAIGN: PL-C (K167-K169, K147) - a Polish campaign staged and counted\n");
            SimulationRandom.LiveState streams = SimulationRandom.CaptureLive();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Sources();
                    Sejm();
                    Presidential();
                }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally { SimulationRandom.Reinstate(streams); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "POLISH CAMPAIGN: PASS - the spending-limit sources held and read; the Sejm campaign staged on the 41 okregi (the MN in okreg 21 alone), its purse art. 199's limit by hand and a spender stopped at it; an idle campaign the prediction and the AI campaigning; the count lays one okreg's movement there alone, every okreg its size and every list its national share, the national total the shares handed, d'Hondt by hand the chamber's, and a real run's okreg movement and doors counted, the chamber and the night one count; the presidential first vote staged on the 16 wojewodztwa with art. 327's limit, its idle run the prediction and its AI run the first vote's poll; the run-off staged on the 14 days with the two finalists alone, their chests the limit less the first round, its idle run B4's share and its AI run counted in B4's place."
                : F("POLISH CAMPAIGN: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) the sources
        private static void Sources()
        {
            _sb.Append("  (1) the sources:\n");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland"));
            string limits = Path.Combine(root, "raw", "spending_limits");
            List<string> faults = ItalySeatingDiagnostic.DigestFaults(limits, out int held);
            Check(held >= 17 && faults.Count == 0, F("the spending-limit folder held to its SHA256SUMS.txt ({0} files){1}", held, faults.Count == 0 ? string.Empty : " - " + string.Join("; ", faults)));
            string code = Text(Path.Combine(root, "raw", "electoral_code", "eli_DU_2011_112_kodeks_wyborczy.html"));
            Check(code.Contains("kwotą 82 groszy przypadającą na każdego wyborcę") && code.Contains("/460,") && code.Contains("wyznaczona jest kwotą 60 groszy przypadającą na każdego wyborcę"),
                "the Kodeks wyborczy as published: art. 199 § 1's 82 groszy and its divisor 460, art. 327 § 1's 60 groszy");
            Check(Text(Path.Combine(limits, "eli_DU_2023_132.html")).Contains("1 złoty 10 groszy") && Text(Path.Combine(limits, "eli_DU_2024_605.html")).Contains("1 złoty 17 groszy")
                  && Text(Path.Combine(limits, "eli_DU_2024_590.html")).Contains("85 groszy"),
                "the regulations raise the amounts the staging reads: the Sejm's to 1 zł 10 gr (Dz.U. 2023 poz. 132) and 1 zł 17 gr (2024 poz. 605), the President's to 85 gr (2024 poz. 590)");

            // the generated wojewodztwa table against its CSV
            var valid = new Dictionary<string, long>(StringComparer.Ordinal);
            var eligible = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(Path.Combine(root, "presidential_votes_by_voivodeship.csv"), new UTF8Encoding(false)))
            {
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("election,", StringComparison.Ordinal) || line.Length == 0) { continue; }
                string[] f = line.Split(',');
                if (f.Length != 6 || f[1] != "1") { continue; }
                string key = f[0] + "|" + f[3];
                if (f[4] == "__VALID__") { valid[key] = long.Parse(f[5], CultureInfo.InvariantCulture); }
                if (f[4] == "__ELIGIBLE__") { eligible[key] = long.Parse(f[5], CultureInfo.InvariantCulture); }
            }
            int differ = 0;
            for (int r = 0; r < PolishWojewodztwa.Names.Length; r++)
            {
                string n = PolishWojewodztwa.Names[r];
                long abroadV = n == "mazowieckie" ? PolishWojewodztwa.AbroadValid2020 : 0L, abroadE = n == "mazowieckie" ? PolishWojewodztwa.AbroadEligible2020 : 0L;   // 2020 counts the voters abroad and on ships inside mazowieckie
                if (!valid.TryGetValue("2020|" + n, out long v20) || v20 - abroadV != PolishWojewodztwa.Valid2020[r] || !eligible.TryGetValue("2020|" + n, out long e20) || e20 - abroadE != PolishWojewodztwa.Eligible2020[r]
                    || !valid.TryGetValue("2025|" + n, out long v25) || v25 != PolishWojewodztwa.Valid2025[r] || !eligible.TryGetValue("2025|" + n, out long e25) || e25 != PolishWojewodztwa.Eligible2025[r]) { differ++; }
            }
            List<string> abroadFaults = ItalySeatingDiagnostic.DigestFaults(Path.Combine(root, "raw", "wojewodztwa_abroad_2020"), out int abroadHeld);
            Check(abroadHeld == 1 && abroadFaults.Count == 0 && PolishWojewodztwa.AbroadValid2020 > 0 && PolishWojewodztwa.AbroadEligible2020 > PolishWojewodztwa.AbroadValid2020,
                F("the 2020 abroad rows' KBW powiat file held to its SHA256SUMS ({0} held); {1:N0} valid votes and {2:N0} electors taken out of mazowieckie", abroadHeld, PolishWojewodztwa.AbroadValid2020, PolishWojewodztwa.AbroadEligible2020));
            Check(PolishWojewodztwa.Names.Length == 16 && differ == 0, F("the generated wojewodztwa table is its CSV's own, 2020's mazowieckie less its voters abroad and on ships: 16 wojewodztwa, {0} differing", differ));
        }

        private static string Text(string path) => File.Exists(path) ? File.ReadAllText(path, new UTF8Encoding(false)).Replace("&nbsp;", " ") : string.Empty;

        // ============================================================ (2)-(5) the Sejm
        private static void Sejm()
        {
            _sb.Append("  (2) the Sejm's staging:\n");
            WorldClock.ApplyStart(CountryId.Poland);
            CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Poland), WorldClock.StartDate(CountryId.Poland));
            var none = new (int, int, Scandal)[0];
            bool staged = LiveCampaignSetup.TryFor(CountryId.Poland, none, cal, out CampaignRun.Setup setup, out string note);
            Check(staged, "Poland's campaign is staged for the Sejm of " + Day(cal.ElectionDate) + (staged ? string.Empty : " - " + note));
            if (!staged) { return; }
            _sb.Append(note);
            string[] keys = LiveCampaignSetup.Keys(CountryId.Poland);
            Check(setup.Contest == CampaignContest.Chamber && setup.Regions.Length == 41 && setup.Regions.Select(r => r.Name).SequenceEqual(PolishOkregElectorates2023.Names),
                F("the contest is the chamber's, on the 41 okregi by the PKW's numbers ({0} regions)", setup.Regions.Length));
            Check(setup.Regions.Sum(r => r.Audience) == PolishOkregElectorates2023.Valid.Sum() && setup.Regions.Sum(r => r.Eligible) == PolishOkregElectorates2023.Eligible.Sum(),
                F("the okregi's audiences the PKW's valid list votes ({0:N0}) and their electorates the PKW's ({1:N0})", setup.Regions.Sum(r => r.Audience), setup.Regions.Sum(r => r.Eligible)));
            int mn = Array.IndexOf(keys, "MN");
            int okreg21 = Array.IndexOf(PolishOkregElectorates2023.Names, "okreg 21");
            bool standsRight = true;
            for (int p = 0; p < keys.Length; p++)
            {
                int where = Enumerable.Range(0, 41).Count(r => setup.StandsIn(p, r));
                bool expected = p == mn ? where == 1 && setup.StandsIn(p, okreg21) : setup.PriorShares[p] > 0.0 ? where == 41 : where == 0;
                if (!expected) { standsRight = false; }
            }
            Check(mn >= 0 && standsRight, "each list stands where the seated election's candidacies say: the MN in okreg 21 alone, a list with a prior share in every okreg, one with none nowhere");
            Check(setup.RegionalOffset != null && setup.RegionalOffset.Length == 41 && setup.DoorCacePp == GotvTurnout.EuropeanDoorCacePp,
                F("a regional poll is on sale (41 offsets on the count's own swing) and the doors count at the turnout CACE ({0:0.00} points)", setup.DoorCacePp));

            // (3) the purse
            _sb.Append("  (3) the purse (K167):\n");
            double full = Register2023 * SejmPerVoter2023;   // art. 199 § 1 by hand: w x k x 460 / 460
            double mnLimit = Register2023 * SejmPerVoter2023 * Okreg21Seats / PolishCampaign.SejmSeats;
            bool chests = true;
            for (int p = 0; p < keys.Length; p++)
            {
                double limit = p == mn ? mnLimit : setup.PriorShares[p] > 0.0 ? full : 0.0;
                if (Math.Abs(setup.Parties[p].StartingMoney * setup.PurseUnit - limit) > 1e-6 * Math.Max(1.0, limit)) { chests = false; }
            }
            Check(Math.Abs(setup.PurseUnit * LiveCampaignSetup.WarChest - full) < 1e-6 * full && chests && PolishSejmDistricts2023.Magnitudes[okreg21] == Okreg21Seats,
                F("each chest is its limit: a full committee {0:N2} zł (29 097 503 x 1.10 zł), the MN {1:N2} zł (12 of 460 seats), a list standing nowhere none; one unit {2:N4} zł", full, mnLimit, setup.PurseUnit));
            int ko = Array.IndexOf(keys, "KO");
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            Func<int, AiDecision[]> spender = d => Enumerable.Range(0, AdsPerDay).Select(_ => new AiDecision(CampaignActionKind.TelevisionAd, new CampaignActions.ActionTarget(-1, -1, null), "national", tv.MoneyCost, tv.Hours, 0.0, false)).ToArray();
            LiveCampaignSetup.TryFor(CountryId.Poland, none, cal, out CampaignRun.Setup spendSetup, out string _, playerParty: ko, playerScript: spender);
            CampaignRun.Result spent = CampaignAiHarness.RunSeeded(spendSetup, Seed);
            double koSpent = spendSetup.Parties[ko].StartingMoney - spent.Parties[ko].MoneyLeft;
            int ads = spent.Parties[ko].ActionCount[Array.IndexOf(CampaignActions.TheEight, CampaignActionKind.TelevisionAd)];
            bool nobodyPast = Enumerable.Range(0, keys.Length).All(p => spent.Parties[p].MoneyLeft >= 0.0 && spendSetup.Parties[p].StartingMoney - spent.Parties[p].MoneyLeft <= spendSetup.Parties[p].StartingMoney + 1e-9);
            Check(ko >= 0 && spent.Parties[ko].MoneyLeft < tv.MoneyCost && koSpent * spendSetup.PurseUnit <= full + 1e-6 && ads < AdsPerDay * spent.DaysRun && nobodyPast,
                F("the cap binds: KO scripted to {0} ads a day bought {1} of {2}, spent {3:N0} zł of its {4:N0} zł limit and stopped with {5:N0} zł - less than one ad - left; no party spent past its own", AdsPerDay, ads,
                    AdsPerDay * spent.DaysRun, koSpent * spendSetup.PurseUnit, full, spent.Parties[ko].MoneyLeft * spendSetup.PurseUnit));

            // L1 (the review): the cap holds because no Polish act adds money - the one act that can (the run-up's mailing) planted, and refused
            PreCampaignRun.State runUp = PreCampaignRun.Begin(setup, ko, new System.Random(Seed), CountryId.Poland);
            double before = runUp.Money;
            PreCampaignRun.StepDay(runUp, new List<PreCampaignRun.Decision> { new PreCampaignRun.Decision(CampaignActionKind.Fundraise) });
            List<PreCampaignRun.Entry> entries = PreCampaignRun.EntriesOn(runUp, 0);
            Check(!PreCampaignRun.FundraiseOpen(runUp) && runUp.Money <= before && entries.Count > 0 && entries.All(e => e.Kind != CampaignActionKind.Fundraise || e.Refusal != null),
                F("a planted income is refused: KO's run-up mailing in Poland ({0}) - the chest {1:N0} before, {2:N0} after; no Polish act raises money, so a committee's spending stops at its limit",
                    entries.Where(e => e.Kind == CampaignActionKind.Fundraise).Select(e => e.Refusal ?? "NOT REFUSED").FirstOrDefault() ?? "no entry", before, runUp.Money));

            // (4) the campaign runs
            _sb.Append("  (4) the campaign:\n");
            CampaignRun.Result idle = CampaignAiHarness.RunSeeded(Idle(setup), Seed);
            NationalElection.TryPredictShares(CountryId.Poland, out Dictionary<string, double> predicted, null, cal.ElectionDate);
            double idleMiss = Enumerable.Range(0, keys.Length).Max(p => Math.Abs(idle.FinalShares[p] - (predicted != null && predicted.TryGetValue(keys[p], out double v) ? v : double.NaN)));
            Check(idleMiss < Exact, F("an idle campaign (no act, office or staff) is the day's prediction: the largest miss {0:0.0e+0}", idleMiss));
            CampaignRun.Result run = CampaignAiHarness.RunSeeded(setup, Seed);
            int actors = 0, standing = 0;
            for (int p = 0; p < keys.Length; p++) { if (setup.Parties[p].StartingMoney >= LiveCampaignSetup.WarChest) { standing++; if (run.Parties[p].ActionCount.Sum() > 0) { actors++; } } }   // every full committee (the MN's twelve seats' chest is read, not judged)
            double rowWorst = run.RegionalShift == null ? double.NaN : run.RegionalShift.Max(row => Math.Abs(row.Sum()));
            Check(standing > 0 && actors == standing && run.RegionalShift != null && run.RegionalShift.Length == 41 && rowWorst < 1e-12,
                F("the AI campaigns: {0} of {1} full committees acted over {2} days (the MN {4} act(s)); each okreg's movement sums to zero (worst {3:0.0e+0})", actors, standing, run.DaysRun, rowWorst, mn >= 0 ? run.Parties[mn].ActionCount.Sum() : 0));
            _sb.Append("    read      the AI run's final shares:" + string.Concat(Enumerable.Range(0, keys.Length).Select(p => F(" {0} {1:0.00}", keys[p], 100.0 * run.FinalShares[p])))).Append('\n');

            // (5) the count reads the shift
            _sb.Append("  (5) the count (K147):\n");
            Probe5(setup, keys, predicted, cal.ElectionDate);
            var handed = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int p = 0; p < keys.Length; p++) { handed[keys[p]] = run.FinalShares[p]; }
            using (ParliamentaryCount.NoiseAtZero())
            {
                ParliamentaryCount.Result counted = ParliamentaryCount.Count(CountryId.Poland, cal.ElectionDate, handed, setup, run.Gotv, false, run.RegionalShift);
                if (counted == null) { Check(false, "the count of the AI run returned nothing"); return; }
                double back = handed.Keys.Sum(k => handed[k] + counted.GroundPp[k] / 100.0);
                double handBack = handed.Keys.Max(k => Math.Abs(counted.Shares[k] - (handed[k] + counted.GroundPp[k] / 100.0) / back));
                Check(counted.OkregShift != null && counted.CampaignRegionsMoved > 0 && !double.IsNaN(counted.Turnout) && handBack < Exact,
                    F("a real run's count reads its okreg movement ({0} okregi moved) and its doors (turnout {1:0.00} %, the ground game moving a share by up to {2:0.000} points), the national shares the shares handed plus the doors (worst {3:0.0e+0})",
                        counted.CampaignRegionsMoved, 100.0 * counted.Turnout, counted.GroundPp.Values.Max(Math.Abs), handBack));
                ElectionRecord record = NationalElection.Run(CountryId.Poland, 0, counted.Shares, cal.ElectionDate, 1.0, counted);
                Dictionary<string, int> divided = PolishSejmAllocation.Allocate(record.Shares, out List<Dictionary<string, int>> _, 1.0, counted.OkregShift);
                Dictionary<string, int> unshifted = PolishSejmAllocation.Allocate(record.Shares, out List<Dictionary<string, int>> _, 1.0);
                PolishSejmNight.State night = PolishSejmNight.At(PolishSejmNight.FinalMinute, record.Shares, record.OkregShift);
                bool nightIsChamber = night != null && Enumerable.Range(0, night.Keys.Length).All(k => (record.Seats.TryGetValue(night.Keys[k], out int s) ? s : 0) == night.Seats[k]);
                Check(ReferenceEquals(record.OkregShift, counted.OkregShift) && record.Seats.Values.Sum() == 460 && record.Seats.All(kv => divided[kv.Key] == kv.Value) && nightIsChamber,
                    F("the chamber is divided on the votes the count counted (460 seats: {0}) and the night declares the chamber's seats; the okreg movement alone moved {1} seat(s) against the swing without it",
                        string.Join(" ", record.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value)), record.Seats.Sum(kv => Math.Abs(kv.Value - (unshifted.TryGetValue(kv.Key, out int u) ? u : 0))) / 2));
            }
        }

        /// <summary>(5): the probe laid on okreg 19 - the swing's identity, its centring, the count's national total and d'Hondt by hand.</summary>
        private static void Probe5(CampaignRun.Setup setup, string[] keys, Dictionary<string, double> predicted, DateTime day)
        {
            if (predicted == null) { Check(false, "no prediction to lay the probe on"); return; }
            int ko = Array.IndexOf(keys, "KO"), pis = Array.IndexOf(keys, "PiS");
            int okreg19 = Array.IndexOf(PolishOkregElectorates2023.Names, "okreg 19");
            var regional = new double[41][];
            for (int r = 0; r < 41; r++) { regional[r] = new double[keys.Length]; }
            regional[okreg19][ko] = Probe;
            regional[okreg19][pis] = -Probe;
            var ordered = predicted.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            var shift = new Dictionary<string, double[]>(StringComparer.Ordinal);
            foreach (string k in ordered) { int p = Array.IndexOf(keys, k); shift[k] = Enumerable.Range(0, 41).Select(r => p >= 0 ? regional[r][p] : 0.0).ToArray(); }
            double[][] plain = PolishSejmNight.DistrictVotes(ordered, predicted);
            double[][] moved = PolishSejmNight.DistrictVotes(ordered, predicted, shift);
            long[] size = PolishSejmDistricts2023.Votes.Select(row => row.Sum()).ToArray();
            long baseVotes = size.Sum();
            double w19 = (double)size[okreg19] / baseVotes;
            int iKo = ordered.IndexOf("KO"), iPis = ordered.IndexOf("PiS");
            bool floors = moved.Any(row => row.Any(v => v <= 0.0)) || plain.Any(row => row.Any(v => v <= 0.0));
            double sizeWorst = Enumerable.Range(0, 41).Max(d => Math.Abs(moved[d].Sum() - plain[d].Sum()));
            double nationalWorst = Enumerable.Range(0, ordered.Count).Max(k => Math.Abs(Enumerable.Range(0, 41).Sum(d => moved[d][k]) - Enumerable.Range(0, 41).Sum(d => plain[d][k])));
            double koThere = (moved[okreg19][iKo] - plain[okreg19][iKo]) / size[okreg19], koElsewhere = (moved[0][iKo] - plain[0][iKo]) / size[0];
            double pisThere = (moved[okreg19][iPis] - plain[okreg19][iPis]) / size[okreg19];
            _sb.Append(F("    read      lists that the swing floors at zero somewhere: {0} (MN stands in okreg 21 alone, so its zeros elsewhere are no floor)\n", floors ? "some" : "none"));
            Check(Math.Abs(koThere - Probe * (1.0 - w19)) < Exact && Math.Abs(koElsewhere + Probe * w19) < Exact && Math.Abs(pisThere + Probe * (1.0 - w19)) < Exact,
                F("the movement laid on okreg 19 is that okreg's less its centring: KO {0:+0.000000;-0.000000} there (the probe times 1 - {1:0.0000}), {2:+0.000000;-0.000000} in okreg 1; PiS {3:+0.000000;-0.000000} there", koThere, w19, koElsewhere, pisThere));
            Check(sizeWorst < VoteExact && nationalWorst < VoteExact,
                F("each okreg still counts its 2023 size (worst {0:0.0e+0} votes) and each list its national total (worst {1:0.0e+0} votes) - the national total the count was handed", sizeWorst, nationalWorst));
            using (ParliamentaryCount.NoiseAtZero())
            {
                ParliamentaryCount.Result counted = ParliamentaryCount.Count(CountryId.Poland, day, predicted, setup, null, false, regional);
                double toHanded = counted == null ? double.NaN : predicted.Keys.Max(k => Math.Abs(counted.Shares[k] - predicted[k] / predicted.Values.Sum()));
                Check(counted != null && counted.CampaignRegionsMoved == 1 && counted.OkregShift != null && counted.OkregShift["KO"][okreg19] == Probe && toHanded < Exact,
                    F("the count lays it on (1 okreg moved, KO +{0} there as staged) and its national shares are the shares handed (worst {1:0.0e+0}, no doors, sigma 0)", Probe, toHanded));
            }
            // L2 (the review): the sole committee's hand-back - the MN +probe in okreg 21 and KO -probe there; the MN's own column is no movement (its national change
            // places it), and the lists that stood in several okregi keep every okreg's size and their national totals
            int okreg21 = Array.IndexOf(PolishOkregElectorates2023.Names, "okreg 21");
            var soleShift = new Dictionary<string, double[]>(StringComparer.Ordinal);
            foreach (string k in ordered) { soleShift[k] = new double[41]; }
            if (soleShift.ContainsKey("MN") && soleShift.ContainsKey("KO")) { soleShift["MN"][okreg21] = Probe; soleShift["KO"][okreg21] = -Probe; }
            double[][] soleMoved = PolishSejmNight.DistrictVotes(ordered, predicted, soleShift);
            int iMn = ordered.IndexOf("MN");
            double soleSize = Enumerable.Range(0, 41).Max(d => Math.Abs(soleMoved[d].Sum() - plain[d].Sum()));
            double soleNational = Enumerable.Range(0, ordered.Count).Max(k => Math.Abs(Enumerable.Range(0, 41).Sum(d => soleMoved[d][k]) - Enumerable.Range(0, 41).Sum(d => plain[d][k])));
            double mnSelf = iMn >= 0 ? Enumerable.Range(0, 41).Max(d => Math.Abs(soleMoved[d][iMn] - plain[d][iMn])) : double.NaN;
            double koIn21 = (soleMoved[okreg21][iKo] - plain[okreg21][iKo]) / size[okreg21];
            Check(iMn >= 0 && soleSize < VoteExact && soleNational < VoteExact && mnSelf < VoteExact && Math.Abs(koIn21) < Probe,
                F("the sole committee's hand-back: the MN moved +{0} in okreg 21 moves no MN vote (worst {1:0.0e+0}); every okreg keeps its size ({2:0.0e+0}) and every list its national total ({3:0.0e+0}); KO there {4:+0.000000;-0.000000}, the MN's take handed back to it in part",
                    Probe, mnSelf, soleSize, soleNational, koIn21));

            // d'Hondt by hand in every okreg, on the moved votes, against the chamber's
            var admitted = ordered.Select(k => PolishSejmAllocation.LineOf(k) <= predicted[k] / predicted.Values.Sum()).ToArray();
            Dictionary<string, int> chamber = PolishSejmAllocation.Allocate(predicted, out List<Dictionary<string, int>> perDistrict, 1.0, shift);
            int wrong = 0;
            for (int d = 0; d < 41; d++)
            {
                var quotients = new List<(double Q, double Votes, int K)>();
                for (int k = 0; k < ordered.Count; k++)
                {
                    if (!admitted[k] || moved[d][k] <= 0.0) { continue; }
                    for (int j = 1; j <= PolishSejmDistricts2023.Magnitudes[d]; j++) { quotients.Add((moved[d][k] / j, moved[d][k], k)); }
                }
                var top = quotients.OrderByDescending(x => x.Q).ThenByDescending(x => x.Votes).ThenBy(x => ordered[x.K], StringComparer.Ordinal).Take(PolishSejmDistricts2023.Magnitudes[d]).ToList();
                for (int k = 0; k < ordered.Count; k++)
                {
                    int byHand = top.Count(x => x.K == k);
                    int got = perDistrict[d].TryGetValue(ordered[k], out int s) ? s : 0;
                    if (byHand != got) { wrong++; }
                }
            }
            Check(wrong == 0 && chamber.Values.Sum() == 460, F("d'Hondt by hand in each of the 41 okregi on the moved votes is the chamber's ({0} list-okreg cells differ; 460 seats)", wrong));
        }

        // ============================================================ (6) the presidency
        private static void Presidential()
        {
            _sb.Append("  (6) the presidential campaign (K168, K169):\n");
            TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 1, out DateTime firstVote);
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(firstVote.AddDays(-1));   // E1's reference world's day, before the world exists
            World world = WorldFactory.CreateDefault();
            Country pl = world.GetCountry(CountryId.Poland);
            var none = new (int, int, Scandal)[0];
            var cal = new CampaignCalendar(firstVote);
            bool staged = LiveCampaignSetup.TryFor(CountryId.Poland, none, cal, out CampaignRun.Setup setup, out string note, played: pl);
            Check(staged && setup.Contest == CampaignContest.PresidentialFirstVote, "the first vote of " + Day(firstVote) + " is staged as a presidential campaign" + (staged ? string.Empty : " - " + note));
            if (!staged) { return; }
            _sb.Append(note);
            string[] keys = LiveCampaignSetup.Keys(CountryId.Poland);
            Check(setup.Regions.Length == 16 && setup.Regions.Select(r => r.Name).SequenceEqual(PolishWojewodztwa.Names) && setup.Regions.Sum(r => r.Audience) == PolishWojewodztwa.Valid2020.Sum()
                  && setup.Regions.Sum(r => r.Eligible) == PolishWojewodztwa.Eligible2020.Sum() && setup.RegionalOffset == null && setup.DoorCacePp == 0.0,
                F("on the 16 wojewodztwa of 2020's first round (audience {0:N0}, electorate {1:N0}); no regional poll and no door turnout - the count is national", setup.Regions.Sum(r => r.Audience), setup.Regions.Sum(r => r.Eligible)));
            double full = Register2025 * PresidentialPerVoter2025;   // art. 327 § 1 by hand: w x k
            bool chests = Enumerable.Range(0, keys.Length).All(p => setup.PriorShares[p] > 0.0 ? Math.Abs(setup.Parties[p].StartingMoney * setup.PurseUnit - full) < 1e-6 * full : setup.Parties[p].StartingMoney == 0.0);
            Check(chests, F("each candidate's committee holds its limit: {0:N2} zł (28 945 200 x 0.85 zł)", full));

            CampaignRun.Result idle = CampaignAiHarness.RunSeeded(Idle(setup), Seed);
            NationalElection.TryPredictShares(CountryId.Poland, out Dictionary<string, double> predicted, null, firstVote, pl);
            double idleMiss = Enumerable.Range(0, keys.Length).Max(p => Math.Abs(idle.FinalShares[p] - (predicted != null && predicted.TryGetValue(keys[p], out double v) ? v : double.NaN)));
            Check(idleMiss < Exact, F("the idle first-round campaign is the day's prediction: the largest miss {0:0.0e+0}", idleMiss));
            CampaignRun.Result run = CampaignAiHarness.RunSeeded(setup, Seed);
            int actors = Enumerable.Range(0, keys.Length).Count(p => setup.Parties[p].StartingMoney > 0.0 && run.Parties[p].ActionCount.Sum() > 0);
            int standing = Enumerable.Range(0, keys.Length).Count(p => setup.Parties[p].StartingMoney > 0.0);
            Check(standing > 0 && actors == standing, F("the AI campaigns for the first vote: {0} of {1} standing parties acted", actors, standing));

            var poll = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int p = 0; p < keys.Length; p++) { poll[keys[p]] = run.FinalShares[p]; }
            PresidentialElection.Contest contest = PresidentialElection.HoldRounds(pl, firstVote, poll);
            Check(contest != null && contest.Field.Count > 1, "the first vote counts the campaign's final shares as its poll: " + (contest?.Line ?? "nothing held"));
            if (contest == null || !contest.RunOffPending()) { Check(false, "a run-off to stage - " + (contest == null ? "no contest" : "the first vote decided it")); return; }
            contest.FirstRoundSpent = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int p = 0; p < keys.Length; p++) { contest.FirstRoundSpent[keys[p]] = Math.Max(0.0, (setup.StandingShare(p) > 0.0 ? LiveCampaignSetup.WarChest : 0.0) - run.Parties[p].MoneyLeft); }   // as the game keeps it

            CampaignCalendar runOff = LiveCampaignSetup.PolandRunOffCalendar(contest.RunOffOn);
            Check(runOff.TotalCampaignDays == 14 && runOff.CampaignStart == firstVote && runOff.PreCampaignWeeks == 0 && contest.RunOffOn == firstVote.AddDays(14)
                  && PolishCampaign.ContestOn(contest.RunOffOn, pl, out PresidentialElection.Contest found) == CampaignContest.PresidentialRunOff && ReferenceEquals(found, contest),
                F("the run-off's calendar: the 14 days from the first vote to {0}, no run-up, and the day reads as the run-off", Day(contest.RunOffOn)));
            bool roStaged = LiveCampaignSetup.TryFor(CountryId.Poland, none, runOff, out CampaignRun.Setup ro, out string roNote, played: pl);
            Check(roStaged && ro.Contest == CampaignContest.PresidentialRunOff, "the run-off is staged" + (roStaged ? string.Empty : " - " + roNote));
            if (!roStaged) { return; }
            _sb.Append(roNote);
            int ia = Array.IndexOf(keys, contest.Field.Find(c => c.Name == contest.RunOffA)?.Party), ib = Array.IndexOf(keys, contest.Field.Find(c => c.Name == contest.RunOffB)?.Party);
            bool onlyTwo = ia >= 0 && ib >= 0 && Enumerable.Range(0, keys.Length).All(p => (p == ia || p == ib)
                ? Enumerable.Range(0, 16).All(r => ro.StandsIn(p, r)) && Math.Abs(ro.Parties[p].StartingMoney - (LiveCampaignSetup.WarChest - contest.FirstRoundSpent[keys[p]])) < 1e-6
                : Enumerable.Range(0, 16).All(r => !ro.StandsIn(p, r)) && ro.Parties[p].StartingMoney == 0.0 && ro.Parties[p].Offices.Length == 0);
            Check(onlyTwo, F("the two finalists alone campaign - {0} ({1}) and {2} ({3}) in every wojewodztwo, each with its limit less its first round ({4:N0} zł and {5:N0} zł left); every other party stands nowhere and holds nothing",
                contest.RunOffA, ia >= 0 ? keys[ia] : "?", contest.RunOffB, ib >= 0 ? keys[ib] : "?", ia >= 0 ? ro.Parties[ia].StartingMoney * ro.PurseUnit : 0.0, ib >= 0 ? ro.Parties[ib].StartingMoney * ro.PurseUnit : 0.0));
            if (ia < 0 || ib < 0) { return; }
            double b4 = PresidentialElection.RunOffShareOf(contest, out double _);
            CampaignRun.Result roIdle = CampaignAiHarness.RunSeeded(Idle(ro), Seed);
            double idleShare = 100.0 * roIdle.FinalShares[ia] / (roIdle.FinalShares[ia] + roIdle.FinalShares[ib]);
            Check(Math.Abs(idleShare - b4) < 1e-7, F("the idle run-off is B4's transfer: {0} {1:0.000000} % of the two against B4's {2:0.000000} %", contest.RunOffA, idleShare, b4));
            CampaignRun.Result roRun = CampaignAiHarness.RunSeeded(ro, Seed);
            double stray = roRun.RegionalShift == null ? double.NaN : roRun.RegionalShift.Max(row => Enumerable.Range(0, keys.Length).Where(p => p != ia && p != ib).Select(p => Math.Abs(row[p])).DefaultIfEmpty(0.0).Max());
            Check(roRun.DaysRun == 14 && roRun.Parties[ia].ActionCount.Sum() > 0 && roRun.Parties[ib].ActionCount.Sum() > 0 && stray < 1e-15,
                F("the finalists campaign the {0} days (acts: {1} {2}, {3} {4}); no other party moves a vote anywhere (largest movement {5:0.0e+0})", roRun.DaysRun, keys[ia], roRun.Parties[ia].ActionCount.Sum(), keys[ib], roRun.Parties[ib].ActionCount.Sum(), stray));
            double share = 100.0 * roRun.FinalShares[ia] / (roRun.FinalShares[ia] + roRun.FinalShares[ib]);
            PresidentialElection.Contest held = PresidentialElection.HoldRounds(pl, contest.RunOffOn, null, share);
            Check(ReferenceEquals(held, contest) && contest.Decided() && contest.RunOffShareA == share && Math.Abs(contest.RunOffTransferShareA - b4) < 1e-12 && contest.Line.Contains("the finalists' campaign between the rounds"),
                F("the run-off counts the campaign's share of the two in B4's place: {0}", contest.Line));
        }

        /// <summary>The staging with every party idle - scripted to nothing, no office, no staff, no buy - so the run is the prior's own preference.</summary>
        private static CampaignRun.Setup Idle(CampaignRun.Setup s)
        {
            var parties = s.Parties.Select(p => new CampaignRun.PartySetup(p.Name, p.Personality, p.Credibility, p.StartingMoney, p.TrueIssueMatch, 0, p.Candidate, new int[0], 0.0, new StaffRole[0], 0,
                d => new AiDecision[0], null)).ToArray();
            return new CampaignRun.Setup(s.Calendar, parties, s.PriorShares, s.LoyaltyPerParty, s.Compatibility, s.TrueSalience, s.NationalAudience, s.Regions, s.PublicHouse, s.PublicPollEveryDays,
                s.InternalHouse, s.ElectorateLoyalty, s.Outlets, s.DebateDays, s.Scandals, 0.0, s.RecordShift, s.Families, s.AwarenessStart, s.Grouping, s.Positions, s.Stands, s.Targeting, s.DoorCacePp,
                s.RegionalOffset, s.PurseUnit, s.Contest);
        }

        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "    ok    " : "    FAIL  ").Append(what).Append('\n');
            if (!ok) { _failures++; }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
