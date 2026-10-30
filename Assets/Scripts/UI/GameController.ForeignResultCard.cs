using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-8 (K163), S6 (Design's return of 2026-10-30, `send/s6_return/RETURN.md`, board `S6 Foreign Election.dc.html`; K320): <b>THE COMPACT CARD OF AN ELECTION
    /// ABROAD</b>, replacing the stand-in - three panes divided by rules: the count (the chamber and its seats, the seats as one bar in the party inks with the
    /// majority tick, the parties two to a row: mark, code, seats, change ▲/▼, share), who governs (the state's stamp, the cabinet's marks, its standing and
    /// seats, the formation as the night's step cells with the days it took, one fragment on how it ended), and the dates. Every figure is read off the engine's
    /// records by <see cref="ForeignElectionCard"/>; the stand-in's captions and each row's swing are the panes' and the rows' slips. Also the entry's line of
    /// figures (<see cref="DrawForeignSeatsLine"/>). Its own file of the partial class so the party ink is drawn where party identity is the whole subject and
    /// no area accent is drawn beside it (`PartyInkDrawSiteCheck`, clause 2 by construction): the Docket item that opens it, with its area stripe and the card's
    /// 4 px bar, is `GameController.WorldCalendar.cs`.
    /// </summary>
    public partial class GameController
    {
        /// <summary>A list's ink - its laddered party ink where the game holds one, the neutral slate where it does not.</summary>
        private static Color ForeignInk(CountryId country, string key) => PoliSimTheme.HasPartyInk(country, key) ? PoliSimTheme.PartyLaddered(country, key) : PoliSimTheme.Neutral;

        /// <summary>S6 (RETURN.md, "The Docket entry"): the line under the caption - per list a 12 px ink square, its code in the bold mono, its seats and the change
        /// ▲/▼ in GOOD/BAD - the lists that fit the line whole; the rest are the card's and the entry's slip's.</summary>
        private void DrawForeignSeatsLine(Rect line, ForeignElectionCard card)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            WorldCalendar.Result r = card.Result;
            GUIStyle code = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle figure = V35Mono(V35.Floor, PoliSimTheme.TextPrimary);
            float square = V35.Px(12f), gap = V35.Px(5f), between = V35.Px(12f);
            float x = line.x;
            foreach (WorldCalendar.Row row in r.Rows)
            {
                int change = row.Seats - row.SeatsBefore;
                string seats = row.Seats.ToString(CultureInfo.InvariantCulture);
                string arrow = ForeignElectionCard.Arrowed(change);
                GUIStyle arrowFace = V35Mono(V35.Floor, change > 0 ? PoliSimTheme.Good : change < 0 ? PoliSimTheme.Bad : PoliSimTheme.TextMuted, bold: true);
                float cw = Mathf.Ceil(code.CalcSize(new GUIContent(row.Name)).x), sw = Mathf.Ceil(figure.CalcSize(new GUIContent(seats)).x), aw = Mathf.Ceil(arrowFace.CalcSize(new GUIContent(arrow)).x);
                float need = square + gap + cw + gap + sw + gap + aw;
                if (x + need > line.xMax) { break; }   // a list that does not fit is left whole to the card, never cut
                PoliSimTheme.Rule(new Rect(x, line.y + Mathf.Round((line.height - square) * 0.5f), square, square), ForeignInk(r.Country, row.Key));
                x += square + gap;
                PoliSimWidgets.MeasuredLabel(new Rect(x, line.y, cw, line.height), row.Name, code);
                x += cw + gap;
                PoliSimWidgets.MeasuredLabel(new Rect(x, line.y, sw, line.height), seats, figure);
                x += sw + gap;
                PoliSimWidgets.MeasuredLabel(new Rect(x, line.y, aw, line.height), arrow, arrowFace);
                x += aw + between;
            }
        }

        /// <summary>S6: the compact card's three panes in the docket body's flow (<see cref="DrawForeignCardFrame"/> holds the frame); each pane's and each row's slip
        /// on the Docket's book under <paramref name="key"/>.</summary>
        private void DrawForeignResultCard(ForeignElectionCard card, string key)
        {
            V35.FloorGuarded = true;
            WorldCalendar.Result r = card.Result;
            bool repaint = Event.current.type == EventType.Repaint;
            float gap = V35.Px(18f), headH = V35.Px(22f), inner = V35.Px(6f);
            float rowH = V35.Px(22f), rowGap = V35.Px(2f);
            int pairs = (r.Rows.Count + 1) / 2;
            float countH = headH + inner + V35.Px(18f) + V35.Px(4f) + V35.Px(16f) + inner + pairs * rowH + Mathf.Max(0, pairs - 1) * rowGap;
            float govH = headH + inner + V35.Px(28f) + inner + (card.StepLabel != null ? V35.Px(26f) + inner : 0f) + V35.Px(20f);
            float datesH = card.Dates.Count * (V35.Px(24f) + V35.Px(4f));
            float h = Mathf.Max(countH, Mathf.Max(govH, datesH));
            Rect area = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));

            // the panes' widths (RETURN.md: 460 · 340 · the rest), narrowed together where the dates would be squeezed under their floor
            float countW = V35.Px(460f), govW = V35.Px(340f), datesFloor = V35.Px(170f);
            float fixedGaps = 2f * (gap * 2f + 1f);
            if (area.width - fixedGaps - countW - govW < datesFloor)
            {
                float f = Mathf.Max(0.5f, (area.width - fixedGaps - datesFloor) / (countW + govW));
                countW = Mathf.Floor(countW * f);
                govW = Mathf.Floor(govW * f);
            }
            var count = new Rect(area.x, area.y, countW, h);
            var gov = new Rect(count.xMax + gap * 2f + 1f, area.y, govW, h);
            var dates = new Rect(gov.xMax + gap * 2f + 1f, area.y, Mathf.Max(1f, area.xMax - gov.xMax - gap * 2f - 1f), h);
            if (repaint)
            {
                PoliSimTheme.Rule(new Rect(count.xMax + gap, area.y, 1f, h), PoliSimTheme.RuleRow);
                PoliSimTheme.Rule(new Rect(gov.xMax + gap, area.y, 1f, h), PoliSimTheme.RuleRow);
            }
            DrawForeignCount(count, card, key);
            DrawForeignGoverns(gov, card, key);
            DrawForeignDates(dates, card);
            V35.FloorGuarded = false;
        }

        /// <summary>The count: the chamber and "N · seats", the seats as one bar in the party inks (1 px apart) with the majority tick and its figure, the lists two to
        /// a row. An election ahead draws the lane empty inside a dashed rule and its seats as "—".</summary>
        private void DrawForeignCount(Rect pane, ForeignElectionCard card, string key)
        {
            WorldCalendar.Result r = card.Result;
            bool repaint = Event.current.type == EventType.Repaint;
            float headH = V35.Px(22f), inner = V35.Px(6f), barH = V35.Px(18f);
            // §962: the tick's figure is as tall as its face's line, not the board's 16 (the mono at the floor needs 17.5 at 1280 - the film's OVERFLOW)
            float tickLabelH = Mathf.Max(V35.Px(16f), Mathf.Ceil(V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: false, TextAnchor.MiddleCenter).CalcSize(new GUIContent("0")).y));
            GUIStyle head = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle muted = V35Mono(V35.Floor, PoliSimTheme.TextMuted, bold: false, TextAnchor.MiddleRight);
            string of = card.Total.ToString(CultureInfo.InvariantCulture) + " · seats";
            float ofW = Mathf.Ceil(muted.CalcSize(new GUIContent(of)).x);
            if (repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, pane.y, Mathf.Max(1f, pane.width - ofW - V35.Px(8f)), headH), card.ChamberHead, head);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.xMax - ofW, pane.y, ofW, headH), of, muted);
            }
            _docketSlipBook.Anchors["docket:abroadcount:" + key] = card.CountSlip;
            SlipAnchor(new Rect(pane.x, pane.y, pane.width, headH + inner + barH + V35.Px(4f) + tickLabelH), "docket:abroadcount:" + key);

            var bar = new Rect(pane.x, pane.y + headH + inner, pane.width, barH);
            float tickX = card.Total > 0 ? Mathf.Round(bar.x + bar.width * card.Majority / card.Total) - 1f : bar.x;
            if (repaint)
            {
                if (card.Counted && card.Total > 0)
                {
                    float x = bar.x;
                    foreach (WorldCalendar.Row row in r.Rows)
                    {
                        float w = bar.width * row.Seats / card.Total;
                        if (Mathf.Round(w) - 1f > 0f) { PoliSimTheme.Rule(new Rect(Mathf.Round(x), bar.y, Mathf.Round(w) - 1f, bar.height), ForeignInk(r.Country, row.Key)); }
                        x += w;
                    }
                }
                else { DashedRect(bar, PoliSimTheme.Hairline); }
                if (card.Total > 0)
                {
                    PoliSimTheme.Rule(new Rect(tickX, bar.y - V35.Px(4f), 2f, bar.height + V35.Px(8f)), PoliSimTheme.TextPrimary);
                    GUIStyle tickFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: false, TextAnchor.MiddleCenter);
                    float lw = V35.Px(60f);
                    float lx = Mathf.Clamp(tickX + 1f - lw * 0.5f, pane.x, pane.xMax - lw);
                    PoliSimWidgets.MeasuredLabel(new Rect(lx, bar.yMax + V35.Px(4f), lw, tickLabelH), card.Majority.ToString(CultureInfo.InvariantCulture), tickFace);
                }
            }

            // the lists, two to a row: mark 14 · code · seats · change · share
            float rowH = V35.Px(22f), rowGap = V35.Px(2f), colGap = V35.Px(18f), cell = V35.Px(7f);
            float colW = Mathf.Floor((pane.width - colGap) * 0.5f);
            GUIStyle code = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle seatsFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            GUIStyle shareFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            float codeW = V35.Px(62f);
            foreach (WorldCalendar.Row row in r.Rows) { codeW = Mathf.Max(codeW, Mathf.Ceil(code.CalcSize(new GUIContent(row.Name)).x)); }
            float seatsW = Mathf.Max(V35.Px(34f), Mathf.Ceil(seatsFace.CalcSize(new GUIContent("000")).x));
            float changeW = Mathf.Max(V35.Px(40f), Mathf.Ceil(seatsFace.CalcSize(new GUIContent("▲00")).x));
            float markSide = V35.Px(14f);
            float top = bar.yMax + V35.Px(4f) + tickLabelH + inner;
            for (int i = 0; i < r.Rows.Count; i++)
            {
                WorldCalendar.Row row = r.Rows[i];
                var line = new Rect(pane.x + (i % 2) * (colW + colGap), top + (i / 2) * (rowH + rowGap), colW, rowH);
                float x = line.x;
                DrawPartyMarkSlot(new Rect(x, line.y + Mathf.Round((rowH - markSide) * 0.5f), markSide, markSide), r.Country, row.Key);
                x += markSide + cell;
                int change = row.Seats - row.SeatsBefore;
                string share = card.Counted && !double.IsNaN(row.Share) ? (100.0 * row.Share).ToString("0.0", CultureInfo.InvariantCulture) + "%" : card.Counted ? "—" : string.Empty;
                if (repaint)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(x, line.y, Mathf.Min(codeW, Mathf.Max(1f, line.xMax - x)), rowH), row.Name, code);
                    float sx = x + codeW + cell;
                    PoliSimWidgets.MeasuredLabel(new Rect(sx, line.y, seatsW, rowH), card.Counted ? row.Seats.ToString(CultureInfo.InvariantCulture) : "—", seatsFace);
                    if (card.Counted)
                    {
                        GUIStyle arrowFace = V35Mono(V35.Floor, change > 0 ? PoliSimTheme.Good : change < 0 ? PoliSimTheme.Bad : PoliSimTheme.TextMuted, bold: true, TextAnchor.MiddleRight);
                        PoliSimWidgets.MeasuredLabel(new Rect(sx + seatsW, line.y, changeW, rowH), ForeignElectionCard.Arrowed(change), arrowFace);
                    }
                    // the share where it fits its column whole; else it stays on the row's slip alone (K320: never cut)
                    float shx = sx + seatsW + changeW + cell, shw = Mathf.Ceil(shareFace.CalcSize(new GUIContent(share)).x);
                    bool shareFits = share.Length > 0 && shx + shw <= line.xMax;
                    if (shareFits) { PoliSimWidgets.MeasuredLabel(new Rect(shx, line.y, shw, rowH), share, shareFace); }
                    UiContainmentGuard.Check("Foreign result row", new Rect(line.x, line.y, (shareFits ? shx + shw : sx + seatsW + changeW) - line.x, rowH), line);
                }
                if (card.RowSlips.TryGetValue(row.Key, out SlipContent slip))
                {
                    string id = "docket:abroadrow:" + key + ":" + row.Key;
                    _docketSlipBook.Anchors[id] = slip;
                    SlipAnchor(line, id);
                }
            }
        }

        /// <summary>Who governs: the head and the state's stamp, the cabinet's marks with its standing and seats, the step cells with their label and figure, how it
        /// ended.</summary>
        private void DrawForeignGoverns(Rect pane, ForeignElectionCard card, string key)
        {
            WorldCalendar.Result r = card.Result;
            bool repaint = Event.current.type == EventType.Repaint;
            float headH = V35.Px(22f), inner = V35.Px(6f);
            float y = pane.y;
            Color stampInk = card.Is == ForeignElectionCard.State.Forming || card.Is == ForeignElectionCard.State.Caretaker ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary;
            float stampW = DrawFlatStamp(pane.xMax, y + V35.Px(2f), V35.Px(18f), card.Stamp, stampInk, dashed: card.Is == ForeignElectionCard.State.Upcoming);
            if (repaint) { PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, Mathf.Max(1f, pane.width - stampW - V35.Px(8f)), headH), "Who governs", V35Serif(V35.Name, PoliSimTheme.TextPrimary)); }
            _docketSlipBook.Anchors["docket:abroadgov:" + key] = card.GovernsSlip;
            SlipAnchor(pane, "docket:abroadgov:" + key);
            y += headH + inner;

            // the cabinet: its marks, its standing, its seats
            float cabH = V35.Px(28f), mark = V35.Px(16f), markGap = V35.Px(3f);
            float x = pane.x;
            int shown = 0;
            foreach (string party in card.CabinetParties)
            {
                if (shown >= 5) { break; }   // the rest are named on the slip's cabinet line
                DrawPartyMarkSlot(new Rect(x, y + Mathf.Round((cabH - mark) * 0.5f), mark, mark), r.Country, party);
                x += mark + markGap;
                shown++;
            }
            if (shown > 0) { x += V35.Px(5f); }
            GUIStyle figure = V35Mono(V35.Name, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            string seats = card.CabinetSeats >= 0 ? card.CabinetSeats.ToString(CultureInfo.InvariantCulture) : string.Empty;
            float fw = seats.Length > 0 ? Mathf.Ceil(figure.CalcSize(new GUIContent(seats)).x) : 0f;
            if (repaint)
            {
                GUIStyle standing = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
                float sw = Mathf.Max(1f, pane.xMax - fw - V35.Px(8f) - x);
                PoliSimWidgets.MeasuredLabel(new Rect(x, y, sw, cabH), card.Standing, standing);
                if (fw > 0f) { PoliSimWidgets.MeasuredLabel(new Rect(pane.xMax - fw, y, fw, cabH), seats, figure); }
            }
            y += cabH + inner;

            // the formation as the night's step cells (22 px, 4 apart), the days it took
            if (card.StepLabel != null)
            {
                float stepH = V35.Px(26f), side = V35.Px(22f), between = V35.Px(4f);
                GUIStyle label = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
                GUIStyle fig = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
                float lw = Mathf.Ceil(label.CalcSize(new GUIContent(card.StepLabel)).x);
                string stepFigure = card.StepFigure ?? string.Empty;
                float figW = stepFigure.Length > 0 ? Mathf.Ceil(fig.CalcSize(new GUIContent(stepFigure)).x) : 0f;
                if (repaint)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, lw, stepH), card.StepLabel, label);
                    if (figW > 0f) { PoliSimWidgets.MeasuredLabel(new Rect(pane.xMax - figW, y, figW, stepH), stepFigure, fig); }
                }
                float cx = pane.x + lw + V35.Px(8f), cy = y + Mathf.Round((stepH - side) * 0.5f);
                if (card.StepsAbsent)
                {
                    float g = V35.Px(16f);
                    if (repaint) { DrawStateGlyph(new Rect(cx, y + Mathf.Round((stepH - g) * 0.5f), g, g), Symbol.Absent, PoliSimTheme.TextMuted); }
                }
                else
                {
                    foreach (GovernmentFormation.RoundCell c in card.Cells)
                    {
                        DrawForeignStepCell(new Rect(cx, cy, side, side), r.Country, c);
                        cx += side + between;
                    }
                    if (repaint) { UiContainmentGuard.Check("Foreign result steps", new Rect(pane.x, y, cx - pane.x, stepH), new Rect(pane.x, y, Mathf.Max(1f, pane.width - figW - V35.Px(6f)), stepH)); }
                }
                y += stepH + inner;
            }

            // one fragment on how it ended
            if (repaint && !string.IsNullOrEmpty(card.Ended))
            {
                GUIStyle ended = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, V35.Px(20f)), card.Ended, ended);
                UiContainmentGuard.Check("Foreign result ended", new Rect(pane.x, y, Mathf.Ceil(ended.CalcSize(new GUIContent(card.Ended)).x), V35.Px(20f)), pane);
            }
        }

        /// <summary>One step cell as the night draws it: the current step ruled in ink with the asked party's mark, a refused step ruled and struck in BAD (its mark
        /// faded), the last refusal ruled in BAD, a step not reached dashed.</summary>
        private void DrawForeignStepCell(Rect r, CountryId country, GovernmentFormation.RoundCell c)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            switch (c.State)
            {
                case GovernmentFormation.RoundCellState.Current: Outline(r, PoliSimTheme.TextPrimary, 1.5f); break;
                case GovernmentFormation.RoundCellState.LastRefused: Outline(r, PoliSimTheme.Bad, 1.5f); break;
                case GovernmentFormation.RoundCellState.Refused: Outline(r, PoliSimTheme.HairlineStrong, 1.5f); break;
                default: DashedRect(r, PoliSimTheme.Hairline); break;
            }
            if (c.Asked != null && c.State != GovernmentFormation.RoundCellState.NotReached)
            {
                float m = V35.Px(14f);
                Color before = GUI.color;
                if (c.State != GovernmentFormation.RoundCellState.Current) { GUI.color = new Color(before.r, before.g, before.b, before.a * 0.45f); }
                DrawPartyMarkSlot(new Rect(r.x + Mathf.Round((r.width - m) * 0.5f), r.y + Mathf.Round((r.height - m) * 0.5f), m, m), country, c.Asked);
                GUI.color = before;
            }
            if (c.State == GovernmentFormation.RoundCellState.Refused || c.State == GovernmentFormation.RoundCellState.LastRefused)
            {
                PoliSimTheme.Stroke(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), -45f, 2f, PoliSimTheme.Bad);
            }
        }

        /// <summary>The dates: rows of the label in the muted mono and the day in the bold, each over its rule.</summary>
        private void DrawForeignDates(Rect pane, ForeignElectionCard card)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float rowH = V35.Px(24f), gap = V35.Px(4f);
            GUIStyle label = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle value = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            float y = pane.y;
            foreach ((string l, string v) in card.Dates)
            {
                float vw = Mathf.Ceil(value.CalcSize(new GUIContent(v)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, Mathf.Max(1f, pane.width - vw - V35.Px(8f)), rowH), l, label);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.xMax - vw, y, vw, rowH), v, value);
                PoliSimTheme.Rule(new Rect(pane.x, y + rowH - 1f, pane.width, 1f), V35.ListRule);
                y += rowH + gap;
            }
        }
    }
}
