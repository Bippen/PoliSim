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
            // Ruling 1 (2026-08-25): this diagnostic advances turns, so the ledger self-audits fire
            // during it - and this is the exact harness whose "RT: PASS" hid 24 ATTRIB lines for a
            // day. Arm the fold: any error logged during the run, its own or the simulation's, now
            // exits nonzero regardless of `failures`.
            CheckExit.ArmLogFold();
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
                // Step 2 (R-S2e): the ledger's save-time values, held aside so the post-restore
                // compare below is EXPLICIT rather than implied by the whole-save string equality
                // - the claim under test is the exact failure the no-case predicted: a load that
                // silently blanks the explanation.
                ApprovalAttribution ledgerAtSave = world.GetCountry(player).ApprovalLedgerLastPeriod;
                float ledgerTermSumAtSave = ledgerAtSave?.TermSum ?? float.NaN;
                float ledgerEventSumAtSave = ledgerAtSave?.EventSum ?? float.NaN;
                int ledgerEventCountAtSave = ledgerAtSave?.Events.Count ?? -1;
                int accruingEventCountAtSave = world.GetCountry(player).ApprovalLedgerAccruing?.Events.Count ?? -1;
                // Step 2's third section (2026-08-25): the DEBT ledger's save-time values, held aside
                // the same way - the save is taken 37 days into a period, so the accruing ledger has
                // 37 observed slices at this point and the closed one a full 365: a real crossing of
                // both, per R-S2e, asserted explicitly below rather than implied by string equality.
                DebtAttribution fiscalAtSave = world.GetCountry(player).FiscalLedgerLastPeriod;
                float fiscalTermSumAtSave = fiscalAtSave?.TermSum ?? float.NaN;
                int fiscalDaysAtSave = fiscalAtSave?.DaysRecorded ?? -1;
                int fiscalEventCountAtSave = fiscalAtSave?.Events.Count ?? -1;
                int fiscalAccruingDaysAtSave = world.GetCountry(player).FiscalLedgerAccruing?.DaysRecorded ?? -1;
                float fiscalAccruingTermSumAtSave = world.GetCountry(player).FiscalLedgerAccruing?.TermSum ?? float.NaN;
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

                // Step 2 (R-S2e): the restored ledger must BE the saved period - the trace panel
                // reads exactly these fields after a load.
                ApprovalAttribution ledgerRestored = worldB.GetCountry(player).ApprovalLedgerLastPeriod;
                float restoredTermSum = ledgerRestored?.TermSum ?? float.NaN;
                float restoredEventSum = ledgerRestored?.EventSum ?? float.NaN;
                int restoredEventCount = ledgerRestored?.Events.Count ?? -1;
                int restoredAccruingCount = worldB.GetCountry(player).ApprovalLedgerAccruing?.Events.Count ?? -1;
                bool ledgerOk = restoredEventCount == ledgerEventCountAtSave
                    && restoredAccruingCount == accruingEventCountAtSave
                    && (float.IsNaN(ledgerTermSumAtSave) ? float.IsNaN(restoredTermSum) : Mathf.Abs(restoredTermSum - ledgerTermSumAtSave) < 1e-4f)
                    && (float.IsNaN(ledgerEventSumAtSave) ? float.IsNaN(restoredEventSum) : Mathf.Abs(restoredEventSum - ledgerEventSumAtSave) < 1e-4f);
                if (!ledgerOk)
                {
                    Debug.LogError($"RT: {player}/{seed} APPROVAL LEDGER did not cross the save - " +
                                   $"terms {ledgerTermSumAtSave:F4}->{restoredTermSum:F4}, events {ledgerEventCountAtSave}->{restoredEventCount}, " +
                                   $"accruing {accruingEventCountAtSave}->{restoredAccruingCount}. The post-load panel would show a blank or wrong period.");
                    ok = false;
                }

                // Step 2's third section: the restored DEBT ledger must BE the saved one - the
                // closed period (its term sum, day count and events) and the mid-period accruing
                // ledger's 37 observed slices, so a load lands on a panel that says what the save
                // said and an audit that continues from where the save's period actually stood.
                Country restoredPlayer = worldB.GetCountry(player);
                DebtAttribution fiscalRestored = restoredPlayer.FiscalLedgerLastPeriod;
                float fiscalRestoredTermSum = fiscalRestored?.TermSum ?? float.NaN;
                int fiscalRestoredDays = fiscalRestored?.DaysRecorded ?? -1;
                int fiscalRestoredEvents = fiscalRestored?.Events.Count ?? -1;
                int fiscalRestoredAccruingDays = restoredPlayer.FiscalLedgerAccruing?.DaysRecorded ?? -1;
                float fiscalRestoredAccruingTermSum = restoredPlayer.FiscalLedgerAccruing?.TermSum ?? float.NaN;
                bool fiscalOk = fiscalRestoredDays == fiscalDaysAtSave
                    && fiscalRestoredEvents == fiscalEventCountAtSave
                    && fiscalRestoredAccruingDays == fiscalAccruingDaysAtSave
                    && (float.IsNaN(fiscalTermSumAtSave) ? float.IsNaN(fiscalRestoredTermSum) : Mathf.Abs(fiscalRestoredTermSum - fiscalTermSumAtSave) < 1e-3f)
                    && (float.IsNaN(fiscalAccruingTermSumAtSave) ? float.IsNaN(fiscalRestoredAccruingTermSum) : Mathf.Abs(fiscalRestoredAccruingTermSum - fiscalAccruingTermSumAtSave) < 1e-3f);
                if (!fiscalOk)
                {
                    Debug.LogError($"RT: {player}/{seed} DEBT LEDGER did not cross the save - " +
                                   $"closed: terms {fiscalTermSumAtSave:F4}->{fiscalRestoredTermSum:F4}, days {fiscalDaysAtSave}->{fiscalRestoredDays}, events {fiscalEventCountAtSave}->{fiscalRestoredEvents}; " +
                                   $"accruing: days {fiscalAccruingDaysAtSave}->{fiscalRestoredAccruingDays}, terms {fiscalAccruingTermSumAtSave:F4}->{fiscalRestoredAccruingTermSum:F4}. " +
                                   "The post-load debt trace would show a blank or wrong period, or the next audit would fail on a period that lost its slices.");
                    ok = false;
                }

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

            // Pass 6 (2026-08-27): a standing partner override (the immediate "Set Override" click,
            // initialised at the effective rate exactly as GameController performs it) plus a pending
            // Trade bill requesting a different rate on it cross the save - so PendingTradeBills is
            // non-empty, the direction path (overrides in the vote) resolves on both sides, and the
            // retaliation term the partner's resolved rate now carries is exercised across the save.
            // Added in the plumbing commit with every TradeCosts constant at 0, so the wired-inert
            // control proves the coverage change itself inert.
            if (playerCountry.TradePartners.Count > 0)
            {
                TradePartner firstLink = playerCountry.TradePartners[0];
                Country firstPartner = world.GetCountry(firstLink.PartnerId);
                firstLink.PlayerTariffOverride = TradeSystem.GetOwnTariffRate(playerCountry, firstPartner, world.TradeBlocs);
                var tradeCoverageBill = new TradePolicyBill { NewBaseTariffRate = playerCountry.BaseTariffRate };
                tradeCoverageBill.PartnerTariffOverrides[firstLink.PartnerId] = 5f;
                sim.IntroduceTradeBill(player, tradeCoverageBill);
            }

            // Law system MVP slice: an already-enacted law (exercises Country.EnactedLaws' plain
            // List<T> round trip - the World layer, not the pending-state layer) plus a pending
            // LawBill for a DIFFERENT law (exercises the nested CountryId -> LawId dictionary the
            // exact same "populate-in-place on a readable member" hazard the original save/load
            // pass found on PublishedData.PeriodClosingValues - see the mechanism report's hazard 5).
            // Content-marathon end-of-run bar: "a dozen laws in force crossing the save" - a dozen
            // enacted, drawn across every batch and every dial (not just batch 1's original two), so
            // the round trip is proving diverse real content, not one repeated pair. Extended
            // 2026-08-25 (batches 4-5, 38 -> 50) with three laws from the two new batches - the weak-
            // proxy anti-mafia law (exercises a law whose comment explicitly documents an
            // unrepresented mechanism, not just a clean single-effect one), and one each from the
            // newly-touched BorderEnforcement (National Guard deployment) and BailReform (the
            // Pretrial Services Agency's dual-dial shape) growth.
            // Pass 3 (2026-08-26): laws from BOTH categories cross the save - the pass's own bar
            // names this explicitly. Four labor laws drawn across batches 1/2/3/5, including the
            // multi-dial flexicurity package (weak-proxy shape) and the dual-dial skills levy, so
            // the round trip proves the twelve-field DialDeltas shape and the two-book labor
            // state, not just C&J content a second time.
            string[] lawsToEnact =
            {
                "truth_in_sentencing_act", "border_security_act", "community_policing_initiative",
                "cash_bail_abolition_act", "drug_decriminalization_act", "public_defender_funding_act",
                "body_worn_camera_program", "court_backlog_reduction_program", "frontex_border_cooperation_agreement",
                "restorative_justice_program", "mental_health_diversion_courts", "human_trafficking_task_force",
                "antimafia_asset_confiscation_law", "national_guard_border_deployment", "pretrial_services_agency_establishment",
                "raise_the_wage_act", "immigration_restriction_act", "flexicurity_package_act",
                "immigration_skills_levy_act"
            };
            foreach (string lawId in lawsToEnact)
            {
                playerCountry.EnactedLaws.Add(new EnactedLaw { LawId = lawId, EnactedOn = sim.CurrentDate });
            }

            // Pass 3: run both category recomputes after the direct adds (reflection, the
            // LaborLawCompositionDiagnostic/UiScreenshotDriver idiom) so the SAVED state is the
            // internally consistent one real enactment produces - labor dials genuinely offset
            // from their bases across the save, C&J dials law-driven, instead of an enacted list
            // whose dials never moved.
            foreach (string recompute in new[] { "RecomputeCrimeJusticeDialsFromEnactedLaws", "RecomputeLaborDialsFromEnactedLaws" })
            {
                MethodInfo recomputeMethod = typeof(SimulationManager).GetMethod(recompute, BindingFlags.Instance | BindingFlags.NonPublic);
                if (recomputeMethod == null)
                {
                    Debug.LogError($"RT: SimulationManager.{recompute} not found by reflection - the enacted-law coverage state is INCONSISTENT, not clean.");
                }
                else
                {
                    recomputeMethod.Invoke(sim, new object[] { playerCountry });
                }
            }

            sim.IntroduceLawBill(player, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
            sim.IntroduceLawBill(player, new LawBill { LawId = "sanctuary_city_policy", IsRepeal = false });
            // Pass 3: a labor-category law bill pending alongside the two C&J ones.
            sim.IntroduceLawBill(player, new LawBill { LawId = "parental_quota_act", IsRepeal = false });

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
                // Law system MVP slice: enacted-law count, plus how many law bills are currently
                // pending (summed across every LawId, since multiple can be pending at once) -
                // mirrors the existing per-bill-kind pending-days lines below for the countdown side.
                snap[$"{p}.EnactedLaws.Count"] = country.EnactedLaws.Count;
                snap[$"{p}.Pending.LawBillCount"] = sim.GetPendingLawBills(country.Id).Count;
                // Pass 3 (coexistence): the six statutory-base fields cross the save alongside the
                // effective dials - a dropped base would silently re-anchor every labor law's
                // offset after a load, so it fails HERE by name instead of surfacing as drift.
                snap[$"{p}.Labor.MinimumWageBase"] = country.MinimumWagePercentOfMedianBase;
                snap[$"{p}.Labor.PaidLeaveBase"] = country.PaidFamilyLeaveWeeksBase;
                snap[$"{p}.Labor.OvertimeBase"] = country.OvertimeRegulationBase;
                snap[$"{p}.Labor.RetrainingBase"] = country.RetrainingProgramBase;
                snap[$"{p}.Labor.FamilyBase"] = country.FamilyPolicyBase;
                snap[$"{p}.Labor.ImmigrationBase"] = country.ImmigrationPolicyBase;
                snap[$"{p}.Infrastructure.Count"] = country.InfrastructureAssets.Count;
                snap[$"{p}.SpendingLines.Count"] = country.SpendingLines.Count;
                snap[$"{p}.TradePartners.Count"] = country.TradePartners.Count;
                snap[$"{p}.CabinetMinisters.Count"] = country.CabinetMinisters.Count;
                snap[$"{p}.ParliamentSeats.Count"] = country.ParliamentSeats.Count;
                snap[$"{p}.Swf.Exists"] = country.SovereignWealthFund != null ? 1 : 0;
                // R4 (maturity rate-lag): the mechanism's one piece of state, snapshotted so a
                // save/load that dropped it would fail HERE rather than silently reverting a
                // loaded game to instant repricing via the sentinel fallback.
                snap[$"{p}.EffectiveDebtRate"] = country.EffectiveDebtInterestRate;

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
