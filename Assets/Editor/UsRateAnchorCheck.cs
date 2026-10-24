using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K72 (Elias's ruling of 2026-10-23: "q16's dial-to-rate scale is sourced (party platforms or roll calls), US only"): <b>the scale q16 votes on is the
    /// sourced one</b>. Re-reads every file `Tools/us_rate_anchors_prep.pl` read (`ElectionsData/usa/raw/q16_scale/`, each against the digest the generated part
    /// carries) and the CSV it wrote (`ElectionsData/usa/q16_rate_anchors.csv`, its digest over its text with LF line ends, its rows the generated part's), and
    /// finds every anchor again where the Code and the roll call print it (26 U.S.C. 1's top brackets before and in 1(j), 11(b)'s corporate rate, House roll 699
    /// of 2017 splitting the parties whole; the platforms are held by their digests - their text is the tool's, read by pdftotext). Then on a fresh US country
    /// (`WorldFactory.CreateDefault`) it holds the scale to its definition: on each anchored line each party's `redistribution` position stands for its own
    /// sourced rate (`SpatialBudgetVote.RateAt` with the country) - the line's seeded rate x the party's rate / the rate in force - and `ScaleAt` is its inverse;
    /// an unanchored line keeps the dial's map; another country keeps the dial's map. Printed, not judged: each party's rate on each line and where the dial's map
    /// had put it (the figures K72 moves). THE TEETH: the dial's map planted for a party gives another rate. Regenerate a stale constant, never retype it:
    /// `perl Tools/us_rate_anchors_prep.pl`. Prints `US RATE ANCHORS: PASS` and exits 0 when every check holds.
    /// </summary>
    public static class UsRateAnchorCheck
    {
        private const string CsvRelative = "ElectionsData/usa/q16_rate_anchors.csv";
        private const string Header = "tax,dem,rep,in_force,dem_by,rep_by";

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        private static string Sha(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2"))); }
        }

        private static string Words(string html) => Regex.Replace(Regex.Replace(Regex.Replace(html, "<[^>]*>", " "), "&nbsp;|&#160;", " "), @"\s+", " ");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder("=== UsRateAnchorCheck (K72): q16's dial-to-rate scale from the platforms and the roll calls ===\n");
            void Check(bool ok, string what)
            {
                sb.Append(ok ? "    ok   " : "    FAIL ").Append(what).Append('\n');
                if (!ok) { failures++; Debug.LogError("US RATE ANCHORS: FAIL - " + what); }
            }
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string usa = Path.Combine(root, "ElectionsData", "usa");
                var stale = new List<string>();
                var text = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach ((string path, string sha) in UsRateAnchors.RawSources)
                {
                    string full = Path.Combine(usa, path.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full)) { stale.Add(path + " is not on disk"); continue; }
                    byte[] bytes = File.ReadAllBytes(full);
                    if (Sha(bytes) != sha) { stale.Add(path + " changed since generation"); }
                    text[Path.GetFileName(path)] = Encoding.UTF8.GetString(bytes);
                }
                Check(stale.Count == 0 && UsRateAnchors.RawSources.Length > 0, F("the {0} saved files are the bytes the run read{1}", UsRateAnchors.RawSources.Length, stale.Count == 0 ? string.Empty : " - " + string.Join("; ", stale)));

                string csv = File.ReadAllText(Path.Combine(root, CsvRelative.Replace('/', Path.DirectorySeparatorChar)), Encoding.UTF8).Replace("\r\n", "\n");
                Check(Sha(Encoding.UTF8.GetBytes(csv)) == UsRateAnchors.SourceDigest, F("{0} (its text with LF line ends) is the digest the generated part carries", CsvRelative));
                List<string[]> rows = csv.Split('\n').Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)).Select(l => l.Split(',')).ToList();
                bool headed = rows.Count > 0 && string.Join(",", rows[0]) == Header;
                if (headed) { rows.RemoveAt(0); }
                Check(headed && rows.Count == UsRateAnchors.Lines.Length && rows.Zip(UsRateAnchors.Lines, (c, g) => c.Length == 6 && c[0] == g.Tax && double.Parse(c[1], CultureInfo.InvariantCulture) == g.Dem
                        && double.Parse(c[2], CultureInfo.InvariantCulture) == g.Rep && double.Parse(c[3], CultureInfo.InvariantCulture) == g.InForce && c[4] == g.DemBy && c[5] == g.RepBy).All(x => x),
                    F("the CSV's {0} rows are the generated part's, field for field", rows.Count));

                // the anchors where the Code and the roll call print them
                string sec1 = text.TryGetValue("govinfo_USCODE-2023-title26-sec1.htm", out string s1) ? Words(s1) : string.Empty;
                string sec11 = text.TryGetValue("govinfo_USCODE-2023-title26-sec11.htm", out string s11) ? Words(s11) : string.Empty;
                string roll = text.TryGetValue("clerk_house_roll699_2017.xml", out string r) ? r : string.Empty;
                var income = UsRateAnchors.Lines.FirstOrDefault(l => l.Tax == "IncomeTax");
                var corporate = UsRateAnchors.Lines.FirstOrDefault(l => l.Tax == "CorporateTax");
                int jAt = sec1.IndexOf("(j) Modifications for taxable years 2018 through 2025", StringComparison.Ordinal), fAt = sec1.IndexOf("(f) Phaseout of marriage penalty", StringComparison.Ordinal);
                string Pct(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
                Check(fAt > 0 && jAt > fAt && sec1.Substring(0, fAt).Contains("plus " + Pct(income.Dem) + "% of the excess over") && sec1.Substring(jAt).Contains("plus " + Pct(income.Rep) + "% of the excess over")
                      && !sec1.Substring(jAt, Math.Max(0, sec1.IndexOf("Editorial Notes", jAt, StringComparison.Ordinal) - jAt)).Contains("plus " + Pct(income.Dem) + "% of the excess over"),
                    F("26 U.S.C. 1: the top bracket before the TCJA {0} % (subsections (a)-(e)) and in 1(j) {1} % (taxable years 2018 through 2025) - the income anchors, DEM {0}, REP and in force {1}", Pct(income.Dem), Pct(income.Rep)));
                Check(sec11.Contains("shall be " + Pct(corporate.Rep) + " percent of taxable income") && corporate.InForce == corporate.Rep && corporate.Dem > corporate.Rep,
                    F("26 U.S.C. 11(b): {0} percent - the corporate anchor REP and in force; DEM's {1} % the platform's (its text the tool's, the PDF held by its digest)", Pct(corporate.Rep), Pct(corporate.Dem)));
                Match rep = Regex.Match(roll, @"<party>Republican</party>\s*<yea-total>(\d+)</yea-total>\s*<nay-total>(\d+)</nay-total>"), dem = Regex.Match(roll, @"<party>Democratic</party>\s*<yea-total>(\d+)</yea-total>\s*<nay-total>(\d+)</nay-total>");
                Check(roll.Contains("<legis-num>H R 1</legis-num>") && roll.Contains("<vote-desc>Tax Cuts and Jobs Act</vote-desc>") && rep.Success && dem.Success && dem.Groups[1].Value == "0"
                      && int.Parse(rep.Groups[1].Value, CultureInfo.InvariantCulture) > int.Parse(rep.Groups[2].Value, CultureInfo.InvariantCulture),
                    F("House roll 699 of 2017 (H R 1, the Tax Cuts and Jobs Act, on concurring): Republicans {0}-{1}, Democrats {2}-{3} - the parties split whole", rep.Groups[1].Value, rep.Groups[2].Value, dem.Groups[1].Value, dem.Groups[2].Value));

                // the scale on a fresh US country
                Country us = WorldFactory.CreateDefault().GetCountry(CountryId.USA);
                foreach (var a in UsRateAnchors.Lines)
                {
                    TaxLine line = us.TaxLines.FirstOrDefault(t => t.Type.ToString() == a.Tax);
                    bool scaled = SpatialBudgetVote.TryUsScale(us, line, out float demAt, out float demRate, out float repAt, out float repRate);
                    float seed = line != null && us.BaselineTaxRates.TryGetValue(line.Type, out float sr) ? sr : float.NaN;
                    bool exact = scaled && Mathf.Abs(SpatialBudgetVote.RateAt(us, line, demAt) - demRate) < 1e-3f && Mathf.Abs(SpatialBudgetVote.RateAt(us, line, repAt) - repRate) < 1e-3f
                                 && Mathf.Abs(demRate - (float)(seed * a.Dem / a.InForce)) < 1e-4f && Mathf.Abs(repRate - (float)(seed * a.Rep / a.InForce)) < 1e-4f
                                 && Mathf.Abs(SpatialBudgetVote.ScaleAt(us, line, demRate) - demAt) < 1e-3f && Mathf.Abs(SpatialBudgetVote.ScaleAt(us, line, repRate) - repAt) < 1e-3f;
                    Check(exact, F("{0}: the seeded {1:0.###} %; DEM at {2:0.00} stands for {3:0.###} % (the dial's map put it at {4:0.###} %), REP at {5:0.00} for {6:0.###} % (the dial's {7:0.###} %) - each its sourced rate x the seed / {8:0.###}",
                        a.Tax, seed, demAt, demRate, line == null ? float.NaN : SpatialBudgetVote.RateAt(line, demAt), repAt, repRate, line == null ? float.NaN : SpatialBudgetVote.RateAt(line, repAt), a.InForce));
                    // THE TEETH: the dial's map planted for the party gives another rate
                    Check(line != null && Mathf.Abs(SpatialBudgetVote.RateAt(line, demAt) - demRate) > 1e-3f, F("planted: the dial's map at DEM's position gives {0:0.###} %, not the sourced {1:0.###} % - the assertion bites", line == null ? float.NaN : SpatialBudgetVote.RateAt(line, demAt), demRate));
                }
                TaxLine payroll = us.TaxLines.FirstOrDefault(t => t.Type == TaxType.PayrollTax);
                Country se = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);
                TaxLine seIncome = se.TaxLines.FirstOrDefault(t => t.Type == TaxType.IncomeTax);
                Check(payroll != null && !SpatialBudgetVote.TryUsScale(us, payroll, out _, out _, out _, out _) && SpatialBudgetVote.ScaleAt(us, payroll, payroll.Rate) == SpatialBudgetVote.ScaleAt(payroll, payroll.Rate)
                      && seIncome != null && !SpatialBudgetVote.TryUsScale(se, seIncome, out _, out _, out _, out _) && SpatialBudgetVote.RateAt(se, seIncome, 5f) == SpatialBudgetVote.RateAt(seIncome, 5f),
                    "an unanchored US line (the payroll tax) and another country's line (Sweden's income tax) keep the dial's map");
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message); }
            Debug.Log(sb.ToString());
            Debug.Log(failures == 0 ? "US RATE ANCHORS: PASS - q16's scale is the sourced one on the anchored lines, found again in the Code and the roll call." : F("US RATE ANCHORS: FAIL - {0} check(s) failed.", failures));
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
