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
            TradeAndSpending,
            TaxPolicy,
            SpendingPolicy,
            WelfarePolicy,
            SectorPolicy
        }

        private const CountryId PlayerCountryId = CountryId.USA;
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

        // Draft ABSOLUTE Police Funding / Sentencing Severity levels (0-100, not deltas) - every
        // country has both dials (unlike minimum wage's country-specific asymmetry), so no fallback-
        // to-"not implemented" branch is needed. Not cleared by ResetPolicyInputs, for the same reason
        // _minimumWageInput isn't.
        private float? _policeFundingInput;
        private float? _sentencingSeverityInput;

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
        private readonly Dictionary<SpendingCategory, float> _cachedSpendingLineInputs = new Dictionary<SpendingCategory, float>();
        private readonly Dictionary<CountryId, float> _cachedPartnerTariffInputs = new Dictionary<CountryId, float>();
        private float _cachedInterestRateChangeInput;
        private float _cachedTariffRateChangeInput;
        private float? _cachedMinimumWageInput;
        private float? _cachedPoliceFundingInput;
        private float? _cachedSentencingSeverityInput;
        private string _cachedGdpGrowthText;
        private string _cachedUnemploymentText;
        private string _cachedInflationText;
        private string _cachedApprovalText;
        private string _cachedNetBudgetText;
        private string _cachedPovertyRateText;
        private string _cachedLaborForceParticipationRateText;
        private string _cachedCrimeIndexText;

        private readonly List<string> _turnLog = new List<string>();
        private Vector2 _logScrollPosition;
        private Vector2 _leftColumnScrollPosition;

        private RightPanelTab _rightPanelTab = RightPanelTab.RecentTurns;
        private Vector2 _tradeAndSpendingScrollPosition;
        private Vector2 _taxPolicyScrollPosition;
        private Vector2 _spendingPolicyScrollPosition;
        private Vector2 _welfarePolicyScrollPosition;
        private Vector2 _sectorPolicyScrollPosition;

        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _tabButtonSelectedStyle;
        private GUIStyle _eventBannerStyle;
        private GUIStyle _gameOverStyle;

        private void Start()
        {
            _world = WorldFactory.CreateDefault();
            _simulationManager = gameObject.AddComponent<SimulationManager>();
            _simulationManager.SetWorld(_world);

            _playerCountry = _world.GetCountry(PlayerCountryId);
            _prevGdp = _playerCountry.State.GDP;
            _previewRandom = new System.Random();
        }

        private void OnGUI()
        {
            InitializeStylesIfNeeded();
            RescaleStylesToScreen();
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
            DrawFederalReservePanel();
            GUILayout.Space(sectionSpacing);
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
            DrawRightColumnTabs();
            GUILayout.Space(sectionSpacing * 0.5f);

            float tabContentHeight = areaHeight - _tabButtonStyle.fixedHeight - sectionSpacing * 0.5f;
            switch (_rightPanelTab)
            {
                case RightPanelTab.RecentTurns:
                    DrawTurnLog(tabContentHeight);
                    break;
                case RightPanelTab.TradeAndSpending:
                    DrawTradeAndSpending(tabContentHeight);
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
                case RightPanelTab.WelfarePolicy:
                    GUI.enabled = !_isGameOver;
                    DrawWelfarePolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.SectorPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSectorPolicy(tabContentHeight);
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
            _tabButtonStyle = new GUIStyle(GUI.skin.button);
            _tabButtonSelectedStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            _tabButtonSelectedStyle.normal.textColor = Color.yellow;
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
            _tabButtonSelectedStyle.fontSize = tabFontSize;
            _tabButtonSelectedStyle.fixedHeight = tabButtonHeight;

            _eventBannerStyle.fontSize = bannerFontSize;
            _gameOverStyle.fontSize = bannerFontSize;
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

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{_playerCountry.Name} - Turn {_simulationManager.CurrentTurn}", _headerStyle);
            GUILayout.Label($"GDP: {state.GDP:F1}  ({_lastGrowthPercent:+0.00;-0.00;0}%)", _labelStyle);
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);
            GUILayout.Label($"Labor Force Participation: {state.LaborForceParticipationRate:F1}%", _labelStyle);
            GUILayout.Label($"Crime Index: {state.CrimeIndex:F1}", _labelStyle);
            GUILayout.Label($"Interest Rate: {_playerCountry.CurrencyZone.InterestRate:F2}%", _labelStyle);

            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }

            GUILayout.Label($"Tariff Rate: {_playerCountry.BaseTariffRate:F2}%", _labelStyle);
            GUILayout.Label($"Government Debt: {state.GovernmentDebt:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);
            GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);
            GUILayout.EndVertical();
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
        /// USA's independent Federal Reserve (see CLAUDE.md's "Federal Reserve" section): always
        /// shows the current chair's name/philosophy/description, and - on a turn where a new
        /// presidential term begins - the 2-3 candidates as selectable buttons instead of a normal
        /// slider. No-op for a country without an independent Fed chair (Sweden, Poland keep their
        /// player-controlled Interest Rate Change slider in DrawPolicyControls instead).
        /// </summary>
        private void DrawFederalReservePanel()
        {
            if (_playerCountry.CurrentFedChair == null)
            {
                return;
            }

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Federal Reserve", _headerStyle);

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

            GUILayout.EndVertical();
        }

        private void DrawFedChairCandidateButton(FedChair candidate)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _tabButtonStyle))
            {
                _playerCountry.CurrentFedChair = candidate;
                _fedChairCandidates = null;
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();
        }

        private void DrawPolicyControls()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("This Turn's Policy", _headerStyle);
            GUILayout.Label("Tax rates are set in the Tax Policy tab (implement/remove and adjust each tax there).", _labelStyle);
            GUILayout.Label("Spending is set in the Spending Policy tab (percentage sliders, both Mandatory and Discretionary).", _labelStyle);
            GUILayout.Label("Tariffs (both the general rate and any per-partner override) are set in the Trade & Spending tab's Trade section.", _labelStyle);

            // Shared-currency countries (e.g. Eurozone members) don't set their own rate - only show
            // this control for a country with an independent CurrencyZone AND no independent Fed chair
            // (a Fed-chair country's rate is set by FederalReserveSystem instead - see
            // DrawFederalReservePanel - bypassing this slider/PolicyDecision.InterestRateChange
            // entirely; Sweden and Poland have no chair, so they're unaffected).
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
            if (hasIndependentCurrency && _playerCountry.CurrentFedChair == null)
            {
                GUILayout.Label($"Interest Rate Change: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -InterestRateChangeRange, InterestRateChangeRange, _sliderStyle, _sliderThumbStyle);
            }

            DrawMinimumWageControl();
            DrawCrimeJusticeControls();

            DrawPolicyPreview();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Police Funding / Sentencing Severity (0-100 dials, both start at a neutral 50 for every
        /// country) - two small always-visible levers, not their own tab, for the same "disproportionate
        /// scope for this few controls" reasoning as the Minimum Wage slider.
        /// </summary>
        private void DrawCrimeJusticeControls()
        {
            float draftPoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            GUILayout.Label($"Police Funding: {draftPoliceFunding:F0}", _labelStyle);
            _policeFundingInput = GUILayout.HorizontalSlider(draftPoliceFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftSentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            GUILayout.Label($"Sentencing Severity: {draftSentencingSeverity:F0} (0 = lenient, 100 = harsh)", _labelStyle);
            _sentencingSeverityInput = GUILayout.HorizontalSlider(draftSentencingSeverity, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Minimum wage (percent of median wage) - a single always-visible lever, not its own tab
        /// (unlike Tax/Spending/Welfare Policy's portfolios), since it's just one slider. Only shown
        /// as adjustable if Country.MinimumWageImplemented (USA - see WorldFactory); Sweden and Italy
        /// have no statutory minimum wage in reality, so this shows a read-only note for them instead
        /// (the player's country is hardcoded to USA, so this branch is currently unreachable in
        /// practice, but kept correct in case PlayerCountryId ever changes).
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

            GUILayout.Label($"GDP Growth: {_cachedGdpGrowthText}", _labelStyle);
            GUILayout.Label($"Unemployment: {_cachedUnemploymentText}", _labelStyle);
            GUILayout.Label($"Inflation: {_cachedInflationText}", _labelStyle);
            GUILayout.Label($"Approval: {_cachedApprovalText}", _labelStyle);
            GUILayout.Label($"Poverty Rate: {_cachedPovertyRateText}", _labelStyle);
            GUILayout.Label($"Labor Force Participation: {_cachedLaborForceParticipationRateText}", _labelStyle);
            GUILayout.Label($"Crime Index: {_cachedCrimeIndexText}", _labelStyle);
            GUILayout.Label($"Net Budget Impact: {_cachedNetBudgetText}", _labelStyle);
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

            foreach (Sector sector in _playerCountry.Sectors)
            {
                if (!Mathf.Approximately(GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel), GetCachedSectorSubsidyInput(sector.Type, sector.SubsidyLevel))
                    || !Mathf.Approximately(GetSectorRegulationInput(sector.Type, sector.RegulationLevel), GetCachedSectorRegulationInput(sector.Type, sector.RegulationLevel)))
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
            _cachedNetBudgetText = FormatEstimate(preview.NetBudgetImpact, " units");
            _cachedPovertyRateText = FormatEstimate(preview.PovertyRateChange, " pts");
            _cachedLaborForceParticipationRateText = FormatEstimate(preview.LaborForceParticipationRateChange, " pts");
            _cachedCrimeIndexText = FormatEstimate(preview.CrimeIndexChange, " pts");

            _cachedInterestRateChangeInput = _interestRateChangeInput;
            _cachedTariffRateChangeInput = _tariffRateChangeInput;
            _cachedMinimumWageInput = _minimumWageInput;
            _cachedPoliceFundingInput = _policeFundingInput;
            _cachedSentencingSeverityInput = _sentencingSeverityInput;

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
            if (GUILayout.Button("Advance Turn", _buttonStyle))
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
            ResetPolicyInputs();
            CheckElection();
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

            foreach (Sector sector in _playerCountry.Sectors)
            {
                decision.SectorSubsidyOverrides[sector.Type] = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
                decision.SectorRegulationOverrides[sector.Type] = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
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

        /// <summary>Tab/toggle set for the right column - "Recent Turns" | "Trade &amp; Spending" | "Tax Policy" | "Spending Policy" | "Welfare Policy". The selected tab gets a distinct (bold, colored) style so it's visibly different from the skin's default button look.</summary>
        private void DrawRightColumnTabs()
        {
            GUILayout.BeginHorizontal();

            DrawRightColumnTabButton("Recent Turns", RightPanelTab.RecentTurns);
            DrawRightColumnTabButton("Trade & Spending", RightPanelTab.TradeAndSpending);
            DrawRightColumnTabButton("Tax Policy", RightPanelTab.TaxPolicy);
            DrawRightColumnTabButton("Spending Policy", RightPanelTab.SpendingPolicy);
            DrawRightColumnTabButton("Welfare Policy", RightPanelTab.WelfarePolicy);
            DrawRightColumnTabButton("Economic Sectors", RightPanelTab.SectorPolicy);

            GUILayout.EndHorizontal();
        }

        private void DrawRightColumnTabButton(string label, RightPanelTab tab)
        {
            GUIStyle style = _rightPanelTab == tab ? _tabButtonSelectedStyle : _tabButtonStyle;
            if (GUILayout.Button(label, style))
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

        private void DrawTradeAndSpending(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _tradeAndSpendingScrollPosition = GUILayout.BeginScrollView(_tradeAndSpendingScrollPosition, GUILayout.Height(scrollHeight));

            DrawTradeSection();
            GUILayout.Space(16f);
            DrawSpendingSection();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTradeSection()
        {
            EconomyState state = _playerCountry.State;

            GUILayout.Label("Trade", _headerStyle);
            GUILayout.Label($"Overall Trade Balance: {state.TradeBalance:F1}", _labelStyle);
            GUILayout.Space(6f);

            GUILayout.Label($"General Base Tariff Rate: {_playerCountry.BaseTariffRate:F2}% (applies to any partner with no override, and only where it isn't superseded by trade-bloc membership)", _labelStyle);
            GUILayout.Label($"Tariff Rate Change: {_tariffRateChangeInput:+0.0;-0.0;0} pts", _labelStyle);
            _tariffRateChangeInput = GUILayout.HorizontalSlider(_tariffRateChangeInput, -TariffRateChangeRange, TariffRateChangeRange, _sliderStyle, _sliderThumbStyle);
            GUILayout.Space(10f);

            GUILayout.Label("Set a specific tariff override on our imports from one partner - it beats the usual trade-bloc/base-rate resolution for that partner only. Doesn't affect what that partner charges on our exports to them.", _labelStyle);
            GUILayout.Space(6f);

            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                DrawTradePartnerRow(link, partner);
                GUILayout.Space(10f);
            }
        }

        private void DrawTradePartnerRow(TradePartner link, Country partner)
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

            float buttonWidth = _labelStyle.fontSize * 8f;
            GUILayout.BeginHorizontal();
            if (link.HasPlayerTariffOverride)
            {
                if (GUILayout.Button("Reset to Default", _tabButtonStyle, GUILayout.Width(buttonWidth)))
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
                if (GUILayout.Button("Set Override", _tabButtonStyle, GUILayout.Width(buttonWidth)))
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
            GUILayout.Label($"Net (matches this turn's Budget change): {net:+0.0;-0.0;0}", _headerStyle);
        }

        /// <summary>Every TaxType for the player's country: an Implement/Remove toggle (immediate - see DrawTaxLineRow) plus, only while implemented, a slider that directly sets this turn's target rate.</summary>
        private void DrawTaxPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _taxPolicyScrollPosition = GUILayout.BeginScrollView(_taxPolicyScrollPosition, GUILayout.Height(scrollHeight));

            GUILayout.Label("Tax Policy", _headerStyle);
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
            if (GUILayout.Button(toggleLabel, _tabButtonStyle, GUILayout.Width(buttonWidth)))
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

            GUILayout.Label("Welfare Policy", _headerStyle);
            GUILayout.Label("Implement or remove a welfare program, and (while implemented) drag its generosity directly to the target you want.", _labelStyle);
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
            if (GUILayout.Button(toggleLabel, _tabButtonStyle, GUILayout.Width(buttonWidth)))
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
        /// pass) plus two always-adjustable sliders (Subsidy/Regulation, both absolute targets like
        /// TaxLine.Rate - no implement/remove, every country has all four Sectors always).
        /// </summary>
        private void DrawSectorPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _sectorPolicyScrollPosition = GUILayout.BeginScrollView(_sectorPolicyScrollPosition, GUILayout.Height(scrollHeight));

            GUILayout.Label("Economic Sectors", _headerStyle);
            GUILayout.Label("Output/Employment/the sector's own metric are descriptive only in this pass - subsidy and regulation nudge them, but they don't feed back into GDP/Unemployment.", _labelStyle);
            GUILayout.Space(8f);

            foreach (Sector sector in _playerCountry.Sectors)
            {
                DrawSectorRow(sector);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
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

        private void DrawSectorRow(Sector sector)
        {
            float labelWidth = _labelStyle.fontSize * 10f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(sector.Type.ToString(), _headerStyle, GUILayout.Width(labelWidth));
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

            GUILayout.Label("Spending Policy", _headerStyle);
            GUILayout.Label("Each line's slider is a percentage change of its OWN current amount, not a flat dollar delta. Mandatory programs have a narrower range and hit approval harder per relative size - entitlement reform is politically costly.", _labelStyle);
            GUILayout.Space(8f);

            DrawInterestOnDebtRow();
            GUILayout.Space(10f);

            GUILayout.Label("Mandatory (narrower range, higher approval cost)", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawSpendingLineRow(spendingLine, MandatoryPercentChangeRange);
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

                DrawSpendingLineRow(spendingLine, DiscretionaryPercentChangeRange);
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

        /// <summary>One SpendingLine's row: a slider representing a PERCENTAGE change of its own current Amount, bounded by <paramref name="rangePercent"/> (narrower for Mandatory - see DrawSpendingPolicy), showing both the requested percentage and the dollar amount it implies at the line's current size.</summary>
        private void DrawSpendingLineRow(SpendingLine spendingLine, float rangePercent)
        {
            float draftPercent = GetSpendingLineInput(spendingLine.Category);
            float impliedDollarChange = spendingLine.Amount * draftPercent / 100f;
            GUILayout.Label(
                $"{spendingLine.Category}: {spendingLine.Amount:F1}  Change: {draftPercent:+0.0;-0.0;0}% ({impliedDollarChange:+0.0;-0.0;0})",
                _labelStyle);
            float newPercent = GUILayout.HorizontalSlider(draftPercent, -rangePercent, rangePercent, _sliderStyle, _sliderThumbStyle);
            _spendingLineInputs[spendingLine.Category] = newPercent;
        }
    }
}
