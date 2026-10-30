using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SP-8 (prereg P1-P5, `docs/specs/FRANCE_PRESIDENTIAL_START_PLAN.md`): FRANCE'S 2022 PRESIDENTIAL RECORD, ASSERTED AGAINST ITS PAGES. It re-reads the files
    /// under `ElectionsData/france/raw/` and fails while any statement below is false:
    /// (a) every file of `presidential2022/` matches its line in the folder's SHA256SUMS.txt;
    /// (b) the generated catalog (`FrenchPresidentialReturns`) equals the Interior's files figure for figure - every unit's registered and cast, every unit's
    /// and France entière's votes - and the Conseil's declarations (CC-195, CC-197) figure for figure; P1 (a): the units summed equal France entière; P1 (b):
    /// the fields where the Conseil and the Interior differ are printed;
    /// (c) the dates the catalog carries are the pages' own (the list of candidates, the rounds, the proclamations, the investiture, the terms) and the run-off's
    /// 20:00 estimate is Ipsos's page's;
    /// (d) France's rule (`TwoRoundElection.RecordRuleOf`) is the Constitution's Art. 6-7 as the Conseil serves it, and the game holds no French election
    /// (`RuleOf` null); the 2022 first round falls in the window Art. 7 sets before the first term's end;
    /// The night's four states and the tiles (the branch's (e) and (f)) land with the night itself at SP-8b (K332): no game path opens them while the card is LOCKED.
    /// </summary>
    public static class FrenchPresidentialRecordCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== FrenchPresidentialRecordCheck (SP-8): France's 2022 presidential record against its pages ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string raw = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "france", "raw"));
                string folder = Path.Combine(raw, "presidential2022");

                // (a) the digests
                var sums = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(Path.Combine(folder, "SHA256SUMS.txt")))
                {
                    Match m = Regex.Match(line, @"^([0-9a-f]{64}) \*?(.+)$");
                    if (m.Success) { sums[m.Groups[2].Value.Trim()] = m.Groups[1].Value; }
                }
                var digestMiss = new List<string>();
                foreach (KeyValuePair<string, string> kv in sums)
                {
                    string path = Path.Combine(folder, kv.Key);
                    if (!File.Exists(path)) { digestMiss.Add(kv.Key + " missing"); continue; }
                    if (Sha256(File.ReadAllBytes(path)) != kv.Value) { digestMiss.Add(kv.Key); }
                }
                foreach (string path in Directory.GetFiles(folder))
                {
                    string name = Path.GetFileName(path);
                    if (name != "SHA256SUMS.txt" && name != "fetch_log.txt" && !sums.ContainsKey(name)) { digestMiss.Add(name + " unlisted"); }
                }
                Check(sums.Count > 0 && digestMiss.Count == 0, F("(a) every source file of presidential2022/ ({0}) is listed in SHA256SUMS.txt and matches it{1}", sums.Count, digestMiss.Count > 0 ? "; NOT: " + string.Join(", ", digestMiss) : string.Empty));

                // (b) the Interior's files against the catalog
                for (int round = 1; round <= 2; round++)
                {
                    List<string[]> dpt = Rows(Path.Combine(folder, "resultats-par-niveau-dpt-t" + round + "-france-entiere.txt"));
                    List<string[]> fe = Rows(Path.Combine(folder, "resultats-par-niveau-fe-t" + round + "-france-entiere.txt"));
                    int unitMiss = 0, unitRows = 0, voteMiss = 0;
                    var sum = new Dictionary<string, long>();
                    foreach (string[] f in dpt)
                    {
                        unitRows++;
                        string code = f[0];
                        long registered = L(f[3]), cast = L(f[14]);
                        Add(sum, "registered", registered); Add(sum, "voters", L(f[6])); Add(sum, "blank", L(f[8])); Add(sum, "null", L(f[11])); Add(sum, "cast", cast);
                        bool unitFound = false;
                        foreach (var u in FrenchPresidentialReturns.Units) { if (u.Round == round && u.Code == code) { unitFound = u.Registered == registered && u.Cast == cast && u.Name == f[1]; } }
                        if (!unitFound) { unitMiss++; }
                        for (int i = 17; i + 5 < f.Length; i += 6)
                        {
                            string surname = f[i + 1]; long votes = L(f[i + 3]);
                            Add(sum, "v:" + surname, votes);
                            bool found = false;
                            foreach (var v in FrenchPresidentialReturns.UnitVotes) { if (v.Round == round && v.Code == code && v.Surname == surname) { found = v.Votes == votes; } }
                            if (!found) { voteMiss++; }
                        }
                    }
                    int catalogUnits = 0;
                    foreach (var u in FrenchPresidentialReturns.Units) { if (u.Round == round) { catalogUnits++; } }
                    Check(unitRows > 0 && unitMiss == 0 && voteMiss == 0 && catalogUnits == unitRows,
                        F("(b) round {0}: the catalog holds the Interior's {1} units as filed (name, registered, cast) and every unit's votes ({2} units off, {3} votes off, {4} in the catalog)", round, unitRows, unitMiss, voteMiss, catalogUnits));
                    string[] t = fe[0];
                    var feTotals = new Dictionary<string, long> { { "registered", L(t[3]) }, { "voters", L(t[6]) }, { "blank", L(t[8]) }, { "null", L(t[11]) }, { "cast", L(t[14]) } };
                    var feVotes = new Dictionary<string, long>();
                    for (int i = 17; i + 4 < t.Length; i += 7) { feVotes[t[i + 2]] = L(t[i + 4]); }
                    FrenchPresidentialReturns.Totals counted = default;
                    foreach (var c in FrenchPresidentialReturns.CountedTotals) { if (c.Round == round) { counted = c; } }
                    bool feSame = counted.Registered == feTotals["registered"] && counted.Voters == feTotals["voters"] && counted.Blank == feTotals["blank"] && counted.Null == feTotals["null"] && counted.Cast == feTotals["cast"];
                    int feVoteMiss = 0, feVoteCount = 0;
                    foreach (var v in FrenchPresidentialReturns.CountedVotes) { if (v.Round != round) { continue; } feVoteCount++; if (!feVotes.TryGetValue(v.Surname, out long fv) || fv != v.Votes) { feVoteMiss++; } }
                    Check(feSame && feVoteMiss == 0 && feVoteCount == feVotes.Count, F("(b) round {0}: the catalog's France entière is the Interior's - cast {1:N0}, {2} candidates ({3} off)", round, feTotals["cast"], feVotes.Count, feVoteMiss));
                    // P1 (a): the units summed equal France entière
                    var p1a = new List<string>();
                    foreach (KeyValuePair<string, long> kv in feTotals) { if (!sum.TryGetValue(kv.Key, out long s) || s != kv.Value) { p1a.Add(kv.Key); } }
                    foreach (KeyValuePair<string, long> kv in feVotes) { if (!sum.TryGetValue("v:" + kv.Key, out long s) || s != kv.Value) { p1a.Add(kv.Key); } }
                    Check(p1a.Count == 0, F("(b) P1 (a) round {0}: the {1} units summed equal France entière on registered, voters, blank, null, cast and every candidate{2}", round, unitRows, p1a.Count > 0 ? "; NOT: " + string.Join(", ", p1a) : string.Empty));

                    // the Conseil's declaration against the catalog, and P1 (b) against the Interior
                    string ccPath = round == 1 ? Path.Combine(raw, "executive", "cc_2022-195PDR_premier-tour.html") : Path.Combine(raw, "records", "cc_2022-197PDR_proclamation.html");
                    string cc = TextOf(ccPath);
                    Match m = Regex.Match(cc, @"Électeurs inscrits : ([\d ]+?) Votants : ([\d ]+?) Bulletins blancs : ([\d ]+?) Bulletins nuls : ([\d ]+?) Suffrages exprimés : ([\d ]+?) Majorité absolue : [\d ]+? Ont obtenu : (.*?)(?: Article 2| Ainsi,)");
                    if (!m.Success) { Check(false, F("(b) round {0}: the Conseil's totals not found in {1}", round, Path.GetFileName(ccPath))); continue; }
                    var ccVotes = new Dictionary<string, long>();
                    foreach (Match v in Regex.Matches(m.Groups[6].Value, @"(?:M\.|Mme) (.+?) (\p{Lu}[\p{Lu}\- ]*\p{Lu}) : ([\d ]+?)(?= (?:M\.|Mme) |$)")) { ccVotes[v.Groups[2].Value] = L(v.Groups[3].Value); }
                    FrenchPresidentialReturns.Totals proclaimed = default;
                    foreach (var c in FrenchPresidentialReturns.ProclaimedTotals) { if (c.Round == round) { proclaimed = c; } }
                    bool ccSame = proclaimed.Registered == L(m.Groups[1].Value) && proclaimed.Voters == L(m.Groups[2].Value) && proclaimed.Blank == L(m.Groups[3].Value) && proclaimed.Null == L(m.Groups[4].Value) && proclaimed.Cast == L(m.Groups[5].Value);
                    int ccMiss = 0;
                    var differ = new List<string>();
                    foreach (var v in FrenchPresidentialReturns.ProclaimedVotes)
                    {
                        if (v.Round != round) { continue; }
                        if (!ccVotes.TryGetValue(v.Surname, out long cv) || cv != v.Votes) { ccMiss++; }
                        if (feVotes.TryGetValue(v.Surname, out long fv2) && fv2 != v.Votes) { differ.Add(v.Surname + " Conseil " + v.Votes + " / Interior " + fv2); }
                    }
                    if (proclaimed.Cast != counted.Cast) { differ.Add("cast Conseil " + proclaimed.Cast + " / Interior " + counted.Cast); }
                    Check(ccSame && ccMiss == 0 && ccVotes.Count == feVotes.Count, F("(b) round {0}: the catalog's proclaimed figures are the Conseil's ({1}) - cast {2:N0}, {3} candidates ({4} off)", round, Path.GetFileName(ccPath), L(m.Groups[5].Value), ccVotes.Count, ccMiss));
                    sb.Append(F("    P1 (b)    round {0}: {1} field(s) differ between the Conseil and the Interior{2}\n", round, differ.Count, differ.Count > 0 ? " - " + string.Join("; ", differ) : string.Empty));
                }

                // (c) the dates and the estimate, re-read from their pages
                string c187 = TextOf(Path.Combine(folder, "cc_2022-187PDR_liste-des-candidats.html"));
                string c171 = TextOf(Path.Combine(folder, "cc_2017-171PDR_proclamation.html"));
                string c169 = TextOf(Path.Combine(folder, "cc_2017-169PDR_premier-tour.html"));
                string c195 = TextOf(Path.Combine(raw, "executive", "cc_2022-195PDR_premier-tour.html"));
                string c197 = TextOf(Path.Combine(raw, "records", "cc_2022-197PDR_proclamation.html"));
                string inv = TextOf(Path.Combine(folder, "elysee_20220507_ceremonie-investiture.html"));
                string ips = TextOf(Path.Combine(folder, "ipsos_20220506_estimations-second-tour.html"));
                Check(c187.Contains("Liste des candidats à l'élection présidentielle") && Day(c187, @"Rendu public le (\d+(?:er)?) (\p{L}+) (\d{4})") == FrenchPresidentialReturns.CandidatesListed,
                    F("(c) the list of candidates made public {0:yyyy-MM-dd} [CC-187] - the start's day (K174)", FrenchPresidentialReturns.CandidatesListed));
                Check(Day(c195, @"les \d+ et (\d+) (\p{L}+) (\d{4})") == FrenchPresidentialReturns.FirstRound && Day(c195, @"Rendu public le (\d+(?:er)?) (\p{L}+) (\d{4})") == FrenchPresidentialReturns.FirstRoundProclaimed
                      && Day(c197, @"les \d+ et (\d+) (\p{L}+) (\d{4})") == FrenchPresidentialReturns.RunOff && Day(c197, @"Rendu public le (\d+(?:er)?) (\p{L}+) (\d{4})") == FrenchPresidentialReturns.RunOffProclaimed
                      && Day(c197, @"française à compter du (\d+) (\p{L}+) (\d{4}) à 0 heure") == FrenchPresidentialReturns.SecondTermFrom,
                    F("(c) the rounds {0:yyyy-MM-dd} and {1:yyyy-MM-dd}, declared {2:yyyy-MM-dd} and proclaimed {3:yyyy-MM-dd}, the second term from {4:yyyy-MM-dd} [CC-195] [CC-197]",
                        FrenchPresidentialReturns.FirstRound, FrenchPresidentialReturns.RunOff, FrenchPresidentialReturns.FirstRoundProclaimed, FrenchPresidentialReturns.RunOffProclaimed, FrenchPresidentialReturns.SecondTermFrom));
                Check(Day(c171, @"aura lieu, au plus tard, le (\d+) (\p{L}+) (\d{4}) à 24 heures") == FrenchPresidentialReturns.FirstTermFromLatest && Day(c171, @"les \d+ et (\d+) (\p{L}+) (\d{4}), sont les suivants") == FrenchPresidentialReturns.FirstTermRunOff
                      && Day(c169, @"les \d+ et (\d+) (\p{L}+) (\d{4}), sont les suivants") == FrenchPresidentialReturns.FirstTermFirstRound,
                    F("(c) the first term: rounds {0:yyyy-MM-dd} and {1:yyyy-MM-dd}, in office by {2:yyyy-MM-dd} [CC-169] [CC-171]", FrenchPresidentialReturns.FirstTermFirstRound, FrenchPresidentialReturns.FirstTermRunOff, FrenchPresidentialReturns.FirstTermFromLatest));
                Check(Day(inv, @"Publié le (\d+) (\p{L}+) (\d{4}) Emmanuel Macron a été officiellement investi") == FrenchPresidentialReturns.Invested,
                    F("(c) invested {0:yyyy-MM-dd} [EL-INV]", FrenchPresidentialReturns.Invested));
                Match est = Regex.Match(ips, @"20h en annonçant une victoire d.{0,3}Emmanuel Macron avec (\d+),(\d)% des suffrages");
                Check(est.Success && Math.Abs(double.Parse(est.Groups[1].Value + "." + est.Groups[2].Value, CultureInfo.InvariantCulture) - FrenchPresidentialReturns.RunOffEstimate) < 1e-9 && FrenchPresidentialReturns.RunOffEstimateSurname == "MACRON",
                    F("(c) the run-off's 20:00 estimate {0} % for Macron, as Ipsos's own page states it [IPSOS-T2]; the first round's is BILLED (P4)", FrenchPresidentialReturns.RunOffEstimate));

                // (d) the rule
                string constitution = TextOf(Path.Combine(raw, "records", "cc_constitution_texte_integral.html"));
                TwoRoundElection.Rule fr = TwoRoundElection.RecordRuleOf(CountryId.France);
                bool art6 = constitution.Contains("Le Président de la République est élu pour cinq ans au suffrage universel direct. Nul ne peut exercer plus de deux mandats consécutifs.");
                bool art7 = constitution.Contains("il est procédé, le quatorzième jour suivant, à un second tour") && constitution.Contains("vingt jours au moins et trente-cinq jours au plus avant l'expiration des pouvoirs du président en exercice")
                            && constitution.Contains("le cas échéant après retrait de candidats plus favorisés");
                (DateTime ends, DateTime opens, DateTime closes) = fr != null ? TwoRoundElection.TermOf(fr, FrenchPresidentialReturns.FirstTermFromLatest) : default;
                Check(fr != null && art6 && art7 && fr.TermYears == 5 && fr.MaxTerms == 2 && fr.RunOffDay == 14 && fr.WithdrawalPostponementDays == 0 && fr.CalledEarliestDaysBefore == 35 && fr.CalledLatestDaysBefore == 20
                      && TwoRoundElection.RuleOf(CountryId.France) == null && ends == FrenchPresidentialReturns.SecondTermFrom && FrenchPresidentialReturns.FirstRound >= opens && FrenchPresidentialReturns.FirstRound <= closes
                      && FrenchPresidentialReturns.RunOff == FrenchPresidentialReturns.FirstRound.AddDays(fr.RunOffDay),
                    F("(d) France's rule is Art. 6-7 as the Conseil serves it (five years, two consecutive terms, the run-off on the 14th day, 35 to 20 days before the term ends) - the first term ends {0:yyyy-MM-dd}, its window {1:yyyy-MM-dd} to {2:yyyy-MM-dd} holds the first round; the game holds no French election (RuleOf null)", ends, opens, closes));
                string brief = TwoRoundElection.RecordBrief(CountryId.France, 2022);
                Check(brief != null && brief.Contains("Emmanuel Macron elected") && brief.Contains("2022-04-24"), F("(d) the record's brief: {0}", brief ?? "null"));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"FRENCH RECORD: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>The Interior's rows: ISO-8859-1 read byte for byte (each byte its own code point), ';'-separated, the header dropped.</summary>
        private static List<string[]> Rows(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) { chars[i] = (char)bytes[i]; }
            var rows = new List<string[]>();
            bool header = true;
            foreach (string line in new string(chars).Split('\n'))
            {
                string l = line.TrimEnd('\r');
                if (l.Length == 0) { continue; }
                if (header) { header = false; continue; }
                rows.Add(l.Split(';'));
            }
            return rows;
        }

        /// <summary>A page's text as the record files quote it: scripts and tags stripped, entities decoded, whitespace collapsed (the prep's rule).</summary>
        private static string TextOf(string path)
        {
            string s = Encoding.UTF8.GetString(File.ReadAllBytes(path));
            s = Regex.Replace(s, "<script.*?</script>", " ", RegexOptions.Singleline);
            s = Regex.Replace(s, "<style.*?</style>", " ", RegexOptions.Singleline);
            s = Regex.Replace(s, "<[^>]+>", " ");
            s = System.Net.WebUtility.HtmlDecode(s);
            s = s.Replace(' ', ' ').Replace(' ', ' ').Replace('’', '\'');
            return Regex.Replace(s, @"\s+", " ");
        }

        private static readonly string[] Months = { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre" };

        /// <summary>The first date the pattern finds (groups: day, French month, year); MinValue where none.</summary>
        private static DateTime Day(string text, string pattern)
        {
            Match m = Regex.Match(text, pattern);
            if (!m.Success) { return DateTime.MinValue; }
            int month = Array.IndexOf(Months, m.Groups[2].Value) + 1;
            if (month < 1) { return DateTime.MinValue; }
            return new DateTime(int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), month, int.Parse(m.Groups[1].Value.Replace("er", string.Empty), CultureInfo.InvariantCulture));
        }

        private static long L(string s) => long.Parse(Regex.Replace(s, @"\s", string.Empty), CultureInfo.InvariantCulture);

        private static void Add(Dictionary<string, long> d, string k, long v) => d[k] = d.TryGetValue(k, out long x) ? x + v : v;

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var hex = new StringBuilder();
                foreach (byte b in sha.ComputeHash(bytes)) { hex.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                return hex.ToString();
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
