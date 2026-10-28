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
    /// GATE-18 piece two (`POLISIM_FEATURE_LIST.md` row 5; Elias's ruling q18 of 2026-10-18 with K-1): <b>AN ELECTION OF RECORD INSIDE A START'S WINDOW IS
    /// REPRODUCED BY AN IDLE GAME</b> - "eve-fitted factors at the unit the count uses (E1's form); each share within 1.5 points, and the record's government
    /// formed; the noise at zero for this gate; the out-of-sample prediction printed beside it. Sweden 2026 (K-1) and Poland's 2025 run-off are inside it."
    /// <list type="number">
    /// <item><description><b>The window.</b> Every election of record on or after each start's opening day, printed with what holds it: the chamber elections
    /// the game counts (`ParliamentaryCount.CountsIn`) carry the record's factors (`RecordFactors.Elections`); the presidential votes of record are E1's; the
    /// rest are named with the reason the game holds no count of them.</description></item>
    /// <item><description><b>The idle game</b>, per chamber election of the table: a world built as a new game builds one at the country's start (the seed of
    /// play, the epoch first, the record seated on its date), the player's party the start's prime minister's (DECLARED), stepped by the game's own day loop in
    /// the controller's order (the day, the player's day tick, the boundary with no decision anywhere, the market's turn state, the player's election) through
    /// the polling day of record and on until the first government formed after it. Nothing is played: each wait on the player is answered at the start of
    /// the next day by the AI's rule for the player's party (`SimulationManager.AnswerWaitsAsTheAi`, Elias's K49 for an unanswered question - DECLARED as the
    /// idle player's answer). The whole run is inside `ParliamentaryCount.NoiseAtZero`.</description></item>
    /// <item><description><b>The fit</b> (E1's form at the count's unit): each list's factor is the record's share over the roster's total divided by the share
    /// the idle game handed the count, refit by the count itself to its fixed point (the doors' turnout moves a counted share a little off the handed one) and
    /// normalised so the factors times the handed shares sum to one. Printed as FIT lines, which `Tools/record_factor_refit.pl` writes into
    /// `RecordFactors` from this log (its `--check` confirms the stored factors are a log's fit).</description></item>
    /// <item><description><b>The gate</b> (FAILS while any is false): every factor of the election fitted; the count drawn at sigma 0; every list's share as the
    /// game counted it within `TolerancePp` of the record's share over the roster's total; and, where the record names the cabinet formed after that election,
    /// the game's first government formed after the polling day led by the record's head's party with the record's cabinet. Where the record names none, the
    /// clause is printed NOT ON RECORD and asserts nothing. <b>The refit clause</b> (K248, superseding K107's "reported, not failed": PS-8's AI countries count
    /// with these factors, K160): the stored factors within `RefitReportFloor` of this run's fit, every list the fit prints holding a row of the table - or
    /// the election is DUE A REFIT and the gate fails on it by name until `Tools/record_factor_refit.pl` has written the fit in.</description></item>
    /// <item><description><b>GATE-18 q20, with it</b> (FAILS while false): the night installs nothing - the government before the polling day governs on
    /// through it - and the night's projection by the procedure's own code (`SimulationManager.ProjectFormation`) is the government the procedure formed,
    /// "formed" read off the procedure's own completion; a projection that stops at the Bundestag's first ballot is printed and asserts nothing.</description></item>
    /// <item><description><b>The out-of-sample prediction, beside it</b>: the same count of the same idle game's handed shares at factor one - the model's own
    /// prediction, which nothing of the record reached (no part of the game before the count reads the factors) - per list, its miss, its mean absolute miss and
    /// its chamber. Printed, never a pass condition.</description></item>
    /// <item><description><b>Poland's 2025 presidential election</b>, on E1's reference world (`PresidentialReferenceWorld` - RULED F4: the world that follows
    /// history is the one E1's factors are fitted on): every first-round candidate of record within `TolerancePp` of the PKW's, the run-off electing the
    /// record's winner with his share of the two within `TolerancePp` of the record's. Beside it, the first round at factor one (the candidates with no roster
    /// party at their share of record, as E1 rules their base) and the run-off it leads to.</description></item>
    /// <item><description><b>Piece three</b> (q3, q25): Sweden's count allocated by the Riksdag's own law on its counted valkretsar - asserted as wiring; its seats
    /// per valkrets, and the out-of-sample count's, against the record's seat table REPORTED; the polls' mean the tactical layer believed, REPORTED.</description></item>
    /// </list>
    /// Prints `=== Gate18Diagnostic: the gate holds ===` and exits 0 when every clause holds. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.Gate18Diagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class Gate18Diagnostic
    {
        /// <summary>q18's tolerance, ruled: each share within this many points of the record's.</summary>
        public const double TolerancePp = 1.5;
        /// <summary>The play key the films and the fork checks seed with.</summary>
        private const int Seed = 777;
        /// <summary>CONVENTION: the fit's fixed point is reached when every counted share sits this close to its target (fractions of the vote).</summary>
        private const double FitConvergence = 1e-12;
        /// <summary>CONVENTION: the most iterations the fit takes to reach its fixed point.</summary>
        private const int FitIterations = 200;
        /// <summary>CONVENTION: the stored factors are due a refit when the refit moves one by more than this, relative (to the larger of one and the fit).</summary>
        private const double RefitReportFloor = 1e-6;
        /// <summary>RULED K248 (Code's recommendation, declared for Elias to strike; supersedes K107's "reported, not failed"): a refit owed FAILS the gate,
        /// since PS-8's AI countries count with these factors (K160). False restores K107: the drift printed, the gate reading the shares alone.</summary>
        private static readonly bool RefitFails = true;

        private static readonly MethodInfo SeatMethod = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("=== Gate18Diagnostic (GATE-18 piece two, q18 with K-1): an election of record inside a start's window reproduced by an idle game, the noise at zero ===\n");
            try
            {
                Window();
                using (ParliamentaryCount.NoiseAtZero())
                {
                    foreach (ElectionVintage election in RecordFactors.Elections) { Chamber(election); }
                    Presidential();
                }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            _sb.Append(_failures == 0 ? "=== Gate18Diagnostic: the gate holds ===" : F("=== Gate18Diagnostic: {0} FAILED ===", _failures));
            if (_failures == 0) { Debug.Log(_sb.ToString()); } else { Debug.LogError(_sb.ToString()); }
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ the window
        private static void Window()
        {
            _sb.Append("  the elections of record inside each start's window (on or after its opening day):\n");
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
            {
                DateTime start = WorldClock.StartDate(id);
                foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(id))
                {
                    if (c.ElectionDay == DateTime.MinValue || c.ElectionDay < start) { continue; }
                    string holds = RecordFactors.Elections.Contains(c.Vintage) ? "the chamber count, the record's factors fitted here - IN THE GATE"
                        : WorldClock.GoverningModeOnly(id) ? "no election is held (governing mode)"
                        : WorldClock.NoElectionYet(id) ? "no election is held in this game yet"
                        : WorldClock.RecordSeatsChamber(id) ? "the House counted on the day loop by its own model, not the chamber count - not in this piece"
                        : !ParliamentaryCount.CountsIn(id) ? "not counted by the chamber count"
                        : "counted, but no factors are carried for it";
                    _sb.Append(F("    {0}: starts {1:yyyy-MM-dd}; the chamber election of {2:yyyy-MM-dd} ({3}) - {4}\n", id, start, c.ElectionDay, c.Vintage, holds));
                }
                foreach (CountryId presidential in new[] { CountryId.Poland, CountryId.USA })
                {
                    if (presidential != id) { continue; }
                    DateTime vote = PresidentialReferenceWorld.VoteOfRecord(id);
                    if (vote < start) { continue; }
                    _sb.Append(F("    {0}: the presidential vote of {1:yyyy-MM-dd} - E1's factors on its reference world{2}\n", id, vote,
                        id == CountryId.Poland ? " - IN THE GATE (the run-off)" : " (UsPresidentialFactorCheck holds them; the count by the states is not in this piece)"));
                }
            }
        }

        // ============================================================ a chamber election of record, by an idle game
        private static void Chamber(ElectionVintage election)
        {
            CountryId id = RecordFactors.CountryOf(election);
            DateTime poll = RecordFactors.PollingDayOf(election);
            _sb.Append(F("  {0} ({1}, polling day {2:yyyy-MM-dd}):\n", election, id, poll));
            bool fitted = RecordFactors.IsFitted(election, out List<string> unfitted);
            Check(fitted, fitted ? "every list's factor is fitted" : "UNFITTED - no fit patched in for " + string.Join(", ", unfitted) + " (perl Tools/record_factor_refit.pl <this log> --record s<NNN>, then run again)");

            if (!PartySystems.TryHistory(id, out double[] recordShares, out double[] _, election)) { Check(false, "the record's shares are not on disk"); return; }
            PoliticalParty[] roster = PartySystems.RealRoster(id);
            double rosterTotal = recordShares.Sum();
            var target = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int i = 0; i < roster.Length && i < recordShares.Length; i++) { target[roster[i].Abbrev] = recordShares[i] / rosterTotal; }

            using (SimulationManager.EpochScope())
            {
                SimulationRandom.Seed(Seed);
                EnergyMarket.ResetCalibration();
                WorldClock.ApplyStart(id);
                World w = WorldFactory.CreateDefault();
                var host = new GameObject("Gate18Diagnostic." + id);
                try
                {
                    SimulationManager s = host.AddComponent<SimulationManager>();
                    s.SetWorld(w);
                    s.PlayerCountryId = id;
                    Country country = w.GetCountry(id);
                    string player = IncumbentParty(id, SimulationManager.EpochDate);
                    country.PlayerPartyAbbrev = player;
                    SeatMethod.Invoke(s, null);
                    int chairs = CabinetSystem.SeatAverageCabinet(country);   // the start's chairs filled as every new game fills them on its start day (GameController.SelectPlayerCountry, PlayProtocolStaging) (§913)
                    _sb.Append(F("    the idle game: opened {0:yyyy-MM-dd}, seed {1}, the player's party {2} (the start's prime minister's, DECLARED), nothing played; the average minister seated in {3} vacant chair(s) of the player's, as a new game's start seats them\n", s.CurrentDate, Seed, player ?? "none", chairs));

                    // to the polling day, the count held by the day loop
                    ParliamentaryCount.Held held = null;
                    GovernmentRecord eve = null;   // GATE-18 q20: the government before the polling day's own day - the night must leave it in place
                    for (int day = 0; day < 800 && held == null && s.CurrentDate <= poll; day++)
                    {
                        eve = country.Government;
                        Day(s, w, id);
                        if (s.LastPlayerCount != null && s.LastPlayerCount.Day == poll) { held = s.LastPlayerCount; }
                    }
                    if (held == null || held.Counted == null) { Check(false, F("the idle game held no chamber count on {0:yyyy-MM-dd} (it stood on {1:yyyy-MM-dd})", poll, s.CurrentDate)); return; }
                    Check(held.Counted.SigmaPp == 0.0, F("the count drawn at sigma {0} (the noise at zero for this gate) - {1}", held.Counted.SigmaPp, held.Setup.HasValue ? "the campaign's shares through the tactical layer, its doors counted" : "the prediction, no campaign staged"));
                    Dictionary<string, double> expected = RecordFactors.Apply(RecordFactors.Based(election, held.BeforeFactors), RecordFactors.Stored(election));
                    bool carried = held.Factors != null && held.Election == election && held.Handed.All(kv => expected.TryGetValue(kv.Key, out double e) && kv.Value == e);
                    Check(fitted ? carried : held.Factors == null && ReferenceEquals(held.Handed, held.BeforeFactors),
                        fitted ? "the count was handed the shares times the stored factors (" + election + ")" : "unfitted: the count was handed the model's own shares, no factor carried");

                    // K109, planted: a list handed nothing carries its share of record as its base; every other list its handed share
                    string plantedKey = roster[0].Abbrev;
                    var planted = new Dictionary<string, double>(held.BeforeFactors, StringComparer.Ordinal) { [plantedKey] = 0.0 };
                    Dictionary<string, double> plantedBase = RecordFactors.Based(election, planted);
                    // PS-7: every other list keeps its own base - its handed share, or K109's share of record where the model handed it nothing too (Italy's IC and ScN, which no 2018 list carries)
                    Dictionary<string, double> unplantedBase = RecordFactors.Based(election, held.BeforeFactors);
                    Check(plantedBase[plantedKey] == target[plantedKey] && planted.Where(kv => kv.Key != plantedKey).All(kv => plantedBase[kv.Key] == unplantedBase[kv.Key]) && held.BeforeFactors.Where(kv => kv.Value > 0.0).All(kv => unplantedBase[kv.Key] == kv.Value),   // K109: a list handed a positive share keeps it as its base
                        F("K109 (planted): {0} handed nothing takes its share of record {1:0.000000} as its base, every other list its own base (its handed share, or its share of record where it was handed nothing too)", plantedKey, target[plantedKey]));

                    // the fit, by the count itself, on the shares the idle game handed it
                    Dictionary<string, double> fit = Fit(id, election, held, target, out double fitMiss, out int iterations);
                    Dictionary<string, double> stored = RecordFactors.Stored(election);
                    double moved = 0.0;
                    string movedKey = null;
                    var rowless = new List<string>();   // a list the fit prints that the table holds no row for: the refit tool refuses it, so the gate names it
                    foreach (KeyValuePair<string, double> kv in fit)
                    {
                        double before = held.BeforeFactors.TryGetValue(kv.Key, out double h) ? h : double.NaN;
                        _sb.Append(F("      FIT {0} {1} {2:R}  (handed {3:0.000000}, record {4:0.000000})\n", election, kv.Key, kv.Value, before, target.TryGetValue(kv.Key, out double t) ? t : double.NaN));
                        if (!stored.TryGetValue(kv.Key, out double old)) { rowless.Add(kv.Key); continue; }
                        if (double.IsNaN(old)) { continue; }
                        double m = Math.Abs(old - kv.Value) / Math.Max(1.0, Math.Abs(kv.Value));
                        if (m > moved) { moved = m; movedKey = kv.Key; }
                    }
                    _sb.Append(F("      the fit's fixed point in {0} iteration(s), its largest miss {1:0.0e+0} of the vote\n", iterations, fitMiss));
                    // the refit clause (K248): the stored factors are this run's fit, within the floor, every list the fit prints a row of the table
                    if (!fitted) { _sb.Append("      the refit: nothing stored to compare\n"); }
                    else
                    {
                        bool isFit = moved <= RefitReportFloor && rowless.Count == 0;
                        string refit = isFit
                            ? F("the refit (K248): the stored factors are the fit (the largest relative move {0:0.0e+0}, the floor {1:0e+0})", moved, RefitReportFloor)
                            : F("the refit (K248): the stored factors of {0} are DUE A REFIT - the largest relative move {1:0.0e+0} ({2}) against the floor {3:0e+0}{4} (perl Tools/record_factor_refit.pl <this log> --record s<NNN>, then run again)",
                                election, moved, movedKey ?? "none", RefitReportFloor, rowless.Count > 0 ? "; no row in the table for " + string.Join(", ", rowless) : string.Empty);
                        if (RefitFails) { Check(isFit, refit); }
                        else { _sb.Append("      ").Append(refit).Append(isFit ? string.Empty : " - reported (K107), the gate reads the shares").Append('\n'); }
                    }

                    // the out-of-sample prediction: the same count of the same handed shares at factor one
                    ParliamentaryCount.Result oos = ParliamentaryCount.Count(id, poll, held.BeforeFactors, held.Setup, held.Gotv, writeReadout: false, regionalShift: held.RegionalShift);
                    ElectionRecord gameRecord = country.ElectionHistory.LastOrDefault(r => r.Date == poll);
                    // GATE-18 q3: the out-of-sample count allocated as the game's is - Sweden's on its counted valkretsar by the Riksdag's law
                    Dictionary<string, int> oosSeats = oos != null ? NationalElection.Run(id, 0, oos.Shares, poll, RecordFactors.RosterShareOfVote(election),
                        oos, RecordFactors.RosterShareByRegion(election, oos.RegionNames)).Seats : null;
                    Dictionary<string, int> recordSeats = PartySystems.InitialSeats(id, election);
                    double worst = 0.0, oosAbs = 0.0, gameAbs = 0.0;
                    string worstKey = null;
                    _sb.Append("      list    record   game    miss  |  out of sample  miss  |  seats record / game / out of sample\n");
                    foreach (PoliticalParty p in roster)
                    {
                        string k = p.Abbrev;
                        double rec = 100.0 * (target.TryGetValue(k, out double tk) ? tk : 0.0);
                        double game = 100.0 * (held.Counted.Shares.TryGetValue(k, out double gk) ? gk : 0.0);
                        double outOf = oos != null && oos.Shares.TryGetValue(k, out double ok) ? 100.0 * ok : double.NaN;
                        if (Math.Abs(game - rec) > worst) { worst = Math.Abs(game - rec); worstKey = k; }
                        gameAbs += Math.Abs(game - rec);
                        oosAbs += Math.Abs(outOf - rec);
                        _sb.Append(F("      {0,-6} {1,7:0.00} {2,7:0.00} {3,6:+0.00;-0.00}  |  {4,7:0.00}  {5,6:+0.00;-0.00}  |  {6} / {7} / {8}\n", k, rec, game, game - rec, outOf, outOf - rec,
                            recordSeats != null && recordSeats.TryGetValue(k, out int rs) ? rs.ToString(CultureInfo.InvariantCulture) : "-",
                            gameRecord != null && gameRecord.Seats.TryGetValue(k, out int gs) ? gs.ToString(CultureInfo.InvariantCulture) : "-",
                            oosSeats != null && oosSeats.TryGetValue(k, out int os) ? os.ToString(CultureInfo.InvariantCulture) : "-"));
                    }
                    _sb.Append(F("      the record's shares over the roster's total ({0:0.00} % of the vote cast); mean absolute miss: the game {1:0.00} pp, the out-of-sample prediction {2:0.00} pp\n",
                        rosterTotal, gameAbs / roster.Length, oosAbs / roster.Length));
                    Check(worst <= TolerancePp, F("q18: every list within {0} points of the record, in-sample (the factors fitted on this run's own shares) - the largest miss {1:0.00} pp ({2})", TolerancePp, worst, worstKey ?? "none"));
                    // REPORTED (the pass rule stands as registered): the statutory line read on the record's base - the lists the game seats against the lists the record seats
                    var seatedRecord = roster.Where(p => recordSeats != null && recordSeats.TryGetValue(p.Abbrev, out int n) && n > 0).Select(p => p.Abbrev).ToList();
                    var seatedGame = roster.Where(p => gameRecord != null && gameRecord.Seats.TryGetValue(p.Abbrev, out int n) && n > 0).Select(p => p.Abbrev).ToList();
                    _sb.Append(F("      the threshold on the record's base (the roster's share of the valid vote {0:0.0000}{1}): the record seats {2}; the game seats {3} - {4}\n",
                        RecordFactors.RosterShareOfVote(election), held.Factors != null ? ", read by the count's allocation" : ", NOT read - no factors carried", string.Join(" ", seatedRecord), string.Join(" ", seatedGame),
                        new HashSet<string>(seatedRecord).SetEquals(seatedGame) ? "the same lists" : "REPORTED: THE LISTS DIFFER"));

                    // GATE-18 piece three, q3: Sweden's chamber by the Riksdag's own law on the counted valkretsar - the wiring asserted (not a pass rule of q18's
                    // registration), the seats per valkrets against the record's REPORTED (printed, not asserted: no registration names a seat-level pass)
                    if (id == CountryId.Sweden)
                    {
                        Check(held.Counted.Riksdag != null && gameRecord != null && gameRecord.Method == ElectionMethod.SwedenTwoTier,
                            "q3: the idle game's count was allocated by the Riksdag's own law on its counted valkretsar (the 12 % route, the fixed and adjustment seats per valkrets)");
                        RiksdagLines(election, "the game", held.Counted);
                        RiksdagLines(election, "out of sample", oos);
                    }
                    // GATE-18 piece three, q25 (REPORTED): what the tactical layer believed on the polling day
                    if (held.Setup.HasValue)
                    {
                        _sb.Append(F("      q25: the tactical layer believed the polls' mean - {0} published tracker(s) over the {1} days to the polling day{2}\n", held.BeliefPolls,
                            NationalElection.TacticalBeliefWindowDays, held.Belief.HasValue ? F(" (pooled sample {0})", held.Belief.Value.SampleSize) : ", none in the window: the layer was skipped"));
                    }

                    // the record's government formed
                    Government(s, w, country, id, poll, eve);
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
        }

        /// <summary>GATE-18 q3 (REPORTED, printed): a Swedish count's seats per valkrets by the Riksdag's law against the record's seat table of that election
        /// (`ElectionsData/sweden/2026/valkrets_seats_2026.csv` for 2026) - the cells that agree, the seats placed elsewhere, the lists seated by the 12 % route alone.</summary>
        private static void RiksdagLines(ElectionVintage election, string what, ParliamentaryCount.Result count)
        {
            if (count?.Riksdag == null) { _sb.Append(F("      q3 ({0}): no allocation by the Riksdag's law was kept on the count\n", what)); return; }
            string table = election == ElectionVintage.Sweden2026 ? System.IO.Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "2026", "valkrets_seats_2026.csv") : null;
            PoliticalParty[] all = PartySystems.For(CountryId.Sweden).ToArray();
            var twelveOnly = new List<string>();
            for (int p = 0; p < all.Length; p++) { if (!count.Riksdag.NationallyEligible[p] && count.Riksdag.Seats[p] > 0) { twelveOnly.Add(all[p].Abbrev + " " + count.Riksdag.Seats[p]); } }
            if (table == null || !System.IO.File.Exists(table))
            {
                _sb.Append(F("      q3 ({0}): {1} seat(s) returned; by the 12 % route alone: {2}; no per-valkrets seat table of record on disk for {3}\n", what, count.Riksdag.SeatsReturned,
                    twelveOnly.Count > 0 ? string.Join(", ", twelveOnly) : "none", election));
                return;
            }
            string[] head = null;
            int cells = 0, same = 0, elsewhere = 0, valkretsar = 0, whole = 0;
            foreach (string line in System.IO.File.ReadAllLines(table, Encoding.UTF8))
            {
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                string[] f = line.Split(';');
                if (head == null) { head = f; continue; }
                int r = Array.IndexOf(count.RegionNames, f[0]);
                if (r < 0) { continue; }
                valkretsar++;
                bool all8 = true;
                for (int c = 0; c < head.Length; c++)
                {
                    int p = Array.FindIndex(all, q => q.Abbrev == head[c]);
                    if (p < 0) { continue; }
                    int record = int.Parse(f[c], CultureInfo.InvariantCulture);
                    cells++;
                    if (count.Riksdag.RegionSeats[r][p] == record) { same++; } else { all8 = false; elsewhere += Math.Max(0, count.Riksdag.RegionSeats[r][p] - record); }
                }
                if (all8) { whole++; }
            }
            _sb.Append(F("      q3 ({0}), REPORTED: the seats per valkrets against the record's - {1} of {2} cells agree, {3} of {4} valkretsar whole, {5} seat(s) placed elsewhere; {6} returned; by the 12 % route alone: {7}\n",
                what, same, cells, whole, valkretsar, elsewhere, count.Riksdag.SeatsReturned, twelveOnly.Count > 0 ? string.Join(", ", twelveOnly) : "none"));
        }

        /// <summary>The fit at the count's fixed point: start from the record over the handed share, then each factor times the record over the counted share,
        /// renormalised so the factors times the bases sum to one - each list's base the share handed, or its share of record where it was handed nothing (K109).</summary>
        private static Dictionary<string, double> Fit(CountryId id, ElectionVintage election, ParliamentaryCount.Held held, Dictionary<string, double> target, out double miss, out int iterations)
        {
            Dictionary<string, double> basis = RecordFactors.Based(election, held.BeforeFactors);
            var f = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> kv in basis)
            {
                double t = target.TryGetValue(kv.Key, out double x) ? x : 0.0;
                f[kv.Key] = kv.Value > 0.0 ? t / kv.Value : 1.0;
            }
            miss = double.PositiveInfinity;
            iterations = 0;
            for (int i = 0; i < FitIterations; i++)
            {
                ParliamentaryCount.Result r = ParliamentaryCount.Count(id, held.Day, RecordFactors.Apply(basis, f), held.Setup, held.Gotv, writeReadout: false, regionalShift: held.RegionalShift);
                if (r == null) { break; }
                iterations = i + 1;
                miss = 0.0;
                foreach (string k in basis.Keys)
                {
                    double t = target.TryGetValue(k, out double x) ? x : 0.0;
                    miss = Math.Max(miss, Math.Abs(r.Shares[k] - t));
                }
                if (miss <= FitConvergence) { break; }
                var next = new Dictionary<string, double>(StringComparer.Ordinal);
                double sum = 0.0;
                foreach (KeyValuePair<string, double> kv in basis)
                {
                    double t = target.TryGetValue(kv.Key, out double x) ? x : 0.0;
                    double counted = r.Shares[kv.Key];
                    next[kv.Key] = kv.Value > 0.0 && counted > 0.0 ? f[kv.Key] * t / counted : f[kv.Key];
                    sum += kv.Value * next[kv.Key];
                }
                foreach (string k in basis.Keys) { f[k] = basis[k] > 0.0 && sum > 0.0 ? next[k] / sum : next[k]; }
            }
            return f;
        }

        /// <summary>The idle game stepped on from the count until the first government formed after the polling day, its procedure complete; held against the
        /// first cabinet of record that took office after the election inside the record's formation window. GATE-18 q20 (ruled 2026-10-18: "one formation
        /// code ... office changes when the procedure completes"): on the night, before a day of the procedure runs, the procedure's own projection
        /// (`SimulationManager.ProjectFormation`, what the night draws) and the government left in place (<paramref name="eve"/>, the record before the polling
        /// day); every change of the record from then on printed with its basis; and the projection held against what the procedure formed - "formed"
        /// being the procedure's own completion (the round concluded, the Sejm's steps closed: `Formed`), never the night's.</summary>
        private static void Government(SimulationManager s, World w, Country country, CountryId id, DateTime poll, GovernmentRecord eve)
        {
            WorldClock.GovernmentOfRecord? ofRecord = null;
            foreach (WorldClock.GovernmentOfRecord g in WorldClock.Governments(id))
            {
                if (g.Kind != WorldClock.ExecutiveKind.Cabinet || g.Head == null || g.Head.IndexOf("caretaker", StringComparison.OrdinalIgnoreCase) >= 0 || !g.CabinetSourced || g.Cabinet == null) { continue; }
                if (g.From <= poll || g.From > poll.AddDays(WorldClock.FormationWindowDays)) { continue; }
                if (!ofRecord.HasValue || g.From < ofRecord.Value.From) { ofRecord = g; }
            }
            // q20: the night - its projection, and the government it leaves in place
            FormationProjection expected = s.ProjectFormation(country, poll);
            Check(expected != null && eve != null && ReferenceEquals(country.Government, eve),
                F("q20: the night installs nothing - {0} governs on through the night; the night projects {1}", Who(eve), expected?.Describe() ?? "NOTHING (no procedure)"));
            if (expected != null) { foreach (string step in expected.Steps) { _sb.Append("        ").Append(step).Append('\n'); } }
            var changes = new List<string>();
            GovernmentRecord last = country.Government;
            // the review's M3: this election's own procedure, held by reference - the Sejm's steps opened tonight, the round that reads this polling day once it
            // opens - so "none formed" is read off the procedure ending within its own limit, never off the window running out
            SejmFormation sejmOf = country.Government?.Sejm != null && country.Government.Sejm.PollingDay == poll.Date ? country.Government.Sejm : null;
            SpeakerRound roundOf = null;
            bool EndedWithout()
            {
                SpeakerRound r = s.RoundOf(id);
                if (roundOf == null && r != null && r.ReadsOn == poll.Date) { roundOf = r; }
                return !Formed(s, country, id, poll) && ((roundOf != null && roundOf.Stage == RoundStage.Concluded) || (sejmOf != null && !sejmOf.Open) || s.ItalianMandatesEndedWithout(country, poll));   // PS-7 (K204): Italy's three mandates failed
            }
            for (int day = 0; day < WorldClock.FormationWindowDays && !Formed(s, country, id, poll) && !EndedWithout(); day++)
            {
                Day(s, w, id);
                if (!ReferenceEquals(country.Government, last)) { last = country.Government; changes.Add(F("{0:yyyy-MM-dd} {1} ({2})", s.CurrentDate, Who(last), last?.Basis ?? "no basis")); }
            }
            bool formed = Formed(s, country, id, poll);
            bool endedWithout = EndedWithout();
            GovernmentRecord gov = country.Government;
            string seen = formed ? (Reappointed(country, poll)
                    ? F("{0} led by {1}, the sitting cabinet reappointed by the Sejm's steps and holding its confidence (its formation day {2:yyyy-MM-dd} kept)", string.Join("+", gov.Cabinet), gov.PmParty, gov.FormedOn)
                    : F("{0} led by {1}, formed {2:yyyy-MM-dd}", string.Join("+", gov.Cabinet), gov.PmParty, gov.FormedOn))
                : endedWithout ? (roundOf != null && roundOf.Stage == RoundStage.Concluded
                    ? F("none formed - the procedure ended within its own limit on {0:yyyy-MM-dd}, the round concluded after {1} rejection(s) of {2}", s.CurrentDate, roundOf.Rejections, SpeakerRound.ProposalLimit)
                    : id == CountryId.Italy ? F("none formed - the procedure ended within its own limit on {0:yyyy-MM-dd}, three mandates failing both chambers' confidence (art. 94) and the chambers dissolved (art. 88)", s.CurrentDate)
                    : F("none formed - the procedure ended within its own limit on {0:yyyy-MM-dd}, the Sejm's steps concluded with no cabinet elected", s.CurrentDate))
                : F("none formed by {0:yyyy-MM-dd} - THE WINDOW ENDED with the procedure still open ({1})", s.CurrentDate,
                    roundOf != null ? "the round at " + roundOf.Stage : sejmOf != null ? "the Sejm's steps at " + sejmOf.Stage : "no procedure of this election seen");
            _sb.Append("      q20: the record changed by the procedure alone - ").Append(changes.Count > 0 ? string.Join("; ", changes) : "no change").Append('\n');
            if (expected != null && !expected.Partial)
            {
                bool match = expected.Expected
                    ? formed && gov.PmParty == expected.PmParty && new HashSet<string>(gov.Cabinet).SetEquals(expected.Cabinet) && new HashSet<string>(gov.Support).SetEquals(expected.Support)
                    : !formed && endedWithout;   // M3: a projection of none holds only where the procedure itself ended forming none, not where the window ran out
                Check(match, F("q20: the night's projection is what the procedure formed - projected {0}; the procedure: {1}",
                    expected.Expected ? string.Join("+", expected.Cabinet) + " led by " + expected.PmParty + (expected.Support.Count > 0 ? " with " + string.Join("+", expected.Support) : string.Empty) : "none", seen));
            }
            else if (expected != null)
            {
                _sb.Append(F("      q20: PROJECTED TO THE FIRST BALLOT ONLY ({0}) - nothing asserted; the procedure: {1}\n", expected.Reason, seen));
            }
            if (!ofRecord.HasValue)
            {
                _sb.Append(F("    NOT ON RECORD - the record names no cabinet formed after this election (as of the record's date); the idle game's: {0}\n", seen));
                return;
            }
            string head = PartyOf(ofRecord.Value.Head);
            bool same = formed && gov.PmParty == head && new HashSet<string>(gov.Cabinet).SetEquals(ofRecord.Value.Cabinet);
            Check(same, F("q18: the record's government formed - the record: {0}, {1} (from {2:yyyy-MM-dd}); the idle game: {3}",
                ofRecord.Value.Head, string.Join("+", ofRecord.Value.Cabinet), ofRecord.Value.From, seen));
        }

        /// <summary>GATE-18 q20: a government record in one line - its cabinet, its prime minister's party, a caretaker's standing.</summary>
        private static string Who(GovernmentRecord g) =>
            g == null ? "no government" : (g.Cabinet.Count > 0 ? string.Join("+", g.Cabinet) : "no cabinet") + " led by " + (g.PmParty ?? "-") + (g.Caretaker ? " (a caretaker)" : string.Empty);

        /// <summary>The delta review's L-d: the Sejm's steps of this election closed on the sitting cabinet - the President's designee, reappointed (Art. 154
        /// ust. 1, `GovernmentRecord.Reappoint`, which ends its caretaking and keeps its formation day), holding the Sejm's confidence. That is the procedure's
        /// own government, formed, whatever its formation day says; a cabinet the steps left a caretaker is not.</summary>
        private static bool Reappointed(Country country, DateTime poll)
        {
            GovernmentRecord g = country.Government;
            return g != null && g.Sejm != null && g.Sejm.PollingDay == poll.Date && !g.Sejm.Open && !g.Caretaker && g.FormedOn <= poll;
        }

        private static bool Formed(SimulationManager s, Country country, CountryId id, DateTime poll)
        {
            GovernmentRecord g = country.Government;
            if (Reappointed(country, poll)) { return true; }   // the delta review's L-d: the steps' own outcome, its formation day kept
            if (g == null || g.OfRecord || g.FormedOn <= poll || (g.Sejm != null && g.Sejm.Open)) { return false; }   // a government seated from the record is not the game's formation
            SpeakerRound round = s.RoundOf(id);
            return round == null || round.Stage == RoundStage.Concluded;
        }

        /// <summary>One day in `GameController.Update`'s order, nothing played: yesterday's waits answered by the AI's rule for the player's party (K49), the day,
        /// the player's day tick, the player's election; on the boundary no decision anywhere, the market's turn state, then the election.</summary>
        private static void Day(SimulationManager s, World w, CountryId id)
        {
            s.IsCounterfactual = true;   // the AI's rule for a wait on the player answers only in a world it may answer for; set for that call alone
            try { s.AnswerWaitsAsTheAi(id); }
            finally { s.IsCounterfactual = false; }
            bool boundary = s.AdvanceDay();
            s.AdvanceCountryDayTick(id);
            if (!boundary) { s.HoldPlayerElection(); return; }
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in w.Countries) { decisions[c.Id] = PolicyDecision.None(); }
            s.AdvanceTurn(decisions);
            EnergyMarket.BeginTurn(w);
            s.HoldPlayerElection();
        }

        // ============================================================ Poland's 2025 presidential election, on E1's reference world
        private static void Presidential()
        {
            _sb.Append("  Poland's 2025 presidential election (E1's factors, on its reference world - RULED F4):\n");
            var host = new GameObject("Gate18Diagnostic.presidential");
            try
            {
                PresidentialReferenceWorld.Result world = PresidentialReferenceWorld.Hold(host);
                PresidentialElection.Contest contest = world.Contest;
                if (contest == null) { Check(false, "the reference world held no first vote"); return; }
                IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> ofRecord = PresidencyOfRecord.CandidatesOf(CountryId.Poland, 2025);
                double worst = 0.0;
                string worstName = null;
                var oosRaw = new List<(string Name, double Raw, PresidentialElection.Candidate In)>();
                foreach (PresidencyOfRecord.CandidateOfRecord c in ofRecord)
                {
                    string name = PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, c.Surname);
                    PresidentialElection.Candidate f = contest.Field.FirstOrDefault(x => x.Name == name);
                    double record = 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname);
                    double miss = f == null ? double.PositiveInfinity : Math.Abs(f.Share - record);
                    if (miss > worst) { worst = miss; worstName = name; }
                    double raw = c.BackingParty != null && world.PollOnFirstVote.TryGetValue(c.BackingParty, out double poll) ? poll : PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname);
                    oosRaw.Add((name, raw, f));
                }
                double rawSum = oosRaw.Sum(x => x.Raw);
                foreach ((string name, double raw, PresidentialElection.Candidate f) in oosRaw)
                {
                    PresidencyOfRecord.CandidateOfRecord c = ofRecord.First(x => PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, x.Surname) == name);
                    _sb.Append(F("      {0,-24} record {1,6:0.00}  game {2,6:0.00}  |  out of sample (factor one{3}) {4,6:0.00}\n", name, 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname),
                        f?.Share ?? double.NaN, c.BackingParty == null ? ", its share of record as ruled" : string.Empty, 100.0 * raw / rawSum));
                }
                Check(worst <= TolerancePp, F("q18: every first-round candidate of record within {0} points of the PKW's - the largest miss {1:0.00} pp ({2})", TolerancePp, worst, worstName ?? "none"));

                IReadOnlyList<(string Candidate, long Votes)> runOff = TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 2, out _);
                (string Candidate, long Votes) winner = runOff.OrderByDescending(v => v.Votes).First();
                double winnerOfRecord = 100.0 * winner.Votes / runOff.Sum(v => v.Votes);
                string winnerName = contest.Field.Select(x => x.Name).FirstOrDefault(n => winner.Candidate.StartsWith(WorldClock.SurnameOf(n).ToUpperInvariant(), StringComparison.Ordinal));
                double winnerGame = contest.RunOffA == winnerName ? contest.RunOffShareA : 100.0 - contest.RunOffShareA;
                Check(world.RunOffStepped && contest.RunOffHeld && contest.Decided() && winnerName != null && contest.Elected == winnerName && Math.Abs(winnerGame - winnerOfRecord) <= TolerancePp,
                    F("q18: the run-off elects the record's winner ({0}) - {1:0.00} % of the two against the record's {2:0.00} (a miss of {3:+0.00;-0.00}, within {4}): {5}",
                        winnerName ?? winner.Candidate, winnerGame, winnerOfRecord, winnerGame - winnerOfRecord, TolerancePp, contest.Line));

                // beside it: the run-off the first round at factor one leads to
                var outOf = oosRaw.Where(x => x.In != null).Select(x => (x.Name, Share: 100.0 * x.Raw / rawSum, x.In)).OrderByDescending(x => x.Share).ToList();
                if (outOf.Count >= 2)
                {
                    var firstRound = outOf.Select(x => (x.Name, x.Share, x.In.Placed ? new PresidentialElection.Point(x.In.Galtan, x.In.Nationalism, x.In.Eu) : (PresidentialElection.Point?)null)).ToList();
                    var a = outOf[0].In; var b = outOf[1].In;
                    double shareA = PresidentialElection.RunOffShare(firstRound, a.Name, b.Name, new PresidentialElection.Point(a.Galtan, a.Nationalism, a.Eu), new PresidentialElection.Point(b.Galtan, b.Nationalism, b.Eu),
                        PresidentialElection.TransferTau, PresidentialElection.AbstentionDraftMax, out double _);
                    _sb.Append(F("      out of sample: the first round at factor one sends {0} and {1} to the run-off - {0} {2:0.00} % of the two\n", a.Name, b.Name, shareA));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        // ============================================================ helpers
        /// <summary>The party of the government of record's head on <paramref name="day"/> - the start's prime minister's party; null where the record holds none.</summary>
        private static string IncumbentParty(CountryId id, DateTime day) =>
            WorldClock.TryGovernmentAt(id, day, out WorldClock.GovernmentOfRecord g) ? PartyOf(g.Head) ?? (g.Cabinet != null && g.Cabinet.Length > 0 ? g.Cabinet[0] : null) : null;

        /// <summary>"Donald Tusk (KO)" - KO: the party in brackets of a head of record.</summary>
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
