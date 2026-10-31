using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Board 21b (D-PS, §685): **THE PARLIAMENT TAB'S POLITICAL BLOCKS AS ROWS** - the support agreements, the caretaker line, the Speaker's round
    /// and the confidence moment, drawn in the desk's row grammar (<see cref="GameController.DrawRowChip"/> and its siblings). Each block reserves
    /// its rows through the tab's layout (the settings screen's way) and draws inside them; the sentences the blocks used to print are the rows'
    /// slips, drawn over the tab's scroll content, where the anchors and the pointer share one space.
    /// <list type="bullet">
    /// <item><b>The agreement.</b> OWED is the state every item begins in, so it draws nothing; only the exception is marked - ✓ DELIVERED, ✕ BROKEN,
    /// leading the item, both verdicts. The tally is three figures over one-word captions. Each item is a law's pair (emblem + verb, 19a) - a dial
    /// demand takes the crime-and-justice emblem and RAISE or LOWER by its target. A withdrawn agreement draws at the reduced presence (10a) under a
    /// WITHDRAWN stamp in Caution ink. The dates are in the slips, never beside every item.</item>
    /// <item><b>The caretaker line and the Speaker's ask.</b> Stamps and a mark, dated day-month-year: CARETAKER · SINCE · 31 JAN 2029; the asked
    /// party's mark · ASKED FIRST · PROPOSAL DUE · 7 FEB 2029. The round's log (ISO-dated by the model) is the head's slip, in the desk's dates.</item>
    /// <item><b>NO CONFIDENCE DECLARED</b> in Caution ink, the days to answer as a figure over its caption, and the week's two answers as PAPER -
    /// a decision with no default (neither is brass). The rail's clock cell reads HELD while a decision holds the clock (<see cref="DrawRailPauseChip"/>).</item>
    /// </list>
    /// </summary>
    public partial class GameController
    {

        /// <summary>A row reserved through the layout at a height in pixels.</summary>
        private Rect ReserveRowPx(float height)
        {
            Rect r = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (_parliamentVisibleWidth > 1f && r.width > _parliamentVisibleWidth) { r.width = _parliamentVisibleWidth; }
            return r;
        }

        /// <summary>The Parliament tab's visible width, set by the Politics tab each pass (its scroll content can be wider).</summary>
        private float _parliamentVisibleWidth;

        /// <summary>The Parliament tab's four political blocks and their slips, in the scroll view's own space. The review of wave/usf (H1): their slips go on
        /// the Politics page's own book and are judged by the page's one `DrawSlips` pass (its `MoveScrolledAnchors` carries them out of the scroll) - this
        /// method used to clear every anchor the page had registered above it (the chamber cards, the Congress view, the top masthead) and run a second pass
        /// inside the scroll, so none of those slips could open by the pointer.</summary>
        private void DrawParliamentPoliticalBlocks()
        {
            PeopleSlips.Book book = _politicsSlipBook;
            DrawSupportAgreements(book);   // PS-3h (§635)
            DrawCaretakerLine(book);
            DrawSpeakerRound(book);   // §646: the formateur's round
            DrawReferenceRow(book);   // §705: history as the reference where no election night carries it
            // K39 (Elias, 2026-10-23; board 30a): under the USA's Congress view the dated rows are retired - the view's provenance chips and its next-vote row
            // carry their dates, and YOUR NOMINEE's action is the who-governs strip's (DrawWhoGovernsNominee). They stand where no Congress view is drawn.
            bool congressView = PoliSim.Simulation.UsCongressView.Applies(_playerCountry);
            if (!congressView) { DrawRecordRow(book); }   // PS-6 US-2: the record by date, where the game does not elect the chamber - the president with it while the record seats the executive
            DrawPresidentRow(book);   // §770 (PS-5 item C4): the president the veto reads, and the game's own presidential election
            if (!congressView)
            {
                DrawUsPresidentRow(book);   // PS-6 US-8: the US president from the game's own count - the electors, the House's choice, the oath, the record beside it
                DrawUsCongressRow(book);   // PS-6 US-16: the House and the Senate class the game elected on the same day - the record's 220/215 and its 119th Senate beside them
                DrawUsNomineeRow(book);   // PS-6 US-28: the term-limited party's next nominee - the Vice President by default, the player's pick
            }
            DrawConfidence(book);   // PS-3i (§636)
            ReserveRowPx(1f);   // the tail the blocks always ended with, kept so the page's layout is unchanged
        }

        /// <summary>A block's head: its words in the serif at 14 with an optional mark before them; returns the rect for a right-hand stamp or figure.</summary>
        private Rect DrawBlockHead(Rect row, string words, string markKey, Color ink)
        {
            float x = row.x;
            if (markKey != null)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), _playerCountry.Id, markKey);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            GUIStyle serif = DeskBody(14f, ink);
            float w = Mathf.Ceil(serif.CalcSize(new GUIContent(words)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, serif);
            return new Rect(x + w, row.y, Mathf.Max(1f, row.xMax - x - w), row.height);
        }

        /// <summary>PS-3h (§635), redrawn from 21b: one block per support agreement - the supporter's mark and the words, the tally, every item as a row; where
        /// the player's party is the supporter, WITHDRAW SUPPORT (the spec's "threaten or withdraw"), its consequence on the slip.</summary>
        private void DrawSupportAgreements(PeopleSlips.Book book)
        {
            GovernmentRecord government = _playerCountry?.Government;
            if (government == null || government.Agreements.Count == 0) { return; }
            CountryId country = _playerCountry.Id;
            foreach (SupportAgreement agreement in government.Agreements)
            {
                bool withdrawn = !government.Support.Contains(agreement.Supporter);
                Color ink = withdrawn ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary;
                string name = PartySystems.ShortName(country, agreement.Supporter);

                Rect head = ReserveRow(30f);
                DrawBlockHead(head, "Support agreement", agreement.Supporter, ink);
                if (withdrawn) { DrawRowChip(RowChipRectEndingAt(head.xMax, head, "WITHDRAWN", padBoard: 6f), "WITHDRAWN", ChipFace.Outline, ink: PoliSimTheme.Caution); }
                SlipAnchor(head, "agreement/" + agreement.Supporter);
                book.Anchors["agreement/" + agreement.Supporter] = new SlipContent(name + " SUPPORT AGREEMENT")
                    .Add("AGREED " + DeskDay(agreement.FormedOn))
                    .Add(withdrawn ? "WITHDRAWN - " + name + " NO LONGER CARRIES THE GOVERNMENT" : name + " CARRIES THE GOVERNMENT FROM OUTSIDE ON THESE ITEMS")
                    .Add(agreement.Tally());

                Rect tally = ReserveRowPx(FigureOverCaptionHeight() + StatsUnit(8f));
                float x = tally.x;
                x += DrawFigureOverCaption(x, tally.y + StatsUnit(2f), agreement.Count(AgreementState.Owed).ToString(CultureInfo.InvariantCulture), "OWED", ink) + StatsUnit(36f);
                x += DrawFigureOverCaption(x, tally.y + StatsUnit(2f), agreement.Count(AgreementState.Delivered).ToString(CultureInfo.InvariantCulture), "DELIVERED", ink) + StatsUnit(36f);
                DrawFigureOverCaption(x, tally.y + StatsUnit(2f), agreement.Count(AgreementState.Broken).ToString(CultureInfo.InvariantCulture), "BROKEN", ink);

                int i = 0;
                foreach (AgreementItem item in agreement.Items)
                {
                    Rect row = ReserveRow(26f);
                    if (item.State != AgreementState.Owed) { DrawVerdictSlot(new Rect(row.x, row.y, StatsUnit(16f), row.height), item.State == AgreementState.Delivered); }
                    DrawAgreementItemPair(new Rect(row.x + StatsUnit(24f), row.y, StatsUnit(36f), row.height), item, ink);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + StatsUnit(68f), row.y, Mathf.Max(1f, row.width - StatsUnit(68f)), row.height), item.Name ?? item.LawId, DeskBody(12.5f, ink));
                    string id = "agreement/" + agreement.Supporter + "/" + i.ToString(CultureInfo.InvariantCulture);
                    SlipAnchor(row, id);
                    book.Anchors[id] = new SlipContent((item.Name ?? item.LawId).ToUpperInvariant())
                        .Add(item.State == AgreementState.Delivered ? "DELIVERED " + DeskDay(item.DeliveredOn)
                            : item.State == AgreementState.Broken ? "BROKEN " + DeskDay(item.BrokenOn)
                            : "OWED - A PROMISE NOT YET KEPT")
                        .Add("DEMANDED BY " + name);
                    DrawRowRule(row);
                    i++;
                }

                if (agreement.Supporter == _playerCountry.PlayerPartyAbbrev && !withdrawn && !_playerCountry.PlayerHoldsPresidency)   // SP-7b: no party verb while the player is President
                {
                    Rect act = ReserveRow(34f);
                    Rect chip = RowChipRectEndingAt(act.xMax, act, "WITHDRAW SUPPORT", 26f, 12f);
                    if (DrawRowChip(chip, "WITHDRAW SUPPORT", ChipFace.Paper))
                    {
                        if (!_simulationManager.WithdrawSupport(PlayerCountryId, out string refused)) { Debug.Log($"AGREEMENT: the withdrawal was refused - {refused}"); }
                    }
                    SlipAnchor(chip, "agreement/withdraw");
                    book.Anchors["agreement/withdraw"] = new SlipContent("WITHDRAW SUPPORT").Add("YOUR PARTY CARRIES THIS GOVERNMENT ON THESE ITEMS")
                        .Add("WITHDRAWING IS RECORDED AGAINST THE GOVERNMENT").Add("YOUR PARTY MAY THEN MOVE NO CONFIDENCE");
                }
                GUILayout.Space(StatsUnit(10f));
            }
        }

        /// <summary>21b: the caretaker line - CARETAKER · SINCE · the date as a stamp - where the government serves on as a caretaker.</summary>
        private void DrawCaretakerLine(PeopleSlips.Book book)
        {
            GovernmentRecord g = _playerCountry?.Government;
            if (g == null || !g.Caretaker) { return; }
            Rect row = ReserveRow(30f);
            float x = row.x;
            Rect stamp = RowChipRectFrom(x, row, "CARETAKER", padBoard: 6f);
            DrawRowChip(stamp, "CARETAKER", ChipFace.Outline);
            x = stamp.xMax + StatsUnit(10f);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            float sw = Mathf.Ceil(muted.CalcSize(new GUIContent("SINCE")).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, sw, row.height), "SINCE", muted);
            x += sw + StatsUnit(10f);
            string day = DeskDay(g.CaretakerSince);
            Rect date = RowChipRectFrom(x, row, day, padBoard: 6f);
            DrawRowChip(date, day, ChipFace.Outline);
            SlipAnchor(new Rect(row.x, row.y, date.xMax - row.x, row.height), "caretaker");
            book.Anchors["caretaker"] = new SlipContent("CARETAKER SINCE " + day).Add("THE OUTGOING GOVERNMENT SERVES ON")
                .Add(ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Bundestag ? "UNTIL THE BUNDESTAG ELECTS A CHANCELLOR (ART. 69 ABS. 3)"
                    : SejmFormation.Applies(PlayerCountryId) ? "UNTIL A NEW COUNCIL OF MINISTERS IS APPOINTED (ART. 162 UST. 3)" : "UNTIL A PROPOSAL WINS ITS INVESTITURE");   // §705; AF-8 the Sejm's words
            DrawRowRule(row);
        }

        /// <summary>
        /// §646 (premises 1, 4-5), redrawn from 21b: THE SPEAKER'S ROUND on the Parliament tab - its head with the rejections as a figure and the round's
        /// log on the slip; the stage as a row: the asked party's mark · ASKED FIRST · PROPOSAL DUE · the date; a tabled proposal and the day the chamber
        /// votes; an offer to the player's party (ACCEPT · DECLINE, both paper - no default); and where the player's party is asked, OPEN THE SHEET
        /// (brass, the act) and PASS.
        /// </summary>
        // -------- §705: the round's words by chamber - the Riksdag's Speaker's round (RF 6 kap.) or the Bundestag's chancellor election (Art. 63 GG) --------

        /// <summary>Who asks a party to form the government: the Riksdag's Speaker, or the Bundespräsident, who proposes the chancellor (Art. 63 Abs. 1 GG).</summary>
        private static string RoundAsker(SpeakerRound round) => round.Bundestag ? "THE BUNDESPRÄSIDENT" : "THE SPEAKER";

        private static string RoundTitle(SpeakerRound round) => round.Bundestag ? "The chancellor's election" : "The Speaker's round";

        /// <summary>The head's figure and its tail: the proposals rejected of the Riksdag's four, or the Art. 63 phase of three.</summary>
        private static string RoundFigure(SpeakerRound round) => (round.Bundestag ? System.Math.Max(1, round.Phase) : round.Rejections).ToString(CultureInfo.InvariantCulture);

        private static string RoundFigureTail(SpeakerRound round) => round.Bundestag ? "⁄ 3 PHASES" : "⁄ " + SpeakerRound.ProposalLimit.ToString(CultureInfo.InvariantCulture) + " REJECTED";

        /// <summary>The round's rule, two lines for a slip.</summary>
        private string[] RoundRule(SpeakerRound round)
        {
            // §714 (the review's defect 1): while the formation's time on record runs, the rule says so - no vote before its day
            if (round.FormationDue > _simulationManager.CurrentDate)
            {
                return new[] { round.Bundestag ? "PHASE 1 OF 3 · ART. 63 ABS. 1-2 GG" : string.Format(CultureInfo.InvariantCulture, "{0} OF {1} PROPOSALS REJECTED", round.Rejections, SpeakerRound.ProposalLimit),
                    "NO VOTE BEFORE " + DeskDay(round.FormationDue) + " · THE FORMATION'S TIME ON RECORD" };
            }
            if (!round.Bundestag)
            {
                return new[] { string.Format(CultureInfo.InvariantCulture, "{0} OF {1} PROPOSALS REJECTED", round.Rejections, SpeakerRound.ProposalLimit),
                    "THE CHAMBER VOTES ON THE " + Ordinal(SpeakerRound.VoteDays) + " DAY AFTER A PROPOSAL IS TABLED" };
            }
            switch (round.Phase)
            {
                case 2: return new[] { "PHASE 2 OF 3 · ART. 63 ABS. 3 GG", "UNTIL " + DeskDay(round.SecondPhaseUntil) + " ANY CANDIDATE WITH A MAJORITY OF THE MEMBERS" };
                case 3: return new[] { "PHASE 3 OF 3 · ART. 63 ABS. 4 GG", "THE MOST VOTES ELECT" };
                default: return new[] { "PHASE 1 OF 3 · ART. 63 ABS. 1-2 GG", "THE BUNDESPRÄSIDENT'S CANDIDATE NEEDS A MAJORITY OF THE MEMBERS" };
            }
        }

        /// <summary>When a proposal tabled today comes to its vote - the round's one rule (<see cref="SpeakerRound.VoteDayIfTabled"/>, §714): the Riksdag's
        /// fourth day after the Speaker submits it, the Bundestag's day once it has convened - and neither before the formation's time on record has run.</summary>
        private string RoundVoteWhen(SpeakerRound round, bool brief)
        {
            System.DateTime today = _simulationManager.CurrentDate;
            System.DateTime vote = round.VoteDayIfTabled(today);
            bool formationHolds = vote == round.FormationDue && vote > (round.Bundestag ? (today > round.Convenes ? today : round.Convenes) : today.AddDays(SpeakerRound.VoteDays));
            if (!round.Bundestag)
            {
                if (formationHolds) { return brief ? "VOTE ON " + DeskDay(vote) : "TABLED, THE SPEAKER SUBMITS IT WHEN THE FORMATION'S TIME ON RECORD HAS RUN; THE CHAMBER VOTES ON " + DeskDay(vote); }
                return brief ? "VOTE ON DAY " + SpeakerRound.VoteDays.ToString(CultureInfo.InvariantCulture) : "TABLED, THE CHAMBER VOTES ON THE " + Ordinal(SpeakerRound.VoteDays) + " DAY";
            }
            if (vote == today) { return brief ? "VOTE THE SAME DAY" : "TABLED, THE BUNDESTAG VOTES THE SAME DAY"; }
            if (formationHolds) { return brief ? "VOTE ON " + DeskDay(vote) : "TABLED, THE BUNDESTAG VOTES WHEN THE FORMATION'S TIME ON RECORD HAS RUN, ON " + DeskDay(vote); }
            return brief ? "VOTE ON " + DeskDay(vote) : "TABLED, THE BUNDESTAG VOTES WHEN IT CONVENES ON " + DeskDay(vote);
        }

        /// <summary>What passing does: the Speaker asks the next party; in the Bundestag the next party in the order stands its candidate.</summary>
        private static string RoundPassLine(SpeakerRound round) => !round.Bundestag ? "THE SPEAKER ASKS THE NEXT PARTY"
            : round.Phase >= 2 ? "YOUR PARTY NOMINATES NO ONE IN THE FOURTEEN DAYS" : "THE BUNDESPRÄSIDENT PROPOSES ANOTHER PARTY'S CANDIDATE";   // §706: a pass in the fourteen days is not re-asked

        private void DrawSpeakerRound(PeopleSlips.Book book)
        {
            SpeakerRound round = _simulationManager.RoundOf(PlayerCountryId);
            if (round == null) { return; }
            CountryId country = _playerCountry.Id;
            string Name(string key) => key != null ? PartySystems.ShortName(country, key) : "NO PARTY";
            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);

            Rect head = ReserveRow(30f);
            DrawBlockHead(head, RoundTitle(round), null, PoliSimTheme.TextPrimary);
            string tail = RoundFigureTail(round);
            float tw = Mathf.Ceil(muted.CalcSize(new GUIContent(tail)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(head.xMax - tw, head.y, tw, head.height), tail, muted);
            GUIStyle fig = DeskCaption(14f, PoliSimTheme.TextPrimary, true);
            string rejected = RoundFigure(round);
            float fw = Mathf.Ceil(fig.CalcSize(new GUIContent(rejected)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(head.xMax - tw - StatsUnit(4f) - fw, head.y, fw, head.height), rejected, fig);
            SlipAnchor(head, "round");
            var log = new SlipContent("THE ROUND " + DeskDated(round.Occasion).ToUpperInvariant());
            for (int i = System.Math.Max(0, round.Log.Count - 4); i < round.Log.Count; i++)
            {
                foreach (string line in SlipWrapped(string.Empty, DeskDated(round.Log[i])).Lines) { log.Add(line); }
            }
            book.Anchors["round"] = log;
            DrawRowRule(head);

            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            void DateStamp(System.DateTime d)
            {
                string day = DeskDay(d);
                Rect stamp = RowChipRectFrom(x, row, day, padBoard: 6f);
                DrawRowChip(stamp, day, ChipFace.Outline);
                x = stamp.xMax + StatsUnit(10f);
            }
            switch (round.Stage)
            {
                case RoundStage.Consulting when round.Asked == null:
                    // §706 (GO-BT § 4 Abs. 2): no nomination is signed by a quarter of the members - the fourteen days run out unballoted
                    Words("NO NOMINATION STANDS · THE FOURTEEN DAYS RUN TO", caption);
                    DateStamp(round.SecondPhaseUntil);
                    SlipAnchor(row, "round/stage");
                    book.Anchors["round/stage"] = new SlipContent("NO NOMINATION STANDS").Add("A NOMINATION NEEDS A QUARTER OF THE MEMBERS' SIGNATURES")
                        .Add("OR A FRAKTION OF A QUARTER (THE BUNDESTAG'S RULES OF PROCEDURE)").Add("THEN A BALLOT THE MOST VOTES WIN (ART. 63 ABS. 4)");
                    break;
                case RoundStage.Consulting:
                    Mark(round.Asked);
                    Words(round.Turn == 0 ? "ASKED FIRST · PROPOSAL DUE" : "ASKED · PROPOSAL DUE", caption);
                    DateStamp(round.AskedOn.AddDays(SpeakerRound.ConsultationDays));
                    SlipAnchor(row, "round/stage");
                    book.Anchors["round/stage"] = new SlipContent(Name(round.Asked) + (round.Turn == 0 ? " ASKED FIRST" : " ASKED"))
                        .Add(RoundAsker(round) + " ASKS " + Name(round.Asked) + " TO FORM A GOVERNMENT").Add("ITS PROPOSAL BY " + DeskDay(round.AskedOn.AddDays(SpeakerRound.ConsultationDays)));
                    break;
                case RoundStage.VotePending:
                    Mark(round.Proposal.Formateur);
                    Words("PROPOSES", caption);
                    foreach (string member in round.Proposal.CabinetParties) { Mark(member); }
                    Words("· THE CHAMBER VOTES", muted);
                    DateStamp(round.VoteOn);
                    SlipAnchor(row, "round/stage");
                    book.Anchors["round/stage"] = new SlipContent(UiFormat.Possessive(Name(round.Proposal.Formateur), true, true) + " PROPOSAL")
                        .Add("THE CABINET " + PartySystems.JoinShortNames(round.Proposal.CabinetParties.ConvertAll(Name)))
                        .Add(round.Proposal.Supporters.Count > 0 ? "SUPPORTED BY " + PartySystems.JoinShortNames(round.Proposal.Supporters.ConvertAll(Name)) : "NO SUPPORT FROM OUTSIDE")
                        .Add("THE CHAMBER VOTES ON " + DeskDay(round.VoteOn));
                    break;
                case RoundStage.OfferToPlayer:
                {
                    string you = _playerCountry.PlayerPartyAbbrev;
                    bool cabinet = round.Proposal.CabinetParties.Contains(you);
                    Mark(round.Asked);
                    Words(cabinet ? "OFFERS YOUR PARTY" : "ASKS YOUR PARTY TO SUPPORT", caption);
                    if (cabinet)
                    {
                        GUIStyle figure = DeskCaption(13f, PoliSimTheme.TextPrimary, true);
                        Words(round.Proposal.PostsOf(you).ToString(CultureInfo.InvariantCulture), figure);
                        x -= StatsUnit(6f);
                        Words(round.Proposal.PostsOf(you) == 1 ? "POST" : "POSTS", muted);
                    }
                    SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "round/stage");
                    book.Anchors["round/stage"] = new SlipContent(Name(round.Asked) + (cabinet ? " OFFERS A PLACE IN THE CABINET" : " ASKS FOR SUPPORT FROM OUTSIDE"))
                        .Add("THE CABINET " + PartySystems.JoinShortNames(round.Proposal.CabinetParties.ConvertAll(Name)))
                        .Add(cabinet ? "YOUR PARTY'S POSTS: " + Posts(round.Proposal.PostsOf(you)) : "ON AN AGREEMENT")
                        .Add("DECLINING LEAVES YOUR PARTY IN OPPOSITION");
                    Rect decline = RowChipRectEndingAt(row.xMax, row, "DECLINE", 26f, 12f);
                    Rect accept = RowChipRectEndingAt(decline.x - StatsUnit(8f), row, "ACCEPT THE OFFER", 26f, 12f);
                    if (DrawRowChip(accept, "ACCEPT THE OFFER", ChipFace.Paper))
                    {
                        if (!_simulationManager.AnswerOffer(PlayerCountryId, true, out string refused)) { Debug.Log($"SPEAKER: refused - {refused}"); }
                    }
                    if (DrawRowChip(decline, "DECLINE", ChipFace.Paper))
                    {
                        if (!_simulationManager.AnswerOffer(PlayerCountryId, false, out string refused)) { Debug.Log($"SPEAKER: refused - {refused}"); }
                    }
                    SlipAnchor(decline, "round/decline");
                    book.Anchors["round/decline"] = new SlipContent("DECLINE").Add("YOUR PARTY STAYS IN OPPOSITION").Add(RoundAsker(round) + " MOVES ON IF YOUR SEATS WERE NEEDED");
                    break;
                }
                case RoundStage.PlayerAsked:
                {
                    FormationProposal current = FormationDraft();
                    Mark(_playerCountry.PlayerPartyAbbrev);
                    Words(round.Turn == 0 ? "ASKED FIRST" : "ASKED", caption);
                    Rect yours = RowChipRectFrom(x, row, "YOUR PARTY", padBoard: 6f);
                    DrawRowChip(yours, "YOUR PARTY", ChipFace.Outline);
                    SlipAnchor(new Rect(row.x, row.y, yours.xMax - row.x, row.height), "round/stage");
                    book.Anchors["round/stage"] = new SlipContent(RoundAsker(round) + " ASKS YOUR PARTY")
                        .Add("TO FORM A GOVERNMENT")
                        .Add("THE SHEET OPENS ON " + PartySystems.JoinShortNames(current.CabinetParties.ConvertAll(Name)) + (current.Supporters.Count > 0 ? " WITH " + PartySystems.JoinShortNames(current.Supporters.ConvertAll(Name)) + " SUPPORTING" : string.Empty))
                        .Add("THE FORMATION'S OWN PROPOSAL WITH YOUR PARTY LEADING");
                    Rect open = RowChipRectEndingAt(row.xMax, row, "OPEN THE SHEET", 26f, 12f);
                    Rect pass = RowChipRectEndingAt(open.x - StatsUnit(8f), row, "PASS", 26f, 12f);
                    if (DrawRowChip(open, "OPEN THE SHEET", ChipFace.Brass)) { OpenFormationSheet(); }
                    if (DrawRowChip(pass, "PASS", ChipFace.Paper))
                    {
                        if (!_simulationManager.PassFormation(PlayerCountryId, out string refused)) { Debug.Log($"SPEAKER: refused - {refused}"); }
                    }
                    SlipAnchor(pass, "round/pass");
                    book.Anchors["round/pass"] = new SlipContent("PASS").Add(RoundPassLine(round));
                    break;
                }
                default:
                    Words("THE ROUND IS OVER", muted);
                    break;
            }
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        /// <summary>
        /// §705 (round 4 follow-up 4): HISTORY AS THE REFERENCE on the Parliament tab, where no election night carries it (Germany - the night is
        /// Sweden's until the Länder night, D-DE): after an election the game held on a polling day of record, the row reads AS IT HAPPENED · the day
        /// the government of record took office · its head's mark and surname · IN CABINET and its parties' marks; the slip carries the real seats
        /// party by party and the record's basis. It stands while that election's chamber sits.
        /// </summary>
        // §705 (the review's cost note): the record's reference is read once per election held, not on every OnGUI event (a seat table and a regex)
        private System.DateTime _referenceHeld = System.DateTime.MinValue;
        private CountryId _referenceCountry;
        private WorldClock.Reference _referenceCached;

        private void DrawReferenceRow(PeopleSlips.Book book)
        {
            // §767: Poland too - and since D-PL its night carries the reference at 41 of 41 as the German night does; the row keeps it while that chamber sits
            if ((ConfidenceProcedure.RulesOf(PlayerCountryId) != ConfidenceProcedure.Rules.Bundestag && PlayerCountryId != CountryId.Poland) || _playerCountry.ElectionHistory == null) { return; }
            System.DateTime held = System.DateTime.MinValue;
            foreach (ElectionRecord e in _playerCountry.ElectionHistory)
            {
                if (NationalElection.CountsParliament(e.Method) && e.Date <= _simulationManager.CurrentDate && e.Date > held) { held = e.Date; }
            }
            if (held == System.DateTime.MinValue) { return; }
            if (held != _referenceHeld || _referenceCountry != PlayerCountryId)
            {
                _referenceHeld = held;
                _referenceCountry = PlayerCountryId;
                _referenceCached = WorldClock.TryReference(PlayerCountryId, held, out WorldClock.Reference fresh) ? fresh : null;
            }
            WorldClock.Reference reference = _referenceCached;
            if (reference == null) { return; }
            CountryId country = _playerCountry.Id;
            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            Words(held.Year.ToString(CultureInfo.InvariantCulture) + ", AS IT HAPPENED", caption);
            if (reference.HeadParty != null)
            {
                string day = DeskDay(reference.HeadFrom);
                Rect stamp = RowChipRectFrom(x, row, day, padBoard: 6f);
                DrawRowChip(stamp, day, ChipFace.Outline);
                x = stamp.xMax + StatsUnit(10f);
                Mark(reference.HeadParty);
                Words(reference.HeadSurname.ToUpperInvariant(), caption);
                if (reference.CabinetOfRecord != null)
                {
                    Words("IN CABINET", muted);
                    foreach (string member in reference.CabinetOfRecord) { Mark(member); }
                }
            }
            else
            {
                Words("NO GOVERNMENT OF RECORD AFTER IT", muted);
            }
            if (!string.IsNullOrEmpty(reference.HistoryNote)) { Words("LOW CONFIDENCE", muted); }   // §767 (Elias's ruling D1): the played vote's past by lineage - the note on the slip
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "reference");
            var slip = new SlipContent(reference.Label);
            var seated = new List<(string key, int seats)>();
            foreach (KeyValuePair<string, int> kv in reference.Seats) { if (kv.Value > 0) { seated.Add((kv.Key, kv.Value)); } }
            seated.Sort((a, b) => b.seats != a.seats ? b.seats.CompareTo(a.seats) : string.CompareOrdinal(a.key, b.key));
            var line = new System.Text.StringBuilder();
            foreach ((string key, int seats) in seated)
            {
                string part = PartySystems.ShortName(country, key).ToUpperInvariant() + " " + seats.ToString(CultureInfo.InvariantCulture);
                if (line.Length > 0 && line.Length + part.Length + 3 > 44) { slip.Add(line.ToString()); line.Clear(); }
                if (line.Length > 0) { line.Append(" · "); }
                line.Append(part);
            }
            if (line.Length > 0) { slip.Add(line.ToString()); }
            foreach (string l in SlipWrapped(string.Empty, reference.GovernmentLine).Lines) { slip.Add(l); }
            if (!string.IsNullOrEmpty(reference.HistoryNote)) { foreach (string l in SlipWrapped(string.Empty, reference.HistoryNote.ToUpperInvariant()).Lines) { slip.Add(l); } }   // §767 (D1)
            slip.Add("THE GAME'S ELECTION IS ITS OWN - THIS IS THE RECORD");
            book.Anchors["reference"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        /// <summary>
        /// PS-6 US-2 (Elias's ruling F8, R-US1 (a)): THE RECORD, BY DATE - where the game does not elect the chamber (`WorldClock.RecordSeatsChamber`; the
        /// USA's, until US-16), the row says which House of record sits today and from when, and the president of record with it while the record seats the
        /// executive (`WorldClock.RecordSeatsExecutive`; since US-8, to the game's own polling day - the president row serves the office from it, <see cref="DrawUsPresidentRow"/>);
        /// past the record - a House elected after the record's date, a president whose term of record has ended - that the last of record stands and the
        /// day its term ended (R-US1 (a): "the 119th stands, said on screen"; the president "until the game holds the presidential election"). The words
        /// are `WorldClock`'s (`RecordStanding`, `ExecutiveStanding`, `TryPresidentOfRecord`); the slip whole. PS-6 US-15: the slip names the Senate of record
        /// too where the record seats it (`WorldClock.RecordSeatsSecondChamber`) - its standing (`WorldClock.SecondChamberStanding`) and its seats as seated; the
        /// chamber itself is drawn as its own card under the House's (<see cref="DrawSecondChamberV35"/>).
        /// </summary>
        private void DrawRecordRow(PeopleSlips.Book book)
        {
            if (_playerCountry == null || !WorldClock.RecordSeatsChamber(_playerCountry, _simulationManager.CurrentDate)) { return; }   // PS-6 US-16: not once the game's own House sits
            System.DateTime today = _simulationManager.CurrentDate;
            CountryId country = _playerCountry.Id;
            WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(country, today);
            bool onRecord = PartySystems.SeatsSourced(chamber.Vintage);
            WorldClock.GovernmentOfRecord ofRecord = default;
            string president = null, surname = null;
            bool head = WorldClock.RecordSeatsExecutive(_playerCountry, today) && WorldClock.TryGovernmentAt(country, today, out ofRecord)   // PS-6 US-7: not once the game has held its own presidential election
                        && WorldClock.TryPresidentOfRecord(country, today, out president, out surname);
            System.DateTime termEnds = head ? WorldClock.TermOfRecordEnds(ofRecord) : System.DateTime.MaxValue;

            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            void Chip(string day)
            {
                Rect stamp = RowChipRectFrom(x, row, day, padBoard: 6f);
                DrawRowChip(stamp, day, ChipFace.Outline);
                x = stamp.xMax + StatsUnit(10f);
            }
            string largest = null;
            int most = -1;
            foreach (KeyValuePair<string, int> kv in _playerCountry.ParliamentSeats) { if (kv.Value > most || (kv.Value == most && string.CompareOrdinal(kv.Key, largest) < 0)) { largest = kv.Key; most = kv.Value; } }

            Words("OF RECORD, BY DATE", caption);
            Words("THE HOUSE", muted);
            if (largest != null && most > 0) { Mark(largest); }   // the majority of the House seated - past the record, of the one that stands
            if (onRecord) { Words("SEATED", muted); Chip(DeskDay(chamber.Convened)); }
            else { Words("STANDS · TERM ENDED", muted); Chip(DeskDay(WorldClock.ChamberSeatedOn(country, today).Until)); }
            if (head)
            {
                Words("THE PRESIDENT", muted);
                if (ofRecord.Cabinet != null && ofRecord.Cabinet.Length > 0) { Mark(ofRecord.Cabinet[0]); }
                Words(surname.ToUpperInvariant(), caption);
                if (today < termEnds) { Chip(DeskDay(ofRecord.From)); }
                else { Words("STANDS · TERM ENDED", muted); Chip(DeskDay(termEnds)); }
            }
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "record");

            var slip = new SlipContent("OF RECORD, BY DATE");
            // PS-6 US-8: the game elects the president and not the House - the record's president serves until the oath of the game's own election's winner (the
            // record stops naming presidents from the polling day, and the president row takes over then); the review's L2: not "until the election"
            // PS-6 US-15: and not the Senate - the record's, senator by senator on its dates
            bool upper = WorldClock.RecordSeatsSecondChamber(country);
            // PS-6 US-16: the game elects the House and a Senate class on its polling day - until its Congress is seated, the record's sits
            slip.Add((upper ? "UNTIL THE CONGRESS THE GAME ELECTS IS SEATED, THE RECORD'S HOUSE AND SENATE SIT ON THE RECORD'S DATES" : "UNTIL THE HOUSE THE GAME ELECTS IS SEATED, THE RECORD'S HOUSE SITS ON THE RECORD'S DATES")
                     + (head ? ", AND ITS PRESIDENT SERVES UNTIL THE WINNER OF THE GAME'S OWN ELECTION TAKES THE OATH" : string.Empty)
                     + "; PAST THE RECORD, THE LAST OF RECORD STANDS");
            var seated = new List<(string key, int seats)>();
            foreach (KeyValuePair<string, int> kv in _playerCountry.ParliamentSeats) { if (kv.Value > 0) { seated.Add((kv.Key, kv.Value)); } }
            seated.Sort((a, b) => b.seats != a.seats ? b.seats.CompareTo(a.seats) : string.CompareOrdinal(a.key, b.key));
            var seats = new System.Text.StringBuilder();
            foreach ((string key, int n) in seated) { if (seats.Length > 0) { seats.Append(" · "); } seats.Append(PartySystems.ShortName(country, key).ToUpperInvariant()).Append(' ').Append(n.ToString(CultureInfo.InvariantCulture)); }
            slip.Add("THE HOUSE: " + WorldClock.RecordStanding(country, today) + (seats.Length > 0 ? " · " + seats : string.Empty));
            if (head)
            {
                // §770's order - the name, the party in words (the row shows it as a mark only), the office
                string party = ofRecord.Cabinet != null && ofRecord.Cabinet.Length > 0 ? " · " + PartySystems.ShortName(country, ofRecord.Cabinet[0]).ToUpperInvariant() : string.Empty;
                slip.Add("THE PRESIDENT: " + president.ToUpperInvariant() + party + " · " + WorldClock.ExecutiveStanding(country, today));
            }
            if (upper)
            {
                // PS-6 US-15: the Senate of record - its standing in WorldClock's words, then its seats as seated, the independents counted with their conference
                UsSenate.Seating senate = UsSenate.On(today);
                slip.Add("THE " + StartBrief.SecondChamberOf(country).ToUpperInvariant() + ": " + WorldClock.SecondChamberStanding(country, today) + SecondChamberSeatsWords(country, _playerCountry.SecondChamberSeats, senate));
            }
            foreach (string next in WorldClock.NextOfRecord(_playerCountry, today)) { slip.Add(next); }   // the House's day and the oath named before they come - the record's oath only while the record seats the president (PS-6 US-7)
            book.Anchors["record"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        /// <summary>
        /// §770 (PS-5 item C4): THE PRESIDENT - who holds the office the veto reads (the record's until the game's own election seats a successor),
        /// a president-elect waiting for the oath, a run-off the first vote has called, or the next first vote; the game's rounds on the slip. Drawn
        /// where the country elects its president in two rounds.
        /// </summary>
        private void DrawPresidentRow(PeopleSlips.Book book)
        {
            if (_playerCountry == null || TwoRoundElection.RuleOf(PlayerCountryId) == null) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            if (!PresidentialElection.PresidentAt(PlayerCountryId, _playerCountry.PresidentialElections, today, out PresidencyOfRecord.President president)) { return; }
            PresidentialElection.Contest last = null;
            foreach (PresidentialElection.Contest c in _playerCountry.PresidentialElections) { if (c.FirstVote <= today && (last == null || c.FirstVote > last.FirstVote)) { last = c; } }
            bool elect = last != null && last.Decided() && last.TakesOffice > today;
            bool runOffAhead = last != null && last.RunOffPending();
            bool undecided = last != null && !last.Decided() && !last.RunOffPending() && last.TakesOffice > today;   // a tie, or a finalist with no position (the count's own line says which) - shown until the term it was for ends
            bool next = PresidentialElection.TryNextFirstVote(_playerCountry, today, out System.DateTime firstVote, out _, out string dayBasis);
            bool ofGame = PresidentialElection.ElectedInGame(president);
            CountryId country = _playerCountry.Id;

            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            void Chip(string day)
            {
                Rect stamp = RowChipRectFrom(x, row, day, padBoard: 6f);
                DrawRowChip(stamp, day, ChipFace.Outline);
                x = stamp.xMax + StatsUnit(10f);
            }
            Words("THE PRESIDENT", caption);
            if (president.BackingParty != null) { Mark(president.BackingParty); }
            Words(Surname(president.Name).ToUpperInvariant(), caption);
            Words(ofGame ? "ELECTED IN THE GAME" : "OF RECORD", muted);
            if (elect)
            {
                Words("ELECT", muted);
                if (last.ElectedParty != null) { Mark(last.ElectedParty); }
                Words(Surname(last.Elected).ToUpperInvariant(), caption);
                Chip(DeskDay(last.TakesOffice));
            }
            else if (runOffAhead)
            {
                Words("RUN-OFF", muted);
                Chip(DeskDay(last.RunOffOn));
            }
            else
            {
                if (undecided) { Words("NOTHING DECIDED", muted); }
                if (next)
                {
                    Words("NEXT VOTE", muted);
                    Chip(DeskDay(firstVote));
                }
            }
            string yours = PlayerPresidentialWords(today, out string yoursLine);   // SP-7b: the verdict for the player, where he stands a candidate
            if (yours != null) { Words(yours, caption); }
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "president");

            var slip = new SlipContent("THE PRESIDENT");
            if (yoursLine != null) { slip.Add(yoursLine); }
            slip.Add(president.Name.ToUpperInvariant() + (president.BackingParty != null ? " · " + PartySystems.ShortName(country, president.BackingParty).ToUpperInvariant() : string.Empty)
                + " · IN OFFICE SINCE " + DeskDay(president.TookOffice));
            slip.Add(ofGame ? "ELECTED IN THE GAME'S OWN ELECTION" : "OF RECORD - THE GAME'S OWN ELECTION SEATS THE NEXT");
            // §770: whole lines - the slip's box wraps them to its own width (a pre-wrapped line is wrapped twice); F3: the rule at risk, the president's own
            // rate - and where no seated party backs the president, no statute is at risk (the slips' own reading, ChamberVerdicts.VetoLine)
            bool backedInChamber = president.BackingParty != null && _playerCountry.ParliamentSeats.TryGetValue(president.BackingParty, out int backingSeats) && backingSeats > 0;
            double vetoRate = PoliSim.Elections.PresidentialVeto.RateOf(president.Name, out string rateBasis);
            slip.Add(!backedInChamber
                ? (president.BackingParty == null ? "NO PARTY BACKS THE PRESIDENT" : "THE PRESIDENT'S BACKING PARTY HOLDS NO SEAT") + ", SO NO STATUTE IS AT RISK OF A VETO - THE SEJM OVERRIDES ONE BY 3/5 OF THOSE VOTING"
                : "A STATUTE THE PRESIDENT'S BACKING PARTY DOES NOT VOTE FOR IS AT RISK - VETOED AT " + (vetoRate * 100.0).ToString("0", CultureInfo.InvariantCulture)
                    + " %, " + rateBasis.ToUpperInvariant() + " - THE SEJM OVERRIDES BY 3/5 OF THOSE VOTING");
            if (last != null)
            {
                slip.Add("FIRST VOTE " + DeskDay(last.FirstVote) + " - " + DayWords(last.DayBasis));
                foreach (PresidentialElection.Candidate c in last.Field)
                {
                    string who = Surname(c.Name).ToUpperInvariant();
                    bool unnamed = UsTermLimit.IsUnnamedLabel(c.Name);   // the label already names the party
                    // §772 (E1): a candidate of record no roster party backs (Braun, Zandberg, Stanowski ...) has no party to name
                    string party = unnamed || c.Party == null ? string.Empty : " (" + PartySystems.ShortName(country, c.Party).ToUpperInvariant() + ")";
                    slip.Add(who + party + " " + c.Share.ToString("0.00", CultureInfo.InvariantCulture) + " %");
                }
                if (last.RunOffHeld && last.Decided())
                {
                    slip.Add("RUN-OFF " + DeskDay(last.RunOffOn) + " · " + Surname(last.RunOffA).ToUpperInvariant() + " " + last.RunOffShareA.ToString("0.00", CultureInfo.InvariantCulture) + " % OF THE TWO");
                }
                else if (runOffAhead)
                {
                    slip.Add("RUN-OFF " + DeskDay(last.RunOffOn) + " · " + Surname(last.RunOffA).ToUpperInvariant() + " AND " + Surname(last.RunOffB).ToUpperInvariant());
                }
                if (undecided) { slip.Add(last.Line.ToUpperInvariant()); }
                foreach (string n in last.NotStanding) { slip.Add("NOT STANDING: " + n.ToUpperInvariant()); }
            }
            if (next && !elect && !runOffAhead)
            {
                slip.Add("NEXT FIRST VOTE " + DeskDay(firstVote) + " - " + DayWords(dayBasis));
            }
            book.Anchors["president"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        // the reference the row's slip reads, cached per polling day and country (the review's L11, DrawReferenceRow's pattern)
        private System.DateTime _usReferenceFor = System.DateTime.MinValue;
        private CountryId _usReferenceCountry;
        private WorldClock.Reference _usReference;

        /// <summary>
        /// PS-6 US-8 (the plan: "The Parliament page's president row serves the USA"): THE US PRESIDENT, from the game's own count - drawn once the game has held a
        /// presidential election (before it the record row names the president of record, <see cref="DrawRecordRow"/>). The row: who holds the office (the
        /// presidency governing - ELECTED IN THE GAME from the oath, else OF RECORD), the president-elect and the oath's day, the House's choice still to come or a
        /// House that could not choose, the next polling day. The slip: the count - each nominee's electors and vote, every other candidate's vote, the dates of the
        /// electors' meeting, the count before Congress and the oath, the House's states - and the record beside it (`WorldClock.TryReference`: the record's
        /// electors and the president it shows taking office). Words only; D-US composes the night.
        /// </summary>
        private void DrawUsPresidentRow(PeopleSlips.Book book)
        {
            if (_playerCountry == null || !UsPresidentialElection.ElectsIn(PlayerCountryId)) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            if (!UsPresidentialElection.TryLatest(_playerCountry, today, out UsPresidentialElection.Contest last)) { return; }
            GovernmentRecord governing = _playerCountry.Government;
            if (governing == null || governing.Kind != WorldClock.ExecutiveKind.Presidency || string.IsNullOrEmpty(governing.Executive)) { return; }
            CountryId country = _playerCountry.Id;
            bool ofGame = governing.Outcome == UsPresidentialElection.ElectedOutcome;
            int tag = governing.Executive.LastIndexOf(" (", System.StringComparison.Ordinal);
            string name = tag > 0 ? governing.Executive.Substring(0, tag) : governing.Executive;
            System.DateTime since = governing.FormedOn;
            if (!ofGame && WorldClock.TryGovernmentAt(country, today, out WorldClock.GovernmentOfRecord ofRecord) && ofRecord.President == governing.Executive) { since = ofRecord.From; }
            bool elect = UsPresidentialElection.TryPresidentElect(_playerCountry, today, out UsPresidentialElection.Contest electContest);
            bool houseAhead = last.Contingent && !last.Decided() && last.Delegations.Count == 0 && last.CountedOn.Date >= today.Date;
            bool undecided = !last.Decided() && !houseAhead;
            bool next = UsPresidentialElection.TryNextPresidentialDay(country, today.AddDays(1), out System.DateTime nextDay);   // PS-6 US-27: the president's next vote, never a midterm's

            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            void Chip(string day)
            {
                Rect stamp = RowChipRectFrom(x, row, day, padBoard: 6f);
                DrawRowChip(stamp, day, ChipFace.Outline);
                x = stamp.xMax + StatsUnit(10f);
            }
            Words("THE PRESIDENT", caption);
            if (!string.IsNullOrEmpty(governing.PmParty)) { Mark(governing.PmParty); }
            // PS-6 US-16 - R-US22: the Speaker acting is named by office, not by a surname the record does not give
            bool speakerActs = governing.Outcome == UsPresidentialElection.ActingOutcome && UsPresidentialElection.TryActingOn(_playerCountry, today, out UsPresidentialElection.Contest actingFor)
                               && actingFor.ActingAs == UsPresidentialElection.ActsAsSpeaker;   // the contest the acting office serves for, not the latest held
            Words(speakerActs ? "THE SPEAKER" : Surname(name).ToUpperInvariant(), caption);
            Words(ofGame ? "ELECTED IN THE GAME" : governing.Outcome == UsPresidentialElection.ActingOutcome ? "ACTING" : "OF RECORD", muted);
            if (elect)
            {
                Words("ELECT", muted);
                if (electContest.WinnerParty != null) { Mark(electContest.WinnerParty); }
                Words(Surname(electContest.Winner).ToUpperInvariant(), caption);
                Chip(DeskDay(electContest.TakesOffice));
            }
            else if (houseAhead)
            {
                Words("THE HOUSE CHOOSES", muted);
                Chip(DeskDay(last.CountedOn));
            }
            else
            {
                if (undecided) { Words("NOTHING DECIDED", muted); }
                if (next)
                {
                    Words("NEXT VOTE", muted);
                    Chip(DeskDay(nextDay));
                }
            }
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "uspresident");

            var slip = new SlipContent("THE PRESIDENT");
            slip.Add(name.ToUpperInvariant() + (string.IsNullOrEmpty(governing.PmParty) ? string.Empty : " · " + PartySystems.ShortName(country, governing.PmParty).ToUpperInvariant())
                + " · IN OFFICE SINCE " + DeskDay(since));
            slip.Add(ofGame ? "ELECTED IN THE GAME'S OWN ELECTION" : governing.Outcome == UsPresidentialElection.ActingOutcome ? "ACTING AS PRESIDENT - NO ONE WAS ELECTED BY THE OATH" : "OF RECORD - THE GAME'S OWN ELECTION SEATS THE NEXT AT THE OATH");
            slip.Add("POLLING DAY " + DeskDay(last.PollingDay) + " - THE TUESDAY AFTER THE FIRST MONDAY IN NOVEMBER, EVERY FOURTH YEAR");
            foreach (UsPresidentialElection.Tally t in last.Electors)
            {
                string share = double.IsNaN(t.Popular()) ? string.Empty : " · " + t.Popular().ToString("0.00", CultureInfo.InvariantCulture) + " % OF THE VOTE";   // PS-6 K29: the popular vote re-summed from the counted states
                slip.Add(t.Party == null
                    ? t.Candidate.ToUpperInvariant() + share
                    : Surname(t.Candidate).ToUpperInvariant() + " (" + PartySystems.ShortName(country, t.Party).ToUpperInvariant() + ") " + t.Votes.ToString(CultureInfo.InvariantCulture) + " ELECTORS" + share);
            }
            slip.Add("THE ELECTORS MEET " + DeskDay(last.ElectorsMeet) + " · CONGRESS COUNTS " + DeskDay(last.CountedOn) + " · THE OATH " + DeskDay(last.TakesOffice));
            // R-US13 (a), RULED: electors carried forward are labelled on screen - derived from the contest's year, never saved (the review's M2)
            if (UsPresidentialElection.ElectorsCarriedForward(last.PollingDay, out int electorsYear))
            {
                slip.Add("THE ELECTORS ARE THOSE IN FORCE IN " + electorsYear.ToString(CultureInfo.InvariantCulture) + ", CARRIED FORWARD - NO LATER APPORTIONMENT IS ON RECORD");
            }
            if (last.Contingent)
            {
                slip.Add("NO MAJORITY OF THE " + last.ElectorsAppointed.ToString(CultureInfo.InvariantCulture) + " ELECTORS - THE HOUSE CHOOSES BY STATES, ONE VOTE A STATE (THE TWELFTH AMENDMENT)");
                foreach (UsPresidentialElection.Tally t in last.Delegations) { slip.Add(Surname(t.Candidate).ToUpperInvariant() + " " + t.Votes.ToString(CultureInfo.InvariantCulture) + " STATES"); }
            }
            if (undecided)
            {
                // PS-6 US-16 - R-US22 as ruled: the Senate chooses the Vice President, who acts as President from the oath; else the Speaker acts - never the sitting president
                slip.Add("NO ONE IS ELECTED - THE HOUSE COULD NOT CHOOSE");
                foreach (UsPresidentialElection.Tally t in last.SenateChoice) { slip.Add("THE SENATE, FOR VICE PRESIDENT: " + t.Candidate.ToUpperInvariant() + " (" + PartySystems.ShortName(country, t.Party).ToUpperInvariant() + ") " + t.Votes.ToString(CultureInfo.InvariantCulture) + " SENATORS"); }
                slip.Add(last.Acting == null
                    ? "FROM THE OATH THE VICE PRESIDENT THE SENATE CHOOSES ACTS AS PRESIDENT, OR, IF THE SENATE CANNOT CHOOSE EITHER, THE SPEAKER - NEVER THE SITTING PRESIDENT"
                    : last.Acting.ToUpperInvariant() + " (" + PartySystems.ShortName(country, last.ActingParty).ToUpperInvariant() + ") ACTS AS PRESIDENT - THE HOUSE VOTES ONCE, SO TO THE NEXT OATH"
                      + (last.ActingAs == UsPresidentialElection.ActsAsSpeaker ? " - THE SENATE COULD NOT CHOOSE A VICE PRESIDENT EITHER" : " - THE VICE PRESIDENT THE SENATE CHOSE"));
            }
            // the record's reference, read once per polling day and country, not on every OnGUI event (DrawReferenceRow's cost note, the review's L11)
            if (last.PollingDay != _usReferenceFor || country != _usReferenceCountry)
            {
                _usReferenceFor = last.PollingDay;
                _usReferenceCountry = country;
                _usReference = WorldClock.TryReference(country, last.PollingDay, out WorldClock.Reference fresh) ? fresh : null;
            }
            WorldClock.Reference reference = _usReference;
            if (reference != null && reference.Electors != null)
            {
                var record = new System.Text.StringBuilder(reference.Label + ": ");
                foreach (KeyValuePair<string, int> kv in reference.Electors) { record.Append(PartySystems.ShortName(country, kv.Key).ToUpperInvariant()).Append(' ').Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append(" · "); }
                record.Append(reference.GovernmentLine);
                slip.Add(record.ToString());
                slip.Add("THE GAME'S ELECTION IS ITS OWN - THIS IS THE RECORD");
            }
            if (next && !elect && !houseAhead) { slip.Add("NEXT PRESIDENTIAL POLLING DAY " + DeskDay(nextDay)); }   // PS-6 US-27: a midterm comes between
            slip.Add(UsCongressElection.Held(_playerCountry, today) ? "THE HOUSE AND THE SENATE SEATS UP WERE ELECTED ON THE SAME DAY - SEE CONGRESS" : "THE HOUSE AND THE SENATE SEATS UP ARE ELECTED ON THE SAME DAY");
            book.Anchors["uspresident"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        // the Congress row's reference, read once per polling day and country (the president row's cost note)
        private System.DateTime _usCongressReferenceFor = System.DateTime.MinValue;
        private CountryId _usCongressReferenceCountry;
        private WorldClock.Reference _usCongressReference;

        /// <summary>
        /// PS-6 US-16 (the plan: "The reference shows the record's 220/215 and the 119th Senate of record"): THE CONGRESS THE GAME ELECTED - drawn once the game
        /// has held its own Congress election (before it the record row names the House of record, <see cref="DrawRecordRow"/>). The row: CONGRESS, ELECTED IN
        /// THE GAME; the House's majority; the Senate's majority as seated; the day the new Congress sits. The slip: the House's count (REP and DEM of 435, the
        /// delegations by class), the Senate seats the game elected by side, the Senate as seated (the seats the game did not elect stay the record's, by date),
        /// and the record beside it - the House elected that day and the Senate of record as the Congress opened (`WorldClock.TryReference`). Words only; D-US
        /// composes the night.
        /// </summary>
        private void DrawUsCongressRow(PeopleSlips.Book book)
        {
            if (_playerCountry == null || !UsCongressElection.ElectsIn(PlayerCountryId)) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            if (!UsCongressElection.TryLatest(_playerCountry, today, out UsCongressElection.Contest last)) { return; }
            CountryId country = _playerCountry.Id;
            bool seated = last.Convenes.Date <= today.Date;
            IReadOnlyDictionary<string, int> senate = _playerCountry.SecondChamberSeats;
            string houseMajority = last.HouseRep > last.HouseDem ? "REP" : last.HouseDem > last.HouseRep ? "DEM" : null;
            string senateMajority = seated && senate != null ? UsSenate.MajorityOf(senate, UsSenate.VicePresidentParty(_playerCountry, today)) : null;

            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            void Mark(string key)
            {
                DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, key);
                x += StatsUnit(16f) + StatsUnit(8f);
            }
            Words("CONGRESS", caption);
            Words("ELECTED IN THE GAME", muted);
            Words("THE HOUSE", muted);
            if (houseMajority != null) { Mark(houseMajority); }
            if (seated)
            {
                Words("THE SENATE", muted);
                if (senateMajority != null) { Mark(senateMajority); }
            }
            Words(seated ? "SEATED" : "SITS FROM", muted);
            Rect stamp = RowChipRectFrom(x, row, DeskDay(last.Convenes), padBoard: 6f);
            DrawRowChip(stamp, DeskDay(last.Convenes), ChipFace.Outline);
            x = stamp.xMax + StatsUnit(10f);
            // PS-6 US-27: once the Congress sits, the next Congress vote - every even year's, a midterm's included (2 U.S.C. §7)
            System.DateTime nextDay = System.DateTime.MinValue;
            bool nextVote = seated && WorldClock.TryNextPollingDay(country, today.AddDays(1), out nextDay);
            if (nextVote)
            {
                Words("NEXT VOTE", muted);
                Rect nextStamp = RowChipRectFrom(x, row, DeskDay(nextDay), padBoard: 6f);
                DrawRowChip(nextStamp, DeskDay(nextDay), ChipFace.Outline);
                x = nextStamp.xMax + StatsUnit(10f);
            }
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "uscongress");

            string Short(string key) => PartySystems.ShortName(country, key).ToUpperInvariant();
            string N(int n) => n.ToString(CultureInfo.InvariantCulture);
            bool midterm = UsCongressElection.IsMidterm(last.PollingDay);   // PS-6 US-27
            var slip = new SlipContent("CONGRESS · ELECTED " + DeskDay(last.PollingDay) + (midterm ? " · A MIDTERM" : string.Empty));
            int delegationsR = 0, delegationsD = 0;
            foreach (UsCongressElection.StateHouse s in last.House) { if (s.Rep > s.Dem) { delegationsR++; } else if (s.Dem > s.Rep) { delegationsD++; } }
            slip.Add("THE HOUSE: " + Short("REP") + " " + N(last.HouseRep) + " · " + Short("DEM") + " " + N(last.HouseDem) + " OF " + N(UsCongressElection.HouseMembers)
                + " · STATE DELEGATIONS " + Short("REP") + " " + N(delegationsR) + " · " + Short("DEM") + " " + N(delegationsD) + " · TIED " + N(last.House.Count - delegationsR - delegationsD));
            slip.Add("EACH STATE MOVED FROM " + (last.BaseOfRecord ? "THE RECORD'S " : "THE GAME'S OWN ") + N(last.BaseYear) + " HOUSE VOTE BY THE NATIONAL SWING IN THE POLL");
            if (midterm && !string.IsNullOrEmpty(last.PresidentsParty))
            {
                // PS-6 US-27 - R-US20 (b): the fitted term, with its n and its interval, as the ruling asks them printed
                slip.Add("THE MIDTERM: " + UiFormat.Number((float)last.TermPoints, 2) + " POINTS ON THE PRESIDENT'S PARTY, " + Short(last.PresidentsParty)
                    + " - THE RECORD'S MEAN MIDTERM SWING, " + N(PoliSim.Elections.Generated.UsMidtermTerm.From) + "-" + N(PoliSim.Elections.Generated.UsMidtermTerm.To)
                    + " (" + N(PoliSim.Elections.Generated.UsMidtermTerm.N) + " MIDTERMS, 95 % INTERVAL " + UiFormat.Number((float)PoliSim.Elections.Generated.UsMidtermTerm.LowerPoints, 2)
                    + " TO " + UiFormat.Number((float)PoliSim.Elections.Generated.UsMidtermTerm.UpperPoints, 2) + ")");
            }
            int specials = last.Senate.FindAll(s => s.Class != last.SenateClass).Count;
            slip.Add("THE SENATE SEATS UP: " + Short("REP") + " " + N(last.SenateOf("REP")) + " · " + Short("DEM") + " " + N(last.SenateOf("DEM")) + " · CLASS " + N(last.SenateClass)
                + (specials > 0 ? " AND " + N(specials) + (specials == 1 ? " SPECIAL ELECTION" : " SPECIAL ELECTIONS") : string.Empty)
                + (midterm ? " · EACH STATE'S SHARE DERIVED FROM THE NATIONAL VOTE - NO PRESIDENT IS ELECTED AT A MIDTERM" : " · EACH STATE'S SHARE FROM THE PRESIDENTIAL COUNT THAT NIGHT"));
            if (seated && senate != null)
            {
                var parts = new List<string>();
                foreach (KeyValuePair<string, int> kv in senate) { if (kv.Value > 0) { parts.Add(Short(kv.Key) + " " + N(kv.Value)); } }
                slip.Add("THE SENATE AS SEATED: " + string.Join(" · ", parts) + " - THE SEATS THE GAME ELECTED ON " + string.Join(", ", UsCongressElection.SeatedPollingDays(_playerCountry, today).ConvertAll(DeskDay))
                    + "; EVERY OTHER SEAT AS THE RECORD SEATS IT, BY DATE");
            }
            if (last.PollingDay != _usCongressReferenceFor || country != _usCongressReferenceCountry)
            {
                _usCongressReferenceFor = last.PollingDay;
                _usCongressReferenceCountry = country;
                _usCongressReference = WorldClock.TryReference(country, last.PollingDay, out WorldClock.Reference fresh) ? fresh : null;
            }
            WorldClock.Reference reference = _usCongressReference;
            if (reference != null)
            {
                var house = new List<string>();
                foreach (KeyValuePair<string, int> kv in reference.Seats) { if (kv.Value > 0) { house.Add(Short(kv.Key) + " " + N(kv.Value)); } }
                slip.Add(reference.Label + ": THE HOUSE " + string.Join(" · ", house));
                if (reference.SecondChamber != null)
                {
                    var upper = new List<string>();
                    foreach (KeyValuePair<string, int> kv in reference.SecondChamber) { if (kv.Value > 0) { upper.Add(Short(kv.Key) + " " + N(kv.Value)); } }
                    slip.Add("THE SENATE OF RECORD ON " + DeskDay(reference.SecondChamberOn) + ": " + string.Join(" · ", upper) + (reference.SecondChamberVacant > 0 ? " · " + N(reference.SecondChamberVacant) + " VACANT" : string.Empty));
                }
                slip.Add("THE GAME'S ELECTION IS ITS OWN - THIS IS THE RECORD");
            }
            else if (last.PollingDay > WorldClock.RecordDate && System.Linq.Enumerable.Any(WorldClock.Chambers(country), c => c.ElectionDay == last.PollingDay.Date)) { slip.Add("THE RECORD HOLDS NO RESULT OF THIS ELECTION - ITS RETURNS ARE NOT YET CERTIFIED"); }   // PS-6 US-27: the 2026 reference is billed, never estimated
            if (nextVote) { slip.Add("NEXT VOTE " + DeskDay(nextDay) + " - " + UsCongressElection.ContestsWords(nextDay)); }
            book.Anchors["uscongress"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        // the nominee row's reading, kept for the day, the country, the party and the picks it was read with - a pick marks it stale (the president row's cost note:
        // an OnGUI event does not re-derive the field)
        private System.DateTime _usNomineeFor = System.DateTime.MinValue;
        private Country _usNomineeCountry;
        private string _usNomineeParty;
        private int _usNomineePicks = -1;
        private bool _usNomineeStale = true;
        private System.DateTime _usNomineeDay;
        private UsTermLimit.Nominee _usNominee;
        private List<string> _usNomineeChoices;

        /// <summary>
        /// PS-6 US-28 (R-US6 (b), as Elias ruled it: "the term limit doesn't end the run. The player picks the party's next nominee (the Vice President by default)
        /// and campaigns on the outgoing record"): THE NEXT NOMINEE of the player's party where its president cannot be elected again at the next polling day (the
        /// Twenty-second Amendment, <see cref="UsTermLimit"/>). The row: YOUR NOMINEE, the person who stands and why (THE VICE PRESIDENT, YOUR PICK, THE PARTY'S OWN),
        /// the polling day, and NEXT CHOICE, which steps the pick through the party's people who may be elected, the default first (<see cref="UsTermLimit.Pick"/>).
        /// The slip: who cannot stand and the elections that bar him, the amendment's words, the choices, and that every nominee stands at the party's poll on the
        /// outgoing record. Drawn for no other party - an AI party takes the default. Built as it stands; its board is asked of Design later.
        /// US-28b (R-US26 (a) as ruled): and where the player's party is out of the White House and has a choice (its people who may be elected beyond the
        /// default), the same row - THE PARTY'S OWN by default, YOUR PICK once picked - and NEXT CHOICE; its slip says a defeated president may run again.
        /// PS-6 US-33: and where the player's own nominee heads the ticket of the field of record (`UsTermLimit.Nominee.Own`), the row names it - YOUR OWN, the
        /// polling day, no choice to step - until the count; its slip says the party's poll reads the nominee's position.
        /// </summary>
        private void DrawUsNomineeRow(PeopleSlips.Book book)
        {
            if (!TryUsNomineeToday(out UsTermLimit.Nominee nominee, out List<string> choices, out System.DateTime day, out bool own, out bool outParty)) { return; }
            CountryId country = _playerCountry.Id;
            string party = _playerCountry.PlayerPartyAbbrev;

            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            Rect row = ReserveRow(30f);
            float x = row.x;
            void Words(string words, GUIStyle style)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(words)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, w, row.height), words, style);
                x += w + StatsUnit(10f);
            }
            // the row's budget (the US-28 review's L5): everything left of NEXT CHOICE - the nominee's label takes what the fixed words and the stamp leave,
            // cut at a word with an ellipsis where it is longer (an unnamed candidate's label can run to fifty characters), the whole name on the slip
            bool stepping = !own && choices.Count > 1;
            Rect chip = stepping ? RowChipRectEndingAt(row.xMax, row, "NEXT CHOICE", 26f, 12f) : default;
            float limit = stepping ? chip.x - StatsUnit(10f) : row.xMax;
            string tag = UsNomineeTag(nominee, own);
            Words("YOUR NOMINEE", caption);
            DrawPartyMarkSlot(new Rect(x, row.y, StatsUnit(16f), row.height), country, party);
            x += StatsUnit(16f) + StatsUnit(8f);
            float tagWidth = Mathf.Ceil(muted.CalcSize(new GUIContent(tag)).x) + 2f + StatsUnit(10f);
            float stampWidth = RowChipRectFrom(0f, row, DeskDay(day), padBoard: 6f).width + StatsUnit(10f);
            float nameBudget = Mathf.Max(StatsUnit(16f), limit - x - tagWidth - stampWidth - StatsUnit(10f));
            string name = V35Fit(Surname(nominee.Person).ToUpperInvariant(), caption, nameBudget - 2f, out bool _);
            float nameWidth = Mathf.Min(nameBudget, Mathf.Ceil(caption.CalcSize(new GUIContent(name)).x) + 2f);
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, nameWidth, row.height), name, caption);
            x += nameWidth + StatsUnit(10f);
            Words(tag, muted);
            Rect stamp = RowChipRectFrom(x, row, DeskDay(day), padBoard: 6f);
            DrawRowChip(stamp, DeskDay(day), ChipFace.Outline);
            x = stamp.xMax + StatsUnit(10f);
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "usnominee");
            if (stepping)
            {
                if (DrawRowChip(chip, "NEXT CHOICE", ChipFace.Paper)) { StepUsNominee(nominee, choices, day, party); }
                SlipAnchor(chip, "usnominee/next");
                book.Anchors["usnominee/next"] = UsNomineeNextSlip(day);
            }
            book.Anchors["usnominee"] = UsNomineeSlip(nominee, choices, day, own, outParty);
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        /// <summary>
        /// PS-6 US-28 / US-28b / US-33: the player's party's next nominee as the row reads it - kept for the day, the country, the party and the picks it was read
        /// with - and whether there is anything to draw: the player's own nominee (<paramref name="own"/>), an out-party with a choice to make
        /// (<paramref name="outParty"/>), or a term-limited party with people who may stand. K39 (Elias, 2026-10-23): read by the row where no Congress view
        /// stands and by the who-governs strip where it does (`DrawWhoGovernsNominee`).
        /// </summary>
        private bool TryUsNomineeToday(out UsTermLimit.Nominee nominee, out List<string> choices, out System.DateTime day, out bool own, out bool outParty)
        {
            nominee = null;
            choices = null;
            day = System.DateTime.MinValue;
            own = false;
            outParty = false;
            if (_playerCountry == null || !UsPresidentialElection.ElectsIn(PlayerCountryId) || string.IsNullOrEmpty(_playerCountry.PlayerPartyAbbrev)) { return false; }
            System.DateTime today = _simulationManager.CurrentDate;
            CountryId country = _playerCountry.Id;
            string party = _playerCountry.PlayerPartyAbbrev;
            int picks = _playerCountry.UsNomineePicks?.Count ?? 0;
            if (_usNomineeStale || _usNomineeFor != today || !ReferenceEquals(_usNomineeCountry, _playerCountry) || _usNomineeParty != party || _usNomineePicks != picks)
            {
                _usNomineeStale = false;
                _usNomineeFor = today;
                _usNomineeCountry = _playerCountry;
                _usNomineeParty = party;
                _usNomineePicks = picks;
                _usNominee = null;
                _usNomineeChoices = null;
                if (UsPresidentialElection.TryNextPresidentialDay(country, today.AddDays(1), out System.DateTime next))   // PS-6 US-27: a nominee stands on a presidential day
                {
                    _usNomineeDay = next;
                    _usNominee = UsTermLimit.NomineeOf(_playerCountry, next, party);
                    _usNomineeChoices = _usNominee != null && (_usNominee.TermLimited || _usNominee.OutParty) ? UsTermLimit.EligibleFor(_playerCountry, party, next) : null;
                }
            }
            nominee = _usNominee;
            choices = _usNomineeChoices;
            // PS-6 US-33 (SP-6a): the player's own nominee heads the ticket of the field of record - the row says so until the count, with no choice to step
            own = nominee != null && nominee.Own;
            // US-28b (R-US26 (a) as ruled): the out-party's row where the player has a choice to make - its default unnamed, a defeated president among its people
            outParty = nominee != null && !own && nominee.OutParty && choices != null && choices.Count > 1;
            if (nominee == null || (!own && !outParty && (!nominee.TermLimited || choices == null || choices.Count == 0))) { return false; }
            day = _usNomineeDay;
            return true;
        }

        /// <summary>The nominee's tag: who stands and why.</summary>
        private static string UsNomineeTag(UsTermLimit.Nominee nominee, bool own) =>
            own ? "YOUR OWN" : nominee.Picked ? "YOUR PICK" : nominee.ViceDefault ? "THE VICE PRESIDENT" : "THE PARTY'S OWN";

        /// <summary>NEXT CHOICE: steps the pick to the next of the party's people who may stand, the default first.</summary>
        private void StepUsNominee(UsTermLimit.Nominee nominee, List<string> choices, System.DateTime day, string party)
        {
            int at = choices.IndexOf(nominee.Person);
            string pick = choices[(at + 1) % choices.Count];
            if (!UsTermLimit.Pick(_playerCountry, day, party, pick, out string refused)) { Debug.Log($"NOMINEE: the pick was refused - {refused}"); }
            _usNomineeStale = true;
        }

        private static SlipContent UsNomineeNextSlip(System.DateTime day) => new SlipContent("NEXT CHOICE")
            .Add("STEPS YOUR NOMINEE THROUGH THE PARTY'S PEOPLE WHO MAY STAND, THE DEFAULT FIRST")
            .Add("THE PICK HOLDS UNTIL POLLING DAY, " + DeskDay(day));

        /// <summary>The nominee's slip: who stands, why, and what the choice changes.</summary>
        private SlipContent UsNomineeSlip(UsTermLimit.Nominee nominee, List<string> choices, System.DateTime day, bool own, bool outParty)
        {
            CountryId country = _playerCountry.Id;
            string party = _playerCountry.PlayerPartyAbbrev;
            if (own)
            {
                OwnNominees.TryOf(country, out OwnNominee registered);
                return new SlipContent("YOUR NOMINEE")
                    .Add(nominee.Person.ToUpperInvariant() + " · " + PartySystems.ShortName(country, party).ToUpperInvariant() + " · POLLING DAY " + DeskDay(day))
                    .Add("YOUR OWN NOMINEE HEADS THE TICKET IN PLACE OF THE NOMINEE OF RECORD")
                    .Add(registered != null ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "THE PARTY'S POLL READS YOUR NOMINEE'S POSITION: ECONOMIC {0:0.0} · GAL-TAN {1:0.0}", registered.LrEcon, registered.Galtan) : "THE PARTY'S POLL READS YOUR NOMINEE'S POSITION")
                    .Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "YOUR NOMINEE STANDS AT THE PARTY'S POLL, AT A FACTOR OF {0:0.0}", PresidentialElection.FutureFactor))
                    .Add("THE TWENTY-SECOND AMENDMENT READS YOUR NOMINEE AS A PERSON: ELECTED TWICE, BARRED");
            }
            var people = new List<string>();
            foreach (string person in choices) { people.Add(person.ToUpperInvariant()); }
            if (outParty)
            {
                return new SlipContent("YOUR NOMINEE")
                    .Add(nominee.Person.ToUpperInvariant() + " · " + PartySystems.ShortName(country, party).ToUpperInvariant() + " · POLLING DAY " + DeskDay(day))
                    .Add("YOUR PARTY IS OUT OF THE WHITE HOUSE - ITS OWN CANDIDATE STANDS BY DEFAULT")
                    .Add("A DEFEATED PRESIDENT MAY RUN AGAIN: THE TWENTY-SECOND AMENDMENT BARS ONLY A PERSON ELECTED TWICE")
                    .Add("THE PARTY'S PEOPLE WHO MAY STAND: " + string.Join(" · ", people))
                    .Add("EVERY NOMINEE STANDS AT THE PARTY'S POLL - THE CHOICE CHANGES WHO STANDS, NOT THE VOTE");
            }
            return new SlipContent("YOUR NOMINEE")
                .Add(nominee.Person.ToUpperInvariant() + " · " + PartySystems.ShortName(country, party).ToUpperInvariant() + " · POLLING DAY " + DeskDay(day))
                .Add(nominee.Barred.ToUpperInvariant() + " CANNOT BE ELECTED AGAIN - " + nominee.BarredBecause.ToUpperInvariant())
                .Add("THE TWENTY-SECOND AMENDMENT: " + UsTermLimit.FirstClause.ToUpperInvariant())
                .Add(nominee.ViceDefault ? "THE VICE PRESIDENT, " + nominee.Default.ToUpperInvariant() + ", STANDS BY DEFAULT" : "NO VICE PRESIDENT OF THE PARTY MAY STAND - THE PARTY'S OWN CANDIDATE STANDS BY DEFAULT")
                .Add("THE PARTY'S PEOPLE WHO MAY STAND: " + string.Join(" · ", people))
                .Add("EVERY NOMINEE STANDS AT THE PARTY'S POLL - THE CHOICE CHANGES WHO STANDS, NOT THE VOTE")
                .Add("THE CAMPAIGN IS FOUGHT ON THE OUTGOING TERM'S RECORD")
                .Add("THE LIMIT BINDS THE PERSON, NOT YOUR RUN");
        }

        /// <summary>A first vote's day in the player's words - the record's, or the ruled practice's (<see cref="PresidentialElection.FirstVoteFor"/>'s basis says which).</summary>
        private static string DayWords(string basis) => basis == null || !basis.StartsWith("the record's day", System.StringComparison.Ordinal)
            ? "THE LAST SUNDAY OF THE WINDOW BEFORE THE TERM ENDS - THE MOST RECENT PRACTICE"
            : "THE RECORD'S DAY";

        /// <summary>A president's or candidate's surname as the row prints it - `WorldClock.SurnameOf`'s one rule (PS-6 US-2: a generational suffix is not the surname).</summary>
        private static string Surname(string name) => WorldClock.SurnameOf(name);

        // §698: the constructive vote projected for the player's party - a formation's worth of work, so kept for the day and the government it was drawn for
        private ConfidenceProcedure.MotionVote _constructiveProjection;
        private System.DateTime _constructiveProjectedOn = System.DateTime.MinValue;
        private GovernmentRecord _constructiveProjectedFor;

        /// <summary>
        /// §698 (Art. 67 GG): the opposition's row in Germany - A CONSTRUCTIVE VOTE OF NO CONFIDENCE with the successor's projected election (for ⁄ needed,
        /// a majority of the members) and ELECT A SUCCESSOR; the slip names the government the player's candidate would lead, and a partner that refuses it.
        /// There is no tenth to take a motion up, no week and no answers: the vote is the election, and carried it installs the successor at once.
        /// K197's Poland row (Art. 158) moved to the Cabinet tab's "In the Sejm" with COAL-2 and §9 (`DrawInTheSejm`), in the one grammar; this row is the
        /// Bundestag's alone.
        /// </summary>
        private void DrawConstructiveVote(PeopleSlips.Book book, GUIStyle caption)
        {
            GovernmentRecord g = _playerCountry.Government;
            if (_constructiveProjection == null || _constructiveProjectedOn != _simulationManager.CurrentDate || !ReferenceEquals(_constructiveProjectedFor, g))
            {
                _constructiveProjection = _simulationManager.ProjectConstructiveVote(PlayerCountryId);
                _constructiveProjectedOn = _simulationManager.CurrentDate;
                _constructiveProjectedFor = g;
            }
            ConfidenceProcedure.MotionVote projected = _constructiveProjection;
            if (projected == null) { return; }
            bool mayMove = _simulationManager.CanMoveNoConfidence(PlayerCountryId, out string moveRefused);
            string Names(List<string> keys) { var names = new List<string>(); foreach (string k in keys) { names.Add(PartySystems.ShortName(PlayerCountryId, k)); } return PartySystems.JoinShortNames(names); }   // K312
            Rect row = ReserveRow(34f);
            float x = row.x;
            const string motion = "A CONSTRUCTIVE VOTE OF NO CONFIDENCE";
            float mw = Mathf.Ceil(caption.CalcSize(new GUIContent(motion)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, mw, row.height), motion, caption);
            x += mw + StatsUnit(12f);
            // AF-8 (ruling q8): GO-BT § 97 Abs. 1 [GOBT-4] - a quarter of the members sign the motion, or it is not put on the agenda; the signatories are the
            // members who would elect the candidate (`SimulationManager.MoveNoConfidence`). Short of it the row says so and the act is not offered - the
            // Riksdag row's tenth, below, in the Bundestag's words.
            int quarter = (projected.Members + 3) / 4;
            bool signed = projected.For >= quarter;
            x = signed
                ? DrawFigurePair(x, row, projected.For.ToString(CultureInfo.InvariantCulture), "⁄ " + projected.Needed.ToString(CultureInfo.InvariantCulture) + " NEEDED")
                : DrawFigurePair(x, row, projected.For.ToString(CultureInfo.InvariantCulture), "⁄ " + quarter.ToString(CultureInfo.InvariantCulture) + " TO SIGN IT");
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "motion");
            string quarterLine = string.Format(CultureInfo.InvariantCulture, "A QUARTER OF THE MEMBERS, {0}, MUST SIGN IT (GO-BT § 97 ABS. 1)", quarter);
            const string house = "MEMBERS";
            const string elect = "WOULD ELECT YOUR CANDIDATE";
            SlipContent slip = !signed
                ? new SlipContent("NOT YET A MOTION").Add(quarterLine)
                    .Add(string.Format(CultureInfo.InvariantCulture, "{0} OF {1} {2} {3}", projected.For, projected.Members, house, elect))
                : new SlipContent("A CONSTRUCTIVE VOTE OF NO CONFIDENCE")
                .Add(string.Format(CultureInfo.InvariantCulture, "{0} OF {1} {2} {3}", projected.For, projected.Members, house, elect))
                .Add(string.Format(CultureInfo.InvariantCulture, "IT NEEDS {0}, A MAJORITY OF THE MEMBERS", projected.Needed))
                .Add(quarterLine)
                .Add("YOUR GOVERNMENT: " + Names(projected.Governing.Count > 0 ? projected.Governing : projected.SuccessorCabinet).ToUpperInvariant() + (projected.PartnersAccept && projected.SuccessorSupport.Count > 0 ? " WITH " + Names(projected.SuccessorSupport).ToUpperInvariant() : string.Empty));
            // §754 (ruling A4): the vote elects the person - a refusing partner changes the government the elected successor forms, never his election
            if (signed && !projected.PartnersAccept) { slip.Add(projected.Carried ? "A PARTNER REFUSES ITS PLACE - ELECTED, YOU GOVERN WITHOUT IT" : "A PARTNER REFUSES ITS PLACE"); }
            book.Anchors["motion"] = slip;
            const string act = "ELECT A SUCCESSOR";
            Rect move = RowChipRectEndingAt(row.xMax, row, act, 26f, 12f);
            if (DrawRowChip(move, act, ChipFace.Paper, disabled: !signed || !mayMove))
            {
                if (!_simulationManager.MoveNoConfidence(PlayerCountryId, out string refused, out _)) { Debug.Log($"CONFIDENCE: the motion was refused - {refused}"); }
                _constructiveProjection = null;
            }
            if (!mayMove) { SlipAnchor(move, "motion/refused"); book.Anchors["motion/refused"] = SlipWrapped(act, moveRefused); }
            DrawRowRule(row);
        }

        /// <summary>
        /// PS-3i (§636), redrawn from 21b: CONFIDENCE on the Parliament tab - an extra election as a stamp and its date; a declaration of no confidence
        /// as NO CONFIDENCE DECLARED in Caution ink with the days left to answer as a figure, and, for the player's government, the week's two answers,
        /// both paper (EXTRA ELECTION, ASK TO BE DISCHARGED); the opposition's motion with its count (for ⁄ needed) and MOVE NO CONFIDENCE; the junior
        /// partner's LEAVE THE GOVERNMENT where the country's coalition verbs are not modelled (elsewhere the Cabinet tab's, COAL-2). The sentences are the slips.
        /// </summary>
        private void DrawConfidence(PeopleSlips.Book book)
        {
            GovernmentRecord g = _playerCountry?.Government;
            // PS-6 US-7: a presidency takes no motion - nothing to draw, as where the rules are unsourced. K197's Sejm rows (Art. 158, the junior partner's
            // leaving) are the Cabinet tab's since COAL-2 with §9; TakesMotions stays false for Poland (no declaration waits there, K195)
            bool sejmRules = ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Sejm;
            if (g == null || !(ConfidenceProcedure.TakesMotions(PlayerCountryId) || sejmRules)) { return; }
            // COAL-2 with §9 (board 9a): Poland's Art. 158 and LEAVE moved to the Cabinet tab ("The coalition", "In the Sejm") - nothing of the Sejm's is left here
            if (sejmRules) { return; }
            GUIStyle caption = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
            GUIStyle muted = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            System.DateTime extra = _simulationManager.ExtraElectionDate;
            bool governs = _simulationManager.PlayerGoverns(_playerCountry);
            if (extra != System.DateTime.MinValue)
            {
                Rect row = ReserveRow(30f);
                Rect stamp = RowChipRectFrom(row.x, row, "EXTRA ELECTION", padBoard: 6f);
                DrawRowChip(stamp, "EXTRA ELECTION", ChipFace.Outline);
                string day = DeskDay(extra);
                Rect date = RowChipRectFrom(stamp.xMax + StatsUnit(10f), row, day, padBoard: 6f);
                DrawRowChip(date, day, ChipFace.Outline);
                SlipAnchor(new Rect(row.x, row.y, date.xMax - row.x, row.height), "extra");
                book.Anchors["extra"] = new SlipContent("AN EXTRA ELECTION ON " + day);
                DrawRowRule(row);
            }
            if (!g.Caretaker && g.NoConfidenceOn != System.DateTime.MinValue)
            {
                System.DateTime discharge = g.NoConfidenceOn.AddDays(ConfidenceProcedure.ExtraElectionWindowDays);
                Rect row = ReserveRowPx(Mathf.Max(StatsUnit(34f), FigureOverCaptionHeight() + StatsUnit(4f)));
                Rect stamp = RowChipRectFrom(row.x, row, "NO CONFIDENCE DECLARED", padBoard: 6f);
                DrawRowChip(stamp, "NO CONFIDENCE DECLARED", ChipFace.Outline, ink: PoliSimTheme.Caution);
                int daysLeft = System.Math.Max(0, (discharge.Date - _simulationManager.CurrentDate.Date).Days);
                float fy = row.y + Mathf.Round((row.height - FigureOverCaptionHeight()) * 0.5f);
                float fw = DrawFigureOverCaption(stamp.xMax + StatsUnit(12f), fy, daysLeft.ToString(CultureInfo.InvariantCulture), daysLeft == 1 ? "DAY TO ANSWER" : "DAYS TO ANSWER", PoliSimTheme.TextPrimary);
                SlipAnchor(new Rect(row.x, row.y, stamp.xMax + StatsUnit(12f) + fw - row.x, row.height), "noconfidence");
                book.Anchors["noconfidence"] = new SlipContent("NO CONFIDENCE DECLARED " + DeskDay(g.NoConfidenceOn))
                    .Add(governs ? "THE CHAMBER HAS NO CONFIDENCE IN YOUR GOVERNMENT" : "THE CHAMBER HAS NO CONFIDENCE IN THE GOVERNMENT")
                    .Add(governs ? "THE CLOCK WAITS ON YOUR ANSWER" : "THE SPEAKER DISCHARGES THE GOVERNMENT ON " + DeskDay(discharge));
                if (governs)
                {
                    bool mayOrder = _simulationManager.CanOrderExtraElection(PlayerCountryId, out string orderRefused);
                    Rect discharged = RowChipRectEndingAt(row.xMax, row, "ASK TO BE DISCHARGED", 26f, 12f);
                    Rect order = RowChipRectEndingAt(discharged.x - StatsUnit(8f), row, "EXTRA ELECTION", 26f, 12f);
                    if (DrawRowChip(order, "EXTRA ELECTION", ChipFace.Paper, disabled: !mayOrder))
                    {
                        if (!_simulationManager.OrderExtraElection(PlayerCountryId, out string refused)) { Debug.Log($"CONFIDENCE: refused - {refused}"); }
                    }
                    // §641: the other answer - the prime minister asks the Speaker to be discharged (6 kap. 8 §); the Speaker's round follows at once.
                    if (DrawRowChip(discharged, "ASK TO BE DISCHARGED", ChipFace.Paper))
                    {
                        if (!_simulationManager.AskToBeDischarged(PlayerCountryId, out string refused)) { Debug.Log($"CONFIDENCE: refused - {refused}"); }
                    }
                    SlipAnchor(order, "noconfidence/order");
                    book.Anchors["noconfidence/order"] = mayOrder
                        ? new SlipContent("EXTRA ELECTION").Add("WITHIN THE WEEK YOU MAY ORDER AN EXTRA ELECTION").Add("INSTEAD OF BEING DISCHARGED")
                        : SlipWrapped("EXTRA ELECTION", orderRefused);
                    SlipAnchor(discharged, "noconfidence/discharge");
                    book.Anchors["noconfidence/discharge"] = new SlipContent("ASK TO BE DISCHARGED").Add("THE SPEAKER DISCHARGES YOU NOW").Add("AND THE SPEAKER'S ROUND BEGINS");
                }
                DrawRowRule(row);
            }
            switch (_playerCountry.PlayerHoldsPresidency ? PlayerRole.None : g.RoleOf(_playerCountry.PlayerPartyAbbrev))   // SP-7b (I14 (2a)): no party verb - no motion, no leaving - while the player is President
            {
                case PlayerRole.Opposition:
                {
                    if (g.Caretaker || g.NoConfidenceOn != System.DateTime.MinValue || extra != System.DateTime.MinValue) { break; }
                    if (ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Bundestag) { if (_simulationManager.RoundOf(PlayerCountryId) == null && !_simulationManager.ElectionAwaitsRound(PlayerCountryId)) { DrawConstructiveVote(book, caption); } break; }   // §698; §705: none while the chancellor's election runs (Art. 63, not Art. 67)
                    ConfidenceProcedure.MotionVote projected = ConfidenceProcedure.Vote(_playerCountry, _playerCountry.PlayerPartyAbbrev, _simulationManager.CurrentDate);
                    bool takenUp = ConfidenceProcedure.CanBeTakenUp(_playerCountry, _playerCountry.PlayerPartyAbbrev, out int moverSeats, out int tenth);
                    Rect row = ReserveRow(34f);
                    float x = row.x;
                    const string motion = "A MOTION OF NO CONFIDENCE";
                    float mw = Mathf.Ceil(caption.CalcSize(new GUIContent(motion)).x) + 2f;
                    PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, mw, row.height), motion, caption);
                    x += mw + StatsUnit(12f);
                    x = takenUp
                        ? DrawFigurePair(x, row, projected.For.ToString(CultureInfo.InvariantCulture), "⁄ " + projected.Needed.ToString(CultureInfo.InvariantCulture) + " NEEDED")
                        : DrawFigurePair(x, row, moverSeats.ToString(CultureInfo.InvariantCulture), "⁄ " + tenth.ToString(CultureInfo.InvariantCulture) + " TO BE TAKEN UP");
                    SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "motion");
                    book.Anchors["motion"] = takenUp
                        ? new SlipContent("A MOTION OF NO CONFIDENCE").Add(string.Format(CultureInfo.InvariantCulture, "{0} OF {1} MEMBERS WOULD BE FOR IT", projected.For, projected.Members))
                            .Add(string.Format(CultureInfo.InvariantCulture, "IT NEEDS {0}", projected.Needed))
                        : new SlipContent("NOT YET A MOTION").Add(string.Format(CultureInfo.InvariantCulture, "A MOTION NEEDS A TENTH OF THE MEMBERS, {0}, TO BE TAKEN UP", tenth))
                            .Add(string.Format(CultureInfo.InvariantCulture, "YOUR PARTY HOLDS {0}", moverSeats));
                    Rect move = RowChipRectEndingAt(row.xMax, row, "MOVE NO CONFIDENCE", 26f, 12f);
                    if (DrawRowChip(move, "MOVE NO CONFIDENCE", ChipFace.Paper, disabled: !takenUp))
                    {
                        if (!_simulationManager.MoveNoConfidence(PlayerCountryId, out string refused, out _)) { Debug.Log($"CONFIDENCE: the motion was refused - {refused}"); }
                    }
                    DrawRowRule(row);
                    break;
                }
                case PlayerRole.JuniorPartner:
                {
                    if (CoalitionVerbs.Applies(PlayerCountryId)) { break; }   // COAL-2 with §9: LEAVE is the Cabinet tab's now, in the one grammar (`DrawCoalitionVerbs`)
                    Rect row = ReserveRow(34f);
                    // K197: the verb's own gate, so a refusal (a parliamentary group's smaller member) draws disabled with its reason
                    bool mayLeave = _simulationManager.CanLeaveGovernment(PlayerCountryId, out string leaveRefused);
                    Rect leave = RowChipRectEndingAt(row.xMax, row, "LEAVE THE GOVERNMENT", 26f, 12f);
                    if (DrawRowChip(leave, "LEAVE THE GOVERNMENT", ChipFace.Paper, disabled: !mayLeave))
                    {
                        if (!_simulationManager.LeaveGovernment(PlayerCountryId, out string refused)) { Debug.Log($"CONFIDENCE: refused - {refused}"); }
                        _constructiveProjection = null;   // the party is in opposition now - its constructive row projects afresh
                    }
                    SlipAnchor(leave, "leave");
                    book.Anchors["leave"] = !mayLeave ? SlipWrapped("LEAVE THE GOVERNMENT", leaveRefused)
                        : sejmRules ? new SlipContent("LEAVE THE GOVERNMENT").Add("YOUR MINISTERS LEAVE THE COUNCIL OF MINISTERS")
                            .Add("THE CABINET GOVERNS ON THE SEATS IT KEEPS")
                            .Add("THE DECLARED OPPOSITION MAY MOVE ART. 158 THE SAME DAY")
                        : new SlipContent("LEAVE THE GOVERNMENT").Add("YOUR MINISTERS LEAVE THE CABINET")
                            .Add("THE GOVERNMENT STANDS UNTIL THE CHAMBER DECLARES OTHERWISE");
                    break;
                }
            }
        }
    }
}
