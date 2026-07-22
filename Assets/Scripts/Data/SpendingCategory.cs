namespace PoliSim.Data
{
    /// <summary>
    /// Individual government spending line items a country's budget can track (see
    /// Country.SpendingLines/SpendingLine). Mandatory categories are automatic entitlement/transfer
    /// programs, not player-sliderable in Phase 1 (see GameController's Tax... spending panel) -
    /// only Discretionary categories take a this-turn delta via
    /// PolicyDecision.SpendingLineChanges. InterestOnDebt is deliberately NOT a category here - it
    /// stays SimulationManager's existing automatic, non-editable GetInterestOnDebt calculation, not
    /// a seeded line.
    /// </summary>
    public enum SpendingCategory
    {
        // Mandatory
        SocialSecurity,
        Medicare,
        Medicaid,
        IncomeSecurity,
        VeteransBenefitsMandatory,
        FederalRetirement,

        // Discretionary
        Defense,
        VeteransAffairsDiscretionary,
        Transportation,
        HHSDiscretionary,
        HomelandSecurity,
        Education,
        Energy,
        Housing,
        Justice,
        StateForeignAffairs,
        Agriculture,
        Interior,
        NASA,
        Commerce,
        Labor,
        TreasuryOps,
        NSF,
        EPA,
        SBA
    }
}
