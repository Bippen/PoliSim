using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-35 (D-US part two's board 30b and answers 2 and 3, a PROPOSAL Elias read as the go-ahead on 2026-10-23): <b>A BILL'S PATH ON THE SIGNING
    /// PLATE.</b> Under the plate's head, for a Congress division: <b>the strip</b> of five stations - the two chambers in the bill's own order, the President,
    /// the override, the law - passed ones in ink, the current one amber, the conditional ones in a light rule, the unreached dimmed (`UsBillPath`, read from
    /// the plate's records); for a bill on the player-president's desk, <b>the ten days</b> under it - the presentment day, each counted day numbered, each
    /// Sunday narrow and unnumbered, and the day it is law unanswered (`UsBillPath.TenDays`, `PresidentialDocket`'s own count); and <b>the pocket veto</b>, one
    /// line in the ABSENT frame - not modelled, Congress held in session. On the desk the third column opens with <b>your answer</b>: each verb and its
    /// consequence - Sign, law today; Veto, the override at once and the lines it needs, never predicted; Later, the Docket and the law's day unanswered.
    /// <para><b>Declared departures from 30b:</b> the stations' third line (the board's sub) is each station's slip, and the ten days are drawn only on the
    /// desk (a vetoed bill's record keeps no presentment day), so the plate keeps its height at 16:9; on a vote on a veto the pocket veto's line stays on the
    /// count's slip, where §853 put it, as the strip takes the room.</para>
    /// </summary>
    public partial class SigningScreen
    {
        /// <summary>PS-6 US-35: what the desk knows of a bill it holds - the day it was presented, the day it is law unanswered, today, and whether it takes a veto.</summary>
        public sealed class DeskContext
        {
            public DateTime PresentedOn;
            public DateTime LawDay;
            public DateTime Today;
            public bool MayVeto;
            /// <summary>Whether the bill runs the ten days - false for one with a deadline of its own (the eve's continuing resolution, J7), law on that day.</summary>
            public bool CountsTenDays = true;
        }

        private const float StationHeight = 54f, ArrowWidth = 33f, DayHeight = 48f, PocketHeight = 30f;

        private static Color StationInk(UsBillPath.Mark m) =>
            m == UsBillPath.Mark.Now ? PoliSimTheme.Caution : m == UsBillPath.Mark.Failed ? PoliSimTheme.Bad : m == UsBillPath.Mark.Passed ? PoliSimTheme.Good
            : m == UsBillPath.Mark.Done ? PoliSimTheme.TextPrimary : Muted;

        /// <summary>PS-6 US-35: the strip, the ten days on the desk, and the pocket veto's line, in one block under the head; nothing outside a Congress.</summary>
        private static void BuildUsPath(Transform parent, Country country, DivisionRecord record, DeskContext desk, Transform overlay)
        {
            List<UsBillPath.Station> path = UsBillPath.Of(country, record, desk != null, desk?.LawDay ?? DateTime.MinValue, desk?.CountsTenDays ?? true);
            if (path == null) { return; }
            var block = new GameObject("UsPath");
            block.transform.SetParent(parent, false);
            block.AddComponent<RectTransform>();
            VerticalLayoutGroup v = block.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            bool onVeto = UsBillPath.OnVeto(record);
            float height = StationHeight + (desk != null && desk.CountsTenDays ? 6f + DayHeight : 0f) + (onVeto ? 0f : 6f + PocketHeight);
            LayoutElement size = block.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = height;

            // the strip
            Transform strip = CanvasRows.HRow(block.transform, "Stations", StationHeight, 0f);
            HorizontalLayoutGroup stripLayout = strip.GetComponent<HorizontalLayoutGroup>();
            stripLayout.childForceExpandHeight = true;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    Text arrow = CanvasChrome.MakeTextRealWeight(strip, "Arrow", "→", PoliSimTheme.Body, Line, PoliSimTheme.Hex(0x8A7A5C), TextAnchor.MiddleCenter);
                    LayoutElement a = arrow.gameObject.AddComponent<LayoutElement>();
                    a.minWidth = a.preferredWidth = ArrowWidth;
                    a.flexibleWidth = 0f;
                }
                BuildStation(strip, path[i], overlay);
            }

            // the ten days, on the desk
            if (desk != null && desk.CountsTenDays) { BuildTenDays(block.transform, country, desk, overlay); }

            // the pocket veto, not modelled
            if (!onVeto)
            {
                Transform pocket = CanvasRows.HRow(block.transform, "PocketVeto", PocketHeight, 15f);
                var box = new GameObject("Absent");
                box.transform.SetParent(pocket, false);
                box.AddComponent<RectTransform>();
                CanvasRows.Edges(box.transform, PoliSimTheme.TextPrimary, 1.5f);
                LayoutElement boxSize = box.AddComponent<LayoutElement>();
                boxSize.minWidth = boxSize.preferredWidth = 36f;
                boxSize.minHeight = boxSize.preferredHeight = 21f;
                Text line = Word(pocket, "Pocket veto · not modelled. Congress is held in session, so a bill left unanswered for ten days becomes law.", PoliSimTheme.Body, Caption, PoliSimTheme.TextPrimary);
                CanvasRows.Slip(line.gameObject, overlay, "POCKET VETO · NOT MODELLED", new[]
                {
                    "A BILL DIES UNSIGNED WHERE CONGRESS ADJOURNS AND PREVENTS ITS RETURN (ART. I, SEC. 7)",
                    "THE GAME HOLDS CONGRESS IN SESSION THROUGH THE TEN DAYS, SO IT NEVER HAPPENS HERE",
                });
            }
        }

        private static void BuildStation(Transform strip, UsBillPath.Station s, Transform overlay)
        {
            var cell = new GameObject("Station " + s.Label);
            cell.transform.SetParent(strip, false);
            cell.AddComponent<RectTransform>();
            Image ground = cell.AddComponent<Image>();
            ground.color = s.Mark == UsBillPath.Mark.Now ? PoliSimTheme.Hex(0xF6EFDF) : s.Mark == UsBillPath.Mark.Conditional || s.Mark == UsBillPath.Mark.Off ? new Color(0f, 0f, 0f, 0f) : PoliSimTheme.Hex(0xF5EDDE);
            ground.raycastTarget = false;
            Color edge = s.Mark == UsBillPath.Mark.Now ? PoliSimTheme.Hex(0xC9A23A) : s.Mark == UsBillPath.Mark.Off || s.Mark == UsBillPath.Mark.Conditional ? PoliSimTheme.Hex(0xB7A98C) : StationInk(s.Mark);
            CanvasRows.Edges(cell.transform, edge, s.Mark == UsBillPath.Mark.Now ? 3f : 1.5f);
            LayoutElement size = cell.AddComponent<LayoutElement>();
            size.flexibleWidth = 1f;
            size.preferredWidth = 0f;
            VerticalLayoutGroup v = cell.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 8, 4, 4);
            v.spacing = 0f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            Text label = CanvasChrome.MakeTextRealWeight(cell.transform, "Label", s.Label.ToUpperInvariant(), PoliSimTheme.Body, 18, Muted, TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 22f;
            Text status = CanvasChrome.MakeTextRealWeight(cell.transform, "Status", s.Status, PoliSimTheme.Document, Figure, StationInk(s.Mark), TextAnchor.MiddleLeft, FontStyle.Bold);
            status.gameObject.AddComponent<LayoutElement>().minHeight = 24f;
            CanvasGroup fade = cell.AddComponent<CanvasGroup>();
            fade.alpha = s.Mark == UsBillPath.Mark.Off ? 0.5f : 1f;
            fade.interactable = false;
            fade.blocksRaycasts = true;
            CanvasRows.Slip(cell, overlay, s.Label.ToUpperInvariant() + " · " + s.Status, new[] { s.Sub.ToUpperInvariant() });
        }

        /// <summary>PS-6 US-35: the ten days, Sundays excepted - the label, the presentment day, each day (a Sunday narrow and unnumbered), and the law's day.</summary>
        private static void BuildTenDays(Transform parent, Country country, DeskContext desk, Transform overlay)
        {
            Transform row = CanvasRows.HRow(parent, "TenDays", DayHeight, 6f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            Text head = CanvasChrome.MakeTextRealWeight(row, "Head", "THE TEN DAYS\nSUNDAYS EXCEPTED", PoliSimTheme.Body, 18, Muted, TextAnchor.MiddleLeft);
            LayoutElement headSize = head.gameObject.AddComponent<LayoutElement>();
            headSize.minWidth = headSize.preferredWidth = 200f;
            string D(DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);
            List<UsBillPath.Day> days = UsBillPath.TenDays(country.Id, desk.PresentedOn);
            int counted = 0;
            foreach (UsBillPath.Day d in days)
            {
                bool today = d.Date == desk.Today.Date;
                bool past = d.Date < desk.Today.Date;
                if (d.Number > 0 && d.Date <= desk.Today.Date) { counted = d.Number; }
                string top = d.Sunday ? "Su" : d.Number == 0 ? (today ? "TODAY" : "0") : d.Number.ToString(CultureInfo.InvariantCulture);
                DayCell(row, top, d.Sunday ? string.Empty : D(d.Date), d.Sunday ? 33f : d.Number == 0 ? 90f : 72f,
                    today ? PoliSimTheme.Hex(0xC9A23A) : d.Sunday ? (Color?)null : PoliSimTheme.Hex(0x8A7A5C), today ? 3f : 1.5f, past && !d.Sunday, d.Sunday ? PoliSimTheme.Hex(0xB7A98C) : PoliSimTheme.TextPrimary);   // the review's L5: today amber on a Sunday too
            }
            DayCell(row, "LAW", D(desk.LawDay), 100f, PoliSimTheme.Hex(0x8A7A5C), 1.5f, false, Muted);
            int left = UsBillPath.CountedDaysLeft(country.Id, desk.PresentedOn, desk.Today);
            CanvasRows.Slip(head.gameObject, overlay, "THE TEN DAYS · SUNDAYS EXCEPTED", new[]
            {
                "ART. I, SEC. 7: A BILL THE PRESIDENT NEITHER SIGNS NOR RETURNS WITHIN TEN DAYS, SUNDAYS EXCEPTED, IS LAW",
                "PRESENTED " + desk.PresentedOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · DAY " + counted.ToString(CultureInfo.InvariantCulture) + " OF 10 · "
                    + left.ToString(CultureInfo.InvariantCulture) + (left == 1 ? " DAY" : " DAYS") + " LEFT",
                "UNANSWERED, LAW ON " + desk.LawDay.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · THE DOCKET COUNTS THEM, AND DOES NOT HOLD THE CLOCK",
            });
        }

        private static void DayCell(Transform row, string top, string date, float width, Color? edge, float edgeWidth, bool elapsed, Color ink)
        {
            var cell = new GameObject("Day " + top);
            cell.transform.SetParent(row, false);
            cell.AddComponent<RectTransform>();
            Image ground = cell.AddComponent<Image>();
            ground.color = elapsed ? PoliSimTheme.Hex(0xE2D7C1) : new Color(0f, 0f, 0f, 0f);
            ground.raycastTarget = false;
            if (edge.HasValue) { CanvasRows.Edges(cell.transform, edge.Value, edgeWidth); }
            LayoutElement size = cell.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = width;
            size.flexibleWidth = 0f;
            VerticalLayoutGroup v = cell.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;
            v.childAlignment = TextAnchor.MiddleCenter;
            CanvasChrome.MakeTextRealWeight(cell.transform, "Number", top, PoliSimTheme.Document, 18, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (date.Length > 0) { CanvasChrome.MakeTextRealWeight(cell.transform, "Date", date, PoliSimTheme.Body, 18, Muted, TextAnchor.MiddleCenter); }
        }

        /// <summary>
        /// PS-6 US-35 (board 30b, answer 3): YOUR ANSWER at the head of the third column - each verb with its consequence in one line. Sign: law today. Veto: the
        /// override follows at once and needs two thirds in each chamber (the lines on the passage's own base, `UsBillPath.OverrideBase`), against the votes the
        /// bill passed with - arithmetic, never a prediction. Later: the Docket, and law unanswered on its day.
        /// </summary>
        private static void BuildUsAnswers(Transform column, Country country, DivisionRecord record, DeskContext desk, Transform overlay)
        {
            ColumnHead(column, "Your answer");
            string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            DivisionRecord first = record.OtherChamber;
            DivisionRecord house = record.Chamber == Congress.House ? record : first;
            DivisionRecord senate = record.Chamber == Congress.Senate ? record : first;
            int houseBase = UsBillPath.OverrideBase(country, house), senateBase = UsBillPath.OverrideBase(country, senate);
            string I(int n) => n.ToString(CultureInfo.InvariantCulture);
            var rows = new List<(string Verb, string Line)> { ("Sign", "Law today, " + Day(desk.Today) + ".") };
            if (desk.MayVeto)
            {
                rows.Add(("Veto", "The override follows at once: " + I(UsCongressView.OverrideLine(houseBase)) + " of " + I(houseBase) + " and " + I(UsCongressView.OverrideLine(senateBase)) + " of " + I(senateBase)
                    + ". It passed with " + UsBillPath.PassedWith(country, house) + " and " + UsBillPath.PassedWith(country, senate) + "."));   // the review's M2: the yeas the records hold, never a zero not counted
            }
            rows.Add(("Later", "It waits on the Docket. Unanswered, it is law on " + Day(desk.LawDay) + "."));
            foreach ((string verb, string line) in rows)
            {
                Transform row = CanvasRows.HRow(column, "Answer " + verb, 54f, 15f);
                row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
                Text v = CanvasChrome.MakeTextRealWeight(row, "Verb", verb, PoliSimTheme.Document, Line, PoliSimTheme.TextPrimary, TextAnchor.UpperLeft, FontStyle.Bold);
                LayoutElement vs = v.gameObject.AddComponent<LayoutElement>();
                vs.minWidth = vs.preferredWidth = 84f;
                vs.flexibleWidth = 0f;
                Text t = CanvasChrome.MakeTextRealWeight(row, "Consequence", line, PoliSimTheme.Body, 19, PoliSimTheme.TextPrimary, TextAnchor.UpperLeft);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                LayoutElement ts = t.gameObject.AddComponent<LayoutElement>();
                ts.flexibleWidth = 1f;
                ts.preferredWidth = 0f;
                CanvasRows.Slip(v.gameObject, overlay, verb.ToUpperInvariant(), new[] { line.ToUpperInvariant(), "THE OVERRIDE IS NEVER PREDICTED: THE LINES ARE ARITHMETIC ON THE BILL'S OWN DIVISIONS" });
            }
        }
    }
}
