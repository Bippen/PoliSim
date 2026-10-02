using System;
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
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
