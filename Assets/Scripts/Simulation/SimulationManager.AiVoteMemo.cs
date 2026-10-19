using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// AF-8 (the s873 review's M2): THE AI'S CONSTRUCTIVE-VOTE WEIGHING, MEMOISED ON WHAT IT READS. Ruling q8 cut the Monday cadence, so the AI weighs
    /// every day - a formation per candidate - and since FORK-1 every ticking fork does too, a whole turn's days at the year boundary. The decision is a
    /// pure function of its inputs, so a day whose inputs equal the last day's that moved nothing moves nothing again: the weighing is skipped. The
    /// memo is this manager's own (an instance field, never static), so the played world and each fork keep their own; it is not saved - a loaded game
    /// weighs on its first day and reaches the same answer, the function being pure. `ConstructiveVoteDiagnostic` (h) runs a German span with it and
    /// without it and asserts the same decisions on the same days, and prints the tick's cost per day both ways.
    /// <para>THE KEY - every input the weighing reads (`TryAiConstructiveVote` → `AiMotionCandidates`, `ConstructiveVoteOf` → `DraftSuccessor`,
    /// `DraftProposal`, `Formateur.Answer`, `GroupAlone`, `ConfidenceProcedure.ConstructiveVote`, `GovernmentFormation.PayoffIn`):</para>
    /// <list type="bullet">
    /// <item>the government - the record itself (by reference), its version, cabinet, support, prime minister's party, standing refusals, and each
    /// agreement's broken items (the aggrieved movers first);</item>
    /// <item>the chamber - every seat, the roster's keys, the player's party (never drafted unasked) and the compatibility the roster's positions give;</item>
    /// <item>the declarations the sitting chamber reads today (`GovernmentFormation.SittingReading`) - its lines, platforms and candidacies, as read,
    /// so a dated declaration taking effect changes the key on its own day;</item>
    /// <item>the posts' salience on today's date (`PortfolioSalience.Weight`, dated) - the Gamson posts and the partners' weighed offers;</item>
    /// <item>what a supporter's tabled demands read of the country (`SupportAgreement`'s own template key, without its date stamp): the laws enacted,
    /// the two agreement dials, the minimum wage's existence, the currency zone's ownership, the electricity-tax statute and the trade axis.</item>
    /// </list>
    /// The gates before it (a caretaker, a round, the week before polling day) are read every day, outside the memo. Nothing continuous enters the
    /// key: the dials move only by law or by the player, and the date enters only through what is dated.
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>False only for the diagnostic's control run, which weighs every day as before the memo.</summary>
        private bool _aiVoteMemoEnabled = true;
        private Elections.GovernmentRecord _aiVoteMemoGovernment;
        private string _aiVoteMemoKey;
        /// <summary>The instrument's counts: days weighed, days the memo skipped.</summary>
        private int _aiVoteWeighed, _aiVoteSkipped;

        /// <summary>True where today's inputs equal those of the last day that moved nothing - the weighing would move nothing again.</summary>
        private bool AiVoteMemoHolds(Country country, Elections.GovernmentRecord g, out string key)
        {
            key = null;
            if (!_aiVoteMemoEnabled) { _aiVoteWeighed++; return false; }
            key = AiVoteKey(country, g);
            if (ReferenceEquals(g, _aiVoteMemoGovernment) && key == _aiVoteMemoKey) { _aiVoteSkipped++; return true; }
            _aiVoteWeighed++;
            return false;
        }

        /// <summary>The weighing moved nothing under <paramref name="key"/>.</summary>
        private void AiVoteMemoNothing(Elections.GovernmentRecord g, string key)
        {
            if (key == null) { return; }
            _aiVoteMemoGovernment = g;
            _aiVoteMemoKey = key;
        }

        private string AiVoteKey(Country country, Elections.GovernmentRecord g)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(1024);
            sb.Append(country.Id).Append('|').Append(country.PlayerPartyAbbrev).Append('|').Append(g.Version.ToString(inv)).Append('|').Append(g.PmParty)
              .Append('|').Append(string.Join("+", g.Cabinet)).Append('|').Append(string.Join("+", g.Support)).Append('|').Append(string.Join(",", g.StandingRefusals)).Append('|');
            foreach (Elections.SupportAgreement a in g.Agreements) { sb.Append(a.Supporter).Append(':').Append(a.Count(Elections.AgreementState.Broken).ToString(inv)).Append(','); }
            sb.Append('|');
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country.Id);
            foreach (PoliticalParty p in parties)
            {
                int seats = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(p.Abbrev, out int held) ? held : 0;
                sb.Append(p.Abbrev).Append('=').Append(seats.ToString(inv)).Append(',');
            }
            sb.Append('|');
            double[,] compat = Elections.GovernmentFormation.Compatibility(parties);
            foreach (double c in compat) { sb.Append(c.ToString("R", inv)).Append(','); }
            sb.Append('|');
            Elections.DeclarationReading reading = Elections.GovernmentFormation.SittingReading(country, CurrentDate);
            sb.Append(reading.Vintage).Append('|');
            foreach (Elections.RedLine l in reading.Lines(country.Id, parties)) { sb.Append(l.A.ToString(inv)).Append('-').Append(l.B.ToString(inv)).Append(l.Kind).Append(l.BlocksSupport ? 's' : 'c').Append(l.OneWay ? 'o' : 't').Append(','); }
            sb.Append('|');
            foreach (Elections.InOrAgainst x in reading.Platforms(country.Id, parties)) { sb.Append(x.Party.ToString(inv)).Append(x.VotesAgainst ? 'a' : 's').Append(','); }
            sb.Append('|');
            foreach ((string abbrev, string candidate, string _) in reading.Candidacies(country.Id)) { sb.Append(abbrev).Append(':').Append(candidate).Append(','); }
            sb.Append('|');
            foreach (CabinetPortfolio post in (CabinetPortfolio[])System.Enum.GetValues(typeof(CabinetPortfolio))) { sb.Append(Elections.PortfolioSalience.Weight(country.Id, post, CurrentDate).ToString("R", inv)).Append(','); }
            sb.Append('|');
            var enacted = new List<string>();
            foreach (EnactedLaw e in country.EnactedLaws) { enacted.Add(e.LawId); }
            enacted.Sort(System.StringComparer.Ordinal);
            sb.Append(string.Join(",", enacted)).Append('|').Append(country.BorderEnforcementLevel.ToString("R", inv)).Append('|').Append(country.PoliceFundingLevel.ToString("R", inv))
              .Append('|').Append(country.MinimumWageImplemented).Append('|').Append(_world != null && _world.OwnsCurrencyZone(country)).Append('|').Append(EnergyLayer.HasElectricityTax(country.Id))
              .Append('|').Append(ParliamentSystem.TradeAxisAvailable(country));
            return sb.ToString();
        }
    }
}
