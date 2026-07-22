namespace PoliSim.Data
{
    /// <summary>
    /// Individual tax instruments a country's fiscal portfolio can implement (see Country.TaxLines).
    /// Tariffs is included for completeness but deliberately never gets a TaxLine - tariff revenue
    /// is already handled by the existing BaseTariffRate/TradeSystem mechanism, not duplicated here.
    /// </summary>
    public enum TaxType
    {
        IncomeTax,
        CorporateTax,
        VAT,
        PayrollTax,
        CapitalGainsTax,
        SalesTax,
        ExciseTax,
        PropertyTax,
        EstateTax,
        WealthTax,
        CarbonTax,
        Tariffs,
        StampDuty
    }
}
