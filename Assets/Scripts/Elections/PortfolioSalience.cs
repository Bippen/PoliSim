using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §706 (Elias's ruling of 2026-10-01): WHAT A CABINET POST IS WORTH IN THE GAMSON SHARE - Druckman &amp; Warwick's expert-survey salience
    /// ("The missing piece", EJPR 44, 2005: each post scored against an average portfolio of 1.00; the appendix per country), mapped onto the
    /// game's six portfolios as `ElectionsData/portfolios/portfolio_salience.md` records: a portfolio that stands for several of the country's
    /// separate ministries weighs their sum, one that stands for one ministry its score (Italy's MEF, one ministry merged from three rated posts,
    /// its highest). The head of government's weight is credited to its party, never allocated. Poland and the USA take the mean of the four
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

        /// <summary>A portfolio's weight in its country (an average portfolio is 1.00).</summary>
        public static double Weight(CountryId country, CabinetPortfolio portfolio)
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
                        case CabinetPortfolio.Education: return 0.82;                     // Science & Education
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
                        case CabinetPortfolio.FinanceTreasury: return 1.92;               // Economy & Finance
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
                        case CabinetPortfolio.FinanceTreasury: return 1.64;               // Treasury - the MEF merged it with Finance 1.32 and Budget 0.98
                        case CabinetPortfolio.InteriorJustice: return 1.78 + 1.23;
                        case CabinetPortfolio.HealthSocialAffairs: return 1.06 + 1.19;   // Labour & Social Security/Welfare + Health
                        case CabinetPortfolio.Defense: return 1.19;
                        case CabinetPortfolio.ForeignAffairs: return 1.69;
                        case CabinetPortfolio.Education: return 1.10;
                    }
                    break;
            }
            switch (portfolio)   // DERIVED: the mean of the four countries rated
            {
                case CabinetPortfolio.FinanceTreasury: return 1.705;
                case CabinetPortfolio.InteriorJustice: return 2.35;
                case CabinetPortfolio.HealthSocialAffairs: return 2.215;
                case CabinetPortfolio.Defense: return 1.17;
                case CabinetPortfolio.ForeignAffairs: return 1.455;
                case CabinetPortfolio.Education: return 1.0975;
                default: return 1.0;
            }
        }

        /// <summary>The weight a set of posts carries (the head's weight added where <paramref name="head"/>).</summary>
        public static double Of(CountryId country, System.Collections.Generic.IEnumerable<CabinetPortfolio> posts, bool head = false)
        {
            double sum = head ? HeadWeight(country) : 0.0;
            if (posts != null) { foreach (CabinetPortfolio p in posts) { sum += Weight(country, p); } }
            return sum;
        }
    }
}
