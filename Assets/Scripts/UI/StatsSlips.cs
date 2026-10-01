using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>One chart of the Statistics sheet: what it reads, how its change prints, which way is better, and the seed value it holds until it goes live.</summary>
    public sealed class StatsChart
    {
        public readonly string Id;
        public readonly string Title;
        public readonly ReadingUnit Unit;
        public readonly bool? HigherIsBetter;
        /// <summary>23a ⑭: the seed's round default the series holds until its formula first runs at a year's close, or null.</summary>
        public readonly float? HeldSeed;
        public readonly Func<StatHistory, MultiResolutionSeries> Series;
        public readonly StatNodeId? MoneyStat;

        public StatsChart(string id, string title, ReadingUnit unit, bool? higherIsBetter, float? heldSeed, Func<StatHistory, MultiResolutionSeries> series, StatNodeId? moneyStat = null)
        {
            Id = id; Title = title; Unit = unit; HigherIsBetter = higherIsBetter; HeldSeed = heldSeed; Series = series; MoneyStat = moneyStat;
        }

        public MoneyUnit? Money => MoneyStat.HasValue ? PolicyWebRenderer.GetStatUnit(MoneyStat.Value) : (MoneyUnit?)null;
    }

    /// <summary>
    /// D-ST (boards 23a-23c, 2026-09-30; 19b's form): **THE STATISTICS SHEET'S SLIPS, BUILT FROM THE MODEL AND NOTHING ELSE.** Every class of word the
    /// sheet took off the page at rest - a subtitle, a head's tail, an axis's arithmetic, a pager's legend, a Δ's definition, a unit phrase, a
    /// sentence explaining an absence - is on a slip here at its own anchor, the built screen's words where it had them. The page draws from this
    /// book and `StatsSlipReachabilityCheck` reads the same book, so the check cannot pass on a slip the page does not open.
    /// </summary>
    public static class StatsSlips
    {
        /// <summary>The seed's approval, held until the first year's close (WorldFactory seeds every country's approvalRating at 50; MacroSystem's
        /// neutral rating is the same 50).</summary>
        public const float ApprovalSeed = 50f;   // SOURCED: WorldFactory.CreateDefault - approvalRating: 50f on every country's EconomyState

        /// <summary>The trade balance's seed, held until the first year's close runs TradeSystem.ApplyTradeEffects.</summary>
        public const float TradeBalanceSeed = 0f;   // SOURCED: EconomyState's tradeBalance seed; TradeSystem writes it first at the boundary

        public static readonly StatsChart[] Domestic =
        {
            new StatsChart("gdp", "Real GDP", ReadingUnit.Money, true, null, h => h.Gdp, StatNodeId.Gdp),
            new StatsChart("unemployment", "Unemployment", ReadingUnit.Percent, false, null, h => h.Unemployment),
            new StatsChart("inflation", "Inflation", ReadingUnit.Percent, false, null, h => h.Inflation),
            new StatsChart("approval", "Approval rating", ReadingUnit.Score, true, ApprovalSeed, h => h.ApprovalRating),
            new StatsChart("poverty", "Poverty rate", ReadingUnit.Percent, false, null, h => h.PovertyRate),
            new StatsChart("debt", "Debt-to-GDP", ReadingUnit.Percent, false, null, h => h.DebtToGdpRatio),
        };

        public static readonly StatsChart Trade = new StatsChart("trade", "Trade balance · goods and services", ReadingUnit.Money, true, TradeBalanceSeed, h => h.TradeBalance, StatNodeId.TradeBalance);

        /// <summary>The ten headline readings that keep no history (their sparkline slot is ABSENT).</summary>
        public static bool KeepsNoHistory(string label) => label == "Currency Strength" || label == "Government Debt" || label == "Credit Rating";

        private static string P1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>The book for the sheet as it stands: <paramref name="partner"/> the pair page's partner (null with no other country),
        /// <paramref name="seriesPage"/> and <paramref name="tradePage"/> the two sections' pages, <paramref name="passThroughPp"/> the last closed
        /// year's tariff pass-through (null before one closed).</summary>
        public static PeopleSlips.Book Build(Country home, World world, Country partner, int seriesPage, int tradePage, float? passThroughPp)
        {
            var book = new PeopleSlips.Book();
            StatHistory history = home.History;

            // ---- the title (23a ①, 23b ①) ---------------------------------------------------------------------------------------------------
            book.Anchors["title"] = new SlipContent("STATISTICS")
                .Add("DOMESTIC BULLETIN — DESK READINGS, LIVE")
                .Add("INTERNATIONAL BULLETIN — WORLD READINGS, LIVE");

            // ---- the cards (23a ③ ④) --------------------------------------------------------------------------------------------------------
            float? growth = StatsReadings.YearOnYearGrowthPercent(history?.Gdp.Quarterly);
            book.Anchors["card:gdp/growth"] = growth.HasValue
                ? new SlipContent("GDP GROWTH").Add("REAL · THE LAST FOUR QUARTERS · " + StatsReadings.TrueMinus(growth.Value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)) + "%")
                    .Add("READ OFF THE KEPT SERIES, NOT THE CARD'S NOMINAL FIGURE")
                : new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("GDP GROWTH · NO FIGURE HELD").Add("NEVER 0.00, NEVER A DASH")
                    .Add("A YEAR OF THE KEPT SERIES IS NEEDED - FIVE QUARTERLY POINTS");
            book.Anchors["card:history"] = new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("HISTORY · NONE KEPT").Add("THE FIGURE ABOVE IS LIVE");

            // ---- the fiscal position (23a ⑥ ⑦) --------------------------------------------------------------------------------------------
            book.Anchors["fiscal:head"] = new SlipContent("FISCAL POSITION").Add("SHARES OF GDP · ONE AXIS TO 30% OR MORE")
                .Add("A DEFICIT ROW IS NAMED BY ITS SIGN - A SURPLUS READS SURPLUS");
            book.Anchors["fiscal:axis"] = new SlipContent("% OF GDP").Add("SHARES OF GDP · ONE AXIS TO ITS END LABEL").Add("A TICK EVERY 10% OF GDP")
                .Add("THE AXIS ENDS AT THE LARGEST SHARE, ROUNDED UP TO THE NEXT 10%, NEVER BELOW 30%");
            book.Anchors["fiscal:percapita"] = new SlipContent("GDP PER CAPITA").Add("LEVEL · NO GAUGE").Add("CURRENCY PER PERSON - NOT A SHARE OF GDP");
            book.Anchors["fiscal:notyet"] = new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("NOT YET COMPUTED — ADVANCE A YEAR")
                .Add("A SHARE OF A YEAR NO YEAR HAS CLOSED");

            // ---- the sectors (23a ⑧) --------------------------------------------------------------------------------------------------------
            var shares = new List<float>();
            foreach (Sector s in home.Sectors) { shares.Add(s.OutputShareOfGdp); }
            float other = StatsReadings.SectorRemainderPercent(shares);
            book.Anchors["sector:head"] = new SlipContent("SECTOR SHARES").Add("SECTOR SHARES OF GDP — ONE DISTRIBUTION").Add("EACH SEGMENT'S LENGTH IS ITS SHARE OF GDP");
            book.Anchors["sector:other"] = new SlipContent("OTHER").Add(P1(other) + " % OF GDP IN NO SECTOR").Add("100 − THE EIGHT")
                .Add("SECTOR SHARES OF GDP — ONE DISTRIBUTION").Add("THE PUBLIC SECTOR, PROPERTY, HEALTH, EDUCATION, SERVICES - NO SECTOR OF THEIR OWN HERE");

            // ---- the live series (23a ⑨-⑭) ------------------------------------------------------------------------------------------------
            book.Anchors["series:head"] = new SlipContent("LIVE SERIES").Add("DASHED = NEXT-YEAR ESTIMATE WHERE ONE EXISTS").Add("TICKS ABOVE = LAWS ENACTED")
                .Add("A DASHED LINE UNDER A SERIES IS THE NO-POLICY COUNTERFACTUAL").Add("DOTTED = ZERO, WHERE A SCALE SPANS IT");
            int pages = 1;
            foreach (StatsChart c in Domestic) { pages = Math.Max(pages, GraphRenderer.PagesFor(history == null ? null : c.Series(history).Quarterly)); }
            book.Anchors["series:pager"] = Pager(pages, seriesPage, history?.Gdp, "THE SIX CHARTS MOVE TOGETHER");
            foreach (StatsChart c in Domestic) { AddChart(book, c, history == null ? null : c.Series(history), seriesPage); }
            book.Anchors["chart:gdp/name"] = new SlipContent("REAL GDP").Add("AT THE PRICES THE GAME OPENED ON").Add("THE CARD ABOVE IS NOMINAL - IN TODAY'S PRICES");
            book.Anchors["chart:unemployment/nairu"] = new SlipContent("NAIRU").Add("THE RATE UNEMPLOYMENT SETTLES AT WITH INFLATION STEADY").Add("THE MODEL'S OWN, READ ON THE LABOUR FORCE TODAY");
            book.Anchors["chart:debt/comfortable"] = new SlipContent("COMFORTABLE").Add("THE DEBT RATIO THIS COUNTRY CARRIES WITHOUT A RATING'S PRESSURE");

            // ---- your policies (23a ⑯) ------------------------------------------------------------------------------------------------------
            book.Anchors["policies:head"] = new SlipContent("YOUR POLICIES").Add("THE GAP FROM THE NO-POLICY COUNTERFACTUAL, AND WHAT OPENED IT");
            book.Anchors["policies:nil"] = new SlipContent("NIL").Add("NO DIAL MOVED YET").Add("THE LIVE SERIES AND THE COUNTERFACTUAL ARE ONE RUN")
                .Add("THE GAP APPEARS HERE WITH ITS REASONS");

            // ---- society (23a ⑰) ------------------------------------------------------------------------------------------------------------
            book.Anchors["society:head"] = new SlipContent("SOCIETY").Add("SHARES AS GAUGES · INDICES AND LEVELS WITH THEIR KEPT HISTORIES");
            bool overburden = home.TracksHousingOverburden;
            AddSociety(book, "Youth unemployment", "OF YOUTH LABOR FORCE");
            AddSociety(book, "Life expectancy", "YEARS AT BIRTH");
            AddSociety(book, "Income inequality (Gini)", "0–100 SCALE");
            AddSociety(book, "Real wages", "INDEX · 100 = TERM START");
            AddSociety(book, "Productivity", "$/HOUR (PPP) · OWN PAST");
            AddSociety(book, "Housing overburden", overburden ? ">40% OF INCOME ON HOUSING" : "ABSENT BY RULING · NOT ZERO");
            AddSociety(book, "Homeownership", overburden ? "OF HOUSEHOLDS" : "OF HOUSEHOLDS · PRIMARY");
            AddSociety(book, "House prices", "INDEX · 100 = TERM START");
            book.Anchors["society:index"] = new SlipContent("INDEX").Add("100 = TERM START").Add("THE DOTTED RULE ON ITS LINE");

            // ---- International (23b) ---------------------------------------------------------------------------------------------------------
            book.Anchors["map"] = new SlipContent("WORLD MAP").Add("THE SIX · MARKERS ARE COUNTRIES").Add("DOTS ARE EVENTS OF THE LAST FEW YEARS,")
                .Add(SymbolRegistry.Word(Symbol.Good) + " HELPED AND " + SymbolRegistry.Word(Symbol.Bad) + " HURT,").Add("SIZED BY THE SHOCK AND FADING WITH IT");
            // §710 (Elias's ruling: R-SP5 retired on every map - the chip's two-letter code is the label, the name first in the chip's tooltip): each chip's
            // slip, the country's name its head, then what the map's own hover box had carried
            foreach (Country c in world.Countries)
            {
                book.Anchors["map:chip:" + c.Id] = new SlipContent(c.Name.ToUpperInvariant())
                    .Add("GDP " + UiFormat.Money(c.State.NominalGdp, MoneyUnit.Billions) + " · UNEMPLOYMENT " + StatsReadings.Rate(c.State.Unemployment))
                    .Add("APPROVAL " + c.State.ApprovalRating.ToString("0.0", CultureInfo.InvariantCulture) + (c.Id == home.Id ? " · HOME" : string.Empty));
            }
            book.Anchors["pair:head"] = new SlipContent("PAIR").Add("PAIR PAGE · THE MODEL'S OWN LINKS").Add("A CODE IS A PARTNER - A CLICK ON ONE IS THE STEP");
            foreach (Country c in world.Countries)
            {
                string zone = c.CurrencyZone != null ? c.CurrencyZone.Name.ToUpperInvariant() : "—";
                book.Anchors["pair:code:" + c.Id] = new SlipContent(c.Name.ToUpperInvariant())
                    .Add(c.Id == home.Id ? "HOME" : partner != null && c.Id == partner.Id ? "PARTNER" : "A PARTNER - A CLICK SHOWS IT").Add(zone + " · " + BlocOf(world, c.Id));
            }
            book.Anchors["pair:home"] = new SlipContent(home.Name.ToUpperInvariant()).Add("HOME").Add("THE LEFT SIDE OF EVERY ROW");
            if (partner != null) { book.Anchors["pair:partner"] = new SlipContent(partner.Name.ToUpperInvariant()).Add("PARTNER").Add("THE RIGHT SIDE OF EVERY ROW"); }
            book.Anchors["readings:head"] = new SlipContent("READINGS").Add("THE HEADLINE READINGS · THE SAME ROWS EACH SIDE").Add("BOTH SIDES · LIVE");
            book.Anchors["links:head"] = new SlipContent("LINKS").Add("THE PAIR · TRADE FROM THE MAP'S OWN LINKS").Add("EACH SIDE'S FACT ON ITS OWN SIDE");
            if (partner != null)
            {
                string mine = home.Name.ToUpperInvariant(), theirs = partner.Name.ToUpperInvariant();
                book.Anchors["links:out"] = new SlipContent(mine + " → " + theirs).Add("WHAT " + mine + " SELLS TO " + theirs).Add("LENGTH RELATIVE TO THE LARGER OF THE TWO");
                book.Anchors["links:in"] = new SlipContent(theirs + " → " + mine).Add("WHAT " + theirs + " SELLS TO " + mine).Add("LENGTH RELATIVE TO THE LARGER OF THE TWO");
                book.Anchors["links:none"] = new SlipContent("NO TRADE LINK").Add("THE MAP HOLDS NO TRADE LINK BETWEEN THESE TWO.")
                    .Add("THIS IS NOT TRADE OF ZERO — NO VOLUME EXISTS TO BE ZERO.").Add("TARIFFS STILL READ: EACH SIDE'S RATE IS A FACT ABOUT THAT SIDE.");
                book.Anchors["links:zero"] = new SlipContent("TRADE OF ZERO").Add("(TRADE OF ZERO, THIS PERIOD) — A LINK EXISTS AND CARRIED NOTHING")
                    .Add("THE ARROWS DRAW AT THEIR MINIMUM, THE FIGURE READS 0");
                book.Anchors["links:tariff"] = new SlipContent("TARIFF").Add("TARIFF " + mine + " CHARGES - ON THE LEFT").Add("TARIFF " + theirs + " CHARGES - ON THE RIGHT")
                    .Add("EACH SIDE'S TARIFF ON THE OTHER'S GOODS");
                string shared = null;
                foreach (TradeBloc b in world.TradeBlocs) { if (b.IsMember(home.Id) && b.IsMember(partner.Id)) { shared = b.Name.ToUpperInvariant(); break; } }
                book.Anchors["links:bloc"] = new SlipContent("BLOC").Add("SHARED BLOC · " + (shared ?? "NONE")).Add("— = NIL: NO BLOC ON THAT SIDE");
                bool same = home.CurrencyZone == partner.CurrencyZone;
                book.Anchors["links:currency"] = new SlipContent("CURRENCY").Add(same
                    ? "SHARED CURRENCY · YES — " + home.CurrencyZone.Name.ToUpperInvariant()
                    : "SHARED CURRENCY · NO — " + home.CurrencyZone.Name.ToUpperInvariant() + " / " + partner.CurrencyZone.Name.ToUpperInvariant());
            }
            book.Anchors["stance:head"] = new SlipContent("STANCE").Add("POLICY STANCE · TWO BLENDS, BOTH SIDES").Add("0–100, THE MODEL'S OWN SCALE");
            book.Anchors["stance:fiscal"] = new SlipContent("FISCAL SIZE").Add("THE SIZE OF THE STATE").Add("THE AVERAGE TAX RATE BLENDED WITH SPENDING AS A SHARE OF GDP");
            book.Anchors["stance:regulation"] = new SlipContent("REGULATION / WELFARE").Add("THE REACH OF ITS REGULATION AND WELFARE")
                .Add("THE AVERAGE SECTOR REGULATION BLENDED WITH THE GENEROSITY OF WELFARE IN FORCE");
            book.Anchors["relations"] = new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("NO BILATERAL RELATIONS STATE").Add("THE MODEL HOLDS NONE — NO RELATIONS SCORE,")
                .Add("NO ALLIANCE OR TREATY STANDING,").Add("NO DIPLOMATIC HISTORY");
            book.Anchors["trade:head"] = new SlipContent("TRADE").Add("THE HOME COUNTRY'S TRADE WITH THE WORLD").Add("DOTTED = ZERO, WHERE THE SCALE SPANS IT");
            book.Anchors["trade:passthrough"] = passThroughPp.HasValue
                ? new SlipContent("TARIFF PASS-THROUGH").Add("TARIFF PASS-THROUGH TO PRICES").Add("PP OF INFLATION, THE LAST CLOSED YEAR")
                : new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("TARIFF PASS-THROUGH TO PRICES").Add("ADVANCE A YEAR - NO YEAR HAS CLOSED");
            book.Anchors["trade:pager"] = Pager(GraphRenderer.PagesFor(history?.TradeBalance.Quarterly), tradePage, history?.TradeBalance, "ONE CHART, ITS OWN PAGER");
            AddChart(book, Trade, history?.TradeBalance, tradePage);
            return book;
        }

        /// <summary>The dense view's lines (D16 §2 - provenance adds lines): each section's window as dates, where the pair's figures come from, and
        /// the glyphs' key - the words that left the page at rest for good and belong to no one anchor. The page draws these; the reachability check
        /// reads them as reachable, as 23c counts the † dense line.</summary>
        public static List<(string Head, string Line)> DenseLines(Country home, bool domestic, int seriesPage, int tradePage)
        {
            StatHistory history = home.History;
            var lines = new List<(string Head, string Line)>();
            if (domestic)
            {
                lines.Add(("LIVE SERIES", (WindowDates(history?.Gdp, seriesPage) ?? "NO POINT KEPT YET") + " · ONE POINT EVERY 91 DAYS · THE MODEL'S OWN SERIES, KEPT FROM THE GAME'S OPENING"));
                lines.Add(("GDP", "THE CARD IS NOMINAL, IN TODAY'S PRICES · THE CHART AND ITS GROWTH ARE REAL, AT THE OPENING'S PRICES"));
            }
            else
            {
                lines.Add(("PAIR", "THE MODEL'S OWN LINKS · NOT THE CHES POSITIONS, WHICH ARE NOT ON THIS PAGE · STANCE: THE BLENDS THE COMPASS PLOTTED UNTIL P2-3.2 · 0–100, THE CODE'S OWN SCALE"));
                lines.Add(("TRADE", (WindowDates(history?.TradeBalance, tradePage) ?? "NO POINT KEPT YET") + " · ONE POINT EVERY 91 DAYS"));
            }
            lines.Add(("KEY", SymbolRegistry.Word(Symbol.Absent) + " - NOT HELD · — NIL - A HELD NOTHING · ◇ " + SymbolRegistry.Word(Symbol.Dated)
                + " - THE WINDOW'S LAST POINT, OLDER THAN THE LIVE CARD · " + SymbolRegistry.Word(Symbol.Good) + " / " + SymbolRegistry.Word(Symbol.Bad)
                + " - THE VERDICT, WHERE LOWER IS BETTER"));
            return lines;
        }

        private static string BlocOf(World world, CountryId id)
        {
            foreach (TradeBloc b in world.TradeBlocs) { if (b.IsMember(id)) { return b.Name.ToUpperInvariant(); } }
            return "NO BLOC";
        }

        private static void AddSociety(PeopleSlips.Book book, string name, string definition)
        {
            book.Anchors["society:" + name] = new SlipContent(name.ToUpperInvariant()).Add(definition);
        }

        /// <summary>The pager's slip (23a ⑨): its legend, the window on the page as dates, and whether it can move.</summary>
        private static SlipContent Pager(int pages, int page, MultiResolutionSeries dates, string together)
        {
            var slip = new SlipContent("◀ OLDER · NEWER ▶").Add("OLDER ◀ ▶ NEWER");
            slip.Add(pages <= 1 ? "THE WHOLE SERIES" : "PAGE " + (page + 1).ToString(CultureInfo.InvariantCulture) + " OF " + pages.ToString(CultureInfo.InvariantCulture) + " FROM THE NEWEST");
            string window = WindowDates(dates, page);
            if (window != null) { slip.Add(window); }
            return slip.Add(together);
        }

        /// <summary>The window's first and last points as dates - one point every 91 days back from the series' last.</summary>
        public static string WindowDates(MultiResolutionSeries series, int page)
        {
            if (series == null || !series.LastQuarterlyDate.HasValue || series.Quarterly.Count == 0) { return null; }
            (int start, int end) = GraphRenderer.WindowOf(series.Quarterly.Count, page);
            DateTime last = series.LastQuarterlyDate.Value;
            DateTime from = last.AddDays(-(series.Quarterly.Count - 1 - start) * MultiResolutionSeries.QuarterlyPeriodDays);
            DateTime to = last.AddDays(-(series.Quarterly.Count - end) * MultiResolutionSeries.QuarterlyPeriodDays);
            return from.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " – " + to.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        }

        /// <summary>A chart's slips: its Δ, its verdict glyph, its dated head and its held seed - each the head's own figures, from the same window arithmetic.</summary>
        private static void AddChart(PeopleSlips.Book book, StatsChart chart, MultiResolutionSeries series, int page)
        {
            string id = "chart:" + chart.Id;
            book.Anchors[id + "/delta"] = new SlipContent("Δ CHANGE").Add("Δ = LAST − FIRST IN WINDOW").Add(chart.Unit == ReadingUnit.Money
                ? "IN THE READING'S OWN UNIT · MONEY IN ITS MONEY, NEVER %" : "IN THE READING'S OWN UNIT · NEVER %");
            IReadOnlyList<float> points = series?.Quarterly;
            if (points == null || points.Count == 0) { return; }
            (int start, int end) = GraphRenderer.WindowOf(points.Count, page);
            int firstLive = Math.Max(start, StatsReadings.FirstLiveIndex(points, chart.HeldSeed));
            if (firstLive >= end) { book.Anchors[id + "/held"] = Held(chart); return; }
            float first = points[firstLive], last = points[end - 1];
            MoneyUnit? money = chart.Money;
            string lastText = Figure(last, chart.Unit, money), firstText = Figure(first, chart.Unit, money);
            string delta = StatsReadings.DeltaText(first, last, chart.Unit, money);
            if (end - firstLive >= 2)
            {
                book.Anchors[id + "/delta"].Lines.Insert(0, delta + " · " + firstText + " → " + lastText);
                bool flat = chart.Unit != ReadingUnit.Money && StatsReadings.IsFlat(last - first);
                if (flat) { book.Anchors[id + "/flat"] = new SlipContent(SymbolRegistry.Word(Symbol.TrendFlat)).Add(delta + " · BELOW ITS PRINTED PRECISION").Add("NO VERDICT"); }
                else if (chart.HigherIsBetter == false)
                {
                    Symbol verdict = last < first ? Symbol.Good : Symbol.Bad;
                    book.Anchors[id + "/verdict"] = new SlipContent(SymbolRegistry.Word(verdict)).Add(delta + " · " + firstText + " → " + lastText).Add("LOWER IS BETTER");
                }
            }
            string when = series.LastQuarterlyDate.HasValue ? " · " + series.LastQuarterlyDate.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() : string.Empty;
            book.Anchors[id + "/dated"] = new SlipContent(SymbolRegistry.Word(Symbol.Dated)).Add(lastText + " · THE WINDOW'S LAST POINT" + when).Add("THE CARD ABOVE IS LIVE");
            if (chart.HeldSeed.HasValue && firstLive > start) { book.Anchors[id + "/held"] = Held(chart); }
        }

        private static SlipContent Held(StatsChart chart) => new SlipContent("HELD SEED")
            .Add(Figure(chart.HeldSeed.Value, chart.Unit, chart.Money) + " HELD UNTIL THE FIRST YEAR CLOSED - NOT A HISTORY").Add("Δ RUNS FROM THE FIRST LIVE POINT");

        /// <summary>A chart figure in the reading's own form - the head's.</summary>
        public static string Figure(float value, ReadingUnit unit, MoneyUnit? money)
        {
            if (unit == ReadingUnit.Money && money.HasValue) { return StatsReadings.TrueMinus(UiFormat.Money(value, money.Value)); }
            return unit == ReadingUnit.Percent ? StatsReadings.Rate(value) : StatsReadings.TrueMinus(UiFormat.Number(value, 1));
        }
    }
}
