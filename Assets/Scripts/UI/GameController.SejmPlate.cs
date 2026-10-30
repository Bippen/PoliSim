using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S11 (Design's return of 2026-10-30, board 11e, built decluttered under K320): <b>THE SEJM'S TERM SHORTENED - THE ANNOUNCEMENT</b>. On the day the last step
    /// is refused and the President shortens the term (Art. 155 ust. 2), one plate over the page below the strip, as the debt crisis's moment is drawn (32a,
    /// `GameController.Crisis.cs`, whose pieces it shares): the head (the chamber, "Term shortened", the country and the day, the SNAP ELECTION stamp); the three
    /// steps, each struck with its article, its party, its result in BAD and its day; what follows (the polling day, the caretaker, the campaign); and the polling
    /// day by the rule - a calendar from the order's week, the order's day outlined in BAD, the forty-fifth dashed, the chosen Sunday filled. It stops the clock
    /// until <b>Continue</b>; nothing else on it is a control.
    /// <para>Owed once per shortening (<see cref="SejmDesk.TryShortened"/> reads the record's concluding line, dated by the engine); acknowledged by the screen alone,
    /// never saved (the K42 idiom: a game loaded on that day shows it again). Every figure and day is the engine's - the record's lines and
    /// `SimulationManager.ExtraElectionDate` - except where a film stages it from a projection (<see cref="_sejmPlateStaged"/>, said so in the film's log).</para>
    /// </summary>
    public partial class GameController
    {
        private object _sejmPlateSeenCountry;
        private System.DateTime _sejmPlateSeenOn = System.DateTime.MinValue;
        /// <summary>The film's staged plate (built from a projection, `SejmDesk.PlateOf(SejmNightSteps)`), drawn while set; null in play.</summary>
        private SejmDesk.Plate _sejmPlateStaged;
        private Rect _sejmContinueRect;
        private bool _sejmPlateWasUp;
        private string _sejmSlipHover;
        private float _sejmSlipSince;
        private readonly List<(Rect Rect, SlipContent Slip, string Id)> _sejmSlips = new List<(Rect Rect, SlipContent Slip, string Id)>();
        /// <summary>The live plate as last read - re-read only when the day, the record or its log moves.</summary>
        private SejmDesk.Plate _sejmPlateLive;
        private System.DateTime _sejmPlateReadOn = System.DateTime.MinValue;
        private object _sejmPlateReadRecord;
        private int _sejmPlateReadLines = -1;

        /// <summary>The plate to draw: the staged one, else the live one owed today and not yet acknowledged; null for none.</summary>
        private SejmDesk.Plate CurrentSejmPlate()
        {
            if (_sejmPlateStaged != null) { return _sejmPlateStaged; }
            if (_playerCountry == null || _simulationManager == null || _isGameOver) { return null; }
            SejmFormation f = SejmDesk.Record(_playerCountry);
            System.DateTime today = _simulationManager.CurrentDate.Date;
            if (f == null) { return null; }
            if (!ReferenceEquals(f, _sejmPlateReadRecord) || f.Log.Count != _sejmPlateReadLines || today != _sejmPlateReadOn)
            {
                _sejmPlateReadRecord = f;
                _sejmPlateReadLines = f.Log.Count;
                _sejmPlateReadOn = today;
                SejmDesk.Plate plate = SejmDesk.PlateOf(_playerCountry, f, _simulationManager.ExtraElectionDate);
                _sejmPlateLive = plate != null && plate.OrderedOn.Date == today ? plate : null;
            }
            if (_sejmPlateLive == null) { return null; }
            if (ReferenceEquals(_sejmPlateSeenCountry, _playerCountry) && _sejmPlateSeenOn == _sejmPlateLive.OrderedOn.Date) { return null; }
            return _sejmPlateLive;
        }

        /// <summary>S11: the shortened term's plate is up and holds the clock (the strip names it among the holds).</summary>
        private bool SejmPlateHolds() => CurrentSejmPlate() != null;

        /// <summary>The plate's Continue (and the film's, by reflection - one path): this shortening is acknowledged, and the clock runs again.</summary>
        private void AcknowledgeSejmPlate()
        {
            SejmDesk.Plate plate = CurrentSejmPlate();
            if (plate == null || plate.Staged) { return; }
            _sejmPlateSeenCountry = _playerCountry;
            _sejmPlateSeenOn = plate.OrderedOn.Date;
        }

        /// <summary>Before the page draws, as the crisis plate's input: a pointer event over the page is the plate's - Continue's press acknowledges it, every
        /// other one is swallowed; no key reaches the page while it is up.</summary>
        private void SejmPlateInput(Rect page)
        {
            if (!SejmPlateHolds()) { _sejmPlateWasUp = false; return; }
            Event e = Event.current;
            if (!_sejmPlateWasUp) { GUIUtility.keyboardControl = 0; GUIUtility.hotControl = 0; _sejmPlateWasUp = true; }
            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp) { e.Use(); return; }
            bool pointer = e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.ScrollWheel;
            if (!pointer || !page.Contains(e.mousePosition)) { return; }
            if (e.type == EventType.MouseDown && e.button == 0 && _sejmContinueRect.Contains(e.mousePosition))
            {
                AcknowledgeSejmPlate();
                AudioDirector.Fire(AudioCue.FolderSwitch);
            }
            e.Use();
        }

        /// <summary>The plate over <paramref name="page"/>, while it is owed (or staged).</summary>
        private void DrawSejmPlate(Rect page)
        {
            SejmDesk.Plate p = CurrentSejmPlate();
            if (p == null) { return; }
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { PoliSimTheme.Rule(page, PlateScrim); }
            float w = Mathf.Min(page.width - V35.Px(24f), V35.Px(1080f)), h = Mathf.Min(page.height - V35.Px(16f), V35.Px(614f));
            var plate = new Rect(Mathf.Round(page.x + (page.width - w) * 0.5f), Mathf.Round(page.y + Mathf.Min(V35.Px(23f), (page.height - h) * 0.5f)), w, h);
            if (repaint)
            {
                PoliSimTheme.Rule(new Rect(plate.x + V35.Px(2f), plate.y + V35.Px(8f), plate.width, plate.height), new Color(0f, 0f, 0f, 0.35f));
                PoliSimTheme.Rule(plate, PlatePaper);
            }
            Outline(plate, StripLine, 1f);
            var inner = new Rect(plate.x + V35.Px(32f), plate.y + V35.Px(22f), plate.width - V35.Px(64f), plate.height - V35.Px(42f));
            _sejmSlips.Clear();

            // ---- the head ----
            float headH = V35.Px(34f), icon = V35.Px(30f);
            DrawV35Icon(new Rect(inner.x, inner.y + Mathf.Round((headH - icon) * 0.5f), icon, icon), "chamber", PoliSimTheme.TextPrimary);
            GUIStyle title = V35Serif(28f, PoliSimTheme.TextPrimary);
            const string titleWords = "Term shortened";
            float tx = inner.x + icon + V35.Px(16f), tw = TextWidth(title, titleWords);
            PlateText(new Rect(tx, inner.y, tw + 2f, headH), titleWords, title);
            Rect stamp = DrawCrisisStamp(inner.xMax, inner.y + headH * 0.5f, "SNAP ELECTION", PoliSimTheme.Caution, 15f, V35.Px(24f));
            GUIStyle sub = V35Serif(17f, PoliSimTheme.TextMuted);
            float sx = tx + tw + V35.Px(16f);
            string subText = "Poland · " + p.OrderedOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            PlateText(new Rect(sx, inner.y, Mathf.Max(1f, Mathf.Min(TextWidth(sub, subText) + 2f, stamp.x - V35.Px(16f) - sx)), headH), subText, sub);
            float ruleY = inner.y + headH + V35.Px(10f);
            if (repaint) { PoliSimTheme.Rule(new Rect(inner.x, ruleY, inner.width, 1f), PoliSimTheme.TextPrimary); }
            _sejmSlips.Add((new Rect(inner.x, inner.y, inner.width, headH), new SlipContent(("Term shortened · " + p.OrderedOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture)).ToUpperInvariant())
                .Add("the third step refused · Art. 155.2")
                .Add("the President shortens the term and orders elections")
                .Add("the poll within 45 days · Art. 98.5")
                .Add(p.Staged ? "STAGED for the film, from the night's projection" : "the engine's record of the steps"), "sejmplate:head"));

            // ---- the foot: the clock's words and Continue ----
            float footH = V35.Px(36f);
            GUIStyle button = V35Serif(16f, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            float bw = TextWidth(button, "Continue") + V35.Px(44f);
            var cont = new Rect(inner.xMax - bw, inner.yMax - footH, bw, footH);
            if (repaint)
            {
                _sejmContinueRect = cont;
                PoliSimTheme.Rule(cont, PlateButton);
            }
            Outline(cont, PlateButtonEdge, 1f);
            PlateText(cont, "Continue", button);
            var clock = new Rect(inner.x, cont.y, Mathf.Max(1f, cont.x - inner.x - V35.Px(14f)), footH);
            PlateText(clock, "clock held", V35Mono(V35.Floor, PoliSimTheme.TextMuted));
            System.DateTime opens = SejmDesk.SnapCampaign(p.OrderedOn, p.PollingDay).CampaignStart;
            _sejmSlips.Add((clock, new SlipContent("THE CLOCK WAITS").Add("until you continue").Add("the campaign opens " + opens.ToString("d MMM", CultureInfo.InvariantCulture) + " · Campaign joins the rail"), "sejmplate:clock"));

            // ---- the three columns ----
            float top = ruleY + 1f + V35.Px(16f), bottom = cont.y - V35.Px(16f);
            float gap = V35.Px(30f), leftW = V35.Px(330f), midW = V35.Px(300f);
            var left = new Rect(inner.x, top, leftW, bottom - top);
            var mid = new Rect(left.xMax + gap, top, midW, bottom - top);
            var right = new Rect(mid.xMax + gap, top, Mathf.Max(1f, inner.xMax - mid.xMax - gap), bottom - top);
            DrawSejmPlateSteps(left, p);
            DrawSejmPlateFollows(mid, p, opens);
            DrawSejmPlateCalendar(right, p);
            DrawSejmSlips(plate);
        }

        /// <summary>The left column: the three steps, each struck - its cell (the last ruled in BAD), its name and day, its article and party, its result in BAD.</summary>
        private void DrawSejmPlateSteps(Rect col, SejmDesk.Plate p)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "the three steps");
            y += V35.Px(20f) + V35.Px(10f);
            bool repaint = Event.current.type == EventType.Repaint;
            float cell = V35.Px(22f), line = V35.Px(19f), pad = V35.Px(9f);
            for (int i = 0; i < p.Steps.Length; i++)
            {
                SejmDesk.PlateStep s = p.Steps[i];
                float rowH = pad + V35.Px(24f) + 2f * line + pad;
                if (repaint) { PoliSimTheme.Rule(new Rect(col.x, y, col.width, 1f), PlateRule); }
                var c = new Rect(col.x, y + pad + V35.Px(2f), cell, cell);
                bool last = i == p.Steps.Length - 1;
                Outline(c, last ? PoliSimTheme.Bad : PlateLadderMuted, last ? 1.5f : 1f);
                Texture2D mark = s.Asked != null ? MarkTexture(s.Asked) : null;
                if (mark != null && repaint)
                {
                    Color before = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                    float m = V35.Px(14f);
                    GUI.DrawTexture(new Rect(c.center.x - m * 0.5f, c.center.y - m * 0.5f, m, m), mark, ScaleMode.ScaleToFit);
                    GUI.color = before;
                }
                if (repaint)
                {
                    // the struck cell: a 2 px BAD stroke at -45 degrees across it, as the night's
                    Matrix4x4 was = GUI.matrix;
                    GUIUtility.RotateAroundPivot(-45f, c.center);
                    PoliSimTheme.Rule(new Rect(c.center.x - cell * 0.64f, c.center.y - 1f, cell * 1.28f, 2f), PoliSimTheme.Bad);
                    GUI.matrix = was;
                }
                float tx = c.xMax + V35.Px(12f), tw = Mathf.Max(1f, col.xMax - tx);
                GUIStyle dayFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
                string day = s.Day != System.DateTime.MinValue ? s.Day.ToString("d MMM", CultureInfo.InvariantCulture) : string.Empty;
                float dw = TextWidth(dayFace, day) + 2f;
                PlateText(new Rect(col.xMax - dw, y + pad, dw, V35.Px(24f)), day, dayFace);
                PlateText(new Rect(tx, y + pad, Mathf.Max(1f, tw - dw - V35.Px(8f)), V35.Px(24f)), s.Name, V35Serif(17f, PoliSimTheme.TextPrimary));
                PlateText(new Rect(tx, y + pad + V35.Px(24f), tw, line), s.Article ?? string.Empty, V35Mono(V35.Floor, PoliSimTheme.TextMuted));
                PlateText(new Rect(tx, y + pad + V35.Px(24f) + line, tw, line), s.Result ?? "not carried on the record", V35Mono(V35.Floor, PoliSimTheme.Bad, bold: true));
                _sejmSlips.Add((new Rect(col.x, y, col.width, rowH), new SlipContent((s.Name + (day.Length > 0 ? " · " + day : string.Empty)).ToUpperInvariant())
                    .Add(i == 0 ? "the President's step · Art. 154.1–2" : i == 1 ? "the Sejm's step · Art. 154.3" : "the President's second step · Art. 155.1")
                    .Add(s.Result ?? "not carried on the record")
                    .Add(i < 2 ? "an absolute majority: " + SejmNightSteps.Needed(PresidentialVeto.StatutoryDeputies).ToString(CultureInfo.InvariantCulture) + " of " + PresidentialVeto.StatutoryDeputies.ToString(CultureInfo.InvariantCulture) : "more for than against, " + PresidentialVeto.Quorum.ToString(CultureInfo.InvariantCulture) + " present"),
                    "sejmplate:step" + i.ToString(CultureInfo.InvariantCulture)));
                y += rowH;
            }
        }

        /// <summary>A party's mark texture (the night's), null where none is held.</summary>
        private static Texture2D MarkTexture(string key)
        {
            foreach (PoliticalParty party in PartySystems.For(CountryId.Poland)) { if (party.Abbrev == key) { return IconLibrary.GetPartyMark(party.MarkName); } }
            return null;
        }

        /// <summary>The middle column: what follows - the polling day, the caretaker, the campaign - one ruled row each, the figure at the right, a short line under.</summary>
        private void DrawSejmPlateFollows(Rect col, SejmDesk.Plate p, System.DateTime opens)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "what follows");
            y += V35.Px(20f) + V35.Px(10f);
            int away = (int)(p.PollingDay.Date - p.OrderedOn.Date).TotalDays;
            int weeks = (int)((p.PollingDay.Date - opens.Date).TotalDays / 7);
            string caretaker = string.IsNullOrEmpty(p.Caretaker) ? "the cabinet" : PartySystems.ShortName(CountryId.Poland, p.Caretaker);
            var rows = new (string Icon, string Name, string Figure, Color Ink, string Sub, SlipContent Slip)[]
            {
                ("ballot", "Polling day", p.PollingDay.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture), PoliSimTheme.TextPrimary, away.ToString(CultureInfo.InvariantCulture) + " days away",
                    new SlipContent("POLLING DAY · " + p.PollingDay.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()).Add("within 45 days of the order · Art. 98.5").Add("the last Sunday on or before day 45").Add("the new Sejm sits by day 15 after it · Art. 98.5")),
                ("chamber", "The caretaker", caretaker, PoliSimTheme.Caution, "until the new cabinet · Art. 162.3",
                    new SlipContent("THE CARETAKER").Add(caretaker + " serves on").Add("the refused cabinet resigns · Art. 162.2").Add("it carries on until the next is appointed").Add("until the new Sejm forms a government")),
                ("cal", "The campaign", "from " + opens.ToString("d MMM", CultureInfo.InvariantCulture), PoliSimTheme.TextPrimary, "no run-up · " + weeks.ToString(CultureInfo.InvariantCulture) + " weeks",
                    new SlipContent("THE CAMPAIGN").Add("opens " + opens.ToString("d MMM yyyy", CultureInfo.InvariantCulture)).Add("no run-up for a snap election").Add("whole weeks back from the polling day, at most " + CampaignCalendar.DefaultCampaignWeeks.ToString(CultureInfo.InvariantCulture)).Add("Campaign joins the rail when it opens")),
            };
            GUIStyle subFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            float pad = V35.Px(8f), side = V35.Px(22f), textX = col.x + side + V35.Px(10f), textW = Mathf.Max(1f, col.xMax - textX), lineH = V35.Px(24f), subH = V35.Px(19f);
            foreach ((string ic, string name, string figure, Color ink, string subText, SlipContent slip) in rows)
            {
                float rowH = pad + lineH + V35.Px(2f) + subH + pad;
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(col.x, y, col.width, 1f), PlateRule); }
                DrawV35Icon(new Rect(col.x, y + pad + V35.Px(2f), side, side), ic, PoliSimTheme.TextPrimary);
                GUIStyle fig = V35Mono(16f, ink, bold: true, TextAnchor.MiddleRight);
                float fw = TextWidth(fig, figure) + 2f;
                PlateText(new Rect(col.xMax - fw, y + pad, fw, lineH), figure, fig);
                PlateText(new Rect(textX, y + pad, Mathf.Max(1f, col.xMax - fw - V35.Px(10f) - textX), lineH), name, V35Serif(17f, PoliSimTheme.TextPrimary));
                PlateText(new Rect(textX, y + pad + lineH + V35.Px(2f), textW, subH), subText, subFace);
                _sejmSlips.Add((new Rect(col.x, y, col.width, rowH), slip, "sejmplate:" + ic));
                y += rowH;
            }
        }

        /// <summary>The right column: the polling day by the rule - seven columns from the order's week (Monday first), the order's day outlined in BAD, the
        /// forty-fifth day dashed, the chosen Sunday filled in ink, the first of a month on the plate's tint; a legend under it.</summary>
        private void DrawSejmPlateCalendar(Rect col, SejmDesk.Plate p)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "the polling day");
            y += V35.Px(20f) + V35.Px(8f);
            bool repaint = Event.current.type == EventType.Repaint;
            float cw = V35.Px(34f), ch = V35.Px(26f), g = V35.Px(3f);
            string[] heads = { "M", "T", "W", "T", "F", "S", "S" };
            for (int d = 0; d < 7; d++) { PlateText(new Rect(col.x + d * (cw + g), y, cw, V35.Px(18f)), heads[d], V35Mono(V35.Floor, d == 6 ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleCenter)); }
            y += V35.Px(18f) + g;
            System.DateTime ordered = p.OrderedOn.Date, limit = ordered.AddDays(SejmFormation.ShortenedTermElectionWithinDays);
            int lead = ((int)ordered.DayOfWeek + 6) % 7;   // Monday first
            System.DateTime first = ordered.AddDays(-lead);
            int cells = lead + SejmFormation.ShortenedTermElectionWithinDays + 1;
            int rows = (cells + 6) / 7;
            Color tint = PoliSimTheme.Hex(0xE7DCC4);
            for (int i = 0; i < rows * 7; i++)
            {
                System.DateTime day = first.AddDays(i);
                var r = new Rect(col.x + (i % 7) * (cw + g), y + (i / 7) * (ch + g), cw, ch);
                if (day < ordered || day > limit) { continue; }
                bool chosen = day == p.PollingDay.Date, isOrder = day == ordered, isLimit = day == limit, sunday = day.DayOfWeek == System.DayOfWeek.Sunday;
                if (repaint && chosen) { PoliSimTheme.Rule(r, PoliSimTheme.TextPrimary); }
                else if (repaint && day.Day == 1) { PoliSimTheme.Rule(r, tint); }
                if (isOrder) { Outline(r, PoliSimTheme.Bad, 1.5f); }
                else if (isLimit) { DashedRect(r, PoliSimTheme.TextPrimary); }
                else if (!chosen) { Outline(r, PlateRule, 1f); }
                PlateText(r, day.Day.ToString(CultureInfo.InvariantCulture), V35Mono(V35.Floor, chosen ? PlatePaper : PoliSimTheme.TextPrimary, bold: sunday, TextAnchor.MiddleCenter));
            }
            float calBottom = y + rows * (ch + g);
            _sejmSlips.Add((new Rect(col.x, y, 7 * (cw + g), rows * (ch + g)), new SlipContent("THE POLLING DAY, BY THE RULE")
                .Add("ordered " + ordered.ToString("ddd d MMM", CultureInfo.InvariantCulture))
                .Add("day 45 is " + limit.ToString("ddd d MMM", CultureInfo.InvariantCulture))
                .Add("the last Sunday on or before it: " + p.PollingDay.ToString("d MMM", CultureInfo.InvariantCulture) + ", day " + ((int)(p.PollingDay.Date - ordered).TotalDays).ToString(CultureInfo.InvariantCulture))
                .Add("DECLARED: the 2007 practice · Art. 98.5"), "sejmplate:calendar"));

            // the legend
            y = calBottom + V35.Px(6f);
            float sw = V35.Px(14f), lh = V35.Px(20f);
            GUIStyle face = V35Mono(V35.Floor, PoliSimTheme.TextPrimary);
            var legend = new (int Kind, string Words)[]
            {
                (0, ordered.ToString("d MMM", CultureInfo.InvariantCulture) + " · the term shortened"),
                (1, limit.ToString("d MMM", CultureInfo.InvariantCulture) + " · day 45"),
                (2, p.PollingDay.ToString("d MMM", CultureInfo.InvariantCulture) + " · the last Sunday by day 45"),
                (3, "the 1st of a month"),
            };
            foreach ((int kind, string words) in legend)
            {
                if (y + lh > col.yMax + 1f) { break; }
                var s = new Rect(col.x, y + Mathf.Round((lh - sw) * 0.5f), sw, sw);
                if (kind == 0) { Outline(s, PoliSimTheme.Bad, 1.5f); }
                else if (kind == 1) { DashedRect(s, PoliSimTheme.TextPrimary); }
                else if (repaint) { PoliSimTheme.Rule(s, kind == 2 ? PoliSimTheme.TextPrimary : tint); }
                PlateText(new Rect(s.xMax + V35.Px(8f), y, Mathf.Max(1f, col.xMax - s.xMax - V35.Px(8f)), lh), words, face);
                y += lh;
            }
        }

        /// <summary>A 1 px dashed outline (day 45's cell).</summary>
        private static void DashedRect(Rect r, Color ink)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float dash = Mathf.Max(2f, V35.Px(3f));
            for (float x = r.x; x < r.xMax; x += dash * 2f) { float w = Mathf.Min(dash, r.xMax - x); PoliSimTheme.Rule(new Rect(x, r.y, w, 1f), ink); PoliSimTheme.Rule(new Rect(x, r.yMax - 1f, w, 1f), ink); }
            for (float yy = r.y; yy < r.yMax; yy += dash * 2f) { float hh = Mathf.Min(dash, r.yMax - yy); PoliSimTheme.Rule(new Rect(r.x, yy, 1f, hh), ink); PoliSimTheme.Rule(new Rect(r.xMax - 1f, yy, 1f, hh), ink); }
        }

        /// <summary>The plate's slips, after the slip delay under the pointer drawn over the plate - the crisis plate's rule.</summary>
        private void DrawSejmSlips(Rect bounds)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) { return; }
            if (PoliSim.Testing.CaptureIdentity.Armed) { return; }
            string hovered = null;
            SlipContent open = null;
            Rect anchor = default;
            foreach ((Rect r, SlipContent slip, string id) in _sejmSlips) { if (r.Contains(e.mousePosition)) { hovered = id; open = slip; anchor = r; break; } }
            float now = Time.realtimeSinceStartup;
            if (hovered != _sejmSlipHover) { _sejmSlipHover = hovered; _sejmSlipSince = now; }
            if (hovered == null || now - _sejmSlipSince < SlipDelay) { return; }
            SlipBox(open, e.mousePosition, bounds, 0, out _, out _, anchor, null);
        }
    }
}
