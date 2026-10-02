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
    /// Cabinet and the bank draw as built under the new frame until their own items.
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

            if (_politicsCategory == PoliticsCategory.Parliament)
            {
                int scrolledFrom = _slipAnchors.Count;
                float viewport = Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f);
                _parliamentScrollPosition = GUILayout.BeginScrollView(_parliamentScrollPosition, GUILayout.Height(viewport));
                DrawParliamentV35(contentWidth);
                GUILayout.EndScrollView();
                MoveScrolledAnchors(scrolledFrom, GUILayoutUtility.GetLastRect(), _parliamentScrollPosition);
                GUILayout.EndVertical();
                if (!DeskProvenance.On) { DrawSlips(_politicsSlipBook, GUILayoutUtility.GetLastRect()); }
                return;
            }

            // The tabs not yet retrofitted - drawn as built under the v3.5 title: the screen's caption, then the content.
            DrawScreenCaption(PoliticsScreenCaption());
            float contentHeight = bodyHeight - ScreenCaptionBlockHeight();
            switch (_politicsCategory)
            {
                case PoliticsCategory.Compass:
                    float compassScrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    _politicsContentScrollPosition = GUILayout.BeginScrollView(_politicsContentScrollPosition, GUILayout.Height(compassScrollHeight));
                    DrawPoliticalCompassContent(availableWidth);
                    GUILayout.EndScrollView();
                    break;
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
