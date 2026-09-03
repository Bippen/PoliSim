using System.Collections.Generic;
using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E2 — the campaign map: the 29 valkretsar as a cartogram of support AS POLLED, with §25's
    /// swing index, and §36's gate drawn as ABSENCE. The fourth screen of the Track E class,
    /// composed on the same board as its three siblings (`GameController.Campaign.cs`), reachable
    /// only through `SetCampaignMapScreen` from the screenshot harness (R-N2).
    ///
    /// **What the sheet says, and only what it can say.** A valkrets the player has bought regional
    /// detail for carries its polled shares with the ± its sample earned; its tile is shaded by the
    /// player's own polled share there, framed BOLD when the race is a swing region (index ≥ 60)
    /// and DASHED when the lead is inside its own sampling error (too close to call). A valkrets
    /// with no regional detail carries NOTHING — the tile is hatched with the draft hatch and
    /// figured "?" — because §36 says the map must not tell the player where the race is close
    /// until they have paid to find out, and a blurred or averaged reading would be telling them
    /// anyway. The right column ranks the bought valkretsar by index, or, when nothing is bought,
    /// says what the two offers that would sharpen the sheet cost and buy (W-E4's ladder, the
    /// same `MarginOfErrorPp`).
    ///
    /// **No geography is claimed.** The grid is `SwedenCartogram.Layout` — a hand-laid reading
    /// aid, north at the top, no borders, no sprite invented. A drawn map of the valkretsar is a
    /// line for the Track H Design ask.
    /// </summary>
    public partial class GameController
    {
        private CampaignMapSnapshot? _campaignMapScreen;
        private Rect _campaignMapInnerRect;

        /// <summary>[AUTHORED-DRAFT] the index at which a valkrets is framed as a swing region (§25's "regions where small changes can determine the result").</summary>
        private const double CampaignMapSwingFrameIndex = 60.0;

        internal void SetCampaignMapScreen(CampaignMapSnapshot? snapshot)
        {
            _campaignMapScreen = snapshot;
        }

        private void DrawCampaignMapStage(float availableHeight, float availableWidth, CampaignMapSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth),
                GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignMapInnerRect = inner; }
            else if (_campaignMapInnerRect.width > 1f) { inner = _campaignMapInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot.Campaign, "CAMPAIGN · THE MAP");

            DrawCampaignMapCartogram(Board(0f, 36f, 703f, 576f), snapshot);
            DrawCampaignMapLedger(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCampaignStrip(Board(0f, 627f, 1156f, 53f), snapshot.Campaign);
        }

        // ------------------------------------------------------------------------------------------
        // Left: the cartogram - 29 tiles, shaded by what was polled, hatched where nothing was.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignMapCartogram(Rect r, CampaignMapSnapshot s)
        {
            float ux = r.width / 703f;
            float uy = r.height / 576f;
            string head = s.MeasuredCount == 0
                ? "THE 29 VALKRETSAR — NO REGIONAL DETAIL BOUGHT"
                : $"THE 29 VALKRETSAR — SUPPORT AS POLLED, {s.MeasuredCount} OF 29 READ";
            float y = DrawCampaignLedgerHead(r, head, ux, uy);

            // The key is measured BEFORE the grid is budgeted (the Desk's rule for wrapped captions).
            GUIStyle key = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string keyText = "SHADE = YOUR POLLED SHARE, DARKER IS HIGHER · BOLD FRAME = SWING REGION (INDEX 60 OR MORE) · " +
                             "DASHED FRAME = THE LEAD IS INSIDE ITS OWN ± · HATCHED = NOT POLLED, NOTHING IS KNOWN HERE";
            float keyHeight = Mathf.Ceil(key.CalcHeight(new GUIContent(keyText), r.width));
            var keyRect = new Rect(r.x, r.yMax - keyHeight, r.width, keyHeight);

            float gridTop = y + Mathf.Round(4f * uy);
            float gridHeight = Mathf.Max(1f, keyRect.y - Mathf.Round(8f * uy) - gridTop);
            float gap = Mathf.Round(4f * ux);
            float tileW = Mathf.Floor((r.width - gap * (SwedenCartogram.Columns - 1)) / SwedenCartogram.Columns);
            float tileH = Mathf.Floor((gridHeight - gap * (SwedenCartogram.Rows - 1)) / SwedenCartogram.Rows);

            // The shade's scale: the player's largest polled share among the measured tiles, so the
            // darkest tile is where the party is strongest and the rest read against it.
            double topShare = 0.0;
            foreach (MapRegionReading reading in s.Regions)
            {
                if (reading.Measured && reading.Poll.Share(s.PlayerPartyIndex) > topShare) { topShare = reading.Poll.Share(s.PlayerPartyIndex); }
            }

            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextSecondary);
            GUIStyle captionOnInk = DeskCaption(8f, PoliSimTheme.TextPrimary);
            GUIStyle figure = DeskCaption(9f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.LowerRight);
            GUIStyle unknown = DeskCaption(11f, PoliSimTheme.TextMuted, bold: true, anchor: TextAnchor.LowerRight);
            Texture2D hatch = IconLibrary.GetChrome("ui_hatch_draft");
            Color political = UiPalette.GetAreaColor(UiPalette.SystemArea.Political);

            var byName = new Dictionary<string, int>();
            for (int i = 0; i < s.Regions.Length; i++) { byName[s.Regions[i].Name] = i; }

            foreach (MapTile tile in s.Layout)
            {
                if (!byName.TryGetValue(tile.Name, out int index)) { continue; }
                MapRegionReading reading = s.Regions[index];
                var rect = new Rect(r.x + tile.Column * (tileW + gap), gridTop + tile.Row * (tileH + gap), tileW, tileH);

                if (Event.current.type == EventType.Repaint)
                {
                    if (reading.Measured)
                    {
                        float strength = topShare > 0 ? Mathf.Clamp01((float)(reading.Poll.Share(s.PlayerPartyIndex) / topShare)) : 0f;
                        Color fill = PoliSimTheme.Tint(political, 0.12f + 0.55f * strength);
                        bool swing = reading.SwingIndex >= CampaignMapSwingFrameIndex;
                        PoliSimTheme.RoundedCard(rect, fill, swing ? PoliSimTheme.HairlineStrong : PoliSimTheme.Hairline, 0f);
                        if (swing)
                        {
                            // A second, inset frame so "bold" reads as bold at every size, not as a 1 px hairline.
                            PoliSimTheme.RoundedCard(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f),
                                Color.clear, PoliSimTheme.HairlineStrong, 0f);
                        }

                        if (reading.TooCloseToCall)
                        {
                            DeskDashedFrame(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), PoliSimTheme.Caution, 3f, 2f);
                        }
                    }
                    else
                    {
                        PoliSimTheme.RoundedCard(rect, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                        var hatchRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
                        if (hatch != null)
                        {
                            GUI.color = new Color(1f, 1f, 1f, 0.55f);
                            GUI.DrawTextureWithTexCoords(hatchRect, hatch, new Rect(0f, 0f, hatchRect.width / hatch.width, hatchRect.height / hatch.height));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUI.color = PoliSimTheme.Tint(PoliSimTheme.TextMuted, 0.15f);
                            GUI.DrawTexture(hatchRect, Texture2D.whiteTexture);
                            GUI.color = Color.white;
                        }
                    }
                }

                float pad = Mathf.Round(3f * ux);
                float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
                PoliSimWidgets.MeasuredLabel(new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, captionHeight),
                    tile.Caption, reading.Measured ? captionOnInk : caption);

                float figureHeight = Mathf.Ceil(DeskCaptionHeight(reading.Measured ? figure : unknown));
                var figureRect = new Rect(rect.x + pad, rect.yMax - pad - figureHeight, rect.width - pad * 2f, figureHeight);
                if (reading.Measured)
                {
                    PoliSimWidgets.MeasuredLabel(figureRect,
                        string.Format(CultureInfo.InvariantCulture, "{0:F0} ±{1:F0}",
                            100.0 * reading.Poll.Share(s.PlayerPartyIndex), reading.Poll.MarginOfErrorPp(s.PlayerPartyIndex)),
                        figure);
                }
                else
                {
                    PoliSimWidgets.MeasuredLabel(figureRect, "?", unknown);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Campaign map key", keyRect, r);
            }

            GUI.Label(keyRect, keyText, key);
        }

        // ------------------------------------------------------------------------------------------
        // Right: the swing ledger, or the gate.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignMapLedger(Rect r, CampaignMapSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, s.MeasuredCount == 0 ? "SWING REGIONS — UNKNOWN" : "SWING REGIONS — BY INDEX, AS POLLED", ux, uy);

            GUIStyle method = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string methodText = s.MeasuredCount == 0
                ? s.OfferLine
                : string.Format(CultureInfo.InvariantCulture,
                    "{0} · n = {1:N0} PER VALKRETS · FIELDED {2} · THE ± IS SAMPLING ERROR ONLY · INDEX = 100 AT A TIE, 0 AT A {3:F0}-POINT LEAD · " +
                    "A LEAD INSIDE ITS ± CANNOT SAY WHO IS AHEAD",
                    s.PollingBought.ToUpperInvariant(), s.SamplePerRegion,
                    s.FieldDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(), SwingRegions.FullScaleGapPp);
            float methodHeight = Mathf.Ceil(method.CalcHeight(new GUIContent(methodText), r.width));
            var methodRect = new Rect(r.x, r.yMax - methodHeight, r.width, methodHeight);

            if (s.MeasuredCount == 0)
            {
                GUIStyle gate = DeskCaptionWrapped(9f, PoliSimTheme.TextSecondary);
                string gateText = "A NATIONAL POLL CANNOT SAY WHERE THE RACE IS CLOSE. UNTIL REGIONAL DETAIL IS BOUGHT, EVERY VALKRETS ON THE " +
                                  "MAP READS AS UNKNOWN AND THIS LEDGER IS EMPTY — THE SHEET DOES NOT GUESS FOR YOU.";
                float gateHeight = Mathf.Ceil(gate.CalcHeight(new GUIContent(gateText), r.width));
                var gateRect = new Rect(r.x, y + Mathf.Round(4f * uy), r.width, gateHeight);
                if (Event.current.type == EventType.Repaint) { UiContainmentGuard.Check("Campaign map gate", gateRect, r); }
                GUI.Label(gateRect, gateText, gate);
            }
            else
            {
                GUIStyle nameStyle = DeskBody(12f, PoliSimTheme.TextPrimary);
                GUIStyle detail = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
                GUIStyle indexStyle = DeskCaption(9f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
                float nameHeight = Mathf.Max(Mathf.Round(15f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);
                float detailHeight = Mathf.Ceil(DeskCaptionHeight(detail));
                float pitch = nameHeight + detailHeight + Mathf.Round(4f * uy);

                List<int> order = s.BySwing();
                int room = Mathf.Max(0, Mathf.FloorToInt((methodRect.y - Mathf.Round(6f * uy) - y) / pitch));
                int shown = Mathf.Min(order.Count, room);
                float indexWidth = Mathf.Ceil(indexStyle.CalcSize(new GUIContent("INDEX 100")).x) + Mathf.Round(4f * ux);

                for (int k = 0; k < shown; k++)
                {
                    MapRegionReading reading = s.Regions[order[k]];
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, Mathf.Max(1f, r.width - indexWidth - Mathf.Round(6f * ux)), nameHeight),
                        reading.Name, nameStyle);
                    PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - indexWidth, y, indexWidth, nameHeight),
                        string.Format(CultureInfo.InvariantCulture, "INDEX {0:F0}", reading.SwingIndex), indexStyle);

                    string leader = reading.Leader >= 0 ? s.PartyNames[reading.Leader] : "—";
                    string runner = reading.RunnerUp >= 0 ? s.PartyNames[reading.RunnerUp] : "—";
                    string detailText = reading.TooCloseToCall
                        ? string.Format(CultureInfo.InvariantCulture, "{0} {1:F1} v {2} {3:F1} · LEAD {4:F1} INSIDE ITS ± {5:F1} — TOO CLOSE TO CALL",
                            leader, 100.0 * reading.Poll.Share(reading.Leader), runner, 100.0 * reading.Poll.Share(reading.RunnerUp),
                            reading.GapPp, reading.GapErrorPp)
                        : string.Format(CultureInfo.InvariantCulture, "{0} {1:F1} v {2} {3:F1} · LEAD {4:F1} ± {5:F1}",
                            leader, 100.0 * reading.Poll.Share(reading.Leader), runner, 100.0 * reading.Poll.Share(reading.RunnerUp),
                            reading.GapPp, reading.GapErrorPp);
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x, y + nameHeight, r.width, detailHeight), detailText,
                        reading.TooCloseToCall ? DeskCaption(8.5f, PoliSimTheme.Caution) : detail);
                    y += pitch;
                }

                if (shown < order.Count)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, Mathf.Round(14f * uy)),
                        $"+{order.Count - shown} MORE VALKRETSAR POLLED, LOWER INDEX", DeskCaption(8.5f, PoliSimTheme.TextMuted));
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Campaign map methodology", methodRect, r);
            }

            GUI.Label(methodRect, methodText, method);
        }
    }
}
