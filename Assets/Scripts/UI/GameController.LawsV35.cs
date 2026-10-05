using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §737 (UI v3.5, Design's V35 composition): THE LAWS PAGE'S FRAME AND ITS LABOUR TAB. The title *Laws* with its six tabs as words and the †; the
    /// Labour tab as the composition lays it - *What these dials move* (the readings the tab's dials reach, as tiles) and *Labour dials* (each dial a
    /// tile, two to a row) - and the bill's call to action. §738: the Crime & justice tab - its six dials as tiles SET BY LAW (no knob, no control).
    /// §739: the Sectors tab - the eight sectors as tiles, the chosen sector's five dials as tiles. §740: the Policy web - two columns over the
    /// model's links, one area at a time (GameController.PolicyWebV35.cs). §741: Trade - trade with the five, partners
    /// and tariffs, the partners' override rates. §742: the Laws tab - the laws in force and before Parliament as a list card, the statute book as built under it.
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

        /// <summary>§748: the book the shared dial tile and the bill's action write their slips into - the Laws page's, unless another page that draws them
        /// (the Energy page's Policy tab) sets its own for the duration.</summary>
        private PeopleSlips.Book _dialSlipBookOverride;
        private PeopleSlips.Book DialSlipBook => _dialSlipBookOverride ?? _lawsSlipBook;

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

            if (_policyLawsCategory != PolicyLawsCategory.Laws)
            {
                // the tabs retrofitted to v3.5, each in its own scroll (the film resets them by name - UiScreenshotDriver.ResetScrolls)
                PolicyLawsCategory tab = _policyLawsCategory;
                float contentWidth = StatsContentWidth(availableWidth);
                int scrolledFrom = _slipAnchors.Count;
                float viewport = Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f);
                Vector2 previous;
                switch (tab)
                {
                    case PolicyLawsCategory.LaborMarket: previous = _laborMarketScrollPosition; break;
                    case PolicyLawsCategory.CrimeJustice: previous = _crimeJusticeScrollPosition; break;
                    case PolicyLawsCategory.Sectors: previous = _sectorPolicyScrollPosition; break;
                    case PolicyLawsCategory.Trade: previous = _policyLawsContentScrollPosition; break;
                    default: previous = _policyWebScrollPosition; break;
                }
                Vector2 scroll = GUILayout.BeginScrollView(previous, GUILayout.Height(viewport));
                switch (tab)
                {
                    case PolicyLawsCategory.LaborMarket: _laborMarketScrollPosition = scroll; break;
                    case PolicyLawsCategory.CrimeJustice: _crimeJusticeScrollPosition = scroll; break;
                    case PolicyLawsCategory.Sectors: _sectorPolicyScrollPosition = scroll; break;
                    case PolicyLawsCategory.Trade: _policyLawsContentScrollPosition = scroll; break;
                    default: _policyWebScrollPosition = scroll; break;
                }
                GUI.enabled = !_isGameOver || tab == PolicyLawsCategory.PolicyWeb;   // the web is read, not drafted - browsable when the game is over
                V35.FloorGuarded = true;
                switch (tab)
                {
                    case PolicyLawsCategory.LaborMarket: DrawLabourV35(contentWidth); break;
                    case PolicyLawsCategory.CrimeJustice: DrawCrimeV35(contentWidth); break;
                    case PolicyLawsCategory.Sectors: DrawSectorsV35(contentWidth); break;
                    case PolicyLawsCategory.Trade: DrawTradeV35(contentWidth); break;
                    default: DrawPolicyWebV35(contentWidth, new Rect(-contentWidth, scroll.y, contentWidth * 3f, viewport)); break;
                }
                V35.FloorGuarded = false;
                GUI.enabled = true;
                GUILayout.EndScrollView();
                MoveScrolledAnchors(scrolledFrom, GUILayoutUtility.GetLastRect(), scroll);
                GUILayout.EndVertical();
                if (!DeskProvenance.On) { DrawSlips(_lawsSlipBook, GUILayoutUtility.GetLastRect()); }
                return;
            }

            // §42: the Laws tab - the composition's list of the laws in force and before Parliament, then the statute book as built (kept, asked) in the
            // height left. Not wrapped in `GUI.enabled = !_isGameOver`: browsing a law is informational; only the enact/repeal action is gated.
            float listWidth = StatsContentWidth(availableWidth);
            V35.FloorGuarded = true;
            float listHeight = DrawLawsInForceCard(listWidth);
            DrawLawsSectionHead("The statute book", "laws:book", listWidth);
            V35.FloorGuarded = false;
            _lawsSlipBook.Anchors["laws:book"] = new SlipContent("THE STATUTE BOOK")
                .Add("EVERY LAW THE GAME HOLDS, BY MAGNITUDE OR NAME OR COST, WITH ITS COST AND THE COUNT IT WOULD MEET - ENACTED AND REPEALED FROM HERE")
                .Add("KEPT AS BUILT UNDER THE NEW PAGE - THE COMPOSITION DRAWS THE LIST ABOVE AND NO WAY TO ENACT A LAW");
            DrawLawsTab(Mathf.Max(0f, bodyHeight - listHeight - V35.Px(32f)), availableWidth);
            GUILayout.EndVertical();
            if (!DeskProvenance.On) { DrawSlips(_lawsSlipBook, GUILayoutUtility.GetLastRect()); }
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
                    Census = hasWage ? "A STATUTORY MINIMUM AS A SHARE OF THE MEDIAN WAGE · % OF MEDIAN" + MinimumWageInForce(c, GetMinimumWageInput(c.MinimumWagePercentOfMedianBase)) : "OFF · NO STATUTORY MINIMUM - WAGES ARE SET BY COLLECTIVE BARGAINING; THE MODEL HOLDS NO ACT TO INTRODUCE ONE",
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
        // Crime & justice
        // =============================================================================================================================================

        /// <summary>
        /// §738 (UI v3.5, the composition's Laws › Crime & justice): *What these dials move* and the six dials as tiles - <b>set by law</b>. Elias's ruling on
        /// "the sliders' fate" (2026-08-24, CLAUDE.md's law-system section) stands over the composition, which draws three of them draggable: the six move
        /// only as the Laws tab enacts and repeals the laws that move them, so a tile has no knob and emits no control (§564's row, now a tile). The standalone
        /// `CrimeJusticePolicyBill` and its pending state stay in code, unoffered - not a rip-out (the 2026-08-24 conversion's own note).
        ///
        /// <para><b>The faces</b> (`V35_ANSWERS.md` §1, Crime, under the main session's dial rule): Sentencing, Drug possession and Border checks by name
        /// (<see cref="DialStops"/> - Design's four stops for the last two; the composition's months are not computed); police and court funding as
        /// <b>their cost a year against today's</b> - the money the model lands on the justice line (`SimulationManager.ApplyEnforcementCostPressure`); bail
        /// by name. The composition draws the first three; the other three are kept where they stood, the question of where they go asked.</para>
        ///
        /// <para><b>Folded</b>: the four history graphs (crime, organised crime, corruption, incarceration) into the readings' tiles and their sparklines -
        /// the Policy Web's own four, corruption and approval on the head's slip with the rest - and the crime index's published bulletin into the crime
        /// reading's slip.</para>
        /// </summary>
        private void DrawCrimeV35(float width)
        {
            Country c = _playerCountry;
            DrawLawsReadings(width, UiPalette.SystemArea.CrimeJustice);

            DrawLawsSectionHead("Crime dials · set by law", "laws:crime", width);
            _lawsSlipBook.Anchors["laws:crime"] = new SlipContent("CRIME DIALS · SET BY LAW")
                .Add("THE LAWS TAB ENACTS AND REPEALS THE LAWS THAT MOVE THEM · NO BILL MOVES THEM HERE")
                .Add("ON EACH TRACK THE PALE TICK IS THE COUNTRY'S STATUS QUO; EACH LAW IN FORCE IS A TICK WHERE IT LEFT THE DIAL")
                .Add("A DIAL THE MODEL HOLDS AS AN INDEX IS SHOWN BY NAME - ITS BANDS' EDGES DECLARED: AUTHORED FOR THE GAME, NOT MEASURED")
                .Add("POLICE AND COURTS SHOW THEIR COST A YEAR AGAINST TODAY'S - THE MONEY THE MODEL SPENDS ON THE JUSTICE LINE");
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(width, 6), rowHeight = BudgetDialTileHeight(false);
            UiPalette.SystemArea area = UiPalette.SystemArea.CrimeJustice;

            Rect row = LawsDialRow(width, rowHeight);
            float prisons = c.State.PrisonPopulationRate, prisonsBase = c.BaselinePrisonPopulationRate;
            float prisonCost = c.State.NominalGdp * CrimeJusticeCouplings.IncarcerationCostGdpPerCapitaPerInmate * (prisons - prisonsBase) / 100000f;
            DrawLawSetDialTile("Sentencing Severity", c.SentencingSeverity, new Rect(row.x, row.y, tileWidth, rowHeight),
                NamedFace("gavel", DialStops.Sentencing, "0 LENIENT … 100 HARSH · THE MODEL COMPUTES NO PRISON TERM - IT MOVES THE INCARCERATION RATE, AND THE PRISONS FILL OVER YEARS", area),
                law => law.SentencingSeverityDelta,
                "INCARCERATION " + UiFormat.Number(prisons, 0) + " PER 100 000 · THE COUNTRY'S OWN " + UiFormat.Number(prisonsBase, 0) + " · ITS COST ON THE JUSTICE LINE "
                    + CostAYear(prisonCost).ToUpperInvariant());
            DrawLawSetDialTile("Drug Policy", c.DrugPolicyLevel, new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight),
                NamedFace("pill", DialStops.DrugPolicy, "0 DECRIMINALISED … 100 STRICT · THE MODEL'S 0 IS DECRIMINALISED, NOT LEGAL", area),
                law => law.DrugPolicyDelta);
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            float borderCost = EnforcementCost(CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint, c.BorderEnforcementLevel);
            DrawLawSetDialTile("Border Enforcement", c.BorderEnforcementLevel, new Rect(row.x, row.y, tileWidth, rowHeight),
                NamedFace("gate", DialStops.Border, "0 OPEN … 100 STRICT", area),
                law => law.BorderEnforcementDelta,
                "ITS COST " + CostAYear(borderCost).ToUpperInvariant() + " - " + UiFormat.Number(CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint, 3)
                    + " % OF GDP A POINT FROM 50, THE SIZE DECLARED");
            DrawLawSetDialTile("Police Funding", c.PoliceFundingLevel, new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight),
                MoneyFace("shield", "Police funding", CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint, area),
                law => law.PoliceFundingDelta);
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            DrawLawSetDialTile("Judicial Funding", c.JudicialFundingLevel, new Rect(row.x, row.y, tileWidth, rowHeight),
                MoneyFace("scales", "Court funding", CrimeJusticeCouplings.JudicialFundingBudgetCostPercentOfGdpPerPoint, area),
                law => law.JudicialFundingDelta);
            DrawLawSetDialTile("Bail Reform", c.BailReformLevel, new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight),
                NamedFace("key", DialStops.Bail, "0 TRADITIONAL CASH BAIL … 100 FULL REFORM", area),
                law => law.BailReformDelta);
            GUILayout.Space(gutter);
        }

        /// <summary>A funding dial's money, in the book's billions a year: the share of nominal GDP a point from the neutral 50 the model lands on its line
        /// (`SimulationManager.ApplyEnforcementCostPressure` - zero at 50, the seed's apparatus already inside the lines).</summary>
        private float EnforcementCost(float percentOfGdpPerPoint, float level) =>
            _playerCountry.State.NominalGdp / 100f * percentOfGdpPerPoint * (level - CrimeJusticeCouplings.NeutralDialLevel);

        /// <summary>A cost a year as a figure: signed, or *Today's level* where there is none to state.</summary>
        private static string CostAYear(float billions) => Mathf.Abs(billions) < 0.0005f ? "Today's level" : UiFormat.MoneyDelta(billions, MoneyUnit.Billions) + " a year";

        /// <summary>§738: a funding dial's face - the cost a year against today's as the figure, the cost at each end as the ends.</summary>
        private V35DialFace MoneyFace(string icon, string title, float percentOfGdpPerPoint, UiPalette.SystemArea area) => new V35DialFace
        {
            Icon = icon, Title = title, Area = area,
            Figure = v => CostAYear(EnforcementCost(percentOfGdpPerPoint, v)),
            EndLeft = UiFormat.MoneyDelta(EnforcementCost(percentOfGdpPerPoint, MinPolicyDialLevel), MoneyUnit.Billions),
            EndRight = UiFormat.MoneyDelta(EnforcementCost(percentOfGdpPerPoint, MaxPolicyDialLevel), MoneyUnit.Billions),
            Census = "ITS COST A YEAR AGAINST TODAY'S, ON THE JUSTICE LINE - " + UiFormat.Number(percentOfGdpPerPoint, 3) + " % OF GDP A POINT FROM 50 · THE SIZE DECLARED: AUTHORED FOR THE GAME INSIDE A BAND OF REAL SPENDING",
        };

        /// <summary>
        /// §738 (UI v3.5): A DIAL SET BY LAW, as a tile - §564's row in the tile's grammar: the icon, the figure (the face's reading of the level), the name,
        /// the track with the neutral level as the pale tick and one statute tick per law at the level the dial reached after it, in order of enactment; the
        /// ends. No knob and no control: the dial moves only as laws are enacted and repealed. The laws in force, each with its move, are the slip's.
        /// </summary>
        private void DrawLawSetDialTile(string name, float level, Rect tile, V35DialFace face, System.Func<LawDefinition, float> deltaOf, string note = null)
        {
            var laws = new List<string>();
            var stops = new List<float>();
            float running = CrimeJusticeCouplings.NeutralDialLevel;
            foreach (EnactedLaw enacted in _playerCountry.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null) { continue; }
                float delta = deltaOf(law);
                if (Mathf.Approximately(delta, 0f)) { continue; }
                running = Mathf.Clamp(running + delta, MinPolicyDialLevel, MaxPolicyDialLevel);
                laws.Add(law.Name.ToUpperInvariant() + " " + (delta > 0f ? "+" : "−") + UiFormat.Number(Mathf.Abs(delta), 0));
                stops.Add(running);
            }

            Rect inner = DrawBudgetTileCard(tile, false, false);
            Rect figureRect = DrawBudgetTileHead(inner, face.Icon, UiPalette.GetAreaColor(face.Area), face.Figure(level), PoliSimTheme.TextPrimary, face.Title, PoliSimTheme.TextPrimary,
                new Rect(inner.xMax, inner.y, 0f, 0f));
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, figureRect.yMax - inner.y + V35.Px(20f)), "dial:" + name);
            Rect track = BudgetTrackRect(inner, out Rect endLane);
            float scale = LedgerRow.ScaleOf(_labelStyle);
            LedgerRow.Track(track, name, level, level, MinPolicyDialLevel, MaxPolicyDialLevel, true, _sliderStyle, _sliderThumbStyle, scale,
                ghost: CrimeJusticeCouplings.NeutralDialLevel, knob: false);
            if (Event.current.type == EventType.Repaint)
            {
                foreach (float stop in stops)
                {
                    float x = track.x + track.width * Mathf.InverseLerp(MinPolicyDialLevel, MaxPolicyDialLevel, stop);
                    PoliSimTheme.Rule(new Rect(Mathf.Round(x - 0.5f * scale), track.y - 2f * scale, Mathf.Max(1f, scale), track.height + 4f * scale), PoliSimTheme.TextMuted);
                }
                DrawBudgetEndLabels(endLane, face.EndLeft, face.EndRight, false);
            }

            var slip = new SlipContent(face.Title.ToUpperInvariant() + " · " + face.Figure(level).ToUpperInvariant());
            if (!string.IsNullOrEmpty(face.Census)) { slip.Add(face.Census); }
            if (!string.IsNullOrEmpty(note)) { slip.Add(note); }
            if (face.Stops != null) { slip.Add("THE STOPS · " + face.Stops.Bands() + " · THE EDGES DECLARED - AUTHORED FOR THE GAME, NOT MEASURED"); }
            slip.Add("THE INDEX " + UiFormat.Number(level, 0) + " OF 100 · 50 IS THE COUNTRY'S STATUS QUO");
            slip.Add(laws.Count == 0 ? "SET BY LAW · NONE IN FORCE" : "SET BY LAW · " + laws.Count + " IN FORCE - " + string.Join(" · ", laws));
            _lawsSlipBook.Anchors["dial:" + name] = slip;
        }

        // =============================================================================================================================================
        // Sectors
        // =============================================================================================================================================

        /// <summary>§739: the sector whose dials the Sectors tab shows - a click on a sector's tile (UI state, not saved).</summary>
        private SectorType _sectorsV35Selected = SectorType.Manufacturing;

        /// <summary>
        /// §739 (UI v3.5, the composition's Laws › Sectors): <b>Sectors · share of GDP</b> - the eight sectors as tiles, each its output's share of GDP; a click
        /// shows that sector's dials - then <b>its dials</b> as tiles, the bill's action, and <i>What these dials move</i> last. The dials of all eight sectors
        /// are still ONE bill's draft (the selected sector only chooses which five are drawn - five controls on every frame, whichever sector).
        ///
        /// <para><b>The faces</b> (`V35_ANSWERS.md` §1, Sectors): tax credits and research grants by name (<see cref="DialStops"/>; the composition's
        /// "% of R&amp;D wages" and "% of output" are not computed - the credit is a general sector credit) with <b>their cost a year</b> as the figure
        /// (SectorCouplings: zero at 50); regulation as <b>the OECD PMR score</b> - the seed's own mapping inverted (level = 50 × PMR / the OECD average);
        /// ownership by name (no ownership share is computed); the subsidy, which the composition does not draw (asked), kept as its cost a year - and on
        /// the Energy sector as <i>Retail price support</i> by name, the cost a year its figure, as the table sent. The Energy page draws the same five
        /// drafts under its own names until its pass (the one-name question is asked).</para>
        ///
        /// <para><b>Folded</b>: each sector's line (output, employment, its own metric) into its tile and slip; the per-sector cost line and the support
        /// line's routing into the dials' slips; the per-sector effects plate into the tiles - while a draft stands, each sector's tile carries the
        /// preview's change in its output share in the draft's ink, and its slip all three changes (`SectorPreview`, unchanged).</para>
        /// </summary>
        private void DrawSectorsV35(float width)
        {
            Country c = _playerCountry;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors);
            int drafted = SectorDraftChanges();
            PolicyPreview preview = drafted > 0 ? SectorPreview() : null;

            // ---- the sectors ----
            DrawLawsSectionHead("Sectors · share of GDP", "laws:sectors", width);
            float outputSum = 0f, employmentSum = 0f;
            foreach (Sector s in c.Sectors) { outputSum += s.OutputShareOfGdp; employmentSum += s.EmploymentShare; }
            _lawsSlipBook.Anchors["laws:sectors"] = new SlipContent("SECTORS · SHARE OF GDP")
                .Add("EACH SECTOR'S OUTPUT AS A SHARE OF GDP · A CLICK SHOWS ITS DIALS")
                .Add("THE EIGHT HOLD " + UiFormat.Number(outputSum, 1) + " % OF GDP AND " + UiFormat.Number(employmentSum, 1) + " % OF ALL JOBS")
                .Add("READOUTS ONLY - THE DIALS MOVE THEM AND NOTHING ELSE IN THE MODEL READS THEM")
                .Add(drafted > 0 ? "WHILE A DRAFT STANDS, A TILE CARRIES THE CHANGE IT WOULD MAKE TO THE SECTOR'S OUTPUT SHARE, IN THE DRAFT'S INK" : "NO DRAFT STANDS");
            float gutter = V35.Px(V35.Gutter), sectorWidth = V35Span(width, 3);
            var tiles = new List<(Sector Sector, V35TileData Tile)>();
            float sectorHeight = 0f;
            foreach (Sector s in c.Sectors)
            {
                string metric = GetSectorMetricLabel(s.Type);
                var t = new V35TileData { Icon = SectorIcon(s.Type), IconInk = area, Figure = UiFormat.Number(s.OutputShareOfGdp, 1) + "%", Name = DisplayName.Spaced(s.Type.ToString()), FigurePx = V35.FigureSmall };
                var slip = new SlipContent(DisplayName.Spaced(s.Type.ToString()).ToUpperInvariant() + " · " + t.Figure + " OF GDP")
                    .Add("EMPLOYMENT " + UiFormat.Number(s.EmploymentShare, 1) + " % OF ALL JOBS · " + metric.ToUpperInvariant() + " " + UiFormat.Number(s.SectorMetric, 1));
                int moved = SectorDialsMoved(s);
                if (preview != null && preview.SectorDeltas != null && preview.SectorDeltas.TryGetValue(s.Type, out (float Output, float Employment, float Metric) d))
                {
                    if (Mathf.Abs(d.Output) >= 0.005f)
                    {
                        t.Change = (d.Output > 0f ? "▲ " : "▼ ") + UiFormat.Number(Mathf.Abs(d.Output), 2);
                        t.ChangeInk = PoliSimTheme.Caution;
                    }
                    if (Mathf.Abs(d.Output) + Mathf.Abs(d.Employment) + Mathf.Abs(d.Metric) >= 0.0005f)
                    {
                        slip.Add("IF THE DRAFT PASSED · OUTPUT " + SignedFigure(d.Output, 2) + " PP · EMPLOYMENT " + SignedFigure(d.Employment, 2) + " PP · " + metric.ToUpperInvariant() + " " + SignedFigure(d.Metric, 2))
                            .Add("THE PREVIEW: THE WHOLE SECTORS DRAFT APPLIED TO A COPY OF THE COUNTRY FOR A TURN");
                    }
                }
                slip.Add(moved == 0 ? "THE DRAFT MOVES NONE OF ITS DIALS" : "THE DRAFT MOVES " + moved + " OF ITS FIVE DIALS");
                slip.Add(s.Type == _sectorsV35Selected ? "ITS DIALS ARE SHOWN BELOW" : "A CLICK SHOWS ITS DIALS");
                _lawsSlipBook.Anchors["sector:" + s.Type] = slip;
                tiles.Add((s, t));
                sectorHeight = Mathf.Max(sectorHeight, V35TileHeight(t));
            }
            for (int i = 0; i < tiles.Count; i += 4)
            {
                Rect row = GUILayoutUtility.GetRect(width, sectorHeight, GUILayout.Width(width), GUILayout.Height(sectorHeight));
                for (int j = i; j < Mathf.Min(i + 4, tiles.Count); j++)
                {
                    var r = new Rect(row.x + (j - i) * (sectorWidth + gutter), row.y, sectorWidth, sectorHeight);
                    // the tile's control, every frame for every sector (stable control layout); a click only chooses which five dials are drawn
                    if (PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none)) { _sectorsV35Selected = tiles[j].Sector.Type; }
                    DrawV35Tile(r, tiles[j].Tile);
                    if (tiles[j].Sector.Type == _sectorsV35Selected && Event.current.type == EventType.Repaint)
                    {
                        float line = Mathf.Max(1f, V35.Px(2f));
                        PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, line), area);
                        PoliSimTheme.Rule(new Rect(r.x, r.yMax - line, r.width, line), area);
                        PoliSimTheme.Rule(new Rect(r.x, r.y, line, r.height), area);
                        PoliSimTheme.Rule(new Rect(r.xMax - line, r.y, line, r.height), area);
                    }
                    SlipAnchor(r, "sector:" + tiles[j].Sector.Type);
                }
                GUILayout.Space(gutter);
            }

            // ---- the selected sector's dials ----
            Sector sector = null;
            foreach (Sector s in c.Sectors) { if (s.Type == _sectorsV35Selected) { sector = s; break; } }
            if (sector == null && c.Sectors.Count > 0) { sector = c.Sectors[0]; _sectorsV35Selected = sector.Type; }
            if (sector != null) { DrawSectorDialsV35(width, sector); }

            // ---- the bill ----
            SectorPolicyBill pending = _simulationManager.GetPendingSectorBill(PlayerCountryId);
            float gdp = c.State.NominalGdp, standingCost = 0f, draftCost = 0f;
            foreach (Sector s in c.Sectors)
            {
                standingCost += SectorCouplings.SupportCost(gdp, s.SubsidyLevel, s.TaxCreditLevel, s.ResearchGrantsLevel);
                draftCost += SectorCouplings.SupportCost(gdp, GetSectorSubsidyInput(s.Type, s.SubsidyLevel), GetSectorTaxCreditInput(s.Type, s.TaxCreditLevel), GetSectorResearchGrantsInput(s.Type, s.ResearchGrantsLevel));
            }
            float costDelta = draftCost - standingCost;
            string status = pending != null
                ? $"An Economic Sectors bill is before Parliament - resolves in {pending.DaysRemaining} day(s)."
                : "No Economic Sectors bill before Parliament - the dials of all eight sectors are its draft"
                    + (Mathf.Abs(costDelta) >= 0.0005f ? " · it would change sector support by " + UiFormat.MoneyDelta(costDelta, MoneyUnit.Billions) + " a year." : ".");
            DrawLawsBillAction(width, "Introduce sectors bill", "laws:sectorsbill", ParliamentSystem.GetSectorBillConcern(c, BuildSectorBillFromDrafts()), pending != null,
                pending != null ? pending.DaysRemaining : 0, drafted, () => _simulationManager.IntroduceSectorBill(PlayerCountryId, BuildSectorBillFromDrafts()), status);

            DrawLawsReadings(width, UiPalette.SystemArea.Sectors);
        }

        /// <summary>
        /// §739: one sector's five dials as tiles, two to a row, in the composition's order - tax credits, research grants, regulation, ownership - and the
        /// subsidy the composition does not draw, kept fifth. The literal names and the caption keys are the old rows' (`DialLabelCheck`,
        /// `RangeCaptionCheck`, the film's `RowTop(" / Subsidy")`).
        /// </summary>
        private void DrawSectorDialsV35(float width, Sector sector)
        {
            Country c = _playerCountry;
            SectorType type = sector.Type;
            string sectorName = DisplayName.Spaced(type.ToString());
            UiPalette.SystemArea area = UiPalette.SystemArea.Sectors;
            bool energy = type == SectorType.Energy;
            bool energyLine = energy && SectorCouplings.HasEnergyLine(c);
            SpendingLine supportLine = SectorCouplings.SupportLine(c);
            string supportLands = supportLine != null
                ? "SECTOR SUPPORT LANDS ON " + DisplayName.Of(supportLine.Category.ToString()).ToUpperInvariant() + ", OUTSIDE THE LINE'S OWN RANGE"
                : "NO SPENDING LINE IN THIS BUDGET CARRIES SECTOR SUPPORT - ITS COST IS NOT BOOKED";

            DrawLawsSectionHead(sectorName + " dials", "laws:sectordials", width);
            _lawsSlipBook.Anchors["laws:sectordials"] = new SlipContent(sectorName.ToUpperInvariant() + " DIALS")
                .Add("THE DIALS OF ALL EIGHT SECTORS ARE ONE BILL'S DRAFT · A SECTOR'S TILE SHOWS ITS FIVE")
                .Add("SUPPORT - TAX CREDITS, RESEARCH GRANTS, THE SUBSIDY - SHOWS ITS COST A YEAR: ZERO AT 50, THE COUNTRY'S STATUS QUO")
                .Add("A DIAL THE MODEL HOLDS AS AN INDEX IS SHOWN BY NAME - ITS BANDS' EDGES DECLARED: AUTHORED FOR THE GAME, NOT MEASURED")
                .Add(supportLands);
            float gutter = V35.Px(V35.Gutter), tileWidth = V35Span(width, 6), rowHeight = BudgetDialTileHeight(false);
            float gdp = c.State.NominalGdp;

            Rect row = LawsDialRow(width, rowHeight);
            _sectorTaxCreditInputs[type] = DrawDialRow("Tax Credits",
                sector.TaxCreditLevel, GetSectorTaxCreditInput(type, sector.TaxCreditLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null,
                new Rect(row.x, row.y, tileWidth, rowHeight), SupportFace("receipt", DialStops.TaxCredits, v => SectorCouplings.SupportCost(gdp, 50f, v, 50f),
                    "A GENERAL TAX CREDIT TO THE SECTOR, NOT AN R&D CREDIT · " + UiFormat.Number(SectorCouplings.TaxCreditBudgetCostPercentOfGdpPerPoint, 3) + " % OF GDP A POINT FROM 50, THE SIZE DECLARED · " + supportLands),
                captionKey: type + "/Tax Credits");
            _sectorResearchGrantsInputs[type] = DrawDialRow("Research Grants",
                sector.ResearchGrantsLevel, GetSectorResearchGrantsInput(type, sector.ResearchGrantsLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null,
                new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight), SupportFace("book", DialStops.ResearchGrants, v => SectorCouplings.SupportCost(gdp, 50f, 50f, v),
                    "THE MODEL COMPUTES NO SHARE OF OUTPUT · " + UiFormat.Number(SectorCouplings.ResearchGrantsBudgetCostPercentOfGdpPerPoint, 3) + " % OF GDP A POINT FROM 50, THE SIZE DECLARED · " + supportLands),
                captionKey: type + "/Research Grants");
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            float pmrAverage = PmrAverage(type);
            _sectorRegulationInputs[type] = DrawDialRow("Regulation",
                sector.RegulationLevel, GetSectorRegulationInput(type, sector.RegulationLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 light - 100 heavy",
                new Rect(row.x, row.y, tileWidth, rowHeight), new V35DialFace
                {
                    Icon = "scales", Title = "Regulation", Area = area,
                    Figure = v => "PMR " + UiFormat.Number(v * pmrAverage / 50f, 2),
                    EndLeft = energy ? "Liberalised" : "Light", EndRight = energy ? "Regulated" : "Heavy",
                    Census = "THE OECD'S PRODUCT MARKET REGULATION SCORE (0-6, LOWER IS LIGHTER) - THE SEED'S OWN MAPPING INVERTED: THE DIAL IS 50 × PMR / THE OECD AVERAGE · "
                        + "MODERATE AT THE OECD AVERAGE, " + UiFormat.Number(pmrAverage, 2) + (PmrSectorSeries(type) ? " FOR THE SECTOR'S OWN SERIES" : " ECONOMY-WIDE")
                        + " · LIGHT BELOW IT, HEAVY ABOVE · THE SEED HELD THE DIAL INSIDE 10-90",
                }, captionKey: type + "/Regulation");
            _sectorDeregulationInputs[type] = DrawDialRow("Nationalization / Deregulation",   // P3-C3: one axis, both ends in the trailing's order
                sector.DeregulationNationalizationLevel, GetSectorDeregulationInput(type, sector.DeregulationNationalizationLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 nationalized - 100 deregulated",
                new Rect(row.x + tileWidth + gutter, row.y, tileWidth, rowHeight), NamedFace("key", DialStops.Ownership,
                    "0 NATIONALISED … 100 DEREGULATED · THE MODEL COMPUTES NO OWNERSHIP SHARE", area),
                captionKey: type + "/Deregulation");
            GUILayout.Space(gutter);

            row = LawsDialRow(width, rowHeight);
            string subsidyLands = !energy ? supportLands
                : !energyLine ? "NO ENERGY LINE IN THIS BUDGET: THE SUBSIDY'S COST LANDS WITH THE OTHER SECTORS' SUPPORT"
                : EnergyLedger.HasPolicyLevy(c.Id)
                    ? (c.AppliedEnergySupportCost < 0f
                        ? "THE ENERGY LINE CARRIES " + UiFormat.Money(c.AppliedEnergySupportCost, MoneyUnit.Billions) + " A YEAR OF IT - SUPPORT GIVEN BACK, WHICH THE POLICY LEVY TAKES UP ONE FOR ONE"
                        : "THE ENERGY LINE CARRIES " + UiFormat.Money(c.AppliedEnergySupportCost, MoneyUnit.Billions) + " A YEAR OF IT, DISPLACING THE POLICY LEVY ONE FOR ONE UNTIL NONE IS LEFT")
                    : "THE ENERGY LINE CARRIES " + UiFormat.Money(c.AppliedEnergySupportCost, MoneyUnit.Billions) + " A YEAR OF IT - NO POLICY LEVY IN THE RETAIL PRICE TO DISPLACE, SO NO RETAIL EFFECT";
            string subsidyCensus = UiFormat.Number(SectorCouplings.SubsidyBudgetCostPercentOfGdpPerPoint, 3) + " % OF GDP A POINT FROM 50, THE SIZE DECLARED · " + subsidyLands;
            System.Func<float, float> subsidyCost = v => SectorCouplings.SupportCost(gdp, v, 50f, 50f);
            _sectorSubsidyInputs[type] = DrawDialRow("Subsidy",
                sector.SubsidyLevel, GetSectorSubsidyInput(type, sector.SubsidyLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null,
                new Rect(row.x, row.y, tileWidth, rowHeight), energy
                    ? SupportFace("coins", DialStops.RetailSupport, subsidyCost, "RETAIL INTERVENTION'S MONEY SIDE · " + subsidyCensus)
                    : new V35DialFace
                    {
                        Icon = "coins", Title = "Subsidy", Area = area,
                        Figure = v => CostAYear(subsidyCost(v)),
                        EndLeft = UiFormat.MoneyDelta(subsidyCost(MinPolicyDialLevel), MoneyUnit.Billions), EndRight = UiFormat.MoneyDelta(subsidyCost(MaxPolicyDialLevel), MoneyUnit.Billions),
                        Census = "ITS COST A YEAR · " + subsidyCensus,
                    },
                captionKey: type + "/Subsidy");
            GUILayout.Space(gutter);
        }

        /// <summary>§739: a support dial's face - the stops' names at the ends and in the slip, the dial's cost a year as the figure.</summary>
        private static V35DialFace SupportFace(string icon, DialStops.Dial stops, System.Func<float, float> cost, string census) => new V35DialFace
        {
            Icon = icon, Title = stops.Title, Stops = stops, Area = UiPalette.SystemArea.Sectors,
            Figure = v => CostAYear(cost(v)),
            EndLeft = stops.Stops[0].Name, EndRight = stops.Stops[stops.Stops.Length - 1].Name,
            Census = "ITS COST A YEAR, ZERO AT 50 · " + census,
        };

        /// <summary>
        /// §739: the OECD average the regulation seed divided by (`WorldFactory`'s regulation seed slots, R-C4): the economy-wide PMR's published "OECD
        /// average" row, 1.3464 (PMR-Indicator_Econwide_2023-24-and-2018_02.02.2026.xlsx, oecd.org, retrieved 2026-08-28), and the 38-member simple means
        /// of the three sector series that override it - ENERGY 1.3134, ECOMM 1.3056, RETAIL_TRADE 1.0409 (OECD.ECO.GCRD, DSD_PMR@DF_PMR 1.3, 2023). A
        /// display scale: the model holds the dial, and the score is the dial read back through the seed's own mapping.
        /// </summary>
        private static float PmrAverage(SectorType type)
        {
            switch (type)
            {
                case SectorType.Energy: return 1.3134f;
                case SectorType.Telecommunications: return 1.3056f;
                case SectorType.Retail: return 1.0409f;
                default: return 1.3464f;
            }
        }

        /// <summary>Whether a sector's regulation was seeded from its own PMR series rather than the economy-wide score.</summary>
        private static bool PmrSectorSeries(SectorType type) => type == SectorType.Energy || type == SectorType.Telecommunications || type == SectorType.Retail;

        /// <summary>A sector's v3.5 icon.</summary>
        private static string SectorIcon(SectorType type)
        {
            switch (type)
            {
                case SectorType.Manufacturing: return "factory";
                case SectorType.Technology: return "gear";
                case SectorType.Agriculture: return "wheat";
                case SectorType.Finance: return "bank";
                case SectorType.Energy: return "bolt";
                case SectorType.Construction: return "town";
                case SectorType.Retail: return "tag";
                case SectorType.Telecommunications: return "antenna";
                default: return "sectors";
            }
        }

        private static string SignedFigure(float v, int decimals) => (v > 0f ? "+" : v < 0f ? "−" : "±") + UiFormat.Number(Mathf.Abs(v), decimals);

        /// <summary>How many of one sector's five dials the draft moves off their standing levels.</summary>
        private int SectorDialsMoved(Sector s)
        {
            int n = 0;
            if (!Mathf.Approximately(GetSectorSubsidyInput(s.Type, s.SubsidyLevel), s.SubsidyLevel)) { n++; }
            if (!Mathf.Approximately(GetSectorRegulationInput(s.Type, s.RegulationLevel), s.RegulationLevel)) { n++; }
            if (!Mathf.Approximately(GetSectorTaxCreditInput(s.Type, s.TaxCreditLevel), s.TaxCreditLevel)) { n++; }
            if (!Mathf.Approximately(GetSectorResearchGrantsInput(s.Type, s.ResearchGrantsLevel), s.ResearchGrantsLevel)) { n++; }
            if (!Mathf.Approximately(GetSectorDeregulationInput(s.Type, s.DeregulationNationalizationLevel), s.DeregulationNationalizationLevel)) { n++; }
            return n;
        }

        /// <summary>How many dials, across all eight sectors, the draft moves.</summary>
        private int SectorDraftChanges()
        {
            int n = 0;
            foreach (Sector s in _playerCountry.Sectors) { n += SectorDialsMoved(s); }
            return n;
        }

        // =============================================================================================================================================
        // Trade
        // =============================================================================================================================================

        /// <summary>
        /// §741 (UI v3.5, the composition's Laws › Trade): <b>Trade with the five</b> - exports and imports as shares of GDP and the balance with its
        /// history, as tiles - then <b>Partners and tariffs</b>: each partner's share of the trade (Statistics' card, the same figures) beside the tariff -
        /// for the five EU members the <b>average tariff charged</b> (a reading: an EU member sets no tariff of its own, so the base rate the model holds
        /// is never charged and draws no dial - `V35_ANSWERS.md` §1, Trade), for the USA the base tariff's dial - and then <b>the partners' override
        /// rates</b>, the working lever, which the composition does not draw (kept, asked), each a dial tile with its Set override / Reset draft action;
        /// the bill's action; <i>What these dials move</i> last.
        ///
        /// <para><b>What the model holds</b>: trade with the five other states it models - Eurostat's flows, goods and services, in the book's dollars
        /// (`TradeMatrixTable`) - and nothing with the rest of the world; the tiles say <i>with the five</i>. The composition's export and import shares
        /// of all trade are not computed. Exports and imports are the flows before tariffs; the balance is after the partners' tariffs and the
        /// currency (`TradeSystem.ApplyTradeEffects`).</para>
        /// </summary>
        private void DrawTradeV35(float width)
        {
            Country c = _playerCountry;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Trade);
            float gdp = c.State.NominalGdp;
            float gutter = V35.Px(V35.Gutter);

            // ---- trade with the five ----
            float exports = 0f, imports = 0f;
            foreach (TradePartner link in c.TradePartners) { exports += Mathf.Max(0f, link.ExportVolume); imports += Mathf.Max(0f, link.ImportVolume); }
            DrawLawsSectionHead("Trade with the five", "laws:trade", width);
            _lawsSlipBook.Anchors["laws:trade"] = new SlipContent("TRADE WITH THE FIVE")
                .Add("THE MODEL HOLDS TRADE WITH THE FIVE OTHER STATES IT MODELS - EUROSTAT'S FLOWS, GOODS AND SERVICES, IN THE BOOK'S DOLLARS · THE REST OF THE WORLD'S IS NOT MODELLED")
                .Add("EXPORTS AND IMPORTS ARE THE FLOWS BEFORE TARIFFS; THE BALANCE IS AFTER THE PARTNERS' TARIFFS AND THE CURRENCY");
            var exportTile = new V35TileData { Icon = "out", IconInk = area, Figure = gdp > 0f ? UiFormat.Number(100f * exports / gdp, 1) + "%" : null, Glyph = gdp > 0f ? (Symbol?)null : Symbol.Absent, Name = "Exports to the five", FigurePx = V35.FigureSmall };
            var importTile = new V35TileData { Icon = "in", IconInk = area, Figure = gdp > 0f ? UiFormat.Number(100f * imports / gdp, 1) + "%" : null, Glyph = gdp > 0f ? (Symbol?)null : Symbol.Absent, Name = "Imports from the five", FigurePx = V35.FigureSmall };
            IReadOnlyList<float> balanceHistory = c.History.TradeBalance.Quarterly;
            var balanceTile = new V35TileData { Icon = "trade", IconInk = area, Figure = UiFormat.MoneyDelta(c.State.TradeBalance, MoneyUnit.Billions), Name = "Trade balance with the five", FigurePx = V35.FigureSmall, Spark = balanceHistory };
            var balanceSlip = new SlipContent("TRADE BALANCE WITH THE FIVE · " + balanceTile.Figure.ToUpperInvariant() + " A YEAR")
                .Add("EXPORTS AFTER THE PARTNERS' TARIFFS AND THE CURRENCY, LESS IMPORTS");
            if (balanceHistory != null && balanceHistory.Count >= 5)
            {
                float d = balanceHistory[balanceHistory.Count - 1] - balanceHistory[balanceHistory.Count - 5];
                if (Mathf.Abs(d) >= 0.005f)
                {
                    balanceTile.Change = (d > 0f ? "▲ " : "▼ ") + UiFormat.Money(Mathf.Abs(d), MoneyUnit.Billions);
                    balanceTile.ChangeInk = V35.DirectionNeutral;   // V35 rule 5: the trade balance prints direction only
                    balanceSlip.Add((d > 0f ? "UP " : "DOWN ") + UiFormat.Money(Mathf.Abs(d), MoneyUnit.Billions) + " OVER THE LAST FOUR QUARTERS");
                }
            }
            balanceSlip.Add("NEUTRAL INK - A SURPLUS IS NOT A GOOD BY ITSELF, NOR A DEFICIT A BAD");
            _lawsSlipBook.Anchors["trade:exports"] = new SlipContent("EXPORTS TO THE FIVE · " + (exportTile.Figure ?? "ABSENT") + " OF GDP")
                .Add(UiFormat.Money(exports, MoneyUnit.Billions).ToUpperInvariant() + " A YEAR, BEFORE THE PARTNERS' TARIFFS");
            _lawsSlipBook.Anchors["trade:imports"] = new SlipContent("IMPORTS FROM THE FIVE · " + (importTile.Figure ?? "ABSENT") + " OF GDP")
                .Add(UiFormat.Money(imports, MoneyUnit.Billions).ToUpperInvariant() + " A YEAR, BEFORE OUR TARIFFS");
            _lawsSlipBook.Anchors["trade:balance"] = balanceSlip;
            float small = V35Span(width, 3), wide = V35Span(width, 6);
            float tileH = Mathf.Max(V35TileHeight(exportTile), Mathf.Max(V35TileHeight(importTile), V35TileHeight(balanceTile)));
            Rect row = GUILayoutUtility.GetRect(width, tileH, GUILayout.Width(width), GUILayout.Height(tileH));
            var exportRect = new Rect(row.x, row.y, small, tileH);
            var importRect = new Rect(row.x + small + gutter, row.y, small, tileH);
            var balanceRect = new Rect(row.x + (small + gutter) * 2f, row.y, row.xMax - (row.x + (small + gutter) * 2f), tileH);
            DrawV35Tile(exportRect, exportTile); SlipAnchor(exportRect, "trade:exports");
            DrawV35Tile(importRect, importTile); SlipAnchor(importRect, "trade:imports");
            DrawV35Tile(balanceRect, balanceTile); SlipAnchor(balanceRect, "trade:balance");
            GUILayout.Space(gutter);

            // ---- partners and tariffs ----
            bool blocMember = _world.TradeBlocs.Exists(bloc => bloc.IsMember(PlayerCountryId));
            DrawLawsSectionHead("Partners and tariffs", "laws:partners", width);
            _lawsSlipBook.Anchors["laws:partners"] = new SlipContent("PARTNERS AND TARIFFS")
                .Add("EACH PARTNER'S SHARE OF OUR TRADE WITH THE FIVE - EXPORTS AND IMPORTS TOGETHER")
                .Add(blocMember
                    ? "AN EU MEMBER SETS NO TARIFF OF ITS OWN: THE BLOC'S RATES APPLY TO EVERY PARTNER, SO THE BASE RATE IS NEVER CHARGED - THE TILE IS THE AVERAGE ACTUALLY CHARGED"
                    : "THE BASE TARIFF APPLIES TO EVERY PARTNER WITH NO OVERRIDE")
                .Add("AN OVERRIDE ON A PARTNER'S IMPORTS BEATS THE BLOC AND BASE RATES FOR THAT PARTNER; THE PARTNER MIRRORS THE EXCESS ONTO OUR EXPORTS FROM THE NEXT BOUNDARY");
            var partners = new List<(string Name, string Code, float Value, bool Absent, bool Own)>();
            float tradeSum = 0f;
            foreach (TradePartner p in c.TradePartners) { tradeSum += Mathf.Max(0f, p.ExportVolume) + Mathf.Max(0f, p.ImportVolume); }
            var shareSlip = new SlipContent("TRADE PARTNERS · SHARE OF OUR TRADE WITH THE FIVE");
            foreach (TradePartner p in c.TradePartners)
            {
                Country pc = _world.GetCountry(p.PartnerId);
                if (pc == null) { continue; }
                float share = tradeSum > 0f ? 100f * (Mathf.Max(0f, p.ExportVolume) + Mathf.Max(0f, p.ImportVolume)) / tradeSum : 0f;
                partners.Add((pc.Name, MapRenderer.TagOf(pc.Id), share, false, false));
                shareSlip.Add(pc.Name.ToUpperInvariant() + " " + UiFormat.Number(share, 1) + " % · EXPORTS " + UiFormat.Money(p.ExportVolume, MoneyUnit.Billions).ToUpperInvariant()
                    + " · IMPORTS " + UiFormat.Money(p.ImportVolume, MoneyUnit.Billions).ToUpperInvariant() + " A YEAR");
            }
            partners.Sort((a, b) => b.Value.CompareTo(a.Value));
            _lawsSlipBook.Anchors["trade:partners"] = shareSlip;

            // the average charged: what we charge each partner on its imports, weighted by them - standing, and with the draft's override rates
            float weight = 0f, standingSum = 0f, draftSum = 0f;
            var chargedLines = new List<string>();
            foreach (TradePartner link in c.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null) { continue; }
                float charged = TradeSystem.GetTariffRate(c, partner, _world.TradeBlocs);
                float drafted = link.HasPlayerTariffOverride ? GetPartnerTariffInput(link.PartnerId, link.PlayerTariffOverride)
                    : TradeSystem.GetStandingTariffRate(c, partner, _world.TradeBlocs, GetTariffRateInput(c.BaseTariffRate));
                float w = Mathf.Max(0f, link.ImportVolume);
                weight += w; standingSum += w * charged; draftSum += w * drafted;
                chargedLines.Add(partner.Name.ToUpperInvariant() + " " + UiFormat.Number(charged, 2) + " %" + (link.HasPlayerTariffOverride ? " · OUR OVERRIDE" : string.Empty));
            }
            float average = weight > 0f ? standingSum / weight : 0f, averageDraft = weight > 0f ? draftSum / weight : 0f;

            float cardH = StatsBarCardHeight(partners.Count);
            Rect band = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            var cardRect = new Rect(band.x, band.y, wide, cardH);
            DrawStatsBarCard(cardRect, "trade", "Trade partners", partners, "% of trade", "trade:partners");
            SlipAnchor(cardRect, "trade:partners");   // StatsAnchor registers on the Statistics page only
            var tariffRect = new Rect(band.x + wide + gutter, band.y, band.xMax - (band.x + wide + gutter), 0f);
            if (blocMember)
            {
                bool draftMoves = Mathf.Abs(averageDraft - average) >= 0.005f;
                var avgTile = new V35TileData { Icon = "gate", IconInk = area, Figure = UiFormat.Number(average, 2) + "%", Name = "Average tariff charged", FigurePx = V35.FigureSmall };
                if (draftMoves) { avgTile.Change = "→ " + UiFormat.Number(averageDraft, 2) + "%"; avgTile.ChangeInk = PoliSimTheme.Caution; }
                tariffRect.height = V35TileHeight(avgTile);
                DrawV35Tile(tariffRect, avgTile);
                SlipAnchor(tariffRect, "trade:average");
                var avgSlip = new SlipContent("AVERAGE TARIFF CHARGED · " + avgTile.Figure)
                    .Add("WHAT WE CHARGE EACH PARTNER ON ITS GOODS, WEIGHTED BY OUR IMPORTS FROM IT")
                    .Add("SET BY THE EU'S BLOC RATES - AN EU MEMBER HAS NO TARIFF OF ITS OWN; THE BASE RATE THE MODEL HOLDS IS NEVER CHARGED, SO NO DIAL IS DRAWN FOR IT");
                foreach (string line in chargedLines) { avgSlip.Add(line); }
                if (draftMoves) { avgSlip.Add("WITH THE DRAFT'S OVERRIDE RATES · " + UiFormat.Number(averageDraft, 2) + " %"); }
                _lawsSlipBook.Anchors["trade:average"] = avgSlip;
            }
            else
            {
                tariffRect.height = BudgetDialTileHeight(false);
                _tariffRateInput = DrawDialRow("General Base Tariff",
                    c.BaseTariffRate, GetTariffRateInput(c.BaseTariffRate),
                    MinBaseTariffRate, MaxBaseTariffRate, "F2", "%", $"{MinBaseTariffRate:F0}-{MaxBaseTariffRate:F0}% range",
                    tariffRect, new V35DialFace
                    {
                        Icon = "gate", Title = "Base tariff", Area = UiPalette.SystemArea.Trade,
                        Figure = v => UiFormat.Number(v, 1) + "%",
                        EndLeft = "Free trade · " + UiFormat.Number(MinBaseTariffRate, 0) + "%", EndRight = "Protection · " + UiFormat.Number(MaxBaseTariffRate, 0) + "%",
                        Census = "THE RATE ON EVERY PARTNER WITH NO OVERRIDE · THE AVERAGE CHARGED TODAY, WEIGHTED BY OUR IMPORTS: " + UiFormat.Number(average, 2) + " % - " + string.Join(" · ", chargedLines),
                    });
            }
            GUILayout.Space(gutter);

            // ---- the partners' override rates (not in the composition; kept, asked) ----
            DrawLawsSectionHead("Override rates", "laws:overrides", width);
            _lawsSlipBook.Anchors["laws:overrides"] = new SlipContent("OVERRIDE RATES")
                .Add("A RATE OF OUR OWN ON ONE PARTNER'S GOODS - IT BEATS THE BLOC AND BASE RATES FOR THAT PARTNER, AND THE PARTNER MIRRORS THE EXCESS ONTO OUR EXPORTS")
                .Add("SET ONE AND IT STARTS AT TODAY'S RATE, CHANGING NOTHING; ITS RATE MOVES ONLY THROUGH THE TRADE BILL · RESET RETURNS A DRAFT TO THE STANDING OVERRIDE");
            float tileWidth = V35Span(width, 6), dialH = BudgetDialTileHeight(false);
            bool mayAct = _simulationManager.PlayerMayIntroduce(PlayerCountryId, CabinetPortfolio.ForeignAffairs, out string lockedBecause);
            int index = 0;
            Rect pairRow = default;
            foreach (TradePartner link in c.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null) { continue; }
                if (index % 2 == 0) { pairRow = LawsDialRow(width, dialH); }
                var tile = new Rect(pairRow.x + (index % 2) * (tileWidth + gutter), pairRow.y, tileWidth, dialH);
                DrawTradePartnerTile(link, partner, tile, index == 0, mayAct, lockedBecause);
                if (index % 2 == 1) { GUILayout.Space(gutter); }
                index++;
            }
            if (index % 2 == 1) { GUILayout.Space(gutter); }

            // ---- the bill ----
            TradePolicyBill pending = _simulationManager.GetPendingTradeBill(PlayerCountryId);
            int changes = TradeDraftChanges(blocMember);
            string status = pending != null
                ? $"A Trade bill is before Parliament - resolves in {pending.DaysRemaining} day(s)."
                : "No Trade bill before Parliament - " + (blocMember ? "the override rates are its draft" : "the base rate and the override rates are its draft");
            if (pending == null && changes > 0)
            {
                // pass 6: the draft's cost at the next boundary, the real functions on throwaway clones (SimulationManager.EstimateTradeBill)
                TradeBillEstimate estimate = _simulationManager.EstimateTradeBill(PlayerCountryId, BuildTradeBillFromDrafts());
                status += " · at these rates the tariff take is " + UiFormat.Money(estimate.Take, MoneyUnit.Billions) + " a year (" + UiFormat.MoneyDelta(estimate.TakeDelta, MoneyUnit.Billions)
                    + " on today), the trade balance moves " + UiFormat.MoneyDelta(estimate.TradeBalanceDelta, MoneyUnit.Billions) + " a year as the partners mirror it, and prices "
                    + StatsReadings.TrueMinus(estimate.PassThroughPp.ToString("+0.00;-0.00", CultureInfo.InvariantCulture)) + " pp this year";
            }
            else if (pending == null) { status += "."; }
            DrawLawsBillAction(width, "Introduce trade bill", "laws:tradebill", ParliamentSystem.GetTradeBillConcern(c, BuildTradeBillFromDrafts(), _world), pending != null,
                pending != null ? pending.DaysRemaining : 0, changes, () => _simulationManager.IntroduceTradeBill(PlayerCountryId, BuildTradeBillFromDrafts()), status, CabinetPortfolio.ForeignAffairs);

            DrawLawsReadings(width, UiPalette.SystemArea.Trade);
        }

        /// <summary>
        /// §741: one partner's override rate as a dial tile - the rate we charge on its goods (our override's draft where one stands; today's bloc or base
        /// rate, the dial disabled, where none does) - with the Set override / Reset draft action at the head's right (the lock's glyph where the role
        /// locks Foreign Affairs' lever). The literal name and the caption key are the old row's (`DialLabelCheck`, `RangeCaptionCheck`); the first
        /// partner keeps the film's geometry key (`RowTop(" / Override rate")`, the draft-reset pair), the others their own. Two controls, always.
        /// </summary>
        private void DrawTradePartnerTile(TradePartner link, Country partner, Rect tile, bool first, bool mayAct, string lockedBecause)
        {
            Country c = _playerCountry;
            float tariffOnOurExports = TradeSystem.GetTariffRate(partner, c, _world.TradeBlocs);
            float tariffOnOurImports = TradeSystem.GetTariffRate(c, partner, _world.TradeBlocs);
            float retaliation = TradeSystem.GetRetaliatoryTariffRate(partner, c, _world.TradeBlocs);
            bool hasOverride = link.HasPlayerTariffOverride;
            float standing = hasOverride ? link.PlayerTariffOverride : tariffOnOurImports;
            string tag = MapRenderer.TagOf(partner.Id);
            string census = (hasOverride
                    ? "OUR OVERRIDE ON " + partner.Name.ToUpperInvariant() + "'S GOODS - ITS RATE MOVES ONLY THROUGH THE TRADE BILL; RESET RETURNS THE DRAFT TO IT"
                    : "NO OVERRIDE - TODAY'S RATE ON " + partner.Name.ToUpperInvariant() + "'S GOODS IS THE BLOC'S OR THE BASE · SET ONE AND IT STARTS HERE, CHANGING NOTHING UNTIL A TRADE BILL MOVES IT")
                + " · THEY CHARGE " + UiFormat.Number(tariffOnOurExports, 2) + " % ON OURS"
                + (retaliation > 0f ? " (" + UiFormat.Number(retaliation, 2) + " OF IT MIRRORS OUR OVERRIDE, FROM THE NEXT BOUNDARY)" : string.Empty);
            float newRate = DrawDialRow("Override rate",
                standing, GetPartnerTariffInput(link.PartnerId, standing),
                PartnerTariffOverrideMin, PartnerTariffOverrideMax, "F2", "%",
                hasOverride ? "via the Trade bill" : "no override set",
                tile, new V35DialFace
                {
                    Icon = "globe", Title = partner.Name + " · " + tag, Area = UiPalette.SystemArea.Trade,
                    Figure = v => UiFormat.Number(v, 2) + "% on their goods",
                    EndLeft = UiFormat.Number(PartnerTariffOverrideMin, 0) + "%", EndRight = UiFormat.Number(PartnerTariffOverrideMax, 0) + "%",
                    Census = census,
                }, hasOverride, captionKey: "Override rate/" + link.PartnerId, geometryKey: first ? null : "Override rate (" + tag + ")");
            if (hasOverride) { _partnerTariffInputs[link.PartnerId] = newRate; }

            // the action, at the head's right
            float padX = V35.Px(V35.CardPadX), padY = V35.Px(V35.CardPadY), h = V35.Px(30f);
            if (!mayAct)
            {
                float side = V35.Px(16f);
                var glyph = new Rect(tile.xMax - padX - side, tile.y + padY + Mathf.Round((h - side) * 0.5f), side, side);
                if (Event.current.type == EventType.Repaint) { DrawStateGlyph(glyph, Symbol.Locked, PoliSimTheme.TextSecondary); }
                SlipAnchor(glyph, "trade:lock");
                _lawsSlipBook.Anchors["trade:lock"] = new SlipContent("LOCKED").Add((lockedBecause ?? string.Empty).ToUpperInvariant());
                return;
            }
            string label = hasOverride ? "Reset draft" : "Set override";
            float w = BudgetButtonWidth(label, null);
            var button = new Rect(tile.xMax - padX - w, tile.y + padY, w, h);
            if (DrawBudgetButton(button, label, null, true))
            {
                if (hasOverride) { ResetPartnerTariffDraft(link.PartnerId); }
                else
                {
                    link.PlayerTariffOverride = Mathf.Clamp(tariffOnOurImports, PartnerTariffOverrideMin, PartnerTariffOverrideMax);
                    RecomputePolicyPreview();
                }
            }
        }

        /// <summary>How many of the Trade bill's terms the draft moves: the base rate (outside a bloc) and each standing override.</summary>
        private int TradeDraftChanges(bool blocMember)
        {
            Country c = _playerCountry;
            int n = !blocMember && !Mathf.Approximately(GetTariffRateInput(c.BaseTariffRate), c.BaseTariffRate) ? 1 : 0;
            foreach (TradePartner link in c.TradePartners)
            {
                if (link.HasPlayerTariffOverride && !Mathf.Approximately(GetPartnerTariffInput(link.PartnerId, link.PlayerTariffOverride), link.PlayerTariffOverride)) { n++; }
            }
            return n;
        }

        // =============================================================================================================================================
        // Laws
        // =============================================================================================================================================

        /// <summary>§742: the list card's own scroll, where more laws stand than its six rows show.</summary>
        private Vector2 _lawsListScroll;

        /// <summary>
        /// §742 (UI v3.5, the composition's Laws › Laws): <b>the laws in force and before Parliament</b> as the composition's list card - each row the
        /// law's icon, its D24 verb (RAISE · LOWER · BAN · ALLOW - `LawVerbs`, §662's pair), its plain name and its state (*In force*, *Before
        /// Parliament · n d*, *Repeal before Parliament · n d*); a click opens it in the statute book below. Returns the height it took.
        ///
        /// <para><b>The statute book stays as built, under it</b> (kept, asked): the composition draws the list and nothing to enact from - the game's
        /// 140 laws, their filters, their cost and count and the enact and repeal actions live in the statute book, and a page without them would take
        /// the player's laws away. The composition marks every law but the electricity tax illustrative.</para>
        /// </summary>
        private float DrawLawsInForceCard(float width)
        {
            Country c = _playerCountry;
            IReadOnlyDictionary<string, LawBill> bills = _simulationManager.GetPendingLawBills(PlayerCountryId);
            var rows = new List<(LawDefinition Law, string State, bool InForce, string Detail)>();
            var listed = new HashSet<string>();
            foreach (EnactedLaw enacted in c.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null || !listed.Add(law.Id)) { continue; }
                bool repealing = bills != null && bills.TryGetValue(law.Id, out LawBill repeal) && repeal.IsRepeal;
                string state = repealing ? "Repeal before Parliament · " + bills[law.Id].DaysRemaining + " d" : "In force";
                rows.Add((law, state, true, "IN FORCE SINCE " + enacted.EnactedOn.ToString("d MMM yyyy").ToUpperInvariant() + (repealing ? " · ITS REPEAL IS BEFORE PARLIAMENT, " + bills[law.Id].DaysRemaining + " DAY(S) TO THE VOTE" : string.Empty)));
            }
            if (bills != null)
            {
                foreach (KeyValuePair<string, LawBill> kv in bills)
                {
                    if (kv.Value.IsRepeal || listed.Contains(kv.Key)) { continue; }
                    LawDefinition law = LawCatalog.GetById(kv.Key);
                    if (law == null || !listed.Add(law.Id)) { continue; }
                    rows.Add((law, "Before Parliament · " + kv.Value.DaysRemaining + " d", false, "A BILL TO ENACT IT IS BEFORE PARLIAMENT · " + kv.Value.DaysRemaining + " DAY(S) TO THE VOTE"));
                }
            }

            DrawLawsSectionHead("Laws", "laws:list", width);
            _lawsSlipBook.Anchors["laws:list"] = new SlipContent("LAWS")
                .Add(rows.Count == 0 ? "NO LAW IS IN FORCE OR BEFORE PARLIAMENT" : rows.Count + (rows.Count == 1 ? " LAW" : " LAWS") + " IN FORCE OR BEFORE PARLIAMENT")
                .Add("EACH ROW: THE LAW'S VERB - RAISE, LOWER, BAN OR ALLOW, READ FROM THE SIGNS OF WHAT IT MOVES - ITS NAME AND ITS STATE")
                .Add("A CLICK OPENS IT IN THE STATUTE BOOK BELOW, WHERE EVERY LAW IS ENACTED AND REPEALED");
            float rowH = V35.Px(V35.ListRow + 7f), pad = V35.Px(V35.CardPadX), padY = V35.Px(8f);
            int visible = Mathf.Clamp(rows.Count, 1, 6);
            float cardH = padY * 2f + visible * rowH;
            Rect card = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            DrawV35Card(card);
            var listRect = new Rect(card.x + pad, card.y + padY, card.width - pad * 2f, cardH - padY * 2f);
            bool repaint = Event.current.type == EventType.Repaint;
            GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle chipFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            GUIStyle chipMuted = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.MiddleCenter);
            GUIStyle wordFace = DeskCaption(6.5f, PoliSimTheme.TextPrimary);
            if (rows.Count == 0)
            {
                if (repaint) { PoliSimWidgets.MeasuredLabel(listRect, "No law is in force or before Parliament - the statute book below enacts them.", V35Serif(V35.Floor, PoliSimTheme.TextMuted)); }
                GUILayout.Space(V35.Px(V35.Gutter));
                return cardH + V35.Px(26f) + V35.Px(6f) + V35.Px(V35.Gutter);
            }

            bool scrolls = rows.Count > visible;
            int scrolledFrom = _slipAnchors.Count;
            float contentW = listRect.width - (scrolls ? GUI.skin.verticalScrollbar.fixedWidth + V35.Px(4f) : 0f);
            if (scrolls) { _lawsListScroll = GUI.BeginScrollView(listRect, _lawsListScroll, new Rect(0f, 0f, contentW, rows.Count * rowH)); }
            float ox = scrolls ? 0f : listRect.x, oy = scrolls ? 0f : listRect.y;
            for (int i = 0; i < rows.Count; i++)
            {
                (LawDefinition law, string state, bool inForce, string detail) = rows[i];
                var row = new Rect(ox, oy + i * rowH, contentW, rowH);
                if (PoliSimWidgets.Button(row, GUIContent.none, GUIStyle.none)) { _pendingSelectedLawId = law.Id; _hasPendingLawSelection = true; }
                Color area = UiPalette.GetAreaColor(LawCategoryArea(law.Category));
                string name = string.IsNullOrEmpty(law.PlainName) ? law.Name : law.PlainName;
                GUIStyle stateFace = inForce && !state.StartsWith("Repeal") ? chipFace : chipMuted;
                float chipW = Mathf.Ceil(stateFace.CalcSize(new GUIContent(state)).x) + V35.Px(16f);
                var chip = new Rect(row.xMax - chipW, row.y + Mathf.Round((rowH - V35.Px(20f)) * 0.5f), chipW, V35.Px(20f));
                if (repaint)
                {
                    if (i > 0) { PoliSimTheme.Rule(new Rect(row.x, row.y, row.width, 1f), V35.ListRule); }
                    float side = V35.Px(18f), glyph = V35.Px(14f);
                    DrawV35Icon(new Rect(row.x, row.y + Mathf.Round((rowH - side) * 0.5f), side, side), LawV35Icon(law, name), area);
                    Symbol? verb = SymbolRegistry.Of(LawVerbs.Of(law));
                    if (verb.HasValue) { SymbolRegistry.Draw(new Rect(row.x + side + V35.Px(10f), row.y + Mathf.Round((rowH - glyph) * 0.5f), glyph, glyph), verb.Value, PoliSimTheme.TextPrimary, wordFace); }
                    float nameX = row.x + side + V35.Px(10f) + glyph + V35.Px(10f);
                    float nameW = Mathf.Max(1f, chip.x - V35.Px(10f) - nameX);
                    PoliSimWidgets.MeasuredLabel(new Rect(nameX, row.y, nameW, rowH), V35Fit(name, nameFace, nameW, out _), nameFace);
                    Color edge = inForce && !state.StartsWith("Repeal") ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;
                    PoliSimTheme.Rule(new Rect(chip.x, chip.y, chip.width, 1f), edge);
                    PoliSimTheme.Rule(new Rect(chip.x, chip.yMax - 1f, chip.width, 1f), edge);
                    PoliSimTheme.Rule(new Rect(chip.x, chip.y, 1f, chip.height), edge);
                    PoliSimTheme.Rule(new Rect(chip.xMax - 1f, chip.y, 1f, chip.height), edge);
                    PoliSimWidgets.MeasuredLabel(chip, state, stateFace);
                }
                SlipAnchor(row, "lawrow:" + law.Id);
                LawVerb v = LawVerbs.Of(law);
                _lawsSlipBook.Anchors["lawrow:" + law.Id] = new SlipContent(name.ToUpperInvariant() + " · " + state.ToUpperInvariant())
                    .Add(law.Name.ToUpperInvariant() + (string.IsNullOrEmpty(law.Citation) ? string.Empty : " · " + law.Citation.ToUpperInvariant()))
                    .Add((v == LawVerb.None ? (LawVerbs.IsBothWays(law) ? "IT MOVES ITS DIALS BOTH WAYS" : "NO VERB - IT MOVES NO DIAL ONE WAY") : "ITS VERB: " + v.ToString().ToUpperInvariant() + " - READ FROM THE SIGNS OF WHAT IT MOVES"))
                    .Add(detail)
                    .Add("A CLICK OPENS IT IN THE STATUTE BOOK BELOW");
            }
            if (scrolls)
            {
                GUI.EndScrollView();
                MoveScrolledAnchors(scrolledFrom, listRect, _lawsListScroll);
            }
            GUILayout.Space(V35.Px(V35.Gutter));
            return cardH + V35.Px(26f) + V35.Px(6f) + V35.Px(V35.Gutter);
        }

        /// <summary>A law's v3.5 icon - by the words of its plain name where one of the set says what it is (the composition's clock, pill, bolt …), else
        /// its category's.</summary>
        private static string LawV35Icon(LawDefinition law, string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("electric")) { return "bolt"; }
            if (n.Contains("hour") || n.Contains("overtime") || n.Contains("working time")) { return "clock"; }
            if (n.Contains("drug")) { return "pill"; }
            if (n.Contains("permit") || n.Contains("immigra") || n.Contains("asylum") || n.Contains("undocumented") || n.Contains("migra")) { return "passport"; }
            if (n.Contains("coal") || n.Contains("emission") || n.Contains("carbon")) { return "smoke"; }
            if (n.Contains("parental") || n.Contains("leave")) { return "pram"; }
            if (n.Contains("border")) { return "gate"; }
            if (n.Contains("bail") || n.Contains("prison")) { return "key"; }
            if (n.Contains("police") || n.Contains("camera")) { return "shield"; }
            if (n.Contains("court") || n.Contains("judicial") || n.Contains("defender")) { return "scales"; }
            if (n.Contains("wage")) { return "coins"; }
            if (n.Contains("debt") || n.Contains("deficit") || n.Contains("brake") || n.Contains("ceiling")) { return "debt"; }
            if (n.Contains("inflation") || n.Contains("central bank") || n.Contains("rate")) { return "bank"; }
            switch (law.Category)
            {
                case LawCategory.CrimeJustice: return "gavel";
                case LawCategory.LaborMarket: return "jobs";
                case LawCategory.LabourInstitutions: return "jobs";
                case LawCategory.FiscalFramework: return "scales";
                case LawCategory.MonetaryRegime: return "bank";
                case LawCategory.ElectricityTax: return "bolt";
                default: return "docket";
            }
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
            if (n.Contains("incarcer") || n.Contains("prison")) { return "key"; }
            if (n.Contains("corrupt")) { return "stamp"; }
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
                if (stat == StatNodeId.Crime)
                {
                    // §738: the crime index's bulletin, which the Crime tab carried (behaviour 6, channel 1 - an annual figure is this number, for this period, released on this date)
                    slip.Add(PublishedFigure.Line("As published", _playerCountry.Published.Series.TryGetValue(PublishedStat.CrimeIndex, out PublishedSeries crimePublished) ? crimePublished : null));
                }
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

        /// <summary>
        /// §756 (Elias's ruling A5: "Germany, France, Poland and the USA carry their statutory rates, dated and sourced by read"): the minimum wage's slip
        /// lines - the statutory rate in force on the game's date with its instrument (<see cref="MinimumWageRates"/>, Poland's monthly figure its headline),
        /// and the rate the standing index and a draft's stand for: the statute's rate scaled by the index over the seed's (the premise, stated on the
        /// table: the statutory steps track the median wage). Empty where no step is held.
        /// </summary>
        private string MinimumWageInForce(Country c, float draft)
        {
            if (!MinimumWageRates.TryInForce(c.Id, _simulationManager.CurrentDate, out MinimumWageRates.Step step) || c.BaselineMinimumWagePercentOfMedian <= 0f) { return string.Empty; }
            string per = step.Unit == MinimumWageRates.Per.Month ? " A MONTH" : " AN HOUR";
            int places = step.Unit == MinimumWageRates.Per.Month ? 0 : 2;
            string Money(float v) => step.Currency + " " + UiFormat.Number(v, places) + per;
            float standing = step.Rate * c.MinimumWagePercentOfMedianBase / c.BaselineMinimumWagePercentOfMedian;
            string line = " · THE STATUTE IN FORCE " + step.From.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + ": " + Money(step.Rate) + " (" + step.Instrument.ToUpperInvariant() + ")";
            if (!Mathf.Approximately(c.MinimumWagePercentOfMedianBase, c.BaselineMinimumWagePercentOfMedian)) { line += " · THE STANDING INDEX STANDS FOR " + Money(standing); }
            if (!Mathf.Approximately(draft, c.MinimumWagePercentOfMedianBase)) { line += " · THE DRAFT'S FOR " + Money(step.Rate * draft / c.BaselineMinimumWagePercentOfMedian); }
            return line;
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
            /// <summary>§738: the area whose ink the icon takes.</summary>
            public UiPalette.SystemArea Area = UiPalette.SystemArea.Labor;
        }

        /// <summary>A named dial's face: the stop the value falls in as the figure, the first and last stops as the ends, the bands in the slip.</summary>
        private static V35DialFace NamedFace(string icon, DialStops.Dial stops, string census, UiPalette.SystemArea area = UiPalette.SystemArea.Labor) => new V35DialFace
        {
            Icon = icon, Title = stops.Title, Stops = stops, Area = area,
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
            V35DialFace face, bool interactive = true, string captionKey = null, string bandNote = null, string geometryKey = null)
        {
            bool drafted = interactive && !Mathf.Approximately(standing, draft);
            Color area = UiPalette.GetAreaColor(face.Area);
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
                // P4-1: rest equals mid-drag; the film's RowTop key - §741: a key of its own where one name draws several tiles (the five partners' override rates)
                LedgerRow.GeometryByRow[UiGuardContext.CurrentScreen + " / " + (geometryKey ?? name)] = (tile, track, figureRect, endLane);
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
            DialSlipBook.Anchors["dial:" + name] = slip;
            return interactive ? result : draft;
        }

        /// <summary>
        /// A tier bill's call to action (v3.5): the button - *Introduce … bill* with the count of changes, or *Pending · n d* while one is before
        /// Parliament - beside the count the draft would meet today (✓ or ✗, FOR and AGAINST; every party's side and reason on its slip), the status under
        /// them; where the role locks the lever, the lock and its reason in the button's place (the lever lock's rule - no control is drawn there).
        /// </summary>
        private void DrawLawsBillAction(float width, string label, string anchor, BillConcern concern, bool pending, int daysRemaining, int changes, System.Action introduce, string status,
            CabinetPortfolio? portfolio = null)
        {
            GUILayout.Space(V35.Px(4f));
            float h = V35.Px(34f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            float countX = row.x;
            if (!_simulationManager.PlayerMayIntroduce(PlayerCountryId, portfolio, out string lockedBecause))   // §741: the lever's portfolio, where it has one (PS-3g - a junior partner holding it draws no lock)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    DrawStateGlyph(new Rect(row.x, row.y + Mathf.Round((row.height - V35.Px(16f)) * 0.5f), V35.Px(16f), V35.Px(16f)), Symbol.Locked, PoliSimTheme.TextSecondary);
                    GUIStyle lockFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                    PoliSimWidgets.MeasuredLabel(new Rect(row.x + V35.Px(24f), row.y, row.width - V35.Px(24f), row.height), V35Fit(lockedBecause, lockFace, row.width - V35.Px(24f), out _), lockFace);
                }
                SlipAnchor(row, anchor);
                DialSlipBook.Anchors[anchor] = new SlipContent("LOCKED").Add(lockedBecause.ToUpperInvariant());
            }
            else
            {
                string text = pending ? "Pending · " + daysRemaining + " d" : label;
                string changeChip = !pending && changes > 0 ? changes + (changes == 1 ? " change" : " changes") : null;
                if (DrawBudgetButton(row, text, changeChip, !pending)) { introduce(); }
                countX = row.x + BudgetButtonWidth(text, changeChip) + V35.Px(16f);
                SlipAnchor(new Rect(row.x, row.y, Mathf.Max(1f, countX - row.x - V35.Px(16f)), row.height), anchor);
                DialSlipBook.Anchors[anchor] = new SlipContent(label.ToUpperInvariant()).Add(status.ToUpperInvariant());

                // the count the draft would meet today, beside the button
                bool contested = concern != null && !concern.IsEmpty;
                bool wouldPass = _chamberVerdicts.WouldPass(_playerCountry, concern);
                // §761 (PS-5), F3: where the President holds a veto the game runs, a statute that passes the Sejm may still meet a veto - its glyph is the
                // count's, in the caution ink while the veto could kill it (no override carrying), and the slip carries the veto risk as a percentage
                // whenever it is at risk (the answer is the vote day's draw)
                PresidentialVeto.Outcome veto = _chamberVerdicts.Veto(_playerCountry, concern, ChamberVerdicts.VoteDay(_simulationManager.CurrentDate, pending ? daysRemaining : (int?)null));
                bool stands = wouldPass;
                bool atRisk = wouldPass && ChamberVerdicts.AtRisk(veto);
                bool atRiskOfFalling = wouldPass && ChamberVerdicts.AtRiskOfFalling(veto);
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
                    DrawStateGlyph(new Rect(countRect.x, countRect.y + Mathf.Round((row.height - side) * 0.5f), side, side), stands ? Symbol.Good : Symbol.Bad,
                        atRiskOfFalling ? PoliSimTheme.Caution : stands ? PoliSimTheme.Good : PoliSimTheme.Bad);
                    string count = contested ? $"FOR {forSeats} · AGAINST {againstSeats}" + (undecided > 0 ? $" · UNDECIDED {undecided}" : "") : "Nothing changes · uncontested";
                    GUIStyle countFace = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
                    float cw = countRect.width - side - V35.Px(8f);
                    PoliSimWidgets.MeasuredLabel(new Rect(countRect.x + side + V35.Px(8f), countRect.y, Mathf.Max(1f, cw), countRect.height), V35Fit(count, countFace, Mathf.Max(1f, cw), out _), countFace);
                }
                SlipAnchor(countRect, anchor + ":count");
                var countSlip = new SlipContent(!wouldPass ? "WOULD FAIL" : atRisk ? "WOULD PASS · VETO RISK " + ChamberVerdicts.RiskWords(veto) : "WOULD PASS")
                    .Add(contested ? $"FOR {forSeats} · AGAINST {againstSeats}" + (undecided > 0 ? $" · UNDECIDED {undecided}" : "") : "NOTHING CHANGES · UNCONTESTED")
                    .Add("THE COUNT DECIDES - FOR AGAINST AGAINST, THE UNDECIDED ABSTAINING");
                string vetoLine = ChamberVerdicts.VetoLine(PlayerCountryId, veto);
                if (vetoLine != null) { countSlip.Add(vetoLine); }
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
                DialSlipBook.Anchors[anchor + ":count"] = countSlip;
            }
            GUIStyle statusFace = V35SerifWrapped(V35.Floor, PoliSimTheme.TextSecondary);
            float sh = Mathf.Ceil(statusFace.CalcHeight(new GUIContent(status), width)) + V35.Px(4f);
            Rect statusRect = GUILayoutUtility.GetRect(width, sh, GUILayout.Width(width), GUILayout.Height(sh));
            if (Event.current.type == EventType.Repaint) { GUI.Label(statusRect, status, statusFace); }
            GUILayout.Space(V35.Px(V35.Gutter));
        }
    }
}
