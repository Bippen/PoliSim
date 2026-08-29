using System;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-A5 / SPEC §19 — **the vote model reads PERCEIVED government performance, not actual.**
    /// PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// The spec asks for a difference between "Actual Economy" and "Perceived Economy". The gap
    /// table found that half of this already exists and is the game's most under-used asset:
    /// `PublicationSystem` writes `Country.Published` on the real release calendar, with a
    /// preliminary figure carrying release noise and a later revision correcting it, while
    /// `Country.State` holds the truth the simulation actually runs on. **Voters cannot see
    /// `State`.** They see what has been published, when it was published, and they see a
    /// preliminary print as readily as a revised one.
    ///
    /// So this type reads `Published` and never `State` for the perceived side — that asymmetry IS
    /// the model. `ActualIndex` exists only so the two can be compared and the divergence reported;
    /// nothing that feeds a vote share may call it.
    ///
    /// **The index.** Three published series the electorate actually reacts to — unemployment,
    /// inflation, and the level of GDP relative to its own recent past — each mapped to 0–100 where
    /// 50 is neutral, then averaged. Higher is better for the incumbent.
    ///
    /// **[AUTHORED-DRAFT] constants** (R-N4; logged one line each in the prototype log, all
    /// strikeable, all to be calibrated by play rather than argued about now):
    /// - `UnemploymentNeutral = 6.0` %, `UnemploymentSpan = 6.0` — 6 % reads neutral; 0 % reads 100,
    ///   12 % reads 0. A band wide enough that ordinary movement does not saturate the index.
    /// - `InflationNeutral = 2.0` %, `InflationSpan = 6.0` — 2 % neutral (the target most central
    ///   banks in the model actually run), and **deviation in EITHER direction is punished**:
    ///   deflation is not a bonus, which a naive lower-is-better mapping would wrongly imply.
    /// - `GrowthNeutral = 2.0` %, `GrowthSpan = 6.0` — trend-ish growth reads neutral.
    /// - `IncumbentSwingSpan = 0.15` — at a perfect perceived economy the incumbent's preference is
    ///   multiplied by 1.15, at the worst by 0.85. Deliberately modest: §39 forbids any single
    ///   variable being overwhelming, and a government that could win on published statistics alone
    ///   would make the campaign layer pointless.
    ///
    /// A stat that has **never been published** is not silently replaced by its live value —
    /// `Latest()` returning null means the electorate has no figure to react to, and that component
    /// drops out of the average rather than leaking the truth into perception.
    /// </summary>
    public static class PerceivedPerformance
    {
        public const double UnemploymentNeutral = 6.0;
        public const double UnemploymentSpan = 6.0;
        public const double InflationNeutral = 2.0;
        public const double InflationSpan = 6.0;
        public const double GrowthNeutral = 2.0;
        public const double GrowthSpan = 6.0;
        public const double IncumbentSwingSpan = 0.15;

        /// <summary>The three components and the index they average to, so §31's attribution can name which one moved.</summary>
        public readonly struct Reading
        {
            public readonly double Index;
            public readonly double UnemploymentScore;
            public readonly double InflationScore;
            public readonly double GrowthScore;
            public readonly int ComponentsUsed;
            public readonly bool UnemploymentPublished;
            public readonly bool InflationPublished;
            public readonly bool GdpPublished;

            public Reading(double index, double unemployment, double inflation, double growth, int used,
                bool unemploymentPublished, bool inflationPublished, bool gdpPublished)
            {
                Index = index; UnemploymentScore = unemployment; InflationScore = inflation;
                GrowthScore = growth; ComponentsUsed = used;
                UnemploymentPublished = unemploymentPublished;
                InflationPublished = inflationPublished;
                GdpPublished = gdpPublished;
            }
        }

        /// <summary>Maps a value to 0–100 where <paramref name="neutral"/> is 50; lower values score higher (unemployment, and the |deviation| forms).</summary>
        public static double LowerIsBetter(double value, double neutral, double span)
        {
            double score = 50.0 - 50.0 * (value - neutral) / (span * 0.5);
            return ElectionScales.Clamp(score);
        }

        /// <summary>Maps a value to 0–100 where <paramref name="neutral"/> is 50 and higher values score higher (growth).</summary>
        public static double HigherIsBetter(double value, double neutral, double span)
        {
            double score = 50.0 + 50.0 * (value - neutral) / (span * 0.5);
            return ElectionScales.Clamp(score);
        }

        /// <summary>
        /// **The perceived economy** — read ONLY from `Country.Published`. A component whose series
        /// has never been published is omitted (not defaulted, not filled from `State`).
        /// <paramref name="publishedGrowthPct"/> is supplied by the caller because growth is a
        /// comparison between two published GDP prints, which the caller holds the history for.
        /// </summary>
        public static Reading Perceived(Country country, double? publishedGrowthPct)
        {
            PublishedEntry unemployment = country.Published.Latest(PublishedStat.Unemployment);
            PublishedEntry inflation = country.Published.Latest(PublishedStat.Inflation);
            PublishedEntry gdp = country.Published.Latest(PublishedStat.Gdp);

            double sum = 0.0;
            int used = 0;
            double unemploymentScore = 0, inflationScore = 0, growthScore = 0;

            if (unemployment != null)
            {
                unemploymentScore = LowerIsBetter(unemployment.Value, UnemploymentNeutral, UnemploymentSpan);
                sum += unemploymentScore;
                used++;
            }

            if (inflation != null)
            {
                // Deviation in EITHER direction is punished - deflation is not a bonus.
                inflationScore = LowerIsBetter(Math.Abs(inflation.Value - InflationNeutral), 0.0, InflationSpan);
                sum += inflationScore;
                used++;
            }

            if (gdp != null && publishedGrowthPct.HasValue)
            {
                growthScore = HigherIsBetter(publishedGrowthPct.Value, GrowthNeutral, GrowthSpan);
                sum += growthScore;
                used++;
            }

            double index = used > 0 ? sum / used : 50.0;
            return new Reading(index, unemploymentScore, inflationScore, growthScore, used,
                unemployment != null, inflation != null, gdp != null);
        }

        /// <summary>
        /// **The actual economy** — read from `Country.State`. Exists ONLY so the divergence can be
        /// reported (§31's ledger, and the honesty check that the model is tracking publication
        /// rather than truth). **Nothing that feeds a vote share may call this.**
        /// </summary>
        public static Reading Actual(Country country, double? actualGrowthPct)
        {
            double unemploymentScore = LowerIsBetter(country.State.Unemployment, UnemploymentNeutral, UnemploymentSpan);
            double inflationScore = LowerIsBetter(Math.Abs(country.State.Inflation - InflationNeutral), 0.0, InflationSpan);
            double growthScore = actualGrowthPct.HasValue
                ? HigherIsBetter(actualGrowthPct.Value, GrowthNeutral, GrowthSpan)
                : 50.0;

            int used = actualGrowthPct.HasValue ? 3 : 2;
            double sum = unemploymentScore + inflationScore + (actualGrowthPct.HasValue ? growthScore : 0.0);
            return new Reading(sum / used, unemploymentScore, inflationScore, growthScore, used, true, true, true);
        }

        /// <summary>The incumbent's multiplier from a performance index: 1 ± <see cref="IncumbentSwingSpan"/> at the extremes, 1.0 at a neutral 50.</summary>
        public static double IncumbentMultiplier(double performanceIndex)
        {
            double normalised = (ElectionScales.Clamp(performanceIndex) - 50.0) / 50.0;   // -1 .. +1
            return 1.0 + IncumbentSwingSpan * normalised;
        }

        /// <summary>
        /// Applies the incumbency effect to a preference vector and renormalises. Only the parties
        /// flagged incumbent are moved; everyone else absorbs the remainder proportionally, so the
        /// distribution stays a distribution.
        /// </summary>
        public static double[] ApplyIncumbency(double[] preference, bool[] isIncumbent, double perceivedIndex)
        {
            if (preference == null || isIncumbent == null || preference.Length != isIncumbent.Length)
            {
                throw new ArgumentException("preference and incumbency flags must line up");
            }

            double multiplier = IncumbentMultiplier(perceivedIndex);
            var adjusted = new double[preference.Length];
            double total = 0.0;
            for (int i = 0; i < preference.Length; i++)
            {
                adjusted[i] = isIncumbent[i] ? preference[i] * multiplier : preference[i];
                total += adjusted[i];
            }

            if (total <= 0.0) { return preference; }

            for (int i = 0; i < adjusted.Length; i++) { adjusted[i] /= total; }
            return adjusted;
        }

        /// <summary>The attribution line §31 will print: how far perception sits from truth, and which way.</summary>
        public static double Divergence(Reading perceived, Reading actual) => perceived.Index - actual.Index;
    }
}
