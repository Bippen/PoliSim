using System;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// AF-11 (Elias's ruling q11 of 2026-10-18, `docs/RULINGS_2026-10-18.md`: <i>"every lever the UI writes gets a manager verb, and the impact ledger
    /// reads it"</i>; the audit's GA-0-55) - the levers a player pulls OUTSIDE the turn's <see cref="PolicyDecision"/>: each acts on the day it is pulled,
    /// through its <see cref="SimulationManager"/> verb, and rides no bill. <see cref="UiLeverVerbCheck"/> fails the cheap bar on a UI file that writes one
    /// of their fields itself.
    /// </summary>
    public enum PlayerLeverKind
    {
        /// <summary>"Set override": a trade partner's rate pinned at today's - <see cref="SimulationManager.SetPartnerTariffOverride"/>.</summary>
        PartnerTariffOverride,
        /// <summary>The central bank's head appointed - <see cref="SimulationManager.AppointCentralBankHead"/>.</summary>
        CentralBankHead,
        /// <summary>A power-plant order placed, a build or a retirement - <see cref="SimulationManager.PlaceFleetOrder"/>.</summary>
        FleetOrder,
        /// <summary>A minister appointed to a vacant portfolio - <see cref="SimulationManager.AppointMinister"/>.</summary>
        MinisterAppointment,
        /// <summary>A minister reshuffled out, the approval cost paid - <see cref="SimulationManager.ReshuffleMinister"/>.</summary>
        MinisterReshuffle,
        /// <summary>The fiscal stance a player partner holding Finance asks (§755) - <see cref="SimulationManager.SetFinanceStanceTarget"/>, a verb before AF-11
        /// that wrote outside the turn's decision, so every family forked before it was booked its effect (§544 (b)'s class).</summary>
        FinanceStance,
    }

    /// <summary>
    /// AF-11: one lever as pulled - its kind, its country and its ACT. The act is the verb's own write, held so it can be done again on a counterfactual
    /// world's copy of the country: the impact ledger replays it on every except-world but its own family's (<see cref="PolicyImpactLedger.AfterLever"/>),
    /// as it hands every except-world the turn's decision less that family's fields. ⚠ The act is replayed, not its result - a pin at today's rate is
    /// today's rate in the world it lands in, as a dial's move is.
    /// </summary>
    public sealed class PlayerLever
    {
        public PlayerLeverKind Kind { get; }
        public CountryId CountryId { get; }

        /// <summary>The act: the world, the country, and whether this is a REPLAY on a counterfactual world (which takes copies of what it seats, GA-1-5:
        /// no object is shared between two worlds); true where it changed something.</summary>
        private readonly Func<World, Country, bool, bool> _act;

        internal PlayerLever(PlayerLeverKind kind, CountryId countryId, Func<World, Country, bool, bool> act)
        {
            Kind = kind;
            CountryId = countryId;
            _act = act;
        }

        /// <summary>The lever's act on <paramref name="country"/> in <paramref name="world"/> - the real game's (<paramref name="replay"/> false) or a
        /// counterfactual world's copy of it; true where it changed something.</summary>
        internal bool ApplyTo(World world, Country country, bool replay) => world != null && country != null && _act(world, country, replay);
    }

    /// <summary>
    /// FORK-2 (D-45's remainder and the s871 review's M2): one of the player's NON-LEVER answers as given - a meeting or a cabinet decision answered, a bill
    /// introduced or a shadow budget tabled, a docket bill signed or vetoed, a resolution withheld or released, a motion, an extra election, a formation's
    /// proposal or an offer's answer, a campaign decision or a story's answer. Its REPLAY is the verb itself, called again on a counterfactual world's manager
    /// with that world's own objects (the meeting's option by its place in the meeting, a docket bill by its place on the docket, a bill as a fresh copy of
    /// the one the player wrote): the impact ledger replays it in every except-world on the day it was given (<see cref="PolicyImpactLedger.AfterAnswer"/>).
    /// An answer is no dial and no lever - it belongs to no family, so no except-world leaves it out (DECLARED, FORK-2). K51 (Elias's ruling of 2026-10-23:
    /// "each kind of bill is its own ledger family"): a BILL the player lays is the exception - it carries its kind (`Bill`), and the ledger reads it as a
    /// lever of that kind's family (`PolicyImpactLedger.BeforeAnswer`): its family's world never lays it, and every other world lays it again.
    /// </summary>
    public sealed class PlayerAnswer
    {
        public string What { get; }
        public CountryId? CountryId { get; }

        /// <summary>K51: the kind of bill this answer lays (`Congress.Kind`) - null for every answer that lays none (a meeting, the docket, a motion, a formation,
        /// the campaign, the debt limit's verbs: DECLARED, they stay answers of no family).</summary>
        public Congress.Kind? Bill { get; }

        /// <summary>The replay: the verb on a counterfactual's manager; true where it acted.</summary>
        private readonly Func<SimulationManager, bool> _replay;

        /// <summary>FORK-2 (the review's H2/M2): true for an answer to a WAIT on the player - a meeting, the Speaker's question, an offer. Where its replay finds
        /// no match in a counterfactual world (that world rolled another meeting, or its round stands elsewhere), the world answers its waits by the AI's rule
        /// from then on (<see cref="SimulationManager.AnswerWaitsAsTheAi"/>), so it never stalls on a player who will not answer there.</summary>
        public bool AnswersAWait { get; }

        internal PlayerAnswer(string what, CountryId? countryId, Func<SimulationManager, bool> replay, bool answersAWait = false)
            : this(what, countryId, replay, answersAWait, null)
        {
        }

        /// <summary>K51: an answer that lays a bill of <paramref name="bill"/>'s kind.</summary>
        internal PlayerAnswer(string what, CountryId? countryId, Func<SimulationManager, bool> replay, bool answersAWait, Congress.Kind? bill)
        {
            What = what;
            CountryId = countryId;
            _replay = replay;
            AnswersAWait = answersAWait;
            Bill = bill;
        }

        /// <summary>The answer given again on <paramref name="sim"/>, a counterfactual world's manager; true where it acted - false where the world holds
        /// nothing it matches by identity (the meeting by its name, the decision by its name and portfolio, the docket bill by its passage's number, date and
        /// title, the round by the party asked), so the answer lands on no other item.</summary>
        internal bool ReplayOn(SimulationManager sim) => sim != null && _replay(sim);

        /// <summary>A fresh copy of <paramref name="value"/> each time it is asked, taken through the save layer's own serializer from the text the value had
        /// when the answer was given (a bill's slot moves on in the played world, and no object is shared between two worlds - GA-1-5).</summary>
        internal static Func<T> Copies<T>(T value) where T : class
        {
            if (value == null) { return () => null; }
            Newtonsoft.Json.JsonSerializerSettings settings = Persistence.SaveGameService.BuildSettings();
            string text = Newtonsoft.Json.JsonConvert.SerializeObject(value, settings);
            return () => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, settings);
        }
    }
}
