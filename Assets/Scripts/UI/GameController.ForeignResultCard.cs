using System.Collections.Generic;
using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-8 (K163): THE RESULTS CARD OF AN ELECTION ABROAD - each list a bar in its party's laddered ink by its seats, the seats and their change, the share and its
    /// swing, then who governs. Its own file of the partial class so the party ink is drawn where party identity is the whole subject and no area accent is
    /// drawn beside it (`PartyInkDrawSiteCheck`, clause 2 by construction): the Docket item that opens it, with its area stripe, is `GameController.WorldCalendar.cs`.
    /// </summary>
    public partial class GameController
    {
        /// <summary>K163: the compact results card - a row per list (its ink, its short name, a bar by its seats of the chamber, the seats and their change, the share
        /// and its swing where both are on record), then who governs.</summary>
        private void DrawForeignResultCard(WorldCalendar.Result r)
        {
            V35.FloorGuarded = true;
            bool repaint = Event.current.type == EventType.Repaint;
            GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle figureFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: false, TextAnchor.MiddleRight);
            GUIStyle mutedFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted, bold: false, TextAnchor.MiddleRight);
            float rowH = V35.Px(24f);
            float most = 1f;
            foreach (WorldCalendar.Row row in r.Rows) { most = Mathf.Max(most, row.Seats); }
            foreach (WorldCalendar.Row row in r.Rows)
            {
                Rect line = GUILayoutUtility.GetRect(10f, rowH, GUILayout.ExpandWidth(true), GUILayout.Height(rowH));
                if (!repaint) { continue; }
                float nameW = V35.Px(96f), seatsW = V35.Px(110f), shareW = V35.Px(150f);
                var nameRect = new Rect(line.x, line.y, nameW, line.height);
                var shareRect = new Rect(line.xMax - shareW, line.y, shareW, line.height);
                var seatsRect = new Rect(shareRect.x - seatsW, line.y, seatsW, line.height);
                float barX = nameRect.xMax + V35.Px(8f), barMax = Mathf.Max(1f, seatsRect.x - V35.Px(10f) - barX);
                Color ink = PoliSimTheme.HasPartyInk(r.Country, row.Key) ? PoliSimTheme.PartyLaddered(r.Country, row.Key) : PoliSimTheme.Neutral;
                PoliSimTheme.Rule(new Rect(line.x, line.y + V35.Px(4f), V35.Px(4f), line.height - V35.Px(8f)), ink);
                PoliSimWidgets.MeasuredLabel(new Rect(nameRect.x + V35.Px(10f), nameRect.y, nameRect.width - V35.Px(10f), nameRect.height), V35Fit(row.Name, nameFace, nameRect.width - V35.Px(10f), out _), nameFace);
                PoliSimTheme.Rule(new Rect(barX, line.y + V35.Px(6f), Mathf.Max(1f, barMax * row.Seats / most), line.height - V35.Px(12f)), ink);
                int change = row.Seats - row.SeatsBefore;
                string seats = row.Seats.ToString(CultureInfo.InvariantCulture) + (change == 0 ? "  ±0" : change > 0 ? "  +" + change.ToString(CultureInfo.InvariantCulture) : "  −" + (-change).ToString(CultureInfo.InvariantCulture));
                PoliSimWidgets.MeasuredLabel(seatsRect, seats, figureFace);
                string share = double.IsNaN(row.Share) ? string.Empty : (100.0 * row.Share).ToString("0.0", CultureInfo.InvariantCulture) + " %";
                if (!double.IsNaN(row.Share) && !double.IsNaN(row.ShareBefore))
                {
                    double swing = 100.0 * (row.Share - row.ShareBefore);
                    share += "  " + (swing >= 0.0 ? "+" : "−") + System.Math.Abs(swing).ToString("0.0", CultureInfo.InvariantCulture);
                }
                PoliSimWidgets.MeasuredLabel(shareRect, share, mutedFace);
            }
            float footH = V35.Px(40f);
            Rect foot = GUILayoutUtility.GetRect(10f, footH, GUILayout.ExpandWidth(true), GUILayout.Height(footH));
            if (repaint)
            {
                GUIStyle governsFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                string seatsLine = r.TotalSeats > 0 ? r.TotalSeats.ToString(CultureInfo.InvariantCulture) + " of " + r.RowsAre + " · change against the chamber before · share and swing in points" : string.Empty;
                PoliSimWidgets.MeasuredLabel(new Rect(foot.x, foot.y, foot.width, footH * 0.5f), V35Fit(seatsLine, mutedFace, foot.width, out _), V35Mono(V35.Floor, PoliSimTheme.TextMuted));
                PoliSimWidgets.MeasuredLabel(new Rect(foot.x, foot.y + footH * 0.5f, foot.width, footH * 0.5f), V35Fit(r.Governs, governsFace, foot.width, out _), governsFace);
            }
            V35.FloorGuarded = false;
        }
    }
}
