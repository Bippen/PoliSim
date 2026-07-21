using System;

namespace PoliSim.Data
{
    /// <summary>
    /// A monetary union with a single shared interest rate. Countries that share a currency
    /// (e.g. the Eurozone) reference the SAME CurrencyZone instance, so a rate change affects
    /// all of them at once; countries with their own currency get their own instance.
    /// </summary>
    [Serializable]
    public class CurrencyZone
    {
        public string Name;
        public float InterestRate;

        public CurrencyZone() { }

        public CurrencyZone(string name, float interestRate)
        {
            Name = name;
            InterestRate = interestRate;
        }
    }
}
