using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §726 (UI v3.5, Design's V35 composition; 27a's grammar): THE v3.5 PRIMITIVES every retrofitted screen draws with - the faces at the 14 px floor,
    /// the page title with its † control, the card and its head, a list row, the dense view's line, and the twelve-column grid the composition lays its
    /// cards on. Measures are the composition's, in px at the 1280 window's client area (1280 x 699), scaled by <see cref="V35.Px"/>; type is
    /// <see cref="V35.FontPx"/>, which never returns less than the floor, so a screen built from these cannot draw below it.
    ///
    /// <para><b>The card</b> (the composition's map, calendar and list cards): the card paper behind a 1 px edge, 12 px top and bottom and 14 px left and
    /// right inside it; its head is the area's icon (28 px) and the card's name in the body serif at 16, 10 px apart.</para>
    ///
    /// <para><b>At rest, almost no sub-text</b> (V35_ASK rule 1): what a card used to say under its name - its scope, its method, its unit - is its head's
    /// slip, and the † dense view (rule 3) prints that slip as one dotted line at the card's foot.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>The body serif at a v3.5 size (names, prose), one line, no padding.</summary>
        private GUIStyle V35Serif(float px, Color ink, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle(_labelStyle) { fontSize = V35.FontPx(px), alignment = anchor, wordWrap = false, fontStyle = FontStyle.Normal, clipping = TextClipping.Clip };
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            return Inked(style, ink);
        }

        /// <summary>The body serif that wraps, for the one piece of prose a card holds (an event's description).</summary>
        private GUIStyle V35SerifWrapped(float px, Color ink)
        {
            GUIStyle style = V35Serif(px, ink, TextAnchor.UpperLeft);
            style.wordWrap = true;
            return style;
        }

        /// <summary>The document mono at a v3.5 size (figures, dates, codes), one line, no padding.</summary>
        private GUIStyle V35Mono(float px, Color ink, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle(_calendarMetaStyle) { fontSize = V35.FontPx(px), alignment = anchor, wordWrap = false, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, clipping = TextClipping.Clip };
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            return Inked(style, ink);
        }

        /// <summary>
        /// PROSE cut to <paramref name="width"/> at a word, with an ellipsis, where it does not fit at its size - the composition's <c>text-overflow:
        /// ellipsis</c> on a list row's name. ⚠ Never a figure: <see cref="PoliSimWidgets.MeasuredLabel"/>'s rule stands (a cut number is a plausible
        /// wrong number), and here the floor forbids its other answer, shrinking. <paramref name="cut"/> says whether it was cut, so the caller hangs
        /// the whole text on a slip and nothing the row said is lost.
        /// </summary>
        private static string V35Fit(string text, GUIStyle face, float width, out bool cut)
        {
            cut = false;
            if (string.IsNullOrEmpty(text) || face.CalcSize(new GUIContent(text)).x <= width) { return text; }
            cut = true;
            string t = text;
            while (t.Length > 1)
            {
                int space = t.LastIndexOf(' ');
                t = (space > 0 ? t.Substring(0, space) : t.Substring(0, t.Length - 1)).TrimEnd(' ', '-', ';', ',', '·', ':');
                if (face.CalcSize(new GUIContent(t + "…")).x <= width) { return t + "…"; }
            }
            return "…";
        }

        /// <summary>The width of <paramref name="span"/> columns of the twelve across <paramref name="pageWidth"/>, gutters included.</summary>
        private static float V35Span(float pageWidth, int span)
        {
            float gutter = V35.Px(V35.Gutter);
            float column = (pageWidth - gutter * 11f) / 12f;
            return Mathf.Floor(column * span + gutter * (span - 1));
        }

        /// <summary>A page's title at the left of <paramref name="r"/> and the † dense view's control at its right (D16 §2: one control for the whole
        /// desk, <see cref="DeskProvenance"/>).</summary>
        private void DrawV35PageTitle(Rect r, string title)
        {
            GUIStyle glyph = V35Serif(V35.Name, DeskProvenance.On ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleCenter);
            float tabW = Mathf.Ceil(glyph.CalcSize(new GUIContent(DeskProvenance.Glyph)).x) + V35.Px(20f);
            var tab = new Rect(r.xMax - tabW, r.y, tabW, r.height);
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle face = new GUIStyle(_headerStyle) { fontSize = V35.FontPx(V35.PageTitle), alignment = TextAnchor.MiddleLeft, wordWrap = false };
                face.padding = new RectOffset(0, 0, 0, 0);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, Mathf.Max(1f, r.width - tabW - V35.Px(8f)), r.height), title, Inked(face, PoliSimTheme.TextPrimary));
                if (DeskProvenance.On) { PoliSimTheme.Rule(new Rect(tab.x, tab.yMax - 2f, tab.width, 2f), PoliSimTheme.Brass); }
                PoliSimWidgets.MeasuredLabel(tab, DeskProvenance.Glyph, glyph);
            }
            if (PoliSimWidgets.Button(tab, GUIContent.none, GUIStyle.none)) { DeskProvenance.On = !DeskProvenance.On; }
        }

        /// <summary>§728, a primitive since §734: a page's title, its sub-tabs as words (the active one underlined in <paramref name="area"/>'s ink) and the †
        /// control, on one row - the composition's Statistics and Budget heads. <paramref name="titleAnchor"/> takes the title's rect (its slip). Returns the
        /// tab clicked, or -1.</summary>
        private int DrawV35TitleTabs(Rect row, string title, string[] tabs, int selected, Color area, System.Action<Rect> titleAnchor = null)
        {
            GUIStyle titleFace = new GUIStyle(_headerStyle) { fontSize = V35.FontPx(V35.PageTitle), alignment = TextAnchor.MiddleLeft, wordWrap = false };
            titleFace.padding = new RectOffset(0, 0, 0, 0);
            float titleWidth = Mathf.Ceil(titleFace.CalcSize(new GUIContent(title)).x);
            titleAnchor?.Invoke(new Rect(row.x, row.y, titleWidth, row.height));
            DrawV35PageTitle(row, title);
            int clicked = -1;
            float x = row.x + titleWidth + V35.Px(32f);
            for (int i = 0; i < tabs.Length; i++)
            {
                GUIStyle face = V35Serif(V35.Name, i == selected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
                float w = Mathf.Ceil(face.CalcSize(new GUIContent(tabs[i])).x);
                var tab = new Rect(x, row.y, w, row.height);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimWidgets.MeasuredLabel(tab, tabs[i], face);
                    if (i == selected) { PoliSimTheme.Rule(new Rect(tab.x, tab.yMax - V35.Px(8f), tab.width, 2f), area); }
                }
                if (PoliSimWidgets.Button(tab, GUIContent.none, GUIStyle.none)) { clicked = i; }
                x += w + V35.Px(24f);
            }
            return clicked;
        }

        private static Texture2D _v35CardTexture;
        private static GUIStyle _v35CardStyle;
        private static int _v35CardStyleHeight = -1;

        /// <summary>§728: the card as a GUILayout box - the card paper inside a 1 px edge (a 3 x 3 nine-slice), the 12 x 14 padding - for content laid out
        /// in the flow (a chart's head and plot), whose own rects then stay in the flow's coordinates. Rebuilt when the window's height changes.</summary>
        private static GUIStyle V35CardStyle()
        {
            if (_v35CardTexture == null)
            {
                _v35CardTexture = new Texture2D(3, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color[9];
                for (int i = 0; i < 9; i++) { px[i] = i == 4 ? V35.CardPaper : V35.CardEdge; }
                _v35CardTexture.SetPixels(px);
                _v35CardTexture.Apply();
            }
            if (_v35CardStyle == null || _v35CardStyleHeight != UiScreen.Height)
            {
                _v35CardStyleHeight = UiScreen.Height;
                int padX = Mathf.RoundToInt(V35.Px(V35.CardPadX)), padY = Mathf.RoundToInt(V35.Px(V35.CardPadY));
                _v35CardStyle = new GUIStyle { border = new RectOffset(1, 1, 1, 1), padding = new RectOffset(padX, padX, padY, padY), margin = new RectOffset(0, 0, 0, 0) };
                _v35CardStyle.normal.background = _v35CardTexture;
            }
            return _v35CardStyle;
        }

        /// <summary>A card: the card paper behind its edge. Returns the inside, the composition's 12 x 14 padding taken off.</summary>
        private static Rect DrawV35Card(Rect r)
        {
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(r, V35.CardPaper);
                PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(r.x, r.y, 1f, r.height), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(r.xMax - 1f, r.y, 1f, r.height), V35.CardEdge);
            }
            float padX = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY);
            return new Rect(r.x + padX, r.y + padY, Mathf.Max(1f, r.width - padX * 2f), Mathf.Max(1f, r.height - padY * 2f));
        }

        /// <summary>A card's icon, tinted, in <paramref name="r"/>.</summary>
        private static void DrawV35Icon(Rect r, string icon, Color ink)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            Texture2D tex = IconLibrary.V35(icon);
            if (tex == null) { return; }
            Color before = GUI.color;
            GUI.color = ink;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            GUI.color = before;
        }

        /// <summary>A card's head inside <paramref name="inner"/>: the icon in <paramref name="iconInk"/>, then the name, and <paramref name="reserveRight"/>
        /// kept at its right for a control the caller draws there. Returns the head's rect; the content starts <see cref="V35.CardHeadGap"/> under it.</summary>
        private Rect DrawV35CardHead(Rect inner, string icon, string name, Color iconInk, float reserveRight = 0f)
        {
            float side = V35.Px(V35.CardIcon);
            var head = new Rect(inner.x, inner.y, inner.width, side);
            DrawV35Icon(new Rect(head.x, head.y, side, side), icon, iconInk);
            if (Event.current.type == EventType.Repaint)
            {
                float x = head.x + side + V35.Px(10f);
                PoliSimWidgets.MeasuredLabel(new Rect(x, head.y, Mathf.Max(1f, head.xMax - reserveRight - x), side), name, V35Serif(V35.Name, PoliSimTheme.TextPrimary));
            }
            return head;
        }

        /// <summary>A section head across <paramref name="r"/>: the serif in capitals at the floor, in the muted ink (the composition's 15 px small caps -
        /// IMGUI has none, and the floor holds).</summary>
        private void DrawV35SectionHead(Rect r, string text)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimWidgets.MeasuredLabel(r, text.ToUpperInvariant(), V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.LowerLeft));
        }

        /// <summary>The content's rect under a card head.</summary>
        private static Rect V35UnderHead(Rect inner, Rect head)
        {
            float top = head.yMax + V35.Px(V35.CardHeadGap);
            return new Rect(inner.x, top, inner.width, Mathf.Max(1f, inner.yMax - top));
        }

        /// <summary>One list row: <paramref name="lead"/> (a date, a code) in the mono at the floor, <paramref name="name"/> in the serif, a figure at the
        /// right in its ink, the row's rule under it.</summary>
        private void DrawV35ListRow(Rect row, string lead, float leadWidth, string name, string figure, Color figureInk)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float x = row.x;
            if (!string.IsNullOrEmpty(lead))
            {
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, leadWidth, row.height), lead, V35Mono(V35.Floor, PoliSimTheme.TextMuted));
                x += leadWidth + V35.Px(10f);
            }
            float figureWidth = 0f;
            if (!string.IsNullOrEmpty(figure))
            {
                GUIStyle face = V35Mono(V35.Name, figureInk, bold: true, TextAnchor.MiddleRight);
                figureWidth = Mathf.Ceil(face.CalcSize(new GUIContent(figure)).x) + V35.Px(4f);
                PoliSimWidgets.MeasuredLabel(new Rect(row.xMax - figureWidth, row.y, figureWidth, row.height), figure, face);
            }
            GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float nameWidth = Mathf.Max(1f, row.xMax - figureWidth - V35.Px(8f) - x);
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, nameWidth, row.height), V35Fit(name, nameFace, nameWidth, out _), nameFace);
            PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), V35.ListRule);
        }

        /// <summary>§728 (UI v3.5): a TILE's content, the composition's T - the icon, the figure with its state glyph and its change, the name, and under
        /// them a share's gauge or a history's sparkline. A glyph of ABSENT takes the figure's slot (never a zero); DATED follows the figure.</summary>
        private sealed class V35TileData
        {
            public string Icon;
            public Color IconInk = PoliSimTheme.TextSecondary;
            public string Figure;
            public Symbol? Glyph;
            public string Change;
            public Color ChangeInk = PoliSimTheme.TextMuted;
            public string Name;
            /// <summary>A share's gauge under the head, 0..1; negative for none.</summary>
            public float Fill = -1f;
            public Color FillInk = PoliSimTheme.Neutral;
            /// <summary>A kept history at the head's right; null for none.</summary>
            public IReadOnlyList<float> Spark;
            public float? SparkReference;
            /// <summary>The sparkline as a band under the head (the gauge's place) rather than at its right - for a tile too narrow for both.</summary>
            public bool SparkBelow;
            /// <summary>The figure's size: the live tile's 24, a Society tile's 21.</summary>
            public float FigurePx = V35.Figure;
            /// <summary>§732: a mark after the figure that is not one of D24's glyphs - D16's ‡ TWO DEFINITIONS - in the secondary ink.</summary>
            public string Mark;
            /// <summary>§734: the figure's ink - the draft's Caution where the figure is drafted, the warning past a statutory rule; the primary ink otherwise.</summary>
            public Color FigureInk = PoliSimTheme.TextPrimary;
        }

        /// <summary>A tile's height for its content - the head (the icon, or the figure over the name), and the gauge's band under it where it has one.</summary>
        private float V35TileHeight(V35TileData t)
        {
            float figure = Mathf.Ceil(V35Mono(t.FigurePx, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y);
            float name = Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y);
            float head = Mathf.Max(V35.Px(V35.CardIcon), figure + 2f + name);
            float below = t.Fill >= 0f ? V35.Px(10f) + V35.Px(8f) : t.Spark != null && t.SparkBelow ? V35.Px(8f) + V35.Px(24f) : 0f;
            return V35.Px(V35.CardPadY) * 2f + head + below;
        }

        /// <summary>
        /// §728: a tile in <paramref name="r"/> (its card included). Returns the rect of its figure and name - the reading's anchor for its slip. The
        /// figure is never cut: where it does not fit the guard is told (a cut number is a wrong number).
        /// </summary>
        private Rect DrawV35Tile(Rect r, V35TileData t)
        {
            Rect inner = DrawV35Card(r);
            GUIStyle figureFace = V35Mono(t.FigurePx, t.FigureInk, bold: true);
            GUIStyle changeFace = V35Mono(15f, t.ChangeInk, bold: true);
            GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float figureHeight = Mathf.Ceil(figureFace.CalcSize(new GUIContent("0")).y);
            float nameHeight = Mathf.Ceil(nameFace.CalcSize(new GUIContent("Ag")).y);
            float iconSide = V35.Px(V35.CardIcon);
            float headHeight = Mathf.Max(iconSide, figureHeight + 2f + nameHeight);
            var head = new Rect(inner.x, inner.y, inner.width, headHeight);
            DrawV35Icon(new Rect(head.x, head.y + Mathf.Round((headHeight - iconSide) * 0.5f), iconSide, iconSide), t.Icon, t.IconInk);
            float x = head.x + iconSide + V35.Px(12f);
            float sparkWidth = t.Spark != null && !t.SparkBelow ? V35.Px(100f) : 0f;
            float right = head.xMax - (sparkWidth > 0f ? sparkWidth + V35.Px(8f) : 0f);
            float y = head.y + Mathf.Round((headHeight - figureHeight - 2f - nameHeight) * 0.5f);
            var reading = new Rect(x, y, Mathf.Max(1f, right - x), figureHeight + 2f + nameHeight);
            if (Event.current.type != EventType.Repaint) { return reading; }

            float fx = x;
            if (t.Glyph == Symbol.Absent || t.Glyph == Symbol.Billed)
            {
                // V35_ASK rule 3: ABSENT and BILLED take the figure's own slot - there is no figure to follow. §732: the glyph's file is a square
                // canvas around a wide dashed box, so it is drawn at 1.6 figure-heights, centred on the figure's line - the box itself then reads at the
                // figure's size (the composition's 45 x 20), not at a third of it
                float side = Mathf.Round(figureHeight * 1.6f);
                DrawStateGlyph(new Rect(fx, y + Mathf.Round((figureHeight - side) * 0.5f), side, side), t.Glyph.Value, PoliSimTheme.TextMuted);
                fx += side + 6f;
            }
            else if (!string.IsNullOrEmpty(t.Figure))
            {
                float w = Mathf.Ceil(figureFace.CalcSize(new GUIContent(t.Figure)).x) + 2f;
                UiOverflowGuard.Check(t.Figure, new Vector2(w, figureHeight), new Vector2(Mathf.Max(1f, right - fx), figureHeight), figureFace.fontSize);
                GUI.Label(new Rect(fx, y, w, figureHeight), t.Figure, figureFace);
                fx += w + 6f;
                if (t.Glyph.HasValue)
                {
                    float side = Mathf.Round(changeFace.fontSize * 1.0f);
                    DrawStateGlyph(new Rect(fx, y + Mathf.Round((figureHeight - side) * 0.5f), side, side), t.Glyph.Value, PoliSimTheme.TextSecondary);
                    fx += side + 6f;
                }
                if (!string.IsNullOrEmpty(t.Mark))
                {
                    GUIStyle markFace = V35Mono(15f, PoliSimTheme.TextSecondary, bold: true);
                    float mw = Mathf.Ceil(markFace.CalcSize(new GUIContent(t.Mark)).x) + 2f;
                    GUI.Label(new Rect(fx, y, mw, figureHeight), t.Mark, markFace);
                    fx += mw + 6f;
                }
            }
            float nameWidth = Mathf.Max(1f, right - x);
            if (!string.IsNullOrEmpty(t.Change))
            {
                float cw = Mathf.Ceil(changeFace.CalcSize(new GUIContent(t.Change)).x) + 2f;
                if (cw <= right - fx) { GUI.Label(new Rect(fx, y, cw, figureHeight), t.Change, changeFace); }
                else if (cw + V35.Px(24f) <= nameWidth)
                {
                    // §41 (the USA's budget strip: "▲ US$766B" beside "−US$1.83T" in a quarter-width tile): a change that does not fit beside the figure takes the
                    // name's line, at its right - the figure is never cut and a change is never squeezed; the name yields the room
                    GUI.Label(new Rect(right - cw, y + figureHeight + 2f, cw, nameHeight), t.Change, changeFace);
                    nameWidth = Mathf.Max(1f, right - cw - V35.Px(8f) - x);
                }
                else { UiOverflowGuard.Check(t.Change, new Vector2(cw, figureHeight), new Vector2(Mathf.Max(1f, right - fx), figureHeight), changeFace.fontSize); }
            }
            PoliSimWidgets.MeasuredLabel(new Rect(x, y + figureHeight + 2f, nameWidth, nameHeight), V35Fit(t.Name, nameFace, nameWidth, out _), nameFace);

            if (t.Spark != null)
            {
                var spark = t.SparkBelow
                    ? new Rect(inner.x, head.yMax + V35.Px(8f), inner.width, V35.Px(24f))
                    : new Rect(head.xMax - sparkWidth, head.y + Mathf.Round((headHeight - V35.Px(24f)) * 0.5f), sparkWidth, V35.Px(24f));
                if (t.Spark.Count >= 2) { GraphRenderer.DrawSparkline(spark, t.Spark, PoliSimTheme.Neutral, reference: t.SparkReference); }
                else { DeskDottedBaseline(spark); }
            }
            if (t.Fill >= 0f)
            {
                var track = new Rect(inner.x, head.yMax + V35.Px(10f), inner.width, V35.Px(8f));
                PoliSimTheme.Rule(track, PoliSimTheme.BarTrack);
                PoliSimTheme.Rule(new Rect(track.x, track.y, track.width * Mathf.Clamp01(t.Fill), track.height), t.FillInk);
            }
            return reading;
        }

        /// <summary>§732: one part of a share bar - its name and figure, its ink and its words' ink, its length in the bar's unit, and the slip its
        /// segment opens (null for none).</summary>
        private struct V35Part
        {
            public string Name, Figure, Anchor;
            public Color Ink, Text;
            public float Length;
            public V35Part(string name, string figure, Color ink, Color text, float length, string anchor = null)
            {
                Name = name; Figure = figure; Ink = ink; Text = text; Length = Mathf.Max(0f, length); Anchor = anchor;
            }
        }

        /// <summary>A share bar's words, laid out once for its width: each part's words inside its segment where they fit at the floor (the name and
        /// the figure, else the name alone), the rest on the key line in bar order, and how many rows the key wraps to.</summary>
        private sealed class V35BarLayout
        {
            public string[] Inside;
            public readonly List<int> Keyed = new List<int>();
            public float[] KeyWidths;
            public int KeyRows;
        }

        private V35BarLayout LayOutV35Bar(float width, IList<V35Part> parts, float whole)
        {
            GUIStyle inside = V35Serif(V35.Floor, PoliSimTheme.TextOnDesk, TextAnchor.MiddleCenter);
            GUIStyle keyName = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle keyFigure = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
            float pad = V35.Px(6f), swatch = V35.Px(10f), entryGap = V35.Px(16f);
            var l = new V35BarLayout { Inside = new string[parts.Count], KeyWidths = new float[parts.Count] };
            for (int i = 0; i < parts.Count; i++)
            {
                float w = width * Mathf.Clamp01(parts[i].Length / Mathf.Max(0.0001f, whole));
                string both = parts[i].Name + " " + parts[i].Figure;
                if (inside.CalcSize(new GUIContent(both)).x + pad * 2f <= w) { l.Inside[i] = both; }
                else if (inside.CalcSize(new GUIContent(parts[i].Name)).x + pad * 2f <= w) { l.Inside[i] = parts[i].Name; }
                else { l.Keyed.Add(i); }
            }
            float run = 0f;
            foreach (int i in l.Keyed)
            {
                l.KeyWidths[i] = swatch + V35.Px(5f) + keyName.CalcSize(new GUIContent(parts[i].Name)).x + V35.Px(4f) + keyFigure.CalcSize(new GUIContent(parts[i].Figure)).x;
                if (l.KeyRows == 0) { l.KeyRows = 1; }
                else if (run + entryGap + l.KeyWidths[i] > width) { l.KeyRows++; run = 0f; }
                run += (run > 0f ? entryGap : 0f) + l.KeyWidths[i];
            }
            return l;
        }

        /// <summary>The bar's height with its key rows.</summary>
        private static float V35BarHeight(V35BarLayout l) => V35.Px(26f) + (l.KeyRows > 0 ? V35.Px(8f) + l.KeyRows * V35.Px(22f) : 0f);

        /// <summary>
        /// The share bar (V35_ASK rule 7, board 23a ⑧): each segment's length is its part of <paramref name="whole"/>; a part's name and figure INSIDE its
        /// segment where they fit at the floor, then the name alone, else the part goes to ONE key line under the bar, in bar order - never slip-only.
        /// <paramref name="anchor"/> registers a part's slip on its segment and its key entry.
        /// </summary>
        private void DrawV35Bar(Rect area, IList<V35Part> parts, float whole, V35BarLayout l, System.Action<Rect, string> anchor)
        {
            var bar = new Rect(area.x, area.y, area.width, V35.Px(26f));
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
            float x = bar.x;
            for (int i = 0; i < parts.Count; i++)
            {
                float w = bar.width * Mathf.Clamp01(parts[i].Length / Mathf.Max(0.0001f, whole));
                var segment = new Rect(x, bar.y, w, bar.height);
                PoliSimTheme.Rule(segment, parts[i].Ink);
                if (i > 0) { PoliSimTheme.Rule(new Rect(Mathf.Round(x), bar.y, 1f, bar.height), V35.CardPaper); }
                if (l.Inside[i] != null) { PoliSimWidgets.MeasuredLabel(segment, l.Inside[i], V35Serif(V35.Floor, parts[i].Text, TextAnchor.MiddleCenter)); }
                if (parts[i].Anchor != null) { anchor?.Invoke(segment, parts[i].Anchor); }
                x += w;
            }
            GUIStyle keyName = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle keyFigure = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
            float swatch = V35.Px(10f), entryGap = V35.Px(16f), keyPitch = V35.Px(22f);
            float kx = area.x, ky = bar.yMax + V35.Px(8f);
            foreach (int i in l.Keyed)
            {
                if (kx > area.x && kx + l.KeyWidths[i] > area.xMax + 0.5f) { kx = area.x; ky += keyPitch; }
                PoliSimTheme.Rule(new Rect(kx, ky + (keyPitch - swatch) * 0.5f, swatch, swatch), parts[i].Ink);
                float nx = kx + swatch + V35.Px(5f);
                float nw = Mathf.Ceil(keyName.CalcSize(new GUIContent(parts[i].Name)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(nx, ky, nw, keyPitch), parts[i].Name, keyName);
                float fw = Mathf.Ceil(keyFigure.CalcSize(new GUIContent(parts[i].Figure)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(nx + nw + V35.Px(4f), ky, fw, keyPitch), parts[i].Figure, keyFigure);
                if (parts[i].Anchor != null) { anchor?.Invoke(new Rect(kx, ky, l.KeyWidths[i], keyPitch), parts[i].Anchor); }
                kx += l.KeyWidths[i] + entryGap;
            }
        }

        /// <summary>A tile's head drawn borderless inside a larger card (a share card's): the icon, the figure over the name. Returns the reading's rect.</summary>
        private Rect DrawV35TileHead(Rect r, V35TileData t)
        {
            float iconSide = V35.Px(V35.CardIcon);
            DrawV35Icon(new Rect(r.x, r.y + Mathf.Round((r.height - iconSide) * 0.5f), iconSide, iconSide), t.Icon, t.IconInk);
            GUIStyle figure = V35Mono(t.FigurePx, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle name = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float fh = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y), nh = Mathf.Ceil(name.CalcSize(new GUIContent("Ag")).y);
            float x = r.x + iconSide + V35.Px(12f), y = r.y + Mathf.Round((r.height - fh - 2f - nh) * 0.5f);
            var reading = new Rect(x, y, Mathf.Max(1f, r.xMax - x), fh + 2f + nh);
            if (Event.current.type != EventType.Repaint) { return reading; }
            GUI.Label(new Rect(x, y, r.xMax - x, fh), t.Figure, figure);
            PoliSimWidgets.MeasuredLabel(new Rect(x, y + fh + 2f, r.xMax - x, nh), V35Fit(t.Name, name, r.xMax - x, out _), name);
            return reading;
        }

        /// <summary>§732: a SHARE CARD's height at <paramref name="width"/> - the composition's tile with a bar (People's age split, attainment and
        /// emissions; Statistics' economy by sector): the tile's head, the bar, its key.</summary>
        private float V35ShareCardHeight(float width, V35TileData head, IList<V35Part> parts, float whole)
        {
            V35BarLayout l = LayOutV35Bar(width - V35.Px(V35.CardPadX) * 2f, parts, whole);
            return V35TileHeight(head) + V35.Px(10f) + V35BarHeight(l);
        }

        /// <summary>A share card in <paramref name="r"/>: the card, the tile's head, the bar under it. Returns the head's reading rect (its slip's anchor).</summary>
        private Rect DrawV35ShareCard(Rect r, V35TileData head, IList<V35Part> parts, float whole, System.Action<Rect, string> anchor)
        {
            Rect inner = DrawV35Card(r);
            float headHeight = V35TileHeight(head) - V35.Px(V35.CardPadY) * 2f;
            Rect reading = DrawV35TileHead(new Rect(inner.x, inner.y, inner.width, headHeight), head);
            V35BarLayout l = LayOutV35Bar(inner.width, parts, whole);
            DrawV35Bar(new Rect(inner.x, inner.y + headHeight + V35.Px(10f), inner.width, V35BarHeight(l)), parts, whole, l, anchor);
            return reading;
        }

        /// <summary>The † dense view's line at the foot of a card's inside (V35_ASK rule 3): the head's slip as one dotted line. Returns the inside less
        /// the line, so the content keeps clear of it; with the view off, the inside unchanged.</summary>
        private Rect DrawV35DenseLine(Rect inner, SlipContent slip)
        {
            if (!DeskProvenance.On || slip == null) { return inner; }
            // It wraps rather than shrinks: a slip's content is longer than a card is wide, and the floor holds.
            GUIStyle face = V35SerifWrapped(V35.Floor, PoliSimTheme.TextMuted);
            string text = slip.Head;
            foreach (string l in slip.Lines) { text += " · " + SlipContent.Plain(l); }
            float textHeight = Mathf.Ceil(face.CalcHeight(new GUIContent(text), inner.width));
            float h = textHeight + V35.Px(4f);
            var line = new Rect(inner.x, inner.yMax - h, inner.width, h);
            if (Event.current.type == EventType.Repaint)
            {
                for (float x = line.x; x < line.xMax; x += 4f) { PoliSimTheme.Rule(new Rect(x, line.y, 1f, 1f), PoliSimTheme.HairlineStrong); }
                UiContainmentGuard.Check("v3.5 dense line", new Rect(line.x, line.y + V35.Px(4f), line.width, textHeight), inner);
                GUI.Label(new Rect(line.x, line.y + V35.Px(4f), line.width, textHeight), text, face);
            }
            return new Rect(inner.x, inner.y, inner.width, Mathf.Max(1f, inner.height - h - V35.Px(4f)));
        }
    }
}
