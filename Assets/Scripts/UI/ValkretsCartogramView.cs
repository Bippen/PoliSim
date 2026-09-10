using System.Globalization;
using PoliSim.Elections;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// **Board 4a drawn — the valkrets cartogram at the centre of election night** (2026-09-10, election night item 2).
    ///
    /// <para>The geometry is `ValkretsCartogram`'s (the board's eleven bands and its area formula over OUR mandate
    /// column); this component only puts it on a canvas. It lays the tiles when uGUI has sized its rect, and again
    /// whenever that size changes, so the map is laid in the rect the page actually gave it and never in a guess.</para>
    ///
    /// <para><b>What a tile says.</b> A DECLARED valkrets is filled in its winner's party ink - the chamber's own laddered
    /// ink, so the map and the seat arc cannot disagree about a colour - and carries, by board 4a's ladder: FULL the
    /// name, the winner's mark and abbreviation, the margin over the runner-up and the winner's change against the
    /// same valkrets in the previous election; COMPACT the same without the change; MINIMAL a code, the winner and the
    /// margin. ⚠ An UNDECLARED valkrets is paper with an em dash - absence, never a zero, the rule the night's model
    /// is built on.</para>
    ///
    /// <para><b>Seats per band</b> sit in the margin at each band's left, beside the coastline the band edges carry:
    /// the fixed mandates the band returns (the board's own "→ 97" column), so the arithmetic of the tile areas is
    /// legible on the page rather than only in the check.</para>
    /// </summary>
    public sealed class ValkretsCartogramView : MonoBehaviour
    {
        /// <summary>What one tile shows. Undeclared: every other field is ignored.</summary>
        public struct TileResult
        {
            public bool Declared;
            public string Winner;
            public Color Ink;
            public Texture2D Mark;
            public double MarginPp;
            public bool HasSwing;
            public double SwingPp;
        }

        /// <summary>The type the tiles carry, in canvas units - the ladder is scaled by its ratio to the board's 9 px name line.</summary>
        private const int NameSize = 11;
        private const int WinnerSize = 14;
        private const int FigureSize = 12;
        private const float BandLabelWidth = 34f;

        private int[] _mandates;
        private TileResult[] _results;
        private Vector2 _built = new Vector2(-1f, -1f);

        /// <summary>Tiles drawn in the last layout, per ladder level - read by the film's log line.</summary>
        public int LastFull, LastCompact, LastMinimal;

        public static ValkretsCartogramView Create(Transform parent, int[] mandates, TileResult[] results)
        {
            var go = new GameObject("Cartogram");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 200f;
            var view = go.AddComponent<ValkretsCartogramView>();
            view._mandates = mandates;
            view._results = results;
            return view;
        }

        private void LateUpdate()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            if (Mathf.Abs(size.x - _built.x) > 0.5f || Mathf.Abs(size.y - _built.y) > 0.5f) { Rebuild(); }
        }

        /// <summary>Lay and draw the tiles in the rect as it stands now. Safe to call again; it clears what it drew.</summary>
        public void Rebuild()
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.rect.size;
            _built = size;
            for (int i = transform.childCount - 1; i >= 0; i--) { Destroy(transform.GetChild(i).gameObject); }
            if (size.x < 50f || size.y < 50f || _mandates == null || _results == null) { return; }

            // Keep the board's aspect: the widest map the rect holds at 1080 : 587.6, centred, the band labels at the left.
            float usableW = size.x - BandLabelWidth;
            float mapW = Mathf.Min(usableW, size.y * ValkretsCartogram.BoardWidth / ValkretsCartogram.BoardHeight);
            float mapH = mapW * ValkretsCartogram.BoardHeight / ValkretsCartogram.BoardWidth;
            float originX = BandLabelWidth + (usableW - mapW) * 0.5f;
            float originY = (size.y - mapH) * 0.5f;

            ValkretsCartogram.Layout layout = ValkretsCartogram.Lay(mapW, mapH, _mandates, NameSize / 9f);
            LastFull = LastCompact = LastMinimal = 0;

            for (int b = 0; b < layout.BandRows.Count; b++)
            {
                (float y, float h, int mandates) = layout.BandRows[b];
                float bandLeft = originX + ValkretsCartogram.Bands[b].Left * layout.Scale;
                Text label = Label(transform, "Band" + (b + 1), mandates.ToString(CultureInfo.InvariantCulture), 10,
                    PoliSimTheme.TextMuted, TextAnchor.MiddleRight, FontStyle.Normal);
                Place(label.rectTransform, bandLeft - 30f, originY + y, 26f, h);
            }

            foreach (ValkretsCartogram.Tile tile in layout.Tiles)
            {
                switch (tile.Level)
                {
                    case ValkretsCartogram.Level.Full: LastFull++; break;
                    case ValkretsCartogram.Level.Compact: LastCompact++; break;
                    default: LastMinimal++; break;
                }
                DrawTile(tile, originX, originY);
            }
        }

        private void DrawTile(ValkretsCartogram.Tile tile, float originX, float originY)
        {
            TileResult result = _results[tile.Region];
            var go = new GameObject("Tile_" + ValkretsCartogram.Codes[tile.Region]);
            go.transform.SetParent(transform, false);
            Image face = go.AddComponent<Image>();
            face.raycastTarget = false;
            face.color = result.Declared ? result.Ink : PoliSimTheme.CardInset;
            RectTransform rt = face.rectTransform;
            Place(rt, originX + tile.X, originY + tile.Y, tile.W, tile.H);

            Color ink = result.Declared && Luminance(result.Ink) < 0.42f ? PoliSimTheme.Card : PoliSimTheme.TextPrimary;
            Color faint = result.Declared ? new Color(ink.r, ink.g, ink.b, 0.82f) : PoliSimTheme.TextMuted;
            const float pad = 4f;
            float innerW = tile.W - pad * 2f;

            if (tile.Level == ValkretsCartogram.Level.Minimal)
            {
                // Code over "S +6.4" - or a lone dash when nothing has declared.
                string code = ValkretsCartogram.Codes[tile.Region];
                string line = result.Declared ? result.Winner + " " + Margin(result.MarginPp) : "—";
                float half = tile.H * 0.5f;
                Fit(Label(go.transform, "Code", code, 9, faint, TextAnchor.LowerCenter, FontStyle.Bold), pad, 1f, innerW, half - 1f, null);
                Fit(Label(go.transform, "Result", line, 10, ink, TextAnchor.UpperCenter, FontStyle.Bold), pad, half, innerW, half - 1f, result.Declared ? result.Winner : "—");
                return;
            }

            float nameH = 15f;
            if (tile.H < nameH + 18f)
            {
                // A SHALLOW band (the 2560 film found it: Norrbotten, Halland, Skåne): two lines do not fit, and the fit rule then
                // hid the winner - the one thing a tile is for. One line instead: mark, name (or code), and the result at the right.
                float lh = tile.H - 4f;
                float lx = pad;
                if (result.Declared && result.Mark != null && lh >= 10f)
                {
                    float chipSide = Mathf.Min(lh, 16f);
                    Chip(go.transform, result.Mark, lx, (tile.H - chipSide) * 0.5f, chipSide);
                    lx += chipSide + 3f;
                }
                Text right = Label(go.transform, "Result", result.Declared ? result.Winner + " " + Margin(result.MarginPp) : "—", FigureSize,
                    result.Declared ? ink : PoliSimTheme.TextMuted, TextAnchor.MiddleRight, FontStyle.Bold);
                float rightW = Mathf.Min(right.preferredWidth + 1f, innerW);
                Fit(right, pad + innerW - rightW, 2f, rightW, lh, result.Declared ? result.Winner : null);
                Fit(Label(go.transform, "Name", ValkretsCartogram.Labels[tile.Region], NameSize, faint, TextAnchor.MiddleLeft, FontStyle.Bold),
                    lx, 2f, innerW - (lx - pad) - rightW - 4f, lh, ValkretsCartogram.Codes[tile.Region]);
                return;
            }

            Fit(Label(go.transform, "Name", ValkretsCartogram.Labels[tile.Region], NameSize, faint, TextAnchor.UpperLeft, FontStyle.Bold),
                pad, 2f, innerW, nameH, ValkretsCartogram.Codes[tile.Region]);

            if (!result.Declared)
            {
                Label(go.transform, "Absent", "—", WinnerSize, PoliSimTheme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Normal);
                Place(go.transform.GetChild(go.transform.childCount - 1) as RectTransform, pad, nameH, innerW, tile.H - nameH - 2f, local: true);
                return;
            }

            float lineY = nameH + 1f;
            float lineH = Mathf.Min(20f, tile.H - lineY - 2f);
            float x = pad;
            if (result.Mark != null && lineH >= 12f)
            {
                Chip(go.transform, result.Mark, x, lineY, lineH);
                x += lineH + 4f;
            }

            int winnerSize = lineH >= 18f ? WinnerSize : FigureSize;
            Fit(Label(go.transform, "Winner", result.Winner, winnerSize, ink, TextAnchor.MiddleLeft, FontStyle.Bold), x, lineY, innerW - (x - pad), lineH, null);
            Fit(Label(go.transform, "Margin", Margin(result.MarginPp), FigureSize, ink, TextAnchor.MiddleRight, FontStyle.Normal), pad, lineY, innerW, lineH, null);

            if (tile.Level == ValkretsCartogram.Level.Full && result.HasSwing && tile.H - (lineY + lineH) >= 13f)
            {
                string swing = "VS LAST " + result.SwingPp.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
                Fit(Label(go.transform, "Swing", swing, 10, faint, TextAnchor.LowerLeft, FontStyle.Normal), pad, lineY + lineH, innerW, tile.H - (lineY + lineH) - 2f, null);
            }
        }

        /// <summary>The winner's mark on a paper chip: the tile is the party's ink, and a mark drawn in that ink on it would vanish.</summary>
        private static void Chip(Transform tile, Texture2D markTexture, float x, float y, float side)
        {
            var chip = new GameObject("MarkChip");
            chip.transform.SetParent(tile, false);
            Image chipFace = chip.AddComponent<Image>();
            chipFace.color = PoliSimTheme.Card;
            chipFace.raycastTarget = false;
            Place(chipFace.rectTransform, x, y, side, side);
            var mark = new GameObject("Mark");
            mark.transform.SetParent(chip.transform, false);
            RawImage markImage = mark.AddComponent<RawImage>();
            markImage.texture = markTexture;
            markImage.raycastTarget = false;
            RectTransform mr = markImage.rectTransform;
            mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one; mr.offsetMin = new Vector2(1f, 1f); mr.offsetMax = new Vector2(-1f, -1f);
        }

        private static string Margin(double pp) => "+" + pp.ToString("0.0", CultureInfo.InvariantCulture);

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        private static Text Label(Transform parent, string name, string content, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            Text t = CanvasChrome.MakeText(parent, name, content, PoliSimTheme.Document, size, color, anchor, style);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Place a label inside its tile; if the text is wider than the room, fall back to <paramref name="fallback"/>, and if
        /// that does not fit either, draw nothing - a label spilling into the next tile is worse than a missing one.</summary>
        private static void Fit(Text text, float x, float y, float w, float h, string fallback)
        {
            Place(text.rectTransform, x, y, w, h, local: true);
            if (text.preferredWidth <= w + 0.5f && text.preferredHeight <= h + 0.5f) { return; }
            if (fallback != null)
            {
                text.text = fallback;
                if (text.preferredWidth <= w + 0.5f && text.preferredHeight <= h + 0.5f) { return; }
            }
            text.gameObject.SetActive(false);
        }

        /// <summary>Top-left placement: x rightward, y DOWNWARD from the parent's top-left corner.</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h, bool local = false)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }
    }
}
