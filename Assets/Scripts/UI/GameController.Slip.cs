using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §666 (UI v3.3 §4.1, board 19b): **THE SLIP - A TWO-LEVEL PINNABLE TOOLTIP.** Level 1 opens after 250 ms at rest on an anchor and stays open
    /// while the pointer is on the anchor or on the slip; a marked term in it (dotted underline) opens level 2 beside it after 250 ms at rest. A slip
    /// never commits anything. The contents are a book's (`PeopleSlips`, `StatsSlips`), so what the page opens is what the reachability checks read.
    ///
    /// <para><b>§733 (UI v3.5, V35_ASK rule 2): THE SLIP AT v3.5.</b> The composition's slip: its paper (#F6EFDF) inside a 1 px edge (#8A7A5C) with a
    /// 2 px shadow; a 30 px head row - the glyph where the slip is a glyph's (its word is the head), the head in the serif at 15 bold, cut with an
    /// ellipsis where it does not fit, the pin's number when pinned, and the pin cell; the lines in the serif AT THE 14 px FLOOR (they were ~10 px),
    /// wrapping inside a slip at most 470 px wide. It opens UNDER its anchor and flips above it where the page has no room below; level 2 opens
    /// beside it, flipping left at the page's edge. <b>Pins</b>: a click on the head row (the pin cell at its right) pins the chain; a pin takes the
    /// first free number of ①–③ - on the slip's head and as a badge on its anchor - and a fourth lets the oldest go; a click on a pinned head lets
    /// it go; <b>ESC lets the newest go</b>; and the pins are <b>cleared on leaving the page</b> (<see cref="KeepSlipPinsToPage"/>).</para>
    /// </summary>
    public partial class GameController
    {
        private const float SlipDelay = 0.25f;
        private const int SlipPinLimit = 3;
        private static readonly string[] SlipPinNumbers = { "①", "②", "③" };

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
        private readonly List<(string Anchor, string Term, Vector2 At, int Number)> _slipPins = new List<(string Anchor, string Term, Vector2 At, int Number)>();

        /// <summary>§733: the page the pins were made on - the sheet that shows and its sub-tab.</summary>
        private (bool Formation, bool CampaignMap, bool Campaign, bool Desk, ConsolidatedTab Tab, StatisticsCategory Statistics, PolicyLawsCategory Laws,
            PoliticsCategory Politics, BudgetProcessCategory Budget) _slipPinsPage;

        /// <summary>§733 (V35_ASK rule 2): the pins are cleared on leaving the page - called once a pass, before the page draws.</summary>
        private void KeepSlipPinsToPage()
        {
            var page = (_formationSheetOpen, _campaignMapScreen.HasValue, _campaignScreen.HasValue, _onDesk, _consolidatedTab, _statisticsCategory,
                _policyLawsCategory, _politicsCategory, _budgetProcessCategory);
            if (page.Equals(_slipPinsPage)) { return; }
            _slipPinsPage = page;
            _slipPins.Clear();
            _slipOpenId = null;
            _slipTermOpen = null;
        }

        /// <summary>Called at the head of a page that carries slips: a repaint re-registers every anchor.</summary>
        private void BeginSlipAnchors() { if (Event.current.type == EventType.Repaint) { _slipAnchors.Clear(); } }

        /// <summary>An anchor: the rect a slip opens from, registered on the repaint that draws it.</summary>
        private void SlipAnchor(Rect r, string id) { if (Event.current.type == EventType.Repaint && r.width > 0f && r.height > 0f) { _slipAnchors.Add((id, r)); } }

        /// <summary>A pin, as a click makes it (and the film's hook - a pointer's slip cannot be filmed, a pinned one can): the chain takes the first free
        /// number of the three, and a fourth lets the oldest go.</summary>
        private void PinSlipForFilm(string anchor, string term, Vector2 at)
        {
            _slipPins.RemoveAll(p => p.Anchor == anchor);
            if (_slipPins.Count >= SlipPinLimit) { _slipPins.RemoveAt(0); }
            int number = 1;
            while (_slipPins.Exists(p => p.Number == number)) { number++; }
            _slipPins.Add((anchor, term, at, number));
        }

        /// <summary>Draws the pinned chains and the live one over the page, and takes the slip's own clicks and ESC. <paramref name="bounds"/> is the
        /// page the slips keep inside.</summary>
        private void DrawSlips(PeopleSlips.Book book, Rect bounds)
        {
            Event e = Event.current;
            // V35 rule 2: ESC lets the newest pin go
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _slipPins.Count > 0) { _slipPins.RemoveAt(_slipPins.Count - 1); e.Use(); return; }
            if (e.type != EventType.Repaint && e.type != EventType.MouseDown) { return; }
            Vector2 mouse = e.mousePosition;
            float now = Time.realtimeSinceStartup;

            // The pins' badges on their anchors, then the pinned chains: a click on a pinned head lets it go.
            if (e.type == EventType.Repaint)
            {
                foreach ((string anchor, string _, Vector2 at, int number) in _slipPins)
                {
                    Rect? on = AnchorRectOf(anchor, at);
                    if (on.HasValue && book.Anchors.ContainsKey(anchor)) { DrawSlipBadge(on.Value, number, bounds); }
                }
            }
            for (int i = _slipPins.Count - 1; i >= 0; i--)
            {
                (string anchor, string term, Vector2 at, int number) = _slipPins[i];
                if (!book.Anchors.TryGetValue(anchor, out SlipContent pinned)) { continue; }
                Rect box = SlipBox(pinned, at, bounds, number, out Rect head, out List<(Rect Rect, string Term)> _, AnchorRectOf(anchor, at), null);
                if (term != null && book.Terms.TryGetValue(term, out SlipContent pinnedTerm)) { SlipBox(pinnedTerm, at, bounds, 0, out Rect _, out List<(Rect Rect, string Term)> _, null, box); }
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
                else if (now - _slipHoverSince >= SlipDelay && hovered != _slipOpenId) { _slipOpenId = hovered; _slipOpenAt = mouse; _slipTermOpen = null; _slipTermHover = null; }
            }
            if (_slipOpenId == null || !book.Anchors.TryGetValue(_slipOpenId, out SlipContent open)) { return; }

            _slipOpenRect = SlipBox(open, _slipOpenAt, bounds, 0, out Rect openHead, out List<(Rect Rect, string Term)> terms, AnchorRectOf(_slipOpenId, _slipOpenAt), null);
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
                _slipTermRect = SlipBox(termSlip, _slipOpenAt, bounds, 0, out Rect _, out List<(Rect Rect, string Term)> _, null, _slipOpenRect);
            }

            // A click on the live head row (its pin cell at the right) pins the chain.
            if (e.type == EventType.MouseDown && openHead.Contains(mouse))
            {
                PinSlipForFilm(_slipOpenId, _slipTermOpen, _slipOpenAt);
                _slipOpenId = null; _slipTermOpen = null;
                e.Use();
            }
        }

        /// <summary>§708 (D-ST's return, flag 3): the rect the anchor <paramref name="id"/> registered on this repaint nearest <paramref name="near"/>
        /// (one id can anchor several cells - the three ABSENT cards share "card:history" - and the slip opens on the one it was opened on), or null.</summary>
        private Rect? AnchorRectOf(string id, Vector2 near)
        {
            if (id == null) { return null; }
            Rect? best = null;
            float bestDistance = float.MaxValue;
            foreach ((string anchorId, Rect r) in _slipAnchors)
            {
                if (anchorId != id) { continue; }
                float dx = Mathf.Max(0f, Mathf.Max(r.xMin - near.x, near.x - r.xMax)), dy = Mathf.Max(0f, Mathf.Max(r.yMin - near.y, near.y - r.yMax));
                float distance = dx * dx + dy * dy;
                if (distance < bestDistance) { best = r; bestDistance = distance; }
            }
            return best;
        }

        /// <summary>§708: the film's pin as a player's is made - a click on the anchor nearest <paramref name="near"/> (its left end, mid-height),
        /// so the slip opens on its anchor. Pins at <paramref name="near"/> itself where no such anchor drew.</summary>
        private void PinSlipOnAnchorForFilm(string anchor, Vector2 near)
        {
            Rect? r = AnchorRectOf(anchor, near);
            PinSlipForFilm(anchor, null, r.HasValue ? new Vector2(r.Value.x + Mathf.Min(6f, r.Value.width * 0.5f), r.Value.center.y) : near);
        }

        /// <summary>The glyph a slip's head IS - a glyph's slip is headed by its word (19a) - or null.</summary>
        private static Symbol? SlipGlyphOf(string head)
        {
            foreach (Symbol s in new[] { Symbol.Absent, Symbol.Billed, Symbol.Dated, Symbol.Provisional }) { if (head == SymbolRegistry.Word(s)) { return s; } }
            return null;
        }

        /// <summary>§733: a pin's badge on its anchor - the number in a dark disc at the anchor's top-right corner (the composition's 18 px).</summary>
        private void DrawSlipBadge(Rect anchor, int number, Rect bounds)
        {
            float side = V35.Px(18f);
            float x = Mathf.Clamp(anchor.xMax - V35.Px(6f), bounds.x, bounds.xMax - side), y = Mathf.Clamp(anchor.y - V35.Px(9f), bounds.y, bounds.yMax - side);
            var disc = new Rect(x, y, side, side);
            PoliSimTheme.Pill(disc, PoliSimTheme.TextPrimary);
            PoliSimWidgets.MeasuredLabel(disc, number.ToString(System.Globalization.CultureInfo.InvariantCulture), V35Serif(V35.Floor, V35.SlipPaper, TextAnchor.MiddleCenter));
        }

        /// <summary>
        /// One slip's box (§733, the composition's): under <paramref name="anchor"/> where one is given (above it where the page has no room below),
        /// beside <paramref name="beside"/> for a level 2 (left of it at the page's edge), else at <paramref name="at"/>; kept inside
        /// <paramref name="bounds"/>. The head row, then the lines wrapping inside the box, the marked terms dotted underneath. Returns the box, and
        /// gives the head row's rect (the pin's target) and each marked term's rect. <paramref name="pin"/> is the pin's number, 0 for a live slip.
        /// Draws on a repaint only; measures always.
        /// </summary>
        private Rect SlipBox(SlipContent c, Vector2 at, Rect bounds, int pin, out Rect head, out List<(Rect Rect, string Term)> terms, Rect? anchor, Rect? beside)
        {
            terms = new List<(Rect Rect, string Term)>();
            GUIStyle headFace = V35Serif(15f, PoliSimTheme.TextPrimary);
            headFace.fontStyle = FontStyle.Bold;
            GUIStyle lineFace = V35SerifWrapped(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle numberFace = V35Serif(15f, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            float padX = V35.Px(10f), headHeight = V35.Px(30f), cell = V35.Px(24f), glyphSide = V35.Px(16f), gap = V35.Px(8f);
            Symbol? glyph = SlipGlyphOf(c.Head);
            string headText = SlipContent.Plain(c.Head);
            string numberText = pin >= 1 && pin <= SlipPinNumbers.Length ? SlipPinNumbers[pin - 1] : null;
            float numberWidth = numberText != null ? Mathf.Ceil(numberFace.CalcSize(new GUIContent(numberText)).x) + V35.Px(2f) : 0f;
            float headRun = padX + (glyph.HasValue ? glyphSide + gap : 0f) + Mathf.Ceil(headFace.CalcSize(new GUIContent(headText)).x) + gap
                + (numberText != null ? numberWidth + gap : 0f) + cell + V35.Px(6f);

            // The width: the longest of the head and the lines on one line, held to the composition's 180-470 px and to the page.
            GUIStyle oneLine = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            float w = headRun;
            foreach (string l in c.Lines) { w = Mathf.Max(w, Mathf.Ceil(oneLine.CalcSize(new GUIContent(SlipContent.Plain(l))).x) + padX * 2f + V35.Px(2f)); }
            w = Mathf.Min(Mathf.Clamp(w, V35.Px(180f), V35.Px(470f)), Mathf.Max(V35.Px(180f), bounds.width));
            float textWidth = w - padX * 2f;
            var lineHeights = new float[c.Lines.Count];
            float h = headHeight + V35.Px(5f) + V35.Px(8f);
            for (int i = 0; i < c.Lines.Count; i++) { lineHeights[i] = Mathf.Ceil(lineFace.CalcHeight(new GUIContent(SlipContent.Plain(c.Lines[i])), textWidth)); h += lineHeights[i]; }

            float x, y;
            if (beside.HasValue)
            {
                x = beside.Value.xMax + V35.Px(6f);
                if (x + w > bounds.xMax) { x = beside.Value.x - V35.Px(6f) - w; }
                y = beside.Value.y;
            }
            else if (anchor.HasValue)
            {
                x = anchor.Value.x;
                y = anchor.Value.yMax + V35.Px(6f);
                if (y + h > bounds.yMax && anchor.Value.y - V35.Px(6f) - h >= bounds.y) { y = anchor.Value.y - V35.Px(6f) - h; }
            }
            else { x = at.x; y = at.y; }
            x = Mathf.Max(bounds.x, Mathf.Min(x, bounds.xMax - w));
            y = Mathf.Max(bounds.y, Mathf.Min(y, bounds.yMax - h));
            var box = new Rect(x, y, w, h);
            head = new Rect(box.x, box.y, box.width, headHeight);
            bool paint = Event.current.type == EventType.Repaint;
            if (paint)
            {
                PoliSimTheme.Rule(new Rect(box.x + 2f, box.y + 2f, box.width, box.height), V35.SlipShadow);
                PoliSimTheme.Rule(box, V35.SlipPaper);
                PoliSimTheme.Rule(new Rect(box.x, box.y, box.width, 1f), V35.SlipEdge);
                PoliSimTheme.Rule(new Rect(box.x, box.yMax - 1f, box.width, 1f), V35.SlipEdge);
                PoliSimTheme.Rule(new Rect(box.x, box.y, 1f, box.height), V35.SlipEdge);
                PoliSimTheme.Rule(new Rect(box.xMax - 1f, box.y, 1f, box.height), V35.SlipEdge);
                PoliSimTheme.Rule(new Rect(box.x + 1f, box.y + headHeight, box.width - 2f, 1f), V35.CardEdge);

                // the head row: the glyph, the head (cut where it does not fit), the pin's number, the pin cell
                float hx = box.x + padX;
                if (glyph.HasValue)
                {
                    DrawStateGlyph(new Rect(hx, box.y + Mathf.Round((headHeight - glyphSide) * 0.5f), glyphSide, glyphSide), glyph.Value, PoliSimTheme.TextPrimary);
                    hx += glyphSide + gap;
                }
                var pinCell = new Rect(box.xMax - V35.Px(6f) - cell, box.y + Mathf.Round((headHeight - cell) * 0.5f), cell, cell);
                float headRight = pinCell.x - gap - (numberText != null ? numberWidth + gap : 0f);
                PoliSimWidgets.MeasuredLabel(new Rect(hx, box.y, Mathf.Max(1f, headRight - hx), headHeight), V35Fit(headText, headFace, Mathf.Max(1f, headRight - hx), out _), headFace);
                if (numberText != null) { PoliSimWidgets.MeasuredLabel(new Rect(pinCell.x - gap - numberWidth, box.y, numberWidth, headHeight), numberText, numberFace); }
                float pinSide = V35.Px(14f);
                SymbolRegistry.Draw(new Rect(pinCell.x + (cell - pinSide) * 0.5f, pinCell.y + (cell - pinSide) * 0.5f, pinSide, pinSide), Symbol.Pin,
                    pin > 0 ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, V35Serif(V35.Floor, PoliSimTheme.TextMuted));
            }
            float ly = box.y + headHeight + V35.Px(5f);
            for (int i = 0; i < c.Lines.Count; i++)
            {
                DrawSlipLine(new Rect(box.x + padX, ly, textWidth, lineHeights[i]), c.Lines[i], lineFace, oneLine, terms, paint);
                ly += lineHeights[i];
            }
            return box;
        }

        /// <summary>One line of a slip, wrapping inside <paramref name="r"/>; each marked term's rect found where the wrap put it (its first line where
        /// it breaks), dotted underneath.</summary>
        private static void DrawSlipLine(Rect r, string line, GUIStyle face, GUIStyle oneLine, List<(Rect Rect, string Term)> terms, bool paint)
        {
            string plain = SlipContent.Plain(line);
            var content = new GUIContent(plain);
            if (paint) { GUI.Label(r, content, face); }
            float rowHeight = Mathf.Ceil(oneLine.CalcSize(new GUIContent("Ag")).y);
            int searchFrom = 0;
            foreach (string term in SlipContent.TermsIn(line))
            {
                int at = plain.IndexOf(term, searchFrom, System.StringComparison.Ordinal);
                if (at < 0) { continue; }
                Vector2 start = face.GetCursorPixelPosition(r, content, at);
                Vector2 end = face.GetCursorPixelPosition(r, content, at + term.Length);
                float right = Mathf.Abs(end.y - start.y) < 1f ? end.x : r.xMax;
                var tr = new Rect(start.x, start.y, Mathf.Max(1f, right - start.x), rowHeight);
                terms.Add((tr, term));
                if (paint) { DrawDashedRule(new Rect(tr.x, tr.yMax - 2f, tr.width, 1f), PoliSimTheme.TextPrimary, 1f, 2f); }
                searchFrom = at + term.Length;
            }
        }
    }
}
