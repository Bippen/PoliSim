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
    ///
    /// <para>⚠ <b>AF-12 (ruling q12, 2026-10-18): the GAME's shadow is no longer the seeded one.</b> "Without your policies" is the player's party
    /// governing with no changes from the start, so a new game forks this world from its own start (the fork constructor, the player's seat in it) and a
    /// save carries it across a load (<see cref="Carry"/>, <see cref="FromCarried"/>). The seeded world - no player, the AI governing every country - stays
    /// the world a fresh process holds before a country is chosen, and the proof gate's.</para>
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
        /// the day loop reaches its day (<see cref="Schedule(int, PlayerLever)"/>). FORK-2: and the player's answers, in the same order with the levers
        /// (<see cref="Schedule(int, PlayerAnswer)"/>) - each entry is one or the other.</summary>
        private readonly List<(int Day, PlayerLever Lever, PlayerAnswer Answer)> _scheduled = new List<(int Day, PlayerLever Lever, PlayerAnswer Answer)>();

        /// <summary>FORK-2 (D-45's remainder): whether this world holds the player's election of the chamber on its polling day
        /// (<see cref="SimulationManager.HoldPlayerElection"/>), as the controller holds the played world's - true for every world that steps the player's
        /// day (<c>dayTick</c>). A check's control sets it false to stand for a fork as it was before FORK-2.</summary>
        public bool HoldsPlayerElection { get; set; } = true;

        /// <summary>FORK-2 (the reviews' H2 and H1; DECLARED, put to Elias): this world answers every wait on the player that no replayed answer covers by the
        /// AI's rule for the player's party (<see cref="SimulationManager.AnswerWaitsAsTheAi"/>) - set by the impact ledger on EVERY world it holds, the no-policy
        /// world (handed no answer) and each family's (a wait the played world never had, or one whose answer found no match there). The rule runs at the
        /// START of the next day: the played clock holds on every such wait (`GameController.Update`'s gates), so the player's answer, given the same day, is
        /// always replayed first - and a save cut during a held wait never answers it before the player does. Off (a world no ledger holds, a check's control),
        /// a world waits as the played world waits.</summary>
        public bool AnswersWaitsAsTheAi { get; set; }

        /// <summary>FORK-2 (the review's M3): why this world stopped - an exception in its days, its boundary, its election or a replay, caught and logged so it
        /// never reaches the played game; null while it runs. A failed world advances no further and is carried in no save; the ledger sets its family aside.</summary>
        public string Faulted { get; private set; }

        /// <summary>FORK-1 (Elias's ruling J8 of 2026-10-19, D-45's class): the country whose day tick (<see cref="SimulationManager.AdvanceCountryDayTick"/>)
        /// this world runs after each day, as the played world runs its player's - the real manager's <see cref="SimulationManager.PlayerCountryId"/> at the
        /// fork, for the impact ledger's except-worlds. The no-policy world from the game's start and a carried one tick it too (AF-12 with the s871 review's H1). Null for the seeded world and for a fork made
        /// without <c>dayTick</c>, which step the days alone, as every shadow did before J8.</summary>
        private readonly CountryId? _dayTickFor;

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
            _sim.IsCounterfactual = true;   // AF-7: a counterfactual world - the player's bank's head is the AI's keyed pick at its term's end, no hold (every constructor sets it)
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
        ///
        /// <para>⚠ FORK-1 (J8): <paramref name="dayTick"/> - the fork steps its days as the played world does, each day's
        /// <see cref="SimulationManager.AdvanceCountryDayTick"/> for the real manager's player country after the day (the controller's order). Without it a
        /// fork ran no bill countdown, no budget window and no US funding horizon, so a lapse in the played world was missing from every except-world and
        /// booked to every family the player had touched (§865's M4), and a fork could differ from the played world with no policy difference.
        /// The impact ledger's forks pass it (<see cref="PolicyImpactLedger"/>); a real manager with no player country has no tick to mirror.</para>
        ///
        /// <para>⚠ LEDGER-1 (filed §871; `PolicyImpactLedgerDiagnostic`'s section 4): <paramref name="seatAsPlayed"/> - the fork seats exactly the player the
        /// real manager seats (<see cref="SimulationManager.PlayerCountryId"/>), NONE where it seats none, rather than <paramref name="playerCountryId"/>. A save
        /// always names a player country, and restored it seated one: a fork of a world with no player (a harness's) then ran the player's paths - the record's
        /// chamber seated on its date, the player's US count, the player's budget by bill - that the world it copied never ran, so the lazy fork was not
        /// that world. The impact ledger's except-worlds pass it; in the game the two are the same country.</para>
        /// </summary>
        public ShadowBaseline(SimulationManager realSim, World realWorld, CountryId playerCountryId, bool insideTurn = false, bool dayTick = false, bool seatAsPlayed = false)
        {
            _dayTickFor = dayTick ? realSim.PlayerCountryId : null;
            _host = new GameObject("ShadowBaseline (forked)") { hideFlags = HideFlags.HideAndDontSave };
            if (insideTurn) { _host.SetActive(false); }
            _sim = _host.AddComponent<SimulationManager>();
            _sim.IsCounterfactual = true;   // AF-7: a counterfactual world - the player's bank's head is the AI's keyed pick at its term's end, no hold (every constructor sets it)

            CountryId header = seatAsPlayed && realSim.PlayerCountryId.HasValue ? realSim.PlayerCountryId.Value : playerCountryId;   // a save names one; LEDGER-1: the played seat where there is one
            bool seats = !seatAsPlayed || realSim.PlayerCountryId.HasValue;
            Persistence.SaveGame snapshot = Persistence.SaveGameService.CreateSaveGame(realSim, realWorld, header, null);
            string json = Persistence.SaveGameService.Serialize(snapshot);

            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();   // GA-1-3: the real game's stream OBJECTS, handed back as they were
            try
            {
                Persistence.SaveGame copy = Persistence.SaveGameService.Deserialize(json);
                Persistence.SaveGameService.RestoreInto(_sim, copy, seats);
                _world = _sim.World;
                _daysRun = Mathf.Clamp(_sim.DaysIntoTurn, 0, SimulationManager.DaysPerTurn);   // AF-11: the real game's day, not the turn's start
                _sim.RecomputePollingDayToday();   // FORK-2 (the review's M1): a fork made on the polling day before the boundary holds that day's election after it

                _own = SimulationRandom.CaptureLive();
            }
            catch
            {
                Object.DestroyImmediate(_host);   // FORK-2 (the fix review's L3): a fork that cannot be made leaves no hidden host behind
                throw;
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
        public void AdvanceTurn(Dictionary<CountryId, PolicyDecision> decisions = null) => OnOwnGenerator(() =>
        {
            // AF-11: the turn's days this world has not run (all of them, but for a fork made inside the turn), each lever on its day
            DoScheduled();
            RunDaysThrough(SimulationManager.DaysPerTurn);
            DoScheduled(int.MaxValue);
            _sim.AdvanceTurn(decisions ?? _noDecisions);
            _daysRun = 0;
            HoldTheElection();   // FORK-2: a polling day that is the turn's last is held after the boundary, as the controller holds it
        });

        /// <summary>
        /// FORK-2 (the review's M3 and L2): <paramref name="step"/> run on this world's own generator - the real game's stream objects saved, swapped away and
        /// handed back in a `finally` (GA-1-3: the shadow's own objects advance in place, so its state is whatever they hold after, taken only once the shadow's
        /// state is in) - with this world's info lines kept out of the played game's log (warnings and errors pass), and NEVER THROWING: an exception marks the
        /// world <see cref="Faulted"/>, is logged, and the world steps no further. A failed world does nothing.
        /// </summary>
        private void OnOwnGenerator(System.Action step)
        {
            if (Faulted != null) { return; }
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();   // GA-1-3: the real game's stream OBJECTS, handed back as they were
            bool swapped = false;
            ILogger log = Debug.unityLogger;
            LogType shown = log.filterLogType;
            System.Action putBack = Elections.NationalElection.KeepRegionalReadout();   // the fix review's L2: every counterfactual step leaves the played world's regional readout as it found it
            try
            {
                SimulationRandom.Reinstate(_own);
                swapped = true;
                // the fix review's L7: the stricter of the caller's filter and warnings-and-errors (Exception lets everything through)
                log.filterLogType = shown == LogType.Exception ? LogType.Warning : (LogType)Mathf.Min((int)shown, (int)LogType.Warning);
                step();
            }
            catch (System.Exception e)
            {
                Faulted = $"{e.GetType().Name} on {(_sim != null ? _sim.CurrentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "?")}: {e.Message}";
                Debug.LogError($"SHADOW: a counterfactual world failed and steps no further - {Faulted}\n{e.StackTrace}");
            }
            finally
            {
                log.filterLogType = shown;
                putBack();
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
            if (day <= _daysRun) { _carried = null; OnOwnGenerator(() => lever.ApplyTo(_world, CountryFor(lever.CountryId), replay: true)); return; }   // FORK-2: a world changed on its carried day is carried again; never throwing (M3)
            _scheduled.Add((day, lever, null));
        }

        /// <summary>
        /// FORK-2 (D-45's remainder): the real game's player gave <paramref name="answer"/> <paramref name="day"/> days into the turn; this world gives it again
        /// on its own manager when it reaches that day - now, where it has, or in <see cref="AdvanceTurn"/>'s day loop - in the order given among the levers.
        /// ⚠ Unlike a lever's act, an answer may draw (a bill voted at once, a docket answer acted on), so given now it runs on this world's own generator.
        /// </summary>
        internal void Schedule(int day, PlayerAnswer answer)
        {
            if (answer == null) { return; }
            if (day > _daysRun) { _scheduled.Add((day, null, answer)); return; }
            _carried = null;   // a world changed on its carried day is carried again
            OnOwnGenerator(() => Replay(answer));
        }

        /// <summary>FORK-2 (the review's M2): the answer given again here - and where it finds no match (another meeting, a round standing elsewhere), one line
        /// saying so; a wait it would have answered is then answered by the AI's rule at the start of the next day (<see cref="AnswersWaitsAsTheAi"/>).</summary>
        private void Replay(PlayerAnswer answer)
        {
            if (answer.ReplayOn(_sim)) { return; }
            AnsweredAlone = true;   // K46: this world now parts from the played one with no act withheld
            Debug.LogWarning($"SHADOW: {answer.What} found no match in a counterfactual world on {_sim.CurrentDate:yyyy-MM-dd}{(answer.AnswersAWait && AnswersWaitsAsTheAi ? " - its wait is answered by the AI's rule the next day" : " - nothing was answered there")}");
        }

        /// <summary>FORK-2 (the fix review's H1): the previous day's waits on the player answered by the AI's rule, at the START of a day, in a world that answers
        /// them so (<see cref="AnswersWaitsAsTheAi"/>) - after every answer the player gave that day has been replayed.</summary>
        private void AnswerTheWaits()
        {
            if (AnswersWaitsAsTheAi && _dayTickFor.HasValue && _sim.AnswerWaitsAsTheAi(_dayTickFor.Value)) { AnsweredAlone = true; }   // K46: answered here and not in the played world
        }

        /// <summary>K46 (Elias's ruling of 2026-10-23): true once this world has answered something the played world did not - a replay that found no match, or a
        /// wait answered by the AI's rule - so it may part from the played world with no act withheld from it; the impact ledger then never reads it as the played
        /// world (`PolicyImpactLedger.Carry`). Not carried: a world a save carried is never read so again.</summary>
        public bool AnsweredAlone { get; private set; }

        /// <summary>Does every scheduled lever and answer whose day this world has reached (<paramref name="reached"/>, by default the days run), in the order
        /// given. ⚠ Called with this world's generator in (<see cref="AdvanceTurn"/>, <see cref="CatchUp"/>).</summary>
        private void DoScheduled(int reached = -1)
        {
            if (_scheduled.Count == 0) { return; }
            if (reached < 0) { reached = _daysRun; }
            for (int i = 0; i < _scheduled.Count;)
            {
                if (_scheduled[i].Day > reached) { i++; continue; }
                var (_, lever, answer) = _scheduled[i];
                _scheduled.RemoveAt(i);
                if (lever != null) { lever.ApplyTo(_world, CountryFor(lever.CountryId), replay: true); }
                else { Replay(answer); }
            }
        }

        /// <summary>
        /// FORK-1 (J8) with FORK-2: the turn's days up to <paramref name="through"/>, each in the played world's order (`GameController.Update`) - yesterday's
        /// unanswered waits by the AI's rule where this world answers them so, the day, the
        /// player's day tick, the player's election where the day is the polling day (not on the turn's last day: the controller holds that one after the
        /// boundary, <see cref="AdvanceTurn"/>), then the levers pulled and the answers given on it. ⚠ Called with this world's generator in.
        /// </summary>
        private void RunDaysThrough(int through)
        {
            for (; _daysRun < through; _daysRun++)
            {
                AnswerTheWaits();   // the fix review's H1: the start of the day - what the player left unanswered yesterday is the AI's
                _sim.AdvanceDay();
                if (_dayTickFor.HasValue) { _sim.AdvanceCountryDayTick(_dayTickFor.Value); }
                if (_daysRun + 1 < SimulationManager.DaysPerTurn) { HoldTheElection(); }
                DoScheduled(_daysRun + 1);
            }
        }

        /// <summary>FORK-2 (D-45's remainder): the player's election of the chamber, where today is its polling day - in a world that steps the player's day
        /// (<c>dayTick</c>), as the controller holds the played world's (<see cref="SimulationManager.HoldPlayerElection"/>; nothing on any other day).</summary>
        private void HoldTheElection()
        {
            if (_dayTickFor.HasValue && HoldsPlayerElection) { _sim.HoldPlayerElection(); }
        }

        /// <summary>
        /// FORK-2 (the s871 review's M1): brings this world to <paramref name="day"/> days into the turn - the real game's - doing every lever and answer it has
        /// been handed up to it, so it holds nothing it has not done and a save can carry it (<see cref="Carry"/>). The same days in the same order as the
        /// boundary would run them (<see cref="RunDaysThrough"/>), only sooner; never across the boundary. A world already there does nothing.
        /// </summary>
        public void CatchUp(int day)
        {
            int through = Mathf.Clamp(day, 0, SimulationManager.DaysPerTurn - 1);
            if (_daysRun >= through && _scheduled.Count == 0) { return; }
            OnOwnGenerator(() =>
            {
                DoScheduled();
                RunDaysThrough(through);
            });
        }

        /// <summary>The counterfactual country matching a real one, or null if the shadow does not hold it.</summary>
        public Country CountryFor(CountryId id) => _world.GetCountry(id);

        public void Dispose()
        {
            if (_host != null) { Object.DestroyImmediate(_host); }
        }

        // ---------------------------------------------------------------------------------------------
        // AF-12 (Elias's ruling q12 of 2026-10-18): "without your policies" is the player's party governing with no changes FROM THE START, and after a load
        // it is the same world - CARRIED IN THE SAVE, the cheaper of the two ways measured (`NoPolicyShadowCarryDiagnostic` times both and fails while the
        // re-run from the start would be the cheaper). The game forks this world from its own start (C-C10's fork, the player's seat in it), advances it
        // with no decision, writes it into every save it makes (`SaveGame.NoPolicyShadow`) and restores it on the load, where until AF-12 it forked the
        // loaded game, so the line counted from the load.
        // ---------------------------------------------------------------------------------------------

        /// <summary>AF-12: the carried world's text, kept for the shadow's date - the world moves only when it advances, so the saves of a turn write one
        /// snapshot (an autosave between two boundaries pays the bytes and not the serialization).</summary>
        private string _carried;
        private System.DateTime _carriedOn;

        /// <summary>
        /// AF-12: this world as a save carries it - the save layer's own snapshot (the C-C10 fork's round trip) of the world, its manager and ITS generator,
        /// compact. Null for a world with no player seat (the seeded shadow a fresh process builds before any country is chosen): there is nothing of the
        /// ruling's world to carry, and the load then forks, saying so. ⚠ The generator is swapped exactly as <see cref="AdvanceTurn"/> swaps it, so the
        /// snapshot reads the shadow's streams and the real game's come back as the very objects they were (GA-1-3). A world holding levers it has not yet
        /// done (an except-world inside a turn) is refused: the save layer has no place for them. FORK-2: <paramref name="catchUpTo"/> - the real game's day
        /// into the turn - brings such a world there first (<see cref="CatchUp"/>), so it holds none; a world holding nothing is carried where it stands.
        /// </summary>
        public string Carry(int catchUpTo = -1)
        {
            if (!_sim.PlayerCountryId.HasValue || Faulted != null) { return null; }   // FORK-2 (M3): a failed world is carried in no save
            if (_scheduled.Count > 0 && catchUpTo >= 0) { CatchUp(catchUpTo); }
            if (Faulted != null) { return null; }
            if (_carried != null && _carriedOn == _sim.CurrentDate) { return _carried; }
            if (_scheduled.Count > 0) { throw new System.InvalidOperationException("a counterfactual holding levers not yet done is not carried - the no-policy world is handed none"); }

            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            bool swapped = false;
            try
            {
                SimulationRandom.Reinstate(_own);
                swapped = true;
                Persistence.SaveGame snapshot = Persistence.SaveGameService.CreateSaveGame(_sim, _world, _sim.PlayerCountryId.Value, null);
                Newtonsoft.Json.JsonSerializerSettings compact = Persistence.SaveGameService.BuildSettings();
                compact.Formatting = Newtonsoft.Json.Formatting.None;   // a world inside a save is read by the loader, not a person
                _carried = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, compact);
                _carriedOn = _sim.CurrentDate;
                return _carried;
            }
            finally
            {
                if (swapped) { _own = SimulationRandom.CaptureLive(); }
                SimulationRandom.Reinstate(real);
            }
        }

        /// <summary>AF-12: the world a save carried (<see cref="Carry"/>), restored as it was saved - its own generator, its days run, its player seat. Throws
        /// the save layer's <see cref="Persistence.SaveLoadException"/> on a text that is no save of this build's format; the caller decides what stands then.</summary>
        public static ShadowBaseline FromCarried(string carried) => new ShadowBaseline(Persistence.SaveGameService.Deserialize(carried));

        /// <summary>AF-12: <see cref="FromCarried"/>'s restore - the fork constructor's, from a snapshot already made. The host is built INACTIVE, as the
        /// fork made at a load builds it: the manager's `Awake` would build a default world, resetting the energy market's turn state the played world's
        /// load has just set.</summary>
        private ShadowBaseline(Persistence.SaveGame carried)
        {
            _host = new GameObject("ShadowBaseline (carried)") { hideFlags = HideFlags.HideAndDontSave };
            _host.SetActive(false);
            _sim = _host.AddComponent<SimulationManager>();
            _sim.IsCounterfactual = true;   // AF-7: a counterfactual world - the player's bank's head is the AI's keyed pick at its term's end, no hold (every constructor sets it; AF-12's carried world too)

            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();   // GA-1-3: the real game's stream OBJECTS, handed back as they were
            try
            {
                Persistence.SaveGameService.RestoreInto(_sim, carried);
                _world = _sim.World;
                _dayTickFor = _sim.PlayerCountryId;   // FORK-1 with AF-12 (the s871 review's H1): a carried no-policy world steps the player's day tick, as it did before the save
                _sim.RecomputePollingDayToday();   // FORK-2 (the review's M1): the save's day as it stands
                _daysRun = Mathf.Clamp(_sim.DaysIntoTurn, 0, SimulationManager.DaysPerTurn);
                _own = SimulationRandom.CaptureLive();
            }
            catch
            {
                Object.DestroyImmediate(_host);   // FORK-2 (the fix review's L3)
                throw;
            }
            finally
            {
                SimulationRandom.Reinstate(real);
            }

            foreach (Country c in _world.Countries) { _noDecisions[c.Id] = PolicyDecision.None(); }
        }

        /// <summary>AF-12: the player country this world seats - the save's, for a world forked from a game or carried by one; null for the seeded shadow.</summary>
        public CountryId? PlayerCountryId => _sim != null ? _sim.PlayerCountryId : null;

        /// <summary>FORK-2: this world's clock - its date and turn (a world lags the real game inside a turn until the boundary or a save brings it up).</summary>
        public System.DateTime CurrentDate => _sim.CurrentDate;
        public int CurrentTurn => _sim.CurrentTurn;
    }
}
