using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>All countries and trade blocs in play for a given simulation.</summary>
    [Serializable]
    public class World
    {
        public List<Country> Countries = new List<Country>();
        public List<TradeBloc> TradeBlocs = new List<TradeBloc>();

        /// <summary>P4-C3 third category (2026-09-05): true when no other country shares this country's CurrencyZone INSTANCE - the parliament
        /// owns its bank (Sweden, Poland, the USA). Reference identity, as the save's zone groups use.</summary>
        public bool OwnsCurrencyZone(Country country)
        {
            if (country?.CurrencyZone == null) { return false; }
            foreach (Country other in Countries)
            {
                if (!ReferenceEquals(other, country) && ReferenceEquals(other.CurrencyZone, country.CurrencyZone)) { return false; }
            }
            return true;
        }

        public Country GetCountry(CountryId id)
        {
            return Countries.Find(c => c.Id == id);
        }
    }
}
