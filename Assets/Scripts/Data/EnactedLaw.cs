using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One law currently in force for a country - Country.EnactedLaws' element type.
    ///
    /// A <c>List&lt;EnactedLaw&gt;</c>, not a <c>Dictionary&lt;LawId,bool&gt;</c> - matching this
    /// codebase's dominant "collection of things a country has" shape (TaxLines/WelfarePrograms/
    /// Sectors are all List&lt;T&gt;), not the Dictionary shape reserved for closed structural enums
    /// with a hard one-per-key constraint (CabinetMinisters/ParliamentSeats). A bare bool would
    /// discard exactly the provenance this project has otherwise consistently kept - see
    /// DivisionRecord/DivisionLog, built specifically to record HOW something was decided, not just
    /// its current state. Carrying EnactedOn here is what makes that record possible for laws too.
    /// [Serializable] to match DivisionRecord's own precedent (a record type sitting in a List&lt;T&gt;
    /// field directly on the [Serializable] Country class) and close Unity's UAC1001 analyzer note -
    /// cosmetic only (Newtonsoft, not Unity's serializer, is what save/load actually uses), kept for
    /// a clean warning set.
    /// </summary>
    [Serializable]
    public sealed class EnactedLaw
    {
        /// <summary>Joins to LawDefinition.Id via LawCatalog.GetById - never the LawDefinition reference itself, so a save persists a stable string rather than a pointer into static content that could be re-authored between sessions.</summary>
        public string LawId;

        public DateTime EnactedOn;
    }
}
