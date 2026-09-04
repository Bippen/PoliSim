namespace PoliSim.Data
{
    /// <summary>
    /// Law system MVP slice: the categories a law can belong to. One member at the slice
    /// (CrimeJustice) - the slice deliberately proved the architecture on ONE category before any
    /// authoring marathon, per the scoping package. THE SECOND CATEGORY (2026-08-26, pass-3 terminal
    /// ruling): LaborMarket - the enum extended, not the dial shape, exactly as this doc committed.
    /// </summary>
    public enum LawCategory
    {
        CrimeJustice,
        LaborMarket,
        /// <summary>P4-C3 (2026-09-04), the first category from the lever map's unreached set: laws that move the natural rate of unemployment - benefit rules, employment protection, bargaining, active programmes.</summary>
        LabourInstitutions,
        /// <summary>P4-C3, the second category: the fiscal framework - debt anchors and brakes, expenditure ceilings, the debt office's mandate, fiscal councils, tax administration - reaching five structural parameters no dial reaches.</summary>
        FiscalFramework
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
    /// conversion to a read-only summary for how the MVP avoids it for that category. THE LABOR
    /// CATEGORY RESOLVES THE SAME PROBLEM THE OTHER WAY (pass-3 terminal ruling, 2026-08-26,
    /// "keeps sliders - coexistence", Elias's deliberate override of the read-only precedent):
    /// each labor dial splits into a bill-owned STATUTORY BASE (Country.*Base fields, set
    /// absolutely by LaborPolicyBill exactly as before) plus a law-owned OFFSET (the pure sum of
    /// enacted laws' labor deltas), composed as effective = clamp(base + offsets) with the clamp
    /// applied ONCE at composition and never persisted into either component - so bills and laws
    /// each own one book, full repeal returns the effective dial exactly to base, and a passed
    /// bill never stomps law effects. See SimulationManager.RecomputeLaborDialsFromEnactedLaws.</para>
    ///
    /// <para>Six C&amp;J fields, one per Country dial CrimeJusticePolicyBill already owns
    /// (PoliceFundingLevel/SentencingSeverity/BailReformLevel/DrugPolicyLevel/JudicialFundingLevel/
    /// BorderEnforcementLevel) - a law's dial shape mirrors CrimeJusticePolicyBill's own field shape
    /// 1:1, reinterpreted as deltas. THE SECOND CATEGORY (2026-08-26) extends the same class with
    /// six labor fields mirroring LaborPolicyBill's shape the same way - the float-field path this
    /// doc always reserved for non-dict categories. A law of one category leaves the other
    /// category's fields at their 0f defaults, which every consumer treats as "does not touch"
    /// (an empty pane section, a zero direction term, a zero recompute contribution). This stays
    /// deliberately category-specific, not a generic Dictionary&lt;string,float&gt; - every existing
    /// bill kind in this codebase is a concrete typed class with named float fields (see
    /// StandalonePolicyBills.cs), never a dial dictionary; a dict-shaped category (e.g. Sectors)
    /// would still need its own LawDefinition variant, explicitly out of scope here.</para>
    ///
    /// <para><b>Units are per-dial, not uniform</b> (the pass-3 magnitude ruling): the six C&amp;J
    /// deltas and four of the labor deltas are 0-100 dial points, but MinimumWageDelta is KAITZ
    /// POINTS (percent of median wage) and PaidFamilyLeaveWeeksDelta is WEEKS - real units on
    /// real-unit dials. The shared magnitude grid applies through LawCatalog.DialMagnitudeScales
    /// (per-dial normalization), not by pretending every dial speaks the same unit.</para>
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
        /// the game (the law-browser request, consumed to COMPLETED.md §24). Never null for a shipped law - the "no
        /// citation" case a genre-idiom law reaches for is GENRE-IDIOM itself, stated as such, not
        /// an empty field.</summary>
        public string Citation;

        public float PoliceFundingDelta;
        public float SentencingSeverityDelta;
        public float BailReformDelta;
        public float DrugPolicyDelta;
        public float JudicialFundingDelta;
        public float BorderEnforcementDelta;

        /// <summary>THE LABOR FIELDS (second category, 2026-08-26). MinimumWageDelta is in KAITZ
        /// POINTS (percent of median wage - inert for a country with no statutory minimum:
        /// Sweden/Italy, honestly matching reality, the same skip GetLaborBillDirection already
        /// applies to the bill); PaidFamilyLeaveWeeksDelta is in WEEKS; the other four are 0-100
        /// dial points like every C&amp;J delta.</summary>
        public float MinimumWageDelta;
        public float PaidFamilyLeaveWeeksDelta;
        public float OvertimeRegulationDelta;
        public float RetrainingProgramDelta;
        public float FamilyPolicyDelta;
        public float ImmigrationPolicyDelta;

        /// <summary>P4-C3 (2026-09-04): the STRUCTURAL effects - moves of the model's seeded per-country parameters (the natural rate of
        /// unemployment, the debt anchor, the debt maturity, the premium sensitivity, collection coverage, the spending share), each in
        /// its own unit, composed from the enacted set by SimulationManager.RecomputeStructuralParametersFromEnactedLaws exactly as the
        /// twelve dials are. One table (StructuralParameters) rather than a field per parameter. Empty for every law of the two dial
        /// categories.</summary>
        public StructuralDelta[] Structural = System.Array.Empty<StructuralDelta>();

        /// <summary>P4-C3: the law's own reading on the economic axis (-1 left … +1 right), used for the NAIRU effect's stance
        /// term because the sign of a NAIRU move does not tell a law's politics - a benefit cut and a training programme both
        /// lower it. The twelve dials keep their uniform axis signs (ParliamentSystem.LawDialAxes); this field is read only
        /// for the structural effects.</summary>
        public float LrEconToward10;

        /// <summary>Code-review pass (2026-08-25): the dial deltas as ONE ordered array - every
        /// consumer that needs "all of a law's dials" (GameController.LawMagnitudeTier,
        /// ParliamentSystem.GetLawBillDirection) should read from this instead of hand-listing the
        /// fields itself, so a new dial is added in exactly one place rather than drifting across
        /// independent enumerations. ORDER IS A CONTRACT: the six C&amp;J dials first (CrimeJusticeDial's
        /// order), then the six labor dials (LaborDial's order) - ParliamentSystem.LawDialSigns and
        /// LawCatalog.DialMagnitudeScales are index-locked to this array and must grow in lockstep
        /// (both state this on their own declarations). Allocates a small array per call rather than
        /// exposing an iterator, deliberately: this is read at most a few times per OnGUI frame (one
        /// law's tier, one law's bill direction), not in a hot per-turn simulation loop.</summary>
        public float[] DialDeltas => new[]
        {
            PoliceFundingDelta, SentencingSeverityDelta, BailReformDelta,
            DrugPolicyDelta, JudicialFundingDelta, BorderEnforcementDelta,
            MinimumWageDelta, PaidFamilyLeaveWeeksDelta, OvertimeRegulationDelta,
            RetrainingProgramDelta, FamilyPolicyDelta, ImmigrationPolicyDelta
        };

        /// <summary>Approval-rating cost paid ONCE, on successful enactment - distinct from ParliamentSystem.BillFailedApprovalCost (which is charged on a FAILED vote, for every bill kind uniformly). Represents a controversial law being costly to enact even when it passes, the same spirit as BudgetBill's own tax-hike approval penalty, sized here per-law rather than derived from the delta magnitude (a simplification, honestly - the MVP's four laws use small, illustrative, gameplay-tuning values, not researched figures, matching every other approval-cost constant in this codebase).</summary>
        public float EnactmentApprovalCost;
    }
}
