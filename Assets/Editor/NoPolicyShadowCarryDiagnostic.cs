using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-12 (Elias's ruling q12 of 2026-10-18: <i>"'without your policies' means the player's party governing with no changes from the start. After a load it
    /// is the same world, either carried in the save or re-run from the start; measure which is cheaper."</i>) - **the counterfactual's proof gate, and the
    /// measurement that chose how it crosses a load.** One game on the game's order (the turn's days, the boundary, the no-policy world's turn, the played
    /// world's market state set again), from Sweden's start with the player's party leading the government of record and a tax dial the player moves:
    ///
    /// <list type="number">
    /// <item><description><b>THE WORLD FROM THE START SEATS THE PLAYER.</b> Forked as the game now forks it at the start, it holds the player's country, party and
    /// government - where the seeded world the game opened on before AF-12 holds no player, and the AI governed the player's country.</description></item>
    /// <item><description><b>NO CHANGES.</b> The player moves the dial; the world from the start keeps the start's rate.</description></item>
    /// <item><description>⚠ <b>THE SAME WORLD AFTER A LOAD.</b> A save cut inside a turn carries the world (`ShadowBaseline.Carry`), the text survives the save's
    /// round trip, and the world it restores runs on byte-identical to the one never saved - every country's state, the policy rate, the player's tax lines.
    /// The fork of the loaded game (every load before AF-12) is shown beside it, and the re-run from the start is checked for the same identity.</description></item>
    /// <item><description>⚠ <b>THE CHEAPER, MEASURED.</b> Carried: the snapshot a turn's saves write, the save's and the load's extra serialization, the restore.
    /// Re-run, at its FLOOR: nothing written, the start restored (standing in for rebuilding it) and the turns since run again. The time the player waits is
    /// the measure; the bytes are printed beside it. The check FAILS while the re-run would be the cheaper at the cut - the built choice would then be wrong.</description></item>
    /// <item><description>⚠ <b>THE OPPOSITION PLAYER</b> (the review's H1, with FORK-1's day tick): a player whose party does not lead the government, acting on
    /// nothing - the world from the start EQUALS the played world after the government's first budget is decided, and a fork without the tick does not.</description></item>
    /// </list>
    /// <para>The played world steps the game's day (the day, then the player's day tick) and the world from the start is forked with the tick, as
    /// `GameController` forks it since FORK-1. The review's L5: carrying the world leaves the played world's state and draw counts as they were (cases 3, 5).</para>
    ///
    /// <para>⚠ <b>What it does not claim, stated.</b> The harness runs no player day tick and holds no election (the controller holds the player's election; no
    /// counterfactual world runs either - the audit's D-45), so the claim is the counterfactual's identity across a load and its cost, not an untouched game's
    /// equality with the played one. Times are one measurement each, on the machine the run is on.</para>
    /// </summary>
    public static class NoPolicyShadowCarryDiagnostic
    {
        private const int Seed = 777;
        private static readonly CountryId Player = CountryId.Sweden;
        /// <summary>The party the player leads - the government of record's at Sweden's start (PlayerRoleDiagnostic holds it).</summary>
        private const string Party = "M";
        /// <summary>The save is cut on this day of this turn - one term in, inside a turn, as a player's save is.</summary>
        private const int CutTurn = 4;
        private const int CutDay = 100;
        private const int EndTurns = CutTurn + 2;
        /// <summary>The dial: the player's income tax this many points over the start's, from the first boundary.</summary>
        private const float TaxStep = 3f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            sb.Append("=== AF-12 (q12): \"without your policies\" from the start, and the same world after a load ===\n");
            sb.Append(F("    {0} from its start, {1} leading; the dial: income tax +{2} from turn 1; the save cut on day {3} of turn {4}; the run to turn {5}\n",
                Player, Party, TaxStep, CutDay, CutTurn, EndTurns));

            var shadows = new List<ShadowBaseline>();
            var go = new GameObject("AF-12 REAL");
            using (SimulationManager.EpochScope())
            {
                try
                {
                    WorldClock.ApplyStart(Player);
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    World world = WorldFactory.CreateDefault();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = Player;
                    Country me = world.GetCountry(Player);
                    me.PlayerPartyAbbrev = Party;
                    me.PartyApprovalRating = me.State.ApprovalRating;
                    me.Government = GovernmentRecord.AtStart(me, WorldClock.StartDate(Player), world);
                    if (!sim.PlayerGoverns(me)) { failures.Add(F("{0} does not lead {1}'s government at the start ({2}) - the item's world is a governing player's", Party, Player, me.Government?.PmParty ?? "none")); }

                    // ---- 1: the world from the start seats the player ----
                    var clock = Stopwatch.StartNew();
                    var shadow = new ShadowBaseline(sim, world, Player, insideTurn: true, dayTick: true);   // as GameController.ForkTheShadowAtTheStart forks it (FORK-1: the player's day tick)
                    double forkMs = clock.Elapsed.TotalMilliseconds;
                    shadows.Add(shadow);
                    var seeded = new ShadowBaseline(Seed);   // the world a new game opened on before AF-12: no player, the AI governing every country
                    shadows.Add(seeded);
                    Country shadowMe = shadow.CountryFor(Player);
                    bool seats = shadow.PlayerCountryId == Player && shadowMe?.PlayerPartyAbbrev == Party && shadowMe.Government?.PmParty == me.Government?.PmParty;
                    if (!seats) { failures.Add(F("the world from the start does not seat the player: country {0}, party {1}, prime minister's party {2}", shadow.PlayerCountryId?.ToString() ?? "none", shadowMe?.PlayerPartyAbbrev ?? "none", shadowMe?.Government?.PmParty ?? "none")); }
                    if (seeded.PlayerCountryId.HasValue) { failures.Add("the seeded world seats a player - the contrast is not the pre-AF-12 world"); }
                    sb.Append(F("    1. the seat         {0} - the world from the start seats {1}, {2} leading {3}; the fork took {4:0.0} ms (a new game's selection pays it once)\n",
                        seats ? "OK" : "⚠ NOT SEATED", shadow.PlayerCountryId?.ToString() ?? "none", shadowMe?.PlayerPartyAbbrev ?? "none", shadowMe?.Government?.PmParty ?? "none", forkMs));

                    clock.Restart();
                    string startCarried = shadow.Carry();
                    double carryStartMs = clock.Elapsed.TotalMilliseconds;
                    if (string.IsNullOrEmpty(startCarried)) { failures.Add("the world from the start carried nothing"); Finish(sb, failures); return; }
                    float taxStart = IncomeRate(shadowMe), taxTarget = IncomeRate(me) + TaxStep;
                    if (float.IsNaN(taxStart)) { failures.Add(F("{0}'s income tax is not implemented - the dial was not exercised", Player)); }

                    // ---- the game ----
                    ShadowBaseline loaded = null, rerun = null, forkAtLoad = null;
                    var m = new Measured { StartBytes = Bytes(startCarried), CarryStartMs = carryStartMs };
                    for (int t = 0; t < EndTurns; t++)
                    {
                        for (int d = 1; d <= SimulationManager.DaysPerTurn; d++)
                        {
                            sim.AdvanceDay();
                            sim.AdvanceCountryDayTick(Player);   // the played world's day: the day, then the player's day tick (GameController.Update)
                            if (t != CutTurn || d != CutDay) { continue; }

                            // ---- the cut: the save, and its load ----
                            clock.Restart();
                            string playedBefore = Fingerprint(world) + Draws(SimulationRandom.CaptureDrawCounts());
                            string carried = shadow.Carry();
                            m.CarryMs = clock.Elapsed.TotalMilliseconds;
                            clock.Restart();
                            string again = shadow.Carry();
                            m.CarryCachedMs = clock.Elapsed.TotalMilliseconds;
                            if (!ReferenceEquals(carried, again)) { failures.Add("a second save on the same day serialized the world again - the turn's snapshot is not kept"); }
                            string playedAfter = Fingerprint(world) + Draws(SimulationRandom.CaptureDrawCounts());
                            m.PlayedUntouched = playedBefore == playedAfter;
                            if (!m.PlayedUntouched) { failures.Add("carrying the no-policy world CHANGED the played world (its state or its draw counts)" + FirstDifference(playedBefore, playedAfter)); }

                            SaveGame save = SaveGameService.CreateSaveGame(sim, world, Player, null);
                            clock.Restart();
                            string without = SaveGameService.Serialize(save);
                            m.SerializeWithoutMs = clock.Elapsed.TotalMilliseconds;
                            save.NoPolicyShadow = carried;
                            clock.Restart();
                            string with = SaveGameService.Serialize(save);
                            m.SerializeWithMs = clock.Elapsed.TotalMilliseconds;
                            m.SaveBytesWithout = Bytes(without);
                            m.SaveBytesWith = Bytes(with);

                            clock.Restart();
                            SaveGameService.Deserialize(without);
                            m.DeserializeWithoutMs = clock.Elapsed.TotalMilliseconds;
                            clock.Restart();
                            SaveGame back = SaveGameService.Deserialize(with);
                            m.DeserializeWithMs = clock.Elapsed.TotalMilliseconds;
                            if (!string.Equals(back.NoPolicyShadow, carried, StringComparison.Ordinal)) { failures.Add("the carried world's text did not survive the save's round trip"); }

                            clock.Restart();
                            loaded = ShadowBaseline.FromCarried(back.NoPolicyShadow);
                            m.RestoreMs = clock.Elapsed.TotalMilliseconds;
                            shadows.Add(loaded);
                            string atCut = Fingerprint(shadow.World), loadedAtCut = Fingerprint(loaded.World);
                            if (atCut != loadedAtCut) { failures.Add("the carried world, restored, is not the world that was saved" + FirstDifference(atCut, loadedAtCut)); }
                            if (loaded.PlayerCountryId != Player) { failures.Add("the carried world restores without the player's seat"); }

                            // the re-run from the start, at its floor: nothing in the save, the start restored and the turns since run again
                            clock.Restart();
                            rerun = ShadowBaseline.FromCarried(startCarried);
                            m.RerunStartMs = clock.Elapsed.TotalMilliseconds;
                            shadows.Add(rerun);
                            clock.Restart();
                            for (int k = 0; k < CutTurn; k++) { rerun.AdvanceTurn(); }
                            m.RerunTurnsMs = clock.Elapsed.TotalMilliseconds;

                            forkAtLoad = new ShadowBaseline(sim, world, Player, insideTurn: true);   // every load before AF-12
                            shadows.Add(forkAtLoad);

                            // §557: a save loaded into a MANAGER is not loaded into the GAME - both saves adopted by the controller's own load path
                            Adopt(SaveGameService.Deserialize(with), out ShadowBaseline adoptedCarried, out string whyCarried);
                            Adopt(SaveGameService.Deserialize(without), out ShadowBaseline adoptedForked, out string whyForked);
                            if (adoptedCarried != null) { shadows.Add(adoptedCarried); }
                            if (adoptedForked != null) { shadows.Add(adoptedForked); }
                            bool gameCarries = adoptedCarried != null && adoptedCarried.PlayerCountryId == Player && Fingerprint(adoptedCarried.World) == atCut;
                            bool gameForks = adoptedForked != null && adoptedForked.PlayerCountryId == Player;
                            if (!gameCarries) { failures.Add("the GAME's load (GameController.RestoreFromSave) does not restore the carried world - " + (whyCarried ?? "its shadow is not the world saved")); }
                            if (!gameForks) { failures.Add("the GAME's load of a save carrying no world does not fork the loaded game with the player's seat - " + (whyForked ?? "no seat")); }
                            m.Game = F("the game's own load restores the carried world: {0}; a save carrying none forks the loaded game{1}: {2}",
                                gameCarries ? "yes" : "⚠ NO", adoptedForked != null && Fingerprint(adoptedForked.World) == Fingerprint(forkAtLoad.World) ? " (the pre-AF-12 world exactly)" : string.Empty, gameForks ? "yes" : "⚠ NO");
                            EnergyMarket.BeginTurn(world);   // the game's own call after another world ran a boundary: the played world's market state stands
                        }

                        var decisions = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                        if (t >= 1 && !float.IsNaN(taxTarget)) { decisions[Player] = new PolicyDecision(); decisions[Player].TaxRateOverrides[TaxType.IncomeTax] = taxTarget; }
                        sim.AdvanceTurn(decisions);
                        shadow.AdvanceTurn();
                        loaded?.AdvanceTurn();   // beside the world never saved, so the two meet the same process state
                        rerun?.AdvanceTurn();
                        forkAtLoad?.AdvanceTurn();
                        seeded.AdvanceTurn();
                        EnergyMarket.BeginTurn(world);   // GameController's order: the played world's market state set again after the counterfactuals' turns
                    }

                    if (loaded == null) { failures.Add("the run never reached the cut - nothing was saved or loaded"); Finish(sb, failures); return; }

                    // ---- 2: no changes ----
                    float taxShadow = IncomeRate(shadow.CountryFor(Player)), taxReal = IncomeRate(me);
                    bool realMoved = !float.IsNaN(taxReal) && Math.Abs(taxReal - taxStart) > 1e-4f;
                    bool shadowStill = !float.IsNaN(taxShadow) && Math.Abs(taxShadow - taxStart) <= 1e-4f;
                    if (!shadowStill) { failures.Add(F("the world from the start moved the player's income tax {0} -> {1} - it follows the player", taxStart, taxShadow)); }
                    sb.Append(!realMoved
                        ? F("    2. no changes       (the dial did not move the played world's rate, {0} -> {1}: this run proves nothing here - untested, not passed)\n", taxStart, taxReal)
                        : F("    2. no changes       {0} - the player moved the income tax {1:0.##} -> {2:0.##}; the world from the start holds {3:0.##}\n", shadowStill ? "OK" : "⚠ FOLLOWED", taxStart, taxReal, taxShadow));

                    // ---- 3: the same world after a load ----
                    string continuous = Fingerprint(shadow.World);
                    string afterLoad = Fingerprint(loaded.World), reRun = Fingerprint(rerun.World), forked = Fingerprint(forkAtLoad.World), ai = Fingerprint(seeded.World);
                    bool carriedSame = continuous == afterLoad, rerunSame = continuous == reRun;
                    int fields = Fields(continuous);
                    if (!carriedSame) { failures.Add(F("the carried world, after the load, is NOT the world never saved at turn {0}", EndTurns) + FirstDifference(continuous, afterLoad)); }
                    sb.Append(F("    3. after a load     carried: {0}; re-run from the start: {1}; over the {2} fields read at turn {3}\n",
                        carriedSame ? "THE SAME WORLD" : "⚠ DIFFERS", rerunSame ? "the same world" : "differs in " + Differing(continuous, reRun) + FirstDifference(continuous, reRun), fields, EndTurns));
                    sb.Append(F("       the fork of the loaded game (every load before AF-12) differs in {0} of them{1}; the seeded world the AI governed (a new game before AF-12) in {2}\n",
                        Differing(continuous, forked), forked == continuous ? " - THIS RUN DID NOT SEPARATE THEM (the contrast is untested)" : string.Empty, Differing(continuous, ai)));
                    sb.Append("       at the cut, ").Append(m.Game).Append('\n');
                    sb.Append(m.PlayedUntouched ? "       the played world is untouched by the carry - its state and every draw count the same before and after\n" : "       ⚠ the carry changed the played world - see the faults\n");

                    // ---- 4: the cheaper, measured ----
                    double carriedSaveMs = m.CarryMs + (m.SerializeWithMs - m.SerializeWithoutMs);
                    double carriedLoadMs = (m.DeserializeWithMs - m.DeserializeWithoutMs) + m.RestoreMs;
                    double carriedMs = carriedSaveMs + carriedLoadMs;
                    double rerunMs = m.RerunStartMs + m.RerunTurnsMs;
                    double perTurnMs = m.RerunTurnsMs / CutTurn;
                    long carriedBytes = m.SaveBytesWith - m.SaveBytesWithout;
                    sb.Append(F("    4. the cost at the cut (turn {0}, day {1}); the time the player waits is the measure, the bytes beside it\n", CutTurn, CutDay));
                    sb.Append(F("       CARRIED   {0,9:0.0} ms = the snapshot {1:0.0} (once a turn: a second save that day {2:0.000}) + the save's extra serialization {3:0.0} + the load's extra parse {4:0.0} + the restore {5:0.0}; {6} bytes more in every save ({7} -> {8}; the start's world {9}, taken in {10:0.0} ms)\n",
                        carriedMs, m.CarryMs, m.CarryCachedMs, m.SerializeWithMs - m.SerializeWithoutMs, m.DeserializeWithMs - m.DeserializeWithoutMs, m.RestoreMs,
                        carriedBytes, m.SaveBytesWithout, m.SaveBytesWith, m.StartBytes, m.CarryStartMs));
                    sb.Append(F("       RE-RUN    {0,9:0.0} ms = the start restored {1:0.0} + {2} turns run again {3:0.0} ({4:0.0} a turn); 0 bytes at its floor (a recipe the save already holds - not built)\n",
                        rerunMs, m.RerunStartMs, CutTurn, m.RerunTurnsMs, perTurnMs));
                    double crossover = perTurnMs > 0 ? (carriedMs - m.RerunStartMs) / perTurnMs : double.PositiveInfinity;
                    bool carriedCheaper = carriedMs < rerunMs;
                    if (!carriedCheaper) { failures.Add(F("the RE-RUN is the cheaper at the cut ({0:0.0} ms against the carried {1:0.0}) - the built choice (carried) is not the measured one", rerunMs, carriedMs)); }
                    sb.Append(F("       VERDICT   {0}; the re-run costs more than carrying from {1:0.00} turns into a game{2}\n",
                        carriedCheaper ? F("CARRIED is the cheaper by {0:0.0} ms - built", rerunMs - carriedMs) : "⚠ RE-RUN is the cheaper - the build is the wrong one",
                        Math.Max(0.0, crossover), rerunSame ? string.Empty : "; and the re-run is not the same world"));
                }
                catch (Exception e)
                {
                    failures.Add("the run threw: " + e.GetType().Name + ": " + e.Message);
                }
                finally
                {
                    foreach (ShadowBaseline s in shadows) { s.Dispose(); }
                    UnityEngine.Object.DestroyImmediate(go);
                    EnergyMarket.ResetTurnState();
                }
            }

            OppositionCase(sb, failures);
            Finish(sb, failures);
        }

        // ------------------------------------------------------------------ 5: the opposition player (the review's H1)

        private const string OppositionParty = "S";
        private static readonly FieldInfo ForkSim = typeof(ShadowBaseline).GetField("_sim", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkOwn = typeof(ShadowBaseline).GetField("_own", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkDays = typeof(ShadowBaseline).GetField("_daysRun", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkTick = typeof(ShadowBaseline).GetField("_dayTickFor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo SeatTheRecord = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// ⚠ THE REVIEW'S H1: a player whose party does NOT lead the government acts on nothing, and the AI government's budget reaches the book only through
        /// the player's day tick (the arrival window tables it as the government's bill, the chamber votes it) - so a no-policy world that seats the player
        /// and steps no tick never gets a budget. Sweden from its start with S in opposition: the world forked at the start as the game forks it (with the
        /// tick) must EQUAL the played world - every country's state, the player's government, divisions, pending budget, spending and tax lines, and each
        /// world's own draw counts - on the day after the government's first budget is decided, before the polling day; a fork WITHOUT the tick (the
        /// control, every shadow before J8) must differ there. The worlds are stepped day by day to that date - the shadow's own loop, read by reflection
        /// (it steps a whole turn at the boundary in play). And the review's L5 again: carrying the world mid-turn leaves the played world as it was.
        /// </summary>
        private static void OppositionCase(StringBuilder sb, List<string> failures)
        {
            if (ForkSim == null || ForkOwn == null || ForkDays == null || ForkTick == null || SeatTheRecord == null) { failures.Add("5: the shadow's or the manager's private stepping could not be reached by name - VERIFIED NOTHING"); return; }
            var go = new GameObject("AF-12 OPPOSITION");
            ShadowBaseline ticking = null, control = null;
            using (SimulationManager.EpochScope())
            {
                try
                {
                    WorldClock.ApplyStart(Player);
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    World world = WorldFactory.CreateDefault();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = Player;
                    Country me = world.GetCountry(Player);
                    me.PlayerPartyAbbrev = OppositionParty;
                    me.PartyApprovalRating = me.State.ApprovalRating;
                    me.Government = GovernmentRecord.AtStart(me, WorldClock.StartDate(Player), world);
                    SeatTheRecord.Invoke(sim, null);   // as the start's own day holds it (ForkDayTickDiagnostic's Sweden)
                    if (sim.PlayerGoverns(me)) { failures.Add(F("5: {0} leads {1}'s government at the start ({2}) - the case is an opposition player's", OppositionParty, Player, me.Government?.PmParty ?? "none")); return; }

                    ticking = new ShadowBaseline(sim, world, Player, insideTurn: true, dayTick: true);   // GameController.ForkTheShadowAtTheStart's
                    control = new ShadowBaseline(sim, world, Player, insideTurn: true);                  // no tick: every shadow before J8

                    // the played world, day by day in the game's order, to the day after the government's first budget is decided
                    bool tabled = false, decided = false;
                    int days = 0;
                    DateTime polling = DateTime.MaxValue;
                    while (!decided && days < SimulationManager.DaysPerTurn - 1)
                    {
                        sim.AdvanceDay();
                        sim.AdvanceCountryDayTick(Player);
                        days++;
                        if (sim.PollingDayToday) { polling = sim.CurrentDate; break; }
                        BudgetBill pending = sim.GetPendingBudgetBill(Player);
                        if (pending != null) { tabled = true; }
                        else if (tabled) { decided = true; }
                    }
                    if (!decided)
                    {
                        failures.Add(F("5: no government budget was {0} in {1} days{2} - the case has nothing to compare", tabled ? "decided" : "tabled", days, polling != DateTime.MaxValue ? F(" (the polling day {0:yyyy-MM-dd} came first)", polling) : string.Empty));
                        return;
                    }
                    sim.AdvanceDay();   // the day after: the decided budget stands in the book
                    sim.AdvanceCountryDayTick(Player);
                    days++;
                    if (sim.PollingDayToday) { failures.Add("5: the day after the budget is the polling day - compare earlier"); return; }
                    DivisionRecord vote = me.Divisions.Entries.Count > 0 ? me.Divisions.Entries[me.Divisions.Entries.Count - 1] : null;

                    StepDays(ticking, days);
                    StepDays(control, days);
                    string played = Book(sim, world, SimulationRandom.CaptureDrawCounts());
                    string withTick = Book((SimulationManager)ForkSim.GetValue(ticking), ticking.World, OwnDraws(ticking));
                    string withoutTick = Book((SimulationManager)ForkSim.GetValue(control), control.World, OwnDraws(control));
                    bool equal = played == withTick, teeth = played != withoutTick;
                    if (!equal) { failures.Add(F("5: the no-policy world of an opposition player who acts on nothing is NOT the played world on {0:yyyy-MM-dd}", sim.CurrentDate) + FirstDifference(played, withTick)); }
                    if (!teeth) { failures.Add(F("5: the control (no day tick) EQUALS the played world on {0:yyyy-MM-dd} - the case has no teeth", sim.CurrentDate)); }

                    // L5: the carry mid-turn reads the no-policy world and leaves the played one as it was
                    string before = played;
                    string carried = ticking.Carry();
                    string after = Book(sim, world, SimulationRandom.CaptureDrawCounts());
                    bool untouched = before == after && !string.IsNullOrEmpty(carried);
                    if (!untouched) { failures.Add("5: carrying the no-policy world changed the played world, or carried nothing" + FirstDifference(before, after)); }

                    sb.Append(F("    5. opposition       {0} - {1} in opposition to {2}, acting on nothing: the government's budget tabled and decided ({3}, division {4} on {5:yyyy-MM-dd}); on {6:yyyy-MM-dd}, {7} days in and before the polling day, the world from the start {8} the played world over {9} fields; the control without the day tick {10}{11}; the carry mid-turn leaves the played world {12}\n",
                        equal && teeth && untouched ? "OK" : "⚠ FAILED", OppositionParty, me.Government?.PmParty ?? "none",
                        vote == null ? "no division recorded" : vote.Passed ? "PASSED" : "FELL", vote?.Number.ToString(CultureInfo.InvariantCulture) ?? "-", vote?.Date ?? DateTime.MinValue,
                        sim.CurrentDate, days, equal ? "EQUALS" : "⚠ DIFFERS FROM", Fields(played),
                        teeth ? "differs in " + Differing(played, withoutTick) : "⚠ EQUALS IT", teeth ? FirstDifference(played, withoutTick) : string.Empty, untouched ? "untouched" : "⚠ CHANGED"));
                }
                catch (Exception e)
                {
                    failures.Add("5: the opposition case threw: " + e.GetType().Name + ": " + e.Message);
                }
                finally
                {
                    ticking?.Dispose();
                    control?.Dispose();
                    UnityEngine.Object.DestroyImmediate(go);
                    EnergyMarket.ResetTurnState();
                }
            }
        }

        /// <summary>A shadow's own day loop for <paramref name="days"/> days - `ShadowBaseline.AdvanceTurn`'s, the day then its tick, under its own
        /// generator - so it can be read inside a turn; its days run are moved on, so its next boundary runs only the rest.</summary>
        private static void StepDays(ShadowBaseline shadow, int days)
        {
            var sim = (SimulationManager)ForkSim.GetValue(shadow);
            var own = (SimulationRandom.LiveState)ForkOwn.GetValue(shadow);
            var tick = (CountryId?)ForkTick.GetValue(shadow);
            int run = (int)ForkDays.GetValue(shadow);
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            try
            {
                SimulationRandom.Reinstate(own);
                for (int i = 0; i < days; i++)
                {
                    sim.AdvanceDay();
                    if (tick.HasValue) { sim.AdvanceCountryDayTick(tick.Value); }
                    run++;
                }
            }
            finally
            {
                ForkOwn.SetValue(shadow, SimulationRandom.CaptureLive());
                ForkDays.SetValue(shadow, run);
                SimulationRandom.Reinstate(real);
            }
        }

        /// <summary>A shadow's own draw counts (its generator swapped in to be read, the process's put back).</summary>
        private static Dictionary<SimulationRandom.Stream, int> OwnDraws(ShadowBaseline shadow)
        {
            var own = (SimulationRandom.LiveState)ForkOwn.GetValue(shadow);
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            try { SimulationRandom.Reinstate(own); return SimulationRandom.CaptureDrawCounts(); }
            finally { SimulationRandom.Reinstate(real); }
        }

        private static string Draws(Dictionary<SimulationRandom.Stream, int> draws)
        {
            var keys = new List<SimulationRandom.Stream>();
            foreach (KeyValuePair<SimulationRandom.Stream, int> kv in draws) { if (kv.Value > 0) { keys.Add(kv.Key); } }
            keys.Sort((a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));
            var sb = new StringBuilder("draws");
            foreach (SimulationRandom.Stream k in keys) { sb.Append('|').Append(k).Append('=').Append(draws[k]); }
            return sb.Append('\n').ToString();
        }

        /// <summary>The widened fingerprint (H1): <see cref="Fingerprint"/>, then the player's government, divisions, pending budget process and bill, its
        /// spending lines, the clock, and the world's own draw counts.</summary>
        private static string Book(SimulationManager sim, World world, Dictionary<SimulationRandom.Stream, int> draws)
        {
            var sb = new StringBuilder(Fingerprint(world));
            Country p = world.GetCountry(Player);
            sb.Append("clock|").Append(sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("|turn=").Append(sim.CurrentTurn).Append('\n');
            GovernmentRecord g = p.Government;
            sb.Append("government|").Append(g == null ? "none" : g.PmParty + "|" + string.Join(",", g.Cabinet) + "|" + string.Join(",", g.Support) + "|" + g.Outcome).Append('\n');
            DivisionRecord last = p.Divisions.Entries.Count > 0 ? p.Divisions.Entries[p.Divisions.Entries.Count - 1] : null;
            sb.Append("divisions|").Append(p.Divisions.Entries.Count).Append('|').Append(last == null ? "none" : last.Number + "|" + last.Title + "|" + last.Passed).Append('\n');
            BudgetBill bill = sim.GetPendingBudgetBill(Player);
            sb.Append("pending|process=").Append(sim.GetPendingBudgetProcess(Player)).Append("|bill=").Append(bill == null ? "none" : bill.DaysRemaining + (bill.GovernmentBill ? " gov" : string.Empty)).Append('\n');
            sb.Append("spending");
            foreach (SpendingLine line in p.SpendingLines) { sb.Append('|').Append(line.Category).Append('=').Append(line.Amount.ToString("R", CultureInfo.InvariantCulture)).Append(line.Pinned ? "p" : string.Empty); }
            sb.Append('\n');
            return sb.Append(Draws(draws)).ToString();
        }

        private sealed class Measured
        {
            public long StartBytes, SaveBytesWithout, SaveBytesWith;
            public double CarryStartMs, CarryMs, CarryCachedMs, SerializeWithoutMs, SerializeWithMs, DeserializeWithoutMs, DeserializeWithMs, RestoreMs, RerunStartMs, RerunTurnsMs;
            public string Game = "the game's own load was not reached";
            public bool PlayedUntouched;
        }

        /// <summary>§557: a save adopted by the controller's own load path - `PlayProtocolCheck`'s idiom: a controller in edit mode, its manager handed to it.
        /// The no-policy world the load built is handed out (the caller disposes it), the ledger disposed, the controller destroyed, and the process's
        /// generator put back as the very objects it was (the load restores the save's streams as new ones).</summary>
        private static void Adopt(SaveGame save, out ShadowBaseline shadow, out string error)
        {
            shadow = null;
            error = null;
            var go = new GameObject("AF-12 controller");
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            try
            {
                SimulationManager adoptSim = go.AddComponent<SimulationManager>();
                var controller = go.AddComponent<PoliSim.UI.GameController>();
                const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(PoliSim.UI.GameController);
                FieldInfo simField = type.GetField("_simulationManager", Private), shadowField = type.GetField("_shadowBaseline", Private), ledgerField = type.GetField("_impactLedger", Private);
                MethodInfo restore = type.GetMethod("RestoreFromSave", Private);
                if (simField == null || shadowField == null || ledgerField == null || restore == null) { error = "the controller's load path could not be reached by name - VERIFIED NOTHING"; return; }
                simField.SetValue(controller, adoptSim);
                try { restore.Invoke(controller, new object[] { save }); }
                catch (TargetInvocationException e) { error = "the controller cannot adopt the save - " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; }
                (ledgerField.GetValue(controller) as PolicyImpactLedger)?.Dispose();
                shadow = shadowField.GetValue(controller) as ShadowBaseline;
                if (shadow == null && error == null) { error = "the controller holds no no-policy world after the load"; }
            }
            finally
            {
                SimulationRandom.Reinstate(live);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static long Bytes(string s) => Encoding.UTF8.GetByteCount(s);

        /// <summary>The player's income tax rate, NaN where the line is not implemented.</summary>
        private static float IncomeRate(Country c)
        {
            if (c?.TaxLines == null) { return float.NaN; }
            foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax && line.IsImplemented) { return line.Rate; } }
            return float.NaN;
        }

        /// <summary>Every country's state (each float, round-trip), its policy rate, and every tax line's rate - one field per '|'.</summary>
        private static string Fingerprint(World world)
        {
            var sb = new StringBuilder();
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=').Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }
                foreach (TaxLine line in c.TaxLines) { sb.Append("tax.").Append(line.Type).Append('=').Append(line.Rate.ToString("R", CultureInfo.InvariantCulture)).Append('|'); }
                sb.Append("rate=").Append((c.CurrencyZone?.InterestRate ?? 0f).ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            }
            return sb.ToString();
        }

        private static int Fields(string fingerprint)
        {
            int n = 0;
            foreach (string line in fingerprint.Split('\n')) { if (line.Length > 0) { n += line.Split('|').Length; } }
            return n;
        }

        private static int Differing(string a, string b)
        {
            string[] x = a.Split('\n'), y = b.Split('\n');
            int n = Math.Abs(x.Length - y.Length);
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                string[] p = x[i].Split('|'), q = y[i].Split('|');
                n += Math.Abs(p.Length - q.Length);
                for (int j = 0; j < Math.Min(p.Length, q.Length); j++) { if (p[j] != q[j]) { n++; } }
            }
            return n;
        }

        private static string FirstDifference(string a, string b)
        {
            string[] x = a.Split('\n'), y = b.Split('\n');
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                if (x[i] == y[i]) { continue; }
                string[] p = x[i].Split('|'), q = y[i].Split('|');
                for (int j = 0; j < Math.Min(p.Length, q.Length); j++) { if (p[j] != q[j]) { return $" - first at line {i}: {p[j]} against {q[j]}"; } }
                return $" - first at line {i}";
            }
            return a.Length == b.Length ? string.Empty : " - the lengths differ";
        }

        private static void Finish(StringBuilder sb, List<string> failures)
        {
            foreach (string f in failures) { sb.Append("    ⚠ FAULT  ").Append(f).Append('\n'); }
            sb.Append(F("\n=== NoPolicyShadowCarryDiagnostic: {0} ===\n", failures.Count == 0 ? "ALL ASSERTIONS PASS" : failures.Count + " FAULT(S)"));
            if (failures.Count == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
