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

        /// <summary>The third section's capture (2026-08-25, `93c_trace_debt` at 1600) found the
        /// gap the row cap alone leaves: on the Budget tab in its budget-pause state the host has
        /// ~7 rows of height under the chips, so a 10-row section ran past the window edge with
        /// every guard silent (each cell fit its own rect). The panel now measures against the
        /// HOST'S remaining height as well and scrolls internally for the rest.
        ///
        /// The share is the WHOLE remaining height, deliberately: a first cut at 0.6 bounded the
        /// window overrun but would also have cut the approval section on the PolicyLaws host from
        /// its approved twelve rows (Step 2's `93_trace_approval`, a set awaiting Elias's eyes) to
        /// five - a silent regression of a capture that already fit. The cap's job is "never past
        /// the window," not "leave the host a body": where twelve rows fit they still show, and
        /// where they don't the panel takes what the host has and scrolls. The tab's own body
        /// under an open panel was already the accepted trade on PolicyLaws. Never fewer than
        /// <see cref="MinVisibleRows"/>: three rows is the floor of usefulness, and no supported
        /// size leaves a host that short.</summary>
        private const float MaxShareOfHostHeight = 1f;
        private const int MinVisibleRows = 3;

        private static StatNodeId? _selected;
        private static StatNodeId? _pendingSelected;
        private static bool _hasPending;
        private static Vector2 _scroll;

        /// <summary>Approval and confidence (Step 2 v1) plus Debt-to-GDP (the third section,
        /// 2026-08-25 - the fiscal chain, on the trigger Italy Debt Crisis fired).</summary>
        public static bool SupportsTrace(StatNodeId stat)
            => stat == StatNodeId.Approval || stat == StatNodeId.ConsumerConfidence || stat == StatNodeId.DebtToGdp;

        /// <summary>The stat the panel is currently open on, or null when closed - read by the
        /// capture driver's assert-own-name guard so a capture named for a trace can prove the
        /// trace it claims is the one on screen.</summary>
        public static StatNodeId? SelectedStat => _selected;

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

        /// <summary>The capture driver's own entry (2026-08-25): an ABSOLUTE selection, not a
        /// toggle. A toggle queued while no chip host is drawing (under an exclusive screen - a
        /// scenario verdict, the selector) stays pending, and the next toggle then composes with
        /// it instead of applying on its own: the "Inherit the Fund" block's close-toggle and the
        /// Italy block's open-toggle collapsed into a close, so `95b_italydebt_in_progress` had
        /// been shot without the approval trace it claimed since Step 3, unasserted. Production
        /// clicks keep the toggle; the driver states what it wants. Same Layout-only commit.</summary>
        public static void RequestSelection(StatNodeId? stat)
        {
            if (stat.HasValue && !SupportsTrace(stat.Value))
            {
                return;
            }

            _pendingSelected = stat;
            _hasPending = true;
        }

        /// <summary>Height the panel will occupy this frame - callers subtract it from their
        /// content budget exactly like the stat row's own MeasureHeight. Applies any pending
        /// selection change first, on the Layout event only.</summary>
        public static float MeasureHeight(Country country, float wageGapStance, GUIStyle labelStyle, float availableWidth, float hostRemainingHeight)
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

            return VisibleRows(rows.Count, labelStyle, hostRemainingHeight) * LedgerRow.Height(labelStyle) + 6f;
        }

        /// <summary>Rows shown before the panel scrolls internally: the row cap, then the host's
        /// own height (see <see cref="MaxShareOfHostHeight"/>), never below the floor. The single
        /// place both <see cref="MeasureHeight"/> and <see cref="Draw"/> decide it.</summary>
        private static int VisibleRows(int rowCount, GUIStyle labelStyle, float hostRemainingHeight)
        {
            float rowHeight = LedgerRow.Height(labelStyle);
            int byHost = rowHeight > 0f ? Mathf.FloorToInt(hostRemainingHeight * MaxShareOfHostHeight / rowHeight) : MaxVisibleRows;
            return Mathf.Min(rowCount, Mathf.Max(MinVisibleRows, Mathf.Min(MaxVisibleRows, byHost)));
        }

        /// <summary>Draws the panel. Must be called with the SAME arguments as MeasureHeight in
        /// the same frame - both route through <see cref="BuildRows"/>, so the measurement and
        /// the drawing cannot disagree (the ComputeLayout lesson).</summary>
        public static void Draw(Country country, float wageGapStance, GUIStyle labelStyle, GUIStyle figureStyle, float availableWidth, float hostRemainingHeight)
        {
            List<TraceRow> rows = BuildRows(country, wageGapStance);
            if (rows == null)
            {
                return;
            }

            float rowHeight = LedgerRow.Height(labelStyle);
            int visibleRows = VisibleRows(rows.Count, labelStyle, hostRemainingHeight);
            bool scrolls = rows.Count > visibleRows;
            if (scrolls)
            {
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(visibleRows * rowHeight));
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

        // The approval terms' NAMES, one vocabulary for this panel's rows and Screen 0's ledger (R-B7).
        private const string TermReversion = "Reversion toward 50";
        private const string TermGrowth = "Growth vs potential";
        private const string TermMisery = "Misery (gaps)";
        private const string TermMiseryUnemployment = "· unemployment above NAIRU";
        private const string TermMiseryInflation = "· inflation off target";
        private const string TermMiseryCrime = "· crime above baseline";
        private const string TermMiseryCorruption = "· corruption above baseline";
        private const string TermTaxHikes = "Tax hikes";
        private const string TermSpending = "Spending changes";
        private const string TermWelfare = "Welfare vs baseline";
        private const string TermPaidLeave = "Paid family leave vs baseline";
        private const string TermDrugPolicy = "Drug policy stance";
        private const string TermGini = "Inequality vs own norm (Gini)";
        private const string TermClamp = "Clamp at 0/100";

        /// <summary>The nine term rows <see cref="BuildApprovalDeskTerms"/> always emits, in its order - Screen 0's Year-0 empty state (board 1m-r2, 2026-08-28) draws them with em-dash figures before the first period closes, so the ledger's shape is on the sheet from day one and no name is written twice.</summary>
        public static readonly string[] ApprovalDeskTermNames =
        {
            TermReversion, TermGrowth, TermMisery, TermTaxHikes, TermSpending, TermWelfare, TermPaidLeave, TermDrugPolicy, TermGini
        };

        /// <summary>One row of Screen 0's approval ledger: a term's name and its signed figure.</summary>
        public readonly struct DeskTerm
        {
            public readonly string Name;
            public readonly float Value;

            public DeskTerm(string name, float value)
            {
                Name = name;
                Value = value;
            }
        }

        /// <summary>
        /// Screen 0's approval ledger (UI v3.0 Phase B, board 1m, R-B7): this panel's OWN Class A terms
        /// in this panel's order and names - the nine non-misery terms, the four misery gaps as their
        /// one total, the clamp only when it is not zero, and the period's dated events as one total
        /// row with their count. Null while no period has closed (the Desk then draws nothing beneath
        /// its hero numeral rather than a placeholder). The Desk restates nothing: it draws these rows
        /// through the same read-only ledger lane, no gauge.
        /// </summary>
        public static List<DeskTerm> BuildApprovalDeskTerms(Country country)
        {
            ApprovalAttribution ledger = country?.ApprovalLedgerLastPeriod;
            if (ledger == null || !ledger.Closed)
            {
                return null;
            }

            var terms = new List<DeskTerm>
            {
                new DeskTerm(TermReversion, ledger.Reversion),
                new DeskTerm(TermGrowth, ledger.GrowthEffect),
                new DeskTerm(TermMisery, ledger.MiseryUnemployment + ledger.MiseryInflation + ledger.MiseryCrime + ledger.MiseryCorruption),
                new DeskTerm(TermTaxHikes, ledger.TaxHikePenalty),
                new DeskTerm(TermSpending, ledger.SpendingEffect),
                new DeskTerm(TermWelfare, ledger.WelfareEffect),
                new DeskTerm(TermPaidLeave, ledger.PaidLeaveEffect),
                new DeskTerm(TermDrugPolicy, ledger.DrugPolicyEffect),
                new DeskTerm(TermGini, ledger.GiniEffect)
            };

            if (!Mathf.Approximately(ledger.ClampLoss, 0f))
            {
                terms.Add(new DeskTerm(TermClamp, ledger.ClampLoss));
            }

            if (ledger.Events.Count > 0)
            {
                float eventsTotal = 0f;
                for (int i = 0; i < ledger.Events.Count; i++) { eventsTotal += ledger.Events[i].AppliedDelta; }
                terms.Add(new DeskTerm($"Events (dated) ×{ledger.Events.Count}", eventsTotal));
            }

            return terms;
        }

        /// <summary>The single source both MeasureHeight and Draw read. Null when the panel is
        /// closed or the selected stat has nothing honest to show yet.</summary>
        private static List<TraceRow> BuildRows(Country country, float wageGapStance)
        {
            if (_selected == null || country == null)
            {
                return null;
            }

            switch (_selected.Value)
            {
                case StatNodeId.Approval: return BuildApprovalRows(country);
                case StatNodeId.DebtToGdp: return BuildDebtRows(country);
                default: return BuildConfidenceRows(country, wageGapStance);
            }
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

            // Class A - the formula's terms, exact and signed. Playtest 3 cut (2026-08-27): the
            // "≈ x sustained" equilibrium framing the sustained terms carried was a (b) and is cut -
            // a row is the term and its figure.
            Term(rows, TermReversion, ledger.Reversion);
            Term(rows, TermGrowth, ledger.GrowthEffect);
            float misery = ledger.MiseryUnemployment + ledger.MiseryInflation + ledger.MiseryCrime + ledger.MiseryCorruption;
            Term(rows, TermMisery, misery);
            Term(rows, TermMiseryUnemployment, ledger.MiseryUnemployment, indented: true);
            Term(rows, TermMiseryInflation, ledger.MiseryInflation, indented: true);
            Term(rows, TermMiseryCrime, ledger.MiseryCrime, indented: true);
            Term(rows, TermMiseryCorruption, ledger.MiseryCorruption, indented: true);
            Term(rows, TermTaxHikes, ledger.TaxHikePenalty);
            Term(rows, TermSpending, ledger.SpendingEffect);
            Term(rows, TermWelfare, ledger.WelfareEffect);
            Term(rows, TermPaidLeave, ledger.PaidLeaveEffect);
            Term(rows, TermDrugPolicy, ledger.DrugPolicyEffect);
            Term(rows, TermGini, ledger.GiniEffect);
            Term(rows, TermClamp, ledger.ClampLoss);

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

            // Playtest 3 cut (2026-08-27): the audit footer ("Terms x + events y = z — audited at
            // the boundary") was a (c) of the header's own delta plus a (b) - cut. The audit itself
            // is CloseAtBoundary's assertion, not a row; ATTRIB fires if the books ever disagree.
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
                // Playtest 3 cut (2026-08-27): "this period's single book", "(healthcare/UBI)",
                // "(period stance)" and "— what the economy reads" were (b) mechanism notes on rows
                // whose figures are the (a) - cut; the row names the term, the figure carries the
                // datum, the gap figure in the trailing column stays (it is a figure).
                new TraceRow { Header = true, Name = "Consumer confidence" },
                new TraceRow { Name = "Policy base", Figure = $"{baseValue:F3}" },
                new TraceRow
                {
                    Name = "Wage-sentiment factor",
                    Figure = $"×{(baseValue > 0f ? effective / baseValue : 1f):F4}",
                    Trailing = $"gap {wageGapStance:+0.00;-0.00} pp"
                },
                new TraceRow { Name = "Effective", Figure = $"{effective:F3}" }
            };
            return rows;
        }

        /// <summary>
        /// The third section (2026-08-25): the debt stock's period, then the ratio's own identity.
        /// Every term is Class A BY OBSERVATION - the daily write's own values, summed over the
        /// period's slices (see DebtAttribution) - so the debt step decomposes exactly: the primary
        /// balance before the reaction, the fiscal reaction's revenue effect (with the frozen
        /// Class C stance the period consumed as its trailing figure), interest at the issuance
        /// rate, the maturity lag's effective-rate term, and the −π·b erosion; the compounding
        /// class's residual line is the clamp/rounding row, skipped when it rounds to nothing.
        /// Events are Class B, dated. The ratio's two-term identity is exact from the four
        /// recorded anchors and carries no residual; GDP's OWN drivers are Class D and are
        /// deliberately not claimed here - that is the identity's honest boundary, stated on the
        /// footer. Every figure is the number the simulation used, recorded at the write, never
        /// recomputed for display (the single-book rider).
        /// </summary>
        private static List<TraceRow> BuildDebtRows(Country country)
        {
            var rows = new List<TraceRow>();
            DebtAttribution ledger = country.FiscalLedgerLastPeriod;
            if (ledger == null || !ledger.Closed)
            {
                rows.Add(new TraceRow { Header = true, Name = "Debt — no period recorded yet. Advance a year." });
                return rows;
            }

            float delta = ledger.DebtAtClose - ledger.DebtAtPeriodOpen;
            rows.Add(new TraceRow
            {
                Header = true,
                Name = $"Debt — period ended {ledger.PeriodCloseDate:MMM d, yyyy}: " +
                       $"{Money(ledger.DebtAtPeriodOpen)} → {Money(ledger.DebtAtClose)} ({SignedMoney(delta)}) · " +
                       $"ratio {ledger.RatioAtPeriodOpen:F1}% → {ledger.RatioAtClose:F1}%"
            });

            // Playtest 3 cut (2026-08-27): the trailing MECHANISM texts ("a primary deficit",
            // "stance ×1.00", "blended pays ... vs issuance", "(rounding, audited)") were (b) and are
            // cut; the rate and π figures in the trailing column are figures and stay.
            MoneyTerm(rows, "Primary balance (before the reaction)", ledger.PrimaryBalanceEffect, "");
            MoneyTerm(rows, "Fiscal reaction on revenue", ledger.FiscalReactionEffect, "");
            MoneyTerm(rows, "Interest at the issuance rate", ledger.InterestAtIssuance, $"{ledger.IssuanceRateAtOpen:F2}→{ledger.IssuanceRateAtClose:F2}%");
            // Name kept short: the first capture (1600, 2026-08-25) showed "Maturity lag (blended −
            // issuance)" SHRUNK to fit the name column rather than wrapping - a parenthetical is one
            // unbreakable run to the measured label.
            MoneyTerm(rows, "Maturity lag", ledger.RateLagEffect, $"{ledger.EffectiveRateAtOpen:F2}→{ledger.EffectiveRateAtClose:F2}%");
            MoneyTerm(rows, "Inflation erosion (−π·b)", ledger.Erosion, $"π {ledger.InflationAtOpen:F1}→{ledger.InflationAtClose:F1}%");
            MoneyTerm(rows, ledger.ClampBoundDays > 0
                    ? $"Clamp at guard/ceiling ({ledger.ClampBoundDays} day{(ledger.ClampBoundDays == 1 ? "" : "s")})"
                    : "Residual",
                ledger.ClampLoss, "");

            if (ledger.Events.Count > 0)
            {
                rows.Add(new TraceRow { Header = true, Name = "Events this period" });
                int shown = Mathf.Min(ledger.Events.Count, MaxEventRows);
                for (int i = 0; i < shown; i++)
                {
                    DebtEventRecord e = ledger.Events[i];
                    rows.Add(new TraceRow { Name = e.Label, Figure = SignedMoney(e.AppliedDelta), Trailing = $"{e.Date:MMM d}" });
                }

                if (ledger.Events.Count > shown)
                {
                    float omittedSum = 0f;
                    for (int i = shown; i < ledger.Events.Count; i++) { omittedSum += ledger.Events[i].AppliedDelta; }
                    rows.Add(new TraceRow { Name = $"+{ledger.Events.Count - shown} more events", Figure = SignedMoney(omittedSum), Trailing = "" });
                }
            }

            // Playtest 3 cut (2026-08-27): the audit footer was a (c) of the header's delta plus a
            // (b) - cut, as on the approval section; the boundary audit stands where it always did.

            // The ratio's identity in three rows, not four - the result rides on the header so the
            // section stays inside a 1600 Budget-tab host (measured on the first capture).
            if (ledger.GdpAtClose > 0f && ledger.GdpAtPeriodOpen > 0f)
            {
                float stockAtClosingGdp = delta / ledger.GdpAtClose * 100f;
                float gdpOnOpeningStock = ledger.DebtAtPeriodOpen * (1f / ledger.GdpAtClose - 1f / ledger.GdpAtPeriodOpen) * 100f;
                rows.Add(new TraceRow
                {
                    Header = true,
                    // Playtest 3 cut (2026-08-27): "— the ratio's own identity, exact; GDP's drivers
                    // are not this section's claim" was a (b) - cut; the claim's boundary lives in
                    // this method's doc comment.
                    Name = $"Debt-to-GDP {ledger.RatioAtClose - ledger.RatioAtPeriodOpen:+0.00;-0.00} pp"
                });
                rows.Add(new TraceRow { Name = "Stock change, at closing GDP", Figure = $"{stockAtClosingGdp:+0.00;-0.00} pp", Trailing = "" });
                rows.Add(new TraceRow
                {
                    Name = "GDP's movement on the opening stock",
                    Figure = $"{gdpOnOpeningStock:+0.00;-0.00} pp",
                    Trailing = $"GDP {Money(ledger.GdpAtPeriodOpen)}→{Money(ledger.GdpAtClose)}"
                });
            }

            return rows;
        }

        private static string Money(float value) => UiFormat.Money(value, MoneyUnit.Billions);

        private static string SignedMoney(float value)
            => (value < 0f ? "−" : "+") + UiFormat.Money(Mathf.Abs(value), MoneyUnit.Billions);

        /// <summary>A dollar term under $50M on a stock in the thousands of billions rounds to
        /// nothing at display precision - skipped, never shown as a confident zero. The audit
        /// footer still sums the exact values.</summary>
        private static void MoneyTerm(List<TraceRow> rows, string name, float value, string trailing)
        {
            if (Mathf.Abs(value) < 0.05f)
            {
                return;
            }

            rows.Add(new TraceRow { Name = name, Figure = SignedMoney(value), Trailing = trailing });
        }

        private static void Term(List<TraceRow> rows, string name, float value, bool indented = false)
        {
            // A term that rounds to 0.00 is noise, not honesty - skipped, never shown as a
            // confident zero. The boundary audit (CloseAtBoundary) still sums the exact values.
            // Playtest 3 cut (2026-08-27): the "≈ x sustained" trailing figure on the sustained
            // terms (value / ApprovalReversionSpeed, an equilibrium projection explained nowhere on
            // screen) was a (b) and is cut; a row is the term and its figure.
            if (Mathf.Abs(value) < 0.005f)
            {
                return;
            }

            rows.Add(new TraceRow
            {
                Name = name,
                Figure = $"{value:+0.00;-0.00}",
                Trailing = "",
                Indented = indented
            });
        }
    }
}
