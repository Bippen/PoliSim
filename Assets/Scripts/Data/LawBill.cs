namespace PoliSim.Data
{
    /// <summary>
    /// A law reaches Parliament through this - the SAME gated-legislation model every other bill
    /// uses, generalized rather than reusing CrimeJusticePolicyBill or PolicyDecision directly (see
    /// ParliamentSystem.GetLawBillDirection/ApplyLawBillResult, and
    /// SimulationManager.IntroduceLawBill/AdvanceLawBillsDay/ApplyLawBillEffects, all of which mirror
    /// CrimeJusticePolicyBill's own Introduce/Advance/Direction/Apply shape exactly - same
    /// DaysRemaining countdown, same ParliamentSystem.WouldBillPass/RecordDivision core).
    ///
    /// Shape mirrors TaxProgramBill/WelfareProgramBill's own {Type/IsAdd, DaysRemaining} - a law
    /// bill is either an ENACTMENT or a REPEAL of one specific, already-cataloged law, never a
    /// bundle of several laws at once (unlike BudgetBill's Tax+Spending+Welfare+SWF bundling - a law
    /// is a single named preset, so one bill per law reads more honestly than one omnibus law bill
    /// would). Multiple different laws may each have their own LawBill pending for the same country
    /// simultaneously (SimulationManager's _pendingLawBillsByCountry is keyed CountryId -&gt; LawId -&gt;
    /// LawBill, the same nested-dictionary shape _pendingTaxProgramBillsByCountry already uses), just
    /// never two bills for the SAME law at once.
    /// </summary>
    public sealed class LawBill
    {
        /// <summary>Which cataloged LawDefinition this bill enacts or repeals - see LawCatalog.GetById.</summary>
        public string LawId;

        /// <summary>False = enact (the law is not currently in force); true = repeal (it is). The UI only ever offers the action that matches the law's current EnactedLaws membership, so this should never be set inconsistently with it - but the bill itself doesn't re-check, the same trust-the-caller shape TaxProgramBill.IsAdd already has.</summary>
        public bool IsRepeal;

        public int DaysRemaining;
    }
}
