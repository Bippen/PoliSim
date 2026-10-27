using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// GATE-18 piece three, q20 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: "one formation code. The night's line names a projection
    /// ('EXPECTED TO FORM'), and office changes when the procedure completes"; the audit's GA-3-5): <b>WHAT THE NIGHT EXPECTS TO FORM</b> - the country's own
    /// procedure, projected from the night by the code that runs it (`SimulationManager.ProjectFormation`): the Riksdag's Speaker's round, proposal by
    /// proposal to its sourced limit; the Bundestag's chancellor election to its first ballot (Art. 63 Abs. 1-2 GG); the Sejm's steps under Art. 162 and
    /// 154. Nothing in it is installed: office changes only when the procedure itself completes. A wait on the player is answered by the AI's rule for the
    /// player's party (K49, `SimulationManager.AnswerWaitsAsTheAi`) - the projection says where it read one, because the player's own answer can part from it.
    /// </summary>
    public sealed class FormationProjection
    {
        public CountryId Country;
        public DateTime PollingDay;
        /// <summary>The procedure projected, in the night's words.</summary>
        public string Procedure;
        /// <summary>The order the procedure asks in - the Speaker's (premise 6), the Bundespräsident's; empty for the Sejm.</summary>
        public List<string> Order = new List<string>();
        public string FirstAsked;
        /// <summary>The cabinet the first party asked drafts (`SimulationManager.DraftProposal`) - the round's own first draft; empty for the Sejm.</summary>
        public List<string> FirstDraft = new List<string>();
        public bool PlayerAskedFirst;
        /// <summary>A government is expected to form by the procedure's own steps.</summary>
        public bool Expected;
        /// <summary>The expected government's prime minister's party, its cabinet and the parties carrying it from outside.</summary>
        public string PmParty;
        public List<string> Cabinet = new List<string>();
        public List<string> Support = new List<string>();
        /// <summary>The Sejm's: the record's head of the expected cabinet where the record names it (null where the formation's projection is elected), and the
        /// vote that carries it - its members for, of the members.</summary>
        public string Head;
        public int VotesFor, Members;
        /// <summary>The step the expected government comes from - the Speaker's proposal (1 the first), the Bundestag's ballot, the Sejm's step - in words.</summary>
        public string Step;
        /// <summary>The projection answered a wait on the player (an offer, or the player's party asked) by the AI's rule for the player's party.</summary>
        public bool ReadsPlayersAnswer;
        /// <summary>The projection stops short of the procedure's end (the Bundestag's fourteen days, Art. 63 Abs. 3), and says where.</summary>
        public bool Partial;
        /// <summary>Where no government is expected: why, in the procedure's own terms.</summary>
        public string Reason;
        /// <summary>The steps projected, dated by their place in the procedure (not by day): the log a check prints.</summary>
        public List<string> Steps = new List<string>();
        /// <summary>The board's view of the expected government (the cabinet and its seats, its supporters, its investiture); null where the procedure
        /// draws none (the Sejm's night draws its sentence alone).</summary>
        public GovernmentFormation.View View;

        /// <summary>The cabinet in the authority's short names, joined as the verdicts join them.</summary>
        public string CabinetLine() => Join(Country, Cabinet);

        public static string Join(CountryId country, IReadOnlyList<string> keys)
        {
            var names = new List<string>(keys.Count);
            foreach (string k in keys) { names.Add(PartySystems.ShortName(country, k)); }
            return string.Join("+", names);
        }

        /// <summary>One line for the logs and the checks: the procedure, what it is expected to form and from which step.</summary>
        public string Describe() => string.Format(CultureInfo.InvariantCulture, "{0} on {1:yyyy-MM-dd}: {2}{3}{4}", Procedure, PollingDay,
            Expected ? "EXPECTED TO FORM " + string.Join("+", Cabinet) + " led by " + PmParty + (Support.Count > 0 ? " with " + string.Join("+", Support) : string.Empty) + " - " + Step
                     : "NO GOVERNMENT EXPECTED - " + Reason,
            ReadsPlayersAnswer ? " (a wait on the player answered by the AI's rule for the player's party)" : string.Empty,
            Partial ? " (projected to the first ballot only)" : string.Empty);
    }
}
