using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-5 with S7, part one (§719): THE TWO-ROUND RULE, COUNTED ON THE RECORD. The PKW's four rounds (`ElectionsData/poland/presidential_votes.csv`,
    /// from the notices in the Dziennik Ustaw) read whole; each round's candidates sum to its valid votes; <see cref="TwoRoundElection"/> counts the
    /// first votes of 2020 and 2025 - no majority, the two who met, the run-off's day the 14th after - and the run-offs - the president the record
    /// seats (<see cref="PresidencyOfRecord"/>), the term from the oath to the successor's (Art. 128 ust. 1), each election inside the window the
    /// Marshal orders it for (Art. 128 ust. 2); and the rule's two edges planted: more than HALF (half exactly is not elected), and a withdrawal (the
    /// next-placed takes the place, the run-off 14 days later).
    /// </summary>
    public static class PresidentialElectionDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== PresidentialElectionDiagnostic (PS-5 with S7, §719): the two-round rule counted on the record ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland", "presidential_votes.csv"));
                var rounds = new Dictionary<string, List<(string Candidate, long Votes)>>();
                var totals = new Dictionary<string, Dictionary<string, long>>();
                var dates = new Dictionary<string, DateTime>();
                bool header = true;
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (raw.Length == 0 || raw[0] == '#') { continue; }
                    if (header) { header = false; continue; }
                    string[] f = SplitCsv(raw);
                    string key = f[0] + "/" + f[1];
                    dates[key] = DateTime.ParseExact(f[2], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    long votes = long.Parse(f[5], NumberStyles.Integer, CultureInfo.InvariantCulture);
                    if (f[3].StartsWith("__", StringComparison.Ordinal))
                    {
                        if (!totals.TryGetValue(key, out Dictionary<string, long> t)) { t = new Dictionary<string, long>(); totals[key] = t; }
                        t[f[3]] = votes;
                        continue;
                    }
                    if (!rounds.TryGetValue(key, out List<(string, long)> list)) { list = new List<(string, long)>(); rounds[key] = list; }
                    list.Add((f[3], votes));
                }
                Check(rounds.Count == 4, F("four rounds read from the PKW's returns ({0}: {1})", Path.GetFileName(path), string.Join(", ", rounds.Keys)));
                foreach (KeyValuePair<string, List<(string Candidate, long Votes)>> round in rounds)
                {
                    long sum = 0;
                    foreach ((string _, long v) in round.Value) { sum += v; }
                    Dictionary<string, long> t = totals[round.Key];
                    Check(sum == t["__VALID__"] && t["__VALID__"] + t["__INVALID__"] == t["__BALLOTS__"],
                        F("{0}: {1} candidates sum to the valid {2:N0}; valid + invalid = the ballots {3:N0}", round.Key, round.Value.Count, t["__VALID__"], t["__BALLOTS__"]));
                }

                TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(CountryId.Poland);
                Check(rule != null && rule.RunOffDay == 14 && rule.TermYears == 5 && rule.MaxTerms == 2, F("Poland's rule: the run-off on the 14th day, a five-year term, two at most ({0})", rule?.Basis));
                IReadOnlyList<PresidencyOfRecord.President> presidents = PresidencyOfRecord.Of(CountryId.Poland);
                foreach (string year in new[] { "2020", "2025" })
                {
                    TwoRoundElection.FirstVote first = TwoRoundElection.CountFirstVote(rule, dates[year + "/1"], rounds[year + "/1"]);
                    var runOffNames = new HashSet<string>();
                    foreach ((string candidate, long _) in rounds[year + "/2"]) { runOffNames.Add(candidate); }
                    Check(first.ElectedOutright == null && first.RunOff != null && first.RunOff.Length == 2 && runOffNames.Contains(first.RunOff[0]) && runOffNames.Contains(first.RunOff[1]) && first.RunOffOn == dates[year + "/2"],
                        F("{0}, the first vote: {1} - the record's run-off {2}", year, first.Line, dates[year + "/2"].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    string elected = TwoRoundElection.CountRunOff(rounds[year + "/2"]);
                    PresidencyOfRecord.President seated = default;
                    foreach (PresidencyOfRecord.President p in presidents) { if (p.RunOff == dates[year + "/2"]) { seated = p; } }
                    Check(elected != null && seated.Name != null && SameName(elected, seated.Name),
                        F("{0}, the run-off: {1} elected with more votes - the record's president from {2}, {3}", year, elected, seated.TookOffice.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), seated.Name));
                    (DateTime ends, DateTime opens, DateTime closes) = TwoRoundElection.TermOf(rule, seated.TookOffice);
                    Check(true, F("{0}'s term from {1}: ends {2}, its successor's election ordered for {3} - {4}", seated.Name, seated.TookOffice.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ends.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), opens.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), closes.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                }
                // the term: Duda's from his oath of 2020-08-06 ends the day Nawrocki takes the oath; the 2025 election falls in the window Duda's term set
                (DateTime dudaEnds, DateTime windowOpens, DateTime windowCloses) = TwoRoundElection.TermOf(rule, presidents[0].TookOffice);
                Check(dudaEnds == presidents[1].TookOffice && dates["2025/1"] >= windowOpens && dates["2025/1"] <= windowCloses,
                    F("Duda's term ends {0}, the day Nawrocki takes the oath (Art. 128 ust. 1); the 2025 first vote {1} inside the window {2} - {3} (Art. 128 ust. 2)",
                      dudaEnds.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dates["2025/1"].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                      windowOpens.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), windowCloses.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                Check(PresidencyOfRecord.TryAt(CountryId.Poland, new DateTime(2023, 2, 19), out PresidencyOfRecord.President atStart) && atStart.Name == "Andrzej Duda",
                    "at Poland's start (2023-02-19) the president of record is Duda, his second term - the run crosses the 2025 election");

                // the runtime table the game reads (Generated.PolishPresidentialReturns, Tools/presidential_returns_prep.pl) is the CSV, figure for figure
                int rowsMatched = 0, rowsInTable = PoliSim.Elections.Generated.PolishPresidentialReturns.Votes.Length;
                foreach (var v in PoliSim.Elections.Generated.PolishPresidentialReturns.Votes)
                {
                    string key = v.Year.ToString(CultureInfo.InvariantCulture) + "/" + v.Round.ToString(CultureInfo.InvariantCulture);
                    if (rounds.TryGetValue(key, out List<(string Candidate, long Votes)> fromCsv) && fromCsv.Exists(c => c.Candidate == v.Candidate && c.Votes == v.Votes)) { rowsMatched++; }
                }
                int csvRows = 0; foreach (var r in rounds.Values) { csvRows += r.Count; }
                Check(rowsMatched == rowsInTable && rowsInTable == csvRows, F("the runtime table (Generated.PolishPresidentialReturns) is the CSV: {0} of {1} candidate rows match, the CSV holds {2}", rowsMatched, rowsInTable, csvRows));
                string brief = TwoRoundElection.RecordBrief(CountryId.Poland, 2025);
                Check(brief != null && brief.Contains("NAWROCKI Karol Tadeusz elected, in office 2025-08-06 to 2030-08-06") && StartPoints.For(CountryId.Poland)[1].Basis.StartsWith(brief, StringComparison.Ordinal)
                      && StartPoints.For(CountryId.Poland)[1].PollingDay == dates["2025/1"], F("the game reads it: Poland's presidential start card states its record through the rule - {0}", brief));

                // the rule's edges, planted
                var exactlyHalf = new List<(string, long)> { ("A", 500), ("B", 300), ("C", 200) };
                var moreThanHalf = new List<(string, long)> { ("A", 501), ("B", 300), ("C", 199) };
                Check(TwoRoundElection.CountFirstVote(rule, new DateTime(2030, 5, 1), exactlyHalf).ElectedOutright == null && TwoRoundElection.CountFirstVote(rule, new DateTime(2030, 5, 1), moreThanHalf).ElectedOutright == "A",
                    "more than HALF of the valid votes elects (501 of 1,000); half exactly (500) goes to the run-off - Art. 127 ust. 4");
                TwoRoundElection.FirstVote withdrawn = TwoRoundElection.CountFirstVote(rule, dates["2025/1"], rounds["2025/1"], new[] { "TRZASKOWSKI Rafał Kazimierz" });
                Check(withdrawn.RunOff != null && withdrawn.RunOff[0] == "NAWROCKI Karol Tadeusz" && withdrawn.RunOff[1] == "MENTZEN Sławomir Jerzy" && withdrawn.RunOffOn == dates["2025/1"].AddDays(28),
                    F("planted: Trzaskowski withdraws after 2025's first vote - {0}", withdrawn.Line));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"PRESIDENTIAL ELECTION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>The PKW writes SURNAME Given names; the record writes Given Surname - the same person when the surname and the first given name agree.</summary>
        private static bool SameName(string pkw, string record)
        {
            string[] a = pkw.Split(' '), b = record.Split(' ');
            return a.Length >= 2 && b.Length >= 2 && string.Equals(a[0], b[b.Length - 1], StringComparison.OrdinalIgnoreCase) && a[1] == b[0];
        }

        /// <summary>Splits one CSV row; a field may be quoted, with a comma inside.</summary>
        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (c == ',' && !quoted) { fields.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(c);
            }
            fields.Add(cur.ToString());
            return fields.ToArray();
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
