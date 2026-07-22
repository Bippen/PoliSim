namespace PoliSim.Data
{
    /// <summary>
    /// Individual government spending line items a country's budget can track (see
    /// Country.SpendingLines/SpendingLine). Both Mandatory and Discretionary categories take a
    /// this-turn PERCENTAGE change via PolicyDecision.SpendingLineChanges - Mandatory categories
    /// (entitlement/transfer programs) get a narrower range reflecting the real political difficulty
    /// of entitlement reform, and a distinctly higher approval-rating penalty per relative size of
    /// change - see SimulationManager.ApplySpendingLineChanges and MacroSystem.
    /// MandatorySpendingApprovalMultiplier. InterestOnDebt is deliberately NOT a category here - it
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
