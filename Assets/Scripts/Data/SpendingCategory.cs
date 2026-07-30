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
        SBA,

        // Country-selection task, Part 2: broad generic categories for Sweden/Germany/France/Italy/
        // Poland's spending decomposition - deliberately NOT the granular USA-style breakdown above
        // (this mirrors USA's own original Phase 1 broad-categories stage, not its later detailed
        // work). All five are Discretionary for every country that uses them (see WorldFactory.
        // SeedGenericSpendingLines) - no Mandatory/Discretionary split was introduced for this small
        // decomposition, since these countries' actual transfer/entitlement spending is already
        // covered by the separate, pre-existing WelfarePrograms portfolio, not by these lines.
        SocialPrograms,
        InfrastructureAndDevelopment,
        PublicServices,
        Administration
    }
}
