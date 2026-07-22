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
    /// Reads two optional command-line arguments (so `Assets/Editor/BatchSimulationRunner.cs` can
    /// drive it headlessly without touching this file per run - see CLAUDE.md's "Real-Unity
    /// Validation is the Standard Path" for why this replaced the standalone harness as the primary
    /// validation tool):
    /// -turns=N (default 100) - how many turns to run.
    /// -scenario=baseline|stress|sustainedexploit|tariffoverride|welfarestress|swfstress|
    /// phase2stress|laborstress|crimejusticestress (default baseline) - baseline is
    /// PolicyDecision.None() for every country every turn (the original behavior, unchanged); the
    /// other eight mirror the standalone harness's own same-named scenarios byte-for-byte (same
    /// targets, same rates, same turn timing) so both tools exercise identical policy sequences
    /// against the real game code.
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

        private static readonly string[] MatrixScenarios = { "baseline", "stress", "sustainedexploit", "tariffoverride", "welfarestress", "swfstress", "phase2stress", "laborstress", "crimejusticestress" };
        private static readonly int[] MatrixTurnCounts = { 100, 500 };

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();

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
                            $"GovernmentDebt={state.GovernmentDebt:F1}, DebtToGdpRatio={state.DebtToGdpRatio:F1}%");
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
        /// Pushes USA's Bail Reform to max (100, full reform) and Drug Policy to max (100, strictest
        /// criminalization) simultaneously at turn 1, then holds - these two pull
        /// PrisonPopulationRate in opposite directions, stress-testing both at once. Confirms
        /// CrimeIndex/PrisonPopulationRate/ApprovalRating all stay bounded. Mirrors the standalone
        /// harness's own --crimejusticestress scenario exactly.
        /// </summary>
        private static PolicyDecision BuildCrimeJusticeStressDecision(int turn)
        {
            if (turn == 1)
            {
                return new PolicyDecision { BailReformOverride = 100f, DrugPolicyOverride = 100f };
            }

            return PolicyDecision.None();
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

            CheckFinite(turn, country, "PrisonPopulationRate", state.PrisonPopulationRate, anomalies);
            if (state.PrisonPopulationRate < 0f)
            {
                anomalies.Add($"Turn {turn} {country.Name}: PrisonPopulationRate is negative ({state.PrisonPopulationRate:F2})");
            }
            CheckFinite(turn, country, "BaselinePrisonPopulationRate", country.BaselinePrisonPopulationRate, anomalies);

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
