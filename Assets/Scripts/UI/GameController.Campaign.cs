using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E1 — **Campaign HQ**, the first screen of the Track E class. A `partial` companion of
    /// <see cref="GameController"/> drawn in the v3 idiom and NOT a re-skin of anything: the folded
    /// rail plus one full-bleed sheet, the board's 1156×680 placements inside it, type scaled from
    /// the board by `DeskPx` and floored at D4's 9 px, captions in the document face, ledger rows at
    /// the Desk's pitch. It reuses `DeskCaption`/`DeskBody`/`DeskNumeral`/`DrawDeskChipButton`
    /// directly rather than restating them, so the two stages share one type ladder — a player
    /// moving between them must not see two.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1.** The screen draws when `_campaignScreen` has a
    /// value, and only the screenshot driver sets that (`-shotcampaign`), exactly as
    /// `_instrumentLadder` works for the instrument ladder. There is no rail cell, no tab, no save
    /// hook and no gameplay path that reaches it; the branch sits beside `_onDesk` in the frame's
    /// content column so the rail is real. When wiring is ruled (W-G1) this becomes an ordinary
    /// screen by adding the rail cell and nothing else.
    ///
    /// **Every figure is DERIVED.** The screen lays out a <see cref="CampaignSnapshot"/> and
    /// computes nothing of its own beyond summing what it was handed, so "is that number real?" is
    /// answered by who filled the snapshot rather than by reading the drawing code. The race is read
    /// from a <see cref="Poll"/>, never from a preference vector — W-B10's rule that the UI never
    /// sees the truth is carried into the view layer by the type the view is given, and the poll's ±
    /// is drawn as a band around the bar rather than printed as a footnote, because the uncertainty
    /// IS the reading.
    ///
    /// **No sprite is invented.** The party identity marks are the delivered `mark_party_*` set
    /// (their first call site — `IconLibrary.GetPartyMark`); everything else is procedural in the
    /// existing idiom. A gap becomes a line for the Track H Design ask, never new art here.
    /// </summary>
    public partial class GameController
    {
        // The board this screen is composed against is the Desk's, deliberately: same inner ratio,
        // same three columns at 440 / 250 / 440, same 8.5 px plate captions, so the ladder is one
        // ladder. DeskBoardInnerWidth / DeskBoardInnerHeight are the authority; these are the
        // per-plate denominators the placements below divide by.
        private const float CampaignResourcesPlateHeight = 300f;
        private const float CampaignRacePlateHeight = 576f;

        /// <summary>
        /// The snapshot the campaign screens draw. Non-null ONLY while the screenshot harness has
        /// staged one; every player path leaves it null and takes the Desk branch. Mirrors
        /// `_instrumentLadder` exactly, including that nothing serialises it.
        /// </summary>
        private CampaignSnapshot? _campaignScreen;

        /// <summary>
        /// The sheet's inner rect as the last Repaint measured it. IMGUI hands back a 1×1 dummy on
        /// the Layout event; the chip controls in the masthead are hit-tested against rects derived
        /// from this one, so they would jump between events without the cache. Same defect, same fix
        /// as `_deskInnerRect` (polisim-imgui-layout-facts, item 6).
        /// </summary>
        private Rect _campaignInnerRect;

        /// <summary>
        /// The ONLY way this screen is reached, and it exists for the screenshot harness (R-N2).
        /// `internal` and undocumented in the rail on purpose: a public setter would be a wiring
        /// path, and W-G1 has not been ruled. Mirrors SetInstrumentLadder exactly, null clearing
        /// back to whatever screen the frame was on.
        /// </summary>
        internal void SetCampaignScreen(CampaignSnapshot? snapshot)
        {
            _campaignScreen = snapshot;
        }

        /// <summary>
        /// Campaign HQ in the frame's content column. Composed exactly as `DrawDeskStage` is: the
        /// caller has already taken the box's padding and margin out of `availableHeight`, so it IS
        /// the inner height and must not be reserved against twice (instance #12 — taking it twice
        /// left a dark band under the sheet on the first 1m-r2 film).
        /// </summary>
        private void DrawCampaignHqStage(float availableHeight, float availableWidth, CampaignSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.Height(availableHeight + _boxStyle.padding.vertical));
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignInnerRect = inner; }
            else if (_campaignInnerRect.width > 1f) { inner = _campaignInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot, "CAMPAIGN HQ");

            DrawCampaignResources(Board(0f, 36f, 440f, CampaignResourcesPlateHeight), snapshot);
            DrawCampaignStaffAndOffices(Board(0f, 348f, 440f, 264f), snapshot);

            DrawCampaignRace(Board(453f, 36f, 250f, CampaignRacePlateHeight), snapshot);

            DrawCampaignQueue(Board(716f, 36f, 440f, CampaignResourcesPlateHeight), snapshot);
            DrawCampaignLegality(Board(716f, 348f, 440f, 264f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCampaignStrip(Board(0f, 627f, 1156f, 53f), snapshot);
        }

        // ------------------------------------------------------------------------------------------
        // The masthead: who is campaigning, in what phase, with how long left.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignMasthead(Rect r, CampaignSnapshot s, string screenTitle)
        {
            float ux = r.width / 1156f;
            float uy = r.height / 28f;

            float x = r.x;
            Texture2D mark = IconLibrary.GetPartyMark(s.MarkKey);
            if (mark != null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    // Authored in its own colours — drawn untinted, per the accessor's contract.
                    GUI.DrawTexture(new Rect(x, r.y + Mathf.Round(2f * uy), Mathf.Round(24f * uy), Mathf.Round(24f * uy)),
                        mark, ScaleMode.ScaleToFit);
                }

                x += Mathf.Round(24f * uy) + Mathf.Round(8f * ux);
            }

            GUIStyle title = DeskCaption(11f, PoliSimTheme.TextPrimary, bold: true);
            string titleText = $"{s.PartyName} · {s.CountryName}".ToUpperInvariant();
            float titleWidth = Mathf.Ceil(title.CalcSize(new GUIContent(titleText)).x) + Mathf.Round(4f * ux);
            PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, titleWidth, r.height), titleText, title);

            PoliSimWidgets.MeasuredLabel(
                new Rect(x + titleWidth + Mathf.Round(12f * ux), r.y, Mathf.Round(260f * ux), r.height),
                screenTitle, DeskCaption(9f, PoliSimTheme.TextSecondary));

            // The phase and the countdown on the board's own chip face. Read-only here: the click is
            // swallowed by `disabled` because nothing is wired (R-N2), and a chip that looked live
            // while doing nothing would be the worse lie.
            GUIStyle chipCaption = DeskCaption(8.5f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float chipHeight = Mathf.Ceil(DeskCaptionHeight(chipCaption)) + Mathf.Round(6f * uy);
            float chipY = r.y + Mathf.Round((r.height - chipHeight) * 0.5f);

            string daysText = s.DaysUntilElection > 0
                ? $"{s.DaysUntilElection} DAYS TO POLLING DAY"
                : s.DaysUntilElection == 0 ? "POLLING DAY" : "POLL CLOSED";
            float daysWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(daysText)).x) + Mathf.Round(16f * ux);
            var daysRect = new Rect(r.xMax - daysWidth, chipY, daysWidth, chipHeight);
            DrawDeskChipButton(daysRect, daysText, chipCaption, selected: false, disabled: true);

            string phaseText = SpacedIdentifier(s.Phase.ToString()).ToUpperInvariant();
            float phaseWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(phaseText)).x) + Mathf.Round(16f * ux);
            DrawDeskChipButton(new Rect(daysRect.x - phaseWidth - Mathf.Round(6f * ux), chipY, phaseWidth, chipHeight),
                phaseText, chipCaption, selected: true, disabled: true);
        }

        // ------------------------------------------------------------------------------------------
        // Left column, upper: the three resources §9 names, in the order they bind.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignResources(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / CampaignResourcesPlateHeight;
            float y = DrawCampaignLedgerHead(r, "RESOURCES — §9 · WAR CHEST, THE DAY, THE GROUND", ux, uy);

            // Money as the hero numeral, in the Desk's hero composition.
            GUIStyle hero = DeskNumeral(34f, PoliSimTheme.TextPrimary);
            string heroText = Kronor(s.Resources.Money);
            Vector2 heroSize = hero.CalcSize(new GUIContent(heroText));
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, heroSize.x + 4f, heroSize.y), heroText, hero);

            GUIStyle heroCaption = DeskCaption(9f, PoliSimTheme.TextSecondary, false, TextAnchor.LowerLeft);
            float captionLeft = r.x + heroSize.x + Mathf.Round(10f * ux);
            string spent = s.MoneyAtCampaignStart > 0
                ? string.Format(CultureInfo.InvariantCulture, "kr · {0:F0} % OF THE CHEST SPENT", s.MoneySpentShare * 100.0)
                : "kr · WAR CHEST";
            PoliSimWidgets.MeasuredLabel(
                new Rect(captionLeft, y, Mathf.Max(1f, r.xMax - captionLeft), heroSize.y - Mathf.Round(4f * uy)),
                spent, heroCaption);
            y += heroSize.y + Mathf.Round(8f * uy);

            // The day's hours: §9's genuinely binding resource, so it gets the bar. Hours cannot be
            // raised, banked or bought — the bar is the one instrument on this screen that shows a
            // ceiling rather than a stock.
            GUIStyle barCaption = DeskCaption(9f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float figureWidth = Mathf.Ceil(barCaption.CalcSize(new GUIContent("00.0 / 00 HOURS")).x) + Mathf.Round(8f * ux);
            var track = new Rect(r.x, y + Mathf.Round(3f * uy),
                Mathf.Max(1f, r.width - figureWidth - Mathf.Round(8f * ux)), Mathf.Round(10f * uy));
            float hoursFraction = Mathf.Clamp01((float)(s.Resources.Hours / CampaignEconomy.HoursPerCampaignDay));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                if (hoursFraction > 0f)
                {
                    PoliSimTheme.RoundedBox(new Rect(track.x, track.y, track.width * hoursFraction, track.height),
                        UiPalette.GetAreaColor(UiPalette.SystemArea.Political), 0f);
                }
            }

            PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - figureWidth, y, figureWidth, Mathf.Round(16f * uy)),
                string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F0} HOURS",
                    s.Resources.Hours, CampaignEconomy.HoursPerCampaignDay), barCaption);
            y += Mathf.Round(20f * uy);

            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);

            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Volunteers",
                string.Format(CultureInfo.InvariantCulture, "{0:N0} · {1:N0} h/day",
                    s.Resources.Volunteers, CampaignEconomy.VolunteerHours(s.Resources.Volunteers)),
                nameStyle, figureStyle);
            y += rowHeight;

            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Perceived economy",
                string.Format(CultureInfo.InvariantCulture, "{0:F0} / 100", s.PerceivedEconomyIndex),
                nameStyle, figureStyle);
            y += rowHeight;

            // §19's distinction, printed because it is load-bearing rather than decorative: the
            // electorate reacts to the PUBLISHED figure, and this screen only ever sees that one.
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y + Mathf.Round(2f * uy), r.width, rowHeight),
                "AS PUBLISHED — §19. NO SCREEN HERE READS THE TRUE STATE.",
                DeskCaption(8.5f, PoliSimTheme.TextMuted));
        }

        // ------------------------------------------------------------------------------------------
        // Left column, lower: §9's staff and §10's offices, one ledger with a rule between them.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignStaffAndOffices(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 264f;
            float y = DrawCampaignLedgerHead(r, "ORGANISATION — §9 STAFF · §10 OFFICES", ux, uy);

            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle roleStyle = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);
            float roleWidth = Mathf.Round(120f * ux);

            if (s.Staff == null || s.Staff.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO STAFF HIRED", nameStyle);
                y += rowHeight;
            }
            else
            {
                foreach (StaffMember member in s.Staff)
                {
                    var row = new Rect(r.x, y, r.width, rowHeight);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, roleWidth, row.height),
                        member.Role.ToUpperInvariant(), roleStyle);
                    DrawCampaignRow(new Rect(row.x + roleWidth, row.y, row.width - roleWidth, row.height),
                        member.Name, member.BonusLabel, nameStyle, figureStyle);
                    y += rowHeight;
                }
            }

            y += Mathf.Round(6f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }
            y += Mathf.Round(6f * uy);

            if (s.Offices == null || s.Offices.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO REGIONAL OFFICES OPEN", nameStyle);
                return;
            }

            int volunteers = 0;
            double upkeep = 0.0;
            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - y) / rowHeight) - 1);
            for (int i = 0; i < s.Offices.Length; i++)
            {
                volunteers += s.Offices[i].Volunteers;
                upkeep += s.Offices[i].UpkeepPerDay;
                if (i >= room) { continue; }

                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), s.Offices[i].RegionName,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} vol · {1} kr/day",
                        s.Offices[i].Volunteers, Kronor(s.Offices[i].UpkeepPerDay)),
                    nameStyle, figureStyle);
                y += rowHeight;
            }

            // The total is derived from the same rows, and says so when rows were dropped for room.
            string totalLabel = s.Offices.Length > room
                ? $"{s.Offices.Length} offices ({s.Offices.Length - room} not shown)"
                : $"{s.Offices.Length} offices";
            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), totalLabel,
                string.Format(CultureInfo.InvariantCulture, "{0:N0} vol · {1} kr/day",
                    volunteers, Kronor(upkeep)),
                DeskBody(13f, PoliSimTheme.TextSecondary),
                DeskCaption(11f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight));
        }

        // ------------------------------------------------------------------------------------------
        // Centre column: the race, AS POLLED. The only view of it this screen has.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignRace(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / CampaignRacePlateHeight;
            float y = DrawCampaignLedgerHead(r, "THE RACE — AS POLLED", ux, uy);

            GUIStyle nameStyle = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle mutedName = DeskBody(12f, PoliSimTheme.TextSecondary);
            GUIStyle shareStyle = DeskCaption(10f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            float labelHeight = Mathf.Max(Mathf.Round(15f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);
            float barHeight = Mathf.Round(9f * uy);
            // The trailer under the bar carries the momentum caption, so it is the CAPTION's height
            // and not a board figure - at the 9 px floor the glyph box is 11.2 px and a 9 px slot
            // both clips the caption and lets it collide with the next party's name.
            GUIStyle momentumStyle = DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            float momentumHeight = Mathf.Ceil(DeskCaptionHeight(momentumStyle));
            float pitch = labelHeight + barHeight + momentumHeight + Mathf.Round(3f * uy);
            int parties = Mathf.Min(s.PartyNames.Length, s.LatestPoll.PartyCount);

            // The scale is the largest polled share plus its band, not 100 %, so eight parties under
            // a third of the vote each are legible instead of eight stubs. The axis is named in the
            // methodology line below, because an unlabelled rescaled bar is a lie by omission.
            float scale = 0.01f;
            for (int i = 0; i < parties; i++)
            {
                float top = (float)(s.LatestPoll.Share(i) + s.LatestPoll.MarginOfErrorPp(i) / 100.0);
                if (top > scale) { scale = top; }
            }

            // The methodology block is measured BEFORE the rows are budgeted, so the reserve is its
            // real wrapped height rather than a board figure that stops matching once the caption
            // floor stops scaling (at 1280 a board-derived 34 px reserve is short of three 11.2 px
            // lines, and the rows would then run into it).
            GUIStyle method = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string methodText = string.Format(CultureInfo.InvariantCulture,
                "{0} · n = {1:N0} · FIELDED {2} · BARS TO {3:F0} %, SHADED BAND = 95 % INTERVAL (SAMPLING ERROR ONLY)",
                s.LatestPoll.House.ToUpperInvariant(), s.LatestPoll.SampleSize,
                s.LatestPoll.FieldDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                scale * 100f);
            float methodHeight = Mathf.Ceil(method.CalcHeight(new GUIContent(methodText), r.width));

            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - y - methodHeight - Mathf.Round(6f * uy)) / pitch));
            int shown = Mathf.Min(parties, room);

            for (int i = 0; i < shown; i++)
            {
                bool isPlayer = i == s.PlayerPartyIndex;
                double share = s.LatestPoll.Share(i);
                double moe = s.LatestPoll.MarginOfErrorPp(i);

                string figure = string.Format(CultureInfo.InvariantCulture, "{0:F1} ±{1:F1}", share * 100.0, moe);
                float figureWidth = Mathf.Ceil(shareStyle.CalcSize(new GUIContent("00.0 ±0.0")).x) + Mathf.Round(4f * ux);
                PoliSimWidgets.MeasuredLabel(
                    new Rect(r.x, y, Mathf.Max(1f, r.width - figureWidth - Mathf.Round(6f * ux)), labelHeight),
                    s.PartyNames[i], isPlayer ? nameStyle : mutedName);
                PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - figureWidth, y, figureWidth, labelHeight),
                    figure, shareStyle);

                var track = new Rect(r.x, y + labelHeight + Mathf.Round(2f * uy), r.width, barHeight);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);

                    // The ± band first, then the point estimate over it: the interval is not an
                    // annotation on the bar, it is the width of what is actually known.
                    float lo = Mathf.Clamp01((float)((share - moe / 100.0) / scale));
                    float hi = Mathf.Clamp01((float)((share + moe / 100.0) / scale));
                    PoliSimTheme.RoundedBox(new Rect(track.x + track.width * lo, track.y,
                            Mathf.Max(1f, track.width * (hi - lo)), track.height),
                        PoliSimTheme.Tint(PoliSimTheme.TextSecondary, 0.22f), 0f);

                    float centre = Mathf.Clamp01((float)(share / scale));
                    PoliSimTheme.RoundedBox(new Rect(track.x, track.y, Mathf.Max(1f, track.width * centre), track.height),
                        isPlayer
                            ? UiPalette.GetAreaColor(UiPalette.SystemArea.Political)
                            : PoliSimTheme.Tint(PoliSimTheme.TextSecondary, 0.55f), 0f);
                }

                if (s.MomentumPp != null && i < s.MomentumPp.Length && Mathf.Abs((float)s.MomentumPp[i]) >= 0.05f)
                {
                    GUIStyle momentum = DeskCaption(8.5f,
                        UiPalette.GetDeltaColor((float)s.MomentumPp[i], higherIsBetter: true), false, TextAnchor.MiddleRight);
                    // The rect takes the STYLE's height, not the board's 9 px: at the 9 px caption
                    // floor the glyph box is 11.2 px, so a board-derived slot clips it. First film's
                    // overflow, fixed at the measurement rather than by shrinking the type.
                    PoliSimWidgets.MeasuredLabel(
                        new Rect(r.x, track.yMax, r.width, momentumHeight),
                        s.MomentumPp[i].ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " pp momentum",
                        momentum);
                }

                y += pitch;
            }

            if (shown < parties)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, Mathf.Round(14f * uy)),
                    $"+{parties - shown} MORE PARTIES POLLED", DeskCaption(8.5f, PoliSimTheme.TextMuted));
            }

            // The methodology line, drawn in the block measured above — the house, the sample, the
            // field date, the axis, and what the band means. Without it a ± is a decoration; with it
            // the reader can price the reading, which is why it is not trimmed to fit: "(SAMPLING
            // ERROR ONLY)" is the honest SCOPE of the ± and dropping it would overstate what the
            // poll knows.
            //
            // ⚠ GUI.Label + UiContainmentGuard, NOT MeasuredLabel — the Desk's established pattern
            // for a wrapped caption (DrawDeskEffectsCard's methodology line). The overflow guard
            // measures the ONE-LINE form, so a genuinely wrapping caption is reported as 500 px in
            // 248; it is checked against its own WRAPPED height instead. The first W-E1 film
            // recorded exactly that false positive at all four widths.
            var methodRect = new Rect(r.x, r.yMax - methodHeight, r.width, methodHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Campaign HQ poll methodology", methodRect, r);
            }

            GUI.Label(methodRect, methodText, method);
        }

        // ------------------------------------------------------------------------------------------
        // Right column, upper: the day's queue and what it costs.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignQueue(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / CampaignResourcesPlateHeight;
            float y = DrawCampaignLedgerHead(r, "TODAY'S QUEUE — §12", ux, uy);

            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle targetStyle = DeskCaption(9f, PoliSimTheme.TextSecondary);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);
            float actionWidth = Mathf.Round(150f * ux);
            float targetWidth = Mathf.Round(120f * ux);

            double money = 0.0;
            double hours = 0.0;
            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - y) / rowHeight) - 2);

            if (s.Queue == null || s.Queue.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    "NOTHING QUEUED — THE DAY'S HOURS ARE UNSPENT", nameStyle);
                y += rowHeight;
            }
            else
            {
                for (int i = 0; i < s.Queue.Length; i++)
                {
                    money += s.Queue[i].MoneyCost;
                    hours += s.Queue[i].Hours;
                    if (i >= room) { continue; }

                    var row = new Rect(r.x, y, r.width, rowHeight);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, actionWidth, row.height),
                        SpacedIdentifier(s.Queue[i].Kind.ToString()), nameStyle);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + actionWidth, row.y, targetWidth, row.height),
                        s.Queue[i].TargetLabel, targetStyle);
                    PoliSimWidgets.MeasuredLabel(
                        new Rect(row.x + actionWidth + targetWidth, row.y,
                            Mathf.Max(1f, row.width - actionWidth - targetWidth), row.height),
                        string.Format(CultureInfo.InvariantCulture, "{0} kr · {1:F0} h",
                            Kronor(s.Queue[i].MoneyCost), s.Queue[i].Hours),
                        figureStyle);
                    y += rowHeight;
                }

                if (s.Queue.Length > room)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                        $"+{s.Queue.Length - room} MORE QUEUED", DeskCaption(8.5f, PoliSimTheme.TextMuted));
                    y += rowHeight;
                }
            }

            y += Mathf.Round(4f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }
            y += Mathf.Round(5f * uy);

            // The totals against the ceilings. Hours is the interesting one, because ResourcePool
            // REFUSES rather than clamping (W-B2) — so a queue CAN be unaffordable, and the screen
            // must say so rather than quietly showing a plausible number.
            bool overHours = hours > s.Resources.Hours + 1e-9;
            bool overMoney = money > s.Resources.Money + 1e-9;
            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Queued today",
                string.Format(CultureInfo.InvariantCulture, "{0} kr · {1:F0} of {2:F0} h",
                    Kronor(money), hours, s.Resources.Hours),
                DeskBody(13f, PoliSimTheme.TextSecondary),
                DeskCaption(11f, overHours || overMoney ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary,
                    false, TextAnchor.MiddleRight));
            y += rowHeight;

            if (overHours || overMoney)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                    overHours && overMoney ? "OVER BUDGET ON BOTH — THE SPEND WOULD BE REFUSED"
                        : overHours ? "OVER THE DAY'S HOURS — THE SPEND WOULD BE REFUSED"
                        : "OVER THE WAR CHEST — THE SPEND WOULD BE REFUSED",
                    DeskCaption(8.5f, PoliSimTheme.Caution, bold: true));
            }
        }

        // ------------------------------------------------------------------------------------------
        // Right column, lower: what the phase permits. §3's gating, derived from the one authority.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignLegality(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 264f;
            float y = DrawCampaignLedgerHead(r, "OPEN TO YOU TODAY — §3 PHASE GATING", ux, uy);

            CampaignActionKind[] legal = CampaignLegality.LegalActions(s.Phase);

            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);

            if (legal.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    "NOTHING — NO CAMPAIGN IS OPEN IN THIS PHASE", nameStyle);
                return;
            }

            // Two columns of verbs. The list is DERIVED from CampaignLegality and never restated
            // here, so a legality change cannot leave this screen showing the old rules.
            float columnWidth = r.width * 0.5f;
            int rows = Mathf.CeilToInt(legal.Length / 2f);
            int room = Mathf.Max(0, Mathf.FloorToInt((r.yMax - y - Mathf.Round(20f * uy)) / rowHeight));
            int shownRows = Mathf.Min(rows, room);

            for (int row = 0; row < shownRows; row++)
            {
                for (int column = 0; column < 2; column++)
                {
                    int index = column * rows + row;
                    if (index >= legal.Length) { continue; }

                    PoliSimWidgets.MeasuredLabel(
                        new Rect(r.x + column * columnWidth, y + row * rowHeight,
                            columnWidth - Mathf.Round(8f * ux), rowHeight),
                        "· " + SpacedIdentifier(legal[index].ToString()), nameStyle);
                }
            }

            y += shownRows * rowHeight + Mathf.Round(6f * uy);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, Mathf.Round(14f * uy)),
                string.Format(CultureInfo.InvariantCulture, "{0} OF {1} VERBS OPEN IN THIS PHASE",
                    legal.Length, System.Enum.GetValues(typeof(CampaignActionKind)).Length),
                DeskCaption(8.5f, PoliSimTheme.TextMuted));
        }

        // ------------------------------------------------------------------------------------------
        // The strip: where the campaign stands in its own window.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignStrip(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 1156f;
            float uy = r.height / 53f;

            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextSecondary);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            float y = r.y + Mathf.Round(6f * uy);

            int total = Mathf.Max(1, s.Calendar.TotalCampaignDays);
            float progress = Mathf.Clamp01(s.CampaignDay / (float)total);
            var track = new Rect(r.x, y + Mathf.Round(3f * uy), r.width * 0.42f, Mathf.Round(8f * uy));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                if (progress > 0f)
                {
                    PoliSimTheme.RoundedBox(new Rect(track.x, track.y, Mathf.Max(1f, track.width * progress), track.height),
                        UiPalette.GetAreaColor(UiPalette.SystemArea.Political), 0f);
                }
            }

            float textLeft = track.xMax + Mathf.Round(12f * ux);
            PoliSimWidgets.MeasuredLabel(new Rect(textLeft, y, Mathf.Max(1f, r.xMax - textLeft), captionHeight),
                string.Format(CultureInfo.InvariantCulture,
                    "CAMPAIGN DAY {0} OF {1} · OPENED {2} · POLLING DAY {3}",
                    s.CampaignDay, total,
                    s.Calendar.CampaignStart.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                    s.Calendar.ElectionDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()),
                caption);

            PoliSimWidgets.MeasuredLabel(
                new Rect(r.x, y + captionHeight + Mathf.Round(6f * uy), r.width, captionHeight),
                string.Format(CultureInfo.InvariantCulture, "TODAY {0} · EVERY FIGURE ON THIS SHEET IS DERIVED",
                    s.Today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()),
                DeskCaption(8.5f, PoliSimTheme.TextMuted));
        }

        // ------------------------------------------------------------------------------------------
        // The ledger primitives, matching DrawDeskApprovalLedger's head and row exactly.
        // ------------------------------------------------------------------------------------------
        private float DrawCampaignLedgerHead(Rect r, string text, float ux, float uy)
        {
            GUIStyle header = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float headerHeight = DeskCaptionHeight(header);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, headerHeight), text, header);

            float y = r.y + headerHeight + Mathf.Round(3f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            return y + Mathf.Round(4f * uy);
        }

        /// <summary>One ledger row: a name at the left, its figure at the right, on one rect.</summary>
        private static void DrawCampaignRow(Rect rect, string name, string figure, GUIStyle nameStyle, GUIStyle figureStyle)
        {
            float figureWidth = Mathf.Ceil(figureStyle.CalcSize(new GUIContent(figure)).x) + 6f;
            PoliSimWidgets.MeasuredLabel(
                new Rect(rect.x, rect.y, Mathf.Max(1f, rect.width - figureWidth - 8f), rect.height), name, nameStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(rect.xMax - figureWidth, rect.y, figureWidth, rect.height),
                figure, figureStyle);
        }

        /// <summary>An empty state: the sentence that says what is absent, never a zero pretending to be data (1m-r2).</summary>
        private static void DrawCampaignEmptyRow(Rect rect, string text, GUIStyle nameStyle)
        {
            var style = new GUIStyle(nameStyle);
            style.normal.textColor = PoliSimTheme.TextMuted;
            style.hover.textColor = PoliSimTheme.TextMuted;
            style.active.textColor = PoliSimTheme.TextMuted;
            style.focused.textColor = PoliSimTheme.TextMuted;
            PoliSimWidgets.MeasuredLabel(rect, text, style);
        }

        /// <summary>
        /// A krona amount with thousands separators, invariant.
        ///
        /// **Not `UiFormat.Number`**, which is a plain `F0` and filmed the war chest as `1120000` —
        /// a seven-digit money hero with no separators is illegible at a glance, which is the one
        /// job a hero numeral has. **Not `UiFormat.Money`** either: that is dollar-prefixed and
        /// tiered (`$1.12M`), and this sheet is denominated in kronor at full precision because a
        /// campaign budget is spent in units a treasurer would recognise, not in rounded millions.
        /// Invariant, like every other numeric site here (B3's defect class).
        /// </summary>
        private static string Kronor(double amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>"TelevisionAd" -> "Television ad", so an enum reads as a verb rather than an identifier.</summary>
        private static string SpacedIdentifier(string pascal)
        {
            if (string.IsNullOrEmpty(pascal)) { return pascal; }

            var sb = new System.Text.StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                if (i > 0 && char.IsUpper(pascal[i]))
                {
                    sb.Append(' ');
                    sb.Append(char.ToLowerInvariant(pascal[i]));
                }
                else
                {
                    sb.Append(pascal[i]);
                }
            }

            return sb.ToString();
        }
    }
}
