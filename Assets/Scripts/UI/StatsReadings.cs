using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>What a reading is, for how its change prints (D-ST, board 23a ⑩): money in its money, a percentage in points of a percentage, a
    /// score or an index as a plain number - never a relative per cent of a rate.</summary>
    public enum ReadingUnit { Money, Percent, Score }

    /// <summary>D-ST (board 23a ⑨): ONE PAGER FOR A SECTION'S CHARTS - the windows move together, which keeps the laws-enacted ticks in line down the
    /// columns. A chart handed a section draws no pager of its own and no foot; the section's head draws the pager.</summary>
    public sealed class GraphSection
    {
        public int PageFromEnd;
    }

    /// <summary>
    /// D-ST (boards 23a-23c, 2026-09-30): **THE STATISTICS SHEET'S RULES AS FUNCTIONS** - one place that says how a chart's change prints, where a
    /// held seed value ends and the series goes live, how far an axis must span, what the sector bar's remainder is and the year's real growth - so
    /// the sheet, the other graphs and `StatsSheetCheck` read one answer. Pure: nothing here draws or reads the running game.
    /// </summary>
    public static class StatsReadings
    {
        /// <summary>A rate or a score prints at one decimal (9a's one form per figure); a move under half its last digit is FLAT (19a).</summary>
        public const float PrintedStep = 0.1f;

        /// <summary>23a ⑬: an axis spans at least four steps of its printed precision.</summary>
        public const int MinimumAxisSteps = 4;

        /// <summary>The true minus, U+2212.</summary>
        public const string Minus = "−";

        /// <summary>23a ⑩: the change from <paramref name="first"/> to <paramref name="last"/> in the reading's own unit - "Δ +US$51B", "Δ −2.2 pp",
        /// "Δ −3.9"; a FLAT move prints its zero unsigned ("Δ 0.0 pp"). <paramref name="decimals"/> is the reading's printed precision (one; a policy
        /// rate's two). The minus is the true minus (sitting A's D5).</summary>
        public static string DeltaText(float first, float last, ReadingUnit unit, MoneyUnit? money, int decimals = 1)
        {
            float change = last - first;
            if (unit == ReadingUnit.Money && money.HasValue) { return "Δ " + TrueMinus(UiFormat.MoneyDelta(change, money.Value)); }
            string suffix = unit == ReadingUnit.Percent ? " pp" : string.Empty;
            string format = decimals <= 1 ? "0.0" : "0.00";
            if (IsFlat(change, decimals)) { return "Δ " + 0f.ToString(format, CultureInfo.InvariantCulture) + suffix; }
            return "Δ " + (change > 0f ? "+" : Minus) + Mathf.Abs(change).ToString(format, CultureInfo.InvariantCulture) + suffix;
        }

        /// <summary>§728 (UI v3.5, rule 5): the change as a v3.5 head prints it - ▲ or ▼ and the magnitude in the reading's own unit (the arrow carries the
        /// direction its ink judges); a FLAT move its zero, with no arrow. The slip keeps <see cref="DeltaText"/>'s Δ form as the definition.</summary>
        public static string ArrowText(float first, float last, ReadingUnit unit, MoneyUnit? money, int decimals = 1)
        {
            float change = last - first;
            string format = decimals <= 1 ? "0.0" : "0.00";
            string suffix = unit == ReadingUnit.Percent ? " pp" : string.Empty;
            if (unit != ReadingUnit.Money && IsFlat(change, decimals)) { return 0f.ToString(format, CultureInfo.InvariantCulture) + suffix; }
            string magnitude = unit == ReadingUnit.Money && money.HasValue
                ? UiFormat.Money(Mathf.Abs(change), money.Value)
                : Mathf.Abs(change).ToString(format, CultureInfo.InvariantCulture) + suffix;
            return (change > 0f ? "▲ " : change < 0f ? "▼ " : string.Empty) + magnitude;
        }

        /// <summary>19a's FLAT: a move below half the printed step rounds to nothing, so it carries no verdict and no sign.</summary>
        public static bool IsFlat(float change, int decimals = 1) => Mathf.Abs(change) < (decimals <= 1 ? PrintedStep : PrintedStep * 0.1f) * 0.5f;

        /// <summary>23a ⑤: the true minus on a figure the sheet prints - the shared formatter keeps the ASCII hyphen its log readers parse.</summary>
        public static string TrueMinus(string figure) => string.IsNullOrEmpty(figure) || figure[0] != '-' ? figure : Minus + figure.Substring(1);

        /// <summary>
        /// 23a ⑭ / 23b ⑭: the index of a series' first LIVE point - the leading run of points exactly equal to <paramref name="heldSeed"/> (the seed's
        /// round default, held until the formula that moves the series first runs at a year's close: approval's 50.0, the trade balance's 0.00) is
        /// not a history. Returns 0 when the series does not open on the seed value, the series' count when every point is still the seed.
        /// </summary>
        public static int FirstLiveIndex(IReadOnlyList<float> series, float? heldSeed)
        {
            if (series == null || !heldSeed.HasValue) { return 0; }
            int i = 0;
            while (i < series.Count && series[i] == heldSeed.Value) { i++; }
            return i;
        }

        /// <summary>23a ⑬: the axis range widened, about its centre, to at least <see cref="MinimumAxisSteps"/> printed steps - poverty's 0.1-point
        /// window had printed 9.0 twice. Money keeps its range (its printed step moves with its magnitude).</summary>
        public static void WidenToPrintedSteps(ref float min, ref float max)
        {
            float span = MinimumAxisSteps * PrintedStep;
            if (max - min >= span) { return; }
            float centre = (min + max) * 0.5f;
            min = centre - span * 0.5f;
            max = centre + span * 0.5f;
        }

        /// <summary>23a ⑧: the share of GDP in no modelled sector - 100 less the eight (the eight are shares of GDP: `Sector.OutputShareOfGdp`, value
        /// added by sector; the rest of the economy - the public sector, property, health, education, business services - is not a sector here).</summary>
        public static float SectorRemainderPercent(IReadOnlyList<float> sharesPercent)
        {
            float sum = 0f;
            foreach (float s in sharesPercent) { sum += Mathf.Max(0f, s); }
            return Mathf.Max(0f, 100f - sum);
        }

        /// <summary>23a ③ / the answer to Design's question 4: real GDP growth over the last four quarterly points (a year: 4 × 91 days), read off the
        /// kept series - null until the series holds five points. It was the controller's figure, set at a year's close by play's own path only, so a
        /// run that closed its years through the manager (the film's warm-up) printed its default, 0.00 %.</summary>
        public static float? YearOnYearGrowthPercent(IReadOnlyList<float> quarterly)
        {
            if (quarterly == null || quarterly.Count < 5) { return null; }
            float then = quarterly[quarterly.Count - 5];
            float now = quarterly[quarterly.Count - 1];
            return then > 0f ? (now - then) / then * 100f : (float?)null;
        }

        /// <summary>A rate's figure at one decimal with its unit (23a ⑤: 5.8 %, never 5.80 % on the card and 5.8 on the chart).</summary>
        public static string Rate(float percent) => TrueMinus(UiFormat.Number(percent, 1)) + "%";
    }
}
