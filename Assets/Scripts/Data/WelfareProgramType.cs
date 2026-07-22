namespace PoliSim.Data
{
    /// <summary>
    /// Welfare/anti-poverty programs a country's welfare portfolio can implement (see
    /// Country.WelfarePrograms). Mirrors TaxType/TaxLine's implement/adjust/remove pattern - see
    /// WelfareProgram for the instrument itself and WelfareProgramCostShares for each type's cost.
    /// </summary>
    public enum WelfareProgramType
    {
        UBI,
        NegativeIncomeTax,
        MeansTestedWelfare,
        UniversalHealthcare,
        HousingAssistance,
        ChildcareSubsidies
    }
}
