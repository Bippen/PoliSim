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
        /// <summary>PS-6 US-21b: on the player-president's docket - voted, presented, awaiting his answer (`DocketEntry`); null otherwise, and in every country but the USA.</summary>
        public DocketEntry Docket;
    }

    /// <summary>A-LOOP q34 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §1 (2)): what a passed TaxProgramBill did to its line's rate - a levy is a move
    /// from 0 to the line's rate, a removal from its rate to 0 - in q37's one unit (`ParliamentSystem.TaxTermPoints`), returned by
    /// `ParliamentSystem.ApplyTaxProgramBillResult` for q37's tax term to book as a rate move like any other. Not saved: it lives for the call.</summary>
    public struct TaxLevyMove
    {
        public TaxType Type;
        /// <summary>False for a failed bill, a line the country does not carry, or a bill that found its line already as it asks.</summary>
        public bool Moved;
        /// <summary>The line's rate before the bill, in the tax term's unit: 0 where it was not levied.</summary>
        public float FromPoints;
        /// <summary>The line's rate after the bill, in the tax term's unit: 0 where the bill removed it.</summary>
        public float ToPoints;
    }

    /// <summary>WelfareProgramType equivalent of TaxProgramBill - see that class's own doc comment, same pattern throughout (GenerosityLevel in place of Rate).</summary>
    public class WelfareProgramBill
    {
        public WelfareProgramType Type;
        public bool IsAdd;
        public int DaysRemaining;
        /// <summary>PS-6 US-21b: on the player-president's docket - voted, presented, awaiting his answer (`DocketEntry`); null otherwise, and in every country but the USA.</summary>
        public DocketEntry Docket;
    }
}
