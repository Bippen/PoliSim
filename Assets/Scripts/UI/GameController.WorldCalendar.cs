using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-8 (Elias's ruling K163: "a foreign election reaches the player as a Docket/news item and a compact results card (a Design board)"): <b>AN ELECTION
    /// ABROAD ON THE DOCKET</b> - each election another country held in the last month (`WorldCalendar.RecentAbroad`) is a docket item in the K38 idiom of
    /// information that never holds the clock. S6 (Design's return of 2026-10-30, `send/s6_return/RETURN.md`, board `S6 Foreign Election.dc.html`; K320):
    /// <list type="bullet">
    /// <item><description><b>The entry</b> is one caption - the country, the chamber, the polling day - with its state at the right ("in 38 days", "held · day 12",
    /// "formed 12 Apr 2029") and, once counted, one line of figures under it (each list's ink square, its code, its seats and their change, drawn by
    /// `GameController.ForeignResultCard.cs`); the stand-in's sentence is the entry's slip. An election ahead is listed from its campaign's start
    /// (`WorldCalendar.UpcomingAbroad`), and a count stays listed while its formation runs (`WorldCalendar.ShownForDays`).</description></item>
    /// <item><description><b>The compact card</b> opens inline under its entry on the card paper with the Global area's 4 px bar (drawn here, so the file that
    /// draws the party inks draws no area accent - `PartyInkDrawSiteCheck`'s clause 2); its three panes are `DrawForeignResultCard`'s.</description></item>
    /// <item><description><b>The polling days</b> - every country's next, the player's first - are a card on Statistics › International
    /// (<see cref="DrawPollingDaysCard"/>); each election abroad in the month is a row on the Desk's This month that opens its entry, and a dot on its calendar
    /// (`BuildCalendarMonthMarkers`).</description></item>
    /// </list>
    /// </summary>
    public partial class GameController
    {
        /// <summary>L8: the day's elections abroad, read once a day (the day, the world and the player's country it was read for). S6: each with its card, held and
        /// ahead.</summary>
        private List<ForeignElectionCard> _abroadCards;
        private System.DateTime _abroadDay;
        private World _abroadWorld;
        private CountryId _abroadPlayer;

        /// <summary>S6: the elections abroad the Docket lists today - the held ones newest first, then the ones ahead soonest first - each read into its card. Read once
        /// a day (L8 of the s938 review: every count, seating and formation abroad lands in the day's step, so the day's list stands until the next).</summary>
        private List<ForeignElectionCard> ForeignElections()
        {
            if (_simulationManager?.World == null) { return new List<ForeignElectionCard>(); }
            System.DateTime today = _simulationManager.CurrentDate;
            if (_abroadCards == null || _abroadDay != today || !ReferenceEquals(_abroadWorld, _simulationManager.World) || _abroadPlayer != PlayerCountryId)
            {
                List<WorldCalendar.Result> results = WorldCalendar.RecentAbroad(_simulationManager.World, PlayerCountryId, today);
                results.AddRange(WorldCalendar.UpcomingAbroad(_simulationManager.World, PlayerCountryId, today));
                _abroadCards = new List<ForeignElectionCard>(results.Count);
                foreach (WorldCalendar.Result r in results)
                {
                    _abroadCards.Add(ForeignElectionCard.Read(r, _simulationManager.World.GetCountry(r.Country), today, _simulationManager.LastCalendarFormation(r.Country)));
                }
                _abroadDay = today; _abroadWorld = _simulationManager.World; _abroadPlayer = PlayerCountryId;
            }
            return _abroadCards;
        }

        /// <summary>K163, S6: the elections abroad as docket items; true where any was drawn.</summary>
        private bool DrawForeignElectionItems()
        {
            List<ForeignElectionCard> cards = ForeignElections();
            foreach (ForeignElectionCard card in cards)
            {
                WorldCalendar.Result r = card.Result;
                string key = "abroad:" + r.Key;
                _docketSlipBook.Anchors["docket:" + key] = card.EntrySlip;
                bool hasCard = r.Rows.Count > 0;
                string caption = CountryName(r.Country) + " · " + r.Chamber + " · " + ForeignElectionCard.Day(r.Day);
                System.Action<Rect> seats = hasCard && card.Counted ? (System.Action<Rect>)(rect => DrawForeignSeatsLine(rect, card)) : null;
                if (DrawDocketItem(key, "globe", UiPalette.GetAreaColor(UiPalette.SystemArea.Global), caption, hasCard ? null : r.Line,
                        holdsTime: false, action: hasCard ? (r.Upcoming ? "Open" : "Results") : null, stampWords: "Information", titleRight: card.When, lineDrawer: seats))
                {
                    DrawForeignCardFrame(card, key);
                }
            }
            return cards.Count > 0;
        }

        private string CountryName(CountryId id) => _simulationManager?.World?.GetCountry(id)?.Name ?? id.ToString();

        /// <summary>S6 (RETURN.md, "The compact card"): the card inline under its entry - the card paper inside its rule, the card's padding, the Global area's 4 px
        /// bar at its left - its panes drawn by <see cref="DrawForeignResultCard"/>.</summary>
        private void DrawForeignCardFrame(ForeignElectionCard card, string key)
        {
            GUILayout.BeginVertical(V35CardStyle());
            DrawForeignResultCard(card, key);
            GUILayout.EndVertical();
            Rect frame = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(frame.x, frame.y, V35.Px(4f), frame.height), UiPalette.GetAreaColor(UiPalette.SystemArea.Global)); }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>S6: a stamp as the board draws it - flat, its word in the bold mono, a 1.5 px rule (dashed for an election ahead), its right edge at
        /// <paramref name="right"/>. Returns its width.</summary>
        private float DrawFlatStamp(float right, float y, float h, string word, Color ink, bool dashed)
        {
            GUIStyle face = V35Mono(V35.Floor, ink, bold: true, TextAnchor.MiddleCenter);
            float w = Mathf.Ceil(face.CalcSize(new GUIContent(word)).x) + V35.Px(10f);
            var r = new Rect(right - w, y, w, h);
            if (Event.current.type == EventType.Repaint)
            {
                if (dashed) { DashedRect(r, ink); } else { Outline(r, ink, 1.5f); }
                PoliSimWidgets.MeasuredLabel(r, word, face);
            }
            return w;
        }

        // ------------------------------------------------------------------------------------------------------------ the polling days (RETURN.md 6c)

        /// <summary>S6: the polling days' slips, by anchor - merged into the Statistics sheet's book where it is built.</summary>
        private readonly Dictionary<string, SlipContent> _pollingDaySlips = new Dictionary<string, SlipContent>();

        /// <summary>S6: the polling days card's top in the Statistics content's coordinates, the last frame it drew - the film scrolls to it.</summary>
        private float _pollingDaysTop = -1f;

        private List<ForeignElectionCard.PollingRow> _pollingRows;
        private System.DateTime _pollingRowsDay;
        private World _pollingRowsWorld;
        private CountryId _pollingRowsPlayer;

        /// <summary>
        /// S6 (RETURN.md 6c, right: "It lives on Stats › International ... a card titled Polling days, with the ballot icon and a DERIVED stamp"): one row per
        /// chamber or office - the country's code, the chamber, the polling day, how far off, its state stamp (YOURS, UPCOMING dashed, FORMING in the caution ink)
        /// - the player's own first, the rest by date (<see cref="ForeignElectionCard.PollingDays"/>); a day the game holds no election for draws ABSENT. The
        /// board's foot sentence and each row's rule are the head's and the row's slips (K320). Read once a day.
        /// </summary>
        private void DrawPollingDaysCard(float contentWidth)
        {
            System.DateTime today = _simulationManager.CurrentDate;
            if (_pollingRows == null || _pollingRowsDay != today || !ReferenceEquals(_pollingRowsWorld, _world) || _pollingRowsPlayer != PlayerCountryId)
            {
                System.DateTime own = _simulationManager.TryPlayerPollingDay(out System.DateTime day) ? day : System.DateTime.MinValue;
                _pollingRows = ForeignElectionCard.PollingDays(_world, PlayerCountryId, today, own);
                _pollingRowsDay = today; _pollingRowsWorld = _world; _pollingRowsPlayer = PlayerCountryId;
            }
            List<ForeignElectionCard.PollingRow> rows = _pollingRows;
            bool repaint = Event.current.type == EventType.Repaint;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            float rowH = V35.Px(30f), colHeadH = V35.Px(22f);
            float h = V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(V35.CardHeadGap) + colHeadH + rows.Count * rowH;
            Rect card = GUILayoutUtility.GetRect(contentWidth, h, GUILayout.Width(contentWidth), GUILayout.Height(h));
            if (repaint) { _pollingDaysTop = card.y; }
            Rect inner = DrawV35Card(card);
            GUIStyle stampProbe = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleCenter);
            float derivedW = Mathf.Ceil(stampProbe.CalcSize(new GUIContent("DERIVED")).x) + V35.Px(10f);
            Rect head = DrawV35CardHead(inner, "ballot", "Polling days", area, derivedW + V35.Px(8f));
            DrawFlatStamp(head.xMax, head.y + Mathf.Round((head.height - V35.Px(20f)) * 0.5f), V35.Px(20f), "DERIVED", PoliSimTheme.TextPrimary, dashed: false);
            _pollingDaySlips["polling:head"] = new SlipContent("Polling days")
                .Add("Each country's next, by its statute")
                .Add("An extra election replaces it when ordered")
                .Add("Yours leads · the rest by date")
                .Add("Each date a dot on the Desk's calendar");
            StatsAnchor(head, "polling:head");

            // the columns (RETURN.md: 44 · 150 · 120 · 90 · the stamp)
            float y = head.yMax + V35.Px(V35.CardHeadGap);
            float codeW = V35.Px(44f), chamberW = V35.Px(150f), dayW = V35.Px(120f), inW = V35.Px(90f);
            float xChamber = inner.x + codeW, xDay = xChamber + chamberW, xIn = xDay + dayW, xState = xIn + inW;
            GUIStyle colFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            if (repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(xChamber, y, chamberW, colHeadH), "chamber", colFace);
                PoliSimWidgets.MeasuredLabel(new Rect(xDay, y, dayW, colHeadH), "polling day", colFace);
                PoliSimWidgets.MeasuredLabel(new Rect(xIn, y, inW, colHeadH), "in", colFace);
                PoliSimWidgets.MeasuredLabel(new Rect(xState, y, Mathf.Max(1f, inner.xMax - xState), colHeadH), "state", colFace);
                PoliSimTheme.Rule(new Rect(inner.x, y + colHeadH - 1f, inner.width, 1f), PoliSimTheme.HairlineStrong);
            }
            y += colHeadH;
            GUIStyle codeFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle chamberFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle dayFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle inFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary);
            for (int i = 0; i < rows.Count; i++)
            {
                ForeignElectionCard.PollingRow p = rows[i];
                var row = new Rect(inner.x, y, inner.width, rowH);
                if (repaint)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(inner.x, y, codeW, rowH), p.Code, codeFace);
                    PoliSimWidgets.MeasuredLabel(new Rect(xChamber, y, chamberW - V35.Px(6f), rowH), p.Chamber, chamberFace);
                    if (p.Day != System.DateTime.MinValue)
                    {
                        PoliSimWidgets.MeasuredLabel(new Rect(xDay, y, dayW, rowH), ForeignElectionCard.Day(p.Day), dayFace);
                        PoliSimWidgets.MeasuredLabel(new Rect(xIn, y, inW, rowH), p.In, inFace);
                    }
                    PoliSimTheme.Rule(new Rect(inner.x, row.yMax - 1f, inner.width, 1f), V35.ListRule);
                    UiContainmentGuard.Check("Polling days row", row, inner);
                }
                if (p.Day == System.DateTime.MinValue && repaint)
                {
                    float side = V35.Px(16f);
                    DrawStateGlyph(new Rect(xDay, y + Mathf.Round((rowH - side) * 0.5f), side, side), Symbol.Absent, PoliSimTheme.TextMuted);
                }
                if (p.Stamp != null)
                {
                    float sh = V35.Px(20f);
                    float sw = Mathf.Ceil(stampProbe.CalcSize(new GUIContent(p.Stamp)).x) + V35.Px(10f);
                    DrawFlatStamp(xState + sw, y + Mathf.Round((rowH - sh) * 0.5f), sh, p.Stamp, p.Stamp == "FORMING" ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, dashed: p.Stamp == "UPCOMING");
                }
                string anchor = "polling:" + i.ToString(CultureInfo.InvariantCulture);
                _pollingDaySlips[anchor] = p.Slip;
                StatsAnchor(row, anchor);
                y += rowH;
            }
        }
    }
}
