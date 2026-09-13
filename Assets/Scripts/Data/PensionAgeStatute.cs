using System;
using System.Collections.Generic;
using System.Globalization;

namespace PoliSim.Data
{
    /// <summary>How a country's statutory pension age moves from one year to the next when nobody moves it - what the statute does to the figure.</summary>
    public enum PensionAgeRule
    {
        /// <summary>The age is recalculated from life expectancy by a rule in the statute and published a set number of years ahead: the path is DATED for
        /// those years and unknown beyond them (Sweden's riktålder, Italy's ISTAT adjustment) - the indexed class is two countries.</summary>
        LifeExpectancyIndexed,
        /// <summary>The age follows a table the statute sets by birth year; the year it reaches its end is known, and after it the figure is flat (Germany, France, the USA).</summary>
        Scheduled,
        /// <summary>The age is a number in the statute and only a bill moves it (Poland).</summary>
        Fixed,
    }

    /// <summary>
    /// PN-1 (2026-09-13; DS-3 ruled §474 "statute + dial, on CarbonRateStatute's precedent: one rule per country with its paragraph, stepped at
    /// the boundary, a passed bill's figure wins"): THE STATUTE'S OWN MOVE OF THE PENSION AGE, one rule per country, each with its paragraph and
    /// its dated path, read verbatim from `ElectionsData/pensions/statutory_ages.csv` (S1, §475) and the files beside it. **This pass builds the
    /// statute layer and verifies it against the CSV; the pensions DRIVER - the age reaching the headcount, the participation rates of the bands
    /// it crosses, the dependency ratio, potential's window - is the BASELINE half and is deferred by ruling (2026-09-13), so nothing in the
    /// model reads this figure yet: the Policy Web's Social Security node prints the sentence, and board 15c draws the path.
    ///
    /// <para><b>The age in force in a year.</b> A scheduled statute writes its table by BIRTH YEAR; the age in force in a calendar year is the age
    /// of the cohort whose retirement falls in that year (born b, retiring at a: in force from b + a), so Germany's § 235 Abs. 2 - 66 years 4
    /// months for 1960, 6 for 1961, 8 for 1962, 10 for 1963, 67 from 1964 (the table quoted from the file) - reads 66 y 4 m in 2026 and 67 from
    /// 2031; France's L161-17-2 - 62 y 9 m for those born 1963 to March 1965, 63 for April to December 1965, then three months a birth year to 64
    /// from 1969 - reads 62 y 9 m in 2026 and 64 from 2033; the USA's 416(l)(1) - 66 y 10 m for those born 1959 (the last of the two-months-a-year
    /// cohorts), 67 from 1960 - reads 66 y 10 m in 2026 and 67 from 2027.</para>
    ///
    /// <para><b>An indexed rule has a SHORT DATED PATH.</b> Socialförsäkringsbalken 2 kap. 10 c § makes the riktålder computed in a year apply
    /// "det sjätte året efter" - it is in force the sixth year after its calculation - so the figure is known six years out and no further:
    /// Pensionsmyndigheten publishes 67 for 2026 through 2032, and 2033 is a date with no figure. Italy's art. 24 DL 201/2011 adjusts every
    /// two years on ISTAT's life expectancy: 67 through 2026, 67 y 1 m in 2027, 67 y 3 m in 2028, then a date with no figure. That is what
    /// sharpens board 15c's third mark: an indexed path's later ticks carry a year and a "?" - the year is known, the figure is not.</para>
    /// </summary>
    public static class PensionAgeStatute
    {
        /// <summary>A point on the dated path: the age in force from this calendar year, in years (a month is a twelfth).</summary>
        public readonly struct PathPoint
        {
            public readonly int Year;
            public readonly float Age;
            public PathPoint(int year, float age) { Year = year; Age = age; }
        }

        /// <summary>One country's statute as the model reads it.</summary>
        public sealed class Rule
        {
            public CountryId Country;
            public PensionAgeRule Kind;
            /// <summary>The paragraph, as the register cites it.</summary>
            public string Paragraph;
            /// <summary>The statute's headline figure - the CSV's `current_age_years` (a schedule's end, an indexed rule's current figure, a fixed rule's number).</summary>
            public float Headline;
            /// <summary>The dated path from the seed year: the age in force from each year it changes; the last point holds until <see cref="DatedTo"/>.</summary>
            public PathPoint[] Path;
            /// <summary>The last calendar year the path is KNOWN for: an indexed rule's publication horizon; int.MaxValue for a schedule (flat after its end) and a fixed rule.</summary>
            public int DatedTo;
            /// <summary>The source file beside the CSV.</summary>
            public string SourceFile;
        }

        /// <summary>The model's first calendar year - the game's epoch (SimulationManager.EpochDate, 2026-01-01); the CSV was fetched 2026-09-12 and its figures are that year's.</summary>
        public const int SeedYear = 2026;

        /// <summary>SFB 2 kap. 10 c §: the riktålder computed in a year is in force the sixth year after it - the horizon a Swedish path is dated to.</summary>
        public const int RiktalderYearsAhead = 6;

        private static float Months(int years, int months) => years + months / 12f;

        private static readonly Dictionary<CountryId, Rule> Rules = new Dictionary<CountryId, Rule>
        {
            { CountryId.Sweden, new Rule { Country = CountryId.Sweden, Kind = PensionAgeRule.LifeExpectancyIndexed, Paragraph = "Socialförsäkringsbalken (2010:110) 2 kap. 10 a–10 c §§ (lag 2019:649)",
                Headline = 67f, Path = new[] { new PathPoint(2026, 67f) }, DatedTo = 2026 + RiktalderYearsAhead, SourceFile = "se_sfb_2010_110.html · se_pensionsmyndigheten_riktalder.html" } },
            { CountryId.Germany, new Rule { Country = CountryId.Germany, Kind = PensionAgeRule.Scheduled, Paragraph = "SGB VI § 35 · § 235 Abs. 2",
                Headline = 67f, Path = new[] { new PathPoint(2026, Months(66, 4)), new PathPoint(2027, Months(66, 6)), new PathPoint(2028, Months(66, 8)), new PathPoint(2029, Months(66, 10)), new PathPoint(2031, 67f) }, DatedTo = int.MaxValue, SourceFile = "de_sgb6_35.html · de_sgb6_235.html" } },
            { CountryId.France, new Rule { Country = CountryId.France, Kind = PensionAgeRule.Scheduled, Paragraph = "code de la sécurité sociale L161-17-2 (loi n° 2023-270)",
                Headline = 64f, Path = new[] { new PathPoint(2026, Months(62, 9)), new PathPoint(2028, 63f), new PathPoint(2029, Months(63, 3)), new PathPoint(2030, Months(63, 6)), new PathPoint(2031, Months(63, 9)), new PathPoint(2033, 64f) }, DatedTo = int.MaxValue, SourceFile = "fr_service_public_F14043.html" } },
            { CountryId.Italy, new Rule { Country = CountryId.Italy, Kind = PensionAgeRule.LifeExpectancyIndexed, Paragraph = "decreto-legge 201/2011 art. 24 (the ISTAT adjustment every two years)",
                Headline = 67f, Path = new[] { new PathPoint(2026, 67f), new PathPoint(2027, Months(67, 1)), new PathPoint(2028, Months(67, 3)) }, DatedTo = 2028, SourceFile = "it_inps_2027_2028.html" } },
            { CountryId.Poland, new Rule { Country = CountryId.Poland, Kind = PensionAgeRule.Fixed, Paragraph = "ustawa o emeryturach i rentach z FUS art. 24 ust. 1 (65 for men; the women's 60 is the deviation the one-age model states)",
                Headline = 65f, Path = new[] { new PathPoint(2026, 65f) }, DatedTo = int.MaxValue, SourceFile = "pl_arslege_art24.html" } },
            { CountryId.USA, new Rule { Country = CountryId.USA, Kind = PensionAgeRule.Scheduled, Paragraph = "Social Security Act § 216(l), 42 U.S.C. 416(l)(1)",
                Headline = 67f, Path = new[] { new PathPoint(2026, Months(66, 10)), new PathPoint(2027, 67f) }, DatedTo = int.MaxValue, SourceFile = "us_42usc416.html" } },
        };

        public static Rule Of(CountryId id) => Rules.TryGetValue(id, out Rule r) ? r : null;

        /// <summary>Whether the country's statute is on file (the six are).</summary>
        public static bool Has(CountryId id) => Rules.ContainsKey(id);

        /// <summary>Whether the figure for a year is KNOWN - inside an indexed rule's publication horizon, or any year of a schedule or a fixed rule.</summary>
        public static bool IsDated(CountryId id, int year) { Rule r = Of(id); return r != null && year <= r.DatedTo; }

        /// <summary>The age in force in a calendar year: the last path point at or before the year (the first point before the path starts); an
        /// indexed rule beyond its horizon holds its last published figure and <see cref="IsDated"/> says it is not known.</summary>
        public static float AgeInForce(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null || r.Path == null || r.Path.Length == 0) { return 0f; }
            float age = r.Path[0].Age;
            foreach (PathPoint p in r.Path) { if (p.Year <= year) { age = p.Age; } }
            return age;
        }

        /// <summary>The next point on the path after a year, or null where the path has no later point (a schedule at its end, a fixed rule, an indexed rule at its horizon).</summary>
        public static PathPoint? NextStep(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null) { return null; }
            foreach (PathPoint p in r.Path) { if (p.Year > year) { return p; } }
            return null;
        }

        /// <summary>An age in years and months, as the statutes write it - "66 y 10 m", "67".</summary>
        public static string Format(float age)
        {
            int years = (int)Math.Floor(age + 1e-6f);
            int months = (int)Math.Round((age - years) * 12f);
            if (months >= 12) { years++; months = 0; }
            return months == 0 ? years.ToString(CultureInfo.InvariantCulture) : string.Format(CultureInfo.InvariantCulture, "{0} y {1} m", years, months);
        }

        /// <summary>The rule's sentence for a surface - the kind, the paragraph, the path's shape; the carbon caption's precedent.</summary>
        public static string Caption(CountryId id)
        {
            Rule r = Of(id);
            if (r == null) { return "No statute on file."; }
            switch (r.Kind)
            {
                case PensionAgeRule.LifeExpectancyIndexed:
                    return id == CountryId.Sweden
                        ? string.Format(CultureInfo.InvariantCulture, "Recalculated from life expectancy under {0} - 65 plus two thirds of the gain at 65 since 1994, rounded to whole years - and in force the sixth year after its calculation, so the figure is published six years out: {1} through {2}, then a date with no figure.", r.Paragraph, Format(r.Headline), r.DatedTo)
                        : string.Format(CultureInfo.InvariantCulture, "Adjusted every two years on ISTAT's life expectancy under {0}: {1} through 2026, {2} in 2027, {3} in 2028, then a date with no figure.", r.Paragraph, Format(r.Path[0].Age), Format(r.Path[1].Age), Format(r.Path[2].Age));
                case PensionAgeRule.Scheduled:
                    return string.Format(CultureInfo.InvariantCulture, "A schedule by birth year under {0}: {1} in force in {2}, {3} from {4}, then flat.", r.Paragraph, Format(r.Path[0].Age), r.Path[0].Year, Format(r.Path[r.Path.Length - 1].Age), r.Path[r.Path.Length - 1].Year);
                default:
                    return string.Format(CultureInfo.InvariantCulture, "A number in the statute, {0}: {1}; only a bill moves it.", r.Paragraph, Format(r.Headline));
            }
        }
    }
}
