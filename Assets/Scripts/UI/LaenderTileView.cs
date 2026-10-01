using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// §707 (D-DE, boards 24a-24b, stamped LOOKED AT 30 SEP 2026): THE LÄNDER AT THE CENTRE OF GERMANY'S ELECTION NIGHT.
    ///
    /// <para><b>The geometry is board 24b's table</b> (A · the sixteen rectangles, read off `assets/dde/geom.json`): tile area = the Land's
    /// registered electorate (k = 2,689.7 px² per million at 1280), four bands north to south, west to east inside each, four slots stacking two
    /// Länder north over south. The table is in 1280 board pixels from the map's origin; the view scales it uniformly into the rect the page
    /// gives it, centred - the positions are the board's, never re-derived.</para>
    ///
    /// <para><b>What a tile says</b> (24a ③, 24b's ladder): a DECLARED Land in its leader's ink - the leader's letters and lead in points, and a
    /// second line VS 2021 (the leader's own change there) where one more line fits; the parts drop by pixels in one order (the VS line, then the
    /// mark, then the name for the code). An UNDECLARED Land is paper with a dashed edge at full size - not yet, not nil.</para>
    /// </summary>
    public sealed class LaenderTileView : MonoBehaviour
    {
        public struct TileResult
        {
            public bool Declared;
            public string Leader;
            public Color Ink;
            public Texture2D Mark;
            public double LeadPp;
            public bool HasSwing;
            public double SwingPp;
        }

        /// <summary>Board 24b's table, in the game's Land order (the Bundeswahlleiterin's: SH MV HH NI HB BB ST BE NW SN HE TH RP BY BW SL):
        /// code, name, x, y, w, h in 1280 board pixels from the map's origin (geom.json's `T` less the sheet's 373, 150).</summary>
        public static readonly (string Code, string Name, float X, float Y, float W, float H)[] Table =
        {
            ("SH", "SCHLESWIG-HOLSTEIN", 118f, 0f, 200.995f, 30.281f),
            ("MV", "MECKLENBURG-VORPOMMERN", 320.995f, 0f, 115.005f, 30.281f),
            ("HH", "HAMBURG", 233.039f, 32.281f, 68.854f, 50.755f),
            ("NI", "NIEDERSACHSEN", 0f, 32.281f, 231.039f, 70.356f),
            ("HB", "BREMEN", 233.039f, 85.036f, 68.854f, 17.601f),
            ("BB", "BRANDENBURG", 303.893f, 71.578f, 176.107f, 31.058f),
            ("ST", "SACHSEN-ANHALT", 346.343f, 104.637f, 68.325f, 68.290f),
            ("BE", "BERLIN", 303.893f, 32.281f, 176.107f, 37.298f),
            ("NW", "NORDRHEIN-WESTFALEN", 0f, 104.637f, 256.054f, 135.341f),
            ("SN", "SACHSEN", 416.668f, 104.637f, 63.332f, 135.341f),
            ("HE", "HESSEN", 258.054f, 104.637f, 86.289f, 135.341f),
            ("TH", "THÜRINGEN", 346.343f, 174.926f, 68.325f, 65.052f),
            ("RP", "RHEINLAND-PFALZ", 8f, 241.978f, 80.005f, 101.344f),
            ("BY", "BAYERN", 252.802f, 241.978f, 199.198f, 128.028f),
            ("BW", "BADEN-WÜRTTEMBERG", 90.005f, 241.978f, 160.797f, 128.028f),
            ("SL", "SAARLAND", 8f, 345.322f, 80.005f, 24.683f),
        };

        /// <summary>The map's extent in 1280 board pixels (24b: the widest band 480, four bands to 370).</summary>
        public const float BoardWidth = 480f, BoardHeight = 370.006f;

        private const int NameSize = 11;
        private const int LeadSize = 12;
        private TileResult[] _results;
        private Vector2 _built = new Vector2(-1f, -1f);

        /// <summary>Tiles drawn in the last layout, per face - read by the film's log line (24b: 11 FULL · 5 COMPACT · 0 MINIMAL at 1280).</summary>
        public int LastFull, LastCompact, LastMinimal, LastWithSwing;

        public static LaenderTileView Create(Transform parent, TileResult[] results)
        {
            var go = new GameObject("Laender");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 200f;
            var view = go.AddComponent<LaenderTileView>();
            view._results = results;
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
            if (size.x < 50f || size.y < 50f || _results == null || _results.Length != Table.Length) { return; }
            float scale = Mathf.Min(size.x / BoardWidth, size.y / BoardHeight);
            float originX = (size.x - BoardWidth * scale) * 0.5f;
            float originY = (size.y - BoardHeight * scale) * 0.5f;
            LastFull = LastCompact = LastMinimal = LastWithSwing = 0;
            for (int r = 0; r < Table.Length; r++)
            {
                (string code, string name, float x, float y, float w, float h) = Table[r];
                DrawTile(r, code, name, originX + x * scale, originY + y * scale, w * scale, h * scale);
            }
        }

        private void DrawTile(int r, string code, string name, float x, float y, float w, float h)
        {
            TileResult result = _results[r];
            var go = new GameObject("Tile_" + code);
            go.transform.SetParent(transform, false);
            Image face = go.AddComponent<Image>();
            face.raycastTarget = false;
            face.color = result.Declared ? result.Ink : PoliSimTheme.CardInset;
            Place(face.rectTransform, x, y, w, h);
            if (!result.Declared) { DashedEdge(go.transform, w, h); }

            Color ink = result.Declared && Luminance(result.Ink) < 0.42f ? PoliSimTheme.Card : PoliSimTheme.TextPrimary;
            Color faint = result.Declared ? new Color(ink.r, ink.g, ink.b, 0.82f) : PoliSimTheme.TextMuted;
            const float pad = 4f;
            float innerW = w - pad * 2f;
            string lead = result.Declared ? result.Leader + " " + ("+" + result.LeadPp.ToString("0.0", CultureInfo.InvariantCulture)) : "—";
            string vs = result.Declared && result.HasSwing ? "VS 2021 " + Signed(result.SwingPp) : null;
            // §707's real film: 24b draws its lines 14 px apart, but the Document face's own line is 15.2 at these sizes (CANVAS TEXT CLIP on
            // every tile at 1280) - the line is the face's measured height, and a tile too short for it steps down the ladder as 24b's rule says
            float lineH = LineHeight;

            // 24b's ladder, the first that fits: one line with mark, name and lead · one line with name and lead · stacked, mark and name, then the
            // lead · stacked, name, then the lead · the same four with the code for the name; else the code alone (MINIMAL). VS when one more line fits.
            foreach (bool useCode in new[] { false, true })
            {
                string label = useCode ? code : name;
                foreach (bool withMark in new[] { true, false })
                {
                    bool markOk = withMark && result.Declared && result.Mark != null;
                    if (withMark && !markOk) { continue; }
                    float markW = markOk ? lineH + 3f : 0f;
                    // one line
                    if (h >= lineH + 2f && TextWidth(label, NameSize, true) + markW + 6f + TextWidth(lead, LeadSize, true) <= innerW)
                    {
                        float lx = pad;
                        if (markOk) { Chip(go.transform, result.Mark, lx, 2f, lineH); lx += markW; }
                        Put(go.transform, "Name", label, NameSize, faint, TextAnchor.UpperLeft, lx, 2f, innerW - (lx - pad), lineH);
                        Put(go.transform, "Lead", lead, LeadSize, result.Declared ? ink : PoliSimTheme.TextMuted, TextAnchor.UpperRight, pad, 2f, innerW, lineH);
                        bool vsFits = vs != null && h >= lineH * 2f + 4f && TextWidth(vs, 10, false) <= innerW;
                        if (vsFits) { Put(go.transform, "Vs", vs, 10, faint, TextAnchor.UpperLeft, pad, 2f + lineH, innerW, lineH); LastWithSwing++; }
                        if (useCode || !markOk) { LastCompact++; } else { LastFull++; }
                        return;
                    }
                    // stacked
                    if (h >= lineH * 2f + 4f && TextWidth(label, NameSize, true) + markW <= innerW && TextWidth(lead, LeadSize, true) <= innerW)
                    {
                        float lx = pad;
                        if (markOk) { Chip(go.transform, result.Mark, lx, 2f, lineH); lx += markW; }
                        Put(go.transform, "Name", label, NameSize, faint, TextAnchor.UpperLeft, lx, 2f, innerW - (lx - pad), lineH);
                        Put(go.transform, "Lead", lead, LeadSize, result.Declared ? ink : PoliSimTheme.TextMuted, TextAnchor.UpperLeft, pad, 2f + lineH, innerW, lineH);
                        bool vsFits = vs != null && h >= lineH * 3f + 4f && TextWidth(vs, 10, false) <= innerW;
                        if (vsFits) { Put(go.transform, "Vs", vs, 10, faint, TextAnchor.UpperLeft, pad, 2f + lineH * 2f, innerW, lineH); LastWithSwing++; }
                        LastCompact++;
                        return;
                    }
                }
            }
            Put(go.transform, "Code", code, NameSize, faint, TextAnchor.MiddleCenter, pad, 0f, innerW, h);
            LastMinimal++;
        }

        private static string Signed(double pp) => (pp >= 0 ? "+" : "−") + System.Math.Abs(pp).ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>An undeclared Land's edge: four dashed hairlines at full size (24b B: not yet, not nil).</summary>
        private static void DashedEdge(Transform tile, float w, float h)
        {
            const float dash = 4f, gap = 3f;
            Color line = PoliSimTheme.TextMuted;
            for (float t = 0f; t < w; t += dash + gap)
            {
                Bar(tile, t, 0f, Mathf.Min(dash, w - t), 1f, line);
                Bar(tile, t, h - 1f, Mathf.Min(dash, w - t), 1f, line);
            }
            for (float t = 0f; t < h; t += dash + gap)
            {
                Bar(tile, 0f, t, 1f, Mathf.Min(dash, h - t), line);
                Bar(tile, w - 1f, t, 1f, Mathf.Min(dash, h - t), line);
            }
        }

        private static void Bar(Transform parent, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject("Dash");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Place(img.rectTransform, x, y, w, h);
        }

        private static void Chip(Transform tile, Texture2D markTexture, float x, float y, float side)
        {
            var chip = new GameObject("MarkChip");
            chip.transform.SetParent(tile, false);
            Image chipFace = chip.AddComponent<Image>();
            chipFace.color = PoliSimTheme.Card;
            chipFace.raycastTarget = false;
            Place(chipFace.rectTransform, x, y, side, side);
            var mark = new GameObject("Mark");
            mark.transform.SetParent(chip.transform, false);
            RawImage markImage = mark.AddComponent<RawImage>();
            markImage.texture = markTexture;
            markImage.raycastTarget = false;
            RectTransform mr = markImage.rectTransform;
            mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one; mr.offsetMin = new Vector2(1f, 1f); mr.offsetMax = new Vector2(-1f, -1f);
        }

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        private static float _lineHeight;

        /// <summary>The tallest of the three line sizes as the Document face sets them, whole pixels up - measured once, like
        /// <see cref="TextWidth"/>, never typed.</summary>
        private static float LineHeight
        {
            get
            {
                if (_lineHeight > 0f) { return _lineHeight; }
                float tallest = 0f;
                foreach (int size in new[] { NameSize, LeadSize, 10 })
                {
                    Text probe = CanvasChrome.MakeTextRealWeight(null, "Probe", "ÄgV+−0", PoliSimTheme.Document, size, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                    tallest = Mathf.Max(tallest, probe.preferredHeight);
                    Object.Destroy(probe.gameObject);
                }
                _lineHeight = Mathf.Ceil(tallest + 0.5f);
                return _lineHeight;
            }
        }

        private static float TextWidth(string s, int size, bool bold)
        {
            Text probe = CanvasChrome.MakeTextRealWeight(null, "Probe", s, PoliSimTheme.Document, size, Color.white, TextAnchor.UpperLeft, bold ? FontStyle.Bold : FontStyle.Normal);
            float w = probe.preferredWidth;
            Object.Destroy(probe.gameObject);
            return w;
        }

        private static void Put(Transform parent, string name, string content, int size, Color color, TextAnchor anchor, float x, float y, float w, float h)
        {
            Text t = CanvasChrome.MakeTextRealWeight(parent, name, content, PoliSimTheme.Document, size, color, anchor, FontStyle.Bold);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            Place(t.rectTransform, x, y, w, h);
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }
    }
}
