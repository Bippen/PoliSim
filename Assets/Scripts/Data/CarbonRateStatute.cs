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
    /// stage gives the ETS 2 a path. The schedule runs on the energy layer's own clock - the seed is 2023's statutory figure (§464) and the
    /// layer's data year - so the first boundary sets 2024's price; the substrate's calendar (EpochDate, 2026 at turn 0) is the election's clock,
    /// not this one, and the alternative reading (re-seeding 2026's corridor floor) would re-make §464's ruling, which this does not.</para>
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
        /// <remarks>SOURCED by ruling (§464): the seed's carbon figures are 2023's statutory ones, the energy layer's data year; the schedule counts from it.</remarks>
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

        /// <summary>The year the boundary opens - the seed's year plus the turns run plus one: the first boundary sets the second year's figure.</summary>
        public static int ComingYear(int currentTurn) => SeedYear + currentTurn + 1;

        /// <summary>The year's price ratio the statute reads at this boundary: the level now over the level at the last indexation (P5-B2's own reference), 1 on a save from before the price level.</summary>
        public static float YearRatio(Country country) => country.PriceLevelAtLastIndex > 0f ? country.State.PriceLevel / country.PriceLevelAtLastIndex : 1f;

        /// <summary>
        /// The boundary's step, before the decisions' overrides: the carbon line's rate moves as its country's statute moves it, and every country's
        /// carbon ceiling carries the year's prices. Reads Country.PriceLevelAtLastIndex without writing it - IndexSpendingLines, later in the same
        /// boundary, advances that reference for the lines and this rule alike, so the two read one ratio.
        /// </summary>
        public static void AdvanceYear(Country country, int currentTurn)
        {
            float ratio = YearRatio(country);
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type != TaxType.CarbonTax) { continue; }
                if (line.RateCeiling > 0f) { line.RateCeiling *= ratio; }   // the dial's reach in the year's prices, for all six
                if (!line.IsImplemented || line.Rate <= 0f) { continue; }    // zero-rate countries: nothing to move
                switch (RuleFor(country.Id))
                {
                    case CarbonRateRule.PriceLevelIndexed:
                        line.Rate *= (float)Math.Round(ratio, JamforelsetalDecimals);
                        break;
                    case CarbonRateRule.LegislatedSchedule:
                    {
                        float? scheduled = BehgFor(ComingYear(currentTurn));
                        if (scheduled.HasValue) { line.Rate = scheduled.Value; }
                        else { line.Rate *= ratio; }   // after the schedule: the last legislated figure carried by the price level, the ETS's own convention
                        break;
                    }
                    default:
                        break;   // nominal fixed: the statute writes no move, and the book shows the erosion
                }
            }
        }

        /// <summary>A rate in the seed's prices - what the couplings compare against their seed references (EnvironmentFamily.TransportTargetFor, the household burden's baseline): the nominal figure over the price level, 1 at the seed.</summary>
        public static float RealRate(float nominalRate, float priceLevel) => nominalRate / Math.Max(0.0001f, priceLevel);
    }
}
