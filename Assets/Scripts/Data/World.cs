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

        public Country GetCountry(CountryId id)
        {
            return Countries.Find(c => c.Id == id);
        }
    }
}
