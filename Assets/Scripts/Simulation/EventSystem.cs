using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// One canned economic event: a one-turn shock plus UI-facing text. Deliberately plain data -
    /// EventSystem.EventPool is hardcoded for now, but nothing downstream (EventSystem.ApplyEvent,
    /// or a UI reading SimulationManager.GetLastEvent) cares where an EconomicEvent came from, so the
    /// pool can later be swapped for AI-generated events without changing how they're consumed.
    /// </summary>
    public class EconomicEvent
    {
        public string Name;
        public string Description;
        public float GdpShockPercent;
        public float InflationShockPoints;
        public float ApprovalEffect;
    }

    /// <summary>
    /// Small random-event layer: each turn, a small chance an event fires for a given country, with
    /// a one-time GDP/inflation shock plus an approval effect. A game-rule heuristic, not economic
    /// theory, so it's kept out of MacroSystem. GDP shocks fade on their own via MacroSystem's
    /// existing output-gap reversion (PotentialGDP is untouched by events, so actual GDP drifts back
    /// toward it); inflation shocks are genuinely one-turn, since the Phillips Curve fully
    /// recomputes Inflation from scratch next turn rather than carrying a delta forward.
    /// </summary>
    public static class EventSystem
    {
        /// <summary>Chance per country per turn that an event fires - small, so events read as notable, not background noise.</summary>
        private const float EventChancePerTurn = 0.12f;

        private static readonly System.Random RandomSource = new System.Random();

        private static readonly List<EconomicEvent> EventPool = new List<EconomicEvent>
        {
            new EconomicEvent
            {
                Name = "Recession in a Trading Partner",
                Description = "A major trading partner has slipped into recession, softening demand for our exports.",
                GdpShockPercent = -2.0f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Global Commodity Price Spike",
                Description = "A spike in global commodity prices is pushing up costs across the economy.",
                GdpShockPercent = -1.0f, InflationShockPoints = 1.5f, ApprovalEffect = -4f
            },
            new EconomicEvent
            {
                Name = "Diplomatic Dispute With a Trade Partner",
                Description = "A diplomatic dispute has disrupted trade with one of our partners.",
                GdpShockPercent = -1.5f, InflationShockPoints = 0.3f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Technology Breakthrough",
                Description = "A domestic technology breakthrough is boosting productivity.",
                GdpShockPercent = 1.5f, InflationShockPoints = 0f, ApprovalEffect = 3f
            },
            new EconomicEvent
            {
                Name = "Favorable Trade Agreement",
                Description = "A new trade agreement is opening up favorable terms with a partner.",
                GdpShockPercent = 1.0f, InflationShockPoints = -0.2f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Natural Disaster",
                Description = "A natural disaster has disrupted economic activity in the country.",
                GdpShockPercent = -2.5f, InflationShockPoints = 0.5f, ApprovalEffect = -5f
            },
            new EconomicEvent
            {
                Name = "Consumer Confidence Surge",
                Description = "A wave of optimism is boosting consumer spending.",
                GdpShockPercent = 1.0f, InflationShockPoints = 0.2f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Energy Price Shock",
                Description = "A sudden spike in energy prices is squeezing households and businesses.",
                GdpShockPercent = -1.5f, InflationShockPoints = 1.0f, ApprovalEffect = -3f
            },
        };

        /// <summary>Rolls whether an event fires this turn; returns null (no event) most of the time.</summary>
        public static EconomicEvent TryRollEvent()
        {
            if (RandomSource.NextDouble() > EventChancePerTurn)
            {
                return null;
            }

            int index = RandomSource.Next(EventPool.Count);
            return EventPool[index];
        }

        /// <summary>Applies a one-time GDP/inflation/approval shock. No-op if economicEvent is null (the common case - no event this turn).</summary>
        public static void ApplyEvent(Country country, EconomicEvent economicEvent)
        {
            if (economicEvent == null)
            {
                return;
            }

            EconomyState state = country.State;
            state.GDP = Mathf.Max(MacroSystem.MinGdp, state.GDP * (1f + economicEvent.GdpShockPercent / 100f));
            state.Inflation = Mathf.Clamp(state.Inflation + economicEvent.InflationShockPoints, 0f, MacroSystem.MaxInflationPercent);
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + economicEvent.ApprovalEffect, 0f, 100f);
        }
    }
}
