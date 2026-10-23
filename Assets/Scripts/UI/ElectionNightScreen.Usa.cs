using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-34 (Elias, 2026-10-23: build US-34 from the D-US boards; boards 29a-29b, drawn by Code through the Design channel as a PROPOSAL and built on
    /// that go-ahead though none is stamped LOOKED AT - DECLARED): <b>THE USA'S ELECTION NIGHT</b> - the President, the House and the Senate class counted
    /// state by state, in the nights' grammar (the paper, the masthead, the foot and its CONTINUE shared; the body at the boards' own sizes, a board
    /// pixel 1.5 canvas units as the Polish nights draw it).
    ///
    /// <para><b>THE COUNT</b> (29b's answer 4): three tug bars - REP from the left, DEM from the right, what is to come between, the majority's tick at the
    /// centre - the College of 538 to 270, the House of 435 to 218, the Senate that sits to 51 with its seats not up at the ends at reduced presence and
    /// the independents inside DEM's; each with its call chip. <b>THE 51</b> - the tiles (`UsTileView`) under the President's or Congress's lens, the
    /// Delegations lens only when the College has no majority. <b>WHO GOVERNS</b> - the sitting President, Vice President and Congress to their dates; the
    /// ELECT card once the College is called, or the no-majority card; after 51 of 51 on the night the record holds, "2024, as it happened"; and "Until
    /// the oath".</para>
    ///
    /// <para>And the House's own moment on 6 January where the College had no majority (29b's answer 6): the fifty votes to 26, and the Senate's choice
    /// of the Vice President where the House cannot choose.</para>
    /// </summary>
    public partial class ElectionNightScreen
    {
        /// <summary>US-34: the tiles on a US night (null on every other night) - read by the film's log line.</summary>
        public UsTileView UsTiles { get; private set; }

        private Action<UsTileView.Lens> _usLens;

        /// <summary>US-34: the tiles re-inked under <paramref name="lens"/>, as the lens chips do it - the film's way to the Congress lens. False where the night
        /// has no tiles or does not offer the lens.</summary>
        public bool SetUsLens(UsTileView.Lens lens)
        {
            if (_usLens == null || UsTiles == null) { return false; }
            _usLens(lens);
            return UsTiles.Current == lens;
        }

        /// <summary>What the US night draws beside its count - all of it read by the caller from the game, none of it from the board.</summary>
        public sealed class UsNightBoard
        {
            public UsNight.State State;
            /// <summary>The sitting President and Vice President on the polling day (persons) and their party; the sitting Congress's number (0 where none).</summary>
            public string President, PresidentParty, VicePresident, VicePresidentParty;
            public int SittingCongress;
            /// <summary>The persons on the two tickets tonight (null on a midterm) - the ELECT card names the one the College calls.</summary>
            public string RepNominee, DemNominee, RepRunningMate, DemRunningMate;
            /// <summary>The national vote the count read, in %: REP, DEM, every other candidate together (NaN on a midterm); on a midterm the House's V.</summary>
            public double RepShare = double.NaN, DemShare = double.NaN, OtherShare = double.NaN, HouseV = double.NaN;
            /// <summary>The day the sitting President's and Vice President's term ends - the oath after this night on a presidential night, the oath after the
            /// next presidential count on a midterm (the Twentieth Amendment's 20 January; MinValue where none is held).</summary>
            public DateTime SittingUntil;
            /// <summary>The dates to the oath: the electors meet, the new Congress sits, Congress counts, the oath (the first and the last two MinValue on a midterm).</summary>
            public DateTime ElectorsMeet, Convenes, CountedOn, TakesOffice;
            /// <summary>The states (excluding the District) whose delegations the Twelfth Amendment's House counts, and the majority of them.</summary>
            public int AllStates;
            /// <summary>The House sitting on the polling day, by state - the replaced delegations, each slip's ± ; null where not held.</summary>
            public Dictionary<string, (int Rep, int Dem)> SittingHouse;
            /// <summary>Each state's REP share of the two-party presidential vote in the comparison base: the record's on the first night, the game's own
            /// previous count after; its label ("Record 2024", "The game's 2024"); null on a midterm.</summary>
            public Dictionary<string, double> BaseRepTwoParty;
            public string BaseLabel;
            /// <summary>History as the reference (PS-2, §619), on the night the record holds only: the record's electors, House and the Senate that opened on
            /// 3 January (null where not held - owed), and each state's and district's winner of record ("ME-2" for a district).</summary>
            public bool HasReference;
            public string ReferenceLabel;
            public int RealElectorsRep, RealElectorsDem, RealHouseRep, RealHouseDem;
            public int? RealSenateRep, RealSenateDem;
            public Dictionary<string, string> RecordWinners;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE NIGHT
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>US-34: the USA's night at one instant, in <paramref name="lens"/> (the President's on a presidential night, Congress's on a midterm, where
        /// none is given). Null, and said, where the furniture or the count is missing.</summary>
        public static ElectionNightScreen BuildUsa(UsNightBoard board, string verdict, UsTileView.Lens? lens = null)
        {
            if (board == null || board.State == null)
            {
                Debug.LogWarning("CANVAS: the US night has no count - the board is dropped, the night stays silent.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }
            UsNight.State s = board.State;
            Masthead(content, s.Presidential ? "THE UNITED STATES  ·  PRESIDENT, HOUSE, SENATE" : "THE UNITED STATES  ·  HOUSE, SENATE",
                DeskDay(s.PollingDay) + "  ·  THE COUNT BY STATE",
                string.Format(CultureInfo.InvariantCulture, "{0} OF {1} DECLARED", s.Declared, s.Total));

            Transform body = Body(content);
            body.GetComponent<HorizontalLayoutGroup>().spacing = 18f * BoardPx;   // 29a's gutters, 18 of its px
            Transform overlay = screen.Root.transform;
            // 29a's columns as the board draws them, 290 · 576 · 290 of its pixels
            Transform count = Column(body, "Count", 290f);
            count.GetComponent<VerticalLayoutGroup>().spacing = 2f;
            BuildUsCount(count, board, overlay);
            Transform centre = Column(body, "MapColumn", 576f);
            centre.GetComponent<VerticalLayoutGroup>().spacing = Px(6f);
            BuildUsMap(centre, board, overlay, screen, lens ?? (s.Presidential ? UsTileView.Lens.President : UsTileView.Lens.Congress));
            Transform right = Column(body, "Governs", 290f);
            right.GetComponent<VerticalLayoutGroup>().spacing = Px(6f);
            BuildUsGoverns(right, board, overlay);
            BuildFooter(content, verdict, screen, null, null, overlay);

            Resolve(contentRect);
            if (screen.UsTiles != null) { screen.UsTiles.Rebuild(); }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        /// <summary>
        /// THE COUNT (29a's left column, 29b's answer 4): the President (the College), the House and the Senate, each a tug bar with its figures under it and
        /// its call chip; the nation's vote, owed until 51 of 51. A midterm night has no College.
        /// </summary>
        private static void BuildUsCount(Transform parent, UsNightBoard board, Transform overlay)
        {
            UsNight.State s = board.State;
            if (s.Presidential)
            {
                string toCome = s.ElectorsToCome > 0 ? Count(s.ElectorsToCome) + " to come" : string.Empty;
                TugBar(parent, overlay, "President", "the College · " + Count(s.Electors), Count(s.ElectorsMajority),
                    new[] { (s.ElectorsRep, Seg.Rep), (s.ElectorsToCome, Seg.ToCome), (s.ElectorsDem, Seg.Dem) }, s.Electors,
                    "REP " + Count(s.ElectorsRep), toCome, Count(s.ElectorsDem) + " DEM",
                    "THE ELECTORAL COLLEGE · " + Count(s.Electors),
                    new[] { Count(s.ElectorsMajority) + " ELECTS · AN EQUAL COLLEGE GOES TO THE HOUSE", "WINNER TAKES ALL, BUT MAINE AND NEBRASKA: TWO TO THE STATE, ONE TO EACH DISTRICT",
                        "WON · REP " + Count(s.ElectorsRep) + " · DEM " + Count(s.ElectorsDem) + (s.ElectorsToCome > 0 ? " · TO COME " + Count(s.ElectorsToCome) : string.Empty) },
                    s.College, s.CollegeAt, s.Total, "OPEN · " + Count(s.ElectorsMajority) + " ELECTS", "NO MAJORITY · TO THE HOUSE",
                    s.College == UsNight.Call.NoMajority ? new[] { "NEITHER SIDE CAN REACH " + Count(s.ElectorsMajority), "THE HOUSE CHOOSES ON " + DeskDay(board.CountedOn) + ", ONE VOTE A STATE" } : null);
            }
            TugBar(parent, overlay, "House", Count(s.HouseSeats), Count(s.HouseMajority),
                new[] { (s.HouseRep, Seg.Rep), (s.HouseToCome, Seg.ToCome), (s.HouseDem, Seg.Dem) }, s.HouseSeats,
                "REP " + Count(s.HouseRep), s.HouseToCome > 0 ? Count(s.HouseToCome) + " to come" : string.Empty, Count(s.HouseDem) + " DEM",
                "THE HOUSE · " + Count(s.HouseSeats),
                new[] { Count(s.HouseMajority) + " IS A MAJORITY · COUNTED BY STATE DELEGATION", "THE DISTRICT ELECTS NONE", "WON · REP " + Count(s.HouseRep) + " · DEM " + Count(s.HouseDem) + (s.HouseToCome > 0 ? " · TO COME " + Count(s.HouseToCome) : string.Empty),
                    "SEATED " + DeskDay(board.Convenes) },
                s.House, s.HouseAt, s.Total, "OPEN · " + Count(s.HouseMajority), "NO MAJORITY", null);

            int half = s.SenateSeats / 2;
            bool even = s.SenateSeats % 2 == 0;
            string classes = SenateClassWords(s);
            TugBar(parent, overlay, "Senate", Count(s.SenateSeats) + " · " + classes, even ? Count(half) + " + VP" : Count(s.SenateMajority),
                new[] { (s.NotUpRep, Seg.RepPale), (s.UpRep, Seg.Rep), (s.UpToCome, Seg.ToCome), (s.UpDem, Seg.Dem), (s.NotUpIndependents, Seg.IndPale), (s.NotUpDem - s.NotUpIndependents, Seg.DemPale) },
                s.SenateSeats, "REP " + Count(s.SenateRep), s.UpToCome > 0 ? Count(s.UpToCome) + " to come" : string.Empty,
                (s.NotUpIndependents > 0 ? "IND " + Count(s.NotUpIndependents) + " · " : string.Empty) + Count(s.SenateDem) + " DEM",
                "THE SENATE · " + Count(s.SenateSeats),
                new[] { Count(s.UpRep + s.UpDem + s.UpToCome) + " SEATS TONIGHT · " + classes.ToUpperInvariant(), Count(s.SenateMajority) + " CONTROLS" + (even ? " · " + Count(half) + "–" + Count(half) + ": THE VICE PRESIDENT" : string.Empty),
                    "PALE ENDS · NOT UP: REP " + Count(s.NotUpRep) + " · DEM " + Count(s.NotUpDem) + (s.NotUpIndependents > 0 ? " (" + Count(s.NotUpIndependents) + " IND, WITH DEM)" : string.Empty),
                    "WON TONIGHT · REP " + Count(s.UpRep) + " · DEM " + Count(s.UpDem) + (s.UpToCome > 0 ? " · TO COME " + Count(s.UpToCome) : string.Empty),
                    "THE SENATE THAT SITS FROM " + DeskDay(board.Convenes) },
                s.Senate, s.SenateAt, s.Total, "OPEN · " + Count(s.SenateMajority), Count(half) + "–" + Count(half) + " · THE VP DECIDES",
                s.Senate == UsNight.Call.Tied ? TiedSenateSlip(board) : null);

            // the nation's vote (For Code 7): owed until 51 of 51 - a share of what has declared is an artefact of the order, not the vote
            Transform nation = CanvasRows.HRow(parent, "NationsVote", Px(30f), Px(10f));
            Edge(nation, PoliSimTheme.Hairline, top: true);
            PlText(nation, "Label", s.Presidential ? "The nation's vote" : "The House vote", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(nation);
            bool shown = s.Complete && (s.Presidential ? !double.IsNaN(board.RepShare) : !double.IsNaN(board.HouseV));
            PlText(nation, "Owed", shown ? "shares only" : "owed", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            var nationLines = new List<string>();
            if (shown && s.Presidential)
            {
                string line = "REP " + Share(board.RepShare) + " · DEM " + Share(board.DemShare) + " · OTHER " + Share(board.OtherShare) + Nbsp + "%";
                SizedWrapped(parent, line, Px(14f), PoliSimTheme.TextPrimary, ColumnWidth);
                nationLines.Add(line);
                nationLines.Add("EVERY OTHER CANDIDATE AS ONE COLUMN");
            }
            else if (shown) { string line = "REP " + Pct(board.HouseV) + " of the two-party vote"; SizedWrapped(parent, line, Px(14f), PoliSimTheme.TextPrimary, ColumnWidth); nationLines.Add(line.ToUpperInvariant()); }
            else { nationLines.Add("OWED UNTIL " + Count(s.Total) + " OF " + Count(s.Total) + ": A SHARE OF WHAT HAS DECLARED IS AN ARTEFACT OF THE ORDER"); }
            nationLines.Add("TURNOUT IS NOT MODELLED: SHARES ONLY");
            CanvasRows.Slip(nation.gameObject, overlay, s.Presidential ? "THE NATION'S VOTE" : "THE HOUSE VOTE", nationLines);
            // real904 (e7c): the legend note clipped at the column's foot - it is 29a's dense-mode line, and each bar's slip carries it (TugBar's last slip line)
        }

        /// <summary>The Senate's ballot tonight in words: its class, and the specials beside it.</summary>
        private static string SenateClassWords(UsNight.State s)
        {
            var classes = new SortedDictionary<int, int>();
            foreach (UsNight.Jurisdiction j in s.InOrder) { foreach (UsNight.SenateSeat seat in j.SenateUp) { classes.TryGetValue(seat.Class, out int n); classes[seat.Class] = n + 1; } }
            int main = 0, most = -1;
            foreach (KeyValuePair<int, int> kv in classes) { if (kv.Value > most) { most = kv.Value; main = kv.Key; } }
            int specials = 0;
            foreach (KeyValuePair<int, int> kv in classes) { if (kv.Key != main) { specials += kv.Value; } }
            return main == 0 ? "no class" : "class " + Count(main) + (specials > 0 ? " + " + Count(specials) + " special" : string.Empty);
        }

        private enum Seg { Rep, Dem, RepPale, DemPale, IndPale, ToCome }

        /// <summary>One tug bar (29a): the head (its title and what it counts), the bar's segments from the left in order - DEM's packed against the right
        /// end - the tick at the centre with the majority under it, the figures, the call chip and its slip.</summary>
        private static void TugBar(Transform parent, Transform overlay, string title, string sub, string tick, (int Seats, Seg Kind)[] segments, int total,
            string figLeft, string figMiddle, string figRight, string slipHead, string[] slipLinesIn, UsNight.Call call, int calledAt, int of, string openWords, string stuckWords, string[] stuckSlip, string decidedWords = null)
        {
            var slipLines = new List<string>(slipLinesIn) { "REP FILLS FROM THE LEFT, DEM FROM THE RIGHT; WHAT IS STILL TO COME SITS BETWEEN; THE TICK IS THE MAJORITY" };
            Transform head = CanvasRows.HRow(parent, title + "Head", Px(26f), 8f);
            PlText(head, "Title", title, Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(head);
            PlText(head, "Sub", sub, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);

            var bar = new GameObject(title + "Bar");
            bar.transform.SetParent(parent, false);
            bar.AddComponent<RectTransform>();
            LayoutElement barSize = bar.AddComponent<LayoutElement>();
            barSize.minHeight = barSize.preferredHeight = Px(54f);
            double t = Math.Max(1, total);
            double x = 0.0;
            // DEM's segments sit against the right end, REP's against the left; the middle is what is to come
            foreach ((int seats, Seg kind) in segments)
            {
                if (seats <= 0) { continue; }
                Color fill; Color? rule = null; string word = null;
                switch (kind)
                {
                    case Seg.Rep: fill = UsInk("REP"); word = Count(seats); break;
                    case Seg.Dem: fill = UsInk("DEM"); word = Count(seats); break;
                    case Seg.RepPale: fill = Color.Lerp(UsInk("REP"), PoliSimTheme.Card, 0.55f); break;
                    case Seg.DemPale: fill = Color.Lerp(UsInk("DEM"), PoliSimTheme.Card, 0.55f); break;
                    case Seg.IndPale: fill = Color.Lerp(PoliSimTheme.Neutral, PoliSimTheme.Card, 0.55f); break;
                    default: fill = PoliSimTheme.CardInset; rule = PoliSimTheme.HairlineStrong; break;
                }
                double from = x / t, to = (x + seats) / t;
                bool split = word != null && from < 0.5 && to > 0.5;
                Block(bar.transform, title + " " + kind, from, to, fill, rule, Px(28f), split ? null : word);
                if (split)
                {
                    // real904 (e7c): the 50 + VP tick cut the REP seats' figure (18) - the figure moves to the wider side of the tick, clear of it, or is dropped
                    double gap = 0.03;
                    bool left = 0.5 - from >= to - 0.5;
                    double figureFrom = left ? from : 0.5 + gap, figureTo = left ? 0.5 - gap : to;
                    // real904b (e7e/e7f): a segment narrower than the gap left the figure a block of negative width - no block, the slip carries the figure
                    if (figureTo > figureFrom) { Block(bar.transform, title + " " + kind + " figure", figureFrom, figureTo, Color.clear, null, Px(28f), word); }
                }
                x += seats;
            }
            Tick(bar.transform, 0.5, tick, Px(28f));
            CanvasRows.Slip(bar, overlay, slipHead, slipLines);

            Transform figures = CanvasRows.HRow(parent, title + "Figures", Px(20f), 4f);
            PlText(figures, "Left", figLeft, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold);
            CanvasRows.Spacer(figures);
            PlText(figures, "Middle", figMiddle, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document);
            CanvasRows.Spacer(figures);
            PlText(figures, "Right", figRight, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold);

            Transform chipRow = CanvasRows.HRow(parent, title + "Call", Px(26f), Px(6f));
            string party = UsNight.PartyOf(call);
            GameObject chip;
            var chipSlip = new List<string>();
            if (party != null)
            {
                chip = FilledChip(chipRow, party + " · " + (decidedWords ?? "CALLED AT " + Count(calledAt)), UsInk(party), PoliSimTheme.Card);
                chipSlip.Add(party + (title == "Senate" && party == "DEM" ? " AND THE INDEPENDENTS WITH IT" : string.Empty) + " REACH " + (title == "President" ? "THE MAJORITY OF THE ELECTORS" : "A MAJORITY"));
                chipSlip.Add(decidedWords != null ? decidedWords + " ON THE DAY" : "CALLED AT " + Count(calledAt) + " OF " + Count(of) + " DECLARED, WHEN THE LINE WAS CROSSED");
            }
            else if (call == UsNight.Call.NoMajority || call == UsNight.Call.Tied)
            {
                chip = FilledChip(chipRow, stuckWords, PoliSimTheme.Caution, PoliSimTheme.Card);
                if (stuckSlip != null) { chipSlip.AddRange(stuckSlip); }
                if (decidedWords == null) { chipSlip.Add("CERTAIN AT " + Count(calledAt) + " OF " + Count(of) + " DECLARED"); }
            }
            else
            {
                chip = CanvasRows.Stamp(chipRow, openWords, Px(14f), PoliSimTheme.TextMuted);
                chipSlip.Add("OPEN · NOTHING IS CALLED UNTIL A LINE IS CROSSED");
            }
            CanvasRows.Slip(chip, overlay, title.ToUpperInvariant() + " · THE CALL", chipSlip);
        }

        /// <summary>A chip filled with an ink, its words in paper (29a's call chips).</summary>
        private static GameObject FilledChip(Transform parent, string words, Color fill, Color ink)
        {
            var chip = new GameObject("Chip " + words);
            chip.transform.SetParent(parent, false);
            chip.AddComponent<RectTransform>();
            var layout = chip.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Px(6f), Px(6f), 1, 1);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            chip.AddComponent<Image>().color = fill;
            HoldWidth(PlText(chip.transform, "Words", words, Px(14f), ink, PoliSimTheme.Document, FontStyle.Bold));
            return chip;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE 51
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// THE 51 (29a's centre, 29b's answers 1-3 and 6): the tiles under a lens, the lens chips (President and Congress; Delegations only when the College
        /// has no majority; a midterm night Congress alone), the key for the lens and its one-line note. Each state's slip names it, its electors and how
        /// they went, its delegation against the House it replaces, its Senate seats tonight, and at 51 of 51 its share against the base.
        /// </summary>
        private static void BuildUsMap(Transform parent, UsNightBoard board, Transform overlay, ElectionNightScreen screen, UsTileView.Lens lens)
        {
            UsNight.State s = board.State;
            var lenses = new List<UsTileView.Lens>();
            if (s.Presidential) { lenses.Add(UsTileView.Lens.President); }
            lenses.Add(UsTileView.Lens.Congress);
            if (s.College == UsNight.Call.NoMajority) { lenses.Add(UsTileView.Lens.Delegations); }
            if (!lenses.Contains(lens)) { lens = lenses[0]; }

            Transform head = CanvasRows.HRow(parent, "MapHead", Px(28f), Px(4f));
            PlText(head, "Heading", "The " + Count(s.Total), Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Spacer(head);
            var chips = new Dictionary<UsTileView.Lens, (Image Face, Text Words)>();
            foreach (UsTileView.Lens l in lenses)
            {
                var chip = new GameObject("Lens " + l);
                chip.transform.SetParent(head, false);
                chip.AddComponent<RectTransform>();
                var layout = chip.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(Px(8f), Px(8f), 1, 1);
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                Image face = chip.AddComponent<Image>();
                CanvasRows.Edges(chip.transform, PoliSimTheme.HairlineStrong, 1f);
                Text words = PlText(chip.transform, "Words", LensWords(l), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold);
                HoldWidth(words);
                chips[l] = (face, words);
                Button button = chip.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;   // the chip's face is its state - a tint would draw over the inversion
                UsTileView.Lens chosen = l;
                button.onClick.AddListener(() => screen._usLens?.Invoke(chosen));
                CanvasRows.Slip(chip, overlay, LensWords(l).ToUpperInvariant(), LensSlip(l, board.CountedOn));
            }

            if (UsTileCartogram.States.Length != 51)
            {
                PlWrapped(parent, "the map draws the 51; the table holds " + Count(UsTileCartogram.States.Length), Px(14f), PoliSimTheme.TextMuted);
                return;
            }
            // the review's L5: the tiles are the 2024 apportionment's - one cell an elector, the House seats plus two. A count on another apportionment
            // refuses the map, loudly, rather than draw a cell that is no elector
            string mismatch = CartogramMismatch(board.State);
            if (mismatch != null)
            {
                Debug.LogError("CANVAS: the US tiles are refused - " + mismatch + ": the map is the 2024 apportionment's and tonight's count stands on another; the map is not drawn.");
                PlWrapped(parent, "The map is not drawn: its tiles are the 2024 apportionment's, and tonight's count stands on another.", Px(14f), PoliSimTheme.TextMuted);
                return;
            }
            var fills = new UsTileView.BlockFill[UsTileCartogram.States.Length];
            for (int b = 0; b < fills.Length; b++) { fills[b] = FillOf(board, UsTileCartogram.States[b]); }
            UsTileView view = UsTileView.Create(parent, fills, overlay, lens);
            screen.UsTiles = view;

            var keysObject = new GameObject("Keys");
            keysObject.transform.SetParent(parent, false);
            keysObject.AddComponent<RectTransform>();
            Transform keys = keysObject.transform;
            var keyColumn = keys.gameObject.AddComponent<VerticalLayoutGroup>();
            keyColumn.childControlWidth = keyColumn.childControlHeight = true;
            keyColumn.childForceExpandWidth = true;
            keyColumn.childForceExpandHeight = false;
            keyColumn.spacing = 2f;
            Text note = CanvasChrome.MakeTextRealWeight(parent, "LensNote", string.Empty, PoliSimTheme.Document, Px(14f), PoliSimTheme.TextMuted, TextAnchor.UpperLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.raycastTarget = false;
            LayoutElement noteSize = note.gameObject.AddComponent<LayoutElement>();

            void Apply(UsTileView.Lens l)
            {
                foreach (KeyValuePair<UsTileView.Lens, (Image Face, Text Words)> kv in chips)
                {
                    bool on = kv.Key == l;
                    kv.Value.Face.color = on ? PoliSimTheme.TextPrimary : PoliSimTheme.Card;
                    kv.Value.Words.color = on ? PoliSimTheme.Card : PoliSimTheme.TextPrimary;
                }
                for (int i = keys.childCount - 1; i >= 0; i--) { UnityEngine.Object.Destroy(keys.GetChild(i).gameObject); }
                BuildKeys(keys, l, s.Presidential);
                note.text = LensNote(l);
                noteSize.minHeight = noteSize.preferredHeight = WrappedHeight(note.text, Px(14f), PoliSimTheme.Document, MapWidth);   // real904: measured, never the column's guess
                view.SetLens(l);
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)parent);
            }
            screen._usLens = l => { if (chips.ContainsKey(l)) { Apply(l); } };
            foreach (KeyValuePair<UsTileView.Lens, (Image Face, Text Words)> kv in chips)
            {
                bool on = kv.Key == lens;
                kv.Value.Face.color = on ? PoliSimTheme.TextPrimary : PoliSimTheme.Card;
                kv.Value.Words.color = on ? PoliSimTheme.Card : PoliSimTheme.TextPrimary;
            }
            BuildKeys(keys, lens, s.Presidential);
            note.text = LensNote(lens);
            noteSize.minHeight = noteSize.preferredHeight = WrappedHeight(note.text, Px(14f), PoliSimTheme.Document, MapWidth);
        }

        private static string LensWords(UsTileView.Lens l) => l == UsTileView.Lens.President ? "President" : l == UsTileView.Lens.Congress ? "Congress" : "Delegations";

        private static string[] LensSlip(UsTileView.Lens l, DateTime countedOn) =>
            l == UsTileView.Lens.President ? new[] { "ONE CELL PER ELECTOR, INKED BY THE CALL" }
            : l == UsTileView.Lens.Congress ? new[] { "THE SAME CELLS: A STATE'S ELECTORS ARE", "ITS HOUSE SEATS PLUS ITS TWO SENATORS" }
            : new[] { "HOW THE HOUSE WOULD VOTE ON " + (countedOn != DateTime.MinValue ? DeskDay(countedOn) : "THE COUNT'S DAY") + ":", "EACH STATE WHOLE, BY ITS DELEGATION'S MAJORITY" };

        private static string LensNote(UsTileView.Lens l) =>
            l == UsTileView.Lens.President ? "One cell, one elector: a block is its state's electors. In ME and NE the numbered cells are the districts' electors. Dashed is not yet declared."
            : l == UsTileView.Lens.Congress ? "The same cells re-inked: a block's first two cells are its senators (hatched when not up), the rest its House delegation, REP first. DC has no vote."
            : "Each state whole by its delegation's majority in the House elected tonight. A tied delegation casts no vote; DC has none.";

        /// <summary>The key under the map for a lens, in rows that fit the column (29a's keys; a key never wider than the map).</summary>
        private static void BuildKeys(Transform keys, UsTileView.Lens l, bool presidential)
        {
            var items = new List<(Color? Fill, string Kind, string Words)>();
            if (l == UsTileView.Lens.President)
            {
                items.Add((UsInk("REP"), "ink", "REP")); items.Add((UsInk("DEM"), "ink", "DEM")); items.Add((null, "notyet", "not yet"));
                items.Add((null, "paper", "1 2 3 · a district (ME, NE)"));
            }
            else if (l == UsTileView.Lens.Congress)
            {
                items.Add((UsInk("REP"), "ink", "REP")); items.Add((UsInk("DEM"), "ink", "DEM")); items.Add((null, "hatch", "senator not up"));
                items.Add((null, "dotted", "no seat (DC)")); items.Add((null, "notyet", "not yet"));
            }
            else
            {
                items.Add((UsInk("REP"), "ink", "REP majority")); items.Add((UsInk("DEM"), "ink", "DEM majority")); items.Add((null, "dotted", "tied or none"));
                items.Add((null, "notyet", "not yet"));
            }
            float room = 576f * BoardPx, used = 0f;
            Transform row = null;
            foreach ((Color? fill, string kind, string words) in items)
            {
                float width = Px(12f) + Px(6f) + Measure(words, Px(14f), PoliSimTheme.Document) + Px(14f);
                if (row == null || used + width > room) { row = CanvasRows.HRow(keys, "KeyRow", Px(20f), Px(6f)); used = 0f; }
                used += width;
                var swatch = new GameObject("Swatch " + words);
                swatch.transform.SetParent(row, false);
                swatch.AddComponent<RectTransform>();
                LayoutElement size = swatch.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = size.minHeight = size.preferredHeight = Px(12f);
                Image face = swatch.AddComponent<Image>();
                face.raycastTarget = false;
                face.color = fill ?? (kind == "notyet" ? PoliSimTheme.CardInset : kind == "hatch" ? PoliSimTheme.Hairline : PoliSimTheme.Card);
                if (kind == "notyet" || kind == "paper" || kind == "dotted") { CanvasRows.Edges(swatch.transform, kind == "dotted" ? PoliSimTheme.TextPrimary : PoliSimTheme.HairlineStrong, 1f); }
                HoldWidth(PlText(row, "Key " + words, words, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                Blank(row, Px(8f));
            }
        }

        /// <summary>One jurisdiction's fill under the three lenses and its slips.</summary>
        private static UsTileView.BlockFill FillOf(UsNightBoard board, UsTileCartogram.Block block)
        {
            UsNight.State s = board.State;
            UsNight.Jurisdiction j = s.Find(block.Code);
            int n = block.Cells.GetLength(0);
            var fill = new UsTileView.BlockFill
            {
                Declared = j == null || j.Declared,   // the District on a midterm is counted for nothing: no seat, never "not yet"
                President = new UsTileView.Cell[n], Congress = new UsTileView.Cell[n], Delegations = new UsTileView.Cell[n],
                SlipHead = block.Code + " · " + block.Name,
            };
            UsTileView.Cell Ink(string party) => new UsTileView.Cell { Paint = UsTileView.Paint.Ink, Ink = UsInk(party) };
            var dotted = new UsTileView.Cell { Paint = UsTileView.Paint.Dotted };
            var hatch = new UsTileView.Cell { Paint = UsTileView.Paint.Hatch };
            var notYet = new UsTileView.Cell { Paint = UsTileView.Paint.NotYet };
            bool hasDistricts = false;
            for (int r = 0; r < n; r++) { if (block.Cells[r, 2] > 0) { hasDistricts = true; } }
            int senator = 0, house = 0;
            for (int r = 0; r < n; r++)
            {
                int district = block.Cells[r, 2];
                if (j == null) { fill.President[r] = fill.Congress[r] = fill.Delegations[r] = dotted; continue; }
                if (!j.Declared) { fill.President[r] = fill.Congress[r] = fill.Delegations[r] = notYet; continue; }
                // the President's lens: the district's call in its own cell, the state's elsewhere
                if (j.Winner >= 0)
                {
                    int at = Array.IndexOf(j.Districts, district);
                    int w = district > 0 && at >= 0 && at < j.DistrictWinners.Length ? j.DistrictWinners[at] : j.Winner;
                    fill.President[r] = Ink(w == ElectoralCollege.Republican ? "REP" : "DEM");
                }
                else { fill.President[r] = notYet; }
                // Congress's lens (29b's answer 2): the two at-large cells, else the first two, the senators; the rest the delegation, REP first
                if (j.HouseSeats == 0 && j.SenateUp.Count == 0 && block.Code == "DC") { fill.Congress[r] = dotted; }
                else if (hasDistricts ? district == 0 : senator < 2 && (r < 2))
                {
                    fill.Congress[r] = senator < j.SenateUp.Count ? Ink(j.SenateUp[senator].Side) : hatch;
                    senator++;
                }
                else
                {
                    fill.Congress[r] = house < j.HouseRep ? Ink("REP") : house < j.HouseRep + j.HouseDem ? Ink("DEM") : dotted;
                    house++;
                }
                // Delegations: the state whole by its delegation's majority; a tie, or no seat, dotted
                fill.Delegations[r] = j.HouseRep > j.HouseDem ? Ink("REP") : j.HouseDem > j.HouseRep ? Ink("DEM") : dotted;
            }
            fill.SlipLines = SlipOf(board, block, j, 0);
            for (int r = 0; r < n; r++)
            {
                int district = block.Cells[r, 2];
                if (district <= 0 || fill.DistrictSlipHeads.ContainsKey(district)) { continue; }
                fill.DistrictSlipHeads[district] = block.Code + "-" + Count(district) + " · " + block.Name + ", district " + Count(district);
                fill.DistrictSlipLines[district] = SlipOf(board, block, j, district);
            }
            return fill;
        }

        /// <summary>A state's slip (29a's), or a district's: what it appoints, how it went, its delegation against the House it replaces, its Senate seats
        /// tonight, and at 51 of 51 its share against the base.</summary>
        private static List<string> SlipOf(UsNightBoard board, UsTileCartogram.Block block, UsNight.Jurisdiction j, int district)
        {
            UsNight.State s = board.State;
            var lines = new List<string>();
            if (j == null)
            {
                lines.Add("No seat in Congress and no vote tonight: the District elects no member of the House or the Senate");
                return lines;
            }
            string senate = SenateWords(j, s);
            if (!j.Declared)
            {
                lines.Add("Not yet declared");
                lines.Add((s.Presidential ? Count(j.Electors) + " electors · " : string.Empty) + (j.HouseSeats > 0 ? Count(j.HouseSeats) + (j.HouseSeats == 1 ? " House seat" : " House seats") : "no House seat") + " · Senate: " + senate);
                return lines;
            }
            if (j.Winner >= 0)
            {
                int at = Array.IndexOf(j.Districts, district);
                if (district > 0 && at >= 0)
                {
                    double dem = at < j.DistrictDemTwoParty.Length ? j.DistrictDemTwoParty[at] : double.NaN;
                    int w = at < j.DistrictWinners.Length ? j.DistrictWinners[at] : j.Winner;
                    lines.Add("District " + Count(district) + " · REP " + Pct(1.0 - dem) + " · DEM " + Pct(dem) + " · 1 elector to " + (w == ElectoralCollege.Republican ? "REP" : "DEM"));
                }
                double rep = j.RepTwoParty;
                lines.Add((district > 0 ? "Statewide" : "President") + " · REP " + Pct(rep) + " · DEM " + Pct(1.0 - rep) + " · lead " + (rep >= 0.5 ? "REP" : "DEM") + " " + Pct(Math.Abs(2.0 * rep - 1.0)) + " of the two");
                lines.Add(j.Districts.Length > 0
                    ? "Electors · 2 to the state, 1 per district: REP " + Count(j.ElectorsRep) + " · DEM " + Count(j.ElectorsDem)
                    : "Electors · " + Count(j.Electors) + " to " + (j.Winner == ElectoralCollege.Republican ? "REP" : "DEM"));
                if (j.OthersLead) { lines.Add("Every other candidate together led here; the electors go to the larger nominee"); }
            }
            if (j.HouseSeats > 0)
            {
                string change = string.Empty;
                if (board.SittingHouse != null && board.SittingHouse.TryGetValue(j.Code, out (int Rep, int Dem) sitting)) { change = " · REP " + Signed(j.HouseRep - sitting.Rep) + " on the sitting House"; }
                lines.Add("House · " + Count(j.HouseSeats) + (j.HouseSeats == 1 ? " seat, at large" : " seats") + " · REP " + Count(j.HouseRep) + " · DEM " + Count(j.HouseDem) + change);
            }
            else { lines.Add("House · the District elects none"); }
            lines.Add("Senate · " + senate);
            if (s.Complete && j.Winner >= 0 && board.BaseRepTwoParty != null && board.BaseRepTwoParty.TryGetValue(j.Code, out double baseRep))
            {
                bool same = (baseRep > 0.5) == (j.RepTwoParty > 0.5);
                lines.Add((board.BaseLabel ?? "Before") + " · REP " + Pct(baseRep) + " · REP " + SignedPp(100.0 * (j.RepTwoParty - baseRep)) + " · " + (same ? "called alike" : "called otherwise"));
            }
            return lines;
        }

        /// <summary>The review's L5: where a block's cells are not the count's - a jurisdiction's electors, or its House seats plus two - its words; null where
        /// every block is the count's (a jurisdiction the night does not count, the District on a midterm, is no block to compare).</summary>
        private static string CartogramMismatch(UsNight.State s)
        {
            foreach (UsTileCartogram.Block block in UsTileCartogram.States)
            {
                UsNight.Jurisdiction j = s.Find(block.Code);
                if (j == null) { continue; }
                int cells = block.Cells.GetLength(0);
                if (s.Presidential && j.Electors != cells) { return block.Code + " appoints " + Count(j.Electors) + " electors against " + Count(cells) + " cells"; }
                if (j.HouseSeats > 0 && j.HouseSeats + 2 != cells) { return block.Code + " holds " + Count(j.HouseSeats) + " House seats against " + Count(cells) + " cells"; }
            }
            return null;
        }

        /// <summary>The tied Senate's slip (the review's M1): who breaks the tie and to when - the sitting Vice President to the term's real end, and on a
        /// presidential night the Vice President-elect from the oath, the side named once the College is called.</summary>
        private static string[] TiedSenateSlip(UsNightBoard board)
        {
            var lines = new List<string> { "AN EQUAL SENATE: THE VICE PRESIDENT'S VOTE DECIDES (THE CONSTITUTION'S SENATE CLAUSE)" };
            string sitting = (board.VicePresident ?? "THE SITTING VICE PRESIDENT").ToUpperInvariant() + (board.VicePresidentParty != null ? " (" + board.VicePresidentParty + ")" : string.Empty);
            string holds = board.VicePresidentParty != null ? ": " + board.VicePresidentParty + " HOLDS IT" : string.Empty;
            lines.Add(board.SittingUntil != DateTime.MinValue ? sitting + " TO " + DeskDay(board.SittingUntil) + holds : sitting + holds);
            if (board.TakesOffice != DateTime.MinValue)
            {
                string called = UsNight.PartyOf(board.State.College);
                string mate = called == "REP" ? board.RepRunningMate : called == "DEM" ? board.DemRunningMate : null;
                lines.Add(called != null
                    ? "FROM " + DeskDay(board.TakesOffice) + " THE VICE PRESIDENT-ELECT" + (string.IsNullOrEmpty(mate) ? string.Empty : ", " + mate.ToUpperInvariant()) + ": " + called + " HOLDS IT"
                    : "FROM " + DeskDay(board.TakesOffice) + " THE VICE PRESIDENT-ELECT - WHICH SIDE IS OPEN UNTIL THE COLLEGE IS CALLED");
            }
            return lines.ToArray();
        }

        private static string SenateWords(UsNight.Jurisdiction j, UsNight.State s)
        {
            if (j.SenateUp.Count == 0) { return "no seat on the ballot"; }
            var parts = new List<string>();
            foreach (UsNight.SenateSeat seat in j.SenateUp)
            {
                string term = seat.Term == "full" ? "class " + Count(seat.Class) : "class " + Count(seat.Class) + " special";
                parts.Add(j.Declared ? term + " " + seat.Side : term);
            }
            return string.Join(" · ", parts);
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // WHO GOVERNS
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// WHO GOVERNS (29a's right column, 29b's answers 6-8): the sitting President, Vice President and Congress to their dates; the ELECT card once the
        /// College is called (28c's form) or the no-majority card with tonight's delegations; at 51 of 51 on the night the record holds, the reference; and
        /// "Until the oath".
        /// </summary>
        private static void BuildUsGoverns(Transform parent, UsNightBoard board, Transform overlay)
        {
            UsNight.State s = board.State;
            Transform head = CanvasRows.HRow(parent, "GovernsHead", Px(28f), 8f);
            PlText(head, "Heading", "Who governs", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);

            bool presidentialYear = board.TakesOffice != DateTime.MinValue;
            string sittingTo = board.SittingUntil != DateTime.MinValue ? "to " + (presidentialYear ? ShortDay(board.SittingUntil) : board.SittingUntil.ToString("MMM yyyy", CultureInfo.InvariantCulture)) : "in office";
            SittingRow(parent, overlay, "President", board.PresidentParty, UsRowName(board.President, board.PresidentParty), sittingTo,
                (board.President ?? "THE PRESIDENT").ToUpperInvariant() + (board.PresidentParty != null ? " · " + board.PresidentParty : string.Empty),
                presidentialYear ? new[] { "SERVES UNTIL THE OATH ON " + DeskDay(board.TakesOffice) } : new[] { "NO PRESIDENT IS ELECTED AT A MIDTERM", board.SittingUntil != DateTime.MinValue ? "THE TERM ENDS " + DeskDay(board.SittingUntil) : "THE TERM'S END IS NOT HELD" });
            SittingRow(parent, overlay, "VP", board.VicePresidentParty, UsRowName(board.VicePresident, board.VicePresidentParty), sittingTo,
                (board.VicePresident ?? "THE VICE PRESIDENT").ToUpperInvariant() + (board.VicePresidentParty != null ? " · " + board.VicePresidentParty : string.Empty),
                new[] { "BREAKS AN EQUAL SENATE" + (board.SittingUntil != DateTime.MinValue ? " UNTIL " + DeskDay(board.SittingUntil) : string.Empty) });
            string congress = board.SittingCongress > 0 ? "the " + UsSenate.Ordinal(board.SittingCongress) : "the sitting";
            SittingRow(parent, overlay, "Congress", null, congress, "to " + ShortDay(board.Convenes),
                (congress + " Congress").ToUpperInvariant(), new[] { "SITS UNTIL THE NEW CONGRESS IS SEATED ON " + DeskDay(board.Convenes) });

            string called = UsNight.PartyOf(s.College);
            if (called != null)
            {
                string name = called == "REP" ? board.RepNominee : board.DemNominee;
                string mate = called == "REP" ? board.RepRunningMate : board.DemRunningMate;
                Transform card = CanvasRows.HRow(parent, "Elect", Px(44f), Px(8f));
                card.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(Px(10f), Px(10f), 0, 0);
                Image face = card.gameObject.AddComponent<Image>();
                face.color = PoliSimTheme.Hex(0xF5EDDE);
                CanvasRows.Edges(card, UsInk(called), Px(2f));
                CanvasRows.Stamp(card, "ELECT", Px(14f), PoliSimTheme.TextPrimary);
                HoldWidth(PlText(card, "Name", UsRowName(name, called) ?? called, Px(17f), PoliSimTheme.TextPrimary, PoliSimTheme.Body, FontStyle.Bold));
                PlText(card, "Party", called, Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body);
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(mate)) { lines.Add(mate.ToUpperInvariant() + ", VICE PRESIDENT-ELECT"); }
                if (board.ElectorsMeet != DateTime.MinValue) { lines.Add("THE ELECTORS VOTE " + DeskDay(board.ElectorsMeet) + " · CONGRESS COUNTS " + DeskDay(board.CountedOn)); }
                lines.Add("THE OATH " + DeskDay(board.TakesOffice));
                CanvasRows.Slip(card.gameObject, overlay, (name ?? called).ToUpperInvariant() + " · PRESIDENT-ELECT", lines);
            }
            else if (s.College == UsNight.Call.NoMajority)
            {
                Transform card = Card(parent, "NoMajority", PoliSimTheme.Hex(0xC9A23A), Px(2f));
                card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Px(10f), Px(10f), Px(8f), Px(8f));
                card.GetComponent<VerticalLayoutGroup>().spacing = Px(4f);
                Transform top = CanvasRows.HRow(card, "Head", Px(22f), Px(8f));
                PlText(top, "Words", "No majority", Px(17f), PoliSimTheme.TextPrimary, PoliSimTheme.Body, FontStyle.Bold);
                CanvasRows.Spacer(top);
                FilledChip(top, "HOUSE · " + ShortDay(board.CountedOn).ToUpperInvariant(), PoliSimTheme.Caution, PoliSimTheme.Card);
                int majority = board.AllStates / 2 + 1;
                PlWrapped(card, "One vote a state · " + Count(majority) + " of " + Count(board.AllStates) + " elects", Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
                Transform tally = CanvasRows.HRow(card, "Delegations", Px(20f), Px(10f));
                HoldWidth(PlText(tally, "Rep", "REP " + Count(s.DelegationsRep), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold));
                HoldWidth(PlText(tally, "Dem", "DEM " + Count(s.DelegationsDem), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold));
                HoldWidth(PlText(tally, "Tied", "tied " + Count(s.DelegationsTied), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document, FontStyle.Bold));
                if (s.DelegationsToCome > 0) { HoldWidth(PlText(tally, "ToCome", Count(s.DelegationsToCome) + " to come", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
                CanvasRows.Slip(card.gameObject, overlay, "NO MAJORITY IN THE COLLEGE", new[]
                {
                    Count(s.ElectorsRep) + "–" + Count(s.ElectorsDem) + ": NEITHER SIDE HAS " + Count(s.ElectorsMajority),
                    "THE HOUSE CHOOSES ON " + DeskDay(board.CountedOn) + ", ONE VOTE A STATE (THE TWELFTH AMENDMENT)",
                    "TONIGHT'S DELEGATIONS · REP " + Count(s.DelegationsRep) + " · DEM " + Count(s.DelegationsDem) + " · TIED " + Count(s.DelegationsTied),
                    Count(majority) + " OF " + Count(board.AllStates) + " ELECTS; A TIED DELEGATION CASTS NONE",
                    "IF IT CANNOT, THE SENATE CHOOSES THE VICE PRESIDENT (" + Count(UsSenate.Members / 2 + 1) + ")",
                });
            }

            if (s.Complete && board.HasReference) { BuildUsReference(parent, board, overlay); }
            if (s.Complete) { BuildUntilTheOath(parent, board, overlay); }
            else
            {
                PlWrapped(parent, "Each state declares its three verdicts at once: its electors, its delegation and its Senate seat. A call lands when a line is crossed. "
                    + "The sitting President and Congress serve until " + ShortDay(board.TakesOffice != DateTime.MinValue ? board.TakesOffice : board.Convenes) + " and " + ShortDay(board.Convenes) + ".",
                    Px(14f), PoliSimTheme.TextMuted);
            }
        }

        /// <summary>A sitting row (29a): the role, the party's ink, the name, the date it serves to.</summary>
        private static void SittingRow(Transform parent, Transform overlay, string role, string party, string name, string until, string slipHead, string[] slipLines)
        {
            Transform row = CanvasRows.HRow(parent, "Sitting " + role, Px(28f), Px(8f));
            Edge(row, PoliSimTheme.Hairline, top: false);
            CanvasRows.FixedCell(row, role, Px(78f), Px(14f), PoliSimTheme.TextMuted);
            var swatch = new GameObject("Ink");
            swatch.transform.SetParent(row, false);
            swatch.AddComponent<RectTransform>();
            LayoutElement size = swatch.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = size.minHeight = size.preferredHeight = Px(12f);
            Image ink = swatch.AddComponent<Image>();
            ink.color = party != null ? UsInk(party) : PoliSimTheme.HairlineStrong;
            ink.raycastTarget = false;
            // real904s (88n): the name collapsed to 43 units under its 160 - it takes the row's remaining width now, and never less than its own
            Text nameText = PlText(row, "Name", name ?? "—", Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            LayoutElement nameSize = nameText.gameObject.AddComponent<LayoutElement>();
            nameSize.minWidth = nameSize.preferredWidth = Measure(nameText.text, nameText.fontSize, nameText.font);
            nameSize.flexibleWidth = 1f;
            HoldWidth(CanvasRows.Stamp(row, until, Px(14f), PoliSimTheme.TextPrimary).GetComponentInChildren<Text>());
            CanvasRows.Slip(row.gameObject, overlay, slipHead, slipLines);
        }

        /// <summary>"2024, as it happened" (29b's answer 7, 21d's played · real · diff): the College's and the House's REP seats, and the Senate's where the
        /// record holds the Senate that opened on 3 January, else owed; the states and districts called otherwise than the record.</summary>
        private static void BuildUsReference(Transform parent, UsNightBoard board, Transform overlay)
        {
            UsNight.State s = board.State;
            Transform head = CanvasRows.HRow(parent, "ReferenceHead", Px(26f), 8f);
            string label = (board.ReferenceLabel ?? "AS IT HAPPENED").ToLowerInvariant();
            label = label.StartsWith("usa ", StringComparison.Ordinal) ? label.Substring("usa ".Length) : label;
            PlText(head, "Label", char.ToUpperInvariant(label[0]) + label.Substring(1), Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            CanvasRows.Slip(head.gameObject, overlay, "DATED", new[] { "THE REAL RESULT, AS THE RECORD HOLDS IT", "THE RECORD AS OF " + DeskDay(WorldClock.RecordDate) });

            float nameW = Px(100f), figW = Px(56f);   // 29a's grid, 100 then three of what is left, inside the 290 with the row's gaps
            Transform keys = CanvasRows.HRow(parent, "ReferenceKeys", Px(20f), Px(6f));
            CanvasRows.FixedCell(keys, "REP", nameW, Px(14f), PoliSimTheme.TextMuted);
            CanvasRows.FixedCell(keys, "played", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "real", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            CanvasRows.FixedCell(keys, "diff", figW, Px(14f), PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            void Row(string what, int played, int? real, string slip)
            {
                Transform row = CanvasRows.HRow(parent, "Reference " + what, Px(23f), Px(6f));
                Edge(row, PoliSimTheme.Hairline, top: true);
                CanvasRows.FixedCell(row, what, nameW, Px(14f), PoliSimTheme.TextPrimary);
                CanvasRows.FixedCell(row, Count(played), figW, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, real.HasValue ? Count(real.Value) : "owed", figW, real.HasValue ? Px(16f) : Px(14f), real.HasValue ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, real.HasValue ? Signed(played - real.Value) : "—", figW, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.Slip(row.gameObject, overlay, what.ToUpperInvariant() + " · REP", new[] { slip, "THE DIFFERENCE IS PLAYED LESS REAL" });
            }
            if (s.Presidential) { Row("College", s.ElectorsRep, board.RealElectorsRep, "REAL: THE ELECTORS AS APPOINTED, NARA'S"); }
            Row("House", s.HouseRep, board.RealHouseRep, "REAL: THE CLERK'S WINNERS BY DISTRICT");
            Row("Senate", s.SenateRep, board.RealSenateRep, board.RealSenateRep.HasValue ? "REAL: THE SENATE OF RECORD AS IT OPENED ON " + DeskDay(board.Convenes) : "THE RECORD'S SENATE ON THAT DAY IS NOT HELD: OWED");

            var otherwise = new List<string>();
            if (board.RecordWinners != null)
            {
                foreach (UsNight.Jurisdiction j in s.InOrder)
                {
                    if (j.Winner < 0) { continue; }
                    if (board.RecordWinners.TryGetValue(j.Code, out string real) && real != (j.Winner == ElectoralCollege.Republican ? "REP" : "DEM")) { otherwise.Add(j.Code); }
                    for (int d = 0; d < j.Districts.Length && d < j.DistrictWinners.Length; d++)
                    {
                        string key = j.Code + "-" + Count(j.Districts[d]);
                        if (board.RecordWinners.TryGetValue(key, out string realD) && realD != (j.DistrictWinners[d] == ElectoralCollege.Republican ? "REP" : "DEM")) { otherwise.Add(key); }
                    }
                }
            }
            if (s.Presidential)
            {
                Transform line = CanvasRows.HRow(parent, "Otherwise", Px(22f), 4f);
                Edge(line, PoliSimTheme.Hairline, top: true);
                PlText(line, "Words", otherwise.Count == 0 ? "called otherwise: none" : "otherwise: " + string.Join(" · ", otherwise.Count > 6 ? otherwise.GetRange(0, 6) : otherwise) + (otherwise.Count > 6 ? " …" : string.Empty),
                    Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document);
                CanvasRows.Slip(line.gameObject, overlay, "CALLED OTHERWISE THAN THE RECORD", otherwise.Count == 0 ? new List<string> { "NONE: EVERY STATE AND DISTRICT AS IN THE RECORD" } : SlipLines(string.Join(" · ", otherwise)));
            }
        }

        /// <summary>"Until the oath" (29b's answer 8): the electors vote, the new Congress sits, Congress counts (or the House chooses, amber), the oath - on a
        /// midterm night the Congress's day alone.</summary>
        private static void BuildUntilTheOath(Transform parent, UsNightBoard board, Transform overlay)
        {
            UsNight.State s = board.State;
            Transform head = CanvasRows.HRow(parent, "OathHead", Px(24f), 8f);
            PlText(head, "Label", s.Presidential ? "Until the oath" : "Until the new Congress", Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            Transform row = CanvasRows.HRow(parent, "OathDays", Px(44f), Px(4f));
            bool house = s.College == UsNight.Call.NoMajority;
            var days = new List<(DateTime Day, string Words, bool Amber, string[] Slip)>();
            if (s.Presidential && board.ElectorsMeet != DateTime.MinValue) { days.Add((board.ElectorsMeet, "electors", false, new[] { "THE ELECTORS VOTE IN THEIR STATES", DeskDay(board.ElectorsMeet) })); }
            days.Add((board.Convenes, "Congress", false, new[] { "THE NEW CONGRESS IS SEATED", DeskDay(board.Convenes) + ": THE HOUSE ELECTED TONIGHT" }));
            if (s.Presidential && board.CountedOn != DateTime.MinValue)
            {
                days.Add((board.CountedOn, house ? "the House" : "the count", house, house
                    ? new[] { "THE HOUSE CHOOSES", "ONE VOTE A STATE · " + Count(board.AllStates / 2 + 1) + " OF " + Count(board.AllStates) + " ELECTS", "IF IT CANNOT, THE SENATE CHOOSES THE VICE PRESIDENT" }
                    : new[] { "CONGRESS COUNTS THE VOTES", DeskDay(board.CountedOn) }));
            }
            if (s.Presidential && board.TakesOffice != DateTime.MinValue) { days.Add((board.TakesOffice, "the oath", false, new[] { "THE OATH", DeskDay(board.TakesOffice) + ", NOON" })); }
            foreach ((DateTime day, string words, bool amber, string[] slip) in days)
            {
                var cell = new GameObject("Day " + words);
                cell.transform.SetParent(row, false);
                cell.AddComponent<RectTransform>();
                var column = cell.AddComponent<VerticalLayoutGroup>();
                column.childControlWidth = column.childControlHeight = true;
                column.childForceExpandWidth = column.childForceExpandHeight = false;
                column.spacing = 2f;
                LayoutElement width = cell.AddComponent<LayoutElement>();
                width.flexibleWidth = 1f;
                width.minWidth = 0f;
                width.preferredWidth = 0f;
                GameObject chip = amber ? FilledChip(cell.transform, ShortDay(day).ToUpperInvariant(), PoliSimTheme.Caution, PoliSimTheme.Card)
                    : CanvasRows.Stamp(cell.transform, ShortDay(day).ToUpperInvariant(), Px(14f), PoliSimTheme.TextPrimary);
                HoldWidth(chip.GetComponentInChildren<Text>());
                PlText(cell.transform, "Words", words, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
                CanvasRows.Slip(cell, overlay, slip[0], new List<string>(slip).GetRange(1, slip.Length - 1));
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // 6 JANUARY - THE HOUSE CHOOSES
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// US-34 (29b's answer 6 and For Code 6): THE HOUSE'S OWN MOMENT - a short takeover on the count's day where the College had no majority, from the
        /// contest the day loop decided (`UsPresidentialElection.HoldHouseChoice`): one vote a state, the fifty as a tug bar to the majority of all the
        /// states, the tied delegations between; and where the House could not choose, the Senate's choice of the Vice President to a majority of its
        /// whole number. Null, and said, where the contest is not contingent or the House has not voted.
        /// </summary>
        public static ElectionNightScreen BuildUsHouseChoice(UsPresidentialElection.Contest contest, int allStates, string verdict)
        {
            if (contest == null || !contest.Contingent || contest.Delegations.Count == 0)
            {
                Debug.LogWarning("CANVAS: the House's choice has no votes - the board is dropped.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }
            Masthead(content, "THE UNITED STATES  ·  THE HOUSE CHOOSES", DeskDay(contest.CountedOn) + "  ·  ONE VOTE A STATE", contest.Decided() ? "THE HOUSE HAS CHOSEN" : "THE HOUSE CANNOT CHOOSE");
            Transform body = Body(content);
            Transform overlay = screen.Root.transform;
            Transform left = Column(body, "Votes", 700f);
            left.GetComponent<VerticalLayoutGroup>().spacing = Px(4f);
            Transform right = Column(body, "Who", 420f);
            right.GetComponent<VerticalLayoutGroup>().spacing = Px(6f);

            int rep = 0, dem = 0;
            string repName = null, demName = null;
            foreach (UsPresidentialElection.Tally t in contest.Delegations)
            {
                if (t.Party == "REP") { rep = t.Votes; repName = UsTermLimit.PersonOf(contest, t); }
                else if (t.Party == "DEM") { dem = t.Votes; demName = UsTermLimit.PersonOf(contest, t); }
            }
            int states = Math.Max(allStates, rep + dem), majority = states / 2 + 1, none = Math.Max(0, states - rep - dem);
            TugBar(left, overlay, "The House", "one vote a state · " + Count(states), Count(majority),
                new[] { (rep, Seg.Rep), (none, Seg.ToCome), (dem, Seg.Dem) }, states, "REP " + Count(rep), none > 0 ? Count(none) + " cast none" : string.Empty, Count(dem) + " DEM",
                "THE HOUSE · BY STATES", new[] { "A MAJORITY OF ALL THE STATES, " + Count(majority) + ", ELECTS (THE TWELFTH AMENDMENT)", "A TIED DELEGATION CASTS NONE" },
                rep >= majority ? UsNight.Call.Republican : dem >= majority ? UsNight.Call.Democrat : UsNight.Call.NoMajority, states, states, "OPEN", "NO ONE HAS " + Count(majority),
                new[] { "THE HOUSE CANNOT CHOOSE", "THE SENATE CHOOSES THE VICE PRESIDENT, WHO ACTS FROM THE OATH" }, "CHOSEN");
            if (!contest.Decided() && contest.SenateChoice.Count > 0)
            {
                int members = UsSenate.Members, needed = members / 2 + 1, sr = 0, sd = 0;
                foreach (UsPresidentialElection.Tally t in contest.SenateChoice) { if (t.Party == "REP") { sr = t.Votes; } else if (t.Party == "DEM") { sd = t.Votes; } }
                TugBar(left, overlay, "The Senate", "the Vice President · " + Count(members), Count(needed),
                    new[] { (sr, Seg.Rep), (Math.Max(0, members - sr - sd), Seg.ToCome), (sd, Seg.Dem) }, members, "REP " + Count(sr), string.Empty, Count(sd) + " DEM",
                    "THE SENATE · THE VICE PRESIDENT", new[] { "A MAJORITY OF THE WHOLE NUMBER, " + Count(needed) + ", CHOOSES (THE TWELFTH AMENDMENT)" },
                    sr >= needed ? UsNight.Call.Republican : sd >= needed ? UsNight.Call.Democrat : UsNight.Call.NoMajority, members, members, "OPEN", "NO ONE HAS " + Count(needed),
                    new[] { "THE SENATE CANNOT CHOOSE EITHER", "THE SPEAKER ACTS AS PRESIDENT (3 U.S.C. 19)" }, "CHOSEN");
            }

            Transform head = CanvasRows.HRow(right, "WhoHead", Px(28f), 8f);
            PlText(head, "Heading", "Who governs", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            string chosen = contest.Decided() ? (contest.WinnerParty == "REP" ? repName : demName) ?? contest.Winner : null;
            Transform card = Card(right, "Choice", contest.Decided() ? UsInk(contest.WinnerParty) : PoliSimTheme.Hex(0xC9A23A), Px(2f));
            PlWrapped(card, contest.Decided() ? "THE HOUSE HAS CHOSEN" : "THE HOUSE CANNOT CHOOSE", Px(15f), PoliSimTheme.TextMuted);
            PlWrapped(card, contest.Decided() ? chosen + " · " + contest.WinnerParty : "No one has " + Count(majority) + " of " + Count(states), Px(17f), PoliSimTheme.TextPrimary, PoliSimTheme.Body);
            PlWrapped(card, contest.Decided() ? "The oath " + DeskDay(contest.TakesOffice) + "." : contest.Acting != null ? contest.Acting + " acts from the oath." : "Who acts from the oath follows the Twentieth Amendment.", Px(14f), PoliSimTheme.TextMuted);
            PlWrapped(right, "Each state casts one vote, by its delegation's majority in the House that sits from 3 January; a tied delegation casts none.", Px(14f), PoliSimTheme.TextMuted);
            BuildFooter(content, verdict, screen, null, null, overlay);
            Resolve(contentRect);
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE PIECES
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>A US party's ink (29b's answer 9): the installed one, or the Neutral where none is on disk (the independents take it).</summary>
        private static Color UsInk(string key) => key != null && PoliSimTheme.HasPartyInk(CountryId.USA, key) ? PoliSimTheme.PartyLaddered(CountryId.USA, key) : PoliSimTheme.Neutral;

        /// <summary>A row's name at the column's width: the surname (`WorldClock.SurnameOf`), or for a candidate the model has not named (the party's label
        /// with its year) the party and "candidate" - the Polish film found such a label wider than its row (88n). The slip keeps the name in full.</summary>
        private static string UsRowName(string name, string party)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            if (UsTermLimit.IsUnnamedLabel(name) && !string.IsNullOrEmpty(party)) { return party; }   // real904s: the party's key, the short form - the slip keeps the label whole
            return WorldClock.SurnameOf(name);
        }

        /// <summary>[AUTHORED] layout - 29a's column widths in canvas units (290 and 576 board px at 1.5 units a pixel): the widths a wrapped line is measured at.</summary>
        private static float ColumnWidth => 290f * BoardPx;
        private static float MapWidth => 576f * BoardPx;

        /// <summary>A share in % with one decimal, no sign - the nation's line carries the % once.</summary>
        private static string Share(double percent) => percent.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>The height a wrapped text takes at <paramref name="width"/> - measured on a probe at that width (§578: a Canvas text measures at its rect's
        /// current width, so the probe is given the column's).</summary>
        private static float WrappedHeight(string text, int size, Font font, float width)
        {
            Text probe = CanvasChrome.MakeTextRealWeight(null, "Probe", text, font, size, Color.white, TextAnchor.UpperLeft);
            probe.horizontalOverflow = HorizontalWrapMode.Wrap;
            probe.rectTransform.sizeDelta = new Vector2(width, 0f);
            TextGenerationSettings settings = probe.GetGenerationSettings(new Vector2(width, 0f));
            float h = probe.cachedTextGeneratorForLayout.GetPreferredHeight(text, settings) / probe.pixelsPerUnit;
            UnityEngine.Object.Destroy(probe.gameObject);
            return Mathf.Ceil(h) + 2f;
        }

        /// <summary>A wrapped line floored at its measured height at <paramref name="width"/> (real904: the nation's line clipped at its one-line floor).</summary>
        private static void SizedWrapped(Transform parent, string text, int size, Color ink, float width)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, "Note", text, PoliSimTheme.Document, size, ink, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.raycastTarget = false;
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = WrappedHeight(text, t.fontSize, t.font, width);
        }

        private static string Count(int n) => n.ToString(CultureInfo.InvariantCulture);

        private static string Signed(int n) => n > 0 ? "+" + Count(n) : n < 0 ? "−" + Count(-n) : "0";

        /// <summary>A day in 29a's short form - "20 Jan".</summary>
        private static string ShortDay(DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);
    }
}
