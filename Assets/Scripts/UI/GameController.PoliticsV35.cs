using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §743 (UI v3.5, Design's V35 composition): THE POLITICS PAGE'S FRAME AND ITS PARLIAMENT TAB. The title *Politics* with its four tabs as words
    /// and the † (the bank's tab named as the country's bank is); the Parliament tab as the composition lays it - the chamber as one bar with the
    /// blocs over it and the majority tick through it, then the parties as tiles - and, under them, what the composition does not draw, kept as
    /// built (asked): the bills before the chamber with their counts, the blocs, formation and confidence rows, and the division records. Compass,
    /// Cabinet and the bank draw as built under the new frame until their own items. §744: the Compass tab - the parties on two axes beside their
    /// positions, the six countries' compass kept under them. §745: the Cabinet tab - one card per portfolio, the shortlists in their cards. §746: the bank - the rate, its path, what the rule reads, the readings it weighs
    /// and the governor, the full rate page kept under them; a bank with no governor of its own keeps its tab as built.
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

            if (_politicsCategory != PoliticsCategory.FederalReserve || _playerCountry.CurrentFedChair != null)
            {
                // the tabs retrofitted to v3.5, each in its own scroll (the film resets them by name)
                PoliticsCategory tab = _politicsCategory;
                int scrolledFrom = _slipAnchors.Count;
                float viewport = Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f);
                Vector2 previous = tab == PoliticsCategory.Parliament ? _parliamentScrollPosition : tab == PoliticsCategory.Compass ? _politicsContentScrollPosition
                    : tab == PoliticsCategory.Cabinet ? _cabinetScrollPosition : _federalReserveScrollPosition;
                Vector2 scroll = GUILayout.BeginScrollView(previous, GUILayout.Height(viewport));
                switch (tab)
                {
                    case PoliticsCategory.Parliament: _parliamentScrollPosition = scroll; DrawParliamentV35(contentWidth); break;
                    case PoliticsCategory.Compass: _politicsContentScrollPosition = scroll; DrawCompassV35(contentWidth); break;
                    case PoliticsCategory.Cabinet:
                        _cabinetScrollPosition = scroll;
                        GUI.enabled = !_isGameOver;
                        DrawCabinetV35(contentWidth);
                        GUI.enabled = true;
                        break;
                    default:
                        // §746: the bank with a governor of its own - the composition's page, then the full rate page as built (kept, asked) and the selection
                        _federalReserveScrollPosition = scroll;
                        GUI.enabled = !_isGameOver;
                        FedChair chair = _playerCountry.CurrentFedChair;
                        DrawBankV35(contentWidth, chair);
                        DrawPoliticsSectionHead("The rate page", "bank:page", contentWidth);
                        _politicsSlipBook.Anchors["bank:page"] = new SlipContent("THE RATE PAGE")
                            .Add("THE PATH'S CHART WITH ITS WINDOW, THE MOVES A YEAR AND TWO OUT, THE RULE TERM BY TERM AND ITS INPUTS AS READINGS, THE POLITICAL HALF")
                            .Add("KEPT AS BUILT UNDER THE NEW PAGE - THE COMPOSITION DRAWS THE PAGE ABOVE");
                        _riksbankCaptions.Clear();
                        _riksbankPeekWanted = false;
                        DrawRiksbankPage(chair, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
                        _riksbankRecording = false;
                        DrawFedChairSelectionModal();
                        GUI.enabled = true;
                        break;
                }
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


        /// <summary>
        /// §745 (the composition's Politics › Cabinet): <b>one card per portfolio</b>, all six (the composition's *three* is the old count - the page
        /// draws all six since R4-4) - a held portfolio its minister's portrait in the brass frame, *Portfolio · Name*, the four attributes on one line,
        /// the philosophy as a chip (and, where the portfolio is underfunded, the effectiveness ratio as a chip in the bad ink - board 9d's warning, kept
        /// at rest), and *Reshuffle* at its right; a vacant one a dashed box with the portfolio's icon and *Vacant*; a vacant one whose shortlist is
        /// drawn carries its candidates in the card, each with *Appoint*. One *Search* under the cards draws a shortlist for every vacant portfolio
        /// with none. The attributes' glossary is the head's slip; each card's slip carries the minister's line, the effectiveness readout (flow, level,
        /// the decomposition) and, for Health and Education, the family's keys. The lock's glyph where the role locks the portfolio's lever.
        /// Control counts follow clicks on this page only, never background state - the exception documented on the panel it replaces.
        /// </summary>
        private void DrawCabinetV35(float width)
        {
            Country c = _playerCountry;
            V35.FloorGuarded = true;
            int count = System.Enum.GetValues(typeof(CabinetPortfolio)).Length;
            DrawPoliticsSectionHead("Cabinet · " + UiFormat.CountWord(count).ToLowerInvariant() + " portfolios", "cab:head", width);
            _politicsSlipBook.Anchors["cab:head"] = new SlipContent("CABINET")
                .Add("LOYALTY: RESIGNS OR LEAKS UNDER PRESSURE")
                .Add("KNOWLEDGE: CAN THE MINISTRY ESTIMATE A DECISION")
                .Add("EFFICIENCY: THE PORTFOLIO'S SPENDING PER UNIT")
                .Add("POPULARITY: HOW A DECISION LANDS, AND WHAT DISMISSAL COSTS");
            var unsearched = new List<CabinetPortfolio>();
            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                if (!c.CabinetMinisters.ContainsKey(portfolio) && !_cabinetCandidatesByPortfolio.ContainsKey(portfolio)) { unsearched.Add(portfolio); }
            }
            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                DrawCabinetCardV35(portfolio, width);
                GUILayout.Space(V35.Px(8f));
            }
            if (unsearched.Count > 0)
            {
                float h = V35.Px(34f);
                Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
                const string label = "Search";
                float bw = BudgetButtonWidth(label, null);
                if (DrawBudgetButton(new Rect(row.x, row.y, bw, row.height), label, null, true))
                {
                    foreach (CabinetPortfolio portfolio in unsearched) { _cabinetCandidatesByPortfolio[portfolio] = CabinetSystem.GenerateCandidates(portfolio); }
                }
                string sentence = unsearched.Count == 1 ? "A shortlist is drawn for the vacant portfolio; you appoint from it." : "One search draws a shortlist for every vacant portfolio; you appoint from each.";
                if (Event.current.type == EventType.Repaint)
                {
                    GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                    float sx = row.x + bw + V35.Px(14f);
                    PoliSimWidgets.MeasuredLabel(new Rect(sx, row.y, Mathf.Max(1f, row.xMax - sx), row.height), V35Fit(sentence, face, Mathf.Max(1f, row.xMax - sx), out _), face);
                }
            }
            V35.FloorGuarded = false;
        }

        /// <summary>§745: one portfolio's card - held, vacant, or vacant with its shortlist.</summary>
        private void DrawCabinetCardV35(CabinetPortfolio portfolio, float width)
        {
            Country c = _playerCountry;
            string name = GetPortfolioName(portfolio);
            UiPalette.SystemArea area = UiPalette.GetPortfolioArea(portfolio);
            bool mayAct = _simulationManager.PlayerMayIntroduce(PlayerCountryId, portfolio, out string lockedBecause);
            GUIStyle titleFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle lineFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
            string anchor = "cab:" + portfolio;
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
            if (c.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister minister))
            {
                GUILayout.BeginHorizontal();
                DrawPersonPortrait(IconLibrary.GetCabinetPortrait(portfolio, minister.Name), area);
                GUILayout.Space(V35.Px(12f));
                GUILayout.BeginVertical();
                GUILayout.Label(name + " · " + minister.Name, titleFace);
                GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Loyalty {0:0} · Knowledge {1:0} · Efficiency {2:0} · Popularity {3:0}",
                    minister.Loyalty, minister.Knowledge, minister.Efficiency, minister.Popularity), lineFace);
                GUILayout.BeginHorizontal();
                DrawCabinetChip(minister.Philosophy.ToString(), PoliSimTheme.TextPrimary);
                float ratio = Effectiveness.RatioOf(c, portfolio);
                if (ratio < 0.995f) { GUILayout.Space(V35.Px(8f)); DrawCabinetChip("Underfunded ×" + ratio.ToString("0.00", CultureInfo.InvariantCulture), PoliSimTheme.Bad); }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (!mayAct) { DrawCabinetLock(anchor + ":lock", lockedBecause); }
                else if (DrawCabinetAction("Reshuffle"))
                {
                    c.CabinetMinisters.Remove(portfolio);
                    float approvalBeforeReshuffle = c.State.ApprovalRating;
                    c.State.ApprovalRating = Mathf.Clamp(c.State.ApprovalRating - CabinetSystem.ReshuffleApprovalCost * CabinetSystem.PopularityFactor(c, portfolio), 0f, 100f);   // P2-5.2: dismissing a popular minister costs the full figure, an unpopular one less
                    ApprovalLedgerRecorder.RecordEvent(c, _simulationManager.CurrentDate, $"Cabinet reshuffle ({DisplayName.Of(portfolio.ToString())})", c.State.ApprovalRating - approvalBeforeReshuffle);
                    _cabinetCandidatesByPortfolio[portfolio] = CabinetSystem.GenerateCandidates(portfolio);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                if (Event.current.type == EventType.Repaint) { SlipAnchor(GUILayoutUtility.GetLastRect(), anchor); }
                var slip = new SlipContent(name.ToUpperInvariant() + " · " + minister.Name.ToUpperInvariant())
                    .Add(minister.Philosophy.ToString().ToUpperInvariant() + " · " + minister.Description.ToUpperInvariant())
                    .Add(EffectivenessStateLine(portfolio));
                foreach (string line in CabinetEffectivenessLines(portfolio)) { slip.Add(line); }
                foreach (string line in CabinetFamilyKeyLines(portfolio)) { slip.Add(line); }
                slip.Add("RESHUFFLE: THE MINISTER GOES, A SHORTLIST IS DRAWN - APPROVAL PAYS FOR A POPULAR ONE");
                _politicsSlipBook.Anchors[anchor] = slip;
                return;
            }

            bool shortlisted = _cabinetCandidatesByPortfolio.TryGetValue(portfolio, out List<CabinetMinister> candidates);
            GUILayout.BeginHorizontal();
            DrawVacantPortrait(portfolio, area);
            GUILayout.Space(V35.Px(12f));
            GUILayout.BeginVertical();
            GUILayout.Label(name, titleFace);
            GUILayout.BeginHorizontal();
            DrawCabinetChip(shortlisted ? "Vacant · the shortlist" : "Vacant", PoliSimTheme.TextSecondary);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (shortlisted)
            {
                foreach (CabinetMinister candidate in candidates)
                {
                    GUILayout.Space(V35.Px(8f));
                    Rect rule = GUILayoutUtility.GetRect(10f, 1f, GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(rule, V35.ListRule); }
                    GUILayout.Space(V35.Px(8f));
                    GUILayout.BeginHorizontal();
                    DrawPersonPortrait(IconLibrary.GetCabinetPortrait(portfolio, candidate.Name), area);
                    GUILayout.Space(V35.Px(12f));
                    GUILayout.BeginVertical();
                    GUILayout.Label(candidate.Name, titleFace);
                    GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Loyalty {0:0} · Knowledge {1:0} · Efficiency {2:0} · Popularity {3:0}",
                        candidate.Loyalty, candidate.Knowledge, candidate.Efficiency, candidate.Popularity), lineFace);
                    GUILayout.BeginHorizontal();
                    DrawCabinetChip(candidate.Philosophy.ToString(), PoliSimTheme.TextPrimary);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    if (!mayAct) { DrawCabinetLock(anchor + ":lock", lockedBecause); }
                    else if (DrawCabinetAction("Appoint"))
                    {
                        c.CabinetMinisters[portfolio] = candidate;
                        _cabinetCandidatesByPortfolio.Remove(portfolio);
                        RecomputePolicyPreview();
                    }
                    GUILayout.EndHorizontal();
                    if (Event.current.type == EventType.Repaint) { SlipAnchor(GUILayoutUtility.GetLastRect(), anchor + ":" + candidate.Name); }
                    _politicsSlipBook.Anchors[anchor + ":" + candidate.Name] = new SlipContent(candidate.Name.ToUpperInvariant() + " · FOR " + name.ToUpperInvariant())
                        .Add(candidate.Philosophy.ToString().ToUpperInvariant() + " · " + candidate.Description.ToUpperInvariant())
                        .Add("APPOINT: THE PORTFOLIO IS HELD FROM TODAY");
                }
            }
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint && !shortlisted) { SlipAnchor(GUILayoutUtility.GetLastRect(), anchor); }
            _politicsSlipBook.Anchors[anchor] = new SlipContent(name.ToUpperInvariant() + " · VACANT")
                .Add(shortlisted ? "ITS SHORTLIST IS DRAWN - APPOINT FROM IT" : "NO SHORTLIST DRAWN - THE SEARCH UNDER THE CARDS DRAWS ONE");
        }

        /// <summary>§745: a chip in the card's text line - a boxed word at the floor, in <paramref name="ink"/>.</summary>
        private void DrawCabinetChip(string text, Color ink)
        {
            GUIStyle face = V35Serif(V35.Floor, ink, TextAnchor.MiddleCenter);
            float w = Mathf.Ceil(face.CalcSize(new GUIContent(text)).x) + V35.Px(14f), h = V35.Px(22f);
            Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, 1f), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.y, 1f, r.height), ink);
            PoliSimTheme.Rule(new Rect(r.xMax - 1f, r.y, 1f, r.height), ink);
            GUI.Label(r, text, face);
        }

        /// <summary>§745: a card's action at its right - the Budget's button, one per card; true on a click.</summary>
        private bool DrawCabinetAction(string label)
        {
            float w = BudgetButtonWidth(label, null), h = V35.Px(34f);
            Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
            return DrawBudgetButton(r, label, null, true);
        }

        /// <summary>§745: the lock's glyph in the action's place, its reason on its slip.</summary>
        private void DrawCabinetLock(string anchor, string because)
        {
            float side = V35.Px(18f);
            Rect r = GUILayoutUtility.GetRect(side, side, GUILayout.Width(side), GUILayout.Height(side));
            if (Event.current.type == EventType.Repaint) { DrawStateGlyph(r, Symbol.Locked, PoliSimTheme.TextSecondary); SlipAnchor(r, anchor); }
            _politicsSlipBook.Anchors[anchor] = new SlipContent("LOCKED").Add((because ?? string.Empty).ToUpperInvariant());
        }

        /// <summary>§745: a vacant portfolio's place for a portrait - a dashed box the portrait's size with the portfolio's icon in it.</summary>
        private void DrawVacantPortrait(CabinetPortfolio portfolio, UiPalette.SystemArea area)
        {
            float height = _labelStyle.fontSize * 5.5f, width = Mathf.Round(height * (74f / 92f));
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            if (Event.current.type != EventType.Repaint) { return; }
            Color ink = V35.PyramidThreshold;
            float dash = V35.Px(5f), gap = V35.Px(4f);
            DrawDashedRule(new Rect(r.x, r.y, r.width, 1f), ink, dash, gap);
            DrawDashedRule(new Rect(r.x, r.yMax - 1f, r.width, 1f), ink, dash, gap);
            for (float y = r.y; y < r.yMax; y += dash + gap)
            {
                float h = Mathf.Min(dash, r.yMax - y);
                PoliSimTheme.Rule(new Rect(r.x, y, 1f, h), ink);
                PoliSimTheme.Rule(new Rect(r.xMax - 1f, y, 1f, h), ink);
            }
            float side = Mathf.Min(V35.Px(24f), r.width * 0.5f);
            DrawV35Icon(new Rect(r.center.x - side * 0.5f, r.center.y - side * 0.5f, side, side), CabinetIcon(portfolio), PoliSimTheme.TextSecondary);
        }

        private static string CabinetIcon(CabinetPortfolio portfolio)
        {
            switch (portfolio)
            {
                case CabinetPortfolio.FinanceTreasury: return "safe";
                case CabinetPortfolio.InteriorJustice: return "gavel";
                case CabinetPortfolio.HealthSocialAffairs: return "heart";
                case CabinetPortfolio.ForeignAffairs: return "globe";
                case CabinetPortfolio.Education: return "book";
                default: return "shield";
            }
        }

        /// <summary>§745: board 9d's readout as slip lines - the flow, its decomposition, the level against the seed.</summary>
        private List<string> CabinetEffectivenessLines(CabinetPortfolio portfolio)
        {
            Country c = _playerCountry;
            float ratio = Effectiveness.RatioOf(c, portfolio);
            float efficiency = Effectiveness.EfficiencyOf(c, portfolio);
            float allocation = c.Effectiveness != null && c.Effectiveness.TryGetValue(portfolio, out PortfolioEffectiveness e) && e.Recorded ? e.AllocationRatio : 1f;
            float level = Effectiveness.LevelOf(c, portfolio);
            return new List<string>
            {
                "EFFECTIVENESS ×" + ratio.ToString("0.00", CultureInfo.InvariantCulture) + " - " + allocation.ToString("0.000", CultureInfo.InvariantCulture) + " ALLOC ÷ REQ × "
                    + efficiency.ToString("0.00", CultureInfo.InvariantCulture) + " EFFICIENCY · UNITY IS THE BASELINE, BELOW IS UNDERFUNDED",
                "LEVEL ×" + level.ToString("0.00", CultureInfo.InvariantCulture) + " · SPENDING PER UNIT AGAINST ITS SEED · "
                    + (level < 0.995f ? "THE ASK HAS SHRUNK" : level > 1.005f ? "THE ASK HAS GROWN" : "THE ASK HOLDS ITS SEED"),
            };
        }

        /// <summary>§745: the Health and Education ministers' cards quoted the family's keys (9c, P5-C3) - on the slip now.</summary>
        private List<string> CabinetFamilyKeyLines(CabinetPortfolio portfolio)
        {
            var lines = new List<string>();
            EconomyState s = _playerCountry.State;
            if (portfolio == CabinetPortfolio.HealthSocialAffairs && _playerCountry.Health != null && _playerCountry.Health.Seeded)
            {
                lines.Add("HEALTH · COVERAGE " + PlateFigure(s.HealthCoverage, 0, " %") + " · TREATABLE MORTALITY " + PlateFigure(s.TreatableMortality, 0) + " / 100 000"
                    + (_playerCountry.Health.HasWaits ? " · WAIT, KNEE " + PlateFigure(s.WaitKneeDays, 0) + " DAYS" : string.Empty));
            }
            if (portfolio == CabinetPortfolio.Education && _playerCountry.Education != null && _playerCountry.Education.Seeded)
            {
                lines.Add("EDUCATION · STUDENTS PER TEACHER " + PlateFigure(s.StudentsPerTeacherPrimary, 1) + " PRIMARY, " + PlateFigure(s.StudentsPerTeacherLowerSecondary, 1) + " LOWER SECONDARY"
                    + (_playerCountry.Education.HasEarlyLeavers ? " · EARLY LEAVERS " + PlateFigure(s.EarlyLeavers, 1, " %") + " OF 18-24" : string.Empty));
            }
            return lines;
        }


        /// <summary>
        /// §746 (the composition's Politics › the bank): where the country's own bank sets its rate under a governor - <b>the policy rate</b> (its change
        /// over four quarters in the neutral ink: a rate has no direction most agree on) beside <b>the rate path</b> (the projection two years out, the
        /// history its sparkline); <b>what the rule reads</b> - the rule's rate and its four terms as one bar (the neutral real rate, inflation, the
        /// inflation gap at its weight, the unemployment gap at its weight; `TaylorRule`, the page's own terms) where every term is positive, as a signed
        /// list where one is not (a negative term cannot stack); the two readings the rule weighs - <b>inflation against its target</b>, <b>unemployment
        /// against the NAIRU</b>; and <b>the governor</b> - the head's title and name, *Holds time* while a choice of successor stands. Under them, kept
        /// as built (asked): the full rate page - the path's chart with its window, the moves a year and two out, the rule's waterfall and its inputs as
        /// readings, the political half (`DrawRiksbankPage`) - and the selection itself. A bank without a governor of its own (the euro area) keeps its
        /// tab as built.
        /// </summary>
        private void DrawBankV35(float width, FedChair chair)
        {
            Country c = _playerCountry;
            float rate = c.CurrencyZone.InterestRate;
            float suggested = TaylorRule.GetSuggestedInterestRate(c);
            float gutter = V35.Px(V35.Gutter);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Political);
            string bank = GetCentralBankName(PlayerCountryId);
            V35.FloorGuarded = true;

            // ---- the rate and its path ----
            if (!_hasCachedPreview || _simulationManager.CurrentTurn != _cachedPreviewTurn) { RecomputePolicyPreview(); }
            RatePathProjection.Step[] path = RatePathProjection.Project(c, _cachedPreview);
            IReadOnlyList<float> history = c.History.InterestRate.Quarterly;
            var rateTile = new V35TileData { Icon = "bank", IconInk = area, Figure = UiFormat.Number(rate, 2) + "%", Name = "Policy rate", FigurePx = V35.FigureSmall };
            var rateSlip = new SlipContent("POLICY RATE · " + rateTile.Figure).Add("SET BY " + bank.ToUpperInvariant() + " AT EACH BOUNDARY, MOVING TOWARD ITS TARGET");
            if (history != null && history.Count >= 5)
            {
                float d = history[history.Count - 1] - history[history.Count - 5];
                if (Mathf.Abs(d) >= 0.005f)
                {
                    rateTile.Change = (d > 0f ? "▲ " : "▼ ") + UiFormat.Number(Mathf.Abs(d), 2) + " pts";
                    rateTile.ChangeInk = V35.DirectionNeutral;
                    rateSlip.Add((d > 0f ? "UP " : "DOWN ") + UiFormat.Number(Mathf.Abs(d), 2) + " POINTS OVER FOUR QUARTERS - NEUTRAL INK: A RATE HAS NO DIRECTION MOST AGREE ON");
                }
            }
            RatePathProjection.Step? far = null;
            if (path != null) { foreach (RatePathProjection.Step step in path) { if (!far.HasValue || step.YearsAhead > far.Value.YearsAhead) { far = step; } } }
            var pathTile = new V35TileData { Icon = "chart", IconInk = area, Figure = far.HasValue ? UiFormat.Number(far.Value.Rate, 2) + "%" : null, Glyph = far.HasValue ? (Symbol?)null : Symbol.Absent,
                Name = far.HasValue && far.Value.YearsAhead > 0 ? "Rate path · " + far.Value.YearsAhead + (far.Value.YearsAhead == 1 ? " year out" : " years out") : "Rate path", FigurePx = V35.FigureSmall, Spark = history };
            var pathSlip = new SlipContent("RATE PATH").Add("THE HISTORY IS THE SPARKLINE; THE FIGURE IS THE PROJECTION'S FAR END - THE PREVIEW'S YEAR AHEAD AND THE ONE AFTER, ON THE GOVERNOR'S TARGET AND SPEED");
            if (path != null) { foreach (RatePathProjection.Step step in path) { pathSlip.Add((step.YearsAhead == 0 ? "TODAY " : "+" + step.YearsAhead + " YR ") + UiFormat.Number(step.Rate, 2) + " %"); } }
            pathSlip.Add("THE FULL PATH, ITS WINDOW AND THE RULE DOTTED ARE ON THE RATE PAGE BELOW");
            float rowH = Mathf.Max(V35TileHeight(rateTile), V35TileHeight(pathTile));
            Rect row = GUILayoutUtility.GetRect(width, rowH, GUILayout.Width(width), GUILayout.Height(rowH));
            float left = V35Span(width, 4);
            var rateRect = new Rect(row.x, row.y, left, rowH);
            var pathRect = new Rect(row.x + left + gutter, row.y, row.xMax - (row.x + left + gutter), rowH);
            DrawV35Tile(rateRect, rateTile); SlipAnchor(rateRect, "bank:rate"); _politicsSlipBook.Anchors["bank:rate"] = rateSlip;
            DrawV35Tile(pathRect, pathTile); SlipAnchor(pathRect, "bank:path"); _politicsSlipBook.Anchors["bank:path"] = pathSlip;
            GUILayout.Space(gutter);

            // ---- what the rule reads ----
            DrawPoliticsSectionHead("What the rule reads", "bank:rulehead", width);
            float inflation = c.State.Inflation;
            float target = TaylorRule.InflationTarget(c);
            float neutralReal = TaylorRule.NeutralRealRate(c);
            float inflationGap = TaylorRule.InflationGapWeight(c) * (inflation - target);
            float unemploymentGap = TaylorRule.GetGapTermPercentagePoints(c);
            _politicsSlipBook.Anchors["bank:rulehead"] = new SlipContent("WHAT THE RULE READS")
                .Add("THE RULE'S RATE IS THE SUM OF FOUR TERMS: THE NEUTRAL REAL RATE, INFLATION, THE INFLATION GAP AT ITS WEIGHT ("
                    + TaylorRule.InflationGapWeight(c).ToString("0.##", CultureInfo.InvariantCulture) + ") AND THE UNEMPLOYMENT GAP AT ITS WEIGHT ("
                    + TaylorRule.UnemploymentGapWeight(c).ToString("0.##", CultureInfo.InvariantCulture) + ")")
                .Add("THE GOVERNOR'S TARGET IS THE RULE'S RATE PLUS THE GOVERNOR'S LEAN; THE RATE MOVES TOWARD IT AT ITS SPEED");
            var terms = new[]
            {
                ("Neutral real rate", neutralReal, V35.DataSand),
                ("Inflation", inflation, V35.DataSlateLight),
                ("Inflation gap", inflationGap, V35.DataSlateMid),
                ("Unemployment gap", unemploymentGap, V35.DataDeep),
            };
            bool stacks = true;
            foreach (var t in terms) { stacks &= t.Item2 >= 0f; }
            // the head as the tile head lays it out - the figure over the name, or the icon where taller (the first film's bar sat on the name)
            float pad = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY);
            float headH = Mathf.Max(V35.Px(V35.ListIcon), Mathf.Ceil(V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y) + 2f
                + Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y));
            var parts = new List<V35Part>();
            V35BarLayout layout = null;
            float sum = neutralReal + inflation + inflationGap + unemploymentGap;
            if (stacks)
            {
                foreach (var t in terms)
                {
                    // the words' ink by the segment's lightness - light words on the two dark inks (the first film's unemployment gap read dark on dark)
                    float luminance = 0.299f * t.Item3.r + 0.587f * t.Item3.g + 0.114f * t.Item3.b;
                    parts.Add(new V35Part(t.Item1, UiFormat.Number(t.Item2, 2), t.Item3, luminance < 0.55f ? V35.OnDataDark : V35.OnDataLight, t.Item2, "bank:term:" + t.Item1));
                }
                layout = LayOutV35Bar(width - pad * 2f, parts, Mathf.Max(0.0001f, sum));
            }
            float bodyH = stacks ? V35BarHeight(layout) : terms.Length * V35.Px(V35.ListRow + 2f);
            float cardH = padY * 2f + headH + V35.Px(10f) + bodyH;
            Rect card = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            Rect inner = DrawV35Card(card);
            Rect figureRect = DrawBudgetTileHead(inner, "scales", area, UiFormat.Number(suggested, 2) + "%", PoliSimTheme.TextPrimary, "The rule reads", PoliSimTheme.TextPrimary, new Rect(inner.xMax, inner.y, 0f, 0f));
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, headH), "bank:rule");
            var ruleSlip = new SlipContent("THE RULE READS " + UiFormat.Number(suggested, 2) + " %");
            foreach (var t in terms) { ruleSlip.Add(t.Item1.ToUpperInvariant() + " " + t.Item2.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)); }
            if (sum < 0f) { ruleSlip.Add("THE TERMS SUM BELOW ZERO; THE RULE READS ZERO"); }
            _politicsSlipBook.Anchors["bank:rule"] = ruleSlip;
            var body = new Rect(inner.x, inner.y + headH + V35.Px(10f), inner.width, bodyH);
            if (stacks)
            {
                DrawV35Bar(body, parts, Mathf.Max(0.0001f, sum), layout, (r, id) => SlipAnchor(r, id));
                foreach (var t in terms)
                {
                    _politicsSlipBook.Anchors["bank:term:" + t.Item1] = new SlipContent(t.Item1.ToUpperInvariant() + " · " + UiFormat.Number(t.Item2, 2) + " POINTS")
                        .Add("ONE OF THE RULE'S FOUR TERMS - THE SEGMENT IS ITS SHARE OF " + UiFormat.Number(sum, 2));
                }
            }
            else if (Event.current.type == EventType.Repaint)
            {
                // a negative term cannot stack on the others: the four as a signed list
                GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                GUIStyle figureFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
                float lh = V35.Px(V35.ListRow + 2f);
                for (int i = 0; i < terms.Length; i++)
                {
                    var line = new Rect(body.x, body.y + i * lh, body.width, lh);
                    PoliSimTheme.Rule(new Rect(line.x, line.y + V35.Px(5f), V35.Px(10f), V35.Px(10f)), terms[i].Item3);
                    GUI.Label(new Rect(line.x + V35.Px(18f), line.y, line.width * 0.6f, line.height), terms[i].Item1, nameFace);
                    GUI.Label(line, StatsReadings.TrueMinus(terms[i].Item2.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)), figureFace);
                }
            }
            GUILayout.Space(gutter);

            // ---- the two readings the rule weighs ----
            float nairu = c.EffectiveNaturalUnemploymentRate;
            var inflationTile = new V35TileData { Icon = "infl", IconInk = area, Figure = UiFormat.Number(inflation, 1) + "%", Name = "Inflation · target " + UiFormat.Number(target, 1) + " %", FigurePx = V35.FigureSmall };
            var unemploymentTile = new V35TileData { Icon = "jobs", IconInk = area, Figure = UiFormat.Number(c.State.Unemployment, 1) + "%", Name = "Unemployment · NAIRU " + UiFormat.Number(nairu, 1) + " %", FigurePx = V35.FigureSmall };
            float half = V35Span(width, 6), readH = Mathf.Max(V35TileHeight(inflationTile), V35TileHeight(unemploymentTile));
            Rect readRow = GUILayoutUtility.GetRect(width, readH, GUILayout.Width(width), GUILayout.Height(readH));
            var inflRect = new Rect(readRow.x, readRow.y, half, readH);
            var unRect = new Rect(readRow.x + half + gutter, readRow.y, readRow.xMax - (readRow.x + half + gutter), readH);
            DrawV35Tile(inflRect, inflationTile); SlipAnchor(inflRect, "bank:inflation");
            DrawV35Tile(unRect, unemploymentTile); SlipAnchor(unRect, "bank:unemployment");
            _politicsSlipBook.Anchors["bank:inflation"] = new SlipContent("INFLATION · " + inflationTile.Figure).Add("THE TARGET " + UiFormat.Number(target, 1) + " % · THE GAP " + (inflation - target).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " POINTS, WEIGHED AT " + TaylorRule.InflationGapWeight(c).ToString("0.##", CultureInfo.InvariantCulture));
            _politicsSlipBook.Anchors["bank:unemployment"] = new SlipContent("UNEMPLOYMENT · " + unemploymentTile.Figure).Add("THE NAIRU " + UiFormat.Number(nairu, 1) + " % - THE RATE AT WHICH WAGES NEITHER SPEED NOR SLOW · THE GAP IS WEIGHED AT " + TaylorRule.UnemploymentGapWeight(c).ToString("0.##", CultureInfo.InvariantCulture));
            GUILayout.Space(gutter);

            // ---- the governor ----
            bool pending = _fedChairCandidates != null && _fedChairCandidates.Count > 0;
            string head = GetCentralBankHeadTitle(PlayerCountryId);
            float govH = padY * 2f + V35.Px(44f);
            Rect gov = GUILayoutUtility.GetRect(width, govH, GUILayout.Width(width), GUILayout.Height(govH));
            Rect govInner = DrawV35Card(gov);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(gov.x, gov.y, Mathf.Max(2f, V35.Px(3f)), gov.height), pending ? PoliSimTheme.Caution : area);
                float side = V35.Px(24f);
                DrawV35Icon(new Rect(govInner.x, govInner.y + Mathf.Round((govInner.height - side) * 0.5f), side, side), "person", area);
                float tx = govInner.x + side + V35.Px(12f);
                GUI.Label(new Rect(tx, govInner.y, govInner.width * 0.7f, V35.Px(24f)), head + " · " + chair.Name, V35Serif(V35.Name, PoliSimTheme.TextPrimary));
                GUI.Label(new Rect(tx, govInner.y + V35.Px(24f), govInner.width * 0.7f, V35.Px(20f)),
                    pending ? CapitalWord(UiFormat.CountWord(_fedChairCandidates.Count)) + " candidates for the next term - the choice is below and on the Docket" : chair.Philosophy + " · the term ends on the bank's own cycle",
                    V35Serif(V35.Floor, PoliSimTheme.TextSecondary));
                if (pending)
                {
                    GUIStyle stamp = V35Mono(V35.Floor, PoliSimTheme.Caution, bold: true, TextAnchor.MiddleCenter);
                    float sw = Mathf.Ceil(stamp.CalcSize(new GUIContent("Holds time")).x) + V35.Px(14f), sh = V35.Px(22f);
                    var sr = new Rect(govInner.xMax - sw, govInner.y + Mathf.Round((govInner.height - sh) * 0.5f), sw, sh);
                    PoliSimTheme.Rule(new Rect(sr.x, sr.y, sr.width, 1f), PoliSimTheme.Caution);
                    PoliSimTheme.Rule(new Rect(sr.x, sr.yMax - 1f, sr.width, 1f), PoliSimTheme.Caution);
                    PoliSimTheme.Rule(new Rect(sr.x, sr.y, 1f, sr.height), PoliSimTheme.Caution);
                    PoliSimTheme.Rule(new Rect(sr.xMax - 1f, sr.y, 1f, sr.height), PoliSimTheme.Caution);
                    GUI.Label(sr, "Holds time", stamp);
                }
            }
            SlipAnchor(gov, "bank:governor");
            _politicsSlipBook.Anchors["bank:governor"] = new SlipContent(head.ToUpperInvariant() + " · " + chair.Name.ToUpperInvariant())
                .Add(chair.Philosophy.ToString().ToUpperInvariant() + " · LEAN " + chair.RateBias.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " ON THE RULE'S RATE")
                .Add(chair.Description.ToUpperInvariant())
                .Add(pending ? "A SUCCESSOR IS TO BE CHOSEN - TIME HOLDS UNTIL ONE IS" : "THE TERM ENDS ON THE BANK'S OWN CYCLE; A SHORTLIST IS DRAWN THEN");
            V35.FloorGuarded = false;
            GUILayout.Space(gutter);
        }

        /// <summary>§746: a count word as a sentence starts it - "Two", not "TWO" or "two".</summary>
        private static string CapitalWord(string word) => string.IsNullOrEmpty(word) ? word : char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();

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
