using System;
using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P4-C3 (2026-09-04): the structural per-country parameters a law may move - the quantities the lever map
    /// (LEVER_MAP.md §2) found no dial and no law reaching. Not player dials: each is a seeded constant of the model
    /// (the natural rate of unemployment the Phillips curve reads, the debt-comfort anchor the fiscal reaction reads,
    /// the debt stock's average maturity, the market's risk-premium sensitivity, tax-collection coverage, the
    /// baseline government-consumption share) that an enacted law of a structural category composes on.
    /// </summary>
    public enum StructuralParameter
    {
        NaturalUnemploymentRate,
        ComfortableDebtToGdpPercent,
        AverageDebtMaturityYears,
        RiskPremiumSensitivity,
        CollectionEfficiency,
        GovernmentSpendingRate
    }

    /// <summary>One law's move of one structural parameter, in the parameter's own unit, signed.</summary>
    public readonly struct StructuralDelta
    {
        public readonly StructuralParameter Parameter;
        public readonly float Delta;

        public StructuralDelta(StructuralParameter parameter, float delta)
        {
            Parameter = parameter;
            Delta = delta;
        }
    }

    /// <summary>
    /// The table that makes a structural effect ONE mechanism rather than one field per parameter (the first cut of
    /// P4-C3 carried the natural rate as a thirteenth DialDeltas entry with a special case in every consumer; the second
    /// category would have made that five special cases, so the table replaced it the same day). Per parameter: the
    /// name the card prints, the unit, the magnitude scale that puts the unit on LawCatalog's tier grid (MINOR 3-6,
    /// MODERATE 7-14, MAJOR 15-22, SWEEPING 23-30), the bounds the recompute clamps to, and how to read and write it on
    /// a Country. The seeded value is the BASE (Country.CaptureStructuralBases, called once the seeds are placed);
    /// SimulationManager.RecomputeStructuralParametersFromEnactedLaws sets value = base + the enacted set's summed
    /// deltas, clamped, FRESH every time - the clamp-safe idiom the crime dials taught (COMPOSITION).
    /// </summary>
    public static class StructuralParameters
    {
        public readonly struct Spec
        {
            public readonly StructuralParameter Parameter;
            public readonly string Name;
            public readonly string Unit;
            public readonly float Scale;   // [AUTHORED-DRAFT] tier-grid scale: delta x scale reads on the 0-30 magnitude grid
            public readonly float Min;     // CONVENTION - the bound the composed value is clamped to
            public readonly float Max;     // CONVENTION - the bound the composed value is clamped to
            public readonly Func<Country, float> Get;
            public readonly Func<Country, float> GetBase;
            public readonly Action<Country, float> Set;

            public Spec(StructuralParameter parameter, string name, string unit, float scale, float min, float max,
                Func<Country, float> get, Func<Country, float> getBase, Action<Country, float> set)
            {
                Parameter = parameter; Name = name; Unit = unit; Scale = scale; Min = min; Max = max;
                Get = get; GetBase = getBase; Set = set;
            }
        }

        /// <summary>The scales and bounds, one row per parameter. Scales: the natural rate speaks in percentage points
        /// (x15 puts -0.4 at MINOR's edge, -0.9 at MODERATE's, -1.4 at MAJOR's - Hartz IV's measured -1.4 pp on the
        /// MAJOR-SWEEPING line); the debt anchor in points of GDP (x1: a ten-point anchor move is MODERATE); maturity in
        /// years (x5: two years is MODERATE); the premium sensitivity as a multiplier (x60: 0.15 is MODERATE); collection
        /// coverage as a multiplier (x300: 0.03 is MODERATE); the spending share in points of GDP (x10: one point is
        /// MODERATE). Bounds: the seeds run 3.3-8.0, 35-138, 5.9-8.5, 0-1, 0.61-1.31 and 17-26 respectively; each bound
        /// sits outside what the OECD has measured for these six.</summary>
        public static readonly Spec[] All =
        {
            new Spec(StructuralParameter.NaturalUnemploymentRate, "Natural rate of unemployment", "pp", 15f, 2f, 12f,
                c => c.NaturalUnemploymentRate, c => c.NaturalUnemploymentRateBase, (c, v) => c.NaturalUnemploymentRate = v),
            new Spec(StructuralParameter.ComfortableDebtToGdpPercent, "Debt-comfort anchor", "pts of GDP", 1f, 20f, 180f,
                c => c.ComfortableDebtToGdpPercent, c => c.ComfortableDebtToGdpPercentBase, (c, v) => c.ComfortableDebtToGdpPercent = v),
            new Spec(StructuralParameter.AverageDebtMaturityYears, "Average debt maturity", "years", 5f, 2f, 20f,
                c => c.AverageDebtMaturityYears, c => c.AverageDebtMaturityYearsBase, (c, v) => c.AverageDebtMaturityYears = v),
            new Spec(StructuralParameter.RiskPremiumSensitivity, "Risk-premium sensitivity", "x", 60f, 0f, 2f,
                c => c.RiskPremiumSensitivity, c => c.RiskPremiumSensitivityBase, (c, v) => c.RiskPremiumSensitivity = v),
            new Spec(StructuralParameter.CollectionEfficiency, "Tax-collection coverage", "x", 300f, 0.3f, 2f,
                c => c.CollectionEfficiency, c => c.CollectionEfficiencyBase, (c, v) => c.CollectionEfficiency = v),
            new Spec(StructuralParameter.GovernmentSpendingRate, "Baseline spending share", "pts of GDP", 10f, 5f, 40f,
                c => c.GovernmentSpendingRate, c => c.GovernmentSpendingRateBase, (c, v) => c.GovernmentSpendingRate = v),
        };

        public static Spec Of(StructuralParameter parameter)
        {
            foreach (Spec s in All) { if (s.Parameter == parameter) { return s; } }
            throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "no spec row - StructuralParameters.All must grow with the enum");
        }

        /// <summary>The magnitude-grid points of one structural delta (|delta| x the parameter's scale).</summary>
        public static float GridPoints(StructuralDelta delta) => Mathf.Abs(delta.Delta) * Of(delta.Parameter).Scale;
    }
}
