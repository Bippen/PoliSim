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

        public TradePartner() { }

        public TradePartner(CountryId partnerId, float exportVolume, float importVolume)
        {
            PartnerId = partnerId;
            ExportVolume = exportVolume;
            ImportVolume = importVolume;
        }
    }
}
