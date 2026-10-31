using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.UI;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SP-5 (S5, Design's return of 2026-10-30): **FOUNDING IN GERMANY, HELD AGAINST THE LAW ON DISK.** Every legal figure `BallotAccess` and `PartyFunding`
    /// carry is read back out of the primary source it came from - the files under `ElectionsData/germany/raw/ballot_access/` and `party_funding/`,
    /// each first matched to its folder's SHA256SUMS.txt - so the check fails while a constant and its source disagree: the BWahlG's days (§18 97 and 79,
    /// §19 69), §27's 1 per mille and 2,000, §20's 200; the 2025 snap's three dates on the Federal Returning Officer's page against
    /// <see cref="BallotAccess.Deadlines"/>; her table of the sixteen Land-list counts against <see cref="BallotAccess.LandListNeed"/>, one by one; PartG §18's
    /// 0.5 % and 1.0 % lines and four million votes, and Drucksache 21/160's 1.21 € and 0.99 €. Then the task as the HQ plays it, on a scripted German party:
    /// offered, committed, staged with its stats' day 0; a GATHER counts from the next day and is refused past the day's volunteers; FILE files the notice and
    /// the full lists; a deadline that passes short refuses; the count drops the refused Länder's share (and the sum stands); and with no created party the
    /// count is the very array it was.
    /// </summary>
    public static class BallotAccessCheck
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        private static string Root => Path.Combine(Directory.GetCurrentDirectory(), "ElectionsData", "germany", "raw");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== BallotAccessCheck (SP-5): founding in Germany - the law on disk, the task, the count, the funding line ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable created = CreatedParties.Scope();
            try
            {
                CreatedParties.Clear();

                // ── the sources: every file its folder's digest ──
                foreach (string folder in new[] { "ballot_access", "party_funding" })
                {
                    string dir = Path.Combine(Root, folder);
                    string sums = Path.Combine(dir, "SHA256SUMS.txt");
                    int rows = 0, matched = 0;
                    var bad = new List<string>();
                    if (File.Exists(sums))
                    {
                        foreach (string line in File.ReadAllLines(sums))
                        {
                            Match m = Regex.Match(line, @"^([0-9a-f]{64}) \*?(\S+)$");
                            if (!m.Success) { continue; }
                            rows++;
                            string path = Path.Combine(dir, m.Groups[2].Value);
                            string have = File.Exists(path) ? ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path)) : "missing";
                            if (have == m.Groups[1].Value) { matched++; } else { bad.Add(m.Groups[2].Value + " " + have.Substring(0, Math.Min(12, have.Length))); }
                        }
                    }
                    Check(rows > 0 && matched == rows, F("{0}/: {1} of {2} files match SHA256SUMS.txt{3}", folder, matched, rows, bad.Count > 0 ? " - " + string.Join(", ", bad) : string.Empty));
                }

                // ── the statute (gesetze-im-internet.de, ISO-8859-1) ──
                string s18 = Text("ballot_access/bwahlg_18.html", latin1: true), s19 = Text("ballot_access/bwahlg_19.html", latin1: true);
                string s20 = Text("ballot_access/bwahlg_20.html", latin1: true), s27 = Text("ballot_access/bwahlg_27.html", latin1: true);
                string s52 = Text("ballot_access/bwahlg_52.html", latin1: true);
                Check(BallotAccess.NoticeDaysBefore == 97 && s18.Contains("spätestens am siebenundneunzigsten Tage vor der Wahl bis 18 Uhr dem Bundeswahlleiter ihre Beteiligung"),
                    F("BWahlG §18(2): the notice by the 97th day ('siebenundneunzigsten Tage') = NoticeDaysBefore {0}", BallotAccess.NoticeDaysBefore));
                Check(BallotAccess.RecognitionDaysBefore == 79 && s18.Contains("spätestens am neunundsiebzigsten Tage vor der Wahl"),
                    F("BWahlG §18(4): recognition by the 79th day = RecognitionDaysBefore {0}", BallotAccess.RecognitionDaysBefore));
                Check(BallotAccess.ListsDaysBefore == 69 && s19.Contains("spätestens am neunundsechzigsten Tage vor der Wahl bis 18 Uhr"),
                    F("BWahlG §19: the lists by the 69th day = ListsDaysBefore {0}", BallotAccess.ListsDaysBefore));
                Check(BallotAccess.PerMille == 1 && BallotAccess.LandListCap == 2000 && s27.Contains("von 1 vom Tausend der Wahlberechtigten des Landes bei der letzten Bundestagswahl, jedoch höchstens 2 000 Wahlberechtigten"),
                    F("BWahlG §27(1): a Land list signed by {0} per mille of the Land's electorate at the last Bundestag election, at most {1}", BallotAccess.PerMille, BallotAccess.LandListCap));
                Check(BallotAccess.PerConstituency == 200 && s20.Contains("von mindestens 200 Wahlberechtigten des Wahlkreises") && s20.Contains("wenn für die Partei in dem betreffenden Land eine Landesliste zugelassen wird"),
                    F("BWahlG §20(2): {0} signatures per constituency candidate, admitted only where the Land list is", BallotAccess.PerConstituency));
                Check(s52.Contains("im Falle einer Auflösung des Deutschen Bundestages die in dem Bundeswahlgesetz und in der Bundeswahlordnung bestimmten Fristen und Termine durch Rechtsverordnung"),
                    "BWahlG §52(3): on a dissolution the Interior Ministry may shorten the deadlines by ordinance (BGBl. 2024 I Nr. 436 did, for 2025)");

                // ── the 2025 snap's dates (the Federal Returning Officer's dates page) ──
                string termine = Text("ballot_access/bwl_btw25_termine.html", latin1: false);
                BallotAccess.Deadlines(BallotAccess.Snap2025, out DateTime notice, out DateTime recognition, out DateTime lists);
                Check(termine.Contains("Di, 07.01.2025 18:00 (47. Tag vor der Wahl) Letzter Tag") && notice == new DateTime(2025, 1, 7) && BallotAccess.DayBefore(BallotAccess.Snap2025, notice) == 47,
                    F("2025: the notice by Tue 7 Jan 2025 18:00, the 47th day - Deadlines gives {0:yyyy-MM-dd} (day {1})", notice, BallotAccess.DayBefore(BallotAccess.Snap2025, notice)));
                Check(termine.Contains("Di, 14.01.2025 (40. Tag vor der Wahl) Letzter Tag für die Feststellung") && recognition == new DateTime(2025, 1, 14),
                    F("2025: recognition by Tue 14 Jan 2025, the 40th day - Deadlines gives {0:yyyy-MM-dd}", recognition));
                Check(termine.Contains("Mo, 20.01.2025 18:00 (34. Tag vor der Wahl) Letzter Tag") && termine.Contains("Landeslisten bei der Landeswahlleitung") && lists == new DateTime(2025, 1, 20),
                    F("2025: the lists by Mon 20 Jan 2025 18:00, the 34th day - Deadlines gives {0:yyyy-MM-dd}", lists));
                BallotAccess.Deadlines(new DateTime(2029, 3, 4), out DateTime n29, out DateTime _, out DateTime l29);
                Check(BallotAccess.DayBefore(new DateTime(2029, 3, 4), n29) == 97 && BallotAccess.DayBefore(new DateTime(2029, 3, 4), l29) == 69,
                    "an election other than the snap reads the statute's own days (97, 69)");

                // ── the sixteen counts (her candidates' page) against the derivation ──
                string bewerber = Text("ballot_access/bwl_btw25_informationen-wahlbewerber.html", latin1: false);
                Check(bewerber.Contains("bleibt die erforderliche Zahl der Unterstützungsunterschriften für Parteien unverändert"), "2025: the signature counts unchanged by the snap (her page's own words)");
                Match table = Regex.Match(bewerber, @"Landeslisten in den einzelnen Ländern: Land Anzahl (.+?) Die Unterschriften");
                var published = new Dictionary<string, int>();
                if (table.Success)
                {
                    foreach (Match row in Regex.Matches(table.Groups[1].Value, @"([A-ZÄÖÜ][\p{L}\-]+(?:-[\p{L}]+)?) ([0-9]{1,3}(?:\.[0-9]{3})?)"))
                    {
                        published[row.Groups[1].Value] = int.Parse(row.Groups[2].Value.Replace(".", string.Empty), CultureInfo.InvariantCulture);
                    }
                }
                var mismatched = new List<string>();
                int agreed = 0;
                for (int r = 0; r < BallotAccess.LandCount; r++)
                {
                    string name = GermanRegions.NameAt(r);
                    int need = BallotAccess.LandListNeed(r, BallotAccess.Snap2025);
                    if (published.TryGetValue(name, out int count) && count == need) { agreed++; } else { mismatched.Add(F("{0} derived {1} published {2}", name, need, published.TryGetValue(name, out int c2) ? c2.ToString(CultureInfo.InvariantCulture) : "none")); }
                }
                Check(published.Count == 16 && agreed == 16, F("the sixteen Land-list counts: {0} of 16 derived (1 per mille of 2021's register, rounded up, at most 2,000) equal her published table ({1} read), total {2}{3}",
                    agreed, published.Count, BallotAccess.TotalListNeed(BallotAccess.Snap2025), mismatched.Count > 0 ? " - " + string.Join("; ", mismatched) : string.Empty));

                // ── PartG §18 and Drucksache 21/160 ──
                string partg = Text("party_funding/partg_18.html", latin1: true);
                Check(PartyFunding.BundestagLine == 0.005 && PartyFunding.LandtagLine == 0.010 && partg.Contains("mindestens 0,5 vom Hundert oder einer Landtagswahl 1,0 vom Hundert der für die Listen abgegebenen gültigen Stimmen"),
                    "PartG §18(4): 0.5 % of the list votes at a Bundestag election, or 1.0 % at a Landtag election, qualifies");
                Check(PartyFunding.HigherRateVotes == 4_000_000 && partg.Contains("bis zu vier Millionen gültigen Stimmen 1 Euro je Stimme") && partg.Contains("erhöhen sich ab dem Jahr 2017"),
                    "PartG §18(3): four million votes at the higher rate, both rates indexed from 2017");
                string drucksache = PdfText(Path.Combine(Root, "party_funding", "btd_21-160.pdf"));
                Check(PartyFunding.PerVoteFirst == 1.21 && PartyFunding.PerVoteBeyond == 0.99 && PartyFunding.RatesYear == 2025
                      && drucksache.Contains("Anspruchsjahr 2025 zu einer Anhebung auf 1,21 Euro für die ersten vier Millionen Wählerstimmen und 0,99 Euro für weitere Wählerstimmen"),
                    F("Drucksache 21/160: the claim year 2025 at {0:0.00} € and {1:0.00} € a vote ({2} characters read)", PartyFunding.PerVoteFirst, PartyFunding.PerVoteBeyond, drucksache.Length));
                long votes = PartyFunding.ListVotes(0.01);
                Check(PartyFunding.YearSum(0.0049, PartyFunding.ListVotes(0.0049)) == 0.0 && Math.Abs(PartyFunding.YearSum(0.01, votes) - votes * 1.21) < 1e-6 && PartyFunding.Funds(CountryId.Germany) && !PartyFunding.Funds(CountryId.Sweden),
                    F("the funding line: none below 0.5 %; at 1 % ({0:N0} list votes) {1:N0} € a year, the votes' part only", votes, PartyFunding.YearSum(0.01, votes)));

                // ── the task, played ──
                const CountryId de = CountryId.Germany;
                double[] before = { 0.3, 0.2, 0.5 };
                Check(ReferenceEquals(BallotAccess.ApplyToCount(de, new[] { "CDU", "SPD", "AFD" }, before, BallotAccess.Snap2025), before) && BallotAccess.StandsIn(de, "CDU", 0, BallotAccess.Snap2025),
                    "no created party: the count is the very array it was, and every party stands where its returns say");
                Check(PartyCreationFlow.Offered(de) && BallotAccess.RulesOf(de) == BallotAccess.Rules.Sourced && BallotAccess.RulesOf(CountryId.Poland) == BallotAccess.Rules.Absent && BallotAccess.RulesOf(CountryId.Sweden) == BallotAccess.Rules.NotModelled,
                    "Germany offered with its rules sourced; Poland and Italy ABSENT until on disk; Sweden as built");
                CreatedParty d = PartyCreationFlow.NewDraft(de);
                d.Name = "Neue Mitte"; d.ShortName = "NM"; d.Key = PartyCreationFlow.KeyFrom(de, d.ShortName); d.LeaderName = "A. Probe";
                Check(PartyCreationFlow.TryCommit(d, out string refused), "a German draft commits: " + (refused ?? d.Key));
                string[] cast = LiveCampaignSetup.Keys(de);
                int me = Array.IndexOf(cast, d.Key);
                bool staged = LiveCampaignSetup.TryFor(de, new (int, int, Scandal)[0], null, out CampaignRun.Setup setup, out string _);
                bool inCast = staged && me >= 0 && setup.Parties != null && me < setup.Parties.Length;
                double chest = inCast ? setup.Parties[me].StartingMoney : -1.0;
                int volunteers = inCast ? setup.Parties[me].Volunteers : -1;
                Check(inCast && Math.Abs(chest - LiveCampaignSetup.WarChest * PartyOrigins.Pip(PartyOrigins.FundingShare, d.Funding)) < 1e-6 && volunteers == BallotAccess.VolunteersOf(d),
                    F("Germany's staging reads the created party's day 0 from its stats: chest {0:N0}, volunteers {1} (expected {2})", chest, volunteers, BallotAccess.VolunteersOf(d)));
                DateTime pd = BallotAccess.Snap2025;
                DateTime day1 = new DateTime(2024, 11, 6), day2 = day1.AddDays(1);
                BallotAccess.EnsureFor(d, pd);
                int bremen = Enumerable.Range(0, BallotAccess.LandCount).First(r => GermanRegions.NameAt(r) == "Bremen");
                bool g1 = BallotAccess.TryGather(d, bremen, 2, pd, day1, out string _);
                bool g2 = BallotAccess.TryGather(d, bremen, 2, pd, day1, out string full);
                Check(g1 && !g2 && full != null && BallotAccess.Signatures(d, bremen, pd, day1) == 0 && BallotAccess.Signatures(d, bremen, pd, day2) == Math.Min(BallotAccess.YieldOf(d, 2), BallotAccess.LandListNeed(bremen, pd)),
                    F("GATHER x2 on Bremen: counts from the next day ({0} signatures), a second x2 the same day refused ('{1}')", BallotAccess.Signatures(d, bremen, pd, day2), full));
                for (DateTime t = day2; BallotAccess.Signatures(d, bremen, pd, t) < BallotAccess.LandListNeed(bremen, pd) && t < notice; t = t.AddDays(1)) { BallotAccess.TryGather(d, bremen, 3, pd, t, out string _); }
                DateTime fileDay = new DateTime(2025, 1, 6);
                int filedLists = BallotAccess.File(d, pd, fileDay, out bool noticeFiled);
                Check(noticeFiled && filedLists == 1 && BallotAccess.FiledOn(d, bremen, pd) == fileDay && BallotAccess.StateOf(de, d, pd, fileDay) == BallotAccess.TaskState.Collecting,
                    F("FILE on 6 Jan: the notice and Bremen's full list ({0} filed); still collecting", filedLists));
                Check(BallotAccess.StandsIn(de, d.Key, 0, lists) && BallotAccess.StateOf(de, d, pd, lists) == BallotAccess.TaskState.Collecting,
                    "until the lists' deadline the party stands everywhere and nothing is refused");
                DateTime after = lists.AddDays(1);
                int refusedLands = BallotAccess.RefusedCount(d, pd, after);
                Check(refusedLands == 15 && BallotAccess.StateOf(de, d, pd, after) == BallotAccess.TaskState.Refused && BallotAccess.StandsIn(de, d.Key, bremen, pd) && !BallotAccess.StandsIn(de, d.Key, (bremen + 1) % 16, pd),
                    F("past 20 Jan: REFUSED in {0} Länder; on polling day it stands in Bremen alone", refusedLands));
                var keys = new List<string>(cast);
                var shares = new double[keys.Count];
                for (int i = 0; i < shares.Length; i++) { shares[i] = 1.0 / shares.Length; }
                double[] counted = BallotAccess.ApplyToCount(de, keys, shares, pd);
                double weightBremen = GermanRegions.ValidAt(bremen, pd), weightAll = Enumerable.Range(0, 16).Sum(r => (double)GermanRegions.ValidAt(r, pd));
                double expectedRaw = shares[me] * weightBremen / weightAll;
                Check(!ReferenceEquals(counted, shares) && Math.Abs(counted.Sum() - shares.Sum()) < 1e-12 && counted[me] < shares[me] && Math.Abs(counted[me] / counted.Where((v, i) => i != me).Sum() - expectedRaw / shares.Where((v, i) => i != me).Sum()) < 1e-9,
                    F("the count: the refused party keeps Bremen's weight of its share ({0:P2} of {1:P2}), the rest spread in proportion, the sum unchanged", counted[me], shares[me]));
                bool nextKnown = WorldClock.TryNextPollingDay(de, pd.AddDays(1), out DateTime nextPoll);
                Check(nextKnown && BallotAccess.StandsIn(de, d.Key, (bremen + 1) % 16, nextPoll) && BallotAccess.TaskIsFor(d, pd) && !BallotAccess.TaskIsFor(d, nextPoll),
                    F("a later election ({0:yyyy-MM-dd}): the first election's task is kept (its record survives the count) and the later one's, not modelled, reads as standing (DECLARED)", nextPoll));
                RegionalVoteModel.RegionInput[] lands = GermanRegions.Regions(keys, pd);
                Check(lands[bremen].PartyAvailable[me] && lands.Count(l => l.PartyAvailable[me]) == 1, "GermanRegions on polling day: the created party available in Bremen alone");
                CreatedParty late = PartyCreationFlow.NewDraft(de);
                late.Name = "Spät"; late.ShortName = "SP2"; late.Key = PartyCreationFlow.KeyFrom(de, late.ShortName); late.LeaderName = "B. Probe";
                PartyCreationFlow.TryCommit(late, out string _);
                BallotAccess.EnsureFor(late, pd);
                Check(BallotAccess.RefusedCount(late, pd, notice.AddDays(1)) == 16 && BallotAccess.File(late, pd, notice.AddDays(1), out bool lateNotice) == 0 && !lateNotice,
                    "no notice by 7 Jan: every list refused at once, and FILE can no longer file it");
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            finally { CreatedParties.Clear(); }

            if (failures > 0) { Debug.LogError($"BALLOT ACCESS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>A fetched HTML page as plain text: scripts and styles dropped, tags to spaces, the entities these pages use decoded, whitespace folded.
        /// gesetze-im-internet.de serves ISO-8859-1 (each byte its own code point), the Federal Returning Officer UTF-8.</summary>
        private static string Text(string relative, bool latin1)
        {
            string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) { return string.Empty; }
            byte[] bytes = File.ReadAllBytes(path);
            string s;
            if (latin1) { var chars = new char[bytes.Length]; for (int i = 0; i < bytes.Length; i++) { chars[i] = (char)bytes[i]; } s = new string(chars); }
            else { s = Encoding.UTF8.GetString(bytes); }
            s = Regex.Replace(s, @"<script.*?</script>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<style.*?</style>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<[^>]+>", " ");
            s = s.Replace("&shy;", string.Empty).Replace("­", string.Empty).Replace("&nbsp;", " ").Replace(" ", " ").Replace("&quot;", "\"").Replace("&amp;", "&");
            s = Regex.Replace(s, @"&#(\d+);", m => ((char)int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToString());
            return Regex.Replace(s, @"\s+", " ");
        }

        /// <summary>A PDF's literal text strings, stream by stream (Flate-inflated, the two-byte zlib header skipped), octal escapes decoded as Latin-1 -
        /// enough to read a Bundestag Drucksache's sentences back; whitespace folded.</summary>
        private static string PdfText(string path)
        {
            if (!File.Exists(path)) { return string.Empty; }
            byte[] b = File.ReadAllBytes(path);
            var latin = new StringBuilder(b.Length);
            foreach (byte x in b) { latin.Append((char)x); }
            string all = latin.ToString();
            var text = new StringBuilder();
            foreach (Match m in Regex.Matches(all, "stream\r?\n(.*?)\r?\nendstream", RegexOptions.Singleline))
            {
                string raw = m.Groups[1].Value;
                if (raw.Length < 3) { continue; }
                var data = new byte[raw.Length - 2];
                for (int i = 2; i < raw.Length; i++) { data[i - 2] = (byte)raw[i]; }
                string inflated;
                try
                {
                    using (var input = new MemoryStream(data))
                    using (var z = new DeflateStream(input, CompressionMode.Decompress))
                    using (var output = new MemoryStream())
                    {
                        z.CopyTo(output);
                        var sb = new StringBuilder();
                        foreach (byte x in output.ToArray()) { sb.Append((char)x); }
                        inflated = sb.ToString();
                    }
                }
                catch (Exception) { continue; }
                foreach (Match s in Regex.Matches(inflated, @"\(((?:[^()\\]|\\.)*)\)", RegexOptions.Singleline))
                {
                    string lit = Regex.Replace(s.Groups[1].Value, @"\\([0-7]{3})", e => ((char)Convert.ToInt32(e.Groups[1].Value, 8)).ToString());
                    lit = Regex.Replace(lit, @"\\(.)", "$1");
                    text.Append(lit);
                }
                text.Append(' ');
            }
            return Regex.Replace(text.ToString(), @"\s+", " ");
        }
    }
}
