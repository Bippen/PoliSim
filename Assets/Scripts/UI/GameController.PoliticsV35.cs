using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §743 (UI v3.5, Design's V35 composition): THE POLITICS PAGE'S FRAME AND ITS PARLIAMENT TAB. The title *Politics* with its four tabs as words
    /// and the † (the bank's tab named as the country's bank is); the Parliament tab as the composition lays it - the chamber as one bar with the
    /// blocs over it and the majority tick through it, then the parties as tiles - and, under them, what the composition does not draw, kept as
    /// built (asked): the bills before the chamber with their counts, the blocs, formation and confidence rows, and the division records. Compass,
    /// Cabinet and the bank draw as built under the new frame until their own items. §744: the Compass tab - the parties on two axes beside their
    /// positions, the six countries' compass kept under them.
    /// </summary>
    public partial class GameController
    {
        private static readonly PoliticsCategory[] PoliticsTabCategories =
            { PoliticsCategory.Parliament, PoliticsCategory.Compass, PoliticsCategory.Cabinet, PoliticsCategory.FederalReserve };

        /// <summary>§743: the Politics page's slips, built each frame as the page draws.</summary>
        private PeopleSlips.Book _politicsSlipBook = new PeopleSlips.Book();

        /// <summary>
        /// Master Sequence step 5e, Phase A: Politics tab - Parliament, the Political Compass half of the old "Compass &amp; Demographics" tab, Cabinet's
        /// management half, and the central bank (Elias's own confirmed placement - a real political institution with its own lever, even though the
        /// Fed/Eurozone exemption means it's never Parliament-gated). Per-category gating matches the old dispatch exactly - Parliament/Compass were never
        /// gated, Cabinet/the bank were. §743: under the v3.5 title.
        /// </summary>
        private void DrawPoliticsTab(float availableHeight, float availableWidth)
        {
            // P2-1.1 (2026-09-02): the sheet is sized to the FRAME, not to its content.
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            _politicsSlipBook = new PeopleSlips.Book();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            string[] words = { "Parliament", "Compass", "Cabinet", GetCentralBankName(PlayerCountryId) };
            int clicked = DrawV35TitleTabs(titleRow, "Politics", words, System.Array.IndexOf(PoliticsTabCategories, _politicsCategory), UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            if (clicked >= 0) { _politicsCategory = PoliticsTabCategories[clicked]; }
            GUILayout.Space(V35.Px(4f));
            float bodyHeight = Mathf.Max(0f, availableHeight - titleHeight - V35.Px(4f));
            float contentWidth = StatsContentWidth(availableWidth);
            // §685 (21b): the political blocks lay their rows to the VISIBLE width - a right-aligned act laid to the content's edge was off-screen
            _parliamentVisibleWidth = contentWidth;

            if (_politicsCategory == PoliticsCategory.Parliament || _politicsCategory == PoliticsCategory.Compass)
            {
                // the tabs retrofitted to v3.5, each in its own scroll (the film resets them by name)
                bool parliament = _politicsCategory == PoliticsCategory.Parliament;
                int scrolledFrom = _slipAnchors.Count;
                float viewport = Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f);
                Vector2 scroll = GUILayout.BeginScrollView(parliament ? _parliamentScrollPosition : _politicsContentScrollPosition, GUILayout.Height(viewport));
                if (parliament) { _parliamentScrollPosition = scroll; DrawParliamentV35(contentWidth); } else { _politicsContentScrollPosition = scroll; DrawCompassV35(contentWidth); }
                GUILayout.EndScrollView();
                MoveScrolledAnchors(scrolledFrom, GUILayoutUtility.GetLastRect(), scroll);
                GUILayout.EndVertical();
                if (!DeskProvenance.On) { DrawSlips(_politicsSlipBook, GUILayoutUtility.GetLastRect()); }
                return;
            }

            // The tabs not yet retrofitted - drawn as built under the v3.5 title: the screen's caption, then the content.
            DrawScreenCaption(PoliticsScreenCaption());
            float contentHeight = bodyHeight - ScreenCaptionBlockHeight();
            switch (_politicsCategory)
            {
                case PoliticsCategory.Cabinet:
                    float cabinetScrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    GUI.enabled = !_isGameOver;
                    _cabinetScrollPosition = GUILayout.BeginScrollView(_cabinetScrollPosition, GUILayout.Height(cabinetScrollHeight));
                    DrawCabinetManagementContent();
                    GUILayout.EndScrollView();
                    GUI.enabled = true;
                    break;
                case PoliticsCategory.FederalReserve:
                    GUI.enabled = !_isGameOver;
                    DrawFederalReserveTab(contentHeight);
                    GUI.enabled = true;
                    break;
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// §743 (the composition's Politics › Parliament): <b>the chamber</b> - its seats as the figure, the chamber's name, and the seats as one bar in
        /// the arc's order with the blocs over it (where sourced) and the majority tick through it (`HemicycleRenderer.DrawSeatBar`, the one surface that
        /// may draw party ink) - then <b>the parties</b> as tiles, by seats: each party's mark, its seats, its share of the chamber and its name. Under
        /// them, kept as built (not in the composition; asked): the bills before the chamber (`DrawPendingLegislation`), the blocs, formation and
        /// confidence rows (`DrawParliamentPoliticalBlocks`) and the division records (`DrawRecentDivisions`). The arc (`HemicycleRenderer.Draw`) leaves
        /// this page for the bar; election night and the signing plate keep it.
        /// </summary>
        private void DrawParliamentV35(float width)
        {
            Country c = _playerCountry;
            IReadOnlyDictionary<string, int> seats = c.ParliamentSeats;
            int total = 0;
            foreach (KeyValuePair<string, int> kv in seats) { total += Mathf.Max(0, kv.Value); }
            string chamber = StartBrief.ChamberFullOf(PlayerCountryId);
            float gutter = V35.Px(V35.Gutter);
            V35.FloorGuarded = true;

            // ---- the chamber ----
            bool blocs = HemicycleRenderer.BlocsKnown(PlayerCountryId);
            GUIStyle captionFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
            GUIStyle abbrevFace = V35Mono(V35.Floor, V35.OnDataDark, bold: true);
            float headH = V35.Px(V35.CardIcon), barBand = V35.Px(blocs ? 74f : 52f);
            float cardH = V35.Px(V35.CardPadY) * 2f + headH + V35.Px(8f) + barBand;
            Rect card = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            Rect inner = DrawV35Card(card);
            Rect head = new Rect(inner.x, inner.y, inner.width, headH);
            if (Event.current.type == EventType.Repaint)
            {
                float side = V35.Px(V35.CardIcon);
                DrawV35Icon(new Rect(head.x, head.y, side, side), "chamber", UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
                GUIStyle figure = V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true);
                string seatsText = total.ToString(System.Globalization.CultureInfo.InvariantCulture);
                float fw = Mathf.Ceil(figure.CalcSize(new GUIContent(seatsText)).x) + 2f;
                GUI.Label(new Rect(head.x + side + V35.Px(12f), head.y, fw, headH), seatsText, figure);
                PoliSimWidgets.MeasuredLabel(new Rect(head.x + side + V35.Px(12f) + fw + V35.Px(8f), head.y, head.width * 0.5f, headH), chamber + " seats", V35Serif(V35.Name, PoliSimTheme.TextPrimary));
            }
            var barRect = new Rect(inner.x, head.yMax + V35.Px(8f), inner.width, barBand);
            HemicycleRenderer.DrawSeatBar(barRect, PlayerCountryId, seats, captionFace, abbrevFace, V35.CardPaper);
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, inner.height), "parl:chamber");
            var chamberSlip = new SlipContent(UiFormat.Seats(total).ToUpperInvariant() + " · THE " + chamber.ToUpperInvariant())
                .Add("THE PARTIES IN THE CHAMBER'S OWN ORDER - " + (blocs ? "THE LEFT BLOC, THE UNAFFILIATED, THE RIGHT BLOC, BY SEATS WITHIN EACH" : "BY SEATS; THE MODEL RECORDS NO BLOCS FOR THIS CHAMBER"))
                .Add("THE TICK: THE SEAT THAT CARRIES THE CHAMBER - " + HemicycleRenderer.LastMajoritySeat + " OF " + total);
            if (!string.IsNullOrEmpty(HemicycleRenderer.LastMajorityReading)) { chamberSlip.Add(HemicycleRenderer.LastMajorityReading.ToUpperInvariant()); }
            if (total != PartySystems.ChamberSeats(PlayerCountryId)) { chamberSlip.Add("THE CHAMBER DECLARES " + PartySystems.ChamberSeats(PlayerCountryId) + " SEATS"); }
            _politicsSlipBook.Anchors["parl:chamber"] = chamberSlip;
            GUILayout.Space(gutter);

            // ---- the parties ----
            DrawPoliticsSectionHead("Parties", "parl:parties", width);
            var parties = new List<PoliticalParty>();
            foreach (PoliticalParty p in PartySystems.For(PlayerCountryId)) { if (seats.TryGetValue(p.Abbrev, out int n) && n > 0) { parties.Add(p); } }
            parties.Sort((a, b) => seats[b.Abbrev].CompareTo(seats[a.Abbrev]));
            int unseated = PartySystems.For(PlayerCountryId).Count - parties.Count;
            _politicsSlipBook.Anchors["parl:parties"] = new SlipContent("PARTIES")
                .Add(parties.Count + " SEATED, BY SEATS · EACH ITS SEATS AND ITS SHARE OF THE CHAMBER")
                .Add(unseated > 0 ? unseated + " MORE STAND WITH NO SEAT" : "EVERY PARTY THE MODEL HOLDS HAS A SEAT");
            float tileW = V35Span(width, 3), tileH = PartyTileHeight();
            for (int i = 0; i < parties.Count; i += 4)
            {
                Rect row = GUILayoutUtility.GetRect(width, tileH, GUILayout.Width(width), GUILayout.Height(tileH));
                for (int j = i; j < Mathf.Min(i + 4, parties.Count); j++)
                {
                    var r = new Rect(row.x + (j - i) * (tileW + gutter), row.y, tileW, tileH);
                    DrawPartyTileV35(r, parties[j], seats[parties[j].Abbrev], total);
                }
                GUILayout.Space(gutter);
            }
            V35.FloorGuarded = false;

            // ---- kept as built (not in the composition; asked) ----
            GUILayout.Space(V35.Px(6f));
            DrawPendingLegislation();
            GUILayout.Space(10f);
            DrawParliamentPoliticalBlocks();   // PS-3h (§635), the caretaker line, §646's round, PS-3i (§636) - board 21b's rows (§685)
            GUILayout.Space(10f);
            DrawRecentDivisions();
        }


        /// <summary>
        /// §744 (the composition's Politics › Compass): <b>the parties on two axes</b> - the chamber's seated parties as chips (their mark and short
        /// name, edged in their ink: `HemicycleRenderer.DrawPartyChip`) at their published CHES 2024 pair (`CompassPositions.Party`; economic left to
        /// right across, 0-10) - beside <b>their positions</b> as a list. Under them, kept as built (asked): the six countries' compass - each chamber's
        /// seat-weighted mean, the cabinet's ring, the electorate's diamond and the trails (`DrawPoliticalCompassContent`).
        ///
        /// <para><b>The vertical axis keeps the game's direction</b> - liberal (GAL) at the top, conservative (TAN) at the foot, as P2-3.2 drew it and as
        /// the six countries' compass under it and the Desk's compass card draw it; the composition sets conservative at the top (its social axis is
        /// marked illustrative). Two compasses on one page cannot disagree about which way is up: asked, not flipped.</para>
        /// </summary>
        private void DrawCompassV35(float width)
        {
            Country c = _playerCountry;
            V35.FloorGuarded = true;
            var parties = new List<PoliticalParty>();
            foreach (PoliticalParty p in PartySystems.For(PlayerCountryId)) { if (c.ParliamentSeats.TryGetValue(p.Abbrev, out int n) && n > 0) { parties.Add(p); } }
            parties.Sort((a, b) => c.ParliamentSeats[b.Abbrev].CompareTo(c.ParliamentSeats[a.Abbrev]));
            float gutter = V35.Px(V35.Gutter);
            float leftW = V35Span(width, 8), rightW = width - leftW - gutter;
            float headH = V35.Px(V35.CardIcon), labelH = V35.Px(20f);
            float plotH = Mathf.Clamp((leftW - V35.Px(V35.CardPadX) * 2f) * 0.52f, V35.Px(220f), V35.Px(360f));
            float rowH = V35.Px(V35.ListRow + 4f);
            float cardH = Mathf.Max(V35.Px(V35.CardPadY) * 2f + headH + V35.Px(8f) + plotH + labelH, V35.Px(V35.CardPadY) * 2f + headH + V35.Px(8f) + parties.Count * rowH);
            Rect band = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Political);
            bool repaint = Event.current.type == EventType.Repaint;

            // ---- the plot ----
            Rect plotCard = new Rect(band.x, band.y, leftW, cardH);
            Rect inner = DrawV35Card(plotCard);
            Rect head = DrawV35CardHead(inner, "compass", "Parties on two axes", area);
            SlipAnchor(head, "compass:plot");
            _politicsSlipBook.Anchors["compass:plot"] = new SlipContent("PARTIES ON TWO AXES")
                .Add("EACH SEATED PARTY AT ITS PUBLISHED PAIR - CHES 2024: ECONOMIC LEFT (0) TO RIGHT (10) ACROSS, LIBERAL (0) TO CONSERVATIVE (10) DOWN")
                .Add("A PARTY THAT PUBLISHES NO PAIR IS NOT PLACED - THE LIST SAYS SO")
                .Add("THE SIX COUNTRIES' CHAMBERS, THE CABINET AND THE ELECTORATE ARE ON THE COMPASS BELOW");
            var plot = new Rect(inner.x, head.yMax + V35.Px(8f), inner.width, plotH);
            if (repaint)
            {
                for (int i = 1; i < 4; i++)
                {
                    PoliSimTheme.Rule(new Rect(Mathf.Round(plot.x + plot.width * i / 4f), plot.y, 1f, plot.height), V35.ListRule);
                    PoliSimTheme.Rule(new Rect(plot.x, Mathf.Round(plot.y + plot.height * i / 4f), plot.width, 1f), V35.ListRule);
                }
                PoliSimTheme.Rule(new Rect(plot.x, plot.y, plot.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(plot.x, plot.yMax - 1f, plot.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(plot.x, plot.y, 1f, plot.height), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(plot.xMax - 1f, plot.y, 1f, plot.height), V35.CardEdge);
                GUIStyle corner = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.UpperRight);
                GUIStyle cornerLow = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.LowerRight);
                GUI.Label(new Rect(plot.x, plot.y + V35.Px(4f), plot.width - V35.Px(6f), labelH), "Liberal", corner);
                GUI.Label(new Rect(plot.x, plot.yMax - labelH - V35.Px(4f), plot.width - V35.Px(6f), labelH), "Conservative", cornerLow);
                GUIStyle axis = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                GUIStyle axisRight = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
                GUI.Label(new Rect(plot.x, plot.yMax + V35.Px(2f), plot.width * 0.5f, labelH), "Economic left", axis);
                GUI.Label(new Rect(plot.x + plot.width * 0.5f, plot.yMax + V35.Px(2f), plot.width * 0.5f, labelH), "Economic right", axisRight);
            }
            GUIStyle chipFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            float chipH = V35.Px(22f);
            var placed = new List<Rect>();
            foreach (PoliticalParty party in parties)
            {
                CompassPositions.Point? point = CompassPositions.Party(party);
                if (!point.HasValue) { continue; }
                float cw = HemicycleRenderer.PartyChipWidth(party, chipFace);
                float cx = plot.x + plot.width * Mathf.InverseLerp(CompassPositions.ScaleMin, CompassPositions.ScaleMax, point.Value.LrEcon);
                float cy = plot.y + plot.height * Mathf.InverseLerp(CompassPositions.ScaleMin, CompassPositions.ScaleMax, point.Value.Galtan);
                var chip = new Rect(Mathf.Clamp(cx - cw * 0.5f, plot.x + 2f, plot.xMax - cw - 2f), Mathf.Clamp(cy - chipH * 0.5f, plot.y + 2f, plot.yMax - chipH - 2f), cw, chipH);
                // two parties at nearly one pair would print one chip over the other: the later (smaller) one steps down, or up at the plot's foot
                for (int tries = 0; tries < 8 && placed.Exists(r => r.Overlaps(chip)); tries++)
                {
                    float down = chip.y + chipH + 2f;
                    chip.y = down + chipH <= plot.yMax - 2f ? down : chip.y - chipH * (tries + 2);
                    chip.y = Mathf.Clamp(chip.y, plot.y + 2f, plot.yMax - chipH - 2f);
                }
                placed.Add(chip);
                HemicycleRenderer.DrawPartyChip(chip, PlayerCountryId, party, chipFace, V35.CardPaper);
                SlipAnchor(chip, "compass:party:" + party.Abbrev);
            }

            // ---- the positions ----
            Rect listCard = new Rect(band.x + leftW + gutter, band.y, rightW, cardH);
            Rect listInner = DrawV35Card(listCard);
            Rect listHead = DrawV35CardHead(listInner, "compass", "Positions", area);
            SlipAnchor(listHead, "compass:positions");
            _politicsSlipBook.Anchors["compass:positions"] = new SlipContent("POSITIONS")
                .Add("EACH SEATED PARTY'S PUBLISHED PAIR, ECONOMIC · SOCIAL, ON CHES 2024'S 0-10 SCALES - BY SEATS");
            GUIStyle abbrevFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle pairFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            GUIStyle absentFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            float y = listHead.yMax + V35.Px(8f);
            foreach (PoliticalParty party in parties)
            {
                var row = new Rect(listInner.x, y, listInner.width, rowH);
                CompassPositions.Point? point = CompassPositions.Party(party);
                string pair = point.HasValue ? UiFormat.Number(point.Value.LrEcon, 1) + " · " + UiFormat.Number(point.Value.Galtan, 1) : "no pair";
                if (repaint)
                {
                    PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), V35.ListRule);
                    float abbrevW = V35.Px(34f), pairW = Mathf.Ceil(pairFace.CalcSize(new GUIContent("10.0 · 10.0")).x) + 4f;
                    GUI.Label(new Rect(row.x, row.y, abbrevW, row.height), party.ShortName, abbrevFace);
                    float nameW = Mathf.Max(1f, row.width - abbrevW - pairW - V35.Px(6f));
                    string shown = V35Fit(party.Name, nameFace, nameW, out bool cut);
                    if (cut && party.Name.Contains("-")) { shown = V35Fit(party.Name.Substring(party.Name.LastIndexOf('-') + 1), nameFace, nameW, out _); }
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + abbrevW, row.y, nameW, row.height), shown, nameFace);
                    GUI.Label(new Rect(row.xMax - pairW, row.y, pairW, row.height), pair, point.HasValue ? pairFace : absentFace);
                }
                SlipAnchor(row, "compass:party:" + party.Abbrev);
                _politicsSlipBook.Anchors["compass:party:" + party.Abbrev] = new SlipContent(party.ShortName.ToUpperInvariant() + " · " + pair.ToUpperInvariant())
                    .Add(party.Name.ToUpperInvariant())
                    .Add(point.HasValue
                        ? "ECONOMIC " + UiFormat.Number(point.Value.LrEcon, 1) + " OF 10 (LEFT 0 … RIGHT 10) · SOCIAL " + UiFormat.Number(point.Value.Galtan, 1) + " OF 10 (LIBERAL 0 … CONSERVATIVE 10)"
                        : "IT PUBLISHES NO PAIR IN CHES 2024 - IT IS NOT PLACED, AND NO POSITION IS AUTHORED FOR IT")
                    .Add(UiFormat.Seats(c.ParliamentSeats[party.Abbrev]).ToUpperInvariant() + " · CHES 2024, THE PARTY'S PUBLISHED PAIR");
                y += rowH;
            }
            V35.FloorGuarded = false;
            GUILayout.Space(gutter);

            // ---- kept as built (not in the composition; asked): the six countries' compass ----
            DrawPoliticsSectionHead("Six countries", "compass:countries", width);
            _politicsSlipBook.Anchors["compass:countries"] = new SlipContent("SIX COUNTRIES")
                .Add("EACH CHAMBER AT THE SEAT-WEIGHTED MEAN OF ITS PARTIES' PAIRS, THE SITTING CABINET RINGED, THE ELECTORATE AS A DIAMOND, EACH CHAMBER'S TRAIL")
                .Add("KEPT AS BUILT UNDER THE NEW PAGE - THE COMPOSITION DRAWS THE HOME CHAMBER'S PARTIES ALONE");
            DrawPoliticalCompassContent(width);
        }

        private float PartyTileHeight()
        {
            float figure = Mathf.Ceil(V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y);
            float name = Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y);
            return V35.Px(V35.CardPadY) * 2f + Mathf.Max(V35.Px(V35.CardIcon), figure + 2f + name);
        }

        /// <summary>§743: a party as a tile - its mark (the chamber's own call: `HemicycleRenderer.DrawMark`), its seats, its share of the chamber, and
        /// <i>ABBREV · Name</i>; the full name and the bloc on its slip.</summary>
        private void DrawPartyTileV35(Rect r, PoliticalParty party, int partySeats, int total)
        {
            Rect inner = DrawV35Card(r);
            float side = V35.Px(V35.CardIcon);
            float share = total > 0 ? 100f * partySeats / total : 0f;
            string seatsText = partySeats.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string shareText = UiFormat.Number(share, 1) + " %";
            // §575: a player reads the display short name, never the key; the model holds the name as published (the composition's common
            // "Socialdemokraterna" is not held) - fitted here, whole on the slip
            string name = party.ShortName + " · " + party.Name;
            if (Event.current.type == EventType.Repaint)
            {
                HemicycleRenderer.DrawMark(new Rect(inner.x, inner.y + Mathf.Round((inner.height - side) * 0.5f), side, side), PlayerCountryId, party);
                GUIStyle figure = V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true);
                GUIStyle shareFace = V35Mono(15f, PoliSimTheme.TextSecondary, bold: true);
                GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
                float figureH = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y);
                float nameH = Mathf.Ceil(nameFace.CalcSize(new GUIContent("Ag")).y);
                float x = inner.x + side + V35.Px(12f);
                float y = inner.y + Mathf.Round((inner.height - figureH - 2f - nameH) * 0.5f);
                float fw = Mathf.Ceil(figure.CalcSize(new GUIContent(seatsText)).x) + 2f;
                GUI.Label(new Rect(x, y, fw, figureH), seatsText, figure);
                float sw = Mathf.Ceil(shareFace.CalcSize(new GUIContent(shareText)).x) + 2f;
                if (x + fw + V35.Px(6f) + sw <= inner.xMax) { GUI.Label(new Rect(x + fw + V35.Px(6f), y, sw, figureH), shareText, shareFace); }
                float nameW = Mathf.Max(1f, inner.xMax - x);
                string fitted = V35Fit(name, nameFace, nameW, out bool cut);
                if (cut && party.Name.Contains("-"))
                {
                    // a hyphenated name has no word to cut at ("Arbetarepartiet-Socialdemokraterna" fitted to "S …", the first film): its last part, which is the
                    // party's own common name where the published name joins two ("Socialdemokraterna")
                    fitted = V35Fit(party.ShortName + " · " + party.Name.Substring(party.Name.LastIndexOf('-') + 1), nameFace, nameW, out _);
                }
                PoliSimWidgets.MeasuredLabel(new Rect(x, y + figureH + 2f, nameW, nameH), fitted, nameFace);
            }
            SlipAnchor(r, "parl:party:" + party.Abbrev);
            int bloc = NationalElection.BlocOf(PlayerCountryId, party.Abbrev);
            _politicsSlipBook.Anchors["parl:party:" + party.Abbrev] = new SlipContent(party.ShortName.ToUpperInvariant() + " · " + UiFormat.Seats(partySeats).ToUpperInvariant())
                .Add(party.Name.ToUpperInvariant())
                .Add(UiFormat.Number(share, 1) + " % OF THE CHAMBER'S " + total + " SEATS")
                .Add(bloc < 0 ? "IN NO BLOC THE MODEL RECORDS" : bloc == 0 ? "THE LEFT BLOC" : "THE RIGHT BLOC");
        }

        /// <summary>§743: a v3.5 section head with its slip anchor - the Laws page's, on the Politics page's book.</summary>
        private void DrawPoliticsSectionHead(string title, string anchor, float width)
        {
            float h = V35.Px(26f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            DrawV35SectionHead(row, title);
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width, face.CalcSize(new GUIContent(title.ToUpperInvariant())).x + 4f), row.height), anchor);
            GUILayout.Space(V35.Px(6f));
        }
    }
}
