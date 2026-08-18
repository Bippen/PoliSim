using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Step 2's trace panel (R-S2a phase 1): click a stat chip, see what wrote it - the actual
    /// terms, surfaced. Term-level only (R-S2b), explaining the LIVE model (R-S2c).
    ///
    /// <para><b>The honesty classes govern every line here</b> (the scoping package's §1):
    /// approval's rows are the boundary formula's own recorded locals (Class A - exact, signed,
    /// period-true) plus the period's events (Class B - dated, post-clamp actuals); sustained
    /// gap terms carry EQUILIBRIUM framing (term ÷ reversion - the honest unit Q1's magnitude
    /// was ruled in); the confidence section is the SINGLE BOOK (Q2's rider): effective = base ×
    /// wage-sentiment factor presented as one truth whose components are named, with the factor
    /// shown as the PERIOD STANCE the identity actually consumed, never the live gap.</para>
    ///
    /// <para><b>Display grammar</b>: LedgerRow rows on the paper idiom; every figure here is a
    /// signed unbounded quantity, so every row passes a NEGATIVE fill - §A.9b: no gauge, because
    /// "there is no proportion here" is the honest statement.</para>
    ///
    /// <para><b>IMGUI safety</b>: selection changes are applied only during the Layout event
    /// (<see cref="MeasureHeight"/>), so Layout and Repaint always agree within a frame - the
    /// stable-control-layout discipline. Rows are Labels via LedgerRow (no focusable controls),
    /// and the row LIST changes only at period boundaries, never mid-drag.</para>
    /// </summary>
    public static class StatTracePanel
    {
        private struct TraceRow
        {
            public string Name;
            public string Figure;
            public string Trailing;
            public bool Indented;
            public bool Header;
        }

        /// <summary>Event rows shown before the "+N more" line - bounds the ROW COUNT so a busy
        /// period stays summarized rather than enumerated.</summary>
        private const int MaxEventRows = 4;

        /// <summary>Rows visible before the panel scrolls internally - bounds the panel's HEIGHT
        /// so it can never push the tab's own content (or itself) past the screen edge; the
        /// first capture set measured exactly that. The full row list stays reachable by
        /// scrolling - bounded box, nothing trimmed.</summary>
        private const int MaxVisibleRows = 12;

        private static StatNodeId? _selected;
        private static StatNodeId? _pendingSelected;
        private static bool _hasPending;
        private static Vector2 _scroll;

        public static bool SupportsTrace(StatNodeId stat)
            => stat == StatNodeId.Approval || stat == StatNodeId.ConsumerConfidence;

        /// <summary>Called by the chip overlay. The toggle is PENDING until the next Layout pass
        /// - flipping selection mid-frame would make Layout and Repaint disagree about control
        /// count, the documented IMGUI desync trigger.</summary>
        public static void NotifyChipClicked(StatNodeId stat)
        {
            if (!SupportsTrace(stat))
            {
                return;
            }

            _pendingSelected = _selected == stat ? (StatNodeId?)null : stat;
            _hasPending = true;
        }

        /// <summary>Height the panel will occupy this frame - callers subtract it from their
        /// content budget exactly like the stat row's own MeasureHeight. Applies any pending
        /// selection change first, on the Layout event only.</summary>
        public static float MeasureHeight(Country country, float wageGapStance, GUIStyle labelStyle, float availableWidth)
        {
            if (_hasPending && Event.current != null && Event.current.type == EventType.Layout)
            {
                _selected = _pendingSelected;
                _hasPending = false;
            }

            List<TraceRow> rows = BuildRows(country, wageGapStance);
            if (rows == null)
            {
                return 0f;
            }

            return Mathf.Min(rows.Count, MaxVisibleRows) * LedgerRow.Height(labelStyle) + 6f;
        }

        /// <summary>Draws the panel. Must be called with the SAME arguments as MeasureHeight in
        /// the same frame - both route through <see cref="BuildRows"/>, so the measurement and
        /// the drawing cannot disagree (the ComputeLayout lesson).</summary>
        public static void Draw(Country country, float wageGapStance, GUIStyle labelStyle, GUIStyle figureStyle, float availableWidth)
        {
            List<TraceRow> rows = BuildRows(country, wageGapStance);
            if (rows == null)
            {
                return;
            }

            float rowHeight = LedgerRow.Height(labelStyle);
            bool scrolls = rows.Count > MaxVisibleRows;
            if (scrolls)
            {
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(MaxVisibleRows * rowHeight));
            }

            foreach (TraceRow row in rows)
            {
                Rect rect = GUILayoutUtility.GetRect(availableWidth - (scrolls ? 18f : 0f), rowHeight);
                if (row.Header)
                {
                    LedgerRow.Cell(rect, row.Name, labelStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
                    continue;
                }

                Rect body = row.Indented ? new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height) : rect;
                LedgerRow.DrawReadOnly(body, row.Name, -1f, row.Figure, row.Trailing,
                    PoliSimTheme.TextPrimary, labelStyle, figureStyle);
            }

            if (scrolls)
            {
                GUILayout.EndScrollView();
            }

            GUILayoutUtility.GetRect(availableWidth, 6f);
        }

        /// <summary>The single source both MeasureHeight and Draw read. Null when the panel is
        /// closed or the selected stat has nothing honest to show yet.</summary>
        private static List<TraceRow> BuildRows(Country country, float wageGapStance)
        {
            if (_selected == null || country == null)
            {
                return null;
            }

            return _selected == StatNodeId.Approval
                ? BuildApprovalRows(country)
                : BuildConfidenceRows(country, wageGapStance);
        }

        private static List<TraceRow> BuildApprovalRows(Country country)
        {
            var rows = new List<TraceRow>();
            ApprovalAttribution ledger = country.ApprovalLedgerLastPeriod;
            if (ledger == null || !ledger.Closed)
            {
                rows.Add(new TraceRow { Header = true, Name = "Approval — no period recorded yet. Advance a turn." });
                return rows;
            }

            float delta = ledger.ApprovalAtClose - ledger.ApprovalAtPeriodOpen;
            rows.Add(new TraceRow
            {
                Header = true,
                Name = $"Approval — period ended {ledger.PeriodCloseDate:MMM d, yyyy}: " +
                       $"{ledger.ApprovalAtPeriodOpen:F1} → {ledger.ApprovalAtClose:F1} ({delta:+0.00;-0.00})"
            });

            // Class A - the formula's terms, exact and signed. Sustained gap terms carry the
            // equilibrium framing; one-off and cyclical terms deliberately do not.
            Term(rows, "Reversion toward 50", ledger.Reversion, sustained: false);
            Term(rows, "Growth vs potential", ledger.GrowthEffect, sustained: false);
            float misery = ledger.MiseryUnemployment + ledger.MiseryInflation + ledger.MiseryCrime + ledger.MiseryCorruption;
            Term(rows, "Misery (gaps)", misery, sustained: true);
            Term(rows, "· unemployment above NAIRU", ledger.MiseryUnemployment, sustained: false, indented: true);
            Term(rows, "· inflation off target", ledger.MiseryInflation, sustained: false, indented: true);
            Term(rows, "· crime above baseline", ledger.MiseryCrime, sustained: false, indented: true);
            Term(rows, "· corruption above baseline", ledger.MiseryCorruption, sustained: false, indented: true);
            Term(rows, "Tax hikes", ledger.TaxHikePenalty, sustained: false);
            Term(rows, "Spending changes", ledger.SpendingEffect, sustained: false);
            Term(rows, "Welfare vs baseline", ledger.WelfareEffect, sustained: true);
            Term(rows, "Paid family leave vs baseline", ledger.PaidLeaveEffect, sustained: true);
            Term(rows, "Drug policy stance", ledger.DrugPolicyEffect, sustained: true);
            Term(rows, "Inequality vs own norm (Gini)", ledger.GiniEffect, sustained: true);
            Term(rows, "Clamp at 0/100", ledger.ClampLoss, sustained: false);

            // Class B - dated events, post-clamp actuals. Capped with a STATED omission (the
            // chips' own "+N more" idiom - say what was left out, never trim quietly): an
            // unbounded busy period must not push the panel past the screen (measured on the
            // first capture set - a 2-bill period already reached the bottom edge).
            if (ledger.Events.Count > 0)
            {
                rows.Add(new TraceRow { Header = true, Name = "Events this period" });
                int shown = Mathf.Min(ledger.Events.Count, MaxEventRows);
                for (int i = 0; i < shown; i++)
                {
                    ApprovalEventRecord e = ledger.Events[i];
                    rows.Add(new TraceRow
                    {
                        Name = e.Label,
                        Figure = $"{e.AppliedDelta:+0.00;-0.00}",
                        Trailing = $"{e.Date:MMM d}"
                    });
                }

                if (ledger.Events.Count > shown)
                {
                    float omittedSum = 0f;
                    for (int i = shown; i < ledger.Events.Count; i++) { omittedSum += ledger.Events[i].AppliedDelta; }
                    rows.Add(new TraceRow
                    {
                        Name = $"+{ledger.Events.Count - shown} more events",
                        Figure = $"{omittedSum:+0.00;-0.00}",
                        Trailing = ""
                    });
                }
            }

            rows.Add(new TraceRow
            {
                Header = true,
                Name = $"Terms {ledger.TermSum + ledger.ClampLoss:+0.00;-0.00} + events {ledger.EventSum:+0.00;-0.00} " +
                       $"= {delta:+0.00;-0.00} — audited at the boundary."
            });
            return rows;
        }

        /// <summary>Q2's single book, presented: one truth, two named components, the factor as
        /// the period stance the identity consumed.</summary>
        private static List<TraceRow> BuildConfidenceRows(Country country, float wageGapStance)
        {
            float baseValue = country.State.ConsumerConfidence;
            float effective = MacroSystem.EffectiveConsumerConfidence(country, wageGapStance);
            var rows = new List<TraceRow>
            {
                new TraceRow { Header = true, Name = "Consumer confidence — this period's single book" },
                new TraceRow { Name = "Policy base (healthcare/UBI accumulation)", Figure = $"{baseValue:F3}" },
                new TraceRow
                {
                    Name = "Wage-sentiment factor (period stance)",
                    Figure = $"×{(baseValue > 0f ? effective / baseValue : 1f):F4}",
                    Trailing = $"gap {wageGapStance:+0.00;-0.00} pp"
                },
                new TraceRow { Name = "Effective — what the economy reads", Figure = $"{effective:F3}" }
            };
            return rows;
        }

        private static void Term(List<TraceRow> rows, string name, float value, bool sustained, bool indented = false)
        {
            // A term that rounds to 0.00 is noise, not honesty - skipped, never shown as a
            // confident zero. The audit footer still sums the exact values.
            if (Mathf.Abs(value) < 0.005f)
            {
                return;
            }

            rows.Add(new TraceRow
            {
                Name = name,
                Figure = $"{value:+0.00;-0.00}",
                Trailing = sustained ? $"≈ {value / MacroSystem.ApprovalReversionSpeed:+0.0;-0.0} sustained" : "",
                Indented = indented
            });
        }
    }
}
