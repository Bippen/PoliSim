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
    /// reproduces 2022 seat-for-seat. The screen has no call rule of its own to disagree with.
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
    ///   it - the 2022 seed IS the previous election - so V-N3 never needed a second one. Only a later
    ///   term, whose record keeps national figures, still waits for the complete count.
    /// </summary>
    public class ElectionNightScreen
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
            GovernmentFormation.View government = null, CountryId inkCountry = CountryId.Sweden)
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

            Sprite frame = CanvasChrome.Sliced("ui_frame_ornate", 64f, 64f, 64f, 64f);
            Texture2D scrimTexture = IconLibrary.GetChrome("ui_scrim_takeover");
            if (frame == null || state == null || partyNames == null)
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
            RectTransform contentRect = content.AddComponent<RectTransform>();
            Stretch(contentRect);
            contentRect.offsetMin = new Vector2(52f, 34f);
            contentRect.offsetMax = new Vector2(-52f, -38f);
            var column = content.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.spacing = 10f;

            BuildMasthead(content.transform, state, countryName, pollsClosed, totalSeats);
            ValkretsCartogramView map = BuildBody(content.transform, state, partyNames, totalSeats, previous, ledger, ledgerParty,
                standingBudget, standingBudgetCitation, government, inkCountry);
            BuildFooter(content.transform, verdict, screen);

            // The map lays itself in the rect the page gives it; resolve the page now so the first frame already has it,
            // not two frames on (the signing screen's lesson: a capture can photograph an unlaid rect).
            // ⚠ On the CONTENT column, not the document: the document carries no layout controller, and uGUI does not descend
            // past a rect without one - a rebuild asked of the document lays nothing (measured: every band read its default 100).
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            if (map != null) { map.Rebuild(); }
            screen.Map = map;

            // S-20: the board stamps its own capture-identity token, so a film that shows the desk over it
            // fails on the pixels rather than passing on a clean exit code.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            return screen;
        }
        private const float DocumentWidth = 1240f;


        /// <summary>§A.14: institution + title left, timestamp and the declared chip right.</summary>
        private static void BuildMasthead(Transform parent, NightState state, string countryName,
            DateTime pollsClosed, int totalSeats)
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

            CanvasChrome.MakeText(left.transform, "Institution", "RETURNING OFFICER  ·  " + countryName.ToUpperInvariant(),
                PoliSimTheme.Display, 12, PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleLeft, FontStyle.Bold)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);
            CanvasChrome.MakeText(left.transform, "Title", "ELECTION NIGHT",
                PoliSimTheme.Display, 30, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 34f);

            var right = new GameObject("Right");
            right.transform.SetParent(mast.transform, false);
            right.AddComponent<RectTransform>();
            var rightColumn = right.AddComponent<VerticalLayoutGroup>();
            rightColumn.childControlHeight = true;
            rightColumn.childForceExpandHeight = false;
            rightColumn.childAlignment = TextAnchor.MiddleRight;

            CanvasChrome.MakeText(right.transform, "Timestamp",
                pollsClosed.AddMinutes(state.Minute).ToString("HH:mm", CultureInfo.InvariantCulture) + "  ·  POLLS CLOSED " + pollsClosed.ToString("HH:mm", CultureInfo.InvariantCulture),
                PoliSimTheme.Document, 12, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

            // The chip §A.14 names: "N OF M SEATS DECLARED". It counts CONSTITUENCIES declared,
            // because seats are national in this system and are not declared one at a time - the
            // chip says what is actually true rather than what the mock-up's wording implied.
            var chip = new GameObject("DeclaredChip");
            chip.transform.SetParent(right.transform, false);
            chip.AddComponent<RectTransform>().sizeDelta = new Vector2(300f, 22f);
            chip.AddComponent<Image>().color = PoliSimTheme.Hex(0x5D564A);
            chip.AddComponent<LayoutElement>().minHeight = 22f;
            Text chipText = CanvasChrome.MakeText(chip.transform, "ChipText",
                string.Format(CultureInfo.InvariantCulture, "{0} OF {1} CONSTITUENCIES DECLARED", state.DeclaredCount, state.TotalConstituencies),
                PoliSimTheme.Display, 11, PoliSimTheme.Hex(0xF4ECDC), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(chipText.GetComponent<RectTransform>());
        }

        /// <summary>
        /// The body, three columns (2026-09-10, election night items 1-3): THE COUNT and the calls at the left, THE MAP at
        /// the centre - the screen's centre, which had never been drawn - and WHO GOVERNS at the right, above the campaign's
        /// ledger and the inherited budget's estimate. ⚠ The constituency list is gone: it printed a raw count per valkrets,
        /// which the map now says better (the winner in its ink, the margin, the change), so the list yields to it whole.
        /// </summary>
        private static ValkretsCartogramView BuildBody(Transform parent, NightState state, string[] partyNames, int totalSeats,
            Previous previous, VoteAttribution.Ledger ledger, string ledgerParty,
            IReadOnlyList<DivisionEffect> standingBudget, string standingBudgetCitation,
            GovernmentFormation.View government, CountryId inkCountry)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            body.AddComponent<RectTransform>();
            var grid = body.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = 28f;
            grid.childControlWidth = true;
            grid.childControlHeight = true;
            grid.childForceExpandHeight = true;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // The map is width-limited (board 4a's 1080 : 587.6), so the width is what makes it the page's centre: it takes
            // half the body, and the two text columns hold what their longest line needs at 1280.
            Transform count = Column(body.transform, "Count", 1.0f);
            BuildTally(count, state, partyNames, totalSeats, previous);
            BuildCalls(count, state, partyNames);

            Transform centre = Column(body.transform, "MapColumn", 2.1f);
            ValkretsCartogramView map = BuildMap(centre, state, partyNames, previous, inkCountry);

            Transform right = Column(body.transform, "Governs", 0.9f);
            BuildGovernment(right, state, government, inkCountry);
            BuildLedger(right, ledger, ledgerParty);   // P2-4.3
            BuildEstimate(right, standingBudget, standingBudgetCitation);   // board 5c
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
            Text key = CanvasChrome.MakeText(parent, "MapKey",
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

        /// <summary>
        /// Item 3: WHO GOVERNS. A count that does not say who won is not a result - so the formation (W-D3) is run on the new
        /// chamber and drawn: the outcome by its own name, the cabinet and who carries it from outside, the investiture the
        /// chamber's own rule sets, and one line for the player by C-R4's rule (D-5 (a): office is cabinet membership, and
        /// support from outside is not office). ⚠ On a PARTIAL count nothing is formed: a government on projected seats is a
        /// forecast, and the section says the formation waits for the last constituency.
        /// </summary>
        private static void BuildGovernment(Transform parent, NightState state, GovernmentFormation.View government, CountryId inkCountry)
        {
            if (state != null && !state.Complete)
            {
                Heading(parent, "WHO GOVERNS - WHEN THE COUNT IS COMPLETE");
                Row(parent, "the chamber is not final until the last valkrets declares", "—", 11, PoliSimTheme.TextMuted);
                return;
            }
            Heading(parent, "WHO GOVERNS - FORMED ON THE NEW CHAMBER");
            if (government == null)
            {
                Row(parent, "no formation was run for this night", "—", 11, PoliSimTheme.TextMuted);
                return;
            }
            if (!government.HasGovernment)
            {
                Row(parent, "NO GOVERNMENT CAN FORM", "NEW ELECTION", 13, PoliSimTheme.Bad, bold: true);
                Wrapped(parent, government.Reason ?? string.Empty, 11, PoliSimTheme.TextSecondary);
            }
            else
            {
                Row(parent, OutcomeName(government.Outcome), string.Format(CultureInfo.InvariantCulture, "{0} of {1} seats in cabinet",
                    government.CabinetSeats, government.TotalSeats), 13, PoliSimTheme.TextPrimary, bold: true);
                Row(parent, "IN CABINET  " + Parties(government.Cabinet), government.CabinetSeats.ToString(CultureInfo.InvariantCulture), 12, PoliSimTheme.TextPrimary);
                Row(parent, government.Support.Count > 0 ? "SUPPORT FROM OUTSIDE  " + Parties(government.Support) : "NO DECLARED SUPPORT FROM OUTSIDE",
                    government.Support.Count > 0 ? string.Format(CultureInfo.InvariantCulture, "{0} with support", government.SupportedSeats) : "—",
                    12, PoliSimTheme.TextSecondary);
                BuildGovernmentBar(parent, government, inkCountry);
                Wrapped(parent, government.NegativeRule
                        ? string.Format(CultureInfo.InvariantCulture, "INVESTITURE BY THE NEGATIVE RULE: {0} seats vote against it, and it takes {1} against to refuse it - it takes office", government.OpposedSeats, government.Majority)
                        : string.Format(CultureInfo.InvariantCulture, "INVESTITURE BY A MAJORITY FOR: {0} seats for it, {1} needed - it takes office", government.SupportedSeats, government.Majority),
                    11, PoliSimTheme.TextSecondary);
                if (!government.DeclarationsSourced)
                {
                    Wrapped(parent, "formed on DERIVED red lines only - this country's declared refusals are not sourced", 10, PoliSimTheme.TextMuted);
                }
            }

            // The player's outcome, by C-R4's rule, in one plain line.
            if (government.PlayerParty == null)
            {
                Row(parent, "NO PLAYER'S PARTY ON THIS BALLOT", "—", 11, PoliSimTheme.TextMuted);
            }
            else if (!government.HasGovernment)
            {
                Row(parent, "YOU - " + government.PlayerParty, "STAY IN OFFICE UNTIL ONE CAN FORM", 13, PoliSimTheme.Caution, bold: true);
            }
            else if (government.PlayerInCabinet)
            {
                Row(parent, "YOU - " + government.PlayerParty + " IN THE CABINET", "IN OFFICE", 14, PoliSimTheme.Good, bold: true);
            }
            else
            {
                Row(parent, "YOU - " + government.PlayerParty + (government.PlayerSupports ? " SUPPORTS IT FROM OUTSIDE" : " IN OPPOSITION"),
                    "OUT OF OFFICE", 14, PoliSimTheme.Bad, bold: true);
                if (government.PlayerSupports)
                {
                    Wrapped(parent, "support is not office - the rule (C-R4, D-5 (a)) counts cabinet seats only", 10, PoliSimTheme.TextMuted);
                }
            }
        }

        private static string OutcomeName(CoalitionOutcomeKind kind)
        {
            switch (kind)
            {
                case CoalitionOutcomeKind.MajorityCoalition: return "A MAJORITY GOVERNMENT";
                case CoalitionOutcomeKind.ConfidenceAndSupply: return "A MINORITY GOVERNMENT WITH SUPPORT";
                case CoalitionOutcomeKind.MinorityGovernment: return "A MINORITY GOVERNMENT";
                default: return kind.ToString().ToUpperInvariant();
            }
        }

        private static string Parties(List<(string Abbrev, int Seats)> list)
        {
            var parts = new List<string>(list.Count);
            foreach ((string abbrev, int seats) in list) { parts.Add(abbrev + " " + seats.ToString(CultureInfo.InvariantCulture)); }
            return string.Join(" · ", parts);
        }

        /// <summary>The government drawn as one bar across the chamber: the cabinet's parties in their inks, the parties carrying it
        /// from outside in their inks at half strength, the rest paper, and the majority line as a dark tick.</summary>
        private static void BuildGovernmentBar(Transform parent, GovernmentFormation.View government, CountryId inkCountry)
        {
            const int w = 480, h = 22;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            Color32 paper = PoliSimTheme.CardInset;
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = paper; }
            float perSeat = government.TotalSeats > 0 ? w / (float)government.TotalSeats : 0f;
            float x = 0f;
            void Paint(List<(string Abbrev, int Seats)> list, bool support)
            {
                foreach ((string abbrev, int seats) in list)
                {
                    Color ink = PoliSimTheme.PartyLaddered(inkCountry, abbrev);
                    int from = Mathf.RoundToInt(x), to = Mathf.RoundToInt(x + seats * perSeat);
                    for (int px = from; px < to && px < w; px++)
                    {
                        for (int py = 0; py < h; py++)
                        {
                            bool hatch = support && ((px + py) % 4) < 2;
                            pixels[py * w + px] = hatch ? (Color32)Color.Lerp(ink, PoliSimTheme.CardInset, 0.55f) : (Color32)ink;
                        }
                    }
                    if (to - 1 >= 0 && to - 1 < w) { for (int py = 0; py < h; py++) { pixels[py * w + to - 1] = paper; } }
                    x += seats * perSeat;
                }
            }
            Paint(government.Cabinet, false);
            Paint(government.Support, true);
            int tick = Mathf.Clamp(Mathf.RoundToInt(government.Majority * perSeat), 1, w - 2);
            for (int py = 0; py < h; py++) { pixels[py * w + tick] = PoliSimTheme.TextPrimary; pixels[py * w + tick - 1] = PoliSimTheme.TextPrimary; }
            texture.SetPixels32(pixels);
            texture.Apply();

            var art = new GameObject("GovernmentBar");
            art.transform.SetParent(parent, false);
            art.AddComponent<RectTransform>();
            LayoutElement element = art.AddComponent<LayoutElement>();
            element.minHeight = 22f;
            element.preferredHeight = 22f;
            RawImage image = art.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            Wrapped(parent, string.Format(CultureInfo.InvariantCulture, "cabinet solid · support hatched · the tick is the majority, {0} of {1}",
                government.Majority, government.TotalSeats), 10, PoliSimTheme.TextMuted);
        }

        private static void Wrapped(Transform parent, string text, int size, Color ink)
        {
            Text t = CanvasChrome.MakeText(parent, "Note", text, PoliSimTheme.Document, size, ink, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.gameObject.AddComponent<LayoutElement>().minHeight = size + 6f;
        }

        /// <summary>
        /// Board 5c (D11 row 3): the effects plate at the right of the count - the same renderer the signing
        /// takeover paints (CanvasPaint.Arrows), titled for the chamber's first budget if unchanged, with the
        /// figures in their arrows' inks and the scope line verbatim. The estimate is the one that travelled
        /// with the standing budget act as enacted - not a forecast made tonight - and the citation says whose.
        /// </summary>
        private static void BuildEstimate(Transform parent, IReadOnlyList<DivisionEffect> standingBudget, string citation)
        {
            Heading(parent, EffectArrowsRenderer.PlateTitleInherited);
            if (standingBudget == null || standingBudget.Count == 0)
            {
                Row(parent, "NO ENACTED BUDGET CARRIES AN ESTIMATE - NOTHING IS DRAWN IN ITS PLACE", "—", 11, PoliSimTheme.TextMuted);
                return;
            }

            var arrows = new List<EffectArrow>(standingBudget.Count);
            foreach (DivisionEffect e in standingBudget) { arrows.Add(new EffectArrow(e.Name, e.Value, e.HigherIsBetter, e.Figure)); }

            var art = new GameObject("Arrows");
            art.transform.SetParent(parent, false);
            art.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 110f);
            LayoutElement element = art.AddComponent<LayoutElement>();
            element.minHeight = 70f;
            element.preferredHeight = 110f;
            Texture2D texture = CanvasPaint.Arrows(420, 120, arrows, PoliSimTheme.Hex(0xF2EADB));
            Image image = art.AddComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            image.preserveAspect = true;
            image.raycastTarget = false;

            Text figures = CanvasChrome.MakeText(parent, "Figures", EffectArrowsRenderer.FiguresLine(arrows), PoliSimTheme.Document, 11, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            figures.horizontalOverflow = HorizontalWrapMode.Wrap;
            figures.gameObject.AddComponent<LayoutElement>().minHeight = 18f;
            if (!string.IsNullOrEmpty(citation))
            {
                Wrapped(parent, "AS ENACTED - " + citation, 11, PoliSimTheme.TextSecondary);   // one line that wraps: as a two-cell row its key broke letter by letter in the narrower column (2026-09-10)
            }
            Text scope = CanvasChrome.MakeText(parent, "Scope", EffectArrowsRenderer.ScopeLine, PoliSimTheme.Document, 10, PoliSimTheme.TextSecondary, TextAnchor.UpperLeft);
            scope.horizontalOverflow = HorizontalWrapMode.Wrap;
            scope.gameObject.AddComponent<LayoutElement>().minHeight = 28f;
        }

        private static void Heading(Transform parent, string text)
        {
            Text t = CanvasChrome.MakeText(parent, "Heading", text, PoliSimTheme.Display, 11,
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

            Text nameText = CanvasChrome.MakeText(row.transform, "Name", name, PoliSimTheme.Document, size, ink,
                TextAnchor.MiddleLeft, bold ? FontStyle.Bold : FontStyle.Normal);
            Text figureText = CanvasChrome.MakeText(row.transform, "Figure", figure, PoliSimTheme.Document, size, ink,
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
            // count was V-N3's whole blocker, and a first-term night always had it: the 2022 seed IS the previous election, and
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
                string figure = string.Format(CultureInfo.InvariantCulture, "{0:N0}  {1:P2}  {2} seats",
                    state.CountedVotes[p], state.CountedShare(p), state.SeatsOnCounted[p]);
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
        /// P2-4.3 (2026-09-02): the campaign's attribution beside the count - W-E7's "why, line by line" for the
        /// player's party, the largest lines first, from the ledger the simulation kept when the live run finished
        /// (SimulationManager.PlayerCampaignLedger). Absent when no live campaign ran to this election, and the
        /// column says so rather than drawing zeros.
        /// </summary>
        private static void BuildLedger(Transform parent, VoteAttribution.Ledger ledger, string party)
        {
            Heading(parent, ledger == null ? "WHY — NO CAMPAIGN LEDGER FOR THIS NIGHT" : $"WHY — {party.ToUpperInvariant()}, LINE BY LINE");
            if (ledger == null || ledger.Lines.Count == 0)
            {
                Row(parent, ledger == null ? "no live campaign was run to this election" : "NO ATTRIBUTION FOR THIS PARTY", "—", 12, PoliSimTheme.TextMuted);
                return;
            }
            var ordered = new List<KeyValuePair<VoteAttributionSource, double>>(ledger.Lines);
            ordered.Sort((a, b) => Math.Abs(b.Value).CompareTo(Math.Abs(a.Value)));
            foreach (KeyValuePair<VoteAttributionSource, double> line in ordered)
            {
                if (Math.Abs(line.Value) < 5e-7) { continue; }   // below a hundredth of a pp
                Row(parent, System.Text.RegularExpressions.Regex.Replace(line.Key.ToString(), "([a-z])([A-Z])", "$1 $2").ToUpperInvariant(),
                    string.Format(CultureInfo.InvariantCulture, "{0:+0.000;-0.000} pp", line.Value * 100.0), 12, PoliSimTheme.TextPrimary);
            }
        }

        /// <summary>
        /// Item 1 (2026-09-10): a call is news when it LANDS. While the night runs, each call prints with the constituency count
        /// at which it became safe (the model stamps it - `ElectionNight.StampLandings`), in the order they landed. Once the count
        /// is complete the calls are history and collapse to a summary: the parties that hold seats as ONE line with the count
        /// each was called at, the parties shut out likewise, and the largest party and any bloc call on their own lines - so
        /// nine identical "will hold seats" rows at 29 of 29 become one statement that says when each was known.
        /// </summary>
        private static void BuildCalls(Transform parent, NightState state, string[] partyNames)
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

            SummariseGroup(parent, state, partyNames, CallKind.ThresholdCleared, "HOLD SEATS");
            SummariseGroup(parent, state, partyNames, CallKind.ThresholdMissed, "SHUT OUT BY THE THRESHOLD");
            foreach (ElectionCall call in state.Calls)
            {
                if (call.Kind == CallKind.ThresholdCleared || call.Kind == CallKind.ThresholdMissed) { continue; }
                Row(parent, CallText(call, partyNames).ToUpperInvariant(),
                    string.Format(CultureInfo.InvariantCulture, "called at {0} of {1}", call.DeclaredAt, call.OfTotal),
                    12, PoliSimTheme.TextPrimary, bold: true);
            }
        }

        private static void SummariseGroup(Transform parent, NightState state, string[] partyNames, CallKind kind, string verb)
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
                Wrapped(parent, "each at the valkrets count it was called:  " + string.Join(" · ", landed), 11, PoliSimTheme.TextSecondary);
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
        private static void BuildFooter(Transform parent, string verdict, ElectionNightScreen screen)
        {
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

            Text line = CanvasChrome.MakeText(foot.transform, "Verdict", string.IsNullOrEmpty(verdict) ? string.Empty : verdict.ToUpperInvariant(),
                PoliSimTheme.Display, 12, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            BuildContinueButton(foot.transform, screen.Dismiss);
        }

        /// <summary>The canvas brass button, the signing screen's own pattern (SigningScreen.BuildSignButton): uGUI Button + SpriteSwap over the delivered per-state strips, a flat brass face when the strips are missing.</summary>
        private static void BuildContinueButton(Transform parent, Action onContinue)
        {
            Sprite normal = CanvasChrome.Sliced("ui_btn_brass_canvas", 24f, 24f, 24f, 24f);
            Sprite hover = CanvasChrome.Sliced("ui_btn_brass_canvas_hover", 24f, 24f, 24f, 24f);
            Sprite pressed = CanvasChrome.Sliced("ui_btn_brass_canvas_pressed", 24f, 24f, 24f, 24f);

            var button = new GameObject("ContinueButton");
            button.transform.SetParent(parent, false);
            button.AddComponent<RectTransform>().sizeDelta = new Vector2(200f, 48f);
            LayoutElement size = button.AddComponent<LayoutElement>();
            size.minWidth = 200f;
            size.preferredWidth = 200f;
            size.minHeight = 48f;
            Image face = button.AddComponent<Image>();
            if (normal != null)
            {
                face.sprite = normal;
                face.type = Image.Type.Sliced;
                face.pixelsPerUnitMultiplier = 2f;
            }
            else
            {
                face.color = PoliSimTheme.Hex(0x8A6B2F);
            }

            Button control = button.AddComponent<Button>();
            if (normal != null && hover != null && pressed != null)
            {
                control.transition = Selectable.Transition.SpriteSwap;
                control.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = pressed };
            }

            control.onClick.AddListener(() => onContinue());

            Text label = CanvasChrome.MakeText(button.transform, "Label", "CONTINUE", PoliSimTheme.Display, 16,
                PoliSimTheme.Hex(0xF0E7D8), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch((RectTransform)label.transform);
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
