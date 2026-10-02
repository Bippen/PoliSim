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
        private Rect _parliamentSlipBounds;

        /// <summary>A row reserved through the layout at a height in pixels.</summary>
        private Rect ReserveRowPx(float height)
        {
            Rect r = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (_parliamentVisibleWidth > 1f && r.width > _parliamentVisibleWidth) { r.width = _parliamentVisibleWidth; }
            return r;
        }

        /// <summary>The Parliament tab's visible width, set by the Politics tab each pass (its scroll content can be wider).</summary>
        private float _parliamentVisibleWidth;

        /// <summary>The Parliament tab's four political blocks and their slips, in the scroll view's own space.</summary>
        private void DrawParliamentPoliticalBlocks()
        {
            BeginSlipAnchors();
            var book = new PeopleSlips.Book();
            DrawSupportAgreements(book);   // PS-3h (§635)
            DrawCaretakerLine(book);
            DrawSpeakerRound(book);   // §646: the formateur's round
            DrawReferenceRow(book);   // §705: history as the reference where no election night carries it
            DrawConfidence(book);   // PS-3i (§636)
            Rect tail = ReserveRowPx(1f);
            if (Event.current.type == EventType.Repaint) { _parliamentSlipBounds = new Rect(tail.x, 0f, tail.width, tail.yMax); }
            if (_parliamentSlipBounds.width > 1f) { DrawSlips(book, _parliamentSlipBounds); }
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

                if (agreement.Supporter == _playerCountry.PlayerPartyAbbrev && !withdrawn)
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
                .Add(ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Bundestag ? "UNTIL THE BUNDESTAG ELECTS A CHANCELLOR (ART. 69 ABS. 3)" : "UNTIL A PROPOSAL WINS ITS INVESTITURE");   // §705
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
                    book.Anchors["round/stage"] = new SlipContent(Name(round.Proposal.Formateur) + "'S PROPOSAL")
                        .Add("THE CABINET " + string.Join("+", round.Proposal.CabinetParties.ConvertAll(Name)))
                        .Add(round.Proposal.Supporters.Count > 0 ? "SUPPORTED BY " + string.Join("+", round.Proposal.Supporters.ConvertAll(Name)) : "NO SUPPORT FROM OUTSIDE")
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
                        .Add("THE CABINET " + string.Join("+", round.Proposal.CabinetParties.ConvertAll(Name)))
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
                        .Add("THE SHEET OPENS ON " + string.Join("+", current.CabinetParties.ConvertAll(Name)) + (current.Supporters.Count > 0 ? " WITH " + string.Join("+", current.Supporters.ConvertAll(Name)) + " SUPPORTING" : string.Empty))
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
            if (ConfidenceProcedure.RulesOf(PlayerCountryId) != ConfidenceProcedure.Rules.Bundestag || _playerCountry.ElectionHistory == null) { return; }
            System.DateTime held = System.DateTime.MinValue;
            foreach (ElectionRecord e in _playerCountry.ElectionHistory)
            {
                if (e.Method != ElectionMethod.NotImplemented && e.Date <= _simulationManager.CurrentDate && e.Date > held) { held = e.Date; }
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
            slip.Add("THE GAME'S ELECTION IS ITS OWN - THIS IS THE RECORD");
            book.Anchors["reference"] = slip;
            DrawRowRule(row);
            GUILayout.Space(StatsUnit(10f));
        }

        // §698: the constructive vote projected for the player's party - a formation's worth of work, so kept for the day and the government it was drawn for
        private ConfidenceProcedure.MotionVote _constructiveProjection;
        private System.DateTime _constructiveProjectedOn = System.DateTime.MinValue;
        private GovernmentRecord _constructiveProjectedFor;

        /// <summary>
        /// §698 (Art. 67 GG): the opposition's row in Germany - A CONSTRUCTIVE VOTE OF NO CONFIDENCE with the successor's projected election (for ⁄ needed,
        /// a majority of the members) and ELECT A SUCCESSOR; the slip names the government the player's candidate would lead, and a partner that refuses it.
        /// There is no tenth to take a motion up, no week and no answers: the vote is the election, and carried it installs the successor at once.
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
            string Names(List<string> keys) { var names = new List<string>(); foreach (string k in keys) { names.Add(PartySystems.ShortName(PlayerCountryId, k)); } return string.Join("+", names); }
            Rect row = ReserveRow(34f);
            float x = row.x;
            const string motion = "A CONSTRUCTIVE VOTE OF NO CONFIDENCE";
            float mw = Mathf.Ceil(caption.CalcSize(new GUIContent(motion)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x, row.y, mw, row.height), motion, caption);
            x += mw + StatsUnit(12f);
            x = DrawFigurePair(x, row, projected.For.ToString(CultureInfo.InvariantCulture), "⁄ " + projected.Needed.ToString(CultureInfo.InvariantCulture) + " NEEDED");
            SlipAnchor(new Rect(row.x, row.y, x - row.x, row.height), "motion");
            SlipContent slip = new SlipContent("A CONSTRUCTIVE VOTE OF NO CONFIDENCE")
                .Add(string.Format(CultureInfo.InvariantCulture, "{0} OF {1} MEMBERS WOULD ELECT YOUR CANDIDATE", projected.For, projected.Members))
                .Add(string.Format(CultureInfo.InvariantCulture, "IT NEEDS {0}, A MAJORITY OF THE MEMBERS", projected.Needed))
                .Add("YOUR GOVERNMENT: " + Names(projected.Governing.Count > 0 ? projected.Governing : projected.SuccessorCabinet).ToUpperInvariant() + (projected.PartnersAccept && projected.SuccessorSupport.Count > 0 ? " WITH " + Names(projected.SuccessorSupport).ToUpperInvariant() : string.Empty));
            // §754 (ruling A4): the vote elects the person - a refusing partner changes the government the elected successor forms, never his election
            if (!projected.PartnersAccept) { slip.Add(projected.Carried ? "A PARTNER REFUSES ITS PLACE - ELECTED, YOU GOVERN WITHOUT IT" : "A PARTNER REFUSES ITS PLACE"); }
            book.Anchors["motion"] = slip;
            Rect move = RowChipRectEndingAt(row.xMax, row, "ELECT A SUCCESSOR", 26f, 12f);
            if (DrawRowChip(move, "ELECT A SUCCESSOR", ChipFace.Paper))
            {
                if (!_simulationManager.MoveNoConfidence(PlayerCountryId, out string refused, out _)) { Debug.Log($"CONFIDENCE: the motion was refused - {refused}"); }
                _constructiveProjection = null;
            }
            DrawRowRule(row);
        }

        /// <summary>
        /// PS-3i (§636), redrawn from 21b: CONFIDENCE on the Parliament tab - an extra election as a stamp and its date; a declaration of no confidence
        /// as NO CONFIDENCE DECLARED in Caution ink with the days left to answer as a figure, and, for the player's government, the week's two answers,
        /// both paper (EXTRA ELECTION, ASK TO BE DISCHARGED); the opposition's motion with its count (for ⁄ needed) and MOVE NO CONFIDENCE; the junior
        /// partner's LEAVE THE GOVERNMENT. The sentences are the slips.
        /// </summary>
        private void DrawConfidence(PeopleSlips.Book book)
        {
            GovernmentRecord g = _playerCountry?.Government;
            if (g == null || ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Unsourced) { return; }
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
            switch (g.RoleOf(_playerCountry.PlayerPartyAbbrev))
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
                    Rect row = ReserveRow(34f);
                    Rect leave = RowChipRectEndingAt(row.xMax, row, "LEAVE THE GOVERNMENT", 26f, 12f);
                    if (DrawRowChip(leave, "LEAVE THE GOVERNMENT", ChipFace.Paper))
                    {
                        if (!_simulationManager.LeaveGovernment(PlayerCountryId, out string refused)) { Debug.Log($"CONFIDENCE: refused - {refused}"); }
                    }
                    SlipAnchor(leave, "leave");
                    book.Anchors["leave"] = new SlipContent("LEAVE THE GOVERNMENT").Add("YOUR MINISTERS LEAVE THE CABINET")
                        .Add("THE GOVERNMENT STANDS UNTIL THE CHAMBER DECLARES OTHERWISE");
                    break;
                }
            }
        }
    }
}
