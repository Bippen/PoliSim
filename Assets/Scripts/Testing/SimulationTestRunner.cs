using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// Debug tool: runs the default six-country World for a fixed number of turns with no player
    /// input, logging per-turn/per-country state and a final summary flagging anything that looks
    /// like a runaway feedback loop or invalid value. Not production code.
    /// </summary>
    public class SimulationTestRunner : MonoBehaviour
    {
        private const int TurnsToRun = 100;
        private const float MaxUnemploymentPercent = 50f;
        private const float MaxInflationPercent = 50f;
        private const float MaxSingleTurnChangePercent = 20f;

        private class Snapshot
        {
            public float GDP;
            public float Unemployment;
            public float Inflation;
            public float InterestRate;
        }

        private void Start()
        {
            World world = WorldFactory.CreateDefault();
            SimulationManager simulationManager = gameObject.AddComponent<SimulationManager>();
            simulationManager.SetWorld(world);

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            var previous = new Dictionary<CountryId, Snapshot>();
            foreach (Country country in world.Countries)
            {
                decisions[country.Id] = PolicyDecision.None();
                previous[country.Id] = SnapshotOf(country);
            }

            var anomalies = new List<string>();

            for (int turn = 1; turn <= TurnsToRun; turn++)
            {
                simulationManager.AdvanceTurn(decisions);

                foreach (Country country in world.Countries)
                {
                    EconomyState state = country.State;
                    Snapshot prev = previous[country.Id];
                    float growthPercent = (state.GDP - prev.GDP) / Mathf.Max(prev.GDP, 1f) * 100f;

                    Debug.Log($"Turn {turn} | {country.Name}: GDP={state.GDP:F1} ({growthPercent:+0.00;-0.00}%), " +
                        $"Unemployment={state.Unemployment:F2}%, Inflation={state.Inflation:F2}%, " +
                        $"InterestRate={country.CurrencyZone.InterestRate:F2}%");

                    CheckAnomalies(turn, country, state, prev, anomalies);

                    previous[country.Id] = SnapshotOf(country);
                }
            }

            LogSummary(anomalies);
        }

        private static Snapshot SnapshotOf(Country country)
        {
            return new Snapshot
            {
                GDP = country.State.GDP,
                Unemployment = country.State.Unemployment,
                Inflation = country.State.Inflation,
                InterestRate = country.CurrencyZone.InterestRate
            };
        }

        private static void CheckAnomalies(int turn, Country country, EconomyState state, Snapshot previous, List<string> anomalies)
        {
            if (state.GDP < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: negative GDP ({state.GDP:F1})");
            }

            if (state.Unemployment < 0f || state.Unemployment > MaxUnemploymentPercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: unemployment out of range ({state.Unemployment:F2}%)");
            }

            if (state.Inflation < 0f || state.Inflation > MaxInflationPercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: inflation out of range ({state.Inflation:F2}%)");
            }

            CheckFinite(turn, country, "GDP", state.GDP, anomalies);
            CheckFinite(turn, country, "Inflation", state.Inflation, anomalies);
            CheckFinite(turn, country, "Unemployment", state.Unemployment, anomalies);
            CheckFinite(turn, country, "ApprovalRating", state.ApprovalRating, anomalies);
            CheckFinite(turn, country, "Budget", state.Budget, anomalies);
            CheckFinite(turn, country, "TaxRate", state.TaxRate, anomalies);
            CheckFinite(turn, country, "TradeBalance", state.TradeBalance, anomalies);
            CheckFinite(turn, country, "CurrencyStrength", state.CurrencyStrength, anomalies);
            CheckFinite(turn, country, "Consumption", state.Consumption, anomalies);
            CheckFinite(turn, country, "Investment", state.Investment, anomalies);
            CheckFinite(turn, country, "PotentialGDP", state.PotentialGDP, anomalies);
            CheckFinite(turn, country, "InflationExpectations", state.InflationExpectations, anomalies);
            CheckFinite(turn, country, "ConsumerConfidence", state.ConsumerConfidence, anomalies);
            CheckFinite(turn, country, "BusinessConfidence", state.BusinessConfidence, anomalies);
            CheckFinite(turn, country, "InterestRate", country.CurrencyZone.InterestRate, anomalies);

            CheckSwing(turn, country, "GDP", previous.GDP, state.GDP, anomalies);
            CheckSwing(turn, country, "Unemployment", previous.Unemployment, state.Unemployment, anomalies);
            CheckSwing(turn, country, "Inflation", previous.Inflation, state.Inflation, anomalies);
            CheckSwing(turn, country, "InterestRate", previous.InterestRate, country.CurrencyZone.InterestRate, anomalies);
        }

        private static void CheckFinite(int turn, Country country, string field, float value, List<string> anomalies)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                anomalies.Add($"Turn {turn} {country.Name}: {field} is not finite ({value})");
            }
        }

        private static void CheckSwing(int turn, Country country, string field, float previousValue, float currentValue, List<string> anomalies)
        {
            if (Mathf.Abs(previousValue) < 0.01f)
            {
                return;
            }

            float changePercent = Mathf.Abs(currentValue - previousValue) / Mathf.Abs(previousValue) * 100f;
            if (changePercent > MaxSingleTurnChangePercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: {field} swung {changePercent:F1}% in one turn ({previousValue:F2} -> {currentValue:F2})");
            }
        }

        private static void LogSummary(List<string> anomalies)
        {
            if (anomalies.Count == 0)
            {
                Debug.Log($"Sanity check complete: {TurnsToRun} turns, no anomalies detected.");
                return;
            }

            Debug.LogWarning($"Sanity check complete: {TurnsToRun} turns, {anomalies.Count} anomalies detected:");
            foreach (string anomaly in anomalies)
            {
                Debug.LogWarning(anomaly);
            }
        }
    }
}
