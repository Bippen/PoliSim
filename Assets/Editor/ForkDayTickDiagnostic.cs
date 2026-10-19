using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FORK-1 (Elias's ruling J8 of 2026-10-19, D-45's class; §865's M4, §866's filing): **THE IMPACT LEDGER'S FORKS STEP AS THE PLAYED WORLD DOES** - each
    /// day, then the player's day tick (`SimulationManager.AdvanceCountryDayTick`), then the levers pulled on it (`ShadowBaseline`'s <c>dayTick</c>). The
    /// done-when: a fork with no policy difference equals the played world, on a US world through a lapse and on a non-US world, and the ledger's attribution
    /// of a lapse no longer lands on a family.
    ///
    /// <para><b>A fork with no policy difference.</b> Two of the ledger's own paths, each with the policy taken out of it:</para>
    /// <list type="bullet">
    /// <item><description>a LEVER's fork (`PolicyImpactLedger.BeforeLever`, made inside the turn) for a lever whose act changes nothing - the family's
    /// except-world forked at the lever's moment, as the game forks it, with no lever in either world;</description></item>
    /// <item><description>a DIAL's fork (`PolicyImpactLedger.AdvanceTurn`, made at a boundary) for the Monetary dial, which a country whose central bank has
    /// a head never reads (`CurrencySystem.ApplyInterestRateChanges` - the ledger's own S-18 note): the played world holds the dial, the except-world not,
    /// and neither moves for it.</description></item>
    /// </list>
    /// <para>Each is stepped beside the played world in `GameController`'s order (the day, the player's tick; at the boundary the ledger's worlds, the played
    /// world, the no-policy shadow, the played world's market state) and compared after the boundary on a fingerprint of the whole game: every country's
    /// `EconomyState` floats and its zone's rate, the player's lapse, government, divisions and elections, the manager's pending budget process, bill,
    /// Speaker's round, meeting and cabinet decisions, and the counted random streams (each world's own).</para>
    ///
    /// <para><b>The teeth.</b> Each case runs twice: with the ledger (its forks run the tick), and with a CONTROL fork built as the ledger built every fork
    /// before J8 (`new ShadowBaseline(real, world, player, insideTurn)`, no tick). The control must DIFFER from the played world where the case says so -
    /// what the stepping before J8 did - and the played world must be the same in both runs (the forks beside it are passive).</para>
    ///
    /// <list type="number">
    /// <item><description>THE USA THROUGH A LAPSE: W2 of `UsBudgetProcedureDiagnostic` (opened 2025-07-29, the DEM player in opposition, the government's
    /// budget planted so it falls at cloture on 2025-08-19, withheld by the player that day), keyed as play draws the withholder; the lever's fork on
    /// 2025-08-20, compared after the turn's boundary. The lapse must open on 2025-10-01 and end inside the turn.</description></item>
    /// <item><description>SWEDEN, a lever's fork on the first day of the start's turn, when the day tick carries the Speaker's round of a chamber seated with
    /// no government - compared after the boundary.</description></item>
    /// <item><description>SWEDEN, the Monetary dial's fork at the first boundary, compared after the second; its control is reported, not required to differ
    /// (whether that year's tick moves anything is the year's).</description></item>
    /// </list>
    /// <para>⚠ <b>What it does not claim.</b> The controller's other daily acts are not in a fork: the chamber's election on the player's polling day
    /// (`GameController.CheckElection`), and every answer the player gives - a bill introduced, a withhold or a release, a meeting or a cabinet decision
    /// answered (D-45's remainder, FORK-2). The harness steps the played world by the same day path without `CheckElection`, so neither world holds the player's election: the US span holds no polling day (asserted); the Sweden spans cross 13 Sep 2026, reported, and their equality speaks for the day tick alone. No act of the player follows the fork.</para>
    /// </summary>
    public static class ForkDayTickDiagnostic
    {
        private const int Seed = 777;   // the play key (UsCongressDiagnostic.OpenUsa seeds it for the USA)
        private static readonly DateTime UsOpens = new DateTime(2025, 7, 29), UsWithholds = new DateTime(2025, 8, 19), LapseOpens = new DateTime(2025, 10, 1);
        private const float Dial = 0.25f;
        private static readonly string[] Stats = { "GDP", "NominalGdp", "GovernmentDebt", "Unemployment", "Inflation", "ApprovalRating" };

        private static readonly FieldInfo SlotField = typeof(SimulationManager).GetField("_pendingBudgetBillByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo UsedField = typeof(SimulationManager).GetField("_incomingBudgetWindowUsed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo SeatMethod = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ExceptField = typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkSimField = typeof(ShadowBaseline).GetField("_sim", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ForkOwnField = typeof(ShadowBaseline).GetField("_own", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly ConstructorInfo LeverCtor = typeof(PlayerLever).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(PlayerLeverKind), typeof(CountryId), typeof(Func<World, Country, bool, bool>) }, null);

        /// <summary>One run: the played world, the no-policy shadow, and either the ledger (its forks tick) or the control fork (none does).</summary>
        private sealed class Game
        {
            public SimulationManager Sim;
            public World World;
            public CountryId Player;
            public ShadowBaseline None;
            public PolicyImpactLedger Ledger;
            public ShadowBaseline Control;
            public bool DialOn;
            public int PollingDays;
            public bool Ticking => Ledger != null;

            public void Dispose()
            {
                Ledger?.Dispose();
                Control?.Dispose();
                None?.Dispose();
            }
        }

        /// <summary>What a case left: the played world's fingerprint, the fork's, the per-stat difference real − fork (the family's line, read through the
        /// ledger where it ran), and the case's own notes.</summary>
        private sealed class Outcome
        {
            public string Real, Fork, Family;
            public Dictionary<string, float> Line = new Dictionary<string, float>();
            public int PollingDays;
            public DateTime LapseOpened = DateTime.MinValue, LapseEnded = DateTime.MinValue;
            public DateTime RealThrough, ForkThrough;
            public float StockPeak;
            public bool Withheld = true;
            public string WithheldWhy;
            public bool HeadSetsRate = true;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ForkDayTickDiagnostic (FORK-1, J8): the impact ledger's forks step through the player's day tick as the played world does ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                bool reached = SlotField != null && UsedField != null && SeatMethod != null && ExceptField != null && ForkSimField != null && ForkOwnField != null && LeverCtor != null
                               && Stats.All(s => (typeof(EconomyState).GetField(s)?.FieldType ?? typeof(EconomyState).GetProperty(s)?.PropertyType) == typeof(float));   // NominalGdp is a property (GDP x PriceLevel)
                Check(reached, "the harness's reach: the manager's budget slot, its arrival window and its seating; the ledger's worlds; a fork's manager and generator; the lever's constructor; the stats as EconomyState floats");
                if (!reached) { throw new InvalidOperationException("a member the harness reads moved - VERIFIED NOTHING"); }

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ 1. the USA through a lapse
                        Outcome usT = RunUsa(hosts, ticking: true), usC = RunUsa(hosts, ticking: false);
                        Check(usT.Withheld && usT.PollingDays == 0 && usT.LapseOpened == LapseOpens && usT.LapseEnded != DateTime.MinValue && usT.StockPeak > 0f,
                            F("1. the USA (W2 staged as UsBudgetProcedureDiagnostic stages it, the withholder keyed on seed {0}): the DEM player withholds on {1}{2}; the played world lapses {3} to {4} (the stopped outlays peak at {5}), all inside the turn; no polling day of the USA in the span ({6})",
                                Seed, Day(UsWithholds), usT.Withheld ? string.Empty : " - REFUSED: " + usT.WithheldWhy, Day(usT.LapseOpened), Day(usT.LapseEnded), Num(usT.StockPeak), usT.PollingDays));
                        Check(usT.Real == usC.Real, "1. the played world is the same with the ledger's ticking fork beside it and with the control beside it - the forks are passive" + (usT.Real == usC.Real ? string.Empty : FirstDifference(usT.Real, usC.Real)));
                        Check(usT.Fork == usT.Real && usT.ForkThrough == usT.RealThrough,
                            F("1. the ledger's {0} fork (a lever's, forked {1}, no lever acted) EQUALS the played world after the boundary - what the lapse left (funded through {2} in both), its book and output, the resolutions, every country's state and the streams{3}",
                                usT.Family, Day(UsWithholds.AddDays(1)), Day(usT.ForkThrough), usT.Fork == usT.Real ? string.Empty : FirstDifference(usT.Real, usT.Fork)));
                        Check(usT.Line.Values.All(v => v == 0f),
                            F("1. the lapse lands on no family: the ledger's {0} line is {1}", usT.Family, Lines(usT.Line)));
                        Check(usC.Fork != usC.Real && usC.ForkThrough != usC.RealThrough && usC.Line.Values.Any(v => v != 0f),
                            F("1. the control (a fork stepped as before J8, no tick) is funded through {0} against the played world's {1} - no resolution, no lapse - and differs from the played world{2}; its line, the lapse booked to the family: {3}",
                                Day(usC.ForkThrough), Day(usC.RealThrough), usC.Fork != usC.Real ? FirstDifference(usC.Real, usC.Fork) : " - IT DOES NOT: the case has no teeth", Lines(usC.Line)));

                        // ============================================================ 2. Sweden, a lever's fork on the start's first day
                        Outcome seT = RunSweden(hosts, ticking: true, dial: false), seC = RunSweden(hosts, ticking: false, dial: false);
                        sb.Append(F("    report    2. Sweden from its start ({0}), the player S: {1} polling day(s) of Sweden in the span - neither world holds the election (the harness steps `GameController.Update`'s day path without its `CheckElection`; the player's election in a fork is FORK-2's), so the equality below speaks for the day tick alone\n", Day(WorldClock.StartDate(CountryId.Sweden)), seT.PollingDays));
                        Check(seT.Real == seC.Real, "2. the played world is the same with the ledger's ticking fork beside it and with the control beside it" + (seT.Real == seC.Real ? string.Empty : FirstDifference(seT.Real, seC.Real)));
                        Check(seT.Fork == seT.Real, F("2. the ledger's {0} fork (a lever's, forked after the first day, no lever acted) EQUALS the played world after the first boundary{1}; its line {2}",
                            seT.Family, seT.Fork == seT.Real ? string.Empty : FirstDifference(seT.Real, seT.Fork), Lines(seT.Line)));
                        Check(seT.Line.Values.All(v => v == 0f), F("2. Sweden's {0} line is zero on every stat read", seT.Family));
                        Check(seC.Fork != seC.Real, F("2. the control (no tick) differs from the played world{0}; its line {1}",
                            seC.Fork != seC.Real ? FirstDifference(seC.Real, seC.Fork) : " - IT DOES NOT: the case has no teeth", Lines(seC.Line)));

                        // ============================================================ 3. Sweden, the Monetary dial's fork at the first boundary
                        Outcome dT = RunSweden(hosts, ticking: true, dial: true), dC = RunSweden(hosts, ticking: false, dial: true);
                        Check(dT.HeadSetsRate, "3. Sweden's central bank has a head, so the Monetary dial is never read (the no-policy-difference premise)" + (dT.HeadSetsRate ? string.Empty : " - NO HEAD: the dial is a policy difference here"));
                        Check(dT.Real == dC.Real, F("3. two turns ({0} polling day(s) of Sweden, held by neither world - FORK-2's); the played world the same beside either fork{1}", dT.PollingDays, dT.Real == dC.Real ? string.Empty : FirstDifference(dT.Real, dC.Real)));
                        Check(dT.Fork == dT.Real && dT.Line.Values.All(v => v == 0f),
                            F("3. the ledger's {0} fork (a dial's, forked at the first boundary by the ledger's AdvanceTurn) EQUALS the played world after the second{1}; its line {2}",
                                dT.Family, dT.Fork == dT.Real ? string.Empty : FirstDifference(dT.Real, dT.Fork), Lines(dT.Line)));
                        sb.Append(F("    report    3. the control (no tick) over the second turn {0}{1}; its line {2}\n",
                            dC.Fork == dC.Real ? "equals the played world - that year's tick moved nothing the fingerprint reads" : "differs from the played world", dC.Fork == dC.Real ? string.Empty : FirstDifference(dC.Real, dC.Fork), Lines(dC.Line)));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
                UsLapseDraw.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== ForkDayTickDiagnostic: ALL ASSERTIONS PASS - a fork with no policy difference equals the played world, and a lapse lands on no family ===\n"
                                    : F("=== ForkDayTickDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        // ------------------------------------------------------------------ the cases

        private static Outcome RunUsa(List<GameObject> hosts, bool ticking)
        {
            var o = new Outcome();
            (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, UsOpens, "DEM");
            ((HashSet<CountryId>)UsedField.GetValue(s)).Add(CountryId.USA);   // the arrival window spent (W2's staging: the run starts mid-term)
            var bill = new BudgetBill { GovernmentBill = true, DaysRemaining = ParliamentSystem.BillDurationDays };
            bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
            ((Dictionary<CountryId, BudgetBill>)SlotField.GetValue(s))[CountryId.USA] = bill;   // the government's bill, planted so its day is 2025-08-19
            Game g = NewGame(s, w, CountryId.USA, ticking);
            try
            {
                DateTime lapseWas = DateTime.MinValue;
                void Watch()
                {
                    if (u.LapseSince != DateTime.MinValue && o.LapseOpened == DateTime.MinValue) { o.LapseOpened = u.LapseSince; }
                    if (lapseWas != DateTime.MinValue && u.LapseSince == DateTime.MinValue && o.LapseEnded == DateTime.MinValue) { o.LapseEnded = s.CurrentDate; }
                    lapseWas = u.LapseSince;
                    o.StockPeak = Mathf.Max(o.StockPeak, u.LapseDeferredOutlays);
                }

                while (s.CurrentDate < UsWithholds) { Day(g); Watch(); }
                o.Withheld = s.WithholdContinuingResolution(CountryId.USA, true, out o.WithheldWhy);   // after the day's tick, as W2 withholds
                Day(g); Watch();
                ForkAtLever(g);
                while (s.CurrentTurn == 0) { Day(g); Watch(); }   // a fork advances only at the boundary: it is read there, its lapse in what the lapse left
                Read(g, o);
                o.RealThrough = u.AppropriatedThrough;
                o.ForkThrough = TheFork(g)?.CountryFor(CountryId.USA)?.AppropriatedThrough ?? DateTime.MinValue;
            }
            finally { g.Dispose(); }
            return o;
        }

        private static Outcome RunSweden(List<GameObject> hosts, bool ticking, bool dial)
        {
            var o = new Outcome();
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            WorldClock.ApplyStart(CountryId.Sweden);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("ForkDayTickDiagnostic.sweden"); hosts.Add(go);
            SimulationManager s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.Sweden;
            w.GetCountry(CountryId.Sweden).PlayerPartyAbbrev = "S";
            SeatMethod.Invoke(s, null);
            o.HeadSetsRate = w.GetCountry(CountryId.Sweden).CurrentFedChair != null;
            Game g = NewGame(s, w, CountryId.Sweden, ticking);
            try
            {
                if (dial) { g.DialOn = true; }   // the dial in every boundary's decision: the ledger forks its family at the first (the played world holds it, the fork not)
                else
                {
                    Day(g);
                    ForkAtLever(g);
                }
                while (s.CurrentTurn < (dial ? 2 : 1)) { Day(g); }
                Read(g, o);
            }
            finally { g.Dispose(); }
            return o;
        }

        // ------------------------------------------------------------------ the game's order

        private static Game NewGame(SimulationManager s, World w, CountryId player, bool ticking)
        {
            // the game builds the no-policy shadow beside a new world (GameController.Start), in both runs, so the played world's sequence is one
            var g = new Game { Sim = s, World = w, Player = player, None = new ShadowBaseline(SimulationRandom.MasterSeed) };
            if (ticking) { g.Ledger = new PolicyImpactLedger(g.None); }
            return g;
        }

        /// <summary>One day in `GameController.Update`'s order: the day, the player's day tick, and on the boundary `GameController.AdvanceTurn`'s - the
        /// ledger's worlds (or the control fork), the played world, the no-policy shadow, the played world's market state.</summary>
        private static void Day(Game g)
        {
            bool boundary = g.Sim.AdvanceDay();
            g.Sim.AdvanceCountryDayTick(g.Player);
            if (g.Sim.PollingDayToday) { g.PollingDays++; }
            if (!boundary) { return; }

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in g.World.Countries) { decisions[c.Id] = PolicyDecision.None(); }
            var stripped = new Dictionary<CountryId, PolicyDecision>(decisions);
            if (g.DialOn) { decisions[g.Player] = new PolicyDecision { InterestRateChange = Dial }; }

            if (g.Ticking) { g.Ledger.AdvanceTurn(g.Sim, g.World, g.Player, decisions); }
            else
            {
                if (g.DialOn && g.Control == null) { g.Control = new ShadowBaseline(g.Sim, g.World, g.Player); }   // a dial's fork at its first boundary, as the ledger made it before J8
                g.Control?.AdvanceTurn(stripped);
            }
            g.Sim.AdvanceTurn(decisions);
            g.None.AdvanceTurn();
            EnergyMarket.BeginTurn(g.World);
        }

        /// <summary>A lever's moment with no lever: the ledger's own fork for the family (`BeforeLever`), or the control built as it was before J8.</summary>
        private static void ForkAtLever(Game g)
        {
            if (g.Ticking)
            {
                var noAct = (PlayerLever)LeverCtor.Invoke(new object[] { PlayerLeverKind.PartnerTariffOverride, g.Player, (Func<World, Country, bool, bool>)((world, country, replay) => false) });
                g.Ledger.BeforeLever(g.Sim, noAct);
            }
            else { g.Control = new ShadowBaseline(g.Sim, g.World, g.Player, insideTurn: true); }
        }

        /// <summary>The run's one fork: the ledger's only except-world, or the control.</summary>
        private static ShadowBaseline TheFork(Game g)
        {
            if (!g.Ticking) { return g.Control; }
            var except = (Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger);
            return except.Count == 1 ? except.Values.First() : null;
        }

        private static void Read(Game g, Outcome o)
        {
            o.PollingDays = g.PollingDays;
            o.Real = Fingerprint(g.Sim, g.World, g.Player, SimulationRandom.CaptureDrawCounts());
            ShadowBaseline fork = TheFork(g);
            if (fork == null)
            {
                int count = g.Ticking ? ((Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger)).Count : 0;
                o.Fork = F("NO FORK - the run holds {0} world(s), not one", count);
                return;
            }

            if (g.Ticking) { o.Family = ((Dictionary<string, ShadowBaseline>)ExceptField.GetValue(g.Ledger)).Keys.First(); }
            var forkSim = (SimulationManager)ForkSimField.GetValue(fork);
            var own = (SimulationRandom.LiveState)ForkOwnField.GetValue(fork);
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            Dictionary<SimulationRandom.Stream, int> forkDraws;
            try { SimulationRandom.Reinstate(own); forkDraws = SimulationRandom.CaptureDrawCounts(); }
            finally { SimulationRandom.Reinstate(real); }
            o.Fork = Fingerprint(forkSim, fork.World, g.Player, forkDraws);

            Country realCountry = g.World.GetCountry(g.Player), forkCountry = fork.CountryFor(g.Player);
            foreach (string stat in Stats)
            {
                if (g.Ticking)
                {
                    List<ImpactLine> lines = g.Ledger.LinesFor(realCountry, stat, out float _);
                    o.Line[stat] = lines.First(l => l.Family == o.Family).Contribution;
                }
                else { o.Line[stat] = Stat(realCountry, stat) - Stat(forkCountry, stat); }   // what the family's line carried, real − except, before J8
            }
        }

        // ------------------------------------------------------------------ the fingerprint

        private static string Fingerprint(SimulationManager sim, World world, CountryId player, Dictionary<SimulationRandom.Stream, int> draws)
        {
            var sb = new StringBuilder();
            sb.Append("clock ").Append(Day(sim.CurrentDate)).Append(" turn ").Append(sim.CurrentTurn).Append('\n');
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=').Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }
                sb.Append("rate=").Append((c.CurrencyZone?.InterestRate ?? 0f).ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            }

            Country p = world.GetCountry(player);
            sb.Append("lapse|since=").Append(Day(p.LapseSince)).Append("|through=").Append(Day(p.AppropriatedThrough)).Append("|flow=").Append(Day(p.LapseFlowDay))
              .Append("|deferred=").Append(p.LapseDeferredOutlays.ToString("R", CultureInfo.InvariantCulture)).Append("|wedge=").Append(p.LapseOutputWedge.ToString("R", CultureInfo.InvariantCulture))
              .Append("|days=").Append(p.LapseOutputDays).Append("|quarter=").Append(Day(p.LapseProfileQuarter)).Append("|withheld=").Append(p.ContinuingResolutionWithheldBy ?? "none").Append('\n');
            GovernmentRecord gov = p.Government;
            sb.Append("government|").Append(gov == null ? "none" : F("{0}|{1}|{2}|{3}|{4}", gov.PmParty, string.Join(",", gov.Cabinet), string.Join(",", gov.Support), Day(gov.FormedOn), gov.Outcome)).Append('\n');
            DivisionRecord last = p.Divisions.Entries.LastOrDefault();
            sb.Append("divisions|").Append(p.Divisions.Entries.Count).Append('|').Append(last == null ? "none" : F("{0}|{1}|{2}|{3}", last.Number, Day(last.Date), last.Title, last.Passed)).Append('\n');
            sb.Append("elections|").Append(p.ElectionHistory.Count).Append('\n');
            BudgetBill bill = sim.GetPendingBudgetBill(player);
            SpeakerRound round = sim.RoundOf(player);
            sb.Append("pending|process=").Append(sim.GetPendingBudgetProcess(player)).Append("|bill=").Append(bill == null ? "none" : F("{0}{1}{2}", bill.DaysRemaining, bill.ContinuingResolution ? " CR" : string.Empty, bill.GovernmentBill ? " gov" : string.Empty))
              .Append("|round=").Append(round == null ? "none" : round.Stage.ToString()).Append("|meeting=").Append(sim.GetPendingForeignPolicyMeeting(player) != null)
              .Append("|cabinet=").Append(sim.GetPendingCabinetDecisions(player).Count).Append('\n');
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
                string head = x[i].Length > 0 && x[i].Contains(':') ? x[i].Substring(0, x[i].IndexOf(':')) + " " : string.Empty;
                for (int j = 0; j < Math.Min(p.Length, q.Length); j++) { if (p[j] != q[j]) { return F(" - first at {0}{1} against {2}", head, p[j], q[j]); } }
                return F(" - first at line {0}", i);
            }
            return " - the lengths differ";
        }

        private static float Stat(Country c, string stat) => c == null ? float.NaN
            : (float)(typeof(EconomyState).GetField(stat)?.GetValue(c.State) ?? typeof(EconomyState).GetProperty(stat).GetValue(c.State));
        private static string Lines(Dictionary<string, float> line) => string.Join(", ", line.Select(kv => kv.Key + " " + kv.Value.ToString("R", CultureInfo.InvariantCulture)));
        private static string Day(DateTime d) => d == DateTime.MinValue ? "never" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Num(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
