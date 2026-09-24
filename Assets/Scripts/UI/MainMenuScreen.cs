using System;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>What the main menu's five items mean to the controller. `None` is the menu still up.</summary>
    public enum MainMenuChoice
    {
        None,
        Continue,
        NewGame,
        LoadGame,
        Settings,
        Quit,
    }

    /// <summary>
    /// MM-1 (2026-09-24, `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §9.1): THE GAME'S FIRST SCREEN.
    /// Continue (the most recent save; absent when there is none), New game (the countries), Load game (the
    /// saves screen that exists), Settings, Quit. The title and the desk's own materials; no text beyond the
    /// items and the title. A Canvas surface on the selector's own ground (`CountrySelectorScreen` is the
    /// pattern: the desk colour under the menu tile, the wordmark over its rule pair), its items the faced
    /// buttons every Canvas control wears (`CanvasChrome.FacedButton`, P6-A2). The seam that shows and hides
    /// it is `GameController`'s takeover machine, never this class - a screen owns its content, the controller
    /// owns the boundary.
    ///
    /// <para>⚠ The controller applies a choice only after the cover is over the menu (its CoverOut step), so
    /// a click here never changes the game under a live Canvas; this class only reports the click.</para>
    ///
    /// <para>BOARD 17a (Design, 2026-09-24, §626 - read against the frame mm1_1280_00_main_menu), built one
    /// for one: (1) the wordmark sits where the SELECTOR's sits and at its size - the 54-unit face over its
    /// 280-unit hairline in a top-anchored title block (centre y ≈ 44 canvas units; 38 px and a 185-px rule at
    /// 1280), not the 84-unit wordmark 150 px lower; the items keep the build's measures (460 × 64, 14 apart)
    /// centred where they are. (2) ONE brass face: brass goes to the DEFAULT item only - CONTINUE when a save
    /// exists, NEW GAME when none; every other item wears the paper face. QUIT stays paper but stands apart
    /// behind a hairline and one extra pitch (a rule plus an extra 14-unit gap above it) - it is the only item
    /// that leaves the desk. (3) D6: on the brass face the label ink is <see cref="PoliSimTheme.TextPrimary"/>
    /// (0x2B2620) bold, never the light 0xF0E7D8; paper faces keep 0x4A3A22. (4) The "no save" case: the
    /// column shrinks by one item and stays centred; NEW GAME takes the brass.</para>
    ///
    /// <para>REFUSED by 17a and NOT built: a caption under CONTINUE naming the save. Nothing else on the
    /// board is deviated from.</para>
    /// </summary>
    public class MainMenuScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>The item column's measures, in canvas units at the board basis (1920×1080).</summary>
        private const float ItemWidth = 460f;
        private const float ItemHeight = 64f;
        private const float ItemSpacing = 14f;

        /// <summary>17a: the selector's wordmark measures, reproduced (CountrySelectorScreen: face 54, a line
        /// of that type needs 62 units, the hairline 280 × 1 in 0x6B5F4A, the block 40 units under the top).</summary>
        private const int WordmarkSize = 54;
        private const float WordmarkMinHeight = 62f;
        private const float WordmarkRuleWidth = 280f;
        private const float TitleTop = 40f;

        /// <summary>
        /// Build the menu under the shared host. <paramref name="hasContinue"/> decides whether CONTINUE is
        /// drawn at all - the spec's *absent when there is none*, not disabled. Returns null only when the
        /// controller gives it nothing to call, which is the caller's own error; a missing sprite degrades
        /// inside the faced button and costs the look, never the door.
        /// </summary>
        public static MainMenuScreen Build(bool hasContinue, Action<MainMenuChoice> onChoose)
        {
            if (onChoose == null)
            {
                Debug.LogWarning("CANVAS: the main menu was asked for with nothing to call - not built.");
                return null;
            }

            Canvas canvas = CanvasChrome.EnsureHost();
            var screen = new MainMenuScreen();

            var root = new GameObject("MainMenu");
            screen.Root = root;
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.AddComponent<RectTransform>());

            // The ground: the selector's own - desk colour under the menu tile.
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

            // 17a (1): the title block where the SELECTOR keeps its own - top-anchored, 40 units under the
            // top, the wordmark at the selector's 54 over the selector's 280-unit hairline. The same
            // measures as CountrySelectorScreen's title block, so the two screens share one wordmark
            // placement (centre y ≈ 44 canvas units; 38 px and a 185-px rule at 1280).
            var title = new GameObject("Title");
            title.transform.SetParent(root.transform, false);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -TitleTop);
            titleRect.sizeDelta = new Vector2(0f, WordmarkMinHeight + 8f + 1f);
            VerticalLayoutGroup titleLayout = title.AddComponent<VerticalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.UpperCenter;
            titleLayout.spacing = 8f;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            titleLayout.childForceExpandWidth = false;

            // ⚠ THE MINIMUMS ARE LOAD-BEARING (P6-A2): a vertical layout group that cannot fit its children
            // shrinks the ones with no minimum toward zero, and a Text under its own line height is clipped.
            Text wordmark = CanvasChrome.MakeText(title.transform, "Wordmark", "PoliSim", PoliSimTheme.Display, WordmarkSize,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            LayoutElement wordmarkLayout = wordmark.gameObject.AddComponent<LayoutElement>();
            wordmarkLayout.minHeight = WordmarkMinHeight;
            wordmarkLayout.preferredHeight = WordmarkMinHeight;
            MakeRule(title.transform, WordmarkRuleWidth, PoliSimTheme.Hex(0x6B5F4A));

            // One centred column of items. The column is anchored to the centre and sized to its content,
            // so the geometry the scaler picks moves nothing; with no save it is one item shorter and
            // stays centred (17a, the "no save" case).
            var column = new GameObject("Column");
            column.transform.SetParent(root.transform, false);
            var columnRect = column.AddComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(0f, 20f);
            columnRect.sizeDelta = new Vector2(ItemWidth, 460f);
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = ItemSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // 17a (2): ONE brass face - the default item. CONTINUE when a save exists, NEW GAME when none.
            if (hasContinue)
            {
                MakeItem(column.transform, "Continue", "CONTINUE", MainMenuChoice.Continue, onChoose, CanvasChrome.Face.Brass);
            }

            MakeItem(column.transform, "NewGame", "NEW GAME", MainMenuChoice.NewGame, onChoose,
                hasContinue ? CanvasChrome.Face.Paper : CanvasChrome.Face.Brass);
            MakeItem(column.transform, "LoadGame", "LOAD GAME", MainMenuChoice.LoadGame, onChoose, CanvasChrome.Face.Paper);
            MakeItem(column.transform, "Settings", "SETTINGS", MainMenuChoice.Settings, onChoose, CanvasChrome.Face.Paper);

            // 17a (2): QUIT stands apart - a hairline and one extra pitch above it (the layout's own 14
            // on either side of the rule, plus this 14-unit gap), because it is the only item that
            // leaves the desk. Paper, like every non-default item.
            MakeRule(column.transform, ItemWidth, PoliSimTheme.Hex(0x6B5F4A));
            MakeGap(column.transform, ItemSpacing);
            MakeItem(column.transform, "Quit", "QUIT", MainMenuChoice.Quit, onChoose, CanvasChrome.Face.Paper);

            // 17a (4), REFUSED by the board and NOT built: no caption under CONTINUE naming the save.

            // S-20: the capture-identity token, so a film of this screen proves it is this screen.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "menu";
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

        /// <summary>One item. 17a (3), D6's rule: on the brass face the label is TextPrimary (0x2B2620) bold,
        /// never the light 0xF0E7D8; a paper face keeps 0x4A3A22. Every item is bold (FacedButton's default).</summary>
        private static void MakeItem(Transform parent, string name, string label, MainMenuChoice choice,
            Action<MainMenuChoice> onChoose, CanvasChrome.Face face)
        {
            bool brass = face == CanvasChrome.Face.Brass;
            Button button = CanvasChrome.FacedButton(parent, name, label, PoliSimTheme.Display, 22,
                brass ? PoliSimTheme.TextPrimary : PoliSimTheme.Hex(0x4A3A22), new Vector2(ItemWidth, ItemHeight), face, FontStyle.Bold);
            LayoutElement itemLayout = button.gameObject.AddComponent<LayoutElement>();
            itemLayout.preferredWidth = ItemWidth;
            itemLayout.preferredHeight = ItemHeight;
            itemLayout.minHeight = ItemHeight;
            button.onClick.AddListener(() => onChoose(choice));
        }

        /// <summary>A one-unit hairline of the given width in the given ink, as a layout child.</summary>
        private static void MakeRule(Transform parent, float width, Color ink)
        {
            var rule = new GameObject("Rule");
            rule.transform.SetParent(parent, false);
            Image ruleImage = rule.AddComponent<Image>();
            ruleImage.color = ink;
            ruleImage.raycastTarget = false;
            LayoutElement ruleLayout = rule.AddComponent<LayoutElement>();
            ruleLayout.preferredWidth = width;
            ruleLayout.preferredHeight = 1f;
            ruleLayout.minHeight = 1f;
        }

        private static void MakeGap(Transform parent, float height)
        {
            var gap = new GameObject("Gap");
            gap.transform.SetParent(parent, false);
            gap.AddComponent<RectTransform>();
            LayoutElement gapLayout = gap.AddComponent<LayoutElement>();
            gapLayout.preferredHeight = height;
            gapLayout.minHeight = height;
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
