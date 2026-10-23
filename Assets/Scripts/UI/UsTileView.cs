using System.Collections.Generic;
using System.Globalization;
using PoliSim.Elections.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-34 (boards 29a-29b, a PROPOSAL built on Elias's go-ahead of 2026-10-23): <b>THE ELECTORAL COLLEGE AS TILES</b> at the centre of the USA's
    /// election night - one cell per elector, 538 in all, each jurisdiction one contiguous block of its electors, the borders between states as paper
    /// gaps, the postal code on one cell, Maine's and Nebraska's district electors numbered in their own cells; Alaska and Hawaii in the inset corner
    /// behind a dotted rule.
    ///
    /// <para><b>The geometry is the board's</b> (`UsTileCartogram`, generated from 29a's own table): a 32 × 20 grid at the board's pitch - 18 px a cell,
    /// the cell 17 of them - scaled uniformly into the rect the page gives it, centred; the positions are never re-derived.</para>
    ///
    /// <para><b>Three lenses on the same cells</b> (29b's answers 2 and 6): the President's - each elector in the ink of its call; Congress - a state's
    /// electors are its House seats and its two senators, so its two at-large cells (the first two in reading order elsewhere) are its senators, inked
    /// where the seat is on tonight's ballot and hatched where not, and the rest its delegation, REP first in reading order; the District's cells
    /// dotted, no vote in Congress - and Delegations, shown only when the College has no majority: each state whole by its delegation's majority, a
    /// tied delegation dotted. A state not yet declared is the plate with its outline dashed - not yet, not nil - in every lens.</para>
    ///
    /// <para>The cells are painted once per layout into one texture at the device's own pixels; the codes and numbers are Canvas text over it, so the
    /// text guard measures them.</para>
    /// </summary>
    public sealed class UsTileView : MonoBehaviour
    {
        /// <summary>[AUTHORED] layout - board 29a's pitch and cell, in its own pixels (tiles.json: pitch 18, cell 17).</summary>
        public const float Pitch = 18f, CellSide = 17f;

        /// <summary>[AUTHORED] layout - board 29a's border between two states, in its pixels (its segments, 3 px).</summary>
        private const float BorderGap = 3f;

        /// <summary>[AUTHORED] layout - board 29a's inset corner for Alaska and Hawaii, in its pixels: the dotted rule along the top at 252 and down the right
        /// at 106, to the map's foot at 360.</summary>
        private const float InsetTop = 252f, InsetRight = 106f;

        /// <summary>[AUTHORED] layout - board 29a's hatch for a senator not up: a 1.5 px stripe every 5 px on the diagonal.</summary>
        private const float HatchPeriod = 5f, HatchStripe = 1.5f;

        /// <summary>[AUTHORED] layout - the night's 14 px floor in canvas units: 14 board px at the screen's 1.5 units a board pixel (`ElectionNightScreen`'s BoardPx), so a squeezed
        /// map never draws its codes smaller than the rest of the night's text.</summary>
        private const int LabelFloor = 21;

        public enum Lens { President, Congress, Delegations }

        /// <summary>How a cell is painted: the plate (not yet declared), an ink, the hatch (a senator not up), or paper with a dotted rule (no vote).</summary>
        public enum Paint { NotYet, Ink, Hatch, Dotted }

        public struct Cell
        {
            public Paint Paint;
            public Color Ink;
        }

        /// <summary>One jurisdiction as the night stands, in `UsTileCartogram.States`' order: declared or not; its cells in reading order under each lens;
        /// its slip, and each district cell's (Maine's, Nebraska's) in the order of its cells.</summary>
        public sealed class BlockFill
        {
            public bool Declared;
            public Cell[] President, Congress, Delegations;
            public string SlipHead;
            public List<string> SlipLines;
            public Dictionary<int, string> DistrictSlipHeads = new Dictionary<int, string>();
            public Dictionary<int, List<string>> DistrictSlipLines = new Dictionary<int, List<string>>();
        }

        private BlockFill[] _fills;
        private Transform _overlay;
        private Vector2 _built = new Vector2(-1f, -1f);
        private Texture2D _painted;

        /// <summary>The lens the cells are drawn in.</summary>
        public Lens Current { get; private set; }

        /// <summary>The cells painted in the last layout, by kind - read by the film's log line.</summary>
        public int LastInked, LastNotYet, LastHatched, LastDotted;

        private void OnDestroy()
        {
            if (_painted != null) { Destroy(_painted); _painted = null; }
        }

        public static UsTileView Create(Transform parent, BlockFill[] fills, Transform overlay, Lens lens)
        {
            var go = new GameObject("UsTiles");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 240f;
            var view = go.AddComponent<UsTileView>();
            view._fills = fills;
            view._overlay = overlay;
            view.Current = lens;
            return view;
        }

        /// <summary>The cells re-inked under another lens.</summary>
        public void SetLens(Lens lens)
        {
            if (lens == Current) { return; }
            Current = lens;
            Rebuild();
        }

        private void LateUpdate()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            if (Mathf.Abs(size.x - _built.x) > 0.5f || Mathf.Abs(size.y - _built.y) > 0.5f) { Rebuild(); }
        }

        private Cell[] CellsOf(BlockFill fill) => Current == Lens.Congress ? fill.Congress : Current == Lens.Delegations ? fill.Delegations : fill.President;

        public void Rebuild()
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.rect.size;
            _built = size;
            for (int i = transform.childCount - 1; i >= 0; i--) { Destroy(transform.GetChild(i).gameObject); }
            UsTileCartogram.Block[] blocks = UsTileCartogram.States;
            float boardWidth = UsTileCartogram.Columns * Pitch, boardHeight = UsTileCartogram.Rows * Pitch;
            if (size.x < 50f || size.y < 50f || _fills == null || _fills.Length != blocks.Length) { return; }
            float scale = Mathf.Min(size.x / boardWidth, size.y / boardHeight);
            float originX = (size.x - boardWidth * scale) * 0.5f;
            float originY = (size.y - boardHeight * scale) * 0.5f;

            float device = Mathf.Max(0.25f, CanvasChrome.ScaleFactor());
            float k = scale * device;
            int w = Mathf.Max(1, Mathf.CeilToInt(boardWidth * k)), h = Mathf.Max(1, Mathf.CeilToInt(boardHeight * k));
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
            void Hatch(float x, float y)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(x * k), 0, w), x1 = Mathf.Clamp(Mathf.RoundToInt((x + CellSide) * k), 0, w);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(y * k), 0, h), y1 = Mathf.Clamp(Mathf.RoundToInt((y + CellSide) * k), 0, h);
                Color32 paper = PoliSimTheme.Card, stripe = PoliSimTheme.Hairline;
                for (int py = y0; py < y1; py++)
                {
                    int row = (h - 1 - py) * w;
                    for (int px = x0; px < x1; px++)
                    {
                        float along = (px + py) / k;
                        pixels[row + px] = along - Mathf.Floor(along / HatchPeriod) * HatchPeriod < HatchStripe ? stripe : paper;
                    }
                }
            }
            void Dots(float x1, float y1, float x2, float y2, Color32 c)
            {
                const float dot = 1f, gap = 2f, thick = 1f;
                if (Mathf.Approximately(y1, y2)) { for (float t = x1; t < x2; t += dot + gap) { Fill(t, y1 - thick * 0.5f, Mathf.Min(dot, x2 - t), thick, c); } }
                else { for (float t = y1; t < y2; t += dot + gap) { Fill(x1 - thick * 0.5f, t, thick, Mathf.Min(dot, y2 - t), c); } }
            }

            Color32 plate = PoliSimTheme.CardInset, paperInk = PoliSimTheme.Card, rule = PoliSimTheme.HairlineStrong, dotted = PoliSimTheme.TextPrimary;
            LastInked = LastNotYet = LastHatched = LastDotted = 0;
            for (int b = 0; b < blocks.Length; b++)
            {
                UsTileCartogram.Block block = blocks[b];
                BlockFill fill = _fills[b];
                Cell[] cells = CellsOf(fill);
                for (int r = 0; r < block.Cells.GetLength(0); r++)
                {
                    float cx = block.Cells[r, 0] * Pitch + 0.5f, cy = block.Cells[r, 1] * Pitch + 0.5f;
                    Cell cell = cells != null && r < cells.Length ? cells[r] : new Cell { Paint = Paint.NotYet };
                    if (!fill.Declared || cell.Paint == Paint.NotYet) { Fill(cx, cy, CellSide, CellSide, plate); LastNotYet++; }
                    else if (cell.Paint == Paint.Ink) { Fill(cx, cy, CellSide, CellSide, cell.Ink); LastInked++; }
                    else if (cell.Paint == Paint.Hatch) { Hatch(cx, cy); LastHatched++; }
                    else
                    {
                        Fill(cx, cy, CellSide, CellSide, paperInk);
                        Dots(cx, cy, cx + CellSide, cy, dotted); Dots(cx, cy + CellSide - 1f, cx + CellSide, cy + CellSide - 1f, dotted);
                        Dots(cx + 0.5f, cy, cx + 0.5f, cy + CellSide, dotted); Dots(cx + CellSide - 0.5f, cy, cx + CellSide - 0.5f, cy + CellSide, dotted);
                        LastDotted++;
                    }
                }
            }
            // the borders between states: gaps of the page's paper over the cells (transparent, so the document shows)
            int[,] borders = UsTileCartogram.Borders;
            for (int s = 0; s < borders.GetLength(0); s++)
            {
                float x1 = borders[s, 0] * Pitch, y1 = borders[s, 1] * Pitch, x2 = borders[s, 2] * Pitch, y2 = borders[s, 3] * Pitch;
                if (Mathf.Approximately(y1, y2)) { Fill(x1, y1 - BorderGap * 0.5f, x2 - x1, BorderGap, clear); }
                else { Fill(x1 - BorderGap * 0.5f, y1, BorderGap, y2 - y1, clear); }
            }
            // a state not yet declared: its outline dashed
            for (int b = 0; b < blocks.Length; b++)
            {
                if (_fills[b].Declared) { continue; }
                int[,] outline = blocks[b].Outline;
                for (int s = 0; s < outline.GetLength(0); s++)
                {
                    float x1 = outline[s, 0] * Pitch, y1 = outline[s, 1] * Pitch, x2 = outline[s, 2] * Pitch, y2 = outline[s, 3] * Pitch;
                    const float dash = 4f, gap = 3f, thick = 1.5f;
                    if (Mathf.Approximately(y1, y2)) { for (float t = x1; t < x2; t += dash + gap) { Fill(t, y1 - thick * 0.5f, Mathf.Min(dash, x2 - t), thick, rule); } }
                    else { for (float t = y1; t < y2; t += dash + gap) { Fill(x1 - thick * 0.5f, t, thick, Mathf.Min(dash, y2 - t), rule); } }
                }
            }
            // the inset corner's dotted rule
            Color32 inset = PoliSimTheme.Hairline;
            Dots(0f, InsetTop, InsetRight, InsetTop, inset);
            Dots(InsetRight, InsetTop, InsetRight, boardHeight, inset);

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
            Place(image.rectTransform, originX, originY, boardWidth * scale, boardHeight * scale);

            // the codes, and the district numbers in the President's lens: light on an inked cell, muted on paper or plate; the slips hang on them
            int labelSize = Mathf.Max(LabelFloor, Mathf.RoundToInt(14f * scale));   // board 29a's 14 px code at the map's own scale, never under the night's 14 px floor (the review's L5)
            for (int b = 0; b < blocks.Length; b++)
            {
                UsTileCartogram.Block block = blocks[b];
                BlockFill fill = _fills[b];
                Cell[] cells = CellsOf(fill);
                for (int r = 0; r < block.Cells.GetLength(0); r++)
                {
                    int district = block.Cells[r, 2];
                    bool isLabel = block.Cells[r, 0] == block.LabelColumn && block.Cells[r, 1] == block.LabelRow;
                    if (!isLabel && (district == 0 || Current != Lens.President)) { continue; }
                    bool inked = fill.Declared && cells != null && r < cells.Length && cells[r].Paint == Paint.Ink;
                    string text = isLabel ? block.Code : district.ToString(CultureInfo.InvariantCulture);
                    float lx = originX + (block.Cells[r, 0] * Pitch - Pitch * 0.5f) * scale, ly = originY + (block.Cells[r, 1] * Pitch - Pitch * 0.25f) * scale;
                    Text t = CanvasChrome.MakeTextRealWeight(transform, (isLabel ? "State " : "District ") + text, text, PoliSimTheme.Display, labelSize,
                        inked ? PoliSimTheme.Card : PoliSimTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    Place(t.rectTransform, lx, ly, Pitch * 2f * scale, Pitch * 1.5f * scale);
                    if (isLabel && fill.SlipHead != null) { CanvasRows.Slip(t.gameObject, _overlay, fill.SlipHead, fill.SlipLines ?? new List<string>()); }
                    else if (!isLabel && fill.DistrictSlipHeads.TryGetValue(district, out string head)) { CanvasRows.Slip(t.gameObject, _overlay, head, fill.DistrictSlipLines[district]); }
                }
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
