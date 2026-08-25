namespace PoliSim.Data
{
    /// <summary>
    /// Law system MVP slice: the categories a law can belong to. One member today
    /// (CrimeJustice) - the slice deliberately proves the architecture on ONE category before any
    /// authoring marathon, per the scoping package. Extend this enum, not the dial shape, when a
    /// second category is added.
    /// </summary>
    public enum LawCategory
    {
        CrimeJustice
    }

    /// <summary>
    /// A law: a named PRESET over the existing dial space, not a bespoke effect - the ruling that
    /// makes the content tractable, per the scoping package ("laws as named presets... not 50xN
    /// bespoke effects"). Static content, never per-country state - see EnactedLaw for what a
    /// country actually persists, and LawCatalog for where these live.
    ///
    /// <para><b>Deltas, not absolute overrides.</b> Every field below is a NUDGE applied to the
    /// dial's current value, not a target it gets set to - matching this codebase's own "ministers
    /// quietly nudge their portfolio's existing channels" idiom (Cabinet's passive competence
    /// effect) rather than the slider-bills' absolute-set convention (CrimeJusticePolicyBill's own
    /// fields ARE the requested new value). This is the ruling that makes multiple enacted laws in
    /// one category compose instead of clobbering each other, and makes repeal clean: repealing
    /// subtracts the same delta back out, decomposable to zero net effect when nothing else has
    /// touched the dial in between (clamp saturation is the one honest exception - see
    /// SimulationManager.ApplyLawBillEffects). A slider the player could still move while laws also
    /// move the same dial would be the two-books problem again - see the Crime & Justice tab's own
    /// conversion to a read-only summary for how the MVP avoids it for this one category.</para>
    ///
    /// <para>Six fields, one per Country dial CrimeJusticePolicyBill already owns
    /// (PoliceFundingLevel/SentencingSeverity/BailReformLevel/DrugPolicyLevel/JudicialFundingLevel/
    /// BorderEnforcementLevel) - a law's dial shape mirrors CrimeJusticePolicyBill's own field shape
    /// 1:1, reinterpreted as deltas. This is deliberately category-specific, not a generic
    /// Dictionary&lt;string,float&gt; - every existing bill kind in this codebase is a concrete typed
    /// class with named float fields (see StandalonePolicyBills.cs), never a dial dictionary; a
    /// second category (e.g. Sectors, which IS dict-shaped) would need its own LawDefinition
    /// variant, explicitly out of scope for this one-category slice.</para>
    ///
    /// <para><b>Prerequisites and conflicts: deferred to v2, not built.</b> A law that can't
    /// conflict is simpler and proves the shape - per the scoping package's own suggestion. Since
    /// deltas stack additively, two laws that pull the same dial in opposite directions simply
    /// partially cancel, a legitimate (if perhaps unrealistic) outcome rather than a broken state -
    /// so a conflict system isn't needed for CORRECTNESS at this scale, only as a future UX nicety.
    /// No fields are reserved for this here; a v2 pass adding them is a pure additive change (new
    /// nullable/empty-default fields deserialize forward-tolerant on old saves, the same
    /// [MissingMemberHandling.Ignore] guarantee every other additive field in this codebase relies
    /// on) and needs no save-shape migration of what already exists.</para>
    /// </summary>
    public sealed class LawDefinition
    {
        /// <summary>Stable string key - the LawBill/EnactedLaw join key, never shown to the player, never renamed once shipped (a save persists it).</summary>
        public string Id;

        public string Name;

        public string Description;

        public LawCategory Category;

        /// <summary>The real-world grounding, in the CONFIRMED/DIRECTIONAL/GENRE-IDIOM voice
        /// LawCatalog's own class doc establishes - a short, UI-facing distillation of the fuller
        /// reasoning that lives in each law's own code comment (magnitude tier, secondary-dial
        /// reasoning, and any honestly-noted caveat stay comment-only; this field is the one
        /// sentence worth putting in front of a player). Surfaced in the law browser's detail pane
        /// for the first time - until now this grounding existed only in source and never reached
        /// the game (see CLAUDE_DESIGN_ASSET_REQUEST.md §7). Never null for a shipped law - the "no
        /// citation" case a genre-idiom law reaches for is GENRE-IDIOM itself, stated as such, not
        /// an empty field.</summary>
        public string Citation;

        public float PoliceFundingDelta;
        public float SentencingSeverityDelta;
        public float BailReformDelta;
        public float DrugPolicyDelta;
        public float JudicialFundingDelta;
        public float BorderEnforcementDelta;

        /// <summary>Code-review pass (2026-08-25): the six dial deltas as ONE ordered array - every
        /// consumer that needs "all of a law's dials" (GameController.LawMagnitudeTier,
        /// ParliamentSystem.GetLawBillDirection) should read from this instead of hand-listing the
        /// six fields itself, so a future seventh dial is added in exactly one place rather than
        /// drifting across two independent enumerations. Allocates a small array per call rather than
        /// exposing an iterator, deliberately: this is read at most a few times per OnGUI frame (one
        /// law's tier, one law's bill direction), not in a hot per-turn simulation loop.</summary>
        public float[] DialDeltas => new[]
        {
            PoliceFundingDelta, SentencingSeverityDelta, BailReformDelta,
            DrugPolicyDelta, JudicialFundingDelta, BorderEnforcementDelta
        };

        /// <summary>Approval-rating cost paid ONCE, on successful enactment - distinct from ParliamentSystem.BillFailedApprovalCost (which is charged on a FAILED vote, for every bill kind uniformly). Represents a controversial law being costly to enact even when it passes, the same spirit as BudgetBill's own tax-hike approval penalty, sized here per-law rather than derived from the delta magnitude (a simplification, honestly - the MVP's four laws use small, illustrative, gameplay-tuning values, not researched figures, matching every other approval-cost constant in this codebase).</summary>
        public float EnactmentApprovalCost;
    }
}
