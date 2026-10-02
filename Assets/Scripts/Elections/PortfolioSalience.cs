using System;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §706 (Elias's ruling of 2026-10-01): WHAT A CABINET POST IS WORTH IN THE GAMSON SHARE - Druckman &amp; Warwick's expert-survey salience
    /// ("The missing piece", EJPR 44, 2005: each post scored against an average portfolio of 1.00; the appendix per country), mapped onto the
    /// game's six portfolios as `ElectionsData/portfolios/portfolio_salience.md` records: a portfolio that stands for several of the country's
    /// separate ministries weighs their sum, one that stands for one ministry its score - and Italy's MEF, one ministry merged from the three posts the
    /// paper rates (Treasury, Finance, Budget), weighs their SUM as the paper does (Elias's ruling of 2026-10-01, item 8; §717 - the miss it makes
    /// against the record is recorded there, the method unadjusted). The head of government's weight is credited to its party, never allocated. Poland and the USA take the mean of the four
    /// countries rated [DERIVED - the play-calibration list's 23rd entry, key SAL-POL: Druckman &amp; Roberts' Polish table is owed].
    /// </summary>
    public static class PortfolioSalience
    {
        public const string Source = "Druckman & Warwick, \"The missing piece\", EJPR 44 (2005), appendix [DW05]; ElectionsData/portfolios/portfolio_salience.md";

        /// <summary>The head of government's weight - the chancellor's or prime minister's post, credited to its party's share.</summary>
        public static double HeadWeight(CountryId country)
        {
            switch (country)
            {
                case CountryId.Germany: return 2.12;   // Chancellor [DW05, Germany]
                case CountryId.Sweden: return 2.19;    // Prime Minister [DW05, Sweden]
                case CountryId.France: return 2.75;    // Prime Minister [DW05, France (V)]
                case CountryId.Italy: return 2.48;     // Prime Minister [DW05, Italy]
                default: return 2.385;                  // DERIVED: the mean of the four
            }
        }

        /// <summary>
        /// §753 (Elias's ruling A3: "merged ministries coded as Druckman &amp; Warwick code them, Germany included"): the paper codes each cabinet's
        /// posts as they stood, a merged post weighing its rated posts summed (DW05 p. 26: Ireland's justice 1.24 + communications 0.91 = 2.15), so a
        /// country's weights are DATED. Germany's BMBF (1994) merged Science &amp; Education (0.82) with Research &amp; Technology (0.93); the cabinet of
        /// 6 May 2025 split it - research to the BMFTR, education to the BMBFSFJ with Families &amp; Youth (0.68) - the Federal Government's cabinet page
        /// (`ElectionsData/germany/raw/records/breg_bundeskabinett.html`) lists both, and the record's Merz government dates them (WorldClock, [BT-KW25]).
        /// The split was the 21st Bundestag's government's own: so the key is that chamber's ELECTION, 2025-02-23 - a cabinet formed on the 2025
        /// chamber takes the ministries as its government of record organized them, one structure through the whole formation (drafted from the day
        /// after polling day, installed weeks later); a cabinet of the 2021 chamber the BMBF.
        /// </summary>
        public static readonly DateTime GermanyBmbfSplit = new DateTime(2025, 2, 23);

        /// <summary>
        /// §753: France's finance ministry has carried industry since the Borne government - the Décret du 20 mai 2022 relatif à la composition du
        /// Gouvernement, art. 1: "M. Bruno LE MAIRE, ministre de l'économie, des finances et de la souveraineté industrielle et numérique"
        /// (`ElectionsData/france/raw/executive/wb_legifrance_JORFTEXT000045819551.html`; the JORF of 2024-12-14 "... et de l'industrie") - a merged
        /// post: Economy &amp; Finance (1.92) and Industry (1.06), summed. Before it, the stored JORF of 2022-05-17 reads "et de la relance".
        /// </summary>
        public static readonly DateTime FranceBercyIndustry = new DateTime(2022, 5, 20);

        /// <summary>A portfolio's weight in its country (an average portfolio is 1.00) for a cabinet formed on <paramref name="asOf"/> - §753: the ministries as
        /// they stood that day.</summary>
        public static double Weight(CountryId country, CabinetPortfolio portfolio, DateTime asOf)
        {
            switch (country)
            {
                case CountryId.Germany:
                    switch (portfolio)
                    {
                        case CabinetPortfolio.FinanceTreasury: return 1.58;
                        case CabinetPortfolio.InteriorJustice: return 1.27 + 1.02;       // Interior (Home Affairs) + Justice
                        case CabinetPortfolio.HealthSocialAffairs: return 1.21 + 0.80;   // Labour (& Social Affairs) + Health
                        case CabinetPortfolio.Defense: return 1.12;
                        case CabinetPortfolio.ForeignAffairs: return 1.41;
                        case CabinetPortfolio.Education:
                            return asOf < GermanyBmbfSplit
                                ? 0.82 + 0.93    // the BMBF: Science & Education + Research & Technology, summed as the paper sums a merged post (§753; was 0.82)
                                : 0.82 + 0.68;   // the BMBFSFJ, cabinets of the 2025 chamber: Science & Education + Families & Youth (research gone to the BMFTR, no post of the six)
                    }
                    break;
                case CountryId.Sweden:
                    switch (portfolio)
                    {
                        case CabinetPortfolio.FinanceTreasury: return 1.68;
                        case CabinetPortfolio.InteriorJustice: return 0.99;               // Justice (no Interior rated)
                        case CabinetPortfolio.HealthSocialAffairs: return 1.22 + 1.26;    // Health & Social Affairs/Welfare + Labour/Employment (the review of §706: labour is summed in, as for the other three)
                        case CabinetPortfolio.Defense: return 0.99;
                        case CabinetPortfolio.ForeignAffairs: return 1.27;
                        case CabinetPortfolio.Education: return 1.07;                     // Education (& Science)
                    }
                    break;
                case CountryId.France:
                    switch (portfolio)
                    {
                        case CabinetPortfolio.FinanceTreasury: return asOf < FranceBercyIndustry ? 1.92 : 1.92 + 1.06;   // Economy & Finance; with Industry from 2022-05-20 (§753)
                        case CabinetPortfolio.InteriorJustice: return 1.63 + 1.48;
                        case CabinetPortfolio.HealthSocialAffairs: return 1.13 + 0.99;   // Employment + Public Health
                        case CabinetPortfolio.Defense: return 1.38;
                        case CabinetPortfolio.ForeignAffairs: return 1.45;
                        case CabinetPortfolio.Education: return 1.40;
                    }
                    break;
                case CountryId.Italy:
                    switch (portfolio)
                    {
                        case CabinetPortfolio.FinanceTreasury: return 1.64 + 1.32 + 0.98;   // the MEF: Treasury + Finance + Budget, summed as the paper does (§717; was the highest, 1.64)
                        case CabinetPortfolio.InteriorJustice: return 1.78 + 1.23;
                        case CabinetPortfolio.HealthSocialAffairs: return 1.06 + 1.19;   // Labour & Social Security/Welfare + Health
                        case CabinetPortfolio.Defense: return 1.19;
                        case CabinetPortfolio.ForeignAffairs: return 1.69;
                        case CabinetPortfolio.Education: return 1.10;                     // Istruzione - the post that holds education (Universities & Research, 0.69, a separate ministry)
                    }
                    break;
            }
            if (!System.Enum.IsDefined(typeof(CabinetPortfolio), portfolio)) { return 1.0; }   // a post outside the six (a corrupt save) is average - never the mean, which would recurse (the review's note)
            // DERIVED: the mean of the four countries rated, each at the same date (§753 - computed, so it follows their dating: Finance 2.545 and
            // Education 1.33 before 2025-02-23, Education 1.2675 from the 2025 chamber; before §753 Finance 2.28 and Education 1.0975, §717's and §706's)
            return (Weight(CountryId.Germany, portfolio, asOf) + Weight(CountryId.Sweden, portfolio, asOf) + Weight(CountryId.France, portfolio, asOf) + Weight(CountryId.Italy, portfolio, asOf)) / 4.0;
        }

        /// <summary>The weight a set of posts carries for a cabinet formed on <paramref name="asOf"/> (the head's weight added where <paramref name="head"/>).</summary>
        public static double Of(CountryId country, System.Collections.Generic.IEnumerable<CabinetPortfolio> posts, DateTime asOf, bool head = false)
        {
            double sum = head ? HeadWeight(country) : 0.0;
            if (posts != null) { foreach (CabinetPortfolio p in posts) { sum += Weight(country, p, asOf); } }
            return sum;
        }
    }
}
