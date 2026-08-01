using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Master Sequence step 9, Step B2 (rendering): draws the contextual stat row on a policy screen -
    /// the stats that screen's own levers actually move, each with its current value and a compact
    /// sparkline.
    ///
    /// **Which stats appear is not decided here.** <see cref="PolicyScreenStats"/> derives that from
    /// the Policy Web's real edge list, so this file only lays out whatever it is handed. Adding a
    /// policy-to-stat relationship anywhere in the Policy Web makes it appear here automatically, with
    /// no second list to keep in sync.
    ///
    /// **Values are LIVE, not published**, per Elias's ruling on Open Question A3 (2026-08-01), which
    /// overruled the directive's original "current published value" wording. A lagged, possibly
    /// preliminary figure sitting in a "what am I doing right now" panel misrepresents itself, and the
    /// instruction was only ever satisfiable for 6 of the 18 policy-screen stats anyway. The published
    /// view lives on the Statistics tab.
    /// </summary>
    public static class PolicyScreenStatsRenderer
    {
        private const float RowHeight = 44f;
        private const float IconSize = 22f;
        private const float SparklineWidth = 72f;
        private const float SparklineHeight = 20f;
        private const float MinChipWidth = 190f;

        /// <summary>
        /// Draws the row, wrapping into as many lines as the available width needs. Draws nothing at
        /// all for an area with no policy levers - callers should not special-case that.
        /// </summary>
        public static void Draw(UiPalette.SystemArea area, Country country, GUIStyle labelStyle, float availableWidth)
        {
            IReadOnlyList<StatNodeId> stats = PolicyScreenStats.GetStatsForArea(area);
            if (stats.Count == 0)
            {
                return;
            }

            int perRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / MinChipWidth));
            float chipWidth = availableWidth / perRow;

            for (int i = 0; i < stats.Count; i += perRow)
            {
                Rect line = GUILayoutUtility.GetRect(availableWidth, RowHeight);
                for (int c = 0; c < perRow && i + c < stats.Count; c++)
                {
                    DrawChip(new Rect(line.x + c * chipWidth, line.y, chipWidth, RowHeight), stats[i + c], country, labelStyle);
                }
            }
        }

        private static void DrawChip(Rect rect, StatNodeId stat, Country country, GUIStyle labelStyle)
        {
            float value = PolicyScreenStats.ReadLiveValue(stat, country);
            bool? higherIsBetter = PolicyScreenStats.GetHigherIsBetter(stat);

            IReadOnlyList<float> history = PolicyWebRenderer.GetHistory(stat, country.History);
            Color trendColor = GetTrendColor(history, higherIsBetter);

            float x = rect.x + 4f;

            // The icon is optional by design: IconLibrary returns null for a missing sprite, and
            // icon_stat_interestrate genuinely does not exist yet. A null just shifts the label left
            // rather than drawing a placeholder that would imply the wrong stat.
            Texture2D icon = IconLibrary.GetStat(GetIconName(stat));
            if (icon != null)
            {
                UiPalette.DrawTintedIcon(new Rect(x, rect.y + (RowHeight - IconSize) * 0.5f, IconSize, IconSize), icon, UiPalette.MutedTextColor);
                x += IconSize + 6f;
            }

            float textWidth = rect.xMax - x - SparklineWidth - 10f;
            GUI.Label(new Rect(x, rect.y + 2f, textWidth, 18f), PolicyScreenStats.GetName(stat), labelStyle);

            Color previous = GUI.color;
            GUI.color = trendColor;
            GUI.Label(new Rect(x, rect.y + 20f, textWidth, 20f), PolicyScreenStats.Format(stat, value), labelStyle);
            GUI.color = previous;

            GraphRenderer.DrawSparkline(
                new Rect(rect.xMax - SparklineWidth - 4f, rect.y + (RowHeight - SparklineHeight) * 0.5f, SparklineWidth, SparklineHeight),
                history, trendColor);
        }

        /// <summary>
        /// Colours the value and its sparkline by the SAME good/bad judgment the Policy Web uses, so the
        /// two surfaces cannot disagree about whether a number turning green makes sense. Neutral for a
        /// stat with no context-free direction (inflation, interest rates) and while history is too
        /// short to have a direction at all.
        /// </summary>
        private static Color GetTrendColor(IReadOnlyList<float> history, bool? higherIsBetter)
        {
            if (higherIsBetter == null || history == null || history.Count < 2)
            {
                return UiPalette.MutedTextColor;
            }

            return UiPalette.GetDeltaColor(history[history.Count - 1] - history[history.Count - 2], higherIsBetter.Value);
        }

        /// <summary>
        /// Maps a stat to its delivered sprite filename. Kept explicit rather than derived from the enum
        /// name, because the delivered filenames follow the EconomyState field names the asset request
        /// was built from, which do not all match StatNodeId's shorter names - "Poverty" against
        /// icon_stat_povertyrate, "Crime" against icon_stat_crimeindex, and so on. A ToLower() would
        /// silently miss those and draw nothing.
        /// </summary>
        private static string GetIconName(StatNodeId stat)
        {
            switch (stat)
            {
                case StatNodeId.Gdp: return "icon_stat_gdp";
                case StatNodeId.Unemployment: return "icon_stat_unemployment";
                case StatNodeId.Inflation: return "icon_stat_inflation";
                case StatNodeId.Approval: return "icon_stat_approvalrating";
                case StatNodeId.DebtToGdp: return "icon_stat_debttogdpratio";
                case StatNodeId.Poverty: return "icon_stat_povertyrate";
                case StatNodeId.InterestRate: return "icon_stat_interestrate"; // not yet delivered - null is correct
                case StatNodeId.TradeBalance: return "icon_stat_tradebalance";
                case StatNodeId.Lfpr: return "icon_stat_laborforceparticipationrate";
                case StatNodeId.Crime: return "icon_stat_crimeindex";
                case StatNodeId.PrisonPopulation: return "icon_stat_prisonpopulationrate";
                case StatNodeId.OrganizedCrime: return "icon_stat_organizedcrimeindex";
                case StatNodeId.Corruption: return "icon_stat_corruptionindex";
                case StatNodeId.PotentialGrowth: return "icon_stat_potentialgdp";
                case StatNodeId.PopulationGrowthRate: return "icon_stat_populationgrowthrate";
                case StatNodeId.DependencyRatio: return "icon_stat_dependencyratio";
                case StatNodeId.ConsumerConfidence: return "icon_stat_consumerconfidence";
                case StatNodeId.BusinessConfidence: return "icon_stat_businessconfidence";
                default: return null;
            }
        }
    }
}
