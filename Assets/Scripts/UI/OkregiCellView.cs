using System.Collections.Generic;
using PoliSim.Elections.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// D-PL (Design's boards 28a-28b, built on Elias's go-ahead of 2026-10-23): <b>THE 41 OKREGI AS SEAT CELLS</b> at the centre of Poland's Sejm night -
    /// one cell per deputy, 460 in all, each okreg a contiguous block of its own magnitude, the voivodeship borders as wide paper gaps and the okreg
    /// borders as narrow ones, the okreg's PKW number printed in its label cell.
    ///
    /// <para><b>The geometry is the board's</b> (`PolishOkregiCartogram`, generated from 28a's own table): a 27 × 24 grid at board 28a's pitch - 18 px a
    /// cell, the cell 17 of them - scaled uniformly into the rect the page gives it, centred; the positions are never re-derived.</para>
    ///
    /// <para><b>What a cell says</b> (28b's answer 2): a declared okreg's cells take the inks of the seats it returned that no open call can change,
    /// in the night's fixed key order and the cells' reading order; a cell waiting on an open call is paper with a hairline (in play); an okreg not yet
    /// declared is the plate with its outline dashed - not yet, not nil. A list with no ink on disk (the minority's) draws in the Neutral.</para>
    ///
    /// <para>The cells are painted once per layout into one texture at the device's own pixels; the numbers are Canvas text over it, so the text guard
    /// measures them.</para>
    /// </summary>
    public sealed class OkregiCellView : MonoBehaviour
    {
        /// <summary>[AUTHORED] layout - board 28a's pitch and cell, in its own pixels (its script: <c>P = 18</c>, cells 17 px).</summary>
        public const float Pitch = 18f, CellSide = 17f;

        /// <summary>[AUTHORED] layout - board 28a's gap widths, in its pixels: an okreg border 2, a voivodeship border 5.</summary>
        private const float OkregGap = 2f, VoivodeshipGap = 5f;

        /// <summary>The map's extent in board pixels.</summary>
        private static float BoardWidth => PolishOkregiCartogram.Columns * Pitch;
        private static float BoardHeight => PolishOkregiCartogram.Rows * Pitch;

        /// <summary>One okreg as the night stands: declared or not, and the fill of each of its cells in reading order (null for a cell in play).</summary>
        public struct OkregFill
        {
            public bool Declared;
            public Color?[] Cells;
            /// <summary>The slip's head and lines - the city, the voivodeship, the magnitude and what has declared there.</summary>
            public string SlipHead;
            public List<string> SlipLines;
        }

        private OkregFill[] _fills;
        private Transform _overlay;
        private Vector2 _built = new Vector2(-1f, -1f);

        /// <summary>The texture the cells were last painted into - destroyed when they are painted again and when the view goes (the review's L5: a
        /// texture per layout, never freed, at 3.2 MB a build at 2560).</summary>
        private Texture2D _painted;

        private void OnDestroy()
        {
            if (_painted != null) { Destroy(_painted); _painted = null; }
        }

        /// <summary>The cells painted in the last layout, by state - read by the film's log line.</summary>
        public int LastInked, LastInPlay, LastNotYet;

        public static OkregiCellView Create(Transform parent, OkregFill[] fills, Transform overlay)
        {
            var go = new GameObject("Okregi");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 240f;
            var view = go.AddComponent<OkregiCellView>();
            view._fills = fills;
            view._overlay = overlay;
            return view;
        }

        /// <summary>A cell's fill in reading order: the okreg's cells sorted by row, then column.</summary>
        private static int[] ReadingOrder(PolishOkregiCartogram.Okreg okreg)
        {
            int n = okreg.Cells.GetLength(0);
            var order = new int[n];
            for (int i = 0; i < n; i++) { order[i] = i; }
            System.Array.Sort(order, (a, b) => okreg.Cells[a, 1] != okreg.Cells[b, 1] ? okreg.Cells[a, 1].CompareTo(okreg.Cells[b, 1]) : okreg.Cells[a, 0].CompareTo(okreg.Cells[b, 0]));
            return order;
        }

        private void LateUpdate()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            if (Mathf.Abs(size.x - _built.x) > 0.5f || Mathf.Abs(size.y - _built.y) > 0.5f) { Rebuild(); }
        }

        public void Rebuild()
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.rect.size;
            _built = size;
            for (int i = transform.childCount - 1; i >= 0; i--) { Destroy(transform.GetChild(i).gameObject); }
            PolishOkregiCartogram.Okreg[] okregi = PolishOkregiCartogram.Okregi;
            if (size.x < 50f || size.y < 50f || _fills == null || _fills.Length != okregi.Length) { return; }
            float scale = Mathf.Min(size.x / BoardWidth, size.y / BoardHeight);
            float originX = (size.x - BoardWidth * scale) * 0.5f;
            float originY = (size.y - BoardHeight * scale) * 0.5f;

            // the texture at the device's pixels: board pixels × scale (canvas units) × the canvas's factor
            float device = Mathf.Max(0.25f, CanvasChrome.ScaleFactor());
            float k = scale * device;
            int w = Mathf.Max(1, Mathf.CeilToInt(BoardWidth * k)), h = Mathf.Max(1, Mathf.CeilToInt(BoardHeight * k));
            var pixels = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }
            void Fill(float x, float y, float fw, float fh, Color32 c)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(x * k), 0, w), x1 = Mathf.Clamp(Mathf.RoundToInt((x + fw) * k), 0, w);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(y * k), 0, h), y1 = Mathf.Clamp(Mathf.RoundToInt((y + fh) * k), 0, h);
                if (x1 <= x0 && fw > 0f) { x1 = Mathf.Min(w, x0 + 1); }
                if (y1 <= y0 && fh > 0f) { y1 = Mathf.Min(h, y0 + 1); }
                for (int py = y0; py < y1; py++) { int row = (h - 1 - py) * w; for (int px = x0; px < x1; px++) { pixels[row + px] = c; } }
            }

            Color32 plate = PoliSimTheme.CardInset, paper = PoliSimTheme.Card, rule = PoliSimTheme.HairlineStrong;
            LastInked = LastInPlay = LastNotYet = 0;
            for (int o = 0; o < okregi.Length; o++)
            {
                PolishOkregiCartogram.Okreg okreg = okregi[o];
                OkregFill fill = _fills[o];
                int[] order = ReadingOrder(okreg);
                for (int r = 0; r < order.Length; r++)
                {
                    int cell = order[r];
                    float cx = okreg.Cells[cell, 0] * Pitch + 0.5f, cy = okreg.Cells[cell, 1] * Pitch + 0.5f;
                    if (!fill.Declared) { Fill(cx, cy, CellSide, CellSide, plate); LastNotYet++; continue; }
                    Color? ink = fill.Cells != null && r < fill.Cells.Length ? fill.Cells[r] : null;
                    if (ink.HasValue) { Fill(cx, cy, CellSide, CellSide, ink.Value); LastInked++; continue; }
                    // in play: paper with a hairline inside its edge
                    Fill(cx, cy, CellSide, CellSide, rule);
                    float inset = 1f / Mathf.Max(0.5f, k);
                    Fill(cx + inset, cy + inset, CellSide - 2f * inset, CellSide - 2f * inset, paper);
                    LastInPlay++;
                }
            }
            // the borders: gaps of the page's paper over the cells (transparent, so the document shows)
            void Gaps(int[,] segments, float width)
            {
                for (int s = 0; s < segments.GetLength(0); s++)
                {
                    float x1 = segments[s, 0] * Pitch, y1 = segments[s, 1] * Pitch, x2 = segments[s, 2] * Pitch, y2 = segments[s, 3] * Pitch;
                    if (Mathf.Approximately(y1, y2)) { Fill(x1, y1 - width * 0.5f, x2 - x1, width, clear); }
                    else { Fill(x1 - width * 0.5f, y1, width, y2 - y1, clear); }
                }
            }
            Gaps(PolishOkregiCartogram.OkregBorders, OkregGap);
            Gaps(PolishOkregiCartogram.VoivodeshipBorders, VoivodeshipGap);
            // an undeclared okreg's outline, dashed
            for (int o = 0; o < okregi.Length; o++)
            {
                if (_fills[o].Declared) { continue; }
                int[,] outline = okregi[o].Outline;
                for (int s = 0; s < outline.GetLength(0); s++)
                {
                    float x1 = outline[s, 0] * Pitch, y1 = outline[s, 1] * Pitch, x2 = outline[s, 2] * Pitch, y2 = outline[s, 3] * Pitch;
                    const float dash = 4f, gap = 3f, thick = 1.5f;
                    if (Mathf.Approximately(y1, y2)) { for (float t = x1; t < x2; t += dash + gap) { Fill(t, y1 - thick * 0.5f, Mathf.Min(dash, x2 - t), thick, rule); } }
                    else { for (float t = y1; t < y2; t += dash + gap) { Fill(x1 - thick * 0.5f, t, thick, Mathf.Min(dash, y2 - t), rule); } }
                }
            }
            if (_painted != null) { Destroy(_painted); }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _painted = texture;
            texture.SetPixels32(pixels);
            texture.Apply();
            var art = new GameObject("Cells");
            art.transform.SetParent(transform, false);
            RawImage image = art.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            Place(image.rectTransform, originX, originY, BoardWidth * scale, BoardHeight * scale);

            // the numbers: each in its label cell, light on an inked cell and muted on paper or plate; the slip hangs on the number
            int labelSize = Mathf.Max(11, Mathf.RoundToInt(14f * scale));   // board 28a's 14 px number, at the map's own scale
            for (int o = 0; o < okregi.Length; o++)
            {
                PolishOkregiCartogram.Okreg okreg = okregi[o];
                OkregFill fill = _fills[o];
                bool inked = false;
                int[] order = ReadingOrder(okreg);
                for (int r = 0; r < order.Length; r++)
                {
                    int cell = order[r];
                    if (okreg.Cells[cell, 0] == okreg.LabelColumn && okreg.Cells[cell, 1] == okreg.LabelRow) { inked = fill.Declared && fill.Cells != null && r < fill.Cells.Length && fill.Cells[r].HasValue; }
                }
                float lx = originX + (okreg.LabelColumn * Pitch - Pitch * 0.5f) * scale, ly = originY + (okreg.LabelRow * Pitch - Pitch * 0.25f) * scale;
                Text t = CanvasChrome.MakeTextRealWeight(transform, "Okreg " + okreg.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    okreg.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), PoliSimTheme.Display, labelSize, inked ? PoliSimTheme.Card : PoliSimTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                Place(t.rectTransform, lx, ly, Pitch * 2f * scale, Pitch * 1.5f * scale);
                if (fill.SlipHead != null) { CanvasRows.Slip(t.gameObject, _overlay, fill.SlipHead, fill.SlipLines ?? new List<string>()); }
            }
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }
    }
}
