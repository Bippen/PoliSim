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
    /// time, the carry's, and the save's bytes with every family's world against without. K46 (Elias's ruling of 2026-10-23): the save carries only the worlds
    /// that differ from the played world and names the rest for the load to rebuild; an autosave carries none, and its load rebuilds every family, the same
    /// worlds on a second load of the same save.</description></item>
    /// <item><description><b>K50</b> (case 1, confirmed 2026-10-23): the no-policy world holds its own election on the polling day, and keeps the standing
    /// budget. <b>K51</b> (case 10): a bill the player lays is its kind's family - that family's world is the played world as the bill found it, and every other
    /// world lays the bill on its day.</description></item>
    /// </list>
    /// <para>⚠ <b>What it does not claim.</b> The no-policy world is compared with the played world only where nothing was answered (case 2, up to the polling
    /// day); one bill kind's family is exercised (the budget's) - that every kind's verb carries its kind is `PolicyImpactLedgerCheck`'s. Times and bytes are one
    /// measurement each.</para>
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
        /// <summary>K46: the ledger's record of the worlds an act was withheld from (planted cases 5 and 11).</summary>
        private static readonly FieldInfo WithheldField = typeof(PolicyImpactLedger).GetField("_withheld", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo AnsweredAloneProperty = typeof(ShadowBaseline).GetProperty("AnsweredAlone", BindingFlags.Instance | BindingFlags.Public);
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
            /// <summary>K50: the player's country's standing figures at the start - every rate line's rate (the carbon line's is the statute's to move) and every
            /// welfare programme's generosity - which the no-policy world keeps.</summary>
            public string StandingAtStart;
        }

        /// <summary>K50 (Elias's ruling of 2026-10-23: "the no-policy world ... keeps the standing budget"): the standing figures a budget sets in the player's
        /// country, one per '|' - the rate lines' rates (not the carbon line, whose figure the statute moves at a boundary) and the welfare programmes' generosity.</summary>
        private static string Standing(Country c) =>
            string.Join("|", c.TaxLines.Where(t => !t.IsPerTonne).Select(t => t.Type + "=" + R(t.Rate)).Concat(c.WelfarePrograms.Select(w => w.Type + "=" + R(w.GenerosityLevel))));

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
                bool reached = ExceptField != null && FamiliesField != null && WithheldField != null && AnsweredAloneProperty?.GetSetMethod(true) != null && ForkSimField != null && ForkOwnField != null && LeverCtor != null && Stats.All(s => typeof(EconomyState).GetField(s)?.FieldType == typeof(float));
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
                        // K50 (Elias's ruling of 2026-10-23, confirmed): "the no-policy world holds its own elections and keeps the standing budget"
                        Check(t.NoneElectionHeld, F("1. K50 - the NO-POLICY world held its own election on the polling day {0}: its last election {1}", t.PollingDay, t.NoneElection));
                        Check(t.NoneStanding == t.StartStanding && t.Bills > 0,
                            F("1. K50 - the NO-POLICY world keeps the standing budget: with the player's party governing it laid none, and every rate line's rate and welfare programme's generosity stands as at the start, where the played world laid {0} budget bill(s){1}",
                                t.Bills, t.NoneStanding == t.StartStanding ? string.Empty : FirstDifference(t.StartStanding, t.NoneStanding)));

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

                        // ============================================================ 10. K51: a bill is its kind's family
                        failures += BillsHaveTheirFamily(hosts, cleanup, sb);

                        // ============================================================ 11. the review's M2: K46's guards and the refused bill, with teeth
                        failures += GuardsHaveTeeth(hosts, cleanup, sb);

                        // ============================================================ 12. a campaign on a ledger game replays (the merged film real911)
                        failures += CampaignReplaysOnALedgerGame(hosts, cleanup, sb);
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
            /// <summary>K50: the no-policy world's own election (its last record's day and whether it counted the chamber) and its standing figures against the start's.</summary>
            public string NoneElection = "none", NoneStanding, StartStanding;
            public bool NoneElectionHeld;
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
            Country noneMe = g.None.World.GetCountry(Player);
            ElectionRecord noneHeld = noneMe?.ElectionHistory.LastOrDefault();
            o.NoneElectionHeld = noneHeld != null && Day(noneHeld.Date) == o.PollingDay && NationalElection.CountsParliament(noneHeld.Method);   // K50: the no-policy world held the player's election on its own polling day
            o.NoneElection = noneHeld == null ? "none" : Day(noneHeld.Date) + " (" + noneHeld.Method + ")";
            o.NoneStanding = noneMe == null ? "no country" : Standing(noneMe);
            o.StartStanding = g.StandingAtStart;
            GovernmentRecord noneGov = noneMe?.Government;
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
                List<string> rebuild = null;
                save.ImpactLedgerWorlds = save.NoPolicyShadow != null ? g.Ledger.Carry(g.Sim, out rebuild) : null;
                save.ImpactLedgerRebuild = rebuild;   // K46: as GameController.SaveToPath names them
                double carryMs = clock.Elapsed.TotalMilliseconds;
                string with = SaveGameService.Serialize(save);
                save.ImpactLedgerWorlds = null;
                save.ImpactLedgerRebuild = null;
                string without = SaveGameService.Serialize(save);
                g.Live = SimulationRandom.CaptureLive();
                var families = new List<string>(((Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger)).Keys);
                families.Sort(StringComparer.Ordinal);
                SaveGame reread = SaveGameService.Deserialize(with);
                var named = new HashSet<string>((reread.ImpactLedgerWorlds?.Keys ?? Enumerable.Empty<string>()).Concat(reread.ImpactLedgerRebuild ?? Enumerable.Empty<string>()));
                // K51: the budget bills the player laid fork the Budget bill family beside the two levers' - every family the save carries or names for the load
                Check(families.Contains("Monetary") && families.Contains("Trade") && reread.ImpactLedgerWorlds != null && reread.ImpactLedgerWorlds.ContainsKey("Monetary") && reread.ImpactLedgerWorlds.ContainsKey("Trade") && named.SetEquals(families),
                    F("4. the save cut on day {0} of turn {1} carries the ledger's worlds [{2}] and names [{3}] for the load to rebuild, of its {4} famil(ies) [{5}], beside the no-policy world: {6:0} ms to carry, {7:N0} bytes against {8:N0} without them",
                        g.Sim.DaysIntoTurn, g.Sim.CurrentTurn, string.Join(", ", (reread.ImpactLedgerWorlds?.Keys ?? Enumerable.Empty<string>()).OrderBy(k => k, StringComparer.Ordinal)), string.Join(", ", reread.ImpactLedgerRebuild ?? new List<string>()),
                        families.Count, string.Join(", ", families), carryMs, Encoding.UTF8.GetByteCount(with), Encoding.UTF8.GetByteCount(without)));

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

        /// <summary>(the fix review's M5; K46, Elias's ruling of 2026-10-23) Every family the ledger has - each world planted on day `HeadDay` as the ledger forks
        /// a family a lever first touches (`PolicyImpactLedger`'s fork, reached here by its table) - and a save cut on day `CutDay` of the next turn: the catch-up's
        /// time, the carry's, the bytes. K46: the save carries only the worlds that differ from the played world - every planted world that saw no act of its own
        /// since (each but the Budget bill family's, whose bills the player kept laying) is the played world, and is NAMED for the load to rebuild, not carried;
        /// the bytes are printed with every world carried (before K46), as K46 carries them, and as an autosave writes them (no world, every family named). An
        /// autosave's load rebuilds every family from the loaded game, twice over the same save to the same worlds, each the loaded game itself.</summary>
        private static int MeasureTheCost(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.cost", Governing, null, out _);
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            var families = new List<string>(((System.Collections.IDictionary)FamiliesField.GetValue(null)).Keys.Cast<string>());
            SimulationRandom.Reinstate(g.Live);
            double catchUpMs, carryMs;
            int cutDay = 0, cutTurn = 0, billsAtFork = -1;
            List<string> rebuilt;
            Dictionary<string, string> carriedWorlds;
            long every, k46, autosave, without;
            SaveGame autosaved;
            try
            {
                while (g.Sim.CurrentTurn == 0 || g.Sim.DaysIntoTurn < CutDay)
                {
                    Day(g);
                    if (g.Sim.CurrentTurn == 0 && g.Sim.DaysIntoTurn == HeadDay)
                    {
                        foreach (string family in families)
                        {
                            if (except.TryGetValue(family, out ShadowBaseline earlier)) { earlier.Dispose(); }   // K51: a budget bill laid before the day forked its family already
                            except[family] = new ShadowBaseline(g.Sim, g.World, Player, insideTurn: true, dayTick: true, seatAsPlayed: true) { AnswersWaitsAsTheAi = true };
                        }
                        ((HashSet<string>)WithheldField.GetValue(g.Ledger)).Clear();   // the review's L6: a bill laid before the day marked its earlier, disposed world - the planted worlds start with nothing withheld
                        billsAtFork = g.Bills;
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
                carriedWorlds = g.Ledger.Carry(g.Sim, out rebuilt);
                carryMs = clock.Elapsed.TotalMilliseconds;
                rebuilt = rebuilt ?? new List<string>();
                save.ImpactLedgerWorlds = carriedWorlds;
                save.ImpactLedgerRebuild = rebuilt;
                k46 = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(save));
                save.ImpactLedgerWorlds = except.ToDictionary(kv => kv.Key, kv => kv.Value.Carry(g.Sim.DaysIntoTurn));   // before K46: every world carried
                save.ImpactLedgerRebuild = null;
                every = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(save));
                save.ImpactLedgerWorlds = null;
                without = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(save));
                save.ImpactLedgerRebuild = g.Ledger.FamiliesToRebuild();   // as GameController.SaveToPath writes an autosave
                string autosaveText = SaveGameService.Serialize(save);
                autosave = Encoding.UTF8.GetByteCount(autosaveText);
                autosaved = SaveGameService.Deserialize(autosaveText);
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }

            var laid = new HashSet<string>(g.Bills > billsAtFork ? new[] { "Budget bill" } : new string[0]);   // the families an act of their own reached since the planting
            var carriedNames = new HashSet<string>(carriedWorlds?.Keys ?? Enumerable.Empty<string>());
            var expectRebuilt = new HashSet<string>(families.Where(f => !laid.Contains(f)));
            bool rebuiltAsExpected = new HashSet<string>(rebuilt).SetEquals(expectRebuilt);
            Check(billsAtFork >= 0 && carriedNames.SetEquals(laid) && rebuiltAsExpected,
                F("5. K46 - A SAVE CARRIES ONLY THE WORLDS THAT DIFFER: every family planted on day {0} ({1}), {2} budget bill(s) laid since; the save cut on day {3} of turn {4} carries [{5}] (the families an act of their own reached: [{6}]) and names the other {7} for the load to rebuild{8}",
                    HeadDay, families.Count, Math.Max(0, g.Bills - billsAtFork), cutDay, cutTurn, string.Join(", ", carriedNames.OrderBy(k => k, StringComparer.Ordinal)), string.Join(", ", laid),
                    rebuilt.Count, rebuiltAsExpected ? string.Empty : " - NOT AS EXPECTED: carried as well " + string.Join(", ", expectRebuilt.Where(f => !rebuilt.Contains(f)))));
            sb.Append(F("    report    5. THE COST: the catch-up of every world to the cut (the most a save pays) {0:0} ms, the carry {1:0} ms; the save, the worlds PLANTED (none withheld, an upper bound on the saving), {2:N0} bytes with every family's world (before K46), {3:N0} as K46 carries them, {4:N0} as an autosave writes it, {5:N0} with no family named ({6:N0} a family carried)\n",
                catchUpMs, carryMs, every, k46, autosave, without, families.Count > 0 ? (every - without) / families.Count : 0));

            // K46: an autosave's load rebuilds every family from the loaded game - the game's own load, twice over the same save
            Game a = null, b = null;
            try
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                a = Adopt(autosaved, hosts, out string whyA);
                double loadMs = clock.Elapsed.TotalMilliseconds;
                b = Adopt(SaveGameService.Deserialize(SaveGameService.Serialize(autosaved)), hosts, out string whyB);
                if (a == null || b == null) { Check(false, "5. K46 - the game's own load adopts the autosave - " + (whyA ?? whyB)); return failures; }
                var worldsA = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(a.Ledger);
                var worldsB = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(b.Ledger);
                string playedA = PlayedOf(a);
                string Print(ShadowBaseline w) => Fingerprint(ForkSim(w), w.World, DrawsOf(w));
                var unlike = families.Where(f => !worldsA.ContainsKey(f) || !worldsB.ContainsKey(f) || Print(worldsA[f]) != Print(worldsB[f]) || Print(worldsA[f]) != playedA).ToList();
                Check(new HashSet<string>(a.Ledger.Rebuilt).SetEquals(families) && unlike.Count == 0 && a.Ledger.HasAnything,
                    F("5. K46 - AN AUTOSAVE CARRIES NONE AND THE LOAD REBUILDS THEM: the autosave names {0} famil(ies) and carries no world; its load ({1:0} ms) rebuilt {2}, every one the loaded game itself and the same world on a second load of the same save (deterministic){3}",
                        autosaved.ImpactLedgerRebuild?.Count ?? 0, loadMs, a.Ledger.Rebuilt.Count, unlike.Count == 0 ? string.Empty : " - NOT SO for " + string.Join(", ", unlike)));
            }
            finally
            {
                foreach (Game adopted in new[] { a, b }) { if (adopted != null) { cleanup.Add(() => { adopted.Ledger.Dispose(); adopted.None.Dispose(); }); } }
            }

            // the review's M1: the same measurement on a game whose families the LEDGER forked (two levers through their verbs, the budget bills the player
            // lays), not planted - every family a real game touches has had an act withheld, so a manual save carries it: K46's saving in play, reported
            Game p = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.cost.played", Governing, null, out _);
            string refusedP = PlayToTheCut(p);
            SimulationRandom.Reinstate(p.Live);
            try
            {
                var worldsP = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(p.Ledger);
                SaveGame saveP = SaveGameService.CreateSaveGame(p.Sim, p.World, Player, null);
                saveP.NoPolicyShadow = p.None.Carry(p.Sim.DaysIntoTurn);
                Dictionary<string, string> carriedP = p.Ledger.Carry(p.Sim, out List<string> rebuiltP);
                saveP.ImpactLedgerWorlds = carriedP;
                saveP.ImpactLedgerRebuild = rebuiltP;
                long k46P = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(saveP));
                saveP.ImpactLedgerWorlds = worldsP.ToDictionary(kv => kv.Key, kv => kv.Value.Carry(p.Sim.DaysIntoTurn));
                saveP.ImpactLedgerRebuild = null;
                long everyP = Encoding.UTF8.GetByteCount(SaveGameService.Serialize(saveP));
                sb.Append(F("    report    5. K46 IN PLAY (the ledger's own forks{0}): the save cut on day {1} of turn {2} carries {3} of the {4} touched families' worlds [{5}], names {6} for the load to rebuild; {7:N0} bytes as K46 carries them against {8:N0} with every world - the planted figures above are an upper bound on the saving, not a game's\n",
                    refusedP == null ? string.Empty : " - a lever refused: " + refusedP, p.Sim.DaysIntoTurn, p.Sim.CurrentTurn, carriedP?.Count ?? 0, worldsP.Count,
                    string.Join(", ", (carriedP?.Keys ?? Enumerable.Empty<string>()).OrderBy(k => k, StringComparer.Ordinal)), rebuiltP?.Count ?? 0, k46P, everyP));
            }
            finally { p.Live = SimulationRandom.CaptureLive(); }
            return failures;
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

        // ------------------------------------------------------------------ 10

        /// <summary>K51 (Elias's ruling of 2026-10-23: "each kind of bill is its own ledger family"): the player lays a budget bill through the manager's verb the
        /// day the budget process opens. The Budget bill family's world is forked at that moment and is the played world AS THE BILL FOUND IT - the bill left out
        /// - while the played world holds the bill; the Trade lever's world, forked earlier, lays the same bill on the same day and stays the played world; and
        /// the ledger reads a Budget bill line of its own (where before K51 the bill sat in every world and its effect on the interaction line). Read again at the
        /// next boundary.</summary>
        private static int BillsHaveTheirFamily(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.bill", Governing, null, out _);
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            SimulationRandom.Reinstate(g.Live);
            string pre = null, after = null, family = null, laidOn = "never";
            bool laid = false;
            try
            {
                Day(g);
                var noAct = (PlayerLever)LeverCtor.Invoke(new object[] { PlayerLeverKind.PartnerTariffOverride, Player, (Func<World, Country, bool, bool>)((w, c, r) => false) });
                g.Ledger.BeforeLever(g.Sim, noAct);   // the Trade family's world, forked with nothing done - a world of another family than the bill's
                for (int d = 0; d < MaxDays && !laid; d++)
                {
                    if (g.Sim.GetPendingBudgetProcess(Player) && !except.ContainsKey("Budget bill"))
                    {
                        pre = Fingerprint(g.Sim, g.World, SimulationRandom.CaptureDrawCounts());
                        laid = g.Sim.IntroduceBudgetBill(Player, new BudgetBill());
                        if (laid)
                        {
                            laidOn = Day(g.Sim.CurrentDate);
                            after = Fingerprint(g.Sim, g.World, SimulationRandom.CaptureDrawCounts());
                            family = except.TryGetValue("Budget bill", out ShadowBaseline w) ? Fingerprint(ForkSim(w), w.World, DrawsOf(w)) : "NO BUDGET BILL WORLD";
                            break;
                        }
                    }
                    Answer(g);
                    Day(g);
                }
                ShadowBaseline trade = TheFork(g);
                trade?.CatchUp(g.Sim.DaysIntoTurn);
                string tradeNow = trade == null ? "NO TRADE WORLD" : Fingerprint(ForkSim(trade), trade.World, DrawsOf(trade));
                Check(laid && family == pre && after != pre && tradeNow == after,
                    F("10. K51 - A BILL IS ITS KIND'S FAMILY: the player laid a budget bill on {0}; the Budget bill family's world is the played world as the bill found it, the bill left out{1}; the played world holds the bill{2}; the Trade lever's world laid it on the same day and is the played world{3}",
                        laidOn, family == pre ? string.Empty : (family == null ? " - NOT LAID" : FirstDifference(pre ?? string.Empty, family)), after != pre ? string.Empty : " - IT DOES NOT: the case has no teeth",
                        tradeNow == after ? string.Empty : FirstDifference(after ?? string.Empty, tradeNow)));
                while (laid && g.Sim.CurrentTurn == 0) { Answer(g); Day(g); if (g.Boundary) { break; } }
                Country me = g.World.GetCountry(Player);
                var line = new List<string>();
                bool hasLine = true;
                foreach (string stat in Stats)
                {
                    ImpactLine budget = g.Ledger.LinesFor(me, stat, out float _).FirstOrDefault(l => l.Family == "Budget bill");
                    hasLine &= budget.Family != null;
                    line.Add(stat + " " + (budget.Family != null ? R(budget.Contribution) : "none"));
                }
                Check(laid && hasLine, F("10. K51 - the ledger reads a Budget bill line of its own at {0}: {1}", Day(g.Sim.CurrentDate), string.Join(", ", line)));
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return failures;
        }

        // ------------------------------------------------------------------ 11

        /// <summary>The review's M2 - K46's guards and K51's refused bill, each with teeth: four family worlds planted on one day of a Sweden game, the same
        /// world as the played one, then (a) one marked withheld - its state alike, it must be CARRIED; (b) one marked as having answered alone - alike, it must be
        /// CARRIED; (c) one neither, its GDP moved - `StatesAlike` must catch it and CARRY it; (d) one left alone - it is the played world and is NAMED for the
        /// load to rebuild. And a budget bill laid on a day the budget process is not open - the chamber's gate refuses it - leaves no Budget bill world.</summary>
        private static int GuardsHaveTeeth(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.teeth", Governing, null, out _);
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            var withheld = (HashSet<string>)WithheldField.GetValue(g.Ledger);
            SimulationRandom.Reinstate(g.Live);
            try
            {
                Day(g);
                for (int d = 0; d < MaxDays && g.Sim.GetPendingBudgetProcess(Player); d++) { Answer(g); Day(g); }
                bool closed = !g.Sim.GetPendingBudgetProcess(Player);
                // the precondition planted (n911: the bill the player LAID while the process was open had forked its family, a fork kept by right): the
                // family set back to untouched, so the world the check looks for can only be one the refused bill left
                bool laidBefore = except.TryGetValue("Budget bill", out ShadowBaseline earlier);
                if (laidBefore) { except.Remove("Budget bill"); withheld.Remove("Budget bill"); earlier.Dispose(); }
                bool refused = closed && !g.Sim.IntroduceBudgetBill(Player, new BudgetBill());
                Check(closed && refused && !except.ContainsKey("Budget bill"),
                    F("11. K51 - A REFUSED BILL LEAVES NO FAMILY: a budget bill laid on {0} with the budget process {1} was {2}, and the ledger holds {3} Budget bill world (the family set back to untouched first: {4})",
                        Day(g.Sim.CurrentDate), closed ? "closed" : "STILL OPEN - the case has no teeth", refused ? "refused" : "ACCEPTED", except.ContainsKey("Budget bill") ? "A" : "no",
                        laidBefore ? "a bill laid while the process was open had forked it" : "it had none"));

                ShadowBaseline Plant(string family) => except[family] = new ShadowBaseline(g.Sim, g.World, Player, insideTurn: true, dayTick: true, seatAsPlayed: true) { AnswersWaitsAsTheAi = true };
                Plant("Labour bill");
                withheld.Add("Labour bill");                                                   // (a) withheld, alike
                AnsweredAloneProperty.SetValue(Plant("Law bill"), true);                       // (b) answered alone, alike
                Plant("Sector bill").World.GetCountry(Player).State.GDP += 1f;                 // (c) neither, not alike
                Plant("Trade bill");                                                           // (d) the played world
                Dictionary<string, string> carried = g.Ledger.Carry(g.Sim, out List<string> rebuilt);
                rebuilt = rebuilt ?? new List<string>();
                bool Carried(string f) => carried != null && carried.ContainsKey(f) && !rebuilt.Contains(f);
                Check(Carried("Labour bill") && Carried("Law bill") && Carried("Sector bill") && rebuilt.Count == 1 && rebuilt[0] == "Trade bill",
                    F("11. K46 - THE GUARDS: carried [{0}], named for the rebuild [{1}] - (a) a withheld world alike {2}, (b) a world that answered alone alike {3}, (c) a world neither whose state differs {4}, (d) the played world itself {5}",
                        string.Join(", ", (carried?.Keys ?? Enumerable.Empty<string>()).OrderBy(k => k, StringComparer.Ordinal)), string.Join(", ", rebuilt),
                        Carried("Labour bill") ? "carried" : "DROPPED", Carried("Law bill") ? "carried" : "DROPPED", Carried("Sector bill") ? "carried" : "DROPPED - StatesAlike missed it",
                        rebuilt.Contains("Trade bill") ? "named" : "NOT NAMED"));
            }
            finally { g.Live = SimulationRandom.CaptureLive(); }
            return failures;
        }

        // ------------------------------------------------------------------ 12

        /// <summary>The merged film real911 (K51's forks in a campaign): a Sweden game whose player's campaign is running lays a bill of a kind first, so the ledger
        /// forks that family's world in mid-campaign (the fork's own restore replays the campaign from its record); a few campaign days on, the game is saved as
        /// `GameController.SaveToPath` saves it and loaded through the game's own load. Every campaign stream must land where the save recorded it - in the fork,
        /// at the load of the played world, and at the load of every world the save carries - with no CAMPAIGN REPLAY error logged anywhere, and the loaded
        /// game's own draw counts equal to the save's.</summary>
        private static int CampaignReplaysOnALedgerGame(List<GameObject> hosts, List<Action> cleanup, StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var replayErrors = new List<string>();
            void Listen(string text, string stack, LogType type) { if ((type == LogType.Error || type == LogType.Exception) && text.StartsWith("CAMPAIGN REPLAY: stream", StringComparison.Ordinal)) { replayErrors.Add(text); } }
            Game g = OpenSweden(hosts, cleanup, "ForkAnswersDiagnostic.campaign", Governing, null, out _);
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            Game loaded = null;
            string forkedFamily = null, cutOn = "never";
            int campaignDay = -1;
            Dictionary<SimulationRandom.Stream, int> saved = null, afterLoad = null;
            Application.logMessageReceived += Listen;
            SimulationRandom.Reinstate(g.Live);
            try
            {
                for (int d = 0; d < MaxDays && !(g.Sim.PlayerCampaign != null && !g.Sim.PlayerCampaign.Finished && g.Sim.PlayerCampaign.Day >= 5); d++) { Day(g); Answer(g); }
                if (g.Sim.PlayerCampaign != null && !g.Sim.PlayerCampaign.Finished)
                {
                    // a kind the game has not forked yet, laid through its verb - the first one the gate accepts
                    var tries = new (string Family, Func<bool> Lay)[]
                    {
                        ("Labour bill", () => g.Sim.IntroduceLaborBill(Player, new LaborPolicyBill { MinimumWage = 12f })),
                        ("Crime and justice bill", () => g.Sim.IntroduceCrimeJusticeBill(Player, new CrimeJusticePolicyBill { PoliceFunding = 55f })),
                        ("Trade bill", () => g.Sim.IntroduceTradeBill(Player, new TradePolicyBill { NewBaseTariffRate = g.World.GetCountry(Player).BaseTariffRate + 2f })),
                    };
                    foreach ((string family, Func<bool> lay) in tries)
                    {
                        if (except.ContainsKey(family)) { continue; }
                        if (lay() && except.ContainsKey(family)) { forkedFamily = family; break; }
                    }
                    for (int d = 0; d < 3; d++) { Day(g); Answer(g); }
                    campaignDay = g.Sim.PlayerCampaign?.Day ?? -1;
                    cutOn = Day(g.Sim.CurrentDate);
                    SaveGame save = SaveGameService.CreateSaveGame(g.Sim, g.World, Player, null);
                    save.NoPolicyShadow = g.None.Carry(g.Sim.DaysIntoTurn);
                    save.ImpactLedgerWorlds = save.NoPolicyShadow != null ? g.Ledger.Carry(g.Sim, out List<string> rebuild) : null;
                    save.ImpactLedgerRebuild = null;
                    saved = new Dictionary<SimulationRandom.Stream, int>(save.RngDrawCounts);
                    g.Live = SimulationRandom.CaptureLive();
                    loaded = Adopt(SaveGameService.Deserialize(SaveGameService.Serialize(save)), hosts, out string why);
                    if (loaded != null)
                    {
                        SimulationRandom.LiveState was = SimulationRandom.CaptureLive();
                        try { SimulationRandom.Reinstate(loaded.Live); afterLoad = SimulationRandom.CaptureDrawCounts(); }
                        finally { SimulationRandom.Reinstate(was); }
                    }
                    else { replayErrors.Add("the game's own load refused the save - " + why); }
                }
            }
            finally
            {
                Application.logMessageReceived -= Listen;
                g.Live = SimulationRandom.CaptureLive();
                if (loaded != null) { Game l = loaded; cleanup.Add(() => { l.Ledger.Dispose(); l.None.Dispose(); }); }
            }
            int Count(Dictionary<SimulationRandom.Stream, int> counts, SimulationRandom.Stream s) => counts != null && counts.TryGetValue(s, out int n) ? n : 0;
            var streams = new[] { SimulationRandom.Stream.CampaignAi, SimulationRandom.Stream.Debate, SimulationRandom.Stream.Scandal };
            bool same = saved != null && afterLoad != null && streams.All(s => Count(saved, s) == Count(afterLoad, s));
            Check(forkedFamily != null && campaignDay > 0 && replayErrors.Count == 0 && same,
                F("12. A CAMPAIGN ON A LEDGER GAME REPLAYS: the {0} family forked on campaign day {1}; the save cut on {2} (campaign day {3}) loads through the game's own load with {4} replay error(s){5}; the campaign streams after the load {6} the save's ({7})",
                    forkedFamily ?? "NO - no bill kind was accepted, the case has no teeth", campaignDay - 3, cutOn, campaignDay, replayErrors.Count,
                    replayErrors.Count == 0 ? string.Empty : " - first: " + replayErrors[0], same ? "equal" : "DO NOT EQUAL",
                    string.Join(", ", streams.Select(s => s + " " + Count(saved, s) + "/" + Count(afterLoad, s)))));
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
            return new Game { Sim = sim, World = world, None = none, Ledger = ledger, Live = SimulationRandom.CaptureLive(), StandingAtStart = Standing(me) };
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

        /// <summary>The case's own fork, by its family's name - the Trade lever's, or the Monetary dial's (case 3). K51: since each bill kind is a family, the
        /// budget bills the player lays fork the Budget bill family beside it, so the fork is no longer the ledger's only world.</summary>
        private static ShadowBaseline TheFork(Game g)
        {
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            return except.TryGetValue("Trade", out ShadowBaseline trade) ? trade : except.TryGetValue("Monetary", out ShadowBaseline monetary) ? monetary : null;
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
