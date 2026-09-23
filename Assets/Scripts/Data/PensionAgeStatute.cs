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

    /// <summary>Board 15c-r2 (Design, 2026-09-15, the D20 return's answer): what the statutory mark reads in a year - six states, three of which draw an
    /// empty track ahead of the knob and are told apart by a sentence, because ink cannot carry a reason.</summary>
    public enum PensionMarkState
    {
        /// <summary>An indexed rule whose published window ahead holds one value (Sweden's 67 from 2026 to 2032): the law's tick coincides with the knob and carries the span.</summary>
        HeldWindow,
        /// <summary>An indexed rule with published steps ahead (Italy in 2026-2027): ticks at the published values, the hairline running one pitch past the last and stopping in air.</summary>
        RisingWindow,
        /// <summary>A schedule with steps ahead and a known end: ticks at its values, the hairline closing at the track's own end-mark.</summary>
        ClosedSchedule,
        /// <summary>A schedule that has finished moving: the last step behind the knob at half ink, nothing ahead.</summary>
        Complete,
        /// <summary>An indexed rule past its horizon: the figure is the last published one, carried; nothing ahead - no tick, no stub, no "?".</summary>
        Carried,
        /// <summary>A number in the statute: no path; only a bill moves it.</summary>
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
    /// two years on ISTAT's life expectancy: 67 through 2026, 67 y 1 m in 2027, 67 y 3 m in 2028, then a date with no figure. Board 15c drew that
    /// date as a "?" tick; board 15c-r2 (2026-09-15) withdrew it - a tick is a value at a position, so the dated year without a figure is a word in
    /// the caption (<see cref="MarkSentence"/>) and the track carries only what the law has published (<see cref="MarkState"/>).</para>
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
            /// <summary>Board 15c: the citation in the form the row's trailing cell holds at 1280 (the act's short name and its article); the paragraph verbatim is the Policy Web's.</summary>
            public string Citation;
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
            { CountryId.Sweden, new Rule { Country = CountryId.Sweden, Kind = PensionAgeRule.LifeExpectancyIndexed, Paragraph = "Socialförsäkringsbalken (2010:110) 2 kap. 10 a–10 c §§ (lag 2019:649)", Citation = "SFB 2 kap. 10 a–10 c",
                Headline = 67f, Path = new[] { new PathPoint(2026, 67f) }, DatedTo = 2026 + RiktalderYearsAhead, SourceFile = "se_sfb_2010_110.html · se_pensionsmyndigheten_riktalder.html" } },
            { CountryId.Germany, new Rule { Country = CountryId.Germany, Kind = PensionAgeRule.Scheduled, Paragraph = "SGB VI § 35 · § 235 Abs. 2", Citation = "SGB VI 35 · 235 Abs. 2",
                Headline = 67f, Path = new[] { new PathPoint(2026, Months(66, 4)), new PathPoint(2027, Months(66, 6)), new PathPoint(2028, Months(66, 8)), new PathPoint(2029, Months(66, 10)), new PathPoint(2031, 67f) }, DatedTo = int.MaxValue, SourceFile = "de_sgb6_35.html · de_sgb6_235.html" } },
            { CountryId.France, new Rule { Country = CountryId.France, Kind = PensionAgeRule.Scheduled, Paragraph = "code de la sécurité sociale L161-17-2 (loi n° 2023-270)", Citation = "CSS L161-17-2",
                Headline = 64f, Path = new[] { new PathPoint(2026, Months(62, 9)), new PathPoint(2028, 63f), new PathPoint(2029, Months(63, 3)), new PathPoint(2030, Months(63, 6)), new PathPoint(2031, Months(63, 9)), new PathPoint(2033, 64f) }, DatedTo = int.MaxValue, SourceFile = "fr_service_public_F14043.html" } },
            { CountryId.Italy, new Rule { Country = CountryId.Italy, Kind = PensionAgeRule.LifeExpectancyIndexed, Paragraph = "decreto-legge 201/2011 art. 24 (the ISTAT adjustment every two years)", Citation = "DL 201/2011 art. 24",
                Headline = 67f, Path = new[] { new PathPoint(2026, 67f), new PathPoint(2027, Months(67, 1)), new PathPoint(2028, Months(67, 3)) }, DatedTo = 2028, SourceFile = "it_inps_2027_2028.html" } },
            { CountryId.Poland, new Rule { Country = CountryId.Poland, Kind = PensionAgeRule.Fixed, Paragraph = "ustawa o emeryturach i rentach z FUS art. 24 ust. 1 (65 for men; the women's 60 is the deviation the one-age model states)", Citation = "FUS art. 24 ust. 1",
                Headline = 65f, Path = new[] { new PathPoint(2026, 65f) }, DatedTo = int.MaxValue, SourceFile = "pl_arslege_art24.html" } },
            { CountryId.USA, new Rule { Country = CountryId.USA, Kind = PensionAgeRule.Scheduled, Paragraph = "Social Security Act § 216(l), 42 U.S.C. 416(l)(1)", Citation = "SSA 216(l)",
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

        /// <summary>PN-1's dial (§590, DS-3): the age in force for THIS country - a passed bill's figure where one stands
        /// (<see cref="Country.PensionAgeOverride"/>), else the statute's for the year. Every reader of the model's age reads this one.</summary>
        public static float AgeInForce(Country country, int year) => country.PensionAgeOverride >= 0f ? country.PensionAgeOverride : AgeInForce(country.Id, year);

        /// <summary>Whether a passed bill's figure stands in place of the statute's (§590).</summary>
        public static bool IsOverridden(Country country) => country.PensionAgeOverride >= 0f;

        /// <summary>
        /// The year the statute has dated without a figure - board 15c's third mark, a word in the caption since 15c-r2 (never a tick): for an indexed rule, the first year its statute has published no figure for, and never a
        /// year already past - the year after the horizon while the calendar is inside it, THIS year once the calendar has passed it (the age in
        /// force is then the last published figure, carried, and <see cref="IsDated"/> says so). Null for a schedule or a fixed rule, whose
        /// every year is known.
        /// </summary>
        public static int? UndatedMarkYear(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null || r.Kind != PensionAgeRule.LifeExpectancyIndexed) { return null; }
            return Math.Max(r.DatedTo + 1, year);
        }

        /// <summary>The next point on the path after a year, or null where the path has no later point (a schedule at its end, a fixed rule, an indexed rule at its horizon).</summary>
        public static PathPoint? NextStep(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null) { return null; }
            foreach (PathPoint p in r.Path) { if (p.Year > year) { return p; } }
            return null;
        }

        /// <summary>
        /// Board 15c-r2 (Design, 2026-09-15): the state the statutory mark reads in a calendar year. *"The track carries figures. The caption carries
        /// years. A tick is a value at a position"* - so a year the law has dated without calculating its figure never gets a tick; it gets a word.
        /// An indexed rule past its horizon is CARRIED; inside it, RISING where a published step lies ahead and HELD where the window holds one
        /// value; a schedule is CLOSED while a step lies ahead and COMPLETE after its last; a fixed rule is FIXED.
        /// </summary>
        public static PensionMarkState MarkState(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null || r.Kind == PensionAgeRule.Fixed) { return PensionMarkState.Fixed; }
            bool ahead = TicksAhead(id, year).Length > 0;
            if (r.Kind == PensionAgeRule.LifeExpectancyIndexed)
            {
                if (year > r.DatedTo) { return PensionMarkState.Carried; }
                return ahead ? PensionMarkState.RisingWindow : PensionMarkState.HeldWindow;
            }
            return ahead ? PensionMarkState.ClosedSchedule : PensionMarkState.Complete;
        }

        /// <summary>Board 15c-r2: the ticks ahead of the knob - the path's PUBLISHED values after this year and never a year past the horizon, so a
        /// dated year without a figure draws nothing (15c's "2033 ?" and "? · 2029" ticks are withdrawn).</summary>
        public static PathPoint[] TicksAhead(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null || r.Kind == PensionAgeRule.Fixed) { return new PathPoint[0]; }
            var ahead = new List<PathPoint>();
            foreach (PathPoint p in r.Path) { if (p.Year > year && p.Year <= r.DatedTo) { ahead.Add(p); } }
            return ahead.ToArray();
        }

        /// <summary>Board 15c-r2: the year the figure in force took effect on the path (the path's first year where it starts later) - a held window's span opens here.</summary>
        public static int InForceSince(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null || r.Path.Length == 0) { return year; }
            int since = r.Path[0].Year;
            foreach (PathPoint p in r.Path) { if (p.Year <= year) { since = p.Year; } }
            return since;
        }

        /// <summary>Board 15c-r2: HALF INK = HISTORY - the step before the figure in force, drawn behind the knob in the two states whose track is otherwise
        /// empty for a reason the path explains (COMPLETE, CARRIED); null where the path has no earlier step or the state draws none.</summary>
        public static PathPoint? HistoryTick(CountryId id, int year)
        {
            PensionMarkState state = MarkState(id, year);
            if (state != PensionMarkState.Complete && state != PensionMarkState.Carried) { return null; }
            Rule r = Of(id);
            PathPoint? previous = null, current = null;
            foreach (PathPoint p in r.Path) { if (p.Year <= year) { previous = current; current = p; } }
            return previous;
        }

        /// <summary>Board 15c-r2: the figure cell's second line (9b's line under the figure) - where the figure comes from. CARRIED FROM the horizon for a
        /// carried figure; THE STATUTE's OWN otherwise (the knob is the law's figure - and since §520 the pension line's driver reads it; a bill's figure, the dial, is still open).</summary>
        public static string FigureProvenance(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null) { return null; }
            return MarkState(id, year) == PensionMarkState.Carried
                ? "CARRIED FROM " + r.DatedTo.ToString(CultureInfo.InvariantCulture)
                : "THE STATUTE's OWN";
        }

        /// <summary>Board 15c-r2's flag, answered on the row as built: France's statute (L161-17-2) writes its table by BIRTH YEAR, and the row's tick
        /// years are the years each cohort reaches its age (born b at a: in force from b + a). The caption names that axis once; a narrow band drops
        /// other segments before it.</summary>
        public const string AxisNote = "BY BIRTH YEAR, YEARS IN FORCE";

        /// <summary>
        /// Board 15c-r2: the caption band's NEXT sentence for the state, as the board writes it - the years the track may not carry - in segments
        /// joined by " · ", each with the RANK it keeps on a narrow band: 1 is the state's own answer and is never dropped (2033 NO FIGURE YET, 2029 NOT
        /// PUBLISHED, SCHEDULE COMPLETE 2031, THE LAW DOES NOT MOVE IT, France's axis), 2 the empty NEXT figure's dash and the window's reach, 3 what
        /// the track's label or the answer already says (the next figure and its year, the carried and standing restatements). The first film dropped from the end and cut Sweden's 2033 NO FIGURE YET - the answer itself.
        /// </summary>
        public static (string Text, int Rank)[] MarkSegments(CountryId id, int year)
        {
            Rule r = Of(id);
            if (r == null) { return new (string, int)[0]; }
            string Y(int y) => y.ToString(CultureInfo.InvariantCulture);
            float age = AgeInForce(id, year);
            PathPoint[] ahead = TicksAhead(id, year);
            PathPoint end = r.Path[r.Path.Length - 1];
            switch (MarkState(id, year))
            {
                case PensionMarkState.HeldWindow:
                    return new[] { (Format(age) + " HELD TO " + Y(r.DatedTo), 2), (Y(UndatedMarkYear(id, year).Value) + " NO FIGURE YET", 1) };
                case PensionMarkState.RisingWindow:
                    return new[] { (Format(ahead[0].Age) + " · " + Y(ahead[0].Year), 3), ("PUBLISHED TO " + Y(r.DatedTo), 2), (Y(UndatedMarkYear(id, year).Value) + " NO FIGURE YET", 1) };
                case PensionMarkState.ClosedSchedule:
                    return id == CountryId.France
                        ? new[] { (Format(ahead[0].Age) + " · " + Y(ahead[0].Year), 3), ("SCHEDULE ENDS AT " + Format(end.Age), 2), (AxisNote, 1) }
                        : new[] { (Format(ahead[0].Age) + " · " + Y(ahead[0].Year), 3), ("SCHEDULE ENDS AT " + Format(end.Age), 1) };
                case PensionMarkState.Complete:
                    return new[] { ("—", 2), ("SCHEDULE COMPLETE " + Y(end.Year), 1), ("THE STANDING AGE IS ALREADY " + Format(end.Age), 3) };
                case PensionMarkState.Carried:
                    return new[] { ("—", 2), (Y(UndatedMarkYear(id, year).Value) + " NOT PUBLISHED", 1), ("THE KNOB CARRIES " + Y(r.DatedTo) + "'s FIGURE", 3) };
                default:
                    return new[] { ("—", 2), ("THE LAW DOES NOT MOVE IT", 1), ("ONLY A BILL MOVES THIS DIAL", 3) };
            }
        }

        /// <summary>Board 15c-r2: the whole sentence, every segment - what the band says where it has the room.</summary>
        public static string MarkSentence(CountryId id, int year) => BandSentence(id, year, int.MaxValue, false);

        /// <summary>Board 15c-r2: the band's sentence at one rung - the segments ranked at or under <paramref name="maxRank"/>, in the board's order, behind
        /// the word NEXT where <paramref name="next"/> asks for it. France's axis note says what the ticks' years count, not what comes next, so standing
        /// alone it takes no NEXT (the fifth film printed "NEXT: BY BIRTH YEAR, YEARS IN FORCE").</summary>
        public static string BandSentence(CountryId id, int year, int maxRank, bool next)
        {
            var parts = new List<string>();
            foreach ((string text, int rank) in MarkSegments(id, year)) { if (rank <= maxRank) { parts.Add(text); } }
            bool axisAlone = parts.Count == 1 && parts[0] == AxisNote;
            return (next && !axisAlone ? "NEXT: " : "") + string.Join(" · ", parts);
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
