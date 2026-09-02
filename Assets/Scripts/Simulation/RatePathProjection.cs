using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P2-3.4 (Playtest 2, 2026-09-02) — **the projected policy-rate path**, derived and scoped. Three
    /// steps for a country with a sitting chair: the rate today; the rate after this year's move, the chair
    /// closing <see cref="FederalReserveSystem.RateAdjustmentSpeed"/> of the gap to a target that is the
    /// Taylor rule on today's readings plus the chair's lean, clamped to the band (exactly what
    /// <see cref="FederalReserveSystem.ApplyFedChairInterestRate"/> does at a turn close); and the rate after
    /// next year's move toward a target that is the same rule on the preview's readings - the deterministic
    /// year run with the current draft (<see cref="PolicyPreview.PreviewRuleRate"/>, the rule evaluated on
    /// the previewed clone at its year's end). Nothing is forecast beyond what the preview already
    /// computes; the preview-parity diagnostic re-derives every figure from the rule's own formula.
    /// </summary>
    public static class RatePathProjection
    {
        public readonly struct Step
        {
            public readonly int YearsAhead;
            /// <summary>The rule's reading the step's target rests on.</summary>
            public readonly float RuleReading;
            /// <summary>The chair's target: the reading plus the lean, clamped to the band.</summary>
            public readonly float Target;
            /// <summary>The policy rate at the step.</summary>
            public readonly float Rate;
            public readonly string Basis;
            public Step(int yearsAhead, float ruleReading, float target, float rate, string basis)
            {
                YearsAhead = yearsAhead; RuleReading = ruleReading; Target = target; Rate = rate; Basis = basis;
            }
        }

        /// <summary>The chair's target for a rule reading: reading plus lean, clamped to the band.</summary>
        public static float TargetFor(float ruleReading, FedChair chair) =>
            Mathf.Clamp(ruleReading + chair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);

        /// <summary>One year's move: the chair closes the adjustment speed's share of the gap to the target.</summary>
        public static float Move(float rate, float target) => rate + (target - rate) * FederalReserveSystem.RateAdjustmentSpeed;

        /// <summary>Null when the country has no sitting chair (a currency-zone member or a rate the player sets).</summary>
        public static Step[] Project(Country country, PolicyPreview preview)
        {
            FedChair chair = country?.CurrentFedChair;
            if (chair == null || preview == null || country.CurrencyZone == null) { return null; }
            float rateNow = country.CurrencyZone.InterestRate;
            float ruleNow = TaylorRule.GetSuggestedInterestRate(country);
            float targetNow = TargetFor(ruleNow, chair);
            float afterThisYear = Move(rateNow, targetNow);
            float ruleNext = preview.PreviewRuleRate;
            float targetNext = TargetFor(ruleNext, chair);
            float afterNextYear = Move(afterThisYear, targetNext);
            return new[]
            {
                new Step(0, ruleNow, targetNow, rateNow, "today's readings"),
                new Step(1, ruleNow, targetNow, afterThisYear, "the rule on today's readings, the chair's lean, the gap closed at the chair's speed"),
                new Step(2, ruleNext, targetNext, afterNextYear, "the rule on the preview's year-end readings - this draft, deterministic, no events"),
            };
        }
    }
}
