using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §734 (UI v3.5, Design's V35 composition): THE BUDGET AS THE COMPOSITION LAYS IT - the title with its tabs as words and the †; four tiles across
    /// the top (the balance, the debt, last year's revenue and spending); the page's dials as TILES on the twelve columns (the icon, the figure, the
    /// name, the chip - what a tax raises - the track with its end labels, and a tax's on/off switch); the *if passed* panel in the right-hand four
    /// columns while a draft stands; and one call to action - *Introduce budget bill* with the count of changes, or the opposition's alternative.
    ///
    /// <para><b>What is the composition's and what is kept.</b> The composition draws two tabs, Revenue and Spending; the game's budget has three more
    /// categories - welfare, infrastructure and the sovereign wealth fund - KEPT as their own tabs, drawn as built, and asked. Revenue is v3.5 here; Spending
    /// is §735's. The policy screen's stat chips and their trace panel are kept under the tiles, as built, and asked. The income tax's statute (its curve
    /// and its bands, six shapes across the six) is kept as built in a card under the tiles.</para>
    ///
    /// <para><b>Two of the composition's choices the model's rules override.</b> (1) The composition shows *if passed* only while drafting and lays the tiles
    /// three to a row without it - the first drag would move every tile under the pointer, which the stable-control-layout rule (P4-1, asserted by the
    /// film) forbids: the panel's four columns are KEPT whatever the draft, and the panel draws in them only while one stands. (2) The tiles keep the
    /// statute's order (the tax types' own): whether a tax is levied can change under a live drag (a programme bill resolving), and a control's order
    /// may never follow mutable state. (The tab under the title switches what the page draws, but only on the player's own click, which cannot race a
    /// drag on another control - the old category column's note, kept.)</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>The preview's scope, said once on every figure it gives (the effects panel's own line less what it says of arrow lengths - the panel draws none).</summary>
        private const string BudgetPreviewScope = "AN ESTIMATE · NEXT YEAR · WITH vs WITHOUT THIS DRAFT · NO EVENTS · ONE DETERMINISTIC POINT - NOT A RANGE";

        private static readonly string[] BudgetTabs = { "Revenue", "Spending", "Welfare", "Infrastructure", "Fund" };
        private static readonly BudgetProcessCategory[] BudgetTabCategories =
            { BudgetProcessCategory.Tax, BudgetProcessCategory.Spending, BudgetProcessCategory.Welfare, BudgetProcessCategory.Infrastructure, BudgetProcessCategory.Swf };

        /// <summary>§734: the page's slips, built each frame as the page draws.</summary>
        private PeopleSlips.Book _budgetSlipBook = new PeopleSlips.Book();

        // ---- K322 S2 (K320, Elias 2026-10-30; K321's caps): the page at rest is labels and one-line captions; the sentences they replaced are slips ----

        /// <summary>K321 (f): a level-1 slip line holds at most this many counted words, and a slip at most <see cref="BudgetSlipLineCap"/> lines under its head.</summary>
        private const int BudgetSlipWordCap = 10;
        private const int BudgetSlipLineCap = 4;

        /// <summary>K321 (b): the most counted words a caption at rest may hold.</summary>
        private const int BudgetRestWordCap = 8;

        /// <summary>Where a packed slip may break a line: the reachability check's own segment breaks (a middle dot, a spaced dash or slash, a colon, a
        /// semicolon, a full stop, a comma) and the em dash, so no segment the check reads is ever split across two lines.</summary>
        private static readonly System.Text.RegularExpressions.Regex BudgetSlipBreaks =
            new System.Text.RegularExpressions.Regex(@"( · | - | — | / |: |; |\. |, )", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// <summary>
        /// K322 S2: a sentence the page drew at rest, as its slip - its words verbatim in the slip's capitals, packed into lines of at most ten counted words
        /// at the sentence's own breaks (a line ends where a break was, and the break's mark goes), at most four lines; past four, the fourth line is a marked
        /// term and the rest is that term's level-2 slip, one line. Nothing is reworded, so every segment the reachability check reads is on the slip.
        /// </summary>
        private static SlipContent BudgetPackedSlip(string head, string text, PeopleSlips.Book book)
        {
            var slip = new SlipContent(head);
            if (string.IsNullOrEmpty(text)) { return slip; }
            string[] parts = BudgetSlipBreaks.Split(text.Trim());
            var lines = new List<string>();
            var line = new System.Text.StringBuilder();
            int words = 0;
            for (int i = 0; i < parts.Length; i += 2)
            {
                string segment = parts[i].Trim();
                if (segment.Length == 0) { continue; }
                int w = RangeCaptions.CountedWords(segment);
                if (line.Length > 0 && words + w > BudgetSlipWordCap) { lines.Add(BudgetSlipLine(line)); line.Clear(); words = 0; }
                else if (line.Length > 0) { line.Append(parts[i - 1]); }
                line.Append(segment);
                words += w;
            }
            if (line.Length > 0) { lines.Add(BudgetSlipLine(line)); }
            if (lines.Count <= BudgetSlipLineCap)
            {
                foreach (string l in lines) { slip.Add(l); }
                return slip;
            }
            for (int i = 0; i < BudgetSlipLineCap - 1; i++) { slip.Add(lines[i]); }
            string term = lines[BudgetSlipLineCap - 1];
            slip.Add("[[" + term + "]]");
            book.Terms[term] = new SlipContent(term).Add(string.Join(" · ", lines.GetRange(BudgetSlipLineCap, lines.Count - BudgetSlipLineCap)));
            return slip;
        }

        private static string BudgetSlipLine(System.Text.StringBuilder line) => line.ToString().Trim().TrimEnd('.', ',', ';', ':').Trim().ToUpperInvariant();

        /// <summary>A count of days as a caption prints it.</summary>
        private static string BudgetDays(int days) => days.ToString(CultureInfo.InvariantCulture) + (days == 1 ? " day" : " days");

        /// <summary>K322 S2: a kept section's caption on its rule (the Statistics sheet's form) with its slip on the caption's words.</summary>
        private void BudgetCaptionWithSlip(string caption, string anchor, SlipContent slip)
        {
            Rect row = DrawStatsSectionCaption(caption);
            GUIStyle face = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width, Mathf.Ceil(face.CalcSize(new GUIContent(caption)).x) + 4f), row.height), anchor);
            _budgetSlipBook.Anchors[anchor] = slip;
        }

        /// <summary>K322 S2 (K320): a kept lever's lock as this page draws it - the lock and its word at rest, the role's reason (a sentence; the shared
        /// <c>DrawLeverLock</c> prints it whole) its slip. False, drawing nothing, where the player's party may introduce the bill.</summary>
        private bool BudgetLock(Congress.Kind kind, string anchor)
        {
            if (_playerCountry == null || _simulationManager.PlayerMayIntroduce(PlayerCountryId, kind, null, out string lockedBecause)) { return false; }
            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleLeft);
            float h = Mathf.Ceil(DeskCaptionHeight(caption)) + V35.Px(4f);
            Rect row = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true));
            float glyph = Mathf.Min(h, V35.Px(14f));
            float wordWidth = Mathf.Ceil(caption.CalcSize(new GUIContent("LOCKED")).x) + 2f;
            var hit = new Rect(row.x, row.y, glyph + V35.Px(6f) + wordWidth, row.height);
            if (Event.current.type == EventType.Repaint)
            {
                bool ambient = GUI.enabled;
                GUI.enabled = true;   // the lock reads at full ink whatever the family's ambient state, as DrawLeverLock's caption did
                DrawStateGlyph(new Rect(row.x, row.y + Mathf.Round((row.height - glyph) * 0.5f), glyph, glyph), Symbol.Locked, PoliSimTheme.TextSecondary);
                GUI.Label(new Rect(row.x + glyph + V35.Px(6f), row.y, wordWidth, row.height), "LOCKED", caption);
                GUI.enabled = ambient;
            }
            SlipAnchor(hit, anchor);
            _budgetSlipBook.Anchors[anchor] = BudgetPackedSlip("LOCKED", lockedBecause, _budgetSlipBook);
            return true;
        }

        /// <summary>Caption segments joined by the middle dot while they stay inside <see cref="BudgetRestWordCap"/> counted words - the first always, a later one
        /// only where it fits (its words are the slip's either way).</summary>
        private static string BudgetRestJoin(params string[] segments)
        {
            string joined = null;
            foreach (string s in segments)
            {
                if (string.IsNullOrEmpty(s)) { continue; }
                if (joined == null) { joined = s; continue; }
                string next = joined + " · " + s;
                if (RangeCaptions.CountedWords(next) <= BudgetRestWordCap) { joined = next; }
            }
            return joined ?? string.Empty;
        }

        private void DrawBudgetProcessTab(float availableHeight, float availableWidth)
        {
            // P2-1.1: the sheet is sized to the FRAME, not to its content.
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            _budgetSlipBook = new PeopleSlips.Book();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            int selected = System.Array.IndexOf(BudgetTabCategories, _budgetProcessCategory);
            int clicked = DrawV35TitleTabs(titleRow, "Budget", BudgetTabs, selected, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), r => SlipAnchor(r, "budget:title"));
            if (clicked >= 0) { _budgetProcessCategory = BudgetTabCategories[clicked]; }
            _budgetSlipBook.Anchors["budget:title"] = new SlipContent("BUDGET")
                .Add("REVENUE AND SPENDING ARE THE COMPOSITION'S TABS; WELFARE, INFRASTRUCTURE AND THE FUND ARE THE GAME'S OTHER THREE")
                .Add("EVERY DIAL ON EVERY TAB RIDES ONE BUDGET BILL");
            GUILayout.Space(V35.Px(4f));

            // the preview the panel and the strip read: with and without the draft, recomputed when the draft moves (debounced) and not otherwise
            if (PolicyInputsChangedSinceLastPreview()) { RecomputePolicyPreview(); }

            float scrollHeight = availableHeight - titleHeight - V35.Px(4f) - _labelStyle.fontSize * 2f;
            float contentWidth = StatsContentWidth(availableWidth);
            int scrolledFrom = _slipAnchors.Count;
            _budgetProcessCenterScrollPosition = GUILayout.BeginScrollView(_budgetProcessCenterScrollPosition, GUILayout.Height(scrollHeight));
            V35.FloorGuarded = true;   // the v3.5 parts; a kept part lifts it around itself
            BudgetBill draft = BuildBudgetBillFromDrafts();
            int changes = BudgetDraftChanges(draft);
            DrawBudgetStrip(contentWidth, changes);

            float leftWidth = V35Span(contentWidth, 8), rightWidth = V35Span(contentWidth, 4), gutter = V35.Px(V35.Gutter);
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            GUILayout.BeginVertical(GUILayout.Width(leftWidth));
            switch (_budgetProcessCategory)
            {
                case BudgetProcessCategory.Tax:
                    DrawBudgetRevenue(leftWidth);
                    break;
                case BudgetProcessCategory.Spending:
                    DrawBudgetSpending(leftWidth);
                    break;
                default:
                    DrawBudgetKeptCategory(leftWidth);
                    break;
            }
            DrawBudgetAction(leftWidth, draft, changes);
            DrawUsCongressVerbs(leftWidth);   // PS-6 K22, K23, K75: the debt limit's verbs and a player-president's own withhold - nothing outside the US Congress
            DrawBudgetKeptReadings(leftWidth);
            GUILayout.EndVertical();
            GUILayout.Space(gutter);
            GUILayout.BeginVertical(GUILayout.Width(rightWidth));
            DrawBudgetIfPassed(rightWidth, draft, changes);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            V35.FloorGuarded = false;
            GUILayout.Space(V35.Px(12f));
            GUILayout.EndScrollView();
            Rect view = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                // the anchors inside the scroll were registered in its content's coordinates; the slips draw over the sheet (Statistics' rule)
                for (int i = _slipAnchors.Count - 1; i >= scrolledFrom; i--)
                {
                    (string id, Rect r) = _slipAnchors[i];
                    var moved = new Rect(r.x + view.x - _budgetProcessCenterScrollPosition.x, r.y + view.y - _budgetProcessCenterScrollPosition.y, r.width, r.height);
                    float top = Mathf.Max(moved.y, view.y), bottom = Mathf.Min(moved.yMax, view.yMax);
                    if (bottom <= top) { _slipAnchors.RemoveAt(i); continue; }
                    _slipAnchors[i] = (id, new Rect(moved.x, top, moved.width, bottom - top));
                }
            }
            GUILayout.EndVertical();
            Rect sheet = GUILayoutUtility.GetLastRect();
            if (!DeskProvenance.On) { DrawSlips(_budgetSlipBook, sheet); }
        }

        /// <summary>§734: what the draft changes, counted the way the bill carries it - a drafted tax rate, a drafted band, a spending line's figure, a
        /// welfare programme's generosity, the fund's fields, the pension age. The *Introduce* button prints it; the panel draws only while it is above zero.</summary>
        private int BudgetDraftChanges(BudgetBill draft)
        {
            int n = 0;
            foreach (TaxLine line in _playerCountry.TaxLines)
            {
                if (draft.TaxLines.TryGetValue(line.Type, out float rate) && !Mathf.Approximately(rate, line.Rate)) { n++; }
            }
            foreach (KeyValuePair<TaxType, float[]> brackets in draft.BracketRates) { if (brackets.Value != null) { foreach (float r in brackets.Value) { if (r >= 0f) { n++; } } } }
            foreach (TaxType carried in draft.CarriedTaxLines) { if (!draft.TaxLines.ContainsKey(carried)) { n++; } }   // AF-36: a carry of a standing figure is a change of its own
            n += draft.SpendingNominalTargets.Count;
            foreach (WelfareProgram program in _playerCountry.WelfarePrograms)
            {
                if (draft.WelfarePrograms.TryGetValue(program.Type, out float level) && !Mathf.Approximately(level, program.GenerosityLevel)) { n++; }
            }
            SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
            if (draft.SwfShouldExist != (fund != null)) { n++; }
            if (fund != null)
            {
                if (!Mathf.Approximately(draft.SwfContributionRatePercent, fund.ContributionRatePercent)) { n++; }
                if (!Mathf.Approximately(draft.SwfEquitiesWeight, fund.EquitiesWeight) || !Mathf.Approximately(draft.SwfBondsWeight, fund.BondsWeight)
                    || !Mathf.Approximately(draft.SwfInfrastructureWeight, fund.InfrastructureWeight) || !Mathf.Approximately(draft.SwfRealEstateWeight, fund.RealEstateWeight)) { n++; }
            }
            if (draft.PensionAgeSet) { n++; }
            return n;
        }

        // =============================================================================================================================================
        // The strip: the balance, the debt, last year's revenue and spending
        // =============================================================================================================================================

        /// <summary>
        /// The four tiles the composition sets across the top. Elias's ruling (§725): the balance and the debt in the neutral ink, the warning only past the
        /// country's statutory rule, the slip naming it; a change in the balance in the direction ink. While a draft stands the balance is THIS year's with the
        /// draft, in the draft's ink (the composition's). The model keeps a yearly history of the balance only - the debt, the revenue and the spending
        /// carry no change rather than an invented one.
        /// </summary>
        private void DrawBudgetStrip(float width, int changes)
        {
            Country c = _playerCountry;
            EconomyState state = c.State;
            FiscalTurnReport lastYear = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal);
            float nominalGdp = Mathf.Max(0.0001f, state.NominalGdp);

            // the balance
            var balance = new V35TileData { Icon = "scales", IconInk = area, Name = "Balance", FigurePx = V35.FigureSmall };
            var balanceSlip = new SlipContent("BALANCE");
            float? withDraft = changes > 0 && _cachedPreview != null ? _cachedPreview.RevenueEstimate - _cachedPreview.SpendingEstimate : (float?)null;
            if (withDraft.HasValue)
            {
                balance.Figure = UiFormat.MoneyDelta(withDraft.Value, MoneyUnit.Billions);
                balance.FigureInk = PoliSimTheme.Caution;
                balanceSlip.Add("THIS YEAR, WITH THE DRAFT · " + balance.Figure).Add("THE PREVIEW'S OWN FIGURE - REVENUE LESS SPENDING ON THE YEAR AHEAD");
            }
            // GA-2-7 (2026-10-14): a dash before the first close (§220) - the running accumulator `state.Budget` stood in here, a figure no year computed
            float? closed = lastYear?.BudgetBalance;
            if (!withDraft.HasValue)
            {
                if (closed.HasValue) { balance.Figure = UiFormat.MoneyDelta(closed.Value, MoneyUnit.Billions); } else { balance.Glyph = Symbol.Absent; }
                float? deficit = DerivedStats.DeficitPercentOfGdp(c, lastYear);
                if (deficit.HasValue) { balance.FigureInk = V35.FiscalInk(PlayerCountryId, FiscalRules.Measure.Deficit, deficit.Value, PoliSimTheme.TextPrimary, out string rule); if (rule != null) { balanceSlip.Add(rule); } }
                List<float> annual = c.History?.BudgetBalanceAnnual;
                if (annual != null && annual.Count >= 2)
                {
                    float d = annual[annual.Count - 1] - annual[annual.Count - 2];
                    if (Mathf.Abs(d) >= 0.05f)
                    {
                        balance.Change = (d > 0f ? "▲ " : "▼ ") + UiFormat.Money(Mathf.Abs(d), MoneyUnit.Billions); balance.ChangeInk = V35.DirectionNeutral;
                        balanceSlip.Add((d > 0f ? "UP " : "DOWN ") + UiFormat.Money(Mathf.Abs(d), MoneyUnit.Billions) + " ON THE YEAR BEFORE");   // §41: the slip carries the change too
                    }
                }
            }
            balanceSlip.Add(!closed.HasValue ? "BALANCE · NO YEAR HAS CLOSED YET" : (_simulationManager.CurrentTurn == 0 ? "LAST YEAR, THE SEED · " : "LAST YEAR · ") + UiFormat.MoneyDelta(closed.Value, MoneyUnit.Billions));
            if (lastYear != null && lastYear.OneOffLines != null && lastYear.OneOffLines.Count > 0)
            {
                balanceSlip.Add("OF WHICH ONE-OFFS · " + UiFormat.MoneyDelta(lastYear.OneOffs, MoneyUnit.Billions) + " (" + lastYear.OneOffLines.Count + ", ITEMISED IN THE BOOK BELOW)");   // AF-17 (q17)
            }
            balanceSlip
                .Add("A CHANGE IN THE BALANCE HAS NO DIRECTION MOST AGREE ON - ITS ARROW IN THE NEUTRAL INK; THE WARNING IS A LEVEL'S, PAST THE COUNTRY'S RULE")
                .Add(FiscalRules.NationalNote(_playerCountry, _simulationManager.CurrentDate));   // PS-6 US-24: the US note live - the limit, the debt subject to it, the measures' days left

            // the debt
            var debt = new V35TileData { Icon = "debt", IconInk = area, Name = "Government debt", Figure = UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), FigurePx = V35.FigureSmall };
            debt.FigureInk = V35.FiscalInk(PlayerCountryId, FiscalRules.Measure.Debt, state.DebtToGdpRatio, PoliSimTheme.TextPrimary, out string debtRule);
            // K322 S2 (K320 (3)): the slip at its caps - the share of GDP (and the country's rule, a marked term), the federal debt limit and the debt crisis as
            // marked terms whose level 2 holds their sentences (the slip ran to eight lines, two of them past ten words), and the history's caveat
            var debtSlip = new SlipContent("GOVERNMENT DEBT · " + debt.Figure);
            var debtRuleWords = new List<string>();
            if (debtRule != null) { debtRuleWords.Add(debtRule); }
            if (FiscalRules.Notice(PlayerCountryId, FiscalRules.Measure.Debt, state.DebtToGdpRatio) is string debtNotice) { debtRuleWords.Add(debtNotice.ToUpperInvariant()); }   // §758 (ruling B6): a notice, neutral ink
            string debtGdp = UiFormat.Number(state.DebtToGdpRatio, 1) + "% OF GDP";
            if (debtRuleWords.Count > 0) { _budgetSlipBook.Terms["THE DEBT RULE"] = new SlipContent("THE DEBT RULE").Add(string.Join(" · ", debtRuleWords)); debtGdp += " · [[THE DEBT RULE]]"; }
            debtSlip.Add(debtGdp);
            BudgetBill pendingRaise = _simulationManager.GetPendingDebtLimitBill(PlayerCountryId);
            List<string> limitLines = FiscalRules.DebtLimitLines(_playerCountry, _simulationManager.CurrentDate, pendingRaise);   // PS-6 US-24: the federal debt limit, live (nothing where the game holds no limit)
            List<string> crisisLines = FiscalRules.CrisisLines(_playerCountry, _simulationManager.CurrentDate);   // MF q33: the debt crisis, live (nothing while the country has the market)
            var debtSections = new List<string>();
            if (limitLines.Count > 0) { _budgetSlipBook.Terms["THE FEDERAL DEBT LIMIT"] = new SlipContent("THE FEDERAL DEBT LIMIT").Add(string.Join(" ", limitLines)); debtSections.Add("[[THE FEDERAL DEBT LIMIT]]"); }   // PS-6 US-35 (board 30c): its lines in sentence case
            if (crisisLines.Count > 0) { _budgetSlipBook.Terms["THE DEBT CRISIS"] = new SlipContent("THE DEBT CRISIS").Add(string.Join(" ", crisisLines)); debtSections.Add("[[THE DEBT CRISIS]]"); }
            if (debtSections.Count > 0) { debtSlip.Add(string.Join(" · ", debtSections)); }
            // PS-6 US-35 (board 30c, answer 4): the tile keeps its figure, and one chip under the name gives the limit's state - or, below it, a muted line
            debt.Chip = FiscalRules.DebtLimitChip(_playerCountry, _simulationManager.CurrentDate, pendingRaise, out FiscalRules.DebtLimitInk limitInk, out debt.Sub);
            debt.ChipGround = limitInk == FiscalRules.DebtLimitInk.Cut ? PoliSimTheme.Bad : limitInk == FiscalRules.DebtLimitInk.SetAside ? PoliSimTheme.Neutral : PoliSimTheme.Caution;
            _budgetSlipBook.Terms["NO YEARLY HISTORY"] = new SlipContent("NO YEARLY HISTORY").Add("THE STOCK KEEPS NO YEARLY HISTORY - NO CHANGE IS DRAWN RATHER THAN ONE INVENTED");
            debtSlip.Add("NO CHANGE DRAWN · [[NO YEARLY HISTORY]]");

            // last year's revenue and spending
            var revenue = new V35TileData { Icon = "in", IconInk = area, Name = "Revenue", Figure = lastYear != null ? UiFormat.Money(lastYear.Revenue, MoneyUnit.Billions) : null, Glyph = lastYear != null ? (Symbol?)null : Symbol.Absent, FigurePx = V35.FigureSmall };
            var spending = new V35TileData { Icon = "out", IconInk = area, Name = "Spending", Figure = lastYear != null ? UiFormat.Money(lastYear.TotalSpending, MoneyUnit.Billions) : null, Glyph = lastYear != null ? (Symbol?)null : Symbol.Absent, FigurePx = V35.FigureSmall };
            var revenueSlip = new SlipContent(lastYear != null ? "REVENUE · LAST YEAR · " + revenue.Figure : SymbolRegistry.Word(Symbol.Absent)).Add(lastYear != null ? "THE CLOSED YEAR'S BOOK" : "REVENUE · NO YEAR HAS CLOSED YET");
            var spendingSlip = new SlipContent(lastYear != null ? "SPENDING · LAST YEAR · " + spending.Figure : SymbolRegistry.Word(Symbol.Absent)).Add(lastYear != null ? "THE CLOSED YEAR'S BOOK" : "SPENDING · NO YEAR HAS CLOSED YET")
                .Add("THE LINES' SHARE OF GDP " + UiFormat.Number(LinesShareOfGdpPercent(), 1) + "% · NEXT YEAR " + UiFormat.Number(LinesShareOfGdpNextPercent(), 1) + "% - AT POTENTIAL GROWTH AND THE PRINTED INFLATION");

            V35TileData[] tiles = { balance, debt, revenue, spending };
            SlipContent[] slips = { balanceSlip, debtSlip, revenueSlip, spendingSlip };
            string[] ids = { "budget:balance", "budget:debt", "budget:revenue", "budget:spending" };
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(width, 3), rowHeight = 0f;
            foreach (V35TileData t in tiles) { rowHeight = Mathf.Max(rowHeight, V35TileHeight(t)); }
            Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
            for (int i = 0; i < tiles.Length; i++)
            {
                var r = new Rect(row.x + i * (tileWidth + gutter), row.y, tileWidth, rowHeight);
                DrawV35Tile(r, tiles[i]);
                SlipAnchor(r, ids[i]);
                _budgetSlipBook.Anchors[ids[i]] = slips[i];
            }
            GUILayout.Space(gutter);
            DrawBudgetProgrammeTarget(width, withDraft);   // MF q33 (D-WORKLIST §1, board 32e): a programme year's target where the deficit is drafted - nothing outside a programme
        }

        // =============================================================================================================================================
        // Revenue: the taxes as tiles
        // =============================================================================================================================================

        private static string BudgetTaxIcon(TaxType type)
        {
            switch (type)
            {
                case TaxType.IncomeTax: return "person";
                case TaxType.CorporateTax: return "factory";
                case TaxType.VAT: return "receipt";
                case TaxType.PayrollTax: return "jobs";
                case TaxType.CapitalGainsTax: return "chart";
                case TaxType.SalesTax: return "tag";
                case TaxType.ExciseTax: return "drop";
                case TaxType.PropertyTax: return "office";
                case TaxType.EstateTax: return "home";
                case TaxType.WealthTax: return "gem";
                case TaxType.CarbonTax: return "smoke";
                case TaxType.StampDuty: return "stamp";
                default: return "trade";
            }
        }

        /// <summary>
        /// The taxes as tiles, and the income tax's statute under them. Every rate here stays a DRAFT (adjusting costs nothing, no vote needed); the
        /// *Introduce budget bill* action carries Tax, Spending, Welfare and the Fund together as one omnibus bill, and a PASSED bill is the only way a draft
        /// here reaches the standing TaxLines.
        ///
        /// <para><b>STABLE CONTROL LAYOUT PATTERN</b> (moved here from the retired DrawTaxPolicyContent at §734 - mandatory for every gated tab; see
        /// "Background/timed state mutation vs. active UI interaction" among the working-discipline failure patterns, preserved in COMPLETED.md section 35):
        /// a background system can resolve on ANY simulated day - a bill passing or failing - and mutate the exact standing value a slider on this tab is
        /// reading, on a day the player has a multi-frame drag in progress on that slider. GUILayout allocates control IDs positionally (call order within
        /// OnGUI), not by a stable key, so <see cref="DrawBudgetTaxTile"/> (and <see cref="DrawBudgetAction"/>, the omnibus bill's own controls) must NEVER
        /// change which controls they emit, in what order, based on live or mutable state (a bill pending or not, a TaxType drafted-implemented or not).
        /// Swapping a Button for a Label, or omitting a Slider some frames, changes the control count or sequence a currently-hot (mid-drag) control was
        /// allocated against, which is a documented Unity IMGUI hang/desync trigger inside a ScrollView. The fix: every control this tab can ever draw is
        /// drawn EVERY frame, in the SAME order; "not currently applicable" is represented via GUI.enabled = false (greyed, non-interactive, but still present
        /// and control-ID-stable), never by branching the control itself in or out of existence. Every gated tab follows this shape from its first draft.</para>
        /// </summary>
        private void DrawBudgetRevenue(float width)
        {
            DrawBudgetSectionHead("Taxes", "budget:taxes", width);
            _budgetSlipBook.Anchors["budget:taxes"] = new SlipContent("TAXES")
                .Add("A RATE'S DRAFT RIDES THE BUDGET BILL · LEVYING OR REMOVING A TAX IS A BILL OF ITS OWN, VOTED WHEN IT IS INTRODUCED")
                .Add("THE CHIP IS WHAT THE TAX RAISES IN A YEAR AT THE STANDING RATE");
            DrawBudgetFinanceStance(width);

            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(V35Span(width, 12) + gutter * 0f, 6);
            List<TaxLine> lines = _playerCountry.TaxLines;
            for (int i = 0; i < lines.Count; i += 2)
            {
                float rowHeight = BudgetDialTileHeight(true);
                Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
                for (int k = 0; k < 2 && i + k < lines.Count; k++)
                {
                    DrawBudgetTaxTile(new Rect(row.x + k * (tileWidth + gutter), row.y, tileWidth, rowHeight), lines[i + k]);
                }
                GUILayout.Space(gutter);
            }

            // the income tax's statute - its curve and its bands - kept as built, in a card of its own
            TaxLine income = lines.Find(l => l.Type == TaxType.IncomeTax);
            if (income != null && TaxSchedule.Of(_playerCountry.Id).Kind != TaxScheduleKind.Flat)
            {
                DrawBudgetSectionHead("Income tax · the statute", "budget:statute", width);
                // K322 S2 (K320): the slip at its caps - the kind, the curve's x axis in full (the foot under the curve says it in seven words at rest), the
                // reading of the curve, and the statute's own words (its citation verbatim, the earned income credit) behind marked terms, one line each
                TaxSchedule.Statute statute = TaxSchedule.Of(_playerCountry.Id);
                var statuteSlip = new SlipContent("THE STATUTE")
                    .Add(TaxSchedule.KindWord(statute.Kind))
                    .Add(BudgetStatuteAxis)
                    .Add("SOLID THE STATUTE, DASHED THE DRAFT · [[ONE BILL]]");
                _budgetSlipBook.Terms["ONE BILL"] = new SlipContent("ONE BILL")
                    .Add("THE MARGINAL RATE OVER INCOME, THE STANDING STATUTE SOLID AND THE DRAFT DASHED; ONE DIAL PER BAND · ONE BILL FOR THE DIAL AND THE BANDS · KEPT AS BUILT - THE COMPOSITION DRAWS THE CURVE UNDER THE INCOME TAX'S DIAL; ITS BANDS ARE ASKED");
                string credit = BudgetCreditWords(income);
                if (statute.CitationLine != null || credit != null)
                {
                    var terms = new List<string>();
                    if (statute.CitationLine != null) { terms.Add("[[THE STATUTE VERBATIM]]"); _budgetSlipBook.Terms["THE STATUTE VERBATIM"] = new SlipContent("THE STATUTE VERBATIM").Add(statute.CitationLine); }
                    if (credit != null) { terms.Add("[[THE EARNED INCOME CREDIT]]"); _budgetSlipBook.Terms["THE EARNED INCOME CREDIT"] = new SlipContent("THE EARNED INCOME CREDIT").Add(credit); }
                    statuteSlip.Add(string.Join(" · ", terms));
                }
                _budgetSlipBook.Anchors["budget:statute"] = statuteSlip;
                bool guarded = V35.FloorGuarded;
                V35.FloorGuarded = false;
                GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
                _incomeTaxTrackRect = Rect.zero;   // the curve lays out in the card's own columns
                DrawTaxScheduleRows(income, FindPendingTaxProgramBill(TaxType.IncomeTax));
                GUILayout.EndVertical();
                V35.FloorGuarded = guarded;
                GUILayout.Space(gutter);
            }
        }

        /// <summary>K322 S2: the income-tax curve's x axis as the statute's slip says it in full (F4-4's foot, word for word), and as the foot says it at rest.</summary>
        private const string BudgetStatuteAxis = "X: THE TARIFF'S OWN CURRENCY, TO ITS TOP THRESHOLD × 1.5";
        private const string BudgetStatuteFoot = "X: THE TARIFF'S CURRENCY, TO TOP THRESHOLD × 1.5";

        /// <summary>P6-E1 (2026-09-17), moved to the statute's slip at K322 S2: what the earned income credit does to the mean income's rate - the rate with it
        /// against the rate without, at the draft's lever - and whether the yield reads it; null where the statute carries no credit.</summary>
        private string BudgetCreditWords(TaxLine taxLine)
        {
            if (!TaxSchedule.CarriesCredit(_playerCountry.Id)) { return null; }
            float draftLever = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            double without = TaxSchedule.AverageEffectiveRateAtMeanIncome(_playerCountry, taxLine, draftLever);
            double with = TaxSchedule.AverageEffectiveRateWithCreditAtMeanIncome(_playerCountry, taxLine, draftLever);
            return "THE EARNED INCOME CREDIT (JOBBSKATTEAVDRAGET, SKV 433 " + EarnedIncomeCredit.IncomeYear.ToString(CultureInfo.InvariantCulture) + "): "
                + with.ToString("0.0", CultureInfo.InvariantCulture) + " % WITH IT AGAINST " + without.ToString("0.0", CultureInfo.InvariantCulture)
                + " % WITHOUT AT THE MEAN INCOME · " + (EarnedIncomeCredit.Live ? "IN THE YIELD" : "HELD OFF THE YIELD UNTIL ITS FAMILY IS RULED");
        }

        /// <summary>
        /// §755 (Elias's ruling A2: "A partner holding Finance acts through the fiscal stance only. The rule is symmetric"): THE FINANCE PARTNER'S STANCE,
        /// at the head of the Taxes tab, where a partner holds Finance in the player's government - an AI partner's read (what it asks, what it has moved),
        /// the player's own as its dial (the stance it asks, within <see cref="FinancePartner.PlayerTargetLimit"/> points of GDP; set at once - the
        /// minister's call, no bill: the step rides the year's budget). Drawn only where a partner holds Finance, a role that changes at a formation, never
        /// inside a frame; the dial is interactive only for the player's own party.
        /// </summary>
        private void DrawBudgetFinanceStance(float width)
        {
            Country c = _playerCountry;
            string holder = FinancePartner.Holder(c);
            if (holder == null) { return; }
            bool mine = holder == c.PlayerPartyAbbrev;
            FinancePartner.Target(c, holder, out float asked);
            float moved = FinancePartner.Applied(c, holder);
            string who = PartySystems.ShortName(PlayerCountryId, holder);
            string Signed(float v) => (v > 0f ? "+" : v < 0f ? "−" : string.Empty) + UiFormat.Number(Mathf.Abs(v), 2);
            float tileWidth = V35Span(width, 6), rowHeight = BudgetDialTileHeight(false);
            Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
            _dialSlipBookOverride = _budgetSlipBook;
            float set = DrawDialRow("Fiscal stance", asked, asked, -FinancePartner.PlayerTargetLimit, FinancePartner.PlayerTargetLimit, "F2", " pp", "pp of GDP",
                new Rect(row.x, row.y, tileWidth, rowHeight), new V35DialFace
                {
                    Icon = "coins", Title = mine ? "Your fiscal stance" : UiFormat.Possessive(who, false, true) + " fiscal stance", Area = UiPalette.SystemArea.Fiscal,
                    Figure = v => Signed(v) + " pp of GDP",
                    EndLeft = "Tighten", EndRight = "Expand",
                    Census = (mine ? "YOU HOLD FINANCE AS A PARTNER · YOU ACT THROUGH THE STANCE ONLY" : "THE FINANCE MINISTER (" + who.ToUpperInvariant() + ") ACTS THROUGH THE STANCE ONLY · THE HEAD OF GOVERNMENT KEEPS EVERY OTHER LEVER")
                        + " · MOVED SO FAR IN THIS GOVERNMENT " + Signed(moved) + " PP · AT MOST " + UiFormat.Number(FinancePartner.StepPointsPerYear, 2) + " PP OF GDP A YEAR, THROUGH THE LINES - IN A TIGHTENING THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE"
                        // §773 (Elias's ruling E2): "A rate moves only through the tax act (Sejm vote, then the veto), whoever proposes it."
                        + (PoliSim.Elections.WorldClock.StatutePartsAreActs(PlayerCountryId)   // US-20: Poland's procedure, not its veto
                            ? " · WHERE THE PRESIDENT HOLDS A VETO, THE RATES' PART IS A TAX ACT OF ITS OWN - THE SEJM VOTES IT, THEN THE PRESIDENT; WHERE IT FALLS THE OLD RATES STAND AND ONLY THE LINES' PART COUNTS"
                            : string.Empty),
                }, mine);
            _dialSlipBookOverride = null;
            if (mine && !Mathf.Approximately(set, asked)) { _simulationManager.SetFinanceStanceTarget(PlayerCountryId, set); }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>A section's head over the left columns, its slip on its words.</summary>
        private void DrawBudgetSectionHead(string title, string anchor, float width)
        {
            float h = V35.Px(26f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            DrawV35SectionHead(row, title);
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width, face.CalcSize(new GUIContent(title.ToUpperInvariant())).x + 4f), row.height), anchor);
            GUILayout.Space(V35.Px(6f));
        }

        // ---- the dial tile ----

        /// <summary>The dial tile's measures: the head (a 30 px icon; the figure over the name), the track under it, its end labels, and - where the
        /// dial carries one - the switch row.</summary>
        private float BudgetDialTileHeight(bool withSwitch)
        {
            float figure = Mathf.Ceil(V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y);
            float name = Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y);
            float head = Mathf.Max(V35.Px(V35.ListIcon), figure + 2f + name);
            float floor = Mathf.Ceil(V35Serif(V35.Floor, PoliSimTheme.TextMuted).CalcSize(new GUIContent("0")).y);
            float track = V35.Px(14f) + LedgerRow.TrackHeight(_labelStyle) + V35.Px(4f) + floor;
            return V35.Px(V35.CardPadY) * 2f + head + track + (withSwitch ? V35.Px(8f) + V35.Px(22f) : 0f);
        }

        /// <summary>The tile's card in its state: the paper and its edge; a drafted dial's edge doubled in the draft's ink (the inside does not move); an
        /// Off dial muted, its edge dashed. Returns the inside.</summary>
        private static Rect DrawBudgetTileCard(Rect r, bool drafted, bool off)
        {
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(r, off ? V35.OffPaper : V35.CardPaper);
                if (off)
                {
                    DrawDashedRule(new Rect(r.x, r.y, r.width, 1f), V35.DataSand, 4f, 3f);
                    DrawDashedRule(new Rect(r.x, r.yMax - 1f, r.width, 1f), V35.DataSand, 4f, 3f);
                    for (float y = r.y; y < r.yMax; y += 7f) { PoliSimTheme.Rule(new Rect(r.x, y, 1f, Mathf.Min(4f, r.yMax - y)), V35.DataSand); PoliSimTheme.Rule(new Rect(r.xMax - 1f, y, 1f, Mathf.Min(4f, r.yMax - y)), V35.DataSand); }
                }
                else
                {
                    Color edge = drafted ? PoliSimTheme.Caution : V35.CardEdge;
                    float t = drafted ? 2f : 1f;
                    PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, t), edge);
                    PoliSimTheme.Rule(new Rect(r.x, r.yMax - t, r.width, t), edge);
                    PoliSimTheme.Rule(new Rect(r.x, r.y, t, r.height), edge);
                    PoliSimTheme.Rule(new Rect(r.xMax - t, r.y, t, r.height), edge);
                }
            }
            float padX = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY);
            return new Rect(r.x + padX, r.y + padY, Mathf.Max(1f, r.width - padX * 2f), Mathf.Max(1f, r.height - padY * 2f));
        }

        /// <summary>A tile's chip at the head's right (what a tax raises; a line's share of GDP): the figure in the light ink on the chip's dark - the draft's
        /// ink while the dial is drafted. Returns its rect (empty where there is none).</summary>
        private Rect DrawBudgetChip(Rect inner, string text, bool drafted)
        {
            if (string.IsNullOrEmpty(text)) { return new Rect(inner.xMax, inner.y, 0f, 0f); }
            GUIStyle face = V35Mono(V35.Floor, V35.OnDataDark, bold: true, TextAnchor.MiddleCenter);
            Vector2 size = face.CalcSize(new GUIContent(text));
            var chip = new Rect(inner.xMax - Mathf.Ceil(size.x) - V35.Px(10f), inner.y, Mathf.Ceil(size.x) + V35.Px(10f), Mathf.Ceil(size.y) + V35.Px(2f));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(chip, drafted ? PoliSimTheme.Caution : V35.PyramidThreshold);
                PoliSimWidgets.MeasuredLabel(chip, text, face);
            }
            return chip;
        }

        /// <summary>The tile's head: the icon, the figure (none where the dial is off), the name; the chip's width kept clear at the right. Returns the
        /// figure's rect (the film's record).</summary>
        private Rect DrawBudgetTileHead(Rect inner, string icon, Color iconInk, string figure, Color figureInk, string name, Color nameInk, Rect chip)
        {
            float iconSide = V35.Px(V35.ListIcon);
            GUIStyle figureFace = V35Mono(V35.FigureSmall, figureInk, bold: true);
            GUIStyle nameFace = V35Serif(V35.Name, nameInk);
            float fh = Mathf.Ceil(figureFace.CalcSize(new GUIContent("0")).y), nh = Mathf.Ceil(nameFace.CalcSize(new GUIContent("Ag")).y);
            float headHeight = Mathf.Max(iconSide, fh + 2f + nh);
            DrawV35Icon(new Rect(inner.x, inner.y + Mathf.Round((headHeight - iconSide) * 0.5f), iconSide, iconSide), icon, iconInk);
            float x = inner.x + iconSide + V35.Px(12f);
            float y = inner.y + Mathf.Round((headHeight - fh - 2f - nh) * 0.5f);
            float right = (chip.width > 0f ? chip.x - V35.Px(8f) : inner.xMax);
            var figureRect = new Rect(x, y, Mathf.Max(1f, right - x), fh);
            if (Event.current.type == EventType.Repaint)
            {
                if (!string.IsNullOrEmpty(figure))
                {
                    float w = Mathf.Ceil(figureFace.CalcSize(new GUIContent(figure)).x) + 2f;
                    UiOverflowGuard.Check(figure, new Vector2(w, fh), new Vector2(figureRect.width, fh), figureFace.fontSize);
                    GUI.Label(new Rect(x, y, w, fh), figure, figureFace);
                }
                float nameWidth = Mathf.Max(1f, inner.xMax - x);
                PoliSimWidgets.MeasuredLabel(new Rect(x, y + fh + 2f, nameWidth, nh), V35Fit(name, nameFace, nameWidth, out _), nameFace);
            }
            return figureRect;
        }

        /// <summary>The track's band under the head: the track rect (at the ledger's height, the knob's overhang clear of the head) and its two end labels.</summary>
        private Rect BudgetTrackRect(Rect inner, out Rect endLabels)
        {
            float figure = Mathf.Ceil(V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y);
            float name = Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y);
            float head = Mathf.Max(V35.Px(V35.ListIcon), figure + 2f + name);
            float th = LedgerRow.TrackHeight(_labelStyle);
            var track = new Rect(inner.x, inner.y + head + V35.Px(14f), inner.width, th);
            float floor = Mathf.Ceil(V35Serif(V35.Floor, PoliSimTheme.TextMuted).CalcSize(new GUIContent("0")).y);
            endLabels = new Rect(inner.x, track.yMax + V35.Px(4f), inner.width, floor);
            return track;
        }

        private void DrawBudgetEndLabels(Rect lane, string left, string right, bool muted)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            Color ink = muted ? PoliSimTheme.TextMuted : V35.DirectionNeutral;
            PoliSimWidgets.MeasuredLabel(new Rect(lane.x, lane.y, lane.width * 0.5f, lane.height), left, V35Serif(V35.Floor, ink));
            PoliSimWidgets.MeasuredLabel(new Rect(lane.x + lane.width * 0.5f, lane.y, lane.width * 0.5f, lane.height), right, V35Serif(V35.Floor, ink, TextAnchor.MiddleRight));
        }

        /// <summary>A rate at a track's end: a percentage to the point, or a per-tonne figure in the country's currency.</summary>
        private string BudgetTaxEndText(TaxLine line, float value) =>
            line.IsPerTonne ? (value <= 0f ? "0" : value.ToString("F0", CultureInfo.InvariantCulture) + " " + EnergyLayer.CurrencyCode(_playerCountry.Id) + "/t")
                : value.ToString("F0", CultureInfo.InvariantCulture) + "%";

        /// <summary>
        /// One tax as a tile. Two controls, in this order on every frame whatever the state (the control-ID rule, <see cref="DrawBudgetRevenue"/>'s doc): the switch
        /// (a programme bill of its own - levy or remove - introduced on the click; none where the role locks it, as the row's lock always drew none), then
        /// the rate's slider (enabled only on a levied tax with no programme bill pending and no Finance partner holding it). The rate's draft rides the
        /// budget bill; the tile's figure is the draft's, in the draft's ink, while it differs.
        /// <para>(Moved from the retired DrawTaxLineRow at §734, Master Sequence step 5d.) Levying or removing a tax is its OWN standalone TaxProgramBill,
        /// introduced on the click - not drafted first, a binary decision has no "adjust before submitting" step the way a rate does - and resolving apart
        /// from the annual budget cycle; the rate stays a DRAFT feeding the annual BudgetBill. Both taxLine.IsImplemented
        /// (ParliamentSystem.ApplyTaxProgramBillResult) and taxLine.Rate (ParliamentSystem.ApplyBillResult) can change out from under a drag on this
        /// tile, from two independently-resolving bill tiers - which is why its two controls never change in number or order. The switch's slip carries
        /// the verdict a click would face (the bill's own concern, its author voting for it), or the pending bill's.</para>
        /// </summary>
        private void DrawBudgetTaxTile(Rect r, TaxLine taxLine)
        {
            string name = DisplayName.Of(taxLine.Type.ToString());
            string id = "tax:" + taxLine.Type;
            TaxProgramBill pending = FindPendingTaxProgramBill(taxLine.Type);
            bool levied = taxLine.IsImplemented;
            float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            bool drafted = levied && !Mathf.Approximately(draftRate, taxLine.Rate);
            bool interactive = levied && pending == null;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal);

            Rect inner = DrawBudgetTileCard(r, drafted, !levied);
            float revenue = levied ? TaxBases.Revenue(_playerCountry, taxLine) : 0f;
            Rect chip = DrawBudgetChip(inner, levied ? UiFormat.Money(revenue, MoneyUnit.Billions) : null, drafted);
            string figure = !levied ? null : drafted ? TaxRateText(taxLine, draftRate) : TaxRateText(taxLine, taxLine.Rate);
            Rect figureRect = DrawBudgetTileHead(inner, BudgetTaxIcon(taxLine.Type), levied ? area : V35.PyramidThreshold, figure, drafted ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary,
                name, levied ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, chip);
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, figureRect.yMax - inner.y + V35.Px(20f)), id);

            Rect track = BudgetTrackRect(inner, out Rect endLane);
            float switchHeight = V35.Px(22f);
            var switchRect = new Rect(inner.x, inner.yMax - switchHeight, inner.width, switchHeight);

            // ---- control 1 of 2: the switch ----
            bool mayIntroduce = _simulationManager.PlayerMayIntroduce(PlayerCountryId, Congress.Kind.TaxProgram, CabinetPortfolio.FinanceTreasury, out string lockedBecause);   // PS-6 US-19: a revenue bill - in a Congress, the House's majority's too
            var bill = pending ?? new TaxProgramBill { Type = taxLine.Type, IsAdd = !levied };
            BillConcern taxConcern = ParliamentSystem.GetTaxProgramBillConcern(_playerCountry, bill);
            System.DateTime taxVoteDay = ChamberVerdicts.VoteDay(_simulationManager.CurrentDate, pending?.DaysRemaining);
            bool wouldPass = _chamberVerdicts.WouldPass(_playerCountry, taxConcern, taxVoteDay);
            PoliSim.Elections.PresidentialVeto.Outcome taxVeto = _chamberVerdicts.Veto(_playerCountry, taxConcern, taxVoteDay);   // §761: a tax programme's bill is an ordinary statute; F3: the president on its vote's day
            // F3: a statute at risk passes the Sejm and meets the President's draw - the verdict is the count's, the risk beside it as a percentage
            Congress.VetoProjection taxUsVeto = _chamberVerdicts.UsVetoProjection(_playerCountry, taxConcern, taxVoteDay);   // PS-6 US-21: the US president's answer (words only, no chance shown)
            string taxVerdict = !wouldPass ? "WOULD FAIL" : ChamberVerdicts.UsVetoFalls(taxUsVeto) ? "WOULD PASS CONGRESS · VETOED"
                : "WOULD PASS" + (ChamberVerdicts.AtRisk(taxVeto) ? " · VETO RISK " + ChamberVerdicts.RiskWords(taxVeto) : "");
            string switchWord = !mayIntroduce ? "Locked" : pending != null ? "Pending · " + pending.DaysRemaining + " d" : levied ? "On" : "Off";
            Rect switchHit = DrawBudgetSwitch(switchRect, switchWord, levied, mayIntroduce, pending == null);
            if (mayIntroduce && switchHit.width > 0f)
            {
                bool ambient = GUI.enabled;
                GUI.enabled = ambient && pending == null;
                if (PoliSimWidgets.Button(switchHit, GUIContent.none, GUIStyle.none)) { _simulationManager.IntroduceTaxProgramBill(PlayerCountryId, taxLine.Type, !levied); }
                GUI.enabled = ambient;
            }
            SlipAnchor(switchHit, id + "/switch");
            var switchSlip = new SlipContent(!mayIntroduce ? "LOCKED" : pending != null ? "PENDING" : levied ? "ON · LEVIED" : "OFF · NOT LEVIED");
            if (!mayIntroduce) { switchSlip.Add(lockedBecause); }
            else if (pending != null && pending.Docket != null) { switchSlip.Add("A BILL TO " + (pending.IsAdd ? "LEVY" : "REMOVE") + " IT IS ON THE PRESIDENT'S DESK - LAW ON " + DeskLawDay(pending.Docket).ToUpperInvariant() + " UNLESS ANSWERED"); }   // PS-6 US-21b: voted, so no count
            else if (pending != null) { switchSlip.Add("A BILL TO " + (pending.IsAdd ? "LEVY" : "REMOVE") + " IT IS BEFORE " + LegislatureCaps + " · " + pending.DaysRemaining + " DAY(S)").Add("IT " + taxVerdict + " ON TODAY'S COUNT"); }
            else { switchSlip.Add("A CLICK INTRODUCES A BILL OF ITS OWN TO " + (levied ? "REMOVE" : "LEVY") + " IT - VOTED THEN, NOT WITH THE BUDGET").Add("IF INTRODUCED NOW · " + taxVerdict); }
            if (mayIntroduce && ChamberVerdicts.VetoLine(PlayerCountryId, taxVeto) is string taxVetoLine) { switchSlip.Add(taxVetoLine); }
            if (mayIntroduce) { foreach (string line in ChamberVerdicts.UsVetoLines(PlayerCountryId, taxUsVeto)) { switchSlip.Add(line); } }   // PS-6 US-21
            if (mayIntroduce && _chamberVerdicts.StopWords(_playerCountry, taxConcern, taxVoteDay) is string taxCongress) { switchSlip.Add(taxCongress); }   // PS-6 US-18: where it stops in Congress
            _budgetSlipBook.Anchors[id + "/switch"] = switchSlip;

            // ---- control 2 of 2: the rate's slider ----
            float newRate = LedgerRow.Track(track, name, taxLine.Rate, draftRate, taxLine.MinRate, taxLine.MaxRate, interactive, _sliderStyle, _sliderThumbStyle,
                LedgerRow.ScaleOf(_labelStyle), taxLine.DialGrain);
            if (levied)
            {
                // GA-8-2 (2026-10-14, the review's finding 7): a draft is the player's move, never the slider at rest. Written every frame, an untouched
                // line held the rate it showed, and once a boundary moved that rate (the Finance partner's step) the stale draft read as a move, so the
                // bill named the line and set the old rate back. As the spending lines' drafts: a figure at the standing rate holds no draft.
                if (Mathf.Approximately(newRate, taxLine.Rate)) { _taxRateInputs.Remove(taxLine.Type); }
                else { _taxRateInputs[taxLine.Type] = newRate; }
            }

            // ---- control 3, on a per-tonne line a statute moves: the carry (AF-36, Elias's ruling q36: "a bill may carry a passed figure into the next
            // year"). Drawn on such a tile every frame whatever the state, after the two above (the control-ID rule): the line's type and its country's
            // statute are the session's, never mutable state. It drafts; the budget bill carries the figure where there is one to carry ----
            SlipContent carrySlip = null;
            if (taxLine.IsPerTonne && CarbonRateStatute.StatuteMoves(_playerCountry.Id))
            {
                float carryDraft = GetTaxRateInput(taxLine.Type, taxLine.Rate);
                bool carryOn = _taxCarryInputs.ContainsKey(taxLine.Type);
                bool carries = CarbonCarryCarries(taxLine, carryDraft);
                // the review s873b's M1 (a): the player's own budget before the chamber was frozen when it was introduced - a box ticked now would ride no bill
                BudgetBill playerBudget = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
                bool playerBudgetPending = (playerBudget != null && !playerBudget.GovernmentBill) || _simulationManager.GetPendingBudgetAlternative(PlayerCountryId) != null;
                bool carryEnabled = interactive && !playerBudgetPending && (carries || carryOn);
                Rect carryHit = DrawBudgetCarry(switchRect, switchHit.xMax, carryOn, carryEnabled);
                bool ambientCarry = GUI.enabled;
                GUI.enabled = ambientCarry && carryEnabled;
                if (PoliSimWidgets.Button(carryHit, GUIContent.none, GUIStyle.none)) { if (carryOn) { _taxCarryInputs.Remove(taxLine.Type); } else { _taxCarryInputs[taxLine.Type] = TaxCarryDraft.Of(taxLine); } }   // the carries the line has passed, so the draft is spent once a bill carries it
                GUI.enabled = ambientCarry;
                SlipAnchor(carryHit, id + "/carry");
                int passage = _simulationManager.DraftPassageYear(PlayerCountryId);
                carrySlip = new SlipContent("CARRY OVER · " + (carryOn ? "ON" : "OFF"));
                if (carries)
                {
                    System.DateTime until = _simulationManager.CarbonFigureYieldsOn(passage + 1);
                    carrySlip.Add((carryOn ? "THE BILL CARRIES " : "A CLICK DRAFTS THE CARRY - THE BILL WOULD CARRY ") + TaxRateText(taxLine, carryDraft) + " INTO " + (passage + 1).ToString(CultureInfo.InvariantCulture))
                        .Add((Mathf.Approximately(carryDraft, taxLine.Rate) ? "CARRIED BY A BILL OF " : "PASSED IN ") + passage.ToString(CultureInfo.InvariantCulture) + ", IT STANDS UNTIL " + until.ToString("d MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture)
                            + ", THEN " + CarbonYieldWords(until, passage + 1, caps: true));
                }
                else if (CarbonRateStatute.PassedFigureStands(_playerCountry.Id, taxLine, _simulationManager.LastBoundaryYear))
                {
                    carrySlip.Add("THE PASSED FIGURE IS CARRIED INTO " + taxLine.PassedForYear.ToString(CultureInfo.InvariantCulture) + " ALREADY - A BILL PASSED IN " + passage.ToString(CultureInfo.InvariantCulture) + " CARRIES IT NO FURTHER");
                }
                else { carrySlip.Add("NOTHING TO CARRY - THE RATE IS THE STATUTE'S").Add("A FIGURE THE BILL PASSES, OR ONE PASSED AND STANDING, MAY BE CARRIED INTO THE NEXT YEAR"); }
                if (!interactive) { carrySlip.Add(pending != null ? "THE RATE WAITS FOR THE PROGRAMME BILL" : "NOT LEVIED"); }
                else if (playerBudgetPending) { carrySlip.Add("YOUR BUDGET BILL IS BEFORE " + LegislatureCaps + " AS IT WAS INTRODUCED - A CARRY RIDES THE NEXT ONE"); }
                _budgetSlipBook.Anchors[id + "/carry"] = carrySlip;
            }
            DrawBudgetEndLabels(endLane, BudgetTaxEndText(taxLine, taxLine.MinRate), BudgetTaxEndText(taxLine, taxLine.MaxRate), !levied);
            if (interactive && PoliSim.Testing.CaptureIdentity.Armed && Event.current.type == EventType.Repaint)
            {
                LedgerRow.GeometryByRow[UiGuardContext.CurrentScreen + " / " + name] = (r, track, figureRect, chip);   // P4-1: the tile's rects at rest equal its rects mid-drag
            }

            // the tile's slip: the census the row printed, and what the tile leaves off
            var slip = new SlipContent(name.ToUpperInvariant() + " · " + (levied ? TaxRateText(taxLine, taxLine.Rate) : "NOT LEVIED"));
            if (drafted) { slip.Add("DRAFTED · " + TaxRateText(taxLine, draftRate) + " · WAS " + TaxRateText(taxLine, taxLine.Rate)); }
            if (levied) { slip.Add("RAISES " + UiFormat.Money(revenue, MoneyUnit.Billions) + " A YEAR AT THE STANDING RATE"); }
            string dialRuns = "THE DIAL RUNS " + BudgetTaxEndText(taxLine, taxLine.MinRate) + " – " + BudgetTaxEndText(taxLine, taxLine.MaxRate);
            if (taxLine.IsPerTonne && track.width > 0f)
            {
                // K322 S2 (K320 (3)): the slip at its four lines - the range and the step one line, the passed figure's day one line with what follows it as its
                // marked term's level 2 (the line ran to sixteen words), the carry one line
                float step = LedgerRow.StepFor((taxLine.MaxRate - taxLine.MinRate) / track.width / Mathf.Max(0.0001f, taxLine.DialGrain)) * taxLine.DialGrain;
                slip.Add(dialRuns + " · BY " + step.ToString("0.#", CultureInfo.InvariantCulture) + " " + EnergyLayer.CurrencyCode(_playerCountry.Id) + "/t A STEP");
                if (_simulationManager.TryPassedCarbonFigure(PlayerCountryId, out TaxLine passed, out System.DateTime reverts))   // AF-36 (q36): the date the passed rate reverts
                {
                    slip.Add("PASSED · " + TaxRateText(passed, passed.Rate) + " STANDS UNTIL " + reverts.ToString("d MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture) + " · [[AFTER THAT DAY]]");
                    _budgetSlipBook.Terms["AFTER THAT DAY"] = new SlipContent("AFTER THAT DAY").Add(CarbonYieldWords(reverts, passed.PassedForYear, caps: true));
                }
                if (carrySlip != null && carrySlip.Lines.Count > 0 && _taxCarryInputs.ContainsKey(taxLine.Type)) { slip.Add("CARRY OVER · " + carrySlip.Lines[0]); }
            }
            else { slip.Add(dialRuns); }
            if (taxLine.Type == TaxType.IncomeTax && TaxSchedule.Of(_playerCountry.Id).Kind != TaxScheduleKind.Flat)
            {
                slip.Add(TaxSchedule.KindWord(TaxSchedule.Of(_playerCountry.Id).Kind) + " · AVERAGE EFFECTIVE RATE AT THE MEAN INCOME "
                    + TaxSchedule.AverageEffectiveRateAtMeanIncome(_playerCountry, taxLine, draftRate).ToString("0.0", CultureInfo.InvariantCulture) + "% · THE STATUTE BELOW");
            }
            if (pending != null) { slip.Add("PENDING - A PROGRAMME BILL IS BEFORE " + LegislatureCaps + "; THE RATE WAITS FOR IT"); }
            if (!levied) { slip.Add("NOT LEVIED - THE SWITCH INTRODUCES THE BILL THAT WOULD LEVY IT"); }
            _budgetSlipBook.Anchors[id] = slip;
        }

        /// <summary>The switch: a pill (its knob right when on), and its word - On, Off, Pending with the days, or Locked with the lock. Returns the rect a
        /// click takes (the pill and the word; empty where the role locks it - no control is drawn there).</summary>
        private Rect DrawBudgetSwitch(Rect row, string word, bool on, bool mayIntroduce, bool enabled)
        {
            float pillW = V35.Px(28f), pillH = V35.Px(14f);
            var pill = new Rect(row.x, row.y + Mathf.Round((row.height - pillH) * 0.5f), pillW, pillH);
            GUIStyle face = V35Serif(V35.Floor, enabled && mayIntroduce ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted);
            float wordW = Mathf.Ceil(face.CalcSize(new GUIContent(word)).x) + 2f;
            if (Event.current.type == EventType.Repaint)
            {
                if (mayIntroduce)
                {
                    PoliSimTheme.Pill(pill, V35.SlipEdge);
                    PoliSimTheme.Pill(new Rect(pill.x + 1f, pill.y + 1f, pill.width - 2f, pill.height - 2f), on ? V35.PyramidThreshold : V35.SwitchTrack);
                    float knob = pillH - 4f;
                    PoliSimTheme.Pill(new Rect(on ? pill.xMax - 2f - knob : pill.x + 2f, pill.y + 2f, knob, knob), V35.SlipPaper);
                }
                else { DrawStateGlyph(new Rect(pill.x, pill.y - 1f, pillH + 2f, pillH + 2f), Symbol.Locked, PoliSimTheme.TextMuted); }
                PoliSimWidgets.MeasuredLabel(new Rect(pill.xMax + V35.Px(8f), row.y, wordW, row.height), word, face);
            }
            return new Rect(pill.x, row.y, pillW + V35.Px(8f) + wordW, row.height);
        }

        /// <summary>AF-36 (Elias's ruling q36): the carry at the right of the switch's row - a box (filled while the carry is drafted) and its word, kept
        /// clear of the switch's word ending at <paramref name="leftLimit"/>; the word is cut, never shrunk, where the row is short. Returns the rect a click takes.</summary>
        private Rect DrawBudgetCarry(Rect row, float leftLimit, bool on, bool enabled)
        {
            const string word = "Carry over";
            GUIStyle face = V35Serif(V35.Floor, enabled ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted);
            float box = V35.Px(12f), gap = V35.Px(6f);
            float wordW = Mathf.Ceil(face.CalcSize(new GUIContent(word)).x) + 2f;
            float x = Mathf.Max(leftLimit + V35.Px(12f), row.xMax - box - gap - wordW);
            var hit = new Rect(x, row.y, Mathf.Max(1f, row.xMax - x), row.height);
            if (Event.current.type == EventType.Repaint)
            {
                var b = new Rect(x, row.y + Mathf.Round((row.height - box) * 0.5f), box, box);
                PoliSimTheme.Rule(b, enabled ? V35.SlipEdge : PoliSimTheme.TextMuted);
                PoliSimTheme.Rule(new Rect(b.x + 1f, b.y + 1f, b.width - 2f, b.height - 2f), on ? (enabled ? V35.PyramidThreshold : V35.SwitchTrack) : V35.SlipPaper);
                float wx = b.xMax + gap, room = Mathf.Max(1f, row.xMax - wx);
                PoliSimWidgets.MeasuredLabel(new Rect(wx, row.y, room, row.height), V35Fit(word, face, room, out _), face);
            }
            return hit;
        }

        // =============================================================================================================================================
        // Spending: the lines as tiles (§735)
        // =============================================================================================================================================

        /// <summary>A spending line's icon, by the words of its name as this country prints it (the categories are the USA's; each country names its own).</summary>
        private static string BudgetSpendingIcon(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("health") || n.Contains("medic") || n.Contains("sickness")) { return "cross"; }
            if (n.Contains("educat") || n.Contains("student") || n.Contains("school")) { return "book"; }
            if (n.Contains("defen") || n.Contains("homeland") || n.Contains("security service")) { return "shield"; }
            if (n.Contains("justice") || n.Contains("police") || n.Contains("court")) { return "gavel"; }
            if (n.Contains("pension") || n.Contains("retire") || n.Contains("old age") || n.Contains("social security")) { return "people"; }
            if (n.Contains("famil") || n.Contains("child")) { return "family"; }
            if (n.Contains("unemploy") || n.Contains("income")) { return "shield"; }
            if (n.Contains("hous")) { return "home"; }
            if (n.Contains("transport") || n.Contains("infra")) { return "trade"; }
            if (n.Contains("energy")) { return "bolt"; }
            if (n.Contains("agricult") || n.Contains("food")) { return "wheat"; }
            if (n.Contains("environ") || n.Contains("climate")) { return "drop"; }
            if (n.Contains("culture") || n.Contains("media")) { return "gem"; }
            if (n.Contains("foreign") || n.Contains("aid") || n.Contains("international")) { return "globe"; }
            if (n.Contains("tax")) { return "receipt"; }
            if (n.Contains("financ")) { return "safe"; }
            if (n.Contains("labo") || n.Contains("work")) { return "jobs"; }
            if (n.Contains("science") || n.Contains("research") || n.Contains("space")) { return "atom"; }
            if (n.Contains("regional") || n.Contains("commerce") || n.Contains("business")) { return "office"; }
            if (n.Contains("veteran")) { return "flag"; }
            if (n.Contains("central") || n.Contains("government") || n.Contains("interior")) { return "bank"; }
            return "coins";
        }

        /// <summary>
        /// §735: the spending lines as dial tiles, two to a row, in their two groups - MANDATORY (±15 %, a higher approval cost for its size) and
        /// DISCRETIONARY (±30 %); a group this country has none of draws nothing, as before. The pensions' statutory row and the payment under it are kept as
        /// built in a card after the group that holds the pension line; then the closed year's book as a list card. One control a tile (the slider), the
        /// pension row's own after the tiles; the order follows the country's lines, which do not change within a session.
        /// </summary>
        private void DrawBudgetSpending(float width)
        {
            bool hasMandatory = false, hasDiscretionary = false;
            foreach (SpendingLine line in _playerCountry.SpendingLines) { if (line.IsMandatory) { hasMandatory = true; } else { hasDiscretionary = true; } }
            _budgetSlipBook.Anchors["budget:mandatory"] = new SlipContent("MANDATORY LINES")
                .Add("THIS YEAR'S ALLOWED CHANGE ±" + SimulationManager.MandatoryPercentChangeRangeForRules.ToString("0", CultureInfo.InvariantCulture) + " % OF THE LINE · A HIGHER APPROVAL COST FOR ITS SIZE THAN A DISCRETIONARY CHANGE")
                .Add("THE DRAFT IS A FIGURE; THE BILL CARRIES IT AS A NOMINAL TARGET");
            _budgetSlipBook.Anchors["budget:discretionary"] = new SlipContent("DISCRETIONARY LINES")
                .Add("THIS YEAR'S ALLOWED CHANGE ±" + SimulationManager.DiscretionaryPercentChangeRangeForRules.ToString("0", CultureInfo.InvariantCulture) + " % OF THE LINE")
                .Add("THE DRAFT IS A FIGURE; THE BILL CARRIES IT AS A NOMINAL TARGET");
            BudgetBandSlips("Mandatory line");   // K322 S2: the tiles' band names at rest open these
            BudgetBandSlips("Discretionary line");
            BudgetBandSlips("Pension age");
            if (hasMandatory) { DrawBudgetSpendingGroup(width, true); }
            if (hasDiscretionary) { DrawBudgetSpendingGroup(width, false); }
            DrawBudgetLastYearBook(width);
        }

        private void DrawBudgetSpendingGroup(float width, bool mandatory)
        {
            DrawBudgetSectionHead(mandatory ? "Mandatory lines" : "Discretionary lines", mandatory ? "budget:mandatory" : "budget:discretionary", width);
            var lines = new List<SpendingLine>();
            foreach (SpendingLine line in _playerCountry.SpendingLines) { if (line.IsMandatory == mandatory) { lines.Add(line); } }
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(V35Span(width, 12), 6);
            float rowHeight = BudgetDialTileHeight(false);
            bool pension = false;
            for (int i = 0; i < lines.Count; i += 2)
            {
                Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
                for (int k = 0; k < 2 && i + k < lines.Count; k++)
                {
                    DrawBudgetSpendingTile(new Rect(row.x + k * (tileWidth + gutter), row.y, tileWidth, rowHeight), lines[i + k], mandatory ? SimulationManager.MandatoryPercentChangeRangeForRules : SimulationManager.DiscretionaryPercentChangeRangeForRules);
                    if (lines[i + k].Category == SpendingCategory.SocialSecurity) { pension = true; }
                }
                GUILayout.Space(gutter);
            }
            if (pension && PensionAgeStatute.Has(_playerCountry.Id))
            {
                // board 15c / PN-1 / PN-2: the pension line's statutory age (the law's path on the track, a lever since §590) and what the line pays - kept as built
                DrawBudgetSectionHead("Pensions · the statutory age and the payment", "budget:pensions", width);
                _budgetSlipBook.Anchors["budget:pensions"] = new SlipContent("PENSIONS")
                    .Add("THE STATUTORY AGE - THE LAW'S PATH ON THE TRACK, THE KNOB THE DRAFT, RIDING THE BUDGET BILL; WHAT THE PENSION LINE PAYS AT IT")
                    .Add("KEPT AS BUILT - THE COMPOSITION'S BUDGET DRAWS NEITHER; ASKED");
                bool guarded = V35.FloorGuarded;
                V35.FloorGuarded = false;
                GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
                DrawPensionAgeRow();
                DrawPensionPaymentRow();
                GUILayout.EndVertical();
                V35.FloorGuarded = guarded;
                GUILayout.Space(gutter);
            }
        }

        /// <summary>
        /// One spending line as a tile: the icon, the figure (the draft's, in the draft's ink, while it differs), the name, the chip (its share of GDP), the
        /// track - this year's allowed change around the standing figure, the year-open amount its third tick (9b) - and its two ends; while the draft is moving
        /// the line's range caption speaks in the ends' lane (P4-B2), the ends yielding it. The census the row printed - the portfolio and its effectiveness, the
        /// driver, next year's figure, the change since the year opened, the step - is the slip's. A draft is stored as the figure; back on the standing figure
        /// it is dropped. One control: the slider.
        /// </summary>
        private void DrawBudgetSpendingTile(Rect r, SpendingLine line, float rangePercent)
        {
            string name = DisplayName.Of(line.Category.ToString());
            string id = "spend:" + line.Category;
            float standing = line.Amount;
            // P5-B5 / SC-1: the track is this year's allowed change around the standing figure - on the line's own path where a dial cost stands outside it
            // AF-9 (the review's L1): the manager's own range, the one IntroduceBudgetBill refuses outside - no copy of it here
            (float min, float max) = SimulationManager.YearlyRangeOf(_playerCountry, line);
            // AF-9 (the second review's N6): the draft clamped to that range as the bill clamps it (BuildBudgetBillFromDrafts), and written back below, so the
            // thumb and the DRAFTED figure show what the bill carries - a draft left from an earlier year included
            float draft = Mathf.Clamp(GetSpendingLineInput(line.Category, standing), min, max);
            bool drafted = !Mathf.Approximately(draft, standing);
            float dialCost = SimulationManager.DialCostOf(_playerCountry, line);
            float grain = SpendingGrain(min, max);   // BR-1
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal);
            float gdp = _playerCountry.State.NominalGdp;
            string share = gdp > 0f ? UiFormat.Number(standing / gdp * 100f, 1) + "% of GDP" : null;

            Rect inner = DrawBudgetTileCard(r, drafted, false);
            Rect chip = DrawBudgetChip(inner, share, drafted);
            Rect figureRect = DrawBudgetTileHead(inner, BudgetSpendingIcon(name), area, UiFormat.Money(drafted ? draft : standing, MoneyUnit.Billions),
                drafted ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, name, PoliSimTheme.TextPrimary, chip);
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, figureRect.yMax - inner.y + V35.Px(20f)), id);

            Rect track = BudgetTrackRect(inner, out Rect endLane);
            float ghost = line.LastDriverRatio > 0f ? line.LastYearAmount : float.NaN;   // 9b: the year-open tick (SC-1: "a year has run" is the index's mark)
            float result = LedgerRow.Track(track, name, standing, draft, min, max, true, _sliderStyle, _sliderThumbStyle, LedgerRow.ScaleOf(_labelStyle), grain, 0f, ghost);
            if (Event.current.type == EventType.Repaint)
            {
                // Two literal keys so RangeCaptionCheck's enumeration of the drawn dials reads them off the controller's partials.
                // K322 S2 (K320, S1's form): the band's NAME alone at rest, its line on the band's slip (DrawBudgetSpending holds every band's)
                bool speaking = line.IsMandatory
                    ? DrawRangeCaption("Mandatory line", line.Category.ToString(), result, standing, min, max, endLane, nameOnly: true, bandKeyed: true)
                    : DrawRangeCaption("Discretionary line", line.Category.ToString(), result, standing, min, max, endLane, nameOnly: true, bandKeyed: true);
                if (!speaking) { DrawBudgetEndLabels(endLane, UiFormat.Money(min, MoneyUnit.Billions), UiFormat.Money(max, MoneyUnit.Billions), false); }
            }
            if (Mathf.Approximately(result, standing)) { _spendingLineInputs.Remove(line.Category); }
            else { _spendingLineInputs[line.Category] = result; }
            if (PoliSim.Testing.CaptureIdentity.Armed && Event.current.type == EventType.Repaint)
            {
                LedgerRow.GeometryByRow[UiGuardContext.CurrentScreen + " / " + name] = (r, track, figureRect, chip);   // P4-1: the tile's rects at rest equal its rects mid-drag
            }

            // the tile's slip: the census the row printed
            var slip = new SlipContent(name.ToUpperInvariant() + " · " + UiFormat.Money(standing, MoneyUnit.Billions));
            if (drafted) { slip.Add("DRAFTED · " + UiFormat.Money(draft, MoneyUnit.Billions) + " · WAS " + UiFormat.Money(standing, MoneyUnit.Billions)); }
            if (share != null) { slip.Add(share.ToUpperInvariant()); }
            slip.Add(SpendingRowCaption(line, rangePercent, out _));
            slip.Add("THIS YEAR'S ALLOWED CHANGE ±" + rangePercent.ToString("0", CultureInfo.InvariantCulture) + " % · " + UiFormat.Money(min, MoneyUnit.Billions) + " – " + UiFormat.Money(max, MoneyUnit.Billions)
                + (dialCost != 0f ? " · ON THE LINE'S OWN PATH, ITS DIAL COST ON TOP" : string.Empty));
            SpendingDriver of = SpendingDrivers.Of(line.Category);
            slip.Add((line.Pinned ? "PINNED" : of == SpendingDriver.None ? "NO DRIVER" : "DRIVEN BY " + SpendingDrivers.Short(of, _playerCountry))
                + " · NEXT YEAR " + UiFormat.Money(SimulationManager.ProjectLineNextYear(_playerCountry, line, _playerCountry.State.Inflation), MoneyUnit.Billions));
            string delta = SpendingDeltaText(line, drafted ? draft : standing);
            if (delta != null) { slip.Add(delta + " SINCE THE YEAR OPENED · ITS TICK ON THE TRACK"); }
            if (track.width > 0f)
            {
                float step = LedgerRow.StepFor((max - min) / track.width / Mathf.Max(0.0001f, grain)) * grain;
                if (step > 1f) { slip.Add("BY " + UiFormat.Money(step, MoneyUnit.Billions) + " A STEP"); }
            }
            _budgetSlipBook.Anchors[id] = slip;
        }

        /// <summary>
        /// §735: the range caption (P4-B2) in a v3.5 tile's lane - the draft's band from the catalog (P4-B1) at the presenter's alpha (held, then fading), at
        /// the floor: the band's NAME in bold, a middle dot, its line; the name drops where both do not fit, and where the line alone does not fit the band
        /// stays empty and the guard is told. The tile's ends yield the lane while it speaks. Returns whether it was painted.
        /// <para>S1 (K320): <paramref name="nameOnly"/> - the band's NAME alone at rest, centred in the lane, the anchor of the band's slip ("dial:" + the
        /// dial's name + ":band", <see cref="RangeCaptions.BandSlip"/>), where its line is; a page opts in.</para>
        /// <para>K322 S2: <paramref name="bandKeyed"/> - the name's anchor is the band's own (<see cref="BudgetBandAnchor"/>), for a page whose tiles share
        /// one dial and so may stand in different bands at once; the page holds every band's slip under those keys (<see cref="BudgetBandSlips"/>).</para>
        /// </summary>
        private bool DrawRangeCaption(string name, string captionKey, float draft, float standing, float min, float max, Rect lane, bool nameOnly = false, bool bandKeyed = false)
        {
            if (!RangeCaptions.TryGet(name, out RangeCaptions.Dial dial)) { return false; }
            int band = RangeCaptions.BandIndex(draft, min, max);
            float alpha = RangeCaptionPresenter.Alpha(captionKey, band, !Mathf.Approximately(draft, standing));
            if (alpha <= 0f) { return false; }
            RangeCaptions.Band b = dial.Bands[band];
            Color ink = PoliSimTheme.TextPrimary;
            ink.a *= alpha;
            GUIStyle line = V35Serif(V35.Floor, ink, TextAnchor.MiddleLeft);
            GUIStyle nameFace = V35Serif(V35.Floor, ink, TextAnchor.MiddleLeft);
            nameFace.fontStyle = FontStyle.Bold;
            if (nameOnly)
            {
                string word = b.Name.ToUpperInvariant();
                float wordWidth = Mathf.Min(lane.width, Mathf.Ceil(nameFace.CalcSize(new GUIContent(word)).x));
                var at = new Rect(lane.x + Mathf.Round((lane.width - wordWidth) * 0.5f), lane.y, wordWidth, lane.height);
                PoliSimWidgets.MeasuredLabel(at, word, nameFace);
                SlipAnchor(at, bandKeyed ? BudgetBandAnchor(name, band) : "dial:" + name + ":band");
                return true;
            }
            string nameText = b.Name.ToUpperInvariant() + " · ";
            float nameWidth = Mathf.Ceil(nameFace.CalcSize(new GUIContent(nameText)).x), lineWidth = Mathf.Ceil(line.CalcSize(new GUIContent(b.Line)).x);
            bool withName = nameWidth + lineWidth <= lane.width;
            if (!withName && lineWidth > lane.width)
            {
                UiOverflowGuard.Check(b.Line, new Vector2(lineWidth, lane.height), new Vector2(lane.width, lane.height), line.fontSize);
                return false;
            }
            float total = (withName ? nameWidth : 0f) + lineWidth;
            float x = lane.x + Mathf.Round((lane.width - total) * 0.5f);
            if (withName) { PoliSimWidgets.MeasuredLabel(new Rect(x, lane.y, nameWidth, lane.height), nameText, nameFace); x += nameWidth; }
            PoliSimWidgets.MeasuredLabel(new Rect(x, lane.y, lineWidth, lane.height), b.Line, line);
            return true;
        }

        /// <summary>K322 S2: a band's own anchor on this page - "dial:" + the dial's name + ":band:" + the band's index.</summary>
        private static string BudgetBandAnchor(string dial, int band) => "dial:" + dial + ":band:" + band.ToString(CultureInfo.InvariantCulture);

        /// <summary>K322 S2 (K320 (5)): every band's slip of a dial the page draws, under the band's own anchor - the band's name at rest opens its own; the line
        /// the caption spoke (it was a sentence at rest) is on it, so the page's book holds every line its captions can drop, whichever band a draft stands in.</summary>
        private void BudgetBandSlips(string dialName)
        {
            if (!RangeCaptions.TryGet(dialName, out RangeCaptions.Dial dial)) { return; }
            for (int b = 0; b < dial.Bands.Length; b++) { _budgetSlipBook.Anchors[BudgetBandAnchor(dialName, b)] = RangeCaptions.BandSlip(dial, b); }
        }

        /// <summary>
        /// K322 S2 (K320): a ledger row's range caption as the band's NAME alone (the pension age's row, kept as built) - in the row's caption band at the caption
        /// face (the ledger caption's 8d size, bold), centred, at the presenter's alpha; the anchor of the band's slip (<see cref="BudgetBandSlips"/>), where its
        /// line is. Replaces the name-and-line caption the controller's shared ledger dial draws, on this one row. Returns whether it was painted (a Repaint's).
        /// </summary>
        private bool DrawLedgerBandName(string name, string captionKey, float draft, float standing, float min, float max)
        {
            if (!RangeCaptions.TryGet(name, out RangeCaptions.Dial dial)) { return false; }
            int band = RangeCaptions.BandIndex(draft, min, max);
            float alpha = RangeCaptionPresenter.Alpha(captionKey, band, !Mathf.Approximately(draft, standing));
            if (alpha <= 0f) { return false; }
            Rect area = LedgerRow.LastCaptionBand;
            if (area.width <= 8f) { return false; }
            int size = Mathf.Max(8, Mathf.RoundToInt(RangeCaptionFontAt1280 * _labelStyle.fontSize / 14f));
            GUIStyle face = new GUIStyle(LedgerRow.CaptionStyle(_labelStyle)) { fontSize = size, alignment = TextAnchor.LowerLeft, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            Color ink = PoliSimTheme.TextPrimary;
            ink.a *= alpha;
            string word = dial.Bands[band].Name.ToUpperInvariant();
            float w = Mathf.Min(area.width, Mathf.Ceil(face.CalcSize(new GUIContent(word)).x) + 2f);
            var at = new Rect(Mathf.Round(area.x + (area.width - w) * 0.5f), area.y, w, area.height);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(at, word, Inked(face, ink));
            GUI.color = previous;
            SlipAnchor(at, BudgetBandAnchor(name, band));
            return true;
        }

        /// <summary>AF-17 (the review's L5): CONVENTION - the most one-off rows the closed year's book draws before the last becomes a row for the rest.</summary>
        private const int MaxBookOneOffRows = 4;

        /// <summary>§735: the closed year's book (§564's) as a list card - each line of the fiscal report and its figure, the recorded balance last in the fiscal
        /// family's neutral ink, the warning only past the country's rule (§725); each line's caption its slip.</summary>
        private void DrawBudgetLastYearBook(float width)
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            DrawBudgetSectionHead("Last year · the book as closed", "budget:book", width);
            _budgetSlipBook.Anchors["budget:book"] = new SlipContent("LAST YEAR · THE BOOK AS CLOSED").Add("THE FISCAL REPORT'S LINES, UNCHANGED; THE BALANCE IS THE RECORDED ONE, NEVER A HAND SUM");
            var rows = new List<(string Name, string Figure, string Caption, Color Ink)>();
            int foldedRow = -1;   // AF-17 (the review's N2): the "N more one-offs" row, whose slip itemises every line it folds
            var folded = new List<string>();
            if (report == null) { rows.Add(("No year closed", "—", "AT THE YEAR'S END", PoliSimTheme.TextMuted)); }
            else
            {
                Color neutral = PoliSimTheme.TextPrimary;
                rows.Add(("Revenue", UiFormat.Money(report.Revenue, MoneyUnit.Billions), "TAX, TARIFFS, FUND", neutral));
                rows.Add(("Baseline", UiFormat.Money(report.BaselineGovernmentSpending, MoneyUnit.Billions), "SPENDING", neutral));
                rows.Add(("Discretionary", UiFormat.MoneyDelta(report.DiscretionarySpending, MoneyUnit.Billions), "CHANGE THIS YEAR", neutral));
                rows.Add(("Mandatory", UiFormat.Money(report.MandatorySpending, MoneyUnit.Billions), "SPENDING", neutral));
                rows.Add(("Unemployment", UiFormat.Money(report.UnemploymentBenefitCost, MoneyUnit.Billions), "BENEFITS", neutral));
                rows.Add(("Interest", UiFormat.Money(report.InterestOnDebt, MoneyUnit.Billions), "ON DEBT, AUTOMATIC", neutral));
                rows.Add(("Welfare", UiFormat.Money(report.WelfareCost, MoneyUnit.Billions), "PROGRAMMES", neutral));
                if (report.DebtLimitCut != 0f) { rows.Add(("Debt-limit cut", UiFormat.MoneyDelta(-report.DebtLimitCut, MoneyUnit.Billions), "OUTLAYS CUT TO REVENUE", neutral)); }   // PS-6 US-24 (the review's M4): the cut's own line, so the lines add to the year's spending
                if (report.CrisisCut != 0f) { rows.Add(("Debt-crisis cut", UiFormat.MoneyDelta(-report.CrisisCut, MoneyUnit.Billions), "NO BORROWING: OUTLAYS CUT TO REVENUE", neutral)); }   // MF q33 (the q32 review's M3): the closed market's cut, its own line
                rows.Add(("Tariffs", UiFormat.Money(report.TariffRevenue, MoneyUnit.Billions), "AT THE STATED RATES", neutral));
                if (report.ElectricityTaxRevenue != 0f) { rows.Add(("Electricity tax", UiFormat.MoneyDelta(report.ElectricityTaxRevenue, MoneyUnit.Billions), "VS THE 2023 STATUTE", neutral)); }   // EN-7b
                // AF-17 (q17): the year's one-offs itemised, each its own named line - they count in the balance below, so the debt's change is the deficit
                if (report.OneOffLines != null)
                {
                    // the review's L5: at most MaxBookOneOffRows rows - past it, the first ones and a row for the rest with their net sum; a long name fits as its siblings' do (V35Fit), whole on its slip
                    int shown = report.OneOffLines.Count <= MaxBookOneOffRows ? report.OneOffLines.Count : MaxBookOneOffRows - 1;
                    for (int k = 0; k < shown; k++)
                    {
                        FiscalOneOff oneOff = report.OneOffLines[k];
                        string what = string.IsNullOrEmpty(oneOff.Source) ? oneOff.Label : oneOff.Source;
                        string caption = (string.IsNullOrEmpty(oneOff.Source) ? string.Empty : oneOff.Label.ToUpperInvariant() + " · ")
                            + "ONE-OFF · " + oneOff.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · COUNTED IN THE YEAR'S BALANCE";
                        rows.Add((what, UiFormat.MoneyDelta(oneOff.Amount, MoneyUnit.Billions), caption, neutral));
                    }
                    if (shown < report.OneOffLines.Count)
                    {
                        float rest = 0f;
                        for (int k = shown; k < report.OneOffLines.Count; k++)
                        {
                            FiscalOneOff oneOff = report.OneOffLines[k];
                            rest += oneOff.Amount;
                            folded.Add((string.IsNullOrEmpty(oneOff.Source) ? oneOff.Label : oneOff.Source + " · " + oneOff.Label).ToUpperInvariant() + " · "
                                + oneOff.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · " + UiFormat.MoneyDelta(oneOff.Amount, MoneyUnit.Billions));
                        }
                        int more = report.OneOffLines.Count - shown;
                        foldedRow = rows.Count;
                        rows.Add((more + " more one-offs", UiFormat.MoneyDelta(rest, MoneyUnit.Billions), "THEIR NET SUM · EACH COUNTED IN THE YEAR'S BALANCE", neutral));
                    }
                }
                float? deficit = DerivedStats.DeficitPercentOfGdp(_playerCountry, report);
                string rule = null;
                Color balanceInk = deficit.HasValue ? V35.FiscalInk(PlayerCountryId, FiscalRules.Measure.Deficit, deficit.Value, neutral, out rule) : neutral;
                rows.Add(("Balance", UiFormat.MoneyDelta(report.BudgetBalance, MoneyUnit.Billions), rule ?? "AS RECORDED", balanceInk));
            }
            float rowH = V35.Px(V35.ListRow);
            float height = V35.Px(V35.CardPadY) * 2f + rowH * rows.Count;
            Rect card = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            Rect inner = DrawV35Card(card);
            var anchored = new HashSet<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new Rect(inner.x, inner.y + i * rowH, inner.width, rowH);
                DrawV35ListRow(row, null, 0f, rows[i].Name, rows[i].Figure, rows[i].Ink);
                string rid = "budget:book:" + rows[i].Name;
                if (!anchored.Add(rid)) { rid += "#" + i; }   // AF-17: two one-offs of one name keep a slip each
                SlipAnchor(row, rid);
                var slip = new SlipContent(rows[i].Name.ToUpperInvariant() + " · " + rows[i].Figure).Add(rows[i].Caption.ToUpperInvariant());
                if (i == foldedRow) { foreach (string line in folded) { slip.Add(line); } }   // N2: every folded one-off named, dated and its figure - the book itemises them all
                _budgetSlipBook.Anchors[rid] = slip;
            }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        // =============================================================================================================================================
        // The kept categories, the kept readings, the action
        // =============================================================================================================================================

        /// <summary>Welfare, infrastructure and the fund, drawn as built under their tab, the floor's guard lifted (kept, asked).</summary>
        private void DrawBudgetKeptCategory(float width)
        {
            bool guarded = V35.FloorGuarded;
            V35.FloorGuarded = false;
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
            switch (_budgetProcessCategory)
            {
                case BudgetProcessCategory.Welfare: DrawWelfarePolicyContent(); break;
                case BudgetProcessCategory.Infrastructure: DrawInfrastructureContent(); break;
                case BudgetProcessCategory.Swf: DrawSwfPolicyContent(); break;
            }
            GUILayout.EndVertical();
            V35.FloorGuarded = guarded;
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>The policy screen's stat chips for this tab's area and the trace a chip opens - kept as built under the dials, asked.</summary>
        private void DrawBudgetKeptReadings(float width)
        {
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_budgetProcessCategory);
            float inner = width - V35.Px(V35.CardPadX) * 2f - 4f;
            float statHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, inner, country: _playerCountry);
            if (statHeight <= 0f) { return; }
            DrawBudgetSectionHead("Readings these dials reach", "budget:readings", width);
            _budgetSlipBook.Anchors["budget:readings"] = new SlipContent("READINGS THESE DIALS REACH")
                .Add("THE POLICY WEB'S EDGES FROM THIS TAB'S AREA - A CHIP OPENS ITS FORMULA'S TERMS")
                .Add("KEPT AS BUILT - THE COMPOSITION'S BUDGET DRAWS NONE OF THEM; ASKED");
            bool guarded = V35.FloorGuarded;
            V35.FloorGuarded = false;
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, inner);
            float gapStance = _simulationManager.GetWageGrowthGapAtPeriodOpen(PlayerCountryId);
            float host = V35.Px(320f);
            StatTracePanel.MeasureHeight(_playerCountry, gapStance, _labelStyle, inner, host);
            StatTracePanel.Draw(_playerCountry, gapStance, _labelStyle, _labelStyle, inner, host);
            GUILayout.EndVertical();
            V35.FloorGuarded = guarded;
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>
        /// The call to action under the dials: *Introduce budget bill* with the count of changes (the composition's) where the player may introduce it;
        /// the opposition's alternative, its lock or its count of days otherwise (PS-3e, the model's - the composition draws no opposition action, asked);
        /// and the bill's status under it.
        /// </summary>
        private void DrawBudgetAction(float width, BudgetBill draft, int changes)
        {
            BudgetBill pendingBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            bool open = _simulationManager.GetPendingBudgetProcess(PlayerCountryId);
            GUILayout.Space(V35.Px(4f));
            float h = V35.Px(34f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            if (_simulationManager.PlayerMayIntroduce(PlayerCountryId, out _))
            {
                bool enabled = pendingBill == null && open;
                if (DrawBudgetButton(row, "Introduce budget bill", changes > 0 ? changes + (changes == 1 ? " change" : " changes") : null, enabled))
                {
                    _simulationManager.IntroduceBudgetBill(PlayerCountryId, BuildBudgetBillFromDrafts());
                }
                SlipAnchor(row, "budget:introduce");
                _budgetSlipBook.Anchors["budget:introduce"] = new SlipContent("INTRODUCE BUDGET BILL")
                    .Add(changes > 0 ? "ONE BILL FOR EVERY DIAL DRAFTED ON EVERY TAB · " + changes + (changes == 1 ? " CHANGE" : " CHANGES") : "MOVE A DIAL TO DRAFT · AN UNCHANGED BILL RESTATES THE STANDING BUDGET")
                    .Add(enabled ? "IT GOES TO " + LegislatureCaps + " ON THE CLICK" : pendingBill != null ? "A BUDGET BILL IS BEFORE " + LegislatureCaps + " ALREADY" : "THE BUDGET PROCESS IS NOT OPEN");
            }
            else
            {
                BudgetBill tabled = _simulationManager.GetPendingBudgetAlternative(PlayerCountryId);
                string line = null, lineAtRest = null;   // K322 S2: the reason's short label at rest beside the lock; the reason itself is the slip's
                // PS-6 US-22: the opposition's verb on a continuing resolution before Congress (Ask 2's reading (P), standing - I13 (2)); US-22b (I13 (3)): or due,
                // since the one decided the day before the horizon is withheld in advance
                bool withholdVerb = UsBudgetProcedure.Applies(_playerCountry) && (_playerCountry.ContinuingResolutionWithheldBy == _playerCountry.PlayerPartyAbbrev   // US-22b (the review's M2): the release is offered whenever the player's party withholds, whatever is before Congress
                                    || ((pendingBill == null || pendingBill.ContinuingResolution) && UsBudgetProcedure.MayWithhold(_playerCountry, pendingBill, _simulationManager.CurrentDate, out _)));
                if (withholdVerb) { }
                else if (pendingBill == null || !pendingBill.GovernmentBill) { _simulationManager.PlayerMayIntroduce(PlayerCountryId, Congress.Kind.Budget, null, out line); lineAtRest = "Locked"; }   // PS-6 US-19: in a Congress the budget's own reason - the president proposes it
                else if (tabled != null) { line = "Your alternative is tabled · the chamber decides in " + pendingBill.DaysRemaining + " day(s)"; lineAtRest = "Alternative tabled · " + BudgetDays(pendingBill.DaysRemaining); }
                else if (PoliSim.Elections.WorldClock.CongressActs(PlayerCountryId)) { line = "Congress votes the president's budget as its acts - no alternative budget is weighed"; lineAtRest = "No alternative weighed"; }   // PS-6 US-22: Congress votes the president's budget as its acts, weighing no alternative
                else if (!PoliSim.Elections.WorldClock.WeighsAlternative(PlayerCountryId)) { line = "This country's budget procedure is not yet modelled - the government's bill is voted alone"; lineAtRest = "Government bill voted alone"; }
                if (withholdVerb)
                {
                    // PS-6 US-22: withhold or release - the party stands against the resolution in both chambers while withheld (PROVISIONAL reading, asked)
                    bool withheld = _playerCountry.ContinuingResolutionWithheldBy == _playerCountry.PlayerPartyAbbrev;
                    if (DrawBudgetButton(row, withheld ? "Release the resolution" : "Withhold the resolution", null, true))   // K322 S2: three words at rest; the slip's head names the continuing resolution
                    {
                        if (!_simulationManager.WithholdContinuingResolution(PlayerCountryId, !withheld, out string refused)) { Debug.Log($"BUDGET: the continuing resolution's verb was refused - {refused}"); }
                    }
                    SlipAnchor(row, "budget:introduce");
                    string decides = pendingBill != null && pendingBill.ContinuingResolution ? "CONGRESS DECIDES IN " + pendingBill.DaysRemaining + " DAY(S)"
                        : _playerCountry.LapseSince != System.DateTime.MinValue ? "THE NEXT IS TABLED AFTER THE PRESIDENT'S REQUEST IS DECIDED"
                        : "THE NEXT IS DECIDED ON " + UsBudgetProcedure.HorizonEve(_playerCountry.AppropriatedThrough).ToString("d MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " IF NO APPROPRIATION IS LAW BY THEN";
                    _budgetSlipBook.Anchors["budget:introduce"] = new SlipContent(withheld ? "RELEASE THE CONTINUING RESOLUTION" : "WITHHOLD THE CONTINUING RESOLUTION")
                        .Add(withheld ? "YOUR PARTY VOTES AGAINST IT IN BOTH CHAMBERS · RELEASED, ONE IS ASKED NOTHING AND PASSES UNCONTESTED THE NEXT DAY"
                            : "YOUR PARTY WOULD VOTE AGAINST IT IN BOTH CHAMBERS AND EVERY OTHER PARTY FOR IT")
                        .Add(decides)
                        .Add("A RESOLUTION THAT FALLS FUNDS NOTHING - FROM 1 OCTOBER UNFUNDED, THE GOVERNMENT SHUTS DOWN, AND IT BEARS EACH ONE THAT FALLS")
                        .Add("IN A SHUTDOWN YOUR OWN SENATORS MAY CROSS AT CLOTURE - DRAWN FROM THE CLOTURE VOTES OF EVERY LAPSE OF RECORD (2013, 2018, 2018-19, 2025), " + PoliSim.Elections.UsLapseDraw.FittedLabel)   // PS-6 US-22c (J7 (iii)): the list widened past 2025 (`UsLapseDraw.Record`)
                        .Add("NO OTHER PARTY WITHHOLDS ONE");
                }
                else if (line != null)
                {
                    // K322 S2 (K320): the lock and a label of at most four words at rest - the reason, a sentence, is the slip's (it was cut with an ellipsis at rest)
                    GUIStyle lockFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                    var lockRow = new Rect(row.x, row.y, Mathf.Min(row.width, V35.Px(24f) + Mathf.Ceil(lockFace.CalcSize(new GUIContent(lineAtRest)).x) + 2f), row.height);
                    if (Event.current.type == EventType.Repaint)
                    {
                        DrawStateGlyph(new Rect(row.x, row.y + Mathf.Round((row.height - V35.Px(16f)) * 0.5f), V35.Px(16f), V35.Px(16f)), Symbol.Locked, PoliSimTheme.TextSecondary);
                        PoliSimWidgets.MeasuredLabel(new Rect(row.x + V35.Px(24f), row.y, Mathf.Max(1f, lockRow.width - V35.Px(24f)), row.height), lineAtRest, lockFace);
                    }
                    SlipAnchor(lockRow, "budget:introduce");
                    _budgetSlipBook.Anchors["budget:introduce"] = BudgetPackedSlip("THE BUDGET IS THE GOVERNMENT'S", line, _budgetSlipBook);
                }
                else
                {
                    if (DrawBudgetButton(row, "Table alternative budget", changes > 0 ? changes + (changes == 1 ? " change" : " changes") : null, true))   // K322 S2: three words at rest; the slip's head is the whole verb
                    {
                        if (!_simulationManager.TableShadowBudget(PlayerCountryId, BuildBudgetBillFromDrafts(), out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: the alternative was refused - {refused}"); }
                    }
                    SlipAnchor(row, "budget:introduce");
                    _budgetSlipBook.Anchors["budget:introduce"] = new SlipContent("TABLE AN ALTERNATIVE BUDGET")
                        .Add("YOUR DRAFT, SET AGAINST THE GOVERNMENT'S BUDGET BEFORE THE CHAMBER")
                        .Add("KEPT - THE COMPOSITION DRAWS NO OPPOSITION ACTION; ASKED");
                }
            }
            // the bill's status, under the action - K322 S2 (K320): a one-line caption at rest, the status's sentences (BuildBudgetBillStatusText, as the
            // Docket's body still prints them) its slip, word for word
            string caption = BudgetStatusCaption();
            GUIStyle statusFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
            Vector2 captionSize = statusFace.CalcSize(new GUIContent(caption));
            float sh = Mathf.Ceil(captionSize.y) + V35.Px(4f);
            Rect statusRect = GUILayoutUtility.GetRect(width, sh, GUILayout.Width(width), GUILayout.Height(sh));
            var captionRect = new Rect(statusRect.x, statusRect.y, Mathf.Min(statusRect.width, Mathf.Ceil(captionSize.x) + 2f), statusRect.height);
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(captionRect, caption, statusFace); }
            SlipAnchor(captionRect, "budget:status");
            _budgetSlipBook.Anchors["budget:status"] = BudgetPackedSlip("BILL STATUS", BuildBudgetBillStatusText(), _budgetSlipBook);
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>
        /// K322 S2 (K320): the bill's status as the page shows it at rest - a caption of at most eight counted words, one line, no sentence: what is before the
        /// chamber and when it decides, or which window is open. Its branches are <see cref="BudgetBillStatusCore"/>'s, and that sentence (with a refusal of
        /// the day ahead of it) is the caption's slip, word for word.
        /// </summary>
        private string BudgetStatusCaption()
        {
            BudgetBill pendingBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            string state;
            if (UsBudgetProcedure.Applies(_playerCountry)) { state = UsBudgetStatusCaption(pendingBill, _simulationManager.GetPendingMajorityReconciliation(PlayerCountryId) != null); }
            else if (pendingBill != null && pendingBill.GovernmentBill) { state = "Government budget before the chamber · " + BudgetDays(pendingBill.DaysRemaining); }
            else if (pendingBill != null) { state = pendingBill.Docket != null ? "Budget bill on the President's desk" : BudgetRestJoin("Budget bill before " + Legislature, BudgetDays(pendingBill.DaysRemaining)); }
            else if (_simulationManager.IsIncomingGovernmentBudgetWindow(PlayerCountryId)) { state = "Incoming government · first budget open"; }
            else if (!_simulationManager.PlayerMayIntroduce(PlayerCountryId, out _)) { state = Congress.Applies(_playerCountry) ? "The president proposes the budget" : "The government lays the budget"; }
            else if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId)) { state = "Budget process open"; }
            else { state = "No budget bill before " + Legislature; }
            bool refusedToday = _budgetRefusal != null && _simulationManager.CurrentDate.Date == _budgetRefusalOn;
            return refusedToday ? BudgetRestJoin("Refused today", state) : state;
        }

        /// <summary>K322 S2: <see cref="UsBudgetStatusText"/>'s branches as a caption - what is before Congress, then the funding (its horizon, or the shutdown's
        /// day) where both fit the cap.</summary>
        private string UsBudgetStatusCaption(BudgetBill pendingBill, bool majorityPending)
        {
            System.DateTime today = _simulationManager.CurrentDate;
            string funding = _playerCountry.LapseSince != System.DateTime.MinValue ? "shut down, day " + UsBudgetProcedure.LapseDays(_playerCountry.LapseSince, today).ToString(CultureInfo.InvariantCulture)
                : _playerCountry.AppropriatedThrough != System.DateTime.MinValue ? "funded to " + _playerCountry.AppropriatedThrough.ToString("d MMM yyyy", CultureInfo.InvariantCulture) : null;
            string state;
            if (DeskOf(pendingBill) != null) { state = "On the President's desk"; }
            else if (pendingBill != null && pendingBill.ContinuingResolution) { state = BudgetRestJoin("Continuing resolution", BudgetDays(pendingBill.DaysRemaining), _playerCountry.ContinuingResolutionWithheldBy != null ? "withheld" : null); }
            else if (pendingBill != null) { state = BudgetRestJoin(pendingBill.GovernmentBill ? "President's budget before Congress" : "Your budget before Congress", BudgetDays(pendingBill.DaysRemaining)); }
            else if (_simulationManager.IsIncomingGovernmentBudgetWindow(PlayerCountryId)) { state = "First budget open"; }
            else if (!_simulationManager.PlayerMayIntroduce(PlayerCountryId, out _)) { state = "The president proposes the budget"; }
            else if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId)) { state = "Budget process open"; }
            else { state = majorityPending ? "President's budget not before Congress" : "No bill before Congress"; }
            return BudgetRestJoin(state, funding);
        }

        /// <summary>
        /// PS-6 K22, K23 and K75 (Elias's rulings of 2026-10-23, `docs/RULINGS_2026-10-23.md`): THE US CONGRESS'S VERBS under the budget's action, drawn only where
        /// the game's Congress votes. A player-president's own party may withhold or release the continuing resolution (K75 - an opposition player's verb stays in
        /// the action row above). While the debt limit is reached, a player-president proposes the raise - at P.L. 117-73's or P.L. 119-21's size, the two increases of record
        /// sizes, each added to the excess (K77) - or a suspension through 1 January of the next year or the year after (P.L. 118-5's form), voted at the
        /// deadline (K25) or, past it, the day it is tabled (K43); a party holding a chamber against the presidency withholds or releases the raise (K22). Each
        /// button's slip says what follows. The debt tile's slip states the limit; these are its actions (board 30c's verb card, drawn as the page's buttons).
        /// </summary>
        private void DrawUsCongressVerbs(float width)
        {
            // the Unity run's real907 (GUILayout: Mismatched LayoutGroup.repaint, every US Budget page blank): the offer is computed ONCE a frame, so the Layout
            // event and the events after it draw the same rows; the rows are planned on the page's width, which both passes know (a GetRect in the Layout
            // event is a placeholder); and a click is a REQUEST, acted on in the next Update, so nothing drawn changes mid-frame
            if (_usCongressVerbsFrame != Time.frameCount || _usCongressVerbs == null)
            {
                _usCongressVerbs = BuildUsCongressVerbs();
                _usCongressVerbsFrame = Time.frameCount;
            }
            List<(string Label, string Id, SlipContent Slip, System.Action Act)> verbs = _usCongressVerbs;
            if (verbs.Count == 0) { return; }

            // the buttons, the page's one form, as many to a row as fit the page's width
            float h = V35.Px(34f), gap = V35.Px(8f);
            var rows = new List<List<int>>();
            float used = 0f;
            for (int k = 0; k < verbs.Count; k++)
            {
                float w = BudgetButtonWidth(verbs[k].Label, null);
                if (rows.Count == 0 || (rows[rows.Count - 1].Count > 0 && used + w > width)) { rows.Add(new List<int>()); used = 0f; }
                rows[rows.Count - 1].Add(k);
                used += w + gap;
            }
            foreach (List<int> line in rows)
            {
                GUILayout.Space(V35.Px(4f));
                Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
                float x = row.x;
                foreach (int k in line)
                {
                    float w = BudgetButtonWidth(verbs[k].Label, null);
                    var cell = new Rect(x, row.y, Mathf.Min(w, Mathf.Max(1f, row.xMax - x)), h);
                    if (DrawBudgetButton(cell, verbs[k].Label, null, true)) { _usCongressVerbRequested = verbs[k].Act; }   // acted on in the next Update
                    SlipAnchor(cell, verbs[k].Id);
                    _budgetSlipBook.Anchors[verbs[k].Id] = verbs[k].Slip;
                    x += w + gap;
                }
            }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>The US Congress's verbs as the page offers them this frame (`DrawUsCongressVerbs`, computed once a frame).</summary>
        private List<(string Label, string Id, SlipContent Slip, System.Action Act)> _usCongressVerbs;
        private int _usCongressVerbsFrame = -1;

        /// <summary>A verb the player clicked on the Budget page, acted on at the top of the next `Update` (so no drawn state changes mid-frame).</summary>
        private System.Action _usCongressVerbRequested;

        /// <summary>The verbs `DrawUsCongressVerbs` draws: their labels, slips and acts, read from the model once a frame.</summary>
        private List<(string Label, string Id, SlipContent Slip, System.Action Act)> BuildUsCongressVerbs()
        {
            var verbs = new List<(string Label, string Id, SlipContent Slip, System.Action Act)>();
            if (_playerCountry == null || !UsBudgetProcedure.Applies(_playerCountry)) { return verbs; }
            System.DateTime today = _simulationManager.CurrentDate;
            bool governs = _simulationManager.PlayerMayIntroduce(PlayerCountryId, out _);
            string mine = _playerCountry.PlayerPartyAbbrev;

            // K75: the player-president's own party and the continuing resolution
            BudgetBill pendingBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            bool ownWithheld = !string.IsNullOrEmpty(mine) && _playerCountry.ContinuingResolutionWithheldBy == mine;
            if (governs && (ownWithheld || ((pendingBill == null || pendingBill.ContinuingResolution) && UsBudgetProcedure.MayWithhold(_playerCountry, pendingBill, today, out _))))
            {
                verbs.Add((ownWithheld ? "Release the resolution" : "Withhold the resolution", "budget:uscr",
                    new SlipContent(ownWithheld ? "RELEASE THE CONTINUING RESOLUTION" : "WITHHOLD THE CONTINUING RESOLUTION")
                        .Add(ownWithheld ? "YOUR PARTY VOTES AGAINST ITS OWN GOVERNMENT'S RESOLUTION IN BOTH CHAMBERS · RELEASED, ONE IS ASKED NOTHING AND PASSES UNCONTESTED"
                            : "YOUR PARTY WOULD VOTE AGAINST ITS OWN GOVERNMENT'S RESOLUTION IN BOTH CHAMBERS AND EVERY OTHER PARTY FOR IT")
                        .Add("A RESOLUTION THAT FALLS FUNDS NOTHING - UNFUNDED, THE GOVERNMENT SHUTS DOWN, AND IT BEARS EACH ONE THAT FALLS")
                        .Add("IN A SHUTDOWN YOUR OWN SENATORS MAY CROSS AT CLOTURE - DRAWN FROM THE CLOTURE VOTES OF EVERY LAPSE OF RECORD, " + PoliSim.Elections.UsLapseDraw.FittedLabel),
                    () => { if (!_simulationManager.WithholdContinuingResolution(PlayerCountryId, !ownWithheld, out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: the continuing resolution's verb was refused - {refused}"); } }));
            }

            // K23: the player-president's raise or suspension
            BudgetBill pendingRaise = _simulationManager.GetPendingDebtLimitBill(PlayerCountryId);
            bool president = governs && !string.IsNullOrEmpty(mine) && _playerCountry.Government != null && _playerCountry.Government.RoleOf(mine) == PoliSim.Elections.PlayerRole.PrimeMinister;
            if (UsDebtLimit.MayPropose(_playerCountry, president, true, pendingRaise, today, UsDebtLimit.RaiseUsd, System.DateTime.MinValue, out _))
            {
                double excess = UsDebtLimit.ExcessUsd(_playerCountry);
                var sizes = new List<double>(UsDebtLimit.ProposalSizesUsd);   // K77: each size is added to the excess on the day the raise is law
                string when = UsDebtLimit.VotedTheDayTabled(_playerCountry, today) ? "BOTH CHAMBERS VOTE IT TODAY - THE MEASURES HAVE RUN OUT"
                    : "BOTH CHAMBERS VOTE IT ON " + UsDebtLimit.BindsOn(_playerCountry).ToString("d MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + ", WHEN THE MEASURES RUN OUT";
                foreach (double size in sizes)
                {
                    double tabled = size;
                    string money = UiFormat.Money((float)(tabled / 1e9), MoneyUnit.Billions);
                    verbs.Add(("Raise · excess + " + money, "budget:uslimit:" + verbs.Count.ToString(CultureInfo.InvariantCulture),
                        new SlipContent("PROPOSE A RAISE OF THE EXCESS AND " + money)
                            .Add("THE LIMIT UP BY THE DEBT'S EXCESS OVER IT ON THE DAY IT IS LAW, AND " + money + " MORE - THE ROOM IT LEAVES · THE EXCESS TODAY " + UiFormat.Money((float)(excess / 1e9), MoneyUnit.Billions))
                            .Add("BY RECONCILIATION - A MAJORITY IN BOTH CHAMBERS, THE HOUSE FIRST · " + when)
                            .Add("THEN TO YOUR DESK, SIGNED OR VETOED AS ANY BILL"),
                        () => { if (!_simulationManager.ProposeDebtLimitRaise(PlayerCountryId, tabled, System.DateTime.MinValue, out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: the debt-limit raise was refused - {refused}"); } }));
                }
                System.DateTime voteDay = UsDebtLimit.VoteDayOf(_playerCountry, today);   // the review's M2: a suspension's day after its vote's
                for (int ahead = 1; ahead <= 2; ahead++)
                {
                    var through = new System.DateTime(voteDay.Year + ahead, 1, 1);
                    string day = through.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
                    verbs.Add(("Suspend to " + day, "budget:uslimit:" + verbs.Count.ToString(CultureInfo.InvariantCulture),
                        new SlipContent("PROPOSE A SUSPENSION THROUGH " + day.ToUpperInvariant())
                            .Add("NO LIMIT APPLIES UNTIL THEN · THE DAY AFTER, IT RETURNS AT THE DEBT SUBJECT TO IT THAT DAY")
                            .Add("BY RECONCILIATION - A MAJORITY IN BOTH CHAMBERS, THE HOUSE FIRST · " + when)
                            .Add("THEN TO YOUR DESK, SIGNED OR VETOED AS ANY BILL"),
                        () => { if (!_simulationManager.ProposeDebtLimitRaise(PlayerCountryId, 0d, through, out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: the debt-limit suspension was refused - {refused}"); } }));
                }
            }

            // K90: the President answers the binding limit's hold by letting it bind - the cut bites until he proposes later
            if (_simulationManager.DebtLimitAwaitsPresident(PlayerCountryId))
            {
                verbs.Add(("Let limit bind", "budget:uslimitbind",
                    new SlipContent("LET THE LIMIT BIND")
                        .Add("THE CLOCK RUNS ON WITH NO RAISE BEFORE CONGRESS: SPENDING OTHER THAN INTEREST IS CUT TO REVENUE LESS INTEREST UNTIL YOU PROPOSE ONE")
                        .Add("YOU ARE ASKED AGAIN WHEN SOMETHING CHANGES: A RAISE THAT FALLS OR IS VETOED, OR A MAJORITY RELEASING ITS WITHHOLD"),
                    () => { if (!_simulationManager.LetDebtLimitBind(PlayerCountryId, out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: letting the debt limit bind was refused - {refused}"); } }));
            }

            // K22: a party holding a chamber against the presidency withholds or releases the raise
            bool raiseWithheld = !string.IsNullOrEmpty(mine) && _playerCountry.DebtLimitWithheldBy == mine;
            if (raiseWithheld || UsDebtLimit.MayWithhold(_playerCountry, true, today, out _))
            {
                verbs.Add((raiseWithheld ? "Release the raise" : "Withhold the raise", "budget:uslimitwithhold",
                    new SlipContent(raiseWithheld ? "RELEASE THE DEBT-LIMIT RAISE" : "WITHHOLD THE DEBT-LIMIT RAISE")
                        .Add(raiseWithheld ? "YOUR PARTY VOTES AGAINST EVERY RAISE IN BOTH CHAMBERS · RELEASED, THE NEXT IS ASKED NOTHING AND PASSES UNCONTESTED"
                            : "YOUR PARTY WOULD VOTE AGAINST EVERY RAISE IN BOTH CHAMBERS AND EVERY OTHER PARTY FOR IT - A CHAMBER YOUR PARTY HOLDS STOPS IT")
                        .Add("ONCE THE MEASURES RUN OUT WITH NO RAISE LAW, SPENDING OTHER THAN INTEREST IS CUT TO REVENUE LESS INTEREST, AND THE GOVERNMENT BEARS EACH RAISE THAT FALLS")
                        .Add("NO OTHER PARTY WITHHOLDS ONE"),
                    () => { if (!_simulationManager.WithholdDebtLimitRaise(PlayerCountryId, !raiseWithheld, out string refused)) { NoteBudgetRefusal(refused); Debug.Log($"BUDGET: the debt-limit verb was refused - {refused}"); } }));
            }
            return verbs;
        }

        /// <summary>The page's button (the composition's): brass and its edge where it acts, the paper and a muted edge where it cannot; the count in a chip
        /// inside it. One control on every frame, enabled or not.</summary>
        /// <summary>§737: the button's width for its label and count - what <see cref="DrawBudgetButton"/> draws, for a caller placing something beside it.</summary>
        private float BudgetButtonWidth(string label, string count)
        {
            float labelW = Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent(label)).x);
            float countW = count != null ? Mathf.Ceil(V35Mono(V35.Floor, V35.OnDataDark, bold: true).CalcSize(new GUIContent(count)).x) + V35.Px(12f) : 0f;
            return V35.Px(14f) * 2f + labelW + (count != null ? V35.Px(10f) + countW : 0f);
        }

        private bool DrawBudgetButton(Rect row, string label, string count, bool enabled)
        {
            GUIStyle face = V35Serif(V35.Name, enabled ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted);
            GUIStyle countFace = V35Mono(V35.Floor, V35.OnDataDark, bold: true, TextAnchor.MiddleCenter);
            float labelW = Mathf.Ceil(face.CalcSize(new GUIContent(label)).x);
            float countW = count != null ? Mathf.Ceil(countFace.CalcSize(new GUIContent(count)).x) + V35.Px(12f) : 0f;
            float pad = V35.Px(14f);
            var button = new Rect(row.x, row.y, BudgetButtonWidth(label, count), row.height);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(button, enabled ? V35.Brass : V35.OffPaper);
                Color edge = enabled ? PoliSimTheme.Caution : V35.DataSand;
                PoliSimTheme.Rule(new Rect(button.x, button.y, button.width, 1f), edge);
                PoliSimTheme.Rule(new Rect(button.x, button.yMax - 1f, button.width, 1f), edge);
                PoliSimTheme.Rule(new Rect(button.x, button.y, 1f, button.height), edge);
                PoliSimTheme.Rule(new Rect(button.xMax - 1f, button.y, 1f, button.height), edge);
                PoliSimWidgets.MeasuredLabel(new Rect(button.x + pad, button.y, labelW, button.height), label, face);
                if (count != null)
                {
                    float ch = Mathf.Ceil(countFace.CalcSize(new GUIContent(count)).y) + V35.Px(2f);
                    var chip = new Rect(button.x + pad + labelW + V35.Px(10f), button.y + Mathf.Round((button.height - ch) * 0.5f), countW, ch);
                    PoliSimTheme.Rule(chip, PoliSimTheme.Caution);
                    PoliSimWidgets.MeasuredLabel(chip, count, countFace);
                }
            }
            bool ambient = GUI.enabled;
            GUI.enabled = ambient && enabled;
            bool clicked = PoliSimWidgets.Button(button, GUIContent.none, GUIStyle.none);
            GUI.enabled = ambient;
            return clicked && enabled;
        }

        // =============================================================================================================================================
        // If passed
        // =============================================================================================================================================

        /// <summary>§773: a statute part as the if-passed slip names it - the subject of its line.</summary>
        private static string StatutePartWords(BudgetBill.StatutePart part) =>
            part == BudgetBill.StatutePart.Rates ? "ITS RATES ARE" : part == BudgetBill.StatutePart.PensionAge ? "ITS PENSION AGE IS"
            : part == BudgetBill.StatutePart.Benefits ? "ITS BENEFIT LEVELS ARE" : "ITS FUND RULES ARE";

        /// <summary>§773: what stays where a statute act falls - the if-passed slip's words.</summary>
        private static string StatutePartStays(BudgetBill.StatutePart part) =>
            part == BudgetBill.StatutePart.Rates ? "THE OLD RATES WOULD STAND" : part == BudgetBill.StatutePart.PensionAge ? "THE PENSION AGE WOULD STAY AS IT IS"
            : part == BudgetBill.StatutePart.Benefits ? "THE BENEFIT LEVELS WOULD STAY AS THEY ARE" : "THE FUND WOULD STAY AS IT IS";

        /// <summary>
        /// The composition's *if passed* panel, in the right-hand four columns, drawn only while a draft stands (the columns are kept either way - the
        /// page's head). The count first: ✓ or ✗ (the count decides, FOR against AGAINST, the undecided abstaining - P3-A2's ruling), the seats FOR of the
        /// chamber, the bar of FOR, UNDECIDED and AGAINST - no majority tick, because no fixed seat line decides; every party's side and reason on the count's
        /// slip. Then what the draft does to the year's book (the balance's direction in the neutral ink, the warning only where the year's deficit with the
        /// draft breaches the country's rule), and the outcomes it moves - the preview with the draft against without, in the outcome's ink where most agree.
        /// </summary>
        private void DrawBudgetIfPassed(float width, BudgetBill draft, int changes)
        {
            if (changes <= 0) { return; }
            bool usActs = UsBudgetProcedure.Applies(_playerCountry);   // PS-6 US-22: the US Congress votes the draft as its acts - the count is the appropriation's
            BillConcern concern = usActs ? UsBudgetProcedure.ConcernOf(_playerCountry, draft, UsBudgetProcedure.Act.Appropriation)
                : ParliamentSystem.GetBudgetBillConcern(_playerCountry, ParliamentSystem.BudgetActOf(_playerCountry, draft));   // §768/§773: Poland's budget act - the spending alone
            bool contested = concern != null && !concern.IsEmpty;
            System.DateTime budgetVoteDay = ChamberVerdicts.VoteDay(_simulationManager.CurrentDate, _simulationManager.GetPendingBudgetBill(PlayerCountryId)?.DaysRemaining);
            bool wouldPass = _chamberVerdicts.WouldPass(_playerCountry, concern, budgetVoteDay);
            int forSeats = 0, againstSeats = 0, undecided = 0;
            if (contested)
            {
                foreach ((PoliticalParty _, int seats, int side, float _, bool measured) in _chamberVerdicts.SeatSides(_playerCountry, concern))
                {
                    if (!measured) { continue; }
                    if (side > 0) { forSeats += seats; } else if (side < 0) { againstSeats += seats; } else { undecided += seats; }
                }
            }
            int total = forSeats + againstSeats + undecided;
            EnsureBudgetImpact(draft);
            List<(string Name, string Icon, string Figure, Color Ink, string Id)> book = new List<(string, string, string, Color, string)>();
            Color neutral = V35.DirectionNeutral;
            book.Add(("Revenue", "in", BudgetSignedFigure(_cachedBudgetImpact.RevenueDelta), neutral, "ifpassed:revenue"));
            book.Add(("Spending", "out", BudgetSignedFigure(_cachedBudgetImpact.SpendingDelta), neutral, "ifpassed:spending"));
            string rule = null;
            if (_cachedPreview != null)
            {
                float yearBalance = _cachedPreview.RevenueEstimate - _cachedPreview.SpendingEstimate;
                FiscalRules.Breaches(PlayerCountryId, FiscalRules.Measure.Deficit, -yearBalance / Mathf.Max(0.0001f, _playerCountry.State.NominalGdp) * 100f, out rule);
            }
            book.Add(("Balance", "scales", BudgetSignedFigure(_cachedBudgetImpact.NetDelta), rule != null ? V35.Warning : neutral, "ifpassed:balance"));
            // MF q33 (D-WORKLIST §1, board 32e): in a programme year the panel says whether the draft meets the year's target (the board's tick and cross; the
            // row's figure is text, so its ink and its words carry them)
            if (_cachedPreview != null)
            {
                float draftDeficit = -(_cachedPreview.RevenueEstimate - _cachedPreview.SpendingEstimate) / Mathf.Max(0.0001f, _playerCountry.State.NominalGdp) * 100f;
                if (ProgrammeVerdict(draftDeficit, out string programmeWords, out Color programmeInk, out float programmeTarget))
                {
                    book.Add(("Programme target", "debt", programmeWords, programmeInk, "ifpassed:programme"));
                    _budgetSlipBook.Anchors["ifpassed:programme"] = new SlipContent("PROGRAMME TARGET · " + programmeWords.ToUpperInvariant())
                        .Add("The year's deficit with the draft, " + UiFormat.Number(draftDeficit, 1) + " % of GDP, against this year's target of " + UiFormat.Number(programmeTarget, 1) + " %")
                        .Add("A miss withholds the next year's loan");
                }
            }

            GUIStyle rowName = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            float rowH = V35.Px(V35.ListRow) + V35.Px(3f);
            float headH = V35.Px(26f), countH = V35.Px(30f), barH = V35.Px(12f);
            float height = V35.Px(V35.CardPadY) * 2f + headH + countH + V35.Px(6f) + barH + V35.Px(10f) + rowH * book.Count + V35.Px(8f) + rowH * _cachedPreviewEffects.Count
                + (_cachedPreviewEffects.Count == 0 ? rowH : 0f);
            Rect card = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(card, V35.CardPaper);
                PoliSimTheme.Rule(new Rect(card.x, card.y, card.width, 2f), PoliSimTheme.Caution);
                PoliSimTheme.Rule(new Rect(card.x, card.yMax - 2f, card.width, 2f), PoliSimTheme.Caution);
                PoliSimTheme.Rule(new Rect(card.x, card.y, 2f, card.height), PoliSimTheme.Caution);
                PoliSimTheme.Rule(new Rect(card.xMax - 2f, card.y, 2f, card.height), PoliSimTheme.Caution);
            }
            float padX = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY);
            var inner = new Rect(card.x + padX, card.y + padY, card.width - padX * 2f, card.height - padY * 2f);
            DrawV35SectionHead(new Rect(inner.x, inner.y, inner.width, headH), "If passed");
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, headH), "ifpassed:head");
            _budgetSlipBook.Anchors["ifpassed:head"] = new SlipContent("IF PASSED")
                .Add("THE DRAFT ON EVERY TAB AS ONE BILL - THE COUNT TODAY, THE YEAR'S BOOK, THE OUTCOMES")
                .Add(BudgetPreviewScope);

            // the count
            float y = inner.y + headH;
            var countRow = new Rect(inner.x, y, inner.width, countH);
            if (Event.current.type == EventType.Repaint)
            {
                float side = V35.Px(18f);
                DrawStateGlyph(new Rect(countRow.x, countRow.y + Mathf.Round((countH - side) * 0.5f), side, side), wouldPass ? Symbol.Good : Symbol.Bad, wouldPass ? PoliSimTheme.Good : PoliSimTheme.Bad);
                GUIStyle figure = V35Mono(V35.Name, PoliSimTheme.TextPrimary, bold: true);
                string count = contested ? forSeats.ToString(CultureInfo.InvariantCulture) : "—";
                float fw = Mathf.Ceil(figure.CalcSize(new GUIContent(count)).x) + 2f;
                float fx = countRow.x + side + V35.Px(8f);
                PoliSimWidgets.MeasuredLabel(new Rect(fx, countRow.y, fw, countH), count, figure);
                string of = contested ? "of " + total.ToString(CultureInfo.InvariantCulture) + " seats" : "uncontested";
                PoliSimWidgets.MeasuredLabel(new Rect(fx + fw + V35.Px(6f), countRow.y, Mathf.Max(1f, countRow.xMax - fx - fw - V35.Px(6f)), countH), of, V35Serif(V35.Floor, PoliSimTheme.TextMuted));
            }
            SlipAnchor(countRow, "ifpassed:count");
            // §768 (Elias's ruling D4), §773 (E2): where the procedure is the Sejm's budget act and statutes (Poland, US-20), each statute part the draft changes travels in its own act - the
            // rates, the pension age, the benefit levels, the fund's rules - its count and, F3, its veto risk as a percentage (the answer is the vote day's draw)
            var acts = new List<BudgetBill.StatutePart>();
            if (PoliSim.Elections.WorldClock.StatutePartsAreActs(PlayerCountryId))   // US-20: Poland's procedure, not its veto
            {
                foreach (BudgetBill.StatutePart part in BudgetBill.StatuteParts) { if (draft.Changes(part, _playerCountry)) { acts.Add(part); } }
            }
            // K322 S2 (K320 (3)): the count's slip at its caps - four lines at most of at most ten words: the count, how it is decided, the acts, the parties.
            // What a line names is a marked term, its words (each act's verdict, the rules, a party's side and reasons) that term's level-2 slip, one line - the
            // slip held up to fourteen lines, a party's reasons one line of some forty words. Every word it held is on it, within the two levels.
            PeopleSlips.Book countBook = _budgetSlipBook;
            string Term(string term, IEnumerable<string> words)
            {
                countBook.Terms[term] = new SlipContent(term).Add(string.Join(" · ", words));
                return "[[" + term + "]]";
            }
            Congress.Verdict congress = contested ? _chamberVerdicts.Chambers(_playerCountry, concern, budgetVoteDay) : null;   // PS-6 US-18: both chambers, where the budget goes through two
            string countLine = contested ? $"FOR {forSeats} · AGAINST {againstSeats}" + (undecided > 0 ? $" · UNDECIDED {undecided}" : "")
                : usActs ? "THE APPROPRIATION CHANGES NO LINE · UNCONTESTED" : acts.Count > 0 ? "THE BUDGET ACT CHANGES NOTHING · UNCONTESTED" : "NOTHING CHANGES · UNCONTESTED";
            var countSlip = new SlipContent(wouldPass ? "WOULD PASS" : "WOULD FAIL").Add(countLine + (usActs && congress != null ? " · THE HOUSE, ON THE APPROPRIATION" : congress != null ? " · THE HOUSE" : string.Empty));
            if (usActs)
            {
                // PS-6 US-22: the draft as Congress votes it - each act with its procedure and where it stops, the president's answer to each, what follows a
                // fallen appropriation, and what is not modelled
                Congress.Verdict reconciliation = null;
                var actTerms = new List<string>();
                foreach (UsBudgetProcedure.Act act in UsBudgetProcedure.Acts)
                {
                    if (!UsBudgetProcedure.IsVoted(draft, act, _playerCountry)) { continue; }
                    BillConcern actConcern = act == UsBudgetProcedure.Act.Appropriation ? concern : UsBudgetProcedure.ConcernOf(_playerCountry, draft, act);
                    Congress.Verdict actVerdict = actConcern.IsEmpty ? null : _chamberVerdicts.Chambers(_playerCountry, actConcern, budgetVoteDay);
                    if (act == UsBudgetProcedure.Act.Reconciliation) { reconciliation = actVerdict; }
                    var actWords = new List<string> { "THE " + UsBudgetProcedure.ActWords(act).ToUpperInvariant() + ", " + UsBudgetProcedure.ProcedureOf(act) + " - "
                        + (actVerdict == null ? "NOTHING ASKED, IT PASSES UNCONTESTED" : Congress.StopWords(PlayerCountryId, actVerdict)) };
                    if (actVerdict != null && actVerdict.Passed) { actWords.AddRange(ChamberVerdicts.UsVetoLines(PlayerCountryId, _chamberVerdicts.UsVetoProjection(_playerCountry, actConcern, budgetVoteDay))); }
                    actTerms.Add(Term(UsBudgetProcedure.ActWords(act).ToUpperInvariant(), actWords));
                }
                if (actTerms.Count > 0) { countSlip.Add("THE ACTS · " + string.Join(" · ", actTerms)); }
                var counts = new List<string>();
                if (congress != null) { counts.Add("THE COUNT ABOVE IS THE HOUSE'S, ON THE APPROPRIATION"); counts.Add(Congress.RuleWords(congress)); }
                if (reconciliation != null) { counts.Add(Congress.RuleWords(reconciliation)); }   // its yearly limit, not modelled, said
                countSlip.Add("THE RULES · " + Term("THE PROCEDURE", UsBudgetProcedure.ProcedureWords()) + (counts.Count > 0 ? " · " + Term("HOW EACH CHAMBER COUNTS", counts) : string.Empty));
            }
            else if (congress != null)
            {
                // the Congress branch counts as each chamber counts (LawsV35's form), and says why a budget that moves a spending line goes to cloture whole
                var stops = new List<string> { "THE COUNT ABOVE IS THE HOUSE'S - " + Congress.StopWords(PlayerCountryId, congress) };
                stops.AddRange(Congress.OrderWords(PlayerCountryId, congress));
                countSlip.Add(Term("WHERE IT STOPS", stops) + " · " + Term("THE RULES", new[] { Congress.RuleWords(congress),
                    "A BUDGET THAT MOVES ANY SPENDING LINE, OR THE PENSION AGE (ITS OWN STATUTE), GOES TO CLOTURE WHOLE; ONE MOVING ONLY RATES, BENEFIT LEVELS OR THE FUND RIDES RECONCILIATION - SPLITTING A BUDGET INTO ITS ACTS IS NOT YET MODELLED" }));
            }
            else
            {
                countSlip.Add("THE COUNT DECIDES · " + Term("NO FIXED SEAT LINE", new[] { "FOR AGAINST AGAINST, THE UNDECIDED ABSTAINING" }));
            }
            bool riskSaid = false;   // F3 (the review's finding: the slip's height): whose rate the risk is, said once - on the first act at risk
            var statuteTerms = new List<string>();
            foreach (BudgetBill.StatutePart part in acts)
            {
                BillConcern actConcern = ParliamentSystem.GetBudgetBillConcern(_playerCountry, draft.PartOf(part));
                bool actPasses = _chamberVerdicts.WouldPass(_playerCountry, actConcern, budgetVoteDay);
                PoliSim.Elections.PresidentialVeto.Outcome actVeto = _chamberVerdicts.Veto(_playerCountry, actConcern, budgetVoteDay);   // F3: the president on the budget's vote day
                string stays = StatutePartStays(part);
                var actWords = new List<string> { "IF THE BUDGET PASSES, " + StatutePartWords(part) + " A SEPARATE " + SimulationManager.ActWords(part).ToUpperInvariant() + " - "
                    + (!actPasses ? "IT WOULD FAIL; " + stays
                    : ChamberVerdicts.AtRisk(actVeto) ? "IT WOULD PASS - VETO RISK " + ChamberVerdicts.RiskWords(actVeto) + "; IF VETOED, "
                        + (actVeto.OverrideCarries ? "THE VETO WOULD BE OVERRIDDEN" : stays)
                    : "IT WOULD PASS AND BE SIGNED") };
                if (!riskSaid && ChamberVerdicts.AtRisk(actVeto))
                {
                    riskSaid = true;
                    actWords.Add("AN ACT IS AT RISK WHERE THE PRESIDENT'S BACKING PARTY (" + PartySystems.ShortName(PlayerCountryId, actVeto.BackingParty) + ") DOES NOT VOTE FOR IT; THE PRESIDENT ("
                        + actVeto.President.ToUpperInvariant() + ") VETOES AT " + actVeto.RiskBasis.ToUpperInvariant());
                }
                statuteTerms.Add(Term(SimulationManager.ActWords(part).ToUpperInvariant(), actWords));
            }
            if (statuteTerms.Count > 0) { countSlip.Add("SEPARATE ACTS · " + string.Join(" · ", statuteTerms)); }
            if (contested)
            {
                var partyTerms = new List<string>();
                foreach (PartyStance stance in _chamberVerdicts.Stances(_playerCountry, concern))
                {
                    if (stance.Seats <= 0) { continue; }
                    string word = !stance.Measured ? "UNMEASURED" : stance.Side > 0 ? "FOR" : stance.Side < 0 ? "AGAINST" : "UNDECIDED";
                    string reason = StanceModel.ReasonShort(stance);
                    partyTerms.Add(Term(stance.Party.ShortName + " " + stance.Seats, new[] { word + (string.IsNullOrEmpty(reason) ? string.Empty : " · " + reason.ToUpperInvariant()) }));
                }
                if (partyTerms.Count > 0) { countSlip.Add(string.Join(" · ", partyTerms)); }
            }
            _budgetSlipBook.Anchors["ifpassed:count"] = countSlip;
            y += countH + V35.Px(6f);

            // the bar: FOR, UNDECIDED, AGAINST
            var bar = new Rect(inner.x, y, inner.width, barH);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
                if (total > 0)
                {
                    float fW = bar.width * forSeats / total, uW = bar.width * undecided / total;
                    PoliSimTheme.Rule(new Rect(bar.x, bar.y, fW, bar.height), PoliSimTheme.Good);
                    PoliSimTheme.Rule(new Rect(bar.x + fW, bar.y, uW, bar.height), V35.DataSand);
                    PoliSimTheme.Rule(new Rect(bar.x + fW + uW, bar.y, bar.width - fW - uW, bar.height), PoliSimTheme.Bad);
                }
            }
            SlipAnchor(bar, "ifpassed:count");
            y += barH + V35.Px(10f);

            // the year's book
            foreach ((string name, string icon, string figure, Color ink, string id) in book)
            {
                var r = new Rect(inner.x, y, inner.width, rowH);
                DrawBudgetPanelRow(r, icon, name, figure, ink);
                SlipAnchor(r, id);
                y += rowH;
            }
            _budgetSlipBook.Anchors["ifpassed:revenue"] = new SlipContent("REVENUE · " + BudgetSignedFigure(_cachedBudgetImpact.RevenueDelta)).Add("THE YEAR'S REVENUE WITH THE DRAFT AGAINST WITHOUT").Add("NO DIRECTION MOST AGREE ON - THE NEUTRAL INK");
            _budgetSlipBook.Anchors["ifpassed:spending"] = new SlipContent("SPENDING · " + BudgetSignedFigure(_cachedBudgetImpact.SpendingDelta)).Add("THE YEAR'S SPENDING WITH THE DRAFT AGAINST WITHOUT").Add("NO DIRECTION MOST AGREE ON - THE NEUTRAL INK");
            if (_cachedPreview != null && _cachedPreview.DebtLimitCut > 0f)   // PS-6 K26 (Elias's ruling of 2026-10-23): while the debt limit binds the preview's year is cut, and says so
            {
                _budgetSlipBook.Anchors["ifpassed:spending"].Add("THE DEBT LIMIT BINDS: THE YEAR'S BOOK CUTS " + UiFormat.Money(_cachedPreview.DebtLimitCut, MoneyUnit.Billions) + " OF SPENDING OTHER THAN INTEREST TO REVENUE LESS INTEREST, UNTIL A RAISE IS LAW")
                    .Add("THE CUT MOVES THE BOOK ONLY - OUTPUT, BENEFITS AND APPROVAL ARE NOT MOVED BY IT");
            }
            var balanceSlip = new SlipContent("BALANCE · " + BudgetSignedFigure(_cachedBudgetImpact.NetDelta)).Add("THE YEAR'S BALANCE WITH THE DRAFT AGAINST WITHOUT");
            balanceSlip.Add(rule ?? "WITHIN THE COUNTRY'S RULE WITH THE DRAFT - THE NEUTRAL INK (ELIAS'S RULING: THE WARNING IS A BREACH'S)");
            _budgetSlipBook.Anchors["ifpassed:balance"] = balanceSlip;
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(inner.x, y + V35.Px(3f), inner.width, 1f), V35.CardEdge); }
            y += V35.Px(8f);

            // the outcomes it moves
            if (_cachedPreviewEffects.Count == 0)
            {
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(new Rect(inner.x, y, inner.width, rowH), "No outcome moves", V35Serif(V35.Floor, PoliSimTheme.TextMuted)); }
            }
            foreach (EffectArrow effect in _cachedPreviewEffects)
            {
                var r = new Rect(inner.x, y, inner.width, rowH);
                string figure = EffectArrowsRenderer.V35Figure(effect.Name, effect.Value);   // §749: the row the signing plate shares
                DrawBudgetPanelRow(r, EffectArrowsRenderer.V35Icon(effect.Name), effect.Name, figure, EffectArrowsRenderer.V35Ink(effect.Value, effect.HigherIsBetter));
                string eid = "ifpassed:effect:" + effect.Name;
                SlipAnchor(r, eid);
                _budgetSlipBook.Anchors[eid] = new SlipContent(effect.Name.ToUpperInvariant() + " · " + figure).Add("NEXT YEAR, THE PREVIEW WITH THE DRAFT AGAINST WITHOUT").Add(BudgetPreviewScope);
                y += rowH;
            }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>One row of the panel: the icon, the name, the signed figure at the right in its ink.</summary>
        private void DrawBudgetPanelRow(Rect r, string icon, string name, string figure, Color ink)
        {
            float iconSide = V35.Px(18f);
            DrawV35Icon(new Rect(r.x, r.y + Mathf.Round((r.height - iconSide) * 0.5f), iconSide, iconSide), icon, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            if (Event.current.type != EventType.Repaint) { return; }
            GUIStyle figureFace = V35Mono(V35.Floor, ink, bold: true, TextAnchor.MiddleRight);
            float fw = Mathf.Ceil(figureFace.CalcSize(new GUIContent(figure)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - fw, r.y, fw, r.height), figure, figureFace);
            GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            float x = r.x + iconSide + V35.Px(8f), nameW = Mathf.Max(1f, r.xMax - fw - V35.Px(8f) - x);
            PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, nameW, r.height), V35Fit(name, nameFace, nameW, out _), nameFace);
        }

        /// <summary>A change in money as the panel prints it: ▲ or ▼ and its size, nothing where it is nil.</summary>
        private static string BudgetSignedFigure(float delta) =>
            Mathf.Abs(delta) < 0.005f ? UiFormat.Money(0f, MoneyUnit.Billions) : (delta > 0f ? "▲ " : "▼ ") + UiFormat.Money(Mathf.Abs(delta), MoneyUnit.Billions);

        /// <summary>The draft's effect on the year's book, cached on the draft's signature and the turn (C-C1: two previews over two clones - never per frame).</summary>
        private void EnsureBudgetImpact(BudgetBill draft)
        {
            string signature = BudgetDraftSignature(draft);
            if (_hasCachedBudgetImpact && _simulationManager.CurrentTurn == _cachedBudgetImpactTurn && string.Equals(signature, _cachedBudgetImpactSignature, System.StringComparison.Ordinal)) { return; }
            _cachedBudgetImpact = _simulationManager.EstimateBudgetBill(PlayerCountryId, draft);
            _cachedBudgetImpactSignature = signature;
            _cachedBudgetImpactTurn = _simulationManager.CurrentTurn;
            _hasCachedBudgetImpact = true;
        }
    }
}
