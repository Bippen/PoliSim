using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-13: THE MIDTERM RECORD, AND THE MODEL'S MIDTERM - R-US20's instrument. It runs R-US20's rule, the fenced block in
    /// `docs/specs/USA_STAGE_PLAN.md` (preregistered before this instrument's first run), on two things side by side:
    /// <list type="bullet">
    /// <item>the record - `docs/generated/US_MIDTERM_RECORD.md` and the CSV it is read on (`ElectionsData/usa/house_midterms.csv`), both written by
    /// `Tools/us_midterm_record.pl` from saved pages. This check holds them: every page the CSV names held to its digest on disk (the figures are read
    /// from the pages by the tool, not here), the document's stamp to the CSV's digest, the rows of its midterm and presidential-year tables and its
    /// ranges recomputed here from the CSV, and the Clerk's 2016-2024 totals held to US-5's catalog (<see cref="UsPresidentialReturns"/>). The
    /// document's comparison with the secondary compilation, its paragraph on the Clerk's gaps, its 2026 section and its winners' names are the
    /// tool's alone (§840's review, finding 7);</item>
    /// <item>the model's midterm - a US world built as a new game builds one on the eve of 5 Nov 2024 (the epoch set first; the USA the player's
    /// country with DEM its party, so the AI governs the president's party through the term the midterm scores), stepped day by day in the
    /// controller's order to 3 Nov 2026 and held to the record's president (R-US1 (a): Trump from the oath) and, PS-6 K73 (the block as amended), the House the world's own count of 5 Nov 2024 elected, from 3 Jan 2025 - since PS-6 US-8 the world's own
    /// count elects him on 5 Nov 2024, as the record did, and the check holds that it did). Its poll is
    /// US-5's House history as R-US15 (a) and G2 ruled it - held at the four countries' median spread, fitted to the 2024 House split, the loyalty 2024
    /// against 2022, the live chain's order, the identity checked - and the economic vote is the record over the term on that day, read three ways:
    /// none, the presidential row (<see cref="EconomicVote"/>'s live constants), and Table 9.1's US legislative cell for the world's government type
    /// (`docs/reference/ECONOMIC_VOTE.md`, [BK-D] p. 247, [draft]) - substituted HERE ONLY; nothing reaches the game.</item>
    /// </list>
    /// The rule compares the model's midterm with the legislative cell against the record's band at full precision and prints the verdict. The
    /// model's seat change is never computed here: R-US18 and R-US20 are both ruled (b) (§856), and the seat change of R-US20 (b)'s fitted term is US-27's.
    ///
    /// <para>Pinned in the cheap bar and the documents bar through the US card's marked block (`docs/reference/US_ELECTIONS.md`, *The midterm -
    /// R-US20*), written by <see cref="WriteReadings"/>; a stale block fails. The instrument holds the rule block's digest list and the ruling record
    /// (R-US18's form): NONE fails until the first run's ask is registered; ASKED and RULED pass while the generated outcome is the registered one.</para>
    /// </summary>
    public static class UsMidtermCheck
    {
        // ------------------------------------------------------------------ the rule block's digests and the ruling record (R-US20's THE ASK AND THE BARS)
        /// <summary>The block's SHA-256 digests, preregistered first, then one per amendment naming Elias's ruling; the block must match the last.</summary>
        private static readonly (string Digest, string Why)[] RuleDigests =
        {
            ("84787a5309a25fb0073a22693a6676d7695aa5e0d44193fc9e2fa5368c00a734", "preregistered (US-13, before the instrument's first run)"),
            ("6cf56a5d32cb18d312dd15c3a14fc19aa153ec405949dd105067200993768a67", "AMENDED (I4, §863): both chambers; the seat change's line after R-US18 RULED (I1)"),
            ("ec389083201d27adfc07fae6622ae9cf58f13b1baaf161c40474992ae0a8e5bb", "AMENDED (K73, 2026-10-23): the model's world reads the game's own House"),
        };

        /// <summary>The ruling record: NONE, ASKED or RULED. The first run registers the ask - its section and the outcome: the verdict, the government
        /// type and L, U and m to two decimals, as the run prints them.</summary>
        private const string RulingState = "RULED";
        private const string RulingSection = "§856";
        // RE-REGISTERED, verdict unchanged (s928): the cause is MF-14 q14 - the convex Phillips slack and the unemployment floor move the US economy the
        // economic vote reads (m +0.16 -> +0.12); L, U, the verdict and the government type stand. Before it:
        // RE-REGISTERED, verdict unchanged: the cause is K73 (R-US20's block as amended, prereg 8b4a8954) - the world reads the game's own House, so the
        // government type and m are read on the House the world's own 2024 count elected; L and U, the record's, did not move
        private const string RegisteredVerdict = "(a)", RegisteredGovernment = "divided", RegisteredL = "-8.97", RegisteredU = "+2.26", RegisteredM = "+0.12";
        /// <summary>RULED only: the ruling's words and the option it names - (a), (b) or (c).</summary>
        private const string RulingRuled = "I4 - (b), a FITTED midterm term on the president's party from the record since 1946, n and its interval printed (US-27 builds it); revisited against (c) once approval reaches the vote; the both-chambers amendment ruled yes", RulingOption = "(b)";

        // ------------------------------------------------------------------ the staging the rule names
        private static readonly DateTime Epoch = new DateTime(2024, 11, 4);
        private static readonly DateTime PollingDay = new DateTime(2026, 11, 3);
        private static readonly DateTime HouseDay = new DateTime(2025, 1, 3);
        private static readonly DateTime OathDay = new DateTime(2025, 1, 20);
        private const int Seed = 777;
        private const string PlayerParty = "DEM";
        private static readonly CountryId[] Held = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy };

        /// <summary>Table 9.1's US legislative row, the president's party ([BK-D] p. 247, `docs/reference/ECONOMIC_VOTE.md`, [draft]): each cell's mean,
        /// its parenthesis (by Table 9.2's note a standard deviation of the studies' estimates) and its n. Signed as printed: a magnitude in code's
        /// convention is the cell's negative, as the presidential row's is (-0.10 printed, <see cref="EconomicVote.UsPresidentUnified"/> 0.10).</summary>
        private const double LegislativeUnified = -0.016, LegislativeUnifiedSd = 0.03, LegislativeDivided = 0.012, LegislativeDividedSd = 0.038;
        private const int LegislativeUnifiedN = 2, LegislativeDividedN = 9;

        private const string CsvRelative = "ElectionsData/usa/house_midterms.csv";
        private const string DocRelative = "docs/generated/US_MIDTERM_RECORD.md";
        private const string PlanRelative = "docs/specs/USA_STAGE_PLAN.md";
        private const string BlockBegin = "<!-- R-US20 RULE BEGIN -->\n", BlockEnd = "<!-- R-US20 RULE END -->";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsMidtermCheck (PS-6 US-13, R-US20): the midterm record, and the model's midterm ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(Path.Combine(Root(), CardRelative), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the US card ({0}) carries no US-13 block", CardRelative)
                    : onDisk == expected ? F("the US card's US-13 block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the US card's US-13 block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsMidtermCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsMidtermCheck: the reading holds ===" : F("=== UsMidtermCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-13 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails
        /// but the stale block itself and an unregistered ask (NONE), which the first run's registration follows.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsMidtermCheck.WriteReadings (US-13): the US card's midterm block ===\n");
            int failures = 0, registration = 0;
            void Check(bool ok, string what)
            {
                if (!ok) { if (what.StartsWith("the ruling record NONE", StringComparison.Ordinal)) { registration++; } else { failures++; } }
                sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n');
            }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = Path.Combine(Root(), CardRelative);
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed, then write"); }
                else if (old == null) { Check(false, F("the US card ({0}) carries no US-13 block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the US card's US-13 block written ({0}) - {1} lines{2}", CardRelative, card.Count, registration > 0 ? "; the ask is still to be registered" : ""));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ the record, as the CSV holds it
        private sealed class Row
        {
            public int Year, Congress, From;
            public string Kind, Party, Elected, VsChange, VsDir;
            public int? SeatsD, SeatsR, SeatsOther, SeatsVacant, SeatsTotal;
            /// <summary>The Senate sitting on the election's day ([SEN-DIV], the tool's reading): its Congress, the period read ("-" where the line has
            /// none), its divisions, the majority as printed and how the tool held it to the line's counts.</summary>
            public int SenateCongress, SenateD, SenateR, SenateOther, SenateTotal;
            public string SenatePeriod, SenateMajority, SenateHow;
            public long? ClerkR, ClerkD, ClerkTotal;
        }

        /// <summary>An election 1946-2026 with what the record derives from it and the election two years before.</summary>
        private sealed class Derived
        {
            public Row Row;
            public string Government;
            public int? SeatsBefore, SeatsAfter, SeatChange;
            public double? ShareBefore, ShareAfter, Swing, VsSwing;
        }

        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            var clock = Stopwatch.StartNew();
            string root = Root();

            // ---- the rule block: its digest the list's last
            string plan = File.ReadAllText(Path.Combine(root, PlanRelative), Encoding.UTF8).Replace("\r\n", "\n");
            int b0 = plan.IndexOf(BlockBegin, StringComparison.Ordinal), b1 = plan.IndexOf(BlockEnd, StringComparison.Ordinal);
            Check(b0 >= 0 && b1 > b0, "R-US20's block found in the plan");
            string blockDigest = b0 >= 0 && b1 > b0 ? Digest(plan.Substring(b0 + BlockBegin.Length, b1 - b0 - BlockBegin.Length)) : "-";
            Check(blockDigest == RuleDigests[RuleDigests.Length - 1].Digest, F("the block's digest {0} is the list's last ({1})", blockDigest.Substring(0, Math.Min(8, blockDigest.Length)), RuleDigests[RuleDigests.Length - 1].Why));

            // ---- the record: the pages, the CSV, the document
            byte[] csvBytes = File.ReadAllBytes(Path.Combine(root, CsvRelative));
            string csvDigest = ElectionsDataCatalogGenerator.Sha256Of(csvBytes);
            var sources = new List<(string Rel, string Sha)>();
            var rows = new List<Row>();
            string[] header = null;
            foreach (string raw in Encoding.UTF8.GetString(csvBytes).Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0) { continue; }
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    Match m = Regex.Match(line, @"^#   (\S+) sha256 ([0-9a-f]{64})$");
                    if (m.Success) { sources.Add((m.Groups[1].Value, m.Groups[2].Value)); }
                    continue;
                }
                if (header == null) { header = line.Split(','); continue; }
                rows.Add(ParseRow(header, line.Split(',')));
            }
            Check(header != null && header.SequenceEqual("year,kind,congress,president_party,president_from,elected_party,seats_d,seats_r,seats_other,seats_vacant,seats_total,clerk_r,clerk_d,clerk_total,vs_dem_pct,vs_rep_pct,vs_vote_change,vs_vote_direction,senate_day,senate_congress,senate_period,senate_d,senate_r,senate_other,senate_total,senate_majority,senate_how".Split(',')),
                "the record's CSV carries the heading this check reads");
            int staleSources = 0;
            foreach (var s in sources)
            {
                string file = Path.Combine(root, "ElectionsData/usa/raw", s.Rel);
                if (!File.Exists(file) || ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(file)) != s.Sha) { staleSources++; Check(false, F("the page {0} is not the bytes the record read (sha256 {1})", s.Rel, s.Sha.Substring(0, 8))); }
            }
            int nara = sources.Count(s => s.Rel.Contains("archives_electoral_college_")), clerkPages = sources.Count(s => s.Rel.Contains("clerk_statistics"));
            Check(staleSources == 0 && sources.Any(s => s.Rel == "executive/history_house_party_divisions.html") && sources.Any(s => s.Rel == "midterms/vitalstats_table_2-2.csv")
                  && sources.Any(s => Regex.IsMatch(s.Rel, @"^records/wayback_senate_party_division_\d{8}\.html$"))
                  && nara == rows.Count(r => r.Year <= 2024 && r.Year % 4 == 0) && clerkPages == rows.Count(r => r.ClerkR.HasValue),
                F("the pages the record was read from are on disk byte for byte: {0} pages - [HH-DIV], [SEN-DIV], NARA's results for every presidential year ({1}), the Clerk's statistics for every year it reads ({2}), the secondary", sources.Count, nara, clerkPages));
            // [SEN-DIV] as the tool read it: the Senate sitting on each election's day, the Congress the election two years before chose
            List<Row> badSenate = rows.Where(r => r.SenateCongress != r.Congress - 1 || r.SenateD + r.SenateR + r.SenateOther != r.SenateTotal || (r.SenateMajority != "R" && r.SenateMajority != "D")
                                                  || !new[] { "seats", "seats-with-caucus", "equal-vp", "page" }.Contains(r.SenateHow) || (r.SenateHow == "equal-vp" && r.SenateMajority != r.Party)).ToList();
            Check(badSenate.Count == 0, F("the Senate sitting at every election is the Congress the election two years before chose, its divisions summing to its seats, its majority REP or DEM (an equal division the president's party's, one ticket){0}",
                badSenate.Count == 0 ? "" : " - not at " + string.Join(", ", badSenate.Select(r => r.Year))));
            string[] doc = File.ReadAllLines(Path.Combine(root, DocRelative), Encoding.UTF8);
            Check(doc.Length > 0 && doc[0].Contains("sha256 " + csvDigest), F("the record document's stamp carries the CSV's digest {0} - a changed CSV is re-run through Tools/us_midterm_record.pl", csvDigest.Substring(0, 8)));

            Dictionary<int, Row> byYear = rows.ToDictionary(r => r.Year);
            Check(rows.Select(r => r.Year).SequenceEqual(Enumerable.Range(0, 42).Select(i => 1944 + 2 * i)), "the record holds every House election from 1944 to 2024 once, and the 2026 midterm");
            // US-5's catalog: the Clerk read by the record's tool gives the catalog's House sums, 2016-2024
            foreach (int y in UsPresidentialReturns.House.Select(h => h.Year).Distinct().OrderBy(y => y))
            {
                long r = UsPresidentialReturns.House.Where(h => h.Year == y).Sum(h => h.VotesR), d = UsPresidentialReturns.House.Where(h => h.Year == y).Sum(h => h.VotesD), t = UsPresidentialReturns.House.Where(h => h.Year == y).Sum(h => h.VotesTotal);
                Check(byYear.TryGetValue(y, out Row x) && x.ClerkR == r && x.ClerkD == d && x.ClerkTotal == t, F("House {0}: the record's Clerk totals are US-5's catalog's - REP {1} DEM {2} total {3}", y, r, d, t));
            }

            // the derived record, recomputed here
            var derived = new List<Derived>();
            foreach (Row x in rows.Where(r => r.Year >= 1946))
            {
                Row prev = byYear[x.Year - 2];
                string largest = prev.SeatsR > prev.SeatsD ? "R" : prev.SeatsD > prev.SeatsR ? "D" : "tie";
                Check(largest != "tie", F("the House sitting at {0} has one largest party", x.Year));
                // R-US20's block as amended under I4: unified where the president's party holds the House sitting and the Senate sitting on the day
                var dv = new Derived { Row = x, Government = largest == x.Party && x.SenateMajority == x.Party ? "unified" : "divided" };
                int? Seats(Row r, string p) => p == "R" ? r.SeatsR : r.SeatsD;
                if (Seats(x, x.Party).HasValue && Seats(prev, x.Party).HasValue) { dv.SeatsBefore = Seats(prev, x.Party); dv.SeatsAfter = Seats(x, x.Party); dv.SeatChange = dv.SeatsAfter - dv.SeatsBefore; }
                if (x.ClerkR.HasValue && prev.ClerkR.HasValue)
                {
                    dv.ShareBefore = ShareOf(prev, x.Party); dv.ShareAfter = ShareOf(x, x.Party); dv.Swing = 100.0 * (dv.ShareAfter - dv.ShareBefore);
                }
                if (x.VsChange != "-") { dv.VsSwing = (x.VsDir == x.Party ? 1.0 : -1.0) * double.Parse(x.VsChange, CultureInfo.InvariantCulture); }
                derived.Add(dv);
            }
            List<Derived> midterms = derived.Where(d => d.Row.Kind == "midterm" && d.Row.Year <= 2022).ToList();
            List<Derived> presYears = derived.Where(d => d.Row.Kind == "presidential").ToList();
            Derived m2026 = derived.Single(d => d.Row.Year == 2026);

            // the document's tables, row by row
            int docRows = 0;
            foreach (Derived d in midterms.Concat(presYears))
            {
                string section = d.Row.Kind == "midterm" ? "## The midterms" : "## The presidential years";
                string line = doc.SkipWhile(l => !l.StartsWith(section, StringComparison.Ordinal)).Skip(1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal))
                                 .FirstOrDefault(l => l.StartsWith(F("| {0} | ", d.Row.Year), StringComparison.Ordinal));
                string[] c = line == null ? new string[0] : Cells(line);
                bool ok = c.Length == 9 && c[1].StartsWith(F("{0} ({1}: ", PartyName(d.Row.Party), d.Row.From), StringComparison.Ordinal) && c[2] == d.Government && c[3] == SenateCell(d.Row)
                          && c[4] == (d.SeatChange.HasValue ? F("{0} -> {1}", d.SeatsBefore, d.SeatsAfter) : "-") && c[5] == Sc(d.SeatChange)
                          && c[6] == (d.Swing.HasValue ? F("{0} -> {1}", Pct(d.ShareBefore.Value), Pct(d.ShareAfter.Value)) : "-") && c[7] == Pp(d.Swing) && c[8] == Vs(d.VsSwing);
                if (ok) { docRows++; } else { Check(false, F("the record document's {0} row is not what the CSV gives: '{1}'", d.Row.Year, line ?? "no row")); }
            }
            Check(docRows == midterms.Count + presYears.Count, F("the record document's rows are the CSV's, recomputed here: {0} midterms and {1} presidential years", midterms.Count, presYears.Count));

            // the ranges, by government type and in the presidential years
            var sets = new (string Name, List<Derived> Set)[]
            {
                ("every midterm, 1946-2022", midterms), ("unified midterms", midterms.Where(d => d.Government == "unified").ToList()), ("divided midterms", midterms.Where(d => d.Government == "divided").ToList()),
                ("every presidential year, 1948-2024", presYears), ("unified presidential years", presYears.Where(d => d.Government == "unified").ToList()), ("divided presidential years", presYears.Where(d => d.Government == "divided").ToList()),
            };
            card.Add("### The record - the president's party in the House (`docs/generated/US_MIDTERM_RECORD.md`)");
            card.Add("");
            card.Add("Each set's least and greatest, recomputed from `ElectionsData/usa/house_midterms.csv`, the government type read over both chambers sitting on the day ([HH-DIV]'s House, [SEN-DIV]'s Senate; R-US20's block as amended under I4): the seat change by [HH-DIV], the two-party vote swing by the Clerk's recapitulations where they carry text (1998 on), and the SECONDARY compilation's (Vital Statistics on Congress, Table 2-2), printed beside and never read by the rule. Points; n counts the elections each column reads.");
            card.Add("");
            card.Add("| set | n | seat change: least / greatest | n (the Clerk) | vote swing (the Clerk): least / greatest | n (SECONDARY) | vote swing (SECONDARY): least / greatest |");
            card.Add("|---|---:|---:|---:|---:|---:|---:|");
            foreach (var s in sets)
            {
                var seats = Range(s.Set.Where(d => d.SeatChange.HasValue).Select(d => (double)d.SeatChange.Value).ToList());
                var votes = Range(s.Set.Where(d => d.Swing.HasValue).Select(d => d.Swing.Value).ToList());
                var vs = Range(s.Set.Where(d => d.VsSwing.HasValue).Select(d => d.VsSwing.Value).ToList());
                string Seat(double? v) => v.HasValue ? Sc((int)v.Value) : "-";
                string docLine = doc.SkipWhile(l => !l.StartsWith("## The ranges", StringComparison.Ordinal)).FirstOrDefault(l => l.StartsWith("| " + s.Name + " |", StringComparison.Ordinal));
                string want = F("| {0} | {1} | {2} / {3} | {4} | {5} / {6} | {7} | {8} / {9} |", s.Name, seats.N, Seat(seats.Lo), Seat(seats.Hi), votes.N, Pp(votes.Lo), Pp(votes.Hi), vs.N, Vs(vs.Lo), Vs(vs.Hi));
                Check(docLine == want, F("the record's range, {0}: {1}", s.Name, docLine == want ? "the document's line, recomputed" : "the document reads '" + (docLine ?? "no line") + "'"));
                card.Add(want);
            }

            // R-US20's band: the midterms whose two elections the Clerk's text pages both read
            List<Derived> band = midterms.Where(d => d.Swing.HasValue).ToList();
            bool bandYears = band.Select(d => d.Row.Year).SequenceEqual(new[] { 2002, 2006, 2010, 2014, 2018, 2022 });
            Check(bandYears, F("the rule's record is the midterms of 2002 to 2022, as its block words it: {0}", string.Join(", ", band.Select(d => d.Row.Year))));
            double bandL = band.Min(d => d.Swing.Value), bandU = band.Max(d => d.Swing.Value);
            string bandText = F("[L, U] = [{0}, {1}] points", Pp(bandL), Pp(bandU));
            Check(doc.Any(l => l.StartsWith("**R-US20's band**", StringComparison.Ordinal) && l.Contains(bandText)), F("the record document prints R-US20's band {0}", bandText));
            card.Add("");
            card.Add(F("**R-US20's band** - the rule's record, the midterms {0} by the Clerk, both government types together: {1} (full precision: L = {2:0.000000}, U = {3:0.000000}). Every earlier midterm's vote swing is BILLED - the Clerk's statistics of 1944-1996 are page images.",
                string.Join(", ", band.Select(d => d.Row.Year)), bandText, bandL, bandU));
            bool record2026 = m2026.Row.Party == "R" && m2026.Government == "unified" && byYear[2024].SeatsR == 220 && byYear[2024].SeatsD == 215 && m2026.Row.SenateMajority == "R";
            Check(record2026,
                F("the record's 2026 midterm: the president's party {0} (2024's winner), the House sitting the 2024 election's, REP {1} DEM {2}, the Senate sitting {3} - {4}", PartyName(m2026.Row.Party), byYear[2024].SeatsR, byYear[2024].SeatsD, SenateCell(m2026.Row), m2026.Government));

            // ---- the model's midterm
            Midterm w = ReadMidterm();
            // a world that does not hold the record is no outcome of R-US20's rule (its block): these checks decide whether a verdict is printed at all
            bool houseFrom = w.HouseFrom == HouseDay && w.HouseChanges == 1;
            bool oath = w.OathFrom == OathDay && w.Kind == WorldClock.ExecutiveKind.Presidency && w.PmParty == "REP" && w.Executive.StartsWith("Donald J. Trump", StringComparison.Ordinal)
                        && w.CountedParty == "REP";   // PS-6 US-8: the world's own count of 5 Nov 2024 elected him (the record yields from its polling day)
            bool termHeld = w.Term.Unemployment.Complete && w.Term.Inflation.Complete && w.TookOffice.HasValue && w.TookOffice.Value == OathDay;
            bool holds = bandYears && record2026 && w.Reached && houseFrom && w.HouseHeld && oath && termHeld;
            Check(w.Reached, F("the world stepped from {0:yyyy-MM-dd} to {1:yyyy-MM-dd}, one day at a time ({2} days, {3} ms)", Epoch, w.Day, w.Days, w.Ms));
            Check(houseFrom, F("the House the world's own count of 5 Nov 2024 elected seated from {0:yyyy-MM-dd} (once; the Twentieth Amendment's day {1:yyyy-MM-dd}) and holding on the polling day: {2}", w.HouseFrom, HouseDay, w.HouseHeld));
            Check(w.HouseHeld, F("PS-6 K73: the House on the polling day is the game's own - the one the world's own 2024 count elected (REP {0}, DEM {1}; the record's 220 and 215 are not read)", w.GameHouseRep, w.GameHouseDem));
            Check(w.CongressCounted != null && w.MidtermSetAside != null,
                F("PS-6 K73 (R-US20's block as amended): the world's own Congress count of 5 Nov 2024 (US-16) held and its House seated - \"{0}\"; the world's own count of 3 Nov 2026 set aside, the block reading the poll it builds: \"{1}\"", w.CongressCounted ?? "NO COUNT HELD", w.MidtermSetAside ?? "NONE HELD"));
            Check(oath,
                F("Trump (REP) the president from the oath, as of record - elected by the world's own count (US-8: {0}): the government changed to REP on {1:yyyy-MM-dd} (the record's {2:yyyy-MM-dd}); on the polling day {3}",
                    w.CountedLine ?? "no count held", w.OathFrom, OathDay, w.Executive));
            Check(termHeld, F("the record over the term read at both ends, the term begun on the oath's day ({0:yyyy-MM-dd}) - {1}", OathDay, EconomicVote.Describe(w.Term)));
            // both chambers (R-US20's block as amended under I4): the House's largest party and the seated Senate's majority by UsSenate.MajorityOf
            Check(w.SenateSeated, F("the Senate is seated in the world on the polling day (the USA the player's country): its majority {0}", w.SenateMajority ?? "none"));
            bool unified = w.Largest == w.PmParty && w.SenateSeated && w.SenateMajority == w.PmParty;
            string government = unified ? "unified" : "divided";
            double liveMag = w.LiveMagnitudes.TryGetValue(w.PmParty, out double lmag) ? lmag : double.NaN;
            Check(w.LiveMagnitudes.Count == 1 && liveMag == (unified ? EconomicVote.UsPresidentUnified : EconomicVote.UsPresidentDivided),
                F("the live economic vote names the president's party alone, at the presidential row's {0} cell", government));
            double liveShift = w.LiveShift.TryGetValue(w.PmParty, out double ls) ? ls : double.NaN;
            double deterioration = -liveShift / liveMag;
            double fromIndex = (50.0 - Math.Max(0.0, Math.Min(100.0, w.Term.Index))) / 50.0;
            Check(!double.IsNaN(deterioration) && Math.Abs(deterioration - fromIndex) < 1e-12, F("the units of deterioration as the live code reads them: {0:0.000000} (the term index {1:0.0} on the book's scale)", deterioration, w.Term.Index));

            // the poll: US-5's House history, held at the four countries' median spread (R-US15 (a), G2)
            PoliticalParty[] roster = PartySystems.RealRoster(CountryId.USA);
            string[] keys = roster.Select(p => p.Abbrev).ToArray();
            Check(keys.SequenceEqual(new[] { "REP", "DEM" }), F("the US roster is REP and DEM, in that order: {0}", string.Join(", ", keys)));
            VoteModel.PartyPoint[] points = roster.Select(p => new VoteModel.PartyPoint(p.Abbrev, p.LrEcon, p.Galtan)).ToArray();
            double wEcon = VoteShareBacktest.UsEconomicWeight;
            var sigmas = new List<double>(); var taus = new List<double>();
            foreach (CountryId id in Held) { bool has = PartySystems.TryElectorate(id, out VoteModel.Electorate e, out double _); Check(has, F("{0}'s electorate is held", id)); if (has) { sigmas.Add(e.Sigma); taus.Add(e.Tau); } }
            double[] prior = HouseShares(2024), before = HouseShares(2022);
            double priorSplit = TwoParty(prior);
            var held = new VoteModel.Electorate(0.0, 0.0, Median(sigmas), Median(taus));
            double lo = 0.0, hi = 1.0;
            Check(RepAt(points, wEcon, held, lo) < priorSplit && priorSplit < RepAt(points, wEcon, held, hi), "the median spread brackets the 2024 House split on the DEM-REP segment");
            for (int i = 0; i < 80; i++) { double mid = 0.5 * (lo + hi); if (RepAt(points, wEcon, held, mid) < priorSplit) { lo = mid; } else { hi = mid; } }
            VoteModel.Electorate fit = Along(points, held, 0.5 * (lo + hi));
            double[] spatial = VoteModel.PredictShares(points, fit, wEcon);
            double[] loyalty = LoyaltyModel.PartyLoyalties(prior, before);
            double[] poll = PreferenceModel.Preference(GateReRun.ToCompatScale(spatial), prior, loyalty);
            double residual = Math.Abs(spatial[0] - priorSplit), off = Math.Abs(TwoParty(poll) - priorSplit);
            Check(Math.Abs(TwoParty(spatial) - priorSplit) < 1e-9, F("the held fit gives the 2024 House split to 1e-9 ({0:0.0e+0})", Math.Abs(TwoParty(spatial) - priorSplit)));
            Check(off <= residual + 1e-12, F("the identity: the poll {0:0.000000} no farther from the prior {1:0.000000} than the fit's residual {2:0.0e+0}", TwoParty(poll), priorSplit, residual));

            // the economic vote, three ways; the model's midterm m on the president's party
            int p = Array.IndexOf(keys, w.PmParty);
            double cell = unified ? LegislativeUnified : LegislativeDivided, sd = unified ? LegislativeUnifiedSd : LegislativeDividedSd;
            int n = unified ? LegislativeUnifiedN : LegislativeDividedN;
            double recordShare = p == 0 ? priorSplit : 1.0 - priorSplit;
            double MidtermOf(double[] shares) { double t = TwoParty(shares); return 100.0 * ((p == 0 ? t : 1.0 - t) - recordShare); }
            double[] With(double shift) => EconomicVote.ApplyRecordShift(keys, poll, new Dictionary<string, double> { { w.PmParty, shift } });
            double mNone = MidtermOf(poll), mPres = MidtermOf(EconomicVote.ApplyRecordShift(keys, poll, w.LiveShift));
            double mCell = MidtermOf(With(cell * deterioration)), mLow = MidtermOf(With((cell - sd) * deterioration)), mHigh = MidtermOf(With((cell + sd) * deterioration));

            card.Add("");
            card.Add("### The model's midterm - 3 Nov 2026");
            card.Add("");
            card.Add(F("- **The world**: the epoch {0:yyyy-MM-dd} (the eve of 5 Nov 2024), seed {1}, the USA the player's country with {2} its party (the AI governs the president's party through the term), no decision on any turn, stepped one day at a time in the controller's order to {3:yyyy-MM-dd} ({4} days). The House the world's own count of 5 Nov 2024 elected (R-US20's block as amended under K73) from {5:yyyy-MM-dd}, Trump (REP) from {6:yyyy-MM-dd} - elected by the world's own count of 5 Nov 2024 (US-8), as the record elected him. On the polling day: the president's party {7}, the House's largest party {8}, the Senate's majority {10} (both chambers, R-US20's block as amended under I4) - **{9}**.",
                Epoch, Seed, PlayerParty, w.Day, w.Days, w.HouseFrom, w.OathFrom, w.PmParty, w.Largest, government, w.SenateMajority ?? "none"));
            card.Add(F("- **The poll**: US-5's House history (R-US15 (a), G2) - held at the four countries' median spread (σ {0:0.00}, τ {1:0.00}), fitted to the 2024 House split (REP {2}); the loyalty 2024 against 2022, REP {3:0.00} DEM {4:0.00}; REP {5} before the economic vote - the identity holds (within {6:0.0e+0} of the prior).",
                held.Sigma, held.Tau, Pct(priorSplit), loyalty[0], loyalty[1], Pct(TwoParty(poll)), off));
            card.Add(F("- **The economic vote**: {0}; {1:0.000000} units of deterioration on the book's scale.", EconomicVote.Describe(w.Term), deterioration));
            card.Add("");
            card.Add(F("| economic vote on the president's party ({0}) | its cell | {0}'s shift (pp) | {0}'s two-party share | the model's midterm m (pp) |", w.PmParty));
            card.Add("|---|---:|---:|---:|---:|");
            card.Add(F("| none - the history alone | - | +0.00 | {0} | {1} |", Pct(recordShare + mNone / 100.0), Pp(mNone)));
            card.Add(F("| the presidential row (the live constants; US-5's House history in 2024) | {0:0.00} ({1}) | {2} | {3} | {4} |", -liveMag, government, Pp(100.0 * liveShift), Pct(recordShare + mPres / 100.0), Pp(mPres)));
            card.Add(F("| **Table 9.1's US legislative cell [draft] - R-US20 (a)** | {0:+0.000;-0.000} ({1:0.000}) n = {2} | {3} | {4} | **{5}** |", cell, sd, n, Pp(100.0 * cell * deterioration), Pct(recordShare + mCell / 100.0), Pp(mCell)));
            card.Add(F("| the legislative cell less its parenthesis | {0:+0.000;-0.000} | {1} | {2} | {3} |", cell - sd, Pp(100.0 * (cell - sd) * deterioration), Pct(recordShare + mLow / 100.0), Pp(mLow)));
            card.Add(F("| the legislative cell plus its parenthesis | {0:+0.000;-0.000} | {1} | {2} | {3} |", cell + sd, Pp(100.0 * (cell + sd) * deterioration), Pct(recordShare + mHigh / 100.0), Pp(mHigh)));
            card.Add("");
            // R-US18 RULED (b) and R-US20 RULED (b) together (§856): the seat change of the midterm R-US20 (b) fits is US-27's, by (b) in play - not of this
            // instrument's m, which (b) replaces. Held here so a later change of either ruling is seen.
            Check(UsHouseCountCheck.RulingState == "RULED" && RulingState == "RULED" && RulingOption == "(b)", F("R-US18 is {0} ({1}) and R-US20 {2} {3} - so the model's seat change is US-27's, by R-US18's ruled method on R-US20 (b)'s fitted term", UsHouseCountCheck.RulingState, UsHouseCountCheck.RulingSection, RulingState, RulingOption));
            card.Add(F("- **The model's seat change**: not computed here - R-US18 is {0} ({1}) by (b), and R-US20 (b) replaces this m with a fitted term; the seat change of that term by (b) is US-27's.", UsHouseCountCheck.RulingState, UsHouseCountCheck.RulingSection));
            card.Add("- **The 2026 midterm's own result**: BILLED until the Clerk's 2026 statistics are saved (expected 2027); then it is printed beside m, never read by the rule.");

            // ---- R-US20's rule
            bool outside = mCell < bandL || mCell > bandU;
            string verdict = !holds ? "none" : outside ? "(b)" : "(a)";
            string verdictLine = !holds ? "**No outcome: the world does not hold the record (the failures above) - by R-US20's block it is not an outcome of the rule, and nothing is registered from it.**" : F("**By R-US20's rule, code recommends {0}: the model's midterm {1} lies {2} the record's range [{3}, {4}]** (full precision: m = {5:0.000000}, L = {6:0.000000}, U = {7:0.000000}; {8}; the legislative cell {9:+0.000;-0.000}). R-US20 is asked with this table.",
                verdict, Pp(mCell), outside ? "outside" : "inside", Pp(bandL), Pp(bandU), mCell, bandL, bandU, government, cell);
            card.Add("");
            card.Add(verdictLine);
            sb.Append("  ").Append(verdictLine.Replace("**", string.Empty)).Append('\n');

            // the rule's reach (§840's review, finding 2): the poll is fitted to the 2024 split m is measured from, so m without the economic vote is the
            // fit's residual; the economic vote moves the share by 100 x cell x d, d in [-1, 1] (the index clamped to 0-100). Printed, never deciding.
            double reach = Math.Abs(mNone) + 100.0 * Math.Max(Math.Abs(LegislativeUnified), Math.Abs(LegislativeDivided));
            double narrower = Math.Min(-bandL, bandU);
            Check(Math.Abs(mCell) <= reach + 1e-9 && Math.Abs(deterioration) <= 1.0 + 1e-12, F("the rule's reach holds: |m| {0:0.000000} within |m without the economic vote| + 100 x the larger legislative cell = {1:0.000000}", Math.Abs(mCell), reach));
            string reachLine = F("**The rule's reach** (printed, never deciding): the poll is fitted to the 2024 House split the midterm is measured from, so m without the economic vote is the fit's residual ({0}); the economic vote moves m by 100 x the cell x the units of deterioration, which the term index bounds to [-1, 1]. So |m| can never exceed {1:0.00} points (the cells: unified {4:+0.000;-0.000}, divided {5:+0.000;-0.000}), and the band's nearer edge is {2:0.00} points from zero: {3}",
                Pp(mNone), reach, narrower, reach < narrower ? "under this rule NO world that holds the record can give (b) - the verdict follows from the band and Table 9.1's cells, not from the world's economy. R-US20 is asked with this stated." : "(b) is reachable under this rule.", LegislativeUnified, LegislativeDivided);
            card.Add("");
            card.Add(reachLine);
            sb.Append("  ").Append(reachLine.Replace("**", string.Empty)).Append('\n');
            foreach (string line in card.Where(l => l.StartsWith("|", StringComparison.Ordinal) || l.StartsWith("- ", StringComparison.Ordinal))) { sb.Append("    ").Append(line).Append('\n'); }

            // ---- the ruling record
            string l2 = Pp(bandL), u2 = Pp(bandU), m2 = Pp(mCell);
            string outcome = F("verdict {0}, government {1}, L {2}, U {3}, m {4}", verdict, government, l2, u2, m2);
            card.Add("");
            card.Add(F("The ruling record: {0}{1}{2}.", RulingState, RulingSection.Length > 0 ? " (" + RulingSection + ")" : "", RulingState == "RULED" ? " - " + RulingOption + ": " + RulingRuled : ""));
            switch (RulingState)
            {
                case "NONE":
                    Check(false, holds ? F("the ruling record NONE: register the ask - RulingState ASKED, its section, and the outcome {0}", outcome) : "the ruling record NONE: nothing to register - the world is not an outcome (the failures above)");
                    break;
                case "ASKED":
                case "RULED":
                    Check(RulingSection.Length > 0, F("the ruling record {0} names its section: '{1}'", RulingState, RulingSection));
                    bool same = RegisteredVerdict == verdict && RegisteredGovernment == government && RegisteredL == l2 && RegisteredU == u2 && RegisteredM == m2;
                    bool verdictSame = RegisteredVerdict == verdict;
                    Check(same, same ? F("the ruling record {0} ({1}): the outcome is the registered one - {2}", RulingState, RulingSection, outcome)
                        : verdictSame ? F("the ruling record {0}: a figure moved with the verdict unchanged - re-register naming its cause and \"verdict unchanged\": {1} (registered: L {2}, U {3}, m {4}, {5})", RulingState, outcome, RegisteredL, RegisteredU, RegisteredM, RegisteredGovernment)
                        : F("the ruling record {0}: the VERDICT CHANGED - {1} against the registered {2}; R-US20 is asked again with the new table, and Elias is told", RulingState, verdict, RegisteredVerdict));
                    if (RulingState == "RULED") { Check(new[] { "(a)", "(b)", "(c)" }.Contains(RulingOption) && RulingRuled.Length > 0, F("RULED names an option that can be built: {0}", RulingOption)); }
                    break;
                default:
                    Check(false, F("the ruling record is NONE, ASKED or RULED, not {0}", RulingState));
                    break;
            }

            sb.Append(F("  measured in {0} ms (the world {1} ms)\n", clock.ElapsedMilliseconds, w.Ms));
            return Digest(csvDigest + blockDigest + RulingState + RulingSection + F("{0:R}|{1:R}|{2:R}|{3:R}|{4:R}|{5:R}", mNone, mPres, mCell, mLow, mHigh, deterioration) + w.Executive + w.Largest);
        }

        private static Row ParseRow(string[] header, string[] cells)
        {
            string Cell(string name) { int i = Array.IndexOf(header, name); if (i < 0 || i >= cells.Length) { throw new InvalidOperationException(F("the record's CSV row '{0}' has no {1}", string.Join(",", cells), name)); } return cells[i]; }
            int? I(string name) => Cell(name) == "-" ? (int?)null : int.Parse(Cell(name), CultureInfo.InvariantCulture);
            long? L(string name) => Cell(name) == "-" ? (long?)null : long.Parse(Cell(name), CultureInfo.InvariantCulture);
            return new Row
            {
                Year = int.Parse(Cell("year"), CultureInfo.InvariantCulture), Kind = Cell("kind"), Congress = int.Parse(Cell("congress"), CultureInfo.InvariantCulture),
                Party = Cell("president_party"), From = int.Parse(Cell("president_from"), CultureInfo.InvariantCulture), Elected = Cell("elected_party"),
                SeatsD = I("seats_d"), SeatsR = I("seats_r"), SeatsOther = I("seats_other"), SeatsVacant = I("seats_vacant"), SeatsTotal = I("seats_total"),
                ClerkR = L("clerk_r"), ClerkD = L("clerk_d"), ClerkTotal = L("clerk_total"), VsChange = Cell("vs_vote_change"), VsDir = Cell("vs_vote_direction"),
                SenateCongress = I("senate_congress").Value, SenatePeriod = Cell("senate_period"), SenateD = I("senate_d").Value, SenateR = I("senate_r").Value,
                SenateOther = I("senate_other").Value, SenateTotal = I("senate_total").Value, SenateMajority = Cell("senate_majority"), SenateHow = Cell("senate_how"),
            };
        }

        private static double ShareOf(Row r, string party) => (double)(party == "R" ? r.ClerkR.Value : r.ClerkD.Value) / (r.ClerkR.Value + r.ClerkD.Value);

        private static (int N, double? Lo, double? Hi) Range(List<double> v) => v.Count == 0 ? (0, (double?)null, (double?)null) : (v.Count, v.Min(), v.Max());

        // ---- the model's world: built as a new US game builds one, stepped day by day to the polling day

        private sealed class Midterm
        {
            public bool Reached, HouseHeld;
            public int Days, HouseChanges;
            public long Ms;
            public DateTime Day, HouseFrom, OathFrom;
            public DateTime? TookOffice;
            public PerceivedPerformance.TermReading Term;
            public Dictionary<string, double> LiveShift, LiveMagnitudes;
            public WorldClock.ExecutiveKind Kind;
            public string PmParty, Executive, Largest;
            /// <summary>PS-6 US-16 with K73: the world's own Congress count of 5 Nov 2024's line, held and seated (see the step loop), and the House it elected.</summary>
            public string CongressCounted;
            public int GameHouseRep, GameHouseDem;
            /// <summary>PS-6 US-27 with K73: the world's own midterm count's line, set aside on its day (the block reads the poll it builds).</summary>
            public string MidtermSetAside;
            /// <summary>The Senate seated on the polling day and its majority (<see cref="UsSenate.MajorityOf"/>, the Vice President read on that day).</summary>
            public bool SenateSeated;
            public string SenateMajority;
            /// <summary>PS-6 US-8: the world's own count of 5 Nov 2024 - the winner's party and the count's line; null where none was held.</summary>
            public string CountedParty, CountedLine;
        }

        /// <summary>The world on the eve of 5 Nov 2024 with the USA the player's country (DEM), stepped in the controller's order - the day, the
        /// player's day tick, the turn at its boundary with no decision anywhere (<see cref="CongressOfRecordDiagnostic"/>'s step) - to 3 Nov 2026 and
        /// read there. Seeded, the RNG and the energy state put back, the host destroyed, the epoch restored.</summary>
        private static Midterm ReadMidterm()
        {
            var result = new Midterm { HouseFrom = DateTime.MinValue, OathFrom = DateTime.MinValue };
            var clock = Stopwatch.StartNew();
            GameObject host = null;
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    SimulationManager.SetEpoch(Epoch);   // before the world exists - SetEpoch's own contract, and a new game's order
                    World world = WorldFactory.CreateDefault();
                    host = new GameObject("UsMidtermCheck");
                    var sim = host.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    Country usa = world.GetCountry(CountryId.USA);
                    sim.PlayerCountryId = CountryId.USA;
                    usa.PlayerPartyAbbrev = PlayerParty;
                    // PS-6 K73 (R-US20's block as amended): the House the world holds is the one its own count of 5 Nov 2024 elects - read once that count is held
                    Dictionary<string, int> house = null;
                    bool Holds(IReadOnlyDictionary<string, int> s) => house != null && s != null && house.All(kv => kv.Value == 0 ? !s.TryGetValue(kv.Key, out int z) || z == 0 : s.TryGetValue(kv.Key, out int k) && k == kv.Value)
                                                                      && s.All(kv => kv.Value == 0 || house.ContainsKey(kv.Key));
                    bool heldBefore = Holds(usa.ParliamentSeats);
                    string partyBefore = usa.Government?.PmParty;
                    for (int day = 0; day < 800 && sim.CurrentDate < PollingDay; day++)
                    {
                        bool boundary = sim.AdvanceDay();
                        sim.AdvanceCountryDayTick(CountryId.USA);
                        if (boundary)
                        {
                            var none = new Dictionary<CountryId, PolicyDecision>();
                            foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }
                            sim.AdvanceTurn(none);
                        }
                        result.Days++;
                        // PS-6 K73 (Elias's ruling of 2026-10-23, "R-US20's midterm world is amended to read the game's own House"): the world's own count of 5 Nov 2024
                        // is held and seats its House on 3 Jan 2025 - the block's world reads it (US-16's DECLARED set-aside retired); the world's own midterm count on
                        // 3 Nov 2026 (its last day) is still set aside - the block reads the poll it builds itself, never the game's midterm
                        UsCongressElection.Contest first2024 = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == new DateTime(2024, 11, 5));
                        if (first2024 != null && house == null)
                        {
                            house = first2024.HouseSeats(CountryId.USA);
                            result.CongressCounted = first2024.Line; result.GameHouseRep = first2024.HouseRep; result.GameHouseDem = first2024.HouseDem;
                        }
                        UsCongressElection.Contest midtermCount = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == PollingDay);
                        if (midtermCount != null) { result.MidtermSetAside ??= midtermCount.Line; usa.UsCongressElections.Remove(midtermCount); }
                        bool heldNow = Holds(usa.ParliamentSeats);
                        if (heldNow && !heldBefore) { result.HouseChanges++; if (result.HouseFrom == DateTime.MinValue) { result.HouseFrom = sim.CurrentDate; } }
                        heldBefore = heldNow;
                        string partyNow = usa.Government?.PmParty;
                        if (partyNow == "REP" && partyBefore != "REP" && result.OathFrom == DateTime.MinValue) { result.OathFrom = sim.CurrentDate; }
                        partyBefore = partyNow;
                    }
                    result.Day = sim.CurrentDate;
                    result.Reached = sim.CurrentDate == PollingDay;
                    result.HouseHeld = Holds(usa.ParliamentSeats);
                    result.TookOffice = EconomicVote.TookOffice(usa);
                    result.LiveShift = EconomicVote.EconomyOverTerm(usa, sim.CurrentDate, out result.Term);   // A-LOOP q15 (review 879 H1): the economic vote alone - R-US20 reads the House, which carries no approval term before the game counts it (US-16)
                    result.LiveMagnitudes = EconomicVote.Magnitudes(usa);
                    result.Kind = usa.Government.Kind;
                    result.PmParty = usa.Government.PmParty;
                    result.Executive = usa.Government.Executive ?? string.Empty;
                    if (usa.UsPresidentialElections.Count == 1) { result.CountedParty = usa.UsPresidentialElections[0].WinnerParty; result.CountedLine = usa.UsPresidentialElections[0].Line; }
                    result.Largest = usa.ParliamentSeats.OrderByDescending(kv => kv.Value).First().Key;
                    result.SenateSeated = usa.SecondChamberSeats != null && usa.SecondChamberSeats.Count > 0;
                    result.SenateMajority = result.SenateSeated ? UsSenate.MajorityOf(usa.SecondChamberSeats, UsSenate.VicePresidentParty(usa, sim.CurrentDate)) : null;
                }
            }
            finally
            {
                if (host != null) { UnityEngine.Object.DestroyImmediate(host); }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }

            result.Ms = clock.ElapsedMilliseconds;
            return result;
        }

        // ---- US-5's House history: the catalog's sums, the held fit (UsNationalVoteCheck's HeldFit, at the median spread G2 rules)

        private static double[] HouseShares(int year)
        {
            long r = 0, d = 0, t = 0;
            foreach (var h in UsPresidentialReturns.House) { if (h.Year == year) { r += h.VotesR; d += h.VotesD; t += h.VotesTotal; } }
            if (t <= 0) { throw new InvalidOperationException(F("the catalog holds no House vote of {0}", year)); }
            return new[] { (double)r / t, (double)d / t };
        }

        private static double TwoParty(double[] shares) => shares[0] / (shares[0] + shares[1]);

        private static double Median(List<double> values)
        {
            List<double> v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? double.NaN : v.Count % 2 == 1 ? v[v.Count / 2] : 0.5 * (v[v.Count / 2 - 1] + v[v.Count / 2]);
        }

        /// <summary>The electorate with <paramref name="e"/>'s σ and τ, its mean at <paramref name="place"/> on the segment from DEM's point (0) to REP's (1).</summary>
        private static VoteModel.Electorate Along(VoteModel.PartyPoint[] points, VoteModel.Electorate e, double place)
            => new VoteModel.Electorate(points[1].Econ + place * (points[0].Econ - points[1].Econ), points[1].Soc + place * (points[0].Soc - points[1].Soc), e.Sigma, e.Tau);

        private static double RepAt(VoteModel.PartyPoint[] points, double w, VoteModel.Electorate e, double place) => VoteModel.PredictShares(points, Along(points, e, place), w)[0];

        // ---- the US card's block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsMidtermCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string Root() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }

        // ---- the record document's formats (Tools/us_midterm_record.pl's)

        private static string[] Cells(string line) => line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
        private static string PartyName(string p) => p == "R" ? "REP" : p == "D" ? "DEM" : p;
        private static string SenateCell(Row r)
        {
            string how = r.SenateHow == "seats-with-caucus" ? ", with its caucusing independents" : r.SenateHow == "equal-vp" ? ", an equal division - the Vice President's party"
                       : r.SenateHow == "page" ? ", the page's - independents of no caucus named" : "";
            return F("{0} (Congress {1}{2}{3})", PartyName(r.SenateMajority), r.SenateCongress, r.SenatePeriod == "-" ? "" : ", " + r.SenatePeriod.Replace("..", " to "), how);
        }
        private static string Pct(double share) => F("{0:0.00} %", 100.0 * share);
        private static string Pp(double? points) { if (!points.HasValue) { return "-"; } string s = F("{0:+0.00;-0.00}", points.Value); return s == "-0.00" ? "+0.00" : s; }
        private static string Sc(int? seats) => seats.HasValue ? F("{0:+0;-0;+0}", seats.Value) : "-";
        private static string Vs(double? v) => v.HasValue ? (v.Value < 0 ? "-" : "+") + Math.Abs(v.Value).ToString("R", CultureInfo.InvariantCulture) : "-";
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
