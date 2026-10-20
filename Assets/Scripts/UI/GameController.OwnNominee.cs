using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-33 (SP-6a): **THE CREATION FLOW'S US FORM, DRAWN** - four steps (ticket, placement, nominee, review) as the creation flow's own screen swap before
    /// the game, opened from the Canvas party panel's YOUR OWN NOMINEE and closed back to it through the same seam (`RequestPartyCreation`, `OpenPartyCreation`,
    /// `ClosePartyCreation`). Built structurally and icon-first, the creation flow's way: the choices are the parties' own marks, a compass and one field; the words
    /// are on slips read from `OwnNomineeFlow.Slips`, the book `PartyCreationCheck` reads. START registers the nominee and seats the player as its party; the world is
    /// rebuilt, so every poll it reads places the party at the nominee's point. Its board is asked of Design later, against the built flow (the plan's §5).
    /// ⚠ Drawn with absolute rects throughout - no GUILayout, so no Layout-pass dummy rect keys a control.
    /// </summary>
    public partial class GameController
    {
        private OwnNominee _onDraft;
        private int _onStep;
        private int _onStepShown = -1;
        private string _onRefused;
        private PeopleSlips.Book _onBook;
        private bool _onDragging;
        private string _onPredictedFor;
        private double _onPredicted = double.NaN;
        private double _onAtParty = double.NaN;
        private string _onBriefFor;
        private List<StartBrief.Row> _onBrief;

        /// <summary>Opens the US form on a fresh draft - the creation flow's entry dispatches here where the country offers an own nominee; also the film's entry.</summary>
        private void OpenOwnNominee(CountryId country)
        {
            _partyCreationCountry = country;
            _onDraft = OwnNomineeFlow.NewDraft(country);
            _onStep = 0;
            _onRefused = null;
            _onPredictedFor = null;
            _onBriefFor = null;
            _partyCreationOpen = true;
        }

        /// <summary>START: the nominee registered, the world rebuilt on it, the player seated as its party.</summary>
        private void CommitOwnNominee()
        {
            if (!OwnNomineeFlow.TryCommit(_onDraft, out _onRefused)) { Debug.Log("OWN NOMINEE: refused - " + _onRefused); return; }
            CountryId country = _onDraft.Country;
            string party = _onDraft.Party;
            Debug.Log($"OWN NOMINEE: '{_onDraft.Name}' registered at the head of the {party} ticket for {country}, at ({_onDraft.LrEcon:0.0}, {_onDraft.Galtan:0.0}).");
            ClosePartyCreation();
            _worldRosterStale = true;   // the world is rebuilt on the start whatever its epoch: every poll it reads from day one places the party at the nominee's point
            SelectPlayerCountryAndParty(country, party);
        }

        /// <summary>The two predictions the placement and the review draw - recomputed when the ticket or the point settles, never mid-drag.</summary>
        private void UpdateOwnNomineePrediction(OwnNominee d)
        {
            string at = string.Format(CultureInfo.InvariantCulture, "{0}/{1:0.000}/{2:0.000}", d.Party, d.LrEcon, d.Galtan);
            if (_onDragging || at == _onPredictedFor || Event.current.type != EventType.Layout) { return; }
            _onPredictedFor = at;
            _onPredicted = OwnNomineeFlow.PredictedShare(d);
            _onAtParty = OwnNomineeFlow.PredictedShare(d, atPartyPoint: true);
        }

        private void DrawOwnNomineeScreen()
        {
            DrawMenuBackground();
            if (_onDraft == null) { OpenOwnNominee(_partyCreationCountry); }
            OwnNominee d = _onDraft;
            UpdateOwnNomineePrediction(d);
            if (Event.current.type == EventType.Layout || _onBook == null) { _onBook = OwnNomineeFlow.Slips(d, _onPredicted, _onAtParty); }
            BeginSlipAnchors();
            if (_onStep != _onStepShown) { if (_onStepShown >= 0) { _slipPins.Clear(); } _onStepShown = _onStep; }   // a pinned slip belongs to its step's anchors

            float marginX = UiScreen.Width * ScreenMarginFraction;
            float marginY = UiScreen.Height * ScreenMarginFraction;
            var area = new Rect(marginX, marginY, UiScreen.Width - marginX * 2f, UiScreen.Height - marginY * 2f);
            if (Event.current.type == EventType.Repaint) { SheetAt(area.y).Draw(area, GUIContent.none, false, false, false, false); }   // §680 (PF-17): the cropped sheet at the window's top edge
            float pad = StatStep(18f);
            var inner = new Rect(area.x + pad, area.y + pad, area.width - pad * 2f, area.height - pad * 2f);

            // ── the head: the title and the four step chips (numerals; each step's words on its slip) ──
            const string title = "YOUR OWN NOMINEE";
            float headH = Mathf.Ceil(_headerStyle.CalcHeight(new GUIContent(title), inner.width));
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y, inner.width * 0.5f, headH), title, _headerStyle); }
            GUIStyle chip = SettingsChipCaption();
            float chipH = SettingsChipHeight(chip);
            float chipW = chipH * 1.6f;
            float cx = inner.xMax - OwnNomineeFlow.StepCount * (chipW - 1f);
            for (int s = 0; s < OwnNomineeFlow.StepCount; s++)
            {
                var r = new Rect(cx + s * (chipW - 1f), inner.y + Mathf.Round((headH - chipH) * 0.5f), chipW, chipH);
                if (DrawDeskChipButton(r, (s + 1).ToString(CultureInfo.InvariantCulture), chip, selected: s == _onStep, disabled: false)) { _onStep = s; _onRefused = null; }
                SlipAnchor(r, "step/" + s);
            }
            float y = inner.y + headH + StatStep(6f);
            y = PcPlateHead(new Rect(inner.x, y, inner.width, 0f), OwnNomineeFlow.StepHeads[_onStep]);

            // ── the foot: the way back, the way on ──
            float footH = Mathf.Max(_neutralActionButtonStyle.fixedHeight, _neutralActionButtonStyle.CalcSize(new GUIContent("BACK")).y);
            var foot = new Rect(inner.x, inner.yMax - footH, inner.width, footH);
            var body = new Rect(inner.x, y + StatStep(8f), inner.width, Mathf.Max(1f, foot.y - StatStep(10f) - y - StatStep(8f)));

            switch (_onStep)
            {
                case 0: DrawOnTicket(body, d); break;
                case 1: DrawOnPlacement(body, d); break;
                case 2: DrawOnName(body, d); break;
                default: DrawOnReview(body, d); break;
            }

            string backLabel = _onStep == 0 ? "BACK TO THE PARTIES" : "BACK";
            float backW = Mathf.Ceil(_neutralActionButtonStyle.CalcSize(new GUIContent(backLabel)).x) + StatStep(24f);
            if (PoliSimWidgets.Button(new Rect(foot.x, foot.y, backW, footH), backLabel, _neutralActionButtonStyle))
            {
                if (_onStep == 0) { ClosePartyCreation(); return; }
                _onStep--; _onRefused = null;
            }
            bool last = _onStep == OwnNomineeFlow.StepCount - 1;
            string nextLabel = last ? "START" : "NEXT";
            GUIStyle nextStyle = last ? _implementButtonStyle : _neutralActionButtonStyle;
            float nextW = Mathf.Ceil(nextStyle.CalcSize(new GUIContent(nextLabel)).x) + StatStep(24f);
            var nextRect = new Rect(foot.xMax - nextW, foot.y, nextW, footH);
            string missing = OwnNomineeFlow.Missing(d);
            if (last && (missing != null || _onRefused != null))
            {
                // the refusal as the registry's BAD glyph left of START, its words on the slip
                var badRect = new Rect(nextRect.x - footH - StatStep(8f), foot.y, footH, footH);
                SymbolRegistry.Draw(badRect, Symbol.Bad, PoliSimTheme.Bad, DeskCaption(8.5f, PoliSimTheme.Bad));
                _onBook.Anchors["refused"] = new SlipContent("NOT YET").Add(_onRefused ?? ("The " + missing + " is not set."));
                SlipAnchor(badRect, "refused");
            }
            if (PoliSimWidgets.Button(nextRect, nextLabel, nextStyle))
            {
                if (last) { CommitOwnNominee(); return; }
                _onStep++; _onRefused = null;
            }

            DrawSlips(_onBook, new Rect(0f, 0f, UiScreen.Width, UiScreen.Height));
        }

        private string OnMark(CountryId country, string party)
        {
            foreach (PoliticalParty p in PartySystems.RealRoster(country)) { if (p.Abbrev == party) { return p.MarkName; } }
            return null;
        }

        // ── step 1: the ticket ──────────────────────────────────────────────────────────────────────────

        /// <summary>The tickets as cards, each the party's own mark in its ink over its short name; the chosen one framed in brass. The name, the range and what
        /// the choice keeps are on the card's slip.</summary>
        private void DrawOnTicket(Rect body, OwnNominee d)
        {
            List<string> tickets = OwnNominees.Tickets(d.Country);
            if (tickets.Count == 0) { return; }
            float gap = StatStep(24f);
            float cardW = Mathf.Floor(Mathf.Min(StatStep(240f), (body.width - (tickets.Count - 1) * gap) / tickets.Count));
            float cardH = Mathf.Floor(Mathf.Min(StatStep(240f), body.height * 0.8f));
            GUIStyle abbrev = DeskCaption(12f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleCenter);
            float abbrevH = Mathf.Ceil(DeskCaptionHeight(abbrev));
            for (int i = 0; i < tickets.Count; i++)
            {
                string party = tickets[i];
                var card = new Rect(body.x + i * (cardW + gap), body.y, cardW, cardH);
                bool chosen = d.Party == party;
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.RoundedCard(card, PoliSimTheme.Tile, chosen ? PoliSimTheme.BrassBorder : PoliSimTheme.Hairline, 0f, chosen ? 2f : 1f); }
                float side = Mathf.Floor(Mathf.Min(card.width * 0.6f, card.height - abbrevH - StatStep(40f)));
                string stem = OnMark(d.Country, party);
                if (!string.IsNullOrEmpty(stem)) { PcMark(new Rect(card.center.x - side * 0.5f, card.y + StatStep(16f), side, side), stem, PoliSimTheme.PartyLaddered(d.Country, party)); }
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(card.x + StatStep(8f), card.y + StatStep(16f) + side + StatStep(10f), card.width - StatStep(16f), abbrevH), PartySystems.ShortName(d.Country, party), abbrev);
                }
                if (PcHit(card) && !chosen) { OwnNomineeFlow.ChooseTicket(d, party); _onPredictedFor = null; _onBriefFor = null; }
                SlipAnchor(card, "ticket/" + party);
            }
        }

        // ── step 2: the placement ───────────────────────────────────────────────────────────────────────

        /// <summary>The creation flow's compass, in its US form: economic left-right across, GAL-TAN up (0-10, CHES), the parties' marks in their inks, the
        /// electorate's centre as a cross, the ticket's range as a frame in the party's ink, and the nominee - the party's mark, larger and framed - where it
        /// stands; a drag moves it, kept inside the frame (`OwnNomineeFlow.Place`). Returns the plot square.</summary>
        private Rect DrawOnCompass(Rect square, OwnNominee d, bool editable)
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

            Color ink = PoliSimTheme.PartyLaddered(d.Country, d.Party);
            if (OwnNominees.TryRangeOf(d.Country, d.Party, out OwnNominees.Range range))
            {
                Vector2 lo = At(range.EconLo, range.SocHi), hi = At(range.EconHi, range.SocLo);
                PcFrame(new Rect(lo.x, lo.y, Mathf.Max(2f, hi.x - lo.x), Mathf.Max(2f, hi.y - lo.y)), ink, 2f);
            }
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
            string stem = OnMark(d.Country, d.Party);
            if (!string.IsNullOrEmpty(stem)) { PcMark(own, stem, ink); }
            PcFrame(new Rect(own.x - 2f, own.y - 2f, own.width + 4f, own.height + 4f), PoliSimTheme.TextPrimary, 1f);

            if (editable)
            {
                Event ev = Event.current;
                if (ev.type == EventType.MouseDown && ev.button == 0 && square.Contains(ev.mousePosition)) { _onDragging = true; }
                if (_onDragging && (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag))
                {
                    OwnNomineeFlow.Place(d, Mathf.Clamp01((ev.mousePosition.x - plot.x) / plot.width) * 10f, Mathf.Clamp01((plot.yMax - ev.mousePosition.y) / plot.height) * 10f);
                    ev.Use();
                }
                if (ev.rawType == EventType.MouseUp) { _onDragging = false; }
            }
            SlipAnchor(square, "compass");
            return plot;
        }

        private void DrawOnPlacement(Rect body, OwnNominee d)
        {
            float side = Mathf.Floor(Mathf.Min(body.height, body.width * 0.58f));
            DrawOnCompass(new Rect(body.x, body.y, side, side), d, editable: true);
            var right = new Rect(body.x + side + StatStep(28f), body.y, Mathf.Max(1f, body.width - side - StatStep(28f)), body.height);
            DrawOnPrediction(right, d);
        }

        /// <summary>The party's poll with the nominee where it stands: its caption ("REP'S POLL HERE" - the coordinator's film finding: a figure with no words
        /// reads as nothing), the one large figure, and under it the poll at the party's own point, so the moved point's effect is on the page; the slip keeps
        /// both and their difference. The captions are one line each, drawn through MeasuredLabel at the column's width.</summary>
        private float DrawOnPrediction(Rect r, OwnNominee d)
        {
            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            GUIStyle under = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            float capH = Mathf.Ceil(DeskCaptionHeight(head));
            string caption = UiFormat.Possessive(PartySystems.ShortName(d.Country, d.Party).ToUpperInvariant(), true, true) + " POLL HERE";
            var cr = new Rect(r.x, r.y, r.width, capH);
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(cr, caption, head); }
            float y = r.y + capH + StatStep(4f);
            GUIStyle figure = DeskBody(30f, PoliSimTheme.TextPrimary);
            string text = double.IsNaN(_onPredicted) ? "—" : _onPredicted.ToString("0.0", CultureInfo.InvariantCulture) + " %";
            float h = Mathf.Ceil(figure.CalcHeight(new GUIContent(text), r.width));
            float w = Mathf.Ceil(figure.CalcSize(new GUIContent(text)).x);
            var fr = new Rect(r.x, y, Mathf.Min(r.width, w), h);
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(fr, text, figure); }
            y += h + StatStep(2f);
            if (!double.IsNaN(_onAtParty))
            {
                var ur = new Rect(r.x, y, r.width, capH);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(ur, "AT THE PARTY'S OWN POINT " + _onAtParty.ToString("0.0", CultureInfo.InvariantCulture) + " %", under); }
                y += capH;
            }
            SlipAnchor(new Rect(r.x, r.y, r.width, y - r.y), "predicted");
            return y;
        }

        // ── step 3: the nominee's name ──────────────────────────────────────────────────────────────────

        private void DrawOnName(Rect body, OwnNominee d)
        {
            float w = Mathf.Floor(body.width * 0.46f);
            float fieldH = StatStep(48f);
            var field = new Rect(body.x, body.y, w, fieldH);
            string name = PcField(field, "NOMINEE", d.Name, OwnNominees.MaxNameLength);
            if (name != d.Name) { d.Name = name; _onBriefFor = null; _onRefused = null; }
            SlipAnchor(field, "name");
            float markSide = Mathf.Floor(Mathf.Min(StatStep(72f), body.height * 0.3f));
            string stem = OnMark(d.Country, d.Party);
            var mark = new Rect(body.x, body.y + fieldH + StatStep(18f), markSide, markSide);
            if (!string.IsNullOrEmpty(stem)) { PcMark(mark, stem, PoliSimTheme.PartyLaddered(d.Country, d.Party)); }
            SlipAnchor(mark, "ticket/" + d.Party);
        }

        // ── step 4: the review ──────────────────────────────────────────────────────────────────────────

        /// <summary>The ticket as it enters the run-up: the party's mark, the nominee's name and the ticket; the start's brief as a ledger with the nominee's row
        /// (`StartBrief.Rows` and `StartBrief.NomineeRow` - the brief's own words); where the nominee stands, and the party's poll there.</summary>
        private void DrawOnReview(Rect body, OwnNominee d)
        {
            float colW = Mathf.Floor(body.width * 0.42f);
            float markSide = Mathf.Floor(Mathf.Min(StatStep(96f), body.height * 0.25f));
            var mark = new Rect(body.x, body.y, markSide, markSide);
            string stem = OnMark(d.Country, d.Party);
            if (!string.IsNullOrEmpty(stem)) { PcMark(mark, stem, PoliSimTheme.PartyLaddered(d.Country, d.Party)); }
            SlipAnchor(mark, "ticket/" + d.Party);
            float tx = mark.xMax + StatStep(16f);
            float textW = Mathf.Max(1f, colW - markSide - StatStep(16f));
            GUIStyle nameStyle = DeskBody(18f, PoliSimTheme.TextPrimary);
            GUIStyle cap = DeskCaption(9f, PoliSimTheme.TextSecondary);
            float nh = Mathf.Ceil(nameStyle.CalcHeight(new GUIContent("Ag"), textW));
            float ch = Mathf.Ceil(DeskCaptionHeight(cap));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(tx, body.y, textW, nh), string.IsNullOrWhiteSpace(d.Name) ? "—" : d.Name.Trim(), nameStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(tx, body.y + nh + StatStep(4f), textW, ch), OwnNomineeFlow.TicketLine(d).ToUpperInvariant(), cap);
            }
            SlipAnchor(new Rect(tx, body.y, textW, nh), "name");

            // the start's brief as a ledger, its last row the nominee's
            string key = d.Party + "|" + d.Name;
            if (_onBriefFor != key && Event.current.type == EventType.Layout)
            {
                _onBriefFor = key;
                _onBrief = new List<StartBrief.Row>();
                if (StartPoints.TryPlayable(d.Country, out StartPoints.StartPoint start))
                {
                    _onBrief.AddRange(StartBrief.Rows(start));
                    if (StartBrief.NomineeRow(start, d) is StartBrief.Row row) { _onBrief.Add(row); }
                }
            }
            float y = PcPlateHead(new Rect(body.x, mark.yMax + StatStep(14f), colW, 0f), "THE RUN-UP");
            if (_onBrief != null)
            {
                GUIStyle rowName = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
                GUIStyle rowFigure = DeskCaption(8.5f, PoliSimTheme.TextPrimary, bold: true);
                float rh = Mathf.Ceil(DeskCaptionHeight(rowFigure)) + StatStep(4f);
                float lane = 0f;
                foreach (StartBrief.Row row in _onBrief) { lane = Mathf.Max(lane, rowName.CalcSize(new GUIContent(row.Name.ToUpperInvariant())).x); }
                lane = Mathf.Min(Mathf.Ceil(lane) + StatStep(10f), colW * 0.4f);
                foreach (StartBrief.Row row in _onBrief)
                {
                    if (y + rh > body.yMax) { break; }
                    if (Event.current.type == EventType.Repaint)
                    {
                        PoliSimWidgets.MeasuredLabel(new Rect(body.x, y, lane, rh), row.Name.ToUpperInvariant(), rowName);
                        PoliSimWidgets.MeasuredLabel(new Rect(body.x + lane, y, Mathf.Max(1f, colW - lane), rh), row.Figure, rowFigure);
                    }
                    y += rh;
                }
            }

            // where the nominee stands, and the party's poll there
            float cx = body.x + colW + StatStep(28f);
            float side = Mathf.Floor(Mathf.Min(body.height * 0.72f, body.xMax - cx));
            DrawOnCompass(new Rect(cx, body.y, side, side), d, editable: false);
            DrawOnPrediction(new Rect(cx, body.y + side + StatStep(8f), side, Mathf.Max(1f, body.yMax - body.y - side - StatStep(8f))), d);
        }
    }
}
