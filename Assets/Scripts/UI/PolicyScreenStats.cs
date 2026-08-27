using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>
    /// Master Sequence step 9, Step B2: resolves WHICH stats belong on a given policy screen, so each
    /// screen can show the numbers its own levers actually move instead of the same global headline row
    /// everywhere.
    ///
    /// **The mapping is derived, not authored.** Every relationship comes from
    /// <see cref="PolicyWebRenderer"/>'s edge list, whose entries are each sourced from a real
    /// MacroSystem/SimulationManager formula (see that class's own per-group comments). Nothing here
    /// invents a policy-to-stat link: this class only asks the existing graph "which stats do the levers
    /// on this screen touch, and which do they touch most", and orders the answer. That means the day an
    /// edge is added, corrected or removed in the Policy Web, every policy screen's contextual stat row
    /// follows automatically - there is no second list to keep in sync, which is the failure mode a
    /// hand-written screen-to-stat table would have guaranteed.
    ///
    /// Formatting delegates rather than reimplementing - the direct lesson of the StatTile formatting
    /// bug, where a second, independently-written formatter is exactly how "GDP shows 9,3" happened.
    /// Currency goes to <see cref="UiFormat.Money"/> with the unit read from the stat's own metadata;
    /// the remaining per-stat units below are taken from the existing display call sites in
    /// GameController, not chosen fresh.
    /// </summary>
    public static class PolicyScreenStats
    {
        // R-K1 (2026-08-28): the edge set is per country now (PolicyWebRenderer.IsLiveFor), so the
        // cache is keyed by area AND by the one country fact that changes the set - whether the
        // issuance rate is pinned (the USA) - rather than by country, which would recompute the same
        // list five times.
        private static readonly Dictionary<(UiPalette.SystemArea, bool), List<StatNodeId>> Cache = new Dictionary<(UiPalette.SystemArea, bool), List<StatNodeId>>();

        private static bool RatePinned(Country country) => country != null && country.BaseDebtInterestRateOverride >= 0f;

        /// <summary>
        /// The stats this screen's own policy levers genuinely affect, most-affected first.
        ///
        /// Ranking is by how many of THIS screen's levers touch the stat - a stat six of the welfare
        /// programs move belongs above one a single lever nudges - with ties broken by enum order so the
        /// result is deterministic rather than dependent on dictionary iteration. Returns an empty list
        /// for an area with no policy nodes (Neutral), which callers should treat as "draw nothing"
        /// rather than "fall back to the global row". <paramref name="country"/> selects the live edge
        /// set (R-K1); null is the full set.
        /// </summary>
        public static IReadOnlyList<StatNodeId> GetStatsForArea(UiPalette.SystemArea area, Country country = null)
        {
            var key = (area, RatePinned(country));
            if (Cache.TryGetValue(key, out List<StatNodeId> cached))
            {
                return cached;
            }

            var edgeCounts = new Dictionary<StatNodeId, int>();
            foreach (PolicyNodeId policy in System.Enum.GetValues(typeof(PolicyNodeId)))
            {
                if (PolicyWebRenderer.GetPolicyArea(policy) != area)
                {
                    continue;
                }

                foreach (PolicyWebEdge edge in PolicyWebRenderer.GetEdgesFor(policy, country))
                {
                    edgeCounts.TryGetValue(edge.Target, out int count);
                    edgeCounts[edge.Target] = count + 1;
                }
            }

            var ordered = new List<StatNodeId>(edgeCounts.Keys);
            ordered.Sort((a, b) =>
            {
                int byCount = edgeCounts[b].CompareTo(edgeCounts[a]);
                return byCount != 0 ? byCount : ((int)a).CompareTo((int)b);
            });

            Cache[key] = ordered;
            return ordered;
        }

        /// <summary>
        /// How many of this screen's levers move this stat. Exposed so a caller can show the strongest
        /// few and honestly say how many were omitted, rather than silently truncating.
        /// </summary>
        public static int GetLeverCount(UiPalette.SystemArea area, StatNodeId stat, Country country = null)
        {
            int count = 0;
            foreach (PolicyNodeId policy in System.Enum.GetValues(typeof(PolicyNodeId)))
            {
                if (PolicyWebRenderer.GetPolicyArea(policy) != area)
                {
                    continue;
                }

                foreach (PolicyWebEdge edge in PolicyWebRenderer.GetEdgesFor(policy, country))
                {
                    if (edge.Target == stat) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// The stat's current LIVE value. Deliberately live rather than published: this is a policy
        /// screen showing what the player's own levers are doing right now, not a press release. The
        /// published/lagged view belongs to the Statistics tab's own graphs (Step A), and mixing the two
        /// would misrepresent an unrevised preliminary figure as the state of the world.
        /// </summary>
        public static float ReadLiveValue(StatNodeId stat, Country country)
        {
            EconomyState state = country.State;
            switch (stat)
            {
                case StatNodeId.Gdp: return state.GDP;
                case StatNodeId.Unemployment: return state.Unemployment;
                case StatNodeId.Inflation: return state.Inflation;
                case StatNodeId.Approval: return state.ApprovalRating;
                case StatNodeId.DebtToGdp: return state.DebtToGdpRatio;
                case StatNodeId.Poverty: return state.PovertyRate;
                case StatNodeId.InterestRate: return country.CurrencyZone.InterestRate;
                case StatNodeId.TradeBalance: return state.TradeBalance;
                case StatNodeId.Lfpr: return state.LaborForceParticipationRate;
                case StatNodeId.Crime: return state.CrimeIndex;
                case StatNodeId.PrisonPopulation: return state.PrisonPopulationRate;
                case StatNodeId.OrganizedCrime: return state.OrganizedCrimeIndex;
                case StatNodeId.Corruption: return state.CorruptionIndex;
                case StatNodeId.PotentialGrowth: return country.PotentialGrowthRate;
                case StatNodeId.PopulationGrowthRate: return state.PopulationGrowthRate;
                case StatNodeId.DependencyRatio: return state.DependencyRatio;
                // Q2's single book: the displayed confidence IS the effective one - the same
                // number the GDP identity consumes. The stored field is the policy-drift base
                // and no surface shows it as "confidence" (R-Q2a's rider).
                case StatNodeId.ConsumerConfidence: return MacroSystem.EffectiveConsumerConfidence(country);
                case StatNodeId.BusinessConfidence: return state.BusinessConfidence;
                default: return 0f;
            }
        }

        /// <summary>
        /// Display text for a value, in the unit that stat is already displayed in elsewhere in this UI.
        ///
        /// Money asks the stat's own metadata for its unit and renders through UiFormat.Money, rather
        /// than switching on the stat here. That is deliberate: the previous version routed GDP and
        /// TradeBalance through GraphRenderer.FormatAxisValue - correctly, by the reasoning available at
        /// the time - and inherited its unit bug, so these chips showed "29k" for $29T. A local list of
        /// which stats are money is a second copy of a fact that now has one home.
        /// </summary>
        public static string Format(StatNodeId stat, float value)
        {
            MoneyUnit? unit = PolicyWebRenderer.GetStatUnit(stat);
            if (unit.HasValue)
            {
                return UiFormat.Money(value, unit.Value);
            }

            switch (stat)
            {
                case StatNodeId.Unemployment:
                case StatNodeId.Inflation:
                case StatNodeId.Approval:
                case StatNodeId.DebtToGdp:
                case StatNodeId.Poverty:
                case StatNodeId.InterestRate:
                case StatNodeId.Lfpr:
                case StatNodeId.PotentialGrowth:
                    return $"{value:F1}%";

                // Per-1,000/yr, signed - matching GameController's own demographics line.
                case StatNodeId.PopulationGrowthRate:
                    return $"{value:+0.0;-0.0}/1k";

                case StatNodeId.PrisonPopulation:
                    return $"{value:F0}/100k";

                // Confidence indices are centered on 1.0, so F1 would collapse the range players
                // actually see move.
                case StatNodeId.ConsumerConfidence:
                case StatNodeId.BusinessConfidence:
                    return $"{value:F2}";

                // 0-100 indices and the dependency ratio.
                default:
                    return $"{value:F1}";
            }
        }

        /// <summary>
        /// Whether a rise in this stat is good, bad, or genuinely neither (null - inflation and interest
        /// rates have no context-free direction). Forwarded from the Policy Web's own framing so the two
        /// surfaces cannot disagree about whether a number turning green makes sense.
        /// </summary>
        public static bool? GetHigherIsBetter(StatNodeId stat) => PolicyWebRenderer.GetStatHigherIsBetter(stat);

        /// <summary>Display name, forwarded so callers need only this one class.</summary>
        public static string GetName(StatNodeId stat) => PolicyWebRenderer.GetStatName(stat);
    }
}
