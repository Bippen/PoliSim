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
    /// D-PL (Elias, 2026-10-23: "Build the Polish election night"; Design's boards 28a-28d, built on that go-ahead though none is stamped LOOKED AT):
    /// <b>POLAND'S TWO NIGHTS, IN ONE GRAMMAR</b> (28d's answer 8) - the Sejm's, counted okreg by okreg, and the President's, counted nationally.
    /// They share the nights' frame (the paper, the masthead, the foot and its CONTINUE); the body is the boards', at their own sizes - a board pixel
    /// is 1.5 canvas units, so 14 board px lands at 14 px on a 1280-wide frame of 720 and 13.8 px at 1280 × 699 (the scaler matches width and height) - where the older nights draw smaller; the shared masthead and foot keep their older sizes.
    ///
    /// <para><b>The Sejm's night</b> (28a-28b): THE COUNT - one row per committee in the night's fixed order, each with its own tick on one lane zoomed
    /// to 0-15 % (5 a party, 8 a coalition; the minority's row "exempt" and its okreg boxed, no tick), the bar what the line has counted so far; the
    /// share and the swing owed until 41 of 41, a committee's seats owed until its call lands. THE 41 OKREGI - the seat cells (`OkregiCellView`). WHO
    /// GOVERNS - since S11 (Design's board 11a) the chamber as one row, the three steps of Art. 154 and 155 as cells, the outcome and the deciding vote,
    /// the President and the government with their stamps, the snap election where none is expected, and after 41 of 41 "2023, as it happened" folded to
    /// one row where the record holds the election; the foot leads with the outcome (11b). K320's caps hold at rest, the rest is in the slips and the dagger
    /// view.</para>
    ///
    /// <para><b>The President's night</b> (28c-28d): national, no map - the field in descending share against the 50 % line, the run-off as one bar of
    /// two with a tick at 50, the ELECT row with the oath's day; turnout and votes not modelled, shares only.</para>
    /// </summary>
    public partial class ElectionNightScreen
    {
        /// <summary>[AUTHORED] layout: board pixels to canvas units at the reference width (1920 units across a 1280 frame) - at 1280 × 699 the scaler's
        /// width-height match draws a unit at 0.657 px, so the board's 14 px lands at 13.8 px there.</summary>
        private const float BoardPx = 1.5f;

        private static int Px(float boardPixels) => Mathf.RoundToInt(boardPixels * BoardPx);

        /// <summary>D-PL: the seat cells on a Polish Sejm night (null on every other night) - read by the film's log line.</summary>
        public OkregiCellView Okregi { get; private set; }

        /// <summary>What the Polish Sejm night draws beside its count - all of it read by the caller from the game, none of it from the board.</summary>
        public sealed class SejmNight
        {
            public PolishSejmNight.State State;
            /// <summary>The previous election's national shares in the night's key order (fractions), or null - the swing column's base.</summary>
            public double[] PreviousShares;
            /// <summary>The swing's head - "VS 2019" on a first night, "VS THE PREVIOUS" after.</summary>
            public string PreviousLabel;
            /// <summary>§767 (D1): the base is carried by lineage - the swing's head carries LOW CONFIDENCE, its slip the note.</summary>
            public string LineageNote;
            /// <summary>The President on the polling day and the party behind him.</summary>
            public string PresidentName, PresidentParty;
            /// <summary>The outgoing cabinet's head of government's party.</summary>
            public string GovernmentParty;
            /// <summary>History as the reference (PS-2, §619): the record's seats in the night's key order, its cabinet - null where none is held.</summary>
            public Reference Reference;
            /// <summary>The record's seats per okreg, okreg 1 to 41, in the night's key order, for the slips at 41 of 41 - null where none is held.</summary>
            public int[][] ReferenceByOkreg;
            /// <summary>S11: the Sejm's three steps as the verdict read them against the projection (`GameController.ResolveSejmVerdict`), null where none were read
            /// (a night part-way, a staged board with no verdict).</summary>
            public SejmNightSteps Steps;
            /// <summary>S11 (11b): the foot's clauses and the whole verdict, null where the night has no verdict.</summary>
            public SejmFoot Foot;
        }

        /// <summary>The President's night's four states (28c's frames).</summary>
        public enum PresidentStage { FirstRound, Between, RunOff, Elected }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE SEJM'S NIGHT
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>D-PL: Poland's Sejm night at one instant. Null, and said, where the furniture or the count is missing.</summary>
        public static ElectionNightScreen BuildPoland(SejmNight night, DateTime pollsClosed, string verdict)
        {
            if (night == null || night.State == null)
            {
                Debug.LogWarning("CANVAS: the Polish night has no count - the board is dropped, the night stays silent.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }
            PolishSejmNight.State s = night.State;
            // S11: the † cell at the masthead's right - WHO GOVERNS's full lines in the column's place (11a: "the full table is in the slip and the † view")
            Masthead(content, "POLAND  ·  THE SEJM", pollsClosed.AddMinutes(s.Minute).ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  POLLS CLOSED " + pollsClosed.ToString("HH:mm", CultureInfo.InvariantCulture),
                string.Format(CultureInfo.InvariantCulture, "{0} OF {1} OKRĘGI DECLARED", s.Declared, s.Total), screen);

            Transform body = Body(content);
            Transform overlay = screen.Root.transform;
            // 28a's columns as the board draws them, 332 · 486 · 322 of its pixels
            Transform count = Column(body, "Count", 332f);
            count.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            Transform calls = BuildCountPoland(count, night, overlay);
            Transform centre = Column(body, "MapColumn", 486f);
            screen.Okregi = BuildOkregiMap(centre, night, overlay);
            Transform right = Column(body, "Governs", 322f);
            BuildChamberPoland(screen, right, night, overlay);
            SejmFoot foot = night.Foot ?? FootOf(verdict);
            Text verdictLine = PolandFooter(content, foot, screen, overlay);

            Resolve(contentRect);
            // S11 (the real film reals11pl): the one-line captions are fitted to the rects the first pass laid - a caption held at its width runs past its column
            // otherwise, and nothing is ever cut (K320)
            bool refit = FitCalls(calls);
            if (FitExempt(count)) { refit = true; }
            if (FitPolandVerdict(verdictLine, foot)) { refit = true; }
            if (refit) { Resolve(contentRect); }
            if (screen.Okregi != null) { screen.Okregi.Rebuild(); }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        /// <summary>
        /// THE COUNT (28a's left column, 28b's answers 3 and 4): a row per committee in the night's order - its mark, its short name, the lane (0-15 %,
        /// its own tick at its line, the bar the votes counted toward the line), the share, the seats and the swing. The minority's row says exempt and
        /// boxes its okreg in the lane. A share and a swing are owed until 41 of 41; a committee's seats until its call lands (the minority's until its
        /// okreg declares). The calls follow in one line, each where it landed.
        /// </summary>
        private static Transform BuildCountPoland(Transform parent, SejmNight night, Transform overlay)
        {
            PolishSejmNight.State s = night.State;
            Transform head = CanvasRows.HRow(parent, "CountHead", Px(28f), 8f);
            PlText(head, "Heading", "THE COUNT", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            bool swingShown = s.Complete && night.PreviousShares != null && night.PreviousShares.Length == s.Keys.Length;
            if (swingShown)
            {
                // the swing's base and its flag on a line of their own: beside the heading the three do not fit the column at the floor (the real film, e7c)
                Transform swingHead = CanvasRows.HRow(parent, "SwingHead", Px(26f), 8f);
                CanvasRows.Spacer(swingHead);
                PlText(swingHead, "SwingBase", "± " + (night.PreviousLabel ?? "VS THE PREVIOUS"), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
                if (!string.IsNullOrEmpty(night.LineageNote))
                {
                    GameObject flag = CanvasRows.Stamp(swingHead, "LOW CONFIDENCE", Px(14f), PoliSimTheme.TextPrimary);
                    CanvasRows.Slip(flag, overlay, "LOW CONFIDENCE", SlipLines(night.LineageNote));
                }
            }

            float markW = Px(18f), codeW = Px(46f), shareW = Px(56f), seatsW = Px(42f), swingW = Px(44f);
            codeW = Mathf.Max(codeW, Widest(s.Keys, k => PartySystems.ShortName(CountryId.Poland, k), Px(16f), PoliSimTheme.Document) + 4f);
            shareW = Mathf.Max(shareW, Measure("00.0" + Nbsp + "%", Px(16f), PoliSimTheme.Document) + 4f, Measure("owed", Px(14f), PoliSimTheme.Document) + 4f);
            seatsW = Mathf.Max(seatsW, Measure("000", Px(16f), PoliSimTheme.Document) + 4f, Measure("owed", Px(14f), PoliSimTheme.Document) + 4f);
            swingW = Mathf.Max(swingW, Measure("−00.0", Px(14f), PoliSimTheme.Document) + 4f, Measure("owed", Px(14f), PoliSimTheme.Document) + 4f);

            // the keys: the lane's scale, and the three columns. ⚠ The real film (e7c) drew 28a's "5" and "8" over their ticks running into the end's
            // "15 %" ("5 815 %") in a lane narrower than the board's: the scale is one label, the lines are the ticks, and each row's slip names its line
            Transform keys = CanvasRows.HRow(parent, "CountKeys", Px(22f), Px(6f));
            CanvasRows.FixedCell(keys, string.Empty, markW + Px(6f) + codeW, Px(14f), PoliSimTheme.TextMuted);
            Transform laneHead = Lane(keys, "LaneHead");
            LaneMark(laneHead, "0–15" + Nbsp + "%", 1.0, Px(14f), TextAnchor.MiddleRight);
            CanvasRows.Slip(laneHead.gameObject, overlay, "THRESHOLD", ThresholdSlip(s));   // K320's test case: the legend paragraph's words, on the lane's own scale
            CanvasRows.FixedCell(keys, "share", shareW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "seats", seatsW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "±", swingW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);

            for (int k = 0; k < s.Keys.Length; k++)
            {
                string key = s.Keys[k];
                string shortName = PartySystems.ShortName(CountryId.Poland, key);
                bool exempt = s.Calls[k] == PolishSejmNight.Call.Exempt;
                Transform row = CanvasRows.HRow(parent, "Count " + key, Px(34f), Px(6f));
                Edge(row, PoliSimTheme.Hairline, top: true);
                if (!exempt && HasMark(key)) { CanvasRows.Mark(row, CountryId.Poland, key, markW); } else { Blank(row, markW); }
                CanvasRows.FixedCell(row, shortName, codeW, Px(16f), PoliSimTheme.TextPrimary);
                Transform lane = Lane(row, "Lane");
                if (exempt)
                {
                    Transform cell = CanvasRows.HRow(lane, "Exempt", Px(22f), Px(6f));
                    Stretch((RectTransform)cell);
                    HoldWidth(CanvasRows.Caption(cell, "exempt", Px(14f), PoliSimTheme.TextMuted));
                    int stands = MinorityOkreg(s, k);
                    if (stands > 0) { HoldWidth(CanvasRows.Stamp(cell, stands.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextPrimary).GetComponentInChildren<Text>()); }
                }
                else
                {
                    LaneBar(lane, Math.Min(1.0, s.LineShare(k) / LaneTop), s.Line[k] / LaneTop, InkOf(key));
                }
                bool shareShown = s.Complete;
                CanvasRows.FixedCell(row, shareShown ? Pct(s.National[k]) : "owed", shareW, shareShown ? Px(16f) : Px(14f), shareShown ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                bool seatsShown = s.SeatsKnown(k);
                CanvasRows.FixedCell(row, seatsShown ? s.Seats[k].ToString(CultureInfo.InvariantCulture) : "owed", seatsW, seatsShown ? Px(16f) : Px(14f), seatsShown ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, swingShown ? SignedPp(100.0 * (s.National[k] - night.PreviousShares[k])) : "owed", swingW, Px(14f), swingShown ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                CanvasRows.Slip(row.gameObject, overlay, shortName + " · " + PartyName(key), CountSlip(night, k));
            }

            // K320 (Elias, 2026-10-30) - its own test case: the calls at rest as one row, "THE CALLS" and each call's party and the okręg it landed at; each call in
            // words is the row's slip, and the legend paragraph that stood under it is the lane scale's THRESHOLD slip. The real films (reals11pl, reals11plw) found
            // the calls in words about 170 canvas units wider than the column at both sizes, so a call is its party's MARK and its okręg (11a's chamber-row grammar;
            // the count rows above pair each mark with its name), and the row folds its last calls into "+N" where it is still wider than the column (FitCalls)
            Transform callsRow = CanvasRows.HRow(parent, "Calls", Px(24f), Px(10f));
            Hold(PlText(callsRow, "Label", "THE CALLS", Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            List<(string Key, int At, bool Out)> calls = CallsOf(s);
            if (calls.Count == 0) { Hold(PlText(callsRow, "None", "nothing called yet", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
            bool outShown = false;
            foreach ((string key, int at, bool isOut) in calls)
            {
                Transform cell = CanvasRows.HRow(callsRow, "Call " + key, Px(24f), Px(4f));
                if (isOut && !outShown) { Hold(PlText(cell, "Out", "out", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); outShown = true; }
                if (HasMark(key)) { CanvasRows.Mark(cell, CountryId.Poland, key, Px(14f)); }
                else { Hold(PlText(cell, "Name", PartySystems.ShortName(CountryId.Poland, key), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document)); }
                Hold(PlText(cell, "At", at.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            }
            Text fold = PlText(callsRow, "Fold", "+0", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            Hold(fold);
            fold.gameObject.SetActive(false);
            CanvasRows.Slip(callsRow.gameObject, overlay, "THE CALLS", CallsSlip(s));
            return callsRow;
        }

        /// <summary>K320's test case: the calls that have landed, in first then out, each by the okręg count it landed at.</summary>
        private static List<(string Key, int At, bool Out)> CallsOf(PolishSejmNight.State s)
        {
            var landed = new List<(string Key, int At, bool Out)>();
            var outs = new List<(string Key, int At, bool Out)>();
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (s.Calls[k] == PolishSejmNight.Call.In) { landed.Add((s.Keys[k], s.CalledAt[k], false)); }
                else if (s.Calls[k] == PolishSejmNight.Call.Out) { outs.Add((s.Keys[k], s.CalledAt[k], true)); }
            }
            landed.Sort((a, b) => a.At.CompareTo(b.At));
            outs.Sort((a, b) => a.At.CompareTo(b.At));
            landed.AddRange(outs);
            return landed;
        }

        /// <summary>
        /// The calls row fitted to the COUNT COLUMN as the first pass laid it - never to the row itself: a row holding captions at their measured widths is laid at
        /// least that wide (a layout never sets a child below its minimum), so measured against its own rect it always "fits" (the cause the real film found: the
        /// calls ran 111 px past the column at 1280 and 230 px at 2560, the same ~170 canvas units). The last calls are set aside into "+N" until the row's own
        /// preferred width fits the column. True where it folded, so the page is laid again.
        /// </summary>
        private static bool FitCalls(Transform row)
        {
            if (row == null || !(row.parent is RectTransform column) || !(row is RectTransform rowRect)) { return false; }
            float room = column.rect.width;
            var cells = new List<GameObject>();
            Text fold = null;
            foreach (Transform child in row)
            {
                if (child.name.StartsWith("Call ", StringComparison.Ordinal)) { cells.Add(child.gameObject); }
                else if (child.name == "Fold") { fold = child.GetComponent<Text>(); }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
            float need = LayoutUtility.GetPreferredWidth(rowRect);
            int shown = cells.Count;
            while (need > room + 0.5f && shown > 0 && fold != null)
            {
                shown--;
                cells[shown].SetActive(false);
                fold.text = "+" + (cells.Count - shown).ToString(CultureInfo.InvariantCulture);
                fold.gameObject.SetActive(true);
                HoldWidth(fold);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
                need = LayoutUtility.GetPreferredWidth(rowRect);
            }
            if (shown == cells.Count) { return false; }
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "CANVAS: SEJM NIGHT - the calls row folded {0} of {1} call(s) into \"{2}\" to fit its column ({3:F0} of {4:F0} canvas units); every call is in its slip",
                cells.Count - shown, cells.Count, fold.text, need, room));
            return true;
        }

        /// <summary>
        /// The minority's lane fitted to its laid width (the real film at 2560: the boxed okręg ran 4.7 px past the lane's cell at 16:9, where the column is a few
        /// units narrower than at 1280 x 699): where "exempt" and the boxed okręg do not fit the lane together, the box is set aside, then the word - the row's slip
        /// and the THRESHOLD slip carry both. True where anything was set aside.
        /// </summary>
        private static bool FitExempt(Transform count)
        {
            bool changed = false;
            foreach (Transform cell in count.GetComponentsInChildren<Transform>())
            {
                if (cell.name != "Exempt" || !(cell is RectTransform rect)) { continue; }
                var optional = new List<GameObject>();
                foreach (Transform child in cell) { if (child.name.StartsWith("Stamp ", StringComparison.Ordinal)) { optional.Add(child.gameObject); } }
                foreach (Transform child in cell) { if (child.name == "Caption") { optional.Add(child.gameObject); } }
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                foreach (GameObject drop in optional)
                {
                    if (LayoutUtility.GetPreferredWidth(rect) <= rect.rect.width + 0.5f) { break; }
                    drop.SetActive(false);
                    changed = true;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "CANVAS: SEJM NIGHT - the minority's lane set aside its {0} to fit ({1:F0} units); the row's slip carries it", drop.name, rect.rect.width));
                }
            }
            return changed;
        }

        /// <summary>The first of <paramref name="candidates"/> (longest first) whose measure at its face fits <paramref name="room"/>, else the last - a one-line caption
        /// that never runs past its column and is never cut; the longer words stay in the slip.</summary>
        private static string FitWords(int size, Font font, float room, params string[] candidates)
        {
            foreach (string c in candidates) { if (Measure(c, size, font) <= room) { return c; } }
            return candidates[candidates.Length - 1];
        }

        /// <summary>The calls line's slip: each landed call in words with its okręg count, the lists still open, and how many okręgi have declared (the words the
        /// line stood in before K320).</summary>
        private static List<string> CallsSlip(PolishSejmNight.State s)
        {
            var called = new List<string>();
            var outs = new List<string>();
            var open = new List<string>();
            for (int k = 0; k < s.Keys.Length; k++)
            {
                string n = PartySystems.ShortName(CountryId.Poland, s.Keys[k]);
                if (s.Calls[k] == PolishSejmNight.Call.In) { called.Add(n + " " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture)); }
                else if (s.Calls[k] == PolishSejmNight.Call.Out) { outs.Add(n + " " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture)); }
                else if (s.Calls[k] == PolishSejmNight.Call.Open) { open.Add(n); }
            }
            var lines = new List<string>();
            lines.Add(called.Count > 0 ? "IN, AT THE OKRĘG · " + string.Join(" · ", called) : "NOTHING CALLED IN YET");
            if (outs.Count > 0) { lines.Add("OUT, AT THE OKRĘG · " + string.Join(" · ", outs)); }
            if (open.Count > 0) { lines.Add("OPEN · " + string.Join(", ", open)); }
            lines.Add(s.Declared.ToString(CultureInfo.InvariantCulture) + " OF " + s.Total.ToString(CultureInfo.InvariantCulture) + " OKRĘGI DECLARED");
            return lines;
        }

        /// <summary>K320's test case: the THRESHOLD slip - the two lines (a party's 5 %, a coalition's 8 %: `PolishSejmAllocation`'s), the minority's exemption and
        /// where it stands, and what the bar is (the legend paragraph's words, as fragments).</summary>
        private static List<string> ThresholdSlip(PolishSejmNight.State s)
        {
            string party = (100.0 * PolishSejmAllocation.PartyThreshold).ToString("0", CultureInfo.InvariantCulture) + Nbsp + "%";
            string coalition = (100.0 * PolishSejmAllocation.CoalitionThreshold).ToString("0", CultureInfo.InvariantCulture) + Nbsp + "%";
            var lines = new List<string> { party + " party · " + coalition + " coalition" };
            string exempt = ExemptLine(s);
            if (exempt != null) { lines.Add(exempt); }
            lines.Add("bar = votes counted");
            lines.Add("crosses the tick as its call lands");
            return lines;
        }

        /// <summary>The minority's exemption in the THRESHOLD slip's words: "MN exempt (okręg 21 only)", or the okręgi this count gives it a vote in; null where no
        /// list is exempt.</summary>
        private static string ExemptLine(PolishSejmNight.State s)
        {
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (s.Calls[k] != PolishSejmNight.Call.Exempt) { continue; }
                string n = PartySystems.ShortName(CountryId.Poland, s.Keys[k]);
                int stands = MinorityOkreg(s, k);
                return stands > 0 ? n + " exempt (okręg " + stands.ToString(CultureInfo.InvariantCulture) + " only)"
                    : n + " exempt (votes in " + OkregiWithVotes(s, k).ToString(CultureInfo.InvariantCulture) + " okręgi)";
            }
            return null;
        }

        /// <summary>[AUTHORED] layout: the lane's top - 28b's answer 3 zooms it to 0-15 % so 5.5 against 5 can be seen.</summary>
        private const double LaneTop = 0.15;

        /// <summary>A committee's row slip: its kind and line, its members where a coalition, where its call stands, what is counted, and its share and
        /// seats at 41 of 41 with the swing against its base.</summary>
        private static List<string> CountSlip(SejmNight night, int k)
        {
            PolishSejmNight.State s = night.State;
            string key = s.Keys[k];
            var lines = new List<string>();
            if (s.Calls[k] == PolishSejmNight.Call.Exempt)
            {
                lines.Add("A NATIONAL MINORITY'S COMMITTEE - EXEMPT FROM THE LINE (ART. 197 OF THE ELECTORAL CODE)");
                int stands = MinorityOkreg(s, k), spread = OkregiWithVotes(s, k);
                if (stands > 0)
                {
                    // D-PL2: a committee that stood in one okręg in 2023 keeps its votes there (PolishSejmAllocation.SoleOkreg), whatever its national share
                    lines.Add("STANDS IN OKRĘG " + stands.ToString(CultureInfo.InvariantCulture) + " ONLY");
                    lines.Add(s.StandsDeclared(k) ? "ITS OKRĘG HAS DECLARED" : "ITS SEATS ARE OWED UNTIL ITS OKRĘG DECLARES");
                }
                else
                {
                    lines.Add("HAS VOTES IN " + spread.ToString(CultureInfo.InvariantCulture) + " OKRĘGI IN THIS COUNT");
                    lines.Add(s.StandsDeclared(k) ? "EVERY OKRĘG WHERE IT HAS VOTES HAS DECLARED" : "ITS SEATS ARE OWED UNTIL EVERY OKRĘG WHERE IT HAS VOTES HAS DECLARED");
                }
                long votes2023 = 0;
                int c2023 = Array.IndexOf(PolishSejmDistricts2023.Committees, key);
                if (c2023 >= 0) { foreach (long[] row in PolishSejmDistricts2023.Votes) { votes2023 += row[c2023]; } }
                if (c2023 >= 0) { lines.Add("IN 2023 " + votes2023.ToString("N0", CultureInfo.InvariantCulture) + " VOTES"); }
            }
            else
            {
                lines.Add(s.Line[k] >= PolishSejmAllocation.CoalitionThreshold ? "A COALITION'S COMMITTEE · LINE 8" + Nbsp + "% (ART. 196 OF THE ELECTORAL CODE)" : "A PARTY'S COMMITTEE · LINE 5" + Nbsp + "% (ART. 196 OF THE ELECTORAL CODE)");
                if (key == "KO") { lines.Add("PO · .N · iPL · ZIELONI"); }
                if (key == "TD") { lines.Add("POLSKA 2050 · PSL"); }
                lines.Add(s.Calls[k] == PolishSejmNight.Call.In ? "IN · CALLED AT " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture) + " OF " + s.Total.ToString(CultureInfo.InvariantCulture)
                    : s.Calls[k] == PolishSejmNight.Call.Out ? "OUT · CALLED AT " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture) + " OF " + s.Total.ToString(CultureInfo.InvariantCulture)
                    : "OPEN · COUNTED SO FAR " + Pct(s.LineShare(k)) + " OF THE NATIONAL VOTE, THE LINE " + Pct(s.Line[k]));
            }
            if (s.Complete)
            {
                lines.Add(Pct(s.National[k]) + " · " + UiFormat.Seats(s.Seats[k]).ToUpperInvariant());
                if (night.PreviousShares != null && night.PreviousShares.Length == s.Keys.Length)
                {
                    lines.Add((night.PreviousLabel ?? "VS THE PREVIOUS") + " " + SignedPp(100.0 * (s.National[k] - night.PreviousShares[k])) + " POINTS");
                }
                if (s.Residue[k] > 0.5)
                {
                    lines.Add("THE SWING TAKES IT BELOW NOTHING IN SOME OKRĘGI - IT COUNTS ZERO THERE, AND THE LINE READS ITS NATIONAL VOTE");
                }
            }
            return lines;
        }

        /// <summary>How many okregi the list has a vote in.</summary>
        private static int OkregiWithVotes(PolishSejmNight.State s, int k)
        {
            int n = 0;
            foreach (PolishSejmNight.Okreg o in s.Okregi) { if (o.Votes != null && o.Votes[k] > 0.0) { n++; } }
            return n;
        }

        /// <summary>The okreg number the list stands in alone - the one its 2023 committee stood in, where that was one (`PolishSejmAllocation.SoleOkreg`,
        /// the count keeping its votes there) - or 0.</summary>
        private static int MinorityOkreg(PolishSejmNight.State s, int k) => PolishSejmAllocation.SoleOkreg(s.Keys[k]) + 1;

        /// <summary>
        /// THE 41 OKREGI (28a's centre, 28b's answers 1 and 2): each declared okreg's cells in the inks of the seats certain under the open calls, the
        /// rest paper (in play); an okreg not yet declared on the plate, dashed. The slip carries the city, the voivodeship, the magnitude, each list's
        /// share and seats there, the leader and the lead, and at 41 of 41 the okreg's real 2023 seats where the record holds them.
        /// </summary>
        private static OkregiCellView BuildOkregiMap(Transform parent, SejmNight night, Transform overlay)
        {
            PolishSejmNight.State s = night.State;
            Transform head = CanvasRows.HRow(parent, "MapHead", Px(28f), 8f);
            PlText(head, "Heading", "THE 41 OKRĘGI", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            if (s.Okregi.Length != PolishOkregiCartogram.Okregi.Length)
            {
                PlWrapped(parent, "the map draws the 41 okręgi; this count has " + s.Okregi.Length.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted);
                return null;
            }
            var fills = new OkregiCellView.OkregFill[s.Okregi.Length];
            for (int d = 0; d < s.Okregi.Length; d++)
            {
                PolishSejmNight.Okreg o = s.Okregi[d];
                PolishOkregiCartogram.Okreg place = PolishOkregiCartogram.Okregi[d];
                var fill = new OkregiCellView.OkregFill { Declared = o.Declared, SlipHead = place.Number.ToString(CultureInfo.InvariantCulture) + " · " + place.City, SlipLines = new List<string>() };
                string voivodeship = PolishOkregiCartogram.VoivodeshipNames.TryGetValue(place.Voivodeship, out string vn) ? vn : place.Voivodeship;
                fill.SlipLines.Add(voivodeship + " · " + UiFormat.Seats(o.Magnitude));
                if (place.Number == 19) { fill.SlipLines.Add("Also counts the votes cast abroad and on ships"); }
                if (!o.Declared)
                {
                    fill.SlipLines.Add("Not yet declared");
                }
                else
                {
                    var inks = new List<Color?>();
                    for (int k = 0; k < s.Keys.Length; k++) { for (int i = 0; i < o.Certain[k]; i++) { inks.Add(InkOf(s.Keys[k])); } }
                    for (int i = 0; i < o.InPlay; i++) { inks.Add(null); }
                    fill.Cells = inks.ToArray();
                    double total = 0.0;
                    foreach (double v in o.Votes) { total += v; }
                    var order = new List<int>();
                    for (int k = 0; k < s.Keys.Length; k++) { if (o.Votes[k] > 0.0) { order.Add(k); } }
                    order.Sort((a, b) => o.Votes[b].CompareTo(o.Votes[a]));
                    foreach (int k in order)
                    {
                        string seats = s.Calls[k] == PolishSejmNight.Call.Open ? "seats owed" : UiFormat.Seats(o.Certain[k]);
                        fill.SlipLines.Add(PartySystems.ShortName(CountryId.Poland, s.Keys[k]) + " " + Pct(o.Votes[k] / total) + " · " + seats);
                    }
                    if (order.Count >= 2) { fill.SlipLines.Add("Lead " + PartySystems.ShortName(CountryId.Poland, s.Keys[order[0]]) + " +" + (100.0 * (o.Votes[order[0]] - o.Votes[order[1]]) / total).ToString("0.0", CultureInfo.InvariantCulture)); }
                    if (o.InPlay > 0) { fill.SlipLines.Add(o.InPlay.ToString(CultureInfo.InvariantCulture) + (o.InPlay == 1 ? " seat waits" : " seats wait") + " on an open call"); }
                    if (s.Complete && night.ReferenceByOkreg != null && d < night.ReferenceByOkreg.Length)
                    {
                        var real = new List<string>();
                        for (int k = 0; k < s.Keys.Length; k++) { if (night.ReferenceByOkreg[d][k] > 0) { real.Add(PartySystems.ShortName(CountryId.Poland, s.Keys[k]) + " " + night.ReferenceByOkreg[d][k].ToString(CultureInfo.InvariantCulture)); } }
                        fill.SlipLines.Add("2023 as it happened: " + string.Join(" · ", real));
                    }
                }
                fills[d] = fill;
            }
            OkregiCellView view = OkregiCellView.Create(parent, fills, overlay);
            // K320: the caption paragraph becomes the key at rest, its sentences the key's slip (moved, never cut)
            Transform key = CanvasRows.HRow(parent, "MapKey", Px(22f), Px(8f));
            Hold(PlText(key, "Key", "1 cell = 1 deputy · dashed = not declared", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            CanvasRows.Slip(key.gameObject, overlay, "THE MAP", new[] { "1 cell, 1 deputy · each okręg its own magnitude", "inked: the seats it has returned", "paper in a declared okręg: a seat on an open call",
                "dashed: not yet declared · wide gaps: voivodeship borders" });
            return view;
        }

        /// <summary>
        /// S11 (Design's return of 2026-10-30, board 11a, built decluttered under K320): <b>WHO GOVERNS ON THE SEJM'S NIGHT</b>, block by block in 11a's order and at
        /// its sizes (a board pixel is 1.5 canvas units: 14 px at 1280, 28 at 2560, nothing reflows - 11c):
        /// <list type="bullet">
        /// <item><description><b>Head</b> - WHO GOVERNS and the PROJECTED stamp ("owed" while the count runs).</description></item>
        /// <item><description><b>The chamber</b> - one row of marks with their seats and "of 460" (the two-line party list it replaces is the slip's and the † view's).</description></item>
        /// <item><description><b>The steps</b> - Art. 154 · 155 as three cells (the President's, the Sejm's, the President's second): the step the formation ends at
        /// outlined with the asked party's mark, a refused one struck, one not reached dashed, the last refusal before a snap election ruled in BAD (the D-NIGHT cell,
        /// <see cref="RoundCellView"/>), and "2 of 3".</description></item>
        /// <item><description><b>Outcome</b> - the cabinet's marks, its kind and seats, or No government expected in BAD; the step in words under it.</description></item>
        /// <item><description><b>The vote</b> in the division grammar - for in GOOD from the left, abstain in tan, against in BAD from the right - its tick at 231 for
        /// Art. 154's absolute majority (the 276 line dashed while a government is expected), or at the against count for Art. 155's simple majority.</description></item>
        /// <item><description><b>President</b> and <b>Government</b> - the stamps DESIGNATES / APPOINTS AGAIN / SHORTENS TERM and OUTGOING / CARETAKER; the
        /// <b>Snap election</b> row where none is expected, its polling day boxed BY its latest day (DERIVED, each step on its window's last day).</description></item>
        /// <item><description><b>2023, as it happened</b> - folded to one row of marks and the played-less-real differences; the government of record and LOW CONFIDENCE
        /// under it. The full table is the slip's and the † view's.</description></item>
        /// </list>
        /// Every figure is the game's (the night's count, <see cref="SejmNightSteps"/>'s steps read against the projection, the record's reference), never the board's
        /// illustrative ones. ⚠ Declared, as D-NIGHT's column: the night's faces have no bold Courier, so 11a's "mono bold" is the Document face at its weight.
        /// </summary>
        private static void BuildChamberPoland(ElectionNightScreen screen, Transform column, SejmNight night, Transform overlay)
        {
            PolishSejmNight.State s = night.State;
            column.GetComponent<VerticalLayoutGroup>().spacing = 0f;
            var atRest = new GameObject("AtRest");
            atRest.transform.SetParent(column, false);
            atRest.AddComponent<RectTransform>();
            var v = atRest.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.spacing = 0f;
            Transform rest = atRest.transform;
            var dense = new List<DenseLine>();
            bool counting = !s.Complete;
            SejmNightSteps steps = counting ? null : night.Steps;

            PolandHead(rest, night, counting, overlay, dense);
            PolandChamber(rest, night, counting, overlay, dense);
            PolandSteps(rest, steps, counting, overlay, dense);
            Gap(rest, 8f);
            Rule(rest, PoliSimTheme.RuleRow);
            PolandOutcome(rest, night, steps, counting, overlay, dense);
            if (steps != null) { PolandVote(rest, night, steps, overlay, dense); }
            Gap(rest, 6f);
            Rule(rest, PoliSimTheme.RuleRow);
            PolandPresident(rest, night, steps, overlay);
            PolandGovernment(rest, night, steps, overlay);
            if (steps != null && !steps.Expected) { PolandSnap(rest, steps, overlay, dense); }
            if (s.Complete && night.Reference != null && night.Reference.Seats != null && night.Reference.Seats.Length == s.Keys.Length) { PolandHappened(rest, night, overlay, dense); }

            screen._governsAtRest = atRest;
            screen._governsDense = DenseView(column, dense);
            screen._governsDense.SetActive(false);
        }

        /// <summary>[AUTHORED] layout: the width a block's word and a row's marks are measured against, in canvas units - 11a's column is 306 px at 1280 x 699, and the
        /// built column is about 310 board px there but about 305 at 16:9 (1280 x 720, 2560 x 1440), so the measure keeps 300 and a row never runs past it.</summary>
        private static float PolandWidth => 300f * BoardPx;

        /// <summary>The heavy rule over the government of record (11a: #8A7A5C).</summary>
        private static readonly Color PolandHeavyRule = PoliSimTheme.Hex(0x8A7A5C);

        private static void PolandHead(Transform parent, SejmNight night, bool counting, Transform overlay, List<DenseLine> dense)
        {
            Transform head = CanvasRows.HRow(parent, "GovernsHead", Px(28f), Px(8f));
            Hold(PlText(head, "Heading", "WHO GOVERNS", Px(21f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            if (counting)
            {
                Hold(PlText(head, "Owed", "owed", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("WHO GOVERNS - WHEN THE COUNT IS COMPLETE", true));
                dense.Add(new DenseLine("the Sejm is not final until the last okręg declares", false));
                dense.Add(new DenseLine("Nothing is projected on a partial chamber", false));
                return;
            }
            GameObject stamp = CanvasRows.Stamp(head, "PROJECTED", Px(14f), PoliSimTheme.TextPrimary);
            CanvasRows.Slip(stamp, overlay, "Projected", SlipWrap("What the steps are expected to form", "Nothing is installed tonight", "Office changes only when the Sejm votes or the President appoints"));
            dense.Add(new DenseLine("WHO GOVERNS - EXPECTED TO FORM", true));
            dense.Add(new DenseLine("A PROJECTION - OFFICE CHANGES ONLY WHEN THE SEJM VOTES OR THE PRESIDENT APPOINTS", false));
            if (!string.IsNullOrEmpty(night.Foot?.Whole)) { dense.Add(new DenseLine(night.Foot.Whole, false)); }
        }

        /// <summary>The chamber as one row: each seated list's mark and seats in the night's order, "of 460" at the right (while counting, the seats certain so far and
        /// what is still owed); a row wider than the column folds its last lists into "+N". The order, the majority and three fifths are the slip's.</summary>
        private static void PolandChamber(Transform parent, SejmNight night, bool counting, Transform overlay, List<DenseLine> dense)
        {
            PolishSejmNight.State s = night.State;
            int seats = PartySystems.ChamberSeats(CountryId.Poland), majority = seats / 2 + 1;
            // the veto's override, three fifths (`PresidentialVeto.OverrideNumerator` / `OverrideDenominator`) - of a full chamber here; the vote counts those present
            int threeFifths = (PresidentialVeto.OverrideNumerator * seats + PresidentialVeto.OverrideDenominator - 1) / PresidentialVeto.OverrideDenominator;
            Gap(parent, 6f);
            Transform row = CanvasRows.HRow(parent, "Chamber", Px(22f), Px(12f));
            var shown = new List<int>();
            int known = 0;
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (counting && !s.SeatsKnown(k)) { continue; }
                if (s.Seats[k] <= 0) { continue; }
                shown.Add(k);
                known += s.Seats[k];
            }
            string right = counting ? (seats - known).ToString(CultureInfo.InvariantCulture) + " owed" : "of " + seats.ToString(CultureInfo.InvariantCulture);
            float rightW = Measure(right, Px(14f), PoliSimTheme.Document);
            float Cell(int k) => Px(14f) + Px(4f) + Measure(s.Seats[k].ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document);
            float used = 0f;
            int fit = 0;
            for (int i = 0; i < shown.Count; i++)
            {
                int left = shown.Count - i - 1;
                float fold = left > 0 ? Px(12f) + Measure("+" + left.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document) : 0f;
                if (used + Cell(shown[i]) + Px(12f) + fold + Px(12f) + rightW > PolandWidth) { break; }
                used += Cell(shown[i]) + Px(12f);
                fit++;
            }
            for (int i = 0; i < fit; i++)
            {
                int k = shown[i];
                Transform cell = CanvasRows.HRow(row, "Seats " + s.Keys[k], Px(22f), Px(4f));
                if (HasMark(s.Keys[k])) { CanvasRows.Mark(cell, CountryId.Poland, s.Keys[k], Px(14f)); }
                else { Hold(PlText(cell, "Name", PartySystems.ShortName(CountryId.Poland, s.Keys[k]), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document)); }
                Hold(PlText(cell, "Seats", s.Seats[k].ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            }
            if (fit < shown.Count) { Hold(PlText(row, "Fold", "+" + (shown.Count - fit).ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
            HoldRight(PlText(row, "Of", right, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));

            var all = new List<string>();
            for (int k = 0; k < s.Keys.Length; k++) { all.Add(PartySystems.ShortName(CountryId.Poland, s.Keys[k]) + " " + (counting && !s.SeatsKnown(k) ? "owed" : s.Seats[k].ToString(CultureInfo.InvariantCulture))); }
            var lines = new List<string> { string.Join(" · ", all), majority.ToString(CultureInfo.InvariantCulture) + " · a majority", threeFifths.ToString(CultureInfo.InvariantCulture) + " · three fifths: overrides a veto" };
            if (counting) { lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} of {1} okręgi declared · in play {2} · not yet {3}", s.Declared, s.Total, s.InPlay, s.NotYet)); }
            CanvasRows.Slip(row.gameObject, overlay, "The chamber · " + seats.ToString(CultureInfo.InvariantCulture), SlipWrap(lines.ToArray()));
            dense.Add(new DenseLine("THE SEJM · " + seats.ToString(CultureInfo.InvariantCulture) + " SEATS", true));
            dense.Add(new DenseLine(string.Join(" · ", all), false));
            dense.Add(new DenseLine(MajorityText(majority), false));
            dense.Add(new DenseLine(threeFifths.ToString(CultureInfo.InvariantCulture) + " · THREE FIFTHS OF A FULL SEJM: OVERRIDES THE PRESIDENT'S VETO (ART. 122 UST. 5 - THREE FIFTHS OF THOSE VOTING)", false));
            dense.Add(new DenseLine("SEATS IN THE NIGHT'S ORDER, NEVER RE-SORTED", false));
        }

        private static string MajorityText(int majority) => majority.ToString(CultureInfo.InvariantCulture) + " · A MAJORITY OF THE STATUTORY 460 (ART. 154 UST. 2-3: AN ABSOLUTE MAJORITY, HALF THE DEPUTIES PRESENT)";

        /// <summary>The three steps as cells, by <see cref="SejmNightSteps"/> - or, while counting, three dashed and "owed".</summary>
        private static void PolandSteps(Transform parent, SejmNightSteps steps, bool counting, Transform overlay, List<DenseLine> dense)
        {
            Gap(parent, 8f);
            Transform row = CanvasRows.HRow(parent, "Steps", Px(24f), Px(10f));
            Hold(PlText(row, "Label", "Art. 154 · 155", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            var cells = new GameObject("Cells");
            cells.transform.SetParent(row, false);
            cells.AddComponent<RectTransform>();
            var h = cells.AddComponent<HorizontalLayoutGroup>();
            h.spacing = Px(4f);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            bool none = steps != null && !steps.Expected;
            for (int i = 0; i < 3; i++) { RoundCellView(cells.transform, CountryId.Poland, CellOf(steps, i, counting)); }
            string fig = counting ? "owed" : steps == null ? "—" : steps.EndStep.ToString(CultureInfo.InvariantCulture) + " of 3";
            HoldRight(PlText(row, "Figure", fig, Px(14f), counting || steps == null ? PoliSimTheme.TextMuted : none ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary, PoliSimTheme.Document));

            var lines = new List<string> { "1 · President's step · Art. 154.1–2", "2 · Sejm's step · Art. 154.3", "3 · President's second · Art. 155.1",
                counting ? "Projected once the count is complete" : "outlined: the end · struck: refused · dashed: not reached" };
            CanvasRows.Slip(row.gameObject, overlay, "The steps" + (counting || steps == null ? string.Empty : " · " + fig), lines);
            if (counting || steps == null) { return; }
            dense.Add(new DenseLine("THE STEPS - KONSTYTUCJA ART. 154, 155", true));
            for (int i = 0; i < 3; i++) { dense.Add(new DenseLine(StepDenseLine(steps, i), false)); }
            dense.Add(new DenseLine("Art. 154 ust. 1-2 and 3 carry by an absolute majority with half the statutory deputies present; Art. 155 ust. 1 by more for than against with half present; refused, the President shortens the term (ust. 2)", false));
            if (steps.Partial) { dense.Add(new DenseLine("Only the step the projection ends at is known - the earlier steps are drawn refused, without a party", false)); }
        }

        /// <summary>One cell: the end outlined with its party, a step before it struck (its party faded, none where none was put or only the end is known), one after
        /// it dashed; where none is expected the third ruled in BAD.</summary>
        private static GovernmentFormation.RoundCell CellOf(SejmNightSteps steps, int i, bool counting)
        {
            if (counting || steps == null) { return new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached); }
            SejmNightSteps.Step step = steps.Steps[i];
            int n = i + 1;
            string asked = step.Voted || n == steps.EndStep ? step.Asked : null;
            if (!steps.Expected) { return new GovernmentFormation.RoundCell(asked, n < 3 ? GovernmentFormation.RoundCellState.Refused : GovernmentFormation.RoundCellState.LastRefused); }
            if (n < steps.EndStep) { return new GovernmentFormation.RoundCell(asked, GovernmentFormation.RoundCellState.Refused); }
            if (n == steps.EndStep) { return new GovernmentFormation.RoundCell(asked, GovernmentFormation.RoundCellState.Current); }
            return new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached);
        }

        private static string StepName(int n) => n == 1 ? "the President's step (Art. 154 ust. 1-2)" : n == 2 ? "the Sejm's step (Art. 154 ust. 3)" : "the President's second step (Art. 155 ust. 1)";

        /// <summary>A step in full, for the † view: who was put, the count and the outcome, or that no one was put / the step was not reached.</summary>
        private static string StepDenseLine(SejmNightSteps steps, int i)
        {
            SejmNightSteps.Step st = steps.Steps[i];
            int n = i + 1;
            string head = n.ToString(CultureInfo.InvariantCulture) + " · " + StepName(n) + ": ";
            if (steps.Expected && n > steps.EndStep) { return head + "not reached"; }
            if (!st.Voted) { return head + (steps.Partial && n != steps.EndStep ? "refused - not carried on this view" : "no cabinet put to the vote"); }
            string who = (st.Head != null ? UiFormat.Possessive(st.Head, false) : UiFormat.Possessive(PartySystems.ShortName(CountryId.Poland, st.Asked), false, true)) + " cabinet, " + SejmNightSteps.Names(st.CabinetParties)
                + (st.SupportParties.Count > 0 ? " with " + SejmNightSteps.Names(st.SupportParties) : string.Empty);
            string count = st.SimpleMajority
                ? string.Format(CultureInfo.InvariantCulture, "{0} for, {1}, {2} abstaining of {3}", st.For, st.Against >= 0 ? st.Against.ToString(CultureInfo.InvariantCulture) + " against" : "against not carried", st.Abstaining, st.Members)
                : string.Format(CultureInfo.InvariantCulture, "{0} of {1} for, {2} needed", st.For, st.Members, SejmNightSteps.Needed(st.Members));
            return head + who + " - " + count + " - " + (st.Carried ? "carried" : "refused");
        }

        /// <summary>The outcome: the cabinet's marks, its kind and seats ("of 460"), or No government expected in BAD; the step in words on a line of its own.</summary>
        private static void PolandOutcome(Transform parent, SejmNight night, SejmNightSteps steps, bool counting, Transform overlay, List<DenseLine> dense)
        {
            PolishSejmNight.State s = night.State;
            int seats = PartySystems.ChamberSeats(CountryId.Poland);
            Transform row = CanvasRows.HRow(parent, "Outcome", Px(30f), Px(8f));
            if (counting)
            {
                Hold(PlText(row, "Waits", "When the count is complete", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                string declared = s.Declared.ToString(CultureInfo.InvariantCulture) + " of " + s.Total.ToString(CultureInfo.InvariantCulture);
                HoldRight(PlText(row, "Declared", declared, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Slip(row.gameObject, overlay, "When the count is complete", SlipWrap(declared + " okręgi declared", "Nothing is projected on a partial chamber"));
                return;
            }
            if (steps == null)
            {
                Hold(PlText(row, "None", "No projection tonight", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("no projection was made for this night", false));
                return;
            }
            Transform words = null;
            if (!steps.Expected)
            {
                Hold(PlText(row, "Name", "No government expected", Px(16f), PoliSimTheme.Bad, PoliSimTheme.Body));
                var lines = new List<string> { steps.Steps[2].Voted ? "All three steps refused" : "No cabinet the President could appoint", "The President shortens the term · Art. 155.2", "The refused cabinet serves on as caretaker" };
                CanvasRows.Slip(row.gameObject, overlay, "No government expected", SlipWrap(lines.ToArray()));
                dense.Add(new DenseLine("NO GOVERNMENT EXPECTED TO FORM - THE TERM IS SHORTENED (ART. 155 UST. 2)", false));
                if (!string.IsNullOrEmpty(steps.Projection?.Reason)) { dense.Add(new DenseLine(steps.Projection.Reason, false)); }
            }
            else
            {
                SejmNightSteps.Step end = steps.Steps[steps.EndStep - 1];
                var cabinet = new List<(string Abbrev, int Seats)>();
                int cabinetSeats = 0;
                foreach (string key in end.CabinetParties) { int n = SeatsOn(s, key); cabinet.Add((key, n)); cabinetSeats += n; }
                bool majority = cabinetSeats * 2 > seats;
                string name = majority ? (end.CabinetParties.Count > 1 ? "Coalition majority" : "Majority") : "Minority cabinet";
                string seatsText = cabinetSeats.ToString(CultureInfo.InvariantCulture), ofText = "of " + seats.ToString(CultureInfo.InvariantCulture);
                float room = PolandWidth - Measure(name, Px(16f), PoliSimTheme.Body) - Measure(seatsText, Px(16f), PoliSimTheme.Document) - Measure(ofText, Px(14f), PoliSimTheme.Document) - 3 * Px(8f);
                MarkRow(row, CountryId.Poland, cabinet, room);
                Hold(PlText(row, "Name", name, Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
                HoldRight(PlText(row, "Seats", seatsText, Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                Hold(PlText(row, "Of", ofText, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                var lines = new List<string> { "In cabinet · " + SeatsList(s, end.CabinetParties), cabinetSeats.ToString(CultureInfo.InvariantCulture) + " of " + seats.ToString(CultureInfo.InvariantCulture) + " seats in cabinet" };
                if (end.SupportParties.Count > 0) { lines.Add("Support from outside · " + SeatsList(s, end.SupportParties)); }
                lines.Add("Ends at " + StepWords(steps));
                CanvasRows.Slip(row.gameObject, overlay, name, SlipWrap(lines.ToArray()));
                dense.Add(new DenseLine((name + " · " + cabinetSeats.ToString(CultureInfo.InvariantCulture) + " of " + seats.ToString(CultureInfo.InvariantCulture) + " seats in cabinet").ToUpperInvariant(), false));
                dense.Add(new DenseLine("IN CABINET  " + SeatsList(s, end.CabinetParties), false));
                if (end.SupportParties.Count > 0) { dense.Add(new DenseLine("SUPPORT FROM OUTSIDE  " + SeatsList(s, end.SupportParties), false)); }
                if (!string.IsNullOrEmpty(steps.Projection?.Step)) { dense.Add(new DenseLine(steps.Projection.Step, false)); }
            }
            words = CanvasRows.HRow(parent, "StepWords", Px(20f), 0f);
            Hold(PlText(words, "Line", FitWords(Px(14f), PoliSimTheme.Document, PolandWidth, StepWordsChoices(steps)), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
        }

        /// <summary>The step in words under the outcome (11a): "the Sejm's step · Art. 154.3", "three steps refused · Art. 155.2".</summary>
        private static string StepWords(SejmNightSteps steps)
        {
            if (!steps.Expected) { return steps.Steps[2].Voted ? "three steps refused · Art. 155.2" : "no cabinet to appoint · Art. 155.2"; }
            return steps.EndStep == 1 ? "the President's step · Art. 154.1–2" : steps.EndStep == 2 ? "the Sejm's step · Art. 154.3" : "the President's second step · Art. 155.1";
        }

        /// <summary>The step in words, longest first, for <see cref="FitWords"/>: the whole, then without the article's step name, then the article alone.</summary>
        private static string[] StepWordsChoices(SejmNightSteps steps)
        {
            if (!steps.Expected) { return steps.Steps[2].Voted ? new[] { "three steps refused · Art. 155.2", "refused · Art. 155.2" } : new[] { "no cabinet to appoint · Art. 155.2", "no cabinet · Art. 155.2" }; }
            return steps.EndStep == 1 ? new[] { "the President's step · Art. 154.1–2", "President's step · Art. 154.1–2", "Art. 154.1–2" }
                : steps.EndStep == 2 ? new[] { "the Sejm's step · Art. 154.3", "Sejm's step · Art. 154.3", "Art. 154.3" }
                : new[] { "the President's second step · Art. 155.1", "second step · Art. 155.1", "Art. 155.1" };
        }

        private static int SeatsOn(PolishSejmNight.State s, string key)
        {
            int k = Array.IndexOf(s.Keys, key);
            return k >= 0 ? s.Seats[k] : 0;
        }

        private static string SeatsList(PolishSejmNight.State s, IEnumerable<string> keys)
        {
            var parts = new List<string>();
            foreach (string key in keys) { parts.Add(PartySystems.ShortName(CountryId.Poland, key) + " " + SeatsOn(s, key).ToString(CultureInfo.InvariantCulture)); }
            return parts.Count == 0 ? "none" : string.Join(" · ", parts);
        }

        /// <summary>
        /// The deciding vote (11a): ✓/✗, the for count and the words, then the bar - for in GOOD from the left, abstain in tan, against in BAD from the right, each
        /// block's label inside it where it fits (its short label, or none) - and the tick where the rule counts: Art. 154's absolute majority at 231 from the for end
        /// with the 276 line dashed while a government is expected (three fifths, the veto's override, Art. 122 ust. 5), or Art. 155's simple majority at the
        /// against count from the for end, which the for block passes when it crosses it.
        /// </summary>
        private static void PolandVote(Transform parent, SejmNight night, SejmNightSteps steps, Transform overlay, List<DenseLine> dense)
        {
            SejmNightSteps.Step d = steps.Deciding;
            if (d == null || !d.Voted || d.Members <= 0) { return; }
            PolishSejmNight.State s = night.State;
            bool simple = d.SimpleMajority;
            int need = SejmNightSteps.Needed(d.Members);
            int seats = PartySystems.ChamberSeats(CountryId.Poland);
            int threeFifths = (PresidentialVeto.OverrideNumerator * seats + PresidentialVeto.OverrideDenominator - 1) / PresidentialVeto.OverrideDenominator;
            bool againstKnown = d.Against >= 0;
            Gap(parent, 8f);
            var block = new GameObject("Vote");
            block.transform.SetParent(parent, false);
            block.AddComponent<RectTransform>();
            var bv = block.AddComponent<VerticalLayoutGroup>();
            bv.childControlWidth = true; bv.childControlHeight = true; bv.childForceExpandWidth = true; bv.childForceExpandHeight = false; bv.spacing = 0f;

            Transform line = CanvasRows.HRow(block.transform, "Count", Px(26f), Px(7f));
            Verdict(line, d.Carried, Px(20f));
            string figure = d.For.ToString(CultureInfo.InvariantCulture);
            Hold(PlText(line, "Figure", figure, Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            // the words fitted to what the row leaves beside the glyph and the figure (longest first); the whole rule is the block's slip
            float wordsRoom = PolandWidth - Px(20f) - Measure(figure, Px(20f), PoliSimTheme.Document) - 2 * Px(7f);
            string againstText = againstKnown ? d.Against.ToString(CultureInfo.InvariantCulture) : null;
            string voteWords = !simple ? FitWords(Px(15f), PoliSimTheme.Body, wordsRoom, "for · " + need.ToString(CultureInfo.InvariantCulture) + " needed", "for")
                : !againstKnown ? FitWords(Px(15f), PoliSimTheme.Body, wordsRoom, "for · more for than against", "for")
                : FitWords(Px(15f), PoliSimTheme.Body, wordsRoom, "for · " + againstText + " against · " + (d.Carried ? "a simple majority" : "more for needed"), "for · " + againstText + " against", "for");
            Hold(PlText(line, "Words", voteWords, Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
            Gap(block.transform, 4f);

            Transform lane = Lane(block.transform, Px(20f));
            int forSeats = d.For, abstain = againstKnown ? d.Abstaining : 0, against = againstKnown ? d.Against : 0;
            double m = d.Members;
            string Fit(int n, params string[] labels)
            {
                foreach (string l in labels) { if (Measure(l, Px(14f), PoliSimTheme.Document) + Px(12f) <= PolandWidth * n / m) { return l; } }
                return null;
            }
            var blocks = new List<(string Name, int Seats, Color Ink, Color Word, string Label)>();
            if (forSeats > 0) { blocks.Add(("For", forSeats, PoliSimTheme.Good, PoliSimTheme.ModalBackground, Fit(forSeats, "for " + forSeats.ToString(CultureInfo.InvariantCulture), "for"))); }
            if (abstain > 0) { blocks.Add(("Abstain", abstain, PoliSimTheme.Hairline, PoliSimTheme.TextPrimary, Fit(abstain, "abstain " + abstain.ToString(CultureInfo.InvariantCulture), "abst."))); }
            if (against > 0) { blocks.Add(("Against", against, PoliSimTheme.Bad, PoliSimTheme.ModalBackground, Fit(against, "against " + against.ToString(CultureInfo.InvariantCulture), "against"))); }
            double x = 0.0;
            for (int i = 0; i < blocks.Count; i++)
            {
                BarSegment(lane, blocks[i].Name, x / m, (x + blocks[i].Seats) / m, blocks[i].Ink, false, blocks[i].Word, blocks[i].Label, i == blocks.Count - 1);
                x += blocks[i].Seats;
            }
            var figures = new List<(double At, string Text, Color Ink)>();
            if (!simple)
            {
                LaneTick(lane, need / m, PoliSimTheme.TextPrimary);
                figures.Add((need / m, need.ToString(CultureInfo.InvariantCulture), PoliSimTheme.TextPrimary));
                if (steps.Expected)
                {
                    DashedTick(lane, threeFifths / m);
                    figures.Add((threeFifths / m, threeFifths.ToString(CultureInfo.InvariantCulture), PoliSimTheme.TextMuted));
                }
            }
            else if (againstKnown)
            {
                LaneTick(lane, d.Against / m, PoliSimTheme.TextPrimary);
                figures.Add((d.Against / m, d.Against.ToString(CultureInfo.InvariantCulture), PoliSimTheme.TextPrimary));
            }
            var figRow = new GameObject("TickFigures");
            figRow.transform.SetParent(block.transform, false);
            figRow.AddComponent<RectTransform>();
            LayoutElement fl = figRow.AddComponent<LayoutElement>();
            fl.minHeight = fl.preferredHeight = Px(18f);
            foreach ((double at, string text, Color ink) in figures) { TickFigure(figRow.transform, at, text, ink); }

            // the long wording, kept whole: the slip and the † view
            List<string> forList, abstainList, againstList;
            SidesOf(s, d, out forList, out abstainList, out againstList);
            var lines = new List<string> { "For · " + Join(forList) };
            if (abstainList.Count > 0) { lines.Add("Abstain · " + Join(abstainList)); }
            if (againstList.Count > 0) { lines.Add("Against · " + Join(againstList)); }
            lines.Add(simple ? "the tick is the against count · " + PresidentialVeto.Quorum.ToString(CultureInfo.InvariantCulture) + " present needed"
                : "the tick is " + need.ToString(CultureInfo.InvariantCulture) + " · " + threeFifths.ToString(CultureInfo.InvariantCulture) + " dashed: overrides a veto");
            string title = steps.Expected ? (simple ? "Confidence · a simple majority" : "Confidence · an absolute majority") : "The last vote · refused";
            CanvasRows.Slip(block, overlay, title, SlipWrap(lines.ToArray()));
            dense.Add(new DenseLine("THE VOTE - " + StepName(d.Number).ToUpperInvariant(), true));
            dense.Add(new DenseLine(simple
                ? string.Format(CultureInfo.InvariantCulture, "{0} for, {1}, {2} abstaining of {3} - more for than against carries it, with at least {4} deputies present (Art. 155 ust. 1)", d.For, againstKnown ? d.Against.ToString(CultureInfo.InvariantCulture) + " against" : "the against count not carried", d.Abstaining, d.Members, PresidentialVeto.Quorum)
                : string.Format(CultureInfo.InvariantCulture, "{0} of {1} for, {2} needed - an absolute majority, every other seat against (Art. 154 ust. 2-3)", d.For, d.Members, need), false));
            dense.Add(new DenseLine("For · " + Join(forList), false));
            if (abstainList.Count > 0) { dense.Add(new DenseLine("Abstain · " + Join(abstainList), false)); }
            if (againstList.Count > 0) { dense.Add(new DenseLine("Against · " + Join(againstList), false)); }
            if (d.Sides != null) { foreach (DivisionSide side in d.Sides) { dense.Add(new DenseLine(PartySystems.ShortName(CountryId.Poland, side.Abbrev) + " " + side.Seats.ToString(CultureInfo.InvariantCulture) + " · " + side.Reason, false)); } }
        }

        private static string Join(List<string> parts) => parts.Count == 0 ? "none" : string.Join(" · ", parts);

        /// <summary>Each side's parties with their seats: Art. 155's sides as the vote took them; Art. 154's as <see cref="SejmFormation.Carries"/> counts - the cabinet and
        /// its supporters for, every other seated list against.</summary>
        private static void SidesOf(PolishSejmNight.State s, SejmNightSteps.Step d, out List<string> forList, out List<string> abstainList, out List<string> againstList)
        {
            forList = new List<string>(); abstainList = new List<string>(); againstList = new List<string>();
            if (d.Sides != null)
            {
                foreach (DivisionSide side in d.Sides)
                {
                    string w = PartySystems.ShortName(CountryId.Poland, side.Abbrev) + " " + side.Seats.ToString(CultureInfo.InvariantCulture);
                    if (side.Side > 0) { forList.Add(w); } else if (side.Side < 0) { againstList.Add(w); } else { abstainList.Add(w); }
                }
                return;
            }
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (s.Seats[k] <= 0) { continue; }
                string w = PartySystems.ShortName(CountryId.Poland, s.Keys[k]) + " " + s.Seats[k].ToString(CultureInfo.InvariantCulture);
                if (d.CabinetParties.Contains(s.Keys[k]) || d.SupportParties.Contains(s.Keys[k])) { forList.Add(w); } else { againstList.Add(w); }
            }
        }

        /// <summary>The 276 line: a 2 px dashed tick across the lane (11a), at a fraction of its width.</summary>
        private static void DashedTick(Transform lane, double at)
        {
            float x = (float)Math.Min(1.0, Math.Max(0.0, at));
            float top = -Px(5f), height = Px(30f);
            for (float y = 0f; y < height; y += Px(5f))
            {
                var dash = new GameObject("Dash");
                dash.transform.SetParent(lane, false);
                Image img = dash.AddComponent<Image>();
                img.color = PoliSimTheme.TextPrimary;
                img.raycastTarget = false;
                RectTransform r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(x, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.sizeDelta = new Vector2(3f, Mathf.Min(Px(3f), height - y));
                r.anchoredPosition = new Vector2(0f, -(top + y));
            }
        }

        /// <summary>A tick's figure under the lane, centred on its fraction of the width.</summary>
        private static void TickFigure(Transform row, double at, string text, Color ink)
        {
            Text t = CanvasChrome.MakeTextRealWeight(row, "Figure " + text, text, PoliSimTheme.Document, Px(14f), ink, TextAnchor.UpperCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2((float)Math.Min(1.0, Math.Max(0.0, at)), 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(Measure(text, Px(14f), PoliSimTheme.Document) + 8f, Px(18f));
            r.anchoredPosition = new Vector2(0f, -Px(2f));
        }

        /// <summary>The President's row: who designates first, the mark and the surname, and the stamp by the step - DESIGNATES at Art. 154's steps, APPOINTS AGAIN at
        /// Art. 155's, SHORTENS TERM (BAD) where none is expected.</summary>
        private static void PolandPresident(Transform parent, SejmNight night, SejmNightSteps steps, Transform overlay)
        {
            Transform president = CanvasRows.HRow(parent, "President", Px(30f), Px(8f));
            CanvasRows.FixedCell(president, "President", Px(84f), Px(14f), PoliSimTheme.TextMuted);
            if (!string.IsNullOrEmpty(night.PresidentParty) && HasMark(night.PresidentParty)) { CanvasRows.Mark(president, CountryId.Poland, night.PresidentParty, Px(16f)); }
            Hold(PlText(president, "Name", RowName(night.PresidentName, night.PresidentParty) ?? "—", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(president);
            bool none = steps != null && !steps.Expected, again = steps != null && steps.Expected && steps.EndStep == 3;
            string stamp = none ? "SHORTENS TERM" : again ? "APPOINTS AGAIN" : "DESIGNATES";
            CanvasRows.Stamp(president, stamp, Px(14f), none ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary);
            string who = (night.PresidentName ?? "THE PRESIDENT").ToUpperInvariant() + " · PRESIDENT";
            string[] lines = none ? new[] { "Shortens the Sejm's term and orders elections · Art. 155.2", "the poll within 45 days · Art. 98.5" }
                : again ? new[] { "Appoints a cabinet again · Art. 155.1", "confidence by more for than against" }
                : new[] { "Designates the prime minister first · Art. 154", "the new Sejm sits within 30 days · Art. 109.2", "holds the veto: three fifths of those voting override it" };
            CanvasRows.Slip(president.gameObject, overlay, who, lines);
        }

        /// <summary>The government's row: the outgoing cabinet's party with OUTGOING, or where none is expected the cabinet that serves on with CARETAKER (AMB).</summary>
        private static void PolandGovernment(Transform parent, SejmNight night, SejmNightSteps steps, Transform overlay)
        {
            Transform government = CanvasRows.HRow(parent, "Government", Px(30f), Px(8f));
            Edge(government, PoliSimTheme.RuleRow, top: true);
            CanvasRows.FixedCell(government, "Government", Px(84f), Px(14f), PoliSimTheme.TextMuted);
            bool caretaker = steps != null && !steps.Expected;
            string party = caretaker ? steps.CaretakerParty ?? night.GovernmentParty : night.GovernmentParty;
            if (!string.IsNullOrEmpty(party) && HasMark(party)) { CanvasRows.Mark(government, CountryId.Poland, party, Px(16f)); }
            Hold(PlText(government, "Party", string.IsNullOrEmpty(party) ? "—" : PartySystems.ShortName(CountryId.Poland, party), Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(government);
            CanvasRows.Stamp(government, caretaker ? "CARETAKER" : "OUTGOING", Px(14f), caretaker ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary);
            CanvasRows.Slip(government.gameObject, overlay, caretaker ? "THE CARETAKER" : "THE OUTGOING CABINET", caretaker
                ? new[] { "The refused cabinet resigns · Art. 162.2", "it carries on until the next is appointed · Art. 162.3", "until the new Sejm forms a government" }
                : new[] { "Resigns at the new Sejm's first sitting · Art. 162.1", "serves on until a new one is appointed" });
        }

        /// <summary>Where none is expected: the snap election's row - the ballot, the words, and its polling day boxed BY the latest day the steps give (DERIVED).</summary>
        private static void PolandSnap(Transform parent, SejmNightSteps steps, Transform overlay, List<DenseLine> dense)
        {
            Transform row = CanvasRows.HRow(parent, "SnapElection", Px(30f), Px(8f));
            Edge(row, PoliSimTheme.RuleRow, top: true);
            Texture2D ballot = IconLibrary.V35("ballot");
            if (ballot != null)
            {
                var icon = new GameObject("Ballot");
                icon.transform.SetParent(row, false);
                RawImage image = icon.AddComponent<RawImage>();
                image.texture = ballot;
                image.color = PoliSimTheme.TextPrimary;
                image.raycastTarget = false;
                LayoutElement le = icon.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Px(16f);
                le.flexibleWidth = 0f;
            }
            Hold(PlText(row, "Words", "Snap election", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(row);
            bool known = steps.SnapPollingDay != DateTime.MinValue;
            if (known) { CanvasRows.Stamp(row, "BY " + DeskDay(steps.SnapPollingDay), Px(14f), PoliSimTheme.TextPrimary); }
            string head = "Snap election" + (known ? " · by " + steps.SnapPollingDay.ToString("d MMM yyyy", CultureInfo.InvariantCulture) : string.Empty);
            var lines = known
                ? new List<string> { "at the latest: each step on its window's last day", "day 45 from " + steps.ShortenedOn.ToString("d MMM", CultureInfo.InvariantCulture) + " is " + steps.ShortenedOn.AddDays(SejmFormation.ShortenedTermElectionWithinDays).ToString("d MMM", CultureInfo.InvariantCulture), "the last Sunday on or before it · Art. 98.5", "DERIVED until the engine orders it" }
                : new List<string> { "the day is named once the term is shortened" };
            CanvasRows.Slip(row.gameObject, overlay, head, lines);
            dense.Add(new DenseLine("THE SNAP ELECTION", true));
            dense.Add(new DenseLine(known
                ? string.Format(CultureInfo.InvariantCulture, "the term shortened by {0:d MMMM yyyy}, the polling day by {1:d MMMM yyyy} - the last Sunday on or before the 45th day (Art. 98 ust. 5), each step on its window's last day (DERIVED)", steps.ShortenedOn, steps.SnapPollingDay)
                : "the polling day is named once the term is shortened", false));
        }

        /// <summary>
        /// "2023, as it happened" folded to one row (11a's answer 4): each list's mark and its played-less-real difference, the government of record under a heavy rule
        /// with its day, and LOW CONFIDENCE where the history is read by lineage (§767, D1). The six-row table is the slip's and the † view's, with the record's own
        /// basis for the votes of record.
        /// </summary>
        private static void PolandHappened(Transform parent, SejmNight night, Transform overlay, List<DenseLine> dense)
        {
            PolishSejmNight.State s = night.State;
            Reference reference = night.Reference;
            Gap(parent, 10f);
            Transform head = CanvasRows.HRow(parent, "ReferenceHead", Px(26f), Px(8f));
            string label = (reference.Label ?? "AS IT HAPPENED").ToLowerInvariant();
            label = label.StartsWith("poland ", StringComparison.Ordinal) ? label.Substring("poland ".Length) : label;
            string labelWords = char.ToUpperInvariant(label[0]) + label.Substring(1);
            Hold(PlText(head, "Label", labelWords, Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            // the key only where the row holds it beside the label; the differences' slip names it either way
            if (Measure(labelWords, Px(18f), PoliSimTheme.Body) + Px(8f) + Measure("played − real", Px(14f), PoliSimTheme.Document) <= PolandWidth) { HoldRight(PlText(head, "Key", "played − real", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
            CanvasRows.Slip(head.gameObject, overlay, "DATED", new[] { "THE REAL RESULT, AS THE RECORD HOLDS IT", "THE RECORD AS OF " + DeskDay(reference.RecordDate) });

            Transform diffs = CanvasRows.HRow(parent, "ReferenceDiffs", Px(22f), Px(12f));
            var pairs = new List<string>();
            dense.Add(new DenseLine((label + " - played · real · diff").ToUpperInvariant(), true));
            float used = 0f;
            int folded = 0;
            for (int k = 0; k < s.Keys.Length; k++)
            {
                int played = s.Seats[k], real = reference.Seats[k], diff = played - real;
                string shortName = PartySystems.ShortName(CountryId.Poland, s.Keys[k]);
                string signed = diff > 0 ? "+" + diff.ToString(CultureInfo.InvariantCulture) : diff < 0 ? "−" + (-diff).ToString(CultureInfo.InvariantCulture) : "0";
                pairs.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} − {2} = {3}", shortName, played, real, signed));
                dense.Add(new DenseLine(string.Format(CultureInfo.InvariantCulture, "{0} · played {1} · real {2} · {3}", shortName, played, real, signed), false));
                if (played <= 0 && real <= 0) { continue; }
                float w = Px(14f) + Px(4f) + Measure(signed, Px(14f), PoliSimTheme.Document) + Px(12f);
                if (used + w > PolandWidth - Px(40f)) { folded++; continue; }
                used += w;
                Transform cell = CanvasRows.HRow(diffs, "Diff " + s.Keys[k], Px(22f), Px(4f));
                if (HasMark(s.Keys[k])) { CanvasRows.Mark(cell, CountryId.Poland, s.Keys[k], Px(14f)); } else { Hold(PlText(cell, "Name", shortName, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document)); }
                Hold(PlText(cell, "Diff", signed, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            }
            if (folded > 0) { Hold(PlText(diffs, "Fold", "+" + folded.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
            var slip = new List<string>();
            for (int i = 0; i < pairs.Count; i += 2) { slip.Add(i + 1 < pairs.Count ? pairs[i] + " · " + pairs[i + 1] : pairs[i]); }
            slip.Add("the full table is in the † view");
            CanvasRows.Slip(diffs.gameObject, overlay, label + " · played − real", slip);

            if (!string.IsNullOrEmpty(reference.HeadParty))
            {
                Gap(parent, 4f);
                Transform cabinet = CanvasRows.HRow(parent, "ReferenceGovernment", Px(30f), Px(8f));
                Edge(cabinet, PolandHeavyRule, top: true);
                if (HasMark(reference.HeadParty)) { CanvasRows.Mark(cabinet, CountryId.Poland, reference.HeadParty, Px(16f)); }
                Hold(PlText(cabinet, "Head", Surname(reference.HeadSurname) ?? "—", Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
                CanvasRows.Stamp(cabinet, DeskDay(reference.HeadFrom), Px(14f), PoliSimTheme.TextPrimary);
                CanvasRows.Slip(cabinet.gameObject, overlay, "THE GOVERNMENT OF RECORD", SlipLines(reference.GovernmentLine));
                dense.Add(new DenseLine("THE GOVERNMENT OF RECORD", true));
                dense.Add(new DenseLine(reference.GovernmentLine ?? string.Empty, false));
                if (night.Steps != null && night.Steps.OfRecord && SejmFormation.TryOfRecord(CountryId.Poland, night.Steps.PollingDay, out SejmFormation.StepsOfRecord record))
                {
                    // the record's own basis for the votes of record (the art. 154 confidence vote lost, the Sejm's election), as the engine holds it
                    dense.Add(new DenseLine("THE RECORD: " + record.Basis, false));
                    if (WorldClock.TryGovernmentAt(CountryId.Poland, record.SejmAppointed, out WorldClock.GovernmentOfRecord elected) && !string.IsNullOrEmpty(elected.Basis)) { dense.Add(new DenseLine("THE RECORD: " + elected.Head + " - " + elected.Basis, false)); }
                }
                if (!string.IsNullOrEmpty(reference.HistoryNote))
                {
                    Transform confidence = CanvasRows.HRow(parent, "ReferenceConfidence", Px(26f), Px(8f));
                    GameObject flag = CanvasRows.Stamp(confidence, "LOW CONFIDENCE", Px(14f), PoliSimTheme.TextPrimary);
                    Hold(PlText(confidence, "Basis", "by lineage", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                    CanvasRows.Slip(flag, overlay, "LOW CONFIDENCE", SlipLines("the cabinet by lineage - " + reference.HistoryNote));
                    dense.Add(new DenseLine("LOW CONFIDENCE - THE CABINET BY LINEAGE: " + reference.HistoryNote, false));
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE PRESIDENT'S NIGHT
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// D-PL (28c-28d): Poland's presidential night in one of its four states, from the contest the game held (`PresidentialElection.Contest`) - the
        /// first round (the field and the call), between the rounds (the first round settled, the run-off owed), the run-off (the two, one bar), and
        /// elected (the ELECT row and the oath's day, where the night closes). <paramref name="sitting"/>: the President in office on the day.
        /// <paramref name="asOf"/>: the day the board is read on - between the rounds the run-off is counted down from it (D-PL2: the Docket opens that
        /// state on any day between the rounds); the first vote's day where none is given. Null, and said, where the contest has no field.
        /// </summary>
        public static ElectionNightScreen BuildPresident(PresidentialElection.Contest contest, PresidentStage stage, string sitting, DateTime pollsClosed, string verdict, DateTime? asOf = null)
        {
            if (contest == null || contest.Field == null || contest.Field.Count == 0)
            {
                Debug.LogWarning("CANVAS: the presidential night has no field - the board is dropped, the night stays silent.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }
            bool runOffStage = stage == PresidentStage.RunOff || stage == PresidentStage.Elected;
            DateTime day = runOffStage ? contest.RunOffOn : contest.FirstVote;
            string chip = stage == PresidentStage.FirstRound ? "FIRST ROUND · " + DeskDay(contest.FirstVote)
                : stage == PresidentStage.Between ? "BETWEEN THE ROUNDS"
                : stage == PresidentStage.RunOff ? "RUN-OFF · " + DeskDay(contest.RunOffOn)
                : "ELECTED · " + DeskDay(contest.RunOffHeld ? contest.RunOffOn : contest.FirstVote);
            Masthead(content, "POLAND  ·  THE PRESIDENT", "POLLS CLOSED " + pollsClosed.ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  THE NATION COUNTED AT ONCE", chip);

            Transform body = Body(content);
            Transform overlay = screen.Root.transform;
            // 28c's two columns, 790 · 320 of its pixels
            Transform field = Column(body, "Field", 790f);
            field.GetComponent<VerticalLayoutGroup>().spacing = 3f;
            Transform call = Column(body, "Call", 320f);
            call.GetComponent<VerticalLayoutGroup>().spacing = Px(12f);
            if (runOffStage && contest.RunOffHeld) { BuildRunOff(field, contest, stage == PresidentStage.Elected, overlay); }
            else
            {
                BuildField(field, contest, stage, overlay);
                if (stage == PresidentStage.Elected && contest.Elected != null) { ElectRow(field, contest, overlay); }   // elected outright in the first vote: the night closes on the ELECT row too
            }
            BuildPresidentCall(call, contest, stage, sitting, overlay, asOf ?? contest.FirstVote);
            BuildFooter(content, verdict, screen, null, null, overlay);

            Resolve(contentRect);
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        /// <summary>The field (28c frames 1-2): a row per candidate in descending share - the name, the party behind (or none), the lane 0-50 % with its
        /// end on the outright line, the share - the finalists bracketed; between the rounds the others at reduced presence (10a).</summary>
        private static void BuildField(Transform parent, PresidentialElection.Contest contest, PresidentStage stage, Transform overlay)
        {
            Transform head = CanvasRows.HRow(parent, "FieldHead", Px(26f), 8f);
            PlText(head, "Heading", string.Format(CultureInfo.InvariantCulture, "FIRST ROUND · SHARE OF THE VALID VOTE · {0} CANDIDATES", contest.Field.Count), Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body);
            float nameW = Mathf.Max(Px(190f), Widest(contest.Field, c => c.Name, Px(16f), PoliSimTheme.Body) + 6f);
            float backW = Mathf.Max(Px(108f), Measure("no Sejm party", Px(14f), PoliSimTheme.Document) + 6f);
            float figW = Mathf.Max(Px(84f), Measure("00.0\u00A0%", Px(16f), PoliSimTheme.Document) + 6f);
            Transform keys = CanvasRows.HRow(parent, "FieldKeys", Px(20f), Px(6f));
            CanvasRows.FixedCell(keys, string.Empty, Px(4f) + Px(6f) + nameW + Px(6f) + backW, Px(14f), PoliSimTheme.TextMuted);
            Transform laneHead = Lane(keys, "LaneHead");
            LaneMark(laneHead, "50\u00A0%", 1.0, Px(14f), TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, string.Empty, figW, Px(14f), PoliSimTheme.TextMuted);

            for (int i = 0; i < contest.Field.Count; i++)
            {
                PresidentialElection.Candidate c = contest.Field[i];
                bool finalist = c.Name == contest.RunOffA || c.Name == contest.RunOffB || c.Name == contest.Elected;
                Transform row = CanvasRows.HRow(parent, "Candidate " + i.ToString(CultureInfo.InvariantCulture), Px(30f), Px(6f));
                if (stage == PresidentStage.Between && !finalist) { row.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f; }
                var bracket = new GameObject("Bracket");
                bracket.transform.SetParent(row, false);
                bracket.AddComponent<RectTransform>();
                Image b = bracket.AddComponent<Image>();
                b.color = finalist ? PoliSimTheme.TextPrimary : Color.clear;
                b.raycastTarget = false;
                LayoutElement bl = bracket.AddComponent<LayoutElement>();
                bl.minWidth = bl.preferredWidth = Px(4f);
                bl.minHeight = bl.preferredHeight = Px(24f);
                CanvasRows.FixedCell(row, c.Name, nameW, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, PoliSimTheme.Body);
                CanvasRows.FixedCell(row, c.Party != null ? PartySystems.ShortName(CountryId.Poland, c.Party) : "no Sejm party", backW, Px(14f), PoliSimTheme.TextMuted);
                Transform lane = Lane(row, "Lane");
                LaneBar(lane, Math.Min(1.0, c.Share / 50.0), 1.0, CandidateInk(c));
                CanvasRows.FixedCell(row, c.Share.ToString("0.0", CultureInfo.InvariantCulture) + "\u00A0%", figW, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                var slip = new List<string> { c.Share.ToString("0.00", CultureInfo.InvariantCulture) + "\u00A0% OF THE VALID VOTE", c.Party != null ? "BACKED BY " + PartySystems.ShortName(CountryId.Poland, c.Party).ToUpperInvariant() + ", A SEJM PARTY" : "NO SEJM PARTY BEHIND THE CANDIDATE" };
                if (finalist && contest.RunOffA != null) { slip.Add("GOES TO THE RUN-OFF, " + DeskDay(contest.RunOffOn)); }
                else if (stage == PresidentStage.Between) { slip.Add("OUT OF THE RUN-OFF"); }
                CanvasRows.Slip(row.gameObject, overlay, c.Name + (c.Party != null ? " · " + PartySystems.ShortName(CountryId.Poland, c.Party) : string.Empty), slip);
            }
            if (contest.NotStanding.Count > 0)
            {
                Transform note = CanvasRows.HRow(parent, "NotStanding", Px(22f), 8f);
                var who = new List<string>();
                foreach (string why in contest.NotStanding) { int cut = why.IndexOf(" - ", StringComparison.Ordinal); who.Add(cut > 0 ? why.Substring(0, cut) : why); }
                PlText(note, "Note", "did not stand: " + string.Join(", ", who), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
                var lines = new List<string>();
                foreach (string why in contest.NotStanding) { lines.AddRange(SlipLines(why)); }
                CanvasRows.Slip(note.gameObject, overlay, "WHO DID NOT STAND", lines);
            }
        }

        /// <summary>The run-off (28c frames 3-4): the two with their shares of the two, one bar of two with a tick at 50, the lead; the first round as one
        /// stacked bar, the eliminated at reduced presence; and on the elected night the ELECT row with the oath's day.</summary>
        private static void BuildRunOff(Transform parent, PresidentialElection.Contest contest, bool elected, Transform overlay)
        {
            PresidentialElection.Candidate a = Find(contest, contest.RunOffA), b = Find(contest, contest.RunOffB);
            double shareA = contest.RunOffShareA, shareB = 100.0 - contest.RunOffShareA;
            // the leader on the left, as 28c draws the winner
            bool aFirst = shareA >= shareB;
            PresidentialElection.Candidate left = aFirst ? a : b, right = aFirst ? b : a;
            double leftShare = aFirst ? shareA : shareB, rightShare = aFirst ? shareB : shareA;

            Transform head = CanvasRows.HRow(parent, "RunOffHead", Px(26f), 8f);
            PlText(head, "Heading", "RUN-OFF · SHARE OF THE TWO", Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body);
            Transform names = CanvasRows.HRow(parent, "RunOffNames", Px(32f), 8f);
            PlText(names, "Left", (left?.Name ?? contest.RunOffA) + "  " + leftShare.ToString("0.0", CultureInfo.InvariantCulture) + "\u00A0%", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(names);
            PlText(names, "Right", rightShare.ToString("0.0", CultureInfo.InvariantCulture) + "\u00A0%  " + (right?.Name ?? contest.RunOffB), Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);

            var bar = new GameObject("RunOffBar");
            bar.transform.SetParent(parent, false);
            bar.AddComponent<RectTransform>();
            LayoutElement barSize = bar.AddComponent<LayoutElement>();
            barSize.minHeight = barSize.preferredHeight = Px(56f);
            double split = leftShare / 100.0;
            Block(bar.transform, "Left", 0.0, split, left != null ? CandidateInk(left) : PoliSimTheme.Neutral, null, Px(56f), left?.Party != null ? PartySystems.ShortName(CountryId.Poland, left.Party) : null, TextAnchor.MiddleLeft);
            Block(bar.transform, "Right", split, 1.0, right != null ? CandidateInk(right) : PoliSimTheme.Neutral, null, Px(56f), right?.Party != null ? PartySystems.ShortName(CountryId.Poland, right.Party) : null, TextAnchor.MiddleRight);
            Tick(bar.transform, 0.5, null, Px(56f));
            CanvasRows.Slip(bar, overlay, "RUN-OFF · " + DeskDay(contest.RunOffOn), new[] { (left?.Name ?? "-").ToUpperInvariant() + " " + leftShare.ToString("0.00", CultureInfo.InvariantCulture) + "\u00A0% OF THE TWO", (right?.Name ?? "-").ToUpperInvariant() + " " + rightShare.ToString("0.00", CultureInfo.InvariantCulture) + "\u00A0% OF THE TWO", "MORE THAN HALF ELECTS (ART. 127)" });
            Transform fifty = CanvasRows.HRow(parent, "Fifty", Px(22f), 8f);
            PlText(fifty, "Note", "the tick is 50\u00A0% · more than half elects", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            PlWrapped(parent, string.Format(CultureInfo.InvariantCulture, "{0} leads by {1:0.0} points of the two.", left?.Name ?? contest.RunOffA, leftShare - rightShare), Px(17f), PoliSimTheme.TextPrimary);

            PlWrapped(parent, "THE FIRST ROUND, " + DeskDay(contest.FirstVote) + " · WHERE EACH FINALIST AND EACH ELIMINATED ELECTORATE STOOD", Px(15f), PoliSimTheme.TextMuted);
            var stack = new GameObject("FirstRoundStack");
            stack.transform.SetParent(parent, false);
            stack.AddComponent<RectTransform>();
            LayoutElement stackSize = stack.AddComponent<LayoutElement>();
            stackSize.minHeight = stackSize.preferredHeight = Px(22f);
            double at = 0.0, total = 0.0, eliminated = 0.0;
            foreach (PresidentialElection.Candidate c in contest.Field) { total += c.Share; }
            foreach (PresidentialElection.Candidate c in contest.Field)
            {
                bool finalist = c.Name == contest.RunOffA || c.Name == contest.RunOffB;
                if (!finalist) { eliminated += c.Share; }
                Color ink = CandidateInk(c);
                if (!finalist) { ink = Color.Lerp(ink, PoliSimTheme.Card, 0.45f); }
                Block(stack.transform, "Share " + c.Name, at / total, (at + c.Share) / total, ink, null, Px(22f));
                at += c.Share;
            }
            PlWrapped(parent, string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0} · {2} {3:0.0} · eliminated {4:0.0}", a?.Name ?? contest.RunOffA, a?.Share ?? 0.0, b?.Name ?? contest.RunOffB, b?.Share ?? 0.0, eliminated), Px(14f), PoliSimTheme.TextMuted);
            PlWrapped(parent, "Each eliminated electorate splits between the two by its distance to each, fitted to the 2025 exit poll; a share of the first round's voters stays home - a drafted figure no source measures. "
                + "Neither is shown: the night holds shares, not votes.", Px(14f), PoliSimTheme.TextMuted);

            if (elected && contest.Elected != null) { ElectRow(parent, contest, overlay); }
        }

        /// <summary>The ELECT row (28d's answer 11): the president-elect, the party behind him and the oath's day - the night's last row; the Parliament
        /// row takes the matter from here.</summary>
        private static void ElectRow(Transform parent, PresidentialElection.Contest contest, Transform overlay)
        {
            PresidentialElection.Candidate winner = Find(contest, contest.Elected);
            Transform row = CanvasRows.HRow(parent, "Elect", Px(44f), Px(12f));
            var pad = row.GetComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(Px(14f), Px(14f), 0, 0);
            Image face = row.gameObject.AddComponent<Image>();
            face.color = PoliSimTheme.Hex(0xF5EDDE);
            CanvasRows.Edges(row, winner != null ? CandidateInk(winner) : PoliSimTheme.TextPrimary, Px(2f));
            CanvasRows.Stamp(row, "ELECT", Px(14f), PoliSimTheme.TextPrimary);
            PlText(row, "Name", contest.Elected, Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            if (contest.ElectedParty != null) { PlText(row, "Party", PartySystems.ShortName(CountryId.Poland, contest.ElectedParty), Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body); }
            CanvasRows.Spacer(row);
            PlText(row, "Oath", "oath " + DeskDay(contest.TakesOffice), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document);
            CanvasRows.Slip(row.gameObject, overlay, contest.Elected.ToUpperInvariant() + " · PRESIDENT-ELECT", new[] { "TAKES THE OATH ON " + DeskDay(contest.TakesOffice) + " (ART. 130)", "THE PARLIAMENT ROW TAKES IT FROM HERE" });
        }

        /// <summary>The call column (28c's right): the call card - what the count says and why; between the rounds the run-off OWED under its chip, two
        /// empty lanes, nothing guessed; and the ABSENT frame - turnout and votes not modelled.</summary>
        private static void BuildPresidentCall(Transform parent, PresidentialElection.Contest contest, PresidentStage stage, string sitting, Transform overlay, DateTime asOf)
        {
            PresidentialElection.Candidate a = Find(contest, contest.RunOffA), b = Find(contest, contest.RunOffB);
            string headLine, bodyLine;
            Color edge;
            if (stage == PresidentStage.FirstRound && contest.RunOffA != null)
            {
                headLine = "No one is over 50\u00A0% · the two with the most votes meet on " + DeskDay(contest.RunOffOn);
                bodyLine = string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0} and {2} {3:0.0}. More than half of the valid vote elects outright (art. 127).", contest.RunOffA, a?.Share ?? 0.0, contest.RunOffB, b?.Share ?? 0.0);
                edge = PoliSimTheme.Caution;
            }
            else if (stage == PresidentStage.Between)
            {
                headLine = "Run-off on " + DeskDay(contest.RunOffOn) + ", " + ((int)Math.Round((contest.RunOffOn - contest.FirstVote).TotalDays)).ToString(CultureInfo.InvariantCulture) + " days on";
                bodyLine = "The first round is settled and does not move. The President in office" + (string.IsNullOrEmpty(sitting) ? string.Empty : ", " + sitting + ",") + " holds the veto until the oath.";
                edge = PoliSimTheme.Hairline;
            }
            else if (contest.Elected != null)
            {
                headLine = contest.Elected + " elected";
                bodyLine = contest.RunOffHeld
                    ? "More than half of the two. The count is certain: the model counts the nation at once."
                    : "More than half of the valid vote in the first round (art. 127). The count is certain: the model counts the nation at once.";
                edge = PoliSimTheme.Good;
            }
            else
            {
                headLine = "Nothing is decided";
                bodyLine = contest.Line ?? string.Empty;
                edge = PoliSimTheme.Hairline;
            }
            Transform card = Card(parent, "CallCard", edge, Px(2f));
            PlWrapped(card, "THE CALL", Px(15f), PoliSimTheme.TextMuted);
            PlWrapped(card, headLine, Px(17f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            PlWrapped(card, bodyLine, Px(14f), PoliSimTheme.TextMuted);

            if (stage == PresidentStage.Between)
            {
                Transform owed = Card(parent, "RunOffOwed", PoliSimTheme.Hairline, 1f);
                Transform chipRow = CanvasRows.HRow(owed, "Chip", Px(24f), Px(10f));
                PlText(chipRow, "When", "RUN-OFF · " + DeskDay(contest.RunOffOn), Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body);
                var chip = new GameObject("InDays");
                chip.transform.SetParent(chipRow, false);
                chip.AddComponent<RectTransform>();
                var chipLayout = chip.AddComponent<HorizontalLayoutGroup>();
                chipLayout.padding = new RectOffset(Px(6f), Px(6f), 1, 1);
                chipLayout.childControlWidth = chipLayout.childControlHeight = true;
                chipLayout.childForceExpandWidth = chipLayout.childForceExpandHeight = false;
                chip.AddComponent<Image>().color = PoliSimTheme.Caution;
                int days = (int)Math.Round((contest.RunOffOn.Date - asOf.Date).TotalDays);   // D-PL2: from the day the board is read on
                PlText(chip.transform, "Days", "IN " + days.ToString(CultureInfo.InvariantCulture) + (days == 1 ? " DAY" : " DAYS"), Px(14f), PoliSimTheme.Card, PoliSimTheme.Document);
                foreach (PresidentialElection.Candidate c in new[] { a, b })
                {
                    if (c == null) { continue; }
                    Transform lane = CanvasRows.HRow(owed, "Owed " + c.Name, Px(22f), Px(10f));
                    CanvasRows.FixedCell(lane, c.Name, Mathf.Max(Px(110f), Measure(c.Name, Px(16f), PoliSimTheme.Body) + 6f), Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, PoliSimTheme.Body);
                    var empty = new GameObject("Empty");
                    empty.transform.SetParent(lane, false);
                    empty.AddComponent<RectTransform>();
                    LayoutElement el = empty.AddComponent<LayoutElement>();
                    el.flexibleWidth = 1f;
                    el.minHeight = el.preferredHeight = Px(14f);
                    CanvasRows.Edges(empty.transform, PoliSimTheme.HairlineStrong, 1f);
                }
                PlWrapped(owed, "Owed until the night. Nothing is guessed: no figure and no poll.", Px(14f), PoliSimTheme.TextMuted);
            }

            // the ABSENT frame's line (28c's foot): wrapped in the column, which is narrower than the line at the 14 px floor
            PlWrapped(parent, "Turnout and votes · not modelled. Shares only.", Px(14f), PoliSimTheme.TextPrimary);
        }

        private static PresidentialElection.Candidate Find(PresidentialElection.Contest contest, string name)
        {
            if (name == null) { return null; }
            foreach (PresidentialElection.Candidate c in contest.Field) { if (c.Name == name) { return c; } }
            return null;
        }

        /// <summary>28d's answer 5: a party-backed candidate in the party's ink, a candidate with no Sejm party in the Neutral - no new hue.</summary>
        private static Color CandidateInk(PresidentialElection.Candidate c) => c.Party != null ? InkOf(c.Party) : PoliSimTheme.Neutral;

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE PIECES
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>A list's ink - the installed one, or the Neutral where none is on disk (the minority's committee, 28b's answer 4).</summary>
        private static Color InkOf(string key) => PoliSimTheme.HasPartyInk(CountryId.Poland, key) ? PoliSimTheme.PartyLaddered(CountryId.Poland, key) : PoliSimTheme.Neutral;

        private static bool HasMark(string key)
        {
            foreach (PoliticalParty p in PartySystems.For(CountryId.Poland)) { if (p.Abbrev == key) { return !string.IsNullOrEmpty(p.MarkName); } }
            return false;
        }

        private static string PartyName(string key)
        {
            foreach (PoliticalParty p in PartySystems.For(CountryId.Poland)) { if (p.Abbrev == key) { return p.Name; } }
            return key;
        }

        /// <summary>The President row's name at the column's width: a surname, or, for a president the model has not named (a placeholder carrying its year
        /// in brackets), the party's short name and "candidate" - the real film found the placeholder wider than the row (88n). The slip keeps the name in full.</summary>
        private static string RowName(string name, string party)
        {
            if (!string.IsNullOrEmpty(name) && name.IndexOf('(') >= 0 && !string.IsNullOrEmpty(party)) { return PartySystems.ShortName(CountryId.Poland, party) + " candidate"; }
            return Surname(name);
        }

        private static string Surname(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            int space = name.LastIndexOf(' ');
            return space >= 0 && name.IndexOf('(') < 0 ? name.Substring(space + 1) : name;
        }

        /// <summary>F3 (the real film): a figure and its sign kept on one line - "8 / %" broke across the note's lines.</summary>
        private const string Nbsp = " ";

        private static string Pct(double share) => (100.0 * share).ToString("0.0", CultureInfo.InvariantCulture) + Nbsp + "%";

        private static string SignedPp(double pp)
        {
            double r = Math.Round(pp, 1);
            string body = Math.Abs(r).ToString("0.0", CultureInfo.InvariantCulture);
            return r > 0 ? "+" + body : r < 0 ? "−" + body : body;
        }

        /// <summary>
        /// S11 (Design's return of 2026-10-30, board 11b; K320): THE SEJM NIGHT'S FOOT, OUTCOME FIRST, ONE LINE - led by ✓ (GOOD) or ✗ (BAD), the verdict's clauses in
        /// their fixed order (the outcome, the figure that decided it, the date or the caretaker) in the serif at 16 board px, beside CONTINUE. A clause that does not
        /// fit drops WHOLE from the end (<see cref="FitPolandVerdict"/>): a clause is never cut and " …" never appears. The whole verdict - the outcome, then the
        /// procedure as first built - is the line's slip. It replaces K302's foot (R2-M1), which led with the procedure at the shared foot's 12-unit bold capitals
        /// and cut it at a word, so the outcome fell under " …".
        /// </summary>
        private static Text PolandFooter(Transform parent, SejmFoot foot, ElectionNightScreen screen, Transform overlay)
        {
            var row = new GameObject("Footer");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 24f;
            row.AddComponent<LayoutElement>().minHeight = 56f;
            Text line = null;
            if (foot != null && foot.Clauses.Count > 0)
            {
                Transform verdict = CanvasRows.HRow(row.transform, "VerdictRow", Px(24f), Px(8f));
                verdict.GetComponent<LayoutElement>().flexibleWidth = 1f;
                if (foot.Carried.HasValue) { Verdict(verdict, foot.Carried.Value, Px(18f)); }
                line = CanvasChrome.MakeTextRealWeight(verdict, "Verdict", string.Join(" · ", foot.Clauses), PoliSimTheme.Body, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
                line.horizontalOverflow = HorizontalWrapMode.Overflow;
                line.verticalOverflow = VerticalWrapMode.Overflow;
                line.raycastTarget = false;
                LayoutElement le = line.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.minWidth = 0f;
                CanvasRows.Slip(verdict.gameObject, overlay, "The verdict", SlipWrap(foot.Whole ?? string.Join(" · ", foot.Clauses)));
            }
            else { CanvasRows.Spacer(row.transform); }
            BuildContinueButton(row.transform, screen.Dismiss);
            return line;
        }

        /// <summary>A verdict handed in as one sentence (a caller with no clauses): one clause, its sentence the slip.</summary>
        private static SejmFoot FootOf(string verdict)
        {
            if (string.IsNullOrEmpty(verdict)) { return null; }
            var foot = new SejmFoot { Whole = verdict };
            foot.Clauses.Add(verdict);
            return foot;
        }

        /// <summary>S11 (11b): the clauses dropped whole from the end until the line fits the room its laid rect holds - measured in canvas units by the text's own
        /// generator - and K320's cap at rest (K321 (b): at most <see cref="SejmNightSteps.RestWords"/> counted words); the first clause, the outcome, always stands.
        /// True where a clause was dropped, so the page is laid again.</summary>
        private static bool FitPolandVerdict(Text line, SejmFoot foot)
        {
            if (line == null || foot == null || foot.Clauses.Count == 0) { return false; }
            float room = line.rectTransform.rect.width;
            for (int n = foot.Clauses.Count; n >= 1; n--)
            {
                string text = string.Join(" · ", foot.Clauses.GetRange(0, n));
                line.text = text;
                if (n > 1 && (SejmNightSteps.CountedWords(text) > SejmNightSteps.RestWords || line.preferredWidth > room)) { continue; }
                if (n < foot.Clauses.Count) { Debug.Log(string.Format(CultureInfo.InvariantCulture, "CANVAS: SEJM NIGHT - the Sejm night's foot shows {0} of {1} clauses (\"{2}\", {3} counted words, {4:F0} of {5:F0} units); the rest is in its slip", n, foot.Clauses.Count, text, SejmNightSteps.CountedWords(text), line.preferredWidth, room)); }
                return n < foot.Clauses.Count;
            }
            return false;
        }

        /// <summary>The body row the columns sit in, taking the page's spare height (the older nights' `BuildBody` rule).</summary>
        private static Transform Body(Transform parent)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            body.AddComponent<RectTransform>();
            var grid = body.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = 26f * BoardPx;
            grid.childControlWidth = true;
            grid.childControlHeight = true;
            grid.childForceExpandHeight = true;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return body.transform;
        }

        /// <summary>§578: a wrapped Canvas text measures its height at the width it has now - so the page is laid twice, the second pass at the widths
        /// the first settled.</summary>
        private static void Resolve(RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private static Text PlText(Transform parent, string name, string text, int size, Color ink, Font font, FontStyle style = FontStyle.Normal)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, name, text, font ?? PoliSimTheme.Document, size, ink, TextAnchor.MiddleLeft, style);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A wrapped line in a column, its floor the face's own line (PF-13: a wrapped Canvas caption needs a floor of its own).</summary>
        private static void PlWrapped(Transform parent, string text, int size, Color ink, Font font = null)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, "Note", text, font ?? PoliSimTheme.Document, size, ink, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.raycastTarget = false;
            t.gameObject.AddComponent<LayoutElement>().minHeight = size * 1.35f;
        }

        /// <summary>D-PL2 (the real films real902/real902w: the exempt cell's caption and stamp compressed below their text by the row's layout): a text
        /// held at its measured width, so the row cannot shrink it - its words never at a smaller size or clipped.</summary>
        private static void HoldWidth(Text text)
        {
            if (text == null) { return; }
            LayoutElement le = text.GetComponent<LayoutElement>();
            if (le == null) { le = text.gameObject.AddComponent<LayoutElement>(); }   // Unity's null, never ??
            le.minWidth = le.preferredWidth = Mathf.Max(Measure(text.text, text.fontSize, text.font), Mathf.Ceil(text.preferredWidth));   // the text's own unwrapped width too (US-36's film: a bold name escaped a probe's measure by 1.6 px)
            le.flexibleWidth = 0f;
        }

        private static float Measure(string text, int size, Font font)
        {
            Text probe = CanvasChrome.MakeTextRealWeight(null, "Probe", text, font, size, Color.white, TextAnchor.MiddleLeft);
            probe.horizontalOverflow = HorizontalWrapMode.Overflow;
            float w = probe.preferredWidth;
            UnityEngine.Object.Destroy(probe.gameObject);
            return Mathf.Ceil(w);
        }

        private static float Widest<T>(IEnumerable<T> items, Func<T, string> text, int size, Font font)
        {
            float w = 0f;
            foreach (T item in items) { w = Mathf.Max(w, Measure(text(item), size, font)); }
            return w;
        }

        private static void Blank(Transform row, float width)
        {
            var go = new GameObject("Blank");
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0f;
        }

        /// <summary>A rule on a row's top or bottom edge, outside its layout.</summary>
        private static void Edge(Transform row, Color ink, bool top)
        {
            var e = new GameObject(top ? "RuleTop" : "RuleBottom");
            e.transform.SetParent(row, false);
            var r = e.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, top ? 1f : 0f);
            r.anchorMax = new Vector2(1f, top ? 1f : 0f);
            r.pivot = new Vector2(0.5f, top ? 1f : 0f);
            r.sizeDelta = new Vector2(0f, 1.5f);
            r.anchoredPosition = Vector2.zero;
            e.AddComponent<LayoutElement>().ignoreLayout = true;
            Image img = e.AddComponent<Image>();
            img.color = ink;
            img.raycastTarget = false;
        }

        /// <summary>A lane cell in a row: it takes the row's spare width (its floor a third of the board's), its marks placed by fraction.</summary>
        private static Transform Lane(Transform row, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = Px(40f);
            le.preferredWidth = Px(120f);
            le.flexibleWidth = 1f;
            le.minHeight = le.preferredHeight = Px(22f);
            return go.transform;
        }

        /// <summary>A figure over the lane at a fraction of its width (the lines' numbers, the lane's end).</summary>
        private static void LaneMark(Transform lane, string text, double fraction, int size, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            Text t = CanvasChrome.MakeTextRealWeight(lane, "Mark " + text, text, PoliSimTheme.Document, size, PoliSimTheme.TextMuted, anchor);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            float width = Measure(text, size, PoliSimTheme.Document) + 4f;
            RectTransform r = t.rectTransform;
            r.anchorMin = new Vector2((float)fraction, 0f);
            r.anchorMax = new Vector2((float)fraction, 1f);
            r.pivot = new Vector2(anchor == TextAnchor.MiddleRight ? 1f : 0.5f, 0.5f);
            r.sizeDelta = new Vector2(width, 0f);
            r.anchoredPosition = Vector2.zero;
        }

        /// <summary>The lane's drawing: the track, the bar to <paramref name="fill"/> of its width in <paramref name="ink"/>, the tick at
        /// <paramref name="tick"/> (none where it sits at the lane's end and the end is the line, as the President's 50 %).</summary>
        private static void LaneBar(Transform lane, double fill, double tick, Color ink)
        {
            void Piece(string name, float x0, float x1, float y0, float y1, Color c, float minWidth = 0f)
            {
                var go = new GameObject(name);
                go.transform.SetParent(lane, false);
                Image img = go.AddComponent<Image>();
                img.color = c;
                img.raycastTarget = false;
                RectTransform r = img.rectTransform;
                r.anchorMin = new Vector2(x0, y0);
                r.anchorMax = new Vector2(x1, y1);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                if (minWidth > 0f) { r.sizeDelta = new Vector2(minWidth, r.sizeDelta.y); }
            }
            Piece("Track", 0f, 1f, 0.32f, 0.68f, PoliSimTheme.CardInset);
            if (fill > 0.0) { Piece("Bar", 0f, (float)Math.Min(1.0, fill), 0.32f, 0.68f, ink); }
            float t = (float)Math.Min(1.0, Math.Max(0.0, tick));
            Piece("Tick", t, t, 0f, 1f, PoliSimTheme.TextPrimary, 3f);
        }

        /// <summary>One block of a horizontal bar, by fraction of its width; an optional rule round it and an optional word inside it.</summary>
        private static void Block(Transform bar, string name, double from, double to, Color fill, Color? rule, float height = 0f, string word = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bar, false);
            Image img = go.AddComponent<Image>();
            img.color = fill;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2((float)from, 1f);
            r.anchorMax = new Vector2((float)to, 1f);
            r.pivot = new Vector2(0f, 1f);
            float h = height > 0f ? height : Px(30f);
            r.sizeDelta = new Vector2(0f, h);
            r.anchoredPosition = Vector2.zero;
            if (rule.HasValue) { CanvasRows.Edges(go.transform, rule.Value, 1.5f); }
            if (!string.IsNullOrEmpty(word))
            {
                Text t = CanvasChrome.MakeTextRealWeight(go.transform, "Word", word, PoliSimTheme.Document, Px(16f), PoliSimTheme.Card, anchor);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.raycastTarget = false;
                RectTransform tr = t.rectTransform;
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(Px(12f), 0f);
                tr.offsetMax = new Vector2(-Px(12f), 0f);
                // a word wider than its block is not drawn - the slip and the names carry it
                t.gameObject.AddComponent<WordFit>().Word = word;
            }
        }

        /// <summary>A tick across a bar at a fraction of its width, its figure under it (none where null).</summary>
        private static void Tick(Transform bar, double at, string figure, float barHeight = 0f)
        {
            float h = barHeight > 0f ? barHeight : Px(30f);
            var go = new GameObject("Tick " + (figure ?? "50"));
            go.transform.SetParent(bar, false);
            Image img = go.AddComponent<Image>();
            img.color = PoliSimTheme.TextPrimary;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2((float)at, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(3f, h + Px(10f));
            r.anchoredPosition = new Vector2(0f, Px(5f));
            if (figure == null) { return; }
            Text t = CanvasChrome.MakeTextRealWeight(bar, "Figure " + figure, figure, PoliSimTheme.Document, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.UpperCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform tr = t.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2((float)at, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(Measure(figure, Px(14f), PoliSimTheme.Document) + 8f, Px(20f));
            tr.anchoredPosition = new Vector2(0f, -(h + Px(7f)));
        }

        /// <summary>A card: paper with a rule round it, its lines stacked inside a padding.</summary>
        private static Transform Card(Transform parent, string name, Color rule, float ruleWidth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = PoliSimTheme.Hex(0xF5EDDE);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(Px(14f), Px(14f), Px(12f), Px(12f));
            v.spacing = Px(6f);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            CanvasRows.Edges(go.transform, rule, ruleWidth);
            return go.transform;
        }
    }

    /// <summary>D-PL: a word inside a bar block is drawn only while it fits the block as laid - a narrow block shows its colour alone, the names and the
    /// slip carry the word (28a: "a figure that does not fit goes to the key").</summary>
    public sealed class WordFit : MonoBehaviour
    {
        /// <summary>The word the block carries where it fits; the text is empty where it does not, so nothing is drawn and nothing escapes.</summary>
        public string Word;
        private Text _text;
        private float _width = -1f;

        private void LateUpdate()
        {
            if (_text == null) { _text = GetComponent<Text>(); }
            if (_text == null || Word == null) { return; }
            if (_width < 0f) { _text.text = Word; _width = _text.preferredWidth; }
            // real904c (US e7e/e7f): a block narrower than its word's inset leaves the text a rect of negative width, which can never hold it - the word's
            // object is set aside, so no empty text stands in a rect it escapes (the board is rebuilt for every state it shows)
            if (_text.rectTransform.rect.width <= 0f) { gameObject.SetActive(false); return; }
            string shown = _width <= _text.rectTransform.rect.width + 0.5f ? Word : string.Empty;
            if (_text.text != shown) { _text.text = shown; }
        }
    }
}
