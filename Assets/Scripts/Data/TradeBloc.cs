using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// A trade bloc (e.g. the EU): its members trade with each other at a near-zero internal
    /// tariff rate, and apply a single common tariff rate to imports from every non-member.
    /// </summary>
    [Serializable]
    public class TradeBloc
    {
        public string Name;
        public List<CountryId> Members = new List<CountryId>();

        /// <summary>Common tariff rate, as a percentage, applied by every member to imports from non-members.</summary>
        public float ExternalTariffRate;

        /// <summary>Tariff rate, as a percentage, applied between members. Near zero for a functioning bloc.</summary>
        public float InternalTariffRate;

        public TradeBloc() { }

        public TradeBloc(string name, List<CountryId> members, float externalTariffRate, float internalTariffRate)
        {
            Name = name;
            Members = members;
            ExternalTariffRate = externalTariffRate;
            InternalTariffRate = internalTariffRate;
        }

        public bool IsMember(CountryId id)
        {
            return Members.Contains(id);
        }
    }
}
