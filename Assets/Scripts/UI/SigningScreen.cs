using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System;
using PoliSim.Data;
using PoliSim.Simulation;
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

        /// <summary>True once §A.13's entrance rows have played out (the document risen and settled,
        /// the controls faded in) — the harness's settle flag composes this in, so a capture never
        /// films the SIGN button mid-fade.</summary>
        public bool EntranceSettled => _entrance == null || _entrance.Settled;

        private SealDrop _seal;
        private DocumentEntrance _entrance;

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
            shadowRect.anchorMin = new Vector2(0.03f, 0.04f);   // P2-4.3: full frame, not a square on black
            shadowRect.anchorMax = new Vector2(0.97f, 0.96f);
            shadowRect.sizeDelta = new Vector2(24f, 20f);
            shadowRect.anchoredPosition = new Vector2(0f, -14f);
            Image shadowImage = shadow.AddComponent<Image>();
            shadowImage.color = new Color(0f, 0f, 0f, 0.55f);
            shadowImage.raycastTarget = false;

            // The document: 820 wide per 1g, flat paper (V-S1), ornate frame inset 14.
            var document = new GameObject("Document");
            document.transform.SetParent(root.transform, false);
            var docRect = document.AddComponent<RectTransform>();
            docRect.anchorMin = new Vector2(0.03f, 0.04f);   // P2-4.3: the document takes the frame, a margin of paper-on-scrim around it
            docRect.anchorMax = new Vector2(0.97f, 0.96f);
            docRect.sizeDelta = Vector2.zero;
            docRect.anchoredPosition = new Vector2(0f, 8f);
            Image paper = document.AddComponent<Image>();
            paper.color = PoliSimTheme.Hex(0xF2EADB);

            // ui_frame_ornate is real-colour (gilt) — as-authored, border only.
            Image ornateImage = CanvasChrome.AsAuthoredImage(document.transform, "OrnateFrame", frame, sliced: true);
            ornateImage.fillCenter = false;
            RectTransform ornateRect = ornateImage.rectTransform;
            ornateRect.anchorMin = Vector2.zero;
            ornateRect.anchorMax = Vector2.one;
            ornateRect.offsetMin = new Vector2(14f, 14f);
            ornateRect.offsetMax = new Vector2(-14f, -14f);

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
            layout.childControlHeight = true;   // P2-4.3: the column hands out heights, so the plate can take the rest
            layout.childForceExpandHeight = false;

            // Masthead: state seal 56 over the institution line, then title + provenance.
            Texture2D stateSeal = IconLibrary.GetChrome("ui_seal_state");
            if (stateSeal != null)
            {
                // ui_seal_state is WoA — untinted it printed WHITE on the paper (caught by eye in
                // the first sgn run: the class's fifth visit, and the reason the tint accessors now
                // exist). On paper it takes an ink, the institution line's own muted tone.
                Image mastImage = CanvasChrome.TintedImage(content.transform, "MastheadSeal",
                    CanvasChrome.Whole(stateSeal, "ui_seal_state#whole"), PoliSimTheme.Hex(0x6B6250));
                mastImage.preserveAspect = true;
                mastImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            }

            Text institution = CanvasChrome.MakeText(content.transform, "Institution",
                $"PARLIAMENT · {country.Name.ToUpperInvariant()}", PoliSimTheme.Display, 12,
                PoliSimTheme.Hex(0x6B6250), TextAnchor.MiddleCenter, FontStyle.Bold);
            institution.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);
            institution.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

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
            provenance.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var closingRule = new GameObject("ClosingRule");
            closingRule.transform.SetParent(content.transform, false);
            closingRule.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 2f);
            closingRule.AddComponent<LayoutElement>().preferredHeight = 2f;
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
            resolved.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            var signatureRule = new GameObject("SignatureRule");
            signatureRule.transform.SetParent(content.transform, false);
            signatureRule.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1.5f);
            signatureRule.AddComponent<LayoutElement>().preferredHeight = 1.5f;
            Image signatureImage = signatureRule.AddComponent<Image>();
            signatureImage.color = PoliSimTheme.Hex(0x2B2620);
            signatureImage.raycastTarget = false;

            var signRow = new GameObject("SignRow");
            signRow.transform.SetParent(content.transform, false);
            signRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 116f);
            signRow.AddComponent<LayoutElement>().preferredHeight = 116f;
            HorizontalLayoutGroup rowLayout = signRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 40f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            // ⚠ PLAYTEST FIX (2026-08-18): the seal used to drop, and the button read "SIGN", for
            // EVERY division regardless of record.Passed - a false player-facing claim, not a
            // cosmetic slip (a rejected bill was never enacted; there is nothing to sign). The
            // landing zone's 104x104 slot stays reserved either way, so the button sits in the
            // identical position for both verdicts; only what fills it, and what the button says,
            // now depends on the record.
            var landing = new GameObject("SealLanding");
            landing.transform.SetParent(signRow.transform, false);
            landing.AddComponent<RectTransform>().sizeDelta = new Vector2(104f, 104f);

            GameObject sealBeat;
            if (record.Passed)
            {
                Texture2D sealTexture = IconLibrary.GetChrome("ui_seal_official");
                // The wax seal is real-colour: as-authored, locked white.
                Image sealImage = CanvasChrome.AsAuthoredImage(landing.transform, "Seal",
                    CanvasChrome.Whole(sealTexture, "ui_seal_official#whole"));
                RectTransform sealRect = sealImage.rectTransform;
                sealRect.anchorMin = sealRect.anchorMax = new Vector2(0.5f, 0.5f);
                sealRect.sizeDelta = new Vector2(104f, 104f);
                sealImage.preserveAspect = true;
                sealBeat = sealImage.gameObject;
            }
            else
            {
                // No enactment, no seal to drop. SealDrop only ever animates its own RectTransform
                // (see its own class below) - it needs no Image - so an empty timer object carries
                // the identical settle beat with nothing visible to show, and Sign()/Sealed keep
                // driving the seam exactly as they do for a passed division.
                var timer = new GameObject("SealTimer");
                timer.transform.SetParent(landing.transform, false);
                RectTransform timerRect = timer.AddComponent<RectTransform>();
                timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 0.5f);
                timerRect.sizeDelta = new Vector2(104f, 104f);
                sealBeat = timer;
            }

            screen._seal = sealBeat.AddComponent<SealDrop>();
            sealBeat.SetActive(false);

            CanvasGroup controls = BuildSignButton(signRow.transform, onSign, record.Passed);

            // §A.13's two rows that had no implementation (re-derived against the seam 2026-08-28,
            // omnibus roadmap item 4): row 4, the document rises 24px and settles −0.6° → 0° over
            // 240–500ms, ease-out cubic; row 6, the controls fade in LAST (700ms+). The seal thunk
            // (row 5) is §1g's own beat below, at its own 1.3 → 1.0 / 140ms - a declared deviation
            // from the envelope's 1.15 / 120ms, kept because §1g is the ceremony's own spec. Rows
            // 1–3 are the IMGUI seam's (GameController's takeover: lock, cover, hold-and-swap).
            screen._entrance = document.AddComponent<DocumentEntrance>();
            screen._entrance.Controls = controls;

            // S-20: the capture-identity token, so a film of this board proves it is this board.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "signing";
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
            // P2-4.3 (2026-09-02): the plate is the division's content, full-frame, where a lean bar sat - three
            // panels in a row: the vote as a per-seat map on the recorded sides (P2-2.2's rings), the citation, and
            // the estimate that travelled with the turn's decision as arrows (P2-2.1's renderer), painted once
            // through CanvasPaint. Its height takes a share of the frame so the document fills what it is given.
            const float plateHeight = 200f;   // the least it needs; the column hands it every flexible pixel it has (LayoutElement below)
            var plate = new GameObject("DivisionPlate");
            plate.transform.SetParent(parent, false);
            plate.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, plateHeight);
            LayoutElement plateElement = plate.AddComponent<LayoutElement>();   // the height pinned as a layout element too
            plateElement.minHeight = plateHeight;
            plateElement.preferredHeight = plateHeight;
            plateElement.flexibleHeight = 1f;
            Image plateImage = plate.AddComponent<Image>();
            plateImage.color = PoliSimTheme.Hex(0xF4ECDC);
            plateImage.raycastTarget = false;
            HorizontalLayoutGroup plateLayout = plate.AddComponent<HorizontalLayoutGroup>();
            plateLayout.childAlignment = TextAnchor.MiddleCenter;
            plateLayout.spacing = 24f;
            plateLayout.padding = new RectOffset(20, 20, 12, 12);
            plateLayout.childControlWidth = true;
            plateLayout.childControlHeight = true;
            plateLayout.childForceExpandWidth = true;
            plateLayout.childForceExpandHeight = true;

            // 1. The vote as seats.
            int forSeats = 0, undecided = 0, against = 0;
            foreach (DivisionSide side in record.Sides)
            {
                if (side.Side > 0) { forSeats += side.Seats; } else if (side.Side < 0) { against += side.Seats; } else { undecided += side.Seats; }
            }
            Transform votePanel = PlatePanel(plate.transform, "Vote", 1.2f);
            PlateCaption(votePanel, "THE DIVISION · EVERY MANDATE");
            if (record.Sides.Count > 0)
            {
                PlateImage(votePanel, "SeatMap", CanvasPaint.SeatMap(360, 190, forSeats, undecided, against, PoliSimTheme.Hex(0xF4ECDC)), 360f / 190f);
                PlateCaption(votePanel, string.Format(CultureInfo.InvariantCulture, "FOR {0} · UNDECIDED {1} · AGAINST {2}", forSeats, undecided, against));
            }
            else
            {
                PlateCaption(votePanel, "no sides recorded for this division - it predates the map");
            }

            // 2. The citation.
            Transform cite = PlatePanel(plate.transform, "Citation", 0.8f);
            PlateCaption(cite, "THE CITATION");
            PlateBody(cite, $"Division No. {record.Number}");
            PlateBody(cite, record.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            PlateBody(cite, string.Format(CultureInfo.InvariantCulture, "alignment {0:+0.00;-0.00} · {1}", record.Alignment, record.Passed ? "CARRIED" : "LOST"),
                record.Passed ? PoliSimTheme.Good : PoliSimTheme.Bad);
            PlateBody(cite, record.Axis == (int)BillAxis.Trade ? "on the openness axis" : "on the fiscal axis");

            // 2b. The stances - P3-A3 (2026-09-03): every party's side with the reason the model gave it, as the vote
            // was (the record carries the alignment and the reason since this row); drawn structurally until D12.
            Transform stances = PlatePanel(plate.transform, "Stances", 1.6f);
            PlateCaption(stances, "THE STANCES · EVERY PARTY, WITH ITS REASON");
            bool anyReason = false;
            foreach (DivisionSide side in record.Sides)
            {
                string verdict = side.Side > 0 ? "FOR" : side.Side < 0 ? "AGAINST" : "UNDECIDED";
                Color ink = side.Side > 0 ? PoliSimTheme.Good : side.Side < 0 ? PoliSimTheme.Bad : PoliSimTheme.TextSecondary;
                PlateBody(stances, string.Format(CultureInfo.InvariantCulture, "{0} · {1} seats · {2}{3}", side.Abbrev, side.Seats, verdict,
                    string.IsNullOrEmpty(side.Reason) ? string.Empty : string.Format(CultureInfo.InvariantCulture, " {0:+0.00;-0.00}", side.Alignment)), ink);
                string reason = string.IsNullOrEmpty(side.ReasonShort) ? side.Reason : side.ReasonShort;   // the plate takes the short form; the record keeps the full line
                if (!string.IsNullOrEmpty(reason)) { PlateCaption(stances, reason); anyReason = true; }
            }
            if (record.Sides.Count == 0) { PlateCaption(stances, "no sides recorded for this division - it predates the map"); }
            else if (!anyReason) { PlateCaption(stances, "no reasons recorded - this division predates the stance model"); }

            // 3. The estimate as arrows - board 5c (D11 row 3): the same three-part grammar as the sheet's
            // panel, titled AS ENACTED: the arrows from a hairline baseline, each figure signed in its
            // arrow's ink in lane order, and the scope line verbatim beneath.
            Transform estimate = PlatePanel(plate.transform, "Estimate", 1.2f);
            PlateCaption(estimate, EffectArrowsRenderer.PlateTitleEnacted);
            if (record.Effects.Count > 0)
            {
                var arrows = new List<EffectArrow>(record.Effects.Count);
                foreach (DivisionEffect e in record.Effects)
                {
                    arrows.Add(new EffectArrow(e.Name, e.Value, e.HigherIsBetter, e.Figure));
                }
                PlateImage(estimate, "Arrows", CanvasPaint.Arrows(420, 120, arrows, PoliSimTheme.Hex(0xF4ECDC)), 420f / 120f);
                PlateCaption(estimate, EffectArrowsRenderer.FiguresLine(arrows));
                PlateCaption(estimate, EffectArrowsRenderer.ScopeLine);
            }
            else
            {
                PlateCaption(estimate, "no estimate travelled with this division - no preview was held for its turn, or it predates the arrows");
            }
        }

        private static Transform PlatePanel(Transform parent, string name, float weight)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            panel.AddComponent<LayoutElement>().flexibleWidth = weight;
            VerticalLayoutGroup column = panel.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 4f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return panel.transform;
        }

        private static void PlateImage(Transform parent, string name, Texture2D texture, float aspect)
        {
            // A sprite that preserves its aspect inside the cell the layout gives it - an AspectRatioFitter would drive the
            // rect the layout group also drives, and the first film showed the two fighting.
            var art = new GameObject(name);
            art.transform.SetParent(parent, false);
            art.AddComponent<RectTransform>();
            LayoutElement element = art.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.minHeight = 40f;
            Image image = art.AddComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void PlateCaption(Transform parent, string text)
        {
            Text caption = CanvasChrome.MakeText(parent, "Caption", text, PoliSimTheme.Document, 11, PoliSimTheme.TextSecondary, TextAnchor.MiddleCenter);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.gameObject.AddComponent<LayoutElement>().minHeight = 16f;
        }

        private static void PlateBody(Transform parent, string text, Color? ink = null)
        {
            Text body = CanvasChrome.MakeText(parent, "Body", text, PoliSimTheme.Document, 14, ink ?? PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.gameObject.AddComponent<LayoutElement>().minHeight = 20f;
        }





        /// <summary>The canvas brass button pattern: uGUI Button + SpriteSwap over the delivered per-state strips. The label reads "SIGN" only for a passed division - "FILE" for a rejected one, matching the plate's own REJECTED stamp rather than claiming an enactment that did not happen. Returns the button's CanvasGroup - §A.13 row 6's fade handle (the controls fade in last).</summary>
        private static CanvasGroup BuildSignButton(Transform parent, Action onSign, bool passed)
        {
            Sprite normal = CanvasChrome.Sliced("ui_btn_brass_canvas", 24f, 24f, 24f, 24f);
            Sprite hover = CanvasChrome.Sliced("ui_btn_brass_canvas_hover", 24f, 24f, 24f, 24f);
            Sprite pressed = CanvasChrome.Sliced("ui_btn_brass_canvas_pressed", 24f, 24f, 24f, 24f);

            var button = new GameObject("SignButton");
            button.transform.SetParent(parent, false);
            button.AddComponent<RectTransform>().sizeDelta = new Vector2(220f, 56f);
            // Not through the tint accessors: a Button face must keep raycastTarget true, and the
            // missing-sprite degradation needs the brass fill, not locked white.
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

            Text label = CanvasChrome.MakeText(button.transform, "Label", passed ? "SIGN" : "FILE", PoliSimTheme.Display, 18,
                PoliSimTheme.Hex(0xF0E7D8), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch((RectTransform)label.transform);

            // Row 6's handle: the group starts invisible and non-interactable; DocumentEntrance brings
            // it in once the document has settled, so a click cannot land on a button that is not yet
            // there (input locks are the envelope's first row).
            CanvasGroup group = button.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// §A.13 rows 4 and 6 (built 2026-08-28, omnibus roadmap item 4): the document rises 24px and
    /// settles −0.6° → 0° over 260ms with an ease-out cubic (the envelope's 240–500ms), and the
    /// controls fade in LAST - 460ms after the document starts (the envelope's 700ms mark, counted
    /// from the 240ms swap), over 200ms. Unscaled time, like the seal beat, so a held sim clock does
    /// not freeze the ceremony. The rest position is captured on the FIRST enable only: the seam can
    /// hide and re-show the screen, and re-capturing mid-rise would drift the document.
    /// </summary>
    public class DocumentEntrance : MonoBehaviour
    {
        private const float RiseSeconds = 0.26f;
        private const float RisePixels = 24f;
        private const float SettleDegrees = -0.6f;
        private const float ControlsDelaySeconds = 0.46f;
        private const float ControlsFadeSeconds = 0.2f;

        public CanvasGroup Controls;

        public bool Settled { get; private set; }

        private Vector2 _rest;
        private bool _restCaptured;
        private float _startTime;

        private void OnEnable()
        {
            var rect = (RectTransform)transform;
            if (!_restCaptured)
            {
                _rest = rect.anchoredPosition;
                _restCaptured = true;
            }

            _startTime = Time.unscaledTime;
            Settled = false;
            Apply(0f);
            if (Controls != null)
            {
                Controls.alpha = 0f;
                Controls.interactable = false;
                Controls.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startTime;
            Apply(Mathf.Clamp01(elapsed / RiseSeconds));

            if (Controls != null)
            {
                float fade = Mathf.Clamp01((elapsed - ControlsDelaySeconds) / ControlsFadeSeconds);
                Controls.alpha = fade;
                bool live = fade >= 1f;
                Controls.interactable = live;
                Controls.blocksRaycasts = live;
            }

            if (elapsed >= ControlsDelaySeconds + ControlsFadeSeconds)
            {
                Settled = true;
            }
        }

        private void Apply(float t)
        {
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            var rect = (RectTransform)transform;
            rect.anchoredPosition = _rest + new Vector2(0f, -RisePixels * (1f - eased));
            rect.localRotation = Quaternion.Euler(0f, 0f, SettleDegrees * (1f - eased));
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
