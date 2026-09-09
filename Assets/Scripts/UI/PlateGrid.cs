using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D16 §3.2 and §8.4 (2026-09-09, §413): the People plate's three grids, in ONE place, so the drawing and the acceptance bar read the
    /// same numbers. The board's tracks are given against its 1119 of content at gap 16 inside the 1149 sheet, and every width here is a
    /// share of that, so the ratios hold at any window size.
    ///
    /// <para><b>The state rule the bar enforces:</b> PROVENANCE adds LINES, never COLUMNS. The first five tracks of the PROVENANCE grid
    /// are not equal to READING's - the band pays for the honesty column - but the boundaries the eye reads down a column, the row order
    /// and every figure's x-position must be identical in both states. That is what `D16AcceptanceCheck` asserts, and it is why the band is
    /// the only track that flexes: **the figure cell's own left and right edge are the same number in both grids.**</para>
    /// </summary>
    public static class PlateGrid
    {
        public const float Content = 1119f;
        public const float Gap = 16f;

        /// <summary>READING: name · figure · band · pips · sparkline.</summary>
        public static readonly float[] Reading = { 296f, 132f, 543f, 40f, 44f };

        /// <summary>PROVENANCE: the same five, the band paying for a sixth - the honesty column.</summary>
        public static readonly float[] Provenance = { 296f, 132f, 453f, 40f, 44f, 74f };

        /// <summary>A gap row: name · the word · the reason, the reason taking the band and both marks' lanes.</summary>
        public static readonly float[] GapRow = { 296f, 132f, 659f };

        /// <summary>The x boundaries of a grid laid into an area - one more than the track count, the first the area's own left edge.</summary>
        public static float[] Tracks(Rect area, float[] tracks)
        {
            var x = new float[tracks.Length + 1];
            x[0] = area.x;
            float cursor = 0f;
            for (int i = 0; i < tracks.Length; i++)
            {
                cursor += tracks[i];
                x[i + 1] = area.x + area.width * (cursor / Content);
                if (i < tracks.Length - 1) { cursor += Gap; }
            }
            return x;
        }

        /// <summary>The grid a state draws by.</summary>
        public static float[] For(bool provenance) => provenance ? Provenance : Reading;
    }
}
