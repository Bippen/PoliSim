using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// Board 21d (D-PS, §685): **THE ROW GRAMMAR ON CANVAS** - the election night's foot and reference view take the desk's row pieces
    /// (<see cref="GameController"/>'s IMGUI row grammar) in uGUI: a party's MARK in its slot, an OUTLINE stamp (a state named, not a control),
    /// a caption, and a SLIP (19b's level 1) on hover, where the sentence a row replaced is kept. The night has no IMGUI pass, so its slip is a
    /// component here: it opens 250 ms after the pointer rests on its anchor, below-right of the pointer, and closes when the pointer leaves -
    /// no pin and no second level on Canvas (a film cannot show a pointer's slip; the sentence is the slip's, never lost).
    /// </summary>
    public static class CanvasRows
    {
        /// <summary>A caption at the night's Document face; its preferred width is the text's, so a horizontal layout gives it exactly that.</summary>
        public static Text Caption(Transform parent, string text, int size, Color ink, FontStyle style = FontStyle.Normal)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, "Caption", text, PoliSimTheme.Document, size, ink, TextAnchor.MiddleLeft, style);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        /// <summary>A party's mark in a square slot: its 4b mark as authored (the accessor's contract), or the board's letter square where none is held.</summary>
        public static GameObject Mark(Transform parent, CountryId country, string key, float side)
        {
            Texture2D mark = null;
            foreach (PoliticalParty party in PartySystems.For(country)) { if (party.Abbrev == key) { mark = IconLibrary.GetPartyMark(party.MarkName); break; } }
            var go = new GameObject("Mark " + key);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(side, side);
            LayoutElement size = go.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = side;
            size.minHeight = size.preferredHeight = side;
            size.flexibleWidth = 0f;
            if (mark != null)
            {
                RawImage image = go.AddComponent<RawImage>();
                image.texture = mark;
                image.raycastTarget = false;
                return go;
            }
            Edges(go.transform, PoliSimTheme.TextPrimary, 1f);
            Text letters = CanvasChrome.MakeTextRealWeight(go.transform, "Letters", PartySystems.ShortName(country, key), PoliSimTheme.Document,
                Mathf.Max(9, Mathf.RoundToInt(side * 0.5f)), PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            Stretch((RectTransform)letters.transform);
            return go;
        }

        /// <summary>An OUTLINE stamp: the words in a 1.5-unit rule (Caution for NO CONFIDENCE and WITHDRAWN, TextPrimary otherwise), padded 6.</summary>
        public static GameObject Stamp(Transform parent, string text, int size, Color ink)
        {
            var go = new GameObject("Stamp " + text);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(6, 6, 2, 2);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            LayoutElement height = go.AddComponent<LayoutElement>();
            height.minHeight = height.preferredHeight = size + 10f;
            Caption(go.transform, text, size, ink);
            Edges(go.transform, ink, 1.5f);
            return go;
        }

        /// <summary>A spacer that takes the row's spare width.</summary>
        public static void Spacer(Transform parent)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        /// <summary>Four rules on a rect's edges, outside its layout.</summary>
        public static void Edges(Transform parent, Color ink, float width)
        {
            void Edge(string name, Vector2 min, Vector2 max, Vector2 size)
            {
                var e = new GameObject(name);
                e.transform.SetParent(parent, false);
                var r = e.AddComponent<RectTransform>();
                r.anchorMin = min;
                r.anchorMax = max;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = size;
                r.anchoredPosition = new Vector2(size.x == 0f ? 0f : (min.x == 0f ? width * 0.5f : -width * 0.5f), size.y == 0f ? 0f : (min.y == 0f ? width * 0.5f : -width * 0.5f));
                e.AddComponent<LayoutElement>().ignoreLayout = true;
                Image img = e.AddComponent<Image>();
                img.color = ink;
                img.raycastTarget = false;
            }
            Edge("EdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, width));
            Edge("EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, width));
            Edge("EdgeLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(width, 0f));
            Edge("EdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(width, 0f));
        }

        /// <summary>A horizontal row of cells at a fixed height, children at their preferred widths, vertically centred.</summary>
        public static Transform HRow(Transform parent, string name, float height, float spacing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
            return go.transform;
        }

        /// <summary>A cell of a fixed width in a row (the reference view's columns).</summary>
        public static Text FixedCell(Transform row, string text, float width, int size, Color ink, TextAnchor anchor = TextAnchor.MiddleLeft, Font font = null)
        {
            Text t = CanvasChrome.MakeTextRealWeight(row, "Cell", text, font ?? PoliSimTheme.Document, size, ink, anchor);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0f;
            return t;
        }

        /// <summary>Hangs a slip on <paramref name="anchor"/>: the head and the lines, drawn over <paramref name="overlay"/> (the screen's root, so it
        /// draws above every column). The anchor takes a clear raycast target so the pointer finds it.</summary>
        public static void Slip(GameObject anchor, Transform overlay, string head, IEnumerable<string> lines)
        {
            if (anchor.GetComponent<Graphic>() == null)
            {
                Image hit = anchor.AddComponent<Image>();
                hit.color = Color.clear;
            }
            else { anchor.GetComponent<Graphic>().raycastTarget = true; }
            CanvasSlip slip = anchor.AddComponent<CanvasSlip>();
            slip.Overlay = overlay;
            slip.Head = head;
            slip.Lines = new List<string>(lines);
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
    }

    /// <summary>The Canvas slip (19b level 1): 250 ms at rest on the anchor opens it below-right of the pointer; leaving the anchor closes it.</summary>
    public sealed class CanvasSlip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private const float Delay = 0.25f;
        public Transform Overlay;
        public string Head;
        public List<string> Lines = new List<string>();
        private bool _over;
        private float _since;
        private GameObject _panel;
        private Vector2 _at;

        public void OnPointerEnter(PointerEventData eventData) { _over = true; _since = Time.unscaledTime; _at = eventData.position; }

        /// <summary>The pointer's screen position while it rests (the event's own, never the legacy Input class - the project runs the Input System).</summary>
        public void OnPointerMove(PointerEventData eventData) { _at = eventData.position; }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            if (_panel != null) { _panel.SetActive(false); }
        }

        private void Update()
        {
            if (PoliSim.Testing.CaptureIdentity.Armed) { return; }   // a film never opens a pointer's slip (GameController.DrawSlips)
            if (Overlay == null) { Canvas host = GetComponentInParent<Canvas>(); Overlay = host != null ? host.transform : null; }   // no overlay named: the canvas itself
            if (!_over || Overlay == null || Time.unscaledTime - _since < Delay || (_panel != null && _panel.activeSelf)) { return; }
            if (_panel == null) { _panel = Build(); }
            var overlayRect = (RectTransform)Overlay;
            Canvas canvas = Overlay.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, _at, cam, out Vector2 local);
            var r = (RectTransform)_panel.transform;
            r.anchoredPosition = local + new Vector2(12f, -12f);
            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
        }

        /// <summary>The panel may hang under the canvas rather than the anchor's screen: it goes with the anchor.</summary>
        private void OnDestroy() { if (_panel != null) { Destroy(_panel); } }

        private void OnDisable() { _over = false; if (_panel != null) { _panel.SetActive(false); } }

        private GameObject Build()
        {
            var go = new GameObject("Slip");
            go.transform.SetParent(Overlay, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 1f);
            Image paper = go.AddComponent<Image>();
            paper.color = PoliSimTheme.Card;
            paper.raycastTarget = false;
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 5, 6);
            v.spacing = 2f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            ContentSizeFitter fit = go.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CanvasRows.Caption(go.transform, Head, 11, PoliSimTheme.TextPrimary);
            foreach (string line in Lines) { CanvasRows.Caption(go.transform, line, 10, PoliSimTheme.TextSecondary); }
            CanvasRows.Edges(go.transform, PoliSimTheme.HairlineStrong, 1f);
            go.SetActive(false);
            return go;
        }
    }
}
