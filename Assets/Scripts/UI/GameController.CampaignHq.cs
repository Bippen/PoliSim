using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S4 (Design's return of 2026-10-30, `PoliSim-captures/design/RETURN_2026-10-30_S4/`: RETURN.md and the board `S4 Campaign HQ.dc.html`, frames 4a-4j):
    /// **THE CAMPAIGN HQ, RE-LAID AT THE 14 PX FLOOR.** One head and one timeline over three columns, each with one job: the STANDING at the left (the race,
    /// the chest, the organisation), the GROUND in the middle (the regions as a tile map, the selected region's plate under it), the DAY at the right (the
    /// verbs, the composer, the queue, the field reports). Every measure is the board's px at 1280 (x 2 at 2560): the page is the board's 1176 x 664 from
    /// (94, 47), laid on the sheet's inner rect by its width and height; type is <see cref="V35.FontPx"/>, never below the floor.
    ///
    /// <para><b>K320 on top of the board</b> (Elias, 2026-10-30; K320 (8)): every string at rest is held to the caps - a label at most three words, a caption
    /// one line never cut, no sentence - and where the board's own words break a cap they are built decluttered and moved to the slip (this page's book,
    /// <see cref="DrawSlips"/>); the list of what moved is `docs/declutter/moved_s4hq.tsv`.</para>
    ///
    /// <para><b>One clock</b> (bug 4): the head, the timeline and the days to polling day read the WORLD's date - the strip's - where the HQ is live; the queue
    /// still steps on the run's next day, and what may be queued is gated by that day (<see cref="CampaignSnapshot.DaysUntilElection"/>).</para>
    ///
    /// <para><b>The composer</b> (board 4c-4e): a verb pressed opens its composer - where it lands, its cost, the attack, the topic and the size - and Queue
    /// queues it; the preview is outlined on the day's hours and the cap until then. The party inks of the race and the map are drawn by
    /// `GameController.CampaignRace.cs`, the one file of the HQ allowed them (`PartyInkDrawSiteCheck`).</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>The board's page: its width and height in px at 1280, from its origin (94, 47).</summary>
        private const float HqBoardWidth = 1176f, HqBoardHeight = 664f, HqOriginX = 94f, HqOriginY = 47f;

        /// <summary>The board's bar track (#DDD1B8) - the race's bars sit on it.</summary>
        private static readonly Color HqTrack = PoliSimTheme.Hex(0xDDD1B8);

        /// <summary>The sheet's inner rect this pass, and the board's px across and down it.</summary>
        private Rect _hqPage;
        private float _hqUx = 1f, _hqUy = 1f;

        /// <summary>The verb open in the composer, and the election it was opened in (a composer from another campaign reads as none).</summary>
        private CampaignActionKind? _hqComposer;
        private System.DateTime _hqComposerElection;

        private float HqX(float x) => _hqPage.x + (x - HqOriginX) * _hqUx;
        private float HqY(float y) => _hqPage.y + (y - HqOriginY) * _hqUy;
        private float HqW(float w) => Mathf.Round(w * _hqUx);
        private float HqH(float h) => Mathf.Round(h * _hqUy);
        private Rect HqR(float x, float y, float w, float h) => new Rect(Mathf.Round(HqX(x)), Mathf.Round(HqY(y)), HqW(w), HqH(h));

        /// <summary>A rule's thickness at this window - the board's 1, 1.5 and 2 px, x 2 at 2560.</summary>
        private static float HqT(float px) => Mathf.Max(1f, px * V35.Scale);

        private GUIStyle HqMono(float px, Color ink, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft) => V35Mono(px, ink, bold, anchor);

        private GUIStyle HqSerif(float px, Color ink, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            GUIStyle s = V35Serif(px, ink, anchor);
            if (bold) { s.fontStyle = FontStyle.Bold; }
            return s;
        }

        private static float HqWidth(string text, GUIStyle style) => string.IsNullOrEmpty(text) ? 0f : Mathf.Ceil(style.CalcSize(new GUIContent(text)).x);

        /// <summary>
        /// Every label on the HQ, drawn through `PoliSimWidgets.MeasuredLabel` in a rect AS TALL AS ITS FONT'S LINE: the board sets 14 px text on 16 px lines
        /// and a 28 px figure in a 34 px row, but the face's real line is about 1.25 times its size, and the first real films measured hundreds of vertical
        /// overflows where a row was only as tall as its type. The row keeps the board's rhythm - its place and its pitch - and the label's rect grows round
        /// it to the line it needs: about its centre for a middle anchor, upward for a lower one, downward for an upper one.
        /// </summary>
        private static Vector2 HqLabel(Rect r, string text, GUIStyle style)
        {
            if (!string.IsNullOrEmpty(text))
            {
                float need = Mathf.Ceil(style.CalcSize(new GUIContent(text)).y);
                if (need > r.height)
                {
                    float grow = need - r.height;
                    TextAnchor a = style.alignment;
                    bool lower = a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight;
                    bool upper = a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight;
                    r = new Rect(r.x, lower ? r.y - grow : upper ? r.y : r.y - Mathf.Floor(grow * 0.5f), r.width, need);
                }
            }
            return PoliSimWidgets.MeasuredLabel(r, text, style);
        }

        /// <summary>K320: a caption is one line and never cut with "…" - so a line too long for its room is said SHORTER, never cut: the first of
        /// <paramref name="options"/> (longest first) that fits <paramref name="width"/>, else the last (which `MeasuredLabel` shrinks, reporting a shrink
        /// below the floor as an overflow - the film fails on it rather than cutting it).</summary>
        private static string HqFirstFitting(GUIStyle face, float width, params string[] options)
        {
            string last = string.Empty;
            foreach (string o in options)
            {
                if (string.IsNullOrEmpty(o)) { continue; }
                last = o;
                if (face.CalcSize(new GUIContent(o)).x <= width) { return o; }
            }
            return last;
        }

        /// <summary>A region's names, longest first, for <see cref="HqFirstFitting"/>: its tile's long label ("okręg 3 · Wrocław"), its display name ("okręg 3"),
        /// its tile's id ("3", "WN", "VGN").</summary>
        private string[] HqRegionForms(CampaignSnapshot s, string regionName)
        {
            string[] names = s.Hq?.Ground?.Names;
            CampaignTiles.TileMap map = CampaignTiles.For(PlayerCountryId, HqContest(s), names);
            string id = null;
            if (map != null && names != null)
            {
                foreach (CampaignTiles.Tile t in map.Tiles) { int r = CampaignTiles.RegionOf(map, t, names); if (r >= 0 && names[r] == regionName) { id = t.Id; break; } }
            }
            return new[] { CampaignTiles.LabelOf(map, regionName, names), CampaignTiles.Display(regionName), id };
        }

        private static void HqFill(Rect r, Color ink) { if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(r, ink); } }

        /// <summary>A solid edge of <paramref name="t"/> px inside <paramref name="r"/>.</summary>
        private static void HqEdge(Rect r, Color ink, float t)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, t), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - t, r.width, t), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.y, t, r.height), ink);
            PoliSimTheme.Rule(new Rect(r.xMax - t, r.y, t, r.height), ink);
        }

        /// <summary>A dashed edge of <paramref name="t"/> px inside <paramref name="r"/> - the board's dashed rules (closed verbs, NOT POLLED, ABSENT).</summary>
        private static void HqDashed(Rect r, Color ink, float t)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float dash = Mathf.Max(3f, 4f * V35.Scale), gap = Mathf.Max(2f, 3f * V35.Scale);
            for (float x = r.x; x < r.xMax; x += dash + gap)
            {
                float w = Mathf.Min(dash, r.xMax - x);
                PoliSimTheme.Rule(new Rect(x, r.y, w, t), ink);
                PoliSimTheme.Rule(new Rect(x, r.yMax - t, w, t), ink);
            }
            for (float y = r.y; y < r.yMax; y += dash + gap)
            {
                float h = Mathf.Min(dash, r.yMax - y);
                PoliSimTheme.Rule(new Rect(r.x, y, t, h), ink);
                PoliSimTheme.Rule(new Rect(r.xMax - t, y, t, h), ink);
            }
        }

        /// <summary>A stamp (the board's 1.5 px rule round mono 14 bold caps, 5 px either side): at the left edge of <paramref name="at"/>, or ending at its right
        /// edge where <paramref name="right"/>; returns its rect.</summary>
        private Rect HqStamp(Rect at, string text, Color ink, bool dashed = false, bool right = false)
        {
            GUIStyle face = HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter);
            float w = HqWidth(text, face) + V35.Px(12f), h = Mathf.Min(at.height, V35.Px(20f));
            var r = new Rect(right ? at.xMax - w : at.x, at.y + Mathf.Round((at.height - h) * 0.5f), w, h);
            if (dashed) { HqDashed(r, ink, HqT(1.5f)); } else { HqEdge(r, ink, HqT(1.5f)); }
            HqLabel(r, text, face);
            return r;
        }

        /// <summary>The verb faces (the board's "verb states").</summary>
        private enum HqVerbFace { Open, Selected, Closed, Absent, Done }

        /// <summary>One verb cell in its face; true when a cell that is neither closed nor absent is pressed. The slip anchors on <paramref name="slipId"/>.</summary>
        private bool HqVerb(Rect r, string label, HqVerbFace face, string slipId)
        {
            Color ink = face == HqVerbFace.Selected ? PoliSimTheme.ModalBackground : face == HqVerbFace.Closed ? PoliSimTheme.TextMuted : face == HqVerbFace.Done ? PoliSimTheme.TextSecondary : PoliSimTheme.TextPrimary;
            switch (face)
            {
                case HqVerbFace.Open: HqFill(r, PoliSimTheme.BrassLight); HqEdge(r, PoliSimTheme.BrassBorder, HqT(1f)); break;
                case HqVerbFace.Selected: HqFill(r, PoliSimTheme.TextPrimary); break;
                case HqVerbFace.Closed: HqDashed(r, PoliSimTheme.HairlineStrong, HqT(1f)); break;
                case HqVerbFace.Absent: HqDashed(r, PoliSimTheme.TextPrimary, HqT(1.5f)); break;
                default: HqEdge(r, PoliSimTheme.Hairline, HqT(1f)); break;
            }
            GUIStyle text = HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter);
            HqLabel(r, label, text);
            if (face == HqVerbFace.Absent && Event.current.type == EventType.Repaint)
            {
                float w = HqWidth(label, text);
                PoliSimTheme.Rule(new Rect(r.center.x - w * 0.5f, Mathf.Round(r.center.y), w, HqT(1f)), ink);   // struck
            }
            SlipAnchor(r, slipId);
            bool live = face != HqVerbFace.Closed && face != HqVerbFace.Absent;   // a done cell answers too (CLEAR); its caller decides what a press does
            return PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none) && live;
        }

        /// <summary>The world's date where the HQ is live (the strip's and the rail's), else the staged snapshot's own day - THE ONE CLOCK the HQ prints (bug 4).</summary>
        private System.DateTime HqWorldToday(CampaignSnapshot s) =>
            _liveCampaignOpen && _simulationManager != null ? _simulationManager.CurrentDate.Date : s.Today.Date;

        /// <summary>Whether the HQ is over the run-up (the snapshot is the pre-campaign's).</summary>
        private static bool HqRunUp(CampaignSnapshot s) => s.Phase == CampaignPhase.PreCampaign || s.Phase == CampaignPhase.Dormant;

        private static CampaignContest HqContest(CampaignSnapshot s) => s.Hq != null ? s.Hq.Contest : CampaignContest.Chamber;

        /// <summary>A party's mark stem (`PoliticalParty.MarkName`) by its key, or null.</summary>
        private string HqMarkKey(string abbrev)
        {
            foreach (PoliticalParty party in PartySystems.For(PlayerCountryId)) { if (party.Abbrev == abbrev) { return party.MarkName; } }
            return null;
        }

        /// <summary>A party's short name as the authority writes it (a site that DRAWS an abbreviation takes the short name; every lookup keeps the key).</summary>
        private string HqShort(string abbrev) => PartySystems.ShortName(PlayerCountryId, abbrev);

        private static string HqDay(System.DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);
        private static string HqDate(System.DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        private static string HqN(double v) => v.ToString("N0", CultureInfo.InvariantCulture);
        private static string HqF(double v, int decimals) => v.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        /// <summary>The purse's national currency per unit for the staging on screen (`LiveCampaignSetup.PursePerUnit`).</summary>
        private double HqPerUnit() => LiveCampaignSetup.PursePerUnit(PlayerCountryId, _simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged);

        /// <summary>A purse amount as the HQ prints it - the national figure and the purse's word ("240,000 PLN").</summary>
        private string HqMoney(double purseUnits) => Kronor(purseUnits) + " " + Purse;

        /// <summary>The chamber, or the office, the race is for.</summary>
        private string HqRaceName(CampaignSnapshot s)
        {
            CampaignContest contest = HqContest(s);
            if (contest == CampaignContest.PresidentialFirstVote) { return "The President · first round " + HqDate(s.Calendar.ElectionDate); }
            if (contest == CampaignContest.PresidentialRunOff) { return "The President · run-off " + HqDate(s.Calendar.ElectionDate); }
            string chamber = PlayerCountryId == CountryId.Sweden ? "The Riksdag" : PlayerCountryId == CountryId.Poland ? "The Sejm" : PlayerCountryId == CountryId.Germany ? "The Bundestag"
                : PlayerCountryId == CountryId.USA ? "The President" : "The election";
            return chamber + " · polling day " + HqDate(s.Calendar.ElectionDate);
        }

        /// <summary>The day the final week opens - the first day a phone bank is legal (`GotvTurnout.PhoneFinalWeekDays` before polling day, as the run refuses it).</summary>
        private static System.DateTime HqFinalWeekOpens(CampaignSnapshot s) => s.Calendar.ElectionDate.AddDays(-GotvTurnout.PhoneFinalWeekDays);

        // ------------------------------------------------------------------------------------------
        // The stage.
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Campaign HQ in the frame's content column. Composed as `DrawDeskStage` is: the caller has already taken the box's padding and margin out of
        /// `availableHeight`, so it IS the inner height and must not be reserved against twice (instance #12).
        /// </summary>
        private void DrawCampaignHqStage(float availableHeight, float availableWidth, CampaignSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight, GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignInnerRect = inner; }
            else if (_campaignInnerRect.width > 1f) { inner = _campaignInnerRect; }

            _hqPage = inner;
            _hqUx = inner.width / HqBoardWidth;
            _hqUy = inner.height / HqBoardHeight;
            KeepHqComposer(snapshot);

            bool guarded = V35.FloorGuarded;
            V35.FloorGuarded = true;   // §726: a shrink below the floor on this page is an overflow
            BeginSlipAnchors();
            var book = new PeopleSlips.Book();
            try
            {
                DrawHqHead(snapshot, book);
                DrawHqTimeline(snapshot, book);
                DrawHqStanding(snapshot, book);
                DrawHqGround(snapshot, book);
                DrawHqDay(snapshot, book);
            }
            finally { V35.FloorGuarded = guarded; }   // never left on for the pages after this one
            if (!DeskProvenance.On) { DrawSlips(book, inner); }
        }

        /// <summary>The composer closes when its campaign is gone, its verb is no longer legal on the queue's day, or the HQ is a staged film.</summary>
        private void KeepHqComposer(CampaignSnapshot s)
        {
            if (!_hqComposer.HasValue) { return; }
            if (!_liveCampaignOpen || s.Calendar.ElectionDate != _hqComposerElection) { _hqComposer = null; return; }
            CampaignActionKind kind = _hqComposer.Value;
            bool open = HqRunUp(s)
                ? kind == CampaignActionKind.EstablishOffice || kind == CampaignActionKind.PrepareAdvertising || kind == CampaignActionKind.Fundraise || kind == CampaignActionKind.CommissionPolling
                : kind == CampaignActionKind.CommissionPolling || kind == HqPollRegion || (kind == CampaignActionKind.PhoneBank && s.DaysUntilElection <= GotvTurnout.PhoneFinalWeekDays)
                    || (System.Array.IndexOf(HqEight, kind) >= 0 && CampaignLegality.IsLegal(kind, s.Phase));
            if (!open) { _hqComposer = null; }
        }

        /// <summary>Opens (or, pressed again, closes) the composer on <paramref name="kind"/> - the verb cells' press, and the film's way of opening one.</summary>
        private void ToggleHqComposer(CampaignActionKind kind, CampaignSnapshot s)
        {
            if (_hqComposer.HasValue && _hqComposer.Value == kind && _hqComposerElection == s.Calendar.ElectionDate) { _hqComposer = null; return; }
            _hqComposer = kind;
            _hqComposerElection = s.Calendar.ElectionDate;
        }

        /// <summary>The film's hook: the composer opened on <paramref name="kind"/> over the live HQ (null closes it).</summary>
        internal void ComposeForFilm(CampaignActionKind? kind)
        {
            _hqComposer = kind;
            _hqComposerElection = _simulationManager?.PlayerCampaign?.Setup.Calendar.ElectionDate ?? _simulationManager?.PlayerPreCampaign?.Staged.Calendar.ElectionDate ?? System.DateTime.MinValue;
        }

        // ------------------------------------------------------------------------------------------
        // The head (94, 47) 1176 x 38: the mark, "Campaign", the race, the phase, the days.
        // ------------------------------------------------------------------------------------------
        private void DrawHqHead(CampaignSnapshot s, PeopleSlips.Book book)
        {
            Rect r = HqR(94f, 47f, 1176f, 38f);
            float x = r.x;
            float markSide = V35.Px(22f);
            Texture2D mark = IconLibrary.GetPartyMark(s.MarkKey);
            if (mark != null && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(new Rect(x, r.y + Mathf.Round((r.height - markSide) * 0.5f), markSide, markSide), mark, ScaleMode.ScaleToFit);   // authored colours, untinted (the accessor's contract)
            }
            var markRect = new Rect(x, r.y + Mathf.Round((r.height - markSide) * 0.5f), markSide, markSide);
            SlipAnchor(markRect, "hq:party");
            book.Anchors["hq:party"] = new SlipContent(HqShort(s.PartyName) + " · " + s.CountryName).Add("your party's campaign");
            x += markSide + V35.Px(12f);
            GUIStyle title = HqSerif(28f, PoliSimTheme.TextPrimary, bold: true);
            float titleW = HqWidth("Campaign", title);
            HqLabel(new Rect(x, r.y, titleW, r.height), "Campaign", title);
            x += titleW + V35.Px(12f);

            // the right end: the days, then the phase cells
            float right = r.xMax;
            System.DateTime today = HqWorldToday(s);
            int days = s.Calendar.DaysUntilElection(today);
            GUIStyle daysFigure = HqMono(22f, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle daysWord = HqMono(14f, PoliSimTheme.TextMuted);
            string figure = days > 0 ? days.ToString(CultureInfo.InvariantCulture) : null;
            string word = days > 0 ? (days == 1 ? "day to polling day" : "days to polling day") : days == 0 ? "polling day" : "poll closed";
            float wordW = HqWidth(word, daysWord), figW = HqWidth(figure, daysFigure);
            HqLabel(new Rect(right - wordW, r.y, wordW, r.height), word, daysWord);
            if (figure != null) { HqLabel(new Rect(right - wordW - V35.Px(6f) - figW, r.y, figW, r.height), figure, daysFigure); }
            var daysRect = new Rect(right - wordW - (figure != null ? V35.Px(6f) + figW : 0f), r.y, wordW + (figure != null ? V35.Px(6f) + figW : 0f), r.height);
            SlipAnchor(daysRect, "hq:days");
            book.Anchors["hq:days"] = new SlipContent("POLLING DAY · " + HqDate(s.Calendar.ElectionDate).ToUpperInvariant())
                .Add("today " + HqDate(today) + " · the world's date")
                .Add("the queue runs " + HqDate(s.Today) + ", when the clock moves");
            right = daysRect.x - V35.Px(10f);

            string[] phases;
            int current;
            HqPhases(s, today, out phases, out current);
            GUIStyle phaseFace = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float cellH = V35.Px(26f), gap = V35.Px(3f);
            float phasesW = 0f;
            foreach (string p in phases) { phasesW += HqWidth(p, phaseFace) + V35.Px(20f) + gap; }
            phasesW -= gap;
            float px = right - phasesW;
            var phaseRow = new Rect(px, r.y + Mathf.Round((r.height - cellH) * 0.5f), phasesW, cellH);
            for (int i = 0; i < phases.Length; i++)
            {
                float w = HqWidth(phases[i], phaseFace) + V35.Px(20f);
                var cell = new Rect(px, phaseRow.y, w, cellH);
                Color ink = i == current ? PoliSimTheme.ModalBackground : i < current ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;
                if (i == current) { HqFill(cell, PoliSimTheme.TextPrimary); }
                else if (i < current) { HqEdge(cell, PoliSimTheme.HairlineStrong, HqT(1f)); }
                else { HqDashed(cell, PoliSimTheme.Hairline, HqT(1f)); }
                HqLabel(cell, phases[i], HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter));
                px += w + gap;
            }
            SlipAnchor(phaseRow, "hq:phase");
            book.Anchors["hq:phase"] = HqContest(s) == CampaignContest.Chamber
                ? new SlipContent("THE PHASE").Add("run-up · offices, hires, polls, TV buys").Add("campaign · the eight verbs, 12 h a day").Add("final week · PHONES opens")
                : new SlipContent("THE PHASE").Add("run-up · first round · run-off").Add("run-off · the two finalists only");
            right = phaseRow.x - V35.Px(10f);

            // the controls just left of the phase, always drawn - the opening's exit holds the clock until it is pressed, so it is never dropped for room
            GUIStyle chip = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float chipH = V35.Px(26f);
            float cy = r.y + Mathf.Round((r.height - chipH) * 0.5f);
            if (DeclaredPageAvailable())
            {
                const string declared = "DECLARED";
                float w = HqWidth(declared, chip) + V35.Px(20f);
                var d = new Rect(right - w, cy, w, chipH);
                if (HqVerb(d, declared, HqVerbFace.Open, "hq:declared")) { OpenLiveDeclared(); }
                book.Anchors["hq:declared"] = new SlipContent("DECLARED").Add("who will govern with whom").Add("the parties' own words, dated");
                right = d.x - V35.Px(8f);
            }
            if (HasPendingCampaignOpening())
            {
                const string trail = "TAKE THE TRAIL";
                float w = HqWidth(trail, chip) + V35.Px(20f);
                var t = new Rect(right - w, cy, w, chipH);
                if (HqVerb(t, trail, HqVerbFace.Open, "hq:trail")) { AcknowledgeCampaignOpening(); }
                book.Anchors["hq:trail"] = new SlipContent("TAKE THE TRAIL").Add("the campaign has opened · the clock waits").Add("pressed, the clock runs again");
                right = t.x - V35.Px(8f);
            }

            // the race, said shorter where the controls leave it less room (K320: never cut)
            GUIStyle race = HqSerif(16f, PoliSimTheme.TextSecondary);
            string raceName = HqRaceName(s);
            int dot = raceName.IndexOf(" · ", System.StringComparison.Ordinal);
            float raceRoom = Mathf.Max(1f, right - V35.Px(6f) - x);
            string raceShown = HqFirstFitting(race, raceRoom, raceName, dot > 0 ? raceName.Substring(0, dot) + " · " + HqDate(s.Calendar.ElectionDate) : raceName, dot > 0 ? raceName.Substring(0, dot) : raceName);
            var raceRect = new Rect(x, r.y, Mathf.Min(HqWidth(raceShown, race), raceRoom), r.height);
            HqLabel(raceRect, raceShown, race);
            SlipAnchor(raceRect, "hq:race");
            book.Anchors["hq:race"] = HqRaceSlip(s);
        }

        /// <summary>The head's phase cells and the current one: the chamber's run-up, campaign and final week; a presidential race's run-up, first round and run-off.</summary>
        private static void HqPhases(CampaignSnapshot s, System.DateTime today, out string[] phases, out int current)
        {
            CampaignContest contest = HqContest(s);
            CampaignPhase phase = s.Calendar.PhaseOn(today);
            if (contest == CampaignContest.Chamber)
            {
                phases = new[] { "Run-up", "Campaign", "Final week" };
                current = phase == CampaignPhase.PreCampaign || phase == CampaignPhase.Dormant ? 0
                    : phase == CampaignPhase.Concluded ? -1
                    : s.Calendar.DaysUntilElection(today) <= GotvTurnout.PhoneFinalWeekDays ? 2 : 1;
                return;
            }
            phases = new[] { "Run-up", "First round", "Run-off" };
            current = contest == CampaignContest.PresidentialRunOff ? 2 : phase == CampaignPhase.PreCampaign || phase == CampaignPhase.Dormant ? 0 : phase == CampaignPhase.Concluded ? -1 : 1;
        }

        /// <summary>The race name's slip: the chamber's seats and rule, or the presidency's two rounds.</summary>
        private SlipContent HqRaceSlip(CampaignSnapshot s)
        {
            CampaignContest contest = HqContest(s);
            var slip = new SlipContent(HqRaceName(s).ToUpperInvariant());
            if (contest != CampaignContest.Chamber)
            {
                TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(PlayerCountryId);
                slip.Add(rule != null ? "two rounds · the top two meet " + rule.RunOffDay.ToString(CultureInfo.InvariantCulture) + " days on" : "two rounds");
                slip.Add("contacts land in the regions");
                return slip;
            }
            int regions = s.Hq?.Ground?.Names?.Length ?? 0;
            if (regions > 0) { slip.Add(regions.ToString(CultureInfo.InvariantCulture) + " regions · local acts land in one"); }
            slip.Add("national acts reach the whole country");
            return slip;
        }

        // ------------------------------------------------------------------------------------------
        // The timeline (94, 93) 1176 x 30: the track, the final week, today, the debates, the polling day.
        // ------------------------------------------------------------------------------------------
        private void DrawHqTimeline(CampaignSnapshot s, PeopleSlips.Book book)
        {
            Rect r = HqR(94f, 93f, 1176f, 30f);
            System.DateTime today = HqWorldToday(s);
            bool runUp = HqRunUp(s);
            System.DateTime start = runUp ? s.Calendar.PreCampaignStart : s.Calendar.CampaignStart;
            int total = Mathf.Max(1, (int)(s.Calendar.ElectionDate - start).TotalDays);
            int at = Mathf.Clamp((int)(today - start).TotalDays, 0, total);
            float X(int d) => r.x + Mathf.Round(r.width * d / (float)total);
            float trackH = V35.Px(6f);
            HqFill(new Rect(r.x, r.y, r.width, trackH), PoliSimTheme.RuleRow);
            int finalWeek = total - GotvTurnout.PhoneFinalWeekDays;
            int campaignFrom = runUp ? (int)(s.Calendar.CampaignStart - start).TotalDays : finalWeek;
            HqFill(new Rect(X(campaignFrom), r.y, Mathf.Max(1f, X(total) - X(campaignFrom)), trackH), PoliSimTheme.Hairline);
            HqFill(new Rect(r.x, r.y, Mathf.Max(2f, X(at) - r.x), trackH), PoliSimTheme.TextPrimary);

            GUIStyle label = HqMono(14f, PoliSimTheme.TextSecondary);
            GUIStyle bold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
            float tickW = HqT(2f), tickH = V35.Px(14f), ly = r.y + V35.Px(11f), lh = V35.Px(18f);
            void Tick(int d, Color ink) => HqFill(new Rect(X(d) - tickW * 0.5f, r.y - V35.Px(4f), tickW, tickH), ink);

            // today, at the left; the polling day's date, at the right
            int dayNumber = runUp ? at : s.Calendar.CampaignDaysElapsed(today);
            string todayText = "Today " + HqDate(today) + " · " + (runUp ? "run-up" : HqContest(s) == CampaignContest.PresidentialRunOff ? "run-off" : "campaign") + " day "
                + dayNumber.ToString(CultureInfo.InvariantCulture) + " of " + (runUp ? (int)(s.Calendar.CampaignStart - start).TotalDays : total).ToString(CultureInfo.InvariantCulture);
            float todayW = HqWidth(todayText, bold);
            HqLabel(new Rect(r.x, ly, todayW, lh), todayText, bold);
            string pollText = HqDay(s.Calendar.ElectionDate);
            float pollW = HqWidth(pollText, bold);
            HqLabel(new Rect(r.xMax - pollW, ly, pollW, lh), pollText, bold);
            float leftClear = r.x + todayW + V35.Px(10f), rightClear = r.xMax - pollW - V35.Px(10f);
            Tick(total, PoliSimTheme.TextPrimary);

            // the campaign's opening (the run-up) or the final week and the debates (the campaign) beside their ticks, where clear of today and the date
            var lines = new List<string> { "today " + HqDate(today) + " · opened " + HqDate(start) };
            var debates = new List<string>();
            if (runUp)
            {
                Tick(campaignFrom, PoliSimTheme.TextSecondary);
                string opens = "campaign · " + HqDay(s.Calendar.CampaignStart);
                float w = HqWidth(opens, label);
                float lx = X(campaignFrom) - V35.Px(6f) - w;
                if (lx >= leftClear) { HqLabel(new Rect(lx, ly, w, lh), opens, label); }
                lines.Add("the campaign opens " + HqDate(s.Calendar.CampaignStart));
            }
            else
            {
                // §961: each label beside a tick claims its span, the final week's first (the phase's edge, and PHONES), then each debate where clear of
                // every label already drawn - Poland's second debate was drawn over "final week · phones". A label that gives way keeps its tick and its slip line.
                var taken = new List<Vector2>();
                bool Clear(float lx, float w)
                {
                    if (lx < leftClear || lx + w > rightClear) { return false; }
                    foreach (Vector2 span in taken) { if (lx < span.y + V35.Px(8f) && lx + w > span.x - V35.Px(8f)) { return false; } }
                    return true;
                }
                string finalWeekLine = null;
                if (finalWeek > 0)
                {
                    Tick(finalWeek, PoliSimTheme.TextSecondary);
                    string fw = HqContest(s) == CampaignContest.Chamber ? "final week · phones" : "final week";
                    float w = HqWidth(fw, label);
                    float lx = at < finalWeek ? X(finalWeek) - V35.Px(6f) - w : X(finalWeek) + V35.Px(6f);
                    if (Clear(lx, w)) { HqLabel(new Rect(lx, ly, w, lh), fw, label); taken.Add(new Vector2(lx, lx + w)); }
                    finalWeekLine = "final week from " + HqDate(HqFinalWeekOpens(s)) + " · PHONES opens";
                }
                if (s.DebateDays != null)
                {
                    foreach (int d in s.DebateDays)
                    {
                        if (d < 0 || d > total) { continue; }
                        Tick(d, PoliSimTheme.TextSecondary);
                        string debate = "debate " + HqDay(s.Calendar.CampaignStart.AddDays(d));
                        float w = HqWidth(debate, label);
                        float lx = X(d) + V35.Px(6f);
                        if (Clear(lx, w)) { HqLabel(new Rect(lx, ly, w, lh), debate, label); taken.Add(new Vector2(lx, lx + w)); }
                        debates.Add(HqDay(s.Calendar.CampaignStart.AddDays(d)));
                    }
                }
                if (debates.Count > 0)
                {
                    string next = s.NextDebateDay >= 0 ? " · next in " + Mathf.Max(0, (int)(s.Calendar.CampaignStart.AddDays(s.NextDebateDay) - today).TotalDays).ToString(CultureInfo.InvariantCulture) + " days" : " · past";
                    lines.Add("debates " + string.Join(", ", debates.ToArray()) + next);
                    lines.Add("the two leading the published poll stand");
                }
                if (finalWeekLine != null) { lines.Add(finalWeekLine); }
            }
            Tick(at, PoliSimTheme.TextPrimary);
            lines.Add("polling day " + HqDate(s.Calendar.ElectionDate));
            SlipAnchor(r, "hq:timeline");
            var slip = new SlipContent("THE CALENDAR");
            if (lines.Count > 4) { lines.RemoveAt(lines.Count - 2); }   // the final week's line gives way (its tick and its PHONES say it)
            for (int i = 0; i < lines.Count && i < 4; i++) { slip.Add(lines[i]); }
            book.Anchors["hq:timeline"] = slip;
        }

        // ------------------------------------------------------------------------------------------
        // The left column (94, 131) 330 wide: the race (CampaignRace.cs), the source, the economy, the chest and the cap, the day, fundraising, the
        // organisation.
        // ------------------------------------------------------------------------------------------
        private void DrawHqStanding(CampaignSnapshot s, PeopleSlips.Book book)
        {
            float x = Mathf.Round(HqX(94f)), w = HqW(330f);
            float y = Mathf.Round(HqY(131f));
            float bottom = _hqPage.yMax;
            bool poland = !double.IsNaN(HqCap());
            // what the column holds under the race, at the board's heights - the race takes what is left of the page (its rows' pitch shrinks to fit)
            float under = HqH(20f + 28f + 14f + 26f + 34f + (poland ? 34f : 0f) + 30f + 28f + 14f + 26f + 32f + 26f + 26f);
            y = DrawHqRace(new Rect(x, y, w, Mathf.Max(HqH(80f), bottom - y - under)), s, book);

            // the source, and the economy over the term
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted);
            string source = HqSourceLine(s);
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            var sourceRect = new Rect(x, y, w, HqH(20f));
            HqLabel(sourceRect, source, muted);
            y += HqH(20f);
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            var econ = new Rect(x, y, w, HqH(28f));
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            string econFigure = string.Format(CultureInfo.InvariantCulture, "{0:F0} / 100", s.PerceivedEconomyIndex);
            GUIStyle fig = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
            // §961: the name, the figure and the AS PUBLISHED stamp share one row - the 330-wide column cannot hold "Economy, the term" with both (the
            // figure ran under the stamp, "90 / 100AS PUBLISHED"), so the name gives way to "Economy" and "over the term" stays the slip's head (K320)
            float stampW = HqWidth("AS PUBLISHED", HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter)) + V35.Px(12f);
            string econName = "Economy, the term";
            if (HqWidth(econName, name16) + HqWidth(econFigure, fig) + stampW + V35.Px(16f) > econ.width) { econName = "Economy"; }
            float nameW = HqWidth(econName, name16);
            HqLabel(new Rect(econ.x, econ.y, nameW, econ.height), econName, name16);
            HqLabel(new Rect(econ.x + nameW + V35.Px(8f), econ.y, HqWidth(econFigure, fig), econ.height), econFigure, fig);
            HqStamp(econ, "AS PUBLISHED", PoliSimTheme.TextPrimary, right: true);
            SlipAnchor(econ, "hq:economy");
            book.Anchors["hq:economy"] = new SlipContent("ECONOMY OVER THE TERM · " + econFigure).Add("as published · what the country was told").Add("not the true state").Add("the change since the government took office");
            y += HqH(28f);

            y = DrawHqChest(x, y, w, s, book);
            y = DrawHqHours(x, y, w, s, book);
            y = DrawHqFundraising(x, y, w, s, book);
            DrawHqOrganisation(x, y, w, s, book);
        }

        /// <summary>The race's source line under its rows: the poll's sample and field date, or the last count's honesty.</summary>
        private string HqSourceLine(CampaignSnapshot s)
        {
            CampaignPollSource source = s.Hq != null ? s.Hq.PollSource : s.LatestPoll.SampleSize > 0 ? CampaignPollSource.PublicTracker : CampaignPollSource.LastCount;
            if (s.LatestPoll.House == null) { return "no poll yet"; }
            if (source == CampaignPollSource.LastCount || s.LatestPoll.SampleSize <= 0) { return "the last count · no sampling error"; }
            string line = "n " + HqN(s.LatestPoll.SampleSize) + " · fielded " + HqDate(s.LatestPoll.FieldDate);
            if (HqContest(s) == CampaignContest.PresidentialRunOff)
            {
                int a = -1;
                for (int i = 0; i < s.LatestPoll.PartyCount; i++) { if (s.Hq == null || s.Hq.Stands(i)) { a = i; break; } }
                if (a >= 0) { line += " · ±" + HqF(s.LatestPoll.MarginOfErrorPp(a), 1) + " each"; }
            }
            return line;
        }

        /// <summary>The player's statutory cap in national currency (Poland's, K167), NaN where the staging carries none.</summary>
        private double HqCap() =>
            LiveCampaignSetup.SpendingLimit(_simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged, _simulationManager?.PlayerPartyIndexForCampaign() ?? -1);

        /// <summary>What the queue and the composer would spend, in purse units and hours: the queue's sums, and the composer's verb at its size.</summary>
        private void HqSpends(CampaignSnapshot s, out double queuedMoney, out double queuedHours, out double previewMoney, out double previewHours)
        {
            queuedMoney = queuedHours = previewMoney = previewHours = 0.0;
            if (s.Queue != null)
            {
                foreach (QueuedAction q in s.Queue) { queuedMoney += q.MoneyCost; queuedHours += q.Hours; }
            }
            if (_hqComposer.HasValue) { HqComposerCost(s, _hqComposer.Value, out previewMoney, out previewHours); }
        }

        /// <summary>The chest, and Poland's cap beside it (board 4b's bar: spent + queued + preview + open = the cap; the open part is the chest).</summary>
        private float DrawHqChest(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            double cap = HqCap();
            bool hasCap = !double.IsNaN(cap) && cap > 0.0;
            double perUnit = HqPerUnit();
            double chestNational = s.Resources.Money * perUnit;
            y += HqH(14f);
            var head = new Rect(x, y, w, HqH(26f));
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth("The chest", head18), head.height), "The chest", head18);
            string note = hasCap ? "the cap − spent" : "no cap on disk";
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(head.xMax - HqWidth(note, muted), head.y, HqWidth(note, muted), head.height), note, muted);
            y += head.height;

            var row = new Rect(x, y, w, HqH(34f));
            HqSpends(s, out double queuedMoney, out double _, out double previewMoney, out double _);
            double spent = hasCap ? System.Math.Max(0.0, cap - chestNational) : 0.0;
            bool overHeadroom = hasCap && chestNational > cap - spent + 1e-6;
            GUIStyle chest = HqMono(28f, overHeadroom ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, bold: true);
            string chestText = Kronor(s.Resources.Money);
            float chestW = HqWidth(chestText, chest);
            HqLabel(new Rect(row.x, row.y, chestW, row.height), chestText, chest);
            GUIStyle cur = HqMono(14f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(row.x + chestW + V35.Px(8f), row.y + V35.Px(6f), HqWidth(Purse, cur), row.height - V35.Px(6f)), Purse, cur);
            double spentShare = hasCap ? spent / cap : s.MoneySpentShare;
            string spentText = string.Format(CultureInfo.InvariantCulture, "{0:F0} % spent", 100.0 * System.Math.Max(0.0, spentShare));
            GUIStyle bold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(row.xMax - HqWidth(spentText, bold), row.y + V35.Px(6f), HqWidth(spentText, bold), row.height - V35.Px(6f)), spentText, bold);
            SlipAnchor(row, "hq:chest");
            book.Anchors["hq:chest"] = hasCap
                ? new SlipContent("THE CHEST · " + HqN(chestNational) + " " + Purse).Add("what you may still spend").Add("the cap less what is spent").Add("spent " + HqN(spent) + " " + Purse)
                : new SlipContent("THE CHEST · " + HqN(chestNational) + " " + Purse).Add(string.Format(CultureInfo.InvariantCulture, "{0:F0} % of the chest spent", 100.0 * System.Math.Max(0.0, spentShare)))
                    .Add("no statutory cap on disk for this country");
            y += row.height;
            if (!hasCap) { return y; }

            // the cap: one bar is the whole legal limit
            y += HqH(2f);
            var bar = new Rect(x, y, w, HqH(12f));
            double queuedNational = queuedMoney * perUnit, previewNational = previewMoney * perUnit;
            float Px(double v) => (float)(bar.width * System.Math.Max(0.0, System.Math.Min(1.0, v / cap)));
            HqFill(bar, PoliSimTheme.Card);
            HqEdge(bar, PoliSimTheme.HairlineStrong, HqT(1f));
            float xs = Px(spent), xq = Px(spent + queuedNational), xp = Px(spent + queuedNational + previewNational);
            HqFill(new Rect(bar.x, bar.y, xs, bar.height), PoliSimTheme.TextPrimary);
            HqFill(new Rect(bar.x + xs, bar.y, Mathf.Max(0f, xq - xs), bar.height), PoliSimTheme.Draft);
            if (xp > xq) { HqEdge(new Rect(bar.x + xq, bar.y, xp - xq, bar.height), PoliSimTheme.Draft, HqT(1.5f)); }
            HqFill(new Rect(bar.xMax - HqT(2f), bar.y - V35.Px(4f), HqT(2f), V35.Px(20f)), PoliSimTheme.TextPrimary);
            y += bar.height;
            var labels = new Rect(x, y, w, HqH(20f));
            GUIStyle mono = HqMono(14f, PoliSimTheme.TextPrimary, anchor: TextAnchor.LowerLeft);
            string left = "spent " + HqF(spent / 1e6, 1) + " m";
            string rightText = "the cap " + HqN(cap) + " †";
            HqLabel(new Rect(labels.x, labels.y, HqWidth(left, mono), labels.height), left, mono);
            GUIStyle monoRight = HqMono(14f, PoliSimTheme.TextPrimary, anchor: TextAnchor.LowerRight);
            HqLabel(new Rect(labels.xMax - HqWidth(rightText, monoRight), labels.y, HqWidth(rightText, monoRight), labels.height), rightText, monoRight);
            var capRect = new Rect(x, bar.y, w, bar.height + labels.height);
            SlipAnchor(capRect, "hq:cap");
            string line = LiveCampaignSetup.SpendingLimitLine(_simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged, _simulationManager?.PlayerPartyIndexForCampaign() ?? -1);
            book.Anchors["hq:cap"] = new SlipContent("THE CAP · " + HqN(cap) + " " + Purse)
                .Add("the electoral code's limit for this committee")
                .Add("ink spent · amber queued · outline the preview")
                .Add(line != null ? "† " + line.ToLowerInvariant() : "† as on disk");
            y += labels.height;
            return y;
        }

        /// <summary>The day's twelve hours (bug 5: the bar read the pool's stock - full when nothing was queued; the cells now fill as hours are queued, so empty
        /// is free, and the composer's preview is outlined). In the run-up the cells are dashed, with the day the hours open.</summary>
        private float DrawHqHours(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            y += HqH(2f);
            var row = new Rect(x, y, w, HqH(28f));
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(row.x, row.y, HqW(70f), row.height), "The day", name16);
            bool runUp = HqRunUp(s);
            HqSpends(s, out double _, out double queued, out double _, out double preview);
            int cells = Mathf.RoundToInt((float)CampaignEconomy.HoursPerCampaignDay);
            int q = Mathf.Clamp(Mathf.RoundToInt((float)queued), 0, cells), p = Mathf.Clamp(Mathf.RoundToInt((float)preview), 0, cells - q);
            float cw = HqW(10f), ch = V35.Px(16f), gap = HqW(2f);
            float cx = row.x + HqW(70f) + V35.Px(8f);
            for (int i = 0; i < cells; i++)
            {
                var c = new Rect(cx + i * (cw + gap), row.y + Mathf.Round((row.height - ch) * 0.5f), cw, ch);
                if (runUp) { HqDashed(c, PoliSimTheme.Hairline, HqT(1f)); }
                else if (i < q) { HqFill(c, PoliSimTheme.Draft); }
                else if (i < q + p) { HqEdge(c, PoliSimTheme.Draft, HqT(1.5f)); }
                else { HqFill(c, PoliSimTheme.Card); HqEdge(c, PoliSimTheme.HairlineStrong, HqT(1f)); }
            }
            string figure = runUp ? "from " + HqDay(s.Calendar.CampaignStart)
                : string.Format(CultureInfo.InvariantCulture, "{0:0.#} of {1:F0} h", queued + preview, CampaignEconomy.HoursPerCampaignDay);
            bool over = !runUp && queued > s.Resources.Hours + 1e-9;
            GUIStyle bold = HqMono(14f, over ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(row.xMax - HqWidth(figure, bold), row.y, HqWidth(figure, bold), row.height), figure, bold);
            SlipAnchor(row, "hq:day");
            book.Anchors["hq:day"] = runUp
                ? new SlipContent("THE DAY").Add("12 hours a day from " + HqDate(s.Calendar.CampaignStart)).Add("run-up acts cost money, not hours")
                : new SlipContent("THE DAY · 12 HOURS").Add("amber queued · outline the preview · paper free")
                    .Add(string.Format(CultureInfo.InvariantCulture, "queued {0:0.#} h · preview {1:0.#} h · left {2:0.#} h", queued, preview, s.Resources.Hours))
                    .Add(over ? "over the day's hours · the run refuses the spend" : "the day closes when the clock runs");
            return y + row.height;
        }

        /// <summary>The fundraising row (Design's item 6): ABSENT in a dashed rule where no postal tariff is on disk; else the letter's price.</summary>
        private float DrawHqFundraising(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            var row = new Rect(x, y, w, HqH(28f));
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            float nameW = HqWidth("Fundraising", name16);
            HqLabel(new Rect(row.x, row.y, nameW, row.height), "Fundraising", name16);
            double tariff = PreCampaignRun.LetterTariff(PlayerCountryId);
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleRight);
            if (double.IsNaN(tariff))
            {
                HqStamp(new Rect(row.x + nameW + V35.Px(8f), row.y, row.width, row.height), SymbolRegistry.Word(Symbol.Absent), PoliSimTheme.TextPrimary, dashed: true);
                const string reason = "no tariff on disk";
                HqLabel(new Rect(row.xMax - HqWidth(reason, muted), row.y, HqWidth(reason, muted), row.height), reason, muted);
                book.Anchors["hq:fund"] = new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("no postal tariff on disk for this country").Add("no letter can be priced").Add("the verb opens where a tariff is sourced");
            }
            else
            {
                string price = string.Format(CultureInfo.InvariantCulture, "{0:N2} {1} a letter", tariff, Purse);
                GUIStyle fig = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
                HqLabel(new Rect(row.xMax - HqWidth(price, fig), row.y, HqWidth(price, fig), row.height), price, fig);
                PreCampaignRun.State pre = _simulationManager?.PlayerPreCampaign;
                var slip = new SlipContent("FUNDRAISING").Add("a mailing to the party's supporters").Add("the run-up's verb · the campaign raises none");
                if (pre != null && _simulationManager.PlayerCampaign == null) { slip.Add(PreCampaignRun.FundraiseLine(pre, Purse).ToLowerInvariant()); }
                book.Anchors["hq:fund"] = slip;
            }
            SlipAnchor(row, "hq:fund");
            return y + row.height;
        }

        /// <summary>The organisation: the payroll, the five posts as cells, the volunteers, the offices.</summary>
        private void DrawHqOrganisation(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            y += HqH(14f);
            var head = new Rect(x, y, w, HqH(26f));
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth("The organisation", head18), head.height), "The organisation", head18);
            double payroll = 0.0;
            if (s.Staff != null) { foreach (StaffMember m in s.Staff) { payroll += m.SalaryPerDay; } }
            string pay = HqMoney(payroll) + "/day";
            GUIStyle bold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(head.xMax - HqWidth(pay, bold), head.y, HqWidth(pay, bold), head.height), pay, bold);
            y += head.height;

            // the five posts
            string[] codes = { "MGR", "MEDIA", "POLL", "FIELD", "DIGI" };
            int[] states = s.Hq != null ? s.Hq.StaffStates : HqStaffStatesOf(s);
            var row = new Rect(x, y, w, HqH(32f));
            float cw = HqW(62f), ch = V35.Px(24f), gap = HqW(4f);
            for (int i = 0; i < codes.Length; i++)
            {
                int st = i < states.Length ? states[i] : 0;
                var c = new Rect(row.x + i * (cw + gap), row.y + Mathf.Round((row.height - ch) * 0.5f), cw, ch);
                if (st == 3) { HqFill(c, PoliSimTheme.TextPrimary); }
                else if (st == 2) { HqEdge(c, PoliSimTheme.TextPrimary, HqT(1.5f)); }
                else if (st == 1) { HqDashed(c, PoliSimTheme.TextPrimary, HqT(1.5f)); }
                else { HqEdge(c, PoliSimTheme.Hairline, HqT(1f)); }
                HqLabel(c, codes[i], HqMono(14f, st == 3 ? PoliSimTheme.ModalBackground : st == 0 ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter));
                string cellId = "hq:staff:" + i.ToString(CultureInfo.InvariantCulture);
                SlipAnchor(c, cellId);
                string role = PreCampaignRun.RoleName(CampaignStaff.TheFive[i]);
                var cellSlip = new SlipContent(role.ToUpperInvariant());
                if (s.Staff != null)
                {
                    foreach (StaffMember m in s.Staff)
                    {
                        if (!string.Equals(m.Role, role, System.StringComparison.OrdinalIgnoreCase)) { continue; }
                        cellSlip.Add(m.Name.ToLowerInvariant() + " · " + m.BonusLabel);
                    }
                }
                if (cellSlip.Lines.Count == 0) { cellSlip.Add(st == 0 ? "not hired" : st == 1 ? "staged · hired on the campaign's first day" : st == 2 ? "hired" : "on the job"); }
                book.Anchors[cellId] = cellSlip;
            }
            SlipAnchor(new Rect(row.x, row.y, codes.Length * (cw + gap), row.height), "hq:staff");
            book.Anchors["hq:staff"] = new SlipContent("STAFF")
                .Add("MGR manager · MEDIA advisor · POLL pollster")
                .Add("FIELD organiser · DIGI digital strategist")
                .Add("ink on the job · solid hired · dashed staged")
                .Add(s.Staff == null || s.Staff.Length == 0 ? "no staff hired" : "payroll " + pay);
            y += row.height;

            // volunteers, and the offices
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            var vols = new Rect(x, y, w, HqH(26f));
            HqLabel(new Rect(vols.x, vols.y, HqWidth("Volunteers", name16), vols.height), "Volunteers", name16);
            string volText = HqRunUp(s) ? HqN(s.Resources.Volunteers)
                : string.Format(CultureInfo.InvariantCulture, "{0:N0} · {1:N0} h/day", s.Resources.Volunteers, CampaignEconomy.VolunteerHours(s.Resources.Volunteers));
            HqLabel(new Rect(vols.xMax - HqWidth(volText, bold), vols.y, HqWidth(volText, bold), vols.height), volText, bold);
            y += vols.height;
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            var offices = new Rect(x, y, w, HqH(26f));
            float nameW = HqWidth("Offices", name16);
            HqLabel(new Rect(offices.x, offices.y, nameW, offices.height), "Offices", name16);
            float sq = V35.Px(10f);
            var square = new Rect(offices.x + nameW + V35.Px(8f), offices.y + Mathf.Round((offices.height - sq) * 0.5f), sq, sq);
            HqFill(square, PoliSimTheme.Brass);
            HqEdge(square, PoliSimTheme.BrassBorder, HqT(1f));
            int count = s.Offices?.Length ?? 0, volunteers = 0;
            double upkeep = 0.0;
            if (s.Offices != null) { foreach (RegionalOffice o in s.Offices) { volunteers += o.Volunteers; upkeep += o.UpkeepPerDay; } }
            string officeText = string.Format(CultureInfo.InvariantCulture, "{0} · {1:N0} vol · {2}/day", count, volunteers, HqMoney(upkeep));
            HqLabel(new Rect(offices.xMax - HqWidth(officeText, bold), offices.y, HqWidth(officeText, bold), offices.height), officeText, bold);
            SlipAnchor(offices, "hq:offices");
            var slip = new SlipContent("OFFICES · " + count.ToString(CultureInfo.InvariantCulture));
            if (s.Offices != null)
            {
                for (int i = 0; i < s.Offices.Length && i < 3; i++) { slip.Add(CampaignTiles.Display(s.Offices[i].RegionName) + " · " + HqN(s.Offices[i].Volunteers) + " vol"); }
                if (s.Offices.Length > 3) { slip.Add("+" + (s.Offices.Length - 3).ToString(CultureInfo.InvariantCulture) + " more · brass squares on the map"); }
            }
            if (count == 0) { slip.Add("none open"); }
            book.Anchors["hq:offices"] = slip;
        }

        /// <summary>A staged film's five posts, read off its staff rows (every paid role listed is on the job): the live HQ reads <see cref="CampaignHqReading.StaffStates"/>.</summary>
        private static int[] HqStaffStatesOf(CampaignSnapshot s)
        {
            var states = new int[CampaignStaff.TheFive.Length];
            if (s.Staff == null) { return states; }
            foreach (StaffMember m in s.Staff)
            {
                if (!(m.SalaryPerDay > 0.0)) { continue; }   // a vacant post is no one on the job
                for (int i = 0; i < CampaignStaff.TheFive.Length; i++)
                {
                    if (string.Equals(PreCampaignRun.RoleName(CampaignStaff.TheFive[i]), m.Role, System.StringComparison.OrdinalIgnoreCase)) { states[i] = 3; }
                }
            }
            return states;
        }
    }
}
