using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// GATE-18 piece three, q3 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: "wire Sweden's 12 % route and per-valkrets seats into the live
    /// count"; GA-0-21): <b>THE RIKSDAG BY ITS OWN LAW, PROVEN ON THE RECORD SEAT FOR SEAT AND PER VALKRETS, THEN LIVE</b> - as
    /// `PolishSejmAllocationDiagnostic` holds the Sejm. The law: regeringsformen 3 kap. 7 § (4 % of the votes in the whole country, or 12 % in a valkrets
    /// for its fixed seats) and vallagen 14 kap. 3-5 §§ (the fixed seats per valkrets by the adjusted odd numbers, the totalfördelning, the return, the
    /// adjustment seats), both on disk.
    /// <list type="number">
    /// <item><description><b>The table</b> (`Tools/riksdag_seats_prep.pl`): the generated fixed seats re-read from Valmyndigheten's workbook, every figure and
    /// its digest; the 2022 seat table re-read from the 2026 result files' previous-election fields, every cell.</description></item>
    /// <item><description><b>2022 and 2026 on the record</b>: each election's sourced per-valkrets counts (`SwedishValkretsReturns2022`, `SwedishValkretsReturns2026`)
    /// on its decided fixed seats, every share over all valid votes, must give the Riksdag Valmyndigheten fixed - every party's seats, and every party's
    /// seats in every valkrets (2026: its fixed and adjustment seats apart, as the decision's Bilaga 1 and 6 give them).</description></item>
    /// <item><description><b>The law's edges, planted</b> on 2026's counts: a list under 4 % nationally and over 12 % in one valkrets takes fixed seats there
    /// and nowhere else; a list at 12 % of the votes the table carries in a valkrets but under 12 % of its valid vote stays out on the statute's base and
    /// is in on the table's own; a party over its entitlement returns a seat, never from a valkrets with fewer than three fixed seats.</description></item>
    /// <item><description><b>The live path</b>: the count of 2026's polling day (`ParliamentaryCount.Count`, the noise at zero) of the record's own shares,
    /// allocated through `NationalElection.Run` on its counted valkretsar, must seat the record's Riksdag; the same on a later polling day reads 2026's
    /// decision carried forward and the same party totals as the national totalfördelning. Its seats per valkrets against the record's are printed - the
    /// count's valkretsar are the record's spread by the uniform swing, each region's votes its valid vote, not the record's counts.</description></item>
    /// </list>
    /// Prints `=== SwedishRiksdagAllocationDiagnostic: ALL CHECKS PASS ===` and exits 0 when every check holds. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.SwedishRiksdagAllocationDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class SwedishRiksdagAllocationDiagnostic
    {
        private static readonly string[] Parties = { "S", "M", "SD", "C", "V", "KD", "L", "MP" };

        private static int _failures;
        private static StringBuilder _sb;

        private static void Check(bool ok, string what) { if (!ok) { _failures++; } _sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("=== SwedishRiksdagAllocationDiagnostic (GATE-18 piece three, q3): the Riksdag by its own law, on the 2022 and 2026 records, then live ===\n");
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden"));
                Table(root);
                Record("2022", SwedishValkretsReturns2022.Parties, SwedishValkretsReturns2022.Names, SwedishValkretsReturns2022.Votes, SwedishValkretsReturns2022.Valid,
                    SwedishFixedSeats.Fixed2022, Path.Combine(root, "valkrets_seats_2022.csv"), false);
                Record("2026", SwedishValkretsReturns2026.Parties, SwedishValkretsReturns2026.Names, SwedishValkretsReturns2026.Votes, SwedishValkretsReturns2026.Valid,
                    SwedishFixedSeats.Fixed2026, Path.Combine(root, "2026", "valkrets_seats_2026.csv"), true);
                Edges();
                Live(root);
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            _sb.Append(_failures == 0 ? "=== SwedishRiksdagAllocationDiagnostic: ALL CHECKS PASS ===" : F("=== SwedishRiksdagAllocationDiagnostic: {0} FAILED ===", _failures));
            if (_failures == 0) { Debug.Log(_sb.ToString()); } else { Debug.LogError(_sb.ToString()); }
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ 1. the table against its sources
        private static void Table(string root)
        {
            _sb.Append("  1. the generated table against Valmyndigheten's files:\n");
            string raw = Path.Combine(root, "2026", "raw");
            string xlsx = Path.Combine(raw, "fordelning-av-mandat-2022-och-2026.xlsx");
            Check(Sha256(xlsx) == SwedishFixedSeats.SourceDigest, F("the workbook's SHA-256 is the generated table's ({0})", SwedishFixedSeats.SourceDigest));
            Dictionary<string, (int F22, int F26)> book = ReadWorkbook(xlsx);
            int same = 0;
            for (int r = 0; r < SwedishFixedSeats.Names.Length; r++)
            {
                if (book.TryGetValue(SwedishFixedSeats.Names[r], out var f) && f.F22 == SwedishFixedSeats.Fixed2022[r] && f.F26 == SwedishFixedSeats.Fixed2026[r]) { same++; }
            }
            Check(book.Count == SwedishFixedSeats.Names.Length && same == book.Count, F("every valkrets's fixed seats of 2022 and 2026 as the workbook's rows 'Val till Riksdagen' give them ({0} of {1} rows, the table {2})", same, book.Count, SwedishFixedSeats.Names.Length));
            Check(SwedishFixedSeats.Fixed2022.Sum() == SeatConversion.FixedSeats && SwedishFixedSeats.Fixed2026.Sum() == SeatConversion.FixedSeats,
                F("each decision's fixed seats sum to the statute's {0} (2022 {1}, 2026 {2})", SeatConversion.FixedSeats, SwedishFixedSeats.Fixed2022.Sum(), SwedishFixedSeats.Fixed2026.Sum()));
            Check(SwedishFixedSeats.Names.SequenceEqual(SwedishValkretsReturns2026.Names) && SwedishFixedSeats.Names.SequenceEqual(SwedishValkretsReturns2022.Names),
                "the table's valkretsar are the count's catalog's, in its order (2022's and 2026's)");

            // the 2022 seat table against the 2026 result files' previous-election fields
            Dictionary<string, Dictionary<string, int>> csv2022 = ReadSeats(Path.Combine(root, "valkrets_seats_2022.csv"), out Dictionary<string, int> fixedCsv, out _);
            int cells = 0, agree = 0, files = 0;
            for (int i = 1; i <= 29; i++)
            {
                string path = Path.Combine(raw, F("resultat_val2026_RD_{0:00}_S.json", i));
                if (!File.Exists(path)) { continue; }
                files++;
                string json = File.ReadAllText(path, Encoding.UTF8);
                Match name = Regex.Match(json, "\"tidigareValdatum\":\"2022-09-11\",\"namn\":\"([^\"]+)\"");
                int at = json.IndexOf("\"partiMandat\":[", StringComparison.Ordinal);
                if (!name.Success || at < 0 || !csv2022.TryGetValue(name.Groups[1].Value, out Dictionary<string, int> row)) { continue; }
                string block = json.Substring(at, json.IndexOf(']', at) - at);
                foreach (Match m in Regex.Matches(block, "\"partiforkortning\":\"([^\"]+)\",\"partikod\":\"[^\"]*\",\"antalMandat\":(\\d+),\"antalMandatForegaendeVal\":(\\d+)"))
                {
                    if (!row.TryGetValue(m.Groups[1].Value, out int seats)) { continue; }
                    cells++;
                    if (seats == int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)) { agree++; }
                }
            }
            int fixedAgree = SwedishFixedSeats.Names.Count(n => fixedCsv.TryGetValue(n, out int fx) && fx == SwedishFixedSeats.Fixed2022[Array.IndexOf(SwedishFixedSeats.Names, n)]);
            Check(files == 29 && cells == 29 * Parties.Length && agree == cells && fixedAgree == 29,
                F("valkrets_seats_2022.csv is the result files' antalMandatForegaendeVal cell for cell ({0} of {1} cells, {2} files) and the workbook's 2022 column ({3} of 29)", agree, cells, files, fixedAgree));
        }

        // ============================================================ 2. an election of record, seat for seat and per valkrets
        private static void Record(string year, string[] catalogParties, string[] names, long[][] votes, long[] valid, int[] fixedSeats, string seatsPath, bool split)
        {
            _sb.Append(F("  2. {0} on the record (the sourced per-valkrets counts, every share over all valid votes, the decided fixed seats):\n", year));
            SeatConversion.Result law = SeatConversion.Sweden(votes, fixedSeats, valid.Sum(), valid);
            Dictionary<string, Dictionary<string, int>> record = ReadSeats(seatsPath, out Dictionary<string, int> fixedOnRecord, out Dictionary<string, Dictionary<string, int>> adjOnRecord);
            var national = new StringBuilder();
            int partiesSame = 0;
            foreach (string p in Parties)
            {
                int c = Array.IndexOf(catalogParties, p);
                int onRecord = record.Values.Sum(row => row.TryGetValue(p, out int n) ? n : 0);
                if (c >= 0 && law.Seats[c] == onRecord) { partiesSame++; }
                national.Append(F(" {0} {1}/{2}", p, c >= 0 ? law.Seats[c] : -1, onRecord));
            }
            Check(partiesSame == Parties.Length && law.Seats.Sum() == SeatConversion.RiksdagSeats,
                F("every party's seats are the record's (law/record:{0}; {1} seats, {2} returned)", national, law.Seats.Sum(), law.SeatsReturned));

            int cells = 0, same = 0, fixedCells = 0, fixedSame = 0;
            var differ = new List<string>();
            for (int r = 0; r < names.Length; r++)
            {
                if (!record.TryGetValue(names[r], out Dictionary<string, int> row)) { differ.Add(names[r] + " not in the record"); continue; }
                foreach (string p in Parties)
                {
                    int c = Array.IndexOf(catalogParties, p);
                    if (c < 0) { continue; }
                    cells++;
                    if (row[p] == law.RegionSeats[r][c]) { same++; } else { differ.Add(F("{0} {1} {2}/{3}", names[r], p, law.RegionSeats[r][c], row[p])); }
                    if (!split) { continue; }
                    fixedCells++;
                    int fixedWon = row[p] - adjOnRecord[names[r]][p];
                    if (fixedWon == law.RegionFixedSeats[r][c]) { fixedSame++; } else { differ.Add(F("{0} {1} fixed {2}/{3}", names[r], p, law.RegionFixedSeats[r][c], fixedWon)); }
                }
            }
            Check(cells == names.Length * Parties.Length && same == cells && (!split || fixedSame == fixedCells),
                F("every party's seats in every valkrets are the record's ({0} of {1} cells{2}){3}", same, cells,
                    split ? F("; its fixed seats {0} of {1}, the adjustment seats the rest", fixedSame, fixedCells) : "; the record gives no split into fixed and adjustment seats for 2022",
                    differ.Count > 0 ? " - law/record: " + string.Join(", ", differ.Take(12)) : string.Empty));
            int fixedTableSame = names.Count(n => fixedOnRecord.TryGetValue(n, out int fx) && fx == fixedSeats[Array.IndexOf(names, n)]);
            Check(fixedTableSame == names.Length, F("the decided fixed seats are the seat table's own fixed column ({0} of {1})", fixedTableSame, names.Length));

            // printed: the statute's rule over the election-day electorate, beside the decision (the decision is the roll of its own date)
            long[] eligible = year == "2022" ? SwedishValkretsReturns2022.Eligible : SwedishValkretsReturns2026.Eligible;
            int[] derived = SeatConversion.FixedSeatsPerRegion(eligible.Select(e => (double)e).ToArray());
            int off = 0;
            for (int r = 0; r < names.Length; r++) { off += Math.Abs(derived[r] - fixedSeats[r]); }
            _sb.Append(F("      printed: the statute's rule over the election-day electorate puts {0} fixed seat(s) elsewhere than the decision (the decision reads the roll before 30 April)\n", off / 2));
        }

        // ============================================================ 3. the law's edges, planted on 2026's counts
        private static void Edges()
        {
            _sb.Append("  3. the law's edges, planted on 2026's counts:\n");
            string[] names = SwedishValkretsReturns2026.Names;
            long[] valid = SwedishValkretsReturns2026.Valid;
            int[] fixedSeats = SwedishFixedSeats.Fixed2026;
            int regions = names.Length;
            int stockholm = Array.IndexOf(names, "Stockholms län");
            int gotland = Array.IndexOf(names, "Gotlands län");

            // (a) a ninth list, X: nothing anywhere but Stockholms län, 13 % of its valid vote there
            long[][] votes = WithNinth(SwedishValkretsReturns2026.Votes, stockholm, (long)Math.Round(0.13 / 0.87 * valid[stockholm]), out long[] validX);
            SeatConversion.Result twelve = SeatConversion.Sweden(votes, fixedSeats, validX.Sum(), validX);
            int x = Parties.Length;
            int elsewhere = 0;
            for (int r = 0; r < regions; r++) { if (r != stockholm) { elsewhere += twelve.RegionSeats[r][x]; } }
            Check(!twelve.NationallyEligible[x] && twelve.RegionSeats[stockholm][x] >= 1 && twelve.RegionSeats[stockholm][x] == twelve.RegionFixedSeats[stockholm][x]
                    && elsewhere == 0 && twelve.AdjustmentSeats[x] == 0 && twelve.Seats.Sum() == SeatConversion.RiksdagSeats,
                F("the 12 % route: X at {5:0.00} % of Stockholms län's valid vote and {0:0.00} % nationally takes {1} fixed seat(s) there, {2} elsewhere, {3} adjustment seat(s); {4} seats in all",
                    100.0 * votes[stockholm][x] / validX.Sum(), twelve.RegionSeats[stockholm][x], elsewhere, twelve.AdjustmentSeats[x], twelve.Seats.Sum(), 100.0 * votes[stockholm][x] / validX[stockholm]));

            // (b) the base: X at 12.05 % of the votes the table carries in Stockholms län - under 12 % of the valkrets's valid vote, the eight's
            //     share of it under one - is out on the statute's base and in on the table's own
            long carried = SwedishValkretsReturns2026.Votes[stockholm].Sum();
            long xVotes = (long)Math.Ceiling(0.1205 / (1.0 - 0.1205) * carried);
            long[][] edge = WithNinth(SwedishValkretsReturns2026.Votes, stockholm, xVotes, out long[] validEdge);
            double ofCarried = 100.0 * xVotes / (carried + xVotes), ofValid = 100.0 * xVotes / validEdge[stockholm];
            SeatConversion.Result onBase = SeatConversion.Sweden(edge, fixedSeats, validEdge.Sum(), validEdge);
            SeatConversion.Result onTable = SeatConversion.Sweden(edge, fixedSeats, 0L, null);
            Check(ofCarried >= 12.0 && ofValid < 12.0 && onBase.RegionSeats[stockholm][x] == 0 && onTable.RegionSeats[stockholm][x] >= 1,
                F("the 12 % line reads all valid votes: X at {0:0.000} % of the lists carried and {1:0.000} % of the valid vote takes {2} seat(s) on the statute's base, {3} on the table's own",
                    ofCarried, ofValid, onBase.RegionSeats[stockholm][x], onTable.RegionSeats[stockholm][x]));

            // (c) the return's floor: KD's whole vote in a Stockholms län where every other party casts three tenths of its vote, and a weak fixed seat in
            //     Gotlands län (two fixed seats) - KD's lowest comparison number - which the return may not take (14 kap. 4 a §)
            int kd = Array.IndexOf(SwedishValkretsReturns2026.Parties, "KD");
            var concentrated = SwedishValkretsReturns2026.Votes.Select(v => (long[])v.Clone()).ToArray();
            long kdTotal = 0;
            for (int r = 0; r < regions; r++) { kdTotal += concentrated[r][kd]; concentrated[r][kd] = 0; }
            for (int p = 0; p < concentrated[stockholm].Length; p++) { if (p != kd) { concentrated[stockholm][p] = concentrated[stockholm][p] * 3 / 10; } }
            long[] gotlandOthers = (long[])concentrated[gotland].Clone();
            int[] gotlandAlone = SeatAllocation.HighestAverages(gotlandOthers, fixedSeats[gotland], SeatAllocation.ModifiedSainteLagueDivisor);
            // KD in Gotland: just over the second seat's comparison number among the others, times the first divisor
            double secondBest = SecondSeatNumber(gotlandOthers, gotlandAlone);
            concentrated[gotland][kd] = (long)Math.Ceiling(secondBest * 1.2) + 1;
            concentrated[stockholm][kd] = kdTotal - concentrated[gotland][kd];
            int[] gotlandFixed = SeatAllocation.HighestAverages(concentrated[gotland], fixedSeats[gotland], SeatAllocation.ModifiedSainteLagueDivisor);
            long[] validC = concentrated.Select(v => v.Sum()).ToArray();
            SeatConversion.Result back = SeatConversion.Sweden(concentrated, fixedSeats, 0L, null);
            int[] entitlement = SeatAllocation.AllocateWithThreshold(NationalVotes(concentrated), validC.Sum(), SeatConversion.NationalThreshold, SeatConversion.RiksdagSeats, SeatAllocation.ModifiedSainteLagueDivisor);
            bool atTotals = true;
            for (int p = 0; p < entitlement.Length; p++) { if (back.Seats[p] != entitlement[p]) { atTotals = false; } }
            double gotlandNumber = concentrated[gotland][kd] / 1.2;
            double stockholmLowest = concentrated[stockholm][kd] / SeatAllocation.ModifiedSainteLagueDivisor(Math.Max(0, SeatAllocation.HighestAverages(concentrated[stockholm], fixedSeats[stockholm], SeatAllocation.ModifiedSainteLagueDivisor)[kd] - 1));
            Check(gotlandFixed[kd] == 1 && gotlandNumber < stockholmLowest && back.SeatsReturned > 0 && back.RegionFixedSeats[gotland][kd] == 1 && atTotals,
                F("the return's floor: KD's Gotland fixed seat (comparison number {0:N0}, below its weakest Stockholms län seat's {1:N0}) is kept; {2} seat(s) returned elsewhere; every party at its entitlement",
                    gotlandNumber, stockholmLowest, back.SeatsReturned));

            // (d) 14 kap. 4 b §'s order (the review's L-1): SD's whole vote in Stockholms län and Östergötlands län, every other party's vote there cut
            //     (authored parameters, found by a search for a case where the order decides) - two parties over their totals return one seat each, C
            //     in Jämtlands län and KD in Blekinge län, and S, with room for one of them, takes the one where its comparison number is the larger: Blekinge län
            //     to S first, then Jämtlands län to MP (the review2 M-1: the placement asserted, so a return to the old order fails it)
            int sd = Array.IndexOf(SwedishValkretsReturns2026.Parties, "SD");
            int ostergotland = Array.IndexOf(names, "Östergötlands län");
            var ordered = SwedishValkretsReturns2026.Votes.Select(v => (long[])v.Clone()).ToArray();
            long sdTotal = 0;
            for (int r = 0; r < regions; r++) { sdTotal += ordered[r][sd]; ordered[r][sd] = 0; }
            for (int p = 0; p < ordered[stockholm].Length; p++)
            {
                if (p == sd) { continue; }
                ordered[stockholm][p] = (long)(ordered[stockholm][p] * 0.1294634798678865);
                ordered[ostergotland][p] = (long)(ordered[ostergotland][p] * 0.558145741866038);
            }
            ordered[stockholm][sd] = (long)(sdTotal * 0.5766032643507251);
            ordered[ostergotland][sd] = sdTotal - ordered[stockholm][sd];
            SeatConversion.Result order = SeatConversion.Sweden(ordered, fixedSeats, 0L, null);
            string[] cat = SwedishValkretsReturns2026.Parties;
            bool twoValkretsar = order.Reallocated != null && order.Reallocated.Select(x => x.Region).Distinct().Count() >= 2;
            bool descending = order.Reallocated != null && order.Reallocated.Zip(order.Reallocated.Skip(1), (x, y) => x.Number >= y.Number).All(ok => ok);
            bool decided = false;
            string orderLine = "no seat was returned";
            if (twoValkretsar)
            {
                var first = order.Reallocated[0];
                var second = order.Reallocated.First(x => x.Region != first.Region);
                // the first taker's comparison number in the second seat's valkrets, as it stood: had the second seat gone first, it was the taker there
                double firstThere = ordered[second.Region][first.Party] / SeatAllocation.ModifiedSainteLagueDivisor(order.RegionFixedSeats[second.Region][first.Party]);
                decided = first.Party != second.Party && firstThere > second.Number && first.Number >= firstThere && order.AdjustmentSeats[first.Party] == 0
                    && names[first.Region] == "Blekinge län" && cat[first.Party] == "S" && names[second.Region] == "Jämtlands län" && cat[second.Party] == "MP";
                orderLine = string.Join("; ", order.Reallocated.Select(x => F("{0} to {1} at {2:N1}", names[x.Region], cat[x.Party], x.Number)))
                    + F(" - {0} would have taken the {1} seat at {2:N1}, but its room went to the larger number first", cat[first.Party], names[second.Region], firstThere);
            }
            Check(twoValkretsar && descending && decided && order.Seats.Sum() == SeatConversion.RiksdagSeats,
                F("4 b §: seats returned in two or more valkretsar go out largest comparison number first ({0}); {1} seats", orderLine, order.Seats.Sum()));

            // (e) an excess that cannot be returned (the review's L-2, DECLARED): a planted chamber of 155 valkretsar of two fixed seats each, where no
            //     return is allowed - O wins a seat in each of 100 small valkretsar on 8 % of the vote; the excess stands, O is set aside with its seats
            //     (as is A after it), the rest share what remains, and the chamber stays at its total
            var small = new long[155][];
            for (int r = 0; r < 155; r++) { small[r] = r < 100 ? new long[] { 1000, 600, 500 } : new long[] { 0, 10000, 9000 }; }
            var twoEach = Enumerable.Repeat(2, 155).ToArray();
            SeatConversion.Result stands = SeatConversion.Sweden(small, twoEach, 0L, null);
            Check(stands.SeatsReturned == 0 && stands.SetAside[0] && stands.Seats.Sum() == SeatConversion.RiksdagSeats && stands.Seats[0] == stands.FixedSeatsWon[0],
                F("an excess no valkrets may return: O keeps its {0} fixed seat(s) against an entitlement of about {1:0} and is set aside; set aside {2}; seats O {3}, A {4}, B {5} = {6}",
                    stands.FixedSeatsWon[0], RiksdagSeatsShare(small, 0), string.Join(" ", Enumerable.Range(0, 3).Where(p => stands.SetAside[p]).Select(p => "OAB"[p].ToString())),
                    stands.Seats[0], stands.Seats[1], stands.Seats[2], stands.Seats.Sum()));
        }

        // ============================================================ 4. the live path
        private static void Live(string root)
        {
            _sb.Append("  4. the live path - the count of the polling day, allocated through NationalElection.Run on its counted valkretsar:\n");
            DateTime poll = RecordFactors.PollingDayOf(ElectionVintage.Sweden2026);
            var shares = new Dictionary<string, double>(StringComparer.Ordinal);
            long roster = SwedishValkretsReturns2026.Votes.Sum(v => v.Sum());
            for (int c = 0; c < SwedishValkretsReturns2026.Parties.Length; c++) { shares[SwedishValkretsReturns2026.Parties[c]] = (double)SwedishValkretsReturns2026.Votes.Sum(v => v[c]) / roster; }
            foreach (PoliticalParty p in PartySystems.For(CountryId.Sweden)) { if (!shares.ContainsKey(p.Abbrev)) { shares[p.Abbrev] = 0.0; } }
            Dictionary<string, Dictionary<string, int>> record = ReadSeats(Path.Combine(root, "2026", "valkrets_seats_2026.csv"), out _, out _);

            using (ParliamentaryCount.NoiseAtZero())
            {
                ParliamentaryCount.Result count = ParliamentaryCount.Count(CountryId.Sweden, poll, shares, null, null, writeReadout: false);
                if (count == null || count.Regional == null) { Check(false, "the count of 2026's polling day carried no counted valkretsar"); return; }
                ElectionRecord seated = NationalElection.Run(CountryId.Sweden, 0, count.Shares, poll, RecordFactors.RosterShareOfVote(ElectionVintage.Sweden2026),
                    count, RecordFactors.RosterShareByRegion(ElectionVintage.Sweden2026, count.RegionNames));
                var national = new StringBuilder();
                int same = 0;
                foreach (string p in Parties)
                {
                    int onRecord = record.Values.Sum(row => row[p]);
                    int got = seated.Seats.TryGetValue(p, out int n) ? n : -1;
                    if (got == onRecord) { same++; }
                    national.Append(F(" {0} {1}/{2}", p, got, onRecord));
                }
                Check(count.Riksdag != null && seated.Method == ElectionMethod.SwedenTwoTier && same == Parties.Length,
                    F("2026's polling day, the record's shares counted at sigma {0}: the Riksdag's law ran on {1} counted valkretsar and seats the record's Riksdag (game/record:{2})",
                        count.SigmaPp, count.Regional.Regions.Length, national));
                Check(count.Riksdag != null && count.Riksdag.FixedSeatsPerRegion.SequenceEqual(SeatConversion.DecidedFixedSeats(poll, count.RegionNames)),
                    "the count's fixed seats are the decision for 2026's election");
                if (count.Riksdag != null)
                {
                    int cells = 0, cellsSame = 0;
                    for (int r = 0; r < count.RegionNames.Length; r++)
                    {
                        if (!record.TryGetValue(count.RegionNames[r], out Dictionary<string, int> row)) { continue; }
                        PoliticalParty[] all = PartySystems.For(CountryId.Sweden).ToArray();
                        foreach (string p in Parties)
                        {
                            int at = Array.FindIndex(all, q => q.Abbrev == p);
                            cells++;
                            if (at >= 0 && count.Riksdag.RegionSeats[r][at] == row[p]) { cellsSame++; }
                        }
                    }
                    _sb.Append(F("      printed: the count's seats per valkrets are the record's in {0} of {1} cells (its valkretsar the record's by the uniform swing at zero swing, each its valid vote)\n", cellsSame, cells));
                }

                // a later polling day: 2026's decision carried forward, the law's totals the totalfördelning's
                DateTime later = poll.AddYears(4);
                ParliamentaryCount.Result next = ParliamentaryCount.Count(CountryId.Sweden, later, shares, null, null, writeReadout: false);
                ElectionRecord byLaw = NationalElection.Run(CountryId.Sweden, 0, next.Shares, later, 1.0, next, null);
                ElectionRecord byTotal = NationalElection.Run(CountryId.Sweden, 0, next.Shares, later);
                bool totalsSame = byLaw.Seats.Count == byTotal.Seats.Count && byLaw.Seats.All(kv => byTotal.Seats.TryGetValue(kv.Key, out int t) && t == kv.Value);
                Check(next.Riksdag != null && next.Riksdag.FixedSeatsPerRegion.SequenceEqual(SwedishFixedSeats.Fixed2026) && totalsSame,
                    F("{0:yyyy-MM-dd}: the law reads 2026's decision carried forward, and its party totals are the national totalfördelning's (no list by the 12 % route alone): {1}",
                        later, string.Join(" ", byLaw.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
            }
        }

        // ============================================================ helpers
        private static long[][] WithNinth(long[][] votes, int region, long ninth, out long[] valid)
        {
            var with = new long[votes.Length][];
            valid = new long[votes.Length];
            for (int r = 0; r < votes.Length; r++)
            {
                with[r] = new long[votes[r].Length + 1];
                Array.Copy(votes[r], with[r], votes[r].Length);
                with[r][votes[r].Length] = r == region ? ninth : 0L;
                valid[r] = SwedishValkretsReturns2026.Valid[r] + with[r][votes[r].Length];   // the valkrets's valid vote, the ninth list's among it
            }
            return with;
        }

        /// <summary>The comparison number the last of a region's seats was won on - the number a newcomer's vote over 1.2 must beat to take a seat.</summary>
        private static double SecondSeatNumber(long[] votes, int[] seats)
        {
            double lowest = double.MaxValue;
            for (int p = 0; p < votes.Length; p++) { if (seats[p] > 0) { lowest = Math.Min(lowest, votes[p] / SeatAllocation.ModifiedSainteLagueDivisor(seats[p] - 1)); } }
            return lowest;
        }

        /// <summary>A list's seats at a plain national share of the chamber - a reading for the planted case's line.</summary>
        private static double RiksdagSeatsShare(long[][] v, int p) { long all = v.Sum(row => row.Sum()); return all > 0 ? SeatConversion.RiksdagSeats * (double)v.Sum(row => row[p]) / all : 0.0; }

        private static long[] NationalVotes(long[][] v) { var n = new long[v[0].Length]; foreach (long[] row in v) { for (int p = 0; p < n.Length; p++) { n[p] += row[p]; } } return n; }

        /// <summary>A seat table (valkrets;...;party columns), by valkrets and party; its 'fixed' column and, where it has them, the *_adj columns.</summary>
        private static Dictionary<string, Dictionary<string, int>> ReadSeats(string path, out Dictionary<string, int> fixedSeats, out Dictionary<string, Dictionary<string, int>> adjustment)
        {
            var rows = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            fixedSeats = new Dictionary<string, int>(StringComparer.Ordinal);
            adjustment = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            string[] head = null;
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                string[] f = line.Split(';');
                if (head == null) { head = f; continue; }
                var row = new Dictionary<string, int>(StringComparer.Ordinal);
                var adj = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (string p in Parties)
                {
                    row[p] = int.Parse(f[Array.IndexOf(head, p)], CultureInfo.InvariantCulture);
                    int a = Array.IndexOf(head, p + "_adj");
                    if (a >= 0) { adj[p] = int.Parse(f[a], CultureInfo.InvariantCulture); }
                }
                rows[f[0]] = row;
                adjustment[f[0]] = adj;
                fixedSeats[f[0]] = int.Parse(f[Array.IndexOf(head, "fixed")], CultureInfo.InvariantCulture);
            }
            return rows;
        }

        /// <summary>The workbook's rows 'Val till Riksdagen': each valkrets's 'Fasta mandat 2022' and 'Fasta mandat 2026', read from its own sheet XML.</summary>
        private static Dictionary<string, (int F22, int F26)> ReadWorkbook(string path)
        {
            var result = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
            using (var zip = new ZipArchive(new MemoryStream(File.ReadAllBytes(path)), ZipArchiveMode.Read))
            {
                var strings = new List<string>();
                using (var reader = new StreamReader(zip.GetEntry("xl/sharedStrings.xml").Open(), Encoding.UTF8))
                {
                    string sst = reader.ReadToEnd();
                    foreach (Match si in Regex.Matches(sst, "<si>(.*?)</si>", RegexOptions.Singleline))
                    {
                        var t = new StringBuilder();
                        foreach (Match m in Regex.Matches(si.Groups[1].Value, "<t[^>]*>(.*?)</t>", RegexOptions.Singleline)) { t.Append(m.Groups[1].Value); }
                        strings.Add(t.ToString().Replace("&amp;", "&"));
                    }
                }
                foreach (ZipArchiveEntry entry in zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal)))
                {
                    string xml;
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) { xml = reader.ReadToEnd(); }
                    var rows = new List<Dictionary<string, string>>();
                    foreach (Match row in Regex.Matches(xml, "<row r=\"\\d+\"[^>]*>(.*?)</row>", RegexOptions.Singleline))
                    {
                        var cells = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (Match c in Regex.Matches(row.Groups[1].Value, "<c r=\"([A-Z]+)\\d+\"([^>]*?)(?:/>|>(.*?)</c>)", RegexOptions.Singleline))
                        {
                            Match v = Regex.Match(c.Groups[3].Value, "<v>(.*?)</v>");
                            string value = v.Success ? v.Groups[1].Value : string.Empty;
                            if (c.Groups[2].Value.Contains("t=\"s\"") && int.TryParse(value, out int si)) { value = strings[si]; }
                            cells[c.Groups[1].Value] = value;
                        }
                        rows.Add(cells);
                    }
                    if (rows.Count == 0) { continue; }
                    string Col(string header) => rows[0].FirstOrDefault(kv => kv.Value == header).Key;
                    string type = Col("Valtyp"), name = Col("Valkrets"), f22 = Col("Fasta mandat 2022"), f26 = Col("Fasta mandat 2026");
                    if (type == null || name == null || f22 == null || f26 == null) { continue; }
                    foreach (Dictionary<string, string> row in rows.Skip(1))
                    {
                        if (!row.TryGetValue(type, out string t) || t != "Val till Riksdagen") { continue; }
                        result[row[name]] = (int.Parse(row[f22], CultureInfo.InvariantCulture), int.Parse(row[f26], CultureInfo.InvariantCulture));
                    }
                }
            }
            return result;
        }

        private static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
