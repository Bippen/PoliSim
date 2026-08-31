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
        private readonly World _world;
        private readonly Dictionary<CountryId, PolicyDecision> _noDecisions = new Dictionary<CountryId, PolicyDecision>();

        /// <summary>The shadow's own generator position, kept between turns so its run is continuous even
        /// though the real game's state is swapped in and out around it.</summary>
        private int _masterSeed;
        private Dictionary<SimulationRandom.Stream, int> _drawCounts;

        /// <summary>The counterfactual world. Read-only to callers by convention — advancing it is
        /// <see cref="AdvanceTurn"/>'s job, because only that path protects the real generator.</summary>
        public World World => _world;

        public ShadowBaseline(int masterSeed)
        {
            _masterSeed = masterSeed;

            // Its own host object so the shadow manager's own lifetime is ours, and hidden so it never
            // appears in a scene view as a second game. ⚠ NOT DontDestroyOnLoad: that is play-mode only and
            // throws in the Editor, where the proof gate runs - and the host is explicitly disposed anyway.
            _host = new GameObject("ShadowBaseline") { hideFlags = HideFlags.HideAndDontSave };

            _sim = _host.AddComponent<SimulationManager>();
            _world = WorldFactory.CreateDefault();
            _sim.SetWorld(_world);

            foreach (Country c in _world.Countries) { _noDecisions[c.Id] = PolicyDecision.None(); }

            // The shadow starts where the real game starts: same master seed, every stream at zero.
            _drawCounts = new Dictionary<SimulationRandom.Stream, int>();
        }

        /// <summary>
        /// Advances the counterfactual by one turn — a full period of days, then the boundary — with no
        /// player decision anywhere.
        ///
        /// ⚠ **The real generator is saved, swapped away, and restored in a `finally`.** That is the
        /// whole safety property of this class: from the real game's side, this call consumes zero draws
        /// and leaves the master seed unchanged, whatever happens inside it.
        /// </summary>
        public void AdvanceTurn()
        {
            int realSeed = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> realCounts = SimulationRandom.CaptureDrawCounts();

            try
            {
                SimulationRandom.RestoreState(_masterSeed, _drawCounts);

                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { _sim.AdvanceDay(); }
                _sim.AdvanceTurn(_noDecisions);

                _masterSeed = SimulationRandom.MasterSeed;
                _drawCounts = SimulationRandom.CaptureDrawCounts();
            }
            finally
            {
                SimulationRandom.RestoreState(realSeed, realCounts);
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
