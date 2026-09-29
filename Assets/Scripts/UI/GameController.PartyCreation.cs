using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §676 (SP-4): **THE CREATION FLOW, DRAWN** - five steps (profile, placement, declarations, leader, review) as one IMGUI screen swap before the
    /// game, opened from the Canvas party panel's CREATE A PARTY and closed back to it, the settings screen's own seam. Built structurally and
    /// ICON-FIRST: the choices are marks, swatches, pips, glyphs and a compass; the words are on slips (the two-level tooltip, §666) read from
    /// `PartyCreationFlow.Slips`, the same book `PartyCreationCheck` reads. The glyphs are the registry's (`SymbolRegistry`, honest fallbacks). START
    /// registers the draft and seats the player as it; the world is rebuilt on the roster that now carries it.
    /// ⚠ Drawn with absolute rects throughout - no GUILayout, so no Layout-pass dummy rect keys a control.
    /// </summary>
    public partial class GameController
    {
        private bool _partyCreationOpen;
        private bool _createPartyRequested;
        private CountryId _partyCreationCountry = CountryId.Sweden;
        private CreatedParty _pcDraft;
        private int _pcStep;
        private int _pcStepShown = -1;
        private string _pcRefused;
        private PeopleSlips.Book _pcBook;
        private bool _pcDragging;
        private string _pcPredictedFor;
        private double _pcPredicted = double.NaN;

        /// <summary>The party panel's CREATE A PARTY: the selector exits under its cover and the flow opens behind it (the seam watches the request).</summary>
        private void RequestPartyCreation(CountryId country)
        {
            _partyCreationCountry = country;
            _createPartyRequested = true;
        }

        /// <summary>Opens the flow on a fresh draft - also the film's entry.</summary>
        private void OpenPartyCreation(CountryId country)
        {
            _partyCreationCountry = country;
            _pcDraft = PartyCreationFlow.NewDraft(country);
            _pcStep = 0;
            _pcRefused = null;
            _pcPredictedFor = null;
            _partyCreationOpen = true;
        }

        /// <summary>BACK from the first step: the draft is dropped and the selector enters again (its build guard watches the flag).</summary>
        private void ClosePartyCreation()
        {
            _partyCreationOpen = false;
            _pcDraft = null;
            _pcBook = null;
            _slipPins.Clear();   // the flow's pinned slips belong to its anchors; none follows the player out
            _pcDragging = false;
        }

        /// <summary>START: the draft registered, the world rebuilt on the roster that carries it, the player seated as it.</summary>
        private void CommitPartyCreation()
        {
            if (!PartyCreationFlow.TryCommit(_pcDraft, out _pcRefused)) { Debug.Log("PARTY CREATION: refused - " + _pcRefused); return; }
            CountryId country = _pcDraft.Country;
            string key = _pcDraft.Key;
            Debug.Log($"PARTY CREATION: '{_pcDraft.Name}' ({key}) registered for {country} - {_pcDraft.Origin}, at ({_pcDraft.LrEcon:0.0}, {_pcDraft.Galtan:0.0}), mark {_pcDraft.MarkStem}, ink {_pcDraft.InkHex}.");
            ClosePartyCreation();
            _worldRosterStale = true;
            SelectPlayerCountryAndParty(country, key);
        }

        /// <summary>Set when the roster changed after the world was built (a party registered): the next opening rebuilds the world even on the same
        /// epoch, because the world snapshots the roster (each party's campaign capital, `WorldFactory`).</summary>
        private bool _worldRosterStale;

        private void DrawPartyCreationScreen()
        {
            DrawMenuBackground();
            if (_pcDraft == null) { OpenPartyCreation(_partyCreationCountry); }
            CreatedParty d = _pcDraft;
            if (Event.current.type == EventType.Layout || _pcBook == null) { _pcBook = PartyCreationFlow.Slips(d); }
            BeginSlipAnchors();
            if (_pcStep != _pcStepShown) { if (_pcStepShown >= 0) { _slipPins.Clear(); } _pcStepShown = _pcStep; }   // a pinned slip belongs to its step's anchors; a new step lets it go

            float u = StatsUnit(1f);
            float marginX = UiScreen.Width * ScreenMarginFraction;
            float marginY = UiScreen.Height * ScreenMarginFraction;
            var area = new Rect(marginX, marginY, UiScreen.Width - marginX * 2f, UiScreen.Height - marginY * 2f);
            if (Event.current.type == EventType.Repaint) { _boxStyle.Draw(area, GUIContent.none, false, false, false, false); }
            float pad = StatStep(18f);
            var inner = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);

            // ── the head: the title and the five step chips (numerals; each step's words on its slip) ──
            float headH = Mathf.Ceil(_headerStyle.CalcHeight(new GUIContent("CREATE A PARTY"), inner.width));
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y, inner.width * 0.5f, headH), "CREATE A PARTY", _headerStyle); }
            GUIStyle chip = SettingsChipCaption();
            float chipH = SettingsChipHeight(chip);
            float chipW = chipH * 1.6f;
            float cx = inner.xMax - PartyCreationFlow.StepCount * (chipW - 1f);
            for (int s = 0; s < PartyCreationFlow.StepCount; s++)
            {
                var r = new Rect(cx + s * (chipW - 1f), inner.y + Mathf.Round((headH - chipH) * 0.5f), chipW, chipH);
                if (DrawDeskChipButton(r, (s + 1).ToString(CultureInfo.InvariantCulture), chip, selected: s == _pcStep, disabled: false)) { _pcStep = s; _pcRefused = null; }
                SlipAnchor(r, "step/" + s);
            }
            float y = inner.y + headH + StatStep(6f);
            y = PcPlateHead(new Rect(inner.x, y, inner.width, 0f), PartyCreationFlow.StepHeads[_pcStep]);

            // ── the foot: the way back, the way on ──
            float footH = Mathf.Max(_neutralActionButtonStyle.fixedHeight, _neutralActionButtonStyle.CalcSize(new GUIContent("BACK")).y);
            var foot = new Rect(inner.x, inner.yMax - footH, inner.width, footH);
            var body = new Rect(inner.x, y + StatStep(8f), inner.width, Mathf.Max(1f, foot.y - StatStep(10f) - y - StatStep(8f)));

            switch (_pcStep)
            {
                case 0: DrawPcProfile(body, d); break;
                case 1: DrawPcPlacement(body, d); break;
                case 2: DrawPcDeclarations(body, d); break;
                case 3: DrawPcLeader(body, d); break;
                default: DrawPcReview(body, d); break;
            }

            string backLabel = _pcStep == 0 ? "BACK TO THE PARTIES" : "BACK";
            float backW = Mathf.Ceil(_neutralActionButtonStyle.CalcSize(new GUIContent(backLabel)).x) + StatStep(24f);
            if (PoliSimWidgets.Button(new Rect(foot.x, foot.y, backW, footH), backLabel, _neutralActionButtonStyle))
            {
                if (_pcStep == 0) { ClosePartyCreation(); return; }
                _pcStep--; _pcRefused = null;
            }
            bool last = _pcStep == PartyCreationFlow.StepCount - 1;
            string nextLabel = last ? "START" : "NEXT";
            GUIStyle nextStyle = last ? _implementButtonStyle : _neutralActionButtonStyle;
            float nextW = Mathf.Ceil(nextStyle.CalcSize(new GUIContent(nextLabel)).x) + StatStep(24f);
            var nextRect = new Rect(foot.xMax - nextW, foot.y, nextW, footH);
            string missing = PartyCreationFlow.Missing(d);
            if (last && (missing != null || _pcRefused != null))
            {
                // the refusal as the registry's BAD glyph left of START, its words on the slip
                var badRect = new Rect(nextRect.x - footH - StatStep(8f), foot.y, footH, footH);
                SymbolRegistry.Draw(badRect, Symbol.Bad, PoliSimTheme.Bad, DeskCaption(8.5f, PoliSimTheme.Bad));
                _pcBook.Anchors["refused"] = new SlipContent("NOT YET").Add(_pcRefused ?? ("The " + missing + " is not set."));
                SlipAnchor(badRect, "refused");
            }
            if (PoliSimWidgets.Button(nextRect, nextLabel, nextStyle))
            {
                if (last) { CommitPartyCreation(); return; }
                _pcStep++; _pcRefused = null;
            }

            DrawSlips(_pcBook, new Rect(0f, 0f, UiScreen.Width, UiScreen.Height));
        }

        private static float StatStep(float boardPx) => StatsUnit(boardPx);

        /// <summary>A plate head (the settings sheet's grammar) at absolute position; returns the y under it.</summary>
        private float PcPlateHead(Rect at, string text)
        {
            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextSecondary, bold: true);
            float textH = Mathf.Ceil(DeskCaptionHeight(head));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(at.x, at.y, at.width, textH + StatStep(2f)), text, head);
                PoliSimTheme.Rule(new Rect(at.x, at.y + textH + StatStep(4f), at.width, 1f), PoliSimTheme.Hairline);
            }
            return at.y + textH + StatStep(8f);
        }

        private void PcFrame(Rect r, Color ink, float w = 1f)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - w, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.y, w, r.height), ink);
            PoliSimTheme.Rule(new Rect(r.xMax - w, r.y, w, r.height), ink);
        }

        private static Color PcInk(CreatedParty d) => !string.IsNullOrEmpty(d.InkHex) && ColorUtility.TryParseHtmlString(d.InkHex, out Color c) ? c : PoliSimTheme.TextMuted;

        /// <summary>A mark, square in the rect: a real party's tinted in its ink (the chamber's way of drawing a mark); a created party's CELL as
        /// authored, with its ink as a rule beneath - ⚠ the cells are authored in slate (95,102,114 measured), not white, so a tint multiplies
        /// them toward black and every ink reads alike (the first real film, s676_1280). Put to Design in D-CP.</summary>
        private static void PcMark(Rect r, string stem, Color ink)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            Texture2D t = IconLibrary.GetPartyMark(stem);
            if (t == null) { return; }
            bool cell = stem.StartsWith("mark_cell_", System.StringComparison.Ordinal);
            float rule = cell ? Mathf.Max(2f, Mathf.Round(r.height * 0.1f)) : 0f;
            Color previous = GUI.color;
            GUI.color = cell ? Color.white : ink;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height - rule - (cell ? 2f : 0f)), t, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
            if (cell) { PoliSimTheme.Rule(new Rect(r.x + r.width * 0.15f, r.yMax - rule, r.width * 0.7f, rule), ink); }
        }

        /// <summary>An invisible hit area over something drawn - a mark, a swatch, a card.</summary>
        private static bool PcHit(Rect r) => PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none);

        /// <summary>One pip row: five squares, the first <paramref name="pips"/> filled.</summary>
        private void PcPips(Rect r, int pips)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float side = Mathf.Min(r.height, (r.width - 4f * StatStep(3f)) / 5f);
            for (int i = 0; i < 5; i++)
            {
                var dot = new Rect(r.x + i * (side + StatStep(3f)), r.y + (r.height - side) * 0.5f, side, side);
                if (i < pips) { PoliSimTheme.Rule(dot, PoliSimTheme.TextPrimary); }
                else { PcFrame(dot, PoliSimTheme.Hairline); }
            }
        }

        private string PcField(Rect r, string caption, string value, int max)
        {
            GUIStyle cap = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float capH = Mathf.Ceil(DeskCaptionHeight(cap));
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, capH), caption, cap); }
            return GUI.TextField(new Rect(r.x, r.y + capH + StatStep(2f), r.width, r.height - capH - StatStep(2f)), value ?? string.Empty, max, UiPalette.BuildTextFieldStyle(_labelStyle.fontSize));
        }

        // ── step 1: profile ─────────────────────────────────────────────────────────────────────────────

        private void DrawPcProfile(Rect body, CreatedParty d)
        {
            float gutter = StatStep(24f);
            float leftW = Mathf.Floor(body.width * 0.46f);
            var left = new Rect(body.x, body.y, leftW, body.height);
            var right = new Rect(body.x + leftW + gutter, body.y, body.width - leftW - gutter, body.height);

            // names and the key
            float fieldH = StatStep(48f);
            float nameW = Mathf.Floor(left.width * 0.56f);
            d.Name = PcField(new Rect(left.x, left.y, nameW, fieldH), "NAME", d.Name, 40);
            float shortW = Mathf.Floor(left.width * 0.22f);
            string shortName = PcField(new Rect(left.x + nameW + StatStep(10f), left.y, shortW, fieldH), "SHORT", d.ShortName, 12);
            if (shortName != d.ShortName) { d.ShortName = shortName; d.Key = PartyCreationFlow.KeyFrom(d.Country, shortName); }
            var keyRect = new Rect(left.x + nameW + shortW + StatStep(20f), left.y + fieldH - StatStep(26f), Mathf.Max(1f, left.xMax - (left.x + nameW + shortW + StatStep(20f))), StatStep(26f));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(keyRect, string.IsNullOrEmpty(d.Key) ? "—" : d.Key, DeskCaption(10f, PoliSimTheme.TextPrimary, bold: true));
            }
            SlipAnchor(keyRect, "key");

            // the mark: the 35 unspent cells, in the chosen ink
            float y = PcPlateHead(new Rect(left.x, left.y + fieldH + StatStep(12f), left.width, 0f), "MARK");
            List<string> cells = PartyCreationFlow.UnspentCells();
            const int columns = 7;
            int rows = Mathf.CeilToInt(cells.Count / (float)columns);
            float gap = StatStep(4f);
            float inkBlock = StatStep(16f) + 2f * StatStep(30f);
            float side = Mathf.Floor(Mathf.Min((left.width - (columns - 1) * gap) / columns, (left.yMax - y - inkBlock - StatStep(24f) - (rows - 1) * gap) / rows));
            Color ink = PcInk(d);
            for (int i = 0; i < cells.Count; i++)
            {
                var r = new Rect(left.x + (i % columns) * (side + gap), y + (i / columns) * (side + gap), side, side);
                PcMark(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), cells[i], ink);
                if (cells[i] == d.MarkStem) { PcFrame(r, PoliSimTheme.BrassBorder, 2f); }
                if (PcHit(r)) { d.MarkStem = cells[i]; }
                SlipAnchor(r, "mark/" + cells[i]);
            }

            // the ink: the ring, a refused step crossed by the registry's BAN glyph
            y = PcPlateHead(new Rect(left.x, y + rows * (side + gap) + StatStep(6f), left.width, 0f), "INK");
            int perRow = PoliSimTheme.CreatedInkSteps / 2;
            float sw = Mathf.Floor(Mathf.Min((left.width - (perRow - 1) * gap) / perRow, StatStep(30f)));
            for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++)
            {
                var r = new Rect(left.x + (step % perRow) * (sw + gap), y + (step / perRow) * (sw + gap), sw, sw);
                Color candidate = PoliSimTheme.CreatedInkCandidate(step);
                bool clear = PartyCreationFlow.InkClear(d.Country, step, out string _, out float _);
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(r, candidate); }
                if (!clear) { SymbolRegistry.Draw(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), Symbol.Ban, PoliSimTheme.Tile, DeskCaption(7f, PoliSimTheme.Tile)); }
                if (PartyCreationFlow.HexOf(candidate) == d.InkHex) { PcFrame(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), PoliSimTheme.TextPrimary, 2f); }
                if (PcHit(r) && clear) { d.InkHex = PartyCreationFlow.HexOf(candidate); }
                SlipAnchor(r, "ink/" + step);
            }

            // the origin: six cards, each its five stats as pips (the stats' names and meanings on the card's slip)
            y = PcPlateHead(new Rect(right.x, right.y, right.width, 0f), "ORIGIN");
            float cardGap = StatStep(10f);
            float cardW = Mathf.Floor((right.width - 2f * cardGap) / 3f);
            float cardH = Mathf.Floor(Mathf.Min(StatStep(128f), (right.height * 0.62f - cardGap) / 2f));
            GUIStyle title = DeskCaption(8.5f, PoliSimTheme.TextPrimary, bold: true);
            float titleH = Mathf.Ceil(DeskCaptionHeight(title));
            for (int i = 0; i < PartyOrigins.All.Length; i++)
            {
                PartyOrigins.Preset p = PartyOrigins.All[i];
                var card = new Rect(right.x + (i % 3) * (cardW + cardGap), y + (i / 3) * (cardH + cardGap), cardW, cardH);
                bool chosen = d.Origin == p.Origin;
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.RoundedCard(card, PoliSimTheme.Tile, chosen ? PoliSimTheme.BrassBorder : PoliSimTheme.Hairline, 0f, chosen ? 2f : 1f);
                    PoliSimWidgets.MeasuredLabel(new Rect(card.x + StatStep(8f), card.y + StatStep(6f), card.width - StatStep(16f), titleH), p.Title.ToUpperInvariant(), title);
                }
                float pipTop = card.y + StatStep(10f) + titleH;
                float pipH = Mathf.Floor((card.yMax - StatStep(8f) - pipTop) / 5f);
                for (int stat = 0; stat < 5; stat++)
                {
                    PcPips(new Rect(card.x + StatStep(8f), pipTop + stat * pipH + 1f, Mathf.Min(card.width - StatStep(16f), (pipH - 3f) * 5f + 4f * StatStep(3f)), pipH - 3f), PartyCreationFlow.StatOf(p, stat));
                }
                if (PcHit(card) && !chosen) { PartyCreationFlow.ChooseOrigin(d, p.Origin); _pcPredictedFor = null; }
                SlipAnchor(card, "origin/" + p.Origin);
            }

            // the origin's own choice
            float cy = y + 2f * (cardH + cardGap) + StatStep(4f);
            var choice = new Rect(right.x, cy, right.width, Mathf.Max(1f, right.yMax - cy));
            switch (d.Origin)
            {
                case PartyOrigin.Splinter: DrawPcSplinter(choice, d); break;
                case PartyOrigin.SingleIssue: DrawPcIssue(choice, d); break;
                case PartyOrigin.Regional: DrawPcRegion(choice, d); break;
            }
        }

        private void DrawPcSplinter(Rect r, CreatedParty d)
        {
            float y = PcPlateHead(new Rect(r.x, r.y, r.width, 0f), "PARENT");
            PoliticalParty[] real = PartySystems.RealRoster(d.Country);
            float side = Mathf.Floor(Mathf.Min(StatStep(34f), (r.width - (real.Length - 1) * StatStep(6f)) / real.Length));
            for (int i = 0; i < real.Length; i++)
            {
                var m = new Rect(r.x + i * (side + StatStep(6f)), y, side, side);
                PcMark(m, real[i].MarkName, PoliSimTheme.PartyLaddered(d.Country, real[i].Abbrev));
                if (real[i].Abbrev == d.ParentKey) { PcFrame(new Rect(m.x - 2f, m.y - 2f, m.width + 4f, m.height + 4f), PoliSimTheme.BrassBorder, 2f); }
                if (PcHit(m)) { PartyCreationFlow.ChooseParent(d, real[i].Abbrev); _pcPredictedFor = null; }
                SlipAnchor(m, "parent/" + real[i].Abbrev);
            }
            float x = r.x;
            float sy = y + side + StatStep(10f);
            GUIStyle chip = SettingsChipCaption();
            float h = SettingsChipHeight(chip);
            foreach (double slice in PartyCreationFlow.Slices)
            {
                string label = (slice * 100.0).ToString("0", CultureInfo.InvariantCulture) + " %";
                float w = Mathf.Ceil(chip.CalcSize(new GUIContent(label)).x) + StatStep(16f);
                var c = new Rect(x, sy, w, h);
                if (DrawDeskChipButton(c, label, chip, selected: System.Math.Abs(d.InheritedSlice - slice) < 1e-9, disabled: false)) { d.InheritedSlice = slice; _pcPredictedFor = null; }
                SlipAnchor(c, "slice/" + slice.ToString("0.00", CultureInfo.InvariantCulture));
                x += w - 1f;
            }
        }

        private void DrawPcIssue(Rect r, CreatedParty d)
        {
            float y = PcPlateHead(new Rect(r.x, r.y, r.width, 0f), "ISSUE");
            int n = PartyCreationFlow.Issues.Length;
            float side = Mathf.Floor(Mathf.Min(StatStep(34f), (r.width - (n - 1) * StatStep(6f)) / n));
            for (int i = 0; i < n; i++)
            {
                UiPalette.SystemArea area = PartyCreationFlow.Issues[i];
                var m = new Rect(r.x + i * (side + StatStep(6f)), y, side, side);
                bool chosen = d.Issue == area.ToString();
                Texture2D t = IconLibrary.GetAreaIcon(area);
                if (t != null && Event.current.type == EventType.Repaint)
                {
                    Color previous = GUI.color;
                    GUI.color = chosen ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;   // plain ink - never an area accent beside a party ink
                    GUI.DrawTexture(new Rect(m.x + 3f, m.y + 3f, m.width - 6f, m.height - 6f), t, ScaleMode.ScaleToFit, true);
                    GUI.color = previous;
                }
                if (chosen) { PcFrame(m, PoliSimTheme.BrassBorder, 2f); }
                if (PcHit(m)) { d.Issue = area.ToString(); }
                SlipAnchor(m, "issue/" + area);
            }
        }

        private void DrawPcRegion(Rect r, CreatedParty d)
        {
            float y = PcPlateHead(new Rect(r.x, r.y, r.width, 0f), "REGION");
            GUIStyle chip = DeskCaption(7.5f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
            float h = Mathf.Ceil(DeskCaptionHeight(chip)) + StatStep(5f);
            float x = r.x;
            foreach (string name in PartyCreationFlow.Regions(d.Country))
            {
                float w = Mathf.Ceil(chip.CalcSize(new GUIContent(name)).x) + StatStep(10f);
                if (x + w > r.xMax) { x = r.x; y += h + StatStep(3f); }
                if (y + h > r.yMax) { break; }
                if (DrawDeskChipButton(new Rect(x, y, w, h), name, chip, selected: d.Region == name, disabled: false)) { d.Region = name; }
                x += w + StatStep(3f);
            }
        }

        // ── step 2: placement ───────────────────────────────────────────────────────────────────────────

        /// <summary>The compass: economic left-right across, GAL-TAN up (0-10, CHES), the real parties' marks in their inks, the electorate's centre
        /// as a cross, the draft's mark where it stands. Returns the plot square.</summary>
        private Rect DrawPcCompass(Rect square, CreatedParty d, bool editable)
        {
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.RoundedCard(square, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f); }
            float inset = StatStep(14f);
            var plot = new Rect(square.x + inset, square.y + inset, square.width - inset * 2f, square.height - inset * 2f);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(plot.x, plot.center.y, plot.width, 1f), PoliSimTheme.Hairline);
                PoliSimTheme.Rule(new Rect(plot.center.x, plot.y, 1f, plot.height), PoliSimTheme.Hairline);
            }
            Vector2 At(float econ, float galtan) => new Vector2(plot.x + econ / 10f * plot.width, plot.yMax - galtan / 10f * plot.height);

            float markSide = Mathf.Floor(Mathf.Clamp(plot.width * 0.07f, StatStep(12f), StatStep(26f)));
            if (PartySystems.TryElectorate(d.Country, out VoteModel.Electorate e, out double _) && Event.current.type == EventType.Repaint)
            {
                Vector2 c = At((float)e.MuEcon, (float)e.MuSoc);
                float arm = markSide * 0.5f;
                PoliSimTheme.Rule(new Rect(c.x - arm, c.y, arm * 2f, 2f), PoliSimTheme.TextSecondary);
                PoliSimTheme.Rule(new Rect(c.x, c.y - arm, 2f, arm * 2f), PoliSimTheme.TextSecondary);
            }
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                if (!p.HasPosition) { continue; }
                Vector2 c = At(p.LrEcon, p.Galtan);
                var m = new Rect(c.x - markSide * 0.5f, c.y - markSide * 0.5f, markSide, markSide);
                PcMark(m, p.MarkName, PoliSimTheme.PartyLaddered(d.Country, p.Abbrev));
                SlipAnchor(m, "party/" + p.Abbrev);
            }
            float mine = Mathf.Floor(markSide * 1.35f);
            Vector2 me = At(d.LrEcon, d.Galtan);
            var own = new Rect(me.x - mine * 0.5f, me.y - mine * 0.5f, mine, mine);
            PcMark(own, d.MarkStem, PcInk(d));
            PcFrame(new Rect(own.x - 2f, own.y - 2f, own.width + 4f, own.height + 4f), PoliSimTheme.TextPrimary, 1f);

            if (editable)
            {
                Event ev = Event.current;
                if (ev.type == EventType.MouseDown && ev.button == 0 && square.Contains(ev.mousePosition)) { _pcDragging = true; }
                if (_pcDragging && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag))
                {
                    d.LrEcon = Mathf.Round(Mathf.Clamp01((ev.mousePosition.x - plot.x) / plot.width) * 100f) / 10f;
                    d.Galtan = Mathf.Round(Mathf.Clamp01((plot.yMax - ev.mousePosition.y) / plot.height) * 100f) / 10f;
                    ev.Use();
                }
                if (ev.rawType == EventType.MouseUp) { _pcDragging = false; }
            }
            SlipAnchor(square, "compass");
            return plot;
        }

        private void DrawPcPlacement(Rect body, CreatedParty d)
        {
            float side = Mathf.Floor(Mathf.Min(body.height, body.width * 0.58f));
            DrawPcCompass(new Rect(body.x, body.y, side, side), d, editable: true);
            var right = new Rect(body.x + side + StatStep(28f), body.y, Mathf.Max(1f, body.width - side - StatStep(28f)), body.height);
            DrawPcPrediction(right, d);
        }

        /// <summary>The model's idle prediction where the draft stands, as one large figure (its meaning on the slip); recomputed when the placement
        /// settles, never mid-drag.</summary>
        private float DrawPcPrediction(Rect r, CreatedParty d)
        {
            string at = string.Format(CultureInfo.InvariantCulture, "{0:0.0}/{1:0.0}/{2}/{3}/{4:0.00}", d.LrEcon, d.Galtan, d.Origin, d.ParentKey, d.InheritedSlice);
            if (!_pcDragging && at != _pcPredictedFor && Event.current.type == EventType.Layout)
            {
                _pcPredictedFor = at;
                _pcPredicted = PartyCreationFlow.PredictedShare(d);
            }
            GUIStyle figure = DeskBody(30f, PoliSimTheme.TextPrimary);
            string text = double.IsNaN(_pcPredicted) ? "—" : _pcPredicted.ToString("0.0", CultureInfo.InvariantCulture) + " %";
            float h = Mathf.Ceil(figure.CalcHeight(new GUIContent(text), r.width));
            float w = Mathf.Ceil(figure.CalcSize(new GUIContent(text)).x);
            var fr = new Rect(r.x, r.y, Mathf.Min(r.width, w), h);
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(fr, text, figure); }
            SlipAnchor(fr, "predicted");
            return r.y + h;
        }

        // ── step 3: declarations ────────────────────────────────────────────────────────────────────────

        private void DrawPcDeclarations(Rect body, CreatedParty d)
        {
            PoliticalParty[] real = PartySystems.RealRoster(d.Country);
            float gutter = StatStep(28f);
            float leftW = Mathf.Floor(body.width * 0.42f);
            float y = PcPlateHead(new Rect(body.x, body.y, leftW, 0f), "RED LINES");
            float rowH = Mathf.Floor(Mathf.Min(StatStep(34f), (body.yMax - y) / real.Length));
            GUIStyle abbrev = DeskCaption(9f, PoliSimTheme.TextPrimary, bold: true);
            for (int i = 0; i < real.Length; i++)
            {
                PoliticalParty p = real[i];
                float ry = y + i * rowH;
                float side = rowH - StatStep(6f);
                var m = new Rect(body.x, ry + StatStep(3f), side, side);
                PcMark(m, p.MarkName, PoliSimTheme.PartyLaddered(d.Country, p.Abbrev));
                SlipAnchor(m, "party/" + p.Abbrev);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(m.xMax + StatStep(8f), ry, StatStep(40f), rowH), p.Abbrev, abbrev); }
                var cell = new Rect(m.xMax + StatStep(52f), ry + StatStep(3f), side, side);
                bool red = d.RedLinesAgainst.Contains(p.Abbrev), oneWay = d.OneWayAgainst.Contains(p.Abbrev);
                PcFrame(cell, red || oneWay ? PoliSimTheme.TextPrimary : PoliSimTheme.Hairline);
                if (red) { SymbolRegistry.Draw(new Rect(cell.x + 3f, cell.y + 3f, cell.width - 6f, cell.height - 6f), Symbol.Ban, PoliSimTheme.Bad, DeskCaption(7f, PoliSimTheme.Bad)); }
                else if (oneWay) { SymbolRegistry.Draw(new Rect(cell.x + 3f, cell.y + 3f, cell.width - 6f, cell.height - 6f), Symbol.Locked, PoliSimTheme.TextPrimary, DeskCaption(7f, PoliSimTheme.TextPrimary)); }
                if (PcHit(cell))
                {
                    // none -> red line -> one way -> none
                    if (red) { d.RedLinesAgainst.Remove(p.Abbrev); d.OneWayAgainst.Add(p.Abbrev); }
                    else if (oneWay) { d.OneWayAgainst.Remove(p.Abbrev); }
                    else { d.RedLinesAgainst.Add(p.Abbrev); }
                }
                SlipAnchor(cell, "line/" + p.Abbrev);
            }

            var right = new Rect(body.x + leftW + gutter, body.y, body.width - leftW - gutter, body.height);
            float ry2 = PcPlateHead(new Rect(right.x, right.y, right.width, 0f), "CANDIDACY");
            float ms = Mathf.Floor(Mathf.Min(StatStep(34f), (right.width - real.Length * StatStep(8f)) / (real.Length + 2)));
            float x = right.x;
            // its own leader first, then each real party's, then none
            var options = new List<(string Key, string Mark, Color Ink)> { (d.Key ?? string.Empty, d.MarkStem, PcInk(d)) };
            foreach (PoliticalParty p in real) { options.Add((p.Abbrev, p.MarkName, PoliSimTheme.PartyLaddered(d.Country, p.Abbrev))); }
            foreach ((string key, string mark, Color ink) in options)
            {
                var m = new Rect(x, ry2, ms, ms);
                PcMark(m, mark, ink);
                bool backed = !string.IsNullOrEmpty(d.BacksCandidateOf) && d.BacksCandidateOf == key;
                if (backed) { SymbolRegistry.Draw(new Rect(m.x + ms * 0.25f, m.yMax + StatStep(3f), ms * 0.5f, ms * 0.5f), Symbol.Good, PoliSimTheme.Good, DeskCaption(7f, PoliSimTheme.Good)); }
                if (PcHit(m)) { d.BacksCandidateOf = backed ? null : key; }
                SlipAnchor(m, "candidate");
                x += ms + StatStep(8f);
            }

            float oy = PcPlateHead(new Rect(right.x, ry2 + ms * 1.6f + StatStep(10f), right.width, 0f), "OUTSIDE THE CABINET");
            float cs = Mathf.Floor(StatStep(34f));
            var yes = new Rect(right.x, oy, cs, cs);
            var no = new Rect(right.x + cs + StatStep(10f), oy, cs, cs);
            PcFrame(yes, !d.InOrAgainst ? PoliSimTheme.BrassBorder : PoliSimTheme.Hairline, !d.InOrAgainst ? 2f : 1f);
            PcFrame(no, d.InOrAgainst ? PoliSimTheme.BrassBorder : PoliSimTheme.Hairline, d.InOrAgainst ? 2f : 1f);
            SymbolRegistry.Draw(new Rect(yes.x + 4f, yes.y + 4f, yes.width - 8f, yes.height - 8f), Symbol.Allow, PoliSimTheme.TextPrimary, DeskCaption(7f, PoliSimTheme.TextPrimary));
            SymbolRegistry.Draw(new Rect(no.x + 4f, no.y + 4f, no.width - 8f, no.height - 8f), Symbol.Ban, PoliSimTheme.TextPrimary, DeskCaption(7f, PoliSimTheme.TextPrimary));
            if (PcHit(yes)) { d.InOrAgainst = false; }
            if (PcHit(no)) { d.InOrAgainst = true; }
            SlipAnchor(yes, "outside/yes");
            SlipAnchor(no, "outside/no");
        }

        // ── step 4: leader ──────────────────────────────────────────────────────────────────────────────

        private void DrawPcLeader(Rect body, CreatedParty d)
        {
            float w = Mathf.Floor(body.width * 0.46f);
            float fieldH = StatStep(48f);
            d.LeaderName = PcField(new Rect(body.x, body.y, w, fieldH), "LEADER", d.LeaderName, 40);
            float y = PcPlateHead(new Rect(body.x, body.y + fieldH + StatStep(14f), w, 0f), "STATS");
            DrawPcStats(new Rect(body.x, y, w, body.yMax - y), d);
        }

        /// <summary>The five stats as pip rows, each named in one word (its meaning on the slip).</summary>
        private float DrawPcStats(Rect r, CreatedParty d)
        {
            GUIStyle name = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float lane = 0f;
            for (int s = 0; s < 5; s++) { lane = Mathf.Max(lane, name.CalcSize(new GUIContent(PartyCreationFlow.StatName(s).ToUpperInvariant())).x); }
            lane = Mathf.Ceil(lane) + StatStep(10f);
            float rowH = Mathf.Floor(Mathf.Min(StatStep(22f), r.height / 5f));
            for (int s = 0; s < 5; s++)
            {
                var row = new Rect(r.x, r.y + s * rowH, Mathf.Min(r.width, lane + (rowH - 6f) * 5f + 4f * StatStep(3f)), rowH);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, lane, rowH), PartyCreationFlow.StatName(s).ToUpperInvariant(), name); }
                PcPips(new Rect(row.x + lane, row.y + 3f, row.width - lane, rowH - 6f), PartyCreationFlow.StatOf(d, s));
                SlipAnchor(row, "stat/" + s);
            }
            return r.y + 5f * rowH;
        }

        // ── step 5: review ──────────────────────────────────────────────────────────────────────────────

        private void DrawPcReview(Rect body, CreatedParty d)
        {
            float markSide = Mathf.Floor(Mathf.Min(StatStep(96f), body.height * 0.3f));
            var mark = new Rect(body.x, body.y, markSide, markSide);
            PcMark(mark, d.MarkStem, PcInk(d));
            SlipAnchor(mark, "mark/" + d.MarkStem);
            float tx = mark.xMax + StatStep(16f);
            float colW = Mathf.Floor(body.width * 0.42f) - markSide - StatStep(16f);
            GUIStyle nameStyle = DeskBody(18f, PoliSimTheme.TextPrimary);
            GUIStyle cap = DeskCaption(9f, PoliSimTheme.TextSecondary);
            float nh = Mathf.Ceil(nameStyle.CalcHeight(new GUIContent("Ag"), colW));
            float ch = Mathf.Ceil(DeskCaptionHeight(cap));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(tx, body.y, colW, nh), string.IsNullOrEmpty(d.Name) ? "—" : d.Name, nameStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(tx, body.y + nh + StatStep(4f), colW, ch), (string.IsNullOrEmpty(d.ShortName) ? "—" : d.ShortName) + " · " + (string.IsNullOrEmpty(d.Key) ? "—" : d.Key), cap);
                PoliSimWidgets.MeasuredLabel(new Rect(tx, body.y + nh + ch + StatStep(8f), colW, ch), PartyOrigins.Of(d.Origin).Title.ToUpperInvariant() + (string.IsNullOrEmpty(d.LeaderName) ? string.Empty : " · " + d.LeaderName), cap);
            }
            SlipAnchor(new Rect(tx, body.y + nh + ch + StatStep(8f), colW, ch), "origin/" + d.Origin);
            float y = PcPlateHead(new Rect(body.x, mark.yMax + StatStep(14f), Mathf.Floor(body.width * 0.42f), 0f), "STATS");
            y = DrawPcStats(new Rect(body.x, y, Mathf.Floor(body.width * 0.42f), body.yMax - y), d);

            // the declarations as marks under their glyphs
            float dy = PcPlateHead(new Rect(body.x, y + StatStep(12f), Mathf.Floor(body.width * 0.42f), 0f), "DECLARATIONS");
            float ds = Mathf.Floor(StatStep(24f));
            float dx = body.x;
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                bool red = d.RedLinesAgainst.Contains(p.Abbrev), oneWay = d.OneWayAgainst.Contains(p.Abbrev);
                if (!red && !oneWay) { continue; }
                var m = new Rect(dx, dy, ds, ds);
                PcMark(m, p.MarkName, PoliSimTheme.PartyLaddered(d.Country, p.Abbrev));
                SymbolRegistry.Draw(new Rect(dx + ds * 0.2f, dy + ds + StatStep(2f), ds * 0.6f, ds * 0.6f), red ? Symbol.Ban : Symbol.Locked, red ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary, DeskCaption(7f, PoliSimTheme.TextPrimary));
                SlipAnchor(m, "line/" + p.Abbrev);
                dx += ds + StatStep(8f);
            }
            var outside = new Rect(dx + StatStep(8f), dy, ds, ds);
            SymbolRegistry.Draw(outside, d.InOrAgainst ? Symbol.Ban : Symbol.Allow, PoliSimTheme.TextPrimary, DeskCaption(7f, PoliSimTheme.TextPrimary));
            SlipAnchor(outside, d.InOrAgainst ? "outside/no" : "outside/yes");

            // where it stands, and what the model makes of it
            float cx = body.x + Mathf.Floor(body.width * 0.42f) + StatStep(28f);
            float side = Mathf.Floor(Mathf.Min(body.height * 0.72f, body.xMax - cx));
            DrawPcCompass(new Rect(cx, body.y, side, side), d, editable: false);
            DrawPcPrediction(new Rect(cx, body.y + side + StatStep(8f), side, Mathf.Max(1f, body.yMax - body.y - side - StatStep(8f))), d);
        }
    }
}
