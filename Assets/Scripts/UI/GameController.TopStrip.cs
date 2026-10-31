using System.Collections.Generic;
using System.Globalization;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// K178 (Elias's ruling: the top strip in every country), drawn to D-WORKLIST §2 (Design's return of 2026-10-28, boards 33a and 33b): THE TOP STRIP - the
    /// frame's banner on every page, in every country, K40's US masthead generalised. Seven places fixed left to right, a chip that does not apply absent
    /// and its neighbours closing up: (1) the clock - Paused with the brass dot, or the speed; (2) what holds it, one chip whose click opens the first
    /// held cell and whose slip lists every hold; (3) what binds and (4) what is coming, at most three together, the rest folded into "+N"
    /// (<see cref="TopStrip.FoldTimeChips"/>); then, from the right end, (7) the player's role with the party's mark, (6) the government's standing in words,
    /// (5) the polling day. What each chip says is <see cref="TopStrip"/>'s; the clock, the holds, the lapse (K40) and the role are the controller's own.
    /// <para>The board's measures at 1280 x 699, scaled with the window (<see cref="V35.Px"/>): 26 px chips, the 14 px face, a 16 px icon, 8 px between
    /// chips, filled grounds BAD #9C4238, AMB #8F6900 and GOOD #2E7048 under paper text, outlined chips 1 px #8A7A5C, paper chips #E9DFCB. The strip
    /// keeps the height K40 built (the banner plate's padding around the hold line), and the chip is centred in it.</para>
    /// </summary>
    public partial class GameController
    {
        private const float StripChipHeight = 26f, StripChipPad = 10f, StripChipGap = 8f, StripIcon = 16f, StripIconGap = 7f, StripDot = 10f;
        private static readonly Color StripPaper = PoliSimTheme.Hex(0xE9DFCB), StripLine = PoliSimTheme.Hex(0x8A7A5C), StripGood = PoliSimTheme.Hex(0x2E7048),
            StripInk = PoliSimTheme.Hex(0xF2EADB), StripBrassDot = PoliSimTheme.Hex(0xC9A23A);

        /// <summary>K178: what the strip took from the page this frame (0 without it) - the Desk's first card row gives it back (<see cref="DrawDeskCards"/>).</summary>
        private float _topStripHeight;

        /// <summary>The strip's 14 px face.</summary>
        private GUIStyle StripFace(Color ink) => V35Serif(V35.Floor, ink);

        private static Color StripGround(TopStrip.Face face) =>
            face == TopStrip.Face.Bad ? PoliSimTheme.Bad : face == TopStrip.Face.Amber ? PoliSimTheme.Caution : face == TopStrip.Face.Good ? StripGood : face == TopStrip.Face.Paper ? StripPaper : Color.clear;

        private static Color StripWords(TopStrip.Face face) =>
            face == TopStrip.Face.Paper ? PoliSimTheme.TextPrimary : face == TopStrip.Face.Line ? StripPaper : StripInk;

        /// <summary>A strip chip's width for its words, its icon and its dot.</summary>
        private float StripChipWidth(string words, bool icon, bool dot) =>
            V35.Px(StripChipPad) * 2f + (icon ? V35.Px(StripIcon) + V35.Px(StripIconGap) : 0f) + (dot ? V35.Px(StripDot) + V35.Px(StripIconGap) : 0f)
            + Mathf.Ceil(StripFace(PoliSimTheme.TextPrimary).CalcSize(new GUIContent(words)).x);

        /// <summary>One strip chip in <paramref name="r"/>: its ground (or its outline), the brass dot, the icon in the words' ink, the words. A state, not a
        /// control, unless <paramref name="control"/> - then an invisible button over the whole rect, drawn on every event, returns the click.</summary>
        private bool DrawStripChip(Rect r, TopStrip.Face face, string words, string icon, bool dot = false, bool control = false)
        {
            Color ink = StripWords(face);
            if (Event.current.type == EventType.Repaint)
            {
                if (face == TopStrip.Face.Line)
                {
                    PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, 1f), StripLine);
                    PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), StripLine);
                    PoliSimTheme.Rule(new Rect(r.x, r.y, 1f, r.height), StripLine);
                    PoliSimTheme.Rule(new Rect(r.xMax - 1f, r.y, 1f, r.height), StripLine);
                }
                else { PoliSimTheme.Rule(r, StripGround(face)); }
            }
            float x = r.x + V35.Px(StripChipPad);
            if (dot)
            {
                float d = V35.Px(StripDot);
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Pill(new Rect(x, r.y + Mathf.Round((r.height - d) * 0.5f), d, d), StripBrassDot); }
                x += d + V35.Px(StripIconGap);
            }
            if (!string.IsNullOrEmpty(icon))
            {
                float side = V35.Px(StripIcon);
                DrawV35Icon(new Rect(x, r.y + Mathf.Round((r.height - side) * 0.5f), side, side), icon, ink);
                x += side + V35.Px(StripIconGap);
            }
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, Mathf.Max(1f, r.xMax - V35.Px(StripChipPad) + 2f - x), r.height), words, StripFace(ink)); }
            return control && PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none);
        }

        private static SlipContent SlipOf(TopStrip.Chip chip)
        {
            var slip = new SlipContent(chip.SlipHead ?? chip.Words.ToUpperInvariant());
            foreach (string line in chip.Slip) { slip.Add(line); }
            return slip;
        }

        /// <summary>K40's lapse chip, in the strip's grammar: lapsed binds (BAD), funded to a horizon and restored are coming (AMB, then GOOD for its day);
        /// its words and its slip are the lapse's own (<see cref="LapseChip"/>, <see cref="LapseSlip"/>).</summary>
        private TopStrip.Chip LapseStripChip(System.DateTime today, out bool binds)
        {
            binds = false;
            (string Words, Color Ground, Color Ink, Color Dot)? lapse = LapseChip(today);
            if (!lapse.HasValue) { return null; }
            binds = lapse.Value.Ground == PoliSimTheme.Bad;
            TopStrip.Face face = binds ? TopStrip.Face.Bad : lapse.Value.Dot == PoliSimTheme.Good ? TopStrip.Face.Good : TopStrip.Face.Amber;
            return new TopStrip.Chip { Id = "lapse", Words = lapse.Value.Words, Face = face, Icon = "gavel" };
        }

        /// <summary>M1, N1: the strip's clock chip reads Paused while the one hold predicate (ClockHeld) holds the clock or the speed is Paused.</summary>
        private static bool ClockHeldBy(bool held, bool speedPaused) => held || speedPaused;

        /// <summary>A Continue's hold in the chip's words: its label before the dash, in sentence case ("Market access lost", "Election night").</summary>
        private static string HoldWords(string label)
        {
            int dash = label.IndexOf(" - ", System.StringComparison.Ordinal);
            string head = dash > 0 ? label.Substring(0, dash) : label;
            return head.Length == 0 ? head : head.Substring(0, 1) + head.Substring(1).ToLowerInvariant();
        }

        /// <summary>N8: every Continue hold named on the chip ("Market access lost · Election night"), not only the first.</summary>
        private static string ContinueWords(List<(string Label, HoldCell Cell)> holds)
        {
            var words = new List<string>();
            foreach ((string label, HoldCell cell) in holds) { if (cell == HoldCell.None) { words.Add(HoldWords(label)); } }
            return string.Join(" · ", words);
        }

        /// <summary>The held cell's icon on the hold chip's slip and click: the Docket's, the Budget's, Parliament's, the campaign's.</summary>
        private static string HoldIcon(HoldCell cell) => cell == HoldCell.Budget ? "coins" : cell == HoldCell.Politics ? "chamber" : cell == HoldCell.Campaign ? "ballot" : "docket";

        /// <summary>
        /// K178: the strip itself - measured as K40's masthead was (the hold line's inner height inside the banner plate's padding), the right end laid out
        /// first (role, standing, polling day) so the time chips fold rather than run under them. Returns the height it takes.
        /// </summary>
        private float DrawTopStrip(List<(string Label, HoldCell Cell)> holds, float width)
        {
            GUIStyle line = DeskCaption(9.5f, PoliSimTheme.TextOnDesk);
            EnsureHoldChipStyle(line);
            float inner = Mathf.Max(Mathf.Max(Mathf.Ceil(_holdChipStyle.CalcSize(new GUIContent("Ag")).y), Mathf.Ceil(line.CalcSize(new GUIContent("Ag")).y)), StatsUnit(20f));
            float stripH = inner + _holdBannerStyle.padding.vertical;
            float chipH = Mathf.Min(V35.Px(StripChipHeight), stripH - 2f);
            float gap = V35.Px(StripChipGap);
            System.DateTime today = _simulationManager.CurrentDate;
            _mastheadSlipBook = new PeopleSlips.Book();

            GUILayout.BeginHorizontal(_holdBannerStyle, GUILayout.Width(width), GUILayout.Height(stripH));
            Rect row = GUILayoutUtility.GetRect(10f, inner, GUILayout.ExpandWidth(true), GUILayout.Height(inner));
            GUILayout.EndHorizontal();
            float chipY = row.y + Mathf.Round((row.height - chipH) * 0.5f);
            Rect At(float x, float w) => new Rect(x, chipY, w, chipH);

            // ---- the right half, from the right end: (7) the role, (6) the standing, (5) the polling day ----
            float right = row.xMax;
            (string Words, bool InOffice)? role = RoleChip();
            if (role.HasValue)
            {
                string tag = RoleTag(role.Value.Words);
                if (tag == "DIVIDED") { tag = null; }   // board 33a: the USA's DIVIDED moves from the role chip to the standing's chip
                float w = RoleChipWidth(role.Value.Words, tag, onStrip: true);
                DrawRoleChip(At(right - w, w), role.Value.Words, role.Value.InOffice, tag, onStrip: true);
                right -= w + gap;
            }
            TopStrip.Chip standing = TopStrip.StandingChip(_playerCountry, today);
            if (standing != null)
            {
                float w = StripChipWidth(standing.Words, false, false);
                Rect r = At(right - w, w);
                DrawStripChip(r, standing.Face, standing.Words, null);
                MastheadAnchor(r, standing.Id);
                _mastheadSlipBook.Anchors[standing.Id] = SlipOf(standing);
                right -= w + gap;
            }
            bool hasPolling = _simulationManager.TryPlayerPollingDay(out System.DateTime pollingDay);
            // §946 (the PL-C film's Low): a campaign under way votes on its own day - Poland's run-off (or a presidential first round) comes before the next
            // Sejm date the calendar answers, and the chip read the Sejm's
            System.DateTime? campaignDay = _simulationManager.PlayerCampaign?.Setup.Calendar.ElectionDate;
            if (campaignDay.HasValue && campaignDay.Value.Date >= today.Date && (!hasPolling || campaignDay.Value.Date < pollingDay.Date)) { pollingDay = campaignDay.Value; hasPolling = true; }
            TopStrip.Chip polling = hasPolling ? TopStrip.PollingChip(pollingDay, today) : null;
            if (polling != null)
            {
                float w = StripChipWidth(polling.Words, true, false);
                Rect r = At(right - w, w);
                DrawStripChip(r, polling.Face, polling.Words, polling.Icon);
                MastheadAnchor(r, polling.Id);
                _mastheadSlipBook.Anchors[polling.Id] = SlipOf(polling);
                right -= w + gap;
            }

            // ---- the left half: (1) the clock, (2) what holds it, (3) what binds, (4) what is coming ----
            float x = row.x;
            // the review of wave/dcrisis, M1: the clock is read off EVERY hold - the predicate Update and isTimePaused use - not the banner's list, which leaves
            // the budget window out on the Budget page, where the window still holds the clock
            List<(string Label, HoldCell Cell)> allHolds = CollectHolds(includeBudgetProcess: true);
            bool paused = ClockHeldBy(ClockHeld(), _gameSpeed == GameSpeed.Paused);   // N1: the one hold predicate
            string clockWords = paused ? "Paused" : _gameSpeed == GameSpeed.VeryFast ? "3×" : _gameSpeed == GameSpeed.Fast ? "2×" : "1×";
            {
                float w = StripChipWidth(clockWords, false, paused);
                Rect r = At(x, w);
                DrawStripChip(r, TopStrip.Face.Paper, clockWords, null, dot: paused);
                MastheadAnchor(r, "strip:clock");
                SlipContent clockSlip = paused ? new SlipContent("PAUSED").Add("The clock waits") : new SlipContent("THE CLOCK · " + clockWords).Add("The days run at this speed");
                foreach ((string label, HoldCell cell) in allHolds) { clockSlip.Add(HoldCellCaption(cell) == null ? label : HoldCellCaption(cell) + " · " + label); }
                _mastheadSlipBook.Anchors["strip:clock"] = clockSlip;
                x += w + gap;
            }
            if (holds.Count > 0)
            {
                // the review, L9: a decision is a hold answered at a cell; the crisis plate and election night wait on a Continue and are named by their own words
                int decisions = 0;
                foreach ((string _, HoldCell cell) in holds) { if (cell != HoldCell.None) { decisions++; } }
                string words = decisions == 1 ? "1 decision waiting" : decisions > 1 ? decisions.ToString(CultureInfo.InvariantCulture) + " decisions waiting" : ContinueWords(holds);
                HoldCell first = HoldCell.None;
                var slip = new SlipContent("WAITING ON YOU");
                foreach ((string label, HoldCell cell) in holds)
                {
                    if (first == HoldCell.None && cell != HoldCell.None) { first = cell; }
                    slip.Add(HoldCellCaption(cell) == null ? label : HoldCellCaption(cell) + " · " + label);
                }
                float w = StripChipWidth(words, true, false);
                Rect r = At(x, w);
                bool clicked = DrawStripChip(r, TopStrip.Face.Paper, words, HoldIcon(first), control: first != HoldCell.None);
                MastheadAnchor(r, "strip:holds");
                _mastheadSlipBook.Anchors["strip:holds"] = slip;
                x += w + gap;
                if (clicked) { GoToHoldCell(first); }
            }

            var binds = new List<TopStrip.Chip>();
            var coming = new List<TopStrip.Chip>();
            TopStrip.Chip lapse = LapseStripChip(today, out bool lapseBinds);
            TopStrip.Chip crisis = TopStrip.CrisisChip(_playerCountry, today, out bool crisisBinds);
            if (lapse != null && lapseBinds) { binds.Add(lapse); }
            if (crisis != null && crisisBinds) { binds.Add(crisis); }
            if (crisis != null && !crisisBinds) { coming.Add(crisis); }
            TopStrip.Chip confidence = TopStrip.ConfidenceChip(_playerCountry, today);
            if (confidence != null) { coming.Add(confidence); }
            TopStrip.Chip art158 = TopStrip.Art158Chip(_playerCountry, today);   // K197: the Sejm's constructive vote, for its one day
            if (art158 != null) { coming.Add(art158); }
            TopStrip.Chip sejm = TopStrip.SejmStepChip(_playerCountry, today);   // S11 (11d): the Sejm's next step while its formation runs
            if (sejm != null) { coming.Add(sejm); }
            TopStrip.CoalitionChips(_playerCountry, today, binds, coming);   // COAL-2 with §9: a leaving or a dismissal for its day (place 3), a threat or an enforcement while it stands (place 4)
            if (lapse != null && !lapseBinds) { coming.Add(lapse); }
            TopStrip.Chip ballot = BallotStripChip(today, out bool ballotBinds);   // SP-5 (S5, board 5c): a created party's ballot access - refused binds, collecting is coming
            if (ballot != null) { (ballotBinds ? binds : coming).Add(ballot); }
            List<TopStrip.Chip> shown = TopStrip.FoldTimeChips(binds, coming, out TopStrip.Chip more);
            // what the right half leaves: a chip that would run under it folds into the "+N" chip too
            while (shown.Count > 0)
            {
                float need = 0f;
                foreach (TopStrip.Chip c in shown) { need += StripChipWidth(c.Words, c.Icon != null, false) + gap; }
                if (more != null) { need += StripChipWidth(more.Words, false, false) + gap; }
                if (x + need <= right) { break; }
                TopStrip.Chip dropped = shown[shown.Count - 1];
                shown.RemoveAt(shown.Count - 1);
                if (more == null) { more = new TopStrip.Chip { Id = "strip:more", Face = TopStrip.Face.Paper, SlipHead = "ALSO" }; }
                more.Slip.Insert(0, dropped.Words);
                more.Words = "+" + more.Slip.Count.ToString(CultureInfo.InvariantCulture);
            }
            foreach (TopStrip.Chip c in shown)
            {
                float w = StripChipWidth(c.Words, c.Icon != null, false);
                Rect r = At(x, w);
                DrawStripChip(r, c.Face, c.Words, c.Icon);
                MastheadAnchor(r, c.Id);
                _mastheadSlipBook.Anchors[c.Id] = c.Id == "lapse" ? LapseSlip(today) : SlipOf(c);
                x += w + gap;
            }
            if (more != null)
            {
                float w = StripChipWidth(more.Words, false, false);
                Rect r = At(x, w);
                DrawStripChip(r, more.Face, more.Words, null);
                MastheadAnchor(r, more.Id);
                _mastheadSlipBook.Anchors[more.Id] = SlipOf(more);
            }
            return stripH + _holdBannerStyle.margin.vertical;
        }
    }
}
