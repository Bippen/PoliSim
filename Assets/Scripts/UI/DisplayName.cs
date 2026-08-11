using System;
using System.Collections.Generic;
using System.Text;

namespace PoliSim.UI
{
    /// <summary>
    /// Turns a model identifier into the name a player reads.
    ///
    /// <para><b>Why this exists.</b> Eight ledger sites printed `enumValue.ToString()` straight into the
    /// UI, so the Budget and Welfare screens rendered `MeansTestedWelfare` and
    /// `VeteransAffairsDiscretionary` as-is. That is not merely ugly: an unspaced identifier gives word
    /// wrap nothing to break on, and IMGUI responds by breaking MID-WORD -
    /// `run_05c_budget_welfare_deep.png` showed `NegativeIncomeTax` set as "NegativeInco / meTax".
    /// The overflow guard flagged 33 of these; this removes the cause.</para>
    ///
    /// <para><b>It looks the names up rather than inventing them.</b> Curated names already existed in
    /// <see cref="PolicyWebRenderer"/>'s node metadata - "Means-Tested Welfare", "Capital Gains Tax",
    /// "Veterans Benefits (Mand.)" - hand-written, correctly hyphenated, abbreviated where a column
    /// needed it. No formatter produces those. The defect was never a missing formatter; it was a
    /// ledger not asking a table that was already there.</para>
    ///
    /// ⚠ **AND IT DOES NOT COPY THEM.** Restating those strings here would be the "two tables that
    /// agree until one is edited" failure this codebase has already written up twice. There is one set
    /// of names; this reaches it.
    ///
    /// <para><b>The bridge is name identity, not a mapping table.</b> `PolicyNodeId` and the model
    /// enums (`SpendingCategory`, `WelfareProgramType`, `TaxType`) were authored with matching member
    /// names, so the lookup is a parse rather than forty hand-maintained pairs that would themselves
    /// drift. Where no node exists - `VeteransAffairsDiscretionary` has none - it falls back to
    /// splitting the identifier, which is worse than a curated name and far better than a raw one.</para>
    /// </summary>
    public static class DisplayName
    {
        /// <summary>
        /// Identifier to rendered name. Populated on demand and never invalidated: the mapping is
        /// derived from compile-time enum members, so it cannot change while the game runs.
        ///
        /// ⚠ Caching is not an optimisation here, it is a requirement. These resolve inside `OnGUI`,
        /// once per row per frame, and `Enum.TryParse` is a reflective call - putting one on that path
        /// uncached would be a per-frame allocation on every ledger row on screen.
        /// </summary>
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        /// <summary>
        /// Takes the identifier rather than a typed enum, so one implementation serves every model enum
        /// the UI prints - including the ones not yet written. Call it as `DisplayName.Of(x.ToString())`.
        /// </summary>
        public static string Of(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            if (Cache.TryGetValue(identifier, out string cached))
            {
                return cached;
            }

            string resolved = Resolve(identifier);
            Cache[identifier] = resolved;
            return resolved;
        }

        private static string Resolve(string identifier)
        {
            if (Enum.TryParse(identifier, out PolicyNodeId node) &&
                PolicyWebRenderer.TryGetPolicyName(node, out string curated))
            {
                return curated;
            }

            return Split(identifier);
        }

        /// <summary>
        /// Spaces a CamelCase identifier, keeping acronym runs intact: `HHSDiscretionary` becomes
        /// "HHS Discretionary" rather than "H H S Discretionary", and `UBI` is left alone.
        ///
        /// A boundary is an uppercase letter that either follows a lowercase one (the ordinary
        /// word break) or precedes one while following another uppercase (the end of an acronym run).
        /// </summary>
        private static string Split(string identifier)
        {
            var builder = new StringBuilder(identifier.Length + 8);
            builder.Append(identifier[0]);

            for (int i = 1; i < identifier.Length; i++)
            {
                char current = identifier[i];
                if (char.IsUpper(current))
                {
                    bool followsLower = char.IsLower(identifier[i - 1]);
                    bool endsAcronym = char.IsUpper(identifier[i - 1]) &&
                                       i + 1 < identifier.Length && char.IsLower(identifier[i + 1]);
                    if (followsLower || endsAcronym)
                    {
                        builder.Append(' ');
                    }
                }

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
