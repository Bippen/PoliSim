using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P2-0.1 (2026-09-02) — <b>the basis of an authored one-time settlement.</b>
    ///
    /// <para>The cabinet decision pool and the foreign-policy meeting pool carry a <c>BudgetImpact</c>
    /// per option, authored in the game's billions when the USA was the only playable country. They
    /// were applied unscaled to whichever country decided, so the same option was a fraction of a
    /// percent of the USA's GDP and a third of Sweden's - a figure on one nation's scale landing on
    /// another nation's books, which is the seam Playtest 2 asked to measure. This type states the
    /// authored basis once and converts at the seam: an option is the same <b>share of the deciding
    /// country's GDP</b> everywhere. The measurement, before and after, is what
    /// <c>DomesticMoneyBasisDiagnostic</c> prints on every run.</para>
    /// </summary>
    public static class AuthoredImpactScale
    {
        /// <summary>The GDP the option pools were authored against: the USA seed's, read off
        /// WorldFactory rather than written here (DERIVED - a scale factor's denominator, not a
        /// researched figure). At this GDP an authored figure applies exactly as written, so the USA
        /// seed is byte-identical.</summary>
        public const float AuthoredScaleGdp = WorldFactory.UsaSeedGdp;

        /// <summary>An authored settlement in the deciding country's billions: the same share of its
        /// GDP that the authored figure is of the authored scale.</summary>
        public static float ToCountryBillions(float authoredBillions, Country country)
        {
            // The seam, closed 2026-09-02: the same share of the deciding country's GDP that the authored
            // figure is of the authored scale. At the USA seed the factor is exactly 1, so the USA applies
            // as written; every other country scales. Current GDP, not seed GDP: a one-time settlement is
            // a share of the economy that pays it.
            return authoredBillions * (country.State.GDP / AuthoredScaleGdp);
        }
    }
}
