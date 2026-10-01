using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-5 with S7, PART TWO (§727): THE VOTE THAT FEEDS THE COUNT - the presidential vote model's candidates, measured on the record before any of
    /// them goes live. Part one (§720) counted the PKW's four rounds by the two-round rule; this asks where the votes it counts come from, in the two
    /// steps the model must take, each backtested on 2020 and 2025 with nothing fitted to either:
    ///
    /// <para><b>The first round - a candidate inherits the party that backs it</b>: the previous Sejm's share of the committee that runs the candidate
    /// (2020 from the 2019 Sejm, 2025 from the 2023 Sejm - the spec's own backtest), normalised over the candidates a committee runs; a candidate no
    /// committee runs (an independent, a splinter) has no share to inherit. The miss is the opinion that moved between the Sejm and the presidential
    /// vote and the candidates' own pull - the model's two unmodelled terms, measured here.</para>
    ///
    /// <para><b>The run-off - the eliminated candidates' voters transfer by proximity</b>: from the record's own first round (so the transfer step is
    /// measured alone), each eliminated candidate's share splits between the two finalists as exp(−d²/τ), τ the country's own choice temperature
    /// (<see cref="PartySystems.TryElectorate"/>), a candidate standing at its own party's CHES 2024 position; a candidate with no position splits as
    /// the finalists' first votes did (no preference is assumed for it). Two spaces are measured: <b>the plane the live vote model uses</b> (lrecon,
    /// galtan at the country's economic weight) and <b>the sovereignty space</b> (galtan, nationalism and the EU position, equally weighted - the
    /// dimensions a head of state's office turns on). Then the whole chain: the first round as modelled, then the run-off.</para>
    ///
    /// <para>Nothing is tuned. The numbers below are PINNED: a change to the positions, the returns or the rule moves them and this fails, so the
    /// reading Elias rules on is the reading the code still gives. Which space goes live is his (owed).</para>
    /// </summary>
    public static class PresidentialVoteBacktest
    {
        private sealed class Position { public double LrEcon, Galtan, Eu, Nationalism; }

        /// <summary>One candidate of a field: the PKW surname, the committee it inherits from in the previous Sejm (null: none), and the CHES 2024 party
        /// it stands at (null: unplaced), with the reason for each mapping - DERIVED, every one stated.</summary>
        private readonly struct Candidate
        {
            public readonly string Surname, Committee, ChesParty, Why;
            public Candidate(string surname, string committee, string chesParty, string why) { Surname = surname; Committee = committee; ChesParty = chesParty; Why = why; }
        }

        private static readonly Candidate[] Field2020 =
        {
            new Candidate("DUDA", "PiS", "PiS", "PiS's candidate; the incumbent"),
            new Candidate("TRZASKOWSKI", "KO", "PO", "KO's candidate (PO's mayor of Warsaw); KO's CHES row is PO's"),
            new Candidate("HOŁOWNIA", null, "Polska 2050", "no committee in 2019 (his movement became Polska 2050 in 2020) - placed at Polska 2050's 2024 row, an anachronism stated"),
            new Candidate("BOSAK", "Konf", "Konfederacja", "Konfederacja's candidate"),
            new Candidate("KOSINIAK-KAMYSZ", "PSL", "PSL", "PSL's leader and candidate"),
            new Candidate("BIEDROŃ", "SLD", "Nowa Lewica", "the Lewica candidate; SLD's 2019 committee, Nowa Lewica's row (SLD and Wiosna merged into it)"),
        };

        private static readonly Candidate[] Field2025 =
        {
            new Candidate("NAWROCKI", "PiS", "PiS", "the candidate PiS backed (a civic committee)"),
            new Candidate("TRZASKOWSKI", "KO", "PO", "KO's candidate"),
            new Candidate("MENTZEN", "Konf", "Konfederacja", "Konfederacja's candidate"),
            new Candidate("BRAUN", null, "Konfederacja", "his party ran on Konfederacja's 2023 list and left it in 2025 - no committee share to inherit, placed at Konfederacja's row"),
            new Candidate("HOŁOWNIA", "TD", "Polska 2050", "Trzecia Droga's candidate (Polska 2050's leader; PSL backed him) - TD's committee share, Polska 2050's row"),
            new Candidate("ZANDBERG", null, "Razem", "Razem ran inside Lewica's 2023 committee and left its club in 2024 - no share to inherit, Razem's row"),
            new Candidate("BIEJAT", "NL", "Nowa Lewica", "Lewica's candidate"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidentialVoteBacktest (PS-5 with S7, part two, §727): the vote that feeds the count, measured on 2020 and 2025 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                Dictionary<string, Position> ches = ReadChesPoland(Path.Combine(root, "ElectionsData", "positions", "raw", "CHES_2024_final_v2.csv"));
                Check(ches.Count == 7, F("CHES 2024 - Poland's seven rows read ({0}: {1})", ches.Count, string.Join(", ", ches.Keys)));
                Dictionary<(int, int), List<(string Name, long Votes)>> rounds = ReadRounds(Path.Combine(root, "ElectionsData", "poland", "presidential_votes.csv"), out Dictionary<(int, int), long> valid);
                Dictionary<string, double> sejm2019 = ReadSejm2019(Path.Combine(root, "ElectionsData", "poland", "raw", "records", "eli_DU_2019_1955_pkw_sejm2019.html"), out long valid2019);
                Dictionary<string, double> sejm2023 = ReadSejm2023(Path.Combine(root, "ElectionsData", "poland", "returns_2023.md"), out long valid2023);
                Check(valid2019 == 18470710L, F("Sejm 2019 - the notice's lists read, {0:N0} valid votes (Dz.U. 2019 poz. 1955, Dział I rozdz. 3)", valid2019));
                Check(valid2023 == 21596674L, F("Sejm 2023 - the committees read, {0:N0} valid votes (returns_2023.md, Dz.U. 2023 poz. 2234)", valid2023));
                if (!PartySystems.TryElectorate(CountryId.Poland, out VoteModel.Electorate electorate, out double wEcon)) { throw new InvalidOperationException("Poland has no electorate"); }
                double tau = electorate.Tau;
                sb.Append(F("    the transfer kernel's τ = {0:0.00} and economic weight {1:0.00} - Poland's own (PartySystems.TryElectorate), not chosen here\n", tau, wEcon));

                var pins = new List<(string What, double Got, double Want)>();
                foreach ((int year, Candidate[] field, Dictionary<string, double> sejm) in new[] { (2020, Field2020, sejm2019), (2025, Field2025, sejm2023) })
                {
                    List<(string Name, long Votes)> r1 = rounds[(year, 1)];
                    List<(string Name, long Votes)> r2 = rounds[(year, 2)];
                    double Share(List<(string Name, long Votes)> r, long total, string surname) => 100.0 * r.Where(c => Surname(c.Name) == surname).Sum(c => c.Votes) / total;

                    // ---- the first round: inheritance ----
                    sb.Append(F("\n  {0} - THE FIRST ROUND, inherited from the {1} Sejm (the committee that runs the candidate; none inherits nothing)\n", year, year == 2020 ? 2019 : 2023));
                    double inheritedSum = field.Where(c => c.Committee != null).Sum(c => sejm[c.Committee]);
                    double absSum = 0.0;
                    var model = new Dictionary<string, double>();
                    foreach (Candidate c in field)
                    {
                        double m = c.Committee != null ? 100.0 * sejm[c.Committee] / inheritedSum : 0.0;
                        double rec = Share(r1, valid[(year, 1)], c.Surname);
                        model[c.Surname] = m;
                        absSum += Math.Abs(m - rec);
                        sb.Append(F("    {0,-16} model {1,6:0.00}  record {2,6:0.00}  miss {3,7:+0.00;-0.00}   ({4})\n", c.Surname, m, rec, m - rec, c.Why));
                    }
                    double others = 100.0 - field.Sum(c => Share(r1, valid[(year, 1)], c.Surname));
                    sb.Append(F("    {0,-16} model {1,6:0.00}  record {2,6:0.00}  - the candidates no committee of the field ran\n", "(the rest)", 0.0, others));
                    double mae = absSum / field.Length;
                    sb.Append(F("    mean absolute miss over the field: {0:0.00} pp\n", mae));
                    pins.Add(($"{year} first round, the field's mean absolute miss", mae, year == 2020 ? 5.68 : 5.86));

                    // ---- the run-off: transfers from the record's own first round ----
                    string fa = year == 2020 ? "DUDA" : "NAWROCKI", fb = "TRZASKOWSKI";
                    double recordA = 100.0 * r2.Where(c => Surname(c.Name) == fa).Sum(c => c.Votes) / valid[(year, 2)];
                    var recordR1 = new List<(string Surname, double Share, Position At)>();
                    foreach (var g in r1.GroupBy(c => Surname(c.Name)))
                    {
                        Candidate known = field.FirstOrDefault(c => c.Surname == g.Key);
                        Position at = known.Surname != null && known.ChesParty != null ? ches[known.ChesParty] : null;
                        recordR1.Add((g.Key, 100.0 * g.Sum(c => c.Votes) / valid[(year, 1)], at));
                    }
                    Position pa = ches[field.First(c => c.Surname == fa).ChesParty], pb = ches[field.First(c => c.Surname == fb).ChesParty];
                    sb.Append(F("  {0} - THE RUN-OFF, transfers from the record's first round ({1} vs {2}; record {3:0.00} for {1})\n", year, fa, fb, recordA));
                    foreach ((string space, Func<Position, Position, double> d2) in new (string, Func<Position, Position, double>)[]
                    {
                        ("the vote model's plane (lrecon, galtan)", (p, q) => wEcon * Sq(p.LrEcon - q.LrEcon) + (1.0 - wEcon) * Sq(p.Galtan - q.Galtan)),
                        ("the sovereignty space (galtan, nationalism, EU)", (p, q) => (Sq(p.Galtan - q.Galtan) + Sq(p.Nationalism - q.Nationalism) + Sq(EuTen(p.Eu) - EuTen(q.Eu))) / 3.0),
                    })
                    {
                        double a = RunOff(recordR1, fa, fb, pa, pb, d2, tau);
                        sb.Append(F("    {0,-48} {1} {2:0.00}  miss {3:+0.00;-0.00}  {4}\n", space, fa, a, a - recordA, (a > 50.0) == (recordA > 50.0) ? "the winner of record" : "THE WRONG WINNER"));
                        pins.Add(($"{year} run-off from the record, {space}", a, Pin(year, space)));
                    }

                    // ---- the whole chain: the modelled first round, then the sovereignty run-off ----
                    var chainR1 = field.Select(c => (c.Surname, model[c.Surname], c.ChesParty != null ? ches[c.ChesParty] : null)).ToList();
                    string topA = chainR1.OrderByDescending(c => c.Item2).First().Surname;
                    string topB = chainR1.OrderByDescending(c => c.Item2).Skip(1).First().Surname;
                    Position ca = ches[field.First(c => c.Surname == topA).ChesParty], cb = ches[field.First(c => c.Surname == topB).ChesParty];
                    double chainA = RunOff(chainR1, topA, topB, ca, cb, (p, q) => (Sq(p.Galtan - q.Galtan) + Sq(p.Nationalism - q.Nationalism) + Sq(EuTen(p.Eu) - EuTen(q.Eu))) / 3.0, tau);
                    bool chainRight = (chainA > 50.0 ? topA : topB) == fa;
                    sb.Append(F("    the whole chain (modelled first round, then the sovereignty run-off): {0} vs {1} - {0} {2:0.00}  {3}\n", topA, topB, chainA,
                        chainRight ? "the winner of record" : "THE WRONG WINNER - the first round's inheritance carries the Sejm's opinion, not the presidential year's"));
                    pins.Add(($"{year} the whole chain, {topA}", chainA, year == 2020 ? 55.61 : 45.16));
                }

                sb.Append("\n  PINS (the reading above, held - a change to the positions, the returns or the rule moves them):\n");
                foreach ((string what, double got, double want) in pins) { Check(Math.Abs(got - want) < 0.005, F("{0}: {1:0.00} (pinned {2:0.00})", what, got, want)); }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== PresidentialVoteBacktest: the reading holds ===" : F("=== PresidentialVoteBacktest: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double Pin(int year, string space) => space.StartsWith("the vote model's", StringComparison.Ordinal)
            ? (year == 2020 ? 50.10 : 40.13)
            : (year == 2020 ? 52.89 : 52.81);

        /// <summary>The run-off: the finalists keep their first votes; an eliminated candidate's voters split by exp(−d²/τ) toward each finalist; one
        /// with no position splits as the finalists' first votes did. Returns the first finalist's share of the two.</summary>
        private static double RunOff(List<(string Surname, double Share, Position At)> r1, string fa, string fb, Position pa, Position pb, Func<Position, Position, double> d2, double tau)
        {
            double a = r1.Where(c => c.Surname == fa).Sum(c => c.Share), b = r1.Where(c => c.Surname == fb).Sum(c => c.Share);
            double a0 = a, b0 = b;
            foreach ((string surname, double share, Position at) in r1)
            {
                if (surname == fa || surname == fb) { continue; }
                if (at == null) { a += share * a0 / (a0 + b0); b += share * b0 / (a0 + b0); continue; }
                double ea = Math.Exp(-d2(at, pa) / tau), eb = Math.Exp(-d2(at, pb) / tau);
                a += share * ea / (ea + eb);
                b += share * eb / (ea + eb);
            }
            return 100.0 * a / (a + b);
        }

        private static double Sq(double x) => x * x;
        /// <summary>CHES's EU position runs 1 (strongly against) to 7 (strongly for); on the 0-10 scale the other two axes use.</summary>
        private static double EuTen(double eu) => (eu - 1.0) / 6.0 * 10.0;
        private static string Surname(string pkwName) { int space = pkwName.IndexOf(' '); return space > 0 ? pkwName.Substring(0, space) : pkwName; }

        private static Dictionary<string, Position> ReadChesPoland(string path)
        {
            var result = new Dictionary<string, Position>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] head = lines[0].Split(',');
            int Col(string name) => Array.IndexOf(head, name);
            int country = Col("country"), party = Col("party"), lrecon = Col("lrecon"), galtan = Col("galtan"), eu = Col("eu_position"), nat = Col("nationalism");
            foreach (string line in lines.Skip(1))
            {
                string[] f = line.Split(',');
                if (f.Length <= nat || f[country] != "26") { continue; }
                result[f[party]] = new Position
                {
                    LrEcon = double.Parse(f[lrecon], CultureInfo.InvariantCulture),
                    Galtan = double.Parse(f[galtan], CultureInfo.InvariantCulture),
                    Eu = double.Parse(f[eu], CultureInfo.InvariantCulture),
                    Nationalism = double.Parse(f[nat], CultureInfo.InvariantCulture),
                };
            }
            return result;
        }

        private static Dictionary<(int, int), List<(string Name, long Votes)>> ReadRounds(string path, out Dictionary<(int, int), long> valid)
        {
            var rounds = new Dictionary<(int, int), List<(string Name, long Votes)>>();
            valid = new Dictionary<(int, int), long>();
            bool header = true;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (raw.Length == 0 || raw[0] == '#') { continue; }
                if (header) { header = false; continue; }
                string[] f = raw.Split(',');
                var key = (int.Parse(f[0], CultureInfo.InvariantCulture), int.Parse(f[1], CultureInfo.InvariantCulture));
                long votes = long.Parse(f[f.Length - 1], CultureInfo.InvariantCulture);
                if (f[3] == "__VALID__") { valid[key] = votes; continue; }
                if (f[3].StartsWith("__", StringComparison.Ordinal)) { continue; }
                if (!rounds.TryGetValue(key, out List<(string Name, long Votes)> list)) { rounds[key] = list = new List<(string Name, long Votes)>(); }
                list.Add((f[3], votes));
            }
            return rounds;
        }

        /// <summary>The 2019 Sejm's national votes per list, read off the PKW's notice as saved (Dział I, rozdział 3, pkt 1): "na listy nr N zgłoszone
        /// przez KOMITET ... oddano V głosów". Keyed as the 2019 committees' own (PartySystems' Poland2019 keys).</summary>
        private static Dictionary<string, double> ReadSejm2019(string path, out long validTotal)
        {
            string text = Regex.Replace(Regex.Replace(File.ReadAllText(path, Encoding.UTF8).Replace("&nbsp;", " "), "<[^>]+>", " "), @"\s+", " ");
            var keys = new Dictionary<string, string>
            {
                { "KOMITET WYBORCZY PRAWO I SPRAWIEDLIWOŚĆ", "PiS" }, { "KOALICYJNY KOMITET WYBORCZY KOALICJA OBYWATELSKA PO .N IPL ZIELONI", "KO" },
                { "KOMITET WYBORCZY SOJUSZ LEWICY DEMOKRATYCZNEJ", "SLD" }, { "KOMITET WYBORCZY POLSKIE STRONNICTWO LUDOWE", "PSL" },
                { "KOMITET WYBORCZY KONFEDERACJA WOLNOŚĆ I NIEPODLEGŁOŚĆ", "Konf" },
            };
            var votes = new Dictionary<string, double>();
            foreach (KeyValuePair<string, string> k in keys)
            {
                Match m = Regex.Match(text, Regex.Escape(k.Key) + @" oddano ([\d ]+) głos");
                if (!m.Success) { throw new InvalidOperationException("2019 notice: no line for " + k.Key); }
                votes[k.Value] = long.Parse(m.Groups[1].Value.Replace(" ", string.Empty), CultureInfo.InvariantCulture);
            }
            Match total = Regex.Match(text, @"Liczba głosów ważnych wyniosła ([\d ]+),");
            validTotal = long.Parse(total.Groups[1].Value.Replace(" ", string.Empty), CultureInfo.InvariantCulture);
            long t = validTotal;
            return votes.ToDictionary(kv => kv.Key, kv => kv.Value / t);
        }

        /// <summary>The 2023 Sejm's national votes per committee, from returns_2023.md's sourced line ("Votes (valid): PiS 7,640,854; ...").</summary>
        private static Dictionary<string, double> ReadSejm2023(string path, out long validTotal)
        {
            string line = File.ReadAllLines(path, Encoding.UTF8).First(l => l.StartsWith("Votes (valid):", StringComparison.Ordinal));
            var votes = new Dictionary<string, double>();
            foreach (Match m in Regex.Matches(line, @"(PiS|KO|TD|NL|Konf|BS|MN) ([\d,]+)")) { votes[m.Groups[1].Value] = long.Parse(m.Groups[2].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture); }
            validTotal = long.Parse(Regex.Match(line, @"Valid votes total ([\d,]+)").Groups[1].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture);
            long t = validTotal;
            return votes.ToDictionary(kv => kv.Key, kv => kv.Value / t);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
