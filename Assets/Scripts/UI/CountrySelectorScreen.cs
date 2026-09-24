using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// CANVAS PILOT (2026-08-12) — the country selector, the first Canvas screen, built per §A.14 and
    /// chosen as the pilot because it is self-contained and already a full-screen state. Programmatic
    /// uGUI throughout (no scene edits); the seam that shows/hides it lives in GameController's
    /// takeover machine, never here — a screen owns its content, the CONTROLLER owns the boundary,
    /// which is the pattern the other seven screens copy.
    ///
    /// **Declared deviations from §A.14, per the boards-deviation practice (V-series):**
    /// - V-C1: no kicker line — §A.14 shows one but no kicker copy exists anywhere in this project,
    ///   and inventing copy is out; wordmark + rule pair + subtitle only.
    /// - V-C2: hover lightens by a uniform tint rather than re-cutting the body gradient (the
    ///   gradient is baked in `ui_folder_country`; a second hover sprite was never delivered), and
    ///   the "button promotes to brass" sub-state is folded into the card being one large button.
    /// - V-C3: the SELECTED beat ("folder opens, brief slides in, 320ms") is a press-acknowledge
    ///   scale only — the opening-folder animation is entrance-art the pilot defers; the exit
    ///   envelope's cover is what carries the moment. Not a pattern the other screens copy blindly:
    ///   each screen's entrance beats are its own §1C.4 row.
    /// </summary>
    public class CountrySelectorScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>CL-2: the party panel over the folders - the country's seeded chamber, largest first; null when no country is open.</summary>
        private GameObject _partyPanel;

        /// <summary>18a: the open folder's SHEET - the start screen, full-bleed paper over the selector; null when no folder is open.</summary>
        private GameObject _sheet;

        /// <summary>The 3×2 folder grid, hidden while a folder is open and restored when it closes.</summary>
        private GameObject _folderGrid;

        /// <summary>
        /// Build the screen under the shared host. Returns null when the folder sprite is missing — the caller keeps the IMGUI selector
        /// as the degradation path, so a broken import costs the new look, never the ability to start a game. CL-2 (2026-09-13; DS-6,
        /// R-CL1): a folder OPENS the country's chamber - the party panel - and a party seats the player; <paramref name="onSelect"/> is
        /// called with the country and the party's abbreviation. The largest-party stand-in that selection seated since C-R2 is retired
        /// from this path (the harness's one-argument shape keeps it).
        /// </summary>
        public static CountrySelectorScreen Build(World world, Action<CountryId, string> onSelect,
            IReadOnlyList<ScenarioDefinition> scenarios = null, Action<ScenarioDefinition> onScenario = null)
        {
            Sprite folder = CanvasChrome.Sliced("ui_folder_country", 48f, 48f, 72f, 40f);
            if (folder == null || world == null)
            {
                Debug.LogWarning("CANVAS: ui_folder_country missing - the IMGUI selector remains the live path.");
                return null;
            }

            Canvas canvas = CanvasChrome.EnsureHost();
            var screen = new CountrySelectorScreen();

            var root = new GameObject("CountrySelector");
            screen.Root = root;
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.AddComponent<RectTransform>());

            // The ground: desk colour under the same menu_pattern_tile the IMGUI selector tiles —
            // that sprite's second call site, and the Canvas one now the primary.
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform, false);
            Stretch(ground.AddComponent<RectTransform>());
            Image groundFill = ground.AddComponent<Image>();
            groundFill.color = PoliSimTheme.Desk;
            groundFill.raycastTarget = false;

            Texture2D tile = IconLibrary.GetTexture("menu_pattern_tile");
            if (tile != null)
            {
                var pattern = new GameObject("Pattern");
                pattern.transform.SetParent(root.transform, false);
                Stretch(pattern.AddComponent<RectTransform>());
                RawImage patternImage = pattern.AddComponent<RawImage>();
                patternImage.texture = tile;
                patternImage.raycastTarget = false;
                pattern.AddComponent<TiledRawImage>().TilePixels = tile.width;
            }

            // Title block, centred: wordmark, rule pair, subtitle (V-C1: no kicker).
            var title = new GameObject("Title");
            title.transform.SetParent(root.transform, false);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            // P6-A2: the scenario controls are faces now, not lines, so the block is taller by the face's
            // padding and starts correspondingly higher. ⚠ The grid below is anchored to the centre and did
            // not move; the block grows into the gap above it, not into it.
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(0f, 172f);
            VerticalLayoutGroup titleLayout = title.AddComponent<VerticalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.UpperCenter;
            titleLayout.spacing = 8f;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            // P6-A2: the scenario controls carry a face, and a face stretched across the whole screen is a
            // bar, not a button; each child keeps its own preferred width and the column centres it.
            titleLayout.childForceExpandWidth = false;

            // ⚠ THE MINIMUMS ARE LOAD-BEARING (P6-A2). A `VerticalLayoutGroup` that cannot fit its children
            // shrinks the ones with no minimum toward zero, and a `Text` squeezed under its own line height
            // is CLIPPED, silently - which is what the canvas-text guard caught the moment the scenario
            // lines grew faces. The wordmark and the subtitle now state what a line of their type needs.
            Text wordmark = CanvasChrome.MakeText(title.transform, "Wordmark", "PoliSim", PoliSimTheme.Display, 54,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            wordmark.gameObject.AddComponent<LayoutElement>().minHeight = WordmarkMinHeight;
            MakeRulePair(title.transform);
            Text subtitle = CanvasChrome.MakeText(title.transform, "Subtitle", "Choose your country", PoliSimTheme.Body, 14,
                PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);
            subtitle.gameObject.AddComponent<LayoutElement>().minHeight = SubtitleMinHeight;

            // STEP 3: the scenario strip — one text line per authored scenario, under the subtitle.
            // Deliberately NOT a seventh folder: the grid is a 3×2 that exactly fits six countries,
            // and a scenario is a different KIND of start (it brings its own country), so it reads as
            // its own line rather than as a seventh peer. The strip is built from the library, so the
            // slate growing from one to six adds lines here with no layout edit.
            if (scenarios != null && onScenario != null)
            {
                foreach (ScenarioDefinition definition in scenarios)
                {
                    BuildScenarioLine(title.transform, definition, onScenario);
                }
            }

            // The 3×2 folder grid, §A.14's own measures at the 1920 reference the scaler establishes.
            var grid = new GameObject("Folders");
            screen._folderGrid = grid;
            grid.transform.SetParent(root.transform, false);
            var gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, -40f);
            gridRect.sizeDelta = new Vector2(1680f, 760f);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(542f, 356f);
            gridLayout.spacing = new Vector2(26f, 30f);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            foreach (Country country in world.Countries)
            {
                BuildFolderCard(grid.transform, country, folder, opened => screen.ShowStartPanel(opened, onSelect));   // SP-1 (§622): the start points first, then the party
            }

            // S-20: the capture-identity token, so a film of this board proves it is this board.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "selector";
            return screen;
        }

        public void SetVisible(bool visible)
        {
            if (Root != null)
            {
                Root.SetActive(visible);
            }
        }

        public void Destroy()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
            }
        }

        /// <summary>The country's seeded parties, largest first - the picker's order (shared with the IMGUI degradation path).</summary>
        public static List<PoliticalParty> PartiesBySeats(CountryId id) => PartiesBySeats(id, WorldClock.PickerViewOf(id).Seats);

        /// <summary>PS-1 (§618): the chamber at the country's START decides the order (a picker reads the start, not the selector's world); a party the
        /// roster carries at zero there (a 2019 list at a 2023 start, a party that missed the threshold) sits at the foot, in the roster's order.</summary>
        public static List<PoliticalParty> PartiesBySeats(CountryId id, Dictionary<string, int> seated)
        {
            var list = new List<PoliticalParty>(PartySystems.For(id));
            list.Sort((a, b) => Seats(seated, b).CompareTo(Seats(seated, a)));
            return list;
        }

        private static int Seats(Dictionary<string, int> seated, in PoliticalParty party) => seated != null && seated.TryGetValue(party.Abbrev, out int n) ? n : 0;

        /// <summary>K-1 part (4) and K-1f: the day-one government's standing, in one wording. The Canvas panel prints it whenever the government
        /// is provisional; the IMGUI pickers print it only when no cabinet forms, because with a cabinet their rows carry the (PROVISIONAL) mark
        /// (§607: the seated Swedish chamber forms none).</summary>
        public static string ProvisionalLine(IReadOnlyList<string> cabinet) => cabinet != null && cabinet.Count > 0
            ? "PROVISIONAL · THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER · THE CABINET MARKED IS THE MODEL'S FORMATION ON THESE SEATS"
            : "PROVISIONAL · THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER · THE MODEL FORMS NO GOVERNMENT FROM THESE SEATS";

        /// <summary>One party's line on the picker: its abbreviation, its name as published, its seats at the last real election, and IN THE CABINET when the chamber's own formation seats it (`GovernmentFormation.Cabinet`).
        /// K-1 part (4): (PROVISIONAL) beside it while that cabinet is the formation's stand-in for a government not yet on record - on the IMGUI pickers, which
        /// have no line to say it; the Canvas panel says it once, under its subtitle, and keeps its rows short (the mark was hiding the longest row's first letter).</summary>
        public static string PartyLine(PoliticalParty party, IReadOnlyList<string> cabinet, bool provisional = false) => PartyLine(party, party.SeedSeats, cabinet, provisional);

        /// <summary>PS-1 (§618): the line with the SEATED chamber's seats - the chamber of record at the start, which need not be the latest election's.</summary>
        public static string PartyLine(PoliticalParty party, int seats, IReadOnlyList<string> cabinet, bool provisional = false)
        {
            bool inCabinet = false;
            if (cabinet != null) { foreach (string abbrev in cabinet) { if (abbrev == party.Abbrev) { inCabinet = true; break; } } }
            return $"{party.ShortName} — {party.Name} · {seats} SEATS{(inCabinet ? (provisional ? " · IN THE CABINET (PROVISIONAL)" : " · IN THE CABINET") : "")}";
        }

        /// <summary>
        /// CL-2: the party panel - the selector's second step. Over the folders (a dim ground that takes the click, so a folder beneath
        /// cannot), the country's chamber as elected: one text line per party, largest first, its seats and whether the chamber's own
        /// formation seats it in the cabinet; a party seats the player and starts the game, BACK returns to the folders. Public so the
        /// capture driver can film it; <paramref name="onSelect"/> null draws the panel and seats nobody.
        /// </summary>
        public void ShowPartyPanel(Country country, Action<CountryId, string> onSelect)
        {
            ClosePartyPanel();   // 18a: the party panel opens OVER the sheet; the sheet closes with it through HidePartyPanel
            if (Root == null || country == null) { return; }

            var panel = new GameObject("PartyPanel");
            _partyPanel = panel;
            panel.transform.SetParent(Root.transform, false);
            Stretch(panel.AddComponent<RectTransform>());
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(PoliSimTheme.Desk.r, PoliSimTheme.Desk.g, PoliSimTheme.Desk.b, 0.94f);
            dim.raycastTarget = true;

            var column = new GameObject("Column");
            column.transform.SetParent(panel.transform, false);
            var columnRect = column.AddComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = Vector2.zero;
            columnRect.sizeDelta = new Vector2(1200f, 800f);
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            // P6-A2: the rows carry a face now, so they are taller than the lines they replace; the spacing
            // comes down to keep the longest chamber inside the column.
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // PS-1 (§618): the panel reads the chamber and the government AT THE COUNTRY'S START - the selector's world is built on the default epoch, so
            // its own country would describe another chamber than the one the game opens on (the review's D4).
            WorldClock.PickerView start = WorldClock.PickerViewOf(country.Id);
            int seats = start.TotalSeats;
            IReadOnlyList<string> cabinet = start.Cabinet;
            bool provisional = start.Provisional;

            CanvasChrome.MakeText(column.transform, "Title", $"CHOOSE YOUR PARTY — {country.Name.ToUpperInvariant()}", PoliSimTheme.Display, 30,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            // K-1f (§607): the key to the cabinet mark is said only when a cabinet is marked - over a chamber that forms none it explained nothing.
            CanvasChrome.MakeText(column.transform, "Subtitle",
                $"THE CHAMBER AS ELECTED · {seats} SEATS · LARGEST FIRST" + (cabinet.Count > 0 ? " · IN THE CABINET = THE CABINET THE CHAMBER FORMS FROM THESE SEATS" : string.Empty),
                PoliSimTheme.Body, 14, PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);
            // K-1 part (4): the day-one government is the model's stand-in until the Riksdag's vote is on record - said where the cabinet is first named.
            if (provisional)
            {
                // K-1f: the formation may form NO government from the seated chamber - it forms none with the declared rules held and the
                // model's hold-out as built (a party votes against while holding out for any admissible cabinet of its own, §607) - the line says which.
                CanvasChrome.MakeText(column.transform, "Provisional", ProvisionalLine(cabinet),
                    PoliSimTheme.Body, 14, PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);
            }

            // P6-A2: every row is the control it is - the delivered brass face under the party's line,
            // not a sentence in interactive ink. The row height is the face's, and the column's spacing
            // is cut to match, so the longest chamber (France's fifteen) still stands inside the column.
            foreach (PoliticalParty party in PartiesBySeats(country.Id, start.Seats))
            {
                if (!PartySystems.IsPlayable(start.Seats, party)) { continue; }   // PS-1 (§618, ruled): seated, or seats at the latest election, or a contested election with a CHES position
                int seatedNow = start.Seats.TryGetValue(party.Abbrev, out int held) ? held : 0;
                Button button = CanvasChrome.FacedButton(column.transform, $"Party_{party.Abbrev}",
                    PartyLine(party, seatedNow, cabinet), PoliSimTheme.Display, 20,   // K-1: the panel's own line carries PROVISIONAL (above); the row stays short
                    PoliSimTheme.TextPrimary, new Vector2(PartyRowWidth, PartyRowHeight));
                // §566 (2026-09-22, Design's sitting part A item 6): THE PARTY'S OWN MARK at the row's left, the delivered `mark_party_*` art the campaign's support
                // plate already draws - the picker is where the player first meets these parties and it showed them as brass strips of text. A party with no mark on
                // disk draws none (PartySystem.MarkName is null where the file does not exist, and inventing one is what PartyMarkCoverageCheck calls an error).
                Texture2D markTexture = IconLibrary.GetPartyMark(party.MarkName);
                if (markTexture != null)
                {
                    Image mark = CanvasChrome.AsAuthoredImage(button.transform, "Mark", CanvasChrome.Whole(markTexture, "mark_" + party.MarkName));
                    mark.raycastTarget = false;
                    mark.preserveAspect = true;
                    RectTransform markRect = mark.rectTransform;
                    markRect.anchorMin = new Vector2(0f, 0.5f);
                    markRect.anchorMax = new Vector2(0f, 0.5f);
                    markRect.pivot = new Vector2(0f, 0.5f);
                    markRect.sizeDelta = new Vector2(PartyMarkSize, PartyMarkSize);
                    markRect.anchoredPosition = new Vector2(PartyMarkInset, 0f);
                }

                LayoutElement rowLayout = button.gameObject.AddComponent<LayoutElement>();
                rowLayout.preferredWidth = PartyRowWidth;
                rowLayout.preferredHeight = PartyRowHeight;
                rowLayout.minHeight = PartyRowHeight;

                string abbrev = party.Abbrev;
                CountryId id = country.Id;
                button.onClick.AddListener(() => { if (onSelect != null) { onSelect(id, abbrev); } });
            }

            Button backButton = CanvasChrome.FacedButton(column.transform, "Back", "BACK TO THE COUNTRIES",
                PoliSimTheme.Display, 14, PoliSimTheme.Hex(0x4A3A22), new Vector2(BackButtonWidth, PartyRowHeight),
                CanvasChrome.Face.Paper);
            LayoutElement backLayout = backButton.gameObject.AddComponent<LayoutElement>();
            backLayout.preferredWidth = BackButtonWidth;
            backLayout.preferredHeight = PartyRowHeight;
            backLayout.minHeight = PartyRowHeight;
            backButton.onClick.AddListener(() => { if (_sheet != null) { ClosePartyPanel(); } else { HidePartyPanel(); } });   // §626 (the review): BACK returns to the step it came from - the start sheet where one is open, else the countries
        }

        /// <summary>CL-2: closes the party panel; 18a: closes the sheet too and restores the folders, so BACK from either step returns to the countries (the harness's pair: ShowStartPanel, then HidePartyPanel).</summary>
        public void HidePartyPanel()
        {
            ClosePartyPanel();
            if (_sheet != null)
            {
                UnityEngine.Object.Destroy(_sheet);
                _sheet = null;
            }
            if (_folderGrid != null) { _folderGrid.SetActive(true); }
        }

        private void ClosePartyPanel()
        {
            if (_partyPanel != null)
            {
                UnityEngine.Object.Destroy(_partyPanel);
                _partyPanel = null;
            }
        }

        /// <summary>
        /// Board 18a (Design, 2026-09-24; built the D11 way): THE START SCREEN IS A SHEET, NOT AN OVERLAY. Choosing a folder OPENS it - the
        /// folder's paper becomes a full-bleed opaque sheet over the selector (no folder type shows through), its tab keeps the flag and the
        /// country's name at the top left under the country's own hue rule, and the other five folders are hidden. On the sheet: the head
        /// (CHOOSE YOUR START · IN DATE ORDER) and a count line; a CARD ROW, one paper card per start point in date order, each with a DATE
        /// STAMP in the stamp register, the mode and opening in caption mono, the election's kind as its name, and THE CHAMBER AS A SEAT BAR
        /// (a presidency draws none; 18b); the BRIEF as a LEDGER of its slots beneath the cards; BACK TO THE COUNTRIES (paper) and ONE brass
        /// SELECT that commits. A card SELECTS - a 3-unit brass spine and a TextPrimary border - and never commits; with one playable card it
        /// opens selected. 18b: a locked card sits in date order at reduced presence (faint border, reduced ink, no hover, no spine, not
        /// selectable) with a LOCKED stamp in the Caution ink where a playable card shows its mode and its reason as one caption line.
        /// Public so the capture driver can film it; <paramref name="onSelect"/> null draws the sheet and seats nobody.
        /// </summary>
        public void ShowStartPanel(Country country, Action<CountryId, string> onSelect)
        {
            HidePartyPanel();
            if (Root == null || country == null) { return; }
            IReadOnlyList<StartPoints.StartPoint> points = StartPoints.For(country.Id);
            int selected = -1;
            for (int i = 0; i < points.Count; i++) { if (points[i].Playable) { selected = i; break; } }
            BuildSheet(country, onSelect, points, selected);
        }

        /// <summary>The sheet, rebuilt whole on every selection (a card row of at most two cards and a six-row ledger - cheaper than keeping the spine, the border and the ledger in step by hand).</summary>
        private void BuildSheet(Country country, Action<CountryId, string> onSelect, IReadOnlyList<StartPoints.StartPoint> points, int selected)
        {
            if (_sheet != null) { UnityEngine.Object.Destroy(_sheet); _sheet = null; }
            if (_folderGrid != null) { _folderGrid.SetActive(false); }   // 18a: the other five folders are hidden while one is open

            Color ink = UiPalette.GetCountryColor(country.Id);

            var sheet = new GameObject("StartSheet");
            _sheet = sheet;
            sheet.transform.SetParent(Root.transform, false);
            Stretch(sheet.AddComponent<RectTransform>());
            Image paper = sheet.AddComponent<Image>();
            paper.color = PoliSimTheme.Card;   // the folder's paper, full-bleed and opaque
            paper.raycastTarget = true;

            // THE TAB: the country's hue rule across the top, the flag and the name at the top left beneath it.
            var rule = new GameObject("HueRule");
            rule.transform.SetParent(sheet.transform, false);
            var ruleRect = rule.AddComponent<RectTransform>();
            ruleRect.anchorMin = new Vector2(0f, 1f);
            ruleRect.anchorMax = new Vector2(1f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 1f);
            ruleRect.offsetMin = new Vector2(0f, -SheetRuleHeight);
            ruleRect.offsetMax = new Vector2(0f, 0f);
            Image ruleImage = rule.AddComponent<Image>();
            ruleImage.color = ink;
            ruleImage.raycastTarget = false;

            var tab = new GameObject("Tab");
            tab.transform.SetParent(sheet.transform, false);
            var tabRect = tab.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0f, 1f);
            tabRect.pivot = new Vector2(0f, 1f);
            tabRect.anchoredPosition = new Vector2(SheetInset, -(SheetRuleHeight + 18f));
            tabRect.sizeDelta = new Vector2(SheetColumnWidth, SheetTabHeight);
            HorizontalLayoutGroup tabLayout = tab.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.spacing = 18f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = false;
            Texture2D flagTexture = IconLibrary.GetFlag(country.Id);
            if (flagTexture != null)
            {
                Image flagImage = CanvasChrome.AsAuthoredImage(tab.transform, "Flag", CanvasChrome.Whole(flagTexture, $"flag_{country.Id}"));
                flagImage.preserveAspect = true;
                LayoutElement flagElement = flagImage.gameObject.AddComponent<LayoutElement>();
                flagElement.preferredWidth = 86f;
                flagElement.preferredHeight = 56f;
            }
            Text name = CanvasChrome.MakeText(tab.transform, "Name", country.Name, PoliSimTheme.Display, 28, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.gameObject.AddComponent<LayoutElement>().minHeight = 36f;

            // THE COLUMN: head, count line, the card row, the ledger, the two controls - left-aligned under the tab.
            var column = new GameObject("Column");
            column.transform.SetParent(sheet.transform, false);
            var columnRect = column.AddComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0f, 1f);
            columnRect.anchorMax = new Vector2(0f, 1f);
            columnRect.pivot = new Vector2(0f, 1f);
            columnRect.anchoredPosition = new Vector2(SheetInset, -(SheetRuleHeight + 18f + SheetTabHeight + 24f));
            columnRect.sizeDelta = new Vector2(SheetColumnWidth, CanvasChrome.ReferenceHeight - (SheetRuleHeight + SheetTabHeight + 80f));
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            Text head = CanvasChrome.MakeText(column.transform, "Head", "CHOOSE YOUR START · IN DATE ORDER", PoliSimTheme.Display, 24, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.gameObject.AddComponent<LayoutElement>().minHeight = 32f;
            int locked = 0;
            foreach (StartPoints.StartPoint p in points) { if (!p.Playable) { locked++; } }
            string count = points.Count == 1 ? "1 START" : points.Count + " STARTS";
            if (locked > 0) { count += " · " + locked + " LOCKED"; }
            Text countLine = CanvasChrome.MakeText(column.transform, "Count", count, PoliSimTheme.Document, 13, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
            countLine.gameObject.AddComponent<LayoutElement>().minHeight = 18f;

            // THE CARD ROW: the empty right half of the row is where a second card goes.
            var row = new GameObject("Cards");
            row.transform.SetParent(column.transform, false);
            row.AddComponent<RectTransform>();
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.spacing = SheetCardGap;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredWidth = SheetColumnWidth;
            rowElement.minHeight = SheetCardHeight;
            for (int i = 0; i < points.Count; i++)
            {
                int index = i;
                BuildStartCard(row.transform, country, points[i], i == selected, points[i].Playable ? () => BuildSheet(country, onSelect, points, index) : (Action)null);
            }

            // THE BRIEF AS A LEDGER of its slots, beneath the cards, for the selected card.
            if (selected >= 0)
            {
                BuildBriefLedger(column.transform, points[selected]);
            }

            // BACK (paper) and ONE brass SELECT that commits. D6's rule on every brass face: the label ink is TextPrimary, never the light paper ink.
            var controls = new GameObject("Controls");
            controls.transform.SetParent(column.transform, false);
            controls.AddComponent<RectTransform>();
            HorizontalLayoutGroup controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.childAlignment = TextAnchor.MiddleLeft;
            controlsLayout.spacing = 18f;
            controlsLayout.childControlWidth = true;
            controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.childForceExpandHeight = false;
            controls.AddComponent<LayoutElement>().minHeight = ControlHeight;

            Button backButton = CanvasChrome.FacedButton(controls.transform, "Back", "BACK TO THE COUNTRIES",
                PoliSimTheme.Display, 14, PoliSimTheme.Hex(0x4A3A22), new Vector2(BackButtonWidth, ControlHeight), CanvasChrome.Face.Paper);
            SizeControl(backButton, BackButtonWidth, ControlHeight);
            backButton.onClick.AddListener(HidePartyPanel);

            Button selectButton = CanvasChrome.FacedButton(controls.transform, "Select", "SELECT",
                PoliSimTheme.Display, 14, PoliSimTheme.TextPrimary, new Vector2(SelectButtonWidth, ControlHeight), CanvasChrome.Face.Brass);
            SizeControl(selectButton, SelectButtonWidth, ControlHeight);
            selectButton.interactable = selected >= 0;
            Country chosen = country;
            selectButton.onClick.AddListener(() => { if (selected >= 0) { ShowPartyPanel(chosen, onSelect); } });
        }

        private static void SizeControl(Button button, float width, float height)
        {
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            element.minWidth = width;
        }

        /// <summary>One start card (18a/18b): paper, a border (TextPrimary when selected, faint when locked), a brass spine when selected; the stamp
        /// head, the name, the seat bar or the locked reason. A playable card selects through <paramref name="onPick"/>; a locked one takes no click.</summary>
        private static void BuildStartCard(Transform parent, Country country, StartPoints.StartPoint point, bool selected, Action onPick)
        {
            bool playable = point.Playable;
            var card = new GameObject($"Start_{point.Kind.Replace(' ', '_')}");
            card.transform.SetParent(parent, false);
            card.AddComponent<RectTransform>();
            Image border = card.AddComponent<Image>();
            border.color = selected ? PoliSimTheme.TextPrimary : (playable ? PoliSimTheme.BorderPaper : PoliSimTheme.EdgeDashed);
            border.raycastTarget = playable;
            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = SheetCardWidth;
            cardElement.minWidth = SheetCardWidth;
            cardElement.minHeight = SheetCardHeight;
            cardElement.preferredHeight = SheetCardHeight;
            if (playable)
            {
                Button button = card.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = border;
                if (onPick != null) { button.onClick.AddListener(() => onPick()); }
            }

            // The paper inside the border (one unit), and the brass spine at the left when selected.
            var face = new GameObject("Paper");
            face.transform.SetParent(card.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(1f, 1f);
            faceRect.offsetMax = new Vector2(-1f, -1f);
            Image faceImage = face.AddComponent<Image>();
            faceImage.color = playable ? PoliSimTheme.Card : PoliSimTheme.Tile;
            faceImage.raycastTarget = false;
            if (selected)
            {
                var spine = new GameObject("Spine");
                spine.transform.SetParent(card.transform, false);
                var spineRect = spine.AddComponent<RectTransform>();
                spineRect.anchorMin = Vector2.zero;
                spineRect.anchorMax = new Vector2(0f, 1f);
                spineRect.pivot = new Vector2(0f, 0.5f);
                spineRect.offsetMin = Vector2.zero;
                spineRect.offsetMax = new Vector2(SpineWidth, 0f);
                Image spineImage = spine.AddComponent<Image>();
                spineImage.color = PoliSimTheme.Brass;
                spineImage.raycastTarget = false;
            }

            var content = new GameObject("Content");
            content.transform.SetParent(card.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(CardPadding + SpineWidth, CardPadding);
            contentRect.offsetMax = new Vector2(-CardPadding, -CardPadding);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Color primary = playable ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;
            Color caption = playable ? PoliSimTheme.TextSecondary : PoliSimTheme.MutedInk;

            // The head: the date stamp, then the mode line (playable) or the LOCKED stamp (18b).
            var headRow = new GameObject("Head");
            headRow.transform.SetParent(content.transform, false);
            headRow.AddComponent<RectTransform>();
            HorizontalLayoutGroup headLayout = headRow.AddComponent<HorizontalLayoutGroup>();
            headLayout.childAlignment = TextAnchor.MiddleLeft;
            headLayout.spacing = 12f;
            headLayout.childControlWidth = true;
            headLayout.childControlHeight = true;
            headLayout.childForceExpandWidth = false;
            headLayout.childForceExpandHeight = false;
            headRow.AddComponent<LayoutElement>().minHeight = StampHeight;
            BuildStamp(headRow.transform, "DateStamp", StartPoints.DateLine(point), primary);
            if (playable)
            {
                Text mode = CanvasChrome.MakeText(headRow.transform, "Mode", StartPoints.ModeLine(point), PoliSimTheme.Document, 12, caption, TextAnchor.MiddleLeft);
                mode.gameObject.AddComponent<LayoutElement>().minHeight = StampHeight;
            }
            else
            {
                BuildStamp(headRow.transform, "LockedStamp", "LOCKED", PoliSimTheme.Caution);
            }
            if (point.DateNote != null)
            {
                // 18b: a date the record does not hold - the provenance as the card's second line.
                Text note = CanvasChrome.MakeText(content.transform, "DateNote", point.DateNote, PoliSimTheme.Document, 12, caption, TextAnchor.MiddleLeft);
                note.gameObject.AddComponent<LayoutElement>().minHeight = 16f;
            }

            Text nameText = CanvasChrome.MakeText(content.transform, "Name", StartPoints.Name(point), PoliSimTheme.Display, 22, primary, TextAnchor.MiddleLeft, FontStyle.Bold);
            nameText.gameObject.AddComponent<LayoutElement>().minHeight = 30f;

            if (playable && !point.Presidential)
            {
                BuildSeatBar(content.transform, country.Id);
            }
            else if (!playable)
            {
                Text reason = CanvasChrome.MakeText(content.transform, "Reason", StartPoints.Reason(point), PoliSimTheme.Document, 12, caption, TextAnchor.MiddleLeft);
                reason.gameObject.AddComponent<LayoutElement>().minHeight = 16f;
            }
        }

        /// <summary>A stamp in the stamp register: a bordered box (≈68 × 20 at 1280) with its text in caption mono.</summary>
        private static void BuildStamp(Transform parent, string name, string text, Color ink)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.AddComponent<RectTransform>();
            Image edge = box.AddComponent<Image>();
            edge.color = ink;
            edge.raycastTarget = false;
            LayoutElement boxElement = box.AddComponent<LayoutElement>();
            boxElement.minWidth = StampWidth;
            boxElement.preferredWidth = StampWidth;
            boxElement.minHeight = StampHeight;
            boxElement.preferredHeight = StampHeight;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(box.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1f, 1f);
            innerRect.offsetMax = new Vector2(-1f, -1f);
            Image innerImage = inner.AddComponent<Image>();
            innerImage.color = PoliSimTheme.Card;
            innerImage.raycastTarget = false;

            Text label = CanvasChrome.MakeText(box.transform, "Label", text, PoliSimTheme.Document, 12, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        /// <summary>18a: THE CHAMBER AS A SEAT BAR - one axis segmented by party in the parties' inks (PoliSimTheme's party accessor, keyed
        /// country/abbreviation; the neutral register where a party has none), largest first, widths proportional to seats, the abbreviations
        /// under the segments that clear 18 device pixels; then the caption THE RIKSDAG OF 11 SEP 2022 · 349.</summary>
        private static void BuildSeatBar(Transform parent, CountryId id)
        {
            WorldClock.PickerView start = WorldClock.PickerViewOf(id);
            List<PoliticalParty> parties = PartiesBySeats(id, start.Seats);
            int total = start.TotalSeats;
            if (total <= 0) { return; }

            float minLabelUnits = 18f / Mathf.Max(0.01f, CanvasChrome.ScaleFactor());
            float barWidth = SheetCardWidth - 2f * CardPadding - SpineWidth;

            var bar = new GameObject("SeatBar");
            bar.transform.SetParent(parent, false);
            bar.AddComponent<RectTransform>();
            HorizontalLayoutGroup barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 1f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = true;
            barLayout.childForceExpandHeight = true;
            LayoutElement barElement = bar.AddComponent<LayoutElement>();
            barElement.minHeight = SeatBarHeight;
            barElement.preferredHeight = SeatBarHeight;

            var labels = new GameObject("SeatLabels");
            labels.transform.SetParent(parent, false);
            labels.AddComponent<RectTransform>();
            HorizontalLayoutGroup labelLayout = labels.AddComponent<HorizontalLayoutGroup>();
            labelLayout.spacing = 1f;
            labelLayout.childControlWidth = true;
            labelLayout.childControlHeight = true;
            labelLayout.childForceExpandWidth = true;
            labelLayout.childForceExpandHeight = true;
            LayoutElement labelsElement = labels.AddComponent<LayoutElement>();
            labelsElement.minHeight = 16f;
            labelsElement.preferredHeight = 16f;

            foreach (PoliticalParty party in parties)
            {
                int seats = Seats(start.Seats, party);
                if (seats <= 0) { continue; }
                float width = barWidth * seats / total;

                var segment = new GameObject($"Seg_{party.Abbrev}");
                segment.transform.SetParent(bar.transform, false);
                segment.AddComponent<RectTransform>();
                Image segmentImage = segment.AddComponent<Image>();
                segmentImage.color = PoliSimTheme.PartyLaddered(id, party.Abbrev);
                segmentImage.raycastTarget = false;
                LayoutElement segmentElement = segment.AddComponent<LayoutElement>();
                segmentElement.flexibleWidth = seats;
                segmentElement.preferredWidth = 0f;

                var cell = new GameObject($"Lbl_{party.Abbrev}");
                cell.transform.SetParent(labels.transform, false);
                cell.AddComponent<RectTransform>();
                LayoutElement cellElement = cell.AddComponent<LayoutElement>();
                cellElement.flexibleWidth = seats;
                cellElement.preferredWidth = 0f;
                if (width >= minLabelUnits)
                {
                    Text label = CanvasChrome.MakeText(cell.transform, "Label", party.ShortName, PoliSimTheme.Document, 10, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    var labelRect = (RectTransform)label.transform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }

            WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(id, WorldClock.StartDate(id));
            DateTime electionDay = WorldClock.ElectionDayOf(id, start.Vintage);
            string when = electionDay != DateTime.MinValue
                ? electionDay.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant()
                : chamber.Convened.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            Text captionText = CanvasChrome.MakeText(parent, "ChamberCaption", $"THE {StartBrief.ChamberOf(id).ToUpperInvariant()} OF {when} · {total}",
                PoliSimTheme.Document, 12, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
            captionText.gameObject.AddComponent<LayoutElement>().minHeight = 16f;
        }

        /// <summary>18a: the brief as a ledger - the head, then one row per slot: the name lane in serif 13, the figure lane bold.</summary>
        private static void BuildBriefLedger(Transform parent, StartPoints.StartPoint point)
        {
            var ledger = new GameObject("Brief");
            ledger.transform.SetParent(parent, false);
            ledger.AddComponent<RectTransform>();
            VerticalLayoutGroup layout = ledger.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ledger.AddComponent<LayoutElement>().preferredWidth = SheetColumnWidth;

            Text head = CanvasChrome.MakeText(ledger.transform, "Head", StartBrief.Head(point), PoliSimTheme.Display, 16, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.gameObject.AddComponent<LayoutElement>().minHeight = 24f;

            foreach (StartBrief.Row row in StartBrief.Rows(point))
            {
                var line = new GameObject("Row_" + row.Name.Replace(' ', '_'));
                line.transform.SetParent(ledger.transform, false);
                line.AddComponent<RectTransform>();
                HorizontalLayoutGroup lineLayout = line.AddComponent<HorizontalLayoutGroup>();
                lineLayout.childAlignment = TextAnchor.MiddleLeft;
                lineLayout.spacing = 12f;
                lineLayout.childControlWidth = true;
                lineLayout.childControlHeight = true;
                lineLayout.childForceExpandWidth = false;
                lineLayout.childForceExpandHeight = false;
                line.AddComponent<LayoutElement>().minHeight = LedgerRowHeight;

                Text nameText = CanvasChrome.MakeText(line.transform, "Name", row.Name, PoliSimTheme.Body, 13, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
                LayoutElement nameElement = nameText.gameObject.AddComponent<LayoutElement>();
                nameElement.preferredWidth = LedgerNameWidth;
                nameElement.minWidth = LedgerNameWidth;
                nameElement.minHeight = LedgerRowHeight;
                Text figureText = CanvasChrome.MakeText(line.transform, "Figure", row.Figure, PoliSimTheme.Display, 14, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
                LayoutElement figureElement = figureText.gameObject.AddComponent<LayoutElement>();
                figureElement.preferredWidth = SheetColumnWidth - LedgerNameWidth - 12f;
                figureElement.minHeight = LedgerRowHeight;
            }

            string tagline = StartBrief.Tagline(point);
            if (!string.IsNullOrEmpty(tagline))
            {
                Text tag = CanvasChrome.MakeText(ledger.transform, "Tagline", tagline, PoliSimTheme.Body, 13, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft, FontStyle.Italic);
                tag.gameObject.AddComponent<LayoutElement>().minHeight = LedgerRowHeight;
            }
        }

        // 18a's measures, in canvas units at the 1920 board basis (a 1280 film scales them by two thirds).
        private const float SheetRuleHeight = 6f;
        private const float SheetInset = 96f;
        private const float SheetTabHeight = 60f;
        private const float SheetColumnWidth = 1260f;
        private const float SheetCardWidth = 618f;
        private const float SheetCardGap = 24f;
        private const float SheetCardHeight = 196f;
        private const float CardPadding = 16f;
        private const float SpineWidth = 3f;
        /// <summary>The stamp register: ≈68 × 20 at 1280, which is 102 × 30 at the board basis.</summary>
        private const float StampWidth = 102f;
        private const float StampHeight = 30f;
        private const float SeatBarHeight = 18f;
        private const float LedgerRowHeight = 22f;
        private const float LedgerNameWidth = 190f;
        private const float ControlHeight = 34f;
        private const float SelectButtonWidth = 200f;

        /// <summary>What a line of the wordmark's type needs, in canvas units - its own size plus the
        /// face's ascent and descent. See the minimums note at the call site.</summary>
        private const float WordmarkMinHeight = 62f;

        /// <summary>See <see cref="WordmarkMinHeight"/>; the subtitle's line at 14 units.</summary>
        private const float SubtitleMinHeight = 18f;

        /// <summary>A party row's face, in canvas units at the board basis: the widest chamber line at 20
        /// units fits inside it, and the height is one line plus the face's padding.</summary>
        private const float PartyRowWidth = 880f;

        /// <summary>See <see cref="PartyRowWidth"/>.</summary>
        private const float PartyRowHeight = 30f;

        /// <summary>§566: the party mark on a picker row - a square the row's own height less its padding, inset from the face's left edge by the same padding.</summary>
        private const float PartyMarkSize = 22f;

        /// <summary>See <see cref="PartyMarkSize"/>.</summary>
        private const float PartyMarkInset = 12f;

        /// <summary>The way back out of the picker: the same height as a row, narrower, and on the paper
        /// face rather than the brass - 6b's own split, where the brass is the committing control and the
        /// paper face carries the navigation.</summary>
        private const float BackButtonWidth = 340f;

        /// <summary>The scenario control's face, in canvas units at the board basis: wide enough for the
        /// longest authored scenario name at 18 units, one line tall plus the face's own padding.</summary>
        private const float ScenarioButtonWidth = 460f;

        /// <summary>See <see cref="ScenarioButtonWidth"/>.</summary>
        private const float ScenarioButtonHeight = 34f;

        /// <summary>
        /// One scenario line, on the chrome's own brass face (P6-A2, 2026-09-17).
        ///
        /// <para>⚠ <b>It used to be a bare sentence.</b> The first form was a `Text` with a `Button` bolted
        /// on and nothing behind it - *"the lightest control this screen can host"*, which is exactly what
        /// playtest 6's finding 2 reports: prose the player cannot tell is a control. The face is the
        /// delivered per-state brass strip the signing plate and election night already wear; no new art is
        /// drawn here.</para>
        /// </summary>
        private static void BuildScenarioLine(Transform parent, ScenarioDefinition definition, Action<ScenarioDefinition> onScenario)
        {
            Button button = CanvasChrome.FacedButton(parent, $"Scenario_{definition.Id}",
                $"Scenario:  {definition.Name}", PoliSimTheme.Display, 18,
                PoliSimTheme.TextPrimary, new Vector2(ScenarioButtonWidth, ScenarioButtonHeight));
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = ScenarioButtonWidth;
            layout.preferredHeight = ScenarioButtonHeight;
            layout.minHeight = ScenarioButtonHeight;

            ScenarioDefinition captured = definition;
            button.onClick.AddListener(() => onScenario(captured));
        }

        private static void BuildFolderCard(Transform parent, Country country, Sprite folder, Action<Country> onOpen)
        {
            UiPalette.SystemArea area = UiPalette.GetCountryArea(country.Id);
            Color ink = UiPalette.GetCountryColor(country.Id);

            var card = new GameObject($"Folder_{country.Id}");
            card.transform.SetParent(parent, false);
            // Not through the tint accessors: the face is the Button's raycast surface
            // (raycastTarget must stay true) and CountryFolderCard drives its hover/press colour.
            Image face = card.AddComponent<Image>();
            face.sprite = folder;
            face.type = Image.Type.Sliced;
            face.pixelsPerUnitMultiplier = 2f; // @2× art at the 1080 reference: slices render @1× thickness

            Button button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            Country opened = country;
            button.onClick.AddListener(() => onOpen(opened));   // CL-2: the folder opens the chamber; the party panel seats the player
            card.AddComponent<CountryFolderCard>();

            // The country hue strip — ui_tab_spine tinted at runtime, per the manifest's own note for
            // this exact card ("country hue strip NOT baked"). WoA, so Image.color IS the tint —
            // the one rendering class where draw-time tinting is correct, same as the IMGUI spine.
            Sprite spine = CanvasChrome.Sliced("ui_tab_spine", 14f, 14f, 0f, 0f);
            if (spine != null)
            {
                // WoA through the tint accessor — the family choice forced at construction.
                Image stripImage = CanvasChrome.TintedImage(card.transform, "HueStrip", spine, ink, sliced: true);
                RectTransform stripRect = stripImage.rectTransform;
                stripRect.anchorMin = new Vector2(0f, 1f);
                stripRect.anchorMax = new Vector2(1f, 1f);
                stripRect.pivot = new Vector2(0.5f, 1f);
                stripRect.offsetMin = new Vector2(26f, 0f);
                stripRect.offsetMax = new Vector2(-26f, 0f);
                stripRect.anchoredPosition = new Vector2(0f, -36f);
                stripRect.sizeDelta = new Vector2(stripRect.sizeDelta.x, 5f);
            }

            // Content column, inset below the baked tab shoulder (top 72 @2× = 36 canvas units).
            var content = new GameObject("Content");
            content.transform.SetParent(card.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(26f, 24f);
            contentRect.offsetMax = new Vector2(-26f, -52f);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            Texture2D flagTexture = IconLibrary.GetFlag(country.Id);
            if (flagTexture != null)
            {
                // Real-colour art through the as-authored accessor — no caller can tint a flag.
                Image flagImage = CanvasChrome.AsAuthoredImage(content.transform, "Flag",
                    CanvasChrome.Whole(flagTexture, $"flag_{country.Id}"));
                LayoutElement flagElement = flagImage.gameObject.AddComponent<LayoutElement>();
                flagElement.preferredWidth = 86f;
                flagElement.preferredHeight = 56f;
                flagImage.preserveAspect = true;
                flagImage.rectTransform.sizeDelta = new Vector2(86f, 56f);
            }

            Text name = CanvasChrome.MakeText(content.transform, "Name", country.Name, PoliSimTheme.Display, 24,
                PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            // §565 (2026-09-22, Design's sitting part A item 5): the card's SUBTITLE, in the country's own ink - the money it keeps its books in. It printed the name of
            // the card's area token (*"HUE: SECTORS"*), which is the palette's word for the ink, not a fact about the country.
            Text zone = CanvasChrome.MakeText(content.transform, "Zone", country.CurrencyZone.Name.ToUpperInvariant(),
                PoliSimTheme.Display, 12, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            zone.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

            // PS-1 (§618): the card says when the world opens for this country and what it opens before - the run-up to its polling day, its
            // snap election's trigger day, or France's governing mode with no election modelled (§8: the selector says so in plain words).
            Text start = CanvasChrome.MakeText(content.transform, "Start", WorldClock.StartLine(country.Id),
                PoliSimTheme.Display, 9, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
            start.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 14f);

            BuildFigureStrip(content.transform, country);
        }

        /// <summary>The three-up figure strip over a hairline rule: population · GDP · debt-to-GDP, live values through the same formatters the IMGUI dashboard uses — B3 holds on Canvas exactly as it does on paper (a money figure never renders without its unit named at the call site).</summary>
        private static void BuildFigureStrip(Transform parent, Country country)
        {
            var rule = new GameObject("Rule");
            rule.transform.SetParent(parent, false);
            Image ruleImage = rule.AddComponent<Image>();
            ruleImage.color = PoliSimTheme.Hex(0xC9BA9B);
            ruleImage.raycastTarget = false;
            rule.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);

            var strip = new GameObject("Figures");
            strip.transform.SetParent(parent, false);
            // A bare GameObject carries only a Transform — the RectTransform must be added before
            // anything sizes it. The pilot's first run threw exactly here (well, at MakeRulePair, the
            // same shape) and the throw became seam defect class 8 below.
            strip.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
            HorizontalLayoutGroup stripLayout = strip.AddComponent<HorizontalLayoutGroup>();
            stripLayout.childAlignment = TextAnchor.UpperLeft;
            stripLayout.childControlWidth = true;
            stripLayout.childControlHeight = true;
            stripLayout.childForceExpandWidth = true;

            EconomyState state = country.State;
            AddFigure(strip.transform, "POPULATION", $"{state.Population:F1}M");
            AddFigure(strip.transform, "GDP", UiFormat.Money(state.GDP, MoneyUnit.Billions));
            AddFigure(strip.transform, "DEBT-TO-GDP", $"{state.DebtToGdpRatio:F0}%");
        }

        private static void AddFigure(Transform parent, string label, string value)
        {
            var cell = new GameObject(label);
            cell.transform.SetParent(parent, false);
            VerticalLayoutGroup cellLayout = cell.AddComponent<VerticalLayoutGroup>();
            cellLayout.childAlignment = TextAnchor.UpperLeft;
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = false;
            cellLayout.spacing = 2f;

            Text labelText = CanvasChrome.MakeText(cell.transform, "Label", label, PoliSimTheme.Display, 9,
                PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
            labelText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 14f);
            Text valueText = CanvasChrome.MakeText(cell.transform, "Value", value, PoliSimTheme.Display, 16,
                PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            valueText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);
        }

        private static void MakeRulePair(Transform parent)
        {
            var pair = new GameObject("RulePair");
            pair.transform.SetParent(parent, false);
            pair.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            HorizontalLayoutGroup layout = pair.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 0f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var rule = new GameObject("Rule");
            rule.transform.SetParent(pair.transform, false);
            Image ruleImage = rule.AddComponent<Image>();
            ruleImage.color = PoliSimTheme.Hex(0x6B5F4A);
            ruleImage.raycastTarget = false;
            rule.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 1f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>§A.14's card states, the interactive half: hover lifts the folder 8 units over 60ms, press settles it to 0.985 scale. Pure transform animation toward targets — no per-state sprites exist for this card, which is deviation V-C2's other half.</summary>
    public class CountryFolderCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float LiftUnits = 8f;
        private const float LiftSeconds = 0.06f;
        private const float PressedScale = 0.985f;

        private Vector2 _restPosition;
        private bool _hasRest;
        private float _lift;
        private float _liftTarget;
        private float _scaleTarget = 1f;

        public void OnPointerEnter(PointerEventData eventData) { _liftTarget = 1f; }
        public void OnPointerExit(PointerEventData eventData) { _liftTarget = 0f; _scaleTarget = 1f; }
        public void OnPointerDown(PointerEventData eventData) { _scaleTarget = PressedScale; }
        public void OnPointerUp(PointerEventData eventData) { _scaleTarget = 1f; }

        private void Update()
        {
            var rect = (RectTransform)transform;
            if (!_hasRest)
            {
                // The grid positions the card on its first layout pass, so the rest position is not
                // knowable at Awake — captured on the first frame it is real.
                if (rect.anchoredPosition == Vector2.zero) { return; }
                _restPosition = rect.anchoredPosition;
                _hasRest = true;
            }

            _lift = Mathf.MoveTowards(_lift, _liftTarget, Time.unscaledDeltaTime / LiftSeconds);
            rect.anchoredPosition = _restPosition + new Vector2(0f, _lift * LiftUnits);
            float scale = Mathf.MoveTowards(transform.localScale.x, _scaleTarget, Time.unscaledDeltaTime * 0.5f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Keeps a RawImage's uvRect tiling at the texture's native pixel size whatever the rect's dimensions — the Canvas equivalent of DrawMenuBackground's DrawTextureWithTexCoords loop, without per-frame draw calls.</summary>
    public class TiledRawImage : MonoBehaviour
    {
        public float TilePixels = 256f;

        private void OnRectTransformDimensionsChange() { Apply(); }
        private void Start() { Apply(); }

        private void Apply()
        {
            RawImage image = GetComponent<RawImage>();
            if (image == null || TilePixels <= 0f) { return; }
            Rect rect = ((RectTransform)transform).rect;
            image.uvRect = new Rect(0f, 0f, rect.width / TilePixels, rect.height / TilePixels);
        }
    }
}
