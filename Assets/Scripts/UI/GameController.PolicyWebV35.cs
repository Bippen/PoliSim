using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §740 (UI v3.5, the composition's Laws › Policy web): THE WEB AS TWO COLUMNS - *Your levers* on the left and *What they move* on the right, each a
    /// boxed name with its icon, every link the model holds between them drawn as a hairline at rest - over `PolicyWebRenderer`'s own nodes and edges,
    /// untouched (R-W2's fence: no edge invented, no grouping the model does not hold).
    ///
    /// <para><b>The scale.</b> The composition draws six levers and six readings, its links *illustrative, labelled on screen* (V35_ASK). The model holds
    /// 55 levers, 18 readings and 121 links: drawn at once they are a mat, not a web - which is why board 2b (P3-B1, §263) drew no edge until a node was
    /// pinned. This page keeps the composition's links at rest by showing <b>one area's levers at a time</b> - the model's own grouping, chosen by the
    /// words over the columns (Fiscal first) - against all eighteen readings. A click on a box FOCUSES it: its links draw dark and weighted by their
    /// relative strength, in the reading's own good/bad framing, and the rest fade; a second click releases it (board 2b's pin, kept, less its click on the paper - the slips take their clicks after the page, and a
    /// card-wide catch would eat a slip head's: the film's
    /// `_selectedPolicyWebPolicyNode` and `_selectedPolicyWebStatNode`). A focused lever brings its area with it.</para>
    ///
    /// <para><b>The ink.</b> At rest every link is the neutral hairline the composition draws: solid where the model's own formula carries it (DERIVED),
    /// dashed where it is a stated coupling (DECLARED). Focused, a link takes the reading's own framing of the move (R-W2) - except Debt-to-GDP and the
    /// trade balance, which stay neutral (Elias's debt-and-balance ruling; V35 rule 5's levers and mixes).</para>
    ///
    /// <para><b>Folded</b>: board 2b's pane into the slips - a lever's slip carries its description, its current effects from the live dials and one
    /// line per link with its ledger term; a reading's, what moves it and the book-to-book links (the causal band's seven); the status line's census
    /// and the legend into the head's slip. ⚠ The links are drawn as rotated quads, which IMGUI's scroll clip does not hold (round 4's trap): every
    /// segment is clipped to the visible part of the page by hand.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>§740: the area whose levers the web shows (UI state, not saved).</summary>
        private UiPalette.SystemArea _policyWebArea = UiPalette.SystemArea.Fiscal;

        private static readonly UiPalette.SystemArea[] PolicyWebAreaOrder =
        {
            UiPalette.SystemArea.Fiscal, UiPalette.SystemArea.Labor, UiPalette.SystemArea.CrimeJustice, UiPalette.SystemArea.Welfare,
            UiPalette.SystemArea.Sectors, UiPalette.SystemArea.SovereignWealth, UiPalette.SystemArea.Trade, UiPalette.SystemArea.Political,
        };

        /// <summary>
        /// §740: the web, laid out at <paramref name="width"/> in the page's flow, as tall as the chosen area's levers or the readings need.
        /// <paramref name="visible"/> is the part of the page the scroll shows, in the page's own coordinates - the links are clipped to it.
        /// </summary>
        private void DrawPolicyWebV35(float width, Rect visible)
        {
            if (_selectedPolicyWebPolicyNode.HasValue) { _policyWebArea = PolicyWebRenderer.GetPolicyArea(_selectedPolicyWebPolicyNode.Value); }
            int levers = 0, books = 0;
            foreach (PolicyNodeId id in (PolicyNodeId[])System.Enum.GetValues(typeof(PolicyNodeId))) { if (PolicyWebRenderer.GetPolicyArea(id) == _policyWebArea) { levers++; } }
            foreach (StatNodeId id in (StatNodeId[])System.Enum.GetValues(typeof(StatNodeId))) { if (PolicyWebRenderer.HasStat(id)) { books++; } }
            float cardH = PolicyWebChromeHeight() + Mathf.Max(Mathf.Max(levers, books), 1) * V35.Px(34f);
            Rect card = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            DrawPolicyWebCard(card, visible);
        }

        /// <summary>The card's height less its columns: the padding, the head, the area words and the column heads.</summary>
        private static float PolicyWebChromeHeight() => V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(6f) + V35.Px(26f) + V35.Px(6f) + V35.Px(22f);

        /// <summary>§740: the web in <paramref name="card"/> - the page's, or a ladder rung's (the columns share what the chrome leaves).</summary>
        private void DrawPolicyWebCard(Rect card, Rect visible)
        {
            Country c = _playerCountry;
            bool repaint = Event.current.type == EventType.Repaint;
            if (_selectedPolicyWebPolicyNode.HasValue) { _policyWebArea = PolicyWebRenderer.GetPolicyArea(_selectedPolicyWebPolicyNode.Value); }

            // ---- the model's web ----
            var byArea = new Dictionary<UiPalette.SystemArea, List<PolicyNodeId>>();
            foreach (PolicyNodeId id in (PolicyNodeId[])System.Enum.GetValues(typeof(PolicyNodeId)))
            {
                UiPalette.SystemArea a = PolicyWebRenderer.GetPolicyArea(id);
                if (!byArea.TryGetValue(a, out List<PolicyNodeId> list)) { list = new List<PolicyNodeId>(); byArea[a] = list; }
                list.Add(id);
            }
            var areas = new List<UiPalette.SystemArea>();
            foreach (UiPalette.SystemArea a in PolicyWebAreaOrder) { if (byArea.ContainsKey(a)) { areas.Add(a); } }
            foreach (UiPalette.SystemArea a in byArea.Keys) { if (!areas.Contains(a)) { areas.Add(a); } }
            if (!byArea.ContainsKey(_policyWebArea) && areas.Count > 0) { _policyWebArea = areas[0]; }
            List<PolicyNodeId> levers = byArea.TryGetValue(_policyWebArea, out List<PolicyNodeId> shown) ? shown : new List<PolicyNodeId>();
            var books = new List<StatNodeId>();
            foreach (StatNodeId id in (StatNodeId[])System.Enum.GetValues(typeof(StatNodeId))) { if (PolicyWebRenderer.HasStat(id)) { books.Add(id); } }
            int leverTotal = System.Enum.GetValues(typeof(PolicyNodeId)).Length;
            int linkTotal = PolicyWebRenderer.GetAllEdges().Count;
            List<StatWebEdge> bookLinks = PolicyWebRenderer.GetAllStatEdges();

            // ---- the card and its head ----
            float pad = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY);
            float headH = V35.Px(V35.CardIcon);
            float wordsH = V35.Px(26f), columnHeadH = V35.Px(22f);
            float pitch = V35.Px(34f);
            float columnsH = Mathf.Max(0f, card.height - PolicyWebChromeHeight());
            float boxH = Mathf.Min(V35.Px(26f), Mathf.Max(1f, columnsH / Mathf.Max(1, Mathf.Max(levers.Count, books.Count)) - V35.Px(4f)));
            if (repaint)
            {
                PoliSimTheme.Rule(card, V35.CardPaper);
                PoliSimTheme.Rule(new Rect(card.x, card.y, card.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(card.x, card.yMax - 1f, card.width, 1f), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(card.x, card.y, 1f, card.height), V35.CardEdge);
                PoliSimTheme.Rule(new Rect(card.xMax - 1f, card.y, 1f, card.height), V35.CardEdge);
            }
            var inner = new Rect(card.x + pad, card.y + padY, card.width - pad * 2f, card.height - padY * 2f);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors);
            var head = new Rect(inner.x, inner.y, inner.width, headH);
            GUIStyle headFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float iconSide = V35.Px(20f);
            if (repaint)
            {
                DrawV35Icon(new Rect(head.x, head.y + Mathf.Round((headH - iconSide) * 0.5f), iconSide, iconSide), "web", area);
                PoliSimWidgets.MeasuredLabel(new Rect(head.x + iconSide + V35.Px(8f), head.y, head.width * 0.5f, headH), "Policy web", headFace);
            }
            SlipAnchor(new Rect(head.x, head.y, iconSide + V35.Px(8f) + headFace.CalcSize(new GUIContent("Policy web")).x, headH), "web:head");
            var headSlip = new SlipContent("POLICY WEB")
                .Add(leverTotal + " LEVERS · " + books.Count + " READINGS · " + linkTotal + " LINKS FROM A LEVER TO A READING · " + bookLinks.Count + " FROM A READING TO A READING")
                .Add("EVERY LINK IS THE MODEL'S OWN: SOLID WHERE ITS FORMULA CARRIES IT, DASHED WHERE IT IS A STATED COUPLING - NONE IS DRAWN FOR THE PAGE")
                .Add("ONE AREA'S LEVERS AT A TIME - THE WORDS ABOVE THE COLUMNS CHOOSE · ALL EIGHTEEN READINGS ON THE RIGHT")
                .Add("A CLICK FOCUSES A LEVER OR A READING: ITS LINKS DRAW DARK, AS HEAVY AS THEY ARE STRONG, IN THE READING'S OWN GOOD OR BAD; THE REST FADE · A SECOND CLICK RELEASES IT")
                .Add("DEBT AND THE TRADE BALANCE KEEP THE NEUTRAL INK");
            if (DeskProvenance.On)
            {
                int derived = 0, declared = 0;
                foreach (PolicyWebEdge e in PolicyWebRenderer.GetAllEdges()) { if (e.Provenance == EdgeProvenance.Derived) { derived++; } else { declared++; } }
                headSlip.Add("DERIVED " + derived + " · DECLARED " + declared + " · NO LINE AUTHORED · NO EDGE INVENTED");
            }
            _lawsSlipBook.Anchors["web:head"] = headSlip;

            // ---- the areas, as words ----
            float y = head.yMax + V35.Px(6f);
            GUIStyle wordFace = V35Serif(V35.Floor, V35.DirectionNeutral);
            GUIStyle wordChosen = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            float x = inner.x;
            foreach (UiPalette.SystemArea a in areas)
            {
                string word = PolicyWebAreaName(a) + " " + byArea[a].Count;
                bool chosen = a == _policyWebArea;
                GUIStyle face = chosen ? wordChosen : wordFace;
                float w = Mathf.Ceil(face.CalcSize(new GUIContent(word)).x);
                if (x + w > inner.xMax) { break; }
                var r = new Rect(x, y, w, wordsH);
                if (PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none) && !chosen)
                {
                    _policyWebArea = a;
                    _selectedPolicyWebPolicyNode = null;   // a lever focused in another area does not travel
                }
                if (repaint)
                {
                    PoliSimWidgets.MeasuredLabel(r, word, face);
                    if (chosen) { PoliSimTheme.Rule(new Rect(r.x, r.yMax - Mathf.Max(1f, V35.Px(2f)), r.width, Mathf.Max(1f, V35.Px(2f))), UiPalette.GetAreaColor(a)); }
                }
                x += w + V35.Px(18f);
            }
            y += wordsH + V35.Px(6f);

            // ---- the column heads ----
            float columnW = Mathf.Round(inner.width * 0.27f);
            var left = new Rect(inner.x, y + columnHeadH, columnW, columnsH);
            var right = new Rect(inner.xMax - columnW, y + columnHeadH, columnW, columnsH);
            GUIStyle capsFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle capsRight = V35Mono(V35.Floor, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            if (repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(left.x, y, columnW, columnHeadH), "YOUR LEVERS", capsFace);
                PoliSimWidgets.MeasuredLabel(new Rect(right.x, y, columnW, columnHeadH), "WHAT THEY MOVE", capsRight);
            }

            // ---- the boxes ----
            bool focusLever = _selectedPolicyWebPolicyNode.HasValue, focusBook = _selectedPolicyWebStatNode.HasValue;
            bool focused = focusLever || focusBook;
            var leverRects = new Dictionary<PolicyNodeId, Rect>();
            var bookRects = new Dictionary<StatNodeId, Rect>();
            float leverPitch = levers.Count > 0 ? columnsH / levers.Count : pitch;
            float bookPitch = books.Count > 0 ? columnsH / books.Count : pitch;
            for (int i = 0; i < levers.Count; i++) { leverRects[levers[i]] = new Rect(left.x, left.y + i * leverPitch + Mathf.Round((leverPitch - boxH) * 0.5f), columnW, boxH); }
            for (int i = 0; i < books.Count; i++) { bookRects[books[i]] = new Rect(right.x, right.y + i * bookPitch + Mathf.Round((bookPitch - boxH) * 0.5f), columnW, boxH); }

            // which boxes a focus lights
            var litLevers = new HashSet<PolicyNodeId>();
            var litBooks = new HashSet<StatNodeId>();
            if (focusLever)
            {
                litLevers.Add(_selectedPolicyWebPolicyNode.Value);
                foreach (PolicyWebEdge e in PolicyWebRenderer.GetEdgesFor(_selectedPolicyWebPolicyNode.Value, c)) { litBooks.Add(e.Target); }
            }
            else if (focusBook)
            {
                litBooks.Add(_selectedPolicyWebStatNode.Value);
                foreach (PolicyWebEdge e in PolicyWebRenderer.GetEdgesForTarget(_selectedPolicyWebStatNode.Value, c)) { litLevers.Add(e.Source); }
            }

            // ---- the links: at rest every one in the hairline; focused, the focus's own dark and weighted, the rest faded ----
            if (repaint)
            {
                float u = V35.Px(1f);
                var dark = new List<PolicyWebEdge>();
                foreach (PolicyNodeId lever in levers)
                {
                    foreach (PolicyWebEdge e in PolicyWebRenderer.GetEdgesFor(lever, c))
                    {
                        if (!bookRects.TryGetValue(e.Target, out Rect to)) { continue; }
                        bool mine = focusLever ? e.Source == _selectedPolicyWebPolicyNode.Value : focusBook && e.Target == _selectedPolicyWebStatNode.Value;
                        if (mine) { dark.Add(e); continue; }
                        Rect from = leverRects[lever];
                        Color ink = PoliSimTheme.TextMuted;
                        ink.a *= focused ? 0.18f : 0.75f;
                        WebLink(new Vector2(from.xMax, from.center.y), new Vector2(to.x, to.center.y), Mathf.Max(1.25f, 1.25f * u), ink, e.Provenance == EdgeProvenance.Derived, visible, false, u);   // 1.25: a one-pixel rotated quad samples into dots at a shallow angle (the first film)
                    }
                }
                foreach (PolicyWebEdge e in dark)
                {
                    Rect from = leverRects[e.Source], to = bookRects[e.Target];
                    float thickness = Mathf.Clamp(1.25f + e.RelativeStrength * 0.6f, 1.25f, 3f) * u;
                    WebLink(new Vector2(from.xMax, from.center.y), new Vector2(to.x, to.center.y), thickness, WebLinkInk(e.Increases, e.Target), e.Provenance == EdgeProvenance.Derived, visible, true, u);
                }
            }

            GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle fadedFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            foreach (PolicyNodeId lever in levers)
            {
                Rect r = leverRects[lever];
                bool isFocus = focusLever && _selectedPolicyWebPolicyNode.Value == lever;
                bool lit = !focused || litLevers.Contains(lever);
                if (PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none))
                {
                    _selectedPolicyWebPolicyNode = isFocus ? (PolicyNodeId?)null : lever;
                    _selectedPolicyWebStatNode = null;
                }
                string name = PolicyWebRenderer.GetPolicyName(lever);
                if (repaint) { WebBox(r, PolicyWebLeverIcon(lever, name), name, lit ? nameFace : fadedFace, isFocus ? UiPalette.GetAreaColor(_policyWebArea) : (Color?)null, lit); }
                SlipAnchor(r, "web:lever:" + lever);
                _lawsSlipBook.Anchors["web:lever:" + lever] = PolicyWebLeverSlip(lever, name, c);
            }
            foreach (StatNodeId book in books)
            {
                Rect r = bookRects[book];
                bool isFocus = focusBook && _selectedPolicyWebStatNode.Value == book;
                bool lit = !focused || litBooks.Contains(book);
                if (PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none))
                {
                    _selectedPolicyWebStatNode = isFocus ? (StatNodeId?)null : book;
                    _selectedPolicyWebPolicyNode = null;
                }
                string name = PolicyWebRenderer.GetStatName(book);
                if (repaint) { WebBox(r, LawsReadingIcon(name), name, lit ? nameFace : fadedFace, isFocus ? V35.DataSlate : (Color?)null, lit); }
                SlipAnchor(r, "web:book:" + book);
                _lawsSlipBook.Anchors["web:book:" + book] = PolicyWebBookSlip(book, name, c);
            }
        }

        private static string PolicyWebAreaName(UiPalette.SystemArea area)
        {
            switch (area)
            {
                case UiPalette.SystemArea.CrimeJustice: return "Crime & justice";
                case UiPalette.SystemArea.SovereignWealth: return "Sovereign wealth";
                case UiPalette.SystemArea.Labor: return "Labour";
                default: return area.ToString();
            }
        }

        /// <summary>A lever's v3.5 icon - by the words of its name where one of the set says what it is, else its area's.</summary>
        private static string PolicyWebLeverIcon(PolicyNodeId lever, string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("income")) { return "person"; }
            if (n.Contains("carbon") || n.Contains("emission")) { return "smoke"; }
            if (n.Contains("corporate")) { return "factory"; }
            if (n.Contains("property") || n.Contains("housing")) { return "home"; }
            if (n.Contains("tariff")) { return "trade"; }
            if (n.Contains("immigration")) { return "passport"; }
            if (n.Contains("border")) { return "gate"; }
            if (n.Contains("drug")) { return "pill"; }
            if (n.Contains("police")) { return "shield"; }
            if (n.Contains("judicial") || n.Contains("court")) { return "scales"; }
            if (n.Contains("bail")) { return "key"; }
            if (n.Contains("sentenc")) { return "gavel"; }
            if (n.Contains("wage")) { return "coins"; }
            if (n.Contains("leave")) { return "pram"; }
            if (n.Contains("family")) { return "family"; }
            if (n.Contains("overtime") || n.Contains("hour")) { return "clock"; }
            if (n.Contains("retrain") || n.Contains("research") || n.Contains("education")) { return "book"; }
            if (n.Contains("health")) { return "heart"; }
            if (n.Contains("pension")) { return "young"; }
            if (n.Contains("defen")) { return "shield"; }
            if (n.Contains("energy") || n.Contains("electric")) { return "bolt"; }
            if (n.Contains("regulat")) { return "scales"; }
            if (n.Contains("credit")) { return "receipt"; }
            if (n.Contains("subsid")) { return "coins"; }
            if (n.Contains("nationali") || n.Contains("deregul")) { return "key"; }
            if (n.Contains("spending") || n.Contains("infrastructure")) { return "out"; }
            switch (PolicyWebRenderer.GetPolicyArea(lever))
            {
                case UiPalette.SystemArea.Fiscal: return "receipt";
                case UiPalette.SystemArea.Labor: return "jobs";
                case UiPalette.SystemArea.CrimeJustice: return "gavel";
                case UiPalette.SystemArea.Welfare: return "bowl";
                case UiPalette.SystemArea.Sectors: return "sectors";
                case UiPalette.SystemArea.SovereignWealth: return "safe";
                case UiPalette.SystemArea.Trade: return "trade";
                case UiPalette.SystemArea.Political: return "ballot";
                default: return "chart";
            }
        }

        /// <summary>A focused link's ink: the reading's own framing of the move (R-W2) - neutral where it holds none, and for debt and the trade balance.</summary>
        private static Color WebLinkInk(bool increases, StatNodeId target)
        {
            if (target == StatNodeId.DebtToGdp || target == StatNodeId.TradeBalance) { return V35.DirectionNeutral; }
            bool? higher = PolicyWebRenderer.GetStatHigherIsBetter(target);
            if (!higher.HasValue) { return V35.DirectionNeutral; }
            return increases == higher.Value ? PoliSimTheme.Good : PoliSimTheme.Bad;
        }

        /// <summary>A box: paper, a hairline edge (the focus's ink, two pixels, where it is focused), the icon and the name.</summary>
        private void WebBox(Rect r, string icon, string name, GUIStyle face, Color? focusInk, bool lit)
        {
            PoliSimTheme.Rule(r, V35.CardPaper);
            Color edge = focusInk ?? V35.CardEdge;
            float line = focusInk.HasValue ? Mathf.Max(1f, V35.Px(2f)) : 1f;
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, line), edge);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - line, r.width, line), edge);
            PoliSimTheme.Rule(new Rect(r.x, r.y, line, r.height), edge);
            PoliSimTheme.Rule(new Rect(r.xMax - line, r.y, line, r.height), edge);
            float side = V35.Px(16f), gap = V35.Px(8f);
            Color iconInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors);
            if (!lit) { iconInk.a *= 0.45f; }
            DrawV35Icon(new Rect(r.x + gap, r.y + Mathf.Round((r.height - side) * 0.5f), side, side), icon, iconInk);
            float textX = r.x + gap + side + gap;
            float textW = Mathf.Max(1f, r.xMax - textX - gap);
            PoliSimWidgets.MeasuredLabel(new Rect(textX, r.y, textW, r.height), V35Fit(name, face, textW, out _), face);
        }

        /// <summary>
        /// A link from a lever's box to a reading's: a straight hairline (the composition's), solid or dashed, with a small head at the reading where it is
        /// focused - every piece clipped to <paramref name="visible"/>, since a rotated quad escapes the scroll view's clip.
        /// </summary>
        private static void WebLink(Vector2 from, Vector2 to, float thickness, Color ink, bool solid, Rect visible, bool head, float u)
        {
            Vector2 d = to - from;
            float length = d.magnitude;
            if (length < 0.5f) { return; }
            Vector2 dir = d / length;
            if (solid) { WebSegment(from, to, thickness, ink, visible); }
            else
            {
                float dash = 6f * u, gap = 4f * u;
                for (float s = 0f; s < length; s += dash + gap) { WebSegment(from + dir * s, from + dir * Mathf.Min(length, s + dash), thickness, ink, visible); }
            }
            if (head)
            {
                float size = 6f * u;
                Vector2 back = to - dir * size;
                Vector2 normal = new Vector2(-dir.y, dir.x) * size * 0.5f;
                WebSegment(to, back + normal, Mathf.Max(1f, 1.5f * u), ink, visible);
                WebSegment(to, back - normal, Mathf.Max(1f, 1.5f * u), ink, visible);
            }
        }

        /// <summary>One straight segment, clipped to <paramref name="clip"/> (Liang–Barsky) before it is drawn as a rotated quad.</summary>
        private static void WebSegment(Vector2 a, Vector2 b, float thickness, Color ink, Rect clip)
        {
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            float[] p = { -d.x, d.x, -d.y, d.y };
            float[] q = { a.x - clip.xMin, clip.xMax - a.x, a.y - clip.yMin, clip.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Approximately(p[i], 0f)) { if (q[i] < 0f) { return; } continue; }
                float t = q[i] / p[i];
                if (p[i] < 0f) { if (t > t1) { return; } if (t > t0) { t0 = t; } }
                else { if (t < t0) { return; } if (t < t1) { t1 = t; } }
            }
            Vector2 s = a + d * t0, e = a + d * t1;
            Vector2 seg = e - s;
            float length = seg.magnitude;
            if (length < 0.5f) { return; }
            float angle = Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, s);
            GUI.color = ink;
            GUI.DrawTexture(new Rect(s.x, s.y - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        /// <summary>A lever's slip: board 2b's pane - the area, the description, the current effects from the live dials, one line per link.</summary>
        private SlipContent PolicyWebLeverSlip(PolicyNodeId lever, string name, Country c)
        {
            List<PolicyWebEdge> edges = PolicyWebRenderer.GetEdgesFor(lever, c);
            var slip = new SlipContent(name.ToUpperInvariant() + " · " + PolicyWebAreaName(PolicyWebRenderer.GetPolicyArea(lever)).ToUpperInvariant() + " · LEVER")
                .Add(PolicyWebRenderer.GetPolicyDescription(lever).ToUpperInvariant());
            foreach (string effect in PolicyWebRenderer.GetCurrentEffectSummary(lever, c)) { slip.Add(effect.ToUpperInvariant()); }
            slip.Add(edges.Count == 0 ? "IT MOVES NO READING IN THE BOOKS" : "IT MOVES " + edges.Count + (edges.Count == 1 ? " READING" : " READINGS") + " - A LINE EACH:");
            foreach (PolicyWebEdge e in edges)
            {
                slip.Add(PolicyWebRenderer.GetStatName(e.Target).ToUpperInvariant() + (e.Increases ? " ▲" : " ▼") + " · "
                    + (e.Provenance == EdgeProvenance.Derived ? "LEDGER: " + (e.LedgerTerm ?? string.Empty).ToUpperInvariant() : "A STATED COUPLING"));
            }
            slip.Add(_selectedPolicyWebPolicyNode.HasValue && _selectedPolicyWebPolicyNode.Value == lever ? "FOCUSED - A SECOND CLICK RELEASES IT" : "A CLICK FOCUSES ITS LINKS");
            return slip;
        }

        /// <summary>A reading's slip: what moves it (twelve at most, the rest counted), its framing, and its links to the other readings.</summary>
        private SlipContent PolicyWebBookSlip(StatNodeId book, string name, Country c)
        {
            List<PolicyWebEdge> incoming = PolicyWebRenderer.GetEdgesForTarget(book, c);
            bool? higher = PolicyWebRenderer.GetStatHigherIsBetter(book);
            var slip = new SlipContent(name.ToUpperInvariant() + " · READING")
                .Add(incoming.Count + (incoming.Count == 1 ? " LEVER MOVES IT" : " LEVERS MOVE IT") + " · "
                    + (book == StatNodeId.DebtToGdp || book == StatNodeId.TradeBalance ? "NEUTRAL INK - NOT A GOOD OR A BAD BY ITSELF" : higher.HasValue ? (higher.Value ? "HIGHER IS BETTER" : "LOWER IS BETTER") : "NO GOOD OR BAD FRAMING"));
            int listed = 0;
            foreach (PolicyWebEdge e in incoming)
            {
                if (listed == 12) { break; }
                slip.Add(PolicyWebRenderer.GetPolicyName(e.Source).ToUpperInvariant() + (e.Increases ? " ▲" : " ▼") + " · " + PolicyWebAreaName(PolicyWebRenderer.GetPolicyArea(e.Source)).ToUpperInvariant());
                listed++;
            }
            if (incoming.Count > listed) { slip.Add("+" + (incoming.Count - listed) + " MORE - FOCUS IT AND CHOOSE AN AREA TO SEE ITS LEVERS' LINKS"); }
            foreach (StatWebEdge e in PolicyWebRenderer.GetStatEdgesFor(book))
            {
                slip.Add((e.Target == book ? "MOVED BY " + PolicyWebRenderer.GetStatName(e.Source) : "FEEDS " + PolicyWebRenderer.GetStatName(e.Target)).ToUpperInvariant()
                    + (e.Increases ? " ▲" : " ▼") + " · LEDGER: " + (e.LedgerTerm ?? string.Empty).ToUpperInvariant());
            }
            return slip;
        }
    }
}
