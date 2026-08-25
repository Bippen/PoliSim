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
        Administration,

        // Playtest-2 item 4 (ruled 2026-08-25): Sweden's real utgiftsomrade structure (see
        // WorldFactory.SeedSwedenSpendingLines for the sourced mapping, consolidations, and the
        // deliberate all-discretionary deviation). APPEND-ONLY - the enum serializes into saves,
        // so existing members' order above must never change. Named generically (LaborMarket, not
        // ArbetsmarknadOchArbetsliv) so a future country pass reuses them rather than growing a
        // per-country namespace; the Swedish specifics live in the seed's own comments.
        CentralGovernment,
        FinancialAdministration,
        TaxAdministration,
        InternationalAid,
        Migration,
        HealthcareAndSocialCare,
        SicknessAndDisability,
        FamilyAndChildren,
        IntegrationAndEquality,
        LaborMarket,
        StudentAid,
        CultureAndMedia,
        RegionalPlanningAndDevelopment,
        ClimateAndEnvironment,
        BusinessAndIndustry,
        MunicipalGrants,
        EuMembershipFee
    }
}
