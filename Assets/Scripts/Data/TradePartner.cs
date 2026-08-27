using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One bilateral trade relationship, from the owning country's point of view.
    /// Volumes are in the same abstract currency units as GDP.
    /// </summary>
    [Serializable]
    public class TradePartner
    {
        public CountryId PartnerId;

        /// <summary>Goods/services this country sells to the partner, before tariff effects.</summary>
        public float ExportVolume;

        /// <summary>Goods/services this country buys from the partner, before tariff effects.</summary>
        public float ImportVolume;

        /// <summary>
        /// This country's own player-set tariff rate specifically for imports from this one partner,
        /// or -1f (the default) if unset. -1f means "no override" - TradeSystem.GetTariffRate falls
        /// through to its existing bloc/base-rate resolution unchanged. When set (>= 0), it is the
        /// MOST specific rate and wins over even trade-bloc membership - see GetTariffRate's
        /// precedence. Only ever set on the PLAYER'S own TradePartner links (via
        /// SimulationManager.ApplyPartnerTariffOverrides/GameController's Trade tab); an AI-controlled
        /// country's links are never touched and stay at the default forever. Amended, pass 6
        /// (2026-08-27): the links are still never written, but the RATE a partner charges on this
        /// country's exports now responds to this field - TradeSystem.GetRetaliatoryTariffRate mirrors
        /// the excess of this override over the rate the owner would otherwise charge that partner,
        /// computed from this link on the fly (never stored on the partner's).
        /// </summary>
        public float PlayerTariffOverride = -1f;

        /// <summary>True if PlayerTariffOverride has been set to a specific rate (>= 0) rather than left at its "no override" default.</summary>
        public bool HasPlayerTariffOverride => PlayerTariffOverride >= 0f;

        public TradePartner() { }

        public TradePartner(CountryId partnerId, float exportVolume, float importVolume)
        {
            PartnerId = partnerId;
            ExportVolume = exportVolume;
            ImportVolume = importVolume;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - PlayerTariffOverride is mutated by ApplyPartnerTariffOverrides, so the preview needs its own copy, not a shared reference (the same reasoning TaxLines/SpendingLines already required).</summary>
        public TradePartner Clone()
        {
            return new TradePartner(PartnerId, ExportVolume, ImportVolume) { PlayerTariffOverride = PlayerTariffOverride };
        }
    }
}
