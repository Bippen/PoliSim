using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §737 (UI v3.5, Design's V35 composition): THE LAWS PAGE'S FRAME AND ITS LABOUR TAB. The title *Laws* with its six tabs as words and the †; the
    /// Labour tab as the composition lays it - *What these dials move* (the readings the tab's dials reach, as tiles) and *Labour dials* (each dial a
    /// tile, two to a row) - and the bill's call to action. The other five tabs are drawn as built under the new frame until their own items.
    ///
    /// <para><b>The dials' units</b> are the main session's rule (relayed 2026-10-01: *a real unit only where the model computes one; named settings,
    /// band edges [AUTHORED-DRAFT], where the dial is an abstract index; never a unit the model does not compute*) applied as Code's table sent to
    /// Design (`V35_ANSWERS.md` §1, Labour): the minimum wage in % of the median wage (Off where the country has no statutory minimum - Sweden,
    /// Italy); parental leave in WEEKS (the model's unit - the composition's 480 days are benefit days, not the model's); the working-hours rules,
    /// retraining and family support by name (<see cref="DialStops"/>); immigration as the flow the lever adds, in thousands of people a year against
    /// the projection (0.1 ‰ of the population per point from 50 - `LaborCouplings.ImmigrationPolicyNetMigrationSensitivity`). The dials still edit
    /// the statutory base (pass 3's coexistence ruling); a law in force's offset is the tile's slip.</para>
    /// </summary>
    public partial class GameController
    {
        private static readonly string[] LawsTabs = { "Labour market", "Crime & justice", "Sectors", "Policy web", "Trade", "Laws" };
        private static readonly PolicyLawsCategory[] LawsTabCategories =
            { PolicyLawsCategory.LaborMarket, PolicyLawsCategory.CrimeJustice, PolicyLawsCategory.Sectors, PolicyLawsCategory.PolicyWeb, PolicyLawsCategory.Trade, PolicyLawsCategory.Laws };

        /// <summary>§737: the Laws page's slips, built each frame as the page draws.</summary>
        private PeopleSlips.Book _lawsSlipBook = new PeopleSlips.Book();

        private void DrawPolicyLawsTab(float availableHeight, float availableWidth)
        {
            // P2-1.1: the sheet is sized to the FRAME, not to its content.
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            _lawsSlipBook = new PeopleSlips.Book();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            int clicked = DrawV35TitleTabs(titleRow, "Laws", LawsTabs, System.Array.IndexOf(LawsTabCategories, _policyLawsCategory), UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors));
            if (clicked >= 0) { _policyLawsCategory = LawsTabCategories[clicked]; }   // a click cannot race a drag on another control (the old sub-tab row's note)
            GUILayout.Space(V35.Px(4f));
            float bodyHeight = Mathf.Max(0f, availableHeight - titleHeight - V35.Px(4f));

            if (_policyLawsCategory == PolicyLawsCategory.LaborMarket)
            {
                float contentWidth = StatsContentWidth(availableWidth);
                int scrolledFrom = _slipAnchors.Count;
                _laborMarketScrollPosition = GUILayout.BeginScrollView(_laborMarketScrollPosition, GUILayout.Height(Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f)));
                GUI.enabled = !_isGameOver;
                V35.FloorGuarded = true;
                DrawLabourV35(contentWidth);
                V35.FloorGuarded = false;
                GUI.enabled = true;
                GUILayout.EndScrollView();
                MoveScrolledAnchors(scrolledFrom, GUILayoutUtility.GetLastRect(), _laborMarketScrollPosition);
                GUILayout.EndVertical();
                if (!DeskProvenance.On) { DrawSlips(_lawsSlipBook, GUILayoutUtility.GetLastRect()); }
                return;
            }

            // The five tabs not yet retrofitted - drawn as built under the v3.5 title: the screen's caption, the stat chips its levers reach, the trace a
            // chip opens, the content.
            DrawScreenCaption(PolicyLawsScreenCaption());
            float statRowWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle) - 8f;
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_policyLawsCategory);
            float statRowHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, statRowWidth, country: _playerCountry);
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, statRowWidth);
            float policyTraceGapStance = _simulationManager.GetWageGrowthGapAtPeriodOpen(PlayerCountryId);
            float policyTraceHostHeight = Mathf.Max(0f, bodyHeight - ScreenCaptionBlockHeight() - statRowHeight);
            float policyTraceHeight = StatTracePanel.MeasureHeight(_playerCountry, policyTraceGapStance, _labelStyle, statRowWidth, policyTraceHostHeight);
            StatTracePanel.Draw(_playerCountry, policyTraceGapStance, _labelStyle, _labelStyle, statRowWidth, policyTraceHostHeight);
            float contentHeight = Mathf.Max(0f, bodyHeight - ScreenCaptionBlockHeight() - statRowHeight - policyTraceHeight);
            switch (_policyLawsCategory)
            {
                case PolicyLawsCategory.CrimeJustice:
                    GUI.enabled = !_isGameOver;
                    DrawCrimeJusticeTab(contentHeight);
                    GUI.enabled = true;
                    break;
                case PolicyLawsCategory.Sectors:
                    GUI.enabled = !_isGameOver;
                    DrawSectorPolicy(contentHeight);
                    GUI.enabled = true;
                    break;
                case PolicyLawsCategory.PolicyWeb:
                    DrawPolicyWebTab(contentHeight);
                    break;
                case PolicyLawsCategory.Trade:
                    float scrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    _policyLawsContentScrollPosition = GUILayout.BeginScrollView(_policyLawsContentScrollPosition, GUILayout.Height(scrollHeight));
                    // The pane's inner width less the scroll view's own bar - the measured budget every wrapping label on the Trade screen takes.
                    DrawTradePolicyContent(Mathf.Max(0f, PoliSimWidgets.InnerWidth(availableWidth, _boxStyle) - GUI.skin.verticalScrollbar.fixedWidth - 12f));
                    GUILayout.EndScrollView();
                    break;
                case PolicyLawsCategory.Laws:
                    // Not wrapped in `GUI.enabled = !_isGameOver`: browsing a law's detail is informational; only the enact/repeal action is gated.
                    DrawLawsTab(contentHeight, availableWidth);
                    break;
            }
            GUILayout.EndVertical();
        }

        /// <summary>The anchors a scroll view's content registered, moved into the sheet's coordinates and kept only where the view shows them (Statistics'
        /// rule, shared).</summary>
        private void MoveScrolledAnchors(int from, Rect view, Vector2 scroll)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            for (int i = _slipAnchors.Count - 1; i >= from; i--)
            {
                (string id, Rect r) = _slipAnchors[i];
                var moved = new Rect(r.x + view.x - scroll.x, r.y + view.y - scroll.y, r.width, r.height);
                float top = Mathf.Max(moved.y, view.y), bottom = Mathf.Min(moved.yMax, view.yMax);
                if (bottom <= top) { _slipAnchors.RemoveAt(i); continue; }
                _slipAnchors[i] = (id, new Rect(moved.x, top, moved.width, bottom - top));
            }
        }

        // =============================================================================================================================================
        // Labour
        // =============================================================================================================================================

        private void DrawLabourV35(float width)
        {
            Country c = _playerCountry;
            DrawLawsReadings(width, UiPalette.SystemArea.Labor);

            // ---- the dials ----
            DrawLawsSectionHead("Labour dials", "laws:labour", width);
            _lawsSlipBook.Anchors["laws:labour"] = new SlipContent("LABOUR DIALS")
                .Add("THE DIALS ARE ONE BILL'S DRAFT · A LAW IN FORCE STACKS ITS OFFSET ON THE DIAL IT MOVES")
                .Add("A DIAL THE MODEL HOLDS AS AN INDEX IS SHOWN BY NAME - ITS BANDS' EDGES DECLARED: AUTHORED FOR THE GAME, NOT MEASURED");
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(width, 6), rowHeight = BudgetDialTileHeight(false);
            float population = c.State.Population;   // millions

            Rect row = LawsDialRow(width, rowHeight);
            // Behaviour 5: the minimum wage's dial is ALWAYS drawn - disabled where the country has no statutory minimum - so the control count never
            // follows mutable state; its draft is kept only where the wage exists.
            bool hasWage = c.MinimumWageImplemented;
            float minimumWage = DrawDialRow("Minimum Wage", c.MinimumWagePercentOfMedianBase, GetMinimumWageInput(c.MinimumWagePercentOfMedianBase),
                MinMinimumWagePercent, MaxMinimumWagePercent, "F0", "%", hasWage ? "% of median wage" : "none - collective bargaining",
                new Rect(row.x, row.y, tileWidth, rowHeight), new V35DialFace
                {
                    Icon = "coins", Title = "Minimum wage", Off = !hasWage,
                    Figure = v => UiFormat.Number(v, 0) + "% of median",
                    EndLeft = "0%", EndRight = "100% of median",
                    Census = hasWage ? "A STATUTORY MINIMUM AS A SHARE OF THE MEDIAN WAGE · % OF MEDIAN" : "OFF · NO STATUTORY MINIMUM - WAGES ARE SET BY COLLECTIVE BARGAINING; THE MODEL HOLDS NO ACT TO INTRODUCE ONE",
                }, hasWage, bandNote: hasWage ? LaborDialInForce(c.MinimumWagePercentOfMedianBase, c.MinimumWagePercentOfMedian) : null);
            if (hasWage) { _minimumWageInput = minimumWage; }
            _paidFamilyLeaveWeeksInput = DrawDialRow("Paid Family Leave", c.PaidFamilyLeaveWeeksBase, GetPaidFamilyLeaveWeeksInput(c.PaidFamilyLeaveWeeksBase),
                MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, "F0", string.Empty, "weeks",
                new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight), new V35DialFace
                {
                    Icon = "pram", Title = "Parental leave",
                    Figure = v => UiFormat.Number(v, 0) + " weeks",
                    EndLeft = "0 weeks", EndRight = "104 weeks",
                    Census = "PAID FAMILY LEAVE, IN WEEKS - THE MODEL'S UNIT (480 BENEFIT DAYS IS SWEDEN'S STATUTE; THE MODEL HOLDS WEEKS)",
                }, bandNote: LaborDialInForce(c.PaidFamilyLeaveWeeksBase, c.PaidFamilyLeaveWeeks));
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            _overtimeRegulationInput = DrawDialRow("Overtime / Working-Hour Regulation", c.OvertimeRegulationBase, GetOvertimeRegulationInput(c.OvertimeRegulationBase),
                MinLaborDialLevel, MaxLaborDialLevel, "F0", string.Empty, "0 unregulated - 100 strict",
                new Rect(row.x, row.y, tileWidth, rowHeight), NamedFace("clock", DialStops.WorkingHours,
                    "THE MODEL COMPUTES NO HOURS · 0 UNREGULATED (LONG HOURS) … 100 STRICT CAPS - LOOSE LEFT, THE MODEL'S DIRECTION"),
                bandNote: LaborDialInForce(c.OvertimeRegulationBase, c.OvertimeRegulationLevel));
            _retrainingProgramInput = DrawDialRow("Workforce Retraining Programs", c.RetrainingProgramBase, GetRetrainingProgramInput(c.RetrainingProgramBase),
                MinLaborDialLevel, MaxLaborDialLevel, "F0", string.Empty, null,
                new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight), NamedFace("book", DialStops.Retraining,
                    "THE MODEL COMPUTES NO SPENDING FOR IT - AN INDEX"),
                bandNote: LaborDialInForce(c.RetrainingProgramBase, c.RetrainingProgramLevel));
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            _familyPolicyInput = DrawDialRow("Family Policy", c.FamilyPolicyBase, GetFamilyPolicyInput(c.FamilyPolicyBase),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 minimal - 100 pro-natalist",
                new Rect(row.x, row.y, tileWidth, rowHeight), NamedFace("family", DialStops.FamilySupport,
                    "0 MINIMAL … 100 PRO-NATALIST · IT MOVES THE BIRTH RATE (±1.5 PER 1 000); THE MODEL COMPUTES NO MONEY FOR IT"),
                bandNote: LaborDialInForce(c.FamilyPolicyBase, c.FamilyPolicyLevel));
            _immigrationPolicyInput = DrawDialRow("Immigration Policy", c.ImmigrationPolicyBase, GetImmigrationPolicyInput(c.ImmigrationPolicyBase),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 restrictive - 100 open",
                new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight), new V35DialFace
                {
                    Icon = "passport", Title = "Immigration",
                    Figure = v => ImmigrationFlowText(v, population),
                    EndLeft = "Restrictive", EndRight = "Open",
                    Census = "THE FLOW THE LEVER ADDS TO NET MIGRATION AGAINST THE PROJECTION - 0.1 ‰ OF THE POPULATION A YEAR PER POINT FROM 50 · "
                        + ImmigrationFlowText(0f, population).ToUpperInvariant() + " AT RESTRICTIVE, " + ImmigrationFlowText(100f, population).ToUpperInvariant() + " AT OPEN · ALL NET MIGRATION, NOT LABOUR MIGRATION ALONE",
                }, bandNote: LaborDialInForce(c.ImmigrationPolicyBase, c.ImmigrationPolicyLevel));
            GUILayout.Space(gutter);

            // ---- the bill ----
            LaborPolicyBill pending = _simulationManager.GetPendingLaborBill(PlayerCountryId);
            LaborPolicyBill draft = BuildLaborBillFromDrafts();
            DrawLawsBillAction(width, "Introduce labour bill", "laws:labourbill", ParliamentSystem.GetLaborBillConcern(c, draft), pending != null, pending != null ? pending.DaysRemaining : 0,
                LaborDraftChanges(), () => _simulationManager.IntroduceLaborBill(PlayerCountryId, BuildLaborBillFromDrafts()),
                pending != null ? $"A Labor Market bill is before Parliament - resolves in {pending.DaysRemaining} day(s)." : "No Labor Market bill before Parliament - the dials above are its draft.");
        }

        /// <summary>Immigration's figure: the flow the lever adds, in thousands of people a year against the projection, at the dial's level (DERIVED -
        /// `LaborCouplings.ImmigrationPolicyNetMigrationSensitivity` per mille of the population per point from 50).</summary>
        private static string ImmigrationFlowText(float level, float populationMillions)
        {
            float k = LaborCouplings.ImmigrationPolicyNetMigrationSensitivity * (level - 50f) * populationMillions;   // ‰ × millions = thousands
            return (k > 0.05f ? "+" : k < -0.05f ? "−" : "±") + UiFormat.Number(Mathf.Abs(k), 0) + " k a year";
        }

        /// <summary>How many of the labour dials the draft moves off their standing base.</summary>
        private int LaborDraftChanges()
        {
            Country c = _playerCountry;
            int n = 0;
            if (c.MinimumWageImplemented && !Mathf.Approximately(GetMinimumWageInput(c.MinimumWagePercentOfMedianBase), c.MinimumWagePercentOfMedianBase)) { n++; }
            if (!Mathf.Approximately(GetPaidFamilyLeaveWeeksInput(c.PaidFamilyLeaveWeeksBase), c.PaidFamilyLeaveWeeksBase)) { n++; }
            if (!Mathf.Approximately(GetOvertimeRegulationInput(c.OvertimeRegulationBase), c.OvertimeRegulationBase)) { n++; }
            if (!Mathf.Approximately(GetRetrainingProgramInput(c.RetrainingProgramBase), c.RetrainingProgramBase)) { n++; }
            if (!Mathf.Approximately(GetFamilyPolicyInput(c.FamilyPolicyBase), c.FamilyPolicyBase)) { n++; }
            if (!Mathf.Approximately(GetImmigrationPolicyInput(c.ImmigrationPolicyBase), c.ImmigrationPolicyBase)) { n++; }
            return n;
        }

        // =============================================================================================================================================
        // The shared pieces: the section head, the readings, the dial tile, the bill's action
        // =============================================================================================================================================

        private Rect LawsDialRow(float width, float height) => GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

        private void DrawLawsSectionHead(string title, string anchor, float width)
        {
            float h = V35.Px(26f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            DrawV35SectionHead(row, title);
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width, face.CalcSize(new GUIContent(title.ToUpperInvariant())).x + 4f), row.height), anchor);
            GUILayout.Space(V35.Px(6f));
        }

        /// <summary>A reading's v3.5 icon, by the words of its name.</summary>
        private static string LawsReadingIcon(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("unemploy")) { return "jobs"; }
            if (n.Contains("labor") || n.Contains("labour") || n.Contains("participation")) { return "people"; }
            if (n.Contains("population") || n.Contains("birth")) { return "family"; }
            if (n.Contains("approval")) { return "check"; }
            if (n.Contains("poverty")) { return "bowl"; }
            if (n.Contains("inflation")) { return "infl"; }
            if (n.Contains("gdp") || n.Contains("growth")) { return "chart"; }
            if (n.Contains("wage")) { return "coins"; }
            if (n.Contains("crime")) { return "gavel"; }
            if (n.Contains("migration")) { return "passport"; }
            if (n.Contains("debt")) { return "debt"; }
            if (n.Contains("confidence")) { return "gear"; }
            return "chart";
        }

        /// <summary>
        /// *What these dials move* (the composition's): the readings the tab's dials reach - the Policy Web's own edges from the tab's area, the most
        /// connected first, four at most (the stat row's rule, `PolicyScreenStats.GetStatsForArea`) - as tiles: the figure in its unit, its change over
        /// the last four quarters in the Policy Web's own judgment of its direction (neutral where it holds none - the two surfaces cannot disagree), its
        /// history's sparkline. A tile whose reading has a trace opens it, as a chip did (`StatTracePanel`), and the trace draws under the tiles; every
        /// tile emits its click control on every frame. What the four leave out is the section head's slip.
        /// </summary>
        private void DrawLawsReadings(float width, UiPalette.SystemArea area)
        {
            IReadOnlyList<StatNodeId> stats = PolicyScreenStats.GetStatsForArea(area, _playerCountry);
            int shown = Mathf.Min(PolicyScreenStatsRenderer.DefaultMaxStats, stats.Count);
            if (shown == 0) { return; }
            DrawLawsSectionHead("What these dials move", "laws:moves", width);
            var head = new SlipContent("WHAT THESE DIALS MOVE").Add("THE POLICY WEB'S EDGES FROM THIS TAB'S DIALS - THE MOST CONNECTED READINGS FIRST, FOUR SHOWN");
            if (stats.Count > shown)
            {
                var rest = new List<string>();
                for (int i = shown; i < stats.Count; i++) { rest.Add(PolicyScreenStats.GetName(stats[i]).ToUpperInvariant()); }
                head.Add("+" + (stats.Count - shown) + " MORE AFFECTED - " + string.Join(" · ", rest) + " - THE POLICY WEB TAB DRAWS THEM ALL");
            }
            head.Add("A TILE WITH A TRACE OPENS ITS FORMULA'S TERMS UNDER THE TILES");
            _lawsSlipBook.Anchors["laws:moves"] = head;

            var tiles = new List<V35TileData>();
            for (int i = 0; i < shown; i++)
            {
                StatNodeId stat = stats[i];
                float value = PolicyScreenStats.ReadLiveValue(stat, _playerCountry);
                bool? higherIsBetter = PolicyScreenStats.GetHigherIsBetter(stat);
                IReadOnlyList<float> history = PolicyWebRenderer.GetHistory(stat, _playerCountry.History);
                string name = PolicyScreenStats.GetName(stat);
                var t = new V35TileData { Icon = LawsReadingIcon(name), IconInk = UiPalette.GetAreaColor(area), Figure = PolicyScreenStats.Format(stat, value), Name = name, FigurePx = V35.FigureSmall,
                    Spark = history, SparkBelow = true };
                var slip = new SlipContent(name.ToUpperInvariant() + " · " + t.Figure);
                if (history != null && history.Count >= 5)
                {
                    float d = history[history.Count - 1] - history[history.Count - 5];
                    if (Mathf.Abs(d) >= 0.05f)
                    {
                        t.Change = (d > 0f ? "▲ " : "▼ ") + UiFormat.Number(Mathf.Abs(d), 1);
                        t.ChangeInk = !higherIsBetter.HasValue ? V35.DirectionNeutral : (d > 0f) == higherIsBetter.Value ? PoliSimTheme.Good : PoliSimTheme.Bad;
                        slip.Add((d > 0f ? "UP " : "DOWN ") + UiFormat.Number(Mathf.Abs(d), 1) + " OVER THE LAST FOUR QUARTERS");
                    }
                }
                slip.Add(!higherIsBetter.HasValue ? "NO DIRECTION MOST AGREE ON - ITS CHANGE IN THE NEUTRAL INK" : higherIsBetter.Value ? "HIGHER IS BETTER" : "LOWER IS BETTER");
                if (stat == StatNodeId.PopulationGrowthRate || name.ToLowerInvariant().Contains("population"))
                {
                    // the population rows the tab carried (§564), on the population reading's slip
                    EconomyState s = _playerCountry.State;
                    slip.Add("POPULATION " + UiFormat.Number(s.Population, 1) + " M · GROWTH " + s.PopulationGrowthRate.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " PER 1 000 A YEAR")
                        .Add("BIRTHS " + UiFormat.Number(s.BirthRate, 1) + " · DEATHS " + UiFormat.Number(s.DeathRate, 1) + " · NET MIGRATION " + s.NetMigrationRate.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " - PER 1 000 A YEAR")
                        .Add("DEPENDENCY " + UiFormat.Number(s.DependencyRatio, 1) + " PER 100 OF WORKING AGE");
                }
                if (StatTracePanel.SupportsTrace(stat)) { slip.Add("A CLICK OPENS ITS TRACE - THE FORMULA'S TERMS"); }
                _lawsSlipBook.Anchors["laws:moves:" + stat] = slip;
                tiles.Add(t);
            }
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(width, 3), rowHeight = 0f;
            foreach (V35TileData t in tiles) { rowHeight = Mathf.Max(rowHeight, V35TileHeight(t)); }
            Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
            for (int i = 0; i < tiles.Count; i++)
            {
                var r = new Rect(row.x + i * (tileWidth + gutter), row.y, tileWidth, rowHeight);
                // the chip's control, every frame for every tile (stable control layout) - a reading with no trace routes to a no-op, as the chips did
                if (PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none)) { StatTracePanel.NotifyChipClicked(stats[i]); }
                DrawV35Tile(r, tiles[i]);
                SlipAnchor(r, "laws:moves:" + stats[i]);
            }
            GUILayout.Space(gutter);

            // the trace a tile opened, as built (kept), under the tiles
            bool guarded = V35.FloorGuarded;
            V35.FloorGuarded = false;
            float gap = _simulationManager.GetWageGrowthGapAtPeriodOpen(PlayerCountryId);
            float host = V35.Px(360f);
            if (StatTracePanel.MeasureHeight(_playerCountry, gap, _labelStyle, width, host) > 0f)
            {
                GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(width));
                StatTracePanel.Draw(_playerCountry, gap, _labelStyle, _labelStyle, width - V35.Px(V35.CardPadX) * 2f - 4f, host);
                GUILayout.EndVertical();
                GUILayout.Space(gutter);
            }
            V35.FloorGuarded = guarded;
        }

        /// <summary>§737: how a v3.5 dial tile prints its value - the tile's icon and name, the figure it makes of the value, the two ends, an Off state
        /// (a dial the country does not have), and the census line its slip leads with; a named dial's stops.</summary>
        private sealed class V35DialFace
        {
            public string Icon;
            public string Title;
            public System.Func<float, string> Figure;
            public string EndLeft, EndRight;
            public bool Off;
            public string Census;
            public DialStops.Dial Stops;
        }

        /// <summary>A named dial's face: the stop the value falls in as the figure, the first and last stops as the ends, the bands in the slip.</summary>
        private static V35DialFace NamedFace(string icon, DialStops.Dial stops, string census) => new V35DialFace
        {
            Icon = icon, Title = stops.Title, Stops = stops,
            Figure = v => stops.At(v).Name,
            EndLeft = stops.Stops[0].Name, EndRight = stops.Stops[stops.Stops.Length - 1].Name,
            Census = census,
        };

        /// <summary>
        /// §737 (UI v3.5): THE DIAL AS A TILE. `DrawDialRow`'s own contract - the literal name first, then standing, draft, min, max, format, suffix and
        /// the trailing end-names, in those positions, which `DialLabelCheck` and `RangeCaptionCheck` read off the source - and the tile it draws in, with
        /// its face. The tile: the icon, the figure (the face's reading of the value - the draft's, in the draft's ink, while it differs), the name, the
        /// track (the ledger's own, <see cref="LedgerRow.Track"/>), its two ends; while a draft moves, the dial's range caption speaks in the ends' lane
        /// (P4-B2), the ends yielding it. The literal name keys the geometry the film reads (`RowTop`) and the caption's catalog; the band note (a law in
        /// force's offset) and the index itself are the slip's. One control, always, enabled or not.
        /// </summary>
        private float DrawDialRow(string name, float standing, float draft, float min, float max, string format, string suffix, string trailing, Rect tile,
            V35DialFace face, bool interactive = true, string captionKey = null, string bandNote = null)
        {
            bool drafted = interactive && !Mathf.Approximately(standing, draft);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Labor);
            Rect inner = DrawBudgetTileCard(tile, drafted, face.Off);
            Rect figureRect = DrawBudgetTileHead(inner, face.Icon, face.Off ? V35.PyramidThreshold : area, face.Off ? "Off" : face.Figure(drafted ? draft : standing),
                face.Off ? PoliSimTheme.TextMuted : drafted ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, face.Title, face.Off ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, new Rect(inner.xMax, inner.y, 0f, 0f));
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, figureRect.yMax - inner.y + V35.Px(20f)), "dial:" + name);

            Rect track = BudgetTrackRect(inner, out Rect endLane);
            float result = LedgerRow.Track(track, name, standing, draft, min, max, interactive, _sliderStyle, _sliderThumbStyle, LedgerRow.ScaleOf(_labelStyle));
            if (Event.current.type == EventType.Repaint)
            {
                bool speaking = interactive && DrawRangeCaption(name, captionKey ?? name, result, standing, min, max, endLane);
                if (!speaking) { DrawBudgetEndLabels(endLane, face.EndLeft, face.EndRight, face.Off || !interactive); }
            }
            if (interactive && PoliSim.Testing.CaptureIdentity.Armed && Event.current.type == EventType.Repaint)
            {
                LedgerRow.GeometryByRow[UiGuardContext.CurrentScreen + " / " + name] = (tile, track, figureRect, endLane);   // P4-1: rest equals mid-drag; the film's RowTop key
            }

            var slip = new SlipContent(face.Title.ToUpperInvariant() + " · " + (face.Off ? "OFF" : face.Figure(standing).ToUpperInvariant()));
            if (drafted) { slip.Add("DRAFTED · " + face.Figure(draft).ToUpperInvariant() + " · WAS " + face.Figure(standing).ToUpperInvariant()); }
            if (!string.IsNullOrEmpty(face.Census)) { slip.Add(face.Census); }
            if (face.Stops != null)
            {
                slip.Add("THE STOPS · " + face.Stops.Bands() + " · THE EDGES DECLARED - AUTHORED FOR THE GAME, NOT MEASURED")
                    .Add("THE INDEX " + standing.ToString(format, CultureInfo.InvariantCulture) + " OF 100 · 50 IS THE COUNTRY'S STATUS QUO");
            }
            if (!string.IsNullOrEmpty(bandNote)) { slip.Add(bandNote.ToUpperInvariant() + " · THE DIAL EDITS THE STATUTORY BASE"); }
            if (!string.IsNullOrEmpty(trailing) && face.Stops == null && !face.Off) { slip.Add(trailing.ToUpperInvariant()); }
            _lawsSlipBook.Anchors["dial:" + name] = slip;
            return interactive ? result : draft;
        }

        /// <summary>
        /// A tier bill's call to action (v3.5): the button - *Introduce … bill* with the count of changes, or *Pending · n d* while one is before
        /// Parliament - beside the count the draft would meet today (✓ or ✗, FOR and AGAINST; every party's side and reason on its slip), the status under
        /// them; where the role locks the lever, the lock and its reason in the button's place (the lever lock's rule - no control is drawn there).
        /// </summary>
        private void DrawLawsBillAction(float width, string label, string anchor, BillConcern concern, bool pending, int daysRemaining, int changes, System.Action introduce, string status)
        {
            GUILayout.Space(V35.Px(4f));
            float h = V35.Px(34f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            float countX = row.x;
            if (!_simulationManager.PlayerMayIntroduce(PlayerCountryId, out string lockedBecause))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    DrawStateGlyph(new Rect(row.x, row.y + Mathf.Round((row.height - V35.Px(16f)) * 0.5f), V35.Px(16f), V35.Px(16f)), Symbol.Locked, PoliSimTheme.TextSecondary);
                    GUIStyle lockFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + V35.Px(24f), row.y, row.width - V35.Px(24f), row.height), V35Fit(lockedBecause, lockFace, row.width - V35.Px(24f), out _), lockFace);
                }
                SlipAnchor(row, anchor);
                _lawsSlipBook.Anchors[anchor] = new SlipContent("LOCKED").Add(lockedBecause.ToUpperInvariant());
            }
            else
            {
                string text = pending ? "Pending · " + daysRemaining + " d" : label;
                string changeChip = !pending && changes > 0 ? changes + (changes == 1 ? " change" : " changes") : null;
                if (DrawBudgetButton(row, text, changeChip, !pending)) { introduce(); }
                countX = row.x + BudgetButtonWidth(text, changeChip) + V35.Px(16f);
                SlipAnchor(new Rect(row.x, row.y, Mathf.Max(1f, countX - row.x - V35.Px(16f)), row.height), anchor);
                _lawsSlipBook.Anchors[anchor] = new SlipContent(label.ToUpperInvariant()).Add(status.ToUpperInvariant());

                // the count the draft would meet today, beside the button
                bool contested = concern != null && !concern.IsEmpty;
                bool wouldPass = _chamberVerdicts.WouldPass(_playerCountry, concern);
                int forSeats = 0, againstSeats = 0, undecided = 0;
                if (contested)
                {
                    foreach ((PoliticalParty _, int seats, int side, float _, bool measured) in _chamberVerdicts.SeatSides(_playerCountry, concern))
                    {
                        if (!measured) { continue; }
                        if (side > 0) { forSeats += seats; } else if (side < 0) { againstSeats += seats; } else { undecided += seats; }
                    }
                }
                var countRect = new Rect(countX, row.y, Mathf.Max(1f, row.xMax - countX), row.height);
                if (Event.current.type == EventType.Repaint)
                {
                    float side = V35.Px(16f);
                    DrawStateGlyph(new Rect(countRect.x, countRect.y + Mathf.Round((row.height - side) * 0.5f), side, side), wouldPass ? Symbol.Good : Symbol.Bad, wouldPass ? PoliSimTheme.Good : PoliSimTheme.Bad);
                    string count = contested ? $"FOR {forSeats} · AGAINST {againstSeats}" + (undecided > 0 ? $" · UNDECIDED {undecided}" : "") : "Nothing changes · uncontested";
                    GUIStyle countFace = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
                    float cw = countRect.width - side - V35.Px(8f);
                    PoliSimWidgets.MeasuredLabel(new Rect(countRect.x + side + V35.Px(8f), countRect.y, Mathf.Max(1f, cw), countRect.height), V35Fit(count, countFace, Mathf.Max(1f, cw), out _), countFace);
                }
                SlipAnchor(countRect, anchor + ":count");
                var countSlip = new SlipContent(wouldPass ? "WOULD PASS" : "WOULD FAIL")
                    .Add(contested ? $"FOR {forSeats} · AGAINST {againstSeats}" + (undecided > 0 ? $" · UNDECIDED {undecided}" : "") : "NOTHING CHANGES · UNCONTESTED")
                    .Add("THE COUNT DECIDES - FOR AGAINST AGAINST, THE UNDECIDED ABSTAINING");
                if (contested)
                {
                    foreach (PartyStance stance in _chamberVerdicts.Stances(_playerCountry, concern))
                    {
                        if (stance.Seats <= 0) { continue; }
                        string word = !stance.Measured ? "UNMEASURED" : stance.Side > 0 ? "FOR" : stance.Side < 0 ? "AGAINST" : "UNDECIDED";
                        string reason = StanceModel.ReasonShort(stance);
                        countSlip.Add(stance.Party.Abbrev + " " + stance.Seats + " · " + word + (string.IsNullOrEmpty(reason) ? string.Empty : " · " + reason.ToUpperInvariant()));
                    }
                }
                _lawsSlipBook.Anchors[anchor + ":count"] = countSlip;
            }
            GUIStyle statusFace = V35SerifWrapped(V35.Floor, PoliSimTheme.TextSecondary);
            float sh = Mathf.Ceil(statusFace.CalcHeight(new GUIContent(status), width)) + V35.Px(4f);
            Rect statusRect = GUILayoutUtility.GetRect(width, sh, GUILayout.Width(width), GUILayout.Height(sh));
            if (Event.current.type == EventType.Repaint) { GUI.Label(statusRect, status, statusFace); }
            GUILayout.Space(V35.Px(V35.Gutter));
        }
    }
}
