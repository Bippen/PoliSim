using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **Board 4a, held as a check** (2026-09-10, election night item 2). The board wrote its geometry as a sentence a check
    /// can hold - *every tile's area is exactly k · mandat* - and this holds it, with the three things the sentence rests on:
    ///
    /// <list type="number">
    /// <item>the ARRANGEMENT: eleven bands that between them carry each of the 29 valkretsar exactly once;</item>
    /// <item>the COLUMN: our derived fixed-seat column (`SeatConversion.FixedSeatsPerRegion` over the catalog's eligible
    /// electorate) equals the column board 4a consumed and re-added, row for row, and sums to 310 - so the map Design drew
    /// and the map this repo draws are sized by the same numbers;</item>
    /// <item>the FORMULA at several map rects: every tile's area is k · mandat to a thousandth, every tile sits inside the
    /// rect, and no two tiles overlap;</item>
    /// <item>the LADDER at the board's own size: the board's RULE, with Gotland alone at MINIMAL. ⚠ Not the board's stated
    /// 13 / 15 / 1 - its mock draws three tiles COMPACT that clear its own FULL rule, so the count and the rule disagree on the
    /// board itself, and the rule is what a check can hold; the count is printed beside ours.</item>
    /// </list>
    /// </summary>
    public static class ValkretsCartogramCheck
    {
        /// <summary>[SOURCED: board 4a, "THE COLUMN AS CONSUMED — RE-ADDED HERE, NOT TRUSTED"] the fixed mandates per valkrets as
        /// Design consumed them, catalog order (Valmyndigheten 01–29). Sum 310.</summary>
        private static readonly int[] BoardColumn =
        {
            40, 29, 12, 9, 14, 11, 6, 8, 2, 5, 9, 12, 10, 10, 10, 11, 8, 7, 8, 17, 9, 9, 8, 9, 9, 8, 4, 8, 8,
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ValkretsCartogramCheck: board 4a's eleven bands and its area formula over our mandate column ===\n");
            int failures = 0;

            // 1. The arrangement.
            var seen = new int[ValkretsCartogram.Labels.Length];
            foreach (ValkretsCartogram.Band band in ValkretsCartogram.Bands)
            {
                foreach (int r in band.Members) { seen[r]++; }
            }
            var wrong = new List<string>();
            for (int r = 0; r < seen.Length; r++) { if (seen[r] != 1) { wrong.Add($"{ValkretsCartogram.Labels[r]} x{seen[r]}"); } }
            failures += Assert(sb, "1. eleven bands carry each of the 29 valkretsar exactly once",
                ValkretsCartogram.Bands.Length == 11 && wrong.Count == 0,
                $"{ValkretsCartogram.Bands.Length} bands; " + (wrong.Count == 0 ? "29 of 29 once" : string.Join(", ", wrong)));
            failures += Assert(sb, "   and the catalog carries the same 29, in the order the bands index",
                SwedishValkretsReturns2022.Names.Length == ValkretsCartogram.Labels.Length,
                $"catalog {SwedishValkretsReturns2022.Names.Length}, labels {ValkretsCartogram.Labels.Length}");

            // 2. The column.
            var eligible = new double[SwedishValkretsReturns2022.Eligible.Length];
            for (int r = 0; r < eligible.Length; r++) { eligible[r] = SwedishValkretsReturns2022.Eligible[r]; }
            int[] ours = SeatConversion.FixedSeatsPerRegion(eligible);
            int sum = 0;
            var differ = new List<string>();
            for (int r = 0; r < ours.Length; r++)
            {
                sum += ours[r];
                if (ours[r] != BoardColumn[r]) { differ.Add($"{ValkretsCartogram.Labels[r]} ours {ours[r]} board {BoardColumn[r]}"); }
            }
            failures += Assert(sb, "2. our fixed-seat column equals the column board 4a consumed, row for row, and sums to 310",
                differ.Count == 0 && sum == SeatConversion.FixedSeats,
                differ.Count == 0 ? $"29 of 29 equal, sum {sum}" : string.Join("; ", differ));

            // 3. The formula, at the board's rect and at two the page can give.
            var rects = new[] { (ValkretsCartogram.BoardWidth, ValkretsCartogram.BoardHeight), (720f, 391.7f), (1400f, 762f), (900f, 600f) };
            foreach ((float w, float h) in rects)
            {
                ValkretsCartogram.Layout layout = ValkretsCartogram.Lay(w, h, ours);
                double worst = 0.0;
                int outside = 0, overlaps = 0;
                for (int i = 0; i < layout.Tiles.Count; i++)
                {
                    ValkretsCartogram.Tile t = layout.Tiles[i];
                    double area = t.W * t.H;
                    double expected = layout.K * ours[t.Region];
                    worst = Math.Max(worst, Math.Abs(area - expected) / expected);
                    if (t.X < -0.01f || t.Y < -0.01f || t.X + t.W > w + 0.01f || t.Y + t.H > h + 0.01f) { outside++; }
                    for (int j = i + 1; j < layout.Tiles.Count; j++)
                    {
                        ValkretsCartogram.Tile u = layout.Tiles[j];
                        bool apart = t.X + t.W <= u.X + 0.01f || u.X + u.W <= t.X + 0.01f || t.Y + t.H <= u.Y + 0.01f || u.Y + u.H <= t.Y + 0.01f;
                        if (!apart) { overlaps++; }
                    }
                }
                failures += Assert(sb, string.Format(CultureInfo.InvariantCulture, "3. at {0:0.#} x {1:0.#}: every tile's area is k · mandat, inside the rect, no overlap", w, h),
                    layout.Tiles.Count == 29 && worst < 1e-3 && outside == 0 && overlaps == 0,
                    string.Format(CultureInfo.InvariantCulture, "k {0:0.0}, worst area error {1:P3}, {2} outside, {3} overlapping", layout.K, worst, outside, overlaps));
            }

            // 4. The ladder at the board's own size.
            ValkretsCartogram.Layout board = ValkretsCartogram.Lay(ValkretsCartogram.BoardWidth, ValkretsCartogram.BoardHeight, ours);
            int full = 0, compact = 0, minimal = 0;
            var minimalNames = new List<string>();
            foreach (ValkretsCartogram.Tile t in board.Tiles)
            {
                if (t.Level == ValkretsCartogram.Level.Full) { full++; }
                else if (t.Level == ValkretsCartogram.Level.Compact) { compact++; }
                else { minimal++; minimalNames.Add(ValkretsCartogram.Labels[t.Region]); }
            }
            // ⚠ The board STATES 13 FULL / 15 COMPACT / 1 MINIMAL, and its own rule (FULL at w ≥ 185 and h ≥ 44) applied to its
            // own mock's rects gives 16 FULL: the mock draws Uppsala (282 × 46.6), Östergötland (314 × 48.9) and Jönköping
            // (218 × 55.5) compact although each clears the rule. The count is the mock's rendering, not the rule's - so the RULE
            // is what is held here, with Gotland alone at MINIMAL as the board says, and the stated count printed beside ours.
            failures += Assert(sb, "4. at the board's size the ladder RULE gives Gotland alone at MINIMAL, and every other tile FULL or COMPACT",
                minimal == 1 && minimalNames.Contains("GOTLAND") && full + compact == 28,
                $"{full} full, {compact} compact, {minimal} minimal ({string.Join(", ", minimalNames)}) - the board states 13 / 15 / 1; its mock draws "
                + $"Uppsala, Östergötland and Jönköping COMPACT though each clears its own FULL rule, which is the 3; k {board.K:0.0} against the mock's rendered 1080-1120");

            sb.Append("    the bands, as laid at the board's size (Σmandat, top, height):\n");
            for (int b = 0; b < board.BandRows.Count; b++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "      B{0,-2} {1,3}  y {2,6:0.0}  h {3,5:0.0}\n", b + 1, board.BandRows[b].Mandates, board.BandRows[b].Y, board.BandRows[b].H));
            }

            Debug.Log(sb.ToString());
            if (failures > 0)
            {
                Debug.LogError($"CARTOGRAM: {failures} assertion(s) failed - see above.");
                CheckExit.Finish(1);
                return;
            }
            Debug.Log("=== ValkretsCartogramCheck: ALL ASSERTIONS PASS ===");
            CheckExit.Finish(0);
        }

        private static int Assert(StringBuilder sb, string what, bool ok, string detail)
        {
            sb.Append(ok ? "    ok     " : "    FAIL   ").Append(what).Append(" - ").Append(detail).Append('\n');
            return ok ? 0 : 1;
        }
    }
}
