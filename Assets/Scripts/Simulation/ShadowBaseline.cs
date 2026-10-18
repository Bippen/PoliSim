using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// C-C9 (P-G1): the no-policy counterfactual, advanced beside the real game so every economic graph
    /// can show **"with your policies"** against **"without"**.
    ///
    /// <para>⚠ <b>THIS CLASS EXISTS BECAUSE A NAIVE SHADOW IS A DETERMINISM BREAK.</b>
    /// `SimulationRandom` is a global static with one counting generator per stream, and the save layer
    /// persists those positions because they are load-bearing. Measured before any of this was written
    /// (`ShadowFeasibilityDiagnostic`, `COMPLETED.md` §103): <b>one shadow turn consumes 41 real draws</b>
    /// — `PublicationRevision` 18, `SovereignWealth` 16, `Event` 7. Advanced naively, merely LOOKING at
    /// the counterfactual would change the future of the game being played — a determinism break arriving
    /// inside a feature whose entire purpose is to be a passive read.</para>
    ///
    /// <para><b>So the shadow runs on its own stream position and puts the real one back.</b>
    /// <see cref="AdvanceTurn"/> saves the real generator state, swaps in the shadow's own, advances,
    /// saves the shadow's new position, and restores the real state <b>in a `finally`</b> — because an
    /// exception midway through a shadow turn must not leave the real game's randomness shifted. From
    /// the real game's side the whole operation consumes <b>zero</b> draws, and
    /// `ShadowBaselineDiagnostic` asserts exactly that rather than leaving it to the reading.</para>
    ///
    /// <para><b>Cost, measured and accepted rather than optimised on a guess</b> (the pre-ruling's own
    /// terms): a real turn ~50 ms, a shadow turn ~97 ms, the pair ~148 ms. ⚠ <b>The named fallback if
    /// play finds that cost</b> is LAZY COMPUTATION — advance the shadow only when a screen actually
    /// reads it, rather than every turn — recorded here so the next person does not invent a different
    /// one under pressure. Nothing is optimised until play says it needs to be.</para>
    ///
    /// <para>⚠ <b>The shadow is a SECOND WORLD, not a second player.</b> It is seeded from the same
    /// `WorldFactory` at the same master seed and advanced with `PolicyDecision.None()` for every
    /// country, so it is the trajectory the game would have had if the player had done nothing — which
    /// is precisely the recorded no-policy baseline family, and what the diagnostic checks it against.</para>
    /// </summary>
    public sealed class ShadowBaseline
    {
        private readonly GameObject _host;
        private SimulationManager _sim;
        private World _world;
        private readonly Dictionary<CountryId, PolicyDecision> _noDecisions = new Dictionary<CountryId, PolicyDecision>();

        /// <summary>The shadow's own generator, kept between turns so its run is continuous even though the real game's is swapped in and out
        /// around it. GA-1-3 (2026-10-14): kept as it LIVES - the stream objects, not their counts - so a campaign the shadow carries goes on
        /// drawing on the objects its table holds, as the real game's does (`SimulationRandom.LiveState`).</summary>
        private SimulationRandom.LiveState _own;

        /// <summary>AF-11: the days this world has run of the turn in progress. 0 for a world from the seed, and after every boundary; a fork takes the
        /// real game's (<see cref="SimulationManager.DaysIntoTurn"/>) - a fork made inside a turn, or at its last day before the boundary, has run those
        /// days already, so <see cref="AdvanceTurn"/> runs only the rest and the world crosses the boundary on the real game's day.</summary>
        private int _daysRun;

        /// <summary>AF-11: the levers the real game pulled on a day of this turn this world has not reached yet, in the order pulled - each done when
        /// the day loop reaches its day (<see cref="Schedule"/>).</summary>
        private readonly List<KeyValuePair<int, PlayerLever>> _scheduled = new List<KeyValuePair<int, PlayerLever>>();

        /// <summary>The counterfactual world. Read-only to callers by convention — advancing it is
        /// <see cref="AdvanceTurn"/>'s job, because only that path protects the real generator.</summary>
        public World World => _world;

        public ShadowBaseline(int masterSeed)
        {
            // Its own host object so the shadow manager's own lifetime is ours, and hidden so it never
            // appears in a scene view as a second game. ⚠ NOT DontDestroyOnLoad: that is play-mode only and
            // throws in the Editor, where the proof gate runs - and the host is explicitly disposed anyway.
            _host = new GameObject("ShadowBaseline") { hideFlags = HideFlags.HideAndDontSave };

            _sim = _host.AddComponent<SimulationManager>();
            _world = WorldFactory.CreateDefault();
            _sim.SetWorld(_world);

            foreach (Country c in _world.Countries) { _noDecisions[c.Id] = PolicyDecision.None(); }

            // The shadow starts where the real game starts: same master seed, every stream at zero.
            _own = new SimulationRandom.LiveState { MasterSeed = masterSeed, Streams = new Dictionary<SimulationRandom.Stream, CountingRandom>() };
        }

        /// <summary>
        /// C-C10 (P-G2): fork a counterfactual **from a game already in progress**, exact as of this
        /// moment, rather than from the seed.
        ///
        /// <para>⚠ <b>Why the impact ledger needs this.</b> Attributing a divergence to the dial that
        /// caused it is leave-one-out: a world running everything the player did EXCEPT one family of
        /// dials. A family the player has never touched has an except-world that is IDENTICAL to the real
        /// game, so it costs nothing to not have one — and the moment they first touch it, the correct
        /// starting state is the real game's state right then. Forking on first touch is therefore not an
        /// approximation of the from-the-seed world; it is exactly equal to it, at a fraction of the cost
        /// and with no per-family cost at all until a family is used.</para>
        ///
        /// <para><b>The copy is a save/load round trip</b> — `CreateSaveGame` → `Serialize` →
        /// `Deserialize` → `RestoreInto` — rather than a hand-written deep clone. That path is the one
        /// `SaveLoadRoundTripDiagnostic` proves clean over 12 scenarios every run, and R4-1's clone-escape
        /// class is the standing reason not to write a second copier that a new field can silently fall
        /// out of.</para>
        ///
        /// <para>⚠ `RestoreInto` restores the GLOBAL generator, so the whole fork sits inside the same
        /// save/swap/restore-in-a-`finally` discipline as <see cref="AdvanceTurn"/>.</para>
        ///
        /// <para>⚠ AF-11: <paramref name="insideTurn"/> - a fork made between a turn's days (a lever's moment, <see cref="PolicyImpactLedger.BeforeLever"/>;
        /// a load, which `GameController` follows with the played world's `EnergyMarket.BeginTurn` and a reset after it would undo)
        /// builds its host INACTIVE, so the manager's `Awake` never runs: in play mode it builds a default world, and `WorldFactory.CreateDefault` resets the
        /// energy market's turn state (`EnergyMarket.ResetTurnState`), which the REAL game's remaining days of the turn read. The fork's world is the save's,
        /// so the default world was never used. The fork at a boundary keeps the host it always had (the boundary sets the market's turn state at its top).</para>
        /// </summary>
        public ShadowBaseline(SimulationManager realSim, World realWorld, CountryId playerCountryId, bool insideTurn = false)
        {
            _host = new GameObject("ShadowBaseline (forked)") { hideFlags = HideFlags.HideAndDontSave };
            if (insideTurn) { _host.SetActive(false); }
            _sim = _host.AddComponent<SimulationManager>();

            Persistence.SaveGame snapshot = Persistence.SaveGameService.CreateSaveGame(realSim, realWorld, playerCountryId, null);
            string json = Persistence.SaveGameService.Serialize(snapshot);

            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();   // GA-1-3: the real game's stream OBJECTS, handed back as they were
            try
            {
                Persistence.SaveGame copy = Persistence.SaveGameService.Deserialize(json);
                Persistence.SaveGameService.RestoreInto(_sim, copy);
                _world = _sim.World;
                _daysRun = Mathf.Clamp(_sim.DaysIntoTurn, 0, SimulationManager.DaysPerTurn);   // AF-11: the real game's day, not the turn's start

                _own = SimulationRandom.CaptureLive();
            }
            finally
            {
                SimulationRandom.Reinstate(real);
            }

            foreach (Country c in _world.Countries) { _noDecisions[c.Id] = PolicyDecision.None(); }
        }

        /// <summary>
        /// Advances the counterfactual by one turn — a full period of days, then the boundary.
        /// <paramref name="decisions"/> defaults to no player decision anywhere, which is the no-policy
        /// baseline; C-C10 passes the player's real decisions minus one family of dials.
        ///
        /// ⚠ **The real generator is saved, swapped away, and restored in a `finally`.** That is the
        /// whole safety property of this class: from the real game's side, this call consumes zero draws
        /// and leaves the master seed unchanged, whatever happens inside it.
        /// </summary>
        public void AdvanceTurn(Dictionary<CountryId, PolicyDecision> decisions = null)
        {
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();   // GA-1-3: the real game's stream OBJECTS, handed back as they were
            bool swapped = false;

            try
            {
                SimulationRandom.Reinstate(_own);
                swapped = true;

                // AF-11: the turn's days this world has not run (all of them, but for a fork made inside the turn), each lever on its day
                DoScheduled();
                for (; _daysRun < SimulationManager.DaysPerTurn; _daysRun++) { _sim.AdvanceDay(); DoScheduled(_daysRun + 1); }
                DoScheduled(int.MaxValue);
                _sim.AdvanceTurn(decisions ?? _noDecisions);
                _daysRun = 0;
            }
            finally
            {
                // GA-1-3's review: the shadow's own objects advance in place, so its state is whatever they hold now - taken here, a turn that throws
                // keeps the streams it made; and only once the shadow's state is in, or the real game's objects would become the shadow's
                if (swapped) { _own = SimulationRandom.CaptureLive(); }
                SimulationRandom.Reinstate(real);
            }
        }

        /// <summary>
        /// AF-11: the real game pulled <paramref name="lever"/> <paramref name="day"/> days into the turn; this world does the same act on its own copy
        /// of the country when it reaches that day - now, where it has (a fork made at the lever's moment), or in <see cref="AdvanceTurn"/>'s day loop.
        /// ⚠ An act draws no random number (an assignment, a queued order, a booked approval), so doing one outside the generator swap moves no stream.
        /// </summary>
        internal void Schedule(int day, PlayerLever lever)
        {
            if (day <= _daysRun) { lever.ApplyTo(_world, CountryFor(lever.CountryId), replay: true); return; }
            _scheduled.Add(new KeyValuePair<int, PlayerLever>(day, lever));
        }

        /// <summary>Does every scheduled lever whose day this world has reached (<paramref name="reached"/>, by default the days run), in the order pulled.</summary>
        private void DoScheduled(int reached = -1)
        {
            if (_scheduled.Count == 0) { return; }
            if (reached < 0) { reached = _daysRun; }
            for (int i = 0; i < _scheduled.Count;)
            {
                if (_scheduled[i].Key > reached) { i++; continue; }
                PlayerLever lever = _scheduled[i].Value;
                _scheduled.RemoveAt(i);
                lever.ApplyTo(_world, CountryFor(lever.CountryId), replay: true);
            }
        }

        /// <summary>The counterfactual country matching a real one, or null if the shadow does not hold it.</summary>
        public Country CountryFor(CountryId id) => _world.GetCountry(id);

        public void Dispose()
        {
            if (_host != null) { Object.DestroyImmediate(_host); }
        }
    }
}
