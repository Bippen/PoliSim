using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Law system MVP slice: the static catalog of every authored law, one category (Crime &amp;
    /// Justice) and a handful of laws - "enough to prove the path from browser -&gt; bill -&gt; vote -&gt;
    /// enacted -&gt; dial effect -&gt; ledger term -&gt; repeal", per the scoping package's own instruction
    /// not to start an authoring marathon. Plain hardcoded data with no logic tying it to how it's
    /// consumed - the same "swappable later, e.g. for AI-generated content" idiom
    /// EventSystem.EventPool/FederalReserveSystem.CandidatePool/CabinetSystem's DecisionPool already
    /// establish for this codebase's other content pools.
    ///
    /// Every delta is a small, illustrative, gameplay-tuning magnitude (not a researched figure),
    /// matching every other policy-dial constant in this codebase's own stated calibration
    /// philosophy - CrimeJusticePolicyBill's own dials carry the same disclaimer. Kept well inside
    /// [-30, +30] against the dials' [0, 100] range and 50-neutral seed, so no single law can push a
    /// dial from one extreme to the other by itself - composing two or three is what gets there,
    /// which is the point of a preset system over a single all-powerful lever.
    /// </summary>
    public static class LawCatalog
    {
        public static readonly List<LawDefinition> All = new List<LawDefinition>
        {
            new LawDefinition
            {
                Id = "truth_in_sentencing_act",
                Name = "Truth in Sentencing Act",
                Description = "Requires offenders to serve a much larger share of their imposed sentence before parole eligibility, and narrows the discretion judges have to depart from guideline sentences.",
                Category = LawCategory.CrimeJustice,
                SentencingSeverityDelta = 15f,
                BailReformDelta = -8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "cash_bail_reform_act",
                Name = "Cash Bail Reform Act",
                Description = "Replaces cash bail with risk-based pretrial release for most non-violent charges, and directs new funding toward the court staff needed to run individualized release hearings.",
                Category = LawCategory.CrimeJustice,
                BailReformDelta = 18f,
                JudicialFundingDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "border_security_act",
                Name = "Border Security Act",
                Description = "Expands border enforcement staffing and surveillance infrastructure, and redirects a portion of drug-interdiction resources toward enforcement rather than treatment diversion.",
                Category = LawCategory.CrimeJustice,
                BorderEnforcementDelta = 20f,
                DrugPolicyDelta = -5f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "community_policing_initiative",
                Name = "Community Policing Initiative",
                Description = "Funds neighborhood policing programs and community liaison officers, paired with stricter enforcement of drug offenses in the areas they cover.",
                Category = LawCategory.CrimeJustice,
                PoliceFundingDelta = 15f,
                DrugPolicyDelta = 10f,
                EnactmentApprovalCost = 0.5f
            }
        };

        /// <summary>Looks up a law by its stable Id, or null if no such law exists (e.g. an old save citing a since-removed law - the caller decides how to degrade, matching PolicyWebRenderer/DisplayName's own "missing entry, not a crash" idiom).</summary>
        public static LawDefinition GetById(string lawId)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == lawId)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}
