using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// CANVAS PILOT (2026-08-12) — the Canvas path's equivalents of the two mechanisms every IMGUI
    /// screen leans on: <c>IconLibrary.GetChrome</c> for pixels and <c>GUIStyle.border</c> for
    /// slicing, plus the one-time host (Canvas + scaler + EventSystem) every Canvas screen shares.
    /// Everything is built FROM CODE — no scene edits — so the Canvas path stays reviewable in a
    /// diff the same way the procedural IMGUI always has been.
    ///
    /// ⚠ **THE BORDER-ORDER TRAP, THIRD CONVENTION, resolved in exactly one place rather than
    /// sidestepped.** `Sprite.Create`'s border Vector4 is **X=left, Y=bottom, Z=right, W=top** — a
    /// third ordering beside `GUIStyle.border`'s `RectOffset(l, r, t, b)` and the manifest's
    /// "L/R/T/B @2×". The IMGUI chrome pass sidestepped `GUI.DrawTexture`'s Vector4 by drawing
    /// through styles; a Canvas `Image` cannot sidestep it, so <see cref="Sliced"/> takes the
    /// manifest's own order and performs the mapping here and nowhere else. **Borders are given in
    /// @2× TEXTURE pixels — the manifest's numbers pass through UNHALVED**, unlike `GUIStyle.border`
    /// which is @1×; `Image.pixelsPerUnitMultiplier` is what controls on-screen slice thickness.
    ///
    /// <para><b>Scaler decision, recorded for the seven screens that will copy it:</b> reference
    /// resolution **1920×1080, match 0.5** — the board basis, so §A.14's px figures are usable as
    /// canvas units directly. This differs from IMGUI's `Screen.height`-fraction scaling on purpose:
    /// Canvas screens are documents composed at a reference size, not furniture re-derived per
    /// resolution.</para>
    /// </summary>
    public static class CanvasChrome
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private static Canvas _canvas;

        /// <summary>The shared screen-space Canvas, created on first use. ScreenSpaceOverlay — which the render-order spike measured as still BELOW IMGUI, which is the whole seam: a Canvas screen is visible exactly when OnGUI suppresses itself.</summary>
        public static Canvas EnsureHost()
        {
            if (_canvas != null)
            {
                return _canvas;
            }

            var root = new GameObject("CanvasHost");
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // The project runs the new Input System exclusively (activeInputHandler: 1), so the
            // legacy StandaloneInputModule would be inert — InputSystemUIInputModule or nothing.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            return _canvas;
        }

        /// <summary>
        /// A 9-sliced sprite from a chrome texture. <paramref name="left"/>/<paramref name="right"/>/
        /// <paramref name="top"/>/<paramref name="bottom"/> are the MANIFEST's own order and scale:
        /// per-edge insets in @2× texture pixels, quoted straight from the delivery table. Null when
        /// the texture is missing — callers degrade per the standing IconLibrary contract.
        /// </summary>
        public static Sprite Sliced(string chromeName, float left, float right, float top, float bottom)
        {
            string key = chromeName + "#sliced";
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = IconLibrary.GetChrome(chromeName);
            if (texture == null)
            {
                return null;
            }

            // Sprite.Create border: X=left, Y=bottom, Z=right, W=top. The one mapping site.
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(left, bottom, right, top));
            SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>An unsliced sprite from any texture (flags, WoA strips drawn whole). Cached per texture name.</summary>
        public static Sprite Whole(Texture2D texture, string cacheKey)
        {
            if (texture == null)
            {
                return null;
            }

            if (SpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// ⚠ THE TINT-FAMILY ACCESSORS (2026-08-12, ruled after the class's FIFTH visit — the WoA
        /// masthead seal printing white on paper, caught by eye). These are the Canvas answer to the
        /// question the IMGUI accessors answer: every chrome Image is constructed through one of the
        /// two, so the family choice is FORCED at the call site instead of defaulting to white and
        /// waiting for an eye. `TintedImage` for WoA art (the ink is a required argument — ink
        /// weights on paper, lifted weights on the desk, per §3.0a); `AsAuthoredImage` for
        /// real-colour art (flags, seals-official, the scrim, ornate frames), where the colour is
        /// LOCKED to white and no caller can accidentally tint. The sixth instance should need a
        /// compiler error, not an eye.
        /// </summary>
        public static Image TintedImage(Transform parent, string name, Sprite sprite, Color ink, bool sliced = false)
        {
            Image image = MakeImage(parent, name, sprite, sliced);
            image.color = ink;
            return image;
        }

        /// <summary>Real-colour art, drawn exactly as authored — see <see cref="TintedImage"/>.</summary>
        public static Image AsAuthoredImage(Transform parent, string name, Sprite sprite, bool sliced = false)
        {
            Image image = MakeImage(parent, name, sprite, sliced);
            image.color = Color.white;
            return image;
        }

        private static Image MakeImage(Transform parent, string name, Sprite sprite, bool sliced)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2f;
            }

            return image;
        }

        /// <summary>Legacy-uGUI Text, deliberately: the pilot's PATTERN decision, recorded in its charter — the fonts already load as `Font` assets through PoliSimTheme, TMP would need font-asset generation, and every pattern the pilot exists to prove (host, slicing, states, the seam) is orthogonal to the text backend. Revisit when a Canvas screen needs masks/outline/per-character effects.</summary>
        public static Text MakeText(Transform parent, string name, string content, Font font, int size, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.text = content;
            if (font != null) { text.font = font; }
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }
    }
}
