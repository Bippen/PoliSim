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
            SpendingPolicy
        }

        private const CountryId PlayerCountryId = CountryId.USA;
        private const int MaxLogEntries = 10;

        /// <summary>Per-Discretionary-category this-turn slider range - a flat starting-point placeholder like every other constant here, not scaled per category (a $1B SBA line and an $850B Defense line share the same +-100 range for now).</summary>
        private const float DiscretionaryLineChangeRange = 100f;
        private const float TariffRateChangeRange = 5f;
        private const float InterestRateChangeRange = 2f;

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

        // Draft dollar CHANGE per Discretionary SpendingCategory (a delta, like the legacy category
        // sliders it replaces - unlike _taxRateInputs, this IS cleared by ResetPolicyInputs each turn,
        // since SpendingLine.Amount itself is what persists, not this draft).
        private readonly Dictionary<SpendingCategory, float> _spendingLineInputs = new Dictionary<SpendingCategory, float>();
        private float _interestRateChangeInput;
        private float _tariffRateChangeInput;

        private bool _isGameOver;
        private string _gameOverReason;

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
        private readonly Dictionary<SpendingCategory, float> _cachedSpendingLineInputs = new Dictionary<SpendingCategory, float>();
        private float _cachedInterestRateChangeInput;
        private float _cachedTariffRateChangeInput;
        private string _cachedGdpGrowthText;
        private string _cachedUnemploymentText;
        private string _cachedInflationText;
        private string _cachedApprovalText;
        private string _cachedNetBudgetText;

        private readonly List<string> _turnLog = new List<string>();
        private Vector2 _logScrollPosition;
        private Vector2 _leftColumnScrollPosition;

        private RightPanelTab _rightPanelTab = RightPanelTab.RecentTurns;
        private Vector2 _tradeAndSpendingScrollPosition;
        private Vector2 _taxPolicyScrollPosition;
        private Vector2 _spendingPolicyScrollPosition;

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

            GUI.enabled = !_isGameOver;
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

        private void DrawPolicyControls()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("This Turn's Policy", _headerStyle);
            GUILayout.Label("Tax rates are set in the Tax Policy tab (implement/remove and adjust each tax there).", _labelStyle);
            GUILayout.Label("Spending is set in the Spending Policy tab (Discretionary categories only - Mandatory programs aren't player-adjustable yet).", _labelStyle);

            GUILayout.Label($"Tariff Rate Change: {_tariffRateChangeInput:+0.0;-0.0;0} pts", _labelStyle);
            _tariffRateChangeInput = GUILayout.HorizontalSlider(_tariffRateChangeInput, -TariffRateChangeRange, TariffRateChangeRange, _sliderStyle, _sliderThumbStyle);

            // Shared-currency countries (e.g. Eurozone members) don't set their own rate - only show
            // this control for a country with an independent CurrencyZone, same test CurrencySystem uses.
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Interest Rate Change: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -InterestRateChangeRange, InterestRateChangeRange, _sliderStyle, _sliderThumbStyle);
            }

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

            GUILayout.Label($"GDP Growth: {_cachedGdpGrowthText}", _labelStyle);
            GUILayout.Label($"Unemployment: {_cachedUnemploymentText}", _labelStyle);
            GUILayout.Label($"Inflation: {_cachedInflationText}", _labelStyle);
            GUILayout.Label($"Approval: {_cachedApprovalText}", _labelStyle);
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

            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!Mathf.Approximately(GetTaxRateInput(taxLine.Type, taxLine.Rate), GetCachedTaxRateInput(taxLine.Type, taxLine.Rate)))
                {
                    return true;
                }
            }

            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    continue;
                }

                if (!Mathf.Approximately(GetSpendingLineInput(spendingLine.Category), GetCachedSpendingLineInput(spendingLine.Category)))
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

            _cachedInterestRateChangeInput = _interestRateChangeInput;
            _cachedTariffRateChangeInput = _tariffRateChangeInput;

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

        /// <summary>The Spending Policy tab's draft dollar CHANGE for a Discretionary SpendingCategory this turn, or 0 if the player hasn't touched that slider.</summary>
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

            // Only Discretionary lines are player-adjustable in Phase 1 - Mandatory programs never
            // get an entry here (see SpendingCategory's doc comment).
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    continue;
                }

                float delta = GetSpendingLineInput(spendingLine.Category);
                if (delta != 0f)
                {
                    decision.SpendingLineChanges[spendingLine.Category] = delta;
                }
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
            // setting; SpendingLine.Amount itself is what persists.
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

        /// <summary>Tab/toggle set for the right column - "Recent Turns" | "Trade &amp; Spending" | "Tax Policy" | "Spending Policy". The selected tab gets a distinct (bold, colored) style so it's visibly different from the skin's default button look.</summary>
        private void DrawRightColumnTabs()
        {
            GUILayout.BeginHorizontal();

            DrawRightColumnTabButton("Recent Turns", RightPanelTab.RecentTurns);
            DrawRightColumnTabButton("Trade & Spending", RightPanelTab.TradeAndSpending);
            DrawRightColumnTabButton("Tax Policy", RightPanelTab.TaxPolicy);
            DrawRightColumnTabButton("Spending Policy", RightPanelTab.SpendingPolicy);

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

            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                // Tariffs are asymmetric: the partner charges its own rate on what we export to
                // them, and we charge our own rate on what we import from them - the same two
                // GetTariffRate calls TradeSystem.ApplyTradeEffects itself makes for this link.
                float tariffOnOurExports = TradeSystem.GetTariffRate(partner, _playerCountry, _world.TradeBlocs);
                float tariffOnOurImports = TradeSystem.GetTariffRate(_playerCountry, partner, _world.TradeBlocs);

                GUILayout.Label(
                    $"{partner.Name}: Exports={link.ExportVolume:F1}, Imports={link.ImportVolume:F1}, " +
                    $"Tariff on our exports={tariffOnOurExports:F2}%, Tariff on our imports={tariffOnOurImports:F2}%",
                    _labelStyle);
                GUILayout.Space(4f);
            }
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
                - report.UnemploymentBenefitCost - report.InterestOnDebt;

            GUILayout.Label($"Revenue (Tax): {report.Revenue:F1}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {report.BaselineGovernmentSpending:F1}", _labelStyle);
            GUILayout.Label($"Discretionary Spending Change (this turn): {report.DiscretionarySpending:F1}", _labelStyle);
            GUILayout.Label($"Mandatory Spending: {report.MandatorySpending:F1}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {report.UnemploymentBenefitCost:F1}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {report.InterestOnDebt:F1}", _labelStyle);
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

        /// <summary>
        /// The player country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's
        /// "Detailed Spending Portfolio"), grouped Mandatory / Discretionary, plus Interest on Debt
        /// as a read-only automatic line. Mandatory lines show only their category name and current
        /// Amount - no slider, since reforming an entitlement program is a future mechanic, not a
        /// simple slider. Discretionary lines each get a this-turn dollar-change slider.
        /// </summary>
        private void DrawSpendingPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _spendingPolicyScrollPosition = GUILayout.BeginScrollView(_spendingPolicyScrollPosition, GUILayout.Height(scrollHeight));

            GUILayout.Label("Spending Policy", _headerStyle);
            GUILayout.Label("Mandatory programs are automatic and not yet player-adjustable. Discretionary categories take a this-turn dollar change.", _labelStyle);
            GUILayout.Space(8f);

            DrawInterestOnDebtRow();
            GUILayout.Space(10f);

            GUILayout.Label("Mandatory (automatic)", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawMandatorySpendingRow(spendingLine);
                GUILayout.Space(6f);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Discretionary", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawDiscretionarySpendingRow(spendingLine);
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

        private void DrawMandatorySpendingRow(SpendingLine spendingLine)
        {
            GUILayout.Label($"{spendingLine.Category} (automatic): {spendingLine.Amount:F1}", _labelStyle);
        }

        private void DrawDiscretionarySpendingRow(SpendingLine spendingLine)
        {
            float draftChange = GetSpendingLineInput(spendingLine.Category);
            GUILayout.Label($"{spendingLine.Category}: {spendingLine.Amount:F1}  Change: {draftChange:+0.0;-0.0;0}", _labelStyle);
            float newChange = GUILayout.HorizontalSlider(draftChange, -DiscretionaryLineChangeRange, DiscretionaryLineChangeRange, _sliderStyle, _sliderThumbStyle);
            _spendingLineInputs[spendingLine.Category] = newChange;
        }
    }
}
