using System;

namespace PoliSim.UI
{
    /// <summary>
    /// The review of wave/dcrisis, M2 (K178: the strip's height comes out of the Desk's first card row): THE CALENDAR CARD NEVER CLIPS SILENTLY. The month
    /// grid sits in a GUILayout area, which clips what overflows without a guard, and a six-row month with the † dense line is the card's worst case. The
    /// fit is decided before the grid is laid out, in this order:
    /// <list type="number">
    /// <item>the grid at its full cell (the day number's line and the dots' row, the full gap) with the dense line - as built;</item>
    /// <item>the grid at its full cell, the dense line dropped (its text is the card's head slip, word for word, so nothing it said is lost);</item>
    /// <item>the grid at its floor cell (the dots' row and the gap at their floor - the day number's own line never shrinks, the 14 px floor), the dense
    /// line dropped;</item>
    /// <item>nothing fits: <see cref="Result.Fits"/> false - the card reports it loudly (an error the film counts) and never pretends.</item>
    /// </list>
    /// A pure function of the measures, so `TopStripCrisisDiagnostic` reads the worst month on the 1280 geometry.
    /// </summary>
    public static class CalendarFit
    {
        public struct Result
        {
            public bool Fits;
            public bool DenseKept;
            public float DotRow;
            public float Gap;
            public float Need;
        }

        /// <summary>The rows a month's grid takes: its lead blanks before the 1st under <paramref name="firstDayOfWeek"/>, then its days, seven to a row.</summary>
        public static int Rows(DateTime monthStart, DayOfWeek firstDayOfWeek)
        {
            int days = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            int lead = ((int)monthStart.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
            return (lead + days + 6) / 7;
        }

        /// <summary>The grid's height: the weekday header, then <paramref name="rows"/> cells of the day number's line and the dots' row, the gaps between them.</summary>
        public static float GridHeight(float header, int rows, float numberLine, float dotRow, float gap) => header + rows * (numberLine + dotRow) + Math.Max(0, rows - 1) * gap;

        /// <summary>The fit in <paramref name="body"/> (the card's content height under its head), in the order the class names.</summary>
        public static Result Fit(float body, float dense, float header, int rows, float numberLine, float dotRow, float gap, float dotFloor, float gapFloor)
        {
            float full = GridHeight(header, rows, numberLine, dotRow, gap);
            if (dense > 0f && full + dense <= body) { return new Result { Fits = true, DenseKept = true, DotRow = dotRow, Gap = gap, Need = full + dense }; }
            if (full <= body) { return new Result { Fits = true, DenseKept = false, DotRow = dotRow, Gap = gap, Need = full }; }
            float floor = GridHeight(header, rows, numberLine, dotFloor, gapFloor);
            return new Result { Fits = floor <= body, DenseKept = false, DotRow = dotFloor, Gap = gapFloor, Need = floor };
        }
    }
}
