using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// K197 (Code's recommendation under Elias's standing instruction of 2026-10-27; COAL-1's ask c): <b>THE PLAYER'S POLISH DESK VERBS</b> - leave the
    /// government (<see cref="LeaveGovernment"/>, COAL-1's exit path: the record relabelled by its seats, then <see cref="ExitOpensConfidenceProcedure"/>) and move
    /// an Art. 158 constructive vote (<see cref="MoveNoConfidence"/>, the Bundestag's shape: the successor drafted on the player's party, voted on the motion's
    /// day - K195 - by a majority of the statutory 460, carried installing through <see cref="InstallSuccessor"/>). The two verbs were the manager's since COAL-1;
    /// what this file adds is their GATES as readers, so the desk draws each verb disabled with the reason the verb itself would give - one list of tests, read
    /// by the verb and by the desk alike.
    /// <para>K270 (Code's recommendation under Elias's standing instruction, on the review of `wave/pldesk`): the player's constructive motion (Art. 158, like
    /// Art. 67) is NOT held by K196's quiet days, which rule an AI partner's exit - it is gated as Germany's is. The three-month bar on a repeated motion
    /// (Art. 158 ust. 2) and Art. 160 are not modelled (K195).</para>
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>K197: the government's record line the player's leaving writes - one sentence, so the Docket's reader finds the day it was written.</summary>
        public static string LeftTheGovernmentLine(System.DateTime day, string party) => $"{day:yyyy-MM-dd}: {party} left the government";

        /// <summary>K197: whether <see cref="LeaveGovernment"/> would be accepted today, and why not - the verb's own tests, so the desk draws it disabled with its reason.</summary>
        public bool CanLeaveGovernment(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { refusedBecause = "NOT THE PLAYER'S COUNTRY"; return false; }
            if (country.PlayerHoldsPresidency) { refusedBecause = PresidentOfficeWords; return false; }   // SP-7b (I14 (2a)): the party is the AI's while the player is President
            if (country.Government.RoleOf(country.PlayerPartyAbbrev) != Elections.PlayerRole.JuniorPartner) { refusedBecause = "YOUR PARTY IS NOT A JUNIOR PARTNER"; return false; }
            // §705 (the review's latent case): the smaller member of a parliamentary group does not leave its group's cabinet alone - the CSU sits in the
            // Union's Fraktion, and a CDU cabinet without it is one no chamber could have elected
            if (SeatedGroupPartner(country, country.PlayerPartyAbbrev) is string groupPartner && country.Government.Cabinet.Contains(groupPartner))
            {
                refusedBecause = "YOUR PARTY SITS IN ONE FRAKTION WITH " + groupPartner.ToUpperInvariant() + " - IT DOES NOT LEAVE THE CABINET ALONE";
                return false;
            }
            refusedBecause = null;
            return true;
        }

        /// <summary>
        /// K197: whether <see cref="MoveNoConfidence"/> would take the player's motion up today, and why not - every test the verb makes before it counts the
        /// vote (who may move it - a tenth of the members, a quarter, Art. 158's 46 deputies - is the count's, and the desk reads it off the projection).
        /// </summary>
        public bool CanMoveNoConfidence(CountryId countryId, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { refusedBecause = "NOT THE PLAYER'S COUNTRY"; return false; }
            Elections.GovernmentRecord g = country.Government;
            if (country.PlayerHoldsPresidency) { refusedBecause = PresidentOfficeWords; return false; }   // SP-7b (I14 (2a)): the party is the AI's while the player is President
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Unsourced) { refusedBecause = "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED"; return false; }
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Parlamento) { refusedBecause = "A MOTION OF NO CONFIDENCE UNDER ART. 94 IS NOT YET MODELLED"; return false; }   // PS-7: the investiture by mandates is built, the mid-term motion is not
            if (Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Presidential) { refusedBecause = "A PRESIDENCY SERVES A FIXED TERM · NO MOTION OF NO CONFIDENCE IS TAKEN UP"; return false; }   // PS-6 US-7: Art. II §1 - a fixed term, no confidence vote
            switch (g.RoleOf(country.PlayerPartyAbbrev))
            {
                case Elections.PlayerRole.PrimeMinister: refusedBecause = "THE GOVERNMENT MOVES NO MOTION AGAINST ITSELF"; return false;
                case Elections.PlayerRole.JuniorPartner: refusedBecause = "LEAVE THE GOVERNMENT FIRST"; return false;
                case Elections.PlayerRole.Support: refusedBecause = "WITHDRAW YOUR SUPPORT FIRST"; return false;
            }
            if (g.Caretaker) { refusedBecause = "NO MOTION IS TAKEN UP AGAINST A CARETAKER GOVERNMENT"; return false; }
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate) { refusedBecause = "NO MOTION IS TAKEN UP BETWEEN AN EXTRA ELECTION'S DECISION AND THE NEW RIKSDAG"; return false; }
            if (g.NoConfidenceOn != System.DateTime.MinValue) { refusedBecause = "THE CHAMBER HAS ALREADY DECLARED NO CONFIDENCE - THE GOVERNMENT'S WEEK RUNS"; return false; }
            if (Elections.ConfidenceProcedure.IsConstructive(countryId))
            {
                bool sejm = Elections.ConfidenceProcedure.RulesOf(countryId) == Elections.ConfidenceProcedure.Rules.Sejm;   // COAL-1 (K133): Art. 158 Konstytucja, the Bundestag's shape
                // §705 (the review's defect 5): no constructive vote while the chancellor's election runs - before the convening the old Bundestag
                // sits, and a successor it elected would never be discharged at the convening (Art. 69 Abs. 2)
                if (!sejm && ((g.Round != null && g.Round.Open) || ElectionAwaitsRound(country, g))) { refusedBecause = "THE CHANCELLOR'S ELECTION IS UNDER WAY - ART. 63, NOT ART. 67"; return false; }
                if (sejm && g.Sejm != null && g.Sejm.Open) { refusedBecause = "THE SEJM'S FORMATION IS UNDER WAY - ART. 154, NOT ART. 158"; return false; }
                // §754 (the review's note): a party with no members moves nothing - the AI's rule, the player's too
                if (SeatsOf(country, country.PlayerPartyAbbrev) <= 0) { refusedBecause = "YOUR PARTY HOLDS NO SEAT - A CONSTRUCTIVE VOTE IS MOVED BY MEMBERS"; return false; }
            }
            refusedBecause = null;
            return true;
        }
    }
}
