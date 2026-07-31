namespace PoliSim.Data
{
    /// <summary>
    /// Master Sequence step 5d (Political Systems Overhaul Part B, full rollout - standalone
    /// program add/remove tier): implementing or removing ONE TaxType entirely, as its own
    /// individual bill, introducible anytime (not tied to the annual fiscal-year date the way
    /// BudgetBill's rate changes are). At most one pending TaxProgramBill per TaxType per country -
    /// see SimulationManager.IntroduceTaxProgramBill - so a second bill for the SAME TaxType can't
    /// be introduced while one's already before Parliament, but bills for DIFFERENT TaxTypes (or a
    /// WelfareProgramBill) can all be pending at once. Resolves via the exact same seat-weighted
    /// FiscalStance alignment scoring as BudgetBill (see ParliamentSystem.GetTaxProgramBillDirection),
    /// reusing the TaxLine's own already-persistent Rate as the direction magnitude - implementing
    /// turns that Rate "on" (a fiscal expansion, same sign as a rate hike), removing turns it "off"
    /// (a fiscal contraction), so no new sign convention is needed beyond what BudgetBill's tax term
    /// already established.
    /// </summary>
    public class TaxProgramBill
    {
        public TaxType Type;
        public bool IsAdd;
        public int DaysRemaining;
    }

    /// <summary>WelfareProgramType equivalent of TaxProgramBill - see that class's own doc comment, same pattern throughout (GenerosityLevel in place of Rate).</summary>
    public class WelfareProgramBill
    {
        public WelfareProgramType Type;
        public bool IsAdd;
        public int DaysRemaining;
    }
}
