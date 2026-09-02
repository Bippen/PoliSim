using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// D-5 (a) — **who is in office after an election, and therefore whether the player still is.**
    ///
    /// <para><b>The rule this exists for.</b> R-CL1 ruled that losing office is game over and losing a
    /// vote is not. Until now nothing could answer "is the player in office?": `ElectionSystem` decided
    /// the player's fate on an APPROVAL THRESHOLD, and `GameController` said so about itself — *"there is
    /// no party for the vote model to award the player's fate to. Until the player IS one of these
    /// parties, the win/lose rule stays exactly the approval threshold it has always been."* C-R2 made
    /// the player one of these parties. This is that item.</para>
    ///
    /// <para><b>Office is CABINET MEMBERSHIP, not support and not seats.</b> A party supporting a
    /// government from outside it is not in it — the Tidö arrangement is the model's own worked example,
    /// and treating support as office would have SD governing in 2022, which is exactly the distinction
    /// `GovernmentOption` already draws between `Cabinet` and `Support`. **A party can gain seats and
    /// leave office, or lose seats and stay**, which is the whole reason this is not a seat comparison.</para>
    ///
    /// <para>⚠ <b>THE COMPATIBILITY MATRIX IS DERIVED FROM SOURCED POSITIONS, AND THE RED LINES ARE
    /// NOT ALL SOURCED.</b> Compatibility comes from each party's own CHES figures through §29's existing
    /// `CoalitionCompatibility`, so any country whose parties carry positions gets a real matrix.
    /// **Declared red lines are political FACTS and exist on disk for Sweden 2022 only**; every other
    /// country runs on DERIVED lines alone. That is a stated limitation, not a silent one — a government
    /// formed without a country's real declarations can be one that country would never form, and
    /// `Formed.DeclarationsSourced` carries the answer to any caller that needs to say so.</para>
    /// </summary>
    public static class GovernmentFormation
    {
        /// <summary>What an office test produced, including the reasons it might not have.</summary>
        public readonly struct Formed
        {
            /// <summary>False when no government could be tested at all — see <see cref="Reason"/>.</summary>
            public readonly bool HasGovernment;
            /// <summary>Whether the player's own party sits in the cabinet. Meaningless when
            /// <see cref="HasGovernment"/> is false.</summary>
            public readonly bool PlayerInCabinet;
            /// <summary>Whether the player's party supports the cabinet from outside it. ⚠ Support is NOT
            /// office; it is carried so a screen can say which of the two it is.</summary>
            public readonly bool PlayerSupports;
            /// <summary>⚠ Whether this country's DECLARED red lines are sourced. False means the
            /// government was formed on derived lines alone and may not be one this country would form.</summary>
            public readonly bool DeclarationsSourced;
            public readonly string CabinetDescription;
            public readonly string Reason;

            public Formed(bool hasGovernment, bool playerInCabinet, bool playerSupports,
                bool declarationsSourced, string cabinetDescription, string reason)
            {
                HasGovernment = hasGovernment;
                PlayerInCabinet = playerInCabinet;
                PlayerSupports = playerSupports;
                DeclarationsSourced = declarationsSourced;
                CabinetDescription = cabinetDescription;
                Reason = reason;
            }

            public static Formed None(string reason) => new Formed(false, false, false, false, null, reason);
        }

        /// <summary>
        /// Form a government from the country's CURRENT chamber and report where the player's party
        /// stands in it.
        ///
        /// <para>Returns <see cref="Formed.None"/>, with the reason in plain English, when the country
        /// has no player party, when its parties carry no positions, or when the chamber is empty. ⚠ **A
        /// reason is returned rather than a default**, because "no government could be formed" and "the
        /// player is out of office" are different states and only one of them should end a game.</para>
        /// </summary>
        public static Formed Form(Country country)
        {
            if (!TryFormChamber(country, out IReadOnlyList<PoliticalParty> parties, out int[] seats, out CoalitionResult result,
                    out bool declarationsSourced, out string reason))
            {
                return Formed.None(reason);
            }
            if (string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return Formed.None("the player holds no party"); }
            int playerIndex = -1;
            for (int p = 0; p < parties.Count; p++)
            {
                if (string.Equals(parties[p].Abbrev, country.PlayerPartyAbbrev, StringComparison.Ordinal)) { playerIndex = p; }
            }
            if (playerIndex < 0) { return Formed.None($"the player's party '{country.PlayerPartyAbbrev}' is not in this chamber"); }
            if (result.Outcome == CoalitionOutcomeKind.NewElection || result.Outcome == CoalitionOutcomeKind.Collapse)
            {
                return new Formed(false, false, false, declarationsSourced, null,
                    $"no government could be formed from this chamber ({result.Outcome})");
            }

            int bit = 1 << playerIndex;
            bool inCabinet = (result.Government.Cabinet & bit) != 0;
            bool supports = !inCabinet && (result.Government.Support & bit) != 0;
            var cabinet = new List<string>();
            for (int p = 0; p < parties.Count; p++)
            {
                if ((result.Government.Cabinet & (1 << p)) != 0) { cabinet.Add(parties[p].Abbrev); }
            }
            return new Formed(true, inCabinet, supports, declarationsSourced, string.Join("+", cabinet), null);
        }

        /// <summary>
        /// P2-3.2 (2026-09-02): the sitting cabinet's parties, by abbreviation, for a chamber - the same
        /// formation <see cref="Form"/> runs, read without the player's standing in it, so the compass can mark
        /// the government whether or not the player holds a party. Empty when no government forms from this
        /// chamber (a new election or a collapse) or nothing is seeded for it.
        /// </summary>
        public static IReadOnlyList<string> Cabinet(Country country)
        {
            var cabinet = new List<string>();
            if (!TryFormChamber(country, out IReadOnlyList<PoliticalParty> parties, out int[] _, out CoalitionResult result, out bool _, out string _))
            {
                return cabinet;
            }
            if (result.Outcome == CoalitionOutcomeKind.NewElection || result.Outcome == CoalitionOutcomeKind.Collapse) { return cabinet; }
            for (int p = 0; p < parties.Count; p++)
            {
                if ((result.Government.Cabinet & (1 << p)) != 0) { cabinet.Add(parties[p].Abbrev); }
            }
            return cabinet;
        }

        /// <summary>The formation itself - the chamber's seats, the derived compatibility, the declared red lines and the chamber's own rule - shared by <see cref="Form"/> and <see cref="Cabinet"/>.</summary>
        private static bool TryFormChamber(Country country, out IReadOnlyList<PoliticalParty> parties, out int[] seats,
            out CoalitionResult result, out bool declarationsSourced, out string reason)
        {
            parties = null; seats = null; result = null; declarationsSourced = false; reason = null;
            if (country == null) { reason = "no country"; return false; }
            parties = PartySystems.For(country.Id);
            if (parties == null || parties.Count == 0) { reason = "no party system is seeded for this country"; return false; }
            int n = parties.Count;
            seats = new int[n];
            int totalSeats = 0;
            for (int p = 0; p < n; p++)
            {
                country.ParliamentSeats.TryGetValue(parties[p].Abbrev, out int held);
                seats[p] = held;
                totalSeats += held;
            }
            if (totalSeats <= 0) { reason = "the chamber holds no seats"; return false; }
            double[,] compatibility = Compatibility(parties);
            List<RedLine> lines = DeclaredRedLines.For(country.Id, parties);
            declarationsSourced = DeclaredRedLines.IsSourced(country.Id);
            result = CoalitionFormation.Form(seats, compatibility, lines,
                negativeRule: ChamberRules.UsesNegativeParliamentarism(country.Id));
            return true;
        }

        /// <summary>§29's matrix, over each party's OWN sourced positions — the same weighting
        /// `CoalitionFilm` proved for Sweden, generalised to any country whose parties carry figures.</summary>
        public static double[,] Compatibility(IReadOnlyList<PoliticalParty> parties)
        {
            int n = parties.Count;
            var m = new double[n, n];
            for (int a = 0; a < n; a++)
            {
                for (int b = 0; b < n; b++)
                {
                    if (a == b) { m[a, b] = 100.0; continue; }
                    if (float.IsNaN(parties[a].LrGen) || float.IsNaN(parties[b].LrGen))
                    {
                        m[a, b] = 0.0;
                        continue;
                    }

                    double ideological = CoalitionCompatibility.FromDistance(parties[a].LrGen - parties[b].LrGen);
                    double policy = CoalitionCompatibility.OverAxes(
                        new[] { (double)parties[a].LrEcon, parties[a].Galtan, CoalitionCompatibility.RescaleEu(parties[a].EuPosition) },
                        new[] { (double)parties[b].LrEcon, parties[b].Galtan, CoalitionCompatibility.RescaleEu(parties[b].EuPosition) });
                    m[a, b] = CoalitionCompatibility.WeightIdeological * ideological
                        + CoalitionCompatibility.WeightPolicy * policy;
                }
            }

            return m;
        }
    }

    /// <summary>
    /// Which chambers elect a prime minister by NEGATIVE parliamentarism — a candidate is elected unless
    /// an absolute majority votes against, so a minority cabinet governs on the votes it does not
    /// provoke.
    ///
    /// <para>⚠ <b>SOURCED, per constitution, and the default is the strict one.</b> Sweden's
    /// Regeringsformen 6 kap. 4 § states the rule explicitly. Every other chamber here is treated as
    /// requiring positive investiture, which is the correct reading for Germany (Grundgesetz Art. 63,
    /// which requires the votes of a majority of the Bundestag's members) and the conservative reading
    /// elsewhere: **it makes governments harder to form, never easier**, so where the model is unsure it
    /// errs toward "no government could be formed" rather than toward inventing one.</para>
    /// </summary>
    public static class ChamberRules
    {
        public static bool UsesNegativeParliamentarism(CountryId country) => country == CountryId.Sweden;
    }
}
