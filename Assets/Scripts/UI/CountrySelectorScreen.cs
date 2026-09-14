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
            titleRect.anchoredPosition = new Vector2(0f, -56f);
            titleRect.sizeDelta = new Vector2(0f, 140f);
            VerticalLayoutGroup titleLayout = title.AddComponent<VerticalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.UpperCenter;
            titleLayout.spacing = 8f;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;

            CanvasChrome.MakeText(title.transform, "Wordmark", "PoliSim", PoliSimTheme.Display, 54,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            MakeRulePair(title.transform);
            CanvasChrome.MakeText(title.transform, "Subtitle", "Choose your country", PoliSimTheme.Body, 14,
                PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);

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
                BuildFolderCard(grid.transform, country, folder, opened => screen.ShowPartyPanel(opened, onSelect));
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
        public static List<PoliticalParty> PartiesBySeats(CountryId id)
        {
            var list = new List<PoliticalParty>(PartySystems.For(id));
            list.Sort((a, b) => b.SeedSeats.CompareTo(a.SeedSeats));
            return list;
        }

        /// <summary>One party's line on the picker: its abbreviation, its name as published, its seats at the last real election, and IN THE CABINET when the chamber's own formation seats it (`GovernmentFormation.Cabinet`).</summary>
        public static string PartyLine(PoliticalParty party, IReadOnlyList<string> cabinet)
        {
            bool inCabinet = false;
            if (cabinet != null) { foreach (string abbrev in cabinet) { if (abbrev == party.Abbrev) { inCabinet = true; break; } } }
            return $"{party.Abbrev} — {party.Name} · {party.SeedSeats} SEATS{(inCabinet ? " · IN THE CABINET" : "")}";
        }

        /// <summary>
        /// CL-2: the party panel - the selector's second step. Over the folders (a dim ground that takes the click, so a folder beneath
        /// cannot), the country's chamber as elected: one text line per party, largest first, its seats and whether the chamber's own
        /// formation seats it in the cabinet; a party seats the player and starts the game, BACK returns to the folders. Public so the
        /// capture driver can film it; <paramref name="onSelect"/> null draws the panel and seats nobody.
        /// </summary>
        public void ShowPartyPanel(Country country, Action<CountryId, string> onSelect)
        {
            HidePartyPanel();
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
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            int seats = 0;
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { seats += party.SeedSeats; }
            IReadOnlyList<string> cabinet = GovernmentFormation.Cabinet(country);

            CanvasChrome.MakeText(column.transform, "Title", $"CHOOSE YOUR PARTY — {country.Name.ToUpperInvariant()}", PoliSimTheme.Display, 30,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            CanvasChrome.MakeText(column.transform, "Subtitle",
                $"THE CHAMBER AS ELECTED · {seats} SEATS · LARGEST FIRST · IN THE CABINET = THE CABINET THE CHAMBER FORMS FROM THESE SEATS",
                PoliSimTheme.Body, 14, PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);

            foreach (PoliticalParty party in PartiesBySeats(country.Id))
            {
                Text line = CanvasChrome.MakeText(column.transform, $"Party_{party.Abbrev}", PartyLine(party, cabinet), PoliSimTheme.Display, 20,
                    PoliSimTheme.Hex(0xC8A24A), TextAnchor.MiddleCenter);
                line.raycastTarget = true;
                Button button = line.gameObject.AddComponent<Button>();
                button.targetGraphic = line;
                string abbrev = party.Abbrev;
                CountryId id = country.Id;
                button.onClick.AddListener(() => { if (onSelect != null) { onSelect(id, abbrev); } });
            }

            Text back = CanvasChrome.MakeText(column.transform, "Back", "BACK TO THE COUNTRIES", PoliSimTheme.Display, 14,
                PoliSimTheme.Hex(0xB7A98C), TextAnchor.MiddleCenter);
            back.raycastTarget = true;
            Button backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = back;
            backButton.onClick.AddListener(HidePartyPanel);
        }

        /// <summary>CL-2: closes the party panel; the folders take the click again.</summary>
        public void HidePartyPanel()
        {
            if (_partyPanel != null)
            {
                UnityEngine.Object.Destroy(_partyPanel);
                _partyPanel = null;
            }
        }

        /// <summary>One scenario line: a text button in the brass ink the screen already uses for
        /// interactive type, with no new art. `Text` carries its own raycast target, so the Button
        /// needs no separate face image — the lightest control this screen can host.</summary>
        private static void BuildScenarioLine(Transform parent, ScenarioDefinition definition, Action<ScenarioDefinition> onScenario)
        {
            Text label = CanvasChrome.MakeText(parent, $"Scenario_{definition.Id}",
                $"Scenario:  {definition.Name}", PoliSimTheme.Display, 18,
                PoliSimTheme.Hex(0xC8A24A), TextAnchor.MiddleCenter);
            label.raycastTarget = true;

            Button button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
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

            Text hue = CanvasChrome.MakeText(content.transform, "Hue", $"HUE: {DisplayName.Spaced(area.ToString()).ToUpperInvariant()}",
                PoliSimTheme.Display, 12, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            hue.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

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
