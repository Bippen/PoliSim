using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// **Board 4a — the valkrets cartogram, as geometry a check can hold** (2026-09-10, election night item 2).
    ///
    /// <para><b>What the board rules.</b> Twenty-nine tiles in ELEVEN latitude bands, west to east inside each band,
    /// and one line of arithmetic: *a band's height is k · Σmandat ⁄ bandwidth and a tile's width is
    /// bandwidth · mandat ⁄ Σmandat, so every tile's area is exactly k · mandat* - where mandat is the valkrets'
    /// FIXED seats (310 of the Riksdag's 349). k scales with the map rect and nothing else changes. **The band left
    /// edges and widths are the only authored numbers**: they carry the coastline. The Valmyndigheten numbering 01–29
    /// is deliberately NOT the arrangement - a numbering is not a geography.</para>
    ///
    /// <para><b>What is ours and what is theirs.</b> The mandates are OUR column - `SeatConversion.FixedSeatsPerRegion`
    /// over each valkrets' eligible electorate, the derived column board 4a consumed and re-added to 310 - passed in,
    /// never copied here. The bands, their members, their left edges and widths are board 4a's, read off the delivered
    /// mock (`PoliSim v2 Screens.dc.html`, the `4a` screen, in the D16 and D18 handoff zips) in its own 1080-unit map rect;
    /// the board states the arrangement and the formula in words and carries the edges only as the mock's inline
    /// geometry, so the edges below are a READING of that geometry, recorded as such.</para>
    ///
    /// <para>⚠ The mock's tiles do not hold one k exactly (it rendered between ~1080 and ~1120 px² per mandate, the
    /// residue of rounding each height to a tenth). This file applies the formula EXACTLY with one k per map, which is
    /// what the board's sentence says; `ValkretsCartogramCheck` holds every tile to it.</para>
    /// </summary>
    public static class ValkretsCartogram
    {
        /// <summary>The board's own map rect, in its units - the frame the band edges are read in.</summary>
        public const float BoardWidth = 1080f;
        public const float BoardHeight = 587.6f;
        /// <summary>The board's gutter between tiles and between bands.</summary>
        public const float BoardGutter = 2f;

        /// <summary>Board 4a's tile ladder, in board units: FULL carries name, lead mark, margin and swing; COMPACT drops
        /// the swing; MINIMAL (Gotland alone on the board) carries a code, the mark and the margin.</summary>
        public const float FullMinWidth = 185f;
        public const float FullMinHeight = 44f;
        public const float CompactMinWidth = 88f;

        public enum Level { Full, Compact, Minimal }

        /// <summary>One band: its members west to east (catalog indices, the Valmyndigheten order the returns and the
        /// model use), and the left edge and width it spans in board units.</summary>
        public readonly struct Band
        {
            public readonly int[] Members;
            public readonly float Left;
            public readonly float Width;

            public Band(float left, float width, params int[] members) { Left = left; Width = width; Members = members; }
        }

        /// <summary>The eleven bands, north to south. Indices are `SwedishValkretsReturns2022.Names`' order:
        /// 0 Stockholms län · 1 Stockholms kommun · 2 Uppsala · 3 Södermanland · 4 Östergötland · 5 Jönköping ·
        /// 6 Kronoberg · 7 Kalmar · 8 Gotland · 9 Blekinge · 10 Skåne västra · 11 Skåne södra · 12 Skåne norra och
        /// östra · 13 Malmö · 14 Halland · 15 VG västra · 16 VG norra · 17 VG södra · 18 VG östra · 19 Göteborg ·
        /// 20 Värmland · 21 Örebro · 22 Västmanland · 23 Dalarna · 24 Gävleborg · 25 Västernorrland · 26 Jämtland ·
        /// 27 Västerbotten · 28 Norrbotten.</summary>
        public static readonly Band[] Bands =
        {
            new Band(330f, 188f, 28),                  // B1  Norrbotten
            new Band(330f, 188f, 27),                  // B2  Västerbotten
            new Band(235f, 283f, 26, 25),              // B3  Jämtland · Västernorrland
            new Band(116f, 426f, 23, 24),              // B4  Dalarna · Gävleborg
            new Band(90f, 898f, 20, 21, 22, 2),        // B5  Värmland · Örebro · Västmanland · Uppsala
            new Band(0f, 1078f, 15, 16, 3, 0, 1),      // B6  VG västra · VG norra · Södermanland · Stockholms län · Stockholms kommun
            new Band(120f, 712f, 17, 18, 4, 8),        // B7  VG södra · VG östra · Östergötland · Gotland
            new Band(100f, 718f, 19, 5, 7),            // B8  Göteborg · Jönköping · Kalmar
            new Band(120f, 618f, 14, 6, 9),            // B9  Halland · Kronoberg · Blekinge
            new Band(140f, 518f, 10, 12),              // B10 Skåne västra · Skåne norra och östra
            new Band(150f, 478f, 13, 11),              // B11 Malmö · Skåne södra
        };

        /// <summary>Board 4a's own tile labels, catalog order - the names the board writes, not the catalog's
        /// long forms, so a tile of 88 units can carry one.</summary>
        public static readonly string[] Labels =
        {
            "STOCKHOLMS LÄN", "STOCKHOLMS KOMMUN", "UPPSALA", "SÖDERMANLAND", "ÖSTERGÖTLAND", "JÖNKÖPING",
            "KRONOBERG", "KALMAR", "GOTLAND", "BLEKINGE", "SKÅNE VÄSTRA", "SKÅNE SÖDRA", "SKÅNE NORRA OCH ÖSTRA",
            "MALMÖ", "HALLAND", "VG VÄSTRA", "VG NORRA", "VG SÖDRA", "VG ÖSTRA", "GÖTEBORG", "VÄRMLAND", "ÖREBRO",
            "VÄSTMANLAND", "DALARNA", "GÄVLEBORG", "VÄSTERNORRLAND", "JÄMTLAND", "VÄSTERBOTTEN", "NORRBOTTEN",
        };

        /// <summary>[AUTHORED-DRAFT] the MINIMAL tile's code: the län letter the board uses for Gotland ("I"), and the same
        /// system for the rest, with the split valkretsar marked by a suffix - a tile below COMPACT width carries this in
        /// place of its name. Only Gotland drops this far on the board; at small map rects others can.</summary>
        public static readonly string[] Codes =
        {
            "AB", "AB·K", "C", "D", "E", "F", "G", "H", "I", "K", "M·V", "M·S", "M·NÖ", "M·MA", "N",
            "O·V", "O·N", "O·S", "O·Ö", "O·GB", "S", "T", "U", "W", "X", "Y", "Z", "AC", "BD",
        };

        /// <summary>One laid tile, in the caller's units, with the ladder level its size earns.</summary>
        public readonly struct Tile
        {
            public readonly int Region;
            public readonly int BandIndex;
            public readonly float X, Y, W, H;
            public readonly Level Level;

            public Tile(int region, int band, float x, float y, float w, float h, Level level)
            {
                Region = region; BandIndex = band; X = x; Y = y; W = w; H = h; Level = level;
            }
        }

        /// <summary>The whole layout: the tiles, the k it used, and the scale from board units.</summary>
        public sealed class Layout
        {
            public readonly List<Tile> Tiles = new List<Tile>();
            /// <summary>Area per mandate, in the caller's squared units.</summary>
            public float K;
            /// <summary>Caller units per board unit, horizontally - the band edges are placed with it.</summary>
            public float Scale;
            /// <summary>Per band: the y of its top, its height and its mandate sum.</summary>
            public readonly List<(float Y, float H, int Mandates)> BandRows = new List<(float, float, int)>();
        }

        /// <summary>Σ mandat per band.</summary>
        public static int BandMandates(int band, IReadOnlyList<int> mandates)
        {
            int sum = 0;
            foreach (int r in Bands[band].Members) { sum += mandates[r]; }
            return sum;
        }

        /// <summary>
        /// Lay the 29 tiles into a rect of <paramref name="width"/> × <paramref name="height"/>, origin at its top-left.
        /// The horizontal frame scales the board's edges; k is then chosen so the bands, stacked with their gutters,
        /// fill the height - and every tile's area is k · mandat by construction (width share × band height).
        /// </summary>
        /// <param name="ladderUnit">Caller units per board pixel FOR THE LADDER. The ladder is a statement about room for
        /// text (the board's name line is 9 px), so a caller drawing larger type passes its type ratio here and a small map
        /// drops tiles to COMPACT or MINIMAL rather than cramming them. Omitted, the ladder scales with the map.</param>
        public static Layout Lay(float width, float height, IReadOnlyList<int> mandates, float ladderUnit = -1f)
        {
            if (mandates == null || mandates.Count != Labels.Length) { throw new ArgumentException("the cartogram needs one mandate per valkrets (29)"); }
            var layout = new Layout { Scale = width / BoardWidth };
            float gutter = BoardGutter * layout.Scale;
            float ladder = ladderUnit > 0f ? ladderUnit : layout.Scale;

            // Σ_b Σm_b / innerWidth_b - the height one unit of k buys, summed over the bands.
            double perK = 0.0;
            var inner = new float[Bands.Length];
            for (int b = 0; b < Bands.Length; b++)
            {
                inner[b] = Bands[b].Width * layout.Scale - gutter * (Bands[b].Members.Length - 1);
                perK += BandMandates(b, mandates) / (double)inner[b];
            }

            float available = height - gutter * (Bands.Length - 1);
            layout.K = (float)(available / perK);

            float y = 0f;
            for (int b = 0; b < Bands.Length; b++)
            {
                int sum = BandMandates(b, mandates);
                float bandH = layout.K * sum / inner[b];
                float x = Bands[b].Left * layout.Scale;
                foreach (int r in Bands[b].Members)
                {
                    float w = inner[b] * mandates[r] / sum;
                    Level level = w >= FullMinWidth * ladder && bandH >= FullMinHeight * ladder ? Level.Full
                        : w >= CompactMinWidth * ladder ? Level.Compact : Level.Minimal;
                    layout.Tiles.Add(new Tile(r, b, x, y, w, bandH, level));
                    x += w + gutter;
                }

                layout.BandRows.Add((y, bandH, sum));
                y += bandH + gutter;
            }

            return layout;
        }
    }
}
