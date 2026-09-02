using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P2-4.3 (Playtest 2, 2026-09-02) — **the instruments as textures, for the Canvas takeovers.** The
    /// IMGUI sheets draw the per-seat vote map (P2-2.2) and the effect arrows (P2-2.1) per frame; the
    /// signing ceremony and election night are retained-mode Canvas screens, so the same instruments are
    /// painted once into a <see cref="Texture2D"/> and shown through a <c>RawImage</c>. The painting rules
    /// are the renderers' own - rings by capacity at a dot-and-a-third pitch, arrows from a baseline with
    /// length by size relative to the largest and ink by whether the move is good for that outcome - so the
    /// two surfaces cannot disagree about what a division or an estimate looks like.
    /// </summary>
    public static class CanvasPaint
    {
        private const int MinRows = 3;
        private const int MaxRows = 24;
        private const float DotFill = 0.62f;
        private const float DotPitch = 1.35f;
        private const float InnerRadiusFraction = 0.38f;

        /// <summary>The per-seat map: FOR left, UNDECIDED middle, AGAINST right, one dot per mandate, on a paper ground.</summary>
        public static Texture2D SeatMap(int width, int height, int forSeats, int undecidedSeats, int againstSeats, Color paper)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[width * height];
            Color32 ground = paper;
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = ground; }

            int total = forSeats + undecidedSeats + againstSeats;
            if (total > 0)
            {
                var inks = new List<Color32>(total);
                for (int i = 0; i < forSeats; i++) { inks.Add(PoliSimTheme.Good); }
                for (int i = 0; i < undecidedSeats; i++) { inks.Add(PoliSimTheme.TextMuted); }
                for (int i = 0; i < againstSeats; i++) { inks.Add(PoliSimTheme.Bad); }

                float outer = Mathf.Min(width * 0.5f - 2f, height - 2f);
                float inner = outer * InnerRadiusFraction;
                int rows = MinRows;
                float gap, dot;
                while (true)
                {
                    gap = (outer - inner) / (rows - 1);
                    dot = gap * DotFill;
                    int capacity = 0;
                    for (int r = 0; r < rows; r++) { capacity += Mathf.FloorToInt(Mathf.PI * (inner + r * gap) / (dot * DotPitch)); }
                    if (capacity >= total || rows >= MaxRows) { break; }
                    rows++;
                }
                float radiusSum = 0f;
                for (int r = 0; r < rows; r++) { radiusSum += inner + r * gap; }
                var perRow = new int[rows];
                int assigned = 0;
                for (int r = 0; r < rows; r++) { perRow[r] = Mathf.RoundToInt(total * ((inner + r * gap) / radiusSum)); assigned += perRow[r]; }
                perRow[rows - 1] += total - assigned;

                float baseX = width * 0.5f;
                float baseY = height - 1f;   // painted top-down; flipped when written
                int seat = 0;
                for (int r = 0; r < rows && seat < total; r++)
                {
                    int rowSeats = Mathf.Min(perRow[r], total - seat);
                    float radius = inner + r * gap;
                    for (int i = 0; i < rowSeats; i++)
                    {
                        float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                        float rad = angle * Mathf.Deg2Rad;
                        FillCircle(pixels, width, height, baseX + Mathf.Cos(rad) * radius, baseY - Mathf.Sin(rad) * radius, dot * 0.5f, inks[seat]);
                        seat++;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        /// <summary>
        /// The effect arrows: one lane per arrow, a vertical shaft from a baseline at mid-height, length by the
        /// value's size relative to the largest, a head at the tip, ink by whether the move is good for that
        /// outcome. Names and figures are the caller's text beneath and above the lanes.
        /// </summary>
        public static Texture2D Arrows(int width, int height, IReadOnlyList<EffectArrow> arrows, Color paper)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[width * height];
            Color32 ground = paper;
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = ground; }

            float maxAbs = 0f;
            if (arrows != null) { foreach (EffectArrow a in arrows) { maxAbs = Mathf.Max(maxAbs, Mathf.Abs(a.Value)); } }
            if (arrows != null && arrows.Count > 0 && maxAbs > 0f)
            {
                float lane = width / (float)arrows.Count;
                float half = height * 0.5f - 2f;
                float shaft = Mathf.Max(2f, Mathf.Round(Mathf.Min(lane, height) * 0.06f));
                float head = Mathf.Max(4f, Mathf.Round(half * 0.22f));
                Color32 rule = PoliSimTheme.Hairline;
                FillRect(pixels, width, height, 0f, height * 0.5f - 0.5f, width, 1f, rule);
                for (int i = 0; i < arrows.Count; i++)
                {
                    EffectArrow a = arrows[i];
                    float fraction = Mathf.Abs(a.Value) / maxAbs;
                    float length = Mathf.Max(half * 0.12f, half * fraction);
                    bool up = a.Value > 0f;
                    Color32 ink = UiPalette.GetDeltaColor(a.Value, a.HigherIsBetter);
                    float cx = lane * (i + 0.5f);
                    float baseline = height * 0.5f;
                    float tip = up ? baseline - length : baseline + length;
                    float shaftTop = up ? tip + head : baseline;
                    float shaftBottom = up ? baseline : tip - head;
                    FillRect(pixels, width, height, cx - shaft * 0.5f, shaftTop, shaft, Mathf.Max(0f, shaftBottom - shaftTop), ink);
                    FillTriangle(pixels, width, height, cx, tip, head, up, ink);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        // Painting helpers: y runs top-down in these calls and is flipped into the texture's bottom-up rows.
        private static void Put(Color32[] px, int w, int h, int x, int y, Color32 ink)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) { return; }
            px[(h - 1 - y) * w + x] = ink;
        }

        private static void FillCircle(Color32[] px, int w, int h, float cx, float cy, float r, Color32 ink)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r), y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            float rr = r * r;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= rr) { Put(px, w, h, x, y, ink); }
                }
            }
        }

        private static void FillRect(Color32[] px, int w, int h, float x, float y, float width, float height, Color32 ink)
        {
            int x0 = Mathf.RoundToInt(x), y0 = Mathf.RoundToInt(y), x1 = Mathf.RoundToInt(x + width), y1 = Mathf.RoundToInt(y + height);
            for (int yy = y0; yy < y1; yy++) { for (int xx = x0; xx < x1; xx++) { Put(px, w, h, xx, yy, ink); } }
        }

        /// <summary>An isosceles head with its apex at (cx, tipY), pointing up or down, base as wide as it is tall.</summary>
        private static void FillTriangle(Color32[] px, int w, int h, float cx, float tipY, float size, bool up, Color32 ink)
        {
            for (int i = 0; i <= Mathf.CeilToInt(size); i++)
            {
                float halfWidth = size * (i / Mathf.Max(1f, size)) * 0.5f;
                int y = Mathf.RoundToInt(up ? tipY + i : tipY - i);
                for (int x = Mathf.RoundToInt(cx - halfWidth); x <= Mathf.RoundToInt(cx + halfWidth); x++) { Put(px, w, h, x, y, ink); }
            }
        }
    }
}
