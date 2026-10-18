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
}
