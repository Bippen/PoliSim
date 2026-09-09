using System.Collections.Generic;
using System.Globalization;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D16-3, board 10c (2026-09-09, §412) - THE RIKSBANK as rate · verdict · path, and the rule as an instrument.
    ///
    /// <para><b>The verdict</b> is the sentence the page exists to say: the rule wants three quarters of a point more than you have. It
    /// was the fifth numeral in a formula row; it is now the rule's numeral in the rule's own ink under the live rate, with 5c's arrow
    /// back to today, the delta, and one line saying which way and how fast the path closes.</para>
    ///
    /// <para><b>The waterfall</b> (§5.2) replaces a row of numerals with `+` between them, which makes the player do the sum. Positive
    /// terms lay left to right at a FIXED scale - 1 point = 130 px, never fitted to the year - the negative terms are a CUT taken out of
    /// the right end, and the solid ink that remains IS the answer. Three rules make two years comparable: the terms are always in the
    /// same order, the scale never changes, and a zero term is drawn as nothing and labelled struck through, because "the inflation gap
    /// contributed nothing" is itself a reading.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>§5.2: the board's fixed scale - one point of rate is 130 px at the board's 1149 sheet, scaled with the window like every other length.</summary>
        private float RuleWaterfallScale() => StatsUnit(130f);

        /// <summary>
        /// §5.1's second thing: the verdict. The rule's numeral at mono 26 in Caution - the rule's own ink - then 5c's arrow back to
        /// today and the delta, then one line: `ABOVE TODAY · CLOSED AT 0.15 ⁄ YR`.
        /// </summary>
        private void DrawRiksbankVerdict(float rate, float suggested, float width)
        {
            GUIStyle label = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle numeral = DeskCaption(26f, PoliSimTheme.Caution, true, TextAnchor.MiddleLeft);
            GUIStyle deltaStyle = DeskCaption(11f, PoliSimTheme.Caution, true, TextAnchor.MiddleLeft);
            float capH = Mathf.Ceil(DeskCaptionHeight(label));
            float numH = Mathf.Ceil(DeskCaptionHeight(numeral));
            Rect area = GUILayoutUtility.GetRect(width, capH * 2f + numH + StatsUnit(10f), GUILayout.Width(width));
            if (Event.current.type != EventType.Repaint) { return; }

            float delta = suggested - rate;
            PoliSimWidgets.MeasuredLabel(new Rect(area.x, area.y, width, capH), "THE RULE READS", label);
            float numeralW = numeral.CalcSize(new GUIContent(suggested.ToString("F2", CultureInfo.InvariantCulture) + "%")).x;
            PoliSimWidgets.MeasuredLabel(new Rect(area.x, area.y + capH, numeralW, numH), suggested.ToString("F2", CultureInfo.InvariantCulture) + "%", numeral);

            // 5c's arrow back to today: a 1 px stem, a 2 px bar the length of the move, a 7 px head - all in the rule's ink.
            float arrowX = area.x + numeralW + StatsUnit(10f);
            float arrowW = Mathf.Max(StatsUnit(16f), Mathf.Min(StatsUnit(52f), Mathf.Abs(delta) * RuleWaterfallScale() * 0.4f));
            float midY = area.y + capH + numH * 0.5f;
            PoliSimTheme.Rule(new Rect(arrowX, midY - 0.5f, arrowW, 2f), PoliSimTheme.Caution);
            float head = StatsUnit(7f);
            bool up = delta >= 0f;
            for (int i = 0; i < 4; i++)
            {
                float t = (i + 1f) / 4f;
                float px = up ? arrowX + arrowW - t * head : arrowX + t * head;
                PoliSimTheme.Rule(new Rect(px - 0.5f, midY - head * 0.5f * (1f - t) - 1f, 1f, head * (1f - t) + 2f), PoliSimTheme.Caution);
            }
            PoliSimWidgets.MeasuredLabel(new Rect(arrowX + arrowW + StatsUnit(6f), midY - Mathf.Ceil(DeskCaptionHeight(deltaStyle)) * 0.5f, width * 0.4f, Mathf.Ceil(DeskCaptionHeight(deltaStyle))),
                delta.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture), deltaStyle);

            string direction = Mathf.Abs(delta) < 0.005f ? "AT TODAY" : delta > 0f ? "ABOVE TODAY" : "BELOW TODAY";
            PoliSimWidgets.MeasuredLabel(new Rect(area.x, area.y + capH + numH, width, capH),
                direction + " · CLOSED AT " + FederalReserveSystem.RateAdjustmentSpeed.ToString("0.##", CultureInfo.InvariantCulture) + " ⁄ YR", label);
        }

        /// <summary>
        /// §5.2, the rule as a waterfall. The positive terms lay left to right in the rule's fixed order, alternating hairline inks; the
        /// negative terms are a CUT taken out of the right end, drawn as paper behind a dashed Bad edge and labelled `TAKEN BACK`; the
        /// total tick is the rule's own ink with its numeral beside it. A zero term draws nothing and is labelled struck through.
        /// </summary>
        private void DrawRuleWaterfall(float neutralReal, float inflation, float inflationGapTerm, float unemploymentGapTerm, float total, float width)
        {
            GUIStyle termLabel = DeskCaption(8f, PoliSimTheme.TextMuted);
            GUIStyle totalStyle = DeskCaption(14f, PoliSimTheme.Caution, true, TextAnchor.MiddleLeft);
            GUIStyle cutLabel = DeskCaption(7.5f, PoliSimTheme.Bad, true, TextAnchor.MiddleCenter);
            float scale = RuleWaterfallScale();
            float lane = StatsUnit(34f);
            float labelH = Mathf.Ceil(DeskCaptionHeight(termLabel));
            Rect area = GUILayoutUtility.GetRect(width, lane + labelH * 2f + StatsUnit(8f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }

            var terms = new (string Name, float Value)[]
            {
                ("NEUTRAL REAL", neutralReal),
                ("INFLATION", inflation),
                ("INFLATION GAP × " + TaylorRule.InflationGapWeight(_playerCountry).ToString("0.##", CultureInfo.InvariantCulture), inflationGapTerm),
                ("U-GAP × " + TaylorRule.UnemploymentGapWeight(_playerCountry).ToString("0.##", CultureInfo.InvariantCulture), unemploymentGapTerm),
            };

            float segTop = area.y + StatsUnit(6f);
            float segH = StatsUnit(22f);
            var labels = new List<(float X, string Text, Color Ink, bool Struck)>();
            // §8.6: the geometry is RuleWaterfall's - the same computation the acceptance bar asserts, so the drawing cannot drift from it.
            RuleWaterfall.Geometry geometry = RuleWaterfall.Compute(terms, total, scale);
            int positiveIndex = 0;
            foreach (RuleWaterfall.Segment seg in geometry.Positives)
            {
                if (seg.Zero)
                {
                    labels.Add((area.x + seg.X, "0.00  " + seg.Name, PoliSimTheme.TextMuted, true));
                    continue;
                }
                PoliSimTheme.Rule(new Rect(area.x + seg.X, segTop, seg.Width, segH), positiveIndex % 2 == 0 ? PoliSimTheme.HairlineStrong : PoliSimTheme.RuleFill);
                labels.Add((area.x + seg.X, seg.Value.ToString("+0.00", CultureInfo.InvariantCulture) + "  " + seg.Name, PoliSimTheme.TextMuted, false));
                positiveIndex++;
            }
            float positiveSum = area.x + geometry.PositiveSum;
            float cutFrom = area.x + geometry.CutFrom;
            if (geometry.CutWidth > 0.5f)
            {
                PoliSimTheme.Rule(new Rect(cutFrom, segTop, positiveSum - cutFrom, segH), PoliSimTheme.Card);
                DrawDashedRule(new Rect(cutFrom, segTop, positiveSum - cutFrom, 1f), PoliSimTheme.Bad, 4f, 3f);
                DrawDashedRule(new Rect(cutFrom, segTop + segH - 1f, positiveSum - cutFrom, 1f), PoliSimTheme.Bad, 4f, 3f);
                if (cutLabel.CalcSize(new GUIContent("TAKEN BACK")).x <= positiveSum - cutFrom)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(cutFrom, segTop + segH * 0.5f - StatsUnit(5f), positiveSum - cutFrom, StatsUnit(10f)), "TAKEN BACK", cutLabel);
                }
                foreach (RuleWaterfall.Segment seg in geometry.Negatives)
                {
                    labels.Add((area.x + seg.X, seg.Value.ToString("0.00", CultureInfo.InvariantCulture) + "  " + seg.Name, PoliSimTheme.Bad, false));
                }
            }
            // The zero baseline and the total tick.
            PoliSimTheme.Rule(new Rect(area.x, area.y, 1f, StatsUnit(42f)), PoliSimTheme.HairlineStrong);
            PoliSimTheme.Rule(new Rect(area.x + total * scale - 1.5f, area.y, 3f, StatsUnit(46f)), PoliSimTheme.Caution);
            PoliSimWidgets.MeasuredLabel(new Rect(area.x + total * scale + StatsUnit(5f), area.y, StatsUnit(60f), StatsUnit(20f)),
                total.ToString("F2", CultureInfo.InvariantCulture), totalStyle);

            // The labels sit under the bar at their own x - the figure in bold, then the term's name.
            float labelY = area.y + lane;
            foreach ((float lx, string text, Color ink, bool struck) in labels)
            {
                var style = DeskCaption(8f, ink);
                float w = Mathf.Min(style.CalcSize(new GUIContent(text)).x + StatsUnit(4f), area.xMax - lx);
                PoliSimWidgets.MeasuredLabel(new Rect(lx, labelY, w, labelH), text, style);
                if (struck) { PoliSimTheme.Rule(new Rect(lx, labelY + labelH * 0.5f, w - StatsUnit(4f), 1f), PoliSimTheme.TextMuted); }
                labelY += labelH;   // one under the other where two would collide at this width; the board lays them along the bar at 1149
            }
        }
    }
}
