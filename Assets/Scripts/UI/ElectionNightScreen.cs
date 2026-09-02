using System;
using System.Collections.Generic;
using System.Globalization;
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
    ///   misleading thing this screen could show, on the night it matters most. ⚠ A RUNNING swing on a
    ///   like-for-like basis needs the previous election's PER-CONSTITUENCY votes so the comparison can
    ///   be restricted to what is actually in; that is V-N3's real blocker, still standing, now stated
    ///   at the level it applies to rather than against the whole column.
    /// </summary>
    public class ElectionNightScreen
    {
        public GameObject Root { get; private set; }

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
        public static ElectionNightScreen Build(NightState state, string[] partyNames, string countryName,
            DateTime pollsClosed, int totalSeats, long[] previousVotes = null, string previousLabel = null,
            string verdict = null)
        {
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
            doc.anchorMin = new Vector2(0.5f, 0.5f);
            doc.anchorMax = new Vector2(0.5f, 0.5f);
            doc.pivot = new Vector2(0.5f, 0.5f);
            doc.sizeDelta = new Vector2(DocumentWidth, 720f);
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
            BuildBody(content.transform, state, partyNames, totalSeats, previousVotes, previousLabel);
            BuildFooter(content.transform, verdict, screen);

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

        /// <summary>§A.14's body grid, 1.25fr | 1fr: the national tally left, the constituencies and the calls right.</summary>
        private static void BuildBody(Transform parent, NightState state, string[] partyNames, int totalSeats,
            long[] previousVotes, string previousLabel)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            body.AddComponent<RectTransform>();
            var grid = body.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = 36f;
            grid.childControlWidth = true;
            grid.childControlHeight = true;
            grid.childForceExpandHeight = true;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var tally = new GameObject("Tally");
            tally.transform.SetParent(body.transform, false);
            tally.AddComponent<RectTransform>();
            tally.AddComponent<LayoutElement>().flexibleWidth = 1.25f;
            var tallyColumn = tally.AddComponent<VerticalLayoutGroup>();
            tallyColumn.childControlHeight = true;
            tallyColumn.childForceExpandHeight = false;
            tallyColumn.spacing = 2f;
            BuildTally(tally.transform, state, partyNames, totalSeats, previousVotes, previousLabel);

            var side = new GameObject("Side");
            side.transform.SetParent(body.transform, false);
            side.AddComponent<RectTransform>();
            side.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var sideColumn = side.AddComponent<VerticalLayoutGroup>();
            sideColumn.childControlHeight = true;
            sideColumn.childForceExpandHeight = false;
            sideColumn.spacing = 2f;
            BuildCalls(side.transform, state, partyNames);
            BuildConstituencies(side.transform, state);
        }

        private static void Heading(Transform parent, string text)
        {
            Text t = CanvasChrome.MakeText(parent, "Heading", text, PoliSimTheme.Display, 11,
                PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleLeft, FontStyle.Bold);
            t.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 20f;
        }

        /// <summary>One row: name at the left, figures at the right. Absence is an em dash, never a zero.</summary>
        private static void Row(Transform parent, string name, string figure, int size, Color ink, bool bold = false)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().minHeight = 20f;

            CanvasChrome.MakeText(row.transform, "Name", name, PoliSimTheme.Document, size, ink,
                TextAnchor.MiddleLeft, bold ? FontStyle.Bold : FontStyle.Normal);
            CanvasChrome.MakeText(row.transform, "Figure", figure, PoliSimTheme.Document, size, ink,
                TextAnchor.MiddleRight, bold ? FontStyle.Bold : FontStyle.Normal);
        }

        /// <summary>
        /// The national tally OF WHAT HAS DECLARED. Two things this panel must say and does:
        /// the share is of the counted vote, not of the electorate; and the seats are a PROJECTION
        /// until the last constituency is in, and the heading says so in those words rather than
        /// letting a reader assume a final number.
        /// </summary>
        private static void BuildTally(Transform parent, NightState state, string[] partyNames, int totalSeats,
            long[] previousVotes, string previousLabel)
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

            // C-D5 (V-N3): the swing column, and ⚠ ONLY ON A COMPLETE COUNT.
            //
            // A swing is a comparison, and a comparison is only honest on a like-for-like basis. Early in
            // the night `CountedShare` is the share of FOUR declared constituencies; setting that beside a
            // full previous national result would print a number that looks like a swing and is an
            // artefact of which constituencies happen to have declared first - the single most misleading
            // thing this screen could show, on the night it matters most. So while the count is partial the
            // screen SAYS the swing is withheld and why, which is the same discipline as the seat
            // projection it already labels as a projection.
            //
            // ⚠ A RUNNING swing on a like-for-like basis is buildable and is NOT built here: it needs the
            // previous election's PER-CONSTITUENCY votes so the comparison can be restricted to the
            // constituencies actually in. That is V-N3's original blocker, still standing, now stated at
            // the level it really applies to rather than to the whole column.
            long previousValid = 0;
            if (previousVotes != null && previousVotes.Length == partyNames.Length)
            {
                foreach (long v in previousVotes) { previousValid += v; }
            }

            bool swingShown = state.Complete && previousValid > 0;

            int seatsShown = 0;
            foreach (int p in order)
            {
                seatsShown += state.SeatsOnCounted[p];
                string figure = string.Format(CultureInfo.InvariantCulture, "{0:N0}    {1:P2}    {2} seats",
                    state.CountedVotes[p], state.CountedShare(p), state.SeatsOnCounted[p]);

                if (swingShown)
                {
                    double swing = (state.CountedShare(p) - previousVotes[p] / (double)previousValid) * 100.0;
                    figure += string.Format(CultureInfo.InvariantCulture, "    {0:+0.00;-0.00;0.00} pp", swing);
                }

                Row(parent, partyNames[p], figure, 13, PoliSimTheme.TextPrimary);
            }

            Row(parent, "COUNTED", string.Format(CultureInfo.InvariantCulture, "{0:N0} votes    {1} of {2} seats",
                state.CountedValid, seatsShown, totalSeats), 12, PoliSimTheme.TextSecondary, bold: true);

            if (swingShown)
            {
                Row(parent, "SWING", "against " + (previousLabel ?? "the previous election"), 12, PoliSimTheme.TextSecondary);
            }
            else if (previousValid > 0)
            {
                Row(parent, "SWING", "held back until every constituency is in - a swing on a partial count compares different places",
                    12, PoliSimTheme.TextMuted);
            }
            else
            {
                Row(parent, "SWING", "no previous election is on hand to compare against", 12, PoliSimTheme.TextMuted);
            }
        }

        /// <summary>
        /// The calls. Each one is a claim the model has proved cannot be overturned by anything
        /// still outstanding, so the panel states it flatly; where nothing is safe yet it says that
        /// too, rather than showing a projection dressed as a call.
        /// </summary>
        private static void BuildCalls(Transform parent, NightState state, string[] partyNames)
        {
            Heading(parent, "CALLS — SAFE WHATEVER IS STILL OUT");
            if (state.Calls.Count == 0)
            {
                Row(parent, "NOTHING CAN BE CALLED YET", "—", 12, PoliSimTheme.TextMuted);
                return;
            }

            foreach (ElectionCall call in state.Calls)
            {
                Row(parent, CallText(call, partyNames),
                    string.Format(CultureInfo.InvariantCulture, "at {0} of {1}", call.DeclaredAt, call.OfTotal),
                    12, PoliSimTheme.TextPrimary);
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

        /// <summary>
        /// The constituencies. An undeclared one is drawn with its NAME and its electorate — both
        /// known before the night — and an em dash where its result would be. Not a zero: a
        /// constituency that has not declared and one that returned nothing are different facts.
        /// </summary>
        private static void BuildConstituencies(Transform parent, NightState state)
        {
            Heading(parent, "THE CONSTITUENCIES");
            int shown = 0;
            foreach (ConstituencyReport c in state.Constituencies)
            {
                if (shown >= 12) { break; }
                shown++;
                Row(parent, c.Name,
                    c.Declared
                        ? string.Format(CultureInfo.InvariantCulture, "{0:N0} counted", c.Valid)
                        : "—    expected " + Clock(c.ArrivesAtMinute),
                    11, c.Declared ? PoliSimTheme.TextSecondary : PoliSimTheme.TextMuted);
            }

            if (state.TotalConstituencies > shown)
            {
                Row(parent, string.Format(CultureInfo.InvariantCulture, "and {0} more", state.TotalConstituencies - shown), "", 11, PoliSimTheme.TextMuted);
            }
        }
        /// <summary>A minute of the night as a wall clock reading from the close of polls - no format escape, and no DateTime standing in for a duration.</summary>
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

        private static string Clock(int minute)
        {
            int hour = 20 + minute / 60;
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hour % 24, minute % 60);
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
