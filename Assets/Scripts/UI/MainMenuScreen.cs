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
    /// </summary>
    public class MainMenuScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>The item column's measures, in canvas units at the board basis (1920×1080).</summary>
        private const float ItemWidth = 460f;
        private const float ItemHeight = 64f;
        private const float ItemSpacing = 14f;
        private const float WordmarkMinHeight = 96f;

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

            // One centred column: the wordmark, its rule, the items. The column is anchored to the
            // centre and sized to its content, so the geometry the scaler picks moves nothing.
            var column = new GameObject("Column");
            column.transform.SetParent(root.transform, false);
            var columnRect = column.AddComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(0f, 20f);
            columnRect.sizeDelta = new Vector2(ItemWidth, 620f);
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = ItemSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // ⚠ THE MINIMUMS ARE LOAD-BEARING (P6-A2): a vertical layout group that cannot fit its children
            // shrinks the ones with no minimum toward zero, and a Text under its own line height is clipped.
            Text wordmark = CanvasChrome.MakeText(column.transform, "Wordmark", "PoliSim", PoliSimTheme.Display, 84,
                PoliSimTheme.Hex(0xE8DDC4), TextAnchor.MiddleCenter, FontStyle.Bold);
            LayoutElement wordmarkLayout = wordmark.gameObject.AddComponent<LayoutElement>();
            wordmarkLayout.minHeight = WordmarkMinHeight;
            wordmarkLayout.preferredHeight = WordmarkMinHeight;
            MakeRule(column.transform);
            MakeGap(column.transform, 18f);

            if (hasContinue)
            {
                MakeItem(column.transform, "Continue", "CONTINUE", MainMenuChoice.Continue, onChoose);
            }

            MakeItem(column.transform, "NewGame", "NEW GAME", MainMenuChoice.NewGame, onChoose);
            MakeItem(column.transform, "LoadGame", "LOAD GAME", MainMenuChoice.LoadGame, onChoose);
            MakeItem(column.transform, "Settings", "SETTINGS", MainMenuChoice.Settings, onChoose);
            MakeItem(column.transform, "Quit", "QUIT", MainMenuChoice.Quit, onChoose, CanvasChrome.Face.Paper);

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

        private static void MakeItem(Transform parent, string name, string label, MainMenuChoice choice,
            Action<MainMenuChoice> onChoose, CanvasChrome.Face face = CanvasChrome.Face.Brass)
        {
            bool brass = face == CanvasChrome.Face.Brass;
            Button button = CanvasChrome.FacedButton(parent, name, label, PoliSimTheme.Display, 22,
                brass ? PoliSimTheme.Hex(0xF0E7D8) : PoliSimTheme.Hex(0x4A3A22), new Vector2(ItemWidth, ItemHeight), face);
            LayoutElement itemLayout = button.gameObject.AddComponent<LayoutElement>();
            itemLayout.preferredWidth = ItemWidth;
            itemLayout.preferredHeight = ItemHeight;
            itemLayout.minHeight = ItemHeight;
            button.onClick.AddListener(() => onChoose(choice));
        }

        private static void MakeRule(Transform parent)
        {
            var rule = new GameObject("Rule");
            rule.transform.SetParent(parent, false);
            Image ruleImage = rule.AddComponent<Image>();
            ruleImage.color = PoliSimTheme.Hex(0x6B5F4A);
            ruleImage.raycastTarget = false;
            LayoutElement ruleLayout = rule.AddComponent<LayoutElement>();
            ruleLayout.preferredWidth = 280f;
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
