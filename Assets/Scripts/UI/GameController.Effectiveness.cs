using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C7 (2026-09-05, late) - THE CABINET'S EFFECTIVENESS READOUT, Design's board 9d (D15 item 4): on the minister's card a readout block under
    /// the attribute rows, separated by a rule and directly beneath EFFICIENCY (its multiplier) - the ratio at mono 15 bold (×0.91), 5c's arrow from a
    /// baseline that is UNITY (length ∝ |r − 1| against the largest departure in the Cabinet, pointing left below 1 and right above), the decomposition
    /// beneath as two ratios and a word (0.965 ALLOC ÷ REQ × 0.94 EFFICIENCY), and the scope line. No money on the card - the amounts live on the
    /// Budget row. Below unity the ratio takes Bad; at or above it inkText (met is not a verdict, overfunding is not Good). The state line under the
    /// name says the consequence in words the coupling table owns. A ratio, never a score: ×, two decimals, no 0–100 scale, no bar-to-full. The Health
    /// minister's card also quotes the family's three KEYS (9c: "the card quotes, the page reads") - figure + unit + source, no band, no arrows.
    /// </summary>
    public partial class GameController
    {
        private void DrawEffectivenessReadout(CabinetPortfolio portfolio)
        {
            Country country = _playerCountry;
            float ratio = Effectiveness.RatioOf(country, portfolio);
            float efficiency = Effectiveness.EfficiencyOf(country, portfolio);
            float allocation = country.Effectiveness != null && country.Effectiveness.TryGetValue(portfolio, out PortfolioEffectiveness e) && e.Recorded ? e.AllocationRatio : 1f;
            bool below = ratio < 0.995f;
            Color ratioInk = below ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary;

            GUILayout.Space(4f);
            Rect rule = GUILayoutUtility.GetRect(10f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(rule, PoliSimTheme.Hairline); }
            GUILayout.Space(3f);

            GUIStyle head = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle ratioStyle = DeskCaption(15f, ratioInk, true, TextAnchor.MiddleLeft);
            GUIStyle decomposition = DeskCaption(7.5f, PoliSimTheme.TextSecondary);
            GUIStyle scope = DeskCaption(6.5f, PoliSimTheme.TextMuted);
            float rowH = Mathf.Max(Mathf.Ceil(DeskCaptionHeight(ratioStyle)), 18f);
            Rect row = GUILayoutUtility.GetRect(10f, rowH, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                float headW = head.CalcSize(new GUIContent("EFFECTIVENESS")).x + 8f;
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, headW, rowH), "EFFECTIVENESS", head);
                string ratioText = "×" + ratio.ToString("0.00", CultureInfo.InvariantCulture);
                float ratioW = ratioStyle.CalcSize(new GUIContent(ratioText)).x + 10f;
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + headW, row.y, ratioW, rowH), ratioText, ratioStyle);
                // 5c's arrow from unity: length against the largest departure in the Cabinet; the minimum arrow for zero departure (zero is a figure).
                float laneX = row.x + headW + ratioW + 6f;
                float laneW = Mathf.Max(20f, row.xMax - laneX - 4f);
                float largest = Mathf.Max(0.0001f, Effectiveness.LargestDeparture(country));
                float length = Mathf.Clamp(Mathf.Abs(ratio - 1f) / largest, 0.08f, 1f) * laneW * 0.5f;
                float midX = laneX + laneW * 0.5f;
                float midY = row.y + rowH * 0.5f;
                PoliSimTheme.Rule(new Rect(midX - 0.5f, midY - rowH * 0.3f, 1f, rowH * 0.6f), PoliSimTheme.TextMuted);   // unity, the baseline
                float endX = below ? midX - length : midX + length;
                PoliSimTheme.Rule(new Rect(Mathf.Min(midX, endX), midY - 0.75f, Mathf.Abs(endX - midX), 1.5f), ratioInk);
                float headSize = 3f;
                for (int i = 0; i < 3; i++) { float t = (i + 1) / 3f; PoliSimTheme.Rule(new Rect(endX + (below ? t * headSize : -t * headSize) - 0.5f, midY - headSize * (1f - t), 1f, headSize * 2f * (1f - t) + 1f), ratioInk); }
            }
            GUILayout.Label(allocation.ToString("0.000", CultureInfo.InvariantCulture) + " ALLOC ÷ REQ  ×  " + efficiency.ToString("0.00", CultureInfo.InvariantCulture) + " EFFICIENCY", decomposition);
            GUILayout.Label("UNITY IS THE BASELINE · BELOW = UNDERFUNDED · THE MONEY IS ON THE BUDGET ROW", scope);
        }

        /// <summary>The state line under the minister's name - the consequence in words the coupling table owns; text, not colour (9d).</summary>
        private string EffectivenessStateLine(CabinetPortfolio portfolio)
        {
            float ratio = Effectiveness.RatioOf(_playerCountry, portfolio);
            if (ratio < 0.995f)
            {
                return portfolio == CabinetPortfolio.HealthSocialAffairs ? "UNDERFUNDED — WAITS RISE, QUALITY DRIFTS" : "UNDERFUNDED — THE PORTFOLIO'S OUTCOMES DRIFT WHEN A FAMILY READS IT";
            }
            return ratio > 1.005f ? "ABOVE UNITY — MET IS NOT A VERDICT" : "MET — THE REQUEST IS FUNDED";
        }

        /// <summary>9c's last panel: the Health minister's card carries the family's three KEYS as figure + unit + source - no band, no arrows, no readouts.</summary>
        private void DrawHealthKeysOnCard()
        {
            HealthSeeds h = _playerCountry.Health;
            if (h == null || !h.Seeded) { return; }
            EconomyState s = _playerCountry.State;
            GUIStyle head = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle key = DeskCaption(9f, PoliSimTheme.TextPrimary, true);
            GUIStyle unit = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUILayout.Space(3f);
            GUILayout.Label("HEALTH · KEYS · FROM THE FAMILY PLATE", head);
            DrawCardKey("Coverage", PlateFigure(s.HealthCoverage, 0, " %"), "OF POPULATION · OECD HEALTH_PROT", key, unit);
            DrawCardKey("Treatable mortality", PlateFigure(s.TreatableMortality, 0) + " / 100 000", "AGE-STANDARDISED · OECD DF_AM", key, unit);
            DrawCardKey("Wait · knee", h.HasWaits ? PlateFigure(s.WaitKneeDays, 0) + " days" : "absent", h.HasWaits ? "MEAN DAYS · OECD DF_WAITING" : "NO COMPARABLE SERIES PUBLISHED", key, unit);
        }

        private void DrawCardKey(string name, string figure, string source, GUIStyle key, GUIStyle unit)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, DeskBody(11f, PoliSimTheme.TextPrimary), GUILayout.Width(150f));
            GUILayout.Label(figure, key, GUILayout.Width(110f));
            GUILayout.Label(source, unit);
            GUILayout.EndHorizontal();
        }
    }
}
