using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// First playable dashboard: an immediate-mode (OnGUI) UI for the player's country (USA,
    /// hardcoded for now) to review its EconomyState, set this turn's PolicyDecision, and advance
    /// the turn. Functional, not styled - validates the core play loop before any visual polish.
    /// Fills the game window and rescales its layout/fonts each frame from Screen.width/height so it
    /// stays usable at any resolution rather than assuming a fixed window size.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private enum RightPanelTab
        {
            RecentTurns,
            WorldMap,
            Trade,
            TaxPolicy,
            SpendingPolicy,
            FederalReserve,
            WelfarePolicy,
            LaborMarket,
            CrimeJustice,
            SectorPolicy,
            Infrastructure,
            SwfPolicy
        }

        // Country-selection task, Part 1: PlayerCountryId is no longer a compile-time constant - the
        // player picks their country on a selector screen shown before the dashboard (see
        // DrawCountrySelector/SelectPlayerCountry). Kept as a property with the SAME name every
        // existing call site already used, so none of them needed to change - only the declaration
        // itself did. _selectedPlayerCountryId is the actual source of truth for "has selection
        // happened yet" (OnGUI gates the whole dashboard behind it); the property's fallback to USA
        // is never actually observed by anything, since nothing that reads PlayerCountryId runs
        // before selection.
        private CountryId? _selectedPlayerCountryId;
        private CountryId PlayerCountryId => _selectedPlayerCountryId ?? CountryId.USA;

        private const int MaxLogEntries = 10;

        /// <summary>This-turn PERCENTAGE-change slider range for a Discretionary SpendingLine - the same range for every category, but a percentage of that line's OWN Amount, so a $1B SBA line and an $850B Defense line move by proportionally (not identically) different dollar amounts. Must match SimulationManager.DiscretionaryPercentChangeRange.</summary>
        private const float DiscretionaryPercentChangeRange = 30f;

        /// <summary>Narrower than DiscretionaryPercentChangeRange - reflects the real political difficulty of entitlement reform. Must match SimulationManager.MandatoryPercentChangeRange.</summary>
        private const float MandatoryPercentChangeRange = 15f;
        private const float TariffRateChangeRange = 5f;
        private const float InterestRateChangeRange = 2f;

        /// <summary>Bounds for the Minimum Wage slider (percent of median wage) - must match SimulationManager.MinMinimumWagePercent/MaxMinimumWagePercent.</summary>
        private const float MinMinimumWagePercent = 0f;
        private const float MaxMinimumWagePercent = 100f;

        /// <summary>Bounds for the Police Funding / Sentencing Severity sliders - must match SimulationManager.MinPolicyDialLevel/MaxPolicyDialLevel.</summary>
        private const float MinPolicyDialLevel = 0f;
        private const float MaxPolicyDialLevel = 100f;

        /// <summary>Bounds for the SWF Contribution/Withdrawal Rate slider - must match SimulationManager.MinSwfContributionRate/MaxSwfContributionRate. Negative values withdraw from the fund (Round 3 item 1, the SWF drawdown mechanic) rather than contribute to it.</summary>
        private const float MinSwfContributionRate = -10f;
        private const float MaxSwfContributionRate = 10f;

        /// <summary>Bounds for the Paid Family Leave slider (weeks) - must match SimulationManager.MinPaidFamilyLeaveWeeks/MaxPaidFamilyLeaveWeeks.</summary>
        private const float MinPaidFamilyLeaveWeeks = 0f;
        private const float MaxPaidFamilyLeaveWeeks = 104f;

        /// <summary>Bounds for the Overtime Regulation / Retraining Program sliders - must match SimulationManager.MinLaborDialLevel/MaxLaborDialLevel.</summary>
        private const float MinLaborDialLevel = 0f;
        private const float MaxLaborDialLevel = 100f;

        /// <summary>Bounds for a per-partner tariff override slider - the same [0,50] range BaseTariffRate itself uses. Must match SimulationManager's MinBaseTariffRate/MaxBaseTariffRate.</summary>
        private const float PartnerTariffOverrideMin = 0f;
        private const float PartnerTariffOverrideMax = 50f;

        // Cosmetic-only margin of error applied to the live policy preview - display layer, never
        // touches the actual PolicyPreview figures the preview math produces.
        private const float MinPreviewMarginPercent = 5f;
        private const float MaxPreviewMarginPercent = 10f;

        // Layout is expressed as fractions of Screen.width/height, not fixed pixel values, so it
        // scales at any window size instead of sitting in a small fixed-size corner box.
        private const float ScreenMarginFraction = 0.02f;
        private const float ColumnSpacingFraction = 0.02f;
        private const float LeftColumnWidthFraction = 0.45f;
        private const float SectionSpacingFraction = 0.03f;

        /// <summary>Vertical gap between the right column's two tab-button rows (see DrawRightColumnTabs) - 11 tabs no longer fit legibly in one row.</summary>
        private const float TabRowSpacing = 4f;

        /// <summary>Fixed display height (px) for the World Map tab's map rect - the map itself stretches to whatever width the tab gives it (see MapRenderer.Draw's ScaleMode.StretchToFill), so only the height needs pinning to keep its aspect roughly sane.</summary>
        private const float WorldMapHeight = 260f;

        private World _world;
        private SimulationManager _simulationManager;
        private Country _playerCountry;

        private float _prevGdp;
        private float _lastGrowthPercent;

        // Draft ABSOLUTE rate per TaxType (not a delta) for the Tax Policy tab's sliders - defaults
        // to that TaxLine's persisted Rate until the player drags it (see GetTaxRateInput). Not
        // cleared by ResetPolicyInputs after Advance Turn: once committed, TaxLine.Rate already
        // equals whatever was in here, so the slider keeps showing the same (now-persisted) value.
        private readonly Dictionary<TaxType, float> _taxRateInputs = new Dictionary<TaxType, float>();

        // Draft ABSOLUTE GenerosityLevel per WelfareProgramType (not a delta) for the Welfare Policy
        // tab's sliders - defaults to that WelfareProgram's persisted GenerosityLevel until the player
        // drags it (see GetWelfareGenerosityInput). Not cleared by ResetPolicyInputs, for the exact
        // same reason _taxRateInputs isn't.
        private readonly Dictionary<WelfareProgramType, float> _welfareGenerosityInputs = new Dictionary<WelfareProgramType, float>();

        // Draft ABSOLUTE Subsidy/Regulation levels per SectorType (not deltas) for the Economic
        // Sectors tab's sliders - every country has all four Sectors (unlike TaxLines/
        // WelfarePrograms), so no implement/remove fallback branch is needed. Not cleared by
        // ResetPolicyInputs, for the same reason _taxRateInputs isn't.
        private readonly Dictionary<SectorType, float> _sectorSubsidyInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorRegulationInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorTaxCreditInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorResearchGrantsInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorDeregulationInputs = new Dictionary<SectorType, float>();

        // Draft ABSOLUTE Sovereign Wealth Fund settings (not deltas) - only meaningful while
        // _playerCountry.SovereignWealthFund is non-null (Create/Dissolve is a separate, immediate
        // action, mirroring TaxLine.IsImplemented). Not cleared by ResetPolicyInputs, for the same
        // reason _minimumWageInput isn't.
        private float? _swfContributionRateInput;
        private float? _swfDomesticAllocationInput;
        private float? _swfEquitiesWeightInput;
        private float? _swfBondsWeightInput;
        private float? _swfInfrastructureWeightInput;
        private float? _swfRealEstateWeightInput;

        // Draft PERCENTAGE change per SpendingCategory (both Mandatory and Discretionary, each
        // clamped to its own range in SimulationManager) - unlike _taxRateInputs, this IS cleared by
        // ResetPolicyInputs each turn, since SpendingLine.Amount itself is what persists, not this
        // draft.
        private readonly Dictionary<SpendingCategory, float> _spendingLineInputs = new Dictionary<SpendingCategory, float>();
        private float _interestRateChangeInput;
        private float _tariffRateChangeInput;

        // Draft ABSOLUTE minimum-wage level (percent of median wage, not a delta) - defaults to
        // Country.MinimumWagePercentOfMedian until the player drags it (see GetMinimumWageInput).
        // Nullable rather than a Dictionary since there's only one lever (unlike _taxRateInputs).
        // Not cleared by ResetPolicyInputs, for the same reason _taxRateInputs isn't - once committed,
        // MinimumWagePercentOfMedian already equals whatever was in here.
        private float? _minimumWageInput;

        // Draft ABSOLUTE Paid Family Leave (weeks) / Overtime Regulation / Retraining Program levels
        // (not deltas) - every country has all three (unlike MinimumWage's country-specific
        // asymmetry). Not cleared by ResetPolicyInputs, for the same reason _minimumWageInput isn't.
        private float? _paidFamilyLeaveWeeksInput;
        private float? _overtimeRegulationInput;
        private float? _retrainingProgramInput;

        // Draft ABSOLUTE Police Funding / Sentencing Severity levels (0-100, not deltas) - every
        // country has both dials (unlike minimum wage's country-specific asymmetry), so no fallback-
        // to-"not implemented" branch is needed. Not cleared by ResetPolicyInputs, for the same reason
        // _minimumWageInput isn't.
        private float? _policeFundingInput;
        private float? _sentencingSeverityInput;
        private float? _bailReformInput;
        private float? _drugPolicyInput;
        private float? _judicialFundingInput;
        private float? _borderEnforcementInput;

        // Draft ABSOLUTE per-partner tariff override rate for the Trade tab's sliders (only shown/
        // meaningful while that partner's TradePartner.HasPlayerTariffOverride is true - mirrors
        // _taxRateInputs' relationship to TaxLine.IsImplemented exactly). Not cleared by
        // ResetPolicyInputs after Advance Turn, for the same reason _taxRateInputs isn't - once
        // committed, TradePartner.PlayerTariffOverride already equals whatever was in here.
        private readonly Dictionary<CountryId, float> _partnerTariffInputs = new Dictionary<CountryId, float>();

        private bool _isGameOver;
        private string _gameOverReason;

        // Federal Reserve (USA only - see CLAUDE.md's "Federal Reserve" section). _fedChairCandidates
        // is non-null exactly while the player must pick a new chair before Advance Turn can proceed;
        // _fedChairCandidatesForTurn records which upcoming turn they were generated for, so a pick
        // already resolved for that turn doesn't immediately regenerate a fresh set next frame (the
        // condition that triggers generation - "next turn is an election turn" - stays true for the
        // rest of that same frame/turn window).
        private List<FedChair> _fedChairCandidates;
        private int _fedChairCandidatesForTurn = -1;

        // Isolated from UnityEngine.Random and from EventSystem's own System.Random - this instance
        // exists purely to jitter the preview's displayed margin of error, so drawing the preview
        // (or dragging sliders to recompute it) can never perturb the event roll or any other RNG
        // consumer's sequence.
        private System.Random _previewRandom;

        // Cached preview text plus the slider/turn snapshot it was computed from - PreviewTurn and
        // the margin roll only re-run when the draft PolicyDecision or the turn number actually
        // changed since last frame, so the displayed numbers read as one stable forecast rather than
        // flickering every OnGUI call even while the player isn't touching anything. Implementing/
        // removing a tax bypasses this cache entirely (see DrawTaxLineRow) since that's an immediate
        // action, not a draft value tracked here.
        private bool _hasCachedPreview;
        private int _cachedPreviewTurn = -1;
        private readonly Dictionary<TaxType, float> _cachedTaxRateInputs = new Dictionary<TaxType, float>();
        private readonly Dictionary<WelfareProgramType, float> _cachedWelfareGenerosityInputs = new Dictionary<WelfareProgramType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorSubsidyInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorRegulationInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorTaxCreditInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorResearchGrantsInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorDeregulationInputs = new Dictionary<SectorType, float>();
        private float? _cachedSwfContributionRateInput;
        private float? _cachedSwfDomesticAllocationInput;
        private float? _cachedSwfEquitiesWeightInput;
        private float? _cachedSwfBondsWeightInput;
        private float? _cachedSwfInfrastructureWeightInput;
        private float? _cachedSwfRealEstateWeightInput;
        private readonly Dictionary<SpendingCategory, float> _cachedSpendingLineInputs = new Dictionary<SpendingCategory, float>();
        private readonly Dictionary<CountryId, float> _cachedPartnerTariffInputs = new Dictionary<CountryId, float>();
        private float _cachedInterestRateChangeInput;
        private float _cachedTariffRateChangeInput;
        private float? _cachedMinimumWageInput;
        private float? _cachedPaidFamilyLeaveWeeksInput;
        private float? _cachedOvertimeRegulationInput;
        private float? _cachedRetrainingProgramInput;
        private float? _cachedPoliceFundingInput;
        private float? _cachedSentencingSeverityInput;
        private float? _cachedBailReformInput;
        private float? _cachedDrugPolicyInput;
        private float? _cachedJudicialFundingInput;
        private float? _cachedBorderEnforcementInput;
        private string _cachedGdpGrowthText;
        private string _cachedUnemploymentText;
        private string _cachedInflationText;
        private string _cachedApprovalText;

        // Raw (unformatted, no cosmetic margin) numeric counterparts of every preview figure -
        // FormatEstimate's margin is a display-only flourish that has no business perturbing what a
        // graph draws or which way UiPalette.GetDeltaColor colors a line. The first three exist for
        // the Phase 2 dashboard graphs' projected-point calculation; all eight are used by Phase 3's
        // policy-preview coloring. Set alongside the formatted text fields in RecomputePolicyPreview.
        private float _cachedGdpGrowthPercentRaw;
        private float _cachedUnemploymentChangeRaw;
        private float _cachedApprovalChangeRaw;
        private float _cachedInflationChangeRaw;
        private float _cachedPovertyRateChangeRaw;
        private float _cachedLaborForceParticipationRateChangeRaw;
        private float _cachedCrimeIndexChangeRaw;
        private float _cachedNetBudgetImpactRaw;
        private float _cachedSwfReturnsEstimateRaw;

        // One GraphRenderer per headline dashboard stat - see GraphRenderer.cs. Each auto-scales its
        // own Y-axis, so instances are never shared across stats with different natural ranges.
        private readonly GraphRenderer _gdpGraph = new GraphRenderer();
        private readonly GraphRenderer _unemploymentGraph = new GraphRenderer();
        private readonly GraphRenderer _approvalGraph = new GraphRenderer();

        // Phase 4's per-tab graph rollout - one GraphRenderer per newly-homed stat, same "never
        // shared across stats" reasoning as the three headline instances above.
        private readonly GraphRenderer _interestRateGraph = new GraphRenderer();
        private readonly GraphRenderer _crimeIndexGraph = new GraphRenderer();
        private readonly GraphRenderer _prisonPopulationGraph = new GraphRenderer();
        private readonly GraphRenderer _organizedCrimeGraph = new GraphRenderer();
        private readonly GraphRenderer _corruptionGraph = new GraphRenderer();
        private readonly GraphRenderer _laborForceParticipationGraph = new GraphRenderer();
        private readonly GraphRenderer _tradeBalanceGraph = new GraphRenderer();
        private readonly GraphRenderer _debtToGdpGraph = new GraphRenderer();
        private readonly GraphRenderer _povertyRateGraph = new GraphRenderer();

        // Phase 5 of the UI revamp: the World Map tab. Event markers are tracked here (not in
        // SimulationManager, which only ever exposes the CURRENT turn's event via GetLastEvent) so a
        // fired event's map dot can fade out over several turns instead of vanishing the instant the
        // next turn advances - see AdvanceTurn for where this list is appended to and pruned.
        private const int EventMarkerFadeTurns = 6;
        private readonly MapRenderer _mapRenderer = new MapRenderer();
        private readonly List<MapEventMarker> _mapEventMarkers = new List<MapEventMarker>();
        private CountryId? _selectedMapCountry;
        private MapEventMarker? _selectedMapEvent;
        private string _cachedNetBudgetText;
        private string _cachedPovertyRateText;
        private string _cachedLaborForceParticipationRateText;
        private string _cachedCrimeIndexText;
        private string _cachedSwfContributionText;
        private string _cachedSwfReturnsText;

        private readonly List<string> _turnLog = new List<string>();
        private Vector2 _logScrollPosition;
        private Vector2 _leftColumnScrollPosition;

        private RightPanelTab _rightPanelTab = RightPanelTab.RecentTurns;
        private Vector2 _worldMapScrollPosition;
        private Vector2 _tradeScrollPosition;
        private Vector2 _taxPolicyScrollPosition;
        private Vector2 _spendingPolicyScrollPosition;
        private Vector2 _federalReserveScrollPosition;
        private Vector2 _welfarePolicyScrollPosition;
        private Vector2 _laborMarketScrollPosition;
        private Vector2 _crimeJusticeScrollPosition;
        private Vector2 _sectorPolicyScrollPosition;
        private Vector2 _infrastructureScrollPosition;
        private Vector2 _swfPolicyScrollPosition;

        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _eventBannerStyle;
        private GUIStyle _gameOverStyle;

        // Phase 3 of the UI revamp: action-type-coded button styles (see UiPalette), rebuilt every
        // frame in RescaleStylesToScreen alongside the base styles they're cloned from, so their
        // font size/fixed height always stay in sync with the current screen size too.
        private GUIStyle _implementButtonStyle;
        private GUIStyle _removeButtonStyle;
        private GUIStyle _neutralActionButtonStyle;
        private GUIStyle _primaryButtonStyle;

        private void Start()
        {
            SetupCameraBackground();

            // World/SimulationManager are created immediately (the selector screen needs every
            // country's Name/Id to exist) - only _playerCountry/_prevGdp wait for SelectPlayerCountry,
            // since which country those refer to isn't known until the player picks.
            _world = WorldFactory.CreateDefault();
            _simulationManager = gameObject.AddComponent<SimulationManager>();
            _simulationManager.SetWorld(_world);
            _previewRandom = new System.Random();
        }

        /// <summary>Commits the player's country choice from DrawCountrySelector - the one place _selectedPlayerCountryId is ever set.</summary>
        private void SelectPlayerCountry(CountryId countryId)
        {
            _selectedPlayerCountryId = countryId;
            _playerCountry = _world.GetCountry(countryId);
            _prevGdp = _playerCountry.State.GDP;
        }

        /// <summary>
        /// Country-selection task, Part 1: shown once, before the dashboard ever renders (see OnGUI's
        /// gate). One button per country, colored with that country's own already-established
        /// UiPalette identity (see UiPalette.GetCountryArea) - the SAME color the World Map tab uses
        /// for that country's node, so a player's choice here reads consistently everywhere else in
        /// the game. No turn has advanced yet at this point (AdvanceTurn is only ever reachable from
        /// the post-selection dashboard), so picking a country has no cost/commitment beyond the
        /// choice itself.
        /// </summary>
        private void DrawCountrySelector()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.4f));
            GUILayout.Label("PoliSim", _headerStyle);
            GUILayout.Label("Choose your country", _labelStyle);
            GUILayout.Space(20f);

            foreach (Country country in _world.Countries)
            {
                UiPalette.SystemArea area = UiPalette.GetCountryArea(country.Id);
                GUIStyle style = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.TabSelected, area);
                if (GUILayout.Button(country.Name, style))
                {
                    SelectPlayerCountry(country.Id);
                }
                GUILayout.Space(10f);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        /// <summary>
        /// This is an IMGUI-only game (see the class doc comment) - nothing is ever meant to render
        /// behind the UI, so Unity's default Skybox clear (visible as sky/horizon in any gap the UI
        /// doesn't cover) is just visual noise, not a deliberate scene. Solid dark color instead,
        /// matching GraphRenderer's own background tone for a consistent dark theme - no new assets
        /// needed, just a clear-flags/color change on whatever camera is tagged MainCamera.
        /// </summary>
        private static void SetupCameraBackground()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        }

        private void OnGUI()
        {
            InitializeStylesIfNeeded();
            RescaleStylesToScreen();

            if (!_selectedPlayerCountryId.HasValue)
            {
                DrawCountrySelector();
                return;
            }

            bool hasPendingFedChairSelection = UpdateFedChairSelectionState();

            float marginX = Screen.width * ScreenMarginFraction;
            float marginY = Screen.height * ScreenMarginFraction;
            float areaWidth = Screen.width - marginX * 2f;
            float areaHeight = Screen.height - marginY * 2f;

            float columnSpacing = areaWidth * ColumnSpacingFraction;
            float leftColumnWidth = areaWidth * LeftColumnWidthFraction;
            float rightColumnWidth = areaWidth - leftColumnWidth - columnSpacing;
            float sectionSpacing = areaHeight * SectionSpacingFraction;

            GUILayout.BeginArea(new Rect(marginX, marginY, areaWidth, areaHeight));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(leftColumnWidth));

            // Advance Turn is pinned outside/below the scroll view so it's always visible and
            // clickable regardless of how tall the banner+dashboard+sliders+preview content gets -
            // its height (plus the spacing before it) is reserved up front, never shared with the
            // scrollable area, so the two can never overlap even in the worst case (event banner
            // present, all sliders visible, preview expanded).
            float advanceButtonAreaHeight = _buttonStyle.fixedHeight + sectionSpacing;
            float leftScrollHeight = areaHeight - advanceButtonAreaHeight;

            _leftColumnScrollPosition = GUILayout.BeginScrollView(_leftColumnScrollPosition, GUILayout.Height(leftScrollHeight));
            DrawTopBanner();
            GUILayout.Space(sectionSpacing);
            DrawDashboard();
            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver;
            DrawPolicyControls();
            GUI.enabled = true;
            GUILayout.EndScrollView();

            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver && !hasPendingFedChairSelection;
            DrawAdvanceTurnButton();
            GUI.enabled = true;

            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);

            GUILayout.BeginVertical(GUILayout.Width(rightColumnWidth));
            DrawRightColumnTabs(rightColumnWidth);
            GUILayout.Space(sectionSpacing * 0.5f);

            // Two tab rows now (see DrawRightColumnTabs) - reserve both rows' height plus the
            // spacing between them, not just one, or the second row would silently eat into the
            // tab-content area below and this whole panel would creep past its allotted height.
            float tabRowsHeight = _tabButtonStyle.fixedHeight * 2f + TabRowSpacing;
            float tabContentHeight = areaHeight - tabRowsHeight - sectionSpacing * 0.5f;
            switch (_rightPanelTab)
            {
                case RightPanelTab.RecentTurns:
                    DrawTurnLog(tabContentHeight);
                    break;
                case RightPanelTab.WorldMap:
                    DrawWorldMapTab(tabContentHeight);
                    break;
                case RightPanelTab.Trade:
                    DrawTrade(tabContentHeight);
                    break;
                case RightPanelTab.TaxPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawTaxPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.SpendingPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSpendingPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.FederalReserve:
                    GUI.enabled = !_isGameOver;
                    DrawFederalReserveTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.WelfarePolicy:
                    GUI.enabled = !_isGameOver;
                    DrawWelfarePolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.LaborMarket:
                    GUI.enabled = !_isGameOver;
                    DrawLaborMarketTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.CrimeJustice:
                    GUI.enabled = !_isGameOver;
                    DrawCrimeJusticeTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.SectorPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSectorPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.Infrastructure:
                    DrawInfrastructureTab(tabContentHeight);
                    break;
                case RightPanelTab.SwfPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSwfPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void InitializeStylesIfNeeded()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            _boxStyle = new GUIStyle(GUI.skin.box);
            // wordWrap so a long label (e.g. "Sovereign Wealth Fund") degrades to two lines at a
            // narrow per-button width instead of being hard-clipped - safe alongside the fixedHeight
            // RescaleStylesToScreen sets below, since a fixed height forces the control to that exact
            // height regardless of how many lines its content wraps to, so this can never push the
            // tab-content area underneath it out of its own reserved space.
            _tabButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };
            _eventBannerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _eventBannerStyle.normal.textColor = new Color(1f, 0.65f, 0f);
            _gameOverStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _gameOverStyle.normal.textColor = Color.red;

            _stylesInitialized = true;
        }

        /// <summary>Re-derives every style's font size/control size from the current screen size every frame (cheap field writes, no allocation) so a live window resize stays legible instead of squinting-small.</summary>
        private void RescaleStylesToScreen()
        {
            int headerFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 42);
            int labelFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 16, 28);
            int buttonFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 38);
            int tabFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 18, 30);
            int bannerFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 20, 36);
            float sliderHeight = Mathf.Clamp(Screen.height * 0.035f, 26f, 50f);
            float sliderThumbWidth = Mathf.Clamp(Screen.width * 0.03f, 26f, 50f);
            float buttonHeight = Mathf.Clamp(Screen.height * 0.09f, 60f, 140f);
            float tabButtonHeight = Mathf.Clamp(Screen.height * 0.05f, 36f, 70f);

            _headerStyle.fontSize = headerFontSize;
            _labelStyle.fontSize = labelFontSize;
            _boxStyle.fontSize = labelFontSize;

            _buttonStyle.fontSize = buttonFontSize;
            _buttonStyle.fixedHeight = buttonHeight;

            _sliderStyle.fixedHeight = sliderHeight;
            _sliderThumbStyle.fixedHeight = sliderHeight;
            _sliderThumbStyle.fixedWidth = sliderThumbWidth;

            _tabButtonStyle.fontSize = tabFontSize;
            _tabButtonStyle.fixedHeight = tabButtonHeight;

            _eventBannerStyle.fontSize = bannerFontSize;
            _gameOverStyle.fontSize = bannerFontSize;

            // Rebuilt every frame (cheap - a handful of GUIStyle clones reusing cached swatch
            // textures, not per-frame texture allocation) so they always match _tabButtonStyle/
            // _buttonStyle's just-updated size above, rather than drifting stale from whatever size
            // they happened to be cloned at in InitializeStylesIfNeeded.
            _implementButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Implement);
            _removeButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Remove);
            _neutralActionButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Neutral);
            _primaryButtonStyle = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Primary);
        }

        /// <summary>Maps a right-column tab to the system area whose hue it should be tinted with (see UiPalette.SystemArea) - Recent Turns is informational, not a system area, so it stays Neutral.</summary>
        private static UiPalette.SystemArea GetTabArea(RightPanelTab tab)
        {
            switch (tab)
            {
                case RightPanelTab.WorldMap: return UiPalette.SystemArea.Global;
                case RightPanelTab.Trade: return UiPalette.SystemArea.Trade;
                case RightPanelTab.TaxPolicy: return UiPalette.SystemArea.Fiscal;
                case RightPanelTab.SpendingPolicy: return UiPalette.SystemArea.Fiscal;
                case RightPanelTab.FederalReserve: return UiPalette.SystemArea.Political;
                case RightPanelTab.WelfarePolicy: return UiPalette.SystemArea.Welfare;
                case RightPanelTab.LaborMarket: return UiPalette.SystemArea.Labor;
                case RightPanelTab.CrimeJustice: return UiPalette.SystemArea.CrimeJustice;
                case RightPanelTab.SectorPolicy: return UiPalette.SystemArea.Sectors;
                case RightPanelTab.Infrastructure: return UiPalette.SystemArea.Infrastructure;
                case RightPanelTab.SwfPolicy: return UiPalette.SystemArea.SovereignWealth;
                default: return UiPalette.SystemArea.Neutral;
            }
        }

        /// <summary>One-off tinted label (GUI.color multiplies the style's own text color, restored immediately after) - used for every signed-delta readout in the UI so its color always reflects UiPalette.GetDeltaColor rather than a hand-picked one-time color.</summary>
        private static void DrawColoredLabel(string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, style);
            GUI.color = previous;
        }

        /// <summary>Above the dashboard: a game-over banner takes priority (the game has effectively ended), otherwise the current turn's event (if any) as "BREAKING: ...".</summary>
        private void DrawTopBanner()
        {
            if (_isGameOver)
            {
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("GAME OVER", _gameOverStyle);
                GUILayout.Label(_gameOverReason, _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            EconomicEvent activeEvent = _simulationManager.GetLastEvent(PlayerCountryId);
            if (activeEvent == null)
            {
                return;
            }

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"BREAKING: {activeEvent.Name}", _eventBannerStyle);
            GUILayout.Label(activeEvent.Description, _labelStyle);
            GUILayout.Label(
                $"Effects: GDP {activeEvent.GdpShockPercent:+0.0;-0.0}%, Inflation {activeEvent.InflationShockPoints:+0.0;-0.0} pts, Approval {activeEvent.ApprovalEffect:+0.0;-0.0}",
                _labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawDashboard()
        {
            EconomyState state = _playerCountry.State;
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);

            // Phase 4 trims this down to true headline indicators only - Labor Force Participation,
            // Crime Index, Paid Family Leave, Incarceration Rate, Infrastructure Condition, Interest
            // Rate, Tariff Rate, and Sovereign Wealth Fund detail all moved to their own dedicated
            // tabs (see DrawRightColumnTabs) and would just be redundant duplication here now - the
            // "compact home view" the task asked for, not everything at once.
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{_playerCountry.Name} - Turn {_simulationManager.CurrentTurn}", _headerStyle);
            DrawColoredLabel($"GDP: {state.GDP:F1}  ({_lastGrowthPercent:+0.00;-0.00;0}%)", _labelStyle, UiPalette.GetDeltaColor(_lastGrowthPercent, higherIsBetter: true));
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);

            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }

            GUILayout.Label($"Government Debt: {state.GovernmentDebt:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);
            GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);

            GUILayout.Space(10f);
            DrawHeadlineGraphs(state);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Phase 2 of the UI revamp's dashboard graphs - proves the GraphRenderer pattern on the
        /// three headline stats before rolling it out further. Each graph reads its country's own
        /// StatHistory (Phase 1) and, once a policy preview has been computed at least once this
        /// session, extends one point further using that PreviewTurn estimate - not a separate
        /// hand-rolled forecast, the same "reuse the real preview math" idiom the existing text
        /// preview already established.
        /// </summary>
        private void DrawHeadlineGraphs(EconomyState state)
        {
            float? projectedGdp = null;
            float? projectedUnemployment = null;
            float? projectedApproval = null;

            if (_hasCachedPreview)
            {
                projectedGdp = state.GDP * (1f + _cachedGdpGrowthPercentRaw / 100f);
                projectedUnemployment = state.Unemployment + _cachedUnemploymentChangeRaw;
                projectedApproval = state.ApprovalRating + _cachedApprovalChangeRaw;
            }

            StatHistory history = _playerCountry.History;
            _gdpGraph.Draw("GDP (last 50 turns; dashed = next-turn estimate)", history.Gdp, projectedGdp, _labelStyle, higherIsBetter: true);
            _unemploymentGraph.Draw("Unemployment (last 50 turns; dashed = next-turn estimate)", history.Unemployment, projectedUnemployment, _labelStyle, higherIsBetter: false);
            _approvalGraph.Draw("Approval Rating (last 50 turns; dashed = next-turn estimate)", history.ApprovalRating, projectedApproval, _labelStyle, higherIsBetter: true);
        }

        /// <summary>
        /// Generates 2-3 Fed chair candidates the first time the upcoming turn is detected as an
        /// election turn (see ElectionSystem.IsElectionTurn), and remembers which turn they were
        /// generated for so picking a candidate doesn't immediately regenerate a fresh set on the
        /// next frame - only when that specific upcoming turn changes (i.e. the next election cycle)
        /// does a new set get drawn. Returns true while a selection is still pending (blocks Advance
        /// Turn - see OnGUI). No-op (returns false) for a country without an independent Fed chair.
        /// </summary>
        private bool UpdateFedChairSelectionState()
        {
            if (_playerCountry.CurrentFedChair == null)
            {
                return false;
            }

            int upcomingTurn = _simulationManager.CurrentTurn + 1;
            if (!ElectionSystem.IsElectionTurn(upcomingTurn))
            {
                return false;
            }

            if (_fedChairCandidatesForTurn != upcomingTurn)
            {
                _fedChairCandidates = FederalReserveSystem.GenerateCandidates();
                _fedChairCandidatesForTurn = upcomingTurn;
            }

            return _fedChairCandidates != null && _fedChairCandidates.Count > 0;
        }

        /// <summary>
        /// Federal Reserve tab (Phase 4 - moved off the dashboard into its own home). For USA's
        /// independent chair (see CLAUDE.md's "Federal Reserve" section): current chair's name/
        /// philosophy/description, and - on a turn where a new presidential term begins - the 2-3
        /// candidates as selectable buttons. For a country with no independent chair and an
        /// independent currency (Sweden, Poland), shows the player-controlled Interest Rate Change
        /// slider. For a Eurozone member (Germany/France/Italy), shows a much narrower National Rate
        /// Push slider instead - see CLAUDE.md's "Eurozone Rate Voice" - this tab is that control's
        /// home regardless of which mechanic the player's country actually uses.
        /// </summary>
        private void DrawFederalReserveTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _federalReserveScrollPosition = GUILayout.BeginScrollView(_federalReserveScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Federal Reserve", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));

            if (_playerCountry.CurrentFedChair != null)
            {
                FedChair chair = _playerCountry.CurrentFedChair;
                GUILayout.Label($"Chair: {chair.Name} ({chair.Philosophy})", _labelStyle);
                GUILayout.Label(chair.Description, _labelStyle);

                if (_fedChairCandidates != null && _fedChairCandidates.Count > 0)
                {
                    GUILayout.Space(8f);
                    GUILayout.Label("A new presidential term begins next turn - choose the next Fed chair:", _labelStyle);
                    foreach (FedChair candidate in _fedChairCandidates)
                    {
                        DrawFedChairCandidateButton(candidate);
                    }
                }
            }
            else
            {
                // Shared-currency countries (e.g. Eurozone members) don't set their own rate - only
                // show this for a country with an independent CurrencyZone (Sweden, Poland; the
                // Eurozone trio share one CurrencyZone.InterestRate that none of them set alone).
                bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
                if (hasIndependentCurrency)
                {
                    GUILayout.Label($"Interest Rate Change: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                    _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -InterestRateChangeRange, InterestRateChangeRange, _sliderStyle, _sliderThumbStyle);
                }
                else
                {
                    // Eurozone Rate-Voice mechanic (see CLAUDE.md's "Eurozone Rate Voice"): the
                    // shared rate is a GDP-weighted blend of all three members' own Taylor Rule
                    // readings (EurozoneRateSystem.GetBlendedSuggestedRate), not something any one
                    // member sets unilaterally - but whichever member the player is currently
                    // controlling now gets a real, bounded push on top of that blend (reusing the
                    // same InterestRateChange field/slider Sweden/Poland use, just with a much
                    // narrower range - a national governor's real but limited sway, not unilateral
                    // control). Superseded the original country-selection Part 1 framing, which
                    // described this as fully read-only before this mechanic existed.
                    GUILayout.Label(
                        $"{_playerCountry.Name} shares the Eurozone's single currency and interest rate with {GetOtherEurozoneMemberNames()}. Each member's own Taylor Rule reading pulls the shared rate toward its own inflation/output-gap situation, weighted by its share of the three countries' combined GDP - a simplified version of the real ECB's \"capital key.\" As {_playerCountry.Name}'s governor you get a modest, bounded push on top of that blend - real influence, not unilateral control, the same way no single member state sets the ECB's rate alone.",
                        _labelStyle);
                    GUILayout.Label($"National Rate Push: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                    _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -EurozoneRateSystem.MemberRatePushRange, EurozoneRateSystem.MemberRatePushRange, _sliderStyle, _sliderThumbStyle);
                    GUILayout.Label($"Current Eurozone Interest Rate: {_playerCountry.CurrencyZone.InterestRate:F2}%", _labelStyle);
                }
            }

            GUILayout.Space(10f);
            // Neutral (no green/red judgment) - which direction of rate change is "good" depends
            // entirely on the current inflation/growth situation, not a fixed convention.
            _interestRateGraph.DrawNeutral("Interest Rate (last 50 turns)", _playerCountry.History.InterestRate, null, _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Every OTHER country sharing the player's own CurrencyZone (reference equality, matching CurrencySystem.SharesCurrencyZoneWithOthers' own idiom - see that method), comma-joined - only meaningful when DrawFederalReserveTab has already confirmed the player's country is in a shared zone.</summary>
        private string GetOtherEurozoneMemberNames()
        {
            var otherNames = new List<string>();
            foreach (Country country in _world.Countries)
            {
                if (country.Id != _playerCountry.Id && country.CurrencyZone == _playerCountry.CurrencyZone)
                {
                    otherNames.Add(country.Name);
                }
            }
            return string.Join(" and ", otherNames);
        }

        private void DrawFedChairCandidateButton(FedChair candidate)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _neutralActionButtonStyle))
            {
                _playerCountry.CurrentFedChair = candidate;
                _fedChairCandidates = null;
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Crime &amp; Justice tab (Phase 4 - moved off the dashboard into its own home): Police
        /// Funding / Sentencing Severity / Bail Reform / Drug Policy / Judicial Funding / Border
        /// Enforcement sliders (the last two added in Round 3 item 3), plus CrimeIndex/
        /// OrganizedCrimeIndex/CorruptionIndex (a clear direction - lower is better for all three) and
        /// PrisonPopulationRate (deliberately neutral - see PrisonPopulationRate's own doc comment on
        /// BailReformLevel/DrugPolicyLevel's honestly-contested effects) history graphs.
        /// </summary>
        private void DrawCrimeJusticeTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _crimeJusticeScrollPosition = GUILayout.BeginScrollView(_crimeJusticeScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Crime & Justice", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.CrimeJustice));
            GUILayout.Space(8f);

            float draftPoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            GUILayout.Label($"Police Funding: {draftPoliceFunding:F0}", _labelStyle);
            _policeFundingInput = GUILayout.HorizontalSlider(draftPoliceFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftSentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            GUILayout.Label($"Sentencing Severity: {draftSentencingSeverity:F0} (0 = lenient, 100 = harsh)", _labelStyle);
            _sentencingSeverityInput = GUILayout.HorizontalSlider(draftSentencingSeverity, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBailReform = GetBailReformInput(_playerCountry.BailReformLevel);
            GUILayout.Label($"Bail Reform: {draftBailReform:F0} (0 = traditional cash bail, 100 = full reform)", _labelStyle);
            _bailReformInput = GUILayout.HorizontalSlider(draftBailReform, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDrugPolicy = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            GUILayout.Label($"Drug Policy: {draftDrugPolicy:F0} (0 = decriminalized, 100 = strict criminalization)", _labelStyle);
            _drugPolicyInput = GUILayout.HorizontalSlider(draftDrugPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftJudicialFunding = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            GUILayout.Label($"Judicial Funding: {draftJudicialFunding:F0}", _labelStyle);
            _judicialFundingInput = GUILayout.HorizontalSlider(draftJudicialFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBorderEnforcement = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);
            GUILayout.Label($"Border Enforcement: {draftBorderEnforcement:F0} (0 = open/lenient, 100 = strict)", _labelStyle);
            _borderEnforcementInput = GUILayout.HorizontalSlider(draftBorderEnforcement, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _crimeIndexGraph.Draw("Crime Index (last 50 turns)", _playerCountry.History.CrimeIndex, null, _labelStyle, higherIsBetter: false);
            _organizedCrimeGraph.Draw("Organized Crime Index (last 50 turns)", _playerCountry.History.OrganizedCrimeIndex, null, _labelStyle, higherIsBetter: false);
            _corruptionGraph.Draw("Corruption Index (last 50 turns)", _playerCountry.History.CorruptionIndex, null, _labelStyle, higherIsBetter: false);
            _prisonPopulationGraph.DrawNeutral("Incarceration Rate per 100k (last 50 turns)", _playerCountry.History.PrisonPopulationRate, null, _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Labor Market tab (Phase 4 - moved off the dashboard into its own home, now also including
        /// Minimum Wage since it's a labor-market lever like the other three): Minimum Wage / Paid
        /// Family Leave / Overtime Regulation / Retraining Program, plus a LaborForceParticipationRate
        /// history graph.
        /// </summary>
        private void DrawLaborMarketTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _laborMarketScrollPosition = GUILayout.BeginScrollView(_laborMarketScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Labor Market", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Labor));
            GUILayout.Space(8f);

            DrawMinimumWageControl();

            float draftPaidLeave = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            GUILayout.Label($"Paid Family Leave: {draftPaidLeave:F0} weeks", _labelStyle);
            _paidFamilyLeaveWeeksInput = GUILayout.HorizontalSlider(draftPaidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, _sliderStyle, _sliderThumbStyle);

            float draftOvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            GUILayout.Label($"Overtime/Working-Hour Regulation: {draftOvertimeRegulation:F0} (0 = unregulated, 100 = strict caps)", _labelStyle);
            _overtimeRegulationInput = GUILayout.HorizontalSlider(draftOvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRetraining = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);
            GUILayout.Label($"Workforce Retraining Programs: {draftRetraining:F0}", _labelStyle);
            _retrainingProgramInput = GUILayout.HorizontalSlider(draftRetraining, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _laborForceParticipationGraph.Draw("Labor Force Participation (last 50 turns)", _playerCountry.History.LaborForceParticipationRate, null, _labelStyle, higherIsBetter: true);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Minimum wage (percent of median wage) - only shown as adjustable if
        /// Country.MinimumWageImplemented (USA - see WorldFactory); Sweden and Italy have no
        /// statutory minimum wage in reality, so this shows a read-only note for them instead (the
        /// player's country is hardcoded to USA, so this branch is currently unreachable in practice,
        /// but kept correct in case PlayerCountryId ever changes).
        /// </summary>
        private void DrawMinimumWageControl()
        {
            if (!_playerCountry.MinimumWageImplemented)
            {
                GUILayout.Label("Minimum Wage: no statutory minimum wage (relies on collective bargaining).", _labelStyle);
                return;
            }

            float draftMinimumWage = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian);
            GUILayout.Label($"Minimum Wage: {draftMinimumWage:F0}% of median wage", _labelStyle);
            _minimumWageInput = GUILayout.HorizontalSlider(draftMinimumWage, MinMinimumWagePercent, MaxMinimumWagePercent, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Infrastructure tab (Phase 4 - new, replacing the old single dashboard summary line):
        /// descriptive only, no player-facing dial (Infrastructure Condition is driven entirely by
        /// the existing Infrastructure spending category - see MacroSystem.ApplyInfrastructureCondition
        /// and GetInfrastructureSummaryLine's own original doc comment for why). Proportional bars,
        /// not a line graph - this is "how do these four assets compare right now" breakdown data,
        /// not a trend-over-time reading, matching the task's own bar-vs-graph guidance.
        /// </summary>
        private void DrawInfrastructureTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _infrastructureScrollPosition = GUILayout.BeginScrollView(_infrastructureScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Infrastructure", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
            GUILayout.Label("Condition Index (0-100) per asset type - driven by the Infrastructure spending category in the Spending Policy tab, not a dial here.", _labelStyle);
            GUILayout.Space(8f);

            foreach (InfrastructureAsset asset in _playerCountry.InfrastructureAssets)
            {
                GUILayout.Label($"{asset.Type}: {asset.ConditionIndex:F0} / 100", _labelStyle);
                UiPalette.DrawBar(asset.ConditionIndex / 100f, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Phase 4's compact dashboard "home view": routing text pointing at every tab (kept brief
        /// since there are now 11 of them) plus the live Policy Preview panel - the preview is
        /// deliberately NOT tab-owned, since it summarizes the draft PolicyDecision across every tab
        /// at once, letting the player gauge this turn's effect without tab-hopping.
        /// </summary>
        private void DrawPolicyControls()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("This Turn's Policy", _headerStyle);
            GUILayout.Label("Every system now has its own tab (Tax/Spending/Federal Reserve/Welfare/Labor Market/Crime & Justice/Economic Sectors/Infrastructure/Sovereign Wealth Fund/Trade) - the estimate below reflects your current draft across all of them at once.", _labelStyle);

            DrawPolicyPreview();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Live estimate of this turn's effect under the sliders' current values, via
        /// SimulationManager.PreviewTurn (reuses the real MacroSystem/SimulationManager formulas
        /// against a throwaway clone rather than a separate hand-rolled estimate) plus a cosmetic
        /// +-5-10% margin of error. Checked every OnGUI call but only actually recomputed (and the
        /// margin re-rolled) when the draft has changed since last frame - see
        /// PolicyInputsChangedSinceLastPreview - so it reads as one stable forecast rather than a
        /// flickering number, while still updating live as the player drags a slider.
        /// </summary>
        private void DrawPolicyPreview()
        {
            if (PolicyInputsChangedSinceLastPreview())
            {
                RecomputePolicyPreview();
            }

            GUILayout.Space(10f);
            GUILayout.Label("Estimated Effects This Turn (±5-10% margin of error)", _headerStyle);
            GUILayout.Label("Projection only, not a guarantee - actual results after you Advance Turn may differ.", _labelStyle);

            // Each line's color follows UiPalette's single green-good/red-bad convention, honoring
            // which direction is actually good for that specific stat (e.g. Unemployment/Inflation/
            // Poverty/Crime falling is the GOOD direction, the opposite of GDP/Approval/LFP rising).
            DrawColoredLabel($"GDP Growth: {_cachedGdpGrowthText}", _labelStyle, UiPalette.GetDeltaColor(_cachedGdpGrowthPercentRaw, higherIsBetter: true));
            DrawColoredLabel($"Unemployment: {_cachedUnemploymentText}", _labelStyle, UiPalette.GetDeltaColor(_cachedUnemploymentChangeRaw, higherIsBetter: false));
            DrawColoredLabel($"Inflation: {_cachedInflationText}", _labelStyle, UiPalette.GetDeltaColor(_cachedInflationChangeRaw, higherIsBetter: false));
            DrawColoredLabel($"Approval: {_cachedApprovalText}", _labelStyle, UiPalette.GetDeltaColor(_cachedApprovalChangeRaw, higherIsBetter: true));
            DrawColoredLabel($"Poverty Rate: {_cachedPovertyRateText}", _labelStyle, UiPalette.GetDeltaColor(_cachedPovertyRateChangeRaw, higherIsBetter: false));
            DrawColoredLabel($"Labor Force Participation: {_cachedLaborForceParticipationRateText}", _labelStyle, UiPalette.GetDeltaColor(_cachedLaborForceParticipationRateChangeRaw, higherIsBetter: true));
            DrawColoredLabel($"Crime Index: {_cachedCrimeIndexText}", _labelStyle, UiPalette.GetDeltaColor(_cachedCrimeIndexChangeRaw, higherIsBetter: false));
            DrawColoredLabel($"Net Budget Impact: {_cachedNetBudgetText}", _labelStyle, UiPalette.GetDeltaColor(_cachedNetBudgetImpactRaw, higherIsBetter: true));
        }

        /// <summary>True if no preview has been computed yet, the turn has advanced since the last one was, or any slider's value (including any tax line's requested rate change) differs from the snapshot the cached preview was computed from.</summary>
        private bool PolicyInputsChangedSinceLastPreview()
        {
            if (!_hasCachedPreview || _simulationManager.CurrentTurn != _cachedPreviewTurn)
            {
                return true;
            }

            bool nonTaxInputsChanged =
                !Mathf.Approximately(_interestRateChangeInput, _cachedInterestRateChangeInput)
                || !Mathf.Approximately(_tariffRateChangeInput, _cachedTariffRateChangeInput);

            if (nonTaxInputsChanged)
            {
                return true;
            }

            if (_playerCountry.MinimumWageImplemented
                && !Mathf.Approximately(
                    GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian),
                    GetCachedMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetPoliceFundingInput(_playerCountry.PoliceFundingLevel),
                    GetCachedPoliceFundingInput(_playerCountry.PoliceFundingLevel))
                || !Mathf.Approximately(
                    GetSentencingSeverityInput(_playerCountry.SentencingSeverity),
                    GetCachedSentencingSeverityInput(_playerCountry.SentencingSeverity)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetBailReformInput(_playerCountry.BailReformLevel),
                    GetCachedBailReformInput(_playerCountry.BailReformLevel))
                || !Mathf.Approximately(
                    GetDrugPolicyInput(_playerCountry.DrugPolicyLevel),
                    GetCachedDrugPolicyInput(_playerCountry.DrugPolicyLevel)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetJudicialFundingInput(_playerCountry.JudicialFundingLevel),
                    GetCachedJudicialFundingInput(_playerCountry.JudicialFundingLevel))
                || !Mathf.Approximately(
                    GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel),
                    GetCachedBorderEnforcementInput(_playerCountry.BorderEnforcementLevel)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks),
                    GetCachedPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks))
                || !Mathf.Approximately(
                    GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel),
                    GetCachedOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel))
                || !Mathf.Approximately(
                    GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel),
                    GetCachedRetrainingProgramInput(_playerCountry.RetrainingProgramLevel)))
            {
                return true;
            }

            foreach (Sector sector in _playerCountry.Sectors)
            {
                if (!Mathf.Approximately(GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel), GetCachedSectorSubsidyInput(sector.Type, sector.SubsidyLevel))
                    || !Mathf.Approximately(GetSectorRegulationInput(sector.Type, sector.RegulationLevel), GetCachedSectorRegulationInput(sector.Type, sector.RegulationLevel))
                    || !Mathf.Approximately(GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel), GetCachedSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel))
                    || !Mathf.Approximately(GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel), GetCachedSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel))
                    || !Mathf.Approximately(GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel), GetCachedSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel)))
                {
                    return true;
                }
            }

            if (_playerCountry.SovereignWealthFund != null)
            {
                SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
                bool swfInputsChanged =
                    !Mathf.Approximately(GetSwfContributionRateInput(fund.ContributionRatePercent), GetCachedSwfContributionRateInput(fund.ContributionRatePercent))
                    || !Mathf.Approximately(GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent), GetCachedSwfDomesticAllocationInput(fund.DomesticAllocationPercent))
                    || !Mathf.Approximately(GetSwfEquitiesWeightInput(fund.EquitiesWeight), GetCachedSwfEquitiesWeightInput(fund.EquitiesWeight))
                    || !Mathf.Approximately(GetSwfBondsWeightInput(fund.BondsWeight), GetCachedSwfBondsWeightInput(fund.BondsWeight))
                    || !Mathf.Approximately(GetSwfInfrastructureWeightInput(fund.InfrastructureWeight), GetCachedSwfInfrastructureWeightInput(fund.InfrastructureWeight))
                    || !Mathf.Approximately(GetSwfRealEstateWeightInput(fund.RealEstateWeight), GetCachedSwfRealEstateWeightInput(fund.RealEstateWeight));

                if (swfInputsChanged)
                {
                    return true;
                }
            }

            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!Mathf.Approximately(GetTaxRateInput(taxLine.Type, taxLine.Rate), GetCachedTaxRateInput(taxLine.Type, taxLine.Rate)))
                {
                    return true;
                }
            }

            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!Mathf.Approximately(GetSpendingLineInput(spendingLine.Category), GetCachedSpendingLineInput(spendingLine.Category)))
                {
                    return true;
                }
            }

            foreach (TradePartner tradePartner in _playerCountry.TradePartners)
            {
                if (!tradePartner.HasPlayerTariffOverride)
                {
                    continue;
                }

                if (!Mathf.Approximately(
                    GetPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride),
                    GetCachedPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride)))
                {
                    return true;
                }
            }

            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                if (!welfareProgram.IsImplemented)
                {
                    continue;
                }

                if (!Mathf.Approximately(
                    GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel),
                    GetCachedWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reruns PreviewTurn, re-rolls each figure's margin, and snapshots the slider values/turn number the result is now valid for.</summary>
        private void RecomputePolicyPreview()
        {
            PolicyPreview preview = _simulationManager.PreviewTurn(PlayerCountryId, BuildPlayerDecision());

            _cachedGdpGrowthText = FormatEstimate(preview.GdpGrowthPercent, "%");
            _cachedUnemploymentText = FormatEstimate(preview.UnemploymentChange, " pts");
            _cachedInflationText = FormatEstimate(preview.InflationChange, " pts");
            _cachedApprovalText = FormatEstimate(preview.ApprovalChange, " pts");
            _cachedGdpGrowthPercentRaw = preview.GdpGrowthPercent;
            _cachedUnemploymentChangeRaw = preview.UnemploymentChange;
            _cachedApprovalChangeRaw = preview.ApprovalChange;
            _cachedNetBudgetText = FormatEstimate(preview.NetBudgetImpact, " units");
            _cachedPovertyRateText = FormatEstimate(preview.PovertyRateChange, " pts");
            _cachedLaborForceParticipationRateText = FormatEstimate(preview.LaborForceParticipationRateChange, " pts");
            _cachedCrimeIndexText = FormatEstimate(preview.CrimeIndexChange, " pts");
            _cachedSwfContributionText = FormatEstimate(preview.SwfContributionEstimate, " units");
            _cachedSwfReturnsText = FormatEstimate(preview.SwfReturnsEstimate, " units");
            _cachedInflationChangeRaw = preview.InflationChange;
            _cachedPovertyRateChangeRaw = preview.PovertyRateChange;
            _cachedLaborForceParticipationRateChangeRaw = preview.LaborForceParticipationRateChange;
            _cachedCrimeIndexChangeRaw = preview.CrimeIndexChange;
            _cachedNetBudgetImpactRaw = preview.NetBudgetImpact;
            _cachedSwfReturnsEstimateRaw = preview.SwfReturnsEstimate;

            _cachedInterestRateChangeInput = _interestRateChangeInput;
            _cachedTariffRateChangeInput = _tariffRateChangeInput;
            _cachedMinimumWageInput = _minimumWageInput;
            _cachedPoliceFundingInput = _policeFundingInput;
            _cachedSentencingSeverityInput = _sentencingSeverityInput;
            _cachedBailReformInput = _bailReformInput;
            _cachedDrugPolicyInput = _drugPolicyInput;
            _cachedJudicialFundingInput = _judicialFundingInput;
            _cachedBorderEnforcementInput = _borderEnforcementInput;
            _cachedPaidFamilyLeaveWeeksInput = _paidFamilyLeaveWeeksInput;
            _cachedOvertimeRegulationInput = _overtimeRegulationInput;
            _cachedRetrainingProgramInput = _retrainingProgramInput;
            _cachedSwfContributionRateInput = _swfContributionRateInput;
            _cachedSwfDomesticAllocationInput = _swfDomesticAllocationInput;
            _cachedSwfEquitiesWeightInput = _swfEquitiesWeightInput;
            _cachedSwfBondsWeightInput = _swfBondsWeightInput;
            _cachedSwfInfrastructureWeightInput = _swfInfrastructureWeightInput;
            _cachedSwfRealEstateWeightInput = _swfRealEstateWeightInput;

            _cachedSectorSubsidyInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorSubsidyInputs)
            {
                _cachedSectorSubsidyInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorRegulationInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorRegulationInputs)
            {
                _cachedSectorRegulationInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorTaxCreditInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorTaxCreditInputs)
            {
                _cachedSectorTaxCreditInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorResearchGrantsInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorResearchGrantsInputs)
            {
                _cachedSectorResearchGrantsInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorDeregulationInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorDeregulationInputs)
            {
                _cachedSectorDeregulationInputs[kvp.Key] = kvp.Value;
            }

            _cachedTaxRateInputs.Clear();
            foreach (KeyValuePair<TaxType, float> kvp in _taxRateInputs)
            {
                _cachedTaxRateInputs[kvp.Key] = kvp.Value;
            }

            _cachedSpendingLineInputs.Clear();
            foreach (KeyValuePair<SpendingCategory, float> kvp in _spendingLineInputs)
            {
                _cachedSpendingLineInputs[kvp.Key] = kvp.Value;
            }

            _cachedPartnerTariffInputs.Clear();
            foreach (KeyValuePair<CountryId, float> kvp in _partnerTariffInputs)
            {
                _cachedPartnerTariffInputs[kvp.Key] = kvp.Value;
            }

            _cachedWelfareGenerosityInputs.Clear();
            foreach (KeyValuePair<WelfareProgramType, float> kvp in _welfareGenerosityInputs)
            {
                _cachedWelfareGenerosityInputs[kvp.Key] = kvp.Value;
            }

            _cachedPreviewTurn = _simulationManager.CurrentTurn;
            _hasCachedPreview = true;
        }

        /// <summary>Formats one estimated figure as "value +- margin", where margin is value's magnitude times a freshly-rolled 5-10% - a range reading, not a precise number, same as any real economic forecast.</summary>
        private string FormatEstimate(float value, string unitSuffix)
        {
            float marginPercent = MinPreviewMarginPercent + (float)_previewRandom.NextDouble() * (MaxPreviewMarginPercent - MinPreviewMarginPercent);
            float marginAmount = Mathf.Abs(value) * marginPercent / 100f;
            return $"{value:+0.00;-0.00;0}{unitSuffix} (±{marginAmount:0.00}{unitSuffix})";
        }

        private void DrawAdvanceTurnButton()
        {
            if (GUILayout.Button("Advance Turn", _primaryButtonStyle))
            {
                AdvanceTurn();
            }
        }

        private void AdvanceTurn()
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country country in _world.Countries)
            {
                decisions[country.Id] = country.Id == PlayerCountryId ? BuildPlayerDecision() : PolicyDecision.None();
            }

            _simulationManager.AdvanceTurn(decisions);

            EconomyState state = _playerCountry.State;
            _lastGrowthPercent = (state.GDP - _prevGdp) / Mathf.Max(_prevGdp, 1f) * 100f;
            _prevGdp = state.GDP;

            AppendLogEntry(state);
            RecordMapEventMarkers();
            ResetPolicyInputs();
            CheckElection();
        }

        /// <summary>
        /// Records this turn's fired event (if any) for every country as a MapEventMarker for the
        /// World Map tab, then prunes any marker old enough to have fully faded (see
        /// EventMarkerFadeTurns) - SimulationManager.GetLastEvent only ever exposes the CURRENT
        /// turn's event, so this is the only place a rolling, fading history exists.
        /// </summary>
        private void RecordMapEventMarkers()
        {
            foreach (Country country in _world.Countries)
            {
                EconomicEvent economicEvent = _simulationManager.GetLastEvent(country.Id);
                if (economicEvent != null)
                {
                    _mapEventMarkers.Add(new MapEventMarker(country.Id, economicEvent, _simulationManager.CurrentTurn));
                }
            }

            _mapEventMarkers.RemoveAll(marker => _simulationManager.CurrentTurn - marker.TurnFired >= EventMarkerFadeTurns);
        }

        /// <summary>The tax-policy tab's draft absolute rate for a TaxType, or <paramref name="fallbackRate"/> (the TaxLine's actual persisted Rate) if the player hasn't touched that slider this turn.</summary>
        private float GetTaxRateInput(TaxType type, float fallbackRate)
        {
            return _taxRateInputs.TryGetValue(type, out float value) ? value : fallbackRate;
        }

        private float GetCachedTaxRateInput(TaxType type, float fallbackRate)
        {
            return _cachedTaxRateInputs.TryGetValue(type, out float value) ? value : fallbackRate;
        }

        /// <summary>The Minimum Wage slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted MinimumWagePercentOfMedian) if the player hasn't touched it this turn.</summary>
        private float GetMinimumWageInput(float fallbackLevel)
        {
            return _minimumWageInput ?? fallbackLevel;
        }

        private float GetCachedMinimumWageInput(float fallbackLevel)
        {
            return _cachedMinimumWageInput ?? fallbackLevel;
        }

        private float GetPaidFamilyLeaveWeeksInput(float fallbackLevel) => _paidFamilyLeaveWeeksInput ?? fallbackLevel;
        private float GetCachedPaidFamilyLeaveWeeksInput(float fallbackLevel) => _cachedPaidFamilyLeaveWeeksInput ?? fallbackLevel;
        private float GetOvertimeRegulationInput(float fallbackLevel) => _overtimeRegulationInput ?? fallbackLevel;
        private float GetCachedOvertimeRegulationInput(float fallbackLevel) => _cachedOvertimeRegulationInput ?? fallbackLevel;
        private float GetRetrainingProgramInput(float fallbackLevel) => _retrainingProgramInput ?? fallbackLevel;
        private float GetCachedRetrainingProgramInput(float fallbackLevel) => _cachedRetrainingProgramInput ?? fallbackLevel;

        /// <summary>The Police Funding slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted PoliceFundingLevel) if the player hasn't touched it this turn.</summary>
        private float GetPoliceFundingInput(float fallbackLevel)
        {
            return _policeFundingInput ?? fallbackLevel;
        }

        private float GetCachedPoliceFundingInput(float fallbackLevel)
        {
            return _cachedPoliceFundingInput ?? fallbackLevel;
        }

        /// <summary>The Sentencing Severity slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted SentencingSeverity) if the player hasn't touched it this turn.</summary>
        private float GetSentencingSeverityInput(float fallbackLevel)
        {
            return _sentencingSeverityInput ?? fallbackLevel;
        }

        private float GetCachedSentencingSeverityInput(float fallbackLevel)
        {
            return _cachedSentencingSeverityInput ?? fallbackLevel;
        }

        private float GetBailReformInput(float fallbackLevel) => _bailReformInput ?? fallbackLevel;
        private float GetCachedBailReformInput(float fallbackLevel) => _cachedBailReformInput ?? fallbackLevel;
        private float GetDrugPolicyInput(float fallbackLevel) => _drugPolicyInput ?? fallbackLevel;
        private float GetCachedDrugPolicyInput(float fallbackLevel) => _cachedDrugPolicyInput ?? fallbackLevel;
        private float GetJudicialFundingInput(float fallbackLevel) => _judicialFundingInput ?? fallbackLevel;
        private float GetCachedJudicialFundingInput(float fallbackLevel) => _cachedJudicialFundingInput ?? fallbackLevel;
        private float GetBorderEnforcementInput(float fallbackLevel) => _borderEnforcementInput ?? fallbackLevel;
        private float GetCachedBorderEnforcementInput(float fallbackLevel) => _cachedBorderEnforcementInput ?? fallbackLevel;

        /// <summary>The Economic Sectors tab's draft absolute Subsidy level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted SubsidyLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorSubsidyInput(SectorType type, float fallbackLevel)
        {
            return _sectorSubsidyInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorSubsidyInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorSubsidyInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>The Economic Sectors tab's draft absolute Regulation level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted RegulationLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorRegulationInput(SectorType type, float fallbackLevel)
        {
            return _sectorRegulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorRegulationInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorRegulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Tax Credit level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted TaxCreditLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorTaxCreditInput(SectorType type, float fallbackLevel)
        {
            return _sectorTaxCreditInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorTaxCreditInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorTaxCreditInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Research Grants level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted ResearchGrantsLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorResearchGrantsInput(SectorType type, float fallbackLevel)
        {
            return _sectorResearchGrantsInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorResearchGrantsInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorResearchGrantsInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Deregulation/Nationalization level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted DeregulationNationalizationLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorDeregulationInput(SectorType type, float fallbackLevel)
        {
            return _sectorDeregulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorDeregulationInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorDeregulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetSwfContributionRateInput(float fallbackLevel) => _swfContributionRateInput ?? fallbackLevel;
        private float GetCachedSwfContributionRateInput(float fallbackLevel) => _cachedSwfContributionRateInput ?? fallbackLevel;
        private float GetSwfDomesticAllocationInput(float fallbackLevel) => _swfDomesticAllocationInput ?? fallbackLevel;
        private float GetCachedSwfDomesticAllocationInput(float fallbackLevel) => _cachedSwfDomesticAllocationInput ?? fallbackLevel;
        private float GetSwfEquitiesWeightInput(float fallbackLevel) => _swfEquitiesWeightInput ?? fallbackLevel;
        private float GetCachedSwfEquitiesWeightInput(float fallbackLevel) => _cachedSwfEquitiesWeightInput ?? fallbackLevel;
        private float GetSwfBondsWeightInput(float fallbackLevel) => _swfBondsWeightInput ?? fallbackLevel;
        private float GetCachedSwfBondsWeightInput(float fallbackLevel) => _cachedSwfBondsWeightInput ?? fallbackLevel;
        private float GetSwfInfrastructureWeightInput(float fallbackLevel) => _swfInfrastructureWeightInput ?? fallbackLevel;
        private float GetCachedSwfInfrastructureWeightInput(float fallbackLevel) => _cachedSwfInfrastructureWeightInput ?? fallbackLevel;
        private float GetSwfRealEstateWeightInput(float fallbackLevel) => _swfRealEstateWeightInput ?? fallbackLevel;
        private float GetCachedSwfRealEstateWeightInput(float fallbackLevel) => _cachedSwfRealEstateWeightInput ?? fallbackLevel;

        /// <summary>The Welfare Policy tab's draft absolute GenerosityLevel for a WelfareProgramType, or <paramref name="fallbackGenerosity"/> (the WelfareProgram's actual persisted GenerosityLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetWelfareGenerosityInput(WelfareProgramType type, float fallbackGenerosity)
        {
            return _welfareGenerosityInputs.TryGetValue(type, out float value) ? value : fallbackGenerosity;
        }

        private float GetCachedWelfareGenerosityInput(WelfareProgramType type, float fallbackGenerosity)
        {
            return _cachedWelfareGenerosityInputs.TryGetValue(type, out float value) ? value : fallbackGenerosity;
        }

        /// <summary>The Trade tab's draft absolute override rate for a partner, or <paramref name="fallbackRate"/> (the TradePartner's actual persisted PlayerTariffOverride) if the player hasn't touched that slider this turn.</summary>
        private float GetPartnerTariffInput(CountryId partnerId, float fallbackRate)
        {
            return _partnerTariffInputs.TryGetValue(partnerId, out float value) ? value : fallbackRate;
        }

        private float GetCachedPartnerTariffInput(CountryId partnerId, float fallbackRate)
        {
            return _cachedPartnerTariffInputs.TryGetValue(partnerId, out float value) ? value : fallbackRate;
        }

        /// <summary>The Spending Policy tab's draft PERCENTAGE change for a SpendingCategory this turn (Mandatory or Discretionary), or 0 if the player hasn't touched that slider.</summary>
        private float GetSpendingLineInput(SpendingCategory category)
        {
            return _spendingLineInputs.TryGetValue(category, out float value) ? value : 0f;
        }

        private float GetCachedSpendingLineInput(SpendingCategory category)
        {
            return _cachedSpendingLineInputs.TryGetValue(category, out float value) ? value : 0f;
        }

        private PolicyDecision BuildPlayerDecision()
        {
            var decision = new PolicyDecision
            {
                InterestRateChange = _interestRateChangeInput,
                TariffRateChange = _tariffRateChangeInput
            };

            // Only meaningful if the country has a statutory minimum wage at all - GetMinimumWageInput's
            // fallback already makes an untouched slider a no-op, so this can be included unconditionally
            // whenever MinimumWageImplemented is true (mirrors TaxRateOverrides' "always safe" reasoning).
            if (_playerCountry.MinimumWageImplemented)
            {
                decision.MinimumWageOverride = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian);
            }

            decision.PoliceFundingOverride = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            decision.SentencingSeverityOverride = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            decision.BailReformOverride = GetBailReformInput(_playerCountry.BailReformLevel);
            decision.DrugPolicyOverride = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            decision.JudicialFundingOverride = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            decision.BorderEnforcementOverride = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);

            decision.PaidFamilyLeaveWeeksOverride = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            decision.OvertimeRegulationOverride = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            decision.RetrainingProgramOverride = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);

            foreach (Sector sector in _playerCountry.Sectors)
            {
                decision.SectorSubsidyOverrides[sector.Type] = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
                decision.SectorRegulationOverrides[sector.Type] = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
                decision.SectorTaxCreditOverrides[sector.Type] = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
                decision.SectorResearchGrantsOverrides[sector.Type] = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
                decision.SectorDeregulationNationalizationOverrides[sector.Type] = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            }

            if (_playerCountry.SovereignWealthFund != null)
            {
                SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
                decision.SwfContributionRateOverride = GetSwfContributionRateInput(fund.ContributionRatePercent);
                decision.SwfDomesticAllocationOverride = GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent);
                decision.SwfEquitiesWeightOverride = GetSwfEquitiesWeightInput(fund.EquitiesWeight);
                decision.SwfBondsWeightOverride = GetSwfBondsWeightInput(fund.BondsWeight);
                decision.SwfInfrastructureWeightOverride = GetSwfInfrastructureWeightInput(fund.InfrastructureWeight);
                decision.SwfRealEstateWeightOverride = GetSwfRealEstateWeightInput(fund.RealEstateWeight);
            }

            // Only currently-implemented lines get an override - a stale draft left over from a since-
            // removed tax must never be sent (GetTaxRateInput's fallback already makes an untouched
            // slider a no-op, so every implemented line can be included unconditionally).
            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!taxLine.IsImplemented)
                {
                    continue;
                }

                decision.TaxRateOverrides[taxLine.Type] = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            }

            // Both Mandatory and Discretionary lines are adjustable - SimulationManager clamps each
            // to its own percentage range (narrower for Mandatory).
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                float percent = GetSpendingLineInput(spendingLine.Category);
                if (percent != 0f)
                {
                    decision.SpendingLineChanges[spendingLine.Category] = percent;
                }
            }

            // Only a partner with an ACTIVE override gets an entry - mirrors "only currently-
            // implemented tax lines get an override" above. A partner without one is left alone
            // entirely (ApplyPartnerTariffOverrides is a no-op with no entry), so an untouched
            // partner's tariff keeps resolving dynamically from bloc/base-rate logic rather than
            // silently getting pinned to whatever its current effective rate happens to be.
            foreach (TradePartner tradePartner in _playerCountry.TradePartners)
            {
                if (!tradePartner.HasPlayerTariffOverride)
                {
                    continue;
                }

                decision.PartnerTariffOverrides[tradePartner.PartnerId] = GetPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride);
            }

            // Only currently-implemented programs get an override - same reasoning as TaxRateOverrides
            // above (GetWelfareGenerosityInput's fallback already makes an untouched slider a no-op,
            // so every implemented program can be included unconditionally).
            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                if (!welfareProgram.IsImplemented)
                {
                    continue;
                }

                decision.WelfareGenerosityOverrides[welfareProgram.Type] = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
            }

            return decision;
        }

        private void ResetPolicyInputs()
        {
            // _taxRateInputs is deliberately NOT cleared here - it holds each tax's absolute rate
            // (not a per-turn delta), and after AdvanceTurn commits it, TaxLine.Rate already equals
            // whatever was in the draft, so leaving it in place keeps the slider showing the same
            // (now-persisted) value instead of snapping back to 0. _spendingLineInputs IS cleared -
            // it's a this-turn delta (like the legacy category sliders it replaces), not an absolute
            // setting; SpendingLine.Amount itself is what persists. _partnerTariffInputs is also
            // deliberately NOT cleared, for the same reason as _taxRateInputs - it holds each
            // overridden partner's absolute rate, which TradePartner.PlayerTariffOverride already
            // equals once committed.
            _spendingLineInputs.Clear();
            _interestRateChangeInput = 0f;
            _tariffRateChangeInput = 0f;
        }

        /// <summary>Checks the player's country against ElectionSystem on election turns; a loss sets the simple game-over state (no restart flow yet, matching the brief).</summary>
        private void CheckElection()
        {
            if (!ElectionSystem.IsElectionTurn(_simulationManager.CurrentTurn))
            {
                return;
            }

            ElectionResult result = ElectionSystem.RunElection(_playerCountry.State);
            if (!result.Won)
            {
                _isGameOver = true;
                _gameOverReason = $"Lost re-election at turn {_simulationManager.CurrentTurn} with {result.ApprovalAtElection:F1} approval " +
                    $"(needed at least {ElectionSystem.LosingThreshold:F0}).";
            }
        }

        private void AppendLogEntry(EconomyState state)
        {
            _turnLog.Add($"Turn {_simulationManager.CurrentTurn}: GDP={state.GDP:F1} ({_lastGrowthPercent:+0.00;-0.00;0}%), " +
                $"Unemp={state.Unemployment:F2}%, Infl={state.Inflation:F2}%, Approval={state.ApprovalRating:F1}, Debt/GDP={state.DebtToGdpRatio:F1}%");

            while (_turnLog.Count > MaxLogEntries)
            {
                _turnLog.RemoveAt(0);
            }
        }

        /// <summary>
        /// Tab/toggle set for the right column - 11 tabs now (Phase 4 gave Federal Reserve/Labor
        /// Market/Crime &amp; Justice/Infrastructure their own homes, split out of the dashboard and
        /// the old combined Trade &amp; Spending tab), split into two rows since that many no longer
        /// fit legibly in one. Each tab is tinted by its own SystemArea (see GetTabArea) - selected
        /// uses the bright TabSelected variant, unselected the dimmer Tab variant, so the currently-
        /// open tab reads as visibly "lit up" in its own area's hue.
        /// </summary>
        private const int TabsPerRow = 6;

        /// <summary>
        /// Each button previously auto-sized to its own label content (no explicit width), which
        /// GUILayout never shrinks to fit - 6 buttons per row summing wider than the actual available
        /// column width (a real risk at smaller window sizes, since column width is itself only a
        /// FRACTION of Screen.width, not a fixed budget) just overflowed silently past the panel/
        /// screen edge instead of wrapping. Now explicitly divided evenly across
        /// <paramref name="availableWidth"/> - the SAME rightColumnWidth OnGUI already computes fresh
        /// from Screen.width every frame - so the row can never exceed its actual budget at any
        /// window size, matching the screen-relative approach already used everywhere else in this
        /// class.
        /// </summary>
        private void DrawRightColumnTabs(float availableWidth)
        {
            float buttonWidth = availableWidth / TabsPerRow;

            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Recent Turns", RightPanelTab.RecentTurns, buttonWidth);
            DrawRightColumnTabButton("World Map", RightPanelTab.WorldMap, buttonWidth);
            DrawRightColumnTabButton("Trade", RightPanelTab.Trade, buttonWidth);
            DrawRightColumnTabButton("Tax Policy", RightPanelTab.TaxPolicy, buttonWidth);
            DrawRightColumnTabButton("Spending Policy", RightPanelTab.SpendingPolicy, buttonWidth);
            DrawRightColumnTabButton("Federal Reserve", RightPanelTab.FederalReserve, buttonWidth);
            GUILayout.EndHorizontal();

            GUILayout.Space(TabRowSpacing);

            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Welfare Policy", RightPanelTab.WelfarePolicy, buttonWidth);
            DrawRightColumnTabButton("Labor Market", RightPanelTab.LaborMarket, buttonWidth);
            DrawRightColumnTabButton("Crime & Justice", RightPanelTab.CrimeJustice, buttonWidth);
            DrawRightColumnTabButton("Economic Sectors", RightPanelTab.SectorPolicy, buttonWidth);
            DrawRightColumnTabButton("Infrastructure", RightPanelTab.Infrastructure, buttonWidth);
            DrawRightColumnTabButton("Sovereign Wealth Fund", RightPanelTab.SwfPolicy, buttonWidth);
            GUILayout.EndHorizontal();
        }

        /// <summary>Each tab is tinted by its own SystemArea (see UiPalette/GetTabArea) - selected uses the bright TabSelected variant, unselected the dimmer Tab variant, so the currently-open tab reads as visibly "lit up" in its own area's hue rather than just bold+yellow text. Width is now explicit (see DrawRightColumnTabs) and the style word-wraps (see InitializeStylesIfNeeded) so a long label like "Sovereign Wealth Fund" degrades to two lines at a narrow width instead of being hard-clipped.</summary>
        private void DrawRightColumnTabButton(string label, RightPanelTab tab, float width)
        {
            UiPalette.SystemArea area = GetTabArea(tab);
            bool selected = _rightPanelTab == tab;
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.TabSelected : UiPalette.ButtonKind.Tab, area);
            if (GUILayout.Button(label, style, GUILayout.Width(width)))
            {
                _rightPanelTab = tab;
            }
        }

        private void DrawTurnLog(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition, GUILayout.Height(scrollHeight));
            foreach (string entry in _turnLog)
            {
                GUILayout.Label(entry, _labelStyle);
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// World Map tab (Phase 5): a stylized, non-geographic map (see MapRenderer) showing all six
        /// countries as clickable markers plus fading event dots. Clicking a marker/dot pins a detail
        /// panel below the map - clicking a country clears any pinned event and vice versa, so
        /// exactly one detail panel shows at a time. Every stat and event description shown here is
        /// read straight from existing SimulationManager/EconomyState/EconomicEvent data - no new
        /// simulation data of any kind.
        /// </summary>
        private void DrawWorldMapTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _worldMapScrollPosition = GUILayout.BeginScrollView(_worldMapScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("World Map", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("Hover a marker for a quick readout, click to pin it below. Colored dots are recent events - green helped, red hurt; size reflects how big a shock it was, and dots fade out over a few turns.", _labelStyle);
            GUILayout.Space(6f);

            Rect mapRect = GUILayoutUtility.GetRect(10f, WorldMapHeight, GUILayout.ExpandWidth(true));
            _mapRenderer.Draw(
                mapRect,
                _world.Countries,
                PlayerCountryId,
                _mapEventMarkers,
                _simulationManager.CurrentTurn,
                EventMarkerFadeTurns,
                _labelStyle,
                out CountryId? clickedCountry,
                out MapEventMarker? clickedEvent);

            if (clickedCountry.HasValue)
            {
                _selectedMapCountry = clickedCountry;
                _selectedMapEvent = null;
            }
            else if (clickedEvent.HasValue)
            {
                _selectedMapEvent = clickedEvent;
                _selectedMapCountry = null;
            }

            GUILayout.Space(10f);

            if (_selectedMapEvent.HasValue)
            {
                DrawSelectedMapEventPanel(_selectedMapEvent.Value);
            }
            else if (_selectedMapCountry.HasValue)
            {
                DrawSelectedMapCountryPanel(_selectedMapCountry.Value);
            }
            else
            {
                GUILayout.Label("Click a country marker or an event dot for details.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Read-only headline readout for the five non-player countries; the full dashboard-level detail set for USA (the player's own country) - matches the task's explicit "read-only for the five, full detail for USA" split.</summary>
        private void DrawSelectedMapCountryPanel(CountryId countryId)
        {
            Country country = _world.GetCountry(countryId);
            if (country == null)
            {
                return;
            }

            EconomyState state = country.State;
            bool isPlayer = countryId == PlayerCountryId;

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(isPlayer ? $"{country.Name} (your country)" : $"{country.Name} (read-only)", _headerStyle);
            GUILayout.Label($"GDP: {state.GDP:F1}", _labelStyle);
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);

            if (isPlayer)
            {
                GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);
                GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        /// <summary>Same effect-description format as the dashboard's own "BREAKING" event banner (see DrawTopBanner) - deliberately not a separate wording.</summary>
        private void DrawSelectedMapEventPanel(MapEventMarker marker)
        {
            Country country = _world.GetCountry(marker.CountryId);
            string countryName = country != null ? country.Name : marker.CountryId.ToString();

            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel($"{countryName}: {marker.Event.Name}", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label(marker.Event.Description, _labelStyle);
            GUILayout.Label(
                $"Effects: GDP {marker.Event.GdpShockPercent:+0.0;-0.0}%, Inflation {marker.Event.InflationShockPoints:+0.0;-0.0} pts, Approval {marker.Event.ApprovalEffect:+0.0;-0.0}",
                _labelStyle);
            GUILayout.Label($"Turn {marker.TurnFired} (this turn: {_simulationManager.CurrentTurn})", _labelStyle);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Trade tab (Phase 4 - split off the old combined "Trade &amp; Spending" tab; the spending
        /// report half moved into the Spending Policy tab instead, where it belongs alongside the
        /// spending sliders it reports on). Adds a TradeBalance history graph and, per partner,
        /// proportional bars for Export/Import volume - "how do these partners compare right now"
        /// breakdown data, exactly the case the task calls out for bars over a line graph.
        /// </summary>
        private void DrawTrade(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _tradeScrollPosition = GUILayout.BeginScrollView(_tradeScrollPosition, GUILayout.Height(scrollHeight));

            EconomyState state = _playerCountry.State;

            DrawColoredLabel("Trade", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            DrawColoredLabel($"Overall Trade Balance: {state.TradeBalance:F1}", _labelStyle, UiPalette.GetDeltaColor(state.TradeBalance, higherIsBetter: true));
            _tradeBalanceGraph.Draw("Trade Balance (last 50 turns)", _playerCountry.History.TradeBalance, null, _labelStyle, higherIsBetter: true);
            GUILayout.Space(6f);

            GUILayout.Label($"General Base Tariff Rate: {_playerCountry.BaseTariffRate:F2}% (applies to any partner with no override, and only where it isn't superseded by trade-bloc membership)", _labelStyle);
            GUILayout.Label($"Tariff Rate Change: {_tariffRateChangeInput:+0.0;-0.0;0} pts", _labelStyle);
            _tariffRateChangeInput = GUILayout.HorizontalSlider(_tariffRateChangeInput, -TariffRateChangeRange, TariffRateChangeRange, _sliderStyle, _sliderThumbStyle);
            GUILayout.Space(10f);

            GUILayout.Label("Set a specific tariff override on our imports from one partner - it beats the usual trade-bloc/base-rate resolution for that partner only. Doesn't affect what that partner charges on our exports to them.", _labelStyle);
            GUILayout.Space(6f);

            // Bars are sized relative to the largest volume across every partner (both directions
            // share one scale) so the bars themselves stay comparable to each other, not just within
            // one partner's own row.
            float maxVolume = 1f;
            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                maxVolume = Mathf.Max(maxVolume, link.ExportVolume, link.ImportVolume);
            }

            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                DrawTradePartnerRow(link, partner, maxVolume);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTradePartnerRow(TradePartner link, Country partner, float maxVolume)
        {
            // Tariffs are asymmetric: the partner charges its own rate on what we export to
            // them, and we charge our own rate on what we import from them - the same two
            // GetTariffRate calls TradeSystem.ApplyTradeEffects itself makes for this link.
            float tariffOnOurExports = TradeSystem.GetTariffRate(partner, _playerCountry, _world.TradeBlocs);
            float tariffOnOurImports = TradeSystem.GetTariffRate(_playerCountry, partner, _world.TradeBlocs);

            GUILayout.Label(
                $"{partner.Name}: Exports={link.ExportVolume:F1}, Imports={link.ImportVolume:F1}, " +
                $"Tariff on our exports={tariffOnOurExports:F2}%, Tariff on our imports={tariffOnOurImports:F2}%" +
                (link.HasPlayerTariffOverride ? " (override active)" : ""),
                _labelStyle);

            GUILayout.Label("Exports:", _labelStyle);
            UiPalette.DrawBar(link.ExportVolume / maxVolume, UiPalette.PositiveChangeColor, 10f);
            GUILayout.Label("Imports:", _labelStyle);
            UiPalette.DrawBar(link.ImportVolume / maxVolume, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade), 10f);

            float buttonWidth = _labelStyle.fontSize * 8f;
            GUILayout.BeginHorizontal();
            if (link.HasPlayerTariffOverride)
            {
                if (GUILayout.Button("Reset to Default", _removeButtonStyle, GUILayout.Width(buttonWidth)))
                {
                    // Reset is immediate (a structural on/off, like TaxLine.IsImplemented), not a
                    // this-turn delta - the preview cache is invalidated right away so it reflects
                    // the reset the moment it happens, rather than waiting for the usual
                    // slider-changed check to catch up.
                    link.PlayerTariffOverride = -1f;
                    RecomputePolicyPreview();
                }

                float draftRate = GetPartnerTariffInput(link.PartnerId, link.PlayerTariffOverride);
                GUILayout.Label($"Override rate: {draftRate:F2}% (range {PartnerTariffOverrideMin:F0}-{PartnerTariffOverrideMax:F0}%)", _labelStyle);
                float newRate = GUILayout.HorizontalSlider(draftRate, PartnerTariffOverrideMin, PartnerTariffOverrideMax, _sliderStyle, _sliderThumbStyle);
                _partnerTariffInputs[link.PartnerId] = newRate;
            }
            else
            {
                if (GUILayout.Button("Set Override", _implementButtonStyle, GUILayout.Width(buttonWidth)))
                {
                    // Enabling is immediate too - starts the override at today's effective rate
                    // (rather than 0) so turning it on never itself changes the tariff; the slider
                    // then lets the player move it from there.
                    link.PlayerTariffOverride = Mathf.Clamp(tariffOnOurImports, PartnerTariffOverrideMin, PartnerTariffOverrideMax);
                    RecomputePolicyPreview();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSpendingSection()
        {
            GUILayout.Label("Spending (Last Turn)", _headerStyle);

            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            if (report == null)
            {
                GUILayout.Label("No turn advanced yet.", _labelStyle);
                return;
            }

            float net = report.Revenue + report.TariffRevenue
                - report.BaselineGovernmentSpending - report.DiscretionarySpending - report.MandatorySpending
                - report.UnemploymentBenefitCost - report.InterestOnDebt - report.WelfareCost;

            GUILayout.Label($"Revenue (Tax): {report.Revenue:F1}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {report.BaselineGovernmentSpending:F1}", _labelStyle);
            GUILayout.Label($"Discretionary Spending Change (this turn): {report.DiscretionarySpending:F1}", _labelStyle);
            GUILayout.Label($"Mandatory Spending: {report.MandatorySpending:F1}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {report.UnemploymentBenefitCost:F1}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {report.InterestOnDebt:F1}", _labelStyle);
            GUILayout.Label($"Welfare Program Cost: {report.WelfareCost:F1}", _labelStyle);
            GUILayout.Label($"Tariff Revenue Collected: {report.TariffRevenue:F1}", _labelStyle);
            GUILayout.Space(6f);
            DrawColoredLabel($"Net (matches this turn's Budget change): {net:+0.0;-0.0;0}", _headerStyle, UiPalette.GetDeltaColor(net, higherIsBetter: true));
        }

        /// <summary>Every TaxType for the player's country: an Implement/Remove toggle (immediate - see DrawTaxLineRow) plus, only while implemented, a slider that directly sets this turn's target rate.</summary>
        private void DrawTaxPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _taxPolicyScrollPosition = GUILayout.BeginScrollView(_taxPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Tax Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("Implement or remove a tax, and (while implemented) drag its rate directly to the target you want.", _labelStyle);
            GUILayout.Space(8f);

            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                DrawTaxLineRow(taxLine);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTaxLineRow(TaxLine taxLine)
        {
            float labelWidth = _labelStyle.fontSize * 8f;
            float buttonWidth = _labelStyle.fontSize * 6f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(taxLine.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = taxLine.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = taxLine.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth)))
            {
                // Implement/Remove is immediate (a structural on/off, not a this-turn delta) - the
                // preview cache is invalidated right away rather than waiting for the usual
                // slider-changed check, so it reflects the toggle the moment it happens.
                taxLine.IsImplemented = !taxLine.IsImplemented;
                RecomputePolicyPreview();
            }
            GUILayout.EndHorizontal();

            if (taxLine.IsImplemented)
            {
                // The slider IS the current setting (defaulting to the persisted Rate until dragged),
                // bounded by this TaxType's own TaxTypeRateRanges - not a small per-turn delta, so a
                // meaningful policy shift (e.g. IncomeTax 37% -> 55%) is reachable in one turn.
                float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);
                GUILayout.Label($"Rate: {draftRate:F2}%  (range {taxLine.MinRate:F0}-{taxLine.MaxRate:F0}%)", _labelStyle);
                float newRate = GUILayout.HorizontalSlider(draftRate, taxLine.MinRate, taxLine.MaxRate, _sliderStyle, _sliderThumbStyle);
                _taxRateInputs[taxLine.Type] = newRate;
            }
            else
            {
                GUILayout.Label($"Not implemented (rate on file: {taxLine.Rate:F2}%)", _labelStyle);
            }
        }

        /// <summary>Every WelfareProgramType for the player's country: an Implement/Remove toggle (immediate - see DrawWelfareProgramRow) plus, only while implemented, a slider that directly sets this turn's target GenerosityLevel. Mirrors DrawTaxPolicy/DrawTaxLineRow exactly.</summary>
        private void DrawWelfarePolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _welfarePolicyScrollPosition = GUILayout.BeginScrollView(_welfarePolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Welfare Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare));
            GUILayout.Label("Implement or remove a welfare program, and (while implemented) drag its generosity directly to the target you want.", _labelStyle);
            _povertyRateGraph.Draw("Poverty Rate (last 50 turns)", _playerCountry.History.PovertyRate, null, _labelStyle, higherIsBetter: false);
            GUILayout.Space(8f);

            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                DrawWelfareProgramRow(welfareProgram);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawWelfareProgramRow(WelfareProgram welfareProgram)
        {
            float labelWidth = _labelStyle.fontSize * 10f;
            float buttonWidth = _labelStyle.fontSize * 6f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(welfareProgram.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = welfareProgram.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = welfareProgram.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth)))
            {
                // Implement/Remove is immediate (a structural on/off, not a this-turn delta) - the
                // preview cache is invalidated right away rather than waiting for the usual
                // slider-changed check, so it reflects the toggle the moment it happens.
                welfareProgram.IsImplemented = !welfareProgram.IsImplemented;
                RecomputePolicyPreview();
            }
            GUILayout.EndHorizontal();

            if (welfareProgram.IsImplemented)
            {
                // The slider IS the current setting (defaulting to the persisted GenerosityLevel until
                // dragged), bounded 0-100% - not a small per-turn delta, so a meaningful policy shift
                // is reachable in one turn.
                float draftGenerosity = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
                GUILayout.Label($"Generosity: {draftGenerosity:F0}%", _labelStyle);
                float newGenerosity = GUILayout.HorizontalSlider(draftGenerosity, 0f, 100f, _sliderStyle, _sliderThumbStyle);
                _welfareGenerosityInputs[welfareProgram.Type] = newGenerosity;
            }
            else
            {
                GUILayout.Label($"Not implemented (generosity on file: {welfareProgram.GenerosityLevel:F0}%)", _labelStyle);
            }
        }

        /// <summary>
        /// Every Sector for the player's country: current Output/Employment/SectorMetric (read-only,
        /// descriptive - see Sector.cs for why they don't feed back into GDP/Unemployment in this
        /// pass) plus five always-adjustable sliders (Subsidy/Regulation/Tax Credits/Research Grants/
        /// Deregulation-Nationalization, all absolute targets like TaxLine.Rate - no implement/remove,
        /// every country has all four Sectors always). The last three were added in Round 3 item 2.
        /// </summary>
        private void DrawSectorPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _sectorPolicyScrollPosition = GUILayout.BeginScrollView(_sectorPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Economic Sectors", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors));
            GUILayout.Label("Output/Employment/the sector's own metric are descriptive only in this pass - the five dials below nudge them, but they don't feed back into GDP/Unemployment.", _labelStyle);
            GUILayout.Space(8f);

            // Measured (not guessed) against _headerStyle - the style the name column is actually
            // drawn in - since _headerStyle's font is both bigger and bolder than _labelStyle's, a
            // width budget borrowed from _labelStyle's own metrics (the original bug here) undersizes
            // the column for the longest name ("Manufacturing"), which then wraps and collides with
            // the adjacent stats text sharing the same horizontal row.
            float sectorNameColumnWidth = GetSectorNameColumnWidth();
            foreach (Sector sector in _playerCountry.Sectors)
            {
                DrawSectorRow(sector, sectorNameColumnWidth);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Widest SectorType name as rendered in _headerStyle (the style DrawSectorRow's name column actually uses), plus a small right-side pad - recomputed each call (not cached) since _headerStyle's font size itself changes every frame in RescaleStylesToScreen as the window resizes.</summary>
        private float GetSectorNameColumnWidth()
        {
            float widest = 0f;
            foreach (SectorType type in System.Enum.GetValues(typeof(SectorType)))
            {
                widest = Mathf.Max(widest, _headerStyle.CalcSize(new GUIContent(type.ToString())).x);
            }
            return widest + 12f;
        }

        /// <summary>The sector-specific metric's label, matching Sector.SectorMetric's per-Type real-world meaning (see Sector.cs).</summary>
        private static string GetSectorMetricLabel(SectorType type)
        {
            switch (type)
            {
                case SectorType.Manufacturing: return "Capacity Utilization";
                case SectorType.Technology: return "Innovation Index";
                case SectorType.Agriculture: return "Export Share";
                case SectorType.Finance: return "Credit Growth Rate";
                default: return "Sector Metric";
            }
        }

        private void DrawSectorRow(Sector sector, float nameColumnWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(sector.Type.ToString(), _headerStyle, GUILayout.Width(nameColumnWidth));
            GUILayout.Label(
                $"Output {sector.OutputShareOfGdp:F1}% of GDP | Employment {sector.EmploymentShare:F1}% | {GetSectorMetricLabel(sector.Type)} {sector.SectorMetric:F1}",
                _labelStyle);
            GUILayout.EndHorizontal();

            float draftSubsidy = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
            GUILayout.Label($"Subsidy: {draftSubsidy:F0}", _labelStyle);
            _sectorSubsidyInputs[sector.Type] = GUILayout.HorizontalSlider(draftSubsidy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRegulation = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
            GUILayout.Label($"Regulation: {draftRegulation:F0} (0 = light-touch, 100 = heavily regulated)", _labelStyle);
            _sectorRegulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftRegulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftTaxCredit = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
            GUILayout.Label($"Tax Credits: {draftTaxCredit:F0}", _labelStyle);
            _sectorTaxCreditInputs[sector.Type] = GUILayout.HorizontalSlider(draftTaxCredit, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftResearchGrants = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
            GUILayout.Label($"Research Grants: {draftResearchGrants:F0}", _labelStyle);
            _sectorResearchGrantsInputs[sector.Type] = GUILayout.HorizontalSlider(draftResearchGrants, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDeregulation = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            GUILayout.Label($"Deregulation/Nationalization: {draftDeregulation:F0} (0 = fully nationalized, 100 = fully deregulated/private)", _labelStyle);
            _sectorDeregulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftDeregulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Sovereign Wealth Fund tab: a Create/Dissolve button (immediate, mirrors TaxLine.
        /// IsImplemented's toggle pattern) plus, only while it exists, TotalAssets/this-turn estimated
        /// contribution-or-withdrawal+returns (read-only) and sliders for every adjustable setting,
        /// including the Contribution/Withdrawal Rate slider that now goes negative to draw the fund
        /// down (Round 3 item 1). Net Government Position (GovernmentDebt minus fund TotalAssets) is
        /// shown ALONGSIDE, not instead of, the raw GovernmentDebt figure already on the dashboard -
        /// per the task's explicit requirement that fund assets must never be used to obscure a real
        /// fiscal problem. This is a GameController-only display computation - it is never written
        /// back into EconomyState/Country and never read by any simulation formula
        /// (GetDebtRiskPremium, GetFiscalReactionMultiplier, etc. all keep reading the real, gross
        /// GovernmentDebt/DebtToGdpRatio exactly as before).
        /// </summary>
        private void DrawSwfPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _swfPolicyScrollPosition = GUILayout.BeginScrollView(_swfPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Sovereign Wealth Fund", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth));

            SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
            string toggleLabel = fund == null ? "Create Fund" : "Dissolve Fund";
            GUIStyle toggleStyle = fund == null ? _implementButtonStyle : _removeButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle))
            {
                _playerCountry.SovereignWealthFund = fund == null ? new SovereignWealthFund() : null;
                RecomputePolicyPreview();
            }

            if (fund == null)
            {
                GUILayout.Label("No fund exists. Creating one starts a new budget expense (the contribution) in exchange for market returns on its growing assets - it can also be drawn down during a recession or emergency instead of borrowing, once it exists.", _labelStyle);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label($"Total Assets: {fund.TotalAssets:F1}", _labelStyle);
            float netGovernmentPosition = _playerCountry.State.GovernmentDebt - fund.TotalAssets;
            GUILayout.Label($"Government Debt (gross): {_playerCountry.State.GovernmentDebt:F1}  |  Net Government Position (debt minus fund assets): {netGovernmentPosition:F1}", _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Estimated this turn - Contribution/Withdrawal: {_cachedSwfContributionText}, Returns: ", _labelStyle, GUILayout.ExpandWidth(false));
            DrawColoredLabel(_cachedSwfReturnsText, _labelStyle, UiPalette.GetDeltaColor(_cachedSwfReturnsEstimateRaw, higherIsBetter: true));
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            float draftContributionRate = GetSwfContributionRateInput(fund.ContributionRatePercent);
            GUILayout.Label($"Contribution/Withdrawal Rate: {draftContributionRate:+0.0;-0.0;0}% of GDP per turn (negative draws the fund down - use during a recession or emergency instead of borrowing)", _labelStyle);
            _swfContributionRateInput = GUILayout.HorizontalSlider(draftContributionRate, MinSwfContributionRate, MaxSwfContributionRate, _sliderStyle, _sliderThumbStyle);

            float draftDomesticAllocation = GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent);
            GUILayout.Label($"Domestic Allocation: {draftDomesticAllocation:F0}% (rest international - this pass doesn't model differing returns by allocation)", _labelStyle);
            _swfDomesticAllocationInput = GUILayout.HorizontalSlider(draftDomesticAllocation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(8f);
            GUILayout.Label("Asset Class Mix (weights, normalized automatically - don't need to sum to 100)", _labelStyle);

            // Each bar's fraction IS the already-normalized weight (0-1) - no further scaling needed,
            // unlike the spending-line/trade-volume bars above which normalize against a group max.
            Color swfColor = UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth);

            float draftEquities = GetSwfEquitiesWeightInput(fund.EquitiesWeight);
            GUILayout.Label($"Equities: {draftEquities:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Equities) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Equities), swfColor, 8f);
            _swfEquitiesWeightInput = GUILayout.HorizontalSlider(draftEquities, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBonds = GetSwfBondsWeightInput(fund.BondsWeight);
            GUILayout.Label($"Bonds: {draftBonds:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Bonds) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Bonds), swfColor, 8f);
            _swfBondsWeightInput = GUILayout.HorizontalSlider(draftBonds, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftInfrastructure = GetSwfInfrastructureWeightInput(fund.InfrastructureWeight);
            GUILayout.Label($"Infrastructure: {draftInfrastructure:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure), swfColor, 8f);
            _swfInfrastructureWeightInput = GUILayout.HorizontalSlider(draftInfrastructure, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRealEstate = GetSwfRealEstateWeightInput(fund.RealEstateWeight);
            GUILayout.Label($"Real Estate: {draftRealEstate:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate), swfColor, 8f);
            _swfRealEstateWeightInput = GUILayout.HorizontalSlider(draftRealEstate, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// The player country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's
        /// "Detailed Spending Portfolio"), grouped Mandatory / Discretionary, plus Interest on Debt
        /// as a read-only automatic line. Both groups now get a this-turn PERCENTAGE-change slider
        /// (SimulationManager.ApplySpendingLineChanges applies it to that line's own Amount) -
        /// Mandatory's range is narrower, reflecting the real political difficulty of entitlement
        /// reform, and a Mandatory change carries a distinctly higher approval-rating penalty per
        /// relative size than a Discretionary one (see MacroSystem.MandatorySpendingApprovalMultiplier).
        /// </summary>
        private void DrawSpendingPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _spendingPolicyScrollPosition = GUILayout.BeginScrollView(_spendingPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Spending Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("Each line's slider is a percentage change of its OWN current amount, not a flat dollar delta. Mandatory programs have a narrower range and hit approval harder per relative size - entitlement reform is politically costly.", _labelStyle);
            GUILayout.Space(8f);

            // Moved here from the old combined "Trade & Spending" tab (Phase 4) - the last-turn
            // fiscal report belongs next to the sliders it explains, not bolted onto Trade.
            DrawSpendingSection();
            _debtToGdpGraph.Draw("Debt-to-GDP (last 50 turns)", _playerCountry.History.DebtToGdpRatio, null, _labelStyle, higherIsBetter: false);
            GUILayout.Space(16f);

            DrawInterestOnDebtRow();
            GUILayout.Space(10f);

            // Bars are scaled within their own group (Mandatory vs. Discretionary), not against each
            // other - the two groups differ by orders of magnitude (e.g. Social Security vs. SBA), so
            // one shared scale would flatten every Discretionary bar to nothing.
            float maxMandatory = 1f;
            float maxDiscretionary = 1f;
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    maxMandatory = Mathf.Max(maxMandatory, spendingLine.Amount);
                }
                else
                {
                    maxDiscretionary = Mathf.Max(maxDiscretionary, spendingLine.Amount);
                }
            }

            GUILayout.Label("Mandatory (narrower range, higher approval cost)", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawSpendingLineRow(spendingLine, MandatoryPercentChangeRange, maxMandatory);
                GUILayout.Space(10f);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Discretionary", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawSpendingLineRow(spendingLine, DiscretionaryPercentChangeRange, maxDiscretionary);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Interest on Debt is SimulationManager's existing automatic GetInterestOnDebt calculation, not a seeded line - shown as a read-only, clearly-marked-automatic figure from last turn's FiscalTurnReport.</summary>
        private void DrawInterestOnDebtRow()
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            string valueText = report != null ? $"{report.InterestOnDebt:F1}" : "not yet computed (advance a turn)";
            GUILayout.Label($"Interest on Debt (automatic, last turn): {valueText}", _labelStyle);
        }

        /// <summary>One SpendingLine's row: a slider representing a PERCENTAGE change of its own current Amount, bounded by <paramref name="rangePercent"/> (narrower for Mandatory - see DrawSpendingPolicy), showing both the requested percentage and the dollar amount it implies at the line's current size, plus a bar sized relative to <paramref name="maxAmountInGroup"/> (its own Mandatory/Discretionary group's largest line) for an at-a-glance size comparison.</summary>
        private void DrawSpendingLineRow(SpendingLine spendingLine, float rangePercent, float maxAmountInGroup)
        {
            float draftPercent = GetSpendingLineInput(spendingLine.Category);
            float impliedDollarChange = spendingLine.Amount * draftPercent / 100f;
            GUILayout.Label(
                $"{spendingLine.Category}: {spendingLine.Amount:F1}  Change: {draftPercent:+0.0;-0.0;0}% ({impliedDollarChange:+0.0;-0.0;0})",
                _labelStyle);
            UiPalette.DrawBar(spendingLine.Amount / maxAmountInGroup, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), 8f);
            float newPercent = GUILayout.HorizontalSlider(draftPercent, -rangePercent, rangePercent, _sliderStyle, _sliderThumbStyle);
            _spendingLineInputs[spendingLine.Category] = newPercent;
        }
    }
}
