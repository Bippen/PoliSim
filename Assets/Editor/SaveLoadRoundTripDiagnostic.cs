using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Item 8's acceptance gate: proves a saved game restores into a fresh SimulationManager and
    /// CONTINUES IDENTICALLY - the same bar SimulationRandomRestoreDiagnostic set for the RNG layer,
    /// generalized to the whole state (its check 2: a restore that rewinds or drifts looks valid in
    /// every way except the continuation, so the continuation is what gets compared).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SaveLoadRoundTripDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Per scenario (all six countries as player x two seeds): run N turns plus a MID-TURN offset
    /// (a real save is a mid-day state, and a mid-period fiscal accrual plus mid-flight bills are
    /// exactly the state the mechanism report's hazards live in), introduce bills so the pending
    /// surface is non-empty, save; run M more turns recording a full per-turn snapshot; restore the
    /// save into a FRESH manager; run the same M turns; compare everything. The day loop mirrors
    /// GameController.Update exactly (AdvanceDay, then AdvanceCountryDayTick, then AdvanceTurn on
    /// the boundary) so the state saved is a state play can actually produce.
    ///
    /// **The asserts, mapped to the mechanism report's hazards:**
    /// - Eurozone zone identity: SaveGameService.Deserialize's own assert (hazard 1), plus the
    ///   re-serialize equality below.
    /// - Re-serialized restored save == original save, string-equal (the master assert: append
    ///   duplication, dropped members, cadence-field loss, tuple-dict loss all land here).
    /// - Snapshot-at-restore == snapshot-at-save: every EconomyState field by reflection (so a new
    ///   field is compared without editing this file), every collection COUNT (hazard 3 named),
    ///   division count + last number (hazard 4 named), every pending structure (day counters).
    /// - RNG draw counts after restore == the saved counts (the existing diagnostic composed in,
    ///   not duplicated - RestoreInto calls the proven RestoreState).
    /// - Trajectory A == trajectory B per turn, per country, per field, exactly - cadence resets
    ///   surface as History count divergence on the first post-restore turn, division renumbering
    ///   as LastNumber divergence when the next division fires.
    /// - endstate save A == endstate save B, string-equal.
    /// - Once per run: file IO (atomic write, re-write over existing, load-back equality) and the
    ///   version gate (a tampered SaveVersion must refuse with SaveLoadException - ruling A).
    ///
    /// ⚠ A divergence here is a FINDING to report, never something to tune away - per the pass
    /// directive. Layer 3 (UI drafts) is structurally out of reach (no OnGUI in batch); that gap is
    /// recorded beside the folder-tongue hover items in CLAUDE.md's open-verification-gap block.
    /// </summary>
    public static class SaveLoadRoundTripDiagnostic
    {
        private const int TurnsBeforeSave = 8;
        private const int TurnsAfterSave = 8;

        /// <summary>Days past the last pre-save turn boundary at which the save is taken - deliberately
        /// NOT a boundary, so the fiscal period is mid-accrual and the calendar mid-turn.</summary>
        private const int MidTurnSaveOffsetDays = 37;

        /// <summary>Bills are introduced this many days into the mid-turn walk, so their 21-day
        /// countdowns are partially consumed at the save (21 - (37-27) = 11 days remaining).</summary>
        private const int BillIntroduceOffsetDays = 27;

        private static readonly int[] Seeds = { 777, 424242 };

        private static FieldInfo[] _stateFields;

        public static void Run()
        {
            _stateFields = typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance);
            Debug.Log($"RT: reflecting {_stateFields.Length} public EconomyState fields into every snapshot.");

            int failures = 0;
            int scenarios = 0;
            bool ioAndVersionProven = false;

            foreach (int seed in Seeds)
            {
                foreach (CountryId player in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    scenarios++;
                    try
                    {
                        bool ok = RunScenario(seed, player, exerciseFileIoAndVersionGate: !ioAndVersionProven);
                        if (ok)
                        {
                            ioAndVersionProven = true;
                        }
                        else
                        {
                            failures++;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"RT: {player} seed {seed} THREW: {e}");
                        failures++;
                    }
                }
            }

            Debug.Log(failures == 0
                ? $"RT: PASS - {scenarios} scenarios (6 countries x {Seeds.Length} seeds) round-trip clean."
                : $"RT: FAIL - {failures} of {scenarios} scenarios failed. A divergence is a finding - report it, do not tune it away.");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static bool RunScenario(int seed, CountryId player, bool exerciseFileIoAndVersionGate)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();
            var goA = new GameObject("RT_A");
            var goB = new GameObject("RT_B");
            try
            {
                SimulationManager simA = goA.AddComponent<SimulationManager>();
                simA.SetWorld(world);
                Country playerCountry = world.GetCountry(player);

                // A minister so cabinet decisions can roll and sit PENDING across the save - the
                // candidate shuffle consumes Cabinet-stream draws pre-save, which the saved draw
                // counts then cover; appointment itself is just the public dictionary, exactly what
                // the UI's appointment click writes.
                playerCountry.CabinetMinisters[CabinetPortfolio.FinanceTreasury] =
                    CabinetSystem.GenerateCandidates(CabinetPortfolio.FinanceTreasury)[0];

                // R4-4 bar item, confirmed-not-assumed: a NEW-portfolio appointment (and any
                // pending decision it rolls) must actually cross a save in this run - enum growth
                // being append-only makes old saves fine by construction, but the new keys
                // round-tripping is the claim under test, so one of the three new portfolios is
                // appointed here alongside the original coverage.
                playerCountry.CabinetMinisters[CabinetPortfolio.Defense] =
                    CabinetSystem.GenerateCandidates(CabinetPortfolio.Defense)[0];

                Dictionary<CountryId, PolicyDecision> decisionsA = BuildNoOpDecisions(world);

                RunDays(simA, world, decisionsA, player, TurnsBeforeSave * SimulationManager.DaysPerTurn, null);
                RunDays(simA, world, decisionsA, player, BillIntroduceOffsetDays, null);
                IntroduceCoverageBills(simA, world, player, playerCountry);
                RunDays(simA, world, decisionsA, player, MidTurnSaveOffsetDays - BillIntroduceOffsetDays, null);

                // One fixed stamp for every serialization in the scenario, so save files are
                // string-comparable - SavedAtUtc is the single nondeterministic field.
                var stamp = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
                SaveGame save = SaveGameService.CreateSaveGame(simA, world, player, null, stamp);
                string json = SaveGameService.Serialize(save);
                Dictionary<string, double> snapAtSave = SnapshotAll(simA, world);
                Dictionary<SimulationRandom.Stream, int> drawCountsAtSave = SimulationRandom.CaptureDrawCounts();

                bool ok = true;
                if (exerciseFileIoAndVersionGate)
                {
                    ok &= ExerciseFileIoAndVersionGate(json);
                }

                // A's continuation, ending exactly on a turn boundary.
                int continuationDays = TurnsAfterSave * SimulationManager.DaysPerTurn - MidTurnSaveOffsetDays;
                var trajectoryA = new List<Dictionary<string, double>>();
                RunDays(simA, world, decisionsA, player, continuationDays, trajectoryA);
                string endJsonA = SaveGameService.Serialize(SaveGameService.CreateSaveGame(simA, world, player, null, stamp));

                // Restore into a FRESH manager. Deserialize runs the zone-identity assert itself.
                SaveGame loaded = SaveGameService.Deserialize(json);
                SimulationManager simB = goB.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                World worldB = loaded.World;

                string jsonB = SaveGameService.Serialize(SaveGameService.CreateSaveGame(simB, worldB, player, null, stamp));
                if (jsonB != json)
                {
                    ReportStringDiff($"{player}/{seed} re-serialized restored save != original save", json, jsonB);
                    ok = false;
                }

                ok &= CompareSnapshots($"{player}/{seed} restore-point", snapAtSave, SnapshotAll(simB, worldB));
                ok &= CompareDrawCounts($"{player}/{seed}", drawCountsAtSave, SimulationRandom.CaptureDrawCounts());

                Dictionary<CountryId, PolicyDecision> decisionsB = BuildNoOpDecisions(worldB);
                var trajectoryB = new List<Dictionary<string, double>>();
                RunDays(simB, worldB, decisionsB, player, continuationDays, trajectoryB);
                string endJsonB = SaveGameService.Serialize(SaveGameService.CreateSaveGame(simB, worldB, player, null, stamp));

                ok &= CompareTrajectories($"{player}/{seed}", trajectoryA, trajectoryB);
                if (endJsonB != endJsonA)
                {
                    ReportStringDiff($"{player}/{seed} end-state save B != end-state save A", endJsonA, endJsonB);
                    ok = false;
                }

                if (ok)
                {
                    Debug.Log($"RT: {player} seed {seed} OK - {trajectoryA.Count} continuation turns identical, restore-point snapshot identical, saves string-equal.");
                }

                return ok;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(goA);
                UnityEngine.Object.DestroyImmediate(goB);
            }
        }

        /// <summary>The controller's own day, in the controller's own order: AdvanceDay, the player
        /// country's AdvanceCountryDayTick, AdvanceTurn on the boundary - then the deterministic
        /// resolution of any pending cabinet decisions (Options[0]), which in the B run doubles as
        /// the proof that a RESTORED decision instance resolves through the reference-equality
        /// removal path (mechanism report, hazard 5's copy note).</summary>
        private static void RunDays(SimulationManager sim, World world, Dictionary<CountryId, PolicyDecision> decisions,
            CountryId player, int days, List<Dictionary<string, double>> trajectory)
        {
            for (int day = 0; day < days; day++)
            {
                bool boundary = sim.AdvanceDay();
                sim.AdvanceCountryDayTick(player);
                if (!boundary)
                {
                    continue;
                }

                sim.AdvanceTurn(decisions);
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in
                         new List<(CabinetPortfolio, CabinetDecision)>(sim.GetPendingCabinetDecisions(player)))
                {
                    sim.ResolveCabinetDecision(player, portfolio, decision, decision.Options[0]);
                }

                trajectory?.Add(SnapshotAll(sim, world));
            }
        }

        private static Dictionary<CountryId, PolicyDecision> BuildNoOpDecisions(World world)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country country in world.Countries)
            {
                decisions[country.Id] = PolicyDecision.None();
            }

            return decisions;
        }

        /// <summary>Non-empty pending state to carry across the save: three player bills whose 21-day
        /// countdowns are mid-flight at the save, plus two bills on a neighbour country - which never
        /// count down (only the player's day ticks), so they persist as stable pending entries through
        /// both continuations.</summary>
        private static void IntroduceCoverageBills(SimulationManager sim, World world, CountryId player, Country playerCountry)
        {
            sim.IntroduceBudgetBill(player, new BudgetBill());
            sim.IntroduceLaborBill(player, new LaborPolicyBill
            {
                MinimumWage = playerCountry.MinimumWagePercentOfMedian,
                PaidFamilyLeaveWeeks = playerCountry.PaidFamilyLeaveWeeks,
                OvertimeRegulation = playerCountry.OvertimeRegulationLevel,
                RetrainingProgram = playerCountry.RetrainingProgramLevel,
                FamilyPolicy = playerCountry.FamilyPolicyLevel,
                ImmigrationPolicy = playerCountry.ImmigrationPolicyLevel
            });
            sim.IntroduceTaxProgramBill(player, TaxType.CarbonTax, isAdd: true);

            int playerIndex = world.Countries.FindIndex(c => c.Id == player);
            Country neighbour = world.Countries[(playerIndex + 1) % world.Countries.Count];
            sim.IntroduceTaxProgramBill(neighbour.Id, TaxType.StampDuty, isAdd: true);
            var welfareTypes = (WelfareProgramType[])Enum.GetValues(typeof(WelfareProgramType));
            sim.IntroduceWelfareProgramBill(neighbour.Id, welfareTypes[0], isAdd: true);
        }

        private static Dictionary<string, double> SnapshotAll(SimulationManager sim, World world)
        {
            var snap = new Dictionary<string, double>
            {
                ["Sim.CurrentTurn"] = sim.CurrentTurn,
                ["Sim.DaysSinceEpoch"] = (sim.CurrentDate - SimulationManager.EpochDate).TotalDays
            };

            foreach (Country country in world.Countries)
            {
                string p = country.Id.ToString();
                foreach (FieldInfo field in _stateFields)
                {
                    snap[$"{p}.State.{field.Name}"] = ToDouble(field.GetValue(country.State));
                }

                snap[$"{p}.Zone.InterestRate"] = country.CurrencyZone?.InterestRate ?? -999.0;
                snap[$"{p}.Divisions.Count"] = country.Divisions.Entries.Count;
                snap[$"{p}.Divisions.LastNumber"] = country.Divisions.Entries.Count > 0
                    ? country.Divisions.Entries[country.Divisions.Entries.Count - 1].Number
                    : 0;
                snap[$"{p}.History.GdpDaily.Count"] = country.History.Gdp.Daily.Count;
                snap[$"{p}.History.GdpWeekly.Count"] = country.History.Gdp.Weekly.Count;
                snap[$"{p}.History.GdpMonthly.Count"] = country.History.Gdp.Monthly.Count;
                snap[$"{p}.History.GdpQuarterly.Count"] = country.History.Gdp.Quarterly.Count;
                snap[$"{p}.History.UnemploymentDaily.Count"] = country.History.Unemployment.Daily.Count;

                int publishedEntries = 0;
                foreach (KeyValuePair<PublishedStat, PublishedSeries> pair in country.Published.Series)
                {
                    publishedEntries += pair.Value.Entries.Count;
                }

                snap[$"{p}.Published.SeriesCount"] = country.Published.Series.Count;
                snap[$"{p}.Published.TotalEntries"] = publishedEntries;
                snap[$"{p}.Published.PeriodClosings"] = country.Published.PeriodClosingValues.Count;

                snap[$"{p}.TaxLines.Count"] = country.TaxLines.Count;
                snap[$"{p}.WelfarePrograms.Count"] = country.WelfarePrograms.Count;
                snap[$"{p}.Sectors.Count"] = country.Sectors.Count;
                snap[$"{p}.Infrastructure.Count"] = country.InfrastructureAssets.Count;
                snap[$"{p}.SpendingLines.Count"] = country.SpendingLines.Count;
                snap[$"{p}.TradePartners.Count"] = country.TradePartners.Count;
                snap[$"{p}.CabinetMinisters.Count"] = country.CabinetMinisters.Count;
                snap[$"{p}.ParliamentSeats.Count"] = country.ParliamentSeats.Count;
                snap[$"{p}.Swf.Exists"] = country.SovereignWealthFund != null ? 1 : 0;

                snap[$"{p}.Pending.BudgetBillDays"] = sim.GetPendingBudgetBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.LaborBillDays"] = sim.GetPendingLaborBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.CrimeBillDays"] = sim.GetPendingCrimeJusticeBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.SectorBillDays"] = sim.GetPendingSectorBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.TradeBillDays"] = sim.GetPendingTradeBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.SwfBillDays"] = sim.GetPendingSwfDrawdownBill(country.Id)?.DaysRemaining ?? -1;
                snap[$"{p}.Pending.FpMeeting"] = sim.GetPendingForeignPolicyMeeting(country.Id) != null ? 1 : 0;
                snap[$"{p}.Pending.CabinetCount"] = sim.GetPendingCabinetDecisions(country.Id).Count;
                snap[$"{p}.Pending.BudgetProcess"] = sim.GetPendingBudgetProcess(country.Id) ? 1 : 0;
            }

            return snap;
        }

        private static double ToDouble(object value)
        {
            switch (value)
            {
                case float f: return f;
                case int i: return i;
                case bool b: return b ? 1 : 0;
                case double d: return d;
                default: return value == null ? double.NaN : Convert.ToDouble(value);
            }
        }

        private static bool CompareSnapshots(string label, Dictionary<string, double> expected, Dictionary<string, double> actual)
        {
            var mismatches = new List<string>();
            foreach (KeyValuePair<string, double> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out double value))
                {
                    mismatches.Add($"{pair.Key}: missing (expected {pair.Value:R})");
                }
                else if (!value.Equals(pair.Value))
                {
                    mismatches.Add($"{pair.Key}: {pair.Value:R} -> {value:R}");
                }
            }

            foreach (string key in actual.Keys)
            {
                if (!expected.ContainsKey(key))
                {
                    mismatches.Add($"{key}: unexpected extra key");
                }
            }

            if (mismatches.Count == 0)
            {
                return true;
            }

            Debug.LogError($"RT: {label} snapshot mismatch - {mismatches.Count} field(s):");
            for (int i = 0; i < Math.Min(mismatches.Count, 12); i++)
            {
                Debug.LogError($"  {mismatches[i]}");
            }

            return false;
        }

        private static bool CompareDrawCounts(string label,
            Dictionary<SimulationRandom.Stream, int> expected, Dictionary<SimulationRandom.Stream, int> actual)
        {
            foreach (SimulationRandom.Stream stream in (SimulationRandom.Stream[])Enum.GetValues(typeof(SimulationRandom.Stream)))
            {
                int e = expected.TryGetValue(stream, out int ev) ? ev : 0;
                int a = actual.TryGetValue(stream, out int av) ? av : 0;
                if (e != a)
                {
                    Debug.LogError($"RT: {label} RNG stream {stream} draw count {e} -> {a} after restore.");
                    return false;
                }
            }

            return true;
        }

        private static bool CompareTrajectories(string label,
            List<Dictionary<string, double>> a, List<Dictionary<string, double>> b)
        {
            if (a.Count != b.Count)
            {
                Debug.LogError($"RT: {label} continuation turn counts differ - A {a.Count}, B {b.Count}.");
                return false;
            }

            for (int turn = 0; turn < a.Count; turn++)
            {
                if (!CompareSnapshots($"{label} continuation turn {turn + 1}", a[turn], b[turn]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Atomic write, overwrite-with-backup, load-back equality, and ruling A's version
        /// gate - exercised once per run against a temp path, since the mechanics do not vary by
        /// scenario.</summary>
        private static bool ExerciseFileIoAndVersionGate(string json)
        {
            string path = Path.Combine(Path.GetTempPath(), "polisim_rt_slot.json");
            try
            {
                SaveGame save = SaveGameService.Deserialize(json);
                SaveGameService.SaveToFile(path, save);
                SaveGameService.SaveToFile(path, save);
                if (!File.Exists(path + ".bak"))
                {
                    Debug.LogError("RT: second SaveToFile left no .bak - the atomic overwrite path is broken.");
                    return false;
                }

                SaveGame loadedBack = SaveGameService.LoadFromFile(path);
                if (loadedBack.CurrentTurn != save.CurrentTurn || loadedBack.World.Countries.Count != save.World.Countries.Count)
                {
                    Debug.LogError("RT: file round trip lost state (turn or country count changed).");
                    return false;
                }

                string tampered = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion,
                    "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion + 1));
                if (tampered == json)
                {
                    Debug.LogError("RT: version-gate probe could not tamper the version field - the probe itself is broken, verified nothing.");
                    return false;
                }

                try
                {
                    SaveGameService.Deserialize(tampered);
                    Debug.LogError("RT: a wrong-version save LOADED - ruling A's refuse-load gate is not working.");
                    return false;
                }
                catch (SaveLoadException e)
                {
                    Debug.Log($"RT: version gate refused as ruled: \"{e.Message}\"");
                }

                Debug.Log("RT: file IO (atomic write, .bak, load-back) and version gate OK.");
                return true;
            }
            finally
            {
                foreach (string stale in new[] { path, path + ".bak", path + ".tmp" })
                {
                    if (File.Exists(stale))
                    {
                        File.Delete(stale);
                    }
                }
            }
        }

        private static void ReportStringDiff(string label, string expected, string actual)
        {
            int firstDiff = -1;
            int max = Math.Min(expected.Length, actual.Length);
            for (int i = 0; i < max; i++)
            {
                if (expected[i] != actual[i])
                {
                    firstDiff = i;
                    break;
                }
            }

            if (firstDiff < 0)
            {
                firstDiff = max;
            }

            int start = Math.Max(0, firstDiff - 120);
            string expectedContext = expected.Substring(start, Math.Min(240, expected.Length - start));
            string actualContext = actual.Substring(start, Math.Min(240, actual.Length - start));
            Debug.LogError($"RT: {label} - lengths {expected.Length} vs {actual.Length}, first difference at {firstDiff}.");
            Debug.LogError($"  expected ...{expectedContext}...");
            Debug.LogError($"  actual   ...{actualContext}...");
        }
    }
}
