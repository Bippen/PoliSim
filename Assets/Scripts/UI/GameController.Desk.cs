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
            return Mathf.Max(DeskCaptionFloorPx, Mathf.RoundToInt(boardPx * UiScreen.Height / DeskBoardHeight));
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

        /// <summary>
        /// D16 §2 and §9.1 (RULED by Elias, 2026-09-09): the desk's `†` PROVENANCE tab - <b>one control for the whole desk</b>, drawn in
        /// the plate's top-right on the People page, the Laws pages and the Riksbank page, its state persisted (<see cref="DeskProvenance"/>).
        /// The face is the board's: mono 12, inactive TextMuted on the stock face with the paper border and no bottom edge, active
        /// TextPrimary on the sheet with a brass inset at the top. It draws the page's title on the same line, so a caller replaces one
        /// header call with this one and nothing moves sideways.
        /// </summary>
        private void DrawPageHeaderWithProvenanceTab(string title, Color titleInk)
        {
            GUIStyle titleStyle = new GUIStyle(_headerStyle);
            GUIStyle glyph = DeskCaption(12f, DeskProvenance.On ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float tabW = glyph.CalcSize(new GUIContent(DeskProvenance.Glyph)).x + StatsUnit(20f);
            float rowH = Mathf.Max(Mathf.Ceil(titleStyle.CalcHeight(new GUIContent(title), 400f)), StatsUnit(22f));
            Rect row = GUILayoutUtility.GetRect(10f, rowH, GUILayout.ExpandWidth(true));
            var tab = new Rect(row.xMax - tabW, row.y + StatsUnit(2f), tabW, rowH - StatsUnit(2f));
            if (Event.current.type == EventType.Repaint)
            {
                Color before = GUI.contentColor;
                GUI.contentColor = titleInk;
                GUI.Label(new Rect(row.x, row.y, Mathf.Max(10f, row.width - tabW - StatsUnit(8f)), rowH), title, titleStyle);
                GUI.contentColor = before;
                PoliSimTheme.Rule(tab, DeskProvenance.On ? PoliSimTheme.Card : PoliSimTheme.StockOff);
                PoliSimTheme.Rule(new Rect(tab.x, tab.y, tab.width, 1f), PoliSimTheme.BorderPaper);
                PoliSimTheme.Rule(new Rect(tab.x, tab.y, 1f, tab.height), PoliSimTheme.BorderPaper);
                PoliSimTheme.Rule(new Rect(tab.xMax - 1f, tab.y, 1f, tab.height), PoliSimTheme.BorderPaper);
                if (DeskProvenance.On) { PoliSimTheme.Rule(new Rect(tab.x, tab.y, tab.width, 2f), PoliSimTheme.Brass); }
                PoliSimWidgets.MeasuredLabel(tab, DeskProvenance.Glyph, glyph);
            }
            if (PoliSimWidgets.Button(tab, GUIContent.none, GUIStyle.none)) { DeskProvenance.On = !DeskProvenance.On; }
        }

        /// <summary>Body type that wraps - D16 §3.5's gap-row reason, the one piece of prose on the People page, at reading size.</summary>
        private GUIStyle DeskBodyWrapped(float boardPx, Color ink)
        {
            GUIStyle style = DeskBody(boardPx, ink, TextAnchor.UpperLeft);
            style.wordWrap = true;
            return style;
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

            // Board 1m-r2's masthead and strip at the 1156×680 inner area; between them, since §726, the v3.5 page.
            BeginSlipAnchors();   // §685 (21e): the role chip's slip
            _deskSlipBook = new PeopleSlips.Book();
            DrawDeskMasthead(Board(0f, 0f, 1156f, 28f), isTimePaused);

            // §726 (UI v3.5, Design's V35): THE DESK AS A v3.5 PAGE - its title and the † control, then the cards on the twelve columns as the composition
            // lays them (World trade 7 | the calendar 5; Approval 7 | This month 5), scrolled, because at the 14 px floor they no longer fit one screen
            // (the nine-row ledger alone needs some 260 px of the 232 the board gave it). The composition draws no compass, no estimated effects and no
            // event card on the desk; the desk keeps all three (the effects card's content is §694's ruling) as a third and fourth row in the same
            // grammar, and asks Design where they go (BOARDS_BUILT). The masthead (17c's controls) and the strip stay where they were: the strip is
            // HELD as built - the composition's nine tiles clip at 1280 (§721 item 4, asked).
            float pageTop = Board(0f, 36f, 1f, 1f).y;
            float pageBottom = Board(0f, 612f, 1f, 1f).y;
            var titleRow = new Rect(inner.x, pageTop, inner.width, V35.Px(DeskTitleRow));
            DrawV35PageTitle(titleRow, "Desk");
            float viewTop = titleRow.yMax + V35.Px(6f);
            DrawDeskCards(new Rect(inner.x, viewTop, inner.width, Mathf.Max(1f, pageBottom - viewTop)));

            // The strip's rule (1m-r2: the strip is part of the sheet - a rule at +8, padding 6,
            // hairline dividers between the cells, no plates).
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawDeskChipStrip(Board(0f, 627f, 1156f, 53f));
            DrawSlips(_deskSlipBook, inner);

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
        /// OPEN strip (a game-over player is the one who most needs Load). Board 17c (2026-09-24) fixes
        /// the right end's order: the joined 1× 2× 3× strip, a pitch with a hairline, SAVES, SETTINGS outermost.
        /// </summary>
        /// <summary>
        /// PS-3a (§628), redrawn from board 21e (§685): **THE PLAYER'S ROLE AS A CHIP beside the year**, led by the party's mark - FILLED where the party
        /// holds office (GOVERNING, JUNIOR PARTNER, CARETAKER), OUTLINED where it does not (SUPPORTER FROM OUTSIDE, IN OPPOSITION): 17b's rule, the fill
        /// is the state, so a glance tells whether the player holds office. The run-on line it replaces ("· IN OPPOSITION · THE GOVERNMENT IS THE AI'S")
        /// is the chip's slip: the chip's own words, who governs, what the role cannot do - in the role gate's own words (<see cref="SimulationManager.PlayerMayIntroduce(CountryId, out string)"/>),
        /// never Design's placeholder "YOU MAY NOT DRAFT A BUDGET", which no gate says - and since when. Null where no government is stored.
        /// </summary>
        /// <summary>The desk's slips (the role chip's), rebuilt on every pass of the stage.</summary>
        private PeopleSlips.Book _deskSlipBook = new PeopleSlips.Book();

        private (string Words, bool InOffice)? RoleChip()
        {
            PoliSim.Elections.GovernmentRecord g = _playerCountry?.Government;
            if (g == null) { return null; }
            switch (g.RoleOf(_playerCountry.PlayerPartyAbbrev))
            {
                case PoliSim.Elections.PlayerRole.PrimeMinister: return (g.Caretaker ? "CARETAKER" : "GOVERNING", true);
                case PoliSim.Elections.PlayerRole.JuniorPartner: return ("JUNIOR PARTNER", true);
                case PoliSim.Elections.PlayerRole.Support: return ("SUPPORTER FROM OUTSIDE", false);
                case PoliSim.Elections.PlayerRole.Opposition: return ("IN OPPOSITION", false);
                default: return null;
            }
        }

        private float RoleChipWidth(string words) =>
            StatsUnit(6f) + StatsUnit(16f) + StatsUnit(6f) + Mathf.Ceil(RowChipCaption(PoliSimTheme.TextPrimary).CalcSize(new GUIContent(words)).x) + StatsUnit(8f);

        /// <summary>Draws the role chip in <paramref name="r"/> and hangs its slip on <see cref="_deskSlipBook"/>.</summary>
        private void DrawRoleChip(Rect r, string words, bool inOffice)
        {
            PoliSim.Elections.GovernmentRecord g = _playerCountry.Government;
            string you = _playerCountry.PlayerPartyAbbrev;
            DrawRowChip(r, string.Empty, inOffice ? ChipFace.Filled : ChipFace.Outline);
            float side = StatsUnit(16f);
            var markRect = new Rect(r.x + StatsUnit(6f), r.y + Mathf.Round((r.height - side) * 0.5f), side, side);
            if (inOffice && Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(markRect, PoliSimTheme.Card); }   // the mark's own ground on the dark fill
            DrawPartyMarkSlot(markRect, _playerCountry.Id, you);
            float wx = markRect.xMax + StatsUnit(6f);
            PoliSimWidgets.MeasuredLabel(new Rect(wx, r.y, Mathf.Max(1f, r.xMax - wx - StatsUnit(4f)), r.height), words,
                DeskCaption(9.5f, inOffice ? PoliSimTheme.Card : PoliSimTheme.TextPrimary, true, TextAnchor.MiddleLeft));

            var slip = new SlipContent(PartySystems.ShortName(_playerCountry.Id, you) + " " + words);
            switch (words)
            {
                case "GOVERNING": slip.Add("YOUR PARTY LEADS THE GOVERNMENT"); break;
                case "CARETAKER": slip.Add("YOUR GOVERNMENT SERVES ON AS A CARETAKER"); break;
                case "JUNIOR PARTNER": slip.Add("IN THE CABINET · YOUR PORTFOLIOS: " + g.PortfoliosOf(you)); break;
                case "SUPPORTER FROM OUTSIDE":
                {
                    slip.Add("THE GOVERNMENT IS THE AI'S");
                    PoliSim.Elections.SupportAgreement agreement = g.AgreementOf(you);   // PS-3h (§635): the agreement's tally
                    if (agreement != null) { slip.Add("AGREEMENT: " + agreement.Tally()); }
                    break;
                }
                default: slip.Add("THE GOVERNMENT IS THE AI'S"); break;
            }
            if (!_simulationManager.PlayerMayIntroduce(PlayerCountryId, out string locked) && !string.IsNullOrEmpty(locked))
            {
                int cut = locked.IndexOf(" · ", System.StringComparison.Ordinal);
                slip.Add(cut >= 0 ? locked.Substring(cut + 3) : locked);   // the gate's words after its role word, which the head already says
            }
            System.DateTime since = words == "CARETAKER" ? g.CaretakerSince : g.FormedOn;
            if (since > System.DateTime.MinValue) { slip.Add("SINCE " + DeskDay(since)); }
            SlipAnchor(r, "role");
            _deskSlipBook.Anchors["role"] = slip;
        }

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
            float titleTextWidth = title.CalcSize(new GUIContent(titleText)).x + 4f;
            // PS-3a (§628), board 21e (§685): the player's role as a chip beside the year, reserved with the title so the LIVE caption keeps its place.
            (string Words, bool InOffice)? role = RoleChip();
            float roleGap = StatsUnit(10f);
            float roleWidth = role.HasValue ? RoleChipWidth(role.Value.Words) : 0f;
            float titleWidth = titleTextWidth + (role.HasValue ? roleGap + roleWidth : 0f);

            // The cluster (board 1m-r2: mono 9 on bordered chips, the active one brass with
            // TextPrimary - D6's flip), measured from its own labels and laid out from the right
            // edge. Not the sprite button faces: at this size their 9-slice borders ate the label on
            // the first v3desk film.
            GUIStyle chipCaption = DeskCaption(9f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
            // §564 (2026-09-22): the three RUNNING speeds only. PAUSE is the rail's chip (R-E1, DrawRailPauseChip) - the desk had two pause controls (Design's sitting,
            // part A item 8), and the rail's is the one that also says PAUSED and RUN. With time paused no masthead chip is lit; the rail's chip is.
            string[] labels = { "1×", "2×", "3×" };
            GameSpeed[] speeds = { GameSpeed.Normal, GameSpeed.Fast, GameSpeed.VeryFast };
            const string savesLabel = "SAVES";
            float gap = Mathf.Round(4f * ux);
            float chipPad = Mathf.Round(8f * ux);
            float chipHeight = Mathf.Min(r.height, Mathf.Ceil(DeskCaptionHeight(chipCaption)) + Mathf.Round(6f * uy));
            float chipY = r.y + Mathf.Round((r.height - chipHeight) * 0.5f);
            float x = r.xMax;

            // BOARD 17c (Design, 2026-09-24): THE DESK'S WAY IN. Left to right: the clock's strip 1× 2× 3×
            // JOINED (no gap between its cells - one control), then a gap of one chip pitch carrying a
            // hairline in the plate rule ink (#B7A98C, PoliSimTheme.Hairline), then SAVES, then SETTINGS
            // outermost - SAVES next to the game's clock, SETTINGS the desk's own, in the corner. SETTINGS
            // stays its own chip, not folded under SAVES. Chip faces and sizes as built (no new chip, no
            // new face); the ≈ 25 px the divider adds at 1280 is taken from the masthead's empty middle
            // (the LIVE caption's right edge follows the strip). The settings sheet's foot reads BACK TO
            // THE DESK when opened from here (17b's rule, in the settings screen's own file).
            // ⚠ MM-2's order (SAVES outermost, SETTINGS inside it) is superseded by this board.
            const string settingsLabel = "SETTINGS";
            float savesWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(savesLabel)).x) + chipPad * 2f;
            float settingsWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(settingsLabel)).x) + chipPad * 2f;

            x -= settingsWidth;
            bool ambient = GUI.enabled;
            GUI.enabled = true;
            if (DrawDeskChipButton(new Rect(x, chipY, settingsWidth, chipHeight), settingsLabel, chipCaption, selected: false, disabled: false))
            {
                OpenSettings();
            }

            x -= gap + savesWidth;
            if (DrawDeskChipButton(new Rect(x, chipY, savesWidth, chipHeight), savesLabel, chipCaption, selected: false, disabled: false))
            {
                OpenSavesMenu();
            }
            GUI.enabled = ambient;

            // The divider: one chip pitch (a SAVES-sized chip's own width, measured, so it scales with the
            // caption) with the hairline standing at its centre, the chip's height tall.
            float pitch = savesWidth;
            x -= pitch;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(Mathf.Round(x + pitch * 0.5f), chipY, 1f, chipHeight), PoliSimTheme.Hairline);
            }

            // The joined strip: the cells share edges (no gap), laid out from the right, one control.
            for (int i = labels.Length - 1; i >= 0; i--)
            {
                float width = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(labels[i])).x) + chipPad * 2f;
                x -= width;
                bool selected = _gameSpeed == speeds[i];
                bool disabled = isTimePaused || _isGameOver;
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
            PoliSimWidgets.MeasuredLabel(new Rect(titleLeft, r.y, Mathf.Max(1f, Mathf.Min(titleTextWidth, liveLeft - gap - titleLeft)), r.height), titleText, title);
            if (role.HasValue)
            {
                float chipH = Mathf.Min(r.height, StatsUnit(20f));
                DrawRoleChip(new Rect(titleLeft + titleTextWidth + roleGap, r.y + Mathf.Round((r.height - chipH) * 0.5f), roleWidth, chipH), role.Value.Words, role.Value.InOffice);
            }
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

        // ---- §726: the v3.5 page's measures (px at 1280 x 699, the composition's) ----
        /// <summary>The page title's row.</summary>
        private const float DeskTitleRow = 40f;
        /// <summary>The first row (World trade | the calendar): the composition's map card.</summary>
        private const float DeskRowWorld = 330f;
        /// <summary>The second row (Approval | This month): the nine-term ledger under its head.</summary>
        private const float DeskRowLedger = 300f;
        /// <summary>The third row the composition does not draw (estimated effects | the compass): two panels of four arrows, the compass square.</summary>
        private const float DeskRowEffects = 340f;
        /// <summary>The fourth row the composition does not draw (the event card).</summary>
        private const float DeskRowEvent = 150f;

        /// <summary>§726: the desk's scroll.</summary>
        private Vector2 _deskScrollPosition;
        /// <summary>§726: the scroll's content origin in the page's space - a slip anchor drawn inside the scroll is registered where the pointer is.</summary>
        private Vector2 _deskAnchorOffset;
        /// <summary>§726: the film's hook - each card's top in the scroll's content, so a frame that films a card below the fold scrolls to it by name.</summary>
        private readonly Dictionary<string, float> _deskCardTops = new Dictionary<string, float>();

        /// <summary>A slip anchor for a rect drawn inside the desk's scroll.</summary>
        private void DeskAnchor(Rect r, string id) => SlipAnchor(new Rect(r.position + _deskAnchorOffset, r.size), id);

        /// <summary>
        /// §726: the desk's cards on the twelve columns, in a scroll inside <paramref name="view"/>: World trade (7) beside the calendar (5); Approval (7)
        /// beside This month (5) - the composition's two rows; then the two rows it does not draw, kept: Estimated effects (7) beside the compass (5), and
        /// the event card across the twelve.
        /// </summary>
        private void DrawDeskCards(Rect view)
        {
            float scrollbar = Mathf.Max(GUI.skin.verticalScrollbar.fixedWidth, V35.Px(12f)) + V35.Px(4f);
            float width = Mathf.Max(1f, view.width - scrollbar);
            float gutter = V35.Px(V35.Gutter);
            float left = V35Span(width, 7);
            float right = width - left - gutter;
            float rowWorld = V35.Px(DeskRowWorld), rowLedger = V35.Px(DeskRowLedger), rowEffects = V35.Px(DeskRowEffects), rowEvent = V35.Px(DeskRowEvent);
            var content = new Rect(0f, 0f, width, rowWorld + rowLedger + rowEffects + rowEvent + gutter * 3f);

            _deskScrollPosition = GUI.BeginScrollView(view, _deskScrollPosition, content, false, true);
            _deskAnchorOffset = view.position - _deskScrollPosition;
            V35.FloorGuarded = true;   // §726: the v3.5 page - a shrink below the floor is an overflow here
            float y = 0f;
            _deskCardTops["world"] = y;
            DrawDeskMapCard(new Rect(0f, y, left, rowWorld));
            DrawDeskCalendarCard(new Rect(left + gutter, y, right, rowWorld));
            y += rowWorld + gutter;
            _deskCardTops["approval"] = y;
            DrawDeskApprovalLedger(new Rect(0f, y, left, rowLedger));
            DrawDeskMonthCard(new Rect(left + gutter, y, right, rowLedger));
            y += rowLedger + gutter;
            _deskCardTops["effects"] = y;
            DrawDeskEffectsCard(new Rect(0f, y, left, rowEffects));
            DrawDeskCompass(new Rect(left + gutter, y, right, rowEffects));
            y += rowEffects + gutter;
            _deskCardTops["event"] = y;
            DrawDeskEventCard(new Rect(0f, y, width, rowEvent));
            V35.FloorGuarded = false;
            GUI.EndScrollView();
        }

        /// <summary>The world map (Annex B I1) on its v3.5 card (§726: globe, "World trade"; the old corner caption is the head's slip), read-only on the
        /// stage (R-B6: a click pins nothing here - the International document is where a readout lives); the renderer's own hover readout stays. The
        /// chips' codes at the floor, mono bold, as the composition sets them.</summary>
        private void DrawDeskMapCard(Rect r)
        {
            Rect inner = DrawV35Card(r);
            var slip = new SlipContent("WORLD TRADE").Add("THE WORLD — TRADE VOLUME").Add("A CHIP NAMES ITS COUNTRY ON HOVER");
            _deskSlipBook.Anchors["card:world"] = slip;
            Rect head = DrawV35CardHead(inner, "globe", "World trade", PoliSimTheme.TextSecondary);
            DeskAnchor(head, "card:world");
            Rect body = DrawV35DenseLine(V35UnderHead(inner, head), slip);
            _mapRenderer.OwnTooltip = true;   // §710: the Desk hangs no slips on the chips - the renderer's own hover box, the name its first line
            _mapRenderer.V35Chips = true;     // §726: the composition's chip (38 x 26, the code at the floor); the renderer is shared, so it is put back
            _mapRenderer.Draw(body, _world.Countries, PlayerCountryId, _mapEventMarkers, _simulationManager.CurrentTurn, EventMarkerFadeTurns, V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true), out _, out _);
            _mapRenderer.V35Chips = false;
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
            // §726 (v3.5, the composition's list card): the icon, the live approval as the hero figure and the card's name in its head; the caption that
            // stood beside the hero ("APPROVAL RATING · LIVE") and the ledger's title are the head's slip. The FIRST ATTRIBUTION promise chip is a state,
            // not sub-text, and stays in the head at the right.
            Rect inner = DrawV35Card(r);
            List<StatTracePanel.DeskTerm> terms = StatTracePanel.BuildApprovalDeskTerms(_playerCountry);
            bool empty = terms == null || terms.Count == 0;
            var slip = new SlipContent("APPROVAL").Add("APPROVAL RATING · LIVE").Add("NINE-TERM ATTRIBUTION · LEDGER - WHAT MOVED IT THIS PERIOD, BY TERM");
            _deskSlipBook.Anchors["card:approval"] = slip;

            float iconSide = V35.Px(V35.ListIcon);
            GUIStyle hero = V35Mono(34f, PoliSimTheme.TextPrimary, bold: true);
            string heroText = UiFormat.Number(_playerCountry.State.ApprovalRating, 1);
            Vector2 heroSize = hero.CalcSize(new GUIContent(heroText));
            float headHeight = Mathf.Max(iconSide, Mathf.Ceil(heroSize.y));
            var head = new Rect(inner.x, inner.y, inner.width, headHeight);
            DrawV35Icon(new Rect(head.x, head.y + Mathf.Round((headHeight - iconSide) * 0.5f), iconSide, iconSide), "check", PoliSimTheme.TextSecondary);
            float x = head.x + iconSide + V35.Px(12f);
            var heroRect = new Rect(x, head.y, Mathf.Ceil(heroSize.x) + 2f, headHeight);
            PoliSimWidgets.MeasuredLabel(heroRect, heroText, hero);
            x = heroRect.xMax + V35.Px(12f);
            float nameRight = head.xMax;

            if (empty)
            {
                // The promise chip (1m-r2): the date the first period closes, in the caution ink and border - the model's boundary, never a placeholder.
                GUIStyle chipStyle = V35Mono(V35.Floor, PoliSimTheme.Caution, bold: true, TextAnchor.MiddleCenter);
                string chipText = $"FIRST ATTRIBUTION — {DeskFirstAttributionDate().ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()}";
                float chipWidth = Mathf.Ceil(chipStyle.CalcSize(new GUIContent(chipText)).x) + V35.Px(12f);
                float chipHeight = Mathf.Ceil(chipStyle.CalcSize(new GUIContent(chipText)).y) + V35.Px(6f);
                var chipRect = new Rect(head.xMax - chipWidth, head.y + Mathf.Round((headHeight - chipHeight) * 0.5f), chipWidth, chipHeight);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.RoundedCard(chipRect, V35.CardPaper, PoliSimTheme.Caution, 0f);
                    PoliSimWidgets.MeasuredLabel(chipRect, chipText, chipStyle);
                }
                nameRight = chipRect.x - V35.Px(8f);
            }
            PoliSimWidgets.MeasuredLabel(new Rect(x, head.y, Mathf.Max(1f, nameRight - x), headHeight), "Approval", V35Serif(17f, PoliSimTheme.TextPrimary));
            DeskAnchor(new Rect(head.x, head.y, Mathf.Max(1f, nameRight - head.x), headHeight), "card:approval");

            Rect body = DrawV35DenseLine(V35UnderHead(inner, head), slip);
            float rowHeight = V35.Px(V35.ListRow) + 1f;
            int room = Mathf.Max(0, Mathf.FloorToInt(body.height / rowHeight));
            float y = body.y;

            if (empty)
            {
                // Year 0 (1m-r2): the nine rows with the em dash in the muted ink, never a zero.
                string[] names = StatTracePanel.ApprovalDeskTermNames;
                int shownNames = Mathf.Min(names.Length, room);
                for (int i = 0; i < shownNames; i++)
                {
                    DrawDeskTermRow(new Rect(body.x, y, body.width, rowHeight), names[i], null);
                    y += rowHeight;
                }
                return;
            }

            int shown = terms.Count <= room ? terms.Count : Mathf.Max(0, room - 1);
            for (int i = 0; i < shown; i++)
            {
                DrawDeskTermRow(new Rect(body.x, y, body.width, rowHeight), terms[i].Name, terms[i].Value);
                y += rowHeight;
            }

            if (shown < terms.Count && room > 0)
            {
                float rest = 0f;
                for (int i = shown; i < terms.Count; i++) { rest += terms[i].Value; }
                DrawDeskTermRow(new Rect(body.x, y, body.width, rowHeight), $"+{terms.Count - shown} more terms", rest);
            }
        }

        /// <summary>One ledger row (§726: the v3.5 list row - the term in the serif, its signed figure in the mono at the right, the row's rule); a null value
        /// is the Year-0 em dash in the muted ink (1m-r2), never a zero.</summary>
        private void DrawDeskTermRow(Rect rect, string name, float? value)
        {
            Color ink = value.HasValue ? UiPalette.GetDeltaColor(value.Value, higherIsBetter: true) : PoliSimTheme.TextMuted;
            string text = value.HasValue ? StatsReadings.TrueMinus(value.Value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)) : "—";
            DrawV35ListRow(rect, null, 0f, name, text, ink);
        }

        /// <summary>
        /// The compass (Annex B I2) in the board's 240 square: the renderer's honest footprint
        /// (R-SP4) is the plot square plus its caption band, so the plot here is the square less the
        /// band the captions need at this width - the captions inside the declared rect, which is the
        /// board's D3 ("axis captions drawn INSIDE its rect") as the renderer already draws it.
        /// </summary>
        private void DrawDeskCompass(Rect card)
        {
            // §726: on a v3.5 card (compass, "Political compass"); the corner caption and the two axis captions that stood under the plot are the head's
            // slip (V35 rule 1). Not in the composition's desk - kept, asked.
            Rect inner = DrawV35Card(card);
            (string axisX, string axisY) = PoliticalCompassRenderer.AxisCaptionTexts;
            var slip = new SlipContent("POLITICAL COMPASS").Add("POLITICAL COMPASS · SIX STATES").Add(axisX.ToUpperInvariant()).Add(axisY.ToUpperInvariant());
            _deskSlipBook.Anchors["card:compass"] = slip;
            Rect head = DrawV35CardHead(inner, "compass", "Political compass", PoliSimTheme.TextSecondary);
            DeskAnchor(head, "card:compass");
            Rect box = DrawV35DenseLine(V35UnderHead(inner, head), slip);
            GUIStyle style = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            _politicalCompassRenderer.AxisCaptions = false;   // the renderer is shared - put back below
            Vector2 footprint = _politicalCompassRenderer.Footprint(_world.Countries, Mathf.Min(box.width, box.height), box.width, style, PlayerCountryId, withLegend: false);
            // The full box width, not the footprint's: the renderer paints its own paper over the rect
            // it is given, and a narrower rect left a strip of the plate showing at the right (the
            // first matrix at 1600/1920); the plot side is bounded by the height either way.
            var rect = new Rect(box.x, box.y, box.width, Mathf.Min(box.height, footprint.y));
            _politicalCompassRenderer.Draw(rect, _world.Countries, PlayerCountryId, style, withLegend: false);
            _politicalCompassRenderer.AxisCaptions = true;
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

            // §694 (ruled 2026-09-30): WHAT THE CARD ESTIMATES IS THE ROLE'S. A governing player's own draft (the cached preview, as before); any other role's
            // what is before the chamber - the player's alternative once tabled, else the government's budget - then the alternative as drafted, then the book as it stands (DeskEffectsNote).
            DeskEffectsSubject subject = DeskEffectsSubjectNow(out string rateLever, out bool mayTable, out int chamberDays);
            PolicyPreview shown = DeskEffectsPreview(subject);
            bool emptyState = DeskEffectsNote.IsEmptyState(subject);

            // §726 (v3.5): on a card - the head NAMES whose budget the arrows estimate (§694's line, at rest), the scope chip at its right (P2-2.1: the
            // horizons retired; the card shows the point the preview produced for next year, and the chip says so). The scope line and the methodology
            // that stood under the arrows are the head's slip (V35 rule 1); the empty state's note is the card's content and stays. Not in the
            // composition's desk - kept, asked.
            Rect inner = DrawV35Card(r);
            GUIStyle chipCaption = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
            const string scopeChip = "NEXT YEAR";
            float scopeWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(scopeChip)).x) + V35.Px(12f);
            Rect head = DrawV35CardHead(inner, "chart", DeskEffectsNote.Head(subject), PoliSimTheme.TextSecondary, reserveRight: scopeWidth + V35.Px(8f));
            DrawDeskChipButton(new Rect(head.xMax - scopeWidth, head.y + V35.Px(2f), scopeWidth, Mathf.Max(1f, head.height - V35.Px(4f))), scopeChip, chipCaption, selected: true, disabled: true);
            string scopeText = DeskEffectsNote.Line(subject, rateLever, mayTable, chamberDays);
            var slip = new SlipContent("ESTIMATED EFFECTS").Add(scopeText);
            if (!emptyState) { slip.Add(DeskEffectsNote.Method(subject, SimulationManager.DaysPerTurn)); }
            _deskSlipBook.Anchors["card:effects"] = slip;
            DeskAnchor(new Rect(head.x, head.y, Mathf.Max(1f, head.width - scopeWidth - V35.Px(8f)), head.height), "card:effects");
            Rect body = DrawV35DenseLine(V35UnderHead(inner, head), slip);

            // 1m-r2's empty state: while nothing moves the estimate, one caption in a dashed frame under the arrows. Its claim is aligned to the model, not
            // copied from the board: the preview reads the budget sheet's draft (P3-C1) and the rate lever as drafted and every other bill as it passes
            // (R-B4's discipline). §694 (ruled): the caption names a lever only where the role holds it. C-C14: no rolled margin.
            GUIStyle note = V35SerifWrapped(V35.Floor, PoliSimTheme.TextMuted);
            float notePad = V35.Px(8f);
            float noteWidth = Mathf.Max(1f, body.width - notePad * 2f);
            float noteHeight = emptyState ? Mathf.Ceil(note.CalcHeight(new GUIContent(scopeText), noteWidth)) : 0f;
            float noteBlock = emptyState ? noteHeight + notePad * 2f + V35.Px(6f) : 0f;

            // The numerals from the same cached scaled figures the OPEN panel prints, formatted here WITHOUT the per-row "(±…)" margin. Invariant culture, as the tiles print.
            float gdp = Mathf.Max(1f, _playerCountry.State.GDP);
            string Signed(float v) => StatsReadings.TrueMinus(v.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture));
            var rows = new List<(string label, float value, string text, bool? higherIsBetter, float range)>
            {
                ("GDP growth", shown.GdpGrowthPercent, Signed(shown.GdpGrowthPercent) + "%", true, DeskRangeGdpGrowthPercent),
                ("Inflation", shown.InflationChange, Signed(shown.InflationChange) + " pts", false, DeskRangeInflationPoints),
                ("Unemployment", shown.UnemploymentChange, Signed(shown.UnemploymentChange) + " pts", false, DeskRangeUnemploymentPoints),
                ("Approval", shown.ApprovalChange, Signed(shown.ApprovalChange), true, DeskRangeApproval),
                ("Poverty rate", shown.PovertyRateChange, Signed(shown.PovertyRateChange) + " pts", false, DeskRangePovertyPoints),
                ("Labor force participation", shown.LaborForceParticipationRateChange, Signed(shown.LaborForceParticipationRateChange) + " pts", true, DeskRangeParticipationPoints),
                ("Crime index", shown.CrimeIndexChange, Signed(shown.CrimeIndexChange), false, DeskRangeCrimeIndex),
                ("Net budget", shown.NetBudgetImpact, UiFormat.MoneyDelta(shown.NetBudgetImpact, MoneyUnit.Billions), null, DeskRangeNetBudgetShareOfGdp * gdp)   // §725 (V35 rule 5, Elias's ruling): a change in the balance has no consensus direction - its arrow in the neutral ink
            };

            // §568 (2026-09-22, Design's drift row D2): BOARD 5c'S ARROWS, not a column of centred bars - the one grammar every surface estimating an effect
            // draws (EffectArrowsRenderer: the arrows rise and fall from one hairline baseline, the figure under each). Two panels of four; the renderer
            // sizes its own lanes by their widest word and the guard reports it if they break. §726: the labels at the floor.
            float gap = V35.Px(4f);
            float panelHeight = Mathf.Max(1f, Mathf.Floor((body.height - noteBlock - gap) * 0.5f));
            GUIStyle arrowLabel = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            var arrows = new List<EffectArrow>(rows.Count);
            foreach (var row in rows) { arrows.Add(new EffectArrow(row.label, row.value, row.higherIsBetter, row.text)); }
            int half = Mathf.CeilToInt(arrows.Count * 0.5f);
            float y = body.y;
            EffectArrowsRenderer.Draw(new Rect(body.x, y, body.width, panelHeight), arrows.GetRange(0, half), arrowLabel, V35.FontPx(V35.Floor));
            y += panelHeight + gap;
            EffectArrowsRenderer.Draw(new Rect(body.x, y, body.width, panelHeight), arrows.GetRange(half, arrows.Count - half), arrowLabel, V35.FontPx(V35.Floor));
            y += panelHeight + V35.Px(6f);

            if (emptyState)
            {
                float boxHeight = Mathf.Min(Mathf.Max(1f, body.yMax - y), noteHeight + notePad * 2f);
                DeskDashedFrame(new Rect(body.x, y, body.width, boxHeight), PoliSimTheme.HairlineStrong, 4f, 3f);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Desk effects no-draft caption", new Rect(body.x + notePad, y + notePad, noteWidth, noteHeight), body);
                }
                GUI.Label(new Rect(body.x + notePad, y + notePad, noteWidth, Mathf.Max(1f, boxHeight - notePad * 2f)), scopeText, note);
            }
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
        /// The 1k calendar sheet (Annex B I5): the month page as built (DrawCalendarMonthGrid - C8's weekday row, C9's cells) inside a GUILayout
        /// island, on its v3.5 card since §726; the dated ledger that stood beneath it is This month's card (<see cref="DrawDeskMonthCard"/>).
        /// </summary>
        private void DrawDeskCalendarCard(Rect r)
        {
            // §726 (v3.5): the month grid on its own card - the calendar icon and the month as the card's name (the grid's own title and section rule
            // drop, the head says it); the dated ledger beneath it is This month's card, beside the Approval ledger, as the composition lays them.
            System.DateTime today = _simulationManager.CurrentDate;
            var monthStart = new System.DateTime(today.Year, today.Month, 1);
            Dictionary<int, List<CalendarMarker>> markers = BuildCalendarMonthMarkers(monthStart, today);

            Rect inner = DrawV35Card(r);
            var slip = new SlipContent("CALENDAR").Add("TODAY · " + today.ToString("d MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture))
                .Add("A DOT IS AN ENTRY IN THIS MONTH'S LEDGER, IN ITS AREA'S INK · A STRUCK DAY HAS PASSED");
            _deskSlipBook.Anchors["card:calendar"] = slip;
            Rect head = DrawV35CardHead(inner, "cal", monthStart.ToString("MMMM yyyy", CultureInfo.CurrentCulture), PoliSimTheme.TextSecondary);
            DeskAnchor(head, "card:calendar");
            Rect body = DrawV35DenseLine(V35UnderHead(inner, head), slip);

            GUILayout.BeginArea(body);
            GUILayout.BeginVertical();
            DrawCalendarMonthGrid(monthStart, today, markers, withTitle: false);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// §726 (v3.5): THIS MONTH - the dated ledger (Annex B I5's rows, C10) on its own card (bell, "This month"), as the composition lays it beside the
        /// Approval ledger: each row the day in the mono at the floor, the area's dot, the entry in the serif, the row's rule; rows that do not fit are
        /// stated as "+N more this month", never trimmed quietly.
        /// </summary>
        private void DrawDeskMonthCard(Rect r)
        {
            System.DateTime today = _simulationManager.CurrentDate;
            var monthStart = new System.DateTime(today.Year, today.Month, 1);
            Dictionary<int, List<CalendarMarker>> markers = BuildCalendarMonthMarkers(monthStart, today);

            Rect inner = DrawV35Card(r);
            var slip = new SlipContent("THIS MONTH").Add("THE MONTH'S DATED ENTRIES, BY DAY - FIGURES PUBLISHED, THE FISCAL YEAR'S DATES").Add("A DOT'S INK IS THE ENTRY'S AREA");
            _deskSlipBook.Anchors["card:month"] = slip;
            Rect head = DrawV35CardHead(inner, "bell", "This month", PoliSimTheme.TextSecondary);
            DeskAnchor(head, "card:month");
            Rect body = DrawV35DenseLine(V35UnderHead(inner, head), slip);
            bool repaint = Event.current.type == EventType.Repaint;

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

            GUIStyle dateFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle labelFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float rowHeight = V35.Px(V35.ListRow) + 1f;
            if (rows.Count == 0)
            {
                if (repaint) { PoliSimWidgets.MeasuredLabel(new Rect(body.x, body.y, body.width, rowHeight), "Nothing dated this month", Inked(new GUIStyle(labelFace), PoliSimTheme.TextMuted)); }
                return;
            }

            int room = Mathf.Max(0, Mathf.FloorToInt(body.height / rowHeight));
            int shown = rows.Count <= room ? rows.Count : Mathf.Max(0, room - 1);
            float dateWidth = Mathf.Ceil(dateFace.CalcSize(new GUIContent("31 Jan")).x) + V35.Px(4f);
            float dot = V35.Px(6f);
            float top = body.y;
            for (int i = 0; i < shown; i++)
            {
                var row = new Rect(body.x, top, body.width, rowHeight);
                float dotX = row.x + dateWidth + V35.Px(6f);
                float textX = dotX + dot + V35.Px(8f);
                float textWidth = Mathf.Max(1f, row.xMax - textX);
                // §726: a long entry is cut at a word with an ellipsis (the floor forbids shrinking it), and the whole entry is the row's slip
                string label = V35Fit(rows[i].Label, labelFace, textWidth, out bool cut);
                string date = new System.DateTime(monthStart.Year, monthStart.Month, rowDays[i]).ToString("d MMM", CultureInfo.CurrentCulture);
                if (cut)
                {
                    _deskSlipBook.Anchors["month:" + i] = new SlipContent(date.ToUpper(CultureInfo.CurrentCulture)).Add(rows[i].Label.ToUpper(CultureInfo.CurrentCulture));
                    DeskAnchor(row, "month:" + i);
                }
                if (repaint)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, dateWidth, rowHeight), date, dateFace);
                    PoliSimTheme.Pill(new Rect(dotX, row.y + Mathf.Round((rowHeight - dot) * 0.5f), dot, dot), UiPalette.GetAreaColor(rows[i].Area));
                    PoliSimWidgets.MeasuredLabel(new Rect(textX, row.y, textWidth, rowHeight), label, labelFace);
                    PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), V35.ListRule);
                    UiContainmentGuard.Check("Desk month row", row, body);
                }
                top += rowHeight;
            }

            if (repaint && shown < rows.Count && room > 0)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(body.x, top, body.width, rowHeight), $"+{rows.Count - shown} more this month", Inked(new GUIStyle(labelFace), PoliSimTheme.TextMuted));
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
            // §726 (v3.5): the event card across the twelve columns - not in the composition's desk; kept, asked. The empty state is the reservation as
            // before (a dashed frame, one line at the floor); a live event is a card: the BREAKING stamp and the event's name as its head, the description
            // (the event's only text - content, not sub-text) in the serif, the three shocks as the diverging bars with their labels at the floor.
            EconomicEvent activeEvent = _simulationManager.GetLastEvent(PlayerCountryId);
            if (activeEvent == null)
            {
                DrawDeskEventReservation(r);
                return;
            }

            Rect inner = DrawV35Card(r);
            GUIStyle chipStyle = V35Mono(V35.Floor, PoliSimTheme.Caution, bold: true, TextAnchor.MiddleCenter);
            const string chipText = "BREAKING";
            Vector2 chipSize = PoliSimWidgets.StampSize(chipText, chipStyle);   // §568: the stamp's one face
            float headHeight = Mathf.Max(V35.Px(V35.CardIcon), chipSize.y);
            var chipRect = new Rect(inner.x, inner.y + Mathf.Round((headHeight - chipSize.y) * 0.5f), chipSize.x, chipSize.y);
            PoliSimWidgets.Stamp(chipRect, chipText, chipStyle, PoliSimTheme.Caution);
            float nameX = chipRect.xMax + V35.Px(12f);
            PoliSimWidgets.MeasuredLabel(new Rect(nameX, inner.y, Mathf.Max(1f, inner.xMax - nameX), headHeight), activeEvent.Name, V35Serif(V35.Name, PoliSimTheme.TextPrimary));
            float y = inner.y + headHeight + V35.Px(V35.CardHeadGap);

            GUIStyle description = V35SerifWrapped(V35.Floor, PoliSimTheme.TextSecondary);
            float descriptionHeight = Mathf.Ceil(description.CalcHeight(new GUIContent(activeEvent.Description), inner.width));
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Desk event description", new Rect(inner.x, y, inner.width, descriptionHeight), inner);
            }
            GUI.Label(new Rect(inner.x, y, inner.width, descriptionHeight), activeEvent.Description, description);
            y += descriptionHeight + V35.Px(8f);

            var bars = new List<(string label, float value, bool higherIsBetter, float range)>
            {
                ("GDP", activeEvent.GdpShockPercent, true, DeskRangeEventGdpPercent),
                ("INFL", activeEvent.InflationShockPoints, false, DeskRangeEventInflationPoints),
                ("APPR", activeEvent.ApprovalEffect, true, DeskRangeEventApproval)
            };
            GUIStyle barLabel = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
            float barWidth = V35.Px(88f);
            float barHeight = V35.Px(10f);
            float rowHeight = Mathf.Max(barHeight, Mathf.Ceil(barLabel.CalcSize(new GUIContent("Ag")).y));
            float x = inner.x;
            for (int i = 0; i < bars.Count; i++)
            {
                float labelWidth = Mathf.Ceil(barLabel.CalcSize(new GUIContent(bars[i].label)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, y, labelWidth, rowHeight), bars[i].label, barLabel);
                x += labelWidth + V35.Px(6f);
                var bar = new Rect(x, y + (rowHeight - barHeight) * 0.5f, barWidth, barHeight);
                DrawDeskDivergingBar(bar, bars[i].value, bars[i].range, bars[i].higherIsBetter);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Desk event bar", bar, inner);
                }
                x += barWidth + V35.Px(24f);
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
            // §568 (2026-09-22, Design's drift row D6): the readings are TILES here too - the same cell the Statistics head draws, at this band's own pitch. The gap
            // between them is where the hairline divider stood; board 1m-r2's *"no plates"* is superseded by the 2026-09-22 ruling, and nothing else about the band moves.
            float gap = Mathf.Round(4f * ux);
            float width = (r.width - gap * (chips.Count - 1)) / chips.Count;
            float padX = Mathf.Round(6f * ux);
            float padY = Mathf.Round(4f * uy);
            float sparkWidth = Mathf.Round(46f * ux);
            float sparkHeight = Mathf.Max(4f, Mathf.Round(10f * uy));
            for (int i = 0; i < chips.Count; i++)
            {
                var plate = new Rect(r.x + i * (width + gap), r.y, width, r.height);
                DrawReadingCell(plate, chips[i], 7.5f, 17f, 7f, padX, padY, sparkWidth, sparkHeight);
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
        private void DrawDeskEventReservation(Rect r)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            // P4-D3 (2026-09-04): the empty slot is an instrument-class line, not a paragraph - the dashed reservation and
            // one caption in the card's own mono, centred in the band: the year and the fact. The purpose line and the
            // two-line paragraph it replaces restated what the frame already says. §726: the line at the floor.
            DeskDashedFrame(r, PoliSimTheme.HairlineStrong, 5f, 4f);
            GUIStyle quiet = V35Mono(V35.Floor, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            PoliSimWidgets.MeasuredLabel(r, $"YEAR {_simulationManager.CurrentTurn} · NO EVENT LIVE", quiet);
        }

        /// <summary>
        /// §694 (ruled): the effects card's subject by the player's role (<see cref="DeskEffectsNote.SubjectOf"/>), with what its note may name: the
        /// rate lever the role holds (<paramref name="rateLever"/> - the dial where the country sets its own rate, the push in the eurozone, none where
        /// a chair sets it or the player does not govern: the rate is the prime minister's lever, `DrawLeverLock`'s), whether the role may table an
        /// alternative budget (not a junior partner - its voice is the coalition agreement - and a chamber that weighs an alternative, `WeighsAlternative` (US-20), `TableShadowBudget`'s
        /// own refusals), and the days until the chamber decides the budget before it. Replaces the test that read the rate input alone, which a
        /// budget draft never reached although it moves the estimate (P3-C1).
        /// </summary>
        private DeskEffectsSubject DeskEffectsSubjectNow(out string rateLever, out bool mayTable, out int chamberDays)
        {
            bool governs = _simulationManager.PlayerGoverns(_playerCountry);
            rateLever = governs && _playerCountry.CurrentFedChair == null
                ? (CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world) ? DeskEffectsNote.RatePush : DeskEffectsNote.RateDial)
                : null;
            bool junior = _playerCountry.Government != null && _playerCountry.Government.RoleOf(_playerCountry.PlayerPartyAbbrev) == PoliSim.Elections.PlayerRole.JuniorPartner;
            mayTable = !governs && !junior && PoliSim.Elections.WorldClock.WeighsAlternative(PlayerCountryId);   // US-20: the chamber weighs an alternative
            BudgetBill pending = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            BudgetBill tabled = _simulationManager.GetPendingBudgetAlternative(PlayerCountryId);
            chamberDays = pending != null ? pending.DaysRemaining : 0;
            bool rateDrafted = rateLever != null && !Mathf.Approximately(_interestRateChangeInput, 0f);
            return DeskEffectsNote.SubjectOf(governs, _cachedDraftMoves, rateDrafted, tabled != null, mayTable, pending != null && pending.GovernmentBill);
        }

        // §694: the preview of a budget the player did not draft this frame - the government's before the chamber, or the player's alternative as tabled -
        // cached on the bill, its fingerprint and the turn, as the main preview is cached on the turn.
        private PolicyPreview _deskSubjectPreview;
        private BudgetBill _deskSubjectBill;
        private int _deskSubjectFingerprint;
        private int _deskSubjectTurn = -1;

        /// <summary>§694: the preview the card draws for its subject - the cached preview for the player's own draft (governing, or an alternative
        /// drafted), the standing book's for nothing before the chamber, and the tabled bill's own for the government's budget or a tabled alternative.</summary>
        private PolicyPreview DeskEffectsPreview(DeskEffectsSubject subject)
        {
            if (subject == DeskEffectsSubject.StandingBook) { return _cachedPreviewWithoutDraft ?? _cachedPreview; }
            if (subject != DeskEffectsSubject.GovernmentDraft && subject != DeskEffectsSubject.YourAlternativeTabled) { return _cachedPreview; }
            BudgetBill bill = subject == DeskEffectsSubject.GovernmentDraft ? _simulationManager.GetPendingBudgetBill(PlayerCountryId) : _simulationManager.GetPendingBudgetAlternative(PlayerCountryId);
            if (bill == null) { return _cachedPreviewWithoutDraft ?? _cachedPreview; }
            int fingerprint = DraftFingerprint(bill);
            if (_deskSubjectPreview == null || !ReferenceEquals(bill, _deskSubjectBill) || fingerprint != _deskSubjectFingerprint || _deskSubjectTurn != _simulationManager.CurrentTurn)
            {
                _deskSubjectPreview = _simulationManager.PreviewTurnWithBudgetDraft(PlayerCountryId, PolicyDecision.None(), bill);
                _deskSubjectBill = bill;
                _deskSubjectFingerprint = fingerprint;
                _deskSubjectTurn = _simulationManager.CurrentTurn;
            }
            return _deskSubjectPreview;
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
            Vector2 stampSize = PoliSimWidgets.StampSize(stampText, stampStyle);   // §568: the stamp's one face

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
            PoliSimWidgets.Stamp(stampRect, stampText, stampStyle, PoliSimTheme.Bad);
            if (reasonHeight > 0f)
            {
                var reasonRect = new Rect(plate.x + Mathf.Round(12f * ux), stampRect.yMax + Mathf.Round(6f * uy), reasonWidth, reasonHeight);
                UiContainmentGuard.Check("Desk game-over reason", reasonRect, plate);
                GUI.Label(reasonRect, reasonText, reason);
            }
        }
    }
}
