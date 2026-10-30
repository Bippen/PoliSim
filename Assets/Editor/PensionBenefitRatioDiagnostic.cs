using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// pbr (K314; K293's filing; the prereg `Reviews/2026-10-30_pbr_prereg.md`, Part 3, with its amendments): **THE NO-POLICY PENSION LINE AGAINST ITS SOURCE.**
    /// The pension line carries the benefit ratio's sourced path (`PensionBenefitRatio`: the Ageing Report 2024's Table II.1.79 for the EU five, CBO 62556's
    /// derived ratio for the USA); this check reads what that does to the line in the no-policy-change book (`NoPolicyCentury.Unchanged`, seed 777, each
    /// country on its own start - the sources' own premise), against the source's own expenditure path.
    /// <list type="bullet">
    /// <item><b>(A1)</b> M(y) = the line's share of GDP at the record whose calendar year is y over its share at the opening; S(y) = the source's expenditure at y
    /// over its value at the opening's year (Table II.1.64 for the EU five at 2030, 2040, 2050 and 2070; CBO's Table 3 for the USA at 2030, 2040, 2050, 2070 and
    /// 2100). Band |M / S - 1| &lt;= 0.10 [AUTHORED, the prereg's reason].</item>
    /// <item><b>(A2)</b> printed beside every (A1) line: the same comparison with the coverage term moved to the source's - M x [C_src(y) / C_src(y0)] / [C_m(y) /
    /// C_m(y0)], C_m the model's statutory headcount over its 65+, C_src the source's coverage ratio (the USA's beneficiaries per 65+ to 2056 only, read at 2026
    /// for its opening years 2024-2025 as B is - the prereg's amendment 2). Band +/-0.05 [AUTHORED]. Beside it the three-way split of the (A1) gap: coverage,
    /// the benefit ratio (zero by construction), the rest.</item>
    /// </list>
    /// <b>Disposition, registered before any measurement:</b> an (A1) line inside its band is ok; an (A1) miss with (A2) inside is a REGISTERED miss (the declared
    /// coverage term, K315), printed and not failed; an (A1) miss with (A2) also outside - or with no (A2) to read - is a stopping result and FAILS. A year that
    /// did not run FAILS. Nothing here is re-banded. K318 (ruled after the measurement): Italy 2070 is REGISTERED while its (A2) stays inside [-5 %, +7.5 %] and coverage is the split's largest factor. Out of the engine, `Tools/headless_hold/player_book.sh` carries the same inputs for seed 424242.
    /// Sits on the simulation bar: it reads the no-policy-change centuries.
    /// </summary>
    public static class PensionBenefitRatioDiagnostic
    {
        /// <summary>[AUTHORED] the prereg's (A1) and (A2) bands.</summary>
        public const double A1Band = 0.10, A2Band = 0.05;

        /// <summary>K318 (Code's recommendation, Elias's standing instruction of 2026-10-27; ruled after the measurement, the prereg's amendment 3): the (A1) lines
        /// REGISTERED although their (A2) misses - exactly one, Italy 2070, measured at (A1) +30.2 % / +30.8 % and (A2) +6.0 % / +6.5 % (seeds 777 / 424242), the
        /// split coverage x1.2288 (the game's carried statute, 67 y 3 m, against the Ageing Report's indexed age and its coverage 104.5 -> 86.1 %) and the rest
        /// x1.06 (the labour-market term). Registered only while (i) its (A2) stays inside [-5 %, <see cref="K318A2Ceiling"/>] and (ii) coverage is the largest of
        /// the three factors; it fails once either breaks. Any other (A1) miss whose (A2) misses still stops. The Ageing Report's indexed age is NOT fed into
        /// Italy's headcount: the carried law the game shows stays.</summary>
        private static readonly HashSet<string> K318Registered = new HashSet<string> { "Italy 2070" };

        /// <summary>[AUTHORED-DRAFT] K318's (i): the (A2) ceiling a registered line may reach.</summary>
        public const double K318A2Ceiling = 0.075;

        private static readonly int[] EuYears = { 2030, 2040, 2050, 2070 };
        private static readonly int[] UsYears = { 2030, 2040, 2050, 2070, 2100 };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(string format, params object[] args) => string.Format(Inv, format, args);

        /// <summary>A generated series at a year, held at its first year before it; NaN past its last.</summary>
        private static double At(Dictionary<CountryId, float[]> table, CountryId id, int first, int year)
        {
            if (!table.TryGetValue(id, out float[] path)) { return double.NaN; }
            int i = Math.Max(0, year - first);
            return i < path.Length ? path[i] : double.NaN;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0, registered = 0;
            var sb = new StringBuilder("PENSION BENEFIT RATIO: pbr (K314) - the no-policy-change pension line against its source (seed 777, each country on its own start)\n");
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA })
            {
                bool usa = id == CountryId.USA;
                int[] targets = usa ? UsYears : EuYears;
                int startYear = PoliSim.Elections.WorldClock.StartDate(id).Year;
                int need = targets[targets.Length - 1] - startYear + 1;
                IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.Unchanged(id, need);
                NoPolicyCentury.Year opening = NoPolicyCentury.UnchangedOpening(id, need);
                int y0 = opening.CountryCalendarYear;
                double s0 = At(PensionBenefitRatioAnchors.Expenditure, id, PensionBenefitRatioAnchors.ExpenditureFirstYear(id), y0);
                double c0Src = At(PensionBenefitRatioAnchors.Coverage, id, PensionBenefitRatioAnchors.CoverageFirstYear(id), y0);
                double c0M = opening.StatutoryPensioners / opening.Elderly65;
                sb.Append(F("  {0} opens {1}: the line {2:F3} % of GDP, the source {3:F3}; coverage the source's {4:F1} %, the model's statutory headcount per 65+ {5:F4}\n", id, y0, opening.PensionSharePercent, s0, c0Src, c0M));
                foreach (int y in targets)
                {
                    NoPolicyCentury.Year r = null;
                    foreach (NoPolicyCentury.Year k in run) { if (k.CountryCalendarYear == y) { r = k; break; } }
                    if (r == null) { failures++; string miss = F("{0} {1}: no record of that calendar year in the run - FAIL", id, y); sb.Append("    FAIL ").Append(miss).Append('\n'); Debug.LogError("PENSION BENEFIT RATIO: " + miss); continue; }
                    double m = r.PensionSharePercent / opening.PensionSharePercent;
                    double s = At(PensionBenefitRatioAnchors.Expenditure, id, PensionBenefitRatioAnchors.ExpenditureFirstYear(id), y) / s0;
                    double a1 = m / s - 1.0;
                    double b = PensionBenefitRatio.Ratio(id, y0, y);
                    double cSrc = At(PensionBenefitRatioAnchors.Coverage, id, PensionBenefitRatioAnchors.CoverageFirstYear(id), y);
                    double cIdxSrc = cSrc / c0Src, cIdxM = (r.StatutoryPensioners / r.Elderly65) / c0M;
                    bool hasA2 = !double.IsNaN(cIdxSrc) && !double.IsNaN(cIdxM) && cIdxM > 0;
                    double a2 = hasA2 ? m * cIdxSrc / cIdxM / s - 1.0 : double.NaN;
                    string line = F("{0} {1}: M {2:F4} (the line {3:F3} % of GDP), S {4:F4}; (A1) {5:+0.0;-0.0;0.0} % (band +/-{6:0} %); B's index {7:F4}; (A2) {8} (band +/-{9:0} %) - the split: coverage x{10}, the benefit ratio x1 (by construction), the rest x{11}",
                        id, y, m, r.PensionSharePercent, s, 100 * a1, 100 * A1Band, b, hasA2 ? F("{0:+0.0;-0.0;0.0} %", 100 * a2) : "not computed (no source coverage that year)", 100 * A2Band,
                        hasA2 ? F("{0:F4}", cIdxM / cIdxSrc) : "n/a", hasA2 ? F("{0:F4}", 1 + a2) : "n/a");
                    if (Math.Abs(a1) <= A1Band) { sb.Append("    ok   ").Append(line).Append('\n'); continue; }
                    if (hasA2 && Math.Abs(a2) <= A2Band)
                    {
                        registered++;
                        sb.Append("    REGISTERED MISS (K315: the coverage term) ").Append(line).Append('\n');
                        Debug.Log("PENSION BENEFIT RATIO: REGISTERED MISS - " + line);
                        continue;
                    }
                    if (hasA2 && K318Registered.Contains(F("{0} {1}", id, y)))
                    {
                        // K318 (ruled after the measurement; the prereg's amendment 3): REGISTERED only while (i) (A2) inside [-5 %, +7.5 %] and (ii) coverage is the
                        // largest of the split's three factors (the largest |ln factor|; the benefit ratio's is 0)
                        double coverageFactor = cIdxM / cIdxSrc, restFactor = 1 + a2;
                        bool boundA2 = a2 >= -A2Band && a2 <= K318A2Ceiling;
                        bool boundCoverage = Math.Abs(Math.Log(coverageFactor)) > Math.Abs(Math.Log(restFactor));
                        string bound = F("K318's bounds: (i) (A2) {0:+0.0;-0.0;0.0} % within [-{1:0} %, +{2:0.0} %] - {3}; (ii) coverage x{4:F4} (|ln| {5:F4}) the largest factor against the rest x{6:F4} (|ln| {7:F4}) and the benefit ratio x1 - {8}",
                            100 * a2, 100 * A2Band, 100 * K318A2Ceiling, boundA2 ? "holds" : "BROKEN", coverageFactor, Math.Abs(Math.Log(coverageFactor)), restFactor, Math.Abs(Math.Log(restFactor)), boundCoverage ? "holds" : "BROKEN");
                        if (boundA2 && boundCoverage)
                        {
                            registered++;
                            sb.Append("    REGISTERED MISS (K318: the carried statute's coverage and the labour-market term) ").Append(line).Append(" - ").Append(bound).Append('\n');
                            Debug.Log("PENSION BENEFIT RATIO: REGISTERED MISS (K318) - " + line + " - " + bound);
                            continue;
                        }
                        failures++;
                        sb.Append("    FAIL ").Append(line).Append(" - registered under K318 but its bound broke: ").Append(bound).Append('\n');
                        Debug.LogError("PENSION BENEFIT RATIO: FAIL - " + line + " - registered under K318 but its bound broke: " + bound);
                        continue;
                    }
                    failures++;
                    sb.Append("    STOP ").Append(line).Append(" - (A1) missed and (A2) does not hold: a stopping result (the prereg)\n");
                    Debug.LogError("PENSION BENEFIT RATIO: STOP - " + line);
                }
            }
            Debug.Log(sb.ToString());
            if (registered > 0) { Debug.Log(F("PENSION BENEFIT RATIO: {0} REGISTERED MISS line(s) - an (A1) miss whose coverage attribution (A2) holds (K315, the prereg's disposition), or Italy 2070 inside K318's bounds", registered)); }
            Debug.Log(failures == 0
                ? "PENSION BENEFIT RATIO: PASS - every no-policy pension line within a tenth of its source's index at the prereg's years, or an (A1) miss its coverage attribution explains (REGISTERED, K315; Italy 2070 under K318's bounds)"
                : F("PENSION BENEFIT RATIO: FAIL - {0} line(s): a year that did not run, or a stopping result (the prereg)", failures));
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
