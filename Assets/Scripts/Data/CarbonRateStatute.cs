using System;

namespace PoliSim.Data
{
    /// <summary>How a country's carbon tax rate moves from one year to the next when nobody moves it - what the statute does to the figure.</summary>
    public enum CarbonRateRule
    {
        /// <summary>The rate is a nominal amount in law and stays where it was set: it erodes in real terms as the price level rises, and the book shows the erosion (France; the countries with no carbon tax).</summary>
        NominalFixed,
        /// <summary>The rate is recalculated every year by the price level's change, so its real value holds by construction (Sweden).</summary>
        PriceLevelIndexed,
        /// <summary>The rate follows a schedule the law sets year by year; after the schedule's last year it carries the price level like the exogenous ETS price does (Germany).</summary>
        LegislatedSchedule,
    }

    /// <summary>
    /// EN-4e (ruled 2026-09-12, COMPLETED.md §471): THE STATUTE'S OWN MOVE OF THE CARBON RATE, one rule per country, each with its paragraph.
    /// A nominal-fixed rate in a current-price book erodes silently - the artefact class P5-B6 exists to prevent - so the rate moves the way the
    /// law moves it, at the boundary, before any decision's override (a passed bill's figure for the coming year wins; where the dial is left,
    /// the statute sets the rate). Zero-rate countries are unaffected: an unimplemented line has no rate to move.
    ///
    /// <para><b>Sweden - PRICE-LEVEL INDEXED.</b> Lag (1994:1776) om skatt på energi, 2 kap. 1 b § (lagen.nu, in its wording as amended by lag
    /// (2026:372)): the energy and carbon tax amounts are recalculated every year by a <i>jämförelsetal</i> - the general price level in June
    /// of the recalculation year over June of the year before, rounded to four decimals - and the government fixes the coming year's amounts
    /// before the end of November. The model's year has one price level, so the ratio is the level at this boundary over the level at the last
    /// (the same year's-prices ratio the spending lines index by, P5-B2/B6), rounded to four decimals as the statute rounds its jämförelsetal;
    /// the energy tax's extra two per cent on the motor fuels is the ENERGY tax's and does not touch the carbon tax.</para>
    ///
    /// <para><b>Germany - A LEGISLATED SCHEDULE.</b> Brennstoffemissionshandelsgesetz § 10 Abs. 2 (gesetze-im-internet.de): the fixed price per
    /// certificate is 25 EUR for 2021, 30 EUR for 2022 and 2023, 45 EUR for 2024, 55 EUR for 2025, and for 2026 a price corridor with a floor of
    /// 55 EUR and a ceiling of 65 EUR; from 2027 the price is an auction's, referenced to the EU ETS 2 by ordinance (§ 10 Abs. 3) - no figure in
    /// the law. The model carries the fixed prices as written, the 2026 corridor at its FLOOR (CONVENTION: the law guarantees the floor and the
    /// auction clears above it; the ceiling, 65, is the alternative), and after the schedule the last legislated figure carried by the price level -
    /// the rule the exogenous ETS price already follows in the merit order (nominal with nominal, B6), stated as the convention it is until a
    /// stage gives the ETS 2 a path. The schedule reads the CALENDAR year each boundary falls in (GA-8-1, ruled 2026-10-14 - it used to count the
    /// seed's year plus the turns run: a German start read a year or two behind the law, a 2026 start three behind, Italy's 2022 start a year
    /// ahead): the seed is 2023's statutory figure (§464, the layer's data year) and stands until the game's first boundary, which sets the
    /// figure of its own year - 2025's 55 for a German start (2024-11-06) - or, where that boundary falls after the schedule, the law's last
    /// figure carried by the level; re-seeding the start's year would re-make §464's ruling, which this does
    /// not.</para>
    ///
    /// <para><b>France - NOMINAL FIXED.</b> The composante carbone is not an instrument of its own but a component inside the TICPE, TICGN and
    /// TICC tariffs, which the law sets as nominal amounts per unit (article 265 du code des douanes, since 2022 the code des impositions sur les
    /// biens et services; the article itself was not reached - legifrance 403 - and is BILLED): 7 EUR/t in 2014, 14.5 in 2015, 22 in 2016,
    /// 30.5 in 2017, 44.6 in 2018 (loi de finances pour 2018, article 16, which also wrote a trajectory to 86.2 in 2022), and the 2019 step was
    /// not enacted after the gilets jaunes - the component has stood at 44.6 EUR/t since (the ministry's fiscalité-carbone page and the Sénat's
    /// PLF 2019 report carry the steps and the trajectory; connaissancedesenergies.org, 6 September 2024, the freeze). No indexation is written:
    /// the rate holds nominal and the book shows its real erosion.</para>
    ///
    /// <para><b>The dial's reach indexes for all six.</b> `TaxLine.RateCeiling` is the 300-dollar bound in the country's currency at the seed's
    /// prices (§464, a convention); a nominal ceiling in a current-price book is the same artefact class, so it carries the price level at every
    /// boundary - the political scale (`TaxLine.PointsOf`, a rise as the per cent of the dial it spans) stays real, and Sweden's indexed rate
    /// never meets a ceiling that stood still. Self-ruled here, strikeable.</para>
    /// </summary>
    public static class CarbonRateStatute
    {
        /// <remarks>SOURCED by ruling (§464): the seed's carbon figures are 2023's statutory ones, the energy layer's data year, and they stand until the game's first boundary. The schedule no longer counts from this year: since GA-8-1 it reads each boundary's calendar year.</remarks>
        public const int SeedYear = 2023;
        /// <remarks>SOURCED - lag (1994:1776) om skatt på energi, 2 kap. 1 b § tredje stycket: "Jämförelsetalet ska avrundas till fyra decimaler."</remarks>
        public const int JamforelsetalDecimals = 4;

        /// <remarks>SOURCED - BEHG § 10 Abs. 2: the fixed price per certificate by year, in euro; 2026 is the corridor's floor (55, ceiling 65 - CONVENTION, the alternative named in the class doc).</remarks>
        public static readonly (int Year, float EurPerTonne)[] BehgSchedule =
        {
            (2021, 25f), (2022, 30f), (2023, 30f), (2024, 45f), (2025, 55f), (2026, 55f),
        };

        public static CarbonRateRule RuleFor(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return CarbonRateRule.PriceLevelIndexed;
                case CountryId.Germany: return CarbonRateRule.LegislatedSchedule;
                default: return CarbonRateRule.NominalFixed;   // France by statute; Italy, Poland and the USA have no rate to move
            }
        }

        /// <summary>The statute's own words for the rule, as a caption reads them - plain, no register names.</summary>
        public static string Caption(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "Recalculated every year by the price level's change, as the energy tax act's indexation rule orders - the real rate holds by law.";
                case CountryId.Germany: return "A legislated schedule under the fuel emissions trading act: 45 EUR in 2024, 55 in 2025, a 55-65 corridor in 2026, then a market price - carried with the price level after the schedule.";
                case CountryId.France: return "A nominal amount fixed in the fuel tax tariffs at 44.6 EUR since 2018 - it erodes as prices rise.";
                default: return "No carbon tax distinct from the ETS.";
            }
        }

        /// <summary>The scheduled figure for a calendar year, or null when the law sets none for it (after 2026).</summary>
        public static float? BehgFor(int year)
        {
            foreach ((int y, float eur) in BehgSchedule) { if (y == year) { return eur; } }
            return null;
        }

        /// <summary>The schedule's last year and its figure - 2026's corridor floor, the law's last fixed price (§ 10 Abs. 2); after it the figure in force is carried by the level.</summary>
        public static int LastScheduledYear => BehgSchedule[BehgSchedule.Length - 1].Year;
        public static float LastScheduledFigure => BehgSchedule[BehgSchedule.Length - 1].EurPerTonne;

        /// <summary>The year's price ratio the statute reads at this boundary: the level now over the level at the last indexation (P5-B2's own reference), 1 on a save from before the price level.</summary>
        public static float YearRatio(Country country) => country.PriceLevelAtLastIndex > 0f ? country.State.PriceLevel / country.PriceLevelAtLastIndex : 1f;

        /// <summary>
        /// The boundary's step, before the decisions' overrides: the carbon line's rate moves as its country's statute moves it, and every country's
        /// carbon ceiling carries the year's prices. Reads Country.PriceLevelAtLastIndex without writing it - IndexSpendingLines, later in the same
        /// boundary, advances that reference for the lines and this rule alike, so the two read one ratio.
        ///
        /// <para>⚠ GA-8-1 (2026-10-14, Elias's ruling: "Keep §471/DS-3's boundary. At each boundary, read the BEHG rate for the calendar year, and let
        /// a passed rate stand for the year it was passed for"). The step stays at the game's year boundary and reads the CALENDAR year that boundary
        /// falls in (<paramref name="boundaryYear"/>, the date of the boundary being crossed), where it used to count the seed's year plus the turns
        /// run - a German start on 2024-11-06 read 2023's 30 EUR/t until 2025-11-06 and 2024's 45 until 2026-11-06, a year or two behind the law (a
        /// 2026 start three behind, Italy's 2022 start a year ahead). A
        /// figure a budget bill passed stands for the year it was passed for (`TaxLine.PassedForYear`), whatever the rule: the statute moves it again
        /// from the first boundary of a later year, where it used to overwrite it at the next boundary. The seed's figure is not re-made (§464): it
        /// stands until the game's first boundary.</para>
        /// </summary>
        public static void AdvanceYear(Country country, int currentTurn, int boundaryYear)
        {
            float ratio = YearRatio(country);
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type != TaxType.CarbonTax) { continue; }
                if (line.RateCeiling > 0f) { line.RateCeiling *= ratio; }   // the dial's reach in the year's prices, for all six
                if (!line.IsImplemented) { continue; }                       // zero-rate countries: nothing to move
                if (line.Rate <= 0f && line.PassedForYear == 0) { continue; } // a zero no bill stamped (a save from before GA-8-1) stays as it was; a passed zero yields to the statute after its year
                if (line.PassedForYear >= boundaryYear) { continue; }        // GA-8-1: a passed figure stands for the year it was passed for
                switch (RuleFor(country.Id))
                {
                    case CarbonRateRule.PriceLevelIndexed:
                        line.Rate *= (float)Math.Round(ratio, JamforelsetalDecimals);
                        break;
                    case CarbonRateRule.LegislatedSchedule:
                    {
                        float? scheduled = BehgFor(boundaryYear);
                        if (scheduled.HasValue) { line.Rate = scheduled.Value; }
                        else
                        {
                            // after the schedule: the last legislated figure carried by the price level, the ETS's own convention. The figure in force IS
                            // that figure - the statute's from an earlier boundary, or a bill's - except at a game's first boundary when that falls after the
                            // schedule and no bill has moved the line since the schedule's last year: the seed's 2023 figure still stands there, and the law's
                            // last one is carried instead
                            bool lawsFigureInForce = currentTurn > 0 || line.PassedForYear >= LastScheduledYear;
                            line.Rate = (lawsFigureInForce ? line.Rate : LastScheduledFigure) * ratio;
                        }
                        break;
                    }
                    default:
                        break;   // nominal fixed: the statute writes no move, and the book shows the erosion
                }
                line.PassedForYear = 0;   // AF-36 (q36): the passed figure's years are over - the figure is the statute's now, and no bill can carry it
            }
        }

        /// <summary>AF-36 (Elias's ruling q36, 2026-10-18: "the slip and the Desk show the date the rate reverts, and a bill may carry a passed figure
        /// into the next year"): whether a statute moves this country's carbon line at all - a passed figure under a nominal-fixed rule never yields,
        /// so it has no date and nothing to carry.</summary>
        public static bool StatuteMoves(CountryId id) => RuleFor(id) != CarbonRateRule.NominalFixed;

        /// <summary>K47 (Elias's ruling of 2026-10-23: "a carried bill is priced as the change it prevents"): the figure the statute's step would set for a line
        /// standing at <paramref name="figure"/> at a boundary in <paramref name="boundaryYear"/> - the rules of `AdvanceYear`, read and never
        /// written, at the year's price ratio as it stands today (`YearRatio`; DECLARED: the ratio the boundary will read is not known before it).
        /// A passed figure is the law's figure in force there, so past the schedule it is carried by the ratio. The figure itself where the statute writes no move.</summary>
        public static float StepOf(Country country, float figure, int boundaryYear)
        {
            float ratio = YearRatio(country);
            switch (RuleFor(country.Id))
            {
                case CarbonRateRule.PriceLevelIndexed:
                    return figure * (float)Math.Round(ratio, JamforelsetalDecimals);
                case CarbonRateRule.LegislatedSchedule:
                    return BehgFor(boundaryYear) ?? figure * ratio;
                default:
                    return figure;
            }
        }

        /// <summary>AF-36: whether the line's figure is a passed one still standing - stamped (`TaxLine.PassedForYear`) for a year no boundary has gone
        /// past. <paramref name="lastBoundaryYear"/> is the calendar year of the last boundary crossed, 0 before the first; it reads a stamp an older
        /// save left after its year as the statute's figure (since AF-36 the stamp is cleared where the statute takes the figure back).</summary>
        public static bool PassedFigureStands(CountryId id, TaxLine line, int lastBoundaryYear) =>
            line != null && line.IsPerTonne && line.IsImplemented && StatuteMoves(id) && line.PassedForYear > 0 && line.PassedForYear >= lastBoundaryYear;

        /// <summary>AF-36: the boundary at which a figure standing for <paramref name="standsForYear"/> yields to the statute - the first boundary from
        /// turn <paramref name="nextTurn"/> on whose calendar year is later (the step in <see cref="AdvanceYear"/>), <paramref name="boundaryOfTurn"/>
        /// being the game's own boundary date (`SimulationManager.TurnBoundary`).</summary>
        public static DateTime YieldsOn(int standsForYear, int nextTurn, Func<int, DateTime> boundaryOfTurn)
        {
            int turn = nextTurn;
            DateTime on = boundaryOfTurn(turn);
            while (on.Year <= standsForYear && turn < nextTurn + 1000) { turn++; on = boundaryOfTurn(turn); }
            return on;
        }

        /// <summary>A rate in the seed's prices - what the couplings compare against their seed references (EnvironmentFamily.TransportTargetFor, the household burden's baseline): the nominal figure over the price level, 1 at the seed.</summary>
        public static float RealRate(float nominalRate, float priceLevel) => nominalRate / Math.Max(0.0001f, priceLevel);
    }
}
