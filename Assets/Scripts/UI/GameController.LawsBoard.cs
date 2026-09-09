using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D16-2, board 10b (2026-09-09, §411) - THE LAWS PAGES, controls demoted. Three stacked bands become <b>one control
    /// line</b>: category · sort · show · search, on the rule that <b>only the active member of a set is boxed</b> - boxes are ink, and
    /// a boxed inactive option spends the same ink as content and out-weighs it four to one. The category keeps the brass face and gains
    /// its count and a `▾`, because it is the one control you are always inside: a place, not a filter. Sort and show become plain mono
    /// words with a brass underline on the live one, and the four status boxes disappear into their own words (`ENACTED 4`).
    ///
    /// <para>The pane reads in the order a decision is made (§4.3): name and status, then <b>the decision block</b> - what it costs and
    /// whether it passes, boxed together on tinted paper with the button - then the estimate, the description, the magnitude and dials,
    /// the party split, and the citation behind the desk's `†`. The build's order put description and magnitude above the estimate and
    /// the button at the bottom, so a player who had decided still had to read past everything to act.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>D16 §4.1: the category menu's open state - the one control you are always inside is a place, so it opens a list rather than spending a row on six chips.</summary>
        private bool _lawCategoryMenuOpen;
        private Rect _lawCategoryMenuAnchor;

        private static readonly (LawBrowserFilter Filter, string Label)[] LawCategoryMenu =
        {
            // D16 §4.2 (§415): the categories in the plain register - what each is about, not the department it belongs to.
            (LawBrowserFilter.All, "All laws"),
            (LawBrowserFilter.CrimeJustice, "Crime & justice"),
            (LawBrowserFilter.LaborMarket, "Work & wages"),
            (LawBrowserFilter.LabourInstitutions, "Unions & contracts"),
            (LawBrowserFilter.FiscalFramework, "The budget rules"),
            (LawBrowserFilter.MonetaryRegime, "The interest rate"),
        };

        private int LawCategoryCount(LawBrowserFilter filter)
        {
            switch (filter)
            {
                case LawBrowserFilter.CrimeJustice: return CrimeJusticeLawCount;
                case LawBrowserFilter.LaborMarket: return LaborMarketLawCount;
                case LawBrowserFilter.LabourInstitutions: return LabourInstitutionsLawCount;
                case LawBrowserFilter.FiscalFramework: return FiscalFrameworkLawCount;
                case LawBrowserFilter.MonetaryRegime: return MonetaryRegimeLawCount;
                default: return LawCatalog.All.Count;
            }
        }

        private static string LawCategoryMenuLabel(LawBrowserFilter filter)
        {
            foreach ((LawBrowserFilter f, string label) in LawCategoryMenu) { if (f == filter) { return label; } }
            return "All laws";
        }

        /// <summary>
        /// D16 §4.1, the one control line: the category as a brass place with its count and a `▾`, SORT and SHOW as plain words with a
        /// brass underline under the live one, the status counts on the words themselves, and the search field. The search slot reflows
        /// to a second line when the measured pieces do not fit - the free-aspect lesson the three bands were carrying, kept.
        /// </summary>
        /// <summary>The control line's own measure, shared by the draw and by the scroll view's chrome reserve: its height, and whether the
        /// search slot fits inline or reflows. One arithmetic in one place - the three bands it replaced were measured twice and the
        /// reserve drifted from the draw (the instance-#12 class the reserve's own comment records).</summary>
        private float LawControlLineHeight(float innerWidth, int enactedCount, int pendingCount, int availableCount, out bool searchInline)
        {
            GUIStyle label = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUIStyle word = DeskCaption(9f, PoliSimTheme.TextMuted);
            GUIStyle place = DeskCaption(9f, PoliSimTheme.InkOnStock, true, TextAnchor.MiddleCenter);
            float gap = StatsUnit(16f);
            float rowH = Mathf.Max(StatsUnit(26f), Mathf.Ceil(DeskCaptionHeight(word)) + StatsUnit(10f));
            string placeText = LawCategoryMenuLabel(_lawBrowserFilter).ToUpperInvariant() + "  " + LawCategoryCount(_lawBrowserFilter) + "  ▾";
            float placeW = place.CalcSize(new GUIContent(placeText)).x + StatsUnit(18f);
            float sortW = 0f;
            foreach (string t in new[] { "MAGNITUDE", "A–Z", "COST" }) { sortW += word.CalcSize(new GUIContent(t)).x + StatsUnit(10f); }
            float showW = 0f;
            foreach (string t in new[] { "ALL", "ENACTED " + enactedCount, "PENDING " + pendingCount, "AVAILABLE " + availableCount }) { showW += word.CalcSize(new GUIContent(t)).x + StatsUnit(10f); }
            float need = placeW + gap
                + label.CalcSize(new GUIContent("SORT")).x + StatsUnit(6f) + sortW + gap
                + label.CalcSize(new GUIContent("SHOW")).x + StatsUnit(6f) + showW + gap
                + label.CalcSize(new GUIContent("SEARCH")).x + StatsUnit(6f) + StatsUnit(130f);
            searchInline = need <= innerWidth;
            return rowH;
        }

        /// <summary>The search field's width on the control line, and the width its reflowed row takes.</summary>
        private float LawControlSearchFieldWidth() => StatsUnit(130f);

        private void DrawLawControlLine(float innerWidth, int enactedCount, int pendingCount, int availableCount)
        {
            GUIStyle label = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUIStyle word = DeskCaption(9f, PoliSimTheme.TextMuted);
            GUIStyle wordOn = DeskCaption(9f, PoliSimTheme.TextPrimary);
            GUIStyle place = DeskCaption(9f, PoliSimTheme.InkOnStock, true, TextAnchor.MiddleCenter);
            float gap = StatsUnit(16f);
            float rowH = Mathf.Max(StatsUnit(26f), Mathf.Ceil(DeskCaptionHeight(word)) + StatsUnit(10f));

            string placeText = LawCategoryMenuLabel(_lawBrowserFilter).ToUpperInvariant() + "  " + LawCategoryCount(_lawBrowserFilter) + "  ▾";
            float placeW = place.CalcSize(new GUIContent(placeText)).x + StatsUnit(18f);
            var sorts = new[] { (LawOrder.Magnitude, "MAGNITUDE"), (LawOrder.Alphabetical, "A–Z"), (LawOrder.Cost, "COST") };
            var shows = new[] { (LawStatusFilter.All, "ALL"), (LawStatusFilter.Enacted, "ENACTED " + enactedCount), (LawStatusFilter.Pending, "PENDING " + pendingCount), (LawStatusFilter.Available, "AVAILABLE " + availableCount) };
            float sortLabelW = label.CalcSize(new GUIContent("SORT")).x + StatsUnit(6f);
            float showLabelW = label.CalcSize(new GUIContent("SHOW")).x + StatsUnit(6f);
            float sortW = 0f; foreach ((LawOrder _, string t) in sorts) { sortW += word.CalcSize(new GUIContent(t)).x + StatsUnit(10f); }
            float showW = 0f; foreach ((LawStatusFilter _, string t) in shows) { showW += word.CalcSize(new GUIContent(t)).x + StatsUnit(10f); }
            float searchLabelW = label.CalcSize(new GUIContent("SEARCH")).x + StatsUnit(6f);
            float searchFieldW = LawControlSearchFieldWidth();
            LawControlLineHeight(innerWidth, enactedCount, pendingCount, availableCount, out bool searchInline);

            Rect row = GUILayoutUtility.GetRect(10f, rowH, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(row.x, row.y, row.width, 1f), PoliSimTheme.Hairline);
                PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.Hairline);
            }

            float x = row.x;
            // The place: the only box on the line.
            var placeRect = new Rect(x, row.y + StatsUnit(4f), placeW, rowH - StatsUnit(8f));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(placeRect, PoliSimTheme.Brass);
                PoliSimTheme.Rule(new Rect(placeRect.x, placeRect.y, placeRect.width, 1f), PoliSimTheme.BrassBorder);
                PoliSimTheme.Rule(new Rect(placeRect.x, placeRect.yMax - 1f, placeRect.width, 1f), PoliSimTheme.BrassBorder);
                PoliSimWidgets.MeasuredLabel(placeRect, placeText, place);
            }
            if (PoliSimWidgets.Button(placeRect, GUIContent.none, GUIStyle.none)) { _lawCategoryMenuOpen = !_lawCategoryMenuOpen; }
            _lawCategoryMenuAnchor = placeRect;
            x += placeW + gap;

            x = DrawLawControlSet(row, x, "SORT", sortLabelW, label, word, wordOn, sorts.Length,
                i => sorts[i].Item2, i => _lawOrder == sorts[i].Item1, i => _lawOrder = sorts[i].Item1) + gap;
            x = DrawLawControlSet(row, x, "SHOW", showLabelW, label, word, wordOn, shows.Length,
                i => shows[i].Item2, i => _lawStatusFilter == shows[i].Item1, i => _lawStatusFilter = shows[i].Item1) + gap;

            if (searchInline)
            {
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, searchLabelW, rowH), "SEARCH", label); }
                _lawSearchText = GUI.TextField(new Rect(x + searchLabelW, row.y + StatsUnit(5f), searchFieldW, rowH - StatsUnit(10f)), _lawSearchText ?? string.Empty, 40, UiPalette.BuildTextFieldStyle(_labelStyle.fontSize));
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                DrawLawSearchSlot(searchLabelW, searchFieldW);
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>One set of plain words on the control line: the label, then the members, only the active one inked and underlined in brass.</summary>
        private float DrawLawControlSet(Rect row, float x, string setLabel, float labelWidth, GUIStyle label, GUIStyle word, GUIStyle wordOn,
            int count, System.Func<int, string> textOf, System.Func<int, bool> activeOf, System.Action<int> select)
        {
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, labelWidth, row.height), setLabel, label); }
            x += labelWidth;
            for (int i = 0; i < count; i++)
            {
                string text = textOf(i);
                bool active = activeOf(i);
                float w = word.CalcSize(new GUIContent(text)).x + StatsUnit(10f);
                var cell = new Rect(x, row.y, w, row.height);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimWidgets.MeasuredLabel(cell, text, active ? wordOn : word);
                    if (active) { PoliSimTheme.Rule(new Rect(cell.x + StatsUnit(4f), cell.yMax - StatsUnit(6f), w - StatsUnit(8f), 2f), PoliSimTheme.Brass); }
                }
                if (PoliSimWidgets.Button(cell, GUIContent.none, GUIStyle.none)) { select(i); }
                x += w;
            }
            return x;
        }

        /// <summary>The category list, drawn last so it paints over the rows beneath it (IMGUI has no layer; the desk's own answer is draw order).</summary>
        private void DrawLawCategoryMenuIfOpen()
        {
            if (!_lawCategoryMenuOpen) { return; }
            GUIStyle item = DeskCaption(9f, PoliSimTheme.TextPrimary);
            GUIStyle count = DeskCaption(8f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            float rowH = Mathf.Ceil(DeskCaptionHeight(item)) + StatsUnit(8f);
            float width = Mathf.Max(_lawCategoryMenuAnchor.width, StatsUnit(190f));
            var menu = new Rect(_lawCategoryMenuAnchor.x, _lawCategoryMenuAnchor.yMax, width, rowH * LawCategoryMenu.Length);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(menu, PoliSimTheme.Card);
                PoliSimTheme.Rule(new Rect(menu.x, menu.y, menu.width, 1f), PoliSimTheme.BorderPaper);
                PoliSimTheme.Rule(new Rect(menu.x, menu.yMax - 1f, menu.width, 1f), PoliSimTheme.BorderPaper);
                PoliSimTheme.Rule(new Rect(menu.x, menu.y, 1f, menu.height), PoliSimTheme.BorderPaper);
                PoliSimTheme.Rule(new Rect(menu.xMax - 1f, menu.y, 1f, menu.height), PoliSimTheme.BorderPaper);
            }
            for (int i = 0; i < LawCategoryMenu.Length; i++)
            {
                var cell = new Rect(menu.x, menu.y + i * rowH, menu.width, rowH);
                if (Event.current.type == EventType.Repaint)
                {
                    if (_lawBrowserFilter == LawCategoryMenu[i].Filter) { PoliSimTheme.Rule(new Rect(cell.x, cell.y, StatsUnit(3f), cell.height), PoliSimTheme.Brass); }
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.x + StatsUnit(9f), cell.y, cell.width - StatsUnit(40f), cell.height), LawCategoryMenu[i].Label, item);
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.xMax - StatsUnit(34f), cell.y, StatsUnit(28f), cell.height), LawCategoryCount(LawCategoryMenu[i].Filter).ToString(CultureInfo.InvariantCulture), count);
                    if (i > 0) { PoliSimTheme.Rule(new Rect(cell.x + StatsUnit(6f), cell.y, cell.width - StatsUnit(12f), 1f), PoliSimTheme.RuleRow); }
                }
                if (PoliSimWidgets.Button(cell, GUIContent.none, GUIStyle.none)) { _lawBrowserFilter = LawCategoryMenu[i].Filter; _lawCategoryMenuOpen = false; }
            }
            // A click anywhere else closes it, which is what a menu does.
            if (Event.current.type == EventType.MouseDown && !menu.Contains(Event.current.mousePosition) && !_lawCategoryMenuAnchor.Contains(Event.current.mousePosition))
            {
                _lawCategoryMenuOpen = false;
            }
        }

        /// <summary>
        /// D16 §4.3, the decision block: what it costs and whether it passes, boxed together on the panel tint because those two facts
        /// ARE the decision, with the vote bar answering "will it pass" in one 14 px drawing - the house at 349 in party inks, FOR then
        /// AGAINST, and the majority tick at 175 - and the CTA travelling with them rather than with the end of the page.
        /// </summary>
        private void DrawLawDecisionBlock(LawDefinition law, bool enacted, LawBill pendingBill, BillConcern concern, float contentWidth)
        {
            GUIStyle small = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUIStyle figure = DeskCaption(22f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleLeft);
            GUIStyle verdict = DeskBody(15f, PoliSimTheme.TextPrimary);
            float pad = StatsUnit(12f);
            float barH = StatsUnit(14f);
            float blockH = Mathf.Ceil(DeskCaptionHeight(small)) * 2f + Mathf.Ceil(DeskCaptionHeight(figure)) + barH + StatsUnit(34f);
            Rect block = GUILayoutUtility.GetRect(contentWidth, blockH, GUILayout.Width(contentWidth));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(block, PoliSimTheme.CardInset);
                PoliSimTheme.Rule(new Rect(block.x, block.y, block.width, 1f), PoliSimTheme.EdgeDashed);
                PoliSimTheme.Rule(new Rect(block.x, block.yMax - 1f, block.width, 1f), PoliSimTheme.EdgeDashed);
                PoliSimTheme.Rule(new Rect(block.x, block.y, 1f, block.height), PoliSimTheme.EdgeDashed);
                PoliSimTheme.Rule(new Rect(block.xMax - 1f, block.y, 1f, block.height), PoliSimTheme.EdgeDashed);

                float y = block.y + StatsUnit(6f);
                float capH = Mathf.Ceil(DeskCaptionHeight(small));
                // What it costs. The desk holds one cost per law - the approval it spends on passage - and no money figure at all,
                // so the board's signed Budget-ink cost is refused and said rather than faked (§411 states it).
                PoliSimWidgets.MeasuredLabel(new Rect(block.x + pad, y, contentWidth * 0.5f, capH), "WHAT IT COSTS · APPROVAL, ON PASSAGE", small);
                PoliSimWidgets.MeasuredLabel(new Rect(block.x + pad, y + capH, contentWidth * 0.4f, Mathf.Ceil(DeskCaptionHeight(figure))),
                    law.EnactmentApprovalCost.ToString("0.0", CultureInfo.InvariantCulture), figure);
                // Will it pass - the count, and the bar beneath it.
                int forSeats = 0, againstSeats = 0;
                if (concern != null && !concern.IsEmpty)
                {
                    foreach ((PoliticalParty _, int seats, int side, float _, bool measured) in ParliamentSystem.SeatSides(_playerCountry, concern))
                    {
                        if (!measured || side == 0) { continue; }
                        if (side > 0) { forSeats += seats; } else { againstSeats += seats; }
                    }
                }
                bool passes = ParliamentSystem.WouldBillPass(_playerCountry, concern);
                string verdictText = concern == null || concern.IsEmpty
                    ? "UNCONTESTED · CARRIES"
                    : (passes ? "CARRIES · " : "FALLS · ") + forSeats + " ⁄ " + againstSeats;
                PoliSimWidgets.MeasuredLabel(new Rect(block.x + contentWidth * 0.45f, y, contentWidth * 0.5f, capH), "WILL IT PASS", small);
                PoliSimWidgets.MeasuredLabel(new Rect(block.x + contentWidth * 0.45f, y + capH, contentWidth * 0.5f, Mathf.Ceil(DeskCaptionHeight(figure))), verdictText, verdict);
                DrawLawVoteBar(new Rect(block.x + pad, y + capH + Mathf.Ceil(DeskCaptionHeight(figure)) + StatsUnit(6f), contentWidth - pad * 2f, barH), concern, forSeats, againstSeats);
            }
        }

        /// <summary>
        /// The vote bar (§4.3): one 14 px band, the parties in FOR-then-AGAINST order at `seats ⁄ 349` of the width in their own inks, the
        /// undecided in the neutral register, and the majority tick 2 px at `175 ⁄ 349`. The inks are the desk's party table - the standing
        /// identity (§256, §279) - not board 10d's assignment, which is not in the record (§409).
        /// </summary>
        private void DrawLawVoteBar(Rect bar, BillConcern concern, int forSeats, int againstSeats)
        {
            PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
            int total = 0;
            var ordered = new List<(PoliticalParty Party, int Seats, int Side)>();
            if (concern != null && !concern.IsEmpty)
            {
                foreach ((PoliticalParty party, int seats, int side, float _, bool measured) in ParliamentSystem.SeatSides(_playerCountry, concern))
                {
                    total += seats;
                    ordered.Add((party, seats, measured ? side : 0));
                }
            }
            if (total <= 0) { return; }
            ordered.Sort((a, b) => b.Side.CompareTo(a.Side));   // FOR, then undecided, then AGAINST
            float x = bar.x;
            foreach ((PoliticalParty party, int seats, int side) in ordered)
            {
                float w = bar.width * seats / total;
                Color ink = side == 0 ? PoliSimTheme.Neutral : PoliSimTheme.Party(_playerCountry.Id, party.Abbrev);
                PoliSimTheme.Rule(new Rect(x, bar.y, w, bar.height), ink);
                x += w;
            }
            float tickX = bar.x + bar.width * (total / 2f + 0.5f) / total;
            PoliSimTheme.Rule(new Rect(tickX - 1f, bar.y - StatsUnit(3f), 2f, bar.height + StatsUnit(6f)), PoliSimTheme.TextPrimary);
            GUIStyle caption = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUIStyle right = DeskCaption(7f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            float capY = bar.yMax + StatsUnit(3f);
            float capH = Mathf.Ceil(DeskCaptionHeight(caption));
            PoliSimWidgets.MeasuredLabel(new Rect(bar.x, capY, bar.width * 0.33f, capH), "FOR " + forSeats, caption);
            PoliSimWidgets.MeasuredLabel(new Rect(bar.x + bar.width * 0.33f, capY, bar.width * 0.34f, capH), "MAJORITY " + (total / 2 + 1), new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter });
            PoliSimWidgets.MeasuredLabel(new Rect(bar.x + bar.width * 0.67f, capY, bar.width * 0.33f, capH), "AGAINST " + againstSeats, right);
        }
    }
}
