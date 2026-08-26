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
        /// <summary>
        /// ⚠ **THESE WERE FIXED px AND THAT WAS A LIVE CLIPPING BUG (fixed 2026-08-03).**
        ///
        /// Every style in this UI derives its `fontSize` from `Screen.height` (see
        /// `GameController.RescaleStylesToScreen`, which re-derives it every frame). These five constants
        /// did not. They are correct at `fontSize` 16 — the bottom of that clamp, i.e. the smallest window
        /// the game supports — and at any larger window the row was too short and too narrow for its own
        /// text. At 900px height the label font is 20px and the name was being drawn into an 18px-tall
        /// rect: the stat row rendered `Debt-to` / `Approval` / `Busines` / `Poverty` over
        /// vertically-cropped figures, on every policy screen, in production.
        ///
        /// **This is instance #8 of the class `PoliSimWidgets.MeasuredLabel` was written to end**, and it
        /// is in the one renderer that never adopted it. The lesson is not "use the helper" — it is that a
        /// helper only helps where someone remembered to call it, and the durable fix is for the GEOMETRY
        /// to derive from the same font size the text does. Each factor below reproduces the original
        /// constant exactly at `fontSize` 16 and scales from there, so nothing changes at the size these
        /// were authored for and everything grows correctly above it.
        ///
        /// `OverflowLineHeight` further down already did this (`fontSize + 6f`). The pattern was in the
        /// file; the row just never got it.
        /// </summary>
        private const float RowHeightPad = 8f;
        private const float IconSizePerFont = 22f / 16f;
        private const float SparklineWidthPerFont = 72f / 16f;
        private const float SparklineHeightPerFont = 20f / 16f;
        private const float MinChipWidthPerFont = 190f / 16f;

        /// <summary>
        /// One text line's height, taken from the STYLE'S OWN METRICS rather than from `fontSize` plus a
        /// guess.
        ///
        /// **The first version of this fix used `fontSize + 4f` and the values were still clipped.** A
        /// font's line height is not a fixed function of its point size - TeX Gyre Pagella has generous
        /// ascenders and descenders and needs roughly `fontSize + 8` at 20px, where the default sans did
        /// not. Guessing the padding replaces a constant that is wrong at every size but one with a
        /// constant that is wrong for every FACE but one, which is not progress. `GUIStyle.lineHeight`
        /// asks the font, so this now survives both a window resize and a change of typeface.
        ///
        /// The `fontSize + 4f` floor is a guard, not the answer: `lineHeight` can read 0 before a
        /// dynamic font has rasterised at a given size.
        ///
        /// ⚠ **THIRD VERSION, 2026-08-11, and `lineHeight` was still the wrong metric — for a third
        /// reason.** `fontSize + 4` was wrong per face; `lineHeight` asks the font and is right about the
        /// FONT, but a label does not occupy its font's line height. It occupies that plus the style's
        /// vertical padding, and `GUI.Label` lays out the second number. Measured on TeX Gyre Pagella at
        /// 20px: `lineHeight` 20.00, `CalcSize().y` 26.13. Every stat chip was provisioned 24.0 for
        /// something that renders at 26.1 — 111 of the 144 violations the overflow guard found.
        ///
        /// So it now asks for **the quantity that governs rendering**, which is the whole lesson of the
        /// two previous versions rather than a new one: `CalcSize` is what `MeasuredLabel` compares
        /// against and what `GUI.Label` obeys, so it is what a height budget must be derived from.
        ///
        /// The probe string is arbitrary BY MEASUREMENT, not by assumption: `CalcSize().y` returned an
        /// identical value for ASCII, for ring-accented capitals (`Åtgärd Ö`) and for digits at every
        /// size from 16 to 22. Unity reports the line box, which does not vary with content.
        /// </summary>
        private static readonly GUIContent LineProbe = new GUIContent("Ag");

        private static float LineHeightFor(GUIStyle s) => Mathf.Max(s.CalcSize(LineProbe).y, s.fontSize + 4f);

        private static float RowHeightFor(GUIStyle s) => LineHeightFor(s) * 2f + RowHeightPad;
        private static float IconSizeFor(GUIStyle s) => s.fontSize * IconSizePerFont;
        private static float SparklineWidthFor(GUIStyle s) => s.fontSize * SparklineWidthPerFont;
        private static float SparklineHeightFor(GUIStyle s) => s.fontSize * SparklineHeightPerFont;
        private static float MinChipWidthFor(GUIStyle s) => s.fontSize * MinChipWidthPerFont;

        /// <summary>
        /// A private copy of the caller's label style for chip text, because
        /// <see cref="PoliSimWidgets.MeasuredLabel"/> SHRINKS the style it is handed and the caller's
        /// `labelStyle` is shared with every other label on the screen. Rebuilt only when the source size
        /// or face changes, and its `fontSize` is reset on every call so one long stat name cannot leave
        /// the next chip permanently smaller.
        /// </summary>
        private static GUIStyle _chipText;
        private static int _chipTextBuiltAtSize = -1;

        private static GUIStyle ChipTextStyle(GUIStyle labelStyle)
        {
            if (_chipText == null || _chipTextBuiltAtSize != labelStyle.fontSize || _chipText.font != labelStyle.font)
            {
                _chipText = new GUIStyle(labelStyle) { wordWrap = false };
                _chipTextBuiltAtSize = labelStyle.fontSize;
            }

            _chipText.fontSize = labelStyle.fontSize;
            return _chipText;
        }

        /// <summary>
        /// Default cap on how many stats a screen shows. Four, because the widest area genuinely has
        /// seven: every tax and every spending line is Fiscal, and all of them touch DebtToGdp and
        /// Approval, so Fiscal's derived list is 7 stats deep. Seven chips at the two-per-row the
        /// Budget Process centre column has space for is four lines of chrome sitting on top of the
        /// line items the player actually came to edit. The overflow is stated, never silently cut.
        /// </summary>
        public const int DefaultMaxStats = 4;

        /// <summary>
        /// Height this row will occupy, for callers that must reserve space before drawing (every
        /// current caller does - a tab computes its content height up front and hands it to a
        /// ScrollView). Pass the SAME availableWidth and maxStats given to <see cref="Draw"/>, since
        /// chips-per-row is derived from width; both route through <see cref="ComputeLayout"/> so the
        /// measurement and the drawing cannot disagree. Returns 0 for an area with no levers, which is
        /// exactly what Draw then occupies.
        /// </summary>
        public static float MeasureHeight(UiPalette.SystemArea area, GUIStyle labelStyle, float availableWidth, int maxStats = DefaultMaxStats)
        {
            ComputeLayout(area, labelStyle, availableWidth, maxStats, out int shown, out _, out int lines, out int omitted);
            if (shown == 0)
            {
                return 0f;
            }

            return lines * RowHeightFor(labelStyle) + (omitted > 0 ? OverflowLineHeight(labelStyle) : 0f);
        }

        /// <summary>
        /// Draws the row, wrapping into as many lines as the available width needs. Draws nothing at
        /// all for an area with no policy levers - callers should not special-case that. Areas do
        /// legitimately have none: no Infrastructure policy node has a single Policy Web edge, so the
        /// Infrastructure screen correctly shows no stat row rather than an invented one.
        /// </summary>
        public static void Draw(UiPalette.SystemArea area, Country country, GUIStyle labelStyle, float availableWidth, int maxStats = DefaultMaxStats)
        {
            IReadOnlyList<StatNodeId> stats = PolicyScreenStats.GetStatsForArea(area);
            ComputeLayout(area, labelStyle, availableWidth, maxStats, out int shown, out int perRow, out _, out int omitted);
            if (shown == 0)
            {
                return;
            }

            float chipWidth = availableWidth / perRow;

            for (int i = 0; i < shown; i += perRow)
            {
                Rect line = GUILayoutUtility.GetRect(availableWidth, RowHeightFor(labelStyle));
                for (int c = 0; c < perRow && i + c < shown; c++)
                {
                    DrawChip(new Rect(line.x + c * chipWidth, line.y, chipWidth, RowHeightFor(labelStyle)), stats[i + c], country, labelStyle);
                }
            }

            // Says what was left out rather than trimming quietly, and names where the full picture
            // lives - the Policy Web is the same edge list this row is derived from, so it is the
            // honest answer to "affected how?", not a consolation link.
            if (omitted > 0)
            {
                Rect overflow = GUILayoutUtility.GetRect(availableWidth, OverflowLineHeight(labelStyle));
                Color previous = GUI.color;
                GUI.color = UiPalette.MutedIconTint;
                GUI.Label(new Rect(overflow.x + 4f, overflow.y, availableWidth - 8f, overflow.height),
                    $"+{omitted} more affected - see Policy Web", labelStyle);
                GUI.color = previous;
            }
        }

        /// <summary>
        /// Was `fontSize + 6f`. That scaled with the window, which is why the doc comment on the row
        /// constants credits it as already having the right idea — but `+6` is still a guess at a font's
        /// descender depth, and under TeX Gyre Pagella it clipped the tail of "Policy Web". Same
        /// correction as <see cref="LineHeightFor"/>: ask the style.
        /// </summary>
        private static float OverflowLineHeight(GUIStyle labelStyle) => LineHeightFor(labelStyle) + 2f;

        /// <summary>
        /// The single place chips-per-row, line count and overflow are decided, so
        /// <see cref="MeasureHeight"/> and <see cref="Draw"/> are the same calculation rather than two
        /// that agree until one is edited. This is the StatTile-formatter lesson applied to layout.
        /// </summary>
        /// <summary>MeasuredLabel's own shrink floor - the smallest size a chip label ever renders
        /// at, and therefore the size the width budget below must be derived from. The value is the
        /// guard's own report ("needs 89.2 wide ... at 8px"), not a guess.</summary>
        private const int LabelShrinkFloorPx = 8;

        private static void ComputeLayout(UiPalette.SystemArea area, GUIStyle labelStyle, float availableWidth, int maxStats, out int shown, out int perRow, out int lines, out int omitted)
        {
            IReadOnlyList<StatNodeId> stats = PolicyScreenStats.GetStatsForArea(area);
            perRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / MinChipWidthFor(labelStyle)));
            shown = Mathf.Min(stats.Count, Mathf.Max(1, maxStats));
            omitted = stats.Count - shown;

            // Free-aspect pass (2026-08-26): the per-font MinChipWidth constant is a measurement at
            // one aspect, and the 1640x707 sweep caught it: at that density the name column came to
            // 83.6px against "Labor Force Participation" needing 89.2 AT THE SHRINK FLOOR - a name
            // no shrink can save. So the chip floor is now ALSO derived from the widest name this
            // screen actually shows, measured at the floor size, plus the chip's fixed parts (the
            // same inset/icon/sparkline arithmetic DrawChip spends) - and perRow drops until every
            // chip can hold its own name. ChipTextStyle resets its fontSize on every call, so
            // borrowing it for the floor-size probe cannot leak a shrunken style (its own doc).
            GUIStyle probe = ChipTextStyle(labelStyle);
            probe.fontSize = LabelShrinkFloorPx;
            float widestNameAtFloor = 0f;
            for (int i = 0; i < shown; i++)
            {
                widestNameAtFloor = Mathf.Max(widestNameAtFloor, probe.CalcSize(new GUIContent(PolicyScreenStats.GetName(stats[i]))).x);
            }
            float chipFixedParts = 4f + (IconSizeFor(labelStyle) + 6f) + 10f + SparklineWidthFor(labelStyle) + 4f;
            float chipNeeded = widestNameAtFloor + chipFixedParts;
            if (chipNeeded > 0f)
            {
                perRow = Mathf.Clamp(Mathf.FloorToInt(availableWidth / chipNeeded), 1, perRow);
            }

            lines = Mathf.CeilToInt(shown / (float)perRow);
        }

        private static void DrawChip(Rect rect, StatNodeId stat, Country country, GUIStyle labelStyle)
        {
            // Step 2: an invisible click target over the whole chip, drawn FIRST so the labels
            // paint over it, and drawn EVERY frame for EVERY chip (stable control layout - the
            // control set never varies with state). Chips without a trace route to a no-op.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                StatTracePanel.NotifyChipClicked(stat);
            }

            float value = PolicyScreenStats.ReadLiveValue(stat, country);
            bool? higherIsBetter = PolicyScreenStats.GetHigherIsBetter(stat);

            IReadOnlyList<float> history = PolicyWebRenderer.GetHistory(stat, country.History);
            Color trendColor = GetTrendColor(history, higherIsBetter);

            GUIStyle text = ChipTextStyle(labelStyle);
            float iconSize = IconSizeFor(labelStyle);
            float sparkWidth = SparklineWidthFor(labelStyle);
            float sparkHeight = SparklineHeightFor(labelStyle);
            float lineHeight = LineHeightFor(labelStyle);

            float x = rect.x + 4f;

            // The icon is optional by design: IconLibrary returns null for a missing sprite, and a null
            // just shifts the label left rather than drawing a placeholder that would imply the wrong
            // stat. Every stat on this row now has one - icon_stat_interestrate, the last gap, landed
            // 2026-08-02 - but the contract stays, because it is what makes adding a stat ahead of its
            // art a non-event.
            Texture2D icon = IconLibrary.GetStat(GetIconName(stat));
            if (icon != null)
            {
                UiPalette.DrawTintedIcon(new Rect(x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize), icon, UiPalette.MutedIconTint);
                x += iconSize + 6f;
            }

            // MeasuredLabel rather than GUI.Label, per the standing rule this file was violating: SHRINK,
            // NEVER TRUNCATE. A clipped stat name is merely ugly; a clipped VALUE is a plausible-looking
            // wrong number, which is the worst failure a readout can have - and both were happening here.
            float textWidth = rect.xMax - x - sparkWidth - 10f;
            var nameRect = new Rect(x, rect.y + 2f, textWidth, lineHeight);
            PoliSimWidgets.MeasuredLabel(nameRect, PolicyScreenStats.GetName(stat), text);

            Color previous = GUI.color;
            GUI.color = trendColor;
            var valueRect = new Rect(x, rect.y + lineHeight + 2f, textWidth, lineHeight);
            PoliSimWidgets.MeasuredLabel(valueRect, PolicyScreenStats.Format(stat, value), ChipTextStyle(labelStyle));
            GUI.color = previous;

            // ⚠ `RowHeightFor` is `LineHeightFor * 2 + RowHeightPad` and these two rects are where that
            // budget is SPENT - two lines plus a 2px inset. The two expressions are the same fact stated
            // twice, and the height half has already been wrong three times (fontSize+4, then lineHeight,
            // then padding). Asserting here means the next change to either one has to be right in both.
            UiContainmentGuard.Check("StatChip name line", nameRect, rect);
            UiContainmentGuard.Check("StatChip value line", valueRect, rect);

            GraphRenderer.DrawSparkline(
                new Rect(rect.xMax - sparkWidth - 4f, rect.y + (rect.height - sparkHeight) * 0.5f, sparkWidth, sparkHeight),
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
                return UiPalette.NeutralChangeColor;
            }

            return UiPalette.GetDeltaColor(history[history.Count - 1] - history[history.Count - 2], higherIsBetter.Value);
        }

        /// <summary>
        /// Maps a stat to its delivered sprite filename. Kept explicit rather than derived from the enum
        /// name, because the delivered filenames follow the EconomyState field names the asset request
        /// was built from, which do not all match StatNodeId's shorter names - "Poverty" against
        /// icon_stat_povertyrate, "Crime" against icon_stat_crimeindex, and so on. A ToLower() would
        /// silently miss those and draw nothing.
        ///
        /// Public so a batch-mode Editor check can enumerate the mapping and confirm every name resolves
        /// to a real sprite. That is the standing rule from the sparkline crash applied to a lookup
        /// rather than to maths: this mapping's only caller is inside a draw call, and a wrong name here
        /// fails silently by design (null draws nothing), so the one place it can be verified is a
        /// headless pass over the enum. The missing interest-rate icon was found by cross-referencing
        /// names against the disk BY HAND - this is that check, made runnable.
        /// </summary>
        public static string GetIconName(StatNodeId stat)
        {
            switch (stat)
            {
                case StatNodeId.Gdp: return "icon_stat_gdp";
                case StatNodeId.Unemployment: return "icon_stat_unemployment";
                case StatNodeId.Inflation: return "icon_stat_inflation";
                case StatNodeId.Approval: return "icon_stat_approvalrating";
                case StatNodeId.DebtToGdp: return "icon_stat_debttogdpratio";
                case StatNodeId.Poverty: return "icon_stat_povertyrate";
                case StatNodeId.InterestRate: return "icon_stat_interestrate";
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
