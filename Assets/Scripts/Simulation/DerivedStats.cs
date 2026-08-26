using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence step 9, Step A4: the Tier 0 derived stats.
    ///
    /// **Nothing here is tracked state, and that is the whole design.** Every value is computed on
    /// demand from figures the simulation already produces. There are no new fields on
    /// <see cref="EconomyState"/>, no new mean-reverting variables, and nothing that folds into
    /// PotentialGrowthRate's or LaborForceParticipationRate's combined ceiling - so this file cannot
    /// change a simulation number, by construction rather than by testing. That is exactly the
    /// directive's framing: "zero simulation risk - pure display arithmetic from already-tracked
    /// values".
    ///
    /// **Every method returns `float?`, null meaning "not computable yet".** Turn 0 has no fiscal
    /// report and a one-entry history has no growth rate. Returning 0f for those cases would put a
    /// confident, wrong number on screen - the StatTile bug's failure shape, where a display change
    /// silently altered what a number meant.
    ///
    /// UNITS, since two of these are easy to get wrong:
    /// - GDP is in BILLIONS of abstract currency units (USA seeds at ~29000, i.e. ~$29T - see
    ///   WorldFactory's own "$29,000B starting GDP" note).
    /// - Population is in MILLIONS (GameController renders it as "{Population:F1}M").
    /// So GDP / Population lands in THOUSANDS per person: 29000 / 340 = 85.3, against a real-world
    /// US figure of roughly $85k. The magnitudes corroborate each other.
    /// </summary>
    public static class DerivedStats
    {
        /// <summary>
        /// GDP per capita, in THOUSANDS of currency units per person - billions divided by millions.
        /// Null if population has collapsed to zero, which the demographics floor should prevent but
        /// which is not worth dividing by on trust.
        /// </summary>
        public static float? GdpPerCapita(Country country)
        {
            float population = country.State.Population;
            return population > 0f ? country.State.GDP / population : (float?)null;
        }

        /// <summary>
        /// Total tax revenue as a percentage of GDP.
        ///
        /// Reads the revenue the simulation ACTUALLY collected - after collection efficiency, the
        /// Finance/Treasury competence bias and the fiscal reaction multiplier - not the theoretical
        /// yield of the tax lines. Those differ, and the second is not what funded anything. Since
        /// pass 5 (2026-08-26) that revenue includes the tariff flow, as a tax burden should - customs
        /// are taxes on imports.
        /// </summary>
        public static float? TaxBurdenPercentOfGdp(Country country, FiscalTurnReport report)
        {
            return PercentOfGdp(country, report?.Revenue);
        }

        /// <summary>
        /// Total government spending as a percentage of GDP.
        ///
        /// Uses <see cref="FiscalTurnReport.TotalSpending"/>, recorded by the simulation itself.
        /// Deliberately does NOT re-add the report's components: DiscretionarySpending there is a
        /// per-turn CHANGE rather than a level, and the baseline field is not the same one the
        /// spending sum uses, so a hand-rolled total would be plausible and wrong.
        /// </summary>
        public static float? SpendingPercentOfGdp(Country country, FiscalTurnReport report)
        {
            return PercentOfGdp(country, report?.TotalSpending);
        }

        /// <summary>
        /// Deficit as a percentage of GDP, **signed so that positive means a DEFICIT** - the
        /// convention every real-world "deficit % of GDP" figure uses.
        ///
        /// Note this inverts the simulation's own sign: `FiscalTurnReport.BudgetBalance` is positive
        /// for a SURPLUS. The flip happens here, once, rather than at each call site.
        /// </summary>
        public static float? DeficitPercentOfGdp(Country country, FiscalTurnReport report)
        {
            return report == null ? (float?)null : PercentOfGdp(country, -report.BudgetBalance);
        }

        /// <summary>
        /// PRIMARY deficit as a percentage of GDP - the balance EXCLUDING interest on debt, signed
        /// like <see cref="DeficitPercentOfGdp"/> (positive means a primary deficit).
        ///
        /// Same book, same path (playtest finding 2, 2026-08-25, per the single-book rider): the
        /// simulation's BudgetBalance is net of interest (TotalSpending includes InterestOnDebt -
        /// see SimulationManager.ApplyRevenueAndSpending), so the primary balance is that same
        /// report's BudgetBalance + InterestOnDebt - a labeled second reading of the one book the
        /// simulation keeps, never a separate computation that could drift from it.
        /// </summary>
        public static float? PrimaryDeficitPercentOfGdp(Country country, FiscalTurnReport report)
        {
            return report == null ? (float?)null : PercentOfGdp(country, -(report.BudgetBalance + report.InterestOnDebt));
        }

        /// <summary>
        /// Real GDP growth between the two most recent quarterly history entries, in percent.
        ///
        /// **Why no inflation adjustment.** GDP here is already real. `MacroSystem.ApplyNationalAccounts`
        /// builds it purely from the expenditure identity (C + I + G + NX) plus output-gap reversion
        /// toward PotentialGDP; no price level multiplies it anywhere, and the interest-rate term works
        /// off a REAL rate (`interestRate - TaylorRule.NeutralRealRate`). Subtracting inflation would
        /// deflate a series that was never inflated.
        ///
        /// The one thing that could mislead: the starting LEVEL is calibrated to a real-world *nominal*
        /// GDP figure (WorldFactory's ~$29,000B). That sets the scale at t=0 - it does not make the
        /// model's period-over-period dynamics nominal.
        ///
        /// Null until two entries exist.
        /// </summary>
        public static float? RealGdpGrowthPercent(Country country)
        {
            IReadOnlyList<float> series = country.History.Gdp.Quarterly;
            if (series == null || series.Count < 2)
            {
                return null;
            }

            float previous = series[series.Count - 2];
            float current = series[series.Count - 1];
            return previous > 0f ? (current - previous) / previous * 100f : (float?)null;
        }

        /// <summary>
        /// Each sector's share of GDP, in percent.
        ///
        /// This one is a pass-through on purpose. `Sector.OutputShareOfGdp` is already a tracked,
        /// simulated field - recomputing a share from sector outputs would invent a second answer to a
        /// question the model has already answered, and the two would drift. Returned in the country's
        /// own sector order so callers render a stable list rather than a reordering one.
        /// </summary>
        public static List<(SectorType Type, float SharePercent)> SectorSharesOfGdp(Country country)
        {
            var shares = new List<(SectorType, float)>();
            foreach (Sector sector in country.Sectors)
            {
                shares.Add((sector.Type, sector.OutputShareOfGdp));
            }
            return shares;
        }

        /// <summary>Shared guard: a percentage of GDP is meaningless at or below zero GDP.</summary>
        private static float? PercentOfGdp(Country country, float? amount)
        {
            if (amount == null)
            {
                return null;
            }

            float gdp = country.State.GDP;
            return gdp > 0f ? amount.Value / gdp * 100f : (float?)null;
        }
    }
}
