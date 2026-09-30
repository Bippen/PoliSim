using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D-PS (boards 21a-21e, Design 2026-09-29, built §685): **THE POLITICAL SCREENS' ROW GRAMMAR.** Design's largest finding: every one of these
    /// screens stated a fact as a sentence where the desk's own grammar has a row - a party a letter in a sentence, not its 4b mark; a status a
    /// trailing word, not a leading glyph or a chip; a default state spelled every time. The pieces the five boards share:
    /// <list type="bullet">
    /// <item>the party's MARK in a 16 px slot - its 4b mark as authored (the accessor's contract, the campaign masthead's), the letter square
    /// where no mark is held (the board's own slot);</item>
    /// <item>four chip faces - PAPER (an action, or a state that is off), FILLED (a state that is on: 17b's rule, the fill is the state - dark,
    /// with paper ink), OUTLINE (a stamp: a state named, not a control), BRASS (the screen's one act);</item>
    /// <item>a figure over its one-word caption, and a verdict glyph (✓ GOOD / ✕ BAD, the registry's) leading what it judges.</item>
    /// </list>
    /// The sentences the rows replace move to each row's slip (19b), so nothing the page stated is lost.
    /// </summary>
    public partial class GameController
    {
        private enum ChipFace { Paper, Filled, Outline, Brass }

        /// <summary>The chip caption: mono 9.5 bold (the boards' chip face).</summary>
        private GUIStyle RowChipCaption(Color ink) => DeskCaption(9.5f, ink, true, TextAnchor.MiddleCenter);

        /// <summary>A chip's width for its words at the chip caption, with the board's pad (8, or 12 on a 26 px act).</summary>
        private float RowChipWidth(string text, float padBoard = 8f) =>
            Mathf.Ceil(RowChipCaption(PoliSimTheme.TextPrimary).CalcSize(new GUIContent(text)).x) + StatsUnit(padBoard) * 2f;

        /// <summary>
        /// One chip in <paramref name="r"/>. PAPER, FILLED and BRASS are controls (an invisible button over the whole rect, drawn on every event so
        /// the control count never varies with state) and return the click; OUTLINE is a stamp and never clicks. <paramref name="disabled"/> draws the
        /// face at the reduced presence and swallows the click (B5: rendered, never omitted). <paramref name="ink"/> overrides the outline's ink
        /// (Caution for NO CONFIDENCE DECLARED and WITHDRAWN). <paramref name="control"/> false draws a PAPER or FILLED face as a state that is
        /// not a control (the campaign phase strip) - no button.
        /// </summary>
        private bool DrawRowChip(Rect r, string text, ChipFace face, bool disabled = false, Color? ink = null, bool control = true)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color fill, border, words;
                switch (face)
                {
                    case ChipFace.Filled: fill = PoliSimTheme.TextPrimary; border = PoliSimTheme.TextPrimary; words = PoliSimTheme.Card; break;
                    case ChipFace.Brass: fill = PoliSimTheme.Brass; border = PoliSimTheme.BrassBorder; words = PoliSimTheme.TextPrimary; break;
                    case ChipFace.Outline: fill = Color.clear; border = ink ?? PoliSimTheme.TextPrimary; words = ink ?? PoliSimTheme.TextPrimary; break;
                    default: fill = PoliSimTheme.StockOff; border = PoliSimTheme.HairlineStrong; words = PoliSimTheme.TextPrimary; break;
                }
                if (disabled && face != ChipFace.Outline)
                {
                    fill = PoliSimTheme.Tint(PoliSimTheme.StockOff, 0.55f);
                    border = PoliSimTheme.Tint(PoliSimTheme.HairlineStrong, 0.6f);
                    words = PoliSimTheme.TextMuted;
                }
                if (face == ChipFace.Outline)
                {
                    float w = Mathf.Max(1f, Mathf.Round(StatsUnit(3f) * 0.5f));   // 1.5 at the board's 720
                    PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, w), border);
                    PoliSimTheme.Rule(new Rect(r.x, r.yMax - w, r.width, w), border);
                    PoliSimTheme.Rule(new Rect(r.x, r.y, w, r.height), border);
                    PoliSimTheme.Rule(new Rect(r.xMax - w, r.y, w, r.height), border);
                }
                else { PoliSimTheme.RoundedCard(r, fill, border, 0f); }
                PoliSimWidgets.MeasuredLabel(r, text, RowChipCaption(words));
            }
            if (face == ChipFace.Outline || !control) { return false; }
            bool ambient = GUI.enabled;
            GUI.enabled = ambient && !disabled;
            bool clicked = PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = ambient;
            return clicked;
        }

        /// <summary>A chip laid right-to-left: drawn ending at <paramref name="right"/>, centred in the row; returns its rect.</summary>
        private Rect RowChipRectEndingAt(float right, Rect row, string text, float heightBoard = 20f, float padBoard = 8f)
        {
            float w = RowChipWidth(text, padBoard);
            float h = StatsUnit(heightBoard);
            return new Rect(right - w, row.y + Mathf.Round((row.height - h) * 0.5f), w, h);
        }

        /// <summary>A chip laid left-to-right from <paramref name="left"/>, centred in the row.</summary>
        private Rect RowChipRectFrom(float left, Rect row, string text, float heightBoard = 20f, float padBoard = 8f)
        {
            float w = RowChipWidth(text, padBoard);
            float h = StatsUnit(heightBoard);
            return new Rect(left, row.y + Mathf.Round((row.height - h) * 0.5f), w, h);
        }

        /// <summary>
        /// A party's mark in a square slot centred on <paramref name="r"/>'s left: its 4b mark AS AUTHORED (IconLibrary's contract, the campaign
        /// masthead's - a mark carries its own colours, so it is never tinted here and this file draws no party ink); a created party's cell as
        /// authored; the board's letter square (a 1 px rule and the short name in bold mono) where no mark is held.
        /// </summary>
        private void DrawPartyMarkSlot(Rect r, CountryId country, string key)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(key)) { return; }
            float side = Mathf.Min(StatsUnit(16f), r.height);
            var square = new Rect(r.x, r.y + Mathf.Round((r.height - side) * 0.5f), side, side);
            Texture2D mark = null;
            foreach (PoliticalParty party in PartySystems.For(country)) { if (party.Abbrev == key) { mark = IconLibrary.GetPartyMark(party.MarkName); break; } }
            if (mark != null)
            {
                GUI.DrawTexture(square, mark, ScaleMode.ScaleToFit, true);
                return;
            }
            PoliSimTheme.Rule(new Rect(square.x, square.y, square.width, 1f), PoliSimTheme.TextPrimary);
            PoliSimTheme.Rule(new Rect(square.x, square.yMax - 1f, square.width, 1f), PoliSimTheme.TextPrimary);
            PoliSimTheme.Rule(new Rect(square.x, square.y, 1f, square.height), PoliSimTheme.TextPrimary);
            PoliSimTheme.Rule(new Rect(square.xMax - 1f, square.y, 1f, square.height), PoliSimTheme.TextPrimary);
            PoliSimWidgets.MeasuredLabel(square, PartySystems.ShortName(country, key), DeskCaption(8.5f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleCenter));
        }

        /// <summary>A verdict glyph (✓ GOOD in Good ink, ✕ BAD in Bad ink) in a 16 px slot at <paramref name="r"/>'s left, vertically centred.</summary>
        private void DrawVerdictSlot(Rect r, bool good)
        {
            float side = Mathf.Min(StatsUnit(16f), r.height);
            SymbolRegistry.Draw(new Rect(r.x, r.y + Mathf.Round((r.height - side) * 0.5f), side, side), good ? Symbol.Good : Symbol.Bad,
                good ? PoliSimTheme.Good : PoliSimTheme.Bad, DeskCaption(6.5f, good ? PoliSimTheme.Good : PoliSimTheme.Bad));
        }

        /// <summary>A figure (bold mono) with a trailing part in the muted caption - "2 ⁄ 2", "0.338 ⁄ 0.338"; returns the x after it.</summary>
        private float DrawFigurePair(float x, Rect row, string figure, string trailing, float figureBoard = 13f)
        {
            GUIStyle fig = DeskCaption(figureBoard, PoliSimTheme.TextPrimary, true);
            GUIStyle tail = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            float fw = Mathf.Ceil(fig.CalcSize(new GUIContent(figure)).x);
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, fw + 2f, row.height), figure, fig);
            x += fw + StatsUnit(4f);
            if (string.IsNullOrEmpty(trailing)) { return x; }
            float tw = Mathf.Ceil(tail.CalcSize(new GUIContent(trailing)).x);
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, tw + 2f, row.height), trailing, tail);
            return x + tw;
        }

        /// <summary>A figure over its one-word caption (the dependency block's grammar, 21b): mono 15 bold over mono 9.5 muted; returns the width used.</summary>
        private float DrawFigureOverCaption(float x, float y, string figure, string caption, Color figureInk)
        {
            GUIStyle fig = DeskCaption(15f, figureInk, true);
            GUIStyle cap = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            float fh = Mathf.Ceil(DeskCaptionHeight(fig));
            float ch = Mathf.Ceil(DeskCaptionHeight(cap));
            float w = Mathf.Ceil(Mathf.Max(fig.CalcSize(new GUIContent(figure)).x, cap.CalcSize(new GUIContent(caption)).x)) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, y, w, fh), figure, fig);
            PoliSimWidgets.MeasuredLabel(new Rect(x, y + fh, w, ch), caption, cap);
            return w;
        }

        /// <summary>The height <see cref="DrawFigureOverCaption"/> takes.</summary>
        private float FigureOverCaptionHeight() =>
            Mathf.Ceil(DeskCaptionHeight(DeskCaption(15f, PoliSimTheme.TextPrimary, true))) + Mathf.Ceil(DeskCaptionHeight(DeskCaption(9.5f, PoliSimTheme.TextMuted)));

        /// <summary>A section's head (21a): the caption at the left, an optional muted column key at the right, a TextPrimary rule under it.</summary>
        private void DrawRowSectionHead(Rect r, string caption, string key = null)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimWidgets.MeasuredLabel(r, caption, DeskCaption(9.5f, PoliSimTheme.TextPrimary));
            if (!string.IsNullOrEmpty(key)) { PoliSimWidgets.MeasuredLabel(r, key, DeskCaption(9.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight)); }
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), PoliSimTheme.TextPrimary);
        }

        /// <summary>A row's hairline under it (the boards' #DED2B8).</summary>
        private static void DrawRowRule(Rect row)
        {
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow); }
        }

        /// <summary>A day as the desk dates everything: "31 JAN 2029" (D1/D5 - never ISO on a player screen).</summary>
        private static string DeskDay(System.DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        private static readonly Regex IsoDay = new Regex(@"\b(\d{4})-(\d{2})-(\d{2})\b", RegexOptions.CultureInvariant);

        /// <summary>A model line's ISO dates put in the desk's form (21b item 4): the Speaker's log and a round's occasion are written by the model in
        /// ISO; the screen reads them day-month-year.</summary>
        private static string DeskDated(string line) => string.IsNullOrEmpty(line) ? line : IsoDay.Replace(line, m =>
            System.DateTime.TryParseExact(m.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out System.DateTime d) ? DeskDay(d) : m.Value);

        /// <summary>The Parliament tab's rows reserve their rect through the layout and draw inside it (the settings screen's way).</summary>
        private Rect ReserveRow(float heightBoard) => ReserveRowPx(StatsUnit(heightBoard));
    }
}
