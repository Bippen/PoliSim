using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// CANVAS SCREEN 3 — **board 1h, ELECTION NIGHT**, the slot reserved for it since v2 and the
    /// one unbuilt board in §A.14. Built on the two pilots' recorded patterns exactly as 1g was:
    /// `CanvasChrome.EnsureHost` / `Sliced` / `MakeText`, `ui_scrim_takeover` as the canvas-side
    /// ground, `ui_frame_ornate` as a border-only sliced Image, layout components wherever text can
    /// vary, and the ABSENCE GUARD — a name is checked against disk before use and a missing
    /// sprite drops the furniture rather than inventing one.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1.** The screen is handed a `NightState` and draws
    /// it; it reaches no `World` and starts no clock of its own.
    ///
    /// **What this screen may not do, and why the model makes it easy.** Every figure it draws
    /// comes from a `NightState` that was itself computed from the DECLARED constituencies only,
    /// so a result cannot appear before its constituency arrives — not because the screen is
    /// careful, but because the number does not exist yet. A constituency that has not declared is
    /// drawn as a name with an em dash where its figures would be, never as a row of zeroes: an
    /// undeclared seat and a seat won with nothing are different facts.
    ///
    /// And the CALL is drawn only when the model says it is safe — safe meaning it holds across
    /// the whole feasible range of what is outstanding, on the same `SeatAllocation` that
    /// reproduces 2022 and 2026 seat-for-seat (§601). The screen has no call rule of its own to disagree with.
    ///
    /// **Declared deviations (V-N series), per the boards-deviation practice:**
    /// - V-N1: the paper is a flat fill and the drop shadow a single dark plate — the CSS gradient
    ///   and double shadow have no delivered sprite (the 1g precedent, V-S1, same absence).
    /// - V-N2: the declaration-wave, count-up and stamp-thunk BEATS are not animated. The screen is
    ///   filmed in four states, and an animation the film cannot show is a claim no capture can
    ///   check; the states are the honest subset. The beats stay in the spec for the wiring item.
    /// - V-N3: ✅ **BUILT at C-D5 (2026-08-31), at the level the data honestly supports, and the
    ///   original deviation was too broad.** The swing against a NAMED previous election is shown on
    ///   the completed count — Sweden 2018 in the film, sourced. ⚠ **It is withheld while the count is
    ///   partial, and the screen says why**: early in the night `CountedShare` is the share of four
    ///   declared constituencies, and setting that beside a full previous national result prints a
    ///   number that looks like a swing and is an artefact of which places declared first — the most
    ///   misleading thing this screen could show, on the night it matters most. ✅ **The RUNNING swing
    ///   landed 2026-09-10**: given the previous election's PER-CONSTITUENCY count, the comparison is
    ///   restricted to what has declared and is shown at every instant. A first-term night always had
    ///   it - the seed (2026 since K-1) IS the previous election - so V-N3 never needed a second one. Only a later
    ///   term, whose record keeps national figures, still waits for the complete count.
    /// </summary>
    public partial class ElectionNightScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>The cartogram at the page's centre, or null when the count has no map (a country without Sweden's 29).</summary>
        public ValkretsCartogramView Map { get; private set; }

        /// <summary>What the night compares against - whichever of the three the caller has.</summary>
        private sealed class Previous
        {
            public string Label;
            public long[][] ByConstituency;
            public double[] Shares;
            public int[] Seats;
        }

        /// <summary>PS-2 (§619), the spec's §7: HISTORY AS THE REFERENCE - what actually happened at this election, where the record holds it.
        /// The real seats per party in the night's party order and the government the record shows after the election, in one line.</summary>
        public sealed class Reference
        {
            public string Label;
            public int[] Seats;
            public string GovernmentLine;
            /// <summary>Board 21d (§685): the government of record after the election as parts - its head's party and surname, whether a caretaker,
            /// from when - so the line draws as a mark, words and a date stamp. Null <see cref="HeadParty"/>: the record shows no change, or no head's party.</summary>
            public string HeadParty, HeadSurname, HeadQualifier;
            public DateTime HeadFrom, RecordDate;
            public bool HeadIsCaretaker;
            /// <summary>§767 (Elias's ruling D1): the played vote's history note (`WorldClock.Reference.HistoryNote`) - drawn under the head, null where clean.</summary>
            public string HistoryNote;
        }

        /// <summary>Board 21d (§685): the night's foot where the Speaker's round runs - who the Speaker asks first (a mark), whether that is the
        /// player's party, and the one caretaker fact; the sentence is the control's slip.</summary>
        public sealed class SpeakerFoot
        {
            public CountryId Country;
            public string FirstAsked;
            public bool PlayerFirst;
            public string Sentence;
        }

        /// <summary>P2-0.2 (2026-09-02): set by the board's own CONTINUE - the takeover's exit. The seam
        /// covers out on it and the controller applies the office verdict after the cover; a takeover
        /// with no exit is the trap DeadStateCheck reported the first time this board was wired.</summary>
        public bool Dismissed { get; private set; }

        /// <summary>CONTINUE, pressed - by the button or by the controller on the harness's behalf.</summary>
        public void Dismiss()
        {
            Dismissed = true;
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) { Root.SetActive(visible); }
        }

        public void Destroy()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
            }
        }

        /// <summary>[AUTHORED-DRAFT] §A.14's document width for 1h.</summary>

        /// <summary>
        /// Build the board for one instant of the night. Returns null (and says so) if the
        /// furniture is missing — the absence guard the two pilots set, never an invented sprite.
        /// </summary>
        /// <param name="verdict">P2-0.2: the office test's verdict for the player, printed on the board's foot beside
        /// CONTINUE - the count and the verdict are the only election outcome a player sees. Null when the
        /// board is filmed without a player (the harness's staged nights).</param>
        /// <param name="standingBudget">Board 5c (D11 row 3): the estimate that travelled with the standing budget act - what the
        /// new chamber's first budget carries if it changes nothing - drawn in the effects grammar at the right. Null when no
        /// enacted budget carries one, and the board says so.</param>
        /// <param name="standingBudgetCitation">The act's citation (division number, date, title) for the plate's own line.</param>
        /// <param name="previousByConstituency">Election night item 3 (2026-09-10): the previous election's count PER CONSTITUENCY,
        /// in the night's constituency and party order. When given, the swing is LIKE FOR LIKE - the declared constituencies
        /// against the same constituencies last time - and is shown at every instant, which is what V-N3 was waiting for.</param>
        /// <param name="previousShares">The previous election's NATIONAL shares, party order - a later term has no per-constituency
        /// record, so its swing is shown on the complete count only, as V-N3 was first built.</param>
        /// <param name="previousSeats">The previous chamber's seats, party order - the seat change on the complete count.</param>
        /// <param name="government">Item 3: the formation on the new chamber (W-D3) with the player's standing by C-R4's rule.
        /// Null draws the section saying no formation was run, never an invented government.</param>
        /// <param name="inkCountry">Whose laddered inks and marks the map draws in.</param>
        public static ElectionNightScreen Build(NightState state, string[] partyNames, string countryName,
            DateTime pollsClosed, int totalSeats, long[] previousVotes = null, string previousLabel = null,
            string verdict = null, VoteAttribution.Ledger ledger = null, string ledgerParty = null,
            IReadOnlyList<DivisionEffect> standingBudget = null, string standingBudgetCitation = null,
            long[][] previousByConstituency = null, double[] previousShares = null, int[] previousSeats = null,
            GovernmentFormation.View government = null, CountryId inkCountry = CountryId.Sweden, Reference reference = null, string continueLabel = null, SpeakerFoot speaker = null)
        {
            if (previousShares == null && previousVotes != null && partyNames != null && previousVotes.Length == partyNames.Length)
            {
                long sum = 0;
                foreach (long v in previousVotes) { sum += v; }
                if (sum > 0)
                {
                    previousShares = new double[previousVotes.Length];
                    for (int p = 0; p < previousVotes.Length; p++) { previousShares[p] = previousVotes[p] / (double)sum; }
                }
            }
            var previous = new Previous
            {
                Label = previousLabel,
                ByConstituency = previousByConstituency != null && state != null && previousByConstituency.Length == state.TotalConstituencies ? previousByConstituency : null,
                Shares = previousShares != null && partyNames != null && previousShares.Length == partyNames.Length ? previousShares : null,
                Seats = previousSeats != null && partyNames != null && previousSeats.Length == partyNames.Length ? previousSeats : null,
            };

            if (state == null || partyNames == null)
            {
                Debug.LogWarning("CANVAS: election-night furniture missing - the board is dropped, the night stays silent.");
                return null;
            }
            ElectionNightScreen screen = Shell(out Transform content, out RectTransform contentRect);
            if (screen == null) { return null; }

            BuildMasthead(content, state, countryName, pollsClosed, totalSeats, screen);   // D-NIGHT: the † cell at its right
            ValkretsCartogramView map = BuildBody(screen, content, state, partyNames, totalSeats, previous, ledger, ledgerParty,
                standingBudget, standingBudgetCitation, government, inkCountry, reference, out LaenderTileView laender);
            BuildFooter(content, verdict, screen, continueLabel, speaker, screen.Root.transform);

            // The map lays itself in the rect the page gives it; resolve the page now so the first frame already has it,
            // not two frames on (the signing screen's lesson: a capture can photograph an unlaid rect).
            // ⚠ On the CONTENT column, not the document: the document carries no layout controller, and uGUI does not descend
            // past a rect without one - a rebuild asked of the document lays nothing (measured: every band read its default 100).
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            if (map != null) { map.Rebuild(); }
            if (laender != null) { laender.Rebuild(); }
            screen.Map = map;
            screen.Laender = laender;

            // S-20: the board stamps its own capture-identity token, so a film that shows the desk over it
            // fails on the pixels rather than passing on a clean exit code.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }

        /// <summary>The night's paper on the canvas - the wash, the document with its ornate frame, and the content column every night lays out in
        /// (D-PL: shared with the Sejm's and the President's nights). Null, and said, where the furniture is missing.</summary>
        private static ElectionNightScreen Shell(out Transform contentTransform, out RectTransform contentRect)
        {
            contentTransform = null;
            contentRect = null;
            Sprite frame = CanvasChrome.Sliced("ui_frame_ornate", 64f, 64f, 64f, 64f);
            Texture2D scrimTexture = IconLibrary.GetChrome("ui_scrim_takeover");
            if (frame == null)
            {
                Debug.LogWarning("CANVAS: election-night furniture missing - the board is dropped, the night stays silent.");
                return null;
            }

            Canvas canvas = CanvasChrome.EnsureHost();
            var screen = new ElectionNightScreen();

            var root = new GameObject("ElectionNightScreen");
            screen.Root = root;
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.AddComponent<RectTransform>());

            var wash = new GameObject("Wash");
            wash.transform.SetParent(root.transform, false);
            Stretch(wash.AddComponent<RectTransform>());
            if (scrimTexture != null)
            {
                RawImage washImage = wash.AddComponent<RawImage>();
                washImage.texture = scrimTexture;
            }
            else
            {
                wash.AddComponent<Image>().color = PoliSimTheme.Hex(0x14110C);
            }

            // The document: §A.14's 1240 px envelope, padding 38/52/34.
            var document = new GameObject("Document");
            document.transform.SetParent(root.transform, false);
            RectTransform doc = document.AddComponent<RectTransform>();
            doc.anchorMin = new Vector2(0.03f, 0.04f);   // P2-4.3: full frame, not a square on black
            doc.anchorMax = new Vector2(0.97f, 0.96f);
            doc.pivot = new Vector2(0.5f, 0.5f);
            doc.sizeDelta = Vector2.zero;
            document.AddComponent<Image>().color = PoliSimTheme.Hex(0xF2EADB);   // V-N1: flat paper

            CanvasChrome.AsAuthoredImage(document.transform, "OrnateFrame", frame, sliced: true).type = Image.Type.Sliced;
            Image ornate = document.transform.Find("OrnateFrame").GetComponent<Image>();
            ornate.fillCenter = false;
            Stretch(ornate.rectTransform);

            var content = new GameObject("Content");
            content.transform.SetParent(document.transform, false);
            contentRect = content.AddComponent<RectTransform>();
            Stretch(contentRect);
            contentRect.offsetMin = new Vector2(52f, 34f);
            contentRect.offsetMax = new Vector2(-52f, -38f);
            var column = content.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.spacing = 10f;
            contentTransform = content.transform;
            return screen;
        }
        private const float DocumentWidth = 1240f;

        /// <summary>§707: the Länder map on a German night (null on Sweden's) - read by the film's log line.</summary>
        public LaenderTileView Laender { get; private set; }


        /// <summary>§A.14: institution + title left, timestamp and the declared chip right.</summary>
        private static void BuildMasthead(Transform parent, NightState state, string countryName,
            DateTime pollsClosed, int totalSeats, ElectionNightScreen dagger = null)
        {
            Masthead(parent, countryName, pollsClosed.AddMinutes(state.Minute).ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  POLLS CLOSED " + pollsClosed.ToString("HH:mm", CultureInfo.InvariantCulture),
                string.Format(CultureInfo.InvariantCulture, countryName.Equals("GERMANY", StringComparison.OrdinalIgnoreCase) ? "{0} OF {1} LÄNDER DECLARED" : "{0} OF {1} CONSTITUENCIES DECLARED", state.DeclaredCount, state.TotalConstituencies), dagger);
        }

        /// <summary>The masthead with its two right-hand lines given (D-PL: the Sejm's night counts okregi, the President's night the nation). D-NIGHT
        /// ("For Code" 6): <paramref name="dagger"/> given, the † cell at the chip's right - the night-wide toggle of the column's full lines.</summary>
        private static void Masthead(Transform parent, string countryName, string timestamp, string chipLine, ElectionNightScreen dagger = null)
        {
            var mast = new GameObject("Masthead");
            mast.transform.SetParent(parent, false);
            mast.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
            var row = mast.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true;
            row.childForceExpandWidth = true;
            // ⚠ Measured 2026-09-10: uGUI's default force-expand-height makes a row report a FLEXIBLE height of 1, so the masthead
            // and the footer each took a third of the page's spare height (~200 units apiece at 1280) away from the body - and the
            // CONTINUE button stretched with its row. The spare height belongs to the body, where the map is.
            row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            mast.AddComponent<LayoutElement>().minHeight = 54f;

            var left = new GameObject("Left");
            left.transform.SetParent(mast.transform, false);
            left.AddComponent<RectTransform>();
            var leftColumn = left.AddComponent<VerticalLayoutGroup>();
            leftColumn.childControlHeight = true;
            leftColumn.childForceExpandHeight = false;
            leftColumn.childAlignment = TextAnchor.MiddleLeft;

            CanvasChrome.MakeTextRealWeight(left.transform, "Institution", "RETURNING OFFICER  ·  " + countryName.ToUpperInvariant(),
                PoliSimTheme.Display, 12, PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleLeft, FontStyle.Bold)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);
            CanvasChrome.MakeTextRealWeight(left.transform, "Title", "ELECTION NIGHT",
                PoliSimTheme.Display, 30, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 34f);

            var right = new GameObject("Right");
            right.transform.SetParent(mast.transform, false);
            right.AddComponent<RectTransform>();
            var rightColumn = right.AddComponent<VerticalLayoutGroup>();
            rightColumn.childControlHeight = true;
            rightColumn.childForceExpandHeight = false;
            rightColumn.childAlignment = TextAnchor.MiddleRight;

            CanvasChrome.MakeTextRealWeight(right.transform, "Timestamp", timestamp,
                PoliSimTheme.Document, 12, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

            // The chip §A.14 names: "N OF M SEATS DECLARED". It counts CONSTITUENCIES declared,
            // because seats are national in this system and are not declared one at a time - the
            // chip says what is actually true rather than what the mock-up's wording implied.
            Transform chipParent = right.transform;
            if (dagger != null)
            {
                Transform chipRow = CanvasRows.HRow(right.transform, "ChipRow", Px(20f), Px(6f));
                chipRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
                chipParent = chipRow;
            }
            var chip = new GameObject("DeclaredChip");
            chip.transform.SetParent(chipParent, false);
            chip.AddComponent<RectTransform>().sizeDelta = new Vector2(300f, 22f);
            chip.AddComponent<Image>().color = PoliSimTheme.Hex(0x5D564A);
            chip.AddComponent<LayoutElement>().minHeight = 22f;
            Text chipText = CanvasChrome.MakeTextRealWeight(chip.transform, "ChipText", chipLine,
                PoliSimTheme.Display, 11, PoliSimTheme.Hex(0xF4ECDC), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(chipText.GetComponent<RectTransform>());
            if (dagger != null)
            {
                chip.GetComponent<LayoutElement>().flexibleWidth = 1f;
                DaggerCell(chipParent, dagger);
            }
        }

        /// <summary>
        /// The body, three columns (2026-09-10, election night items 1-3): THE COUNT and the calls at the left, THE MAP at
        /// the centre - the screen's centre, which had never been drawn - and WHO GOVERNS at the right, above the campaign's
        /// ledger and the inherited budget's estimate. ⚠ The constituency list is gone: it printed a raw count per valkrets,
        /// which the map now says better (the winner in its ink, the margin, the change), so the list yields to it whole.
        /// </summary>
        private static ValkretsCartogramView BuildBody(ElectionNightScreen screen, Transform parent, NightState state, string[] partyNames, int totalSeats,
            Previous previous, VoteAttribution.Ledger ledger, string ledgerParty,
            IReadOnlyList<DivisionEffect> standingBudget, string standingBudgetCitation,
            GovernmentFormation.View government, CountryId inkCountry, Reference reference, out LaenderTileView laender)
        {
            laender = null;
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            body.AddComponent<RectTransform>();
            var grid = body.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = 28f;
            grid.childControlWidth = true;
            grid.childControlHeight = true;
            grid.childForceExpandHeight = true;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // The map is width-limited (board 4a's 1080 : 587.6), so the width is what makes it the page's centre, and the two text
            // columns hold what their longest line needs at 1280. PF-16 (§609, measured): at 1280 the who-governs column's wrapped
            // notes stood taller than the body (small-pixel text wraps sooner and leads taller than at 2560, on a canvas of the same
            // units), so the layout compressed every text in it below its own height. The column takes width from the map, which had
            // spare height under it (1.8 : 1.2 - at 1.6 : 1.4 the narrower tiles dropped their labels), and the effects plate and the
            // column's line gap yielded the rest: 19 clipped texts at 1280 to none. D-NIGHT (31a-31c) replaced that column whole: its blocks are planned to its 516 px rect (`PlanColumn`, ElectionNightScreen.Governs.cs).
            if (inkCountry == CountryId.Germany)
            {
                // §707 (D-DE, boards 24a-24b): Germany's night is Sweden's sheet as built with the Länder at its centre - the count with the 5 %
                // line, the single-Land and exempt lists tagged, the seats owed until 16 of 16; the Länder map; WHO GOVERNS the chamber alone
                // (the Bundestag elects the chancellor weeks later, §705) with WHY as Sweden's; no ESTIMATED IMPACT (no German budget act carries one).
                Transform countDe = Column(body.transform, "Count", 1.0f);
                BuildTallyGermany(countDe, state, partyNames, totalSeats, previous);
                BuildCalls(countDe, state, partyNames, "Land");   // GA-3-6: Germany counts by Land
                BuildReference(countDe, state, partyNames, reference, inkCountry);
                Transform centreDe = Column(body.transform, "MapColumn", 1.8f);
                laender = BuildLaenderMap(centreDe, state, partyNames, previous, inkCountry);
                Transform rightDe = Column(body.transform, "Governs", 1.2f);
                // D-NIGHT (31a-31c): the column in v3.5 - the first ballot projected (GATE-18 q20) as the round's first cell and the vote, WHY as Sweden's
                BuildGoverns(screen, rightDe, new GovernsNight { State = state, PartyNames = partyNames, Government = government, Country = inkCountry, TotalSeats = totalSeats,
                    Ledger = ledger, LedgerParty = ledgerParty, BudgetBlock = false, Overlay = screen.Root.transform });
                return null;
            }
            Transform count = Column(body.transform, "Count", 1.0f);
            BuildTally(count, state, partyNames, totalSeats, previous);
            BuildCalls(count, state, partyNames, "valkrets");
            BuildReference(count, state, partyNames, reference, inkCountry);   // PS-2 (§619): history beside the count, under it; board 21d's table (§685)

            Transform centre = Column(body.transform, "MapColumn", 1.8f);
            ValkretsCartogramView map = BuildMap(centre, state, partyNames, previous, inkCountry);

            Transform right = Column(body.transform, "Governs", 1.2f);
            // D-NIGHT (Design's 31a-31c): WHO GOVERNS in v3.5 in the built column's rect - the projection, the vote, you, why (P2-4.3, 13b) and the
            // budget (5c); the long wording in the slips and the † view
            BuildGoverns(screen, right, new GovernsNight { State = state, PartyNames = partyNames, Government = government, Country = inkCountry, TotalSeats = totalSeats,
                Ledger = ledger, LedgerParty = ledgerParty, BudgetBlock = true, StandingBudget = standingBudget, StandingBudgetCitation = standingBudgetCitation,
                Overlay = screen.Root.transform });
            return map;
        }

        private static Transform Column(Transform parent, string name, float weight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            // ⚠ The WEIGHT decides the width, not the longest line: with preferred widths left to the children, a long wrapped
            // note in one column (the ledger, the scope line) out-bid the others and the played night's tally wrapped "S / D"
            // (the 2026-09-10 film). Preferred and min pinned to zero hand every unit to the flexible weights.
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = weight;
            element.preferredWidth = 0f;
            element.minWidth = 0f;
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 2f;
            return go.transform;
        }

        /// <summary>
        /// §707 (24a ⑤, 24b B): THE GERMAN COUNT - one row per list in vote order, the 5 % as a rule across the rows between the last list above
        /// it and the first below, a single-Land list tagged (IN BY), the national minority's below the rule, tagged EXEMPT (§ 4 Abs. 2
        /// Satz 3 BWahlG); the seats OWED until 16 of 16 (a partial count seats the wrong lists - 24b measured the BSW at 5.72 % and 39 seats at
        /// 11 of 16, the CSU at 0.00 % until Bayern declares); the swing like for like against the same Länder in 2021, a list with no 2021 base
        /// ABSENT (19a); the Erststimme's gap row where it would change a result (24a ⑦).
        /// </summary>
        private static void BuildTallyGermany(Transform parent, NightState state, string[] partyNames, int totalSeats, Previous previous)
        {
            Heading(parent, state.Complete
                ? "THE COUNT — COMPLETE"
                : string.Format(CultureInfo.InvariantCulture, "THE COUNT SO FAR — {0} OF {1} LÄNDER · SEATS OWED UNTIL {1} OF {1}", state.DeclaredCount, state.TotalConstituencies));
            if (state.CountedValid <= 0)
            {
                Row(parent, "NOTHING HAS DECLARED", "—", 13, PoliSimTheme.TextMuted);
                return;
            }
            var order = new List<int>();
            for (int p = 0; p < partyNames.Length; p++) { order.Add(p); }
            order.Sort((a, b) => state.CountedVotes[b].CompareTo(state.CountedVotes[a]));
            double[] swing = null;
            bool[] hasBase = null;
            var counted = new bool[partyNames.Length];
            if (previous.ByConstituency != null)
            {
                var now = new double[partyNames.Length];
                var before = new double[partyNames.Length];
                double nowTotal = 0.0, beforeTotal = 0.0;
                for (int r = 0; r < state.TotalConstituencies; r++)
                {
                    if (!state.Constituencies[r].Declared) { continue; }
                    for (int p = 0; p < partyNames.Length; p++) { now[p] += state.Constituencies[r].Votes[p]; before[p] += previous.ByConstituency[r][p]; }
                }
                foreach (double v in now) { nowTotal += v; }
                foreach (double v in before) { beforeTotal += v; }
                if (nowTotal > 0.0 && beforeTotal > 0.0)
                {
                    swing = new double[partyNames.Length];
                    hasBase = new bool[partyNames.Length];
                    for (int p = 0; p < partyNames.Length; p++) { swing[p] = (now[p] / nowTotal - before[p] / beforeTotal) * 100.0; hasBase[p] = before[p] > 0.0; counted[p] = now[p] > 0.0; }
                }
            }
            bool ruled = false;
            int seatsShown = 0;
            foreach (int p in order)
            {
                double share = state.CountedShare(p);
                bool exempt = NationalElection.ExemptFromThreshold(CountryId.Germany, partyNames[p]);
                if (!ruled && share < 0.05)   // §707's real film (24a ⑤): the rule above every list under it - the SSW below it, seated as exempt
                {
                    Row(parent, "5 %", "the line - a list below it is seated only if exempt", 10, PoliSimTheme.TextMuted);
                    ruled = true;
                }
                // 24a's "IN SH · EXEMPT" wrapped beside the SSW's figures in the 1280 column; the rule above it says what the exemption is
                string tag = partyNames[p] == "CSU" ? " IN BY" : exempt ? " · EXEMPT" : string.Empty;
                string seats = state.Complete ? UiFormat.Seats(state.SeatsOnCounted[p]) : "owed";   // the heading says "SEATS OWED UNTIL 16 OF 16"; the row's word is the short one, so the SSW's row holds one line
                if (state.Complete) { seatsShown += state.SeatsOnCounted[p]; }
                string sw = swing == null ? string.Empty : hasBase[p] ? string.Format(CultureInfo.InvariantCulture, "  {0:+0.00;−0.00;0.00} pp", swing[p]) : counted[p] ? "  ABSENT" : "  —";   // ABSENT is a list with no 2021 base; a list with no vote yet where it stands (the CSU before Bayern) is not yet
                Row(parent, PartySystems.ShortName(CountryId.Germany, partyNames[p]).ToUpperInvariant() + tag, string.Format(CultureInfo.InvariantCulture, "{0:N0}  {1:P2}  {2}{3}", state.CountedVotes[p], share, seats, sw), 12, PoliSimTheme.TextPrimary, nameWidth: 34f);
            }
            if (!ruled) { Row(parent, "5 %", "every list counted is above the line", 10, PoliSimTheme.TextMuted); }
            Row(parent, "ERSTSTIMME · THREE-CONSTITUENCY RULE", "NOT YET MODELLED", 10, PoliSimTheme.TextMuted);
            Row(parent, "COUNTED", state.Complete
                ? string.Format(CultureInfo.InvariantCulture, "{0:N0} votes    {1} of {2} seats", state.CountedValid, seatsShown, totalSeats)
                : string.Format(CultureInfo.InvariantCulture, "{0:N0} votes    seats owed until {1} of {1}", state.CountedValid, state.TotalConstituencies), 12, PoliSimTheme.TextSecondary, bold: true);
            Wrapped(parent, swing != null
                ? "SWING (the last column), against " + (previous.Label ?? "GERMANY 2021") + (state.Complete ? ", every Land" : string.Format(CultureInfo.InvariantCulture, ", the same {0} Länder", state.DeclaredCount)) + " · ABSENT: no list in 2021 to compare"
                : "SWING - no previous election is on hand to compare against", 11, PoliSimTheme.TextSecondary);
        }

        /// <summary>§707 (24a ②③, 24b A): the Länder map - each declared Land in its leader's ink, the lead in points and VS 2021 (the leader's
        /// own change there, like for like).</summary>
        private static LaenderTileView BuildLaenderMap(Transform parent, NightState state, string[] partyNames, Previous previous, CountryId inkCountry)
        {
            if (state == null || state.TotalConstituencies != LaenderTileView.Table.Length)
            {
                Heading(parent, "THE MAP - NONE FOR THIS COUNT");
                Row(parent, "the map draws the sixteen Länder; this count has " + (state?.TotalConstituencies ?? 0), "—", 11, PoliSimTheme.TextMuted);
                return null;
            }
            Heading(parent, "THE LÄNDER · TILE AREA = REGISTERED ELECTORATE · EACH IN ITS LEADER'S INK");
            var results = new LaenderTileView.TileResult[state.TotalConstituencies];
            for (int r = 0; r < results.Length; r++)
            {
                ConstituencyReport c = state.Constituencies[r];
                if (!c.Declared || c.Votes == null) { continue; }
                int first = -1, second = -1;
                long total = 0;
                for (int p = 0; p < c.Votes.Length; p++)
                {
                    total += c.Votes[p];
                    if (first < 0 || c.Votes[p] > c.Votes[first]) { second = first; first = p; }
                    else if (second < 0 || c.Votes[p] > c.Votes[second]) { second = p; }
                }
                if (first < 0 || total <= 0) { continue; }
                double lead = (c.Votes[first] - (second >= 0 ? c.Votes[second] : 0)) * 100.0 / total;
                bool hasSwing = false;
                double swing = 0.0;
                if (previous.ByConstituency != null && previous.ByConstituency.Length == results.Length)
                {
                    long beforeTotal = 0;
                    foreach (long v in previous.ByConstituency[r]) { beforeTotal += v; }
                    if (beforeTotal > 0 && previous.ByConstituency[r][first] > 0)
                    {
                        swing = (c.Votes[first] / (double)total - previous.ByConstituency[r][first] / (double)beforeTotal) * 100.0;
                        hasSwing = true;
                    }
                }
                string key = partyNames[first];
                results[r] = new LaenderTileView.TileResult
                {
                    Declared = true,
                    Leader = PartySystems.ShortName(inkCountry, key).ToUpperInvariant(),
                    Ink = PoliSimTheme.PartyLaddered(inkCountry, key),
                    Mark = MarkOf(inkCountry, key),
                    LeadPp = lead,
                    HasSwing = hasSwing,
                    SwingPp = swing,
                };
            }
            LaenderTileView view = LaenderTileView.Create(parent, results);
            Wrapped(parent, "FOUR BANDS NORTH TO SOUTH · A TILE'S FIGURE IS THE LEADER'S LEAD IN POINTS · VS 2021 IS THE LEADER'S CHANGE THERE · DASHED = NOT DECLARED", 10, PoliSimTheme.TextMuted);
            return view;
        }

        private static Texture2D MarkOf(CountryId country, string key)
        {
            foreach (PoliticalParty party in PartySystems.For(country)) { if (string.Equals(party.Abbrev, key, StringComparison.Ordinal)) { return IconLibrary.GetPartyMark(party.MarkName); } }
            return null;
        }

        /// <summary>
        /// Item 2: board 4a's cartogram - 29 tiles, area = fixed mandates, eleven bands - each declared valkrets in its
        /// winner's ink. The mandates are OUR column (`SeatConversion.FixedSeatsPerRegion` over each valkrets' eligible
        /// electorate, 310 in all), computed here from the night's own constituencies rather than passed in, so the map
        /// cannot be handed a column that disagrees with the count it draws.
        /// </summary>
        private static ValkretsCartogramView BuildMap(Transform parent, NightState state, string[] partyNames, Previous previous, CountryId inkCountry)
        {
            if (state == null || state.TotalConstituencies != ValkretsCartogram.Labels.Length)
            {
                Heading(parent, "THE MAP - NONE FOR THIS COUNT");
                Row(parent, "the map draws Sweden's 29 valkretsar; this count has " + (state?.TotalConstituencies ?? 0), "—", 11, PoliSimTheme.TextMuted);
                return null;
            }

            var eligible = new double[state.TotalConstituencies];
            for (int r = 0; r < eligible.Length; r++) { eligible[r] = state.Constituencies[r].Eligible; }
            int[] mandates = SeatConversion.FixedSeatsPerRegion(eligible);

            IReadOnlyList<PoliticalParty> seeded = PartySystems.For(inkCountry);
            var results = new ValkretsCartogramView.TileResult[state.TotalConstituencies];
            for (int r = 0; r < results.Length; r++)
            {
                ConstituencyReport c = state.Constituencies[r];
                if (!c.Declared || c.Votes == null) { continue; }
                int first = -1, second = -1;
                long total = 0;
                for (int p = 0; p < c.Votes.Length; p++)
                {
                    total += c.Votes[p];
                    if (first < 0 || c.Votes[p] > c.Votes[first]) { second = first; first = p; }
                    else if (second < 0 || c.Votes[p] > c.Votes[second]) { second = p; }
                }
                if (first < 0 || total <= 0) { continue; }

                string winner = partyNames[first];
                Texture2D mark = null;
                foreach (PoliticalParty party in seeded)
                {
                    if (string.Equals(party.Abbrev, winner, StringComparison.Ordinal)) { mark = IconLibrary.GetPartyMark(party.MarkName); break; }
                }

                var tile = new ValkretsCartogramView.TileResult
                {
                    Declared = true,
                    Winner = winner,
                    Ink = PoliSimTheme.PartyLaddered(inkCountry, winner),
                    Mark = mark,
                    MarginPp = second < 0 ? 100.0 : (c.Votes[first] - c.Votes[second]) * 100.0 / total,
                };
                if (previous.ByConstituency != null)
                {
                    long before = 0;
                    foreach (long v in previous.ByConstituency[r]) { before += v; }
                    if (before > 0)
                    {
                        tile.HasSwing = true;
                        tile.SwingPp = (c.Votes[first] / (double)total - previous.ByConstituency[r][first] / (double)before) * 100.0;
                    }
                }
                results[r] = tile;
            }

            Heading(parent, string.Format(CultureInfo.InvariantCulture,
                "THE VALKRETSAR - TILE AREA = FIXED SEATS, {0} OF THE CHAMBER'S 349 - EACH IN ITS WINNER'S INK", Sum(mandates)));
            ValkretsCartogramView view = ValkretsCartogramView.Create(parent, mandates, results);
            Text key = CanvasChrome.MakeTextRealWeight(parent, "MapKey",
                "ELEVEN BANDS NORTH TO SOUTH, WEST TO EAST INSIDE EACH · THE FIGURE AT A BAND'S LEFT IS THE SEATS IT RETURNS · "
                + "A TILE'S FIGURE IS THE WINNER'S LEAD IN POINTS" + (previous.ByConstituency != null ? " · 'VS LAST' IS THE WINNER'S CHANGE THERE" : string.Empty)
                + " · — HAS NOT DECLARED",
                PoliSimTheme.Document, 10, PoliSimTheme.TextSecondary, TextAnchor.UpperLeft);
            key.horizontalOverflow = HorizontalWrapMode.Wrap;
            key.gameObject.AddComponent<LayoutElement>().minHeight = 28f;
            return view;
        }

        private static int Sum(int[] values)
        {
            int s = 0;
            foreach (int v in values) { s += v; }
            return s;
        }

        private static void Wrapped(Transform parent, string text, int size, Color ink)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, "Note", text, PoliSimTheme.Document, size, ink, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.gameObject.AddComponent<LayoutElement>().minHeight = size + 6f;
        }

        private static void Heading(Transform parent, string text)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, "Heading", text, PoliSimTheme.Display, 11,
                PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleLeft, FontStyle.Bold);
            t.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 20f;
        }

        /// <summary>One row: name at the left, figures at the right. Absence is an em dash, never a zero.</summary>
        private static void Row(Transform parent, string name, string figure, int size, Color ink, bool bold = false, float nameWidth = -1f)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;   // a row keeps its own height; the page's spare height is the map's (2026-09-10, measured)
            row.AddComponent<LayoutElement>().minHeight = 20f;

            Text nameText = CanvasChrome.MakeTextRealWeight(row.transform, "Name", name, PoliSimTheme.Document, size, ink,
                TextAnchor.MiddleLeft, bold ? FontStyle.Bold : FontStyle.Normal);
            Text figureText = CanvasChrome.MakeTextRealWeight(row.transform, "Figure", figure, PoliSimTheme.Document, size, ink,
                TextAnchor.MiddleRight, bold ? FontStyle.Bold : FontStyle.Normal);
            if (nameWidth > 0f)
            {
                // A short key (a party's abbreviation) at a fixed width, the figure taking the rest - so a long figure cannot squeeze the key.
                LayoutElement key = nameText.gameObject.AddComponent<LayoutElement>();
                key.minWidth = nameWidth; key.preferredWidth = nameWidth; key.flexibleWidth = 0f;
                LayoutElement rest = figureText.gameObject.AddComponent<LayoutElement>();
                rest.flexibleWidth = 1f;
            }
        }

        /// <summary>
        /// The national tally OF WHAT HAS DECLARED. Two things this panel must say and does:
        /// the share is of the counted vote, not of the electorate; and the seats are a PROJECTION
        /// until the last constituency is in, and the heading says so in those words rather than
        /// letting a reader assume a final number.
        /// </summary>
        private static void BuildTally(Transform parent, NightState state, string[] partyNames, int totalSeats, Previous previous)
        {
            Heading(parent, state.Complete
                ? "THE COUNT — COMPLETE"
                : string.Format(CultureInfo.InvariantCulture, "THE COUNT SO FAR — {0} OF {1} CONSTITUENCIES, SEATS PROJECTED ON WHAT IS IN",
                    state.DeclaredCount, state.TotalConstituencies));

            if (state.CountedValid <= 0)
            {
                Row(parent, "NOTHING HAS DECLARED", "—", 13, PoliSimTheme.TextMuted);
                return;
            }

            var order = new List<int>();
            for (int p = 0; p < partyNames.Length; p++) { order.Add(p); }
            order.Sort((a, b) => state.CountedVotes[b].CompareTo(state.CountedVotes[a]));

            // V-N3, LANDED (2026-09-10, election night item 3). A swing is a comparison and is only honest like for like.
            //
            // With the previous election's count PER CONSTITUENCY the comparison is restricted to what has actually declared -
            // the declared valkretsar now against the SAME valkretsar last time - so the swing is shown at every instant of the
            // night, early returns included, and cannot be an artefact of which places declared first. That per-constituency
            // count was V-N3's whole blocker, and a first-term night always had it: the seed (2026 since K-1) IS the previous election, and
            // the model reproduces it seat for seat. ⚠ Both sides are shares of the votes cast for the parties LISTED, so a
            // small party the count does not carry cannot move every swing by its own absence.
            //
            // With only a NATIONAL previous result (a later term - the record keeps no per-constituency count) the old discipline
            // stands: the swing waits for the complete count, and the screen says so.
            double[] swing = null;
            string swingBasis = null;
            if (previous.ByConstituency != null)
            {
                var now = new double[partyNames.Length];
                var before = new double[partyNames.Length];
                double nowTotal = 0.0, beforeTotal = 0.0;
                for (int r = 0; r < state.TotalConstituencies; r++)
                {
                    if (!state.Constituencies[r].Declared) { continue; }
                    for (int p = 0; p < partyNames.Length; p++)
                    {
                        now[p] += state.Constituencies[r].Votes[p];
                        before[p] += previous.ByConstituency[r][p];
                    }
                }
                foreach (double v in now) { nowTotal += v; }
                foreach (double v in before) { beforeTotal += v; }
                if (nowTotal > 0.0 && beforeTotal > 0.0)
                {
                    swing = new double[partyNames.Length];
                    for (int p = 0; p < partyNames.Length; p++) { swing[p] = (now[p] / nowTotal - before[p] / beforeTotal) * 100.0; }
                    swingBasis = state.Complete
                        ? "against " + (previous.Label ?? "the previous election") + ", every valkrets"
                        : string.Format(CultureInfo.InvariantCulture, "against the same {0} valkretsar in {1}", state.DeclaredCount, previous.Label ?? "the previous election");
                }
            }
            else if (previous.Shares != null && state.Complete)
            {
                swing = new double[partyNames.Length];
                for (int p = 0; p < partyNames.Length; p++) { swing[p] = (state.CountedShare(p) - previous.Shares[p]) * 100.0; }
                swingBasis = "against " + (previous.Label ?? "the previous election");
            }

            bool seatChange = state.Complete && previous.Seats != null;

            int seatsShown = 0;
            foreach (int p in order)
            {
                seatsShown += state.SeatsOnCounted[p];
                string figure = string.Format(CultureInfo.InvariantCulture, "{0:N0}  {1:P2}  {2}",
                    state.CountedVotes[p], state.CountedShare(p), UiFormat.Seats(state.SeatsOnCounted[p]));
                if (seatChange)
                {
                    figure += string.Format(CultureInfo.InvariantCulture, " ({0:+0;-0;±0})", state.SeatsOnCounted[p] - previous.Seats[p]);
                }
                if (swing != null)
                {
                    figure += string.Format(CultureInfo.InvariantCulture, "  {0:+0.00;-0.00;0.00} pp", swing[p]);
                }

                Row(parent, partyNames[p], figure, 12, PoliSimTheme.TextPrimary, nameWidth: 34f);
            }

            Row(parent, "COUNTED", string.Format(CultureInfo.InvariantCulture, "{0:N0} votes    {1} of {2} seats",
                state.CountedValid, seatsShown, totalSeats), 12, PoliSimTheme.TextSecondary, bold: true);

            if (swing != null)
            {
                Wrapped(parent, "SWING (the last column), " + swingBasis + (seatChange ? " · seats in brackets against its chamber" : string.Empty), 11, PoliSimTheme.TextSecondary);
            }
            else if (previous.Shares != null)
            {
                Wrapped(parent, "SWING held back until every constituency is in - only a national previous result is on hand, and a partial count compares different places",
                    11, PoliSimTheme.TextMuted);
            }
            else
            {
                Wrapped(parent, "SWING - no previous election is on hand to compare against", 11, PoliSimTheme.TextMuted);
            }
        }

        /// <summary>
        /// The calls. Each one is a claim the model has proved cannot be overturned by anything
        /// still outstanding, so the panel states it flatly; where nothing is safe yet it says that
        /// too, rather than showing a projection dressed as a call.
        /// </summary>
        /// <summary>
        /// Item 1 (2026-09-10): a call is news when it LANDS. While the night runs, each call prints with the constituency count
        /// at which it became safe (the model stamps it - `ElectionNight.StampLandings`), in the order they landed. Once the count
        /// is complete the calls are history and collapse to a summary: the parties that hold seats as ONE line with the count
        /// each was called at, the parties shut out likewise, and the largest party and any bloc call on their own lines - so
        /// nine identical "will hold seats" rows at 29 of 29 become one statement that says when each was known.
        /// </summary>
        /// <summary>
        /// PS-2 (§619), the spec's §7 - "after the player's first election night, the game shows what actually happened beside the
        /// player's": the real result's seats per party with the played count beside each, and the government the record shows after
        /// it. Absent when the record holds no election on this polling day (a later term), and absent until the count is complete -
        /// history is compared against a final count, never a projection.
        /// <para><b>Board 21d (§685): the table.</b> The paragraph (7 px on the film, below the floor) becomes the count's own eight rows in the
        /// count's order with the real seats beside the played: the mark, the party, PLAYED, REAL at the reduced presence (10a: the reference,
        /// not the game) and DIFF signed with + and − and no arrowheads; under it the government of record as a mark, words and a date stamp,
        /// and the caretaker state phrase with the record's date. The sentences are the slips.</para>
        /// </summary>
        private static void BuildReference(Transform parent, NightState state, string[] partyNames, Reference reference, CountryId country)
        {
            if (reference == null || state == null || !state.Complete || reference.Seats == null || reference.Seats.Length != partyNames.Length) { return; }
            string label = (reference.Label ?? "AS IT HAPPENED").ToLowerInvariant();
            label = char.ToUpperInvariant(label[0]) + label.Substring(1);
            string countryWord = country.ToString();
            if (label.StartsWith(countryWord.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)) { label = countryWord + label.Substring(countryWord.Length); }

            Transform head = CanvasRows.HRow(parent, "ReferenceHead", 24f, 8f);
            CanvasChrome.MakeTextRealWeight(head, "Label", label, PoliSimTheme.Body, 13, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft).horizontalOverflow = HorizontalWrapMode.Overflow;
            Texture2D dated = SymbolRegistry.TextureOf(Symbol.Dated);
            if (dated != null)
            {
                var glyph = new GameObject("Dated");
                glyph.transform.SetParent(head, false);
                glyph.AddComponent<RectTransform>();
                RawImage image = glyph.AddComponent<RawImage>();
                image.texture = dated;
                image.color = PoliSimTheme.TextMuted;
                LayoutElement side = glyph.AddComponent<LayoutElement>();
                side.minWidth = side.preferredWidth = side.minHeight = side.preferredHeight = 14f;
            }
            else { CanvasRows.Caption(head, SymbolRegistry.Word(Symbol.Dated), 10, PoliSimTheme.TextMuted); }
            CanvasRows.Slip(head.gameObject, null, "DATED", new[] { "THE REAL RESULT, AS THE RECORD HOLDS IT", "THE RECORD AS OF " + DeskDay(reference.RecordDate) });
            if (!string.IsNullOrEmpty(reference.HistoryNote))
            {
                // §767 (Elias's ruling D1): the played vote's history is carried by lineage - the flag at rest, the note on its slip
                Transform confidence = CanvasRows.HRow(parent, "ReferenceConfidence", 16f, 8f);
                CanvasRows.Caption(confidence, "LOW CONFIDENCE · THE PLAYED VOTE'S PAST BY LINEAGE", 10, PoliSimTheme.TextMuted);
                CanvasRows.Slip(confidence.gameObject, null, "LOW CONFIDENCE", SlipLines(reference.HistoryNote));
            }

            const float markW = 16f, figW = 50f, pitch = 18f;
            // §707's real film: Sweden's letters fit 30 px, Germany's names do not ("GRÜNE" wrapped to two lines in an 18 px row) - the column is
            // its widest name as the face sets it, 30 px at the least; the names upper case, as the count's rows print them
            float nameW = 30f;
            foreach (string abbrev in partyNames)
            {
                Text probe = CanvasChrome.MakeTextRealWeight(null, "Probe", PartySystems.ShortName(country, abbrev).ToUpperInvariant(), PoliSimTheme.Body, 13, Color.white, TextAnchor.MiddleLeft);
                nameW = Mathf.Max(nameW, Mathf.Ceil(probe.preferredWidth + 2f));
                UnityEngine.Object.Destroy(probe.gameObject);
            }
            Transform keys = CanvasRows.HRow(parent, "ReferenceKeys", 16f, 8f);
            CanvasRows.FixedCell(keys, string.Empty, markW + 8f + nameW, 10, PoliSimTheme.TextMuted);
            CanvasRows.FixedCell(keys, "PLAYED", figW, 10, PoliSimTheme.TextMuted);
            CanvasRows.FixedCell(keys, "REAL", figW, 10, PoliSimTheme.TextMuted);
            CanvasRows.FixedCell(keys, "DIFF", figW, 10, PoliSimTheme.TextMuted);

            var order = new List<int>();
            for (int p = 0; p < partyNames.Length; p++) { order.Add(p); }
            order.Sort((a, b) => state.CountedVotes[b].CompareTo(state.CountedVotes[a]));   // the count's own order
            foreach (int p in order)
            {
                int played = state.SeatsOnCounted[p], real = reference.Seats[p], diff = played - real;
                Transform row = CanvasRows.HRow(parent, "Reference " + partyNames[p], pitch, 8f);
                CanvasRows.Mark(row, country, partyNames[p], markW);
                CanvasRows.FixedCell(row, PartySystems.ShortName(country, partyNames[p]).ToUpperInvariant(), nameW, 13, PoliSimTheme.TextPrimary, font: PoliSimTheme.Body);
                CanvasRows.FixedCell(row, played.ToString(CultureInfo.InvariantCulture), figW, 13, PoliSimTheme.TextPrimary);
                CanvasRows.FixedCell(row, real.ToString(CultureInfo.InvariantCulture), figW, 12, PoliSimTheme.TextMuted);
                CanvasRows.FixedCell(row, diff > 0 ? "+" + diff.ToString(CultureInfo.InvariantCulture) : diff < 0 ? "−" + (-diff).ToString(CultureInfo.InvariantCulture) : "0", figW, 12, PoliSimTheme.TextPrimary);
                CanvasRows.Slip(row.gameObject, null, PartySystems.ShortName(country, partyNames[p]),
                    new[] { string.Format(CultureInfo.InvariantCulture, "PLAYED {0} · THE REAL RESULT {1}", UiFormat.Seats(played).ToUpperInvariant(), real), "THE DIFFERENCE IS PLAYED LESS REAL" });
            }

            if (!string.IsNullOrEmpty(reference.HeadParty))
            {
                Transform gov = CanvasRows.HRow(parent, "ReferenceGovernment", 30f, 8f);
                CanvasRows.Mark(gov, country, reference.HeadParty, markW);
                CanvasRows.Caption(gov, (reference.HeadSurname ?? string.Empty).ToUpperInvariant() + (reference.HeadIsCaretaker ? " · CARETAKER" : string.IsNullOrEmpty(reference.HeadQualifier) ? string.Empty : " · " + reference.HeadQualifier.ToUpperInvariant()), 11, PoliSimTheme.TextPrimary);
                CanvasRows.Stamp(gov, DeskDay(reference.HeadFrom), 11, PoliSimTheme.TextPrimary);
                CanvasRows.Slip(gov.gameObject, null, "THE GOVERNMENT OF RECORD", SlipLines(reference.GovernmentLine));
                if (reference.HeadIsCaretaker)
                {
                    Transform chosen = CanvasRows.HRow(parent, "ReferenceChamber", 24f, 8f);
                    CanvasRows.Caption(chosen, "CHAMBER HAD NOT CHOSEN", 11, PoliSimTheme.TextMuted);
                    CanvasRows.Stamp(chosen, DeskDay(reference.RecordDate), 11, PoliSimTheme.TextPrimary);
                    CanvasRows.Slip(chosen.gameObject, null, "CHAMBER HAD NOT CHOSEN", new[] { "THE CHAMBER HAD NOT CHOSEN A HEAD OF GOVERNMENT", "BY THE RECORD'S DATE, " + DeskDay(reference.RecordDate) });
                }
            }
            else if (!string.IsNullOrEmpty(reference.GovernmentLine)) { Wrapped(parent, reference.GovernmentLine, 11, PoliSimTheme.TextSecondary); }
        }

        private static string DeskDay(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        /// <summary>A sentence cut into slip lines of at most 56 characters at word boundaries, upper-cased.</summary>
        private static List<string> SlipLines(string sentence)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(sentence)) { return lines; }
            var line = new System.Text.StringBuilder();
            foreach (string word in sentence.ToUpperInvariant().Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > 56) { lines.Add(line.ToString()); line.Clear(); }
                if (line.Length > 0) { line.Append(' '); }
                line.Append(word);
            }
            if (line.Length > 0) { lines.Add(line.ToString()); }
            return lines;
        }

        private static void BuildCalls(Transform parent, NightState state, string[] partyNames, string unit)
        {
            Heading(parent, state.Complete ? "THE CALLS, AS THEY LANDED" : "CALLS — SAFE WHATEVER IS STILL OUT");
            if (state.Calls.Count == 0)
            {
                Row(parent, "NOTHING CAN BE CALLED YET", "—", 12, PoliSimTheme.TextMuted);
                return;
            }

            if (!state.Complete)
            {
                foreach (ElectionCall call in state.Calls)
                {
                    Row(parent, CallText(call, partyNames),
                        string.Format(CultureInfo.InvariantCulture, "at {0} of {1}", call.DeclaredAt, call.OfTotal),
                        12, PoliSimTheme.TextPrimary);
                }
                return;
            }

            SummariseGroup(parent, state, partyNames, CallKind.ThresholdCleared, "HOLD SEATS", unit);
            SummariseGroup(parent, state, partyNames, CallKind.ThresholdMissed, "SHUT OUT BY THE THRESHOLD", unit);
            foreach (ElectionCall call in state.Calls)
            {
                if (call.Kind == CallKind.ThresholdCleared || call.Kind == CallKind.ThresholdMissed) { continue; }
                Row(parent, CallText(call, partyNames).ToUpperInvariant(),
                    string.Format(CultureInfo.InvariantCulture, "called at {0} of {1}", call.DeclaredAt, call.OfTotal),
                    12, PoliSimTheme.TextPrimary, bold: true);
            }
        }

        private static void SummariseGroup(Transform parent, NightState state, string[] partyNames, CallKind kind, string verb, string unit)
        {
            var members = new List<ElectionCall>();
            foreach (ElectionCall call in state.Calls) { if (call.Kind == kind) { members.Add(call); } }
            if (members.Count == 0) { return; }

            int first = int.MaxValue, last = 0;
            var landed = new List<string>(members.Count);
            foreach (ElectionCall call in members)
            {
                first = Math.Min(first, call.DeclaredAt);
                last = Math.Max(last, call.DeclaredAt);
                landed.Add(partyNames[call.Party] + " " + call.DeclaredAt.ToString(CultureInfo.InvariantCulture));
            }

            string head = members.Count == 1
                ? partyNames[members[0].Party] + " " + (kind == CallKind.ThresholdCleared ? "HOLDS SEATS" : "IS SHUT OUT BY THE THRESHOLD")
                : string.Format(CultureInfo.InvariantCulture, "{0} PARTIES {1}", members.Count, verb);
            Row(parent, head,
                first == last
                    ? string.Format(CultureInfo.InvariantCulture, "called at {0} of {1}", first, state.TotalConstituencies)
                    : string.Format(CultureInfo.InvariantCulture, "called from {0} to {1} of {2}", first, last, state.TotalConstituencies),
                12, PoliSimTheme.TextPrimary, bold: true);
            if (members.Count > 1)
            {
                Wrapped(parent, "each at the " + unit + " count it was called:  " + string.Join(" · ", landed), 11, PoliSimTheme.TextSecondary);
            }
        }

        private static string CallText(ElectionCall call, string[] partyNames)
        {
            switch (call.Kind)
            {
                case CallKind.ThresholdCleared: return partyNames[call.Party] + " will hold seats";
                case CallKind.ThresholdMissed: return partyNames[call.Party] + " cannot reach the threshold";
                case CallKind.LargestParty: return partyNames[call.Party] + " is the largest party";
                case CallKind.BlocMajority: return call.Bloc + " has a majority";
                default: return call.Bloc + " is short of a majority";
            }
        }

        /// <summary>P2-0.2: the board's foot - the office verdict (when a player is on the ballot) and CONTINUE, the
        /// takeover's one exit. The verdict is the same sentence the desk prints when a game ends on it.</summary>
        /// <summary>
        /// Premise 9 (§5.3, §656): the board ends with a line naming who the Speaker asks first - one sentence, the game's and the film's. Where the
        /// player's party is asked, the board's control is the way to the formation sheet (<see cref="ContinueToSheet"/>): leaving the board lets the
        /// round open the next day, and the sheet opens on the Speaker's request (premise 1).
        /// </summary>
        public static string SpeakerLine(bool playerAskedFirst, string firstShortName, bool bundestag = false) => bundestag
            ? (playerAskedFirst
                ? "Your party forms the government its candidate would lead, and the Bundespräsident proposes your candidate first - continue, and the formation sheet opens. The Bundestag elects the chancellor once it convenes (Art. 63 GG); the outgoing government serves on until then."
                : $"{firstShortName ?? "No party"} forms the government its candidate would lead, and the Bundespräsident proposes that candidate first. The Bundestag elects the chancellor once it convenes (Art. 63 GG); the outgoing government serves on until then.")
            : playerAskedFirst
            ? "The Speaker will ask your party first to form a government - continue, and the formation sheet opens with the round. The outgoing government serves on as a caretaker until a proposal wins its investiture."
            : $"The Speaker will ask {firstShortName ?? "no party"} first to form a government. The outgoing government serves on as a caretaker until a proposal wins its investiture.";

        /// <summary>Premise 9: the control's face where the player's party is asked first.</summary>
        public const string ContinueToSheet = "CONTINUE · FORM A GOVERNMENT";

        private static void BuildFooter(Transform parent, string verdict, ElectionNightScreen screen, string continueLabel = null, SpeakerFoot speaker = null, Transform overlay = null)
        {
            if (speaker != null)
            {
                // Board 21d (§685): who is asked first and the one caretaker fact, each a mark or a stamp; one decision, one brass face - FORM A
                // GOVERNMENT where the player's party is asked (the act that changes the sim beyond time), CONTINUE on paper where it is not (it only
                // passes time). The two sentences are the control's slip.
                Transform speakerRow = CanvasRows.HRow(parent, "Footer", 56f, 12f);
                Transform asks = CanvasRows.HRow(speakerRow, "Asks", 30f, 10f);
                CanvasRows.Caption(asks, speaker.Country == CountryId.Germany ? "THE BUNDESPRÄSIDENT PROPOSES FIRST" : "THE SPEAKER ASKS FIRST", 12, PoliSimTheme.TextPrimary);   // §707
                if (speaker.FirstAsked != null) { CanvasRows.Mark(asks, speaker.Country, speaker.FirstAsked, 18f); }
                else { CanvasRows.Caption(asks, "NO PARTY", 12, PoliSimTheme.TextMuted); }
                if (speaker.PlayerFirst) { CanvasRows.Stamp(asks, "YOUR PARTY", 11, PoliSimTheme.TextPrimary); }
                CanvasRows.Caption(asks, "· OUTGOING GOVERNMENT", 12, PoliSimTheme.TextMuted);
                CanvasRows.Stamp(asks, "CARETAKER", 11, PoliSimTheme.TextPrimary);
                CanvasRows.Slip(asks.gameObject, overlay, speaker.Country == CountryId.Germany ? "THE BUNDESPRÄSIDENT PROPOSES FIRST" : "THE SPEAKER ASKS FIRST", SlipLines(speaker.Sentence));
                CanvasRows.Spacer(speakerRow);
                string face = speaker.PlayerFirst ? ContinueToSheet : "CONTINUE";
                float width = speaker.PlayerFirst ? 320f : 200f;
                Button control = CanvasChrome.FacedButton(speakerRow, "ContinueButton", face, PoliSimTheme.Display, 16, PoliSimTheme.TextPrimary, new Vector2(width, 48f),
                    speaker.PlayerFirst ? CanvasChrome.Face.Brass : CanvasChrome.Face.Paper, FontStyle.Normal);
                LayoutElement size = control.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = width;
                size.minHeight = size.preferredHeight = 48f;
                control.onClick.AddListener(() => screen.Dismiss());
                CanvasRows.Slip(control.gameObject, overlay, face, SlipLines(speaker.Sentence));
                return;
            }

            var foot = new GameObject("Footer");
            foot.transform.SetParent(parent, false);
            foot.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            var row = foot.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;   // the masthead's reason, above: the spare height is the body's
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 24f;
            foot.AddComponent<LayoutElement>().minHeight = 56f;

            Text line = CanvasChrome.MakeTextRealWeight(foot.transform, "Verdict", string.IsNullOrEmpty(verdict) ? string.Empty : verdict.ToUpperInvariant(),
                PoliSimTheme.Display, 12, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            BuildContinueButton(foot.transform, screen.Dismiss, continueLabel);
        }

        /// <summary>The canvas brass button, through the ONE definition of it since P6-A2 (2026-09-17):
        /// `CanvasChrome.FacedButton`. This file used to carry its own copy of `SigningScreen.BuildSignButton`
        /// - the same strips, the same SpriteSwap, the same flat-brass degradation - and the playtest's
        /// finding 2 is what made a third copy unacceptable rather than merely untidy.</summary>
        private static void BuildContinueButton(Transform parent, Action onContinue, string label = null)
        {
            string face = string.IsNullOrEmpty(label) ? "CONTINUE" : label;
            float width = string.IsNullOrEmpty(label) ? 200f : 320f;   // §656: the longer face, the control to the formation sheet
            Button control = CanvasChrome.FacedButton(parent, "ContinueButton", face, PoliSimTheme.Display, 16,
                PoliSimTheme.TextPrimary, new Vector2(width, 48f), style: FontStyle.Normal);   // §626 (D6, board 17a): TextPrimary on the brass face; §648: the Display file is the weight
            LayoutElement size = control.gameObject.AddComponent<LayoutElement>();
            size.minWidth = width;
            size.preferredWidth = width;
            size.minHeight = 48f;
            control.onClick.AddListener(() => onContinue());
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
