using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// D-IT (Design's boards 34a-34c): <b>ITALY'S CHAMBER AS SEAT CELLS</b> at the centre of the Italian night - one cell per seat, each circoscrizione
    /// (Camera) or region (Senato) a block of its own magnitude inside Italy's outline, the abroad seats and the senators for life in their boxes in the sea.
    ///
    /// <para><b>The geometry is the board's</b> (`ItalianNightCartogram`, generated from 34a's own table) and the night computes what each cell says
    /// (`ElectionNightScreen.Italy.cs`): this view only paints a plan - cells at their board positions with their ink, or paper with a tan rule where the seat is
    /// still to come; a college's paper dot; the paper borders between blocks; and the labels and captions as Canvas text over the cells, so the text guard
    /// measures them. The plan is in board pixels and is drawn at the rect's own scale - 1.5 canvas units a board pixel in the night's column, so a 14 px label
    /// lands at 14 px at 1280 and 28 at 2560 (34c).</para>
    /// </summary>
    public sealed class ItalianCellView : MonoBehaviour
    {
        /// <summary>One seat: its board-pixel square, its ink (null: still to come, paper with a tan rule), and the college's dot.</summary>
        public struct Cell
        {
            public float X, Y, Side;
            public Color? Ink;
            public bool Dot;
            public float DotSide;
        }

        /// <summary>A text over the cells: its board-pixel rect, words, board-pixel size, ink, an outline where it sits on cells, and its slip.</summary>
        public struct Label
        {
            public float X, Y, W, H, Size;
            public string Text;
            public Color Ink;
            public Color? Outline;
            public TextAnchor Anchor;
            public string SlipHead;
            public List<string> SlipLines;
        }

        /// <summary>What the view draws, in board pixels.</summary>
        public sealed class Plan
        {
            public float Width, Height;
            public readonly List<Cell> Cells = new List<Cell>();
            /// <summary>The paper borders (x, y, width, height).</summary>
            public readonly List<Rect> Borders = new List<Rect>();
            public readonly List<Label> Labels = new List<Label>();
        }

        /// <summary>[AUTHORED] layout - 34a's colours for a seat still to come: paper with a 1 px tan rule, and a college's dot in tan.</summary>
        public static readonly Color Paper = PoliSimTheme.ModalBackground, ToComeRule = PoliSimTheme.Hex(0xC9BB9B), ToComeDot = PoliSimTheme.Hairline;

        private Plan _plan;
        private Transform _overlay;
        private Vector2 _built = new Vector2(-1f, -1f);
        private Texture2D _painted;

        /// <summary>The cells painted in the last layout, by state, and the labels' smallest size in canvas units - read by the film's log line and the check.</summary>
        public int LastInked, LastToCome;
        public float LastSmallestLabel;

        private void OnDestroy()
        {
            if (_painted != null) { Destroy(_painted); _painted = null; }
        }

        /// <summary>The view in <paramref name="parent"/> at the plan's own size times <paramref name="unitsPerBoardPixel"/>.</summary>
        public static ItalianCellView Create(Transform parent, Plan plan, Transform overlay, float unitsPerBoardPixel)
        {
            var go = new GameObject("ItalianCells");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = plan.Height * unitsPerBoardPixel;
            element.preferredWidth = plan.Width * unitsPerBoardPixel;
            element.flexibleHeight = 0f;
            var view = go.AddComponent<ItalianCellView>();
            view._plan = plan;
            view._overlay = overlay;
            return view;
        }

        private void LateUpdate()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            if (Mathf.Abs(size.x - _built.x) > 0.5f || Mathf.Abs(size.y - _built.y) > 0.5f) { Rebuild(); }
        }

        public void Rebuild()
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.rect.size;
            _built = size;
            for (int i = transform.childCount - 1; i >= 0; i--) { Destroy(transform.GetChild(i).gameObject); }
            if (_plan == null || size.x < 50f || size.y < 50f || _plan.Width <= 0f || _plan.Height <= 0f) { return; }
            float scale = Mathf.Min(size.x / _plan.Width, size.y / _plan.Height);

            // the texture at the device's pixels: board pixels × scale (canvas units) × the canvas's factor
            float device = Mathf.Max(0.25f, CanvasChrome.ScaleFactor());
            float k = scale * device;
            int w = Mathf.Max(1, Mathf.CeilToInt(_plan.Width * k)), h = Mathf.Max(1, Mathf.CeilToInt(_plan.Height * k));
            var pixels = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }
            void Fill(float x, float y, float fw, float fh, Color32 c)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(x * k), 0, w), x1 = Mathf.Clamp(Mathf.RoundToInt((x + fw) * k), 0, w);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(y * k), 0, h), y1 = Mathf.Clamp(Mathf.RoundToInt((y + fh) * k), 0, h);
                if (x1 <= x0 && fw > 0f) { x1 = Mathf.Min(w, x0 + 1); }
                if (y1 <= y0 && fh > 0f) { y1 = Mathf.Min(h, y0 + 1); }
                for (int py = y0; py < y1; py++) { int row = (h - 1 - py) * w; for (int px = x0; px < x1; px++) { pixels[row + px] = c; } }
            }

            LastInked = LastToCome = 0;
            float hair = 1f / Mathf.Max(0.5f, k) * Mathf.Max(1f, Mathf.Round(device * scale / 1.5f));   // 34a's 1 px rule, a whole device pixel at least
            foreach (Cell c in _plan.Cells)
            {
                if (c.Ink.HasValue)
                {
                    Fill(c.X, c.Y, c.Side, c.Side, c.Ink.Value);
                    if (c.Dot) { float d = (c.Side - c.DotSide) * 0.5f; Fill(c.X + d, c.Y + d, c.DotSide, c.DotSide, Paper); }
                    LastInked++;
                    continue;
                }
                Fill(c.X, c.Y, c.Side, c.Side, ToComeRule);
                Fill(c.X + hair, c.Y + hair, c.Side - 2f * hair, c.Side - 2f * hair, Paper);
                if (c.Dot) { float d = (c.Side - c.DotSide) * 0.5f; Fill(c.X + d, c.Y + d, c.DotSide, c.DotSide, ToComeDot); }
                LastToCome++;
            }
            // the borders: 34a's paper strips over the cells (transparent, so the document's paper shows)
            foreach (Rect b in _plan.Borders) { Fill(b.x, b.y, b.width, b.height, clear); }

            if (_painted != null) { Destroy(_painted); }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _painted = texture;
            texture.SetPixels32(pixels);
            texture.Apply();
            var art = new GameObject("Cells");
            art.transform.SetParent(transform, false);
            RawImage image = art.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            Place(image.rectTransform, 0f, 0f, _plan.Width * scale, _plan.Height * scale);

            LastSmallestLabel = float.MaxValue;
            foreach (Label l in _plan.Labels)
            {
                int fontSize = Mathf.RoundToInt(l.Size * scale);
                LastSmallestLabel = Mathf.Min(LastSmallestLabel, fontSize);
                Text t = CanvasChrome.MakeTextRealWeight(transform, "Label " + l.Text, l.Text, PoliSimTheme.Document, fontSize, l.Ink, l.Anchor);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                float width = Mathf.Max(l.W * scale, Mathf.Ceil(t.preferredWidth) + 2f);   // measured: the words decide the rect, never a guessed pad
                Place(t.rectTransform, l.X * scale, l.Y * scale, width, l.H * scale);
                if (l.Outline.HasValue)
                {
                    Outline o = t.gameObject.AddComponent<Outline>();
                    o.effectColor = l.Outline.Value;
                    o.effectDistance = new Vector2(scale * 0.75f, scale * 0.75f);
                }
                if (l.SlipHead != null) { CanvasRows.Slip(t.gameObject, _overlay, l.SlipHead, l.SlipLines ?? new List<string>()); }
            }
            if (LastSmallestLabel == float.MaxValue) { LastSmallestLabel = 0f; }
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
