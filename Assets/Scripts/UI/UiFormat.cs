using System;
using System.Globalization;

namespace PoliSim.UI
{
    /// <summary>
    /// The unit a stored currency number is expressed in. Every currency value in this project is
    /// stored in BILLIONS - verified against the seeds rather than assumed (USA GDP 29000 = $29T,
    /// Defense 850 = $850B, SocialSecurity 1530 = $1.53T, Sweden's SWF 195 = its real AP-fund total);
    /// seven <c>EconomyState</c> fields, <c>SpendingLine.Amount</c>, <c>SovereignWealthFund.TotalAssets</c>
    /// and all twelve <c>FiscalTurnReport</c> money fields agree.
    ///
    /// <see cref="Thousands"/> exists for the one genuine exception, and it is a DERIVED value rather
    /// than a stored one: <c>DerivedStats.GdpPerCapita</c> is thousands per person (billions divided by
    /// millions), as its own doc comment states. That single exception is why the unit has to travel
    /// per stat instead of being a constant inside the formatter - a formatter that assumed
    /// "currency implies billions" would render GDP per capita a million times too large.
    /// </summary>
    public enum MoneyUnit
    {
        Billions,
        Thousands,
    }

    /// <summary>
    /// The one place currency becomes text.
    ///
    /// **The bug this exists to end.** <c>GraphRenderer.FormatAxisValue(29000)</c> rendered "29k" for
    /// $29 TRILLION. The arithmetic was right and the suffix ladder was right; both were applied to a
    /// base unit of 1 while the model stores billions. That was the THIRD instance on this same value -
    /// StatTile once showed GDP as "9,3", and the suffix ladder was itself built to make losing
    /// magnitude impossible. It worked, and still misreported by a factor of a thousand, because it
    /// scaled from the wrong starting point. A player reading "29k" has nothing to contradict them:
    /// before this class, not one of the 21 currency display sites printed a symbol or a unit.
    ///
    /// **Why an entry point rather than a sixth careful fix.** Two previous fixes were correct in
    /// isolation and failed because nothing stopped the next site doing something else. The property
    /// that matters here is not that this function is right - it is that a call site cannot render
    /// currency without naming a <see cref="MoneyUnit"/>, so being inconsistent takes deliberate
    /// effort. The unit lives on the stat's own metadata (<c>PolicyWebRenderer.GetStatUnit</c>), so a
    /// graph axis asks the series what it is instead of assuming.
    ///
    /// **Money renders in InvariantCulture, and deliberately so.** This machine's locale is sv-SE, so
    /// the first version of this class produced "$29,0T" - a Swedish decimal comma against a US dollar
    /// sign. Two reasons it is pinned rather than left to the machine: the amounts are US dollars in
    /// every country's UI (Sweden's SWF included), so the separator should match the symbol already
    /// chosen; and a locale-dependent formatter cannot have a fixed-string regression test, which this
    /// function above all others needs. Note the project's own history here - the "9,3" incident was a
    /// comma-decimal figure clipped in a narrow rect - so the separator is not a cosmetic detail.
    /// Non-money readouts were originally left on machine locale as out of scope. That produced a
    /// headline grid printing "$29.9T" beside "4,27" and has since been corrected - see
    /// <see cref="Number"/>.
    ///
    /// **No simulation change.** Every stored value keeps its number and its unit; this changes only
    /// how it is read out.
    /// </summary>
    public static class UiFormat
    {
        /// <summary>Dollars per stored unit. Billions and thousands are the only two in the model.</summary>
        private static double DollarsPerUnit(MoneyUnit unit) => unit == MoneyUnit.Billions ? 1e9 : 1e3;

        private static readonly (double Threshold, string Suffix)[] Tiers =
        {
            (1e12, "T"),
            (1e9, "B"),
            (1e6, "M"),
            (1e3, "k"),
            (0d, ""),
        };

        /// <summary>
        /// A currency amount, rendered at the magnitude a person actually says it at: $29.0T, $850B,
        /// $500M, $85.6k. The suffix describes the REAL magnitude, never the storage scale.
        ///
        /// Always three significant digits, which is what keeps the string short enough for a graph's
        /// axis gutter and a StatTile's fixed rect - the two places a too-wide number has silently
        /// mangled before. Rounding happens BEFORE the tier is chosen, so $999.97B reads "$1.00T"
        /// rather than the "$1000B" a round-then-scale order would produce.
        ///
        /// <paramref name="explicitPlus"/> prefixes a "+" on positives, for the sites showing a change
        /// rather than a level. The sign sits outside the "$", as "-$1.2T" rather than "$-1.2T".
        /// </summary>
        public static string Money(float value, MoneyUnit unit, bool explicitPlus = false)
        {
            double dollars = (double)value * DollarsPerUnit(unit);
            double magnitude = Math.Abs(dollars);
            string sign = dollars < 0d ? "-" : (explicitPlus ? "+" : string.Empty);

            // Zero gets its own branch so it reads "$0" rather than "$0.00".
            if (magnitude < 0.5d)
            {
                return explicitPlus ? "+$0" : "$0";
            }

            int tier = Tiers.Length - 1;
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (magnitude >= Tiers[i].Threshold)
                {
                    tier = i;
                    break;
                }
            }

            double scaled = Tiers[tier].Threshold > 0d ? magnitude / Tiers[tier].Threshold : magnitude;
            double rounded = Math.Round(scaled, DecimalsFor(scaled), MidpointRounding.AwayFromZero);

            // Rounding can carry a value up into the next tier: $999.97B is "$1.00T", not "$1000B".
            // Because the tiers step by exactly 1000, a carry always lands on 1.00 of the tier above.
            //
            // Done HERE, on the already-scaled number, and that ordering is load-bearing. The first
            // version of this function rounded the dollar amount to three significant digits and then
            // picked a tier, which meant dividing by a negative power of ten - 1e-9 is not exactly
            // representable, so $999.97B came back a hair BELOW 1e12, missed the trillions tier and
            // printed "$1000B". Caught by MoneyFormatDiagnostic CHECK 4.
            if (rounded >= 1000d && tier > 0)
            {
                tier--;
                rounded = 1d;
            }

            return sign + "$" + rounded.ToString("F" + DecimalsFor(rounded), CultureInfo.InvariantCulture) + Tiers[tier].Suffix;
        }

        /// <summary>
        /// <see cref="Money"/> with an explicit sign, for deltas and balances where the direction is
        /// the point. Named separately so a call site reads as what it is at the point of use.
        /// </summary>
        public static string MoneyDelta(float value, MoneyUnit unit) => Money(value, unit, explicitPlus: true);

        /// <summary>
        /// A non-currency readout - a percentage, a rate, an index - pinned to InvariantCulture like
        /// everything else the UI prints.
        ///
        /// ⚠ **This reverses the scoping note above, deliberately and with the reason recorded.** When
        /// <see cref="Money"/> landed, non-money was left on machine locale as out of scope. The result
        /// on a sv-SE machine is visible in every capture: the headline grid prints `$29.9T` beside
        /// `4,27` and `48,0` - one tile with a period, the next with a comma, in the same three-column
        /// block. Mixed separators are worse than either convention, because neither reading is
        /// available: a Swedish player sees a broken decimal, a US player sees a comma.
        ///
        /// The codebase had already settled this in practice. Every other numeric site in
        /// `GameController` passes `CultureInfo.InvariantCulture` explicitly; only the tile grid did
        /// not. This makes the settled convention askable instead of remembered - the same argument
        /// that produced <see cref="Money"/>, applied to the numbers <see cref="Money"/> declined.
        /// </summary>
        public static string Number(float value, int decimals)
        {
            return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Three significant digits total, so the decimal places shrink as the integer part grows:
        /// 850 -> "850", 29.0 -> "29.0", 1.53 -> "1.53". Keeps every rendered amount to at most five
        /// characters before the suffix.
        /// </summary>
        private static int DecimalsFor(double scaled)
        {
            double magnitude = Math.Abs(scaled);
            if (magnitude >= 100d) { return 0; }
            if (magnitude >= 10d) { return 1; }
            return 2;
        }
    }
}
