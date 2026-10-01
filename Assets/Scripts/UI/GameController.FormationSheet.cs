using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §647, redrawn from Design's board 21a (D-PS, §685): **THE FORMATION SHEET AS ROWS.** The sheet the Speaker's request opens over the desk
    /// (premise 1): the cabinet and each post, the supporters and their demands, every invited party's answer, and the investiture's count - the
    /// player revises and offers again until every invited party accepts, then tables the proposal (premise 4) or passes.
    ///
    /// **The board's grammar, applied.** A party is a row of cells - its mark, its name, its seats as a figure and as a bar on the chamber's one axis
    /// (0.34 px a seat at the board's scale, 18b's device), the ●/○ toggle (17b: the fill is the state), and the posts' WEIGHT OFFERED ⁄ DUE (§706: each
    /// post at its published salience) with ✕ leading where the offer weighs less than the Gamson share - the one departure that lowers a
    /// partner's acceptance (an over-offer raises none). The
    /// sentence each row used to be goes to the row's slip. The Answers are rows - ✓/✕, the party, ACCEPTS or REFUSES, worth ⁄ elsewhere - and
    /// the reason is the slip. The investiture is a bar with the majority tick (Bad ink, a mark, not a word) and ✓ leading. Every post, Finance
    /// included (§706: the Treasury lock lifted), passes to the next party. Brass is the one act (TABLE IT); PASS is paper; CLOSE THE SHEET is in the head. **No scroll at 1280 × 720**: the
    /// two columns are absolute rows; a column taller than the sheet (a proposal with three supporters' demands open) scrolls alone, never clipped.
    ///
    /// A click is QUEUED and applied after the sheet is drawn: a change made mid-event would add or drop rows the event's other half never saw.
    /// </summary>
    public partial class GameController
    {
        private Rect _formationSheetInnerRect;
        private Vector2 _formationLeftScroll, _formationRightScroll;

        private void DrawFormationSheetStage(float availableHeight, float availableWidth)
        {
            SpeakerRound round = _simulationManager.RoundOf(PlayerCountryId);
            if (round == null || round.Stage != RoundStage.PlayerAsked) { _formationSheetOpen = false; return; }
            FormationProposal draft = FormationDraft();
            ProposalVerdict verdict = FormationVerdict(draft);
            string you = _playerCountry.PlayerPartyAbbrev;
            CountryId country = _playerCountry.Id;
            string Name(string key) => PartySystems.ShortName(country, key);

            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight, GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint) { _formationSheetInnerRect = inner; }
            else if (_formationSheetInnerRect.width > 1f) { inner = _formationSheetInnerRect; }

            BeginSlipAnchors();
            var book = new PeopleSlips.Book();
            System.Action change = null;
            float ux = inner.width / DeskBoardInnerWidth;
            float rowH = StatsUnit(27f), headH = StatsUnit(18f), gap = StatsUnit(12f);
            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle figure = DeskCaption(13f, PoliSimTheme.TextPrimary, true);
            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            int totalSeats = 0;
            foreach (int s in _playerCountry.ParliamentSeats.Values) { totalSeats += s; }
            totalSeats = System.Math.Max(1, totalSeats);

            // ---- The head: Formation, the formateur's mark, ASKED BY THE SPEAKER; at the right the rejections as a figure and CLOSE THE SHEET.
            var head = new Rect(inner.x, inner.y, inner.width, StatsUnit(34f));
            GUIStyle title = DeskBody(18f, PoliSimTheme.TextPrimary);
            title.fontStyle = FontStyle.Bold;
            float x = head.x;
            float tw = Mathf.Ceil(title.CalcSize(new GUIContent("Formation")).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, head.y, tw, head.height), "Formation", title);
            x += tw + StatsUnit(10f);
            DrawPartyMarkSlot(new Rect(x, head.y, StatsUnit(16f), head.height), country, you);
            x += StatsUnit(16f) + StatsUnit(8f);
            string asked = "ASKED BY " + RoundAsker(round);
            float aw = Mathf.Ceil(caption.CalcSize(new GUIContent(asked)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, head.y, aw, head.height), asked, caption);
            SlipAnchor(new Rect(head.x, head.y, x + aw - head.x, head.height), "head");
            string[] rule = RoundRule(round);
            book.Anchors["head"] = new SlipContent(Name(you) + " " + asked)
                .Add("THE ROUND " + DeskDated(round.Occasion).ToUpperInvariant())
                .Add(rule[0])
                .Add(rule[1]);

            Rect close = RowChipRectEndingAt(head.xMax, head, "CLOSE THE SHEET");
            if (DrawRowChip(close, "CLOSE THE SHEET", ChipFace.Paper)) { _formationSheetOpen = false; }
            SlipAnchor(close, "close");
            book.Anchors["close"] = new SlipContent("CLOSE THE SHEET").Add("CLOSED, THE CLOCK STILL WAITS ON YOUR ANSWER").Add("THE PARLIAMENT TAB OPENS THE SHEET AGAIN");
            string rejectedTail = RoundFigureTail(round);
            float rtw = Mathf.Ceil(muted.CalcSize(new GUIContent(rejectedTail)).x) + 2f;
            GUIStyle rejectedFigure = DeskCaption(14f, PoliSimTheme.TextPrimary, true);
            string rejected = RoundFigure(round);
            float rfw = Mathf.Ceil(rejectedFigure.CalcSize(new GUIContent(rejected)).x) + 2f;
            float rx = close.x - StatsUnit(14f) - rtw;
            PoliSimWidgets.MeasuredLabel(new Rect(rx, head.y, rtw, head.height), rejectedTail, muted);
            PoliSimWidgets.MeasuredLabel(new Rect(rx - StatsUnit(4f) - rfw, head.y, rfw, head.height), rejected, rejectedFigure);

            float top = head.yMax + StatsUnit(4f);
            var left = new Rect(inner.x, top, 560f * ux, inner.yMax - top);
            var right = new Rect(inner.x + 584f * ux, top, inner.xMax - (inner.x + 584f * ux), inner.yMax - top);

            // ---- The left column: THE CABINET, THE SUPPORT.
            Dictionary<string, List<CabinetPortfolio>> gamson = GovernmentRecord.GamsonPosts(_playerCountry, draft.CabinetParties, you);
            var seated = new List<PoliticalParty>();
            foreach (PoliticalParty party in PartySystems.For(PlayerCountryId))
            {
                if (_playerCountry.ParliamentSeats.TryGetValue(party.Abbrev, out int seats) && seats > 0) { seated.Add(party); }
            }
            float leftNeeded = headH + seated.Count * rowH + gap + headH;
            foreach (PoliticalParty party in seated)
            {
                if (draft.CabinetParties.Contains(party.Abbrev)) { continue; }
                leftNeeded += rowH;
                if (draft.Supporters.Contains(party.Abbrev) && draft.Tabled.TryGetValue(party.Abbrev, out List<AgreementItem> t)) { leftNeeded += t.Count * StatsUnit(22f); }
            }
            float leftShift = BeginSheetColumn(left, leftNeeded, ref _formationLeftScroll);
            float y = left.y;
            DrawRowSectionHead(new Rect(left.x, y, left.width, headH), "THE CABINET", "SEATS · IN · WEIGHT OFFERED ⁄ DUE");
            y += headH;
            foreach (PoliticalParty party in seated)
            {
                string key = party.Abbrev;
                int seats = _playerCountry.ParliamentSeats[key];
                bool formateur = key == you;
                bool inCabinet = draft.CabinetParties.Contains(key);
                var row = new Rect(left.x, y, left.width, rowH);
                DrawPartyMarkSlot(new Rect(row.x, row.y, 20f * ux, row.height), country, key);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 28f * ux, row.y, 50f * ux, row.height), Name(key), nameStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 86f * ux, row.y, 36f * ux, row.height), seats.ToString(CultureInfo.InvariantCulture), figure);
                if (Event.current.type == EventType.Repaint)
                {
                    float barH = StatsUnit(8f);
                    PoliSimTheme.Rule(new Rect(row.x + 128f * ux, row.y + Mathf.Round((row.height - barH) * 0.5f), Mathf.Max(1f, Mathf.Round(120f * ux * seats / totalSeats)), barH),
                        inCabinet ? PoliSimTheme.TextPrimary : PoliSimTheme.Hairline);
                }
                float chipX = row.x + 256f * ux;
                if (formateur) { DrawRowChip(RowChipRectFrom(chipX, row, "FORMATEUR", padBoard: 6f), "FORMATEUR", ChipFace.Outline); }
                else if (DrawRowChip(RowChipRectFrom(chipX, row, inCabinet ? "● IN" : "○ OUT"), inCabinet ? "● IN" : "○ OUT", inCabinet ? ChipFace.Filled : ChipFace.Paper))
                {
                    change = () =>
                    {
                        if (!inCabinet) { draft.CabinetParties.Add(key); draft.Supporters.Remove(key); draft.AcceptedDemands.Remove(key); return; }
                        draft.CabinetParties.Remove(key);
                        if (draft.Posts.TryGetValue(key, out List<CabinetPortfolio> lost))
                        {
                            draft.Posts.Remove(key);
                            if (!draft.Posts.ContainsKey(you)) { draft.Posts[you] = new List<CabinetPortfolio>(); }
                            draft.Posts[you].AddRange(lost);   // a partner's posts come back to the formateur
                        }
                    };
                }
                var slip = new SlipContent(Name(key) + " · " + seats.ToString(CultureInfo.InvariantCulture) + " SEATS")
                    .Add(formateur ? (round.Bundestag ? "THE FORMATEUR - THE CHANCELLOR'S PARTY" : "THE FORMATEUR - THE PRIME MINISTER'S PARTY") : inCabinet ? "INVITED INTO THE CABINET" : "NOT INVITED");
                if (inCabinet)
                {
                    // §706 (the review's defect 4): the posts WEIGHED, as the partner's answer weighs them (Druckman & Warwick's salience) - a count of
                    // posts said 2 ⁄ 2 where the weights cut the partner's payoff, and marked a post short where the weights were whole
                    int offered = draft.PostsOf(key);
                    double offeredWeight = PortfolioSalience.Of(_playerCountry.Id, draft.Posts.TryGetValue(key, out List<CabinetPortfolio> held) ? held : null);
                    double dueWeight = PortfolioSalience.Of(_playerCountry.Id, gamson.TryGetValue(key, out List<CabinetPortfolio> g) ? g : null);
                    bool short_ = offeredWeight < dueWeight - 0.005;
                    float px = row.x + 356f * ux;
                    if (short_) { DrawVerdictSlot(new Rect(px, row.y, StatsUnit(16f), row.height), false); }
                    DrawFigurePair(px + StatsUnit(20f), row, offeredWeight.ToString("0.00", CultureInfo.InvariantCulture), "⁄ " + dueWeight.ToString("0.00", CultureInfo.InvariantCulture));
                    slip.Add("OFFERED " + Posts(offered) + ", WEIGHING " + offeredWeight.ToString("0.00", CultureInfo.InvariantCulture) + " · GAMSON'S LAW GIVES IT " + dueWeight.ToString("0.00", CultureInfo.InvariantCulture));
                    slip.Add("A POST WEIGHS ITS SALIENCE - FINANCE MORE THAN ONE");
                    if (short_) { slip.Add("THE OFFER WEIGHS LESS THAN THE LAW: IT LOWERS ITS ACCEPTANCE; MORE BUYS NOTHING"); }
                }
                SlipAnchor(Shifted(row, leftShift), "cabinet/" + key);
                book.Anchors["cabinet/" + key] = slip;
                DrawRowRule(row);
                y += rowH;
            }

            y += gap;
            DrawRowSectionHead(new Rect(left.x, y, left.width, headH), "THE SUPPORT");
            y += headH;
            foreach (PoliticalParty party in seated)
            {
                string key = party.Abbrev;
                if (draft.CabinetParties.Contains(key)) { continue; }
                int seats = _playerCountry.ParliamentSeats[key];
                bool supports = draft.Supporters.Contains(key);
                var row = new Rect(left.x, y, left.width, rowH);
                DrawPartyMarkSlot(new Rect(row.x, row.y, 20f * ux, row.height), country, key);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 28f * ux, row.y, 50f * ux, row.height), Name(key), nameStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 86f * ux, row.y, 36f * ux, row.height), seats.ToString(CultureInfo.InvariantCulture), figure);
                string face = supports ? "● ASKED" : "○ ASK";
                if (DrawRowChip(RowChipRectEndingAt(row.xMax, row, face), face, supports ? ChipFace.Filled : ChipFace.Paper))
                {
                    change = () =>
                    {
                        if (supports) { draft.Supporters.Remove(key); draft.AcceptedDemands.Remove(key); return; }
                        draft.Supporters.Add(key);
                        draft.AcceptedDemands[key] = new List<string>();
                        draft.FreezeTabled(_playerCountry, _simulationManager.CurrentDate, _world);
                    };
                }
                SlipAnchor(Shifted(row, leftShift), "support/" + key);
                book.Anchors["support/" + key] = new SlipContent(Name(key) + " · " + seats.ToString(CultureInfo.InvariantCulture) + " SEATS")
                    .Add(supports ? "ASKED TO CARRY THE CABINET FROM OUTSIDE, ON AN AGREEMENT" : "NOT ASKED TO SUPPORT")
                    .Add("A SUPPORTER TABLES ITS DEMANDS; REFUSE MORE THAN IT TOLERATES AND IT REFUSES");
                DrawRowRule(row);
                y += rowH;
                if (!supports || !draft.Tabled.TryGetValue(key, out List<AgreementItem> tabled)) { continue; }
                draft.AcceptedDemands.TryGetValue(key, out List<string> accepted);
                foreach (AgreementItem item in tabled)
                {
                    string demand = SupportAgreement.KeyOf(item);
                    bool yes = accepted != null && accepted.Contains(demand);
                    var d = new Rect(left.x + 28f * ux, y, left.width - 28f * ux, StatsUnit(22f));
                    DrawAgreementItemPair(new Rect(d.x, d.y, StatsUnit(36f), d.height), item, PoliSimTheme.TextPrimary);
                    string demandFace = yes ? "● ACCEPTED" : "○ REFUSED";
                    Rect chip = RowChipRectEndingAt(d.xMax, d, demandFace);
                    PoliSimWidgets.MeasuredLabel(new Rect(d.x + StatsUnit(44f), d.y, Mathf.Max(1f, chip.x - StatsUnit(8f) - (d.x + StatsUnit(44f))), d.height), item.Name ?? item.LawId, DeskBody(12.5f, PoliSimTheme.TextPrimary));
                    if (DrawRowChip(chip, demandFace, yes ? ChipFace.Filled : ChipFace.Paper))
                    {
                        change = () =>
                        {
                            if (!draft.AcceptedDemands.TryGetValue(key, out List<string> list)) { list = new List<string>(); draft.AcceptedDemands[key] = list; }
                            if (yes) { list.Remove(demand); } else { list.Add(demand); }
                        };
                    }
                    SlipAnchor(Shifted(d, leftShift), "demand/" + demand);
                    book.Anchors["demand/" + demand] = new SlipContent((item.Name ?? item.LawId).ToUpperInvariant())
                        .Add("TABLED BY " + Name(key) + " · " + (yes ? "THE FORMATEUR ACCEPTS IT" : "THE FORMATEUR REFUSES IT"));
                    DrawRowRule(d);
                    y += d.height;
                }
            }
            EndSheetColumn(leftShift);

            // ---- The right column: THE POSTS, THE ANSWERS, THE INVESTITURE and the acts.
            float ansH = StatsUnit(24f);
            int answers = verdict != null ? verdict.Answers.Count : 0;
            float rightNeeded = headH + System.Enum.GetValues(typeof(CabinetPortfolio)).Length * rowH + gap + headH + System.Math.Max(1, answers) * ansH
                + gap + headH + StatsUnit(36f) + StatsUnit(14f) + StatsUnit(26f) + (_formationRefusal != null ? ansH : 0f);
            float rightShift = BeginSheetColumn(right, rightNeeded, ref _formationRightScroll);
            y = right.y;
            DrawRowSectionHead(new Rect(right.x, y, right.width, headH), "THE POSTS");
            y += headH;
            // Each portfolio held by one cabinet party, passed on to the next (premise 2: posts carry their levers, §634); the formateur never gives its
            // last one away. §706 (Elias's ruling of 2026-10-01): Finance is a post like any other - it passes to a partner as the others do.
            foreach (CabinetPortfolio post in (CabinetPortfolio[])System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                string holder = null;
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in draft.Posts) { if (kv.Value.Contains(post)) { holder = kv.Key; } }
                if (holder == null || !draft.CabinetParties.Contains(holder)) { holder = you; }
                int at = draft.CabinetParties.IndexOf(holder);
                string next = draft.CabinetParties.Count > 0 ? draft.CabinetParties[(at + 1) % draft.CabinetParties.Count] : holder;
                bool fixedPost = next == holder || (holder == you && draft.PostsOf(you) <= 1);
                var row = new Rect(right.x, y, right.width, rowH);
                string postName = Effectiveness.ShortName(post).ToUpperInvariant();
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 28f * ux, row.y, 110f * ux, row.height), postName, caption);
                DrawPartyMarkSlot(new Rect(row.x + 146f * ux, row.y, 20f * ux, row.height), country, holder);
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + 172f * ux, row.y, 50f * ux, row.height), Name(holder), nameStyle);
                var slip = new SlipContent(postName + " · " + Name(holder)).Add("THE POST CARRIES ITS PORTFOLIO'S LEVERS TO THE PARTY THAT HOLDS IT");
                if (fixedPost)
                {
                    slip.Add(next == holder ? "A ONE-PARTY CABINET HOLDS EVERY POST" : "THE FORMATEUR NEVER GIVES ITS LAST POST AWAY");
                }
                else
                {
                    string to = next;
                    float markSide = StatsUnit(16f);
                    float chipW = RowChipWidth("PASS TO") + StatsUnit(4f) + markSide;
                    var chip = new Rect(row.x + 230f * ux, row.y + Mathf.Round((row.height - StatsUnit(20f)) * 0.5f), chipW, StatsUnit(20f));
                    bool clicked = DrawRowChip(chip, string.Empty, ChipFace.Paper);
                    float labelW = RowChipWidth("PASS TO") - StatsUnit(8f);
                    PoliSimWidgets.MeasuredLabel(new Rect(chip.x + StatsUnit(8f), chip.y, labelW, chip.height), "PASS TO", RowChipCaption(PoliSimTheme.TextPrimary));
                    DrawPartyMarkSlot(new Rect(chip.x + StatsUnit(8f) + labelW + StatsUnit(4f), chip.y, markSide, chip.height), country, to);
                    if (clicked)
                    {
                        change = () =>
                        {
                            foreach (List<CabinetPortfolio> held in draft.Posts.Values) { held.Remove(post); }
                            if (!draft.Posts.ContainsKey(to)) { draft.Posts[to] = new List<CabinetPortfolio>(); }
                            draft.Posts[to].Add(post);
                        };
                    }
                    slip.Add("PASS TO " + Name(to) + " - THE NEXT PARTY IN THE CABINET");
                }
                SlipAnchor(Shifted(row, rightShift), "post/" + post);
                book.Anchors["post/" + post] = slip;
                DrawRowRule(row);
                y += rowH;
            }

            y += gap;
            DrawRowSectionHead(new Rect(right.x, y, right.width, headH), "THE ANSWERS", "WORTH ⁄ ELSEWHERE");
            y += headH;
            if (verdict == null || verdict.Investiture == null)
            {
                var row = new Rect(right.x, y, right.width, ansH);
                PoliSimWidgets.MeasuredLabel(row, "NOT WELL FORMED", muted);
                SlipAnchor(Shifted(row, rightShift), "answers/none");
                book.Anchors["answers/none"] = SlipWrapped("NOT WELL FORMED", verdict?.Reason ?? "the proposal is not well formed");
                y += ansH;
            }
            if (verdict != null)
            {
                foreach (PartyAnswer answer in verdict.Answers)
                {
                    var row = new Rect(right.x, y, right.width, ansH);
                    DrawVerdictSlot(new Rect(row.x, row.y, 20f * ux, row.height), answer.Accepts);
                    DrawPartyMarkSlot(new Rect(row.x + 28f * ux, row.y, 20f * ux, row.height), country, answer.Party);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + 54f * ux, row.y, 50f * ux, row.height), Name(answer.Party), nameStyle);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + 110f * ux, row.y, 90f * ux, row.height), answer.Accepts ? "ACCEPTS" : "REFUSES", caption);
                    if (answer.InCabinet)
                    {
                        DrawFigurePair(row.x + 208f * ux, row, answer.Payoff.ToString("0.000", CultureInfo.InvariantCulture), "⁄ " + answer.Alternative.ToString("0.000", CultureInfo.InvariantCulture));
                    }
                    SlipAnchor(Shifted(row, rightShift), "answer/" + answer.Party);
                    book.Anchors["answer/" + answer.Party] = SlipWrapped(Name(answer.Party) + (answer.Accepts ? " ACCEPTS" : " REFUSES"), Name(answer.Party) + " " + answer.Reason);
                    DrawRowRule(row);
                    y += ansH;
                }
            }

            y += gap;
            DrawRowSectionHead(new Rect(right.x, y, right.width, headH), round.Bundestag ? "THE CHANCELLOR'S ELECTION" : "THE INVESTITURE");   // §705
            y += headH;
            var bar = new Rect(right.x, y, right.width, StatsUnit(36f));
            if (verdict?.Investiture != null)
            {
                CoalitionFormation.CabinetEvaluation inv = verdict.Investiture;
                int majority = totalSeats / 2 + 1;
                // §712 (the review's defect 4): in the fourteen days the tabling meets a ballot on persons, not the investiture - the bar shows that ballot as
                // it would stand (the votes for your candidate against those for the other nominations, the tick a majority of the members)
                (bool shownBallot, int forYours, int forOthers, int needed) = round.Bundestag ? FormationPersonBallot(draft) : (false, 0, 0, 0);   // cached by draft and day (the second pass, defect 6)
                bool person = shownBallot;
                int carrying = person ? forYours : inv.SupportedSeats, against = person ? forOthers : inv.OpposedSeats;
                bool wins = person ? forYours >= needed : inv.Wins;
                if (person) { majority = needed; }
                DrawVerdictSlot(new Rect(bar.x, bar.y, 20f * ux, bar.height), wins);
                float trackW = Mathf.Min(366f * ux, bar.width - 28f * ux - StatsUnit(110f));
                float trackH = StatsUnit(12f);
                var track = new Rect(bar.x + 28f * ux, bar.y + Mathf.Round((bar.height - trackH) * 0.5f), trackW, trackH);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.Rule(track, PoliSimTheme.MagnitudeStepEmpty);
                    PoliSimTheme.Rule(new Rect(track.x, track.y, Mathf.Round(track.width * Mathf.Clamp01(carrying / (float)totalSeats)), track.height), PoliSimTheme.TextPrimary);
                    float tick = track.x + Mathf.Round(track.width * majority / (float)totalSeats);
                    PoliSimTheme.Rule(new Rect(tick - 1f, track.y - StatsUnit(4f), 2f, track.height + StatsUnit(8f)), PoliSimTheme.Bad);
                }
                float fx = DrawFigurePair(track.xMax + StatsUnit(10f), bar, carrying.ToString(CultureInfo.InvariantCulture), "⁄");
                PoliSimWidgets.MeasuredLabel(new Rect(fx + StatsUnit(4f), bar.y, StatsUnit(40f), bar.height), against.ToString(CultureInfo.InvariantCulture), figure);
                SlipAnchor(Shifted(bar, rightShift), "investiture");
                book.Anchors["investiture"] = person
                    ? new SlipContent(wins ? "YOUR CANDIDATE WOULD BE ELECTED" : "YOUR CANDIDATE WOULD NOT BE ELECTED")
                        .Add(string.Format(CultureInfo.InvariantCulture, "THE BALLOT ON THE NOMINATIONS STANDING: {0} FOR YOURS, {1} FOR THE OTHERS", carrying, against))
                        .Add(string.Format(CultureInfo.InvariantCulture, "THE TICK: A MAJORITY OF THE MEMBERS, {0} OF {1}", majority, totalSeats))
                    : new SlipContent(inv.Wins ? "IT WOULD PASS" : "IT WOULD FAIL")
                        .Add(string.Format(CultureInfo.InvariantCulture, "AS IT WOULD STAND: {0} CARRYING IT, {1} AGAINST", inv.SupportedSeats, inv.OpposedSeats))
                        .Add(string.Format(CultureInfo.InvariantCulture, "THE TICK: A MAJORITY OF THE CHAMBER, {0} OF {1}", majority, totalSeats));
            }
            y += bar.height;
            if (_formationRefusal != null)
            {
                var refusal = new Rect(right.x, y, right.width, ansH);
                PoliSimWidgets.MeasuredLabel(refusal, "REFUSED", DeskCaption(9.5f, PoliSimTheme.Bad, true));
                SlipAnchor(Shifted(refusal, rightShift), "refused");
                book.Anchors["refused"] = SlipWrapped("REFUSED", _formationRefusal);
                y += ansH;
            }
            y += StatsUnit(14f);
            var acts = new Rect(right.x, y, right.width, StatsUnit(26f));
            bool allAccept = verdict != null && verdict.AllAccept;
            Rect table = RowChipRectEndingAt(acts.xMax, acts, "TABLE IT", 26f, 12f);
            if (DrawRowChip(table, "TABLE IT", ChipFace.Brass, disabled: !allAccept))
            {
                if (_simulationManager.SubmitFormation(PlayerCountryId, draft, out _, out string refused)) { _formationSheetOpen = false; }
                else { _formationRefusal = refused; }
            }
            SlipAnchor(Shifted(table, rightShift), "table");
            book.Anchors["table"] = new SlipContent("TABLE IT").Add(allAccept ? "EVERY INVITED PARTY ACCEPTS" : "NOT EVERY INVITED PARTY ACCEPTS - REVISE THE OFFER")
                .Add(RoundVoteWhen(round, brief: false));
            Rect pass = RowChipRectEndingAt(table.x - StatsUnit(10f), acts, "PASS", 26f, 12f);
            if (DrawRowChip(pass, "PASS", ChipFace.Paper))
            {
                if (_simulationManager.PassFormation(PlayerCountryId, out string refused)) { _formationSheetOpen = false; } else { _formationRefusal = refused; }
            }
            SlipAnchor(Shifted(pass, rightShift), "pass");
            book.Anchors["pass"] = new SlipContent("PASS").Add(RoundPassLine(round));
            string voteOn = RoundVoteWhen(round, brief: true);
            float vw = Mathf.Ceil(muted.CalcSize(new GUIContent(voteOn)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(pass.x - StatsUnit(10f) - vw, acts.y, vw, acts.height), voteOn, muted);
            EndSheetColumn(rightShift);

            DrawSlips(book, inner);

            if (change != null)
            {
                change();
                _formationDraftVersion++;
                _formationRefusal = null;   // a revised offer is answered afresh
            }
        }

        /// <summary>A column taller than its rect scrolls alone (never clipped); returns the scroll's offset, which the slip anchors subtract (a
        /// scroll view's rects are its own, the pointer's the window's). Zero, and no scroll view, when the column fits - the sheet at 1280 × 720.</summary>
        private float BeginSheetColumn(Rect column, float needed, ref Vector2 scroll)
        {
            if (needed <= column.height) { scroll = Vector2.zero; _sheetColumnScrolls.Push(false); return 0f; }
            scroll = GUI.BeginScrollView(column, scroll, new Rect(column.x, column.y, column.width - StatsUnit(14f), needed));
            _sheetColumnScrolls.Push(true);
            return scroll.y;
        }

        private void EndSheetColumn(float shift)
        {
            if (_sheetColumnScrolls.Count > 0 && _sheetColumnScrolls.Pop()) { GUI.EndScrollView(); }
        }

        private readonly Stack<bool> _sheetColumnScrolls = new Stack<bool>();

        private static Rect Shifted(Rect r, float shift) => shift == 0f ? r : new Rect(r.x, r.y - shift, r.width, r.height);

        private static string Posts(int n) => n.ToString(CultureInfo.InvariantCulture) + (n == 1 ? " POST" : " POSTS");

        private static string Ordinal(int n) => n switch { 1 => "FIRST", 2 => "SECOND", 3 => "THIRD", 4 => "FOURTH", 5 => "FIFTH", 6 => "SIXTH", 7 => "SEVENTH", _ => n.ToString(CultureInfo.InvariantCulture) + "TH" };

        /// <summary>A slip whose one sentence is wrapped into lines of at most 64 characters at word boundaries, upper-cased (the slip's face).</summary>
        private static SlipContent SlipWrapped(string head, string sentence)
        {
            var slip = new SlipContent(head);
            if (string.IsNullOrEmpty(sentence)) { return slip; }
            var line = new System.Text.StringBuilder();
            foreach (string word in sentence.ToUpperInvariant().Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > 64) { slip.Add(line.ToString()); line.Clear(); }
                if (line.Length > 0) { line.Append(' '); }
                line.Append(word);
            }
            if (line.Length > 0) { slip.Add(line.ToString()); }
            return slip;
        }

        /// <summary>
        /// An agreement item's pair (19a, 21b item 3): a law's emblem and verb (<see cref="DrawLawPair"/>); a dial demand's the crime-and-justice area
        /// icon and RAISE or LOWER by the target against the dial's value when tabled (the structural-law rule - a dial set to a value).
        /// </summary>
        private void DrawAgreementItemPair(Rect r, AgreementItem item, Color ink)
        {
            float side = Mathf.Min(StatsUnit(16f), r.height);
            float y0 = r.y + Mathf.Round((r.height - side) * 0.5f);
            Texture2D emblem;
            Symbol? verb;
            if (item.Kind == AgreementItemKind.Law)
            {
                // The law's own pair (DrawLawPair's emblem and verb), at the row's scale: the Laws page's pair is a fixed 16 px, which read as a speck
                // beside this row's type at 2560 (s685_2560).
                LawDefinition law = LawCatalog.GetById(item.LawId);
                if (law == null) { return; }
                emblem = LawEmblem(law.Category);
                verb = SymbolRegistry.Of(LawVerbs.Of(law));
            }
            else
            {
                emblem = IconLibrary.GetAreaIcon(UiPalette.SystemArea.CrimeJustice);
                verb = item.Target >= item.StartValue ? Symbol.Raise : Symbol.Lower;
            }
            if (emblem != null && Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color; GUI.color = ink;
                GUI.DrawTexture(new Rect(r.x, y0, side, side), emblem, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
            if (verb.HasValue) { SymbolRegistry.Draw(new Rect(r.x + side + StatsUnit(4f), y0, side, side), verb.Value, ink, DeskCaption(6.5f, ink)); }   // a both-ways law: no verb (§662)
        }
    }
}
