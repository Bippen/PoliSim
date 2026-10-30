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
    /// GOVERNS - the 460 as one bar in the night's order with ticks at 231 and 276, the President who designates first, the outgoing cabinet, and
    /// after 41 of 41 "2023, as it happened" where the record holds the election.</para>
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
            Masthead(content, "POLAND  ·  THE SEJM", pollsClosed.AddMinutes(s.Minute).ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  POLLS CLOSED " + pollsClosed.ToString("HH:mm", CultureInfo.InvariantCulture),
                string.Format(CultureInfo.InvariantCulture, "{0} OF {1} OKRĘGI DECLARED", s.Declared, s.Total));

            Transform body = Body(content);
            Transform overlay = screen.Root.transform;
            // 28a's columns as the board draws them, 332 · 486 · 322 of its pixels
            Transform count = Column(body, "Count", 332f);
            count.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            BuildCountPoland(count, night, overlay);
            Transform centre = Column(body, "MapColumn", 486f);
            screen.Okregi = BuildOkregiMap(centre, night, overlay);
            Transform right = Column(body, "Governs", 322f);
            right.GetComponent<VerticalLayoutGroup>().spacing = 4f;
            BuildChamberPoland(right, night, overlay);
            Text verdictLine = PolandFooter(content, verdict, screen, overlay);

            Resolve(contentRect);
            if (FitPolandVerdict(verdictLine, verdict)) { Resolve(contentRect); }
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
        private static void BuildCountPoland(Transform parent, SejmNight night, Transform overlay)
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

            // the calls, in one line, each where it landed (the existing nights' "as they landed", folded to the line 28a has room for)
            var called = new List<string>();
            var open = new List<string>();
            for (int k = 0; k < s.Keys.Length; k++)
            {
                string n = PartySystems.ShortName(CountryId.Poland, s.Keys[k]);
                if (s.Calls[k] == PolishSejmNight.Call.In) { called.Add(n + " IN AT " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture)); }
                else if (s.Calls[k] == PolishSejmNight.Call.Out) { called.Add(n + " OUT AT " + s.CalledAt[k].ToString(CultureInfo.InvariantCulture)); }
                else if (s.Calls[k] == PolishSejmNight.Call.Open) { open.Add(n); }
            }
            string calls = called.Count == 0 ? "THE CALLS · NOTHING CAN BE CALLED YET" : "THE CALLS · " + string.Join(" · ", called);
            if (open.Count > 0 && called.Count > 0) { calls += " · OPEN " + string.Join(", ", open); }
            PlWrapped(parent, calls, Px(14f), PoliSimTheme.TextPrimary);
            PlWrapped(parent, "A party's committee needs 5" + Nbsp + "% of the national vote, a coalition's (KO, TD) 8" + Nbsp + "%; the minority's is exempt" + MinorityWhere(s)
                + ". The bar is the votes counted toward the line so far; it crosses the tick when the call lands.", Px(14f), PoliSimTheme.TextMuted);
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

        /// <summary>The legend's words for where the exempt list stands: its one okreg (a committee that stood in one in 2023 keeps its votes there),
        /// else every okreg where this count gives it a vote.</summary>
        private static string MinorityWhere(PolishSejmNight.State s)
        {
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (s.Calls[k] != PolishSejmNight.Call.Exempt) { continue; }
                int stands = MinorityOkreg(s, k);
                return stands > 0 ? " and stands in okręg " + stands.ToString(CultureInfo.InvariantCulture) + " only"
                    : " and has votes in " + OkregiWithVotes(s, k).ToString(CultureInfo.InvariantCulture) + " okręgi in this count";
            }
            return string.Empty;
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
            PlWrapped(parent, "One cell, one deputy: each okręg holds its own magnitude, in the inks of the seats it has returned. Paper inside a declared okręg is a seat waiting on an open call; "
                + "dashed is not yet declared; the wide gaps are the voivodeships' borders.", Px(14f), PoliSimTheme.TextMuted);
            return view;
        }

        /// <summary>
        /// WHO GOVERNS (28a's right column, 28b's answers 5 and 6): the 460 as one bar in the night's fixed order (never re-sorted mid-count), the seats in
        /// play paper and the okregi not yet declared on the plate, the ticks at 231 (a majority) and 276 (three fifths, the veto's override); the
        /// President, who designates first; the outgoing cabinet; and at 41 of 41 the reference where the record holds the election.
        /// </summary>
        private static void BuildChamberPoland(Transform parent, SejmNight night, Transform overlay)
        {
            PolishSejmNight.State s = night.State;
            int seats = PartySystems.ChamberSeats(CountryId.Poland), majority = seats / 2 + 1;
            // the veto's override, three fifths (`PresidentialVeto.OverrideNumerator` / `OverrideDenominator`) - of a full chamber here; the vote counts those present
            int threeFifths = (PresidentialVeto.OverrideNumerator * seats + PresidentialVeto.OverrideDenominator - 1) / PresidentialVeto.OverrideDenominator;
            string seatsText = seats.ToString(CultureInfo.InvariantCulture), majorityText = majority.ToString(CultureInfo.InvariantCulture), fifthsText = threeFifths.ToString(CultureInfo.InvariantCulture);
            Transform head = CanvasRows.HRow(parent, "GovernsHead", Px(28f), 8f);
            PlText(head, "Heading", "WHO GOVERNS", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);

            var bar = new GameObject("ChamberBar");
            bar.transform.SetParent(parent, false);
            bar.AddComponent<RectTransform>();
            LayoutElement barSize = bar.AddComponent<LayoutElement>();
            barSize.minHeight = barSize.preferredHeight = Px(62f);
            double x = 0.0;
            var key = new List<string>();
            for (int k = 0; k < s.Keys.Length; k++)
            {
                if (s.Seats[k] <= 0) { continue; }
                Block(bar.transform, "Seats " + s.Keys[k], x / seats, (x + s.Seats[k]) / seats, InkOf(s.Keys[k]), null);
                key.Add(PartySystems.ShortName(CountryId.Poland, s.Keys[k]) + " " + s.Seats[k].ToString(CultureInfo.InvariantCulture));
                x += s.Seats[k];
            }
            if (s.InPlay > 0) { Block(bar.transform, "InPlay", x / seats, (x + s.InPlay) / seats, PoliSimTheme.Card, PoliSimTheme.HairlineStrong); key.Add("IN PLAY " + s.InPlay.ToString(CultureInfo.InvariantCulture)); x += s.InPlay; }
            if (s.NotYet > 0) { Block(bar.transform, "NotYet", x / seats, (x + s.NotYet) / seats, PoliSimTheme.CardInset, PoliSimTheme.Hairline); key.Add("NOT YET " + s.NotYet.ToString(CultureInfo.InvariantCulture)); }
            Tick(bar.transform, majority / (double)seats, majority.ToString(CultureInfo.InvariantCulture));
            Tick(bar.transform, threeFifths / (double)seats, threeFifths.ToString(CultureInfo.InvariantCulture));
            CanvasRows.Slip(bar, overlay, "SEJM · " + seatsText + " SEATS", new[] { majorityText + " · A MAJORITY", fifthsText + " · THREE FIFTHS OF A FULL SEJM: OVERRIDES THE PRESIDENT'S VETO (ART. 122 UST. 5 - THREE FIFTHS OF THOSE VOTING)",
                "SEATS IN THE NIGHT'S ORDER, NEVER RE-SORTED", string.Join(" · ", key) });
            PlWrapped(parent, string.Join(" · ", key), Px(14f), PoliSimTheme.TextPrimary);   // 28a's dense line (the order, the majority, three fifths) is the bar's slip: the column has no room for it at the floor beside the reference

            Transform president = CanvasRows.HRow(parent, "President", Px(32f), Px(8f));
            Edge(president, PoliSimTheme.Hairline, top: true);
            PlText(president, "Role", "President", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            if (!string.IsNullOrEmpty(night.PresidentParty) && HasMark(night.PresidentParty)) { CanvasRows.Mark(president, CountryId.Poland, night.PresidentParty, Px(16f)); }
            PlText(president, "Name", RowName(night.PresidentName, night.PresidentParty) ?? "—", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(president);
            CanvasRows.Stamp(president, "DESIGNATES", Px(14f), PoliSimTheme.TextPrimary);
            CanvasRows.Slip(president.gameObject, overlay, (night.PresidentName ?? "THE PRESIDENT").ToUpperInvariant() + " · PRESIDENT",
                new[] { "DESIGNATES THE PRIME MINISTER FIRST (ART. 154)", "THE NEW SEJM SITS WITHIN 30 DAYS (ART. 109 UST. 2)", "HOLDS THE VETO: THREE FIFTHS OF THOSE VOTING OVERRIDE IT" });

            Transform government = CanvasRows.HRow(parent, "Government", Px(32f), Px(8f));
            Edge(government, PoliSimTheme.Hairline, top: true);
            Edge(government, PoliSimTheme.Hairline, top: false);
            PlText(government, "Role", "Government", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            if (!string.IsNullOrEmpty(night.GovernmentParty) && HasMark(night.GovernmentParty)) { CanvasRows.Mark(government, CountryId.Poland, night.GovernmentParty, Px(16f)); }
            PlText(government, "Party", string.IsNullOrEmpty(night.GovernmentParty) ? "—" : PartySystems.ShortName(CountryId.Poland, night.GovernmentParty), Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(government);
            CanvasRows.Stamp(government, "OUTGOING", Px(14f), PoliSimTheme.TextPrimary);
            CanvasRows.Slip(government.gameObject, overlay, "THE OUTGOING CABINET", new[] { "RESIGNS AT THE NEW SEJM'S FIRST SITTING (ART. 162 UST. 1)", "SERVES ON UNTIL A NEW ONE IS APPOINTED" });

            if (s.Complete && night.Reference != null && night.Reference.Seats != null && night.Reference.Seats.Length == s.Keys.Length) { BuildReferencePoland(parent, night, overlay); }
        }

        /// <summary>"2023, as it happened" (28b's answer 6, 21d's table): played · real · diff per committee in the night's order - the PKW's seats, no flag
        /// on them - and the cabinet of record with LOW CONFIDENCE where the history is read by lineage (§767, D1).</summary>
        private static void BuildReferencePoland(Transform parent, SejmNight night, Transform overlay)
        {
            PolishSejmNight.State s = night.State;
            Reference reference = night.Reference;
            Transform head = CanvasRows.HRow(parent, "ReferenceHead", Px(26f), 8f);
            string label = (reference.Label ?? "AS IT HAPPENED").ToLowerInvariant();
            label = label.StartsWith("poland ", StringComparison.Ordinal) ? label.Substring("poland ".Length) : label;
            PlText(head, "Label", char.ToUpperInvariant(label[0]) + label.Substring(1), Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Slip(head.gameObject, overlay, "DATED", new[] { "THE REAL RESULT, AS THE RECORD HOLDS IT", "THE RECORD AS OF " + DeskDay(reference.RecordDate) });

            float markW = Px(16f), nameW = Px(54f), figW = Px(70f);
            nameW = Mathf.Max(nameW, Widest(s.Keys, k => PartySystems.ShortName(CountryId.Poland, k), Px(16f), PoliSimTheme.Document) + 4f);
            figW = Mathf.Max(figW, Measure("played", Px(14f), PoliSimTheme.Document) + 4f);
            Transform keys = CanvasRows.HRow(parent, "ReferenceKeys", Px(20f), Px(6f));
            CanvasRows.FixedCell(keys, string.Empty, markW + Px(6f) + nameW, Px(14f), PoliSimTheme.TextMuted);
            CanvasRows.FixedCell(keys, "played", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "real", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "diff", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            for (int k = 0; k < s.Keys.Length; k++)
            {
                int played = s.Seats[k], real = reference.Seats[k], diff = played - real;
                Transform row = CanvasRows.HRow(parent, "Reference " + s.Keys[k], Px(23f), Px(6f));
                if (HasMark(s.Keys[k])) { CanvasRows.Mark(row, CountryId.Poland, s.Keys[k], markW); } else { Blank(row, markW); }
                CanvasRows.FixedCell(row, PartySystems.ShortName(CountryId.Poland, s.Keys[k]), nameW, Px(16f), PoliSimTheme.TextPrimary);
                CanvasRows.FixedCell(row, played.ToString(CultureInfo.InvariantCulture), figW, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, real.ToString(CultureInfo.InvariantCulture), figW, Px(16f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, diff > 0 ? "+" + diff.ToString(CultureInfo.InvariantCulture) : diff < 0 ? "−" + (-diff).ToString(CultureInfo.InvariantCulture) : "0", figW, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.Slip(row.gameObject, overlay, PartySystems.ShortName(CountryId.Poland, s.Keys[k]),
                    new[] { string.Format(CultureInfo.InvariantCulture, "PLAYED {0} · THE REAL RESULT {1}", UiFormat.Seats(played).ToUpperInvariant(), real), "THE DIFFERENCE IS PLAYED LESS REAL" });
            }
            if (!string.IsNullOrEmpty(reference.HeadParty))
            {
                Transform cabinet = CanvasRows.HRow(parent, "ReferenceGovernment", Px(32f), Px(8f));
                Edge(cabinet, PoliSimTheme.Hairline, top: true);
                if (HasMark(reference.HeadParty)) { CanvasRows.Mark(cabinet, CountryId.Poland, reference.HeadParty, markW); }
                PlText(cabinet, "Head", Surname(reference.HeadSurname) ?? "—", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
                CanvasRows.Stamp(cabinet, DeskDay(reference.HeadFrom), Px(14f), PoliSimTheme.TextPrimary);
                CanvasRows.Slip(cabinet.gameObject, overlay, "THE GOVERNMENT OF RECORD", SlipLines(reference.GovernmentLine));
                if (!string.IsNullOrEmpty(reference.HistoryNote))
                {
                    // the flag on a line of its own under the cabinet - beside it the row is wider than the column at the 14 px floor
                    Transform confidence = CanvasRows.HRow(parent, "ReferenceConfidence", Px(26f), Px(8f));
                    GameObject flag = CanvasRows.Stamp(confidence, "LOW CONFIDENCE", Px(14f), PoliSimTheme.TextPrimary);
                    PlText(confidence, "Basis", "the cabinet by lineage", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
                    CanvasRows.Slip(flag, overlay, "LOW CONFIDENCE", SlipLines(reference.HistoryNote));
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
        /// K302 (the art155 review's R2-M1): THE SEJM NIGHT'S FOOT, ONE LINE - the verdict beside CONTINUE at the foot's own face (the shared foot's 12-unit bold
        /// upper case), cut at a whole word to the room the laid foot leaves it, "…" where cut, the whole sentence in its slip, as D-IT's Italian foot is
        /// (`ItalyFooter`). The shared foot wrapped it into a foot sized for about three lines, and Art. 155's sentence on a refusal runs to about four at 1280.
        /// The cut is made by <see cref="FitPolandVerdict"/> once the page is laid. Returns the verdict's text, null where there is none.
        /// </summary>
        private static Text PolandFooter(Transform parent, string verdict, ElectionNightScreen screen, Transform overlay)
        {
            var foot = new GameObject("Footer");
            foot.transform.SetParent(parent, false);
            foot.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            var row = foot.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 24f;
            foot.AddComponent<LayoutElement>().minHeight = 56f;
            Text line = null;
            if (!string.IsNullOrEmpty(verdict))
            {
                line = CanvasChrome.MakeTextRealWeight(foot.transform, "Verdict", verdict.ToUpperInvariant(), PoliSimTheme.Display, 12, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
                line.horizontalOverflow = HorizontalWrapMode.Overflow;
                line.verticalOverflow = VerticalWrapMode.Overflow;
                line.raycastTarget = false;
                line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                CanvasRows.Slip(line.gameObject, overlay, "THE VERDICT", SlipLines(verdict));
            }
            else { CanvasRows.Spacer(foot.transform); }
            BuildContinueButton(foot.transform, screen.Dismiss);
            return line;
        }

        /// <summary>K302 (R2-M1): the verdict's one line cut to the room its laid rect holds, measured in canvas units by the text's own generator (its
        /// preferred width at its own face and size) - the longest run of whole words with " …" after it that fits, or the whole sentence where it fits. True
        /// where the line was cut, so the page is laid again.</summary>
        private static bool FitPolandVerdict(Text line, string verdict)
        {
            if (line == null || string.IsNullOrEmpty(verdict)) { return false; }
            float room = line.rectTransform.rect.width;
            string whole = verdict.ToUpperInvariant();
            line.text = whole;
            if (line.preferredWidth <= room) { return false; }
            string[] words = whole.Split(' ');
            string best = "…";
            for (int i = 1; i <= words.Length; i++)
            {
                string candidate = string.Join(" ", words, 0, i) + " …";
                line.text = candidate;
                if (line.preferredWidth > room) { break; }
                best = candidate;
            }
            line.text = best;
            return true;
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
