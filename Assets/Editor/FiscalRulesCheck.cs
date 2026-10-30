using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §758 (Elias's rulings A6 and B6): THE FISCAL RULES' INK AND NOTICES, held. A6 (installed by §725): debt and the balance stay neutral ink unless they
    /// breach the country's statutory rule - the EU's 3 % and 60 % for the five Member States, the USA never (a dollar ceiling, not a ratio). B6: Sweden's
    /// 35 % debt anchor is a NOTICE, not a breach - more than 5 points of GDP from it, up or down, the government explains why to the Riksdag with the
    /// spring bill (skr. 2025/26:76) - the ink untouched. The constants against the quotes held in `ElectionsData/rules/sweden_fiscal_framework.md`.
    /// S10 (Design's return of 2026-10-30, 10b): the debt chart's framework line - its gutter label's two lines read off the rule table, the USA's stamp where
    /// no line is drawn, and each country's slip (Design's fragments under K320's caps, its marked term's level 2).
    /// </summary>
    public static class FiscalRulesCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FiscalRulesCheck (§758, rulings A6 and B6): the statutory rules' ink, Sweden's anchor a notice ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // A6: the EU's two reference values for the five, nothing for the USA
                Check(FiscalRules.Breaches(CountryId.Germany, FiscalRules.Measure.Deficit, 3.1f, out string deficitRule) && deficitRule.Contains("3 %")
                      && !FiscalRules.Breaches(CountryId.Germany, FiscalRules.Measure.Deficit, 3.0f, out _),
                    "A6: a deficit above 3 % of GDP breaches the EU's reference value; 3.0 does not");
                Check(FiscalRules.Breaches(CountryId.Italy, FiscalRules.Measure.Debt, 60.5f, out _) && !FiscalRules.Breaches(CountryId.Italy, FiscalRules.Measure.Debt, 60f, out _),
                    "A6: debt above 60 % of GDP breaches; 60.0 does not");
                Check(!FiscalRules.Breaches(CountryId.USA, FiscalRules.Measure.Debt, 120f, out _) && !FiscalRules.Breaches(CountryId.USA, FiscalRules.Measure.Deficit, 7f, out _),
                    "A6: the USA never breaches - its limit is a dollar ceiling on a different measure");

                // B6: Sweden's anchor - a notice either side of the band, never a breach
                string above = FiscalRules.Notice(CountryId.Sweden, FiscalRules.Measure.Debt, 40.5f);
                string below = FiscalRules.Notice(CountryId.Sweden, FiscalRules.Measure.Debt, 29.5f);
                Check(above != null && above.Contains("above") && below != null && below.Contains("below") && above.Contains("skr. 2025/26:76"),
                    F("B6: Sweden's debt at 40.5 % and 29.5 % - a notice each way: \"{0}\"", above));
                Check(FiscalRules.Notice(CountryId.Sweden, FiscalRules.Measure.Debt, 40f) == null && FiscalRules.Notice(CountryId.Sweden, FiscalRules.Measure.Debt, 30f) == null
                      && FiscalRules.Notice(CountryId.Sweden, FiscalRules.Measure.Deficit, 4f) == null && FiscalRules.Notice(CountryId.Germany, FiscalRules.Measure.Debt, 70f) == null,
                    "B6: no notice within 5 points (40.0, 30.0), none on the balance, none for another country");
                Check(!FiscalRules.Breaches(CountryId.Sweden, FiscalRules.Measure.Debt, 45f, out _),
                    "B6: the notice is not a breach - Sweden's debt at 45 % keeps the neutral ink (the EU's 60 % not breached)");
                Check(FiscalRules.NationalNote(CountryId.Sweden).Contains("from 2027") && FiscalRules.NationalNote(CountryId.Sweden).Contains("rskr. 2025/26:64"),
                    "B6: Sweden's national note dates the balance target - 0 % from 2027 (rskr. 2025/26:64)");

                // the constants against the held quotes
                string md = File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "rules", "sweden_fiscal_framework.md")), Encoding.UTF8);
                Check(md.Contains("Skuldankaret är satt till 35") && FiscalRules.SwedenDebtAnchorPercent == 35f, "the anchor 35 % - \"Skuldankaret är satt till 35 procent av BNP\" (skr. 2025/26:76)");
                Check(md.Contains("med mer än 5 procent av BNP") && FiscalRules.SwedenDebtAnchorBandPoints == 5f, "the band 5 points - \"avviker ... med mer än 5 procent av BNP\" (skr. 2025/26:76)");

                // S10 (Design's return of 2026-10-30, 10b): THE DEBT CHART'S FRAMEWORK LINE - the label's two lines read off the rule table, the USA's stamp, the slips
                Check(FiscalRules.DebtReferenceLabelLines(CountryId.Sweden, out string sf, out string sw) && sf == "35 %" && sw == "anchor" && FiscalRules.DebtReferenceLabel(CountryId.Sweden) == "35 % anchor"
                      && FiscalRules.NoDebtRatioStamp(CountryId.Sweden) == null,
                    F("10b: Sweden's label reads '{0}' / '{1}' - the anchor's figure and its word, no stamp", sf, sw));
                bool treaty = true;
                foreach (CountryId c in new[] { CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland })
                {
                    treaty &= FiscalRules.DebtReferenceLabelLines(c, out string tf, out string tw) && tf == "60 %" && tw == "Treaty" && FiscalRules.NoDebtRatioStamp(c) == null;
                }
                Check(treaty, "10b: Germany, France, Italy and Poland read 60 % / Treaty - the Treaty's reference value - with no stamp");
                Check(!FiscalRules.DebtReferenceLabelLines(CountryId.USA, out _, out _) && FiscalRules.DebtReferenceLabel(CountryId.USA) == null && FiscalRules.NoDebtRatioStamp(CountryId.USA) == "NO RATIO IN LAW",
                    "10b: the USA draws no line and carries the NO RATIO IN LAW stamp");
                var slipFaults = new List<string>();
                foreach (CountryId c in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    List<string> lines = FiscalRules.DebtReferenceLines(c, out string head);
                    var terms = FiscalRules.DebtReferenceTerms(c);
                    if (string.IsNullOrEmpty(head) || lines.Count == 0 || lines.Count > 4) { slipFaults.Add(F("{0}: {1} line(s) under '{2}'", c, lines.Count, head)); }
                    foreach (string l in lines) { if (CountedWords(l) > 10) { slipFaults.Add(F("{0}: '{1}' holds {2} counted words", c, l, CountedWords(l))); } }   // K320 (3), K321 (f)
                    var marked = new List<string>();
                    foreach (string l in lines) { marked.AddRange(SlipContent.TermsIn(l)); }
                    if ((c == CountryId.USA) != (terms.Count == 0) || marked.Count != terms.Count) { slipFaults.Add(F("{0}: {1} term(s) marked, {2} level-2 slip(s)", c, marked.Count, terms.Count)); }
                    foreach ((string term, string definition) in terms)
                    {
                        if (!marked.Contains(term) || string.IsNullOrEmpty(definition) || System.Text.RegularExpressions.Regex.IsMatch(definition, @"[.!?;]\s+[A-Z]") || !definition.EndsWith(".", StringComparison.Ordinal))
                        {
                            slipFaults.Add(F("{0}: the term '{1}' - marked {2}, its level 2 one sentence: '{3}'", c, term, marked.Contains(term), definition));   // K320 (3): level 2 one plain sentence
                        }
                    }
                }
                List<string> se = FiscalRules.DebtReferenceLines(CountryId.Sweden, out _), eu = FiscalRules.DebtReferenceLines(CountryId.Italy, out _), us = FiscalRules.DebtReferenceLines(CountryId.USA, out _);
                if (!(se[0] == "[[Skuldankaret]] · 35 % of GDP" && eu[0] == "60 % of GDP · the [[reference value]]" && eu.Count > 2 && eu[2] == "Germany, France, Italy, Poland" && us[0] == "No debt ratio in law"))
                {
                    slipFaults.Add(F("the fragments read '{0}', '{1}', '{2}', '{3}'", se[0], eu[0], eu.Count > 2 ? eu[2] : "-", us[0]));
                }
                Check(slipFaults.Count == 0, "10b: each country's slip is Design's fragments (the figure read off the rule table), at most four lines of at most ten counted words, every marked term's level 2 one sentence (none for the USA)"
                    + (slipFaults.Count > 0 ? " - " + string.Join("; ", slipFaults) : string.Empty));
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>K321 (b)'s count on a slip line as it prints (the term's marks removed): a word that holds a letter.</summary>
        private static int CountedWords(string line)
        {
            int n = 0;
            foreach (string token in SlipContent.Plain(line).Split(' ')) { foreach (char ch in token) { if (char.IsLetter(ch)) { n++; break; } } }
            return n;
        }
    }
}
