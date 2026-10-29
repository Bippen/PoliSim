using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// D-IT (Design's boards 34a-34d, `PoliSim-captures/design/RETURN_2026-10-29_DIT/`): <b>ITALY'S ELECTION NIGHT</b> - the Camera and the Senato counted
    /// college by college (`ItalianNight`, on the game's own count), in the nights' frame at Design's sizes (a board pixel is 1.5 canvas units, so 14 board px
    /// is 14 px at 1280 and 28 at 2560: nothing reflows, 34c). Three columns at the board's widths - THE COUNT (318), THE MAP (447), WHO GOVERNS (330, D-NIGHT's
    /// rect) - under a masthead at the 14 px floor, with the † cell that swaps all three for their full wording, each scrolled inside its own rect.
    /// <list type="bullet">
    /// <item><description><b>The count is the legend</b> (34a): each coalition a row with its lists indented under a bracket, every row its own tick on one lane
    /// zoomed to 0-15 % (10 a coalition, 3 a list, a short tick at 1 for a coalition's list - below it the list's votes are struck from the coalition, lett. c,
    /// and its share prints struck); the bar the counted votes over every valid vote, so it only grows; the minority list's lane its route (TAA ≥ 20, ✓ once
    /// met); MAIE and UV boxed where they stand; the Senato's senators for life a row by record. The share is owed until the count closes; the seats column
    /// is headed "won" (colleges won) while counting and "seats" at the close.</description></item>
    /// <item><description><b>The map: one cell, one seat</b> (`ItalianCellView`): each block's colleges first in U order with a paper dot, then its list
    /// seats in the night's fixed order - never re-sorted; a college inks in its seat's list ink when it declares; list seats and the abroad seats ink at the
    /// close (art. 83's quotient needs the whole count); a seat still to come is paper with a tan rule; the senators for life inked from the start in the
    /// rule's tan (#8A7A5C), seated by record. A Camera/Senato pick swaps the chamber.</description></item>
    /// <item><description><b>Who governs</b>: the President's three mandates and the first sitting; art. 94's two lanes, each ticked at half the chamber plus
    /// one (read off the chamber's size - art. 64 with every member voting, `ConfidenceProcedure.Article94`); PROJECTED, and nothing installed on the night.
    /// While counting the lanes carry the leading coalition's certain seats; once both chambers are called, the projection; three failed mandates draw the
    /// dissolution from the formation's own code (`ItalianFormation.Run`, art. 61 and 88 c. 2 by `WorldClock.ItalyDissolutionBarred` and `ConfidenceProcedure.ItalianElectionDay`).</description></item>
    /// </list>
    /// ⚠ Declared deviations: the night's faces have no bold Courier (§648), so the board's bold figures are the Document face at its weight; numbers read with a
    /// point (§568); a figure that needs a vote count prints a share only - the colleges' votes are the swing's, on 2018's college sizes, so their absolute
    /// numbers are not votes cast in the election the night counts; the slips hang on the rows, the block labels and the boxes, not on every one of the cells.
    /// </summary>
    public partial class ElectionNightScreen
    {
        /// <summary>D-IT: the Italian night's map views (null on every other night) - read by the film's log line (the cells by state, the smallest label).</summary>
        public ItalianCellView ItalyCameraMap { get; private set; }
        public ItalianCellView ItalySenatoMap { get; private set; }

        /// <summary>D-IT: the chamber the sheet shows.</summary>
        public ItalianCount.Chamber ItalyChamber { get; private set; }

        /// <summary>D-IT: the count's and the map's views per chamber, at rest and in full (the † view) - toggled with the chamber and the † cell.</summary>
        private List<(GameObject AtRest, GameObject Dense, int Chamber)> _italyViews;

        /// <summary>D-IT: every Text the Italian sheet built in its own columns, masthead and foot - read by the film's log line (the smallest size); the check holds the sizes on the source (`ItalianNightDiagnostic` (g)).</summary>
        public readonly List<Text> ItalyTexts = new List<Text>();

        /// <summary>What the Italian night draws beside its count - all of it read by the caller from the game, none of it from the board.</summary>
        public sealed class ItalyNight
        {
            public ItalianNight.State State;
            /// <summary>The chamber the sheet opens on.</summary>
            public ItalianCount.Chamber Chamber = ItalianCount.Chamber.Camera;
            public DateTime PollingDay, FirstSitting;
            /// <summary>The procedure's projection (`SimulationManager.ProjectFormation` on the chambers the count seated) - drawn once both chambers are called.</summary>
            public FormationProjection Projection;
            /// <summary>The mandates as the formation's code tries them on those chambers (`ItalianFormation.Run`, the list the projection is drawn from) - each with
            /// its chambers' votes, or none where it reached no vote.</summary>
            public List<ItalianFormation.Mandate> Mandates;
            public string PlayerParty;
            /// <summary>The player's party sits in the government in office (the caretaker, should the mandates fail).</summary>
            public bool PlayerInGovernment;
            /// <summary>The dissolution after three failed mandates, as the formation's code dates it: the semestre bianco's bar, the day dissolved, art. 61's polling day.</summary>
            public bool DissolutionBarred;
            public DateTime DissolvedOn, PollingAfter;
            /// <summary>Italy's Why and budget rows: the campaign ledger's one line (null: none kept) and the standing budget's (null: none enacted).</summary>
            public string LedgerLine, BudgetLine;
            /// <summary>A film's staged fixture says so on the masthead ("STAGED"); null on the live night.</summary>
            public string Staged;
        }

        /// <summary>[AUTHORED] layout: the board's three columns and their gutter, in board pixels (34a: 318 at x 73, 447 at x 411, 330 at x 878).</summary>
        private const float ItalyCountPx = 318f, ItalyMapPx = 447f, ItalyGovernsPx = 330f, ItalyGutterPx = 20f;

        /// <summary>[AUTHORED] layout: the count's lane top - 34a zooms it to 0-15 %.</summary>
        private const double ItalyLaneTop = 0.15;

        /// <summary>[AUTHORED] layout - 34a's script, in board px: a college's dot (3 on the Camera's 12 px pitch, 5 on the Senato's 17), the boxes in the sea at
        /// x 4 (the abroad box at y 404 on the Camera and 344 on the Senato, the senators for life at y 404), their caption 22 above the cells, 5 between groups.</summary>
        private const float CameraDotPx = 3f, SenatoDotPx = 5f, BoxXPx = 4f, CameraAbroadYPx = 404f, SenatoAbroadYPx = 344f, LifeYPx = 404f, BoxCaptionRisePx = 22f, BoxGroupGapPx = 5f;

        /// <summary>[AUTHORED] 34a's tan for the senators for life, inked from the start (the desk's heavy rule).</summary>
        private static Color LifeInk => PoliSimTheme.HairlineStrong;

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE SHEET
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>D-IT: Italy's night at one instant. Null, and said, where the furniture or the count is missing.</summary>
        public static ElectionNightScreen BuildItaly(ItalyNight night, string verdict)
        {
            if (night == null || night.State == null)
            {
                Debug.LogWarning("CANVAS: the Italian night has no count - the board is dropped, the night stays silent.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }
            Transform overlay = screen.Root.transform;
            screen._italyViews = new List<(GameObject AtRest, GameObject Dense, int Chamber)>();
            screen.ItalyChamber = night.Chamber;

            ItalyMasthead(screen, content, night);
            var body = new GameObject("Body");
            body.transform.SetParent(content, false);
            body.AddComponent<RectTransform>();
            var grid = body.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = Px(ItalyGutterPx);
            grid.childControlWidth = true;
            grid.childControlHeight = true;
            grid.childForceExpandWidth = false;
            grid.childForceExpandHeight = true;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Transform count = ItalyColumn(body.transform, "Count", ItalyCountPx);
            Transform map = ItalyColumn(body.transform, "MapColumn", ItalyMapPx);
            Transform governs = ItalyColumn(body.transform, "Governs", ItalyGovernsPx);
            foreach (ItalianCount.Chamber c in new[] { ItalianCount.Chamber.Camera, ItalianCount.Chamber.Senato })
            {
                var countDense = new List<DenseLine>();
                GameObject countRest = ItalyCount(screen, count, night, c, overlay, countDense);
                screen._italyViews.Add((countRest, DenseView(count, countDense), (int)c));
                var mapDense = new List<DenseLine>();
                GameObject mapRest = ItalyMap(screen, map, night, c, overlay, mapDense);
                screen._italyViews.Add((mapRest, DenseView(map, mapDense), (int)c));
            }
            var governsDense = new List<DenseLine>();
            screen._governsAtRest = ItalyGoverns(screen, governs, night, overlay, governsDense);
            screen._governsDense = DenseView(governs, governsDense);
            screen._governsDense.SetActive(false);   // the film's defect: DenseView builds its view active, and BuildGoverns hides it - this column must too
            ItalyFooter(screen, content, verdict, overlay);
            screen.ApplyItalyViews();

            Resolve(contentRect);
            // only the shown map is laid now: a hidden view's rect is its default 100 units, and a rebuild there drew its labels at 3 units (the film's log); it
            // lays itself when it is shown (`ItalianCellView.LateUpdate`)
            if (screen.ItalyCameraMap != null && screen.ItalyCameraMap.gameObject.activeInHierarchy) { screen.ItalyCameraMap.Rebuild(); }
            if (screen.ItalySenatoMap != null && screen.ItalySenatoMap.gameObject.activeInHierarchy) { screen.ItalySenatoMap.Rebuild(); }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        /// <summary>D-IT: the chamber the sheet shows - the count, the map and the masthead's declared line swap; Who governs reads both and stays.</summary>
        public void SetItalyChamber(ItalianCount.Chamber chamber)
        {
            if (_italyViews == null) { return; }
            ItalyChamber = chamber;
            ApplyItalyViews();
        }

        /// <summary>D-IT: each view by the chamber shown and the † cell (called by `SetDense` too); a † view opened is laid twice (§578).</summary>
        private void ApplyItalyViews()
        {
            if (_italyViews == null) { return; }
            foreach ((GameObject rest, GameObject dense, int chamber) in _italyViews)
            {
                bool mine = chamber < 0 || chamber == (int)ItalyChamber;
                if (rest != null) { rest.SetActive(mine && (dense == null || !DenseShown)); }   // a view with no † twin (the masthead's declared line) stays in the † view
                if (dense != null)
                {
                    dense.SetActive(mine && DenseShown);
                    if (mine && DenseShown && dense.GetComponent<ScrollRect>() != null)
                    {
                        var c = (RectTransform)dense.GetComponent<ScrollRect>().content;
                        LayoutRebuilder.ForceRebuildLayoutImmediate(c);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(c);
                    }
                }
            }
            if (Root != null)
            {
                Transform content = Root.transform.Find("Document/Content");
                if (content != null) { LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content); }
            }
        }

        /// <summary>A column at a fixed board width (it may give up two board pixels where the content's rect is a hair narrower than the board's sheet).</summary>
        private static Transform ItalyColumn(Transform parent, string name, float boardWidth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = Px(boardWidth);
            element.minWidth = Px(boardWidth - 2f);
            element.flexibleWidth = 0f;
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 0f;
            return go.transform;
        }

        /// <summary>A text the Italian sheet owns - recorded for the check's size floor.</summary>
        private static Text ItText(ElectionNightScreen screen, Transform parent, string name, string text, int size, Color ink, Font font)
        {
            Text t = PlText(parent, name, text, size, ink, font);
            screen.ItalyTexts.Add(t);
            return t;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE MASTHEAD AND THE FOOT
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>34a's masthead at the 14 px floor: the flag, "Election night", the chambers and the day; the clock, the declared bar and its line (per
        /// chamber), the † cell; the rule under it.</summary>
        private static void ItalyMasthead(ElectionNightScreen screen, Transform content, ItalyNight night)
        {
            ItalianNight.State s = night.State;
            Transform mast = CanvasRows.HRow(content, "Masthead", Px(32f), Px(12f));
            Texture2D flag = IconLibrary.GetFlag(CountryId.Italy);
            if (flag != null)
            {
                var f = new GameObject("Flag");
                f.transform.SetParent(mast, false);
                RawImage image = f.AddComponent<RawImage>();
                image.texture = flag;
                image.raycastTarget = false;
                LayoutElement le = f.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = Px(24f);
                le.minHeight = le.preferredHeight = Px(16f);
                le.flexibleWidth = 0f;
                CanvasRows.Edges(f.transform, PoliSimTheme.HairlineStrong, 1.5f);
            }
            Hold(ItText(screen, mast, "Title", "Election night", Px(24f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            string day = night.PollingDay.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            Hold(ItText(screen, mast, "Chambers", "Camera · Senato · " + day + (string.IsNullOrEmpty(night.Staged) ? string.Empty : " · " + night.Staged), Px(16f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
            CanvasRows.Spacer(mast);
            DateTime clock = night.PollingDay.Date.AddHours(ItalianNight.PollsCloseHour).AddMinutes(ItalianNight.MinuteAt(s.Index));
            Text time = ItText(screen, mast, "Clock", clock.ToString("HH:mm", CultureInfo.InvariantCulture), Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Document);
            Hold(time);
            CanvasRows.Slip(time.gameObject, screen.Root.transform, "The count", SlipWrap(
                string.Format(CultureInfo.InvariantCulture, "Camera: {0} of {1} colleges declared", s.Camera.Declared, s.Camera.Colleges),
                string.Format(CultureInfo.InvariantCulture, "Senato: {0} of {1}", s.Senato.Declared, s.Senato.Colleges),
                "The polls closed at 23:00; the order the colleges declare in is seeded - a presentation, never a forecast"));
            foreach (ItalianNight.ChamberNight c in new[] { s.Camera, s.Senato })
            {
                Transform declared = CanvasRows.HRow(mast, "Declared " + c.Of, Px(32f), Px(12f));
                var bar = new GameObject("Bar");
                bar.transform.SetParent(declared, false);
                bar.AddComponent<RectTransform>();
                Image track = bar.AddComponent<Image>();
                track.color = PoliSimTheme.CardInset;
                track.raycastTarget = false;
                LayoutElement bl = bar.AddComponent<LayoutElement>();
                bl.minWidth = bl.preferredWidth = Px(200f);
                bl.minHeight = bl.preferredHeight = Px(12f);
                bl.flexibleWidth = 0f;
                CanvasRows.Edges(bar.transform, PoliSimTheme.Hairline, 1.5f);
                if (c.Declared > 0) { Absolute(bar.transform, "Fill", 0.0, c.Declared / (double)c.Colleges, 0f, Px(12f), PoliSimTheme.TextSecondary); }
                string line = c.Declared.ToString(CultureInfo.InvariantCulture) + " of " + c.Colleges.ToString(CultureInfo.InvariantCulture) + " colleges";
                Text t = ItText(screen, declared, "Line", line, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document);
                LayoutElement tl = t.gameObject.AddComponent<LayoutElement>();
                tl.minWidth = tl.preferredWidth = Mathf.Max(Measure("147 of 147 colleges", Px(14f), PoliSimTheme.Document), Measure(line, Px(14f), PoliSimTheme.Document)) + 2f;
                tl.flexibleWidth = 0f;
                screen._italyViews.Add((declared.gameObject, null, (int)c.Of));
            }
            DaggerCell(mast, screen);
            Rule(content, PoliSimTheme.HairlineStrong);
        }

        /// <summary>The foot at the board's sizes: the verdict at the 14 px floor beside Continue (127 × 24 board px).</summary>
        private static void ItalyFooter(ElectionNightScreen screen, Transform content, string verdict, Transform overlay)
        {
            Transform foot = CanvasRows.HRow(content, "Footer", Px(30f), Px(16f));
            // the board's one line (the film's clip: wrapped, the sentence stood 67 units in a 45-unit foot): the verdict cut at a word to the room beside
            // Continue, measured, with the whole sentence in its slip
            float room = Px(ItalyCountPx + ItalyMapPx + ItalyGovernsPx + 2f * ItalyGutterPx - 2f) - Px(127f) - Px(16f);
            string shown = FitOneLine(verdict ?? string.Empty, Px(14f), room);
            Text line = CanvasChrome.MakeTextRealWeight(foot, "Verdict", shown, PoliSimTheme.Document, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            line.horizontalOverflow = HorizontalWrapMode.Overflow;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            line.raycastTarget = false;
            line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            screen.ItalyTexts.Add(line);
            if (!string.IsNullOrEmpty(verdict)) { CanvasRows.Slip(line.gameObject, overlay, "The verdict", SlipWrap(verdict)); }
            Button control = CanvasChrome.FacedButton(foot, "ContinueButton", "Continue", PoliSimTheme.Display, Px(16f), PoliSimTheme.TextPrimary, new Vector2(Px(127f), Px(24f)), style: FontStyle.Normal);
            LayoutElement size = control.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = Px(127f);
            size.minHeight = size.preferredHeight = Px(24f);
            size.flexibleWidth = 0f;
            control.onClick.AddListener(screen.Dismiss);
            CanvasRows.Slip(control.gameObject, overlay, "Continue", SlipWrap("The clock runs again; the mandates follow on the first sitting"));
        }

        /// <summary>The longest run of whole words of <paramref name="text"/> that fits <paramref name="room"/> canvas units at <paramref name="size"/>, with an
        /// ellipsis where it was cut - measured, never counted in characters.</summary>
        private static string FitOneLine(string text, int size, float room)
        {
            if (string.IsNullOrEmpty(text) || Measure(text, size, PoliSimTheme.Document) <= room) { return text; }
            string[] words = text.Split(' ');
            string best = string.Empty;
            for (int i = 1; i <= words.Length; i++)
            {
                string candidate = string.Join(" ", words, 0, i) + " …";
                if (Measure(candidate, size, PoliSimTheme.Document) > room) { break; }
                best = candidate;
            }
            return best.Length > 0 ? best : "…";
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE COUNT
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>One row of the count: a coalition, a list (indented under its coalition or alone), the other lists, or the senators for life.</summary>
        private enum ItRowKind { Coalition, Member, Alone, Other, Life }

        /// <summary>34a's left column for one chamber, at rest; its full lines into <paramref name="dense"/>.</summary>
        private static GameObject ItalyCount(ElectionNightScreen screen, Transform column, ItalyNight night, ItalianCount.Chamber chamber, Transform overlay, List<DenseLine> dense)
        {
            ItalianNight.State s = night.State;
            ItalianNight.ChamberNight c = s.Of(chamber);
            bool final = s.Final, before = s.Index == 0;
            var rest = new GameObject("Count " + chamber);
            rest.transform.SetParent(column, false);
            rest.AddComponent<RectTransform>();
            var v = rest.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false; v.spacing = 0f;
            Transform p = rest.transform;

            Transform head = CanvasRows.HRow(p, "CountHead", Px(28f), Px(8f));
            Hold(ItText(screen, head, "Heading", "The count", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            Hold(ItText(screen, head, "Chamber", chamber.ToString(), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            dense.Add(new DenseLine("THE COUNT, IN FULL - " + chamber.ToString().ToUpperInvariant(), true));

            // the widths: the board's, widened to what the words measure
            float bracketW = Px(14f), markW = Px(16f), gapW = Px(2f);
            float codeW = Px(54f), shareW = Px(52f), seatsW = Px(50f);
            foreach (string key in c.OrderedKeys()) { codeW = Mathf.Max(codeW, Measure(Short(CountryId.Italy, key), Px(16f), PoliSimTheme.Document) + 2f + Px(6f)); }
            shareW = Mathf.Max(shareW, Measure("00.0", Px(16f), PoliSimTheme.Document) + 2f, Measure("owed", Px(14f), PoliSimTheme.Document) + 2f);
            seatsW = Mathf.Max(seatsW, Measure("000", Px(16f), PoliSimTheme.Document) + 2f, Measure("seats", Px(14f), PoliSimTheme.Document) + 2f, Measure("owed", Px(14f), PoliSimTheme.Document) + 2f);
            float laneW = Mathf.Max(Px(60f), Px(ItalyCountPx) - bracketW - markW - codeW - gapW - shareW - seatsW);

            Transform keys = CanvasRows.HRow(p, "CountKeys", Px(20f), 0f);
            Blank(keys, bracketW + markW + codeW + gapW);
            Transform laneHead = ItLane(keys, laneW, Px(20f));
            ItLaneMark(screen, laneHead, "1", 0.01 / ItalyLaneTop);
            ItLaneMark(screen, laneHead, "3", 0.03 / ItalyLaneTop);
            ItLaneMark(screen, laneHead, "10" + Nbsp + "%", 0.10 / ItalyLaneTop);
            CanvasRows.FixedCell(keys, "share", shareW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, final ? "seats" : "won", seatsW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            foreach (Text t in keys.GetComponentsInChildren<Text>()) { if (!screen.ItalyTexts.Contains(t)) { screen.ItalyTexts.Add(t); } }

            double Share(double votes) => c.AllValid > 0.0 ? votes / c.AllValid : 0.0;
            string Pct1(double share) => (100.0 * share).ToString("0.0", CultureInfo.InvariantCulture);
            bool routeMet = c.MinorityRouteMet(out int _);

            void Row(ItRowKind kind, string id, string[] members)
            {
                bool coalition = kind == ItRowKind.Coalition, member = kind == ItRowKind.Member;
                string key = members != null && members.Length == 1 ? members[0] : null;
                Transform row = CanvasRows.HRow(p, "Count " + id, Px(coalition ? 28f : member ? 24f : 26f), 0f);
                if (!member) { Edge(row, coalition ? PoliSimTheme.RuleRow : PoliSimTheme.CardInset, top: true); }
                // the bracket: a coalition's lists hang from a rule in its gutter
                var bracket = new GameObject("Bracket");
                bracket.transform.SetParent(row, false);
                bracket.AddComponent<RectTransform>();
                LayoutElement bl = bracket.AddComponent<LayoutElement>();
                bl.minWidth = bl.preferredWidth = bracketW; bl.flexibleWidth = 0f;
                if (member) { AbsolutePx(bracket.transform, "Rule", 0.0, Px(4f), BoardPx, 0f, Px(24f), PoliSimTheme.HairlineStrong); }
                if (key != null && (kind == ItRowKind.Member || kind == ItRowKind.Alone) && MarkOf(CountryId.Italy, key) != null) { CanvasRows.Mark(row, CountryId.Italy, key, markW); } else { Blank(row, markW); }
                string code = kind == ItRowKind.Other ? "Other" : kind == ItRowKind.Life ? "Life" : coalition ? id : Short(CountryId.Italy, key);
                int codeSize = coalition ? Px(16f) : Px(14f);
                Color codeInk = kind == ItRowKind.Other ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary;
                Text codeText = CanvasRows.FixedCell(row, code, codeW, codeSize, codeInk);
                codeText.alignment = TextAnchor.MiddleLeft;
                screen.ItalyTexts.Add(codeText);
                Blank(row, gapW);

                double counted = 0.0, national = 0.0;
                int won = 0, seats = 0;
                if (members != null) { foreach (string m in members) { int k = c.K(m); if (k >= 0) { counted += c.Counted[k]; national += c.National[k]; won += c.CollegesWon[k]; } seats += c.SeatsOf(m); } }
                if (kind == ItRowKind.Other) { counted = c.CountedOther; national = c.NationalOther; }
                if (kind == ItRowKind.Life) { seats = c.LifeSeats; }

                Transform lane = ItLane(row, laneW, Px(22f));
                string boxed = null, boxNote = null;
                bool minority = key == ItalianCount.MinorityKey;
                if (minority) { boxed = "TAA"; boxNote = chamber == ItalianCount.Chamber.Camera ? "≥ 20" + (routeMet ? " ✓" : string.Empty) : "colleges"; }
                else if (key == "MAIE") { boxed = "abroad"; }
                else if (key == "UV") { boxed = "VdA"; }
                else if (kind == ItRowKind.Life) { boxed = "by record"; }
                if (boxed != null)
                {
                    Transform box = CanvasRows.HRow(lane, "Box", Px(22f), Px(6f));
                    Stretch((RectTransform)box);
                    GameObject stamp = CanvasRows.Stamp(box, boxed, Px(14f), PoliSimTheme.TextPrimary);
                    HoldWidth(stamp.GetComponentInChildren<Text>());
                    screen.ItalyTexts.Add(stamp.GetComponentInChildren<Text>());
                    if (boxNote != null) { Text note = PlText(box, "Note", boxNote, Px(14f), routeMet && minority && chamber == ItalianCount.Chamber.Camera ? PoliSimTheme.Good : PoliSimTheme.TextMuted, PoliSimTheme.Document); HoldWidth(note); screen.ItalyTexts.Add(note); }
                }
                else
                {
                    Color ink = coalition ? PoliSimTheme.TextSecondary : kind == ItRowKind.Other ? PoliSimTheme.Hairline : ItalyInk(key);
                    double tick = coalition ? Rosatellum.CoalitionThreshold : Rosatellum.ListThreshold;
                    ItLaneBar(lane, Math.Min(1.0, Share(counted) / ItalyLaneTop), tick / ItalyLaneTop, member ? Rosatellum.MinorityCoalitionFloor / ItalyLaneTop : -1.0, ink);
                }

                // the share: owed until the close; a list's share in the lane's rows only (a box has none, but the minority's on the Camera)
                bool shareRow = boxed == null || (minority && chamber == ItalianCount.Chamber.Camera);
                bool struck = member && final && Share(national) < Rosatellum.MinorityCoalitionFloor;
                string shareText = !shareRow ? string.Empty : final ? Pct1(Share(national)) : "owed";
                Text shareCell = CanvasRows.FixedCell(row, shareText, shareW, final && shareRow ? (member ? Px(14f) : Px(16f)) : Px(14f), final ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                screen.ItalyTexts.Add(shareCell);
                if (struck) { Strike(shareCell.transform, PoliSimTheme.TextPrimary); }

                // the seats: colleges won while counting, the seats at the close; owed before the first college and, for the abroad list, until the close
                string seatsText;
                if (kind == ItRowKind.Life) { seatsText = seats.ToString(CultureInfo.InvariantCulture); }
                else if (kind == ItRowKind.Other) { seatsText = final ? "0" : string.Empty; }
                else if (before || (key == "MAIE" && !final)) { seatsText = "owed"; }
                else { seatsText = (final ? seats : won).ToString(CultureInfo.InvariantCulture); }
                bool owedSeats = seatsText == "owed";
                Text seatsCell = CanvasRows.FixedCell(row, seatsText, seatsW, owedSeats ? Px(14f) : member ? Px(14f) : Px(16f), owedSeats ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                screen.ItalyTexts.Add(seatsCell);

                // the slip and the † view
                var lines = new List<string>();
                string name, status;
                if (coalition)
                {
                    name = CoalitionName(members) + " coalition";
                    var shorts = new List<string>(); foreach (string m in members) { shorts.Add(Short(CountryId.Italy, m)); }
                    lines.Add(string.Join(" · ", shorts));
                    lines.Add("Admitted at 10" + Nbsp + "% with a list at 3" + Nbsp + "% (art. 83 lett. e)");
                    if (final)
                    {
                        double strip = 0.0;
                        foreach (string m in members) { int k = c.K(m); if (k >= 0 && Share(c.National[k]) >= Rosatellum.MinorityCoalitionFloor) { strip += c.National[k]; } }
                        lines.Add(Pct1(Share(national)) + Nbsp + "% of the valid vote; " + Pct1(Share(strip)) + Nbsp + "% after the 1" + Nbsp + "% strip (lett. c)");
                    }
                    status = string.Empty;
                }
                else if (kind == ItRowKind.Other)
                {
                    name = "Every other list";
                    lines.Add("The lists the game's roster does not carry: the rest of every base the statutes read");
                    status = "The count seats no list outside the roster: they take no college and no list seat";
                    lines.Add(status);
                    lines.Add((final ? "Their share " + Pct1(Share(national)) : "Counted so far " + Pct1(Share(counted))) + Nbsp + "% of every valid vote");
                }
                else if (kind == ItRowKind.Life)
                {
                    name = "Senators for life · " + c.LifeSeats.ToString(CultureInfo.InvariantCulture);
                    foreach ((string who, string group, string lifeKey) in c.Life) { lines.Add(who + " · " + group + " (seated as " + Short(CountryId.Italy, lifeKey) + ")"); }
                    lines.Add("Seated by record (K154), not by the count; on confidence they vote with their group - the model counts them against");
                    status = string.Empty;
                }
                else
                {
                    string[] co = c.CoalitionOf(key);
                    name = ItalyPartyName(key) + " · " + Short(CountryId.Italy, key);
                    double sh = Share(national);
                    if (minority) { status = chamber == ItalianCount.Chamber.Camera ? "The minority route (lett. e n. 2): a fifth of its circoscrizione, or a quarter of its colleges" + (routeMet ? " - met" : " - not yet met") : national > 0.0 ? "The minority list (art. 16-bis): a fifth of its region, or a quarter of its colleges" + (routeMet ? " - met" : " - not yet met") : "No list vote for it in this count - its seats are the colleges its group wins"; }
                    else if (key == "MAIE") { status = "Stands abroad only (L. 459/2001)"; }
                    else if (key == "UV") { status = "Stands in Valle d'Aosta's one college only"; }
                    else if (co != null) { status = !final ? "In the " + CoalitionName(co) + " coalition" : sh >= Rosatellum.ListThreshold ? "At 3" + Nbsp + "%: takes list seats inside its coalition" : sh >= Rosatellum.MinorityCoalitionFloor ? "Between 1 and 3" + Nbsp + "%: counts for its coalition, takes no list seat" : "Under 1" + Nbsp + "%: its votes are struck from the coalition (lett. c)"; }
                    else { status = !final ? "A list alone" : sh >= Rosatellum.ListThreshold ? "At 3" + Nbsp + "%: admitted on its own (lett. e)" : "Under 3" + Nbsp + "%: no list seat"; }
                    if (co != null) { lines.Add(CoalitionName(co) + " coalition"); }
                    lines.Add(status);
                }
                if (kind != ItRowKind.Life && kind != ItRowKind.Other && shareRow)
                {
                    lines.Add(final ? Pct1(Share(national)) + Nbsp + "% of the valid vote" : "Counted so far: " + Pct1(Share(counted)) + Nbsp + "% of every valid vote");
                }
                if (kind != ItRowKind.Life && kind != ItRowKind.Other)
                {
                    if (final)
                    {
                        int col = 0, list = 0, abroad = 0;
                        foreach (string m in members) { c.Result.CollegeSeats.TryGetValue(m, out int a); c.Result.ProportionalSeats.TryGetValue(m, out int b); c.Result.EsteroSeats.TryGetValue(m, out int e); col += a; list += b; abroad += e; }
                        lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} seats: {1} colleges · {2} list · {3} abroad", seats, col, list, abroad));
                    }
                    else { lines.Add("Colleges won so far " + won.ToString(CultureInfo.InvariantCulture)); }
                }
                CanvasRows.Slip(row.gameObject, overlay, name, SlipWrap(lines.ToArray()));
                dense.Add(new DenseLine(name + (kind == ItRowKind.Life ? string.Empty : final ? " · " + seats.ToString(CultureInfo.InvariantCulture) + " seats" : " · " + won.ToString(CultureInfo.InvariantCulture) + " colleges won"), false));
                foreach (string l in lines) { dense.Add(new DenseLine("   " + l, false)); }
            }

            var done = new HashSet<string>();
            foreach (string key in c.OrderedKeys())
            {
                if (done.Contains(key)) { continue; }
                string[] co = c.CoalitionOf(key);
                if (co != null)
                {
                    Row(ItRowKind.Coalition, CoalitionCode(co), co);
                    foreach (string m in co) { done.Add(m); Row(ItRowKind.Member, m, new[] { m }); }
                    continue;
                }
                done.Add(key);
                int k = c.K(key);
                bool stands = k >= 0 && (c.National[k] > 0.0 || c.SeatsOf(key) > 0 || key == "MAIE");
                if (!stands) { continue; }
                Row(ItRowKind.Alone, key, new[] { key });
            }
            if (c.LifeSeats > 0) { Row(ItRowKind.Life, "Life", null); }
            if (c.NationalOther > 0.5) { Row(ItRowKind.Other, "Other", null); }
            if (c.AllValid > 0.0) { dense.Add(new DenseLine("Shares are of every valid vote; the colleges' votes are the game's swing on 2018's college sizes, so the night prints shares, not vote counts", false)); }
            return rest;
        }

        /// <summary>[AUTHORED] a coalition's code and name by its members - the 2022 coalitions' customary names (CDX the centre-right, with FdI; CSX the
        /// centre-left, with PD); any other coalition its first list's name.</summary>
        private static string CoalitionCode(string[] members) => Array.IndexOf(members, "FdI") >= 0 ? "CDX" : Array.IndexOf(members, "PD") >= 0 ? "CSX" : Short(CountryId.Italy, members[0]) + "+";

        private static string CoalitionName(string[] members) => Array.IndexOf(members, "FdI") >= 0 ? "Centre-right" : Array.IndexOf(members, "PD") >= 0 ? "Centre-left" : UiFormat.Possessive(Short(CountryId.Italy, members[0]), false, true);

        /// <summary>A lane cell at a fixed width, its marks placed by fraction.</summary>
        private static Transform ItLane(Transform row, float width, float height)
        {
            var go = new GameObject("Lane");
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0f;
            le.minHeight = le.preferredHeight = height;
            return go.transform;
        }

        /// <summary>A figure over the lane, centred on its fraction and sized to its words.</summary>
        private static void ItLaneMark(ElectionNightScreen screen, Transform lane, string text, double fraction)
        {
            Text t = CanvasChrome.MakeTextRealWeight(lane, "Mark " + text, text, PoliSimTheme.Document, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform r = t.rectTransform;
            r.anchorMin = new Vector2((float)fraction, 0f);
            r.anchorMax = new Vector2((float)fraction, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(Measure(text, Px(14f), PoliSimTheme.Document) + 2f, 0f);
            r.anchoredPosition = Vector2.zero;
            screen.ItalyTexts.Add(t);
        }

        /// <summary>The lane's drawing (34a): the track, the bar to <paramref name="fill"/>, the row's tick at <paramref name="tick"/> (2 px, the lane's
        /// height), and a coalition list's short tick at 1 % (<paramref name="shortTick"/>, none below zero).</summary>
        private static void ItLaneBar(Transform lane, double fill, double tick, double shortTick, Color ink)
        {
            void Piece(string name, float x0, float x1, float y0, float y1, Color col, float width = 0f)
            {
                var go = new GameObject(name);
                go.transform.SetParent(lane, false);
                Image img = go.AddComponent<Image>();
                img.color = col;
                img.raycastTarget = false;
                RectTransform r = img.rectTransform;
                r.anchorMin = new Vector2(x0, y0);
                r.anchorMax = new Vector2(x1, y1);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                if (width > 0f) { r.sizeDelta = new Vector2(width, r.sizeDelta.y); }
            }
            Piece("Track", 0f, 1f, 0.32f, 0.68f, PoliSimTheme.CardInset);
            if (fill > 0.0) { Piece("Bar", 0f, (float)Math.Min(1.0, fill), 0.32f, 0.68f, ink); }
            if (shortTick >= 0.0) { float s = (float)Math.Min(1.0, shortTick); Piece("Tick1", s, s, 0.18f, 0.82f, PoliSimTheme.TextPrimary, BoardPx); }
            float t = (float)Math.Min(1.0, Math.Max(0.0, tick));
            Piece("Tick", t, t, 0f, 1f, PoliSimTheme.TextPrimary, Px(2f));
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE MAP
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>34a's centre column for one chamber: the title and the chamber pick, the cells, the key.</summary>
        private static GameObject ItalyMap(ElectionNightScreen screen, Transform column, ItalyNight night, ItalianCount.Chamber chamber, Transform overlay, List<DenseLine> dense)
        {
            ItalianNight.State s = night.State;
            ItalianNight.ChamberNight c = s.Of(chamber);
            bool camera = chamber == ItalianCount.Chamber.Camera;
            var rest = new GameObject("Map " + chamber);
            rest.transform.SetParent(column, false);
            rest.AddComponent<RectTransform>();
            var v = rest.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false; v.spacing = 0f;
            Transform p = rest.transform;

            string title = "The " + c.Seats.ToString(CultureInfo.InvariantCulture) + (camera ? " deputies" : " senators");
            Transform head = CanvasRows.HRow(p, "MapHead", Px(28f), Px(8f));
            Hold(ItText(screen, head, "Heading", title, Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            foreach (ItalianCount.Chamber pick in new[] { ItalianCount.Chamber.Camera, ItalianCount.Chamber.Senato }) { ChamberPick(screen, head, pick, pick == chamber); }
            Gap(p, 8f);

            ItalianCellView.Plan plan = MapPlan(night, c, out List<string> blockLines);
            ItalianCellView view = ItalianCellView.Create(p, plan, overlay, BoardPx);
            if (camera) { screen.ItalyCameraMap = view; } else { screen.ItalySenatoMap = view; }

            Gap(p, 10f);
            Transform key = CanvasRows.HRow(p, "MapKey", Px(18f), Px(16f));
            KeyEntry(screen, key, PoliSimTheme.TextSecondary, true, "a college");
            KeyEntry(screen, key, PoliSimTheme.TextSecondary, false, "a list seat");
            KeyEntry(screen, key, null, false, "to come");
            CanvasRows.Spacer(key);
            CanvasRows.Slip(key.gameObject, overlay, "The key", SlipWrap("One cell, one seat: a college with its dot, a list seat, a seat still to come"));

            dense.Add(new DenseLine(title.ToUpperInvariant() + ", IN FULL", true));
            dense.Add(new DenseLine("One cell, one seat. A block is the seats of its " + (camera ? "circoscrizione" : "region") + ": its colleges first (the dot), in U order, then its list seats in the night's order. To come: paper.", false));
            dense.Add(new DenseLine("Colleges ink as they declare; the list seats and the abroad seats when the count closes" + (camera ? string.Empty : "; the senators for life are seated by record"), false));
            foreach (string l in blockLines) { dense.Add(new DenseLine(l, false)); }
            return rest;
        }

        /// <summary>The sheet's Camera/Senato pick: the chamber shown in ink, the other on paper, each a rule round it.</summary>
        private static void ChamberPick(ElectionNightScreen screen, Transform row, ItalianCount.Chamber chamber, bool shown)
        {
            var go = new GameObject("Pick " + chamber);
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            Image face = go.AddComponent<Image>();
            face.color = shown ? PoliSimTheme.TextPrimary : PoliSimTheme.ModalBackground;
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(Px(8f), Px(8f), 0, 0);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleCenter;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Px(22f);
            le.minWidth = le.preferredWidth = Measure(chamber.ToString(), Px(14f), PoliSimTheme.Document) + 2f + 2f * Px(8f);   // the words' measure, the rule's 1 px each side, the padding
            le.flexibleWidth = 0f;
            Text t = ItText(screen, go.transform, "Words", chamber.ToString(), Px(14f), shown ? PoliSimTheme.ModalBackground : PoliSimTheme.TextPrimary, PoliSimTheme.Document);
            Hold(t);
            CanvasRows.Edges(go.transform, PoliSimTheme.TextPrimary, 1.5f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => screen.SetItalyChamber(chamber));
            CanvasRows.Slip(go, screen.Root.transform, chamber.ToString(), new[] { shown ? "The chamber shown" : "Show the count and map of the " + chamber.ToString() });
        }

        /// <summary>A key entry: a swatch (ink, or paper with the tan rule; a college's dot) and its words.</summary>
        private static void KeyEntry(ElectionNightScreen screen, Transform row, Color? ink, bool dot, string words)
        {
            Transform cell = CanvasRows.HRow(row, "Key " + words, Px(18f), Px(6f));
            var sw = new GameObject("Swatch");
            sw.transform.SetParent(cell, false);
            sw.AddComponent<RectTransform>();
            Image img = sw.AddComponent<Image>();
            img.color = ink ?? ItalianCellView.Paper;
            img.raycastTarget = false;
            LayoutElement le = sw.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Px(11f);
            le.flexibleWidth = 0f;
            if (!ink.HasValue) { CanvasRows.Edges(sw.transform, ItalianCellView.ToComeRule, BoardPx); }
            if (dot)
            {
                var d = new GameObject("Dot");
                d.transform.SetParent(sw.transform, false);
                Image di = d.AddComponent<Image>();
                di.color = ItalianCellView.Paper;
                di.raycastTarget = false;
                RectTransform r = di.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(Px(3f), Px(3f));
            }
            Hold(ItText(screen, cell, "Words", words, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
        }

        /// <summary>The map's plan for one chamber, in board pixels: every cell as the count stands, the borders, the block labels (paper text on a block at least
        /// half inked, ink text otherwise), the abroad seats and the senators for life in their boxes; each block's full line into <paramref name="blockLines"/>.</summary>
        private static ItalianCellView.Plan MapPlan(ItalyNight night, ItalianNight.ChamberNight c, out List<string> blockLines)
        {
            bool camera = c.Of == ItalianCount.Chamber.Camera, final = night.State.Final;
            ItalianNightCartogram.Chamber g = c.Geometry;
            float pitch = g.Pitch, side = pitch - 1f, dot = camera ? CameraDotPx : SenatoDotPx;
            var plan = new ItalianCellView.Plan { Width = ItalyMapPx, Height = g.MapHeight };
            int blocks = g.BlockCode.Length;
            var listKeys = new string[blocks][];
            var overflow = new int[blocks];
            for (int b = 0; b < blocks; b++) { listKeys[b] = c.BlockListKeys(b, out overflow[b]); }
            var inked = new int[blocks];
            for (int i = 0; i < g.Cells.GetLength(0); i++)
            {
                int x = g.Cells[i, 0], y = g.Cells[i, 1], b = g.Cells[i, 2], j = g.Cells[i, 3];
                int[] cols = g.BlockColleges[b];
                var cell = new ItalianCellView.Cell { X = x, Y = y, Side = side, DotSide = dot };
                if (j < cols.Length)
                {
                    int college = cols[j];
                    cell.Dot = true;
                    if (c.CollegeDeclared[college]) { cell.Ink = ItalyInk(c.Result.CollegeSeatKey[college]); }
                }
                else if (final)
                {
                    int at = j - cols.Length;
                    string key = at < listKeys[b].Length ? listKeys[b][at] : null;
                    if (key != null) { cell.Ink = ItalyInk(key); }
                }
                if (cell.Ink.HasValue) { inked[b]++; }
                plan.Cells.Add(cell);
            }
            for (int i = 0; i < g.Segs.GetLength(0); i++) { plan.Borders.Add(new Rect(g.Segs[i, 0], g.Segs[i, 1], g.Segs[i, 2], g.Segs[i, 3])); }

            blockLines = new List<string>();
            string[] circs = ItalianCount.Circoscrizioni(c.Of);
            foreach (ItalianNightCartogram.Label l in g.Labels)
            {
                int b = l.Block;
                bool paperText = l.Inside && inked[b] * 2 >= c.Magnitude(b);
                var lines = new List<string>();
                int cols = g.BlockColleges[b].Length, list = c.Magnitude(b) - cols, declared = c.DeclaredIn(b);
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} seats: {1} colleges, {2} list seats", c.Magnitude(b), cols, list));
                var won = new Dictionary<string, int>();
                foreach (int college in g.BlockColleges[b]) { if (c.CollegeDeclared[college]) { string k = c.Result.CollegeSeatKey[college]; won.TryGetValue(k, out int n); won[k] = n + 1; } }
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} of {1} colleges declared", declared, cols) + (won.Count > 0 ? ": " + Counts(won) : string.Empty));
                if (list > 0)
                {
                    if (final)
                    {
                        var comp = new Dictionary<string, int>();
                        foreach (string k in listKeys[b]) { if (k != null) { comp.TryGetValue(k, out int n); comp[k] = n + 1; } }
                        lines.Add("List seats: " + (comp.Count > 0 ? Counts(comp) : "none filled"));
                        if (overflow[b] > 0) { lines.Add(overflow[b].ToString(CultureInfo.InvariantCulture) + " more seat(s) placed here by the count than the block's list cells - not drawn"); }
                    }
                    else { lines.Add("List seats owed until the count closes: art. 83's quotient needs the whole count"); }
                }
                string head = l.Text + " · " + circs[b];
                plan.Labels.Add(new ItalianCellView.Label
                {
                    X = l.X, Y = l.Y, W = 0f, H = 16f, Size = 14f, Text = l.Text, Anchor = TextAnchor.MiddleLeft,
                    Ink = paperText ? ItalianCellView.Paper : PoliSimTheme.TextPrimary,
                    Outline = l.Inside ? (paperText ? PoliSimTheme.TextPrimary : ItalianCellView.Paper) : (Color?)null,
                    SlipHead = head, SlipLines = SlipWrap(lines.ToArray()),
                });
                blockLines.Add(head + " - " + string.Join("; ", lines));
            }

            // the boxes in the sea: the abroad seats (inked at the close) and the senators for life (inked from the start) - 34a's own placement
            void Box(float x0, float y0, List<(string Name, List<(Color? Ink, string Words)> Items)> groups, string caption, List<string> slip)
            {
                plan.Labels.Add(new ItalianCellView.Label { X = x0, Y = y0 - BoxCaptionRisePx, W = 0f, H = 18f, Size = 14f, Text = caption, Ink = PoliSimTheme.TextMuted, Anchor = TextAnchor.MiddleLeft, SlipHead = caption, SlipLines = SlipWrap(slip.ToArray()) });
                float x = x0;
                foreach ((string _, List<(Color? Ink, string Words)> items) in groups)
                {
                    foreach ((Color? ink, string _) in items) { plan.Cells.Add(new ItalianCellView.Cell { X = x, Y = y0, Side = side, Ink = ink }); x += pitch; }
                    x += BoxGroupGapPx;
                }
            }
            string[] ripartizioni = ItalianCount2022.EsteroRipartizioni;
            var abroad = new List<(string Name, List<(Color? Ink, string Words)> Items)>();
            var abroadSlip = new List<string>();
            for (int r = 0; r < ripartizioni.Length; r++)
            {
                string[] keys = c.AbroadKeys(r);
                var items = new List<(Color? Ink, string Words)>();
                var words = new List<string>();
                foreach (string k in keys)
                {
                    items.Add((final ? (k != null ? ItalyInk(k) : PoliSimTheme.Neutral) : (Color?)null, k ?? "a list no key carries"));
                    words.Add(final ? (k != null ? Short(CountryId.Italy, k) : "a list no key carries") : "owed");
                }
                abroad.Add((ripartizioni[r], items));
                abroadSlip.Add(Title(ripartizioni[r]) + ": " + string.Join(" · ", words));
            }
            abroadSlip.Add(final ? "Counted after the national votes (L. 459/2001 art. 15)" : "Counted after the national votes: owed until the count closes");
            float abroadY = camera ? CameraAbroadYPx : SenatoAbroadYPx;
            Box(BoxXPx, abroadY, abroad, "Abroad · " + c.AbroadSeats.ToString(CultureInfo.InvariantCulture), abroadSlip);
            if (c.LifeSeats > 0)
            {
                var life = new List<(Color? Ink, string Words)>();
                var lifeSlip = new List<string>();
                foreach ((string who, string group, string key) in c.Life) { life.Add((LifeInk, who)); lifeSlip.Add(who + " · " + group); }
                lifeSlip.Add("Seated by record (K154), not by the count; they vote on confidence with their group");
                Box(BoxXPx, LifeYPx, new List<(string Name, List<(Color? Ink, string Words)> Items)> { ("by record", life) }, "For life · " + c.LifeSeats.ToString(CultureInfo.InvariantCulture), lifeSlip);
            }
            return plan;
        }

        private static string Counts(Dictionary<string, int> counts)
        {
            var keys = new List<string>(counts.Keys);
            keys.Sort((a, b) => Array.IndexOf(ItalianNight.Order, a).CompareTo(Array.IndexOf(ItalianNight.Order, b)));
            var parts = new List<string>();
            foreach (string k in keys) { parts.Add(Short(CountryId.Italy, k) + " " + counts[k].ToString(CultureInfo.InvariantCulture)); }
            return string.Join(" · ", parts);
        }

        private static string Title(string upper) => upper.Length == 0 ? upper : upper.Substring(0, 1) + upper.Substring(1).ToLowerInvariant();

        /// <summary>A list's ink - the laddered ink where Italy has one, the Neutral where none is held.</summary>
        private static Color ItalyInk(string key) => key != null && PoliSimTheme.HasPartyInk(CountryId.Italy, key) ? PoliSimTheme.PartyLaddered(CountryId.Italy, key) : PoliSimTheme.Neutral;

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // WHO GOVERNS
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>D-NIGHT's column in its rect for Italy (34a, 34b): the head, the mandates and the first sitting, the outcome (or the count), art. 94's two
        /// lanes, You, Why and the budget - by the night's stage.</summary>
        private static GameObject ItalyGoverns(ElectionNightScreen screen, Transform column, ItalyNight night, Transform overlay, List<DenseLine> dense)
        {
            ItalianNight.State s = night.State;
            var rest = new GameObject("AtRest");
            rest.transform.SetParent(column, false);
            rest.AddComponent<RectTransform>();
            var v = rest.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false; v.spacing = 0f;
            Transform p = rest.transform;
            bool projected = s.Stage == ItalianNight.Stage.Called || s.Stage == ItalianNight.Stage.Final;
            GovernmentFormation.View g = projected ? night.Projection?.View : null;
            bool dissolution = g != null && !g.HasGovernment;
            string sitting = night.FirstSitting.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

            // the head
            Transform head = CanvasRows.HRow(p, "GovernsHead", Px(28f), Px(8f));
            Hold(ItText(screen, head, "Heading", "Who governs", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            if (projected)
            {
                GameObject stamp = CanvasRows.Stamp(head, "PROJECTED", Px(14f), PoliSimTheme.TextPrimary);
                screen.ItalyTexts.Add(stamp.GetComponentInChildren<Text>());
                CanvasRows.Slip(stamp, overlay, "Projected", SlipWrap("What the procedure is expected to form; the night installs nothing", "The mandates run from the first sitting (K204)", "Office changes only when both chambers grant confidence (art. 94)"));
                dense.Add(new DenseLine("WHO GOVERNS - PROJECTED", true));
                dense.Add(new DenseLine("PROJECTED · the night installs nothing. The President's mandates run from the first sitting, " + sitting + ", each government to the confidence of both chambers (art. 94).", false));
            }
            else
            {
                Hold(ItText(screen, head, "Owed", "owed", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("WHO GOVERNS - OWED UNTIL BOTH CHAMBERS ARE CALLED", true));
            }

            // the mandates
            Gap(p, 6f);
            Transform track = CanvasRows.HRow(p, "Mandates", Px(24f), Px(10f));
            Hold(ItText(screen, track, "Label", "President's mandate", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            var cells = new GameObject("Cells");
            cells.transform.SetParent(track, false);
            cells.AddComponent<RectTransform>();
            var ch = cells.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = Px(4f); ch.childControlWidth = true; ch.childControlHeight = true; ch.childForceExpandWidth = false; ch.childForceExpandHeight = false; ch.childAlignment = TextAnchor.MiddleLeft;
            int limit = ConfidenceProcedure.ItalianMandates, reached = 0;
            var trackLines = new List<string>();
            List<ItalianFormation.Mandate> mandates = projected ? night.Mandates : null;
            for (int i = 0; i < limit; i++)
            {
                GovernmentFormation.RoundCell cell = g != null && i < g.Round.Count ? g.Round[i] : new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached);
                RoundCellView(cells.transform, CountryId.Italy, cell);
                if (cell.State != GovernmentFormation.RoundCellState.NotReached) { reached = i + 1; }
                if (mandates != null && i < mandates.Count)
                {
                    ItalianFormation.Mandate m = mandates[i];
                    trackLines.Add("Mandate " + m.Number.ToString(CultureInfo.InvariantCulture) + ", on " + m.Arithmetic + ": " + (m.Camera == null ? "no vote - " + m.Failure : m.Granted
                        ? string.Format(CultureInfo.InvariantCulture, "both chambers grant ({0}-{1}, {2}-{3})", m.Camera.For, m.Camera.Against, m.Senato.For, m.Senato.Against)
                        : m.Failure + string.Format(CultureInfo.InvariantCulture, " (Camera {0}-{1}, Senato {2}-{3})", m.Camera.For, m.Camera.Against, m.Senato.For, m.Senato.Against)));
                }
            }
            if (!projected) { trackLines.Add("Owed until both chambers are called"); }
            else if (mandates != null && reached < limit && !dissolution) { trackLines.Add("Mandates " + (reached + 1).ToString(CultureInfo.InvariantCulture) + (reached + 1 < limit ? " to " + limit.ToString(CultureInfo.InvariantCulture) : string.Empty) + ": not reached"); }
            trackLines.Add("At most three mandates; then the chambers are dissolved (art. 88, K155)");
            string fig = projected ? reached.ToString(CultureInfo.InvariantCulture) + " of " + limit.ToString(CultureInfo.InvariantCulture) : "owed";
            Text figText = ItText(screen, track, "Figure", fig, Px(14f), dissolution ? PoliSimTheme.Bad : projected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, PoliSimTheme.Document);
            HoldRight(figText);
            CanvasRows.Slip(track.gameObject, overlay, "The President's mandates · at most three (K155)", SlipWrap(trackLines.ToArray()));
            foreach (string l in trackLines) { dense.Add(new DenseLine(l, false)); }

            // the first sitting
            if (projected)
            {
                Transform sit = CanvasRows.HRow(p, "Sitting", Px(22f), Px(8f));
                Hold(ItText(screen, sit, "Label", "First sitting", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                HoldRight(ItText(screen, sit, "Day", sitting, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Slip(sit.gameObject, overlay, "First sitting · " + sitting, SlipWrap("The new chambers first sit on this day (art. 61: no later than the twentieth day after the election)", "Every mandate is tried that day (DECLARED)"));
            }
            Gap(p, 6f);
            Rule(p, PoliSimTheme.RuleRow);

            // the outcome, or the count it waits on
            Transform outcome = CanvasRows.HRow(p, "Outcome", Px(30f), Px(8f));
            if (!projected)
            {
                Hold(ItText(screen, outcome, "Waits", "When the count is complete", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                HoldRight(ItText(screen, outcome, "Declared", s.Index.ToString(CultureInfo.InvariantCulture) + " of " + ItalianNight.Total.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            }
            else if (g == null)
            {
                Hold(ItText(screen, outcome, "None", "No formation was run for this night", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("No formation was run for this night", false));
            }
            else if (dissolution)
            {
                Hold(ItText(screen, outcome, "Name", "Chambers dissolved", Px(16f), PoliSimTheme.Bad, PoliSimTheme.Body));
                string polling = night.PollingAfter.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
                foreach (string l in new[] { "Three mandates fail", "The government in office serves on", "Polling day " + polling + " (art. 61)" })
                {
                    Transform reason = CanvasRows.HRow(p, "Reason", Px(20f), 0f);
                    Hold(ItText(screen, reason, "Line", l, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                }
                var lines = new List<string> { night.Projection?.Reason ?? "three mandates fail - the chambers are dissolved (art. 88)" };
                if (night.DissolutionBarred) { lines.Add("The semestre bianco (art. 88 c. 2): the President may not dissolve in the last six months of the term - the chambers are dissolved on " + night.DissolvedOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture) + ", the day the bar lifts"); }
                lines.Add("Art. 61: the new chambers are elected within seventy days - the Sunday on or before the seventieth day");
                CanvasRows.Slip(outcome.gameObject, overlay, "Chambers dissolved", SlipWrap(lines.ToArray()));
                foreach (string l in lines) { dense.Add(new DenseLine(l, false)); }
            }
            else
            {
                string name = OutcomeShort(g.Outcome);
                bool final = s.Final;
                string seatsText = final ? g.CabinetSeats.ToString(CultureInfo.InvariantCulture) : "called at " + s.CalledAt.ToString(CultureInfo.InvariantCulture);
                string ofText = final ? "of " + g.TotalSeats.ToString(CultureInfo.InvariantCulture) : string.Empty;
                float room = Px(ItalyGovernsPx) - Measure(name, Px(16f), PoliSimTheme.Body) - Measure(seatsText, final ? Px(16f) : Px(14f), PoliSimTheme.Document) - Measure(ofText, Px(14f), PoliSimTheme.Document) - 3 * Px(8f);
                MarkRow(outcome, CountryId.Italy, g.Cabinet, room);
                Hold(ItText(screen, outcome, "Name", name, Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
                HoldRight(ItText(screen, outcome, "Seats", seatsText, final ? Px(16f) : Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                if (final) { Hold(ItText(screen, outcome, "Of", ofText, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
                var lines = new List<string> { OutcomeName(g.Outcome) + " · " + Parties(g.Cabinet) };
                if (g.Support.Count > 0) { lines.Add("Support from outside · " + Parties(g.Support)); }
                if (!string.IsNullOrEmpty(g.ProjectionStep)) { lines.Add(g.ProjectionStep); }
                if (!final) { lines.Add(string.Format(CultureInfo.InvariantCulture, "Called when both chambers were: the Camera at {0}, the Senato at {1}", s.Camera.CalledAt, s.Senato.CalledAt)); }
                if (!g.DeclarationsSourced) { lines.Add(DerivedNote); }
                CanvasRows.Slip(outcome.gameObject, overlay, name, SlipWrap(lines.ToArray()));
                foreach (string l in lines) { dense.Add(new DenseLine(l, false)); }
            }

            // art. 94's two lanes
            GovernmentFormation.NightVote cameraVote = null, senatoVote = null;
            int lastMandate = 0;
            if (g != null)
            {
                foreach (GovernmentFormation.NightVote vote in g.Votes) { if (vote.Chamber == "Camera") { cameraVote = vote; } else if (vote.Chamber == "Senato") { senatoVote = vote; } }
                if (night.Mandates != null) { foreach (ItalianFormation.Mandate m in night.Mandates) { if (m.Camera != null) { lastMandate = m.Number; } } }
            }
            if (dissolution && cameraVote != null)
            {
                Transform cap = CanvasRows.HRow(p, "LastVote", Px(28f), 0f);
                Hold(ItText(screen, cap, "Line", "The last vote · mandate " + lastMandate.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            }
            dense.Add(new DenseLine("BOTH CHAMBERS (ART. 94)", true));
            ItalyLane(screen, p, night, s.Camera, cameraVote, 10f, overlay, dense);
            ItalyLane(screen, p, night, s.Senato, senatoVote, 8f, overlay, dense);

            // You, Why and the budget
            Gap(p, 12f);
            Rule(p, PoliSimTheme.RuleRow);
            Transform you = CanvasRows.HRow(p, "You", Px(30f), Px(8f));
            Hold(ItText(screen, you, "Label", "You", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            string party = night.PlayerParty;
            if (party == null)
            {
                Hold(ItText(screen, you, "None", "No player's party on this ballot", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            }
            else
            {
                CanvasRows.Mark(you, CountryId.Italy, party, Px(16f));
                Hold(ItText(screen, you, "Party", Short(CountryId.Italy, party), Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Spacer(you);
                string stampText = null; Color ink = PoliSimTheme.TextMuted;
                if (g != null && dissolution) { stampText = night.PlayerInGovernment ? "CARETAKER SERVES ON" : "EXPECTED OUT OF OFFICE"; ink = night.PlayerInGovernment ? PoliSimTheme.Caution : PoliSimTheme.Bad; }
                else if (g != null) { stampText = g.PlayerInCabinet ? "EXPECTED IN OFFICE" : "EXPECTED OUT OF OFFICE"; ink = g.PlayerInCabinet ? PoliSimTheme.Good : PoliSimTheme.Bad; }
                if (stampText != null)
                {
                    GameObject stamp = CanvasRows.Stamp(you, stampText, Px(14f), ink);
                    screen.ItalyTexts.Add(stamp.GetComponentInChildren<Text>());
                    var youLines = new List<string> { dissolution ? (night.PlayerInGovernment ? "In the government in office, which serves on as caretaker" : "Not in the government in office, which serves on as caretaker") : g.PlayerInCabinet ? "In the cabinet: expected in office" : g.PlayerSupports ? "Supports it from outside: expected out of office" : "In opposition: expected out of office",
                        "Office is cabinet membership (C-R4)" };
                    CanvasRows.Slip(you.gameObject, overlay, "You · " + Short(CountryId.Italy, party), SlipWrap(youLines.ToArray()));
                    dense.Add(new DenseLine("You: " + Short(CountryId.Italy, party) + " · " + stampText.ToLowerInvariant(), false));
                }
                else
                {
                    Hold(ItText(screen, you, "Owed", "owed", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                    dense.Add(new DenseLine("You: " + Short(CountryId.Italy, party) + " · owed until both chambers are called", false));
                }
                if (g != null && g.ReadsPlayersAnswer)
                {
                    Transform answer = CanvasRows.HRow(p, "YourAnswer", Px(20f), 0f);
                    Hold(ItText(screen, answer, "Line", "If you answer as the model would", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                    CanvasRows.Slip(answer.gameObject, overlay, "Your answer", SlipWrap("The projection reads your party as the model would answer for it (K119); yours can differ"));
                    dense.Add(new DenseLine("If you answer as the model would (K119)", false));
                }
            }
            ItalyOneLine(screen, p, party != null ? "Why " + Short(CountryId.Italy, party) + " moved" : "Why the vote moved", night.LedgerLine ?? "no campaign ledger", 12f, overlay,
                night.LedgerLine != null ? "The campaign ledger, in the † view" : "No campaign ledger is kept for this count", dense);
            ItalyOneLine(screen, p, "If the budget stands", night.BudgetLine ?? "none enacted", 10f, overlay,
                night.BudgetLine != null ? "The standing budget act's estimate" : "No enacted budget carries an estimate", dense);
            return rest;
        }

        /// <summary>A Why or budget row: the head, and Italy's one muted line at its right.</summary>
        private static void ItalyOneLine(ElectionNightScreen screen, Transform p, string head, string line, float gap, Transform overlay, string slip, List<DenseLine> dense)
        {
            Gap(p, gap);
            Transform row = CanvasRows.HRow(p, head, Px(26f), Px(10f));
            Hold(ItText(screen, row, "Head", head, Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            HoldRight(ItText(screen, row, "Line", line, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            CanvasRows.Slip(row.gameObject, overlay, head, SlipWrap(slip));
            dense.Add(new DenseLine(head + ": " + line, false));
        }

        /// <summary>One chamber's lane (34a, 34b): its name, ✓/✗, the count and the words, then the bar with its tick at half the chamber plus one - before the
        /// count the line alone; while counting the leading coalition's certain seats; once called the certain seats for; at the close the projection's vote
        /// (for and against), or the last vote where the mandates fail.</summary>
        private static void ItalyLane(ElectionNightScreen screen, Transform p, ItalyNight night, ItalianNight.ChamberNight c, GovernmentFormation.NightVote vote, float gap, Transform overlay, List<DenseLine> dense)
        {
            ItalianNight.State s = night.State;
            int members = vote != null && vote.Members > 0 ? vote.Members : c.Seats;
            int line = members / 2 + 1;   // art. 64 with every member voting: more for than against is half the chamber plus one
            string lineText = line.ToString(CultureInfo.InvariantCulture);
            Gap(p, gap);
            Transform words = CanvasRows.HRow(p, "Lane " + c.Of, Px(24f), Px(7f));
            Text nameText = CanvasRows.FixedCell(words, c.Of.ToString(), Mathf.Max(Px(58f), Measure("Senato", Px(14f), PoliSimTheme.Document) + 2f), Px(14f), PoliSimTheme.TextMuted);
            screen.ItalyTexts.Add(nameText);
            string glyph = null, number = null, said;
            bool good = true;
            var segments = new List<(int Seats, Color Ink, string Word)>();
            var lines = new List<string>();
            string leader = c.Leader != null ? (c.Leader.Length > 1 ? CoalitionCode(c.Leader) : Short(CountryId.Italy, c.Leader[0])) : null;
            if (s.Index == 0 || c.Leader == null)
            {
                said = lineText + " a majority";
                lines.Add(string.Format(CultureInfo.InvariantCulture, "The line is {0} of {1}: more for than against with every member voting (art. 64)", line, members));
            }
            else if (!s.Final && c.Called)
            {
                // M2: the call as it was stamped - the coalition it was stamped for and that coalition's certain seats at the stamp, never the live leader's bound
                string calledFor = c.CalledFor.Length > 1 ? CoalitionCode(c.CalledFor) : Short(CountryId.Italy, c.CalledFor[0]);
                glyph = "✓";
                number = c.CalledCertain.ToString(CultureInfo.InvariantCulture);
                said = "for at least · " + lineText;
                segments.Add((c.CalledCertain, PoliSimTheme.Good, "for"));
                lines.Add(string.Format(CultureInfo.InvariantCulture, "At least {0} for: {1} certain seats when the call was made, the rest in play", c.CalledCertain, UiFormat.Possessive(calledFor, false, true)));
                lines.Add("Called at " + c.CalledAt.ToString(CultureInfo.InvariantCulture) + " of " + ItalianNight.Total.ToString(CultureInfo.InvariantCulture) + " · the line " + lineText + " of " + members.ToString(CultureInfo.InvariantCulture));
            }
            else if (!s.Final)
            {
                glyph = c.LeaderCertain >= line ? "✓" : null;
                number = c.LeaderCertain.ToString(CultureInfo.InvariantCulture);
                said = leader + " certain · " + lineText;
                segments.Add((c.LeaderCertain, PoliSimTheme.TextSecondary, null));
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}: the colleges it has won ({1}) plus the list seats its counted votes already guarantee ({2})", leader, c.LeaderColleges, c.LeaderListBound));
                lines.Add(c.Of == ItalianCount.Chamber.Camera
                    ? "List seats count once its admission is certain on the counted votes: the national pool's share its counted votes take of every valid vote, rounded down - a floor"
                    : "List seats count once its admission is certain on the counted votes: in each region, that region's seats by its counted votes over every valid vote there, rounded down and summed - a floor");
                lines.Add("Not yet called · the line is " + lineText + " of " + members.ToString(CultureInfo.InvariantCulture));
            }
            else if (vote != null)
            {
                good = vote.Carried;
                glyph = good ? "✓" : "✗";
                number = vote.For.ToString(CultureInfo.InvariantCulture);
                said = "for · " + lineText + " to carry";
                segments.Add((vote.For, PoliSimTheme.Good, "for"));
                segments.Add((vote.Against + vote.Abstain, PoliSimTheme.Bad, "against"));
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} for · {1} against - carried by more for than against (art. 64); with every member voting, {2} of {3}", vote.For, vote.Against + vote.Abstain, line, members));
                if (vote.Sides.Count > 0)
                {
                    lines.Add("For · " + SideList(CountryId.Italy, vote, true, false));
                    lines.Add("Against · " + SideList(CountryId.Italy, vote, false, true));
                }
                if (c.LifeSeats > 0) { lines.Add("The " + c.LifeSeats.ToString(CultureInfo.InvariantCulture) + " for life vote with their groups (Aut as SVP, Misto) - the model puts them against"); }
            }
            else
            {
                said = "no vote · " + lineText + " to carry";
                lines.Add("No mandate reached a vote in this chamber");
            }
            if (glyph != null) { Text gl = PlText(words, "Glyph", glyph, Px(18f), good ? PoliSimTheme.Good : PoliSimTheme.Bad, PoliSimTheme.Body); Hold(gl); screen.ItalyTexts.Add(gl); }
            if (number != null) { Hold(ItText(screen, words, "Count", number, Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Document)); }
            Hold(ItText(screen, words, "Words", said, Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));

            Gap(p, 3f);
            var bar = new GameObject("VoteBar " + c.Of);
            bar.transform.SetParent(p, false);
            bar.AddComponent<RectTransform>();
            Image paper = bar.AddComponent<Image>();
            paper.color = PoliSimTheme.ModalBackground;
            paper.raycastTarget = false;
            LayoutElement bl = bar.AddComponent<LayoutElement>();
            bl.minHeight = bl.preferredHeight = Px(18f);
            CanvasRows.Edges(bar.transform, ItalianCellView.ToComeRule, BoardPx);
            double x = 0.0;
            for (int i = 0; i < segments.Count; i++)
            {
                (int seats, Color ink, string word) = segments[i];
                if (seats <= 0) { continue; }
                double from = x / members, to = Math.Min(1.0, (x + seats) / (double)members);
                x += seats;
                bool fits = word != null && Measure(word, Px(14f), PoliSimTheme.Document) + Px(12f) <= (to - from) * Px(ItalyGovernsPx);
                BarSegment(bar.transform, "Segment " + i.ToString(CultureInfo.InvariantCulture), from, to, ink, false, PoliSimTheme.ModalBackground, fits ? word : null, i == segments.Count - 1);
            }
            LaneTick(bar.transform, line / (double)members, PoliSimTheme.TextPrimary);
            string title = c.Of + " · " + (s.Final && vote != null ? "confidence (art. 94)" : s.Index == 0 ? "the line" : "the leading coalition's certain seats");
            CanvasRows.Slip(words.gameObject, overlay, title, SlipWrap(lines.ToArray()));
            dense.Add(new DenseLine(c.Of + ": " + (glyph != null ? glyph + " " : string.Empty) + (number != null ? number + " " : string.Empty) + said, false));
            foreach (string l in lines) { dense.Add(new DenseLine("   " + l, false)); }
        }

        /// <summary>A key's full name, as the roster carries it.</summary>
        private static string ItalyPartyName(string key)
        {
            foreach (PoliticalParty party in PartySystems.For(CountryId.Italy)) { if (party.Abbrev == key) { return party.Name; } }
            return key;
        }
    }
}
