using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The chamber as seats: one dot per mandate on concentric half-rings, the ring count chosen for the
    /// width it is given so that every dot is distinct. P2-3.1 (Playtest 2, 2026-09-02): five fixed rows
    /// of ten-pixel dots had merged Sweden's mandates into curved bands - the rows are now found by
    /// capacity (the arc length each ring offers at a dot-and-a-third pitch) and the dots sized from the
    /// ring gap. Parties run left to right by bloc - the left bloc, then the unaffiliated, then the right
    /// bloc, from <see cref="NationalElection.BlocOf"/>, sourced for Sweden's 2022 blocs and unknown
    /// elsewhere - and within a bloc by mandates; the legend lists them in the same order with a header per
    /// bloc where blocs are known (the earlier order was each party's published CHES lrecon, W-G1). The
    /// dot count of the last Repaint is recorded (<see cref="LastDotsDrawn"/> against
    /// <see cref="LastChamberSeats"/>) so the screenshot driver holds it against the chamber. Point
    /// placement is the Policy Web's cos/sin on a circle swept across 180 degrees.
    /// </summary>
    public class HemicycleRenderer
    {
        // ------------------------------------------------------------------------------------------
        // BOARD 10d (D16-4), BUILT 2026-09-09 (§426). Design drew the chamber at 1× on a 1149-wide
        // sheet and put the geometry in the caption: Ø 9 everywhere, pitch 13.3, party gap 1, bloc gap
        // 3, nine rows from r 110 to r 220, the bloc arc at r 238. ⚠ ONE DOT SIZE FOR ALL 349 IS THE
        // POINT: size would read as importance, so a party is separated by INK, never by area. The
        // inks are the RECORD's - the published hues, seated, nudged, and separated by §423's bloc
        // fence - not board 10d's assigned palette, whose ruling is not in the record (§409).
        //
        // The unit is the label's own font size, not the sheet's width: the desk scales its type with
        // the window (D4), the board's 1× is the 17 px label at 1280, and a width-driven unit cannot be
        // computed at Layout time when the height has to be reserved. The radius is then clamped to the
        // width it is actually given, so a 640-wide gallery cell draws the same chamber smaller.
        // ------------------------------------------------------------------------------------------
        /// <summary>DERIVED - the label font size at 1280×720, where board 10d is drawn at 1×: clamp(round(720 × 0.024), 17, 30) = 17.</summary>
        private const float BoardUnitFontSize = 17f;
        private const int BoardRows = 9;
        private const float BoardInnerRadius = 110f;
        private const float BoardOuterRadius = 220f;
        /// <summary>Ø 9 at 1×, every seat, every ring. Board 10d's whole argument.</summary>
        private const float BoardDotDiameter = 9f;
        /// <summary>Centre-to-centre along a ring at 1×. The rows are apportioned by radius, so this is what the layout aims at rather than a fixed step; the run prints what it achieved.</summary>
        private const float BoardSeatPitch = 13.3f;
        private const float BoardPartyGap = 1f;
        private const float BoardBlocGap = 3f;
        private const float BoardBlocArcRadius = 238f;
        private const float BoardTickInner = 100f;
        private const float BoardTickOuter = 232f;
        private const float BoardTickWeight = 2.5f;

        /// <summary>Dots drawn on the last Repaint (the harness's tally).</summary>
        public static int LastDotsDrawn { get; private set; }
        /// <summary>The seats the drawn dictionary summed to on the last Repaint.</summary>
        public static int LastChamberSeats { get; private set; }
        /// <summary>The chamber the party system declares for that country, on the last Repaint.</summary>
        public static int LastDeclaredSeats { get; private set; }
        /// <summary>Rings used on the last Repaint.</summary>
        public static int LastRows { get; private set; }

        /// <summary>The tightest centre-to-centre spacing any ring achieved on the last Repaint, in the
        /// board's own unit. ⚠ MEASURED, not asserted: board 10d's caption says pitch 13.3 and nine rings
        /// from 110 to 220 cannot hold 349 seats at that step, so the layout apportions by radius and this
        /// says what it actually got.</summary>
        public static float LastPitch { get; private set; }

        /// <summary>The seat that carries the chamber on the last Repaint - what the majority tick marks.</summary>
        public static int LastMajoritySeat { get; private set; }

        /// <summary>Where the majority line fell, in words, computed from the drawn order rather than
        /// written down: board 10d's own caption, re-derived every frame because the seats drift.</summary>
        public static string LastMajorityReading { get; private set; } = string.Empty;

        private Texture2D _dotTexture;

        /// <summary>Left bloc first, then the unaffiliated, then the right bloc.</summary>
        private static int BlocRank(int bloc) => bloc == 0 ? 0 : bloc < 0 ? 1 : 2;

        /// <summary>P5-6 (board 6b row 5): the seat map's own order, for every list that walks the arc with it - left bloc first, then the unaffiliated, then the right bloc, by mandates within a bloc.</summary>
        public static List<PoliticalParty> SeatOrder(CountryId country, IReadOnlyDictionary<string, int> seats) => ByBlocThenMandates(country, seats);

        /// <summary>P5-6 (board 6b row 5): a party's mark in its own laddered ink, drawn HERE because the chamber is the one surface that may draw party ink (PartyInkDrawSiteCheck) - the breakdown row on any surface asks this for its mark, so the arc, the legend and the row come from the same call.</summary>
        public static void DrawMark(Rect rect, CountryId country, PoliticalParty party)
        {
            Texture2D emblem = IconLibrary.GetPartyMark(party.MarkName);
            Color previous = GUI.color;
            GUI.color = PoliSimTheme.PartyLaddered(country, party.Abbrev);
            if (emblem != null) { GUI.DrawTexture(rect, emblem, ScaleMode.ScaleToFit); } else { GUI.DrawTexture(rect, Texture2D.whiteTexture); }
            GUI.color = previous;
        }

        private static List<PoliticalParty> ByBlocThenMandates(CountryId country, IReadOnlyDictionary<string, int> seats)
        {
            var ordered = new List<PoliticalParty>(PartySystems.For(country));
            ordered.Sort((a, b) =>
            {
                int rankA = BlocRank(NationalElection.BlocOf(country, a.Abbrev));
                int rankB = BlocRank(NationalElection.BlocOf(country, b.Abbrev));
                if (rankA != rankB) { return rankA.CompareTo(rankB); }
                int seatsA = seats.TryGetValue(a.Abbrev, out int sa) ? sa : 0;
                int seatsB = seats.TryGetValue(b.Abbrev, out int sb) ? sb : 0;
                if (seatsA != seatsB) { return seatsB.CompareTo(seatsA); }
                return string.CompareOrdinal(a.Abbrev, b.Abbrev);
            });
            return ordered;
        }

        /// <summary>The height the arc reserves for a label style - half a disc at the capped radius plus a margin.</summary>
        /// <summary>The bloc arc's radius plus a line for its labels - the tallest thing the chamber draws.</summary>
        private static float ArcHeight(GUIStyle labelStyle) => Mathf.Round(BoardUnit(labelStyle) * (BoardBlocArcRadius + 14f)) + 8f;

        /// <summary>Board 10d's unit: 1 at the 17 px label the 1280 sheet carries, and it grows with the type.</summary>
        private static float BoardUnit(GUIStyle labelStyle) => labelStyle.fontSize / BoardUnitFontSize;

        public void Draw(string title, CountryId country, IReadOnlyDictionary<string, int> seats, GUIStyle labelStyle)
        {
            EnsureTexture();
            if (!string.IsNullOrEmpty(title)) { GUILayout.Label(title, labelStyle); }

            int totalSeats = 0;
            foreach (KeyValuePair<string, int> kvp in seats) { totalSeats += kvp.Value; }
            if (totalSeats <= 0)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            List<PoliticalParty> order = ByBlocThenMandates(country, seats);
            var seatColors = new List<Color>(totalSeats);
            // Board 10d needs more than a colour per seat: the gaps are cut at party and bloc
            // boundaries, the arc spans a bloc, and the majority tick has to say which bloc it lands in.
            var seatParty = new List<string>(totalSeats);
            var seatBloc = new List<int>(totalSeats);
            bool blocsKnown = false;
            var seatsByRank = new int[3];
            foreach (PoliticalParty party in order)
            {
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                int bloc = NationalElection.BlocOf(country, party.Abbrev);
                blocsKnown |= bloc >= 0;
                seatsByRank[BlocRank(bloc)] += count;
                Color color = PoliSimTheme.PartyLaddered(country, party.Abbrev);
                for (int j = 0; j < count; j++) { seatColors.Add(color); seatParty.Add(party.Abbrev); seatBloc.Add(BlocRank(bloc)); }
            }

            // BOARD 10d's own reading, computed here rather than in the Repaint so the caption exists on
            // the Layout pass too: which bloc the majority seat lands in, and how far inside its edge.
            // ⚠ Re-derived every frame - the seats drift with approval, and a sentence about where the
            // line falls is exactly the kind that goes wrong by being written down once.
            LastMajorityReading = blocsKnown ? MajorityReading(seatBloc, totalSeats) : string.Empty;

            Rect area = GUILayoutUtility.GetRect(10f, ArcHeight(labelStyle), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                DrawArc(area, seatColors, seatParty, seatBloc, blocsKnown, labelStyle);
                LastDotsDrawn = seatColors.Count;
                LastChamberSeats = totalSeats;
                LastDeclaredSeats = PartySystems.ChamberSeats(country);
            }

            if (!string.IsNullOrEmpty(LastMajorityReading))
            {
                GUIStyle reading = CaptionStyle(labelStyle);
                GUILayout.Label(LastMajorityReading.ToUpperInvariant(), reading);
            }

            GUILayout.Space(4f);
            DrawLegend(country, seats, order, totalSeats, blocsKnown, seatsByRank, labelStyle);
        }

        /// <summary>
        /// <summary>
        /// BOARD 10d, THE CHAMBER: nine rings from r 110 to r 220 at the board's unit, one Ø 9 dot per
        /// mandate on every one of them, a gap cut at each party boundary and a wider one at each bloc's,
        /// the bloc arc outside at r 238, and the majority tick where the 175th seat falls.
        ///
        /// <para><b>Sectors, not rings</b> (P3-C5, unchanged): every position is laid out first, ordered
        /// by angle - left to right across the chamber, inner ring before outer at one angle - and the
        /// parties' inks are dealt onto that order, so a party owns a contiguous wedge across all nine
        /// rings. What board 10d adds is the GAP: once each ring knows which party each of its seats
        /// belongs to, the ring is laid again from scratch with 1 unit of air at a party boundary and 3 at
        /// a bloc's, taken out of the 180° the ring has to spend.</para>
        ///
        /// <para>⚠ <b>The rows are apportioned by radius, not filled at a fixed pitch.</b> Nine rings from
        /// 110 to 220 hold 345 seats at a literal 13.3 pitch and Sweden returns 349, so a fixed step would
        /// drop four mandates or need a tenth ring the board does not draw. Seats per ring are therefore
        /// proportional to the ring's radius (largest remainder, the outermost taking the balance) and the
        /// pitch lands near the board's figure rather than on it. <see cref="LastPitch"/> carries what it
        /// actually achieved, so the number is measured on every run rather than asserted here.</para>
        /// </summary>
        private void DrawArc(Rect area, IReadOnlyList<Color> seatColors, IReadOnlyList<string> seatParty,
            IReadOnlyList<int> seatBloc, bool blocsKnown, GUIStyle labelStyle)
        {
            int total = seatColors.Count;
            if (total <= 0) { return; }

            // The board's unit, clamped to the width it is actually given: the gallery draws this same
            // chamber in a 640-wide cell and the Parliament page in the whole sheet.
            float unit = BoardUnit(labelStyle);
            float widthCap = (area.width * 0.5f - 4f) / BoardOuterRadius;
            float heightCap = (area.height - 4f) / BoardBlocArcRadius;
            unit = Mathf.Min(unit, Mathf.Min(widthCap, heightCap));
            if (unit <= 0.02f) { return; }

            float outer = BoardOuterRadius * unit;
            float inner = BoardInnerRadius * unit;
            float dot = BoardDotDiameter * unit;
            float ringGap = (outer - inner) / (BoardRows - 1);
            LastRows = BoardRows;

            // ---- seats per ring, proportional to the ring's radius -----------------------------------
            float radiusSum = 0f;
            for (int r = 0; r < BoardRows; r++) { radiusSum += inner + r * ringGap; }
            var perRow = new int[BoardRows];
            int assigned = 0;
            for (int r = 0; r < BoardRows; r++)
            {
                perRow[r] = Mathf.RoundToInt(total * ((inner + r * ringGap) / radiusSum));
                assigned += perRow[r];
            }

            perRow[BoardRows - 1] += total - assigned;
            if (perRow[BoardRows - 1] < 0) { perRow[BoardRows - 1] = 0; }

            // ---- the sector deal: positions by angle, parties dealt onto them ------------------------
            var slots = new List<(float Angle, int Ring)>(total);
            int laid = 0;
            for (int r = 0; r < BoardRows && laid < total; r++)
            {
                int rowSeats = Mathf.Min(perRow[r], total - laid);
                for (int i = 0; i < rowSeats; i++)
                {
                    float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                    slots.Add((angle, r));
                    laid++;
                }
            }

            slots.Sort((p, q) => p.Angle != q.Angle ? q.Angle.CompareTo(p.Angle) : p.Ring.CompareTo(q.Ring));

            // ---- each ring re-laid, with the gaps cut out of its own 180° ---------------------------
            var byRing = new List<int>[BoardRows];
            for (int r = 0; r < BoardRows; r++) { byRing[r] = new List<int>(); }
            for (int seat = 0; seat < slots.Count && seat < total; seat++) { byRing[slots[seat].Ring].Add(seat); }

            var baseline = new Vector2(Mathf.Round(area.x + area.width * 0.5f), area.yMax - 4f);
            var placed = new Vector2[total];
            var angleOf = new float[total];
            float minPitch = float.MaxValue;

            for (int r = 0; r < BoardRows; r++)
            {
                List<int> ring = byRing[r];
                if (ring.Count == 0) { continue; }

                float radius = inner + r * ringGap;
                float gapUnits = 0f;
                for (int i = 1; i < ring.Count; i++)
                {
                    if (seatBloc[ring[i]] != seatBloc[ring[i - 1]]) { gapUnits += BoardBlocGap; }
                    else if (!string.Equals(seatParty[ring[i]], seatParty[ring[i - 1]], System.StringComparison.Ordinal)) { gapUnits += BoardPartyGap; }
                }

                // Air is spent in degrees at this ring's own radius, so a 1-unit gap is 1 unit of paper
                // on every ring rather than 1 degree everywhere.
                float gapDegrees = Mathf.Min(60f, gapUnits * unit / radius * Mathf.Rad2Deg);
                float span = 180f - gapDegrees;
                float pitch = ring.Count > 1 ? span / (ring.Count - 1) : 0f;
                if (ring.Count > 1) { minPitch = Mathf.Min(minPitch, pitch * Mathf.Deg2Rad * radius); }

                float cursor = 180f;
                for (int i = 0; i < ring.Count; i++)
                {
                    if (i > 0)
                    {
                        cursor -= pitch;
                        if (seatBloc[ring[i]] != seatBloc[ring[i - 1]]) { cursor -= BoardBlocGap * unit / radius * Mathf.Rad2Deg; }
                        else if (!string.Equals(seatParty[ring[i]], seatParty[ring[i - 1]], System.StringComparison.Ordinal)) { cursor -= BoardPartyGap * unit / radius * Mathf.Rad2Deg; }
                    }

                    int seat = ring[i];
                    angleOf[seat] = cursor;
                    placed[seat] = PointOnArc(baseline, radius, cursor);
                }
            }

            LastPitch = minPitch == float.MaxValue ? 0f : minPitch / unit;

            // ---- the bloc arc, outside the seats ----------------------------------------------------
            Color previousColor = GUI.color;
            var blocFrom = new float[3];
            var blocTo = new float[3];
            var blocSeats = new int[3];
            for (int i = 0; i < 3; i++) { blocFrom[i] = float.MinValue; blocTo[i] = float.MaxValue; }
            for (int seat = 0; seat < total; seat++)
            {
                int rank = seatBloc[seat];
                if (rank < 0 || rank > 2) { continue; }
                blocSeats[rank]++;
                blocFrom[rank] = Mathf.Max(blocFrom[rank], angleOf[seat]);
                blocTo[rank] = Mathf.Min(blocTo[rank], angleOf[seat]);
            }

            GUIStyle caption = CaptionStyle(labelStyle);
            for (int rank = 0; rank < 3; rank++)
            {
                if (blocSeats[rank] == 0) { continue; }

                GUI.color = PoliSimTheme.HairlineStrong;
                float arcRadius = BoardBlocArcRadius * unit;
                // ⚠ Stepped by ARC LENGTH, not by a fixed angle: a fixed step samples the 2560
                // sheet's larger radius every five pixels and the hairline reads as a dotted line.
                // Half a pixel of arc between samples draws as one stroke at every size.
                float step = Mathf.Max(0.02f, 0.5f / arcRadius * Mathf.Rad2Deg);
                for (float a = blocTo[rank]; a <= blocFrom[rank]; a += step)
                {
                    Vector2 point = PointOnArc(baseline, arcRadius, a);
                    GUI.DrawTexture(new Rect(point.x, point.y, 1f, 1f), Texture2D.whiteTexture);
                }

                GUI.color = previousColor;
            }

            // ---- the dots ---------------------------------------------------------------------------
            for (int seat = 0; seat < total; seat++)
            {
                GUI.color = seatColors[seat];
                GUI.DrawTexture(new Rect(placed[seat].x - dot * 0.5f, placed[seat].y - dot * 0.5f, dot, dot), _dotTexture);
            }

            GUI.color = previousColor;

            // ---- the bloc labels, AFTER the seats ---------------------------------------------------
            // ⚠ Drawn last on purpose. The first film of this board had them under the dots: a label sits
            // at its bloc's mid-angle, which on a half-circle is a diagonal, and a horizontal text box
            // centred there puts its inner half straight onto the outermost ring.
            //
            // ⚠ AND ONLY WHERE THE BLOCS ARE REAL (§430, found by filming France). With one bloc the
            // label is "UNAFFILIATED 577" at the arc's mid-angle - which repeats the page header and
            // lands on the majority tick's own label, since a single bloc's midpoint is 90°. The arc
            // stays as the chamber's outer rule; the label is a bloc device and goes with the blocs.
            for (int rank = 0; blocsKnown && rank < 3; rank++)
            {
                if (blocSeats[rank] == 0) { continue; }

                float mid = (blocFrom[rank] + blocTo[rank]) * 0.5f;
                string text = $"{BlocRankName(rank)} {blocSeats[rank]}";
                Vector2 size = caption.CalcSize(new GUIContent(text));

                // Pushed out along its own radius by enough to clear the box's own half-width at that
                // angle, so the label sits outside the arc at every bloc width.
                float clearance = Mathf.Abs(Mathf.Cos(mid * Mathf.Deg2Rad)) * size.x * 0.5f;
                Vector2 label = PointOnArc(baseline, BoardBlocArcRadius * unit + 8f * unit + clearance, mid);
                GUI.Label(new Rect(label.x - size.x * 0.5f, label.y - size.y * 0.5f, size.x, size.y), text, caption);
            }

            // ---- the majority tick ------------------------------------------------------------------
            // The seat that carries the chamber, found in the drawn order rather than assumed: the tick
            // sits between it and the one before, which is where a majority is actually won.
            int majority = total / 2 + 1;
            LastMajoritySeat = majority;
            if (majority >= 1 && majority <= total)
            {
                int index = majority - 1;
                float tickAngle = index > 0 ? (angleOf[index] + angleOf[index - 1]) * 0.5f : angleOf[index];
                GUI.color = PoliSimTheme.TextPrimary;
                float weight = Mathf.Max(1f, BoardTickWeight * unit);
                for (float rr = BoardTickInner * unit; rr <= BoardTickOuter * unit; rr += 0.5f)
                {
                    Vector2 point = PointOnArc(baseline, rr, tickAngle);
                    GUI.DrawTexture(new Rect(point.x - weight * 0.5f, point.y - weight * 0.5f, weight, weight), Texture2D.whiteTexture);
                }

                GUI.color = previousColor;
                string tick = $"MAJORITY {majority}";
                Vector2 size = caption.CalcSize(new GUIContent(tick));
                Vector2 at = PointOnArc(baseline, (BoardTickOuter + 11f) * unit, tickAngle);
                GUI.Label(new Rect(at.x - size.x * 0.5f, at.y - size.y * 0.5f, size.x, size.y), tick, caption);

            }

            GUI.color = previousColor;
        }

        /// <summary>Which bloc carries the seat that carries the chamber, and how far inside that bloc it
        /// sits. Walks the drawn order, so it says what the arc shows rather than what a table would.
        /// ⚠ Called only where the blocs are SOURCED: with every party unaffiliated the sentence reads
        /// "289 seats inside the unaffiliated", which is arithmetic about a bloc that is the whole
        /// chamber - true, and empty.</summary>
        private static string MajorityReading(IReadOnlyList<int> seatBloc, int totalSeats)
        {
            if (totalSeats <= 0 || seatBloc.Count < totalSeats) { return string.Empty; }

            int majority = totalSeats / 2 + 1;
            var seatsByRank = new int[3];
            foreach (int rank in seatBloc) { if (rank >= 0 && rank <= 2) { seatsByRank[rank]++; } }

            int cumulative = 0;
            for (int rank = 0; rank < 3; rank++)
            {
                if (seatsByRank[rank] == 0) { continue; }
                if (majority <= cumulative + seatsByRank[rank])
                {
                    int inside = majority - cumulative;
                    return $"the majority line falls {inside} seat{(inside == 1 ? string.Empty : "s")} inside the {BlocRankName(rank).ToLowerInvariant()}";
                }

                cumulative += seatsByRank[rank];
            }

            return string.Empty;
        }

        /// <summary>The bloc's name as the arc labels it.</summary>
        private static string BlocRankName(int rank) => rank == 0 ? "LEFT BLOC" : rank == 1 ? "UNAFFILIATED" : "RIGHT BLOC";


        private static void DrawLegend(CountryId country, IReadOnlyDictionary<string, int> seats, List<PoliticalParty> order,
            int totalSeats, bool blocsKnown, int[] seatsByRank, GUIStyle labelStyle)
        {
            GUIStyle caption = CaptionStyle(labelStyle);
            Color previousColor = GUI.color;
            bool anyUnsourced = false;
            int lastRank = -1;
            for (int i = 0; i < order.Count; i++)
            {
                PoliticalParty party = order[i];
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                float percent = totalSeats > 0 ? count / (float)totalSeats * 100f : 0f;
                int bloc = NationalElection.BlocOf(country, party.Abbrev);
                int rank = BlocRank(bloc);
                if (blocsKnown && rank != lastRank)
                {
                    // A header per group: the bloc's name and its seats, from the same dictionary the dots read.
                    if (lastRank >= 0) { GUILayout.Space(4f); }
                    string header = string.Format(CultureInfo.InvariantCulture, "{0} · {1} SEATS",
                        NationalElection.BlocName(bloc).ToUpperInvariant(), seatsByRank[rank]);
                    GUILayout.Label(header, caption);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Rect drawn = GUILayoutUtility.GetLastRect();
                        UiOverflowGuard.Check(header, caption.CalcSize(new GUIContent(header)), new Vector2(drawn.width, drawn.height), caption.fontSize);
                    }
                    lastRank = rank;
                }

                GUILayout.BeginHorizontal();
                float markerSize = labelStyle.fontSize;
                float rowHeight = LedgerRow.Height(labelStyle);
                Rect markerLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                var swatchRect = new Rect(markerLane.x, markerLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize);
                bool inkIsSourced = PoliSimTheme.HasPartyInk(country, party.Abbrev);
                anyUnsourced |= !inkIsSourced;
                GUI.color = PoliSimTheme.PartyLaddered(country, party.Abbrev);
                if (inkIsSourced)
                {
                    GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                }
                else
                {
                    DrawHairlineBox(swatchRect);
                }
                GUI.color = previousColor;

                Texture2D emblem = IconLibrary.GetPartyMark(party.MarkName);
                if (emblem != null)
                {
                    Rect emblemLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(
                        new Rect(emblemLane.x, emblemLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize),
                        emblem, ScaleMode.ScaleToFit);
                }

                Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(labelStyle), GUILayout.ExpandWidth(true));
                LedgerRow.DrawReadOnly(
                    rowRect,
                    party.Name,
                    totalSeats > 0 ? count / (float)totalSeats : -1f,
                    count.ToString(CultureInfo.InvariantCulture) + " seats",
                    percent.ToString("F0", CultureInfo.InvariantCulture) + "%",
                    PoliSimTheme.PartyLaddered(country, party.Abbrev),
                    labelStyle,
                    labelStyle);
                GUILayout.EndHorizontal();
            }

            if (anyUnsourced)
            {
                GUILayout.Space(2f);
                GUILayout.Label("Outlined swatch: no published colour for this party", caption);
            }
        }

        private static GUIStyle CaptionStyle(GUIStyle labelStyle)
        {
            var caption = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.Max(9, Mathf.RoundToInt(labelStyle.fontSize * 0.72f)),
                wordWrap = false
            };
            caption.normal.textColor = PoliSimTheme.TextMuted;
            caption.hover.textColor = PoliSimTheme.TextMuted;
            caption.active.textColor = PoliSimTheme.TextMuted;
            caption.focused.textColor = PoliSimTheme.TextMuted;
            return caption;
        }

        private static void DrawHairlineBox(Rect r)
        {
            const float t = 1f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }

        private static Vector2 PointOnArc(Vector2 baseline, float radius, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return baseline + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius;
        }

        private void EnsureTexture()
        {
            if (_dotTexture != null) { return; }
            const int diameter = 32;
            _dotTexture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            var pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // A one-pixel soft edge so a downscaled dot keeps a round rim instead of a jagged one.
                    pixels[y * diameter + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                }
            }
            _dotTexture.SetPixels(pixels);
            _dotTexture.Apply(false);
        }
    }
}
