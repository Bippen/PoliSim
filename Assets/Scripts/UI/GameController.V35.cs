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
