using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// FORK-2 (D-45's remainder and the s871 review's M1/M2, filed `COMPLETED.md` §871; the fix review of 2026-10-20) - **A FORK HOLDS THE PLAYER'S ELECTION,
    /// GIVES THE PLAYER'S ANSWERS, NEVER STALLS ON A PLAYER WHO WILL NOT ANSWER THERE, AND A LOAD KEEPS EVERY LINE'S SPAN.** The done-when: a fork with no policy
    /// difference equals the played world across a polling day and an answered meeting; a load keeps every line's span. Every game runs on the controller's
    /// order (`GameController.Update`: the day, the player's day tick; at the boundary the ledger's worlds, the played world, the no-policy world, the played
    /// world's market state; then the player's election on its polling day - `SimulationManager.HoldPlayerElection`), from Sweden, the player answering what the
    /// clock waits for the day it is asked (a meeting with its first option, a budget bill at the standing figures where the process is open, the Speaker's
    /// question with a pass, an offer with a decline), all through the manager's verbs.
    ///
    /// <list type="number">
    /// <item><description><b>ACROSS THE POLLING DAY AND THE MEETINGS</b> (the player's party leading the government of record). A lever's fork with no act (the
    /// Trade family's world, forked after the first day as `PolicyImpactLedger.BeforeLever` forks it) EQUALS the played world at the first boundary after the
    /// polling day, and its line is zero. Two controls must DIFFER: the fork holding no election (`ShadowBaseline.HoldsPlayerElection` false), and the
    /// MEETINGS answered around the ledger (the bills still through it) - so a broken meeting replay fails here on its own.</description></item>
    /// <item><description><b>THE SPEAKER'S QUESTION</b> (the player's party in opposition, asked first after the count). The played world passes (`PassFormation`);
    /// the Trade fork EQUALS it at the boundary; the control with the pass given around the ledger differs, and does not stall: the next day it answers by the AI's rule and forms a government. The
    /// NO-POLICY world - handed no answer, the same world as the played one up to the polling day - answers by the AI's rule and FORMS A GOVERNMENT, no
    /// caretaker and no round open at the read (the fix review's H2).</description></item>
    /// <item><description><b>THE POLLING DAY ON A BOUNDARY</b> (the world opened a year before the polling day, so the first boundary is the polling day): the
    /// Monetary dial's fork, made by the ledger at that boundary before the played world's election, holds the election after it (the fix review's M1) and
    /// EQUALS the played world there.</description></item>
    /// <item><description><b>A LOAD KEEPS EVERY LINE'S SPAN.</b> Two levers through their verbs (the central bank's head, a partner's tariff pinned) fork two
    /// families; a save cut inside the next turn carries both worlds (`PolicyImpactLedger.Carry`) beside the no-policy world, and the GAME's own load
    /// (`GameController.RestoreFromSave`) restores them. The loaded game reads the SAME lines on every graphed stat as a third game, identical and never saved
    /// nor carried (so a catch-up that differed from the boundary's days would show), and so does the game that was saved; a save carrying no family's world
    /// loads a ledger with no line.</description></item>
    /// <item><description><b>A WAIT THE PLAYED WORLD NEVER HAD</b> (case 6): the day after the count the family world's Speaker is made to ask the player's
    /// party where the played world's is not - with no answer to replay, the family world answers by the AI's rule the next day and forms a government.
    /// <b>THE DOCKET'S IDENTITY</b> (case 7): a US player-president signs a bill; the family world holds one division more before it, so its passage's number
    /// is one higher, and the signature still lands there. <b>A REPLAY THAT MISSES</b> (case 8) is said in one line and answers nothing; <b>A FORK THAT
    /// FAILS</b> (case 9) is set aside as unavailable, the played world untouched beside a twin.</description></item>
    /// <item><description><b>THE COST, MEASURED</b> (the fix review's M5): every one of the ledger's families forked, a save cut inside a turn - the catch-up's
    /// time, the carry's, and the save's bytes with every family's world against without. Reported, for Elias's ruling.</description></item>
    /// </list>
    /// <para>⚠ <b>What it does not claim.</b> The no-policy world is compared with the played world only where nothing was answered (case 2, up to the polling
    /// day); the bills' families are not claimed (an answer belongs to no family, DECLARED and asked). Times and bytes are one measurement each.</para>
    /// </summary>
    public static class ForkAnswersDiagnostic
    {
        private const int Seed = 777;
        private static readonly CountryId Player = CountryId.Sweden;
        /// <summary>The government of record's leading party at Sweden's start (as `NoPolicyShadowCarryDiagnostic` seats it), and the largest opposition party.</summary>
        private const string Governing = "M", Opposition = "S";
        /// <summary>Case 3's world opens a year (one turn) before Sweden's polling day of record, so the first boundary IS the polling day.</summary>
        private static readonly DateTime BoundaryPolls = new DateTime(2026, 9, 13);
        /// <summary>The longest a run may take to reach its read, in days (three turns) - past it the case did not reach its polling day.</summary>
        private const int MaxDays = 3 * SimulationManager.DaysPerTurn;
        /// <summary>Case 4: the levers' days in the first turn, and the save's cut in the second.</summary>
        private const int HeadDay = 30, TariffDay = 60, CutDay = 100;
        private const float Dial = 0.25f;
        private static readonly string[] Stats = { "GDP", "Inflation", "Unemployment", "ApprovalRating", "Budget", "GovernmentDebt" };

        private static readonly FieldInfo ExceptField = typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FamiliesField = typeof(PolicyImpactLedger).GetField("Families", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkSimField = typeof(ShadowBaseline).GetField("_sim", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkOwnField = typeof(ShadowBaseline).GetField("_own", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly ConstructorInfo LeverCtor = typeof(PlayerLever).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(PlayerLeverKind), typeof(CountryId), typeof(Func<World, Country, bool, bool>) }, null);

        /// <summary>One game: the played world, its generator between steps, the no-policy world forked at the start (as the game forks it) and the ledger.</summary>
        private sealed class Game
        {
            public SimulationManager Sim;
            public World World;
            public ShadowBaseline None;
            public PolicyImpactLedger Ledger;
            public SimulationRandom.LiveState Live;
            public bool MeetingsAroundLedger, FormationAroundLedger, DialAtBoundary;
            public int Meetings, Bills, Formations, PollingDays;
            public bool Boundary, PlayerAskedFirst;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ForkAnswersDiagnostic (FORK-2): a fork holds the player's election, gives the player's answers and never stalls; a load keeps every line's span ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            var cleanup = new List<Action>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                bool reached = ExceptField != null && FamiliesField != null && ForkSimField != null && ForkOwnField != null && LeverCtor != null && Stats.All(s => typeof(EconomyState).GetField(s)?.FieldType == typeof(float));
                Check(reached, "the harness's reach: the ledger's worlds and families, a fork's manager and generator, the lever's constructor, the stats as EconomyState floats");
                if (!reached) { throw new InvalidOperationException("a member the harness reads moved - VERIFIED NOTHING"); }

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ 1. across the polling day and the meetings
                        Outcome t = RunAcross(hosts, cleanup, Governing, Mode.Ledger), e = RunAcross(hosts, cleanup, Governing, Mode.NoElection), m = RunAcross(hosts, cleanup, Governing, Mode.MeetingsAround);
                        Check(t.Governs, F("1. {0} leads {1}'s government at the start, holding Foreign Affairs (the meetings roll for it)", Governing, Player));
                        Check(t.PollingDays == 1 && t.ElectionHeld && t.Meetings > 0,
                            F("1. the played world held its election on {0} ({1} polling day(s) in the span, the chamber counted: {2}); the player answered {3} meeting(s), laid {4} budget bill(s) and gave {5} formation answer(s); read at the boundary of {6}",
                                t.PollingDay, t.PollingDays, t.ElectionHeld ? "yes" : "NO", t.Meetings, t.Bills, t.Formations, t.ReadOn));
                        Check(t.Real == e.Real && t.Real == m.Real, "1. the played world is the same in all three runs - the forks are passive, and an answer given around the ledger changes nothing in it"
                            + (t.Real != e.Real ? FirstDifference(t.Real, e.Real) : t.Real != m.Real ? FirstDifference(t.Real, m.Real) : string.Empty));
                        Check(t.Fork == t.Real && t.Line.Values.All(v => v == 0f),
                            F("1. the ledger's Trade fork (a lever's, no act) EQUALS the played world across the polling day, the answered meetings and the bills{0}; its line {1}",
                                t.Fork == t.Real ? string.Empty : FirstDifference(t.Real, t.Fork), Lines(t.Line)));
                        Check(e.Fork != e.Real, "1. the control holding no election (every fork before FORK-2) differs from the played world" + (e.Fork != e.Real ? FirstDifference(e.Real, e.Fork) : " - IT DOES NOT: the case has no teeth"));
                        Check(m.Fork != m.Real, "1. the control handed no MEETING answer (the bills still replayed) differs from the played world" + (m.Fork != m.Real ? FirstDifference(m.Real, m.Fork) : " - IT DOES NOT: a broken meeting replay would pass"));
                        sb.Append(F("    report    1. the no-policy world (handed no answer, its waits answered by the AI's rule): its meeting slot {0}; its ForeignPolicy draws {1} against the played world's {2}\n",
                            t.NoneMeetingPending ? "HOLDS a meeting" : "is empty", t.NoneForeignDraws, t.RealForeignDraws));

                        // ============================================================ 2. the Speaker's question
                        // the precondition planted, not assumed of the seed: the player's party is the one the Speaker asks first after the count - case 1's run
                        // read who that is; where it was not the player's party, the game is played again as the party asked (at most twice)
                        string asked = Governing;
                        Outcome q = t;
                        int runs = 1;
                        for (int again = 0; again < 2 && !q.PlayerAskedFirst && q.FirstAsked != null && q.FirstAsked != asked; again++)
                        {
                            asked = q.FirstAsked;
                            q = RunAcross(hosts, cleanup, asked, Mode.Ledger);
                            runs++;
                        }
                        Outcome qa = RunAcross(hosts, cleanup, asked, Mode.FormationAround);
                        Check(q.PlayerAskedFirst && q.Formations > 0,
                            F("2. the player leading {0}: the Speaker asked the player's party first after the count of {1} ({2}, found in {3} run(s)); the player answered {4} formation question(s) (a pass, an offer declined)",
                                asked, q.PollingDay, q.PlayerAskedFirst ? "yes" : "NO - the case has no teeth: the Speaker asked " + (q.FirstAsked ?? "no party"), runs, q.Formations));
                        Check(q.Real == qa.Real, "2. the played world is the same with the pass given through the ledger and around it" + (q.Real == qa.Real ? string.Empty : FirstDifference(q.Real, qa.Real)));
                        Check(q.Fork == q.Real && q.Line.Values.All(v => v == 0f),
                            F("2. the ledger's Trade fork EQUALS the played world through the Speaker's round{0}; its line {1}", q.Fork == q.Real ? string.Empty : FirstDifference(q.Real, q.Fork), Lines(q.Line)));
                        Check(qa.Fork != qa.Real && qa.ForkFormed,
                            F("2. the control handed no pass differs from the played world{0} - and does not stall: the next day it answers the question by the AI's rule and FORMS A GOVERNMENT: {1}",
                                qa.Fork != qa.Real ? FirstDifference(qa.Real, qa.Fork) : " - IT DOES NOT: the case has no teeth", qa.ForkGovernment));
                        Check((q.NoneFirstAsked == asked || q.NonePlanted) && q.NoneFormed,
                            F("2. the NO-POLICY world, its Speaker asking the player's party first ({0}) and handed no answer, answered by the AI's rule and FORMED A GOVERNMENT: {1}",
                                q.NoneFirstAsked == asked ? "as counted" : q.NonePlanted ? "planted - its own count asked " + (q.NoneFirstAsked ?? "no party") : "NEITHER COUNTED NOR PLANTED - the case has no teeth", q.NoneGovernment));

                        // ============================================================ 3. the polling day on a boundary
                        Outcome b = RunBoundaryPolls(hosts, cleanup);
                        Check(b.ElectionHeld && b.ForkHeld && b.Fork == b.Real,
                            F("3. the polling day {0} IS the first boundary: the played world held its election ({1}); the Monetary dial's fork, made by the ledger at that boundary before it, held its own after the boundary ({2}) and EQUALS the played world{3}",
                                Day(BoundaryPolls), b.ElectionHeld ? "yes" : "NO", b.ForkHeld ? "yes" : "NO - the flag was not restored with the fork", b.Fork == b.Real ? string.Empty : FirstDifference(b.Real, b.Fork)));

                        // ============================================================ 4. a load keeps every line's span; 5. the cost
                        failures += LoadKeepsTheSpans(hosts, cleanup, sb);
                        failures += MeasureTheCost(hosts, cleanup, sb);

                        // ============================================================ 6. a wait the played world never had
                        // the precondition planted: a player whose party the played world's Speaker does NOT ask first (case 2 found who is asked)
                        string unasked = asked == Opposition ? Governing : Opposition;
                        Outcome p = RunAcross(hosts, cleanup, unasked, Mode.PlantedAsk);
                        Check(p.Planted && p.ForkFormed,
                            F("6. the player leading " + unasked + ": the day after the count the played world's round stands at {0}; the Trade fork's Speaker is made to ask the player's party there ({1}) - a family world whose seats ask the player where the played world's did not - and with no answer replayed it answers by the AI's rule the next day and FORMS A GOVERNMENT: {2}",
                                p.PlantedOver, p.Planted ? "planted" : "NOT PLANTED - the case has no teeth", p.ForkGovernment));

                        // ============================================================ 7. the docket's identity across worlds
                        failures += DocketIdentity(hosts, cleanup, sb);

                        // ============================================================ 8. a replay that misses; 9. a fork that fails
                        failures += MissAndFault(hosts, cleanup, sb);
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw - ").Append(ex.GetType().Name).Append(": ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            finally
            {
                foreach (Action done in cleanup) { done(); }
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
                UsLapseDraw.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== ForkAnswersDiagnostic: ALL ASSERTIONS PASS - a fork with no policy difference equals the played world across a polling day and an answered meeting, and a load keeps every line's span ===\n"
                                    : F("=== ForkAnswersDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private enum Mode { Ledger, NoElection, MeetingsAround, FormationAround, PlantedAsk }

        private sealed class Outcome
        {
            public string Real, Fork, PollingDay = "never", ReadOn = "never", NoneGovernment = "none", ForkGovernment = "none", PlantedOver = "never read", FirstAsked;
            public Dictionary<string, float> Line = new Dictionary<string, float>();
            public bool Governs, ElectionHeld, ForkHeld, NoneMeetingPending, PlayerAskedFirst, NoneFormed, ForkFormed, Planted, NonePlanted;
            public string NoneFirstAsked;
            public int PollingDays, Meetings, Bills, Formations, NoneForeignDraws, RealForeignDraws;
        }

        // ------------------------------------------------------------------ 1 and 2

        private static Outcome RunAcross(List<GameObject> hosts, List<Action> cleanup, string party, Mode mode)
        {
            var o = new Outcome();
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic." + party + "." + mode, party, null, out o.Governs);
            g.MeetingsAroundLedger = mode == Mode.MeetingsAround;
            g.FormationAroundLedger = mode == Mode.FormationAround;
            SimulationRandom.Reinstate(g.Live);
            try
            {
                Country me = g.World.GetCountry(Player);
                int electionsBefore = me.ElectionHistory.Count;
                Day(g);
                Answer(g);
                var noAct = (PlayerLever)LeverCtor.Invoke(new object[] { PlayerLeverKind.PartnerTariffOverride, Player, (Func<World, Country, bool, bool>)((w, c, r) => false) });
                g.Ledger.BeforeLever(g.Sim, noAct);   // the Trade family's world, forked at a lever's moment with nothing done
                ShadowBaseline fork = TheFork(g);
                if (fork != null && mode == Mode.NoElection) { fork.HoldsPlayerElection = false; }

                bool polled = false;
                for (int d = 0; d < MaxDays; d++)
                {
                    Day(g);
                    if (g.Sim.PollingDayToday && !polled) { polled = true; o.PollingDay = Day(g.Sim.CurrentDate); }
                    if (polled && o.FirstAsked == null && g.Sim.RoundOf(Player) != null)
                    {
                        o.FirstAsked = g.Sim.RoundOf(Player).Asked;   // the party the played world's Speaker asks first after the count
                        // the no-policy world brought to the same day: whom its own Speaker asks first - and where that is not the player's party, the question
                        // planted there (case 2's precondition: the world handed no answer, asked)
                        g.None.CatchUp(g.Sim.DaysIntoTurn);
                        SimulationManager noneSim = ForkSim(g.None);
                        SpeakerRound noneRound = noneSim.RoundOf(Player);
                        o.NoneFirstAsked = noneRound?.Asked;
                        if (mode == Mode.Ledger && noneRound != null && !(noneRound.Stage == RoundStage.PlayerAsked && noneRound.Asked == party))
                        {
                            noneRound.Asked = party;
                            noneRound.Stage = RoundStage.PlayerAsked;
                            noneRound.AskedOn = noneSim.CurrentDate;
                            o.NonePlanted = true;
                        }
                    }
                    if (polled && g.Boundary) { break; }   // read at the boundary after the polling day, before the next answer (the fork reaches it at its next turn)
                    if (mode == Mode.PlantedAsk && polled && !o.Planted && fork != null && g.Sim.CurrentDate == DateTime.ParseExact(o.PollingDay, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1))
                    {
                        // the day after the count: where the played world's Speaker is not asking the player, the fork's is made to - the fork brought to today first
                        SpeakerRound played = g.Sim.RoundOf(Player);
                        o.PlantedOver = played == null ? "no round" : played.Stage + " (" + played.Asked + " asked)";
                        if (played != null && played.Stage != RoundStage.PlayerAsked && played.Stage != RoundStage.OfferToPlayer)
                        {
                            fork.CatchUp(g.Sim.DaysIntoTurn);
                            SpeakerRound forked = ForkSim(fork).RoundOf(Player);
                            if (forked != null) { forked.Asked = party; forked.Stage = RoundStage.PlayerAsked; forked.AskedOn = ForkSim(fork).CurrentDate; o.Planted = true; }
                        }
                    }
                    Answer(g);
                }
                Read(g, fork, me, electionsBefore, o);
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return o;
        }

        // ------------------------------------------------------------------ 3

        private static Outcome RunBoundaryPolls(List<GameObject> hosts, List<Action> cleanup)
        {
            var o = new Outcome();
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.boundary", Governing, BoundaryPolls.AddDays(-SimulationManager.DaysPerTurn), out o.Governs);
            g.DialAtBoundary = true;   // the Monetary dial in the boundary's decision: the ledger forks its family there (a head sets the rate, so the dial is never read)
            SimulationRandom.Reinstate(g.Live);
            try
            {
                Country me = g.World.GetCountry(Player);
                int electionsBefore = me.ElectionHistory.Count;
                for (int d = 0; d < SimulationManager.DaysPerTurn + 1 && g.Sim.CurrentTurn == 0; d++)
                {
                    Day(g);
                    if (g.Boundary) { break; }
                    Answer(g);
                }
                if (g.Sim.PollingDayToday) { o.PollingDay = Day(g.Sim.CurrentDate); }
                ShadowBaseline fork = TheFork(g);
                ElectionRecord forkLast = fork?.World.GetCountry(Player)?.ElectionHistory.LastOrDefault();
                o.ForkHeld = forkLast != null && forkLast.Date == BoundaryPolls;
                Read(g, fork, me, electionsBefore, o);
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return o;
        }

        private static void Read(Game g, ShadowBaseline fork, Country me, int electionsBefore, Outcome o)
        {
            o.ReadOn = Day(g.Sim.CurrentDate);
            o.PollingDays = g.PollingDays;
            o.Meetings = g.Meetings;
            o.Bills = g.Bills;
            o.Formations = g.Formations;
            o.PlayerAskedFirst = g.PlayerAskedFirst;
            o.ElectionHeld = me.ElectionHistory.Count > electionsBefore && NationalElection.CountsParliament(me.ElectionHistory[me.ElectionHistory.Count - 1].Method);   // PS-6 K74 (the review's L3)

            Dictionary<SimulationRandom.Stream, int> realDraws = SimulationRandom.CaptureDrawCounts();
            o.Real = Fingerprint(g.Sim, g.World, realDraws);
            o.RealForeignDraws = realDraws.TryGetValue(SimulationRandom.Stream.ForeignPolicy, out int rf) ? rf : 0;
            o.Fork = fork == null ? F("NO FORK - the run holds {0} world(s)", ((Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger)).Count) : Fingerprint(ForkSim(fork), fork.World, DrawsOf(fork));
            Dictionary<SimulationRandom.Stream, int> noneDraws = DrawsOf(g.None);
            o.NoneForeignDraws = noneDraws.TryGetValue(SimulationRandom.Stream.ForeignPolicy, out int nf) ? nf : 0;
            SimulationManager noneSim = ForkSim(g.None);
            o.NoneMeetingPending = noneSim.GetPendingForeignPolicyMeeting(Player) != null;
            GovernmentRecord noneGov = g.None.World.GetCountry(Player)?.Government;
            o.NoneFormed = noneGov != null && !noneGov.Caretaker && noneGov.Cabinet.Count > 0 && noneSim.RoundOf(Player) == null && g.None.Faulted == null;
            if (fork != null)
            {
                GovernmentRecord forkGov = fork.World.GetCountry(Player)?.Government;
                SpeakerRound forkRound = ForkSim(fork).RoundOf(Player);
                o.ForkFormed = forkGov != null && !forkGov.Caretaker && forkGov.Cabinet.Count > 0 && forkRound == null && fork.Faulted == null;
                o.ForkGovernment = forkGov == null ? "none" : F("{0} led by {1}{2}{3}", string.Join("+", forkGov.Cabinet), forkGov.PmParty, forkGov.Caretaker ? " - A CARETAKER" : string.Empty,
                    forkRound != null ? " - its round still open at " + forkRound.Stage : string.Empty);
            }
            o.NoneGovernment = noneGov == null ? "none" : F("{0} led by {1}{2}{3}{4}", string.Join("+", noneGov.Cabinet), noneGov.PmParty, noneGov.Caretaker ? " - A CARETAKER" : string.Empty,
                noneSim.RoundOf(Player) != null ? " - its round still open at " + noneSim.RoundOf(Player).Stage : string.Empty, g.None.Faulted != null ? " - FAILED: " + g.None.Faulted : string.Empty);
            foreach (string stat in Stats)
            {
                List<ImpactLine> lines = g.Ledger.LinesFor(me, stat, out float _);
                ImpactLine line = lines.FirstOrDefault(l => l.Family == "Trade" || l.Family == "Monetary");
                o.Line[stat] = line.Family != null ? line.Contribution : float.NaN;
            }
        }

        // ------------------------------------------------------------------ 4

        private static int LoadKeepsTheSpans(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }

            // the game that is saved, and a third, identical and never saved nor carried: each to the cut
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.saved", Governing, null, out bool governs);
            string refused = PlayToTheCut(g);
            Game n = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.never", Governing, null, out _);
            string refusedN = PlayToTheCut(n);
            Check(governs && refused == null && refusedN == null,
                F("4. the levers pulled through their verbs on days {0} and {1} of the first turn, in the saved game and the never-saved one: {2}", HeadDay, TariffDay, refused ?? refusedN ?? "the head appointed, a partner's tariff pinned"));
            Game loaded = null, bare = null;
            try
            {
                // ---- the cut: the save as GameController.SaveToPath writes it, and the game's own load of it ----
                SimulationRandom.Reinstate(g.Live);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                SaveGame save = SaveGameService.CreateSaveGame(g.Sim, g.World, Player, null);
                save.NoPolicyShadow = g.None.Carry(g.Sim.DaysIntoTurn);
                save.ImpactLedgerWorlds = save.NoPolicyShadow != null ? g.Ledger.Carry(g.Sim) : null;
                double carryMs = clock.Elapsed.TotalMilliseconds;
                string with = SaveGameService.Serialize(save);
                save.ImpactLedgerWorlds = null;
                string without = SaveGameService.Serialize(save);
                g.Live = SimulationRandom.CaptureLive();
                var families = new List<string>(((Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger)).Keys);
                families.Sort(StringComparer.Ordinal);
                Check(families.Count == 2 && families.Contains("Monetary") && families.Contains("Trade") && SaveGameService.Deserialize(with).ImpactLedgerWorlds?.Count == 2,
                    F("4. the save cut on day {0} of turn {1} carries the ledger's {2} world(s) [{3}] beside the no-policy world: {4:0} ms to carry, {5:N0} bytes against {6:N0} without them",
                        g.Sim.DaysIntoTurn, g.Sim.CurrentTurn, families.Count, string.Join(", ", families), carryMs, Encoding.UTF8.GetByteCount(with), Encoding.UTF8.GetByteCount(without)));

                loaded = Adopt(SaveGameService.Deserialize(with), hosts, out string whyLoaded);
                bare = Adopt(SaveGameService.Deserialize(without), hosts, out string whyBare);
                Check(loaded != null && bare != null, "4. the game's own load adopts both saves" + (loaded == null ? " - " + whyLoaded : string.Empty) + (bare == null ? " - " + whyBare : string.Empty));
                if (loaded == null || bare == null) { return failures; }
                Check(loaded.Ledger.HasAnything && !bare.Ledger.HasAnything,
                    F("4. the load of the save carrying the worlds holds every family's line ({0}); the save carrying none (every save before FORK-2) loads a ledger with {1}",
                        loaded.Ledger.HasAnything ? "yes" : "NO", bare.Ledger.HasAnything ? "lines - it should hold none" : "no line - each line would count from the load"));

                // ---- the three games on to the next boundary and one day past it, each on its own generator ----
                int endTurn = g.Sim.CurrentTurn + 1;
                foreach (Game each in new[] { g, n, loaded }) { SimulationRandom.Reinstate(each.Live); RunTo(each, endTurn); each.Live = SimulationRandom.CaptureLive(); }

                string playedNever = PlayedOf(n), playedLoaded = PlayedOf(loaded), playedSaved = PlayedOf(g);
                Check(playedNever == playedLoaded && playedNever == playedSaved,
                    F("4. the loaded game, the saved one and the never-saved one stand alike on {0} (the save's round trip, and a save leaves the game it is cut from as it was){1}",
                        Day(n.Sim.CurrentDate), playedNever != playedLoaded ? FirstDifference(playedNever, playedLoaded) : playedNever != playedSaved ? FirstDifference(playedNever, playedSaved) : string.Empty));
                string linesNever = LinesOf(n), linesLoaded = LinesOf(loaded), linesSaved = LinesOf(g);
                Check(linesNever == linesLoaded && linesNever.Length > 0,
                    F("4. EVERY LINE KEEPS ITS SPAN: the loaded game's ledger reads the never-saved, never-carried game's lines on all six stats{0}\n              {1}",
                        linesNever == linesLoaded ? string.Empty : FirstDifference(linesNever, linesLoaded), linesNever.Replace("\n", "\n              ")));
                Check(linesNever == linesSaved, "4. the catch-up is the boundary's days run sooner: the saved game's own lines (its worlds caught up by the carry) equal the never-carried game's"
                    + (linesNever == linesSaved ? string.Empty : FirstDifference(linesNever, linesSaved)));
            }
            finally
            {
                foreach (Game adopted in new[] { loaded, bare }) { if (adopted != null) { cleanup.Add(() => { adopted.Ledger.Dispose(); adopted.None.Dispose(); }); } }
            }
            return failures;
        }

        /// <summary>Case 4's game to its cut: the two levers in the first turn, then day <see cref="CutDay"/> of the second; the reason a lever was refused, or null.</summary>
        private static string PlayToTheCut(Game g)
        {
            string refused = null;
            SimulationRandom.Reinstate(g.Live);
            try
            {
                Country me = g.World.GetCountry(Player);
                while ((g.Sim.CurrentTurn == 0 || g.Sim.DaysIntoTurn < CutDay) && g.Sim.CurrentTurn < 2)
                {
                    Day(g);
                    if (g.Sim.CurrentTurn == 0 && g.Sim.DaysIntoTurn == HeadDay)
                    {
                        float bias = (me.CurrentFedChair?.RateBias ?? 0f) + 1f;
                        if (!g.Sim.AppointCentralBankHead(Player, new FedChair("FORK-2 probe head", FedChairPhilosophy.Hawkish, "the diagnostic's probe", bias), out string why)) { refused = refused ?? "the head: " + (why ?? "no reason"); }
                    }
                    if (g.Sim.CurrentTurn == 0 && g.Sim.DaysIntoTurn == TariffDay)
                    {
                        string why = "no trade partner";
                        if (me.TradePartners.Count == 0 || !g.Sim.SetPartnerTariffOverride(Player, me.TradePartners[0].PartnerId, out why)) { refused = refused ?? "the tariff: " + (why ?? "no reason"); }
                    }
                    Answer(g);
                }
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return refused;
        }

        private static string PlayedOf(Game g)
        {
            SimulationRandom.LiveState was = SimulationRandom.CaptureLive();
            try { SimulationRandom.Reinstate(g.Live); return Fingerprint(g.Sim, g.World, SimulationRandom.CaptureDrawCounts()); }
            finally { SimulationRandom.Reinstate(was); }
        }

        /// <summary>Every family's line and the interaction, on every graphed stat - one line per stat, the families in name order.</summary>
        private static string LinesOf(Game g)
        {
            var sb = new StringBuilder();
            Country me = g.World.GetCountry(Player);
            foreach (string stat in Stats)
            {
                List<ImpactLine> lines = g.Ledger.LinesFor(me, stat, out float divergence);
                sb.Append(stat).Append(": divergence=").Append(R(divergence));
                foreach (ImpactLine line in lines.OrderBy(l => l.Family, StringComparer.Ordinal)) { sb.Append('|').Append(line.Family).Append('=').Append(R(line.Contribution)); }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        private static void RunTo(Game g, int endTurn)
        {
            while (g.Sim.CurrentTurn < endTurn || g.Sim.DaysIntoTurn < 1)
            {
                Day(g);
                Answer(g);
            }
        }

        // ------------------------------------------------------------------ 5

        /// <summary>(the fix review's M5) Every family the ledger has forked - as the ledger forks a family a lever first touches (`PolicyImpactLedger`'s fork,
        /// reached here by its table), on day <see cref="HeadDay"/> - and a save cut on day <see cref="CutDay"/> of the next turn: the catch-up's time, the carry's,
        /// the bytes. Reported, not asserted, but that the carry carries them all.</summary>
        private static int MeasureTheCost(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.cost", Governing, null, out _);
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            var families = new List<string>(((System.Collections.IDictionary)FamiliesField.GetValue(null)).Keys.Cast<string>());
            SimulationRandom.Reinstate(g.Live);
            double catchUpMs, carryMs;
            int cutDay = 0, cutTurn = 0;
            int carried;
            long with, without;
            try
            {
                while (g.Sim.CurrentTurn == 0 || g.Sim.DaysIntoTurn < CutDay)
                {
                    Day(g);
                    if (g.Sim.CurrentTurn == 0 && g.Sim.DaysIntoTurn == HeadDay)
                    {
                        foreach (string family in families) { except[family] = new ShadowBaseline(g.Sim, g.World, Player, insideTurn: true, dayTick: true, seatAsPlayed: true) { AnswersWaitsAsTheAi = true }; }
                    }
                    Answer(g);
                    if (g.Sim.CurrentTurn > 2) { break; }
                }
                var clock = System.Diagnostics.Stopwatch.StartNew();
                cutDay = g.Sim.DaysIntoTurn;
                cutTurn = g.Sim.CurrentTurn;
                foreach (ShadowBaseline world in except.Values) { world.CatchUp(g.Sim.DaysIntoTurn); }
                catchUpMs = clock.Elapsed.TotalMilliseconds;
                clock.Restart();
                SaveGame save = SaveGameService.CreateSaveGame(g.Sim, g.World, Player, null);
                save.NoPolicyShadow = g.None.Carry(g.Sim.DaysIntoTurn);
                save.ImpactLedgerWorlds = g.Ledger.Carry(g.Sim);
                carryMs = clock.Elapsed.TotalMilliseconds;
                carried = save.ImpactLedgerWorlds?.Count ?? 0;
                with = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(save));
                save.ImpactLedgerWorlds = null;
                without = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(save));
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            bool ok = carried == families.Count;
            sb.Append(ok ? "    ok        " : "    FAIL      ").Append(F(
                "5. THE COST, every family forked ({0} of {1} carried), a save cut on day {2} of turn {3}: the catch-up of every world to the cut (the most a save pays) {4:0} ms, the carry {5:0} ms; the save {6:N0} bytes with every family's world against {7:N0} without ({8:N0} a family)\n",
                carried, families.Count, cutDay, cutTurn, catchUpMs, carryMs, with, without, carried > 0 ? (with - without) / carried : 0));
            return ok ? 0 : 1;
        }

        // ------------------------------------------------------------------ 7

        /// <summary>(the fix review's M1) A US player-president's docket answer replayed into a family world that holds ONE DIVISION MORE before the bill - so
        /// every later division's number there is one higher than the played world's - must still reach that world's own passage of the bill.</summary>
        private static int DocketIdentity(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            (SimulationManager s, World w, Country u) = UsDocketDiagnostic.PlayerPresidentWorld(hosts);
            var none = new ShadowBaseline(s, w, CountryId.USA, insideTurn: true, dayTick: true);
            var ledger = new PolicyImpactLedger(none);
            cleanup.Add(() => { ledger.Dispose(); none.Dispose(); });
            s.ImpactLedger = ledger;
            var noAct = (PlayerLever)LeverCtor.Invoke(new object[] { PlayerLeverKind.PartnerTariffOverride, CountryId.USA, (Func<World, Country, bool, bool>)((world, c, r) => false) });
            ledger.BeforeLever(s, noAct);
            ShadowBaseline fork = TheFork(new Game { Ledger = ledger });
            List<TaxType> taxes = UsDocketDiagnostic.Candidates(u, 1);
            string verdict;
            bool ok = false;
            if (fork == null || taxes.Count == 0) { verdict = fork == null ? "NO FORK" : "no levied tax whose removal both chambers pass - nothing to answer"; }
            else
            {
                Country forked = fork.World.GetCountry(CountryId.USA);
                forked.Divisions.Append("FORK-2 planted division", s.CurrentDate, 0f, true);   // one division more in the family world, before the bill
                TaxType type = taxes[0];
                bool introduced = s.IntroduceTaxProgramBill(CountryId.USA, type, false);
                while (s.CurrentDate < UsDocketDiagnostic.AnswerDay)
                {
                    s.AdvanceDay();
                    s.AdvanceCountryDayTick(CountryId.USA);
                }
                DivisionRecord passage = s.GetPendingTaxProgramBills(CountryId.USA).FirstOrDefault(b => b.Type == type)?.Docket?.Passage;
                bool signed = passage != null && s.SignDocketBill(CountryId.USA, passage, out _);
                fork.CatchUp(s.DaysIntoTurn);   // the family world to the answer's day: its own passage held, then the signature replayed
                DivisionRecord forkPassage = passage == null ? null : forked.Divisions.Entries.LastOrDefault(r => SimulationManager.SamePassage(r, passage.Title, passage.Date, passage.Chamber, passage.Origin));
                bool playedLaw = signed && !u.TaxLines.First(t => t.Type == type).IsImplemented;
                bool forkLaw = !forked.TaxLines.First(t => t.Type == type).IsImplemented && !ForkSim(fork).GetPendingTaxProgramBills(CountryId.USA).Any(b => b.Type == type);
                ok = introduced && playedLaw && forkLaw && forkPassage != null && forkPassage.Number == passage.Number + 1 && fork.Faulted == null;
                verdict = F("the player signs the removal of {0} on {1} (passage no. {2} in the played world, no. {3} in the family world): law in the played world {4}, law in the family world {5}",
                    type, Day(s.CurrentDate), passage?.Number ?? -1, forkPassage?.Number ?? -1, playedLaw ? "yes" : "NO", forkLaw ? "yes - the replay found its own passage" : "NO - the replay missed");
            }
            sb.Append(ok ? "    ok        " : "    FAIL      ").Append("7. THE DOCKET'S IDENTITY: one division more in the family world, the answer still lands - ").Append(verdict).Append('\n');
            return ok ? 0 : 1;
        }

        // ------------------------------------------------------------------ 8 and 9

        /// <summary>(the fix review's L6) A REPLAY THAT MISSES: a meeting planted in the played world alone, answered there - the family world finds no meeting of
        /// that name, says so in one line, and answers nothing (the planted option moves nothing, so it still equals the played world). A FORK THAT FAILS: an
        /// answer whose replay throws - the family is set aside as unavailable, and the played world is the one a twin game with no such answer plays.</summary>
        private static int MissAndFault(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            FieldInfo meetingSlot = typeof(SimulationManager).GetField("_pendingForeignPolicyMeetingByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructorInfo answerCtor = typeof(PlayerAnswer).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(CountryId?), typeof(Func<SimulationManager, bool>), typeof(bool) }, null);
            if (meetingSlot == null || answerCtor == null) { Check(false, "8./9. the harness's reach: the manager's meeting slot and the answer's constructor - VERIFIED NOTHING"); return failures; }

            // 8. the miss
            int misses = 0;
            void Count(string text, string stack, LogType type) { if (type == LogType.Warning && text.Contains("found no match")) { misses++; } }
            Application.logMessageReceived += Count;
            Game m;
            ShadowBaseline missFork;
            try
            {
                m = RunToFirstBoundary(hosts, cleanup, "miss", (g, fork) =>
                {
                    var slot = (Dictionary<CountryId, ForeignPolicyMeeting>)meetingSlot.GetValue(g.Sim);
                    if (!slot.ContainsKey(Player)) { slot[Player] = new ForeignPolicyMeeting("FORK-2 planted meeting", "a meeting the family world never rolled", new ForeignPolicyMeetingOption("hear it out")); }
                }, out missFork);
            }
            finally { Application.logMessageReceived -= Count; }
            string missReal = PlayedOf(m), missForked = missFork == null ? "NO FORK" : Fingerprint(ForkSim(missFork), missFork.World, DrawsOf(missFork));
            Check(misses >= 1 && missFork?.Faulted == null && missReal == missForked,
                F("8. A REPLAY THAT MISSES: the planted meeting answered in the played world finds no match in the family world ({0} line(s) said so), nothing is answered there, and the fork still EQUALS the played world{1}",
                    misses, missReal == missForked ? string.Empty : FirstDifference(missReal, missForked)));

            // 9. the fault, beside a twin with no fault
            Game twin = RunToFirstBoundary(hosts, cleanup, "twin", null, out _);
            Game faulty = RunToFirstBoundary(hosts, cleanup, "fault", null, out _, g =>
            {
                var planted = (PlayerAnswer)answerCtor.Invoke(new object[] { "a planted answer that throws", (CountryId?)Player, (Func<SimulationManager, bool>)(sim => throw new InvalidOperationException("FORK-2's planted fault")), false });
                bool logged = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false;   // the planted fault's own error lines are this case's expectation; the fault is read from the ledger below
                try { g.Ledger.AfterAnswer(g.Sim, planted); }
                finally { Debug.unityLogger.logEnabled = logged; }
            });
            bool unavailable = faulty.Ledger.Unavailable.ContainsKey("Trade") && TheFork(faulty) == null && faulty.Ledger.HasAnything;
            string twinReal = PlayedOf(twin), faultyReal = PlayedOf(faulty);
            Check(unavailable && twinReal == faultyReal,
                F("9. A FORK THAT FAILS: a replay that throws sets the Trade family aside as unavailable ({0}) and the played world is the twin's, untouched{1}",
                    unavailable ? "\"" + faulty.Ledger.Unavailable["Trade"] + "\"" : "NOT SET ASIDE", twinReal == faultyReal ? string.Empty : FirstDifference(twinReal, faultyReal)));
            return failures;
        }

        /// <summary>Cases 8 and 9: a game from Sweden's start, the player's party leading, a Trade lever-fork after the first day (and <paramref name="atFork"/>
        /// then), <paramref name="secondDay"/> after the second day, answered as the clock holds - to its first boundary, read before the next answer.</summary>
        private static Game RunToFirstBoundary(List<GameObject> hosts, List<Action> cleanup, string name, Action<Game, ShadowBaseline> secondDay, out ShadowBaseline fork, Action<Game> atFork = null)
        {
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic." + name, Governing, null, out _);
            SimulationRandom.Reinstate(g.Live);
            fork = null;
            try
            {
                Day(g);
                Answer(g);
                var noAct = (PlayerLever)LeverCtor.Invoke(new object[] { PlayerLeverKind.PartnerTariffOverride, Player, (Func<World, Country, bool, bool>)((w, c, r) => false) });
                g.Ledger.BeforeLever(g.Sim, noAct);
                fork = TheFork(g);
                atFork?.Invoke(g);
                Day(g);
                secondDay?.Invoke(g, fork);
                Answer(g);
                while (g.Sim.CurrentTurn == 0)
                {
                    Day(g);
                    if (g.Boundary) { break; }
                    Answer(g);
                }
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return g;
        }

        // ------------------------------------------------------------------ the game

        /// <summary>Sweden from its start (or from <paramref name="opens"/>), the player leading <paramref name="party"/> - the government of record seated
        /// (`NoPolicyShadowCarryDiagnostic`'s seating) - the no-policy world forked at the start as `GameController.ForkTheShadowAtTheStart` forks it, the
        /// ledger around it watching the manager's verbs.</summary>
        private static Game OpenSweden(List<GameObject> hosts, List<Action> cleanup, string name, string party, DateTime? opens, out bool governs)
        {
            WorldClock.ApplyStart(Player);
            if (opens.HasValue) { SimulationManager.SetEpoch(opens.Value); }
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            var go = new GameObject(name); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            World world = WorldFactory.CreateDefault();
            sim.SetWorld(world);
            sim.PlayerCountryId = Player;
            Country me = world.GetCountry(Player);
            me.PlayerPartyAbbrev = party;
            me.PartyApprovalRating = me.State.ApprovalRating;
            me.Government = GovernmentRecord.AtStart(me, SimulationManager.EpochDate, world);
            governs = sim.PlayerGoverns(me) && sim.PlayerMayIntroduce(Player, CabinetPortfolio.ForeignAffairs, out _);
            var none = new ShadowBaseline(sim, world, Player, insideTurn: true, dayTick: true);   // GameController.ForkTheShadowAtTheStart's
            var ledger = new PolicyImpactLedger(none);
            cleanup.Add(() => { ledger.Dispose(); none.Dispose(); });
            sim.ImpactLedger = ledger;
            return new Game { Sim = sim, World = world, None = none, Ledger = ledger, Live = SimulationRandom.CaptureLive() };
        }

        /// <summary>One day in `GameController.Update`'s order: the day, the player's day tick; on the boundary `GameController.AdvanceTurn`'s - the ledger's
        /// worlds, the played world, the no-policy world, the played world's market state; then the player's election where today is its polling day.</summary>
        private static void Day(Game g)
        {
            g.Boundary = g.Sim.AdvanceDay();
            g.Sim.AdvanceCountryDayTick(Player);
            if (g.Sim.PollingDayToday) { g.PollingDays++; }
            if (g.Boundary)
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in g.World.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                if (g.DialAtBoundary) { decisions[Player] = new PolicyDecision { InterestRateChange = Dial }; }
                g.Ledger.AdvanceTurn(g.Sim, g.World, Player, decisions);
                g.Sim.AdvanceTurn(decisions);
                g.None.AdvanceTurn();
                EnergyMarket.BeginTurn(g.World);
            }
            g.Sim.HoldPlayerElection();
        }

        /// <summary>The player's answers the clock waits for, given the way the screens give them (the manager's verbs): a meeting answered with its first
        /// option, each cabinet decision with its first, a budget bill laid at the standing figures where the budget process is open, the Speaker's question passed, an offer declined. In a control
        /// the ledger is not watching while one kind is given, as no ledger watched an answer before FORK-2.</summary>
        private static void Answer(Game g)
        {
            PolicyImpactLedger watching = g.Sim.ImpactLedger;
            try
            {
                ForeignPolicyMeeting meeting = g.Sim.GetPendingForeignPolicyMeeting(Player);
                if (meeting != null && meeting.Options.Count > 0)
                {
                    g.Sim.ImpactLedger = g.MeetingsAroundLedger ? null : watching;
                    g.Sim.ResolveForeignPolicyMeeting(Player, meeting.Options[0]);
                    g.Meetings++;
                }
                g.Sim.ImpactLedger = watching;
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in g.Sim.GetPendingCabinetDecisions(Player).ToList())
                {
                    if (decision?.Options != null && decision.Options.Count > 0) { g.Sim.ResolveCabinetDecision(Player, portfolio, decision, decision.Options[0]); }   // the fix review's L4: the clock holds on these too
                }
                if (g.Sim.GetPendingBudgetProcess(Player) && g.Sim.IntroduceBudgetBill(Player, new BudgetBill())) { g.Bills++; }
                SpeakerRound round = g.Sim.RoundOf(Player);
                if (round != null && (round.Stage == RoundStage.PlayerAsked || round.Stage == RoundStage.OfferToPlayer))
                {
                    if (round.Stage == RoundStage.PlayerAsked && g.Formations == 0) { g.PlayerAskedFirst = true; }
                    g.Sim.ImpactLedger = g.FormationAroundLedger ? null : watching;
                    bool answered = round.Stage == RoundStage.PlayerAsked ? g.Sim.PassFormation(Player, out _) : g.Sim.AnswerOffer(Player, false, out _);
                    if (answered) { g.Formations++; }
                }
            }
            finally { g.Sim.ImpactLedger = watching; }
        }

        /// <summary>A save adopted by the controller's own load path (`GameController.RestoreFromSave`), as `NoPolicyShadowCarryDiagnostic` adopts one: the game
        /// it leaves - its manager, world, no-policy world and ledger - with the generator the load set, the caller's put back.</summary>
        private static Game Adopt(SaveGame save, List<GameObject> hosts, out string error)
        {
            error = null;
            var go = new GameObject("ForkAnswersDiagnostic.controller"); hosts.Add(go);
            SimulationRandom.LiveState caller = SimulationRandom.CaptureLive();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                var controller = go.AddComponent<PoliSim.UI.GameController>();
                const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(PoliSim.UI.GameController);
                FieldInfo simField = type.GetField("_simulationManager", Private), worldField = type.GetField("_world", Private),
                          shadowField = type.GetField("_shadowBaseline", Private), ledgerField = type.GetField("_impactLedger", Private);
                MethodInfo restore = type.GetMethod("RestoreFromSave", Private);
                if (simField == null || worldField == null || shadowField == null || ledgerField == null || restore == null) { error = "the controller's load path could not be reached by name - VERIFIED NOTHING"; return null; }
                simField.SetValue(controller, sim);
                try { restore.Invoke(controller, new object[] { save }); }
                catch (TargetInvocationException e) { error = "the controller cannot adopt the save - " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; return null; }
                var game = new Game { Sim = sim, World = worldField.GetValue(controller) as World, None = shadowField.GetValue(controller) as ShadowBaseline, Ledger = ledgerField.GetValue(controller) as PolicyImpactLedger, Live = SimulationRandom.CaptureLive() };
                if (game.World == null || game.None == null || game.Ledger == null) { error = "the controller holds no world, no-policy world or ledger after the load"; return null; }
                if (!ReferenceEquals(sim.ImpactLedger, game.Ledger)) { error = "the loaded manager is not watched by the loaded ledger"; return null; }
                return game;
            }
            finally { SimulationRandom.Reinstate(caller); }
        }

        // ------------------------------------------------------------------ reading

        private static ShadowBaseline TheFork(Game g)
        {
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            return except.Count == 1 ? except.Values.First() : null;
        }

        private static SimulationManager ForkSim(ShadowBaseline w) => (SimulationManager)ForkSimField.GetValue(w);

        /// <summary>A counterfactual world's own draw counts, read with its generator in and the caller's put back.</summary>
        private static Dictionary<SimulationRandom.Stream, int> DrawsOf(ShadowBaseline w)
        {
            var own = (SimulationRandom.LiveState)ForkOwnField.GetValue(w);
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            try { SimulationRandom.Reinstate(own); return SimulationRandom.CaptureDrawCounts(); }
            finally { SimulationRandom.Reinstate(real); }
        }

        /// <summary>The whole game, one field per '|': every country's EconomyState floats and its zone's rate; the player's government, divisions, elections
        /// (the last one's seats), chamber and pending slots (budget process and bill, the Speaker's round, the meeting); the counted streams.</summary>
        private static string Fingerprint(SimulationManager sim, World world, Dictionary<SimulationRandom.Stream, int> draws)
        {
            var sb = new StringBuilder();
            sb.Append("clock ").Append(Day(sim.CurrentDate)).Append(" turn ").Append(sim.CurrentTurn).Append('\n');
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=').Append(R((float)f.GetValue(c.State))).Append('|');
                }
                sb.Append("rate=").Append(R(c.CurrencyZone?.InterestRate ?? 0f)).Append('\n');
            }

            Country p = world.GetCountry(Player);
            GovernmentRecord gov = p.Government;
            sb.Append("government|").Append(gov == null ? "none" : F("{0}|{1}|{2}|{3}|{4}|{5}", gov.PmParty, string.Join(",", gov.Cabinet), string.Join(",", gov.Support), Day(gov.FormedOn), gov.Outcome, gov.Caretaker)).Append('\n');
            DivisionRecord last = p.Divisions.Entries.LastOrDefault();
            sb.Append("divisions|").Append(p.Divisions.Entries.Count).Append('|').Append(last == null ? "none" : F("{0}|{1}|{2}|{3}", last.Number, Day(last.Date), last.Title, last.Passed)).Append('\n');
            ElectionRecord held = p.ElectionHistory.LastOrDefault();
            sb.Append("elections|").Append(p.ElectionHistory.Count).Append('|').Append(held == null ? "none" : Day(held.Date) + "|" + held.Method + "|" + (held.Seats == null ? "no seats" : string.Join(",", held.Seats.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "=" + kv.Value)))).Append('\n');
            sb.Append("chamber|").Append(string.Join(",", p.ParliamentSeats.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "=" + kv.Value))).Append('\n');
            BudgetBill bill = sim.GetPendingBudgetBill(Player);
            SpeakerRound round = sim.RoundOf(Player);
            sb.Append("pending|process=").Append(sim.GetPendingBudgetProcess(Player)).Append("|bill=").Append(bill == null ? "none" : F("{0}{1}", bill.DaysRemaining, bill.GovernmentBill ? " gov" : string.Empty))
              .Append("|round=").Append(round == null ? "none" : round.Stage + " " + round.Asked).Append("|meeting=").Append(sim.GetPendingForeignPolicyMeeting(Player)?.Name ?? "none")
              .Append("|cabinet=").Append(sim.GetPendingCabinetDecisions(Player).Count).Append('\n');
            sb.Append("draws|").Append(string.Join("|", draws.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key.ToString(), StringComparer.Ordinal).Select(kv => kv.Key + "=" + kv.Value))).Append('\n');
            return sb.ToString();
        }

        private static string FirstDifference(string a, string b)
        {
            string[] x = a.Split('\n'), y = b.Split('\n');
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                if (x[i] == y[i]) { continue; }
                string[] p = x[i].Split('|'), q = y[i].Split('|');
                string head = x[i].Contains(":") ? x[i].Substring(0, x[i].IndexOf(':')) + " " : string.Empty;
                for (int j = 0; j < Math.Min(p.Length, q.Length); j++) { if (p[j] != q[j]) { return F(" - first at {0}{1} against {2}", head, p[j], q[j]); } }
                return F(" - first at line {0}", i);
            }
            return " - the lengths differ";
        }

        private static string Lines(Dictionary<string, float> line) => string.Join(", ", line.Select(kv => kv.Key + " " + R(kv.Value)));
        private static string R(float v) => v.ToString("R", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "never" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
