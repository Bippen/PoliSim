using System;
using PoliSim.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// CANVAS SCREEN 2 (2026-08-12) — the SIGNING ceremony per §A.14 (board 1g), chosen as the
    /// selector's nearest neighbour: the same full-screen-takeover shape, a document over the scrim
    /// wash, no IMGUI compositing mid-screen. Built entirely on the pilot's recorded patterns:
    /// `CanvasChrome.Sliced` for everything sliced, `Image.color` tinting through the family logic
    /// (the verdict stamp takes INK weights on paper, exactly as the Parliament panel does), layout
    /// components rather than fixed rects wherever text can vary.
    ///
    /// **Patterns this screen ADDS to the discipline statement (deliberate, not improvised):**
    /// - The canvas brass button: uGUI `Button` with `SpriteSwap` targeting the delivered per-state
    ///   strips (`ui_btn_brass_canvas` / `_hover` / `_pressed`) — the Canvas analogue of
    ///   `UiPalette.BuildButtonStyle`'s state faces.
    /// - The diverging lean bar as two Images (track + centred fill) — retained-mode's version of
    ///   `UiPalette.DrawDivergingBar`; a widget, not a per-frame draw.
    /// - `ui_frame_ornate` as a border-only sliced Image (`fillCenter = false`) — the B-ruled
    ///   Canvas-path use it was reserved for.
    /// - `ui_scrim_takeover` as the CANVAS-SIDE ground (its second call site): the wash lives under
    ///   the document, which is what lets the IMGUI cover fade away without the wash disappearing.
    ///
    /// **Declared deviations (V-S series), per the boards-deviation practice:**
    /// - V-S1: paper is a flat `#F2EADB` and the drop shadow is a single dark plate — the CSS
    ///   gradient and double shadow have no delivered sprite (`ui_shadow_soft` does not exist; the
    ///   name was checked against disk before use, per the absence guard).
    /// - V-S2: the two-column bill-figure grid is OMITTED — `DivisionRecord` does not carry bill
    ///   figures, and the enrichment-at-write-time scoping (see the election-night scoping record)
    ///   already owns that gap. The document shows what the record honestly holds.
    /// - V-S3: the pen-scratch beat and office/presentation copy are absent — no audio asset, no
    ///   authored copy; spec slots without copy stay EMPTY rather than invented (the V-C1 precedent).
    /// </summary>
    public class SigningScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>True once the seal has landed and settled — the seam watches this to begin CoverOut, the same watch-the-result idiom as the selector's selection.</summary>
        public bool Sealed => _seal != null && _seal.Settled;

        private SealDrop _seal;

        /// <summary>Null when the document furniture is missing — the caller drops the ceremony and the resolution stays silent, which is exactly today's behaviour (degradation costs the ceremony, never correctness).</summary>
        public static SigningScreen Build(Country country, DivisionRecord record, Action onSign)
        {
            Sprite frame = CanvasChrome.Sliced("ui_frame_ornate", 64f, 64f, 64f, 64f);
            Texture2D scrimTexture = IconLibrary.GetChrome("ui_scrim_takeover");
            if (frame == null || country == null || record == null)
            {
                Debug.LogWarning("CANVAS: signing furniture missing - the ceremony is dropped, the resolution stays silent.");
                return null;
            }

            Canvas canvas = CanvasChrome.EnsureHost();
            var screen = new SigningScreen();

            var root = new GameObject("SigningScreen");
            screen.Root = root;
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.AddComponent<RectTransform>());

            // The wash: canvas-side scrim ground, stretched whole, untinted (real-colour, §3.0a).
            var wash = new GameObject("Wash");
            wash.transform.SetParent(root.transform, false);
            Stretch(wash.AddComponent<RectTransform>());
            if (scrimTexture != null)
            {
                RawImage washImage = wash.AddComponent<RawImage>();
                washImage.texture = scrimTexture;
                washImage.raycastTarget = true; // swallow clicks outside the document
            }
            else
            {
                Image washFill = wash.AddComponent<Image>();
                washFill.color = new Color(0f, 0f, 0f, 0.75f);
            }

            // V-S1: one dark plate as the shadow.
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            var shadowRect = shadow.AddComponent<RectTransform>();
            shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.sizeDelta = new Vector2(844f, 700f);
            shadowRect.anchoredPosition = new Vector2(0f, -14f);
            Image shadowImage = shadow.AddComponent<Image>();
            shadowImage.color = new Color(0f, 0f, 0f, 0.55f);
            shadowImage.raycastTarget = false;

            // The document: 820 wide per 1g, flat paper (V-S1), ornate frame inset 14.
            var document = new GameObject("Document");
            document.transform.SetParent(root.transform, false);
            var docRect = document.AddComponent<RectTransform>();
            docRect.anchorMin = docRect.anchorMax = new Vector2(0.5f, 0.5f);
            docRect.sizeDelta = new Vector2(820f, 680f);
            docRect.anchoredPosition = new Vector2(0f, 8f);
            Image paper = document.AddComponent<Image>();
            paper.color = PoliSimTheme.Hex(0xF2EADB);

            var ornate = new GameObject("OrnateFrame");
            ornate.transform.SetParent(document.transform, false);
            var ornateRect = ornate.AddComponent<RectTransform>();
            ornateRect.anchorMin = Vector2.zero;
            ornateRect.anchorMax = Vector2.one;
            ornateRect.offsetMin = new Vector2(14f, 14f);
            ornateRect.offsetMax = new Vector2(-14f, -14f);
            Image ornateImage = ornate.AddComponent<Image>();
            ornateImage.sprite = frame;
            ornateImage.type = Image.Type.Sliced;
            ornateImage.fillCenter = false;
            ornateImage.pixelsPerUnitMultiplier = 2f;
            ornateImage.raycastTarget = false;

            // Content column, inside the 1g padding.
            var content = new GameObject("Content");
            content.transform.SetParent(document.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(58f, 40f);
            contentRect.offsetMax = new Vector2(-58f, -46f);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            // Masthead: state seal 56 over the institution line, then title + provenance.
            Texture2D stateSeal = IconLibrary.GetChrome("ui_seal_state");
            if (stateSeal != null)
            {
                var mastSeal = new GameObject("MastheadSeal");
                mastSeal.transform.SetParent(content.transform, false);
                Image mastImage = mastSeal.AddComponent<Image>();
                mastImage.sprite = CanvasChrome.Whole(stateSeal, "ui_seal_state#whole");
                mastImage.raycastTarget = false;
                mastImage.preserveAspect = true;
                var le = mastSeal.AddComponent<LayoutElement>();
                le.preferredHeight = 56f;
            }

            Text institution = CanvasChrome.MakeText(content.transform, "Institution",
                $"PARLIAMENT · {country.Name.ToUpperInvariant()}", PoliSimTheme.Display, 12,
                PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleCenter, FontStyle.Bold);
            institution.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

            Text title = CanvasChrome.MakeText(content.transform, "Title", record.Title,
                PoliSimTheme.Display, 30, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            // Layout-sized, not fixed: a long bill title wraps rather than clipping — the clipping
            // class re-enters this surface through sized rects, so the title gets a preferred height.
            var titleText = title;
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var titleElement = title.gameObject.AddComponent<ContentSizeFitter>();
            titleElement.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text provenance = CanvasChrome.MakeText(content.transform, "Provenance",
                $"DIVISION No. {record.Number} · {record.Date:yyyy-MM-dd}", PoliSimTheme.Document, 12,
                PoliSimTheme.TextSecondary, TextAnchor.MiddleCenter);
            provenance.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

            var closingRule = new GameObject("ClosingRule");
            closingRule.transform.SetParent(content.transform, false);
            closingRule.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 2f);
            Image closingImage = closingRule.AddComponent<Image>();
            closingImage.color = PoliSimTheme.Hex(0x2B2620);
            closingImage.raycastTarget = false;

            BuildDivisionPlate(content.transform, record);

            // Signature block: the presentation slot stays data-honest (V-S3), then the rule and
            // the 104×104 seal landing zone beside the SIGN button.
            Text resolved = CanvasChrome.MakeText(content.transform, "Resolved",
                $"RESOLVED · ALIGNMENT {record.Alignment:+0.00;-0.00}", PoliSimTheme.Document, 11,
                PoliSimTheme.TextSecondary, TextAnchor.MiddleCenter);
            resolved.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 16f);

            var signatureRule = new GameObject("SignatureRule");
            signatureRule.transform.SetParent(content.transform, false);
            signatureRule.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1.5f);
            Image signatureImage = signatureRule.AddComponent<Image>();
            signatureImage.color = PoliSimTheme.Hex(0x2B2620);
            signatureImage.raycastTarget = false;

            var signRow = new GameObject("SignRow");
            signRow.transform.SetParent(content.transform, false);
            signRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 116f);
            HorizontalLayoutGroup rowLayout = signRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 40f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            var landing = new GameObject("SealLanding");
            landing.transform.SetParent(signRow.transform, false);
            landing.AddComponent<RectTransform>().sizeDelta = new Vector2(104f, 104f);
            Texture2D sealTexture = IconLibrary.GetChrome("ui_seal_official");
            var sealGo = new GameObject("Seal");
            sealGo.transform.SetParent(landing.transform, false);
            var sealRect = sealGo.AddComponent<RectTransform>();
            sealRect.anchorMin = sealRect.anchorMax = new Vector2(0.5f, 0.5f);
            sealRect.sizeDelta = new Vector2(104f, 104f);
            Image sealImage = sealGo.AddComponent<Image>();
            sealImage.sprite = CanvasChrome.Whole(sealTexture, "ui_seal_official#whole");
            sealImage.preserveAspect = true;
            sealImage.raycastTarget = false;
            screen._seal = sealGo.AddComponent<SealDrop>();
            sealGo.SetActive(false);

            BuildSignButton(signRow.transform, onSign);

            return screen;
        }

        /// <summary>Starts the seal beat (§1g: drop 1.3 → 1.0 over 140ms with a 6px settle). The seam watches <see cref="Sealed"/>.</summary>
        public void Sign()
        {
            if (_seal != null && !_seal.gameObject.activeSelf)
            {
                _seal.gameObject.SetActive(true);
            }
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

        /// <summary>The division plate: lean bar + verdict stamp, the record's own facts. The stamp is WoA on PAPER, so it takes the INK weights — the same family answer the Parliament panel recorded.</summary>
        private static void BuildDivisionPlate(Transform parent, DivisionRecord record)
        {
            var plate = new GameObject("DivisionPlate");
            plate.transform.SetParent(parent, false);
            plate.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 64f);
            Image plateImage = plate.AddComponent<Image>();
            plateImage.color = PoliSimTheme.Hex(0xF4ECDC);
            plateImage.raycastTarget = false;
            HorizontalLayoutGroup plateLayout = plate.AddComponent<HorizontalLayoutGroup>();
            plateLayout.childAlignment = TextAnchor.MiddleCenter;
            plateLayout.spacing = 26f;
            plateLayout.padding = new RectOffset(20, 20, 12, 12);
            plateLayout.childControlWidth = false;
            plateLayout.childControlHeight = false;

            Color verdictInk = record.Passed ? PoliSimTheme.Good : PoliSimTheme.Bad;

            // The diverging bar as a widget: track + a fill anchored from centre, sign by ink.
            var track = new GameObject("LeanTrack");
            track.transform.SetParent(plate.transform, false);
            track.AddComponent<RectTransform>().sizeDelta = new Vector2(320f, 10f);
            Image trackImage = track.AddComponent<Image>();
            trackImage.color = PoliSimTheme.Hex(0xC9BA9B);
            trackImage.raycastTarget = false;

            var fill = new GameObject("LeanFill");
            fill.transform.SetParent(track.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            float half = 160f;
            float extent = Mathf.Clamp(record.Alignment / 0.5f, -1f, 1f) * half;
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(extent >= 0f ? 0f : 1f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(Mathf.Abs(extent), 10f);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = record.Alignment >= 0f ? PoliSimTheme.Good : PoliSimTheme.Bad;
            fillImage.raycastTarget = false;

            Texture2D stampTexture = IconLibrary.GetChrome(record.Passed ? "ui_stamp_carried" : "ui_stamp_rejected");
            if (stampTexture != null)
            {
                var stamp = new GameObject("Stamp");
                stamp.transform.SetParent(plate.transform, false);
                stamp.AddComponent<RectTransform>().sizeDelta = new Vector2(122f, 36f);
                Image stampImage = stamp.AddComponent<Image>();
                stampImage.sprite = CanvasChrome.Whole(stampTexture, stampTexture.name + "#whole");
                stampImage.color = verdictInk;
                stampImage.preserveAspect = true;
                stampImage.raycastTarget = false;
            }
        }

        /// <summary>The canvas brass button pattern: uGUI Button + SpriteSwap over the delivered per-state strips.</summary>
        private static void BuildSignButton(Transform parent, Action onSign)
        {
            Sprite normal = CanvasChrome.Sliced("ui_btn_brass_canvas", 24f, 24f, 24f, 24f);
            Sprite hover = CanvasChrome.Sliced("ui_btn_brass_canvas_hover", 24f, 24f, 24f, 24f);
            Sprite pressed = CanvasChrome.Sliced("ui_btn_brass_canvas_pressed", 24f, 24f, 24f, 24f);

            var button = new GameObject("SignButton");
            button.transform.SetParent(parent, false);
            button.AddComponent<RectTransform>().sizeDelta = new Vector2(220f, 56f);
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

            control.onClick.AddListener(() => onSign());

            Text label = CanvasChrome.MakeText(button.transform, "Label", "SIGN", PoliSimTheme.Display, 18,
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

    /// <summary>§1g's seal beat: scale 1.3 → 1.0 over 140ms, a 6px settle nudge, then a short hold before <see cref="Settled"/> reports true and the seam covers out.</summary>
    public class SealDrop : MonoBehaviour
    {
        private const float DropSeconds = 0.14f;
        private const float HoldSeconds = 0.5f;

        public bool Settled { get; private set; }

        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startTime;
            float t = Mathf.Clamp01(elapsed / DropSeconds);
            float scale = Mathf.Lerp(1.3f, 1f, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            var rect = (RectTransform)transform;
            rect.anchoredPosition = t >= 1f && elapsed < DropSeconds + 0.08f
                ? new Vector2(0f, -6f * (1f - (elapsed - DropSeconds) / 0.08f))
                : Vector2.zero;

            if (elapsed >= DropSeconds + HoldSeconds)
            {
                Settled = true;
            }
        }
    }
}
