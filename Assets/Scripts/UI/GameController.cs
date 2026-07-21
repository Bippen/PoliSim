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
            TradeAndSpending
        }

        private const CountryId PlayerCountryId = CountryId.USA;
        private const int MaxLogEntries = 10;

        private const float TaxRateChangeRange = 10f;
        private const float GovernmentSpendingRange = 2000f;
        private const float InterestRateChangeRange = 2f;

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

        private float _taxRateChangeInput;
        private float _governmentSpendingInput;
        private float _interestRateChangeInput;

        private readonly List<string> _turnLog = new List<string>();
        private Vector2 _logScrollPosition;

        private RightPanelTab _rightPanelTab = RightPanelTab.RecentTurns;
        private Vector2 _tradeAndSpendingScrollPosition;

        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _tabButtonSelectedStyle;

        private void Start()
        {
            _world = WorldFactory.CreateDefault();
            _simulationManager = gameObject.AddComponent<SimulationManager>();
            _simulationManager.SetWorld(_world);

            _playerCountry = _world.GetCountry(PlayerCountryId);
            _prevGdp = _playerCountry.State.GDP;
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
            DrawDashboard();
            GUILayout.Space(sectionSpacing);
            DrawPolicyControls();
            GUILayout.Space(sectionSpacing);
            DrawAdvanceTurnButton();
            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);

            GUILayout.BeginVertical(GUILayout.Width(rightColumnWidth));
            DrawRightColumnTabs();
            GUILayout.Space(sectionSpacing * 0.5f);

            float tabContentHeight = areaHeight - _tabButtonStyle.fixedHeight - sectionSpacing * 0.5f;
            if (_rightPanelTab == RightPanelTab.RecentTurns)
            {
                DrawTurnLog(tabContentHeight);
            }
            else
            {
                DrawTradeAndSpending(tabContentHeight);
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

            _stylesInitialized = true;
        }

        /// <summary>Re-derives every style's font size/control size from the current screen size every frame (cheap field writes, no allocation) so a live window resize stays legible instead of squinting-small.</summary>
        private void RescaleStylesToScreen()
        {
            int headerFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 42);
            int labelFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 16, 28);
            int buttonFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 38);
            int tabFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 18, 30);
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
            GUILayout.Label($"Interest Rate: {_playerCountry.CurrencyZone.InterestRate:F2}%", _labelStyle);

            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }

            GUILayout.Label($"Government Debt: {state.GovernmentDebt:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);
            GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawPolicyControls()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("This Turn's Policy", _headerStyle);

            GUILayout.Label($"Tax Rate Change: {_taxRateChangeInput:+0.0;-0.0;0} pts", _labelStyle);
            _taxRateChangeInput = GUILayout.HorizontalSlider(_taxRateChangeInput, -TaxRateChangeRange, TaxRateChangeRange, _sliderStyle, _sliderThumbStyle);

            GUILayout.Label($"Government Spending Change: {_governmentSpendingInput:+0.0;-0.0;0} units", _labelStyle);
            _governmentSpendingInput = GUILayout.HorizontalSlider(_governmentSpendingInput, -GovernmentSpendingRange, GovernmentSpendingRange, _sliderStyle, _sliderThumbStyle);

            // Shared-currency countries (e.g. Eurozone members) don't set their own rate - only show
            // this control for a country with an independent CurrencyZone, same test CurrencySystem uses.
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Interest Rate Change: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -InterestRateChangeRange, InterestRateChangeRange, _sliderStyle, _sliderThumbStyle);
            }

            GUILayout.EndVertical();
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
        }

        private PolicyDecision BuildPlayerDecision()
        {
            return new PolicyDecision
            {
                TaxRateChange = _taxRateChangeInput,
                GovernmentSpending = _governmentSpendingInput,
                InterestRateChange = _interestRateChangeInput
            };
        }

        private void ResetPolicyInputs()
        {
            _taxRateChangeInput = 0f;
            _governmentSpendingInput = 0f;
            _interestRateChangeInput = 0f;
        }

        private void AppendLogEntry(EconomyState state)
        {
            _turnLog.Add($"Turn {_simulationManager.CurrentTurn}: GDP={state.GDP:F1} ({_lastGrowthPercent:+0.00;-0.00;0}%), " +
                $"Unemp={state.Unemployment:F2}%, Infl={state.Inflation:F2}%, Debt/GDP={state.DebtToGdpRatio:F1}%");

            while (_turnLog.Count > MaxLogEntries)
            {
                _turnLog.RemoveAt(0);
            }
        }

        /// <summary>Tab/toggle pair for the right column - "Recent Turns" | "Trade &amp; Spending". The selected tab gets a distinct (bold, colored) style so it's visibly different from the skin's default button look.</summary>
        private void DrawRightColumnTabs()
        {
            GUILayout.BeginHorizontal();

            GUIStyle recentTurnsStyle = _rightPanelTab == RightPanelTab.RecentTurns ? _tabButtonSelectedStyle : _tabButtonStyle;
            if (GUILayout.Button("Recent Turns", recentTurnsStyle))
            {
                _rightPanelTab = RightPanelTab.RecentTurns;
            }

            GUIStyle tradeAndSpendingStyle = _rightPanelTab == RightPanelTab.TradeAndSpending ? _tabButtonSelectedStyle : _tabButtonStyle;
            if (GUILayout.Button("Trade & Spending", tradeAndSpendingStyle))
            {
                _rightPanelTab = RightPanelTab.TradeAndSpending;
            }

            GUILayout.EndHorizontal();
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
                - report.BaselineGovernmentSpending - report.DiscretionarySpending
                - report.UnemploymentBenefitCost - report.InterestOnDebt;

            GUILayout.Label($"Revenue (Tax): {report.Revenue:F1}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {report.BaselineGovernmentSpending:F1}", _labelStyle);
            GUILayout.Label($"Discretionary Spending: {report.DiscretionarySpending:F1}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {report.UnemploymentBenefitCost:F1}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {report.InterestOnDebt:F1}", _labelStyle);
            GUILayout.Label($"Tariff Revenue Collected: {report.TariffRevenue:F1}", _labelStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"Net (matches this turn's Budget change): {net:+0.0;-0.0;0}", _headerStyle);
        }
    }
}
