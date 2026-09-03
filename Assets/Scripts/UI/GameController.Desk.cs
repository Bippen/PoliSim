using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// UI v3.0 Phase B (2026-08-28) — Screen 0, "The Desk", built against Design's board 1m ("Screen 0 —
    /// The Desk, folded", drawn 2026-08-28 at 1280×720 against Annex A's census and Annex B's measured
    /// minimums). The landing surface in the FOLDED shell: a full-bleed instrument stage above the six
    /// documents, composed only from renderers the project already has — the world map, the approval
    /// ledger's own terms, the compass on its honest footprint (R-SP4), the policy preview's eight
    /// figures as bars, the 1k calendar sheet, the event card, the ten headline readings as a chip
    /// strip — and nothing authored: every string is a mono caption or an instrument's label/numeral;
    /// every figure is one the inventory says the game holds.
    ///
    /// THE BOARD'S OWN MEASURES ARE THE LAYOUT. Revised to board 1m-r2 (UI v3.1 Phase B, 2026-08-28:
    /// D4's tokens applied, the Year-0 empty states designed, drawn at Sweden Year 0): the sheet's
    /// inner area at 1280×720 is 1156×680 and the board places the masthead (28), three columns
    /// 440/250/440 with 13 px gaps and an 8 px top margin (the map 320 over the ledger 244; the
    /// compass 250 over the effects card 314; the calendar 420 over the event reservation 144), a
    /// rule at +8 and the chip strip integrated into the sheet (no plates, hairline dividers) inside
    /// it; every rect here is that placement scaled by the inner area's ratio to the board's, so at
    /// 1280×720 the stage IS the board and at every other size it is the board's proportions. Type
    /// comes from the frame's own height-derived styles at the board's px sizes scaled from 720
    /// (DeskPx), floored at D4's 9 px. The Year-0 empty states (1m-r2): the ledger's nine rows with
    /// em-dash figures and a FIRST ATTRIBUTION chip naming the model's first boundary; the effects
    /// card's zero rows on bare tracks under a dashed NO DRAFT PENDING caption; a dotted baseline
    /// ending in today's dot on a chip without a history; the event reservation drawn as a dashed
    /// frame with its two captions. Zero is a reading, not an absence.
    ///
    /// The split (board 1m, D1): the chrome census folds with the chrome, so every chrome (a) lands
    /// here or on the rail; the content rows keep their document (Statistics stands one rail cell
    /// away) and the stage restates only the ten headlines, as the strip. Deviations the build
    /// declares beyond the board's seven are logged in COMPLETED.md §41 (the R-B rulings).
    /// </summary>
    public partial class GameController
    {
        /// <summary>Screen 0 is above the six documents: while true the folded frame draws the stage
        /// instead of a tab. Not persisted - a loaded game lands on the Desk, as a new one does
        /// (R-B1). Set by SelectPlayerCountry, the rail's calendar chip and a document's own rail icon
        /// clicked again (R-B2); cleared by any rail icon.</summary>
        private bool _onDesk;

        /// <summary>The harness reads it to assert the state it films.</summary>
        internal bool OnDesk => _onDesk;

        /// <summary>The stage's inner rect as the last Repaint measured it - what the Layout event sees (DrawDeskStage).</summary>
        private Rect _deskInnerRect;

        /// <summary>Board 1m-r2's sheet at 1280×720: the inner area the board laid its instruments in (1180×704 less the 12 padding).</summary>
        private const float DeskBoardInnerWidth = 1156f;
        private const float DeskBoardInnerHeight = 680f;
        private const float DeskBoardHeight = 720f;

        /// <summary>The effects card's display ranges - the bars' axis scale per figure (a presentation
        /// choice, declared here, never printed as a figure; the numeral beside each bar is the
        /// estimate itself). In the figure's own unit: percent of GDP growth, points, approval, and
        /// for the net budget a share of GDP.</summary>
        private const float DeskRangeGdpGrowthPercent = 3f;
        private const float DeskRangeUnemploymentPoints = 2f;
        private const float DeskRangeInflationPoints = 2f;
        private const float DeskRangeApproval = 5f;
        private const float DeskRangePovertyPoints = 2f;
        private const float DeskRangeParticipationPoints = 2f;
        private const float DeskRangeCrimeIndex = 5f;
        private const float DeskRangeNetBudgetShareOfGdp = 0.02f;

        /// <summary>The event card's three bars: the shock's own units (percent of GDP, inflation points, approval).</summary>
        private const float DeskRangeEventGdpPercent = 5f;
        private const float DeskRangeEventInflationPoints = 3f;
        private const float DeskRangeEventApproval = 10f;

        // ------------------------------------------------------------------------------------------
        // Type: the board's px sizes at 720, scaled by the window height, floored at the guard's 8.
        // ------------------------------------------------------------------------------------------
        /// <summary>D4 (2026-08-28): the Desk caption floor 8 → 9 (the guard's 8 stays the shrink floor everywhere else).</summary>
        private const int DeskCaptionFloorPx = 9;

        private static int DeskPx(float boardPx)
        {
            return Mathf.Max(DeskCaptionFloorPx, Mathf.RoundToInt(boardPx * Screen.height / DeskBoardHeight));
        }

        private static GUIStyle Inked(GUIStyle style, Color ink)
        {
            // Every state: GUI.Label draws the hover face when the cursor rests on it (the v3a film's
            // black hover ink), so a Desk style inks all four (polisim-imgui-layout-facts, item 5).
            style.normal.textColor = ink;
            style.hover.textColor = ink;
            style.active.textColor = ink;
            style.focused.textColor = ink;
            return style;
        }

        /// <summary>A mono caption (Courier, the document face): upper-case by convention at the call sites, one line, no wrap.</summary>
        private GUIStyle DeskCaption(float boardPx, Color ink, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle(_calendarMetaStyle)
            {
                fontSize = DeskPx(boardPx),
                alignment = anchor,
                wordWrap = false,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal
            };
            style.padding = new RectOffset(0, 0, 0, 0);
            return Inked(style, ink);
        }

        /// <summary>The caption that may wrap (C20's methodology line, the event's description).</summary>
        private GUIStyle DeskCaptionWrapped(float boardPx, Color ink)
        {
            GUIStyle style = DeskCaption(boardPx, ink, false, TextAnchor.UpperLeft);
            style.wordWrap = true;
            return style;
        }

        /// <summary>Body type (Pagella): the instrument labels the board sets at 11-13 px.</summary>
        private GUIStyle DeskBody(float boardPx, Color ink, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle(_labelStyle) { fontSize = DeskPx(boardPx), alignment = anchor, wordWrap = false };
            style.padding = new RectOffset(0, 0, 0, 0);
            return Inked(style, ink);
        }

        /// <summary>A numeral in the display weight (the header style's bold face).</summary>
        private GUIStyle DeskNumeral(float boardPx, Color ink, TextAnchor anchor = TextAnchor.LowerLeft)
        {
            var style = new GUIStyle(_headerStyle) { fontSize = DeskPx(boardPx), alignment = anchor, wordWrap = false };
            style.padding = new RectOffset(0, 0, 0, 0);
            return Inked(style, ink);
        }

        private static float DeskCaptionHeight(GUIStyle caption)
        {
            return caption.CalcSize(new GUIContent("ÅG")).y;
        }

        // ------------------------------------------------------------------------------------------
        // The stage.
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Screen 0 in the folded frame's content column: one paper sheet, the board's placements
        /// inside it. Every instrument draws into a rect derived from the board (see the class doc);
        /// the calendar sheet alone is a GUILayout island (BeginArea) because the 1k month grid is
        /// GUILayout, and its ledger rows are rect-drawn beneath the grid on the Repaint the grid's
        /// own rect is known on. The game-over stamp (C4/C5) is an overlay over the dimmed stage.
        /// </summary>
        private void DrawDeskStage(float availableHeight, float availableWidth, bool isTimePaused)
        {
            // The caller already took the box's padding and margin out of availableHeight (instance
            // #12's reserve, the same figure every tab budgets its scroll view against), so it IS the
            // inner height; the box is laid out at that plus its padding, and the margin the frame
            // adds brings it to the column's full height - the sheet stands as tall as the rail. The
            // first 1m-r2 film took the reserve twice and left a dark band under the sheet.
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);
            float innerHeight = availableHeight;

            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, innerHeight, GUILayout.Width(innerWidth), GUILayout.Height(innerHeight));
            GUILayout.EndVertical();

            // IMGUI hands a 1×1 dummy rect back during the Layout event and the real one on every
            // other event; the calendar island (BeginArea) lays its grid out on the LAYOUT event, so it
            // must see the real rect then - the one the last Repaint measured (the frame is stable
            // between repaints; the first frame lays the island out at the dummy, the second at the
            // rect). The first v3desk film had the month grid laid out one pixel wide for this.
            if (Event.current.type == EventType.Repaint)
            {
                _deskInnerRect = inner;
            }
            else if (_deskInnerRect.width > 1f)
            {
                inner = _deskInnerRect;
            }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) => new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            // Board 1m-r2's placements at the 1156×680 inner area.
            DrawDeskMasthead(Board(0f, 0f, 1156f, 28f), isTimePaused);

            DrawDeskMapPlate(Board(0f, 36f, 440f, 320f));
            DrawDeskApprovalLedger(Board(0f, 368f, 440f, 244f));

            DrawDeskCompass(Board(453f, 36f, 250f, 250f));
            DrawDeskEffectsCard(Board(453f, 298f, 250f, 314f));

            DrawDeskCalendarSheet(Board(716f, 36f, 440f, 420f));
            DrawDeskEventCard(Board(716f, 468f, 440f, 144f));

            // The strip's rule (1m-r2: the strip is part of the sheet - a rule at +8, padding 6,
            // hairline dividers between the cells, no plates).
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawDeskChipStrip(Board(0f, 627f, 1156f, 53f));

            if (_isGameOver)
            {
                DrawDeskGameOver(inner);
            }
        }

        /// <summary>
        /// The masthead (board 1m, D5): the flag and `{COUNTRY} · YEAR {N}` (C6) at the left; at the
        /// right the speed cluster and Saves (C28 - the pinned strip folds away, so its controls
        /// live here on Screen 0) with the LIVE caption before them (S4's second half, the one
        /// statement that these are desk readings). B5 holds: while time is held the non-Pause
        /// faces are disabled, rendered never omitted; Saves is enabled unconditionally, as on the
        /// OPEN strip (a game-over player is the one who most needs Load).
        /// </summary>
        private void DrawDeskMasthead(Rect r, bool isTimePaused)
        {
            float ux = r.width / DeskBoardInnerWidth;
            float uy = r.height / 28f;
            // 1m-r2: the flag 26×17 in the 28 masthead; the title mono 10.5 bold; LIVE 10; the
            // cluster's chips mono 9 with padding 3/8, centred in the masthead's height.
            float flagHeight = Mathf.Round(r.height * (17f / 28f));
            float flagWidth = Mathf.Round(flagHeight * 1.5f);
            var flagRect = new Rect(r.x, r.y + (r.height - flagHeight) * 0.5f, flagWidth, flagHeight);
            Texture2D flag = IconLibrary.GetFlag(PlayerCountryId);
            if (flag != null && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(flagRect, flag, ScaleMode.StretchToFill, true);
            }

            GUIStyle title = DeskCaption(10.5f, PoliSimTheme.TextPrimary, bold: true);
            string titleText = $"{_playerCountry.Name.ToUpperInvariant()} · YEAR {_simulationManager.CurrentTurn}";
            float titleWidth = title.CalcSize(new GUIContent(titleText)).x + 4f;

            // The cluster (board 1m-r2: mono 9 on bordered chips, the active one brass with
            // TextPrimary - D6's flip), measured from its own labels and laid out from the right
            // edge. Not the sprite button faces: at this size their 9-slice borders ate the label on
            // the first v3desk film.
            GUIStyle chipCaption = DeskCaption(9f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
            string[] labels = { "PAUSE", "1×", "2×", "3×" };
            GameSpeed[] speeds = { GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.VeryFast };
            const string savesLabel = "SAVES";
            float gap = Mathf.Round(4f * ux);
            float chipPad = Mathf.Round(8f * ux);
            float chipHeight = Mathf.Min(r.height, Mathf.Ceil(DeskCaptionHeight(chipCaption)) + Mathf.Round(6f * uy));
            float chipY = r.y + Mathf.Round((r.height - chipHeight) * 0.5f);
            float x = r.xMax;

            float savesWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(savesLabel)).x) + chipPad * 2f;
            x -= savesWidth;
            bool ambient = GUI.enabled;
            GUI.enabled = true;
            if (DrawDeskChipButton(new Rect(x, chipY, savesWidth, chipHeight), savesLabel, chipCaption, selected: false, disabled: false))
            {
                OpenSavesMenu();
            }
            GUI.enabled = ambient;

            for (int i = labels.Length - 1; i >= 0; i--)
            {
                float width = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(labels[i])).x) + chipPad * 2f;
                x -= gap + width;
                bool selected = _gameSpeed == speeds[i];
                bool disabled = (isTimePaused && speeds[i] != GameSpeed.Paused) || _isGameOver;
                if (DrawDeskChipButton(new Rect(x, chipY, width, chipHeight), labels[i], chipCaption, selected, disabled))
                {
                    _gameSpeed = speeds[i];
                }
            }

            GUIStyle live = DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            const string liveText = "DESK READINGS · LIVE";
            float liveWidth = live.CalcSize(new GUIContent(liveText)).x + 4f;
            float liveRight = x - Mathf.Round(14f * ux);
            float titleLeft = flagRect.xMax + Mathf.Round(10f * ux);
            float liveLeft = Mathf.Max(titleLeft + titleWidth + gap, liveRight - liveWidth);
            PoliSimWidgets.MeasuredLabel(new Rect(titleLeft, r.y, Mathf.Max(1f, Mathf.Min(titleWidth, liveLeft - gap - titleLeft)), r.height), titleText, title);
            PoliSimWidgets.MeasuredLabel(new Rect(liveLeft, r.y, Mathf.Max(1f, liveRight - liveLeft), r.height), liveText, live);
        }

        /// <summary>
        /// The board's chip control (the speed cluster, Saves, the horizon chips): a bordered plate
        /// with a mono caption - the stock-off plate under a hairline-strong border; selected = brass
        /// under the brass border with light text (the board's active face); disabled = the plate and
        /// the text muted (B5: rendered, never omitted), the click swallowed. The control is an
        /// invisible button over the whole rect, drawn on every event, so the count never varies with
        /// state. Returns true on the click. DrawSpeedButton's kinds, the same composition with the
        /// ambient GUI.enabled, so the two clusters cannot disagree about what a held clock looks like.
        /// </summary>
        private static bool DrawDeskChipButton(Rect rect, string label, GUIStyle caption, bool selected, bool disabled)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color fill = selected ? PoliSimTheme.Brass : disabled ? PoliSimTheme.Tint(PoliSimTheme.StockOff, 0.55f) : PoliSimTheme.StockOff;
                Color border = selected ? PoliSimTheme.BrassBorder : disabled ? PoliSimTheme.Tint(PoliSimTheme.HairlineStrong, 0.6f) : PoliSimTheme.HairlineStrong;
                // D6 (2026-08-28): the selected caption is TextPrimary on brass - an ASSIGNMENT flip, not a
                // value (light-on-brass read 3.2 : 1 at 8 px; measured after the flip 4.03; brass unchanged).
                Color ink = disabled ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary;
                PoliSimTheme.RoundedCard(rect, fill, border, 0f);
                PoliSimWidgets.MeasuredLabel(rect, label, Inked(new GUIStyle(caption), ink));
            }

            bool ambient = GUI.enabled;
            GUI.enabled = ambient && !disabled;
            bool clicked = PoliSimWidgets.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.enabled = ambient;
            return clicked;
        }

        /// <summary>A caption at the plate's corner, the board's own placement for every plate's label (1m-r2: 8.5).</summary>
        private void DrawDeskPlateCaption(Rect plate, string text, float ux, float uy)
        {
            GUIStyle caption = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float height = DeskCaptionHeight(caption);
            PoliSimWidgets.MeasuredLabel(new Rect(plate.x + Mathf.Round(8f * ux), plate.y + Mathf.Round(5f * uy), plate.width - Mathf.Round(16f * ux), height), text, caption);
        }

        /// <summary>The world map (Annex B I1) on its plate, read-only on the stage (R-B6: a click pins nothing here - the International document is where a readout lives); the renderer's own hover readout stays. Names at the board's 11 px, on §A.9a's ladder (R-SP5).</summary>
        private void DrawDeskMapPlate(Rect r)
        {
            float ux = r.width / 440f;
            float uy = r.height / 320f;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(r, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
            }

            GUIStyle names = DeskBody(11f, PoliSimTheme.TextPrimary);
            _mapRenderer.Draw(r, _world.Countries, PlayerCountryId, _mapEventMarkers, _simulationManager.CurrentTurn, EventMarkerFadeTurns, names, out _, out _);
            DrawDeskPlateCaption(r, "THE WORLD — TRADE VOLUME", ux, uy);
        }

        /// <summary>
        /// The approval ledger (board 1m, D6): no face exists (Annex B I3) and none is invented - the
        /// live approval as a hero numeral over the attribution panel's OWN terms (R-B7:
        /// StatTracePanel.BuildApprovalDeskTerms - the nine non-misery Class A terms, the four gaps
        /// as one misery total, the dated events as one total; the clamp row only when it is not
        /// zero), each through the read-only ledger lane with no gauge (fill −1: "there is no
        /// proportion here"). Rows that do not fit are stated, never trimmed quietly. Before the
        /// first period closes (board 1m-r2's Year-0 empty state) the nine rows draw with em-dash
        /// figures under a FIRST ATTRIBUTION chip naming the model's own first boundary - the shape
        /// of the ledger is on the sheet from day one, and no figure is invented for it.
        /// </summary>
        private void DrawDeskApprovalLedger(Rect r)
        {
            float ux = r.width / 440f;
            float uy = r.height / 244f;
            float y = r.y;

            GUIStyle header = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float headerHeight = DeskCaptionHeight(header);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, headerHeight), "APPROVAL — NINE-TERM ATTRIBUTION · LEDGER", header);
            y += headerHeight + Mathf.Round(3f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }
            y += Mathf.Round(4f * uy);

            List<StatTracePanel.DeskTerm> terms = StatTracePanel.BuildApprovalDeskTerms(_playerCountry);
            bool empty = terms == null || terms.Count == 0;

            GUIStyle hero = DeskNumeral(34f, PoliSimTheme.TextPrimary);
            string heroText = UiFormat.Number(_playerCountry.State.ApprovalRating, 1);
            Vector2 heroSize = hero.CalcSize(new GUIContent(heroText));
            var heroRect = new Rect(r.x, y, heroSize.x + 4f, heroSize.y);
            PoliSimWidgets.MeasuredLabel(heroRect, heroText, hero);
            GUIStyle heroCaption = DeskCaption(9f, PoliSimTheme.TextSecondary, false, TextAnchor.LowerLeft);
            float captionLeft = heroRect.xMax + Mathf.Round(10f * ux);
            float captionRight = r.xMax;

            if (empty)
            {
                // The promise chip (1m-r2): the date the first period closes, in the caution ink
                // and border - the model's boundary, never the board's placeholder.
                GUIStyle chipStyle = DeskCaption(8.5f, PoliSimTheme.Caution, bold: true, anchor: TextAnchor.MiddleCenter);
                string chipText = $"FIRST ATTRIBUTION — {DeskFirstAttributionDate().ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()}";
                float chipPadX = Mathf.Round(6f * ux);
                float chipWidth = Mathf.Ceil(chipStyle.CalcSize(new GUIContent(chipText)).x) + chipPadX * 2f;
                float chipHeight = Mathf.Ceil(DeskCaptionHeight(chipStyle)) + Mathf.Round(4f * uy);
                var chipRect = new Rect(r.xMax - chipWidth, y + Mathf.Round((heroSize.y - chipHeight) * 0.5f), chipWidth, chipHeight);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.RoundedCard(chipRect, PoliSimTheme.Tile, PoliSimTheme.Caution, 0f);
                    PoliSimWidgets.MeasuredLabel(chipRect, chipText, chipStyle);
                }
                captionRight = chipRect.x - Mathf.Round(8f * ux);
            }

            PoliSimWidgets.MeasuredLabel(new Rect(captionLeft, y, Mathf.Max(1f, captionRight - captionLeft), heroSize.y - Mathf.Round(4f * uy)), "APPROVAL RATING · LIVE", heroCaption);
            y += heroSize.y + Mathf.Round(2f * uy);

            // The board's rows: the term's name at 14 px, its signed figure in mono 12 at the right,
            // a 17 px pitch - two measured labels on one rect, not the gauge lane (whose bar slot and
            // name column made a 40 px row and wrapped "Reversion toward 50" on the first v3desk film).
            GUIStyle nameStyle = DeskBody(14f, empty ? PoliSimTheme.TextSecondary : PoliSimTheme.TextPrimary);
            GUIStyle figureStyle = DeskCaption(12f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);
            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - y) / rowHeight));

            if (empty)
            {
                string[] names = StatTracePanel.ApprovalDeskTermNames;
                int shownNames = Mathf.Min(names.Length, room);
                for (int i = 0; i < shownNames; i++)
                {
                    DrawDeskTermRow(new Rect(r.x, y, r.width, rowHeight), names[i], null, nameStyle, figureStyle);
                    y += rowHeight;
                }
                return;
            }

            int shown = terms.Count <= room ? terms.Count : Mathf.Max(0, room - 1);
            for (int i = 0; i < shown; i++)
            {
                DrawDeskTermRow(new Rect(r.x, y, r.width, rowHeight), terms[i].Name, terms[i].Value, nameStyle, figureStyle);
                y += rowHeight;
            }

            if (shown < terms.Count && room > 0)
            {
                float rest = 0f;
                for (int i = shown; i < terms.Count; i++) { rest += terms[i].Value; }
                DrawDeskTermRow(new Rect(r.x, y, r.width, rowHeight), $"+{terms.Count - shown} more terms", rest, nameStyle, figureStyle);
            }
        }

        /// <summary>One ledger row; a null value is the Year-0 em dash in the muted ink (1m-r2), never a zero.</summary>
        private static void DrawDeskTermRow(Rect rect, string name, float? value, GUIStyle nameStyle, GUIStyle figureStyle)
        {
            float figureWidth = Mathf.Ceil(figureStyle.CalcSize(new GUIContent("+00.00")).x) + 6f;
            PoliSimWidgets.MeasuredLabel(new Rect(rect.x, rect.y, Mathf.Max(1f, rect.width - figureWidth - 8f), rect.height), name, nameStyle);
            GUIStyle figure = Inked(new GUIStyle(figureStyle), value.HasValue ? UiPalette.GetDeltaColor(value.Value, higherIsBetter: true) : PoliSimTheme.TextMuted);
            string text = value.HasValue ? value.Value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) : "—";
            PoliSimWidgets.MeasuredLabel(new Rect(rect.xMax - figureWidth, rect.y, figureWidth, rect.height), text, figure);
        }

        /// <summary>
        /// The compass (Annex B I2) in the board's 240 square: the renderer's honest footprint
        /// (R-SP4) is the plot square plus its caption band, so the plot here is the square less the
        /// band the captions need at this width - the captions inside the declared rect, which is the
        /// board's D3 ("axis captions drawn INSIDE its rect") as the renderer already draws it.
        /// </summary>
        private void DrawDeskCompass(Rect box)
        {
            float ux = box.width / 250f;
            float uy = box.height / 250f;
            GUIStyle style = DeskBody(9f, PoliSimTheme.TextPrimary);
            Vector2 probe = _politicalCompassRenderer.Footprint(_world.Countries, box.width, box.width, style, PlayerCountryId, withLegend: false);
            float band = Mathf.Max(0f, probe.y - box.width);
            float plot = Mathf.Max(1f, box.height - band);
            Vector2 footprint = _politicalCompassRenderer.Footprint(_world.Countries, plot, box.width, style, PlayerCountryId, withLegend: false);
            // The full box width, not the footprint's: the renderer paints its own paper over the rect
            // it is given, and a narrower rect left a strip of the plate showing at the right (the
            // first matrix at 1600/1920); the plot side is bounded by the height either way.
            var rect = new Rect(box.x, box.y, box.width, Mathf.Min(box.height, footprint.y));

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(box, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
            }

            _politicalCompassRenderer.Draw(rect, _world.Countries, PlayerCountryId, style, withLegend: false);
            DrawDeskPlateCaption(box, "POLITICAL COMPASS · SIX STATES", ux, uy);
        }

        /// <summary>
        /// The effects card: C16's label (P2-2.1 retired C17's horizon control - the chip now names the scope, next year;
        /// the board's 1D / 1W / 1M were four rescaled copies of one projection), C22's eight figures each as a diverging
        /// bar with its numeral (the bar fills from the centre toward the sign, in the GOOD/BAD ink
        /// GetDeltaColor keys - neutral valence, the shape says nothing the ink does not), C19's
        /// margin and C20's methodology as mono captions (D4). The board named two figures the
        /// preview does not estimate (debt-to-GDP, currency); the census is the content list, so the
        /// preview's own eight draw (R-B4). The estimate is the same cached PreviewTurn the Budget
        /// column's arrows show, the full-turn point.
        /// </summary>
        private void DrawDeskEffectsCard(Rect r)
        {
            if (PolicyInputsChangedSinceLastPreview())
            {
                RecomputePolicyPreview();
            }

            float ux = r.width / 250f;
            float uy = r.height / 314f;
            float y = r.y;

            GUIStyle header = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float headerHeight = Mathf.Max(DeskCaptionHeight(header), Mathf.Round(16f * uy));

            GUIStyle chipCaption = DeskCaption(7.5f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
            // P2-2.1 (2026-09-02): the horizon chips (1D / 1W / 1M / 1Y - four rescaled copies of one projection) are
            // retired with the horizons; the card shows the point the preview produced for next year, and the chip
            // says so. The rows below read the same full-turn figures the Statistics projections read.
            const string scopeChip = "NEXT YEAR";
            float chipPad = Mathf.Round(4f * ux);
            float x = r.xMax;
            float scopeWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(scopeChip)).x) + chipPad * 2f;
            x -= scopeWidth;
            DrawDeskChipButton(new Rect(x, y, scopeWidth, headerHeight), scopeChip, chipCaption, selected: true, disabled: true);

            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, Mathf.Max(1f, x - Mathf.Round(6f * ux) - r.x), headerHeight), "ESTIMATED EFFECTS", header);
            y += headerHeight + Mathf.Round(3f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }
            y += Mathf.Round(4f * uy);

            // The numerals from the same cached scaled figures the OPEN panel prints, formatted here
            // WITHOUT the per-row "(±…)" margin: the board states the margin once (C19, below), and
            // the margin texts overflowed the value column at the floor on the first v3desk film.
            // Invariant culture, as the tiles print.
            float gdp = Mathf.Max(1f, _playerCountry.State.GDP);
            string Signed(float v) => v.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
            var rows = new List<(string label, float value, string text, bool higherIsBetter, float range)>
            {
                ("GDP growth", _cachedGdpGrowthPercentRaw, Signed(_cachedGdpGrowthPercentRaw) + "%", true, DeskRangeGdpGrowthPercent),
                ("Inflation", _cachedInflationChangeRaw, Signed(_cachedInflationChangeRaw) + " pts", false, DeskRangeInflationPoints),
                ("Unemployment", _cachedUnemploymentChangeRaw, Signed(_cachedUnemploymentChangeRaw) + " pts", false, DeskRangeUnemploymentPoints),
                ("Approval", _cachedApprovalChangeRaw, Signed(_cachedApprovalChangeRaw), true, DeskRangeApproval),
                ("Poverty rate", _cachedPovertyRateChangeRaw, Signed(_cachedPovertyRateChangeRaw) + " pts", false, DeskRangePovertyPoints),
                ("Labor force participation", _cachedLaborForceParticipationRateChangeRaw, Signed(_cachedLaborForceParticipationRateChangeRaw) + " pts", true, DeskRangeParticipationPoints),
                ("Crime index", _cachedCrimeIndexChangeRaw, Signed(_cachedCrimeIndexChangeRaw), false, DeskRangeCrimeIndex),
                ("Net budget", _cachedNetBudgetImpactRaw, UiFormat.MoneyDelta(_cachedNetBudgetImpactRaw, MoneyUnit.Billions), true, DeskRangeNetBudgetShareOfGdp * gdp)
            };

            // 1m-r2: rows 26 tall, the label at 12, the bar 66×9, the value mono 10.5 (a zero reads in
            // the neutral ink - "zero is a reading, not an absence"; the bare centre-lined track IS
            // the zero row's face).
            float rowHeight = Mathf.Round(26f * uy);
            float barWidth = Mathf.Round(66f * ux);
            float barHeight = Mathf.Max(4f, Mathf.Round(9f * uy));
            GUIStyle label = DeskBody(12f, PoliSimTheme.TextPrimary);
            float valueWidth = Mathf.Round(58f * ux);
            float labelWidth = Mathf.Max(1f, r.width - barWidth - valueWidth - Mathf.Round(12f * ux));
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowRect = new Rect(r.x, y, r.width, rowHeight);
                PoliSimWidgets.MeasuredLabel(new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight), row.label, label);
                var bar = new Rect(rowRect.x + labelWidth + Mathf.Round(6f * ux), rowRect.y + (rowHeight - barHeight) * 0.5f, barWidth, barHeight);
                DrawDeskDivergingBar(bar, row.value, row.range, row.higherIsBetter);
                GUIStyle value = DeskCaption(10.5f, UiPalette.GetDeltaColor(row.value, row.higherIsBetter), false, TextAnchor.MiddleRight);
                PoliSimWidgets.MeasuredLabel(new Rect(bar.xMax + Mathf.Round(6f * ux), rowRect.y, Mathf.Max(1f, rowRect.xMax - bar.xMax - Mathf.Round(6f * ux)), rowHeight), row.text, value);
                y += rowHeight;
            }

            y += Mathf.Round(6f * uy);
            if (!DeskHasDraftPending())
            {
                // 1m-r2's empty state: while nothing is drafted the two footer captions become one
                // caption in a dashed frame. Its claim is aligned to the model, not copied from the
                // board: the preview reads the rate dial as drafted (the one input it still reads)
                // and every bill as it passes - a drafted bill does not move it (R-B4's discipline).
                GUIStyle note = DeskCaptionWrapped(8f, PoliSimTheme.TextMuted);
                // C-C14 (2026-08-31): "±5–10% MARGIN" is gone from this caption. It was a rolled number,
                // and the scope it sat beside is the part that was doing the work.
                const string noteText = "NO DRAFT PENDING — ESTIMATES FOLLOW THE RATE DIAL AS DRAFTED AND EVERY BILL AS IT PASSES · NO MARGIN: THE PROJECTION IS DETERMINISTIC · SCALED DISPLAY ESTIMATE, NOT A SIMULATED SUB-YEAR VALUE";
                float notePadX = Mathf.Round(7f * ux);
                float notePadY = Mathf.Round(5f * uy);
                float noteWidth = Mathf.Max(1f, r.width - notePadX * 2f);
                float noteHeight = note.CalcHeight(new GUIContent(noteText), noteWidth);
                float boxHeight = Mathf.Min(Mathf.Max(1f, r.yMax - y), noteHeight + notePadY * 2f);
                DeskDashedFrame(new Rect(r.x, y, r.width, boxHeight), PoliSimTheme.HairlineStrong, 4f, 3f);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Desk effects no-draft caption", new Rect(r.x + notePadX, y + notePadY, noteWidth, noteHeight), r);
                }
                GUI.Label(new Rect(r.x + notePadX, y + notePadY, noteWidth, Mathf.Max(1f, boxHeight - notePadY * 2f)), noteText, note);
                return;
            }

            // C-C14: the margin line becomes the scope line. The board's slot stays - a reader who has
            // learned to look here for "how much should I trust this" still finds an answer, and now it
            // is a true one. Same style, same height, same position, so no layout below it moves.
            GUIStyle margin = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float marginHeight = DeskCaptionHeight(margin);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, marginHeight), "NO MARGIN — THE PROJECTION IS DETERMINISTIC", margin);
            y += marginHeight + Mathf.Round(2f * uy);

            GUIStyle method = DeskCaptionWrapped(8f, PoliSimTheme.TextMuted);
            string methodText = $"SCALED DISPLAY ESTIMATE — FROM THE {SimulationManager.DaysPerTurn}-DAY PROJECTION, NOT A SIMULATED SUB-YEAR VALUE";
            float methodHeight = Mathf.Min(Mathf.Max(1f, r.yMax - y), method.CalcHeight(new GUIContent(methodText), r.width));
            if (Event.current.type == EventType.Repaint)
            {
                // Wrapped by design (two lines on the board); the overflow guard measures the
                // one-line form, so this caption is checked against its own wrapped height instead.
                UiContainmentGuard.Check("Desk effects methodology", new Rect(r.x, y, r.width, method.CalcHeight(new GUIContent(methodText), r.width)), r);
            }
            GUI.Label(new Rect(r.x, y, r.width, methodHeight), methodText, method);
        }

        /// <summary>
        /// A diverging bar keyed to GOOD, not to up: the track, the centre tick, and the fill from
        /// the centre toward the value's sign in the ink GetDeltaColor gives the value - so a falling
        /// unemployment fills LEFT in the good ink, exactly as the board draws it. UiPalette's
        /// DrawDivergingBar keys its ink to the sign (built for a vote alignment, where the sign is
        /// the meaning) and would paint a good fall red here.
        /// </summary>
        private static void DrawDeskDivergingBar(Rect rect, float value, float range, bool higherIsBetter)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            PoliSimTheme.Rule(rect, PoliSimTheme.BarTrack);
            float centre = rect.x + rect.width * 0.5f;
            float fraction = range > 0f ? Mathf.Clamp(value / range, -1f, 1f) : 0f;
            float half = rect.width * 0.5f * Mathf.Abs(fraction);
            if (half > 0.5f)
            {
                Rect fill = fraction >= 0f
                    ? new Rect(centre, rect.y + 1f, half, rect.height - 2f)
                    : new Rect(centre - half, rect.y + 1f, half, rect.height - 2f);
                PoliSimTheme.Rule(fill, UiPalette.GetDeltaColor(value, higherIsBetter));
            }

            PoliSimTheme.Rule(new Rect(Mathf.Round(centre) - 0.5f, rect.y, 1f, rect.height), PoliSimTheme.HairlineStrong);
        }

        /// <summary>
        /// The 1k calendar sheet in the third column (Annex B I5): the month page as built
        /// (DrawCalendarMonthGrid - the section rule, C7's month, C8's weekday row, C9's cells) inside
        /// a GUILayout island, then the dated ledger's rows drawn on rects beneath it on Repaint,
        /// where the grid's own rect is known: C10's "This Month" and C13's empty sentence are dropped
        /// on this surface (the 1k precedent, board 1m); rows that do not fit are stated as "+N more".
        /// </summary>
        private void DrawDeskCalendarSheet(Rect r)
        {
            System.DateTime today = _simulationManager.CurrentDate;
            var monthStart = new System.DateTime(today.Year, today.Month, 1);
            Dictionary<int, List<CalendarMarker>> markers = BuildCalendarMonthMarkers(monthStart, today);

            GUILayout.BeginArea(r);
            GUILayout.BeginVertical();
            DrawCalendarMonthGrid(monthStart, today, markers);
            GUILayout.EndVertical();
            Rect grid = GUILayoutUtility.GetLastRect();
            GUILayout.EndArea();

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float top = r.y + grid.yMax + 6f;
            PoliSimTheme.Rule(new Rect(r.x, top, r.width, 1.5f), PoliSimTheme.HairlineStrong);
            top += 1.5f + 5f;

            var days = new List<int>(markers.Keys);
            days.Sort();
            var rows = new List<CalendarMarker>();
            var rowDays = new List<int>();
            foreach (int day in days)
            {
                foreach (CalendarMarker marker in markers[day])
                {
                    rows.Add(marker);
                    rowDays.Add(day);
                }
            }

            if (rows.Count == 0)
            {
                return;
            }

            GUIStyle date = DeskCaption(10f, PoliSimTheme.TextSecondary);
            GUIStyle label = DeskBody(12.5f, PoliSimTheme.TextPrimary);
            // The board's ledger pitch (1m-r2: 22 on the 420 sheet; date 10, label 12.5), never the
            // gauge lane's height (which fit one row under the grid on the second v3desk film).
            float rowHeight = Mathf.Max(Mathf.Round(22f * (r.height / 420f)), label.CalcSize(new GUIContent("Ag")).y);
            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - top) / rowHeight));
            int shown = rows.Count <= room ? rows.Count : Mathf.Max(0, room - 1);
            float dateWidth = date.CalcSize(new GUIContent("12/31")).x + 4f;
            float ux = r.width / 440f;
            for (int i = 0; i < shown; i++)
            {
                var row = new Rect(r.x, top, r.width, rowHeight);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, dateWidth, rowHeight), $"{monthStart.Month}/{rowDays[i]}", date);
                float dotX = row.x + dateWidth + Mathf.Round(7f * ux);
                PoliSimTheme.Pill(new Rect(dotX, row.y + (rowHeight - CalendarDotSize) * 0.5f, CalendarDotSize, CalendarDotSize), UiPalette.GetAreaColor(rows[i].Area));
                float textX = dotX + CalendarDotSize + Mathf.Round(7f * ux);
                PoliSimWidgets.MeasuredLabel(new Rect(textX, row.y, Mathf.Max(1f, row.xMax - textX), rowHeight), rows[i].Label, Inked(new GUIStyle(label), UiPalette.GetAreaColor(rows[i].Area)));
                UiContainmentGuard.Check("Desk calendar row", row, r);
                top += rowHeight;
            }

            if (shown < rows.Count && room > 0)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, top, r.width, rowHeight), $"+{rows.Count - shown} more this month", Inked(new GUIStyle(label), PoliSimTheme.TextMuted));
            }
        }

        /// <summary>
        /// The event card (C1/C2/C3) - the card draws only while an event is live; the empty state
        /// is the reservation, DRAWN since board 1m-r2 (a dashed frame with its two captions, see
        /// DrawDeskEventReservation). The BREAKING chip is §A.11's urgency chip (procedural, 1.5 px,
        /// −2°) in the caution ink; the name and the event's description (its only text) are
        /// captions; the three effects return as instruments (C3's (b) resolved): the shock's GDP,
        /// inflation and approval figures as diverging bars in the good/bad ink.
        /// </summary>
        private void DrawDeskEventCard(Rect r)
        {
            EconomicEvent activeEvent = _simulationManager.GetLastEvent(PlayerCountryId);
            float ux = r.width / 440f;
            float uy = r.height / 144f;
            float padX = Mathf.Round(11f * ux);
            float padY = Mathf.Round(9f * uy);
            if (activeEvent == null)
            {
                DrawDeskEventReservation(r, padX, padY, uy);
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(r, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
            }

            float y = r.y + padY;

            GUIStyle chipStyle = DeskCaption(9f, PoliSimTheme.Caution, bold: true, anchor: TextAnchor.MiddleCenter);
            const string chipText = "BREAKING";
            Vector2 chipSize = PoliSimWidgets.StampSize(chipText, chipStyle, Mathf.Round(7f * ux), Mathf.Round(2f * uy), 1.5f);
            var chipRect = new Rect(r.x + padX, y, chipSize.x, chipSize.y);
            PoliSimWidgets.Stamp(chipRect, chipText, chipStyle, PoliSimTheme.Caution, PoliSimTheme.Caution, 1.5f, -2f);

            GUIStyle name = DeskCaption(9.5f, PoliSimTheme.TextPrimary, bold: true);
            float nameX = chipRect.xMax + Mathf.Round(9f * ux);
            PoliSimWidgets.MeasuredLabel(new Rect(nameX, y, Mathf.Max(1f, r.xMax - padX - nameX), chipSize.y), activeEvent.Name.ToUpperInvariant(), name);
            y += chipSize.y + Mathf.Round(5f * uy);

            GUIStyle description = DeskCaptionWrapped(8.5f, PoliSimTheme.TextSecondary);
            string descriptionText = activeEvent.Description.ToUpperInvariant();
            float descriptionWidth = r.width - padX * 2f;
            float descriptionHeight = description.CalcHeight(new GUIContent(descriptionText), descriptionWidth);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Desk event description", new Rect(r.x + padX, y, descriptionWidth, descriptionHeight), r);
            }
            GUI.Label(new Rect(r.x + padX, y, descriptionWidth, descriptionHeight), descriptionText, description);
            y += descriptionHeight + Mathf.Round(7f * uy);

            var bars = new List<(string label, float value, bool higherIsBetter, float range)>
            {
                ("GDP", activeEvent.GdpShockPercent, true, DeskRangeEventGdpPercent),
                ("INFL", activeEvent.InflationShockPoints, false, DeskRangeEventInflationPoints),
                ("APPR", activeEvent.ApprovalEffect, true, DeskRangeEventApproval)
            };
            GUIStyle barLabel = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float barWidth = Mathf.Round(44f * ux);
            float barHeight = Mathf.Max(4f, Mathf.Round(8f * uy));
            float rowHeight = Mathf.Max(barHeight, DeskCaptionHeight(barLabel));
            float x = r.x + padX;
            for (int i = 0; i < bars.Count; i++)
            {
                float labelWidth = barLabel.CalcSize(new GUIContent(bars[i].label)).x + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, y, labelWidth, rowHeight), bars[i].label, barLabel);
                x += labelWidth + Mathf.Round(5f * ux);
                var bar = new Rect(x, y + (rowHeight - barHeight) * 0.5f, barWidth, barHeight);
                DrawDeskDivergingBar(bar, bars[i].value, bars[i].range, bars[i].higherIsBetter);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Desk event bar", bar, r);
                }
                x += barWidth + Mathf.Round(14f * ux);
            }
        }

        /// <summary>
        /// The chip strip (board 1m's split, D1): the ten headline readings Statistics › Domestic
        /// tiles (S6-S9, the same list DrawHeadlineStatTiles builds - one source), restated on the
        /// stage as chips: the label caption, the numeral with its unit, the GDP delta and the
        /// credit outlook where the tile shows them, and a sparkline through the one renderer the
        /// graphs use (R-G4's weight) for every reading that keeps a history - the four that keep
        /// none (currency, the debt stock, the rating, the balance) draw no line rather than an
        /// invented one. Neutral ink on the lines (D7). No area keyline: the tiles one cell away
        /// carry B9's key (R-B5).
        /// </summary>
        private void DrawDeskChipStrip(Rect r) => DrawChipStrip(r, BuildHeadlineReadings());

        /// <summary>P2-1.4 (2026-09-02): the chip strip as an idiom - the desk's headline readings and the Budget's fiscal
        /// header draw through the same code, so the two cannot drift in type, pitch or the sparkline's place.</summary>
        private void DrawChipStrip(Rect r, List<HeadlineReading> chips)
        {
            // ONE list with the Statistics plates (BuildHeadlineReadings, board 2a) - the strip and
            // the sheet one cell away can never disagree about the tenth reading.

            // 1m-r2: the strip is part of the sheet - no plates, a hairline divider between
            // neighbours, padding 6; caption 7.5 in the muted ink, numeral 17 bold, sparkline 46×10 -
            // and a chip without a kept history draws a dotted baseline ending in today's dot in the
            // sparkline's slot (the line will start here), never a flat line that would imply a trend.
            float ux = r.width / DeskBoardInnerWidth;
            float uy = r.height / 53f;
            float width = r.width / chips.Count;
            float padX = Mathf.Round(6f * ux);
            float padY = Mathf.Round(4f * uy);
            GUIStyle caption = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle numeral = DeskNumeral(17f, PoliSimTheme.TextPrimary);
            float captionHeight = DeskCaptionHeight(caption);
            float sparkWidth = Mathf.Round(46f * ux);
            float sparkHeight = Mathf.Max(4f, Mathf.Round(10f * uy));

            for (int i = 0; i < chips.Count; i++)
            {
                HeadlineReading chip = chips[i];
                var plate = new Rect(r.x + i * width, r.y, width, r.height);
                if (i > 0 && Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.Rule(new Rect(Mathf.Round(plate.x), plate.y + padY, 1f, Mathf.Max(1f, plate.height - padY * 2f)), PoliSimTheme.Hairline);
                }

                var inner = new Rect(plate.x + padX, plate.y + padY, plate.width - padX * 2f, plate.height - padY * 2f);
                PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y, inner.width, captionHeight), chip.Label.ToUpperInvariant(), caption);

                bool hasSeries = chip.Series != null && chip.Series.Count >= 2;
                float sparkX = inner.xMax - sparkWidth;
                if (Event.current.type == EventType.Repaint)
                {
                    var spark = new Rect(sparkX, inner.yMax - sparkHeight, sparkWidth, sparkHeight);
                    if (hasSeries)
                    {
                        GraphRenderer.DrawSparkline(spark, chip.Series, PoliSimTheme.TextSecondary);
                    }
                    else
                    {
                        DeskDottedBaseline(spark);
                    }
                    UiContainmentGuard.Check("Desk chip sparkline", spark, plate);
                }

                float numeralRight = sparkX - Mathf.Round(4f * ux);
                // The delta's rect is the DELTA style's own measured height (bold 7 → 9 px at 1600 stands
                // taller than the 6.5 → 8 px caption whose height it borrowed on the first matrix: "0%"
                // needs 11.2 tall in 10.0).
                GUIStyle delta = string.IsNullOrEmpty(chip.Delta) ? null : DeskCaption(7f, UiPalette.GetDeltaColor(chip.DeltaIsGood ? 1f : -1f, higherIsBetter: true), bold: true);
                float deltaHeight = delta == null ? 0f : DeskCaptionHeight(delta);
                var numeralRect = new Rect(inner.x, inner.y + captionHeight, Mathf.Max(1f, numeralRight - inner.x), Mathf.Max(1f, inner.yMax - deltaHeight - inner.y - captionHeight));
                PoliSimWidgets.MeasuredLabel(numeralRect, chip.Value, numeral);

                if (delta != null)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.yMax - deltaHeight, Mathf.Max(1f, numeralRight - inner.x), deltaHeight), chip.Delta, delta);
                }
            }
        }

        /// <summary>The tile grid's own rule for the currency reading, shared so the strip and the tiles can never disagree about the tenth chip.</summary>
        private bool PlayerHasIndependentCurrency()
        {
            return !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
        }

        // ------------------------------------------------------------------------------------------
        // Board 1m-r2's empty-state furniture (2026-08-28): the dashed frame, the dotted baseline, and
        // the two model facts the states read - whether a draft is pending, and when the ledger's
        // first period closes.
        // ------------------------------------------------------------------------------------------

        /// <summary>A dashed 1 px frame from rule 10's primitives (no sprite ships one): dash / gap along all four edges. Repaint-gated.</summary>
        private static void DeskDashedFrame(Rect r, Color ink, float dash, float gap)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float step = dash + gap;
            for (float x = r.x; x < r.xMax; x += step)
            {
                float w = Mathf.Min(dash, r.xMax - x);
                PoliSimTheme.Rule(new Rect(x, r.y, w, 1f), ink);
                PoliSimTheme.Rule(new Rect(x, r.yMax - 1f, w, 1f), ink);
            }

            for (float y = r.y; y < r.yMax; y += step)
            {
                float h = Mathf.Min(dash, r.yMax - y);
                PoliSimTheme.Rule(new Rect(r.x, y, 1f, h), ink);
                PoliSimTheme.Rule(new Rect(r.xMax - 1f, y, 1f, h), ink);
            }
        }

        /// <summary>1m-r2: the sparkline slot of a chip with no kept history - a dotted baseline (1 px dots on a 4 px pitch, hairline-strong) ending in today's solid dot (TextPrimary). Repaint-gated by its caller.</summary>
        private static void DeskDottedBaseline(Rect spark)
        {
            float y = Mathf.Round(spark.y + spark.height * 0.5f);
            const float dot = 4f;
            for (float x = spark.x; x < spark.xMax - dot - 1f; x += 4f)
            {
                PoliSimTheme.Rule(new Rect(x, y, 1f, 1f), PoliSimTheme.HairlineStrong);
            }

            PoliSimTheme.Pill(new Rect(spark.xMax - dot, y - dot * 0.5f + 0.5f, dot, dot), PoliSimTheme.TextPrimary);
        }

        /// <summary>1m-r2's empty state for the event card: the reservation DRAWN - a dashed frame, its purpose as one caption at the corner, the quiet as two centred lines (the board's "YEAR 0 OPENS QUIET" at turn 0; the same sentence at any later turn names that turn). Nothing in it is a figure.</summary>
        private void DrawDeskEventReservation(Rect r, float padX, float padY, float uy)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DeskDashedFrame(r, PoliSimTheme.HairlineStrong, 5f, 4f);
            GUIStyle purpose = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float purposeHeight = DeskCaptionHeight(purpose);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + padX, r.y + padY, Mathf.Max(1f, r.width - padX * 2f), purposeHeight), "EVENT CARD — DRAWS ONLY WHILE AN EVENT IS LIVE", purpose);

            GUIStyle quiet = DeskCaption(8f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float lineHeight = DeskCaptionHeight(quiet);
            float lineGap = Mathf.Round(2f * uy);
            int turn = _simulationManager.CurrentTurn;
            string opening = turn == 0 ? "YEAR 0 OPENS QUIET" : $"YEAR {turn} IS QUIET";
            float top = r.y + padY + purposeHeight;
            float band = r.yMax - padY - top;
            float firstY = top + Mathf.Max(0f, (band - lineHeight * 2f - lineGap) * 0.5f);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + padX, firstY, Mathf.Max(1f, r.width - padX * 2f), lineHeight), opening + " — THE RESERVATION HOLDS ITS GROUND", quiet);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + padX, firstY + lineHeight + lineGap, Mathf.Max(1f, r.width - padX * 2f), lineHeight), "AND THE CALENDAR ABOVE SAYS WHAT IS COMING INSTEAD", quiet);
        }

        /// <summary>Whether the preview has a draft to read: the interest-rate change is the one input BuildPlayerDecision still carries (PolicyInputsChangedSinceLastPreview's own note) - bills reach the estimate only as they pass.</summary>
        private bool DeskHasDraftPending()
        {
            return !Mathf.Approximately(_interestRateChangeInput, 0f);
        }

        /// <summary>
        /// When the approval ledger's first period closes: the current turn's boundary, on the same
        /// arithmetic the calendar's election and event markers use (EpochDate + turns × DaysPerTurn
        /// - ApprovalLedgerRecorder.CloseAtBoundary runs inside AdvanceTurn). The board's "JAN 31" is
        /// a placeholder; a year-long turn puts the first attribution a year out, and the chip says so.
        /// </summary>
        private System.DateTime DeskFirstAttributionDate()
        {
            return SimulationManager.EpochDate.AddDays((_simulationManager.CurrentTurn + 1) * (double)SimulationManager.DaysPerTurn);
        }

        /// <summary>
        /// Game over on the stage (C4/C5, board 1m): §A.11's stamp treatment over the dimmed stage -
        /// the procedural stamp at verdict weight (2.5 px, −2°) in the bad ink on a plate, the reason
        /// as the one caption beneath it. No new sprite. The stage beneath stays legible (the read-
        /// only instruments were never gated) while every control is disabled by the frame.
        /// </summary>
        private void DrawDeskGameOver(Rect stage)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            PoliSimTheme.Rule(stage, PoliSimTheme.Tint(PoliSimTheme.Desk, 0.45f));

            float ux = stage.width / DeskBoardInnerWidth;
            float uy = stage.height / DeskBoardInnerHeight;
            GUIStyle stampStyle = DeskNumeral(17f, PoliSimTheme.Bad, TextAnchor.MiddleCenter);
            const string stampText = "GAME OVER";
            Vector2 stampSize = PoliSimWidgets.StampSize(stampText, stampStyle, Mathf.Round(16f * ux), Mathf.Round(4f * uy), 2.5f);

            GUIStyle reason = DeskCaptionWrapped(8.5f, PoliSimTheme.TextSecondary);
            reason.alignment = TextAnchor.UpperCenter;
            string reasonText = (_gameOverReason ?? string.Empty).ToUpperInvariant();
            float plateWidth = Mathf.Min(stage.width, Mathf.Max(stampSize.x + Mathf.Round(48f * ux), Mathf.Round(360f * ux)));
            float reasonWidth = plateWidth - Mathf.Round(24f * ux);
            float reasonHeight = string.IsNullOrEmpty(reasonText) ? 0f : reason.CalcHeight(new GUIContent(reasonText), reasonWidth);
            float plateHeight = Mathf.Round(12f * uy) * 2f + stampSize.y + (reasonHeight > 0f ? Mathf.Round(6f * uy) + reasonHeight : 0f);
            var plate = new Rect(stage.center.x - plateWidth * 0.5f, stage.center.y - plateHeight * 0.5f, plateWidth, plateHeight);
            PoliSimTheme.RoundedCard(plate, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);

            var stampRect = new Rect(plate.center.x - stampSize.x * 0.5f, plate.y + Mathf.Round(12f * uy), stampSize.x, stampSize.y);
            PoliSimWidgets.Stamp(stampRect, stampText, stampStyle, PoliSimTheme.Bad, PoliSimTheme.Bad, 2.5f, -2f);
            if (reasonHeight > 0f)
            {
                var reasonRect = new Rect(plate.x + Mathf.Round(12f * ux), stampRect.yMax + Mathf.Round(6f * uy), reasonWidth, reasonHeight);
                UiContainmentGuard.Check("Desk game-over reason", reasonRect, plate);
                GUI.Label(reasonRect, reasonText, reason);
            }
        }
    }
}
