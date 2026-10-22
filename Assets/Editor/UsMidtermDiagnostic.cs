using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-27 (`docs/specs/USA_STAGE_PLAN.md` US-27, "Midterms"; R-US20 RULED (b), Elias's ruling I4): THE MIDTERM DIAGNOSTIC. The plan's done-when: "a US
    /// world from 12 Mar 2024 holds 3 Nov 2026 and seats the game's 120th on 3 Jan 2027, its swing printed against US-13's record".
    /// <list type="bullet">
    /// <item>(a) the calendar: 2 U.S.C. §7's day every even year - the USA's polling days from its start are 5 Nov 2024, 3 Nov 2026, 7 Nov 2028 and 5 Nov 2030,
    /// the presidential ones 2024 and 2028 (<see cref="UsPresidentialElection.TryNextPresidentialDay"/>), 2026 and 2030 the midterms; the 2026 reference is
    /// BILLED (no reference of record for 3 Nov 2026 until the Clerk's 2026 statistics are saved), never estimated;</item>
    /// <item>(b) the world: the USA's start (12 Mar 2024, seed 777, the USA the player's country, DEM - so the AI governs the president's party through the term
    /// the midterm scores), stepped day by day in the controller's order (the day, the player's day tick, the turn at its boundary, the player's election where
    /// the day is the polling day) to 3 Jan 2027: the polling-day flag raised on 5 Nov 2024 and 3 Nov 2026 and no other day; one presidential count (5 Nov 2024)
    /// and two Congress counts; no run-up and no campaign open for the midterm (DECLARED, the conservative reading); the midterm counted with no president, the
    /// fitted term on the party of the president governing that day, swung from the game's own 2024 House, the Senate's class 2; the game's 120th Congress
    /// seated on 3 Jan 2027 - the House the midterm elected;</item>
    /// <item>(c) the swing printed against US-13's record: the president's party's two-party change from the game's 2024 count to its midterm, with the term and
    /// without it, beside the record's band (the Clerk's midterms 2002-2022), the fit's interval and the SECONDARY range since 1946; the seat change beside the
    /// record's seat changes;</item>
    /// <item>(d) FORK-2: a world restored from a save cut on the evening of 2 Nov 2026 (the snapshot a fork is made of, `ShadowBaseline`) holds the same midterm
    /// on its own day loop - the same count, line for line - and a world restored from a save cut on 3 Nov after the count holds it once;</item>
    /// <item>(e) THE TEETH, planted: a presidential day refused by the midterm count, a midterm refused by the presidential count, the term's sign on each party,
    /// and the term reaching the count (the same day's House counted without it differs).</item>
    /// </list>
    /// Opens its worlds under <see cref="SimulationManager.EpochScope"/> and restores the random streams and the regional readout. Writes nothing.
    /// Prints `US MIDTERM: PASS` and exits 0 when every check holds.
    /// </summary>
    public static class UsMidtermDiagnostic
    {
        /// <summary>CONVENTION: the master seed every US instrument opens on.</summary>
        private const int Seed = 777;
        /// <summary>CONVENTION: the player's party - DEM, so the AI governs the president's party (R-US20's world reads the same).</summary>
        private const string PlayerParty = "DEM";
        private static readonly DateTime Presidential2024 = new DateTime(2024, 11, 5);
        /// <summary>2 U.S.C. §7 ([USC-7]): the Tuesday next after the first Monday in November of 2026.</summary>
        private static readonly DateTime Midterm2026 = new DateTime(2026, 11, 3);
        /// <summary>The Twentieth Amendment §2: the 3rd of January after it.</summary>
        private static readonly DateTime Seated2027 = new DateTime(2027, 1, 3);
        /// <summary>The Congress the 2024 election chose ([HH-DIV]: the 119th); the midterm chooses the next.</summary>
        private const int CongressOf2024 = 119;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsMidtermDiagnostic (PS-6 US-27): the midterm on 2 U.S.C. §7's day, R-US20 (b)'s fitted term, the game's own Congress seated ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            double[][] regionalShares = NationalElection.LastRegionalShares;
            string[] regionalNames = NationalElection.LastRegionalNames;
            double[] regionalWeights = NationalElection.LastRegionalWeights;
            double regionalWorst = NationalElection.LastRegionalWorstAbsError;
            IReadOnlyList<string> regionalKeys = NationalElection.LastRegionalKeys;
            var clock = Stopwatch.StartNew();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Calendar(sb, Check);
                    Teeth(sb, Check);
                    World(sb, Check, hosts);
                }
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n");
            }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
                void Set(string property, object value) =>
                    typeof(NationalElection).GetProperty(property, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });
                Set(nameof(NationalElection.LastRegionalShares), regionalShares);
                Set(nameof(NationalElection.LastRegionalNames), regionalNames);
                Set(nameof(NationalElection.LastRegionalWeights), regionalWeights);
                Set(nameof(NationalElection.LastRegionalWorstAbsError), regionalWorst);
                Set(nameof(NationalElection.LastRegionalKeys), regionalKeys);
            }
            sb.Append(F("    ({0} ms)\n", clock.ElapsedMilliseconds));
            if (failures > 0)
            {
                Debug.LogError($"US MIDTERM: FAIL - {failures} failure(s).\n{sb}");
                CheckExit.Finish(1);
                return;
            }
            sb.Append("US MIDTERM: PASS - the world holds 3 Nov 2026 as a midterm and seats the game's 120th Congress on 3 Jan 2027");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        // ------------------------------------------------------------------ (a) the calendar

        private static void Calendar(StringBuilder sb, Action<bool, string> Check)
        {
            var days = new List<DateTime>();
            for (DateTime from = WorldClock.StartDate(CountryId.USA); days.Count < 4 && WorldClock.TryNextPollingDay(CountryId.USA, from, out DateTime d); from = d.AddDays(1)) { days.Add(d); }
            bool every = days.SequenceEqual(new[] { new DateTime(2024, 11, 5), Midterm2026, new DateTime(2028, 11, 7), new DateTime(2030, 11, 5) });
            Check(every && days.All(d => UsPresidentialElection.PresidentialDay(d.Year) == d),
                F("(a) the USA's polling days from its start {0}: {1} - 2 U.S.C. §7's Tuesday of every even year", Day(WorldClock.StartDate(CountryId.USA)), string.Join(", ", days.Select(Day))));
            Check(!UsCongressElection.IsMidterm(days[0]) && UsCongressElection.IsMidterm(days[1]) && !UsCongressElection.IsMidterm(days[2]) && UsCongressElection.IsMidterm(days[3])
                  && UsPresidentialElection.IsPresidentialYear(2024) && !UsPresidentialElection.IsPresidentialYear(2026) && UsPresidentialElection.IsPresidentialYear(2028),
                "(a) 2024 and 2028 elect a president (3 U.S.C. §21(1), every fourth year from the election of record); 2026 and 2030 are the midterms");
            Check(UsPresidentialElection.TryNextPresidentialDay(CountryId.USA, new DateTime(2024, 11, 6), out DateTime p) && p == new DateTime(2028, 11, 7)
                  && UsPresidentialElection.TryNextPresidentialDay(CountryId.USA, Midterm2026, out DateTime q) && q == new DateTime(2028, 11, 7),
                F("(a) the next presidential day after 5 Nov 2024 - and from the midterm itself - is {0}", Day(p)));
            Check(UsCongressElection.ClassUp(2026) == 2, F("(a) the class whose terms expire in 2027 is class {0} (the catalog's 2020 races, Art. I §3 cl. 2)", UsCongressElection.ClassUp(2026)));
            Check(!WorldClock.TryReference(CountryId.USA, Midterm2026, out _),
                "(a) no reference of record for 3 Nov 2026 - BILLED until the Clerk's 2026 statistics are saved (expected 2027), never estimated");
            Check(WorldClock.PollingDayBasis(CountryId.USA).Contains("[USC-7]") && WorldClock.PollingDayBasis(CountryId.USA).Contains("every even numbered year"),
                "(a) the USA's basis quotes 2 U.S.C. §7 [USC-7]");
        }

        // ------------------------------------------------------------------ (e) the teeth

        private static void Teeth(StringBuilder sb, Action<bool, string> Check)
        {
            double t = UsCongressElection.MidtermTermPoints;
            Check(UsCongressElection.WithMidtermTerm(0.5, "REP", t) == 0.5 + t / 100.0 && UsCongressElection.WithMidtermTerm(0.5, "DEM", t) == 0.5 - t / 100.0 && UsCongressElection.WithMidtermTerm(0.5, null, t) == 0.5,
                F("(e) planted: the term {0:F4} pp moves REP's share down under a REP president, up under a DEM one, and nothing with no president's party", t));
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            WorldClock.ApplyStart(CountryId.USA);
            Country usa = WorldFactory.CreateDefault().GetCountry(CountryId.USA);
            var poll = new Dictionary<string, double> { { "REP", 0.48 }, { "DEM", 0.47 }, { "LIB", 0.05 } };
            Check(UsCongressElection.CountMidterm(usa, Presidential2024, poll, 0, out string refused) == null && refused != null,
                F("(e) planted: the midterm count refuses a presidential day ({0})", refused));
            Check(!UsPresidentialElection.IsPollingDay(usa, Midterm2026) && UsCongressElection.IsPollingDay(usa, Midterm2026),
                "(e) planted: on 3 Nov 2026 the presidential count has no polling day and the Congress count has one");
            double[] national = UsCongressElection.MidtermNational(poll, 0.5);
            Check(national != null && Math.Abs(national[0] - 0.475) < 1e-12 && Math.Abs(national[1] - 0.475) < 1e-12 && Math.Abs(national[2] - 0.05) < 1e-12,
                F("(e) planted: the midterm's national vote keeps the others' share and splits the two parties' at V ({0})", national == null ? "none" : string.Join(" ", national.Select(v => v.ToString("0.000", CultureInfo.InvariantCulture)))));
        }

        // ------------------------------------------------------------------ (b), (c), (d) the world

        private static void World(StringBuilder sb, Action<bool, string> Check, List<GameObject> hosts)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            WorldClock.ApplyStart(CountryId.USA);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsMidtermDiagnostic"); hosts.Add(go);
            var sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(w);
            sim.PlayerCountryId = CountryId.USA;
            Country usa = w.GetCountry(CountryId.USA);
            usa.PlayerPartyAbbrev = PlayerParty;
            typeof(SimulationManager).GetMethod("SeatTheSecondChamberOfRecord", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(sim, null);

            var flagged = new List<DateTime>();
            bool campaignForMidterm = false;
            Dictionary<string, int> houseOn2025 = null;
            string partyOnMidterm = null;
            string json2Nov = null;
            bool recordOk = false;
            double recordEconomy = double.NaN, recordApproval = double.NaN, recordFiscal = double.NaN, recordTotal = double.NaN;
            for (int d = 0; d < 1200 && sim.CurrentDate < Seated2027; d++)
            {
                Step(sim, w);
                if (sim.PollingDayToday) { flagged.Add(sim.CurrentDate); }
                if (sim.CurrentDate > new DateTime(2024, 11, 6) && sim.CurrentDate <= Midterm2026 && (sim.PlayerCampaign != null || sim.PlayerPreCampaign != null)) { campaignForMidterm = true; }
                if (sim.CurrentDate == new DateTime(2025, 1, 3)) { houseOn2025 = new Dictionary<string, int>(usa.ParliamentSeats); }
                if (sim.CurrentDate == Midterm2026.AddDays(-1))
                {
                    partyOnMidterm = UsCongressElection.PresidentsPartyOn(usa);
                    json2Nov = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                }
                // the second review's M1: the record's components in the midterm's poll, read on the polling day itself (the history before the day, so the
                // count just held is not in it) - measurement only
                if (sim.CurrentDate == Midterm2026) { recordOk = UsCongressElection.TryRecordInV(usa, Midterm2026, out recordEconomy, out recordApproval, out recordFiscal, out recordTotal); }
            }
            Check(sim.CurrentDate == Seated2027, F("(b) the USA's start {0} (seed {1}, the USA the player's country, {2}) stepped to {3}", Day(WorldClock.StartDate(CountryId.USA)), Seed, PlayerParty, Day(sim.CurrentDate)));
            Check(flagged.SequenceEqual(new[] { Presidential2024, Midterm2026 }), F("(b) the polling-day flag was raised on {0} - the presidential day and the midterm, no other day", string.Join(", ", flagged.Select(Day))));
            Check(usa.UsPresidentialElections.Count == 1 && usa.UsPresidentialElections[0].PollingDay == Presidential2024, F("(b) one presidential count, of {0} - none at the midterm", usa.UsPresidentialElections.Count > 0 ? Day(usa.UsPresidentialElections[0].PollingDay) : "NONE"));
            Check(!campaignForMidterm, "(b) no run-up and no campaign opened for the midterm (DECLARED: the campaign staged is the presidential one)");
            UsCongressElection.Contest first = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == Presidential2024);
            UsCongressElection.Contest midterm = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == Midterm2026);
            Check(usa.UsCongressElections.Count == 2 && first != null && midterm != null, F("(b) two Congress counts - {0}", string.Join("; ", usa.UsCongressElections.Select(c => Day(c.PollingDay)))));
            if (first == null || midterm == null) { return; }
            sb.Append("      the 2024 count: " + first.Line + "\n      the midterm:    " + midterm.Line + "\n");
            Check(partyOnMidterm != null && midterm.PresidentsParty == partyOnMidterm && midterm.TermPoints == UsMidtermTerm.MeanPoints
                  && midterm.V == UsCongressElection.WithMidtermTerm(midterm.PollV, midterm.PresidentsParty, midterm.TermPoints) && midterm.V != midterm.PollV,
                F("(b) the midterm's V {0:F4} is the poll's {1:F4} with the fitted term {2:F4} pp on the president's party {3} (FITTED, n {4}, {5}-{6}, 95 % interval {7:F2} to {8:F2})",
                    midterm.V, midterm.PollV, midterm.TermPoints, midterm.PresidentsParty, UsMidtermTerm.N, UsMidtermTerm.From, UsMidtermTerm.To, UsMidtermTerm.LowerPoints, UsMidtermTerm.UpperPoints));
            Check(!midterm.BaseOfRecord && midterm.BaseYear == 2024 && midterm.VBase == first.V && midterm.House.Count == first.House.Count,
                F("(b) the midterm House swung from the game's own 2024 count (V(B) {0:F4}, base {1}, {2})", midterm.VBase, midterm.BaseYear, midterm.BaseOfRecord ? "of record" : "the game's own"));
            Check(midterm.SenateClass == 2 && midterm.Senate.Count > 0 && midterm.Senate.All(s => s.Class == 2 && s.Term == "full") && midterm.Convenes == Seated2027,
                F("(b) the Senate's class 2 elected - {0} seats, REP {1}, DEM {2}, no special of record in the catalog - seated {3}", midterm.Senate.Count, midterm.SenateOf("REP"), midterm.SenateOf("DEM"), Day(midterm.Convenes)));
            Dictionary<string, int> seated = midterm.HouseSeats(CountryId.USA);
            bool houseSeated = seated.All(kv => usa.ParliamentSeats.TryGetValue(kv.Key, out int n) && n == kv.Value);
            Check(houseSeated && UsCongressElection.TryInOffice(usa, Seated2027, out UsCongressElection.Contest sitting) && ReferenceEquals(sitting, midterm),
                F("(b) on {0} the game's {1}th Congress sits - the House the midterm elected, REP {2}, DEM {3}", Day(Seated2027), CongressOf2024 + 1, midterm.HouseRep, midterm.HouseDem));

            // (c) the swing against US-13's record
            string pp = midterm.PresidentsParty;
            double Share(double v) => pp == "REP" ? v : 1.0 - v;
            double swing = 100.0 * (Share(midterm.V) - Share(first.V)), swingNoTerm = 100.0 * (Share(midterm.PollV) - Share(first.V));
            int seatsBefore = houseOn2025 != null && houseOn2025.TryGetValue(pp, out int sb0) ? sb0 : -1;
            int seatsAfter = pp == "REP" ? midterm.HouseRep : midterm.HouseDem;
            List<UsMidtermFitCheck.Row> record = UsMidtermFitCheck.Midterms(UsMidtermFitCheck.ReadRecord(out string _));
            List<double> clerk = record.Where(r => !double.IsNaN(r.Clerk)).Select(r => r.Clerk).ToList();
            double L = clerk.Min(), U = clerk.Max();
            double secLo = record.Min(r => r.Secondary), secHi = record.Max(r => r.Secondary);
            int seatLo = record.Min(r => r.SeatChange), seatHi = record.Max(r => r.SeatChange);
            sb.Append(F("    (c) THE MIDTERM'S SWING: the president's party {0}, {1:+0.00;-0.00} points of the two-party House vote from the game's 2024 count ({2:+0.00;-0.00} without the term); "
                + "seats {3} -> {4} ({5:+0;-0})\n", pp, swing, swingNoTerm, seatsBefore, seatsAfter, seatsAfter - seatsBefore));
            sb.Append(F("        beside US-13's record (docs/generated/US_MIDTERM_RECORD.md): R-US20's band, the Clerk's midterms 2002-2022, [{0:+0.00;-0.00}, {1:+0.00;-0.00}]; the fit, the mean {2:+0.00;-0.00} (95 % interval {3:+0.00;-0.00} to {4:+0.00;-0.00}, n {5}); "
                + "SECONDARY 1946-2022 {6:+0.0;-0.0} to {7:+0.0;-0.0}; seat changes 1946-2022 {8:+0;-0} to {9:+0;-0} (mean {10:F2})\n",
                L, U, UsMidtermTerm.MeanPoints, UsMidtermTerm.LowerPoints, UsMidtermTerm.UpperPoints, UsMidtermTerm.N, secLo, secHi, seatLo, seatHi, UsMidtermTerm.SeatChangeMean));
            // the second review's M1 (I4's revisit clause): the record's components in the poll that day, in points of V and on the president's party, beside the
            // fitted term - the overlap measured, no figure decided on it
            double OnPresident(double inV) => pp == "REP" ? inV : -inV;
            sb.Append(F("        the record in the midterm's poll on the president's party, in points: economy {0:+0.0000;-0.0000}, approval {1:+0.0000;-0.0000}, fiscal {2:+0.0000;-0.0000}, together {3:+0.0000;-0.0000}; "
                + "the fitted term {4:+0.0000;-0.0000} (the record's unconditional mean swing, which already holds the record's own approval and economy) - the overlap I4's revisit clause governs\n",
                OnPresident(recordEconomy), OnPresident(recordApproval), OnPresident(recordFiscal), OnPresident(recordTotal), UsMidtermTerm.MeanPoints));
            Check(recordOk && !double.IsNaN(recordTotal), "(c) the record's components in the midterm's poll were read on the polling day (measurement only; I4's revisit is asked)");
            Check(seatsBefore >= 0 && Math.Abs(swing - swingNoTerm - UsMidtermTerm.MeanPoints) < 1e-9,
                F("(c) the term moves the president's party's swing by exactly the fitted {0:F4} pp; the 2026 result of record BILLED beside it", UsMidtermTerm.MeanPoints));
            List<UsCongressElection.StateHouse> without = UsCongressElection.CountHouse(Midterm2026, midterm.PollV, first, out int _, out double _, out bool _, out string _);
            int withoutPresident = without == null ? -1 : without.Sum(s => pp == "REP" ? s.Rep : s.Dem);
            Check(without != null && withoutPresident > seatsAfter,
                F("(e) planted: the same day's House counted without the term seats the president's party {0}, the midterm {1} - the term reaches the seats", withoutPresident, seatsAfter));

            // (d) FORK-2: a world restored from the evening of 2 Nov 2026 holds the same midterm on its own day loop; one restored after it holds it once
            SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
            try
            {
                var forkGo = new GameObject("UsMidtermDiagnostic (restored)"); hosts.Add(forkGo);
                var fork = forkGo.AddComponent<SimulationManager>();
                fork.IsCounterfactual = true;
                SaveGameService.RestoreInto(fork, SaveGameService.Deserialize(json2Nov), true);
                fork.RecomputePollingDayToday();
                Step(fork, fork.World);
                Country forked = fork.World.GetCountry(CountryId.USA);
                UsCongressElection.Contest forkMidterm = forked.UsCongressElections.FirstOrDefault(c => c.PollingDay == Midterm2026);
                Check(fork.CurrentDate == Midterm2026 && forkMidterm != null && forkMidterm.Line == midterm.Line && forked.UsPresidentialElections.Count == 1,
                    F("(d) FORK-2: a world restored from the save of 2 Nov 2026 holds the midterm on its own day loop, line for line: {0}", forkMidterm?.Line ?? "NO MIDTERM COUNT"));
                string jsonAfter = SaveGameService.Serialize(SaveGameService.CreateSaveGame(fork, fork.World, CountryId.USA, null));
                var againGo = new GameObject("UsMidtermDiagnostic (restored after)"); hosts.Add(againGo);
                var again = againGo.AddComponent<SimulationManager>();
                again.IsCounterfactual = true;
                SaveGameService.RestoreInto(again, SaveGameService.Deserialize(jsonAfter), true);
                again.RecomputePollingDayToday();
                again.HoldPlayerElection();
                Country twice = again.World.GetCountry(CountryId.USA);
                // the second review's L1: the hold itself, asked on the restored world on 3 Nov - it must refuse, the day counted
                var planted = new Dictionary<string, double> { { "REP", 0.48 }, { "DEM", 0.47 }, { "LIB", 0.05 } };
                UsCongressElection.Contest recount = UsCongressElection.HoldMidterm(twice, Midterm2026, planted, UsPresidentialElection.TieSeedOf(Seed, 2026), out string recountWhy);
                Check(again.CurrentDate == Midterm2026 && recount == null && recountWhy != null && recountWhy.Contains("still to count") && twice.UsCongressElections.Count(c => c.PollingDay == Midterm2026) == 1,
                    F("(d) on the restored world of 3 Nov the midterm hold refuses a second count: \"{0}\"", recountWhy));
                Step(again, again.World);
                Check(twice.UsCongressElections.Count(c => c.PollingDay == Midterm2026) == 1 && twice.UsCongressElections.First(c => c.PollingDay == Midterm2026).TermPoints == midterm.TermPoints
                      && twice.UsCongressElections.First(c => c.PollingDay == Midterm2026).PollV == midterm.PollV,
                    "(d) a world restored from a save cut after the midterm holds it once, its term and its poll carried by the save (format 52)");
            }
            finally { SimulationRandom.Reinstate(real); }
        }

        /// <summary>One day in the controller's order (`GameController.Update`, FORK-2): the day, the player's day tick, the turn at its boundary, then the player's
        /// election where the day is the polling day.</summary>
        private static void Step(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
            if (boundary)
            {
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                s.AdvanceTurn(none);
            }
            s.HoldPlayerElection();
        }

        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
