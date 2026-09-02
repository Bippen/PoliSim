using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>One estimated effect on one outcome: the preview's own point, its direction of good, and the figure the sheet prints beside its arrow.</summary>
    public readonly struct EffectArrow
    {
        public readonly string Name;
        public readonly float Value;
        public readonly bool HigherIsBetter;
        public readonly string Figure;

        public EffectArrow(string name, float value, bool higherIsBetter, string figure)
        {
            Name = name;
            Value = value;
            HigherIsBetter = higherIsBetter;
            Figure = figure;
        }
    }

    /// <summary>
    /// P2-2.1 (Playtest 2, 2026-09-02) — **the effects panel as arrows.** One column per affected outcome:
    /// the figure at the head, the arrow growing up from the baseline for a rise and down for a fall, its
    /// length the effect's magnitude against the largest effect on the panel, its ink the direction-aware
    /// verdict (`UiPalette.GetDeltaColor` - a rise in unemployment is a bad arrow, a rise in approval a
    /// good one), and the outcome's name at the foot. Every length traces to the preview's own figure:
    /// <c>length = lane × |value| / max|value|</c>, and the figure printed at the head IS the value the
    /// length was scaled from. No horizon, no range: the point the deterministic preview produced, with its
    /// scope said once by the caller.
    /// </summary>
    public static class EffectArrowsRenderer
    {
        /// <summary>The shortest arrow drawn for a non-zero effect, so the smallest affected outcome still reads as an arrow rather than a dot.</summary>
        private const float MinShaftFraction = 0.12f;
        private const float HeadLengthFraction = 0.22f;

        // ------------------------------------------------------------------------------------------
        // Board 5c (D11 row 3, 2026-09-02): ONE GRAMMAR for "estimated impact", three parts, never
        // fewer - the arrow from a hairline baseline (length ∝ |move| / max|move| in this panel), the
        // figure at the head signed in the arrow's ink, and the SCOPE LINE under every panel,
        // verbatim. The same three parts on the sheet (the Budget mid-draft), on the signing takeover
        // (AS ENACTED) and on election night (the chamber's first budget, if unchanged); the plate is
        // one renderer painted once (CanvasPaint.Arrows on the takeovers), only the title changes.
        // No whiskers, no bands, no ±: a deterministic preview is one point, and a range that is not
        // a range is forbidden by the row.
        // ------------------------------------------------------------------------------------------
        public const string ScopeLine = "ESTIMATE · NEXT YEAR · WITH vs WITHOUT THIS DRAFT · NO EVENTS · ONE DETERMINISTIC POINT — NOT A RANGE. ARROW LENGTH IS RELATIVE TO THE LARGEST MOVE IN THIS PANEL ONLY.";
        public const string PlateTitleDraft = "ESTIMATED IMPACT · THIS DRAFT";
        public const string PlateTitleEnacted = "ESTIMATED IMPACT · AS ENACTED";
        public const string PlateTitleInherited = "ESTIMATED IMPACT · THE NEW CHAMBER'S FIRST BUDGET, IF UNCHANGED";

        /// <summary>The scope line's style: the caption face at the figures' size, wrapped, in the secondary ink.</summary>
        public static GUIStyle ScopeStyle(GUIStyle labelStyle)
        {
            var style = new GUIStyle(labelStyle) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            // 0.66 of the label (the figures' 0.72 was a line taller at 1280 and the Budget column at 720 has no line to give); the guard's 8 px floor holds it.
            style.fontSize = Mathf.Max(8, Mathf.RoundToInt(labelStyle.fontSize * 0.66f));
            style.normal.textColor = PoliSimTheme.TextSecondary;
            return style;
        }

        /// <summary>The scope line under a panel on an IMGUI sheet - laid out by GUILayout so it wraps to the column it is in.</summary>
        public static void DrawScopeLine(GUIStyle labelStyle)
        {
            GUILayout.Label(ScopeLine, ScopeStyle(labelStyle));
        }

        /// <summary>Board 5c's figures line for a painted plate: each figure signed in its own arrow's ink (uGUI rich text), in lane order, separated by middle dots.</summary>
        public static string FiguresLine(IReadOnlyList<EffectArrow> arrows)
        {
            var text = new System.Text.StringBuilder();
            foreach (EffectArrow a in arrows)
            {
                if (text.Length > 0) { text.Append("   ·   "); }
                text.Append(a.Name.ToUpperInvariant()).Append(' ')
                    .Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.GetDeltaColor(a.Value, a.HigherIsBetter))).Append('>')
                    .Append(a.Figure).Append("</color>");
            }
            return text.ToString();
        }

        public static float MeasureHeight(GUIStyle labelStyle)
        {
            float line = Mathf.Max(labelStyle.lineHeight, labelStyle.fontSize + 4f);
            return line * 3f + labelStyle.fontSize * 3f;   // P2-2.2: half a font size shorter, for the seat map above
        }

        /// <summary>Lays the arrows out across <paramref name="area"/>. An empty list draws the caller's own idle text instead; this draws nothing for it.</summary>
        public static void Draw(Rect area, IReadOnlyList<EffectArrow> arrows, GUIStyle labelStyle)
        {
            if (arrows == null || arrows.Count == 0 || Event.current.type != EventType.Repaint)
            {
                return;
            }

            float maxAbs = 0f;
            foreach (EffectArrow a in arrows) { maxAbs = Mathf.Max(maxAbs, Mathf.Abs(a.Value)); }
            if (maxAbs <= 0f) { return; }

            GUIStyle caption = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true, clipping = TextClipping.Overflow };
            caption.fontSize = Mathf.Max(8, Mathf.RoundToInt(labelStyle.fontSize * 0.72f));
            float line = Mathf.Max(caption.lineHeight, caption.fontSize + 4f);
            float nameHeight = line * 3f;   // three lines: "Labor force participation" needs them at a 1280 column
            float figureHeight = line;
            float lane = Mathf.Max(8f, area.height - nameHeight - figureHeight);
            float baselineY = area.y + figureHeight + lane * 0.5f;
            // The lanes are sized by their widest word (a caption never breaks inside a word) with the slack
            // shared equally; only when the words alone exceed the width do the lanes fall back to equal shares,
            // and then the overflow guard reports the break.
            float[] laneWidth = new float[arrows.Count];
            float[] laneX = new float[arrows.Count];
            // P4-1 film (2026-09-03): seven lanes at a 1280 column could not hold "participation" at the caption's
            // size and the fallback broke the word. The resort ladder's own order applies - the caption SHRINKS
            // (to the 8 px floor) until every lane holds its widest word, and only past the floor do the lanes fall
            // to equal shares, where the guard reports the break.
            float needSum = 0f;
            bool wordsFit = false;
            for (int size = caption.fontSize; size >= 8; size--)
            {
                caption.fontSize = size;
                needSum = 0f;
                for (int i = 0; i < arrows.Count; i++)
                {
                    float need = caption.CalcSize(new GUIContent(arrows[i].Figure)).x;
                    foreach (string word in arrows[i].Name.Split(' ')) { need = Mathf.Max(need, caption.CalcSize(new GUIContent(word)).x); }
                    laneWidth[i] = need + 2f;
                    needSum += laneWidth[i];
                }
                wordsFit = needSum <= area.width;
                if (wordsFit) { break; }
            }
            line = Mathf.Max(caption.lineHeight, caption.fontSize + 4f);
            nameHeight = line * 3f;
            figureHeight = line;
            lane = Mathf.Max(8f, area.height - nameHeight - figureHeight);
            baselineY = area.y + figureHeight + lane * 0.5f;
            float slack = wordsFit ? (area.width - needSum) / arrows.Count : 0f;
            float laneStart = area.x;
            for (int i = 0; i < arrows.Count; i++)
            {
                laneWidth[i] = wordsFit ? laneWidth[i] + slack : area.width / arrows.Count;
                laneX[i] = laneStart;
                laneStart += laneWidth[i];
            }
            float shaftWidth = Mathf.Max(2f, Mathf.Round(labelStyle.fontSize * 0.22f));
            float headLength = Mathf.Max(4f, Mathf.Round(lane * HeadLengthFraction * 0.5f));
            float headHalfWidth = shaftWidth * 2.2f;
            float halfLane = lane * 0.5f;

            Color previous = GUI.color;
            // Board 5c: the arrows rise and fall from ONE hairline baseline across the panel (the per-lane
            // tick it replaces said the same thing eight times).
            PoliSimTheme.Rule(new Rect(area.x, baselineY - 0.5f, area.width, 1f), PoliSimTheme.Hairline);
            for (int i = 0; i < arrows.Count; i++)
            {
                EffectArrow a = arrows[i];
                float cx = Mathf.Round(laneX[i] + laneWidth[i] * 0.5f);
                float fraction = Mathf.Abs(a.Value) / maxAbs;
                float length = Mathf.Max(halfLane * MinShaftFraction, halfLane * fraction);
                bool up = a.Value > 0f;
                Color ink = UiPalette.GetDeltaColor(a.Value, a.HigherIsBetter);
                GUI.color = ink;

                // The shaft and the stepped head - whiteTexture rects, no sprite invented.
                float shaftLength = Mathf.Max(1f, length - headLength);
                float shaftTop = up ? baselineY - shaftLength : baselineY;
                GUI.DrawTexture(new Rect(cx - shaftWidth * 0.5f, shaftTop, shaftWidth, shaftLength), Texture2D.whiteTexture);
                const int Steps = 5;
                for (int s = 0; s < Steps; s++)
                {
                    float t = (s + 0.5f) / Steps;                 // 0 near the shaft, 1 at the tip
                    float halfSpan = headHalfWidth * (1f - t);
                    float sliceHeight = headLength / Steps;
                    float y = up ? baselineY - shaftLength - headLength * t - sliceHeight * 0.5f
                                 : baselineY + shaftLength + headLength * t - sliceHeight * 0.5f;
                    GUI.DrawTexture(new Rect(cx - halfSpan, y, halfSpan * 2f, sliceHeight + 0.6f), Texture2D.whiteTexture);
                }

                GUI.color = previous;
                caption.normal.textColor = ink;
                var figureRect = new Rect(laneX[i], up ? area.y : area.y + area.height - nameHeight - figureHeight, laneWidth[i], figureHeight);
                if (!up)
                {
                    // A falling arrow's figure sits under its head, above the name row; a rising one's above the head.
                    figureRect.y = Mathf.Min(area.y + area.height - nameHeight - figureHeight, baselineY + length + 2f);
                }
                GUI.Label(figureRect, a.Figure, caption);
                caption.normal.textColor = PoliSimTheme.TextSecondary;
                GUI.Label(new Rect(laneX[i], area.y + area.height - nameHeight, laneWidth[i], nameHeight), a.Name, caption);
                // The name wraps to three lines by design, so the guard is asked the wrapped question: does the wrapped
                // height fit the three-line foot at this column width.
                UiOverflowGuard.Check(a.Name, new Vector2(laneWidth[i], caption.CalcHeight(new GUIContent(a.Name), laneWidth[i])), new Vector2(laneWidth[i], nameHeight), caption.fontSize);
            }

            GUI.color = previous;
        }
    }
}
