using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K-1 part (0), ordered 2026-09-23: THE OUT-OF-SAMPLE TEST, ONCE AND KEPT - the model's own prediction for the 2026 Riksdag election, run
    /// from the 2022 seed, against Valmyndigheten's final result (fixed 19 September 2026), per party, BEFORE anything is refreshed.
    ///
    /// <para><b>What "the model's own prediction" is here.</b> `NationalElection.TryPredictShares(Sweden)` - the campaign-free prediction: the fitted
    /// electorate, CHES 2024 positions, the 2022 shares as the prior and 2018→2022 loyalty, blended by `PreferenceModel`. It reads no World, no date and
    /// no seed, so "run from the 2022 seed" needs no simulated run: advancing the simulation would not change it (`PartySystems.TryElectorate`'s standing
    /// gap - the electorate does not move with the simulation). Its inputs are numerically unchanged since the last commit before the polls closed, so it
    /// is an ex-ante forecast. The campaign path is NOT run: its code changed after polling day and it depends on the player's party.</para>
    ///
    /// <para><b>Once.</b> A guard refuses to run once the seed is no longer 2022's (the live prior re-derived from the generated 2022 catalog, and the
    /// seeded seats against the 2022 count through the statute's own procedure), and the kept record is never overwritten. Not enrolled in any bar.</para>
    ///
    /// <para><b>Read with the benchmarks.</b> Loyalty keeps most of each party's 2022 share (78-96 %), so the prediction is prior-dominated; the no-change
    /// forecast (the 2022 shares) and the bare spatial layer are printed on the same basis, or the deviation cannot be read.</para>
    /// </summary>
    public static class OutOfSample2026Diagnostic
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string RecordRelative = "ElectionsData/sweden/2026/oos_prediction_2026.md";
        private const string VotesRelative = "ElectionsData/sweden/2026/valkrets_votes_2026.csv";
        private const string SeatsRelative = "ElectionsData/sweden/2026/valkrets_seats_2026.csv";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string recordPath = Path.Combine(root, RecordRelative);
            string commit = Arg("-oosCommit=") ?? "(not given)";
            var sb = new StringBuilder();
            string F(string f, params object[] a) => string.Format(Inv, f, a);

            if (File.Exists(recordPath)) { Debug.LogError("OOS: the kept record exists (" + RecordRelative + ") - the test runs ONCE; nothing was written."); CheckExit.Finish(1); return; }

            // (a) THE GUARD - the seed is still 2022's, derived, never transcribed
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Sweden);
            string[] catalogParties = SwedishValkretsReturns2022.Parties;
            long valid2022 = SwedishValkretsReturns2022.Valid.Sum();
            if (!PartySystems.TryHistory(CountryId.Sweden, out double[] latest, out double[] previous)) { Debug.LogError("OOS: Sweden has no history - nothing to test."); CheckExit.Finish(1); return; }
            var guardFail = new List<string>();
            for (int i = 0; i < parties.Count; i++)
            {
                int c = Array.IndexOf(catalogParties, parties[i].Abbrev);
                if (c < 0) { guardFail.Add(parties[i].Abbrev + " is not in the 2022 catalog"); continue; }
                long votes = 0; foreach (long[] row in SwedishValkretsReturns2022.Votes) { votes += row[c]; }
                double pct = Math.Round(100.0 * votes / valid2022, 2, MidpointRounding.AwayFromZero);
                if (Math.Abs(pct - latest[i]) > 0.005) { guardFail.Add(F("{0}: the live prior {1:0.00} is not the 2022 catalog's {2:0.00}", parties[i].Abbrev, latest[i], pct)); }
            }
            double[] eligible2022 = SwedishValkretsReturns2022.Eligible.Select(e => (double)e).ToArray();
            SeatConversion.Result seats2022 = SeatConversion.Sweden(SwedishValkretsReturns2022.Votes, eligible2022);
            for (int i = 0; i < parties.Count; i++)
            {
                int c = Array.IndexOf(catalogParties, parties[i].Abbrev);
                if (c >= 0 && seats2022.Seats[c] != parties[i].SeedSeats) { guardFail.Add(F("{0}: the seeded seats {1} are not the 2022 count's {2}", parties[i].Abbrev, parties[i].SeedSeats, seats2022.Seats[c])); }
            }
            if (guardFail.Count > 0) { Debug.LogError("OOS: THE SEED HAS MOVED - the out-of-sample test is the kept record, not a re-run:\n  " + string.Join("\n  ", guardFail)); CheckExit.Finish(1); return; }
            sb.Append("GUARD: the live prior equals the 2022 catalog's shares for all eight, and the seeded seats equal the 2022 count through SeatConversion.Sweden - the seed is 2022's.\n");

            // the real result, read from the sourced catalog at run time
            string votesPath = Path.Combine(root, VotesRelative), seatsPath = Path.Combine(root, SeatsRelative);
            if (!File.Exists(votesPath) || !File.Exists(seatsPath)) { Debug.LogError("OOS: the 2026 catalog is not on disk (" + VotesRelative + ", " + SeatsRelative + ")."); CheckExit.Finish(1); return; }
            ReadCsv(votesPath, out string[] vHeader, out List<string[]> vRows);
            ReadCsv(seatsPath, out string[] sHeader, out List<string[]> sRows);
            string[] realParties = { "S", "M", "SD", "C", "V", "KD", "L", "MP" };   // the catalog's own columns, checked against its header below
            foreach (string p in realParties) { if (Array.IndexOf(vHeader, p) < 0 || Array.IndexOf(sHeader, p) < 0) { Debug.LogError("OOS: the 2026 catalog has no column " + p); CheckExit.Finish(1); return; } }
            var realVotes = new Dictionary<string, long>(); var realSeats = new Dictionary<string, int>();
            long realValid = 0; int validCol = Array.IndexOf(vHeader, "valid"), eligibleCol = Array.IndexOf(vHeader, "eligible");
            var regionVotes2026 = new List<long[]>(); var eligible2026 = new List<double>(); var names2026 = new List<string>();
            foreach (string[] row in vRows)
            {
                realValid += long.Parse(row[validCol], Inv);
                var rv = new long[realParties.Length];
                for (int k = 0; k < realParties.Length; k++) { long v = long.Parse(row[Array.IndexOf(vHeader, realParties[k])], Inv); rv[k] = v; realVotes[realParties[k]] = (realVotes.TryGetValue(realParties[k], out long t) ? t : 0) + v; }
                regionVotes2026.Add(rv); eligible2026.Add(double.Parse(row[eligibleCol], Inv)); names2026.Add(row[0]);
            }
            foreach (string[] row in sRows) { foreach (string p in realParties) { realSeats[p] = (realSeats.TryGetValue(p, out int t) ? t : 0) + int.Parse(row[Array.IndexOf(sHeader, p)], Inv); } }
            long eightVotes = realParties.Sum(p => realVotes[p]);
            sb.Append(F("REAL (Valmyndigheten, fixed 2026-09-19): {0} valid votes, {1} for the eight parties the model carries, {2} ({3:0.00} %) for all others - unrepresentable by the model; {4} seats.\n",
                realValid, eightVotes, realValid - eightVotes, 100.0 * (realValid - eightVotes) / realValid, realSeats.Values.Sum()));
            sb.Append("  catalog: " + VotesRelative + " sha256 " + Sha(votesPath) + "; " + SeatsRelative + " sha256 " + Sha(seatsPath) + "\n");

            // (b)-(c) THE PREDICTION
            if (!NationalElection.TryCompatibility(CountryId.Sweden, out string[] keys, out double[] compat, out double[] prior, out double[] loyalty)
                || !NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> shares))
            { Debug.LogError("OOS: Sweden's prediction could not be made."); CheckExit.Finish(1); return; }
            PartySystems.TryElectorate(CountryId.Sweden, out VoteModel.Electorate electorate, out double wEcon);
            var points = new List<VoteModel.PartyPoint>();
            foreach (string k in keys) { PoliticalParty pp = parties.First(x => x.Abbrev == k); points.Add(new VoteModel.PartyPoint(k, pp.LrEcon, pp.Galtan)); }
            double[] spatial = VoteModel.PredictShares(points.ToArray(), electorate, wEcon);

            // the comparison basis: the eight, renormalised (the existing backtests' basis); the raw basis printed beside it
            int n = keys.Length;
            double[] model = new double[n], real8 = new double[n], persist = new double[n], realRaw = new double[n];
            double priorSum = prior.Sum();
            for (int i = 0; i < n; i++)
            {
                model[i] = shares[keys[i]];
                real8[i] = (double)realVotes[keys[i]] / eightVotes;
                realRaw[i] = (double)realVotes[keys[i]] / realValid;
                persist[i] = prior[i] / priorSum;
            }

            // (d) seats as the game seats them; (e) the full two-tier procedure on the model's derived per-valkrets counts
            ElectionRecord record = NationalElection.Run(CountryId.Sweden, ElectionSystem.ElectionCycle, shares);
            double[][] regShares = NationalElection.LastRegionalShares; double[] regWeights = NationalElection.LastRegionalWeights;
            var derived = new long[regShares.Length][]; var eligibleDerived = new double[regShares.Length];
            for (int r = 0; r < regShares.Length; r++)
            {
                derived[r] = new long[n];
                for (int p = 0; p < n; p++) { derived[r][p] = (long)Math.Round(regShares[r][p] * regWeights[r], MidpointRounding.AwayFromZero); }
                eligibleDerived[r] = SwedishRegions.EligibleAt(r);
            }
            SeatConversion.Result twoTier = SeatConversion.Sweden(derived, eligibleDerived);

            sb.Append("\nPER PARTY (shares over the eight; seats of 349)\n");
            sb.Append("party   real%8   real%raw  model%   dev pp   2022%(no-change) dev pp   spatial% dev pp   loyalty  | seats real  model(game)  model(two-tier)\n");
            for (int i = 0; i < n; i++)
            {
                int tt = twoTier.Seats[i];
                sb.Append(F("{0,-6} {1,7:0.00}  {2,8:0.00}  {3,6:0.00}  {4,6:+0.00;-0.00}   {5,6:0.00}         {6,6:+0.00;-0.00}   {7,6:0.00}  {8,6:+0.00;-0.00}   {9,6:0.0}   |   {10,4}       {11,4}         {12,4}\n",
                    keys[i], 100 * real8[i], 100 * realRaw[i], 100 * model[i], 100 * (model[i] - real8[i]), 100 * persist[i], 100 * (persist[i] - real8[i]),
                    100 * spatial[i], 100 * (spatial[i] - real8[i]), loyalty[i], realSeats[keys[i]], record.Seats.TryGetValue(keys[i], out int gs) ? gs : 0, tt));
            }
            double madModel = VoteModel.MeanAbsoluteDeviationPp(model, real8), madPersist = VoteModel.MeanAbsoluteDeviationPp(persist, real8), madSpatial = VoteModel.MeanAbsoluteDeviationPp(spatial, real8);
            int seatErrModel = keys.Sum(k => Math.Abs((record.Seats.TryGetValue(k, out int s) ? s : 0) - realSeats[k]));
            int seatErrTwoTier = Enumerable.Range(0, n).Sum(i => Math.Abs(twoTier.Seats[i] - realSeats[keys[i]]));
            int seatErrPersist = parties.Sum(p => Math.Abs(p.SeedSeats - realSeats[p.Abbrev]));
            sb.Append(F("\nMEAN ABSOLUTE DEVIATION over the eight: the model {0:0.00} pp · the no-change forecast (2022 shares) {1:0.00} pp · the bare spatial layer {2:0.00} pp\n", madModel, madPersist, madSpatial));
            sb.Append(F("SEATS, total absolute error of 349: the model as the game seats {0} · the model through the two-tier procedure {1} · the no-change chamber (2022 seats) {2}\n", seatErrModel, seatErrTwoTier, seatErrPersist));

            // (h) THE ALLOCATOR CONTROL - the real 2026 counts through the model's own procedures, against the real seats
            long[] real8Counts = keys.Select(k => realVotes[k]).ToArray();
            int[] alloc = SeatAllocation.AllocateWithThreshold(real8Counts, realValid, SeatConversion.NationalThreshold, SeatConversion.RiksdagSeats, SeatAllocation.ModifiedSainteLagueDivisor);
            int allocErr = Enumerable.Range(0, n).Sum(i => Math.Abs(alloc[i] - realSeats[keys[i]]));
            var regionReal = regionVotes2026.Select(rv => keys.Select(k => rv[Array.IndexOf(realParties, k)]).ToArray()).ToArray();
            SeatConversion.Result realTwoTier = SeatConversion.Sweden(regionReal, eligible2026.ToArray());
            int twoTierErr = Enumerable.Range(0, n).Sum(i => Math.Abs(realTwoTier.Seats[i] - realSeats[keys[i]]));
            int fixedCol = Array.IndexOf(sHeader, "fixed");
            int fixedDiff = 0, fixedMatched = 0;
            for (int r = 0; r < names2026.Count; r++)
            {
                string[] seatRow = sRows.FirstOrDefault(row => row[0] == names2026[r]);   // joined by the valkrets' name, never by row position
                if (seatRow == null) { continue; }
                fixedMatched++; fixedDiff += Math.Abs(realTwoTier.FixedSeatsPerRegion[r] - int.Parse(seatRow[fixedCol], Inv));
            }
            if (fixedMatched != names2026.Count) { Debug.LogError(F("OOS: only {0} of {1} valkretsar joined between the two 2026 files by name.", fixedMatched, names2026.Count)); CheckExit.Finish(1); return; }
            sb.Append(F("\nALLOCATOR CONTROL on the REAL 2026 counts: national modified Sainte-Lague {0} seats off the real chamber; the full two-tier procedure {1} seats off; its fixed seats per valkrets, derived from the election-day eligible counts, {2} seats off the decision's own column (the statute has them decided by 30 April from the roll, not derived).\n", allocErr, twoTierErr, fixedDiff));

            sb.Append("\nINPUTS (the prediction's own): electorate " + electorate + F(", economic weight {0}", wEcon) + "\n");
            for (int i = 0; i < n; i++) { sb.Append(F("  {0,-4} compatibility {1,6:0.00}  prior {2,6:0.00}  loyalty {3,6:0.0}\n", keys[i], compat[i], prior[i], loyalty[i])); }
            sb.Append("\nNOT REPRESENTED: the parties outside the eight (their votes above), anything between 2022 and 2026 (the economy, approval, leaders, polls - the electorate does not move with the simulation), turnout, differential regional swing, campaign effects and tactical voting.\n");

            string text = sb.ToString();
            Debug.Log("=== OutOfSample2026Diagnostic (K-1 part 0) ===\n" + text);
            var md = new StringBuilder();
            md.Append("# The out-of-sample test - the model's own 2026 prediction from the 2022 seed, against the real result (K-1 part 0)\n\n");
            md.Append("> **Class: DERIVED** - written once by `Assets/Editor/OutOfSample2026Diagnostic.cs` at commit " + commit + ", before any part of K-1 refreshed the seed; the diagnostic refuses to run again once the seed moves, and refuses to overwrite this file. Real figures read at run time from `" + VotesRelative + "` and `" + SeatsRelative + "` (SOURCED, Valmyndigheten's final result, Dnr VAL-735-2026, fixed 2026-09-19).\n\n");
            md.Append("```\n").Append(text).Append("```\n");
            File.WriteAllText(recordPath, md.ToString().Replace("\n", "\r\n"), new UTF8Encoding(false));
            Debug.Log("OOS: kept record written - " + RecordRelative);
            CheckExit.Finish(0);
        }

        private static void ReadCsv(string path, out string[] header, out List<string[]> rows)
        {
            header = null; rows = new List<string[]>();
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) { continue; }
                string[] cells = line.Split(';');
                if (header == null) { header = cells; continue; }
                rows.Add(cells);
            }
        }

        private static string Sha(string path) { using (var s = SHA256.Create()) { return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); } }

        private static string Arg(string prefix) { foreach (string a in Environment.GetCommandLineArgs()) { if (a.StartsWith(prefix, StringComparison.Ordinal)) { return a.Substring(prefix.Length); } } return null; }
    }
}
