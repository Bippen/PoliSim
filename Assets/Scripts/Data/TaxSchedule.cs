using System;
using System.Collections.Generic;
using PoliSim.Data.Generated;

namespace PoliSim.Data
{
    /// <summary>The shape a country's income-tax statute has (`POLISIM_TAX_SPECLET.md` D-3 (b), ruled 2026-08-31; the plan's S3a, §474).</summary>
    public enum TaxScheduleKind
    {
        /// <summary>No schedule: the line is the flat rate it always was, and the response ratio is exactly one.</summary>
        Flat,
        /// <summary>A bracket table - a marginal rate above each threshold (Poland's skala, the USA's § 1(j)(2) table with the standard deduction folded in as the exempt band).</summary>
        BracketTable,
        /// <summary>A formula in the statute (Germany's § 32a EStG: two quadratic ramps, then two linear zones), the marginal rate its derivative.</summary>
        Formula,
        /// <summary>A barème applied per PART of the quotient familial (France): the table on income per part, the tax times the parts.</summary>
        QuotientBareme,
        /// <summary>Two layers on one line (Sweden, DS-2d ruled): a flat municipal rate on every krona and a state bracket above the skiktgräns.</summary>
        TwoLayer,
        /// <summary>Three layers billed (Italy, E-6): the statute is not on disk with a text layer, so the line stays flat and says so.</summary>
        ThreeLayerBilled,
    }

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
    /// <para><b>What the incomes are, stated.</b> The EU-SILC figure is equivalised disposable income per person and the CPS figure total
    /// money income per person; a tariff written for taxable individual income is applied to them as the model's one income proxy
    /// (F4-1's two concepts, carried as two). The ratio construction is what makes that honest: only the SHAPE's response enters, never
    /// its level. France's parts are one per person (the incomes are already per person); Sweden's grundavdrag is not modelled and the
    /// state layer starts at the skiktgräns on the whole income; the USA's standard deduction is the single filer's ($16 100, Rev. Proc.
    /// 2025-32 § 3.14) and its table the unmarried one (Table 3); Poland's 3 600 zł credit is folded into a tax-free band to 30 000 zł
    /// (DERIVED: 3 600 ÷ 12 %) and the 4 % solidarity levy above 1 000 000 zł (art. 30h, another act) is a separate term the shift does
    /// not touch.</para>
    /// </summary>
    public static class TaxSchedule
    {
        /// <summary>A marginal rate, in per cent, on the income above a threshold in the statute's currency.</summary>
        public readonly struct Bracket
        {
            public readonly double Threshold, Rate;
            public Bracket(double threshold, double rate) { Threshold = threshold; Rate = rate; }
        }

        /// <summary>One country's statute as the model reads it - the figures verbatim from the sourced page or document beside the row.</summary>
        public sealed class Statute
        {
            public CountryId Country;
            public TaxScheduleKind Kind;
            public string Currency;
            /// <summary>The authority and the paragraph, as the register cites them.</summary>
            public string Source;
            /// <summary>The bracket table (BracketTable, QuotientBareme, and TwoLayer's STATE layer); empty for Formula and Flat.</summary>
            public Bracket[] Brackets = Array.Empty<Bracket>();
            /// <summary>TwoLayer: the flat municipal rate on every unit of income, %.</summary>
            public double FlatLayerRate;
            /// <summary>QuotientBareme: parts per person (1 - the incomes are per person).</summary>
            public double Parts = 1.0;
            /// <summary>Whether the statute indexes its thresholds (they move with the price level) or leaves them nominal.</summary>
            public bool ThresholdsIndexed;
            /// <summary>A separate levy above a threshold (Poland's danina solidarnościowa): rate %, threshold; the shift does not touch it.</summary>
            public double LevyThreshold, LevyRate;
            /// <summary>Formula (Germany): the zone ends and coefficients of § 32a, verbatim.</summary>
            public double FormulaGrund, FormulaZone2End, FormulaZone3End, FormulaZone4End;
        }

        // ---- Germany's § 32a EStG 2026, verbatim (ElectionsData/tax/germany/estg_32a.html, digest 053bfcfc) --------------------------
        private const double DeY1 = 914.51, DeY0 = 1400.0, DeZ1 = 173.10, DeZ0 = 2397.0, DeZ2 = 1034.87, DeX4 = 0.42, DeC4 = 11135.63, DeX5 = 0.45, DeC5 = 19470.38;

        private static readonly Dictionary<CountryId, Statute> Statutes = new Dictionary<CountryId, Statute>
        {
            // Sweden: SCB Kommunalskatterna 2026 (national average 32.38 %); Skatteverket Belopp och procent 2026 (statlig 20 % above the skiktgräns 643 000 kr) - POLISIM_TAX_SPECLET.md §2
            { CountryId.Sweden, new Statute { Country = CountryId.Sweden, Kind = TaxScheduleKind.TwoLayer, Currency = "SEK", Source = "SCB Kommunalskatterna 2026 · Skatteverket Belopp och procent 2026 (spec-let §2)",
                FlatLayerRate = 32.38, Brackets = new[] { new Bracket(0, 0), new Bracket(643000, 20) }, ThresholdsIndexed = true } },
            // Germany: § 32a EStG, ab dem Veranlagungszeitraum 2026 - the formula's zones and coefficients
            { CountryId.Germany, new Statute { Country = CountryId.Germany, Kind = TaxScheduleKind.Formula, Currency = "EUR", Source = "§ 32a EStG 2026 · gesetze-im-internet · 053bfcfc",
                FormulaGrund = 12348, FormulaZone2End = 17799, FormulaZone3End = 69878, FormulaZone4End = 277825, ThresholdsIndexed = true } },
            // France: barème 2026 on 2025 income, per part - economie.gouv.fr / service-public F1419
            { CountryId.France, new Statute { Country = CountryId.France, Kind = TaxScheduleKind.QuotientBareme, Currency = "EUR", Source = "barème 2026 · economie.gouv.fr 964f5bee · service-public F1419 741a32d7",
                Brackets = new[] { new Bracket(0, 0), new Bracket(11600, 11), new Bracket(29579, 30), new Bracket(84577, 41), new Bracket(181917, 45) }, Parts = 1.0, ThresholdsIndexed = true } },
            // Italy: three layers billed (E-6) - the line stays flat and says so
            { CountryId.Italy, new Statute { Country = CountryId.Italy, Kind = TaxScheduleKind.ThreeLayerBilled, Currency = "EUR", Source = "TUIR art. 11 · BILLED (E-6): no primary text with a text layer on disk", ThresholdsIndexed = true } },
            // Poland: art. 27 ust. 1 PIT (12 % minus 3 600 zł to 120 000; 32 % above) and art. 30h (4 % above 1 000 000) - podatki.gov.pl 6426b64c, the Sejm's consolidated act eca28a1b
            { CountryId.Poland, new Statute { Country = CountryId.Poland, Kind = TaxScheduleKind.BracketTable, Currency = "PLN", Source = "art. 27 ust. 1 PIT · podatki.gov.pl 6426b64c · Sejm ELI eca28a1b · art. 30h",
                Brackets = new[] { new Bracket(0, 0), new Bracket(30000, 12), new Bracket(120000, 32) }, LevyThreshold = 1000000, LevyRate = 4, ThresholdsIndexed = false } },
            // USA: Rev. Proc. 2025-32 § 1(j)(2) Table 3 (unmarried individuals) with § 3.14's standard deduction $16 100 folded in as the exempt band - rp-25-32.pdf e9ada115
            { CountryId.USA, new Statute { Country = CountryId.USA, Kind = TaxScheduleKind.BracketTable, Currency = "USD", Source = "Rev. Proc. 2025-32 § 1(j)(2) Table 3 · § 3.14 · irs.gov e9ada115",
                Brackets = new[] { new Bracket(0, 0), new Bracket(16100, 10), new Bracket(16100 + 12400, 12), new Bracket(16100 + 50400, 22), new Bracket(16100 + 105700, 24), new Bracket(16100 + 201775, 32), new Bracket(16100 + 256225, 35), new Bracket(16100 + 640600, 37) }, ThresholdsIndexed = true } },
        };

        /// <summary>The number of quantile points a cohort's log-normal is integrated over - midpoints in probability, deterministic.</summary>
        public const int QuadraturePoints = 200;

        public static Statute Of(CountryId id) => Statutes.TryGetValue(id, out Statute s) ? s : new Statute { Country = id, Kind = TaxScheduleKind.Flat, Currency = EnergyLayer.CurrencyCode(id), Source = "no statute on file", ThresholdsIndexed = true };

        /// <summary>Whether a country's income tax has a shape that responds (Flat and ThreeLayerBilled do not - their ratio is one).</summary>
        public static bool Responds(CountryId id) { TaxScheduleKind k = Of(id).Kind; return k != TaxScheduleKind.Flat && k != TaxScheduleKind.ThreeLayerBilled; }

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

        /// <summary>The tax the statute levies on one income in its own currency, with the player's uniform shift (points) on every band whose statutory rate
        /// is above zero (requirement 2) and the thresholds scaled where the statute indexes them.</summary>
        public static double Tax(Statute s, double income, double shiftPoints, double thresholdScale)
        {
            if (income <= 0) { return 0; }
            switch (s.Kind)
            {
                case TaxScheduleKind.BracketTable:
                    return BracketTax(s.Brackets, income, shiftPoints, thresholdScale) + LevyTax(s, income, thresholdScale);
                case TaxScheduleKind.QuotientBareme:
                    return s.Parts * BracketTax(s.Brackets, income / Math.Max(1e-9, s.Parts), shiftPoints, thresholdScale);
                case TaxScheduleKind.TwoLayer:
                    return (s.FlatLayerRate + shiftPoints) / 100.0 * income + BracketTax(s.Brackets, income, shiftPoints, thresholdScale);
                case TaxScheduleKind.Formula:
                    return FormulaTax(s, income, shiftPoints, thresholdScale);
                default:
                    return 0;
            }
        }

        private static double BracketTax(Bracket[] brackets, double income, double shiftPoints, double thresholdScale)
        {
            double tax = 0;
            for (int i = 0; i < brackets.Length; i++)
            {
                double rate = brackets[i].Rate;
                if (rate <= 0) { continue; }   // an exempt band is an exemption, not a rate: the shift does not touch it
                double lo = brackets[i].Threshold * thresholdScale;
                double hi = i + 1 < brackets.Length ? brackets[i + 1].Threshold * thresholdScale : double.PositiveInfinity;
                double span = Math.Min(income, hi) - lo;
                if (span > 0) { tax += (rate + shiftPoints) / 100.0 * span; }
            }
            return tax;
        }

        private static double LevyTax(Statute s, double income, double thresholdScale)
        {
            if (s.LevyRate <= 0) { return 0; }
            double above = income - s.LevyThreshold * thresholdScale;
            return above > 0 ? s.LevyRate / 100.0 * above : 0;
        }

        /// <summary>§ 32a EStG: the tariff on the income in a year's euros - an indexed year's tariff is the seed's on the deflated income, scaled back
        /// (the "Tarif auf Rädern"); the uniform shift is added on the income above the Grundfreibetrag, the one exempt band.</summary>
        private static double FormulaTax(Statute s, double income, double shiftPoints, double thresholdScale)
        {
            double x = Math.Floor(income / Math.Max(1e-9, thresholdScale));   // Satz 3-5: the income rounded down to a full euro, in the seed's euros
            double tax;
            if (x <= s.FormulaGrund) { tax = 0; }
            else if (x <= s.FormulaZone2End) { double y = (x - s.FormulaGrund) / 10000.0; tax = (DeY1 * y + DeY0) * y; }
            else if (x <= s.FormulaZone3End) { double z = (x - s.FormulaZone2End) / 10000.0; tax = (DeZ1 * z + DeZ0) * z + DeZ2; }
            else if (x <= s.FormulaZone4End) { tax = DeX4 * x - DeC4; }
            else { tax = DeX5 * x - DeC5; }
            tax = Math.Max(0, tax) * thresholdScale;
            double taxable = income - s.FormulaGrund * thresholdScale;
            if (taxable > 0) { tax += shiftPoints / 100.0 * taxable; }
            return tax;
        }

        /// <summary>The marginal rate at an income, %, the statute's plus the shift where the band is taxed - the curve a row draws (15b).</summary>
        public static double MarginalRate(Statute s, double income, double shiftPoints, double thresholdScale)
        {
            const double h = 1.0;
            return 100.0 * (Tax(s, income + h, shiftPoints, thresholdScale) - Tax(s, income, shiftPoints, thresholdScale)) / h;
        }

        /// <summary>The first threshold above which the statute levies a rate above zero, in its currency at the seed's prices; 0 for a shape with none.</summary>
        public static double FirstTaxedThreshold(Statute s)
        {
            if (s.Kind == TaxScheduleKind.Formula) { return s.FormulaGrund; }
            if (s.Kind == TaxScheduleKind.TwoLayer) { return 0; }   // the municipal layer taxes the first krona
            foreach (Bracket b in s.Brackets) { if (b.Rate > 0) { return b.Threshold; } }
            return 0;
        }

        /// <summary>The state layer's threshold for Sweden (the skiktgräns) or the first taxed threshold elsewhere - the figure requirement (1) is judged against.</summary>
        public static double FirstBracketThreshold(Statute s)
        {
            foreach (Bracket b in s.Brackets) { if (b.Rate > 0) { return b.Threshold; } }
            return s.Kind == TaxScheduleKind.Formula ? s.FormulaGrund : 0;
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
        public static double AverageEffectiveRate(Country country, double shiftPoints, double incomeScale, double thresholdScale)
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
                    t += Tax(s, income, shiftPoints, thresholdScale);
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

        /// <summary>The per-country memo of yield ratios by lever position, valid for one boundary: the daily terms read the ratio every day and the
        /// AI ministry's search reads it at many lever positions, and the integration behind it is a boundary figure, not a daily one.</summary>
        public sealed class Memo
        {
            public float Level = float.NaN, Wage = float.NaN;
            public readonly Dictionary<int, double> ByRate = new Dictionary<int, double>();
            public bool Holds(float level, float wage) => Level == level && Wage == wage;
            public void Reset(float level, float wage) { Level = level; Wage = wage; ByRate.Clear(); }
        }

        /// <summary>The seeded rate the shift is measured from - captured at the seed; a save from before it is captured reads the line's rate as the seed's.</summary>
        public static float RateSeedOf(TaxLine line)
        {
            if (line.RateSeed <= 0f) { line.RateSeed = line.Rate; }
            return line.RateSeed;
        }

        /// <summary>
        /// The average effective rate the statute yields at a lever position, %, over the cohorts' incomes and the thresholds as they stand at this
        /// boundary - memoised per country and boundary (the daily terms read it every day, the AI ministry's search at many lever positions), and
        /// rounded to the float the seed yield is stored as, so the seed's own reading is the seed's figure exactly. Zero for a shape that does not respond.
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
            Memo memo = country.ScheduleMemo ?? (country.ScheduleMemo = new Memo());
            if (!memo.Holds(level, wage)) { memo.Reset(level, wage); }
            int key = (int)Math.Round(rate * 100f);
            if (memo.ByRate.TryGetValue(key, out double cached)) { return cached; }
            double shift = rate - RateSeedOf(line);
            double now = (double)(float)AverageEffectiveRate(country, shift, IncomeScale(country), ThresholdScale(country));
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

        /// <summary>The average effective rate at the headcount-weighted mean income - 15b's one figure - %, at this turn's scales and the lever's shift.</summary>
        public static double AverageEffectiveRateAtMeanIncome(Country country, TaxLine line, float rate)
        {
            if (!Responds(country.Id)) { return rate; }
            double income = AverageIncome(country, IncomeScale(country));
            if (income <= 0) { return 0; }
            return 100.0 * Tax(Of(country.Id), income, rate - RateSeedOf(line), ThresholdScale(country)) / income;
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
