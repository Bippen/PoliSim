using System;
using System.Collections.Generic;
using System.Globalization;

namespace PoliSim.Data
{
    /// <summary>The shape a country's income-tax statute has (`POLISIM_TAX_SPECLET.md` D-3 (b), ruled 2026-08-31; the plan's S3a, §474).</summary>
    public enum TaxScheduleKind
    {
        /// <summary>No schedule: the line is the flat rate it always was, and the response ratio is exactly one.</summary>
        Flat,
        /// <summary>A bracket table - a marginal rate above each threshold (Poland's skala, the USA's § 1(j)(2) table with the standard deduction folded in as the exempt band).</summary>
        BracketTable,
        /// <summary>A formula in the statute (Germany's § 32a EStG: two quadratic ramps, then two linear zones) - read as bands whose marginal rate RAMPS from a start to an end rate, the ends derived from the coefficients.</summary>
        Formula,
        /// <summary>A barème applied per PART of the quotient familial (France): the table on income per part, the tax times the parts.</summary>
        QuotientBareme,
        /// <summary>Two layers on one line (Sweden, DS-2d ruled): a flat municipal rate on every krona and a state bracket above the skiktgräns.</summary>
        TwoLayer,
        /// <summary>Three layers billed (Italy, E-6): the statute is not on disk with a text layer, so the line stays flat and says so.</summary>
        ThreeLayerBilled,
    }

    /// <summary>What a sub-row of the schedule row is (board 15b): the exempt band, a band with one rate, a ramp to an end rate, the flat layer on every unit, a levy under another act.</summary>
    public enum TaxSubRowKind { Exempt, Band, Ramp, FlatLayer, Levy }

    /// <summary>
    /// F4-2 (2026-09-13, the plan's S3a; D-3 (b) ruled 2026-08-31, DS-1 · DS-2c · DS-2d ruled §474): **the income tax as its statute's own
    /// shape, pluggable, over the cohort substrate's income dimension** - the six shapes read verbatim from the sourced tariffs
    /// (`ElectionsData/tax/README.md`, `POLISIM_TAX_SPECLET.md` §2), applied to each cohort's log-normal income (F4-1, `CohortIncomeSeeds`),
    /// and folded into the revenue engine as ONE ratio.
    ///
    /// <para><b>The three requirements Elias set from the abandoned patch's findings (2026-09-13), carried here as rules, not code.</b>
    /// (1) <b>The statute's own currency.</b> The cohort incomes are the publisher's - EUR for the EU five (EU-SILC), USD for the USA - and the
    /// statutes are written in SEK and PLN for two of them; a schedule that read a Swedish income of 23 833 as kronor put every earner under
    /// the first threshold and stood built while inert. Every income is converted into the statute's currency at the catalog's ECB reference
    /// rates (`EnergyLayer.NationalPerUsd`) before a threshold is compared. (2) <b>A uniform shift leaves an exempt band exempt.</b> The one
    /// dial (DS-2c) shifts every RATE by the same points - a 0 % band is not a rate, it is an exemption, and shifting Sweden's 0 % state band
    /// multiplied the yield 4.28×; here the shift touches only bands whose statutory rate is above zero. (3) <b>The response is a RATIO against
    /// the schedule's own yield at the seed.</b> The revenue engine keeps its anchored figure - the seeded rate times the sourced base
    /// (`TaxBases`, D-16) - and the schedule enters as `YieldRatio`: the headcount-weighted average effective rate the schedule yields now,
    /// over the seed's; at the seed the ratio is one BY CONSTRUCTION, so revenue at the seed is today's figure to the cent and no integration
    /// error can move a seeded budget.</para>
    ///
    /// <para><b>What moves the ratio.</b> The player's lever is the line's rate as before (0–70 for a percentage tax): its distance from the
    /// seeded rate is the uniform shift in points. Incomes at a boundary are the seed's log-normals scaled by the real wage index and the
    /// price level (nominal, B6); thresholds move with the price level where the statute indexes them (Sweden's skiktgräns by the KPI
    /// rule, France's barème and the USA's table yearly, Germany's tariff by its periodic revisions - the last an approximation, stated)
    /// and stand nominal where it does not (Poland's 120 000 zł, unchanged since 2022) - so what the ratio carries beyond the lever is
    /// BRACKET CREEP, real for the four and nominal for Poland, and nothing else: the base's own growth stays the base's.</para>
    ///
    /// <para><b>F4-4 / board 15b (2026-09-13): the sub-rows and their own rates.</b> Each statute is a list of SUB-ROWS - the exempt band, the
    /// bands, Germany's ramps (the marginal rate rising from the zone's start to its END rate; the ends are DERIVED from § 32a's
    /// coefficients: 23.97 % at 17 799 €, 42 % at 69 878 €), Sweden's flat municipal layer, Poland's solidarity levy - and a line may carry
    /// the player's own rate for any adjustable one (`TaxLine.BracketRates`, a passed budget bill's figures): a band's rate, a ramp's END rate
    /// (the next ramp's start follows it), the flat layer's rate. The uniform shift still lands on top of every taxed band. What the class
    /// recomputes from a ramp's moved end is the class's, not the statute's, and the row prints it as DRAFT beside the statute's text.</para>
    ///
    /// <para><b>What the incomes are, stated.</b> The EU-SILC figure is equivalised disposable income per person and the CPS figure total
    /// money income per person; a tariff written for taxable individual income is applied to them as the model's one income proxy
    /// (F4-1's two concepts, carried as two). The ratio construction is what makes that honest: only the SHAPE's response enters, never
    /// its level. France's parts are one per person (the incomes are already per person); Sweden's grundavdrag is not modelled and the
    /// state layer starts at the skiktgräns on the whole income; the USA's standard deduction is the single filer's ($16 100, Rev. Proc.
    /// 2025-32 § 3.14) and its table the unmarried one (Table 3); Poland's 3 600 zł credit is folded into a tax-free band to 30 000 zł
    /// (DERIVED: 3 600 ÷ 12 %) and the 4 % solidarity levy above 1 000 000 zł (art. 30h, another act) is a separate term the shift does
    /// not touch; Germany's "x rounded down to a full euro" is not carried (the ramps integrate exactly - within a euro of the formula at
    /// every zone end, the diagnostic checks).</para>
    /// </summary>
    public static class TaxSchedule
    {
        /// <summary>A band of the statute: a marginal rate above a threshold in the statute's currency - flat when the end rate equals the rate, a RAMP
        /// (the marginal rate rising linearly across the band) when the end rate differs.</summary>
        public readonly struct Bracket
        {
            public readonly double Threshold, Rate, RateEnd;
            public Bracket(double threshold, double rate) { Threshold = threshold; Rate = rate; RateEnd = rate; }
            public Bracket(double threshold, double rateStart, double rateEnd) { Threshold = threshold; Rate = rateStart; RateEnd = rateEnd; }
            public bool IsRamp => Math.Abs(RateEnd - Rate) > 1e-9;
            /// <summary>An exempt band: no rate at all - the shift never touches it.</summary>
            public bool IsExempt => Rate <= 0 && RateEnd <= 0;
        }

        /// <summary>One row of the schedule row's sub-rows (board 15b): what it is, its span, its statute rate, whether the player may move it.</summary>
        public readonly struct SubRow
        {
            public readonly TaxSubRowKind Kind;
            public readonly double From, To;
            /// <summary>The statute's rate: a band's, a ramp's END, the flat layer's, the levy's; 0 for the exempt band.</summary>
            public readonly double StatuteRate;
            /// <summary>A ramp's statute START rate (the entry rate for the first ramp, the previous ramp's end otherwise); NaN elsewhere.</summary>
            public readonly double StatuteStart;
            public readonly bool Adjustable;
            /// <summary>The band this sub-row is (index into the statute's brackets), −1 for the flat layer and the levy.</summary>
            public readonly int BracketIndex;
            public SubRow(TaxSubRowKind kind, double from, double to, double statuteRate, double statuteStart, bool adjustable, int bracketIndex)
            {
                Kind = kind; From = from; To = to; StatuteRate = statuteRate; StatuteStart = statuteStart; Adjustable = adjustable; BracketIndex = bracketIndex;
            }
        }

        /// <summary>One country's statute as the model reads it - the figures verbatim from the sourced page or document beside the row.</summary>
        public sealed class Statute
        {
            public CountryId Country;
            public TaxScheduleKind Kind;
            public string Currency;
            /// <summary>The authority and the paragraph, as the register cites them.</summary>
            public string Source;
            /// <summary>The bands (BracketTable, QuotientBareme, Formula's ramps, and TwoLayer's STATE layer); empty for Flat and Billed.</summary>
            public Bracket[] Brackets = Array.Empty<Bracket>();
            /// <summary>TwoLayer: the flat municipal rate on every unit of income, %.</summary>
            public double FlatLayerRate;
            /// <summary>QuotientBareme: parts per person (1 - the incomes are per person).</summary>
            public double Parts = 1.0;
            /// <summary>Whether the statute indexes its thresholds (they move with the price level) or leaves them nominal.</summary>
            public bool ThresholdsIndexed;
            /// <summary>A separate levy above a threshold (Poland's danina solidarnościowa): rate %, threshold; the shift does not touch it.</summary>
            public double LevyThreshold, LevyRate;
            /// <summary>Formula (Germany): the zone ends and coefficients of § 32a, verbatim - kept for the statute's own arithmetic the diagnostic checks the ramps against.</summary>
            public double FormulaGrund, FormulaZone2End, FormulaZone3End, FormulaZone4End;
            /// <summary>The exempt band's own honesty line, where it is derived rather than written (Poland's tax-free band by the credit; the USA's standard deduction).</summary>
            public string ExemptNote;
            /// <summary>A citation line the row prints under the curve (Germany's coefficients verbatim; France's CSG/CRDS supporting row).</summary>
            public string CitationLine;
        }

        // ---- Germany's § 32a EStG 2026, verbatim (ElectionsData/tax/germany/estg_32a.html, digest 053bfcfc) --------------------------
        private const double DeGrund = 12348, DeZone2End = 17799, DeZone3End = 69878, DeZone4End = 277825;
        private const double DeY1 = 914.51, DeY0 = 1400.0, DeZ1 = 173.10, DeZ0 = 2397.0, DeZ2 = 1034.87, DeX4 = 0.42, DeC4 = 11135.63, DeX5 = 0.45, DeC5 = 19470.38;
        /// <summary>The marginal rate at the end of zone 2, %, DERIVED from the coefficients: the derivative of (914,51·y + 1 400)·y at y = (17 799 − 12 348) ⁄ 10 000.</summary>
        public static readonly double DeRampEnd2 = (2 * DeY1 * ((DeZone2End - DeGrund) / 10000.0) + DeY0) / 100.0;
        /// <summary>The marginal rate at the end of zone 3, %, DERIVED: the derivative of (173,10·z + 2 397)·z + 1 034,87 at z = (69 878 − 17 799) ⁄ 10 000 - which is 42.</summary>
        public static readonly double DeRampEnd3 = (2 * DeZ1 * ((DeZone3End - DeZone2End) / 10000.0) + DeZ0) / 100.0;
        /// <summary>The entry rate at the Grundfreibetrag, %: the derivative at y = 0, 1 400 ⁄ 100.</summary>
        public static readonly double DeEntryRate = DeY0 / 100.0;

        private static readonly Dictionary<CountryId, Statute> Statutes = new Dictionary<CountryId, Statute>
        {
            // Sweden: SCB Kommunalskatterna 2026 (national average 32.38 %); Skatteverket Belopp och procent 2026 (statlig 20 % above the skiktgräns 643 000 kr) - POLISIM_TAX_SPECLET.md §2
            { CountryId.Sweden, new Statute { Country = CountryId.Sweden, Kind = TaxScheduleKind.TwoLayer, Currency = "SEK", Source = "SCB Kommunalskatterna 2026 · Skatteverket Belopp och procent 2026 (spec-let §2)",
                FlatLayerRate = 32.38, Brackets = new[] { new Bracket(0, 0), new Bracket(643000, 20) }, ThresholdsIndexed = true, ExemptNote = "FROM THE SKIKTGRÄNS · NO GRUNDAVDRAG" } },
            // Germany: § 32a EStG, ab dem Veranlagungszeitraum 2026 - the formula's zones as ramps, the ends derived from the coefficients
            { CountryId.Germany, new Statute { Country = CountryId.Germany, Kind = TaxScheduleKind.Formula, Currency = "EUR", Source = "§ 32a EStG 2026 · gesetze-im-internet · 053bfcfc",
                Brackets = new[] { new Bracket(0, 0), new Bracket(DeGrund, DeEntryRate, DeRampEnd2), new Bracket(DeZone2End, DeRampEnd2, DeRampEnd3), new Bracket(DeZone3End, 42), new Bracket(DeZone4End, 45) },
                FormulaGrund = DeGrund, FormulaZone2End = DeZone2End, FormulaZone3End = DeZone3End, FormulaZone4End = DeZone4End, ThresholdsIndexed = true,
                CitationLine = "(914,51 · y + 1 400) · y · (173,10 · z + 2 397) · z + 1 034,87 · 0,42 · x – 11 135,63 · 0,45 · x – 19 470,38 — THE STATUTE'S COEFFICIENTS, VERBATIM · SPLITTING: TWICE THE TARIFF ON HALF, NOT DRAWN" } },
            // France: barème 2026 on 2025 income, per part - economie.gouv.fr / service-public F1419
            { CountryId.France, new Statute { Country = CountryId.France, Kind = TaxScheduleKind.QuotientBareme, Currency = "EUR", Source = "barème 2026 · economie.gouv.fr 964f5bee · service-public F1419 741a32d7",
                Brackets = new[] { new Bracket(0, 0), new Bracket(11600, 11), new Bracket(29579, 30), new Bracket(84577, 41), new Bracket(181917, 45) }, Parts = 1.0, ThresholdsIndexed = true,
                CitationLine = "CSG 9.20 + CRDS 0.50 ON 98.25 % OF GROSS TO 192 240 € · 2.40 OF IT DEDUCTIBLE — THE SPLIT IS THE INSTRUMENT · A SUPPORTING FLAT ROW, NOT A BRACKET · urssaf 2fd04556" } },
            // Italy: three layers billed (E-6) - the line stays flat and says so
            { CountryId.Italy, new Statute { Country = CountryId.Italy, Kind = TaxScheduleKind.ThreeLayerBilled, Currency = "EUR", Source = "TUIR art. 11 · BILLED (E-6): no primary text with a text layer on disk", ThresholdsIndexed = true } },
            // Poland: art. 27 ust. 1 PIT (12 % minus 3 600 zł to 120 000; 32 % above) and art. 30h (4 % above 1 000 000) - podatki.gov.pl 6426b64c, the Sejm's consolidated act eca28a1b
            { CountryId.Poland, new Statute { Country = CountryId.Poland, Kind = TaxScheduleKind.BracketTable, Currency = "PLN", Source = "art. 27 ust. 1 PIT · podatki.gov.pl 6426b64c · Sejm ELI eca28a1b · art. 30h",
                Brackets = new[] { new Bracket(0, 0), new Bracket(30000, 12), new Bracket(120000, 32) }, LevyThreshold = 1000000, LevyRate = 4, ThresholdsIndexed = false, ExemptNote = "TAX-FREE BY THE 3 600 zł CREDIT · DERIVED" } },
            // USA: Rev. Proc. 2025-32 § 1(j)(2) Table 3 (unmarried individuals) with § 3.14's standard deduction $16 100 folded in as the exempt band - rp-25-32.pdf e9ada115
            { CountryId.USA, new Statute { Country = CountryId.USA, Kind = TaxScheduleKind.BracketTable, Currency = "USD", Source = "Rev. Proc. 2025-32 § 1(j)(2) Table 3 · § 3.14 · irs.gov e9ada115",
                Brackets = new[] { new Bracket(0, 0), new Bracket(16100, 10), new Bracket(16100 + 12400, 12), new Bracket(16100 + 50400, 22), new Bracket(16100 + 105700, 24), new Bracket(16100 + 201775, 32), new Bracket(16100 + 256225, 35), new Bracket(16100 + 640600, 37) }, ThresholdsIndexed = true,
                ExemptNote = "DEDUCTION $16,100 · TABLE 3 · SINGLE" } },
        };

        /// <summary>The number of quantile points a cohort's log-normal is integrated over - midpoints in probability, deterministic.</summary>
        public const int QuadraturePoints = 200;

        public static Statute Of(CountryId id) => Statutes.TryGetValue(id, out Statute s) ? s : new Statute { Country = id, Kind = TaxScheduleKind.Flat, Currency = EnergyLayer.CurrencyCode(id), Source = "no statute on file", ThresholdsIndexed = true };

        /// <summary>Whether a country's income tax has a shape that responds (Flat and ThreeLayerBilled do not - their ratio is one).</summary>
        public static bool Responds(CountryId id) { TaxScheduleKind k = Of(id).Kind; return k != TaxScheduleKind.Flat && k != TaxScheduleKind.ThreeLayerBilled; }

        /// <summary>The kind's word on the row (board 15b): BRACKETS · FORMULA · PER PART · LAYERS · BILLED.</summary>
        public static string KindWord(TaxScheduleKind kind)
        {
            switch (kind)
            {
                case TaxScheduleKind.BracketTable: return "BRACKETS";
                case TaxScheduleKind.Formula: return "FORMULA";
                case TaxScheduleKind.QuotientBareme: return "PER PART";
                case TaxScheduleKind.TwoLayer: return "TWO LAYERS";
                case TaxScheduleKind.ThreeLayerBilled: return "LAYERS · BILLED";
                default: return "FLAT";
            }
        }

        /// <summary>Requirement (1): the statute's currency per unit of the cohort incomes' currency - SEK and PLN per EUR for the two, one elsewhere.
        /// The incomes are EUR for the EU five (`CohortIncomeSeeds`, EU-SILC) and USD for the USA; the rates are the catalog's ECB 2023 reference.</summary>
        public static double StatutePerIncomeUnit(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:
                case CountryId.Poland:
                    return EnergyLayer.NationalPerUsd(id) / Math.Max(1e-9, EnergyLayer.NationalPerUsd(CountryId.Germany));   // national per USD ÷ EUR per USD = national per EUR
                default:
                    return 1.0;
            }
        }

        // ---- the sub-rows: what the schedule row draws and what a bill may move ---------------------------------------------------------

        /// <summary>The statute as sub-rows, in the order the row draws them: the flat layer first (Sweden), then every band from the exempt one up, then the levy (Poland).</summary>
        public static List<SubRow> SubRows(Statute s)
        {
            var rows = new List<SubRow>();
            if (s.Kind == TaxScheduleKind.TwoLayer) { rows.Add(new SubRow(TaxSubRowKind.FlatLayer, 0, double.PositiveInfinity, s.FlatLayerRate, double.NaN, true, -1)); }
            for (int i = 0; i < s.Brackets.Length; i++)
            {
                Bracket b = s.Brackets[i];
                double to = i + 1 < s.Brackets.Length ? s.Brackets[i + 1].Threshold : double.PositiveInfinity;
                if (b.IsExempt) { rows.Add(new SubRow(TaxSubRowKind.Exempt, b.Threshold, to, 0, double.NaN, false, i)); }
                else if (b.IsRamp) { rows.Add(new SubRow(TaxSubRowKind.Ramp, b.Threshold, to, b.RateEnd, b.Rate, true, i)); }
                else { rows.Add(new SubRow(TaxSubRowKind.Band, b.Threshold, to, b.Rate, double.NaN, true, i)); }
            }
            if (s.LevyRate > 0) { rows.Add(new SubRow(TaxSubRowKind.Levy, s.LevyThreshold, double.PositiveInfinity, s.LevyRate, double.NaN, false, -1)); }
            return rows;
        }

        /// <summary>A line's own rate for a sub-row, or the statute's where the line carries none (a negative or missing entry is "the statute's").</summary>
        public static double RowRate(Statute s, SubRow row, IReadOnlyList<float> overrides, int rowIndex)
        {
            if (row.Adjustable && overrides != null && rowIndex < overrides.Count && overrides[rowIndex] >= 0f) { return overrides[rowIndex]; }
            return row.StatuteRate;
        }

        /// <summary>The rate every sub-row stands at under a line's overrides and the uniform shift: the flat layer's, each band's (a ramp's END), the levy's; the exempt band's 0.</summary>
        public static double[] EffectiveRowRates(Statute s, IReadOnlyList<float> overrides, double shiftPoints)
        {
            List<SubRow> rows = SubRows(s);
            var rates = new double[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                SubRow r = rows[i];
                double rate = RowRate(s, r, overrides, i);
                rates[i] = r.Kind == TaxSubRowKind.Exempt || r.Kind == TaxSubRowKind.Levy ? rate : rate + shiftPoints;
            }
            return rates;
        }

        /// <summary>The bands as they stand under a line's overrides and the shift: each band's start and end marginal rate, %, in the statute's currency at the seed's prices.
        /// A ramp's start follows the previous band's moved end by the same points it moved (the entry rate at the Grundfreibetrag is the statute's own and takes the shift).</summary>
        private static void BandRates(Statute s, IReadOnlyList<float> overrides, double shiftPoints, double[] starts, double[] ends)
        {
            List<SubRow> rows = SubRows(s);
            double previousEndDelta = 0;
            for (int i = 0; i < s.Brackets.Length; i++)
            {
                Bracket b = s.Brackets[i];
                if (b.IsExempt) { starts[i] = 0; ends[i] = 0; previousEndDelta = 0; continue; }
                int rowIndex = -1;
                for (int r = 0; r < rows.Count; r++) { if (rows[r].BracketIndex == i) { rowIndex = r; break; } }
                double end = rowIndex >= 0 ? RowRate(s, rows[rowIndex], overrides, rowIndex) : b.RateEnd;
                double start = b.IsRamp ? b.Rate + previousEndDelta : end;
                previousEndDelta = end - b.RateEnd;
                starts[i] = start + shiftPoints; ends[i] = end + shiftPoints;
            }
        }

        /// <summary>The tax the statute levies on one income in its own currency (the statute's rates alone, the uniform shift on every taxed band).</summary>
        public static double Tax(Statute s, double income, double shiftPoints, double thresholdScale) => Tax(s, income, shiftPoints, thresholdScale, null);

        /// <summary>The tax the statute levies on one income in its own currency, with a line's own rates for the sub-rows it moved (null: the statute's),
        /// the uniform shift (points) on every band whose rate is above zero (requirement 2), and the thresholds scaled where the statute indexes them.</summary>
        public static double Tax(Statute s, double income, double shiftPoints, double thresholdScale, IReadOnlyList<float> overrides)
        {
            if (income <= 0) { return 0; }
            switch (s.Kind)
            {
                case TaxScheduleKind.BracketTable:
                    return BandTax(s, income, shiftPoints, thresholdScale, overrides) + LevyTax(s, income, thresholdScale);
                case TaxScheduleKind.QuotientBareme:
                    return s.Parts * BandTax(s, income / Math.Max(1e-9, s.Parts), shiftPoints, thresholdScale, overrides);
                case TaxScheduleKind.TwoLayer:
                {
                    List<SubRow> rows = SubRows(s);
                    double flat = RowRate(s, rows[0], overrides, 0) + shiftPoints;
                    return flat / 100.0 * income + BandTax(s, income, shiftPoints, thresholdScale, overrides);
                }
                case TaxScheduleKind.Formula:
                    return BandTax(s, income, shiftPoints, thresholdScale, overrides);
                default:
                    return 0;
            }
        }

        /// <summary>The bands' tax: a flat band's rate on its span, a ramp's average rate on its span - the integral of a marginal rate that rises linearly from the band's start to its end.</summary>
        private static double BandTax(Statute s, double income, double shiftPoints, double thresholdScale, IReadOnlyList<float> overrides)
        {
            int n = s.Brackets.Length;
            var starts = new double[n]; var ends = new double[n];
            BandRates(s, overrides, shiftPoints, starts, ends);
            double tax = 0;
            for (int i = 0; i < n; i++)
            {
                if (s.Brackets[i].IsExempt) { continue; }
                double lo = s.Brackets[i].Threshold * thresholdScale;
                double hi = i + 1 < n ? s.Brackets[i + 1].Threshold * thresholdScale : double.PositiveInfinity;
                double span = Math.Min(income, hi) - lo;
                if (span <= 0) { continue; }
                double width = hi - lo;
                if (double.IsInfinity(width) || Math.Abs(ends[i] - starts[i]) < 1e-12)
                {
                    tax += ends[i] / 100.0 * span;
                }
                else
                {
                    // the marginal rate at x in the band: start + (end − start) · (x − lo) ⁄ width; its integral from lo to lo + span
                    double slope = (ends[i] - starts[i]) / width;
                    tax += (starts[i] * span + 0.5 * slope * span * span) / 100.0;
                }
            }
            return tax;
        }

        private static double LevyTax(Statute s, double income, double thresholdScale)
        {
            if (s.LevyRate <= 0) { return 0; }
            double above = income - s.LevyThreshold * thresholdScale;
            return above > 0 ? s.LevyRate / 100.0 * above : 0;
        }

        /// <summary>§ 32a EStG's own arithmetic on an income in the seed's euros (no shift, no override) - what the ramps are checked against at every zone end.</summary>
        public static double StatuteFormulaTax(Statute s, double income)
        {
            double x = Math.Floor(income);
            if (x <= s.FormulaGrund) { return 0; }
            if (x <= s.FormulaZone2End) { double y = (x - s.FormulaGrund) / 10000.0; return (DeY1 * y + DeY0) * y; }
            if (x <= s.FormulaZone3End) { double z = (x - s.FormulaZone2End) / 10000.0; return (DeZ1 * z + DeZ0) * z + DeZ2; }
            if (x <= s.FormulaZone4End) { return DeX4 * x - DeC4; }
            return DeX5 * x - DeC5;
        }

        /// <summary>The marginal rate at an income, %, under the line's rates and the shift - the curve a row draws (15b).</summary>
        public static double MarginalRate(Statute s, double income, double shiftPoints, double thresholdScale, IReadOnlyList<float> overrides = null)
        {
            const double h = 1.0;
            return 100.0 * (Tax(s, income + h, shiftPoints, thresholdScale, overrides) - Tax(s, income, shiftPoints, thresholdScale, overrides)) / h;
        }

        /// <summary>The top threshold the row's x axis runs to × 1.5 (board 15b: every curve fills its lane, no shared axis across currencies), in the statute's currency at the seed's prices.</summary>
        public static double AxisCeiling(Statute s)
        {
            double top = 0;
            foreach (Bracket b in s.Brackets) { top = Math.Max(top, b.Threshold); }
            if (s.LevyRate > 0) { top = Math.Max(top, s.LevyThreshold); }
            return top > 0 ? top * 1.5 : 100000;
        }

        /// <summary>The first threshold above which the statute levies a rate above zero, in its currency at the seed's prices; 0 for a shape with none.</summary>
        public static double FirstTaxedThreshold(Statute s)
        {
            if (s.Kind == TaxScheduleKind.TwoLayer) { return 0; }   // the municipal layer taxes the first krona
            foreach (Bracket b in s.Brackets) { if (!b.IsExempt) { return b.Threshold; } }
            return 0;
        }

        /// <summary>The state layer's threshold for Sweden (the skiktgräns) or the first taxed threshold elsewhere - the figure requirement (1) is judged against.</summary>
        public static double FirstBracketThreshold(Statute s)
        {
            foreach (Bracket b in s.Brackets) { if (!b.IsExempt) { return b.Threshold; } }
            return 0;
        }

        /// <summary>The cohorts' incomes in the statute's currency at a scale: median × the conversion × the scale, sigma as seeded.</summary>
        private static bool CohortIncome(Country country, int cohort, double conversion, double incomeScale, out double median, out double sigma)
        {
            median = 0; sigma = 0;
            PopulationCohorts c = country.Cohorts;
            if (c == null || !c.HasIncome(cohort)) { return false; }
            median = c.IncomeMedian[cohort] * conversion * incomeScale;
            sigma = c.IncomeSigma[cohort];
            return median > 0;
        }

        /// <summary>
        /// The headcount-weighted AVERAGE EFFECTIVE RATE the statute yields, %, over every cohort with an income: Σ counts × E[T(y)] ⁄ Σ counts × E[y],
        /// each cohort's y a log-normal in the statute's currency (median × conversion × incomeScale, the seeded sigma), integrated over
        /// <see cref="QuadraturePoints"/> quantile midpoints. Zero where no cohort has an income or the shape does not respond.
        /// </summary>
        public static double AverageEffectiveRate(Country country, double shiftPoints, double incomeScale, double thresholdScale, IReadOnlyList<float> overrides = null)
        {
            Statute s = Of(country.Id);
            if (!Responds(country.Id) || country.Cohorts == null) { return 0; }
            double conversion = StatutePerIncomeUnit(country.Id);
            double taxSum = 0, incomeSum = 0;
            for (int cohort = 0; cohort < PopulationCohorts.CohortCount; cohort++)
            {
                if (!CohortIncome(country, cohort, conversion, incomeScale, out double median, out double sigma)) { continue; }
                double count = country.Cohorts.Counts[cohort];
                if (count <= 0) { continue; }
                double t = 0, y = 0;
                for (int i = 0; i < QuadraturePoints; i++)
                {
                    double p = (i + 0.5) / QuadraturePoints;
                    double income = median * Math.Exp(sigma * InverseNormal(p));
                    t += Tax(s, income, shiftPoints, thresholdScale, overrides);
                    y += income;
                }
                taxSum += count * t / QuadraturePoints;
                incomeSum += count * y / QuadraturePoints;
            }
            return incomeSum > 0 ? 100.0 * taxSum / incomeSum : 0;
        }

        /// <summary>The headcount-weighted mean income in the statute's currency at a scale - the "average wage" 15b's figure is read at.</summary>
        public static double AverageIncome(Country country, double incomeScale)
        {
            if (country.Cohorts == null) { return 0; }
            double conversion = StatutePerIncomeUnit(country.Id);
            double sum = 0, n = 0;
            for (int cohort = 0; cohort < PopulationCohorts.CohortCount; cohort++)
            {
                if (!CohortIncome(country, cohort, conversion, incomeScale, out double median, out double sigma)) { continue; }
                double count = country.Cohorts.Counts[cohort];
                if (count <= 0) { continue; }
                sum += count * median * Math.Exp(0.5 * sigma * sigma);
                n += count;
            }
            return n > 0 ? sum / n : 0;
        }

        /// <summary>The share of the cohorts' aggregate income that lies above a threshold in the statute's currency - the reading requirement (1) is judged by.</summary>
        public static double IncomeShareAbove(Country country, double threshold, double incomeScale)
        {
            if (country.Cohorts == null) { return 0; }
            double conversion = StatutePerIncomeUnit(country.Id);
            double above = 0, all = 0;
            for (int cohort = 0; cohort < PopulationCohorts.CohortCount; cohort++)
            {
                if (!CohortIncome(country, cohort, conversion, incomeScale, out double median, out double sigma)) { continue; }
                double count = country.Cohorts.Counts[cohort];
                if (count <= 0) { continue; }
                for (int i = 0; i < QuadraturePoints; i++)
                {
                    double income = median * Math.Exp(sigma * InverseNormal((i + 0.5) / QuadraturePoints));
                    all += count * income;
                    if (income > threshold) { above += count * (income - threshold); }
                }
            }
            return all > 0 ? above / all : 0;
        }

        /// <summary>The income scale at this BOUNDARY: the seed's log-normals grow with the real wage index (seeded 100) and the price level (seeded 1) -
        /// nominal, B6 - read at the last index, as the statutes and the incomes are annual; a day inside the year reads the year's figure.</summary>
        public static double IncomeScale(Country country) => Math.Max(0.01, BoundaryWage(country) / 100.0) * Math.Max(0.01, BoundaryLevel(country));

        /// <summary>The threshold scale at this boundary: the price level where the statute indexes its thresholds, one where it leaves them nominal.</summary>
        public static double ThresholdScale(Country country) => Of(country.Id).ThresholdsIndexed ? Math.Max(0.01, BoundaryLevel(country)) : 1.0;

        private static float BoundaryLevel(Country country) => country.PriceLevelAtLastIndex > 0f ? country.PriceLevelAtLastIndex : country.State.PriceLevel;
        private static float BoundaryWage(Country country) => country.RealWageIndexAtLastIndex > 0f ? country.RealWageIndexAtLastIndex : country.State.RealWageIndex;

        /// <summary>The per-country memo of yields by lever position, valid for one boundary and one set of the line's own rates: the daily terms read the
        /// yield every day and the AI ministry's search reads it at many lever positions, and the integration behind it is a boundary figure, not a daily one.</summary>
        public sealed class Memo
        {
            public float Level = float.NaN, Wage = float.NaN;
            public string Overrides = "";
            public readonly Dictionary<int, double> ByRate = new Dictionary<int, double>();
            public bool Holds(float level, float wage, string overrides) => Level == level && Wage == wage && Overrides == overrides;
            public void Reset(float level, float wage, string overrides) { Level = level; Wage = wage; Overrides = overrides; ByRate.Clear(); }
        }

        /// <summary>A line's own rates as one string - the memo's key and the record's print; empty where the line carries none.</summary>
        public static string OverridesKey(TaxLine line)
        {
            if (line == null || line.BracketRates == null || line.BracketRates.Length == 0) { return ""; }
            var sb = new System.Text.StringBuilder();
            foreach (float r in line.BracketRates) { sb.Append(r.ToString("0.##", CultureInfo.InvariantCulture)).Append(';'); }
            return sb.ToString();
        }

        /// <summary>The seeded rate the shift is measured from - captured at the seed; a save from before it is captured reads the line's rate as the seed's.</summary>
        public static float RateSeedOf(TaxLine line)
        {
            if (line.RateSeed <= 0f) { line.RateSeed = line.Rate; }
            return line.RateSeed;
        }

        /// <summary>
        /// The average effective rate the statute yields at a lever position, %, over the cohorts' incomes and the thresholds as they stand at this
        /// boundary and the line's own rates - memoised per country, boundary and rates, and rounded to the float the seed yield is stored as, so
        /// the seed's own reading is the seed's figure exactly. Zero for a shape that does not respond.
        /// </summary>
        public static double YieldNow(Country country, TaxLine line, float rate)
        {
            if (!Responds(country.Id)) { return 0.0; }
            if (country.IncomeTaxSeedAer <= 0f)
            {
                double captured = AverageEffectiveRate(country, 0, 1.0, 1.0);   // a save from before the capture: the reference is taken now, and the ratio is one now
                if (captured <= 0) { return 0.0; }
                country.IncomeTaxSeedAer = (float)captured;
            }
            float level = BoundaryLevel(country), wage = BoundaryWage(country);
            string overrides = OverridesKey(line);
            Memo memo = country.ScheduleMemo ?? (country.ScheduleMemo = new Memo());
            if (!memo.Holds(level, wage, overrides)) { memo.Reset(level, wage, overrides); }
            int key = (int)Math.Round(rate * 100f);
            if (memo.ByRate.TryGetValue(key, out double cached)) { return cached; }
            double shift = rate - RateSeedOf(line);
            double now = (double)(float)AverageEffectiveRate(country, shift, IncomeScale(country), ThresholdScale(country), line.BracketRates);
            if (memo.ByRate.Count < 512) { memo.ByRate[key] = now; }
            return now;
        }

        /// <summary>
        /// Requirement (3): the schedule's response as a RATIO - the yield at this lever position over the yield at the seed (`Country.IncomeTaxSeedAer`,
        /// captured with the structural bases). Exactly one at the seed, one for a shape that does not respond, one where no seed yield is on record.
        /// The revenue engine multiplies its anchored figure by it: a five-point rise of Poland's two bracket rates yields some forty per cent more,
        /// as the real PIT would, because the statute's yield is small against its rates - the schedule's own elasticity, not the flat line's.
        /// </summary>
        public static double YieldRatio(Country country, TaxLine line, float rate)
        {
            if (!Responds(country.Id)) { return 1.0; }
            double now = YieldNow(country, line, rate);
            return now > 0 && country.IncomeTaxSeedAer > 0f ? now / country.IncomeTaxSeedAer : 1.0;
        }

        /// <summary>
        /// The flat-equivalent rate a line stands at for the terms that read "the income tax rate" as a share of income - the household burden,
        /// the Gini term, the labour-supply term: the seeded rate moved by the POINTS the statute's yield moved, seed + (yield now − yield at the seed).
        /// A five-point rise of every taxed band moves this by the taxed share of income times five - two and a half points where half the
        /// income sits above the first threshold - which is what a household's share of income taxed actually does; the seeded rate itself is
        /// the anchored figure on the sourced base, not a yield, so it is moved in points, never scaled. The lever's own rate for a shape that
        /// does not respond.
        /// </summary>
        public static float EffectiveRate(Country country, TaxLine line, float rate)
        {
            if (!Responds(country.Id)) { return rate; }
            double now = YieldNow(country, line, rate);
            if (now <= 0 || country.IncomeTaxSeedAer <= 0f) { return rate; }
            return (float)(RateSeedOf(line) + (now - country.IncomeTaxSeedAer));
        }

        /// <summary>The average effective rate at the headcount-weighted mean income - 15b's one figure - %, at this turn's scales, the lever's shift and the line's own rates.</summary>
        public static double AverageEffectiveRateAtMeanIncome(Country country, TaxLine line, float rate)
        {
            if (!Responds(country.Id)) { return rate; }
            double income = AverageIncome(country, IncomeScale(country));
            if (income <= 0) { return 0; }
            return 100.0 * Tax(Of(country.Id), income, rate - RateSeedOf(line), ThresholdScale(country), line.BracketRates) / income;
        }

        /// <summary>The standard normal's quantile (Acklam's rational approximation, relative error below 1.2e-9) - the log-normal's integration grid.</summary>
        public static double InverseNormal(double p)
        {
            if (p <= 0) { return double.NegativeInfinity; }
            if (p >= 1) { return double.PositiveInfinity; }
            const double a1 = -3.969683028665376e+01, a2 = 2.209460984245205e+02, a3 = -2.759285104469687e+02, a4 = 1.383577518672690e+02, a5 = -3.066479806614716e+01, a6 = 2.506628277459239e+00;
            const double b1 = -5.447609879822406e+01, b2 = 1.615858368580409e+02, b3 = -1.556989798598866e+02, b4 = 6.680131188771972e+01, b5 = -1.328068155288572e+01;
            const double c1 = -7.784894002430293e-03, c2 = -3.223964580411365e-01, c3 = -2.400758277161838e+00, c4 = -2.549732539343734e+00, c5 = 4.374664141464968e+00, c6 = 2.938163982698783e+00;
            const double d1 = 7.784695709041462e-03, d2 = 3.224671290700398e-01, d3 = 2.445134137142996e+00, d4 = 3.754408661907416e+00;
            const double pLow = 0.02425, pHigh = 1 - pLow;
            double q, r;
            if (p < pLow)
            {
                q = Math.Sqrt(-2 * Math.Log(p));
                return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) / ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }
            if (p <= pHigh)
            {
                q = p - 0.5; r = q * q;
                return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q / (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
            }
            q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) / ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
    }
}
