using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part B (Parliament), Master Sequence step 4 - a small number of
    /// ORIGINAL, GENERIC, clearly-fictional party archetypes, per the Master Roadmap's own explicit
    /// instruction (never a real party name). Deliberately a SHARED taxonomy applied identically
    /// across all six countries (mirroring CabinetMinisterPhilosophy's own single shared taxonomy),
    /// not distinct per-country party names - "per country" in the spec means each country tracks its
    /// own SEAT DISTRIBUTION across this same shared archetype set, not that Sweden's parties have
    /// different fictional names than USA's.
    /// </summary>
    public enum PartyArchetype
    {
        ProgressiveAlliance,
        ConservativeUnion,
        CentristCoalition,
        NationalistFront
    }

    /// <summary>
    /// Fixed, hardcoded per-archetype constants (gameplay-tuning placeholders, not researched
    /// figures) - the Master Roadmap's own Open Question ("seats derive from ApprovalRating plus
    /// bounded inertia/randomness - exact formula is an Open Question") resolved here as a concrete,
    /// STATED proposal rather than a silent guess. Three axes:
    /// <list type="bullet">
    /// <item><description><c>BaseSupportShare</c> - fixed starting/floor seat-share weight per
    /// archetype, sums to 1.0 across all four.</description></item>
    /// <item><description><c>ApprovalSensitivity</c> - how strongly this archetype's target seat
    /// share moves with the sitting government's ApprovalRating. The three mainstream archetypes
    /// (Progressive/Conservative/Centrist) are ESTABLISHMENT-coded and gain modestly as approval
    /// rises (political stability favors mainstream parties generally, regardless of which one is
    /// "in government" - this game never assigns the player's own government a party identity, so
    /// approval is modeled as benefiting establishment stability broadly, not one specific party).
    /// NationalistFront is the PROTEST archetype, strongly INVERSE - a classic anti-incumbent
    /// backlash pattern, surging when approval falls.</description></item>
    /// <item><description><c>FiscalStance</c> - -1 (favors lower taxes) to +1 (favors higher taxes/
    /// more government revenue) - the axis ParliamentSystem scores a TaxBill's alignment against.
    /// Progressive favors higher taxes, Conservative favors lower, Centrist is neutral/pragmatic,
    /// Nationalist is mildly tax-skeptic (a generic populist-right placeholder stance, not a claim
    /// about any real nationalist party's actual platform - fictional and generic per this system's
    /// own scope).</description></item>
    /// </list>
    /// </summary>
    public static class PartyArchetypeData
    {
        public const float ProgressiveBaseSupportShare = 0.32f;
        public const float ConservativeBaseSupportShare = 0.32f;
        public const float CentristBaseSupportShare = 0.24f;
        public const float NationalistBaseSupportShare = 0.12f;

        public const float ProgressiveApprovalSensitivity = 0.35f;
        public const float ConservativeApprovalSensitivity = 0.35f;
        public const float CentristApprovalSensitivity = 0.20f;
        public const float NationalistApprovalSensitivity = -0.90f;

        public const float ProgressiveFiscalStance = 0.7f;
        public const float ConservativeFiscalStance = -0.7f;
        public const float CentristFiscalStance = 0.0f;
        public const float NationalistFiscalStance = -0.3f;

        public static readonly PartyArchetype[] AllArchetypes =
        {
            PartyArchetype.ProgressiveAlliance,
            PartyArchetype.ConservativeUnion,
            PartyArchetype.CentristCoalition,
            PartyArchetype.NationalistFront
        };

        public static float GetBaseSupportShare(PartyArchetype archetype)
        {
            switch (archetype)
            {
                case PartyArchetype.ProgressiveAlliance: return ProgressiveBaseSupportShare;
                case PartyArchetype.ConservativeUnion: return ConservativeBaseSupportShare;
                case PartyArchetype.CentristCoalition: return CentristBaseSupportShare;
                case PartyArchetype.NationalistFront: return NationalistBaseSupportShare;
                default: return 0f;
            }
        }

        public static float GetApprovalSensitivity(PartyArchetype archetype)
        {
            switch (archetype)
            {
                case PartyArchetype.ProgressiveAlliance: return ProgressiveApprovalSensitivity;
                case PartyArchetype.ConservativeUnion: return ConservativeApprovalSensitivity;
                case PartyArchetype.CentristCoalition: return CentristApprovalSensitivity;
                case PartyArchetype.NationalistFront: return NationalistApprovalSensitivity;
                default: return 0f;
            }
        }

        public static float GetFiscalStance(PartyArchetype archetype)
        {
            switch (archetype)
            {
                case PartyArchetype.ProgressiveAlliance: return ProgressiveFiscalStance;
                case PartyArchetype.ConservativeUnion: return ConservativeFiscalStance;
                case PartyArchetype.CentristCoalition: return CentristFiscalStance;
                case PartyArchetype.NationalistFront: return NationalistFiscalStance;
                default: return 0f;
            }
        }

        public static string GetDisplayName(PartyArchetype archetype)
        {
            switch (archetype)
            {
                case PartyArchetype.ProgressiveAlliance: return "Progressive Alliance";
                case PartyArchetype.ConservativeUnion: return "Conservative Union";
                case PartyArchetype.CentristCoalition: return "Centrist Coalition";
                case PartyArchetype.NationalistFront: return "Nationalist Front";
                default: return archetype.ToString();
            }
        }

        /// <summary>
        /// Turn-0 seed: every country starts at ApprovalRating 50 (WorldFactory), so the general
        /// per-turn target-share formula's <c>(ApprovalRating - 50) / 100</c> term is exactly 0 at
        /// seed time - this just applies BaseSupportShare directly, reconciled to TotalSeats via
        /// largest-remainder rounding so the four integer seat counts sum exactly to TotalSeats.
        /// </summary>
        public static Dictionary<PartyArchetype, int> GetInitialSeats()
        {
            var result = new Dictionary<PartyArchetype, int>();
            var remainders = new Dictionary<PartyArchetype, float>();
            int assigned = 0;

            foreach (PartyArchetype archetype in AllArchetypes)
            {
                float exact = GetBaseSupportShare(archetype) * ParliamentConstants.TotalSeats;
                int floor = (int)exact;
                result[archetype] = floor;
                remainders[archetype] = exact - floor;
                assigned += floor;
            }

            int remaining = ParliamentConstants.TotalSeats - assigned;
            var byRemainder = new List<PartyArchetype>(AllArchetypes);
            byRemainder.Sort((a, b) => remainders[b].CompareTo(remainders[a]));
            for (int i = 0; i < remaining; i++)
            {
                result[byRemainder[i % byRemainder.Count]]++;
            }

            return result;
        }
    }

    /// <summary>Shared sizing constant, kept in Data (not Simulation) so PartyArchetypeData.GetInitialSeats can use it without a Data-&gt;Simulation dependency.</summary>
    public static class ParliamentConstants
    {
        /// <summary>Total hemicycle seats per country - an arbitrary round number for a clean visualization, not modeled on any one real chamber's exact size.</summary>
        public const int TotalSeats = 200;
    }
}
