using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §666 (UI v3.3 §4.1, board 19b): **THE SLIP - A TWO-LEVEL PINNABLE TOOLTIP.** Level 1 opens after 250 ms at rest on an anchor, below-right of
    /// the pointer, and stays open while the pointer is on the anchor or on the slip; a marked term in it (dotted underline) opens level 2 beside it
    /// after 250 ms at rest; a click on a slip's head pins the chain (the level-1 slip and the level 2 open under it) - three pinned chains a page, the
    /// oldest let go - and a click on a pinned head lets it go. A slip never commits anything. The contents are a book's (`PeopleSlips`), so what
    /// the page opens is what `PeopleSlipReachabilityCheck` reads.
    /// </summary>
    public partial class GameController
    {
        private const float SlipDelay = 0.25f;
        private const int SlipPinLimit = 3;

        private readonly List<(string Id, Rect Rect)> _slipAnchors = new List<(string Id, Rect Rect)>();
        private string _slipHoverId;
        private float _slipHoverSince;
        private string _slipOpenId;
        private Vector2 _slipOpenAt;
        private Rect _slipOpenRect;
        private string _slipTermHover;
        private float _slipTermSince;
        private string _slipTermOpen;
        private Rect _slipTermRect;
        private readonly List<(string Anchor, string Term, Vector2 At)> _slipPins = new List<(string Anchor, string Term, Vector2 At)>();

        /// <summary>Called at the head of a page that carries slips: a repaint re-registers every anchor.</summary>
        private void BeginSlipAnchors() { if (Event.current.type == EventType.Repaint) { _slipAnchors.Clear(); } }

        /// <summary>An anchor: the rect a slip opens from, registered on the repaint that draws it.</summary>
        private void SlipAnchor(Rect r, string id) { if (Event.current.type == EventType.Repaint && r.width > 0f && r.height > 0f) { _slipAnchors.Add((id, r)); } }

        /// <summary>The film's hook: a pinned chain, drawn whatever the pointer does (a pointer's slip cannot be filmed, a pinned one can).</summary>
        private void PinSlipForFilm(string anchor, string term, Vector2 at)
        {
            _slipPins.RemoveAll(p => p.Anchor == anchor);
            _slipPins.Add((anchor, term, at));
            while (_slipPins.Count > SlipPinLimit) { _slipPins.RemoveAt(0); }
        }

        /// <summary>Draws the pinned chains and the live one over the page, and takes the slip's own clicks. <paramref name="bounds"/> is the page's
        /// width the slips keep inside.</summary>
        private void DrawSlips(PeopleSlips.Book book, Rect bounds)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint && e.type != EventType.MouseDown) { return; }
            Vector2 mouse = e.mousePosition;
            float now = Time.realtimeSinceStartup;

            // The pinned chains first: a click on a pinned head lets it go.
            for (int i = _slipPins.Count - 1; i >= 0; i--)
            {
                (string anchor, string term, Vector2 at) = _slipPins[i];
                if (!book.Anchors.TryGetValue(anchor, out SlipContent pinned)) { continue; }
                Rect box = SlipBox(pinned, at, bounds, pinned: true, out Rect head, out List<(Rect Rect, string Term)> _);
                if (term != null && book.Terms.TryGetValue(term, out SlipContent pinnedTerm)) { SlipBox(pinnedTerm, new Vector2(box.xMax + 6f, box.y), bounds, pinned: false, out Rect _, out List<(Rect Rect, string Term)> _); }
                if (e.type == EventType.MouseDown && head.Contains(mouse)) { _slipPins.RemoveAt(i); e.Use(); return; }
            }

            // §685: a FILM never opens a pointer's slip - the harness parks the cursor, but IMGUI keeps the last position it saw, and a slip that opens
            // on the wall clock's 250 ms is a frame two films of one code can disagree on (s685_2560 filmed one). The pinned chains above are the film's hook.
            if (PoliSim.Testing.CaptureIdentity.Armed) { return; }

            // Level 1: the anchor under the pointer, after the delay; kept while the pointer is on the anchor, the slip or its level 2.
            string hovered = null;
            foreach ((string id, Rect r) in _slipAnchors) { if (r.Contains(mouse)) { hovered = id; break; } }
            bool onOpen = _slipOpenId != null && (_slipOpenRect.Contains(mouse) || (_slipTermOpen != null && _slipTermRect.Contains(mouse)));
            if (!onOpen)
            {
                if (hovered != _slipHoverId) { _slipHoverId = hovered; _slipHoverSince = now; }
                if (hovered == null) { _slipOpenId = null; _slipTermOpen = null; _slipTermHover = null; }
                else if (now - _slipHoverSince >= SlipDelay && hovered != _slipOpenId) { _slipOpenId = hovered; _slipOpenAt = mouse + new Vector2(12f, 12f); _slipTermOpen = null; _slipTermHover = null; }
            }
            if (_slipOpenId == null || !book.Anchors.TryGetValue(_slipOpenId, out SlipContent open)) { return; }

            _slipOpenRect = SlipBox(open, _slipOpenAt, bounds, pinned: false, out Rect openHead, out List<(Rect Rect, string Term)> terms);
            // Level 2: a marked term under the pointer, after the delay; kept while the pointer is on that term or on the level-2 slip.
            string termHovered = null;
            foreach ((Rect r, string t) in terms) { if (r.Contains(mouse)) { termHovered = t; break; } }
            if (!(_slipTermOpen != null && _slipTermRect.Contains(mouse)))
            {
                if (termHovered != _slipTermHover) { _slipTermHover = termHovered; _slipTermSince = now; }
                if (termHovered != null && now - _slipTermSince >= SlipDelay) { _slipTermOpen = termHovered; }
                else if (termHovered == null && !_slipOpenRect.Contains(mouse)) { _slipTermOpen = null; }
            }
            if (_slipTermOpen != null && book.Terms.TryGetValue(_slipTermOpen, out SlipContent termSlip))
            {
                _slipTermRect = SlipBox(termSlip, new Vector2(_slipOpenRect.xMax + 6f, _slipOpenRect.y), bounds, pinned: false, out Rect _, out List<(Rect Rect, string Term)> _);
            }

            // A click on the live head pins the chain.
            if (e.type == EventType.MouseDown && openHead.Contains(mouse))
            {
                PinSlipForFilm(_slipOpenId, _slipTermOpen, _slipOpenAt);
                _slipOpenId = null; _slipTermOpen = null;
                e.Use();
            }
        }

        /// <summary>One slip's box at <paramref name="at"/> (kept inside <paramref name="bounds"/>): the head, then the lines, the marked terms dotted
        /// underneath; returns the box, and gives the head's rect and each marked term's rect. Draws on a repaint only; measures always.</summary>
        private Rect SlipBox(SlipContent c, Vector2 at, Rect bounds, bool pinned, out Rect head, out List<(Rect Rect, string Term)> terms)
        {
            terms = new List<(Rect Rect, string Term)>();
            GUIStyle headStyle = DeskCaption(8f, PoliSimTheme.TextPrimary, true);
            GUIStyle lineStyle = DeskCaption(7.5f, PoliSimTheme.TextSecondary);
            float lineH = Mathf.Ceil(DeskCaptionHeight(lineStyle)) + StatsUnit(2f);
            float pad = StatsUnit(8f), pinSide = StatsUnit(10f);
            float w = headStyle.CalcSize(new GUIContent(SlipContent.Plain(c.Head))).x + pinSide + StatsUnit(6f);
            foreach (string l in c.Lines) { w = Mathf.Max(w, lineStyle.CalcSize(new GUIContent(SlipContent.Plain(l))).x); }
            w += pad * 2f;
            float h = lineH * (c.Lines.Count + 1) + StatsUnit(10f);
            float x = Mathf.Min(at.x, bounds.xMax - w);
            var box = new Rect(Mathf.Max(bounds.x, x), at.y, w, h);
            head = new Rect(box.x, box.y, box.width, lineH + StatsUnit(5f));
            bool paint = Event.current.type == EventType.Repaint;
            if (paint)
            {
                PoliSimTheme.Rule(box, PoliSimTheme.Card);
                Color edge = pinned ? PoliSimTheme.TextSecondary : PoliSimTheme.HairlineStrong;
                PoliSimTheme.Rule(new Rect(box.x, box.y, box.width, 1f), edge);
                PoliSimTheme.Rule(new Rect(box.x, box.yMax - 1f, box.width, 1f), edge);
                PoliSimTheme.Rule(new Rect(box.x, box.y, 1f, box.height), edge);
                PoliSimTheme.Rule(new Rect(box.xMax - 1f, box.y, 1f, box.height), edge);
            }
            float y = box.y + StatsUnit(5f);
            DrawSlipLine(new Rect(box.x + pad, y, w - pad * 2f - pinSide - StatsUnit(6f), lineH), c.Head, headStyle, terms, paint);
            if (paint) { SymbolRegistry.Draw(new Rect(box.xMax - pad - pinSide, y + (lineH - pinSide) * 0.5f, pinSide, pinSide), Symbol.Pin, pinned ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, lineStyle); }
            foreach (string l in c.Lines) { y += lineH; DrawSlipLine(new Rect(box.x + pad, y, w - pad * 2f, lineH), l, lineStyle, terms, paint); }
            return box;
        }

        private static void DrawSlipLine(Rect r, string line, GUIStyle style, List<(Rect Rect, string Term)> terms, bool paint)
        {
            string plain = SlipContent.Plain(line);
            if (paint) { PoliSimWidgets.MeasuredLabel(r, plain, style); }
            int searchFrom = 0;
            foreach (string term in SlipContent.TermsIn(line))
            {
                int at = plain.IndexOf(term, searchFrom, System.StringComparison.Ordinal);
                if (at < 0) { continue; }
                float x0 = r.x + style.CalcSize(new GUIContent(plain.Substring(0, at))).x;
                float tw = style.CalcSize(new GUIContent(term)).x;
                var tr = new Rect(x0, r.y, tw, r.height);
                terms.Add((tr, term));
                if (paint) { DrawDashedRule(new Rect(tr.x, tr.yMax - 2f, tr.width, 1f), PoliSimTheme.TextSecondary, 2f, 2f); }
                searchFrom = at + term.Length;
            }
        }
    }
}
