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
    ///
    /// <para>⚠ <b>What that decision cost, measured at P6-A1 (2026-09-17, playtest 6's finding 1).</b> A
    /// document composed at the basis is drawn through a FRACTIONAL factor at every geometry this project
    /// films, and two things followed that the charter above did not foresee: a glyph quad landed on
    /// fractional device pixels and was filtered across two columns, and the authored type sizes had no
    /// floor, so the selector card's smallest labels were asked for at under six device pixels. The
    /// composition stays at the basis - that is Design's - and the two mechanical consequences are answered
    /// here instead: <see cref="Canvas.pixelPerfect"/> on the host, and <see cref="MinDeviceTextPx"/>
    /// enforced in <see cref="MakeText"/>. ⚠ The floor is a legibility BOUND, not a design: what the card
    /// should show at the smallest geometry is a composition question and it goes to Design.</para>
    /// </summary>
    public static class CanvasChrome
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private static Canvas _canvas;

        /// <summary>The board basis the Canvas screens are composed at. ⚠ Declared once because
        /// <see cref="ScaleFactor"/> has to reproduce the scaler's arithmetic to know what a canvas unit is
        /// worth in device pixels.</summary>
        public const float ReferenceWidth = 1920f;

        /// <summary>See <see cref="ReferenceWidth"/>.</summary>
        public const float ReferenceHeight = 1080f;

        /// <summary>See <see cref="ReferenceWidth"/>. 0.5 weights width and height equally, so the factor is
        /// the geometric mean of the two ratios.</summary>
        public const float ScalerMatch = 0.5f;

        /// <summary>
        /// ⚠ **THE SMALLEST TYPE A CANVAS SCREEN MAY RENDER, IN DEVICE PIXELS — the desk's own caption
        /// floor, not a new number** (`GameController.Desk.cs` holds every IMGUI caption to it).
        ///
        /// <para><b>Why it exists (P6-A1, 2026-09-17).</b> Canvas type is authored in canvas units at the
        /// board basis and multiplied by <see cref="ScaleFactor"/> at draw time, and nothing floored the
        /// product. At the smallest geometry this project films, the selector card's figure labels were
        /// asked for at under six device pixels and its hue line at under eight - below anything the IMGUI
        /// screens draw - and the letterforms collapsed into each other. The floor is applied where the
        /// text is made, so every Canvas screen inherits it.</para>
        /// </summary>
        private const int MinDeviceTextPx = 9;

        /// <summary>
        /// What one canvas unit is worth in device pixels right now — `CanvasScaler`'s own arithmetic for
        /// `ScaleWithScreenSize` with `MatchWidthOrHeight`, reproduced here because the scaler does not
        /// publish its factor until its first update and <see cref="MakeText"/> runs while a screen is
        /// being built. ⚠ It reads `Screen`, not `UiScreen`: the Canvas is scaled by the real backbuffer,
        /// and the editor-only override exists for the IMGUI seam.
        /// </summary>
        public static float ScaleFactor()
        {
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            float logWidth = Mathf.Log(w / ReferenceWidth, 2f);
            float logHeight = Mathf.Log(h / ReferenceHeight, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, ScalerMatch));
        }

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
            // ⚠ P6-A1 (2026-09-17): SNAP EVERY GRAPHIC TO A DEVICE PIXEL. The scaler below puts these
            // screens on a fractional factor at every geometry this project films, so a glyph quad landed
            // on fractional pixels and was filtered across two columns. `pixelPerfect` rounds each
            // graphic's vertices to whole device pixels - what the IMGUI screens get for free by drawing
            // in device pixels to begin with.
            _canvas.pixelPerfect = true;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = ScalerMatch;

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

        /// <summary>Which delivered face a Canvas control wears. The strips are Design's, delivered per state
        /// (`ui_btn_brass_canvas` / `ui_btn_paper_canvas`, each with `_hover` and `_pressed`).</summary>
        public enum Face
        {
            /// <summary>Brass: the screen's own interactive ink - what the signing plate and election night already wear.</summary>
            Brass,

            /// <summary>Paper: a control that sits on the desk's paper rather than over it.</summary>
            Paper,
        }

        /// <summary>
        /// **THE CANVAS FACED BUTTON, in one place (P6-A2, 2026-09-17).** A sliced face from the delivered
        /// per-state strips, `SpriteSwap` between them, and a centred label stretched over it - the pattern
        /// `SigningScreen` and `ElectionNightScreen` each carried inline before this existed.
        ///
        /// <para>⚠ <b>Why it is a seam and not a convenience.</b> Playtest 6's finding 2 is that controls
        /// draw as prose: the selector's scenario lines, the picker's party rows and its way back were
        /// `Text` with a `Button` bolted on and nothing behind them, so nothing said they could be
        /// clicked. A control built here cannot be that by accident - the face is the first argument
        /// the caller cannot omit.</para>
        ///
        /// <para>⚠ <b>Degradation is the standing one.</b> A missing strip leaves the face's fill colour
        /// rather than white or nothing: a control still reads as a control when its art is absent.</para>
        /// </summary>
        public static Button FacedButton(Transform parent, string name, string label, Font font, int size,
            Color ink, Vector2 sizeDelta, Face face = Face.Brass, FontStyle style = FontStyle.Bold)
        {
            // ⚠ THE SIX NAMES ARE WRITTEN OUT, and that is not verbosity. The delivered-asset inventory and
            // the coverage checks read the SOURCE for the names an asset is reached by; the first form of
            // this method built them as `stem + "_hover"`, and the regenerated inventory immediately
            // reported `ui_btn_brass_canvas_hover` and `_pressed` as held-but-unreached while the running
            // game was drawing them. A name assembled at runtime is a name no census can see.
            bool brass = face == Face.Brass;
            Sprite normal = Sliced(brass ? "ui_btn_brass_canvas" : "ui_btn_paper_canvas", 24f, 24f, 24f, 24f);
            Sprite hover = Sliced(brass ? "ui_btn_brass_canvas_hover" : "ui_btn_paper_canvas_hover", 24f, 24f, 24f, 24f);
            Sprite pressed = Sliced(brass ? "ui_btn_brass_canvas_pressed" : "ui_btn_paper_canvas_pressed", 24f, 24f, 24f, 24f);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = sizeDelta;

            // Not through the tint accessors: a Button face keeps raycastTarget true, and the missing-sprite
            // degradation needs the face's own fill rather than a locked white.
            Image faceImage = go.AddComponent<Image>();
            if (normal != null)
            {
                faceImage.sprite = normal;
                faceImage.type = Image.Type.Sliced;
                faceImage.pixelsPerUnitMultiplier = 2f;
            }
            else
            {
                faceImage.color = face == Face.Brass ? PoliSimTheme.Hex(0x8A6B2F) : PoliSimTheme.Hex(0xD9CBAC);
            }

            Button control = go.AddComponent<Button>();
            control.targetGraphic = faceImage;
            if (normal != null && hover != null && pressed != null)
            {
                control.transition = Selectable.Transition.SpriteSwap;
                control.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = pressed };
            }

            Text text = MakeText(go.transform, "Label", label, font, size, ink, TextAnchor.MiddleCenter, style);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return control;
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
            // ⚠ P6-A1: the authored size is in CANVAS UNITS and the product with the scaler's factor is what
            // gets rasterised, so the floor is applied to that product, in device pixels, and the authored
            // size raised until it clears. `CeilToInt` rather than rounding: rounding down would land back
            // under the floor. Vertical overflow is opened at the same time - a floored line is taller than
            // the rect its caller sized in canvas units, and `Truncate` would drop the line rather than
            // show it, which is the one outcome worse than small type.
            int canvasSize = Mathf.Max(1, size);
            float scale = ScaleFactor();
            if (scale > 0f && canvasSize * scale < MinDeviceTextPx)
            {
                canvasSize = Mathf.Max(canvasSize, Mathf.CeilToInt(MinDeviceTextPx / scale));
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }

            text.fontSize = canvasSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }
    }
}
