using System;
using System.Collections.Generic;
using System.Linq;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// Debug tool: runs the default six-country World for a configurable number of turns, logging
    /// per-turn/per-country state and a final summary flagging anything that looks like a runaway
    /// feedback loop or invalid value. Not production code.
    ///
    /// Reads three optional command-line arguments (so `Assets/Editor/BatchSimulationRunner.cs` can
    /// drive it headlessly without touching this file per run - see CLAUDE.md's "Real-Unity
    /// Validation is the Standard Path" for why this replaced the standalone harness as the primary
    /// validation tool):
    /// -turns=N (default 100) - how many turns to run.
    /// -scenario=baseline|stress|sustainedexploit|tariffoverride|welfarestress|swfstress|
    /// phase2stress|laborstress|crimejusticestress|infrastructurestress|deferredmaintenance|
    /// growthstackstress|demographicpolicystress (default baseline) - baseline is
    /// PolicyDecision.None() for every country every turn (the original behavior, unchanged); most of
    /// the rest mirror the standalone harness's own same-named scenarios byte-for-byte (same targets,
    /// same rates, same turn timing) so both tools exercise identical policy sequences against the
    /// real game code; demographicpolicystress (Round 3 item 5, Part B) has no harness equivalent -
    /// added directly here, following the same pattern.
    /// -skipsimulationtestrunner - added for Master Sequence step 5a's own Play-mode diagnostics
    /// (e.g. a UI screenshot check that has nothing to do with SimulationTestRunner): SampleScene
    /// entering Play mode ran this unconditionally before this flag existed, so ANY Play-mode
    /// diagnostic - however unrelated - paid for a full 100-turn baseline pass (with per-turn logging)
    /// before its own setup ever got a chance to run. No effect on BatchSimulationRunner or any
    /// existing invocation, which never pass this flag - purely additive, opt-in only.
    /// </summary>
    public class SimulationTestRunner : MonoBehaviour
    {
        private const float MaxUnemploymentPercent = 50f;
        private const float MaxInflationPercent = 50f;
        private const float MaxSingleTurnChangePercent = 20f;

        private class Snapshot
        {
            public float GDP;
            public float Unemployment;
            public float Inflation;
            public float InterestRate;
            public float DebtToGdpRatio;
        }

        private static readonly string[] MatrixScenarios = { "baseline", "stress", "sustainedexploit", "tariffoverride", "welfarestress", "swfstress", "phase2stress", "laborstress", "crimejusticestress", "infrastructurestress", "deferredmaintenance", "growthstackstress", "demographicpolicystress", "cabinetstress", "parliamentstress" };
        private static readonly int[] MatrixTurnCounts = { 100, 500 };

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Contains("-skipsimulationtestrunner"))
            {
                return;
            }

            if (args.Contains("-runmatrix"))
            {
                // Runs every (scenario, turn count) combination in one Play session, each against its
                // own fresh World/SimulationManager, instead of requiring a separate Unity process
                // launch (and a full script recompile) per combination.
                foreach (int turnsToRun in MatrixTurnCounts)
                {
                    foreach (string scenario in MatrixScenarios)
                    {
                        RunOne(turnsToRun, scenario, logEveryTurn: false);
                    }
                }
                return;
            }

            // Master Sequence step 9, Step A0: -seed=N makes a run reproducible so before/after
            // trajectories can be compared strictly. Absent (or 0), behaviour is unchanged and every run
            // still differs, which is what real play wants.
            int seed = GetIntArg(args, "-seed=", 0);
            if (seed != 0)
            {
                SimulationRandom.Seed(seed);
                Debug.Log($"SimulationRandom seeded with {seed} - this run is reproducible.");
            }

            int singleTurnsToRun = GetIntArg(args, "-turns=", 100);
            string singleScenario = GetStringArg(args, "-scenario=", "baseline");
            RunOne(singleTurnsToRun, singleScenario, logEveryTurn: true);
        }

        /// <summary>
        /// <paramref name="logEveryTurn"/> is false for -runmatrix's 8 combinations (nobody reads
        /// 14,000+ log lines live - anomalies and the final per-combination summary are what matter,
        /// and Debug.Log's own overhead at that volume was making full 500-turn matrix runs
        /// unreasonably slow/unreliable in batch mode) - only every 25th turn plus the first and last
        /// get a full per-country log line then. CheckAnomalies still runs and Debug.LogWarning fires
        /// EVERY turn an anomaly is actually found, regardless of this flag - reduced logging only
        /// skips the routine, no-anomaly per-turn status lines. A single manual/-scenario= run keeps
        /// logging every turn (true), matching the original behavior for someone reading the Console
        /// live during interactive testing.
        /// </summary>
        private void RunOne(int turnsToRun, string scenario, bool logEveryTurn)
        {
            World world = WorldFactory.CreateDefault();
            SimulationManager simulationManager = gameObject.AddComponent<SimulationManager>();
            simulationManager.SetWorld(world);

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            var previous = new Dictionary<CountryId, Snapshot>();
            foreach (Country country in world.Countries)
            {
                decisions[country.Id] = PolicyDecision.None();
                previous[country.Id] = SnapshotOf(country);
            }

            var anomalies = new List<string>();
            Country usa = world.GetCountry(CountryId.USA);

            for (int turn = 1; turn <= turnsToRun; turn++)
            {
                decisions[CountryId.USA] = BuildUsaDecision(scenario, usa, turn, world);
                simulationManager.AdvanceTurn(decisions);

                // Political Systems Overhaul Part A: no player is present to click a response, so any
                // cabinet decision that fired this turn is auto-resolved by always picking whichever
                // option has the largest absolute combined effect - see BuildCabinetStressDecision's
                // own doc comment for why this is the correct worst-case stress for the interactive
                // decision channel specifically. Harmless no-op for every other scenario, since none of
                // them ever appoint a minister, so GetPendingCabinetDecisions is always empty there.
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in simulationManager.GetPendingCabinetDecisions(CountryId.USA).ToList())
                {
                    CabinetDecisionOption worstCaseOption = decision.Options
                        .OrderByDescending(o => Mathf.Abs(o.CrimeIndexShock) + Mathf.Abs(o.PovertyRateShock) + Mathf.Abs(o.BudgetImpact) + Mathf.Abs(o.ApprovalEffect))
                        .First();
                    simulationManager.ResolveCabinetDecision(CountryId.USA, portfolio, decision, worstCaseOption);
                }

                // Political Systems Overhaul Part B, full rollout: BudgetBill resolution is day-driven
                // (SimulationManager.AdvanceBudgetBillDay), not turn-driven, so this harness's own
                // turn-only loop (unlike GameController's real Update() day loop) has to explicitly
                // step it - CurrentDate itself is deliberately NOT advanced here (AdvanceBudgetBillDay
                // doesn't read it, only DaysRemaining), keeping every other scenario's behavior
                // byte-for-byte unchanged. Worst-case sustained stress: always keep a maximal
                // every-lever-at-its-own-ceiling omnibus bill in flight for the whole 121-day turn
                // (Tax+Spending+Welfare+SWF together, generalized from the Master Sequence step 4
                // pilot's Tax-only version once step 5c folded them into one bill), immediately
                // re-introducing the instant the previous one resolves (pass or fail) - repeatedly
                // stacking the largest possible one-time tax-hike approval penalty and revenue/spending
                // swing this mechanic can produce, the same "always the most extreme option" stress
                // philosophy cabinetstress already established for Cabinet's own interactive channel.
                if (scenario == "parliamentstress")
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        if (simulationManager.GetPendingBudgetBill(CountryId.USA) == null)
                        {
                            simulationManager.IntroduceBudgetBill(CountryId.USA, BuildParliamentStressBill(usa));
                        }
                        simulationManager.AdvanceBudgetBillDay(CountryId.USA);
                    }
                }

                bool shouldLogThisTurn = logEveryTurn || turn == 1 || turn == turnsToRun || turn % 25 == 0;

                foreach (Country country in world.Countries)
                {
                    EconomyState state = country.State;
                    Snapshot prev = previous[country.Id];
                    float growthPercent = (state.GDP - prev.GDP) / Mathf.Max(prev.GDP, 1f) * 100f;

                    if (shouldLogThisTurn)
                    {
                        Debug.Log($"[{scenario}/{turnsToRun}] Turn {turn} | {country.Name}: GDP={state.GDP:F1} ({growthPercent:+0.00;-0.00}%), " +
                            $"Unemployment={state.Unemployment:F2}%, Inflation={state.Inflation:F2}%, " +
                            $"InterestRate={country.CurrencyZone.InterestRate:F2}%, " +
                            $"GovernmentDebt={state.GovernmentDebt:F1}, DebtToGdpRatio={state.DebtToGdpRatio:F1}%, " +
                            $"Population={state.Population:F3}, PopulationGrowthRate={state.PopulationGrowthRate:F3}, DependencyRatio={state.DependencyRatio:F2}");
                    }

                    CheckAnomalies(turn, country, state, prev, anomalies);

                    previous[country.Id] = SnapshotOf(country);
                }
            }

            LogSummary(anomalies, turnsToRun, scenario);
        }

        private static int GetIntArg(string[] args, string prefix, int fallback)
        {
            string arg = args.FirstOrDefault(a => a.StartsWith(prefix));
            return arg != null ? int.Parse(arg.Substring(prefix.Length)) : fallback;
        }

        private static string GetStringArg(string[] args, string prefix, string fallback)
        {
            string arg = args.FirstOrDefault(a => a.StartsWith(prefix));
            return arg != null ? arg.Substring(prefix.Length) : fallback;
        }

        /// <summary>
        /// USA's PolicyDecision for this turn under the requested scenario - every other country
        /// always gets PolicyDecision.None(), matching the standalone harness's own convention. Ports
        /// the harness's "stress"/"sustainedexploit"/"tariffoverride" scenarios verbatim (same targets,
        /// rates, and turn timing) so BatchSimulationRunner exercises the identical policy sequences
        /// against the real game code that the harness already validated them against.
        /// </summary>
        private static PolicyDecision BuildUsaDecision(string scenario, Country usa, int turn, World world)
        {
            switch (scenario)
            {
                case "tariffoverride":
                    return BuildTariffOverrideDecision(turn);
                case "sustainedexploit":
                    return BuildSustainedExploitDecision();
                case "stress":
                    return BuildStressDecision(usa, turn);
                case "welfarestress":
                    return BuildWelfareStressDecision(usa, turn);
                case "swfstress":
                    return BuildSwfStressDecision(usa, turn, world);
                case "phase2stress":
                    return BuildPhase2StressDecision();
                case "laborstress":
                    return BuildLaborStressDecision(turn);
                case "crimejusticestress":
                    return BuildCrimeJusticeStressDecision(turn);
                case "infrastructurestress":
                    return BuildInfrastructureStressDecision();
                case "deferredmaintenance":
                    return BuildDeferredMaintenanceDecision(usa, turn);
                case "growthstackstress":
                    return BuildGrowthStackStressDecision(usa, turn);
                case "demographicpolicystress":
                    return BuildDemographicPolicyStressDecision(turn);
                case "cabinetstress":
                    return BuildCabinetStressDecision(usa, turn);
                case "parliamentstress":
                    // Political Systems Overhaul Part B, full rollout: this scenario's actual stress is
                    // applied via the omnibus BudgetBill (see RunOne's parliamentstress-only day-driving
                    // block, since Tax/Spending/Welfare/SWF no longer flow through PolicyDecision at all
                    // once gated - Master Sequence step 5c) - no lever is touched via PolicyDecision, so
                    // PolicyDecision.None() here is correct, not a placeholder.
                    return PolicyDecision.None();
                default:
                    return PolicyDecision.None();
            }
        }

        /// <summary>
        /// Creates USA's Sovereign Wealth Fund at turn 1 with the maximum contribution rate (10% of
        /// GDP/turn) and a 100% Equities allocation (the highest-average-return, highest-variance
        /// asset class) held for the entire run with no dissolution - the worst-case sustained
        /// compounding-growth stress this mechanic can produce. ALSO creates an equally-maxed fund for
        /// Germany (a Eurozone shared-currency, non-USA country with a different GDP scale/
        /// ComfortableDebtToGdpPercent anchor) at the same time, to confirm the mechanism - especially
        /// the 300%-of-GDP ceiling - generalizes beyond USA (Round 2 item 1's "expand to all six
        /// countries" own validation requirement). Mirrors the standalone harness's own --swfstress
        /// scenario exactly.
        /// </summary>
        private static PolicyDecision BuildSwfStressDecision(Country usa, int turn, World world)
        {
            if (turn == 1)
            {
                usa.SovereignWealthFund = new SovereignWealthFund
                {
                    ContributionRatePercent = 10f,
                    EquitiesWeight = 100f,
                    BondsWeight = 0f,
                    InfrastructureWeight = 0f,
                    RealEstateWeight = 0f
                };
                Country germany = world.GetCountry(CountryId.Germany);
                germany.SovereignWealthFund = new SovereignWealthFund
                {
                    ContributionRatePercent = 10f,
                    EquitiesWeight = 100f,
                    BondsWeight = 0f,
                    InfrastructureWeight = 0f,
                    RealEstateWeight = 0f
                };
            }

            return PolicyDecision.None();
        }

        /// <summary>
        /// Political Systems Overhaul Part A: appoints the HIGHEST-CompetenceBias candidate available
        /// for each of the three implemented portfolios at turn 1 and never reshuffles - the worst-case
        /// SUSTAINED stress on each portfolio's passive competence bias (see CabinetSystem.
        /// GetCompetenceBias and its three point-of-use call sites), which only ever pushes its target
        /// in one beneficial direction, so "worst case for the ceiling" here means "largest possible
        /// bias, held for the whole run," not a symmetric slider extreme. RunOne's own per-turn loop
        /// (see the ResolveCabinetDecision call there) additionally auto-resolves every decision this
        /// appointment roster fires by always picking whichever response option has the largest
        /// absolute combined effect - the worst-case stress on the INTERACTIVE decision channel too,
        /// so both of Cabinet's two independent mechanics get genuinely exercised, not just the passive
        /// one.
        /// </summary>
        private static PolicyDecision BuildCabinetStressDecision(Country usa, int turn)
        {
            if (turn == 1)
            {
                foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
                {
                    List<CabinetMinister> candidates = CabinetSystem.GenerateCandidates(portfolio);
                    usa.CabinetMinisters[portfolio] = candidates.OrderByDescending(c => c.CompetenceBias).First();
                }
            }

            return PolicyDecision.None();
        }

        /// <summary>
        /// Every TaxType implemented at its own TaxTypeRateRanges maximum, every SpendingCategory
        /// pushed for the largest increase it'll accept, every WelfareProgramType implemented at max
        /// generosity, and a maxed-out SWF - see RunOne's parliamentstress-only day-driving block for
        /// why this is submitted as a BudgetBill rather than a PolicyDecision. Master Sequence step 5d
        /// moved implement/remove out of BudgetBill (see BudgetBill's own doc comment) - a rate/
        /// generosity entry here is only meaningful for an ALREADY-implemented program, so
        /// IsImplemented is forced true directly first, bypassing the new standalone ProgramBill
        /// mechanism entirely (this harness isn't stressing tier 2, only the annual bill's rate/
        /// spending/SWF paths - same "always the most extreme option" philosophy as the rest of this
        /// scenario, just applied via direct field access since ProgramBill's own 21-day wait would
        /// defeat "worst case within one turn"). Spending/SWF requested magnitudes are deliberately
        /// larger than any real range (SimulationManager's own ApplySpendingLineChanges/
        /// ApplySwfPolicyChanges clamp internally), so this doesn't need to duplicate those private
        /// range constants here.
        /// </summary>
        private static BudgetBill BuildParliamentStressBill(Country usa)
        {
            var bill = new BudgetBill();

            foreach (TaxLine taxLine in usa.TaxLines)
            {
                taxLine.IsImplemented = true;
                bill.TaxLines[taxLine.Type] = taxLine.MaxRate;
            }

            foreach (SpendingLine spendingLine in usa.SpendingLines)
            {
                bill.SpendingPercentChanges[spendingLine.Category] = 1000f;
            }

            foreach (WelfareProgram program in usa.WelfarePrograms)
            {
                program.IsImplemented = true;
                bill.WelfarePrograms[program.Type] = 100f;
            }

            bill.SwfShouldExist = true;
            bill.SwfContributionRatePercent = 1000f;
            bill.SwfDomesticAllocationPercent = 1000f;
            bill.SwfEquitiesWeight = 1000f;
            bill.SwfBondsWeight = 1000f;
            bill.SwfInfrastructureWeight = 1000f;
            bill.SwfRealEstateWeight = 1000f;

            return bill;
        }

        private static PolicyDecision BuildWelfareStressDecision(Country usa, int turn)
        {
            if (turn != 1)
            {
                // No entries after turn 1 - GenerosityLevel persists on its own, like a tax rate.
                return PolicyDecision.None();
            }

            foreach (WelfareProgram program in usa.WelfarePrograms)
            {
                program.IsImplemented = true;
            }

            return new PolicyDecision
            {
                WelfareGenerosityOverrides = new Dictionary<WelfareProgramType, float>
                {
                    { WelfareProgramType.UBI, 90f },
                    { WelfareProgramType.NegativeIncomeTax, 90f },
                    { WelfareProgramType.MeansTestedWelfare, 90f },
                    { WelfareProgramType.UniversalHealthcare, 90f },
                    { WelfareProgramType.HousingAssistance, 90f },
                    { WelfareProgramType.ChildcareSubsidies, 90f },
                }
            };
        }

        private static PolicyDecision BuildTariffOverrideDecision(int turn)
        {
            if (turn != 1)
            {
                // No entries after turn 1 - confirms the override persists on its own
                // (TradePartner.PlayerTariffOverride) without needing to be resent every turn.
                return PolicyDecision.None();
            }

            return new PolicyDecision
            {
                PartnerTariffOverrides = new Dictionary<CountryId, float>
                {
                    { CountryId.Germany, 40f },
                    { CountryId.France, 0f },
                }
            };
        }

        private static PolicyDecision BuildSustainedExploitDecision()
        {
            return new PolicyDecision
            {
                SpendingLineChanges = new Dictionary<SpendingCategory, float>
                {
                    { SpendingCategory.Defense, 30f },
                    { SpendingCategory.Transportation, 30f },
                    { SpendingCategory.Education, 30f },
                    { SpendingCategory.HHSDiscretionary, -30f },
                    { SpendingCategory.SocialSecurity, 15f },
                    { SpendingCategory.Medicare, 15f },
                }
            };
        }

        /// <summary>
        /// Pushes all 4 new Phase 2 categories (Justice/HomelandSecurity/Energy/Housing - see
        /// CLAUDE.md's "Detailed Spending Portfolio Phase 2") to their max +30%/turn every turn,
        /// sustained for the whole run - the same "sustained extreme, no reset" stress pattern that
        /// originally found the SpendingLine compounding-growth bug, now confirming the new effects
        /// (BaselineCrimeIndex, BaselinePovertyRate, an extra BusinessConfidence nudge) stay bounded.
        /// Mirrors the standalone harness's own --phase2stress scenario exactly.
        /// </summary>
        private static PolicyDecision BuildPhase2StressDecision()
        {
            return new PolicyDecision
            {
                SpendingLineChanges = new Dictionary<SpendingCategory, float>
                {
                    { SpendingCategory.Justice, 30f },
                    { SpendingCategory.HomelandSecurity, 30f },
                    { SpendingCategory.Energy, 30f },
                    { SpendingCategory.Housing, 30f },
                }
            };
        }

        /// <summary>
        /// Pushes USA's Paid Family Leave to max (104 weeks), Overtime Regulation to max (100,
        /// strictest), and Retraining Program to max (100) simultaneously at turn 1, then holds -
        /// confirms LaborForceParticipationRate/Unemployment/ApprovalRating (and, transitively via
        /// the existing Phillips Curve, Inflation) all stay bounded under the largest possible
        /// simultaneous labor-policy push. Mirrors the standalone harness's own --laborstress
        /// scenario exactly.
        /// </summary>
        private static PolicyDecision BuildLaborStressDecision(int turn)
        {
            if (turn == 1)
            {
                return new PolicyDecision
                {
                    PaidFamilyLeaveWeeksOverride = 104f,
                    OvertimeRegulationOverride = 100f,
                    RetrainingProgramOverride = 100f
                };
            }

            return PolicyDecision.None();
        }

        /// <summary>
        /// Round 3 item 5, Part B: pushes USA's FamilyPolicyLevel and ImmigrationPolicyLevel both to
        /// their maximum (100) at turn 1, then holds - the worst-case simultaneous push toward MORE
        /// population growth, stress-testing MacroSystem.ApplyPopulationGrowth's
        /// MaxPopulationGrowthRateDeviation cap and ApplyLaborForceParticipationRate's combined ceiling
        /// (ImmigrationPolicyLevel's NetMigrationRate nudge feeds the SAME NetMigrationRate-gap term
        /// laborstress above doesn't touch) at once, under this project's now-corrected
        /// YearsPerTurn-scaled growth pipeline. No harness equivalent (added after the harness was
        /// superseded as the source of truth - see "Real-Unity Validation is the Standard Path").
        /// </summary>
        private static PolicyDecision BuildDemographicPolicyStressDecision(int turn)
        {
            if (turn == 1)
            {
                return new PolicyDecision
                {
                    FamilyPolicyOverride = 100f,
                    ImmigrationPolicyOverride = 100f
                };
            }

            return PolicyDecision.None();
        }

        /// <summary>
        /// Pushes USA's Bail Reform to max (100, full reform) and Drug Policy to max (100, strictest
        /// criminalization) simultaneously at turn 1, then holds - these two pull
        /// PrisonPopulationRate in opposite directions, stress-testing both at once. Confirms
        /// CrimeIndex/PrisonPopulationRate/ApprovalRating all stay bounded. Mirrors the standalone
        /// harness's own --crimejusticestress scenario exactly.
        /// </summary>
        /// <remarks>
        /// Round 3 item 3 extended this to also push Judicial Funding and Border Enforcement to their
        /// OWN worst case (0, minimum - both REDUCE OrganizedCrimeIndex/CorruptionIndex/
        /// PrisonPopulationRate when higher, so 0 maximizes all three) alongside the original two -
        /// now stress-testing all six Crime &amp; Justice dials and both new tracked stats
        /// (OrganizedCrimeIndex/CorruptionIndex) at once, confirming BusinessConfidence (their shared
        /// new output channel) and ApprovalRating (CorruptionIndex's new channel) both stay bounded
        /// too. PoliceFundingLevel is deliberately left at its neutral default here, isolating the two
        /// genuinely NEW levers rather than re-testing the already-proven PoliceFundingLevel.
        /// </remarks>
        private static PolicyDecision BuildCrimeJusticeStressDecision(int turn)
        {
            if (turn == 1)
            {
                return new PolicyDecision
                {
                    BailReformOverride = 100f,
                    DrugPolicyOverride = 100f,
                    JudicialFundingOverride = 0f,
                    BorderEnforcementOverride = 0f
                };
            }

            return PolicyDecision.None();
        }

        /// <summary>
        /// Pushes USA's Transportation spending DOWN 30%/turn EVERY turn, sustained for the whole run
        /// with no reset - the line hits its own 0.2x-of-SeedAmount floor within ~9 turns and stays
        /// there, so InfrastructureSpendingChange (the investment signal
        /// MacroSystem.ApplyInfrastructureCondition reads) craters to ~0 for the rest of the run,
        /// leaving only passive decay acting on every InfrastructureAsset.ConditionIndex - the
        /// worst-case "zero investment, pure decay" test. The existing sustainedexploit scenario
        /// already covers the opposite case (sustained +30% Transportation, ceiling-bound), so this
        /// deliberately covers the case it doesn't. Mirrors the standalone harness's own
        /// --infrastructurestress scenario exactly.
        /// </summary>
        private static PolicyDecision BuildInfrastructureStressDecision()
        {
            return new PolicyDecision
            {
                SpendingLineChanges = new Dictionary<SpendingCategory, float>
                {
                    { SpendingCategory.Transportation, -30f },
                }
            };
        }

        /// <summary>
        /// Infrastructure Feedback follow-up: directly forces every USA InfrastructureAsset.
        /// ConditionIndex to 0 at turn 1 (the theoretical worst case - a 50-point gap below
        /// MacroSystem.InfrastructureConditionGrowthThreshold, well past where the condition-drag's
        /// own individual cap already binds), THEN sustains a -30%/turn Transportation cut every turn
        /// for the whole run so nothing can recover it - isolating and maximally stressing the
        /// ConditionIndex -&gt; PotentialGrowthRate drag specifically (distinct from
        /// infrastructurestress above, which stresses the ConditionIndex STOCK bound via a slower
        /// ~9-turn decay-to-floor path, not the growth-rate feedback). Mirrors the standalone
        /// harness's own --deferredmaintenance scenario exactly.
        /// </summary>
        private static PolicyDecision BuildDeferredMaintenanceDecision(Country usa, int turn)
        {
            if (turn == 1)
            {
                foreach (InfrastructureAsset asset in usa.InfrastructureAssets)
                {
                    asset.ConditionIndex = 0f;
                }
            }

            return new PolicyDecision
            {
                SpendingLineChanges = new Dictionary<SpendingCategory, float>
                {
                    { SpendingCategory.Transportation, -30f },
                }
            };
        }

        /// <summary>
        /// Sector Integration follow-up: worst-case SAME-DIRECTION stacking test for
        /// MacroSystem.MaxTotalPotentialGrowthAdjustment - the single most important safeguard added
        /// alongside Sector Integration. Forces every USA InfrastructureAsset.ConditionIndex to 0 at
        /// turn 1 (max condition drag) AND pushes EVERY Sector to its weakest simultaneous performance
        /// (min Subsidy/max Regulation - the largest possible negative Output/Employment gap) - both
        /// Infrastructure's and Sector's contributions push PotentialGrowthRate DOWN at the same time,
        /// the genuinely dangerous case for an additive combined ceiling (distinct from
        /// deferredmaintenance above, which only stresses Infrastructure's own sub-ceiling in
        /// isolation). Also sustains -30%/turn Transportation cuts to keep ConditionIndex pinned at 0
        /// for the whole run. Mirrors the standalone harness's own --growthstackstress scenario
        /// exactly.
        /// </summary>
        /// <remarks>
        /// Round 3 item 2 extended this to also push all three new sector policy dials to their own
        /// Output-worst-case setting (min Tax Credits/Research Grants, fully NATIONALIZED - see
        /// Sector.DeregulationNationalizationLevel) - five simultaneous downward-pushing sources per
        /// sector, feeding the SAME MaxTotalPotentialGrowthAdjustment ceiling. Honest caveat:
        /// DeregulationNationalizationLevel=0 (full nationalization) is worst-case for OUTPUT but
        /// pushes Employment the OPPOSITE direction (nationalization preserves jobs - see Sector.cs) -
        /// this scenario is not simultaneously worst-case for
        /// MacroSystem.MaxSectorUnemploymentAdjustment/Okun's Law the way it is for
        /// MaxTotalPotentialGrowthAdjustment, which remains its primary target exactly as before.
        ///
        /// Round 3 item 4 doubled the sector count (Manufacturing/Technology/Agriculture/Finance plus
        /// Energy/Construction/Retail/Telecommunications) - this method was refactored from five
        /// hand-listed 4-entry dictionaries to a loop over EVERY SectorType, both so the stress
        /// scenario genuinely covers "all new and existing sectors" (the task's own explicit wording)
        /// without relying on someone remembering to hand-add each new sector here, and so it
        /// automatically keeps covering every sector this project adds in the future.
        /// </remarks>
        private static PolicyDecision BuildGrowthStackStressDecision(Country usa, int turn)
        {
            if (turn == 1)
            {
                foreach (InfrastructureAsset asset in usa.InfrastructureAssets)
                {
                    asset.ConditionIndex = 0f;
                }
            }

            var subsidyOverrides = new Dictionary<SectorType, float>();
            var regulationOverrides = new Dictionary<SectorType, float>();
            var taxCreditOverrides = new Dictionary<SectorType, float>();
            var researchGrantsOverrides = new Dictionary<SectorType, float>();
            var deregulationOverrides = new Dictionary<SectorType, float>();
            foreach (SectorType type in System.Enum.GetValues(typeof(SectorType)))
            {
                subsidyOverrides[type] = 0f;
                regulationOverrides[type] = 100f;
                taxCreditOverrides[type] = 0f;
                researchGrantsOverrides[type] = 0f;
                deregulationOverrides[type] = 0f;
            }

            return new PolicyDecision
            {
                SpendingLineChanges = new Dictionary<SpendingCategory, float>
                {
                    { SpendingCategory.Transportation, -30f },
                },
                SectorSubsidyOverrides = subsidyOverrides,
                SectorRegulationOverrides = regulationOverrides,
                SectorTaxCreditOverrides = taxCreditOverrides,
                SectorResearchGrantsOverrides = researchGrantsOverrides,
                SectorDeregulationNationalizationOverrides = deregulationOverrides
            };
        }

        private static PolicyDecision BuildStressDecision(Country usa, int turn)
        {
            if (turn == 10)
            {
                usa.TaxLines.First(t => t.Type == TaxType.WealthTax).IsImplemented = true;
                usa.TaxLines.First(t => t.Type == TaxType.CarbonTax).IsImplemented = true;
            }
            if (turn == 20)
            {
                usa.TaxLines.First(t => t.Type == TaxType.EstateTax).IsImplemented = false;
            }

            float incomeTarget = Mathf.Min(TaxTypeRateRanges.IncomeTaxMax - 1f, 37f + turn * 0.4f);
            float payrollTarget = Mathf.Min(TaxTypeRateRanges.PayrollTaxMax - 1f, 15.3f + turn * 0.6f);
            float corporateTarget = Mathf.Min(TaxTypeRateRanges.CorporateTaxMax - 1f, 21f + turn * 0.3f);

            var taxOverrides = new Dictionary<TaxType, float>
            {
                { TaxType.IncomeTax, incomeTarget },
                { TaxType.CorporateTax, corporateTarget },
                { TaxType.PayrollTax, payrollTarget },
            };
            if (turn >= 10)
            {
                taxOverrides[TaxType.WealthTax] = Mathf.Min(TaxTypeRateRanges.WealthTaxMax, turn * 0.05f);
                taxOverrides[TaxType.CarbonTax] = Mathf.Min(TaxTypeRateRanges.CarbonTaxMax - 1f, 5f + turn * 0.9f);
            }

            int cyclePosition = (turn / 5) % 2;
            float mandatorySign = cyclePosition == 0 ? 1f : -1f;
            var spendingOverrides = new Dictionary<SpendingCategory, float>();
            if (turn % 5 == 0)
            {
                spendingOverrides[SpendingCategory.SocialSecurity] = mandatorySign * 15f;
                spendingOverrides[SpendingCategory.Medicare] = mandatorySign * 14f;
                spendingOverrides[SpendingCategory.Medicaid] = mandatorySign * 15f;
                spendingOverrides[SpendingCategory.IncomeSecurity] = -mandatorySign * 15f;
                spendingOverrides[SpendingCategory.VeteransBenefitsMandatory] = -mandatorySign * 13f;
                spendingOverrides[SpendingCategory.FederalRetirement] = mandatorySign * 15f;
                spendingOverrides[SpendingCategory.Defense] = mandatorySign * 30f;
                spendingOverrides[SpendingCategory.HHSDiscretionary] = -mandatorySign * 30f;
                spendingOverrides[SpendingCategory.Transportation] = mandatorySign * 30f;
                spendingOverrides[SpendingCategory.Education] = mandatorySign * 30f;
            }

            return new PolicyDecision
            {
                TaxRateOverrides = taxOverrides,
                SpendingLineChanges = spendingOverrides,
                TariffRateChange = (turn % 5 == 0) ? 1f : 0f
            };
        }

        private static Snapshot SnapshotOf(Country country)
        {
            return new Snapshot
            {
                GDP = country.State.GDP,
                Unemployment = country.State.Unemployment,
                Inflation = country.State.Inflation,
                InterestRate = country.CurrencyZone.InterestRate,
                DebtToGdpRatio = country.State.DebtToGdpRatio
            };
        }

        private static void CheckAnomalies(int turn, Country country, EconomyState state, Snapshot previous, List<string> anomalies)
        {
            if (state.GDP < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: negative GDP ({state.GDP:F1})");
            }

            if (state.Unemployment < 0f || state.Unemployment > MaxUnemploymentPercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: unemployment out of range ({state.Unemployment:F2}%)");
            }

            if (state.Inflation < 0f || state.Inflation > MaxInflationPercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: inflation out of range ({state.Inflation:F2}%)");
            }

            if (state.GovernmentDebt < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: negative GovernmentDebt ({state.GovernmentDebt:F1})");
            }

            CheckFinite(turn, country, "GDP", state.GDP, anomalies);
            CheckFinite(turn, country, "Inflation", state.Inflation, anomalies);
            CheckFinite(turn, country, "Unemployment", state.Unemployment, anomalies);
            CheckFinite(turn, country, "ApprovalRating", state.ApprovalRating, anomalies);
            CheckFinite(turn, country, "Budget", state.Budget, anomalies);
            foreach (TaxLine taxLine in country.TaxLines)
            {
                CheckFinite(turn, country, $"TaxLine[{taxLine.Type}].Rate", taxLine.Rate, anomalies);
            }
            foreach (SpendingLine spendingLine in country.SpendingLines)
            {
                CheckFinite(turn, country, $"SpendingLine[{spendingLine.Category}].Amount", spendingLine.Amount, anomalies);
                if (spendingLine.Amount < 0f)
                {
                    anomalies.Add($"Turn {turn} {country.Name}: SpendingLine[{spendingLine.Category}].Amount is negative ({spendingLine.Amount:F1})");
                }
            }
            CheckFinite(turn, country, "TradeBalance", state.TradeBalance, anomalies);
            CheckFinite(turn, country, "CurrencyStrength", state.CurrencyStrength, anomalies);
            CheckFinite(turn, country, "Consumption", state.Consumption, anomalies);
            CheckFinite(turn, country, "Investment", state.Investment, anomalies);
            CheckFinite(turn, country, "PotentialGDP", state.PotentialGDP, anomalies);
            CheckFinite(turn, country, "InflationExpectations", state.InflationExpectations, anomalies);
            CheckFinite(turn, country, "ConsumerConfidence", state.ConsumerConfidence, anomalies);
            CheckFinite(turn, country, "BusinessConfidence", state.BusinessConfidence, anomalies);
            CheckFinite(turn, country, "InterestRate", country.CurrencyZone.InterestRate, anomalies);
            CheckFinite(turn, country, "GovernmentDebt", state.GovernmentDebt, anomalies);
            CheckFinite(turn, country, "DebtToGdpRatio", state.DebtToGdpRatio, anomalies);
            CheckFinite(turn, country, "PovertyRate", state.PovertyRate, anomalies);
            if (state.PovertyRate < 0f || state.PovertyRate > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: PovertyRate out of range ({state.PovertyRate:F2}%)");
            }
            foreach (WelfareProgram welfareProgram in country.WelfarePrograms)
            {
                CheckFinite(turn, country, $"WelfareProgram[{welfareProgram.Type}].GenerosityLevel", welfareProgram.GenerosityLevel, anomalies);
            }

            CheckFinite(turn, country, "LaborForceParticipationRate", state.LaborForceParticipationRate, anomalies);
            if (state.LaborForceParticipationRate < 0f || state.LaborForceParticipationRate > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: LaborForceParticipationRate out of range ({state.LaborForceParticipationRate:F2}%)");
            }

            CheckFinite(turn, country, "CrimeIndex", state.CrimeIndex, anomalies);
            if (state.CrimeIndex < 0f || state.CrimeIndex > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: CrimeIndex out of range ({state.CrimeIndex:F2})");
            }

            CheckFinite(turn, country, "BaselineCrimeIndex", country.BaselineCrimeIndex, anomalies);
            CheckFinite(turn, country, "BaselinePovertyRate", country.BaselinePovertyRate, anomalies);

            CheckFinite(turn, country, "OrganizedCrimeIndex", state.OrganizedCrimeIndex, anomalies);
            if (state.OrganizedCrimeIndex < 0f || state.OrganizedCrimeIndex > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: OrganizedCrimeIndex out of range ({state.OrganizedCrimeIndex:F2})");
            }
            CheckFinite(turn, country, "BaselineOrganizedCrimeIndex", country.BaselineOrganizedCrimeIndex, anomalies);

            CheckFinite(turn, country, "CorruptionIndex", state.CorruptionIndex, anomalies);
            if (state.CorruptionIndex < 0f || state.CorruptionIndex > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: CorruptionIndex out of range ({state.CorruptionIndex:F2})");
            }
            CheckFinite(turn, country, "BaselineCorruptionIndex", country.BaselineCorruptionIndex, anomalies);

            // Round 3 item 5, Part A: Population/BirthRate/DeathRate/NetMigrationRate/DependencyRatio.
            CheckFinite(turn, country, "Population", state.Population, anomalies);
            if (state.Population <= 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: Population is non-positive ({state.Population:F3})");
            }

            CheckFinite(turn, country, "BirthRate", state.BirthRate, anomalies);
            if (state.BirthRate < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: BirthRate is negative ({state.BirthRate:F2})");
            }

            CheckFinite(turn, country, "DeathRate", state.DeathRate, anomalies);
            if (state.DeathRate < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: DeathRate is negative ({state.DeathRate:F2})");
            }

            CheckFinite(turn, country, "NetMigrationRate", state.NetMigrationRate, anomalies);
            CheckFinite(turn, country, "NaturalBirthRate", state.NaturalBirthRate, anomalies);
            CheckFinite(turn, country, "NaturalNetMigrationRate", state.NaturalNetMigrationRate, anomalies);

            CheckFinite(turn, country, "DependencyRatio", state.DependencyRatio, anomalies);
            if (state.DependencyRatio < 0f || state.DependencyRatio > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: DependencyRatio out of range ({state.DependencyRatio:F2})");
            }
            CheckFinite(turn, country, "BaselineDependencyRatio", country.BaselineDependencyRatio, anomalies);
            CheckFinite(turn, country, "BaselineNetMigrationRate", country.BaselineNetMigrationRate, anomalies);

            CheckFinite(turn, country, "PopulationGrowthRate", state.PopulationGrowthRate, anomalies);
            if (state.PopulationGrowthRate < -50f || state.PopulationGrowthRate > 50f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: PopulationGrowthRate out of a sane range ({state.PopulationGrowthRate:F3})");
            }
            CheckFinite(turn, country, "SteadyStateGrowthRate", country.SteadyStateGrowthRate, anomalies);

            CheckFinite(turn, country, "FamilyPolicyLevel", country.FamilyPolicyLevel, anomalies);
            if (country.FamilyPolicyLevel < 0f || country.FamilyPolicyLevel > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: FamilyPolicyLevel out of range ({country.FamilyPolicyLevel:F2})");
            }
            CheckFinite(turn, country, "ImmigrationPolicyLevel", country.ImmigrationPolicyLevel, anomalies);
            if (country.ImmigrationPolicyLevel < 0f || country.ImmigrationPolicyLevel > 100f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: ImmigrationPolicyLevel out of range ({country.ImmigrationPolicyLevel:F2})");
            }

            CheckFinite(turn, country, "PrisonPopulationRate", state.PrisonPopulationRate, anomalies);
            if (state.PrisonPopulationRate < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: PrisonPopulationRate is negative ({state.PrisonPopulationRate:F2})");
            }
            CheckFinite(turn, country, "BaselinePrisonPopulationRate", country.BaselinePrisonPopulationRate, anomalies);

            CheckFinite(turn, country, "PotentialGrowthRate", country.PotentialGrowthRate, anomalies);
            if (country.PotentialGrowthRate < 0f || country.PotentialGrowthRate > 8f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: PotentialGrowthRate out of range ({country.PotentialGrowthRate:F2})");
            }
            CheckFinite(turn, country, "BasePotentialGrowthRate", country.BasePotentialGrowthRate, anomalies);
            CheckFinite(turn, country, "InfrastructureSpendingGrowthAdjustment", country.InfrastructureSpendingGrowthAdjustment, anomalies);
            if (country.InfrastructureSpendingGrowthAdjustment < 0f || country.InfrastructureSpendingGrowthAdjustment > 1f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: InfrastructureSpendingGrowthAdjustment out of range ({country.InfrastructureSpendingGrowthAdjustment:F2})");
            }

            foreach (Sector sector in country.Sectors)
            {
                CheckFinite(turn, country, $"Sector[{sector.Type}].OutputShareOfGdp", sector.OutputShareOfGdp, anomalies);
                CheckFinite(turn, country, $"Sector[{sector.Type}].EmploymentShare", sector.EmploymentShare, anomalies);
                CheckFinite(turn, country, $"Sector[{sector.Type}].SectorMetric", sector.SectorMetric, anomalies);
                if (sector.OutputShareOfGdp < 0f)
                {
                    anomalies.Add($"Turn {turn} {country.Name}: Sector[{sector.Type}].OutputShareOfGdp is negative ({sector.OutputShareOfGdp:F2})");
                }
            }

            foreach (InfrastructureAsset asset in country.InfrastructureAssets)
            {
                CheckFinite(turn, country, $"InfrastructureAsset[{asset.Type}].ConditionIndex", asset.ConditionIndex, anomalies);
                if (asset.ConditionIndex < 0f || asset.ConditionIndex > 100f)
                {
                    anomalies.Add($"Turn {turn} {country.Name}: InfrastructureAsset[{asset.Type}].ConditionIndex out of range ({asset.ConditionIndex:F2})");
                }
            }

            if (country.SovereignWealthFund != null)
            {
                CheckFinite(turn, country, "SovereignWealthFund.TotalAssets", country.SovereignWealthFund.TotalAssets, anomalies);
                if (country.SovereignWealthFund.TotalAssets < 0f)
                {
                    anomalies.Add($"Turn {turn} {country.Name}: SovereignWealthFund.TotalAssets is negative ({country.SovereignWealthFund.TotalAssets:F2})");
                }
            }

            CheckSwing(turn, country, "GDP", previous.GDP, state.GDP, anomalies);
            CheckSwing(turn, country, "Unemployment", previous.Unemployment, state.Unemployment, anomalies);
            CheckSwing(turn, country, "Inflation", previous.Inflation, state.Inflation, anomalies);
            CheckSwing(turn, country, "InterestRate", previous.InterestRate, country.CurrencyZone.InterestRate, anomalies);
            CheckSwing(turn, country, "DebtToGdpRatio", previous.DebtToGdpRatio, state.DebtToGdpRatio, anomalies);
        }

        private static void CheckFinite(int turn, Country country, string field, float value, List<string> anomalies)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                anomalies.Add($"Turn {turn} {country.Name}: {field} is not finite ({value})");
            }
        }

        private static void CheckSwing(int turn, Country country, string field, float previousValue, float currentValue, List<string> anomalies)
        {
            if (Mathf.Abs(previousValue) < 0.01f)
            {
                return;
            }

            float changePercent = Mathf.Abs(currentValue - previousValue) / Mathf.Abs(previousValue) * 100f;
            if (changePercent > MaxSingleTurnChangePercent)
            {
                anomalies.Add($"Turn {turn} {country.Name}: {field} swung {changePercent:F1}% in one turn ({previousValue:F2} -> {currentValue:F2})");
            }
        }

        private static void LogSummary(List<string> anomalies, int turnsToRun, string scenario)
        {
            if (anomalies.Count == 0)
            {
                Debug.Log($"Sanity check complete: {turnsToRun} turns ({scenario}), no anomalies detected.");
                return;
            }

            Debug.LogWarning($"Sanity check complete: {turnsToRun} turns ({scenario}), {anomalies.Count} anomalies detected:");
            foreach (string anomaly in anomalies)
            {
                Debug.LogWarning(anomaly);
            }
        }
    }
}
