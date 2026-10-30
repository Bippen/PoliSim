using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-8 (`POLISIM_FEATURE_LIST.md` row PS-8, its done-when: "a run from any start holds every modelled country's elections on their dates"; Elias's rulings
    /// K159-K163): <b>EVERY COUNTRY'S CALENDAR, HELD.</b> FAILS while any clause is false.
    /// <list type="number">
    /// <item><description><b>The procedures' rules, planted</b> (no day stepped): a count's formation is due on its first sitting and not before; a failed
    /// formation's discharge ends it; the Riksdag's broken-off round orders the extra election three months on (RF 6 kap. 5 §), its reason the round's own,
    /// unless the ordinary falls inside them; Italy's orders art. 61's day from the dissolution, from the day the semestre bianco lifts where it bars it (art. 88
    /// c. 2); the Bundestag's orders none; the Sejm's (K302) shortens the term and orders Art. 98 ust. 5's day from the sitting, the Sejm so elected sitting
    /// on the fifteenth day with a term of its own; France holds no polling day (K162); a day a country's record is dated holds no second count.</description></item>
    /// <item><description><b>The Bundestag past its first ballot</b> (H1 of the s938 review), planted: a German chamber in which the first ballot elects no one goes on
    /// by Art. 63 Abs. 3-4 through the player's own round - the fourteen days opened, then the ballot the most votes win - and the round ends within them.</description></item>
    /// <item><description><b>No counted stream drawn</b> (M5): the chamber count, the US count, Poland's presidential round and the formations (the Bundestag's,
    /// the Sejm's, Italy's mandates), each invoked on a world standing on its day, leave every stream's draws where they were.</description></item>
    /// <item><description><b>A run from every start</b> - a world built as a new game builds one (the seed of play, the epoch the start's, the record seated on its
    /// date, the player's party the start's prime minister's, as GATE-18's idle game), stepped by the controller's own day order with every wait on the player
    /// answered by the AI's rule, until every modelled country other than the player's has held its first election and formed its government:
    /// each first election on the day WRITTEN HERE from the statutes and the record (M5: never the calendar's own answer, which is printed beside it), and
    /// every count on an ordinary day or an extra election its procedure ordered; the count GATE-18's idle game's arithmetic (K160) - the shares it was handed
    /// held against the same prediction call made again the same day on the same state (a PURITY check - the count was handed exactly the model's prediction and
    /// nothing between them moved it; not an independent model), the record's factors on a day of record, every count after the first judged against the
    /// statutory days written here too (`StatuteDays`), the
    /// count again bit for bit, the law again seat for seat, the chamber seated; on the first sitting written here, the procedure's government installed or the
    /// outgoing one discharged, and a German round past its first ballot ended; the USA's first count on its November and its House seated on the 3rd of
    /// January; Poland's first presidential vote on its day; France's record by date to K162's end, no election; the player's country never counted as an AI
    /// country. From Germany's start (M3): the player's own Bundestag elected on K159's day, 28 Jan 2029, by the player's path, its round opening the day
    /// after; and a save made between an AI count and its first sitting, loaded, forms the same government on the same day.</description></item>
    /// <item><description><b>Twice identical</b>: the Sweden start (the trajectory dump's) run again from the seed gives the same elections, chambers,
    /// governments and contests in every country, character for character.</description></item>
    /// </list>
    /// Prints `=== WorldCalendarDiagnostic: every country's calendar holds ===` and exits 0 when every clause holds. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.WorldCalendarDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class WorldCalendarDiagnostic
    {
        /// <summary>The play key the films and the fork checks seed with.</summary>
        private const int Seed = 777;
        /// <summary>CONVENTION: the longest run from a start, in years - every first election of the six starts falls inside it.</summary>
        private const int HorizonYears = 6;
        /// <summary>CONVENTION: the days a run goes on past the last first-election event it waits for (a run-off, an oath, a seating, a round's end).</summary>
        private const int MarginDays = 2;

        private static readonly CountryId[] Starts = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France };
        private const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo SeatMethod = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", Own);
        private static readonly MethodInfo CountMethod = typeof(SimulationManager).GetMethod("CountChamberAsAi", Own);
        private static readonly MethodInfo UsMethod = typeof(SimulationManager).GetMethod("HoldUsCountsAsAi", Own);
        private static readonly MethodInfo PresidentMethod = typeof(SimulationManager).GetMethod("HoldPresidentialRoundAsAi", Own);
        private static readonly MethodInfo FormMethod = typeof(SimulationManager).GetMethod("FormAsAi", Own);
        private static readonly MethodInfo ProjectRoundMethod = typeof(SimulationManager).GetMethod("ProjectRound", Own);
        private static readonly MethodInfo AdvanceRoundMethod = typeof(SimulationManager).GetMethod("AdvanceSpeakerRound", Own);

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("=== WorldCalendarDiagnostic (PS-8, K159-K163): every modelled country holds its elections on its own calendar as an AI country ===\n");
            try
            {
                Check(SeatMethod != null && CountMethod != null && UsMethod != null && PresidentMethod != null && FormMethod != null && ProjectRoundMethod != null && AdvanceRoundMethod != null, "the manager's record seating, AI counts, AI formation and the round's day are reachable");
                Planted();
                Bundestag();
                Streams();
                string first = null;
                foreach (CountryId start in Starts)
                {
                    string digest = FromStart(start, replay: false);
                    if (start == CountryId.Sweden) { first = digest; }
                }
                string again = FromStart(CountryId.Sweden, replay: true);
                Check(first != null && first == again, F("twice identical: the Sweden start run again from the seed holds the same elections, chambers, governments and contests in every country ({0} characters of record{1})",
                    first?.Length ?? 0, first == again ? string.Empty : "; FIRST DIFFERENCE at " + FirstDifference(first, again)));
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            _sb.Append(_failures == 0 ? "=== WorldCalendarDiagnostic: every country's calendar holds ===" : F("=== WorldCalendarDiagnostic: {0} FAILED ===", _failures));
            if (_failures == 0) { Debug.Log(_sb.ToString()); } else { Debug.LogError(_sb.ToString()); }
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ the procedures' rules, planted
        private static void Planted()
        {
            _sb.Append("  the procedures' rules, planted on a world at Sweden's start (no day stepped):\n");
            using (SimulationManager.EpochScope())
            {
                World w = OpenWorld(CountryId.Sweden, null);

                // Germany: due on the sitting, a discharge ends it, no extra election
                Country germany = w.GetCountry(CountryId.Germany);
                var poll = new DateTime(2029, 1, 28);
                germany.ElectionHistory.Add(new ElectionRecord { Date = poll, CountryId = "Germany", Method = ElectionMethod.GermanyNationalProportional });
                DateTime sitting = WorldCalendar.FirstSittingAfter(CountryId.Germany, poll);
                bool before = WorldCalendar.FormationDue(germany, sitting.AddDays(-1), out _, out _), on = WorldCalendar.FormationDue(germany, sitting, out _, out _);
                germany.Government.Discharge(sitting);
                bool after = WorldCalendar.FormationDue(germany, sitting, out _, out _), extraDe = WorldCalendar.TryExtraElection(germany, out _, out _, out _);
                Check(sitting == new DateTime(2029, 2, 27) && !before && on && !after && !extraDe,
                    F("Germany: a count of {0:yyyy-MM-dd} first sits on {1:yyyy-MM-dd} (Art. 39 Abs. 2's thirtieth day); its formation not due the day before, due on it, ended by the outgoing cabinet's discharge - and no extra election (the chancellor's election goes on by Art. 63 Abs. 3-4, no dissolution taken)", poll, sitting));

                // Sweden: the extra election three months on, its reason the round's own, unless the ordinary serves
                Country sweden = w.GetCountry(CountryId.Sweden);
                var sePoll = new DateTime(2030, 9, 8);
                sweden.ElectionHistory.Add(new ElectionRecord { Date = sePoll, CountryId = "Sweden", Method = ElectionMethod.SwedenTwoTier });
                DateTime seSitting = WorldCalendar.FirstSittingAfter(CountryId.Sweden, sePoll);
                sweden.Government.Discharge(seSitting);
                sweden.Government.Breaks.Add(F("{0:yyyy-MM-dd}: {1}{2:yyyy-MM-dd} - the Riksdag rejects 4 proposals - the procedure breaks off (RF 6 kap. 5 §); the outgoing government governs on as caretaker", seSitting, WorldCalendar.FailureMarker, sePoll));
                bool extraSe = WorldCalendar.TryExtraElection(sweden, out DateTime seDay, out DateTime seOrdered, out string seWhy);
                Check(seSitting == new DateTime(2030, 9, 23) && extraSe && seOrdered == seSitting && seDay == new DateTime(2030, 12, 22) && WorldCalendar.IsChamberPollingDay(sweden, seDay, out bool seExtra) && seExtra
                      && seWhy.Contains("rejects 4 proposals"),
                    F("Sweden: a count of {0:yyyy-MM-dd} first sits on {1:yyyy-MM-dd} (RF 3 kap. 10 §'s fifteenth day); a round broken off there orders the extra election on Sunday 22 Dec 2030, the last Sunday of the three months - {2}", sePoll, seSitting, seWhy));
                var seLate = new DateTime(2034, 6, 4);
                sweden.ElectionHistory.Add(new ElectionRecord { Date = seLate, CountryId = "Sweden", Method = ElectionMethod.SwedenTwoTier });
                sweden.Government.Discharge(WorldCalendar.FirstSittingAfter(CountryId.Sweden, seLate));
                bool serves = !WorldCalendar.TryExtraElection(sweden, out _, out _, out _) && WorldClock.TryNextPollingDay(CountryId.Sweden, seLate.AddDays(1), out DateTime ordinary) && ordinary == new DateTime(2034, 9, 10);
                Check(serves, "Sweden: a failure on 19 Jun 2034 orders no extra election - the ordinary of 10 Sep 2034 falls within the three months and serves (RF 6 kap. 5 §)");

                // Italy: art. 61 from the dissolution, deferred by the semestre bianco
                Country italy = w.GetCountry(CountryId.Italy);
                var itPoll = new DateTime(2027, 10, 10);
                italy.ElectionHistory.Add(new ElectionRecord { Date = itPoll, CountryId = "Italy", Method = ElectionMethod.ItalyRosatellum });
                DateTime itSitting = WorldCalendar.FirstSittingAfter(CountryId.Italy, itPoll);
                italy.Government.Discharge(itSitting);
                bool extraIt = WorldCalendar.TryExtraElection(italy, out DateTime itDay, out _, out string itWhy);
                Check(itSitting == new DateTime(2027, 10, 30) && extraIt && itDay == ConfidenceProcedure.ItalianElectionDay(itSitting),
                    F("Italy: a count of {0:yyyy-MM-dd} first sits on {1:yyyy-MM-dd}; three failed mandates there dissolve the chambers that day and elect on {2:yyyy-MM-dd} - {3}", itPoll, itSitting, itDay, itWhy));
                Country italy2 = WorldFactory.CreateDefault().GetCountry(CountryId.Italy);
                var itBarred = new DateTime(2028, 8, 15);
                italy2.ElectionHistory.Add(new ElectionRecord { Date = itBarred, CountryId = "Italy", Method = ElectionMethod.ItalyRosatellum });
                DateTime barredSitting = WorldCalendar.FirstSittingAfter(CountryId.Italy, itBarred);
                italy2.Government.Discharge(barredSitting);
                bool extraBarred = WorldCalendar.TryExtraElection(italy2, out DateTime barredDay, out _, out string barredWhy);
                Check(extraBarred && barredDay == ConfidenceProcedure.ItalianElectionDay(new DateTime(2029, 2, 3)),
                    F("Italy: failing on {0:yyyy-MM-dd}, inside the President's last six months, the dissolution waits for 3 Feb 2029 and the election falls on {1:yyyy-MM-dd} - {2}", barredSitting, barredDay, barredWhy));

                // Poland (K302): the Sejm's steps failed on the first sitting are Art. 155's - the term shortened that day, the elections on Art. 98 ust. 5's last
                // Sunday, the Sejm they elect first sitting on the fifteenth day and its term its own (ust. 6); France: no polling day; a dated day holds no second count
                Country poland = w.GetCountry(CountryId.Poland);
                var plPoll = new DateTime(2027, 10, 17);
                poland.ElectionHistory.Add(new ElectionRecord { Date = plPoll, CountryId = "Poland", Method = ElectionMethod.PolandDistricts });
                DateTime plSitting = WorldCalendar.FirstSittingAfter(poland, plPoll);
                poland.Government.Discharge(plSitting);
                bool extraPl = WorldCalendar.TryExtraElection(poland, out DateTime plDay, out DateTime plOrdered, out string plWhy);
                bool plPolls = extraPl && WorldCalendar.IsChamberPollingDay(poland, plDay, out bool plExtra) && plExtra;
                poland.ElectionHistory.Add(new ElectionRecord { Date = plDay, CountryId = "Poland", Method = ElectionMethod.PolandDistricts });
                DateTime plSitting2 = WorldCalendar.FirstSittingAfter(poland, plDay);
                bool plDue = !WorldCalendar.FormationDue(poland, plSitting2.AddDays(-1), out _, out _) && WorldCalendar.FormationDue(poland, plSitting2, out _, out _);
                bool plOwedNothing = !WorldCalendar.TryExtraElection(poland, out _, out _, out _);
                bool plNext = WorldCalendar.TryNextOrdinaryDay(poland, plDay.AddDays(1), out DateTime plOrdinary);
                Check(plSitting == new DateTime(2027, 11, 16) && extraPl && plOrdered == plSitting && plDay == new DateTime(2027, 12, 26) && plPolls && plSitting2 == new DateTime(2028, 1, 10) && plDue && plOwedNothing
                      && plNext && plOrdinary == new DateTime(2031, 12, 14) && plWhy != null && plWhy.Contains("Art. 155 ust. 2"),
                    F("Poland (K302): a count of {0:yyyy-MM-dd} first sits on {1:yyyy-MM-dd}; its steps failed there, the President shortens the term that day and orders elections for Sunday 26 Dec 2027 (16 Nov + 45 days is Friday 31 Dec, Art. 98 ust. 5) - {2:yyyy-MM-dd}; that Sejm first sits on 10 Jan 2028, the fifteenth day ({3:yyyy-MM-dd}), formed on it, and its term runs to the window opening 11 Dec 2031, its first Sunday 14 Dec 2031 ({4:yyyy-MM-dd}, Art. 98 ust. 2 and 6) - {5}",
                        plPoll, plSitting, plDay, plSitting2, plOrdinary, plWhy));
                Country france = w.GetCountry(CountryId.France);
                bool frenchDay = false;
                for (DateTime d = new DateTime(2026, 1, 19); d < new DateTime(2032, 1, 1); d = d.AddDays(1)) { if (WorldCalendar.IsChamberPollingDay(france, d, out _)) { frenchDay = true; break; } }
                Check(!frenchDay && !WorldCalendar.IsChamberPollingDay(sweden, sePoll, out _),
                    "France holds no polling day from 2026 to 2031 (K162); a day the record is dated holds no second count");

                // M5 (review 2): each country's SECOND ordinary day after a count, against the days written here - on a fresh world, a count planted on the first
                World fresh = WorldFactory.CreateDefault();
                bool seconds = true;
                var secondsLine = new List<string>();
                foreach ((CountryId id, DateTime first, ElectionMethod method, DateTime second) in new[]
                {
                    (CountryId.Germany, new DateTime(2029, 1, 28), ElectionMethod.GermanyNationalProportional, new DateTime(2033, 1, 2)),
                    (CountryId.Poland, new DateTime(2027, 10, 17), ElectionMethod.PolandDistricts, new DateTime(2031, 10, 19)),
                    (CountryId.Italy, new DateTime(2027, 10, 10), ElectionMethod.ItalyRosatellum, new DateTime(2032, 10, 24)),
                    (CountryId.Italy, new DateTime(2028, 3, 5), ElectionMethod.ItalyRosatellum, new DateTime(2033, 3, 20)),
                    (CountryId.Sweden, new DateTime(2030, 9, 8), ElectionMethod.SwedenTwoTier, new DateTime(2034, 9, 10)),
                })
                {
                    Country c = fresh.GetCountry(id);
                    c.ElectionHistory.Add(new ElectionRecord { Date = first, CountryId = id.ToString(), Method = method });
                    bool got = WorldCalendar.TryNextOrdinaryDay(c, first.AddDays(1), out DateTime next) && next == second;
                    seconds &= got;
                    secondsLine.Add(F("{0} after {1:yyyy-MM-dd}: {2:yyyy-MM-dd}{3}", id, first, next, got ? string.Empty : F(" (the statute's {0:yyyy-MM-dd})", second)));
                }
                bool usSecond = WorldClock.TryNextPollingDay(CountryId.USA, new DateTime(2026, 11, 4), out DateTime us2) && us2 == new DateTime(2028, 11, 7);
                Check(seconds && usSecond, "the second ordinary days, written here from the statutes (an Italian term re-anchored on an art. 61 election of 5 Mar 2028, first sitting 25 Mar 2028: 20 Mar 2033): "
                    + string.Join("; ", secondsLine) + F("; the USA after 3 Nov 2026: {0:yyyy-MM-dd}", us2));
            }
        }

        // ============================================================ the Bundestag past its first ballot, planted (H1)
        private static void Bundestag()
        {
            _sb.Append("  the Bundestag past its first ballot (H1), planted on a world at Sweden's start:\n");
            using (SimulationManager.EpochScope())
            {
                var host = new GameObject("WorldCalendarDiagnostic.bundestag");
                var host2 = new GameObject("WorldCalendarDiagnostic.bundestag.loaded");
                var host3 = new GameObject("WorldCalendarDiagnostic.bundestag.streams");
                try
                {
                    World w = OpenWorld(CountryId.Sweden, host);
                    SimulationManager s = host.GetComponent<SimulationManager>();
                    Country germany = w.GetCountry(CountryId.Germany);
                    // N2: the plant is checked to fail its first ballot before anything runs on it - a plan the projection says carries is not used, and none failing FAILS
                    if (!PlantHung(s, germany, out ElectionRecord record, out string plan, out FormationProjection projected))
                    {
                        Check(false, "N2: no planted chamber's first ballot is projected not to carry (" + plan + ") - the H1 path is UNEXERCISED, which this clause does not pass");
                        return;
                    }
                    // L-R3: a projection that stops at the first ballot either elects no one or elects a candidate on whose party no government can be drawn - said as which
                    bool electedWithout = projected.View != null && projected.View.NoneWords != null && projected.View.NoneWords.StartsWith("ELECTED", StringComparison.Ordinal);
                    Check(true, F("N2: the planted chamber ({0}) - its first ballot projected not to carry ({1}): {2}", plan, electedWithout ? "a candidate elected with no government drawable on its party" : "no candidate elected", projected.Reason));
                    GovernmentRecord eve = germany.Government;
                    int divisionsBefore = germany.Divisions?.Entries?.Count ?? 0;   // L-R2
                    Dictionary<SimulationRandom.Stream, int> drawsBefore = SimulationRandom.CaptureDrawCounts();
                    FormMethod.Invoke(s, new object[] { germany, record, s.CurrentDate });
                    bool firstDrewNone = SameDraws(drawsBefore, SimulationRandom.CaptureDrawCounts());
                    SpeakerRound round = germany.Government.Round;
                    bool opened = round != null && round.Open && round.Phase == 2 && ReferenceEquals(germany.Government, eve) && eve.Caretaker && eve.CaretakerSince == s.CurrentDate.Date;
                    int divisions = germany.Divisions?.Entries?.Count ?? 0;
                    Check(opened && divisions == divisionsBefore + 1 && firstDrewNone, F("the first ballot taken on the first sitting by the player's own PersonBallot - recorded as one more of the country's divisions ({0} before, {1} after), carries no government, and opens the fourteen days (Art. 63 Abs. 3) with the outgoing government serving on (Art. 69 Abs. 3); no stream drawn: {2}",
                        divisionsBefore, divisions, firstDrewNone ? "none" : "A STREAM WAS DRAWN"));
                    if (!opened) { return; }

                    // M5: a save made with the round OPEN, loaded, steps to the same installed government on the same day
                    string saved = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(s, w, CountryId.Sweden, null));
                    DateTime savedOn = s.CurrentDate.Date;
                    for (int day = 0; day <= SpeakerRound.BundestagSecondPhaseDays + MarginDays && round.Open; day++) { Day(s, w, CountryId.Sweden, null); }
                    GovernmentRecord g = germany.Government;
                    bool installed = !ReferenceEquals(g, eve) && g.FormedOn > record.Date;
                    bool phase3 = round.Phase == 3;
                    Check(!round.Open && (installed || phase3), F("the round ends inside the fourteen days and the ballot after them - {0}; the round's log: {1}",
                        installed ? Who(g) + " installed " + F("{0:yyyy-MM-dd}", g.FormedOn) + (phase3 ? " by the ballot the most votes win (Art. 63 Abs. 4)" : " in the fourteen days (Art. 63 Abs. 3)") : "no candidate stood in the ballot the most votes win; the outgoing government serves on",
                        string.Join(" | ", round.Log.Skip(Math.Max(0, round.Log.Count - 4)))));
                    string formedThen = GovernmentLine(germany.Government);
                    DateTime closedOn = s.CurrentDate.Date;

                    SimulationManager loaded = host2.AddComponent<SimulationManager>();
                    Persistence.SaveGameService.RestoreInto(loaded, Persistence.SaveGameService.Deserialize(saved));
                    EnergyMarket.BeginTurn(loaded.World);
                    Country loadedGermany = loaded.World.GetCountry(CountryId.Germany);
                    bool roundLoaded = loadedGermany.Government?.Round != null && loadedGermany.Government.Round.Open && loadedGermany.Government.Round.Phase == 2;
                    while (loaded.CurrentDate.Date < closedOn) { Day(loaded, loaded.World, CountryId.Sweden, null); }
                    string formedLoaded = GovernmentLine(loadedGermany.Government);
                    Check(roundLoaded && formedLoaded == formedThen, F("M5: a save made on {0:yyyy-MM-dd} with the chancellor's election open (phase 2), loaded and stepped to {1:yyyy-MM-dd}, ends as the continuous run ends - {2}{3}",
                        savedOn, closedOn, formedThen, !roundLoaded ? "; THE LOADED ROUND WAS NOT OPEN" : formedLoaded == formedThen ? string.Empty : "; the loaded world: " + formedLoaded));

                    // M5: the H1 path's later ballots draw no stream - on a fresh plant, the round's own day invoked directly (no day stepped): the fourteen days'
                    // ballot with the consultation run out, then the ballot the most votes win with the fourteen days past
                    World w3 = OpenWorld(CountryId.Sweden, host3);
                    SimulationManager s3 = host3.GetComponent<SimulationManager>();
                    Country germany3 = w3.GetCountry(CountryId.Germany);
                    bool replanted = PlantHung(s3, germany3, out ElectionRecord record3, out string _, out FormationProjection _);
                    if (replanted) { FormMethod.Invoke(s3, new object[] { germany3, record3, s3.CurrentDate }); }
                    SpeakerRound r3 = germany3.Government?.Round;
                    bool drewNone = true;
                    int invoked = 0;
                    for (int i = 0; i < 6 && r3 != null && r3.Open; i++)
                    {
                        if (r3.Stage == RoundStage.Consulting) { r3.AskedOn = s3.CurrentDate.AddDays(-SpeakerRound.ConsultationDays); }
                        if (r3.Stage == RoundStage.VotePending) { r3.VoteOn = s3.CurrentDate.Date; }
                        if (i >= 2) { r3.SecondPhaseUntil = s3.CurrentDate.AddDays(-1); }
                        Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
                        AdvanceRoundMethod.Invoke(s3, new object[] { germany3, germany3.Government });
                        drewNone &= SameDraws(before, SimulationRandom.CaptureDrawCounts());
                        invoked++;
                    }
                    Check(replanted && invoked > 0 && r3 != null && !r3.Open && drewNone, F("M5: the H1 path's ballots in the fourteen days and the ballot the most votes win (the round's own day invoked {0} time(s)) draw no stream and end the round{1}",
                        invoked, r3 == null ? " - NO ROUND" : r3.Open ? " - THE ROUND IS STILL OPEN" : drewNone ? string.Empty : " - A STREAM WAS DRAWN"));
                }
                finally { UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(host2); UnityEngine.Object.DestroyImmediate(host3); }
            }
        }

        /// <summary>N2: the hung chambers tried, in order - each a Bundestag of 630 whose declared lines are expected to leave the Bundespräsident's candidate short of a
        /// majority of the members; the first whose first ballot the procedure's own projection says fails is seated.</summary>
        private static readonly (string Key, int Seats)[][] HungPlans =
        {
            new[] { ("AfD", 315), ("Linke", 315) },
            new[] { ("AfD", 300), ("Linke", 170), ("CDU", 120), ("CSU", 40) },
            new[] { ("AfD", 315), ("Linke", 160), ("BSW", 155) },
            new[] { ("AfD", 315), ("Grune", 160), ("Linke", 155) },
        };

        /// <summary>N2: seats a hung chamber and its count (dated the first sitting's thirtieth day back, so today is its sitting), checked by the procedure's own
        /// projection (`ProjectRound`, which moves no state) to fail its first ballot; false, nothing left planted, where no plan fails.</summary>
        private static bool PlantHung(SimulationManager s, Country germany, out ElectionRecord record, out string plan, out FormationProjection projected)
        {
            record = null; projected = null;
            var tried = new List<string>();
            foreach ((string Key, int Seats)[] seats in HungPlans)
            {
                var map = seats.ToDictionary(x => x.Key, x => x.Seats);
                ParliamentSystem.SetSeatsFromElection(germany, map);
                record = new ElectionRecord { Date = s.CurrentDate.AddDays(-WorldClock.GermanyFirstSittingAfterDays), CountryId = "Germany", Method = ElectionMethod.GermanyNationalProportional };
                foreach (KeyValuePair<string, int> kv in map) { record.Seats[kv.Key] = kv.Value; }
                germany.ElectionHistory.Add(record);
                plan = string.Join(" ", seats.Select(x => x.Key + " " + x.Seats.ToString(CultureInfo.InvariantCulture)));
                projected = ProjectRoundMethod.Invoke(s, new object[] { germany, record.Date.Date }) as FormationProjection;
                if (projected != null && projected.Partial && !projected.Expected) { return true; }
                tried.Add(plan + (projected == null ? ": no projection" : ": " + projected.Describe()));
                germany.ElectionHistory.Remove(record);
            }
            record = null;
            plan = string.Join("; ", tried);
            return false;
        }

        private static bool SameDraws(Dictionary<SimulationRandom.Stream, int> before, Dictionary<SimulationRandom.Stream, int> after) =>
            before.Count == after.Count && before.All(kv => after.TryGetValue(kv.Key, out int n) && n == kv.Value);

        // ============================================================ no counted stream drawn (M5)
        private static void Streams()
        {
            _sb.Append("  no counted stream drawn - each AI path invoked on a world standing on its day:\n");
            Measure("the chamber count (Germany's, off its calendar)", new DateTime(2026, 1, 18), (s, w) => { CountMethod.Invoke(s, new object[] { w.GetCountry(CountryId.Germany), false }); return w.GetCountry(CountryId.Germany).ElectionHistory.Count > 0; });
            Measure("the US presidential count, the House and the Senate class (7 Nov 2028, the Tuesday after the first Monday)", new DateTime(2028, 11, 7), (s, w) =>
            {
                UsMethod.Invoke(s, new object[] { w.GetCountry(CountryId.USA) });
                return w.GetCountry(CountryId.USA).UsPresidentialElections.Exists(c => c.PollingDay.Date == new DateTime(2028, 11, 7));
            });
            Measure("Poland's presidential first round (19 May 2030)", new DateTime(2030, 5, 19), (s, w) =>
            {
                PresidentMethod.Invoke(s, new object[] { w.GetCountry(CountryId.Poland) });
                return w.GetCountry(CountryId.Poland).PresidentialElections.Exists(c => c.FirstVote == new DateTime(2030, 5, 19));
            });
            foreach ((CountryId id, ElectionMethod method, int days) in new[] { (CountryId.Germany, ElectionMethod.GermanyNationalProportional, 30), (CountryId.Poland, ElectionMethod.PolandDistricts, 30), (CountryId.Italy, ElectionMethod.ItalyRosatellum, 20), (CountryId.Sweden, ElectionMethod.SwedenTwoTier, 15) })
            {
                Measure(F("the formation in {0} on a planted count's first sitting", id), new DateTime(2026, 1, 18), (s, w) =>
                {
                    Country c = w.GetCountry(id);
                    var record = new ElectionRecord { Date = s.CurrentDate.AddDays(-days), CountryId = id.ToString(), Method = method };
                    foreach (KeyValuePair<string, int> kv in c.ParliamentSeats) { record.Seats[kv.Key] = kv.Value; }
                    c.ElectionHistory.Add(record);
                    FormMethod.Invoke(s, new object[] { c, record, s.CurrentDate });
                    return s.LastCalendarFormation(id) != null;
                }, player: id == CountryId.Sweden ? CountryId.Germany : CountryId.Sweden);
            }
        }

        private static void Measure(string what, DateTime epoch, Func<SimulationManager, World, bool> act, CountryId player = CountryId.Sweden)
        {
            using (SimulationManager.EpochScope())
            {
                var host = new GameObject("WorldCalendarDiagnostic.streams");
                try
                {
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    SimulationManager.SetEpoch(epoch);
                    World w = WorldFactory.CreateDefault();
                    SimulationManager s = host.AddComponent<SimulationManager>();
                    s.SetWorld(w);
                    s.PlayerCountryId = player;
                    Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
                    bool acted = act(s, w);
                    Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
                    bool none = before.Count == after.Count && before.All(kv => after.TryGetValue(kv.Key, out int n) && n == kv.Value);
                    Check(acted && none, F("{0} on {1:yyyy-MM-dd}: {2}; {3} stream(s) read before and after, {4}", what, epoch, acted ? "held" : "NOTHING HELD - the measurement measured nothing", before.Count, none ? "none drawn" : "A STREAM WAS DRAWN"));
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
        }

        // ============================================================ a run from a start
        private static string FromStart(CountryId start, bool replay)
        {
            _sb.Append(F("  {0} start{1}:\n", start, replay ? " (again, from the seed)" : string.Empty));
            using (SimulationManager.EpochScope())
            {
                var host = new GameObject("WorldCalendarDiagnostic." + start);
                try
                {
                    World w = OpenWorld(start, host);
                    SimulationManager s = host.GetComponent<SimulationManager>();
                    DateTime opened = s.CurrentDate;

                    // what each country owes, written here (M5); the calendar's own answer printed and held beside it
                    Dictionary<CountryId, DateTime> firstDay = FirstDaysOfStatute(start, out DateTime usDay, out DateTime plDay);
                    DateTime horizon = opened;
                    foreach (KeyValuePair<CountryId, DateTime> kv in firstDay)
                    {
                        DateTime sittingOf = SittingOfStatute(kv.Key, kv.Value);
                        horizon = Max(horizon, kv.Key == CountryId.Germany ? sittingOf.AddDays(SpeakerRound.BundestagSecondPhaseDays) : sittingOf);
                        bool agrees = WorldCalendar.TryNextOrdinaryDay(w.GetCountry(kv.Key), opened.AddDays(1), out DateTime calendar) && calendar == kv.Value && WorldCalendar.FirstSittingAfter(kv.Key, kv.Value) == sittingOf;
                        Check(agrees, F("{0}: its first polling day by the statute {1:yyyy-MM-dd}, the new chamber's first sitting {2:yyyy-MM-dd} - the calendar gives {3:yyyy-MM-dd} and {4:yyyy-MM-dd}",
                            kv.Key, kv.Value, sittingOf, calendar, WorldCalendar.FirstSittingAfter(kv.Key, kv.Value)));
                    }
                    if (usDay != DateTime.MinValue) { horizon = Max(horizon, UsCongressElection.ConvenesAfter(usDay)); }
                    if (plDay != DateTime.MinValue) { horizon = Max(horizon, plDay.AddDays(TwoRoundElection.RuleOf(CountryId.Poland).RunOffDay)); }
                    DateTime playerDay = new DateTime(2029, 1, 28);   // M3: the player's own Bundestag on K159's day
                    // M3: through the player's 27 Feb 2029 convening and its chancellor's election - the formation's time of record (§714) before the first ballot, then the
                    // fourteen days and the ballot the most votes win
                    DateTime playerSitting = new DateTime(2029, 2, 27);
                    DateTime playerEnds = playerDay.AddDays((WorldClock.FormationDays(CountryId.Germany) ?? 0) + SpeakerRound.BundestagSecondPhaseDays + MarginDays);
                    if (start == CountryId.Germany) { horizon = Max(horizon, playerEnds); }
                    DateTime cap = opened.AddYears(HorizonYears);
                    horizon = horizon.AddDays(MarginDays);
                    Check(horizon <= cap, F("every first election from the {0} start ({1:yyyy-MM-dd}) falls within {2} years - the run stops on {3:yyyy-MM-dd}", start, opened, HorizonYears, horizon));
                    if (horizon > cap) { horizon = cap; }

                    var allowedExtra = new Dictionary<CountryId, HashSet<DateTime>>();
                    int counts = 0, offDay = 0, arithmetic = 0, predicted = 0, seatedWrong = 0, formations = 0, formationWrong = 0;
                    var firstHeld = new Dictionary<CountryId, DateTime>();
                    var roundsOpened = new Dictionary<CountryId, DateTime>();
                    var notes = new List<string>();
                    bool usHouseSeated = false;   // the USA's House the first count elected, seated on its 3rd of January (read that day: a later count seats its own)
                    string savedBetween = null; CountryId savedFor = start; DateTime savedSitting = DateTime.MinValue; string formedThen = null;   // M5: the save between an AI count and its sitting
                    bool playerCounted = false, playerRound = false, playerCampaign = false, playerConvened = false;
                    while (s.CurrentDate < horizon)
                    {
                        var countBefore = new Dictionary<CountryId, int>();
                        var govBefore = new Dictionary<CountryId, GovernmentRecord>();
                        var seatsBefore = new Dictionary<CountryId, Dictionary<string, int>>();
                        foreach (Country c in w.Countries)
                        {
                            if (c.Id == start) { continue; }
                            countBefore[c.Id] = c.ElectionHistory.Count;
                            govBefore[c.Id] = c.Government;
                            seatsBefore[c.Id] = c.ParliamentSeats;   // the eve's chamber - the count replaces the dictionary, so this one stays as the count read it
                            if (WorldCalendar.TryExtraElection(c, out DateTime x, out DateTime _, out string _))
                            {
                                if (!allowedExtra.TryGetValue(c.Id, out HashSet<DateTime> set)) { allowedExtra[c.Id] = set = new HashSet<DateTime>(); }
                                set.Add(x);
                            }
                        }
                        // the prediction made again on the state the count read (M5): after the day's step, with the chamber put back to the eve's for the call - the
                        // economic vote's magnitudes read the seated chamber (EconomicVote.Magnitudes: the cabinet's majority, each partner's share of its seats), and the
                        // count has seated the new one since, the measurement's (1); the day's own record is not read (the history is the counts BEFORE the day)
                        Day(s, w, start, () =>
                        {
                            foreach (Country c in w.Countries)
                            {
                                if (c.Id == start || !WorldCalendar.CountsChamber(c.Id) || c.ElectionHistory.Count <= countBefore[c.Id]) { continue; }
                                if (c.ElectionHistory[c.ElectionHistory.Count - 1].NotHeldReason == WorldCalendar.RecordSeatedReason) { continue; }   // seated by record: no prediction was made
                                ParliamentaryCount.Held held = s.LastCalendarCount(c.Id);
                                Action putBack = NationalElection.KeepRegionalReadout();
                                Dictionary<string, int> seatedNow = c.ParliamentSeats;
                                bool made;
                                Dictionary<string, double> again;
                                try
                                {
                                    c.ParliamentSeats = seatsBefore[c.Id];
                                    made = NationalElection.TryPredictShares(c.Id, out again, EconomicVote.RecordOverTerm(c, s.CurrentDate, out _), on: s.CurrentDate, played: c);
                                }
                                finally { c.ParliamentSeats = seatedNow; putBack(); }
                                string differs = null;
                                if (made && held?.BeforeFactors != null)
                                {
                                    foreach (string k in again.Keys.Union(held.BeforeFactors.Keys).OrderBy(x => x, StringComparer.Ordinal))
                                    {
                                        double a = again.TryGetValue(k, out double av) ? av : double.NaN, b = held.BeforeFactors.TryGetValue(k, out double bv) ? bv : double.NaN;
                                        if (!a.Equals(b)) { differs = F("{0}: the count's {1:R}, again {2:R}", k, b, a); break; }
                                    }
                                }
                                else { differs = !made ? "the prediction could not be made again" : "no count readout"; }
                                if (differs != null) { predicted++; notes.Add(F("{0} on {1:yyyy-MM-dd}: the shares the count was handed before the record's factors are not the same prediction call made again on the state it read (the purity check) - first difference {2}", c.Id, s.CurrentDate, differs)); }
                            }
                        });
                        DateTime today = s.CurrentDate.Date;
                        if (usDay != DateTime.MinValue && today == UsCongressElection.ConvenesAfter(usDay))
                        {
                            Country usaToday = w.GetCountry(CountryId.USA);
                            UsCongressElection.Contest elected = usaToday.UsCongressElections.FirstOrDefault(k => k.PollingDay.Date == usDay);
                            usHouseSeated = elected != null && SameSeats(usaToday.ParliamentSeats, elected.HouseSeats(CountryId.USA));
                        }
                        if (start == CountryId.Germany)
                        {
                            Country player = w.GetCountry(start);
                            if (today == playerDay)
                            {
                                playerCounted = player.ElectionHistory.Exists(e => e.Date.Date == playerDay && NationalElection.CountsParliament(e.Method)) && s.LastPlayerCount != null && s.LastPlayerCount.Day == playerDay;
                                playerCampaign = s.LastPlayerCount != null && s.LastPlayerCount.Setup.HasValue && s.LastPlayerCount.Setup.Value.Calendar.ElectionDate == playerDay;   // the 2029 campaign staged on its own calendar
                            }
                            if (today == playerSitting) { playerConvened = player.Government != null && player.Government.Caretaker && player.Government.CaretakerSince == playerSitting; }   // Art. 69 Abs. 2 at the convening
                            if (today == playerDay.AddDays(1)) { playerRound = player.Government?.Round != null && player.Government.Round.Occasion != null && player.Government.Round.Occasion.Contains("2029-01-28"); }
                        }
                        foreach (Country c in w.Countries)
                        {
                            if (c.Id == start || !WorldCalendar.CountsChamber(c.Id)) { continue; }
                            if (c.ElectionHistory.Count > countBefore[c.Id])
                            {
                                ElectionRecord r = c.ElectionHistory[c.ElectionHistory.Count - 1];
                                counts++;
                                bool firstOne = !firstHeld.ContainsKey(c.Id);
                                if (firstOne) { firstHeld[c.Id] = r.Date; }
                                bool ordinaryDay = firstOne ? firstDay.TryGetValue(c.Id, out DateTime written) && written == r.Date.Date : StatuteDays(c.Id).Contains(r.Date.Date);   // M5: later counts by the days written here too
                                bool extraDay = allowedExtra.TryGetValue(c.Id, out HashSet<DateTime> ex) && ex.Contains(r.Date.Date);
                                if (!(ordinaryDay || extraDay) || r.Date.Date != today) { offDay++; notes.Add(F("{0} counted on {1:yyyy-MM-dd}, neither its ordinary day nor an extra election its procedure ordered", c.Id, r.Date)); }
                                bool byRecord = r.NotHeldReason == WorldCalendar.RecordSeatedReason;   // the measurement's (1): seated by record where the model has no prediction
                                string why = byRecord ? (SameSeats(r.Seats, PartySystems.InitialSeats(c.Id, WorldClock.VintageOfElection(c.Id, r.Date))) ? null : "seated by record, but not the record's seats") : Arithmetic(s, c, r);
                                if (byRecord) { _sb.Append(F("    {0}: no prediction on {1:yyyy-MM-dd} - the chamber of record seated by date ({2})\n", c.Id, r.Date, WorldClock.VintageOfElection(c.Id, r.Date))); }
                                if (why != null) { arithmetic++; notes.Add(F("{0} on {1:yyyy-MM-dd}: {2}", c.Id, r.Date, why)); }
                                if (!NationalElection.CountsParliament(r.Method) || !SameSeats(c.ParliamentSeats, r.Seats)) { seatedWrong++; notes.Add(F("{0} on {1:yyyy-MM-dd}: the chamber seated is not the count's ({2})", c.Id, r.Date, r.Method)); }
                                _sb.Append(F("    {0}: held {1:yyyy-MM-dd}{2} by {3} - {4}\n", c.Id, r.Date, extraDay ? " (an extra election)" : string.Empty, r.Method, Seats(r.Seats)));
                                if (start == CountryId.Germany && !replay && savedBetween == null && NationalElection.CountsParliament(r.Method))
                                {
                                    savedBetween = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(s, w, start, null));
                                    savedFor = c.Id;
                                    savedSitting = SittingOfStatute(c.Id, r.Date.Date);
                                }
                            }
                            if (WorldCalendar.TryLatestCount(c, out ElectionRecord latest) && SittingOfStatute(c.Id, latest.Date.Date) == today)
                            {
                                formations++;
                                GovernmentRecord g = c.Government;
                                FormationProjection p = s.LastCalendarFormation(c.Id);
                                bool installed = !ReferenceEquals(g, govBefore[c.Id]) && g.FormedOn == today && !g.Caretaker && p != null && p.Expected && g.PmParty == p.PmParty && new HashSet<string>(g.Cabinet).SetEquals(p.Cabinet) && new HashSet<string>(g.Support).SetEquals(p.Support);
                                bool discharged = ReferenceEquals(g, govBefore[c.Id]) && g.Caretaker && g.CaretakerSince == today && (p == null || !p.Expected || p.Cabinet.Count == 0);
                                bool roundGoesOn = discharged && g.Round != null && g.Round.Open && g.Round.Bundestag;
                                if (roundGoesOn) { roundsOpened[c.Id] = latest.Date.Date; }
                                if (!(installed || discharged)) { formationWrong++; notes.Add(F("{0} on {1:yyyy-MM-dd}: the first sitting neither installed the procedure's government nor discharged the outgoing one", c.Id, today)); }
                                _sb.Append(F("    {0}: on the first sitting {1:yyyy-MM-dd} {2}\n", c.Id, today, installed ? F("{0} installed - {1}", Who(g), p.Procedure)
                                    : roundGoesOn ? F("the first ballot elects no one ({0}) - the fourteen days open (Art. 63 Abs. 3)", p?.Reason ?? "-")
                                    : discharged ? F("no government formed ({0}) - {1} governs on as caretaker", p?.Reason ?? "no procedure", Who(g)) : "NOTHING RECORDED"));
                                if (c.Id == savedFor && today == savedSitting) { formedThen = GovernmentLine(c.Government); }
                            }
                        }
                    }

                    foreach (KeyValuePair<CountryId, DateTime> kv in firstDay)
                    {
                        bool held = firstHeld.TryGetValue(kv.Key, out DateTime got) && got == kv.Value;
                        Check(held, F("{0} held its first election on the statute's day, {1:yyyy-MM-dd}{2}", kv.Key, kv.Value, held ? string.Empty : got == default ? " - NONE HELD" : F(" - held on {0:yyyy-MM-dd}", got)));
                    }
                    foreach (KeyValuePair<CountryId, DateTime> kv in roundsOpened)
                    {
                        Country c = w.GetCountry(kv.Key);
                        bool ended = c.Government?.Round == null || !c.Government.Round.Open;
                        Check(ended && (c.Government.FormedOn > kv.Value || c.Government.Caretaker), F("{0}: the chancellor's election past its first ballot ended by Art. 63 Abs. 3-4 - {1}", kv.Key, Who(c.Government)));
                    }
                    Check(offDay == 0, F("every count of the run ({0}) on its country's ordinary polling day or an extra election its procedure ordered", counts));
                    Check(predicted == 0, "every count was handed exactly the vote model's prediction of its day - the same call made again on the same state gives the same shares (a purity check, K160)");
                    Check(arithmetic == 0, "every count the idle game's arithmetic (K160): no campaign, the handed shares the prediction times the record's factors where the day is of record, recounted bit for bit, allocated again seat for seat");
                    Check(seatedWrong == 0, "every count's chamber seated on its day");
                    Check(formationWrong == 0, F("every first sitting ({0}) installed the procedure's government or discharged the outgoing one", formations));
                    foreach (string n in notes.Take(12)) { _sb.Append("      ").Append(n).Append('\n'); }
                    Check(s.LastCalendarCount(start) == null, F("the player's {0} was never counted as an AI country", start));

                    if (usDay != DateTime.MinValue)
                    {
                        Country usa = w.GetCountry(CountryId.USA);
                        UsCongressElection.Contest congress = usa.UsCongressElections.FirstOrDefault(k => k.PollingDay.Date == usDay);
                        bool presidential = usDay.Year % 4 == 0;   // 3 U.S.C. §21(1): the presidential Novembers are 2024's every fourth year
                        bool president = !presidential || usa.UsPresidentialElections.Exists(p => p.PollingDay.Date == usDay);
                        Check(congress != null && president && usHouseSeated, F("the USA counted its {0:yyyy-MM-dd} {1} as an AI country and seated its House on {2:yyyy-MM-dd} - {3}", usDay,
                            presidential ? "president, House and Senate class" : "midterm", UsCongressElection.ConvenesAfter(usDay), congress?.Line ?? "NO COUNT"));
                    }
                    if (plDay != DateTime.MinValue)
                    {
                        PresidentialElection.Contest pl = w.GetCountry(CountryId.Poland).PresidentialElections.FirstOrDefault(c => c.FirstVote == plDay);
                        Check(pl != null && pl.Decided(), F("Poland's first presidential vote held on its day, {0:yyyy-MM-dd}, decided - {1}", plDay, pl?.Line ?? "NONE"));
                    }
                    if (start != CountryId.France)
                    {
                        Country france = w.GetCountry(CountryId.France);
                        DateTime asOf = s.CurrentDate.Date > WorldCalendar.FranceRecordEnds ? WorldCalendar.FranceRecordEnds : s.CurrentDate.Date;
                        WorldClock.ChamberOfRecord? chamber = null;
                        foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(CountryId.France)) { if (c.Convened <= asOf && (chamber == null || c.Convened > chamber.Value.Convened)) { chamber = c; } }
                        bool seats = chamber.HasValue && SameSeats(france.ParliamentSeats, PartySystems.InitialSeats(CountryId.France, chamber.Value.Vintage));
                        bool government = WorldClock.TryGovernmentAt(CountryId.France, asOf, out WorldClock.GovernmentOfRecord fg) && france.Government != null && france.Government.FormedOn == Max(fg.From, opened);
                        Check(seats && government && !WorldCalendar.TryLatestCount(france, out _), F("France held no election and stands as its record on {0:yyyy-MM-dd} (K162: by date to {1:yyyy-MM-dd}, then held) - {2}, the government of {3} from {4:yyyy-MM-dd}",
                            asOf, WorldCalendar.FranceRecordEnds, chamber?.Vintage.ToString() ?? "NO CHAMBER", fg.Head, france.Government?.FormedOn ?? DateTime.MinValue));
                    }
                    if (start == CountryId.Germany)
                    {
                        Country player = w.GetCountry(start);
                        bool playerFormed = player.Government != null && (player.Government.FormedOn > playerDay || (player.Government.Round != null && !player.Government.Round.Open && player.Government.Round.Phase == 3));
                        Check(playerCounted && playerCampaign && playerRound && playerConvened && playerFormed,
                            F("M3: the player's own Bundestag elected on K159's day, 28 Jan 2029, by the player's path ({0}; {1}), its chancellor's election opening the day after ({2}), the outgoing government's office ending at the convening on 27 Feb 2029 ({3}), and by {4:yyyy-MM-dd} {5}",
                            playerCounted ? "the count held" : "NO COUNT", playerCampaign ? "the 2029 campaign's shares counted" : "NO 2029 CAMPAIGN COUNTED", playerRound ? "opened" : "NOT OPENED", playerConvened ? "discharged that day" : "NOT DISCHARGED",
                            playerEnds, playerFormed ? "the government its election formed: " + GovernmentLine(player.Government) : "NO GOVERNMENT FORMED - " + GovernmentLine(player.Government)));
                    }

                    string digest = Digest(w);
                    if (start == CountryId.Germany && !replay) { LoadedFormsTheSame(savedBetween, start, savedFor, savedSitting, formedThen); }
                    return digest;
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
        }

        /// <summary>M5: the save made on an AI count's day, loaded into a manager of its own and stepped to that count's first sitting, forms the government the
        /// continuous run formed that day. Run after the continuous run is read: the load puts the save's generator in place, and nothing reads the process's
        /// streams after it but this world.</summary>
        private static void LoadedFormsTheSame(string saved, CountryId player, CountryId abroad, DateTime sitting, string formedThen)
        {
            if (saved == null || formedThen == null) { Check(false, F("M5: no save was made between an AI count and its first sitting ({0}), so the load is UNMEASURED", saved == null ? "no count" : "no sitting reached")); return; }
            var host = new GameObject("WorldCalendarDiagnostic.loaded");
            try
            {
                SimulationManager loaded = host.AddComponent<SimulationManager>();
                Persistence.SaveGameService.RestoreInto(loaded, Persistence.SaveGameService.Deserialize(saved));
                EnergyMarket.BeginTurn(loaded.World);
                DateTime savedOn = loaded.CurrentDate;
                while (loaded.CurrentDate < sitting) { Day(loaded, loaded.World, player, null); }
                string formedLoaded = GovernmentLine(loaded.World.GetCountry(abroad).Government);
                Check(formedLoaded == formedThen, F("M5: a save made on {0:yyyy-MM-dd}, between {1}'s count and its first sitting, loaded and stepped to {2:yyyy-MM-dd}, forms the same government - {3}{4}",
                    savedOn, abroad, sitting, formedThen, formedLoaded == formedThen ? string.Empty : "; the loaded world formed " + formedLoaded));
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        private static string GovernmentLine(GovernmentRecord g) => g == null ? "none" : F("{0} formed {1:yyyy-MM-dd}{2}", Who(g), g.FormedOn, g.Caretaker ? F(", caretaker since {0:yyyy-MM-dd}", g.CaretakerSince) : string.Empty);

        /// <summary>A world built as a new game builds one at <paramref name="start"/>'s start - the seed of play, the epoch, the record seated, the player's party
        /// the start's prime minister's and the average minister in its vacant chairs; a manager on <paramref name="host"/> where one is given.</summary>
        private static World OpenWorld(CountryId start, GameObject host)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            WorldClock.ApplyStart(start);
            World w = WorldFactory.CreateDefault();
            if (host == null) { return w; }
            SimulationManager s = host.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = start;
            Country player = w.GetCountry(start);
            player.PlayerPartyAbbrev = IncumbentParty(start, SimulationManager.EpochDate);
            SeatMethod.Invoke(s, null);
            CabinetSystem.SeatAverageCabinet(player);
            return w;
        }

        /// <summary>The count against GATE-18's idle game: no campaign; the handed shares the prediction's, times the record's factors on a day of record; the
        /// count of the handed shares again, bit for bit; the country's law on the counted shares again, seat for seat. Null where it holds, else why not.</summary>
        private static string Arithmetic(SimulationManager s, Country c, ElectionRecord r)
        {
            ParliamentaryCount.Held held = s.LastCalendarCount(c.Id);
            if (held == null || held.Day.Date != r.Date.Date) { return "no count readout for the day"; }
            if (held.Setup.HasValue || held.Gotv != null || held.RegionalShift != null) { return "a campaign reached the count"; }
            bool ofRecord = RecordFactors.TryFor(c.Id, r.Date, out ElectionVintage election, out Dictionary<string, double> factors);
            Dictionary<string, double> expected = ofRecord ? RecordFactors.Apply(RecordFactors.Based(election, held.BeforeFactors), factors) : held.BeforeFactors;
            if (ofRecord != (held.Factors != null) || !held.Handed.All(kv => expected.TryGetValue(kv.Key, out double e) && e == kv.Value) || expected.Count != held.Handed.Count) { return "the handed shares are not the prediction" + (ofRecord ? " times the record's factors (" + election + ")" : string.Empty); }
            ParliamentaryCount.Result again = ParliamentaryCount.Count(c.Id, r.Date, held.Handed, null, null, writeReadout: false);
            if (again == null || held.Counted == null || again.Shares.Count != held.Counted.Shares.Count || !again.Shares.All(kv => held.Counted.Shares.TryGetValue(kv.Key, out double v) && v == kv.Value)) { return "the count of the handed shares is not the count held"; }
            ElectionRecord allocated = NationalElection.Run(c.Id, 0, again.Shares, r.Date, ofRecord ? RecordFactors.RosterShareOfVote(election) : 1.0, again, ofRecord ? RecordFactors.RosterShareByRegion(election, again.RegionNames) : null, logFallback: false);
            if (!SameSeats(allocated.Seats, r.Seats)) { return "the country's law on the counted shares does not give the seats held"; }
            return null;
        }

        /// <summary>M5 (the s938 review): each start's first polling days, WRITTEN HERE from the statutes and the record, never read off the calendar under test -
        /// Sweden's second Sunday of September (2022-09-11, 2026-09-13); Germany's snap of record (2025-02-23) and K159's first Sunday of Art. 39's window from the
        /// 21st Bundestag's 25 Mar 2025 (2029-01-28); Poland's 2023-10-15 of record and Art. 98 ust. 2's first Sunday from the 10th term's 13 Nov 2023 (2027-10-17);
        /// Italy's 2022-09-25 snap and K156's last Sunday before 13 Oct 2027 (2027-10-10); the USA's Tuesdays after the first Monday of an even November
        /// (2022-11-08, 2024-11-05, 2026-11-03); Poland's president - the record's 2025-05-18 first round, then Art. 128 ust. 2's last Sunday at least 75 days
        /// before the term that began on 6 Aug 2025 ends (2030-05-19).</summary>
        private static Dictionary<CountryId, DateTime> FirstDaysOfStatute(CountryId start, out DateTime usDay, out DateTime plDay)
        {
            DateTime D(int y, int m, int d) => new DateTime(y, m, d);
            var days = new Dictionary<CountryId, DateTime>();
            usDay = plDay = DateTime.MinValue;
            switch (start)
            {
                case CountryId.Sweden:
                    days[CountryId.Germany] = D(2029, 1, 28); days[CountryId.Poland] = D(2027, 10, 17); days[CountryId.Italy] = D(2027, 10, 10);
                    usDay = D(2026, 11, 3); plDay = D(2030, 5, 19); break;
                case CountryId.Germany:
                    days[CountryId.Sweden] = D(2026, 9, 13); days[CountryId.Poland] = D(2027, 10, 17); days[CountryId.Italy] = D(2027, 10, 10);
                    usDay = D(2026, 11, 3); plDay = D(2025, 5, 18); break;
                case CountryId.Poland:
                    days[CountryId.Sweden] = D(2026, 9, 13); days[CountryId.Germany] = D(2025, 2, 23); days[CountryId.Italy] = D(2027, 10, 10);
                    usDay = D(2024, 11, 5); break;
                case CountryId.Italy:
                    days[CountryId.Sweden] = D(2022, 9, 11); days[CountryId.Germany] = D(2025, 2, 23); days[CountryId.Poland] = D(2023, 10, 15);
                    usDay = D(2022, 11, 8); plDay = D(2025, 5, 18); break;
                case CountryId.USA:
                    days[CountryId.Sweden] = D(2026, 9, 13); days[CountryId.Germany] = D(2025, 2, 23); days[CountryId.Poland] = D(2027, 10, 17); days[CountryId.Italy] = D(2027, 10, 10);
                    plDay = D(2025, 5, 18); break;
                case CountryId.France:
                    days[CountryId.Sweden] = D(2026, 9, 13); days[CountryId.Germany] = D(2025, 2, 23); days[CountryId.Poland] = D(2027, 10, 17); days[CountryId.Italy] = D(2027, 10, 10);
                    usDay = D(2024, 11, 5); plDay = D(2025, 5, 18); break;
            }
            return days;
        }

        /// <summary>M5: the first sitting a polling day gives, WRITTEN HERE - the record's convening for an election of record (Sweden 2022-09-26 and 2026-09-28,
        /// Germany 2025-03-25, Poland 2023-11-13, Italy 2022-10-13), else the statute's day: RF 3 kap. 10 §'s fifteenth, Art. 39 Abs. 2 GG's thirtieth, Art. 109
        /// ust. 2's thirtieth, art. 61's twentieth.</summary>
        private static DateTime SittingOfStatute(CountryId id, DateTime poll)
        {
            if (id == CountryId.Sweden && poll == new DateTime(2022, 9, 11)) { return new DateTime(2022, 9, 26); }
            if (id == CountryId.Sweden && poll == new DateTime(2026, 9, 13)) { return new DateTime(2026, 9, 28); }
            if (id == CountryId.Germany && poll == new DateTime(2025, 2, 23)) { return new DateTime(2025, 3, 25); }
            if (id == CountryId.Poland && poll == new DateTime(2023, 10, 15)) { return new DateTime(2023, 11, 13); }
            if (id == CountryId.Italy && poll == new DateTime(2022, 9, 25)) { return new DateTime(2022, 10, 13); }
            return poll.AddDays(id == CountryId.Sweden ? 15 : id == CountryId.Italy ? 20 : 30);
        }

        /// <summary>M5 (review 2): every statutory polling day of the modelled chambers from 2022 to 2034, WRITTEN HERE - Sweden's second Sundays of September; Germany's
        /// snap of record, then K159's first Sundays (BGB-counted: 28 Jan 2029, 2 Jan 2033); Poland's record day, then Art. 98 ust. 2's first Sundays (17 Oct 2027,
        /// 19 Oct 2031); Italy's snap, then K156's last Sundays before the fifth anniversary (10 Oct 2027; 24 Oct 2032 for a term first sitting 30 Oct 2027). A
        /// re-anchored Italian term after an extra election is not listed: such a count is judged as the extra election its procedure ordered.</summary>
        private static HashSet<DateTime> StatuteDays(CountryId id)
        {
            DateTime D(int y, int m, int d) => new DateTime(y, m, d);
            switch (id)
            {
                case CountryId.Sweden: return new HashSet<DateTime> { D(2022, 9, 11), D(2026, 9, 13), D(2030, 9, 8), D(2034, 9, 10) };
                case CountryId.Germany: return new HashSet<DateTime> { D(2025, 2, 23), D(2029, 1, 28), D(2033, 1, 2) };
                case CountryId.Poland: return new HashSet<DateTime> { D(2023, 10, 15), D(2027, 10, 17), D(2031, 10, 19) };
                case CountryId.Italy: return new HashSet<DateTime> { D(2022, 9, 25), D(2027, 10, 10), D(2032, 10, 24) };
                default: return new HashSet<DateTime>();
            }
        }

        /// <summary>The controller's day, as GATE-18's idle game steps it: the waits on the player answered by the AI's rule, the day (then <paramref name="afterDay"/>,
        /// on the day's own state), the player's day tick, the boundary with no decision anywhere and the market's turn state, the player's election.</summary>
        private static void Day(SimulationManager s, World w, CountryId id, Action afterDay)
        {
            s.IsCounterfactual = true;
            try { s.AnswerWaitsAsTheAi(id); }
            finally { s.IsCounterfactual = false; }
            bool boundary = s.AdvanceDay();
            afterDay?.Invoke();
            s.AdvanceCountryDayTick(id);
            if (!boundary) { s.HoldPlayerElection(); return; }
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in w.Countries) { decisions[c.Id] = PolicyDecision.None(); }
            s.AdvanceTurn(decisions);
            EnergyMarket.BeginTurn(w);
            s.HoldPlayerElection();
        }

        /// <summary>Every country's elections, chambers, governments and contests, in one string - the determinism check's reading.</summary>
        private static string Digest(World w)
        {
            var sb = new StringBuilder();
            foreach (Country c in w.Countries)
            {
                sb.Append(c.Id).Append('|');
                foreach (ElectionRecord e in c.ElectionHistory) { sb.Append(e.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append(e.Method).Append(Seats(e.Seats)).Append(';'); }
                sb.Append("|seats ").Append(Seats(c.ParliamentSeats));
                GovernmentRecord g = c.Government;
                if (g != null) { sb.Append("|gov ").Append(g.PmParty).Append(':').Append(string.Join("+", g.Cabinet)).Append('/').Append(string.Join("+", g.Support)).Append('@').Append(g.FormedOn.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append(g.Caretaker ? " caretaker" : string.Empty).Append(' ').Append(g.Executive); }
                foreach (UsPresidentialElection.Contest p in c.UsPresidentialElections) { sb.Append("|us ").Append(p.Line); }
                foreach (UsCongressElection.Contest k in c.UsCongressElections) { sb.Append("|congress ").Append(k.Line); }
                foreach (PresidentialElection.Contest p in c.PresidentialElections) { sb.Append("|pres ").Append(p.Line); }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string FirstDifference(string a, string b)
        {
            if (a == null || b == null) { return "a missing run"; }
            int i = 0;
            while (i < a.Length && i < b.Length && a[i] == b[i]) { i++; }
            int from = Math.Max(0, i - 40);
            return F("character {0}: '{1}' against '{2}'", i, a.Substring(from, Math.Min(120, a.Length - from)), b.Substring(from, Math.Min(120, b.Length - from)));
        }

        private static string Seats(IReadOnlyDictionary<string, int> seats)
        {
            if (seats == null) { return "-"; }
            return string.Join(" ", seats.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)));
        }

        private static bool SameSeats(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
        {
            if (a == null || b == null) { return false; }
            foreach (KeyValuePair<string, int> kv in a) { if ((b.TryGetValue(kv.Key, out int n) ? n : 0) != kv.Value) { return false; } }
            foreach (KeyValuePair<string, int> kv in b) { if ((a.TryGetValue(kv.Key, out int n) ? n : 0) != kv.Value) { return false; } }
            return true;
        }

        private static string Who(GovernmentRecord g) => g == null ? "none" : (g.Cabinet.Count > 0 ? string.Join("+", g.Cabinet) : "no cabinet") + " led by " + (g.PmParty ?? "-");

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static string IncumbentParty(CountryId id, DateTime day) =>
            WorldClock.TryGovernmentAt(id, day, out WorldClock.GovernmentOfRecord g) ? PartyOf(g.Head) ?? (g.Cabinet != null && g.Cabinet.Length > 0 ? g.Cabinet[0] : null) : null;

        private static string PartyOf(string head)
        {
            if (head == null) { return null; }
            Match m = Regex.Match(head, @"\(([^)]+)\)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static void Check(bool ok, string what)
        {
            if (!ok) { _failures++; }
            _sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n');
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
