using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using PoliSim.Data;

namespace PoliSim.UI
{
    /// <summary>
    /// Board 2b (D7 / D9 row 1; P3-B1, built 2026-09-03 against Elias's images 7 and 8 - `COMPLETED.md` §263): THE POLICY
    /// WEB, DRAWN TO BE READ. The same nodes and edges the ring drew (`PolicyWebRenderer`'s data, untouched), in the
    /// board's composition:
    /// <list type="bullet">
    /// <item>the LEVERS on the left, grouped by area - a name, a count and a rule per group, one dot per lever;</item>
    /// <item>THE BOOKS on the right - one row per stat with the number of lever lines into it and whether it is in the history;</item>
    /// <item>THE CAUSAL GRAPH - a band under the levers, the books as a row of dots and every derived stat→stat link as an arc;</item>
    /// <item>at rest nothing but dots, names and dividers - no edge draws until a node is PINNED;</item>
    /// <item>pinned: the lever's own lines draw to the books and nothing else does, the band yields to a PANE on the right
    /// (the name, the area, the description verbatim, CURRENT EFFECTS from the live dials, MOVES - one line per edge with its
    /// ledger term or DECLARED), and a click on the paper unpins.</item>
    /// </list>
    /// R-W2's fence holds: no edge invented, no valence hue beyond the target's own good/bad framing, no grouping the model
    /// does not hold. The dots' inks are the board's own two (lever purple, book blue), stated here as the board's tokens.
    /// </summary>
    public sealed class PolicyWebBoard
    {
        /// <summary>Board 2b's lever ink.</summary>
        public static readonly Color LeverInk = PoliSimTheme.Hex(0x5B4B9E);
        /// <summary>Board 2b's book ink.</summary>
        public static readonly Color BookInk = PoliSimTheme.Hex(0x4E7E9B);

        private static readonly UiPalette.SystemArea[] AreaOrder =
        {
            UiPalette.SystemArea.Fiscal, UiPalette.SystemArea.Labor, UiPalette.SystemArea.CrimeJustice, UiPalette.SystemArea.Welfare,
            UiPalette.SystemArea.Sectors, UiPalette.SystemArea.SovereignWealth, UiPalette.SystemArea.Trade, UiPalette.SystemArea.Political,
        };

        private static Texture2D _disc;
        private readonly Dictionary<PolicyNodeId, Vector2> _leverCentres = new Dictionary<PolicyNodeId, Vector2>();
        private readonly Dictionary<StatNodeId, Vector2> _bookCentres = new Dictionary<StatNodeId, Vector2>();

        /// <summary>The height the board needs at this width, so the caller can reserve it (the sheet scrolls when the window is shorter).</summary>
        public float NeededHeight(float width, GUIStyle labelStyle, bool pinned)
        {
            float u = Unit(labelStyle);
            Layout(new Rect(0f, 0f, width, 10000f), labelStyle, pinned, out Rect _, out Rect _, out Rect _, out Rect band, out Rect legend);
            return (pinned ? legend.yMax : Mathf.Max(band.yMax, legend.yMax)) + 6f * u;
        }

        public void Draw(Rect rect, GUIStyle labelStyle, GUIStyle headerStyle, GUIStyle monoBase, Country country,
            PolicyNodeId? pinnedPolicy, StatNodeId? pinnedStat,
            out PolicyNodeId? clickedPolicy, out StatNodeId? clickedStat, out bool clickedEmptySpace)
        {
            clickedPolicy = null; clickedStat = null; clickedEmptySpace = false;
            bool pinned = pinnedPolicy.HasValue || pinnedStat.HasValue;
            float u = Unit(labelStyle);
            GUIStyle mono = Mono(monoBase, labelStyle, 0.64f, PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
            GUIStyle monoRight = Mono(monoBase, labelStyle, 0.64f, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
            GUIStyle monoInk = Mono(monoBase, labelStyle, 0.64f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            GUIStyle groupName = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false, fontSize = Mathf.RoundToInt(labelStyle.fontSize * 1.05f) };
            groupName.normal.textColor = PoliSimTheme.TextPrimary;
            GUIStyle bookName = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            bookName.normal.textColor = PoliSimTheme.TextPrimary;
            GUIStyle paneTitle = new GUIStyle(headerStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            GUIStyle paneBody = new GUIStyle(labelStyle) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            paneBody.normal.textColor = PoliSimTheme.TextPrimary;

            Layout(rect, labelStyle, pinned, out Rect status, out Rect levers, out Rect books, out Rect band, out Rect legend);
            Rect pane = pinned ? new Rect(books.xMax + 4f * u, books.y, rect.xMax - books.xMax - 4f * u, legend.y - books.y) : Rect.zero;

            bool repaint = Event.current.type == EventType.Repaint;
            _leverCentres.Clear(); _bookCentres.Clear();

            // The status line: the census on the left, the state on the right.
            int leverCount = Enum.GetValues(typeof(PolicyNodeId)).Length;
            int bookCount = Stats().Length;
            int linkCount = PolicyWebRenderer.GetAllEdges().Count;
            int statLinkCount = PolicyWebRenderer.GetAllStatEdges().Count;
            string left = pinnedPolicy.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "PINNED — {0} · ITS {1} LINE(S) DRAW, NOTHING ELSE DOES", PolicyWebRenderer.GetPolicyName(pinnedPolicy.Value).ToUpperInvariant(), PolicyWebRenderer.GetEdgesFor(pinnedPolicy.Value, country).Count)
                : pinnedStat.HasValue
                    ? string.Format(CultureInfo.InvariantCulture, "PINNED — {0} · THE {1} LINE(S) INTO IT DRAW, NOTHING ELSE DOES", PolicyWebRenderer.GetStatName(pinnedStat.Value).ToUpperInvariant(), PolicyWebRenderer.GetEdgesForTarget(pinnedStat.Value, country).Count)
                    : string.Format(CultureInfo.InvariantCulture, "{0} LEVERS · {1} BOOKS · {2} LEVER→BOOK LINKS · {3} BOOK→BOOK", leverCount, bookCount, linkCount, statLinkCount);
            string right = pinned ? "THE CAUSAL BAND YIELDS TO THE PANE · CLICK THE PAPER TO UNPIN" : "AT REST: DOTS, NAMES, DIVIDERS · NO EDGE DRAWS UNTIL A NODE IS PINNED";
            PoliSimWidgets.MeasuredLabel(new Rect(status.x, status.y, status.width * 0.55f, status.height), left, mono);
            PoliSimWidgets.MeasuredLabel(new Rect(status.x + status.width * 0.55f, status.y, status.width * 0.45f, status.height), right, monoRight);

            // THE LEVERS, grouped by area.
            float dot = Mathf.Round(8f * u);
            float pitchX = Mathf.Round(26f * u);
            float pitchY = Mathf.Round(22f * u);
            float groupHead = Mathf.Round(22f * u);
            int perRow = Mathf.Max(1, Mathf.FloorToInt((levers.width - 4f * u) / pitchX));
            float y = levers.y;
            var byArea = LeversByArea();
            foreach (UiPalette.SystemArea area in AreaKeys(byArea))
            {
                List<PolicyNodeId> group = byArea[area];
                float nameWidth = groupName.CalcSize(new GUIContent(AreaName(area))).x + 4f * u;
                Rect groupHeader = new Rect(levers.x, y, levers.width, groupHead);
                PoliSimWidgets.MeasuredLabel(new Rect(levers.x, y, Mathf.Min(nameWidth, levers.width * 0.6f), groupHead), AreaName(area), groupName);
                PoliSimWidgets.MeasuredLabel(new Rect(levers.x + Mathf.Min(nameWidth, levers.width * 0.6f), y, 40f * u, groupHead), group.Count.ToString(CultureInfo.InvariantCulture), mono);
                y += groupHead;
                for (int i = 0; i < group.Count; i++)
                {
                    int row = i / perRow, col = i % perRow;
                    var centre = new Vector2(levers.x + 4f * u + col * pitchX + dot * 0.5f, y + row * pitchY + pitchY * 0.5f);
                    _leverCentres[group[i]] = centre;
                    bool isPinned = pinnedPolicy.HasValue && pinnedPolicy.Value == group[i];
                    bool feedsPinnedStat = pinnedStat.HasValue && Feeds(group[i], pinnedStat.Value, country);
                    bool lit = !pinned || isPinned || feedsPinnedStat;
                    if (repaint)
                    {
                        if (isPinned) { Disc(centre, dot + 6f * u, PoliSimTheme.TextPrimary); Disc(centre, dot + 3f * u, PoliSimTheme.Card); }
                        Disc(centre, dot, lit ? LeverInk : Fade(LeverInk, 0.35f));
                    }
                    var hit = new Rect(centre.x - pitchX * 0.5f, centre.y - pitchY * 0.5f, pitchX, pitchY);
                    if (Event.current.type == EventType.MouseDown && hit.Contains(Event.current.mousePosition)) { clickedPolicy = group[i]; Event.current.Use(); }
                    else if (!pinned && Event.current.type == EventType.Repaint && hit.Contains(Event.current.mousePosition))
                    {
                        PoliSimWidgets.MeasuredLabel(new Rect(centre.x + dot, centre.y - pitchY * 0.5f, levers.xMax - centre.x - dot, pitchY), PolicyWebRenderer.GetPolicyName(group[i]).ToUpperInvariant(), monoInk);
                    }
                    if (isPinned)
                    {
                        // The caption sits in the group's header row, right-aligned, so it never lands on a dot or the group's name.
                        PoliSimWidgets.MeasuredLabel(new Rect(groupHeader.x + groupHeader.width * 0.45f, groupHeader.y, groupHeader.width * 0.55f, groupHeader.height), PolicyWebRenderer.GetPolicyName(group[i]).ToUpperInvariant() + " · PINNED", Mono(monoBase, labelStyle, 0.64f, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight));
                    }
                }
                int rows = Mathf.CeilToInt(group.Count / (float)perRow);
                y += rows * pitchY + 4f * u;
                if (repaint) { Rule(new Rect(levers.x, y, levers.width, 1f), PoliSimTheme.RuleRow); }
                y += 4f * u;
            }

            // THE BOOKS: one row per stat, the count of lever lines into it, the history box.
            float rowH = Mathf.Round(24f * u);
            PoliSimWidgets.MeasuredLabel(new Rect(books.x, books.y, books.width * 0.7f, groupHead), string.Format(CultureInfo.InvariantCulture, "THE BOOKS — {0} STATS", bookCount), mono);
            PoliSimWidgets.MeasuredLabel(new Rect(books.x + books.width * 0.7f, books.y, books.width * 0.3f, groupHead), "IN  HIST", monoRight);
            float by = books.y + groupHead;
            float boxSize = Mathf.Round(9f * u);
            foreach (StatNodeId stat in Stats())
            {
                var centre = new Vector2(books.x + dot, by + rowH * 0.5f);
                _bookCentres[stat] = centre;
                bool isPinned = pinnedStat.HasValue && pinnedStat.Value == stat;
                bool targeted = pinnedPolicy.HasValue && IsTarget(pinnedPolicy.Value, stat, country);
                bool lit = !pinned || isPinned || targeted;
                if (repaint)
                {
                    if (isPinned) { Disc(centre, dot + 6f * u, PoliSimTheme.TextPrimary); Disc(centre, dot + 3f * u, PoliSimTheme.Card); }
                    Disc(centre, dot, lit ? BookInk : Fade(BookInk, 0.35f));
                }
                int inDegree = PolicyWebRenderer.GetEdgesForTarget(stat, country).Count;
                bool inHistory = country != null && PolicyWebRenderer.GetHistory(stat, country.History) != null;
                GUIStyle nameStyle = lit ? bookName : Faded(bookName, 0.45f);
                PoliSimWidgets.MeasuredLabel(new Rect(books.x + dot * 2.4f, by, books.width * 0.7f - dot * 2.4f, rowH), PolicyWebRenderer.GetStatName(stat), nameStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(books.x + books.width * 0.7f, by, books.width * 0.3f - boxSize - 6f * u, rowH), inDegree.ToString(CultureInfo.InvariantCulture), monoRight);
                if (repaint)
                {
                    var box = new Rect(books.xMax - boxSize, by + (rowH - boxSize) * 0.5f, boxSize, boxSize);
                    Frame(box, PoliSimTheme.HairlineStrong);
                    if (inHistory) { Fill(new Rect(box.x + 2f, box.y + 2f, box.width - 4f, box.height - 4f), BookInk); }
                }
                var hit = new Rect(books.x, by, books.width, rowH);
                if (Event.current.type == EventType.MouseDown && hit.Contains(Event.current.mousePosition)) { clickedStat = stat; Event.current.Use(); }
                by += rowH;
            }
            if (repaint) { Rule(new Rect(books.x - 3f * u, books.y, 1f, by - books.y), PoliSimTheme.RuleRow); }

            // THE EDGES - only when pinned: the lever's lines to the books (or the lines into the pinned book).
            if (repaint && pinned)
            {
                if (pinnedPolicy.HasValue)
                {
                    foreach (PolicyWebEdge edge in PolicyWebRenderer.GetEdgesFor(pinnedPolicy.Value, country))
                    {
                        if (_leverCentres.TryGetValue(edge.Source, out Vector2 from) && _bookCentres.TryGetValue(edge.Target, out Vector2 to)) { Edge(from, to, edge.Increases, edge.Target, edge.Provenance == EdgeProvenance.Derived, edge.RelativeStrength, u); }
                    }
                }
                else
                {
                    foreach (PolicyWebEdge edge in PolicyWebRenderer.GetEdgesForTarget(pinnedStat.Value, country))
                    {
                        if (_leverCentres.TryGetValue(edge.Source, out Vector2 from) && _bookCentres.TryGetValue(edge.Target, out Vector2 to)) { Edge(from, to, edge.Increases, edge.Target, edge.Provenance == EdgeProvenance.Derived, edge.RelativeStrength, u); }
                    }
                }
            }

            // THE CAUSAL GRAPH - at rest only; pinned, the band yields to the pane.
            if (!pinned)
            {
                List<StatWebEdge> statEdges = PolicyWebRenderer.GetAllStatEdges();
                PoliSimWidgets.MeasuredLabel(new Rect(band.x, band.y, band.width, groupHead),
                    string.Format(CultureInfo.InvariantCulture, "THE CAUSAL GRAPH — {0} DERIVED STAT→STAT LINKS · ALL {1} DRAWN", statEdges.Count, Words(statEdges.Count)), mono);
                var bandCentres = new Dictionary<StatNodeId, Vector2>();
                StatNodeId[] stats = Stats();
                float stepX = (band.width - dot * 2f) / Mathf.Max(1, stats.Length - 1);
                float baseY = band.yMax - dot;
                for (int i = 0; i < stats.Length; i++)
                {
                    var c = new Vector2(band.x + dot + i * stepX, baseY);
                    bandCentres[stats[i]] = c;
                    if (repaint) { Disc(c, dot, BookInk); }
                }
                if (repaint)
                {
                    float arcRoom = baseY - (band.y + groupHead) - dot;
                    foreach (StatWebEdge e in statEdges)
                    {
                        if (!bandCentres.TryGetValue(e.Source, out Vector2 a) || !bandCentres.TryGetValue(e.Target, out Vector2 b)) { continue; }
                        float rise = Mathf.Clamp(Mathf.Abs(b.x - a.x) * 0.45f, 8f * u, arcRoom);
                        Arc(a, b, rise, Ink(e.Increases, e.Target), Mathf.Clamp(1f + e.RelativeStrength * 0.6f, 1f, 3f) * u, u);
                    }
                }
            }

            // THE PANE - pinned only.
            if (pinned)
            {
                DrawPane(pane, pinnedPolicy, pinnedStat, country, paneTitle, paneBody, mono, monoInk, monoRight, u);
            }

            // THE LEGEND.
            DrawLegend(legend, mono, u);

            // A click on the paper (no dot, no row) unpins.
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && !clickedPolicy.HasValue && !clickedStat.HasValue && !(pinned && pane.Contains(Event.current.mousePosition)))
            {
                clickedEmptySpace = true;
                Event.current.Use();
            }
        }

        private void DrawPane(Rect pane, PolicyNodeId? pinnedPolicy, StatNodeId? pinnedStat, Country country,
            GUIStyle title, GUIStyle body, GUIStyle mono, GUIStyle monoInk, GUIStyle monoRight, float u)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { Rule(new Rect(pane.x - 3f * u, pane.y, 1f, pane.height), PoliSimTheme.RuleRow); }
            float line = Mathf.Round(20f * u);
            float y = pane.y;
            if (pinnedPolicy.HasValue)
            {
                PolicyNodeId node = pinnedPolicy.Value;
                List<PolicyWebEdge> edges = PolicyWebRenderer.GetEdgesFor(node, country);
                GUIStyle inkedTitle = new GUIStyle(title); inkedTitle.normal.textColor = UiPalette.GetAreaColor(PolicyWebRenderer.GetPolicyArea(node));
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line * 1.4f), PolicyWebRenderer.GetPolicyName(node), inkedTitle);
                y += line * 1.4f;
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), string.Format(CultureInfo.InvariantCulture, "{0} · LEVER · {1} LINE(S) IN THE BOOKS", AreaName(PolicyWebRenderer.GetPolicyArea(node)).ToUpperInvariant(), edges.Count), mono);
                y += line;
                string description = PolicyWebRenderer.GetPolicyDescription(node);
                float descHeight = body.CalcHeight(new GUIContent(description), pane.width);
                GUI.Label(new Rect(pane.x, y, pane.width, descHeight), description, body);
                y += descHeight + 6f * u;
                if (repaint) { Rule(new Rect(pane.x, y, pane.width, 1f), PoliSimTheme.RuleRow); }
                y += 4f * u;
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), "CURRENT EFFECTS — FROM THE LIVE DIALS", mono);
                y += line;
                foreach (string effect in PolicyWebRenderer.GetCurrentEffectSummary(node, country))
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), effect, body);
                    y += line;
                }
                y += 4f * u;
                if (repaint) { Rule(new Rect(pane.x, y, pane.width, 1f), PoliSimTheme.RuleRow); }
                y += 4f * u;
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), "MOVES — ONE LINE PER EDGE", mono);
                y += line;
                int derived = 0, declared = 0;
                foreach (PolicyWebEdge edge in edges)
                {
                    bool isDerived = edge.Provenance == EdgeProvenance.Derived;
                    if (isDerived) { derived++; } else { declared++; }
                    if (repaint) { Swatch(new Rect(pane.x, y + line * 0.5f - 1f, 12f * u, 2f), Ink(edge.Increases, edge.Target), isDerived); }
                    string arrow = edge.Increases ? " ▲" : " ▼";
                    GUIStyle nameStyle = new GUIStyle(body) { wordWrap = false };
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x + 16f * u, y, pane.width * 0.5f - 16f * u, line), PolicyWebRenderer.GetStatName(edge.Target) + arrow, nameStyle);
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x + pane.width * 0.5f, y, pane.width * 0.5f, line), isDerived ? "LEDGER: " + (edge.LedgerTerm ?? string.Empty).ToUpperInvariant() : "— DECLARED", monoRight);
                    y += line;
                }
                string foot = string.Format(CultureInfo.InvariantCulture, "DERIVED {0} · DECLARED {1} · NO LINE AUTHORED · NO EDGE INVENTED", derived, declared);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, pane.yMax - line, pane.width, line), foot, monoRight);
                if (repaint) { Rule(new Rect(pane.x, pane.yMax - line - 2f * u, pane.width, 1f), PoliSimTheme.RuleRow); }
            }
            else
            {
                StatNodeId node = pinnedStat.Value;
                List<PolicyWebEdge> incoming = PolicyWebRenderer.GetEdgesForTarget(node, country);
                List<StatWebEdge> statEdges = PolicyWebRenderer.GetStatEdgesFor(node);
                GUIStyle inkedTitle = new GUIStyle(title); inkedTitle.normal.textColor = BookInk;
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line * 1.4f), PolicyWebRenderer.GetStatName(node), inkedTitle);
                y += line * 1.4f;
                bool? higher = PolicyWebRenderer.GetStatHigherIsBetter(node);
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), string.Format(CultureInfo.InvariantCulture, "BOOK · {0} LEVER LINE(S) INTO IT · {1}", incoming.Count, higher.HasValue ? (higher.Value ? "HIGHER IS BETTER" : "LOWER IS BETTER") : "NO GOOD/BAD FRAMING"), mono);
                y += line + 4f * u;
                PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), "AFFECTED BY — ONE LINE PER LEVER", mono);
                y += line;
                foreach (PolicyWebEdge edge in incoming)
                {
                    bool isDerived = edge.Provenance == EdgeProvenance.Derived;
                    if (repaint) { Swatch(new Rect(pane.x, y + line * 0.5f - 1f, 12f * u, 2f), Ink(edge.Increases, node), isDerived); }
                    GUIStyle nameStyle = new GUIStyle(body) { wordWrap = false };
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x + 16f * u, y, pane.width * 0.5f - 16f * u, line), PolicyWebRenderer.GetPolicyName(edge.Source) + (edge.Increases ? " ▲" : " ▼"), nameStyle);
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x + pane.width * 0.5f, y, pane.width * 0.5f, line), isDerived ? "LEDGER: " + (edge.LedgerTerm ?? string.Empty).ToUpperInvariant() : "— DECLARED", monoRight);
                    y += line;
                }
                if (statEdges.Count > 0)
                {
                    y += 4f * u;
                    PoliSimWidgets.MeasuredLabel(new Rect(pane.x, y, pane.width, line), "IN THE BOOKS — STAT → STAT", mono);
                    y += line;
                    foreach (StatWebEdge edge in statEdges)
                    {
                        string text = edge.Target == node
                            ? "moved by " + PolicyWebRenderer.GetStatName(edge.Source) + (edge.Increases ? " ▲" : " ▼")
                            : "feeds " + PolicyWebRenderer.GetStatName(edge.Target) + (edge.Increases ? " ▲" : " ▼");
                        GUIStyle nameStyle = new GUIStyle(body) { wordWrap = false };
                        PoliSimWidgets.MeasuredLabel(new Rect(pane.x + 16f * u, y, pane.width * 0.5f - 16f * u, line), text, nameStyle);
                        PoliSimWidgets.MeasuredLabel(new Rect(pane.x + pane.width * 0.5f, y, pane.width * 0.5f, line), "LEDGER: " + (edge.LedgerTerm ?? string.Empty).ToUpperInvariant(), monoRight);
                        y += line;
                    }
                }
            }
        }

        private void DrawLegend(Rect legend, GUIStyle mono, float u)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { Rule(new Rect(legend.x, legend.y, legend.width, 1f), PoliSimTheme.RuleRow); }
            float y = legend.y + 3f * u;
            float h = legend.height - 3f * u;
            float x = legend.x;
            x = LegendItem(x, y, h, "DERIVED — THE MODEL'S OWN FORMULA", mono, u, () => Swatch(new Rect(x, y + h * 0.5f - 1f, 14f * u, 2f), PoliSimTheme.TextSecondary, true));
            x = LegendItem(x, y, h, "DECLARED — A STATED COUPLING", mono, u, () => Swatch(new Rect(x, y + h * 0.5f - 1f, 14f * u, 2f), PoliSimTheme.TextSecondary, false));
            x = LegendItem(x, y, h, "WEIGHT 1.1–3.4 = RELATIVE STRENGTH", mono, u, () => Fill(new Rect(x, y + h * 0.5f - 2f, 14f * u, 3f), PoliSimTheme.TextSecondary));
            x = LegendItem(x, y, h, "THE TARGET STAT'S OWN GOOD/BAD FRAMING", mono, u, () => { Fill(new Rect(x, y + h * 0.5f - 4f, 7f * u, 8f), PoliSimTheme.Good); Fill(new Rect(x + 8f * u, y + h * 0.5f - 4f, 7f * u, 8f), PoliSimTheme.Bad); });
            x = LegendItem(x, y, h, "LEVER", mono, u, () => Fill(new Rect(x, y + h * 0.5f - 4f, 8f, 8f), LeverInk));
            LegendItem(x, y, h, "BOOK", mono, u, () => Fill(new Rect(x, y + h * 0.5f - 4f, 8f, 8f), BookInk));
        }

        private static float LegendItem(float x, float y, float h, string text, GUIStyle mono, float u, Action swatch)
        {
            if (Event.current.type == EventType.Repaint) { swatch(); }
            float tw = mono.CalcSize(new GUIContent(text)).x;
            PoliSimWidgets.MeasuredLabel(new Rect(x + 18f * u, y, tw + 2f, h), text, mono);
            return x + 18f * u + tw + 16f * u;
        }

        // ---- layout ----

        private static void Layout(Rect rect, GUIStyle labelStyle, bool pinned, out Rect status, out Rect levers, out Rect books, out Rect band, out Rect legend)
        {
            float u = Unit(labelStyle);
            float gap = Mathf.Round(12f * u);
            status = new Rect(rect.x, rect.y, rect.width, Mathf.Round(18f * u));
            float top = status.yMax + 2f * u;
            float leversW = pinned ? Mathf.Round(rect.width * 0.34f) : Mathf.Round(rect.width * 0.60f);
            float booksW = pinned ? Mathf.Round(rect.width * 0.30f) : rect.width - leversW - gap;
            float leversH = LeversHeight(leversW, labelStyle);
            float booksH = Mathf.Round(22f * u) + Stats().Length * Mathf.Round(24f * u);
            levers = new Rect(rect.x, top, leversW, leversH);
            books = new Rect(rect.x + leversW + gap, top, booksW, booksH);
            float bandH = Mathf.Round(92f * u);
            band = pinned ? Rect.zero : new Rect(rect.x, levers.yMax + 6f * u, leversW, bandH);
            float contentBottom = pinned ? Mathf.Max(levers.yMax, books.yMax) : Mathf.Max(band.yMax, books.yMax);
            legend = new Rect(rect.x, contentBottom + 8f * u, rect.width, Mathf.Round(22f * u));
        }

        private static float LeversHeight(float width, GUIStyle labelStyle)
        {
            float u = Unit(labelStyle);
            float pitchX = Mathf.Round(26f * u), pitchY = Mathf.Round(22f * u), groupHead = Mathf.Round(22f * u);
            int perRow = Mathf.Max(1, Mathf.FloorToInt((width - 4f * u) / pitchX));
            float h = 0f;
            var byArea = LeversByArea();
            foreach (UiPalette.SystemArea area in AreaKeys(byArea))
            {
                h += groupHead + Mathf.CeilToInt(byArea[area].Count / (float)perRow) * pitchY + 8f * u;
            }
            return h;
        }

        /// <summary>The books the web knows - the enum filtered by the renderer's own table (two appended Society rows have no entry yet).</summary>
        private static StatNodeId[] Stats()
        {
            var list = new List<StatNodeId>();
            foreach (StatNodeId id in (StatNodeId[])Enum.GetValues(typeof(StatNodeId))) { if (PolicyWebRenderer.HasStat(id)) { list.Add(id); } }
            return list.ToArray();
        }

        private static Dictionary<UiPalette.SystemArea, List<PolicyNodeId>> LeversByArea()
        {
            var byArea = new Dictionary<UiPalette.SystemArea, List<PolicyNodeId>>();
            foreach (PolicyNodeId id in (PolicyNodeId[])Enum.GetValues(typeof(PolicyNodeId)))
            {
                UiPalette.SystemArea area = PolicyWebRenderer.GetPolicyArea(id);
                if (!byArea.TryGetValue(area, out List<PolicyNodeId> list)) { list = new List<PolicyNodeId>(); byArea[area] = list; }
                list.Add(id);
            }
            return byArea;
        }

        private static IEnumerable<UiPalette.SystemArea> AreaKeys(Dictionary<UiPalette.SystemArea, List<PolicyNodeId>> byArea)
        {
            foreach (UiPalette.SystemArea a in AreaOrder) { if (byArea.ContainsKey(a)) { yield return a; } }
            foreach (UiPalette.SystemArea a in byArea.Keys) { if (Array.IndexOf(AreaOrder, a) < 0) { yield return a; } }
        }

        private static string AreaName(UiPalette.SystemArea area)
        {
            switch (area)
            {
                case UiPalette.SystemArea.CrimeJustice: return "Crime & Justice";
                case UiPalette.SystemArea.SovereignWealth: return "Sovereign Wealth";
                default: return area.ToString();
            }
        }

        private static string Words(int n)
        {
            string[] words = { "ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN", "TWELVE" };
            return n >= 0 && n < words.Length ? words[n] : n.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsTarget(PolicyNodeId lever, StatNodeId stat, Country country)
        {
            foreach (PolicyWebEdge e in PolicyWebRenderer.GetEdgesFor(lever, country)) { if (e.Target == stat) { return true; } }
            return false;
        }

        private static bool Feeds(PolicyNodeId lever, StatNodeId stat, Country country) => IsTarget(lever, stat, country);

        private static float Unit(GUIStyle labelStyle) => Mathf.Max(1f, labelStyle.fontSize) / 14f;

        private static GUIStyle Mono(GUIStyle monoBase, GUIStyle labelStyle, float scale, Color ink, TextAnchor anchor)
        {
            var s = new GUIStyle(monoBase) { fontSize = Mathf.Max(8, Mathf.RoundToInt(labelStyle.fontSize * scale)), alignment = anchor, wordWrap = false };
            s.padding = new RectOffset(0, 0, 0, 0);
            s.normal.textColor = ink; s.hover.textColor = ink; s.active.textColor = ink; s.focused.textColor = ink;
            return s;
        }

        private static GUIStyle Faded(GUIStyle s, float alpha)
        {
            var f = new GUIStyle(s);
            Color c = s.normal.textColor; c.a *= alpha;
            f.normal.textColor = c; f.hover.textColor = c; f.active.textColor = c; f.focused.textColor = c;
            return f;
        }

        private static Color Fade(Color c, float alpha) { c.a *= alpha; return c; }

        /// <summary>The edge's ink is the TARGET stat's own good/bad framing of the move - never a hue of the lever's.</summary>
        private static Color Ink(bool increases, StatNodeId target)
        {
            bool? higher = PolicyWebRenderer.GetStatHigherIsBetter(target);
            if (!higher.HasValue) { return PoliSimTheme.TextSecondary; }
            return increases == higher.Value ? PoliSimTheme.Good : PoliSimTheme.Bad;
        }

        // ---- primitives ----

        private static void Disc(Vector2 centre, float diameter, Color ink)
        {
            if (_disc == null) { _disc = BuildDisc(32); }
            Color previous = GUI.color; GUI.color = ink;
            GUI.DrawTexture(new Rect(centre.x - diameter * 0.5f, centre.y - diameter * 0.5f, diameter, diameter), _disc);
            GUI.color = previous;
        }

        private static void Fill(Rect r, Color ink)
        {
            Color previous = GUI.color; GUI.color = ink; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = previous;
        }

        private static void Rule(Rect r, Color ink) => Fill(r, ink);

        private static void Frame(Rect r, Color ink)
        {
            Fill(new Rect(r.x, r.y, r.width, 1f), ink); Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), ink);
            Fill(new Rect(r.x, r.y, 1f, r.height), ink); Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), ink);
        }

        private static void Swatch(Rect r, Color ink, bool solid)
        {
            if (solid) { Fill(r, ink); return; }
            float x = r.x;
            while (x < r.xMax) { Fill(new Rect(x, r.y, Mathf.Min(3f, r.xMax - x), r.height), ink); x += 5f; }
        }

        private static void Segment(Vector2 a, Vector2 b, float thickness, Color ink)
        {
            Vector2 d = b - a;
            float length = d.magnitude;
            if (length < 0.5f) { return; }
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.color = ink;
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static void Edge(Vector2 from, Vector2 to, bool increases, StatNodeId target, bool derived, float strength, float u)
        {
            Color ink = Ink(increases, target);
            if (!derived) { ink.a *= 0.65f; }
            float thickness = Mathf.Clamp(1f + strength * 0.6f, 1f, 3f) * u;
            const int steps = 28;
            Vector2 c1 = new Vector2(from.x + (to.x - from.x) * 0.5f, from.y);
            Vector2 c2 = new Vector2(from.x + (to.x - from.x) * 0.5f, to.y);
            Vector2 previous = from;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float mt = 1f - t;
                Vector2 p = mt * mt * mt * from + 3f * mt * mt * t * c1 + 3f * mt * t * t * c2 + t * t * t * to;
                bool draw = derived || (i % 4 != 0);   // a declared edge is dashed: every fourth step skipped
                if (draw) { Segment(previous, p, thickness, ink); }
                previous = p;
            }
            Head(to, new Vector2(1f, 0f), 5f * u, ink);
        }

        private static void Arc(Vector2 a, Vector2 b, float rise, Color ink, float thickness, float u)
        {
            const int steps = 24;
            Vector2 previous = a;
            Vector2 last = a;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = new Vector2(Mathf.Lerp(a.x, b.x, t), a.y - Mathf.Sin(t * Mathf.PI) * rise);
                Segment(previous, p, thickness, ink);
                last = previous; previous = p;
            }
            Vector2 dir = (b - last).normalized;
            Head(b, dir, 5f * u, ink);
        }

        private static void Head(Vector2 tip, Vector2 dir, float size, Color ink)
        {
            Vector2 back = tip - dir * size;
            Vector2 normal = new Vector2(-dir.y, dir.x) * size * 0.5f;
            Segment(tip, back + normal, 1.5f, ink);
            Segment(tip, back - normal, 1.5f, ink);
        }

        private static Texture2D BuildDisc(int diameter)
        {
            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = diameter * 0.5f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
